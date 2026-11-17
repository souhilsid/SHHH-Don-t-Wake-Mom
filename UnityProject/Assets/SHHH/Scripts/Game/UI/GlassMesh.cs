using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Cached rounded-rectangle meshes for the V5 "night glass" UI: tinted translucent panels with a cream rim and a soft top
    /// sheen. No blur/refraction (Quest-safe): 3 small transparent layers per panel, everything else opaque.
    /// </summary>
    public static class GlassMesh
    {
        static readonly Dictionary<long, Mesh> _cache = new Dictionary<long, Mesh>();

        public static Mesh RoundedRect(float w, float h, float r, int seg = 6)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            long key = ((long)Mathf.RoundToInt(w * 1000) << 40) ^ ((long)Mathf.RoundToInt(h * 1000) << 20) ^ Mathf.RoundToInt(r * 1000) ^ ((long)seg << 58);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            var verts = new List<Vector3> { Vector3.zero }; var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var centers = new[] { new Vector2(w / 2 - r, h / 2 - r), new Vector2(-w / 2 + r, h / 2 - r), new Vector2(-w / 2 + r, -h / 2 + r), new Vector2(w / 2 - r, -h / 2 + r) };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + i * 90f / seg) * Mathf.Deg2Rad;
                    var p = centers[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    verts.Add(new Vector3(p.x, p.y, 0)); uvs.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f));
                }
            var tris = new List<int>();
            int n = verts.Count - 1;
            for (int i = 1; i <= n; i++) { tris.Add(0); tris.Add(i == n ? 1 : i + 1); tris.Add(i); }
            m = new Mesh { name = $"rrect_{w:0.00}x{h:0.00}" };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            _cache[key] = m; return m;
        }

        public static GameObject Layer(Transform parent, string name, float w, float h, float r, float z, Material mat)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = new Vector3(0, 0, z);
            // RoundedRect is wound to face -Z (towards the viewer of our boards).
            g.AddComponent<MeshFilter>().sharedMesh = RoundedRect(w, h, r);
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return g;
        }

        /// <summary>Night-glass panel: cream rim, tinted body, top sheen, accent bar. Size in metres; front faces -Z.</summary>
        public static void Panel(Transform root, Vector2 size, Color tint, Color accent)
        {
            float r = Mathf.Min(0.06f, Mathf.Min(size.x, size.y) * 0.18f);
            Layer(root, "glassRim", size.x + 0.024f, size.y + 0.024f, r + 0.012f, 0.016f, Mats.Glow(new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.55f)));
            Layer(root, "glassBody", size.x, size.y, r, 0.012f, Mats.Glow(new Color(tint.r, tint.g, tint.b, 0.84f)));
            var sheen = Layer(root, "glassSheen", size.x - 0.03f, size.y * 0.32f, r * 0.8f, 0.010f, Mats.Glow(new Color(1f, 1f, 1f, 0.07f)));
            sheen.transform.localPosition = new Vector3(0, size.y * 0.30f, 0.010f);
            var bar = Layer(root, "accent", size.x * 0.28f, 0.014f, 0.007f, 0.009f, Mats.Unlit(accent));
            bar.transform.localPosition = new Vector3(0, size.y / 2 - 0.02f, 0.009f);
        }
    }
}
