using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (owner, Quest c15: "after the crash the mug and plate are gone and nothing puts them back"). Practice only: an item that has
    /// ended up on the floor (it started on furniture), fell out of the room or lies far from the bench for 4 s - and nobody holds it -
    /// is put back where the family left it, with a little poof, so the tutorial can always be finished. Practice has no rules state
    /// for these items (Mom only grumbles), so nothing else changes; matches never use this.
    /// </summary>
    public sealed class PracticeTidy : MonoBehaviour
    {
        public RoomRuntime Room;
        public int Returned { get; private set; }
        readonly Dictionary<PropBody, float> _lostSince = new Dictionary<PropBody, float>();
        float _next;

        void Update()
        {
            if (Room == null || !GameServices.PracticeMode || Time.time < _next) return;
            _next = Time.time + 0.5f;
            float floor = Room.transform.position.y;
            foreach (var p in Room.Props)
            {
                if (p == null || p.Spec == null || p.Body == null) continue;
                var home = Room.transform.TransformPoint(VisualFactory.V(p.Spec.Spawn));
                var pos = p.transform.position;
                bool startedHigh = home.y - floor > 0.3f;
                bool onFloor = startedHigh && pos.y - floor < 0.15f;
                bool gone = pos.y < floor - 0.5f || new Vector2(pos.x - Room.transform.position.x, pos.z - Room.transform.position.z).magnitude > 3.6f;
                bool lost = !p.IsHeld && (onFloor || gone);
                if (!lost) { _lostSince.Remove(p); continue; }
                if (!_lostSince.TryGetValue(p, out var since)) { _lostSince[p] = Time.time; continue; }
                if (Time.time - since < 4f) continue;
                _lostSince.Remove(p);
                RoomFx.Burst("toon_impact", pos + Vector3.up * 0.05f, 0.15f, 1.2f);
                p.Body.linearVelocity = Vector3.zero; p.Body.angularVelocity = Vector3.zero;
                var rot = Room.transform.rotation * Quaternion.Euler(0f, p.Spec.SpawnYaw, 0f);
                p.Body.position = home + Vector3.up * 0.02f; p.Body.rotation = rot; p.transform.SetPositionAndRotation(home + Vector3.up * 0.02f, rot);
                RoomFx.Burst("sparkle", home + Vector3.up * 0.1f, 0.15f, 1.2f);
                AudioBank.I?.Play("ui_hover", home, 0.4f, 0f, AudioBus.UI);
                Returned++;
            }
        }
    }
}
