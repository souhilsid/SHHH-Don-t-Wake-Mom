using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Core;

/// <summary>
/// 2026-11-09 directive Gate 1: ONE continuous 5/10-minute match in the whole connected house. No 4->3->2->1 eliminations,
/// one shared authoritative clock (keeps running while Mom visits), Mom penalties/timeouts instead of elimination, one full
/// confrontation per match (later ones quick), one decisive result + one award. Journaled host + replica.
/// </summary>
static class ContinuousTests
{
    sealed class Ctx { public MatchController Host, Rep; public RoundController R; public Action Flush; public List<string> Trace = new List<string>(); }

    static Ctx Start(float seconds, ulong seed, int humans = 4)
    {
        var host = new MatchController();
        for (int h = 0; h < humans; h++) { host.Roster.SeatHuman(900 + h, new[] { "Pip", "Nell", "Rory", "Tess" }[h]); host.Roster.SetReady(900 + h, true); }
        host.SetMatchLength(seconds);
        host.Journal = new MatchJournal();
        var rep = new MatchController(); byte[] carry = null;
        var ctx = new Ctx { Host = host, Rep = rep };
        ctx.Flush = () => { var b = host.Journal.TakePending(); if (b != null) MatchJournal.Apply(rep, b, ref carry); };
        if (!host.Start(humans == 1 ? MatchMode.Solo : MatchMode.PrivateOnline, seed, out var err)) throw new Exception(err);
        host.SkipIntro(); host.Tick(0.01f); for (int i = 0; i < 40; i++) host.Tick(0.1f);
        ctx.R = host.Round; ctx.R.Log.Sink = l => ctx.Trace.Add(l.ToString());
        return ctx;
    }

    static void RunUntil(Ctx c, Func<bool> cond, float max = 60f) { for (float t = 0; t < max && !cond(); t += 0.1f) c.Host.Tick(0.1f); }

    static Incident Crash(Ctx c, int actor)
    {
        var r = c.R;
        var loud = new[] { NoiseMaterial.Metal, NoiseMaterial.Glass, NoiseMaterial.Ceramic, NoiseMaterial.Plastic, NoiseMaterial.Wood, NoiseMaterial.Toy };
        int prop = r.Room.Props.Where(p => r.IsRecipeProp(p.Id) && !r.Recipes.World.Prop(r.RecipePropOf[p.Id]).IsPlaced).OrderBy(p => { int k = Array.IndexOf(loud, p.Material); return k < 0 ? 99 : k; }).First().Id;
        for (int k = 0; k < 40 && r.Phase == RoundPhase.Playing; k++)
        {
            if (!r.Grab(actor, prop)) throw new Exception("grab for crash failed");
            r.Release(actor, prop, 3f, true);
            r.SubmitImpact(prop, 9f, r.Room.Props.First(p => p.Id == prop).Spawn);
            c.Host.Tick(0.1f); c.Host.Tick(0.1f);
        }
        if (r.Phase != RoundPhase.MomSequence) throw new Exception("crash did not wake Mom");
        return r.Belief.Incidents.LastOrDefault(i => i.Kind == IncidentKind.Noise);
    }

    static void ToEvidence(Ctx c) => RunUntil(c, () => c.R.Interro != null && c.R.Mom.State == MomState.Interrogating && c.R.Interro.EvidenceOpen, 40f);
    static void PassAll(Ctx c) { foreach (int s in c.R.Roster.ActiveIndices()) c.R.PassInterrogation(s); }

    public static void Run(Action<string, Action> Test, Action<bool, string> Check)
    {
        bool prev = MatchController.ContinuousDefault; MatchController.ContinuousDefault = true;
        try
        {
            Test("CM-01 5-minute solo: 30 seeded headless matches, one continuous clock, nobody eliminated, one ranked result", () =>
            {
                int botWins = 0, humanWins = 0, finishedEarly = 0, penalties = 0, warnings = 0;
                for (ulong seed = 1; seed <= 30; seed++)
                {
                    int finished = 0;
                    var m = MatchController.CreateSolo("You", (BotDifficulty)(seed % 3), seed);
                    m.MatchFinished += _ => finished++;
                    MatchSimulator.Run(m, MatchMode.Solo, seed, BotProfile.For(BotDifficulty.Normal), 0.1f, 900f);
                    Check(m.Continuous && m.Rounds.Length == 1, "one continuous round config");
                    Check(m.Phase == MatchPhase.Winner, $"seed {seed} ended in {m.Phase}");
                    Check(finished == 1, $"seed {seed}: MatchFinished x{finished}");
                    Check(m.Summary.EliminationOrder.Count == 0 && m.Roster.ActiveCount == 4, $"seed {seed}: eliminations {m.Summary.EliminationOrder.Count}, active {m.Roster.ActiveCount}");
                    Check(m.RoundIndex == 0, $"seed {seed}: a second round started ({m.RoundIndex})");
                    Check(m.Summary.Ranking.Count == 4 && m.Summary.Ranking[0] == m.Summary.WinnerSlot, "ranking + winner");
                    for (int i = 1; i < 4; i++) Check(m.Summary.FinalScore[m.Summary.Ranking[i - 1]] >= m.Summary.FinalScore[m.Summary.Ranking[i]] - 0.01f, "ranking ordered by score");
                    Check(m.Summary.MatchSeconds <= 300f + 120f, $"seed {seed}: match ran {m.Summary.MatchSeconds:0}s (5 min + at most one closing visit)");
                    Check(m.Summary.MatchSeconds >= 299f, $"seed {seed}: match ended before 0:00 ({m.Summary.MatchSeconds:0}s)");
                    if (m.Summary.WinnerIsBot) botWins++; else humanWins++;
                    if (m.Summary.MatchSeconds < 299f) finishedEarly++;
                    penalties += m.Round.Penalties.Count; warnings += m.Round.Warnings;
                    Check(!m.Log.Lines.Any(l => l.Contains("ELIMINATED")), "no elimination lines");
                }
                Console.WriteLine($"     5-min solo sim: bot wins {botWins}, human(policy) wins {humanWins}, early finishes {finishedEarly}, penalties {penalties}, warnings {warnings}");
                Check(botWins > 0 && humanWins > 0, "both bots and the human policy can win");
            });

            Test("CM-02 10-minute house: 3 jobs per seat dealt over living room, kitchen and laundry; stations inside their rooms", () =>
            {
                var c = Start(MatchRules.TenMinutes, 7);
                var r = c.R;
                Check(r.Config.Continuous && r.Config.JobsPerSeat == 3 && Math.Abs(r.Config.Duration - 600f) < 0.01f, "10-min config");
                Check(r.Recipes.Instances.Count == 12, $"12 jobs (got {r.Recipes.Instances.Count})");
                for (int s = 0; s < 4; s++) Check(r.Recipes.Instances.Count(i => i.Owner == s) == 3, $"seat {s} has 3 jobs");
                var rooms = new HashSet<RoomId>();
                foreach (var f in r.RecipeFixtures) rooms.Add(HouseLayout.RoomAt(f.Position));
                Check(rooms.SetEquals(HouseLayout.Rooms), "fixtures in all three rooms: " + string.Join(",", rooms));
                foreach (var inst in r.Recipes.Instances)
                {
                    var fx = r.RecipeFixtures.Where(f => f.Instance == inst.Index).Select(f => HouseLayout.RoomAt(f.Position)).Distinct().ToList();
                    Check(fx.Count == 1, $"{inst.Def.Id}: one job stays in one room");
                }
                Check(r.Room.Props.Select(p => p.Id).Distinct().Count() == r.Room.Props.Count, "prop ids unique across rooms");
                var five = Start(MatchRules.FiveMinutes, 7);
                Check(five.R.Config.JobsPerSeat == 2 && five.R.Recipes.Instances.Count == 8, $"5-min deals 2 jobs per seat ({five.R.Recipes.Instances.Count})");
            });

            Test("CM-V12-SCATTER 10-minute house: chore items left all over the house (clothes, dishes, toys, books, shopping), same house on every peer, a new house every seed", () =>
            {
                var c = Start(MatchRules.TenMinutes, 7); var r = c.R;
                var sc = r.Room.Props.Where(p => r.IsRecipeProp(p.Id) && p.StartHint != null).ToList();
                Check(r.ScatteredProps >= 6 && sc.Count == r.ScatteredProps, $"{r.ScatteredProps} chore items start away from their job");
                var rooms = sc.Select(p => p.StartHint.Substring(p.StartHint.LastIndexOf(' ') + 1)).Distinct().ToList();
                Check(sc.Select(p => p.StartHint.Contains("living") ? "living" : p.StartHint.Contains("kitchen") ? "kitchen" : p.StartHint.Contains("laundry") ? "laundry" : "?").Distinct().Count() >= 2, "spread over at least two rooms: " + string.Join(" | ", sc.Take(8).Select(p => p.Kind + " " + p.StartHint)));
                Check(sc.Any(p => p.StartHint.Contains("floor")) && sc.Any(p => !p.StartHint.Contains("floor")), "some on the floor, some on furniture");
                Check(sc.All(p => r.Room.Furniture.All(f => !(f.Kind.StartsWith("counter") || f.Kind == "stove" || f.Kind == "fridge") || p.Spawn.FlatDistance(f.Position) > 0.2f)), "never inside a counter / the stove / the fridge");
                var c2 = Start(MatchRules.TenMinutes, 7); var sc2 = c2.R.Room.Props.Where(p => c2.R.IsRecipeProp(p.Id) && p.StartHint != null).ToList();
                Check(sc2.Count == sc.Count && sc.Zip(sc2, (a, b) => a.Id == b.Id && a.Spawn.FlatDistance(b.Spawn) < 1e-4f && a.StartHint == b.StartHint).All(x => x), "same seed: the same house on every peer");
                var c3 = Start(MatchRules.TenMinutes, 8); var sc3 = c3.R.Room.Props.Where(p => c3.R.IsRecipeProp(p.Id) && p.StartHint != null).ToList();
                Check(sc3.Count != sc.Count || sc.Zip(sc3, (a, b) => a.Spawn.FlatDistance(b.Spawn) > 0.05f).Any(x => x), "another seed: things are left elsewhere");
            });

            Test("CM-V12-BOWLS 30 houses: the pet bowls are never under a job's table or under a chore item (kibble must reach the bowl)", () =>
            {
                int bad = 0; var why = new List<string>();
                for (ulong seed = 1; seed <= 30; seed++)
                {
                    var c = Start(MatchRules.FiveMinutes, seed); var r = c.R;
                    foreach (var bowl in new[] { r.Room.CatBowl, r.Room.DogBowl })
                    {
                        foreach (var st in r.RecipeStations.Values)
                        {
                            float hx = (st.AlongZ ? st.Depth : st.Length) * 0.5f, hz = (st.AlongZ ? st.Length : st.Depth) * 0.5f;
                            if (Math.Abs(bowl.x - st.Center.x) < hx + 0.1f && Math.Abs(bowl.z - st.Center.z) < hz + 0.1f) { bad++; why.Add($"seed {seed}: bowl {bowl} under station {st.Center}"); }
                        }
                        foreach (var q in r.Room.Props) if (q.Recipe && bowl.FlatDistance(q.Spawn) < 0.3f) { bad++; why.Add($"seed {seed}: {q.Kind} over the bowl"); }
                    }
                }
                Check(bad == 0, bad == 0 ? "30 houses: every bowl clear" : string.Join("; ", why.Take(5)));
            });

            Test("CM-03 Mom's visit does not stop the match clock; nobody is eliminated; play resumes with time running", () =>
            {
                var c = Start(MatchRules.FiveMinutes, 11);
                var r = c.R; float before = r.PlayTime;
                Crash(c, 1);
                float atWake = r.PlayTime;
                ToEvidence(c); PassAll(c);
                RunUntil(c, () => r.Phase == RoundPhase.Playing, 90f);
                Check(r.Phase == RoundPhase.Playing, "play resumed: " + r.Phase);
                Check(r.PlayTime - atWake > 8f, $"clock kept running during the visit ({r.PlayTime - atWake:0.0}s)");
                Check(c.Host.Roster.ActiveCount == 4 && c.Host.Summary.EliminationOrder.Count == 0, "nobody eliminated");
                Check(c.Host.Phase == MatchPhase.InRound && c.Host.RoundIndex == 0, "same match, same round");
            });

            Test("CM-04 proven crash -> penalty + short timeout: the culprit cannot grab, others can; recovers after the timeout", () =>
            {
                var c = Start(MatchRules.FiveMinutes, 12);
                var r = c.R;
                var inc = Crash(c, 2);
                r.ReportWitness(inc.Id, 0, 0.9f);
                ToEvidence(c);
                var mem = r.Belief.MemoriesOf(0).First(m => m.Incident == inc.Id);
                Check(r.PresentCard(0, mem.Id), "card shown");
                PassAll(c);
                RunUntil(c, () => r.Phase == RoundPhase.Playing, 90f);
                Check(r.Penalties.Count == 1 && r.Penalties[0].Slot == 2, "one penalty on seat 2: " + string.Join(";", r.Penalties.Select(p => p.Slot)));
                Check(r.Scores[2].Penalty >= MatchRules.PenaltyPoints - 0.01f, "points deducted");
                Check(c.Host.Roster[2].IsActive, "still in the match (no elimination)");
                Check(r.IsTimedOut(2) || r.PlayTime - r.Penalties[0].Time >= MatchRules.TimeoutSeconds, "timeout running");
                int prop = r.Room.Props.First(p => r.IsRecipeProp(p.Id) && r.HolderOf(p.Id) < 0 && !r.Recipes.World.Prop(r.RecipePropOf[p.Id]).IsPlaced).Id;
                if (r.IsTimedOut(2)) Check(!r.Grab(2, prop), "timed-out seat cannot grab");
                RunUntil(c, () => !r.IsTimedOut(2), 30f);
                Check(r.Grab(2, prop), "after the timeout the seat plays again");
                r.Release(2, prop, 0.1f, false);
                c.Flush();
                Check(c.Rep.Round != null && c.Rep.Round.Penalties.Count == 1 && Math.Abs(c.Rep.Round.Scores[2].Total - r.Scores[2].Total) < 0.01f, "replica agrees on the penalty and score");
            });

            Test("CM-05 only the first confrontation is full; a later critical event gets a quick challenge", () =>
            {
                var c = Start(MatchRules.TenMinutes, 13);
                var r = c.R;
                Crash(c, 1); ToEvidence(c);
                Check(!r.Interro.Quick && Math.Abs(r.Interro.Evidence - MatchRules.FullEvidence) < 0.01f, "first is full");
                PassAll(c); RunUntil(c, () => r.Phase == RoundPhase.Playing, 90f);
                RunUntil(c, () => r.Meter.Value < 60f, 60f);
                Crash(c, 3); ToEvidence(c);
                Check(r.Interro.Quick && Math.Abs(r.Interro.Evidence - MatchRules.QuickEvidence) < 0.01f, "second is quick");
                Check(r.FullConfrontations == 1 && r.Confrontations == 2, $"full {r.FullConfrontations} / all {r.Confrontations}");
            });

            Test("CM-06 0:00 during a Mom visit ends the match after it (no second inspection, no new round), one award", () =>
            {
                var c = Start(MatchRules.FiveMinutes, 14);
                var r = c.R; int finished = 0; c.Host.MatchFinished += _ => finished++;
                // run near the end, then crash so the visit overlaps 0:00
                RunUntil(c, () => r.TimeRemaining < 6f || r.Phase != RoundPhase.Playing, 400f);
                if (r.Phase == RoundPhase.Playing) Crash(c, 0);
                RunUntil(c, () => c.Host.Phase == MatchPhase.Winner, 200f);
                Check(c.Host.Phase == MatchPhase.Winner && finished == 1, $"phase {c.Host.Phase}, finished x{finished}");
                Check(c.Host.RoundIndex == 0, "no second round");
                var p = new PlayerProgress(); var lines = ProgressionRules.Award(p, c.Host.Summary, 0, false);
                Check(p.MatchesPlayed == 1 && lines.Count > 0, "one award");
                c.Flush();
                Check(c.Rep.Phase == MatchPhase.Winner && c.Rep.Summary.WinnerSlot == c.Host.Summary.WinnerSlot, "replica: same winner");
            });

            Test("CM-07 replica follows a whole 5-minute online match (2 humans + 2 bots, headless bodies): same result", () =>
            {
                var host = new MatchController();
                host.Roster.SeatHuman(901, "A"); host.Roster.SetReady(901, true); host.Roster.SeatHuman(902, "B"); host.Roster.SetReady(902, true);
                host.SetMatchLength(MatchRules.FiveMinutes);
                host.Journal = new MatchJournal(); var rep = new MatchController(); byte[] carry = null;
                var world = new HeadlessWorld(5); world.HumanPolicy = _ => BotProfile.For(BotDifficulty.Normal);
                host.RoundStarting += rr => world.Attach(rr);
                Check(host.Start(MatchMode.PrivateOnline, 5, out var err), err);
                for (float t = 0; t < 900f && host.Phase != MatchPhase.Winner; t += 0.1f)
                {
                    host.Tick(0.1f); if (host.Phase == MatchPhase.InRound) world.Step(0.1f);
                    var b = host.Journal.TakePending(); if (b != null) MatchJournal.Apply(rep, b, ref carry);
                }
                Check(host.Phase == MatchPhase.Winner && rep.Phase == MatchPhase.Winner, $"host {host.Phase} replica {rep.Phase}");
                Check(rep.Summary.WinnerSlot == host.Summary.WinnerSlot && rep.Summary.Ranking.SequenceEqual(host.Summary.Ranking), "same ranking");
                for (int s = 0; s < 4; s++) Check(Math.Abs(rep.Summary.FinalScore[s] - host.Summary.FinalScore[s]) < 0.01f, $"seat {s} score");
            });

            Test("CM-08 host chooses 5 or 10 minutes before start only; the choice is journaled", () =>
            {
                var m = new MatchController();
                m.SetMatchLength(MatchRules.TenMinutes); Check(Math.Abs(m.MatchLength - 600f) < 0.01f, "10");
                m.SetMatchLength(MatchRules.FiveMinutes); Check(Math.Abs(m.MatchLength - 300f) < 0.01f, "5");
                var c = Start(MatchRules.TenMinutes, 21);
                c.Host.SetMatchLength(MatchRules.FiveMinutes);
                Check(Math.Abs(c.Host.MatchLength - 600f) < 0.01f, "cannot change mid-match");
                c.Flush();
                Check(c.Rep.Continuous && Math.Abs(c.Rep.MatchLength - 600f) < 0.01f && c.Rep.Rounds[0].JobsPerSeat == 3, "replica got the continuous 10-min config");
            });
        }
        finally { MatchController.ContinuousDefault = prev; }
    }
}
