using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SHHH.EditorTools
{
    /// <summary>`ui_capture &lt;tag&gt;`: screenshots of the running game's UI (Scripts/Game/QA/UiCapture) into QA_Evidence/ui_v8/&lt;tag&gt;.</summary>
    public static class ShhhUiCapture
    {
        [ShhhCommand("ui_capture")]
        public static string Start(string args)
        {
            if (EditorApplication.isPlaying) return "already playing";
            ShhhKeepVisible.Arm();
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); AssetDatabase.SaveAssets(); }
            PlayerSettings.runInBackground = true;
            var dir = ShhhAutomationBridge.AutomationDir;
            File.WriteAllText(Path.Combine(dir, "uicapture.flag"), string.IsNullOrWhiteSpace(args) ? "capture" : args.Trim());
            foreach (var f in new[] { "autopilot.flag", "recipediag.flag" }) { var p = Path.Combine(dir, f); if (File.Exists(p)) File.Delete(p); }
            EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            return "ui_capture started; poll QA_Evidence/ui_v8/<tag>/index.json";
        }
    }
}

namespace SHHH.EditorTools
{
    /// <summary>`genies_diag &lt;tag&gt;`: runs SHHH.Genies.GeniesDiag in play mode (real Genies SDK API smoke test) -> QA_Evidence/genies/&lt;tag&gt;.</summary>
    public static class ShhhGeniesDiag
    {
        [SHHH.Automation.ShhhCommand("genies_diag")]
        public static string Start(string args)
        {
            if (UnityEditor.EditorApplication.isPlaying) return "already playing";
            var dir = SHHH.Automation.ShhhAutomationBridge.AutomationDir;
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "geniesdiag.flag"), string.IsNullOrWhiteSpace(args) ? "diag" : args.Trim());
            foreach (var f in new[] { "autopilot.flag", "recipediag.flag", "uicapture.flag" }) { var p = System.IO.Path.Combine(dir, f); if (System.IO.File.Exists(p)) System.IO.File.Delete(p); }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            ShhhKeepVisible.Arm(); UnityEditor.EditorApplication.EnterPlaymode();
            return "genies_diag started; poll QA_Evidence/genies/<tag>/diag.json";
        }
    }
}

namespace SHHH.EditorTools
{
    /// <summary>`genies_journey &lt;tag&gt;`: real Genies wardrobe -> save -> stage -> match -> podium flow (SHHH.Genies.GeniesWardrobeQa).</summary>
    public static class ShhhGeniesJourney
    {
        [SHHH.Automation.ShhhCommand("genies_journey")]
        public static string Start(string args)
        {
            if (UnityEditor.EditorApplication.isPlaying) return "already playing";
            var dir = SHHH.Automation.ShhhAutomationBridge.AutomationDir; ShhhKeepVisible.Arm();
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "geniesjourney.flag"), string.IsNullOrWhiteSpace(args) ? "journey" : args.Trim());
            foreach (var f in new[] { "autopilot.flag", "recipediag.flag", "uicapture.flag", "geniesdiag.flag" }) { var p = System.IO.Path.Combine(dir, f); if (System.IO.File.Exists(p)) System.IO.File.Delete(p); }
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(UnityEditor.BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; UnityEditor.EditorUtility.SetDirty(xr); UnityEditor.AssetDatabase.SaveAssets(); }
            UnityEditor.PlayerSettings.runInBackground = true;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            UnityEditor.EditorApplication.EnterPlaymode();
            return "genies_journey started; poll QA_Evidence/genies/<tag>/journey.json";
        }
    }
}
