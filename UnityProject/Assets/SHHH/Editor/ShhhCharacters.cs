using System.IO;
using System.Linq;
using System.Text;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Astra character integration: rig inspection and the runtime CharacterCatalog (V2 reduced meshes + V3 clips).</summary>
    public static class ShhhCharacters
    {
        const string V2 = "Assets/SHHHArtV2/Models/", V3 = "Assets/SHHHArtV3/Models/";

        [ShhhCommand("inspect_rigs")]
        public static string Inspect(string args)
        {
            var paths = string.IsNullOrWhiteSpace(args)
                ? new[] { V2 + "254_sibling_pip_Reduced.fbx", V2 + "254_sibling_pip.fbx", V3 + "SH3D_254_Rig_V3.fbx", V2 + "314_mom_Reduced.fbx", V3 + "SH3D_314_Rig_V3.fbx", V2 + "345_mochi_cat_Reduced.fbx", V2 + "346_biscuit_dog_Reduced.fbx", V3 + "V3_Pip_Idle.fbx" }
                : args.Split(';');
            var sb = new StringBuilder();
            foreach (var p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p.Trim());
                if (go == null) { sb.AppendLine(p + ": MISSING"); continue; }
                var mi = AssetImporter.GetAtPath(p.Trim()) as ModelImporter;
                int tris = 0, smr = 0, bones = 0;
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr++; if (r.sharedMesh) tris += r.sharedMesh.triangles.Length / 3; bones = Mathf.Max(bones, r.bones.Length); }
                foreach (var f in go.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh) tris += f.sharedMesh.triangles.Length / 3;
                var b = new Bounds(); bool init = false;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds); }
                var clips = AssetDatabase.LoadAllAssetsAtPath(p.Trim()).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => $"{c.name}({c.length:0.0}s{(c.isLooping ? ",loop" : "")})");
                var names = go.GetComponentsInChildren<Transform>(true).Select(t => t.name).Take(18);
                sb.AppendLine($"{p}: type={mi?.animationType} smr={smr} tris={tris} bones={bones} size={b.size} clips=[{string.Join(", ", clips)}]\n   nodes: {string.Join(" ", names)}");
            }
            return sb.ToString();
        }

        /// <summary>args = model path: lists every child node with its world bounds (room masters / layouts).</summary>
        [ShhhCommand("inspect_layout")]
        public static string InspectLayout(string args)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(args.Trim()); if (go == null) return "missing " + args;
            var sb = new StringBuilder();
            void Walk(Transform t, int depth)
            {
                var rs = t.GetComponentsInChildren<Renderer>(true);
                var b = new Bounds(); bool init = false; foreach (var r in rs) { if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds); }
                int tris = 0; foreach (var f in t.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh) tris += f.sharedMesh.triangles.Length / 3;
                sb.AppendLine($"{new string(' ', depth * 2)}{t.name}  c={b.center.ToString("F2")} s={b.size.ToString("F2")} tris={tris}");
                if (depth < 2) foreach (Transform c in t) Walk(c, depth + 1);
            }
            Walk(go.transform, 0);
            return sb.ToString();
        }

        /// <summary>First non-preview clip in a V3 single-take FBX, or a named clip inside a V2 multi-clip FBX ("file#Clip").</summary>
        static AnimationClip Clip(string spec)
        {
            string file = spec, name = null; int h = spec.IndexOf('#'); if (h >= 0) { file = spec.Substring(0, h); name = spec.Substring(h + 1); }
            string p = file.StartsWith("Assets/") ? file : V3 + file + ".fbx";
            var all = AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview"));
            return name == null ? all.FirstOrDefault() : all.FirstOrDefault(c => c.name == name);
        }

        static GameObject Model(params string[] candidates)
        {
            foreach (var c in candidates) { var g = AssetDatabase.LoadAssetAtPath<GameObject>(c); if (g != null) return g; }
            return null;
        }

        static int Tris(GameObject go)
        {
            int t = 0; if (go == null) return 0;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (r.sharedMesh) t += r.sharedMesh.triangles.Length / 3;
            foreach (var f in go.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh) t += f.sharedMesh.triangles.Length / 3;
            return t;
        }

        static CharacterCatalog.Entry Entry(CharacterCatalog cat, StringBuilder sb, string key, GameObject model, float height, params (string n, string spec)[] clips)
        {
            var e = new CharacterCatalog.Entry { key = key, model = model, height = height, triangles = Tris(model) };
            var missing = new System.Collections.Generic.List<string>();
            foreach (var c in clips) { var a = Clip(c.spec); if (a != null) e.clips.Add(new CharacterCatalog.NamedClip { name = c.n, clip = a }); else missing.Add(c.n); }
            cat.characters.Add(e);
            sb.Append($"{key}: {(model ? model.name : "NO MODEL")} {e.triangles} tris, {e.clips.Count} clips{(missing.Count > 0 ? " (missing " + string.Join(",", missing) + ")" : "")}; ");
            return e;
        }

        /// <summary>args "v3" uses the full V3 rigs; default = the light V2 _Reduced meshes (Quest 2 budget, same bone names).</summary>
        [ShhhCommand("build_character_catalog")]
        public static string Build(string args)
        {
            bool full = (args ?? "").Trim() == "v3";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SHHH/Resources"));
            var cat = ScriptableObject.CreateInstance<CharacterCatalog>();
            var sb = new StringBuilder();
            foreach (var (key, id, Cap) in new[] { ("pip", "254", "Pip"), ("nell", "255", "Nell"), ("rory", "256", "Rory"), ("tess", "257", "Tess") })
            {
                var v2full = V2 + $"{id}_sibling_{key}.fbx";
                var model = full ? Model(V3 + $"SH3D_{id}_Rig_V3.fbx", V2 + $"{id}_sibling_{key}_Reduced.fbx") : Model(V2 + $"{id}_sibling_{key}_Reduced.fbx", V3 + $"SH3D_{id}_Rig_V3.fbx");
                Entry(cat, sb, key, model, 1.3f,
                    ("idle", $"V3_{Cap}_Idle"), ("walk", $"V3_{Cap}_Sneak_InPlace"), ("run", $"V3_{Cap}_Run_InPlace"), ("react", $"V3_{Cap}_Reaction"),
                    ("carry", v2full + "#Carry_One"), ("celebrate", v2full + "#Celebrate"), ("shush", v2full + "#Shush"), ("surprised", v2full + "#Surprised"),
                    ("grab", v2full + "#Grab"), ("setdown", v2full + "#Set_Down"), ("panic", v2full + "#Panic"), ("laugh", v2full + "#Laugh"), ("lose", v2full + "#Lose"));
            }
            var momFull = V2 + "314_mom.fbx";
            Entry(cat, sb, "mom", full ? Model(V3 + "SH3D_314_Rig_V3.fbx", V2 + "314_mom_Reduced.fbx") : Model(V2 + "314_mom_Reduced.fbx", V3 + "SH3D_314_Rig_V3.fbx"), 1.72f,
                ("idle", momFull + "#Stand"), ("walk", momFull + "#Shuffle"), ("sleep", momFull + "#Sleep_Snore"), ("wake", momFull + "#Wake_Startle"), ("situp", momFull + "#Sit_Up"),
                ("scan", momFull + "#Scan_Room"), ("point", momFull + "#Point_Culprit"), ("admonish", momFull + "#Admonish"), ("squint", momFull + "#Suspicious_Squint"),
                ("inspect", momFull + "#Inspect_Chore"), ("returnbed", momFull + "#Return_Bed"), ("opendoor", momFull + "#Open_Door"), ("arms", momFull + "#Arms_Crossed"));
            var catFull = V2 + "345_mochi_cat.fbx";
            Entry(cat, sb, "mochi", Model(V2 + "345_mochi_cat_Reduced.fbx", catFull), 0.42f,
                ("idle", catFull + "#Cat_Idle"), ("walk", catFull + "#Cat_Walk"), ("run", catFull + "#Cat_Walk"), ("startle", catFull + "#Cat_Startle"), ("jump", catFull + "#Cat_Jump"), ("eat", catFull + "#Cat_Eat"), ("react", catFull + "#Cat_Paw"), ("sleep", catFull + "#Cat_Sleep"));
            var dogFull = V2 + "346_biscuit_dog.fbx";
            Entry(cat, sb, "biscuit", Model(V2 + "346_biscuit_dog_Reduced.fbx", dogFull), 0.55f,
                ("idle", dogFull + "#Dog_Idle"), ("walk", dogFull + "#Dog_Walk"), ("run", dogFull + "#Dog_Run"), ("sniff", dogFull + "#Dog_Sniff"), ("wag", dogFull + "#Dog_Wag"), ("startle", dogFull + "#Dog_Startle"), ("pick", dogFull + "#Dog_Pick_Toy"), ("eat", dogFull + "#Dog_Eat"), ("react", dogFull + "#Dog_Sit"));
            AssetDatabase.CreateAsset(cat, "Assets/SHHH/Resources/CharacterCatalog.asset");
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }
    }
}
