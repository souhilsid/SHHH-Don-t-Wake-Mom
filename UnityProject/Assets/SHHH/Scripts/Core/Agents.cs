using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum AgentCommandType { None, MoveTo, PickUp, PlaceAt, PutDown, ArmTrapAt, LurePet, DisarmTrap, Knock, Wait, Emote, Bark, Rest, RecipeOp,
        PourAt,     // V12: hold the carried container over Target and tip it for Duration (the pour is physics - finite, may spill)
        DropInto }  // V12: hold the carried item over Target (a bin opening) and let go

    [Serializable]
    public struct AgentCommand
    {
        public AgentCommandType Type;
        public int PropId;
        public int AnchorId;
        public Vec3 Target;
        public bool Careful;
        public int TrapId;
        public int PetId;
        public float Duration;
        public string Emote;
        /// <summary>V7 recipe step the body performs physically at <see cref="Target"/> (place in a slot, lid, wipe, fold, pair...).</summary>
        public ROp Op;
        public override string ToString() => $"{Type} prop={PropId} anchor={AnchorId} careful={Careful} {Target}";
    }

    /// <summary>
    /// A physical agent body (bot sibling avatar or pet). Unity implements it with NavMeshAgent + animation/IK and the SAME
    /// prop interaction authority human hands use; the headless simulator implements it kinematically. Bodies report
    /// physical outcomes (grabs, releases, impacts) to the RoundController — brains never edit scores.
    /// </summary>
    public interface IAgentBody
    {
        Vec3 Position { get; }
        bool Busy { get; }
        bool LastSucceeded { get; }
        int HeldProp { get; }
        void Execute(AgentCommand cmd);
        void Stop();
    }

    /// <summary>What agents may perceive. Implementations must not leak hidden info (e.g. unseen traps).</summary>
    public interface IWorldQuery
    {
        float Now { get; }
        Vec3 PropPosition(int propId);
        int PropHolder(int propId);
        bool PropAvailable(int propId);
        Vec3 ActorPosition(int actor);
        int HeldBy(int actor);
    }

    [Serializable]
    public sealed class BotProfile
    {
        public BotDifficulty Difficulty;
        public float SpeedFactor;        // locomotion & hand speed multiplier
        public float ReactionMin, ReactionMax;
        public float Carefulness;        // probability of a gentle placement
        public float NoiseAwareness;     // how strongly Mom's meter changes behaviour
        public float MistakeRate;        // suboptimal choices
        public float SabotageChance;     // per decision, after cooldown
        public float SabotageCooldown;
        public float TrapNoticeChance;   // chance to notice a visible trap clue nearby
        public float RescueChance;

        public static BotProfile For(BotDifficulty d)
        {
            switch (d)
            {
                case BotDifficulty.Rookie: return new BotProfile { Difficulty = d, SpeedFactor = 0.78f, ReactionMin = 1.3f, ReactionMax = 2.4f, Carefulness = 0.7f, NoiseAwareness = 0.35f, MistakeRate = 0.18f, SabotageChance = 0.03f, SabotageCooldown = 45f, TrapNoticeChance = 0.45f, RescueChance = 0.1f };
                case BotDifficulty.Clever: return new BotProfile { Difficulty = d, SpeedFactor = 1.0f, ReactionMin = 0.5f, ReactionMax = 1.0f, Carefulness = 0.93f, NoiseAwareness = 0.95f, MistakeRate = 0.04f, SabotageChance = 0.10f, SabotageCooldown = 28f, TrapNoticeChance = 0.85f, RescueChance = 0.45f };
                default: return new BotProfile { Difficulty = d, SpeedFactor = 0.9f, ReactionMin = 0.8f, ReactionMax = 1.6f, Carefulness = 0.85f, NoiseAwareness = 0.7f, MistakeRate = 0.09f, SabotageChance = 0.06f, SabotageCooldown = 35f, TrapNoticeChance = 0.65f, RescueChance = 0.25f };
            }
        }
    }

    /// <summary>
    /// Utility-AI sibling bot. Each decision scores goals (shared step, personal bonus, sabotage, rescue, wait-for-quiet),
    /// then issues one physical command to its body. Debuggable via <see cref="DebugGoal"/>.
    /// Difficulty changes choices/speed/noise tolerance — never hidden score bonuses.
    /// </summary>
    public sealed class BotBrain
    {
        public readonly int Slot;
        public readonly BotProfile Profile;
        public string DebugGoal = "idle";
        readonly DetRng _rng;
        readonly IAgentBody _body;
        readonly RoundController _round;
        readonly IWorldQuery _world;
        float _nextThinkAt;
        float _sabotageReadyAt;
        int _plannedTrapTargetSlot = ActorIds.None;
        Vec3 _plannedTarget; bool _planTrap, _planHide;
        /// <summary>True while this bot is on its way to swipe an item or set a trap (presentation tell only, never a rule input).</summary>
        public bool Plotting => _planTrap || _planHide;

        public BotBrain(int slot, BotProfile profile, IAgentBody body, RoundController round, IWorldQuery world, ulong seed)
        {
            Slot = slot; Profile = profile; _body = body; _round = round; _world = world;
            _rng = new DetRng(seed ^ (ulong)(slot * 7777 + 13));
            _sabotageReadyAt = 20f + _rng.Range(0f, 15f); // nobody sabotages in the opening seconds
            _nextThinkAt = _rng.Range(Profile.ReactionMin, Profile.ReactionMax); // human-like start delay
        }

        public void Tick(float now)
        {
            if (_round.Interro != null && _round.Mom.State == MomState.Interrogating && _round.Roster[Slot].IsActive) { Confront(now); return; }
            if (!_round.IsPlayable || !_round.Roster[Slot].IsActive) { if (_body.Busy) _body.Stop(); DebugGoal = "frozen"; return; }
            // Counterplay reflex: notice my item was swiped (the game shows an alert + glow) and react after a human-like delay.
            var sb = _round.Board.Sabotage;
            if (sb.Count > _seenSabotage)
            {
                for (int i = _seenSabotage; i < sb.Count; i++)
                    if (sb[i].Victim == Slot && _world.HeldBy(Slot) < 0 && _body.Busy)
                    { _body.Stop(); _nextThinkAt = now + _rng.Range(0.6f, 1.6f) / Profile.SpeedFactor; DebugGoal = "hey! that's mine"; }
                _seenSabotage = sb.Count;
            }
            if (_round.Recipes != null && _round.Recipes.Sabotage.Count > _seenRecipeSabotage)
            {
                for (int i = _seenRecipeSabotage; i < _round.Recipes.Sabotage.Count; i++)
                    if (_round.Recipes.Sabotage[i].Victim == Slot && _body.Busy && _world.HeldBy(Slot) < 0)
                    { _body.Stop(); _nextThinkAt = now + _rng.Range(0.6f, 1.6f) / Profile.SpeedFactor; DebugGoal = "hey! my job"; }
                _seenRecipeSabotage = _round.Recipes.Sabotage.Count;
            }
            if (_body.Busy || now < _nextThinkAt) return;
            Decide(now);
            _nextThinkAt = now + _rng.Range(Profile.ReactionMin, Profile.ReactionMax) * (0.6f + 0.4f / Profile.SpeedFactor);
        }

        int _seenSabotage, _seenRecipeSabotage;

        bool RollCareful()
        {
            float meter01 = _round.Meter.Value / 100f;
            float p = Profile.Carefulness + Profile.NoiseAwareness * 0.15f * meter01;
            return _rng.Value() < MathX.Clamp(p, 0f, 0.985f);
        }

        void Decide(float now)
        {
            if (_round.IsTimedOut(Slot)) { DebugGoal = "sent to bed (timeout)"; _body.Execute(new AgentCommand { Type = AgentCommandType.Wait, Duration = Math.Max(0.5f, _round.TimeoutRemaining(Slot)), Emote = "sad" }); return; }
            int held = _world.HeldBy(Slot);
            // V12 care first (in recipe rounds): a care job in progress continues whatever the hands hold
            if (_round.Recipes != null && (_careStep >= 0 || held < 0) && DecideCare(now, held)) return;
            if (_round.Recipes != null && _trashProp >= 0 && held == _trashProp && DecideTrash(held)) return;
            if (held >= 0) { DecideWithHeld(held); return; }
            _planTrap = _planHide = false;

            // 1) Freeze when Mom is close to waking (noise-aware bots wait for the meter to decay).
            var stage = _round.Meter.Stage;
            if (stage >= SleepStage.AlmostAwake && _rng.Value() < Profile.NoiseAwareness)
            { DebugGoal = "freeze (Mom almost awake)"; _body.Execute(new AgentCommand { Type = AgentCommandType.Wait, Duration = _rng.Range(1.5f, 3.5f), Emote = "shh" }); return; }

            // 2) Rescue: disarm a noticed trap nearby (fair perception: only visible clues within 3.5m).
            foreach (var t in _round.Ledger.Traps.Values)
            {
                if (t.Fired || t.Disarmed || t.Owner == Slot || !t.WarningVisible) continue;
                int anyProp = -1; foreach (var p in t.ObjectIds) { anyProp = p; break; }
                if (anyProp < 0 || _world.PropHolder(anyProp) != ActorIds.None) continue;
                if (_body.Position.FlatDistance(_world.PropPosition(anyProp)) > 3.5f) continue;
                if (_rng.Value() < Profile.RescueChance)
                { DebugGoal = "rescue: disarm trap " + t.Id; _body.Execute(new AgentCommand { Type = AgentCommandType.DisarmTrap, TrapId = t.Id, PropId = anyProp, Careful = true }); return; }
            }

            // V7 recipe rounds: own recipe (one solver step at a time, performed by the body), recipe sabotage, repair.
            if (_round.Recipes != null) { DecideRecipe(now); return; }

            // 3) Sabotage (bounded, physical, fair: always leaves a visible clue).
            if (now >= _sabotageReadyAt && _rng.Value() < Profile.SabotageChance && TryPlanSabotage(now)) return;

            // 4) Chores by utility.
            bool quietOnly = stage >= SleepStage.Suspicious && _rng.Value() < Profile.NoiseAwareness;
            float bestU = float.MinValue; ChoreStep best = null; ChoreInstance bestC = null;
            foreach (var c in _round.Board.Chores)
            {
                if (c.State == ChoreState.Completed) continue;
                if (c.IsBonus && c.BonusOwner != Slot) continue;
                float risk = c.Template.NoiseRisk;
                if (quietOnly && risk > 0.3f) continue;
                foreach (var s in c.Steps)
                {
                    if (s.Done || !_world.PropAvailable(s.PropId)) continue;
                    var pPos = _world.PropPosition(s.PropId);
                    var a = _round.Room.Anchor(s.AnchorId);
                    float travel = (_body.Position.FlatDistance(pPos) + pPos.FlatDistance(a.Position)) / (1.25f * Profile.SpeedFactor);
                    float work = c.Template.WorkSecondsPerStep / Profile.SpeedFactor;
                    float value = (float)c.Points / c.Steps.Count;
                    if (c.IsBonus) value *= 2.0f;                       // V5: my own job comes first
                    if (c.IsBonus && s.UndoneBy != ActorIds.None && s.UndoneBy != Slot) value *= 1.8f; // counterplay: restore stolen item fast
                    // Rivals heading for the same prop? Clever bots avoid contention.
                    float contention = 0f;
                    for (int r = 0; r < CompetitorRoster.SlotCount; r++)
                        if (r != Slot && _round.Roster[r].IsActive && _world.ActorPosition(r).FlatDistance(pPos) < _body.Position.FlatDistance(pPos) * 0.6f) contention += 0.35f;
                    float meter01 = _round.Meter.Value / 100f;
                    float riskCost = risk * (0.3f + Profile.NoiseAwareness * meter01 * 1.5f);
                    float u = value / (travel + work + 1f) * (1f - riskCost) * (1f - contention * (Profile.Difficulty == BotDifficulty.Rookie ? 0.2f : 1f));
                    u += _rng.Range(-Profile.MistakeRate, Profile.MistakeRate) * 1.5f; // human-like noise in judgement
                    if (u > bestU) { bestU = u; best = s; bestC = c; }
                }
            }
            if (best != null)
            {
                DebugGoal = $"chore: {bestC.Title}";
                _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = best.PropId });
                return;
            }
            DebugGoal = "wander";
            var room = _round.Room;
            _body.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = new Vec3(_rng.Range(-room.Width * 0.3f, room.Width * 0.3f), 0, _rng.Range(-room.Depth * 0.3f, room.Depth * 0.3f)) });
        }

        void DecideWithHeld(int held)
        {
            if (_planTrap)
            {
                DebugGoal = "sabotage: set a trap";
                _planTrap = false;
                _body.Execute(new AgentCommand { Type = AgentCommandType.ArmTrapAt, PropId = held, Target = _plannedTarget, Careful = true });
                return;
            }
            if (_planHide)
            {
                DebugGoal = "sabotage: hide an item";
                _planHide = false;
                _body.Execute(new AgentCommand { Type = AgentCommandType.PutDown, PropId = held, Target = _plannedTarget, Careful = RollCareful() });
                return;
            }
            if (_round.Recipes != null) { DecideRecipe(_world.Now); return; }
            var c = _round.Board.ChoreForProp(held);
            if (c != null && (!c.IsBonus || c.BonusOwner == Slot))
            {
                AnchorSpec bestA = null; float bestD = float.MaxValue;
                foreach (var s in c.Steps)
                {
                    if (s.Done) continue;
                    var a = _round.Room.Anchor(s.AnchorId);
                    float d = _body.Position.FlatDistance(a.Position);
                    if (d < bestD) { bestD = d; bestA = a; }
                }
                if (bestA != null)
                {
                    DebugGoal = $"place for: {c.Title}";
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PlaceAt, PropId = held, AnchorId = bestA.Id, Target = bestA.Position, Careful = RollCareful() });
                    return;
                }
            }
            DebugGoal = "put down";
            _body.Execute(new AgentCommand { Type = AgentCommandType.PutDown, PropId = held, Target = _body.Position + new Vec3(0.3f, 0, 0.3f), Careful = RollCareful() });
        }

        // ------------------------------------------------------------------ V7 confrontation (same calls a player's cards/pointing make)
        Interrogation _confrontFor; float _confrontAt;

        void Confront(float now)
        {
            var q = _round.Interro;
            if (_body.Busy) _body.Stop();
            if (q != _confrontFor) { _confrontFor = q; _confrontAt = now + _rng.Range(1.5f, 6f) / Profile.SpeedFactor; DebugGoal = "listening to Mom"; }
            if (!q.EvidenceOpen || now < _confrontAt || q.Done.Contains(Slot)) return;
            var b = _round.Belief;
            // 1) a real memory of this incident (truthful: cards are only what I saw)
            WitnessMemory best = null;
            if (q.Incident != null) foreach (var m in b.MemoriesOf(Slot)) if (m.Incident == q.Incident.Id && !m.Presented && (best == null || m.Confidence > best.Confidence)) best = m;
            // 2) otherwise something I saw a rival do earlier this round (shows a pattern; prefer the strongest rival)
            if (best == null)
            {
                float top = float.MinValue;
                foreach (var m in b.MemoriesOf(Slot))
                {
                    if (m.Presented || !ActorIds.IsSibling(m.SeenActor) || m.SeenActor == Slot || !_round.Roster[m.SeenActor].IsActive) continue;
                    var inc = b.Get(m.Incident); if (inc == null || inc.Kind != IncidentKind.Sabotage) continue;
                    float sc = _round.Scores[m.SeenActor].Total; if (sc > top) { top = sc; best = m; }
                }
            }
            if (best != null) { _round.PresentCard(Slot, best.Id); DebugGoal = "shows Mom a memory"; }
            else if (_rng.Value() < 0.25f + Profile.MistakeRate)
            {
                // no proof: some siblings point anyway (weak, capped) - at the current leader
                int lead = ActorIds.None; float sc = float.MinValue;
                for (int r = 0; r < CompetitorRoster.SlotCount; r++) if (r != Slot && _round.Roster[r].IsActive && _round.Scores[r].Total > sc) { sc = _round.Scores[r].Total; lead = r; }
                if (lead != ActorIds.None) { _round.Accuse(Slot, lead); DebugGoal = "points at " + _round.Roster[lead].DisplayName; }
            }
            _round.PassInterrogation(Slot);
        }

        // ------------------------------------------------------------------ V7 recipes
        public string RecipeDebug = "";
        int _job = -1;
        readonly List<ROp> _plan = new List<ROp>(); int _planSabSeen;

        // ------------------------------------------------------------------ V12 care (bot parity with players): feed hungry pets, bin trash
        int _careStep = -1, _carePet = -1; Vec3 _bagHome;
        public string CareWhy = "";
        int PropOfKind(string kind) { foreach (var p in _round.Room.Props) if (p.Kind == kind) return p.Id; return -1; }
        /// <summary>A hungry pet nobody else is feeding: fetch the food bag, tip it over the bowl, put it down. Same physics as a player
        /// (the host pours the finite kibble; the pet eats what really landed). Returns true when it issued a command.</summary>
        bool DecideCare(float now, int held)
        {
            if (_round.Room == null) return false;
            int bag = PropOfKind("pet_food");
            if (_careStep < 0)
            {
                int pi = _round.PetHunger[1] >= _round.PetHunger[0] ? 1 : 0;
                CareWhy = _round.PetHunger[pi] < RoundController.HungryAt + 8f ? "pets fine" : bag < 0 ? "no food bag" : held >= 0 ? "hands busy" : (_round.FeedingBot >= 0 && _round.FeedingBot != Slot) ? "someone else feeds" : !_world.PropAvailable(bag) ? "bag unavailable" : _world.PropHolder(bag) >= 0 ? "bag held" : "";
                if (CareWhy.Length > 0) return false;
                _round.FeedingBot = Slot; _carePet = pi; _careStep = 0; _bagHome = _world.PropPosition(bag);
            }
            var bowl = _carePet == 0 ? _round.Room.CatBowl : _round.Room.DogBowl;
            switch (_careStep)
            {
                case 0: DebugGoal = "fetching the pet food"; _careStep = 1; _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = bag }); return true;
                case 1:
                    if (held != bag) { EndCare(); return false; }
                    DebugGoal = "feeding the " + (_carePet == 0 ? "cat" : "dog"); _careStep = 2;
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PourAt, PropId = bag, Target = bowl, Duration = 2.6f }); return true;
                case 2:
                    // put the bag back where it was found (not in the walkway, where someone would knock it over and spill it)
                    _careStep = 3; _body.Execute(new AgentCommand { Type = AgentCommandType.PutDown, PropId = bag, Target = _bagHome, Careful = true }); return true;
                default: EndCare(); return false;
            }
        }
        void EndCare() { if (_round.FeedingBot == Slot) _round.FeedingBot = -1; _careStep = -1; _carePet = -1; }

        int _trashProp = -1;
        /// <summary>Nothing else to do: pick up the nearest loose trash and drop it into the nearest bin (the host counts it when it rests inside).</summary>
        bool DecideTrash(int held)
        {
            if (_round.Room == null || _round.Room.Bins.Count == 0) return false;
            if (_trashProp >= 0 && held == _trashProp)
            {
                int bi = -1; float bd = float.MaxValue;
                for (int i = 0; i < _round.Room.Bins.Count; i++) { if (_round.BinClaimedByOther(i, Slot)) continue; float d = _round.Room.Bins[i].FlatDistance(_body.Position); if (d < bd) { bd = d; bi = i; } }
                if (bi < 0) { DebugGoal = "waiting for a free bin"; _body.Execute(new AgentCommand { Type = AgentCommandType.Wait, Duration = _rng.Range(1f, 2f) }); return true; }
                var bin = _round.Room.Bins[bi]; _round.ClaimBin(Slot, bin);
                DebugGoal = "binning trash"; int t = _trashProp; _trashProp = -1;
                _body.Execute(new AgentCommand { Type = AgentCommandType.DropInto, PropId = t, Target = bin }); return true;
            }
            if (held >= 0) return false;
            int best = -1; float bdist = 6f;
            foreach (var p in _round.Room.Props)
                if (HouseClutter.IsTrash(p.Kind) && !_round.Binned.Contains(p.Id) && _world.PropAvailable(p.Id) && _world.PropHolder(p.Id) < 0 && !_round.TrashClaimedByOther(p.Id, Slot))
                { float d = _world.PropPosition(p.Id).FlatDistance(_body.Position); if (d < bdist) { bdist = d; best = p.Id; } }
            if (best < 0) return false;
            _trashProp = best; _round.ClaimTrash(Slot, best); DebugGoal = "picking up trash"; _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = best }); return true;
        }

        void DecideRecipe(float now)
        {
            var R = _round.Recipes;
            int held = _world.HeldBy(Slot);
            if (DecideCare(now, held)) return;   // V12: a hungry pet beats a chore (a whining pet wakes Mom)
            if (_trashProp >= 0 && held == _trashProp && DecideTrash(held)) return;
            if (held < 0 && now >= _sabotageReadyAt && _rng.Value() < Profile.SabotageChance * 2f && TryPlanRecipeSabotage(now)) return;
            // continuous match: several jobs around the house - keep the current one until it is done, then take the nearest open one
            RecipeInstance mine = _job >= 0 && _job < R.Instances.Count && R.Instances[_job].Owner == Slot && !R.Instances[_job].Complete ? R.Instances[_job] : null;
            if (mine == null)
            {
                float bd = float.MaxValue; RecipeInstance lastOwned = null;
                foreach (var i in R.Instances)
                {
                    if (i.Owner != Slot) continue; lastOwned = i; if (i.Complete) continue;
                    float d = StationOf(i).FlatDistance(_body.Position); if (d < bd) { bd = d; mine = i; }
                }
                if (mine == null) mine = lastOwned;
                if (mine != null && mine.Index != _job) { _job = mine.Index; _plan.Clear(); }
            }
            // follow a committed plan; re-plan when it ran out, the last action failed, or someone disturbed my recipe
            if (mine != null && (_plan.Count == 0 || !_body.LastSucceeded || _planSabSeen != R.Sabotage.Count || mine.Complete))
            { _plan.Clear(); if (!mine.Complete) _plan.AddRange(RecipeSolver.Plan(R, mine, Slot)); _planSabSeen = R.Sabotage.Count; }
            ROp? next = null;
            if (_plan.Count > 0) { next = _plan[0]; _plan.RemoveAt(0); }
            if (next == null)
            {
                if (held >= 0) { DebugGoal = "put down"; _body.Execute(new AgentCommand { Type = AgentCommandType.PutDown, PropId = held, Target = _body.Position + new Vec3(0.3f, 0, 0.3f), Careful = true }); return; }
                if (DecideTrash(held)) return;   // V12: idle hands tidy up
                DebugGoal = mine != null && mine.Complete ? "guarding my job" : "no recipe step";
                var guard = mine != null ? StationOf(mine) : _body.Position;
                _body.Execute(new AgentCommand { Type = AgentCommandType.Wait, Duration = _rng.Range(1.5f, 3f), Target = guard });
                return;
            }
            var op = next.Value; RecipeDebug = op.ToString();
            DebugGoal = $"recipe {mine.Def.Id}: {op.Verb}";
            switch (op.Verb)
            {
                case Verb.PickUp:
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = _round.RoomPropOf[op.Prop] }); return;
                case Verb.PutDown: case Verb.Release:
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PutDown, PropId = _round.RoomPropOf[op.Prop], Target = _world.PropPosition(_round.RoomPropOf[op.Prop]), Careful = true }); return;
                default:
                    _body.Execute(new AgentCommand { Type = AgentCommandType.RecipeOp, Op = op, PropId = op.Prop >= 0 ? _round.RoomPropOf[op.Prop] : -1, Target = OpTarget(op), Careful = RollCareful() }); return;
            }
        }

        /// <summary>Where the body must stand/reach for a recipe op (room coordinates).</summary>
        public Vec3 OpTarget(ROp op)
        {
            foreach (var pl in _round.RecipeFixtures)
            {
                if (pl.FixtureId != op.Fixture) continue;
                return op.Slot >= 0 && op.Slot < pl.Slots.Length ? pl.Slots[op.Slot] : pl.Position;
            }
            if (op.Prop2 >= 0) return _world.PropPosition(_round.RoomPropOf[op.Prop2]);
            if (op.Prop >= 0) { var p = _round.Recipes.World.Prop(op.Prop); if (p.IsPlaced) return OpTarget(new ROp { Fixture = p.Fixture, Slot = p.Slot, Prop = -1, Prop2 = -1 }); return _world.PropPosition(_round.RoomPropOf[op.Prop]); }
            return _body.Position;
        }

        Vec3 StationOf(RecipeInstance inst) { foreach (var pl in _round.RecipeFixtures) if (pl.Instance == inst.Index) return pl.Position; return _body.Position; }

        /// <summary>
        /// Recipe sabotage against the leader: take a placed item out of their recipe and stash it (Remove), or re-open a closed lid
        /// (Reopen). Physical, visible, bounded by the board (cooldown, max actions per target, repair protection).
        /// </summary>
        bool TryPlanRecipeSabotage(float now)
        {
            var R = _round.Recipes; int target = ActorIds.None; float bestScore = float.MinValue;
            for (int r = 0; r < CompetitorRoster.SlotCount; r++)
            {
                if (r == Slot || !_round.Roster[r].IsActive) continue;
                float sc = _round.Scores[r].Total; if (sc > bestScore) { bestScore = sc; target = r; }
            }
            if (target == ActorIds.None) return false;
            RecipeInstance inst = null; foreach (var i in R.Instances) if (i.Owner == target && i.StepsDone > 0 && now >= i.ProtectedUntil && StationOf(i).FlatDistance(_body.Position) < 6f) inst = i;
            if (inst == null) return false;
            _sabotageReadyAt = now + Profile.SabotageCooldown * _rng.Range(0.8f, 1.3f);
            var w = R.World;
            foreach (var kv in inst.PropsByKind)
                foreach (var id in kv.Value)
                {
                    var p = w.Prop(id);
                    if (!p.IsPlaced || p.HeldBy != ActorIds.None) continue;
                    var f = w.Fixture(p.Fixture); if (f.HasDoor && !f.Open) continue;
                    int roomId = _round.RoomPropOf[id]; if (!_world.PropAvailable(roomId)) continue;
                    var pp = _world.PropPosition(roomId); if (pp.FlatDistance(_body.Position) > 6f) continue;
                    _planHide = true;
                    var room = _round.Room;
                    if (_round.Config.Continuous)
                    {   // hide it across the room it belongs to (house coordinates: mirror around that room's centre)
                        var c = HouseLayout.Offset(HouseLayout.RoomAt(pp));
                        _plannedTarget = HouseLayout.ClampToRoom(new Vec3(c.x - (pp.x - c.x) * 0.7f, 0.02f, c.z - (pp.z - c.z) * 0.7f));
                    }
                    else _plannedTarget = new Vec3(MathX.Clamp(-pp.x * 0.7f, -room.Width * 0.38f, room.Width * 0.38f), 0.02f, MathX.Clamp(-pp.z * 0.7f, -room.Depth * 0.38f, room.Depth * 0.38f));
                    DebugGoal = $"sabotage: take {_round.Roster[target].DisplayName}'s {p.Kind}";
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = roomId });
                    return true;
                }
            foreach (var fd in inst.Def.Fixtures)
            {
                if (!fd.HasDoor) continue; var f = w.Fixture(inst.Fixture(fd.Kind)); if (f.Open) continue;
                var op = new ROp { Actor = Slot, Verb = Verb.Open, Fixture = f.Id, Prop = -1, Prop2 = -1, Slot = -1 };
                DebugGoal = $"sabotage: open {_round.Roster[target].DisplayName}'s {fd.Kind}";
                _body.Execute(new AgentCommand { Type = AgentCommandType.RecipeOp, Op = op, PropId = -1, Target = OpTarget(op) });
                return true;
            }
            return false;
        }

        bool TryPlanSabotage(float now)
        {
            // Target the best-scoring rival (makes the game tense, never targets by human/bot identity).
            int target = ActorIds.None; float bestScore = float.MinValue;
            for (int r = 0; r < CompetitorRoster.SlotCount; r++)
            {
                if (r == Slot || !_round.Roster[r].IsActive) continue;
                float sc = _round.Scores[r].Total + _round.Board.ValidatedSteps(r);
                if (sc > bestScore) { bestScore = sc; target = r; }
            }
            if (target == ActorIds.None) return false;
            var rivalPos = _world.ActorPosition(target);
            _sabotageReadyAt = now + Profile.SabotageCooldown * _rng.Range(0.8f, 1.3f);
            // V5 primary sabotage: SWIPE an item the leader already put in place for their personal job and stash it elsewhere.
            // Physical, visible (owner is alerted, item glows in their colour) and counterable (restore within the foil window).
            var duty = _round.Board.BonusFor(target);
            if (duty != null && _rng.Value() < 0.7f)
            {
                foreach (var s2 in duty.Steps)
                {
                    if (!s2.Done || !_world.PropAvailable(s2.PropId)) continue;
                    bool already = false; foreach (var rec in _round.Board.Sabotage) if (rec.Saboteur == Slot && rec.PropId == s2.PropId) { already = true; break; }
                    if (already) continue; // anti-grief: never swipe the same item twice in a round
                    var pp = _world.PropPosition(s2.PropId);
                    if (pp.FlatDistance(_body.Position) > 5.5f) continue;
                    _planHide = true;
                    var room = _round.Room;
                    // Stash spot: across the room, but always reachable and inside bounds (never griefing).
                    _plannedTarget = new Vec3(MathX.Clamp(-pp.x * 0.8f, -room.Width * 0.38f, room.Width * 0.38f), 0.02f, MathX.Clamp(-pp.z * 0.8f, -room.Depth * 0.38f, room.Depth * 0.38f));
                    DebugGoal = $"sabotage: swipe {_round.Roster[target].DisplayName}'s {_round.Room.Prop(s2.PropId)?.Kind}";
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = s2.PropId });
                    return true;
                }
            }
            // Find a noisy free prop near me that is NOT part of an open step I need, to use as a trap.
            int trapProp = -1; float bestD = 4f;
            foreach (var p in _round.Room.Props)
            {
                if (NoiseModel.Loudness(p.Material) < 0.8f || !_world.PropAvailable(p.Id)) continue;
                var ch = _round.Board.ChoreForProp(p.Id);
                if (ch != null) { var st = ch.StepForProp(p.Id); if (st != null && st.Done) continue; } // never undo completed work for traps
                float d = _body.Position.FlatDistance(_world.PropPosition(p.Id));
                if (d < bestD) { bestD = d; trapProp = p.Id; }
            }
            if (trapProp >= 0 && _rng.Value() < 0.65f)
            {
                _planTrap = true; _plannedTrapTargetSlot = target;
                _plannedTarget = Vec3.MoveTowards(rivalPos, _round.Room.MomDoor, 0.6f); // in the rival's path, toward the door side
                DebugGoal = $"sabotage: trap for {_round.Roster[target].DisplayName}";
                _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = trapProp });
                return true;
            }
            // Otherwise "hide" a prop the rival is likely to need (moved to another reachable, valid spot — never out of bounds).
            foreach (var c in _round.Board.Chores)
            {
                if (c.State == ChoreState.Completed || c.IsBonus) continue;
                foreach (var s in c.Steps)
                {
                    if (s.Done || !_world.PropAvailable(s.PropId)) continue;
                    var pp = _world.PropPosition(s.PropId);
                    if (pp.FlatDistance(rivalPos) > 2.5f || pp.FlatDistance(_body.Position) > 3f) continue;
                    _planHide = true;
                    var room = _round.Room;
                    _plannedTarget = new Vec3(MathX.Clamp(-pp.x, -room.Width * 0.4f, room.Width * 0.4f), 0.02f, MathX.Clamp(-pp.z, -room.Depth * 0.4f, room.Depth * 0.4f));
                    DebugGoal = $"sabotage: hide {_round.Room.Prop(s.PropId)?.Kind}";
                    _body.Execute(new AgentCommand { Type = AgentCommandType.PickUp, PropId = s.PropId });
                    return true;
                }
            }
            return false;
        }
    }

    public enum PetKind { Cat, Dog }
    public enum PetState { Idle, Patrol, Investigate, Play, Startled, Rest, Calmed, Hungry }

    /// <summary>
    /// Independent pet NPC. Occasional, readable incidents (cat knocks a small item off a ledge, dog barks or carries a toy).
    /// Pets never target a specific sibling. Lures by siblings are recorded so provokers can be held responsible.
    /// </summary>
    public sealed class PetBrain
    {
        public readonly PetKind Kind;
        public readonly int ActorId;
        public PetState State { get; private set; } = PetState.Idle;
        public string DebugGoal = "";
        public int LuredBy = ActorIds.None;
        public int LureToyProp = -1;
        readonly DetRng _rng;
        readonly IAgentBody _body;
        readonly RoundController _round;
        readonly IWorldQuery _world;
        float _nextDecision, _nextIncidentAllowed, _calmUntil;

        public PetBrain(PetKind kind, IAgentBody body, RoundController round, IWorldQuery world, ulong seed)
        {
            Kind = kind; ActorId = kind == PetKind.Cat ? ActorIds.Cat : ActorIds.Dog;
            _body = body; _round = round; _world = world;
            _rng = new DetRng(seed ^ (ulong)(ActorId * 31337));
            _nextIncidentAllowed = 25f + _rng.Range(0f, 15f);
        }

        public void Calm(float now, float seconds) { _calmUntil = now + seconds; State = PetState.Calmed; LuredBy = ActorIds.None; if (_body.Busy) _body.Stop(); }

        /// <summary>A sibling squeaked/threw a pet toy near this pet.</summary>
        public void Lure(int sibling, int toyProp, float now)
        {
            if (now < _calmUntil) return;
            LuredBy = sibling; LureToyProp = toyProp; State = PetState.Play; // ledger lure is recorded by RoundController.Release
            if (_body.Busy) _body.Stop();
            _nextDecision = now;
        }

        public void Tick(float now)
        {
            if (!_round.IsPlayable) { if (_body.Busy) _body.Stop(); return; }
            if (_body.Busy || now < _nextDecision) return;
            _nextDecision = now + _rng.Range(1.5f, 4f);
            var room = _round.Room;
            if (now < _calmUntil) { State = PetState.Rest; DebugGoal = "resting"; _body.Execute(new AgentCommand { Type = AgentCommandType.Rest, Target = Kind == PetKind.Cat ? room.CatBed : room.DogBed, Duration = 3f }); return; }

            if (State == PetState.Play && LureToyProp >= 0)
            {
                // Chase the toy; if a knockable item sits near the toy, the pet may crash into it (provoker blamed).
                DebugGoal = "chasing a toy";
                var toyPos = _world.PropPosition(LureToyProp);
                int victim = NearestKnockable(toyPos, 0.9f);
                if (victim >= 0 && _rng.Value() < 0.5f)
                { _body.Execute(new AgentCommand { Type = AgentCommandType.Knock, PropId = victim, Target = _world.PropPosition(victim) }); _nextIncidentAllowed = now + 20f; }
                else _body.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = toyPos });
                if (_rng.Value() < 0.4f) { State = PetState.Patrol; LureToyProp = -1; }
                return;
            }

            // V12: a hungry pet goes to its bowl and waits there (whining is the host's noise event; the family has to bring food)
            int pi = Kind == PetKind.Cat ? 0 : 1; var bowl = Kind == PetKind.Cat ? room.CatBowl : room.DogBowl;
            if (_round.PetHunger[pi] >= RoundController.HungryAt && (bowl.x != 0f || bowl.z != 0f))
            {
                State = PetState.Hungry;
                // an empty bowl: wait eagerly BESIDE it (out of the way of the food); food in it: step up and eat
                var bed = Kind == PetKind.Cat ? room.CatBed : room.DogBed; var away = new Vec3(bowl.x - bed.x, 0, bowl.z - bed.z); float al = (float)Math.Sqrt(away.x * away.x + away.z * away.z);
                var beside = al > 0.05f ? new Vec3(bowl.x - away.z / al * 0.5f, 0, bowl.z + away.x / al * 0.5f) : new Vec3(bowl.x + 0.5f, 0, bowl.z);
                var spot = _round.BowlHasFood[pi] ? bowl : beside;
                DebugGoal = _round.BowlHasFood[pi] ? "eating" : "hungry - waiting beside the bowl";
                if (_body.Position.FlatDistance(spot) > 0.3f) _body.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = spot });
                else _body.Execute(new AgentCommand { Type = AgentCommandType.Rest, Target = spot, Duration = 1.5f });
                return;
            }
            float activity = _round.Config.PetActivity;
            if (now >= _nextIncidentAllowed && _rng.Value() < 0.22f * activity)
            {
                _nextIncidentAllowed = now + _rng.Range(28f, 48f) / Math.Max(0.3f, activity);
                if (Kind == PetKind.Cat)
                {
                    int victim = NearestKnockable(_body.Position, 6f, ledgeOnly: true);
                    if (victim >= 0) { State = PetState.Investigate; DebugGoal = "eyeing something on a ledge"; _body.Execute(new AgentCommand { Type = AgentCommandType.Knock, PropId = victim, Target = _world.PropPosition(victim) }); return; }
                }
                else
                {
                    State = PetState.Startled; DebugGoal = "woof!";
                    _body.Execute(new AgentCommand { Type = AgentCommandType.Bark, Duration = 1.2f });
                    return;
                }
            }
            State = PetState.Patrol; DebugGoal = "patrolling";
            _body.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = new Vec3(_rng.Range(-room.Width * 0.4f, room.Width * 0.4f), 0, _rng.Range(-room.Depth * 0.4f, room.Depth * 0.4f)) });
        }

        int NearestKnockable(Vec3 from, float range, bool ledgeOnly = false)
        {
            int best = -1; float bd = range;
            foreach (var p in _round.Room.Props)
            {
                if (!p.PetKnockable || !_world.PropAvailable(p.Id)) continue;
                var pos = _world.PropPosition(p.Id);
                if (ledgeOnly && pos.y < 0.3f) continue;
                float d = from.FlatDistance(pos);
                if (d < bd) { bd = d; best = p.Id; }
            }
            return best;
        }
    }
}
