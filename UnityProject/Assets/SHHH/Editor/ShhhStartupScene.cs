using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.EditorTools
{
    /// <summary>
    /// V12: builds the tiny first scene (Startup: a black camera + StartupLoader) that shows the loading card at once and loads Boot in
    /// the background (Quest c15: ~10 s of grey void while Boot started up). Leaves the scenes open in the Editor untouched.
    /// </summary>
    public static class ShhhStartupScene
    {
        const string Path = "Assets/SHHH/Scenes/Startup.unity";

        [ShhhCommand("make_startup_scene")]
        public static string Make()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("StartupCamera");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.nearClipPlane = 0.05f; cam.farClipPlane = 20f;
            cam.stereoTargetEye = StereoTargetEyeMask.Both;
            go.AddComponent<StartupLoader>();
            EditorSceneManager.SaveScene(scene, Path);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            return "startup scene written: " + Path;
        }

        /// <summary>QA: enter play mode from the Startup scene, as the APK starts (exit with playtest_stop / editor_state).</summary>
        [ShhhCommand("play_from_startup")]
        public static string PlayFromStartup()
        {
            if (EditorApplication.isPlaying) return "already playing";
            EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            return "playing from " + Path;
        }
    }
}
