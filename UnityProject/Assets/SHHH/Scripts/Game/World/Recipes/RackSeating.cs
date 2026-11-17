using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V10 physical dish rack (daycare KIT018, authored 1:1). Replaces the V9 kinematic snap ("lerp the plate into the slot and freeze
    /// it"), which together with box-shaped plates on 3 mm rails produced the v10g failure: plate 2 never seated, 441 ceramic clatters.
    /// <para>How a plate gets racked now:</para>
    /// <list type="number">
    /// <item>Gaps come from the model's own divider colliders (COL_DIVIDER_NN), not from layout numbers.</item>
    /// <item>A released plate stays DYNAMIC. Only in the last few centimetres (inside a gap's capture band, roughly on edge, low in the rack)
    /// a capped spring-damper nudges it toward the gap's centre plane and a small torque stands it up. Max assist acceleration
    /// <see cref="MaxAssist"/> — a hand-sized nudge, not a teleport; gravity, the tray floor and the wires do the rest.</item>
    /// <item>It counts as racked only when it physically rests there: slow for <see cref="RestTime"/>, bottom on the tray floor, centre
    /// inside the gap, within <see cref="MaxLean"/> of vertical. Then <c>PlaceInSocket</c> goes to the authoritative rules.</item>
    /// <item>A plate released at a bad angle falls over / lies on the wires — real, recoverable, never "seated".</item>
    /// </list>
    /// Rules slots are indices (SlotRule.Any); each occupied slot remembers which physical gap its plate stands in.
    /// </summary>
    public sealed class RackSeating : MonoBehaviour
    {
        public const float MaxAssist = 2.5f, CaptureBand = 0.012f, RestTime = 0.3f, FloorTolerance = 0.025f, GuideHeight = 0.07f;
        /// <summary>V11: the steepest lean this rack physically allows a plate resting on its hooks (gap clearance over hook height) + 4 deg -
        /// measured from the model, not a guess (run 10: 21-27 deg leaning on the hook is "racked"; a fixed 25 deg made it flicker).</summary>
        public float MaxLean { get; private set; } = 25f;
        RecipeFixtureView _fv; RecipeRuntime _rt; ArticulatedModel _rack;
        readonly List<float> _gapX = new List<float>(); float _gapHalf;   // fixture-local x of gap centre planes, clear half width
        float _floorLocalY;                                                 // tray floor top, fixture-local
        float _hookZ, _hookTop, _halfDepth;                         // the dividers' tall hook (where a plate leans), fixture-local
        /// <summary>Where a plate's centre should sit front-to-back so its face leans on the tall divider hooks, not the 2 cm low runs.</summary>
        public float SupportZ { get; private set; }
        readonly Dictionary<int, int> _slotGap = new Dictionary<int, int>(); // rules slot -> gap index
        readonly Dictionary<PropBody, float> _rest = new Dictionary<PropBody, float>();
        public int Seated { get; private set; }
        public IReadOnlyList<float> Gaps => _gapX;
        public float GapClearWidth => _gapHalf * 2f;
        public Vector3 FloorWorld(float x) => _fv.transform.TransformPoint(new Vector3(x, _floorLocalY, 0f));

        public void Init(RecipeFixtureView fv, RecipeRuntime rt, ArticulatedModel rack, float wireDiameter)
        {
            _fv = fv; _rt = rt; _rack = rack;
            Physics.SyncTransforms();   // the authored COL boxes were created this frame: make their world bounds current
            // divider planes from the authored collision (grouped by divider number); fatten each wire box to the visible wire
            var xs = new SortedDictionary<string, List<float>>();
            float floorTop = float.MinValue;
            foreach (var c in rack.Colliders)
            {
                var bc = c as BoxCollider; if (bc == null) continue;
                string n = bc.name;
                if (n.StartsWith("COL_DIVIDER_"))
                {
                    // the converted FBX nodes are rotated: find which LOCAL axis of this box runs across the gaps (fixture right), in world
                    // terms, and make the box as thick as the painted wire along that axis only (v10 run 1 assumed local X - wrong axis)
                    var s = bc.size; int thin = 0; float best = -1f;
                    for (int i = 0; i < 3; i++) { var ax = bc.transform.TransformVector(i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward); float d = Mathf.Abs(Vector3.Dot(ax.normalized, fv.transform.right)); if (d > best) { best = d; thin = i; } }
                    float unit = bc.transform.TransformVector(thin == 0 ? Vector3.right : thin == 1 ? Vector3.up : Vector3.forward).magnitude;
                    if (unit > 1e-6f) s[thin] = Mathf.Max(s[thin], wireDiameter / unit); bc.size = s;
                    string grp = n.Substring(0, Mathf.Min(n.Length, 14));
                    if (!xs.TryGetValue(grp, out var l)) xs[grp] = l = new List<float>();
                    l.Add(fv.transform.InverseTransformPoint(bc.bounds.center).x);
                    var lc = fv.transform.InverseTransformPoint(bc.bounds.center); float top = fv.transform.InverseTransformPoint(new Vector3(bc.bounds.center.x, bc.bounds.max.y, bc.bounds.center.z)).y;
                    _halfDepth = Mathf.Max(Mathf.Abs(lc.z), _halfDepth);
                    if (Mathf.Abs(lc.z) < 0.13f && top > _hookTop) { _hookTop = top; _hookZ = lc.z; }   // tallest point away from the end posts
                }
                else if (n.StartsWith("COL_TrayFloor"))
                {
                    // top of the tray floor from the WORLD bounds (the fixture only yaws, so world up = fixture up)
                    var wb = bc.bounds; floorTop = Mathf.Max(floorTop, fv.transform.InverseTransformPoint(new Vector3(wb.center.x, wb.max.y, wb.center.z)).y);
                }
            }
            var planes = new List<float>(); foreach (var kv in xs) { float a = 0; foreach (var v in kv.Value) a += v; planes.Add(a / kv.Value.Count); }
            planes.Sort();
            for (int i = 0; i + 1 < planes.Count; i++) _gapX.Add((planes[i] + planes[i + 1]) * 0.5f);
            _gapHalf = planes.Count > 1 ? ((planes[1] - planes[0]) - wireDiameter) * 0.5f : 0.02f;
            _floorLocalY = floorTop > float.MinValue ? floorTop : 0.03f;
            float r = 0.11f; // plate radius (22 cm dinner plate)
            // as far toward the hook as the tray depth allows: the disk then overlaps the tall hook by ~5 cm (max lean ~10-13 deg)
            // 12 mm clear of the front/rear rim wire so a plate lowered straight down does not graze it and get pushed off the hooks (V11)
            SupportZ = Mathf.Clamp(_hookZ, -(_halfDepth - r - 0.012f), _halfDepth - r - 0.012f);
            float plateThick = 0.018f;
            MaxLean = Mathf.Clamp(Mathf.Atan2(Mathf.Max(0.005f, GapClearWidth - plateThick), Mathf.Max(0.02f, _hookTop - _floorLocalY) * 0.85f) * Mathf.Rad2Deg + 4f, 15f, 40f);
            Debug.Log($"[SHHH][rack] max lean on the hooks {MaxLean:0.0} deg (gap {GapClearWidth * 1000f:0} mm, hook {(_hookTop - _floorLocalY) * 1000f:0} mm)");
            Debug.Log($"[SHHH][rack] divider hook at z {_hookZ:0.000} (top {_hookTop - _floorLocalY:0.000} above the tray floor), rack half depth {_halfDepth:0.000} -> plate support line z {SupportZ:0.000}");
            if (_floorLocalY < 0f || _floorLocalY > 0.08f) Debug.LogError($"[SHHH][rack] tray floor measured at {_floorLocalY:0.000} m above the rack base - expected ~0.03 (collider axes?)");
            Debug.Log($"[SHHH][rack] KIT018 seating: {planes.Count} dividers, {_gapX.Count} gaps, clear gap {GapClearWidth * 1000f:0.0} mm, tray floor y {_floorLocalY:0.000} (fixture-local)");
        }

        /// <summary>Gap a free rules slot should use (bots / harness / layout): spread out from the middle (0, +1, -1, +2 ...).</summary>
        int DefaultGap(int slot)
        {
            int mid = _gapX.Count / 2; var used = new HashSet<int>(_slotGap.Values);
            int[] order = new int[_gapX.Count]; int n = 0;
            for (int d = 0; n < _gapX.Count; d++) { foreach (int g in d == 0 ? new[] { mid } : new[] { mid + 2 * d - 1, mid - 2 * d + 1, mid + 2 * d, mid - 2 * d }) if (g >= 0 && g < _gapX.Count && System.Array.IndexOf(order, g, 0, n) < 0) order[n++] = g; if (d > _gapX.Count) break; }
            int k = 0; foreach (var g in order) { if (used.Contains(g)) continue; if (k == slot) return g; k++; }
            return mid;
        }

        public int GapOfSlot(int slot) => _slotGap.TryGetValue(slot, out var g) ? g : DefaultGap(slot);

        /// <summary>World point on the tray floor at the centre of the gap used by this rules slot.</summary>
        public Vector3 SlotPoint(int slot) { int g = Mathf.Clamp(GapOfSlot(slot), 0, Mathf.Max(0, _gapX.Count - 1)); return _fv.transform.TransformPoint(new Vector3(_gapX.Count > 0 ? _gapX[g] : 0f, _floorLocalY, SupportZ)); }

        int NearestGap(float x, out float dx)
        {
            int best = -1; dx = float.MaxValue;
            for (int i = 0; i < _gapX.Count; i++) { float d = x - _gapX[i]; if (Mathf.Abs(d) < Mathf.Abs(dx)) { dx = d; best = i; } }
            return best;
        }

        /// <summary>Angle between the plate's face normal and the rack's across-gap axis (0 = standing perfectly in the gap).</summary>
        float Lean(PropBody p) { float a = Vector3.Angle(p.transform.up, _fv.transform.right); return Mathf.Min(a, 180f - a); }

        /// <summary>Is this plate physically standing in a gap right now (used by the crooked check and by QA)?</summary>
        public bool StandsInGap(PropBody p, out int gap)
        {
            var l = _fv.transform.InverseTransformPoint(p.Col.bounds.center); gap = NearestGap(l.x, out float dx);
            float bottom = _fv.transform.InverseTransformPoint(p.Col.ClosestPoint(p.Col.bounds.center + Vector3.down)).y;
            return gap >= 0 && Mathf.Abs(dx) <= _gapHalf + 0.004f && Lean(p) <= MaxLean && bottom <= _floorLocalY + FloorTolerance && Mathf.Abs(l.z) < 0.16f;
        }

        /// <summary>QA: where a plate stands relative to the rack (gap offset, front-back, lean, height above the tray floor).</summary>
        public string Describe(PropBody p)
        {
            var c = p.Col.bounds.center; var l = _fv.transform.InverseTransformPoint(c); int g = NearestGap(l.x, out float dx);
            float bottom = _fv.transform.InverseTransformPoint(p.Col.ClosestPoint(c + Vector3.down)).y;
            return $"gap {g} dx {dx * 1000f:0}mm z {l.z * 1000f:0}mm (support {SupportZ * 1000f:0}) lean {Lean(p):0}deg bottom +{(bottom - _floorLocalY) * 1000f:0}mm";
        }

        void FixedUpdate()
        {
            if (!GameServices.IsAuthority || _rt == null || _fv == null) return;
            var st = _fv.State; var round = _rt.Round; if (st == null || round == null || !round.IsPlayable) return;
            int seated = 0;
            foreach (var p in _rt.Bodies)
            {
                if (p == null || p.Body == null) continue;
                var rp = _rt.RProp(p); if (rp == null || (rp.Kind != "plate" && rp.Kind != "kid_plate")) continue;
                if (rp.IsPlaced && rp.Fixture == st.Id) { seated++; p.Racked = !p.IsHeld; continue; }
                if (p.IsHeld || p.Body.isKinematic || rp.IsPlaced || _rt.IsFollower(rp)) { _rest.Remove(p); continue; }
                var c = p.Col.bounds.center; var l = _fv.transform.InverseTransformPoint(c);
                if (Mathf.Abs(l.x) > 0.24f || Mathf.Abs(l.z) > 0.17f || l.y > 0.35f || l.y < _floorLocalY - 0.02f) { _rest.Remove(p); continue; }
                int g = NearestGap(l.x, out float dx); if (g < 0) continue;
                float lean = Lean(p);
                float bottom = _fv.transform.InverseTransformPoint(p.Col.ClosestPoint(c + Vector3.down)).y;
                // ---- guidance: only in the last centimetres, only when it is already roughly on edge and over a gap
                bool entering = Mathf.Abs(dx) < _gapHalf + CaptureBand && lean < 50f && bottom < _floorLocalY + GuideHeight;
                p.Racked = entering;   // standing on edge between the wires is what a rack is for: never a "precarious" trap
                if (entering)
                {
                    var right = _fv.transform.right; var fwd = _fv.transform.forward;
                    float vx = Vector3.Dot(p.Body.linearVelocity, right), vz = Vector3.Dot(p.Body.linearVelocity, fwd);
                    float ax = Mathf.Clamp(-dx * 120f - vx * 9f, -MaxAssist, MaxAssist);
                    // front-to-back: onto the line where the plate's face meets the tall hooks (V10 rev: plates sat on the 2 cm low runs, 20-30 deg lean)
                    float az = Mathf.Clamp(-(l.z - SupportZ) * 60f - vz * 8f, -MaxAssist, MaxAssist);
                    p.Body.AddForce(right * ax + fwd * az, ForceMode.Acceleration);
                    // stand it up: rotate the face normal toward the nearer across-gap direction (capped angular acceleration)
                    var tgt = Vector3.Dot(p.transform.up, right) >= 0 ? right : -right;
                    var axis = Vector3.Cross(p.transform.up, tgt); float ang = Mathf.Asin(Mathf.Clamp(axis.magnitude, 0f, 1f));
                    if (ang > 0.01f) p.Body.AddTorque(axis.normalized * Mathf.Min(ang * 30f, 12f) - p.Body.angularVelocity * 2.5f, ForceMode.Acceleration);
                }
                // ---- seated = really resting there
                bool resting = p.Body.linearVelocity.magnitude < 0.06f && p.Body.angularVelocity.magnitude < 0.6f;
                bool inGap = Mathf.Abs(dx) <= _gapHalf + 0.004f && lean <= MaxLean && bottom <= _floorLocalY + FloorTolerance;
                if (!(resting && inGap)) { _rest.Remove(p); continue; }
                _rest.TryGetValue(p, out float t); t += Time.fixedDeltaTime; _rest[p] = t;
                if (t < RestTime || p.Reserved) continue;
                _rest.Remove(p);
                int slot = -1; for (int k = 0; k < st.Occupant.Length; k++) if (st.Occupant[k] < 0) { slot = k; break; }
                if (slot < 0) { Debug.Log($"[SHHH][rack] plate#{p.Spec.Id} stands in gap {g} but the rules have no free rack slot"); p.ReservedUntil = Time.time + 2f; continue; }
                // the rules accept a placement from the last holder of a released item (it was let go into the rack)
                int actor = rp.LastHolder != ActorIds.None ? rp.LastHolder : p.LastActor;
                _slotGap[slot] = g;   // before the op: SlotPoint(slot) must already name this gap when the runtime checks the placement
                var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.PlaceInSocket, Prop = rp.Id, Prop2 = -1, Fixture = st.Id, Slot = slot });
                if (err != null) { _slotGap.Remove(slot); Debug.Log($"[SHHH][rack] gap {g} slot {slot} rejected plate#{p.Spec.Id} by {actor}: {err}"); p.ReservedUntil = Time.time + 1.5f; continue; }
                if (!rp.Oriented) RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Align, Prop = rp.Id, Prop2 = -1, Fixture = st.Id, Slot = slot, Arg = "" });
                _slotGap[slot] = g; p.Racked = true; seated++;
                Debug.Log($"[SHHH][rack] plate#{p.Spec.Id} seated in gap {g} (dx {dx * 1000f:0.0} mm, z {l.z * 1000f:0} mm vs support {SupportZ * 1000f:0}, lean {lean:0}°, bottom +{(bottom - _floorLocalY) * 1000f:0} mm) -> slot {slot}");
            }
            // forget gaps of slots the rules emptied (taken out / knocked out)
            List<int> gone = null; foreach (var kv in _slotGap) if (kv.Key >= st.Occupant.Length || st.Occupant[kv.Key] < 0) (gone ??= new List<int>()).Add(kv.Key);
            if (gone != null) foreach (var k in gone) _slotGap.Remove(k);
            Seated = seated;
        }
    }
}
