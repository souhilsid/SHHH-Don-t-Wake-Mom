using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace SHHH.EditorTools {
 public static class HandcraftedRuntimeReview {
  [ShhhCommand("v5_walkthrough")]
  public static string Walkthrough(){
   if(EditorApplication.isPlaying||Lightmapping.isRunning)return "BUSY";
   File.WriteAllText(Path.Combine(ShhhAutomationBridge.AutomationDir,"v5_walk.flag"),"1");
   EditorSceneManager.OpenScene("Assets/SHHH/ArtProductionV5/Scenes/ConnectedHouse_V5_Living.unity");EditorApplication.EnterPlaymode();return "Started native physics-derived camera route review; not actor animation or VR tracking validation.";
  }
  [ShhhCommand("v5_native_views")]
  public static string NativeViews(string args){
   if(EditorApplication.isPlaying)return "Already playing";
   if(Lightmapping.isRunning)return "BAKE_RUNNING: capture deferred until lighting is saved";
   string label=args=="before"?"Before":"After";File.WriteAllText(Path.Combine(ShhhAutomationBridge.AutomationDir,"v5_capture.flag"),label);
   EditorSceneManager.OpenScene(args=="before"?"Assets/SHHH/ArtProductionV4/World/Scenes/ConnectedHouse_Production.unity":"Assets/SHHH/ArtProductionV5/Scenes/ConnectedHouse_V5_Living.unity");EditorApplication.EnterPlaymode();return "Queued matched native Play-mode camera captures: "+label;
  }
  [ShhhCommand("v5_book_runtime")]
  public static string Start(){
   if(EditorApplication.isPlaying)return "Already playing";
   var xr=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(xr){xr.InitManagerOnStart=false;EditorUtility.SetDirty(xr);}
   PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();var dir=ShhhAutomationBridge.AutomationDir;
   foreach(string name in new[]{"xrdiag.flag","autopilot.flag"}){string p=Path.Combine(dir,name);if(File.Exists(p))File.Delete(p);}
   File.WriteAllText(Path.Combine(dir,"v5_book.flag"),"1");EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity");EditorApplication.EnterPlaymode();return "Started actual living-room book chore/sabotage/repair diagnostic using simulated input on Boot hands; see SHHH_ArtProduction_V5/Evidence/Runtime. Hardware remains unverified.";
  }
 }
}
