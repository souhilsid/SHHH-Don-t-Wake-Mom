using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Hands-first grabbing for one hand: hover highlight, pinch or fist grab with hysteresis, physics-following carry
    /// (held items still collide, so you can't push them through walls and slamming them makes noise), two-hand holds,
    /// clamped throws, and tracking-loss stabilisation (held items freeze, then are lowered gently — never flung).
    /// Also drives a kinematic palm collider so hands can physically bump things (attributed to this sibling).
    /// </summary>
    public sealed class HandInteractor : MonoBehaviour
    {
        public HandSource Source;
        public HandInteractor Other;
        public int Actor => GameServices.LocalSlot;
        public PropBody Held { get; private set; }
        public bool IsGrabbing => Held != null;
        public bool IsHovering => _hover != null && !_farHover;
        /// <summary>Diagnostics (xr_diag harness, logcat): current hover target and whether it is a far (magic-pull) target.</summary>
        public PropBody HoverTarget => _hover;
        public bool FarHover => _farHover;
        public string LastGrabFailure { get; private set; } = "";
        public int GrabAttempts { get; private set; }
        public int GrabSuccesses { get; private set; }
        public bool Enabled = true;

        PropBody _hover;
        bool _grabLatched;
        Vector3 _localGrabPos; Quaternion _localGrabRot;
        readonly Queue<Vector3> _vel = new Queue<Vector3>(); Vector3 _lastPinch; bool _haveLast;
        float _lostSince = -1f; bool _frozen; float _softDropT = -1f; Vector3 _softFrom, _softTo;
        Rigidbody _palmRb; ActorTag _palmTag; SphereCollider _palmCol;
        public const float MaxThrowSpeed = 5.5f;

        void Start()
        {
            var palm = new GameObject(Source.Hand + "_palm");
            palm.layer = Layers.Hands;
            var sc = palm.AddComponent<SphereCollider>(); sc.radius = 0.035f; _palmCol = sc; sc.enabled = false;
            _palmRb = palm.AddComponent<Rigidbody>(); _palmRb.isKinematic = true; _palmRb.interpolation = RigidbodyInterpolation.Interpolate;
            _palmTag = palm.AddComponent<ActorTag>(); _palmTag.ActorId = Actor;
            palm.transform.SetParent(transform, true);
        }

        float _latchTime, _lowSince = -1f, _holdLogAt;
        /// <summary>Grab may start up to 0.8 s after the pinch latched (players close their fingers while still arriving).</summary>
        public const float GrabWindow = 0.8f;
        bool GrabWindowOpen => _grabLatched && Time.time - _latchTime < GrabWindow;

        bool WantsGrab()
        {
            float p = Source.Pinch, g = Source.Grip;
            // V13: a finger gun (index out, three fingers curled) is the locomotion gesture - it never latches a grab
            if (!_grabLatched && !Source.GunPose && (p > 0.75f || g > 0.7f)) { _grabLatched = true; _latchTime = Time.time; _lowSince = -1f; LogLatch(p, g); }
            else if (_grabLatched)
            {
                // Release needs a deliberate open hand for 80 ms (OVR pinch strength flickers mid-carry -> phantom drops).
                // V12: an open hand (all four fingers straight, no pinch) always lets go, whatever the grip estimate says.
                bool open = (p < 0.4f && g < 0.4f) || Source.OpenHand;
                if (open) { if (_lowSince < 0f) _lowSince = Time.time; else if (Time.time - _lowSince > 0.08f) { _grabLatched = false; if (Held != null) GrabLog.Add($"{Source.Hand} RELEASE {Held.Spec.Kind}#{Held.Spec.Id} p={p:0.00} g={g:0.00} open={Source.OpenHand}"); } }
                else _lowSince = -1f;
                // while holding: one line a second, so a stuck item shows why on the device (adb pull shhh_grablog.txt)
                if (Held != null && Time.time >= _holdLogAt) { _holdLogAt = Time.time + 1f; var o = Source as OvrHandSource; GrabLog.Add($"{Source.Hand} HOLD {Held.Spec.Kind} p={p:0.00} g={g:0.00} open={Source.OpenHand}{(o != null ? $" curl i{o.FingerCurlOf(0):0.00} m{o.FingerCurlOf(1):0.00} r{o.FingerCurlOf(2):0.00} l{o.FingerCurlOf(3):0.00}" : "")}"); }
            }
            return _grabLatched;
        }

        void Update()
        {
            if (Source == null) return;
            _palmTag.ActorId = Actor;
            bool tracked = Source.Tracked && !Source.SystemGesture;
            if (!tracked) { HandleTrackingLoss(); return; }
            if (_lostSince >= 0) RecoverTracking();

            Vector3 pinch = Source.PinchPoint;
            if (_haveLast) { _vel.Enqueue((pinch - _lastPinch) / Mathf.Max(1e-4f, Time.deltaTime)); while (_vel.Count > 6) _vel.Dequeue(); }
            _lastPinch = pinch; _haveLast = true;

            bool spectator = GameServices.Round != null && ActorIds.IsSibling(Actor) && !GameServices.Round.Roster[Actor].IsActive;
            bool wants = WantsGrab() && Enabled && !spectator;

            if (Held == null)
            {
                // V12: a hand on a mechanism handle (door, drawer, dial) holds the handle until it opens; no prop grab meanwhile
                var mech = MechPart.HeldBy(this);
                if (mech != null) { if (!wants) mech.EndGrab(); _wasGrabbingLastFrame = wants; return; }
                UpdateHover(pinch, spectator);
                if (wants && !spectator && (!_wasGrabbingLastFrame || GrabWindowOpen))
                {
                    // the handle the fingers are actually on wins over a loose prop behind it (surface distance, V11 selection rule)
                    var m = MechPart.Nearest(pinch, out float md);
                    float pd = _hover != null && _hover.Col != null && !_farHover ? Vector3.Distance(_hover.Col.ClosestPoint(pinch), pinch) : float.MaxValue;
                    if (m != null && md <= pd)
                    {
                        if (_hover != null) { _hover.SetHighlight(false, Palette.Yellow); _hover = null; }
                        m.BeginGrab(this, pinch); _wasGrabbingLastFrame = wants; return;
                    }
                }
                // Inside the late-grab window only take an item the hand has arrived at (slowed down) or is touching - so a pinch
                // closed early while sweeping past a neighbour (xr_diag: fork -> grabbed the sock) doesn't snatch the wrong thing.
                bool arrived = HandSpeed() < 0.35f || (_hover != null && !_farHover && _hover.Col != null && Vector3.Distance(_hover.Col.ClosestPoint(pinch), pinch) < 0.04f);
                // V12 (Quest run: players pinch first, then reach): a pinch held past the late-grab window still takes an item once the
                // fingers are ON it and the hand has stopped - but never with the palm up (that pinch-and-hold is the walk gesture)
                bool lateTouch = wants && _wasGrabbingLastFrame && !GrabWindowOpen && _hover != null && !_farHover && FingerDistance(_hover) < 0.03f
                                 && HandSpeed() < 0.25f && Vector3.Dot(Source.PalmNormal, Vector3.up) < 0.6f && Time.time - _lastLateTry > 0.5f
                                 && (Source.Pinch > 0.75f || Source.Grip > 0.7f);   // actually pinching now (not the 80 ms release grace)
                if (lateTouch) _lastLateTry = Time.time;
                if (wants && _hover != null && (((!_wasGrabbingLastFrame || GrabWindowOpen) && (arrived || _farHover)) || lateTouch)) TryGrab(_hover);
                else if (wants && !_wasGrabbingLastFrame) LogMiss(pinch, spectator);
            }
            else if (!wants) Release();
            _wasGrabbingLastFrame = wants;
        }
        bool _wasGrabbingLastFrame; float _lastLateTry = -9f;
        float HandSpeed() { if (_vel.Count == 0) return 0f; Vector3 v = Vector3.zero; foreach (var x in _vel) v += x; return (v / _vel.Count).magnitude; }

        void FixedUpdate()
        {
            if (_palmRb != null && Source != null && Source.Tracked)
            {
                _palmRb.MovePosition(Source.PalmCenter);
                // Palm bumps only for deliberate swipes, so reaching for an item never shoves it away.
                bool swipe = _palmTag.Speed > 0.7f && _hover == null && Held == null && Source.Pinch < 0.3f;
                if (_palmCol.enabled != swipe) _palmCol.enabled = swipe;
            }
            if (Held == null || _frozen) { if (_softDropT >= 0) StepSoftDrop(); return; }
            if (!Source.Tracked || Source.SystemGesture)
            {
                // Tracking just dropped: never chase stale/jumping joint data - hold the item still until it freezes.
                Held.Body.linearVelocity = Vector3.zero; Held.Body.angularVelocity = Vector3.zero;
                return;
            }
            // Physics-following carry: drive velocity toward the hand pose (respects collisions), with a snap-back limit.
            Vector3 targetPos; Quaternion targetRot;
            if (Other != null && Other.Held == Held && Source.Hand == Handedness.Right)
            {
                // Two-hand hold: centre between both hands, orientation from the hand-to-hand axis.
                targetPos = Vector3.Lerp(Source.PinchPoint, Other.Source.PinchPoint, 0.5f) + Held.transform.rotation * Vector3.zero;
                var axis = Source.PinchPoint - Other.Source.PinchPoint;
                targetRot = axis.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(Vector3.Cross(axis, Vector3.up).normalized, Vector3.up) : Held.transform.rotation;
            }
            else if (Other != null && Other.Held == Held && Source.Hand == Handedness.Left) return;
            else
            {
                var hr = Source.HandRotation;
                targetRot = hr * _localGrabRot;
                targetPos = Source.PinchPoint + hr * _localGrabPos;
            }
            var rb = Held.Body;
            if (GameServices.IsReplica)
            {
                // Client prediction: the item is network-kinematic here; move it with the hand. If the host decided
                // otherwise (taken by a rival, Mom froze play, eliminated), drop the prediction.
                int holder = GameServices.Net != null ? GameServices.Net.HolderOf(Held.Spec.Id) : Actor;
                if (Time.time - _predictedAt > 0.8f && holder != Actor)
                {
                    var lost = Held; Held = null; _pullT = -1f; lost.OnReleased(Actor, Vector3.zero, false);
                    Fail(holder >= 0 ? "a rival got it first" : "the host refused the grab", lost.transform.position, holder < 0);
                    return;
                }
                if (_pullT >= 0f) { _pullT += Time.fixedDeltaTime / _pullDur; targetPos = Vector3.Lerp(_pullFrom, targetPos, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, _pullT))); if (_pullT >= 1f) _pullT = -1f; }
                rb.MovePosition(targetPos); rb.MoveRotation(targetRot);
                return;
            }
            if (_pullT >= 0f)
            {
                _pullT += Time.fixedDeltaTime / _pullDur;
                rb.MovePosition(Vector3.Lerp(_pullFrom, targetPos, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, _pullT))) + Vector3.up * Mathf.Sin(Mathf.Min(1f, _pullT) * Mathf.PI) * _pullArc);
                rb.linearVelocity = Vector3.zero;
                if (_pullT >= 1f) _pullT = -1f;
                return;
            }
            Vector3 delta = targetPos - rb.position;
            if (delta.magnitude > 0.7f) { Release(); return; } // item stuck behind geometry: let go rather than tunnel
            rb.linearVelocity = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime, 8f);
            Quaternion dq = targetRot * Quaternion.Inverse(rb.rotation);
            dq.ToAngleAxis(out float ang, out Vector3 ax);
            if (ang > 180f) ang -= 360f;
            if (Mathf.Abs(ang) > 0.01f && !float.IsNaN(ax.x)) rb.angularVelocity = Vector3.ClampMagnitude(ax * (ang * Mathf.Deg2Rad / Time.fixedDeltaTime), 25f);
        }

        void UpdateHover(Vector3 pinch, bool spectator)
        {
            PropBody best = null;
            if (!spectator)
            {
                float bestD = HoverAccept;
                var cols = Physics.OverlapSphere(pinch, HoverRadius, Layers.PropsMask, QueryTriggerInteraction.Ignore);
                foreach (var c in cols)
                {
                    var p = c.GetComponentInParent<PropBody>();
                    if (p == null || !p.Spec.Grabbable) continue;
                    // the pinch point or the index fingertip, whichever is on the item (tracked pinch points sit between the tips)
                    float d = Mathf.Min(Vector3.Distance(c.ClosestPoint(pinch), pinch), Vector3.Distance(c.ClosestPoint(Source.IndexTip), Source.IndexTip));
                    if (d < bestD) { bestD = d; best = p; }
                }
            }
            // V6 root cause #6 (xr_diag): _farHover was cleared BEFORE FindDistant read it, so closing the pinch on a far target
            // (pinch > 0.6, before the 0.75 latch) always lost the target - magic pull could never fire.
            bool wasFar = _farHover;
            _farHover = false;
            // V12: a palm-up hand is walking / aiming a hop - its pointer ray must not pull items from across the room (that grab ended the
            // locomotion gesture and snatched whatever the arc swept over)
            if (best == null && !spectator && Vector3.Dot(Source.PalmNormal, Vector3.up) < 0.6f) { best = FindDistant(pinch, wasFar); _farHover = best != null; }
            if (best != _hover)
            {
                if (_hover != null) _hover.SetHighlight(false, Palette.Yellow);
                _hover = best;
                if (_hover != null) { _hover.SetHighlight(true, Palette.Yellow); AudioBank.I?.Play("ui_hover", pinch, 0.08f, 0.1f, AudioBus.UI); }
            }
        }

        bool _farHover; float _pullT = -1f, _pullDur = 0.28f, _pullArc = 0.15f; Vector3 _pullFrom;
        /// <summary>Near-grab probe: sphere radius around the pinch point and max collider-surface distance accepted.</summary>
        public const float HoverRadius = 0.15f, HoverAccept = 0.15f;

        /// <summary>
        /// "Magic pull" (V5 playability fix from the Quest recording: players had to crouch to the floor for small items).
        /// Point an open hand at an item up to 3.5 m away along the shoulder->hand ray; it highlights; pinch and it flies
        /// gently into your hand (impacts ignored during the pull, so it is never a noise exploit). Same Round.Grab rules.
        /// </summary>
        PropBody FindDistant(Vector3 pinch, bool wasFar)
        {
            var cam = Camera.main; if (cam == null) return null;
            if (UiPointer.IsPointingAtUi(Source)) return null; // the ray is on a menu button: that pinch belongs to the UI
            if (Source.Pinch > 0.6f) return wasFar ? _hover : null; // keep the current target while closing the pinch
            var shoulder = cam.transform.position + Vector3.down * 0.25f;
            var dir = pinch - shoulder; if (dir.sqrMagnitude < 0.01f) return null; dir.Normalize();
            // Precise ray first (lets you pick an item behind/next to others), then a forgiving 9 cm sphere.
            if (Physics.Raycast(pinch, dir, out var exact, 3.5f, Layers.PropsMask, QueryTriggerInteraction.Ignore))
            {
                var p = exact.collider.GetComponentInParent<PropBody>();
                if (Pullable(p)) return p;
            }
            if (Physics.SphereCast(pinch, 0.09f, dir, out var hit, 3.5f, Layers.PropsMask, QueryTriggerInteraction.Ignore))
            {
                var p = hit.collider.GetComponentInParent<PropBody>();
                if (Pullable(p)) return p;
            }
            return null;
        }

        // ------------------------------------------------------------------ rules routing (offline/host: direct; online client: intents)
        int HandIndex => Source != null && Source.Hand == Handedness.Left ? 1 : 2;
        float _predictedAt = -1f;
        void RulesRelease(PropBody p, Vector3 v, bool thrown)
        {
            if (GameServices.IsReplica) GameServices.Net?.RequestRelease(p.Spec.Id, v, thrown, HandIndex);
            else GameServices.Round?.Release(Actor, p.Spec.Id, v.magnitude, thrown);
        }
        void RulesActivity() { if (GameServices.IsReplica) GameServices.Net?.ReportActivity(); else GameServices.Round?.ReportActivity(Actor); }

        static bool Pullable(PropBody p) => p != null && p.Spec.Grabbable && !p.IsHeld && !(p.Body != null && p.Body.isKinematic);

        /// <summary>QA: every grabbable candidate around the pinch point at the last grab, with its collider-surface distance.</summary>
        public string LastGrabCandidates = "";

        float FingerDistance(PropBody p)
        {
            if (p == null || p.Col == null) return float.MaxValue;
            var pin = Source.PinchPoint; var tip = Source.IndexTip;
            return Mathf.Min(Vector3.Distance(p.Col.ClosestPoint(pin), pin), Vector3.Distance(p.Col.ClosestPoint(tip), tip));
        }

        void LogLatch(float p, float g)
        {
            var pin = Source.PinchPoint; PropBody near = null; float nd = 9f;
            foreach (var c in Physics.OverlapSphere(pin, 0.4f, Layers.PropsMask, QueryTriggerInteraction.Ignore)) { var q = c.GetComponentInParent<PropBody>(); if (q == null) continue; float d = Vector3.Distance(c.ClosestPoint(pin), pin); if (d < nd) { nd = d; near = q; } }
            var ovr = Source as OvrHandSource;
            GrabLog.Add($"{Source.Hand} pinch-start p={p:0.00} g={g:0.00} conf={Source.HighConfidence} skel={(ovr != null ? ovr.SkeletonName : Source.GetType().Name)} palmUp={Vector3.Dot(Source.PalmNormal, Vector3.up):0.00} speed={HandSpeed():0.00} hover={(_hover != null ? _hover.Spec.Kind + (_farHover ? "(far)" : "") : "-")} nearest={(near != null ? near.Spec.Kind + "#" + near.Spec.Id + " " + (nd * 100f).ToString("0.0") + "cm" + (near.Spec.Grabbable ? "" : " not-grabbable") + (near.IsHeld ? " held" : "") : "none")} pinchPt-indexTip={(Vector3.Distance(pin, Source.IndexTip) * 100f):0.0}cm");
        }

        void TryGrab(PropBody p)
        {
            var round = GameServices.Round;
            GrabAttempts++;
            {
                var sb = new System.Text.StringBuilder(); var pin = Source.PinchPoint;
                foreach (var c in Physics.OverlapSphere(pin, HoverRadius, Layers.PropsMask, QueryTriggerInteraction.Ignore))
                { var q = c.GetComponentInParent<PropBody>(); if (q != null) sb.Append($"{q.Spec.Kind}#{q.Spec.Id}:{Vector3.Distance(c.ClosestPoint(pin), pin) * 1000f:0}mm "); }
                LastGrabCandidates = $"chose {p.Spec.Kind}#{p.Spec.Id}; candidates {sb}";
            }
            if (GameServices.IsReplica)
            {
                // Online client: ask the host (it applies the same rules + owns physics); hold it locally right away
                // (prediction) and let go if the host gives it to someone else.
                if (round == null || !round.IsPlayable || !round.Roster[Actor].IsActive) { Fail($"round not playable - {p.Spec.Kind}#{p.Spec.Id}", p.transform.position, true); return; }
                if (GameServices.Net != null && GameServices.Net.HolderOf(p.Spec.Id) >= 0 && GameServices.Net.HolderOf(p.Spec.Id) != Actor) { Fail("held by another sibling", p.transform.position, false); return; }
                GameServices.Net?.RequestGrab(p.Spec.Id, HandIndex); _predictedAt = Time.time;
            }
            else if (round == null || !round.IsPlayable)
            {
                // Outside of play (menus, practice warm-up) props can still be handled freely, without scoring.
                if (!(GameServices.PracticeMode || round == null)) { Fail($"round not playable ({round.Phase}) - {p.Spec.Kind}#{p.Spec.Id}", p.transform.position, true); return; }
            }
            else if (!round.Grab(Actor, p.Spec.Id)) { Fail($"rules rejected grab of {p.Spec.Kind}#{p.Spec.Id} (spectator/inactive?)", p.transform.position, false); return; }
            GrabSuccesses++; LastGrabFailure = ""; GrabLog.Add($"{Source.Hand} GRAB {p.Spec.Kind}#{p.Spec.Id} at {FingerDistance(p) * 100f:0.0}cm{(_farHover ? " (pull)" : "")}");
            Held = p; p.OnGrabbed(Actor);
            OwnBody(p, true);
            p.SetHighlight(false, Palette.Yellow);
            var hr = Source.HandRotation;
            _localGrabRot = Quaternion.Inverse(hr) * p.transform.rotation;
            _localGrabPos = Quaternion.Inverse(hr) * (p.transform.position - Source.PinchPoint);
            // V6 root cause #5 (xr_diag): this used to compare the PIVOT distance (> 0.2 m) - big/tall items (cushion, book on
            // its end, pan, basket) were then "magic pulled" so their pivot jumped into the hand. Far = surface distance only.
            float surface = p.Col != null ? Vector3.Distance(p.Col.ClosestPoint(Source.PinchPoint), Source.PinchPoint) : _localGrabPos.magnitude;
            if (!_farHover && surface > 0.03f && surface <= 0.2f && p.Col != null)
            {
                // Grabbed a few cm short (pinch closed while still arriving): the nearest surface point snaps into the fingers in
                // 0.12 s instead of the item dangling where it was (xr_diag early-pinch cases: 15-16 cm hanging offset).
                var cp = p.Col.ClosestPoint(Source.PinchPoint);
                _localGrabPos = Quaternion.Inverse(hr) * (p.transform.position - cp);
                _pullT = 0f; _pullDur = 0.12f; _pullArc = 0f; _pullFrom = p.transform.position;
                p.IgnoreImpactsUntil = Time.time + 0.3f;
            }
            else if (_farHover || surface > 0.2f)
            {
                // Pull so the item's visual centre (not its pivot) arrives between the fingers.
                var centre = p.Col != null ? p.Col.bounds.center : p.transform.position;
                _localGrabPos = Quaternion.Inverse(hr) * (p.transform.position - centre);
                _pullT = 0f; _pullDur = 0.28f; _pullArc = 0.15f; _pullFrom = p.transform.position;
                p.IgnoreImpactsUntil = Time.time + 0.8f;
                RoomFx.Burst("toon_impact", p.transform.position + Vector3.up * 0.05f, 0.1f, 0.8f);
            }
            if (!GameServices.IsReplica) { p.Body.useGravity = false; p.BoostHeldMass(true); }
            AudioBank.I?.Play("grab", p.transform.position, 0.2f, 0.1f, AudioBus.UI);
            RulesActivity();
        }

        float _lastDenyFx;
        void Fail(string why, Vector3 at, bool feedback)
        {
            LastGrabFailure = why;
            Debug.Log($"[SHHH][grab] {Source.Hand} FAILED: {why}"); GrabLog.Add($"{Source.Hand} FAILED {why}");
            if (feedback && Time.time - _lastDenyFx > 1.5f)
            {
                _lastDenyFx = Time.time;
                RoomFx.Say(at + Vector3.up * 0.2f, "Wait for GO!", Palette.Coral, 1.2f, 0.6f);
            }
        }

        /// <summary>Pinch closed with nothing grabbable in reach: log the nearest prop and why it was not taken (root-cause data).</summary>
        void LogMiss(Vector3 pinch, bool spectator)
        {
            if (spectator) { LastGrabFailure = "spectator"; return; }
            PropBody nearest = null; float nd = float.MaxValue;
            foreach (var c in Physics.OverlapSphere(pinch, 0.5f, Layers.PropsMask, QueryTriggerInteraction.Ignore))
            {
                var p = c.GetComponentInParent<PropBody>(); if (p == null) continue;
                float d = Vector3.Distance(c.ClosestPoint(pinch), pinch);
                if (d < nd) { nd = d; nearest = p; }
            }
            if (nearest == null) return; // pinching in the air (locomotion gesture, UI) - not a grab attempt
            GrabAttempts++;
            string why = !nearest.Spec.Grabbable ? "not grabbable" : nearest.IsHeld ? "held by someone else" : nd > HoverAccept ? $"too far ({nd:0.000} m > {HoverAccept} m)" : "unknown";
            LastGrabFailure = $"miss {nearest.Spec.Kind}#{nearest.Spec.Id}: {why}";
            Debug.Log($"[SHHH][grab] {Source.Hand} {LastGrabFailure}"); GrabLog.Add($"{Source.Hand} MISS {nearest.Spec.Kind}#{nearest.Spec.Id} {why}");
        }

        public void Release()
        {
            _pullT = -1f;
            var p = Held; if (p == null) return;
            Held = null;
            if (Other != null && Other.Held == p) { p.HoldCount = Mathf.Max(1, p.HoldCount - 1); return; } // other hand keeps it
            Vector3 v = Vector3.zero; int n = 0; foreach (var x in _vel) { v += x; n++; }
            v = n > 0 ? v / n : Vector3.zero;
            v = Vector3.ClampMagnitude(v, MaxThrowSpeed);
            bool thrown = v.magnitude > 1.8f;
            if (!GameServices.IsReplica)
            {
                p.Body.useGravity = true; p.Body.isKinematic = false;
                p.BoostHeldMass(false);
                p.Body.linearVelocity = v;
            }
            p.OnReleased(Actor, v, thrown);
            RulesRelease(p, v, thrown);
            _bodyIgnored[p] = Time.time + 0.6f;   // collisions with our own body come back once it has left the body
        }

        // V12: what a hand holds never collides with its own player's body capsule (owner run: a laundry bundle pushed into the washer
        // drum snagged on the XRRig CharacterController and the hand had to let go). Restored after release once clear of the body.
        readonly System.Collections.Generic.Dictionary<PropBody, float> _bodyIgnored = new System.Collections.Generic.Dictionary<PropBody, float>();
        static readonly System.Collections.Generic.List<PropBody> _restore = new System.Collections.Generic.List<PropBody>();
        Collider OwnCapsule => GameRoot.I != null && GameRoot.I.Rig != null ? GameRoot.I.Rig.GetComponent<CharacterController>() : null;
        void OwnBody(PropBody p, bool ignore)
        {
            var cc = OwnCapsule; if (cc == null || p == null || p.Col == null) return;
            Physics.IgnoreCollision(p.Col, cc, ignore);
            if (ignore) _bodyIgnored.Remove(p);
        }
        void LateUpdate()
        {
            if (_bodyIgnored.Count == 0) return;
            var cc = OwnCapsule; _restore.Clear();
            foreach (var kv in _bodyIgnored) if (kv.Key == null || (Time.time > kv.Value && !kv.Key.IsHeld && (cc == null || !kv.Key.Col.bounds.Intersects(cc.bounds)))) _restore.Add(kv.Key);
            foreach (var p in _restore) { _bodyIgnored.Remove(p); if (p != null && !p.IsHeld) OwnBody(p, false); }
        }

        // ------------------------------------------------------------------ tracking loss
        void HandleTrackingLoss()
        {
            var mechHeld = MechPart.HeldBy(this); if (mechHeld != null) mechHeld.EndGrab();
            if (_hover != null) { _hover.SetHighlight(false, Palette.Yellow); _hover = null; }
            _haveLast = false; _vel.Clear();
            if (Held == null) return;
            if (_lostSince < 0) _lostSince = Time.time;
            if (!_frozen && Time.time - _lostSince > 0.12f)
            {
                _frozen = true;
                Held.Body.linearVelocity = Vector3.zero; Held.Body.angularVelocity = Vector3.zero; Held.Body.isKinematic = true;
            }
            if (_frozen && _softDropT < 0 && Time.time - _lostSince > 6f) BeginSoftDrop();
        }

        void RecoverTracking()
        {
            _lostSince = -1f;
            if (Held != null && _frozen && _softDropT < 0)
            {
                // Resume only if the hand is back near the item and still pinching; otherwise lower it gently.
                bool near = Vector3.Distance(Source.PinchPoint, Held.transform.position) < 0.25f && WantsGrab();
                if (near) { _frozen = false; Held.Body.isKinematic = false; var hr = Source.HandRotation; _localGrabRot = Quaternion.Inverse(hr) * Held.transform.rotation; _localGrabPos = Quaternion.Inverse(hr) * (Held.transform.position - Source.PinchPoint); }
                else BeginSoftDrop();
            }
        }

        void BeginSoftDrop()
        {
            _softDropT = 0f; _softFrom = Held.transform.position;
            float y = 0.02f;
            if (Physics.Raycast(_softFrom, Vector3.down, out var hit, 3f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) y = hit.point.y + 0.01f;
            _softTo = new Vector3(_softFrom.x, y, _softFrom.z);
        }

        void StepSoftDrop()
        {
            if (Held == null) { _softDropT = -1f; return; }
            _softDropT += Time.fixedDeltaTime / 0.7f;
            Held.Body.MovePosition(Vector3.Lerp(_softFrom, _softTo, Mathf.SmoothStep(0, 1, _softDropT)));
            if (_softDropT < 1f) return;
            var p = Held; Held = null; _frozen = false; _softDropT = -1f;
            p.IgnoreImpactsUntil = Time.time + 0.6f;
            if (!GameServices.IsReplica) { p.Body.isKinematic = false; p.Body.useGravity = true; p.Body.linearVelocity = Vector3.zero; p.BoostHeldMass(false); }
            p.OnReleased(Actor, Vector3.zero, false);
            RulesRelease(p, Vector3.zero, false);
        }

        public void ForceDropGently() { if (Held != null) { _frozen = true; Held.Body.isKinematic = true; BeginSoftDrop(); } }
    }
}
