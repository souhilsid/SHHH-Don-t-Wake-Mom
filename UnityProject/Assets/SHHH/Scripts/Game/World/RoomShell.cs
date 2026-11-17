using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Builds a cosy night-time room shell: floor, walls with baseboards, ceiling, Mom's bedroom door (hinged, warm light
    /// leaking underneath) with a short hallway behind it so she can physically walk in, a moonlit window, and lights tuned
    /// for Quest 2 (one unshadowed moon light + two warm lamps, no realtime shadows).
    /// </summary>
    public static class RoomShell
    {
        public sealed class Result { public Transform Door; public Light DoorLight; public Vector3 MomOffstage, MomDoorway; public Bounds PlayBounds; }

        public static Result Build(RoomDefinition def, Transform root)
        {
            var res = new Result();
            float W = def.Width, D = def.Depth, H = def.Height;
            Color wall = def.Id == RoomId.Kitchen ? Palette.WallKitchen : def.Id == RoomId.Laundry ? Palette.WallLaundry : Palette.WallLiving;
            Color floor = def.Id == RoomId.LivingRoom || def.Id == RoomId.Practice ? Palette.Floor : Palette.Tile;
            var env = new GameObject("Shell").transform; env.SetParent(root, false);
            Prim.Box(env, "floor", new Vector3(0, -0.05f, 0), new Vector3(W, 0.1f, D), Mats.Lit(floor, 0.25f));
            // floor planks/tiles pattern (thin unlit strips: cheap texture-free detail)
            for (float x = -W / 2 + 0.5f; x < W / 2; x += 0.5f)
                Prim.Box(env, "seam", new Vector3(x, 0.0005f, 0), new Vector3(0.008f, 0.001f, D), Mats.Lit(floor * 0.82f), false);
            Prim.Box(env, "ceiling", new Vector3(0, H + 0.05f, 0), new Vector3(W, 0.1f, D + 2f), Mats.Lit(wall * 0.6f));
            var wm = Mats.Lit(wall, 0.1f); var trim = Mats.Lit(Palette.Cream);
            // South, west, east walls
            Prim.Box(env, "wallS", new Vector3(0, H / 2, -D / 2 - 0.05f), new Vector3(W + 0.2f, H, 0.1f), wm);
            Prim.Box(env, "wallW", new Vector3(-W / 2 - 0.05f, H / 2, 0), new Vector3(0.1f, H, D), wm);
            Prim.Box(env, "wallE", new Vector3(W / 2 + 0.05f, H / 2, 0), new Vector3(0.1f, H, D), wm);
            // North wall with Mom's door opening (1.0 x 2.1) at x = 0
            float doorW = 1.0f, doorH = 2.1f, side = (W - doorW) / 2f;
            Prim.Box(env, "wallN_L", new Vector3(-doorW / 2 - side / 2, H / 2, D / 2 + 0.05f), new Vector3(side, H, 0.1f), wm);
            Prim.Box(env, "wallN_R", new Vector3(doorW / 2 + side / 2, H / 2, D / 2 + 0.05f), new Vector3(side, H, 0.1f), wm);
            Prim.Box(env, "wallN_top", new Vector3(0, (doorH + H) / 2, D / 2 + 0.05f), new Vector3(doorW, H - doorH, 0.1f), wm);
            // baseboards
            Prim.Box(env, "baseS", new Vector3(0, 0.05f, -D / 2 + 0.005f), new Vector3(W, 0.1f, 0.012f), trim, false);
            Prim.Box(env, "baseW", new Vector3(-W / 2 + 0.005f, 0.05f, 0), new Vector3(0.012f, 0.1f, D), trim, false);
            Prim.Box(env, "baseE", new Vector3(W / 2 - 0.005f, 0.05f, 0), new Vector3(0.012f, 0.1f, D), trim, false);
            // door frame + hinged door panel (panel excluded from NavMesh baking: layer UI)
            Prim.Box(env, "frameL", new Vector3(-doorW / 2 - 0.04f, doorH / 2, D / 2 - 0.01f), new Vector3(0.08f, doorH, 0.14f), trim, false);
            Prim.Box(env, "frameR", new Vector3(doorW / 2 + 0.04f, doorH / 2, D / 2 - 0.01f), new Vector3(0.08f, doorH, 0.14f), trim, false);
            Prim.Box(env, "frameT", new Vector3(0, doorH + 0.04f, D / 2 - 0.01f), new Vector3(doorW + 0.16f, 0.08f, 0.14f), trim, false);
            var hinge = new GameObject("DoorHinge").transform; hinge.SetParent(env, false); hinge.localPosition = new Vector3(-doorW / 2, 0, D / 2 + 0.02f);
            var panel = Prim.Box(hinge, "door", new Vector3(doorW / 2, doorH / 2, 0), new Vector3(doorW - 0.02f, doorH - 0.02f, 0.045f), Mats.Lit(Palette.Cream, 0.3f));
            panel.layer = Layers.UI;
            Prim.Shape(PrimitiveType.Sphere, panel.transform, "knob", new Vector3(0.4f, -0.02f, -0.8f), new Vector3(0.05f, 0.025f, 1.2f), Mats.Lit(Palette.Yellow, 0.8f));
            var sign = WorldText.Make(panel.transform, "sign", "MOM'S ROOM\n<size=60%>shhh!</size>", new Vector3(0, 0.22f, -0.7f), 0.6f, Palette.Purple, TMPro.TextAlignmentOptions.Center, 0.8f);
            sign.transform.localScale = new Vector3(1f / (doorW - 0.02f), 1f / (doorH - 0.02f), 1f / 0.045f);
            sign.outlineWidth = 0f;
            res.Door = hinge;
            // Warm light strip under the door (emissive) and the bedroom hallway behind it
            Prim.Box(env, "underDoorGlow", new Vector3(0, 0.006f, D / 2 + 0.02f), new Vector3(doorW - 0.05f, 0.012f, 0.06f), Mats.Unlit(Palette.Yellow), false);
            var hall = new GameObject("MomHallway").transform; hall.SetParent(env, false); hall.localPosition = new Vector3(0, 0, D / 2 + 0.9f);
            Prim.Box(hall, "hallFloor", new Vector3(0, -0.05f, 0), new Vector3(1.6f, 0.1f, 1.7f), Mats.Lit(Palette.Floor * 0.8f));
            Prim.Box(hall, "hallL", new Vector3(-0.85f, H / 2, 0), new Vector3(0.1f, H, 1.7f), Mats.Lit(Palette.Purple * 0.7f));
            Prim.Box(hall, "hallR", new Vector3(0.85f, H / 2, 0), new Vector3(0.1f, H, 1.7f), Mats.Lit(Palette.Purple * 0.7f));
            Prim.Box(hall, "hallBack", new Vector3(0, H / 2, 0.85f), new Vector3(1.6f, H, 0.1f), Mats.Lit(Palette.Purple * 0.6f));
            var dl = new GameObject("DoorLight").AddComponent<Light>(); dl.transform.SetParent(hall, false); dl.transform.localPosition = new Vector3(0, 1.9f, 0.2f);
            dl.type = LightType.Point; dl.color = new Color(1f, 0.82f, 0.55f); dl.range = 4.5f; dl.intensity = 0.3f; dl.shadows = LightShadows.None;
            res.DoorLight = dl;
            res.MomOffstage = root.TransformPoint(new Vector3(0, 0, D / 2 + 1.2f));
            res.MomDoorway = root.TransformPoint(new Vector3(0, 0, D / 2 - 0.4f));
            // Moonlit window on the east wall
            var win = new GameObject("Window").transform; win.SetParent(env, false); win.localPosition = new Vector3(W / 2 - 0.01f, 1.5f, -0.6f); win.localRotation = Quaternion.Euler(0, -90, 0);
            Prim.Box(win, "sky", new Vector3(0, 0, 0.02f), new Vector3(1.1f, 0.9f, 0.01f), Mats.Unlit(Palette.Hex(0x232766)), false);
            Prim.Shape(PrimitiveType.Sphere, win, "moon", new Vector3(0.25f, 0.18f, 0.015f), new Vector3(0.18f, 0.18f, 0.01f), Mats.Unlit(Palette.Moon));
            for (int i = 0; i < 6; i++) Prim.Shape(PrimitiveType.Sphere, win, "star", new Vector3(-0.45f + i * 0.15f, -0.1f + (i % 3) * 0.12f, 0.015f), Vector3.one * 0.015f, Mats.Unlit(Palette.Yellow));
            Prim.Box(win, "frameH", new Vector3(0, 0, 0), new Vector3(1.15f, 0.04f, 0.04f), trim, false);
            Prim.Box(win, "frameV", new Vector3(0, 0, 0), new Vector3(0.04f, 0.95f, 0.04f), trim, false);
            Prim.Box(win, "sill", new Vector3(0, -0.47f, -0.05f), new Vector3(1.2f, 0.04f, 0.12f), trim, false);
            // Lights: cool moon (directional, no shadows) + warm lamp glows. Ambient set by GameRoot.
            var moon = new GameObject("Moonlight").AddComponent<Light>(); moon.transform.SetParent(root, false);
            moon.type = LightType.Directional; moon.color = Palette.Hex(0xA9C2FF); moon.intensity = 0.55f; moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(40f, -60f, 0f);
            var lamp = new GameObject("LampLight").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(-W / 2 + 0.8f, 1.6f, D / 2 - 0.8f); lamp.type = LightType.Point; lamp.color = Palette.Hex(0xFFC27A); lamp.range = 7.5f; lamp.intensity = 1.6f; lamp.shadows = LightShadows.None;
            var fill = new GameObject("FillLight").AddComponent<Light>(); fill.transform.SetParent(root, false);
            fill.transform.localPosition = new Vector3(W / 2 - 1f, 2.2f, -D / 2 + 1f); fill.type = LightType.Point; fill.color = Palette.Hex(0xC9A7FF); fill.range = 8f; fill.intensity = 0.8f; fill.shadows = LightShadows.None;
            res.PlayBounds = new Bounds(root.TransformPoint(new Vector3(0, H / 2, 0.6f)), new Vector3(W + 0.6f, H + 2f, D + 3.5f));
            return res;
        }
    }
}
