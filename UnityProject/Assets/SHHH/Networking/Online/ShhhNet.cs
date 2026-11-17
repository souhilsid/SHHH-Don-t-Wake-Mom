using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// V6 NET-G4: real private online play on Photon Fusion 2 (Host Mode, Photon Cloud, private 4-letter room codes).
    /// <list type="bullet">
    /// <item>Lobby: host seats humans as they join (max 4, the session itself refuses a fifth), bots fill the rest, everyone readies, host starts.</item>
    /// <item>Rules: the host runs the authoritative MatchController; its <see cref="MatchJournal"/> is streamed to every client as
    /// ordered reliable data and replayed into an identical replica that drives the unchanged game presentation.</item>
    /// <item>Physics: host-simulated; props/bots/pets replicate through <see cref="NetWorld"/>; client grabs/releases are intents
    /// (<see cref="NetLobby"/> RPCs) applied by <see cref="RemoteHold"/> on the host.</item>
    /// <item>Avatars: every human's head/hands replicate through <see cref="NetHuman"/> input.</item>
    /// <item>Disconnects: lobby leave frees the seat; mid-match a seat gets the Core grace period then a bot; reconnecting with the
    /// same name reclaims it and fast-forwards from the full journal. Host loss ends the match for everyone (no host migration).</item>
    /// </list>
    /// </summary>
    public sealed class ShhhNet : MonoBehaviour, INetworkRunnerCallbacks, INetBridge
    {
        public static ShhhNet I { get; private set; }
        public const string SessionPrefix = "SHHH-";

        NetworkRunner _runner;
        GameObject _sessionPrefab, _humanPrefab;
        readonly Dictionary<PlayerRef, NetHuman> _humans = new Dictionary<PlayerRef, NetHuman>();
        readonly List<NetSeat> _seats = new List<NetSeat>(4);
        RemoteHold _hold;
        string _status = "", _code = "", _region = "", _myName = "Player"; int _myCosmetic;
        bool _starting;

        // host
        CompetitorRoster _roster;
        readonly Dictionary<PlayerRef, int> _sendSeq = new Dictionary<PlayerRef, int>();
        readonly HashSet<PlayerRef> _needFullJournal = new HashSet<PlayerRef>();
        readonly Dictionary<int, HashSet<int>> _roomAcks = new Dictionary<int, HashSet<int>>();
        int _hostLoadedRound = -1; float _hostLoadedAt;
        public readonly List<string> ClientReports = new List<string>();

        // client
        MatchController _replica;
        readonly SortedDictionary<int, (byte[] data, bool reset)> _chunks = new SortedDictionary<int, (byte[], bool)>();
        int _nextSeq; byte[] _carry;
        public int Desyncs { get; private set; }
        public int Checksums { get; private set; }
        public long JournalBytes { get; private set; }

        /// <summary>Automated tests: overrides the tracked pose sent to the host (independent headless clients).</summary>
        public static Func<NetHumanInput?> InputOverride;
        public event Action<string> Note;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("ShhhNet"); DontDestroyOnLoad(go);
            go.AddComponent<ShhhNet>();
        }

        void Awake()
        {
            I = this;
            _hold = gameObject.AddComponent<RemoteHold>();
            _hold.HandPose = (slot, hand) => PoseOf(slot, hand);
            _sessionPrefab = Resources.Load<GameObject>("NetSession");
            _humanPrefab = Resources.Load<GameObject>("NetHuman");
            GameServices.Net = this;
            GameServices.RemoteLook = RemoteLookForSlot;
            AvatarService.LocalLookChanged += OnLocalLookSaved;
            GameServices.RemotePose = (slot, k) => slot == GameServices.LocalSlot ? null : PoseOf(slot, k);
            GameServices.HolderOf = HolderOf;
            GameServices.SendConfrontIntent = (kind, arg) => { if (!IsHost && NetLobby.I != null) NetLobby.I.RPC_Confront(kind, arg); };
            GameServices.SendRecipeIntent = op =>
            {
                if (IsHost || NetLobby.I == null) return false;
                // V12: only what a client's OWN hand moves locally comes as an intent (doors / drawers / START / house mechanisms);
                // everything physical (placing, scrubbing, pouring, rinsing) is detected by the host's simulation
                if (op.Verb != SHHH.Core.Verb.Open && op.Verb != SHHH.Core.Verb.Close && op.Verb != SHHH.Core.Verb.Press && op.Verb != SHHH.Core.Verb.Mech) return false;
                NetLobby.I.RPC_RecipeOp((int)op.Verb, op.Prop, op.Prop2, op.Fixture, op.Slot, op.Arg ?? ""); IntentsSent++; return true;
            };
            GameServices.IsRemoteHuman = slot =>
            {
                var m = GameServices.Match; if (!Active || m == null || slot < 0 || slot > 3) return false;
                var s = m.Roster[slot]; return s.IsHuman && !s.BotTakeover && slot != GameServices.LocalSlot;
            };
            if (_sessionPrefab == null || _humanPrefab == null) _status = "network prefabs missing (run net_game_setup)";
        }

        void Log(string s) { Debug.Log("[SHHH][net] " + s); GrabLog.Add("net: " + s); Note?.Invoke(s); }

        // V13 (owner, Quest c21: "me in Qatar and my friend in Algeria ... both ways ... popup game connection lost"). Each headset picked
        // its own best Photon region by ping (the room board showed "region uae" in Qatar; Algeria pings Europe best), and a room only
        // exists in the region it was made in. Everybody now uses ONE region. "eu" reaches the Gulf and North Africa well (and Europe).
        public const string OnlineRegion = "eu";
        /// <summary>Game version carried by the room; a player on another build gets a clear message instead of a dropped link.</summary>
        static string BuildTag => Application.version;

        static Fusion.Photon.Realtime.FusionAppSettings RegionSettings()
        {
            try
            {
                var app = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();
                app.FixedRegion = OnlineRegion; return app;
            }
            catch (Exception e) { Debug.LogWarning("[SHHH][net] region settings: " + e.Message); return null; }
        }

        /// <summary>A shutdown reason in words a player can act on.</summary>
        public static string Explain(ShutdownReason r) => r switch
        {
            ShutdownReason.GameNotFound => "no room with that code (check the code)",
            ShutdownReason.GameIsFull => "the room is full",
            ShutdownReason.GameClosed => "that room already started its match",
            ShutdownReason.IncompatibleConfiguration => "your friend has a different version of the game - both need the same update",
            ShutdownReason.PhotonCloudTimeout or ShutdownReason.ConnectionTimeout or ShutdownReason.OperationTimeout => "the internet connection timed out - check Wi-Fi",
            ShutdownReason.ConnectionRefused => "the host refused the connection",
            ShutdownReason.DisconnectedByPluginLogic or ShutdownReason.Ok => "the host closed the room",
            ShutdownReason.HostMigration => "the host left",
            _ => r.ToString(),
        };

        // ================================================================== INetBridge
        public bool Active => _runner != null && (_runner.IsRunning || _starting);
        public bool IsHost => _runner != null && _runner.IsServer;
        public bool InMatch => NetLobby.I != null && NetLobby.I.MatchRunning;
        public string RoomCode => _code;
        public string Status => _status;
        public string Region => _region;
        public bool LocalReady
        {
            get
            {
                var l = NetLobby.I; if (l == null || _runner == null) return false;
                int s = l.SeatOf(_runner.LocalPlayer); return s >= 0 && l.SeatReady[s];
            }
        }
        public bool CanStart => IsHost && _roster != null && !InMatch && _roster.AllHumansReady && _roster.HumanCount >= 1 && GameRoot.I != null && GameRoot.I.State == AppState.FrontEnd;

        public IReadOnlyList<NetSeat> Seats
        {
            get
            {
                _seats.Clear();
                var l = NetLobby.I; if (l == null) return _seats;
                for (int i = 0; i < 4; i++)
                {
                    int pid = l.SeatPlayer[i];
                    _seats.Add(new NetSeat
                    {
                        Human = pid >= 0, Name = l.SeatName[i].Value, Ready = l.SeatReady[i], Connected = l.SeatConnected[i], Cosmetic = l.SeatCosmetic[i],
                        Local = _runner != null && pid == _runner.LocalPlayer.PlayerId,
                    });
                }
                return _seats;
            }
        }

        async void Run(Task t) { try { await t; } catch (Exception e) { _status = "Network error: " + e.Message; Debug.LogException(e); Log("session task failed: " + e); } }

        public void Create(string playerName, int cosmetic)
        {
            var code = new System.Text.StringBuilder();
            var rng = new System.Random(Environment.TickCount ^ GetHashCode());
            for (int i = 0; i < 4; i++) code.Append(MenuBuilder.CodeAlphabet[rng.Next(MenuBuilder.CodeAlphabet.Length)]);
            Run(StartSession(GameMode.Host, code.ToString(), playerName, cosmetic));
        }

        public void CreateWithCode(string code, string playerName, int cosmetic) => Run(StartSession(GameMode.Host, code, playerName, cosmetic));

        public void Join(string code, string playerName, int cosmetic) => Run(StartSession(GameMode.Client, code.ToUpperInvariant(), playerName, cosmetic));

        public void SetReady(bool ready)
        {
            if (!Active) return;
            if (IsHost) HostOnReady(_runner.LocalPlayer, ready);
            else NetLobby.I?.RPC_Ready(ready);
        }

        public void StartMatch()
        {
            if (!CanStart) { Log("start refused: not everyone ready"); return; }
            ulong seed = (ulong)DateTime.UtcNow.Ticks;
            var m = new MatchController(_roster) { Journal = new MatchJournal() };
            int localSlot = _roster.FindByPeer(_runner.LocalPlayer.PlayerId)?.Index ?? 0;
            _sendSeq.Clear(); _needFullJournal.Clear(); _roomAcks.Clear(); _hostLoadedRound = -1; _bseq = 0; _flushT = 0f; _synced.Clear();
            foreach (var p in _runner.ActivePlayers) if (p != _runner.LocalPlayer) _synced.Add(p); // they receive chunk 0 onwards
            if (!GameRoot.I.StartOnlineHost(m, localSlot, seed)) return;
            NetLobby.I.MatchRunning = true; NetLobby.I.MatchSerial++;
            _status = "Match running";
            Log($"match started seed={seed} humans={_roster.HumanCount} bots={_roster.BotCount}");
        }

        public void RequestRematch()
        {
            if (!IsHost || GameServices.Match == null) return;
            foreach (var s in _roster.Slots) if (s.IsHuman && !s.BotTakeover) _roster.SetReady(s.PeerId, true);
            GameRoot.I.RematchOnline((ulong)DateTime.UtcNow.Ticks);
        }

        public void Leave()
        {
            if (_runner == null) return;
            Log("leaving session");
            var r = _runner; _runner = null;
            ResetState();
            _ = r.Shutdown();
        }

        public void RequestGrab(int propId, int hand) { if (!IsHost) NetLobby.I?.RPC_Grab(propId, hand); }
        public void RequestRelease(int propId, Vector3 velocity, bool thrown, int hand) { if (!IsHost) NetLobby.I?.RPC_Release(propId, velocity, thrown, hand); }
        public void ReportActivity() { if (!IsHost) NetLobby.I?.RPC_Activity(); }
        public void ReportTrackingLost(bool lost) { if (!IsHost) NetLobby.I?.RPC_Tracking(lost); }
        public int HolderOf(int propId) => IsHost ? (GameServices.Round?.HolderOf(propId) ?? -1) : (NetWorld.I != null ? NetWorld.I.HolderOf(propId) : -1);

        public void RoomLoaded(int roundIndex)
        {
            if (IsHost) { _hostLoadedRound = roundIndex; _hostLoadedAt = Time.realtimeSinceStartup; }
            else NetLobby.I?.RPC_RoomLoaded(roundIndex);
        }

        // ================================================================== voice (Photon Voice 2.64 / Fusion)
        Recorder _rec; FusionVoiceClient _voice;
        public int VoiceFramesDecoded { get; private set; }
        public int RemoteVoices { get; private set; }
        public bool MicOn => _rec != null && _rec.TransmitEnabled;
        public string VoiceStatus => _voice == null ? "voice off" : $"voice {_voice.ClientState}{(MicOn ? "" : " (mic muted)")}{(_micDenied ? " - microphone permission denied" : "")}";
        bool _micDenied;

        void SetupVoice(GameObject runnerGo)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#endif
                _rec = runnerGo.AddComponent<Recorder>();
                // Headless test players have no microphone: they transmit a generated tone so voice is still proven end-to-end.
                _rec.SourceType = Application.isBatchMode ? Recorder.InputSourceType.Factory : Recorder.InputSourceType.Microphone;
                _rec.TransmitEnabled = Settings.Current.VoiceChatOn;
                _voice = runnerGo.AddComponent<FusionVoiceClient>();
                _voice.PrimaryRecorder = _rec;
                _voice.RemoteVoiceAdded += link => { RemoteVoices++; link.FloatFrameDecoded += f => VoiceFramesDecoded++; Log("voice: remote voice added"); };
            }
            catch (Exception e) { Log("voice setup failed (game continues without voice): " + e.Message); _rec = null; _voice = null; }
        }

        public void SetMic(bool on)
        {
            Settings.Current.VoiceChatOn = on; Settings.Save();
#if UNITY_ANDROID && !UNITY_EDITOR
            _micDenied = on && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
            if (_micDenied) UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#endif
            if (_rec != null) _rec.TransmitEnabled = on;
        }

        // ================================================================== session start
        async Task StartSession(GameMode mode, string code, string playerName, int cosmetic)
        {
            if (Active) Leave();
            if (_sessionPrefab == null || _humanPrefab == null) { _status = "Online files missing in this build"; return; }
            _starting = true; _code = code; _myName = string.IsNullOrEmpty(playerName) ? "Player" : playerName; _myCosmetic = cosmetic;
            _status = mode == GameMode.Host ? "Creating room..." : $"Joining {code}...";
            var go = new GameObject("FusionRunner"); DontDestroyOnLoad(go);
            _runner = go.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            var sm = go.AddComponent<NetworkSceneManagerDefault>();
            SetupVoice(go);
            if (_voice != null) _runner.AddCallbacks(_voice); // net_multi run 3: not auto-registered -> voice stayed "PeerCreated"
            StartGameResult res;
            try
            {
                res = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = mode, SessionName = SessionPrefix + code, PlayerCount = CompetitorRoster.SlotCount,
                    IsVisible = false, IsOpen = true, SceneManager = sm,
                    CustomPhotonAppSettings = RegionSettings(),
                    SessionProperties = mode == GameMode.Host ? new Dictionary<string, SessionProperty> { { "v", (SessionProperty)BuildTag } } : null,
                });
            }
            catch (Exception e) { _starting = false; _status = "Network error: " + e.Message; Log(_status); return; }
            _starting = false;
            if (!res.Ok)
            {
                _status = res.ShutdownReason switch
                {
                    ShutdownReason.GameNotFound => $"No room with code {code}",
                    ShutdownReason.GameIsFull => $"Room {code} is full (4 siblings max)",
                    ShutdownReason.GameClosed => $"Room {code} already started its match",
                    ShutdownReason.PhotonCloudTimeout or ShutdownReason.ConnectionTimeout => "Photon is unreachable - check Wi-Fi",
                    _ => "Could not connect: " + Explain(res.ShutdownReason),
                };
                Log($"start failed: {res.ShutdownReason} {res.ErrorMessage}");
                _runner = null;
                return;
            }
            _region = _runner.SessionInfo != null ? _runner.SessionInfo.Region : "";
            Log($"{mode} in session {SessionPrefix + code} region={_region} local={_runner.LocalPlayer} version={BuildTag}");
            if (mode != GameMode.Host && _runner.SessionInfo != null && _runner.SessionInfo.Properties != null
                && _runner.SessionInfo.Properties.TryGetValue("v", out var hv) && hv.IsString && (string)hv.PropertyValue != BuildTag)
            {
                string theirs = (string)hv.PropertyValue;
                Log($"version mismatch: host {theirs}, me {BuildTag}");
                Leave();
                _status = $"Different game versions: the room is {theirs}, you have {BuildTag}. Both need the same update.";
                return;
            }
            if (mode == GameMode.Host)
            {
                _roster = new CompetitorRoster();
                _roster.SeatHuman(_runner.LocalPlayer.PlayerId, _myName, _myCosmetic);
                var so = _runner.Spawn(_sessionPrefab, Vector3.zero, Quaternion.identity);
                var ho = _runner.Spawn(_humanPrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
                Log($"spawned session={(so != null ? so.Id.ToString() : "NULL")} human={(ho != null ? ho.Id.ToString() : "NULL")}");
                _status = "Room open - tell your friends the code";
            }
            else _status = "Joined - press READY";
        }

        public void OnLobbySpawned(NetLobby l)
        {
            if (IsHost) { HostSyncLobby(); HostSetOwnLook(); }
            else { l.RPC_Hello(_myName, _myCosmetic); SendMyLook(); }
        }

        // ================================================================== Genies looks (definition strings) per player
        const int LookChunk = 180;
        readonly Dictionary<int, string> _looks = new Dictionary<int, string>();
        readonly Dictionary<int, string[]> _lookParts = new Dictionary<int, string[]>();
        public int LooksKnown => _looks.Count;
        public string LookOfPlayer(int playerId) => _looks.TryGetValue(playerId, out var d) ? d : null;

        void OnLocalLookSaved() { if (_runner == null || NetLobby.I == null) return; if (IsHost) HostSetOwnLook(); else SendMyLook(); }

        void SendMyLook()
        {
            var l = NetLobby.I; var d = Settings.Current.GeniesDefinition; if (l == null || string.IsNullOrEmpty(d)) return;
            int parts = (d.Length + LookChunk - 1) / LookChunk;
            for (int i = 0; i < parts; i++) l.RPC_LookIn(i, parts, d.Substring(i * LookChunk, Math.Min(LookChunk, d.Length - i * LookChunk)));
            Log($"look sent ({d.Length} chars, {parts} chunks)");
        }

        void HostSetOwnLook()
        {
            var d = Settings.Current.GeniesDefinition; if (!IsHost || string.IsNullOrEmpty(d)) return;
            _looks[_runner.LocalPlayer.PlayerId] = d; HostBroadcastLook(_runner.LocalPlayer.PlayerId);
        }

        void HostBroadcastLook(int pid)
        {
            var l = NetLobby.I; if (l == null || !IsHost || !_looks.TryGetValue(pid, out var d)) return;
            int parts = (d.Length + LookChunk - 1) / LookChunk;
            for (int i = 0; i < parts; i++) l.RPC_LookOut(pid, i, parts, d.Substring(i * LookChunk, Math.Min(LookChunk, d.Length - i * LookChunk)));
        }

        bool Assemble(int pid, int part, int parts, string chunk)
        {
            if (parts <= 0 || parts > 64 || part < 0 || part >= parts || chunk == null) return false;
            if (!_lookParts.TryGetValue(pid, out var arr) || arr.Length != parts) _lookParts[pid] = arr = new string[parts];
            arr[part] = chunk;
            foreach (var c in arr) if (c == null) return false;
            var d = string.Concat(arr); _lookParts.Remove(pid);
            if (_looks.TryGetValue(pid, out var old) && old == d) return false;
            _looks[pid] = d; return true;
        }

        public void HostOnLookChunk(int pid, int part, int parts, string chunk)
        {
            if (!IsHost || !Assemble(pid, part, parts, chunk)) return;
            Log($"look received from player {pid} ({_looks[pid].Length} chars)");
            HostBroadcastLook(pid); AvatarService.NotifySeatLooksChanged();
        }

        public void ClientOnLookChunk(int pid, int part, int parts, string chunk)
        {
            if (IsHost || !Assemble(pid, part, parts, chunk)) return;
            Log($"look of player {pid} arrived ({_looks[pid].Length} chars)");
            AvatarService.NotifySeatLooksChanged();
        }

        string RemoteLookForSlot(int slot)
        {
            var l = NetLobby.I; if (l == null || slot < 0 || slot > 3) return null;
            int pid = l.SeatPlayer[slot]; return pid >= 0 ? LookOfPlayer(pid) : null;
        }

        void ResetState()
        {
            _humans.Clear(); _roster = null; _replica = null; _chunks.Clear(); _nextSeq = 0; _carry = null; _sendSeq.Clear(); _needFullJournal.Clear();
            _roomAcks.Clear(); _hostLoadedRound = -1; _hold.Clear(); _code = ""; _region = ""; _status = "";
            _bseq = 0; _synced.Clear(); _fullJournal = null; _flushT = 0f; _looks.Clear(); _lookParts.Clear();
        }

        // ================================================================== host: lobby & intents
        void HostSyncLobby()
        {
            var l = NetLobby.I; if (l == null || _roster == null || !IsHost) return;
            for (int i = 0; i < 4; i++)
            {
                var s = _roster[i];
                bool human = s.IsHuman && !s.BotTakeover;
                l.SeatPlayer.Set(i, human ? s.PeerId : -1);
                l.SeatName.Set(i, (s.DisplayName ?? "").Length > 30 ? s.DisplayName.Substring(0, 30) : (s.DisplayName ?? ""));
                l.SeatReady.Set(i, human && s.Ready);
                l.SeatCosmetic.Set(i, s.CosmeticId);
                l.SeatConnected.Set(i, human && s.DisconnectedAt < 0f);
            }
        }

        public void HostOnHello(PlayerRef p, string name, int cosmetic)
        {
            if (!IsHost || _roster == null) return;
            name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
            var m = GameServices.Match;
            if (!InMatch)
            {
                var seat = _roster.FindByPeer(p.PlayerId);
                if (seat == null) { int idx = _roster.SeatHuman(p.PlayerId, name, cosmetic); if (idx < 0) { Log($"seat refused for {p}"); return; } }
                else { seat.DisplayName = name; seat.CosmeticId = cosmetic; }
                Log($"lobby: {name} seated ({_roster.HumanCount} humans)");
            }
            else if (m != null)
            {
                // Reconnect: same name reclaims a disconnected / bot-taken seat; then fast-forward from the full journal.
                int slot = -1;
                for (int i = 0; i < 4; i++) { var s = m.Roster[i]; if (s.DisplayName == name && (s.DisconnectedAt >= 0 || s.BotTakeover)) { slot = i; break; } }
                if (slot >= 0 && m.HumanReconnected(p.PlayerId, slot, name, m.Round != null ? m.Round.Time : 0f))
                {
                    m.Roster[slot].PeerId = p.PlayerId;
                    Log($"reconnect: {name} back in seat {slot + 1}");
                }
                else Log($"{name} joined mid-match as a spectator");
                _needFullJournal.Add(p);
            }
            HostSyncLobby();
            foreach (var pid in new List<int>(_looks.Keys)) HostBroadcastLook(pid);
        }

        public void HostOnReady(PlayerRef p, bool ready)
        {
            if (!IsHost || _roster == null || InMatch) return;
            _roster.SetReady(p.PlayerId, ready); HostSyncLobby();
        }

        int SlotOf(PlayerRef p) { var m = GameServices.Match; var s = m != null ? m.Roster.FindByPeer(p.PlayerId) : _roster?.FindByPeer(p.PlayerId); return s != null ? s.Index : -1; }

        public void HostOnGrab(PlayerRef p, int propId, int hand)
        {
            var round = GameServices.Round; var room = GameServices.Room; int slot = SlotOf(p);
            if (!IsHost || round == null || room == null || slot < 0) return;
            var prop = room.Prop(propId); if (prop == null || !prop.Spec.Grabbable) return;
            int holder = round.HolderOf(propId);
            if (holder >= 0 && holder != slot) return;               // contention: first holder keeps it (host decides)
            if (_hold.IsHeldBy(propId, slot)) return;
            if (!round.Grab(slot, propId)) { Log($"grab #{propId} by seat {slot + 1} refused by rules ({round.Phase})"); return; }  // same rules as a local hand
            _hold.Begin(prop, slot, hand);
            var hp = PoseOf(slot, hand);
            Log($"grab #{propId} {prop.Spec.Kind} -> seat {slot + 1}; item {prop.transform.position:F2}, hand {(hp.HasValue ? hp.Value.position.ToString("F2") : "untracked")}");
            round.ReportActivity(slot);
        }

        public void HostOnRelease(PlayerRef p, int propId, Vector3 v, bool thrown, int hand)
        {
            int slot = SlotOf(p); if (!IsHost || slot < 0) return;
            var prop = GameServices.Room?.Prop(propId);
            bool held = _hold.End(propId, slot, v, thrown);
            if (!held) GameServices.Round?.Release(slot, propId, v.magnitude, thrown);
            Log($"release #{propId} by seat {slot + 1} (was held={held}) at {(prop != null ? prop.transform.position.ToString("F2") : "?")}");
        }

        public int IntentsSent { get; private set; }
        public int MechIntentsApplied { get; private set; }
        public int ClientSlamsVoiced { get; private set; }

        /// <summary>V12 client intent with the whole op (doors / drawers / START / house mechanisms). The actor is the RPC source's seat;
        /// every rule runs in the host's round. A client's slam ("slam" on Close, "lvl:slam" on Mech) becomes the host's noise event at the
        /// part, so Mom hears doors slammed on any peer.</summary>
        public void HostOnRecipeOp(PlayerRef p, int verb, int prop, int prop2, int fixture, int slot, string arg)
        {
            int seat = SlotOf(p); var round = GameServices.Round;
            if (!IsHost || seat < 0 || round == null) return;
            var v = (SHHH.Core.Verb)verb;
            if (v != SHHH.Core.Verb.Open && v != SHHH.Core.Verb.Close && v != SHHH.Core.Verb.Press && v != SHHH.Core.Verb.Mech) { Log($"recipe intent {v} from seat {seat + 1} ignored (host-detected verb)"); return; }
            if (v != SHHH.Core.Verb.Mech && round.Recipes == null) return;
            var err = round.RecipeAction(new SHHH.Core.ROp { Actor = seat, Verb = v, Fixture = fixture, Prop = prop, Prop2 = prop2, Slot = slot, Arg = string.IsNullOrEmpty(arg) ? null : arg });
            if (v == SHHH.Core.Verb.Mech && err == null) MechIntentsApplied++;
            // a remote player is using a bin lid: the host's bots leave that bin to them
            if (v == SHHH.Core.Verb.Mech) foreach (var f in MechFurniture.All) if (f != null && f.Lid != null && f.Lid.HouseId == slot) { f.ClaimBinFor(seat); break; }
            bool slam = (arg ?? "").EndsWith("slam");
            if (slam && (err == null || err == "no change"))
            {
                var part = v == SHHH.Core.Verb.Mech ? MechPart.ByHouseId(slot) : MechPart.ByFixture(fixture);
                if (part != null && part.HostSlamFromClient(seat)) ClientSlamsVoiced++;
            }
            Log($"recipe op {v} f{fixture} s{slot} {arg} by seat {seat + 1}: {err ?? "ok"}");
            round.ReportActivity(seat);
        }

        /// <summary>Client poke on a recipe lid/control. Only poke verbs are accepted; every rule check runs in the host's board.</summary>
        public void HostOnRecipe(PlayerRef p, int verb, int fixture, string arg)
        {
            int slot = SlotOf(p); var round = GameServices.Round;
            if (!IsHost || slot < 0 || round == null || round.Recipes == null) return;
            var v = (SHHH.Core.Verb)verb;
            if (v != SHHH.Core.Verb.Open && v != SHHH.Core.Verb.Close && v != SHHH.Core.Verb.Press) { Log($"recipe intent {v} from seat {slot + 1} ignored (host-detected verb)"); return; }
            var err = round.RecipeAction(new SHHH.Core.ROp { Actor = slot, Verb = v, Fixture = fixture, Prop = -1, Prop2 = -1, Slot = -1, Arg = string.IsNullOrEmpty(arg) ? null : arg });
            Log($"recipe {v} f{fixture} {arg} by seat {slot + 1}: {err ?? "ok"}");
            round.ReportActivity(slot);
        }

        /// <summary>Client confrontation input; the seat comes from the RPC source, the rules validate (own memories only, one point, open window).</summary>
        public void HostOnConfront(PlayerRef p, int kind, int arg)
        {
            int slot = SlotOf(p); var round = GameServices.Round;
            if (!IsHost || slot < 0 || round == null) return;
            bool ok = kind == 0 ? round.PresentCard(slot, arg) : kind == 1 && round.Accuse(slot, arg);
            if (kind != 0) round.PassInterrogation(slot);
            Log($"confront kind {kind} arg {arg} by seat {slot + 1}: {(ok || kind == 2 ? "ok" : "refused")}");
        }

        public void HostOnActivity(PlayerRef p) { int s = SlotOf(p); if (IsHost && s >= 0) GameServices.Round?.ReportActivity(s); }
        public void HostOnTracking(PlayerRef p, bool lost) { int s = SlotOf(p); if (IsHost && s >= 0) GameServices.Round?.ReportTrackingLost(s, lost); }
        public void HostOnRoomLoaded(PlayerRef p, int roundIndex) { if (!IsHost) return; if (!_roomAcks.TryGetValue(roundIndex, out var set)) _roomAcks[roundIndex] = set = new HashSet<int>(); set.Add(p.PlayerId); }
        public void HostOnReport(PlayerRef p, string line) { ClientReports.Add($"{p}: {line}"); Log($"client {p}: {line}"); }

        // ================================================================== avatars
        public void OnHumanSpawned(NetHuman h) { _humans[h.Player] = h; }
        public void OnHumanDespawned(NetHuman h) { if (_humans.TryGetValue(h.Player, out var x) && x == h) _humans.Remove(h.Player); }

        NetHuman HumanForSlot(int slot)
        {
            var l = NetLobby.I; if (l == null || slot < 0 || slot > 3) return null;
            int pid = l.SeatPlayer[slot]; if (pid < 0) return null;
            foreach (var kv in _humans) if (kv.Key.PlayerId == pid) return kv.Value;
            return null;
        }

        Pose? PoseOf(int slot, int k)
        {
            var h = HumanForSlot(slot); if (h == null || h.Object == null || !h.Object.IsValid) return null;
            switch (k)
            {
                case 0: return new Pose(h.Head, h.HeadRot);
                case 1: return h.LTracked ? new Pose(h.L, h.LRot) : (Pose?)null;
                default: return h.RTracked ? new Pose(h.R, h.RRot) : (Pose?)null;
            }
        }

        // ================================================================== per-frame
        void Update()
        {
            if (_runner == null || !_runner.IsRunning) return;
            if (IsHost) HostUpdate(); else ClientUpdate();
        }

        public const int ChunkBytes = 400;     // stays well inside Fusion's RPC payload limit
        public const float FlushInterval = 0.1f;
        int _bseq; float _flushT;
        readonly HashSet<PlayerRef> _synced = new HashSet<PlayerRef>();

        void HostUpdate()
        {
            var m = GameServices.Match;
            if (m == null || m.Journal == null || !InMatch || NetLobby.I == null) return;
            // Newcomers / reconnects: one bulk reliable transfer of everything so far, tagged with the broadcast seq it precedes.
            foreach (var p in _runner.ActivePlayers)
            {
                if (p == _runner.LocalPlayer) continue;
                if (_synced.Contains(p) && !_needFullJournal.Contains(p)) continue;
                FlushJournal(m, true);
                _runner.SendReliableDataToPlayer(p, ReliableKey.FromInts(0x5348, _bseq, 1, 0), m.Journal.ToArray());
                _synced.Add(p); _needFullJournal.Remove(p);
                Log($"full journal ({m.Journal.Length} B) -> {p}, continues at chunk {_bseq}");
            }
            _flushT += Time.unscaledDeltaTime;
            if (_flushT >= FlushInterval) { _flushT = 0f; FlushJournal(m, false); }
            // Countdown starts when every connected human has loaded the room (or after 10 s so nobody can stall it).
            if (m.Phase == MatchPhase.LoadingRoom && m.Round != null && _hostLoadedRound == m.Round.Config.Index)
            {
                bool all = true;
                _roomAcks.TryGetValue(_hostLoadedRound, out var set);
                foreach (var p in _runner.ActivePlayers)
                {
                    if (p == _runner.LocalPlayer) continue;
                    var s = m.Roster.FindByPeer(p.PlayerId);
                    if (s != null && s.IsHuman && (set == null || !set.Contains(p.PlayerId))) { all = false; break; }
                }
                if (all || Time.realtimeSinceStartup - _hostLoadedAt > 10f) { Log($"room {_hostLoadedRound + 1} ready ({(all ? "all loaded" : "timeout")})"); _hostLoadedRound = -2; m.NotifyRoomReady(); }
            }
        }

        /// <summary>Broadcast pending journal bytes as ordered chunks (skipped bytes are already inside any full transfer).</summary>
        void FlushJournal(MatchController m, bool force)
        {
            var pending = m.Journal.TakePending(); if (pending == null) return;
            JournalBytes += pending.Length;
            for (int i = 0; i < pending.Length; i += ChunkBytes)
            {
                int n = Math.Min(ChunkBytes, pending.Length - i);
                var c = new byte[n]; Array.Copy(pending, i, c, 0, n);
                NetLobby.I.RPC_Journal(_bseq++, c);
            }
        }

        public void ClientOnJournal(int seq, byte[] data)
        {
            if (IsHost || data == null) return;
            if (seq < _nextSeq && _replica != null) return; // already contained in a full transfer
            _chunks[seq] = (data, false);
        }

        byte[] _fullJournal; int _fullBase;

        void ClientUpdate()
        {
            if (_fullJournal != null)
            {
                var full = _fullJournal; _fullJournal = null;
                _nextSeq = _fullBase; _replica = null;
                ApplyChunk(full, true);
            }
            while (_chunks.TryGetValue(_nextSeq, out var c))
            {
                _chunks.Remove(_nextSeq); _nextSeq++;
                ApplyChunk(c.data, false);
            }
        }

        void ApplyChunk(byte[] data, bool reset)
        {
            var c = (data: data, reset: reset);
            {
                if (c.reset || _replica == null)
                {
                    int slot = NetLobby.I != null ? NetLobby.I.SeatOf(_runner.LocalPlayer) : -1;
                    _replica = new MatchController(); _carry = null;
                    GameRoot.I.StartOnlineClient(_replica, Mathf.Max(0, slot));
                    Log($"replica created (seat {slot + 1}{(c.reset ? ", full journal" : "")})");
                }
                JournalBytes += c.data.Length;
                try
                {
                    MatchJournal.Apply(_replica, c.data, ref _carry, sum =>
                    {
                        Checksums++;
                        if (sum != MatchJournal.Checksum(_replica)) { Desyncs++; Log($"DESYNC #{Desyncs} at round {_replica.RoundIndex + 1} phase {_replica.Phase}"); }
                    });
                }
                catch (Exception e) { Log("journal apply failed: " + e); Desyncs++; }
            }
        }

        // ================================================================== Fusion callbacks
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Log($"player joined {player}");
            if (!runner.IsServer || player == runner.LocalPlayer) return;
            runner.Spawn(_humanPrefab, Vector3.zero, Quaternion.identity, player);
            if (InMatch) _needFullJournal.Add(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Log($"player left {player}");
            if (!runner.IsServer) return;
            if (_humans.TryGetValue(player, out var h) && h != null && h.Object != null) runner.Despawn(h.Object);
            _humans.Remove(player); _sendSeq.Remove(player); _synced.Remove(player);
            var m = GameServices.Match;
            if (InMatch && m != null)
            {
                int slot = SlotOf(player); if (slot >= 0) _hold.DropAll(slot);
                m.HumanDisconnected(player.PlayerId, m.Round != null ? m.Round.Time : 0f);
            }
            else _roster?.UnseatHumanPreMatch(player.PlayerId);
            HostSyncLobby();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var o = InputOverride?.Invoke();
            if (o.HasValue) { input.Set(o.Value); return; }
            var rig = GameRoot.I != null ? GameRoot.I.Rig : null;
            if (rig == null || rig.Head == null) return;
            var i = new NetHumanInput { Head = rig.Head.position, HeadRot = rig.Head.rotation };
            if (rig.Left != null && rig.Left.Tracked) { i.L = rig.Left.PinchPoint; i.LRot = rig.Left.HandRotation; i.LTracked = true; }
            if (rig.Right != null && rig.Right.Tracked) { i.R = rig.Right.PinchPoint; i.RRot = rig.Right.HandRotation; i.RTracked = true; }
            input.Set(i);
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            key.GetInts(out int tag, out int baseSeq, out int reset, out _);
            if (tag != 0x5348 || reset != 1) return;
            // Full journal: rebuild the replica from scratch, then continue with broadcast chunks from baseSeq.
            var stale = new List<int>(); foreach (var k in _chunks.Keys) if (k < baseSeq) stale.Add(k);
            foreach (var k in stale) _chunks.Remove(k);
            _fullJournal = data.ToArray(); _fullBase = baseSeq;
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            Log("shutdown: " + reason);
            bool wasInMatch = GameServices.Match != null && (GameServices.IsReplica || GameRoot.I.State != AppState.FrontEnd);
            if (_runner == runner || _runner == null)
            {
                bool expected = _runner == null; // we called Leave()
                _runner = null; ResetState();
                if (!expected)
                {
                    _status = reason == ShutdownReason.DisconnectedByPluginLogic || reason == ShutdownReason.Ok ? "The host closed the room" : "Connection lost: " + Explain(reason);
                    if (wasInMatch || GameRoot.I.State == AppState.FrontEnd) GameRoot.I.OnlineEnded(_status);
                }
            }
            if (runner != null) Destroy(runner.gameObject);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Log("disconnected from host: " + reason); _status = "Lost the host (" + reason + ")"; }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Log("connect failed: " + reason); _status = "Connect failed: " + reason; }
        public void OnConnectedToServer(NetworkRunner runner) { Log("connected to host"); }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { if (_roster != null && _roster.HumanCount >= CompetitorRoster.SlotCount && !InMatch) request.Refuse(); else request.Accept(); }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
