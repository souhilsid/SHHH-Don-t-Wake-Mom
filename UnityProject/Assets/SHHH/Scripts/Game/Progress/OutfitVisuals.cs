using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Wardrobe pieces on a sibling (hat / glasses / pyjama pattern / accessory) as small stylised meshes attached to the rig's Head
    /// and Chest bones, so they move with the floating pieces. Placeholder geometry in the house palette; purely visual.
    /// </summary>
    public static class OutfitVisuals
    {
        static Vector3 _face = Vector3.forward, _mouth; static float _top;
        public static int Apply(Transform rigRoot, Transform head, Dictionary<string, string> outfit, float scale = 1f)
        {
            if (rigRoot == null || outfit == null || outfit.Count == 0) return 0;
            _face = rigRoot.parent != null ? rigRoot.parent.forward : rigRoot.forward; // AstraRig turns the model to face the holder's +Z
            // landmarks from the rest pose: top of the hair and the mouth (eyes sit just above it)
            var all = Prim.RendererBounds(rigRoot.gameObject); _top = all.max.y;
            _mouth = head != null ? head.position + _face * 0.14f : all.center;
            foreach (var r in rigRoot.GetComponentsInChildren<Renderer>(true)) if (r.name.ToLowerInvariant().Contains("mouth")) { _mouth = r.bounds.center; break; }
            Transform chest = null; foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true)) if (t.name == "Chest") { chest = t; break; }
            int n = 0;
            if (head != null && outfit.TryGetValue("hat", out var hat) && hat != "hat_none") { Hat(head, hat, scale); n++; }
            if (head != null && outfit.TryGetValue("glasses", out var gl) && gl != "glasses_none") { Glasses(head, gl, scale); n++; }
            if (chest != null && outfit.TryGetValue("accessory", out var acc) && acc != "accessory_none") { Accessory(chest, acc, scale); n++; }
            if (outfit.TryGetValue("pyjama", out var pj) && pj != "pyjama_classic") { Pattern(chest ?? rigRoot, pj, scale); n++; }
            return n;
        }

        static Transform Holder(Transform bone, string name, Vector3 worldUpOffset)
        {
            // built in world units, then parented keeping world size (rig bones carry the import scale)
            var h = new GameObject("outfit_" + name).transform;
            h.position = bone.position + worldUpOffset; h.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(_face, Vector3.up).sqrMagnitude > 1e-4f ? Vector3.ProjectOnPlane(_face, Vector3.up) : Vector3.forward, Vector3.up);
            h.SetParent(bone, true);
            return h;
        }

        static void Hat(Transform head, string id, float s)
        {
            var h = Holder(head, id, Vector3.zero); h.position = new Vector3(head.position.x, _top - 0.03f * s, head.position.z);
            switch (id)
            {
                case "hat_beanie": Prim.Shape(PrimitiveType.Sphere, h, "beanie", Vector3.zero, new Vector3(0.24f, 0.13f, 0.24f) * s, Mats.Lit(Palette.Teal)); Prim.Shape(PrimitiveType.Sphere, h, "pom", new Vector3(0, 0.08f, 0) * s, Vector3.one * 0.05f * s, Mats.Lit(Palette.Cream)); break;
                case "hat_nightcap": Prim.Shape(PrimitiveType.Capsule, h, "cap", new Vector3(0, 0.06f, -0.03f) * s, new Vector3(0.2f, 0.14f, 0.2f) * s, Mats.Lit(Palette.Navy)).transform.localRotation = Quaternion.Euler(-25, 0, 0); Prim.Shape(PrimitiveType.Sphere, h, "pom", new Vector3(0, 0.16f, -0.1f) * s, Vector3.one * 0.05f * s, Mats.Lit(Palette.Yellow)); break;
                case "hat_crown": for (int i = 0; i < 5; i++) { var a = i * 72f * Mathf.Deg2Rad; Prim.Box(h, "pt" + i, new Vector3(Mathf.Sin(a) * 0.09f, 0.03f, Mathf.Cos(a) * 0.09f) * s, new Vector3(0.045f, 0.07f, 0.01f) * s, Mats.Lit(Palette.Yellow), false).transform.localRotation = Quaternion.Euler(0, i * 72f, 0); } break;
            }
        }

        static void Glasses(Transform head, string id, float s)
        {
            var h = Holder(head, id, Vector3.zero);
            h.position = _mouth + Vector3.up * 0.075f * s + h.forward * 0.02f * s; // eye line
            if (id == "glasses_sleepmask") { h.position += Vector3.up * 0.08f * s; Prim.Box(h, "mask", Vector3.zero, new Vector3(0.18f, 0.05f, 0.02f) * s, Mats.Lit(Palette.Purple), false); return; }
            var c = id == "glasses_star" ? Palette.Yellow : Palette.Ink;
            foreach (var x in new[] { -0.045f, 0.045f })
                Prim.Shape(PrimitiveType.Cylinder, h, "lens", new Vector3(x, 0, 0) * s, new Vector3(0.06f, 0.004f, 0.06f) * s, Mats.Lit(c)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.Box(h, "bridge", Vector3.zero, new Vector3(0.03f, 0.006f, 0.006f) * s, Mats.Lit(c), false);
        }

        static void Accessory(Transform chest, string id, float s)
        {
            var h = Holder(chest, id, Vector3.zero);
            h.position = chest.position + h.forward * 0.11f * s;
            switch (id)
            {
                case "accessory_teddy": Prim.Shape(PrimitiveType.Sphere, h, "teddy", new Vector3(0.05f, 0, 0) * s, Vector3.one * 0.07f * s, Mats.Lit(new Color(0.75f, 0.55f, 0.38f))); Prim.Shape(PrimitiveType.Sphere, h, "head", new Vector3(0.05f, 0.055f, 0) * s, Vector3.one * 0.05f * s, Mats.Lit(new Color(0.75f, 0.55f, 0.38f))); break;
                case "accessory_magnifier": Prim.Shape(PrimitiveType.Cylinder, h, "lens", new Vector3(-0.05f, 0.02f, 0) * s, new Vector3(0.06f, 0.004f, 0.06f) * s, Mats.Lit(Palette.Moon)).transform.localRotation = Quaternion.Euler(90, 0, 0); Prim.Box(h, "handle", new Vector3(-0.05f, -0.04f, 0) * s, new Vector3(0.012f, 0.06f, 0.012f) * s, Mats.Lit(Palette.Wood), false); break;
                case "accessory_toolbelt": h.position = chest.position - Vector3.up * 0.12f * s; Prim.Shape(PrimitiveType.Cylinder, h, "belt", Vector3.zero, new Vector3(0.3f, 0.02f, 0.26f) * s, Mats.Lit(Palette.Wood)); break;
            }
        }

        static void Pattern(Transform chest, string id, float s)
        {
            var h = Holder(chest, id, Vector3.zero); h.position = chest.position + h.forward * 0.12f * s;
            var c = id == "pyjama_moon" ? Palette.Moon : Palette.Yellow;
            for (int i = 0; i < 3; i++) Prim.Shape(PrimitiveType.Sphere, h, "dot" + i, new Vector3((i - 1) * 0.06f, (i % 2) * 0.05f, 0) * s, new Vector3(0.03f, 0.03f, 0.008f) * s, Mats.Unlit(c));
        }
    }
}
