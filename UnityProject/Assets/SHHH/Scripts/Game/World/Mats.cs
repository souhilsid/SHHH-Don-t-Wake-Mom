using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Brand palette (see Docs/ART_BIBLE.md) and cached material factory. One material per colour keeps SRP batching cheap.</summary>
    public static class Palette
    {
        // V8 (2026-11-09) brand palette: midnight #211530, hush yellow #FFCF2E, coral #F57F78, purple #70409F, soft white #FFF8E8
        public static readonly Color Navy = Hex(0x211530), Yellow = Hex(0xFFCF2E), Coral = Hex(0xF57F78), Teal = Hex(0x65C8C5),
            Purple = Hex(0x70409F), Cream = Hex(0xFFF8E8), Mint = Hex(0x7BE39A), Ink = Hex(0x1A0E2A), Moon = Hex(0xFFF0BA),
            WallLiving = Hex(0xB98AA6), WallKitchen = Hex(0x86B8AE), WallLaundry = Hex(0x9D8FCB), Wood = Hex(0x9A6142), WoodLight = Hex(0xD49A6A),
            Floor = Hex(0xA9704F), Tile = Hex(0xE6D9C3), Danger = Hex(0xFF4D5E), Ok = Hex(0x5BE38A);
        public static Color Hex(int rgb, float a = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        public static readonly Color[] Siblings = { Hex(0xFF7769), Hex(0x65C8C5), Hex(0xFFD65A), Hex(0x9B7BE0) };
        public static readonly string[] SiblingColorNames = { "Coral", "Teal", "Sunny", "Grape" };
    }

    public static class Mats
    {
        static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        static Material Base(Material asset, string shader)
        {
            if (asset != null) return asset;
            var s = Shader.Find(shader);
            return new Material(s != null ? s : Shader.Find("Unlit/Color"));
        }

        public static Material Lit(Color c, float smooth = 0.15f)
        {
            string k = "L" + ColorUtility.ToHtmlStringRGBA(c) + smooth;
            if (_cache.TryGetValue(k, out var m)) return m;
            m = new Material(Base(ArtCatalog.I.toonBase != null ? ArtCatalog.I.toonBase : ArtCatalog.I.litBase, "SHHH/Toon")) { name = "SHHH_Toon_" + k };
            m.SetColor("_BaseColor", c); m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            m.enableInstancing = true;
            _cache[k] = m; return m;
        }

        public static Material Unlit(Color c)
        {
            string k = "U" + ColorUtility.ToHtmlStringRGBA(c);
            if (_cache.TryGetValue(k, out var m)) return m;
            m = new Material(Base(ArtCatalog.I.unlitBase, "Universal Render Pipeline/Unlit")) { name = "SHHH_Unlit_" + k };
            m.SetColor("_BaseColor", c); m.color = c;
            _cache[k] = m; return m;
        }

        /// <summary>Transparent unlit (VFX, glows, glass panels). Use sparingly on Quest 2: overdraw.</summary>
        public static Material Glow(Color c)
        {
            string k = "G" + ColorUtility.ToHtmlStringRGBA(c);
            if (_cache.TryGetValue(k, out var m)) return m;
            var b = ArtCatalog.I.unlitTransparent;
            m = b != null ? new Material(b) : MakeTransparent(Base(null, "Universal Render Pipeline/Unlit"));
            m.name = "SHHH_Glow_" + k;
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            m.SetColor("_BaseColor", c); m.color = c;
            _cache[k] = m; return m;
        }

        static Material MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        static readonly Dictionary<int, Material> _urpSafe = new Dictionary<int, Material>();

        /// <summary>Returns a URP-compatible version of an imported material (built-in "Standard" renders magenta in URP).</summary>
        public static Material UrpSafe(Material src)
        {
            if (src == null || src.shader == null) return Lit(Color.white);
            var sn = src.shader.name;
            // Keep unlit, particle, text, vendor-VFX and our own shaders; convert every LIT surface to the stylized toon look.
            bool litLike = sn == "Standard" || sn.StartsWith("Legacy") || sn.StartsWith("Universal Render Pipeline/Lit") || sn.StartsWith("Universal Render Pipeline/Simple Lit") || sn.StartsWith("Universal Render Pipeline/Baked Lit") || sn.StartsWith("Autodesk") || !src.shader.isSupported;
            if (!litLike) return src;
            if (_urpSafe.TryGetValue(src.GetInstanceID(), out var m)) return m;
            m = new Material(Base(ArtCatalog.I.toonBase != null ? ArtCatalog.I.toonBase : ArtCatalog.I.litBase, "SHHH/Toon")) { name = src.name + "_Toon" };
            var tex = src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap") != null ? src.GetTexture("_BaseMap") : src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            var col = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            if (src.HasProperty("_EmissionColor") && src.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", src.GetColor("_EmissionColor"));
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            m.enableInstancing = true;
            _urpSafe[src.GetInstanceID()] = m;
            return m;
        }

        public static void MakeUrpSafe(GameObject g)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++) { var n = UrpSafe(mats[i]); if (n != mats[i]) { mats[i] = n; changed = true; } }
                if (changed) r.sharedMaterials = mats;
            }
        }

        public static Material Textured(Texture tex, bool transparent)
        {
            string k = "T" + (tex != null ? tex.GetInstanceID() : 0) + transparent;
            if (_cache.TryGetValue(k, out var m)) return m;
            m = transparent ? new Material(Glow(Color.white)) : new Material(Unlit(Color.white));
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            _cache[k] = m; return m;
        }
    }

    /// <summary>Tiny primitive-construction helpers for procedural props, fixtures and UI frames.</summary>
    public static class Prim
    {
        public static void Remove(Object obj) { if (Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj); }
        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Remove(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Shape(PrimitiveType t, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Remove(g.GetComponent<Collider>());
            return g;
        }

        public static Bounds RendererBounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static Bounds LocalRendererBounds(GameObject g, Transform relativeTo)
        {
            var result = new Bounds(); bool first = true;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                Bounds b;
                var mf = r.GetComponent<MeshFilter>();
                if (mf && mf.sharedMesh) b = mf.sharedMesh.bounds;
                else if (r is SkinnedMeshRenderer skin) b = skin.localBounds;
                else continue;
                var matrix = relativeTo.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var p = matrix.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
                }
            }
            return result;
        }

        public static void SetLayerRecursive(GameObject g, int layer) { foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
    }
}
