using System;
using System.Runtime.InteropServices;
using UnityEditor;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Desktop QA captures render through the Editor's Vulkan device, which produces black frames while the Editor window is minimized
    /// (genies_journey j4, net Genies run 1). While a capture harness runs in play mode, a minimized Editor is restored WITHOUT taking focus
    /// (SW_SHOWNOACTIVATE), so whatever the owner is doing keeps the keyboard. Windows only; no-op elsewhere.
    /// </summary>
    [InitializeOnLoad]
    public static class ShhhKeepVisible
    {
        const string Key = "shhh_qa_keep_visible";
        public static void Arm() => SessionState.SetBool(Key, true);
        static double _next;

        static ShhhKeepVisible()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key, false); };
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Key, false) || EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
#if UNITY_EDITOR_WIN
            try
            {
                var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (h != IntPtr.Zero && IsIconic(h)) { ShowWindow(h, 4); UnityEngine.Debug.Log("[SHHH][qa] Editor was minimized during a capture run - restored without focus"); }
            }
            catch (Exception) { }
#endif
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#endif
    }
}
