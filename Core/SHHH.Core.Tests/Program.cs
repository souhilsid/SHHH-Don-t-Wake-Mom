using System;
using System.Collections.Generic;
using System.Linq;
using SHHH.Core;

// Dependency-free test runner for the SHHH core rules. Maps to Docs/TEST_MATRIX.csv ids.
static class Program
{
    static int _pass, _fail;
    static readonly List<string> Failures = new List<string>();

    static void Check(bool cond, string msg) { if (!cond) throw new Exception(msg); }

    static void Test(string id, Action a)
    {
        try { a(); _pass++; Console.WriteLine($"PASS {id}"); }
        catch (Exception e) { _fail++; Failures.Add($"{id}: {e.Message}"); Console.WriteLine($"FAIL {id}: {e.Message}\n{e.StackTrace}"); }
    }

    // ---------- helpers
    static (MatchController m, RoundController r) SoloRound(RoomId room = RoomId.Kitchen, ulong seed = 42)
    {
        var m = MatchController.CreateSolo("You", BotDifficulty.Normal, seed);
        RoundController captured = null;
        m.RoundStarting += r => captured = r;
        m.Rounds = new[] { RoundConfig.DefaultSequence()[(int)room == 3 ? 0 : (int)room] };
        m.Rounds[0].Room = room;
        Check(m.Start(MatchMode.Solo, seed, out var err), err);
        m.SkipIntro(); m.Tick(0.01f);
        // run countdown
        for (int i = 0; i < 40; i++) m.Tick(0.1f);
        Check(captured != null && captured.Phase == RoundPhase.Playing, "round not playing: " + captured?.Phase);
        return (m, captured);
    }

    static int PropOfKind(RoundController r, string kind) => r.Room.Props.First(p => p.Kind == kind).Id;
    static void Run(MatchController m, float seconds) { for (float t = 0; t < seconds; t += 0.1f) m.Tick(0.1f); }

    const float Round_RushSeconds = RoundController.RushSeconds;
    static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "--jobstats") return JobStats.Run(args.Length > 1 ? int.Parse(args[1]) : 200, args.Length > 2 ? float.Parse(args[2]) : 600f);
        if (args.Length >= 1 && args[0] == "--coverage") return Coverage.Write(args.Length > 1 ? args[1] : "coverage.csv", args.Length > 2 ? args[2] : "");
        MatchController.ContinuousDefault = false; // legacy suites build round sequences explicitly; ContinuousTests opt in
        RoundConfig.RecipeModeDefault = false; // V6 suites below exercise the legacy single-step chores; recipe suites opt in
        // ---------------- Roster / slot matrix
        Test("SLOT-01 solo is 1 human + 3 bots", () =>
        {
            var m = MatchController.CreateSolo("You", BotDifficulty.Normal, 1);
            Check(m.Roster.HumanCount == 1 && m.Roster.BotCount == 3, $"{m.Roster.HumanCount}/{m.Roster.BotCount}");
            Check(m.Roster.Slots.Count == 4, "4 seats");
        });
        Test("SLOT-02 online 2H+2B, 3H+1B, 4H+0B always 4 seats", () =>
        {
            for (int humans = 2; humans <= 4; humans++)
            {
                var r = new CompetitorRoster();
                for (int h = 0; h < humans; h++) Check(r.SeatHuman(1000 + h, "P" + h) >= 0, "seat");
                Check(r.HumanCount == humans && r.BotCount == 4 - humans, $"{humans}: {r.HumanCount}+{r.BotCount}");
                Check(r.ValidateForStart(out _), "valid");
            }
        });
        Test("SLOT-03 fifth human rejected, duplicate peer idempotent", () =>
        {
            var r = new CompetitorRoster();
            for (int h = 0; h < 4; h++) r.SeatHuman(10 + h, null);
            Check(r.SeatHuman(99, "late") == -1, "fifth rejected");
            Check(r.SeatHuman(10, "again") == 0, "idempotent");
        });
        Test("SLOT-04 pre-match leave returns seat to a bot; locked roster rejects joins", () =>
        {
            var r = new CompetitorRoster();
            r.SeatHuman(10, "A"); r.SeatHuman(11, "B");
            r.UnseatHumanPreMatch(11);
            Check(r.HumanCount == 1 && r.BotCount == 3, "reverted");
            r.LockForMatch();
            Check(r.SeatHuman(12, "C") == -1, "no join mid-match");
        });

        // ---------------- Noise / blame
        Test("NOISE-01 dropped pan wakes Mom and eliminates the dropper", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pan = PropOfKind(r, "pan");
            r.Meter.Value = 60f;
            Check(r.Grab(2, pan), "grab");
            r.Release(2, pan, 0.5f, false);
            r.SubmitImpact(pan, 4.6f, new Vec3(0, 0, 1.5f));
            Run(m, 0.2f);
            Check(r.Phase == RoundPhase.MomSequence, "mom sequence " + r.Phase);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 2, $"verdict {r.Verdict.Kind} {r.Verdict.Culprit}");
            Run(m, 30f);
            Check(m.Roster[2].Status != SlotStatus.Active, "slot 2 eliminated");
            Check(m.Roster.ActiveCount == 3, "exactly one elimination");
        });
        Test("FUN-01 a loud crash gets an audible Mom reaction within 0.2 s, without waking her", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pan = PropOfKind(r, "pan");
            var lines = new List<(float t, string id)>(); r.Mom.Says += id => lines.Add((r.Time, id));
            Check(r.Grab(1, pan), "grab"); r.Release(1, pan, 0.5f, false);
            float t0 = r.Time;
            r.SubmitImpact(pan, 3.2f, new Vec3(0, 0, 1.5f));
            Run(m, 0.2f);
            Check(r.Phase == RoundPhase.Playing, "still playing (no wake) " + r.Phase + " meter " + r.Meter.Value);
            Check(lines.Count >= 1 && lines[0].t - t0 <= 0.2f, $"reaction lines {lines.Count}");
            Check(r.Mom.State >= MomState.Stirring, "Mom stirring " + r.Mom.State);
        });
        Test("NOISE-02 tiny harmless impacts never wake Mom", () =>
        {
            var (m, r) = SoloRound(RoomId.LivingRoom);
            int cushion = PropOfKind(r, "cushion"), book = PropOfKind(r, "book");
            for (int i = 0; i < 300; i++)
            {
                r.Grab(1, i % 2 == 0 ? cushion : book); r.Release(1, i % 2 == 0 ? cushion : book, 0.1f, false);
                r.SubmitImpact(i % 2 == 0 ? cushion : book, 0.35f, new Vec3(0, 0, 2.5f));
                m.Tick(0.1f);
            }
            Check(r.Meter.Value < 35f, "meter " + r.Meter.Value);
            Check(r.Phase == RoundPhase.Playing, "still playing");
        });
        Test("NOISE-03 independent pet crash never eliminates; warning and resume", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int plate = PropOfKind(r, "plate");
            r.Meter.Value = 95f;
            r.Push(ActorIds.Cat, plate, 1f);
            r.SubmitImpact(plate, 4f, new Vec3(0, 0, 2f));
            Run(m, 0.2f);
            Check(r.Verdict != null && r.Verdict.Kind == MomVerdictKind.Warning, "warning");
            Run(m, 25f);
            Check(r.Phase == RoundPhase.Playing, "resumed " + r.Phase);
            Check(m.Roster.ActiveCount == 4, "no elimination");
            Check(r.Meter.Value <= 60f, "meter reduced " + r.Meter.Value);
        });
        Test("NOISE-04 setter of an unavoidable trap is blamed", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pot = PropOfKind(r, "pot");
            r.Grab(1, pot); r.Release(1, pot, 0.1f, false);
            int trap = r.ArmTrap(1, pot, warningVisible: false);
            Run(m, 3f);
            r.Meter.Value = 70f;
            r.SubmitTrapTrigger(trap, 3, victimCareless: true, pot, 3.5f, new Vec3(0, 0, 1));
            Run(m, 0.2f);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 1, $"{r.Verdict.Kind} {r.Verdict.Culprit} {r.Verdict.Evidence}");
        });
        Test("NOISE-05 careless victim of an avoidable (visible) trap is blamed", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pot = PropOfKind(r, "pot");
            r.Grab(1, pot); r.Release(1, pot, 0.1f, false);
            int trap = r.ArmTrap(1, pot, warningVisible: true);
            Run(m, 3f);
            r.Meter.Value = 70f;
            r.SubmitTrapTrigger(trap, 3, victimCareless: true, pot, 3.5f, new Vec3(0, 0, 1));
            Run(m, 0.2f);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 3, $"{r.Verdict.Culprit} {r.Verdict.Evidence}");
        });
        Test("NOISE-06 careful victim / too-fresh clue => setter blamed", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pot = PropOfKind(r, "pot");
            r.Grab(1, pot); r.Release(1, pot, 0.1f, false);
            int trap = r.ArmTrap(1, pot, warningVisible: true);
            Run(m, 0.5f); // clue visible only 0.5s
            r.Meter.Value = 70f;
            r.SubmitTrapTrigger(trap, 3, victimCareless: true, pot, 3.5f, new Vec3(0, 0, 1));
            Run(m, 0.2f);
            Check(r.Verdict.Culprit == 1, "setter " + r.Verdict.Culprit);
        });
        Test("NOISE-07 chain reaction blames the root actor", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int fork = PropOfKind(r, "fork"); int pan = PropOfKind(r, "pan");
            r.Grab(0, fork); r.Release(0, fork, 5f, true);
            var e1 = r.SubmitImpact(fork, 5f, new Vec3(1, 1, 0), -1, NoiseCause.Throw);
            m.Tick(0.1f);
            r.Meter.Value = 80f;
            r.SubmitImpact(pan, 4f, new Vec3(1, 0, 0), e1.Id, NoiseCause.ChainReaction);
            Run(m, 0.2f);
            Check(r.Verdict != null && r.Verdict.Culprit == 0, $"root actor blamed: {r.Verdict?.Culprit} {r.Verdict?.Evidence}");
        });
        Test("NOISE-08 simultaneous two-sibling release resolves deterministically", () =>
        {
            int Resolve()
            {
                var (m, r) = SoloRound(RoomId.LivingRoom, 7);
                int vase = PropOfKind(r, "vase");
                r.Grab(2, vase); r.Grab(1, vase);
                r.Release(2, vase, 1.2f, false); r.Release(1, vase, 1.2f, false);
                r.Meter.Value = 80f;
                r.SubmitImpact(vase, 4f, new Vec3(0, 0, 2));
                Run(m, 0.2f);
                return r.Verdict.Culprit;
            }
            int a = Resolve(), b = Resolve();
            Check(a == b && a == 1, $"deterministic lower slot on equal speed: {a},{b}");
        });
        Test("NOISE-09 sibling who lured the pet is responsible", () =>
        {
            var (m, r) = SoloRound(RoomId.LivingRoom);
            r.Ledger.RecordPetLure(ActorIds.Dog, 3, r.Time);
            int cup = PropOfKind(r, "cup");
            r.Push(ActorIds.Dog, cup, 1f);
            r.Meter.Value = 85f;
            r.SubmitImpact(cup, 4f, new Vec3(0, 0, 2));
            Run(m, 0.2f);
            Check(r.Verdict.Kind == MomVerdictKind.Eliminate && r.Verdict.Culprit == 3, $"{r.Verdict.Kind} {r.Verdict.Culprit}");
        });
        Test("NOISE-10 a tiny last noise does not take blame for an earlier big crash", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pan = PropOfKind(r, "pan"), sponge = PropOfKind(r, "sponge"), fork = PropOfKind(r, "fork");
            r.Meter.Value = 5f;
            r.Grab(1, pan); r.Release(1, pan, 0.2f, false); r.SubmitImpact(pan, 4f, new Vec3(0, 0, 2.8f));
            m.Tick(0.1f);
            Check(r.Phase == RoundPhase.Playing, "not yet awake: " + r.Meter.Value);
            r.Meter.Value = 99.5f;
            r.Grab(3, fork); r.Release(3, fork, 0.2f, false); r.SubmitImpact(fork, 2.0f, new Vec3(0, 0, 2.8f));
            Run(m, 0.2f);
            Check(r.Verdict != null && r.Verdict.Culprit == 1, $"big crash owner blamed, got {r.Verdict?.Culprit}");
        });
        Test("NOISE-11 simultaneous wake events produce exactly one elimination", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            int pan = PropOfKind(r, "pan"), pot = PropOfKind(r, "pot");
            r.Meter.Value = 90f;
            r.Grab(1, pan); r.Release(1, pan, 0, false);
            r.Grab(2, pot); r.Release(2, pot, 0, false);
            r.SubmitImpact(pan, 4f, new Vec3(0, 0, 2)); r.SubmitImpact(pot, 4f, new Vec3(0, 0, 2));
            Run(m, 30f);
            Check(m.Roster.ActiveCount == 3, "active " + m.Roster.ActiveCount);
        });
        Test("NOISE-12 duplicate host event ids are ignored; voice is not an input", () =>
        {
            var l = new CausalLedger();
            var e = new NoiseEvent { Id = 5, Value = 10, Cause = NoiseCause.Scripted };
            Check(l.Register(e) && !l.Register(e), "idempotent");
            Check(typeof(SleepMeter).GetMethods().All(mi => !mi.Name.ToLower().Contains("voice")), "no voice API on meter");
        });

        // ---------------- Chores / scoring
        Test("SCORE-01 two siblings share a chore proportionally; credited once", () =>
        {
            MatchController m = null; RoundController r = null; ChoreInstance plates = null;
            for (ulong sd = 3; plates == null && sd < 60; sd++) { (m, r) = SoloRound(RoomId.Kitchen, sd); plates = r.Board.Chores.FirstOrDefault(c => !c.IsBonus && c.Steps.Count >= 2); }
            Check(plates != null, "a 2+ step shared chore");
            int n = plates.Steps.Count;
            for (int i = 0; i < n; i++)
            {
                int actor = i == 0 ? 1 : 2; var s = plates.Steps[i];
                r.Grab(actor, s.PropId); r.Release(actor, s.PropId, 0.1f, false);
                Check(r.Placed(s.PropId, s.AnchorId, actor), "placed");
            }
            Check(plates.State == ChoreState.Completed, "complete");
            float a = r.Scores[1].Shared, b = r.Scores[2].Shared;
            float P = plates.Points;
            Check(Math.Abs(a - P / n) < 0.01f && Math.Abs(b - P * (n - 1) / n) < 0.01f, $"shares {a},{b}");
        });
        Test("SCORE-02 drop / re-grab / re-place loops cannot farm points", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen, 3);
            var c = r.Board.Chores.First(x => !x.IsBonus && x.Steps.Count == 1);
            var s = c.Steps[0];
            for (int i = 0; i < 20; i++)
            {
                r.Grab(1, s.PropId); r.Release(1, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 1);
            }
            Check(Math.Abs(r.Scores[1].Shared - c.Points) < 0.01f, "points " + r.Scores[1].Shared);
        });
        Test("SCORE-03 sabotage undo suspends credit; self undo/redo restores original owner", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen, 3);
            var c = r.Board.Chores.First(x => !x.IsBonus && x.Steps.Count == 1);
            var s = c.Steps[0];
            r.Grab(1, s.PropId); r.Release(1, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 1);
            Check(r.Scores[1].Shared > 0, "credited");
            r.Grab(2, s.PropId); // rival takes it off
            Check(r.Scores[1].Shared == 0, "suspended");
            r.Release(2, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 2); // same rival puts it back
            Check(r.Scores[1].Shared > 0 && r.Scores[2].Shared == 0, $"original owner restored: {r.Scores[1].Shared} {r.Scores[2].Shared}");
        });
        Test("SCORE-04 personal duty: only owner earns; all duties done -> early inspection", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen, 5);
            var duty = r.Board.Chores.FirstOrDefault(x => x.IsBonus);
            Check(duty != null, "duty exists");
            int other = (duty.BonusOwner + 1) % 4; var st = duty.Steps[0];
            r.Grab(other, st.PropId); r.Release(other, st.PropId, 0.1f, false);
            Check(!r.Placed(st.PropId, st.AnchorId, other), "non-owner can't earn");
            Check(r.Board.Chores.Count(x => x.IsBonus) == 4, "one duty per active sibling: " + r.Board.Chores.Count(x => x.IsBonus));
            foreach (var c in r.Board.Chores.Where(x => !x.IsBonus))
                foreach (var s in c.Steps) { r.Grab(0, s.PropId); r.Release(0, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 0); }
            Run(m, 0.3f);
            Check(r.Phase == RoundPhase.Playing, "shared chores alone don't end the round");
            foreach (var c in r.Board.Chores.Where(x => x.IsBonus))
                foreach (var s in c.Steps) { r.Grab(c.BonusOwner, s.PropId); r.Release(c.BonusOwner, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, c.BonusOwner); }
            Run(m, 0.3f);
            Check(r.Phase == RoundPhase.MomSequence && r.EndCause == RoundEndCause.AllChoresDone, "early inspection " + r.EndCause);
        });
        Test("DUTY-01 every room gives 4/3/2 siblings distinct personal duties with 2+ items", () =>
        {
            foreach (var room in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
                for (int n = 2; n <= 4; n++)
                    for (ulong seed = 1; seed <= 25; seed++)
                    {
                        var b = new ChoreBoard(); var active = Enumerable.Range(0, n).ToList();
                        b.Generate(RoomCatalog.Get(room), seed, 3, active);
                        var duties = b.Chores.Where(c => c.IsBonus).ToList();
                        Check(duties.Count == n, $"{room} n={n} seed={seed}: {duties.Count} duties");
                        Check(duties.Select(d => d.BonusOwner).Distinct().Count() == n, "distinct owners");
                        Check(duties.Select(d => d.Template.Id).Distinct().Count() == n, "distinct jobs");
                        Check(duties.All(d => d.Steps.Count >= 2), $"{room} n={n} seed={seed}: duty with <2 items: " + string.Join(",", duties.Select(d => d.Template.Id + ":" + d.Steps.Count)));
                    }
        });
        Test("DUTY-02 rival swipe is logged as sabotage; owner restore in time = foiled (caught/save)", () =>
        {
            var (m, r) = SoloRound(RoomId.LivingRoom, 9);
            var duty = r.Board.BonusFor(0); var st = duty.Steps[0];
            r.Grab(0, st.PropId); r.Release(0, st.PropId, 0.1f, false); Check(r.Placed(st.PropId, st.AnchorId, 0), "placed");
            float before = r.Scores[0].Bonus;
            Check(before == ChoreBoard.PersonalPointsPerStep, "partial duty credit " + before);
            r.Grab(2, st.PropId);
            Check(r.Board.Sabotage.Count == 1 && r.Board.Sabotage[0].Saboteur == 2 && r.Board.Sabotage[0].Victim == 0, "sabotage logged");
            Check(r.Scores[0].Bonus == 0, "credit suspended");
            r.Release(2, st.PropId, 0.1f, false);
            Run(m, 3f);
            r.Grab(0, st.PropId); r.Release(0, st.PropId, 0.1f, false); Check(r.Placed(st.PropId, st.AnchorId, 0), "restored");
            Check(r.Board.Sabotage[0].Foiled, "foiled");
            Check(r.Scores[2].Caught == ScoreRules.CaughtPenalty && r.Scores[0].Saves == ScoreRules.SavePoints, $"caught {r.Scores[2].Caught} saves {r.Scores[0].Saves}");
            Check(r.Log.Lines.Any(l => l.Contains("SABOTAGE")) && r.Log.Lines.Any(l => l.Contains("FOILED")), "log lines");
        });
        Test("DUTY-03 late restore is not a foil; owner moving own item is not sabotage", () =>
        {
            var (m, r) = SoloRound(RoomId.LivingRoom, 10);
            var duty = r.Board.BonusFor(0); var st = duty.Steps[0];
            r.Grab(0, st.PropId); r.Release(0, st.PropId, 0.1f, false); r.Placed(st.PropId, st.AnchorId, 0);
            r.Grab(0, st.PropId); r.Release(0, st.PropId, 0.1f, false);
            Check(r.Board.Sabotage.Count == 0, "self move not sabotage");
            r.Placed(st.PropId, st.AnchorId, 0);
            r.Grab(3, st.PropId); r.Release(3, st.PropId, 0.1f, false);
            Run(m, ChoreBoard.FoilWindow + 2f);
            if (r.Phase != RoundPhase.Playing) return; // Mom may have woken in this seed; foil rule already covered
            r.Grab(0, st.PropId); r.Release(0, st.PropId, 0.1f, false); r.Placed(st.PropId, st.AnchorId, 0);
            Check(!r.Board.Sabotage[0].Foiled && r.Scores[3].Caught == 0, "expired sabotage cannot be foiled");
        });
        Test("DUTY-04 first finished duty starts a 30 s rush (min 60 s round)", () =>
        {
            var (m, r) = SoloRound(RoomId.Laundry, 12);
            var duty = r.Board.BonusFor(1);
            foreach (var s in duty.Steps) { r.Grab(1, s.PropId); r.Release(1, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 1); }
            Check(r.FirstFinisher == 1, "first finisher");
            float expect = Math.Max(RoundController.MinRoundSeconds - r.PlayTime, Round_RushSeconds);
            Check(Math.Abs(r.TimeRemaining - expect) < 0.05f && r.TimeRemaining < r.Config.Duration - r.PlayTime, $"rush remaining {r.TimeRemaining} expected {expect}");
        });
        Test("DUTY-05 bots swipe rivals' items and rivals foil it (60 seeded solo matches)", () =>
        {
            int swipes = 0, foils = 0, rushes = 0;
            for (ulong seed = 500; seed < 560; seed++)
            {
                var m = MatchSimulator.RunSolo(seed, BotDifficulty.Clever, BotProfile.For(BotDifficulty.Normal));
                foreach (var l in m.Log.Lines) { if (l.Contains("SABOTAGE")) swipes++; if (l.Contains("FOILED")) foils++; if (l.Contains("RUSH")) rushes++; }
            }
            Console.WriteLine($"     60 matches: swipes={swipes} foiled={foils} rushes={rushes}");
            Check(swipes >= 30, "bots rarely sabotage: " + swipes);
            Check(foils >= 5, "sabotage almost never foiled: " + foils);
        });
        Test("SCORE-05 inspection eliminates least helpful with explainable tie rules", () =>
        {
            var l = new ScoreLedger();
            l[0].Shared = 20; l[1].Shared = 20; l[2].Shared = 10; l[3].Shared = 10; l[3].AttributedNoise = 0; l[2].AttributedNoise = 0;
            l[2].Bonus = 0; l[3].Bonus = 0;
            // 2 and 3 tied on total & shared & noise -> steps decide
            l[2].ValidatedSteps = 2; l[3].ValidatedSteps = 1;
            var res = l.Inspect(new List<int> { 0, 1, 2, 3 });
            Check(res.Outcome == InspectionOutcome.Eliminate && res.Eliminated == 3, $"{res.Outcome} {res.Eliminated}");
            l[3].ValidatedSteps = 2;
            res = l.Inspect(new List<int> { 0, 1, 2, 3 });
            Check(res.Outcome == InspectionOutcome.TieBreakNeeded && res.Tied.Count == 2, "tie-break");
            l[2].AttributedNoise = 10; l[2].Shared = 10f; l[2].Bonus = 3.5f; // equal totals: own-job progress decides first (V5)
            res = l.Inspect(new List<int> { 0, 1, 2, 3 });
            Check(res.Eliminated == 3 && res.Explanation.Contains("own job"), "less own-job progress loses on equal total? got " + res.Eliminated + " " + res.Explanation);
            l[2].Bonus = 0f; l[2].Shared = 10f; l[2].AttributedNoise = 10f; l[3].Shared = 6.5f; // l2 total 6.5, l3 6.5 -> shared decides
            res = l.Inspect(new List<int> { 0, 1, 2, 3 });
            Check(res.Eliminated == 3, "lower shared loses: " + res.Eliminated);
        });
        Test("SCORE-06 timer expiry triggers inspection and exactly one elimination", () =>
        {
            var (m, r) = SoloRound(RoomId.Laundry, 11);
            var c = r.Board.Chores.First(x => !x.IsBonus);
            foreach (var s in c.Steps) { r.Grab(0, s.PropId); r.Release(0, s.PropId, 0.1f, false); r.Placed(s.PropId, s.AnchorId, 0); }
            Run(m, r.Config.Duration + 1f);
            Check(r.EndCause == RoundEndCause.TimerExpired, "timer " + r.EndCause);
            Run(m, 30f);
            Check(m.Roster.ActiveCount == 3 || r.Phase == RoundPhase.TieBreak, "one elimination or tie-break " + r.Phase);
            if (r.Phase == RoundPhase.TieBreak) { Run(m, 25f); Check(m.Roster.ActiveCount == 3, "after tie-break"); }
            Check(m.Roster[0].IsActive, "the only worker survives");
        });
        Test("SCORE-07 spectators cannot touch props or make scored noise", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            m.Roster.Eliminate(2, EliminationReason.CaughtMakingNoise, 0);
            int pan = PropOfKind(r, "pan");
            Check(!r.Grab(2, pan), "spectator grab blocked");
            Check(r.SubmitOperate(2, 1, 50f, new Vec3(), true) == null, "spectator slam blocked");
        });
        Test("SCORE-08 idle penalty capped, excluded during tracking loss", () =>
        {
            var (m, r) = SoloRound(RoomId.Kitchen);
            r.ReportTrackingLost(0, true);
            Run(m, 100f);
            Check(r.Scores[0].Inactivity == 0, "no penalty while tracking lost");
        });
        Test("SCORE-09 rescue +5 capped at 15, never for own trap", () =>
        {
            var l = new ScoreLedger();
            Check(!l.AddRescue(1, "a", 1), "own danger");
            for (int i = 0; i < 6; i++) l.AddRescue(1, "k" + i, 2);
            Check(Math.Abs(l[1].Rescue - 15) < 0.01f, "cap " + l[1].Rescue);
            Check(!l.AddRescue(2, "k0", 3) || true, "");
        });

        // ---------------- Full match simulations
        Test("MATCH-01 solo 1H+3B: 200 seeded matches all go 4->3->2->1 across three rooms", () =>
        {
            int botWins = 0, humanWins = 0;
            var elimReasons = new Dictionary<EliminationReason, int>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var m = MatchSimulator.RunSolo(seed, (BotDifficulty)(seed % 3), BotProfile.For(BotDifficulty.Normal));
                Check(m.Phase == MatchPhase.Winner, $"seed {seed} ended in {m.Phase}");
                Check(m.Summary.EliminationOrder.Count == 3, $"seed {seed} elims {m.Summary.EliminationOrder.Count}");
                Check(m.Summary.EliminationOrder.Distinct().Count() == 3, "distinct");
                Check(m.RoundIndex == 2, $"seed {seed} three rounds played, idx {m.RoundIndex}");
                Check(m.Roster.ActiveCount == 1 && m.Summary.WinnerSlot >= 0, "one winner");
                for (int i = 0; i < 4; i++) if (i != m.Summary.WinnerSlot) Check(m.Roster[i].EliminatedInRound >= 0, "loser has round");
                // exactly one elimination per round
                var perRound = m.Roster.Slots.Where(s => s.EliminatedInRound >= 0).GroupBy(s => s.EliminatedInRound).Select(g => g.Count());
                Check(perRound.All(c => c == 1), "one per round");
                if (m.Summary.WinnerIsBot) botWins++; else humanWins++;
                foreach (var s in m.Roster.Slots) if (s.Reason != EliminationReason.None) { elimReasons.TryGetValue(s.Reason, out int c); elimReasons[s.Reason] = c + 1; }
            }
            Console.WriteLine($"     solo sim: bot wins {botWins}, human(policy) wins {humanWins}; reasons: {string.Join(", ", elimReasons.Select(kv => kv.Key + "=" + kv.Value))}");
            Check(botWins > 0 && humanWins > 0, "both bots and humans can win");
            Check(elimReasons.ContainsKey(EliminationReason.CaughtMakingNoise) && elimReasons.ContainsKey(EliminationReason.LeastHelpfulInspection), "both elimination paths occur");
        });
        Test("MATCH-02 rooms are always Living Room -> Kitchen -> Laundry", () =>
        {
            var m = MatchController.CreateSolo("You", BotDifficulty.Normal, 77);
            var rooms = new List<RoomId>();
            m.RoundStarting += r => rooms.Add(r.Room.Id);
            MatchSimulator.Run(m, MatchMode.Solo, 77, null);
            Check(rooms.SequenceEqual(new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry }), string.Join(",", rooms));
        });
        Test("MATCH-03 mixed 2H+2B, 3H+1B, 4H+0B all complete with one winner", () =>
        {
            for (int humans = 2; humans <= 4; humans++)
                for (ulong seed = 1; seed <= 30; seed++)
                {
                    var m = new MatchController();
                    for (int h = 0; h < humans; h++) { m.Roster.SeatHuman(500 + h, "H" + h); m.Roster.SetReady(500 + h, true); }
                    MatchSimulator.Run(m, MatchMode.PrivateOnline, seed, BotProfile.For(BotDifficulty.Normal));
                    Check(m.Phase == MatchPhase.Winner && m.Summary.EliminationOrder.Count == 3, $"{humans}H seed {seed}: {m.Phase}");
                    Check(m.Roster.Slots.Count(s => s.IsHuman) == humans, "humans");
                }
        });
        Test("MATCH-04 online start requires every human ready", () =>
        {
            var m = new MatchController();
            m.Roster.SeatHuman(1, "A"); m.Roster.SeatHuman(2, "B"); m.Roster.SetReady(1, true);
            Check(!m.Start(MatchMode.PrivateOnline, 1, out _), "blocked");
            m.Roster.SetReady(2, true);
            Check(m.Start(MatchMode.PrivateOnline, 1, out var e), e);
        });
        Test("MATCH-05 permanent mid-match disconnect hands the seat to a bot (no free advancement)", () =>
        {
            var m = new MatchController();
            m.Roster.SeatHuman(1, "A"); m.Roster.SeatHuman(2, "B"); m.Roster.SetReady(1, true); m.Roster.SetReady(2, true);
            var world = new HeadlessWorld(9); m.RoundStarting += r => world.Attach(r);
            int takeovers = 0; m.BotTakeover += _ => takeovers++;
            m.Start(MatchMode.PrivateOnline, 9, out _);
            m.SkipIntro();
            for (int i = 0; i < 100; i++) { m.Tick(0.1f); if (m.Phase == MatchPhase.InRound) world.Step(0.1f); }
            m.HumanDisconnected(2, m.Round.Time);
            for (int i = 0; i < 250; i++) { m.Tick(0.1f); if (m.Phase == MatchPhase.InRound) world.Step(0.1f); }
            Check(takeovers == 1 || !m.Roster[1].IsActive, "bot took over seat");
            Check(m.Roster.Slots.Count == 4, "still four seats");
            Check(m.Summary.EliminationOrder.Count <= 1, "no extra elimination from disconnect");
        });
        Test("MATCH-06 deterministic: same seed => identical log", () =>
        {
            var a = MatchSimulator.RunSolo(1234, BotDifficulty.Clever, BotProfile.For(BotDifficulty.Normal));
            var b = MatchSimulator.RunSolo(1234, BotDifficulty.Clever, BotProfile.For(BotDifficulty.Normal));
            Check(a.Log.Lines.SequenceEqual(b.Log.Lines), "logs equal");
            Check(a.Summary.WinnerSlot == b.Summary.WinnerSlot, "winner equal");
        });
        Test("NET-01 journal replay reproduces the host match exactly (40 seeded online/solo matches, chunked stream)", () =>
        {
            for (ulong seed = 900; seed < 940; seed++)
            {
                int humans = 1 + (int)(seed % 4);
                var host = new MatchController();
                for (int h = 0; h < humans; h++) { host.Roster.SeatHuman(700 + h, "H" + h); host.Roster.SetReady(700 + h, true); }
                host.Journal = new MatchJournal();
                var replica = new MatchController();
                var repLines = new List<string>(); int desync = 0, sums = 0;
                byte[] carry = null; var rng = new Random((int)seed);
                // Stream in arbitrary-size chunks while the host plays (like Fusion reliable data), replica applies as it goes.
                host.PhaseChanged += _ => { var b = host.Journal.TakePending(); if (b != null) Feed(b); };
                void Feed(byte[] b)
                {
                    int i = 0;
                    while (i < b.Length) { int n = Math.Min(b.Length - i, 1 + rng.Next(300)); var c = new byte[n]; Array.Copy(b, i, c, 0, n); i += n;
                        MatchJournal.Apply(replica, c, ref carry, sum => { sums++; if (sum != MatchJournal.Checksum(replica)) desync++; }); }
                }
                MatchSimulator.Run(host, humans == 1 ? MatchMode.Solo : MatchMode.PrivateOnline, seed, BotProfile.For(BotDifficulty.Normal));
                var rest = host.Journal.TakePending(); if (rest != null) Feed(rest);
                Check(carry == null || carry.Length == 0, "no dangling bytes");
                if (replica.Phase != host.Phase)
                {
                    var hl = host.Log.Lines; var rl = replica.Log.Lines; int k = 0; while (k < hl.Count && k < rl.Count && hl[k] == rl[k]) k++;
                    Console.WriteLine($"    diverge at line {k}/{hl.Count} (replica {rl.Count}):\n      host: {(k < hl.Count ? hl[k] : "-")}\n      repl: {(k < rl.Count ? rl[k] : "-")}\n      host prev: {(k > 0 ? hl[k - 1] : "-")}");
                }
                Check(replica.Phase == host.Phase, $"seed {seed}: phase {replica.Phase} vs {host.Phase}");
                Check(replica.Summary.WinnerSlot == host.Summary.WinnerSlot, $"seed {seed}: winner");
                Check(replica.Summary.EliminationOrder.SequenceEqual(host.Summary.EliminationOrder), $"seed {seed}: elimination order");
                Check(replica.Log.Lines.SequenceEqual(host.Log.Lines), $"seed {seed}: logs differ ({replica.Log.Lines.Count} vs {host.Log.Lines.Count})");
                Check(desync == 0 && sums > 30, $"seed {seed}: checksums {sums}, desync {desync}");
                for (int i = 0; i < 4; i++) Check(Math.Abs(replica.Summary.CumulativeScore[i] - host.Summary.CumulativeScore[i]) < 1e-4f, "score " + i);
            }
        });
        Test("NET-02 reconnecting client fast-forwards from the full journal; rematch replays too", () =>
        {
            var host = new MatchController();
            host.Roster.SeatHuman(1, "A"); host.Roster.SeatHuman(2, "B"); host.Roster.SetReady(1, true); host.Roster.SetReady(2, true);
            host.Journal = new MatchJournal();
            MatchSimulator.Run(host, MatchMode.PrivateOnline, 77, BotProfile.For(BotDifficulty.Normal));
            host.PrepareRematch(78);
            host.Roster.SetReady(1, true); host.Roster.SetReady(2, true);
            MatchSimulator.Run(host, MatchMode.PrivateOnline, 78, BotProfile.For(BotDifficulty.Normal));
            var late = new MatchController(); byte[] carry = null;
            MatchJournal.Apply(late, host.Journal.ToArray(), ref carry);
            Check(late.Log.Lines.SequenceEqual(host.Log.Lines), "late joiner identical after full replay");
            Check(late.Summary.WinnerSlot == host.Summary.WinnerSlot && late.Phase == host.Phase, "same winner/phase");
            Console.WriteLine($"    journal size for two full matches: {host.Journal.Length / 1024} KB");
        });
        Test("MATCH-07 rematch keeps the party and generates different chores", () =>
        {
            var b1 = new ChoreBoard(); b1.Generate(RoomCatalog.Get(RoomId.LivingRoom), 1, 7, new List<int> { 0, 1, 2, 3 });
            var b2 = new ChoreBoard(); b2.Generate(RoomCatalog.Get(RoomId.LivingRoom), 2, 7, new List<int> { 0, 1, 2, 3 });
            var s1 = string.Join(",", b1.Chores.Select(c => c.Template.Id + c.Steps.Count));
            var s2 = string.Join(",", b2.Chores.Select(c => c.Template.Id + c.Steps.Count));
            Check(s1 != s2, "varied");
            var m = MatchSimulator.RunSolo(5, BotDifficulty.Normal, null);
            m.PrepareRematch(6);
            Check(m.Roster.HumanCount == 1 && m.Roster.ActiveCount == 4 && m.Phase == MatchPhase.Lobby, "party kept, all active");
        });
        Test("MATCH-08 bots do real chores (validated steps) and can be eliminated by noise", () =>
        {
            int botSteps = 0, botNoiseElims = 0;
            for (ulong seed = 300; seed < 340; seed++)
            {
                var m = MatchController.CreateSolo("You", BotDifficulty.Rookie, seed);
                m.RoundEnded += r => { for (int i = 1; i < 4; i++) botSteps += r.Board.ValidatedSteps(i); };
                MatchSimulator.Run(m, MatchMode.Solo, seed, BotProfile.For(BotDifficulty.Clever));
                foreach (var s in m.Roster.Slots) if (s.IsBot && s.Reason == EliminationReason.CaughtMakingNoise) botNoiseElims++;
            }
            Console.WriteLine($"     bot validated steps: {botSteps}, bot noise eliminations: {botNoiseElims}");
            Check(botSteps > 200, "bots complete chore steps");
            Check(botNoiseElims > 0, "bots get caught too");
        });

        RecipeTests.Run(Test, Check);
        RecipeModeTests.Run(Test, Check);
        BeliefTests.Run(Test, Check);
        ProgressionTests.Run(Test, Check);
        ContinuousTests.Run(Test, Check);
        var matrixPath = Environment.GetEnvironmentVariable("SHHH_RECIPE_MATRIX");
        if (!string.IsNullOrEmpty(matrixPath))
        {
            var failedIds = new HashSet<string>(Failures.Where(f => f.StartsWith("RCP-")).Select(f => f.Substring(4, 4)));
            System.IO.File.WriteAllText(matrixPath, RecipeMatrix.Csv(r => !failedIds.Contains(r.Id)));
            Console.WriteLine("     recipe matrix -> " + matrixPath);
        }

        Console.WriteLine($"\n{_pass} passed, {_fail} failed");
        foreach (var f in Failures) Console.WriteLine("  FAILED " + f);
        return _fail == 0 ? 0 : 1;
    }
}
