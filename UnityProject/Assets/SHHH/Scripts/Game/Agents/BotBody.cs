using SHHH.Core;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// Physical body of a sibling bot (also used by the autopilot test player). Walks on the NavMesh, carries REAL props,
    /// places them on real anchors (validated by AnchorFixture like a human placement) and can fumble them, which makes
    /// real physics impacts and therefore real, attributable noise. No teleporting, no direct score edits.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BotBody : MonoBehaviour, IAgentBody
    {
        public int Slot;
        public BotProfile Profile;
        public SiblingAvatar Avatar;
        NavMeshAgent _agent;
        ActorTag _tag;
        AgentCommand _cmd;
        enum Stage { Idle, Travel, Act }
        Stage _stage;
        float _actTimer, _stuckTimer; Vector3 _lastPos;
        PropBody _carried;
        public bool Busy { get; private set; }
        public bool LastSucceeded { get; private set; }
        public int HeldProp => _carried != null ? _carried.Spec.Id : -1;
        /// <summary>Walking speed (m/s) for network replication of the walk cycle.</summary>
        public float CurrentSpeed => _agent != null && _agent.enabled ? _agent.velocity.magnitude : 0f;
        public Vec3 Position => VisualFactory.C(transform.position);

        public void Init(int slot, BotProfile profile, SiblingAvatar avatar)
        {
            Slot = slot; Profile = profile; Avatar = avatar;
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 1.15f * profile.SpeedFactor; _agent.angularSpeed = 540f; _agent.acceleration = 6f;
            _agent.radius = 0.22f; _agent.height = 1.3f; _agent.stoppingDistance = 0.05f; _agent.autoBraking = true;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;   // V12: steers around players (PersonalSpace)
            _tag = gameObject.AddComponent<ActorTag>(); _tag.ActorId = slot;
            var cap = gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 0.65f, 0); cap.height = 1.3f; cap.radius = 0.2f;
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            gameObject.layer = Layers.Agents;
        }

        public void Execute(AgentCommand cmd)
        {
            _cmd = cmd; Busy = true; LastSucceeded = false; _actTimer = 0f; _stuckTimer = 0f; _lastPos = transform.position;
            var room = GameServices.Room;
            Vector3 dest = VisualFactory.V(cmd.Target);
            switch (cmd.Type)
            {
                case AgentCommandType.PickUp:
                case AgentCommandType.DisarmTrap:
                {
                    var p = room != null ? room.Prop(cmd.PropId) : null;
                    if (p == null) { Finish(false); return; }
                    dest = p.transform.position; break;
                }
                case AgentCommandType.PlaceAt:
                {
                    var a = room != null ? room.Anchor(cmd.AnchorId) : null;
                    if (a == null) { Finish(false); return; }
                    dest = a.transform.position; break;
                }
                case AgentCommandType.ArmTrapAt:
                {
                    var a = room != null ? room.NearestOpenAnchor(dest, _carried) : null;
                    if (a != null) { dest = a.transform.position; _cmd.AnchorId = a.Spec.Id; } else _cmd.AnchorId = -1;
                    break;
                }
                case AgentCommandType.Wait:
                case AgentCommandType.Emote:
                    _stage = Stage.Act; _agent.ResetPath();
                    if (!string.IsNullOrEmpty(cmd.Emote)) Avatar?.ShowStatus(cmd.Emote == "shh" ? "Shh!" : cmd.Emote, cmd.Duration);
                    return;
            }
            if (NavMesh.SamplePosition(new Vector3(dest.x, 0, dest.z), out var hit, 1.6f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
                _stage = Stage.Travel;
            }
            else { _stage = Stage.Act; }
        }

        public void Stop()
        {
            Busy = false; _stage = Stage.Idle;
            if (_agent != null && _agent.isOnNavMesh) _agent.ResetPath();
        }

        void Finish(bool ok) { Busy = false; LastSucceeded = ok; _stage = Stage.Idle; Avatar?.SetReach(null, 0); }
        float _tilt, _pourT, _missT, _recvLast, _pour0 = -1f, _recv0; Vector3? _holdAt; LiquidContainer _receiver; bool _pourDone; MechFurniture _bin;
        /// <summary>QA: where the last bot pour landed.</summary>
        public string LastPour = "";

        float ReachDistance => _cmd.Type == AgentCommandType.MoveTo ? 0.2f : 0.6f;

        void Update()
        {
            if (Avatar != null) Avatar.SetMotion(_agent != null ? _agent.velocity.magnitude : 0f, _carried != null);
            if (_carried != null) FollowSocket();
            if (!Busy) return;
            if (_stage == Stage.Travel)
            {
                Vector3 goal = _agent.destination;
                float d = Vector3.Distance(Flat(transform.position), Flat(goal));
                if (d <= ReachDistance || (!_agent.pathPending && _agent.remainingDistance <= ReachDistance))
                { _stage = Stage.Act; _agent.ResetPath(); _actTimer = 0f; }
                else
                {
                    _stuckTimer += Time.deltaTime;
                    if (_stuckTimer > 1.5f)
                    {
                        if (Vector3.Distance(_lastPos, transform.position) < 0.15f) { Finish(false); return; }
                        _stuckTimer = 0f; _lastPos = transform.position;
                    }
                }
                return;
            }
            _actTimer += Time.deltaTime;
            Act();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        void Face(Vector3 p) { var d = Flat(p - transform.position); if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 0.25f); }

        void Act()
        {
            var round = GameServices.Round; var room = GameServices.Room;
            if (round == null || room == null) { Finish(false); return; }
            switch (_cmd.Type)
            {
                case AgentCommandType.MoveTo: Finish(true); break;
                case AgentCommandType.Wait: case AgentCommandType.Emote: case AgentCommandType.Rest:
                    if (_actTimer >= Mathf.Max(0.2f, _cmd.Duration)) Finish(true); break;
                case AgentCommandType.PickUp:
                {
                    var p = room.Prop(_cmd.PropId);
                    if (p == null || p.IsHeld || p.Reserved || Vector3.Distance(Flat(p.transform.position), Flat(transform.position)) > 1.1f) { Finish(false); break; }
                    Face(p.transform.position);
                    Avatar?.SetReach(p.transform, Mathf.Clamp01(_actTimer / 0.4f));
                    if (_actTimer < 0.55f / Profile.SpeedFactor) break;
                    if (!round.Grab(Slot, p.Spec.Id)) { Finish(false); break; }
                    _carried = p; p.OnGrabbed(Slot);
                    p.Body.isKinematic = true;
                    p.Body.interpolation = RigidbodyInterpolation.None; // carried kinematically: write poses directly
                    p.Col.enabled = false; // carried at chest height: no phantom collisions while walking
                    Finish(true); break;
                }
                case AgentCommandType.PlaceAt:
                case AgentCommandType.PutDown:
                case AgentCommandType.ArmTrapAt:
                {
                    if (_carried == null || _carried.Spec.Id != _cmd.PropId) { Finish(false); break; }
                    Vector3 target;
                    AnchorFixture anchor = null;
                    if (_cmd.Type == AgentCommandType.PlaceAt) { anchor = room.Anchor(_cmd.AnchorId); target = anchor != null ? anchor.PlacePoint : transform.position + transform.forward * 0.4f; }
                    else if (_cmd.Type == AgentCommandType.ArmTrapAt && _cmd.AnchorId >= 0) { var a = room.Anchor(_cmd.AnchorId); target = a.PlacePoint + room.EdgeOffset(a.PlacePoint, 0.13f); }
                    else target = FloorPointAhead();
                    Face(target);
                    Avatar?.SetReach(_carried.transform, 1f);
                    var c = round.Board.ChoreForProp(_cmd.PropId);
                    float work = (c != null ? c.Template.WorkSecondsPerStep * 0.45f : 0.9f) / Profile.SpeedFactor;
                    if (_actTimer < work) break;
                    if (anchor != null) room.ResolveTrapsNear(anchor.transform.position, Slot, Profile);
                    ReleaseCarried(target, _cmd.Careful || _cmd.Type == AgentCommandType.ArmTrapAt);
                    if (_cmd.Type == AgentCommandType.ArmTrapAt) { var p = room.Prop(_cmd.PropId); if (p != null) StartCoroutine(ArmLater(p)); }
                    Finish(true); break;
                }
                case AgentCommandType.RecipeOp: ActRecipe(round, room); break;
                case AgentCommandType.PourAt:
                case AgentCommandType.DropInto:
                {
                    // V12 care: arm out over the target, then tip (pour) or open the hand (drop) - the rest is ordinary physics
                    if (_carried == null || _carried.Spec.Id != _cmd.PropId) { _holdAt = null; _tilt = 0f; Finish(false); break; }
                    if (_actTimer <= Time.deltaTime * 1.5f) { _holdAt = null; _receiver = null; _pourDone = false; _pourT = 0f; _tilt = 0f; _missT = 0f; _recvLast = 0f; _bin = null; _pour0 = -1f; }   // a new hold: start above the target
                    var tgt = room.transform.TransformPoint(VisualFactory.V(_cmd.Target)); Face(tgt);
                    var lc = Vessels.Of(_carried); float lift = _cmd.Type == AgentCommandType.PourAt ? 0.42f : 0.62f;
                    if (!_holdAt.HasValue) _holdAt = tgt + Vector3.up * lift;

                    Avatar?.SetReach(_carried.transform, 1f);
                    if (_cmd.Type == AgentCommandType.PourAt)
                    {
                        // pour like a person: spout over the bowl first, tip until it flows, stop when the bowl is nearly full or 3 s passed
                        if (_receiver == null) { float bd = 0.35f; foreach (var c in LiquidContainer.All) { if (c == null || c == lc) continue; var dd = Flat(c.transform.position - tgt).magnitude; if (dd < bd) { bd = dd; _receiver = c; } } }
                        if (_receiver != null && _recvLast <= 0f) _recvLast = _receiver.Received;
                        bool full = _receiver != null && (_receiver.Amount >= _receiver.Capacity * 0.85f || (_pour0 >= 0f && _receiver.Received - _recv0 >= 0.1f)), empty = lc == null || lc.Amount <= 0.002f;   // a meal (100 g) in the bowl
                        if (lc != null)
                        {
                            var lip = lc.transform.TransformPoint(new Vector3(0, lc.RimY, 0)); var dl = tgt - lip; dl.y = 0f;
                            _holdAt = _holdAt.Value + Vector3.ClampMagnitude(dl, 0.02f);
                            bool aligned = dl.magnitude < 0.04f;
                            if (_pourDone) _tilt = Mathf.Max(0f, _tilt - 200f * Time.deltaTime);
                            else if (aligned && !lc.Pouring) _tilt = Mathf.Min(120f, _tilt + 60f * Time.deltaTime);
                            else if (aligned && lc.Pouring) _tilt = Mathf.Min(95f, _tilt + 12f * Time.deltaTime);   // flowing: tip a little further for a steady stream
                            // missing the bowl (the pet in the way, a bad angle): stop, straighten, try again later - never empty the bag on the floor
                            // (measured on what the bowl RECEIVED: a pet may already be eating it)
                            if (lc.Pouring && _receiver != null)
                            {
                                if (_pour0 < 0f) { _pour0 = lc.PouredOut; _recv0 = _receiver.Received; }
                                float poured = lc.PouredOut - _pour0, got = _receiver.Received - _recv0;
                                if (poured > 0.03f && got < poured * 0.3f) _pourDone = true;
                            }
                            if (lc.Pouring) { _pourT += Time.deltaTime; LastPour = $"stream lands on {lc.StreamLanding}; poured {(lc.PouredOut - Mathf.Max(0f, _pour0)) * 1000f:0} g, bowl received {(_receiver != null ? (_receiver.Received - _recv0) * 1000f : 0f):0} g, tilt {_tilt:0}, bag left {lc.Amount * 1000f:0} g"; }
                            if (!_pourDone && (full || empty || _pourT > 7f || _actTimer > 14f)) _pourDone = true;
                        }
                        if (!_pourDone || _tilt > 0.5f) break;
                        _tilt = 0f; _holdAt = null; _receiver = null; _pourDone = false; _pourT = 0f; Finish(true); break;
                    }
                    // a bin with a lid: lift it first (a house mechanism - everyone sees it), drop the trash in, close it gently
                    if (_bin == null) { float bd = 0.8f; foreach (var f in MechFurniture.All) { if (f == null || f.Kind != "bin") continue; float dd = Flat(f.transform.position - tgt).magnitude; if (dd < bd) { bd = dd; _bin = f; } } }
                    if (_bin != null && _bin.Lid != null && _bin.Lid.Opening < 0.8f) { if (_bin.Lid.Drive == null) _bin.Lid.ScriptedMove(Slot, 1f); if (_actTimer < 6f) break; }
                    if (_actTimer < 0.9f / Profile.SpeedFactor) break;
                    var p = _carried; _carried = null; _holdAt = null;
                    if (_bin != null && _bin.Lid != null) StartCoroutine(CloseLidLater(_bin.Lid));
                    p.Col.enabled = true; p.Body.interpolation = RigidbodyInterpolation.Interpolate; p.Body.isKinematic = false; p.Body.linearVelocity = Vector3.zero;
                    p.OnReleased(Slot, Vector3.zero, false); round.Release(Slot, p.Spec.Id, 0.1f, false);
                    Finish(true); break;
                }
                case AgentCommandType.DisarmTrap:
                {
                    var p = room.Prop(_cmd.PropId);
                    if (p == null) { Finish(false); break; }
                    Face(p.transform.position);
                    if (_actTimer < 0.9f) break;
                    if (p.ArmedTrapId >= 0)
                    {
                        round.DisarmTrap(p.ArmedTrapId, Slot); p.ArmedTrapId = -1; RoomFx.TrapCleared(p);
                        Avatar?.ShowStatus("Phew!", 1.2f);
                    }
                    Finish(true); break;
                }
                default: Finish(true); break;
            }
        }

        /// <summary>
        /// V7 recipe step with the body: slots get a real careful release onto the slot (the fixture snaps and reports it, exactly as
        /// for a hand), joins are made while carrying and the item is set down on its partner; lids, controls, wiping, folding and
        /// straightening are a visible reach + work time and then the same intent a poke/rub produces.
        /// </summary>
        void ActRecipe(RoundController round, RoomRuntime room)
        {
            var op = _cmd.Op; op.Actor = Slot; op.EventId = 0;
            var target = VisualFactory.V(_cmd.Target);
            Face(target);
            Avatar?.SetReach(_carried != null ? _carried.transform : null, Mathf.Clamp01(_actTimer / 0.4f));
            float work = (op.Verb == Verb.Stroke || op.Verb == Verb.Fold || op.Verb == Verb.Fill ? 1.6f : 0.7f) / Profile.SpeedFactor;
            // V10: scrubbing stains takes time per stained cell (the same Scrub rule a player's sponge contact feeds)
            if (op.Verb == Verb.Scrub && ulong.TryParse(op.Arg ?? "", System.Globalization.NumberStyles.HexNumber, null, out var cells)) work = (0.6f + 0.12f * RecipeWorld.CountBits(cells)) / Profile.SpeedFactor;
            if (_actTimer < work) return;
            if (round.Recipes == null) { Finish(false); return; }
            var rt = RecipeRuntime.I;
            switch (op.Verb)
            {
                case Verb.PlaceInSocket:
                {
                    if (_carried == null || !round.RecipePropOf.TryGetValue(_carried.Spec.Id, out var rid) || rid != op.Prop) { Finish(false); return; }
                    var fv = rt != null ? rt.FixtureView(op.Fixture) : null;
                    var slot = fv != null ? fv.SlotPoint(op.Slot) : target;
                    ReleaseCarried(slot + Vector3.up * 0.004f, true); // the fixture's snap validates + reports PlaceInSocket (+ Align)
                    Finish(true); return;
                }
                case Verb.Pair: case Verb.Attach:
                {
                    var err = RecipeRuntime.Submit(op);
                    if (err == null && _carried != null && round.RecipePropOf.TryGetValue(_carried.Spec.Id, out var rid))
                    {
                        int other = rid == op.Prop ? op.Prop2 : op.Prop;
                        var ob = rt != null ? rt.Body(other) : null;
                        if (ob != null)
                        {
                            var carried = _carried;
                            ReleaseCarried(ob.Col.bounds.center + Vector3.up * (ob.Col.bounds.extents.y + 0.01f), true);
                            rt.RegisterJoin(rid, other);
                            Avatar?.ShowStatus("click!", 0.8f);
                        }
                    }
                    Finish(err == null); return;
                }
                default:
                {
                    if (op.Verb == Verb.Align && op.Arg != "tilt" && rt != null && op.Prop >= 0)
                    {
                        // straighten it by hand: square the item to its fixture (a touch, not a throw)
                        var b = rt.Body(op.Prop); var rp = round.Recipes.World.Prop(op.Prop); var fv = rp.IsPlaced ? rt.FixtureView(rp.Fixture) : null;
                        if (b != null && fv != null && !b.IsHeld)
                        {
                            float rel = Mathf.DeltaAngle(fv.transform.eulerAngles.y, b.transform.eulerAngles.y);
                            var rot = Quaternion.Euler(0, fv.transform.eulerAngles.y + Mathf.Round(rel / 90f) * 90f, 0);
                            b.transform.rotation = rot; b.Body.rotation = rot; b.Body.angularVelocity = Vector3.zero;
                        }
                    }
                    var err = RecipeRuntime.Submit(op);
                    if (err != null) Debug.Log($"[SHHH][bot {Slot}] recipe {op.Verb} rejected: {err}");
                    else if (op.Verb == Verb.Stroke || op.Verb == Verb.Scrub) RoomFx.Burst("bubbles", target + Vector3.up * 0.05f, 0.12f, 1f);
                    Finish(err == null); return;
                }
            }
        }

        System.Collections.IEnumerator CloseLidLater(HingedPart lid) { yield return new WaitForSeconds(1.2f); if (lid != null && !lid.IsHeld) lid.ScriptedMove(Slot, 0f); }

        System.Collections.IEnumerator ArmLater(PropBody p)
        {
            yield return new WaitForSeconds(0.6f);
            if (p != null && !p.IsHeld && p.ArmedTrapId < 0) { p.ArmAsTrap(Slot); Avatar?.ShowStatus("hehe", 1.5f); }
        }

        Vector3 FloorPointAhead()
        {
            var p = VisualFactory.V(_cmd.Target);
            if (p == Vector3.zero) p = transform.position + transform.forward * 0.45f;
            return new Vector3(p.x, 0.03f, p.z);
        }

        void FollowSocket()
        {
            if (Avatar == null || Avatar.CarrySocket == null) return;
            var t = _carried.transform;
            var pos = Vector3.Lerp(t.position, Avatar.CarrySocket.position - (t.rotation * Vector3.up) * 0.05f, 0.35f);
            var rot = Quaternion.Slerp(t.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0) * Quaternion.AngleAxis(_tilt, Vector3.right), 0.3f);
            if (_holdAt.HasValue) pos = Vector3.Lerp(t.position, _holdAt.Value, 0.25f);   // V12: arm out over a bowl / bin
            Teleport(_carried, pos, rot);
        }

        void ReleaseCarried(Vector3 target, bool careful)
        {
            var p = _carried; _carried = null;
            var round = GameServices.Round;
            p.Col.enabled = true;
            p.ReservedUntil = Time.time + 1.5f;
            if (careful)
            {
                Teleport(p, target + Vector3.up * 0.005f, Quaternion.Euler(0, transform.eulerAngles.y, 0));
                p.Body.interpolation = RigidbodyInterpolation.Interpolate;
                p.Body.isKinematic = false; p.Body.linearVelocity = Vector3.zero; p.Body.angularVelocity = Vector3.zero;
                p.IgnoreImpactsUntil = Time.time + 0.3f;
                p.OnReleased(Slot, Vector3.zero, false);
                round.Release(Slot, p.Spec.Id, 0.1f, false);
            }
            else
            {
                // Fumble: drops from hand height with a little sideways slip — real physics decides how loud it is.
                // Mostly a clumsy clack (low set-down); one in three is a genuine slip from the hands.
                bool realDrop = Random.value < 0.33f;
                Vector3 slip = Random.insideUnitSphere * (realDrop ? 0.08f : 0.03f); slip.y = 0;
                Teleport(p, target + Vector3.up * (realDrop ? 0.3f : Random.Range(0.06f, 0.12f)) + slip, p.transform.rotation);
                p.Body.interpolation = RigidbodyInterpolation.Interpolate;
                p.Body.isKinematic = false;
                p.Body.linearVelocity = new Vector3(slip.x * 6f, realDrop ? -1.2f : -0.6f, slip.z * 6f);
                p.Body.angularVelocity = Random.insideUnitSphere * 4f;
                p.OnReleased(Slot, p.Body.linearVelocity, false);
                round.Release(Slot, p.Spec.Id, 1.2f, false);
                Avatar?.ShowStatus("Oops!", 1.2f);
            }
        }

        /// <summary>Moves a prop's transform AND its physics body (transform writes alone are overridden by interpolation).</summary>
        static void Teleport(PropBody p, Vector3 pos, Quaternion rot)
        {
            p.transform.SetPositionAndRotation(pos, rot);
            p.Body.position = pos; p.Body.rotation = rot;
        }

        /// <summary>Drop anything carried gently (round frozen, bot eliminated...).</summary>
        public void DropCarriedGently()
        {
            if (_carried == null) return;
            ReleaseCarried(FloorPointAhead(), true);
        }
    }
}
