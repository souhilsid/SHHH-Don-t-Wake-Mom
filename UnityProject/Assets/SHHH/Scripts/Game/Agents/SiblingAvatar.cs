using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Visible sibling body (bots and remote humans). Original procedural kid in seat-coloured pyjamas
    /// (KidModel), animated procedurally. The nameplate states
    /// HUMAN or BOT truthfully.
    /// </summary>
    public sealed class SiblingAvatar : MonoBehaviour
    {
        public int Slot;
        public bool IsBot;
        public Transform CarrySocket { get; private set; }
        public Transform Head { get; private set; }
        Animator _anim;
        KidModel _kid;
        TextMeshPro _plate;
        Transform _plateRoot;
        float _speed;
        public Transform ReachTarget;
        float _reachWeight;
        string _status = "";
        float _statusUntil;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int CarryId = Animator.StringToHash("Carry");

        public void Build(int slot, string displayName, bool isBot, int cosmetic)
        {
            Slot = slot; IsBot = isBot;
            var color = Palette.Siblings[((cosmetic % 4) + 4) % 4];
            // V6: Astra's original sibling cast (Pip, Nell, Rory, Tess: faces, hair, pyjamas, rounded hands) on the light
            // reduced meshes, animated by V3 locomotion clips. Fallback: the procedural V5 kid.
            _standInArgs = (displayName, cosmetic);
            // V12 (owner, Quest 2026-11-12: "from the beginning they should have Genies avatars, not fallback characters"): with the Genies
            // SDK present a kid shows a soft silhouette in its colour until its Genies body is ready; the old stand-in body is built only
            // if the Genies load fails or takes longer than 25 s.
            if (AvatarService.Backend != null) BuildPlaceholder(color);
            else BuildStandIn();
            CarrySocket = new GameObject("carry").transform;
            CarrySocket.SetParent(transform, false); CarrySocket.localPosition = new Vector3(0, 0.82f, 0.32f);

            _plateRoot = new GameObject("nameplate").transform; _plateRoot.SetParent(transform, false); _plateRoot.localPosition = new Vector3(0, 1.62f, 0);
            Prim.Box(_plateRoot, "bg", new Vector3(0, 0, 0.01f), new Vector3(0.46f, 0.11f, 0.01f), Mats.Unlit(isBot ? Palette.Purple : Palette.Navy), false);
            _plate = WorldText.Make(_plateRoot, "label", "", Vector3.zero, 0.42f, Color.white, TextAlignmentOptions.Center, 0.6f);
            SetName(displayName, isBot);
            // V8: the real body is a Genies avatar (player's saved look / seat preset / remote player's look). The sibling body
            // built above stays only until it has loaded, or permanently if the Genies SDK is unavailable (status logged).
            if (AvatarService.Backend != null)
            {
                SetGeniesLook(AvatarService.DefinitionFor(slot, isBot));
                if (!_subscribed) { _subscribed = true; AvatarService.SeatLooksChanged += OnSeatLooksChanged; }
            }
        }

        // ------------------------------------------------------------------ Genies body (V8)
        AvatarHandle _genies; int _geniesReq; GameObject _placeholder; Material _phMat; Color _phColor; float _phSince;
        (string name, int cosmetic) _standInArgs; bool _standInBuilt;
        public bool ShowsPlaceholder => _placeholder != null && _placeholder.activeSelf;

        /// <summary>The old sibling body (Astra cast rig, else the procedural kid): offline / Genies-failed fallback only.</summary>
        void BuildStandIn()
        {
            if (_standInBuilt) return; _standInBuilt = true;
            var color = Palette.Siblings[((_standInArgs.cosmetic % 4) + 4) % 4];
            var cat = CharacterCatalog.I;
            string key = CharacterKey(Slot, _standInArgs.name);
            var entry = cat != null ? cat.Get(key) : null;
            if (entry != null) { _rig = AstraRig.Create(transform, entry, 1.3f); Head = _rig != null ? _rig.Head : null; }
            if (_rig != null && Detached)
            {
                // decision 02: floating head / torso / hands / shoes over an internal humanoid-compatible rig
                LastDetach = DetachedLimbs.Apply(_rig.Model);
                Debug.Log($"[SHHH] sibling {key}: detached pieces - {LastDetach}");
            }
            if (_rig == null)
            {
                // Original stylised kid in pyjamas (family-friendly, child proportions, ~1.3 m tall).
                _kid = new GameObject("kidRoot").AddComponent<KidModel>();
                _kid.transform.SetParent(transform, false);
                _kid.Build(Slot, _standInArgs.cosmetic, color);
                Head = _kid.Head;
            }
            // wardrobe (decoded from the replicated cosmetic int; colour bits only = default look)
            if (_rig != null) Outfit = OutfitVisuals.Apply(_rig.Model, Head, SHHH.Core.Outfit.Decode(_standInArgs.cosmetic), 1f);
            if (_placeholder != null) { Destroy(_placeholder); _placeholder = null; }
        }

        /// <summary>A soft, faintly glowing kid silhouette (body + head) in the seat colour while the Genies body loads.</summary>
        void BuildPlaceholder(Color color)
        {
            _placeholder = new GameObject("loadingSilhouette"); _placeholder.transform.SetParent(transform, false);
            var src = Resources.Load<Material>("UI_V8/Mats/UiTransparent");
            _phMat = src != null ? new Material(src) : Mats.Unlit(color); _phColor = Color.Lerp(color, Color.white, 0.35f);
            var body = Prim.Shape(PrimitiveType.Capsule, _placeholder.transform, "body", new Vector3(0, 0.52f, 0), new Vector3(0.3f, 0.42f, 0.22f), _phMat);
            var head = Prim.Shape(PrimitiveType.Sphere, _placeholder.transform, "head", new Vector3(0, 1.08f, 0), Vector3.one * 0.24f, _phMat);
            foreach (var r in _placeholder.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Head = head.transform; _phSince = Time.time;
        }
        public bool HasGeniesBody => _genies != null;
        /// <summary>QA: the old stand-in body (Astra rig / procedural kid) is what this seat shows right now.</summary>
        public bool ShowsStandIn => _genies == null && ((_rig != null && _rig.gameObject.activeInHierarchy) || (_kid != null && _kid.gameObject.activeInHierarchy));
        public Animator GeniesAnimator => _genies != null ? _anim : null;
        public Transform GeniesRoot => _genies != null && _genies.Root != null ? _genies.Root.transform : null;
        /// <summary>V12: set when head + hands tracking drives this body (TrackedGeniesBody does its IK; the reach / carry IK stands down).</summary>
        public TrackedGeniesBody Tracked;
        /// <summary>V12: raised each time a Genies body finished loading (a new look = a new body to attach tracking to).</summary>
        public event System.Action<SiblingAvatar> GeniesLoaded;
        /// <summary>V12: the local player's own body - Genies only (no stand-in, no nameplate).</summary>
        public bool IsSelf { get; private set; }

        public void BuildSelf(int slot, string displayName, int cosmetic)
        {
            Slot = slot; IsBot = false; IsSelf = true;
            CarrySocket = new GameObject("carry").transform; CarrySocket.SetParent(transform, false); CarrySocket.localPosition = new Vector3(0, 0.82f, 0.32f);
            if (AvatarService.Backend == null) { Debug.LogWarning("[SHHH][body] no Genies backend - my own body is not shown (gloves only)"); return; }
            SetGeniesLook(AvatarService.DefinitionFor(slot, false));
            if (!_subscribed) { _subscribed = true; AvatarService.SeatLooksChanged += OnSeatLooksChanged; }
        }

        public void SetPlateHeight(float y) { if (_plateRoot != null) _plateRoot.localPosition = new Vector3(0, y, 0); }
        public string GeniesDefinition => _genies?.Definition;
        /// <summary>The definition this seat last asked the backend to wear (the SDK re-serializes, so the loaded body's own JSON can differ textually).</summary>
        public string RequestedDefinition => _genies != null ? _wantDef : null;
        /// <summary>Kid scale for Genies bodies (≈1.54 m adults → ≈1.32 m siblings).</summary>
        public const float GeniesScale = 0.86f;

        /// <summary>Show this seat as a Genies avatar: a saved definition, or null for the seat's starter preset.</summary>
        public void SetGeniesLook(string definition)
        {
            if (AvatarService.Backend == null) return;
            // the same look again (loading or worn): nothing to do - no second Genies build
            if (_lookRequested && definition == _wantDef && !_lookFailed) return;
            _lookRequested = true; _lookFailed = false;
            _wantDef = definition;
            int req = ++_geniesReq;
            AvatarService.Backend.Load(definition, Slot, transform, h =>
            {
                if (this == null || req != _geniesReq) { h?.Release?.Invoke(); return; }
                if (h == null || h.Root == null) { _lookFailed = true; if (_genies == null) BuildStandIn(); return; }   // keep the current body; no Genies at all -> the old stand-in
                if (_placeholder != null) { Destroy(_placeholder); _placeholder = null; }
                _genies?.Release?.Invoke();
                _genies = h;
                h.Root.transform.SetParent(transform, false);
                h.Root.transform.localPosition = Vector3.zero; h.Root.transform.localRotation = Quaternion.identity; h.Root.transform.localScale = Vector3.one * GeniesScale;
                foreach (Transform c in transform)
                    if (c.gameObject != h.Root && c != _plateRoot && c != CarrySocket && c.name != "plotIcon") c.gameObject.SetActive(false);
                _rig = null; _kid = null;
                _anim = h.Animator;
                if (_anim != null)
                {
                    var head = _anim.isHuman ? _anim.GetBoneTransform(HumanBodyBones.Head) : null; if (head != null) Head = head;
                    var ik = h.Root.GetComponentInChildren<Animator>(); if (ik != null && ik.gameObject.GetComponent<GeniesIkRelay>() == null) ik.gameObject.AddComponent<GeniesIkRelay>().Owner = this;
                }
                if (_plateRoot != null) _plateRoot.localPosition = new Vector3(0, 1.54f * GeniesScale + 0.2f, 0); // Genies are 1.54 m: plate just above the hair
                Tracked = null;
                GeniesLoaded?.Invoke(this);
            }, () => this != null && req == _geniesReq);
        }

        bool _subscribed, _lookRequested, _lookFailed, _plateShown = true; string _wantDef;
        void OnSeatLooksChanged()
        {
            if (this == null || AvatarService.Backend == null) return;
            var d = AvatarService.DefinitionFor(Slot, IsBot);
            if (!string.IsNullOrEmpty(d) && d != _wantDef && d != GeniesDefinition) SetGeniesLook(d);
            else if (_genies == null && _lookFailed) SetGeniesLook(d);   // a look became available after a failed load: try again
        }
        void OnDestroy() { if (_subscribed) AvatarService.SeatLooksChanged -= OnSeatLooksChanged; _geniesReq++; _genies?.Release?.Invoke(); _genies = null; }

        /// <summary>Called by GeniesIkRelay (the Animator lives on the Genies root): hand IK for reaching and carrying.</summary>
        /// <summary>Remote human's tracked hands (Classroom-VR "VRMap" idea: the network pose drives the body's IK targets) - set by
        /// RemoteHumanView when the seat wears a Genies body; null/inactive = no tracking for that hand.</summary>
        public Transform IkLeft, IkRight;
        internal void GeniesIk()
        {
            if (_anim == null || Tracked != null) return;
            bool tl = IkLeft != null && IkLeft.gameObject.activeInHierarchy, tr = IkRight != null && IkRight.gameObject.activeInHierarchy;
            if (tl || tr)
            {
                _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, tl ? 1f : 0f); _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, tr ? 1f : 0f);
                _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f); _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                if (tl) _anim.SetIKPosition(AvatarIKGoal.LeftHand, IkLeft.position);
                if (tr) _anim.SetIKPosition(AvatarIKGoal.RightHand, IkRight.position);
                return;
            }
            float w = ReachTarget != null ? _reachWeight : 0f;
            if (w <= 0f && _carrying && CarrySocket != null) { w = 0.8f; }
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, w); _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, w);
            if (w <= 0f) return;
            var t = ReachTarget != null && _reachWeight > 0f ? ReachTarget : CarrySocket;
            _anim.SetIKPosition(AvatarIKGoal.RightHand, t.position + t.right * 0.09f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, t.position - t.right * 0.09f);
        }
        bool _carrying;

        AstraRig _rig;
        public AstraRig Rig => _rig;
        /// <summary>Locked decision 02 (detached-piece siblings). Astra's approved cast is ALREADY built as floating head / torso /
        /// hands / shoes on a Root-Chest-Head-Wrist-Foot rig (verified 2026-11-09, recipe_diag siblings), so nothing is cut; the
        /// pass stays available for any future full-body sibling model.</summary>
        public static bool Detached = false;
        public DetachedLimbs.Report LastDetach;
        /// <summary>Number of wardrobe pieces shown.</summary>
        public int Outfit;

        /// <summary>Bots named after a cast member wear that character; anyone else gets the cast member of their seat.</summary>
        public static string CharacterKey(int slot, string displayName)
        {
            var n = (displayName ?? "").Trim().ToLowerInvariant();
            foreach (var k in CharacterCatalog.SiblingKeys) if (n == k) return k;
            return CharacterCatalog.SiblingKeys[((slot % 4) + 4) % 4];
        }

        public void SetName(string n, bool bot)
        {
            IsBot = bot;
            if (_plate != null) _plate.text = $"{n} <size=70%><color=#{(bot ? "FFD65A" : "65C8C5")}>{(bot ? "BOT" : "HUMAN")}</color></size>";
        }

        public void ShowStatus(string s, float seconds)
        {
            _status = s; _statusUntil = Time.time + seconds;
            if (_rig != null && !string.IsNullOrEmpty(s))
            {
                var l = s.ToLowerInvariant();
                if (l.StartsWith("hehe")) _rig.Play("laugh"); else if (l.StartsWith("oops")) _rig.Play("surprised"); else if (l.StartsWith("caught")) _rig.Play("react");
                else if (l.StartsWith("phew") || l.StartsWith("nice")) _rig.Play("celebrate"); else if (l.StartsWith("shh")) _rig.Play("shush"); else if (l.StartsWith("off to bed")) _rig.Play("lose");
            }
            if (!string.IsNullOrEmpty(s)) RoomFx.Say(transform.position + Vector3.up * 1.85f, s, IsBot ? Palette.Yellow : Palette.Teal, Mathf.Min(seconds, 2f), 0.55f);
        }

        public void SetMotion(float speed, bool carrying)
        {
            _speed = Mathf.Lerp(_speed, speed, 0.2f); _carrying = carrying;
            if (_rig != null) { _rig.SetSpeed(speed); return; }
            if (_kid != null) { _kid.SetSpeed(_speed); _kid.Carrying = carrying; }
            else if (_anim != null && _anim.runtimeAnimatorController != null) { _anim.SetFloat(SpeedId, _speed); _anim.SetBool(CarryId, carrying); }
        }

        public void Trigger(string name)
        {
            if (_rig != null)
            {
                string c = name == "Cheer" ? "celebrate" : name == "Shush" ? "shush" : name == "Oops" ? "surprised" : name == "Caught" ? "react" : name == "Hehe" ? "laugh" : name.ToLowerInvariant();
                _rig.Play(c); return;
            }
            if (_kid != null) { if (name == "Cheer") _kid.CheerUntil = Time.time + 1.6f; else if (name == "Shush") _kid.ShushUntil = Time.time + 1.2f; return; }
            if (_anim != null && _anim.runtimeAnimatorController != null) { foreach (var p in _anim.parameters) if (p.name == name && p.type == AnimatorControllerParameterType.Trigger) { _anim.SetTrigger(name); break; } }
        }

        /// <summary>
        /// FUN-G3 sabotage wind-up tell: while a bot sneaks toward a rival's item / sets a trap it giggles, tiptoes with a
        /// shush, and a wobbling purple "?!" hovers over its head - readable from across the room before anything happens.
        /// </summary>
        public void SetPlotting(bool on)
        {
            if (on == _plotting) return;
            _plotting = on;
            if (on)
            {
                ShowStatus("hehe...", 1.6f);
                Debug.Log($"[SHHH] tell: {name} plotting (giggle + ?! icon)");
                AudioBank.I?.Play(AudioBank.I.Has("bot_giggle") ? "bot_giggle" : "squeak", transform.position + Vector3.up * 1.3f, 0.55f, 0.08f, AudioBus.Voice);
                if (_plotIcon == null)
                {
                    _plotIcon = WorldText.Make(transform, "plotIcon", "?!", new Vector3(0, 2.05f, 0), 1.4f, Palette.Purple, TMPro.TextAlignmentOptions.Center, 0.4f);
                }
                _plotIcon.gameObject.SetActive(true);
            }
            else if (_plotIcon != null) _plotIcon.gameObject.SetActive(false);
        }
        bool _plotting; TMPro.TextMeshPro _plotIcon;

        public void SetReach(Transform target, float weight) { ReachTarget = target; _reachWeight = weight; }

        void LateUpdate()
        {
            // loading silhouette: a gentle breathing glow; still no Genies body after 25 s -> show the old stand-in instead
            if (_placeholder != null)
            {
                if (_phMat != null && _phMat.HasProperty("_BaseColor")) { var c = _phColor; c.a = 0.35f + 0.15f * Mathf.Sin(Time.time * 3f + Slot); _phMat.SetColor("_BaseColor", c); }
                if (_genies == null && Time.time - _phSince > 25f) BuildStandIn();
            }
            if (_plateRoot != null)
            {
                WorldText.Billboard(_plateRoot);
                // V12 (Quest run: bots' nameplates filled the view up close): smaller up close, hidden inside 0.7 m
                var cam = Camera.main;
                if (cam != null && _plateRoot.gameObject.activeSelf)   // a plate switched off on purpose (family stage) stays off
                {
                    float d = Vector3.Distance(cam.transform.position, _plateRoot.position);
                    bool show = d > 0.7f;
                    if (show != _plateShown) { _plateShown = show; foreach (var r in _plateRoot.GetComponentsInChildren<Renderer>(true)) r.enabled = show; }
                    _plateRoot.localScale = Vector3.one * Mathf.Clamp(d / 2.5f, 0.35f, 1f);
                }
            }
            if (_plotting && _plotIcon != null) { WorldText.Billboard(_plotIcon.transform); _plotIcon.transform.localPosition = new Vector3(0, 2.05f + Mathf.Sin(Time.time * 6f) * 0.04f, 0); _plotIcon.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 9f) * 0.08f); }
        }

        void OnAnimatorIK(int layer)
        {
            if (_anim == null || ReachTarget == null || _reachWeight <= 0f) return;
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _reachWeight);
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, _reachWeight);
            _anim.SetIKPosition(AvatarIKGoal.RightHand, ReachTarget.position + ReachTarget.right * 0.08f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, ReachTarget.position - ReachTarget.right * 0.08f);
        }
    }
}
