using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (Quest run 2026-11-11: bots spawned on top of me and walked through me - Nell's hair passed through the camera). A human
    /// player is invisible to the NavMesh, so agents planned and steered straight through them. This puts a non-carving NavMeshObstacle
    /// (local avoidance only - paths and bot targets stay valid) on the floor under a tracked head, for the local player and for every
    /// remote human; bots and Mom steer around it. Presentation/movement only: no rule reads it.
    /// </summary>
    public sealed class PersonalSpace : MonoBehaviour
    {
        public const float Radius = 0.34f;
        public static readonly List<PersonalSpace> All = new List<PersonalSpace>();
        public Func<Vector3?> Head;
        public Vector3 HeadPos { get; private set; }
        public bool Live { get; private set; }
        NavMeshObstacle _ob;

        public static PersonalSpace Follow(Transform parent, string name, Func<Vector3?> head)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var p = g.AddComponent<PersonalSpace>(); p.Head = head;
            p._ob = g.AddComponent<NavMeshObstacle>();
            p._ob.shape = NavMeshObstacleShape.Capsule; p._ob.radius = Radius; p._ob.height = 1.9f; p._ob.center = new Vector3(0f, 0.95f, 0f);
            p._ob.carving = false;   // avoidance only: carving would make the stations next to a player unreachable
            return p;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void LateUpdate()
        {
            var h = Head != null ? Head() : null;
            Live = h.HasValue; if (_ob != null && _ob.enabled != Live) _ob.enabled = Live;
            if (!Live) return;
            var p = h.Value; HeadPos = p;
            float floor = p.y - 1.5f;
            if (NavMesh.SamplePosition(new Vector3(p.x, p.y - 1.2f, p.z), out var hit, 1.2f, NavMesh.AllAreas)) floor = hit.position.y;
            transform.position = new Vector3(p.x, floor, p.z);
        }

        /// <summary>Flat distance from a point to the nearest live player head (99 if none).</summary>
        public static float Nearest(Vector3 p)
        {
            float best = 99f;
            foreach (var s in All) { if (s == null || !s.Live) continue; var d = s.HeadPos - p; d.y = 0f; best = Mathf.Min(best, d.magnitude); }
            return best;
        }
    }
}
