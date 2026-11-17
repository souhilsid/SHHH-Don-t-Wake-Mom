using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    /// <summary>
    /// Procedural round generator (spec §04.3): picks one owned recipe per active seat from the round's focus areas, balanced by
    /// difficulty, avoiding recently used recipes and exclusive-fixture conflicts, restricted to areas the current house build can
    /// reach. Every pick is proven completable by running the solver on a trial board before it is accepted. Seeded; the seed
    /// and every rejection are logged for reproduction.
    /// </summary>
    public static class RecipeGenerator
    {
        public sealed class Pick { public int Seat; public RecipeDef Recipe; }
        public sealed class Result
        {
            public ulong Seed; public int RoundIndex; public readonly List<Pick> Picks = new List<Pick>(); public readonly List<string> Log = new List<string>();
            public HouseArea[] Focus = Array.Empty<HouseArea>();
        }

        /// <summary>Data-driven round focus (round index 0..2); filtered by availability at generation time.</summary>
        public static readonly HouseArea[][] RoundFocus =
        {
            new[] { HouseArea.Living, HouseArea.Foyer, HouseArea.Hallway },
            new[] { HouseArea.Kitchen, HouseArea.PetArea },
            new[] { HouseArea.Laundry, HouseArea.Bathroom, HouseArea.ChildBedroom },
        };

        /// <summary>
        /// V13 (owner 2026-11-17: "tasks like washing machine, cleaning plates using water and stuff like that I'm not getting them at
        /// all, only easy tasks"; 200 solo 10-min matches gave the player 65 % Easy / 27 % Medium / 8 % Hard, plates 8 %, load the washer
        /// 2.5 %). The big physical jobs of each room are FEATURED: every human seat gets one first when its room has one that fits a
        /// table and a free fixture; the other seats (bots) are dealt as before.
        /// </summary>
        public static readonly string[] Featured = { "KT01", "KT17", "LD04", "LD05" };

        public static Result Generate(ulong seed, int roundIndex, IList<int> seats, ICollection<HouseArea> available,
            ICollection<string> recentlyUsed = null, RecipeDifficulty maxDifficulty = RecipeDifficulty.Hard, Func<RecipeDef, bool> fits = null,
            Func<int, bool> isHuman = null)
        {
            var res = new Result { Seed = seed, RoundIndex = roundIndex };
            var rng = new DetRng(seed ^ 0x52454350UL ^ ((ulong)roundIndex << 32));
            var focus = RoundFocus[Math.Max(0, Math.Min(RoundFocus.Length - 1, roundIndex))].Where(available.Contains).ToArray();
            if (focus.Length == 0) { focus = available.ToArray(); res.Log.Add("focus areas not built; falling back to all available areas"); }
            res.Focus = focus;
            var pool = RecipeCatalog.All.Where(r => focus.Contains(r.Area) && r.Difficulty <= maxDifficulty && (fits == null || fits(r))).ToList();
            int unavailable = RecipeCatalog.All.Count(r => !available.Contains(r.Area));
            if (unavailable > 0) res.Log.Add($"{unavailable} recipes excluded: their area is not built in this house");
            if (recentlyUsed != null) { int before = pool.Count; pool = pool.Where(r => !recentlyUsed.Contains(r.Id)).ToList(); if (pool.Count < seats.Count) pool = RecipeCatalog.All.Where(r => focus.Contains(r.Area) && r.Difficulty <= maxDifficulty && (fits == null || fits(r))).ToList(); else res.Log.Add($"{before - pool.Count} recently used recipes rested"); }
            rng.Shuffle(pool);
            // balance: target the same difficulty for everyone (pick a level present at least seats.Count times, else mix)
            // seeded target difficulty, weighted by how many candidates each level has (so rare Hard recipes still appear); everyone
            // gets that level when enough candidates exist, otherwise the nearest levels fill the remaining seats (logged)
            var byDiff = pool.GroupBy(r => r.Difficulty).OrderBy(g => g.Key).ToList();
            IGrouping<RecipeDifficulty, RecipeDef> target = null;
            // V13: when a human seat gets a featured (big) job, the room's other jobs aim at its difficulty - the bots get comparable work
            var featuredLevel = isHuman != null && seats.Any(isHuman)
                ? RecipeCatalog.All.Where(r => Featured.Contains(r.Id) && focus.Contains(r.Area) && (fits == null || fits(r)) && (recentlyUsed == null || !recentlyUsed.Contains(r.Id))).Select(r => (RecipeDifficulty?)r.Difficulty).Max()
                : null;
            if (featuredLevel.HasValue && byDiff.Any(g => g.Key == featuredLevel.Value))
            {
                target = byDiff.First(g => g.Key == featuredLevel.Value);
                res.Log.Add($"difficulty target {target.Key} (matches the featured job)");
            }
            else if (byDiff.Count > 0)
            {
                int roll = rng.Range(0, pool.Count), acc = 0;
                foreach (var g in byDiff) { acc += g.Count(); if (roll < acc) { target = g; break; } }
                res.Log.Add($"difficulty target {target.Key} ({target.Count()} candidates{(target.Count() < seats.Count ? ", mixed with nearest levels" : "")})");
            }
            var usedFixtures = new HashSet<string>();
            // featured jobs for the human seats first
            var humanSeats = isHuman != null ? seats.Where(isHuman).ToList() : new List<int>();
            var featured = new List<RecipeDef>();
            if (humanSeats.Count > 0)
            {
                var cands = RecipeCatalog.All.Where(r => Featured.Contains(r.Id) && focus.Contains(r.Area) && r.Difficulty <= maxDifficulty
                                                         && (fits == null || fits(r)) && (recentlyUsed == null || !recentlyUsed.Contains(r.Id))).ToList();
                rng.Shuffle(cands);
                foreach (var c in cands)
                {
                    if (featured.Count >= humanSeats.Count) break;
                    if (c.Fixtures.Any(f => usedFixtures.Contains(f.Kind)) || Validate(c) != null) continue;
                    featured.Add(c); foreach (var f in c.Fixtures) usedFixtures.Add(f.Kind);
                    res.Log.Add($"{c.Id} featured for human seat {humanSeats[featured.Count - 1]}");
                }
            }
            var chosen = new List<RecipeDef>();
            int wanted = seats.Count - featured.Count;
            foreach (var r in pool.OrderBy(r => target == null ? 0 : Math.Abs((int)r.Difficulty - (int)target.Key)))
            {
                if (chosen.Count >= wanted) break;
                if (featured.Contains(r)) continue;
                if (r.Fixtures.Any(f => usedFixtures.Contains(f.Kind))) { res.Log.Add($"{r.Id} skipped: shares a fixture with another active recipe"); continue; }
                var why = Validate(r);
                if (why != null) { res.Log.Add($"{r.Id} rejected: {why}"); continue; }
                chosen.Add(r); foreach (var f in r.Fixtures) usedFixtures.Add(f.Kind);
            }
            // human seats take the featured jobs (in order), every other seat the regular picks
            int fi = 0, ci = 0;
            foreach (int seat in seats)
            {
                if (fi < featured.Count && humanSeats.Contains(seat)) res.Picks.Add(new Pick { Seat = seat, Recipe = featured[fi++] });
                else if (ci < chosen.Count) res.Picks.Add(new Pick { Seat = seat, Recipe = chosen[ci++] });
            }
            if (res.Picks.Count < seats.Count) res.Log.Add($"only {res.Picks.Count}/{seats.Count} seats received a recipe");
            return res;
        }

        /// <summary>Reachability/consistency validator: structure (2–5 steps, ≥1 sabotage), not complete at start, solvable by an owner.</summary>
        public static string Validate(RecipeDef r)
        {
            if (r.Steps.Count < 2 || r.Steps.Count > 5) return $"has {r.Steps.Count} steps (need 2-5)";
            if (r.Sabotage.Count == 0) return "no sabotage variant";
            foreach (var s in r.Steps)
            {
                if (s.Fixture != null && r.Fixtures.All(f => f.Kind != s.Fixture)) return $"step '{s.Label}' uses missing fixture {s.Fixture}";
                if (s.PropKind != null && r.Props.All(p => p.Kind != s.PropKind)) return $"step '{s.Label}' uses missing prop {s.PropKind}";
                foreach (var a in s.After) if (a < 0 || a >= r.Steps.Count) return "bad step dependency";
            }
            var b = new RecipeBoard(); var inst = b.Add(r, 0);
            if (inst.Complete || inst.Satisfied.All(x => x)) return "already complete at start";
            var sol = RecipeSolver.Solve(b, inst, 0);
            if (!sol.Complete) return "owner cannot complete it: " + sol.Failure;
            return null;
        }
    }
}
