using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.Net
{
    /// <summary>
    /// Photon Fusion 2 smoke test (Host Mode, private room code) with TWO peers that connect through the real Photon
    /// Cloud using the project's App ID: a Host and a Client. Proves: both join, each sees the other's moving rig,
    /// the client grabs a shared host-authoritative prop and it follows the client's hand, the host switches scenes
    /// and both peers stay in sync afterwards. Results -> Automation/net_smoke_result.json.
    /// Mode "multipeer": both peers in one Editor process (Fusion multi-peer). Mode "host"/"client": one peer per process.
    /// </summary>
    public sealed class NetSmokeHarness : MonoBehaviour
    {
        public static string Mode = "multipeer";
        public static string RoomCode;
        public static string ResultPath;
        public static int SceneA = -1, SceneB = -1;
        public static GameObject PlayerPrefab, PropPrefab;

        /// <summary>Started by the editor command net_smoke (flag file) - never in player builds of the game.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Application.isEditor && Environment.GetCommandLineArgs().Length == 0) return;
            string flag = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation", "netsmoke.flag"));
            string mode = null;
            foreach (var a in Environment.GetCommandLineArgs()) if (a.StartsWith("-shhhNet=")) mode = a.Substring(9);
            if (mode == null && Application.isEditor && File.Exists(flag)) mode = File.ReadAllText(flag).Trim();
            if (string.IsNullOrEmpty(mode)) return;
            Mode = mode;
            ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation", "net_smoke_result.json"));
            PlayerPrefab = Resources.Load<GameObject>("NetRigPlayer"); PropPrefab = Resources.Load<GameObject>("NetSharedProp");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var n = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                if (n == "NetSmokeA") SceneA = i; else if (n == "NetSmokeB") SceneB = i;
            }
            var go = new GameObject("NetSmokeHarness"); DontDestroyOnLoad(go); go.AddComponent<NetSmokeHarness>();
        }

        NetPeer _host, _client;
        readonly List<string> _log = new List<string>();
        readonly Dictionary<string, string> _checks = new Dictionary<string, string>();
        float _t0; bool _done;

        void Log(string s) { var line = $"[{Time.realtimeSinceStartup - _t0:0.00}] {s}"; _log.Add(line); Debug.Log("[NetSmoke] " + line); }
        void Check(string id, bool ok, string detail) { _checks[id] = (ok ? "PASS " : "FAIL ") + detail; Log($"{id}: {(ok ? "PASS" : "FAIL")} {detail}"); }

        async void Start()
        {
            _t0 = Time.realtimeSinceStartup;
            if (string.IsNullOrEmpty(RoomCode)) RoomCode = "SHHH-" + UnityEngine.Random.Range(10000, 99999);
            Log($"mode={Mode} room={RoomCode} sceneA={SceneA} sceneB={SceneB}");
            try
            {
                if (Mode == "multipeer" || Mode == "host") _host = await StartPeer(GameMode.Host, "Host");
                if (Mode == "multipeer" || Mode == "client") _client = await StartPeer(GameMode.Client, "Client");
                if (Mode == "multipeer") await RunScenario();
            }
            catch (Exception e) { Check("exception", false, e.ToString()); }
            if (Mode == "multipeer") Finish();
        }

        async Task<NetPeer> StartPeer(GameMode mode, string name)
        {
            var go = new GameObject("Runner_" + name);
            DontDestroyOnLoad(go); // a host scene switch must never destroy a co-located peer's runner
            var peer = go.AddComponent<NetPeer>();
            peer.Harness = this; peer.Name = name;
            var runner = go.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
            peer.Runner = runner;
            runner.AddCallbacks(peer);
            var sm = go.AddComponent<NetworkSceneManagerDefault>();
            var scene = new NetworkSceneInfo();
            if (SceneA >= 0) scene.AddSceneRef(SceneRef.FromIndex(SceneA), LoadSceneMode.Additive);
            var res = await runner.StartGame(new StartGameArgs
            {
                GameMode = mode, SessionName = RoomCode, PlayerCount = 4, IsVisible = false, IsOpen = true,
                Scene = scene, SceneManager = sm,
            });
            Check($"start_{name}", res.Ok, res.Ok ? $"{mode} joined '{RoomCode}' region={runner.SessionInfo?.Region}" : $"{res.ShutdownReason} {res.ErrorMessage}");
            if (!res.Ok) throw new Exception("start failed " + name);
            return peer;
        }

        async Task Wait(Func<bool> cond, float seconds, string what)
        {
            float t = 0;
            while (!cond() && t < seconds) { await Task.Delay(100); t += 0.1f; }
            if (!cond()) throw new Exception("timeout waiting: " + what);
        }

        async Task RunScenario()
        {
            var cr = _client.Runner; var hr = _host.Runner;
            // 1) both peers see two rigs
            await Wait(() => CountRigs(cr) >= 2 && CountRigs(hr) >= 2, 20f, "2 rigs on both peers");
            Check("both_see_two_players", true, $"client sees {CountRigs(cr)} rigs, host sees {CountRigs(hr)} rigs; players={hr.ActivePlayers.ToString()}");
            // 2) client sees the host's rig MOVING (remote head pose sync)
            var hostRigOnClient = NetRigPlayer.For(cr, hr.LocalPlayer);
            Vector3 a = hostRigOnClient.Head; await Task.Delay(700); Vector3 b = hostRigOnClient.Head;
            Check("remote_head_moves", (a - b).magnitude > 0.02f, $"host head on client moved {(a - b).magnitude:0.000} m");
            var clientRigOnHost = NetRigPlayer.For(hr, cr.LocalPlayer);
            a = clientRigOnHost.Hand; await Task.Delay(700); b = clientRigOnHost.Hand;
            Check("remote_hand_moves", (a - b).magnitude > 0.02f, $"client hand on host moved {(a - b).magnitude:0.000} m");
            // 3) client grabs the shared host-authoritative prop
            await Wait(() => NetSharedProp.In(cr) != null, 10f, "shared prop on client");
            var propC = NetSharedProp.In(cr);
            propC.RPC_RequestGrab(cr.LocalPlayer);
            await Wait(() => propC.Holder == cr.LocalPlayer, 5f, "grab granted");
            await Task.Delay(500);
            var myRig = NetRigPlayer.For(cr, cr.LocalPlayer);
            float err = Vector3.Distance(propC.Pos, myRig.Hand);
            Check("shared_grab_follows_client_hand", err < 0.25f, $"holder=client, prop-hand error {err:0.000} m");
            // 3b) contention: host tries to grab while client holds -> must stay with client
            var propH = NetSharedProp.In(hr);
            propH.RPC_RequestGrab(hr.LocalPlayer); await Task.Delay(400);
            Check("grab_contention_host_authoritative", propC.Holder == cr.LocalPlayer, $"holder stays client after host request (grabs={propC.GrabCount})");
            // 4) scene transition driven by host; state survives
            if (SceneB >= 0)
            {
                int loadsBefore = _client.SceneLoads;
                // Multi-peer: scenes are per-runner, so load additively (single-peer game builds use Single).
                hr.LoadScene(SceneRef.FromIndex(SceneB), Mode == "multipeer" ? LoadSceneMode.Additive : LoadSceneMode.Single);
                await Wait(() => _client.SceneLoads > loadsBefore, 20f, "client scene load");
                await Task.Delay(800);
                bool rigs = CountRigs(cr) >= 2;
                propC = NetSharedProp.In(cr); myRig = NetRigPlayer.For(cr, cr.LocalPlayer);
                err = propC != null && myRig != null ? Vector3.Distance(propC.Pos, myRig.Hand) : 99f;
                Check("scene_transition_keeps_sync", rigs && propC != null && propC.Holder == cr.LocalPlayer && err < 0.25f, $"after LoadScene: rigs={CountRigs(cr)} holder={(propC != null ? propC.Holder.ToString() : "-")} err={err:0.000}");
            }
            propC?.RPC_Release(cr.LocalPlayer); await Task.Delay(300);
            Check("release", propC != null && propC.Holder == PlayerRef.None, "prop released");
        }

        static int CountRigs(NetworkRunner r) { int n = 0; foreach (var x in NetRigPlayer.All) if (x != null && x.Runner == r) n++; return n; }

        public void Finish()
        {
            if (_done) return; _done = true;
            int fail = 0; foreach (var v in _checks.Values) if (v.StartsWith("FAIL")) fail++;
            var sb = new StringBuilder();
            sb.Append("{\n  \"utc\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n  \"mode\": \"").Append(Mode).Append("\",\n  \"room\": \"").Append(RoomCode)
              .Append("\",\n  \"fusion\": \"").Append(typeof(NetworkRunner).Assembly.GetName().Version).Append("\",\n  \"pass\": ").Append(fail == 0 && _checks.Count > 0 ? "true" : "false").Append(",\n  \"checks\": {\n");
            int i = 0; foreach (var kv in _checks) sb.Append("    \"").Append(kv.Key).Append("\": \"").Append(Esc(kv.Value)).Append('"').Append(++i < _checks.Count ? ",\n" : "\n");
            sb.Append("  },\n  \"log\": [\n");
            for (int k = 0; k < _log.Count; k++) sb.Append("    \"").Append(Esc(_log[k])).Append('"').Append(k + 1 < _log.Count ? ",\n" : "\n");
            sb.Append("  ]\n}\n");
            if (!string.IsNullOrEmpty(ResultPath)) File.WriteAllText(ResultPath, sb.ToString());
            Debug.Log("[NetSmoke] DONE pass=" + (fail == 0));
            _host?.Runner?.Shutdown(); _client?.Runner?.Shutdown();
        }

        static string Esc(string v) => v.Replace("\\", "/").Replace("\"", "'").Replace("\r", "").Replace("\n", " | ");

        public void OnPeerShutdown(NetPeer p, ShutdownReason r) { if (!_done) Log($"{p.Name} shutdown: {r}"); }
        public void Note(string s) => Log(s);
    }
}
