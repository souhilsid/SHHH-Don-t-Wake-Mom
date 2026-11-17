using System.Text;
using SHHH.Automation;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>Play-mode snapshot of the live room: bot goals/bodies and every chore step's physical state.</summary>
    public static class ShhhRoomDebug
    {
        [ShhhCommand("debug_room")]
        public static string Dump()
        {
            var room = GameServices.Room; var r = GameServices.Round;
            if (room == null || r == null) return "no live room";
            var sb = new StringBuilder($"t={r.Time:0.0} phase={r.Phase} meter={r.Meter.Value:0.0} timeLeft={r.TimeRemaining:0}\n");
            foreach (var b in room.Brains)
            {
                var body = room.BodyFor(b.Slot);
                sb.AppendLine($"bot{b.Slot} goal='{b.DebugGoal}' busy={body?.Busy} ok={body?.LastSucceeded} held={body?.HeldProp} pos={body?.transform.position} onNav={body?.GetComponent<UnityEngine.AI.NavMeshAgent>()?.isOnNavMesh}");
            }
            foreach (var c in r.Board.Chores)
            {
                sb.AppendLine($"chore '{c.Title}' {c.State} {c.DoneSteps}/{c.Steps.Count} bonus={c.IsBonus}");
                foreach (var s in c.Steps)
                {
                    var p = room.Prop(s.PropId); var a = room.Anchor(s.AnchorId);
                    if (p == null || a == null) { sb.AppendLine($"   step prop{s.PropId} anchor{s.AnchorId} MISSING p={p != null} a={a != null}"); continue; }
                    sb.AppendLine($"   {p.Spec.Kind}#{s.PropId} done={s.Done} d={Vector3.Distance(p.transform.position, a.transform.position):0.00} dy={p.transform.position.y - a.transform.position.y:0.00} spd={p.Speed:0.00} held={p.IsHeld}/{p.HoldCount} kin={p.Body.isKinematic} col={p.Col.enabled} rel={Time.time - p.LastReleaseTime:0.0}s act={a.Active} occ={(a.Occupant != null)}");
                }
            }
            return sb.ToString();
        }
    }
}
