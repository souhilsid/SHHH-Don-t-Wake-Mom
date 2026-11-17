using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V11 spilled liquid as persistent world state: litres that miss every vessel land on a real surface (ray down to furniture / floor) and
    /// grow a thin toon puddle there (2 mm film: radius from volume), merging with a nearby puddle on the same surface. Puddles persist until
    /// wiped/mopped (<see cref="Absorb"/>) or slowly dry; they are what wet-floor mopping and "spill" incidents act on.
    /// </summary>
    public sealed class Puddle : MonoBehaviour
    {
        public static readonly List<Puddle> All = new List<Puddle>();
        /// <summary>QA: litres that found no surface below (should stay 0 inside the house).</summary>
        public static float Lost;
        public const float FilmDepth = 0.002f, MergeDistance = 0.18f, DryLitresPerSecond = 0.00005f;
        public float Litres; public Vector3 Normal = Vector3.up;
        /// <summary>V12: free surface radius around the spill point (edge of a washer top, a counter) - beyond it liquid runs off.</summary>
        public float MaxRadius = 3f; Vector3 _edgeDir = Vector3.forward;
        Material _mat;

        public float Radius => Mathf.Sqrt((Litres / 1000f) / (Mathf.PI * FilmDepth));

        /// <summary>Spill litres at (or above) a world point; returns the puddle that received it (null if nothing below).</summary>
        public static Puddle Spill(Vector3 at, float litres) => Spill(at, litres, 0, false);
        public static Puddle Spill(Vector3 at, float litres, bool granular) => Spill(at, litres, 0, granular);
        /// <summary>V12: spilled dry food (kibble) - scattered pellets, no film, never merges with water.</summary>
        public bool Granular;
        readonly List<Transform> _pellets = new List<Transform>(); readonly List<Vector2> _pelletDirs = new List<Vector2>();

        static Puddle Spill(Vector3 at, float litres, int depth, bool granular = false)
        {
            if (litres <= 0f) return null;
            if (GameServices.HostStreamsLiquids) return null;   // online client: puddles come from the host (no double spills)
            if (!Physics.Raycast(at + Vector3.up * 0.05f, Vector3.down, out var hit, 3f, Layers.EnvironmentMask | Layers.PropsMask, QueryTriggerInteraction.Ignore))
            { Lost += litres; return null; }   // nothing below (outside the house): counted, never silently dropped
            Puddle best = null; float bd = MergeDistance;
            foreach (var p in All) { if (p.Granular != granular) continue; float d = Vector3.Distance(p.transform.position, hit.point); if (d < bd + p.Radius * 0.5f && Mathf.Abs(p.transform.position.y - hit.point.y) < 0.03f) { bd = d; best = p; } }
            if (best == null)
            {
                var g = new GameObject("puddle"); best = g.AddComponent<Puddle>(); best.transform.position = hit.point + hit.normal * 0.0015f;
                best.Normal = hit.normal; best.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, Random.value * 360f, 0);
                g.AddComponent<MeshFilter>().sharedMesh = Blob(); var r = g.AddComponent<MeshRenderer>();
                best._mat = ToonWaterStream.WaterMaterial(new Color(0.55f, 0.8f, 0.97f, 0.55f), new Color(0.3f, 0.6f, 0.9f, 0.75f), 0.6f); r.sharedMaterial = best._mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                best.MaxRadius = FreeRadius(hit.point, out best._edgeDir);
                if (granular) best.MakeGranular();
            }
            best.Litres += litres;
            // more than this surface can hold as a film: the rest runs over the nearest edge and falls to what is below
            float capL = Mathf.PI * best.MaxRadius * best.MaxRadius * FilmDepth * 1000f;
            if (best.Litres > capL && best.MaxRadius < 2.5f && depth < 3)   // after 3 run-offs the rest stays here (volume is never lost)
            {
                float over = best.Litres - capL; best.Litres = capL;
                var edge = best.transform.position + best._edgeDir * (best.MaxRadius + 0.05f);
                Spill(edge, over, depth + 1, granular);
            }
            return best;
        }

        /// <summary>How far the flat surface under a point extends (min over 12 directions; ray probes 2.5 cm apart, max 2.5 m).</summary>
        static float FreeRadius(Vector3 p, out Vector3 edgeDir)
        {
            float best = 2.5f; edgeDir = Vector3.forward;
            for (int i = 0; i < 12; i++)
            {
                var d = Quaternion.Euler(0, i * 30f, 0) * Vector3.forward;
                for (float r = 0.025f; r < best; r += 0.025f)
                {
                    var q = p + d * r + Vector3.up * 0.03f;
                    if (!Physics.Raycast(q, Vector3.down, out var h, 0.06f, Layers.EnvironmentMask | Layers.PropsMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.point.y - p.y) > 0.012f)
                    { if (r < best) { best = r; edgeDir = d; } break; }
                }
            }
            return Mathf.Max(0.03f, best - 0.01f);
        }

        /// <summary>A cloth / mop / sponge soaks up to <paramref name="litres"/>; returns how much it took.</summary>
        public float Absorb(float litres) { float t = Mathf.Min(litres, Litres); Litres -= t; return t; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>V12 online client: show the host's puddle i (create / update / remove).</summary>
        static readonly Puddle[] _replicas = new Puddle[32];
        bool _replica;
        public static void SyncFromHost(int i, Vector3 p, Vector3 n, float litres, float maxR, bool granular = false)
        {
            if (i < 0 || i >= _replicas.Length) return; var cur = _replicas[i];
            if (litres <= 0.0005f) { if (cur != null) Destroy(cur.gameObject); _replicas[i] = null; return; }
            if (cur == null)
            {
                var g = new GameObject("puddle_net" + i); cur = g.AddComponent<Puddle>(); cur._replica = true;
                g.AddComponent<MeshFilter>().sharedMesh = Blob(); var r = g.AddComponent<MeshRenderer>();
                cur._mat = ToonWaterStream.WaterMaterial(new Color(0.55f, 0.8f, 0.97f, 0.55f), new Color(0.3f, 0.6f, 0.9f, 0.75f), 0.6f); r.sharedMaterial = cur._mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _replicas[i] = cur; if (granular) cur.MakeGranular();
            }
            cur.transform.position = p; cur.transform.rotation = Quaternion.FromToRotation(Vector3.up, n.sqrMagnitude > 0.1f ? n : Vector3.up);
            cur.Normal = n; cur.Litres = litres; cur.MaxRadius = maxR;
        }

        /// <summary>Dry food: scattered painted pellets instead of a film; radius from a thicker heap (12 mm).</summary>
        public void MakeGranular()
        {
            Granular = true; var r = GetComponent<MeshRenderer>(); if (r != null) r.enabled = false;
            var m = Mats.Lit(new Color(0.6f, 0.36f, 0.17f), 0.2f); var rng = new System.Random(GetInstanceID());
            for (int i = 0; i < 26; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = Mathf.Sqrt((float)rng.NextDouble()) * 0.5f;
                g.transform.localPosition = new Vector3(Mathf.Cos(a) * d, 0.004f, Mathf.Sin(a) * d); g.transform.localScale = new Vector3(0.012f, 0.008f, 0.011f);
                g.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0); _pelletDirs.Add(new Vector2(Mathf.Cos(a) * d * 2f, Mathf.Sin(a) * d * 2f)); _pellets.Add(g.transform);
                g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void Update()
        {
            if (Granular)
            {
                // kibble does not dry; the heap spreads with its amount (pellets keep their size - no stretching)
                float rr = Mathf.Max(0.04f, Mathf.Sqrt((Litres / 1000f) / (Mathf.PI * 0.012f))); transform.localScale = Vector3.one;
                for (int i = 0; i < _pellets.Count; i++) _pellets[i].localPosition = new Vector3(_pelletDirs[i].x * rr, 0.004f, _pelletDirs[i].y * rr);
                return;
            }
            if (_replica) { float rr = Radius; transform.localScale = new Vector3(rr * 2f, 1f, rr * 2f); return; }   // the host dries / removes it
            Litres -= DryLitresPerSecond * Time.deltaTime;
            if (Litres <= 0.0005f) { Destroy(gameObject); return; }
            float r = Radius; transform.localScale = new Vector3(r * 2f, 1f, r * 2f);
        }

        static Mesh _blob;
        /// <summary>Irregular painted-edge disc (unit diameter).</summary>
        static Mesh Blob()
        {
            if (_blob != null) return _blob;
            const int N = 28; var v = new Vector3[N + 1]; var uv = new Vector2[N + 1]; var n = new Vector3[N + 1]; var t = new int[N * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f); n[0] = Vector3.up;
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N, k = 1f + 0.12f * Mathf.Sin(a * 3f + 0.7f) + 0.07f * Mathf.Sin(a * 7f + 2f);
                v[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f * k, 0, Mathf.Sin(a) * 0.5f * k); uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f); n[i + 1] = Vector3.up;
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % N + 1; t[i * 3 + 2] = i + 1;
            }
            _blob = new Mesh { name = "puddle_blob" }; _blob.vertices = v; _blob.uv = uv; _blob.normals = n; _blob.triangles = t; _blob.RecalculateBounds(); return _blob;
        }
    }
}
