using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// My OWN body only (owner, Quest 2 run 2026-11-11: "my arms block my view" - the sleeve filled a third to half of the view when
    /// reaching). On a private copy of each skinned mesh: the triangles that belong to the upper arms are removed and the forearms are
    /// slimmed toward their bone axis. Friends never see this copy - they build my body from my replicated poses on their own machine.
    /// The gloves stay my hands (Genies hands are already collapsed for my camera).
    /// Genies skins the visible mesh to helper bones ("LeftArmTwist1Bind".."3", "LeftForeArmTwist1Bind".., "LeftHandBind"), not to the
    /// Animator's humanoid bones, so the arm parts are found by side + name.
    /// </summary>
    public sealed class SelfArmTrim
    {
        readonly List<(SkinnedMeshRenderer smr, Mesh original, Mesh trimmed)> _parts = new List<(SkinnedMeshRenderer, Mesh, Mesh)>();
        public string Report { get; private set; } = "";
        public bool On { get; private set; }

        enum Part { None, Upper, Fore, Hand }
        static Part Classify(string n, string side)
        {
            if (!n.StartsWith(side)) return Part.None;
            var r = n.Substring(side.Length);
            if (r == "Arm" || r.StartsWith("ArmTwist") || r == "ArmBind") return Part.Upper;
            if (r == "ForeArm" || r.StartsWith("ForeArmTwist") || r == "ForeArmBind") return Part.Fore;
            if (r == "Hand" || r == "HandBind") return Part.Hand;
            return Part.None;
        }

        /// <param name="removeUpper">V12 owner 2026-11-12 ("the clothes should appear completely"): false = nothing is cut; the forearm
        /// sleeves are only slimmed a little (no taper), so the outfit stays whole.</param>
        public static SelfArmTrim Build(Animator a, float slim = 0.7f, bool removeUpper = true)
        {
            var t = new SelfArmTrim(); if (a == null) { t.Report = "no animator"; return t; }
            int meshes = 0, unreadable = 0, removedTris = 0, slimmed = 0;
            foreach (var smr in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var src = smr.sharedMesh; if (src == null) continue;
                if (!src.isReadable) { unreadable++; continue; }
                var bones = smr.bones; var bind = src.bindposes; var bw = src.boneWeights;
                if (bones == null || bw == null || bw.Length != src.vertexCount || bind.Length != bones.Length) continue;
                var sides = new[] { "Left", "Right" };
                var part = new Part[2, bones.Length];
                var elbow = new[] { -1, -1 }; var wrist = new[] { -1, -1 };
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] == null) continue;
                    for (int s = 0; s < 2; s++)
                    {
                        var p = Classify(bones[i].name, sides[s]); part[s, i] = p;
                        if (p == Part.Fore && (elbow[s] < 0 || bones[i].name == sides[s] + "ForeArm")) elbow[s] = i;
                        if (p == Part.Hand && (wrist[s] < 0 || bones[i].name.EndsWith("Bind"))) wrist[s] = i;
                    }
                }
                float Sum(BoneWeight w, int s, Part p)
                {
                    float x = 0f;
                    if (w.boneIndex0 < bones.Length && part[s, w.boneIndex0] == p) x += w.weight0;
                    if (w.boneIndex1 < bones.Length && part[s, w.boneIndex1] == p) x += w.weight1;
                    if (w.boneIndex2 < bones.Length && part[s, w.boneIndex2] == p) x += w.weight2;
                    if (w.boneIndex3 < bones.Length && part[s, w.boneIndex3] == p) x += w.weight3;
                    return x;
                }
                var hide = new bool[bw.Length]; bool any = false;
                if (removeUpper) for (int v = 0; v < bw.Length; v++) if (Sum(bw[v], 0, Part.Upper) + Sum(bw[v], 1, Part.Upper) > 0.3f) { hide[v] = true; any = true; }
                // forearms: pull the sleeve toward the elbow->wrist axis (bind space)
                var verts = src.vertices; bool moved = false;
                for (int s = 0; s < 2; s++)
                {
                    if (elbow[s] < 0 || wrist[s] < 0) continue;
                    var e = bind[elbow[s]].inverse.MultiplyPoint3x4(Vector3.zero); var h = bind[wrist[s]].inverse.MultiplyPoint3x4(Vector3.zero);
                    var axis = h - e; float len = axis.magnitude; if (len < 1e-4f) continue; axis /= len;
                    for (int v = 0; v < verts.Length; v++)
                    {
                        if (hide[v]) continue;
                        float w = Sum(bw[v], s, Part.Fore); if (w <= 0.05f) continue;
                        float tt = Vector3.Dot(verts[v] - e, axis); if (tt < -0.1f || tt > len * 1.05f) continue;
                        var c = e + axis * tt;
                        // taper to a soft rounded end at the elbow (no ragged cut where the upper arm was removed)
                        float k = removeUpper ? Mathf.Lerp(0.12f, slim, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tt / 0.07f))) : slim;
                        verts[v] = c + (verts[v] - c) * Mathf.Lerp(1f, k, Mathf.Clamp01(w)); moved = true; slimmed++;
                    }
                }
                if (!any && !moved) continue;
                var m = Object.Instantiate(src); m.name = src.name + "_selfArms";
                if (moved) m.vertices = verts;
                for (int sm = 0; sm < m.subMeshCount; sm++)
                {
                    var tris = m.GetTriangles(sm); var keep = new List<int>(tris.Length);
                    for (int i = 0; i + 2 < tris.Length; i += 3)
                    {
                        if (hide[tris[i]] || hide[tris[i + 1]] || hide[tris[i + 2]]) { removedTris++; continue; }
                        keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
                    }
                    m.SetTriangles(keep, sm, false);
                }
                t._parts.Add((smr, src, m)); meshes++;
            }
            t.Report = $"{meshes} mesh(es) trimmed: {removedTris} upper-arm triangles removed, {slimmed} forearm vertices slimmed to {slim:0.00}" + (unreadable > 0 ? $"; {unreadable} mesh(es) not readable (left as they are)" : "");
            return t;
        }

        public void Set(bool on)
        {
            foreach (var (smr, original, trimmed) in _parts) if (smr != null) smr.sharedMesh = on ? trimmed : original;
            On = on;
        }

        public void Dispose() { Set(false); foreach (var p in _parts) if (p.trimmed != null) Object.Destroy(p.trimmed); _parts.Clear(); }
    }
}
