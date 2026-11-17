using System;

namespace SHHH.Core
{
    public enum MomState
    {
        Sleeping, Stirring, Suspicious, AlmostAwake,   // live during play, driven by the SleepMeter
        Waking, WalkingIn, Investigating, Inspecting,  // round frozen from here
        Interrogating,                                 // V7: the playable confrontation (cards / pointing / optional voice)
        Verdict, ReturningToBed, Offstage
    }

    public enum MomVerdictKind { None, Eliminate, Warning, TieBreak, Penalty }

    [Serializable]
    public sealed class MomVerdict
    {
        public MomVerdictKind Kind;
        public int Culprit = ActorIds.None;
        public EliminationReason Reason;
        public string Line = "";          // English VO/subtitle line id or text
        public string Evidence = "";      // explanation shown on the in-world evidence card
        public int DecisiveEventId = -1;
    }

    /// <summary>
    /// Mom's authoritative behaviour state. Gameplay decisions (culprit, verdict) come from the CausalLedger/ScoreLedger,
    /// never from an LLM or a client. The Unity MomCharacter animates whatever state this reports — it never teleports:
    /// she walks from her bedroom door along the nav path while this timeline runs.
    /// </summary>
    public sealed class MomDirector
    {
        public MomState State { get; private set; } = MomState.Sleeping;
        public float StateTime { get; private set; }
        public MomVerdict PendingVerdict { get; private set; }
        public bool IsInspection { get; private set; }
        public event Action<MomState, MomState> StateChanged;
        public event Action<string> Says; // dialogue line ids (see Docs/AUDIO_DESIGN.md script)

        // Presentation timings (seconds). Unity may shorten via SkipCinematics for repeat players.
        /// <summary>V7: run a playable interrogation between investigating and the verdict. The round supplies the timer and the resolver.</summary>
        public bool Interrogate;
        public Func<float, bool> InterrogationTick;   // (dt) -> true when the confrontation is over
        public Func<MomVerdict> ResolveVerdict;       // computed from Mom's belief ledger at the end of the confrontation
        public float WakingDuration = 2.2f, WalkInDuration = 3.2f, InvestigateDuration = 3.6f, InspectDuration = 4.6f, VerdictDuration = 4.2f, ReturnDuration = 2.8f;
        readonly DetRng _rng;
        float _lastStirLine = -99f;

        public MomDirector(ulong seed) { _rng = new DetRng(seed ^ 0xA11CE); }

        public bool RoundFrozen => State >= MomState.Waking && State <= MomState.ReturningToBed;

        public void ResetForRound() { Set(MomState.Sleeping); PendingVerdict = null; IsInspection = false; Interrogate = false; _startledUntil = -1f; }

        float _startledUntil = -1f;

        /// <summary>
        /// FUN-G3: a single loud crash gets an audible reaction from behind the door within the same frame
        /// ("Was that a plate?!") and Mom stays at least Stirring for 4 s, even if the meter itself is still low.
        /// </summary>
        public void ReactToLoud(float value, float now)
        {
            if (RoundFrozen || State == MomState.Offstage) return;
            _startledUntil = now + 4f;
            if (State < MomState.Stirring) Set(MomState.Stirring);
            if (now - _lastStirLine > 2.5f) { _lastStirLine = now; Says?.Invoke(value >= 30f ? Pick("mom_suspicious_", 4) : Pick("mom_stir_", 4)); }
        }

        public void UpdateFromMeter(SleepStage stage, float now)
        {
            if (RoundFrozen || State == MomState.Offstage) return;
            MomState want;
            switch (stage)
            {
                case SleepStage.DeepSleep: want = MomState.Sleeping; break;
                case SleepStage.Stirring: want = MomState.Stirring; break;
                case SleepStage.Suspicious: want = MomState.Suspicious; break;
                default: want = MomState.AlmostAwake; break;
            }
            if (now < _startledUntil && want < MomState.Stirring) want = MomState.Stirring;
            if (want != State)
            {
                bool rising = want > State;
                Set(want);
                if (rising && now - _lastStirLine > 6f)
                {
                    _lastStirLine = now;
                    Says?.Invoke(want == MomState.Stirring ? Pick("mom_stir_", 4) : want == MomState.Suspicious ? Pick("mom_suspicious_", 4) : Pick("mom_almost_", 3));
                }
            }
        }

        /// <summary>
        /// V6 pacing: the full wake sequence used to freeze play for ~16 s even for a mere warning (user: "slow, forced").
        /// Warnings now run at 55% length (~9 s), catches/inspections at 80%. Unity animates from the same state timings.
        /// </summary>
        public float Pace { get; private set; } = 1f;
        float D(float seconds) => seconds * Pace;

        /// <summary>Practice room: Mom only grumbles from behind her door - no freeze, no walk-in.</summary>
        public void Grumble() { Says?.Invoke(Pick("mom_warning_", 4)); Set(MomState.Sleeping); }

        public void BeginWake(MomVerdict verdict)
        {
            Pace = verdict != null && verdict.Kind == MomVerdictKind.Warning ? 0.55f : 0.8f;
            PendingVerdict = verdict; IsInspection = false;
            Set(MomState.Waking); Says?.Invoke(Pick("mom_wake_", 4));
        }

        public void BeginInspection(MomVerdict verdict)
        {
            Pace = 0.8f;
            PendingVerdict = verdict; IsInspection = true;
            Set(MomState.Waking); Says?.Invoke(Pick("mom_inspect_start_", 3));
        }

        /// <returns>true exactly once, when the verdict moment is reached (apply elimination/warning then).</returns>
        public bool Tick(float dt, out bool sequenceFinished)
        {
            sequenceFinished = false;
            StateTime += dt;
            switch (State)
            {
                case MomState.Waking: if (StateTime >= D(WakingDuration)) Set(MomState.WalkingIn); break;
                case MomState.WalkingIn:
                    if (StateTime >= D(WalkInDuration)) { Set(IsInspection ? MomState.Inspecting : MomState.Investigating); Says?.Invoke(IsInspection ? Pick("mom_inspect_look_", 3) : Pick("mom_investigate_", 4)); }
                    break;
                case MomState.Investigating:
                case MomState.Inspecting:
                    if (StateTime >= D(State == MomState.Inspecting ? InspectDuration : InvestigateDuration))
                    {
                        if (Interrogate) { Set(MomState.Interrogating); Says?.Invoke(Pick("mom_question_", 3)); break; }
                        return EnterVerdict();
                    }
                    break;
                case MomState.Interrogating:
                    if (InterrogationTick == null || InterrogationTick(dt))
                    {
                        if (ResolveVerdict != null) { var rv = ResolveVerdict(); if (rv != null) PendingVerdict = rv; }
                        return EnterVerdict();
                    }
                    break;
                case MomState.Verdict: if (StateTime >= D(VerdictDuration)) { Set(MomState.ReturningToBed); Says?.Invoke(Pick("mom_bed_", 3)); } break;
                case MomState.ReturningToBed: if (StateTime >= D(ReturnDuration)) { Set(MomState.Sleeping); sequenceFinished = true; } break;
            }
            return false;
        }

        bool EnterVerdict()
        {
                        Set(MomState.Verdict);
                        var v = PendingVerdict;
                        if (v != null) Says?.Invoke(v.Kind == MomVerdictKind.Eliminate ? (IsInspection ? Pick("mom_lazy_", 4) : Pick("mom_caught_", 5))
                                                   : v.Kind == MomVerdictKind.Penalty ? Pick("mom_penalty_", 4)   // continuous match: proven -> points + short timeout
                                                   : v.Kind == MomVerdictKind.TieBreak ? "mom_tiebreak_1" : Pick("mom_warning_", 4));
                        return true;
        }

        public void GoOffstage() => Set(MomState.Offstage);

        void Set(MomState s)
        {
            var old = State; State = s; StateTime = 0f;
            if (old != s) StateChanged?.Invoke(old, s);
        }

        string Pick(string prefix, int count) => prefix + (1 + _rng.Range(0, count));
    }
}
