using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SHHH.Core
{
    /// <summary>
    /// V7 Gate 8: local-first, versioned player progress (spec §07): matches, XP/level, achievements, cosmetic unlocks, equipped
    /// cosmetics and preferences. Rewards come ONLY from the authoritative <see cref="MatchSummary"/> after a finished match, never
    /// from a client, never in dev/QA sessions, and sabotage itself earns nothing (no sabotage/repair farming: foils are capped).
    /// No currencies, no purchases.
    /// </summary>
    public sealed class PlayerProgress
    {
        public const int CurrentVersion = 2;
        public int Version = CurrentVersion;
        public int Xp, MatchesPlayed, Wins, RecipesCompleted, Foils, ProvingCards;
        public readonly SortedSet<string> Achievements = new SortedSet<string>();
        public readonly SortedSet<string> Unlocked = new SortedSet<string>(Cosmetics.Starter);
        public readonly Dictionary<string, string> Equipped = new Dictionary<string, string>();      // "pip.hat" -> "hat_beanie"
        public readonly Dictionary<string, string> Preferences = new Dictionary<string, string>();   // free-form local prefs
        public readonly List<string> LastAwards = new List<string>();
        public int Level => ProgressionRules.LevelFor(Xp);
    }

    public static class Cosmetics
    {
        public sealed class Item { public string Id, Slot, Name; public int Level; public string Achievement; }
        public static readonly string[] Slots = { "hat", "glasses", "pyjama", "accessory", "emote" };
        public static readonly Item[] All =
        {
            new Item { Id = "hat_none", Slot = "hat", Name = "No hat" },
            new Item { Id = "glasses_none", Slot = "glasses", Name = "No glasses" },
            new Item { Id = "pyjama_classic", Slot = "pyjama", Name = "Classic pyjamas" },
            new Item { Id = "accessory_none", Slot = "accessory", Name = "Nothing" },
            new Item { Id = "emote_shush", Slot = "emote", Name = "Shush" },
            new Item { Id = "hat_beanie", Slot = "hat", Name = "Sleepy beanie", Level = 2 },
            new Item { Id = "glasses_round", Slot = "glasses", Name = "Round glasses", Level = 2 },
            new Item { Id = "emote_wave", Slot = "emote", Name = "Tiny wave", Level = 3 },
            new Item { Id = "pyjama_stars", Slot = "pyjama", Name = "Star pyjamas", Level = 3 },
            new Item { Id = "hat_nightcap", Slot = "hat", Name = "Nightcap", Level = 4 },
            new Item { Id = "accessory_teddy", Slot = "accessory", Name = "Pocket teddy", Level = 5 },
            new Item { Id = "glasses_star", Slot = "glasses", Name = "Star glasses", Level = 6 },
            new Item { Id = "emote_tiptoe", Slot = "emote", Name = "Tiptoe", Level = 7 },
            new Item { Id = "pyjama_moon", Slot = "pyjama", Name = "Moon onesie", Level = 8 },
            new Item { Id = "hat_crown", Slot = "hat", Name = "Paper crown", Achievement = "first_win" },
            new Item { Id = "accessory_magnifier", Slot = "accessory", Name = "Detective magnifier", Achievement = "detective" },
            new Item { Id = "glasses_sleepmask", Slot = "glasses", Name = "Sleep mask (on forehead)", Achievement = "silent_night" },
            new Item { Id = "accessory_toolbelt", Slot = "accessory", Name = "Fix-it toolbelt", Achievement = "fixer" },
        };
        public static readonly string[] Starter = All.Where(i => i.Level == 0 && i.Achievement == null).Select(i => i.Id).ToArray();
        public static Item Get(string id) => All.FirstOrDefault(i => i.Id == id);
    }

    /// <summary>
    /// Packs a seat's look into the existing replicated cosmetic int (no network schema change): bits 0-1 = sibling colour/character,
    /// then 5 bits per wardrobe slot = index into <see cref="Cosmetics.All"/> (0 = default). Purely visual, never gameplay.
    /// </summary>
    public static class Outfit
    {
        public static int Encode(int colour, IDictionary<string, string> slotToItem)
        {
            int code = ((colour % 4) + 4) % 4;
            for (int i = 0; i < Cosmetics.Slots.Length; i++)
            {
                int idx = 0;
                if (slotToItem != null && slotToItem.TryGetValue(Cosmetics.Slots[i], out var id)) { int k = Array.FindIndex(Cosmetics.All, c => c.Id == id); if (k > 0 && k < 32) idx = k; }
                code |= idx << (2 + i * 5);
            }
            return code;
        }
        public static Dictionary<string, string> Decode(int code)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i < Cosmetics.Slots.Length; i++)
            {
                int idx = (code >> (2 + i * 5)) & 31;
                if (idx > 0 && idx < Cosmetics.All.Length && Cosmetics.All[idx].Slot == Cosmetics.Slots[i]) d[Cosmetics.Slots[i]] = Cosmetics.All[idx].Id;
            }
            return d;
        }
        public static int Colour(int code) => ((code % 4) + 4) % 4;
        /// <summary>The local player's equipped look for a character, as the cosmetic int to send.</summary>
        public static int ForLocal(PlayerProgress p, int colour, string character)
        {
            var d = new Dictionary<string, string>();
            foreach (var slot in Cosmetics.Slots) d[slot] = ProgressionRules.EquippedOr(p, character, slot);
            return Encode(colour, d);
        }
        /// <summary>Bots dress up from the whole wardrobe (decorative variety; deterministic by seed).</summary>
        public static int ForBot(int colour, ulong seed)
        {
            var rng = new DetRng(seed * 7919UL + (ulong)colour); var d = new Dictionary<string, string>();
            foreach (var slot in Cosmetics.Slots) { var opts = Cosmetics.All.Where(c => c.Slot == slot).ToArray(); if (rng.Value() < 0.6f) d[slot] = opts[rng.Range(0, opts.Length)].Id; }
            return Encode(colour, d);
        }
    }

    public static class ProgressionRules
    {
        public static readonly (string id, string name, string how)[] AchievementList =
        {
            ("first_match", "Lights Out", "Finish a match"),
            ("first_win", "Top Sibling", "Win a match"),
            ("detective", "Little Detective", "Show Mom a memory that proves who did it"),
            ("fixer", "Fix-It Kid", "Foil 3 sabotages in total"),
            ("silent_night", "Silent Night", "Finish a match without a single noise blamed on you"),
            ("tidy_three", "Tidy Three", "Complete 3 jobs in one match"),
        };
        public static int LevelFor(int xp) { int lvl = 1, need = 200; while (xp >= need && lvl < 50) { xp -= need; lvl++; need += 100; } return lvl; }
        public const int XpMatch = 50, XpRound = 30, XpPlace = 30, XpWin = 150, XpRecipe = 20, XpFoil = 15, FoilCapPerMatch = 3, XpProvingCard = 25;

        /// <summary>Apply one finished match for the local seat. Returns the award lines shown on the results screen.</summary>
        public static List<string> Award(PlayerProgress p, MatchSummary s, int seat, bool devSession)
        {
            var lines = new List<string>(); p.LastAwards.Clear();
            if (devSession) { lines.Add("Practice/QA session - no progress recorded"); p.LastAwards.AddRange(lines); return lines; }
            if (!ActorIds.IsSibling(seat)) return lines;
            int before = p.Level;
            p.MatchesPlayed++; int xp = XpMatch; lines.Add($"+{XpMatch} XP for playing");
            int rounds = s.RoundsSurvived[seat]; if (rounds > 0) { xp += rounds * XpRound; lines.Add($"+{rounds * XpRound} XP for {rounds} round(s) awake"); }
            if (s.Continuous)
            {   // one match: placement XP (2nd/3rd place still earn something), no rounds
                int place = s.Ranking.IndexOf(seat); int px = place == 1 ? 2 * XpPlace : place == 2 ? XpPlace : 0;
                if (px > 0) { xp += px; lines.Add($"+{px} XP for {(place == 1 ? "2nd" : "3rd")} place"); }
            }
            bool won = s.WinnerSlot == seat; if (won) { p.Wins++; xp += XpWin; lines.Add($"+{XpWin} XP - you won!"); }
            int rc = s.RecipesCompleted[seat]; if (rc > 0) { xp += rc * XpRecipe; p.RecipesCompleted += rc; lines.Add($"+{rc * XpRecipe} XP for {rc} job(s) done"); }
            int foils = Math.Min(FoilCapPerMatch, s.Foils[seat]); if (foils > 0) { xp += foils * XpFoil; p.Foils += foils; lines.Add($"+{foils * XpFoil} XP for fixing sabotage"); }
            int cards = Math.Min(2, s.ProvingCards[seat]); if (cards > 0) { xp += cards * XpProvingCard; p.ProvingCards += cards; lines.Add($"+{cards * XpProvingCard} XP for telling Mom the truth"); }
            p.Xp += xp;
            void Ach(string id) { if (p.Achievements.Add(id)) lines.Add("Achievement: " + AchievementList.First(a => a.id == id).name); }
            Ach("first_match");
            if (won) Ach("first_win");
            if (s.ProvingCards[seat] > 0) Ach("detective");
            if (p.Foils >= 3) Ach("fixer");
            if (s.CumulativeNoise[seat] <= 0.01f && (rounds >= 1 || (s.Continuous && s.MatchSeconds >= 60f))) Ach("silent_night");
            if (rc >= 3) Ach("tidy_three");
            foreach (var it in Cosmetics.All)
                if (!p.Unlocked.Contains(it.Id) && ((it.Level > 0 && p.Level >= it.Level) || (it.Achievement != null && p.Achievements.Contains(it.Achievement))))
                { p.Unlocked.Add(it.Id); lines.Add("Unlocked: " + it.Name); }
            if (p.Level > before) lines.Add($"Level up! Level {p.Level}");
            p.LastAwards.AddRange(lines);
            return lines;
        }

        /// <summary>Equip only what is unlocked (anything else is refused).</summary>
        public static bool Equip(PlayerProgress p, string character, string itemId)
        {
            var it = Cosmetics.Get(itemId); if (it == null || !p.Unlocked.Contains(itemId)) return false;
            p.Equipped[character + "." + it.Slot] = itemId; return true;
        }
        public static string EquippedOr(PlayerProgress p, string character, string slot) => p.Equipped.TryGetValue(character + "." + slot, out var v) ? v : slot + "_none";
    }

    /// <summary>
    /// Save format: UTF-8 "key=value" lines + a final "sum=" checksum line. Writes go to a temp file, the previous good file is kept
    /// as .bak, then the temp is moved into place (power-loss safe). Load falls back to .bak, then to a fresh profile, and migrates
    /// older versions (v1 had only xp/matches/wins/unlocked).
    /// </summary>
    public static class ProgressStore
    {
        public static string Serialize(PlayerProgress p)
        {
            var sb = new StringBuilder();
            void L(string k, string v) => sb.Append(k).Append('=').Append(Esc(v)).Append('\n');
            L("version", p.Version.ToString(CultureInfo.InvariantCulture));
            L("xp", p.Xp.ToString(CultureInfo.InvariantCulture)); L("matches", p.MatchesPlayed.ToString(CultureInfo.InvariantCulture)); L("wins", p.Wins.ToString(CultureInfo.InvariantCulture));
            L("recipes", p.RecipesCompleted.ToString(CultureInfo.InvariantCulture)); L("foils", p.Foils.ToString(CultureInfo.InvariantCulture)); L("cards", p.ProvingCards.ToString(CultureInfo.InvariantCulture));
            L("achievements", string.Join(",", p.Achievements)); L("unlocked", string.Join(",", p.Unlocked));
            foreach (var kv in p.Equipped.OrderBy(k => k.Key)) L("equip." + kv.Key, kv.Value);
            foreach (var kv in p.Preferences.OrderBy(k => k.Key)) L("pref." + kv.Key, kv.Value);
            var body = sb.ToString();
            return body + "sum=" + Sum(body) + "\n";
        }

        public static bool TryParse(string text, out PlayerProgress p, out string why)
        {
            p = null; why = null;
            if (string.IsNullOrEmpty(text)) { why = "empty"; return false; }
            int i = text.LastIndexOf("sum=", StringComparison.Ordinal);
            if (i < 0) { why = "no checksum"; return false; }
            var body = text.Substring(0, i);
            if (text.Substring(i + 4).Trim() != Sum(body)) { why = "checksum mismatch (corrupt)"; return false; }
            var d = new Dictionary<string, string>();
            foreach (var line in body.Split('\n')) { int e = line.IndexOf('='); if (e > 0) d[line.Substring(0, e)] = Unesc(line.Substring(e + 1)); }
            if (!d.TryGetValue("version", out var vs) || !int.TryParse(vs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ver)) { why = "no version"; return false; }
            if (ver > PlayerProgress.CurrentVersion) { why = $"newer save v{ver}"; return false; }
            p = new PlayerProgress();
            int I(string k) => d.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Max(0, n) : 0;
            p.Xp = I("xp"); p.MatchesPlayed = I("matches"); p.Wins = I("wins");
            if (ver >= 2) { p.RecipesCompleted = I("recipes"); p.Foils = I("foils"); p.ProvingCards = I("cards"); }  // v1 -> v2: new counters start at 0
            if (d.TryGetValue("achievements", out var a) && a.Length > 0) foreach (var x in a.Split(',')) if (ProgressionRules.AchievementList.Any(q => q.id == x)) p.Achievements.Add(x);
            if (d.TryGetValue("unlocked", out var u) && u.Length > 0) foreach (var x in u.Split(',')) if (Cosmetics.Get(x) != null) p.Unlocked.Add(x);
            foreach (var kv in d)
            {
                if (kv.Key.StartsWith("equip.")) { var k = kv.Key.Substring(6); if (p.Unlocked.Contains(kv.Value)) p.Equipped[k] = kv.Value; }
                else if (kv.Key.StartsWith("pref.")) p.Preferences[kv.Key.Substring(5)] = kv.Value;
            }
            p.Version = PlayerProgress.CurrentVersion;
            return true;
        }

        public static PlayerProgress Load(string path, out string report)
        {
            foreach (var f in new[] { path, path + ".bak" })
            {
                try { if (File.Exists(f) && TryParse(File.ReadAllText(f, Encoding.UTF8), out var p, out var why)) { report = f == path ? "loaded" : "recovered from backup"; return p; } else if (File.Exists(f)) { report = "unreadable: " + f; } }
                catch (Exception e) { report = "error " + e.Message; }
            }
            report = File.Exists(path) || File.Exists(path + ".bak") ? "corrupt save replaced by a fresh profile (old file kept as .corrupt)" : "new profile";
            try { if (File.Exists(path)) File.Copy(path, path + ".corrupt", true); } catch { }
            return new PlayerProgress();
        }

        public static void Save(string path, PlayerProgress p)
        {
            var dir = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, Serialize(p), Encoding.UTF8);
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\n", "\\n");
        static string Unesc(string s) => s.Replace("\\n", "\n").Replace("\\\\", "\\");
        static string Sum(string s) { unchecked { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return h.ToString("x8"); } }
    }
}
