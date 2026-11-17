using System;
using System.IO;
using System.Linq;
using SHHH.Automation;
using UnityEditor;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Asset Store packages the user owns are downloaded by Unity into its own cache; this lists them and imports one
    /// into an isolated audit folder flow (the same thing Package Manager's "Import" button does).
    /// </summary>
    public static class ShhhAssetStore
    {
        static string Cache => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x");

        [ShhhCommand("store_cache_list")]
        public static string List(string filter)
        {
            if (!Directory.Exists(Cache)) return "no cache at " + Cache;
            var files = Directory.GetFiles(Cache, "*.unitypackage", SearchOption.AllDirectories)
                .Select(f => f.Substring(Cache.Length + 1) + $"  ({new FileInfo(f).Length / 1024} KB, {File.GetLastWriteTimeUtc(f):yyyy-MM-dd})")
                .Where(f => string.IsNullOrWhiteSpace(filter) || filter.Split('|').Any(k => f.IndexOf(k.Trim(), StringComparison.OrdinalIgnoreCase) >= 0));
            return string.Join("\n", files);
        }

        /// <summary>Copies matching cached packages into SHHH/SourceAssets/AssetStore (project-local, never committed) for offline audit.</summary>
        [ShhhCommand("store_copy")]
        public static string Copy(string filter)
        {
            var dst = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "..", "SourceAssets", "AssetStore"));
            Directory.CreateDirectory(dst);
            var log = new System.Text.StringBuilder();
            foreach (var f in Directory.GetFiles(Cache, "*.unitypackage", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f);
                if (!filter.Split('|').Any(k => name.IndexOf(k.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                var to = Path.Combine(dst, name);
                if (!File.Exists(to) || new FileInfo(to).Length != new FileInfo(f).Length) File.Copy(f, to, true);
                log.Append(name).Append('\n');
            }
            return log.ToString();
        }

        /// <summary>Copies the audited, family-friendly effect prefabs into Resources/VFX under gameplay names (blood/skull/sword effects are never used).</summary>
        [ShhhCommand("vfx_setup")]
        public static string VfxSetup()
        {
            var map = new (string key, string prefab)[]
            {
                ("pow", "CFXR _POW_"), ("wham", "CFXR2 _WHAM_ 3"), ("hit_yellow", "CFXR Hit D 3D (Yellow)"), ("toon_impact", "VFX_ImpactToon_1.1.0"),
                ("stars", "CFXR4 Falling Stars"), ("wow", "CFXR3 _WOW_"), ("flash", "CFXR Flash"), ("splash", "CFXR Water Splash (Smaller)"),
                ("bubbles", "VFX_Bubbles_Single_Transparent_01"), ("puff", "CFXR3 Hit Misc A"), ("heart", "CFXR2 Broken Heart"), ("sparks", "CFXR2 Sparks Rain"), ("ripples", "CFXR Water Ripples"),
            };
            const string dir = "Assets/SHHH/Resources/VFX";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/SHHH/Resources", "VFX");
            var log = new System.Text.StringBuilder();
            foreach (var (key, name) in map)
            {
                var path = AssetDatabase.FindAssets("t:Prefab " + name).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == name);
                if (path == null) { log.Append(key).Append(": MISSING ").Append(name).Append('\n'); continue; }
                var dst = $"{dir}/vfx_{key}.prefab";
                AssetDatabase.DeleteAsset(dst);
                AssetDatabase.CopyAsset(path, dst);
                log.Append(key).Append(" <- ").Append(path).Append('\n');
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        /// <summary>Renders every Resources/VFX prefab mid-effect into QA_Evidence/vfx_gallery.png (detects pink/unsupported shaders).</summary>
        [ShhhCommand("vfx_gallery")]
        public static string Gallery()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var camGo = new UnityEngine.GameObject("cam"); var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = new UnityEngine.Color(0.1f, 0.09f, 0.25f);
            camGo.transform.position = new UnityEngine.Vector3(0, 1.0f, -4.2f);
            var light = new UnityEngine.GameObject("light").AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Directional; light.transform.rotation = UnityEngine.Quaternion.Euler(50, -30, 0);
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SHHH/Resources/VFX" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x).ToArray();
            var log = new System.Text.StringBuilder(); int i = 0;
            foreach (var p in prefabs)
            {
                var go = (UnityEngine.GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p));
                float x = -3.2f + (i % 5) * 1.6f, y = i < 5 ? 1.8f : 0.2f; i++;
                go.transform.position = new UnityEngine.Vector3(x, y, 0); go.transform.localScale = UnityEngine.Vector3.one * 0.5f;
                foreach (var ps in go.GetComponentsInChildren<UnityEngine.ParticleSystem>()) ps.Simulate(0.45f, true, true);
                var shaders = go.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.shader.name).Distinct();
                log.Append(Path.GetFileNameWithoutExtension(p)).Append(": ").Append(string.Join(", ", shaders)).Append('\n');
            }
            var rt = new UnityEngine.RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            UnityEngine.RenderTexture.active = rt; var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false); tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            var outp = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "..", "QA_Evidence", "vfx_gallery.png"));
            File.WriteAllBytes(outp, UnityEngine.ImageConversion.EncodeToPNG(tex)); UnityEngine.RenderTexture.active = null; cam.targetTexture = null;
            return "saved " + outp + "\n" + log;
        }

        /// <summary>Quest-safe VFX materials: our camera has no depth texture, so soft-particle fades must be off (else effects are invisible);
        /// built-in-only shaders (pink in URP) are swapped for URP Particles/Unlit keeping the main texture and colour. Materials are cloned under
        /// Assets/SHHH/VFX/Materials so the vendor files stay untouched.</summary>
        [ShhhCommand("vfx_fix_materials")]
        public static string FixMaterials()
        {
            const string matDir = "Assets/SHHH/VFX/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/SHHH/VFX")) AssetDatabase.CreateFolder("Assets/SHHH", "VFX");
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/SHHH/VFX", "Materials");
            var urpParticle = UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var log = new System.Text.StringBuilder();
            var clones = new System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material>();
            foreach (var p in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SHHH/Resources/VFX" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var root = PrefabUtility.LoadPrefabContents(p);
                bool dirty = false;
                foreach (var r in root.GetComponentsInChildren<UnityEngine.Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i]; if (m == null) continue;
                        if (!clones.TryGetValue(m, out var c))
                        {
                            c = new UnityEngine.Material(m) { name = m.name + "_Q" };
                            var kws = c.shaderKeywords.Where(k => k.ToUpperInvariant().Contains("SOFT")).ToArray();
                            foreach (var k in kws) c.DisableKeyword(k);
                            foreach (var prop in new[] { "_UseSP", "_SoftParticlesEnabled", "_SoftParticles", "_UseSoftParticles" }) if (c.HasProperty(prop)) c.SetFloat(prop, 0f);
                            bool unsupported = !c.shader.isSupported || c.shader.name.Contains("BIRP") || c.shader.name.StartsWith("Legacy") ;
                            if (unsupported && urpParticle != null)
                            {
                                var tex = c.HasProperty("_MainTex") ? c.GetTexture("_MainTex") : null;
                                var col = c.HasProperty("_Color") ? c.GetColor("_Color") : UnityEngine.Color.white;
                                c.shader = urpParticle;
                                c.SetFloat("_Surface", 1f); c.SetFloat("_Blend", 0f);
                                if (tex != null) c.SetTexture("_BaseMap", tex);
                                c.SetColor("_BaseColor", col);
                            }
                            var dst = $"{matDir}/{c.name.Replace('/', '_')}.mat";
                            AssetDatabase.DeleteAsset(dst); AssetDatabase.CreateAsset(c, dst);
                            clones[m] = c;
                            log.Append(m.name).Append(" [").Append(m.shader.name).Append("] -soft:").Append(kws.Length).Append(unsupported ? " ->URP Particles/Unlit" : "").Append('\n');
                        }
                        mats[i] = c; dirty = true;
                    }
                    r.sharedMaterials = mats;
                }
                if (dirty) PrefabUtility.SaveAsPrefabAsset(root, p);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        /// <summary>Undo an accidental vendor import: moves an Assets/ folder to the OS recycle bin (recoverable).</summary>
        [ShhhCommand("trash_asset")]
        public static string Trash(string path)
        {
            path = path.Trim();
            if (!path.StartsWith("Assets/") || path.StartsWith("Assets/SHHH")) throw new Exception("refusing: " + path);
            return AssetDatabase.MoveAssetToTrash(path) ? "moved to recycle bin: " + path : "failed: " + path;
        }

        [ShhhCommand("vfx_gallery_play")]
        public static string GalleryPlay()
        {
            File.WriteAllText(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "..", "Automation", "vfxgallery.flag")), "1");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SHHH/Networking/Scenes/NetSmokeB.unity");
            EditorApplication.isPlaying = true;
            return "vfx gallery play started";
        }

        /// <summary>args = path relative to the cache (exactly as listed). Interactive=false import.</summary>
        [ShhhCommand("store_import")]
        public static string Import(string rel)
        {
            var p = Path.Combine(Cache, rel.Trim());
            if (!File.Exists(p) || !p.EndsWith(".unitypackage")) throw new Exception("not a cached package: " + rel);
            AssetDatabase.ImportPackage(p, false);
            return "import queued: " + rel;
        }
    }
}
