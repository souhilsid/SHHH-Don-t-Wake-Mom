using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Diegetic menus: every screen is a physical board in the house (front hall notice board, fridge-door results,
    /// pin-board intro), pressed with fingertips. Boards spawn at a comfortable distance in front of the player and stay
    /// world-anchored (never head-locked).
    /// </summary>
    public static class MenuBuilder
    {
        static Transform _popup;

        static Vector3 InFront(float dist, float height)
        {
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null;
            if (rig == null || rig.Head == null) return new Vector3(0, height, dist);
            var f = rig.Head.forward; f.y = 0; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
            var p = rig.Head.position + f * dist; p.y = Mathf.Max(1.0f, rig.Head.position.y - 0.15f) + (height - 1.35f);
            return p;
        }

        static Transform Popup(string name, Vector2 size, float dist = 0.62f)
        {
            if (_popup != null) Object.Destroy(_popup.gameObject);
            var root = new GameObject(name).transform;
            // V6: every board uses the same comfortable "lectern" anchor as the main menu (chest height, ~0.6 m, tilted back,
            // 72% scale): pokeable without stepping or reaching up, and ray-pinchable from anywhere.
            PlaceLectern(root, dist);
            // A popup replaces the hall board while open (the recording showed both panels in the same place, text over text).
            if (_frontEnd != null) _frontEnd.gameObject.SetActive(false);
            UiKit.Panel(root, "board", Vector3.zero, size);
            _popup = root;
            return root;
        }

        public static Transform FrontEndBoard => _frontEnd;
        public static Transform CurrentPopup => _popup;

        /// <summary>World-anchored (never head-locked) placement in front of the player's current head pose.</summary>
        /// <summary>V12: one shared board spot - every popup opens where the previous board was (no jumping with each head turn),
        /// unless the player has turned / walked away from it; then a fresh spot in front of them.</summary>
        static Vector3 _anchorPos; static Quaternion _anchorRot; static bool _hasAnchor;

        public static void LecternPose(Transform head, float dist, out Vector3 pos, out Quaternion rot)
        {
            var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            float chest = Mathf.Clamp(head.position.y - 0.28f, 0.75f, 1.5f);
            pos = new Vector3(head.position.x, chest, head.position.z) + fwd * dist;
            // a gentle 10 deg tilt toward the eyes (18 deg made the board look steep from most heights)
            rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(10f, 0, 0);
        }

        public static void PlaceLectern(Transform t, float dist = 0.62f)
        {
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null;
            var head = rig != null && rig.Head != null ? rig.Head : (Camera.main != null ? Camera.main.transform : null);
            if (head == null) { t.position = new Vector3(0, 1.2f, dist); return; }
            bool reuse = false;
            if (_hasAnchor)
            {
                var toA = _anchorPos - head.position; toA.y = 0f; var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                reuse = toA.sqrMagnitude > 0.01f && fwd.sqrMagnitude > 0.01f && Vector3.Angle(fwd, toA) < 55f && Mathf.Abs(toA.magnitude - dist) < 0.7f;
            }
            if (!reuse) { LecternPose(head, dist, out _anchorPos, out _anchorRot); _hasAnchor = true; }
            t.SetPositionAndRotation(_anchorPos, _anchorRot);
            t.localScale = Vector3.one * 0.72f;
            var f = t.GetComponent<LecternFollow>() ?? t.gameObject.AddComponent<LecternFollow>(); f.Distance = dist;
        }

        /// <summary>A board glided to a new spot: later popups open there.</summary>
        public static void RememberLectern(Transform t) { _anchorPos = t.position; _anchorRot = t.rotation; _hasAnchor = true; }

        public static void ClosePopup() { if (_popup != null) Object.Destroy(_popup.gameObject); _popup = null; if (_frontEnd != null) _frontEnd.gameObject.SetActive(true); }
        static Transform _frontEnd;

        /// <summary>
        /// V6 UX-G2 fix (Quest recording: the hall board sat ~2.1 m away, forcing a room-scale walk to press SOLO).
        /// Re-anchors the physical menu board ~0.62 m in front of the player's real head, at chest height, facing them,
        /// at 72% size - within comfortable arm reach for poking, seated or standing. World-anchored, not head-locked.
        /// </summary>
        public static void BringMenuToPlayer()
        {
            if (_frontEnd == null) return;
            PlaceLectern(_frontEnd, 0.56f); // xr_diag: at 0.62 m the top key needed a 0.70 m shoulder reach (comfort <= 0.68)
            if (_popup != null) PlaceLectern(_popup);
        }

        static TextMeshPro T(Transform p, string text, float y, float size, Color c, float width = 0.9f, float x = 0f, TextAlignmentOptions a = TextAlignmentOptions.Center)
        {
            // V8: headings (big) in Luckiest Guy, everything else in Poppins; both with the ink outline
            bool heading = size >= 0.5f;
            var t = UiKit.Text(p, text, new Vector3(x, y, -0.015f), size, c, heading ? UiKit.Display : UiKit.Body, width, a, heading ? 0.24f : 0.14f);
            return t;
        }

        // ------------------------------------------------------------------ Front end (V8, 2026-11-09)
        /// <summary>Where the player stands in the front end: the real living room, facing the moonlit windows and the title sign.</summary>
        public static readonly Vector3 FrontEndSpawn = new Vector3(0f, 0f, 1.7f);
        public const float FrontEndYaw = 180f;

        /// <summary>
        /// V8 front end: the family's real living room at night (the same house the match plays in, with the night street outside),
        /// the approved SHHH! logo on a hanging sign over the sofa, the four siblings on the Family Stage, and one lectern board with
        /// four illustrated mode cards (Solo, Friends, Practice, Wardrobe) and a row of icon keys. Old front-hall box = fallback only.
        /// </summary>
        public static void BuildFrontEnd(Transform root, GameRoot game)
        {
            var house = HouseAssembly.Create(RoomCatalog.Get(RoomId.LivingRoom), root);
            if (house != null) NightExterior.Build(root, house.WorldBounds.center);
            else
            {
                var def = new RoomDefinition { Id = RoomId.Practice, Title = "Front Hall", Width = 5f, Depth = 5f }; def.MomDoor = new Vec3(0, 0, 2.45f);
                RoomShell.Build(def, root);
            }
            TitleSign.Build(root, new Vector3(0f, 2.3f, -1.75f), FrontEndYaw);
            // Main board: four mode cards + key row (placed at the comfortable lectern pose by BringMenuToPlayer)
            var board = UiKit.Panel(root, "MainMenu", new Vector3(0, 1.2f, 1.1f), new Vector2(1.26f, 0.84f), "PICK A MODE");
            board.localRotation = Quaternion.Euler(0, FrontEndYaw, 0);
            _frontEnd = board;
            var cs = new Vector2(0.255f, 0.35f); float cy = 0.08f;
            UiKit.Card(board, "solo", "SOLO", new Vector3(-0.45f, cy, 0), cs, () => ShowMatchSetup(game), "you vs 3 bots");
            UiKit.Card(board, "friends", "FRIENDS", new Vector3(-0.15f, cy, 0), cs, () => ShowOnline(game), "2-4 players online");
            UiKit.Card(board, "practice", "PRACTICE", new Vector3(0.15f, cy, 0), cs, () => game.StartPractice(), "learn the moves");
            UiKit.Card(board, "wardrobe", "WARDROBE", new Vector3(0.45f, cy, 0), cs, () => OpenAvatarWardrobe(game), "create your Genie");
            var ks = new Vector2(0.27f, 0.085f); float ky = -0.3f;
            PokeButton.Create(board, "Customize", new Vector3(-0.45f, ky, 0), ks, UiKit.Purple, () => ShowCustomize(game), 0.3f, "wardrobe");
            PokeButton.Create(board, "Settings", new Vector3(-0.15f, ky, 0), ks, UiKit.Purple, () => ShowSettings(game), 0.3f, "settings");
            PokeButton.Create(board, "How to win", new Vector3(0.15f, ky, 0), ks, UiKit.Teal, () => ShowRules(game), 0.3f, "card");
            PokeButton.Create(board, "Quit", new Vector3(0.45f, ky, 0), ks, UiKit.Coral, () => game.QuitApp(), 0.3f, "cross");
            UiKit.Text(board, "Poke a card with your finger - or point and pinch", new Vector3(0, -0.385f, -0.012f), 0.2f, UiKit.MoonCream, UiKit.Body, 1.1f, TextAlignmentOptions.Center, 0.1f);
            // V7 Family Stage: the four siblings beside the player, facing them
            FamilyStage.Build(root, new Vector3(-1.85f, 0f, 0.75f), 61f);
        }

        /// <summary>V8: the Genies wardrobe (primary character creator). Falls back to the legacy colour wardrobe only when the
        /// Genies SDK is not available, and says why.</summary>
        public static void OpenAvatarWardrobe(GameRoot game)
        {
            if (AvatarService.Available) { var head = game.Rig != null ? game.Rig.Head : null; AvatarService.Backend.OpenWardrobe(head, null); return; }
            ShowWardrobe(game);
            if (_popup != null) T(_popup, "Genies: " + AvatarService.Status, -0.44f, 0.2f, UiKit.Coral, 1.0f);
        }

        /// <summary>The in-game rules / help card (directive: simple published rules + tie-break order).</summary>
        public static void ShowRules(GameRoot game)
        {
            var p = Popup("Rules", new Vector2(1.2f, 0.92f));
            UiKit.Ribbon(p, "HOW TO WIN", new Vector3(0, 0.465f, -0.03f), 0.8f);
            var rows = new (string icon, string text)[] {
                ("check", "Finish YOUR jobs in every room before the clock hits 0:00."),
                ("noise", "Stay quiet. Noise fills Mom's sleep meter - she wakes up and comes to look."),
                ("sabotage", "Sneakily mess up a rival's job... but someone may be watching."),
                ("repair", "Fix what rivals mess up - catching them scores for you."),
                ("witness", "When Mom asks, show her what you saw (cards or your voice)."),
                ("penalty", "Caught by Mom: -20 points and 15 seconds in bed."),
                ("trophy", "Most points at 0:00 wins. Ties: more jobs > less noise > first job done.") };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 0.31f - i * 0.1f;
                UiKit.Icon(p, rows[i].icon, new Vector3(-0.5f, y, -0.012f), 0.075f);
                UiKit.Text(p, rows[i].text, new Vector3(0.06f, y, -0.012f), 0.27f, UiKit.SoftWhite, UiKit.Body, 1.0f, TextAlignmentOptions.Left, 0.12f);
            }
            PokeButton.Create(p, "Got it", new Vector3(0, -0.4f, 0), new Vector2(0.3f, 0.085f), UiKit.Hush, ClosePopup, 0.38f, "check");
        }

        /// <summary>Solo match setup (directive flow Mode -> MatchSetup): 5:00 or 10:00, bot skill, START.</summary>
        public static void ShowMatchSetup(GameRoot game)
        {
            var p = Popup("MatchSetup", new Vector2(1.1f, 0.8f));
            T(p, "SOLO MATCH", 0.32f, 0.6f, Palette.Yellow);
            T(p, "How long until Mom's alarm?", 0.21f, 0.32f, Palette.Cream);
            PokeButton b5 = null, b10 = null;
            System.Action refresh = () => { bool ten = Settings.Current.MatchMinutes >= 10; b5.SetColor(ten ? Palette.Cream : Palette.Yellow); b10.SetColor(ten ? Palette.Yellow : Palette.Cream); };
            b5 = PokeButton.Create(p, "5 MIN", new Vector3(-0.2f, 0.08f, 0), new Vector2(0.34f, 0.12f), Palette.Cream, () => { Settings.Current.MatchMinutes = 5; Settings.Save(); refresh(); }, 0.46f);
            b10 = PokeButton.Create(p, "10 MIN", new Vector3(0.2f, 0.08f, 0), new Vector2(0.34f, 0.12f), Palette.Cream, () => { Settings.Current.MatchMinutes = 10; Settings.Save(); refresh(); }, 0.46f);
            refresh();
            T(p, "Bot skill", -0.05f, 0.3f, Palette.Cream);
            var names = new[] { "Rookie", "Normal", "Clever" }; var bs = new System.Collections.Generic.List<PokeButton>();
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var b = PokeButton.Create(p, names[i], new Vector3(-0.27f + i * 0.27f, -0.14f, 0), new Vector2(0.25f, 0.09f), Settings.Current.BotDifficulty == i ? Palette.Yellow : Palette.Cream, null, 0.32f);
                b.OnPress = () => { Settings.Current.BotDifficulty = k; Settings.Save(); for (int j = 0; j < bs.Count; j++) bs[j].SetColor(j == k ? Palette.Yellow : Palette.Cream); };
                bs.Add(b);
            }
            PokeButton.Create(p, "Back", new Vector3(-0.27f, -0.3f, 0), new Vector2(0.24f, 0.09f), Palette.Cream, ClosePopup, 0.36f);
            PokeButton.Create(p, "START", new Vector3(0.12f, -0.3f, 0), new Vector2(0.44f, 0.11f), Palette.Teal, () => { ClosePopup(); game.StartSolo(); }, 0.48f);
        }

        /// <summary>Customize: pyjama colour (your duty colour) and bot skill - on one reachable board, not on the far wall.</summary>
        public static void ShowCustomize(GameRoot game)
        {
            var p = Popup("Customize", new Vector2(1.1f, 0.8f));
            var s = Settings.Current;
            T(p, "CUSTOMIZE", 0.32f, 0.6f, Palette.Yellow);
            T(p, "Your colour", 0.2f, 0.36f, Palette.Cream);
            var swatches = new System.Collections.Generic.List<PokeButton>();
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                var b = PokeButton.Create(p, Palette.SiblingColorNames[i], new Vector3(-0.39f + i * 0.26f, 0.09f, 0), new Vector2(0.24f, 0.1f), Palette.Siblings[i], null, 0.32f);
                b.OnPress = () => { Settings.Current.Cosmetic = k; Settings.Save(); FamilyStage.I?.Rebuild(); AudioBank.I?.Play2D("step_done"); foreach (var w in swatches) w.SetLabel((w == b ? "> " : "") + Palette.SiblingColorNames[swatches.IndexOf(w)]); };
                swatches.Add(b);
                if (s.Cosmetic == i) b.SetLabel("> " + Palette.SiblingColorNames[i]);
            }
            // V12 (Quest run): bot skill lives in the SOLO panel only; the look you wear is your Genies avatar (the old pyjama / hat
            // wardrobe is only the fallback when Genies is unavailable)
            T(p, "Your look", -0.06f, 0.36f, Palette.Cream);
            PokeButton.Create(p, AvatarService.Available ? "My Genies look" : "Wardrobe", new Vector3(0f, -0.17f, 0), new Vector2(0.5f, 0.1f), Palette.Purple, () => { ClosePopup(); OpenAvatarWardrobe(game); }, 0.36f);
            PokeButton.Create(p, "Done", new Vector3(0f, -0.32f, 0), new Vector2(0.3f, 0.09f), Palette.Teal, ClosePopup, 0.4f);
        }

        /// <summary>V7 Gate 8 wardrobe: hat / glasses / pyjama / accessory / emote for your sibling, unlocked by playing (local save).</summary>
        public static void ShowWardrobe(GameRoot game)
        {
            ClosePopup();
            var p = Popup("Wardrobe", new Vector2(1.2f, 0.95f));
            var prog = ProgressService.Current;
            int colour = Settings.Current.Cosmetic; string character = CharacterCatalog.SiblingKeys[((colour % 4) + 4) % 4];
            T(p, $"WARDROBE - {char.ToUpper(character[0]) + character.Substring(1)}", 0.41f, 0.6f, Palette.Yellow);
            T(p, $"Level {prog.Level}  ·  {prog.Xp} XP  ·  {prog.Achievements.Count}/{ProgressionRules.AchievementList.Length} achievements  ·  {prog.Wins} wins", 0.31f, 0.3f, Palette.Cream, 1.05f);
            for (int r = 0; r < Cosmetics.Slots.Length; r++)
            {
                string slot = Cosmetics.Slots[r]; float y = 0.18f - r * 0.105f;
                var items = System.Array.FindAll(Cosmetics.All, c => c.Slot == slot);
                T(p, char.ToUpper(slot[0]) + slot.Substring(1), y, 0.25f, Palette.Moon).transform.localPosition = new Vector3(-0.44f, y, -0.015f);
                PokeButton cur = null;
                System.Action refresh = () =>
                {
                    var id = ProgressionRules.EquippedOr(prog, character, slot); var it = Cosmetics.Get(id) ?? items[0];
                    cur.SetLabel(it.Name);
                };
                System.Action<int> step = d => { WardrobeStep(prog, character, slot, d); refresh(); };
                PokeButton.Create(p, "<", new Vector3(-0.22f, y, 0), new Vector2(0.08f, 0.08f), Palette.Cream, () => step(-1), 0.4f);
                cur = PokeButton.Create(p, "", new Vector3(0.05f, y, 0), new Vector2(0.4f, 0.08f), Palette.Teal, null, 0.3f);
                PokeButton.Create(p, ">", new Vector3(0.32f, y, 0), new Vector2(0.08f, 0.08f), Palette.Cream, () => step(1), 0.4f);
                int locked = 0; foreach (var it in items) if (!prog.Unlocked.Contains(it.Id)) locked++;
                if (locked > 0) T(p, $"{locked} locked", y, 0.2f, Palette.Moon).transform.localPosition = new Vector3(0.48f, y, -0.015f);
                refresh();
            }
            T(p, "Unlock more by playing: level ups and achievements. Everyone sees your look.", -0.36f, 0.26f, Palette.Moon, 1.05f);
            PokeButton.Create(p, "Done", new Vector3(0, -0.43f, 0), new Vector2(0.3f, 0.07f), Palette.Teal, ClosePopup, 0.36f);
        }

        /// <summary>One "‹" / "›" press in the wardrobe: equip the next unlocked item of that slot. Returns whether the look changed.</summary>
        public static bool WardrobeStep(PlayerProgress prog, string character, string slot, int d)
        {
            var items = System.Array.FindAll(Cosmetics.All, c => c.Slot == slot);
            string cur = ProgressionRules.EquippedOr(prog, character, slot);
            int idx = System.Math.Max(0, System.Array.FindIndex(items, c => c.Id == cur));
            for (int k = 1; k < items.Length; k++)
            {
                int j = ((idx + d * k) % items.Length + items.Length) % items.Length;
                var it = items[j];
                if (!prog.Unlocked.Contains(it.Id) || it.Id == cur) continue;
                ProgressionRules.Equip(prog, character, it.Id); ProgressService.Save(); AudioBank.I?.Play2D("step_done");
                // Genies bodies do not wear these pieces: only the stand-in bodies need a refresh (and it never rebuilds Genies bodies)
                FamilyStage.I?.RefreshOutfits();
                return true;
            }
            // everything else in this row is still locked: say so, change nothing (crash 2026-11-12: every press rebuilt all 4 Genies bodies)
            AudioBank.I?.Play2D(AudioBank.I.Has("locked") ? "locked" : "step_fail");
            return false;
        }

        // ------------------------------------------------------------------ Online (honest status until Fusion is integrated)
        // ------------------------------------------------------------------ Online (V6: real Photon Fusion rooms)
        public static void ShowOnline(GameRoot game)
        {
            var net = GameServices.Net;
            if (net == null) { ShowMessage("PLAY WITH FRIENDS", "Online play is not included in this build."); return; }
            if (net.Active) { ShowLobby(game); return; }
            var p = Popup("Online", new Vector2(1.0f, 0.78f));
            T(p, "PLAY WITH FRIENDS", 0.3f, 0.6f, Palette.Yellow);
            T(p, "2-4 humans in a private room. Empty seats = bots.", 0.2f, 0.32f, Palette.Cream, 0.95f);
            PokeButton.Create(p, "CREATE ROOM", new Vector3(0, 0.06f, 0), new Vector2(0.62f, 0.12f), Palette.Teal, () =>
            {
                net.Create(PlayerName(), ProgressService.LocalCosmetic(Settings.Current.Cosmetic)); ShowLobby(game);
            }, 0.46f);
            PokeButton.Create(p, "JOIN WITH CODE", new Vector3(0, -0.1f, 0), new Vector2(0.62f, 0.12f), Palette.Yellow, () => ShowKeypad(game), 0.46f);
            PokeButton.Create(p, "Back", new Vector3(0, -0.27f, 0), new Vector2(0.3f, 0.09f), Palette.Cream, ClosePopup, 0.38f);
        }

        static string PlayerName() => string.IsNullOrEmpty(Settings.Current.PlayerName) || Settings.Current.PlayerName == "You" ? "Player" + Random.Range(10, 99) : Settings.Current.PlayerName;

        /// <summary>Room codes use 4 letters without look-alikes (no I/O/L) - spoken easily over voice chat.</summary>
        public const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ";

        public static void ShowKeypad(GameRoot game)
        {
            var p = Popup("JoinCode", new Vector2(1.0f, 0.95f));
            T(p, "ENTER ROOM CODE", 0.39f, 0.5f, Palette.Yellow);
            var code = new System.Text.StringBuilder();
            var display = T(p, "_ _ _ _", 0.28f, 0.9f, Palette.Cream);
            void Refresh() { var c = code.ToString().PadRight(4, '_'); display.text = string.Join(" ", c.ToCharArray()); }
            for (int i = 0; i < CodeAlphabet.Length; i++)
            {
                char ch = CodeAlphabet[i]; int col = i % 6, row = i / 6;
                PokeButton.Create(p, ch.ToString(), new Vector3(-0.35f + col * 0.14f, 0.14f - row * 0.11f, 0), new Vector2(0.12f, 0.095f), Palette.Cream, () => { if (code.Length < 4) { code.Append(ch); Refresh(); } }, 0.5f);
            }
            PokeButton.Create(p, "Del", new Vector3(0.35f, 0.14f - 3 * 0.11f, 0), new Vector2(0.12f, 0.095f), Palette.Coral, () => { if (code.Length > 0) { code.Length--; Refresh(); } }, 0.36f);
            PokeButton.Create(p, "JOIN", new Vector3(0.13f, -0.38f, 0), new Vector2(0.34f, 0.1f), Palette.Teal, () =>
            {
                if (code.Length != 4) { RoomFx.Say(p.position + Vector3.up * 0.3f, "4 letters!", Palette.Coral, 1.2f, 0.5f); return; }
                GameServices.Net.Join(code.ToString(), PlayerName(), ProgressService.LocalCosmetic(Settings.Current.Cosmetic)); ShowLobby(game);
            }, 0.42f);
            PokeButton.Create(p, "Back", new Vector3(-0.22f, -0.38f, 0), new Vector2(0.26f, 0.1f), Palette.Cream, () => ShowOnline(game), 0.38f);
        }

        public static void ShowLobby(GameRoot game)
        {
            var p = Popup("Lobby", new Vector2(1.1f, 0.95f));
            var lb = p.gameObject.AddComponent<LobbyBoard>();
            lb.Code = T(p, "", 0.37f, 0.62f, Palette.Yellow, 1.0f);
            lb.StatusText = T(p, "", 0.27f, 0.28f, Palette.Moon, 1.0f);
            for (int i = 0; i < 4; i++) lb.Seats[i] = T(p, "", 0.17f - i * 0.075f, 0.36f, Palette.Cream, 0.9f, -0.02f, TextAlignmentOptions.Left);
            lb.Ready = PokeButton.Create(p, "I'M READY", new Vector3(-0.25f, -0.2f, 0), new Vector2(0.4f, 0.11f), Palette.Teal, () => GameServices.Net.SetReady(!GameServices.Net.LocalReady), 0.4f);
            lb.Start = PokeButton.Create(p, "START", new Vector3(0.25f, -0.2f, 0), new Vector2(0.4f, 0.11f), Palette.Yellow, () => { if (GameServices.Net.CanStart) GameServices.Net.StartMatch(); }, 0.46f);
            PokeButton.Create(p, "Leave", new Vector3(0, -0.36f, 0), new Vector2(0.28f, 0.09f), Palette.Coral, () => { GameServices.Net.Leave(); ClosePopup(); }, 0.36f);
        }

        public static void ShowMessage(string title, string body)
        {
            var p = Popup("Message", new Vector2(1.0f, 0.62f));
            T(p, title, 0.2f, 0.55f, Palette.Yellow);
            T(p, body, 0.03f, 0.36f, Palette.Cream, 0.9f);
            PokeButton.Create(p, "OK", new Vector3(0, -0.2f, 0), new Vector2(0.3f, 0.09f), Palette.Teal, ClosePopup, 0.4f);
        }

        public static void ShowOnlineInfo(GameRoot game) => ShowOnline(game);

        public static void ShowSettings(GameRoot game)
        {
            var p = Popup("Settings", new Vector2(1.1f, 0.9f));
            var s = Settings.Current;
            T(p, "SETTINGS", 0.35f, 0.6f, Palette.Yellow);
            TextMeshPro status = T(p, "", 0.24f, 0.36f, Palette.Cream, 1f);
            void Refresh() => status.text = $"Music {(s.MusicOn ? Mathf.RoundToInt(s.MusicVolume * 100) + "%" : "OFF")}   Sounds {Mathf.RoundToInt(s.SfxVolume * 100)}%   Voices {Mathf.RoundToInt(s.VoiceVolume * 100)}%\nSnap turn {s.SnapAngle:0}°   Move: {(s.TeleportMode ? "Teleport" : "Hand-walk")}   {(s.Seated ? "Seated" : "Standing")}   Mic {(s.VoiceChatOn ? "ON" : "OFF")}";
            Refresh();
            void Btn(string label, float x, float y, System.Action a) => PokeButton.Create(p, label, new Vector3(x, y, 0), new Vector2(0.3f, 0.085f), Palette.Cream, () => { a(); Settings.Apply(); Settings.Save(); Refresh(); ApplyRig(game); }, 0.3f);
            float Dn(float v) => Mathf.Clamp01(Mathf.Round(v * 10f - 1f) / 10f);
            float Up(float v) => Mathf.Clamp01(Mathf.Round(v * 10f + 1f) / 10f);
            // three columns: volume down | volume up | toggles. Every press plays a short sample on that bus so the level is heard.
            Btn("Music -", -0.34f, 0.12f, () => { s.MusicVolume = Dn(s.MusicVolume); s.MusicOn = s.MusicVolume > 0.01f; });
            Btn("Music +", 0f, 0.12f, () => { s.MusicVolume = Up(s.MusicVolume); s.MusicOn = true; });
            Btn("Music on/off", 0.34f, 0.12f, () => s.MusicOn = !s.MusicOn);
            Btn("Sounds -", -0.34f, 0.01f, () => { s.SfxVolume = Dn(s.SfxVolume); Sample(AudioBus.Sfx); });
            Btn("Sounds +", 0f, 0.01f, () => { s.SfxVolume = Up(s.SfxVolume); Sample(AudioBus.Sfx); });
            Btn("Snap 30/45", 0.34f, 0.01f, () => s.SnapAngle = s.SnapAngle < 40 ? 45 : 30);
            Btn("Voices -", -0.34f, -0.1f, () => { s.VoiceVolume = Dn(s.VoiceVolume); Sample(AudioBus.Voice); });
            Btn("Voices +", 0f, -0.1f, () => { s.VoiceVolume = Up(s.VoiceVolume); Sample(AudioBus.Voice); });
            Btn("Walk / hop", 0.34f, -0.1f, () => PlayerRig.CycleLocomotion(s));
            Btn("Seated/Standing", -0.34f, -0.21f, () => s.Seated = !s.Seated);
            Btn("Skip intros", 0f, -0.21f, () => s.SkipIntro = !s.SkipIntro);
            Btn("Mic on/off", 0.34f, -0.21f, () => { s.VoiceChatOn = !s.VoiceChatOn; GameServices.Net?.SetMic(s.VoiceChatOn); });
            PokeButton.Create(p, "Done", new Vector3(0, -0.33f, 0), new Vector2(0.3f, 0.09f), Palette.Teal, ClosePopup, 0.4f);
        }

        static void Sample(AudioBus bus)
        {
            if (AudioBank.I == null) return;
            if (bus == AudioBus.Voice) AudioBank.I.Play2D(AudioBank.I.Has("mom_babble") ? "mom_babble" : "mom_snore", 0.8f, AudioBus.Voice);
            else AudioBank.I.Play2D("impact_wood", 0.8f, AudioBus.Sfx);
        }

        static void ApplyRig(GameRoot g)
        {
            if (g.Rig == null) return;
            var s = Settings.Current;
            g.Rig.SnapAngle = s.SnapAngle; g.Rig.ApplyLocomotion(s); g.Rig.SeatedOffset = s.Seated ? 0.45f : 0f;
        }

        // ------------------------------------------------------------------ Intro: the roster (truthful HUMAN/BOT) + Mom's rules
        public static void ShowIntro(GameRoot game, MatchController m)
        {
            var p = Popup("Intro", new Vector2(1.2f, 0.85f), 0.9f);
            T(p, "TONIGHT'S SIBLINGS", 0.34f, 0.6f, Palette.Yellow);
            for (int i = 0; i < 4; i++)
            {
                var s = m.Roster[i];
                float x = -0.42f + i * 0.28f;
                Prim.Box(p, "seat" + i, new Vector3(x, 0.12f, -0.005f), new Vector3(0.25f, 0.26f, 0.01f), Mats.Lit(Palette.Siblings[s.CosmeticId % 4]), false);
                T(p, $"{s.DisplayName}\n<size=70%>{(s.IsBot ? "BOT · " + s.Difficulty : "HUMAN")}</size>", 0.12f, 0.38f, Palette.Navy, 0.24f, x);
            }
            T(p, MomLines.Text("mom_intro_v5"), -0.15f, 0.34f, Palette.Cream, 1.1f);
            T(p, m.Continuous ? $"<color=#65C8C5>{m.MatchLength / 60f:0} minutes, the whole house.  Finish your jobs, stay quiet, fix what rivals mess up.  Mom's caught = points off + a short timeout.</color>"
                              : "<color=#65C8C5>Your colour = your job.  Crash = caught.  Least helpful = bedtime.</color>", -0.3f, 0.3f, Palette.Cream, 1.1f);
            PokeButton.Create(p, "LET'S GO", new Vector3(0, -0.38f, 0), new Vector2(0.4f, 0.08f), Palette.Yellow, () => { ClosePopup(); m.SkipIntro(); }, 0.4f);
            AudioBank.I?.Play2D(AudioBank.I.Has("vo_mom_intro_v5") ? "vo_mom_intro_v5" : "mom_babble", 0.9f, AudioBus.Voice);
            game.StartCoroutine(AutoClose(m.IntroSeconds + 0.2f));
        }

        static System.Collections.IEnumerator AutoClose(float s) { yield return new WaitForSeconds(s); ClosePopup(); }

        // ------------------------------------------------------------------ Spectator (no dead ends)
        public static void ShowSpectatorBoard(GameRoot game, RoomRuntime room)
        {
            var p = Popup("Spectator", new Vector2(0.9f, 0.55f), 0.7f);
            T(p, "YOU'RE OFF TO BED... (spectating)", 0.19f, 0.45f, Palette.Yellow);
            T(p, "You can't touch anything now - but you can cheer!", 0.09f, 0.3f, Palette.Cream);
            PokeButton.Create(p, "Next view", new Vector3(-0.28f, -0.05f, 0), new Vector2(0.25f, 0.08f), Palette.Teal, game.NextSpectatorView, 0.3f);
            PokeButton.Create(p, "Cheer!", new Vector3(0f, -0.05f, 0), new Vector2(0.25f, 0.08f), Palette.Yellow, () => RoomFx.Say(InFront(1.2f, 1.6f), "Yay!", Palette.Yellow, 1.5f, 1.2f), 0.3f);
            PokeButton.Create(p, "Restart", new Vector3(0.28f, -0.05f, 0), new Vector2(0.25f, 0.08f), Palette.Coral, () => { ClosePopup(); game.Rematch(); }, 0.3f);
            PokeButton.Create(p, "Main menu", new Vector3(0f, -0.18f, 0), new Vector2(0.3f, 0.08f), Palette.Cream, () => { ClosePopup(); game.BackToMenu(); }, 0.3f);
        }

        // ------------------------------------------------------------------ Results (winner is always a single sibling, human or bot)
        public static void ShowResults(GameRoot game, MatchController m)
        {
            var sum = m.Summary;
            // the podium stands ~2.4 m in front of the player, the board at the lectern pose in front of it
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null; var head = rig != null && rig.Head != null ? rig.Head : (Camera.main != null ? Camera.main.transform : null);
            if (head != null)
            {
                var f = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
                var pos = head.position + f * 2.4f; pos.y = rig != null ? rig.transform.position.y : 0f;
                ResultsPodium.Build(m, pos, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
            }
            var p = Popup("Results", new Vector2(1.2f, 0.86f), 0.95f);
            UiKit.Ribbon(p, "RESULTS", new Vector3(0, 0.435f, -0.03f), 0.7f);
            string winner = sum.WinnerSlot >= 0 ? m.Roster[sum.WinnerSlot].DisplayName : "Nobody";
            bool you = sum.WinnerSlot == GameServices.LocalSlot;
            T(p, you ? "YOU WIN!" : $"{winner} WINS!" + (sum.WinnerIsBot ? " <size=60%>(BOT)</size>" : ""), 0.3f, 0.72f, UiKit.Hush, 1.15f);
            T(p, MomLines.Text("mom_win", winner), 0.19f, 0.27f, UiKit.MoonCream, 1.1f);
            var order = sum.Continuous && sum.Ranking.Count > 0 ? sum.Ranking : new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            for (int k = 0; k < order.Count && k < 4; k++)
            {
                int i = order[k]; var s = m.Roster[i]; float y = 0.09f - k * 0.075f;
                string place = sum.Continuous ? new[] { "1st", "2nd", "3rd", "4th" }[k] : i == sum.WinnerSlot ? "1st" : s.EliminatedInRound >= 0 ? (4 - s.EliminatedInRound) switch { 4 => "4th", 3 => "3rd", _ => "2nd" } : "-";
                UiKit.Icon(p, k == 0 ? "crown" : "star", new Vector3(-0.52f, y, -0.012f), 0.055f);
                T(p, $"{place}  {s.DisplayName}{(s.IsBot ? " <size=70%>(bot)</size>" : "")}", y, 0.32f, i == GameServices.LocalSlot ? UiKit.Hush : UiKit.SoftWhite, 0.5f, -0.21f, TextAlignmentOptions.Left);
                T(p, $"{(sum.Continuous ? $"<b>{sum.FinalScore[i]:0}</b> pts   " : "")}jobs {sum.RecipesCompleted[i] + sum.ChoresCompleted[i]}  fixed {sum.Foils[i]}  caught {sum.CaughtByEvidence[i]}{(sum.Continuous && sum.Penalties[i] > 0 ? $"  <color=#F57F78>Mom -{sum.Penalties[i]:0}</color>" : "")}", y, 0.26f, UiKit.MoonCream, 0.62f, 0.27f, TextAlignmentOptions.Right);
            }
            if (!string.IsNullOrEmpty(sum.FunniestIncident)) T(p, "Loudest moment: " + sum.FunniestIncident, -0.23f, 0.24f, UiKit.Coral, 1.1f);
            var awards = ProgressService.AwardMatch(m, GameServices.LocalSlot);
            if (awards.Count > 0) T(p, string.Join("   ", awards), -0.3f, 0.22f, UiKit.Mint, 1.1f);
            PokeButton.Create(p, "REMATCH", new Vector3(-0.22f, -0.37f, 0), new Vector2(0.38f, 0.1f), UiKit.Hush, () => { ClosePopup(); ResultsPodium.Clear(); game.Rematch(); }, 0.42f, "retry");
            PokeButton.Create(p, "MAIN MENU", new Vector3(0.22f, -0.37f, 0), new Vector2(0.38f, 0.1f), UiKit.Teal, () => { ClosePopup(); ResultsPodium.Clear(); game.BackToMenu(); }, 0.42f, "home");
        }

        // ------------------------------------------------------------------ Pause (wrist "Menu")
        public static void ShowPauseBoard(GameRoot game)
        {
            var p = Popup("Pause", new Vector2(0.8f, 0.45f), 0.6f);
            T(p, "PAUSE MENU", 0.15f, 0.5f, Palette.Yellow);
            T(p, "The house keeps going while you're here!", 0.06f, 0.3f, Palette.Cream);
            PokeButton.Create(p, "Back", new Vector3(-0.25f, -0.09f, 0), new Vector2(0.22f, 0.08f), Palette.Teal, ClosePopup, 0.32f);
            PokeButton.Create(p, "Settings", new Vector3(0f, -0.09f, 0), new Vector2(0.22f, 0.08f), Palette.Purple, () => ShowSettings(game), 0.32f);
            PokeButton.Create(p, "Quit match", new Vector3(0.25f, -0.09f, 0), new Vector2(0.22f, 0.08f), Palette.Coral, () => { ClosePopup(); game.BackToMenu(); }, 0.28f);
        }

        // ------------------------------------------------------------------ Tutorial (practice room; Mom only warns)
        public static void ShowTutorial(GameRoot game, RoomRuntime room)
        {
            var p = new GameObject("TutorialBoard").transform;
            // V12 (owner, Quest c15: the board hung on the far wall, too small to read from the practice table): it stands just right of
            // the practice bench, at eye height, turned to where the player starts, and 25 % bigger
            var start = room != null ? room.SpawnFor(0) : new Vector3(-1.2f, 0f, -2f);
            var at = room != null ? room.transform.TransformPoint(new Vector3(0.15f, 1.5f, -1.2f)) : new Vector3(0.15f, 1.5f, -1.2f);
            p.position = at; var face = at - (start + Vector3.up * 1.5f); face.y = 0f; p.rotation = Quaternion.LookRotation(face.sqrMagnitude > 1e-4f ? face : Vector3.forward);
            p.localScale = Vector3.one * 1.25f;
            WorldText.Board(p, "board", Vector3.zero, new Vector2(0.95f, 0.85f));
            T(p, "PRACTICE", 0.36f, 0.55f, Palette.Yellow);
            T(p, "1  Pinch (thumb + index) near the BOOK to grab it.\n2  Put it on the glowing spot on the shelf.\n3  Lower the CUP slowly onto its tray - fast = loud!\n4  Drop the PAN and watch the noise ripple and Mom's monitor.\n5  WALK - LEFT hand finger gun (index out, thumb up): point, HOLD the thumb down to walk.\n6  HOP - RIGHT hand finger gun: point at the floor, DROP the thumb to hop there.\n7  Turn your LEFT palm to your face for the wrist menu.\n\nHere Mom only grumbles - nobody gets sent to bed.", 0.0f, 0.3f, Palette.Cream, 0.9f, 0, TextAlignmentOptions.Left);
            PokeButton.Create(p, "Done - back to menu", new Vector3(0, -0.36f, 0), new Vector2(0.5f, 0.08f), Palette.Teal, () => { Settings.Current.TutorialDone = true; Settings.Save(); game.BackToMenu(); }, 0.32f);
        }
    }
}
