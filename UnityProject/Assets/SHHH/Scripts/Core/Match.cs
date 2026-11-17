using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum MatchMode { Solo, PrivateOnline, Practice }
    public enum MatchPhase { Lobby, Intro, LoadingRoom, InRound, RoundOutro, Winner, Results, Cancelled }

    [Serializable]
    public sealed class MatchSummary
    {
        public int WinnerSlot = ActorIds.None;
        public bool WinnerIsBot;
        public readonly List<int> EliminationOrder = new List<int>();
        public readonly List<string> RoundStories = new List<string>();
        public readonly float[] CumulativeScore = new float[CompetitorRoster.SlotCount];
        public readonly float[] CumulativeNoise = new float[CompetitorRoster.SlotCount];
        public readonly int[] ChoresCompleted = new int[CompetitorRoster.SlotCount];
        public readonly int[] Rescues = new int[CompetitorRoster.SlotCount];
        public string FunniestIncident = "";
        // V7 recipe-round stats (authoritative; progression reads only these)
        public readonly int[] RecipesCompleted = new int[CompetitorRoster.SlotCount];
        public readonly int[] Foils = new int[CompetitorRoster.SlotCount];
        public readonly int[] ProvingCards = new int[CompetitorRoster.SlotCount];   // cards that backed a proven verdict
        public readonly int[] CaughtByEvidence = new int[CompetitorRoster.SlotCount];
        public readonly int[] RoundsSurvived = new int[CompetitorRoster.SlotCount];
        // 2026-11-09 continuous match (authoritative)
        public bool Continuous; public float MatchSeconds;
        public readonly float[] FinalScore = new float[CompetitorRoster.SlotCount];
        public readonly float[] Penalties = new float[CompetitorRoster.SlotCount];
        public readonly int[] Timeouts = new int[CompetitorRoster.SlotCount];
        public readonly float[] FirstJobAt = { -1f, -1f, -1f, -1f };
        /// <summary>Best first. Tie-break: score > job progress > fewer noise points > earlier first finished job > lower seat.</summary>
        public readonly List<int> Ranking = new List<int>();
        public string EndReason = "";
    }

    /// <summary>
    /// Owns the four-seat roster. 2026-11-09: the shipping game is ONE continuous 5/10-minute match in the whole house
    /// (<see cref="Continuous"/>, default); the legacy 4 -> 3 -> 2 -> 1 room sequence is kept only for old tests/replays.
    /// Runs identically for offline solo (1H+3B) and the online host (2H+2B / 3H+1B / 4H+0B).
    /// The Unity layer listens to events to load scenes, spawn bodies and stage presentation.
    /// </summary>
    public sealed class MatchController
    {
        public readonly CompetitorRoster Roster;
        public readonly GameLog Log = new GameLog();
        public MatchMode Mode { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.Lobby;
        public RoundConfig[] Rounds = ContinuousDefault ? new[] { RoundConfig.ContinuousMatch(MatchRules.FiveMinutes) } : RoundConfig.DefaultSequence();
        /// <summary>Shipping default (tests may build legacy matches).</summary>
        public static bool ContinuousDefault = true;
        public bool Continuous => Rounds.Length > 0 && Rounds[0].Continuous;
        /// <summary>Host choice before start: 300 or 600 seconds.</summary>
        public void SetMatchLength(float seconds) { if (Phase == MatchPhase.Lobby || Phase == MatchPhase.Results || Phase == MatchPhase.Cancelled) Rounds = new[] { RoundConfig.ContinuousMatch(seconds) }; }
        public float MatchLength => Continuous ? Rounds[0].Duration : 0f;
        public int RoundIndex { get; private set; } = -1;
        public RoundController Round { get; private set; }
        public MatchSummary Summary { get; private set; } = new MatchSummary();
        public ulong MatchSeed { get; private set; }
        public float IntroSeconds = 11.5f, OutroSeconds = 4f, WinnerSeconds = 8f;
        public float DisconnectGrace = 20f;
        public BotDifficulty TakeoverDifficulty = BotDifficulty.Normal;
        float _phaseTime;
        float _largestNoise; // for "funniest incident"

        public event Action<MatchPhase> PhaseChanged;
        public event Action<RoundController> RoundStarting;  // Unity: load the room scene, spawn bodies, wire brains
        public event Action<RoundController> RoundEnded;
        public event Action<int, EliminationReason, int> SlotEliminated;
        public event Action<MatchSummary> MatchFinished;

        public MatchController(CompetitorRoster roster = null) { Roster = roster ?? new CompetitorRoster(); }

        public static MatchController CreateSolo(string playerName, BotDifficulty botDifficulty, ulong seed)
        {
            var m = new MatchController();
            m.Roster.SeatHuman(0, playerName);
            m.Roster.SetReady(0, true);
            m.Roster.SetBotDifficulty(botDifficulty);
            m.Mode = MatchMode.Solo; m.MatchSeed = seed;
            return m;
        }

        bool StartImpl(MatchMode mode, ulong seed, out string error)
        {
            Mode = mode; MatchSeed = seed;
            if (!Roster.ValidateForStart(out error)) return false;
            if (mode != MatchMode.Solo && !Roster.AllHumansReady) { error = "Not every human is ready"; return false; }
            Roster.LockForMatch();
            Summary = new MatchSummary();
            RoundIndex = -1; _largestNoise = 0f;
            Log.Add(0, $"MATCH START mode={mode} seed={seed} humans={Roster.HumanCount} bots={Roster.BotCount}");
            foreach (var s in Roster.Slots) Log.Add(0, "  " + s);
            SetPhase(MatchPhase.Intro);
            return true;
        }

        void SetPhase(MatchPhase p) { Phase = p; _phaseTime = 0f; PhaseChanged?.Invoke(p); }

        void SkipIntroImpl() { if (Phase == MatchPhase.Intro) _phaseTime = IntroSeconds; }

        void TickImpl(float dt)
        {
            _phaseTime += dt;
            CheckDisconnectGrace();
            switch (Phase)
            {
                case MatchPhase.Intro:
                    if (_phaseTime >= IntroSeconds) NextRound();
                    break;
                case MatchPhase.LoadingRoom:
                    // Unity calls NotifyRoomReady(); headless/offline proceeds immediately.
                    break;
                case MatchPhase.InRound:
                    if (!Continuous && Roster.ActiveCount <= 1) { Round.ForceResolve(RoundEndCause.OneSurvivor); break; }
                    Round.Tick(dt);
                    break;
                case MatchPhase.RoundOutro:
                    if (_phaseTime >= OutroSeconds) { if (Roster.ActiveCount <= 1) FinishMatch(); else NextRound(); }
                    break;
                case MatchPhase.Winner:
                    if (_phaseTime >= WinnerSeconds) SetPhase(MatchPhase.Results);
                    break;
            }
        }

        void NextRound()
        {
            if (Roster.ActiveCount <= 1) { FinishMatch(); return; }
            RoundIndex++;
            // Always the full sequence; if somehow more rounds needed than defined, repeat the laundry config.
            var cfg = Rounds[Math.Min(RoundIndex, Rounds.Length - 1)];
            ulong seed = MatchSeed * 0x9E3779B97F4A7C15UL + (ulong)(RoundIndex + 1) * 0xD1B54A32D192ED03UL;
            Round = new RoundController(cfg, Roster, seed, Log) { Journal = Journal };
            Round.MatchScoreLookup = s => Summary.CumulativeScore[s];
            Round.EliminationRequested += OnEliminationRequested;
            Round.Resolved += OnRoundResolved;
            Round.NoiseRegistered += (e, r) =>
            {
                if (e.Value > _largestNoise) { _largestNoise = e.Value; Summary.FunniestIncident = $"Round {RoundIndex + 1}: {r.Explanation} ({e.Value:0} noise!)"; }
            };
            Round.RescueAwarded += (s, p) => Summary.Rescues[s]++;
            Round.PenaltyApplied += pr => { Summary.Penalties[pr.Slot] += pr.Points; if (pr.Seconds > 0f) Summary.Timeouts[pr.Slot]++; };
            if (Round.Recipes != null) Round.Recipes.Completed += (inst, a) => { if (ActorIds.IsSibling(inst.Owner) && Summary.FirstJobAt[inst.Owner] < 0f) Summary.FirstJobAt[inst.Owner] = Round.PlayTime; };
            SetPhase(MatchPhase.LoadingRoom);
            RoundStarting?.Invoke(Round);
            Round.Begin();
            if (!WaitForRoomReady) NotifyRoomReady();
        }

        /// <summary>When true (Unity), the round countdown waits for NotifyRoomReady after the scene loads and all clients ack.</summary>
        public bool WaitForRoomReady;
        void NotifyRoomReadyImpl() { if (Phase == MatchPhase.LoadingRoom) SetPhase(MatchPhase.InRound); }

        void OnEliminationRequested(int slot, EliminationReason reason)
        {
            if (!Roster[slot].IsActive) return; // exactly-once guard
            Roster.Eliminate(slot, reason, RoundIndex);
            Summary.EliminationOrder.Add(slot);
            Log.Add(Round.Time, $"ELIMINATED {Roster[slot].DisplayName} ({reason}) in round {RoundIndex + 1}");
            SlotEliminated?.Invoke(slot, reason, RoundIndex);
        }

        void OnRoundResolved(RoundEndCause cause)
        {
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                Summary.CumulativeScore[i] += Round.Scores[i].Total;
                Summary.CumulativeNoise[i] += Round.Scores[i].AttributedNoise;
            }
            foreach (var c in Round.Board.Chores)
                if (c.State == ChoreState.Completed) foreach (var st in c.Steps) if (ActorIds.IsSibling(st.CreditTo)) Summary.ChoresCompleted[st.CreditTo]++;
            if (Round.Recipes != null)
            {
                foreach (var inst in Round.Recipes.Instances) if (inst.Complete && ActorIds.IsSibling(inst.Owner)) Summary.RecipesCompleted[inst.Owner]++;
                var foiledActions = new HashSet<(int, int)>();
                foreach (var r in Round.Recipes.Sabotage) if (r.Foiled && ActorIds.IsSibling(r.FoiledBy) && foiledActions.Add((r.ActionId, r.FoiledBy))) Summary.Foils[r.FoiledBy]++;
                var res = Round.LastResolution; var v = Round.Verdict;
                if (res != null && res.Proven && v != null && ActorIds.IsSibling(v.Culprit))
                {
                    Summary.CaughtByEvidence[v.Culprit]++;
                    if (Round.Interro != null) foreach (var (seat, mid) in Round.Interro.Cards) if (Round.Belief.Memories[mid].SeenActor == v.Culprit && ActorIds.IsSibling(seat)) Summary.ProvingCards[seat]++;
                }
            }
            for (int i = 0; i < CompetitorRoster.SlotCount; i++) if (Roster[i].IsActive) Summary.RoundsSurvived[i]++;
            Summary.RoundStories.Add($"{Round.Room.Title}: {cause} — {(Round.Verdict != null ? Round.Verdict.Evidence : "")}");
            RoundEnded?.Invoke(Round);
            if (Continuous) { Summary.EndReason = cause == RoundEndCause.AllChoresDone ? "Every job was done - Mom came for the final check early." : "Time's up! Mom's final check."; FinishContinuous(); return; }
            SetPhase(MatchPhase.RoundOutro);
        }

        /// <summary>One decisive end: rank everybody (nobody was eliminated), one result, one summary, one award.</summary>
        void FinishContinuous()
        {
            Summary.Continuous = true; Summary.MatchSeconds = Round.PlayTime;
            var seats = new List<int>(); for (int i = 0; i < CompetitorRoster.SlotCount; i++) if (Roster[i].IsActive || Roster[i].IsHuman || Roster[i].IsBot) seats.Add(i);
            for (int i = 0; i < CompetitorRoster.SlotCount; i++) Summary.FinalScore[i] = Summary.CumulativeScore[i];
            float Progress(int s) => Round.Recipes != null ? Round.Recipes.OwnerProgress(s) + Round.Recipes.Help(s) : 0f;
            seats.Sort((a, b) =>
            {
                if (Math.Abs(Summary.FinalScore[a] - Summary.FinalScore[b]) > ScoreRules.TieEpsilon) return Summary.FinalScore[b].CompareTo(Summary.FinalScore[a]);
                float pa = Progress(a), pb = Progress(b); if (Math.Abs(pa - pb) > ScoreRules.TieEpsilon) return pb.CompareTo(pa);
                if (Math.Abs(Summary.CumulativeNoise[a] - Summary.CumulativeNoise[b]) > ScoreRules.TieEpsilon) return Summary.CumulativeNoise[a].CompareTo(Summary.CumulativeNoise[b]);
                float fa = Summary.FirstJobAt[a] < 0 ? float.MaxValue : Summary.FirstJobAt[a], fb = Summary.FirstJobAt[b] < 0 ? float.MaxValue : Summary.FirstJobAt[b];
                if (fa != fb) return fa.CompareTo(fb);
                return a.CompareTo(b);
            });
            Summary.Ranking.Clear(); Summary.Ranking.AddRange(seats);
            if (seats.Count > 0) { Summary.WinnerSlot = seats[0]; Summary.WinnerIsBot = Roster[seats[0]].IsBot; }
            for (int i = 0; i < CompetitorRoster.SlotCount; i++) Summary.RoundsSurvived[i] = 0; // no rounds in a continuous match
            Log.Add(Round.Time, $"MATCH OVER ({Summary.EndReason}) ranking: {string.Join(" > ", seats.ConvertAll(x => $"{Roster[x].DisplayName} {Summary.FinalScore[x]:0.0}"))}");
            SetPhase(MatchPhase.Winner);
            MatchFinished?.Invoke(Summary);
        }

        void FinishMatch()
        {
            var alive = Roster.ActiveIndices();
            if (alive.Count == 1)
            {
                Summary.WinnerSlot = alive[0];
                Summary.WinnerIsBot = Roster[alive[0]].IsBot;
                Log.Add(0, $"WINNER {Roster[alive[0]].DisplayName}{(Summary.WinnerIsBot ? " (BOT)" : "")}");
            }
            SetPhase(MatchPhase.Winner);
            MatchFinished?.Invoke(Summary);
        }

        // ------------------------------------------------------------------ connection policy (online host)
        void HumanDisconnectedImpl(int peerId, float now)
        {
            var s = Roster.FindByPeer(peerId);
            if (s == null) return;
            if (!Roster.Locked) { Roster.UnseatHumanPreMatch(peerId); return; }
            s.DisconnectedAt = now;
            if (Round != null) { int held = Round.HeldBy(s.Index); if (held >= 0) Round.Release(s.Index, held, 0f, false); Round.ReportTrackingLost(s.Index, true); }
            Log.Add(now, $"{s.DisplayName} disconnected — {DisconnectGrace:0}s grace");
        }

        bool HumanReconnectedImpl(int peerId, int slotIndex, string name, float now)
        {
            var s = Roster[slotIndex];
            if (s.IsHuman && s.PeerId == peerId && s.DisconnectedAt >= 0) { s.DisconnectedAt = -1; Round?.ReportTrackingLost(slotIndex, false); return true; }
            return Roster.TryReclaim(slotIndex, peerId, name);
        }

        float _clock;
        void CheckDisconnectGrace()
        {
            _clock = Round != null ? Round.Time : _clock;
            foreach (var s in Roster.Slots)
                if (s.IsHuman && s.DisconnectedAt >= 0 && _clock - s.DisconnectedAt >= DisconnectGrace)
                {
                    s.DisconnectedAt = -1;
                    Roster.BotTakeOver(s.Index, TakeoverDifficulty);
                    Log.Add(_clock, $"Seat {s.Index + 1} handed to a bot after disconnect");
                    BotTakeover?.Invoke(s.Index);
                }
        }
        public event Action<int> BotTakeover;

        void HostLeftImpl(string reason)
        {
            Log.Add(0, "MATCH CANCELLED: " + reason);
            SetPhase(MatchPhase.Cancelled);
        }

        /// <summary>Same party, new seed, fresh tasks/pets/meter.</summary>
        void PrepareRematchImpl(ulong newSeed)
        {
            Roster.ResetForRematch();
            MatchSeed = newSeed; RoundIndex = -1; Round = null;
            SetPhase(MatchPhase.Lobby);
        }

        // ------------------------------------------------------------------ journaled public API (V6 online replication)
        /// <summary>Online host: non-null => every outermost rules call is recorded for client replay (see MatchJournal).</summary>
        public MatchJournal Journal;
        JournalScope J(MatchJournal.Op op, out System.IO.BinaryWriter w) { var j = Journal; w = j != null && j.Begin(op) ? j.W : null; return new JournalScope(j); }
        float _checksumT;

        public bool Start(MatchMode mode, ulong seed, out string error)
        {
            using (J(MatchJournal.Op.MStart, out var w))
            {
                if (w != null)
                {
                    w.Write((byte)mode); w.Write(seed); w.Write(IntroSeconds); w.Write(OutroSeconds); w.Write(WinnerSeconds); w.Write(DisconnectGrace);
                    w.Write(WaitForRoomReady); w.Write((byte)TakeoverDifficulty);
                    w.Write((byte)Rounds.Length);
                    foreach (var c in Rounds) { w.Write((byte)c.Index); w.Write((byte)c.Room); w.Write(c.Duration); w.Write((byte)c.SharedTasks); w.Write(c.NoiseMultiplier); w.Write(c.MeterBaseline); w.Write(c.PetActivity); w.Write(c.CountdownSeconds); w.Write(c.RecipeMode); w.Write(c.Continuous); w.Write((byte)c.JobsPerSeat); w.Write(c.TimeoutSeconds); w.Write(c.PenaltyPoints); }
                    MatchJournal.WriteRoster(w, Roster);
                }
                return StartImpl(mode, seed, out error);
            }
        }
        public void Tick(float dt)
        {
            using (J(MatchJournal.Op.MTick, out var w)) { if (w != null) w.Write(dt); TickImpl(dt); }
            if (Journal != null && Journal.Recording) { _checksumT += dt; if (_checksumT >= 1f) { _checksumT = 0f; Journal.WriteChecksum(this); } }
        }
        public void SkipIntro() { using (J(MatchJournal.Op.MSkipIntro, out _)) SkipIntroImpl(); }
        public void NotifyRoomReady() { using (J(MatchJournal.Op.MRoomReady, out _)) NotifyRoomReadyImpl(); }
        public void HumanDisconnected(int peerId, float now) { using (J(MatchJournal.Op.MDisconnected, out var w)) { if (w != null) { w.Write(peerId); w.Write(now); } HumanDisconnectedImpl(peerId, now); } }
        public bool HumanReconnected(int peerId, int slotIndex, string name, float now) { using (J(MatchJournal.Op.MReconnected, out var w)) { if (w != null) { w.Write(peerId); w.Write(slotIndex); w.Write(name ?? ""); w.Write(now); } return HumanReconnectedImpl(peerId, slotIndex, name, now); } }
        public void HostLeft(string reason) { using (J(MatchJournal.Op.MHostLeft, out var w)) { if (w != null) w.Write(reason ?? ""); HostLeftImpl(reason); } }
        public void PrepareRematch(ulong newSeed) { using (J(MatchJournal.Op.MRematch, out var w)) { if (w != null) w.Write(newSeed); PrepareRematchImpl(newSeed); } }
    }
}
