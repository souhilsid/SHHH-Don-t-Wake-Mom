using UnityEngine;

namespace SHHH.Game
{
    public enum Handedness { Left = 0, Right = 1 }

    /// <summary>Per-frame tracked-hand data in world space. Implemented by OVR hand tracking, and by a simulator in the Editor.</summary>
    public abstract class HandSource : MonoBehaviour
    {
        public Handedness Hand;
        public abstract bool Tracked { get; }
        public abstract bool HighConfidence { get; }
        /// <summary>Index-thumb pinch 0..1.</summary>
        public abstract float Pinch { get; }
        /// <summary>Whole-hand curl (fist) 0..1.</summary>
        public abstract float Grip { get; }
        /// <summary>Point between thumb and index tips — where grabbed objects attach.</summary>
        public abstract Vector3 PinchPoint { get; }
        public abstract Vector3 PalmCenter { get; }
        /// <summary>Unit vector pointing OUT of the palm.</summary>
        public abstract Vector3 PalmNormal { get; }
        public abstract Vector3 IndexTip { get; }
        public abstract Quaternion HandRotation { get; }
        /// <summary>System gesture (Meta menu pinch) in progress — never interpret as gameplay input.</summary>
        public virtual bool SystemGesture => false;
        /// <summary>Optional pointer ray for teleport aiming.</summary>
        public virtual Ray PointerRay => new Ray(PinchPoint, HandRotation * Vector3.forward);
        /// <summary>V12: all four fingers straight and not pinching - an open hand always lets go.</summary>
        public virtual bool OpenHand => Pinch < 0.1f && Grip < 0.1f;
        /// <summary>V13 finger gun (owner: "make the teleport easier, using finger gun gesture and same for walking"): index finger
        /// straight, middle / ring / little curled, no pinch. Used for locomotion; it never starts a grab.</summary>
        public virtual bool GunPose => false;
        /// <summary>V13: the thumb lowered onto the side of the index / middle finger (the finger gun's "trigger").</summary>
        public virtual bool ThumbDown => false;
        /// <summary>V13: where the index finger points (origin at the fingertip).</summary>
        public virtual Ray FingerRay => PointerRay;
    }
}
