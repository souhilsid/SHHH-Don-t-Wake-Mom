using System;
using System.Collections.Generic;
using System.Linq;
using SHHH.Core;

/// <summary>V13 probe (owner: "washing machine, cleaning plates with water ... I'm not getting them at all, only easy tasks"):
/// `core_tests.sh --jobstats [matches] [seconds]` - which recipes a solo player (seat 0) and the bots receive over many seeds.</summary>
static class JobStats
{
    public static int Run(int matches, float seconds)
    {
        var mine = new Dictionary<string, int>(); var all = new Dictionary<string, int>(); var diff = new Dictionary<string, int>();
        for (int i = 0; i < matches; i++)
        {
            var m = new MatchController(); m.Roster.SeatHuman(900, "Pip"); m.Roster.SetReady(900, true); m.SetMatchLength(seconds);
            if (!m.Start(MatchMode.Solo, (ulong)(1000 + i * 7919), out var err)) { Console.WriteLine("start failed: " + err); return 1; }
            m.SkipIntro(); m.Tick(0.01f); if (m.Round == null || m.Round.Recipes == null) { Console.WriteLine("no round/recipes"); return 1; }
            foreach (var inst in m.Round.Recipes.Instances)
            {
                string k = inst.Def.Id + " " + inst.Def.DisplayName + " [" + inst.Def.Difficulty + "]";
                all[k] = all.TryGetValue(k, out var a) ? a + 1 : 1;
                if (inst.Owner == 0) { mine[k] = mine.TryGetValue(k, out var b) ? b + 1 : 1; diff[inst.Def.Difficulty.ToString()] = diff.TryGetValue(inst.Def.Difficulty.ToString(), out var c) ? c + 1 : 1; }
            }
        }
        Console.WriteLine($"{matches} solo matches of {seconds:0} s. MY jobs by difficulty: " + string.Join(", ", diff.Select(kv => kv.Key + " " + kv.Value)));
        Console.WriteLine("MY jobs:"); foreach (var kv in mine.OrderByDescending(x => x.Value)) Console.WriteLine($"  {kv.Value,4}  {kv.Key}");
        foreach (var id in new[] { "KT01", "LD04", "LD05" }) Console.WriteLine($"{id}: given to anyone {all.Where(x => x.Key.StartsWith(id)).Sum(x => x.Value)} times, to me {mine.Where(x => x.Key.StartsWith(id)).Sum(x => x.Value)}");
        return 0;
    }
}
