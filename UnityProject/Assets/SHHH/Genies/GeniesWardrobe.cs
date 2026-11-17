using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Sdk;
using SHHH.Game;
using TMPro;
using UnityEngine;

namespace SHHH.Genies
{
    /// <summary>
    /// V8 in-world Genies wardrobe (Family Stage dressing room). The player's real Genies avatar stands on a turntable plinth on the
    /// Family Stage; a closet board (left) lists real SDK inventory by category with the SDK's own item icons, a side board (right)
    /// turns the turntable, applies the four sibling starter looks, undoes, saves and exits. Every change is a real SDK call
    /// (EquipHair/EquipWearable/UnEquipWearable/SetColor/SetAvatarBodyType/SetAvatarFeature) on the live ManagedAvatar, and Save stores
    /// the avatar definition (SHHH settings + SDK local profile "shhh_player"). Nothing here is a fake item: lists come from
    /// GetDefault*/GetUser* and an empty category shows "nothing available" (inventory needs a Genies session).
    /// </summary>
    public sealed class GeniesWardrobe : MonoBehaviour
    {
        public static GeniesWardrobe Current { get; private set; }
        GeniesBackend _be; Action _onClosed; ManagedAvatar _av; Transform _plinth, _closet, _side, _grid;
        TextMeshPro _status, _catTitle, _page; string _openedDef; readonly Stack<string> _undo = new Stack<string>();
        bool _busy; int _cat, _pageIx; float _spin, _spinVel;
        readonly List<PokeButton> _catKeys = new List<PokeButton>();
        readonly List<(PokeButton btn, Renderer icon, Material mat, TextMeshPro label, GameObject equipped)> _cells = new List<(PokeButton, Renderer, Material, TextMeshPro, GameObject)>();
        readonly Dictionary<int, List<Item>> _lists = new Dictionary<int, List<Item>>();
        Vector3 _restoreSpawn; float _restoreYaw;

        sealed class Item { public string Id, Name; public Sprite Icon; public Color Swatch; public Func<UniTask> Apply; public bool Wearable; }
        static readonly string[] Cats = { "Looks", "Hair", "Hair colour", "Skin", "Eyes", "Tops", "Bottoms", "Shoes", "Hats", "Glasses", "Extras", "Body", "Face" };
        static readonly string[] CatIcons = { "star", "home", "noise", "hand", "eye", "wardrobe", "wardrobe", "play", "crown", "eye", "star", "friends", "mom" };
        const int Cols = 3, Rows = 3;

        public static void Open(GeniesBackend be, Transform anchor, Action onClosed)
        {
            if (Current != null) return;
            var g = new GameObject("GeniesWardrobe"); var w = g.AddComponent<GeniesWardrobe>(); Current = w;
            w._be = be; w._onClosed = onClosed;
            w.Build();
        }

        void Build()
        {
            var game = GameRoot.I; var rig = game != null ? game.Rig : null;
            // stand on the Family Stage: the player turns to face it; the stage siblings step aside while you dress up
            var stage = FamilyStage.I != null ? FamilyStage.I.transform : null;
            Vector3 stagePos = stage != null ? stage.position : new Vector3(-1.85f, 0f, 0.75f);
            if (stage != null) stage.gameObject.SetActive(false);
            MenuBuilder.ClosePopup(); if (MenuBuilder.FrontEndBoard != null) MenuBuilder.FrontEndBoard.gameObject.SetActive(false);
            Vector3 eye = rig != null && rig.Head != null ? rig.Head.position : new Vector3(0, 1.55f, 1.7f);
            var toStage = stagePos - new Vector3(eye.x, 0, eye.z); toStage.y = 0;
            if (rig != null) { _restoreSpawn = new Vector3(eye.x, 0, eye.z); _restoreYaw = rig.Head.eulerAngles.y; rig.Spawn(new Vector3(eye.x, 0, eye.z), Mathf.Atan2(toStage.x, toStage.z) * Mathf.Rad2Deg); eye = rig.Head.position; }
            var fwd = toStage.normalized; var right = Vector3.Cross(Vector3.up, fwd);
            transform.position = Vector3.zero;

            // plinth + turntable on the stage spot, warm spot light
            _plinth = new GameObject("turntable").transform; _plinth.SetParent(transform, false);
            _plinth.position = new Vector3(stagePos.x, 0f, stagePos.z); _plinth.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            Prim.Shape(PrimitiveType.Cylinder, _plinth, "plinth", new Vector3(0, 0.2f, 0), new Vector3(0.9f, 0.2f, 0.9f), Mats.Lit(UiKit.Purple, 0.3f));
            Prim.Shape(PrimitiveType.Cylinder, _plinth, "ring", new Vector3(0, 0.402f, 0), new Vector3(0.94f, 0.004f, 0.94f), Mats.Unlit(UiKit.Hush));
            var lightGo = new GameObject("wardrobeSpot"); lightGo.transform.SetParent(transform, false);
            lightGo.transform.position = _plinth.position + Vector3.up * 2.6f - fwd * 1.0f; lightGo.transform.LookAt(_plinth.position + Vector3.up * 1f);
            var sl = lightGo.AddComponent<Light>(); sl.type = LightType.Spot; sl.spotAngle = 55f; sl.range = 6f; sl.intensity = 2.2f; sl.color = new Color(1f, 0.92f, 0.8f); sl.shadows = LightShadows.None;

            // boards: closet (left-front) and side controls (right-front), facing the player, below the mannequin's eye line
            float chest = Mathf.Clamp(eye.y - 0.38f, 0.7f, 1.35f);
            _closet = UiKit.Panel(transform, "closet", Vector3.zero, new Vector2(1.06f, 0.86f), "WARDROBE");
            Place(_closet, new Vector3(eye.x, chest, eye.z) + fwd * 0.6f - right * 0.36f, eye, 0.62f);
            _side = UiKit.Panel(transform, "side", Vector3.zero, new Vector2(0.42f, 0.86f), "YOU");
            Place(_side, new Vector3(eye.x, chest, eye.z) + fwd * 0.62f + right * 0.42f, eye, 0.62f);

            // category column
            for (int i = 0; i < Cats.Length; i++)
            {
                int k = i; var b = PokeButton.Create(_closet, Cats[i], new Vector3(-0.41f, 0.36f - i * 0.061f, 0), new Vector2(0.2f, 0.052f), UiKit.Purple, () => SelectCat(k), 0.22f, CatIcons[i]);
                _catKeys.Add(b);
            }
            _catTitle = UiKit.Text(_closet, "", new Vector3(0.12f, 0.37f, -0.015f), 0.42f, UiKit.Hush, UiKit.Display, 0.7f);
            _grid = new GameObject("grid").transform; _grid.SetParent(_closet, false);
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++)
            {
                int ix = r * Cols + c; var pos = new Vector3(-0.1f + c * 0.22f, 0.21f - r * 0.21f, 0);
                var cell = PokeButton.CreateBlank(_grid, "item" + ix, pos, new Vector2(0.19f, 0.19f), () => Press(ix));
                UiKit.SlabObject(cell.Cap, "frame", new Vector3(0, 0, 0.004f), 0.2f, 0.2f, 0.03f, 0.02f, 0.006f, Mats.Lit(UiKit.MoonCream, 0.3f));
                // V12 (owner, Quest c15: hair tiles were dark grey on dark purple): the Genies icons have transparent backgrounds - draw
                // them with alpha (cutout) over a light paper backing instead of opaque (the transparent part rendered dark)
                UiKit.Quad(cell.Cap, "backing", Mats.Unlit(Color.Lerp(UiKit.MoonCream, Color.white, 0.4f)), new Vector3(0, 0.012f, -0.007f), new Vector2(0.15f, 0.15f));
                var cut = Resources.Load<Material>("UI_V8/Mats/UiCutout");
                var mat = cut != null ? new Material(cut) : new Material(UiKit.Opaque(UiKit.Tex("map_paper")));
                var icon = UiKit.Quad(cell.Cap, "icon", mat, new Vector3(0, 0.012f, -0.0078f), new Vector2(0.15f, 0.15f));
                var lab = UiKit.Text(cell.Cap, "", new Vector3(0, -0.078f, -0.012f), 0.13f, UiKit.Deep, UiKit.Body, 0.18f, TextAlignmentOptions.Center, 0f);
                lab.textWrappingMode = TextWrappingModes.NoWrap; lab.overflowMode = TextOverflowModes.Ellipsis;
                var eq = UiKit.Icon(cell.Cap, "check", new Vector3(0.07f, 0.07f, -0.012f), 0.05f); eq.SetActive(false);   // V12: no "equipped" ticks on cells that have nothing in them yet
                var glow = UiKit.SlabObject(cell.transform, "selected", new Vector3(0, 0, -0.001f), 0.215f, 0.215f, 0.035f, 0.008f, 0.003f, Mats.Unlit(UiKit.Hush)); glow.SetActive(false); cell.SelectedMark = glow;
                _cells.Add((cell, icon.GetComponent<Renderer>(), mat, lab, eq));
            }
            PokeButton.Create(_closet, "", new Vector3(0.0f, -0.39f, 0), new Vector2(0.09f, 0.06f), UiKit.Teal, () => Page(-1), 0.2f, "back");
            PokeButton.Create(_closet, "", new Vector3(0.32f, -0.39f, 0), new Vector2(0.09f, 0.06f), UiKit.Teal, () => Page(1), 0.2f, "play");
            _page = UiKit.Text(_closet, "", new Vector3(0.16f, -0.39f, -0.015f), 0.2f, UiKit.MoonCream, UiKit.Body, 0.2f);
            _status = UiKit.Text(_closet, "Loading your Genie...", new Vector3(0.12f, -0.315f, -0.015f), 0.18f, UiKit.SoftWhite, UiKit.Body, 0.66f, TextAlignmentOptions.Center, 0.1f);

            // side board: spin, starter looks, undo, save, back
            PokeButton.Create(_side, "", new Vector3(-0.09f, 0.33f, 0), new Vector2(0.14f, 0.07f), UiKit.Teal, () => _spinVel = -140f, 0.2f, "retry");
            PokeButton.Create(_side, "", new Vector3(0.09f, 0.33f, 0), new Vector2(0.14f, 0.07f), UiKit.Teal, () => _spinVel = 140f, 0.2f, "retry");
            UiKit.Text(_side, "Starter looks", new Vector3(0, 0.25f, -0.015f), 0.2f, UiKit.MoonCream, UiKit.Body, 0.38f);
            string[] sibs = { "Pip", "Nell", "Rory", "Tess" };
            for (int i = 0; i < 4; i++) { int k = i; PokeButton.Create(_side, sibs[i], new Vector3(i % 2 == 0 ? -0.09f : 0.09f, 0.18f - (i / 2) * 0.075f, 0), new Vector2(0.16f, 0.06f), Palette.Siblings[i], () => ApplyPreset(k), 0.24f); }
            PokeButton.Create(_side, "Undo", new Vector3(0, -0.02f, 0), new Vector2(0.32f, 0.065f), UiKit.MoonCream, Undo, 0.26f, "back");
            PokeButton.Create(_side, "Reset", new Vector3(0, -0.1f, 0), new Vector2(0.32f, 0.065f), UiKit.MoonCream, ResetToSaved, 0.26f, "retry");
            PokeButton.Create(_side, "SAVE", new Vector3(0, -0.2f, 0), new Vector2(0.32f, 0.08f), UiKit.Hush, () => SaveAsync().Forget(), 0.36f, "check");
            PokeButton.Create(_side, "Done", new Vector3(0, -0.32f, 0), new Vector2(0.32f, 0.07f), UiKit.Coral, Close, 0.3f, "home");

            LoadAvatarAsync().Forget();
        }

        static void Place(Transform t, Vector3 pos, Vector3 eye, float scale)
        {
            t.position = pos; var look = pos - new Vector3(eye.x, pos.y, eye.z);
            t.rotation = Quaternion.LookRotation(look.normalized, Vector3.up) * Quaternion.Euler(14f, 0, 0); t.localScale = Vector3.one * scale;
        }

        async UniTaskVoid LoadAvatarAsync()
        {
            _busy = true;
            string def = !string.IsNullOrEmpty(Settings.Current.GeniesDefinition) ? Settings.Current.GeniesDefinition : _be.PresetDefinition(((Settings.Current.Cosmetic % 4) + 4) % 4);
            try
            {
                _av = string.IsNullOrEmpty(def)
                    ? await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Default { AvatarName = "WardrobeGenie", Parent = _plinth, PlayerAnimationController = _be.Controller })
                    : await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.ByDefinition { DefinitionToLoad = def, AvatarName = "WardrobeGenie", Parent = _plinth, PlayerAnimationController = _be.Controller });
                if (_av == null) { Status("Genies could not load an avatar: " + _be.Status); _busy = false; return; }
                await GeniesBackend.WaitLoaded(_av);
                if (this == null) { _av.Dispose(); return; }
                _av.Root.transform.localPosition = new Vector3(0, 0.4f, 0); _av.Root.transform.localRotation = Quaternion.identity; _av.Root.transform.localScale = Vector3.one * SiblingAvatar.GeniesScale;
                _openedDef = _av.GetDefinition();
                Status(AvatarSdk.IsLoggedIn ? "Pick a category, then poke an item to try it on" : "Not signed in to Genies: starter looks + body only");
            }
            catch (Exception e) { Status("Load failed: " + e.Message); }
            _busy = false;
            SelectCat(1);
        }

        void Status(string s) { if (_status != null) _status.text = s; }

        void SelectCat(int k)
        {
            _cat = k; _pageIx = 0;
            for (int i = 0; i < _catKeys.Count; i++) _catKeys[i].SetColor(i == k ? UiKit.Hush : UiKit.Purple);
            _catTitle.text = Cats[k].ToUpperInvariant();
            if (_lists.ContainsKey(k)) Refresh();
            else { foreach (var c in _cells) c.btn.gameObject.SetActive(false); FetchAsync(k).Forget(); }   // V12: no placeholder tiles while the drawer opens
        }

        // V12: icons arrive after the list - refresh twice a second while any shown item still waits for its picture; and once the first
        // drawer is open, the others are fetched in the background (hair took ~8 s to open on the Quest)
        bool _iconsPending, _prefetching; float _iconPoll;
        async UniTaskVoid PrefetchRest()
        {
            if (_prefetching) return; _prefetching = true;
            foreach (int k in new[] { 5, 6, 0, 2, 3, 7, 8, 4, 9, 10 }) { if (this == null) return; if (!_lists.ContainsKey(k)) { await FetchQuiet(k); } }
        }
        async UniTask FetchQuiet(int k) { FetchAsync(k).Forget(); float w = 0; while (this != null && !_lists.ContainsKey(k) && w < 20f) { await UniTask.Delay(100); w += 0.1f; } }

        async UniTaskVoid FetchAsync(int k)
        {
            if (_cat == k) Status("Opening the " + Cats[k].ToLowerInvariant() + " drawer...");
            var l = new List<Item>();
            try
            {
                switch (k)
                {
                    case 0: for (int i = 0; i < 4; i++) { int s = i; l.Add(new Item { Id = "preset" + i, Name = new[] { "Pip", "Nell", "Rory", "Tess" }[i], Swatch = Palette.Siblings[i], Apply = () => SetDef(_be.PresetDefinition(s)) }); } break;
                    case 1: foreach (var h in await AvatarSdk.GetDefaultHairAssets(HairType.Hair) ?? new List<WearableAssetInfo>()) { var a = h; l.Add(new Item { Id = a.AssetId, Name = a.Name, Icon = a.Icon, Apply = () => AvatarSdk.EquipHairAsync(_av, a) }); } break;
                    case 2: Colors(l, await AvatarSdk.GetDefaultColorsAsync(ColorType.Hair)); break;
                    case 3: Colors(l, await AvatarSdk.GetDefaultColorsAsync(ColorType.Skin)); break;
                    case 4: Colors(l, await AvatarSdk.GetDefaultColorsAsync(ColorType.Eyes)); break;
                    case 5: await Wear(l, WearablesCategory.Hoodie, WearablesCategory.Shirt, WearablesCategory.Jacket); break;
                    case 6: await Wear(l, WearablesCategory.Pants, WearablesCategory.Shorts, WearablesCategory.Skirt, WearablesCategory.Dress); break;
                    case 7: await Wear(l, WearablesCategory.Shoes); break;
                    case 8: await Wear(l, WearablesCategory.Hat); break;
                    case 9: await Wear(l, WearablesCategory.Glasses); break;
                    case 10: await Wear(l, WearablesCategory.Earrings, WearablesCategory.Mask); break;
                    case 11:
                        foreach (GenderType g in Enum.GetValues(typeof(GenderType))) foreach (BodySize b in Enum.GetValues(typeof(BodySize)))
                        { var gg = g; var bb = b; l.Add(new Item { Id = g + "_" + b, Name = $"{g} · {b}", Swatch = UiKit.MoonCream, Apply = () => AvatarSdk.SetAvatarBodyTypeAsync(_av, gg, bb) }); }
                        break;
                    case 12: foreach (var f in await AvatarSdk.GetDefaultAvatarFeaturesByCategory(AvatarFeatureCategory.All) ?? new List<AvatarFeaturesInfo>()) { var a = f; l.Add(new Item { Id = a.AssetId, Name = a.SubCategories != null && a.SubCategories.Count > 0 ? a.SubCategories[0] : "feature", Icon = a.Icon, Apply = async () => { await AvatarSdk.SetAvatarFeatureAsync(_av, a); } }); } break;
                }
            }
            catch (Exception e) { Status("Inventory error: " + e.Message); }
            _lists[k] = l;
            if (!_prefetching) PrefetchRest().Forget();
            if (_cat == k) { Refresh(); Status(l.Count == 0 ? (AvatarSdk.IsLoggedIn ? "Nothing available in this drawer" : "This drawer needs a Genies session (sign in on the Genies hub)") : $"{l.Count} items - poke one to try it on"); }
        }

        void Colors(List<Item> l, List<IAvatarColor> cs) { if (cs == null) return; int i = 0; foreach (var c in cs) { var cc = c; l.Add(new Item { Id = cc.AssetId ?? ("c" + i), Name = "", Swatch = cc.Hexes != null && cc.Hexes.Length > 0 ? cc.Hexes[0] : Color.gray, Apply = async () => { await AvatarSdk.SetColorAsync(_av, cc); } }); i++; } }

        async UniTask Wear(List<Item> l, params WearablesCategory[] cats)
        {
            foreach (var c in cats)
            {
                var d = await AvatarSdk.GetDefaultWearablesByCategoryAsync(c);
                if (d != null) foreach (var w in d) { var a = w; l.Add(new Item { Id = a.AssetId, Name = a.Name, Icon = a.Icon, Wearable = true, Apply = () => ToggleWear(a) }); }
            }
            if (AvatarSdk.IsLoggedIn && !AvatarSdk.IsLoggedInAnonymously)
            {
                // the player's own Genies items (owned on their account) are listed too; nothing purchasable is shown
                var u = await AvatarSdk.GetUserWearablesByCategoryAsync(UserWearablesCategory.All);
                if (u != null) foreach (var w in u) { if (!Matches(w.Category, cats)) continue; var a = w; l.Add(new Item { Id = a.AssetId, Name = "★ " + a.Name, Icon = a.Icon, Wearable = true, Apply = () => ToggleWear(a) }); }
            }
        }

        static bool Matches(string category, WearablesCategory[] cats) { if (string.IsNullOrEmpty(category)) return false; foreach (var c in cats) if (category.ToLowerInvariant().Contains(c.ToString().ToLowerInvariant())) return true; return false; }

        async UniTask ToggleWear(WearableAssetInfo a)
        {
            if (_av.IsAssetEquipped(a.AssetId)) await AvatarSdk.UnEquipWearableAsync(_av, a); else await AvatarSdk.EquipWearableAsync(_av, a);
        }

        async UniTask SetDef(string def) { if (!string.IsNullOrEmpty(def)) await _av.SetDefinitionAsync(def); }

        void Page(int d)
        {
            if (!_lists.TryGetValue(_cat, out var l)) return;
            int pages = Mathf.Max(1, Mathf.CeilToInt(l.Count / (float)(Cols * Rows)));
            _pageIx = (_pageIx + d + pages) % pages; Refresh();
        }

        void Refresh()
        {
            _lists.TryGetValue(_cat, out var l); l = l ?? new List<Item>();
            int pages = Mathf.Max(1, Mathf.CeilToInt(l.Count / (float)(Cols * Rows))); _page.text = $"{_pageIx + 1}/{pages}"; _iconsPending = false;
            for (int i = 0; i < _cells.Count; i++)
            {
                int ix = _pageIx * Cols * Rows + i; var cell = _cells[i];
                bool on = ix < l.Count; cell.btn.gameObject.SetActive(on); if (!on) continue;
                var it = l[ix];
                cell.label.text = it.Name ?? "";
                if (it.Icon == null && (it.Wearable || _cat == 1)) _iconsPending = true;
                if (it.Icon != null && it.Icon.texture != null)
                {
                    var tr = it.Icon.textureRect; var tex = it.Icon.texture;
                    cell.mat.SetTexture("_BaseMap", tex); cell.mat.mainTexture = tex; cell.mat.SetColor("_BaseColor", Color.white);
                    var st = new Vector4(tr.width / tex.width, tr.height / tex.height, tr.x / tex.width, tr.y / tex.height);
                    cell.mat.SetVector("_BaseMap_ST", st); cell.mat.mainTextureScale = new Vector2(st.x, st.y); cell.mat.mainTextureOffset = new Vector2(st.z, st.w);
                }
                else { cell.mat.SetTexture("_BaseMap", Texture2D.whiteTexture); cell.mat.mainTexture = Texture2D.whiteTexture; cell.mat.SetColor("_BaseColor", it.Swatch); cell.mat.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0)); }
                bool eq = _av != null && it.Wearable && _av.IsAssetEquipped(it.Id);
                cell.equipped.SetActive(eq); cell.btn.SetSelected(eq);
            }
        }

        void Press(int cellIx)
        {
            if (_busy || _av == null || !_lists.TryGetValue(_cat, out var l)) return;
            int ix = _pageIx * Cols * Rows + cellIx; if (ix >= l.Count) return;
            ApplyAsync(l[ix]).Forget();
        }

        /// <summary>QA: press item <paramref name="index"/> of category <paramref name="cat"/> (same path as a poke).</summary>
        public async UniTask QaApply(int cat, int index)
        {
            SelectCat(cat); float w = 0; while (!_lists.ContainsKey(cat) && w < 20f) { await UniTask.Delay(100); w += 0.1f; }
            if (_lists.TryGetValue(cat, out var l) && index < l.Count) await ApplyCore(l[index]);
        }
        public bool AvatarReady => _av != null && !_busy;
        public int ListCount(int cat) => _lists.TryGetValue(cat, out var l) ? l.Count : -1;
        public string CurrentDefinition => _av?.GetDefinition();
        public UniTask QaSave() => SaveCore();

        async UniTaskVoid ApplyAsync(Item it) { await ApplyCore(it); }

        async UniTask ApplyCore(Item it)
        {
            _busy = true; Status("Trying on " + (string.IsNullOrEmpty(it.Name) ? "that colour" : it.Name) + "...");
            var before = _av.GetDefinition();
            try
            {
                await it.Apply();
                if (_av.GetDefinition() != before) _undo.Push(before);
                AudioBank.I?.Play2D("rustle", 0.5f); Status(string.IsNullOrEmpty(it.Name) ? "Looking good!" : it.Name);
            }
            catch (Exception e) { Status("Could not apply: " + e.Message); }
            _busy = false; Refresh();
        }

        void ApplyPreset(int seat) { if (_busy || _av == null) return; var p = _be.PresetDefinition(seat); if (string.IsNullOrEmpty(p)) { Status("Starter looks are still being built..."); return; } ApplyAsync(new Item { Name = new[] { "Pip", "Nell", "Rory", "Tess" }[seat] + "'s look", Apply = () => SetDef(p) }).Forget(); }

        void Undo() { if (_busy || _av == null || _undo.Count == 0) return; var d = _undo.Pop(); _busy = true; UndoAsync(d).Forget(); }
        async UniTaskVoid UndoAsync(string d) { await _av.SetDefinitionAsync(d); _busy = false; Status("Undone"); Refresh(); }
        void ResetToSaved() { if (_busy || _av == null || string.IsNullOrEmpty(_openedDef)) return; _undo.Push(_av.GetDefinition()); _busy = true; UndoAsync(_openedDef).Forget(); }

        async UniTaskVoid SaveAsync() { await SaveCore(); }
        async UniTask SaveCore()
        {
            if (_av == null || _busy) return;
            _busy = true; Status("Saving...");
            if (!await _be.SavePlayerLookAsync(_av)) { _busy = false; Status("Put some clothes on first - a top and a bottom!"); AudioBank.I?.Play2D("recipe_undone", 0.5f); return; }
            _openedDef = _av.GetDefinition();
            _busy = false; Status("Saved! Everyone will see this look."); AudioBank.I?.Play2D("chore_done", 0.6f);
            if (_av.Animator != null) _av.Animator.SetTrigger("Cheer");
        }

        public void Close()
        {
            if (_av != null && !_av.IsDisposed) _av.Dispose(); _av = null;
            if (FamilyStage.I != null) { FamilyStage.I.gameObject.SetActive(true); FamilyStage.I.Rebuild(); }
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null;
            if (rig != null) rig.Spawn(_restoreSpawn, MenuBuilder.FrontEndYaw);
            if (MenuBuilder.FrontEndBoard != null) { MenuBuilder.FrontEndBoard.gameObject.SetActive(true); MenuBuilder.BringMenuToPlayer(); }
            Current = null; Destroy(gameObject); _onClosed?.Invoke();
        }

        void Update()
        {
            if (_plinth == null) return;
            if (_iconsPending && Time.unscaledTime >= _iconPoll) { _iconPoll = Time.unscaledTime + 0.5f; Refresh(); }
            if (Mathf.Abs(_spinVel) > 1f) { _plinth.Rotate(0, _spinVel * Time.deltaTime, 0, Space.World); _spinVel = Mathf.MoveTowards(_spinVel, 0, 90f * Time.deltaTime); }
        }

        void OnDestroy() { if (Current == this) Current = null; if (_av != null && !_av.IsDisposed) _av.Dispose(); }
    }
}
