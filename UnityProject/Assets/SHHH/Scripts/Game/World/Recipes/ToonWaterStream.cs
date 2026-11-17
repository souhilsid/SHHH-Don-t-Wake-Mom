using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V10 falling water (owner: "water must look like WATER, not a LineRenderer"). A procedurally extruded tube from the spout down to
    /// whatever the water really hits first (sphere-cast every physics step: plate, hand, sponge, basin floor, pooled water), rendered
    /// with the original <c>SHHH/ToonWater</c> shader (painted two-tone body, flow streaks running down, toon highlight, wobble).
    /// <list type="bullet">
    /// <item>Flow 0..1 comes from the valve angle: radius, fall speed, streak speed and sound follow it continuously.</item>
    /// <item>The column narrows as it accelerates (continuity: r ∝ sqrt(v0 / v)), and frays into a few drops below ~1.2 m/s at low flow.</item>
    /// <item>Impact: a foam crown (<c>SHHH/ToonFoam</c>) oriented to the hit surface + pooled ballistic droplets (no particle systems).</item>
    /// <item>Shut-off: the column's top detaches and falls under gravity, then 2-3 last drips leave the spout.</item>
    /// </list>
    /// Presentation only: what the water wets is reported (<see cref="HitCollider"/>, <see cref="HitProp"/>) for the authority's rinse
    /// validation, but every client derives the stream from the replicated valve level.
    /// </summary>
    public sealed class ToonWaterStream : MonoBehaviour
    {
        const int Rings = 14, Sides = 12, DropPool = 14;
        public float MaxFall = 1.0f, MaxRadius = 0.017f, MinRadius = 0.005f;   // V10 rev 2 (owner: "still a thin line"): a full tap reads as a ~3.4 cm column
        public float Flow { get; private set; }          // smoothed 0..1
        public float TargetFlow;                         // set by the owner each frame
        public bool Hitting { get; private set; }
        public Vector3 HitPoint { get; private set; }
        public Vector3 HitNormal { get; private set; } = Vector3.up;
        public Collider HitCollider { get; private set; }
        public PropBody HitProp { get; private set; }
        public float FallLength { get; private set; }
        public int LayerMask = ~0;
        public System.Func<Collider, bool> Ignore;      // e.g. the faucet's own colliders
        /// <summary>World height of standing water below the spout (basin pool), or -inf: the stream ends on its surface.</summary>
        public float SurfaceY = float.NegativeInfinity;
        /// <summary>V11: initial velocity of the water leaving the outlet (a jug lip, a tilted cup). Zero = a tap falling straight down.
        /// Non-zero = the stream follows the ballistic arc p0 + v0 t + g t^2 / 2 and is sensed along it.</summary>
        public Vector3 Velocity0;
        readonly List<Vector3> _path = new List<Vector3>();   // world points along the arc (arc mode)
        readonly List<float> _pathLen = new List<float>();

        /// <summary>V12: colour the stream (blue detergent, brown kibble) - set once, cheap to call every frame.</summary>
        public void SetTint(Color c) { if (_mat == null || _tint == c) return; _tint = c; _mat.SetColor("_ShallowColor", c); _mat.SetColor("_DeepColor", Color.Lerp(c, Color.black, 0.3f)); }
        Color _tint;
        /// <summary>V12 dry goods (kibble): the stream is tumbling pellets along the same physical arc, no water tube / foam / drips.</summary>
        public bool Granular;
        Transform[] _pellets; float _pelletT;
        void UpdatePellets(bool show)
        {
            if (_pellets == null)
            {
                _pellets = new Transform[16]; var m = Mats.Lit(_tint.a > 0f ? _tint : new Color(0.62f, 0.38f, 0.18f), 0.2f);
                for (int i = 0; i < _pellets.Length; i++) { var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "pellet"; g.transform.SetParent(transform, false); g.transform.localScale = new Vector3(0.012f, 0.009f, 0.011f); g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; g.SetActive(false); _pellets[i] = g.transform; }
            }
            _pelletT += Time.deltaTime * Mathf.Lerp(1.2f, 2.6f, Flow);
            int n = show ? Mathf.Clamp(Mathf.RoundToInt(_pellets.Length * Mathf.Lerp(0.35f, 1f, Flow)), 3, _pellets.Length) : 0;
            for (int i = 0; i < _pellets.Length; i++)
            {
                bool on = i < n; if (_pellets[i].gameObject.activeSelf != on) _pellets[i].gameObject.SetActive(on); if (!on) continue;
                float u = Mathf.Repeat(_pelletT + i * 0.618f, 1f), d = u * u * Mathf.Max(0.02f, FallLength);   // accelerate as they fall
                var c = Along(d, out var tan); var jitter = new Vector3(Mathf.Sin(i * 12.9f), 0, Mathf.Cos(i * 7.3f)) * 0.008f * (1f + u);
                _pellets[i].position = transform.position + c + jitter; _pellets[i].rotation = Quaternion.Euler(i * 47f + _pelletT * 300f, i * 31f, 0);
            }
        }
        Mesh _mesh; Vector3[] _v; Vector3[] _n; Vector2[] _uv; Color[] _col; Material _mat, _foamMat;
        Transform _crown; Mesh _crownMesh;
        readonly Transform[] _drops = new Transform[DropPool]; readonly Vector3[] _dropV = new Vector3[DropPool]; readonly float[] _dropLife = new float[DropPool];
        float _detach = -1f, _detachV, _dripT, _spawnAcc; int _drips;
        static Mesh _sphere;

        public static Material WaterMaterial(Color shallow, Color deep, float ripple)
        {
            var sh = Resources.Load<Shader>("Shaders/SHHH_ToonWater") ?? Shader.Find("SHHH/ToonWater");
            var m = sh != null ? new Material(sh) : Mats.Glow(new Color(0.55f, 0.8f, 1f, 0.5f));
            if (sh != null) { m.SetColor("_ShallowColor", shallow); m.SetColor("_DeepColor", deep); m.SetFloat("_Ripple", ripple); }
            m.name = ripple > 0 ? "SHHH_ToonWater_Surface" : "SHHH_ToonWater_Stream";
            return m;
        }
        public static Material FoamMaterial()
        {
            var sh = Resources.Load<Shader>("Shaders/SHHH_ToonFoam") ?? Shader.Find("SHHH/ToonFoam");
            var m = sh != null ? new Material(sh) : Mats.Lit(Color.white);
            m.name = "SHHH_ToonFoam"; return m;
        }

        void Awake()
        {
            _mesh = new Mesh { name = "toon_water_stream" }; _mesh.MarkDynamic();
            int nv = Rings * (Sides + 1);
            _v = new Vector3[nv]; _n = new Vector3[nv]; _uv = new Vector2[nv]; _col = new Color[nv];
            var tri = new List<int>();
            for (int k = 0; k < Rings - 1; k++) for (int j = 0; j < Sides; j++)
            {
                int a = k * (Sides + 1) + j, b = a + 1, c = a + Sides + 1, d = c + 1;
                tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
            }
            _mesh.vertices = _v; _mesh.triangles = tri.ToArray();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            _mat = WaterMaterial(new Color(0.52f, 0.82f, 0.98f, 0.78f), new Color(0.16f, 0.44f, 0.86f, 0.97f), 0f);
            _mat.SetFloat("_StreakAmount", 0.8f); _mat.SetFloat("_StreakScale", 5f); _mat.SetFloat("_HighlightWidth", 0.3f);
            mr.sharedMaterial = _mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            _foamMat = FoamMaterial();
            var cg = new GameObject("splash_crown"); cg.transform.SetParent(transform, false); _crown = cg.transform;
            cg.AddComponent<MeshFilter>().sharedMesh = _crownMesh = CrownMesh();
            var cr = cg.AddComponent<MeshRenderer>(); cr.sharedMaterial = _foamMat; cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (_sphere == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); _sphere = tmp.GetComponent<MeshFilter>().sharedMesh; Destroy(tmp); }
            var dropMat = WaterMaterial(new Color(0.85f, 0.97f, 1f, 0.75f), new Color(0.3f, 0.62f, 0.95f, 0.95f), 0f);
            for (int i = 0; i < DropPool; i++)
            {
                var d = new GameObject("drop" + i); d.transform.SetParent(transform, false);
                d.AddComponent<MeshFilter>().sharedMesh = _sphere; var r = d.AddComponent<MeshRenderer>(); r.sharedMaterial = dropMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d.SetActive(false); _drops[i] = d.transform;
            }
            GetComponent<MeshRenderer>().enabled = false; _crown.gameObject.SetActive(false);
        }

        /// <summary>Jagged open cone: base ring on the surface, rim flared up and out (a splash crown), uv.y = height fraction.</summary>
        static Mesh CrownMesh()
        {
            const int N = 16; var v = new List<Vector3>(); var tri = new List<int>(); var rnd = new System.Random(7);
            for (int i = 0; i <= N; i++)
            {
                float a = i * Mathf.PI * 2f / N; float h = (i % 2 == 0 ? 1f : 0.55f) * (0.8f + (float)rnd.NextDouble() * 0.4f); if (i == N) h = 1f;
                v.Add(new Vector3(Mathf.Cos(a) * 0.45f, 0f, Mathf.Sin(a) * 0.45f));
                v.Add(new Vector3(Mathf.Cos(a) * (0.75f + 0.25f * h), h, Mathf.Sin(a) * (0.75f + 0.25f * h)));
            }
            for (int i = 0; i < N; i++) { int a = i * 2; tri.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
            // a low dome of foam in the middle
            int c0 = v.Count; v.Add(new Vector3(0, 0.25f, 0));
            for (int i = 0; i < N; i++) tri.AddRange(new[] { c0, i * 2, (i + 1) * 2 });
            var m = new Mesh { name = "splash_crown" }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        void FixedUpdate() { Sense(); }

        /// <summary>What does the falling water hit first? (authority uses it for rinsing; every peer for the splash)</summary>
        void Sense()
        {
            Hitting = false; HitCollider = null; HitProp = null;
            if (Flow <= 0.001f && TargetFlow <= 0.001f) return;
            if (Velocity0.sqrMagnitude > 1e-4f) { SenseArc(); return; }
            _path.Clear();
            float r = Mathf.Lerp(MinRadius, MaxRadius, Flow) * 0.8f;
            float best = float.MaxValue; RaycastHit bh = default;
            // V10 rev 2: the spout sits inside the faucet's (house) mesh collider; a cast that starts inside a collider reports it once at
            // distance 0 and never its basin floor below - so re-cast from a little lower until the cast starts in free air
            foreach (float start in new[] { 0.006f, 0.035f, 0.07f })
            {
                var o = transform.position + Vector3.down * start; bool startedInside = false;
                foreach (var h in Physics.SphereCastAll(o, r, Vector3.down, MaxFall - start, LayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (Ignore != null && Ignore(h.collider)) continue;
                    if (h.distance <= 0f) { if (h.collider.GetComponentInParent<PropBody>() == null) startedInside = true; continue; }
                    if (h.distance + start < best) { best = h.distance + start; bh = h; }
                }
                if (!startedInside || best < float.MaxValue) break;
            }
            // standing water: the column ends on the pool surface (the floor below it is under water)
            float toSurface = transform.position.y - SurfaceY;
            if (toSurface > 0.01f && toSurface < best && (best == float.MaxValue || bh.collider.GetComponentInParent<PropBody>() == null))
            { Hitting = true; HitCollider = best < float.MaxValue ? bh.collider : null; HitPoint = new Vector3(transform.position.x, SurfaceY, transform.position.z); HitNormal = Vector3.up; FallLength = toSurface; HitProp = null; return; }
            if (best == float.MaxValue) { FallLength = MaxFall; HitPoint = transform.position + Vector3.down * MaxFall; return; }
            Hitting = true; HitCollider = bh.collider; HitPoint = bh.point; HitNormal = bh.normal; FallLength = Mathf.Max(0.01f, best);
            HitProp = bh.collider.GetComponentInParent<PropBody>();
            if (HitProp == null)
            {
                // water running onto a hand that holds something pours over onto what it holds (a plate held from above)
                var hi = bh.collider.GetComponentInParent<HandInteractor>();
                if (hi != null && hi.Held != null) HitProp = hi.Held;
            }
        }

        /// <summary>Arc mode: march the ballistic path in 15 ms steps, sphere-casting each segment; stop at the first real hit or MaxFall.</summary>
        void SenseArc()
        {
            _path.Clear(); _pathLen.Clear();
            var p = transform.position; var v = Velocity0; float r = Mathf.Lerp(MinRadius, MaxRadius, Flow) * 0.7f, len = 0f; const float dt = 0.015f;
            _path.Add(p); _pathLen.Add(0f);
            for (int i = 0; i < 90; i++)
            {
                var np = p + v * dt + 0.5f * Physics.gravity * dt * dt; v += Physics.gravity * dt;
                var seg = np - p; float sl = seg.magnitude; if (sl < 1e-5f) break;
                float best = float.MaxValue; RaycastHit bh = default;
                foreach (var h in Physics.SphereCastAll(p, r, seg / sl, sl, LayerMask, QueryTriggerInteraction.Ignore))
                { if (h.distance <= 0f || (Ignore != null && Ignore(h.collider))) continue; if (h.distance < best) { best = h.distance; bh = h; } }
                float toSurface = SurfaceY > float.NegativeInfinity && np.y < SurfaceY && p.y >= SurfaceY ? (p.y - SurfaceY) / Mathf.Max(1e-4f, p.y - np.y) * sl : float.MaxValue;
                if (best < float.MaxValue || toSurface < float.MaxValue)
                {
                    bool surf = toSurface < best;
                    var hp = surf ? p + seg / sl * toSurface : bh.point;
                    len += Vector3.Distance(p, hp); _path.Add(hp); _pathLen.Add(len);
                    Hitting = true; HitPoint = hp; HitNormal = surf ? Vector3.up : bh.normal; HitCollider = surf ? null : bh.collider; FallLength = len;
                    HitProp = HitCollider != null ? HitCollider.GetComponentInParent<PropBody>() : null;
                    return;
                }
                len += sl; p = np; _path.Add(p); _pathLen.Add(len);
                if (transform.position.y - p.y > MaxFall) break;
            }
            HitPoint = p; FallLength = len;
        }

        /// <summary>Stream centre (local to this transform, which never rotates) and direction at arc length d.</summary>
        Vector3 Along(float d, out Vector3 tangent)
        {
            if (_path.Count < 2) { tangent = Vector3.down; return Vector3.down * d; }
            for (int i = 1; i < _path.Count; i++)
                if (_pathLen[i] >= d || i == _path.Count - 1)
                {
                    float seg = Mathf.Max(1e-5f, _pathLen[i] - _pathLen[i - 1]); float t = Mathf.Clamp01((d - _pathLen[i - 1]) / seg);
                    tangent = (_path[i] - _path[i - 1]).normalized;
                    return Vector3.Lerp(_path[i - 1], _path[i], t) - transform.position;
                }
            tangent = Vector3.down; return Vector3.down * d;
        }

        void LateUpdate()
        {
            transform.rotation = Quaternion.identity;   // falls along world down, whatever the faucet does
            float prev = Flow;
            Flow = Mathf.MoveTowards(Flow, Mathf.Clamp01(TargetFlow), Time.deltaTime * 2.5f);
            if (prev > 0.02f && Flow <= 0.02f) { _detach = 0f; _detachV = Mathf.Lerp(0.4f, 1.1f, prev); _drips = 3; _dripT = 0.25f; }
            if (Flow > 0.02f) _detach = -1f;
            bool show = Flow > 0.02f || (_detach >= 0f && _detach < FallLength);
            var mr = GetComponent<MeshRenderer>();
            if (Granular) { if (mr.enabled) mr.enabled = false; if (_crown.gameObject.activeSelf) _crown.gameObject.SetActive(false); UpdatePellets(Flow > 0.02f); return; }
            if (mr.enabled != show) mr.enabled = show;
            if (show) BuildTube();
            UpdateCrown(show && _detach < 0f);
            UpdateDrops();
            if (_detach >= 0f) { _detachV += 9.81f * Time.deltaTime; _detach += _detachV * Time.deltaTime; }
        }

        void BuildTube()
        {
            float len = FallLength; float flow = Mathf.Max(Flow, 0.05f);
            float r0 = Mathf.Lerp(MinRadius, MaxRadius, Mathf.Sqrt(flow));
            float v0 = Mathf.Lerp(0.35f, 1.1f, flow);
            float top = _detach >= 0f ? Mathf.Min(_detach, len - 0.005f) : 0f;
            _mat.SetFloat("_FlowSpeed", Mathf.Lerp(0.8f, 2.2f, flow));
            _mat.SetFloat("_WobbleAmp", Mathf.Lerp(0.0012f, 0.0035f, flow));
            _mat.SetFloat("_WobbleFreq", 16f);
            for (int k = 0; k < Rings; k++)
            {
                float s = k / (float)(Rings - 1); float d = Mathf.Lerp(top, len, s);
                float v = Mathf.Sqrt(v0 * v0 + 2f * 9.81f * d);
                float r = r0 * Mathf.Pow(v0 / v, 0.22f);                              // thins as it speeds up (stylised: gently, it must read as a volume)
                // travelling bulges + a slightly uneven column (presentation; the sense/rinse truth uses the cast above)
                float tt = Time.time;
                r *= 1f + 0.16f * Mathf.Sin(d * 38f - tt * 17f) * Mathf.Clamp01(d / 0.08f) + 0.07f * Mathf.Sin(d * 91f - tt * 29f);
                if (k == 0 && _detach < 0f) r *= 0.92f;                             // aerator lip
                if (k == Rings - 1) r *= 1.15f;                                     // spreads where it lands
                float fade = _detach >= 0f ? Mathf.Clamp01(1f - s * 0.3f) : 1f;
                var centre = Along(d, out var tan);
                var b1 = Vector3.Cross(tan, Mathf.Abs(Vector3.Dot(tan, Vector3.forward)) < 0.95f ? Vector3.forward : Vector3.right).normalized; var b2 = Vector3.Cross(tan, b1);
                for (int j = 0; j <= Sides; j++)
                {
                    float a = j * Mathf.PI * 2f / Sides + d * 3.5f; var nrm = b1 * Mathf.Cos(a) + b2 * Mathf.Sin(a);   // ring around the (possibly arcing) axis, slight twist
                    int i = k * (Sides + 1) + j;
                    float lobe = 1f + 0.08f * Mathf.Sin(a * 3f + Time.time * 6f + d * 20f) * Mathf.Clamp01(d / 0.1f);       // uneven, liquid silhouette
                    _v[i] = centre + nrm * r * lobe; _n[i] = nrm; _uv[i] = new Vector2(j / (float)Sides, d);
                    _col[i] = new Color(fade, 1f, 1f, Mathf.Clamp01(d / 0.05f));      // r = alpha fade, a = wobble weight (none at the spout)
                }
            }
            _mesh.vertices = _v; _mesh.normals = _n; _mesh.uv = _uv; _mesh.colors = _col; _mesh.RecalculateBounds();
        }

        void UpdateCrown(bool on)
        {
            bool vis = on && Hitting;
            if (_crown.gameObject.activeSelf != vis) _crown.gameObject.SetActive(vis);
            if (!vis) return;
            float s = Mathf.Lerp(0.018f, 0.05f, Flow) * (1f + 0.08f * Mathf.Sin(Time.time * 23f));
            _crown.position = HitPoint + HitNormal * 0.002f;
            _crown.rotation = Quaternion.FromToRotation(Vector3.up, HitNormal);
            _crown.localScale = new Vector3(s, s * Mathf.Lerp(0.5f, 0.9f, Flow), s);
            // droplets thrown off the impact, rate ~ flow
            _spawnAcc += Time.deltaTime * Mathf.Lerp(6f, 26f, Flow);
            while (_spawnAcc >= 1f) { _spawnAcc -= 1f; Spawn(HitPoint + HitNormal * 0.004f, (Random.insideUnitSphere * 0.45f + HitNormal * 0.9f) * Mathf.Lerp(0.5f, 1.1f, Flow), Mathf.Lerp(0.0025f, 0.0045f, Flow)); }
        }

        void Spawn(Vector3 p, Vector3 v, float size)
        {
            for (int i = 0; i < DropPool; i++)
            {
                if (_dropLife[i] > 0f) continue;
                _dropLife[i] = 0.5f; _dropV[i] = v; var t = _drops[i]; t.gameObject.SetActive(true); t.position = p; t.localScale = Vector3.one * size * 2f; return;
            }
        }

        void UpdateDrops()
        {
            // last drips after the tap is closed
            if (_drips > 0 && Flow <= 0.02f) { _dripT -= Time.deltaTime; if (_dripT <= 0f) { _dripT = 0.35f + Random.value * 0.4f; _drips--; Spawn(transform.position + Vector3.down * 0.004f, Vector3.zero, 0.004f); } }
            for (int i = 0; i < DropPool; i++)
            {
                if (_dropLife[i] <= 0f) continue;
                _dropLife[i] -= Time.deltaTime; _dropV[i] += Physics.gravity * Time.deltaTime;
                var t = _drops[i]; t.position += _dropV[i] * Time.deltaTime;
                t.localScale = Vector3.one * t.localScale.x * 0.995f + new Vector3(0, Mathf.Clamp(_dropV[i].magnitude * 0.002f, 0f, 0.004f), 0); // stretch while falling
                if (_dropLife[i] <= 0f || t.position.y < HitPoint.y - 0.02f) { _dropLife[i] = 0f; t.gameObject.SetActive(false); }
            }
        }
    }
}
