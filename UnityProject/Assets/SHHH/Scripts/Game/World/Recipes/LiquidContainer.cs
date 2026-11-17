using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V11 finite liquid reservoir in a vessel (cup, bowl, bucket, jug, the sink basin's neighbours). Quest-practical, no CFD:
    /// <list type="bullet">
    /// <item><b>Volume</b> (litres) is conserved: it only changes by inflow (a stream landing inside the opening), outflow (pouring over the lowest rim
    /// point) and spills; every transfer is accounted for in <see cref="Ledger"/>.</item>
    /// <item><b>Surface</b>: the vessel is modelled as an upright cylinder (inner radius, floor, rim heights in the vessel's local space). The free
    /// surface stays horizontal in WORLD space whatever the tilt; its height is solved from the volume (mean height of the liquid column over the
    /// cross-section), and it lags behind accelerations like a damped spring (slosh).</item>
    /// <item><b>Pour</b>: when the surface rises above the lowest rim point the excess flows out: flux = k * w * excess^1.5 (weir law), limited by the
    /// volume. It leaves as an arcing <see cref="ToonWaterStream"/> with the rim's speed; what the stream lands on decides where the volume goes:
    /// another vessel's opening (transfer), the basin (drain), or a surface (<see cref="Puddle"/> grows).</item>
    /// <item><b>Fill</b>: any falling stream whose landing point is inside this opening adds its flow (see <see cref="Catch"/>).</item>
    /// </list>
    /// Presentation: a toon water surface disc (ripple shader) clipped to the inner cross-section; tint differs for soapy water.
    /// </summary>
    public sealed class LiquidContainer : MonoBehaviour
    {
        public static readonly List<LiquidContainer> All = new List<LiquidContainer>();
        public float Capacity = 0.3f, Amount;            // litres
        public float InnerRadius = 0.04f, FloorY = 0.005f, RimY = 0.1f; // vessel-local (y up), metres
        public bool Soapy;
        /// <summary>V12: liquid colour (alpha 0 = clear toon water); detergent is blue.</summary>
        public Color Tint;
        /// <summary>V12: bottle neck radius for the pour lip (0 = open vessel, the lip is the full rim).</summary>
        public float NeckRadius;
        /// <summary>V12 network replica: <see cref="Amount"/> is set from the authority's replicated value (RProp.Ml); this peer only shows
        /// the level and the pour stream, it never moves volume (no double counting, no client-side spills).</summary>
        public bool Replica;
        /// <summary>V12: a screw cap is on (see <see cref="ScrewCap"/>): the vessel cannot pour or spill.</summary>
        public bool Sealed;
        /// <summary>V12 dry goods (kibble): pellet stream, kibble pile surface, spills become scattered kibble (not a water puddle).</summary>
        public bool Granular;
        /// <summary>V12 open bowls: whatever lands inside the OUTER rim ends up in the bowl (its prop collider is a solid box whose top is the rim).</summary>
        public float CatchRadius;
        /// <summary>QA: which container caught this one's last pour.</summary>
        public string LastReceiver = "";
        /// <summary>V12: the seat that last poured something into this container (who fed the pet).</summary>
        public int LastFeeder = SHHH.Core.ActorIds.None;
        int PourerActor() { var pb = GetComponentInParent<PropBody>(); return pb == null ? SHHH.Core.ActorIds.None : pb.IsHeld ? pb.HeldBy : pb.LastActor; }
        public float PourCoefficient = 1.8f;              // weir constant (tuned so a full mug empties in ~2 s held upside down)
        public readonly List<string> Ledger = new List<string>();
        public float PouredOut { get; private set; }
        public float Received { get; private set; }
        public bool Pouring { get; private set; }
        /// <summary>QA: where the last pour stream landed (collider name or "surface"/"nothing") and the point.</summary>
        public string StreamLanding => _stream == null ? "none" : !_stream.Hitting ? "nothing (falling)" : (_stream.HitCollider != null ? _stream.HitCollider.name : "surface") + " @" + _stream.HitPoint.ToString("F3");

        Transform _surface; Material _mat; ToonWaterStream _stream; Rigidbody _rb;
        Vector3 _lastVel; float _sloshX, _sloshZ, _sloshVX, _sloshVZ, _accumOut;

        public float Area => Mathf.PI * InnerRadius * InnerRadius;
        public float LevelFraction => Capacity > 0f ? Amount / Capacity : 0f;
        /// <summary>World height of the free surface (mean), or -inf when empty.</summary>
        public float SurfaceWorldY { get; private set; } = float.NegativeInfinity;

        public static LiquidContainer Add(GameObject go, float capacityL, float innerRadius, float floorY, float rimY, float startL)
        {
            var c = go.AddComponent<LiquidContainer>();
            c.Capacity = capacityL; c.InnerRadius = innerRadius; c.FloorY = floorY; c.RimY = rimY; c.Amount = Mathf.Clamp(startL, 0f, capacityL);
            return c;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();
            var g = new GameObject("liquid_surface"); g.transform.SetParent(transform, false); _surface = g.transform;
            g.AddComponent<MeshFilter>().sharedMesh = Disc();
            var r = g.AddComponent<MeshRenderer>(); _mat = ToonWaterStream.WaterMaterial(new Color(0.45f, 0.75f, 0.96f, 0.85f), new Color(0.2f, 0.5f, 0.88f, 0.95f), 1f);
            r.sharedMaterial = _mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var sg = new GameObject("pour_stream"); sg.transform.SetParent(transform, false); _stream = sg.AddComponent<ToonWaterStream>();
            _stream.MaxFall = 1.2f; _stream.MaxRadius = 0.009f; _stream.MinRadius = 0.003f;
            if (Granular) { _stream.Granular = true; r.sharedMaterial = _mat = KibbleMaterial(); }
            var body = GetComponentInParent<PropBody>(); var self = body != null ? body.transform : transform;   // never land on our own bottle
            _stream.Ignore = col => col.transform.IsChildOf(self);
        }

        /// <summary>Is a world point inside this vessel's opening (within the rim circle, between floor and just above the rim)?</summary>
        public bool InOpening(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return new Vector2(l.x, l.z).magnitude <= Mathf.Max(InnerRadius * 1.05f, CatchRadius) && l.y >= FloorY - 0.01f && l.y <= RimY + 0.03f;
        }

        /// <summary>V12: a stream that lands on this vessel's own floor (an up-facing surface of the body that carries it, below the rim) is
        /// inside it - e.g. the far corner of a rectangular detergent drawer that the round opening model does not cover.</summary>
        public bool HitsOwnFloor(Collider hit, Vector3 point, Vector3 normal)
        {
            if (hit == null || normal.y < 0.7f || transform.parent == null || !hit.transform.IsChildOf(transform.parent)) return false;
            var l = transform.InverseTransformPoint(point); return l.y >= FloorY - 0.02f && l.y <= RimY + 0.01f;
        }

        /// <summary>A stream lands here: add volume (returns what did not fit = overflow, spilled over the rim).</summary>
        public float Catch(float litres, string from)
        {
            if (litres <= 0f) return 0f;
            float room = Mathf.Max(0f, Capacity - Amount), take = Mathf.Min(room, litres);
            Amount += take; Received += take;
            if (take > 0f && (Ledger.Count == 0 || !Ledger[Ledger.Count - 1].StartsWith("in " + from))) Ledger.Add($"in {from} {Time.time:0.0}");
            return litres - take;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // slosh: the surface tilts against the vessel's horizontal acceleration (damped spring), bounded
            var vel = _rb != null ? _rb.linearVelocity : Vector3.zero; var acc = (vel - _lastVel) / Mathf.Max(1e-4f, dt); _lastVel = vel;
            _sloshVX += (-acc.x * 0.012f - _sloshX * 60f - _sloshVX * 6f) * dt; _sloshVZ += (-acc.z * 0.012f - _sloshZ * 60f - _sloshVZ * 6f) * dt;
            _sloshX = Mathf.Clamp(_sloshX + _sloshVX * dt, -0.35f, 0.35f); _sloshZ = Mathf.Clamp(_sloshZ + _sloshVZ * dt, -0.35f, 0.35f);
            if (Granular) { _sloshX = _sloshZ = _sloshVX = _sloshVZ = 0f; }   // dry kibble does not slosh out of a carried bag
            Pouring = false;
            if (Amount <= 1e-5f) { Amount = 0f; SurfaceWorldY = float.NegativeInfinity; _stream.TargetFlow = 0f; return; }
            // mean surface height: the vessel's floor centre + the liquid column height (volume / cross-section) along world up,
            // the cross-section seen by gravity shrinks with tilt (cos) - an honest cylinder approximation
            var up = transform.up; float cos = Mathf.Max(0.05f, Vector3.Dot(up, Vector3.up));
            var floorC = transform.TransformPoint(new Vector3(0, FloorY, 0));
            float column = (Amount / 1000f) / Area;                       // m along the vessel axis when upright
            SurfaceWorldY = floorC.y + column * cos + Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos)) * 0f;
            // the rim point that sits lowest in world space (towards the tilt): weir height above it
            var tiltDir = Vector3.ProjectOnPlane(Vector3.down, up); Vector3 rimLow; float lipR = NeckRadius > 0f ? NeckRadius : InnerRadius;
            if (tiltDir.sqrMagnitude < 1e-6f) rimLow = transform.TransformPoint(new Vector3(lipR, RimY, 0));
            else rimLow = transform.TransformPoint(new Vector3(0, RimY, 0)) + tiltDir.normalized * lipR * transform.lossyScale.x;
            // the surface reaches the low rim when the liquid wedge does: mean level + half the wedge rise across the radius
            float wedge = (NeckRadius > 0f ? NeckRadius : InnerRadius) * Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos)) / cos;   // tan(tilt) * lip radius
            float edgeLevel = SurfaceWorldY + wedge + (_sloshX * tiltDir.x + _sloshZ * tiltDir.z) * InnerRadius;
            float excess = edgeLevel - rimLow.y;
            if (Sealed) { _stream.TargetFlow = 0f; return; }   // capped bottle: nothing comes out however it is tilted
            if (Granular && cos > 0.82f) { _stream.TargetFlow = 0f; return; }   // kibble only comes out when the bag is really tipped (> 35 deg)
            if (excess > 0f || cos < 0.15f)
            {
                float w = (NeckRadius > 0f ? NeckRadius : InnerRadius) * 1.2f;
                float flux = PourCoefficient * w * Mathf.Pow(Mathf.Max(excess, cos < 0.15f ? 0.02f : 0f), 1.5f) * 1000f; // L/s
                // dry goods do not pour like water: a granular flow through an opening is ~constant (Beverloo: W = C rho sqrt(g) (D - k d)^2.5);
                // kibble (bulk ~600 kg/m3, d ~1 cm) through a 6 cm bag opening ~0.45 kg/s - a bumped bag no longer empties in half a second
                if (Granular) { float D = (NeckRadius > 0f ? NeckRadius : InnerRadius) * 2f; flux = Mathf.Min(flux, 0.58f * 600f * 3.132f * Mathf.Pow(Mathf.Max(0f, D - 0.015f), 2.5f)); }
                float out_ = Mathf.Min(Amount, flux * dt);
                if (Replica) { Pouring = out_ > 0f; out_ = 0f; }   // replica: stream visuals only, the authority moves the volume
                Amount -= out_; PouredOut += out_; _accumOut += out_; if (!Replica) Pouring = out_ > 0f;
                _stream.transform.position = rimLow; _stream.Velocity0 = (tiltDir.sqrMagnitude > 1e-6f ? tiltDir.normalized : Vector3.zero) * Mathf.Lerp(0.15f, 0.6f, Mathf.Clamp01(flux / 0.4f)) + vel;   // lip speed: a gentle pour leaves slowly
                _stream.TargetFlow = Mathf.Clamp01(flux / 0.35f);
                // where does it land? that receiver gets the volume
                if (Replica) { }
                else if (_stream.Hitting)
                {
                    LiquidContainer to = null;
                    foreach (var c in All) if (c != this && (c.InOpening(_stream.HitPoint) || c.HitsOwnFloor(_stream.HitCollider, _stream.HitPoint, _stream.HitNormal))) { to = c; break; }
                    LastReceiver = to != null ? (to.GetComponentInParent<PropBody>() != null ? to.GetComponentInParent<PropBody>().name : to.name) : "none";
                    if (to != null) { to.LastFeeder = PourerActor(); float over = to.Catch(out_, name); if (over > 0f) Puddle.Spill(_stream.HitPoint, over, Granular); }
                    else if (SinkBasin(_stream.HitPoint)) { /* drained */ }
                    else Puddle.Spill(_stream.HitPoint, out_, Granular);
                }
                else if (out_ > 0f)
                {
                    // the arc has not resolved a landing yet (still falling past its sensed length): trace straight down from its end and
                    // deliver to whatever is really below - a bowl / vessel opening, otherwise the floor (never dumped mid-air)
                    var end = _stream.HitPoint; LiquidContainer below = null;
                    var self = GetComponentInParent<PropBody>();
                    foreach (var h in Physics.RaycastAll(end + Vector3.up * 0.02f, Vector3.down, 3f, Layers.EnvironmentMask | Layers.PropsMask, QueryTriggerInteraction.Ignore))
                    {
                        if (self != null && h.collider.transform.IsChildOf(self.transform)) continue;
                        foreach (var c in All) if (c != this && (c.InOpening(h.point) || c.HitsOwnFloor(h.collider, h.point, h.normal))) { below = c; break; }
                        if (below != null) break;
                    }
                    if (below != null) { below.LastFeeder = PourerActor(); LastReceiver = below.name; float over = below.Catch(out_, name); if (over > 0f) Puddle.Spill(end, over, Granular); }
                    else Puddle.Spill(end, out_, Granular);
                }
            }
            else _stream.TargetFlow = 0f;
        }

        static bool SinkBasin(Vector3 p)
        {
            foreach (var t in Object.FindObjectsByType<SinkTapView>(FindObjectsSortMode.None)) { var l = t.transform.InverseTransformPoint(p); if (Mathf.Abs(l.x) < 0.35f && Mathf.Abs(l.z) < 0.2f && l.y > -0.05f && l.y < 0.3f) return true; }
            return false;
        }

        void LateUpdate()
        {
            if (_surface == null) return;
            bool show = Amount > 1e-4f && SurfaceWorldY > float.NegativeInfinity;
            if (_surface.gameObject.activeSelf != show) _surface.gameObject.SetActive(show);
            if (!show) return;
            var up = transform.up; float cos = Mathf.Max(0.05f, Vector3.Dot(up, Vector3.up));
            var axisPoint = transform.TransformPoint(new Vector3(0, FloorY, 0)); axisPoint += up * ((SurfaceWorldY - axisPoint.y) / cos);
            // keep the disc inside the vessel: world horizontal, ellipse = cylinder cross-section cut by a horizontal plane
            var rimTop = transform.TransformPoint(new Vector3(0, RimY, 0)).y;
            if (axisPoint.y > rimTop + 0.004f) axisPoint += up * ((rimTop - axisPoint.y) / cos);
            _surface.position = axisPoint;
            _surface.rotation = Quaternion.Euler(_sloshZ * 40f, 0f, -_sloshX * 40f);   // horizontal + slosh
            float r = InnerRadius * transform.lossyScale.x;
            _surface.localScale = Vector3.one; var ls = _surface.lossyScale;
            _surface.localScale = new Vector3(r * 2f / Mathf.Max(1e-4f, ls.x), 1f / Mathf.Max(1e-4f, ls.y), r * 2f / cos / Mathf.Max(1e-4f, ls.z));
            if (_stream != null && Tint.a > 0f) _stream.SetTint(Tint);
            if (Granular) return;   // the kibble pile keeps its painted material
            _mat.SetColor("_ShallowColor", Tint.a > 0f ? Tint : Soapy ? new Color(0.85f, 0.9f, 0.98f, 0.9f) : new Color(0.45f, 0.75f, 0.96f, 0.85f));
            if (Tint.a > 0f) _mat.SetColor("_DeepColor", Color.Lerp(Tint, Color.black, 0.25f));
        }

        static Material _kibble;
        /// <summary>Painted kibble pile: brown base with darker / lighter pellet dots (procedural, original).</summary>
        public static Material KibbleMaterial()
        {
            if (_kibble != null) return _kibble;
            const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "kibble" }; var px = new Color[N * N];
            var rng = new System.Random(31); var baseC = new Color(0.55f, 0.33f, 0.16f);
            for (int i = 0; i < px.Length; i++) px[i] = Color.Lerp(baseC, baseC * 0.8f, (float)rng.NextDouble() * 0.4f);
            for (int k = 0; k < 70; k++) { int cx = rng.Next(N), cy = rng.Next(N), r = 2 + rng.Next(3); var c = rng.NextDouble() < 0.5 ? new Color(0.72f, 0.47f, 0.24f) : new Color(0.38f, 0.21f, 0.1f); for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= r * r) px[((cy + y + N) % N) * N + (cx + x + N) % N] = c; }
            t.SetPixels(px); t.Apply(true);
            _kibble = new Material(Mats.Lit(Color.white, 0.1f)) { name = "SHHH_Kibble" }; _kibble.SetTexture("_BaseMap", t); _kibble.mainTexture = t; return _kibble;
        }

        static Mesh _disc;
        static Mesh Disc()
        {
            if (_disc != null) return _disc;
            const int N = 24; var v = new Vector3[N + 1]; var uv = new Vector2[N + 1]; var n = new Vector3[N + 1]; var t = new int[N * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f); n[0] = Vector3.up;
            for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2f / N; v[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f); uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f); n[i + 1] = Vector3.up; t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % N + 1; t[i * 3 + 2] = i + 1; }
            _disc = new Mesh { name = "liquid_surface_disc" }; _disc.vertices = v; _disc.uv = uv; _disc.normals = n; _disc.triangles = t; _disc.RecalculateBounds(); return _disc;
        }
    }
}
