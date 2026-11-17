using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V7 recipe prop and fixture visuals. Kinds with real art (V5 handcrafted, Astra A2, CC0) use it; the rest get a readable
    /// procedural placeholder sized from <see cref="RecipePropSize"/> (long side along local Z, narrow side along X). Every tagged
    /// item carries a colour + word sticker so sorting never relies on colour alone (colour-blind safe). Placeholders are
    /// replaced kind-by-kind in Gate 2 without touching gameplay.
    /// </summary>
    public static class RecipeVisuals
    {
        enum S { Box, Cyl, Sphere, Capsule }
        struct K { public S Shape; public Vector3 Size; public Color Color; public K(S s, float x, float y, float z, Color c) { Shape = s; Size = new Vector3(x, y, z); Color = c; } }

        static readonly Color Paper = Palette.Hex(0xF4ECD8), Plastic = Palette.Hex(0x7FB8E6), Fabric = Palette.Hex(0xC9A4D8), Ceramic = Palette.Hex(0xF2F0EA),
            Metal = Palette.Hex(0xB8C0C8), Food = Palette.Hex(0xE8B864), WoodC = Palette.WoodLight, Green = Palette.Hex(0x7CC66E);

        static readonly Dictionary<string, K> Table = new Dictionary<string, K>
        {
            ["toy_block"] = new K(S.Box, .06f, .06f, .06f, Palette.Yellow), ["game_pieces"] = new K(S.Box, .07f, .04f, .08f, Palette.Coral),
            ["game_lid"] = new K(S.Box, .24f, .035f, .25f, Palette.Teal), ["game_base"] = new K(S.Box, .23f, .05f, .24f, Palette.Cream),
            ["photo_frame"] = new K(S.Box, .03f, .2f, .25f, WoodC), ["popcorn_cluster"] = new K(S.Sphere, .06f, .05f, .06f, Palette.Cream),
            ["gamepad"] = new K(S.Box, .1f, .04f, .14f, Palette.Ink), ["gamepad_strap"] = new K(S.Box, .03f, .01f, .12f, Palette.Purple),
            ["squeaky_toy"] = new K(S.Capsule, .07f, .07f, .2f, Palette.Coral), ["coaster"] = new K(S.Cyl, .09f, .01f, .09f, WoodC),
            ["fruit_bowl"] = new K(S.Cyl, .2f, .07f, .2f, Ceramic), ["cutlery"] = new K(S.Box, .03f, .012f, .18f, Metal),
            ["jar"] = new K(S.Cyl, .08f, .11f, .08f, Plastic), ["apple"] = new K(S.Sphere, .075f, .075f, .075f, Palette.Hex(0xE0473C)),
            ["bread_bottom"] = new K(S.Box, .11f, .02f, .11f, Food), ["bread_top"] = new K(S.Box, .11f, .02f, .11f, Food),
            ["cheese"] = new K(S.Box, .1f, .006f, .1f, Palette.Yellow), ["lettuce"] = new K(S.Box, .1f, .006f, .1f, Green),
            ["recipe_card"] = new K(S.Box, .1f, .004f, .14f, Paper), ["cereal_jar"] = new K(S.Cyl, .1f, .18f, .1f, Plastic),
            ["jar_lid"] = new K(S.Cyl, .1f, .02f, .1f, Palette.Coral), ["cloth"] = new K(S.Box, .15f, .012f, .15f, Palette.Teal),
            ["kitchen_towel"] = new K(S.Box, .18f, .015f, .26f, Palette.Cream), ["placemat"] = new K(S.Box, .2f, .006f, .28f, Palette.Purple),
            ["kid_plate"] = new K(S.Cyl, .2f, .02f, .2f, Palette.Mint), ["kid_cup"] = new K(S.Cyl, .075f, .1f, .075f, Palette.Yellow),
            ["mixer_whisk"] = new K(S.Capsule, .06f, .06f, .15f, Metal), ["toaster_cord"] = new K(S.Box, .03f, .02f, .12f, Palette.Ink),
            ["clothes"] = new K(S.Box, .2f, .05f, .25f, Fabric), ["bath_towel"] = new K(S.Box, .2f, .05f, .28f, Palette.Teal),
            ["laundry_bundle"] = new K(S.Sphere, .22f, .14f, .25f, Fabric), ["wet_bundle"] = new K(S.Sphere, .22f, .14f, .25f, Palette.Hex(0x6E8FB8)),
            ["hanger"] = new K(S.Box, .03f, .14f, .3f, Plastic), ["storage_bin"] = new K(S.Box, .22f, .14f, .25f, Plastic),
            ["bin_lid"] = new K(S.Box, .22f, .015f, .25f, Palette.Teal), ["folded_towel"] = new K(S.Box, .18f, .05f, .2f, Palette.Cream),
            ["lint_cluster"] = new K(S.Sphere, .06f, .03f, .07f, Palette.Hex(0xB0A8B8)), ["empty_bottle"] = new K(S.Cyl, .07f, .2f, .07f, Plastic),
            ["magazine"] = new K(S.Box, .21f, .01f, .28f, Palette.Purple), ["sock"] = new K(S.Box, .07f, .03f, .16f, Palette.Coral),
            ["envelope"] = new K(S.Box, .11f, .005f, .22f, Paper), ["drawing"] = new K(S.Box, .2f, .004f, .28f, Paper),
            ["notebook"] = new K(S.Box, .17f, .015f, .23f, Palette.Teal), ["marker"] = new K(S.Cyl, .02f, .13f, .02f, Palette.Coral),
            ["toothbrush"] = new K(S.Box, .02f, .02f, .18f, Plastic), ["slipper"] = new K(S.Box, .1f, .05f, .24f, Fabric),
            ["family_shoe"] = new K(S.Box, .1f, .08f, .26f, Palette.Hex(0x5B4A6E)), ["umbrella"] = new K(S.Capsule, .07f, .07f, .3f, Palette.Navy),
            ["paper_roll"] = new K(S.Cyl, .11f, .1f, .11f, Paper), ["pet_toy"] = new K(S.Sphere, .07f, .07f, .07f, Palette.Coral),
            ["collar"] = new K(S.Cyl, .1f, .015f, .1f, Palette.Coral), ["collar_charm"] = new K(S.Cyl, .03f, .006f, .03f, Palette.Yellow),
            ["food_can"] = new K(S.Cyl, .08f, .1f, .08f, Metal), ["grocery"] = new K(S.Box, .14f, .1f, .2f, Food),
            ["hand_towel"] = new K(S.Box, .14f, .03f, .2f, Palette.Mint), ["bath_toy"] = new K(S.Sphere, .08f, .07f, .1f, Palette.Yellow),
            ["mirror_cloth"] = new K(S.Box, .12f, .01f, .12f, Palette.Moon), ["duster"] = new K(S.Capsule, .05f, .05f, .28f, Palette.Purple),
            ["hook_item"] = new K(S.Box, .16f, .05f, .25f, Fabric), ["ledge_item"] = new K(S.Cyl, .07f, .14f, .07f, Plastic),
            ["rocket_base"] = new K(S.Cyl, .1f, .08f, .1f, Palette.Coral), ["rocket_top"] = new K(S.Cyl, .07f, .12f, .07f, Palette.Moon),
            ["stray_toy"] = new K(S.Box, .08f, .08f, .1f, Palette.Mint), ["pot_lid"] = new K(S.Cyl, .2f, .02f, .2f, Metal),
            ["cat_bowl"] = new K(S.Cyl, .14f, .05f, .14f, Palette.Moon), ["dog_bowl"] = new K(S.Cyl, .18f, .06f, .18f, Palette.Teal),
            ["pet_dish"] = new K(S.Cyl, .14f, .04f, .14f, Palette.Mint), ["backpack"] = new K(S.Box, .14f, .3f, .26f, Palette.Teal),
            ["coat"] = new K(S.Box, .1f, .3f, .35f, Fabric), ["shopping_bag"] = new K(S.Box, .14f, .25f, .3f, Paper),
            ["plush"] = new K(S.Sphere, .16f, .18f, .16f, Palette.Hex(0xD9A07A)), ["pillow"] = new K(S.Box, .3f, .1f, .42f, Palette.Cream),
            ["pet_blanket"] = new K(S.Box, .25f, .04f, .3f, Palette.Purple), ["pet_cushion"] = new K(S.Cyl, .35f, .08f, .35f, Palette.Coral),
            ["dirty_clothes"] = new K(S.Sphere, .22f, .1f, .25f, Fabric), ["floor_runner"] = new K(S.Box, .3f, .008f, .6f, Palette.Coral),
            ["welcome_mat"] = new K(S.Box, .35f, .01f, .55f, Palette.WoodLight), ["throw"] = new K(S.Box, .28f, .05f, .35f, Palette.Teal),
            ["dining_chair"] = new K(S.Box, .42f, .85f, .45f, Palette.Wood), ["shirt"] = new K(S.Box, .32f, .04f, .46f, Palette.Yellow),
        };

        /// <summary>Colour for a tag word (null when the tag is not a colour).</summary>
        /// <summary>V12 (owner 2026-11-10: "clothes are not look clothes"): laundry is real garment geometry made by cloth simulation
        /// (Tools/blender/make_clothes.py - a T-shirt and a towel wrapped round a rolled core; a crumpled T-shirt) with painted fabric and
        /// soft secondary motion (<see cref="SoftCloth"/>). Null = not a garment kind (or the model is missing: old placeholder).</summary>
        static GameObject Clothes(string kind, string tag, Transform parent)
        {
            string key = kind == "laundry_bundle" || kind == "wet_bundle" || kind == "dirty_clothes" ? "ld_laundry_clothes_bundle" : kind == "clothes" ? "ld_laundry_clothes_shirt" : null;
            if (key == null) return null;
            var model = ArtCatalog.I != null ? ArtCatalog.I.Model(key) : null; if (model == null) return null;
            bool wet = kind == "wet_bundle";
            var v = new GameObject("Visual_" + kind); v.transform.SetParent(parent, false);
            var inst = Object.Instantiate(model, v.transform); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Prim.Remove(c);
            var tc = TagColor(tag);
            Color shirt = tc ?? (kind == "dirty_clothes" ? new Color(0.62f, 0.66f, 0.42f) : new Color(0.98f, 0.8f, 0.36f));
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                string n = r.name;
                r.sharedMaterial = n.Contains("Towel") ? FabricLook.Get(FabricLook.Pattern.Terry, tc.HasValue ? Color.Lerp(tc.Value, Color.white, 0.3f) : new Color(0.38f, 0.74f, 0.8f), wet)
                    : n.Contains("CORE") ? FabricLook.Get(FabricLook.Pattern.Knit, tc.HasValue ? Color.Lerp(tc.Value, Color.black, 0.2f) : new Color(0.86f, 0.55f, 0.6f), wet)
                    : FabricLook.Get(FabricLook.Pattern.Stripes, shirt, wet);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // fit: largest horizontal extent to the kind's size, resting on y = 0
            var b = Prim.LocalRendererBounds(v, parent); float ext = Mathf.Max(b.size.x, b.size.z);
            float want = kind == "clothes" ? 0.3f : 0.22f;
            if (ext > 1e-4f) inst.transform.localScale = Vector3.one * (want / ext);
            b = Prim.LocalRendererBounds(v, parent); inst.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            if (parent.GetComponent<SoftCloth>() == null) parent.gameObject.AddComponent<SoftCloth>();
            return v;
        }

        public static Color? TagColor(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            switch (tag)
            {
                case "red": case "red_stripe": return Palette.Hex(0xE8534A);
                case "blue": case "blue_stripe": return Palette.Hex(0x4A8FE8);
                case "green": return Palette.Hex(0x4DBA74);
                case "yellow": return Palette.Yellow;
                case "pink": return Palette.Hex(0xF59AC0);
                case "purple": return Palette.Purple;
                case "light": return Palette.Cream;
                case "dark": return Palette.Hex(0x3A3860);
                case "bruised": return Palette.Hex(0x9A6A3A);
            }
            int name = System.Array.IndexOf(new[] { "pip", "nell", "rory", "tess" }, tag);
            return name >= 0 ? Palette.Siblings[name] : (Color?)null;
        }

        static float TagScale(string tag) => tag == "small" ? 0.8f : tag == "big" || tag == "large" ? 1.15f : 1f;

        static readonly HashSet<string> LiesDown = new HashSet<string> { "sock", "clothes", "shirt", "pyjamas", "towel", "bath_towel", "folded_towel", "pencil", "crayon", "toothbrush" };

        /// <summary>Visual for a recipe (or any) prop. Returns the visual root parented under <paramref name="parent"/>.</summary>
        /// <summary>V13 KT17 groceries: the stylized pizza / burger / juice / apple (Tools/blender/make_fridge_food.py), modelled at real size,
        /// resting on y = 0. The food itself says what it is (no colour sticker).</summary>
        static GameObject Grocery(string kind, string tag, Transform parent)
        {
            if (kind != "grocery" || string.IsNullOrEmpty(tag)) return null;
            var model = ArtCatalog.I != null ? ArtCatalog.I.Model("sk_food_" + tag) : null; if (model == null) return null;
            var v = new GameObject("Visual_" + kind + "_" + tag); v.transform.SetParent(parent, false);
            var inst = Object.Instantiate(model, v.transform); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Prim.Remove(c);
            Mats.MakeUrpSafe(inst);
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = Prim.LocalRendererBounds(v, parent); inst.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            return v;
        }

        public static GameObject BuildProp(string kind, string tag, Transform parent, bool recipe = false)
        {
            GameObject v;
            var cloth = Clothes(kind, tag, parent); if (cloth != null) return cloth;   // V12 real garments
            var food = Grocery(kind, tag, parent); if (food != null) return food;      // V13 stylized food (KT17)
            if (VisualFactory.HasArt(kind) || !Table.TryGetValue(kind, out var k))
            {
                v = VisualFactory.BuildProp(kind, parent);
                // recipe layout assumes the long side runs along local Z (toward the player); authored art may lie along X
                if (recipe)
                {
                    var b = Prim.LocalRendererBounds(v, parent);
                    // flat things (magazines, plates, mats...) must lie flat when "upright": thinnest side along Y
                    // ...and soft things that rest on their side (a sock modelled standing up would otherwise stand like a rod)
                    if ((RecipePropSize.IsFlat(kind) || LiesDown.Contains(kind)) && (b.size.y > b.size.x * 1.15f || b.size.y > b.size.z * 1.15f))
                    {
                        var axis = b.size.x < b.size.z ? parent.forward : parent.right; // tip the tall side down onto the thin side's axis
                        v.transform.RotateAround(parent.TransformPoint(b.center), axis, 90f);
                        b = Prim.LocalRendererBounds(v, parent);
                        v.transform.position += parent.TransformVector(new Vector3(0, -b.min.y, 0)); // pivot back to the bottom
                        b = Prim.LocalRendererBounds(v, parent);
                    }
                    if (b.size.x > b.size.z * 1.15f) { v.transform.RotateAround(parent.TransformPoint(new Vector3(b.center.x, 0, b.center.z)), parent.up, 90f); }
                }
            }
            else
            {
                v = new GameObject("Visual_" + kind); v.transform.SetParent(parent, false);
                var c = TagColor(tag) ?? k.Color; var size = k.Size;
                var mat = Mats.Lit(c);
                switch (k.Shape)
                {
                    case S.Box: Prim.Box(v.transform, kind, new Vector3(0, size.y * 0.5f, 0), size, mat, false); break;
                    case S.Cyl: Prim.Shape(PrimitiveType.Cylinder, v.transform, kind, new Vector3(0, size.y * 0.5f, 0), new Vector3(size.x, size.y * 0.5f, size.z), mat); break;
                    case S.Sphere: Prim.Shape(PrimitiveType.Sphere, v.transform, kind, new Vector3(0, size.y * 0.5f, 0), size, mat); break;
                    case S.Capsule:
                        var cap = Prim.Shape(PrimitiveType.Capsule, v.transform, kind, new Vector3(0, size.y * 0.5f, 0), new Vector3(size.x, size.z * 0.5f, size.y), mat);
                        cap.transform.localRotation = Quaternion.Euler(90, 0, 0); break;
                }
                Detail(kind, v.transform, size);
            }
            float sc = TagScale(tag); if (sc != 1f) v.transform.localScale *= sc;
            if (!string.IsNullOrEmpty(tag) && tag != "small" && tag != "big" && tag != "large") Sticker(v.transform, tag, TopOf(v, parent) + 0.002f);
            return v;
        }

        static void Detail(string kind, Transform t, Vector3 s)
        {
            switch (kind)
            {
                case "jar": case "cereal_jar": Prim.Shape(PrimitiveType.Cylinder, t, "lid", new Vector3(0, s.y + 0.008f, 0), new Vector3(s.x * 1.02f, 0.008f, s.z * 1.02f), Mats.Lit(Palette.Cream)); break;
                case "game_lid": case "game_base": Prim.Box(t, "band", new Vector3(0, s.y * 0.5f, 0), new Vector3(s.x * 1.02f, s.y * 0.25f, s.z * 1.02f), Mats.Lit(Palette.Yellow), false); break;
                case "photo_frame": Prim.Box(t, "photo", new Vector3(-s.x * 0.52f, s.y * 0.5f, 0), new Vector3(0.002f, s.y * 0.75f, s.z * 0.75f), Mats.Lit(Palette.Moon), false); break;
                case "gamepad": Prim.Box(t, "btns", new Vector3(0, s.y + 0.003f, 0.03f), new Vector3(0.04f, 0.006f, 0.02f), Mats.Lit(Palette.Coral), false); break;
                case "kid_cup": case "empty_bottle": Prim.Shape(PrimitiveType.Cylinder, t, "rim", new Vector3(0, s.y, 0), new Vector3(s.x * 0.6f, 0.006f, s.z * 0.6f), Mats.Lit(Palette.Ink)); break;
                case "storage_bin": Prim.Box(t, "hole", new Vector3(0, s.y + 0.001f, 0), new Vector3(s.x * 0.85f, 0.002f, s.z * 0.85f), Mats.Lit(Palette.Ink), false); break;
                case "notebook": case "envelope": Prim.Box(t, "label", new Vector3(0, s.y + 0.001f, s.z * 0.2f), new Vector3(s.x * 0.6f, 0.002f, s.z * 0.2f), Mats.Lit(Palette.Cream), false); break;
            }
        }

        static float TopOf(GameObject v, Transform parent)
        {
            var b = Prim.LocalRendererBounds(v, parent);
            return b.size.sqrMagnitude > 0 ? b.max.y : 0.05f;
        }

        /// <summary>Round sticker on top of the item: tag colour (if any) + the tag word, readable from above at arm's length.</summary>
        public static Transform Sticker(Transform parent, string tag, float top, float diameter = 0.05f)
        {
            var root = new GameObject("tag_" + tag).transform; root.SetParent(parent, false); root.localPosition = new Vector3(0, top, 0);
            var c = TagColor(tag) ?? Palette.Cream;
            Prim.Shape(PrimitiveType.Cylinder, root, "disc", Vector3.zero, new Vector3(diameter, 0.001f, diameter), Mats.Unlit(c));
            var txt = WorldText.Make(root, "word", Pretty(tag), new Vector3(0, 0.0015f, 0), 0.11f, c.grayscale > 0.55f ? Palette.Navy : Palette.Cream, TextAlignmentOptions.Center, diameter * 1.6f);
            txt.transform.localRotation = Quaternion.Euler(90, 180, 0); // faces up, reads from the player side (local +Z = toward the player)
            txt.outlineWidth = 0f;
            return root;
        }

        public static string Pretty(string tag) => string.IsNullOrEmpty(tag) ? "" : char.ToUpperInvariant(tag[0]) + tag.Substring(1).Replace('_', ' ');
        public static string PrettyKind(string kind) => Pretty(kind);
    }
}
