using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// A grabbable household prop. Host-side physics: collisions become gameplay noise via the RoundController
    /// (material + relative speed + distance to Mom), bumps are attributed to the actor who caused them, armed traps are
    /// detected from precarious resting poses, and lost props are recovered so objectives always stay reachable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PropBody : MonoBehaviour
    {
        public PropSpec Spec;
        public Rigidbody Body { get; private set; }
        public Collider Col { get; private set; }
        public int HeldBy = ActorIds.None;
        public int HoldCount;
        public int LastActor = ActorIds.None;
        public float LastReleaseTime = -99f;
        public bool LastReleaseWasThrow;
        public int LastNoiseEventId = -1;
        public float LastNoiseTime = -99f;
        public int ArmedTrapId = -1;
        public float IgnoreImpactsUntil;
        /// <summary>While being snapped/validated (or just placed by a bot) AI must not pick it up again.</summary>
        public float ReservedUntil;
        public bool Reserved => Time.time < ReservedUntil;
        public bool IsHeld => HoldCount > 0;
        public float Speed => Body != null ? Body.linearVelocity.magnitude : 0f;
        /// <summary>Smoothed speed of actual movement (position change per physics step), not the commanded velocity.</summary>
        public float MovedSpeed { get; private set; }
        Vector3 _lastPos; bool _hasLastPos;
        // ~0.12 s of positions: a held item jittering against a surface (velocity-driven toward a hand inside it) has a high
        // per-step speed but almost no net displacement; a real slam travels.
        readonly Vector3[] _posRing = new Vector3[8]; int _posHead, _posCount;
        public Vector3 SpawnPos; public Quaternion SpawnRot;

        Renderer[] _renderers; Material[] _baseMats; Material[] _hiMats; bool _highlighted;
        float _settledTime; bool _trapChecked; float _outOfBoundsSince = -1f;
        int _disturbedBy = ActorIds.None; float _disturbedSpeed; float _disturbedAt = -99f;
        Bounds _roomBounds;

        public void Init(PropSpec spec, Bounds roomBounds)
        {
            Spec = spec; _roomBounds = roomBounds;
            Body = GetComponent<Rigidbody>();
            Body.mass = Mathf.Max(0.05f, spec.MassKg);
            Body.linearDamping = 0.05f; Body.angularDamping = 0.2f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // tunnelling guard for thrown props
            Body.maxAngularVelocity = 20f;
            var vis = transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject;
            var b = Prim.LocalRendererBounds(vis, transform);
            var authored = vis.transform.Find("AuthoredCollision");
            if (authored && authored.TryGetComponent<BoxCollider>(out var template))
            {
                // Reuse the collision shape shipped with authored art, in physics-root space.
                // The visual template remains disabled for tutorial/anchor ghost copies.
                var centre = template.center; var half = template.size * 0.5f; bool first = true;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                {
                    var point = transform.InverseTransformPoint(authored.TransformPoint(centre + Vector3.Scale(half, new Vector3(x, y, z))));
                    if (first) { b = new Bounds(point, Vector3.zero); first = false; } else b.Encapsulate(point);
                }
            }
            var sz = b.size;
            Collider col;
            if (IsRoundFlat(spec.Kind) && sz.y < Mathf.Min(sz.x, sz.z) * 0.5f)
            {
                // V10: a plate is a DISK, not a box. A box standing on edge has corners where a plate has none: in the KIT018 rack it
                // landed on the 3 mm cross rails instead of passing between them to the tray floor and rocked there (v10g: 441 clatters).
                var mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = DiskMesh(b.center, Mathf.Min(sz.x, sz.z) * 0.5f, Mathf.Max(0.012f, sz.y));
                mc.convex = true; col = mc;
            }
            else
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = new Vector3(Mathf.Max(0.02f, sz.x), Mathf.Max(0.015f, sz.y), Mathf.Max(0.02f, sz.z));
                col = box;
            }
            Col = col;
            var pm = new PhysicsMaterial("prop_" + spec.Material) { dynamicFriction = Friction(spec.Material), staticFriction = Friction(spec.Material) + 0.1f, bounciness = Bounce(spec.Material), bounceCombine = PhysicsMaterialCombine.Minimum };
            col.sharedMaterial = pm;
            _renderers = GetComponentsInChildren<Renderer>();
            _baseMats = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _baseMats[i] = _renderers[i].sharedMaterial;
            Prim.SetLayerRecursive(gameObject, Layers.Props);
            SpawnPos = transform.position; SpawnRot = transform.rotation;
        }

        /// <summary>Round flat kinds (plates, lids, coasters) get a convex disk collider instead of a box.</summary>
        public static bool IsRoundFlat(string kind) => kind == "plate" || kind == "kid_plate" || kind == "pot_lid" || kind == "coaster";

        static readonly System.Collections.Generic.Dictionary<long, Mesh> _disks = new System.Collections.Generic.Dictionary<long, Mesh>();
        /// <summary>16-sided convex cylinder (axis = local Y) with a slightly bevelled rim, centred on <paramref name="c"/>.</summary>
        static Mesh DiskMesh(Vector3 c, float r, float h)
        {
            long key = ((long)Mathf.RoundToInt(r * 2000f) << 40) ^ ((long)Mathf.RoundToInt(h * 2000f) << 20) ^ (long)Mathf.RoundToInt((c.y + 10f) * 1000f);
            if (_disks.TryGetValue(key, out var m) && m != null) return m;
            const int N = 16; var v = new Vector3[N * 2 + 2]; var tri = new System.Collections.Generic.List<int>();
            float rb = r * 0.88f; // the foot ring is narrower than the rim (a dished plate)
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N; float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                v[i] = new Vector3(c.x + cs * rb, c.y - h * 0.5f, c.z + sn * rb);
                v[N + i] = new Vector3(c.x + cs * r, c.y + h * 0.5f, c.z + sn * r);
            }
            v[2 * N] = new Vector3(c.x, c.y - h * 0.5f, c.z); v[2 * N + 1] = new Vector3(c.x, c.y + h * 0.5f, c.z);
            for (int i = 0; i < N; i++)
            {
                int j = (i + 1) % N;
                tri.AddRange(new[] { 2 * N, j, i, 2 * N + 1, N + i, N + j, i, j, N + j, i, N + j, N + i });
            }
            m = new Mesh { name = "plate_disk_collider" }; m.vertices = v; m.triangles = tri.ToArray(); m.RecalculateNormals(); m.RecalculateBounds();
            _disks[key] = m; return m;
        }

        static float Friction(NoiseMaterial m) => m == NoiseMaterial.Fabric || m == NoiseMaterial.Soft ? 0.9f : m == NoiseMaterial.Metal || m == NoiseMaterial.Glass ? 0.35f : 0.55f;
        static float Bounce(NoiseMaterial m) => m == NoiseMaterial.Toy || m == NoiseMaterial.Plastic ? 0.25f : 0.05f;

        public void SetHighlight(bool on, Color c)
        {
            if (_highlighted == on || _renderers == null) return;
            _highlighted = on;
            if (on && _hiMats == null)
            {
                _hiMats = new Material[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    var m = new Material(_baseMats[i]);
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 0.35f);
                    _hiMats[i] = m;
                }
            }
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].sharedMaterial = on ? _hiMats[i] : _baseMats[i];
        }

        /// <summary>
        /// While held, the item is driven by the hand: give it enough inertia to push other loose props aside instead of being
        /// blocked by a heavier pan or vase (xr_diag: a carried book stalled 20 cm behind the hand against a cup + pan).
        /// </summary>
        public void BoostHeldMass(bool on)
        {
            if (Body == null) return;
            Body.mass = on ? Mathf.Max(4f, Spec.MassKg * 3f) : Mathf.Max(0.05f, Spec.MassKg);
        }

        // ------------------------------------------------------------------ holding
        public void OnGrabbed(int actor)
        {
            HoldCount++; HeldBy = actor; LastActor = actor; Racked = false;
            _trapChecked = false; _settledTime = 0f;
            IgnoreImpactsUntil = Mathf.Max(IgnoreImpactsUntil, Time.time + 0.35f); // the grab pull-in into the fingers is not a slam
            if (ArmedTrapId >= 0 && GameServices.IsAuthority) { GameServices.Round?.DisarmTrap(ArmedTrapId, actor); ArmedTrapId = -1; RoomFx.TrapCleared(this); }
        }

        public void OnReleased(int actor, Vector3 velocity, bool thrown)
        {
            HoldCount = Mathf.Max(0, HoldCount - 1);
            if (HoldCount == 0) HeldBy = ActorIds.None;
            LastActor = actor; LastReleaseTime = Time.time; LastReleaseWasThrow = thrown;
            _trapChecked = false; _settledTime = 0f;
        }

        // ------------------------------------------------------------------ host physics -> gameplay
        void OnCollisionEnter(Collision c)
        {
            if (!GameServices.IsAuthority || !GameServices.RoundPlayable) return;
            var round = GameServices.Round;
            float rel = c.relativeVelocity.magnitude;
            // V7: a held item pressed against a surface is velocity-driven toward the hand and reports large contact velocities while
            // barely moving; judge a held impact by how fast the item REALLY moved (a real slam still arrives fast).
            if (IsHeld) rel = Mathf.Min(rel, MovedSpeed * 1.3f + 0.1f);

            // Attribute bumps from actors (hands, bot bodies, pets) and from other moving props.
            var tag = c.collider.GetComponentInParent<ActorTag>();
            if (tag != null && !IsHeld)
            {
                if (ActorIds.IsSibling(tag.ActorId) && !round.Roster[tag.ActorId].IsActive) return; // spectators never interact
                round.Push(tag.ActorId, Spec.Id, tag.Speed);
                LastActor = tag.ActorId;
                if (ArmedTrapId >= 0) { _disturbedBy = tag.ActorId; _disturbedSpeed = tag.Speed; _disturbedAt = Time.time; }
                if (ActorIds.IsPet(tag.ActorId)) RoomFx.PetBump(this);
                return;
            }
            if (tag != null && IsHeld) return; // a hand touching a held item (rubbing, a second grip) is not a slam
            var otherProp = c.collider.GetComponentInParent<PropBody>();
            if (otherProp != null && otherProp.Speed > Speed + 0.3f && !IsHeld)
            {
                int resp = round.Ledger.ResolveDirectActor(otherProp.Spec.Id, round.Time, out _, out _, out _);
                if (resp != ActorIds.None) { round.Push(resp, Spec.Id, otherProp.Speed); LastActor = resp; }
                if (ArmedTrapId >= 0 && resp != ActorIds.None) { _disturbedBy = resp; _disturbedSpeed = otherProp.Speed; _disturbedAt = Time.time; }
            }

            if (Time.time < IgnoreImpactsUntil) return;
            if (rel < (IsHeld ? 1.2f : 0.55f)) return;
            // Only the faster body of a prop-prop pair reports, so one clack is not counted twice.
            if (otherProp != null && otherProp.Speed > Speed) return;
            if (!ImpactGate.Accept(this, c, rel)) return;   // V10: rattles / resting chatter / same-cluster echoes are not new noises

            Vector3 point = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            NoiseEvent e;
            if (ArmedTrapId >= 0 && _disturbedBy != ActorIds.None && Time.time - _disturbedAt < 3f)
            {
                bool careless = _disturbedSpeed > 1.1f;
                e = round.SubmitTrapTrigger(ArmedTrapId, _disturbedBy, careless, Spec.Id, rel, VisualFactory.C(point));
                ArmedTrapId = -1;
            }
            else
            {
                int parent = -1;
                if (otherProp != null && Time.time - otherProp.LastNoiseTime < 2f) parent = otherProp.LastNoiseEventId;
                var cause = IsHeld ? NoiseCause.Slam : (LastReleaseWasThrow && Time.time - LastReleaseTime < 2.5f) ? NoiseCause.Throw : parent >= 0 ? NoiseCause.ChainReaction : NoiseCause.DirectImpact;
                e = round.SubmitImpact(Spec.Id, rel, VisualFactory.C(point), parent, cause, IsHeld ? HeldBy : ActorIds.None);
            }
            LastNoiseEventId = e.Id; LastNoiseTime = Time.time;
            RoomFx.Impact(this, point, e.Value, rel);
        }

        void FixedUpdate()
        {
            if (Body != null)
            {
                var pos = Body.position;
                if (_hasLastPos)
                {
                    float step = Mathf.Lerp(MovedSpeed, (pos - _lastPos).magnitude / Mathf.Max(1e-4f, Time.fixedDeltaTime), 0.5f);
                    float net = _posCount >= _posRing.Length ? (pos - _posRing[_posHead]).magnitude / (_posRing.Length * Mathf.Max(1e-4f, Time.fixedDeltaTime)) : step;
                    MovedSpeed = Mathf.Min(step, net);
                }
                _posRing[_posHead] = pos; _posHead = (_posHead + 1) % _posRing.Length; _posCount = Mathf.Min(_posCount + 1, _posRing.Length);
                _lastPos = pos; _hasLastPos = true;
            }
            if (!GameServices.IsAuthority || Body == null) return;
            // Recovery: props that leave the playable volume (or fall through) return to their spawn after a grace.
            bool outOfBounds = transform.position.y < -0.5f || !_roomBounds.Contains(transform.position);
            if (outOfBounds && !IsHeld)
            {
                if (_outOfBoundsSince < 0) _outOfBoundsSince = Time.time;
                else if (Time.time - _outOfBoundsSince > 1.5f) Respawn();
            }
            else _outOfBoundsSince = -1f;

            // Precarious-placement trap detection (humans; bots arm traps explicitly).
            if (!IsHeld && !_trapChecked && ArmedTrapId < 0 && Time.time - LastReleaseTime < 8f && ActorIds.IsSibling(LastActor))
            {
                if (Speed < 0.05f && Body.angularVelocity.magnitude < 0.1f) _settledTime += Time.fixedDeltaTime; else _settledTime = 0f;
                if (_settledTime > 0.6f)
                {
                    _trapChecked = true;
                    if (NoiseModel.Loudness(Spec.Material) >= 0.8f && IsPrecarious() && GameServices.RoundPlayable)
                        ArmAsTrap(LastActor);
                }
            }
        }

        public void MarkDisturbed(int actor, float speed) { _disturbedBy = actor; _disturbedSpeed = speed; _disturbedAt = Time.time; LastActor = actor; }

        public void ArmAsTrap(int owner)
        {
            var round = GameServices.Round; if (round == null) return;
            int id = round.ArmTrap(owner, Spec.Id, warningVisible: true);
            if (id >= 0) { ArmedTrapId = id; RoomFx.TrapArmed(this); }
        }

        /// <summary>Resting on an edge: the centre has no support beneath while at least one corner does.</summary>
        /// <summary>V10: set while the prop stands in a holder that is MEANT to hold it on edge (a plate between rack dividers).</summary>
        public bool Racked;

        bool IsPrecarious()
        {
            if (Racked) return false;
            var b = Col.bounds; float h = b.extents.y + 0.06f;
            int mask = Layers.EnvironmentMask | Layers.PropsMask;
            bool Hit(Vector3 p) { return Physics.Raycast(p, Vector3.down, out var hit, h, mask, QueryTriggerInteraction.Ignore) && hit.collider != Col; }
            bool centre = Hit(b.center);
            int corners = 0;
            Vector3 e = b.extents * 0.9f;
            if (Hit(b.center + new Vector3(e.x, 0, e.z))) corners++;
            if (Hit(b.center + new Vector3(-e.x, 0, e.z))) corners++;
            if (Hit(b.center + new Vector3(e.x, 0, -e.z))) corners++;
            if (Hit(b.center + new Vector3(-e.x, 0, -e.z))) corners++;
            bool tilted = Vector3.Angle(transform.up, Vector3.up) > 35f && b.center.y > 0.25f;
            return (!centre && corners > 0 && corners < 4 && b.center.y > 0.2f) || tilted;
        }

        public void Respawn()
        {
            _outOfBoundsSince = -1f; _posCount = 0; _hasLastPos = false;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(SpawnPos + Vector3.up * 0.05f, SpawnRot);
            Body.position = SpawnPos + Vector3.up * 0.05f; Body.rotation = SpawnRot;
            IgnoreImpactsUntil = Time.time + 1f;
            if (ArmedTrapId >= 0 && GameServices.IsAuthority) { GameServices.Round?.DisarmTrap(ArmedTrapId, ActorIds.Environment); ArmedTrapId = -1; RoomFx.TrapCleared(this); }
            Debug.Log($"[SHHH] recovered lost prop {Spec.Kind}#{Spec.Id}");
        }
    }
}
