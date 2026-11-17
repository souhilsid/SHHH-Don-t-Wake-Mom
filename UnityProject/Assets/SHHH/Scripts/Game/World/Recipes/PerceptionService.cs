using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V7 Gate 5, host only: decides who actually SAW each incident (a sabotage, a decisive crash) and records it as a witness memory
    /// through the journaled <see cref="RoundController.ReportWitness"/>. A sibling saw it when the spot was within 6 m, inside their
    /// field of view (bots 140°, players 110° around their head direction) and not hidden behind walls/furniture (line of sight to
    /// the spot). Confidence falls with distance and angle. Mom only witnesses while she is in the room (never during play).
    /// Remote players use their replicated head pose, the local player the rig head, bots their body/eye height.
    /// </summary>
    public sealed class PerceptionService : MonoBehaviour
    {
        RoomRuntime _room; RoundController _round;
        public const float MaxDistance = 6f, BotHalfFov = 70f, HumanHalfFov = 55f;
        public int Reported { get; private set; }

        public void Bind(RoomRuntime room, RoundController round) { _room = room; _round = round; }

        void Update()
        {
            if (!GameServices.IsAuthority || _round == null || !_round.BeliefMode) return;
            while (_round.TryTakeIncidentToPerceive(out var inc))
            {
                var spot = _room.transform.TransformPoint(VisualFactory.V(inc.Position)) + Vector3.up * 0.12f; // just above the work surface (inside a chest the lid line still counts)
                for (int s = 0; s < CompetitorRoster.SlotCount; s++)
                {
                    if (s == inc.TruthActor || !_round.Roster[s].IsActive) continue;
                    if (!Eye(s, out var eye, out var fwd, out float halfFov)) continue;
                    float conf = Sees(eye, fwd, halfFov, spot);
                    if (conf > 0f) { _round.ReportWitness(inc.Id, s, conf); Reported++; }
                }
            }
        }

        bool Eye(int slot, out Vector3 eye, out Vector3 fwd, out float halfFov)
        {
            eye = default; fwd = Vector3.forward; halfFov = HumanHalfFov;
            if (slot == GameServices.LocalSlot && _room.LocalHead != null) { eye = _room.LocalHead.position; fwd = _room.LocalHead.forward; return true; }
            if (GameServices.IsRemoteHuman != null && GameServices.IsRemoteHuman(slot) && GameServices.RemotePose != null)
            { var p = GameServices.RemotePose(slot, 0); if (!p.HasValue) return false; eye = p.Value.position; fwd = p.Value.rotation * Vector3.forward; return true; }
            var t = _room.ActorTransform(slot); if (t == null) return false;
            eye = t.position + Vector3.up * 1.05f; fwd = t.forward; halfFov = BotHalfFov; return true;
        }

        /// <summary>0 = not seen; otherwise a confidence in (0,1].</summary>
        public static float Sees(Vector3 eye, Vector3 fwd, float halfFov, Vector3 spot)
        {
            var d = spot - eye; float dist = d.magnitude; if (dist > MaxDistance || dist < 0.01f) return dist < 0.01f ? 1f : 0f;
            float ang = Vector3.Angle(fwd, d); if (ang > halfFov) return 0f;
            // occluded by the house (walls, furniture) - props and people do not block
            if (Physics.Linecast(eye, spot, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) return 0f;
            return Mathf.Clamp01(1f - dist / (MaxDistance + 1f)) * Mathf.Lerp(1f, 0.6f, ang / halfFov);
        }
    }
}
