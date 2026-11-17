using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Locked decision 02: siblings are floating, disconnected pieces - head, torso, hands, shoes - driven by an internal
    /// humanoid-compatible skeleton. This keeps Astra's approved sibling art (faces, hair, pyjamas, rounded hands, shoes) and its
    /// rig/animations, and removes the arm and leg segments from the skinned mesh at load: triangles weighted to upper/lower arm and
    /// thigh/shin bones are dropped, and every cut opening is closed with a soft cap, so the remaining pieces read as solid parts
    /// that move with the (invisible) limbs. Blend shapes, bone weights and materials are preserved. Mom keeps her full body.
    /// </summary>
    public static class DetachedLimbs
    {
        public struct Report { public int Meshes, TrisBefore, TrisAfter, Caps; public string LimbBones, Notes; public override string ToString() => $"meshes {Meshes}, tris {TrisBefore} -> {TrisAfter}, caps {Caps}, limb bones [{LimbBones}] {Notes}"; }

        static readonly string[] Keep = { "hand", "finger", "thumb", "index", "middle", "ring", "pinky", "little", "palm", "foot", "toe", "ball", "heel", "shoe", "head", "neck", "spine", "chest", "hip", "pelvis", "root", "clav", "shoulder" };
        static readonly string[] Limb = { "upperarm", "upper_arm", "arm", "forearm", "lowerarm", "lower_arm", "elbow", "thigh", "upleg", "up_leg", "upperleg", "upper_leg", "leg", "calf", "shin", "knee", "lowerleg", "lower_leg" };

        public static bool IsLimbBone(string boneName)
        {
            var n = boneName.ToLowerInvariant().Replace(" ", "").Replace(".", "_");
            foreach (var k in Keep) if (n.Contains(k)) return false;
            foreach (var l in Limb) if (n.Contains(l)) return true;
            return false;
        }

        public static Report Apply(Transform model)
        {
            var rep = new Report(); var limbNames = new SortedSet<string>(); var notes = new System.Text.StringBuilder();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || !mesh.isReadable || smr.bones == null || smr.bones.Length == 0)
                {
                    notes.Append($"[{smr.name}: mesh {(mesh ? mesh.name : "null")} readable {(mesh ? mesh.isReadable : false)} bones {smr.bones?.Length ?? 0}: {string.Join(",", System.Linq.Enumerable.Take(System.Linq.Enumerable.Select(smr.bones ?? new Transform[0], b => b ? b.name : "-"), 40))}] ");
                    continue;
                }
                var bones = smr.bones; var limb = new bool[bones.Length];
                for (int i = 0; i < bones.Length; i++) if (bones[i] != null && IsLimbBone(bones[i].name)) { limb[i] = true; limbNames.Add(bones[i].name); }
                bool any = false; foreach (var l in limb) any |= l;
                if (!any) { notes.Append($"[{smr.name}: no limb bones in {string.Join(",", System.Linq.Enumerable.Take(System.Linq.Enumerable.Select(bones, b => b ? b.name : "-"), 40))}] "); continue; }
                var bw = mesh.boneWeights; if (bw == null || bw.Length != mesh.vertexCount) continue;
                int vc = mesh.vertexCount; var isLimb = new bool[vc];
                for (int v = 0; v < vc; v++)
                {
                    var w = bw[v]; float lw = 0f;
                    if (w.boneIndex0 < limb.Length && limb[w.boneIndex0]) lw += w.weight0;
                    if (w.boneIndex1 < limb.Length && limb[w.boneIndex1]) lw += w.weight1;
                    if (w.boneIndex2 < limb.Length && limb[w.boneIndex2]) lw += w.weight2;
                    if (w.boneIndex3 < limb.Length && limb[w.boneIndex3]) lw += w.weight3;
                    isLimb[v] = lw > 0.5f;
                }
                rep.Meshes++;
                var verts = new List<Vector3>(mesh.vertices); var normals = new List<Vector3>(mesh.normals); var tangents = new List<Vector4>(mesh.tangents);
                var uv = new List<Vector2>(); mesh.GetUVs(0, uv); var colors = new List<Color>(mesh.colors); var weights = new List<BoneWeight>(bw);
                var newSubs = new List<List<int>>();
                // original open edges (not ours to cap)
                var origCount = new Dictionary<long, int>();
                for (int s = 0; s < mesh.subMeshCount; s++) { var t = mesh.GetTriangles(s); for (int i = 0; i < t.Length; i += 3) for (int e = 0; e < 3; e++) Count(origCount, t[i + e], t[i + (e + 1) % 3]); }
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var t = mesh.GetTriangles(s); rep.TrisBefore += t.Length / 3;
                    var kept = new List<int>(t.Length);
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        int n = (isLimb[t[i]] ? 1 : 0) + (isLimb[t[i + 1]] ? 1 : 0) + (isLimb[t[i + 2]] ? 1 : 0);
                        if (n >= 2) continue;
                        kept.Add(t[i]); kept.Add(t[i + 1]); kept.Add(t[i + 2]);
                    }
                    // cap the new openings of this submesh (directed boundary edges -> loops -> fan)
                    var used = new Dictionary<long, int>(); var dir = new Dictionary<int, int>();
                    for (int i = 0; i < kept.Count; i += 3) for (int e = 0; e < 3; e++) Count(used, kept[i + e], kept[i + (e + 1) % 3]);
                    for (int i = 0; i < kept.Count; i += 3)
                        for (int e = 0; e < 3; e++)
                        {
                            int a = kept[i + e], b = kept[i + (e + 1) % 3]; long k = Key(a, b);
                            if (used[k] == 1 && origCount.TryGetValue(k, out var oc) && oc >= 2 && !dir.ContainsKey(b)) dir[b] = a; // reversed for the cap
                        }
                    var visited = new HashSet<int>();
                    foreach (var start in new List<int>(dir.Keys))
                    {
                        if (visited.Contains(start)) continue;
                        var loop = new List<int>(); int cur = start, guard = 0;
                        while (!visited.Contains(cur) && dir.ContainsKey(cur) && guard++ < 4096) { visited.Add(cur); loop.Add(cur); cur = dir[cur]; }
                        if (loop.Count < 3 || cur != start) continue;
                        Vector3 c = Vector3.zero, nn = Vector3.zero; Vector2 cu = Vector2.zero;
                        foreach (var v in loop) { c += verts[v]; if (normals.Count > v) nn += normals[v]; if (uv.Count > v) cu += uv[v]; }
                        c /= loop.Count; cu /= loop.Count;
                        // cap normal: average of the loop's face direction
                        var capN = Vector3.zero; for (int i = 0; i < loop.Count; i++) capN += Vector3.Cross(verts[loop[i]] - c, verts[loop[(i + 1) % loop.Count]] - c);
                        int ci = verts.Count; verts.Add(c);
                        if (normals.Count > 0) normals.Add(capN.sqrMagnitude > 1e-12f ? capN.normalized : nn.normalized);
                        if (tangents.Count > 0) tangents.Add(tangents[loop[0]]);
                        if (uv.Count > 0) uv.Add(cu);
                        if (colors.Count > 0) colors.Add(colors[loop[0]]);
                        weights.Add(bw[loop[0]]);
                        for (int i = 0; i < loop.Count; i++) { kept.Add(loop[i]); kept.Add(loop[(i + 1) % loop.Count]); kept.Add(ci); }
                        rep.Caps++;
                    }
                    rep.TrisAfter += kept.Count / 3;
                    newSubs.Add(kept);
                }
                var m = new Mesh { name = mesh.name + "_detached", indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : mesh.indexFormat };
                m.SetVertices(verts); if (normals.Count == verts.Count) m.SetNormals(normals); if (tangents.Count == verts.Count) m.SetTangents(tangents);
                if (uv.Count == verts.Count) m.SetUVs(0, uv); if (colors.Count == verts.Count) m.SetColors(colors);
                m.boneWeights = weights.ToArray(); m.bindposes = mesh.bindposes;
                m.subMeshCount = newSubs.Count; for (int s = 0; s < newSubs.Count; s++) m.SetTriangles(newSubs[s], s);
                // blend shapes (faces): copy frames, zero deltas for cap centres
                int extra = verts.Count - vc;
                for (int bs = 0; bs < mesh.blendShapeCount; bs++)
                    for (int f = 0; f < mesh.GetBlendShapeFrameCount(bs); f++)
                    {
                        var dv = new Vector3[vc]; var dn = new Vector3[vc]; var dt = new Vector3[vc];
                        mesh.GetBlendShapeFrameVertices(bs, f, dv, dn, dt);
                        System.Array.Resize(ref dv, vc + extra); System.Array.Resize(ref dn, vc + extra); System.Array.Resize(ref dt, vc + extra);
                        m.AddBlendShapeFrame(mesh.GetBlendShapeName(bs), mesh.GetBlendShapeFrameWeight(bs, f), dv, dn, dt);
                    }
                m.RecalculateBounds();
                smr.sharedMesh = m;
            }
            rep.LimbBones = string.Join(",", limbNames); rep.Notes = notes.ToString();
            return rep;
        }

        static long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        static void Count(Dictionary<long, int> d, int a, int b) { long k = Key(a, b); d.TryGetValue(k, out var c); d[k] = c + 1; }
    }
}
