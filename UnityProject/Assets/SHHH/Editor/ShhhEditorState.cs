using SHHH.Automation;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    public static class ShhhEditorState
    {
        [ShhhCommand("editor_state")]
        public static string State() => $"playing={EditorApplication.isPlaying} paused={EditorApplication.isPaused} frame={Time.frameCount} t={Time.time:0.0} timeScale={Time.timeScale} rib={Application.runInBackground} focus={UnityEditorInternal.InternalEditorUtility.isApplicationActive}";

        [ShhhCommand("unpause")]
        public static string Unpause() { EditorApplication.isPaused = false; return State(); }
    }
}
