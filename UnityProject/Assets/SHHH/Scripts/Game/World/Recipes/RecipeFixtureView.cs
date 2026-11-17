using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// One recipe fixture on a station (tray, rack, chest, drawer, hooks...). Local +Z points toward the player.
    /// Shows slot pads with their tag stickers, a hinged lid (poke "Open"/"Close"), smudge decals (rub to wipe) and control
    /// buttons (flags). On the authority it snaps a released recipe item into the nearest free slot and reports
    /// <c>PlaceInSocket</c> (+ <c>Align</c>) through <see cref="RecipeRuntime.Submit"/>. Every peer renders from the replicated
    /// <see cref="RecipeWorld"/> state, so what you see is what the rules think.
    /// </summary>
    public sealed class RecipeFixtureView : MonoBehaviour
    {
        public RecipeFixturePlacement Placement;
        public RecipeFixtureDecl Decl;
        public RecipeInstance Instance;
        RecipeRuntime _rt;
        Transform _lidHinge; float _lidAngle; PokeButton _lidButton;
        readonly List<GameObject> _decals = new List<GameObject>();
        readonly List<float> _patchR = new List<float>(); readonly List<Material> _patchMat = new List<Material>();
        float _scrub; int _scrubPatch = -1;

        /// <summary>The patch that has to be scrubbed next (cleaned in index order), or -1 when the top is clean.</summary>
        public int ActivePatch { get { var st = State; return st != null && st.Decals > 0 && st.Decals <= _decals.Count ? st.Decals - 1 : -1; } }
        public Vector3 PatchWorld(int i) => i >= 0 && i < _decals.Count ? _decals[i].transform.position : transform.position + Vector3.up * BaseH;
        public float PatchRadius(int i) => i >= 0 && i < _patchR.Count ? _patchR[i] : 0.05f;
        /// <summary>Local scrub progress 0..1 of the active patch (presentation; the authoritative step is the Stroke op).</summary>
        public void SetScrub(float k) { _scrub = Mathf.Clamp01(k); }

        static Texture2D _dirt;
        /// <summary>Procedural dirt blotch: noisy radial alpha, so raising the alpha cutoff erodes it from the edges inward.</summary>
        static Texture2D DirtTexture()
        {
            if (_dirt != null) return _dirt;
            const int N = 64; _dirt = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "shhh_dirt_blotch", wrapMode = TextureWrapMode.Clamp };
            var r = new System.Random(4242); var px = new Color32[N * N];
            float[] lobes = new float[7]; for (int i = 0; i < 7; i++) lobes[i] = 0.75f + (float)r.NextDouble() * 0.35f;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f, d = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float edge = 0.78f * Mathf.Lerp(lobes[(int)((a + Mathf.PI) / (2 * Mathf.PI) * 7f) % 7], lobes[((int)((a + Mathf.PI) / (2 * Mathf.PI) * 7f) + 1) % 7], 0.5f);
                float n = (float)r.NextDouble() * 0.22f;
                float al = Mathf.Clamp01(1f - d / edge) * 0.95f + n * (d < edge ? 1f : 0f);
                byte shade = (byte)(200 + r.Next(55));
                px[y * N + x] = new Color32(shade, shade, shade, (byte)(Mathf.Clamp01(al) * 255));
            }
            _dirt.SetPixels32(px); _dirt.Apply(true, false); return _dirt;
        }
        readonly List<Renderer> _pads = new List<Renderer>();
        readonly Dictionary<string, PokeButton> _flagButtons = new Dictionary<string, PokeButton>();
        public float WallHeight { get; private set; }
        /// <summary>Wash basin with a running tap (KT01) - null for ordinary fixtures.</summary>
        public SinkTapView Tap { get; private set; }
        public SoapPumpView Pump { get; private set; }
        /// <summary>V12: the real house furniture / appliance this fixture IS (washer drum, dryer + lint panel, towel drawer, lint bin).</summary>
        public MechHost Host { get; private set; }
        public WasherAppliance Appliance => Host as WasherAppliance;
        public ArticulatedModel Rack { get; private set; }
        /// <summary>V10: physical seating for an upright dish rack (null for other fixtures).</summary>
        public RackSeating Seating { get; private set; }
        public bool Upright => Decl != null && RecipeLayout.IsUprightRack(Decl.Kind);
        public float Top => transform.position.y + BaseH;
        float BaseH = 0.02f;   // V10: 0 for fixtures standing on real furniture (world anchors) - no base box

        // snap state (authority)
        PropBody _candidate, _snapping; float _settle, _snapT; Vector3 _snapFrom, _snapTo; Quaternion _snapFromRot, _snapToRot; int _snapSlot, _snapActor; bool _snapOriented;

        public RFixture State => _rt != null ? _rt.World.Fixture(Placement.FixtureId) : null;

        public void Build(RecipeRuntime rt, RecipeFixturePlacement pl, RecipeFixtureDecl decl, RecipeInstance inst, Color owner, float itemHeight)
        {
            _rt = rt; Placement = pl; Decl = decl; Instance = inst;
            name = "RF_" + pl.Kind + "_" + pl.FixtureId;
            transform.localPosition = VisualFactory.V(pl.Position);
            transform.localRotation = Quaternion.Euler(0, pl.Yaw, 0);
            float w = pl.Width, d = pl.Depth;
            if (pl.Anchored && decl.Kind == "sink_tap")
            {
                // V9: the house's own kitchen sink - no table base; only the working parts are added
                WallHeight = 0.15f; Tap = gameObject.AddComponent<SinkTapView>(); Tap.BuildAnchored(this, rt, w, d, owner);
                var lab = WorldText.Make(transform, "name", "Sink - " + inst.Def.Id, new Vector3(0, 0.17f, d * 0.5f + 0.25f), 0.14f, Palette.Navy, TextAlignmentOptions.Center, 0.4f);
                lab.transform.localRotation = Quaternion.Euler(0, 180, 0); lab.outlineWidth = 0;
                return;
            }
            if (pl.Anchored && decl.Kind != "sink_tap")
            {
                // V12: real house furniture that IS this fixture (washer, dryer, chest of drawers, bin): no box, lid or buttons are added
                var host = MechHost.Find(decl.Kind, transform.position);
                if (host != null) { Host = host; host.Bind(this); BaseH = 0f; WallHeight = 0f; return; }
                if (decl.Kind == "washer" || decl.Kind == "washer2" || decl.Kind == "dryer" || decl.Kind == "towel_cupboard" || decl.Kind == "lint_panel" || decl.Kind == "lint_bin")
                    Debug.LogWarning($"[SHHH][recipe] {inst.Def.Id}: no house {decl.Kind} near {transform.position:F2} - falling back to the table fixture");
            }
            bool onFurniture = pl.Anchored;   // V10 world anchor: stands on the real counter, no cream base / owner rim / label / pads
            if (onFurniture) BaseH = 0f;
            else
            {
                var body = Mats.Lit(Color.Lerp(Palette.Cream, owner, 0.25f));
                var baseGo = Prim.Box(transform, "base", new Vector3(0, BaseH * 0.5f, 0), new Vector3(w, BaseH, d), body);
                baseGo.layer = Layers.Furniture;
            }
            if (decl.Kind == "soap_pump") { Pump = gameObject.AddComponent<SoapPumpView>(); Pump.Build(this, rt, BaseH); }
            bool uprightRack = RecipeLayout.IsUprightRack(decl.Kind);
            if (uprightRack)
            {
                // V9: the daycare KIT018 dish drying rack (CC0) - plates stand on edge between its real divider colliders
                Rack = ArticulatedModel.Build("dc_kit018", transform, "dish_rack", w, Layers.Furniture);
                if (Rack.Ok)
                {
                    Rack.Root.localPosition = new Vector3(0, BaseH, 0);
                    Seating = gameObject.AddComponent<RackSeating>(); Seating.Init(this, rt, Rack, 0.004f); // KIT018 wires are 4 mm thick
                }
            }
            // owner-colour rim (V5: every job is public and colour-coded) - not on real furniture
            if (!onFurniture) Prim.Box(transform, "rim", new Vector3(0, BaseH + 0.001f, d * 0.5f - 0.006f), new Vector3(w, 0.003f, 0.012f), Mats.Lit(owner), false);
            bool tap = decl.Kind == "sink_tap";
            WallHeight = tap ? 0.085f : decl.HasDoor ? Mathf.Clamp(itemHeight + 0.025f, 0.08f, 0.2f) : 0f;
            if (tap) { Tap = gameObject.AddComponent<SinkTapView>(); Tap.Build(this, rt, w, d, BaseH, owner); }
            else if (decl.HasDoor)
            {
                var wall = Mats.Lit(Color.Lerp(Palette.WoodLight, owner, 0.15f));
                float h = WallHeight;
                foreach (var (pos, size) in new[] {
                    (new Vector3(-w * 0.5f + 0.006f, BaseH + h * 0.5f, 0), new Vector3(0.012f, h, d)), (new Vector3(w * 0.5f - 0.006f, BaseH + h * 0.5f, 0), new Vector3(0.012f, h, d)),
                    (new Vector3(0, BaseH + h * 0.5f, -d * 0.5f + 0.006f), new Vector3(w, h, 0.012f)), (new Vector3(0, BaseH + h * 0.5f, d * 0.5f - 0.006f), new Vector3(w, h, 0.012f)) })
                { var g = Prim.Box(transform, "wall", pos, size, wall); g.layer = Layers.Furniture; }
                _lidHinge = new GameObject("lid_hinge").transform; _lidHinge.SetParent(transform, false);
                _lidHinge.localPosition = new Vector3(0, BaseH + h + 0.006f, -d * 0.5f);
                var lid = Prim.Box(_lidHinge, "lid", new Vector3(0, 0, d * 0.5f), new Vector3(w + 0.01f, 0.012f, d + 0.01f), Mats.Lit(owner));
                lid.layer = Layers.Furniture;
                Prim.Box(_lidHinge, "lip", new Vector3(0, -0.004f, d + 0.012f), new Vector3(Mathf.Min(0.08f, w * 0.5f), 0.02f, 0.012f), Mats.Lit(Palette.Cream), false);
                _lidButton = Button("Open", new Vector3(w * 0.5f - 0.05f, 0.03f, d * 0.5f + 0.035f), () =>
                {
                    var st = State; if (st == null) return;
                    RecipeRuntime.Submit(new ROp { Actor = GameServices.LocalSlot, Verb = st.Open ? Verb.Close : Verb.Open, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1 });
                });
            }
            // V12 (Quest run 2026-11-11 #18: the tray's two flat cream pads read as an open book; the coaster spot was a pad saying
            // "stack 2" in tiny text): a tray is a wooden tray (floor, raised rims, handle grips - visual only, no colliders) with a
            // round ring per mug; "spots" are coaster-sized round marks with a standing tag that says what goes there
            bool trayLook = !uprightRack && decl.Kind.Contains("tray");
            bool roundSpots = trayLook || decl.Kind.EndsWith("_spots") || decl.Kind.Contains("coaster");
            float padY = BaseH + 0.0015f;
            if (trayLook)
            {
                var wood = Mats.Lit(Palette.WoodLight); var dark = Mats.Lit(Color.Lerp(Palette.WoodLight, Palette.Ink, 0.55f));
                float rimH = 0.026f, t = 0.014f;
                Prim.Box(transform, "tray_floor", new Vector3(0, BaseH + 0.0015f, 0), new Vector3(w, 0.003f, d), Mats.Lit(Color.Lerp(Palette.Cream, owner, 0.3f)), false);
                Prim.Box(transform, "tray_rim_f", new Vector3(0, BaseH + rimH * 0.5f, d * 0.5f - t * 0.5f), new Vector3(w, rimH, t), wood, false);
                Prim.Box(transform, "tray_rim_b", new Vector3(0, BaseH + rimH * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(w, rimH, t), wood, false);
                foreach (float sx in new[] { -1f, 1f })
                {
                    Prim.Box(transform, "tray_end", new Vector3(sx * (w * 0.5f - t * 0.5f), BaseH + (rimH + 0.012f) * 0.5f, 0), new Vector3(t, rimH + 0.012f, d), wood, false);
                    Prim.Box(transform, "tray_grip", new Vector3(sx * (w * 0.5f - t * 0.5f), BaseH + rimH * 0.62f, 0), new Vector3(t + 0.002f, 0.011f, Mathf.Min(0.08f, d * 0.45f)), dark, false);
                }
                padY = BaseH + 0.0035f;
            }
            // slot pads (with the slot's tag sticker, or its order number for "in order" steps)
            bool ordered = false; foreach (var s in inst.Def.Steps) if (s.Fixture == decl.Kind && s.Rule == SlotRule.InOrder) ordered = true;
            int pads = uprightRack && Rack != null && Rack.Ok ? 0 : pl.Stack ? 1 : pl.Slots.Length;
            for (int k = 0; k < pads; k++)
            {
                var lp = transform.InverseTransformPoint(transform.parent.TransformPoint(VisualFactory.V(pl.Slots[k])));
                GameObject pad;
                if (roundSpots)
                {
                    float dia = Mathf.Clamp(Mathf.Min(pl.Stack ? Mathf.Min(w, d) : pl.Pitch, d) - 0.02f, 0.07f, 0.12f);
                    if (pl.Stack) lp = new Vector3(0f, lp.y, 0f);
                    // a ring: owner-tinted disc with a cream centre (cylinder primitives are 2 units tall: y scale = half height)
                    pad = Prim.Shape(PrimitiveType.Cylinder, transform, "slot" + k, new Vector3(lp.x, padY, lp.z), new Vector3(dia, 0.0012f, dia), Mats.Unlit(Color.Lerp(Palette.Cream, owner, 0.4f)));
                    Prim.Shape(PrimitiveType.Cylinder, transform, "slot_in" + k, new Vector3(lp.x, padY + 0.0006f, lp.z), new Vector3(dia * 0.72f, 0.0012f, dia * 0.72f), Mats.Unlit(Color.Lerp(Palette.Cream, Palette.WoodLight, 0.25f)));
                }
                else pad = Prim.Box(transform, "slot" + k, new Vector3(lp.x, BaseH + 0.0015f, lp.z), new Vector3(pl.Stack ? w - 0.03f : Mathf.Max(0.05f, pl.Pitch - 0.012f), 0.003f, d - 0.03f), Mats.Unlit(Color.Lerp(Palette.Cream, owner, 0.4f)), false);
                _pads.Add(pad.GetComponent<Renderer>());
                string tag = pl.Stack ? "" : decl.SlotTags[k];
                if (!string.IsNullOrEmpty(tag)) RecipeVisuals.Sticker(pad.transform.parent, tag, BaseH + 0.004f, 0.045f).localPosition = new Vector3(lp.x, BaseH + 0.004f, lp.z - (d * 0.5f - 0.04f) * 0.5f);
                else if (ordered && !pl.Stack) { var n = WorldText.Make(transform, "n" + k, (k + 1).ToString(), new Vector3(lp.x, BaseH + 0.004f, lp.z), 0.3f, Palette.Navy); n.transform.localRotation = Quaternion.Euler(90, 180, 0); n.outlineWidth = 0; }
            }
            if (pl.Stack && roundSpots)
            {
                // a small standing tag behind the mark: "2 coasters here" (readable from standing height, not flat on the table)
                string item = "items"; foreach (var s in inst.Def.Steps) if (s.Fixture == decl.Kind && !string.IsNullOrEmpty(s.PropKind)) item = s.PropKind.Replace('_', ' ') + (pl.Slots.Length > 1 ? "s" : "");
                var tagRoot = new GameObject("spot_tag").transform; tagRoot.SetParent(transform, false);
                tagRoot.localPosition = new Vector3(0f, BaseH + 0.035f, -Mathf.Min(d * 0.5f, 0.09f)); tagRoot.localRotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(-15f, 0, 0);
                WorldText.Board(tagRoot, "bg", new Vector3(0, 0, 0.002f), new Vector2(0.15f, 0.045f), Color.Lerp(Palette.Navy, owner, 0.15f));
                var tt = WorldText.Make(tagRoot, "t", $"{pl.Slots.Length} {item} here", new Vector3(0, 0, -0.001f), 0.2f, Palette.Cream, TextAlignmentOptions.Center, 0.145f); tt.outlineWidth = 0f;
                tagRoot.gameObject.AddComponent<FaceViewer>();
            }
            else if (pl.Stack) { var n = WorldText.Make(transform, "stack", "stack " + pl.Slots.Length, new Vector3(0, BaseH + 0.004f, d * 0.5f - 0.02f), 0.16f, Palette.Navy); n.transform.localRotation = Quaternion.Euler(90, 180, 0); n.outlineWidth = 0; }
            // V8 (2026-11-09) dirt patches: each smudge is a real patch at a real spot on the top. Scrubbing must happen ON the
            // current patch (the one that pulses); it erodes from the edges as you rub (local, per-hand progress) and the authoritative
            // Stroke is submitted when it is gone. Patches are cleaned in index order, so every peer agrees which one vanishes.
            var rng = new System.Random(pl.FixtureId * 7919 + 17);
            for (int i = 0; i < 4; i++)
            {
                float pr = 0.045f + (float)rng.NextDouble() * 0.02f;
                var lp = new Vector3((float)(rng.NextDouble() - 0.5) * Mathf.Max(0.02f, w - 0.1f), BaseH + 0.0035f + i * 0.0003f, (float)(rng.NextDouble() - 0.5) * Mathf.Max(0.02f, d - 0.1f));
                var m = new Material(UiKit.Cutout(DirtTexture())); m.SetColor("_BaseColor", Color.Lerp(new Color(0.42f, 0.29f, 0.2f), new Color(0.55f, 0.47f, 0.25f), (float)rng.NextDouble()));
                var g = UiKit.Quad(transform, "dirt" + i, m, lp, new Vector2(pr * 2f, pr * 2f));
                g.transform.localRotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f);
                g.SetActive(false); _decals.Add(g); _patchR.Add(pr); _patchMat.Add(m);
            }
            // controls (flags): a button per flag, labelled with the recipe step that needs it
            var flags = decl.Flags ?? new string[0];
            for (int i = 0; i < flags.Length; i++)
            {
                string flag = flags[i], label = flag;
                foreach (var s in inst.Def.Steps) if (s.Kind == StepKind.Flag && s.Fixture == decl.Kind && s.FlagName == flag) label = s.Label;
                float x = -w * 0.5f + 0.06f + i * 0.11f;
                var b = Button(Short(label), new Vector3(x, 0.03f, d * 0.5f + 0.035f), () =>
                {
                    var st = State; if (st == null) return;
                    RecipeRuntime.Submit(new ROp { Actor = GameServices.LocalSlot, Verb = Verb.Press, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1, Arg = st.Flags.Contains(flag) ? "!" + flag : flag });
                });
                _flagButtons[flag] = b;
            }
            if (!onFurniture)
            {
                var title = WorldText.Make(transform, "name", RecipeVisuals.PrettyKind(decl.Kind), new Vector3(0, 0.012f, d * 0.5f + 0.002f), 0.14f, Palette.Navy, TextAlignmentOptions.Center, w);
                title.transform.localRotation = Quaternion.Euler(0, 180, 0); title.outlineWidth = 0;
            }
        }

        static string Short(string s) => s.Length <= 14 ? s : s.Substring(0, 13) + "…";

        PokeButton Button(string label, Vector3 lp, System.Action press)
        {
            var b = PokeButton.Create(transform, label, lp, new Vector2(0.09f, 0.04f), Palette.Yellow, press, 0.16f);
            // face the player (+Z) and tip back 35° so it reads from standing or seated height
            b.transform.localRotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(35, 0, 0);
            return b;
        }

        // presentation sounds from the replicated fixture state (so every peer hears them, not only the host)
        int[] _sndOcc; int _sndDecals = -1, _sndFlags = -1; bool _sndOpen, _sndInit;

        void Sounds(RFixture st)
        {
            var ab = AudioBank.I;
            if (!_sndInit) { _sndOcc = (int[])st.Occupant.Clone(); _sndDecals = st.Decals; _sndFlags = st.Flags.Count; _sndOpen = st.Open; _sndInit = true; return; }
            if (ab != null)
            {
                for (int k = 0; k < st.Occupant.Length && k < _sndOcc.Length; k++)
                    if (st.Occupant[k] >= 0 && st.Occupant[k] != _sndOcc[k]) { var at = SlotPoint(Placement.Stack ? 0 : k); ab.Play("slot_click", at, 0.7f); RoomFx.Burst("sparkle", at + Vector3.up * 0.06f, 0.15f, 1f); }
                if (st.Decals < _sndDecals) { ab.Play("wipe", transform.position + Vector3.up * BaseH, 0.6f); RoomFx.Burst("bubbles", transform.position + Vector3.up * (BaseH + 0.02f), 0.12f, 1f); }
                if (st.Open != _sndOpen && Tap == null && Host == null) ab.Play(st.Open ? "lid_open" : "lid_close", transform.position + Vector3.up * (BaseH + 0.1f), 0.6f);
                if (st.Flags.Count != _sndFlags) ab.Play("ui_click", transform.position + Vector3.up * BaseH, 0.6f);
            }
            if (_sndOcc.Length != st.Occupant.Length) _sndOcc = new int[st.Occupant.Length];
            System.Array.Copy(st.Occupant, _sndOcc, st.Occupant.Length); _sndDecals = st.Decals; _sndFlags = st.Flags.Count; _sndOpen = st.Open;
        }

        void Update()
        {
            var st = State; if (st == null) return;
            Sounds(st);
            if (_lidHinge != null)
            {
                float target = st.Open ? -105f : 0f;
                _lidAngle = Mathf.MoveTowards(_lidAngle, target, 300f * Time.deltaTime);
                _lidHinge.localRotation = Quaternion.Euler(_lidAngle, 0, 0);
                if (_lidButton && _lidButton.Label != (st.Open ? "Close" : "Open")) _lidButton.SetLabel(st.Open ? "Close" : "Open");
            }
            for (int i = 0; i < _decals.Count; i++)
            {
                bool on = i < st.Decals; if (_decals[i].activeSelf != on) _decals[i].SetActive(on);
                if (!on) continue;
                bool active = i == st.Decals - 1;
                float k = active ? _scrub : 0f;
                _patchMat[i].SetFloat("_Cutoff", Mathf.Lerp(0.18f, 0.97f, k));
                float pulse = active && _scrub < 0.02f ? 1f + 0.05f * Mathf.Sin(Time.time * 5f) : 1f;
                _decals[i].transform.localScale = new Vector3(_patchR[i] * 2f * pulse, _patchR[i] * 2f * pulse, 1f);
            }
            if (st.Decals != _scrubPatch) { _scrubPatch = st.Decals; _scrub = 0f; }
            for (int k = 0; k < _pads.Count; k++)
            {
                bool filled = Placement.Stack ? st.Occupant[st.Occupant.Length - 1] >= 0 : st.Occupant[k] >= 0;
                // green = filled with a fitting item, coral = wrong item in a tagged slot, cream = empty
                bool wrong = false;
                if (filled && !Placement.Stack && !string.IsNullOrEmpty(st.SlotTags[k])) { var occ = _rt.World.Prop(st.Occupant[k]); wrong = occ != null && !string.IsNullOrEmpty(occ.Tag) && occ.Tag != st.SlotTags[k]; }
                var c = !filled ? Palette.Cream : wrong ? Palette.Coral : Palette.Ok;
                _pads[k].sharedMaterial = Mats.Unlit(Color.Lerp(c, Palette.Navy, filled ? 0.15f : 0.05f));
            }
            foreach (var kv in _flagButtons) kv.Value.SetColor(st.Flags.Contains(kv.Key) ? Palette.Ok : Palette.Yellow);
        }

        // ------------------------------------------------------------------ authority: snap released items into slots
        /// <summary>World position of a slot's pad centre (resting surface).</summary>
        public Vector3 SlotPoint(int slot) => Host != null ? Host.SlotPoint(Decl.Kind, slot) : Seating != null ? Seating.SlotPoint(slot) : transform.parent.TransformPoint(VisualFactory.V(Placement.Slots[Mathf.Clamp(slot, 0, Placement.Slots.Length - 1)]));

        int NearestFreeSlot(Vector3 p, out float dist)
        {
            var st = State; dist = float.MaxValue; int best = -1;
            for (int k = 0; k < st.Occupant.Length; k++)
            {
                if (st.Occupant[k] >= 0) continue;
                if (Placement.Stack) { dist = Flat(p - SlotPoint(k)); return k; } // stacks fill bottom-up
                float dd = Flat(p - SlotPoint(k)); if (dd < dist) { dist = dd; best = k; }
            }
            return best;
        }

        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }

        public bool Contains(Vector3 world, float above)
        {
            if (Host != null) return Host.Inside(Decl.Kind, world);
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= Placement.Width * 0.5f + 0.02f && Mathf.Abs(l.z) <= Placement.Depth * 0.5f + 0.02f && l.y >= -0.02f && l.y <= BaseH + Mathf.Max(WallHeight, 0.02f) + above;
        }

        void FixedUpdate()
        {
            if (!GameServices.IsAuthority || _rt == null) return;
            var round = _rt.Round; var st = State; if (round == null || st == null) return;
            if (Seating != null || Host != null) return;   // V10: an upright rack seats plates physically; V12: the washer loads what is in its drum (RackSeating), no kinematic snap
            if (_snapping != null) { StepSnap(); return; }
            if (!round.IsPlayable || (st.HasDoor && !st.Open)) { _candidate = null; return; }
            PropBody best = null; float bestD = float.MaxValue; int bestSlot = -1;
            foreach (var p in _rt.Bodies)
            {
                if (p == null || p.IsHeld || p.Reserved || p.Body == null || p.Body.isKinematic) continue;
                var rp = _rt.RProp(p); if (rp == null || rp.IsPlaced || rp.HeldBy != ActorIds.None || _rt.IsFollower(rp)) continue;
                if (!Contains(p.Col.bounds.center, Upright ? 0.24f : 0.14f)) continue;
                if (Upright) { float flat = Vector3.Angle(p.transform.up, Vector3.up); flat = Mathf.Min(flat, 180f - flat); if (flat < 35f) continue; } // lying flat on the rack: not racked
                int k = NearestFreeSlot(p.Col.bounds.center, out float dd);
                if (k < 0) continue;
                float tol = Placement.Stack ? Placement.Width * 0.6f : Mathf.Max(0.07f, Placement.Pitch * 0.75f);
                if (dd > tol) continue;
                if (dd < bestD) { bestD = dd; best = p; bestSlot = k; }
            }
            if (best == null) { _candidate = null; _settle = 0; return; }
            if (best != _candidate) { _candidate = best; _settle = 0; }
            if (best.Speed < 0.35f) _settle += Time.fixedDeltaTime; else _settle = 0;
            if (_settle < 0.12f || Time.time - best.LastReleaseTime > 30f) return;
            BeginSnap(best, bestSlot);
        }

        void BeginSnap(PropBody p, int slot)
        {
            var pt = SlotPoint(slot);
            if (Upright)
            {
                // stand it in the gap: face normal along the rack's long axis, resting on the tray floor
                float side = Vector3.Dot(p.transform.up, transform.right) >= 0f ? 1f : -1f;
                _snapToRot = Quaternion.LookRotation(transform.forward, transform.right * side);
                _snapOriented = true;
                float radius = RecipePropSize.Of(p.Spec.Kind) * 0.5f;
                var centre = new Vector3(pt.x, transform.position.y + BaseH + 0.022f + radius, pt.z);
                var localOff = Quaternion.Inverse(p.transform.rotation) * (p.transform.position - p.Col.bounds.center);
                _snapTo = centre + _snapToRot * localOff;
                _snapping = p; _snapT = 0; _snapFrom = p.transform.position; _snapFromRot = p.transform.rotation; _snapSlot = slot; _snapActor = p.LastActor;
                p.ReservedUntil = Time.time + 1f; p.Body.isKinematic = true; p.IgnoreImpactsUntil = Time.time + 0.7f;
                return;
            }
            float bottom = p.Col.bounds.min.y, pivotUp = p.transform.position.y - bottom;
            float rest = Placement.Stack ? StackTop(pt.y) : pt.y;
            // oriented = upright (within 35°) and square to the fixture (within 35° of a quarter turn)
            float tilt = Vector3.Angle(p.transform.up, Vector3.up);
            float rel = YawOf(p.transform) - YawOf(transform); rel = Mathf.DeltaAngle(0, rel);
            float quarter = Mathf.Round(rel / 90f) * 90f;
            _snapOriented = tilt <= 35f && Mathf.Abs(rel - quarter) <= 35f;
            if (!_snapOriented) Debug.Log($"[SHHH][recipe] {p.Spec.Kind}#{p.Spec.Id} arrives unsquare in {Decl.Kind}: tilt {tilt:0}°, yaw off {Mathf.Abs(rel - quarter):0}°");
            _snapToRot = _snapOriented ? Quaternion.Euler(0, YawOf(transform) + quarter, 0) : p.transform.rotation;
            // keep the collider centred over the slot: carry the pivot->centre offset through the rotation change
            var off = p.transform.position - p.Col.bounds.center; off.y = 0;
            _snapTo = new Vector3(pt.x, rest + pivotUp + 0.002f, pt.z) + _snapToRot * Quaternion.Inverse(p.transform.rotation) * off;
            _snapping = p; _snapT = 0; _snapFrom = p.transform.position; _snapFromRot = p.transform.rotation; _snapSlot = slot; _snapActor = p.LastActor;
            p.ReservedUntil = Time.time + 1f; p.Body.isKinematic = true; p.IgnoreImpactsUntil = Time.time + 0.7f; // the snap itself is silent
        }

        /// <summary>Heading of a transform's forward projected on the floor (robust when the item is pitched/rolled).</summary>
        public static float YawOf(Transform t) { var f = t.forward; f.y = 0; if (f.sqrMagnitude < 1e-4f) { f = t.up; f.y = 0; f = -f; } return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; }

        /// <summary>Upright within 40° and square (within 40° of a quarter turn) to the fixture.</summary>
        public static bool Square(Transform fixture, Transform item)
        {
            if (Vector3.Angle(item.up, Vector3.up) > 40f) return false;
            float rel = Mathf.DeltaAngle(0, YawOf(item) - YawOf(fixture));
            return Mathf.Abs(rel - Mathf.Round(rel / 90f) * 90f) <= 40f;
        }

        float StackTop(float floor)
        {
            float top = floor; var st = State;
            foreach (var id in st.Occupant) { if (id < 0) continue; var b = _rt.Body(id); if (b != null) top = Mathf.Max(top, b.Col.bounds.max.y); }
            return top;
        }

        void StepSnap()
        {
            var p = _snapping;
            if (p == null || p.IsHeld) { _snapping = null; _candidate = null; _settle = 0; if (p != null && !p.IsHeld) p.Body.isKinematic = false; return; }
            _snapT += Time.fixedDeltaTime / 0.2f;
            float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_snapT));
            p.Body.MovePosition(Vector3.Lerp(_snapFrom, _snapTo, e)); p.Body.MoveRotation(Quaternion.Slerp(_snapFromRot, _snapToRot, e));
            if (_snapT < 1f) return;
            _snapping = null; _candidate = null; _settle = 0;
            // an upright rack holds what it racks: the tines constrain the plate (kinematic while seated; a grab frees it)
            p.Body.isKinematic = Upright; if (!Upright) { p.Body.linearVelocity = Vector3.zero; p.Body.angularVelocity = Vector3.zero; } p.IgnoreImpactsUntil = Time.time + 0.4f;
            var rp = _rt.RProp(p); var st = State; if (rp == null || st == null) return;
            var err = RecipeRuntime.Submit(new ROp { Actor = _snapActor, Verb = Verb.PlaceInSocket, Prop = rp.Id, Prop2 = -1, Fixture = st.Id, Slot = _snapSlot });
            if (err != null) { Debug.Log($"[SHHH][recipe] {st.Kind} slot {_snapSlot} rejected {rp.Kind}#{rp.Id} by {_snapActor}: {err}"); p.Body.AddForce(transform.forward * 0.6f + Vector3.up * 0.4f, ForceMode.VelocityChange); return; }
            if (rp.Oriented != _snapOriented) RecipeRuntime.Submit(new ROp { Actor = _snapActor, Verb = Verb.Align, Prop = rp.Id, Prop2 = -1, Fixture = st.Id, Slot = _snapSlot, Arg = _snapOriented ? "" : "tilt" });
            PlacePairPartner(rp);
        }

        /// <summary>
        /// A pair (two socks) rides together; the rules still want each half in its own slot. When the leader lands in a slot that
        /// takes its kind, its partner goes into the nearest free slot beside it (same actor), and stops riding - a rolled pair in the
        /// drawer. Taking one out later is an ordinary removal (the pair itself stays matched).
        /// </summary>
        void PlacePairPartner(RProp rp)
        {
            if (rp.Partner < 0) return; var w = _rt.World; var q = w.Prop(rp.Partner); if (q == null || q.IsPlaced) return;
            var qb = _rt.Body(q.Id); if (qb == null || qb.IsHeld) return;
            var inst = _rt.Board.Instances[q.RecipeInstance]; bool wanted = false;
            foreach (var s in inst.Def.Steps) if (s.Kind == StepKind.Place && s.PropKind == q.Kind && s.Fixture == Decl.Kind) wanted = true;
            if (!wanted) return;
            int k = NearestFreeSlot(_rt.Body(rp.Id).Col.bounds.center, out _); if (k < 0) return;
            // the rules place only what the actor holds: the partner rode in the same hand
            if (q.HeldBy != _snapActor) RecipeRuntime.Submit(new ROp { Actor = _snapActor, Verb = Verb.PickUp, Prop = q.Id, Prop2 = -1, Fixture = -1, Slot = -1 });
            var err = RecipeRuntime.Submit(new ROp { Actor = _snapActor, Verb = Verb.PlaceInSocket, Prop = q.Id, Prop2 = -1, Fixture = State.Id, Slot = k });
            if (err != null) { Debug.Log($"[SHHH][recipe] pair partner {q.Kind}#{q.Id} not placed: {err}"); return; }
            _rt.Unfollow(q.Id);
            var pt = SlotPoint(k); float rest = Placement.Stack ? StackTop(pt.y) : pt.y;
            var rot = Quaternion.Euler(0, YawOf(transform), 0);
            qb.Body.isKinematic = false; qb.Body.linearVelocity = Vector3.zero; qb.Body.angularVelocity = Vector3.zero;
            float pivotUp = qb.transform.position.y - qb.Col.bounds.min.y;
            var pos = new Vector3(pt.x, rest + pivotUp + 0.004f, pt.z);
            qb.transform.SetPositionAndRotation(pos, rot); qb.Body.position = pos; qb.Body.rotation = rot;
            qb.ReservedUntil = Time.time + 0.6f; qb.IgnoreImpactsUntil = Time.time + 0.8f;
            if (!q.Oriented) RecipeRuntime.Submit(new ROp { Actor = _snapActor, Verb = Verb.Align, Prop = q.Id, Prop2 = -1, Fixture = State.Id, Slot = k, Arg = "" });
        }
    }
}
