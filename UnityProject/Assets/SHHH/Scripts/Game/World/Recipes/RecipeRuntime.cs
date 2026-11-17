using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V7 Gate 4 physical layer: turns hand physics in a built room into <see cref="ROp"/> intents for the authoritative
    /// <see cref="RecipeBoard"/> (via <see cref="RoundController.RecipeAction"/>, journaled to every client).
    /// <list type="bullet">
    /// <item>Grab / release / push are already mapped by the round (PickUp / PutDown / Align tilt).</item>
    /// <item>Slots snap released items (<see cref="RecipeFixtureView"/>); lids and controls are poke buttons.</item>
    /// <item>Wipe: rub a hand (or a held cloth) over a smudged fixture — every 0.3 m of rubbing removes one smudge.</item>
    /// <item>Fold / fill / brush (stage steps): hold the item and rub it with your other hand (0.35 m of relative motion per stage).</item>
    /// <item>Pair / attach: hold one item against its partner for 0.25 s; pulling them 15 cm apart unpairs / detaches.</item>
    /// <item>Knocked out of a slot without a grab: reported as moved by the last toucher (sabotage evidence, or a pet incident).</item>
    /// </list>
    /// Gesture detection runs on the authority only (local rig + remote humans' replicated hand poses), so nothing is counted
    /// twice; clients only send poke-button intents (<see cref="GameServices.SendRecipeIntent"/>).
    /// </summary>
    public sealed class RecipeRuntime : MonoBehaviour
    {
        public RoomRuntime Room { get; private set; }
        public RoundController Round { get; private set; }
        public RecipeWorld World => Round?.Recipes?.World;
        public RecipeBoard Board => Round?.Recipes;
        readonly List<RecipeFixtureView> _fixtures = new List<RecipeFixtureView>();
        readonly Dictionary<int, PropBody> _byRecipeProp = new Dictionary<int, PropBody>();
        readonly List<PropBody> _bodies = new List<PropBody>();
        readonly List<RecipeClipboard> _cards = new List<RecipeClipboard>();
        public IReadOnlyList<PropBody> Bodies => _bodies;
        public IReadOnlyList<RecipeFixtureView> Fixtures => _fixtures;
        public static RecipeRuntime I { get; private set; }

        // gesture accumulators
        readonly Dictionary<(int fixture, int actor), float> _rub = new Dictionary<(int, int), float>();
        readonly Dictionary<(int fixture, int actor, int hand), Vector3> _rubLast = new Dictionary<(int, int, int), Vector3>();
        readonly Dictionary<(int prop, int hand), Vector3> _stageLast = new Dictionary<(int, int), Vector3>();
        readonly Dictionary<int, float> _stageAcc = new Dictionary<int, float>();
        readonly Dictionary<(int, int), float> _touch = new Dictionary<(int, int), float>();
        readonly Dictionary<int, Vector3> _baseScale = new Dictionary<int, Vector3>();
        readonly HashSet<int> _ignored = new HashSet<int>();
        readonly Dictionary<int, (int leader, Vector3 offset, Quaternion rot)> _follow = new Dictionary<int, (int, Vector3, Quaternion)>();
        /// <summary>QA: log join probes (gap / touch time) twice a second.</summary>
        public static bool DebugJoins; static float _dbgNext;
        public const float RubPerStroke = 0.3f, RubPerStage = 0.35f, JoinHold = 0.25f, JoinGap = 0.045f, // held together within ~4 cm (tracked hands rarely press flush)
            SplitDistance = 0.15f, DislodgeDistance = 0.1f;

        public void Bind(RoomRuntime room, RoundController round)
        {
            I = this; Room = room; Round = round;
            var root = new GameObject("Recipes").transform; root.SetParent(room.transform, false);
            ClearStations(room, round);
            foreach (var kv in round.RecipePropOf)
            {
                var b = room.Prop(kv.Key); if (b == null) continue; _byRecipeProp[kv.Value] = b; _bodies.Add(b);
                var rp = round.Recipes.World.Prop(kv.Value);
                if (rp != null && rp.StrokeCap > 0 && b.GetComponent<WashableView>() == null) b.gameObject.AddComponent<WashableView>().Init(this, kv.Value);
            }
            SettleSpawns();
            // fixture item heights decide chest wall heights (a closed lid must clear its contents)
            foreach (var pl in round.RecipeFixtures)
            {
                var inst = round.Recipes.Instances[pl.Instance];
                var decl = inst.Def.Fixtures.Find(f => f.Kind == pl.Kind);
                float h = 0.06f;
                foreach (var s in inst.Def.Steps) if (s.Kind == StepKind.Place && s.Fixture == pl.Kind) foreach (var id in inst.Props(s.PropKind)) { var b = Body(id); if (b != null) h = Mathf.Max(h, Mathf.Min(b.Col.bounds.size.y, b.Col.bounds.size.x)); }
                var g = new GameObject("fixture"); g.transform.SetParent(root, false);
                var v = g.AddComponent<RecipeFixtureView>();
                v.Build(this, pl, decl, inst, DutyVisuals.ColorFor(round, inst.Owner), h);
                _fixtures.Add(v);
            }
            // a clipboard per recipe at its station (everyone can read every job - and see what to mess with)
            foreach (var inst in round.Recipes.Instances)
            {
                int st = -1; foreach (var pl in round.RecipeFixtures) if (pl.Instance == inst.Index) { st = pl.Station; break; }
                if (st < 0 || !round.RecipeStations.TryGetValue(st, out var station)) continue;
                var g = new GameObject("clipboard_" + inst.Def.Id); g.transform.SetParent(root, false);
                var c = g.AddComponent<RecipeClipboard>(); c.Build(this, inst, station); _cards.Add(c);
            }
            Debug.Log($"[SHHH][recipe] runtime bound: {round.Recipes.Instances.Count} recipes, {_fixtures.Count} fixtures, {_bodies.Count} props");
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>
        /// Real art is thicker than the layout's nominal sizes: lift any recipe item that would start inside another (piles, items
        /// placed in slots on top of each other) so it rests on top, and keep the first settle silent (no spawn "crash" noise).
        /// </summary>
        void SettleSpawns()
        {
            Physics.SyncTransforms();
            var order = new List<PropBody>(_bodies); order.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            var settled = new List<PropBody>();
            foreach (var b in order)
            {
                b.IgnoreImpactsUntil = Time.time + 2.5f;
                var bb = b.Col.bounds; float lift = 0f;
                foreach (var o in settled)
                {
                    var ob = o.Col.bounds;
                    bool overlapXZ = bb.min.x < ob.max.x - 0.005f && bb.max.x > ob.min.x + 0.005f && bb.min.z < ob.max.z - 0.005f && bb.max.z > ob.min.z + 0.005f;
                    if (overlapXZ && bb.min.y + lift < ob.max.y) lift = ob.max.y + 0.002f - bb.min.y;
                }
                if (lift > 0f) { var p = b.transform.position + Vector3.up * lift; b.transform.position = p; b.Body.position = p; Physics.SyncTransforms(); }
                settled.Add(b);
            }
        }

        /// <summary>
        /// The connected house is dressed (plates, cutlery, bread on the dining table...). A recipe station's top must be clear, so
        /// small dressing objects resting on a station are hidden for this round (renderers + colliders off; restored with the room).
        /// Large furniture (the table itself, chairs) is never touched.
        /// </summary>
        /// <summary>Continuous match: the house round stages jobs in several rooms - clear the stations of every built room in use.</summary>
        public static int ClearStations(RoomRuntime room, RoundController round)
        {
            if (round == null || !round.Config.Continuous) return ClearStations(room, round != null ? round.Room.Id : RoomId.LivingRoom);
            int n = 0; int jobs = Mathf.Clamp(round.Config.JobsPerSeat, 1, HouseLayout.Rooms.Length);
            for (int k = 0; k < jobs; k++) n += ClearStations(room, HouseLayout.Rooms[k], HouseLayout.Offset(HouseLayout.Rooms[k]));
            return n;
        }

        static RecipeStation[] StationsAt(RoomId roomId, Vec3 off) { var raw = RecipeLayout.Stations(roomId); var o = new RecipeStation[raw.Length]; for (int i = 0; i < raw.Length; i++) o[i] = HouseLayout.Shift(raw[i], off); return o; }

        public static int ClearStations(RoomRuntime room, RoomId roomId) => ClearStations(room, roomId, new Vec3(0, 0, 0));

        public static int ClearStations(RoomRuntime room, RoomId roomId, Vec3 off)
        {
            int hidden = 0; var done = new HashSet<GameObject>();
            foreach (var st in StationsAt(roomId, off))
            {
                var c = room.transform.TransformPoint(VisualFactory.V(st.Center)) + Vector3.up * 0.2f;
                var half = st.AlongZ ? new Vector3(st.Depth * 0.5f, 0.19f, st.Length * 0.5f) : new Vector3(st.Length * 0.5f, 0.19f, st.Depth * 0.5f);
                foreach (var col in Physics.OverlapBox(c, half, room.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (col.GetComponentInParent<PropBody>() != null || col.GetComponentInParent<HandInteractor>() != null || col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) continue;
                    var b = col.bounds; if (b.size.x > 0.6f || b.size.z > 0.6f || b.size.y > 0.5f || b.min.y < c.y - 0.2f - 0.02f) continue; // furniture / table top itself
                    var go = col.gameObject; if (!done.Add(go)) continue;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = false;
                    foreach (var cc in go.GetComponentsInChildren<Collider>()) cc.enabled = false;
                    hidden++;
                }
            }
            // render-only dressing (no collider) under the room hierarchy
            var boxes = new List<Bounds>();
            foreach (var st in StationsAt(roomId, off))
            {
                var c = room.transform.TransformPoint(VisualFactory.V(st.Center)) + Vector3.up * 0.2f;
                boxes.Add(new Bounds(c, st.AlongZ ? new Vector3(st.Depth, 0.38f, st.Length) : new Vector3(st.Length, 0.38f, st.Depth)));
            }
            foreach (var r in room.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.GetComponentInParent<PropBody>() != null || r.GetComponentInParent<RecipeFixtureView>() != null || r.GetComponentInParent<RecipeClipboard>() != null || r.GetComponentInParent<BotBody>() != null) continue;
                var b = r.bounds; if (b.size.x > 0.6f || b.size.z > 0.6f || b.size.y > 0.5f) continue;
                foreach (var box in boxes)
                    if (box.Contains(b.center) && b.min.y > box.min.y - 0.01f) { r.enabled = false; foreach (var cc in r.GetComponents<Collider>()) cc.enabled = false; hidden++; break; }
            }
            if (hidden > 0) Debug.Log($"[SHHH][recipe] cleared {hidden} dressing objects from recipe station tops");
            return hidden;
        }

        public PropBody Body(int recipePropId) => _byRecipeProp.TryGetValue(recipePropId, out var b) ? b : null;
        public RProp RProp(PropBody b) => b != null && Round != null && Round.RecipePropOf.TryGetValue(b.Spec.Id, out var id) ? World.Prop(id) : null;
        public RecipeFixtureView FixtureViewOf(int instance, string kind) { foreach (var f in _fixtures) if (f.Placement.Instance == instance && f.Placement.Kind == kind) return f; return null; }
        /// <summary>Authority: is any tracked hand (local, remote, bot reach) within <paramref name="r"/> of a point? (levers, handles)</summary>
        public bool HandNear(Vector3 p, float r, out int actor)
        {
            actor = ActorIds.None; foreach (var h in _hands) if (h.Hand < 2 && Vector3.Distance(h.Pos, p) <= r) { actor = h.Actor; return true; }
            foreach (var h in _reach) if (Vector3.Distance(h.Pos, p) <= r) { actor = h.Actor; return true; }
            return false;
        }

        /// <summary>
        /// V13 (owner, Quest c22: "the soap is not working, it should work with any kind of gesture to press but it's not pressing at all"):
        /// is any part of a hand (palm, index fingertip, pinch point) over a press surface - within <paramref name="radius"/> of its centre
        /// horizontally and from <paramref name="below"/> under to <paramref name="above"/> over its top? A finger poke, a flat palm, a fist
        /// or a pinch all count. (The pump used to need the PALM CENTRE within 3.5 cm of the pad part's pivot - a real hand never got there.)
        /// </summary>
        public bool HandPress(Vector3 top, float radius, float below, float above, out int actor)
        {
            actor = ActorIds.None;
            bool In(Vector3 q) { var d = q - top; return new Vector2(d.x, d.z).magnitude <= radius && d.y >= -below && d.y <= above; }
            foreach (var h in _hands) if (h.Hand < 2 && In(h.Pos)) { actor = h.Actor; return true; }
            foreach (var h in _reach) if (In(h.Pos)) { actor = h.Actor; return true; }
            return false;
        }
        /// <summary>Nearest tracked hand within <paramref name="r"/> of a point, with its position (lever/handle drags).</summary>
        public bool HandNear(Vector3 p, float r, out int actor, out Vector3 pos)
        {
            actor = ActorIds.None; pos = p; float best = r;
            foreach (var h in _hands) { if (h.Hand >= 2) continue; float d = Vector3.Distance(h.Pos, p); if (d <= best) { best = d; actor = h.Actor; pos = h.Pos; } }
            foreach (var h in _reach) { float d = Vector3.Distance(h.Pos, p); if (d <= best) { best = d; actor = h.Actor; pos = h.Pos; } }
            return actor != ActorIds.None;
        }
        public RecipeFixtureView FixtureView(int fixtureId) { foreach (var f in _fixtures) if (f.Placement.FixtureId == fixtureId) return f; return null; }
        public bool IsFollower(RProp p) => p != null && _follow.ContainsKey(p.Id);

        /// <summary>Send a recipe intent: applied on the authority (journaled), or sent to the host from a client.</summary>
        public static string Submit(ROp op)
        {
            var round = GameServices.Round; if (round == null || round.Recipes == null) return "no recipe round";
            if (GameServices.IsAuthority) return round.RecipeAction(op);
            if (GameServices.SendRecipeIntent != null && GameServices.SendRecipeIntent(op)) return null;
            return "not connected to the host";
        }

        // ------------------------------------------------------------------ hands the authority can see
        struct HandPt { public int Actor, Hand; public Vector3 Pos; public PropBody Tool; }
        readonly List<HandPt> _hands = new List<HandPt>();
        /// <summary>V13: extra touch points of the local hands (index fingertip, pinch point) - only for touch checks (press, lever), never
        /// for wiping (which keeps using the palm).</summary>
        readonly List<HandPt> _reach = new List<HandPt>();

        void CollectHands()
        {
            _hands.Clear(); _reach.Clear();
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null;
            if (rig != null && Round.Roster[GameServices.LocalSlot].IsActive)
            {
                for (int k = 0; k < 2; k++)
                {
                    var src = k == 0 ? rig.Left : rig.Right; if (src == null || !src.Tracked) continue;
                    _reach.Add(new HandPt { Actor = GameServices.LocalSlot, Hand = k, Pos = src.IndexTip });
                    _reach.Add(new HandPt { Actor = GameServices.LocalSlot, Hand = k, Pos = src.PinchPoint });
                }
                if (rig.Left != null && rig.Left.Tracked) _hands.Add(new HandPt { Actor = GameServices.LocalSlot, Hand = 0, Pos = rig.Left.PalmCenter });
                if (rig.Right != null && rig.Right.Tracked) _hands.Add(new HandPt { Actor = GameServices.LocalSlot, Hand = 1, Pos = rig.Right.PalmCenter });
            }
            if (GameServices.RemotePose != null)
                for (int s = 0; s < CompetitorRoster.SlotCount; s++)
                {
                    if (s == GameServices.LocalSlot || !GameServices.IsRemoteHuman(s) || !Round.Roster[s].IsActive) continue;
                    for (int h = 0; h < 2; h++) { var p = GameServices.RemotePose(s, h + 1); if (p.HasValue) _hands.Add(new HandPt { Actor = s, Hand = h, Pos = p.Value.position }); }
                }
            // a held cloth/towel counts as the hand that holds it (wiping with a cloth)
            foreach (var b in _bodies) if (b.IsHeld && (b.Spec.Kind == "cloth" || b.Spec.Kind == "kitchen_towel" || b.Spec.Kind == "mirror_cloth" || b.Spec.Kind == "duster" || b.Spec.Kind == "sponge"))
                _hands.Add(new HandPt { Actor = b.HeldBy, Hand = 2, Pos = b.Col.bounds.center, Tool = b });
        }

        void FixedUpdate()
        {
            if (!GameServices.IsAuthority || Round == null || Round.Recipes == null || !Round.IsPlayable) return;
            CollectHands();
            Wipe();
            Stage();
            JoinAndSplit();
            Follow();
            Dislodge();
            Crooked();
        }

        void Wipe()
        {
            foreach (var f in _fixtures)
            {
                var st = f.State; if (st == null || st.Decals <= 0) continue;
                int patch = f.ActivePatch; var pc = f.PatchWorld(patch); float pr = f.PatchRadius(patch) + 0.05f;
                float best = 0f;
                foreach (var h in _hands)
                {
                    var key = (st.Id, h.Actor, h.Hand);
                    // V8: scrub ON the dirt patch (flat distance to the patch centre), close to the surface
                    var flat = h.Pos - pc; flat.y = 0f;
                    if (!f.Contains(h.Pos, 0.07f) || flat.magnitude > pr) { _rubLast.Remove(key); continue; }
                    if (_rubLast.TryGetValue(key, out var last))
                    {
                        float dd = Vector3.Distance(last, h.Pos); if (dd > 0.2f) dd = 0; // tracking jump
                        var k2 = (st.Id, h.Actor); _rub.TryGetValue(k2, out var acc); acc += dd;
                        _suds += dd; if (_suds > 0.12f) { _suds = 0f; RoomFx.Burst("bubbles", h.Pos + Vector3.down * 0.02f, 0.06f, 0.6f); AudioBank.I?.Play("wipe", h.Pos, 0.25f, 0.12f); }
                        if (acc >= RubPerStroke)
                        {
                            acc = 0;
                            var err = Submit(new ROp { Actor = h.Actor, Verb = Verb.Stroke, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1 });
                            // (patch vanishes + sound/bubbles: RecipeFixtureView, from replicated state on every peer)
                        }
                        _rub[k2] = acc; best = Mathf.Max(best, acc / RubPerStroke);
                    }
                    _rubLast[key] = h.Pos;
                }
                f.SetScrub(best);
            }
        }
        float _suds;

        /// <summary>The stage step (fold/fill/brush) this prop still needs, or null.</summary>
        public RecipeStepDef PendingStage(RProp p)
        {
            if (p == null) return null; var inst = Board.Instances[p.RecipeInstance];
            foreach (var s in inst.Def.Steps) if (s.Kind == StepKind.Stage && s.PropKind == p.Kind && p.Stage < s.StageTarget) return s;
            return null;
        }

        void Stage()
        {
            foreach (var b in _bodies)
            {
                if (!b.IsHeld) continue;
                var rp = RProp(b); var step = PendingStage(rp); if (step == null) continue;
                // V8 Gate C: a stage done AT a fixture (rinse a plate under the running tap) - hold it in the stream for a moment
                if (step.Verb == Verb.Fill && !string.IsNullOrEmpty(step.Fixture))
                {
                    var sfv = FixtureViewOf(rp.RecipeInstance, step.Fixture);
                    if (sfv != null && sfv.Pump != null) continue;   // the soap pump doses on its own press events
                    Rinse(b, rp, step); continue;
                }
                var c = b.Col.bounds.center; float reach = b.Col.bounds.extents.magnitude + 0.06f;
                var wash = rp.StrokeCap > 0 ? b.GetComponent<WashableView>() : null;
                // V10: a stained washable is cleaned ONLY by real sponge contact on its eating face, cell by cell (stain grid = world state)
                if (wash != null && step.Verb == Verb.Stroke && rp.Dirt != 0) { ScrubContact(b, rp, wash); continue; }
                foreach (var h in _hands)
                {
                    // a held cloth/sponge counts as a hand - for washables (sponge the plate) it is the best tool; otherwise bare hands only
                    if (h.Hand == 2 && wash == null) continue;
                    // V9: a washable is scrubbed by a SPONGE (bare hands just smear) - and only once the sponge has soap when the job asks for it
                    if (wash != null && (h.Hand != 2 || h.Tool == null || h.Tool.Spec.Kind != "sponge" || !SpongeReady(h.Tool))) continue;
                    if (h.Hand < 2 && h.Actor == b.HeldBy && wash != null && Vector3.Distance(h.Pos, c) < 0.02f) continue;
                    var key = (rp.Id, h.Actor * 4 + h.Hand);
                    if (Vector3.Distance(h.Pos, c) > reach) { _stageLast.Remove(key); continue; }
                    var rel = b.transform.InverseTransformPoint(h.Pos); // relative to the item: the holding hand barely moves
                    if (_stageLast.TryGetValue(key, out var last))
                    {
                        float dd = Vector3.Distance(rel, last); if (dd > 0.15f) dd = 0;
                        if (h.Hand == 2) dd *= 1.6f; // a sponge scrubs faster than a bare hand
                        _stageAcc.TryGetValue(rp.Id, out var acc); acc += dd;
                        if (wash != null && dd > 0.002f)
                        {
                            wash.SetScrub(acc / RubPerStage);
                            _sponge += dd; if (_sponge > 0.1f) { _sponge = 0f; AudioBank.I?.Play("sponge", c, 0.3f, 0.1f); RoomFx.Burst("bubbles", c + Vector3.up * 0.03f, 0.07f, 0.8f); }
                        }
                        if (acc >= RubPerStage)
                        {
                            acc = 0;
                            var err = Submit(new ROp { Actor = b.HeldBy, Verb = step.Verb, Prop = rp.Id, Prop2 = -1, Fixture = -1, Slot = -1 });
                            if (err == null) StageVisual(b, rp); // puff + sound: RecipeClipboard.Sounds (every peer)
                        }
                        _stageAcc[rp.Id] = acc;
                    }
                    _stageLast[key] = rel;
                }
            }
        }
        float _sponge;
        readonly Dictionary<(int plate, int actor), Vector3> _scrubLast = new Dictionary<(int, int), Vector3>();
        readonly Dictionary<int, ulong> _scrubPending = new Dictionary<int, ulong>();
        readonly Dictionary<int, float> _scrubSubmitAt = new Dictionary<int, float>();
        float _smearT;
        /// <summary>Metres of sponge travel over a cell that scrub it clean (a few back-and-forth strokes).</summary>
        public const float ScrubMetresPerCell = 0.05f;

        /// <summary>
        /// V10 stain removal: the held sponge must TOUCH the plate's eating face (closest-point gap &lt; 12 mm, on the face, not the underside)
        /// and MOVE along it; work accumulates only in stained cells under the sponge footprint (falls off to its edge). A cell that has had
        /// <see cref="ScrubMetresPerCell"/> of scrubbing is reported through the authoritative Scrub rule (which also checks: plate held, soapy
        /// sponge in the same actor's hand). A dry sponge or a bare hand only smears (sound, no cleaning). Waving a sponge in the air does nothing.
        /// </summary>
        void ScrubContact(PropBody b, RProp rp, WashableView wash)
        {
            foreach (var h in _hands)
            {
                if (h.Hand != 2 || h.Tool == null || h.Tool.Spec.Kind != "sponge") continue;
                var key = (rp.Id, h.Actor);
                var sc = h.Tool.Col.bounds.center; var cp = b.Col.ClosestPoint(sc);
                float gap = Vector3.Distance(h.Tool.Col.ClosestPoint(cp), cp);
                var lp = b.transform.InverseTransformPoint(cp);
                if (gap > 0.012f || lp.y < wash.FaceCenterLocal.y - 0.012f) { _scrubLast.Remove(key); continue; }
                if (_scrubLast.TryGetValue(key, out var last))
                {
                    float d = new Vector2(lp.x - last.x, lp.z - last.z).magnitude; if (d > 0.08f) d = 0f;   // tracking jump
                    if (d > 0.0005f)
                    {
                        if (!SpongeReady(h.Tool))
                        {   // grease only smears around without soap
                            if (Time.time > _smearT) { _smearT = Time.time + 0.5f; AudioBank.I?.Play("sponge", cp, 0.15f, 0.2f); }
                        }
                        else
                        {
                            var g = wash.ToGrid(lp); float cellM = wash.Radius * 2f / WashableView.Grid;
                            var se = h.Tool.Col.bounds.extents; float brush = Mathf.Max(0.7f, Mathf.Min(se.x, se.z) / cellM);   // sponge footprint in cells
                            _scrubPending.TryGetValue(rp.Id, out var pend);
                            for (int r = Mathf.Max(0, Mathf.FloorToInt(g.y - brush)); r <= Mathf.Min(WashableView.Grid - 1, Mathf.CeilToInt(g.y + brush)); r++)
                                for (int col = Mathf.Max(0, Mathf.FloorToInt(g.x - brush)); col <= Mathf.Min(WashableView.Grid - 1, Mathf.CeilToInt(g.x + brush)); col++)
                                {
                                    int cell = r * WashableView.Grid + col; ulong bit = 1UL << cell; if ((rp.Dirt & bit) == 0) continue;
                                    float dist = new Vector2(col - g.x, r - g.y).magnitude; if (dist > brush) continue;
                                    wash.AddWork(cell, d / ScrubMetresPerCell * (1f - 0.5f * dist / brush));
                                    if (wash.Work(cell) >= 1f) pend |= bit;
                                }
                            _scrubPending[rp.Id] = pend;
                            _sponge += d; if (_sponge > 0.08f) { _sponge = 0f; AudioBank.I?.Play("sponge", cp, 0.3f, 0.1f); RoomFx.Burst("bubbles", cp + b.transform.up * 0.02f, 0.05f, 0.7f); }
                        }
                    }
                }
                _scrubLast[key] = lp;
            }
            // report scrubbed cells through the authoritative rule (rate-limited)
            if (_scrubPending.TryGetValue(rp.Id, out var cells) && (cells & rp.Dirt) != 0 && (!_scrubSubmitAt.TryGetValue(rp.Id, out var t) || Time.time >= t))
            {
                PropBody spongeB = null; foreach (var h in _hands) if (h.Hand == 2 && h.Tool != null && h.Tool.Spec.Kind == "sponge" && h.Actor == b.HeldBy) spongeB = h.Tool;
                var sp = RProp(spongeB);
                if (sp != null)
                {
                    var err = Submit(new ROp { Actor = b.HeldBy, Verb = Verb.Scrub, Prop = rp.Id, Prop2 = sp.Id, Fixture = -1, Slot = -1, Arg = (cells & rp.Dirt).ToString("X") });
                    if (err != null) Debug.Log($"[SHHH][recipe] scrub of plate#{rp.Id} refused: {err}");
                    _scrubPending[rp.Id] = 0UL; _scrubSubmitAt[rp.Id] = Time.time + 0.15f;
                }
            }
        }

        bool SpongeReady(PropBody sponge)
        {
            var sp = RProp(sponge); if (sp == null) return true;
            foreach (var st in Board.Instances[sp.RecipeInstance].Def.Steps) if (st.Kind == StepKind.Stage && st.PropKind == "sponge" && sp.Stage < st.StageTarget) return false;
            return true;
        }
        public const float RinseSeconds = 1.1f;

        void Rinse(PropBody b, RProp rp, RecipeStepDef step)
        {
            var fv = FixtureViewOf(rp.RecipeInstance, step.Fixture); var tap = fv != null ? fv.Tap : null;
            _rinseAcc.TryGetValue(rp.Id, out var acc);
            // V10: only what the falling water really lands on is rinsed (the stream's first hit), and a trickle rinses slower
            if (tap != null && tap.Wets(b))
            {
                tap.Hitting(tap.Stream.HitPoint);
                acc += Time.fixedDeltaTime * Mathf.Lerp(0.35f, 1f, tap.Flow);
                b.GetComponent<WashableView>()?.SetRinse(acc / RinseSeconds);
                if (acc >= RinseSeconds) { acc = 0f; Submit(new ROp { Actor = b.HeldBy, Verb = Verb.Fill, Prop = rp.Id, Prop2 = -1, Fixture = fv.State.Id, Slot = -1 }); }
            }
            else acc = Mathf.Max(0f, acc - Time.fixedDeltaTime * 0.5f);
            _rinseAcc[rp.Id] = acc;
        }
        readonly Dictionary<int, float> _rinseAcc = new Dictionary<int, float>();

        /// <summary>Folded / filled items visibly change: folded = flatter and smaller, filled = a fill disc.</summary>
        public void StageVisual(PropBody b, RProp rp)
        {
            var vis = b.transform.childCount > 0 ? b.transform.GetChild(0) : null; if (vis == null) return;
            var step = (RecipeStepDef)null; foreach (var s in Board.Instances[rp.RecipeInstance].Def.Steps) if (s.Kind == StepKind.Stage && s.PropKind == rp.Kind) step = s;
            if (step == null) return;
            float t = Mathf.Clamp01(rp.Stage / (float)Mathf.Max(1, step.StageTarget));
            if (step.Verb != Verb.Fold) return;
            if (!_baseScale.TryGetValue(rp.Id, out var bs)) _baseScale[rp.Id] = bs = vis.localScale;
            // folding/rolling: smaller footprint, thicker - along the item's REAL up axis (art is often rotated inside the body, e.g. a
            // sock modelled standing whose length is the art's local Y would otherwise grow into a tall rod)
            var upL = Quaternion.Inverse(vis.localRotation) * Vector3.up; upL = new Vector3(Mathf.Abs(upL.x), Mathf.Abs(upL.y), Mathf.Abs(upL.z));
            int ax = upL.x >= upL.y && upL.x >= upL.z ? 0 : upL.y >= upL.z ? 1 : 2;
            var k = new Vector3(1f - 0.45f * t, 1f - 0.45f * t, 1f - 0.45f * t); k[ax] = 1f + 0.8f * t;
            vis.localScale = Vector3.Scale(bs, k);
        }

        void JoinAndSplit()
        {
            foreach (var b in _bodies)
            {
                if (!b.IsHeld) continue;
                var p = RProp(b); if (p == null) continue;
                var inst = Board.Instances[p.RecipeInstance];
                foreach (var s in inst.Def.Steps)
                {
                    bool pair = s.Kind == StepKind.Paired && s.PropKind == p.Kind && p.Partner < 0;
                    bool attachMe = s.Kind == StepKind.Attached && s.PropKind == p.Kind && p.AttachedTo < 0;
                    bool attachToMe = s.Kind == StepKind.Attached && s.PartnerKind == p.Kind;
                    if (!pair && !attachMe && !attachToMe) continue;
                    foreach (var qb in _bodies)
                    {
                        if (qb == b) continue; var q = RProp(qb); if (q == null || q.RecipeInstance != p.RecipeInstance) continue;
                        RProp item, partner;
                        if (pair) { if (q.Kind != p.Kind || q.Partner >= 0) continue; item = p; partner = q; }
                        else if (attachMe) { if (q.Kind != s.PartnerKind) continue; item = p; partner = q; }
                        else { if (q.Kind != s.PropKind || q.AttachedTo >= 0) continue; item = q; partner = p; }
                        // (a wrong-tag join is physically allowed - the rules only credit matching tags)
                        // touching the partner OR anything already riding on it (a sandwich layer, a lid) counts as touching it
                        float gap = Gap(b, qb);
                        foreach (var fkv in _follow) if (fkv.Value.leader == q.Id) { var fb = Body(fkv.Key); if (fb != null && fb != b) gap = Mathf.Min(gap, Gap(b, fb)); }
                        var key = (Mathf.Min(p.Id, q.Id), Mathf.Max(p.Id, q.Id));
                        if (DebugJoins && Time.time > _dbgNext) { _dbgNext = Time.time + 0.5f; Debug.Log($"[SHHH][recipe] join probe {p.Kind}#{p.Id} held by {b.HeldBy} -> {q.Kind}#{q.Id}: gap {gap:0.000} m, touch {(_touch.TryGetValue(key, out var tt) ? tt : 0f):0.00} s, {(pair ? "pair" : "attach")}"); }
                        if (gap > JoinGap) { _touch.Remove(key); continue; }
                        _touch.TryGetValue(key, out var t); t += Time.fixedDeltaTime; _touch[key] = t;
                        if (t < JoinHold) continue;
                        _touch.Remove(key);
                        var err = Submit(new ROp { Actor = b.HeldBy, Verb = pair ? Verb.Pair : Verb.Attach, Prop = item.Id, Prop2 = partner.Id, Fixture = -1, Slot = -1 });
                        if (err != null) { Debug.Log($"[SHHH][recipe] join {item.Kind}+{partner.Kind} rejected: {err}"); continue; }
                        // the loose one follows the held one (pair) / the item rides on its partner (attach)
                        var followerB = pair ? qb : Body(item.Id); var leaderB = pair ? b : Body(partner.Id);
                        _follow[RProp(followerB).Id] = (RProp(leaderB).Id, leaderB.transform.InverseTransformPoint(followerB.transform.position), Quaternion.Inverse(leaderB.transform.rotation) * followerB.transform.rotation);
                        return;
                    }
                }
            }
        }

        /// <summary>Bots: an accepted pair/attach made by a body (the follower rides on the leader from now on).</summary>
        public void RegisterJoin(int follower, int leader)
        {
            var fb = Body(follower); var lb = Body(leader); if (fb == null || lb == null) return;
            _follow[follower] = (leader, lb.transform.InverseTransformPoint(fb.transform.position), Quaternion.Inverse(lb.transform.rotation) * fb.transform.rotation);
        }

        /// <summary>Stop a joined item riding on its leader (the join stays in the rules) - used when a sock pair goes into a drawer.</summary>
        public void Unfollow(int follower)
        {
            if (!_follow.TryGetValue(follower, out var j)) return;
            var a = Body(follower); var l = Body(j.leader);
            if (_ignored.Remove(follower) && a != null && l != null) Physics.IgnoreCollision(a.Col, l.Col, false);
            _follow.Remove(follower);
        }

        public IEnumerable<PropBody> FollowersOf(int leaderRecipeProp) { foreach (var kv in _follow) if (kv.Value.leader == leaderRecipeProp) { var b = Body(kv.Key); if (b != null) yield return b; } }

        static float Gap(PropBody a, PropBody c) => Vector3.Distance(a.Col.ClosestPoint(c.Col.bounds.center), c.Col.ClosestPoint(a.Col.bounds.center));

        void Follow()
        {
            List<int> drop = null, swap = null;
            foreach (var kv in _follow)
            {
                var fb = Body(kv.Key); var lb = Body(kv.Value.leader); var fp = World.Prop(kv.Key);
                bool joined = fp.Partner == kv.Value.leader || fp.AttachedTo == kv.Value.leader || World.Prop(kv.Value.leader).Partner == kv.Key;
                if (fb == null || lb == null || !joined) { (drop ??= new List<int>()).Add(kv.Key); if (fb != null && !fb.IsHeld) fb.Body.isKinematic = false; continue; }
                if (fb.IsHeld)
                {
                    // a pair has no "base": whichever half you pick up carries the other (you can't tell which sock leads). Pulling a
                    // pair apart takes both halves in hand (two hands / two players); an attached item (lid, layer) comes off its base
                    // with one hand.
                    bool pairJoin = fp.AttachedTo < 0;
                    if (pairJoin && !lb.IsHeld) { (swap ??= new List<int>()).Add(kv.Key); continue; }
                    // pulled apart by a hand -> unpair / detach (rival sabotage evidence or the owner redoing it)
                    if (Vector3.Distance(fb.Col.bounds.center, lb.Col.bounds.center) > SplitDistance + fb.Col.bounds.extents.magnitude)
                    {
                        var err = Submit(new ROp { Actor = fb.HeldBy, Verb = fp.AttachedTo >= 0 ? Verb.Detach : Verb.Unpair, Prop = kv.Key, Prop2 = -1, Fixture = -1, Slot = -1 });
                        if (err == null) (drop ??= new List<int>()).Add(kv.Key);
                    }
                    continue;
                }
                fb.Body.isKinematic = true;
                fb.IgnoreImpactsUntil = Time.time + 0.5f; // riding along is silent; only a real drop of the pair makes noise (via the leader)
                if (!_ignored.Contains(kv.Key)) { Physics.IgnoreCollision(fb.Col, lb.Col, true); _ignored.Add(kv.Key); }
                fb.Body.MovePosition(lb.transform.TransformPoint(kv.Value.offset));
                fb.Body.MoveRotation(lb.transform.rotation * kv.Value.rot);
            }
            if (swap != null) foreach (var id in swap)
                {
                    if (!_follow.TryGetValue(id, out var j)) continue;
                    var nl = Body(id); var nf = Body(j.leader); if (nl == null || nf == null) continue;
                    _follow.Remove(id);
                    _follow[j.leader] = (id, nl.transform.InverseTransformPoint(nf.transform.position), Quaternion.Inverse(nl.transform.rotation) * nf.transform.rotation);
                    if (_ignored.Remove(id)) _ignored.Add(j.leader); // the ignore is between the same two colliders
                }
            if (drop != null) foreach (var id in drop)
                {
                    if (_ignored.Remove(id) && _follow.TryGetValue(id, out var j)) { var a = Body(id); var l = Body(j.leader); if (a != null && l != null) Physics.IgnoreCollision(a.Col, l.Col, false); }
                    _follow.Remove(id); var b = Body(id); if (b != null && !b.IsHeld) b.Body.isKinematic = false;
                }
            // rules may also split a pair (sabotage by a bot, rejoin): drop followers whose join is gone
        }

        void Dislodge()
        {
            foreach (var b in _bodies)
            {
                if (b.IsHeld || b.Reserved || b.Body.isKinematic) continue;
                var p = RProp(b); if (p == null || !p.IsPlaced || IsFollower(p)) continue;
                var fv = FixtureView(p.Fixture); if (fv == null) continue;
                var slot = fv.SlotPoint(p.Slot);
                var c = b.Col.bounds.center; var d = c - slot; d.y = 0;
                if (fv.Host != null)
                {
                    // V12: laundry tumbles in the drum, towels slide in the drawer - still placed while inside; taken out = unloaded (no snap-back)
                    if (fv.Host.Inside(fv.Decl.Kind, c)) continue;
                    if (Submit(new ROp { Actor = b.LastActor, Verb = Verb.PickUp, Prop = p.Id, Prop2 = -1, Fixture = -1, Slot = -1 }) == null)
                        Submit(new ROp { Actor = b.LastActor, Verb = Verb.PutDown, Prop = p.Id, Prop2 = -1, Fixture = -1, Slot = -1, Arg = "floor" });
                    continue;
                }
                if (d.magnitude < DislodgeDistance + (fv.Placement.Stack ? fv.Placement.Width * 0.5f : 0f) && c.y > slot.y - 0.05f) continue;
                int actor = b.LastActor;
                if (Submit(new ROp { Actor = actor, Verb = Verb.PickUp, Prop = p.Id, Prop2 = -1, Fixture = -1, Slot = -1 }) == null)
                    Submit(new ROp { Actor = actor, Verb = Verb.PutDown, Prop = p.Id, Prop2 = -1, Fixture = -1, Slot = -1, Arg = "knocked" });
                else
                {
                    // not accepted (protected repair window, cooldown, nobody to blame): the item settles back into its slot
                    b.Body.position = new Vector3(slot.x, slot.y + (b.transform.position.y - b.Col.bounds.min.y) + 0.01f, slot.z);
                    b.Body.linearVelocity = Vector3.zero; b.Body.angularVelocity = Vector3.zero; b.ReservedUntil = Time.time + 0.5f;
                }
            }
        }

        readonly Dictionary<int, float> _crookedFor = new Dictionary<int, float>();

        /// <summary>A placed item that must stand straight and got knocked crooked (by a hand, a bot, a pet or another item) is reported
        /// as tilted by whoever touched it last; it counts as straight again only after a re-place (the slot re-checks).</summary>
        void Crooked()
        {
            foreach (var b in _bodies)
            {
                var p = RProp(b); if (p == null || !p.IsPlaced || !p.Oriented || b.IsHeld || b.Body.isKinematic || IsFollower(p)) { if (p != null) _crookedFor.Remove(p.Id); continue; }
                bool needs = false; foreach (var s in Board.Instances[p.RecipeInstance].Def.Steps) if (s.RequireOriented && s.PropKind == p.Kind) needs = true;
                if (!needs) continue;
                var fv = FixtureView(p.Fixture); if (fv == null) continue;
                bool square = fv.Seating != null ? fv.Seating.StandsInGap(b, out _) : RecipeFixtureView.Square(fv.transform, b.transform);
                if (square) { _crookedFor.Remove(p.Id); continue; }
                _crookedFor.TryGetValue(p.Id, out var t); t += Time.fixedDeltaTime; _crookedFor[p.Id] = t;
                if (t < 0.3f) continue;
                if (fv.Seating != null) Debug.Log($"[SHHH][rack] plate#{b.Spec.Id} no longer stands in its gap: {fv.Seating.Describe(b)}");
                _crookedFor.Remove(p.Id);
                var err = Submit(new ROp { Actor = b.LastActor, Verb = Verb.Align, Prop = p.Id, Prop2 = -1, Fixture = -1, Slot = -1, Arg = "tilt" });
                if (err != null) Debug.Log($"[SHHH][recipe] {p.Kind}#{p.Id} knocked crooked (last touched by {b.LastActor}) - not accepted: {err}");
            }
        }

        void Update()
        {
            if (Round == null || Round.Recipes == null) return;
            // keep stage visuals in sync on every peer (clients follow the replicated rules state)
            foreach (var b in _bodies) { var rp = RProp(b); if (rp != null) StageVisual(b, rp); }
        }
    }
}
