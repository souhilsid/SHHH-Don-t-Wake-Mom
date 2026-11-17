using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SHHH.EditorTools
{
    /// <summary>`recipe_diag [recipeId|LivingRoom|Kitchen|Laundry|all]`: plays recipes with simulated hands (Scripts/Game/QA/RecipeDiagnostic).</summary>
    public static class ShhhRecipeDiag
    {
        [ShhhCommand("recipe_diag")]
        public static string Start(string args)
        {
            if (EditorApplication.isPlaying) return "already playing";
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); AssetDatabase.SaveAssets(); }
            PlayerSettings.runInBackground = true;
            var dir = ShhhAutomationBridge.AutomationDir;
            File.WriteAllText(Path.Combine(dir, "recipediag.flag"), string.IsNullOrWhiteSpace(args) ? "1" : args.Trim());
            var res = Path.Combine(dir, "recipe_diag_result.json"); if (File.Exists(res)) File.Delete(res);
            var ap = Path.Combine(dir, "autopilot.flag"); if (File.Exists(ap)) File.Delete(ap);
            ShhhKeepVisible.Arm();
            EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            return "recipe_diag started; poll Automation/recipe_diag_result.json";
        }
    }
}
