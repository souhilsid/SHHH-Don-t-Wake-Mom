using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// 2026-11-09 Gate 3: the world outside the windows. A painted night-sky panorama skybox (DesignSources/UI_V8: original
    /// procedural painting, Krita master) plus a ring of low-poly cartoon neighbour houses with warm lit windows, trees, street lamps,
    /// a road and lawn, so every window shows real parallax geometry instead of a flat picture. Everything is merged into one mesh
    /// per material (≈6 draw calls) and unlit/toon, Quest-cheap. Deterministic (seeded) so every peer sees the same street.
    /// </summary>
    public static class NightExterior
    {
        static readonly Dictionary<Material, List<CombineInstance>> _batch = new Dictionary<Material, List<CombineInstance>>();
        static Mesh _cube, _prism, _cone, _sphere, _cyl;

        public static GameObject Build(Transform parent, Vector3 center, float innerRadius = 16f, int seed = 20261109)
        {
            ApplySky();
            var hs = parent != null ? parent.GetComponentInChildren<HouseAssembly>(true) : null; if (hs != null) OpenWindows(hs.transform);
            var root = new GameObject("NightExterior"); root.transform.SetParent(parent, false); root.transform.position = new Vector3(center.x, 0, center.z);
            _batch.Clear(); EnsureMeshes();
            var rng = new System.Random(seed);
            var lawn = Mats.Lit(Palette.Hex(0x2B3A4E), 0.05f); var road = Mats.Lit(Palette.Hex(0x3A3350), 0.1f); var line = Mats.Unlit(Palette.Hex(0xBFA66A));
            var litWin = Mats.Unlit(Palette.Hex(0xFFD36B)); var warmWin = Mats.Unlit(Palette.Hex(0xF5A867)); var darkWin = Mats.Lit(Palette.Hex(0x23183A), 0.6f);
            var trunk = Mats.Lit(Palette.Hex(0x4A3328)); var leaf = Mats.Lit(Palette.Hex(0x2F5B53)); var leaf2 = Mats.Lit(Palette.Hex(0x3E4F7A));
            var pole = Mats.Lit(Palette.Hex(0x2A2238)); var bulb = Mats.Unlit(Palette.Hex(0xFFF0BA)); var fence = Mats.Lit(Palette.Hex(0xD8C7E8));
            var walls = new[] { Palette.Hex(0x7E6AA8), Palette.Hex(0x5E7FA6), Palette.Hex(0xA8708E), Palette.Hex(0x6E9A92), Palette.Hex(0x9C86C4), Palette.Hex(0xB5869A) };
            var roofs = new[] { Palette.Hex(0x3B2A57), Palette.Hex(0x5A2E4C), Palette.Hex(0x2E3F5E), Palette.Hex(0x4B3A2E) };

            // ground: lawn disc + road ring + dashed centre line
            Add(_cyl, lawn, Matrix4x4.TRS(new Vector3(0, -0.06f, 0), Quaternion.identity, new Vector3(110f, 0.05f, 110f)));
            float rr = innerRadius - 2.6f;
            for (int i = 0; i < 64; i++)
            {
                float a0 = i * Mathf.PI * 2f / 64f, a1 = (i + 1) * Mathf.PI * 2f / 64f, am = (a0 + a1) / 2f;
                var mid = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am)) * rr; float len = 2f * rr * Mathf.Sin(Mathf.PI / 64f) + 0.06f;
                var rot = Quaternion.LookRotation(new Vector3(-Mathf.Sin(am), 0, Mathf.Cos(am)));
                Add(_cube, road, Matrix4x4.TRS(mid + Vector3.up * -0.02f, rot, new Vector3(3.2f, 0.04f, len)));
                if (i % 2 == 0) Add(_cube, line, Matrix4x4.TRS(mid + Vector3.up * 0.005f, rot, new Vector3(0.12f, 0.01f, len * 0.55f)));
            }
            // neighbour houses around the ring (gaps let the sky through), facing the family house
            int houses = 20;
            for (int i = 0; i < houses; i++)
            {
                float ang = (i + (float)rng.NextDouble() * 0.35f) * Mathf.PI * 2f / houses;
                float dist = innerRadius + 1.5f + (float)rng.NextDouble() * 5f;
                var pos = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * dist;
                var face = Quaternion.LookRotation(-pos.normalized, Vector3.up);
                float w = 4.2f + (float)rng.NextDouble() * 3f, d = 4f + (float)rng.NextDouble() * 2.5f;
                int floors = rng.NextDouble() < 0.55 ? 2 : 1; float h = floors * 2.6f + 0.3f;
                var wall = Mats.Lit(walls[rng.Next(walls.Length)], 0.1f); var roof = Mats.Lit(roofs[rng.Next(roofs.Length)], 0.15f);
                var T = Matrix4x4.TRS(pos, face, Vector3.one);
                Add(_cube, wall, T * Matrix4x4.TRS(new Vector3(0, h / 2f, 0), Quaternion.identity, new Vector3(w, h, d)));
                Add(_prism, roof, T * Matrix4x4.TRS(new Vector3(0, h, 0), Quaternion.identity, new Vector3(w + 0.5f, 1.4f + (float)rng.NextDouble() * 0.9f, d + 0.5f)));
                if (rng.NextDouble() < 0.6) Add(_cube, roof, T * Matrix4x4.TRS(new Vector3(w * 0.28f, h + 1.0f, d * 0.15f), Quaternion.identity, new Vector3(0.45f, 1.3f, 0.45f)));
                // front door + porch light
                Add(_cube, Mats.Lit(roofs[rng.Next(roofs.Length)]), T * Matrix4x4.TRS(new Vector3(-w * 0.18f, 1.0f, d / 2f + 0.02f), Quaternion.identity, new Vector3(0.9f, 2.0f, 0.06f)));
                Add(_sphere, bulb, T * Matrix4x4.TRS(new Vector3(-w * 0.18f + 0.65f, 2.0f, d / 2f + 0.1f), Quaternion.identity, Vector3.one * 0.16f));
                // windows on the side facing us (most lit warm, some dark: people asleep)
                for (int f = 0; f < floors; f++)
                    for (int k = 0; k < 3; k++)
                    {
                        float x = -w / 2f + w * (k + 0.5f) / 3f; if (f == 0 && k == 0) continue; // the door is there
                        double roll = rng.NextDouble(); var m = roll < 0.45 ? litWin : roll < 0.65 ? warmWin : darkWin;
                        var wp = new Vector3(x, 1.35f + f * 2.6f, d / 2f + 0.025f);
                        Add(_cube, fence, T * Matrix4x4.TRS(wp, Quaternion.identity, new Vector3(0.95f, 1.05f, 0.04f)));       // frame
                        Add(_cube, m, T * Matrix4x4.TRS(wp + new Vector3(0, 0, 0.012f), Quaternion.identity, new Vector3(0.8f, 0.9f, 0.04f)));
                    }
                // trees between houses
                var tp = new Vector3(Mathf.Cos(ang + 0.16f), 0, Mathf.Sin(ang + 0.16f)) * (dist - 2.2f);
                float th = 2.2f + (float)rng.NextDouble() * 2.2f;
                Add(_cyl, trunk, Matrix4x4.TRS(tp + Vector3.up * th * 0.25f, Quaternion.identity, new Vector3(0.28f, th * 0.25f, 0.28f)));
                if (rng.NextDouble() < 0.5) { Add(_cone, leaf, Matrix4x4.TRS(tp + Vector3.up * th * 0.45f, Quaternion.identity, new Vector3(1.9f, th * 0.75f, 1.9f))); Add(_cone, leaf, Matrix4x4.TRS(tp + Vector3.up * th * 0.8f, Quaternion.identity, new Vector3(1.4f, th * 0.6f, 1.4f))); }
                else Add(_sphere, rng.NextDouble() < 0.5 ? leaf : leaf2, Matrix4x4.TRS(tp + Vector3.up * th * 0.75f, Quaternion.identity, Vector3.one * (1.6f + th * 0.35f)));
                // street lamp every other house, on the road's outer edge
                if (i % 2 == 0)
                {
                    var lp = new Vector3(Mathf.Cos(ang - 0.12f), 0, Mathf.Sin(ang - 0.12f)) * (rr + 1.9f);
                    Add(_cyl, pole, Matrix4x4.TRS(lp + Vector3.up * 1.6f, Quaternion.identity, new Vector3(0.12f, 1.6f, 0.12f)));
                    Add(_sphere, bulb, Matrix4x4.TRS(lp + Vector3.up * 3.3f, Quaternion.identity, Vector3.one * 0.42f));
                }
            }
            // a short picket fence ring just outside the family house's walls (reads through ground-floor windows)
            for (int i = 0; i < 90; i++)
            {
                float a = i * Mathf.PI * 2f / 90f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (innerRadius - 5.2f);
                Add(_cube, fence, Matrix4x4.TRS(p + Vector3.up * 0.45f, Quaternion.LookRotation(p), new Vector3(0.1f, 0.9f, 0.05f)));
                if (i % 3 == 0) Add(_cube, fence, Matrix4x4.TRS(p + Vector3.up * 0.6f, Quaternion.LookRotation(new Vector3(-p.z, 0, p.x)), new Vector3(0.05f, 0.08f, 2.0f * Mathf.PI * (innerRadius - 5.2f) / 30f)));
            }
            Flush(root.transform);
            return root;
        }

        /// <summary>
        /// Replaces the house prefab's painted window backdrops (materials "*NightView*" / "*NightBlue*": a flat picture of a night
        /// street in every window) with faint glass, so the real sky and street are seen through the window openings.
        /// </summary>
        public static int OpenWindows(Transform house)
        {
            if (house == null) return 0;
            int n = 0;
            foreach (var r in house.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials; var hit = new List<int>();
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null && (mats[i].name.Contains("NightView") || mats[i].name.Contains("NightBlue"))) hit.Add(i);
                if (hit.Count == 0) continue;
                if (hit.Count == mats.Length)
                {
                    // window painted on a solid wall (kitchen, laundry, Mom's room): show the night panorama by view direction
                    var wv = Resources.Load<Material>("UI_V8/Mats/WindowView");
                    if (wv != null) { var nm = new Material[mats.Length]; for (int i = 0; i < nm.Length; i++) nm[i] = wv; r.sharedMaterials = nm; r.lightmapIndex = -1; }
                    else r.enabled = false;
                    n += hit.Count; continue;
                }
                // V12 (owner, Quest run 2026-11-11: "the house is not showing the walls, showing directly the outside"): this used to copy the
                // mesh and delete the pane triangles. The living room's walls + windows are ONE mesh (V5_LivingArchitecture.fbx, Read/Write
                // off): the Editor can still read it, the APK cannot - on the Quest the copy came out empty and every living-room wall
                // vanished. Now only the pane material slots change (faint glass): no CPU mesh access, the same in the Editor and on the Quest.
                var glassed = (Material[])mats.Clone();
                foreach (var i in hit) { glassed[i] = Glass(); n++; }
                r.sharedMaterials = glassed;
            }
            Debug.Log($"[SHHH] night exterior: {n} painted window backdrops replaced by glass / window view (real sky + street behind)");
            return n;
        }

        static Material _glass;
        /// <summary>Faint blue window glass (URP Unlit transparent - the UI's own material, so its shader variant is in every build).</summary>
        public static Material Glass()
        {
            if (_glass != null) return _glass;
            var src = Resources.Load<Material>("UI_V8/Mats/UiTransparent");
            _glass = src != null ? new Material(src) : Mats.Unlit(new Color(0.62f, 0.72f, 0.95f));
            _glass.name = "SHHH_WindowGlass";
            if (_glass.HasProperty("_BaseColor")) _glass.SetColor("_BaseColor", new Color(0.62f, 0.72f, 0.95f, 0.12f));
            return _glass;
        }

        /// <summary>Night-sky panorama skybox + camera clear to skybox; fog pushed out so the street reads through the windows.</summary>
        public static void ApplySky()
        {
            var sky = Resources.Load<Material>("UI_V8/Mats/SkyNight");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogEndDistance, 46f); RenderSettings.fogStartDistance = Mathf.Max(RenderSettings.fogStartDistance, 9f);
            RenderSettings.fogColor = Palette.Hex(0x2E1F4E);
            foreach (var cam in Camera.allCameras) if (cam != null && cam.cameraType == CameraType.Game && sky != null) cam.clearFlags = CameraClearFlags.Skybox;
        }

        static void Add(Mesh mesh, Material mat, Matrix4x4 m)
        {
            if (!_batch.TryGetValue(mat, out var l)) _batch[mat] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = mesh, transform = m });
        }

        static void Flush(Transform root)
        {
            foreach (var kv in _batch)
            {
                var g = new GameObject("ext_" + kv.Key.name); g.transform.SetParent(root, false);
                var mesh = new Mesh { name = g.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = kv.Key;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                g.isStatic = true;
            }
            _batch.Clear();
        }

        static void EnsureMeshes()
        {
            if (_cube != null) return;
            _cube = Prim0(PrimitiveType.Cube); _sphere = Prim0(PrimitiveType.Sphere); _cyl = Prim0(PrimitiveType.Cylinder);
            // gable roof prism: base 1x1 at y 0, ridge along z at y 1
            _prism = new Mesh { name = "roof_prism" };
            var v = new List<Vector3>(); var t = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(i); t.Add(i + 1); t.Add(i + 2); }
            Vector3 A = new Vector3(-.5f, 0, -.5f), B = new Vector3(.5f, 0, -.5f), C = new Vector3(.5f, 0, .5f), D = new Vector3(-.5f, 0, .5f), E = new Vector3(0, 1, -.5f), F = new Vector3(0, 1, .5f);
            Tri(A, E, B); Tri(D, C, F);                        // gables
            Tri(A, D, F); Tri(A, F, E); Tri(B, E, F); Tri(B, F, C); // slopes
            _prism.SetVertices(v); _prism.SetTriangles(t, 0); _prism.RecalculateNormals(); _prism.RecalculateBounds();
            // low-poly cone (8 sides), base at y -0.5, tip at +0.5 (unit height like the primitives)
            _cone = new Mesh { name = "cone8" }; v = new List<Vector3>(); t = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f;
                var p0 = new Vector3(Mathf.Cos(a0) * .5f, -.5f, Mathf.Sin(a0) * .5f); var p1 = new Vector3(Mathf.Cos(a1) * .5f, -.5f, Mathf.Sin(a1) * .5f);
                Tri(p1, p0, new Vector3(0, .5f, 0)); Tri(p0, p1, new Vector3(0, -.5f, 0));
            }
            _cone.SetVertices(v); _cone.SetTriangles(t, 0); _cone.RecalculateNormals(); _cone.RecalculateBounds();
        }

        static Mesh Prim0(PrimitiveType p)
        {
            var g = GameObject.CreatePrimitive(p); var m = g.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(g); else Object.DestroyImmediate(g);
            return m;
        }
    }
}
