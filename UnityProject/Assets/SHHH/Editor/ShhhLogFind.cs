using System;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Automation;

namespace SHHH.EditorTools
{
    /// <summary>log_find "cur|prev;pat1|pat2;max": last N Editor.log lines matching any pattern.</summary>
    public static class ShhhLogFind
    {
        [ShhhCommand("log_find")]
        public static string Find(string args)
        {
            // args: "prev|cur;pattern;maxLines"
            var a = (args ?? "").Split(';');
            bool prev = a.Length > 0 && a[0] == "prev";
            string pat = a.Length > 1 ? a[1] : "[SHHH]";
            int max = a.Length > 2 && int.TryParse(a[2], out int m) ? m : 200;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor");
            string path = Path.Combine(dir, prev ? "Editor-prev.log" : "Editor.log");
            if (!File.Exists(path)) return "missing " + path;
            // V6: '|' separates alternatives; the LAST maxLines matches are returned (newest evidence first matters).
            var pats = pat.Split('|');
            var hits = new System.Collections.Generic.Queue<string>(); int n = 0, total = 0;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    total++;
                    bool hit = false; foreach (var pp in pats) if (pp.Length > 0 && line.IndexOf(pp, StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; break; }
                    if (hit) { hits.Enqueue(total + ": " + (line.Length > 300 ? line.Substring(0, 300) : line)); if (hits.Count > max) hits.Dequeue(); n++; }
                }
            }
            var sb = new StringBuilder(); foreach (var h in hits) sb.AppendLine(h);
            return $"{path}: {total} lines, {n} matches (last {hits.Count})\n" + sb;
        }
    }
}
