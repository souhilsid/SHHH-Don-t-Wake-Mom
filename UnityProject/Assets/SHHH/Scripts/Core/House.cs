using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>
    /// 2026-11-09 directive: one continuous match plays in the whole connected house. The house prefab is one model; each built
    /// room is an offset inside it (the same offsets the Unity <c>HouseAssembly.Center</c> uses). House coordinates = living-room
    /// coordinates (the living room is the origin), so a "whole house" round keeps every legacy room-local position valid by adding
    /// the room offset.
    /// </summary>
    public static class HouseLayout
    {
        /// <summary>Rooms that are built and playable today, in the order jobs are dealt (job k of every seat goes to Rooms[k]).</summary>
        public static readonly RoomId[] Rooms = { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry };

        public static Vec3 Offset(RoomId id) => id == RoomId.Kitchen ? new Vec3(6.9f, 0, 0) : id == RoomId.Laundry ? new Vec3(6.4f, 0, 6f) : new Vec3(0, 0, 0);

        /// <summary>The built room whose floor contains (or is nearest to) a house-space point.</summary>
        public static RoomId RoomAt(Vec3 p)
        {
            RoomId best = RoomId.LivingRoom; float bd = float.MaxValue;
            foreach (var r in Rooms)
            {
                var d = RoomCatalog.Get(r); var o = Offset(r);
                float dx = Math.Max(0f, Math.Abs(p.x - o.x) - d.Width * 0.5f), dz = Math.Max(0f, Math.Abs(p.z - o.z) - d.Depth * 0.5f);
                float dist = dx * dx + dz * dz;
                if (dist < bd - 1e-6f) { bd = dist; best = r; }
            }
            return best;
        }

        public static string PlaceName(RoomId r) => r == RoomId.Kitchen ? "kitchen" : r == RoomId.Laundry ? "laundry room" : r == RoomId.LivingRoom ? "living room" : "playroom";
        public static string PlaceAt(Vec3 p) => PlaceName(RoomAt(p));

        public static RecipeStation Shift(RecipeStation s, Vec3 o) { var r = new RecipeStation(s.Center.x + o.x, s.Center.y + o.y, s.Center.z + o.z, s.Length, s.Depth, s.AlongZ, s.Front); if (s.HasSink) r = r.WithSink(new Vec3(s.Sink.x + o.x, s.Sink.y + o.y, s.Sink.z + o.z), s.SinkYaw); if (s.Anchors != null) foreach (var an in s.Anchors) r = r.WithAnchor(an.Kind, new Vec3(an.Pos.x + o.x, an.Pos.y + o.y, an.Pos.z + o.z), an.Yaw, an.Prop); return r; }

        /// <summary>Clamp a point to the floor of the room it is in (keeps hide spots inside walls).</summary>
        public static Vec3 ClampToRoom(Vec3 p, float margin = 0.38f)
        {
            var r = RoomAt(p); var d = RoomCatalog.Get(r); var o = Offset(r);
            return new Vec3(MathX.Clamp(p.x, o.x - d.Width * margin, o.x + d.Width * margin), p.y, MathX.Clamp(p.z, o.z - d.Depth * margin, o.z + d.Depth * margin));
        }
    }

    /// <summary>Tunable continuous-match rules (data, one place). Values documented in Docs/Continuous_Match_Rules.md.</summary>
    public static class MatchRules
    {
        public const float FiveMinutes = 300f, TenMinutes = 600f;
        /// <summary>Mom's penalty after a proven disturbance: a short, funny "sent to bed" timeout plus points.</summary>
        public const float TimeoutSeconds = 15f, PenaltyPoints = 20f;
        /// <summary>The first confrontation of a match is the full one; later critical events get a quick challenge.</summary>
        public const float FullPresent = 8f, FullEvidence = 25f, QuickPresent = 3f, QuickEvidence = 12f;
        /// <summary>Jobs dealt to every seat: one per room (5 min = living + kitchen, 10 min = the three built rooms).</summary>
        public static int JobsPerSeat(float seconds) => seconds >= TenMinutes - 1f ? 3 : 2;
    }
}
