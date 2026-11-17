using System;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>QA: runs one queued capture at the very end of LateUpdate (after every body / IK driver), i.e. the state the frame is
    /// rendered in. WaitForEndOfFrame does not fire in the Editor when the Game view is not repainting.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class QaLateShooter : MonoBehaviour
    {
        public static Action Pending;
        static QaLateShooter _i;
        public static void Ensure() { if (_i == null) _i = new GameObject("QA_LateShooter").AddComponent<QaLateShooter>(); }
        void LateUpdate() { var a = Pending; Pending = null; a?.Invoke(); }
    }
}
