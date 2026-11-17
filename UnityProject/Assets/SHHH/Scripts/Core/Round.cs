using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    [Serializable]
    public sealed class RoundConfig
    {
        public int Index;
        public RoomId Room;
        public float Duration;
        public int SharedTasks;
        public float NoiseMultiplier;
        public float MeterBaseline;
        public float PetActivity;
        public float CountdownSeconds = 3f;
        public float WakeQualifying = 8f;
        public float WakeLookback = 8f;
        /// <summary>V13 (owner 2026-11-15, "calmer Mom": she woke every ~13-30 s in a noisy house): after a warning the meter drops to
        /// deep sleep (30, below Stirring at 35) instead of 60 (already Stirring), so it takes real noise to wake her again.</summary>
        public float WarningResetValue = 30f;
        public float PetCalmSeconds = 30f;
        public float TieBreakSeconds = 20f;
        public int TieBreakSteps = 3;
        /// <summary>Tutorial/practice: Mom only ever warns; the timer never triggers an inspection.</summary>
        public bool Practice;
        /// <summary>V7: the round's chores are generated multi-step recipes (RecipeCatalog) instead of the legacy single-step templates.</summary>
        public bool RecipeMode = RecipeModeDefault;
        /// <summary>V7 default for new round configs (the shipped game plays recipes; tests can still build legacy rounds).</summary>
        public static bool RecipeModeDefault = true;

        /// <summary>2026-11-09: ONE uninterrupted match in the whole connected house (5 or 10 minutes). No eliminations: Mom warns,
        /// questions and hands out short timeouts / point penalties; the clock keeps running through her visits.</summary>
        public bool Continuous;
        /// <summary>Continuous: jobs dealt to every seat (job k in HouseLayout.Rooms[k]).</summary>
        public int JobsPerSeat = 1;
        /// <summary>V12: chore items start left around the house (not at their job). QA labs that test one mechanism switch it off.</summary>
        public bool ScatterChoreProps = true;
        public float TimeoutSeconds = MatchRules.TimeoutSeconds, PenaltyPoints = MatchRules.PenaltyPoints;

        public static RoundConfig ContinuousMatch(float seconds) => new RoundConfig
        {
            Index = 0, Room = RoomId.LivingRoom, Duration = seconds, SharedTasks = 0, NoiseMultiplier = 1.1f, MeterBaseline = 8f, PetActivity = 0.8f,
            CountdownSeconds = 3f, Continuous = true, JobsPerSeat = MatchRules.JobsPerSeat(seconds), RecipeMode = true,
        };

        public static RoundConfig PracticeConfig() => new RoundConfig { Index = 0, Room = RoomId.Practice, Duration = 3600f, SharedTasks = 3, NoiseMultiplier = 1f, MeterBaseline = 5f, PetActivity = 0.3f, CountdownSeconds = 0.5f, Practice = true, RecipeMode = false }; // tutorial keeps the single-step practice chores (Codex book acceptance relies on it)

        public static RoundConfig[] DefaultSequence() => new[]
        {
            new RoundConfig { Index = 0, Room = RoomId.LivingRoom, Duration = 150f, SharedTasks = 3, NoiseMultiplier = 1.00f, MeterBaseline = 8f,  PetActivity = 0.7f },
            new RoundConfig { Index = 1, Room = RoomId.Kitchen,    Duration = 130f, SharedTasks = 3, NoiseMultiplier = 1.15f, MeterBaseline = 14f, PetActivity = 0.85f },
            new RoundConfig { Index = 2, Room = RoomId.Laundry,    Duration = 110f, SharedTasks = 2, NoiseMultiplier = 1.30f, MeterBaseline = 20f, PetActivity = 0.95f },
        };
    }

    public enum RoundPhase { NotStarted, Countdown, Playing, MomSequence, TieBreak, Resolved }
    public enum RoundEndCause { None, NoiseWake, AllChoresDone, TimerExpired, OneSurvivor }

    /// <summary>
    /// One authoritative round (one room). Inputs are physical facts reported by host-side bodies/hands (grab, release,
    /// impact, placement); outputs are noise, blame, Mom's sequence and EXACTLY ONE elimination (latched).
    /// </summary>
    public sealed class RoundController
    {
        public readonly RoundConfig Config;
        public readonly RoomDefinition Room;
        public readonly CompetitorRoster Roster;
        public readonly ChoreBoard Board = new ChoreBoard();
        public readonly ScoreLedger Scores = new ScoreLedger();
        public readonly SleepMeter Meter = new SleepMeter();
        public readonly CausalLedger Ledger = new CausalLedger();
        public readonly NoiseModel Model = new NoiseModel();
        public readonly NoiseGate Gate = new NoiseGate();
        public readonly MomDirector Mom;
        public readonly GameLog Log;
        public readonly List<PetBrain> Pets = new List<PetBrain>();
        public static readonly int[] PetIds = { ActorIds.Cat, ActorIds.Dog };
        readonly Dictionary<int, float> _petCalmUntil = new Dictionary<int, float>();
        public float PetCalmUntil(int pet) => _petCalmUntil.TryGetValue(pet, out var t) ? t : -1f;
        public readonly ulong Seed;

        public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;
        public RoundEndCause EndCause { get; private set; }
        public float Time { get; private set; }
        public float PlayTime { get; private set; }
        public float TimeRemaining => Math.Max(0f, EffectiveDuration - PlayTime);
        /// <summary>V5 "rush": when the first sibling finishes their personal duty, at most RushSeconds remain.</summary>
        public const float RushSeconds = 30f;
        public const float MinRoundSeconds = 60f;
        /// <summary>V7 recipe rounds: time left after the first finished job (finish, guard, meddle, repair).</summary>
        public const float RecipeRushSeconds = 45f;
        public float EffectiveDuration { get; private set; }
        public int FirstFinisher { get; private set; } = ActorIds.None;
        public event Action<int> RushStarted;
        public MomVerdict Verdict { get; private set; }
        public int Warnings { get; private set; }
        public bool IsPlayable => Phase == RoundPhase.Playing || Phase == RoundPhase.TieBreak;

        // Held props: propId -> actor
        readonly Dictionary<int, int> _held = new Dictionary<int, int>();
        readonly List<NoiseEvent> _pending = new List<NoiseEvent>();
        readonly List<NoiseEvent> _recent = new List<NoiseEvent>();
        int _nextEventId = 1;
        bool _eliminationLatched;
        float _countdown;
        // Tie-break
        readonly Dictionary<int, int> _tieProgress = new Dictionary<int, int>();
        readonly Dictionary<int, float> _tieLastAt = new Dictionary<int, float>();
        float _tieTime;
        public IReadOnlyCollection<int> TieBreakers => _tieProgress.Keys;
        public Func<int, float> MatchScoreLookup; // cumulative previous-round score for the last deterministic tie fallback

        public event Action<RoundPhase> PhaseChanged;
        public event Action<NoiseEvent, Responsibility> NoiseRegistered;
        public event Action<MomVerdict> VerdictReached;     // apply elimination presentation here
        public event Action<int, EliminationReason> EliminationRequested;
        public event Action<RoundEndCause> Resolved;
        public event Action<int, int, int> TrapArmed;       // trapId, owner, propId
        public event Action<int, int> RescueAwarded;        // slot, points

        public RoundController(RoundConfig cfg, CompetitorRoster roster, ulong seed, GameLog log = null)
        {
            Config = cfg; Roster = roster; Seed = seed; Log = log ?? new GameLog();
            Room = cfg.Continuous ? BuildHouse() : cfg.RecipeMode ? BuildRecipeRoom(RoomCatalog.Get(cfg.Room)) : RoomCatalog.Get(cfg.Room);
            Mom = new MomDirector(seed);
            Board.ChoreCompleted += (c, a) =>
            {
                if (c.IsBonus && FirstFinisher == ActorIds.None && Phase == RoundPhase.Playing && !Config.Practice && !Config.Continuous)
                {
                    FirstFinisher = c.BonusOwner;
                    EffectiveDuration = Math.Min(EffectiveDuration, Math.Max(MinRoundSeconds, PlayTime + RushSeconds));
                    Log.Add(Time, $"RUSH {ActorIds.Describe(c.BonusOwner, Roster)} finished their job first - {TimeRemaining:0}s left");
                    RushStarted?.Invoke(c.BonusOwner);
                }
            };
            Board.ChoreCompleted += (c, a) => { Log.Add(Time, $"CHORE DONE '{c.Title}'{(c.IsBonus ? " (bonus " + ActorIds.Describe(c.BonusOwner, Roster) + ")" : "")} last step by {ActorIds.Describe(a, Roster)}"); Scores.SyncFromBoard(Board); };
            Board.ChoreReopened += c => { Log.Add(Time, $"CHORE UNDONE '{c.Title}' — credit suspended"); Scores.SyncFromBoard(Board); };
            Board.StepChanged += (c, s, a) => { Scores.SyncFromBoard(Board); Scores.ReportActivity(a, Time); };
            Board.Sabotaged += r => Log.Add(Time, $"SABOTAGE {ActorIds.Describe(r.Saboteur, Roster)} took {ActorIds.Describe(r.Victim, Roster)}'s item #{r.PropId}");
            Board.SabotageFoiled += r => { Log.Add(Time, $"FOILED {ActorIds.Describe(r.Victim, Roster)} restored item #{r.PropId} - {ActorIds.Describe(r.Saboteur, Roster)} caught"); Scores.SyncFromBoard(Board); };
        }

        // ------------------------------------------------------------------ V7 recipe mode
        public RecipeBoard Recipes { get; private set; }
        public RecipeGenerator.Result RecipePicks { get; private set; }
        public readonly List<RecipeFixturePlacement> RecipeFixtures = new List<RecipeFixturePlacement>();
        public readonly Dictionary<int, int> RecipePropOf = new Dictionary<int, int>();   // room prop id -> recipe world prop id
        public readonly Dictionary<int, int> RoomPropOf = new Dictionary<int, int>();     // recipe world prop id -> room prop id
        /// <summary>V7: every active seat's recipe is complete right now (early inspection, like "all personal duties done").</summary>
        public bool AllRecipesComplete()
        {
            var active = Roster.ActiveIndices(); if (active.Count == 0 || Recipes == null) return false;
            foreach (int s in active) { bool has = false; foreach (var i in Recipes.Instances) if (i.Owner == s) { has = true; if (!i.Complete) return false; } if (!has) return false; }
            return true;
        }

        // ------------------------------------------------------------------ V7 Gate 5: incidents, witness memory, Mom's belief, confrontation
        /// <summary>Mom reasons from her belief ledger, not from WorldTruth (recipe rounds = the shipping game). Legacy rounds keep the V6 verdicts.</summary>
        public bool BeliefMode => Recipes != null;
        public readonly BeliefLedger Belief = new BeliefLedger();
        public Interrogation Interro { get; private set; }
        public VerdictCalculator.Result LastResolution { get; private set; }
        public event Action<Incident> IncidentAdded;
        readonly Queue<int> _toPerceive = new Queue<int>();
        readonly Dictionary<int, int> _incidentOfNoise = new Dictionary<int, int>();

        /// <summary>Host perception layer: next incident whose witnesses must be determined (then call <see cref="ReportWitness"/>).</summary>
        public bool TryTakeIncidentToPerceive(out Incident inc) { inc = null; if (_toPerceive.Count == 0) return false; inc = Belief.Get(_toPerceive.Dequeue()); return inc != null; }

        Incident NewIncident(Incident i) { Belief.AddIncident(i); _toPerceive.Enqueue(i.Id); IncidentAdded?.Invoke(i); Log.Add(Time, $"INCIDENT {i}"); return i; }

        /// <summary>Plain place name for witness cards ("in the kitchen").</summary>
        string Place => HouseLayout.PlaceName(Room.Id);
        string PlaceOf(Vec3 p) => Config.Continuous ? HouseLayout.PlaceAt(p) : Place;

        string Name(int a) => ActorIds.IsSibling(a) ? Roster[a].DisplayName : a == ActorIds.Cat ? "the cat" : a == ActorIds.Dog ? "the dog" : "someone";

        /// <summary>The seat whose recipe station is closest to a position (within 1.2 m), for Mom's location speculation.</summary>
        public int SeatNear(Vec3 p)
        {
            int best = ActorIds.None; float bd = 1.2f;
            foreach (var f in RecipeFixtures) { float d = f.Position.FlatDistance(p); if (d < bd && Recipes.Instances[f.Instance].Owner >= 0) { bd = d; best = Recipes.Instances[f.Instance].Owner; } }
            return best;
        }

        void ReportWitnessImpl(int incident, int witness, float confidence)
        {
            var m = Belief.AddWitness(incident, witness, confidence, Name);
            if (m != null) Log.Add(Time, $"WITNESS {(witness == BeliefLedger.MomWitness ? "Mom" : Name(witness))} remembers I{incident} ({m.Confidence:0.00}): {m.Card}");
        }

        bool InterrogationOpen(int seat) => Interro != null && Mom.State == MomState.Interrogating && Interro.EvidenceOpen && ActorIds.IsSibling(seat) && Roster[seat].IsActive && !Interro.Done.Contains(seat);

        bool PresentCardImpl(int seat, int memoryId)
        {
            if (!InterrogationOpen(seat) || memoryId < 0 || memoryId >= Belief.Memories.Count) return false;
            var m = Belief.Memories[memoryId]; if (m.Witness != seat || m.Presented) return false;
            m.Presented = true; Interro.Cards.Add((seat, memoryId));
            Belief.Mom.Add(new Observation { Source = ObservationSource.WitnessCard, Incident = m.Incident, BelievedActor = m.SeenActor, Confidence = m.Confidence, Reporter = seat, MemoryId = m.Id, Time = Time, Text = m.Card });
            Log.Add(Time, $"CARD {Name(seat)} shows Mom: \"{m.Card}\"");
            return true;
        }

        bool AccuseImpl(int seat, int suspect)
        {
            if (!InterrogationOpen(seat) || !ActorIds.IsSibling(suspect) || suspect == seat || !Roster[suspect].IsActive) return false;
            if (Interro.Accusations.Exists(a => a.seat == seat)) return false; // one pointing gesture per seat
            Interro.Accusations.Add((seat, suspect));
            Belief.Mom.Add(new Observation { Source = ObservationSource.Accusation, Incident = Interro.Incident?.Id ?? -1, BelievedActor = suspect, Confidence = 0.1f, Reporter = seat, Time = Time, Text = $"{Name(seat)} points at {Name(suspect)} (no proof)" });
            Log.Add(Time, $"POINT {Name(seat)} points at {Name(suspect)}");
            return true;
        }

        void PassImpl(int seat) { if (InterrogationOpen(seat) || (Interro != null && ActorIds.IsSibling(seat) && Roster[seat].IsActive)) Interro?.Done.Add(seat); }

        /// <summary>Confrontation timer (called by MomDirector while Interrogating): over when every active seat is done or time ran out.</summary>
        bool TickInterrogation(float dt)
        {
            if (Interro == null) return true;
            Interro.Elapsed += dt;
            bool everyone = true; foreach (int s in Roster.ActiveIndices()) if (!Interro.Done.Contains(s)) everyone = false;
            return Interro.Remaining <= 0f || (everyone && Interro.Elapsed >= Interro.Present);
        }

        void StartInterrogation(Incident incident, bool inspection)
        {
            Interro = new Interrogation { Incident = incident, IsInspection = inspection, Started = Time };
            if (Config.Continuous)
            {
                // one full confrontation per match; later critical events (and the closing inspection after a full one) are quick
                bool full = FullConfrontations == 0;
                Interro.Present = full ? MatchRules.FullPresent : MatchRules.QuickPresent; Interro.Evidence = full ? MatchRules.FullEvidence : MatchRules.QuickEvidence;
                Interro.Quick = !full; if (full) FullConfrontations++;
                Confrontations++;
            }
            Interro.Question = incident == null ? "Who did their jobs - and who messed things up?"
                : !string.IsNullOrEmpty(incident.Question) ? incident.Question : "Who did this?";
            Mom.Interrogate = true; Mom.InterrogationTick = TickInterrogation; Mom.ResolveVerdict = ResolveConfrontation;
            Log.Add(Time, $"CONFRONTATION {(incident != null ? incident.ToString() : "(inspection)")} — Mom asks: {Interro.Question}");
        }

        MomVerdict ResolveConfrontation()
        {
            var v = Verdict ?? new MomVerdict();
            var active = Roster.ActiveIndices();
            int nearby = Interro?.Incident != null && Interro.Incident.Kind == IncidentKind.Noise ? SeatNear(Interro.Incident.Position) : ActorIds.None;
            var res = VerdictCalculator.Resolve(Belief, Interro ?? new Interrogation(), active, nearby);
            LastResolution = res;
            string truth = Interro?.Incident != null ? Name(Interro.Incident.TruthActor) : "-";
            if (Config.Continuous)
            {
                if (res.Proven)
                {
                    v.Kind = MomVerdictKind.Penalty; v.Culprit = res.Culprit;
                    v.Reason = Interro.Incident != null && Interro.Incident.Kind == IncidentKind.Noise ? EliminationReason.CaughtMakingNoise : EliminationReason.CaughtSabotaging;
                    v.Evidence = string.Join("; ", Belief.Mom.Where(o => (o.Source == ObservationSource.WitnessCard || o.Source == ObservationSource.SeenByMom) && o.BelievedActor == res.Culprit).Select(o => o.Text).Take(2));
                }
                else { v.Kind = MomVerdictKind.Warning; v.Evidence = "Not enough proof - everybody gets a warning."; }
            }
            else if (res.Proven)
            {
                v.Kind = MomVerdictKind.Eliminate; v.Culprit = res.Culprit;
                v.Reason = Interro.Incident != null && Interro.Incident.Kind == IncidentKind.Noise ? EliminationReason.CaughtMakingNoise : EliminationReason.CaughtSabotaging;
                v.Evidence = string.Join("; ", Belief.Mom.Where(o => (o.Source == ObservationSource.WitnessCard || o.Source == ObservationSource.SeenByMom) && o.BelievedActor == res.Culprit).Select(o => o.Text).Take(2));
            }
            else
            {
                // unresolvable: deterministic fair fallback = chore contributions (existing inspection, incl. tie-break)
                Scores.SyncFromBoard(Board);
                var ins = Scores.Inspect(active);
                if (ins.Outcome == InspectionOutcome.Eliminate) { v.Kind = MomVerdictKind.Eliminate; v.Culprit = ins.Eliminated; v.Reason = EliminationReason.LeastHelpfulInspection; v.Evidence = "Not enough proof. " + ins.Explanation; }
                else if (ins.Outcome == InspectionOutcome.TieBreakNeeded)
                { v.Kind = MomVerdictKind.TieBreak; v.Evidence = ins.Explanation; _tieProgress.Clear(); _tieLastAt.Clear(); foreach (int s in ins.Tied) { _tieProgress[s] = 0; _tieLastAt[s] = float.MaxValue; } }
                else v.Kind = MomVerdictKind.Warning;
            }
            Log.Add(Time, $"VERDICT {v.Kind} {ActorIds.Describe(v.Culprit, Roster)} ({v.Reason}) :: {string.Join(" | ", res.Trace)} :: truth(QA only)={truth}");
            Verdict = v;
            return v;
        }

        public bool IsRecipeProp(int propId) => Recipes != null && RecipePropOf.ContainsKey(propId);

        static int RoundIndexFor(RoomId room) => room == RoomId.Kitchen ? 1 : room == RoomId.Laundry ? 2 : 0;

        RoomDefinition BuildRecipeRoom(RoomDefinition src)
        {
            // per-round copy: legacy chore props/anchors removed (pet toys stay so pets keep their behaviour), recipe props added
            var r = CopyShell(src);
            // V12: household supplies stay where the family keeps them (detergent on the dryer) - they are tools, not chore props
            foreach (var p in src.Props) if (p.PetToy || p.Kind == "detergent") r.Props.Add(p);
            HouseClutter.Add(r, new List<(Vec3, float, float)> { (new Vec3(0, 0, 0), src.Width, src.Depth) }, Seed, Config.Duration);
            Recipes = new RecipeBoard();
            Recipes.World.CanAct = a => ActorIds.IsSibling(a) ? Roster[a].IsActive && !IsTimedOut(a) : ActorIds.IsPet(a);
            var stations = RecipeLayout.Stations(src.Id);
            var active = Roster.ActiveIndices();
            var seats = new List<int>(); foreach (var a in active) if (seats.Count < stations.Length) seats.Add(a);
            int next = (int)src.Id * 10000 + 5000;
            RecipePicks = StageRecipes(src.Id, stations, 0, seats, new List<string>(), ref next, r);
            Scatter(r, new List<(RoomId, Vec3, float, float)> { (src.Id, new Vec3(0, 0, 0), src.Width, src.Depth) });
            WireRecipeEvents();
            return r;
        }

        static RoomDefinition CopyShell(RoomDefinition src)
        {
            var r = new RoomDefinition { Id = src.Id, Title = src.Title, Width = src.Width, Depth = src.Depth, Height = src.Height, MomDoor = src.MomDoor,
                SiblingSpawns = (Vec3[])src.SiblingSpawns.Clone(), ChoreBoard = src.ChoreBoard, CatBed = src.CatBed, DogBed = src.DogBed };
            r.Furniture.AddRange(src.Furniture); r.SpectatorViews.AddRange(src.SpectatorViews);
            return r;
        }

        /// <summary>
        /// Continuous match: one runtime house. House coordinates = living-room coordinates; every built room adds its offset.
        /// Every seat gets <see cref="RoundConfig.JobsPerSeat"/> jobs, job k in <see cref="HouseLayout.Rooms"/>[k] (one table each),
        /// so players and bots travel through the house during one uninterrupted match.
        /// </summary>
        RoomDefinition BuildHouse()
        {
            var living = RoomCatalog.Get(RoomId.LivingRoom);
            var r = CopyShell(living); r.Title = "The House";
            foreach (var p in living.Props) if (p.PetToy) r.Props.Add(p);
            Recipes = new RecipeBoard();
            Recipes.World.CanAct = a => ActorIds.IsSibling(a) ? Roster[a].IsActive && !IsTimedOut(a) : ActorIds.IsPet(a);
            var active = Roster.ActiveIndices();
            var rest = new List<string>();
            RecipePicks = new RecipeGenerator.Result();
            int jobs = Math.Max(1, Math.Min(Config.JobsPerSeat, HouseLayout.Rooms.Length));
            for (int k = 0; k < jobs; k++)
            {
                var room = HouseLayout.Rooms[k]; var off = HouseLayout.Offset(room);
                var src = RoomCatalog.Get(room);
                if (k > 0) foreach (var f in src.Furniture) r.Furniture.Add(f.Shifted(off));
                var raw = RecipeLayout.Stations(room); var stations = new RecipeStation[raw.Length];
                for (int i = 0; i < raw.Length; i++) stations[i] = HouseLayout.Shift(raw[i], off);
                var seats = new List<int>(); foreach (var a in active) if (seats.Count < stations.Length) seats.Add(a);
                int next = (int)room * 10000 + 5000;
                var picks = StageRecipes(room, stations, k * 10, seats, rest, ref next, r);
                foreach (var pk in picks.Picks) { RecipePicks.Picks.Add(pk); rest.Add(pk.Recipe.Id); }
                foreach (var l in picks.Log) RecipePicks.Log.Add($"[{room}] {l}");
            }
            // V12: the lived-in house - trash anywhere, the pet food left somewhere, bowls by the beds (every room of the house)
            var areas = new List<(Vec3, float, float)>(); var named = new List<(RoomId, Vec3, float, float)>();
            for (int k = 0; k < jobs; k++) { var room = HouseLayout.Rooms[k]; var o = HouseLayout.Offset(room); var src = RoomCatalog.Get(room); areas.Add((new Vec3(o.x, 0, o.z), src.Width, src.Depth)); named.Add((room, new Vec3(o.x, 0, o.z), src.Width, src.Depth)); }
            Scatter(r, named);
            HouseClutter.Add(r, areas, Seed, Config.Duration, StationRects());
            WireRecipeEvents();
            return r;
        }

        List<(Vec3, float, float)> StationRects()
        {
            var l = new List<(Vec3, float, float)>();
            foreach (var s in RecipeStations.Values) l.Add((s.Center, (s.AlongZ ? s.Depth : s.Length) * 0.5f, (s.AlongZ ? s.Length : s.Depth) * 0.5f));
            return l;
        }

        /// <summary>Number of chore items that start somewhere else in the house this round.</summary>
        public int ScatteredProps { get; private set; }
        void Scatter(RoomDefinition r, List<(RoomId, Vec3, float, float)> rooms)
        {
            if (!Config.ScatterChoreProps || Recipes == null) return;
            var st = new List<Vec3>(); foreach (var s in RecipeStations.Values) st.Add(s.Center);
            var fx = new List<Vec3>(); foreach (var f in RecipeFixtures) fx.Add(f.Position);
            ScatteredProps = HouseClutter.ScatterChoreProps(r, rooms, Seed, st, fx,
                spec => RecipePropOf.TryGetValue(spec.Id, out var rp) && !Recipes.World.Prop(rp).IsPlaced, l => Log.Add(Time, l));
        }

        /// <summary>Every recipe station used this round, by the index stored in <see cref="RecipeFixturePlacement.Station"/> (house coordinates).</summary>
        public readonly Dictionary<int, RecipeStation> RecipeStations = new Dictionary<int, RecipeStation>();

        RecipeGenerator.Result StageRecipes(RoomId room, RecipeStation[] stations, int stationBase, List<int> seats, List<string> rest, ref int next, RoomDefinition r)
        {
            for (int i = 0; i < stations.Length; i++) RecipeStations[stationBase + i] = stations[i];
            // pick one recipe per seat that fits some station, then give each a distinct station it fits; if the picks cannot share the
            // tables, rest the hardest-to-place pick and pick again (deterministic, logged)
            int[] assign = null; RecipeGenerator.Result picks = null;
            var localRest = new List<string>(rest);
            ulong seed = Config.Continuous ? Seed * 0x9E3779B97F4A7C15UL + (ulong)((int)room + 1) * 0xBF58476D1CE4E5B9UL : Seed;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                picks = RecipeGenerator.Generate(seed, RoundIndexFor(room), seats, RecipeLayout.AreasHostedBy(room), localRest, RecipeDifficulty.Hard,
                    d => { foreach (var st in stations) if (RecipeLayout.Fits(d, st)) return true; return false; },
                    isHuman: a => Roster[a].IsHuman && !Config.Practice);
                var defs = new List<RecipeDef>(); foreach (var pk in picks.Picks) defs.Add(pk.Recipe);
                assign = RecipeLayout.Assign(defs, stations);
                if (assign != null) break;
                RecipeDef worst = null; int wc = int.MaxValue;
                foreach (var d in defs) { int c = 0; foreach (var st in stations) if (RecipeLayout.Fits(d, st)) c++; if (c < wc) { wc = c; worst = d; } }
                picks.Log.Add($"stations: picks do not share the tables; resting {worst.Id}"); localRest.Add(worst.Id);
            }
            if (assign == null) throw new InvalidOperationException("no station assignment for the recipe picks");
            for (int i = 0; i < picks.Picks.Count; i++)
            {
                var pick = picks.Picks[i];
                var inst = Recipes.Add(pick.Recipe, pick.Seat);
                RecipeFixtures.AddRange(RecipeLayout.Place(Recipes, inst, stations[assign[i]], stationBase + assign[i], out var spawn));
                foreach (var kv in spawn)
                {
                    var rp = Recipes.World.Prop(kv.Key);
                    var spec = new PropSpec { Id = next++, Kind = rp.Kind, Material = rp.Material, MassKg = 0.3f, Spawn = kv.Value, SpawnYaw = rp.IsPlaced ? RecipeFixtures.Find(x => x.FixtureId == rp.Fixture).Yaw : RecipeLayout.PropYaw(stations[assign[i]]), Tag = rp.Tag, Recipe = true,
                        PetKnockable = rp.Material == NoiseMaterial.Ceramic || rp.Material == NoiseMaterial.Metal || rp.Material == NoiseMaterial.Plastic || rp.Material == NoiseMaterial.Toy };
                    r.Props.Add(spec); RecipePropOf[spec.Id] = rp.Id; RoomPropOf[rp.Id] = spec.Id;
                }
            }
            return picks;
        }

        void WireRecipeEvents()
        {
            Recipes.Completed += (inst, a) =>
            {
                Log.Add(Time, $"RECIPE DONE {inst.Def.Id} '{inst.Def.DisplayName}' ({ActorIds.Describe(inst.Owner, Roster)}) last step by {ActorIds.Describe(a, Roster)}"); Scores.SyncFromBoard(Board);
                // first finished job starts the rush: the rest finish (and everyone guards / meddles) before Mom comes to inspect
                if (FirstFinisher == ActorIds.None && Phase == RoundPhase.Playing && !Config.Practice && !Config.Continuous && ActorIds.IsSibling(inst.Owner))
                {
                    FirstFinisher = inst.Owner;
                    EffectiveDuration = Math.Min(EffectiveDuration, Math.Max(MinRoundSeconds, PlayTime + RecipeRushSeconds));
                    Log.Add(Time, $"RUSH {ActorIds.Describe(inst.Owner, Roster)} finished their job first - {TimeRemaining:0}s left");
                    RushStarted?.Invoke(inst.Owner);
                }
            };
            Recipes.Reopened += inst => { Log.Add(Time, $"RECIPE UNDONE {inst.Def.Id}"); Scores.SyncFromBoard(Board); };
            Recipes.Sabotaged += rec =>
            {
                Log.Add(Time, $"SABOTAGE {ActorIds.Describe(rec.Saboteur, Roster)} disturbed {ActorIds.Describe(rec.Victim, Roster)}'s {Recipes.Instances[rec.Instance].Def.Id} step {rec.Step + 1} ({rec.Evidence})"); Scores.SyncFromBoard(Board);
                // one incident per sabotage ACTION (grouped records of one action share it)
                bool known = false; foreach (var i in Belief.Incidents) if (i.Kind == IncidentKind.Sabotage && i.TruthActor == rec.Saboteur && i.RecipeInstance == rec.Instance && Time - i.Time <= RecipeBoard.ActionWindow) known = true;
                if (known) return;
                var inst = Recipes.Instances[rec.Instance]; Vec3 at = default; foreach (var f in RecipeFixtures) if (f.Instance == rec.Instance) { at = f.Position; break; }
                NewIncident(new Incident { Kind = IncidentKind.Sabotage, Time = Time, TruthActor = rec.Saboteur, Victim = rec.Victim, What = $"mess with {Name(rec.Victim)}'s \"{inst.Def.DisplayName}\"", Question = $"Who messed with {Name(rec.Victim)}'s job?", Where = PlaceOf(at), Position = at, RecipeInstance = rec.Instance, SabotageIndex = Recipes.Sabotage.IndexOf(rec) });
            };
            Recipes.Foiled += rec => { Log.Add(Time, $"FOILED {ActorIds.Describe(rec.FoiledBy, Roster)} repaired {Recipes.Instances[rec.Instance].Def.Id} - {ActorIds.Describe(rec.Saboteur, Roster)} caught"); Scores.SyncFromBoard(Board); };
            Scores.Recipes = Recipes;
        }

        // ------------------------------------------------------------------ V12 pets: hunger + feeding (deterministic: journaled clock + journaled meals)
        /// <summary>Hunger 0 (full) .. 100 (starving) per pet (index 0 cat, 1 dog). Rises with the match clock at a per-pet rate so a pet
        /// needs 2-6 meals per match (5 min: 2-3, 10 min: 5-6); feeding = the pet really eating food from its bowl (host physics, journaled).
        /// A hungry pet whines / barks (host noise events) - Mom wakes if the family lets the pets starve.</summary>
        public readonly float[] PetHunger = new float[2];
        public readonly float[] PetHungerRate = new float[2];
        public readonly int[] PetMeals = new int[2], PetMealsTarget = new int[2];
        public float PetFedGrams { get; private set; }
        public const float GramsPerFullMeal = 110f, HungryAt = 55f, StarvingAt = 85f, FeedBonus = 3f, TrashBonus = 1.5f;
        public static int PetIndex(int petActor) => petActor == ActorIds.Dog ? 1 : 0;
        void InitPetNeeds()
        {
            var rng = new DetRng(Seed ^ 0xFEEDUL);
            float dur = Math.Max(120f, Config.Duration);
            for (int i = 0; i < 2; i++)
            {
                int target = Math.Max(2, Math.Min(6, (int)Math.Round(dur / 100f) + (rng.Value() < 0.5f ? 0 : 1) - (dur < 400f ? 1 : 0)));
                PetMealsTarget[i] = target; PetMeals[i] = 0;
                PetHunger[i] = 25f + rng.Range(0f, 20f);
                PetHungerRate[i] = 75f / (dur / target);   // from fed (~20) to hungry/barking in one meal interval
            }
            PetFedGrams = 0f;
        }
        void TickPetNeeds(float dt) { for (int i = 0; i < 2; i++) PetHunger[i] = Math.Min(100f, PetHunger[i] + PetHungerRate[i] * dt); }
        void PetEatImpl(int petActor, float grams, int feeder)
        {
            if (grams <= 0f) return; int i = PetIndex(petActor); float before = PetHunger[i];
            PetHunger[i] = Math.Max(0f, PetHunger[i] - grams * (90f / GramsPerFullMeal)); PetFedGrams += grams;
            if (before >= HungryAt * 0.7f && PetHunger[i] < 30f) { PetMeals[i]++; if (ActorIds.IsSibling(feeder)) { Scores.AddBonus(feeder, FeedBonus); Log.Add(Time, $"{(i == 0 ? "cat" : "dog")} fed by S{feeder + 1} (meal {PetMeals[i]}/{PetMealsTarget[i]})"); } }
        }
        // ------------------------------------------------------------------ V12 trash: a piece of litter physically inside a bin (host physics, journaled)
        public readonly HashSet<int> Binned = new HashSet<int>();
        /// <summary>Host-side courtesy (not journaled): who is already going for a piece of trash, so two siblings don't chase the same one.
        /// A claim lapses after <see cref="TrashClaimSeconds"/> (a failed pick-up never locks a piece).</summary>
        public const float TrashClaimSeconds = 20f;
        readonly Dictionary<int, (int slot, float at)> _trashClaims = new Dictionary<int, (int, float)>();
        public bool TrashClaimedByOther(int propId, int slot) => _trashClaims.TryGetValue(propId, out var c) && c.slot != slot && Time - c.at < TrashClaimSeconds;
        public void ClaimTrash(int slot, int propId) { _trashClaims[propId] = (slot, Time); }
        /// <summary>Same courtesy for bins: a sibling at a bin (hand on its lid, or heading there with trash) has it for a while; bots use
        /// another bin or wait - nobody yanks a lid out of a player's hand.</summary>
        readonly Dictionary<int, (int slot, float at)> _binClaims = new Dictionary<int, (int, float)>();
        public int BinIndexNear(Vec3 p) { int best = -1; float bd = 1.5f; if (Room == null) return -1; for (int i = 0; i < Room.Bins.Count; i++) { float d = Room.Bins[i].FlatDistance(p); if (d < bd) { bd = d; best = i; } } return best; }
        public void ClaimBin(int slot, Vec3 at) { int i = BinIndexNear(at); if (i >= 0) _binClaims[i] = (slot, Time); }
        public bool BinClaimedByOther(int index, int slot) => _binClaims.TryGetValue(index, out var c) && c.slot != slot && Time - c.at < TrashClaimSeconds;
        /// <summary>Host-only coordination (not rules state): which bot is currently feeding a pet.</summary>
        public int FeedingBot = -1;
        /// <summary>Host-only world facts for the pet brains (not rules state): is there food in each pet's bowl (0 cat, 1 dog)?</summary>
        public readonly bool[] BowlHasFood = new bool[2];
        void TrashBinnedImpl(int actor, int propId)
        {
            if (!Binned.Add(propId)) return;
            if (ActorIds.IsSibling(actor)) { Scores.AddBonus(actor, TrashBonus); Scores.ReportActivity(actor, Time); }
        }

        /// <summary>V12 house mechanisms (not recipe fixtures): mechanism id -> opening 0..100, set by <see cref="Verb.Mech"/>.</summary>
        public readonly Dictionary<int, int> HouseMech = new Dictionary<int, int>();

        string MechImpl(ROp op)
        {
            if (!ActorIds.IsSibling(op.Actor) || !Roster[op.Actor].IsActive) return "actor may not act";
            if (op.Slot < 0 || op.Slot > 999999) return "no such mechanism";
            var a = (op.Arg ?? "").Split(':');   // "level" or "level:slam" (a client's door slammed: the host makes the noise)
            if (!int.TryParse(a[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var lvl) || lvl < 0 || lvl > 100) return "bad level";
            HouseMech.TryGetValue(op.Slot, out var was);
            if (was == lvl && HouseMech.ContainsKey(op.Slot)) return "no change";
            HouseMech[op.Slot] = lvl; return null;
        }

        string RecipeActionImpl(ROp op)
        {
            if (op.Verb == Verb.Mech) return MechImpl(op);
            if (Recipes == null) return "not a recipe round";
            if (!IsPlayable) return "not playing";
            Recipes.World.Time = Time;
            var err = Recipes.Request(op);
            if (err == null && ActorIds.IsSibling(op.Actor)) Scores.ReportActivity(op.Actor, Time);
            return err;
        }

        public void Begin()
        {
            var active = Roster.ActiveIndices();
            int shared = Math.Max(1, Math.Min(Config.SharedTasks, CountTemplates() - active.Count));
            if (Config.RecipeMode) Board.Generate(Room, Seed, 0, new List<int>()); // recipes replace the legacy chores
            else Board.Generate(Room, Seed, shared, active);
            Scores.Reset(0f);
            Scores.SyncFromBoard(Board);
            Meter.Reset(Config.MeterBaseline);
            Ledger.Clear(); Gate.Clear(); _held.Clear(); _pending.Clear(); _recent.Clear(); _petCalmUntil.Clear();
            Mom.ResetForRound();
            InitPetNeeds(); Binned.Clear();
            _eliminationLatched = false; Verdict = null; EndCause = RoundEndCause.None; Warnings = 0; Interro = null;
            _timeoutUntil.Clear(); Confrontations = 0; FullConfrontations = 0; Penalties.Clear(); _finalInspection = false;
            EffectiveDuration = Config.Duration; FirstFinisher = ActorIds.None;
            Time = 0f; PlayTime = 0f; _countdown = Config.CountdownSeconds;
            SetPhase(RoundPhase.Countdown);
            Log.Add(Time, $"ROUND {Config.Index + 1} '{Room.Title}' seed={Seed} shared={Board.SharedTotal} active={active.Count}");
        }

        int CountTemplates() { int n = 0; foreach (var t in ChoreLibrary.All) if (t.Room == Config.Room) n++; return n; }

        void SetPhase(RoundPhase p) { if (Phase == p) return; Phase = p; PhaseChanged?.Invoke(p); }

        // ------------------------------------------------------------------ physical inputs (HOST ONLY)
        bool CanAct(int actor) => !ActorIds.IsSibling(actor) || (Roster[actor].IsActive && !IsTimedOut(actor));

        // ------------------------------------------------------------------ continuous match: timeouts and penalties
        readonly Dictionary<int, float> _timeoutUntil = new Dictionary<int, float>();
        public int Confrontations { get; private set; }
        public int FullConfrontations { get; private set; }
        bool _finalInspection;
        public bool FinalInspection => _finalInspection;
        public sealed class PenaltyRecord { public int Slot; public EliminationReason Reason; public float Time, Seconds, Points; public string Evidence; }
        public readonly List<PenaltyRecord> Penalties = new List<PenaltyRecord>();
        public event Action<PenaltyRecord> PenaltyApplied;
        /// <summary>"Sent to bed" for a few seconds after a proven disturbance: cannot grab or act (clock keeps running).</summary>
        public bool IsTimedOut(int slot) => _timeoutUntil.TryGetValue(slot, out var t) && Time < t;
        public float TimeoutRemaining(int slot) => _timeoutUntil.TryGetValue(slot, out var t) ? Math.Max(0f, t - Time) : 0f;

        bool GrabImpl(int actor, int propId)
        {
            if (!IsPlayable) return false;
            if (!CanAct(actor)) return false; // spectators can never touch props
            if (_held.TryGetValue(propId, out int holder) && holder != actor)
            {
                // Two-hand/two-player hold allowed: record the touch, the latest grabber shares control.
                Ledger.RecordTouch(propId, actor, TouchKind.Grab, Time);
                return true;
            }
            if (IsRecipeProp(propId))
            {
                Recipes.World.Time = Time;
                var rid = RecipePropOf[propId];
                if (Recipes.World.Prop(rid).HeldBy != actor && Recipes.Request(new ROp { Actor = actor, Verb = Verb.PickUp, Prop = rid, Prop2 = -1, Fixture = -1, Slot = -1 }) != null) return false; // e.g. inside a closed chest
            }
            _held[propId] = actor;
            Ledger.RecordTouch(propId, actor, TouchKind.Grab, Time);
            Scores.ReportActivity(actor, Time);
            // Picking a prop off its anchor un-does the step (credit suspended until it is placed again).
            Board.ReportRemoved(propId, actor, Time);
            return true;
        }

        void ReleaseImpl(int actor, int propId, float speed, bool thrown)
        {
            if (_held.TryGetValue(propId, out int holder) && holder == actor) _held.Remove(propId);
            if (IsRecipeProp(propId) && Recipes.World.Prop(RecipePropOf[propId]).HeldBy == actor)
            { Recipes.World.Time = Time; Recipes.Request(new ROp { Actor = actor, Verb = Verb.PutDown, Prop = RecipePropOf[propId], Prop2 = -1, Fixture = -1, Slot = -1, Arg = thrown ? "thrown" : "dropped" }); }
            Ledger.RecordTouch(propId, actor, thrown ? TouchKind.Throw : TouchKind.Release, Time, speed);
            if (ActorIds.IsSibling(actor)) Scores.ReportActivity(actor, Time);
            // Pet toy squeezed/thrown near a pet = lure.
            var spec = Room.Prop(propId);
            if (spec != null && spec.PetToy && ActorIds.IsSibling(actor))
            {
                // Rules state (who lured which pet) lives here so online replicas reproduce blame without pet brains.
                foreach (int pid in PetIds) if (Time >= PetCalmUntil(pid)) Ledger.RecordPetLure(pid, actor, Time);
                foreach (var pet in Pets) pet.Lure(actor, propId, Time);
            }
        }

        /// <summary>Host-side physics: a prop (or something it hit) is pushed/nudged by an actor without grabbing.</summary>
        void PushImpl(int actor, int propId, float speed)
        {
            if (!CanAct(actor)) return;
            Ledger.RecordTouch(propId, actor, ActorIds.IsPet(actor) ? TouchKind.PetNudge : TouchKind.Push, Time, speed);
            if (Board.ChoreForProp(propId) != null) Board.ReportRemoved(propId, actor, Time);
            // V7 recipes: a push only records the touch (evidence + blame). Whether a placed item actually got knocked crooked is decided
            // from its physical pose by the presentation layer (RecipeRuntime), which then reports Align "tilt" with the last toucher.
        }

        public int HolderOf(int propId) => _held.TryGetValue(propId, out int a) ? a : ActorIds.None;
        public int HeldBy(int actor) { foreach (var kv in _held) if (kv.Value == actor) return kv.Key; return -1; }

        bool PlacedImpl(int propId, int anchorId, int actor)
        {
            if (!IsPlayable || !CanAct(actor)) return false;
            bool ok = Board.ReportPlaced(propId, anchorId, actor, Time);
            if (ok) Log.Add(Time, $"step: {ActorIds.Describe(actor, Roster)} placed {Room.Prop(propId)?.Kind}");
            return ok;
        }

        void RemovedImpl(int propId, int actor) => Board.ReportRemoved(propId, actor, Time);

        /// <summary>Host physics impact (collision callback filtered by the Unity layer). Creates a pending noise event.</summary>
        NoiseEvent SubmitImpactImpl(int propId, float relSpeed, Vec3 pos, int parentEventId, NoiseCause cause, int directActor)
        {
            var spec = Room.Prop(propId);
            var mat = spec != null ? spec.Material : NoiseMaterial.Wood;
            float mass = spec != null ? spec.MassKg : 0.5f;
            float v = Model.ImpactValue(mat, mass, relSpeed, pos.FlatDistance(Room.MomDoor), Config.NoiseMultiplier);
            var e = new NoiseEvent
            {
                Id = _nextEventId++, Time = Time, SourceObjectId = propId, Material = mat, Cause = cause,
                ImpactEnergy = 0.5f * mass * relSpeed * relSpeed, Value = v, ParentEventId = parentEventId, Position = pos
            };
            if (directActor != ActorIds.None) e.DirectActor = directActor; // held item: the holder made this noise
            var trap = Ledger.TrapForObject(propId);
            if (trap != null && cause != NoiseCause.Throw) e.TrapId = trap.Id;
            _pending.Add(e);
            return e;
        }

        /// <summary>Door/drawer/washer slam or an appliance started by a sibling.</summary>
        NoiseEvent SubmitOperateImpl(int actor, int objectId, float rawValue, Vec3 pos, bool slam)
        {
            if (!CanAct(actor)) return null;
            var e = new NoiseEvent { Id = _nextEventId++, Time = Time, SourceObjectId = -1000 - objectId, Material = NoiseMaterial.Wood, Cause = slam ? NoiseCause.Slam : NoiseCause.Appliance, DirectActor = actor, Value = rawValue * Config.NoiseMultiplier, Position = pos };
            _pending.Add(e); return e;
        }

        NoiseEvent SubmitPetSoundImpl(int petActor, float value, Vec3 pos)
        {
            var e = new NoiseEvent { Id = _nextEventId++, Time = Time, Cause = NoiseCause.PetAction, DirectActor = petActor, Value = value * Config.NoiseMultiplier, Position = pos, Material = NoiseMaterial.Soft };
            _pending.Add(e); return e;
        }

        /// <summary>A trap prop started falling because <paramref name="triggeringActor"/> bumped it.</summary>
        NoiseEvent SubmitTrapTriggerImpl(int trapId, int triggeringActor, bool victimCareless, int propId, float relSpeed, Vec3 pos)
        {
            var e = SubmitImpactImpl(propId, relSpeed, pos, -1, NoiseCause.TrapTrigger, ActorIds.None);
            e.TrapId = trapId; e.TriggeringActor = triggeringActor;
            e.VictimHadCounterplay = victimCareless && Ledger.CounterplayExisted(trapId, Time);
            return e;
        }

        int ArmTrapImpl(int owner, int propId, bool warningVisible)
        {
            if (!IsPlayable || !CanAct(owner)) return -1;
            int id = Ledger.ArmTrap(owner, new[] { propId }, Time, warningVisible);
            Log.Add(Time, $"TRAP {id} armed by {ActorIds.Describe(owner, Roster)} using {Room.Prop(propId)?.Kind}");
            TrapArmed?.Invoke(id, owner, propId);
            return id;
        }

        bool DisarmTrapImpl(int trapId, int actor)
        {
            if (!IsPlayable || !CanAct(actor)) return false;
            var t = Ledger.GetTrap(trapId); if (t == null) return false;
            bool rescue = Ledger.DisarmTrap(trapId, actor);
            if (rescue && Scores.AddRescue(actor, "trap" + trapId, t.Owner)) { RescueAwarded?.Invoke(actor, 5); Log.Add(Time, $"RESCUE {ActorIds.Describe(actor, Roster)} disarmed trap {trapId}"); }
            return true;
        }

        /// <summary>Caught a falling object before impact (verified by host physics).</summary>
        void ReportCatchRescueImpl(int actor, int propId, int eventKey)
        {
            int creator = Ledger.ResolveDirectActor(propId, Time, out _, out _, out _);
            if (Scores.AddRescue(actor, "catch" + propId + ":" + eventKey, creator)) RescueAwarded?.Invoke(actor, 5);
        }

        void ReportActivityImpl(int slot) => Scores.ReportActivity(slot, Time);
        void ReportTrackingLostImpl(int slot, bool lost) => Scores.SetTrackingLost(slot, lost, Time);

        // ------------------------------------------------------------------ simulation step
        public void Tick(float dt)
        {
            Time += dt;
            switch (Phase)
            {
                case RoundPhase.Countdown:
                    _countdown -= dt;
                    if (_countdown <= 0f) { SetPhase(RoundPhase.Playing); Scores.Reset(Time); Scores.SyncFromBoard(Board); Log.Add(Time, "GO!"); }
                    break;
                case RoundPhase.Playing:
                    PlayTime += dt;
                    TickPetNeeds(dt);
                    FlushNoise();
                    if (Phase != RoundPhase.Playing) break; // wake started
                    Meter.Tick(Time, dt);
                    Mom.UpdateFromMeter(Meter.Stage, Time);
                    Scores.TickIdle(Time, Roster.ActiveIndices());
                    // Deterministic end precedence: (1) one survivor handled by MatchController; (2) wake (above); (3) all chores; (4) timer.
                    if (Config.Practice) break;
                    Board.ExpireSabotage(Time);
                    // continuous match: the clock always runs to 0:00 (finished players help, guard or meddle); legacy rounds end early
                    if (!Config.Continuous && (Recipes != null ? AllRecipesComplete() && PlayTime >= MinRoundSeconds : Board.AllPersonalComplete(Roster.ActiveIndices()))) BeginInspection(RoundEndCause.AllChoresDone);
                    else if (PlayTime >= EffectiveDuration) BeginInspection(RoundEndCause.TimerExpired);
                    break;
                case RoundPhase.MomSequence:
                    if (Config.Continuous) PlayTime += dt; // one uninterrupted match clock
                    _pending.Clear(); // physics during Mom's visit is ignored for gameplay
                    if (Mom.Tick(dt, out bool finished)) ApplyVerdict();
                    if (finished) AfterMomSequence();
                    break;
                case RoundPhase.TieBreak:
                    _tieTime += dt;
                    if (_tieTime >= Config.TieBreakSeconds) ResolveTieBreak();
                    break;
            }
        }

        void FlushNoise()
        {
            if (_pending.Count == 0) return;
            // Deterministic processing order for simultaneous events: time, then louder first, then id.
            _pending.Sort((a, b) => a.Time != b.Time ? a.Time.CompareTo(b.Time) : (a.Value != b.Value ? b.Value.CompareTo(a.Value) : a.Id.CompareTo(b.Id)));
            var batch = new List<NoiseEvent>(_pending); _pending.Clear();
            foreach (var e in batch)
            {
                if (_eliminationLatched || Phase != RoundPhase.Playing) break;
                if (ActorIds.IsSibling(e.DirectActor) && !Roster[e.DirectActor].IsActive) continue; // spectators can't make scored noise
                if (!Gate.Allow(e)) continue;
                if (!Ledger.Register(e)) continue;
                var resp = Ledger.Resolve(e);
                Scores.AddNoise(resp, e.Value);
                _recent.Add(e);
                _recent.RemoveAll(x => Time - x.Time > Config.WakeLookback);
                NoiseRegistered?.Invoke(e, resp);
                if (BeliefMode && e.Value >= BeliefLedger.NoiseIncidentMin && (ActorIds.IsSibling(e.DirectActor) || ActorIds.IsPet(e.DirectActor)))
                {
                    string kind = Room.Prop(e.SourceObjectId)?.Kind;
                    var inc = NewIncident(new Incident { Kind = IncidentKind.Noise, Time = Time, TruthActor = resp.Principal != ActorIds.None ? resp.Principal : e.DirectActor, What = kind != null ? $"make the {kind.Replace('_', ' ')} crash" : "make that noise", Question = kind != null ? $"Who made the {kind.Replace('_', ' ')} crash?" : "Who made that noise?", Where = PlaceOf(e.Position), Position = e.Position, Loudness = e.Value, NoiseId = e.Id });
                    _incidentOfNoise[e.Id] = inc.Id;
                }
                Log.Add(Time, $"noise {e} -> meter {Meter.Value:0.0}+{e.Value:0.0} :: {resp.Explanation}");
                if (Meter.Add(e.Value, Time)) { OnWake(e); break; }
                if (e.Value >= 12f) Mom.ReactToLoud(e.Value, Time);
            }
        }

        void OnWake(NoiseEvent trigger)
        {
            var decisive = trigger;
            if (!trigger.Qualifying(Config.WakeQualifying))
            {
                NoiseEvent best = null;
                foreach (var r in _recent) if (r.Qualifying(Config.WakeQualifying) && (best == null || r.Value > best.Value)) best = r;
                if (best != null) decisive = best;
            }
            var resp = Ledger.Resolve(decisive);
            if (Config.Practice)
            {
                // Tutorial: Mom grumbles, the meter calms down, play never freezes (xr_diag: placements were rejected for 16 s).
                Warnings++; Meter.Reset(Config.MeterBaseline); Mom.Grumble();
                Log.Add(Time, $"MOM GRUMBLES (practice) — {resp.Explanation}");
                return;
            }
            if (BeliefMode && (ActorIds.IsSibling(decisive.DirectActor) || ActorIds.IsSibling(resp.Principal)))
            {
                // V7: Mom heard it - she does not know who did it. She comes down and asks (confrontation decides from evidence).
                var pv = new MomVerdict { DecisiveEventId = decisive.Id, Kind = MomVerdictKind.None, Evidence = "" };
                _eliminationLatched = true; if (!Config.Continuous) EndCause = RoundEndCause.NoiseWake;
                _incidentOfNoise.TryGetValue(decisive.Id, out var incId);
                var incident = _incidentOfNoise.ContainsKey(decisive.Id) ? Belief.Get(incId) : NewIncident(new Incident { Kind = IncidentKind.Noise, Time = Time, TruthActor = resp.Principal, What = "make that noise", Question = "Who made that noise?", Where = PlaceOf(decisive.Position), Position = decisive.Position, Loudness = decisive.Value, NoiseId = decisive.Id });
                Belief.Mom.Add(new Observation { Source = ObservationSource.Heard, Incident = incident.Id, Confidence = Math.Min(1f, decisive.Value / 40f), Time = Time, Text = $"heard a {decisive.Material} crash in the {PlaceOf(decisive.Position)}" });
                Log.Add(Time, $"MOM WAKES — heard N{decisive.Id} ({decisive.Value:0.0}); she does not know who. Confrontation follows.");
                Verdict = pv; StartInterrogation(incident, false);
                SetPhase(RoundPhase.MomSequence);
                Mom.BeginWake(pv);
                return;
            }
            var v = new MomVerdict { DecisiveEventId = decisive.Id, Evidence = resp.Explanation };
            if (!Config.Practice && resp.IsAttributableSibling(0.7f) && Roster[resp.Principal].IsActive)
            {
                v.Kind = MomVerdictKind.Eliminate; v.Culprit = resp.Principal; v.Reason = EliminationReason.CaughtMakingNoise;
                _eliminationLatched = true; EndCause = RoundEndCause.NoiseWake;
            }
            else { v.Kind = MomVerdictKind.Warning; Warnings++; }
            Log.Add(Time, $"MOM WAKES — decisive N{decisive.Id}: {resp.Explanation} => {v.Kind} {ActorIds.Describe(v.Culprit, Roster)}");
            Verdict = v;
            SetPhase(RoundPhase.MomSequence);
            Mom.BeginWake(v);
        }

        void BeginInspection(RoundEndCause cause)
        {
            if (_eliminationLatched) return;
            _eliminationLatched = true; EndCause = cause; _finalInspection = true;
            Scores.SyncFromBoard(Board);
            if (BeliefMode)
            {
                // V7: Mom inspects the jobs AND asks about the worst disturbance she can see (an unrepaired sabotage), if any
                Incident worst = null;
                foreach (var i in Belief.Incidents)
                    if (i.Kind == IncidentKind.Sabotage && i.SabotageIndex >= 0 && !Recipes.Sabotage[i.SabotageIndex].Foiled && !Recipes.Instances[i.RecipeInstance].Complete) worst = i;
                if (worst != null) Belief.Mom.Add(new Observation { Source = ObservationSource.Inspected, Incident = worst.Id, Confidence = 0.5f, Time = Time, Text = $"{Name(worst.Victim)}'s job is messed up" });
                var pv = new MomVerdict { Kind = MomVerdictKind.None };
                Verdict = pv; StartInterrogation(worst, true);
                Log.Add(Time, $"INSPECTION ({cause}) — Mom inspects the jobs{(worst != null ? " and asks about " + worst : "")}");
                SetPhase(RoundPhase.MomSequence);
                Mom.BeginInspection(pv);
                return;
            }
            var res = Scores.Inspect(Roster.ActiveIndices());
            var v = new MomVerdict();
            if (res.Outcome == InspectionOutcome.Eliminate)
            { v.Kind = MomVerdictKind.Eliminate; v.Culprit = res.Eliminated; v.Reason = EliminationReason.LeastHelpfulInspection; v.Evidence = res.Explanation; }
            else if (res.Outcome == InspectionOutcome.TieBreakNeeded)
            {
                v.Kind = MomVerdictKind.TieBreak; v.Evidence = res.Explanation;
                _tieProgress.Clear(); _tieLastAt.Clear();
                foreach (int s in res.Tied) { _tieProgress[s] = 0; _tieLastAt[s] = float.MaxValue; }
            }
            else v.Kind = MomVerdictKind.Warning;
            var sb = new System.Text.StringBuilder();
            foreach (var r in res.Ranking) sb.Append(" | ").Append(r);
            Log.Add(Time, $"INSPECTION ({cause}) => {v.Kind} {ActorIds.Describe(v.Culprit, Roster)} :: {v.Evidence}{sb}");
            Verdict = v;
            SetPhase(RoundPhase.MomSequence);
            Mom.BeginInspection(v);
        }

        void ApplyVerdict()
        {
            if (Verdict.Kind == MomVerdictKind.Penalty && ActorIds.IsSibling(Verdict.Culprit))
            {
                var pr = new PenaltyRecord { Slot = Verdict.Culprit, Reason = Verdict.Reason, Time = Time, Seconds = _finalInspection ? 0f : Config.TimeoutSeconds, Points = Config.PenaltyPoints, Evidence = Verdict.Evidence };
                Penalties.Add(pr); Scores.AddPenalty(pr.Slot, pr.Points);
                if (pr.Seconds > 0f) _timeoutUntil[pr.Slot] = Time + pr.Seconds;
                int held = HeldBy(pr.Slot); if (held >= 0 && pr.Seconds > 0f) ReleaseImpl(pr.Slot, held, 0f, false);
                Log.Add(Time, $"PENALTY {ActorIds.Describe(pr.Slot, Roster)} -{pr.Points:0} pts{(pr.Seconds > 0 ? $", {pr.Seconds:0}s timeout" : "")} ({pr.Reason})");
                PenaltyApplied?.Invoke(pr);
            }
            VerdictReached?.Invoke(Verdict);
            if (Verdict.Kind == MomVerdictKind.Eliminate)
                EliminationRequested?.Invoke(Verdict.Culprit, Verdict.Reason);
        }

        void AfterMomSequence()
        {
            if (Config.Continuous)
            {
                // the match never eliminates: the closing inspection (or a visit that ran past 0:00) ends it; anything else resumes
                if (_finalInspection || PlayTime >= EffectiveDuration)
                {
                    if (EndCause == RoundEndCause.None) EndCause = RoundEndCause.TimerExpired;
                    SetPhase(RoundPhase.Resolved); Resolved?.Invoke(EndCause); return;
                }
                if (Verdict.Kind != MomVerdictKind.Penalty) Warnings++;
                Meter.SetAfterWarning(Config.WarningResetValue, Time);
                foreach (int pid in PetIds) _petCalmUntil[pid] = Time + Config.PetCalmSeconds;
                foreach (var p in Pets) p.Calm(Time, Config.PetCalmSeconds);
                Verdict = null; _eliminationLatched = false;
                SetPhase(RoundPhase.Playing);
                Log.Add(Time, $"Mom goes back to bed. {TimeRemaining:0}s left on the clock.");
                return;
            }
            switch (Verdict.Kind)
            {
                case MomVerdictKind.Eliminate:
                    SetPhase(RoundPhase.Resolved); Resolved?.Invoke(EndCause); break;
                case MomVerdictKind.TieBreak:
                    _tieTime = 0f; SetPhase(RoundPhase.TieBreak); Log.Add(Time, "TIE-BREAK challenge begins"); break;
                default:
                    // Pet-only / ambiguous wake: warning, controlled meter reduction, pets calmed, round resumes.
                    Meter.SetAfterWarning(Config.WarningResetValue, Time);
                    foreach (int pid in PetIds) _petCalmUntil[pid] = Time + Config.PetCalmSeconds;
                    foreach (var p in Pets) p.Calm(Time, Config.PetCalmSeconds);
                    Verdict = null;
                    SetPhase(RoundPhase.Playing);
                    Log.Add(Time, "Mom grumbles and goes back to bed. Round resumes.");
                    break;
            }
        }

        // ------------------------------------------------------------------ tie-break
        void ReportTieBreakStepImpl(int slot)
        {
            if (Phase != RoundPhase.TieBreak || !_tieProgress.ContainsKey(slot)) return;
            _tieProgress[slot]++; _tieLastAt[slot] = _tieTime;
            if (_tieProgress[slot] >= Config.TieBreakSteps) ResolveTieBreak();
        }

        void ResolveTieBreak()
        {
            if (Phase != RoundPhase.TieBreak) return;
            // Loser: fewest steps; then latest last-progress time; then lower previous cumulative score; then higher slot index.
            int loser = ActorIds.None;
            foreach (var s in _tieProgress.Keys)
            {
                if (loser == ActorIds.None) { loser = s; continue; }
                int c = CompareTie(s, loser);
                if (c < 0) loser = s;
            }
            Verdict = new MomVerdict { Kind = MomVerdictKind.Eliminate, Culprit = loser, Reason = EliminationReason.TieBreakLost, Evidence = "Slowest in the tie-break challenge" };
            Log.Add(Time, $"TIE-BREAK lost by {ActorIds.Describe(loser, Roster)}");
            VerdictReached?.Invoke(Verdict);
            EliminationRequested?.Invoke(loser, EliminationReason.TieBreakLost);
            SetPhase(RoundPhase.Resolved); Resolved?.Invoke(EndCause);
        }

        /// <summary>negative => a is "worse" (should lose) than b.</summary>
        int CompareTie(int a, int b)
        {
            if (_tieProgress[a] != _tieProgress[b]) return _tieProgress[a].CompareTo(_tieProgress[b]);
            if (_tieLastAt[a] != _tieLastAt[b]) return _tieLastAt[b].CompareTo(_tieLastAt[a]);
            float ma = MatchScoreLookup?.Invoke(a) ?? 0f, mb = MatchScoreLookup?.Invoke(b) ?? 0f;
            if (Math.Abs(ma - mb) > 0.01f) return ma.CompareTo(mb);
            return b.CompareTo(a); // documented last resort: higher seat number loses
        }

        /// <summary>Withdrawal/one-survivor short-circuit from the MatchController.</summary>
        void ForceResolveImpl(RoundEndCause cause)
        {
            _eliminationLatched = true; EndCause = cause; SetPhase(RoundPhase.Resolved); Resolved?.Invoke(cause);
        }


        // ------------------------------------------------------------------ journaled public API (V6 online replication)
        /// <summary>Set by the MatchController: every outermost call below is recorded for client replay.</summary>
        public MatchJournal Journal;
        JournalScope J(MatchJournal.Op op, out System.IO.BinaryWriter w) { var j = Journal; w = j != null && j.Begin(op) ? j.W : null; return new JournalScope(j); }

        /// <summary>V7: any recipe operation that is not a plain grab/release/push (snap into a slot, straighten, lid, wipe, fold, button, pair/attach). Journaled.</summary>
        public void ReportWitness(int incident, int witness, float confidence) { using (J(MatchJournal.Op.RWitness, out var w)) { if (w != null) { w.Write(incident); w.Write(witness); w.Write(confidence); } ReportWitnessImpl(incident, witness, confidence); } }
        public bool PresentCard(int seat, int memoryId) { using (J(MatchJournal.Op.RCard, out var w)) { if (w != null) { w.Write(seat); w.Write(memoryId); } return PresentCardImpl(seat, memoryId); } }
        public bool Accuse(int seat, int suspect) { using (J(MatchJournal.Op.RAccuse, out var w)) { if (w != null) { w.Write(seat); w.Write(suspect); } return AccuseImpl(seat, suspect); } }
        public void PassInterrogation(int seat) { using (J(MatchJournal.Op.RPass, out var w)) { if (w != null) w.Write(seat); PassImpl(seat); } }
        public string RecipeAction(ROp op) { using (J(MatchJournal.Op.RRecipe, out var w)) { if (w != null) { w.Write(op.EventId); w.Write(op.Actor); w.Write((byte)op.Verb); w.Write(op.Prop); w.Write(op.Prop2); w.Write(op.Fixture); w.Write(op.Slot); w.Write(op.Arg ?? ""); } return RecipeActionImpl(op); } }
        public bool Grab(int actor, int propId) { using (J(MatchJournal.Op.RGrab, out var w)) { if (w != null) { w.Write(actor); w.Write(propId); } return GrabImpl(actor, propId); } }
        public void Release(int actor, int propId, float speed, bool thrown) { using (J(MatchJournal.Op.RRelease, out var w)) { if (w != null) { w.Write(actor); w.Write(propId); w.Write(speed); w.Write(thrown); } ReleaseImpl(actor, propId, speed, thrown); } }
        public void Push(int actor, int propId, float speed) { using (J(MatchJournal.Op.RPush, out var w)) { if (w != null) { w.Write(actor); w.Write(propId); w.Write(speed); } PushImpl(actor, propId, speed); } }
        public bool Placed(int propId, int anchorId, int actor) { using (J(MatchJournal.Op.RPlaced, out var w)) { if (w != null) { w.Write(propId); w.Write(anchorId); w.Write(actor); } return PlacedImpl(propId, anchorId, actor); } }
        public void Removed(int propId, int actor) { using (J(MatchJournal.Op.RRemoved, out var w)) { if (w != null) { w.Write(propId); w.Write(actor); } RemovedImpl(propId, actor); } }
        public NoiseEvent SubmitImpact(int propId, float relSpeed, Vec3 pos, int parentEventId = -1, NoiseCause cause = NoiseCause.DirectImpact, int directActor = ActorIds.None)
        { using (J(MatchJournal.Op.RImpact, out var w)) { if (w != null) { w.Write(propId); w.Write(relSpeed); MatchJournal.WriteVec(w, pos); w.Write(parentEventId); w.Write((byte)cause); w.Write(directActor); } return SubmitImpactImpl(propId, relSpeed, pos, parentEventId, cause, directActor); } }
        public NoiseEvent SubmitOperate(int actor, int objectId, float rawValue, Vec3 pos, bool slam) { using (J(MatchJournal.Op.ROperate, out var w)) { if (w != null) { w.Write(actor); w.Write(objectId); w.Write(rawValue); MatchJournal.WriteVec(w, pos); w.Write(slam); } return SubmitOperateImpl(actor, objectId, rawValue, pos, slam); } }
        public void PetEat(int petActor, float grams, int feeder) { using (J(MatchJournal.Op.RPetEat, out var w)) { if (w != null) { w.Write(petActor); w.Write(grams); w.Write(feeder); } PetEatImpl(petActor, grams, feeder); } }
        public void TrashBinned(int actor, int propId) { using (J(MatchJournal.Op.RTrash, out var w)) { if (w != null) { w.Write(actor); w.Write(propId); } TrashBinnedImpl(actor, propId); } }
        public NoiseEvent SubmitPetSound(int petActor, float value, Vec3 pos) { using (J(MatchJournal.Op.RPetSound, out var w)) { if (w != null) { w.Write(petActor); w.Write(value); MatchJournal.WriteVec(w, pos); } return SubmitPetSoundImpl(petActor, value, pos); } }
        public NoiseEvent SubmitTrapTrigger(int trapId, int triggeringActor, bool victimCareless, int propId, float relSpeed, Vec3 pos)
        { using (J(MatchJournal.Op.RTrap, out var w)) { if (w != null) { w.Write(trapId); w.Write(triggeringActor); w.Write(victimCareless); w.Write(propId); w.Write(relSpeed); MatchJournal.WriteVec(w, pos); } return SubmitTrapTriggerImpl(trapId, triggeringActor, victimCareless, propId, relSpeed, pos); } }
        public int ArmTrap(int owner, int propId, bool warningVisible) { using (J(MatchJournal.Op.RArmTrap, out var w)) { if (w != null) { w.Write(owner); w.Write(propId); w.Write(warningVisible); } return ArmTrapImpl(owner, propId, warningVisible); } }
        public bool DisarmTrap(int trapId, int actor) { using (J(MatchJournal.Op.RDisarm, out var w)) { if (w != null) { w.Write(trapId); w.Write(actor); } return DisarmTrapImpl(trapId, actor); } }
        public void ReportCatchRescue(int actor, int propId, int eventKey) { using (J(MatchJournal.Op.RCatch, out var w)) { if (w != null) { w.Write(actor); w.Write(propId); w.Write(eventKey); } ReportCatchRescueImpl(actor, propId, eventKey); } }
        public void ReportActivity(int slot) { using (J(MatchJournal.Op.RActivity, out var w)) { if (w != null) w.Write(slot); ReportActivityImpl(slot); } }
        public void ReportTrackingLost(int slot, bool lost) { using (J(MatchJournal.Op.RTracking, out var w)) { if (w != null) { w.Write(slot); w.Write(lost); } ReportTrackingLostImpl(slot, lost); } }
        public void ReportTieBreakStep(int slot) { using (J(MatchJournal.Op.RTieStep, out var w)) { if (w != null) w.Write(slot); ReportTieBreakStepImpl(slot); } }
        public void ForceResolve(RoundEndCause cause) { using (J(MatchJournal.Op.RForceResolve, out var w)) { if (w != null) w.Write((byte)cause); ForceResolveImpl(cause); } }

        public IReadOnlyList<NoiseEvent> RecentNoise => _recent;
        public float Countdown => Math.Max(0f, _countdown);
    }
}
