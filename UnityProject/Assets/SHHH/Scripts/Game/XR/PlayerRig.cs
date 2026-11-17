using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// Room-scale player rig with controller-free comfort locomotion:
    ///  • V13 finger gun (owner 2026-11-15: "make the teleport easier, using finger gun gesture and same for walking"): an EMPTY hand
    ///    makes a finger gun (index out, other fingers curled) with the THUMB UP for 0.2 s = cocked. Then:
    ///    - hop hand (RIGHT by default): an arc comes out of the index finger; drop the thumb = hop there (short fade). Lift the thumb
    ///      to aim again. The arc stops at the first thing it hits - never through a wall - and only lands on the house floor with
    ///      head room and nobody standing there.
    ///    - walk hand (LEFT by default): point where to go; hold the thumb down = walk that way; thumb up / open hand = stop.
    ///    A button poke (index out, thumb tucked from the start) never cocks the gun, and the gun pose never grabs anything.
    ///    Wrist button cycles: walk + hop / walk only / hop only.
    ///  • Snap turn buttons on the wrist; recenter; seated height offset.
    /// Never moves the camera without player intent; stops instantly on hand-tracking loss.
    /// </summary>
    public sealed class PlayerRig : MonoBehaviour
    {
        public Transform Head;
        public Transform TrackingSpace;
        public HandSource Left, Right;
        public HandInteractor LeftHand, RightHand;
        public float MoveSpeed = 1.3f;
        public float SnapAngle = 30f;
        public bool TeleportMode;          // hop only (both hands)
        public bool HopRightHand = true;   // V12: right hand hops, left hand walks
        public const float HopRange = 7f;
        /// <summary>QA: hops done / last refusal.</summary>
        public int Hops; public string LastHopRefusal = "";
        public bool HopAimValid => _tpValid;
        public Vector3 HopAimTarget => _tpTarget;

        /// <summary>Wrist / settings button: walk + hop -> walk only -> hop only -> walk + hop. Returns the new style's name.</summary>
        public static string CycleLocomotion(Settings s)
        {
            if (!s.TeleportMode && s.HopRightHand) { s.HopRightHand = false; }
            else if (!s.TeleportMode) { s.TeleportMode = true; }
            else { s.TeleportMode = false; s.HopRightHand = true; }
            GrabLog.Add("settings: locomotion switched to " + LocomotionName(s));
            return LocomotionName(s);
        }
        public static string LocomotionName(Settings s) => s.TeleportMode ? "Hop only" : s.HopRightHand ? "Walk + hop (right hand)" : "Walk only";
        public void ApplyLocomotion(Settings s) { TeleportMode = s.TeleportMode; HopRightHand = s.HopRightHand; }
        public bool LocomotionEnabled = true;
        public float SeatedOffset;
        CharacterController _cc;
        Transform _fade; Material _fadeMat; float _fadeA, _fadeTarget;
        Transform _marker; LineRenderer _arc;

        sealed class Gesture { public float armT; public bool armed, moving, hop, fired; public float speedK; }
        readonly Gesture[] _g = { new Gesture(), new Gesture() };

        public void Init()
        {
            _cc = gameObject.GetComponent<CharacterController>(); if (_cc == null) _cc = gameObject.AddComponent<CharacterController>();
            _cc.radius = 0.2f; _cc.height = 1.5f; _cc.center = new Vector3(0, 0.75f, 0); _cc.stepOffset = 0.2f; _cc.slopeLimit = 30f; _cc.skinWidth = 0.02f;
            gameObject.layer = Layers.Agents;
            // Fade sphere (for teleports, room changes, spectator moves) — only rendered while fading.
            // V12 (Quest run: a 0.4 m sphere left a moving black wedge over a grey void while loading): a big quad 12 cm in front of
            // the eyes covers both eyes' whole view
            var f = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(f.GetComponent<Collider>());
            f.name = "fade"; f.transform.SetParent(Head, false); f.transform.localPosition = new Vector3(0f, 0f, 0.12f); f.transform.localScale = Vector3.one * 2.5f;
            _fadeMat = new Material(Mats.Glow(Color.black)); _fadeMat.renderQueue = 4000;
            if (_fadeMat.HasProperty("_ZTest")) _fadeMat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            f.GetComponent<Renderer>().sharedMaterial = _fadeMat; _fade = f.transform; f.SetActive(false);
            _marker = Prim.Shape(PrimitiveType.Cylinder, null, "teleportMarker", Vector3.zero, new Vector3(0.4f, 0.005f, 0.4f), Mats.Unlit(Palette.Teal)).transform;
            _marker.gameObject.SetActive(false); DontDestroyOnLoad(_marker.gameObject);
            var arcGo = new GameObject("teleportArc"); arcGo.transform.SetParent(transform, false);
            _arc = arcGo.AddComponent<LineRenderer>(); _arc.positionCount = 30; _arc.enabled = false;
            BuildHopVisuals();
            gameObject.AddComponent<MoveHints>().Rig = this;
            gameObject.AddComponent<InteractionHints>().Rig = this;   // V13: highlight / next-step arrow / ghost hand for every interaction
        }

        public void FadeTo(float a) { _fadeTarget = a; if (a > 0) _fade.gameObject.SetActive(true); if (a < 0.5f) _loading = false; }

        /// <summary>V12: "SHHH! loading the house..." on the black while a scene loads (drawn after the fade).</summary>
        public bool Loading { get => _loading; set { _loading = value; if (value && _loadCard == null) BuildLoadCard(); } }
        bool _loading; Transform _loadCard; TMPro.TextMeshPro _loadDots;
        void BuildLoadCard()
        {
            _loadCard = new GameObject("loadingCard").transform; _loadCard.SetParent(Head, false); _loadCard.localPosition = new Vector3(0f, -0.05f, 0.9f);
            var title = WorldText.Make(_loadCard, "title", "SHHH!", new Vector3(0f, 0.06f, 0f), 1.6f, Palette.Yellow, TMPro.TextAlignmentOptions.Center, 0.8f);
            _loadDots = WorldText.Make(_loadCard, "line", "loading the house", new Vector3(0f, -0.06f, 0f), 0.55f, Palette.Cream, TMPro.TextAlignmentOptions.Center, 0.8f);
            foreach (var t in new[] { title, _loadDots }) { var m = t.fontMaterial; m.renderQueue = 4001; t.fontMaterial = m; }
            _loadCard.gameObject.SetActive(false);
        }
        public bool FadeDone => Mathf.Abs(_fadeA - _fadeTarget) < 0.02f;
        /// <summary>Current black-fade opacity (0 = clear, 1 = black).</summary>
        public float FadeAlpha => _fadeA;

        void Update()
        {
            _fadeA = Mathf.MoveTowards(_fadeA, _fadeTarget, Time.deltaTime * _fadeSpeed);
            UpdateHeadInWall();
            if (_fade != null)
            {
                float a = Mathf.Max(_fadeA, _wallA);
                if (a > 0.001f && !_fade.gameObject.activeSelf) _fade.gameObject.SetActive(true);
                _fadeMat.SetColor("_BaseColor", new Color(0, 0, 0, a));
                if (a <= 0.001f && _fadeTarget <= 0f) _fade.gameObject.SetActive(false);
            }
            if (_loadCard != null)
            {
                bool show = _loading && _fadeA > 0.9f;
                if (_loadCard.gameObject.activeSelf != show) _loadCard.gameObject.SetActive(show);
                if (show && _loadDots != null) _loadDots.text = "loading the house" + new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);
            }
            if (TrackingSpace != null) TrackingSpace.localPosition = new Vector3(0, SeatedOffset, 0);
            FollowHeadWithCollider();
            if (!LocomotionEnabled)
            {
                foreach (var gg in _g) { gg.moving = gg.armed = gg.hop = gg.fired = false; gg.armT = 0f; gg.speedK = 0f; }
                WalkingHand = AimingHand = WalkReadyHand = -1; HideTeleport(); return;
            }
            Vector3 move = Vector3.zero;
            for (int i = 0; i < 2; i++)
            {
                var src = i == 0 ? Left : Right; var hi = i == 0 ? LeftHand : RightHand;
                move += UpdateGesture(_g[i], src, hi);
            }
            if (move.sqrMagnitude > 1e-6f)
            {
                _cc.Move(move * Time.deltaTime);
                if (GameServices.IsReplica) GameServices.Net?.ReportActivity(); else GameServices.Round?.ReportActivity(GameServices.LocalSlot);
            }
            if (!_cc.isGrounded) _cc.Move(Vector3.down * 2f * Time.deltaTime);
        }

        // ---- V13 (owner, Quest c21: "the view goes inside walls and door frames when you stand close to them"). A game cannot stop a real
        // head, so - as VR games do - the view darkens as the head goes into a wall or furniture (fully dark ~3 cm past the surface), with
        // a "step back" hint; it clears as soon as the head is out. Nothing is moved and no collision is removed.
        public const float HeadRadius = 0.1f;
        float _wallA, _wallHintAt; SphereCollider _headProbe; readonly Collider[] _headHits = new Collider[12];
        /// <summary>0 = clear, ~1 = head inside a wall / furniture (QA).</summary>
        public float WallFade => _wallA;
        public float HeadDepth { get; private set; }

        void UpdateHeadInWall()
        {
            if (Head == null) return;
            if (_headProbe == null)
            {
                var g = new GameObject("headProbe"); g.layer = 2; DontDestroyOnLoad(g);   // Ignore Raycast: touches nothing, only measured
                _headProbe = g.AddComponent<SphereCollider>(); _headProbe.radius = HeadRadius; _headProbe.isTrigger = true;
            }
            var p = Head.position; _headProbe.transform.position = p; float depth = 0f;
            int n = Physics.OverlapSphereNonAlloc(p, HeadRadius, _headHits, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _headHits[i]; if (c == null || c.isTrigger || c.GetComponentInParent<PlayerRig>() != null) continue;
                if (Physics.ComputePenetration(_headProbe, p, Quaternion.identity, c, c.transform.position, c.transform.rotation, out _, out float d)) depth = Mathf.Max(depth, d);
                else if (c is MeshCollider mc && !mc.convex) depth = Mathf.Max(depth, HeadRadius * 0.6f);   // a non-convex mesh: overlap only
            }
            HeadDepth = depth;
            float target = Mathf.InverseLerp(0.045f, 0.13f, depth) * 0.97f;
            _wallA = Mathf.MoveTowards(_wallA, target, Time.deltaTime * 5f);
            if (_wallA > 0.6f && Time.time > _wallHintAt) { _wallHintAt = Time.time + 8f; TaskHud.Caption("Step back - your head is inside the wall", 2.5f); }
        }

        void FollowHeadWithCollider()
        {
            if (_cc == null || Head == null) return;
            var local = transform.InverseTransformPoint(Head.position);
            _cc.center = new Vector3(local.x, Mathf.Max(0.5f, local.y * 0.5f), local.z);
            _cc.height = Mathf.Max(1f, local.y);
        }

        Vector3 UpdateGesture(Gesture g, HandSource src, HandInteractor hi)
        {
            bool tracked = src != null && src.Tracked && src.HighConfidence && !src.SystemGesture;
            bool gun = tracked && src.GunPose;
            bool free = hi == null || (!hi.IsGrabbing && MechPart.HeldBy(hi) == null);
            int hIdx = src != null && src.Hand == Handedness.Right ? 1 : 0;
            if (!gun || !free)
            {
                if (WalkingHand == hIdx) WalkingHand = -1; if (AimingHand == hIdx) AimingHand = -1; if (WalkReadyHand == hIdx) WalkReadyHand = -1;
                if (g.hop && g.armed) CancelHop();
                g.armT = 0f; g.armed = g.moving = g.hop = g.fired = false; g.speedK = 0f;
                return Vector3.zero;
            }
            if (!g.armed)
            {
                // cock the gun: thumb UP for 0.2 s (a poke at a button keeps the thumb tucked, so it never arms)
                if (!src.ThumbDown) g.armT += Time.deltaTime; else g.armT = 0f;
                if (g.armT < 0.2f) return Vector3.zero;
                g.armed = true; g.fired = false; g.hop = TeleportMode || (HopRightHand && src.Hand == Handedness.Right);
                if (g.hop) AimingHand = hIdx; else WalkReadyHand = hIdx;
                AudioBank.I?.Play("ui_hover", src.IndexTip, 0.15f, 0f, AudioBus.UI);
            }
            if (g.hop)
            {
                if (g.fired) { if (src.ThumbDown) { HideTeleport(); return Vector3.zero; } g.fired = false; }   // lift the thumb to aim again
                AimTeleport(src);
                if (src.ThumbDown)
                {
                    g.fired = true;
                    if (_tpValid) DoTeleport(); else { HideTeleport(); AudioBank.I?.Play("ui_click", src.IndexTip, 0.2f, 0f, AudioBus.UI); }
                }
                return Vector3.zero;
            }
            // walk: hold the thumb down to walk where the index finger points (flattened); thumb up = stop
            if (!src.ThumbDown)
            {
                if (WalkingHand == hIdx) WalkingHand = -1; WalkReadyHand = hIdx;
                g.moving = false; g.speedK = 0f; return Vector3.zero;
            }
            if (!g.moving) { g.moving = true; WalkingHand = hIdx; WalkReadyHand = -1; Walks++; }
            var d = src.FingerRay.direction; d.y = 0f;
            if (d.magnitude < 0.35f) return Vector3.zero;          // pointing straight down / up: no direction
            g.speedK = Mathf.MoveTowards(g.speedK, 1f, Time.deltaTime * 4f);
            return d.normalized * (MoveSpeed * g.speedK);
        }

        Vector3 _tpTarget; bool _tpValid; Material _arcOk, _arcBad; NavMeshPath _hopPath;
        Transform _badMarker; Vector3 _hopEnd; bool _hopHit;
        /// <summary>Where the arc ends now (landing spot or the thing it hit) and whether it hit anything.</summary>
        public Vector3 HopAimPoint => _hopEnd;
        public bool HopAimHit => _hopHit;
        /// <summary>Hand (0 left, 1 right) whose gesture is active: walking or aiming a hop; -1 none.</summary>
        public int WalkingHand { get; private set; } = -1;
        public int AimingHand { get; private set; } = -1;
        /// <summary>V13: hand holding a cocked finger gun for walking, thumb still up (not walking yet); -1 none.</summary>
        public int WalkReadyHand { get; private set; } = -1;
        public int Walks;

        /// <summary>
        /// V12 (owner: "please prepare a very nice teleportation shader for the ray and the area"): a glowing tube arc whose dashes flow
        /// toward the landing spot (teal = can land, coral = cannot), and a landing spot that is a soft disc with a crisp rim, a ripple
        /// travelling outward and an arrow showing which way I will face; a crossed-out coral spot where I cannot land.
        /// </summary>
        void BuildHopVisuals()
        {
            var arcSh = Resources.Load<Shader>("Shaders/SHHH_TeleportArc"); var tgtSh = Resources.Load<Shader>("Shaders/SHHH_TeleportTarget");
            Material Arc(Color c) { if (arcSh == null) return Mats.Unlit(c); var m = new Material(arcSh); m.SetColor("_Color", c); return m; }
            _arcOk = Arc(Palette.Teal); _arcBad = Arc(Palette.Coral);
            _arc.sharedMaterial = _arcOk; _arc.textureMode = LineTextureMode.Stretch; _arc.numCapVertices = 4; _arc.alignment = LineAlignment.View;
            _arc.widthCurve = new AnimationCurve(new Keyframe(0f, 0.012f), new Keyframe(0.8f, 0.03f), new Keyframe(1f, 0.035f)); _arc.widthMultiplier = 1f;
            _arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _arc.receiveShadows = false;
            // landing spot (the old marker cylinder becomes an invisible root)
            foreach (var r in _marker.GetComponents<Renderer>()) r.enabled = false;
            foreach (var c in _marker.GetComponents<Collider>()) Destroy(c);
            _marker.localScale = Vector3.one;
            Transform Spot(string n, Transform parent, bool bad)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = n;
                q.transform.SetParent(parent, false); q.transform.localPosition = new Vector3(0f, 0.01f, 0f); q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = Vector3.one * 0.6f;
                Material m = tgtSh != null ? new Material(tgtSh) : Mats.Unlit(bad ? Palette.Coral : Palette.Teal);
                if (tgtSh != null) { m.SetColor("_Color", bad ? Palette.Coral : Palette.Teal); m.SetFloat("_Bad", bad ? 1f : 0f); }
                var rr = q.GetComponent<Renderer>(); rr.sharedMaterial = m; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return q.transform;
            }
            Spot("spot", _marker, false);
            // facing arrow: a flat chevron just past the rim
            var arrow = new GameObject("arrow"); arrow.transform.SetParent(_marker, false); arrow.transform.localPosition = new Vector3(0f, 0.012f, 0.36f);
            var mf = arrow.AddComponent<MeshFilter>(); var mesh = new Mesh { name = "hop_arrow" };
            mesh.vertices = new[] { new Vector3(-0.09f, 0f, -0.04f), new Vector3(0f, 0f, 0.07f), new Vector3(0.09f, 0f, -0.04f), new Vector3(0f, 0f, 0.0f) };
            mesh.triangles = new[] { 0, 1, 3, 3, 1, 2 }; mesh.RecalculateNormals(); mf.sharedMesh = mesh;
            var amr = arrow.AddComponent<MeshRenderer>(); amr.sharedMaterial = Mats.Unlit(Palette.Cream); amr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bad = new GameObject("hopNoSpot").transform; DontDestroyOnLoad(bad.gameObject); Spot("spot", bad, true).localScale = Vector3.one * 0.4f;
            _badMarker = bad; bad.gameObject.SetActive(false);
        }
        /// <summary>
        /// V12 hop arc. The old arc ignored anything that was not floor-facing and flew on THROUGH walls (it could land in another room).
        /// Now it stops at the first collider it meets; the landing must be walkable floor (NavMesh within 15 cm of the hit, up-facing),
        /// within <see cref="HopRange"/>, with a clear 1.6 m tall space (no furniture over it) and nobody (bot / player) standing there.
        /// </summary>
        void AimTeleport(HandSource src)
        {
            var ray = src.FingerRay;
            Vector3 p = ray.origin, v = ray.direction.normalized * 6.5f; _tpValid = false; LastHopRefusal = "no floor in reach"; _hopHit = false; _hopEnd = p;
            if (_badMarker != null) _badMarker.gameObject.SetActive(false);
            _arc.enabled = true; int n = _arc.positionCount, used = n;
            for (int i = 0; i < n; i++)
            {
                _arc.SetPosition(i, p);
                Vector3 np = p + v * 0.04f; v += Physics.gravity * 0.04f;
                if (Physics.Linecast(p, np, out var hit, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
                {
                    _arc.SetPosition(i, hit.point); used = i + 1; _hopEnd = hit.point; _hopHit = true;
                    _tpValid = HopSpot(hit, out _tpTarget, out var why); LastHopRefusal = _tpValid ? "" : why;
                    if (_tpValid) _hopEnd = _tpTarget;
                    else if (_badMarker != null) { _badMarker.gameObject.SetActive(true); _badMarker.SetPositionAndRotation(hit.point + hit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, hit.normal)); }
                    break;
                }
                p = np;
            }
            _arc.positionCount = n; for (int i = used; i < n; i++) _arc.SetPosition(i, _arc.GetPosition(used - 1));
            _arc.sharedMaterial = _tpValid ? _arcOk : _arcBad;
            _marker.gameObject.SetActive(_tpValid);
            if (_tpValid)
            {
                _marker.position = _tpTarget + Vector3.up * 0.01f;
                var f = Head.forward; f.y = 0f; if (f.sqrMagnitude > 1e-4f) _marker.rotation = Quaternion.LookRotation(f);
            }
        }

        /// <summary>Is this arc hit a place I may hop to? (shared with QA)</summary>
        public bool HopSpot(RaycastHit hit, out Vector3 target, out string why)
        {
            target = hit.point; why = "";
            if (hit.normal.y < 0.7f) { why = "not a floor (wall / side)"; return false; }
            if (!NavMesh.SamplePosition(hit.point, out var nh, 0.3f, NavMesh.AllAreas) || Mathf.Abs(nh.position.y - hit.point.y) > 0.15f) { why = "not walkable floor (table top, bed...)"; return false; }
            target = nh.position;
            // furniture tops carry their own NavMesh islands: the landing must connect to where I stand by a walkable path
            var feet = new Vector3(Head.position.x, transform.position.y, Head.position.z);
            if (NavMesh.SamplePosition(feet, out var fh, 1.0f, NavMesh.AllAreas))
            {
                var path = _hopPath ??= new NavMeshPath();
                if (!NavMesh.CalculatePath(fh.position, target, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) { why = "not walkable floor (table top, bed...)"; return false; }
            }
            var flat = target - Head.position; flat.y = 0f; if (flat.magnitude > HopRange) { why = "too far"; return false; }
            if (Physics.CheckCapsule(target + Vector3.up * 0.35f, target + Vector3.up * 1.6f, 0.18f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) { why = "no head room"; return false; }
            if (PersonalSpace.Nearest(target) < 0.45f && flat.magnitude > 0.5f) { why = "someone is standing there"; return false; }
            foreach (var c in Physics.OverlapCapsule(target + Vector3.up * 0.3f, target + Vector3.up * 1.2f, 0.3f, 1 << Layers.Agents, QueryTriggerInteraction.Ignore))
                if (c.GetComponentInParent<PlayerRig>() == null) { why = "someone is standing there"; return false; }
            return true;
        }

        void HideTeleport() { if (_arc != null) _arc.enabled = false; if (_marker != null) _marker.gameObject.SetActive(false); if (_badMarker != null) _badMarker.gameObject.SetActive(false); }
        void CancelHop() { HideTeleport(); _tpValid = false; }

        void DoTeleport()
        {
            HideTeleport();
            if (!_tpValid) return;
            _tpValid = false;
            StartCoroutine(FadeMove(_tpTarget));
        }

        System.Collections.IEnumerator FadeMove(Vector3 target)
        {
            AudioBank.I?.Play2D("ui_click", 0.35f);
            float k0 = _fadeSpeed; _fadeSpeed = 8f;   // a quick blink (0.12 s each way): comfortable, not a long black-out
            FadeTo(1f); while (!FadeDone) yield return null;
            PlaceHeadAt(target); Hops++;
            if (GameServices.IsReplica) GameServices.Net?.ReportActivity(); else GameServices.Round?.ReportActivity(GameServices.LocalSlot);
            FadeTo(0f); while (!FadeDone) yield return null;
            _fadeSpeed = k0;
        }
        float _fadeSpeed = 3f;

        /// <summary>Moves the rig so the player's head is above <paramref name="floorPoint"/> (keeps physical orientation).</summary>
        public void PlaceHeadAt(Vector3 floorPoint)
        {
            if (_cc) _cc.enabled = false;
            Vector3 headFlat = Head.position; headFlat.y = transform.position.y;
            transform.position += new Vector3(floorPoint.x - headFlat.x, floorPoint.y - transform.position.y, floorPoint.z - headFlat.z);
            if (_cc) _cc.enabled = true;
        }

        /// <summary>Places the rig at a spawn point facing <paramref name="facing"/> — only during fades/loads.</summary>
        public void Spawn(Vector3 floorPoint, float yaw)
        {
            float headYaw = Head.eulerAngles.y - transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, yaw - headYaw, 0);
            PlaceHeadAt(floorPoint);
            Playspace.MarkStart(this);
        }

        public void SnapTurn(int dir)
        {
            if (_cc) _cc.enabled = false;
            transform.RotateAround(new Vector3(Head.position.x, transform.position.y, Head.position.z), Vector3.up, dir * SnapAngle);
            if (_cc) _cc.enabled = true;
            AudioBank.I?.Play2D("ui_click", 0.3f);
        }

        public void Recenter()
        {
            try { if (OVRManager.display != null) OVRManager.display.RecenterPose(); } catch { }
        }
    }
}
