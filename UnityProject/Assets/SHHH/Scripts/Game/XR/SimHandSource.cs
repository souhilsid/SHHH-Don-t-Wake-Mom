using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Scriptable hand used in the Editor (no headset) and by automated play-mode tests.</summary>
    public sealed class SimHandSource : HandSource
    {
        public bool simTracked = true;
        public float simPinch, simGrip;
        public Vector3 palmNormal = Vector3.down;
        /// <summary>V13 finger gun: index out along transform.forward; simThumbDown = trigger.</summary>
        public bool simGun, simThumbDown;
        public override bool GunPose => simTracked && simGun;
        public override bool ThumbDown => simTracked && simThumbDown;
        public override Ray FingerRay => new Ray(IndexTip, transform.forward);
        public override bool Tracked => simTracked;
        public override bool HighConfidence => simTracked;
        public override float Pinch => simTracked ? simPinch : 0f;
        public override float Grip => simTracked ? simGrip : 0f;
        public override Vector3 PinchPoint => transform.position;
        public override Vector3 PalmCenter => transform.position - transform.forward * 0.04f;
        public override Vector3 PalmNormal => palmNormal;
        public override Vector3 IndexTip => transform.position + transform.forward * 0.03f;
        public override Quaternion HandRotation => transform.rotation;
        // QA: who destroys a simulated hand mid-test (KT01 run, 2026-11-17)
        void OnDestroy() { if (Application.isPlaying) Debug.Log("[SHHH][simhand] " + name + " destroyed at frame " + Time.frameCount + " " + System.Environment.StackTrace); }
    }
}
