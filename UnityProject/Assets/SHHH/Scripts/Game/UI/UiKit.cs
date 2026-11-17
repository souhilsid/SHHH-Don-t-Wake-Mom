using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// 2026-11-09 V8 UI kit (Gate 3): the brand art (Resources/UI_V8: original Inkscape icons/cards/plates, approved logo), brand fonts
    /// (Luckiest Guy display, Poppins body; Fredoka kept for button labels) and the chunky "toy" geometry every board and key is made of:
    /// bevelled extruded slabs, opaque and toon-lit (Quest-cheap: no blur, no stacked transparency). Sources: DesignSources/UI_V8.
    /// </summary>
    public static class UiKit
    {
        // ---- brand palette (Docs/UI_Art_Bible.md)
        public static readonly Color Midnight = Palette.Hex(0x211530), Deep = Palette.Hex(0x261239), Purple = Palette.Hex(0x70409F),
            Hush = Palette.Hex(0xFFCF2E), MoonCream = Palette.Hex(0xFFF0BA), Coral = Palette.Hex(0xF57F78), Pink = Palette.Hex(0xF45F73),
            RibbonRed = Palette.Hex(0xC92C4D), SoftWhite = Palette.Hex(0xFFF8E8), Board = Palette.Hex(0x34204F), BoardFace = Palette.Hex(0x3E2760),
            Mint = Palette.Hex(0x7BE39A), Teal = Palette.Hex(0x65C8C5);

        static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> _mat = new Dictionary<string, Material>();
        static TMP_FontAsset _display, _body; static bool _fontsTried;

        public static Texture2D Tex(string name)
        {
            if (_tex.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>("UI_V8/" + name); _tex[name] = t; return t;
        }

        static void Fonts()
        {
            if (_fontsTried) return; _fontsTried = true;
            _display = Resources.Load<TMP_FontAsset>("UI_V8/Fonts/LuckiestGuy SDF");
            _body = Resources.Load<TMP_FontAsset>("UI_V8/Fonts/Poppins-SemiBold SDF");
        }
        /// <summary>Luckiest Guy (titles, big numbers). Falls back to the project brand font.</summary>
        public static TMP_FontAsset Display { get { Fonts(); return _display != null ? _display : ArtCatalog.I.brandFont as TMP_FontAsset; } }
        /// <summary>Poppins SemiBold (small informative text).</summary>
        public static TMP_FontAsset Body { get { Fonts(); return _body != null ? _body : ArtCatalog.I.brandFont as TMP_FontAsset; } }
        /// <summary>Fredoka SemiBold (button labels / CTA, brand guideline).</summary>
        public static TMP_FontAsset Label => ArtCatalog.I.brandFont as TMP_FontAsset;

        /// <summary>Alpha-clipped unlit material for a kit texture (opaque queue: no sorting, cheap on Quest).</summary>
        public static Material Cutout(Texture2D tex)
        {
            if (tex == null) return Mats.Unlit(Color.magenta);
            string k = "C" + tex.GetInstanceID();
            if (_mat.TryGetValue(k, out var m) && m != null) return m;
            var b = Resources.Load<Material>("UI_V8/Mats/UiCutout");
            m = b != null ? new Material(b) : new Material(Mats.Textured(tex, true));
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            _mat[k] = m; return m;
        }
        /// <summary>Opaque unlit textured (art faces, paper).</summary>
        public static Material Opaque(Texture2D tex)
        {
            if (tex == null) return Mats.Unlit(Color.magenta);
            string k = "O" + tex.GetInstanceID();
            if (_mat.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Mats.Unlit(Color.white)); m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            _mat[k] = m; return m;
        }

        // ------------------------------------------------------------------ geometry
        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        static List<Vector2> Ring(float w, float h, float r, int seg)
        {
            r = Mathf.Clamp(r, 0.0005f, Mathf.Min(w, h) * 0.5f);
            var pts = new List<Vector2>();
            var c = new[] { new Vector2(w / 2 - r, h / 2 - r), new Vector2(-w / 2 + r, h / 2 - r), new Vector2(-w / 2 + r, -h / 2 + r), new Vector2(w / 2 - r, -h / 2 + r) };
            for (int q = 0; q < 4; q++) for (int i = 0; i <= seg; i++) { float a = (q * 90f + i * 90f / seg) * Mathf.Deg2Rad; pts.Add(c[q] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return pts;
        }

        /// <summary>
        /// Bevelled rounded slab, front face at z = -depth/2 facing -Z (towards the viewer), centred. The bevel is smooth-shaded so the
        /// toon ramp draws a soft highlight band around every key/board edge; sides and back are hard-edged.
        /// </summary>
        public static Mesh Slab(float w, float h, float r, float depth, float bevel, int seg = 5)
        {
            string key = $"{w:0.000}_{h:0.000}_{r:0.000}_{depth:0.000}_{bevel:0.000}_{seg}";
            if (_meshes.TryGetValue(key, out var m) && m != null) return m;
            bevel = Mathf.Min(bevel, Mathf.Min(w, h) * 0.25f, depth * 0.6f);
            var outer = Ring(w, h, r, seg); var inner = Ring(w - 2 * bevel, h - 2 * bevel, Mathf.Max(0.001f, r - bevel), seg);
            int n = outer.Count; float zf = -depth / 2f, zb = depth / 2f;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            // front face (fan)
            int f0 = v.Count; v.Add(new Vector3(0, 0, zf)); uv.Add(new Vector2(0.5f, 0.5f));
            foreach (var p in inner) { v.Add(new Vector3(p.x, p.y, zf)); uv.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f)); }
            for (int i = 0; i < n; i++) { int a = f0 + 1 + i, b = f0 + 1 + (i + 1) % n; t.Add(f0); t.Add(b); t.Add(a); }
            // bevel ring (inner front -> outer, slightly back)
            int bi = v.Count; foreach (var p in inner) { v.Add(new Vector3(p.x, p.y, zf)); uv.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f)); }
            int bo = v.Count; foreach (var p in outer) { v.Add(new Vector3(p.x, p.y, zf + bevel)); uv.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f)); }
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; t.Add(bi + i); t.Add(bi + j); t.Add(bo + j); t.Add(bi + i); t.Add(bo + j); t.Add(bo + i); }
            // sides
            int s0 = v.Count; foreach (var p in outer) { v.Add(new Vector3(p.x, p.y, zf + bevel)); uv.Add(Vector2.zero); }
            int s1 = v.Count; foreach (var p in outer) { v.Add(new Vector3(p.x, p.y, zb)); uv.Add(Vector2.zero); }
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; t.Add(s0 + i); t.Add(s0 + j); t.Add(s1 + j); t.Add(s0 + i); t.Add(s1 + j); t.Add(s1 + i); }
            // back
            int k0 = v.Count; v.Add(new Vector3(0, 0, zb)); uv.Add(new Vector2(0.5f, 0.5f));
            foreach (var p in outer) { v.Add(new Vector3(p.x, p.y, zb)); uv.Add(Vector2.zero); }
            for (int i = 0; i < n; i++) { int a = k0 + 1 + i, b = k0 + 1 + (i + 1) % n; t.Add(k0); t.Add(a); t.Add(b); }
            m = new Mesh { name = "slab_" + key }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            // hard front: force the front fan normal to -Z (RecalculateNormals averages per vertex, the fan vertices are unshared)
            var nn = m.normals; for (int i = f0; i < f0 + 1 + n; i++) nn[i] = Vector3.back; m.normals = nn;
            m.RecalculateBounds(); _meshes[key] = m; return m;
        }

        public static GameObject SlabObject(Transform parent, string name, Vector3 localPos, float w, float h, float r, float depth, float bevel, Material mat)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = localPos;
            g.AddComponent<MeshFilter>().sharedMesh = Slab(w, h, r, depth, bevel);
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            g.layer = Layers.UI;
            return g;
        }

        static Mesh _quad;
        /// <summary>A textured quad facing -Z (no collider). size in metres.</summary>
        public static GameObject Quad(Transform parent, string name, Material mat, Vector3 localPos, Vector2 size)
        {
            if (_quad == null)
            {
                _quad = new Mesh { name = "ui_quad" };
                _quad.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) });
                _quad.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
                _quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0); _quad.RecalculateNormals(); _quad.RecalculateBounds();
            }
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localScale = new Vector3(size.x, size.y, 1f);
            g.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            g.layer = Layers.UI;
            return g;
        }

        public static GameObject Icon(Transform parent, string icon, Vector3 localPos, float size)
            => Quad(parent, "icon_" + icon, Cutout(Tex("icon_" + icon)), localPos, new Vector2(size, size));

        // ------------------------------------------------------------------ text
        public static TextMeshPro Text(Transform parent, string text, Vector3 localPos, float size, Color color, TMP_FontAsset font, float width, TextAlignmentOptions align = TextAlignmentOptions.Center, float outline = 0.22f)
        {
            var t = WorldText.Make(parent, "txt", text, localPos, size, color, align, width);
            if (font != null) t.font = font;
            t.outlineWidth = outline; t.outlineColor = new Color32(0x26, 0x12, 0x39, 255);
            return t;
        }

        /// <summary>Readable text colour for a key/cap colour (dark ink on light caps, cream on dark).</summary>
        public static Color InkFor(Color cap) => (0.299f * cap.r + 0.587f * cap.g + 0.114f * cap.b) > 0.55f ? Deep : SoftWhite;

        // ------------------------------------------------------------------ boards
        /// <summary>
        /// The V8 board: a thick toon-lit plum slab with a moon-cream rim and a darker inset face, optionally crowned by the pink ribbon
        /// banner carrying a Luckiest Guy title. Front faces -Z; the face is at z = +0.004, so content at z ≤ 0 (keys, text at -0.015) sits in front of it.
        /// </summary>
        public static Transform Panel(Transform parent, string name, Vector3 localPos, Vector2 size, string title = null, Color? body = null)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = localPos;
            float r = Mathf.Min(0.07f, Mathf.Min(size.x, size.y) * 0.16f);
            SlabObject(root, "rim", new Vector3(0, 0, 0.026f), size.x + 0.036f, size.y + 0.036f, r + 0.018f, 0.03f, 0.012f, Mats.Lit(MoonCream, 0.35f));
            SlabObject(root, "body", new Vector3(0, 0, 0.019f), size.x, size.y, r, 0.03f, 0.014f, Mats.Lit(body ?? Board, 0.25f));
            if (!string.IsNullOrEmpty(title)) Ribbon(root, title, new Vector3(0, size.y / 2f + 0.005f, -0.03f), Mathf.Min(size.x * 0.82f, 0.95f));
            return root;
        }

        /// <summary>Pink ribbon banner (plate_ribbon art) with a title. Width in metres.</summary>
        public static Transform Ribbon(Transform parent, string title, Vector3 localPos, float width)
        {
            var g = new GameObject("ribbon").transform; g.SetParent(parent, false); g.localPosition = localPos;
            Quad(g, "art", Cutout(Tex("plate_ribbon")), Vector3.zero, new Vector2(width, width * 0.25f));
            var t = Text(g, title, new Vector3(0, width * 0.012f, -0.004f), width * 0.62f, SoftWhite, Display, width * 0.72f);
            t.enableAutoSizing = true; t.fontSizeMax = width * 0.62f; t.fontSizeMin = width * 0.2f; t.rectTransform.sizeDelta = new Vector2(width * 0.72f, width * 0.12f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return g;
        }

        /// <summary>
        /// Illustrated menu card (card_* art in a cream frame) that is also a poke/ray button. The title sits on the card's dark band.
        /// </summary>
        public static PokeButton Card(Transform parent, string art, string title, Vector3 localPos, Vector2 size, System.Action onPress, string subtitle = null)
        {
            var b = PokeButton.CreateBlank(parent, "card_" + art, localPos, size, onPress);
            var cap = b.Cap;
            SlabObject(cap, "frame", new Vector3(0, 0, 0.004f), size.x + 0.02f, size.y + 0.02f, 0.03f, 0.022f, 0.008f, Mats.Lit(Deep, 0.3f));
            Quad(cap, "art", Opaque(Tex("card_" + art)), new Vector3(0, 0, -0.0085f), size);
            var t = Text(cap, title, new Vector3(0, -size.y * 0.36f, -0.012f), size.x * 1.25f, Hush, Display, size.x * 0.92f);
            t.enableAutoSizing = true; t.fontSizeMax = size.x * 1.25f; t.fontSizeMin = size.x * 0.5f; t.rectTransform.sizeDelta = new Vector2(size.x * 0.9f, size.y * 0.12f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (!string.IsNullOrEmpty(subtitle)) Text(cap, subtitle, new Vector3(0, -size.y * 0.445f, -0.012f), size.x * 0.55f, MoonCream, Body, size.x * 0.92f, TextAlignmentOptions.Center, 0.1f);
            var glow = SlabObject(b.transform, "selected", new Vector3(0, 0, -0.001f), size.x + 0.05f, size.y + 0.05f, 0.045f, 0.01f, 0.004f, Mats.Unlit(Hush));
            glow.SetActive(false); b.SelectedMark = glow;
            return b;
        }
    }
}
