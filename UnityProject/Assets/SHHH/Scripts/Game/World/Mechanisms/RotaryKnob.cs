using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 dial / knob (mechanic 6 "Twisting"): pinched, it turns with the hand's twist about the knob's own axis (not by touching it),
    /// between two stops, clicking through detents (sound + controller pulse per detent). Released it settles into the nearest detent.
    /// <see cref="Detent"/> is the selected position (a washer programme, an oven setting).
    /// </summary>
    public sealed class RotaryKnob : MechPart
    {
        public Vector3 Axis = Vector3.forward;   // parent space, pointing out of the panel
        public float MaxAngle = 270f;
        public int Detents = 6;
        public float Angle { get; private set; }
        public int Detent => Detents <= 1 ? 0 : Mathf.RoundToInt(Angle / MaxAngle * (Detents - 1));
        public override float Opening => MaxAngle > 0 ? Angle / MaxAngle : 0f;
        public override bool IsClosed => Detent == 0;
        public System.Action<RotaryKnob, int, int> DetentChanged;   // (knob, detent, actor)
        public int Clicks { get; private set; }

        Quaternion _rest, _hand0; float _angle0, _twistAcc, _twistLast; int _lastDetent; bool _init;
        public float? Drive;

        public void Init(Vector3 axisWorld, float maxAngle, int detents, Vector3 handleWorld, float grabRadius = 0.045f)
        {
            Axis = transform.parent != null ? transform.parent.InverseTransformDirection(axisWorld).normalized : axisWorld.normalized;
            MaxAngle = maxAngle; Detents = detents; _rest = transform.localRotation; HandleLocal = transform.InverseTransformPoint(handleWorld); GrabRadius = grabRadius; _init = true;
        }

        Vector3 AxisWorld => transform.parent != null ? transform.parent.TransformDirection(Axis).normalized : Axis;

        protected override void OnGrab(Vector3 at) { _hand0 = Hand.Source.HandRotation; _angle0 = Angle; _twistAcc = 0f; _twistLast = 0f; }
        float _releasedAt = -99f;
        protected override void OnRelease() { _releasedAt = Time.time; }

        /// <summary>Twist of the hand since the grab, about the knob axis (deg, clockwise seen from the front = positive).</summary>
        float Twist()
        {
            var ax = AxisWorld; var r = Vector3.Cross(ax, Mathf.Abs(Vector3.Dot(ax, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var q = Hand.Source.HandRotation * Quaternion.Inverse(_hand0);
            var v = Vector3.ProjectOnPlane(q * r, ax);
            return v.sqrMagnitude < 1e-6f ? 0f : -Vector3.SignedAngle(r, v, ax);
        }

        void Update()
        {
            if (!_init) return;
            float target;
            if (Hand != null)
            {
                float tw = Twist(); _twistAcc += Mathf.DeltaAngle(_twistLast, tw); _twistLast = tw;   // unwrapped: turns beyond 180 deg keep counting
                target = Mathf.Clamp(_angle0 + _twistAcc, 0f, MaxAngle);
                if (Vector3.Distance(HandPoint, HandleWorld) > 0.15f) EndGrab();
            }
            else if (Drive.HasValue) { target = Mathf.Clamp01(Drive.Value) * MaxAngle; if (Mathf.Abs(target - Angle) < 0.5f) Drive = null; }
            else
            {
                float step = Detents > 1 ? MaxAngle / (Detents - 1) : MaxAngle;
                target = Remote.HasValue && Time.time - _releasedAt > 1.2f ? Mathf.Clamp01(Remote.Value) * MaxAngle : Mathf.Round(Angle / step) * step;   // settle into the detent (or follow another player's turn)
            }
            Angle = Mathf.MoveTowards(Angle, target, (Hand != null ? 720f : 240f) * Time.deltaTime);
            int d = Detent;
            if (d != _lastDetent)
            {
                _lastDetent = d; Clicks++;
                if (AudioBank.I != null) AudioBank.I.Play("ui_click", transform.position, 0.45f, 0.1f);
                if (Hand != null) MechHaptics.Pulse(Hand, 0.25f, 0.02f);
                DetentChanged?.Invoke(this, d, LastActor);
            }
            transform.localRotation = Quaternion.AngleAxis(-Angle, Axis) * _rest;
        }
    }
}
