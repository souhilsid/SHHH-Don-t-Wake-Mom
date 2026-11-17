using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (Quest run 2026-11-11: menu boards jumped to wherever the head pointed when each popup opened and were often seen at steep
    /// angles / half off-screen). A menu board keeps ONE stable spot in front of the player; it only glides back in front (0.5 s ease)
    /// when the player has clearly turned away (> 55 deg) or stepped away (> 0.7 m). World-anchored, never head-locked.
    /// </summary>
    public sealed class LecternFollow : MonoBehaviour
    {
        public float Distance = 0.62f;
        Vector3 _fromPos, _toPos; Quaternion _fromRot, _toRot; float _t = 1f;
        int _onTopCount = -1; float _onTopCheck;

        Transform Head { get { var rig = GameRoot.I != null ? GameRoot.I.Rig : null; return rig != null && rig.Head != null ? rig.Head : (Camera.main != null ? Camera.main.transform : null); } }

        // V12 owner 2026-11-12: my arms and hands never hide a menu board (built in the same frame - made on-top on the next one)
        void Start() { TaskHud.DrawOnTop(transform); _onTopCount = GetComponentsInChildren<Renderer>(true).Length; QaOnTopPasses++; }
        /// <summary>QA: how often this board was made on-top.</summary>
        public int QaOnTopPasses;

        void LateUpdate()
        {
            // rows / buttons added after the board was built (or rebuilt in place) are made on-top too
            if (Time.unscaledTime >= _onTopCheck)
            {
                _onTopCheck = Time.unscaledTime + 0.5f;
                var rs = GetComponentsInChildren<Renderer>(true); int n = rs.Length; bool low = false;
                foreach (var r in rs) { if (r is TMPro.TextMeshPro) continue; var m = r.sharedMaterial; if (m != null && m.renderQueue < 3990) { low = true; break; } }
                if (n != _onTopCount || low) { TaskHud.DrawOnTop(transform); _onTopCount = n; QaOnTopPasses++; }
            }
            var head = Head; if (head == null) return;
            if (_t >= 1f)
            {
                var toBoard = transform.position - head.position; toBoard.y = 0f;
                var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                bool turned = fwd.sqrMagnitude > 0.01f && toBoard.sqrMagnitude > 0.01f && Vector3.Angle(fwd, toBoard) > 55f;
                bool moved = Mathf.Abs(toBoard.magnitude - Distance) > 0.7f;
                if (turned || moved)
                {
                    _fromPos = transform.position; _fromRot = transform.rotation;
                    MenuBuilder.LecternPose(head, Distance, out _toPos, out _toRot);
                    _t = 0f;
                }
            }
            if (_t < 1f)
            {
                _t = Mathf.Min(1f, _t + Time.deltaTime / 0.5f);
                float e = Mathf.SmoothStep(0f, 1f, _t);
                transform.SetPositionAndRotation(Vector3.Lerp(_fromPos, _toPos, e), Quaternion.Slerp(_fromRot, _toRot, e));
                if (_t >= 1f) MenuBuilder.RememberLectern(transform);
            }
        }
    }
}
