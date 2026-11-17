using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Editor-only automated play-test hook: when Automation/autopilot.flag exists, the Boot scene runs a full solo match
    /// with the human seat driven by a bot body, and milestones (rooms loaded, eliminations, winner) are written to
    /// Automation/playtest_status.json as executable evidence that the Unity layer completes 4 -> 3 -> 2 -> 1.
    /// </summary>
    public static class QAReporter
    {
#if UNITY_EDITOR
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation"));
        static readonly List<string> _events = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _events.Clear();
            GameRoot.Autopilot = File.Exists(Path.Combine(Dir, "autopilot.flag"));
            if (!GameRoot.Autopilot) return;
            Application.logMessageReceived += (c, s, t) =>
            {
                if (t == LogType.Exception || t == LogType.Error) { Add("ERROR " + c); return; }
                // V7 milestones from the round log: recipes, sabotage, confrontation, verdicts
                if (c.StartsWith("[SHHH]") && (c.Contains("RECIPE DONE") || c.Contains("SABOTAGE") || c.Contains("FOILED") || c.Contains("CONFRONTATION") || c.Contains("CARD ") || c.Contains("POINT ") || c.Contains("VERDICT") || c.Contains("MOM WAKES") || c.Contains("WITNESS")))
                    Add(c.Length > 260 ? c.Substring(0, 260) : c);
            };
            var go = new GameObject("QAReporterHook"); Object.DontDestroyOnLoad(go); go.AddComponent<Hook>();
            Write("starting");
        }

        public static void Add(string e) { _events.Add($"{Time.realtimeSinceStartup:0.0}s {e}"); Write(null); }

        static void Write(string state)
        {
            try
            {
                var sb = new System.Text.StringBuilder("{\n  \"events\": [\n");
                for (int i = 0; i < _events.Count; i++) sb.Append("    \"").Append(_events[i].Replace("\\", "/").Replace("\"", "'")).Append(i < _events.Count - 1 ? "\",\n" : "\"\n");
                sb.Append("  ]\n}\n");
                File.WriteAllText(Path.Combine(Dir, "playtest_status.json"), sb.ToString());
            }
            catch { }
        }

        sealed class Hook : MonoBehaviour
        {
            bool _hooked; float _nextMem; float _stopAt = -1f;
            const long MemLimit = 6L * 1024 * 1024 * 1024; // safety: abort the automated test above 6 GB reserved (editor included; V7 house + recipes)
            void Update()
            {
                float now = Time.realtimeSinceStartup;
                if (now >= _nextMem)
                {
                    _nextMem = now + 10f;
                    long reserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
                    long mono = System.GC.GetTotalMemory(false);
                    Add($"mem reserved={reserved / 1048576}MB mono={mono / 1048576}MB fps={1f / Mathf.Max(1e-4f, Time.unscaledDeltaTime):0}");
                    if (reserved > MemLimit) { Add("ABORT: memory limit exceeded"); Stop(); }
                }
                if (now > 900f && _stopAt < 0) { Add("ABORT: 15 minute limit"); Stop(); }
                if (_stopAt > 0 && now >= _stopAt) Stop();
                if (_hooked || GameRoot.I == null) return;
                _hooked = true;
                GameRoot.I.Milestone += m => { Add(m); if (m.StartsWith("winner")) _stopAt = Time.realtimeSinceStartup + 6f; };
                Add("hooked");
            }
            static void Stop()
            {
                try { File.Delete(Path.Combine(Dir, "autopilot.flag")); } catch { }
                Add("stopping play mode");
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
#else
        public static void Add(string e) { }
#endif
    }
}
