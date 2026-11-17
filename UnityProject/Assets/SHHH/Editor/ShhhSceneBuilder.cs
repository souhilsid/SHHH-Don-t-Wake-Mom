using System.Collections.Generic;
using System.IO;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Generates the project's scenes without manual editor work: Boot (XR rig with tracked hands + GameRoot) and one
    /// scene per room (Living Room, Kitchen, Laundry), Practice and FrontEnd. Room content is generated at runtime from
    /// the shared RoomCatalog data, so these scenes stay tiny and merge-friendly.
    /// </summary>
    public static class ShhhSceneBuilder
    {
        const string SceneDir = "Assets/SHHH/Scenes";
        static readonly string[] Rooms = { "FrontEnd", "LivingRoom", "Kitchen", "Laundry", "Practice" };

        [ShhhCommand("setup_layers")]
        public static string SetupLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            void Set(int i, string n) { var p = layers.GetArrayElementAtIndex(i); if (string.IsNullOrEmpty(p.stringValue) || p.stringValue == n) p.stringValue = n; }
            Set(8, "Props"); Set(9, "Hands"); Set(10, "Agents"); Set(11, "WorldUI"); Set(12, "Furniture");
            tm.ApplyModifiedPropertiesWithoutUndo();
            // Hands never collide with agents or UI; UI never collides with anything.
            int ui = 11;
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(ui, i, true);
            Physics.IgnoreLayerCollision(9, 10, true); Physics.IgnoreLayerCollision(9, 9, true); Physics.IgnoreLayerCollision(9, 12, true); Physics.IgnoreLayerCollision(9, 0, true);
            Physics.IgnoreLayerCollision(10, 10, true);
            return "layers set: Props, Hands, Agents, WorldUI, Furniture";
        }

        [ShhhCommand("build_scenes")]
        public static string BuildScenes()
        {
            SetupLayers();
            Directory.CreateDirectory(SceneDir);
            var list = new List<EditorBuildSettingsScene>();
            string boot = BuildBoot();
            list.Add(new EditorBuildSettingsScene(boot, true));
            foreach (var r in Rooms)
            {
                string path = $"{SceneDir}/{r}.unity";
                var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var marker = new GameObject("SceneInfo_" + r);
                EditorSceneManager.SaveScene(s, path);
                list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            EditorSceneManager.OpenScene(boot, OpenSceneMode.Single);
            return "scenes: " + string.Join(", ", list.ConvertAll(x => x.path));
        }

        static T Ensure<T>(GameObject g) where T : Component { var c = g.GetComponent<T>(); return c != null ? c : g.AddComponent<T>(); }

        static string BuildBoot()
        {
            string path = SceneDir + "/Boot.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            var handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRHandPrefab.prefab");
            if (rigPrefab == null) throw new System.Exception("OVRCameraRig prefab not found");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.name = "XRRig";
            var mgr = Ensure<OVRManager>(rig);
            mgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            var cam = rig.transform.Find("TrackingSpace/CenterEyeAnchor");
            var camera = cam.GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = 0.03f; camera.farClipPlane = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Palette.Hex(0x0E0D26);
            var pr = rig.AddComponent<PlayerRig>();
            pr.Head = cam; pr.TrackingSpace = rig.transform.Find("TrackingSpace");
            foreach (var side in new[] { "Left", "Right" })
            {
                var anchor = rig.transform.Find($"TrackingSpace/{side}HandAnchor");
                GameObject hand;
                if (handPrefab != null) hand = (GameObject)PrefabUtility.InstantiatePrefab(handPrefab, anchor);
                else hand = new GameObject("OVRHand");
                hand.name = side + "Hand";
                var ovrHand = Ensure<OVRHand>(hand);
                var skel = Ensure<OVRSkeleton>(hand);
                var so = new SerializedObject(ovrHand);
                var ht = so.FindProperty("HandType");
                if (ht != null) { ht.enumValueIndex = side == "Left" ? 1 : 2; so.ApplyModifiedPropertiesWithoutUndo(); }
                // Let OVRHand.OnValidate sync skeleton/mesh types to the hand.
                ovrHand.OnValidate();
                var src = hand.AddComponent<OvrHandSource>();
                src.Hand = side == "Left" ? Handedness.Left : Handedness.Right; src.ovrHand = ovrHand; src.skeleton = skel;
                var hi = hand.AddComponent<HandInteractor>(); hi.Source = src;
                if (side == "Left") { pr.Left = src; pr.LeftHand = hi; } else { pr.Right = src; pr.RightHand = hi; }
                foreach (var r in hand.GetComponentsInChildren<SkinnedMeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            pr.LeftHand.Other = pr.RightHand; pr.RightHand.Other = pr.LeftHand;
            var wrist = new GameObject("WristMenu").AddComponent<WristMenu>();
            wrist.transform.SetParent(rig.transform, false);
            wrist.Hand = pr.Left; wrist.Rig = pr;
            var rootGo = new GameObject("GameRoot");
            var gr = rootGo.AddComponent<GameRoot>(); gr.Rig = pr; gr.Wrist = wrist;
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
