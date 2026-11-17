using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Core;

/// <summary>
/// Gate 5 scripted confrontation scenarios (spec §06 "sample event ledger and verdict trace for at least 12 scripted scenarios").
/// Every scenario runs a real recipe round (4 seats, journaled host + replica), injects physical facts through the round API
/// (grab / release / impact / recipe ops), host perception through ReportWitness, and player input through cards / pointing / pass.
/// The verdict must follow Mom's belief ledger only; WorldTruth is printed for QA. Traces are written to QA_Evidence.
/// </summary>
static class BeliefTests
{
    sealed class Ctx { public MatchController Host, Rep; public RoundController R; public Action Flush; public List<string> Trace = new List<string>(); }

    static Ctx Start(RoomId room, ulong seed)
    {
        var host = new MatchController();
        for (int h = 0; h < 4; h++) { host.Roster.SeatHuman(800 + h, new[] { "Pip", "Nell", "Rory", "Tess" }[h]); host.Roster.SetReady(800 + h, true); }
        host.Journal = new MatchJournal();
        var rounds = RoundConfig.DefaultSequence(); foreach (var c in rounds) c.RecipeMode = true;
        host.Rounds = rounds.Where(c => c.Room == room).ToArray(); host.Rounds[0].Index = (int)room;
        var rep = new MatchController(); byte[] carry = null;
        var ctx = new Ctx { Host = host, Rep = rep };
        ctx.Flush = () => { var b = host.Journal.TakePending(); if (b != null) MatchJournal.Apply(rep, b, ref carry); };
        if (!host.Start(MatchMode.PrivateOnline, seed, out var err)) throw new Exception(err);
        host.SkipIntro(); host.Tick(0.01f); for (int i = 0; i < 40; i++) host.Tick(0.1f);
        ctx.R = host.Round; ctx.R.Log.Sink = l => ctx.Trace.Add(l.ToString());
        return ctx;
    }

    static void Run(Ctx c, float seconds) { for (float t = 0; t < seconds; t += 0.1f) c.Host.Tick(0.1f); }
    static void RunUntil(Ctx c, Func<bool> cond, float max = 60f) { for (float t = 0; t < max && !cond(); t += 0.1f) c.Host.Tick(0.1f); }

    /// <summary>A loud, decisive crash by <paramref name="actor"/> with one of its own recipe items (wakes Mom).</summary>
    static Incident Crash(Ctx c, int actor)
    {
        var r = c.R;
        // the loudest loose recipe item in the room (a crash needs something that clacks)
        var loud = new[] { NoiseMaterial.Metal, NoiseMaterial.Glass, NoiseMaterial.Ceramic, NoiseMaterial.Plastic, NoiseMaterial.Wood, NoiseMaterial.Toy };
        int prop = r.Room.Props.Where(p => r.IsRecipeProp(p.Id) && !r.Recipes.World.Prop(r.RecipePropOf[p.Id]).IsPlaced).OrderBy(p => { int k = Array.IndexOf(loud, p.Material); return k < 0 ? 99 : k; }).First().Id;
        // real, journaled crashes (no direct meter writes, so the replica must agree)
        for (int k = 0; k < 40 && r.Phase == RoundPhase.Playing; k++)
        {
            if (!r.Grab(actor, prop)) throw new Exception("grab for crash failed");
            r.Release(actor, prop, 3f, true);
            r.SubmitImpact(prop, 9f, r.Room.Props.First(p => p.Id == prop).Spawn);
            c.Host.Tick(0.1f); c.Host.Tick(0.1f);
        }
        if (r.Phase != RoundPhase.MomSequence) throw new Exception("crash did not wake Mom: " + string.Join(" | ", c.Trace.Skip(Math.Max(0, c.Trace.Count - 3))));
        return r.Belief.Incidents.LastOrDefault(i => i.Kind == IncidentKind.Noise);
    }

    static void ToEvidence(Ctx c) => RunUntil(c, () => c.R.Interro != null && c.R.Mom.State == MomState.Interrogating && c.R.Interro.EvidenceOpen, 40f);
    static void ToVerdict(Ctx c) { foreach (int s in c.R.Roster.ActiveIndices()) c.R.PassInterrogation(s); RunUntil(c, () => c.R.Mom.State >= MomState.Verdict || c.R.Phase != RoundPhase.MomSequence, 60f); }

    /// <summary>A rival's sabotage of <paramref name="victim"/>'s completed recipe (through the round API).</summary>
    static Incident Sabotage(Ctx c, int victim, int saboteur)
    {
        var r = c.R; var inst = r.Recipes.Instances.First(i => i.Owner == victim);
        var exec = RecipeModeTests.Exec(c.Host);
        var sol = RecipeSolver.Solve(r.Recipes, inst, victim, 200, exec);
        if (!sol.Complete) throw new Exception("could not complete victim recipe: " + sol.Failure);
        Run(c, 1f);
        foreach (var v in inst.Def.Sabotage) { var err = RecipeSolver.ApplySabotage(r.Recipes, inst, v, saboteur, exec); if (err == null && !inst.Complete) break; }
        c.Host.Tick(0.1f);
        return r.Belief.Incidents.LastOrDefault(i => i.Kind == IncidentKind.Sabotage);
    }

    static string Dump(Ctx c, string id)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "QA_Evidence", "belief_v7");
        try { Directory.CreateDirectory(dir); File.WriteAllLines(Path.Combine(dir, id + ".log"), c.Trace); } catch { }
        return string.Join(" / ", c.Trace.Where(l => l.Contains("VERDICT") || l.Contains("CARD") || l.Contains("POINT")).Select(l => l.Length > 240 ? l.Substring(0, 240) : l));
    }

    static void Replicated(Ctx c, Action<bool, string> Check)
    {
        Run(c, 0.5f); c.Flush();
        Check(c.Rep.Round != null && c.Rep.Round.Log.Lines.SequenceEqual(c.Host.Round.Log.Lines), "replica log identical");
        Check(MatchJournal.Checksum(c.Rep) == MatchJournal.Checksum(c.Host), "checksum equal");
    }

    public static void Run(Action<string, Action> Test, Action<bool, string> Check)
    {
        Test("BEL-01 crash seen by one sibling who shows the card -> dropper caught (no omniscience: proof comes from the card)", () =>
        {
            var c = Start(RoomId.LivingRoom, 101); var r = c.R;
            var inc = Crash(c, 2);
            Check(inc != null && r.Phase == RoundPhase.MomSequence && r.Verdict.Kind == MomVerdictKind.None, "Mom woke, verdict pending (does not know who)");
            r.ReportWitness(inc.Id, 1, 0.9f);
            ToEvidence(c);
            var mem = r.Belief.MemoriesOf(1).First();
            Check(r.PresentCard(1, mem.Id), "Nell shows her memory");
            ToVerdict(c);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 2 && r.Verdict.Reason == EliminationReason.CaughtMakingNoise, $"Rory caught: {r.Verdict.Kind} {r.Verdict.Culprit} :: {Dump(c, "BEL-01")}");
            Replicated(c, Check);
        });
        Test("BEL-02 crash nobody saw, nobody accuses -> not proven -> fair fallback (chore contribution inspection)", () =>
        {
            var c = Start(RoomId.LivingRoom, 102); var r = c.R;
            Crash(c, 2); ToEvidence(c); ToVerdict(c);
            Check(r.LastResolution != null && !r.LastResolution.Proven, "not proven");
            Check(r.Verdict.Reason != EliminationReason.CaughtMakingNoise, $"no one is 'caught' without evidence: {r.Verdict.Reason} :: {Dump(c, "BEL-02")}");
        });
        Test("BEL-03 two siblings point at an innocent without a memory -> capped, not proven", () =>
        {
            var c = Start(RoomId.LivingRoom, 103); var r = c.R;
            Crash(c, 2); ToEvidence(c);
            Check(r.Accuse(0, 3) && r.Accuse(1, 3), "two pointings accepted");
            Check(!r.Accuse(0, 1), "one pointing per seat");
            ToVerdict(c);
            Check(!r.LastResolution.Proven && r.LastResolution.Suspicion[3] <= VerdictCalculator.AccuseCap + 0.001f, $"Tess not convicted by pointing alone :: {Dump(c, "BEL-03")}");
        });
        Test("BEL-04 nobody can present a memory they do not have (hallucinated / someone else's card)", () =>
        {
            var c = Start(RoomId.LivingRoom, 104); var r = c.R;
            var inc = Crash(c, 2); r.ReportWitness(inc.Id, 1, 0.9f); ToEvidence(c);
            var nells = r.Belief.MemoriesOf(1).First();
            Check(!r.PresentCard(0, nells.Id), "Pip cannot show Nell's memory");
            Check(!r.PresentCard(0, 9999), "no invented memory ids");
            Check(r.Belief.MemoriesOf(0).Count() == 0, "Pip has no memory of it");
            r.ReportWitness(inc.Id, 2, 1f); // the actor cannot 'witness' their own act
            Check(!r.Belief.MemoriesOf(2).Any(), "no self-witness memory");
            ToVerdict(c);
            Check(!r.LastResolution.Proven, $"not proven :: {Dump(c, "BEL-04")}");
        });
        Test("BEL-05 true card beats pointing at someone else", () =>
        {
            var c = Start(RoomId.LivingRoom, 105); var r = c.R;
            var inc = Crash(c, 2); r.ReportWitness(inc.Id, 0, 0.85f); ToEvidence(c);
            r.Accuse(2, 3); r.Accuse(1, 3);                                   // the culprit and another point at Tess
            r.PresentCard(0, r.Belief.MemoriesOf(0).First().Id);             // Pip saw Rory
            ToVerdict(c);
            Check(r.Verdict.Culprit == 2 && r.LastResolution.Proven, $"Rory caught despite pointing at Tess :: {Dump(c, "BEL-05")}");
        });
        Test("BEL-06 pet crash wakes Mom: warning only, no confrontation, round resumes", () =>
        {
            var c = Start(RoomId.LivingRoom, 106); var r = c.R;
            r.Meter.Value = 70f;
            r.SubmitPetSound(ActorIds.Dog, 30f, new Vec3(0, 0, 0));
            RunUntil(c, () => r.Phase == RoundPhase.MomSequence, 3f);
            Check(r.Interro == null && (r.Verdict == null || r.Verdict.Kind == MomVerdictKind.Warning), $"warning, no confrontation ({r.Verdict?.Kind})");
            RunUntil(c, () => r.Phase == RoundPhase.Playing, 40f);
            Check(r.Phase == RoundPhase.Playing && r.Roster.ActiveCount == 4, "round resumed, nobody eliminated");
        });
        Test("BEL-07 unrepaired sabotage nobody saw -> inspection asks about it, no proof -> fallback", () =>
        {
            var c = Start(RoomId.LivingRoom, 107); var r = c.R;
            var inc = Sabotage(c, 0, 3);
            Check(inc != null && inc.TruthActor == 3, "sabotage incident recorded (truth for QA)");
            RunUntil(c, () => r.Phase == RoundPhase.MomSequence, 220f);
            ToEvidence(c);
            Check(r.Interro != null && r.Interro.Incident == inc && r.Interro.IsInspection, "Mom asks about Pip's messed-up job");
            ToVerdict(c);
            Check(!r.LastResolution.Proven && r.Verdict.Reason != EliminationReason.CaughtSabotaging, $"no proof, no 'caught sabotaging' :: {Dump(c, "BEL-07")}");
        });
        Test("BEL-08 unrepaired sabotage seen by a witness who shows it -> saboteur caught", () =>
        {
            var c = Start(RoomId.LivingRoom, 108); var r = c.R;
            var inc = Sabotage(c, 0, 3);
            r.ReportWitness(inc.Id, 1, 0.95f);
            RunUntil(c, () => r.Phase == RoundPhase.MomSequence, 220f); ToEvidence(c);
            r.PresentCard(1, r.Belief.MemoriesOf(1).First().Id);
            ToVerdict(c);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 3 && r.Verdict.Reason == EliminationReason.CaughtSabotaging, $"Tess caught sabotaging :: {Dump(c, "BEL-08")}");
            Replicated(c, Check);
        });
        Test("BEL-09 sabotage repaired in time (foiled) -> nothing to ask about at inspection", () =>
        {
            var c = Start(RoomId.LivingRoom, 109); var r = c.R;
            var inst = r.Recipes.Instances.First(i => i.Owner == 0);
            Sabotage(c, 0, 3);
            RecipeSolver.Solve(r.Recipes, inst, 0, 200, RecipeModeTests.Exec(c.Host));
            Check(inst.Complete && r.Recipes.Sabotage.Any(s => s.Foiled), "owner repaired, foiled");
            RunUntil(c, () => r.Phase == RoundPhase.MomSequence, 220f); ToEvidence(c);
            Check(r.Interro != null && r.Interro.Incident == null, "general inspection, no incident");
            ToVerdict(c);
            Check(!r.LastResolution.Proven, $"contributions decide :: {Dump(c, "BEL-09")}");
        });
        Test("BEL-10 the only witness stays silent (passes) -> not proven", () =>
        {
            var c = Start(RoomId.LivingRoom, 110); var r = c.R;
            var inc = Crash(c, 2); r.ReportWitness(inc.Id, 1, 0.9f); ToEvidence(c);
            ToVerdict(c);
            Check(!r.LastResolution.Proven, $"memory unused -> no proof :: {Dump(c, "BEL-10")}");
        });
        Test("BEL-11 a memory of a DIFFERENT incident is only a pattern (not proof on its own)", () =>
        {
            var c = Start(RoomId.LivingRoom, 111); var r = c.R;
            var sab = Sabotage(c, 0, 2); r.ReportWitness(sab.Id, 3, 1f);  // Tess saw Rory sabotage Pip earlier
            Run(c, 1f);
            Crash(c, 2); ToEvidence(c);                                     // nobody saw the crash
            r.PresentCard(3, r.Belief.MemoriesOf(3).First().Id);
            ToVerdict(c);
            Check(!r.LastResolution.Proven && Math.Abs(r.LastResolution.Suspicion[2] - VerdictCalculator.WPattern) < 0.2f, $"pattern weight only :: {Dump(c, "BEL-11")}");
        });
        Test("BEL-12 the place is only speculation: a crash at someone's table is not proof", () =>
        {
            var c = Start(RoomId.LivingRoom, 112); var r = c.R;
            Crash(c, 1); ToEvidence(c); ToVerdict(c);
            Check(!r.LastResolution.Proven && r.LastResolution.Suspicion.Values.Max() <= VerdictCalculator.WNearby + 0.001f, $"speculation capped :: {Dump(c, "BEL-12")}");
        });
        Test("BEL-13 confrontation times out if nobody responds (no indefinite wait)", () =>
        {
            var c = Start(RoomId.LivingRoom, 113); var r = c.R;
            Crash(c, 2);
            RunUntil(c, () => r.Mom.State >= MomState.Verdict, 80f);
            Check(r.Mom.State >= MomState.Verdict && r.Verdict.Kind != MomVerdictKind.None, $"verdict reached after the evidence window ({r.Verdict.Kind})");
        });
        Test("BEL-14 a sibling sent to bed cannot present memories or point", () =>
        {
            var c = Start(RoomId.LivingRoom, 114); var r = c.R;
            var inc = Crash(c, 2); r.ReportWitness(inc.Id, 3, 0.9f);
            c.Host.Roster.Eliminate(3, EliminationReason.None, -1);
            ToEvidence(c);
            Check(!r.PresentCard(3, r.Belief.MemoriesOf(3).First().Id) && !r.Accuse(3, 2), "spectator input refused");
        });
        Test("BEL-15 bots present their true memories in the confrontation (headless bodies, perception by distance)", () =>
        {
            RoundConfig.RecipeModeDefault = true;
            try
            {
                int confrontations = 0, proven = 0, correct = 0, cards = 0;
                for (ulong seed = 1; seed <= 60; seed++)
                {
                    var m = MatchSimulator.RunSolo(seed, (BotDifficulty)(seed % 3), BotProfile.For(BotDifficulty.Normal));
                    confrontations += m.Log.Lines.Count(l => l.Contains("CONFRONTATION"));
                    cards += m.Log.Lines.Count(l => l.Contains("CARD "));
                    // every proven verdict must rest on a presented card that truly showed the culprit doing something this round
                    var lines = m.Log.Lines.ToList(); int start = 0;
                    for (int li = 0; li < lines.Count; li++)
                    {
                        if (lines[li].Contains("CONFRONTATION")) start = li;
                        if (!(lines[li].Contains("VERDICT Eliminate") && lines[li].Contains("PROVEN"))) continue;
                        proven++; var who = lines[li].Substring(lines[li].IndexOf("VERDICT Eliminate ", StringComparison.Ordinal) + 18).Split(' ')[0];
                        bool backed = lines.Skip(start).Take(li - start).Any(x => x.Contains("CARD ") && x.Contains("Saw " + who + " "));
                        if (backed) correct++; else Console.WriteLine("       unbacked: " + lines[li]);
                    }
                }
                Console.WriteLine($"     60 recipe matches: {confrontations} confrontations, {cards} cards shown, {proven} proven verdicts, {correct} backed by a true card");
                Check(confrontations >= 60 * 3, "a confrontation every round");
                Check(proven == correct, "every proven verdict is backed by a truthful card naming the culprit");
            }
            finally { RoundConfig.RecipeModeDefault = false; }
        });
    }
}
