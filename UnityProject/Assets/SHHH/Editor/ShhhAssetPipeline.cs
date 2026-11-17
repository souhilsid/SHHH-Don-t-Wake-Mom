using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Import rules for third-party CC0 art (Quest-friendly textures, readable meshes for colliders/NavMesh, humanoid rigs
    /// for characters) and generation of the runtime ArtCatalog, base materials and animator controllers.
    /// </summary>
    public sealed class ShhhModelImportRules : AssetPostprocessor
    {
        const string Root = "Assets/SHHH/Art/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.isReadable = true;
            mi.importCameras = false; mi.importLights = false;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            // V13 note: Tools/blender/make_fridge_food.py exports its pieces as several top-level objects - a single top-level node is
            // folded into the model root by Unity and ArticulatedModel resets the root's rotation (the fridge lay on its back at half size)
            bool women = assetPath.Contains("/QuaterniusWomen/");
            bool character = assetPath.Contains("/QuaterniusBase/") && !assetPath.Contains("/Hair/");
            if (women)
            {
                // Quaternius 2019 rig has IK-detached feet -> not Humanoid-mappable; Generic keeps its own clips working (Mom).
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                mi.optimizeGameObjects = false;
            }
            else if (character)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = assetPath.Contains("/QuaterniusWomen/");
                mi.optimizeGameObjects = false;
            }
            else
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.importBlendShapes = false;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.Contains("/QuaterniusWomen/")) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.name.ToLowerInvariant();
                c.loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run") || n.Contains("standing");
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }

        /// <summary>FBX materials arrive as built-in "Standard" (magenta under URP): convert to URP Lit keeping texture + colour.</summary>
        void OnPostprocessMaterial(Material m)
        {
            if (!assetPath.StartsWith(Root)) return;
            if (m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline")) return;
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) return;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            m.shader = urp;
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            m.SetColor("_BaseColor", col);
            if (normal != null && !assetPath.Contains("/Quaternius")) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", 0.12f); m.SetFloat("_Metallic", 0f);
            m.enableInstancing = true;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/SHHH/Resources/Audio")) return;
            var ai = (AudioImporter)assetImporter;
            // V13: music stays stereo (2D, not spatial); full-length licensed tracks stream from disk (one at a time, little memory)
            bool music = assetPath.Contains("/music_");
            bool track = assetPath.Contains("/music_play_") || assetPath.Contains("/music_menu_") || assetPath.Contains("/music_hurry_");
            ai.forceToMono = !music;
            bool longLoop = music || assetPath.Contains("amb_") || assetPath.Contains("snore");
            var s = ai.defaultSampleSettings;
            s.loadType = track ? AudioClipLoadType.Streaming : longLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.6f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ai.defaultSampleSettings = s;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            int max = assetPath.Contains("/Quaternius") ? 512 : 1024;
            if (assetPath.Contains("/Brand/")) { max = 1024; ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.textureType = TextureImporterType.Default; }
            if (assetPath.EndsWith("_Normal.png")) ti.textureType = TextureImporterType.NormalMap;
            ti.maxTextureSize = max;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = max; android.format = TextureImporterFormat.ASTC_6x6; android.compressionQuality = 50;
            ti.SetPlatformTextureSettings(android);
        }
    }

    public static class ShhhAssetPipeline
    {
        const string ArtRoot = "Assets/SHHH/Art/ThirdParty";
        const string ResDir = "Assets/SHHH/Resources";
        const string GenDir = "Assets/SHHH/Generated";

        static string KeyFor(string path)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            if (path.Contains("/SHHHArtV2/")) return "a2_" + file;
            if (path.Contains("/KayKitFurniture/")) return "kf_" + file;
            if (path.Contains("/KayKitRestaurant/")) return "kr_" + file;
            if (path.Contains("/Daycare/")) return "dc_" + file;          // V9: kimchistan Daycare & Everyday Interiors (CC0), articulated parts kept
            if (path.Contains("/TinyTreats/")) return "tt_" + file;
            if (path.Contains("/Laundry/")) return "ld_" + file;          // V12: PtDesigns Colorful Laundry & Utility (CC0); washer split into parts (Tools/blender/split_parts.py)
            if (path.Contains("/UltimateHouse/")) return "uh_" + file;    // V12: Quaternius Ultimate House Interior (CC0); fridge/bin/drawers split into parts
            if (path.Contains("/KenneyFurniture/")) return "kn_" + file;
            if (path.Contains("/KenneyFood/")) return "food_" + file;
            if (path.Contains("/KenneyToyCar/")) return "kc_" + file;
            if (path.Contains("/KenneyBrick/")) return "kb_" + file;
            if (path.Contains("/QuaterniusBase/Hair/")) return "qb_hair_" + file;
            if (path.Contains("/QuaterniusBase/")) return "qb_" + file;
            if (path.Contains("/QuaterniusWomen/")) return "qw_" + file;
            if (path.Contains("/PolyByGoogle/")) return "pg_" + file;
            return file;
        }

        [ShhhCommand("import_tmp")]
        public static string ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return "TMP essentials already present";
            var candidates = Directory.GetFiles(Path.GetFullPath("Library/PackageCache"), "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (candidates.Length == 0) return "TMP Essential Resources package not found";
            AssetDatabase.ImportPackage(candidates[0], false);
            return "imported " + candidates[0];
        }

        [ShhhCommand("build_catalog")]
        public static string BuildCatalog()
        {
            Directory.CreateDirectory(ResDir); Directory.CreateDirectory(GenDir);
            AssetDatabase.Refresh();
            var cat = AssetDatabase.LoadAssetAtPath<ArtCatalog>(ResDir + "/ArtCatalog.asset");
            if (cat == null) { cat = ScriptableObject.CreateInstance<ArtCatalog>(); AssetDatabase.CreateAsset(cat, ResDir + "/ArtCatalog.asset"); }
            cat.models.Clear();
            // Astra V2 original art library (preferred) + CC0 third-party fallbacks.
            var roots = AssetDatabase.IsValidFolder("Assets/SHHHArtV2/Models") ? new[] { ArtRoot, "Assets/SHHHArtV2/Models" } : new[] { ArtRoot };
            var guids = AssetDatabase.FindAssets("t:Model", roots);
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) cat.models.Add(new ArtCatalog.Entry { key = KeyFor(p), model = go });
            }
            cat.litBase = EnsureMaterial(GenDir + "/SHHH_Lit.mat", "Universal Render Pipeline/Lit", false);
            cat.toonBase = EnsureMaterial(GenDir + "/SHHH_Toon.mat", "SHHH/Toon", false);
            cat.unlitBase = EnsureMaterial(GenDir + "/SHHH_Unlit.mat", "Universal Render Pipeline/Unlit", false);
            cat.unlitTransparent = EnsureMaterial(GenDir + "/SHHH_UnlitTransparent.mat", "Universal Render Pipeline/Unlit", true);
            cat.logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SHHH/Art/Brand/shhh_logo_v5.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SHHH/Art/Brand/shhh_logo.png");
            cat.logoIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SHHH/Art/Brand/shhh_icon_v5.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SHHH/Art/Brand/shhh_icon.png");
            var font = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/SHHH" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x.Contains("Fredoka") ? 0 : 1).FirstOrDefault();
            cat.brandFont = font != null ? AssetDatabase.LoadAssetAtPath<Object>(font) : null;
            cat.siblingAnimator = null; // siblings: ProceduralHumanoid on the Quaternius base humanoid rig
            cat.momAnimator = BuildAnimator(GenDir + "/Mom.controller", "Female_Dress", true);
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            return $"catalog: {cat.models.Count} models, logo={(cat.logo != null)}, font={(cat.brandFont != null)}, animators={(cat.siblingAnimator != null)}/{(cat.momAnimator != null)}\n" + string.Join(", ", cat.models.Select(m => m.key));
        }

        static Material EnsureMaterial(string path, string shaderName, bool transparent)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find(shaderName);
                m = new Material(sh); AssetDatabase.CreateAsset(m, path);
            }
            if (transparent)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_Cull", 0f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
                m.SetOverrideTag("RenderType", "Transparent");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static AnimationClip FindClip(string modelName, string contains)
        {
            var path = AssetDatabase.FindAssets(modelName + " t:Model", new[] { ArtRoot + "/QuaterniusWomen" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (path == null) return null;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.ToLowerInvariant().Contains(contains));
        }

        static RuntimeAnimatorController BuildAnimator(string path, string source, bool mom)
        {
            var idle = FindClip(source, "idle"); var walk = FindClip(source, "walk"); var run = FindClip(source, "run");
            var clap = FindClip(source, "clapping"); var jump = FindClip(source, "jump");
            if (idle == null || walk == null) return null;
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Carry", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Point", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Cheer", AnimatorControllerParameterType.Trigger);
            var layers = ac.layers; layers[0].iKPass = true; ac.layers = layers;
            var sm = ac.layers[0].stateMachine;
            var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f); tree.AddChild(walk, mom ? 1.2f : 1.0f); if (run != null) tree.AddChild(run, 2.6f);
            sm.defaultState = loco;
            if (clap != null)
            {
                var st = sm.AddState(mom ? "Point" : "Cheer"); st.motion = clap;
                var t = sm.AddAnyStateTransition(st); t.AddCondition(AnimatorConditionMode.If, 0, mom ? "Point" : "Cheer"); t.duration = 0.15f;
                var back = st.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.2f;
            }
            if (!mom && jump != null)
            {
                var st = sm.AddState("Jump"); st.motion = jump;
                var t = sm.AddAnyStateTransition(st); t.AddCondition(AnimatorConditionMode.If, 0, "Point"); t.duration = 0.1f;
                var back = st.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.85f;
            }
            AssetDatabase.SaveAssets();
            return ac;
        }

        [ShhhCommand("build_font")]
        public static string BuildFont()
        {
            const string ttfPath = "Assets/SHHH/Art/Fonts/Fredoka-SemiBold.ttf", outPath = "Assets/SHHH/Art/Fonts/Fredoka-SemiBold SDF.asset";
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null) return "font file missing: " + ttfPath;
            AssetDatabase.DeleteAsset(outPath);
            var fa = TMPro.TMP_FontAsset.CreateFontAsset(ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, TMPro.AtlasPopulationMode.Dynamic, true);
            fa.name = "Fredoka-SemiBold SDF";
            AssetDatabase.CreateAsset(fa, outPath);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (fa.atlasTextures != null) foreach (var t in fa.atlasTextures) { if (t == null) continue; t.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
            string chars = "";
            for (int c = 32; c < 127; c++) chars += (char)c;
            chars += "\u2026\u2014\u00B0\u00B7\u2019\u201C\u201D";
            fa.TryAddCharacters(chars, out string missing);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return "font asset created; missing glyphs: '" + missing + "'";
        }

        [ShhhCommand("reimport_art")]
        public static string ReimportArt()
        {
            AssetDatabase.ImportAsset(ArtRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            return "reimported";
        }
    }
}
