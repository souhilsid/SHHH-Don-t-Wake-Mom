using System.IO;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Automated in-editor play tests (no headset): full solo match with an autopilot human seat.</summary>
    public static class ShhhPlaytest
    {
        static string Flag => Path.Combine(ShhhAutomationBridge.AutomationDir, "autopilot.flag");

        [ShhhCommand("playtest_autopilot")]
        public static string Start(string args)
        {
            if (EditorApplication.isPlaying) return "already playing";
            // Never start the PC VR runtime (Meta Horizon Link) for headless editor tests.
            var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xr != null && xr.InitManagerOnStart) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); AssetDatabase.SaveAssets(); }
            PlayerSettings.runInBackground = true; // play mode must keep ticking while the editor window is unfocused
            EditorUtility.UnloadUnusedAssetsImmediate(true); System.GC.Collect(); // long editor sessions accumulate; free before the run
            File.WriteAllText(Flag, "1");
            var status = Path.Combine(ShhhAutomationBridge.AutomationDir, "playtest_status.json");
            if (File.Exists(status)) File.Delete(status);
            EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            return "entering play mode with autopilot; poll Automation/playtest_status.json";
        }

        [ShhhCommand("playtest_stop")]
        public static string Stop()
        {
            if (File.Exists(Flag)) File.Delete(Flag);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            return "stopped";
        }

        [ShhhCommand("screenshot_game")]
        public static string Screenshot(string args)
        {
            string p = Path.Combine(ShhhAutomationBridge.RepoRoot, "QA_Evidence", string.IsNullOrEmpty(args) ? "editor_capture.png" : args);
            Directory.CreateDirectory(Path.GetDirectoryName(p));
            var cam = Camera.main;
            if (cam == null) return "no main camera";
            var rt = new RenderTexture(1600, 900, 24);
            var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(p, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); rt.Release();
            return "saved " + p;
        }

        /// <summary>Moves the main camera (play mode) to a viewpoint "x,y,z,yaw,pitch" for evidence screenshots.</summary>
        [ShhhCommand("camera_to")]
        public static string CameraTo(string args)
        {
            var cam = Camera.main; if (cam == null) return "no camera";
            var a = args.Split(',');
            var rig = cam.transform.root;
            rig.position = new Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
            rig.rotation = Quaternion.Euler(a.Length > 4 ? float.Parse(a[4]) : 0, a.Length > 3 ? float.Parse(a[3]) : 0, 0);
            return "camera rig at " + rig.position;
        }
    }
}
