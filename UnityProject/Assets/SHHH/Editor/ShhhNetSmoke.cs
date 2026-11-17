using System.IO;
using System.Linq;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Builds the Photon Fusion smoke-test assets and runs the two-peer test in Play mode (multi-peer).</summary>
    public static class ShhhNetSmoke
    {
        const string Dir = "Assets/SHHH/Networking";
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string ConfigPath => AssetDatabase.FindAssets("NetworkProjectConfig").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith(".fusion"));

        [ShhhCommand("net_setup")]
        public static string Setup()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SHHH/Networking/Resources"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SHHH/Networking/Scenes"));
            AssetDatabase.Refresh();
            MakePrefab("NetRigPlayer", "SHHH.Net.NetRigPlayer");
            MakePrefab("NetSharedProp", "SHHH.Net.NetSharedProp");
            foreach (var n in new[] { "NetSmokeA", "NetSmokeB" })
            {
                var path = $"{Dir}/Scenes/{n}.unity";
                if (File.Exists(Path.Combine(Repo, "UnityProject", path))) continue;
                var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var l = new GameObject("Light").AddComponent<Light>(); l.type = LightType.Directional;
                new GameObject(n + "_Marker");
                EditorSceneManager.SaveScene(sc, path);
            }
            var list = EditorBuildSettings.scenes.ToList();
            foreach (var n in new[] { "NetSmokeA", "NetSmokeB" })
            { var p = $"{Dir}/Scenes/{n}.unity"; if (!list.Any(s => s.path == p)) list.Add(new EditorBuildSettingsScene(p, true)); }
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            return "net prefabs + scenes ready; build scenes: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)));
        }

        static void MakePrefab(string name, string typeName)
        {
            var path = $"{Dir}/Resources/{name}.prefab";
            var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).FirstOrDefault(x => x != null);
            if (t == null) throw new System.Exception("type not compiled: " + typeName);
            var go = new GameObject(name);
            var noType = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Fusion.NetworkObject")).First(x => x != null);
            go.AddComponent(noType);
            go.AddComponent(t);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void SetPeerMode(int mode)
        {
            var p = ConfigPath; if (p == null) throw new System.Exception("NetworkProjectConfig.fusion not found");
            var full = Path.Combine(Repo, "UnityProject", p);
            var txt = File.ReadAllText(full);
            txt = System.Text.RegularExpressions.Regex.Replace(txt, "\"PeerMode\"\\s*:\\s*\\d", "\"PeerMode\": " + mode);
            File.WriteAllText(full, txt);
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>args: multipeer (default). Sets Fusion PeerMode=Multiple, opens NetSmokeA, enters Play mode.</summary>
        [ShhhCommand("net_smoke")]
        public static string Run(string args)
        {
            string mode = string.IsNullOrWhiteSpace(args) ? "multipeer" : args.Trim();
            var res = Path.Combine(Repo, "Automation", "net_smoke_result.json"); if (File.Exists(res)) File.Delete(res);
            var auto = Path.Combine(Repo, "Automation", "autopilot.flag"); if (File.Exists(auto)) File.Delete(auto);
            if (mode == "multipeer") SetPeerMode(1);
            File.WriteAllText(Path.Combine(Repo, "Automation", "netsmoke.flag"), mode);
            EditorSceneManager.OpenScene($"{Dir}/Scenes/NetSmokeA.unity");
            PlayerSettings.runInBackground = true;
            EditorApplication.isPlaying = true;
            return "net smoke started (" + mode + "); poll Automation/net_smoke_result.json";
        }

        /// <summary>Force Fusion to (re)bake network prefabs: reimport them; also invoke any Fusion "BakeAll" editor entry point found.</summary>
        [ShhhCommand("net_bake")]
        public static string Bake()
        {
            foreach (var n in new[] { "NetRigPlayer", "NetSharedProp" }) AssetDatabase.ImportAsset($"{Dir}/Resources/{n}.prefab", ImportAssetOptions.ForceUpdate);
            var log = new System.Text.StringBuilder();
            var pp = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Fusion.Editor.NetworkObjectPostprocessor")).FirstOrDefault(x => x != null);
            var bm = pp?.GetMethod("BakePrefab", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (bm != null)
                foreach (var n in new[] { "NetRigPlayer", "NetSharedProp" })
                {
                    var args = new object[] { $"{Dir}/Resources/{n}.prefab", null };
                    try { var ok = bm.Invoke(null, args); log.Append(n).Append(" BakePrefab=").Append(ok).Append("; "); }
                    catch (System.Exception e) { log.Append(n).Append(" bake err ").Append(e.InnerException?.Message ?? e.Message).Append("; "); }
                }
            else log.Append("BakePrefab not found; ");
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("Fusion.Unity.Editor")))
                foreach (var t in asm.GetTypes().Where(t => t.Name.Contains("Baker") || t.Name.Contains("Bake")))
                    foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Where(m => m.GetParameters().Length == 0 && (m.Name.Contains("BakeAll") || m.Name.Contains("BakeAllNetworkObjects"))))
                    { try { m.Invoke(null, null); log.Append(t.Name).Append('.').Append(m.Name).Append(" ok; "); } catch (System.Exception e) { log.Append(t.Name).Append('.').Append(m.Name).Append(" err ").Append(e.InnerException?.Message ?? e.Message).Append("; "); } }
            AssetDatabase.SaveAssets();
            return "rebake: " + (log.Length > 0 ? log.ToString() : "reimport only");
        }

        /// <summary>args "line;count": raw Editor.log lines (current session) for diagnosing exceptions.</summary>
        [ShhhCommand("log_range")]
        public static string LogRange(string args)
        {
            var parts = new System.Collections.Generic.List<string>(args.Split(';')); string file = "Editor.log";
            if (parts[0] == "prev" || parts[0] == "cur") { if (parts[0] == "prev") file = "Editor-prev.log"; parts.RemoveAt(0); }
            int start = int.Parse(parts[0]); int count = parts.Count > 1 ? int.Parse(parts[1]) : 30;
            var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", file);
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) using (var sr = new StreamReader(fs))
            {
                var sb = new System.Text.StringBuilder(); int i = 0; string l;
                while ((l = sr.ReadLine()) != null) { i++; if (i >= start && i < start + count) sb.Append(i).Append(": ").Append(l).Append('\n'); if (i >= start + count) break; }
                return sb.ToString();
            }
        }

        /// <summary>args "cur|prev": copy Editor.log / Editor-prev.log to Automation/logs/ so it can be searched outside Unity (e.g. after a crash).</summary>
        [ShhhCommand("log_copy")]
        public static string LogCopy(string args)
        {
            var file = (args ?? "").Trim() == "prev" ? "Editor-prev.log" : "Editor.log";
            var src = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", file);
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Automation/logs")); Directory.CreateDirectory(dir);
            var dst = Path.Combine(dir, file);
            using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) using (var o = File.Create(dst)) fs.CopyTo(o);
            return "copied " + file + " -> Automation/logs/" + file + " (" + new FileInfo(dst).Length + " bytes)";
        }

        /// <summary>Full script recompile (needed after changing Fusion's AssembliesToWeave so package assemblies get woven).</summary>
        [ShhhCommand("recompile_clean")]
        public static string RecompileClean()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
            return "clean recompile requested";
        }

        [ShhhCommand("net_smoke_stop")]
        public static string Stop()
        {
            var flag = Path.Combine(Repo, "Automation", "netsmoke.flag"); if (File.Exists(flag)) File.Delete(flag);
            EditorApplication.isPlaying = false;
            SetPeerMode(0);
            return "stopped; PeerMode restored to Single";
        }
    }
}
