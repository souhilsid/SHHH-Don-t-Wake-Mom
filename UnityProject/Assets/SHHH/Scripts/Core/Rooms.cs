using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum RoomId { LivingRoom = 0, Kitchen = 1, Laundry = 2, Practice = 3 }

    /// <summary>A physical, grabbable prop. Kind maps to a visual prefab in the Unity layer (see Art/PropCatalog).</summary>
    [Serializable]
    public sealed class PropSpec
    {
        public int Id;
        public string Kind;
        public NoiseMaterial Material;
        public float MassKg;
        public Vec3 Spawn;
        public float SpawnYaw;
        public bool Grabbable = true;
        public bool PetToy;       // pets are attracted to it; squeaks (Lure)
        public bool PetKnockable; // cat/dog can knock it from a ledge
        public string Tag;        // V7 recipe props: colour/name/size tag used for sorting and matching (shown on the prop)
        public bool Recipe;       // V7: a recipe item (visual long side is normalised along local Z, the layout's assumption)
        public string StartHint;  // V12: where it was left in the house ("on the sofa in the living room"); null = at its job
    }

    /// <summary>A target location where a prop of a given kind completes a chore step (shelf slot, rack hook, drum...).</summary>
    [Serializable]
    public sealed class AnchorSpec
    {
        public int Id;
        public string Kind;
        public string AcceptsPropKind;
        public Vec3 Position;
        public float Radius = 0.12f;
    }

    /// <summary>Static non-grabbable furniture (for scene building, nav-blocking and bot perception).</summary>
    [Serializable]
    public sealed class FurnitureSpec
    {
        public string Kind;
        public Vec3 Position;
        public float Yaw;
        public Vec3 Size; // approximate footprint/extent (m)
        public FurnitureSpec Shifted(Vec3 o) => new FurnitureSpec { Kind = Kind, Position = Position + o, Yaw = Yaw, Size = Size };
    }

    [Serializable]
    public sealed class RoomDefinition
    {
        public RoomId Id;
        public string Title;
        public float Width = 7f, Depth = 6f, Height = 2.7f;
        public Vec3 MomDoor;           // Mom's bedroom door (noise distance reference, entry for Mom)
        public Vec3[] SiblingSpawns = new Vec3[4];
        public Vec3 ChoreBoard;        // in-world taskboard position
        public Vec3 CatBed, DogBed;
        /// <summary>V12 where each pet's food bowl stands (house coordinates; zero = none).</summary>
        public Vec3 CatBowl, DogBowl;
        /// <summary>V12 bin openings (house coordinates) for bots binning trash - filled by the host's world build.</summary>
        public readonly List<Vec3> Bins = new List<Vec3>();
        public readonly List<PropSpec> Props = new List<PropSpec>();
        public readonly List<AnchorSpec> Anchors = new List<AnchorSpec>();
        public readonly List<FurnitureSpec> Furniture = new List<FurnitureSpec>();
        public readonly List<Vec3> SpectatorViews = new List<Vec3>();

        public PropSpec Prop(int id) { foreach (var p in Props) if (p.Id == id) return p; return null; }
        public AnchorSpec Anchor(int id) { foreach (var a in Anchors) if (a.Id == id) return a; return null; }
    }

    /// <summary>
    /// Data-driven room layouts — the SINGLE source of truth used by both the headless simulation and the Unity
    /// scene builder (Editor/ShhhSceneBuilder), so bots, chores and art always agree on positions.
    /// Coordinate frame: origin at room floor centre, +Z toward Mom's bedroom door, metres.
    /// </summary>
    public static class RoomCatalog
    {
        static readonly Dictionary<RoomId, RoomDefinition> _cache = new Dictionary<RoomId, RoomDefinition>();

        public static RoomDefinition Get(RoomId id)
        {
            if (!_cache.TryGetValue(id, out var r)) { r = Build(id); _cache[id] = r; }
            return r;
        }

        static RoomDefinition Build(RoomId id)
        {
            switch (id)
            {
                case RoomId.LivingRoom: return LivingRoom();
                case RoomId.Kitchen: return Kitchen();
                case RoomId.Laundry: return Laundry();
                default: return Practice();
            }
        }

        // ---------- helpers ----------
        sealed class B
        {
            public readonly RoomDefinition R; int _p = 1, _a = 1001;
            public B(RoomDefinition r) { R = r; }
            public void Prop(string kind, NoiseMaterial m, float mass, float x, float y, float z, float yaw = 0, bool toy = false, bool knock = false)
                => R.Props.Add(new PropSpec { Id = (int)R.Id * 10000 + _p++, Kind = kind, Material = m, MassKg = mass, Spawn = new Vec3(x, y, z), SpawnYaw = yaw, PetToy = toy, PetKnockable = knock });
            public void Anchor(string kind, string accepts, float x, float y, float z, float radius = 0.12f)
                => R.Anchors.Add(new AnchorSpec { Id = (int)R.Id * 10000 + _a++, Kind = kind, AcceptsPropKind = accepts, Position = new Vec3(x, y, z), Radius = radius });
            public void Furn(string kind, float x, float y, float z, float yaw, float sx, float sy, float sz)
                => R.Furniture.Add(new FurnitureSpec { Kind = kind, Position = new Vec3(x, y, z), Yaw = yaw, Size = new Vec3(sx, sy, sz) });
        }

        static void CommonShell(RoomDefinition r)
        {
            r.MomDoor = new Vec3(0f, 0f, r.Depth * 0.5f - 0.05f);
            r.SiblingSpawns[0] = new Vec3(-1.2f, 0, -2.0f);
            r.SiblingSpawns[1] = new Vec3(-0.4f, 0, -2.2f);
            r.SiblingSpawns[2] = new Vec3(0.4f, 0, -2.2f);
            r.SiblingSpawns[3] = new Vec3(1.2f, 0, -2.0f);
            r.ChoreBoard = new Vec3(-r.Width * 0.5f + 0.05f, 1.45f, -1.6f);
            r.SpectatorViews.Add(new Vec3(-r.Width * 0.5f + 0.6f, 0, -r.Depth * 0.5f + 0.6f));
            r.SpectatorViews.Add(new Vec3(r.Width * 0.5f - 0.6f, 0, -r.Depth * 0.5f + 0.6f));
        }

        static RoomDefinition LivingRoom()
        {
            var r = new RoomDefinition { Id = RoomId.LivingRoom, Title = "Living Room Panic", Width = 7.2f, Depth = 6.2f };
            CommonShell(r);
            r.CatBed = new Vec3(3.0f, 0, 0.6f); r.DogBed = new Vec3(-2.9f, 0, 1.6f);
            var b = new B(r);
            // Furniture
            b.Furn("sofa", 0f, 0f, -0.2f, 0f, 2.1f, 0.9f, 0.9f);
            b.Furn("coffee_table", 0f, 0f, 0.9f, 0f, 1.1f, 0.42f, 0.6f);
            b.Furn("bookshelf", -3.3f, 0f, -0.3f, 90f, 1.2f, 1.9f, 0.35f);
            b.Furn("tv_stand", 0f, 0f, 2.6f, 180f, 1.6f, 0.5f, 0.45f);
            b.Furn("toy_box", 3.1f, 0f, -1.55f, -90f, 0.7f, 0.9f, 0.45f);  // raised toy chest: slots at 0.72 m
            b.Furn("shoe_rack", 3.25f, 0f, -0.3f, -90f, 1.15f, 0.8f, 0.32f);  // waist-high shoe shelf
            b.Furn("armchair", 2.15f, 0f, 0.55f, -135f, 0.9f, 0.9f, 0.9f); // V4: clear the kitchen arch / hall approach
            b.Furn("side_table", -2.2f, 0f, 2.4f, 0f, 0.5f, 0.6f, 0.5f);
            b.Furn("rug", 0f, 0f, 0.6f, 0f, 3.0f, 0.01f, 2.2f);
            b.Furn("lamp_floor", -2.9f, 0f, 2.6f, 0f, 0.4f, 1.6f, 0.4f);
            b.Furn("dog_bed", r.DogBed.x, 0, r.DogBed.z, 0, 0.8f, 0.2f, 0.6f);
            b.Furn("cat_bed", r.CatBed.x, 0, r.CatBed.z, 0, 0.5f, 0.15f, 0.5f);
            // V6 reach pass: every duty item starts on a waist-high "messy pile" surface (0.74 m) near the sibling spawns,
            // never on the floor (Quest recording 20261107-201409: players kept crouching for floor toys).
            b.Furn("mess_table", -1.65f, 0f, -1.25f, 0f, 1.1f, 0.74f, 0.55f);   // A: books, magazines, remote, dog toy
            b.Furn("mess_table", 1.65f, 0f, -1.25f, 0f, 1.1f, 0.74f, 0.55f);    // B: toys + cups
            b.Furn("mess_table", 2.65f, 0f, -2.6f, 0f, 1.0f, 0.74f, 0.5f);      // C: shoes + picture frame
            const float T = 0.76f;
            b.Prop("book", NoiseMaterial.Paper, 0.45f, -2.05f, T, -1.3f, 20);
            b.Prop("book", NoiseMaterial.Paper, 0.45f, -1.8f, T, -1.15f, -40);
            b.Prop("book", NoiseMaterial.Paper, 0.45f, -1.55f, T, -1.35f, 70);
            b.Prop("book", NoiseMaterial.Paper, 0.45f, 0.2f, 0.44f, 0.9f, 95);
            b.Prop("book", NoiseMaterial.Paper, 0.45f, -0.6f, 0.45f, -0.2f, 10);
            b.Prop("toy_brick", NoiseMaterial.Toy, 0.08f, 1.25f, T, -1.35f, 0, false, true);
            b.Prop("toy_brick", NoiseMaterial.Toy, 0.08f, 1.38f, T, -1.15f, 30, false, true);
            b.Prop("toy_brick", NoiseMaterial.Toy, 0.08f, 1.52f, T, -1.38f, 60, false, true);
            b.Prop("toy_brick", NoiseMaterial.Toy, 0.08f, 1.66f, T, -1.12f, 10, false, true);
            b.Prop("toy_car", NoiseMaterial.Toy, 0.2f, 1.85f, T, -1.32f, 45, false, true);
            b.Prop("toy_car", NoiseMaterial.Toy, 0.2f, 2.05f, T, -1.12f, 120, false, true);
            b.Prop("cushion", NoiseMaterial.Soft, 0.5f, 2.15f, 0.5f, 0.55f, 15);
            b.Prop("cushion", NoiseMaterial.Soft, 0.5f, 0.3f, 0.45f, 0.95f, -30);
            b.Prop("cushion", NoiseMaterial.Soft, 0.5f, -0.3f, 0.45f, 0.95f, 40);
            b.Prop("remote", NoiseMaterial.Plastic, 0.15f, -1.3f, T, -1.1f, 30, false, true);
            b.Prop("cup", NoiseMaterial.Ceramic, 0.3f, 2.15f, T, -1.38f, 0, false, true);
            b.Prop("cup", NoiseMaterial.Ceramic, 0.3f, 1.22f, T, -1.1f, 0, false, true); // V5: keep the whole base on the authored table.
            b.Prop("cup", NoiseMaterial.Ceramic, 0.3f, -2.2f, 0.62f, 2.4f, 0, false, true);
            b.Prop("shoe", NoiseMaterial.Fabric, 0.35f, 2.3f, T, -2.7f, 30);
            b.Prop("shoe", NoiseMaterial.Fabric, 0.35f, 2.55f, T, -2.5f, -60);
            b.Prop("shoe", NoiseMaterial.Fabric, 0.35f, 2.8f, T, -2.72f, 80);
            b.Prop("shoe", NoiseMaterial.Fabric, 0.35f, 3.0f, T, -2.48f, 10);
            b.Prop("magazine", NoiseMaterial.Paper, 0.25f, -1.95f, T + 0.03f, -1.1f, 25);
            b.Prop("magazine", NoiseMaterial.Paper, 0.25f, -1.25f, T, -1.38f, -15);
            b.Prop("dog_toy", NoiseMaterial.Toy, 0.12f, -1.15f, T, -1.25f, 0, true, false);
            b.Prop("picture_frame", NoiseMaterial.Glass, 0.6f, 2.45f, T, -2.75f, 0, false, true);
            b.Prop("vase", NoiseMaterial.Ceramic, 0.7f, -2.2f, 0.62f, 2.2f, 0, false, true);
            // Anchors
            for (int i = 0; i < 5; i++) b.Anchor("shelf_book", "book", -3.25f, 1.05f, -0.75f + i * 0.2f, 0.11f);
            for (int i = 0; i < 6; i++) b.Anchor("toybox_slot", i < 4 ? "toy_brick" : "toy_car", 3.04f + (i / 3) * 0.13f, 0.72f, -1.75f + (i % 3) * 0.17f, 0.15f);
            for (int i = 0; i < 3; i++) b.Anchor("sofa_cushion", "cushion", -0.7f + i * 0.7f, 0.5f, -0.35f, 0.25f);
            b.Anchor("tv_remote_spot", "remote", 0.5f, 0.52f, 2.55f, 0.14f);
            for (int i = 0; i < 3; i++) b.Anchor("cup_tray", "cup", -2.3f + i * 0.13f, 0.62f, 2.45f, 0.08f);
            for (int i = 0; i < 4; i++) b.Anchor("shoe_rack_slot", "shoe", 3.25f, 0.82f, -0.7f + i * 0.27f, 0.13f);
            for (int i = 0; i < 2; i++) b.Anchor("magazine_stack", "magazine", -3.25f, 1.34f + i * 0.02f, -0.45f, 0.15f);
            b.Anchor("dog_bed_toy", "dog_toy", r.DogBed.x, 0.12f, r.DogBed.z, 0.3f);
            b.Anchor("frame_hook", "picture_frame", -1.0f, 1.55f, 3.05f, 0.18f);
            b.Anchor("vase_spot", "vase", -2.05f, 0.62f, 2.45f, 0.08f);
            return r;
        }

        static RoomDefinition Kitchen()
        {
            var r = new RoomDefinition { Id = RoomId.Kitchen, Title = "Kitchen Chaos", Width = 6.6f, Depth = 6.0f };
            CommonShell(r);
            r.CatBed = new Vec3(2.8f, 0, -2.4f); r.DogBed = new Vec3(-2.7f, 0, -2.3f);
            var b = new B(r);
            b.Furn("counter_long", -2.95f, 0f, -0.2f, 90f, 2.7f, 0.92f, 0.65f); // V4: 1.6 m shared arch at z=2.15
            b.Furn("counter_sink", 0.8f, 0f, 2.65f, 180f, 2.4f, 0.92f, 0.65f);
            // V10: real counter run beside the sink (same KayKit family, same worktop) - the dish rack, soap and sponge live here
            b.Furn("counter_side", 1.82f, 0f, 2.65f, 180f, 1.06f, 0.96f, 1.04f);   // sink edge 1.28 .. fridge 2.36, sink depth
            b.Furn("counter_side", -0.15f, 0f, 2.65f, 180f, 0.94f, 0.96f, 1.04f);  // stove edge -0.62 .. sink edge 0.32
            b.Furn("stove", -1.0f, 0f, 2.65f, 180f, 0.8f, 0.92f, 0.65f);
            b.Furn("fridge", 2.8f, 0f, 2.55f, 180f, 0.8f, 1.9f, 0.75f);
            b.Furn("dining_table", 0.6f, 0f, -0.2f, 0f, 1.6f, 0.75f, 0.95f);
            b.Furn("chair", 0.0f, 0f, -0.95f, 0f, 0.45f, 0.9f, 0.45f);
            b.Furn("chair", 1.2f, 0f, -0.95f, 0f, 0.45f, 0.9f, 0.45f);
            b.Furn("chair", 0.0f, 0f, 0.55f, 180f, 0.45f, 0.9f, 0.45f);
            b.Furn("chair", 1.2f, 0f, 0.55f, 180f, 0.45f, 0.9f, 0.45f);
            b.Furn("wall_cupboard", -2.95f, 1.5f, 0.3f, 90f, 2.0f, 0.7f, 0.35f);
            b.Furn("pet_mat", 2.6f, 0f, -1.2f, 0f, 0.9f, 0.01f, 0.5f);
            b.Furn("dog_bed", r.DogBed.x, 0, r.DogBed.z, 0, 0.8f, 0.2f, 0.6f);
            b.Furn("cat_bed", r.CatBed.x, 0, r.CatBed.z, 0, 0.5f, 0.15f, 0.5f);
            for (int i = 0; i < 4; i++) b.Prop("plate", NoiseMaterial.Ceramic, 0.45f, 0.2f + i * 0.28f, 0.77f, -0.1f + (i % 2) * 0.2f, i * 20, false, true);
            for (int i = 0; i < 4; i++) b.Prop("mug", NoiseMaterial.Ceramic, 0.3f, 0.4f + i * 0.35f, 0.94f, 2.6f, i * 40, false, true);
            for (int i = 0; i < 5; i++) b.Prop(i % 2 == 0 ? "fork" : "spoon", NoiseMaterial.Metal, 0.05f, 0.1f + i * 0.25f, 0.77f, -0.35f, i * 33);
            b.Prop("pan", NoiseMaterial.Metal, 1.3f, 1.3f, 0.77f, 0.15f, 30);
            b.Prop("pot", NoiseMaterial.Metal, 1.1f, -2.9f, 0.94f, 0.5f, 0);
            b.Prop("pet_bowl", NoiseMaterial.Metal, 0.25f, 1.25f, 0.77f, 0.15f, 0);
            b.Prop("pet_bowl", NoiseMaterial.Metal, 0.25f, -0.05f, 0.77f, 0.15f, 0);
            b.Prop("sponge", NoiseMaterial.Soft, 0.05f, 1.6f, 0.94f, 2.6f, 0);
            b.Prop("cat_toy", NoiseMaterial.Toy, 0.1f, 0.55f, 0.77f, -0.5f, 0, true, false);
            b.Prop("bottle", NoiseMaterial.Glass, 0.6f, -2.9f, 0.94f, -0.6f, 0, false, true);
            b.Prop("bread", NoiseMaterial.Soft, 0.3f, 0.9f, 0.77f, -0.45f, 0);
            for (int i = 0; i < 4; i++) b.Anchor("plate_rack", "plate", 0.25f + i * 0.08f, 1.02f, 2.75f, 0.1f);
            for (int i = 0; i < 4; i++) b.Anchor("mug_hook", "mug", -2.95f, 1.35f, -0.3f + i * 0.28f, 0.1f);
            for (int i = 0; i < 5; i++) b.Anchor("cutlery_tray", i % 2 == 0 ? "fork" : "spoon", -2.9f, 0.94f, 0.9f + i * 0.07f, 0.06f);
            b.Anchor("stove_burner", "pan", -1.15f, 0.94f, 2.6f, 0.15f);
            b.Anchor("pot_shelf", "pot", -2.9f, 0.94f, 0.5f, 0.15f);
            b.Anchor("pet_mat_slot", "pet_bowl", 2.4f, 0.02f, -1.2f, 0.15f);
            b.Anchor("pet_mat_slot", "pet_bowl", 2.8f, 0.02f, -1.2f, 0.15f);
            b.Anchor("sink_spot", "sponge", 0.9f, 0.94f, 2.65f, 0.15f);
            b.Anchor("toy_basket", "cat_toy", r.CatBed.x, 0.1f, r.CatBed.z, 0.3f);
            b.Anchor("bottle_spot", "bottle", -2.9f, 0.94f, -1.2f, 0.1f);
            b.Anchor("bread_bin", "bread", 2.0f, 0.94f, 2.65f, 0.15f);
            return r;
        }

        static RoomDefinition Laundry()
        {
            var r = new RoomDefinition { Id = RoomId.Laundry, Title = "Laundry Final Cleanup", Width = 5.6f, Depth = 5.4f };
            CommonShell(r);
            r.CatBed = new Vec3(2.2f, 0, -2.0f); r.DogBed = new Vec3(-2.2f, 0, -2.0f);
            var b = new B(r);
            b.Furn("washer", -0.6f, 0f, 2.3f, 180f, 0.7f, 0.9f, 0.65f);
            b.Furn("dryer", 0.2f, 0f, 2.3f, 180f, 0.7f, 0.9f, 0.65f);
            b.Furn("fold_table", -2.4f, 0f, 0.3f, 90f, 1.8f, 0.85f, 0.7f);
            b.Furn("shelf_unit", 2.45f, 0f, 1.2f, -90f, 1.4f, 1.8f, 0.4f);
            b.Furn("towel_rack", 1.5f, 0f, 2.55f, 180f, 0.8f, 1.2f, 0.3f);
            b.Furn("shoe_mat", 1.8f, 0f, -1.5f, 0f, 0.9f, 0.01f, 0.5f);
            b.Furn("dog_bed", r.DogBed.x, 0, r.DogBed.z, 0, 0.8f, 0.2f, 0.6f);
            b.Furn("cat_bed", r.CatBed.x, 0, r.CatBed.z, 0, 0.5f, 0.15f, 0.5f);
            // V6 reach pass: laundry piles on two waist-high sorting tables instead of the floor.
            b.Furn("mess_table", 0.5f, 0f, -0.55f, 0f, 1.4f, 0.8f, 0.7f);   // towels + socks
            b.Furn("mess_table", -0.95f, 0f, -1.45f, 0f, 1.0f, 0.8f, 0.55f); // sneakers, detergent, dog toy
            for (int i = 0; i < 4; i++) b.Prop("towel", NoiseMaterial.Fabric, 0.4f, -0.05f + i * 0.33f, 0.82f, -0.72f, i * 25);
            for (int i = 0; i < 4; i++) b.Prop("sock", NoiseMaterial.Fabric, 0.04f, 0.0f + i * 0.3f, 0.82f, -0.38f, i * 50);
            for (int i = 0; i < 3; i++) b.Prop("shirt", NoiseMaterial.Fabric, 0.25f, -2.3f, 0.86f, -0.2f + i * 0.35f, 0);
            b.Prop("laundry_basket", NoiseMaterial.Plastic, 1.2f, 1.6f, 0.02f, 0.2f, 0);
            b.Prop("detergent", NoiseMaterial.Plastic, 1.5f, 0.38f, 0.93f, 2.36f, 0, false, true);   // V12: on top of the dryer, beside the washer
            b.Prop("sneaker", NoiseMaterial.Fabric, 0.4f, -1.25f, 0.82f, -1.55f, 40);
            b.Prop("sneaker", NoiseMaterial.Fabric, 0.4f, -1.0f, 0.82f, -1.35f, -70);
            b.Prop("iron", NoiseMaterial.Metal, 1.2f, -2.35f, 0.86f, 0.9f, 0, false, true);
            b.Prop("dog_toy", NoiseMaterial.Toy, 0.12f, -0.75f, 0.82f, -1.6f, 0, true, false);
            for (int i = 0; i < 4; i++) b.Anchor("towel_rack_bar", "towel", 1.25f + i * 0.17f, 1.0f, 2.5f, 0.12f);
            for (int i = 0; i < 4; i++) b.Anchor("sock_bin", "sock", 2.35f, 0.62f, 0.8f + i * 0.12f, 0.1f);
            for (int i = 0; i < 3; i++) b.Anchor("washer_drum", "shirt", -0.6f, 0.45f, 2.05f, 0.2f);
            b.Anchor("basket_spot", "laundry_basket", -1.6f, 0.02f, 2.2f, 0.3f);
            b.Anchor("detergent_shelf", "detergent", 2.4f, 1.33f, 1.5f, 0.12f);
            for (int i = 0; i < 2; i++) b.Anchor("shoe_mat_slot", "sneaker", 1.6f + i * 0.35f, 0.02f, -1.5f, 0.15f);
            b.Anchor("iron_spot", "iron", 2.4f, 1.33f, 0.9f, 0.12f);
            b.Anchor("dog_bed_toy", "dog_toy", r.DogBed.x, 0.12f, r.DogBed.z, 0.3f);
            return r;
        }

        static RoomDefinition Practice()
        {
            var r = new RoomDefinition { Id = RoomId.Practice, Title = "Practice Room", Width = 5f, Depth = 5f };
            CommonShell(r);
            r.CatBed = new Vec3(2.0f, 0, 1.6f); r.DogBed = new Vec3(-2.0f, 0, 1.6f);
            var b = new B(r);
            b.Furn("coffee_table", 0f, 0f, 0.2f, 0f, 1.1f, 0.42f, 0.6f);
            b.Furn("bookshelf", -2.3f, 0f, -0.3f, 90f, 1.2f, 1.9f, 0.35f);
            // Practice = XR interaction test bench (also used by the automated xr_diag harness): one of each grip archetype
            // (thin book, round cup, flat plate, small toy car, big soft cushion, very thin fork, floppy sock, heavy pan).
            b.Furn("mess_table", -1.2f, 0f, -1.35f, 0f, 1.5f, 0.74f, 0.55f);
            b.Prop("book", NoiseMaterial.Paper, 0.45f, -1.8f, 0.76f, -1.35f, 20);
            b.Prop("cup", NoiseMaterial.Ceramic, 0.3f, -1.55f, 0.76f, -1.4f, 0);
            b.Prop("pan", NoiseMaterial.Metal, 1.3f, 0.4f, 0.44f, 0.05f, 0);
            b.Prop("dog_toy", NoiseMaterial.Toy, 0.12f, -0.6f, 0.76f, -1.2f, 0, true);
            b.Prop("plate", NoiseMaterial.Ceramic, 0.45f, -1.3f, 0.76f, -1.4f, 0);
            b.Prop("toy_car", NoiseMaterial.Toy, 0.2f, -1.05f, 0.76f, -1.45f, 30);
            b.Prop("cushion", NoiseMaterial.Soft, 0.5f, 0.15f, 0.45f, 0.3f, 0);
            b.Prop("fork", NoiseMaterial.Metal, 0.05f, -0.85f, 0.76f, -1.25f, 70);
            b.Prop("sock", NoiseMaterial.Fabric, 0.04f, -0.7f, 0.76f, -1.45f, 10);
            b.Anchor("shelf_book", "book", -2.25f, 1.05f, -0.3f, 0.12f);
            b.Anchor("cup_tray", "cup", -2.25f, 0.62f, 0.1f, 0.1f);
            b.Anchor("dog_bed_toy", "dog_toy", r.DogBed.x, 0.12f, r.DogBed.z, 0.3f);
            return r;
        }
    }
}
