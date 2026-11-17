using System.Linq;
using System.Text;
using SHHH.Automation;
using SHHH.Game;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>V13: `probe_model <key>[;fit]` - builds an ArticulatedModel in edit mode exactly as the game does and reports its sizes,
    /// parts and the raw (unscaled) bounds of every mesh (import / orientation checks of new art).</summary>
    public static class ShhhProbeModel
    {
        [ShhhCommand("probe_model")]
        public static string Probe(string args)
        {
            var a = (args ?? "").Split(';'); string key = a[0]; float fit = a.Length > 1 && float.TryParse(a[1], out var f) ? f : 0.9f;
            var sb = new StringBuilder();
            var model = ArtCatalog.I != null ? ArtCatalog.I.Model(key) : Resources.Load<ArtCatalog>("ArtCatalog")?.Model(key);
            if (model == null) return "no model " + key;
            var raw = Object.Instantiate(model);
            foreach (var t in raw.GetComponentsInChildren<Transform>(true).Take(12)) sb.Append($"{t.name} pos {t.localPosition:F3} rot {t.localEulerAngles:F0} scale {t.localScale:F3}\n");
            var rb = new Bounds(); bool any = false;
            foreach (var r in raw.GetComponentsInChildren<Renderer>()) { if (!any) { rb = r.bounds; any = true; } else rb.Encapsulate(r.bounds); }
            sb.Append($"RAW world bounds {rb.size:F3}\n"); Object.DestroyImmediate(raw);
            var go = new GameObject("probe"); var am = ArticulatedModel.Build(key, go.transform, "m", fit, 0);
            var b = new Bounds(); any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            sb.Append($"BUILT fit {fit}: bounds {b.size:F3}; parts {string.Join(",", am.Parts.Keys)}; colliders {am.Colliders.Count}\n");
            Object.DestroyImmediate(go);
            return sb.ToString();
        }
    }
}
