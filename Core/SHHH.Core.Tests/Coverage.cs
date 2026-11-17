using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Core;

/// <summary>`dotnet run -c Release -- --coverage &lt;out.csv&gt; &lt;Canonical_Recipe_Audit.csv&gt;`: writes Docs/SHHH_PHYSICS_TASK_COVERAGE.csv from the LIVE
/// recipe catalog (steps, verbs, props, fixtures, sabotage) + the last Unity recipe_diag result per ID + the primitive each step maps to and
/// whether that primitive is physically real yet. Regenerate after every recipe change so the file never drifts from source.</summary>
static class Coverage
{
    // live interaction depth per (verb, context) - kept honest: "gesture" means it does not yet meet the V9 physical bar
    static string Primitive(RecipeStepDef s, RecipeDef r)
    {
        bool washable = r.Props.Any(p => p.Kind == s.PropKind && p.StrokeCap > 0);
        switch (s.Kind)
        {
            case StepKind.Place: return s.RequireOriented ? "sliding/placing (slot snap + orientation)" : "sliding/placing (slot snap)";
            case StepKind.OpenState: return "hinging (poke lid - gesture)";
            case StepKind.CloseState: return s.Fixture == "sink_tap" ? "hinging (tap lever pushed by hand)" : "hinging (poke lid - gesture)";
            case StepKind.Strokes: return "wiping/scrubbing (patch-local contact)";
            case StepKind.Flag: return "pressing (poke key - gesture)";
            case StepKind.Paired: return "connecting/pairing (touch partner)";
            case StepKind.Attached: return "connecting (touch partner)";
            case StepKind.Stage:
                if (s.Verb == Verb.Fill && s.Fixture == "sink_tap") return "pouring/rinsing (held in the real tap stream)";
                if (s.Verb == Verb.Fill && s.Fixture == "soap_pump") return "pumping (real pump head, dose into sponge)";
                if (s.Verb == Verb.Stroke && washable) return "scrubbing (soapy sponge contact)";
                if (s.Verb == Verb.Fold) return "folding (rub gesture - to replace with two-hand cloth)";
                if (s.Verb == Verb.Fill) return "pouring (rub gesture - to replace with tilt + volume)";
                return "scrubbing (rub gesture)";
        }
        return "?";
    }
    static bool Real(string prim) => !prim.Contains("gesture") && !prim.Contains("?");

    static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'") + "\"";

    public static int Write(string outPath, string auditPath)
    {
        var audit = new Dictionary<string, string>();
        if (File.Exists(auditPath))
            foreach (var line in File.ReadAllLines(auditPath).Skip(1))
            {
                var cols = SplitCsv(line); if (cols.Count > 11) audit[cols[0]] = cols[11];
            }
        var lines = new List<string> { "id,name,area,difficulty,steps,step_primitives,physical_steps_real,props,fixtures,sabotage,repair,evidence,quiet_path,bot_path,photon_authority,editor_recipe_diag,full_match,quest,blocker_or_next" };
        foreach (var r in RecipeCatalog.All)
        {
            var prims = r.Steps.Select(s => Primitive(s, r)).ToList();
            int real = prims.Count(Real);
            string steps = string.Join(" | ", r.Steps.Select((s, i) => $"{i + 1}. {s.Label}"));
            string props = string.Join(" ", r.Props.Select(p => p.Count + "x" + p.Kind + (string.IsNullOrEmpty(p.Tag) ? "" : ":" + p.Tag)));
            string fixtures = string.Join(" ", r.Fixtures.Select(f => f.Kind + (f.HasDoor ? "(door)" : "")));
            string sab = string.Join(" / ", r.Sabotage.Select(s => s.Kind + ": " + s.Note));
            audit.TryGetValue(r.Id, out var diag);
            string next = real == prims.Count ? "polish: real models/VFX/SFX per V9 §2C" : "replace gesture steps: " + string.Join("; ", prims.Where(p => !Real(p)).Distinct());
            if (r.Id == "KT01") diag = "V9 2026-11-10: PLAY desktop sim - pump/scrub/rinse at the house sink verified; rack placement intermittently fails (see QA index)";
            lines.Add(string.Join(",", new[] { r.Id, Q(r.DisplayName), r.Area.ToString(), r.Difficulty.ToString(), Q(steps), Q(string.Join(" | ", prims)), $"{real}/{prims.Count}",
                Q(props), Q(fixtures), Q(sab), Q(r.Repair), Q(r.Evidence), Q(r.QuietPath), "RecipeSolver plan -> bot motor -> same RecipeWorld ops",
                "host RecipeWorld authority; clients send RecipeAction intents; journal replicated", Q(diag ?? "not run"), "not run per recipe (continuous autopilot covers a sample)", "NOT TESTED ON HEADSET", Q(next) }));
        }
        File.WriteAllLines(outPath, lines);
        Console.WriteLine($"coverage: {RecipeCatalog.All.Count} recipes -> {outPath}");
        return 0;
    }

    static List<string> SplitCsv(string line)
    {
        var res = new List<string>(); var cur = new System.Text.StringBuilder(); bool q = false;
        foreach (var ch in line) { if (ch == '"') q = !q; else if (ch == ',' && !q) { res.Add(cur.ToString()); cur.Clear(); } else cur.Append(ch); }
        res.Add(cur.ToString()); return res;
    }
}
