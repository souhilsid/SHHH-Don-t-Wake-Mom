using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 hand-painted fabric for the generated clothes (Tools/blender/make_clothes.py): soft painted stripes for T-shirts, a terry
    /// check for towels, a mottled knit for the rolled core; wet laundry is darker and more saturated. Procedural, original, cached
    /// per (pattern, colour). Uses the toon base material so it sits with the rest of the house.
    /// </summary>
    public static class FabricLook
    {
        public enum Pattern { Stripes, Terry, Knit }
        static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        public static Material Get(Pattern p, Color c, bool wet)
        {
            if (wet) c = Color.Lerp(c, new Color(0.18f, 0.28f, 0.48f), 0.35f);
            string k = p + ColorUtility.ToHtmlStringRGB(c) + wet;
            if (_cache.TryGetValue(k, out var m)) return m;
            m = new Material(Mats.Lit(Color.white, wet ? 0.45f : 0.08f)) { name = "SHHH_Fabric_" + k };
            var tex = Paint(p, c); m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.color = Color.white;
            _cache[k] = m; return m;
        }

        static Texture2D Paint(Pattern p, Color c)
        {
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "fabric_" + p, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[N * N]; var rng = new System.Random(p.GetHashCode() * 31 + (int)(c.r * 255) * 7 + (int)(c.g * 255));
            Color dark = Color.Lerp(c, new Color(0.25f, 0.18f, 0.3f), 0.32f), light = Color.Lerp(c, Color.white, 0.28f), accent = Color.Lerp(c, Color.white, 0.55f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float brush = Mathf.PerlinNoise(u * 6f + 3.1f, v * 6f) * 0.5f + Mathf.PerlinNoise(u * 19f, v * 19f + 7f) * 0.25f;   // painted mottling
                    Color col = Color.Lerp(dark, light, 0.35f + brush * 0.6f);
                    if (p == Pattern.Stripes) { float s = Mathf.Repeat(v * 7f + Mathf.PerlinNoise(u * 3f, v * 3f) * 0.25f, 1f); if (s > 0.62f && s < 0.86f) col = Color.Lerp(accent, light, brush * 0.5f); }
                    else if (p == Pattern.Terry) { float a = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 16f)) * Mathf.Abs(Mathf.Sin(v * Mathf.PI * 16f)); col = Color.Lerp(col, dark, (1f - a) * 0.25f); if (v > 0.84f && v < 0.92f) col = accent; }
                    else { float r = Mathf.Abs(Mathf.Sin((u * 24f + Mathf.Sin(v * 40f) * 0.3f) * Mathf.PI)); col = Color.Lerp(col, dark, r * 0.18f); }
                    px[y * N + x] = col;
                }
            t.SetPixels(px); t.Apply(true, false); return t;
        }
    }
}
