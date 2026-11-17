using System.Collections.Generic;
using SHHH.Core;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// One playable room scene at runtime. Builds the room from the shared RoomCatalog data (so bots, chores and art always
    /// agree), bakes its NavMesh, binds the authoritative RoundController, spawns bot/pet bodies with their brains, and
    /// exposes an IWorldQuery so AI perceives the REAL physical world (no omniscience: positions only).
    /// </summary>
    public sealed class RoomRuntime : MonoBehaviour, IWorldQuery
    {
        /// <summary>V12: the local player's own tracked Genies body (null when there is no human local seat).</summary>
        public SelfBodyView SelfBody { get; private set; }
        /// <summary>QA switch: build rooms without the local body.</summary>
        public static bool NoSelfBody;

        public RoomDefinition Def { get; private set; }
        public RoundController Round { get; private set; }
        public Vector3 ChoreBoardPos { get; private set; }
        public MomCharacter Mom { get; private set; }
        readonly Dictionary<int, PropBody> _props = new Dictionary<int, PropBody>();
        readonly Dictionary<int, AnchorFixture> _anchors = new Dictionary<int, AnchorFixture>();
        readonly Dictionary<int, BotBody> _botBodies = new Dictionary<int, BotBody>();
        readonly List<BotBrain> _brains = new List<BotBrain>();
        readonly Dictionary<PetKind, PetBody> _pets = new Dictionary<PetKind, PetBody>();
        readonly Dictionary<int, NetPuppet> _puppets = new Dictionary<int, NetPuppet>();
        readonly Dictionary<int, RemoteHumanView> _remoteHumans = new Dictionary<int, RemoteHumanView>();
        /// <summary>Online client: replicated body for a bot slot (0-3) or pet actor id.</summary>
        public NetPuppet PuppetFor(int actor) => _puppets.TryGetValue(actor, out var p) ? p : null;
        public PetBody PetFor(PetKind k) => _pets.TryGetValue(k, out var p) ? p : null;
        /// <summary>The visible sibling for a slot on this peer (bot body, network puppet or remote human), or null.</summary>
        public SiblingAvatar AvatarFor(int slot)
        {
            if (_botBodies.TryGetValue(slot, out var b) && b != null) return b.Avatar;
            if (_puppets.TryGetValue(slot, out var p) && p != null) return p.Avatar;
            if (_remoteHumans.TryGetValue(slot, out var h) && h != null) return h.Avatar;
            return null;
        }
        RoomShell.Result _shell;
        NavMeshSurface _nav;
        HouseAssembly _house;
        int _agentTypeId;
        public Transform LocalHead;  // set by GameRoot (for ActorTransform of the local human)
        public bool AutopilotLocal;  // automated tests: the human seat is driven by a bot brain + body

        public float Now => Round != null ? Round.Time : 0f;

        public void Build(RoomDefinition def)
        {
            Def = def;
            GameServices.Room = this;
            _house = def.Id != RoomId.Practice ? HouseAssembly.Create(def, transform) : null;
            if (_house != null) { _shell = _house.ShellResult(); NightExterior.Build(transform, _house.WorldBounds.center); HouseAdditions.Build(def.Id, transform, _house.transform); }
            else
            {
                _shell = RoomShell.Build(def, transform);
                RoomDressing.Build(def, transform);
                var furnRoot = new GameObject("Furniture").transform; furnRoot.SetParent(transform, false);
                foreach (var f in def.Furniture) VisualFactory.BuildFurniture(f, furnRoot, Palette.WallLiving);
            }
            var anchorRoot = new GameObject("Anchors").transform; anchorRoot.SetParent(transform, false);
            foreach (var a in def.Anchors)
            {
                var g = new GameObject("A_" + a.Kind + "_" + a.Id); g.transform.SetParent(anchorRoot, false);
                var fx = g.AddComponent<AnchorFixture>(); fx.Init(a); _anchors[a.Id] = fx;
            }
            var propRoot = new GameObject("Props").transform; propRoot.SetParent(transform, false);
            foreach (var p in def.Props)
            {
                var g = new GameObject("P_" + p.Kind + "_" + p.Id); g.transform.SetParent(propRoot, false);
                g.transform.localPosition = VisualFactory.V(p.Spawn) + Vector3.up * 0.01f;
                g.transform.localRotation = Quaternion.Euler(0, p.SpawnYaw, 0);
                RecipeVisuals.BuildProp(p.Kind, p.Tag, g.transform, p.Recipe); // = VisualFactory art, plus recipe placeholders + tag stickers
                g.AddComponent<Rigidbody>();
                var body = g.AddComponent<PropBody>(); body.Init(p, _shell.PlayBounds);
                Vessels.Attach(body);   // V12: mugs, cups, pots, bottles hold a finite, pourable, replicated liquid
                _props[p.Id] = body;
            }
            // Chore board on the west wall, monitor next to Mom's door.
            var boardGo = new GameObject("ChoreBoard"); boardGo.transform.SetParent(transform, false);
            boardGo.transform.localPosition = VisualFactory.V(def.ChoreBoard) + new Vector3(0.03f, 0, 0);
            boardGo.transform.localRotation = Quaternion.Euler(0, -90, 0);
            boardGo.AddComponent<ChoreBoardView>().Build(def.Title);
            ChoreBoardPos = boardGo.transform.position;
            var mon = new GameObject("MomMonitor"); mon.transform.SetParent(transform, false);
            mon.transform.localPosition = new Vector3(0.95f, 1.55f, def.Depth / 2 - 0.03f);
            mon.AddComponent<MomMonitorView>().Build();
            gameObject.AddComponent<RoomFx>();
            BakeNavMesh();
            // Mom lives behind her door.
            var momGo = new GameObject("Mom"); momGo.transform.SetParent(transform, false);
            Mom = momGo.AddComponent<MomCharacter>();
            Mom.Init(_shell.MomOffstage, _shell.MomDoorway, _shell.Door, _shell.DoorLight, _agentTypeId);
            if (AudioBank.I != null) AudioBank.I.SetAmbience(null);   // owner 2026-11-15: background ambience removed
        }

        void BakeNavMesh()
        {
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = 0.2f; settings.agentHeight = 1.3f; settings.agentClimb = 0.12f; settings.agentSlope = 30f;
            if (_house != null) { settings.agentHeight = 1.75f; settings.agentClimb = .19f; settings.agentSlope = 35f; }
            _agentTypeId = settings.agentTypeID;
            _nav = gameObject.AddComponent<NavMeshSurface>();
            _nav.agentTypeID = _agentTypeId;
            _nav.collectObjects = CollectObjects.Children;
            _nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            _nav.layerMask = (1 << 0) | (1 << Layers.Furniture);
            _nav.overrideVoxelSize = true; _nav.voxelSize = 0.05f;
            var data = new NavMeshData(_agentTypeId);
            _nav.navMeshData = data;
            // Build with our custom agent settings.
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(transform, _nav.layerMask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var b = _house != null ? _house.WorldBounds : new Bounds(transform.position + Vector3.up, new Vector3(Def.Width + 2f, 4f, Def.Depth + 4f));
            var built = NavMeshBuilder.BuildNavMeshData(settings, sources, b, transform.position, Quaternion.identity);
            _nav.navMeshData = built;
            _nav.AddData();
            Debug.Log($"[SHHH] NavMesh baked for {Def.Title}: {sources.Count} sources");
        }

        readonly List<Vector3> _botStarts = new List<Vector3>();

        /// <summary>
        /// V12 (Quest run 2026-11-11: at match start the bots stood on top of me, nameplates filling the view - seat spawns are 0.8 m apart).
        /// A bot starts at its seat spawn unless that is within 1.5 m of a human's spawn; then at the nearest walkable spot on the same floor
        /// that is 1.5 m from every human and 0.8 m from the other bots (host only - clients see the replicated positions).
        /// </summary>
        Vector3 ClearSpawn(int seat, RoundController round, List<Vector3> placed)
        {
            var orig = VisualFactory.V(Def.SiblingSpawns[seat]);
            var humans = new List<Vector3>();
            for (int j = 0; j < CompetitorRoster.SlotCount; j++)
            {
                var s = round.Roster[j]; if (!s.IsActive || s.IsBot || j == seat) continue;
                if (j == GameServices.LocalSlot && AutopilotLocal) continue;
                humans.Add(VisualFactory.V(Def.SiblingSpawns[j]));
            }
            float Clear(Vector3 p) { float m = 99f; foreach (var h in humans) m = Mathf.Min(m, new Vector2(p.x - h.x, p.z - h.z).magnitude); return m; }
            bool Ok(Vector3 p) { if (Clear(p) < 1.5f) return false; foreach (var b in placed) if (new Vector2(p.x - b.x, p.z - b.z).magnitude < 0.8f) return false; return true; }
            if (Ok(orig)) return orig;
            for (float r = 0.6f; r <= 3.01f; r += 0.3f)
            {
                Vector3 best = orig; float bestClear = -1f;
                for (int a = 0; a < 12; a++)
                {
                    var c = orig + Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward * r;
                    if (!NavMesh.SamplePosition(c, out var hit, 0.25f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - orig.y) > 0.3f) continue;
                    if (!Ok(hit.position)) continue;
                    float cl = Clear(hit.position); if (cl > bestClear) { bestClear = cl; best = hit.position; }
                }
                if (bestClear > 0f) return best;
            }
            return orig;
        }

        NavMeshAgent AddAgent(GameObject g, Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas)) pos = hit.position;
            g.transform.position = pos;
            var a = g.AddComponent<NavMeshAgent>();
            a.agentTypeID = _agentTypeId;
            a.Warp(pos);
            return a;
        }

        /// <summary>Wires the authoritative round to this room: chores -> anchors, bots & pets -> bodies + brains.</summary>
        public void Bind(RoundController round, ulong seed)
        {
            Round = round;
            foreach (var a in _anchors.Values) a.SetActive(false);
            foreach (var c in round.Board.Chores)
                foreach (var s in c.Steps)
                    if (_anchors.TryGetValue(s.AnchorId, out var fx))
                    {
                        // V5: every personal job is public and colour-coded (you can see a rival's job - and mess with it).
                        fx.OwnerColor = c.IsBonus ? DutyVisuals.ColorFor(round, c.BonusOwner) : Palette.Cream;
                        fx.IsBonusFor = c.IsBonus; fx.BonusOwner = c.BonusOwner;
                        fx.SetActive(true);
                    }
            var duties = GetComponent<DutyVisuals>() ?? gameObject.AddComponent<DutyVisuals>();
            duties.Bind(this, round);
            if (round.Recipes != null)
            {
                // V7 recipe round: object-local fixtures + clipboards replace the anchor rings and the big wall job board
                (GetComponent<RecipeRuntime>() ?? gameObject.AddComponent<RecipeRuntime>()).Bind(this, round);
                var wall = transform.Find("ChoreBoard"); if (wall) wall.gameObject.SetActive(false);
                // V7 Gate 5: who saw what (host) + the confrontation tray (every peer)
                (GetComponent<PerceptionService>() ?? gameObject.AddComponent<PerceptionService>()).Bind(this, round);
                ConfrontationPanel.Ensure(LocalHead);
            }
            (GetComponent<TrashSystem>() ?? gameObject.AddComponent<TrashSystem>()).Bind(this);   // V12 litter -> bins (host decides)
            TaskHud.Ensure(LocalHead);                                                         // V12 HUD: my step, pets, trash
            round.Pets.Clear(); _puppets.Clear(); _remoteHumans.Clear();
            bool replica = GameServices.IsReplica; // online client: brains run on the host; we only show replicated poses
            foreach (var kind in new[] { PetKind.Cat, PetKind.Dog })
            {
                var g = new GameObject(kind.ToString()); g.transform.SetParent(transform, false);
                var home = VisualFactory.V(kind == PetKind.Cat ? Def.CatBed : Def.DogBed);
                AddAgent(g, home + new Vector3(0.4f, 0, -0.4f));
                var pb = g.AddComponent<PetBody>(); pb.Init(kind);
                _pets[kind] = pb;
                g.AddComponent<PetCare>().Init(kind, pb, this);   // V12 hunger, eating from the real bowl, whines (host) + badge (every peer)
                if (!replica) round.Pets.Add(new PetBrain(kind, pb, round, this, seed));
                else { var ag = g.GetComponent<NavMeshAgent>(); if (ag) ag.enabled = false; var pp = g.AddComponent<NetPuppet>(); pp.ActorId = pb.ActorId; _puppets[pb.ActorId] = pp; }
            }
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                var slot = round.Roster[i];
                if (!slot.IsActive) continue;
                bool local = i == GameServices.LocalSlot;
                if (local && !(AutopilotLocal && !replica))
                {
                    // V12: my own full Genies body under my headset (hidden head, gloves as hands)
                    var rig = GameRoot.I != null && GameRoot.I.Rig != null ? GameRoot.I.Rig : FindFirstObjectByType<PlayerRig>();
                    if (rig != null && !NoSelfBody)
                    {
                        var sg = new GameObject("MyBody_" + i); sg.transform.SetParent(transform, false);
                        SelfBody = sg.AddComponent<SelfBodyView>(); SelfBody.Build(i, slot.DisplayName, slot.CosmeticId, rig);
                    }
                    PersonalSpace.Follow(transform, "MySpace_" + i, () => LocalHead != null ? LocalHead.position : (Vector3?)null);
                    continue;
                }
                if (GameServices.IsRemoteHuman(i) && !local)
                {
                    // Another human (online): avatar follows their tracked head + hands.
                    var hg = new GameObject("Human_" + i + "_" + slot.DisplayName); hg.transform.SetParent(transform, false);
                    hg.transform.position = VisualFactory.V(Def.SiblingSpawns[i]);
                    var hav = new GameObject("avatar").AddComponent<SiblingAvatar>(); hav.transform.SetParent(hg.transform, false);
                    hav.Build(i, slot.DisplayName, false, slot.CosmeticId);
                    var view = hg.AddComponent<RemoteHumanView>(); view.Build(i, hav, Palette.Siblings[((slot.CosmeticId % 4) + 4) % 4]);
                    int si = i; PersonalSpace.Follow(hg.transform, "space", () => { var f = GameServices.RemotePose; var pz = f != null ? f(si, 0) : null; return pz.HasValue ? pz.Value.position : (Vector3?)null; });
                    _remoteHumans[i] = view;
                    continue;
                }
                var g = new GameObject("Sibling_" + i + "_" + slot.DisplayName); g.transform.SetParent(transform, false);
                var av = new GameObject("avatar").AddComponent<SiblingAvatar>(); av.transform.SetParent(g.transform, false);
                av.Build(i, slot.DisplayName, slot.IsBot, slot.CosmeticId);
                if (replica)
                {
                    g.transform.position = VisualFactory.V(Def.SiblingSpawns[i]);
                    var pp = g.AddComponent<NetPuppet>(); pp.ActorId = i; pp.Avatar = av; _puppets[i] = pp;
                    continue;
                }
                var start = ClearSpawn(i, round, _botStarts); _botStarts.Add(start);
                AddAgent(g, start);
                g.transform.rotation = Quaternion.identity;
                var profile = slot.IsBot ? BotProfile.For(slot.Difficulty) : BotProfile.For(BotDifficulty.Normal);
                var body = g.AddComponent<BotBody>(); body.Init(i, profile, av);
                _botBodies[i] = body;
                _brains.Add(new BotBrain(i, profile, body, round, this, seed));
            }
            if (replica) round.NoiseRegistered += (e, resp) => RoomFx.ImpactAt(VisualFactory.V(e.Position), e.Material, e.Value, Def.Prop(e.SourceObjectId)?.Kind);
            round.Mom.StateChanged += (from, to) => Mom.OnStateChanged(from, to, round);
            round.Mom.Says += id => Mom.Say(id, round.Verdict != null && ActorIds.IsSibling(round.Verdict.Culprit) ? round.Roster[round.Verdict.Culprit].DisplayName : null);
            round.EliminationRequested += (slot, reason) => OnEliminated(slot);
        }

        void OnEliminated(int slot)
        {
            if (_botBodies.TryGetValue(slot, out var b))
            {
                b.DropCarriedGently(); b.Stop();
                RoomFx.Burst("heart", b.transform.position + Vector3.up * 1.6f, 0.3f, 3f);
                b.Avatar?.ShowStatus("Off to bed...", 4f);
                StartCoroutine(WalkOffToBed(b));
            }
        }

        System.Collections.IEnumerator WalkOffToBed(BotBody b)
        {
            yield return new WaitForSeconds(1.5f);
            if (b == null) yield break;
            b.Execute(new AgentCommand { Type = AgentCommandType.MoveTo, Target = VisualFactory.C(_shell.MomOffstage) });
            float t = 0; while (b != null && b.Busy && t < 12f) { t += Time.deltaTime; yield return null; }
            if (b != null) b.gameObject.SetActive(false);
        }

        /// <summary>QA only: bot brains stop issuing commands (a lab drives one bot body directly).</summary>
        public static bool QaPauseBrains;

        void Update()
        {
            if (Round == null || GameServices.IsReplica) return;
            float now = Round.Time;
            if (!QaPauseBrains) foreach (var br in _brains) { br.Tick(now); var bb = BodyFor(br.Slot); if (bb != null && bb.Avatar != null) bb.Avatar.SetPlotting(br.Plotting); }
            foreach (var p in Round.Pets) p.Tick(now);
            if (!Round.IsPlayable) foreach (var b in _botBodies.Values) if (b.Busy && Round.Phase == RoundPhase.MomSequence) b.Stop();
        }

        // ------------------------------------------------------------------ queries used by bodies and AI
        public PropBody Prop(int id) => _props.TryGetValue(id, out var p) ? p : null;
        public AnchorFixture Anchor(int id) => _anchors.TryGetValue(id, out var a) ? a : null;
        public IEnumerable<PropBody> Props => _props.Values;
        public IReadOnlyList<BotBrain> Brains => _brains;
        public BotBody BodyFor(int slot) => _botBodies.TryGetValue(slot, out var b) ? b : null;

        /// <summary>World position of a sibling's body, or of the local player's head (XR camera) for the human seat.</summary>
        public Vector3 ActorTransformOrHead(int actor)
        {
            if (_botBodies.TryGetValue(actor, out var b) && b != null) return b.transform.position + Vector3.up * 1.1f;
            if (actor != GameServices.LocalSlot && GameServices.RemotePose != null) { var rp = GameServices.RemotePose(actor, 0); if (rp.HasValue) return rp.Value.position; }
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.up * 1.4f;
        }

        public Transform ActorTransform(int actor)
        {
            if (_botBodies.TryGetValue(actor, out var b) && b.gameObject.activeInHierarchy) return b.transform;
            if (actor == GameServices.LocalSlot) return LocalHead;
            return null;
        }

        public AnchorFixture NearestOpenAnchor(Vector3 near, PropBody exclude)
        {
            AnchorFixture best = null; float bd = float.MaxValue;
            foreach (var a in _anchors.Values)
            {
                if (!a.Active || a.Occupant != null || a.Spec.Position.y < 0.3f) continue;
                float d = Vector3.Distance(a.transform.position, near);
                if (d < bd) { bd = d; best = a; }
            }
            return best;
        }

        /// <summary>Offset from a surface point toward the nearest open edge of its supporting surface (for precarious traps).</summary>
        public Vector3 EdgeOffset(Vector3 p, float dist)
        {
            Vector3 best = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                for (float d = 0.04f; d < 0.4f; d += 0.03f)
                {
                    if (!Physics.Raycast(p + dir * d + Vector3.up * 0.1f, Vector3.down, 0.2f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
                    { if (best == Vector3.zero || d < best.magnitude) best = dir * Mathf.Max(0f, d - dist * 0.5f); break; }
                }
            }
            return best == Vector3.zero ? Vector3.zero : best;
        }

        /// <summary>A bot reaching toward an anchor either notices a nearby armed trap (disarms it) or bumps it carelessly.</summary>
        public void ResolveTrapsNear(Vector3 anchorPos, int slot, BotProfile profile)
        {
            foreach (var p in _props.Values)
            {
                if (p.ArmedTrapId < 0 || p.IsHeld || Vector3.Distance(p.transform.position, anchorPos) > 0.4f) continue;
                var trap = Round.Ledger.GetTrap(p.ArmedTrapId); if (trap == null || trap.Owner == slot) continue;
                bool clueVisible = trap.WarningVisible && Round.Time - trap.ArmedAt >= Round.Ledger.CounterplayMinWarning;
                if (clueVisible && Random.value < profile.TrapNoticeChance)
                {
                    Round.DisarmTrap(p.ArmedTrapId, slot); p.ArmedTrapId = -1; RoomFx.TrapCleared(p);
                    _botBodies[slot].Avatar?.ShowStatus("Nice try!", 1.5f);
                }
                else
                {
                    Round.Push(slot, p.Spec.Id, 1.6f);
                    p.Body.AddForce((p.transform.position - anchorPos).normalized * 1.5f * p.Body.mass + Vector3.up * 0.5f, ForceMode.Impulse);
                    // Mark the disturbance so the prop's next impact resolves as this trap's trigger (careless only if the clue was fair).
                    p.MarkDisturbed(slot, clueVisible ? 1.6f : 0.5f);
                }
            }
        }

        // ------------------------------------------------------------------ IWorldQuery
        public Vec3 PropPosition(int propId) => _props.TryGetValue(propId, out var p) ? VisualFactory.C(p.transform.position) : default;
        public int PropHolder(int propId) => Round != null ? Round.HolderOf(propId) : ActorIds.None;
        public bool PropAvailable(int propId) => _props.TryGetValue(propId, out var p) && !p.IsHeld && !p.Reserved && p.gameObject.activeInHierarchy && p.Body != null && !p.Body.isKinematic;
        public Vec3 ActorPosition(int actor)
        {
            if (_botBodies.TryGetValue(actor, out var b)) return VisualFactory.C(b.transform.position);
            if (actor == GameServices.LocalSlot && LocalHead != null) return VisualFactory.C(new Vector3(LocalHead.position.x, 0, LocalHead.position.z));
            if (actor == ActorIds.Cat && _pets.TryGetValue(PetKind.Cat, out var c)) return VisualFactory.C(c.transform.position);
            if (actor == ActorIds.Dog && _pets.TryGetValue(PetKind.Dog, out var d)) return VisualFactory.C(d.transform.position);
            return default;
        }
        public int HeldBy(int actor) => Round != null ? Round.HeldBy(actor) : -1;

        public Vector3 SpectatorView(int i) => VisualFactory.V(Def.SpectatorViews[i % Def.SpectatorViews.Count]);
        public Vector3 SpawnFor(int slot) => VisualFactory.V(Def.SiblingSpawns[slot]);
    }
}
