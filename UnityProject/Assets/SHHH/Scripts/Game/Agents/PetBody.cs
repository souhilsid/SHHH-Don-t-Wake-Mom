using SHHH.Core;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// Cat or dog body: NavMesh roaming, procedural stylised model with tail wag / trot / ear twitch, and physical
    /// incidents (nudging a prop off a ledge creates a real fall and real noise attributed to the pet, or to the sibling who lured it).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class PetBody : MonoBehaviour, IAgentBody
    {
        public PetKind Kind;
        NavMeshAgent _agent;
        AgentCommand _cmd;
        bool _travel; float _act;
        Transform _tail, _head, _legs, _model;
        float _phase;
        public bool Busy { get; private set; }
        public bool LastSucceeded { get; private set; }
        public int HeldProp => -1;
        public Vec3 Position => VisualFactory.C(transform.position);
        public int ActorId => Kind == PetKind.Cat ? ActorIds.Cat : ActorIds.Dog;

        public void Init(PetKind kind)
        {
            Kind = kind;
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = kind == PetKind.Cat ? 1.3f : 1.6f; _agent.radius = 0.15f; _agent.height = 0.5f; _agent.angularSpeed = 600f; _agent.acceleration = 8f;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            var tag = gameObject.AddComponent<ActorTag>(); tag.ActorId = ActorId;
            var col = gameObject.AddComponent<CapsuleCollider>(); col.direction = 2; col.center = new Vector3(0, 0.2f, 0); col.radius = 0.12f; col.height = kind == PetKind.Cat ? 0.45f : 0.6f;
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;
            gameObject.layer = Layers.Agents;
            BuildModel();
        }

        AstraRig _rig; Vector3 _lastPos;

        PetAnimator _pa;
        /// <summary>V12 (owner): the Asset Store "Free Pack - Cartoon Dog, Cat" (PolyOne Studio) models + their Idle / Run clips, painted with
        /// the house toon shading. Falls back to the older pets only if the pack's files are missing.</summary>
        bool BuildPolyOne()
        {
            bool cat = Kind == PetKind.Cat; string n = cat ? "Cat" : "Dog";
            var prefab = Resources.Load<GameObject>("Pets/SM_CartoonAnimal_" + n);
            var idle = Resources.Load<AnimationClip>("Pets/A_CartoonAnimal-" + n + "_Idle");
            var run = Resources.Load<AnimationClip>(cat ? "Pets/A_CartoonAnimal-Cat__Run" : "Pets/A_CartoonAnimal-Dog_Run");
            if (prefab == null || idle == null || run == null) { Debug.LogWarning($"[SHHH][pets] PolyOne {n} missing (prefab {prefab != null}, idle {idle != null}, run {run != null}) - using the old pet"); return false; }
            var v = VisualFactory.Normalise(prefab, cat ? 0.34f : 0.5f, transform, "pet_" + n, byHeight: true, keepAnimators: true);
            // the pack's URP Unlit look -> the house's painted toon lighting (same texture)
            foreach (var r in v.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i]; var tex = src != null ? (src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.mainTexture) : null;
                    var m = new Material(Mats.Lit(Color.white, 0.12f)) { name = "SHHH_Toon_Pet_" + n }; if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
                    mats[i] = m;
                }
                r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            _model = v.transform; _pa = PetAnimator.Attach(v, idle, run); _pa.RunSpeedForClip = cat ? 1.1f : 1.5f;
            Debug.Log($"[SHHH][pets] {n}: PolyOne cartoon model + Idle/Run ({idle.length:0.00}/{run.length:0.00} s) height {(cat ? 0.34f : 0.5f):0.00} m");
            return true;
        }

        void BuildModel()
        {
            if (BuildPolyOne()) return;
            // V6: Astra's Mochi (cat) and Biscuit (dog) with authored walk/idle/startle clips; procedural fallback below.
            var pe = CharacterCatalog.I != null ? CharacterCatalog.I.Get(Kind == PetKind.Cat ? "mochi" : "biscuit") : null;
            if (pe != null)
            {
                _rig = AstraRig.Create(transform, pe, Kind == PetKind.Cat ? 0.36f : 0.5f);
                if (_rig != null) { _rig.WalkSpeedForClip = 0.8f; _rig.RunSpeedForClip = 2.0f; _model = _rig.transform; _lastPos = transform.position; return; }
            }
            _model = new GameObject("model").transform; _model.SetParent(transform, false);
            bool cat = Kind == PetKind.Cat;
            Color fur = cat ? Palette.Hex(0xF2A65A) : Palette.Hex(0xC8A27A), dark = cat ? Palette.Hex(0xC9733A) : Palette.Hex(0x7A5636);
            float s = cat ? 0.8f : 1f;
            var body = Prim.Shape(PrimitiveType.Capsule, _model, "body", new Vector3(0, 0.24f * s, 0), new Vector3(0.2f * s, 0.2f * s, 0.2f * s), Mats.Lit(fur));
            body.transform.localRotation = Quaternion.Euler(90, 0, 0); body.transform.localScale = new Vector3(0.22f * s, 0.24f * s, 0.2f * s);
            _head = Prim.Shape(PrimitiveType.Sphere, _model, "head", new Vector3(0, 0.36f * s, 0.24f * s), Vector3.one * 0.19f * s, Mats.Lit(fur)).transform;
            Prim.Shape(PrimitiveType.Sphere, _head, "eyeL", new Vector3(-0.22f, 0.12f, 0.42f), Vector3.one * 0.18f, Mats.Unlit(Palette.Ink));
            Prim.Shape(PrimitiveType.Sphere, _head, "eyeR", new Vector3(0.22f, 0.12f, 0.42f), Vector3.one * 0.18f, Mats.Unlit(Palette.Ink));
            Prim.Shape(PrimitiveType.Sphere, _head, "nose", new Vector3(0, -0.05f, 0.5f), Vector3.one * 0.12f, Mats.Unlit(Palette.Coral));
            if (cat)
            {
                for (int i = -1; i <= 1; i += 2) { var e = Prim.Shape(PrimitiveType.Cube, _head, "ear", new Vector3(0.28f * i, 0.45f, 0.05f), new Vector3(0.22f, 0.28f, 0.08f), Mats.Lit(dark)); e.transform.localRotation = Quaternion.Euler(0, 0, 45); }
            }
            else
            {
                for (int i = -1; i <= 1; i += 2) Prim.Shape(PrimitiveType.Capsule, _head, "ear", new Vector3(0.5f * i, 0.05f, -0.05f), new Vector3(0.18f, 0.3f, 0.12f), Mats.Lit(dark));
                Prim.Shape(PrimitiveType.Sphere, _head, "snout", new Vector3(0, -0.15f, 0.42f), new Vector3(0.45f, 0.35f, 0.5f), Mats.Lit(Palette.Cream));
            }
            _legs = new GameObject("legs").transform; _legs.SetParent(_model, false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 0.06f * s, z = (i < 2 ? 0.12f : -0.12f) * s;
                Prim.Shape(PrimitiveType.Capsule, _legs, "leg" + i, new Vector3(x, 0.08f * s, z), new Vector3(0.06f * s, 0.08f * s, 0.06f * s), Mats.Lit(dark));
            }
            var tailRoot = new GameObject("tail").transform; tailRoot.SetParent(_model, false); tailRoot.localPosition = new Vector3(0, 0.28f * s, -0.2f * s);
            var tail = Prim.Shape(PrimitiveType.Capsule, tailRoot, "t", new Vector3(0, 0.1f * s, -0.04f), new Vector3(0.04f, 0.12f * s, 0.04f), Mats.Lit(dark));
            tail.transform.localRotation = Quaternion.Euler(-30, 0, 0);
            _tail = tailRoot;
        }

        public void Execute(AgentCommand cmd)
        {
            _cmd = cmd; Busy = true; _travel = false; _act = 0f; LastSucceeded = false;
            Vector3 dest = VisualFactory.V(cmd.Target);
            if (cmd.Type == AgentCommandType.Knock) { var p = GameServices.Room?.Prop(cmd.PropId); if (p == null) { Busy = false; return; } dest = p.transform.position; }
            if (cmd.Type == AgentCommandType.Bark || cmd.Type == AgentCommandType.Wait) return;
            if (_agent.isOnNavMesh && NavMesh.SamplePosition(new Vector3(dest.x, 0, dest.z), out var hit, 1.5f, NavMesh.AllAreas)) { _agent.SetDestination(hit.position); _travel = true; }
        }

        public void Stop() { Busy = false; _travel = false; if (_agent != null && _agent.isOnNavMesh) _agent.ResetPath(); }

        void Update()
        {
            Animate();
            if (!Busy) return;
            if (_travel)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= (_cmd.Type == AgentCommandType.Knock ? 0.45f : 0.15f)) { _travel = false; _agent.ResetPath(); }
                else { _act += Time.deltaTime; if (_act > 12f) { Busy = false; } return; }
                _act = 0f;
            }
            _act += Time.deltaTime;
            var round = GameServices.Round;
            switch (_cmd.Type)
            {
                case AgentCommandType.Knock:
                {
                    var p = GameServices.Room?.Prop(_cmd.PropId);
                    if (p == null || p.IsHeld) { Busy = false; break; }
                    var d = p.transform.position - transform.position; d.y = 0;
                    if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 0.2f);
                    if (_act < 0.8f) break;
                    // The paw-swipe: a real nudge; the fall and its noise are resolved by physics + the noise model.
                    round?.Push(ActorId, p.Spec.Id, 1f);
                    p.LastActor = ActorId;
                    if (p.Body.isKinematic) p.Body.isKinematic = false;
                    p.Body.AddForce((d.normalized + Vector3.up * 0.3f) * 1.6f * p.Body.mass, ForceMode.Impulse);
                    if (Kind != PetKind.Cat) AudioBank.I?.Play("dog_huff", transform.position, 0.5f);   // V13: no meow on every swipe (too repetitive)
                    RoomFx.Burst("puff", p.transform.position + Vector3.up * 0.1f, 0.18f, 1.4f);
                    RoomFx.Say(transform.position + Vector3.up * 0.6f, "!", Palette.Yellow, 0.9f, 1.2f); // pet alert glyph
                    _rig?.Play(Kind == PetKind.Cat ? "react" : "pick");
                    LastSucceeded = true; Busy = false; break;
                }
                case AgentCommandType.Bark:
                    _rig?.Play(Kind == PetKind.Dog ? "startle" : "startle");
                    if (Kind == PetKind.Dog) { AudioBank.I?.Play("dog_bark", transform.position + Vector3.up * 0.4f, 0.9f); round?.SubmitPetSound(ActorId, Random.Range(6f, 13f), Position); RoomFx.Say(transform.position + Vector3.up * 0.7f, "WOOF!", Palette.Coral, 1f, 0.9f); }
                    else { AudioBank.I?.Play("cat_meow", transform.position, 0.7f); round?.SubmitPetSound(ActorId, Random.Range(2f, 5f), Position); }
                    LastSucceeded = true; Busy = false; break;
                default:
                    if (_act >= Mathf.Max(0.1f, _cmd.Duration)) { LastSucceeded = true; Busy = false; }
                    break;
            }
        }

        void Animate()
        {
            if (_pa != null) { var r = GameServices.Round; _pa.Excite = r != null && r.PetHunger[RoundController.PetIndex(ActorId)] >= RoundController.HungryAt ? 1f : 0f; return; }   // the clip mixer measures speed itself
            if (_rig != null)
            {
                // speed from actual motion: works for host NavMesh pets and network puppets alike
                float sp = (transform.position - _lastPos).magnitude / Mathf.Max(1e-3f, Time.deltaTime); _lastPos = transform.position;
                _rig.SetSpeed(sp);
                return;
            }
            float v = _agent != null ? _agent.velocity.magnitude : 0f;
            _phase += Time.deltaTime * (4f + v * 10f);
            if (_tail) _tail.localRotation = Quaternion.Euler(0, Mathf.Sin(_phase * (Kind == PetKind.Dog ? 2.2f : 0.8f)) * (Kind == PetKind.Dog ? 35f : 15f), 0);
            if (_model) _model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_phase)) * 0.02f * Mathf.Clamp01(v), 0);
            if (_head) _head.localRotation = Quaternion.Euler(Mathf.Sin(_phase * 0.3f) * 5f, Mathf.Sin(_phase * 0.17f) * 12f, 0);
            if (_legs) for (int i = 0; i < _legs.childCount; i++) _legs.GetChild(i).localRotation = Quaternion.Euler(Mathf.Sin(_phase + i * Mathf.PI * 0.5f) * 30f * Mathf.Clamp01(v), 0, 0);
        }
    }
}
