using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SHHH.EditorTools
{
    /// <summary>`xr_diag`: runs the offline XR interaction harness (Scripts/Game/QA/XrInteractionDiagnostic) in play mode.</summary>
    public static class ShhhXrDiag
    {
        [ShhhCommand("xr_diag")]
        public static string Start(string args)
        {
            if (EditorApplication.isPlaying) return "already playing";
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); AssetDatabase.SaveAssets(); }
            PlayerSettings.runInBackground = true;
            var dir = ShhhAutomationBridge.AutomationDir;
            File.WriteAllText(Path.Combine(dir, "xrdiag.flag"), "1");
            var res = Path.Combine(dir, "xr_diag_result.json"); if (File.Exists(res)) File.Delete(res);
            var ap = Path.Combine(dir, "autopilot.flag"); if (File.Exists(ap)) File.Delete(ap);
            EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            return "xr_diag started; poll Automation/xr_diag_result.json";
        }
    }
}
