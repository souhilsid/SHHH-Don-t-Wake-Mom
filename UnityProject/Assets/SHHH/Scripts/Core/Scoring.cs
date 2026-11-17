using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    [Serializable]
    public sealed class RoundScore
    {
        public int Slot;
        public float Shared;
        public float Bonus;
        /// <summary>V12 care / tidy points (pet fed, trash binned). Kept apart from Bonus, which SyncFromBoard rebuilds from the board.</summary>
        public float Care;
        public float Rescue;
        public float Saves;        // restored own item a rival took (counterplay)
        public float Caught;       // own sabotage that was foiled
        public float AttributedNoise;
        public float Inactivity;
        /// <summary>Continuous match: Mom's proven-disturbance penalties.</summary>
        public float Penalty;
        public int ValidatedSteps;
        /// <summary>Bonus = personal duty points (V5). </summary>
        public float Total => Shared + Bonus + Care + Rescue + Saves - Caught - ScoreRules.NoiseWeight * AttributedNoise - Inactivity - Penalty;
        public override string ToString() => $"slot{Slot}: total={Total:0.0} (duty {Bonus:0} care {Care:0.0} shared {Shared:0.0} rescue {Rescue:0} saves {Saves:0} caught -{Caught:0} noise {AttributedNoise:0.0} idle -{Inactivity:0})";
    }

    public static class ScoreRules
    {
        public const float NoiseWeight = 0.35f;
        public const float BonusCap = 12f;
        public const float RescuePoints = 5f;
        public const float RescueCap = 15f;
        public const float IdlePenalty = 10f;
        public const float IdleCap = 20f;
        public const float IdleWarnSeconds = 25f;
        public const float IdlePenaltySeconds = 35f;
        public const float TieEpsilon = 0.01f;
        public const float SavePoints = 4f;
        public const float CaughtPenalty = 8f;
    }

    public enum InspectionOutcome { Eliminate, TieBreakNeeded, NoCandidates }

    public sealed class InspectionResult
    {
        public InspectionOutcome Outcome;
        public int Eliminated = ActorIds.None;
        public readonly List<int> Tied = new List<int>();
        public readonly List<RoundScore> Ranking = new List<RoundScore>(); // best first
        public string Explanation = "";
    }

    /// <summary>Per-round authoritative score ledger. Never accepts client-reported scores.</summary>
    public sealed class ScoreLedger
    {
        readonly RoundScore[] _s = new RoundScore[CompetitorRoster.SlotCount];
        readonly float[] _lastActivity = new float[CompetitorRoster.SlotCount];
        readonly bool[] _idleWarned = new bool[CompetitorRoster.SlotCount];
        readonly bool[] _trackingLost = new bool[CompetitorRoster.SlotCount];
        readonly HashSet<string> _rescueKeys = new HashSet<string>();
        public event Action<int> IdleWarning;
        public event Action<int> IdlePenaltyApplied;

        public ScoreLedger() { Reset(0f); }

        public void Reset(float now)
        {
            for (int i = 0; i < _s.Length; i++) { _s[i] = new RoundScore { Slot = i }; _lastActivity[i] = now; _idleWarned[i] = false; _trackingLost[i] = false; }
            _rescueKeys.Clear();
        }

        public RoundScore this[int slot] => _s[slot];

        /// <summary>V7 recipe round: owner progress, help credit, foils and caught sabotage add to the same score fields.</summary>
        public RecipeBoard Recipes;

        public void SyncFromBoard(ChoreBoard board)
        {
            for (int i = 0; i < _s.Length; i++)
            {
                _s[i].Shared = board.SharedContribution(i);
                _s[i].ValidatedSteps = board.ValidatedSteps(i);
                _s[i].Bonus = board.PersonalContribution(i);
                _s[i].Saves = board.SaveCount(i) * ScoreRules.SavePoints;
                _s[i].Caught = board.CaughtCount(i) * ScoreRules.CaughtPenalty;
                if (Recipes != null)
                {
                    int slot = i;
                    _s[i].Bonus += Recipes.OwnerProgress(slot);
                    _s[i].Shared += Recipes.Help(slot);
                    foreach (var inst in Recipes.Instances) foreach (var c in inst.CreditedTo) if (c == slot) _s[i].ValidatedSteps++;
                    foreach (var sab in Recipes.Sabotage)
                    {
                        if (sab.Foiled && sab.FoiledBy == slot && sab.Victim == slot) _s[i].Saves += ScoreRules.SavePoints;
                        if (sab.Foiled && sab.Saboteur == slot) _s[i].Caught += ScoreRules.CaughtPenalty;
                    }
                }
            }
        }

        public void AddPenalty(int slot, float points) { if (ActorIds.IsSibling(slot)) _s[slot].Penalty += points; }
        /// <summary>V12 care / tidy bonus (feeding a pet, binning trash).</summary>
        public void AddBonus(int slot, float points) { if (ActorIds.IsSibling(slot)) _s[slot].Care += points; }

        public void AddNoise(Responsibility r, float value)
        {
            foreach (var kv in r.Weights) if (ActorIds.IsSibling(kv.Key)) _s[kv.Key].AttributedNoise += value * kv.Value;
        }

        /// <summary>Rescue credit (+5, cap 15/round). Key prevents repeated credit for the same danger; self-created danger never counts.</summary>
        public bool AddRescue(int slot, string dangerKey, int dangerCreator)
        {
            if (!ActorIds.IsSibling(slot) || slot == dangerCreator) return false;
            if (!_rescueKeys.Add(dangerKey)) return false;
            if (_s[slot].Rescue >= ScoreRules.RescueCap) return false;
            _s[slot].Rescue = Math.Min(ScoreRules.RescueCap, _s[slot].Rescue + ScoreRules.RescuePoints);
            return true;
        }

        public void ReportActivity(int slot, float now) { if (ActorIds.IsSibling(slot)) { _lastActivity[slot] = now; _idleWarned[slot] = false; } }
        public void SetTrackingLost(int slot, bool lost, float now) { if (!ActorIds.IsSibling(slot)) return; _trackingLost[slot] = lost; if (!lost) _lastActivity[slot] = now; }

        public void TickIdle(float now, IList<int> active)
        {
            foreach (int i in active)
            {
                if (_trackingLost[i]) { _lastActivity[i] = now; continue; }
                float idle = now - _lastActivity[i];
                if (!_idleWarned[i] && idle >= ScoreRules.IdleWarnSeconds) { _idleWarned[i] = true; IdleWarning?.Invoke(i); }
                if (idle >= ScoreRules.IdlePenaltySeconds)
                {
                    if (_s[i].Inactivity < ScoreRules.IdleCap)
                    {
                        _s[i].Inactivity = Math.Min(ScoreRules.IdleCap, _s[i].Inactivity + ScoreRules.IdlePenalty);
                        IdlePenaltyApplied?.Invoke(i);
                    }
                    _lastActivity[i] = now; _idleWarned[i] = false;
                }
            }
        }

        /// <summary>
        /// Quiet inspection: lowest total is eliminated. Ties: lower shared contribution loses, then higher attributed noise loses,
        /// then fewer validated steps loses; if still tied -> deterministic tie-break chore (never random, never client-chosen).
        /// </summary>
        public InspectionResult Inspect(IList<int> active)
        {
            var res = new InspectionResult();
            if (active.Count == 0) { res.Outcome = InspectionOutcome.NoCandidates; return res; }
            var list = new List<RoundScore>();
            foreach (int i in active) list.Add(_s[i]);
            list.Sort(Compare); // best first
            res.Ranking.AddRange(list);
            var worst = list[list.Count - 1];
            foreach (var s in list) if (Compare(s, worst) == 0) res.Tied.Add(s.Slot);
            if (res.Tied.Count > 1)
            {
                res.Outcome = InspectionOutcome.TieBreakNeeded;
                res.Explanation = "A dead heat at the bottom — tie-break challenge!";
                return res;
            }
            res.Tied.Clear();
            res.Outcome = InspectionOutcome.Eliminate;
            res.Eliminated = worst.Slot;
            var second = list.Count > 1 ? list[list.Count - 2] : null;
            if (second != null && Math.Abs(second.Total - worst.Total) <= ScoreRules.TieEpsilon)
            {
                if (Math.Abs(second.Bonus - worst.Bonus) > ScoreRules.TieEpsilon) res.Explanation = "Same score, but finished less of their own job";
                else if (Math.Abs(second.Shared - worst.Shared) > ScoreRules.TieEpsilon) res.Explanation = "Same score, but did the least shared chores";
                else if (Math.Abs(second.AttributedNoise - worst.AttributedNoise) > ScoreRules.TieEpsilon) res.Explanation = "Same score, but made more noise";
                else res.Explanation = "Same score, but finished fewer chore steps";
            }
            else res.Explanation = $"Least helpful tonight ({worst.Total:0} pts)";
            return res;
        }

        /// <summary>Best-first ordering. Returns 0 only for a true dead heat.</summary>
        public static int Compare(RoundScore a, RoundScore b)
        {
            if (Math.Abs(a.Total - b.Total) > ScoreRules.TieEpsilon) return b.Total.CompareTo(a.Total);
            if (Math.Abs(a.Bonus - b.Bonus) > ScoreRules.TieEpsilon) return b.Bonus.CompareTo(a.Bonus);
            if (Math.Abs(a.Shared - b.Shared) > ScoreRules.TieEpsilon) return b.Shared.CompareTo(a.Shared);
            if (Math.Abs(a.AttributedNoise - b.AttributedNoise) > ScoreRules.TieEpsilon) return a.AttributedNoise.CompareTo(b.AttributedNoise);
            if (a.ValidatedSteps != b.ValidatedSteps) return b.ValidatedSteps.CompareTo(a.ValidatedSteps);
            return 0;
        }
    }
}
