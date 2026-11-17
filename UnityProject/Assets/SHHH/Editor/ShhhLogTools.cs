using System;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Automation;

namespace SHHH.EditorTools
{
    /// <summary>Grep the current or previous Editor log (read-only) for remote diagnosis.</summary>
    public static class ShhhLogTools
    {
        [ShhhCommand("log_grep")]
        public static string Grep(string args)
        {
            // args: "prev|cur;pattern;maxLines"
            var a = (args ?? "").Split(';');
            bool prev = a.Length > 0 && a[0] == "prev";
            string pat = a.Length > 1 ? a[1] : "[SHHH]";
            int max = a.Length > 2 && int.TryParse(a[2], out int m) ? m : 200;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor");
            string path = Path.Combine(dir, prev ? "Editor-prev.log" : "Editor.log");
            if (!File.Exists(path)) return "missing " + path;
            var sb = new StringBuilder(); int n = 0, total = 0;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    total++;
                    if (line.IndexOf(pat, StringComparison.OrdinalIgnoreCase) >= 0) { if (n < max) sb.AppendLine(total + ": " + (line.Length > 300 ? line.Substring(0, 300) : line)); n++; }
                }
            }
            return $"{path}: {total} lines, {n} matches\n" + sb;
        }
    }
}
