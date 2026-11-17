using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V9 helper for the interaction-ready CC0 models (kimchistan "Daycare &amp; Everyday Interiors", converted GLB -> FBX with the hierarchy
    /// kept): PART_* empties are real pivots (lever, pump head, swivel spout), COL_* meshes are authored collision boxes, FRONT marks the
    /// front, SOCKET_* marks emission points. Instantiates the model, faces FRONT toward a fixture's +Z, scales it, turns COL_* meshes
    /// into BoxColliders (renderer off) and returns the parts by name.
    /// </summary>
    public sealed class ArticulatedModel
    {
        public Transform Root;
        public readonly Dictionary<string, Transform> Parts = new Dictionary<string, Transform>();
        public readonly List<Collider> Colliders = new List<Collider>();
        public bool Ok => Root != null;

        public Transform Part(string name) => Parts.TryGetValue(name, out var t) ? t : null;

        /// <summary>key: ArtCatalog key (e.g. "dc_kit018"); size: target largest horizontal extent in metres (0 = keep 1:1).</summary>
        public static ArticulatedModel Build(string key, Transform parent, string name, float size, int layer)
        {
            var am = new ArticulatedModel();
            var model = ArtCatalog.I != null ? ArtCatalog.I.Model(key) : null;
            if (model == null) { Debug.LogWarning("[SHHH][art] missing model " + key + " (run build_catalog after importing Art/ThirdParty/Daycare)"); return am; }
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var inst = Object.Instantiate(model, root);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Prim.Remove(c);
            foreach (var a in inst.GetComponentsInChildren<Animator>()) Prim.Remove(a);
            Mats.MakeUrpSafe(inst);
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("PART_") || t.name.StartsWith("SOCKET_") || t.name == "FRONT") am.Parts[t.name] = t;
            // V10: some GLB->FBX conversions arrive Z-up (KIT018 audit 2026-11-10: visible 0.45 x 0.30 x 0.20 instead of 0.45 x 0.20 x 0.30 -
            // the dish rack lay on its back, its tray floor a vertical wall). FRONT is authored horizontal, so a mostly vertical FRONT means
            // the up axis is wrong: turn FRONT to +Z in 3D, then use the authored foot boxes (lowest part of furniture) to avoid upside-down.
            if (am.Parts.TryGetValue("FRONT", out var front0))
            {
                var d3 = root.InverseTransformPoint(front0.position);
                if (d3.sqrMagnitude > 1e-6f && Mathf.Abs(d3.normalized.y) > 0.9f)   // V12: 0.7 caught the fridge (FRONT authored 45 deg up), 0.9 = really vertical
                {
                    inst.transform.localRotation = Quaternion.FromToRotation(d3.normalized, Vector3.forward) * inst.transform.localRotation;
                    Transform foot = null; foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("COL_Foot") || t.name.StartsWith("PART_Foot")) { foot = t; break; }
                    var vb = VisibleBounds(inst.transform, root);
                    bool flip = false;
                    if (foot != null) flip = root.InverseTransformPoint(foot.position).y > vb.center.y;
                    else
                    {
                        // no feet authored: the working parts (pump head, mixer lever, spout, sockets) sit above the model's middle
                        float sum = 0f; int n = 0;
                        foreach (var kv in am.Parts) if (kv.Key != "FRONT" && !kv.Key.Contains("Tray") && !kv.Key.Contains("Base")) { sum += root.InverseTransformPoint(kv.Value.position).y; n++; }
                        flip = n > 0 && sum / n < vb.center.y;
                    }
                    if (flip) inst.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.forward) * inst.transform.localRotation;
                    Debug.Log($"[SHHH][art] {key}: imported with a vertical FRONT ({d3.normalized:F2}) - re-oriented to Y-up (feet {(foot != null ? foot.name : "unknown")}); visible now {VisibleBounds(inst.transform, root).size:F3}");
                }
            }
            // face FRONT toward local +Z (the person using it)
            if (am.Parts.TryGetValue("FRONT", out var front))
            {
                var d = root.InverseTransformPoint(front.position); d.y = 0;
                if (d.sqrMagnitude > 1e-6f) inst.transform.localRotation = Quaternion.FromToRotation(d.normalized, Vector3.forward) * inst.transform.localRotation;
            }
            // scale by the visual (non-collider) horizontal extent, then sit it on y = 0
            var b = VisibleBounds(inst.transform, root);
            float ext = Mathf.Max(b.size.x, b.size.z);
            if (size > 0f && ext > 1e-4f) inst.transform.localScale = Vector3.one * (size / ext);
            b = VisibleBounds(inst.transform, root);
            inst.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            // authored collision boxes -> BoxColliders; their render meshes are hidden
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.name.StartsWith("COL_") || mf.sharedMesh == null) continue;
                var r = mf.GetComponent<Renderer>(); if (r != null) r.enabled = false;
                var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size;
                am.Colliders.Add(bc);
            }
            Prim.SetLayerRecursive(root.gameObject, layer);
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            am.Root = root;
            return am;
        }

        static Bounds VisibleBounds(Transform t, Transform space)
        {
            bool any = false; var b = new Bounds();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("COL_") || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = space.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { b = new Bounds(c, Vector3.zero); any = true; } else b.Encapsulate(c);
                }
            }
            return b;
        }
    }
}
