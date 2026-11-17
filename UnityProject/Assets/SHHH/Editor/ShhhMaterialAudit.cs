using System.Collections.Generic;
using System.Linq;
using System.Text;
using SHHH.Automation;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Lists renderers whose material shader is missing, unsupported or the error shader (magenta).</summary>
    public static class ShhhMaterialAudit
    {
        [ShhhCommand("material_audit")]
        public static string Audit()
        {
            var bad = new Dictionary<string, List<string>>();
            int total = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                foreach (var m in r.sharedMaterials)
                {
                    total++;
                    string why = m == null ? "null material" : m.shader == null ? "null shader" : !m.shader.isSupported ? "unsupported " + m.shader.name : m.shader.name.Contains("Error") ? "error shader" : (m.shader.name == "Standard" || m.shader.name.StartsWith("Legacy")) ? "BUILT-IN shader " + m.shader.name : null;
                    if (why == null) continue;
                    string key = why + " | mat=" + (m != null ? m.name : "-");
                    if (!bad.TryGetValue(key, out var l)) bad[key] = l = new List<string>();
                    if (l.Count < 4) l.Add(r.name + " (" + r.GetType().Name + ")");
                }
            }
            var sb = new StringBuilder($"{total} material slots checked, {bad.Count} problem groups\n");
            foreach (var kv in bad) sb.AppendLine(kv.Key + " :: " + string.Join(", ", kv.Value));
            var shaders = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.shader != null).Select(m => m.shader.name).Distinct();
            sb.AppendLine("shaders in use: " + string.Join(", ", shaders));
            return sb.ToString();
        }
    }
}
