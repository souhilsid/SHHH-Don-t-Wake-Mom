using UnityEngine;

namespace SHHH.Game
{
    /// <summary>V12: a small standing label that turns (yaw only, slightly tipped back) to face the camera, so it reads from any side.</summary>
    public sealed class FaceViewer : MonoBehaviour
    {
        public float Tilt = -15f;
        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            var to = cam.transform.position - transform.position; to.y = 0f; if (to.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(-to) * Quaternion.Euler(Tilt, 0f, 0f);
        }
    }
}
