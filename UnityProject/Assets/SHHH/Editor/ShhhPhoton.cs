using System;
using System.IO;
using System.Linq;
using SHHH.Automation;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>
    /// Photon Fusion 2.1 + Voice 2 (Realtime 5) import and configuration without opening the Editor UI.
    /// App IDs are read from SHHH/Secrets/photon.json (never committed) and written into the SDK's PhotonAppSettings asset.
    /// </summary>
    public static class ShhhPhoton
    {
        static string Repo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Serializable] sealed class Ids { public string fusion, voice; }

        /// <summary>args = path relative to repo (e.g. SourceAssets/Photon/x.unitypackage). Import runs async; poll photon_status.</summary>
        [ShhhCommand("import_package")]
        public static string ImportPackage(string args)
        {
            var p = Path.Combine(Repo, args.Trim());
            if (!File.Exists(p)) throw new Exception("not found: " + p);
            AssetDatabase.ImportPackage(p, false);
            return "import queued: " + Path.GetFileName(p) + " (poll photon_status after compile)";
        }

        [ShhhCommand("photon_status")]
        public static string Status()
        {
            bool fusion = AssetDatabase.IsValidFolder("Assets/Photon/Fusion");
            bool voice = AssetDatabase.IsValidFolder("Assets/Photon/PhotonVoice");
            var settings = AssetDatabase.FindAssets("t:ScriptableObject PhotonAppSettings").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var asm = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).Where(n => n.StartsWith("Fusion") || n.Contains("Photon")).OrderBy(n => n).ToArray();
            string ver = "";
            var vf = AssetDatabase.FindAssets("build_info t:TextAsset", new[] { "Assets/Photon" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (vf != null) ver = File.ReadAllText(Path.Combine(Repo, "UnityProject", vf)).Trim();
            return $"fusionFolder={fusion} voiceFolder={voice}\nsettings: {string.Join(", ", settings)}\nassemblies: {string.Join(", ", asm)}\n{ver}";
        }

        /// <summary>Writes Fusion/Voice App IDs from Secrets/photon.json into every PhotonAppSettings asset found.</summary>
        [ShhhCommand("photon_configure")]
        public static string Configure()
        {
            var sp = Path.Combine(Repo, "Secrets", "photon.json");
            if (!File.Exists(sp)) throw new Exception("missing " + sp);
            var ids = JsonUtility.FromJson<Ids>(File.ReadAllText(sp));
            int changed = 0; var log = new System.Text.StringBuilder();
            foreach (var path in AssetDatabase.FindAssets("PhotonAppSettings").Select(AssetDatabase.GUIDToAssetPath).Where(x => x.EndsWith(".asset")))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(path); if (obj == null) continue;
                var so = new SerializedObject(obj);
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.String) continue;
                    if (it.name == "AppIdFusion" && !string.IsNullOrEmpty(ids.fusion)) { it.stringValue = ids.fusion; changed++; log.Append(path).Append(':').Append(it.propertyPath).Append(' '); }
                    else if (it.name == "AppIdVoice" && !string.IsNullOrEmpty(ids.voice)) { it.stringValue = ids.voice; changed++; log.Append(path).Append(':').Append(it.propertyPath).Append(' '); }
                    else if (it.name == "FixedRegion" && string.IsNullOrEmpty(it.stringValue)) { /* best region by ping */ }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(obj);
            }
            AssetDatabase.SaveAssets();
            if (changed == 0) throw new Exception("No AppIdFusion/AppIdVoice fields found (is the SDK imported?)");
            return $"configured {changed} app id fields (values not echoed): {log}";
        }
    }
}
