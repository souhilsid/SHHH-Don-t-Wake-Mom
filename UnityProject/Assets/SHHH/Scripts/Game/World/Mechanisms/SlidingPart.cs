using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 rail (mechanics 7 "Pulling" and 20 "Sliding"): a drawer / tray / detergent drawer that moves only along its rail between the
    /// closed stop and its travel limit. The hand stays on the handle: the part follows the hand's motion projected on the rail through a
    /// capped spring (a sideways tug does nothing); released it coasts with friction. The open stop is a mechanical stop (thunk, never
    /// detaches); the closed stop latches quietly, or slams (noise event) when shoved. A spring-loaded drawer (comedy incident 8) pops
    /// open by itself when <see cref="PopOpen"/> is called. Kinematic Rigidbody: things standing in the drawer ride with it by friction.
    /// </summary>
    public sealed class SlidingPart : MechPart
    {
        /// <summary>Rail direction (opening) in the parent's space.</summary>
        public Vector3 Axis = Vector3.forward;
        /// <summary>Travel in world metres.</summary>
        public float Travel = 0.3f;
        public float SlamSpeed = 0.55f, Friction = 4f, MaxSpeed = 1.6f, SoftCloseZone = 0.015f;
        public int NoiseKey = 960;

        public float Position { get; private set; }   // metres from closed
        public override float Opening => Travel > 1e-4f ? Mathf.Clamp01(Position / Travel) : 0f;
        public override bool IsClosed => Position < 0.002f;
        public float Velocity => _vel;
        public float? Drive;
        /// <summary>Scripted motion owned by a seat (network tests, bots): ease to an opening, or shove at a speed (negative = closing).</summary>
        public void ScriptedMove(int actor, float target01) { Touch(actor); Drive = target01; }
        public void Shove(int actor, float metresPerSecond) { Touch(actor); Drive = null; _vel = metresPerSecond; }

        Vector3 _restLocal; Rigidbody _rb; float _vel, _grabOffset, _releasedAt = -99f; bool _closedLatched = true, _init;

        public void Init(Vector3 axisWorld, float travel, Vector3 handleWorld, float grabRadius = 0.06f)
        {
            Axis = transform.parent != null ? transform.parent.InverseTransformDirection(axisWorld).normalized : axisWorld.normalized;
            Travel = travel; _restLocal = transform.localPosition; HandleLocal = transform.InverseTransformPoint(handleWorld); GrabRadius = grabRadius;
            _rb = GetComponent<Rigidbody>(); if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true; _rb.interpolation = RigidbodyInterpolation.Interpolate; _rb.useGravity = false;
            _init = true;
        }

        public Vector3 AxisW => AxisWorld;
        Vector3 AxisWorld => transform.parent != null ? transform.parent.TransformDirection(Axis).normalized : Axis;
        float Scale => transform.parent != null ? Mathf.Max(1e-4f, transform.parent.lossyScale.x) : 1f;
        float HandAlong(Vector3 w) => Vector3.Dot(w - (transform.parent != null ? transform.parent.TransformPoint(_restLocal) : _restLocal), AxisWorld);

        protected override void OnGrab(Vector3 at) { _grabOffset = Position - HandAlong(HandPoint); }
        protected override void OnRelease() { _releasedAt = Time.time; }

        /// <summary>Spring-loaded drawer: it shoots open on its own (incident 8) - catch the handle to stop it banging.</summary>
        public void PopOpen(float speed = 1.1f) { if (Hand == null && !Locked) _vel = speed; }

        void FixedUpdate()
        {
            if (!_init) return;
            float dt = Time.fixedDeltaTime, hi = Locked ? 0.002f : Travel;
            if (Hand != null)
            {
                float target = HandAlong(HandPoint) + _grabOffset;
                if (Locked && target > 0.02f) Rattle(HandleWorld);
                float want = Mathf.Clamp((target - Position) / 0.06f, -MaxSpeed, MaxSpeed);
                _vel = Mathf.Lerp(_vel, want, 0.45f);
                if (Vector3.Distance(HandPoint, HandleWorld) > 0.2f) EndGrab();
            }
            else if (Drive.HasValue)
            {
                float target = Mathf.Clamp01(Drive.Value) * Travel;
                _vel = Mathf.Clamp((target - Position) / 0.25f, -0.4f, 0.4f);
                if (Mathf.Abs(target - Position) < 0.002f) { Drive = null; _vel = 0f; LastLocalTouch = Time.time; }
            }
            else
            {
                _vel *= Mathf.Exp(-Friction * dt);
                if (Time.time - LastLocalTouch < 4f && Mathf.Abs(_vel) > (this is SlidingPart ? 0.02f : 4f)) LastLocalTouch = Time.time;   // still coasting from our push
                if (Position < SoftCloseZone && _vel < 0.05f && _vel > -0.2f && Position > 0f) _vel = Mathf.Lerp(_vel, -0.08f, 0.25f);   // a shove is not braked: it slams
                if (Remote.HasValue && Time.time - Mathf.Max(_releasedAt, LastLocalTouch) > 1.2f)   // never pull back a part THIS peer just moved (it reports where it rests first)
                {
                    float target = Mathf.Clamp01(Remote.Value) * Travel;
                    if (Mathf.Abs(target - Position) > 0.02f) _vel = Mathf.Clamp((target - Position) / 0.3f, -0.4f, 0.4f);
                }
            }
            float before = Position, slide = Mathf.Abs(_vel); Position += _vel * dt;
            if (Position > hi)
            {
                Position = hi;
                if (_vel > 0.45f && !Locked) { if (AudioBank.I != null) AudioBank.I.Play("impact_wood", HandleWorld, Mathf.Clamp01(_vel / 1.5f)); if (Hand != null) MechHaptics.Pulse(Hand, 0.7f, 0.05f); }
                _vel = Locked ? 0f : -_vel * 0.12f;
            }
            if (!_closedLatched && Position <= 0.001f)
            {
                float speed = Mathf.Max(slide, Mathf.Abs(before - Position) / dt); bool slam = speed > SlamSpeed;
                Position = 0f; _vel = 0f; _closedLatched = true;
                StopNoise(Mathf.InverseLerp(0.1f, 1.4f, speed), slam, HandleWorld, NoiseKey);
                Closed?.Invoke(this, LastActor, slam);
            }
            else if (Position < 0f) { Position = 0f; _vel = 0f; }
            else if (_closedLatched && Position > 0.02f) { _closedLatched = false; if (AudioBank.I != null) AudioBank.I.Play("lid_open", HandleWorld, 0.3f); Opened?.Invoke(this, LastActor); }
            var local = _restLocal + Axis * (Position / Scale);
            _rb.MovePosition(transform.parent != null ? transform.parent.TransformPoint(local) : local);
        }
    }
}
