using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 hinge (mechanic 21 "Hinging"): a door / lid / flap that rotates about its real hinge line between the closed stop (0°) and an
    /// open limit. Grasped at the handle it follows the hand's angle around the hinge (capped spring, so a fast yank cannot spin it); let go
    /// it keeps its swing and slows by friction. Near the closed stop a soft-close spring draws it shut and it latches with a quiet click;
    /// arriving faster than <see cref="SlamSpeed"/> is a slam (one Mom-audible noise event). Locked (washer running) it only rattles.
    /// The part carries a kinematic Rigidbody, so its colliders push props (a door swung into a bundle shoves it).
    /// </summary>
    public sealed class HingedPart : MechPart
    {
        /// <summary>Hinge axis in the parent's space (through this transform's origin = the hinge pivot).</summary>
        public Vector3 Axis = Vector3.up;
        /// <summary>Open limit in degrees (sign = opening direction).</summary>
        public float OpenLimit = 100f;
        public float SlamSpeed = 150f;      // deg/s into the closed stop
        public float SoftCloseZone = 7f;    // deg: the latch spring takes over
        public float Friction = 2.2f;       // 1/s velocity decay when free
        public float MaxSpeed = 420f;
        public int NoiseKey = 950;

        public float Angle { get; private set; }
        public float Velocity => _vel;
        public override float Opening => Mathf.Clamp01(Angle / OpenLimit);
        public override bool IsClosed => Mathf.Abs(Angle) < 0.5f;

        Quaternion _rest; Rigidbody _rb; float _vel, _grabOffset, _releasedAt = -99f, _creakAt; bool _closedLatched = true, _init;

        /// <summary>axisWorld: hinge direction in world space; open: degrees (sign = direction that swings toward the user).</summary>
        public void Init(Vector3 axisWorld, float open, Vector3 handleWorld, float grabRadius = 0.07f)
        {
            Axis = transform.parent != null ? transform.parent.InverseTransformDirection(axisWorld).normalized : axisWorld.normalized;
            OpenLimit = open; _rest = transform.localRotation; HandleLocal = transform.InverseTransformPoint(handleWorld); GrabRadius = grabRadius;
            _rb = GetComponent<Rigidbody>(); if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true; _rb.interpolation = RigidbodyInterpolation.Interpolate; _rb.useGravity = false;
            _init = true;
        }

        /// <summary>Which sign of rotation moves <paramref name="pointWorld"/> toward <paramref name="towardWorld"/> (pick the outward swing).</summary>
        public static float OutwardSign(Transform pivot, Vector3 axisWorld, Vector3 pointWorld, Vector3 towardWorld)
        {
            var r = pointWorld - pivot.position; var moved = Quaternion.AngleAxis(10f, axisWorld) * r;
            return Vector3.Dot(moved - r, towardWorld) >= 0f ? 1f : -1f;
        }

        public Vector3 AxisW => AxisWorld;
        Vector3 AxisWorld => transform.parent != null ? transform.parent.TransformDirection(Axis).normalized : Axis;

        /// <summary>Angle of the hand around the hinge (deg) in a fixed reference frame; NaN when the hand is too close to the hinge line.</summary>
        float HandAngle(Vector3 world)
        {
            var ax = AxisWorld; var v = Vector3.ProjectOnPlane(world - transform.position, ax);
            if (v.magnitude < 0.03f) return float.NaN;
            var refv = Vector3.ProjectOnPlane(transform.parent != null ? transform.parent.TransformDirection(_rest * Vector3.right) : Vector3.right, ax);
            if (refv.sqrMagnitude < 1e-6f) refv = Vector3.ProjectOnPlane(Vector3.forward, ax);
            return Vector3.SignedAngle(refv, v, ax);
        }

        protected override void OnGrab(Vector3 at) { float h = HandAngle(HandPoint); _grabOffset = float.IsNaN(h) ? 0f : Mathf.DeltaAngle(h, Angle); }
        protected override void OnRelease() { _releasedAt = Time.time; }

        /// <summary>Bots / scripted repair: swing toward a target opening at hand speed (no teleport).</summary>
        public float? Drive;
        public void ScriptedMove(int actor, float target01) { Touch(actor); Drive = target01; }
        public void Shove(int actor, float degPerSecond) { Touch(actor); Drive = null; _vel = degPerSecond; }

        void FixedUpdate()
        {
            if (!_init) return;
            float dt = Time.fixedDeltaTime, lo = Mathf.Min(0f, OpenLimit), hi = Mathf.Max(0f, OpenLimit), dir = Mathf.Sign(OpenLimit);
            if (Locked) { hi = dir > 0 ? 1.5f : 0f; lo = dir > 0 ? 0f : -1.5f; }
            if (Hand != null)
            {
                float h = HandAngle(HandPoint);
                if (!float.IsNaN(h))
                {
                    float target = h + _grabOffset;
                    target = Angle + Mathf.DeltaAngle(Angle, target);
                    if (Locked && Mathf.Abs(target) > 6f) Rattle(HandleWorld);
                    float want = Mathf.Clamp((target - Angle) / 0.07f, -MaxSpeed, MaxSpeed);
                    _vel = Mathf.Lerp(_vel, want, 0.45f);
                }
                // the hand left the handle (part stopped by a limit or lock): the grip breaks, the hand is never dragged
                if (Vector3.Distance(HandPoint, GrabPointWorld) > 0.22f) EndGrab();
            }
            else if (Drive.HasValue)
            {
                float target = Mathf.Clamp01(Drive.Value) * OpenLimit;
                _vel = Mathf.Clamp((target - Angle) / 0.25f, -90f, 90f);
                if (Mathf.Abs(target - Angle) < 0.5f) { Drive = null; _vel = 0f; LastLocalTouch = Time.time; }
            }
            else
            {
                _vel *= Mathf.Exp(-Friction * dt);
                if (Time.time - LastLocalTouch < 4f && Mathf.Abs(_vel) > (this is SlidingPart ? 0.02f : 4f)) LastLocalTouch = Time.time;   // still coasting from our push
                // soft-close spring near the latch; it only pulls if the door is not swinging open
                if (Mathf.Abs(Angle) < SoftCloseZone && Angle * dir > 0f && _vel * dir < 25f && Mathf.Abs(_vel) < 60f) _vel = Mathf.Lerp(_vel, -dir * 30f, 0.2f);   // only a gentle close is caught by the latch spring; a shove slams
                // replicated opening (another player's hand or a bot moved it): ease there unless this peer just let go
                if (Remote.HasValue && Time.time - Mathf.Max(_releasedAt, LastLocalTouch) > 1.2f)   // never pull back a part THIS peer just moved (it reports where it rests first)
                {
                    float target = Mathf.Clamp01(Remote.Value) * OpenLimit;
                    if (Mathf.Abs(target - Angle) > 6f) _vel = Mathf.Clamp((target - Angle) / 0.3f, -120f, 120f);
                }
            }
            float before = Angle, swing = Mathf.Abs(_vel); Angle += _vel * dt;
            if (Angle < lo || Angle > hi)
            {
                bool closedStop = (dir > 0 && Angle < lo) || (dir < 0 && Angle > hi);
                Angle = Mathf.Clamp(Angle, lo, hi);
                if (!closedStop) { if (Mathf.Abs(_vel) > 160f && AudioBank.I != null) AudioBank.I.Play("impact_wood", HandleWorld, 0.35f); _vel = -_vel * 0.15f; }   // bump the open limit
                else if (Locked) { _vel = 0f; }
            }
            // closed: latch (quiet click) or slam (noise event); report state changes to the owner
            if (!_closedLatched && Mathf.Abs(Angle) < 0.5f)
            {
                float speed = Mathf.Max(swing, Mathf.Abs(before - Angle) / dt); bool slam = speed > SlamSpeed;   // the door's swing speed as it meets the frame
                Angle = 0f; _vel = 0f; _closedLatched = true;
                StopNoise(Mathf.InverseLerp(40f, 400f, speed), slam, HandleWorld, NoiseKey);
                Closed?.Invoke(this, LastActor, slam);
            }
            else if (_closedLatched && Mathf.Abs(Angle) > 12f)
            {
                _closedLatched = false;
                if (AudioBank.I != null) AudioBank.I.Play("lid_open", HandleWorld, 0.35f);
                Opened?.Invoke(this, LastActor);
            }
            if (Mathf.Abs(_vel) > 25f && Mathf.Abs(_vel) < 140f && Time.time > _creakAt && AudioBank.I != null) { _creakAt = Time.time + 1.6f; AudioBank.I.Play("door_creak", HandleWorld, 0.18f, 0.1f); }
            var rot = Quaternion.AngleAxis(Angle, Axis) * _rest;
            if (transform.parent != null) _rb.MoveRotation(transform.parent.rotation * rot); else _rb.MoveRotation(rot);
        }
    }
}
