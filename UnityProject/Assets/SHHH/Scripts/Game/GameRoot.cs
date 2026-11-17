using System.Collections;
using SHHH.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.Game
{
    public enum AppState { Boot, FrontEnd, Intro, InMatch, Results, Practice }

    /// <summary>
    /// Application flow owner (lives in the Boot scene for the whole session). Runs the authoritative MatchController for
    /// offline solo (1 human + 3 bots), loads each room as its own additive scene (Living Room -> Kitchen -> Laundry),
    /// keeps party/scores across rooms, and stages intro, spectating, winner, results, rematch and the main menu.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }
        public static bool Autopilot;          // automated tests: human seat played by a bot body, faster time
        public static float AutopilotTimeScale = 3f;
        /// <summary>Network QA: the local seat is played by a bot body (authoritative host only), real time, no auto-solo.</summary>
        public static bool AutopilotLocalSeat;
        public PlayerRig Rig;
        public WristMenu Wrist;
        public AppState State { get; private set; } = AppState.Boot;
        public MatchController Match { get; private set; }
        public RoomRuntime Room { get; private set; }
        string _loadedScene;
        bool _busy;
        int _spectatorView;
        public event System.Action<string> Milestone;   // for automated tests / QA evidence

        static readonly string[] RoomScenes = { "LivingRoom", "Kitchen", "Laundry", "Practice" };

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            BootTimeline.Mark("Boot: GameRoot.Awake begins");
            Application.targetFrameRate = 72;
            QualitySettings.vSyncCount = 0;
            PerfMonitor.Ensure();   // V12: frame-rate log on the headset + fixed foveated rendering
            Settings.Load();
            if (AudioBank.I == null) new GameObject("AudioBank").AddComponent<AudioBank>().transform.SetParent(transform, false);
            Settings.Apply();
            CartoonLook.Apply(); // stylized ambient + fog + cheap cartoon grading (see CartoonLook)
            if (Rig == null) Rig = FindFirstObjectByType<PlayerRig>();
            if (Rig != null)
            {
                Rig.Init();
                foreach (var smr in Rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (smr.name.ToLowerInvariant().Contains("hand") && smr.GetComponentInParent<SimpleHandVisual>() == null) smr.sharedMaterial = Mats.Lit(Palette.Hex(0xF2C7A5));
                Rig.ApplyLocomotion(Settings.Current); Rig.SnapAngle = Settings.Current.SnapAngle;
                if (Rig.Left) PokeButton.Pokers.Add(Rig.Left);
                if (Rig.Right) PokeButton.Pokers.Add(Rig.Right);
                // V6: far UI = pointer ray + pinch on every board (near = poke). Both always work.
                var ptr = Rig.gameObject.AddComponent<UiPointer>();
                ptr.Bind(Rig.Left, Rig.LeftHand); ptr.Bind(Rig.Right, Rig.RightHand);
                if (Wrist == null) Wrist = FindFirstObjectByType<WristMenu>();
                if (Wrist != null) { Wrist.Rig = Rig; Wrist.Build(); Wrist.OnMenuPressed = OnWristMenu; }
            }
            // On-device QA (one-shot): `adb shell touch /sdcard/Android/data/<pkg>/files/qa_autopilot.flag` runs an unattended
            // real-time solo match so frame timing can be captured without anyone wearing the headset. Never present in normal installs.
            if (!Application.isEditor)
            {
                var qa = System.IO.Path.Combine(Application.persistentDataPath, "qa_autopilot.flag");
                if (System.IO.File.Exists(qa)) { try { System.IO.File.Delete(qa); } catch { } Autopilot = true; AutopilotTimeScale = 1f; }
            }
            Debug.Log($"[SHHH] Boot. Unity {Application.version} autopilot={Autopilot} xr={UnityEngine.XR.XRSettings.isDeviceActive}");
            BootTimeline.Mark("Boot: GameRoot.Awake done");
        }

        void Start() { BootTimeline.Mark("Boot: first Start"); AvatarService.Backend?.Warmup(); BootTimeline.Mark("Boot: Genies warm-up called"); if (Autopilot) StartSolo(); else StartCoroutine(GoFrontEnd()); }

        // ------------------------------------------------------------------ front end
        public IEnumerator GoFrontEnd()
        {
            if (_busy) yield break; _busy = true;
            Match = null; GameServices.Match = null; GameServices.PracticeRound = null; GameServices.PracticeMode = false;
            if (GameServices.Net == null || !GameServices.Net.Active) GameServices.IsAuthority = true;
            yield return SwapScene("FrontEnd");
            BootTimeline.Mark("front end scene loaded");
            var root = new GameObject("FrontEndRoot");
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetSceneByName("FrontEnd"));
            MenuBuilder.BuildFrontEnd(root.transform, this);
            BootTimeline.Mark("front end built");
            Rig?.Spawn(MenuBuilder.FrontEndSpawn, MenuBuilder.FrontEndYaw);
            if (Rig) Rig.LocomotionEnabled = true;
            yield return null; yield return null; // let the XR camera pose settle
            MenuBuilder.BringMenuToPlayer();
            AudioBank.I?.SetMatchMusic(false);
            AudioBank.I?.SetMusic("music_menu");
            AudioBank.I?.SetAmbience(null);   // owner 2026-11-15: no background "sea waves" (amb_night wind bed) - silence instead
            State = AppState.FrontEnd;
            Rig?.FadeTo(0f);
            _busy = false;
            BootTimeline.Mark("front end shown");
            Milestone?.Invoke("frontend");
        }

        IEnumerator SwapScene(string next)
        {
            Rig?.FadeTo(1f); if (Rig != null) Rig.Loading = true;
            float t = 0; while (Rig != null && !Rig.FadeDone && t < 1.5f) { t += Time.unscaledDeltaTime; yield return null; }
            if (!string.IsNullOrEmpty(_loadedScene) && SceneManager.GetSceneByName(_loadedScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(_loadedScene);
            Room = null; GameServices.Room = null;
            if (Application.CanStreamedLevelBeLoaded(next))
            {
                yield return SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(next));
            }
            else
            {
                var s = SceneManager.CreateScene(next); SceneManager.SetActiveScene(s);
                Debug.LogWarning("[SHHH] scene " + next + " not in build — created empty runtime scene");
            }
            _loadedScene = next;
        }

        /// <summary>One continuous match: 5 or 10 minutes (Settings), QA may shorten it with the SHHH_MATCH_SECONDS environment variable.</summary>
        public static float MatchSeconds
        {
            get
            {
                var env = System.Environment.GetEnvironmentVariable("SHHH_MATCH_SECONDS");
                if (!string.IsNullOrEmpty(env) && float.TryParse(env, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qa) && qa >= 60f) return qa;
                return Settings.Current.MatchMinutes >= 10 ? MatchRules.TenMinutes : MatchRules.FiveMinutes;
            }
        }

        // ------------------------------------------------------------------ solo competitive match (offline)
        public void StartSolo()
        {
            if (_busy) return;
            var s = Settings.Current;
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
            if (Autopilot) seed = 20261105;
            Match = MatchController.CreateSolo(string.IsNullOrEmpty(s.PlayerName) ? "You" : s.PlayerName, (BotDifficulty)Mathf.Clamp(s.BotDifficulty, 0, 2), seed);
            Match.Roster[0].CosmeticId = ProgressService.LocalCosmetic(s.Cosmetic); // colour + equipped wardrobe
            Match.SetMatchLength(MatchSeconds);
            Match.WaitForRoomReady = true;
            if (Settings.Current.SkipIntro || Autopilot) Match.IntroSeconds = 0.5f;
            HookMatch();
            GameServices.Match = Match; GameServices.LocalSlot = 0; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            if (!Match.Start(MatchMode.Solo, seed, out var err)) { Debug.LogError("[SHHH] cannot start: " + err); return; }
            for (int i = 1; i < CompetitorRoster.SlotCount; i++) if (Match.Roster[i].IsBot) Match.Roster[i].CosmeticId = Outfit.ForBot(Match.Roster[i].CosmeticId, seed); // bots dress up too
            State = AppState.Intro;
            MenuBuilder.ShowIntro(this, Match);
            Milestone?.Invoke("match_start");
        }

        // ------------------------------------------------------------------ private online match (Photon Fusion host mode)
        /// <summary>Host: the lobby's roster becomes an authoritative match whose every rules call is journaled to clients.</summary>
        public bool StartOnlineHost(MatchController m, int localSlot, ulong seed)
        {
            if (_busy) return false;
            Match = m;
            Match.WaitForRoomReady = true;
            Match.SetMatchLength(MatchSeconds); // host's choice; journaled to every client with MStart
            if (Settings.Current.SkipIntro || Autopilot) Match.IntroSeconds = 2f;
            if (Match.Journal == null) Match.Journal = new MatchJournal();
            HookMatch();
            GameServices.Match = Match; GameServices.LocalSlot = localSlot; GameServices.IsAuthority = true; GameServices.PracticeMode = false;
            if (!Match.Start(MatchMode.PrivateOnline, seed, out var err)) { Debug.LogError("[SHHH] online start refused: " + err); Match = null; GameServices.Match = null; return false; }
            _spectatorView = 0; State = AppState.Intro;
            MenuBuilder.ShowIntro(this, Match);
            Milestone?.Invoke("match_start");
            return true;
        }

        /// <summary>Client: an empty replica that the host's journal will Start and drive (never ticked locally).</summary>
        public void StartOnlineClient(MatchController replica, int localSlot)
        {
            Match = replica;
            HookMatch();
            Match.PhaseChanged += p =>
            {
                if (p == MatchPhase.Intro) { _spectatorView = 0; State = AppState.Intro; MenuBuilder.ShowIntro(this, Match); Milestone?.Invoke("match_start"); }
                if (p == MatchPhase.Cancelled) OnlineEnded("The host ended the match.");
            };
            GameServices.Match = Match; GameServices.LocalSlot = localSlot; GameServices.IsAuthority = false; GameServices.PracticeMode = false;
        }

        /// <summary>Session lost / host left / kicked: explain on a board, then back to the hall.</summary>
        public void OnlineEnded(string reason)
        {
            Debug.Log("[SHHH] online ended: " + reason);
            Milestone?.Invoke("online_ended");
            GameServices.IsAuthority = true;
            StartCoroutine(OnlineEndedFlow(reason));
        }

        IEnumerator OnlineEndedFlow(string reason)
        {
            while (_busy) yield return null;
            yield return GoFrontEnd();
            MenuBuilder.ShowMessage("Online game ended", reason);
        }

        void HookMatch()
        {
            Match.Log.Sink = line => Debug.Log("[SHHH] " + line);
            Match.RoundStarting += r => StartCoroutine(LoadRound(r));
            Match.SlotEliminated += OnSlotEliminated;
            Match.MatchFinished += sum => StartCoroutine(ShowWinner(sum));
            Match.RoundEnded += r => Milestone?.Invoke("round_end_" + (r.Config.Index + 1));
        }

        IEnumerator LoadRound(RoundController round)
        {
            while (_busy) yield return null;
            if (Match == null || round != Match.Round) yield break; // fast-forwarded past this room (reconnect)
            _busy = true;
            yield return SwapScene(RoomScenes[(int)round.Config.Room]);
            var go = new GameObject("Room_" + round.Room.Title);
            Room = go.AddComponent<RoomRuntime>();
            Room.AutopilotLocal = Autopilot || (AutopilotLocalSeat && GameServices.IsAuthority);
            Room.LocalHead = Rig != null ? Rig.Head : null;
            Room.Build(round.Room);
            Room.Bind(round, round.Seed);
            round.PenaltyApplied += OnPenalty;
            bool localActive = Match.Roster[GameServices.LocalSlot].IsActive;
            if (Rig != null)
            {
                if (localActive && !Autopilot) Rig.Spawn(Room.SpawnFor(GameServices.LocalSlot), 0f);
                else Rig.Spawn(Room.SpectatorView(_spectatorView), 35f);
            }
            AudioBank.I?.SetMusic(null);
            AudioBank.I?.SetMatchMusic(true);   // V8 adaptive stems (calm / tension / danger, final minute)
            yield return new WaitForSeconds(0.3f);
            Rig?.FadeTo(0f);
            if (!localActive) MenuBuilder.ShowSpectatorBoard(this, Room);
            State = AppState.InMatch;
            _busy = false;
            // Online: every peer reports its room as loaded; the host starts the countdown when all are in (or after a timeout).
            if (GameServices.Net != null && GameServices.Net.Active) GameServices.Net.RoomLoaded(round.Config.Index);
            else Match.NotifyRoomReady();
            AudioBank.I?.Play2D("countdown", 0.7f);
            Milestone?.Invoke("round_start_" + (round.Config.Index + 1));
        }

        /// <summary>Continuous match: Mom's proven-disturbance penalty - a short, funny "sent to bed" timeout; the match goes on.</summary>
        void OnPenalty(RoundController.PenaltyRecord pr)
        {
            Milestone?.Invoke("penalty_" + pr.Slot);
            string who = Match != null ? Match.Roster[pr.Slot].DisplayName : "?";
            var head = Rig != null && Rig.Head != null ? Rig.Head : Camera.main != null ? Camera.main.transform : null;
            var fwd = head != null ? Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized : Vector3.forward;
            var at = head != null ? head.position + fwd * 1.1f + Vector3.down * 0.15f : Vector3.up * 1.4f;
            if (pr.Slot == GameServices.LocalSlot && !Autopilot)
            {
                AudioBank.I?.Play2D("elim_you", 0.7f);
                Rig?.LeftHand?.ForceDropGently(); Rig?.RightHand?.ForceDropGently();
                // V12 (Quest run: "-20 PTS" with no idea why): the card says what Mom heard and who saw it
                string why = TaskHud.Why(pr.Evidence, pr.Reason);
                RoomFx.Say(at, (pr.Seconds > 0 ? $"Mom caught you!\n<size=70%>-{pr.Points:0} points · sit still for {pr.Seconds:0}s</size>" : $"Mom caught you! -{pr.Points:0}") + $"\n<size=55%>{why}</size>", Palette.Coral, Mathf.Max(4f, pr.Seconds * 0.5f), 0.9f);
            }
            else
            {
                AudioBank.I?.Play2D("elim_other", 0.5f);
                RoomFx.Say(at, $"Mom caught {who}! -{pr.Points:0}\n<size=55%>{TaskHud.Why(pr.Evidence, pr.Reason)}</size>", Palette.Coral, 3.5f, 0.8f);
            }
        }

        void OnSlotEliminated(int slot, EliminationReason reason, int roundIndex)
        {
            Milestone?.Invoke("eliminated_" + slot);
            if (slot != GameServices.LocalSlot || Autopilot) { AudioBank.I?.Play2D("elim_other", 0.6f); return; }
            AudioBank.I?.Play2D("elim_you", 0.9f);
            Rig?.LeftHand?.ForceDropGently(); Rig?.RightHand?.ForceDropGently();
            StartCoroutine(BecomeSpectator());
        }

        IEnumerator BecomeSpectator()
        {
            yield return new WaitForSeconds(3.5f);
            Rig?.FadeTo(1f); yield return new WaitForSeconds(0.5f);
            if (Room != null) Rig?.Spawn(Room.SpectatorView(_spectatorView), 35f);
            Rig?.FadeTo(0f);
            if (Room != null) MenuBuilder.ShowSpectatorBoard(this, Room);
        }

        public void NextSpectatorView() { _spectatorView++; if (Room != null) StartCoroutine(FadeTo(Room.SpectatorView(_spectatorView))); }

        IEnumerator FadeTo(Vector3 p) { Rig?.FadeTo(1f); yield return new WaitForSeconds(0.4f); Rig?.Spawn(p, 35f); Rig?.FadeTo(0f); }

        IEnumerator ShowWinner(MatchSummary sum)
        {
            State = AppState.Results;
            AudioBank.I?.SetMatchMusic(false);
            Milestone?.Invoke("winner_" + sum.WinnerSlot + (sum.WinnerIsBot ? "_bot" : "_human"));
            AudioBank.I?.Play2D(sum.WinnerSlot == GameServices.LocalSlot ? "win_you" : "win_other", 0.9f);
            var cam = Camera.main;
            if (cam != null)
            {
                var front = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 1.8f;
                RoomFx.Burst("sparks", front + Vector3.up * 1.2f, 0.5f, 4f);
                RoomFx.Burst("stars", front + Vector3.up * 0.6f, 0.45f, 4f);
            }
            yield return new WaitForSeconds(0.5f);
            MenuBuilder.ShowResults(this, Match);
        }

        public void Rematch()
        {
            if (_busy || Match == null) return;
            if (GameServices.Net != null && GameServices.Net.Active) { if (GameServices.Net.IsHost) GameServices.Net.RequestRematch(); return; }
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
            Match.PrepareRematch(seed);
            if (!Match.Start(MatchMode.Solo, seed, out var err)) { Debug.LogError(err); return; }
            _spectatorView = 0;
            State = AppState.Intro;
            MenuBuilder.ShowIntro(this, Match);
            Milestone?.Invoke("rematch");
        }

        /// <summary>Online host: same party, new seed - journaled, so every client follows into the new intro.</summary>
        public void RematchOnline(ulong seed)
        {
            if (_busy || Match == null || !GameServices.IsAuthority) return;
            Match.PrepareRematch(seed);
            foreach (var s in Match.Roster.Slots) if (s.IsHuman && !s.BotTakeover) Match.Roster.SetReady(s.PeerId, true);
            if (!Match.Start(MatchMode.PrivateOnline, seed, out var err)) { Debug.LogError("[SHHH] online rematch refused: " + err); return; }
            _spectatorView = 0; State = AppState.Intro;
            MenuBuilder.ShowIntro(this, Match);
            Milestone?.Invoke("rematch");
        }

        public void BackToMenu()
        {
            if (GameServices.Net != null && GameServices.Net.Active) GameServices.Net.Leave();
            GameServices.IsAuthority = true;
            if (!_busy) StartCoroutine(GoFrontEnd());
        }

        // ------------------------------------------------------------------ practice / tutorial
        public void StartPractice()
        {
            if (_busy) return;
            StartCoroutine(PracticeFlow());
        }

        IEnumerator PracticeFlow()
        {
            _busy = true;
            yield return SwapScene("Practice");
            var roster = new CompetitorRoster(); roster.SeatHuman(0, Settings.Current.PlayerName); roster.LockForMatch();
            for (int i = 1; i < 4; i++) roster.Eliminate(i, EliminationReason.None, -1);
            var round = new RoundController(RoundConfig.PracticeConfig(), roster, 7, new GameLog());
            round.Log.Sink = l => Debug.Log("[SHHH][practice] " + l);
            GameServices.Match = null; GameServices.PracticeRound = round; GameServices.PracticeMode = true; GameServices.LocalSlot = 0;
            var go = new GameObject("Room_Practice");
            Room = go.AddComponent<RoomRuntime>(); Room.LocalHead = Rig != null ? Rig.Head : null;
            Room.Build(round.Room);
            round.Begin();
            Room.Bind(round, 7);
            Rig?.Spawn(Room.SpawnFor(0), 0f);
            Rig?.FadeTo(0f);
            MenuBuilder.ShowTutorial(this, Room);
            Room.gameObject.AddComponent<PracticeTidy>().Room = Room;   // V12: knocked-off practice items come back
            State = AppState.Practice;
            _busy = false;
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            float dt = Time.deltaTime;
            if (Match != null && State != AppState.FrontEnd && !GameServices.IsReplica) Match.Tick(dt); // clients: the journal ticks it
            if (GameServices.PracticeRound != null && State == AppState.Practice) GameServices.PracticeRound.Tick(dt);
            if (Autopilot) Time.timeScale = AutopilotTimeScale;
        }

        void OnWristMenu()
        {
            if (State == AppState.FrontEnd) return;
            MenuBuilder.ShowPauseBoard(this);
        }

        public void QuitApp()
        {
            Settings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationPause(bool paused)
        {
            // Headset removed / system menu: keep the authoritative state consistent; hand input is already ignored.
            if (paused) { Rig?.LeftHand?.ForceDropGently(); Rig?.RightHand?.ForceDropGently(); Settings.Save(); }
        }

        void OnApplicationFocus(bool focus)
        {
            if (GameServices.IsReplica) GameServices.Net?.ReportTrackingLost(!focus);
            else if (Match != null && Match.Round != null) Match.Round.ReportTrackingLost(GameServices.LocalSlot, !focus);
        }
    }
}
