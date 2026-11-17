using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (Quest run 2026-11-11 #21: the boundary grid came up while I walked to the dog bed - the jobs are spread over the house and
    /// the play space is small). How close my real (tracked) head is to the edge of my play space: the runtime's play-area rectangle when
    /// it reports one (OVRBoundary, else the XR input subsystem's boundary points); otherwise how far I have physically walked from where
    /// the match started. The HUD uses it to remind me to walk with the gesture instead of my feet.
    /// </summary>
    public static class Playspace
    {
        static Rect _area; static bool _haveArea; static float _nextTry;
        static readonly List<XRInputSubsystem> _subs = new List<XRInputSubsystem>();
        static readonly List<Vector3> _pts = new List<Vector3>();
        static Vector3? _start;

        /// <summary>Forget the starting spot (new match / respawn).</summary>
        public static void MarkStart(PlayerRig rig) { _start = rig != null && rig.Head != null ? Local(rig) : (Vector3?)null; }

        static Vector3 Local(PlayerRig rig) { var p = rig.transform.InverseTransformPoint(rig.Head.position); p.y = 0f; return p; }

        /// <summary>Metres from my head to the play-space edge (negative = outside), or to the 1.1 m "walked away" ring.</summary>
        public static float EdgeDistance(PlayerRig rig, out string source)
        {
            source = "none";
            if (rig == null || rig.Head == null) return 99f;
            var p = Local(rig);
            if (!_haveArea && Time.unscaledTime >= _nextTry) { _nextTry = Time.unscaledTime + 5f; TryArea(); }
            if (_haveArea)
            {
                source = "play area";
                return Mathf.Min(Mathf.Min(p.x - _area.xMin, _area.xMax - p.x), Mathf.Min(p.z - _area.yMin, _area.yMax - p.z));
            }
            if (_start == null) _start = p;
            source = "walked";
            return 1.1f - Vector3.Distance(p, _start.Value);
        }

        static void TryArea()
        {
            try
            {
                if (OVRManager.boundary != null && OVRManager.boundary.GetConfigured())
                {
                    var d = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                    if (d.x > 0.5f && d.z > 0.5f) { _area = new Rect(-d.x * 0.5f, -d.z * 0.5f, d.x, d.z); _haveArea = true; return; }
                }
            }
            catch { }
            SubsystemManager.GetSubsystems(_subs);
            foreach (var s in _subs)
            {
                _pts.Clear();
                if (s == null || !s.running || !s.TryGetBoundaryPoints(_pts) || _pts.Count < 3) continue;
                float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                foreach (var q in _pts) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.z); z1 = Mathf.Max(z1, q.z); }
                // the inscribed half of a free-form boundary is safer than its bounding box
                float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, hx = (x1 - x0) * 0.35f, hz = (z1 - z0) * 0.35f;
                if (hx > 0.25f && hz > 0.25f) { _area = new Rect(cx - hx, cz - hz, hx * 2f, hz * 2f); _haveArea = true; return; }
            }
        }
    }
}
