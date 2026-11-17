using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    /// <summary>A live recipe: its owner, its props/fixtures in the shared world, and per-step state/credit.</summary>
    public sealed class RecipeInstance
    {
        public int Index; public RecipeDef Def; public int Owner;
        public readonly Dictionary<string, List<int>> PropsByKind = new Dictionary<string, List<int>>();
        public readonly Dictionary<string, int> FixtureByKind = new Dictionary<string, int>();
        public bool[] Satisfied; public bool[] Latched; public int[] CreditedTo; public int[] LastUndoneBy; public float[] UndoneAt;
        public bool Complete; public int CompletedBy = ActorIds.None; public float CompletedAt = -1;
        public readonly HashSet<int> Helpers = new HashSet<int>();
        public float ProtectedUntil = -1f;
        public int StepsDone => Satisfied.Count(x => x);
        public List<int> Props(string kind) => kind != null && PropsByKind.TryGetValue(kind, out var l) ? l : new List<int>();
        public int Fixture(string kind) => kind != null && FixtureByKind.TryGetValue(kind, out var f) ? f : -1;
    }

    public sealed class RecipeSabotage
    {
        public int Saboteur, Victim, Instance, Step, ActionId; public float Time; public long EventId; public string Evidence;
        public bool Foiled; public int FoiledBy = ActorIds.None; public float RepairedAt = -1;
        public override string ToString() => $"{Saboteur}->{Victim} r{Instance} step{Step} t{Time:0.0} {(Foiled ? "foiled by " + FoiledBy : Evidence)}";
    }

    /// <summary>
    /// Authoritative recipe board over a <see cref="RecipeWorld"/>. Completion is evaluated ONLY from physical state
    /// predicates; credit follows the actor whose accepted event made a step true; reversal by a rival sibling is a sabotage
    /// record (with evidence), by a pet a non-punitive incident; repairs within the foil window foil the sabotage.
    /// Anti-exploit: credit once per step, no credit for undoing-and-redoing your own work, helper credit capped per round,
    /// per-target sabotage cooldown and interference limit, short repair protection, spectators rejected by the world.
    /// </summary>
    public sealed class RecipeBoard
    {
        public const int PointsPerStep = 15, HelpPoints = 5, HelpCapPerRound = 10;
        public const float FoilWindow = 20f, SabotageCooldown = 20f, RepairProtection = 8f;
        /// <summary>Ops by the same actor on the same chore within this window form ONE sabotage action (open lid + take block).</summary>
        public const float ActionWindow = 3f;
        public const int MaxSabotagePerTargetPerRound = 2;

        public readonly RecipeWorld World = new RecipeWorld();
        public readonly List<RecipeInstance> Instances = new List<RecipeInstance>();
        public readonly List<RecipeSabotage> Sabotage = new List<RecipeSabotage>();
        public readonly List<REvent> PetIncidents = new List<REvent>();
        public readonly Dictionary<int, int> HelpEarned = new Dictionary<int, int>();
        public readonly Dictionary<int, int> SelfMessCount = new Dictionary<int, int>();   // telemetry: farming attempts
        public int RejectedSabotage;                                                        // telemetry: blocked griefing
        public event Action<RecipeInstance, int> Completed;
        public event Action<RecipeInstance> Reopened;
        public event Action<RecipeSabotage> Sabotaged;
        public event Action<RecipeSabotage> Foiled;

        public RecipeBoard() { World.Accepted += OnAccepted; }

        public RecipeInstance Add(RecipeDef def, int owner)
        {
            var inst = new RecipeInstance { Index = Instances.Count, Def = def, Owner = owner };
            foreach (var fd in def.Fixtures) inst.FixtureByKind[fd.Kind] = World.AddFixture(fd, inst.Index);
            foreach (var pd in def.Props)
            {
                if (!inst.PropsByKind.TryGetValue(pd.Kind, out var list)) inst.PropsByKind[pd.Kind] = list = new List<int>();
                int start = list.Count;
                for (int i = 0; i < pd.Count; i++) list.Add(World.AddProp(pd, start + i, inst.Index));
            }
            foreach (var pd in def.Props.Where(d => !string.IsNullOrEmpty(d.StartIn)))
            {
                var f = World.Fixture(inst.Fixture(pd.StartIn));
                foreach (var id in inst.Props(pd.Kind))
                {
                    var p = World.Prop(id); if (p.IsPlaced) continue;
                    int k = Array.IndexOf(f.Occupant, -1); if (k < 0) break;
                    f.Occupant[k] = id; p.Fixture = f.Id; p.Slot = k; p.Spot = f.Kind;
                }
            }
            int n = def.Steps.Count;
            inst.Satisfied = new bool[n]; inst.Latched = new bool[n]; inst.CreditedTo = Enumerable.Repeat(ActorIds.None, n).ToArray();
            inst.LastUndoneBy = Enumerable.Repeat(ActorIds.None, n).ToArray(); inst.UndoneAt = new float[n];
            Instances.Add(inst);
            Evaluate(inst, out _);
            return inst;
        }

        // ------------------------------------------------------------------ predicates
        public bool Predicate(RecipeInstance inst, int stepIndex)
        {
            var s = inst.Def.Steps[stepIndex]; var w = World;
            switch (s.Kind)
            {
                case StepKind.Place:
                {
                    var f = w.Fixture(inst.Fixture(s.Fixture)); if (f == null) return false;
                    var props = inst.Props(s.PropKind); int need = s.Count < 0 ? props.Count : s.Count, ok = 0;
                    foreach (var id in props)
                    {
                        var p = w.Prop(id);
                        if (p.Fixture != f.Id || p.HeldBy != ActorIds.None) continue;
                        if (s.RequireOriented && !p.Oriented) continue;
                        if (s.Rule == SlotRule.MatchTag && !string.IsNullOrEmpty(f.SlotTags[p.Slot]) && f.SlotTags[p.Slot] != p.Tag) continue;
                        if (s.Rule == SlotRule.InOrder && p.Slot != p.Order) continue;
                        ok++;
                    }
                    return ok >= need;
                }
                case StepKind.OpenState: { var f = w.Fixture(inst.Fixture(s.Fixture)); return f != null && f.Open; }
                case StepKind.CloseState: { var f = w.Fixture(inst.Fixture(s.Fixture)); return f != null && !f.Open; }
                case StepKind.Strokes: { var f = w.Fixture(inst.Fixture(s.Fixture)); return f != null && f.Decals == 0; }
                case StepKind.Flag: { var f = w.Fixture(inst.Fixture(s.Fixture)); return f != null && f.Flags.Contains(s.FlagName); }
                case StepKind.Stage:
                {
                    var props = inst.Props(s.PropKind); int need = s.Count < 0 ? props.Count : s.Count;
                    return props.Count(id => w.Prop(id).Stage >= s.StageTarget) >= need;
                }
                case StepKind.Paired:
                    return inst.Props(s.PropKind).All(id => { var p = w.Prop(id); return p.Partner >= 0 && w.Prop(p.Partner).Tag == p.Tag; });
                case StepKind.Attached:
                    return inst.Props(s.PropKind).All(id => { var p = w.Prop(id); return p.AttachedTo >= 0 && TagFits(p, w.Prop(p.AttachedTo), s.PartnerKind); });
            }
            return false;
        }

        /// <summary>Attachment target fits: right kind, and the same tag when both carry one (lid "big" on pot "big").</summary>
        public static bool TagFits(RProp p, RProp partner, string partnerKind)
            => partner.Kind == partnerKind && (string.IsNullOrEmpty(p.Tag) || string.IsNullOrEmpty(partner.Tag) || p.Tag == partner.Tag);

        /// <summary>Transitional steps (opening a lid/door to work) must physically happen once; they stay latched afterwards.</summary>
        public static bool IsTransitional(RecipeStepDef s) => s.Kind == StepKind.OpenState;

        bool StepOk(RecipeInstance inst, int i, bool[] raw)
        {
            var s = inst.Def.Steps[i];
            bool own = IsTransitional(s) ? (raw[i] || inst.Latched[i]) : raw[i];
            if (!own) return false;
            foreach (var a in s.After) if (!StepOk(inst, a, raw)) return false;
            return true;
        }

        void Evaluate(RecipeInstance inst, out bool[] before)
        {
            before = (bool[])inst.Satisfied.Clone();
            int n = inst.Def.Steps.Count; var raw = new bool[n];
            for (int i = 0; i < n; i++) raw[i] = Predicate(inst, i);
            for (int i = 0; i < n; i++) if (IsTransitional(inst.Def.Steps[i]) && raw[i]) inst.Latched[i] = true;
            for (int i = 0; i < n; i++) inst.Satisfied[i] = StepOk(inst, i, raw);
        }

        // ------------------------------------------------------------------ authoritative request path
        /// <summary>Validate fairness rules, then apply to the world. Returns null when accepted.</summary>
        public string Request(ROp op)
        {
            // trial-apply to see whether this op would reverse someone else's satisfied step (sabotage)
            var snap = World.Snapshot(); var ledgerCount = World.Ledger.Count;
            var victimSteps = new List<(RecipeInstance inst, int step)>();
            _suspend = true;
            var err = World.Apply(op);
            _suspend = false;
            if (err != null) return err;
            var e = World.Ledger[World.Ledger.Count - 1];
            foreach (var inst in AffectedBy(e))
            {
                int n = inst.Def.Steps.Count; var raw = new bool[n];
                for (int i = 0; i < n; i++) raw[i] = Predicate(inst, i);
                for (int i = 0; i < n; i++) if (inst.Satisfied[i] && !StepOkTrial(inst, i, raw)) victimSteps.Add((inst, i));
            }
            string block = null;
            if (ActorIds.IsSibling(op.Actor))
                foreach (var (inst, step) in victimSteps)
                {
                    if (inst.Owner == op.Actor) continue;
                    if (World.Time < inst.ProtectedUntil) { block = "just repaired - give them a moment"; break; }
                    var last = Sabotage.LastOrDefault(x => x.Saboteur == op.Actor && x.Instance == inst.Index);
                    if (last != null && World.Time - last.Time <= ActionWindow) continue;           // same sabotage action, still in progress
                    int actions = Sabotage.Where(x => x.Saboteur == op.Actor && x.Instance == inst.Index).Select(x => x.ActionId).Distinct().Count();
                    if (actions >= MaxSabotagePerTargetPerRound) { block = "you've messed with this one enough"; break; }
                    if (last != null && World.Time - last.Time < SabotageCooldown) { block = "too soon to mess with it again"; break; }
                }
            // roll back the trial; then either reject or re-apply for real (same event id stays unused when rejected)
            World.Restore(snap); World.Ledger.RemoveRange(ledgerCount, World.Ledger.Count - ledgerCount); World.ForgetEvent(e.EventId);
            if (block != null) { RejectedSabotage++; return block; }
            op.EventId = e.EventId;
            return World.Apply(op);
        }

        bool StepOkTrial(RecipeInstance inst, int i, bool[] raw)
        {
            var s = inst.Def.Steps[i];
            bool own = IsTransitional(s) ? (raw[i] || inst.Latched[i]) : raw[i];
            if (!own) return false;
            foreach (var a in s.After) if (!StepOkTrial(inst, a, raw)) return false;
            return true;
        }

        bool _suspend; int _actionSeq;
        IEnumerable<RecipeInstance> AffectedBy(REvent e)
        {
            if (e.RecipeInstance >= 0) yield return Instances[e.RecipeInstance];
            // a prop can sit in another recipe's fixture (cross-recipe clutter): include that owner too
            var p = World.Prop(e.Prop);
            if (p != null && p.Fixture >= 0) { var fi = World.Fixture(p.Fixture).RecipeInstance; if (fi != e.RecipeInstance && fi >= 0) yield return Instances[fi]; }
        }

        void OnAccepted(REvent e)
        {
            if (_suspend) return;
            foreach (var inst in AffectedBy(e).ToList()) Attribute(inst, e);
        }

        void Attribute(RecipeInstance inst, REvent e)
        {
            Evaluate(inst, out var before);
            int actor = e.Actor; float t = World.Time;
            for (int i = 0; i < before.Length; i++)
            {
                bool now = inst.Satisfied[i];
                if (!before[i] && now)
                {
                    bool selfRedo = inst.LastUndoneBy[i] == actor && ActorIds.IsSibling(actor);
                    if (selfRedo) { SelfMessCount[actor] = (SelfMessCount.TryGetValue(actor, out var c) ? c : 0) + 1; }
                    else if (inst.CreditedTo[i] == ActorIds.None && ActorIds.IsSibling(actor)) inst.CreditedTo[i] = actor;
                    if (ActorIds.IsSibling(actor) && actor != inst.Owner && !selfRedo) inst.Helpers.Add(actor);
                    // repair of a rival's sabotage within the window -> foiled
                    var sab = Sabotage.LastOrDefault(x => x.Instance == inst.Index && x.Step == i && !x.Foiled && x.RepairedAt < 0);
                    if (sab != null)
                    {
                        sab.RepairedAt = t;
                        if (actor != sab.Saboteur && t - sab.Time <= FoilWindow) { sab.Foiled = true; sab.FoiledBy = actor; Foiled?.Invoke(sab); }
                        inst.ProtectedUntil = t + RepairProtection;
                    }
                    inst.LastUndoneBy[i] = ActorIds.None;
                }
                else if (before[i] && !now)
                {
                    inst.LastUndoneBy[i] = actor; inst.UndoneAt[i] = t;
                    if (ActorIds.IsPet(actor)) PetIncidents.Add(e);
                    else if (ActorIds.IsSibling(actor) && actor != inst.Owner)
                    {
                        var p = World.Prop(e.Prop);
                        var prev = Sabotage.LastOrDefault(x => x.Saboteur == actor && x.Instance == inst.Index);
                        int action = prev != null && t - prev.Time <= ActionWindow ? prev.ActionId : ++_actionSeq;
                        var rec = new RecipeSabotage { ActionId = action, Saboteur = actor, Victim = inst.Owner, Instance = inst.Index, Step = i, Time = t, EventId = e.EventId,
                            Evidence = p != null ? $"{p.Kind} moved to {p.Spot}" : $"{inst.Def.Steps[i].Fixture} disturbed" };
                        Sabotage.Add(rec); Sabotaged?.Invoke(rec);
                    }
                }
            }
            bool all = inst.Satisfied.All(x => x);
            if (all && !inst.Complete)
            {
                inst.Complete = true; inst.CompletedBy = actor; inst.CompletedAt = t;
                foreach (var h in inst.Helpers)
                {
                    if (Sabotage.Any(x => x.Saboteur == h && x.Instance == inst.Index)) continue;  // no help credit for your own mess
                    int have = HelpEarned.TryGetValue(h, out var v) ? v : 0;
                    HelpEarned[h] = Math.Min(HelpCapPerRound, have + HelpPoints);
                }
                Completed?.Invoke(inst, actor);
            }
            else if (!all && inst.Complete) { inst.Complete = false; Reopened?.Invoke(inst); }
        }

        /// <summary>Planning copy (same recipes, owners, physical state and latches; no sabotage history or events).</summary>
        public RecipeBoard Clone()
        {
            var c = new RecipeBoard();
            foreach (var i in Instances) c.Add(i.Def, i.Owner);
            c.World.Time = World.Time; c.Restore(Snapshot());
            return c;
        }

        /// <summary>Join/rejoin snapshot: physical world + per-instance latches (steps that physically happened, e.g. "opened the lid").</summary>
        public string Snapshot()
        {
            var sb = new System.Text.StringBuilder(World.Snapshot()).Append("#L");
            foreach (var i in Instances) { sb.Append(';'); foreach (var l in i.Latched) sb.Append(l ? '1' : '0'); }
            return sb.ToString();
        }

        public void Restore(string snap)
        {
            int k = snap.IndexOf("#L", StringComparison.Ordinal);
            World.Restore(snap.Substring(0, k));
            var parts = snap.Substring(k + 2).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Instances.Count; i++)
            {
                var inst = Instances[i];
                for (int s = 0; s < inst.Latched.Length; s++) inst.Latched[s] = parts[i][s] == '1';
                Evaluate(inst, out _);
                inst.Complete = inst.Satisfied.All(x => x);
            }
        }

        /// <summary>Owner progress for scoring/verdict: validated steps x points (helpers' work counts for the owner's chore).</summary>
        /// <summary>
        /// Owner points: every recipe is worth <see cref="RecipePoints"/> in total whatever its length (fair across 2-5 step recipes),
        /// split evenly per satisfied step, plus a speed bonus up to <see cref="SpeedBonus"/> for finishing early (decays to 0 at
        /// three times the recipe's estimated duration). Keeps partial credit; a sabotaged step stops counting until repaired.
        /// </summary>
        public float OwnerProgress(int seat)
        {
            float sum = 0;
            foreach (var i in Instances)
            {
                if (i.Owner != seat) continue;
                sum += RecipePoints * DifficultyWeight(i.Def.Difficulty) * i.StepsDone / Math.Max(1, i.Def.Steps.Count);
                if (i.Complete && i.CompletedAt >= 0) sum += SpeedBonus * Math.Max(0f, Math.Min(1f, 1f - i.CompletedAt / (3f * Math.Max(10f, i.Def.DurationEstimate))));
            }
            return sum;
        }
        public const float RecipePoints = 60f, SpeedBonus = 15f;
        /// <summary>V13: harder jobs are worth more (owner gets the big physical jobs; easy 48 / medium 60 / hard 78 points).</summary>
        public static float DifficultyWeight(RecipeDifficulty d) => d == RecipeDifficulty.Easy ? 0.8f : d == RecipeDifficulty.Hard ? 1.3f : 1f;
        public int Help(int seat) => HelpEarned.TryGetValue(seat, out var v) ? v : 0;
    }
}
