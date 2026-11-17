using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 hand-held pouring (mechanic 4): props that are real vessels get a finite <see cref="LiquidContainer"/> sized from their own mesh
    /// (open top along the prop's up axis) plus a <see cref="VesselSync"/> so every player sees the same level. Tilt it in the hand and the
    /// liquid leaves over the low lip by the weir law; a receiver's opening catches it, anything else becomes a persistent puddle.
    /// </summary>
    public static class Vessels
    {
        struct Spec { public float CapacityL, StartL, Neck; public Color Tint; public bool Open; public Spec(float c, float s, float neck = 0f, Color tint = default, bool open = false) { CapacityL = c; StartL = s; Neck = neck; Tint = tint; Open = open; } }
        static readonly Color Detergent = new Color(0.35f, 0.55f, 0.98f, 0.9f), Kibble = new Color(0.62f, 0.38f, 0.18f, 1f);
        static readonly Dictionary<string, Spec> Table = new Dictionary<string, Spec>
        {
            ["mug"] = new Spec(0.30f, 0f), ["cup"] = new Spec(0.25f, 0f), ["kid_cup"] = new Spec(0.20f, 0f), ["pot"] = new Spec(2.0f, 0f),
            ["detergent"] = new Spec(0.9f, 0.7f, 0.012f, Detergent), ["empty_bottle"] = new Spec(0.5f, 0f, 0.012f),
            ["watering_can"] = new Spec(1.5f, 1.2f, 0.01f),
            // V12 pets: dry kibble pours like a (granular) liquid - finite, conserved, misses make a mess on the floor
            ["pet_food"] = new Spec(1.2f, 1.0f, 0.03f, Kibble, true), ["pet_bowl_cat"] = new Spec(0.12f, 0f, 0f, Kibble), ["pet_bowl_dog"] = new Spec(0.18f, 0f, 0f, Kibble), ["bucket"] = new Spec(6f, 0f), ["mop_bucket"] = new Spec(8f, 3f),
        };

        public static bool IsVessel(string kind) => kind != null && Table.ContainsKey(kind);
        static readonly Dictionary<PropBody, LiquidContainer> _of = new Dictionary<PropBody, LiquidContainer>();
        /// <summary>The liquid of a vessel prop (null if it is not a vessel).</summary>
        public static LiquidContainer Of(PropBody b) { if (b == null) return null; if (!_of.TryGetValue(b, out var lc) || (lc == null && IsVessel(b.Spec.Kind))) { lc = b.GetComponentInChildren<LiquidContainer>(); _of[b] = lc; } return lc; }

        public static LiquidContainer Attach(PropBody body)
        {
            if (body == null || body.Spec == null || !Table.TryGetValue(body.Spec.Kind, out var sp)) return null;
            var t = body.transform; bool any = false; var lb = new Bounds();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = t.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { lb = new Bounds(c, Vector3.zero); any = true; } else lb.Encapsulate(c);
                }
            }
            if (!any) return null;
            float r = Mathf.Min(lb.extents.x, lb.extents.z) * 0.8f, floor = lb.min.y + Mathf.Max(0.004f, lb.size.y * 0.06f), rim = lb.max.y - 0.003f;
            float geomCap = Mathf.PI * r * r * (rim - floor) * 1000f;
            float cap = Mathf.Min(sp.CapacityL, geomCap > 0.01f ? geomCap : sp.CapacityL);
            var holder = new GameObject("liquid"); holder.transform.SetParent(t, false); holder.transform.localPosition = new Vector3(lb.center.x, 0f, lb.center.z);   // vessel axis through the mesh centre
            var lc = LiquidContainer.Add(holder, cap, r, floor, rim, Mathf.Min(sp.StartL, cap * 0.7f));   // never brim-full: a full vessel spills when carried
            lc.NeckRadius = sp.Neck; lc.Tint = sp.Tint; lc.Granular = sp.Tint == Kibble;
            if (sp.Neck <= 0f) lc.CatchRadius = Mathf.Max(lb.extents.x, lb.extents.z) * 0.95f;   // open vessel: the whole rim catches
            if (sp.Neck > 0f && !sp.Open) { var screw = ScrewCap.Add(t, lc, sp.Neck, new Vector3(lb.center.x, lb.max.y - 0.004f, lb.center.z), sp.Tint.a > 0f ? Color.Lerp(sp.Tint, Color.white, 0.4f) : new Color(0.95f, 0.95f, 0.98f)); screw.HouseId = 100000 + body.Spec.Id; screw.gameObject.AddComponent<HouseMechSync>(); }   // bottles come capped; the cap replicates
            body.gameObject.AddComponent<VesselSync>();
            return lc;
        }
    }
}
