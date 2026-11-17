using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Builds the visual for a prop or furniture kind: an imported CC0 model (normalised to a target size, pivot at the
    /// bottom centre) or a stylised procedural fallback. Visuals carry no active colliders; callers add physics.
    /// </summary>
    public static class VisualFactory
    {
        struct PropArt { public string key; public float size; public Color tint; public PropArt(string k, float s, Color t = default) { key = k; size = s; tint = t; } }

        static PropArt ForProp(string kind)
        {
            switch (kind)
            {
                case "book": return new PropArt("kf_book_single", 0.24f);
                case "toy_brick": return new PropArt("kb_bevel-hq-brick-2x2", 0.07f);
                case "toy_car": return new PropArt("kc_vehicle-racer", 0.16f);
                case "cushion": return new PropArt("kf_pillow_A", 0.42f);
                case "cup": return new PropArt("food_cup-coffee", 0.09f);
                case "mug": return new PropArt("food_mug", 0.1f);
                case "shoe": case "sneaker": return new PropArt("pg_Sneakers", 0.28f);
                case "picture_frame": return new PropArt("kf_pictureframe_medium", 0.32f);
                case "vase": return new PropArt("kr_jar_A_large", 0.24f);
                case "plate": return new PropArt("kr_plate", 0.22f);   // V11: 22 cm dinner plate (RecipePropSize; the 24 cm scale left 2.5 mm to the KIT018 front rim)
                case "fork": return new PropArt("food_utensil-fork", 0.18f);
                case "spoon": return new PropArt("food_utensil-spoon", 0.18f);
                case "pan": return new PropArt("kr_pan_A", 0.42f);
                case "pot": return new PropArt("kr_pot_A", 0.3f);
                case "pet_bowl": return new PropArt("kr_bowl_small", 0.17f);
                case "bottle": return new PropArt("food_soda-bottle", 0.28f);
                case "bread": return new PropArt("food_loaf", 0.24f);
                case "laundry_basket": return new PropArt("kn_cardboardBoxOpen", 0.5f);
                case "detergent": return new PropArt("food_bottle-ketchup", 0.26f);
            }
            return new PropArt(null, 0.2f);
        }

        /// <summary>Astra V2 original SHHH! art (reduced LOD1 meshes where available, for Quest). Falls back to CC0 packs.</summary>
        static PropArt A2Prop(string kind)
        {
            switch (kind)
            {
                case "book": return new PropArt("a2_094_hardcover_book_medium_LOD1", 0.24f);
                case "toy_brick": return new PropArt("a2_100_building_block_cube", 0.08f);
                // V12 pets + trash (original trash models: Tools/blender/make_trash.py)
                case "pet_bowl_cat": return new PropArt("a2_185_cat_food_bowl_LOD1", 0.17f);
                case "pet_bowl_dog": return new PropArt("a2_186_dog_food_bowl_LOD1", 0.27f);   // V12 #19: 0.22 read as a flat disc on the floor
                case "pet_food": return new PropArt("a2_187_food_bag", 0.26f);
                case "trash_paper_ball": return new PropArt("ld_trash_paper_ball", 0.075f);
                case "trash_tissue": return new PropArt("ld_trash_tissue", 0.06f);
                case "trash_can_crushed": return new PropArt("ld_trash_can_crushed", 0.09f);
                case "trash_banana_peel": return new PropArt("ld_trash_banana_peel", 0.18f);
                case "trash_chip_bag": return new PropArt("ld_trash_chip_bag", 0.19f);
                case "trash_pizza_box": return new PropArt("ld_trash_pizza_box", 0.3f);
                case "trash_juice_box": return new PropArt("ld_trash_juice_box", 0.09f);
                case "trash_bag_full": return new PropArt("ld_trash_bag_full", 0.35f);
                case "toy_car": return new PropArt("a2_098_toy_car_red_LOD1", 0.17f);
                case "cushion": return new PropArt("a2_070_removable_sofa_cushion_a", 0.42f);
                case "cup": return new PropArt("a2_106_cup_ceramic_LOD1", 0.1f);
                case "mug": return new PropArt("a2_106_cup_ceramic_LOD1", 0.11f);
                case "shoe": case "sneaker": return new PropArt("a2_109_pair_sneakers_left_LOD1", 0.28f);
                case "picture_frame": return new PropArt("a2_119_child_safe_photo_frame_LOD1", 0.3f);
                case "vase": return new PropArt("a2_118_small_vase", 0.24f);
                case "plate": return new PropArt("a2_160_stackable_plate_small_LOD1", 0.22f);
                case "fork": return new PropArt("a2_167_fork_LOD1", 0.18f);
                case "spoon": return new PropArt("a2_168_spoon", 0.18f);
                case "pan": return new PropArt("a2_175_frying_pan", 0.42f);
                case "pot": return new PropArt("a2_173_small_pot", 0.3f);
                case "pet_bowl": return new PropArt("a2_186_dog_food_bowl_LOD1", 0.18f);
                case "bottle": return new PropArt("a2_182_milk_carton", 0.24f);
                case "bread": return new PropArt("a2_183_bread_loaf", 0.24f);
                case "laundry_basket": return new PropArt("a2_213_laundry_hamper_LOD1", 0.5f);
                case "detergent": return new PropArt("a2_239_detergent_box", 0.24f);
                case "remote": return new PropArt("a2_105_tv_remote_LOD1", 0.18f);
                case "magazine": return new PropArt("a2_096_comic_book_LOD1", 0.26f);
                case "dog_toy": return new PropArt("a2_116_squeaky_dog_toy", 0.14f);
                case "cat_toy": return new PropArt("a2_103_soft_ball", 0.09f);
                case "sponge": return new PropArt("a2_188_sponge", 0.1f);
                case "towel": return new PropArt("a2_228_foldable_towel_blue_LOD1", 0.34f);
                case "sock": return new PropArt("a2_223_red_sock_left_LOD1", 0.2f);
                case "shirt": return new PropArt("a2_234_pajama_trousers_LOD1", 0.34f);
            }
            return new PropArt(null, 0f);
        }

        static string A2Furniture(string kind)
        {
            switch (kind)
            {
                case "sofa": return "a2_069_large_sofa_frame_LOD1";
                case "coffee_table": return "a2_074_coffee_table";
                case "armchair": return "a2_073_armchair_LOD1";
                case "side_table": return "a2_075_side_table";
                case "rug": return "a2_089_living_room_rug_LOD1";
                case "lamp_floor": return "a2_029_floor_lamp_LOD1";
                case "stove": return "a2_141_oven_body_LOD1";
                case "fridge": return "a2_138_fridge_body_LOD1";
                case "chair": return "a2_154_kitchen_stool";
                case "washer": return "a2_203_washing_machine_body_LOD1";
                case "dryer": return "a2_206_dryer_body_LOD1";
                case "fold_table": return "a2_211_folding_table";
            }
            return null;
        }

        /// <summary>True when a real model (V5 handcrafted, Astra A2 or CC0 pack) exists for this prop kind.</summary>
        public static bool HasArt(string kind)
        {
            if (HandcraftedArtCatalog.Prop(kind) != null) return true;
            var a = A2Prop(kind); if (a.key != null && ArtCatalog.I.Model(a.key) != null) return true;
            var f = ForProp(kind); return f.key != null && ArtCatalog.I.Model(f.key) != null;
        }

        public static GameObject BuildProp(string kind, Transform parent)
        {
            var authored = HandcraftedArtCatalog.Prop(kind);
            if (authored != null) { var v5 = Object.Instantiate(authored, parent, false); v5.name = "Visual_" + kind; return v5; }
            var art = A2Prop(kind);
            if (art.key == null || ArtCatalog.I.Model(art.key) == null) art = ForProp(kind);
            var model = ArtCatalog.I.Model(art.key);
            GameObject v = model != null ? Normalise(model, art.size, parent, kind) : ProceduralProp(kind, parent);
            v.name = "Visual_" + kind;
            return v;
        }

        /// <summary>Instantiate, strip colliders, scale uniformly so the largest extent equals <paramref name="size"/>, pivot bottom-centre.</summary>
        public static GameObject Normalise(GameObject model, float size, Transform parent, string name, bool byHeight = false, bool keepAnimators = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var inst = Object.Instantiate(model, root.transform);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Prim.Remove(c);
            if (!keepAnimators) foreach (var a in inst.GetComponentsInChildren<Animator>()) Prim.Remove(a);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
            Mats.MakeUrpSafe(inst);
            var b = Prim.RendererBounds(inst);
            float extent = byHeight ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float s = extent > 1e-4f ? size / extent : 1f;
            inst.transform.localScale = Vector3.one * s;
            b = Prim.RendererBounds(inst);
            Vector3 offset = root.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            inst.transform.position += offset;
            return root;
        }

        static GameObject ProceduralProp(string kind, Transform parent)
        {
            var root = new GameObject(kind); root.transform.SetParent(parent, false);
            var t = root.transform;
            switch (kind)
            {
                case "remote":
                    Prim.Box(t, "body", new Vector3(0, 0.012f, 0), new Vector3(0.05f, 0.024f, 0.17f), Mats.Lit(Palette.Ink), false);
                    Prim.Box(t, "btn", new Vector3(0, 0.026f, 0.05f), new Vector3(0.018f, 0.006f, 0.018f), Mats.Lit(Palette.Coral), false);
                    break;
                case "magazine":
                    Prim.Box(t, "mag", new Vector3(0, 0.005f, 0), new Vector3(0.21f, 0.01f, 0.28f), Mats.Lit(Palette.Purple), false);
                    Prim.Box(t, "cover", new Vector3(0, 0.0105f, 0.03f), new Vector3(0.17f, 0.002f, 0.12f), Mats.Lit(Palette.Yellow), false);
                    break;
                case "dog_toy":
                    Prim.Shape(PrimitiveType.Capsule, t, "bone", new Vector3(0, 0.03f, 0), new Vector3(0.045f, 0.07f, 0.045f), Mats.Lit(Palette.Coral), false).transform.localRotation = Quaternion.Euler(0, 0, 90);
                    Prim.Shape(PrimitiveType.Sphere, t, "k1", new Vector3(0.075f, 0.03f, 0.018f), Vector3.one * 0.045f, Mats.Lit(Palette.Coral), false);
                    Prim.Shape(PrimitiveType.Sphere, t, "k2", new Vector3(0.075f, 0.03f, -0.018f), Vector3.one * 0.045f, Mats.Lit(Palette.Coral), false);
                    Prim.Shape(PrimitiveType.Sphere, t, "k3", new Vector3(-0.075f, 0.03f, 0.018f), Vector3.one * 0.045f, Mats.Lit(Palette.Coral), false);
                    Prim.Shape(PrimitiveType.Sphere, t, "k4", new Vector3(-0.075f, 0.03f, -0.018f), Vector3.one * 0.045f, Mats.Lit(Palette.Coral), false);
                    break;
                case "cat_toy":
                    Prim.Shape(PrimitiveType.Sphere, t, "yarn", new Vector3(0, 0.04f, 0), Vector3.one * 0.08f, Mats.Lit(Palette.Purple), false);
                    break;
                case "sponge":
                    Prim.Box(t, "sponge", new Vector3(0, 0.02f, 0), new Vector3(0.1f, 0.04f, 0.065f), Mats.Lit(Palette.Yellow), false);
                    Prim.Box(t, "scrub", new Vector3(0, 0.043f, 0), new Vector3(0.1f, 0.008f, 0.065f), Mats.Lit(Palette.Mint), false);
                    break;
                case "towel":
                    Prim.Box(t, "towel", new Vector3(0, 0.025f, 0), new Vector3(0.3f, 0.05f, 0.22f), Mats.Lit(Palette.Teal), false);
                    Prim.Box(t, "stripe", new Vector3(0, 0.051f, 0.07f), new Vector3(0.3f, 0.003f, 0.03f), Mats.Lit(Palette.Cream), false);
                    break;
                case "sock":
                    Prim.Box(t, "leg", new Vector3(0, 0.015f, 0), new Vector3(0.07f, 0.03f, 0.16f), Mats.Lit(Palette.Coral), false);
                    Prim.Box(t, "foot", new Vector3(0, 0.015f, 0.1f), new Vector3(0.07f, 0.03f, 0.06f), Mats.Lit(Palette.Cream), false);
                    break;
                case "shirt":
                    Prim.Box(t, "shirt", new Vector3(0, 0.02f, 0), new Vector3(0.32f, 0.04f, 0.26f), Mats.Lit(Palette.Yellow), false);
                    Prim.Box(t, "sleeves", new Vector3(0, 0.02f, 0.08f), new Vector3(0.46f, 0.035f, 0.08f), Mats.Lit(Palette.Yellow), false);
                    break;
                case "iron":
                    Prim.Box(t, "base", new Vector3(0, 0.03f, 0), new Vector3(0.12f, 0.06f, 0.24f), Mats.Lit(new Color(0.75f, 0.78f, 0.82f)), false);
                    Prim.Box(t, "handle", new Vector3(0, 0.1f, -0.01f), new Vector3(0.04f, 0.04f, 0.16f), Mats.Lit(Palette.Teal), false);
                    break;
                case "detergent":
                    Prim.Box(t, "jug", new Vector3(0, 0.12f, 0), new Vector3(0.16f, 0.24f, 0.1f), Mats.Lit(Palette.Coral), false);
                    Prim.Shape(PrimitiveType.Cylinder, t, "cap", new Vector3(0.04f, 0.26f, 0), new Vector3(0.05f, 0.02f, 0.05f), Mats.Lit(Palette.Yellow), false);
                    break;
                case "laundry_basket":
                    var m = Mats.Lit(Palette.Cream);
                    Prim.Box(t, "bottom", new Vector3(0, 0.01f, 0), new Vector3(0.5f, 0.02f, 0.36f), m, false);
                    Prim.Box(t, "w1", new Vector3(0.24f, 0.13f, 0), new Vector3(0.02f, 0.26f, 0.36f), m, false);
                    Prim.Box(t, "w2", new Vector3(-0.24f, 0.13f, 0), new Vector3(0.02f, 0.26f, 0.36f), m, false);
                    Prim.Box(t, "w3", new Vector3(0, 0.13f, 0.17f), new Vector3(0.5f, 0.26f, 0.02f), m, false);
                    Prim.Box(t, "w4", new Vector3(0, 0.13f, -0.17f), new Vector3(0.5f, 0.26f, 0.02f), m, false);
                    break;
                default:
                    Prim.Box(t, "box", new Vector3(0, 0.05f, 0), Vector3.one * 0.1f, Mats.Lit(Palette.Yellow), false);
                    break;
            }
            return root;
        }

        // ------------------------------------------------------------------ furniture
        public static GameObject BuildFurniture(SHHH.Core.FurnitureSpec f, Transform parent, Color wallTint)
        {
            var root = new GameObject("F_" + f.Kind);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = V(f.Position);
            root.transform.localRotation = Quaternion.Euler(0, f.Yaw, 0);
            var size = V(f.Size);
            string key = A2Furniture(f.Kind) != null && ArtCatalog.I.Model(A2Furniture(f.Kind)) != null ? A2Furniture(f.Kind) : FurnitureKey(f.Kind);
            var model = ArtCatalog.I.Model(key);
            if (model != null && !IsProceduralFurniture(f.Kind))
            {
                bool flat = size.y < 0.05f;
                var v = Normalise(model, flat ? Mathf.Max(size.x, size.z) : size.y, root.transform, "model", byHeight: !flat);
                foreach (var mf in v.GetComponentsInChildren<MeshFilter>())
                {
                    if (flat) continue; // rugs: no collider (walkable, avoids trip-physics)
                    var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                }
                Prim.SetLayerRecursive(root, Layers.Furniture);
                return root;
            }
            ProceduralFurniture(f.Kind, root.transform, size);
            Prim.SetLayerRecursive(root, Layers.Furniture);
            return root;
        }

        static bool IsProceduralFurniture(string kind)
        {
            switch (kind) { case "bookshelf": case "toy_box": case "shoe_rack": case "shelf_unit": case "towel_rack": case "dog_bed": case "cat_bed": case "pet_mat": case "shoe_mat": case "wall_cupboard": case "mess_table": return true; }
            return false;
        }

        static string FurnitureKey(string kind)
        {
            switch (kind)
            {
                case "sofa": return "kf_couch_pillows";
                case "coffee_table": return "kf_table_low";
                case "armchair": return "kf_armchair_pillows";
                case "side_table": return "kf_table_small";
                case "rug": return "kf_rug_rectangle_stripes_A";
                case "lamp_floor": return "kf_lamp_standing";
                case "tv_stand": return "kn_cabinetTelevision";
                case "counter_long": return "kr_kitchencounter_straight_A_backsplash";
                case "counter_sink": return "kr_kitchencounter_sink_backsplash";
                case "counter_side": return "kr_kitchencounter_straight_A_backsplash";
                case "stove": return "kr_stove_single";
                case "fridge": return "kr_fridge_A";
                case "dining_table": return "kr_kitchentable_A";
                case "chair": return "kr_chair_A";
                case "washer": return "kn_washer";
                case "dryer": return "kn_dryer";
                case "fold_table": return "kf_table_medium_long";
            }
            return null;
        }

        static void ProceduralFurniture(string kind, Transform t, Vector3 s)
        {
            var wood = Mats.Lit(Palette.WoodLight); var dark = Mats.Lit(Palette.Wood);
            switch (kind)
            {
                case "bookshelf":
                case "shelf_unit":
                {
                    // Open shelf: sides, back and boards at 0.02, 0.62, 1.03, 1.35, top. Boards double as anchor support.
                    float w = s.x, h = s.y, d = s.z;
                    Prim.Box(t, "sideL", new Vector3(-w / 2 + 0.015f, h / 2, 0), new Vector3(0.03f, h, d), dark);
                    Prim.Box(t, "sideR", new Vector3(w / 2 - 0.015f, h / 2, 0), new Vector3(0.03f, h, d), dark);
                    Prim.Box(t, "back", new Vector3(0, h / 2, -d / 2 + 0.01f), new Vector3(w, h, 0.02f), dark);
                    foreach (float y in new[] { 0.02f, 0.6f, 1.02f, 1.33f, h - 0.02f })
                        Prim.Box(t, "board", new Vector3(0, y - 0.015f, 0), new Vector3(w - 0.06f, 0.03f, d), wood);
                    break;
                }
                case "toy_box":
                {
                    // V6: raised toy chest on a cabinet, slots at ~0.72 m (no crouching to the floor).
                    var m = Mats.Lit(Palette.Coral); var cab = Mats.Lit(Palette.Wood);
                    float b0 = Mathf.Max(0.04f, s.y - 0.24f);
                    Prim.Box(t, "cabinet", new Vector3(0, (b0 - 0.02f) / 2, 0), new Vector3(s.x - 0.02f, b0 - 0.02f, s.z - 0.02f), cab);
                    Prim.Box(t, "bottom", new Vector3(0, b0 - 0.02f, 0), new Vector3(s.x, 0.04f, s.z), m);
                    float wh = s.y - b0 + 0.02f, wy = b0 - 0.02f + wh / 2;
                    Prim.Box(t, "f", new Vector3(0, wy, s.z / 2 - 0.02f), new Vector3(s.x, wh, 0.04f), m);
                    Prim.Box(t, "b", new Vector3(0, wy, -s.z / 2 + 0.02f), new Vector3(s.x, wh, 0.04f), m);
                    Prim.Box(t, "l", new Vector3(-s.x / 2 + 0.02f, wy, 0), new Vector3(0.04f, wh, s.z), m);
                    Prim.Box(t, "r", new Vector3(s.x / 2 - 0.02f, wy, 0), new Vector3(0.04f, wh, s.z), m);
                    Prim.Box(t, "stripe", new Vector3(0, b0 * 0.55f, s.z / 2 + 0.002f), new Vector3(s.x * 0.9f, 0.06f, 0.01f), Mats.Lit(Palette.Yellow), false);
                    break;
                }
                case "shoe_rack":
                    Prim.Box(t, "lower", new Vector3(0, s.y * 0.45f, 0), new Vector3(s.x, 0.03f, s.z), wood);
                    Prim.Box(t, "upper", new Vector3(0, s.y - 0.015f, 0), new Vector3(s.x, 0.03f, s.z), wood);
                    Prim.Box(t, "l", new Vector3(-s.x / 2, s.y / 2, 0), new Vector3(0.03f, s.y, s.z), dark);
                    Prim.Box(t, "r", new Vector3(s.x / 2, s.y / 2, 0), new Vector3(0.03f, s.y, s.z), dark);
                    break;
                case "mess_table":
                {
                    // Waist-high "messy pile" work table: exact footprint so spawned items always land on the top.
                    Prim.Box(t, "top", new Vector3(0, s.y - 0.02f, 0), new Vector3(s.x, 0.04f, s.z), wood);
                    Prim.Box(t, "runner", new Vector3(0, s.y + 0.001f, 0), new Vector3(s.x * 0.92f, 0.003f, s.z * 0.55f), Mats.Lit(Palette.Teal), false);
                    for (int i = 0; i < 4; i++)
                    {
                        float lx = (i % 2 == 0 ? -1 : 1) * (s.x / 2 - 0.05f), lz = (i < 2 ? -1 : 1) * (s.z / 2 - 0.05f);
                        Prim.Box(t, "leg", new Vector3(lx, (s.y - 0.04f) / 2, lz), new Vector3(0.05f, s.y - 0.04f, 0.05f), dark);
                    }
                    break;
                }
                case "towel_rack":
                    Prim.Box(t, "postL", new Vector3(-s.x / 2, s.y / 2, 0), new Vector3(0.04f, s.y, 0.04f), Mats.Lit(Palette.Cream));
                    Prim.Box(t, "postR", new Vector3(s.x / 2, s.y / 2, 0), new Vector3(0.04f, s.y, 0.04f), Mats.Lit(Palette.Cream));
                    Prim.Box(t, "bar", new Vector3(0, 0.97f, 0.05f), new Vector3(s.x, 0.03f, 0.12f), Mats.Lit(Palette.Cream));
                    Prim.Box(t, "foot", new Vector3(0, 0.02f, 0), new Vector3(s.x, 0.04f, 0.3f), Mats.Lit(Palette.Cream));
                    break;
                case "dog_bed":
                case "cat_bed":
                {
                    var c = kind == "dog_bed" ? Palette.Teal : Palette.Purple;
                    Prim.Shape(PrimitiveType.Cylinder, t, "ring", new Vector3(0, 0.07f, 0), new Vector3(s.x, 0.07f, s.z), Mats.Lit(c), true);
                    Prim.Shape(PrimitiveType.Cylinder, t, "cushion", new Vector3(0, 0.09f, 0), new Vector3(s.x * 0.8f, 0.03f, s.z * 0.8f), Mats.Lit(Palette.Cream), false);
                    break;
                }
                case "pet_mat":
                case "shoe_mat":
                    Prim.Box(t, "mat", new Vector3(0, 0.005f, 0), new Vector3(s.x, 0.01f, s.z), Mats.Lit(kind == "pet_mat" ? Palette.Mint : Palette.Purple), false);
                    break;
                case "wall_cupboard":
                    Prim.Box(t, "body", new Vector3(0, s.y / 2, 0), new Vector3(s.x, s.y, s.z), Mats.Lit(Palette.Cream));
                    Prim.Box(t, "mugshelf", new Vector3(0, -0.16f, 0.02f), new Vector3(s.x, 0.03f, 0.25f), wood);
                    break;
                case "tv_stand":
                    Prim.Box(t, "stand", new Vector3(0, s.y / 2, 0), s, dark);
                    Prim.Box(t, "tv", new Vector3(0, s.y + 0.35f, -0.1f), new Vector3(s.x * 0.8f, 0.6f, 0.05f), Mats.Lit(Palette.Ink));
                    break;
                default:
                    Prim.Box(t, kind, new Vector3(0, Mathf.Max(0.01f, s.y) / 2, 0), new Vector3(s.x, Mathf.Max(0.01f, s.y), s.z), wood);
                    break;
            }
        }

        public static Vector3 V(SHHH.Core.Vec3 v) => new Vector3(v.x, v.y, v.z);
        public static SHHH.Core.Vec3 C(Vector3 v) => new SHHH.Core.Vec3(v.x, v.y, v.z);
    }
}
