using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>
    /// V12 owner design: the house is lived in - things are NOT laid out for you. Trash lies anywhere (seeded, different every match), the
    /// pet food bag was left somewhere random (maybe the laundry), the pets' bowls stand by their beds. Players find, carry, feed and bin.
    /// Deterministic from the round seed so every peer and every replay builds the same house.
    /// </summary>
    public static class HouseClutter
    {
        public static readonly string[] TrashKinds = { "trash_paper_ball", "trash_tissue", "trash_can_crushed", "trash_banana_peel", "trash_chip_bag", "trash_pizza_box", "trash_juice_box" };
        public static bool IsTrash(string kind) => kind != null && kind.StartsWith("trash_");
        public static bool Keep(string kind) => kind == "detergent" || kind == "pet_food" || kind == "pet_bowl_cat" || kind == "pet_bowl_dog" || IsTrash(kind);

        /// <summary>Add clutter to a built room / house. <paramref name="areas"/>: (room rectangle centre, width, depth) in the room's coordinates.</summary>
        public static void Add(RoomDefinition r, IList<(Vec3 centre, float w, float d)> areas, ulong seed, float duration, IList<(Vec3 centre, float hx, float hz)> stations = null)
        {
            var rng = new DetRng(seed ^ 0x7A5AB1EUL);
            int id = 900000; foreach (var p in r.Props) id = Math.Max(id, p.Id + 1);
            // bowls beside the beds (a pet eats where it sleeps) - on a side that is clear: no furniture, no job table above it, no chore
            // item there (a job laid out over the bowl caught the kibble before it reached the bowl - network run SWFV)
            r.CatBowl = BowlSpot(r, r.CatBed, 0.45f, stations);
            r.DogBowl = BowlSpot(r, r.DogBed, 0.5f, stations);
            r.Props.Add(new PropSpec { Id = id++, Kind = "pet_bowl_cat", Material = NoiseMaterial.Ceramic, MassKg = 0.4f, Spawn = new Vec3(r.CatBowl.x, 0.02f, r.CatBowl.z) });
            r.Props.Add(new PropSpec { Id = id++, Kind = "pet_bowl_dog", Material = NoiseMaterial.Metal, MassKg = 0.5f, Spawn = new Vec3(r.DogBowl.x, 0.02f, r.DogBowl.z) });
            // V12 (Quest run 2026-11-11 #20: the yellow squeaky dog toy sat on the edge of the living-room side table all match, belonging
            // to no job): the dog's toy lies on the floor by the dog's bed, on another clear side than the bowl
            foreach (var q in r.Props)
                if (q.Kind == "dog_toy") { var t = BowlSpot(r, r.DogBed, 0.5f, stations, r.DogBowl); q.Spawn = new Vec3(t.x, 0.06f, t.z); }
            // the food bag: somewhere in the house (not next to the bowls)
            var food = FreeSpot(r, areas, rng, 30); r.Props.Add(new PropSpec { Id = id++, Kind = "pet_food", Material = NoiseMaterial.Paper, MassKg = 1.2f, Spawn = new Vec3(food.x, 0.02f, food.z), SpawnYaw = rng.Range(0f, 360f) });
            // trash: 5 per 5 minutes, spread over every area
            int n = Math.Max(4, Math.Min(12, (int)Math.Round(duration / 60f)));
            for (int i = 0; i < n; i++)
            {
                var at = FreeSpot(r, areas, rng, 40);
                var kind = TrashKinds[rng.Range(0, TrashKinds.Length)];
                r.Props.Add(new PropSpec { Id = id++, Kind = kind, Material = kind == "trash_can_crushed" ? NoiseMaterial.Metal : kind == "trash_pizza_box" ? NoiseMaterial.Paper : NoiseMaterial.Soft, MassKg = 0.05f, Spawn = new Vec3(at.x, 0.02f, at.z), SpawnYaw = rng.Range(0f, 360f) });
            }
        }

        static Vec3 BowlSpot(RoomDefinition r, Vec3 bed, float off, IList<(Vec3 centre, float hx, float hz)> stations, Vec3? avoid = null)
        {
            float inward = bed.x > 0 ? -1f : 1f;
            var cands = new[] { new Vec3(bed.x + inward * off, 0f, bed.z), new Vec3(bed.x - inward * off, 0f, bed.z), new Vec3(bed.x, 0f, bed.z + off), new Vec3(bed.x, 0f, bed.z - off),
                                new Vec3(bed.x + inward * off, 0f, bed.z + off), new Vec3(bed.x + inward * off, 0f, bed.z - off) };
            foreach (var p in cands)
            {
                bool clear = true;
                foreach (var f in r.Furniture)
                {
                    if (f.Kind == "dog_bed" || f.Kind == "cat_bed" || f.Kind == "rug" || f.Kind == "pet_mat" || f.Size.y < 0.05f) continue;
                    float c = (float)Math.Cos(f.Yaw * Math.PI / 180.0), s = (float)Math.Sin(f.Yaw * Math.PI / 180.0);
                    float dx = p.x - f.Position.x, dz = p.z - f.Position.z; float lx = dx * c - dz * s, lz = dx * s + dz * c;
                    if (Math.Abs(lx) < f.Size.x * 0.5f + 0.15f && Math.Abs(lz) < f.Size.z * 0.5f + 0.15f) { clear = false; break; }
                }
                if (clear && stations != null) foreach (var st in stations) if (Math.Abs(p.x - st.centre.x) < st.hx + 0.2f && Math.Abs(p.z - st.centre.z) < st.hz + 0.2f) { clear = false; break; }
                if (clear) foreach (var q in r.Props) if (q.Recipe && p.FlatDistance(q.Spawn) < 0.45f) { clear = false; break; }
                if (clear && avoid.HasValue && p.FlatDistance(avoid.Value) < 0.4f) clear = false;
                if (clear) return p;
            }
            return cands[0];
        }

        /// <summary>A floor point inside one of the areas, 0.4 m from the walls and 0.18 m clear of every furniture footprint and prop.</summary>
        public static Vec3 FreeSpot(RoomDefinition r, IList<(Vec3 centre, float w, float d)> areas, DetRng rng, int tries)
        {
            Vec3 best = areas[0].centre;
            for (int t = 0; t < tries; t++)
            {
                var a = areas[rng.Range(0, areas.Count)];
                var p = new Vec3(a.centre.x + rng.Range(-a.w * 0.5f + 0.4f, a.w * 0.5f - 0.4f), 0f, a.centre.z + rng.Range(-a.d * 0.5f + 0.4f, a.d * 0.5f - 0.4f));
                bool clear = true;
                foreach (var f in r.Furniture)
                {
                    float c = (float)Math.Cos(f.Yaw * Math.PI / 180.0), s = (float)Math.Sin(f.Yaw * Math.PI / 180.0);
                    float dx = p.x - f.Position.x, dz = p.z - f.Position.z; float lx = dx * c - dz * s, lz = dx * s + dz * c;
                    if (Math.Abs(lx) < f.Size.x * 0.5f + 0.18f && Math.Abs(lz) < f.Size.z * 0.5f + 0.18f) { clear = false; break; }
                }
                if (clear) foreach (var q in r.Props) if (p.FlatDistance(q.Spawn) < 0.25f) { clear = false; break; }
                if (clear) return p;
                best = p;
            }
            return best;
        }
    
        // ------------------------------------------------------------------ V12: chore items left around the house
        /// <summary>What kind of "left lying around" a recipe item is (null = a tool or part that lives at its spot: sponge, jar lid ...).</summary>
        public static string ScatterCategory(string kind)
        {
            switch (kind)
            {
                case "shirt": case "sock": case "clothes": case "dirty_clothes": case "coat": case "slipper": case "shoe": case "family_shoe":
                case "backpack": case "bath_towel": case "hand_towel": case "throw": case "pet_blanket": case "laundry_bundle": return "clothes";
                case "cup": case "kid_cup": case "mug": case "plate": case "kid_plate": case "cutlery": return "dishes";
                case "plush": case "pet_toy": case "squeaky_toy": case "stray_toy": case "toy_block": case "toy_car": case "gamepad": case "remote": case "bath_toy": return "toys";
                case "book": case "magazine": case "notebook": case "envelope": case "marker": case "drawing": return "paper";
                case "grocery": case "food_can": case "shopping_bag": case "apple": case "empty_bottle": return "groceries";
                default: return null;
            }
        }

        // measured in the baked house (recipe_diag surfaces, 2026-11-11): coffee table 0.43, side table 0.60, dining table 0.75 and the
        // sofa seat 0.52 match the furniture list; the armchair is a solid 0.96 m box and the TV stand is 0.93 m (not 0.50) - not used
        static bool IsTableTop(string k) => k == "coffee_table" || k == "side_table" || k == "dining_table";
        static bool IsSeat(string k) => k == "sofa";
        public static string RoomName(RoomId id) => id == RoomId.Kitchen ? "kitchen" : id == RoomId.Laundry ? "laundry room" : id == RoomId.LivingRoom ? "living room" : "room";
        static string Say(string k) => k.Replace('_', ' ');

        /// <summary>
        /// The family left things everywhere: most loose chore items (clothes, dishes, toys, books, groceries) do not start at their job
        /// but somewhere believable in the house - clothes on the sofa or the floor, a mug on the coffee table, a toy car under the TV,
        /// shopping in the laundry. Tools and parts stay where they live. Seeded: every peer builds the same house. Each moved item gets a
        /// <see cref="PropSpec.StartHint"/> ("on the coffee table in the living room") for the task HUD.
        /// </summary>
        public static int ScatterChoreProps(RoomDefinition r, IList<(RoomId room, Vec3 centre, float w, float d)> rooms, ulong seed,
            IList<Vec3> stations, IList<Vec3> fixtures, Func<PropSpec, bool> loose, Action<string> log, float share = 0.75f)
        {
            if (rooms == null || rooms.Count == 0) return 0;
            var rng = new DetRng(seed ^ 0x5CA77E2DUL);
            int RoomOf(Vec3 p) { for (int i = 0; i < rooms.Count; i++) { var a = rooms[i]; if (Math.Abs(p.x - a.centre.x) <= a.w * 0.5f && Math.Abs(p.z - a.centre.z) <= a.d * 0.5f) return i; } return -1; }
            bool NearStation(Vec3 p, float m) { foreach (var s in stations) if (p.FlatDistance(s) < m) return true; return false; }
            bool Crowded(Vec3 p, float m)
            {
                foreach (var q in r.Props) if (p.FlatDistance(q.Spawn) < m && Math.Abs(p.y - q.Spawn.y) < 0.3f) return true;
                foreach (var f in fixtures) if (p.FlatDistance(f) < 0.45f) return true;
                return false;
            }
            // usable surfaces (not a table some job is laid out on)
            var tables = new List<FurnitureSpec>(); var seats = new List<FurnitureSpec>();
            foreach (var f in r.Furniture) { if (NearStation(f.Position, 1.0f)) continue; if (IsTableTop(f.Kind)) tables.Add(f); else if (IsSeat(f.Kind)) seats.Add(f); }
            Vec3 OnTop(FurnitureSpec f, float inner, float y)
            {
                float lx = rng.Range(-0.5f, 0.5f) * Math.Max(0f, f.Size.x - inner), lz = rng.Range(-0.5f, 0.5f) * Math.Max(0f, f.Size.z - inner);
                float c = (float)Math.Cos(f.Yaw * Math.PI / 180.0), s = (float)Math.Sin(f.Yaw * Math.PI / 180.0);
                return new Vec3(f.Position.x + lx * c + lz * s, y, f.Position.z - lx * s + lz * c);
            }
            int moved = 0;
            foreach (var p in r.Props)
            {
                if (!p.Recipe || !loose(p)) continue;
                var cat = ScatterCategory(p.Kind); if (cat == null) continue;
                if (rng.Range(0f, 1f) > share) continue;
                int home = RoomOf(p.Spawn); if (home < 0) home = 0;
                int ri = rooms.Count > 1 && rng.Range(0f, 1f) < 0.6f ? (home + 1 + rng.Range(0, rooms.Count - 1)) % rooms.Count : home;
                // where in that room: dishes on tables; clothes on seats or the floor; the rest on the floor or a table
                bool wantTable = cat == "dishes" || (cat != "clothes" && rng.Range(0f, 1f) < 0.4f);
                bool wantSeat = cat == "clothes" && rng.Range(0f, 1f) < 0.5f;
                Vec3 at = default; string hint = null;
                for (int t = 0; t < 12 && hint == null; t++)
                {
                    var list = wantSeat ? seats : wantTable ? tables : null;
                    if (list != null)
                    {
                        var cand = new List<FurnitureSpec>(); foreach (var f in list) if (RoomOf(f.Position) == ri) cand.Add(f);
                        if (cand.Count == 0) foreach (var f in list) cand.Add(f);   // that room has none: any room that does
                        if (cand.Count > 0)
                        {
                            var f = cand[rng.Range(0, cand.Count)];
                            var q = wantSeat ? OnTop(f, f.Size.x * 0.45f, f.Size.y * 0.62f) : OnTop(f, 0.24f, f.Size.y + 0.04f);
                            if (!Crowded(q, 0.2f)) { at = q; int fr = RoomOf(f.Position); hint = $"on the {Say(f.Kind)} in the {RoomName(rooms[Math.Max(0, fr)].room)}"; }
                            continue;
                        }
                    }
                    var fl = FreeSpot(r, new List<(Vec3, float, float)> { (rooms[ri].centre, rooms[ri].w, rooms[ri].d) }, rng, 30);
                    if (!NearStation(fl, 0.7f) && !Crowded(fl, 0.3f)) { at = new Vec3(fl.x, 0.04f, fl.z); hint = $"on the {RoomName(rooms[ri].room)} floor"; }
                }
                if (hint == null) continue;   // no believable free spot: it stays at its job
                log?.Invoke($"SCATTER {p.Kind}#{p.Id} ({cat}) {hint}");
                p.Spawn = at; p.SpawnYaw = rng.Range(0f, 360f); p.StartHint = hint; moved++;
            }
            return moved;
        }
    }
}
