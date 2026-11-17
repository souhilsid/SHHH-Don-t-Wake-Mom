using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Project configuration for Quest 2 / 3 / 3S and reproducible APK builds (dev + signed release/Alpha).
    /// Signing secrets live OUTSIDE the repo in SHHH/Secrets/signing.json (never committed).
    /// Meta manifest rules followed: minSdk 32, targetSdk 34, headtracking required, supportedDevices quest2|quest3|quest3s.
    /// </summary>
    public static class ShhhBuild
    {
        public const string PackageId = "com.shhhgame.dontwakemom";
        public const string ProductName = "SHHH! Don't Wake Mom";
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string BuildDir => Path.Combine(Repo, "Builds", "Quest2_3_3S");
        static string VersionFile => Path.Combine(Repo, "Builds", "version.json");

        [Serializable] sealed class Ver { public int major = 0, minor = 1, patch = 0, code = 1; }
        [Serializable] sealed class Signing { public string keystorePath, keystorePass, keyAlias, keyPass; }

        [ShhhCommand("configure_project")]
        public static string Configure()
        {
            PlayerSettings.companyName = "SHHH Game";
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            KeepLightmapAndFogVariants();
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            ApplyVersion(LoadVersion());
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SHHH/Art/Brand/shhh_icon_v5.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            ConfigureUrpForQuest();
            ConfigureOvr();
            AssetDatabase.SaveAssets();
            return $"configured {PackageId} min32/target34 IL2CPP ARM64 Vulkan Linear ASTC; version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})";
        }

        /// <summary>
        /// V12 (owner, Quest c15: "the environment lighting is dark now"). The build scenes are generated and tiny: no baked lightmaps, no
        /// fog. With lightmap / fog stripping on "Automatic", Unity therefore stripped the LIGHTMAP_ON and fog shader variants from the
        /// APK - but the house applies its baked lightmaps (and CartoonLook its fog) at runtime. On the Quest the painted house fell
        /// back to ambient-only lighting (dark walls; the c13 run hid this because those walls were missing) and the street had no fog.
        /// Custom stripping keeps them.
        /// </summary>
        [ShhhCommand("keep_lightmap_fog_variants")]
        public static string KeepLightmapAndFogVariants()
        {
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            void Set(string n, int v) { var p = gs.FindProperty(n); if (p != null) p.intValue = v; }
            void SetB(string n, bool v) { var p = gs.FindProperty(n); if (p != null) p.boolValue = v; }
            Set("m_LightmapStripping", 1); Set("m_FogStripping", 1);
            // only what the game uses: non-directional baked lightmaps (HouseAssembly.BindLighting) and linear fog (CartoonLook) - keeping every
            // mode made c16 292 MB (URP Lit alone 50 MB of variants) instead of 176 MB
            SetB("m_LightmapKeepPlain", true); SetB("m_FogKeepLinear", true);
            foreach (var k in new[] { "m_LightmapKeepDirCombined", "m_LightmapKeepDynamicPlain", "m_LightmapKeepDynamicDirCombined", "m_LightmapKeepShadowMask", "m_LightmapKeepSubtractive", "m_FogKeepExp", "m_FogKeepExp2" }) SetB(k, false);
            gs.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            var chk = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            return $"lightmap stripping {chk.FindProperty("m_LightmapStripping").intValue} (1 = custom), fog stripping {chk.FindProperty("m_FogStripping").intValue}";
        }

        static void ConfigureOvr()
        {
            var cfg = OVRProjectConfig.CachedProjectConfig;
            cfg.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            cfg.targetDeviceTypes = new System.Collections.Generic.List<OVRProjectConfig.DeviceType>
            { OVRProjectConfig.DeviceType.Quest2, OVRProjectConfig.DeviceType.Quest3, OVRProjectConfig.DeviceType.Quest3S };
            OVRProjectConfig.CommitProjectConfig(cfg);
        }

        /// <summary>Quest-lean URP: no shadows, no HDR, no post, 4x MSAA, no depth/opaque textures.</summary>
        static void ConfigureUrpForQuest()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;
                asset.supportsHDR = false;
                asset.msaaSampleCount = 4;
                asset.renderScale = 1f;
                asset.supportsCameraDepthTexture = false;
                asset.supportsCameraOpaqueTexture = false;
                asset.shadowDistance = 0f;
                asset.maxAdditionalLightsCount = 4;
                var so = new SerializedObject(asset);
                so.FindProperty("m_MainLightShadowsSupported")?.SetBool(false);
                so.FindProperty("m_AdditionalLightShadowsSupported")?.SetBool(false);
                so.FindProperty("m_SupportsSoftShadows")?.SetBool(false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            QualitySettings.vSyncCount = 0;
        }

        static void SetBool(this SerializedProperty p, bool v) { p.boolValue = v; }

        static Ver LoadVersion()
        {
            try { if (File.Exists(VersionFile)) return JsonUtility.FromJson<Ver>(File.ReadAllText(VersionFile)); } catch { }
            return new Ver();
        }

        static void ApplyVersion(Ver v)
        {
            PlayerSettings.bundleVersion = $"{v.major}.{v.minor}.{v.patch}";
            PlayerSettings.Android.bundleVersionCode = v.code;
        }

        static void SaveVersion(Ver v) { Directory.CreateDirectory(Path.GetDirectoryName(VersionFile)); File.WriteAllText(VersionFile, JsonUtility.ToJson(v, true)); }

        // V12: Startup first - a tiny scene that shows the loading card at once and loads Boot in the background
        static readonly string[] SceneOrder = { "Startup", "Boot", "FrontEnd", "LivingRoom", "Kitchen", "Laundry", "Practice" };

        [ShhhCommand("ensure_build_scenes")]
        public static string EnsureBuildScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var n in SceneOrder)
            {
                string p = $"Assets/SHHH/Scenes/{n}.unity";
                if (!File.Exists(Path.Combine(Application.dataPath, "..", p))) throw new Exception("missing scene " + p + " (run build_scenes)");
                list.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            return "build scenes: " + string.Join(", ", SceneOrder);
        }

        [ShhhCommand("build_apk_dev")]
        public static string BuildDev() => Build(false);

        [ShhhCommand("build_apk_release")]
        public static string BuildRelease() => Build(true);

        static string Build(bool release)
        {
            Configure();
            EnsureBuildScenes();
            var v = LoadVersion();
            v.code++; v.patch++;
            ApplyVersion(v);
            Directory.CreateDirectory(BuildDir);
            string ver = PlayerSettings.bundleVersion;
            string file = Path.Combine(BuildDir, $"SHHH_{(release ? "alpha" : "dev")}_{ver}_c{v.code}.apk");
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = file,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = release ? BuildOptions.None : (BuildOptions.Development)
            };
            if (opts.scenes.Length == 0) throw new Exception("No scenes in build settings — run build_scenes first");
            if (release)
            {
                var sPath = Path.Combine(Repo, "Secrets", "signing.json");
                if (!File.Exists(sPath)) throw new Exception("Release signing config missing: " + sPath + " (see Docs/BUILD_RELEASE_GUIDE.md)");
                var s = JsonUtility.FromJson<Signing>(File.ReadAllText(sPath));
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = s.keystorePath; PlayerSettings.Android.keystorePass = s.keystorePass;
                PlayerSettings.Android.keyaliasName = s.keyAlias; PlayerSettings.Android.keyaliasPass = s.keyPass;
            }
            else PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.development = !release;
            var report = BuildPipeline.BuildPlayer(opts);
            if (release) { PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = ""; }
            var sum = report.summary;
            if (sum.result != BuildResult.Succeeded)
            {
                var errs = report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Take(25).Select(m => m.content);
                throw new Exception($"Build {sum.result}: {sum.totalErrors} errors\n" + string.Join("\n", errs));
            }
            SaveVersion(v);
            string hash;
            using (var sha = SHA256.Create()) using (var fs = File.OpenRead(file)) hash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            long size = new FileInfo(file).Length;
            File.AppendAllText(Path.Combine(BuildDir, "BUILDS.csv"), $"{DateTime.UtcNow:o},{Path.GetFileName(file)},{ver},{v.code},{(release ? "release-signed" : "development")},{size},{hash}\n");
            return $"OK {file}\nsize={size} sha256={hash}\ntime={sum.totalTime.TotalSeconds:0}s warnings={sum.totalWarnings}";
        }
    }
}
