using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>
    /// V6 NET-G4 tooling: network prefabs for the real game session, a Windows standalone test player, and the
    /// independent-process multiplayer test (1 host + N-1 clients, each its own OS process, real Photon Cloud).
    /// </summary>
    public static class ShhhNetGame
    {
        const string Dir = "Assets/SHHH/Networking/Resources";
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string WinExe => Path.Combine(Repo, "Builds", "Win64", "SHHH.exe");
        static string MultiDir => Path.Combine(Repo, "Automation", "net_multi");

        static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(x => x != null) ?? throw new Exception("type not compiled: " + name);

        [ShhhCommand("net_game_setup")]
        public static string Setup()
        {
            Directory.CreateDirectory(Path.Combine(Repo, "UnityProject", Dir));
            AssetDatabase.Refresh();
            Make("NetSession", "SHHH.Net.NetLobby", "SHHH.Net.NetWorld");
            Make("NetHuman", "SHHH.Net.NetHuman", "Photon.Voice.Fusion.VoiceNetworkObject");
            // positional voice: Speaker + AudioSource child at the head
            var hp = $"{Dir}/NetHuman.prefab";
            var root = PrefabUtility.LoadPrefabContents(hp);
            var sp = new GameObject("VoiceSpeaker"); sp.transform.SetParent(root.transform, false);
            var src = sp.AddComponent<AudioSource>(); src.spatialBlend = 1f; src.minDistance = 0.6f; src.maxDistance = 9f; src.rolloffMode = AudioRolloffMode.Linear; src.playOnAwake = false; src.dopplerLevel = 0f;
            sp.AddComponent(T("Photon.Voice.Unity.Speaker"));
            PrefabUtility.SaveAsPrefabAsset(root, hp); PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            return "NetSession + NetHuman prefabs written; now run net_bake_game";
        }

        static void Make(string name, params string[] types)
        {
            var path = $"{Dir}/{name}.prefab";
            var go = new GameObject(name);
            go.AddComponent(T("Fusion.NetworkObject"));
            foreach (var t in types) go.AddComponent(T(t));
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [ShhhCommand("net_bake_game")]
        public static string Bake()
        {
            var log = new StringBuilder();
            var pp = T("Fusion.Editor.NetworkObjectPostprocessor");
            var bm = pp.GetMethod("BakePrefab", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var n in new[] { "NetSession", "NetHuman", "NetRigPlayer", "NetSharedProp" })
            {
                var p = $"Assets/SHHH/Networking/Resources/{n}.prefab";
                if (!File.Exists(Path.Combine(Repo, "UnityProject", p))) continue;
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                if (bm != null) { try { log.Append(n).Append(" bake=").Append(bm.Invoke(null, new object[] { p, null })).Append("; "); } catch (Exception e) { log.Append(n).Append(" err ").Append(e.InnerException?.Message ?? e.Message).Append("; "); } }
            }
            // Fusion 2 keeps a prefab table in NetworkProjectConfig; new prefabs must be added (menu: Tools/Fusion/Rebuild Prefab Table).
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Fusion")))
            {
                Type[] types; try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                    foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Where(m => m.Name == "RebuildPrefabTable" && m.GetParameters().Length == 0))
                    { try { m.Invoke(null, null); log.Append(t.Name).Append(".RebuildPrefabTable ok; "); } catch (Exception e) { log.Append(t.Name).Append(" rebuild err ").Append(e.InnerException?.Message ?? e.Message).Append("; "); } }
            }
            AssetDatabase.SaveAssets();
            return log.Length > 0 ? log.ToString() : "reimported";
        }

        /// <summary>Windows x64 standalone player of the SAME game (for independent network clients). Switches back to Android.</summary>
        [ShhhCommand("build_win")]
        public static string BuildWin()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WinExe));
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = WinExe, target = BuildTarget.StandaloneWindows64, targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
            };
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); AssetDatabase.SaveAssets(); }
            PlayerSettings.runInBackground = true;
            // the existing Win64 folder is a Mono build (fast iteration for network QA clients); keep it Mono, restore the setting afterwards
            var nbt = UnityEditor.Build.NamedBuildTarget.Standalone; var prevBackend = PlayerSettings.GetScriptingBackend(nbt);
            if (prevBackend != ScriptingImplementation.Mono2x) PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.Mono2x);
            var sw = Stopwatch.StartNew();
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(opts); }
            finally { if (prevBackend != ScriptingImplementation.Mono2x) PlayerSettings.SetScriptingBackend(nbt, prevBackend); }
            var s = report.summary;
            string msg = $"{s.result} in {sw.Elapsed.TotalSeconds:0}s, {s.totalSize / (1024 * 1024)} MB, errors={s.totalErrors} -> {WinExe}";
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            if (s.result != BuildResult.Succeeded)
            {
                var errs = report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error).Select(m => m.content).Take(10);
                throw new Exception(msg + "\n" + string.Join("\n", errs));
            }
            return msg;
        }

        /// <summary>
        /// args "humans[;code]" (2-4): launches 1 host + humans-1 client players as separate headless processes against the
        /// real Photon Cloud. Each writes Automation/net_multi/&lt;role&gt;.json + .log. Poll with net_multi_status.
        /// </summary>
        [ShhhCommand("net_multi")]
        public static string Multi(string args)
        {
            if (!File.Exists(WinExe)) throw new Exception("build_win first: " + WinExe);
            var a = (args ?? "").Split(';');
            int humans = a.Length > 0 && int.TryParse(a[0], out int h) ? Mathf.Clamp(h, 2, 4) : 2;
            string code = a.Length > 1 && a[1].Length == 4 ? a[1].ToUpperInvariant() : RandomCode();
            int batchClients = a.Length > 2 && int.TryParse(a[2], out int bc) ? Mathf.Clamp(bc, 0, humans - 1) : humans - 1;
            // "windowed": small visible player windows instead of headless ones - the Genies SDK only runs with graphics, so this is
            // how the network test sees real Genies bodies driven by the other player's replicated head + hands
            _windowed = (args ?? "").Contains("windowed");   // fewer = the Editor joins as a client (net_auto_editor)
            Directory.CreateDirectory(MultiDir);
            foreach (var f in Directory.GetFiles(MultiDir)) File.Delete(f);
            var sb = new StringBuilder($"code {code}, humans {humans}: ");
            // Genies looks for the batch peers (no SDK there - they only carry the definition strings): the starter presets the Editor built
            string look = "";
            try
            {
                var pf = Path.Combine(Application.persistentDataPath, "shhh_genies_presets_v2.json");
                if (File.Exists(pf))
                {
                    var defs = JsonUtility.FromJson<LookCache>(File.ReadAllText(pf)).defs;
                    for (int i = 0; i < humans && defs != null; i++)
                    {
                        var d = defs[(i + 2) % defs.Length]; if (string.IsNullOrEmpty(d)) continue;
                        var lf = Path.Combine(MultiDir, $"look_{i}.json"); File.WriteAllText(lf, d);
                    }
                }
            }
            catch (Exception e) { sb.Append("(looks: " + e.Message + ") "); }
            string L(int i) { var lf = Path.Combine(MultiDir, $"look_{i}.json"); return File.Exists(lf) ? $" -shhhLook=\"{lf}\"" : ""; }
            Launch("host", $"-shhhAuto=host:{code}:{humans}" + L(0), sb);
            for (int i = 1; i <= batchClients; i++) Launch("client" + i, $"-shhhAuto=client:{code}:Kid{i}" + L(i), sb);
            File.WriteAllText(Path.Combine(MultiDir, "session.txt"), $"code={code}\nhumans={humans}\nstarted={DateTime.UtcNow:o}\n");
            return sb.ToString();
        }

        static bool _windowed;
        static void Launch(string role, string auto, StringBuilder sb)
        {
            string mode = _windowed ? "-screen-fullscreen 0 -screen-width 480 -screen-height 270" : "-batchmode -nographics";
            var psi = new ProcessStartInfo(WinExe, $"{mode} {auto} -shhhOut=\"{Path.Combine(MultiDir, role + ".json")}\" -logFile \"{Path.Combine(MultiDir, role + ".log")}\"")
            { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(WinExe) };
            var p = Process.Start(psi);
            sb.Append($"{role}=pid {p?.Id}; ");
        }

        [Serializable] sealed class LookCache { public string[] defs; }

        static string RandomCode() { var r = new System.Random(); const string A = "ABCDEFGHJKMNPQRSTUVWXYZ"; var c = new char[4]; for (int i = 0; i < 4; i++) c[i] = A[r.Next(A.Length)]; return new string(c); }

        [ShhhCommand("net_multi_status")]
        public static string Status()
        {
            if (!Directory.Exists(MultiDir)) return "no run";
            var sb = new StringBuilder();
            foreach (var f in Directory.GetFiles(MultiDir).OrderBy(x => x)) sb.Append(Path.GetFileName(f)).Append(' ').Append(new FileInfo(f).Length).Append("B\n");
            var procs = Process.GetProcessesByName("SHHH");
            sb.Append("running SHHH.exe processes: ").Append(procs.Length);
            return sb.ToString();
        }

        [ShhhCommand("net_multi_kill")]
        public static string Kill()
        {
            int n = 0; foreach (var p in Process.GetProcessesByName("SHHH")) { try { p.Kill(); n++; } catch { } }
            return "killed " + n;
        }

        /// <summary>Editor-as-host variant (play mode): args "host:CODE:HUMANS" written to the netauto flag.</summary>
        [ShhhCommand("net_auto_editor")]
        public static string EditorAuto(string args)
        {
            File.WriteAllText(Path.Combine(Repo, "Automation", "netauto.flag"), args); ShhhKeepVisible.Arm();
            EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", OpenSceneMode.Single);
            PlayerSettings.runInBackground = true;
            EditorApplication.EnterPlaymode();
            return "editor netauto started: " + args;
        }
    }
}
