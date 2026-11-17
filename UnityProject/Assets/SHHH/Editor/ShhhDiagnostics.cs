using System.Linq;
using System.Text;
using SHHH.Automation;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Read-only inspection commands used when diagnosing imports remotely.</summary>
    public static class ShhhDiagnostics
    {
        [ShhhCommand("inspect_models")]
        public static string InspectModels(string folder)
        {
            if (string.IsNullOrEmpty(folder)) folder = "Assets/SHHH/Art/ThirdParty/QuaterniusWomen";
            var sb = new StringBuilder();
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var all = AssetDatabase.LoadAllAssetsAtPath(p);
                var avatar = all.OfType<Avatar>().FirstOrDefault();
                var clips = all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => c.name + (c.isHumanMotion ? "(H)" : "(G)"));
                var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                sb.AppendLine($"{p}\n  type={mi?.animationType} avatar={(avatar != null ? (avatar.isValid + "/human=" + avatar.isHuman) : "none")} clips=[{string.Join(", ", clips)}]");
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null)
                {
                    var names = go.GetComponentsInChildren<Transform>(true).Select(t => t.name).Take(70);
                    sb.AppendLine("  bones: " + string.Join(" ", names));
                }
            }
            return sb.ToString();
        }
    }
}
