using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>A reachable work surface in a built room where one seat's recipe is staged (V7: the existing task tables).</summary>
    public struct RecipeStation
    {
        public Vec3 Center; public float Length, Depth; public bool AlongZ; public float Front; // Front = +1/-1 along the depth axis
        /// <summary>V9: the room's real kitchen sink (basin floor centre + yaw that turns local +Z toward the person at the sink), reachable
        /// from this station. Fixtures that ARE the sink ("sink_tap") anchor there instead of taking table space - you carry the dishes over.</summary>
        public Vec3 Sink; public float SinkYaw; public bool HasSink;
        public RecipeStation(float x, float top, float z, float length, float depth, bool alongZ, float front)
        { Center = new Vec3(x, top, z); Length = length; Depth = depth; AlongZ = alongZ; Front = front; Sink = default; SinkYaw = 0; HasSink = false; Anchors = null; }
        public RecipeStation WithSink(Vec3 basinFloor, float yaw) { var c = this; c.Sink = basinFloor; c.SinkYaw = yaw; c.HasSink = true; return c; }
        /// <summary>V10 world task anchors: where a fixture kind (dish rack, soap pump) or a loose prop kind (sponge) REALLY lives in this
        /// room - on the counter by the sink, in the caddy - instead of being laid out on the station table. No anchor = table layout.</summary>
        public RecipeAnchor[] Anchors;
        public RecipeStation WithAnchor(string kind, Vec3 pos, float yaw, bool prop = false)
        {
            var c = this; var l = new List<RecipeAnchor>(Anchors ?? new RecipeAnchor[0]); l.Add(new RecipeAnchor { Kind = kind, Pos = pos, Yaw = yaw, Prop = prop }); c.Anchors = l.ToArray(); return c;
        }
        public bool TryAnchor(string kind, bool prop, out RecipeAnchor a)
        {
            if (Anchors != null) foreach (var x in Anchors) if (x.Kind == kind && x.Prop == prop) { a = x; return true; }
            a = default; return false;
        }
        public Vec3 Local(float u, float v, float up = 0f) => AlongZ ? new Vec3(Center.x + v, Center.y + up, Center.z + u) : new Vec3(Center.x + u, Center.y + up, Center.z + v);
    }

    /// <summary>V10: an authored in-house location for a fixture kind or a loose prop kind (room coordinates, resting surface height).</summary>
    public struct RecipeAnchor { public string Kind; public Vec3 Pos; public float Yaw; public bool Prop; }

    /// <summary>Where a recipe fixture and its slots are, in room coordinates (shared by Unity visuals, bots and tests).</summary>
    public sealed class RecipeFixturePlacement
    {
        public int Instance, FixtureId; public string Kind; public bool HasDoor; public Vec3 Position; public float Yaw; public float Width;
        public Vec3[] Slots; public int Station; public float Pitch; public float Depth; public bool Stack;
        public bool Anchored;   // part of the house (the kitchen sink): Position = basin floor centre
    }

    /// <summary>
    /// Lays recipes out on room stations: fixtures in the back row, props in the front row (toward the player), all on a
    /// table top >= 0.4 m (reach rule). Deterministic; used identically by host, replicas, bots and tests.
    /// </summary>
    /// <summary>Approximate largest horizontal extent (m) of each recipe prop kind, used for table layout and fit checks.</summary>
    public static class RecipePropSize
    {
        static readonly Dictionary<string, float> W = new Dictionary<string, float>
        {
            ["book"] = .22f, ["magazine"] = .28f, ["remote"] = .17f, ["toy_block"] = .07f, ["toy_car"] = .17f, ["shoe"] = .28f, ["cushion"] = .42f, ["throw"] = .35f,
            ["mug"] = .11f, ["coaster"] = .09f, ["game_pieces"] = .08f, ["game_lid"] = .25f, ["game_base"] = .24f, ["photo_frame"] = .25f, ["popcorn_cluster"] = .07f,
            ["gamepad"] = .14f, ["gamepad_strap"] = .12f, ["squeaky_toy"] = .2f, ["plate"] = .22f, ["cup"] = .1f, ["pot"] = .28f, ["pot_lid"] = .2f, ["pan"] = .42f,
            ["fruit_bowl"] = .2f, ["dining_chair"] = .45f, ["cutlery"] = .18f, ["jar"] = .08f, ["apple"] = .08f, ["bread_bottom"] = .11f, ["cheese"] = .1f, ["lettuce"] = .1f,
            ["bread_top"] = .11f, ["recipe_card"] = .14f, ["cereal_jar"] = .1f, ["jar_lid"] = .1f, ["cloth"] = .15f, ["kitchen_towel"] = .26f, ["placemat"] = .28f,
            ["kid_plate"] = .2f, ["kid_cup"] = .08f, ["mixer_whisk"] = .15f, ["toaster_cord"] = .12f, ["clothes"] = .25f, ["sock"] = .16f, ["bath_towel"] = .28f,
            ["laundry_bundle"] = .25f, ["wet_bundle"] = .25f, ["shirt"] = .46f, ["hanger"] = .3f, ["storage_bin"] = .25f, ["bin_lid"] = .25f, ["folded_towel"] = .2f,
            ["lint_cluster"] = .08f, ["empty_bottle"] = .08f, ["dirty_clothes"] = .25f, ["sponge"] = .1f, ["soap_pump"] = .08f,
            ["grocery"] = .24f,   // V13 KT17: the widest grocery (the pizza); burger .16, juice .09, apple .085
        };
        public static float Of(string kind) => W.TryGetValue(kind, out var w) ? w : 0.12f;
        /// <summary>Flat items that stack on one spot (thickness in m); fixtures that only take flat items are stacks.</summary>
        static readonly Dictionary<string, float> Flat = new Dictionary<string, float>
        {
            ["magazine"] = .012f, ["plate"] = .025f, ["kid_plate"] = .025f, ["placemat"] = .006f, ["folded_towel"] = .05f, ["coaster"] = .01f,
            ["envelope"] = .006f, ["drawing"] = .004f, ["recipe_card"] = .004f, ["bath_towel"] = .05f, ["game_base"] = .05f, ["game_lid"] = .035f,
            ["dirty_clothes"] = .08f, // soft piles: they heap in a hamper rather than standing side by side
        };
        /// <summary>Narrow side of items that stand side by side in a slot row (books spine-out, shoes, cars, cutlery...).</summary>
        static readonly Dictionary<string, float> Narrow = new Dictionary<string, float>
        {
            ["book"] = .05f, ["shoe"] = .11f, ["toy_car"] = .08f, ["cutlery"] = .04f, ["remote"] = .06f, ["sock"] = .08f, ["gamepad"] = .1f,
["photo_frame"] = .04f, ["clothes"] = .2f, ["hanger"] = .04f, ["slipper"] = .1f,
            ["family_shoe"] = .11f, ["toothbrush"] = .03f, ["marker"] = .03f, ["umbrella"] = .08f, ["envelope"] = .02f, ["drawing"] = .02f,
            ["notebook"] = .03f, ["paper_roll"] = .11f, ["mixer_whisk"] = .07f,
        };
        public static float NarrowOf(string kind) => Narrow.TryGetValue(kind, out var n) ? n : Of(kind);
        public static bool IsFlat(string kind) => Flat.ContainsKey(kind);
        public static float Thickness(string kind) => Flat.TryGetValue(kind, out var t) ? t : 0.05f;
        /// <summary>Items wider than this need a furniture-integrated fixture (not a table station).</summary>
        public const float StationMax = 0.3f;
    }

    public static class RecipeLayout
    {
        public const float SlotSpacing = 0.11f, PropSpacing = 0.11f, FixtureGap = 0.05f;

        public static RecipeStation[] Stations(RoomId room)
        {
            switch (room)
            {
                case RoomId.LivingRoom: return new[] {
                    new RecipeStation(-1.65f, 0.74f, -1.25f, 1.1f, 0.55f, false, -1f), new RecipeStation(1.65f, 0.74f, -1.25f, 1.1f, 0.55f, false, -1f),
                    new RecipeStation(2.65f, 0.74f, -2.6f, 1.0f, 0.5f, false, 1f), new RecipeStation(0f, 0.42f, 0.9f, 1.1f, 0.6f, false, 1f) };
                // V7: measured in the connected house (recipe_diag surfaces/stations, 2026-11-09): flat, free 0.3 m above
                case RoomId.Kitchen:
                {
                    // dining table x[-0.14,1.34] z[-0.96,0.56]; stools stand at its four corners, so players work from the free west/east ends
                    var sink = new Vec3(0.80f, 0.565f, 2.62f); const float sinkYaw = 180f;   // V10: basin floor after the sink run is fitted to a 0.80 m worktop (was 0.36, probe 2026-11-10; re-probed below)
                    // V10: the sink run has its own counters (Rooms.Kitchen counter_side, same worktop as the sink module): the dish rack stands
                    // on the right-hand counter, the soap pump and the sponge spot on the left. Washing happens AT the sink; only the dirty
                    // plates start on the dining table (the station).
                    // V13 KT17: the house fridge (replaced by the working sk_fridge where the baked one stood); anchor = its middle shelf
                    RecipeStation Fr(RecipeStation st) => st.WithAnchor("fridge", FridgeShelf, 180f);
                    RecipeStation Dish(RecipeStation st) => Fr(st).WithSink(sink, sinkYaw)
                        .WithAnchor("plate_rack", new Vec3(1.78f, WorktopY, 2.47f), sinkYaw)
                        .WithAnchor("soap_pump", new Vec3(-0.02f, WorktopY, 2.52f), sinkYaw)
                        .WithAnchor("sponge", new Vec3(0.12f, WorktopY + 0.03f, 2.36f), sinkYaw, prop: true);
                    return new[] {
                    Dish(new RecipeStation(0.14f, 0.75f, -0.15f, 0.7f, 0.56f, true, -1f)), Dish(new RecipeStation(1.06f, 0.75f, -0.15f, 0.7f, 0.56f, true, 1f)),
                    Fr(new RecipeStation(-2.68f, 0.767f, -0.6f, 0.7f, 0.5f, true, 1f)), Fr(new RecipeStation(-2.68f, 0.767f, 0.2f, 0.6f, 0.5f, true, 1f)) };
                }
                case RoomId.Laundry:
                {
                    // V12: the house's own washer and dryer (baked SHHH_203 washing machine / SHHH_206 dryer, measured with recipe_diag look
                    // 2026-11-10: x -0.60 / 0.20, z 2.28, 0.71 x 0.90 x 0.74, fronts facing -z). Laundry is loaded INTO them, not into a box on a table.
                    RecipeStation Wash(RecipeStation st) => st
                        // anchor = the drum (centre ~0.40 m up, ~0.08 m in front of the cabinet centre): slots are INSIDE the drum
                        .WithAnchor("washer", new Vec3(-0.60f, 0.40f, 2.20f), 180f).WithAnchor("washer2", new Vec3(-0.60f, 0.40f, 2.20f), 180f)
                        .WithAnchor("dryer", new Vec3(0.20f, 0.40f, 2.20f), 180f)
                        // LD09: the lint filter is behind the dryer door (lint starts in the dryer); the lint bin is the lidded bin by the right wall
                        .WithAnchor("lint_panel", new Vec3(0.20f, 0.40f, 2.20f), 180f).WithAnchor("lint_bin", new Vec3(2.52f, 0.05f, 0.15f), -90f)
                        // LD08: the towel cupboard is the chest of drawers' top drawer (chest at x 2.45 z -0.55 against the right wall, front -x)
                        .WithAnchor("towel_cupboard", new Vec3(2.40f, 0.62f, -0.55f), -90f);
                    return new[] {
                    Wash(new RecipeStation(0.5f, 0.8f, -0.55f, 1.4f, 0.7f, false, -1f)), Wash(new RecipeStation(-0.95f, 0.8f, -1.45f, 1.0f, 0.55f, false, -1f)),
                    new RecipeStation(-2.38f, 0.85f, 0.0f, 0.56f, 0.5f, true, 1f), new RecipeStation(-2.38f, 0.85f, 0.6f, 0.56f, 0.5f, true, 1f) };
                }
                default: return new[] {
                    new RecipeStation(-1.2f, 0.74f, -1.35f, 1.5f, 0.55f, false, -1f) };
            }
        }

        /// <summary>V13 KT17: kitchen coordinates of the fridge's middle shelf (measured from the baked fridge it replaces).</summary>
        public static Vec3 FridgeShelf = new Vec3(2.8f, 0.52f, 2.55f);

        public static HouseArea AreaOf(RoomId room) => room == RoomId.Kitchen ? HouseArea.Kitchen : room == RoomId.Laundry ? HouseArea.Laundry : HouseArea.Living;

        /// <summary>
        /// Areas whose recipes a built room can stage on its tables. Until the dedicated rooms exist (Gate 2), the round's room hosts
        /// its whole focus group (spec round focus): living room + foyer + hallway; kitchen + pet area; laundry + bathroom + bedroom.
        /// </summary>
        public static HouseArea[] AreasHostedBy(RoomId room) => room == RoomId.Kitchen ? new[] { HouseArea.Kitchen, HouseArea.PetArea }
            : room == RoomId.Laundry ? new[] { HouseArea.Laundry, HouseArea.Bathroom, HouseArea.ChildBedroom }
            : new[] { HouseArea.Living, HouseArea.Foyer, HouseArea.Hallway };

        /// <summary>Slot pitch for a fixture: wide enough for the widest item that is placed in (or starts in) it.</summary>
        /// <summary>A fixture whose items are all flat and that has 2+ slots is a vertical stack (slot k sits on slot k-1).</summary>
        /// <summary>Racks whose items stand on edge between tines (a dish drying rack): narrow slots, never a flat stack.</summary>
        public static bool IsUprightRack(string kind) => kind == "plate_rack";
        /// <summary>Fixtures that are part of the house (the kitchen sink), not table items.</summary>
        public static bool IsAnchored(string kind) => kind == "sink_tap";
        /// <summary>Kitchen sink-run worktop height (the KayKit sink module's measured top; counter_side modules share it).</summary>
        public const float WorktopY = 0.80f;
        /// <summary>V10: is this fixture kind placed at a world anchor (not on the station table) for this station?</summary>
        public static bool AnchoredAt(RecipeStation s, string kind) => IsAnchored(kind) || s.TryAnchor(kind, false, out _);

        public static bool IsStack(RecipeDef r, RecipeFixtureDecl f)
        {
            if (IsUprightRack(f.Kind)) return false;
            if (f.SlotTags.Length < 2) return false; bool any = false;
            foreach (var s in r.Steps) if (s.Kind == StepKind.Place && s.Fixture == f.Kind && s.PropKind != null) { any = true; if (!RecipePropSize.IsFlat(s.PropKind)) return false; }
            foreach (var p in r.Props) if (p.StartIn == f.Kind) { any = true; if (!RecipePropSize.IsFlat(p.Kind)) return false; }
            return any;
        }
        /// <summary>mode 0 = widest footprint, 1 = thickness (stacks), 2 = narrow side (slot pitch).</summary>
        static float MaxItem(RecipeDef r, RecipeFixtureDecl f, int mode)
        {
            float w = 0;
            Func<string, float> m = k => mode == 1 ? RecipePropSize.Thickness(k) : mode == 2 ? RecipePropSize.NarrowOf(k) : RecipePropSize.Of(k);
            foreach (var s in r.Steps) if (s.Kind == StepKind.Place && s.Fixture == f.Kind && s.PropKind != null) w = Math.Max(w, m(s.PropKind));
            foreach (var p in r.Props) if (p.StartIn == f.Kind) w = Math.Max(w, m(p.Kind));
            return w;
        }
        public static float Pitch(RecipeDef r, RecipeFixtureDecl f)
        {
            if (IsStack(r, f)) return 0f;
            if (IsUprightRack(f.Kind)) return 0.10f;    // V10: every other gap of the KIT018 rack at its authored 1:1 size (dividers 0.05 m apart)
            return Math.Max(0.06f, MaxItem(r, f, 2) + 0.03f);
        }
        /// <summary>Fixtures with their own body (not a slot row): a wash basin must take a whole plate under its tap.</summary>
        static readonly Dictionary<string, float> BodyWidth = new Dictionary<string, float> { ["sink_tap"] = 0.68f, ["plate_rack"] = 0.45f };   // the house sink basin; daycare KIT018 dish rack at authored 1:1 (V10: 0.85 scale left the tray shallower than a plate)
        static float FixtureWidth(RecipeDef r, RecipeFixtureDecl f) => BodyWidth.TryGetValue(f.Kind, out var bw) ? bw : IsStack(r, f) ? Math.Max(0.16f, MaxItem(r, f, 0) + 0.04f) : Math.Max(0.16f, f.SlotTags.Length * Pitch(r, f) + 0.03f);
        /// <summary>Fixture footprint depth (m) along the station's depth axis.</summary>
        public static float FixtureDepth(RecipeDef r, RecipeFixtureDecl f) => f.Kind == "sink_tap" ? 0.36f : f.Kind == "plate_rack" ? 0.30f : Math.Max(0.12f, Math.Min(0.32f, MaxItem(r, f, 0) + 0.04f));

        const float RowGap = 0.03f, FrontOverhang = 0.01f, Margin = 0.02f, PropGap = 0.03f;

        /// <summary>A layout plan in station coordinates (u along the table, t toward the player).</summary>
        sealed class Plan
        {
            public bool Side; public float FixT; public float[] FixU; public float[] PropU, PropT, PropUp;
        }

        /// <summary>
        /// Two layouts are tried: "rows" (fixtures along the back edge, loose props in rows in front of them) and "side" (fixtures at
        /// one end of the table, props packed in rows beside them). Loose props lie with their long side toward the player and are
        /// packed by their narrow side; rows are spaced by their longest item so footprints never overlap. Null = does not fit.
        /// </summary>
        static Plan MakePlan(RecipeDef r, RecipeStation s, IList<string> looseKinds)
        {
            foreach (var f in r.Fixtures) if (IsAnchored(f.Kind) && !s.HasSink) return null;
            var tableFix = new List<RecipeFixtureDecl>(); foreach (var f in r.Fixtures) if (!AnchoredAt(s, f.Kind)) tableFix.Add(f);
            { var lk = new List<string>(); foreach (var k in looseKinds) if (!s.TryAnchor(k, true, out _)) lk.Add(k); looseKinds = lk; }
            foreach (var f in tableFix) if (FixtureDepth(r, f) > s.Depth - 0.04f) return null;
            foreach (var k in looseKinds) if (RecipePropSize.Of(k) > RecipePropSize.StationMax) return null;
            float wf = -FixtureGap, fd = 0; var fw = new List<float>();
            foreach (var f in tableFix) { float w = FixtureWidth(r, f); fw.Add(w); wf += w + FixtureGap; fd = Math.Max(fd, FixtureDepth(r, f)); }
            if (tableFix.Count == 0) wf = 0;
            float fixT = -s.Depth * 0.5f + fd * 0.5f + 0.03f;
            for (int mode = 0; mode < 2; mode++)
            {
                bool side = mode == 1;
                float zoneU0, zoneU1, tEdge;
                if (!side) { if (wf > s.Length - 2 * Margin) continue; zoneU0 = -s.Length * 0.5f + Margin; zoneU1 = s.Length * 0.5f - Margin; tEdge = fixT + fd * 0.5f + 0.02f; }
                else { zoneU0 = -s.Length * 0.5f + Margin + wf + FixtureGap; zoneU1 = s.Length * 0.5f - Margin; tEdge = -s.Depth * 0.5f + 0.03f; if (zoneU1 - zoneU0 < 0.06f && looseKinds.Count > 0) continue; }
                var plan = new Plan { Side = side, FixT = fixT, FixU = new float[fw.Count], PropU = new float[looseKinds.Count], PropT = new float[looseKinds.Count], PropUp = new float[looseKinds.Count] };
                float u = side ? -s.Length * 0.5f + Margin : -wf * 0.5f;
                for (int i = 0; i < fw.Count; i++) { plan.FixU[i] = u + fw[i] * 0.5f; u += fw[i] + FixtureGap; }
                // flat items of one kind start as a loose pile (up to 3: a pile of magazines, lids, plates); each pile/item is one footprint
                var units = new List<List<int>>();
                for (int i = 0; i < looseKinds.Count; i++)
                {
                    var last = units.Count > 0 ? units[units.Count - 1] : null;
                    if (last != null && RecipePropSize.IsFlat(looseKinds[i]) && looseKinds[last[0]] == looseKinds[i] && last.Count < 3 && !JoinKind(r, looseKinds[i]) && !Washable(r, looseKinds[i])) last.Add(i);
                    else units.Add(new List<int> { i });
                }
                bool ok = true; int idx = 0; float len = zoneU1 - zoneU0;
                while (idx < units.Count && ok)
                {
                    var row = new List<int>(); float used = -PropGap, deep = 0;
                    while (idx < units.Count)
                    {
                        string k0 = looseKinds[units[idx][0]]; float n = RecipePropSize.NarrowOf(k0);
                        if (row.Count > 0 && used + PropGap + n > len) break;
                        if (row.Count == 0 && n > len) { ok = false; break; }
                        row.Add(idx); used += PropGap + n; deep = Math.Max(deep, RecipePropSize.Of(k0)); idx++;
                    }
                    if (!ok) break;
                    float t = tEdge + deep * 0.5f;
                    if (t + deep * 0.5f > s.Depth * 0.5f + FrontOverhang) { ok = false; break; }
                    float cu = (zoneU0 + zoneU1) * 0.5f - used * 0.5f;
                    foreach (var ui in row)
                    {
                        string k0 = looseKinds[units[ui][0]]; float n = RecipePropSize.NarrowOf(k0);
                        for (int j = 0; j < units[ui].Count; j++) { int it = units[ui][j]; plan.PropU[it] = cu + n * 0.5f; plan.PropT[it] = t; plan.PropUp[it] = j * (RecipePropSize.Thickness(k0) + 0.004f); }
                        cu += n + PropGap;
                    }
                    tEdge = t + deep * 0.5f + RowGap;
                }
                if (ok) return plan;
            }
            return null;
        }

        /// <summary>Kinds that something gets attached to / paired with: never start them in a pile (a pile hides which one you touch).</summary>
        static bool JoinKind(RecipeDef r, string kind)
        {
            foreach (var s in r.Steps) if ((s.Kind == StepKind.Attached && (s.PartnerKind == kind || s.PropKind == kind)) || (s.Kind == StepKind.Paired && s.PropKind == kind)) return true;
            return false;
        }

        static bool Washable(RecipeDef r, string kind) { foreach (var p in r.Props) if (p.Kind == kind && p.StrokeCap > 0) return true; return false; }

        static List<string> LooseKinds(RecipeDef r)
        {
            var l = new List<string>();
            foreach (var p in r.Props) if (string.IsNullOrEmpty(p.StartIn)) for (int i = 0; i < p.Count; i++) l.Add(p.Kind);
            return l;
        }

        /// <summary>True when the recipe (fixtures + loose props) can be laid out on this station.</summary>
        public static bool Fits(RecipeDef r, RecipeStation s) => MakePlan(r, s, LooseKinds(r)) != null;

        /// <summary>Assign each picked recipe a distinct station it fits (small exhaustive search), or null.</summary>
        public static int[] Assign(IList<RecipeDef> picks, RecipeStation[] st)
        {
            var res = new int[picks.Count]; var used = new bool[st.Length];
            bool Rec(int i)
            {
                if (i == picks.Count) return true;
                for (int k = 0; k < st.Length; k++) if (!used[k] && Fits(picks[i], st[k])) { used[k] = true; res[i] = k; if (Rec(i + 1)) return true; used[k] = false; }
                return false;
            }
            return picks.Count <= st.Length && Rec(0) ? res : null;
        }

        /// <summary>Yaw (deg) that turns local +Z toward the player at this station (fixtures and loose props; long side toward the player).</summary>
        public static float PropYaw(RecipeStation s) => s.AlongZ ? (s.Front > 0 ? 90f : -90f) : (s.Front > 0 ? 0f : 180f);

        /// <summary>Place one instance on a station: fixture placements (with slot positions) and prop spawn points in board prop order.</summary>
        public static List<RecipeFixturePlacement> Place(RecipeBoard board, RecipeInstance inst, RecipeStation s, int stationIndex, out Dictionary<int, Vec3> propSpawn)
        {
            var list = new List<RecipeFixturePlacement>();
            var loose = new List<int>(); var placed = new List<int>();
            foreach (var kv in inst.PropsByKind) foreach (var id in kv.Value) (board.World.Prop(id).IsPlaced ? placed : loose).Add(id);
            loose.Sort();
            var kinds = new List<string>(); foreach (var id in loose) kinds.Add(board.World.Prop(id).Kind);
            var plan = MakePlan(inst.Def, s, kinds) ?? throw new InvalidOperationException($"{inst.Def.Id} does not fit station {stationIndex}");
            float yaw = PropYaw(s);
            int ti = 0;
            for (int i = 0; i < inst.Def.Fixtures.Count; i++)
            {
                var fd = inst.Def.Fixtures[i];
                var fx = board.World.Fixture(inst.Fixture(fd.Kind));
                if (!IsAnchored(fd.Kind) && s.TryAnchor(fd.Kind, false, out var wa))
                {
                    // V10 world anchor (counter by the sink): the fixture sits on real furniture, slots spread along its own x axis
                    float wpitch = Pitch(inst.Def, fd), ww = FixtureWidth(inst.Def, fd);
                    var wpl = new RecipeFixturePlacement { Instance = inst.Index, FixtureId = fx.Id, Kind = fd.Kind, HasDoor = fd.HasDoor, Position = wa.Pos, Yaw = wa.Yaw, Width = ww, Pitch = wpitch, Depth = FixtureDepth(inst.Def, fd), Stack = IsStack(inst.Def, fd), Station = stationIndex, Slots = new Vec3[fd.SlotTags.Length], Anchored = true };
                    float cy = (float)Math.Cos(wa.Yaw * Math.PI / 180.0), sy = (float)Math.Sin(wa.Yaw * Math.PI / 180.0);
                    for (int k = 0; k < wpl.Slots.Length; k++) { float u = (k - (wpl.Slots.Length - 1) * 0.5f) * wpitch; wpl.Slots[k] = new Vec3(wa.Pos.x + u * cy, wa.Pos.y + 0.02f, wa.Pos.z - u * sy); }
                    list.Add(wpl); continue;
                }
                if (IsAnchored(fd.Kind))
                {
                    list.Add(new RecipeFixturePlacement { Instance = inst.Index, FixtureId = fx.Id, Kind = fd.Kind, HasDoor = fd.HasDoor, Position = s.Sink, Yaw = s.SinkYaw, Width = FixtureWidth(inst.Def, fd), Pitch = 0, Depth = FixtureDepth(inst.Def, fd), Stack = false, Station = stationIndex, Slots = new Vec3[0], Anchored = true });
                    continue;
                }
                float w = FixtureWidth(inst.Def, fd), pitch = Pitch(inst.Def, fd), cu = plan.FixU[ti++], back = s.Front * plan.FixT;
                var pl = new RecipeFixturePlacement { Instance = inst.Index, FixtureId = fx.Id, Kind = fd.Kind, HasDoor = fd.HasDoor, Position = s.Local(cu, back), Yaw = yaw, Width = w, Pitch = pitch, Depth = FixtureDepth(inst.Def, fd), Stack = IsStack(inst.Def, fd), Station = stationIndex, Slots = new Vec3[fd.SlotTags.Length] };
                float thick = MaxItem(inst.Def, fd, 1);
                for (int k = 0; k < pl.Slots.Length; k++) pl.Slots[k] = pl.Stack ? s.Local(cu, back, 0.02f + k * thick) : s.Local(cu + (k - (pl.Slots.Length - 1) * 0.5f) * pitch, back, 0.02f);
                list.Add(pl);
            }
            propSpawn = new Dictionary<int, Vec3>();
            foreach (var id in placed) { var p = board.World.Prop(id); var fp = list.Find(x => x.FixtureId == p.Fixture); propSpawn[id] = fp.Slots[p.Slot]; }
            // V10: props with a world anchor (the sponge in its spot by the sink) start there; the rest follow the station plan order
            int pi = 0; var anchoredCount = new Dictionary<string, int>();
            for (int i = 0; i < loose.Count; i++)
            {
                var kind = board.World.Prop(loose[i]).Kind;
                if (s.TryAnchor(kind, true, out var pa)) { anchoredCount.TryGetValue(kind, out var n); anchoredCount[kind] = n + 1; propSpawn[loose[i]] = new Vec3(pa.Pos.x + n * 0.09f, pa.Pos.y, pa.Pos.z); continue; }
                propSpawn[loose[i]] = s.Local(plan.PropU[pi], s.Front * plan.PropT[pi], 0.04f + plan.PropUp[pi]); pi++;
            }
            return list;
        }
    }
}
