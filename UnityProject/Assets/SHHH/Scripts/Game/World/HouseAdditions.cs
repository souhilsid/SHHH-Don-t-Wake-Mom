using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V10: permanent house furniture fixes applied to the baked connected house (Resources/SHHH_WorldHouse) when a room is built - part of
    /// the house from the first frame of every match, never spawned by a task.
    /// <list type="bullet">
    /// <item><b>Kitchen sink run at a real worktop height.</b> The baked KayKit sink module was normalised by its total height including the
    /// faucet, which shrank it to a 0.51 m worktop / 0.36 m basin floor (knee height for a standing VR player). It is stretched vertically to a
    /// <see cref="RecipeLayout.WorktopY"/> worktop, keeping its 0.64 x 0.32 m basin footprint so a 24 cm plate still fits.</item>
    /// <item><b>Counters beside the sink</b> (KayKit straight counter, same family): fitted per axis into the real gaps sink-to-stove and
    /// sink-to-fridge with the sink's depth and worktop, so the dish rack, soap and sponge stand on furniture where people keep them
    /// (owner V10: no central task table).</item>
    /// </list>
    /// When the house is re-baked these should be baked in and this pass removed.
    /// </summary>
    public static class HouseAdditions
    {
        public const string SinkModel = "kitchencounter_sink_backsplash";
        /// <summary>V12 laundry-room furniture spots (room coordinates; the rules' RecipeLayout anchors use the same numbers).</summary>
        public static readonly Vector3 LaundryChest = new Vector3(2.45f, 0f, -0.55f), LaundryBin = new Vector3(2.52f, 0f, 0.15f);
        /// <summary>Old (baked) worktop of the sink module, measured with recipe_diag probe on 2026-11-10.</summary>
        public const float BakedSinkWorktop = 0.51f;
        /// <summary>Worktop / total height of the KayKit straight counter with backsplash (probe 2026-11-10: 0.77 m top at 0.92 m total).</summary>
        const float CounterTopRatio = 0.77f / 0.92f;

        public static int Build(RoomId active, Transform roomRoot, Transform house)
        {
            var root = new GameObject("V10_HouseAdditions").transform; root.SetParent(roomRoot, false);
            GameServices.Round?.Room?.Bins.Clear();   // V12: bins re-register as they are built
            int n = 0; var a = HouseLayout.Offset(active);
            // 1. the baked sink module, stretched to the worktop height - collider AND visible mesh.
            //    V10 rev 3 (owner screenshot: "the water and the ground water are a lot up"): the house bake MERGED the sink's visible mesh
            //    into shared per-material meshes (Batched_Reuse_*) that Unity also statically batches, so rescaling the sink object moved only
            //    its collider. Now the collider is scaled as before and the sink's own vertices inside those merged meshes get the same
            //    vertical stretch (selected by the sink's collision volume, short of the wall), shown by un-batched copies of the renderers.
            if (house != null)
                foreach (var t in house.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith(SinkModel) || t.GetComponentInParent<PropBody>() != null) continue;
                    if (t.parent != null && t.parent.name.StartsWith(SinkModel)) continue;
                    var cols = t.GetComponentsInChildren<Collider>(true); if (cols.Length == 0) continue;
                    Physics.SyncTransforms();
                    var box = cols[0].bounds; foreach (var c in cols) box.Encapsulate(c.bounds);
                    float floorY = box.min.y, k = RecipeLayout.WorktopY / BakedSinkWorktop;
                    // collider
                    t.localScale = new Vector3(t.localScale.x, t.localScale.y * k, t.localScale.z);
                    Physics.SyncTransforms();
                    var after = cols[0].bounds; foreach (var c in cols) after.Encapsulate(c.bounds); t.position += Vector3.up * (floorY - after.min.y);
                    // visible vertices: the sink's footprint, below its old top, not the wall behind it (room-local z < 2.985)
                    var sel = new Bounds(box.center, box.size - new Vector3(0.004f, 0f, 0.004f));
                    int meshes = StretchMerged(house, roomRoot, sel, floorY, box.max.y + 0.002f, k, 2.985f);
                    n++; Debug.Log($"[SHHH][house] V10 sink module stretched x{k:0.000} (worktop {BakedSinkWorktop:0.00} -> {RecipeLayout.WorktopY:0.00} m): collider + {meshes} merged visible mesh(es)");
                }
            // 2. counters beside the sink
            foreach (var r in HouseLayout.Rooms)
            {
                var def = RoomCatalog.Get(r); var o = HouseLayout.Offset(r);
                foreach (var f in def.Furniture)
                {
                    if (f.Kind != "counter_side") continue;
                    var pos = new Vector3(f.Position.x + o.x - a.x, f.Position.y + o.y - a.y, f.Position.z + o.z - a.z);
                    if (FitCounter("kr_kitchencounter_straight_A_backsplash", root, pos, f.Yaw, f.Size.x, RecipeLayout.WorktopY / CounterTopRatio, f.Size.z)) n++;
                }
            }
            // 3. V12: the laundry room's baked (static, solid) washer and dryer become working front-loaders with a real door, drum,
            //    detergent drawer, dial and start button (WasherAppliance). The baked shells are hidden in the merged meshes and their
            //    solid colliders are replaced by the appliance's own (frame + open drum), so laundry can really go inside.
            if (house != null) n += ReplaceAppliances(house, roomRoot, root, active);
            // 4. V12 laundry-room furniture with real moving parts (free floor measured with recipe_diag probe 2026-11-10: the right wall
            //    x 1.6..2.76, z -1.2..0.44 is clear): a chest of drawers (top drawer = towel cupboard, LD08) and a lidded bin (lint, LD09).
            if (house != null)
            {
                var lo = HouseLayout.Offset(RoomId.Laundry);
                Vector3 L(float x, float z) => new Vector3(x + lo.x - a.x, 0f, z + lo.z - a.z);
                if (MechFurniture.Build("chest", "uh_uh_drawers3_mech", root, L(LaundryChest.x, LaundryChest.z), -90f, 0.62f, 200) != null) n++;
                if (MechFurniture.Build("bin", "uh_uh_bin_mech", root, L(LaundryBin.x, LaundryBin.z), -90f, 0.36f, 210) != null) n++;
                // V12 trash: a lidded bin in the kitchen and in the living room too, on free floor along a wall (measured, not guessed)
                int bid = 212;
                foreach (var rm in new[] { RoomId.Kitchen, RoomId.LivingRoom })
                {
                    var o = HouseLayout.Offset(rm); var def = RoomCatalog.Get(rm);
                    if (FreeWallSpot(roomRoot, new Vector3(o.x - a.x, 0, o.z - a.z), def.Width, def.Depth, out var spot, out var yaw) && MechFurniture.Build("bin", "uh_uh_bin_mech", root, root.InverseTransformPoint(spot), yaw - root.eulerAngles.y, 0.36f, bid) != null) { n++; bid += 2; }
                }
            }
            if (n > 0) Debug.Log($"[SHHH][house] V10 additions: {n} pieces (sink run fitted to a {RecipeLayout.WorktopY:0.00} m worktop)");
            return n;
        }

        /// <summary>First free 0.45 x 0.6 x 0.45 floor spot along the walls of a room (inset 0.32 m), front toward the room centre.</summary>
        static bool FreeWallSpot(Transform roomRoot, Vector3 centreLocal, float w, float d, out Vector3 spot, out float yaw)
        {
            Physics.SyncTransforms(); spot = default; yaw = 0f; var c = roomRoot.TransformPoint(centreLocal);
            for (int side = 0; side < 4; side++)
                for (float t = -0.35f; t <= 0.35f; t += 0.07f)
                {
                    var local = side == 0 ? new Vector3(t * w, 0, d * 0.5f - 0.32f) : side == 1 ? new Vector3(w * 0.5f - 0.32f, 0, t * d) : side == 2 ? new Vector3(t * w, 0, -d * 0.5f + 0.32f) : new Vector3(-w * 0.5f + 0.32f, 0, t * d);
                    var p = roomRoot.TransformPoint(centreLocal + local);
                    if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 2f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore) || hit.point.y > c.y + 0.04f) continue;
                    if (Physics.CheckBox(new Vector3(p.x, hit.point.y + 0.36f, p.z), new Vector3(0.24f, 0.3f, 0.24f), Quaternion.identity, Layers.EnvironmentMask | Layers.PropsMask, QueryTriggerInteraction.Ignore)) continue;
                    spot = new Vector3(p.x, hit.point.y, p.z); var to = c - spot; to.y = 0; yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; return true;
                }
            return false;
        }

        static int ReplaceAppliances(Transform house, Transform roomRoot, Transform root, RoomId active)
        {
            var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Collider>>();
            foreach (var c in house.GetComponentsInChildren<Collider>(true))
            {
                if (c.GetComponentInParent<PropBody>() != null) continue;
                string k = c.name.Contains("washing_machine") ? "washer" : c.name.Contains("dryer") ? "dryer" : c.name.ToLowerInvariant().Contains("fridge") ? "fridge" : null;
                if (k == null) continue;
                if (!groups.TryGetValue(k, out var l)) groups[k] = l = new System.Collections.Generic.List<Collider>();
                l.Add(c);
            }
            if (groups.Count == 0) return 0;
            Physics.SyncTransforms();
            var boxes = new System.Collections.Generic.Dictionary<string, Bounds>();
            foreach (var kv in groups) { var b = kv.Value[0].bounds; foreach (var c in kv.Value) b.Encapsulate(c.bounds); boxes[kv.Key] = b; foreach (var c in kv.Value) c.enabled = false; }
            Physics.SyncTransforms();
            int built = 0;
            foreach (var kv in boxes)
            {
                var box = kv.Value;
                if (kv.Key == "fridge") { if (BuildFridge(house, roomRoot, root, active, box)) built++; else foreach (var c in groups[kv.Key]) c.enabled = true; continue; }
                // V12 fix (owner screenshot 2026-11-10: "it is in the wrong direction"): the nearest-wall raycast picked the room side and
                // the machines showed their backs. The front now faces the centre of the room the appliance stands in (laundry room centre),
                // snapped to the dominant axis - deterministic, independent of other colliders.
                var o = HouseLayout.Offset(RoomId.Laundry); var aOff = HouseLayout.Offset(active);
                var roomCentre = roomRoot.TransformPoint(new Vector3(o.x - aOff.x, 0f, o.z - aOff.z));
                var toC = roomCentre - box.center; toC.y = 0f;
                var back = Mathf.Abs(toC.x) > Mathf.Abs(toC.z) ? new Vector3(-Mathf.Sign(toC.x), 0, 0) : new Vector3(0, 0, -Mathf.Sign(toC.z));
                var front = -back; float yawWorld = Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg;
                int hidden = HideMerged(house, box);
                var floor = root.InverseTransformPoint(new Vector3(box.center.x, box.min.y, box.center.z));
                var wa = WasherAppliance.Build(kv.Key, root, floor, yawWorld - root.eulerAngles.y, Mathf.Max(box.size.x, box.size.z) * 0.95f);
                if (wa == null) { Debug.LogWarning($"[SHHH][house] V12 {kv.Key}: model {WasherAppliance.ModelKey} missing - baked appliance restored"); foreach (var c in groups[kv.Key]) c.enabled = true; continue; }
                built++;
                int idBase = kv.Key == "washer" ? 220 : 230;   // house mechanism ids (replicated when no recipe drives them)
                if (wa.Door != null) { wa.Door.HouseId = idBase; wa.Door.gameObject.AddComponent<HouseMechSync>(); }
                if (wa.Drawer != null) { wa.Drawer.HouseId = idBase + 1; wa.Drawer.gameObject.AddComponent<HouseMechSync>(); }
                if (wa.Dial != null) { wa.Dial.HouseId = idBase + 2; wa.Dial.gameObject.AddComponent<HouseMechSync>(); }
                Debug.Log($"[SHHH][house] V12 {kv.Key}: baked shell {box.size:F2} at room {roomRoot.InverseTransformPoint(box.center):F2} replaced (front {front}, {hidden} merged meshes hidden)");
            }
            return built;
        }

        /// <summary>
        /// V13 (owner 2026-11-17: "putting the food in the fridge does not exist"): the baked kitchen fridge (a solid shell) is hidden and the
        /// stylized working fridge (sk_fridge: hollow, shelves, hinged door) stands in its place, front toward the kitchen. It is the KT17
        /// "fridge" fixture's host and a house mechanism (door replicated) the rest of the time.
        /// </summary>
        static bool BuildFridge(Transform house, Transform roomRoot, Transform root, RoomId active, Bounds box)
        {
            var o = HouseLayout.Offset(RoomId.Kitchen); var aOff = HouseLayout.Offset(active);
            // front = the kitchen layout's fridge yaw (its door faces -z); "toward the room centre" pointed it at a counter
            float fy = 180f; foreach (var fu in RoomCatalog.Get(RoomId.Kitchen).Furniture) if (fu.Kind == "fridge") { fy = fu.Yaw; break; }
            var front = roomRoot.TransformDirection(Quaternion.Euler(0f, fy, 0f) * Vector3.forward); front.y = 0f; front.Normalize();
            float yawWorld = Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg;
            int hidden = HideMerged(house, box);
            var floor = root.InverseTransformPoint(new Vector3(box.center.x, box.min.y, box.center.z));
            // the model is 1.80 m tall and 0.817 m deep with its handle (its largest horizontal size, which the fit uses): as tall as the old one
            float fit = Mathf.Clamp(box.size.y * (0.817f / 1.80f), 0.6f, 0.95f);
            var f = MechFurniture.Build("fridge", "sk_fridge", root, floor, yawWorld - root.eulerAngles.y, fit, 240);
            if (f == null) { Debug.LogWarning("[SHHH][house] V13 fridge: model sk_fridge missing - baked fridge restored"); return false; }
            var inRoom = roomRoot.InverseTransformPoint(box.center);
            Debug.Log($"[SHHH][house] V13 fridge: baked shell {box.size:F2} at room ({inRoom.x + aOff.x - o.x:F2}, {inRoom.z + aOff.z - o.z:F2}) (kitchen coords) replaced, front {front}, {hidden} merged meshes hidden");
            return true;
        }

        /// <summary>Collapse the vertices of every merged house mesh that lie inside a world box (above its floor) to one point, so the
        /// static shell there disappears (degenerate triangles) while the rest of the merged mesh stays; un-batched copies as for the sink.</summary>
        static int HideMerged(Transform house, Bounds box)
        {
            // owner screenshot 2026-11-10 ("grey thing"): a box shrunk by 4 mm kept the shell's outer faces (they lie ON the collider box).
            // Reach 12 mm beyond it (the neighbouring appliance is 9 cm away, the wall 30 cm), keep the floor out by height.
            int changed = 0; var inner = new Bounds(box.center, box.size + new Vector3(0.024f, 0.02f, 0.024f));
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.enabled || !mr.bounds.Intersects(inner)) continue;
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (inner.Contains(mr.bounds.min) && inner.Contains(mr.bounds.max)) { mr.enabled = false; changed++; continue; }   // a separate renderer of the shell
                var src = mf.sharedMesh; if (!src.isReadable) continue;
                var v = src.vertices; int moved = 0; var tr = mf.transform; var sink = tr.InverseTransformPoint(box.center);
                for (int i = 0; i < v.Length; i++)
                {
                    var w = tr.TransformPoint(v[i]);
                    if (w.x < inner.min.x || w.x > inner.max.x || w.z < inner.min.z || w.z > inner.max.z || w.y < box.min.y + 0.003f || w.y > box.max.y + 0.012f) continue;
                    v[i] = sink; moved++;
                }
                if (moved == 0) continue;
                var copy = Object.Instantiate(src); copy.name = src.name + "_V12app"; copy.vertices = v; copy.RecalculateBounds();
                var g = new GameObject(mr.name + "_V12"); g.transform.SetParent(tr.parent, false);
                g.transform.SetPositionAndRotation(tr.position, tr.rotation); g.transform.localScale = tr.localScale; g.layer = mr.gameObject.layer;
                g.AddComponent<MeshFilter>().sharedMesh = copy;
                var nr = g.AddComponent<MeshRenderer>(); nr.sharedMaterials = mr.sharedMaterials;
                nr.lightmapIndex = mr.lightmapIndex; nr.lightmapScaleOffset = mr.lightmapScaleOffset; nr.shadowCastingMode = mr.shadowCastingMode; nr.receiveShadows = mr.receiveShadows; nr.lightProbeUsage = mr.lightProbeUsage;
                mr.enabled = false; changed++;
                Debug.Log($"[SHHH][house] V12 appliance shell: {moved}/{v.Length} vertices of {src.name} collapsed");
            }
            return changed;
        }

        /// <summary>Stretch, in every merged house mesh, the vertices inside a world box (x/z footprint, y in [floor, top]) vertically about the
        /// floor; the renderer is replaced by an un-batched copy (same materials and baked lightmap). Returns the number of meshes changed.</summary>
        static int StretchMerged(Transform house, Transform roomRoot, Bounds box, float floorY, float topY, float k, float maxRoomZ)
        {
            int changed = 0;
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.enabled || !mr.bounds.Intersects(box)) continue;
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var src = mf.sharedMesh;
                if (!src.isReadable) { Debug.LogWarning($"[SHHH][house] merged mesh {src.name} is not CPU-readable - the visible sink cannot be stretched (enable Read/Write on the house bake)"); continue; }
                var v = src.vertices; int moved = 0; var tr = mf.transform;
                for (int i = 0; i < v.Length; i++)
                {
                    var w = tr.TransformPoint(v[i]);
                    if (w.x < box.min.x || w.x > box.max.x || w.z < box.min.z || w.z > box.max.z || w.y < floorY - 0.001f || w.y > topY) continue;
                    if (roomRoot.InverseTransformPoint(w).z > maxRoomZ) continue;
                    w.y = floorY + (w.y - floorY) * k; v[i] = tr.InverseTransformPoint(w); moved++;
                }
                if (moved == 0) continue;
                var copy = Object.Instantiate(src); copy.name = src.name + "_V10sink"; copy.vertices = v; copy.RecalculateBounds();
                var g = new GameObject(mr.name + "_V10"); g.transform.SetParent(tr.parent, false);
                g.transform.SetPositionAndRotation(tr.position, tr.rotation); g.transform.localScale = tr.localScale; g.layer = mr.gameObject.layer;
                g.AddComponent<MeshFilter>().sharedMesh = copy;
                var nr = g.AddComponent<MeshRenderer>(); nr.sharedMaterials = mr.sharedMaterials;
                nr.lightmapIndex = mr.lightmapIndex; nr.lightmapScaleOffset = mr.lightmapScaleOffset; nr.shadowCastingMode = mr.shadowCastingMode; nr.receiveShadows = mr.receiveShadows;
                nr.lightProbeUsage = mr.lightProbeUsage;
                mr.enabled = false; changed++;
                Debug.Log($"[SHHH][house] V10 sink stretch: {moved}/{v.Length} vertices of {src.name} ({mr.name})");
            }
            return changed;
        }

        /// <summary>Instantiate a catalog model and fit it per axis into a w x h x d box standing at pos (yaw 0/90/180/270), with mesh colliders.</summary>
        static bool FitCounter(string key, Transform parent, Vector3 pos, float yaw, float w, float h, float d, string name = "F_counter_side")
        {
            var model = ArtCatalog.I != null ? ArtCatalog.I.Model(key) : null; if (model == null) { Debug.LogWarning("[SHHH][house] missing " + key); return false; }
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var inst = Object.Instantiate(model, g.transform);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Prim.Remove(c);
            foreach (var an in inst.GetComponentsInChildren<Animator>()) Prim.Remove(an);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
            Mats.MakeUrpSafe(inst);
            var b = LocalBounds(inst.transform, g.transform);
            inst.transform.localScale = new Vector3(w / Mathf.Max(1e-4f, b.size.x), h / Mathf.Max(1e-4f, b.size.y), d / Mathf.Max(1e-4f, b.size.z));
            b = LocalBounds(inst.transform, g.transform);
            inst.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            Prim.SetLayerRecursive(g, Layers.Furniture);
            return true;
        }

        static Bounds LocalBounds(Transform t, Transform space)
        {
            bool any = false; var b = new Bounds();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = space.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }
    }
}
