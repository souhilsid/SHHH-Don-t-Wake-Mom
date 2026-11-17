using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>
    /// 2026-11-09 Gate 3 UI kit setup: import rules for Resources/UI_V8 (clamp, alpha, mips, Quest ASTC), TMP font assets for the
    /// brand fonts (Luckiest Guy display, Poppins body), and the material assets the runtime UI clones (alpha-clip unlit, transparent
    /// unlit, panoramic night sky) so their shader variants are included in the APK.
    /// </summary>
    public sealed class ShhhUiV8 : AssetPostprocessor
    {
        const string Res = "Assets/SHHH/Resources/UI_V8";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Res)) return;
            var ti = (TextureImporter)assetImporter;
            bool sky = assetPath.Contains("sky_");
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.alphaIsTransparency = !sky;
            ti.alphaSource = sky ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            ti.wrapMode = sky ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; ti.mipmapEnabled = !sky; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 2;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = sky ? 4096 : 2048;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = ti.maxTextureSize;
            android.format = sky ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            ti.SetPlatformTextureSettings(android);
        }

        [ShhhCommand("ui_v8_setup")]
        public static string Setup(string args)
        {
            AssetDatabase.ImportAsset(Res, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Directory.CreateDirectory(Res + "/Fonts"); Directory.CreateDirectory(Res + "/Mats");
            var sb = new System.Text.StringBuilder();
            sb.Append(Font("Assets/SHHH/Fonts_V8/LuckiestGuy-Regular.ttf", Res + "/Fonts/LuckiestGuy SDF.asset")).Append("; ");
            sb.Append(Font("Assets/SHHH/Fonts_V8/Poppins-SemiBold.ttf", Res + "/Fonts/Poppins-SemiBold SDF.asset")).Append("; ");
            // materials
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var cut = new Material(unlit) { name = "UiCutout" };
            cut.SetFloat("_AlphaClip", 1f); cut.SetFloat("_Cutoff", 0.5f); cut.EnableKeyword("_ALPHATEST_ON"); cut.renderQueue = 2450;
            Save(cut, Res + "/Mats/UiCutout.mat");
            var tr = new Material(unlit) { name = "UiTransparent" };
            tr.SetFloat("_Surface", 1f); tr.SetFloat("_Blend", 0f); tr.SetFloat("_ZWrite", 0f);
            tr.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); tr.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            tr.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); tr.renderQueue = 3000;
            Save(tr, Res + "/Mats/UiTransparent.mat");
            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Res + "/sky_night_pano.png");
            var pano = Shader.Find("Skybox/Panoramic");
            if (pano != null && skyTex != null)
            {
                var sky = new Material(pano) { name = "SkyNight" };
                sky.SetTexture("_MainTex", skyTex); sky.SetFloat("_Mapping", 1f); sky.SetFloat("_ImageType", 0f); sky.SetFloat("_Exposure", 1f); sky.SetFloat("_Rotation", 0f);
                Save(sky, Res + "/Mats/SkyNight.mat");
                sb.Append("sky material ok; ");
            }
            var wv = Shader.Find("SHHH/WindowView");
            if (wv != null && skyTex != null) { var w = new Material(wv) { name = "WindowView" }; w.SetTexture("_MainTex", skyTex); w.SetColor("_Tint", new Color(0.92f, 0.93f, 1f, 1f)); Save(w, Res + "/Mats/WindowView.mat"); sb.Append("window view ok; "); }
            else sb.Append("window view skipped; ");
            if (pano == null || skyTex == null) sb.Append($"sky material skipped (shader {(pano != null)}, tex {(skyTex != null)}); ");
            AssetDatabase.SaveAssets();
            int n = Directory.GetFiles(Res, "*.png").Length;
            return sb.Append(n).Append(" textures").ToString();
        }

        /// <summary>Lists house-prefab renderers that look like windows / painted backdrops (name, material, texture, bounds).</summary>
        [ShhhCommand("house_windows")]
        public static string HouseWindows(string args)
        {
            var house = Resources.Load<GameObject>("SHHH_WorldHouse"); if (house == null) return "no SHHH_WorldHouse in Resources";
            var rx = new System.Text.RegularExpressions.Regex(string.IsNullOrEmpty(args) ? "window|backdrop|exterior|sky|outside|night|glass|curtain|pane" : args, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var sb = new System.Text.StringBuilder(); int n = 0;
            foreach (var r in house.GetComponentsInChildren<Renderer>(true))
            {
                string mats = ""; foreach (var m in r.sharedMaterials) { if (m == null) continue; var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture; mats += m.name + "(" + m.shader.name + (t ? ":" + t.name : "") + ") "; }
                string path = r.name; var p = r.transform.parent; for (int d = 0; d < 3 && p != null; d++) { path = p.name + "/" + path; p = p.parent; }
                if (!rx.IsMatch(path + " " + mats)) continue;
                n++; if (n <= 80) sb.Append($"{path} | {mats}| c={r.bounds.center} s={r.bounds.size} on={r.enabled}\n");
            }
            return n + " matches\n" + sb;
        }

        /// <summary>Big renderers of the house prefab (any bounds side >= args metres, default 5): finds shells/backdrops.</summary>
        [ShhhCommand("house_big")]
        public static string HouseBig(string args)
        {
            var house = Resources.Load<GameObject>("SHHH_WorldHouse"); if (house == null) return "no SHHH_WorldHouse";
            float min = float.TryParse(args, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 5f;
            var sb = new System.Text.StringBuilder("top: "); foreach (Transform c in house.transform) sb.Append(c.name).Append(", "); sb.Append("\n");
            foreach (var r in house.GetComponentsInChildren<Renderer>(true))
            {
                var s = r.bounds.size; if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < min) continue;
                string mats = ""; foreach (var m in r.sharedMaterials) if (m) mats += m.name + " ";
                string path = r.name; var p = r.transform.parent; for (int d = 0; d < 3 && p != null; d++) { path = p.name + "/" + path; p = p.parent; }
                sb.Append($"{path} | {mats}| c={r.bounds.center} s={s}\n");
            }
            return sb.ToString();
        }

        static void Save(Material m, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old != null) { EditorUtility.CopySerialized(m, old); EditorUtility.SetDirty(old); }
            else AssetDatabase.CreateAsset(m, path);
        }

        static string Font(string ttfPath, string outPath)
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null) return "missing " + ttfPath;
            AssetDatabase.DeleteAsset(outPath);
            var fa = TMPro.TMP_FontAsset.CreateFontAsset(ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic, true);
            fa.name = Path.GetFileNameWithoutExtension(outPath);
            AssetDatabase.CreateAsset(fa, outPath);
            fa.material.name = fa.name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (fa.atlasTextures != null) foreach (var t in fa.atlasTextures) { if (t == null) continue; t.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
            string chars = ""; for (int c = 32; c < 127; c++) chars += (char)c;
            chars += "…—°·’“”★×";
            fa.TryAddCharacters(chars, out string missing);
            EditorUtility.SetDirty(fa);
            return $"{fa.name} (missing '{missing}')";
        }
    }
}
