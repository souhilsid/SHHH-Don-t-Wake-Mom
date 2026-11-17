using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// KT01 dishwashing sink (the "sink_tap" recipe fixture). V10 rebuild (owner: no poke buttons, no line water):
    /// <list type="bullet">
    /// <item><b>Valve</b>: the mixer lever is a 1-DOF mechanism. A hand (local, remote via Photon poses, or a bot's reach) that touches the
    /// lever tip drags it along its real arc between its stops; the opening stays where the hand leaves it (friction). Valve 0..1 =
    /// continuous flow. No hidden button: the old "Tap on/off" poke key is gone.</item>
    /// <item><b>Rules</b>: the authority mirrors the valve into the replicated fixture (<see cref="RFixture.Open"/> + <see cref="RFixture.Level"/>
    /// 0..100 via the Open op's Arg) in 10 % bands, so the rinse rules (needs running water), Mom's noise and every client agree.</item>
    /// <item><b>Water</b>: <see cref="ToonWaterStream"/> (tube mesh + toon shader, real first-hit point, splash crown, droplets, shut-off
    /// drips) and a finite basin volume (inflow from the valve, sqrt-law drain) shown as a rippling toon surface.</item>
    /// <item><b>Rinse truth</b>: a prop is wetted only when the stream's first hit IS that prop (or the hand holding it) — never by
    /// being near the faucet. Rinse speed scales with flow.</item>
    /// </list>
    /// </summary>
    public sealed class SinkTapView : MonoBehaviour
    {
        RecipeFixtureView _fv; RecipeRuntime _rt;
        Transform _lever, _pool; Material _poolMat; ToonWaterStream _stream;
        AudioSource _loop;
        float _w, _d, _baseH, _noiseT, _hitT, _splashT, _syncT; bool _wasOpen, _init;
        Vector3 _spoutLocal; float _leverAngle, _maxAngle = 35f; bool _leverAxisTilt; Vector3 _tipLocal;
        float _valve; int _lastSent = -1; int _engagedActor = ActorIds.None;
        float _volume; Vector3 _poolSize;   // basin water (m^3), pool footprint (m)
        const float FullFlowM3s = 1.2e-4f, PoolEqDepth = 0.012f, PoolArea = 0.06f;   // 0.12 L/s; ~1.2 cm standing water at full flow

        /// <summary>Replicated opening 0..1 (all peers).</summary>
        public float Level { get { var st = _fv != null ? _fv.State : null; return st == null || !st.Open ? 0f : Mathf.Clamp01((st.Level > 0 ? st.Level : 100) / 100f); } }
        /// <summary>Current flow 0..1 for presentation and rinse rate (authority: the live valve; clients: the replicated level).</summary>
        public float Flow => GameServices.IsAuthority ? (Running ? Mathf.Max(_valve, 0.1f) : 0f) : Level;
        public bool Running { get { var st = _fv != null ? _fv.State : null; return st != null && st.Open; } }
        public Vector3 SpoutWorld => transform.TransformPoint(_spoutLocal);
        public Vector3 BasinBottomWorld => transform.TransformPoint(new Vector3(_spoutLocal.x, _baseH + 0.012f, _spoutLocal.z));
        public Vector3 LeverWorld => _lever != null ? _lever.position : transform.position;
        /// <summary>QA / hint: the point in the stream where a plate is rinsed.</summary>
        public Vector3 RinsePoint => Vector3.Lerp(SpoutWorld, BasinBottomWorld, 0.55f);
        public ToonWaterStream Stream => _stream;
        public float BasinLitres => _volume * 1000f;

        /// <summary>World position of the lever tip at opening t (0 = closed stop, 1 = fully open stop).</summary>
        public Vector3 LeverTipAt(float t)
        {
            if (_lever == null) return transform.position;
            var rot = _leverAxisTilt ? Quaternion.Euler(-_maxAngle * Mathf.Clamp01(t), 0, 0) : Quaternion.Euler(0, 0, _maxAngle * Mathf.Clamp01(t));
            return _lever.parent.TransformPoint(_lever.localPosition + rot * _tipLocal);
        }

        /// <summary>V9: the fixture IS the house's kitchen sink. Only the working parts are added: the daycare KIT015 mixer lever beside the
        /// faucet, the stream, the basin water.</summary>
        public void BuildAnchored(RecipeFixtureView fv, RecipeRuntime rt, float w, float d, Color owner)
        {
            _fv = fv; _rt = rt; _w = w; _d = d; _baseH = 0f;
            // V10: house sink stretched to a 0.80 m worktop (HouseAdditions): old measured spout 0.43 above a 0.36 floor scales with it
            float kH = RecipeLayout.WorktopY / HouseAdditions.BakedSinkWorktop;
            _spoutLocal = new Vector3(0f, (0.36f + 0.43f) * kH - 0.36f * kH, -0.06f);
            var am = ArticulatedModel.Build("dc_kit015", transform, "kit015_source", 0.78f, Layers.Furniture);
            var lever = am.Part("PART_MixerLever");
            var piv = new GameObject("lever_pivot").transform; piv.SetParent(transform, false); piv.localPosition = new Vector3(-0.08f, (0.36f + 0.15f) * kH - 0.36f * kH, -0.31f);
            if (lever != null) { lever.SetParent(piv, true); lever.localPosition = Vector3.zero; _leverAxisTilt = true; _maxAngle = 35f; _tipLocal = new Vector3(0, 0.02f, 0.03f); }
            else
            {
                Prim.Box(piv, "lever_arm", new Vector3(0.035f, 0, 0), new Vector3(0.07f, 0.012f, 0.016f), Mats.Lit(Color.Lerp(Palette.Cream, owner, 0.5f)), false);
                Prim.Shape(PrimitiveType.Sphere, piv, "lever_tip", new Vector3(0.07f, 0, 0), Vector3.one * 0.022f, Mats.Lit(owner));
                _maxAngle = 55f; _tipLocal = new Vector3(0.06f, 0, 0);
            }
            if (am.Root != null) Object.Destroy(am.Root.gameObject); // only the lever is used; the house provides basin + faucet
            _lever = piv;
            BuildWater(new Vector3(w - 0.08f, 0.002f, d - 0.06f));
        }

        public void Build(RecipeFixtureView fv, RecipeRuntime rt, float w, float d, float baseH, Color owner)
        {
            _fv = fv; _rt = rt; _w = w; _d = d; _baseH = baseH;
            var steel = Mats.Lit(new Color(0.78f, 0.81f, 0.86f), 0.75f);
            var dark = Mats.Lit(new Color(0.42f, 0.46f, 0.52f), 0.6f);
            const float wall = 0.012f, h = 0.085f;
            foreach (var (pos, size) in new[] {
                (new Vector3(-w * 0.5f + wall * 0.5f, baseH + h * 0.5f, 0), new Vector3(wall, h, d)), (new Vector3(w * 0.5f - wall * 0.5f, baseH + h * 0.5f, 0), new Vector3(wall, h, d)),
                (new Vector3(0, baseH + h * 0.5f, -d * 0.5f + wall * 0.5f), new Vector3(w, h, wall)), (new Vector3(0, baseH + h * 0.5f, d * 0.5f - wall * 0.5f), new Vector3(w, h, wall)) })
            { var g = Prim.Box(transform, "basin_wall", pos, size, steel); g.layer = Layers.Furniture; }
            Prim.Box(transform, "basin_floor", new Vector3(0, baseH + 0.002f, 0), new Vector3(w - 2 * wall, 0.004f, d - 2 * wall), dark, false);
            Prim.Shape(PrimitiveType.Cylinder, transform, "drain", new Vector3(0, baseH + 0.0045f, 0.02f), new Vector3(0.035f, 0.0008f, 0.035f), Mats.Lit(new Color(0.3f, 0.32f, 0.36f)));
            float zb = -d * 0.5f + 0.03f, top = baseH + h + 0.16f;
            Prim.Shape(PrimitiveType.Cylinder, transform, "tap_base", new Vector3(0, baseH + h + 0.006f, zb), new Vector3(0.045f, 0.006f, 0.045f), steel);
            Prim.Shape(PrimitiveType.Cylinder, transform, "tap_pillar", new Vector3(0, (baseH + h + top) * 0.5f, zb), new Vector3(0.022f, (top - baseH - h) * 0.5f, 0.022f), steel);
            float reach = Mathf.Min(0.12f, d * 0.5f);
            var neck = Prim.Shape(PrimitiveType.Cylinder, transform, "tap_neck", new Vector3(0, top, zb + reach * 0.5f), new Vector3(0.02f, reach * 0.5f, 0.02f), steel);
            neck.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.Shape(PrimitiveType.Sphere, transform, "tap_elbow", new Vector3(0, top, zb), Vector3.one * 0.024f, steel);
            Prim.Shape(PrimitiveType.Cylinder, transform, "tap_nozzle", new Vector3(0, top - 0.015f, zb + reach), new Vector3(0.024f, 0.015f, 0.024f), steel);
            _spoutLocal = new Vector3(0, top - 0.032f, zb + reach);
            var piv = new GameObject("lever_pivot").transform; piv.SetParent(transform, false); piv.localPosition = new Vector3(0.045f, baseH + h + 0.03f, zb);
            Prim.Shape(PrimitiveType.Sphere, piv, "lever_hub", Vector3.zero, Vector3.one * 0.026f, steel);
            Prim.Box(piv, "lever_arm", new Vector3(0.035f, 0, 0), new Vector3(0.07f, 0.012f, 0.016f), Mats.Lit(Color.Lerp(Palette.Cream, owner, 0.5f)), false);
            Prim.Shape(PrimitiveType.Sphere, piv, "lever_tip", new Vector3(0.07f, 0, 0), Vector3.one * 0.022f, Mats.Lit(owner));
            _lever = piv; _maxAngle = 55f; _tipLocal = new Vector3(0.06f, 0, 0);
            BuildWater(new Vector3(w - 0.05f, 0.002f, d - 0.05f));
        }

        void BuildWater(Vector3 poolSize)
        {
            var sgo = new GameObject("toon_water_stream"); sgo.transform.SetParent(transform, false); sgo.transform.localPosition = _spoutLocal;
            _stream = sgo.AddComponent<ToonWaterStream>();
            _stream.MaxFall = Mathf.Max(0.2f, _spoutLocal.y - _baseH + 0.05f);
            var basin = transform;
            _stream.Ignore = c => c.transform.IsChildOf(basin) && c.GetComponentInParent<PropBody>() == null && c.name.StartsWith("lever");
            // basin water: a rippling toon surface that grows/shrinks with the conserved volume
            _poolSize = poolSize;
            var pg = new GameObject("basin_water"); pg.transform.SetParent(transform, false);
            pg.AddComponent<MeshFilter>().sharedMesh = DiscMesh();
            var pr = pg.AddComponent<MeshRenderer>(); _poolMat = ToonWaterStream.WaterMaterial(new Color(0.40f, 0.72f, 0.95f, 0.72f), new Color(0.20f, 0.48f, 0.86f, 0.88f), 1f);
            pr.sharedMaterial = _poolMat; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pool = pg.transform; _pool.gameObject.SetActive(false);
            _loop = gameObject.AddComponent<AudioSource>(); _loop.clip = Resources.Load<AudioClip>("Audio/tap_run_1"); _loop.loop = true; _loop.playOnAwake = false;
            _loop.spatialBlend = 1f; _loop.minDistance = 0.4f; _loop.maxDistance = 9f; _loop.rolloffMode = AudioRolloffMode.Linear; _loop.volume = 0f;
        }

        static Mesh _disc;
        static Mesh DiscMesh()
        {
            if (_disc != null) return _disc;
            const int N = 32; var v = new Vector3[N + 1]; var uv = new Vector2[N + 1]; var n = new Vector3[N + 1]; var t = new int[N * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f); n[0] = Vector3.up;
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N; float wob = 1f + 0.05f * Mathf.Sin(a * 5f) + 0.03f * Mathf.Sin(a * 3f + 1f); // painted irregular edge
                v[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f * wob, 0, Mathf.Sin(a) * 0.5f * wob); uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f); n[i + 1] = Vector3.up;
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % N + 1; t[i * 3 + 2] = i + 1;
            }
            _disc = new Mesh { name = "basin_water_disc" }; _disc.vertices = v; _disc.uv = uv; _disc.normals = n; _disc.triangles = t; _disc.RecalculateBounds();
            return _disc;
        }

        /// <summary>Authority: is this prop actually under the falling water right now (first thing the stream hits)?</summary>
        public bool Wets(PropBody b) => Running && _stream != null && _stream.Hitting && b != null && _stream.HitProp == b;

        /// <summary>Back-compat for callers that only have bounds: true when the stream lands on something inside them.</summary>
        public bool InStream(Bounds b) => Running && _stream != null && _stream.Hitting && b.Contains(_stream.HitPoint + Vector3.up * 0.003f);

        /// <summary>The runtime reports a plate under the stream this frame (louder water, presentation only).</summary>
        public void Hitting(Vector3 at) { _hitT = 0.25f; if (Time.time > _splashT) { _splashT = Time.time + 0.25f; RoomFx.Burst("bubbles", at, 0.07f, 0.6f); } }

        void Update()
        {
            var st = _fv != null ? _fv.State : null; if (st == null) return;
            bool open = st.Open;
            if (!_init) { _wasOpen = open; _init = true; _valve = Level; }
            if (open != _wasOpen) { AudioBank.I?.Play("tap_turn", LeverWorld, 0.5f); _wasOpen = open; }
            float flow = Flow;
            // lever pose: the live valve on the authority, the replicated level elsewhere
            float a = _maxAngle * (GameServices.IsAuthority ? _valve : Level); _leverAngle = Mathf.MoveTowards(_leverAngle, a, 240f * Time.deltaTime);
            _lever.localRotation = _leverAxisTilt ? Quaternion.Euler(-_leverAngle, 0, 0) : Quaternion.Euler(0, 0, _leverAngle);
            _stream.TargetFlow = flow;
            // basin water volume (presentation on every peer from the same flow): inflow - sqrt-law drain
            float qIn = FullFlowM3s * flow, k = FullFlowM3s / Mathf.Sqrt(PoolEqDepth);
            // V11: water landing inside a vessel's opening fills the vessel (finite, conserved); only the overflow reaches the basin
            if (flow > 0.01f && _stream.Hitting)
                foreach (var lc in LiquidContainer.All)
                    if (lc.InOpening(_stream.HitPoint)) { float over = lc.Catch(qIn * 1000f * Time.deltaTime, "tap"); qIn = over / 1000f / Mathf.Max(1e-4f, Time.deltaTime); break; }
            float depth = _volume / PoolArea; _volume = Mathf.Max(0f, _volume + (qIn - k * Mathf.Sqrt(Mathf.Max(0f, depth))) * Time.deltaTime);
            bool pool = _volume > 2e-6f;
            _stream.SurfaceY = pool ? transform.TransformPoint(new Vector3(0, _baseH + 0.004f + depth * 0.6f, 0)).y : float.NegativeInfinity;
            if (_pool.gameObject.activeSelf != pool) _pool.gameObject.SetActive(pool);
            if (pool)
            {
                float fill = Mathf.Clamp01(_volume / (PoolEqDepth * PoolArea));
                var c = _stream != null ? transform.InverseTransformPoint(_stream.HitPoint) : Vector3.zero;
                _pool.localPosition = new Vector3(Mathf.Clamp(c.x, -_poolSize.x * 0.3f, _poolSize.x * 0.3f) * 0.4f, _baseH + 0.004f + depth * 0.6f, Mathf.Clamp(c.z, -_poolSize.z * 0.3f, _poolSize.z * 0.3f) * 0.4f);
                _pool.localScale = new Vector3(_poolSize.x * Mathf.Lerp(0.35f, 0.98f, Mathf.Sqrt(fill)), 1f, _poolSize.z * Mathf.Lerp(0.45f, 0.98f, Mathf.Sqrt(fill)));
                _poolMat.SetFloat("_RippleSpeed", 0.4f + flow * 1.4f);
                // ripples/froth only where the water actually lands on the pool
                var hl = _pool.InverseTransformPoint(_stream != null && _stream.Hitting ? _stream.HitPoint : _pool.position);
                _poolMat.SetVector("_RippleCenter", flow > 0.02f ? new Vector4(Mathf.Clamp01(hl.x + 0.5f), Mathf.Clamp01(hl.z + 0.5f), 0, 0) : new Vector4(-5f, -5f, 0, 0));
            }
            _hitT -= Time.deltaTime;
            float vol = flow > 0.01f ? Mathf.Lerp(0.12f, 0.36f, flow) + (_hitT > 0f ? 0.18f : 0f) : 0f;
            if (vol > 0f && _loop.clip != null && !_loop.isPlaying) _loop.Play();
            _loop.volume = Mathf.MoveTowards(_loop.volume, vol * AudioBank.Master * AudioBank.BusVolume[(int)AudioBus.Sfx], 2.5f * Time.deltaTime);
            _loop.pitch = Mathf.Lerp(0.88f, 1.04f, flow) + (_hitT > 0f ? 0.06f : 0f);
            if (vol <= 0f && _loop.isPlaying && _loop.volume <= 0.001f) _loop.Stop();
        }

        void FixedUpdate()
        {
            if (!GameServices.IsAuthority || _rt == null || _rt.Round == null || !_rt.Round.IsPlayable) return;
            var st = _fv.State; if (st == null) return;
            // ---- the lever as a 1-DOF mechanism: a touching hand drags the tip along its arc between the stops
            Vector3 t0 = LeverTipAt(0f), t1 = LeverTipAt(1f), cur = LeverTipAt(_valve);
            float engageR = _engagedActor != ActorIds.None ? 0.075f : 0.045f;
            if (_rt.HandNear(cur, engageR, out int actor, out Vector3 hand))
            {
                _engagedActor = actor;
                var chord = t1 - t0; float target = Mathf.Clamp01(Vector3.Dot(hand - t0, chord) / Mathf.Max(1e-6f, chord.sqrMagnitude));
                _valve = Mathf.MoveTowards(_valve, target, 2.5f * Time.fixedDeltaTime);   // lever travel speed limit (no flicking through the stops)
            }
            else _engagedActor = ActorIds.None;
            // ops from elsewhere (a bot, a rules repair, a client intent) move the lever when nobody is holding it
            int lvl = st.Open ? (st.Level > 0 ? st.Level : 100) : 0;
            if (_engagedActor == ActorIds.None && lvl != _lastSent && _lastSent >= 0) { _valve = lvl / 100f; _lastSent = lvl; }
            if (_lastSent < 0) _lastSent = lvl;
            // ---- mirror the valve into the replicated rules state in 10 % bands
            _syncT -= Time.fixedDeltaTime;
            int band = _valve < 0.05f ? 0 : Mathf.Clamp(Mathf.RoundToInt(_valve * 10f) * 10, 10, 100);
            if (_engagedActor != ActorIds.None && band != lvl && _syncT <= 0f)
            {
                _syncT = 0.15f;
                var err = RecipeRuntime.Submit(new ROp { Actor = _engagedActor, Verb = band > 0 ? Verb.Open : Verb.Close, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1, Arg = band > 0 ? band.ToString() : null });
                if (err == null) _lastSent = band;
            }
            // ---- running water is audible: an operate-noise every 2.5 s scaled by flow (louder splashing off a plate)
            if (st.Open)
            {
                _noiseT -= Time.fixedDeltaTime;
                if (_noiseT <= 0f)
                {
                    _noiseT = 2.5f;
                    int who = ActorIds.None; var led = _rt.World.Ledger;
                    for (int i = led.Count - 1; i >= 0; i--) if (led[i].Fixture == st.Id && led[i].Verb == Verb.Open) { who = led[i].Actor; break; }
                    var p = SpoutWorld;
                    _rt.Round.SubmitOperate(who, 900 + st.Id, (0.5f + 0.8f * Flow) * (_hitT > 0f ? 1.5f : 1f), new Vec3(p.x, p.y, p.z), false);
                }
            }
            else _noiseT = 1.0f;
        }
    }
}
