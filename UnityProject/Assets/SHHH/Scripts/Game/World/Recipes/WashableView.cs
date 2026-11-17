using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V10 washable plate (KT01). The stains are WORLD STATE: <see cref="RProp.Dirt"/> is an 8x8 grid over the eating face, replicated to every
    /// peer, cleared only by the <c>Scrub</c> rule (soapy sponge in hand, real contact - see <see cref="RecipeRuntime"/>). This view paints it:
    /// a per-plate 64x64 hand-painted stain texture (sauce body, darker painted rim, glossy dabs, crumbs) generated from the grid, eroding
    /// cell by cell (and, on the authority, continuously inside a cell while the sponge works on it). Suds appear where it was scrubbed and melt
    /// in the running water; a clean plate sparkles and drips. Owner directive V10 G1: "brown food disappears along its local path".
    /// </summary>
    public sealed class WashableView : MonoBehaviour
    {
        public const int Grid = 8, Tex = 64;
        RecipeRuntime _rt; int _id; PropBody _b;
        Texture2D _tex; Material _mat; GameObject _quad; Color32[] _px; static float[] _noise;
        readonly float[] _work = new float[Grid * Grid];            // authority: scrub work inside each cell (0..1)
        readonly List<Transform> _suds = new List<Transform>(); Material _foam;
        ulong _shownDirt = ulong.MaxValue; int _shown = -1; float _drip, _rinse, _repaint; bool _workDirty;
        Color _sauce, _sauceDark, _sauceGloss;

        /// <summary>Eating-face plane in the plate's local space: centre, radius of the stain grid.</summary>
        public Vector3 FaceCenterLocal { get; private set; }
        public float Radius { get; private set; }
        public Vector3 FaceNormal => transform.up;
        /// <summary>QA: the stain decal state.</summary>
        public string Debug_State()
        {
            int opaque = 0; if (_px != null) foreach (var c in _px) if (c.a > 0) opaque++;
            var r = _quad != null ? _quad.GetComponent<Renderer>() : null;
            return $"quad {(_quad == null ? "none" : _quad.activeInHierarchy ? "active" : "inactive")}, renderer {(r != null && r.enabled)}, shader {(r != null && r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")}, opaque px {opaque}/{Tex * Tex}, quad y {(r != null ? r.bounds.center.y : 0f):0.000} vs collider top {(_b != null ? _b.Col.bounds.max.y : 0f):0.000}, face local {FaceCenterLocal:F3} r {Radius:0.000}, lossy {transform.lossyScale:F2}";
        }

        public void Init(RecipeRuntime rt, int recipePropId) { _rt = rt; _id = recipePropId; _b = GetComponent<PropBody>(); Build(); }

        /// <summary>Legacy hook (whole-plate scrub progress) - the grid replaces it.</summary>
        public void SetScrub(float k) { }
        public void SetRinse(float k) => _rinse = Mathf.Clamp01(k);

        /// <summary>World centre of a grid cell on the eating face.</summary>
        public Vector3 CellWorld(int cell) => transform.TransformPoint(CellLocal(cell));
        public Vector3 CellLocal(int cell) { int r = cell / Grid, c = cell % Grid; return FaceCenterLocal + new Vector3(((c + 0.5f) / Grid * 2f - 1f) * Radius, 0f, ((r + 0.5f) / Grid * 2f - 1f) * Radius); }
        /// <summary>Plate-local point -> fractional grid coordinates (col, row).</summary>
        public Vector2 ToGrid(Vector3 local) => new Vector2(((local.x - FaceCenterLocal.x) / Radius * 0.5f + 0.5f) * Grid - 0.5f, ((local.z - FaceCenterLocal.z) / Radius * 0.5f + 0.5f) * Grid - 0.5f);
        public float Work(int cell) => _work[cell];
        public void AddWork(int cell, float w) { _work[cell] = Mathf.Clamp01(_work[cell] + w); _workDirty = true; }
        public void ResetWork() { System.Array.Clear(_work, 0, _work.Length); _workDirty = true; }

        void Build()
        {
            if (_b == null || _b.Col == null) return;
            // the collider in local space: thin axis = local Y (plates are flat discs; PropBody builds the disk around local Y)
            var lb = new Bounds(transform.InverseTransformPoint(_b.Col.bounds.center), Vector3.zero);
            if (_b.Col is MeshCollider mc && mc.sharedMesh != null) lb = mc.sharedMesh.bounds;
            else if (_b.Col is BoxCollider bc) lb = new Bounds(bc.center, bc.size);
            FaceCenterLocal = new Vector3(lb.center.x, lb.max.y + 0.0015f, lb.center.z);
            Radius = Mathf.Min(lb.extents.x, lb.extents.z) * 0.78f;   // the well of the plate, inside the rim
            Color[] sauces = { new Color(0.80f, 0.28f, 0.14f), new Color(0.56f, 0.34f, 0.16f), new Color(0.88f, 0.64f, 0.18f) };
            _sauce = sauces[_id % sauces.Length]; _sauceDark = Color.Lerp(_sauce, new Color(0.25f, 0.12f, 0.08f), 0.45f); _sauceGloss = Color.Lerp(_sauce, new Color(1f, 0.95f, 0.85f), 0.45f);
            _tex = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false) { name = "shhh_plate_stains_" + _id, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            _px = new Color32[Tex * Tex];
            if (_noise == null) { _noise = new float[Tex * Tex]; var rng = new System.Random(1234); for (int i = 0; i < _noise.Length; i++) _noise[i] = (float)rng.NextDouble(); }
            _mat = new Material(UiKit.Cutout(_tex)); _mat.SetFloat("_Cutoff", 0.5f);
            _quad = UiKit.Quad(transform, "stains", _mat, FaceCenterLocal, new Vector2(Radius * 2f, Radius * 2f));
            // UiKit's quad is visible from its -Z side; Euler(90,0,0) turns that side UP (the eating face). Quad +y -> plate +z (rows).
            // (V11 note: a "fix" to -90 was wrong and hid the stains from above - reverted after the face-on capture showed it.)
            _quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _foam = new Material(ToonWaterStream.FoamMaterial());
        }

        /// <summary>Paint the stain texture from the grid (+ authority work). Painterly: a smooth field over the dirty cells, noisy threshold edge,
        /// darker rim, glossy dabs, a few crumbs.</summary>
        void Paint(ulong dirt)
        {
            float cellPx = Tex / (float)Grid;
            for (int py = 0; py < Tex; py++)
                for (int px = 0; px < Tex; px++)
                {
                    float gx = (px + 0.5f) / cellPx - 0.5f, gy = (py + 0.5f) / cellPx - 0.5f;
                    float field = 0f;
                    int c0 = Mathf.Clamp(Mathf.FloorToInt(gx) - 1, 0, Grid - 1), c1 = Mathf.Clamp(Mathf.FloorToInt(gx) + 2, 0, Grid - 1);
                    int r0 = Mathf.Clamp(Mathf.FloorToInt(gy) - 1, 0, Grid - 1), r1 = Mathf.Clamp(Mathf.FloorToInt(gy) + 2, 0, Grid - 1);
                    for (int r = r0; r <= r1; r++) for (int c = c0; c <= c1; c++)
                    {
                        int cell = r * Grid + c; if ((dirt & (1UL << cell)) == 0) continue;
                        float w = 1f - _work[cell] * 0.85f; float dx = gx - c, dy = gy - r; float d2 = dx * dx + dy * dy;
                        field += w * Mathf.Exp(-d2 * 1.6f);
                    }
                    float n = _noise[py * Tex + px], n2 = _noise[((py * 7 + 13) % Tex) * Tex + (px * 5 + 3) % Tex];
                    float edge = field + (n - 0.5f) * 0.35f;
                    // round plate well only
                    float ux = (px + 0.5f) / Tex * 2f - 1f, uy = (py + 0.5f) / Tex * 2f - 1f; if (ux * ux + uy * uy > 1f) edge = 0f;
                    Color col; byte a;
                    if (edge < 0.55f) { col = _sauce; a = 0; }
                    else
                    {
                        a = 255;
                        float rim = Mathf.InverseLerp(0.55f, 0.75f, edge);                 // 0 at the painted edge -> 1 inside
                        col = Color.Lerp(_sauceDark, _sauce, rim);
                        if (rim > 0.9f && n2 > 0.86f) col = _sauceGloss;                   // glossy dabs
                        if (n > 0.985f) col = new Color(0.3f, 0.18f, 0.1f);                // crumbs
                        col = Color.Lerp(col, col * 0.9f, n * 0.25f);
                    }
                    _px[py * Tex + px] = new Color32((byte)(Mathf.Clamp01(col.r) * 255), (byte)(Mathf.Clamp01(col.g) * 255), (byte)(Mathf.Clamp01(col.b) * 255), a);
                }
            _tex.SetPixels32(_px); _tex.Apply(false, false);
        }

        void Suds(ulong scrubbed)
        {
            // foam where it was scrubbed (cells that were stained at the start and are clean now), max 14 blobs
            int k = 0;
            for (int cell = 0; cell < Grid * Grid && k < 14; cell++)
            {
                if ((scrubbed & (1UL << cell)) == 0) continue;
                Transform t;
                if (k < _suds.Count) t = _suds[k];
                else
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "suds" + k;
                    g.transform.SetParent(transform, false); g.GetComponent<Renderer>().sharedMaterial = _foam; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    t = g.transform; _suds.Add(t);
                }
                float s = Radius * (0.16f + 0.08f * ((cell * 37) % 7) / 7f);
                t.localPosition = CellLocal(cell) + new Vector3(0, s * 0.2f, 0); t.localScale = new Vector3(s, s * 0.45f, s); t.gameObject.SetActive(true); k++;
            }
            for (int i = k; i < _suds.Count; i++) _suds[i].gameObject.SetActive(false);
        }

        ulong _startDirt;
        void Update()
        {
            var p = _rt != null && _rt.World != null ? _rt.World.Prop(_id) : null; if (p == null || _tex == null) return;
            if (p.Stage == 0 && p.Dirt != 0 && (_startDirt == 0 || (p.Dirt & ~_startDirt) != 0)) _startDirt |= p.Dirt;   // remembers the original stains (smudge adds new ones)
            int stage = Mathf.Clamp(p.Stage, 0, 2);
            _repaint -= Time.deltaTime;
            if (p.Dirt != _shownDirt || (_workDirty && _repaint <= 0f))
            {
                if (p.Dirt != _shownDirt && _shownDirt != ulong.MaxValue && WashBits(_shownDirt & ~p.Dirt) > 0) { var at = _b.Col.bounds.center; AudioBank.I?.Play("sponge", at, 0.3f, 0.12f); }
                Paint(p.Dirt); _shownDirt = p.Dirt; _workDirty = false; _repaint = 0.08f;
                _quad.SetActive(p.Dirt != 0);
                if (stage <= 1) Suds(_startDirt & ~p.Dirt);
            }
            if (stage != _shown)
            {
                if (_shown >= 0 && stage > _shown)
                {
                    var at = _b != null ? _b.Col.bounds.center : transform.position;
                    if (stage == 1) { AudioBank.I?.Play("sponge", at, 0.45f); RoomFx.Burst("bubbles", at + Vector3.up * 0.04f, 0.14f, 1f); }
                    if (stage == 2) { AudioBank.I?.Play("rinse", at, 0.45f); RoomFx.Burst("sparkle", at + Vector3.up * 0.05f, 0.14f, 1f); _drip = 3f; }
                }
                if (stage == 0 && _shown > 0) { ResetWork(); _startDirt = p.Dirt; }
                _shown = stage; _rinse = 0f;
                if (stage == 2) foreach (var t in _suds) t.gameObject.SetActive(false);
            }
            if (stage == 1) for (int i = 0; i < _suds.Count; i++) { if (!_suds[i].gameObject.activeSelf && _rinse <= 0f) continue; float k = 1f - _rinse * (0.6f + 0.06f * i); if (_rinse > 0f) _suds[i].gameObject.SetActive(k > 0.15f); }
            if (_drip > 0f)
            {
                _drip -= Time.deltaTime;
                if (Random.value < Time.deltaTime * 3f && _b != null) RoomFx.Burst("bubbles", _b.Col.bounds.center + Vector3.down * _b.Col.bounds.extents.y, 0.04f, 0.6f);
            }
            _rinse = Mathf.MoveTowards(_rinse, 0f, 0.3f * Time.deltaTime);
        }

        static int WashBits(ulong v) => RecipeWorld.CountBits(v);
    }
}
