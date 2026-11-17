using System.Linq;
using System.Text;
using SHHH.Automation;
using UnityEditor;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>`prefab_find &lt;asset path&gt;;&lt;name substring&gt;`: lists matching objects in a prefab with their world position, renderer bounds and
    /// parent chain (used to anchor recipe fixtures to real house furniture, e.g. the kitchen sink for KT01).</summary>
    public static class ShhhPrefabFind
    {
        [ShhhCommand("prefab_find")]
        public static string Find(string args)
        {
            var a = (args ?? "").Split(';'); var path = a[0]; var sub = a.Length > 1 ? a[1].ToLowerInvariant() : "";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var sb = new StringBuilder();
                foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.ToLowerInvariant().Contains(sub)).Take(40))
                {
                    // prefab contents are not rendered: renderer bounds are empty, so transform the mesh bounds' corners instead
                    bool any = false; var b = new Bounds(t.position, Vector3.zero);
                    foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds;
                        for (int i = 0; i < 8; i++)
                        {
                            var c = mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                            if (!any) { b = new Bounds(c, Vector3.zero); any = true; } else b.Encapsulate(c);
                        }
                    }
                    string chain = ""; for (var p = t.parent; p != null && p != root.transform; p = p.parent) chain = p.name + "/" + chain;
                    sb.Append($"{chain}{t.name}: pos {t.position:F3} rotY {t.eulerAngles.y:F0} bounds c{b.center:F3} s{b.size:F3}\n");
                }
                return sb.Length > 0 ? sb.ToString() : "no match";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
