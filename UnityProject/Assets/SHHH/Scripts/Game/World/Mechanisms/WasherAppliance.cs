using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 front-loading washer / dryer of the house (laundry room), built from the split PtDesigns washer (CC0; Blender split 2026-11-10:
    /// PART_Door, PART_DetergentDrawer, PART_Dial, PART_StartButton, PART_Drum) in place of the baked static washer/dryer.
    /// <list type="bullet">
    /// <item>Door: <see cref="HingedPart"/> on its real hinge, grasped at the edge opposite the hinge; soft-close latch or slam (Mom);
    /// LOCKED while the drum turns (the rules refuse Open while "start" is set, and the hinge only rattles).</item>
    /// <item>Detergent drawer: <see cref="SlidingPart"/> on its rail with a finite <see cref="LiquidContainer"/> tray - pour detergent in
    /// from a bottle; the wash draws it into the drum (suds).</item>
    /// <item>Programme dial: <see cref="RotaryKnob"/> with 6 detents (Spin is the loud one). START: a real <see cref="MechButton"/> that
    /// asks the rules to start / stop (refused with the door open).</item>
    /// <item>Drum: an open cylinder of colliders with three lifters on a kinematic body that really turns while running, so the bundles
    /// inside tumble; the front bezel and the door close it. Laundry counts as loaded only when it is physically inside the drum.</item>
    /// </list>
    /// Recipe fixtures (LD04 washer, LD05 washer2 / dryer) bind to it (<see cref="Bind"/>); without a recipe it still works as furniture.
    /// </summary>
    public sealed class WasherAppliance : MechHost
    {
        public static readonly List<WasherAppliance> All = new List<WasherAppliance>();
        public const string ModelKey = "ld_laundry_washer_mech";
        public static readonly string[] Programmes = { "Quick 30", "Cotton 40", "Delicates", "Wool", "Spin", "Rinse" };

        public string Kind = "washer";   // "washer" | "dryer"
        public ArticulatedModel Model;
        public HingedPart Door; public SlidingPart Drawer; public RotaryKnob Dial; public MechButton StartButton;
        public LiquidContainer Detergent;
        public Transform Drum;
        /// <summary>World drum geometry (centre of the cylinder, axis toward the front, inner radius, depth).</summary>
        public Vector3 DrumCentre => transform.TransformPoint(_drumC);
        public float DrumRadius { get; private set; }
        public float DrumDepth { get; private set; }
        public bool Running => StartFixtures() > 0 ? _runRemote : _runLocal;
        public float DrumAngle { get; private set; }
        public int RejectedStarts { get; private set; }
        public string LastStartError { get; private set; }
        public float DetergentUsedL { get; private set; }
        public int NoiseReports { get; private set; }

        Vector3 _drumC; Rigidbody _ringRb; Quaternion _ringBase, _drumRest; Vector3 _drumAxisLocal;
        bool _runLocal, _runRemote; int _starter = ActorIds.None; float _spinVel, _noiseAt, _sudsAt, _dir = 1f, _dirAt;
        AudioSource _hum; Vector3 _shakeBase; float _trayWidth, _trayDepth;

        protected override void OnEnable() { base.OnEnable(); All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        // ---- MechHost: washer drum (washer / washer2), dryer drum + its door as the lint panel (LD09)
        public override bool Serves(string k) => Kind == "washer" ? k == "washer" || k == "washer2" : k == "dryer" || k == "lint_panel";
        public override MechPart DoorFor(string k) => Door;
        public override Vector3 SlotPoint(string k, int slot) => SlotPoint(slot);
        public override bool Inside(string k, Vector3 world) => InDrum(world);
        public override Vector3 Focus => DrumCentre;
        public override Vector3 LoadFrom(string k) => Front;
        int StartFixtures() { int n = 0; foreach (var fv in Bound) { var st = fv.State; if (st != null && st.FlagNames != null && System.Array.IndexOf(st.FlagNames, "start") >= 0) n++; } return n; }

        /// <summary>Build at a floor point (parent space) facing local +Z after <paramref name="yaw"/>, fitted to a footprint width (m).</summary>
        public static WasherAppliance Build(string kind, Transform parent, Vector3 floorLocal, float yaw, float fitExtent)
        {
            var g = new GameObject("V12_" + kind); g.transform.SetParent(parent, false);
            g.transform.localPosition = floorLocal; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var am = ArticulatedModel.Build(ModelKey, g.transform, kind + "_model", fitExtent, Layers.Furniture);
            if (!am.Ok) { Object.Destroy(g); return null; }
            var wa = g.AddComponent<WasherAppliance>(); wa.Kind = kind; wa.Model = am; wa.Assemble();
            return wa;
        }

        static Bounds LocalBoundsOf(Transform t, Transform space)
        {
            bool any = false; var b = new Bounds();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = space.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { b = new Bounds(c, Vector3.zero); any = true; } else b.Encapsulate(c);
                }
            }
            return b;
        }

        static BoxCollider Box(Transform parent, string name, Vector3 min, Vector3 max)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.layer = Layers.Furniture;
            var bc = g.AddComponent<BoxCollider>(); bc.center = (min + max) * 0.5f; bc.size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
            return bc;
        }

        void Assemble()
        {
            var root = transform; var am = Model;
            var vis = LocalBoundsOf(am.Root, root);
            // ---- drum geometry (local to this appliance; +Z = front)
            var drumT = am.Part("PART_Drum"); Drum = drumT;
            var db = drumT != null ? LocalBoundsOf(drumT, root) : new Bounds(new Vector3(0, vis.size.y * 0.5f, 0), new Vector3(0.38f, 0.38f, 0.4f));
            _drumC = db.center; DrumRadius = Mathf.Min(db.extents.x, db.extents.y) * 0.97f; DrumDepth = db.size.z;
            float zFront = vis.max.z, zBack = vis.min.z, dr = DrumRadius;
            // ---- static body (frame around the drum; Furniture layer, static)
            var body = new GameObject("body_colliders").transform; body.SetParent(root, false);
            Box(body, "top", new Vector3(vis.min.x, _drumC.y + dr + 0.01f, zBack), new Vector3(vis.max.x, vis.max.y, zFront - 0.002f));
            Box(body, "bottom", new Vector3(vis.min.x, vis.min.y, zBack), new Vector3(vis.max.x, _drumC.y - dr - 0.01f, zFront));
            Box(body, "left", new Vector3(vis.min.x, _drumC.y - dr - 0.01f, zBack), new Vector3(_drumC.x - dr - 0.01f, _drumC.y + dr + 0.01f, zFront));
            Box(body, "right", new Vector3(_drumC.x + dr + 0.01f, _drumC.y - dr - 0.01f, zBack), new Vector3(vis.max.x, _drumC.y + dr + 0.01f, zFront));
            Box(body, "back", new Vector3(_drumC.x - dr - 0.01f, _drumC.y - dr - 0.01f, zBack), new Vector3(_drumC.x + dr + 0.01f, _drumC.y + dr + 0.01f, _drumC.z - DrumDepth * 0.5f - 0.012f));
            // front bezel corners (between the round opening and the square frame)
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var g = new GameObject("bezel" + i); g.transform.SetParent(body, false); g.layer = Layers.Furniture;
                g.transform.localPosition = new Vector3(_drumC.x, _drumC.y, zFront - 0.015f) + dir * (dr + 0.07f);
                g.transform.localRotation = Quaternion.Euler(0, 0, 45f + 90f * i);
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.14f, 0.3f, 0.03f);
            }
            // ---- drum: rotating ring of colliders + lifters (kinematic: the laundry tumbles against it)
            var ring = new GameObject("drum_ring"); ring.layer = Layers.Furniture; ring.transform.SetParent(root, false);
            ring.transform.localPosition = _drumC; ring.transform.localRotation = Quaternion.identity;
            _ringRb = ring.AddComponent<Rigidbody>(); _ringRb.isKinematic = true; _ringRb.interpolation = RigidbodyInterpolation.Interpolate; _ringBase = ring.transform.localRotation;
            const int N = 16; float seg = 2f * Mathf.PI * (dr + 0.01f) / N * 1.2f;
            for (int i = 0; i < N; i++)
            {
                float a = i * 360f / N; var g = new GameObject("stave" + i); g.layer = Layers.Furniture; g.transform.SetParent(ring.transform, false);
                g.transform.localRotation = Quaternion.Euler(0, 0, a); g.transform.localPosition = g.transform.localRotation * new Vector3(dr + 0.01f, 0, 0);
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.02f, seg, DrumDepth);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f; var g = new GameObject("lifter" + i); g.layer = Layers.Furniture; g.transform.SetParent(ring.transform, false);
                g.transform.localRotation = Quaternion.Euler(0, 0, a); g.transform.localPosition = g.transform.localRotation * new Vector3(dr - 0.04f, 0, 0);
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.075f, 0.04f, DrumDepth * 0.9f);
            }
            var grip = new PhysicsMaterial("washer_drum") { dynamicFriction = 0.9f, staticFriction = 1.0f, frictionCombine = PhysicsMaterialCombine.Maximum };
            foreach (var c in ring.GetComponentsInChildren<Collider>()) c.sharedMaterial = grip;
            { var g = new GameObject("drum_back"); g.layer = Layers.Furniture; g.transform.SetParent(ring.transform, false); g.transform.localPosition = new Vector3(0, 0, -DrumDepth * 0.5f); var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(dr * 2f, dr * 2f, 0.02f); }
            if (drumT != null) { _drumRest = drumT.localRotation; _drumAxisLocal = drumT.parent.InverseTransformDirection(root.forward).normalized; }
            // ---- door (hinge pivot authored on the hinge side)
            var doorT = am.Part("PART_Door");
            if (doorT != null)
            {
                var dbW = LocalBoundsOf(doorT, doorT);
                var dc = doorT.TransformPoint(dbW.center);
                var bc = doorT.gameObject.AddComponent<BoxCollider>(); bc.center = dbW.center; bc.size = dbW.size + new Vector3(0, 0, 0.01f);
                var away = Vector3.ProjectOnPlane(dc - doorT.position, Vector3.up); var handle = dc + away.normalized * (away.magnitude * 0.8f) + Front * 0.025f;
                Door = doorT.gameObject.AddComponent<HingedPart>();
                float sign = HingedPart.OutwardSign(doorT, Vector3.up, dc, Front);
                Door.Init(Vector3.up, 105f * sign, handle, 0.075f); Door.Label = Kind + " door"; Door.Material = NoiseMaterial.Glass; Door.NoiseKey = 970 + All.Count;

            }
            // ---- owner screenshot 2026-11-10: the Blender split left a stair-stepped (saw-tooth) edge round the opening and on the door.
            //      A smooth rubber gasket covers the opening edge; the door gets a clean painted ring, see-through glass and a chunky handle.
            {
                var gk = new GameObject("gasket"); gk.transform.SetParent(am.Root, false); gk.layer = Layers.Furniture;
                // centred on the carved door opening (the door's closed centre), wide enough to cover its stair-stepped edge
                var hole = doorT != null ? root.InverseTransformPoint(doorT.TransformPoint(LocalBoundsOf(doorT, doorT).center)) : _drumC;
                float holeR = doorT != null ? Mathf.Max(dr, LocalBoundsOf(doorT, root).size.x * 0.5f) : dr;
                gk.transform.localPosition = new Vector3(hole.x, hole.y, zFront + 0.006f);
                gk.AddComponent<MeshFilter>().sharedMesh = Ring(dr - 0.012f, holeR + 0.03f, 0.035f, 64);
                var gr = gk.AddComponent<MeshRenderer>(); gr.sharedMaterial = Mats.Lit(new Color(0.30f, 0.32f, 0.40f), 0.35f); gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (doorT != null)
            {
                var dbW = LocalBoundsOf(doorT, doorT); var dc = doorT.TransformPoint(dbW.center);
                var ws = Vector3.Scale(dbW.size, doorT.lossyScale); var sizes = new[] { ws.x, ws.y, ws.z }; System.Array.Sort(sizes);
                float rD = sizes[1] * 0.5f;
                foreach (var r in doorT.GetComponentsInChildren<Renderer>()) r.enabled = false;
                Transform Piece(string n, Mesh m, Material mat, Vector3 at)
                {
                    var g = new GameObject(n); g.transform.SetParent(doorT, false); g.layer = Layers.Furniture;
                    g.transform.position = at; g.transform.rotation = Quaternion.LookRotation(Front, Vector3.up);
                    var ls = doorT.lossyScale; g.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
                    g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    return g.transform;
                }
                Piece("door_ring", Ring(rD * 0.64f, rD, 0.04f, 56), Mats.Lit(new Color(0.94f, 0.94f, 0.97f), 0.3f), dc + Front * 0.012f);
                Piece("door_ring_trim", Ring(rD * 0.6f, rD * 0.68f, 0.05f, 56), Mats.Lit(new Color(0.55f, 0.6f, 0.72f), 0.5f), dc + Front * 0.016f);
                Piece("door_glass", Disc(rD * 0.62f, 40), ToonWaterStream.WaterMaterial(new Color(0.7f, 0.86f, 0.98f, 0.28f), new Color(0.45f, 0.65f, 0.9f, 0.55f), 0f), dc - Front * 0.004f);
                var hd = Door.HandleWorld; var hb = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(hb.GetComponent<Collider>()); hb.name = "door_handle";
                hb.transform.SetParent(doorT, true); hb.transform.position = hd - Front * 0.01f; hb.transform.rotation = Quaternion.LookRotation(Front, Vector3.up);
                var ls2 = doorT.lossyScale; hb.transform.localScale = new Vector3(0.028f / ls2.x, 0.1f / ls2.y, 0.03f / ls2.z);
                hb.GetComponent<Renderer>().sharedMaterial = Mats.Lit(new Color(0.95f, 0.45f, 0.35f), 0.3f); hb.layer = Layers.Furniture;
            }
            // ---- detergent drawer (pivot = its front)
            var drawerT = am.Part("PART_DetergentDrawer");
            if (drawerT != null && Kind == "washer")
            {
                var b = LocalBoundsOf(drawerT, drawerT);
                // compartment floor in an UPRIGHT frame (the drawer's local Y is sideways in this Z-up-converted model)
                var floor = new GameObject("tray_floor"); floor.layer = Layers.Furniture; floor.transform.SetParent(drawerT, false); floor.transform.rotation = transform.rotation;
                var fbw = LocalBoundsOf(drawerT, transform); floor.transform.position = transform.TransformPoint(new Vector3(fbw.center.x, fbw.min.y, fbw.center.z));
                var fb = LocalBoundsOf(drawerT, floor.transform); var ls = floor.transform.lossyScale;
                var fc = floor.AddComponent<BoxCollider>(); fc.center = new Vector3(fb.center.x, fb.min.y + 0.004f / ls.y, fb.center.z); fc.size = new Vector3(fb.size.x, 0.008f / ls.y, fb.size.z);
                float depthW = Vector3.Project(drawerT.TransformVector(b.size), Front).magnitude, travel = depthW * 0.9f;   // pulls out to its rail stop: the compartment is exposed
                Drawer = drawerT.gameObject.AddComponent<SlidingPart>();
                Drawer.Init(Front, Mathf.Clamp(travel, 0.08f, 0.26f), drawerT.position + Front * 0.008f, 0.06f); Drawer.Label = "detergent drawer"; Drawer.NoiseKey = 980 + All.Count;
                float r = Mathf.Min(b.extents.x, b.extents.z) * 0.85f;
                // the compartment needs an UPRIGHT frame: this model was converted from Z-up, so the drawer's own local Y is not world up
                var tray = new GameObject("detergent_tray"); tray.transform.SetParent(drawerT, false); tray.transform.rotation = transform.rotation;
                var wb = LocalBoundsOf(drawerT, transform); tray.transform.position = transform.TransformPoint(new Vector3(wb.center.x, wb.min.y, wb.center.z));
                var tb = LocalBoundsOf(drawerT, tray.transform);
                Detergent = LiquidContainer.Add(tray, 0.12f, Mathf.Min(tb.extents.x, tb.extents.z) * 0.85f, tb.min.y + 0.004f, tb.max.y - 0.004f, 0f);
                Detergent.Tint = new Color(0.35f, 0.55f, 0.98f, 0.9f);
                _trayWidth = Mathf.Abs(Vector3.Dot(drawerT.TransformVector(b.size), transform.right)); _trayDepth = depthW;
            }
            // ---- programme dial and start button
            var dialT = am.Part("PART_Dial");
            if (dialT != null) { Dial = dialT.gameObject.AddComponent<RotaryKnob>(); Dial.Init(Front, 250f, Programmes.Length, dialT.position + Front * 0.012f, 0.045f); Dial.Label = "programme dial"; }
            var btnT = am.Part("PART_StartButton");
            if (btnT != null)
            {
                var b = LocalBoundsOf(btnT, btnT); float faceOut = Vector3.Dot(btnT.TransformPoint(b.max) - btnT.position, Front);
                StartButton = btnT.gameObject.AddComponent<MechButton>(); StartButton.Init(Front, Mathf.Max(0.004f, faceOut), Mathf.Max(0.012f, b.extents.x * btnT.lossyScale.x)); StartButton.Pressed += OnStartPressed;
            }
            // ---- motor hum (procedural, original)
            _hum = gameObject.AddComponent<AudioSource>(); _hum.clip = HumClip(); _hum.loop = true; _hum.spatialBlend = 1f; _hum.minDistance = 0.6f; _hum.maxDistance = 9f; _hum.volume = 0f; _hum.playOnAwake = false;
            _shakeBase = am.Root.localPosition;
            Debug.Log($"[SHHH][washer] {Kind} assembled: visible {vis.size:F3}, drum r {DrumRadius:0.000} depth {DrumDepth:0.000} at {_drumC:F3}; door {(Door != null)}, drawer {(Drawer != null)} travel {(Drawer != null ? Drawer.Travel : 0f):0.000}, dial {(Dial != null)}, start {(StartButton != null)}");
        }


        /// <summary>A point inside the drum for a slot (spawn of LD05's wet bundles, bots' release point).</summary>
        public Vector3 SlotPoint(int k) => DrumCentre - Vector3.up * DrumRadius * 0.45f + transform.right * ((k % 2 == 0 ? -1f : 1f) * DrumRadius * 0.3f) - Front * (DrumDepth * 0.15f * (k / 2));

        public bool InDrum(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world) - _drumC;
            return new Vector2(l.x, l.y).magnitude < DrumRadius - 0.01f && l.z > -DrumDepth * 0.5f && l.z < DrumDepth * 0.5f + 0.03f;
        }

        void OnStartPressed(MechButton b, int actor)
        {
            if (actor == ActorIds.None) actor = GameServices.LocalSlot;
            if (StartFixtures() == 0)
            {
                if (!_runLocal && (Door == null || Door.IsClosed)) { _runLocal = true; _starter = actor; }
                else if (_runLocal) _runLocal = false;
                else Refuse("close the door first");
                return;
            }
            foreach (var fv in Bound)
            {
                var st = fv.State; if (st == null || st.FlagNames == null || System.Array.IndexOf(st.FlagNames, "start") < 0) continue;
                bool on = st.Flags.Contains("start");
                var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Press, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1, Arg = on ? "!start" : "start" });
                if (err != null) Refuse(err); else _starter = actor;
            }
        }

        void Refuse(string why)
        {
            RejectedStarts++; LastStartError = why;
            if (AudioBank.I != null) AudioBank.I.Play("recipe_undone", StartButton != null ? StartButton.transform.position : transform.position, 0.4f);
            RoomFx.Say(transform.position + Vector3.up * 1.05f, why, Palette.Coral, 1.6f, 0.6f);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();   // MechHost: laundry physically inside the drum + resting = loaded
            // drum motion (kinematic ring + visible drum)
            float dt = Time.fixedDeltaTime; int prog = Dial != null ? Dial.Detent : 1;
            float rpm = Running ? (prog == 4 ? 110f : Kind == "dryer" ? 50f : 42f) : 0f;
            if (Running && rpm < 100f && Time.time > _dirAt) { _dirAt = Time.time + 7f; _dir = -_dir; }   // wash rhythm: reverse every 7 s
            _spinVel = Mathf.MoveTowards(_spinVel, rpm * 6f * (rpm < 100f ? _dir : 1f), 120f * dt);
            DrumAngle += _spinVel * dt;
            if (_ringRb != null) _ringRb.MoveRotation(transform.rotation * (Quaternion.AngleAxis(DrumAngle, Vector3.forward) * _ringBase));
            if (Drum != null) Drum.localRotation = Quaternion.AngleAxis(DrumAngle, _drumAxisLocal) * _drumRest;
        }

        /// <summary>Owner 2026-11-10 ("water is not positioning exactly in the drawer"): the detergent compartment that can catch a pour
        /// is the part of the drawer that is pulled OUT of the cabinet - its centre and radius follow the drawer's travel every frame
        /// (closed = nothing exposed, nothing caught; the cabinet top shields the rest).</summary>
        void PlaceTray()
        {
            if (Drawer == null || Detergent == null) return;
            float exposed = Mathf.Min(Drawer.Position, _trayDepth * 0.9f);
            var front = Drawer.HandleWorld - Front * 0.012f;
            Detergent.transform.position = new Vector3(0, Detergent.transform.position.y, 0) + Vector3.ProjectOnPlane(front - Front * (exposed * 0.5f), Vector3.up);
            float s = Mathf.Max(1e-4f, Detergent.transform.lossyScale.x);
            Detergent.InnerRadius = Mathf.Max(0.005f, Mathf.Min(exposed * 0.5f, _trayWidth * 0.5f) * 0.9f) / s;
        }

        protected override void Update()
        {
            base.Update();   // MechHost: the door follows the authoritative fixture opening
            PlaceTray();
            // replicated machine state: running = the start flag of any bound fixture; the door lock follows it on every peer
            bool run = false;
            foreach (var fv in Bound) { var st = fv.State; if (st != null && st.Flags.Contains("start")) run = true; }
            _runRemote = run;
            if (Door != null) Door.Locked = Running;
            float speed = Mathf.Abs(_spinVel) / 360f;   // rev/s
            if (_hum != null)
            {
                if (Running && !_hum.isPlaying) _hum.Play(); else if (!Running && speed < 0.02f && _hum.isPlaying) _hum.Stop();
                _hum.volume = Mathf.Clamp01(speed * 0.5f) * 0.6f * AudioBank.Master; _hum.pitch = 0.8f + speed * 0.35f;
            }
            if (Model != null && Model.Root != null)
            {
                float a = Running ? Mathf.Clamp01(speed) * 0.0025f : 0f;
                Model.Root.localPosition = _shakeBase + new Vector3(Mathf.Sin(Time.time * 37f) * a, Mathf.Abs(Mathf.Sin(Time.time * 23f)) * a * 0.5f, Mathf.Cos(Time.time * 31f) * a);
            }
            if (!Running) return;
            // detergent is drawn in during the wash; suds in the drum
            if (Detergent != null && Detergent.Amount > 0f) { float take = Mathf.Min(Detergent.Amount, 0.01f * Time.deltaTime); Detergent.Amount -= take; DetergentUsedL += take; }
            if (DetergentUsedL > 0.005f && Time.time > _sudsAt) { _sudsAt = Time.time + 1.4f; RoomFx.Burst("bubbles", DrumCentre + Front * 0.05f, 0.14f, 1.2f); }
            // the machine is audible: a low noise event every few seconds (Spin is loud), owned by whoever started it
            var round = GameServices.Round;
            if (GameServices.IsAuthority && round != null && _starter == ActorIds.None && round.Recipes != null)
            {
                // a client pressed START (its intent ran on the host): the machine's noise belongs to whoever the ledger says started it
                for (int i = round.Recipes.World.Ledger.Count - 1; i >= 0 && _starter == ActorIds.None; i--)
                { var e = round.Recipes.World.Ledger[i]; if (e.Verb == Verb.Press) foreach (var fv in Bound) if (fv.State != null && fv.State.Id == e.Fixture) _starter = e.Actor; }
            }
            if (GameServices.IsAuthority && round != null && round.IsPlayable && Time.time > _noiseAt && _starter != ActorIds.None)
            {
                _noiseAt = Time.time + 3f; int prog = Dial != null ? Dial.Detent : 1; var c = DrumCentre;
                if (round.SubmitOperate(_starter, 990 + All.IndexOf(this), prog == 4 ? 7f : 1.6f, new Vec3(c.x, c.y, c.z), false) != null) NoiseReports++;
            }
        }

        /// <summary>Flat annulus extruded backwards (front face at z = 0, toward local -Z by depth), smooth rounded look from many segments.</summary>
        static Mesh Ring(float rIn, float rOut, float depth, int n)
        {
            var v = new List<Vector3>(); var nm = new List<Vector3>(); var tr = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); nm.Add(na); nm.Add(nb); nm.Add(nb); nm.Add(na); tr.Add(i); tr.Add(i + 1); tr.Add(i + 2); tr.Add(i); tr.Add(i + 2); tr.Add(i + 3); }
            for (int k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n; var d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0); var d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0);
                var f = Vector3.forward; var bk = Vector3.back * depth;
                Quad(d0 * rIn, d0 * rOut, d1 * rOut, d1 * rIn, f, f);                                        // front face
                Quad(d0 * rOut, d0 * rOut + bk, d1 * rOut + bk, d1 * rOut, d0, d1);                           // outer wall
                Quad(d1 * rIn, d1 * rIn + bk, d0 * rIn + bk, d0 * rIn, -d1, -d0);                             // inner wall
            }
            var m = new Mesh { name = "washer_ring" }; m.SetVertices(v); m.SetNormals(nm); m.SetTriangles(tr, 0); m.RecalculateBounds(); return m;
        }

        static Mesh Disc(float r, int n)
        {
            var v = new List<Vector3> { Vector3.zero }; var tr = new List<int>(); var nm = new List<Vector3> { Vector3.forward };
            for (int k = 0; k < n; k++) { float a = k * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r); nm.Add(Vector3.forward); }
            for (int k = 0; k < n; k++) { tr.Add(0); tr.Add(1 + k); tr.Add(1 + (k + 1) % n); }
            var m = new Mesh { name = "washer_glass" }; m.SetVertices(v); m.SetNormals(nm); m.SetTriangles(tr, 0); m.RecalculateBounds(); return m;
        }

        static AudioClip _humClip;
        /// <summary>Original procedural motor hum: low fundamental + soft harmonics + slow water swish (2 s loop).</summary>
        static AudioClip HumClip()
        {
            if (_humClip != null) return _humClip;
            const int rate = 22050, n = rate * 2; var d = new float[n]; var rng = new System.Random(77); float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float hum = 0.32f * Mathf.Sin(2 * Mathf.PI * 50f * t) + 0.12f * Mathf.Sin(2 * Mathf.PI * 100f * t) + 0.05f * Mathf.Sin(2 * Mathf.PI * 150f * t);
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * 0.04f;                       // filtered noise = sloshing water
                float swish = lp * 0.9f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.5f * t));
                d[i] = (hum + swish) * 0.6f;
            }
            _humClip = AudioClip.Create("washer_hum", n, 1, rate, false); _humClip.SetData(d, 0); return _humClip;
        }
    }
}
