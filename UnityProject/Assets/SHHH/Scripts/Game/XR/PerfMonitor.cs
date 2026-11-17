using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (owner, Quest c15: "a lot of vibration when moving or turning my head - nothing is stable"). Head motion never moves the rig
    /// and the body does not shake (recipe_diag stable12), so the suspect is the headset missing frames: every missed frame is
    /// re-projected and the world judders on every head turn. This measures it on the device: every 5 s one line in the grab log
    /// (persistentDataPath/shhh_grablog.txt) - average / worst frame rate, frames over budget, skinned meshes alive - and, while the
    /// frame rate is low, a small "NN fps" on the task strip. It also switches on the Quest's (dynamic) fixed foveated rendering.
    /// </summary>
    public sealed class PerfMonitor : MonoBehaviour
    {
        public static PerfMonitor I { get; private set; }
        public float AvgFps { get; private set; } = 72f;
        public float WorstFrameMs { get; private set; }
        public int SlowFrames { get; private set; }
        public bool Low => AvgFps < 66f;
        float _acc, _worst; int _frames, _slow; float _next;
        bool _metrics; float _cpuAcc, _gpuAcc, _cpuMax, _gpuMax; int _mN;
        /// <summary>Average app CPU / GPU frame time (ms) over the last 5 s, from the Meta runtime (0 = not available).</summary>
        public float CpuMs { get; private set; }
        public float GpuMs { get; private set; }

        static float Ms(float? v) => v.HasValue ? (v.Value < 1f ? v.Value * 1000f : v.Value) : 0f;   // the runtime reports seconds

        public static void Ensure()
        {
            if (I != null) return;
            var g = new GameObject("PerfMonitor"); DontDestroyOnLoad(g); I = g.AddComponent<PerfMonitor>();
            try
            {
                if (OVRManager.fixedFoveatedRenderingSupported)
                {
                    OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;
                    OVRManager.useDynamicFoveatedRendering = true;
                    GrabLog.Add("perf foveated rendering: High, dynamic");
                }
                else GrabLog.Add("perf foveated rendering: not supported here");
            }
            catch (System.Exception e) { GrabLog.Add("perf foveated rendering: " + e.Message); }
            // V13 (owner, Quest c20: "the vibrations appear again, I think fps"): the app never asked for higher clocks, so the headset ran
            // its default (lower) CPU / GPU levels. Sustained-high is the level Meta allows apps to hold indefinitely.
            try
            {
                OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
                OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
                GrabLog.Add("perf cpu / gpu level: sustained high");
            }
            catch (System.Exception e) { GrabLog.Add("perf cpu / gpu level: " + e.Message); }
            try { I._metrics = OVRPlugin.IsPerfMetricsSupported(OVRPlugin.PerfMetrics.App_CpuTime_Float) && OVRPlugin.IsPerfMetricsSupported(OVRPlugin.PerfMetrics.App_GpuTime_Float); } catch { I._metrics = false; }
            GrabLog.Add("perf app cpu / gpu timing: " + (I._metrics ? "available" : "not available"));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime; if (dt <= 0f) return;
            _acc += dt; _frames++; if (dt > _worst) _worst = dt; if (dt > 1.5f / 72f) _slow++;
            if (_metrics)
            {
                float c = 0f, g = 0f;
                try { c = Ms(OVRPlugin.GetPerfMetricsFloat(OVRPlugin.PerfMetrics.App_CpuTime_Float)); g = Ms(OVRPlugin.GetPerfMetricsFloat(OVRPlugin.PerfMetrics.App_GpuTime_Float)); } catch { _metrics = false; }
                if (c > 0f || g > 0f) { _cpuAcc += c; _gpuAcc += g; _mN++; if (c > _cpuMax) _cpuMax = c; if (g > _gpuMax) _gpuMax = g; }
            }
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 5f;
            if (_frames > 0 && _acc > 0f)
            {
                AvgFps = _frames / _acc; WorstFrameMs = _worst * 1000f; SlowFrames = _slow;
                int skinned = 0, verts = 0;
                foreach (var s in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)) { if (!s.isVisible) continue; skinned++; if (s.sharedMesh != null) verts += s.sharedMesh.vertexCount; }
                CpuMs = _mN > 0 ? _cpuAcc / _mN : 0f; GpuMs = _mN > 0 ? _gpuAcc / _mN : 0f;
                float hz = 0f; try { hz = OVRManager.display != null ? OVRManager.display.displayFrequency : 0f; } catch { }
                GrabLog.Add($"perf {AvgFps:0.0} fps, worst frame {WorstFrameMs:0} ms, {_slow}/{_frames} frames over 21 ms, app cpu {CpuMs:0.0} ms (max {_cpuMax:0}), app gpu {GpuMs:0.0} ms (max {_gpuMax:0}), {hz:0} Hz, visible skinned meshes {skinned} ({verts / 1000}k verts), scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
            _acc = 0f; _frames = 0; _worst = 0f; _slow = 0; _cpuAcc = _gpuAcc = _cpuMax = _gpuMax = 0f; _mN = 0;
        }
    }
}
