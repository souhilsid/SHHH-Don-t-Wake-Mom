using System;
using System.Collections.Generic;
using System.Text;

namespace SHHH.Core
{
    public enum TouchKind { Grab, Release, Throw, Push, Place, Slam, Operate, PetNudge, PetCarry, Lure, ArmTrap }

    [Serializable]
    public struct Touch
    {
        public int Actor;
        public TouchKind Kind;
        public float Time;
        public float Speed;
        public override string ToString() => $"{Kind} by {Actor} @{Time:0.00} v={Speed:0.0}";
    }

    [Serializable]
    public sealed class Trap
    {
        public int Id;
        public int Owner;
        public readonly HashSet<int> ObjectIds = new HashSet<int>();
        public float ArmedAt;
        public bool WarningVisible;       // the Unity layer shows a readable wobble/sparkle clue near the trap
        public bool Fired;
        public bool Disarmed;
        public int DisarmedBy = ActorIds.None;
    }

    [Serializable]
    public sealed class Responsibility
    {
        public int Principal = ActorIds.None;
        public float Confidence;
        public readonly Dictionary<int, float> Weights = new Dictionary<int, float>();
        public string Explanation = "";
        public int RootEventId = -1;
        public bool IsAttributableSibling(float minConfidence) => ActorIds.IsSibling(Principal) && Confidence >= minConfidence;
        public override string ToString() => $"principal={Principal} conf={Confidence:0.00} :: {Explanation}";
    }

    /// <summary>
    /// Host-side causal evidence ledger. Records per-object interaction history, armed traps, pet lures and noise events,
    /// and resolves *who is responsible* for a noise event using explicit rules (Docs/NOISE_CAUSALITY.md).
    /// It never infers guilt from raw collision callbacks alone and never accepts client-reported culprits.
    /// </summary>
    public sealed class CausalLedger
    {
        public float DirectWindow = 1.5f;     // seconds: release->impact counts as fully direct
        public float ActorWindow = 3.0f;      // seconds: weaker attribution window
        public float ChainWindow = 6.0f;      // seconds: max chain duration root->leaf
        public int MaxChainDepth = 4;
        public float LureWindow = 8.0f;       // seconds: sibling lure of pet stays attributable
        public float CounterplayMinWarning = 1.2f; // seconds a trap clue must be visible before victim can be blamed

        readonly Dictionary<int, List<Touch>> _history = new Dictionary<int, List<Touch>>();
        readonly Dictionary<int, Trap> _traps = new Dictionary<int, Trap>();
        readonly Dictionary<int, int> _trapByObject = new Dictionary<int, int>();
        readonly Dictionary<int, NoiseEvent> _events = new Dictionary<int, NoiseEvent>();
        readonly Dictionary<int, (int actor, float time)> _petLures = new Dictionary<int, (int, float)>();
        int _nextTrapId = 1;

        public IReadOnlyDictionary<int, NoiseEvent> Events => _events;
        public IReadOnlyDictionary<int, Trap> Traps => _traps;

        public void Clear() { _history.Clear(); _traps.Clear(); _trapByObject.Clear(); _events.Clear(); _petLures.Clear(); _nextTrapId = 1; }

        public void RecordTouch(int objectId, int actor, TouchKind kind, float time, float speed = 0f)
        {
            if (!_history.TryGetValue(objectId, out var list)) { list = new List<Touch>(8); _history[objectId] = list; }
            list.Add(new Touch { Actor = actor, Kind = kind, Time = time, Speed = speed });
            if (list.Count > 24) list.RemoveRange(0, list.Count - 24);
        }

        public IReadOnlyList<Touch> History(int objectId) =>
            _history.TryGetValue(objectId, out var l) ? (IReadOnlyList<Touch>)l : Array.Empty<Touch>();

        /// <summary>Find who most recently caused this object's motion. Deterministic tie-break on simultaneous releases.</summary>
        public int ResolveDirectActor(int objectId, float atTime, out float age, out bool simultaneous, out int coActor)
        {
            age = float.MaxValue; simultaneous = false; coActor = ActorIds.None;
            if (!_history.TryGetValue(objectId, out var list) || list.Count == 0) return ActorIds.None;
            int best = ActorIds.None; float bestTime = float.MinValue; float bestSpeed = -1f;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var t = list[i];
                if (t.Time > atTime + 1e-4f) continue;
                if (atTime - t.Time > ActorWindow + 1e-4f) break;
                bool causal = t.Kind == TouchKind.Release || t.Kind == TouchKind.Throw || t.Kind == TouchKind.Push ||
                              t.Kind == TouchKind.Slam || t.Kind == TouchKind.Operate || t.Kind == TouchKind.Place ||
                              t.Kind == TouchKind.PetNudge || t.Kind == TouchKind.PetCarry || t.Kind == TouchKind.Grab;
                if (!causal) continue;
                if (best == ActorIds.None) { best = t.Actor; bestTime = t.Time; bestSpeed = t.Speed; continue; }
                // simultaneous (same instant, different actor): higher release speed wins, then lower slot id
                if (Math.Abs(t.Time - bestTime) < 0.02f && t.Actor != best)
                {
                    simultaneous = true;
                    if (t.Speed > bestSpeed + 1e-3f || (Math.Abs(t.Speed - bestSpeed) <= 1e-3f && t.Actor < best))
                    { coActor = best; best = t.Actor; bestSpeed = t.Speed; }
                    else coActor = t.Actor;
                }
                else break;
            }
            age = best == ActorIds.None ? float.MaxValue : atTime - bestTime;
            return best;
        }

        public int ArmTrap(int owner, IEnumerable<int> objectIds, float time, bool warningVisible)
        {
            var trap = new Trap { Id = _nextTrapId++, Owner = owner, ArmedAt = time, WarningVisible = warningVisible };
            foreach (var o in objectIds) { trap.ObjectIds.Add(o); _trapByObject[o] = trap.Id; RecordTouch(o, owner, TouchKind.ArmTrap, time); }
            _traps[trap.Id] = trap;
            return trap.Id;
        }

        public Trap TrapForObject(int objectId) =>
            _trapByObject.TryGetValue(objectId, out int id) && _traps.TryGetValue(id, out var t) && !t.Fired && !t.Disarmed ? t : null;

        public Trap GetTrap(int id) => _traps.TryGetValue(id, out var t) ? t : null;

        /// <summary>Returns true if this counts as a rescue (disarming someone else's trap).</summary>
        public bool DisarmTrap(int trapId, int actor)
        {
            if (!_traps.TryGetValue(trapId, out var t) || t.Fired || t.Disarmed) return false;
            t.Disarmed = true; t.DisarmedBy = actor;
            foreach (var o in t.ObjectIds) _trapByObject.Remove(o);
            return actor != t.Owner;
        }

        /// <summary>Victim can only be blamed if the clue was visible long enough before triggering.</summary>
        public bool CounterplayExisted(int trapId, float triggerTime)
        {
            var t = GetTrap(trapId);
            return t != null && t.WarningVisible && triggerTime - t.ArmedAt >= CounterplayMinWarning;
        }

        public void RecordPetLure(int petId, int actor, float time) { if (ActorIds.IsSibling(actor)) _petLures[petId] = (actor, time); }

        public int PetProvoker(int petId, float time)
        {
            if (_petLures.TryGetValue(petId, out var l) && time - l.time <= LureWindow) return l.actor;
            return ActorIds.None;
        }

        /// <summary>Register a host-generated event (idempotent by id). Fills DirectActor from history if unset.</summary>
        public bool Register(NoiseEvent e)
        {
            if (_events.ContainsKey(e.Id)) return false;
            if (e.DirectActor == ActorIds.None && e.SourceObjectId >= 0 && e.Cause != NoiseCause.PetAction)
                e.DirectActor = ResolveDirectActor(e.SourceObjectId, e.Time, out _, out _, out _);
            if (e.TrapId < 0 && e.SourceObjectId >= 0)
            {
                var trap = TrapForObject(e.SourceObjectId);
                if (trap != null && e.Cause != NoiseCause.Throw)
                {
                    // An armed trap object made noise without a fresh deliberate throw: treat as trap trigger.
                    e.TrapId = trap.Id;
                    if (e.Cause == NoiseCause.DirectImpact || e.Cause == NoiseCause.ChainReaction || e.Cause == NoiseCause.PetAction)
                    {
                        if (e.TriggeringActor == ActorIds.None) e.TriggeringActor = e.DirectActor;
                        e.Cause = NoiseCause.TrapTrigger;
                    }
                }
            }
            if (e.Cause == NoiseCause.TrapTrigger && e.TrapId >= 0)
            {
                var t = GetTrap(e.TrapId);
                if (t != null) t.Fired = true;
            }
            _events[e.Id] = e;
            return true;
        }

        public NoiseEvent Get(int id) => _events.TryGetValue(id, out var e) ? e : null;

        public Responsibility Resolve(NoiseEvent leaf)
        {
            var r = new Responsibility();
            var sb = new StringBuilder();
            // 1) Walk to root, collecting the causal chain (bounded depth and window).
            var chain = new List<NoiseEvent> { leaf };
            var cur = leaf;
            int depth = 0;
            while (cur.ParentEventId >= 0 && depth < MaxChainDepth)
            {
                var p = Get(cur.ParentEventId);
                if (p == null || leaf.Time - p.Time > ChainWindow) break;
                chain.Add(p); cur = p; depth++;
            }
            var root = chain[chain.Count - 1];
            r.RootEventId = root.Id;

            // 2) Root rules.
            int principal = ActorIds.None; float conf = 0f;
            switch (root.Cause)
            {
                case NoiseCause.TrapTrigger:
                {
                    var trap = GetTrap(root.TrapId);
                    int owner = trap != null ? trap.Owner : ActorIds.None;
                    int victim = root.TriggeringActor;
                    bool counterplay = root.VictimHadCounterplay && trap != null && CounterplayExisted(trap.Id, root.Time);
                    if (ActorIds.IsSibling(victim) && victim != owner && counterplay)
                    {
                        principal = victim; conf = 0.85f;
                        r.Weights[victim] = 0.7f; if (ActorIds.IsSibling(owner)) r.Weights[owner] = 0.3f;
                        sb.Append($"{ActorIds.Describe(victim)} ignored a visible trap clue and made it crash");
                    }
                    else if (ActorIds.IsSibling(owner))
                    {
                        principal = owner; conf = 1f; r.Weights[owner] = 1f;
                        sb.Append(ActorIds.IsPet(victim)
                            ? $"{ActorIds.Describe(owner)}'s trap was set off by {ActorIds.Describe(victim)}"
                            : $"{ActorIds.Describe(owner)}'s trap went off with no fair warning");
                    }
                    else sb.Append("a trap with no known owner went off");
                    break;
                }
                case NoiseCause.PetAction:
                {
                    int pet = root.DirectActor;
                    int provoker = root.ProvokerActor != ActorIds.None ? root.ProvokerActor : PetProvoker(pet, root.Time);
                    if (ActorIds.IsSibling(provoker))
                    { principal = provoker; conf = 0.8f; r.Weights[provoker] = 1f; sb.Append($"{ActorIds.Describe(provoker)} lured {ActorIds.Describe(pet)} into the mess"); }
                    else sb.Append($"{ActorIds.Describe(pet)} acted on its own — no sibling is to blame");
                    break;
                }
                default:
                {
                    int a = root.DirectActor;
                    if (ActorIds.IsPet(a))
                    {
                        int provoker = PetProvoker(a, root.Time);
                        if (ActorIds.IsSibling(provoker)) { principal = provoker; conf = 0.8f; r.Weights[provoker] = 1f; sb.Append($"{ActorIds.Describe(provoker)} lured {ActorIds.Describe(a)}"); }
                        else sb.Append($"{ActorIds.Describe(a)} knocked it over by itself");
                    }
                    else if (ActorIds.IsSibling(a))
                    {
                        float age = 0f; bool simul = false; int co = ActorIds.None;
                        if (root.SourceObjectId >= 0)
                        {
                            int resolved = ResolveDirectActor(root.SourceObjectId, root.Time, out age, out simul, out co);
                            if (resolved != a) { age = 0f; simul = false; co = ActorIds.None; } // explicit DirectActor wins (e.g. door slam)
                        }
                        conf = age <= DirectWindow ? 1f : 0.75f;
                        principal = a;
                        if (simul && ActorIds.IsSibling(co)) { r.Weights[a] = 0.5f; r.Weights[co] = 0.5f; sb.Append($"{ActorIds.Describe(a)} and {ActorIds.Describe(co)} let go together; {ActorIds.Describe(a)} moved it hardest"); }
                        else { r.Weights[a] = 1f; sb.Append(CauseVerb(root.Cause, a)); }
                    }
                    else sb.Append("nobody can be clearly blamed for that noise");
                    break;
                }
            }

            // 3) Intermediate deliberate contributors share responsibility (scoring), principal stays the root cause.
            for (int i = chain.Count - 2; i >= 0; i--)
            {
                var e = chain[i];
                if (ActorIds.IsSibling(e.DirectActor) && e.DirectActor != principal &&
                    (e.Cause == NoiseCause.Throw || e.Cause == NoiseCause.Slam || e.Cause == NoiseCause.DirectImpact))
                {
                    r.Weights.TryGetValue(e.DirectActor, out float w);
                    r.Weights[e.DirectActor] = w + 0.25f;
                }
            }
            if (chain.Count > 1) sb.Append($" (chain of {chain.Count} events)");
            r.Principal = principal; r.Confidence = conf; r.Explanation = sb.ToString();
            return r;
        }

        static string CauseVerb(NoiseCause c, int a)
        {
            string n = ActorIds.Describe(a);
            switch (c)
            {
                case NoiseCause.Throw: return n + " threw something noisy";
                case NoiseCause.Slam: return n + " slammed it shut";
                case NoiseCause.Appliance: return n + " started a loud appliance";
                case NoiseCause.ChainReaction: return n + " set off a chain reaction";
                default: return n + " dropped something with a crash";
            }
        }
    }
}
