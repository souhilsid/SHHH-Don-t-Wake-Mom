using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V6 VIS-G5 environment pass (Ref C "lived-in cosy interior"): Astra V2 architecture/decor around the gameplay shell -
    /// wainscot + crown moulding, exposed ceiling beams, a pendant lamp, framed pictures, a wall clock, sconces, plants,
    /// a crooked window with curtains, Mom's door frame + sleep plaque, plus room-specific dressing (kitchen racks,
    /// laundry rail/mop/bucket). Pure decoration: no colliders, not navigable obstacles, never gameplay-relevant.
    /// Floor pieces are only placed where no gameplay furniture/spawn is within reach (checked against the room definition).
    /// Budget: shared meshes/materials, no extra realtime lights.
    /// </summary>
    public static class RoomDressing
    {
        public static int Placed { get; private set; }

        public static void Build(RoomDefinition def, Transform root)
        {
            Placed = 0;
            var d = new GameObject("Dressing").transform; d.SetParent(root, false);
            float W = def.Width, D = def.Depth, H = def.Height;
            Wainscot(d, W, D, H, def.Id);

            // ceiling: beams across the room + one pendant over the middle
            for (int i = -1; i <= 1; i++) Put(d, "a2_011_exposed_ceiling_beam", new Vector3(0, H - 0.1f, i * D * 0.3f), 0f, W - 0.1f, fit: Fit.Longest);
            Put(d, "a2_028_pendant_light", new Vector3(0, H - 0.02f, -0.3f), 0f, 0.6f, fit: Fit.Height, hang: true);

            // south wall (behind the spawns): two framed pictures + two sconces
            Put(d, "a2_040_wall_art_frame", new Vector3(-1.4f, 1.75f, -D / 2 + 0.03f), 0f, 0.55f, fit: Fit.Height);
            Put(d, "a2_040_wall_art_frame", new Vector3(1.4f, 1.8f, -D / 2 + 0.03f), 0f, 0.45f, fit: Fit.Height);
            Put(d, "a2_026_wall_sconce", new Vector3(-2.6f, 1.95f, -D / 2 + 0.05f), 0f, 0.35f, fit: Fit.Height);
            Put(d, "a2_026_wall_sconce", new Vector3(2.6f, 1.95f, -D / 2 + 0.05f), 0f, 0.35f, fit: Fit.Height);
            // west wall: clock + postcard; east wall: crooked window with curtains replaces the plain frame
            Put(d, "a2_041_wall_clock", new Vector3(-W / 2 + 0.04f, 2.15f, -D * 0.28f), 90f, 0.38f, fit: Fit.Height);
            Put(d, "a2_398_wall_postcard", new Vector3(-W / 2 + 0.03f, 1.7f, D * 0.32f), 90f, 0.22f, fit: Fit.Height);
            Put(d, "a2_016_crooked_rectangular_window", new Vector3(W / 2 - 0.04f, 0.95f, -0.6f), -90f, 1.15f, fit: Fit.Height);
            Put(d, "a2_020_window_curtains", new Vector3(W / 2 - 0.12f, 0.85f, -0.6f), -90f, 1.7f, fit: Fit.Height);
            // Mom's door: real frame + sleepy plaque on the north wall (the hinged panel stays the gameplay door)
            Put(d, "a2_024_door_frame", new Vector3(0, 0f, D / 2 - 0.02f), 180f, 2.3f, fit: Fit.Height);
            Put(d, "a2_382_mom_sleep_door_plaque", new Vector3(0.75f, 1.62f, D / 2 - 0.03f), 180f, 0.22f, fit: Fit.Height);
            Put(d, "a2_030_wall_light_switch", new Vector3(-0.75f, 1.15f, D / 2 - 0.02f), 180f, 0.12f, fit: Fit.Height);
            Put(d, "a2_033_household_ventilation_pipe", new Vector3(-W / 2 + 0.08f, H - 0.85f, D / 2 - 0.4f), 90f, 0.8f, fit: Fit.Height);

            // floor plants only in free corners
            FloorIfFree(def, d, "a2_039_large_potted_plant", new Vector3(-W / 2 + 0.38f, 0, -D / 2 + 0.38f), 1.0f);
            FloorIfFree(def, d, "a2_039_large_potted_plant", new Vector3(W / 2 - 0.38f, 0, D / 2 - 0.38f), 0.9f);
            FloorIfFree(def, d, "a2_039_large_potted_plant", new Vector3(-W / 2 + 0.38f, 0, D / 2 - 0.38f), 0.85f);
            FloorIfFree(def, d, "a2_043_woven_rug_circular", new Vector3(0, 0.004f, -D / 2 + 1.2f), 1.6f, Fit.Longest, 0.55f);

            switch (def.Id)
            {
                case RoomId.LivingRoom:
                    Put(d, "a2_045_wall_shelf_straight", new Vector3(-W / 2 + 0.12f, 1.85f, D * 0.08f), 90f, 1.0f, fit: Fit.Longest);
                    Put(d, "a2_038_small_leafy_plant", new Vector3(-W / 2 + 0.12f, 1.9f, D * 0.08f - 0.3f), 0f, 0.28f, fit: Fit.Height);
                    Put(d, "a2_121_wobbly_stack_of_books", new Vector3(-W / 2 + 0.12f, 1.9f, D * 0.08f + 0.25f), 0f, 0.2f, fit: Fit.Height);
                    Put(d, "a2_092_coat_rack", new Vector3(-W / 2 + 0.3f, 0, -D / 2 + 1.2f), 0f, 1.7f, fit: Fit.Height, floorCheck: def);
                    break;
                case RoomId.Kitchen:
                    Put(d, "a2_152_spice_rack", new Vector3(-W / 2 + 0.04f, 1.65f, 0.4f), 90f, 0.3f, fit: Fit.Height);
                    Put(d, "a2_151_pan_rack", new Vector3(-W / 2 + 0.05f, 1.85f, -0.6f), 90f, 0.45f, fit: Fit.Height);
                    Put(d, "a2_178_fruit_bowl", new Vector3(W / 2 - 0.35f, 1.86f, D / 2 - 0.35f), 0f, 0.14f, fit: Fit.Height);
                    Put(d, "a2_158_kitchen_waste_bin", new Vector3(W / 2 - 0.3f, 0f, -D / 2 + 0.35f), 0f, 0.5f, fit: Fit.Height, floorCheck: def);
                    break;
                case RoomId.Laundry:
                    Put(d, "a2_210_hanging_rail", new Vector3(0.6f, 2.2f, -D / 2 + 0.25f), 0f, 1.4f, fit: Fit.Longest);
                    Put(d, "a2_233_shirt_on_hanger", new Vector3(0.3f, 1.75f, -D / 2 + 0.25f), 0f, 0.6f, fit: Fit.Height);
                    Put(d, "a2_233_shirt_on_hanger", new Vector3(0.9f, 1.75f, -D / 2 + 0.25f), 0f, 0.6f, fit: Fit.Height);
                    Put(d, "a2_221_small_night_window", new Vector3(-W / 2 + 0.04f, 1.7f, 0.6f), 90f, 0.7f, fit: Fit.Height);
                    Put(d, "a2_242_mop", new Vector3(W / 2 - 0.2f, 0f, D / 2 - 0.5f), 0f, 1.25f, fit: Fit.Height, floorCheck: def);
                    Put(d, "a2_243_water_bucket", new Vector3(W / 2 - 0.42f, 0f, D / 2 - 0.62f), 0f, 0.32f, fit: Fit.Height, floorCheck: def);
                    break;
            }
            Debug.Log($"[SHHH] dressing {def.Title}: {Placed} Astra pieces");
        }

        enum Fit { Height, Longest }

        static void Wainscot(Transform d, float W, float D, float H, RoomId id)
        {
            var panel = Mats.Lit(id == RoomId.Kitchen ? Palette.Hex(0x7FB7B0) : id == RoomId.Laundry ? Palette.Hex(0x8FA9D6) : Palette.Hex(0x9C6B4E), 0.2f);
            var rail = Mats.Lit(Palette.Cream, 0.3f);
            const float h = 0.95f;
            void Wall(Vector3 c, Vector3 size, Vector3 railSize)
            {
                Prim.Box(d, "wainscot", c, size, panel, false);
                Prim.Box(d, "rail", new Vector3(c.x, h, c.z), railSize, rail, false);
            }
            Wall(new Vector3(0, h / 2, -D / 2 + 0.012f), new Vector3(W, h, 0.02f), new Vector3(W, 0.05f, 0.05f));
            Wall(new Vector3(-W / 2 + 0.012f, h / 2, 0), new Vector3(0.02f, h, D), new Vector3(0.05f, 0.05f, D));
            Wall(new Vector3(W / 2 - 0.012f, h / 2, 0), new Vector3(0.02f, h, D), new Vector3(0.05f, 0.05f, D));
            float side = (W - 1.0f) / 2f;
            Wall(new Vector3(-0.5f - side / 2, h / 2, D / 2 - 0.012f), new Vector3(side, h, 0.02f), new Vector3(side, 0.05f, 0.05f));
            Wall(new Vector3(0.5f + side / 2, h / 2, D / 2 - 0.012f), new Vector3(side, h, 0.02f), new Vector3(side, 0.05f, 0.05f));
            // crown moulding
            Prim.Box(d, "crownS", new Vector3(0, H - 0.04f, -D / 2 + 0.03f), new Vector3(W, 0.08f, 0.06f), rail, false);
            Prim.Box(d, "crownN", new Vector3(0, H - 0.04f, D / 2 - 0.03f), new Vector3(W, 0.08f, 0.06f), rail, false);
            Prim.Box(d, "crownW", new Vector3(-W / 2 + 0.03f, H - 0.04f, 0), new Vector3(0.06f, 0.08f, D), rail, false);
            Prim.Box(d, "crownE", new Vector3(W / 2 - 0.03f, H - 0.04f, 0), new Vector3(0.06f, 0.08f, D), rail, false);
        }

        static bool Free(RoomDefinition def, Vector3 p, float radius)
        {
            foreach (var f in def.Furniture)
            {
                var c = VisualFactory.V(f.Position);
                float hx = Mathf.Max(f.Size.x, f.Size.z) / 2f + radius;
                if (Mathf.Abs(c.x - p.x) < hx && Mathf.Abs(c.z - p.z) < hx) return false;
            }
            foreach (var s in def.SiblingSpawns) { var c = VisualFactory.V(s); if ((new Vector3(c.x, 0, c.z) - new Vector3(p.x, 0, p.z)).magnitude < radius + 0.6f) return false; }
            if ((VisualFactory.V(def.MomDoor) - p).magnitude < 1.2f) return false;
            return true;
        }

        static void FloorIfFree(RoomDefinition def, Transform d, string key, Vector3 p, float size, Fit fit = Fit.Height, float radius = 0.35f)
        {
            if (Free(def, p, radius)) Put(d, key, p, Random.Range(0f, 360f), size, fit: fit);
        }

        static void Put(Transform parent, string key, Vector3 pos, float yaw, float size, Fit fit = Fit.Height, bool hang = false, RoomDefinition floorCheck = null)
        {
            if (floorCheck != null && !Free(floorCheck, pos, 0.3f)) return;
            var model = ArtCatalog.I.Model(key); if (model == null) return;
            var v = VisualFactory.Normalise(model, size, parent, key, byHeight: fit == Fit.Height);
            v.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            v.transform.localPosition = hang ? pos - Vector3.up * size : pos;
            foreach (var r in v.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Placed++;
        }
    }
}
