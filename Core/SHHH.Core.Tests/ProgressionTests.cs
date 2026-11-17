using System;
using System.IO;
using System.Linq;
using SHHH.Core;

/// <summary>Gate 8 local progression: awards only from the authoritative summary, no farming, save/load/migration/corruption.</summary>
static class ProgressionTests
{
    public static void Run(Action<string, Action> Test, Action<bool, string> Check)
    {
        MatchSummary Summary(int winner, int recipes, int foils, int cards, float noise, int rounds)
        {
            var s = new MatchSummary { WinnerSlot = winner };
            s.RecipesCompleted[0] = recipes; s.Foils[0] = foils; s.ProvingCards[0] = cards; s.CumulativeNoise[0] = noise; s.RoundsSurvived[0] = rounds;
            return s;
        }
        Test("PROG-01 a won match awards XP, achievements and unlocks from the summary", () =>
        {
            var p = new PlayerProgress();
            var lines = ProgressionRules.Award(p, Summary(0, 3, 1, 1, 0f, 3), 0, false);
            Check(p.Xp == 50 + 90 + 150 + 60 + 15 + 25, $"xp {p.Xp}");
            Check(p.Achievements.SetEquals(new[] { "first_match", "first_win", "detective", "silent_night", "tidy_three" }), string.Join(",", p.Achievements));
            Check(p.Unlocked.Contains("hat_crown") && p.Unlocked.Contains("accessory_magnifier") && p.Unlocked.Contains("glasses_sleepmask"), "achievement cosmetics unlocked");
            Check(p.Level == 2 && p.Unlocked.Contains("hat_beanie"), $"level {p.Level}");
            Check(lines.Any(l => l.StartsWith("Level up")), "level-up line");
        });
        Test("PROG-02 dev/QA sessions record nothing; foils are capped (no sabotage/repair farming)", () =>
        {
            var p = new PlayerProgress();
            ProgressionRules.Award(p, Summary(0, 3, 9, 0, 1f, 3), 0, true);
            Check(p.Xp == 0 && p.MatchesPlayed == 0 && p.Achievements.Count == 0, "dev session ignored");
            ProgressionRules.Award(p, Summary(1, 0, 40, 0, 5f, 1), 0, false);
            Check(p.Foils == ProgressionRules.FoilCapPerMatch && p.Xp == 50 + 30 + ProgressionRules.FoilCapPerMatch * 15, $"capped foils {p.Foils} xp {p.Xp}");
        });
        Test("PROG-03 cosmetics: only unlocked items can be equipped", () =>
        {
            var p = new PlayerProgress();
            Check(!ProgressionRules.Equip(p, "pip", "hat_crown"), "locked crown refused");
            Check(ProgressionRules.Equip(p, "pip", "emote_shush"), "starter emote ok");
            p.Unlocked.Add("hat_crown"); Check(ProgressionRules.Equip(p, "pip", "hat_crown") && ProgressionRules.EquippedOr(p, "pip", "hat") == "hat_crown", "crown equipped");
        });
        Test("PROG-04 save/load round trip, backup recovery, corruption detection, v1 migration, newer-version refusal", () =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "shhh_prog_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "progress.sav");
            var p = new PlayerProgress(); ProgressionRules.Award(p, Summary(0, 2, 1, 0, 2f, 3), 0, false); ProgressionRules.Equip(p, "nell", "hat_crown"); p.Preferences["seated"] = "true";
            ProgressStore.Save(path, p);
            var q = ProgressStore.Load(path, out var rep);
            Check(rep == "loaded" && q.Xp == p.Xp && q.Wins == 1 && q.Achievements.SetEquals(p.Achievements) && q.Equipped["nell.hat"] == "hat_crown" && q.Preferences["seated"] == "true", "round trip: " + rep);
            p.Xp += 10; ProgressStore.Save(path, p);                         // second save leaves the first as .bak
            File.WriteAllText(path, File.ReadAllText(path).Replace("xp=", "xp=9")); // tamper -> checksum fails
            var r = ProgressStore.Load(path, out rep);
            Check(rep == "recovered from backup" && r.Xp == p.Xp - 10, "backup used: " + rep + " xp " + r.Xp);
            File.WriteAllText(path, "garbage"); File.WriteAllText(path + ".bak", "garbage");
            var f = ProgressStore.Load(path, out rep);
            Check(f.Xp == 0 && rep.StartsWith("corrupt") && File.Exists(path + ".corrupt"), "fresh profile on total corruption: " + rep);
            // v1 file (no new counters) migrates
            string v1body = "version=1\nxp=300\nmatches=2\nwins=1\nachievements=first_match\nunlocked=hat_none,glasses_none,pyjama_classic,accessory_none,emote_shush\n";
            uint h = 2166136261; unchecked { foreach (char c in v1body) { h ^= c; h *= 16777619; } }
            Check(ProgressStore.TryParse(v1body + "sum=" + h.ToString("x8") + "\n", out var m, out var why) && m.Xp == 300 && m.Version == PlayerProgress.CurrentVersion && m.Foils == 0, "v1 migrated " + why);
            string v9body = "version=9\nxp=1\n"; h = 2166136261; unchecked { foreach (char c in v9body) { h ^= c; h *= 16777619; } }
            Check(!ProgressStore.TryParse(v9body + "sum=" + h.ToString("x8") + "\n", out _, out why) && why.Contains("newer"), "newer version refused: " + why);
            Directory.Delete(dir, true);
        });
        Test("PROG-06 outfit code round-trips through the replicated cosmetic int and keeps the colour in the low bits", () =>
        {
            var d = new System.Collections.Generic.Dictionary<string, string> { ["hat"] = "hat_crown", ["glasses"] = "glasses_round", ["pyjama"] = "pyjama_moon", ["accessory"] = "accessory_teddy", ["emote"] = "emote_tiptoe" };
            int code = Outfit.Encode(2, d);
            var back = Outfit.Decode(code);
            Check(Outfit.Colour(code) == 2 && ((code % 4) + 4) % 4 == 2, "colour bits");
            Check(d.All(kv => back.TryGetValue(kv.Key, out var v) && v == kv.Value), "all slots round-trip: " + string.Join(",", back.Select(kv => kv.Key + "=" + kv.Value)));
            Check(Outfit.Decode(3).Count == 0, "legacy colour-only codes decode to the default outfit");
        });
        Test("PROG-05 a real recipe match fills the summary stats the progression reads", () =>
        {
            RoundConfig.RecipeModeDefault = true;
            try
            {
                int recipes = 0, foils = 0, rounds = 0;
                for (ulong seed = 1; seed <= 10; seed++)
                {
                    var m = MatchSimulator.RunSolo(seed, BotDifficulty.Normal, BotProfile.For(BotDifficulty.Normal));
                    recipes += m.Summary.RecipesCompleted.Sum(); foils += m.Summary.Foils.Sum(); rounds += m.Summary.RoundsSurvived.Sum();
                    Check(m.Summary.RoundsSurvived[m.Summary.WinnerSlot] == 3, "winner stayed awake 3 rounds");
                }
                Console.WriteLine($"     10 matches: {recipes} recipes, {foils} foils, {rounds} rounds survived (total)");
                Check(recipes > 0 && rounds == 10 * (3 + 2 + 1), "stats filled (3+2+1 survivals per match)");
            }
            finally { RoundConfig.RecipeModeDefault = false; }
        });
    }
}
