using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Genies.Sdk;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Genies
{
    /// <summary>
    /// V8 primary character system: the installed Genies Avatar SDK 3.8.6 (public `Genies.Sdk.AvatarSdk` API only).
    /// - Init + session: AvatarSdk.InitializeAsync, TryInstantLoginAsync (stored Genies session), anonymous login as a fallback.
    /// - Starter presets for the four seats (Pip, Nell, Rory, Tess) are real Genies avatar definitions built once from the SDK's
    ///   default inventory (body type, hair, colours, wearables picked by seat palette) and cached on device.
    /// - Every body in the game (Family Stage, bots, remote humans, results podium, wardrobe mirror) is loaded with
    ///   LoadAvatarOptions.ByDefinition and driven by SHHH's humanoid controller built from Genies' own clips.
    /// Registered into SHHH.Game.AvatarService before the first scene loads.
    /// </summary>
    public sealed class GeniesBackend : IAvatarBackend
    {
        public static GeniesBackend I { get; private set; }
        public string Name => "Genies Avatar SDK 3.8.6";
        public bool Ready => _ready;
        public string Status => _status;
        public bool WardrobeOpen => GeniesWardrobe.Current != null;
        public bool LoggedIn => AvatarSdk.IsLoggedIn;

        bool _ready, _warming, _presetsBuilding; string _status = "not started";
        // V12 (owner): FIVE saved looks used always - the four players (Pip, Nell, Rory, Tess) + Mom - and every look is CLOTHED
        public const int MomPreset = 4;
        readonly string[] _presets = new string[5];
        readonly HashSet<string> _topIds = new HashSet<string>(), _bottomIds = new HashSet<string>();
        RuntimeAnimatorController _ctrl;
        Transform _hidden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            if (Application.isBatchMode) return;
            I = new GeniesBackend(); AvatarService.Backend = I;
        }

        public RuntimeAnimatorController Controller { get { if (_ctrl == null) _ctrl = Resources.Load<RuntimeAnimatorController>("Genies/SHHH_GeniesAnimator"); return _ctrl; } }
        static string PresetFile => Path.Combine(Application.persistentDataPath, "shhh_genies_presets_v3.json");
        static string PresetFileV2 => Path.Combine(Application.persistentDataPath, "shhh_genies_presets_v2.json");

        [Serializable] sealed class PresetCache { public string[] defs = new string[5]; public string[] tops = new string[0]; public string[] bottoms = new string[0]; }

        // ------------------------------------------------------------------ V12 clothing guard
        static readonly Regex Equipped = new Regex("\"equippedAssetIds\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
        static List<string> EquippedIds(string def)
        {
            var l = new List<string>(); if (string.IsNullOrEmpty(def)) return l; var m = Equipped.Match(def); if (!m.Success) return l;
            foreach (var part in m.Groups[1].Value.Split(',')) { var id = part.Trim().Trim('"'); if (id.Length > 0) l.Add(id); }
            return l;
        }
        /// <summary>A look is wearable in this game only if it is dressed: a top AND a bottom from the Genies wardrobe (ids learned from the
        /// SDK's own clothing lists); before those are known, at least three wardrobe items (hair + top + bottom). The owner rule: avatars
        /// are ALWAYS clothed - an empty or stripped definition is never shown, the seat's clothed look is used instead.</summary>
        public bool IsClothed(string def)
        {
            var ids = EquippedIds(def); if (ids.Count == 0) return false;
            if (_topIds.Count > 0 && _bottomIds.Count > 0)
            {
                bool top = false, bottom = false;
                foreach (var id in ids) { var k = id.Substring(id.LastIndexOf('/') + 1); if (_topIds.Contains(id) || _topIds.Contains(k)) top = true; if (_bottomIds.Contains(id) || _bottomIds.Contains(k)) bottom = true; }
                if (top && bottom) return true;
            }
            int gear = 0; foreach (var id in ids) if (id.StartsWith("WardrobeGear/")) gear++;
            return gear >= 3 && (_topIds.Count == 0 || _bottomIds.Count == 0);
        }

        public void Warmup() { if (!_warming && !_ready) WarmupAsync().Forget(); }

        async UniTaskVoid WarmupAsync()
        {
            _warming = true;
            try
            {
                _status = "starting Genies...";
                bool ok = await AvatarSdk.InitializeAsync();
                if (!ok) { _status = "Genies SDK failed to initialise (see Editor/Player log)"; Debug.LogWarning("[SHHH][genies] " + _status); return; }
                if (!AvatarSdk.IsLoggedIn) { var r = await AvatarSdk.TryInstantLoginAsync(); }
                if (!AvatarSdk.IsLoggedIn) { var a = await AvatarSdk.StartLoginAnonymousAsync(); if (!a.succeeded) Debug.LogWarning("[SHHH][genies] anonymous login failed: " + a.failReason); }
                _status = AvatarSdk.IsLoggedIn ? "Genies ready" : "Genies ready (not logged in: default looks only, wardrobe inventory needs a Genies session)";
                LoadPresetCache();
                _ready = true;
                Debug.Log($"[SHHH][genies] ready. loggedIn={AvatarSdk.IsLoggedIn} anonymous={AvatarSdk.IsLoggedInAnonymously} presetsCached={CountPresets()}");
                if (CountPresets() < 5) await BuildPresetsAsync();
            }
            catch (Exception e) { _status = "Genies error: " + e.Message; Debug.LogException(e); }
            finally { _warming = false; }
        }

        int CountPresets() { int n = 0; foreach (var p in _presets) if (!string.IsNullOrEmpty(p)) n++; return n; }

        void LoadPresetCache()
        {
            try
            {
                var f = File.Exists(PresetFile) ? PresetFile : File.Exists(PresetFileV2) ? PresetFileV2 : null;   // v2 = the four sibling looks (Mom is built once)
                // V12 (owner, Quest 2026-11-12: Nell / Rory / Tess stayed old stand-ins on the front-hall stage): the looks used to exist only
                // after this device built them from the SDK inventory (login + up to 90 s). The verified clothed looks now ship with the game
                // (asset ids + colours + body shape only) - every install has all five from the first frame.
                string json = f != null ? File.ReadAllText(f) : null;
                if (json == null) { var ta = Resources.Load<TextAsset>("Genies/shhh_genies_presets_v3"); if (ta != null) { json = ta.text; Debug.Log("[SHHH][genies] starter looks: shipped with the game"); } }
                if (json != null)
                {
                    var c = JsonUtility.FromJson<PresetCache>(json);
                    for (int i = 0; i < 5 && c?.defs != null && i < c.defs.Length; i++) _presets[i] = string.IsNullOrEmpty(c.defs[i]) ? null : c.defs[i];
                    if (c?.tops != null) foreach (var t in c.tops) _topIds.Add(t); if (c?.bottoms != null) foreach (var b in c.bottoms) _bottomIds.Add(b);
                }
                for (int i = 0; i < 5; i++) if (_presets[i] != null && !IsClothed(_presets[i])) { Debug.LogWarning($"[SHHH][genies] cached look {i} is not clothed - rebuilding it"); _presets[i] = null; }
            }
            catch (Exception e) { Debug.LogWarning("[SHHH][genies] preset cache unreadable: " + e.Message); }
        }

        public string PresetDefinition(int seat) => seat == MomPreset ? _presets[MomPreset] : _presets[((seat % 4) + 4) % 4];

        // ------------------------------------------------------------------ starter presets (real Genies definitions)
        // Seat palettes from the approved sibling identities: Pip coral, Nell teal, Rory sunny yellow, Tess purple.
        static readonly (string name, GenderType g, string[] top, string[] bottom, string[] shoes, int hair, int hairCol, int skin)[] Spec =
        {
            ("Pip",  GenderType.Androgynous, new[]{"pink","coral","red","rose"},     new[]{"short","jean","denim"},  new[]{"sneaker","shoe"}, 4, 2, 3),
            ("Nell", GenderType.Female,      new[]{"teal","green","mint","blue"},    new[]{"skirt","pant","legging"},new[]{"sneaker","boot"},   9, 5, 6),
            ("Rory", GenderType.Male,        new[]{"yellow","orange","mustard"},     new[]{"pant","jogger","short"}, new[]{"sneaker","shoe"},   2, 1, 8),
            ("Tess", GenderType.Female,      new[]{"purple","lilac","violet","lavender"}, new[]{"pant","jean","skirt"}, new[]{"sneaker","boot"}, 14, 7, 1),
            ("Mom",  GenderType.Female,      new[]{"robe","pajama","sweater","cardigan","hoodie"}, new[]{"pajama","pant","legging","jogger"}, new[]{"slipper","slide","sneaker"}, 6, 3, 4),
        };

        async UniTask BuildPresetsAsync()
        {
            if (_presetsBuilding) return; _presetsBuilding = true;
            try
            {
                if (!AvatarSdk.IsLoggedIn) { Debug.Log("[SHHH][genies] presets: not logged in - seats use the SDK default avatar"); return; }
                _status = "building the family looks (four siblings + Mom)...";
                var hair = await AvatarSdk.GetDefaultHairAssets(HairType.Hair) ?? new List<WearableAssetInfo>();
                var skins = await AvatarSdk.GetDefaultColorsAsync(ColorType.Skin) ?? new List<IAvatarColor>();
                var hairCols = await AvatarSdk.GetDefaultColorsAsync(ColorType.Hair) ?? new List<IAvatarColor>();
                var tops = new List<WearableAssetInfo>(); var bottoms = new List<WearableAssetInfo>(); var shoes = await AvatarSdk.GetDefaultWearablesByCategoryAsync(WearablesCategory.Shoes) ?? new List<WearableAssetInfo>();
                foreach (var c in new[] { WearablesCategory.Hoodie, WearablesCategory.Shirt }) { var l = await AvatarSdk.GetDefaultWearablesByCategoryAsync(c); if (l != null) tops.AddRange(l); }
                foreach (var c in new[] { WearablesCategory.Pants, WearablesCategory.Shorts, WearablesCategory.Skirt }) { var l = await AvatarSdk.GetDefaultWearablesByCategoryAsync(c); if (l != null) bottoms.AddRange(l); }
                foreach (var t in tops) if (t?.AssetId != null) _topIds.Add(t.AssetId); foreach (var b in bottoms) if (b?.AssetId != null) _bottomIds.Add(b.AssetId);
                var holder = Hidden();
                Debug.Log($"[SHHH][genies] presets: inventory hair={hair.Count} skins={skins.Count} hairColours={hairCols.Count} tops={tops.Count} bottoms={bottoms.Count} shoes={shoes.Count}");
                // One workshop avatar, loaded exactly like the wardrobe's (Default + SHHH controller), reset to its base definition per seat.
                float t0 = Time.realtimeSinceStartup;
                ManagedAvatar av = null;
                await Gate();
                try
                {
                    var (lt, a0) = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Default { AvatarName = "preset_workshop", Parent = holder, PlayerAnimationController = Controller }).TimeoutWithoutException(TimeSpan.FromSeconds(60));
                    av = lt ? null : a0;
                    if (av != null) await WaitLoaded(av);
                    Debug.Log($"[SHHH][genies] presets: workshop avatar {(lt ? "timed out" : av == null ? "returned null" : "loaded")} in {Time.realtimeSinceStartup - t0:0.0}s");
                }
                finally { _gate = false; }
                if (av == null) return;
                // j3: editing straight after the load returned (0.2 s) took the whole Editor down inside native Genies code with no managed
                // exception. In the diag (which passed) several seconds of inventory calls separated load and edits - settle first.
                await UniTask.Delay(2500);
                string baseDef = av.GetDefinition();
                var usedTop = new HashSet<string>(); var usedBottom = new HashSet<string>();
                for (int s = 0; s < 5; s++)
                {
                    if (!string.IsNullOrEmpty(_presets[s])) continue;
                    var sp = Spec[s]; t0 = Time.realtimeSinceStartup;
                    try
                    {
                        if (s > 0 && !string.IsNullOrEmpty(baseDef)) await Step(sp.name, "reset", () => av.SetDefinitionAsync(baseDef));
                        await Step(sp.name, "body", () => AvatarSdk.SetAvatarBodyTypeAsync(av, sp.g, BodySize.Medium)); // Medium = the combination the diag verified
                        if (hair.Count > 0) await Step(sp.name, "hair", () => AvatarSdk.EquipHairAsync(av, hair[sp.hair % hair.Count]));
                        if (hairCols.Count > 0) await Step(sp.name, "hair colour", () => AvatarSdk.SetColorAsync(av, hairCols[sp.hairCol % hairCols.Count]).AsUniTask());
                        if (skins.Count > 0) await Step(sp.name, "skin", () => AvatarSdk.SetColorAsync(av, skins[sp.skin % skins.Count]).AsUniTask());
                        var t = (s == MomPreset ? Pick(tops, sp.top, s) : PickByColour(tops, SeatColour[s], usedTop)) ?? Pick(tops, sp.top, s); if (t != null) usedTop.Add(t.AssetId);
                        if (t != null) await Step(sp.name, "top", () => AvatarSdk.EquipWearableAsync(av, t));
                        var b = PickDistinct(bottoms, sp.bottom, s * 3 + 1, usedBottom); if (b != null) usedBottom.Add(b.AssetId);
                        if (b != null) await Step(sp.name, "bottom", () => AvatarSdk.EquipWearableAsync(av, b));
                        var f = Pick(shoes, sp.shoes, s); if (f != null) await Step(sp.name, "shoes", () => AvatarSdk.EquipWearableAsync(av, f));
                        var d = av.GetDefinition();
                        if (!string.IsNullOrEmpty(d) && d != baseDef && IsClothed(d)) _presets[s] = d;
                        else if (!string.IsNullOrEmpty(d) && !IsClothed(d)) Debug.LogWarning($"[SHHH][genies] preset {sp.name}: NOT clothed (top {t != null}, bottom {b != null}) - not kept");
                        Debug.Log($"[SHHH][genies] preset {sp.name}: top '{t?.Name}', bottom '{b?.Name}', shoes '{f?.Name}', {d?.Length} chars{(d == baseDef ? " (UNCHANGED from base - not kept)" : "")}, {Time.realtimeSinceStartup - t0:0.0}s");
                    }
                    catch (Exception e) { Debug.LogWarning($"[SHHH][genies] preset {sp.name} failed: {e.Message}"); }
                }
                av.Dispose();
                if (CountPresets() > 0) File.WriteAllText(PresetFile, JsonUtility.ToJson(new PresetCache { defs = (string[])_presets.Clone(), tops = new List<string>(_topIds).ToArray(), bottoms = new List<string>(_bottomIds).ToArray() }));
                _status = "Genies ready";
                PresetsChanged?.Invoke();
                if (CountPresets() > 0) AvatarService.NotifyLocalLookChanged(); // stage / bots re-dress in the sibling looks
            }
            finally { _presetsBuilding = false; }
        }
        public event Action PresetsChanged;

        // The SDK returned null for some of several simultaneous loads (seat 1 / seat 3 in j1): loads go through one gate.
        bool _gate; int _loadSerial;

        async UniTask Gate() { while (_gate) await UniTask.Delay(50); _gate = true; }

        static async UniTask Step(string who, string what, Func<UniTask> make)
        {
            await UniTask.Delay(250);
            Debug.Log($"[SHHH][genies] preset {who}: {what}..."); // logged before the call, so a native fault names its step
            if (await make().TimeoutWithoutException(TimeSpan.FromSeconds(20))) Debug.LogWarning($"[SHHH][genies] preset {who}: '{what}' timed out (continuing)");
        }

        // Seat identity colours (approved sibling palettes): Pip coral, Nell teal, Rory sunny yellow, Tess purple.
        static readonly Color[] SeatColour = { new Color(1f, 0.42f, 0.38f), new Color(0.15f, 0.72f, 0.68f), new Color(1f, 0.8f, 0.18f), new Color(0.62f, 0.38f, 0.92f) };

        /// <summary>Wearable whose SDK icon is closest in hue to the seat colour (icons are the SDK's own thumbnails; read back through a
        /// blit because they are not CPU-readable). Distinct per seat.</summary>
        static WearableAssetInfo PickByColour(List<WearableAssetInfo> l, Color target, HashSet<string> used)
        {
            if (l == null) return null; WearableAssetInfo best = null; float bestD = 0.22f; // must be reasonably close, else keyword fallback
            Color.RGBToHSV(target, out var th, out var ts, out _);
            foreach (var a in l)
            {
                if (a == null || used.Contains(a.AssetId) || !IconColour(a.Icon, out var c)) continue;
                Color.RGBToHSV(c, out var h, out var sat, out _);
                if (sat < 0.18f) continue;
                float dh = Mathf.Abs(h - th); dh = Mathf.Min(dh, 1f - dh);
                float d = dh + Mathf.Abs(sat - ts) * 0.15f;
                if (d < bestD) { bestD = d; best = a; }
            }
            if (best != null) Debug.Log($"[SHHH][genies] colour pick '{best.Name}' (distance {bestD:0.00})");
            return best;
        }

        static bool IconColour(Sprite sp, out Color avg)
        {
            avg = Color.clear; if (sp == null || sp.texture == null) return false;
            var rt = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.ARGB32); var prev = RenderTexture.active;
            try
            {
                var r = sp.textureRect; var tx = sp.texture;
                Graphics.Blit(tx, rt, new Vector2(r.width / tx.width, r.height / tx.height), new Vector2(r.x / tx.width, r.y / tx.height));
                RenderTexture.active = rt; var t = new Texture2D(32, 32, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); t.Apply();
                var px = t.GetPixels(); UnityEngine.Object.Destroy(t);
                float n = 0; Color sum = Color.clear;
                foreach (var p in px)
                {
                    Color.RGBToHSV(p, out _, out var s, out var v);
                    if (p.a < 0.5f || v < 0.22f) continue; // skip the dark backdrop
                    float w = 0.2f + s; sum += p * w; n += w;
                }
                if (n < 1f) return false; avg = sum / n; return true;
            }
            finally { RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); }
        }

        static WearableAssetInfo PickDistinct(List<WearableAssetInfo> l, string[] words, int fallback, HashSet<string> used)
        {
            if (l == null || l.Count == 0) return null;
            foreach (var w in words) foreach (var a in l) if (a?.Name != null && !used.Contains(a.AssetId) && a.Name.ToLowerInvariant().Contains(w)) return a;
            for (int k = 0; k < l.Count; k++) { var a = l[((fallback + k) % l.Count + l.Count) % l.Count]; if (a != null && !used.Contains(a.AssetId)) return a; }
            return l[0];
        }

        static WearableAssetInfo Pick(List<WearableAssetInfo> l, string[] words, int fallback)
        {
            if (l == null || l.Count == 0) return null;
            foreach (var w in words) foreach (var a in l) if (a?.Name != null && a.Name.ToLowerInvariant().Contains(w)) return a;
            return l[((fallback % l.Count) + l.Count) % l.Count];
        }

        public static async UniTask WaitLoaded(ManagedAvatar av, float timeout = 30f)
        {
            float w = 0; while (av != null && !av.IsDisposed && !av.IsLoadingComplete && w < timeout) { await UniTask.Delay(100); w += 0.1f; }
        }

        Transform Hidden()
        {
            if (_hidden == null) { var g = new GameObject("GeniesPresetWorkshop"); UnityEngine.Object.DontDestroyOnLoad(g); g.transform.position = new Vector3(0, -30, -200); _hidden = g.transform; }
            return _hidden;
        }

        // ------------------------------------------------------------------ loading bodies
        public void Load(string definition, int presetSeat, Transform parent, Action<AvatarHandle> done, Func<bool> wanted = null) => LoadAsync(definition, presetSeat, parent, done, wanted).Forget();

        /// <summary>
        /// Crash fix 2026-11-12 (Quest 2 run 1:16.7 + Editor crash 07:59, native crash in libgnUnityPlugin AssetBuilder.Build on a thread-pool
        /// thread): a Genies body was destroyed while the SDK was still building it in the background (its parent object was destroyed by a
        /// stage rebuild / room teardown). Bodies are now ALWAYS built under the backend's own hidden, never-destroyed workshop object and
        /// handed to the caller only once fully loaded; a request nobody wants any more is skipped before the SDK starts, or disposed only
        /// after its build finished. Nothing we destroy can pull a body out from under the SDK's builder.
        /// </summary>
        async UniTaskVoid LoadAsync(string definition, int presetSeat, Transform parent, Action<AvatarHandle> done, Func<bool> wanted)
        {
            AvatarHandle h = null; AvatarService.LoadsRequested++;
            bool Want() { try { return parent != null && (wanted == null || wanted()); } catch { return false; } }
            try
            {
                float w = 0; while (!_ready && w < 20f) { await UniTask.Delay(100); w += 0.1f; }
                if (!string.IsNullOrEmpty(definition) && !IsClothed(definition)) { Debug.LogWarning($"[SHHH][genies] seat {presetSeat}: look is not clothed - using the seat's clothed look"); definition = null; }
                if (string.IsNullOrEmpty(definition))
                {
                    w = 0; while (_presetsBuilding && string.IsNullOrEmpty(PresetDefinition(presetSeat)) && w < 90f) { await UniTask.Delay(200); w += 0.2f; }
                    definition = PresetDefinition(presetSeat);
                }
                // never the SDK's bare default body: no clothed definition = keep the seat's clothed stand-in body (owner: always clothed)
                if (string.IsNullOrEmpty(definition) || !IsClothed(definition)) { Debug.Log($"[SHHH][genies] seat {presetSeat}: no clothed Genies look yet - keeping the clothed stand-in"); done?.Invoke(null); return; }
                if (!Want()) { AvatarService.LoadsSkipped++; Debug.Log($"[SHHH][genies] load seat {presetSeat}: skipped (no longer wanted)"); done?.Invoke(null); return; }
                await Gate();
                ManagedAvatar av = null;
                try
                {
                    // still wanted now that it is our turn? (a superseded / destroyed request costs nothing)
                    if (!Want()) { AvatarService.LoadsSkipped++; done?.Invoke(null); return; }
                    // a private slot + unique name per load: a disposed body left in the workshop under the same name made the next load hang
                    int n = ++_loadSerial; var shop = new GameObject($"load_{n}_seat{presetSeat}").transform; shop.SetParent(Hidden(), false); float t0 = Time.realtimeSinceStartup;
                    Debug.Log($"[SHHH][genies] load seat {presetSeat}: building ({(string.IsNullOrEmpty(definition) ? "preset" : "saved look")})");
                    var task = string.IsNullOrEmpty(definition)
                        ? AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Default { AvatarName = $"Genie_{presetSeat}_{n}", Parent = shop, PlayerAnimationController = Controller, ShowLoadingSilhouette = true })
                        : AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.ByDefinition { DefinitionToLoad = definition, AvatarName = $"Genie_{presetSeat}_{n}", Parent = shop, PlayerAnimationController = Controller, ShowLoadingSilhouette = true });
                    // one hung SDK load must not block every other body: give up waiting after 45 s (its slot is never destroyed)
                    var (timedOut, got) = await task.TimeoutWithoutException(TimeSpan.FromSeconds(45));
                    if (timedOut) { Debug.LogWarning($"[SHHH][genies] load seat {presetSeat}: SDK did not answer in 45 s - skipped (slot kept)"); done?.Invoke(null); return; }
                    av = got;
                    if (av != null) { AvatarService.LoadsBuilt++; await WaitLoaded(av, 60f); }
                    Debug.Log($"[SHHH][genies] load seat {presetSeat}: {(av == null ? "null" : av.IsLoadingComplete ? "complete" : "NOT complete")} after {Time.realtimeSinceStartup - t0:0.0} s");
                }
                finally { _gate = false; }
                if (av == null) { Debug.LogWarning("[SHHH][genies] load returned null for seat " + presetSeat); done?.Invoke(null); return; }
                await WaitLoaded(av, 60f);
                // only a fully built body is ever disposed or handed over
                if (!Want()) { AvatarService.LoadsDiscarded++; av.Dispose(); done?.Invoke(null); return; }
                var slotT = av.Root.transform.parent;
                av.Root.transform.SetParent(parent, false);
                if (slotT != null && slotT != Hidden() && slotT.childCount == 0) UnityEngine.Object.Destroy(slotT.gameObject);   // built and moved out: the empty slot can go
                if (av.Animator != null && Controller != null && av.Animator.runtimeAnimatorController != Controller) av.SetAnimatorController(Controller);
                if (av.Animator != null) { av.Animator.applyRootMotion = false; av.Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; }
                foreach (var r in av.Root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                AvatarService.LiveBodies++;
                h = new AvatarHandle { Root = av.Root, Animator = av.Animator, Definition = av.GetDefinition(), Release = () => { if (!av.IsDisposed) { av.Dispose(); AvatarService.LiveBodies--; } } };
            }
            catch (Exception e) { Debug.LogWarning("[SHHH][genies] load failed: " + e.Message); }
            // V12 (Quest c15 judder: five Genies bodies at once): 2-bone skinning, no skinned motion vectors - my own body is set back to
            // full quality when it is attached to my tracking (TrackedGeniesBody)
            if (h != null && h.Root != null) foreach (var smr in h.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.quality = SkinQuality.Bone2; smr.skinnedMotionVectors = false; }
            done?.Invoke(h);
        }

        // ------------------------------------------------------------------ wardrobe
        public void OpenWardrobe(Transform anchor, Action onClosed) => GeniesWardrobe.Open(this, anchor, onClosed);

        /// <summary>Persist the player's look: SHHH settings (definition JSON, used everywhere incl. online) + the SDK's local profile.</summary>
        public async UniTask<bool> SavePlayerLookAsync(ManagedAvatar av)
        {
            var def = av.GetDefinition();
            if (string.IsNullOrEmpty(def)) return false;
            if (!IsClothed(def)) { Debug.Log("[SHHH][genies] save refused: the look is not clothed"); return false; }   // owner: always clothed
            Settings.Current.GeniesDefinition = def; Settings.Save();
            try { await AvatarSdk.SaveAvatarDefinitionLocallyAsync(av, "shhh_player"); } catch (Exception e) { Debug.LogWarning("[SHHH][genies] local profile save failed: " + e.Message); }
            Debug.Log($"[SHHH][genies] player look saved ({def.Length} chars, {av.GetEquippedAssetIds().Count} equipped)");
            AvatarService.NotifyLocalLookChanged();
            return true;
        }
    }
}
