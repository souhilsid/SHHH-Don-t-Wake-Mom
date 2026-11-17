using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    /// <summary>
    /// Plans and executes the primitive operations that bring a recipe from its CURRENT physical state to complete.
    /// Used for (1) per-recipe automated tests, (2) generation-time reachability validation, (3) bots: the motor executes the
    /// same verbs physically (spec §07: 100% of recipes have a bot step mapping), (4) repair after sabotage (same planner).
    /// Every op goes through <see cref="RecipeBoard.Request"/>, so bots obey exactly the human rules.
    /// </summary>
    public static class RecipeSolver
    {
        public sealed class Result { public bool Complete; public int Ops; public string Failure; public readonly List<Verb> Verbs = new List<Verb>(); }

        /// <summary>The first op the solver would take for this actor, planned on a copy (the real board is untouched). Null when
        /// the recipe is complete or no plan exists. Bots use this one op at a time and perform it with their bodies.</summary>
        public static ROp? NextOp(RecipeBoard b, RecipeInstance inst, int actor)
        {
            var plan = Plan(b, inst, actor);
            return plan.Count > 0 ? plan[0] : (ROp?)null;
        }

        /// <summary>The whole accepted op sequence that completes the recipe from the current state (planned on a copy). Executors
        /// should follow it in order and re-plan only when an op fails or the world changes under them (replanning after every op
        /// can oscillate between steps that share items, e.g. "all cars on the shelf" vs "cars in order").</summary>
        public static List<ROp> Plan(RecipeBoard b, RecipeInstance inst, int actor)
        {
            var ops = new List<ROp>(); if (inst.Complete) return ops;
            var c = b.Clone();
            Solve(c, c.Instances[inst.Index], actor, 120, op => { var e = c.Request(op); if (e == null) { op.EventId = 0; ops.Add(op); } return e; });
            return ops;
        }

        public static Result Solve(RecipeBoard b, RecipeInstance inst, int actor, int maxOps = 120, Func<ROp, string> exec = null)
        {
            exec = exec ?? b.Request;
            var r = new Result(); var w = b.World;
            string Do(ROp op)
            {
                op.Actor = actor; if (op.Prop2 == 0 && op.Verb != Verb.Pair && op.Verb != Verb.Attach) op.Prop2 = -1;
                var e = exec(op); r.Ops++; if (e == null) r.Verbs.Add(op.Verb); else r.Failure = $"{op} -> {e}"; return e;
            }
            ROp Op(Verb v, int prop = -1, int fixture = -1, int slot = -1, string arg = null, int prop2 = -1) => new ROp { Verb = v, Prop = prop, Fixture = fixture, Slot = slot, Arg = arg, Prop2 = prop2 };

            void FreeHands(int keep)
            {
                foreach (var p in w.Props.Where(p => p.HeldBy == actor && p.Id != keep).ToList()) Do(Op(Verb.PutDown, p.Id, arg: "staged"));
            }
            bool Hold(RProp p)
            {
                if (p.HeldBy == actor) return true;
                if (p.HeldBy != ActorIds.None) return false;
                FreeHands(p.Id);
                if (p.IsPlaced) { var pf = w.Fixture(p.Fixture); if (pf.HasDoor && !pf.Open && Do(Op(Verb.Open, fixture: pf.Id)) != null) return false; }
                return Do(Op(Verb.PickUp, p.Id)) == null;
            }

            int guard = 0;
            while (!inst.Complete && r.Ops < maxOps && guard++ < 200)
            {
                int i = FirstOpen(b, inst);
                if (i < 0) break;
                var s = inst.Def.Steps[i];
                var f = w.Fixture(inst.Fixture(s.Fixture));
                int opsBefore = r.Ops;
                switch (s.Kind)
                {
                    case StepKind.OpenState: Do(Op(Verb.Open, fixture: f.Id)); break;
                    case StepKind.CloseState: FreeHands(-1); Do(Op(Verb.Close, fixture: f.Id)); break;
                    case StepKind.Strokes: while (f.Decals > 0 && r.Ops < maxOps) if (Do(Op(Verb.Stroke, fixture: f.Id)) != null) break; break;
                    case StepKind.Flag: Do(Op(Verb.Press, fixture: f.Id, arg: s.FlagName)); break;
                    case StepKind.Stage:
                        // a stage done AT a fixture (rinse under the tap): the tap must be running
                        if (f != null && f.HasDoor && !f.Open && Do(Op(Verb.Open, fixture: f.Id)) != null) break;
                        foreach (var id in inst.Props(s.PropKind))
                        {
                            var p = w.Prop(id); if (p.Stage >= s.StageTarget) continue;
                            if (!p.IsPlaced && !Hold(p)) continue;
                            if (p.StrokeCap > 0 && p.Dirt != 0 && s.Verb == Verb.Stroke)
                            {
                                // V10: wash the stains off with the sponge (soaped by the earlier step) held in the other hand
                                var sp = w.Props.FirstOrDefault(x => x.Kind == "sponge" && x.RecipeInstance == p.RecipeInstance);
                                if (sp == null) continue;
                                if (sp.HeldBy != actor) { if (sp.HeldBy != ActorIds.None || Do(Op(Verb.PickUp, sp.Id)) != null) continue; }
                                var e = Do(new ROp { Actor = actor, Verb = Verb.Scrub, Prop = p.Id, Prop2 = sp.Id, Fixture = -1, Slot = -1, Arg = p.Dirt.ToString("X") });
                                if (sp.HeldBy == actor) Do(Op(Verb.PutDown, sp.Id, arg: "done"));
                                if (e != null) continue;
                            }
                            while (p.Stage < s.StageTarget && r.Ops < maxOps) if (Do(Op(s.Verb, p.Id, fixture: f != null ? f.Id : -1)) != null) break;
                            if (p.HeldBy == actor) Do(Op(Verb.PutDown, p.Id, arg: "done"));
                        }
                        break;
                    case StepKind.Paired:
                        foreach (var id in inst.Props(s.PropKind))
                        {
                            var p = w.Prop(id);
                            if (p.Partner >= 0 && w.Prop(p.Partner).Tag == p.Tag) continue;
                            if (p.Partner >= 0) Do(Op(Verb.Unpair, p.Id));
                            var q = inst.Props(s.PropKind).Select(w.Prop).FirstOrDefault(x => x != p && x.Tag == p.Tag && x.Partner < 0);
                            if (q == null || !Hold(p)) continue;
                            Do(Op(Verb.Pair, p.Id, prop2: q.Id)); Do(Op(Verb.PutDown, p.Id, arg: "paired"));
                        }
                        break;
                    case StepKind.Attached:
                        foreach (var id in inst.Props(s.PropKind))
                        {
                            var p = w.Prop(id);
                            if (p.AttachedTo >= 0 && RecipeBoard.TagFits(p, w.Prop(p.AttachedTo), s.PartnerKind)) continue;
                            if (p.AttachedTo >= 0) Do(Op(Verb.Detach, p.Id));
                            var q = inst.Props(s.PartnerKind).Select(w.Prop).FirstOrDefault(x => RecipeBoard.TagFits(p, x, s.PartnerKind));
                            if (q == null || !Hold(p)) continue;
                            Do(Op(Verb.Attach, p.Id, prop2: q.Id)); if (p.HeldBy == actor) Do(Op(Verb.PutDown, p.Id, arg: "assembled"));
                        }
                        break;
                    case StepKind.Place: SolvePlace(b, inst, s, f, actor, Do, Hold, Op); break;
                }
                if (r.Ops == opsBefore) { r.Failure = r.Failure ?? $"no progress on step {i} ({s.Label})"; break; }
            }
            FreeHands(-1);
            r.Complete = inst.Complete;
            if (!r.Complete && r.Failure == null) r.Failure = "op budget exhausted";
            return r;
        }

        static int FirstOpen(RecipeBoard b, RecipeInstance inst)
        {
            // first unsatisfied step whose prerequisites are satisfied (process dependencies first)
            for (int i = 0; i < inst.Satisfied.Length; i++)
                if (!inst.Satisfied[i] && inst.Def.Steps[i].After.All(a => inst.Satisfied[a])) return i;
            for (int i = 0; i < inst.Satisfied.Length; i++) if (!inst.Satisfied[i]) return i;
            return -1;
        }

        static void SolvePlace(RecipeBoard b, RecipeInstance inst, RecipeStepDef s, RFixture f, int actor, Func<ROp, string> Do, Func<RProp, bool> Hold, Func<Verb, int, int, int, string, int, ROp> Op)
        {
            var w = b.World;
            if (f.HasDoor && !f.Open && Do(Op(Verb.Open, -1, f.Id, -1, null, -1)) != null) return;
            var props = inst.Props(s.PropKind).Select(w.Prop).ToList();
            int need = s.Count < 0 ? props.Count : s.Count;
            bool Correct(RProp p) => p.Fixture == f.Id && p.HeldBy == ActorIds.None && (!s.RequireOriented || p.Oriented)
                && (s.Rule != SlotRule.MatchTag || string.IsNullOrEmpty(f.SlotTags[p.Slot]) || f.SlotTags[p.Slot] == p.Tag)
                && (s.Rule != SlotRule.InOrder || p.Slot == p.Order);
            // placed but only mis-oriented: straighten in place
            foreach (var p in props.Where(p => p.Fixture == f.Id && s.RequireOriented && !p.Oriented)) Do(Op(Verb.Align, p.Id, -1, -1, null, -1));
            int correct = props.Count(Correct);
            for (int k = 0; k < f.Occupant.Length && correct < need; k++)
            {
                var occ = w.Prop(f.Occupant[k]);
                if (occ != null && props.Contains(occ) && Correct(occ)) continue;
                string want = s.Rule == SlotRule.MatchTag ? f.SlotTags[k] : null;
                // candidates: loose props first (never strip a finished placement of another step), placed elsewhere last;
                // for an "Any" placement a naive player order is used (reverse declaration) so later "order/match" steps are real work
                var pool = props.Where(p => !Correct(p) && p.HeldBy != ActorIds.None == (p.HeldBy == actor)
                    && (s.Rule != SlotRule.MatchTag || string.IsNullOrEmpty(want) || p.Tag == want)
                    && (s.Rule != SlotRule.InOrder || p.Order == k)).OrderBy(p => p.IsPlaced && p.Fixture != f.Id ? 1 : 0).ToList();
                if (s.Rule == SlotRule.Any) pool = pool.OrderBy(p => p.IsPlaced && p.Fixture != f.Id ? 1 : 0).ThenByDescending(p => p.Id).ToList();
                if (pool.Any(p => !p.IsPlaced || p.Fixture == f.Id)) pool = pool.Where(p => !p.IsPlaced || p.Fixture == f.Id).ToList();
                var cand = pool.FirstOrDefault();
                if (cand == null) continue;
                if (occ != null) { if (!Hold(occ)) continue; Do(Op(Verb.PutDown, occ.Id, -1, -1, "floor", -1)); }
                if (!Hold(cand)) continue;
                // place first, then straighten in the slot (physically the slot's snap squares an item that arrives upright and square)
                if (Do(Op(Verb.PlaceInSocket, cand.Id, f.Id, k, null, -1)) == null)
                {
                    if (s.RequireOriented && !cand.Oriented) Do(Op(Verb.Align, cand.Id, -1, -1, null, -1));
                    correct++;
                }
            }
        }

        /// <summary>Apply one sabotage variant as a rival would (physical ops through the board). Returns null when it took effect.</summary>
        public static string ApplySabotage(RecipeBoard b, RecipeInstance inst, SabotageVariant v, int saboteur, Func<ROp, string> exec = null)
        {
            var w = b.World; exec = exec ?? b.Request;
            string Do(ROp op) { op.Actor = saboteur; if (op.Prop2 == 0) op.Prop2 = -1; if (op.Slot == 0 && op.Verb != Verb.PlaceInSocket) op.Slot = -1; return exec(op); }
            var props = inst.PropsByKind.Values.SelectMany(x => x).Select(w.Prop).ToList();
            var placed = props.Where(p => p.IsPlaced).ToList();
            var doorFix = inst.FixtureByKind.Values.Select(w.Fixture).FirstOrDefault(f => f.HasDoor);
            switch (v.Kind)
            {
                case SabotageKind.Remove:
                {
                    var p = placed.FirstOrDefault(); if (p == null) return "nothing placed";
                    var f = w.Fixture(p.Fixture);
                    if (f.HasDoor && !f.Open && f.Flags.Contains("start")) { var es = Do(new ROp { Verb = Verb.Press, Fixture = f.Id, Prop = -1, Arg = "!start" }); if (es != null) return es; }   // V12: stop the machine to unlock its door
                    if (f.HasDoor && !f.Open) { var e0 = Do(new ROp { Verb = Verb.Open, Fixture = f.Id, Prop = -1 }); if (e0 != null) return e0; }
                    var e = Do(new ROp { Verb = Verb.PickUp, Prop = p.Id, Fixture = -1 }); if (e != null) return e;
                    return Do(new ROp { Verb = Verb.PutDown, Prop = p.Id, Fixture = -1, Arg = v.HideSpot });
                }
                case SabotageKind.Swap:
                {
                    RProp a = null, c = null;
                    foreach (var x in placed) { c = placed.FirstOrDefault(y => y != x && y.Kind == x.Kind && (y.Tag != x.Tag || y.Order != x.Order)); if (c != null) { a = x; break; } }
                    if (a == null || c == null) return "nothing to swap";
                    int fa = a.Fixture, sa = a.Slot, fc = c.Fixture, sc = c.Slot;
                    foreach (var fid in new[] { fa, fc }) { var f = w.Fixture(fid); if (f.HasDoor && !f.Open) Do(new ROp { Verb = Verb.Open, Fixture = f.Id, Prop = -1 }); }
                    string e;
                    if ((e = Do(new ROp { Verb = Verb.PickUp, Prop = a.Id, Fixture = -1 })) != null) return e;
                    if ((e = Do(new ROp { Verb = Verb.PickUp, Prop = c.Id, Fixture = -1 })) != null) return e;
                    if ((e = Do(new ROp { Verb = Verb.PlaceInSocket, Prop = a.Id, Fixture = fc, Slot = sc })) != null) return e;
                    return Do(new ROp { Verb = Verb.PlaceInSocket, Prop = c.Id, Fixture = fa, Slot = sa });
                }
                case SabotageKind.Reopen:
                {
                    if (doorFix == null) return "no door";
                    // V12: a running washer's door is locked - the saboteur has to press stop first (two real actions, one sabotage)
                    if (doorFix.Flags.Contains("start")) { var e0 = Do(new ROp { Verb = Verb.Press, Fixture = doorFix.Id, Prop = -1, Arg = "!start" }); if (e0 != null) return e0; }
                    return Do(new ROp { Verb = Verb.Open, Fixture = doorFix.Id, Prop = -1 });
                }
                case SabotageKind.Mess:
                {
                    var f = inst.FixtureByKind.Values.Select(w.Fixture).FirstOrDefault(x => inst.Def.Steps.Any(s => s.Kind == StepKind.Strokes && inst.Fixture(s.Fixture) == x.Id));
                    if (f != null) return Do(new ROp { Verb = Verb.Smudge, Fixture = f.Id, Prop = -1 });
                    var st = inst.Def.Steps.FirstOrDefault(s => s.Kind == StepKind.Stage); if (st == null) return "nothing to mess";
                    var p = inst.Props(st.PropKind).Select(w.Prop).FirstOrDefault(x => x.Stage > 0); if (p == null) return "nothing to mess";
                    return Do(new ROp { Verb = st.Verb == Verb.Fold ? Verb.Unfold : Verb.Smudge, Prop = p.Id, Fixture = -1 });
                }
                case SabotageKind.Unpair:
                { var p = props.FirstOrDefault(x => x.Partner >= 0); return p == null ? "nothing paired" : Do(new ROp { Verb = Verb.Unpair, Prop = p.Id, Fixture = -1 }); }
                case SabotageKind.Detach:
                { var p = props.FirstOrDefault(x => x.AttachedTo >= 0); return p == null ? "nothing attached" : Do(new ROp { Verb = Verb.Detach, Prop = p.Id, Fixture = -1 }); }
                case SabotageKind.Tilt:
                { var p = placed.FirstOrDefault(x => x.Oriented); return p == null ? "nothing upright" : Do(new ROp { Verb = Verb.Align, Prop = p.Id, Fixture = -1, Arg = "tilt" }); }
                case SabotageKind.Unflag:
                {
                    var f = inst.FixtureByKind.Values.Select(w.Fixture).FirstOrDefault(x => x.Flags.Count > 0); if (f == null) return "no control set";
                    return Do(new ROp { Verb = Verb.Press, Fixture = f.Id, Prop = -1, Arg = "!" + f.Flags.First() });
                }
            }
            return "unknown sabotage";
        }
    }
}
