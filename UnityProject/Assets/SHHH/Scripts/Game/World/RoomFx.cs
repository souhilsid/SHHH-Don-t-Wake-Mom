using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Presentation hooks for host gameplay events: material-specific impact sounds, 3D noise ripples that start AT the
    /// noisy object, cartoon stars, trap wobble clues, chore-complete sparkles and short in-world toasts.
    /// Everything is pooled and opaque-first to stay inside Quest 2 budgets.
    /// </summary>
    public sealed class RoomFx : MonoBehaviour
    {
        static RoomFx _i;
        sealed class Ripple { public Transform t; public Renderer r; public float age, life, maxScale; public Color c; }
        sealed class Toast { public TextMeshPro text; public float age, life; public Vector3 start; }
        readonly List<Ripple> _ripples = new List<Ripple>();
        readonly List<Toast> _toasts = new List<Toast>();
        readonly Dictionary<PropBody, Transform> _trapMarkers = new Dictionary<PropBody, Transform>();
        Mesh _ringMesh;

        void Awake() { _i = this; _ringMesh = BuildRing(48, 0.86f, 1f); }
        void OnDestroy() { if (_i == this) _i = null; }

        /// <summary>Online client: the host's physics made this noise; present it here (sound + rings) from the replicated event.</summary>
        public static void ImpactAt(Vector3 point, NoiseMaterial mat, float noise, string kind)
        {
            if (AudioBank.I != null)
            {
                string key = kind == "dog_toy" || kind == "cat_toy" ? "squeak" : AudioBank.MaterialKey(mat);
                AudioBank.I.Play(key, point, Mathf.Clamp01(0.2f + noise / 40f), 0.08f);
            }
            Rings(point, noise);
        }

        static void Rings(Vector3 point, float noise)
        {
            if (_i == null || noise < 1.5f) return;
            var col = noise >= 35f ? Palette.Danger : noise >= 12f ? Palette.Coral : Palette.Yellow;
            _i.SpawnRipple(point, Mathf.Lerp(0.4f, 2.6f, Mathf.InverseLerp(1.5f, 60f, noise)), col, 0.9f);
            if (noise >= 12f) _i.SpawnRipple(point, Mathf.Lerp(0.8f, 3.6f, Mathf.InverseLerp(12f, 80f, noise)), col, 1.3f);
            if (noise >= 20f) _i.ShowToast(point + Vector3.up * 0.35f, noise >= 45f ? "CRASH!!" : noise >= 30f ? "CLANG!" : "Clack!", col, 1.1f, 0.9f);
            if (noise >= 45f) Burst("pow", point + Vector3.up * 0.15f, 0.3f);
            else if (noise >= 20f) Burst("hit_yellow", point + Vector3.up * 0.08f, 0.25f);
            else if (noise >= 8f) Burst("toon_impact", point + Vector3.up * 0.05f, 0.18f, 1.5f);
        }

        public static void Impact(PropBody p, Vector3 point, float noise, float relSpeed)
        {
            if (AudioBank.I != null && ImpactGate.TakeVoice())
            {
                string key = AudioBank.MaterialKey(p.Spec.Material);
                float vol = Mathf.Clamp01(0.15f + relSpeed / 4.5f);
                if (p.Spec.Kind == "dog_toy" || p.Spec.Kind == "cat_toy") key = "squeak";
                AudioBank.I.Play(key, point, vol, 0.08f);
            }
            if (_i == null || noise < 1.5f) return;
            var col = noise >= 35f ? Palette.Danger : noise >= 12f ? Palette.Coral : Palette.Yellow;
            _i.SpawnRipple(point, Mathf.Lerp(0.4f, 2.6f, Mathf.InverseLerp(1.5f, 60f, noise)), col, 0.9f);
            if (noise >= 12f) _i.SpawnRipple(point, Mathf.Lerp(0.8f, 3.6f, Mathf.InverseLerp(12f, 80f, noise)), col, 1.3f);
            if (noise >= 20f) _i.ShowToast(point + Vector3.up * 0.35f, noise >= 45f ? "CRASH!!" : noise >= 30f ? "CLANG!" : "Clack!", col, 1.1f, 0.9f);
            if (noise >= 45f) Burst("pow", point + Vector3.up * 0.15f, 0.3f);
            else if (noise >= 20f) Burst("hit_yellow", point + Vector3.up * 0.08f, 0.25f);
            else if (noise >= 8f) Burst("toon_impact", point + Vector3.up * 0.05f, 0.18f, 1.5f);
        }

        /// <summary>
        /// Comic burst from the audited Asset Store packs (Cartoon FX Remaster Free, VFX Impact &amp; Hit Light) copied to Resources/VFX.
        /// Scaled down for VR, rate-limited, auto-destroyed. Missing prefab = silently skipped (in-house ripples still show).
        /// </summary>
        public static void Burst(string key, Vector3 pos, float scale = 0.35f, float life = 2.5f)
        {
            if (_i == null) return;
            if (Time.time < _i._burstCooldownUntil) return;
            if (!_i._vfx.TryGetValue(key, out var prefab)) { prefab = Resources.Load<GameObject>("VFX/vfx_" + key); _i._vfx[key] = prefab; }
            if (prefab == null) return;
            _i._burstCooldownUntil = Time.time + 0.08f;
            var g = Instantiate(prefab, pos, Quaternion.identity, _i.transform);
            g.transform.localScale = Vector3.one * scale;
            var cam = Camera.main; if (cam != null) { var f = pos - cam.transform.position; f.y = 0; if (f.sqrMagnitude > 1e-4f) g.transform.rotation = Quaternion.LookRotation(f); }
            foreach (var ps in g.GetComponentsInChildren<ParticleSystem>()) { var m = ps.main; m.scalingMode = ParticleSystemScalingMode.Hierarchy; }
            foreach (var a in g.GetComponentsInChildren<AudioSource>()) a.enabled = false; // SHHH owns all audio
            Destroy(g, life);
        }
        readonly Dictionary<string, GameObject> _vfx = new Dictionary<string, GameObject>();
        float _burstCooldownUntil;

        public static void TrapArmed(PropBody p)
        {
            if (_i == null) return;
            var m = new GameObject("trapClue").transform;
            m.SetParent(p.transform, false); m.localPosition = Vector3.up * 0.02f;
            var star = Prim.Shape(PrimitiveType.Sphere, m, "wobble", Vector3.up * 0.18f, Vector3.one * 0.035f, Mats.Unlit(Palette.Coral));
            star.AddComponent<WobbleClue>();
            _i._trapMarkers[p] = m;
            AudioBank.I?.Play("trap_set", p.transform.position, 0.35f);
        }

        public static void TrapCleared(PropBody p)
        {
            if (_i == null) return;
            if (_i._trapMarkers.TryGetValue(p, out var m) && m != null) Destroy(m.gameObject);
            _i._trapMarkers.Remove(p);
        }

        public static void PetBump(PropBody p) { }

        public static void StepDone(AnchorFixture a, ChoreInstance c, int actor)
        {
            if (_i == null) return;
            AudioBank.I?.Play(c != null && c.State == ChoreState.Completed ? "chore_done" : "step_done", a.transform.position, 0.5f, 0.03f, AudioBus.UI);
            _i.SpawnRipple(a.transform.position + Vector3.up * 0.02f, 0.35f, Palette.Ok, 0.6f);
            string k = a.Spec != null ? a.Spec.AcceptsPropKind : "";
            if (k == "sponge" || k == "plate" || k == "shirt" || k == "towel" || k == "sock" || k == "mug") Burst("bubbles", a.transform.position + Vector3.up * 0.1f, 0.35f, 2f);
            else Burst("toon_impact", a.transform.position + Vector3.up * 0.06f, 0.12f, 1.2f);
            if (c != null && c.State == ChoreState.Completed) { _i._burstCooldownUntil = 0f; Burst("wow", a.transform.position + Vector3.up * 0.35f, 0.25f); }
            if (c != null && c.State == ChoreState.Completed)
                _i.ShowToast(a.transform.position + Vector3.up * 0.3f, "Done: " + c.Title, Palette.Ok, 1.6f, 0.6f);
        }

        public static void Say(Vector3 pos, string text, Color c, float seconds = 2f, float size = 0.8f) { if (_i != null) _i.ShowToast(pos, text, c, seconds, size); }

        void SpawnRipple(Vector3 pos, float maxScale, Color c, float life)
        {
            Ripple r = null;
            foreach (var x in _ripples) if (x.age >= x.life) { r = x; break; }
            if (r == null)
            {
                if (_ripples.Count >= 16) return;
                var g = new GameObject("ripple"); g.transform.SetParent(transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = _ringMesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r = new Ripple { t = g.transform, r = mr }; _ripples.Add(r);
            }
            // V12 (owner, Quest c15: a big yellow ring covered the view around the book in my hand): a ripple faces the camera and grew to
            // 2.6 m - from something 30 cm from my eyes it surrounded the whole view. Its size is capped by its distance from my eyes and
            // it fades when it is that close.
            var cam = Camera.main; float camD = cam != null ? Vector3.Distance(cam.transform.position, pos) : 3f;
            maxScale = Mathf.Min(maxScale, Mathf.Max(0.12f, camD * 0.55f));
            float near = Mathf.InverseLerp(0.35f, 1.2f, camD);
            r.t.gameObject.SetActive(true);
            r.t.position = pos; r.age = 0; r.life = life; r.maxScale = maxScale; r.c = new Color(c.r, c.g, c.b, Mathf.Lerp(0.25f, 0.85f, near));
            r.t.rotation = cam != null ? Quaternion.LookRotation(r.t.position - cam.transform.position) : Quaternion.identity;
            if (r.r.sharedMaterial == null || !r.r.sharedMaterial.name.StartsWith("ripple")) r.r.sharedMaterial = new Material(Mats.Glow(Color.white)) { name = "ripple" };
            r.r.sharedMaterial.SetColor("_BaseColor", new Color(c.r, c.g, c.b, Mathf.Lerp(0.25f, 0.85f, near)));
        }

        void ShowToast(Vector3 pos, string text, Color c, float life, float size)
        {
            Toast t = null;
            foreach (var x in _toasts) if (x.age >= x.life) { t = x; break; }
            if (t == null)
            {
                if (_toasts.Count >= 8) return;
                t = new Toast { text = WorldText.Make(transform, "toast", "", Vector3.zero, size, c, TextAlignmentOptions.Center, 1.4f) };
                _toasts.Add(t);
            }
            // Declutter: never stack a toast on top of a live one (or of the same text) - lift it until it has its own space.
            for (int guard = 0; guard < 6; guard++)
            {
                bool clash = false;
                foreach (var x in _toasts)
                {
                    if (x == t || x.age >= x.life) continue;
                    if (x.text.text == text && (x.start - pos).sqrMagnitude < 1f) return;             // duplicate spam
                    var d = x.text.transform.position - pos;
                    if (Mathf.Abs(d.y) < 0.16f * Mathf.Max(size, x.text.fontSize) && new Vector2(d.x, d.z).magnitude < 0.45f) { clash = true; break; }
                }
                if (!clash) break;
                pos += Vector3.up * 0.17f * size;
            }
            t.text.gameObject.SetActive(true);
            t.text.text = text; t.text.color = c; t.text.fontSize = size;
            t.age = 0; t.life = life; t.start = pos; t.text.transform.position = pos;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var r in _ripples)
            {
                if (r.age >= r.life) { if (r.t.gameObject.activeSelf) r.t.gameObject.SetActive(false); continue; }
                r.age += dt; float k = r.age / r.life;
                r.t.localScale = Vector3.one * Mathf.Lerp(0.05f, r.maxScale, 1f - (1f - k) * (1f - k));
                var c = r.c; c.a = r.c.a * (1f - k);
                r.r.sharedMaterial.SetColor("_BaseColor", c);
            }
            foreach (var t in _toasts)
            {
                if (t.age >= t.life) { if (t.text.gameObject.activeSelf) t.text.gameObject.SetActive(false); continue; }
                t.age += dt;
                t.text.transform.position = t.start + Vector3.up * (t.age * 0.25f);
                WorldText.Billboard(t.text.transform);
                t.text.alpha = Mathf.Clamp01((t.life - t.age) / 0.35f);
            }
        }

        static Mesh BuildRing(int seg, float inner, float outer)
        {
            var m = new Mesh { name = "ring" };
            var v = new Vector3[seg * 2]; var tri = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v[i * 2] = d * inner; v[i * 2 + 1] = d * outer;
                int n = (i + 1) % seg; int k = i * 6;
                tri[k] = i * 2; tri[k + 1] = n * 2; tri[k + 2] = i * 2 + 1;
                tri[k + 3] = i * 2 + 1; tri[k + 4] = n * 2; tri[k + 5] = n * 2 + 1;
            }
            m.vertices = v; m.triangles = tri; m.RecalculateBounds();
            return m;
        }
    }
}
