// SHHH! Editor automation bridge.
// Lets the production agent drive this Unity Editor without anyone clicking in it: a JSON job dropped into
// <repo>/Automation/inbox is executed (only commands explicitly registered with [ShhhCommand]), and the result is
// written to <repo>/Automation/outbox. A heartbeat + compile report is written every few seconds.
// This assembly has NO dependency on game code so it keeps working while game scripts are being fixed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SHHH.Automation
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ShhhCommandAttribute : Attribute
    {
        public readonly string Name;
        public ShhhCommandAttribute(string name) { Name = name; }
    }

    [Serializable]
    public sealed class ShhhJob { public string id; public string cmd; public string args; }

    [Serializable]
    sealed class ShhhResult
    {
        public string id, cmd, args, status, message, startedUtc, finishedUtc;
        public double seconds;
        public string[] log;
    }

    [Serializable]
    sealed class Heartbeat
    {
        public string utc, unity, project, activeScene, buildTarget, currentJob;
        public bool compiling, updating, playing, lastCompileOk;
        public int compileErrors;
        public string[] commands;
    }

    [Serializable]
    sealed class CompileReport { public string utc; public bool ok; public string[] errors; public string[] warnings; }

    [InitializeOnLoad]
    public static class ShhhAutomationBridge
    {
        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        public static string AutomationDir => Path.Combine(RepoRoot, "Automation");
        static string Inbox => Path.Combine(AutomationDir, "inbox");
        static string Outbox => Path.Combine(AutomationDir, "outbox");
        static string Done => Path.Combine(AutomationDir, "done");

        static double _nextPoll, _nextBeat;
        static string _currentJob;
        static readonly List<string> _capture = new List<string>();
        static bool _capturing;
        static Dictionary<string, MethodInfo> _commands;
        static readonly List<string> _compileErrors = new List<string>();
        static readonly List<string> _compileWarnings = new List<string>();
        static bool _lastCompileOk = true;

        static ShhhAutomationBridge()
        {
            try
            {
                Directory.CreateDirectory(Inbox); Directory.CreateDirectory(Outbox); Directory.CreateDirectory(Done);
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
                CompilationPipeline.compilationStarted -= OnCompileStarted; CompilationPipeline.compilationStarted += OnCompileStarted;
                CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompiled; CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
                CompilationPipeline.compilationFinished -= OnCompileFinished; CompilationPipeline.compilationFinished += OnCompileFinished;
                Application.logMessageReceivedThreaded -= OnLog; Application.logMessageReceivedThreaded += OnLog;
                // Don't throttle the editor loop when it is in the background; jobs must keep running unattended.
                try { EditorPrefs.SetInt("InteractionMode", 1); } catch { }
                WriteHeartbeat();
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        static void OnLog(string condition, string stack, LogType type)
        {
            if (!_capturing) return;
            lock (_capture)
            {
                string prefix = type == LogType.Log ? "" : "[" + type + "] ";
                _capture.Add(prefix + condition + ((type == LogType.Exception || type == LogType.Error) && !string.IsNullOrEmpty(stack) ? "\n" + stack : ""));
                if (_capture.Count > 3000) _capture.RemoveRange(0, 500);
            }
        }

        static void OnCompileStarted(object o) { _compileErrors.Clear(); _compileWarnings.Clear(); }

        static void OnAssemblyCompiled(string asm, CompilerMessage[] msgs)
        {
            foreach (var m in msgs)
            {
                string line = $"{m.file}({m.line},{m.column}): {m.message}";
                if (m.type == CompilerMessageType.Error) _compileErrors.Add(line);
                else if (_compileWarnings.Count < 200) _compileWarnings.Add(line);
            }
        }

        static void OnCompileFinished(object o)
        {
            _lastCompileOk = _compileErrors.Count == 0;
            try
            {
                var rep = new CompileReport { utc = DateTime.UtcNow.ToString("o"), ok = _lastCompileOk, errors = _compileErrors.ToArray(), warnings = _compileWarnings.ToArray() };
                File.WriteAllText(Path.Combine(AutomationDir, "compile_report.json"), JsonUtility.ToJson(rep, true));
            }
            catch { }
        }

        static Dictionary<string, MethodInfo> Commands
        {
            get
            {
                if (_commands != null) return _commands;
                _commands = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var mi in TypeCache.GetMethodsWithAttribute<ShhhCommandAttribute>())
                {
                    if (!mi.IsStatic) continue;
                    var a = mi.GetCustomAttribute<ShhhCommandAttribute>();
                    _commands[a.Name] = mi;
                }
                return _commands;
            }
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now >= _nextBeat) { _nextBeat = now + 3; WriteHeartbeat(); }
            if (now < _nextPoll) return;
            _nextPoll = now + 1.0;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || _currentJob != null) return;
            string file = null;
            try { file = Directory.GetFiles(Inbox, "*.json").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault(); } catch { }
            if (file == null) return;
            RunJobFile(file);
        }

        static void RunJobFile(string file)
        {
            ShhhJob job;
            try { job = JsonUtility.FromJson<ShhhJob>(File.ReadAllText(file)); }
            catch (Exception e) { job = new ShhhJob { id = Path.GetFileNameWithoutExtension(file), cmd = "invalid", args = e.Message }; }
            if (string.IsNullOrEmpty(job.id)) job.id = Path.GetFileNameWithoutExtension(file);
            try { File.Move(file, Path.Combine(Done, Path.GetFileName(file) + "." + DateTime.UtcNow.Ticks)); } catch { try { File.Delete(file); } catch { } }

            var res = new ShhhResult { id = job.id, cmd = job.cmd, args = job.args, startedUtc = DateTime.UtcNow.ToString("o") };
            _currentJob = job.id + ":" + job.cmd;
            WriteResult(res, "running", "");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            lock (_capture) _capture.Clear();
            _capturing = true;
            try
            {
                if (!Commands.TryGetValue(job.cmd ?? "", out var mi)) throw new Exception("Unknown command '" + job.cmd + "'. Known: " + string.Join(", ", Commands.Keys));
                object ret = mi.GetParameters().Length == 1 ? mi.Invoke(null, new object[] { job.args ?? "" }) : mi.Invoke(null, null);
                res.status = "ok"; res.message = ret?.ToString() ?? "";
            }
            catch (Exception e)
            {
                var inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                res.status = "error"; res.message = inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace;
            }
            finally
            {
                _capturing = false;
                res.seconds = sw.Elapsed.TotalSeconds;
                _currentJob = null;
                WriteResult(res, res.status, res.message);
            }
        }

        static void WriteResult(ShhhResult res, string status, string message)
        {
            try
            {
                res.status = status; res.message = message;
                res.finishedUtc = status == "running" ? null : DateTime.UtcNow.ToString("o");
                lock (_capture) res.log = _capture.ToArray();
                File.WriteAllText(Path.Combine(Outbox, res.id + ".json"), JsonUtility.ToJson(res, true));
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        static void WriteHeartbeat()
        {
            try
            {
                var hb = new Heartbeat
                {
                    utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion, project = Application.dataPath,
                    activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    compiling = EditorApplication.isCompiling, updating = EditorApplication.isUpdating, playing = EditorApplication.isPlaying,
                    lastCompileOk = _lastCompileOk, compileErrors = _compileErrors.Count, currentJob = _currentJob,
                    commands = Commands.Keys.OrderBy(k => k).ToArray()
                };
                File.WriteAllText(Path.Combine(AutomationDir, "heartbeat.json"), JsonUtility.ToJson(hb, true));
            }
            catch { }
        }

        // ------------------------------------------------------------------ built-in commands
        [ShhhCommand("ping")] static string Ping() => "pong " + Application.unityVersion;

        [ShhhCommand("refresh")]
        static string Refresh()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
            return "refresh + compile requested";
        }

        [ShhhCommand("list_commands")] static string ListCommands() => string.Join("\n", Commands.Keys.OrderBy(k => k));

        [ShhhCommand("save_all")] static string SaveAll() { AssetDatabase.SaveAssets(); UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); return "saved"; }

        /// <summary>Copies the engine reference assemblies into Tools/UnityRefs so code can be compile-checked outside Unity.</summary>
        [ShhhCommand("export_engine_refs")]
        static string ExportEngineRefs()
        {
            string dst = Path.Combine(RepoRoot, "Tools", "UnityRefs");
            Directory.CreateDirectory(dst);
            string managed = Path.Combine(EditorApplication.applicationContentsPath, "Managed");
            int n = 0;
            foreach (var f in Directory.GetFiles(Path.Combine(managed, "UnityEngine"), "*.dll")) { File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true); n++; }
            foreach (var f in Directory.GetFiles(Path.Combine(RepoRoot, "UnityProject", "Library", "ScriptAssemblies"), "*.dll")) { File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true); n++; }
            var sb = new StringBuilder();
            foreach (var asm in CompilationPipeline.GetAssemblies(AssembliesType.Player)) sb.AppendLine(asm.name + " :: " + string.Join(";", asm.defines.Take(5)));
            File.WriteAllText(Path.Combine(dst, "assemblies.txt"), sb.ToString());
            return "copied " + n + " dlls";
        }

        /// <summary>Last N lines of the Editor log (for diagnosing imports/crashes).</summary>
        [ShhhCommand("editor_log_tail")]
        static string EditorLogTail(string args)
        {
            int n = 200; int.TryParse(args, out n); if (n <= 0) n = 200;
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
            if (!File.Exists(path)) return "no log at " + path;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var lines = sr.ReadToEnd().Split('\n');
                return string.Join("\n", lines.Skip(Math.Max(0, lines.Length - n)));
            }
        }
    }
}
