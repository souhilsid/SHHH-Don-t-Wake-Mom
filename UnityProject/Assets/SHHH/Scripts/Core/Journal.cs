using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHHH.Core
{
    /// <summary>
    /// V6 NET-G4: deterministic match journal. The online HOST records every call made into the rules engine
    /// (MatchController / RoundController public API, including every Tick dt) in order; clients replay the byte stream
    /// into their own MatchController, which therefore reaches exactly the same state, fires exactly the same events
    /// (Mom lines, verdicts, eliminations, chore steps) and drives the unchanged Unity presentation layer.
    /// Only outermost calls are recorded (calls made by the engine into itself are reproduced by the replay).
    /// A late/reconnecting client simply receives the whole journal so far and fast-forwards.
    /// </summary>
    public sealed class MatchJournal
    {
        public enum Op : byte
        {
            MStart = 1, MTick, MSkipIntro, MRoomReady, MDisconnected, MReconnected, MHostLeft, MRematch, MChecksum,
            RGrab = 30, RRelease, RPush, RPlaced, RRemoved, RImpact, ROperate, RPetSound, RTrap, RArmTrap, RDisarm, RCatch,
            RActivity, RTracking, RTieStep, RForceResolve, RRecipe, RWitness, RCard, RAccuse, RPass,
            RPetEat, RTrash,   // V12 (appended): a pet ate from its bowl; a piece of trash went into a bin
        }

        readonly MemoryStream _ms = new MemoryStream(64 * 1024);
        readonly BinaryWriter _w;
        int _depth;
        public bool Recording = true;
        /// <summary>Bytes written since the last <see cref="TakePending"/> (what the host still has to send).</summary>
        int _sentUpTo;

        public MatchJournal() { _w = new BinaryWriter(_ms, Encoding.UTF8, true); }

        public long Length => _ms.Length;
        public byte[] ToArray() => _ms.ToArray();

        /// <summary>New bytes since the previous call (host streams these to clients every network tick).</summary>
        public byte[] TakePending()
        {
            int n = (int)_ms.Length - _sentUpTo; if (n <= 0) return null;
            var b = new byte[n]; Array.Copy(_ms.GetBuffer(), _sentUpTo, b, 0, n); _sentUpTo = (int)_ms.Length; return b;
        }

        public void Clear() { _ms.SetLength(0); _sentUpTo = 0; _depth = 0; }

        // ---------------------------------------------------------------- recording
        internal bool Begin(Op op)
        {
            if (!Recording || _depth > 0) { _depth++; return false; }
            _depth++; _w.Write((byte)op); return true;
        }
        internal void End() { if (_depth > 0) _depth--; _w.Flush(); }
        internal BinaryWriter W => _w;

        internal static void WriteVec(BinaryWriter w, Vec3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        static Vec3 ReadVec(BinaryReader r) => new Vec3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        internal static void WriteRoster(BinaryWriter w, CompetitorRoster roster)
        {
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                var s = roster[i];
                w.Write((byte)s.Kind); w.Write(s.PeerId); w.Write(s.DisplayName ?? ""); w.Write((byte)s.Difficulty); w.Write(s.CosmeticId);
                w.Write(s.Ready); w.Write(s.BotTakeover);
            }
        }

        static void ReadRoster(BinaryReader r, CompetitorRoster roster)
        {
            for (int i = 0; i < CompetitorRoster.SlotCount; i++)
            {
                var s = roster[i];
                s.Kind = (SlotKind)r.ReadByte(); s.PeerId = r.ReadInt32(); s.DisplayName = r.ReadString(); s.Difficulty = (BotDifficulty)r.ReadByte();
                s.CosmeticId = r.ReadInt32(); s.Ready = r.ReadBoolean(); s.BotTakeover = r.ReadBoolean();
                s.Status = SlotStatus.Active; s.Reason = EliminationReason.None; s.EliminatedInRound = -1; s.DisconnectedAt = -1f;
            }
        }

        // ---------------------------------------------------------------- replay
        /// <summary>
        /// Applies a journal chunk to a replica match. Returns the number of ops applied. Chunks may be split anywhere:
        /// incomplete trailing bytes are kept in <paramref name="carry"/> and prepended next time.
        /// </summary>
        public static int Apply(MatchController m, byte[] chunk, ref byte[] carry, Action<uint> onChecksum = null)
        {
            byte[] data = chunk;
            if (carry != null && carry.Length > 0) { data = new byte[carry.Length + chunk.Length]; Array.Copy(carry, data, carry.Length); Array.Copy(chunk, 0, data, carry.Length, chunk.Length); }
            carry = null;
            int ops = 0; long good = 0;
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8))
            {
                while (ms.Position < ms.Length)
                {
                    long start = ms.Position;
                    try { ApplyOne(m, r, onChecksum); ops++; good = ms.Position; }
                    catch (EndOfStreamException) { ms.Position = start; break; }
                }
                if (good < data.Length) { carry = new byte[data.Length - good]; Array.Copy(data, good, carry, 0, carry.Length); }
            }
            return ops;
        }

        static void ApplyOne(MatchController m, BinaryReader r, Action<uint> onChecksum)
        {
            var op = (Op)r.ReadByte();
            var rd = m.Round;
            switch (op)
            {
                case Op.MStart:
                {
                    var mode = (MatchMode)r.ReadByte(); ulong seed = r.ReadUInt64();
                    m.IntroSeconds = r.ReadSingle(); m.OutroSeconds = r.ReadSingle(); m.WinnerSeconds = r.ReadSingle(); m.DisconnectGrace = r.ReadSingle();
                    m.WaitForRoomReady = r.ReadBoolean(); m.TakeoverDifficulty = (BotDifficulty)r.ReadByte();
                    int nr = r.ReadByte(); var rounds = new RoundConfig[nr];
                    for (int i = 0; i < nr; i++)
                        rounds[i] = new RoundConfig { Index = r.ReadByte(), Room = (RoomId)r.ReadByte(), Duration = r.ReadSingle(), SharedTasks = r.ReadByte(), NoiseMultiplier = r.ReadSingle(), MeterBaseline = r.ReadSingle(), PetActivity = r.ReadSingle(), CountdownSeconds = r.ReadSingle(), RecipeMode = r.ReadBoolean(),
                            Continuous = r.ReadBoolean(), JobsPerSeat = r.ReadByte(), TimeoutSeconds = r.ReadSingle(), PenaltyPoints = r.ReadSingle() };
                    m.Rounds = rounds;
                    m.Roster.UnlockForReplay();
                    ReadRoster(r, m.Roster);
                    m.Start(mode, seed, out _);
                    break;
                }
                case Op.MTick: m.Tick(r.ReadSingle()); break;
                case Op.MSkipIntro: m.SkipIntro(); break;
                case Op.MRoomReady: m.NotifyRoomReady(); break;
                case Op.MDisconnected: m.HumanDisconnected(r.ReadInt32(), r.ReadSingle()); break;
                case Op.MReconnected: m.HumanReconnected(r.ReadInt32(), r.ReadInt32(), r.ReadString(), r.ReadSingle()); break;
                case Op.MHostLeft: m.HostLeft(r.ReadString()); break;
                case Op.MRematch: m.PrepareRematch(r.ReadUInt64()); break;
                case Op.MChecksum: { uint c = r.ReadUInt32(); onChecksum?.Invoke(c); break; }
                case Op.RGrab: { int a = r.ReadInt32(), p = r.ReadInt32(); rd?.Grab(a, p); break; }
                case Op.RRelease: { int a = r.ReadInt32(), p = r.ReadInt32(); float s = r.ReadSingle(); bool t = r.ReadBoolean(); rd?.Release(a, p, s, t); break; }
                case Op.RPush: { int a = r.ReadInt32(), p = r.ReadInt32(); float s = r.ReadSingle(); rd?.Push(a, p, s); break; }
                case Op.RPlaced: { int p = r.ReadInt32(), an = r.ReadInt32(), a = r.ReadInt32(); rd?.Placed(p, an, a); break; }
                case Op.RRemoved: { int p = r.ReadInt32(), a = r.ReadInt32(); rd?.Removed(p, a); break; }
                case Op.RImpact: { int p = r.ReadInt32(); float s = r.ReadSingle(); var v = ReadVec(r); int par = r.ReadInt32(); var c = (NoiseCause)r.ReadByte(); int da = r.ReadInt32(); rd?.SubmitImpact(p, s, v, par, c, da); break; }
                case Op.ROperate: { int a = r.ReadInt32(), o = r.ReadInt32(); float raw = r.ReadSingle(); var v = ReadVec(r); bool sl = r.ReadBoolean(); rd?.SubmitOperate(a, o, raw, v, sl); break; }
                case Op.RPetEat: { int pa = r.ReadInt32(); float g = r.ReadSingle(); int fd = r.ReadInt32(); rd?.PetEat(pa, g, fd); break; }
                case Op.RTrash: { int ta = r.ReadInt32(), tp = r.ReadInt32(); rd?.TrashBinned(ta, tp); break; }
                case Op.RPetSound: { int a = r.ReadInt32(); float val = r.ReadSingle(); var v = ReadVec(r); rd?.SubmitPetSound(a, val, v); break; }
                case Op.RTrap: { int t = r.ReadInt32(), a = r.ReadInt32(); bool car = r.ReadBoolean(); int p = r.ReadInt32(); float s = r.ReadSingle(); var v = ReadVec(r); rd?.SubmitTrapTrigger(t, a, car, p, s, v); break; }
                case Op.RArmTrap: { int o = r.ReadInt32(), p = r.ReadInt32(); bool w = r.ReadBoolean(); rd?.ArmTrap(o, p, w); break; }
                case Op.RDisarm: { int t = r.ReadInt32(), a = r.ReadInt32(); rd?.DisarmTrap(t, a); break; }
                case Op.RCatch: { int a = r.ReadInt32(), p = r.ReadInt32(), k = r.ReadInt32(); rd?.ReportCatchRescue(a, p, k); break; }
                case Op.RActivity: rd?.ReportActivity(r.ReadInt32()); break;
                case Op.RRecipe:
                {
                    var rop = new ROp { EventId = r.ReadInt64(), Actor = r.ReadInt32(), Verb = (Verb)r.ReadByte(), Prop = r.ReadInt32(), Prop2 = r.ReadInt32(), Fixture = r.ReadInt32(), Slot = r.ReadInt32() };
                    var arg = r.ReadString(); rop.Arg = arg.Length == 0 ? null : arg; rd?.RecipeAction(rop); break;
                }
                case Op.RWitness: { int i = r.ReadInt32(), wi = r.ReadInt32(); float c = r.ReadSingle(); rd?.ReportWitness(i, wi, c); break; }
                case Op.RCard: { int seat = r.ReadInt32(), mid = r.ReadInt32(); rd?.PresentCard(seat, mid); break; }
                case Op.RAccuse: { int seat = r.ReadInt32(), sus = r.ReadInt32(); rd?.Accuse(seat, sus); break; }
                case Op.RPass: rd?.PassInterrogation(r.ReadInt32()); break;
                case Op.RTracking: { int s = r.ReadInt32(); bool l = r.ReadBoolean(); rd?.ReportTrackingLost(s, l); break; }
                case Op.RTieStep: rd?.ReportTieBreakStep(r.ReadInt32()); break;
                case Op.RForceResolve: rd?.ForceResolve((RoundEndCause)r.ReadByte()); break;
                default: throw new InvalidDataException("bad journal op " + (byte)op);
            }
        }

        /// <summary>Cheap state fingerprint (host writes it every ~1 s; clients compare = desync detector).</summary>
        public static uint Checksum(MatchController m)
        {
            unchecked
            {
                uint h = 2166136261;
                void Mix(int v) { h = (h ^ (uint)v) * 16777619; }
                Mix((int)m.Phase); Mix(m.RoundIndex);
                var r = m.Round;
                if (r != null)
                {
                    Mix((int)r.Phase); Mix((int)(r.Meter.Value * 10)); Mix((int)r.Mom.State);
                    for (int i = 0; i < CompetitorRoster.SlotCount; i++) { Mix((int)(r.Scores[i].Total * 10)); Mix((int)m.Roster[i].Status); }
                    foreach (var c in r.Board.Chores) foreach (var s in c.Steps) Mix(s.Done ? 1 : 0);
                    if (r.Recipes != null) { foreach (var ch in r.Recipes.Snapshot()) Mix(ch); Mix(r.Recipes.Sabotage.Count); }
                }
                return h;
            }
        }

        public void WriteChecksum(MatchController m)
        {
            if (!Recording) return;
            _w.Write((byte)Op.MChecksum); _w.Write(Checksum(m)); _w.Flush();
        }
    }

    /// <summary>using-guard returned by <see cref="MatchJournal"/> recording helpers (null-safe).</summary>
    public readonly struct JournalScope : IDisposable
    {
        readonly MatchJournal _j;
        public JournalScope(MatchJournal j) { _j = j; }
        public void Dispose() { _j?.End(); }
    }
}
