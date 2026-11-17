#!/usr/bin/env python3
"""Generates Scripts/Game/Agents/MomLines.cs from mom_lines_v8.json (subtitles keep the {name} prefix the VO speaks first)."""
import json, os
here = os.path.dirname(os.path.abspath(__file__)); root = os.path.abspath(os.path.join(here, "..", ".."))
L = json.load(open(os.path.join(here, "mom_lines_v8.json"))); L.pop("_about", None)
NAMED = {"mom_caught_", "mom_penalty_", "mom_lazy_", "mom_win"}
def named(k): return any(k.startswith(p) for p in NAMED)
rows = []
for k, v in L.items():
    txt = ("{name}! " + v) if named(k) and k != "mom_win" else ("{name} - " + v[0].lower() + v[1:] if k == "mom_win" else v)
    rows.append(f'            ["{k}"] = "{txt.replace(chr(34), chr(92) + chr(34))}",')
cs = '''using System.Collections.Generic;

namespace SHHH.Game
{
    /// <summary>
    /// Mom's English script - GENERATED from Tools/audio_v8/mom_lines_v8.json (V8, 2026-11-09, original lines for the continuous match).
    /// Audio "vo_&lt;id&gt;" (offline voice bank, Tools/audio_v8/make_vo.py) plays when present; subtitles always shown. Lines with {name}:
    /// the "vo_name_&lt;name&gt;" clip is spoken first, then the line.
    /// </summary>
    public static class MomLines
    {
        static readonly Dictionary<string, string> L = new Dictionary<string, string>
        {
''' + "\n".join(rows) + '''
        };

        public static bool HasName(string id) => L.TryGetValue(id, out var s) && s.Contains("{name}");
        public static bool Has(string id) => L.ContainsKey(id);

        public static string Text(string id, string name = null)
        {
            if (!L.TryGetValue(id, out var s)) return "";
            return name != null ? s.Replace("{name}", name) : s.Replace("{name}! ", "").Replace("{name} - ", "");
        }
    }
}
'''
open(os.path.join(root, "UnityProject/Assets/SHHH/Scripts/Game/Agents/MomLines.cs"), "w").write(cs)
print(len(rows), "lines")
