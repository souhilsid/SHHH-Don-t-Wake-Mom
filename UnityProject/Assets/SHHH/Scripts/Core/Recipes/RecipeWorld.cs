using System;
using System.Collections.Generic;
using System.Text;

namespace SHHH.Core
{
    /// <summary>Authoritative physical state of one recipe prop (engine-free mirror of the Unity PropBody state).</summary>
    public sealed class RProp
    {
        public int Id; public string Kind; public string Tag; public int Order; public NoiseMaterial Material;
        public int Fixture = -1, Slot = -1;     // where it sits (fixture id + slot) or -1 = loose
        public string Spot = "loose";           // named loose location (evidence)
        public bool Oriented; public int Stage; public int Partner = -1; public int AttachedTo = -1;
        public int StrokeCap;                   // washable: scrub reaches this stage, rinse (Fill at an open tap fixture) goes beyond
        /// <summary>V10 stain grid of a washable item: 8x8 cells over its eating face (bit = row*8+col, row along local z, col along local x).
        /// Cleared only by <see cref="Verb.Scrub"/> with a soapy sponge in hand; the item reaches its scrubbed stage when it is 0.</summary>
        public ulong Dirt;
        /// <summary>V12 liquid in this vessel (millilitres), reported by the authority's conserved liquid simulation (fill under a tap, pour by
        /// tilting, spills). Replicated (11th snapshot field) so every player sees the same water level.</summary>
        public int Ml;
        public int HeldBy = ActorIds.None;
        public int LastHolder = ActorIds.None;   // a released prop can still be snapped into a slot by its last holder
        public int RecipeInstance;              // owning recipe instance
        public bool IsPlaced => Fixture >= 0;
    }

    public sealed class RFixture
    {
        public int Id; public string Kind; public bool HasDoor; public bool Open; public int Decals;
        /// <summary>V10: continuous opening 0..100 % (a tap's valve = flow rate; a door/drawer's travel). Open == Level &gt; 0 for valves.
        /// Set by the Open op's Arg ("0".."100", default 100); Close sets 0. Replicated in the snapshot (5th fixture field).</summary>
        public int Level;
        public string[] SlotTags; public int[] Occupant; public HashSet<string> Flags = new HashSet<string>(); public string[] FlagNames;
        public int RecipeInstance;
    }

    /// <summary>An operation request (from a hand, controller, bot motor or test). Untrusted until <see cref="RecipeWorld.Apply"/> validates it.</summary>
    public struct ROp
    {
        public long EventId;                    // idempotency key (client-chosen, unique per actor)
        public int Actor; public Verb Verb; public int Prop; public int Prop2; public int Fixture; public int Slot; public string Arg;
        public override string ToString() => $"{Verb} a{Actor} p{Prop}{(Prop2 >= 0 ? "/p" + Prop2 : "")} f{Fixture}#{Slot} {Arg}";
    }

    /// <summary>Immutable accepted event (spec §03.1 subset: id, sequence, time, actor, prop, task, action, before/after, validation).</summary>
    public sealed class REvent
    {
        public long EventId; public int Seq; public float Time; public int Actor; public Verb Verb;
        public int Prop = -1, Fixture = -1, RecipeInstance = -1; public string Before, After, Validation; public NoiseMaterial Material;
        public override string ToString() => $"#{Seq} t{Time:0.0} a{Actor} {Verb} p{Prop} f{Fixture} r{RecipeInstance} [{Before} -> {After}] {Validation}";
    }

    /// <summary>
    /// Headless authoritative world for recipe props and fixtures. All gameplay mutations go through <see cref="Apply"/>:
    /// legality checks (holding, open doors, free slots, eliminated seats), idempotency by event id, an append-only ledger,
    /// and a compact snapshot for join/rejoin. The Unity layer mirrors physical results into this world; tests drive it directly.
    /// </summary>
    public sealed class RecipeWorld
    {
        public readonly List<RProp> Props = new List<RProp>();
        public readonly List<RFixture> Fixtures = new List<RFixture>();
        public readonly List<REvent> Ledger = new List<REvent>();
        readonly HashSet<long> _seen = new HashSet<long>();
        public float Time;
        public Func<int, bool> CanAct = a => true;   // round wires this to "active sibling or pet" (spectators rejected)
        public event Action<REvent> Accepted;
        long _autoId = 1L << 40;

        public RProp Prop(int id) => id >= 0 && id < Props.Count ? Props[id] : null;

        /// <summary>Deterministic food stains on an 8x8 grid (inside the plate's round face): 2-3 blobs, ~15-30 cells. Same on every peer.</summary>
        public static ulong StainPattern(int seed)
        {
            uint s = (uint)(seed * 2654435761u + 0x9E3779B9u);
            float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
            ulong m = 0; int blobs = 2 + (int)(Next() * 2f);
            for (int b = 0; b < blobs; b++)
            {
                float cx = 1.8f + Next() * 4.4f, cz = 1.8f + Next() * 4.4f, rx = 1.0f + Next() * 1.3f, rz = 0.8f + Next() * 1.1f;
                for (int r = 0; r < 8; r++) for (int c = 0; c < 8; c++)
                {
                    float x = c + 0.5f, z = r + 0.5f;
                    if ((x - 4f) * (x - 4f) + (z - 4f) * (z - 4f) > 3.6f * 3.6f) continue;          // only on the round face
                    float dx = (x - cx) / rx, dz = (z - cz) / rz; if (dx * dx + dz * dz <= 1f) m |= 1UL << (r * 8 + c);
                }
            }
            if (m == 0) m = 1UL << (3 * 8 + 3);
            return m;
        }
        public static int CountBits(ulong v) { int n = 0; while (v != 0) { v &= v - 1; n++; } return n; }
        public RFixture Fixture(int id) => id >= 0 && id < Fixtures.Count ? Fixtures[id] : null;

        public int AddProp(RecipePropDecl d, int index, int recipeInstance)
        {
            var p = new RProp { Id = Props.Count, Kind = d.Kind, Tag = d.Tag, Material = d.Material, Spot = d.StartSpot, Stage = d.StartStage,
                Order = d.Ordered ? index : 0, Oriented = d.StartsOriented, RecipeInstance = recipeInstance, StrokeCap = d.StrokeCap };
            if (p.StrokeCap > 0 && p.Stage < p.StrokeCap) p.Dirt = StainPattern(p.Id);
            Props.Add(p); return p.Id;
        }

        public int AddFixture(RecipeFixtureDecl d, int recipeInstance)
        {
            var f = new RFixture { Id = Fixtures.Count, Kind = d.Kind, HasDoor = d.HasDoor, Open = !d.HasDoor || d.StartsOpen, Level = (!d.HasDoor || d.StartsOpen) ? 100 : 0, Decals = d.StartDecals,
                SlotTags = (string[])d.SlotTags.Clone(), Occupant = new int[d.SlotTags.Length], FlagNames = d.Flags, RecipeInstance = recipeInstance };
            for (int i = 0; i < f.Occupant.Length; i++) f.Occupant[i] = -1;
            Fixtures.Add(f); return f.Id;
        }

        public long NextEventId() => ++_autoId;
        public void ForgetEvent(long id) => _seen.Remove(id);   // only for an authoritative trial roll-back
        public int CountHeld(int actor) { int n = 0; foreach (var q in Props) if (q.HeldBy == actor) n++; return n; }

        /// <summary>Validate and apply. Returns null when accepted, otherwise the rejection reason (nothing changes).</summary>
        public string Apply(ROp op)
        {
            if (op.EventId == 0) op.EventId = NextEventId();
            if (_seen.Contains(op.EventId)) return "duplicate event (idempotent)";
            if (!CanAct(op.Actor)) return "actor may not act (spectator / unknown)";
            var p = Prop(op.Prop); var f = Fixture(op.Fixture);
            string before = Describe(p, f), err = null;
            switch (op.Verb)
            {
                case Verb.PickUp:
                    if (p == null) { err = "no prop"; break; }
                    if (p.HeldBy != ActorIds.None) { err = "already held"; break; }
                    if (p.IsPlaced) { var pf = Fixture(p.Fixture); if (pf.HasDoor && !pf.Open) { err = "inside a closed " + pf.Kind; break; } pf.Occupant[p.Slot] = -1; p.Fixture = -1; p.Slot = -1; }
                    if (CountHeld(op.Actor) >= 2) { err = "both hands full"; break; }
                    p.HeldBy = op.Actor; p.LastHolder = op.Actor; p.Spot = "held"; break;
                case Verb.PutDown:
                case Verb.Release:
                    if (p == null || p.HeldBy != op.Actor) { err = "not holding it"; break; }
                    p.HeldBy = ActorIds.None; p.Spot = string.IsNullOrEmpty(op.Arg) ? "floor" : op.Arg; break;
                case Verb.PlaceInSocket:
                    if (p == null || !(p.HeldBy == op.Actor || (p.HeldBy == ActorIds.None && !p.IsPlaced && p.LastHolder == op.Actor))) { err = "not holding it"; break; }
                    if (f == null || op.Slot < 0 || op.Slot >= f.Occupant.Length) { err = "no such slot"; break; }
                    if (f.HasDoor && !f.Open) { err = f.Kind + " is closed"; break; }
                    if (f.Occupant[op.Slot] >= 0) { err = "slot occupied"; break; }
                    f.Occupant[op.Slot] = p.Id; p.Fixture = f.Id; p.Slot = op.Slot; p.HeldBy = ActorIds.None; p.Spot = f.Kind; break;
                case Verb.Align:
                    if (p == null || (p.HeldBy != op.Actor && !p.IsPlaced)) { err = "must hold or reach the placed prop"; break; }
                    p.Oriented = op.Arg != "tilt"; break;
                case Verb.Open:
                case Verb.Close:
                    if (f == null || !f.HasDoor) { err = "nothing to open"; break; }
                    // V12: an appliance door is locked while the machine runs (washer "start" flag) - stop it first
                    if (op.Verb == Verb.Open && f.Flags.Contains("start")) { err = "the door is locked while it runs"; break; }
                    f.Open = op.Verb == Verb.Open;
                    f.Level = f.Open ? (int.TryParse(op.Arg, out var lvl) ? Math.Max(1, Math.Min(100, lvl)) : 100) : 0; break;
                case Verb.Scrub:
                {
                    // V10: stains come off only where a SOAPY SPONGE held by the actor scrubbed (the Unity layer reports which cells)
                    if (p == null || p.StrokeCap <= 0) { err = "not washable"; break; }
                    if (p.HeldBy != op.Actor && !p.IsPlaced) { err = "must hold it"; break; }
                    var tool = Prop(op.Prop2);
                    if (tool == null || tool.Kind != "sponge" || tool.HeldBy != op.Actor) { err = "scrub with a sponge in your hand"; break; }
                    if (tool.Stage < 1) { err = "the sponge needs soap"; break; }
                    if (p.Stage >= p.StrokeCap || p.Dirt == 0) { err = "already scrubbed - rinse it"; break; }
                    if (!ulong.TryParse(op.Arg ?? "", System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var cells) || (cells & p.Dirt) == 0) { err = "no stain there"; break; }
                    p.Dirt &= ~cells;
                    if (p.Dirt == 0) p.Stage = p.StrokeCap;   // last stain gone: soapy/sudsy (stage 1)
                    break;
                }
                case Verb.Stroke:
                    if (f != null) { if (f.Decals <= 0) { err = "already clean"; break; } f.Decals--; }
                    else if (p != null)
                    {
                        if (p.HeldBy != op.Actor && !p.IsPlaced) { err = "must hold it"; break; }
                        if (p.StrokeCap > 0 && p.Dirt != 0) { err = "food left on it - scrub the stains with a soapy sponge"; break; }
                        if (p.StrokeCap > 0 && p.Stage >= p.StrokeCap) { err = "already scrubbed - rinse it"; break; }
                        p.Stage++;
                    }
                    else err = "nothing to wipe"; break;
                case Verb.Smudge:
                    if (f != null) f.Decals++; else if (p != null && p.Stage > 0) { p.Stage = 0; if (p.StrokeCap > 0) p.Dirt = StainPattern(p.Id + 7919 * Ledger.Count); } else err = "nothing to mess"; break;
                case Verb.Fold:
                case Verb.Fill:
                    if (p == null || (p.HeldBy != op.Actor && !p.IsPlaced)) { err = "must hold it"; break; }
                    if (op.Verb == Verb.Fill && f != null)
                    {
                        // filling/rinsing AT a fixture (a tap): the water has to be running, and a washable item must be scrubbed first
                        if (f.HasDoor && !f.Open) { err = "the " + f.Kind.Replace('_', ' ') + " is off"; break; }
                        if (p.StrokeCap > 0 && p.Stage < p.StrokeCap) { err = "scrub it first"; break; }
                    }
                    p.Stage++; break;
                case Verb.Unfold:
                    if (p == null || p.Stage == 0) { err = "nothing to undo"; break; }
                    p.Stage = 0; break;
                case Verb.Press:
                    if (f == null || string.IsNullOrEmpty(op.Arg)) { err = "no control"; break; }
                    if (op.Arg[0] == '!') { if (!f.Flags.Remove(op.Arg.Substring(1))) err = "already off"; } // "!flag" = undo the control
                    else if (op.Arg == "start" && f.HasDoor && f.Open) err = "close the door first";   // V12: a real machine will not start with its door open
                    else if (!f.Flags.Add(op.Arg)) err = "already on";
                    break;
                case Verb.Pair:
                case Verb.Attach:
                {
                    var q = Prop(op.Prop2);
                    if (p == null || q == null || p == q) { err = "need two props"; break; }
                    if (p.HeldBy != op.Actor && q.HeldBy != op.Actor) { err = "hold one of them"; break; }
                    if (op.Verb == Verb.Pair) { if (p.Partner >= 0 || q.Partner >= 0) { err = "already paired"; break; } p.Partner = q.Id; q.Partner = p.Id; }
                    else { if (q.AttachedTo >= 0 && q.AttachedTo != p.Id) { err = "partner busy"; break; } p.AttachedTo = q.Id; }
                    break;
                }
                case Verb.Unpair:
                    if (p == null || p.Partner < 0) { err = "not paired"; break; }
                    Prop(p.Partner).Partner = -1; p.Partner = -1; break;
                case Verb.Detach:
                    if (p == null || p.AttachedTo < 0) { err = "not attached"; break; }
                    p.AttachedTo = -1; break;
                case Verb.Pour:
                    if (p == null) { err = "no vessel"; break; }
                    if (!int.TryParse(op.Arg, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ml) || ml < 0) { err = "bad amount"; break; }
                    if (ml == p.Ml) { err = "no change"; break; }
                    p.Ml = Math.Min(ml, 100000); break;
                default: err = "unsupported"; break;
            }
            if (err != null) return err;
            _seen.Add(op.EventId);
            var e = new REvent { EventId = op.EventId, Seq = Ledger.Count + 1, Time = Time, Actor = op.Actor, Verb = op.Verb, Prop = op.Prop, Fixture = op.Fixture,
                RecipeInstance = p != null ? p.RecipeInstance : f != null ? f.RecipeInstance : -1, Before = before, After = Describe(p, f), Validation = "accepted",
                Material = p != null ? p.Material : NoiseMaterial.Wood };
            Ledger.Add(e);
            Accepted?.Invoke(e);
            return null;
        }

        static string Describe(RProp p, RFixture f)
        {
            var sb = new StringBuilder();
            if (p != null) sb.Append(p.Kind).Append(p.IsPlaced ? "@f" + p.Fixture + "#" + p.Slot : "@" + p.Spot).Append(p.Oriented ? "+o" : "").Append(p.Stage > 0 ? "+s" + p.Stage : "").Append(p.Dirt != 0 ? "+dirt" + CountBits(p.Dirt) : "").Append(p.Ml > 0 ? "+" + p.Ml + "ml" : "").Append(p.Partner >= 0 ? "+pair" + p.Partner : "").Append(p.AttachedTo >= 0 ? "+on" + p.AttachedTo : "");
            if (f != null) sb.Append(sb.Length > 0 ? " " : "").Append(f.Kind).Append(f.HasDoor ? (f.Open ? ":open" : ":closed") : "").Append(f.Decals > 0 ? " decals" + f.Decals : "").Append(f.Flags.Count > 0 ? " [" + string.Join(",", f.Flags) + "]" : "");
            return sb.ToString();
        }

        /// <summary>Compact versioned snapshot (join/rejoin, desync checks). Order-stable, no object graphs.</summary>
        public string Snapshot()
        {
            var sb = new StringBuilder("RW1;").Append(Props.Count).Append(';').Append(Fixtures.Count).Append(';');
            foreach (var p in Props) sb.Append(p.Fixture).Append(',').Append(p.Slot).Append(',').Append(p.Spot).Append(',').Append(p.Oriented ? 1 : 0).Append(',').Append(p.Stage).Append(',').Append(p.Partner).Append(',').Append(p.AttachedTo).Append(',').Append(p.HeldBy).Append(',').Append(p.LastHolder).Append(',').Append(p.Dirt.ToString("X", System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(p.Ml).Append('|');
            sb.Append(';');
            foreach (var f in Fixtures) sb.Append(f.Open ? 1 : 0).Append(',').Append(f.Decals).Append(',').Append(string.Join("/", f.Occupant)).Append(',').Append(string.Join("/", SortedFlags(f))).Append(',').Append(f.Level).Append('|');
            return sb.ToString();
        }

        static List<string> SortedFlags(RFixture f) { var l = new List<string>(f.Flags); l.Sort(StringComparer.Ordinal); return l; }

        /// <summary>Restore physical state from <see cref="Snapshot"/> onto a world built from the same recipe declarations.</summary>
        public void Restore(string snap)
        {
            var parts = snap.Split(';');
            if (parts[0] != "RW1" || int.Parse(parts[1]) != Props.Count || int.Parse(parts[2]) != Fixtures.Count) throw new InvalidOperationException("snapshot does not match this world");
            var ps = parts[3].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Props.Count; i++)
            {
                var v = ps[i].Split(','); var p = Props[i];
                p.Fixture = int.Parse(v[0]); p.Slot = int.Parse(v[1]); p.Spot = v[2]; p.Oriented = v[3] == "1"; p.Stage = int.Parse(v[4]); p.Partner = int.Parse(v[5]); p.AttachedTo = int.Parse(v[6]); p.HeldBy = int.Parse(v[7]); p.LastHolder = int.Parse(v[8]);
                p.Dirt = v.Length > 9 ? ulong.Parse(v[9], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture) : 0UL;
                p.Ml = v.Length > 10 ? int.Parse(v[10], System.Globalization.CultureInfo.InvariantCulture) : 0;
            }
            var fs = parts[4].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Fixtures.Count; i++)
            {
                var v = fs[i].Split(','); var f = Fixtures[i];
                f.Open = v[0] == "1"; f.Decals = int.Parse(v[1]);
                var occ = v[2].Split('/'); for (int k = 0; k < f.Occupant.Length; k++) f.Occupant[k] = int.Parse(occ[k]);
                f.Flags.Clear(); if (v[3].Length > 0) foreach (var fl in v[3].Split('/')) f.Flags.Add(fl);
                f.Level = v.Length > 4 ? int.Parse(v[4]) : (f.Open ? 100 : 0);
            }
        }
    }
}
