using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>
    /// Engine-free kinematic world used for automated match simulation (regression tests, balancing, CI) and the in-editor
    /// "simulate match" tool. Bodies call the SAME RoundController APIs the Unity physics/hand layer calls.
    /// </summary>
    public sealed class HeadlessWorld : IWorldQuery
    {
        public RoundController Round { get; private set; }
        public float Now => Round != null ? Round.Time : 0f;
        readonly Dictionary<int, Vec3> _propPos = new Dictionary<int, Vec3>();
        readonly Dictionary<int, HeadlessBody> _bodies = new Dictionary<int, HeadlessBody>();
        readonly List<BotBrain> _brains = new List<BotBrain>();
        readonly DetRng _rng;
        readonly HashSet<string> _trapNoticed = new HashSet<string>();
        public Func<int, BotProfile> HumanPolicy; // how the scripted "human" slot behaves in simulation
        public int SimulatedNoiseEvents;

        public HeadlessWorld(ulong seed) { _rng = new DetRng(seed ^ 0xBEEF); }

        public void Attach(RoundController round)
        {
            Round = round; _propPos.Clear(); _bodies.Clear(); _brains.Clear(); _trapNoticed.Clear();
            round.Pets.Clear();
            foreach (var p in round.Room.Props) _propPos[p.Id] = p.Spawn;
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                var slot = round.Roster[i];
                var body = new HeadlessBody(this, i, round.Room.SiblingSpawns[i], 1.25f);
                _bodies[i] = body;
                if (!slot.IsActive) continue;
                var profile = slot.IsBot ? BotProfile.For(slot.Difficulty) : (HumanPolicy != null ? HumanPolicy(i) : BotProfile.For(BotDifficulty.Normal));
                body.Speed = 1.25f * profile.SpeedFactor; body.Profile = profile;
                _brains.Add(new BotBrain(i, profile, body, round, this, round.Seed));
            }
            var cat = new HeadlessBody(this, ActorIds.Cat, round.Room.CatBed, 1.6f);
            var dog = new HeadlessBody(this, ActorIds.Dog, round.Room.DogBed, 1.8f);
            _bodies[ActorIds.Cat] = cat; _bodies[ActorIds.Dog] = dog;
            round.Pets.Add(new PetBrain(PetKind.Cat, cat, round, this, round.Seed));
            round.Pets.Add(new PetBrain(PetKind.Dog, dog, round, this, round.Seed));
        }

        public void Step(float dt)
        {
            if (Round == null) return;
            float now = Now;
            foreach (var b in _brains) b.Tick(now);
            foreach (var p in Round.Pets) p.Tick(now);
            foreach (var body in _bodies.Values) body.Step(dt);
            CheckTraps();
            Perceive();
        }

        /// <summary>Headless perception: an active sibling within 3.5 m of an incident (no walls in this model) saw who did it.</summary>
        void Perceive()
        {
            while (Round.TryTakeIncidentToPerceive(out var inc))
                for (int s = 0; s < CompetitorRoster.SlotCount; s++)
                {
                    if (s == inc.TruthActor || !Round.Roster[s].IsActive || !_bodies.TryGetValue(s, out var b)) continue;
                    float d = b.Position.FlatDistance(inc.Position);
                    if (d <= 3.5f) Round.ReportWitness(inc.Id, s, 1f - d / 5f);
                }
        }

        void CheckTraps()
        {
            foreach (var t in Round.Ledger.Traps.Values)
            {
                if (t.Fired || t.Disarmed) continue;
                foreach (int prop in t.ObjectIds)
                {
                    if (Round.HolderOf(prop) != ActorIds.None) continue;
                    var pp = _propPos[prop];
                    foreach (var kv in _bodies)
                    {
                        int actor = kv.Key; var body = kv.Value;
                        if (actor == t.Owner || !body.Moving) continue;
                        if (ActorIds.IsSibling(actor) && !Round.Roster[actor].IsActive) continue;
                        float d = body.Position.FlatDistance(pp);
                        string key = t.Id + ":" + actor;
                        if (d < 1.2f && ActorIds.IsSibling(actor) && t.WarningVisible && Now - t.ArmedAt >= 1.2f && !_trapNoticed.Contains(key))
                        {
                            _trapNoticed.Add(key);
                            if (_rng.Value() < (body.Profile?.TrapNoticeChance ?? 0.5f)) { body.AvoidUntil[prop] = Now + 6f; continue; }
                        }
                        if (body.AvoidUntil.TryGetValue(prop, out float until) && until > Now) continue;
                        if (d < 0.45f)
                        {
                            bool careless = ActorIds.IsSibling(actor) && _rng.Value() > (body.Profile?.Carefulness ?? 0.7f);
                            _propPos[prop] = new Vec3(pp.x, 0f, pp.z);
                            Round.SubmitTrapTrigger(t.Id, actor, careless, prop, _rng.Range(2.4f, 3.6f), pp);
                            SimulatedNoiseEvents++;
                            goto nextTrap;
                        }
                    }
                }
                nextTrap:;
            }
        }

        // IWorldQuery
        public Vec3 PropPosition(int propId) => _propPos.TryGetValue(propId, out var p) ? p : default;
        public int PropHolder(int propId) => Round.HolderOf(propId);
        public bool PropAvailable(int propId) => Round.HolderOf(propId) == ActorIds.None && _propPos.ContainsKey(propId);
        public Vec3 ActorPosition(int actor) => _bodies.TryGetValue(actor, out var b) ? b.Position : default;
        public int HeldBy(int actor) => Round.HeldBy(actor);
        internal void SetPropPos(int prop, Vec3 p) => _propPos[prop] = p;
        internal DetRng Rng => _rng;
        public BotBrain BrainFor(int slot) { foreach (var b in _brains) if (b.Slot == slot) return b; return null; }

        public sealed class HeadlessBody : IAgentBody
        {
            readonly HeadlessWorld _w;
            public readonly int Actor;
            public Vec3 Position { get; private set; }
            public float Speed;
            public BotProfile Profile;
            public bool Busy { get; private set; }
            public bool LastSucceeded { get; private set; }
            public bool Moving { get; private set; }
            public int HeldProp => _w.Round.HeldBy(Actor);
            public readonly Dictionary<int, float> AvoidUntil = new Dictionary<int, float>();
            AgentCommand _cmd; float _actTimer; bool _arrived;

            public HeadlessBody(HeadlessWorld w, int actor, Vec3 start, float speed) { _w = w; Actor = actor; Position = start; Speed = speed; }

            public void Execute(AgentCommand cmd)
            {
                _cmd = cmd; Busy = true; _arrived = false; _actTimer = 0f; LastSucceeded = false;
                if (cmd.Type == AgentCommandType.PickUp || cmd.Type == AgentCommandType.Knock || cmd.Type == AgentCommandType.DisarmTrap) _cmd.Target = _w.PropPosition(cmd.PropId);
            }
            public void Stop() { Busy = false; Moving = false; }

            public void Step(float dt)
            {
                Moving = false;
                if (!Busy) return;
                var R = _w.Round;
                // Carried prop follows the body.
                int held = HeldProp; if (held >= 0) _w.SetPropPos(held, Position + new Vec3(0, 0.9f, 0));
                bool needsTravel = _cmd.Type != AgentCommandType.Wait && _cmd.Type != AgentCommandType.Bark && _cmd.Type != AgentCommandType.Emote;
                if (needsTravel && !_arrived)
                {
                    if (_cmd.Type == AgentCommandType.PickUp && !_w.PropAvailable(_cmd.PropId)) { Finish(false); return; }
                    var flatTarget = new Vec3(_cmd.Target.x, 0, _cmd.Target.z);
                    float reach = (_cmd.Type == AgentCommandType.MoveTo || _cmd.Type == AgentCommandType.Rest) ? 0.05f : 0.55f;
                    if (Position.FlatDistance(flatTarget) <= reach) _arrived = true;
                    else { Position = Vec3.MoveTowards(Position, flatTarget, Speed * dt); Moving = true; return; }
                }
                _actTimer += dt;
                switch (_cmd.Type)
                {
                    case AgentCommandType.MoveTo: Finish(true); break;
                    case AgentCommandType.Wait: case AgentCommandType.Rest: case AgentCommandType.Emote:
                        if (_actTimer >= Math.Max(0.1f, _cmd.Duration)) Finish(true); break;
                    case AgentCommandType.PickUp:
                        if (_actTimer < 0.7f) break;
                        Finish(_w.PropAvailable(_cmd.PropId) && R.Grab(Actor, _cmd.PropId)); break;
                    case AgentCommandType.PlaceAt:
                    case AgentCommandType.PutDown:
                    case AgentCommandType.ArmTrapAt:
                    {
                        var c = R.Board.ChoreForProp(_cmd.PropId);
                        float work = (c != null ? c.Template.WorkSecondsPerStep : 2f) * 0.6f / Math.Max(0.4f, Profile?.SpeedFactor ?? 1f);
                        if (_actTimer < work) break;
                        int prop = _cmd.PropId;
                        if (R.HolderOf(prop) != Actor) { Finish(false); break; }
                        if (_cmd.Careful)
                        {
                            R.Release(Actor, prop, 0.15f, false);
                            _w.SetPropPos(prop, _cmd.Target);
                            if (_cmd.Type == AgentCommandType.PlaceAt) R.Placed(prop, _cmd.AnchorId, Actor);
                            if (_cmd.Type == AgentCommandType.ArmTrapAt) R.ArmTrap(Actor, prop, true);
                        }
                        else
                        {
                            // Fumble: the prop clatters down — a REAL physics-style impact attributed through the ledger.
                            float v = _w.Rng.Range(2.2f, 4.4f);
                            R.Release(Actor, prop, v * 0.3f, false);
                            bool stillLands = _w.Rng.Value() < 0.5f;
                            var land = stillLands ? _cmd.Target : new Vec3(_cmd.Target.x + 0.3f, 0f, _cmd.Target.z);
                            _w.SetPropPos(prop, land);
                            R.SubmitImpact(prop, v, land);
                            _w.SimulatedNoiseEvents++;
                            if (stillLands && _cmd.Type == AgentCommandType.PlaceAt) R.Placed(prop, _cmd.AnchorId, Actor);
                        }
                        Finish(true); break;
                    }
                    case AgentCommandType.DisarmTrap:
                        if (_actTimer < 1.0f) break;
                        Finish(R.DisarmTrap(_cmd.TrapId, Actor)); break;
                    case AgentCommandType.Knock:
                    {
                        if (_actTimer < 0.6f) break;
                        int prop = _cmd.PropId;
                        if (!_w.PropAvailable(prop)) { Finish(false); break; }
                        var pp = _w.PropPosition(prop);
                        R.Push(Actor, prop, 1.0f);
                        if (R.IsRecipeProp(prop))
                        {
                            // the item really left its slot: the recipe world sees the pet move it (a non-punitive pet incident)
                            var rop = new ROp { Actor = Actor, Verb = Verb.PickUp, Prop = R.RecipePropOf[prop], Prop2 = -1, Fixture = -1, Slot = -1 };
                            if (R.RecipeAction(rop) == null) { rop.Verb = Verb.PutDown; rop.Arg = "knocked"; rop.EventId = 0; R.RecipeAction(rop); }
                        }
                        float h = Math.Max(0.05f, pp.y);
                        _w.SetPropPos(prop, new Vec3(pp.x + 0.2f, 0f, pp.z));
                        R.SubmitImpact(prop, (float)Math.Sqrt(2 * 9.81 * h), pp, -1, NoiseCause.DirectImpact);
                        _w.SimulatedNoiseEvents++;
                        Finish(true); break;
                    }
                    case AgentCommandType.RecipeOp:
                    {
                        // kinematic stand-in for the Unity body: reach + work time, then the same round API a hand would drive
                        float work = (_cmd.Op.Verb == Verb.Stroke || _cmd.Op.Verb == Verb.Fold || _cmd.Op.Verb == Verb.Fill ? 1.4f : 0.8f) / Math.Max(0.4f, Profile?.SpeedFactor ?? 1f);
                        if (_actTimer < work) break;
                        var op = _cmd.Op; op.Actor = Actor; op.EventId = 0;
                        if (op.Verb == Verb.PlaceInSocket && R.Recipes.World.Prop(op.Prop).HeldBy == Actor)
                        {
                            int roomProp = R.RoomPropOf[op.Prop]; R.Release(Actor, roomProp, 0.15f, false); _w.SetPropPos(roomProp, _cmd.Target);
                            if (!_cmd.Careful)
                            {
                                // a hurried set-down clacks: louder for riskier recipes (same impact path a real drop takes)
                                float risk = R.Recipes.Instances[R.Recipes.World.Prop(op.Prop).RecipeInstance].Def.NoiseRisk;
                                float v = _w.Rng.Range(1.6f, 3.6f) * (0.6f + risk);
                                R.SubmitImpact(roomProp, v, _cmd.Target); _w.SimulatedNoiseEvents++;
                            }
                        }
                        Finish(R.RecipeAction(op) == null); break;
                    }
                    case AgentCommandType.Bark:
                        R.SubmitPetSound(Actor, _w.Rng.Range(6f, 14f), Position); _w.SimulatedNoiseEvents++;
                        Finish(true); break;
                    default: Finish(true); break;
                }
            }

            void Finish(bool ok) { Busy = false; LastSucceeded = ok; }
        }
    }

    /// <summary>Runs complete matches headlessly. Used by tests and the editor's "Simulate 100 matches" balancing tool.</summary>
    public static class MatchSimulator
    {
        public static MatchController RunSolo(ulong seed, BotDifficulty botDiff, BotProfile humanProfile, float dt = 0.1f, float maxSeconds = 1500f)
        {
            var m = MatchController.CreateSolo("You", botDiff, seed);
            return Run(m, MatchMode.Solo, seed, humanProfile, dt, maxSeconds);
        }

        public static MatchController Run(MatchController m, MatchMode mode, ulong seed, BotProfile humanProfile, float dt = 0.1f, float maxSeconds = 1500f)
        {
            var world = new HeadlessWorld(seed);
            world.HumanPolicy = _ => humanProfile ?? BotProfile.For(BotDifficulty.Normal);
            m.RoundStarting += r => world.Attach(r);
            if (!m.Start(mode, seed, out string err)) throw new InvalidOperationException(err);
            float t = 0f;
            while (t < maxSeconds && m.Phase != MatchPhase.Winner && m.Phase != MatchPhase.Results && m.Phase != MatchPhase.Cancelled)
            {
                m.Tick(dt);
                if (m.Phase == MatchPhase.InRound) world.Step(dt);
                t += dt;
            }
            return m;
        }
    }
}
