using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 house furniture with real moving parts, built from the split Quaternius "Ultimate House" models (CC0; Tools/blender/split_parts.py):
    /// <list type="bullet">
    /// <item><b>chest</b> (uh_uh_drawers3_mech): three drawers on rails (<see cref="SlidingPart"/>), each a real open box (floor, walls,
    /// front) that carries what is in it; the top drawer is the LD08 towel cupboard.</item>
    /// <item><b>bin</b> (uh_uh_bin_mech): a hollow can (floor + walls + dark liner) with a hinged lid (<see cref="HingedPart"/>, axis
    /// along the back rim); the LD09 lint bin - lint only goes in with the lid open.</item>
    /// <item><b>fridge</b> (uh_uh_fridge_mech): two hinged doors (house mechanism, no recipe yet).</item>
    /// </list>
    /// Every collider is built in the furniture's UPRIGHT frame (these models were converted from Z-up: a part's own local Y is not up -
    /// the V12 detergent-drawer lesson). Parts replicate as house mechanisms (<see cref="HouseMechSync"/>) unless a recipe binds them.
    /// </summary>
    public sealed class MechFurniture : MechHost
    {
        public static readonly List<MechFurniture> All = new List<MechFurniture>();
        public string Kind;
        public ArticulatedModel Model;
        public readonly List<SlidingPart> Drawers = new List<SlidingPart>();
        public readonly List<HingedPart> Doors = new List<HingedPart>();
        public HingedPart Lid;
        readonly List<Bounds> _drawerBox = new List<Bounds>();   // closed drawer boxes in this frame
        Bounds _vis, _binInside, _fridgeInside;
        readonly List<Transform> _fridgeSlots = new List<Transform>();
        /// <summary>V13: the new stylized fridge (sk_fridge) - hollow compartment, two shelves, one hinged door - not the old two-door shell.</summary>
        public bool IsFoodFridge => Kind == "fridge" && _fridgeSlots.Count > 0;

        protected override void OnEnable() { base.OnEnable(); All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        public static MechFurniture Build(string kind, string key, Transform parent, Vector3 floorLocal, float yaw, float fitExtent, int houseIdBase)
        {
            var g = new GameObject("V12_" + kind); g.transform.SetParent(parent, false);
            g.transform.localPosition = floorLocal; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var am = ArticulatedModel.Build(key, g.transform, kind + "_model", fitExtent, Layers.Furniture);
            if (!am.Ok) { Destroy(g); return null; }
            var f = g.AddComponent<MechFurniture>(); f.Kind = kind; f.Model = am; f.Assemble(houseIdBase);
            return f;
        }

        // ---- geometry helpers (frame = this furniture's upright transform)
        static Bounds BoundsIn(Transform t, Transform space)
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
        static Bounds MinMax(Vector3 a, Vector3 b) { var r = new Bounds(); r.SetMinMax(Vector3.Min(a, b), Vector3.Max(a, b)); return r; }

        /// <summary>A box collider given in this frame, parented to <paramref name="parent"/> (so it moves with a drawer / lid).</summary>
        BoxCollider UprightBox(Transform parent, string name, Bounds b)
        {
            var g = new GameObject(name); g.layer = Layers.Furniture; g.transform.SetParent(parent, false);
            g.transform.rotation = transform.rotation; g.transform.position = transform.TransformPoint(b.center);
            float s = Mathf.Max(1e-4f, parent.lossyScale.x); g.transform.localScale = Vector3.one / s;
            var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(Mathf.Max(0.004f, b.size.x), Mathf.Max(0.004f, b.size.y), Mathf.Max(0.004f, b.size.z));
            return bc;
        }

        void Sync(MechPart p, int id) { p.HouseId = id; p.gameObject.AddComponent<HouseMechSync>(); }

        void Assemble(int idBase)
        {
            var root = transform; _vis = BoundsIn(Model.Root, root); var v = _vis; const float T = 0.014f;
            var body = new GameObject("body_colliders").transform; body.SetParent(root, false);
            if (Kind == "chest")
            {
                var parts = new List<Transform>();
                for (int i = 0; i < 6; i++) { var p = Model.Part("PART_Drawer" + i); if (p != null) parts.Add(p); }
                parts.Sort((a, b) => BoundsIn(a, root).center.y.CompareTo(BoundsIn(b, root).center.y));
                float dMinZ = v.max.z, dMinX = v.max.x, dMaxX = v.min.x, dMinY = v.max.y, dMaxY = v.min.y;
                foreach (var p in parts) { var db = BoundsIn(p, root); _drawerBox.Add(db); dMinZ = Mathf.Min(dMinZ, db.min.z); dMinX = Mathf.Min(dMinX, db.min.x); dMaxX = Mathf.Max(dMaxX, db.max.x); dMinY = Mathf.Min(dMinY, db.min.y); dMaxY = Mathf.Max(dMaxY, db.max.y); }
                // carcass: back, sides, top, bottom around the drawer stack
                UprightBox(body, "back", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(v.max.x, v.max.y, Mathf.Max(v.min.z + T, dMinZ - 0.004f))));
                UprightBox(body, "side_l", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(Mathf.Max(v.min.x + T, dMinX - 0.003f), v.max.y, v.max.z - 0.01f)));
                UprightBox(body, "side_r", MinMax(new Vector3(Mathf.Min(v.max.x - T, dMaxX + 0.003f), v.min.y, v.min.z), new Vector3(v.max.x, v.max.y, v.max.z - 0.01f)));
                UprightBox(body, "top", MinMax(new Vector3(v.min.x, Mathf.Min(v.max.y - T, dMaxY + 0.003f), v.min.z), new Vector3(v.max.x, v.max.y, v.max.z)));
                UprightBox(body, "bottom", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(v.max.x, Mathf.Max(v.min.y + T, dMinY - 0.003f), v.max.z)));
                for (int i = 0; i < parts.Count; i++)
                {
                    var p = parts[i]; var db = _drawerBox[i];
                    // an open box that rides with the drawer: floor, two sides, back, front panel
                    UprightBox(p, "d_floor", MinMax(new Vector3(db.min.x, db.min.y, db.min.z), new Vector3(db.max.x, db.min.y + 0.012f, db.max.z)));
                    UprightBox(p, "d_left", MinMax(new Vector3(db.min.x, db.min.y, db.min.z), new Vector3(db.min.x + 0.012f, db.max.y - 0.02f, db.max.z)));
                    UprightBox(p, "d_right", MinMax(new Vector3(db.max.x - 0.012f, db.min.y, db.min.z), new Vector3(db.max.x, db.max.y - 0.02f, db.max.z)));
                    UprightBox(p, "d_back", MinMax(new Vector3(db.min.x, db.min.y, db.min.z), new Vector3(db.max.x, db.max.y - 0.02f, db.min.z + 0.012f)));
                    UprightBox(p, "d_front", MinMax(new Vector3(db.min.x, db.min.y, db.max.z - 0.02f), new Vector3(db.max.x, db.max.y, db.max.z)));
                    var sp = p.gameObject.AddComponent<SlidingPart>();
                    var handle = root.TransformPoint(new Vector3(db.center.x, db.center.y + db.extents.y * 0.25f, db.max.z + 0.01f));
                    sp.Init(Front, db.size.z * 0.72f, handle, Mathf.Min(0.12f, db.size.x * 0.4f)); sp.Label = "drawer " + i; sp.NoiseKey = 960 + idBase + i; sp.Material = SHHH.Core.NoiseMaterial.Wood;
                    Drawers.Add(sp); Sync(sp, idBase + i);
                }
            }
            else if (Kind == "bin")
            {
                var lidT = Model.Part("PART_Lid"); var lb = lidT != null ? BoundsIn(lidT, root) : new Bounds(new Vector3(0, v.max.y, 0), new Vector3(v.size.x, 0.02f, v.size.z));
                float top = lb.min.y;
                UprightBox(body, "floor", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(v.max.x, v.min.y + T, v.max.z)));
                UprightBox(body, "wall_b", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(v.max.x, top, v.min.z + T)));
                UprightBox(body, "wall_f", MinMax(new Vector3(v.min.x, v.min.y, v.max.z - T), new Vector3(v.max.x, top, v.max.z)));
                UprightBox(body, "wall_l", MinMax(new Vector3(v.min.x, v.min.y, v.min.z), new Vector3(v.min.x + T, top, v.max.z)));
                UprightBox(body, "wall_r", MinMax(new Vector3(v.max.x - T, v.min.y, v.min.z), new Vector3(v.max.x, top, v.max.z)));
                _binInside = MinMax(new Vector3(v.min.x + T, v.min.y + T, v.min.z + T), new Vector3(v.max.x - T, top, v.max.z - T));
                // a dark painted liner so the open can is hollow-looking (the imported shell is single-sided)
                var ln = new GameObject("liner"); ln.transform.SetParent(Model.Root, false); ln.transform.position = root.TransformPoint(new Vector3(_binInside.center.x, _binInside.min.y, _binInside.center.z)); ln.transform.rotation = root.rotation;
                float lr = Mathf.Min(_binInside.extents.x, _binInside.extents.z) * 1.02f; float s0 = Mathf.Max(1e-4f, Model.Root.lossyScale.x); ln.transform.localScale = Vector3.one / s0;
                ln.AddComponent<MeshFilter>().sharedMesh = Liner(lr, _binInside.size.y, 28);
                var lmr = ln.AddComponent<MeshRenderer>(); lmr.sharedMaterial = Mats.Lit(new Color(0.22f, 0.24f, 0.3f), 0.1f); lmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (lidT != null)
                {
                    UprightBox(lidT, "lid_box", lb);
                    var right = root.right; var lc = root.TransformPoint(lb.center);
                    Lid = lidT.gameObject.AddComponent<HingedPart>();
                    float sign = HingedPart.OutwardSign(lidT, right, lc, Vector3.up);
                    Lid.Init(right, 100f * sign, lc + Front * (lb.extents.z * 0.85f) + Vector3.up * 0.015f, 0.09f); Lid.Label = "bin lid"; Lid.NoiseKey = 960 + idBase; Lid.Material = SHHH.Core.NoiseMaterial.Plastic;
                    // V13: the whole front rim is the handle (players grab the lid where their hand is, not one exact point)
                    Lid.HandleSpanLocal = lidT.InverseTransformVector(right * lb.extents.x * 0.85f);
                    Lid.SoftCloseZone = 4f; Sync(Lid, idBase);
                    Lid.Grabbed += (m, a) => ClaimBinFor(a);
                }
            }
            else if (Kind == "fridge" && Model.Part("PART_Door") != null)
            {
                // V13 (owner 2026-11-17: "putting the food in the fridge does not exist, so do it ... model the fridge"): Tools/blender/
                // make_fridge_food.py. The shell, the two shelves and the door carry authored COL_* boxes (ArticulatedModel turns them into
                // colliders, the door's ride with it), so food really rests on the shelves and the closed door holds it in.
                var dt = Model.Part("PART_Door"); var db = BoundsIn(dt, root);
                var dc = root.TransformPoint(db.center);
                var h = dt.gameObject.AddComponent<HingedPart>(); float sign = HingedPart.OutwardSign(dt, Vector3.up, dc, Front);
                // the handle bar runs up the door's free edge (the side away from the hinge), at grabbing height
                var piv = root.InverseTransformPoint(dt.position);
                float hx = Mathf.Abs(db.min.x - piv.x) > Mathf.Abs(db.max.x - piv.x) ? db.min.x + 0.075f : db.max.x - 0.075f;
                var handle = root.TransformPoint(new Vector3(hx, db.min.y + db.size.y * 0.58f, db.max.z + 0.02f));
                h.Init(Vector3.up, 115f * sign, handle, 0.09f); h.Label = "fridge door"; h.NoiseKey = 960 + idBase; h.SlamSpeed = 130f;
                h.HandleSpanLocal = dt.InverseTransformVector(Vector3.up * db.size.y * 0.22f);   // grab the bar anywhere along it
                h.Material = SHHH.Core.NoiseMaterial.Appliance; h.SoftCloseZone = 6f;
                Doors.Add(h); Sync(h, idBase);
                for (int k = 0; k < 8; k++) { var s = Model.Part("SOCKET_Slot" + k); if (s != null) _fridgeSlots.Add(s); }
                // the job's places are the two shelves first (easy to reach at chest height); the bottom of the fridge is overflow
                _fridgeSlots.Sort((p, q) => { var a2 = root.InverseTransformPoint(p.position); var b3 = root.InverseTransformPoint(q.position);
                                             int c = Mathf.Abs(a2.y - 0.5f).CompareTo(Mathf.Abs(b3.y - 0.5f)) == 0 ? 0 : (a2.y < 0.3f ? 1 : 0) - (b3.y < 0.3f ? 1 : 0);
                                             return c != 0 ? c : (Mathf.Abs(a2.y - b3.y) > 0.05f ? b3.y.CompareTo(a2.y) : a2.x.CompareTo(b3.x)); });
                var a = Model.Part("SOCKET_InsideMin"); var b2 = Model.Part("SOCKET_InsideMax");
                if (a != null && b2 != null) _fridgeInside = MinMax(root.InverseTransformPoint(a.position), root.InverseTransformPoint(b2.position));
            }
            else if (Kind == "fridge")
            {
                float doorMinZ = v.max.z; int i = 0;
                foreach (var n in new[] { "PART_DoorLow", "PART_DoorHigh" })
                {
                    var dt = Model.Part(n); if (dt == null) continue; var db = BoundsIn(dt, root); doorMinZ = Mathf.Min(doorMinZ, db.min.z);
                    UprightBox(dt, "door_box", db);
                    var dc = root.TransformPoint(db.center); var away = Vector3.ProjectOnPlane(dc - dt.position, Vector3.up);
                    var h = dt.gameObject.AddComponent<HingedPart>(); float sign = HingedPart.OutwardSign(dt, Vector3.up, dc, Front);
                    h.Init(Vector3.up, 110f * sign, dc + away.normalized * (away.magnitude * 0.85f) + Front * 0.03f, 0.1f); h.Label = "fridge " + n; h.NoiseKey = 960 + idBase + i; h.SlamSpeed = 120f;
                    Doors.Add(h); Sync(h, idBase + i); i++;
                }
                UprightBox(body, "cabinet", MinMax(v.min, new Vector3(v.max.x, v.max.y, Mathf.Max(v.min.z + 0.05f, doorMinZ - 0.004f))));
            }
            if (Kind == "bin")
            {
                // V12: tell the bots where this bin's opening is (house coordinates of the round's room; host-only use)
                var round = GameServices.Round; var rr = GetComponentInParent<RoomRuntime>();
                if (round != null && round.Room != null && rr != null) round.Room.Bins.Add(VisualFactory.C(rr.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(_binInside.center.x, _binInside.max.y, _binInside.center.z)))));
            }
            Debug.Log($"[SHHH][furniture] {Kind} assembled: visible {v.size:F3}; drawers {Drawers.Count}, doors {Doors.Count}, lid {(Lid != null)}");
        }

        /// <summary>Inward-facing open cylinder + floor (a hollow can's inside).</summary>
        static Mesh Liner(float r, float h, int n)
        {
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var tr = new List<int>();
            for (int k = 0; k <= n; k++) { float a = k * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); vs.Add(d * r); vs.Add(d * r + Vector3.up * h); ns.Add(-d); ns.Add(-d); }
            for (int k = 0; k < n; k++) { int i = k * 2; tr.Add(i); tr.Add(i + 2); tr.Add(i + 1); tr.Add(i + 1); tr.Add(i + 2); tr.Add(i + 3); }
            int c = vs.Count; vs.Add(Vector3.up * 0.002f); ns.Add(Vector3.up);
            for (int k = 0; k <= n; k++) { float a = k * Mathf.PI * 2f / n; vs.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + Vector3.up * 0.002f); ns.Add(Vector3.up); }
            for (int k = 0; k < n; k++) { tr.Add(c); tr.Add(c + 2 + k); tr.Add(c + 1 + k); }
            var m = new Mesh { name = "bin_liner" }; m.SetVertices(vs); m.SetNormals(ns); m.SetTriangles(tr, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>V12 trash: is a world point inside this bin's can (any bin, not only the lint bin)?</summary>
        public bool InsideBin(Vector3 world)
        {
            if (Kind != "bin" || _binInside.size.sqrMagnitude <= 0f) return false; var l = transform.InverseTransformPoint(world);
            return l.x > _binInside.min.x && l.x < _binInside.max.x && l.z > _binInside.min.z && l.z < _binInside.max.z && l.y > _binInside.min.y - 0.01f && l.y < _binInside.max.y + 0.02f;
        }

        // ---- MechHost: chest top drawer = towel cupboard (LD08), bin = lint bin (LD09)
        public override bool Serves(string k) => (Kind == "chest" && k == "towel_cupboard") || (Kind == "bin" && k == "lint_bin") || (IsFoodFridge && k == "fridge");
        public override MechPart DoorFor(string k) => Kind == "chest" && Drawers.Count > 0 ? Drawers[Drawers.Count - 1] : IsFoodFridge && Doors.Count > 0 ? Doors[0] : null;
        public override Vector3 Focus => Kind == "chest" && Drawers.Count > 0 ? Drawers[Drawers.Count - 1].HandleWorld
            : IsFoodFridge ? transform.TransformPoint(new Vector3(_fridgeInside.center.x, _fridgeInside.center.y, _vis.max.z))
            : transform.TransformPoint(new Vector3(_vis.center.x, _vis.max.y, _vis.center.z));
        /// <summary>Food goes into the fridge through its front (like the washer drum), not from above.</summary>
        public override Vector3 LoadFrom(string k) => IsFoodFridge ? Front : Vector3.up;

        public override Vector3 SlotPoint(string k, int slot)
        {
            if (IsFoodFridge) return _fridgeSlots[((slot % _fridgeSlots.Count) + _fridgeSlots.Count) % _fridgeSlots.Count].position + Vector3.up * 0.01f;
            if (Kind == "chest" && Drawers.Count > 0)
            {
                var db = _drawerBox[_drawerBox.Count - 1]; var d = Drawers[Drawers.Count - 1]; int n = 3;
                float pitch = Mathf.Min(0.16f, (db.size.x - 0.04f) / n), x = db.center.x + (slot - (n - 1) * 0.5f) * pitch;
                return transform.TransformPoint(new Vector3(x, db.min.y + 0.02f, db.center.z + d.Position));
            }
            return transform.TransformPoint(new Vector3(_binInside.center.x + (slot % 2 == 0 ? -0.03f : 0.03f), _binInside.min.y + 0.02f, _binInside.center.z));
        }

        public override bool Inside(string k, Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            if (IsFoodFridge) return _fridgeInside.size.sqrMagnitude > 0f && l.x > _fridgeInside.min.x && l.x < _fridgeInside.max.x && l.y > _fridgeInside.min.y - 0.01f && l.y < _fridgeInside.max.y
                                      && l.z > _fridgeInside.min.z && l.z < _fridgeInside.max.z;
            if (Kind == "chest" && Drawers.Count > 0)
            {
                var db = _drawerBox[_drawerBox.Count - 1]; l.z -= Drawers[Drawers.Count - 1].Position;
                return l.x > db.min.x + 0.01f && l.x < db.max.x - 0.01f && l.z > db.min.z + 0.01f && l.z < db.max.z - 0.015f && l.y > db.min.y - 0.01f && l.y < db.max.y + 0.06f;
            }
            return _binInside.size.sqrMagnitude > 0f && l.x > _binInside.min.x && l.x < _binInside.max.x && l.z > _binInside.min.z && l.z < _binInside.max.z && l.y > _binInside.min.y - 0.01f && l.y < _binInside.max.y + 0.02f;
        }
    
        /// <summary>V12: a sibling took this bin's lid (local hand, or a client's lid move seen by the host): bots leave this bin to them.</summary>
        public void ClaimBinFor(int actor)
        {
            var round = GameServices.Round; var rr = GetComponentInParent<RoomRuntime>();
            if (Kind != "bin" || round == null || rr == null || !SHHH.Core.ActorIds.IsSibling(actor)) return;
            round.ClaimBin(actor, VisualFactory.C(rr.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(_binInside.center.x, _binInside.max.y, _binInside.center.z)))));
        }
    }
}
