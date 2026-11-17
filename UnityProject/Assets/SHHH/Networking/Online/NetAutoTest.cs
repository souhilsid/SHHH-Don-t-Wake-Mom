using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// NET-G4 independent-process test driver (standalone Windows players, usually -batchmode -nographics).
    /// <c>-shhhAuto=host:CODE:HUMANS</c> creates the real Photon room, waits for HUMANS-1 clients, starts the match; its own seat
    /// is played by the normal bot body (authoritative). <c>-shhhAuto=client:CODE:NAME</c> joins with the code, readies up, and
    /// plays its personal duty through the REAL client path: networked hand pose + grab/release intents to the host, which
    /// validates placement with host physics; success is observed in the client's journal replica.
    /// Each process writes <c>-shhhOut=FILE</c> JSON (evidence) and quits at the end of the match.
    /// </summary>
    public sealed class NetAutoTest : MonoBehaviour
    {
        string _role, _code, _name = "Bot", _out; int _humans = 2;
        readonly List<string> _log = new List<string>();
        readonly Dictionary<string, object> _r = new Dictionary<string, object>();
        float _t0; bool _done; static string _lookFile;
        Vector3 _hand, _head; bool _handOn; Quaternion _handRot = Quaternion.identity;
        int _grabsAsked, _grabsGranted, _steps, _stepsDone, _roundsLoaded, _maxPeers;
        readonly List<string> _rooms = new List<string>();
        readonly List<string> _elims = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string auto = null, outp = null;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("-shhhAuto=")) auto = a.Substring(10);
                if (a.StartsWith("-shhhOut=")) outp = a.Substring(9);
                if (a.StartsWith("-shhhLook=")) _lookFile = a.Substring(10);
            }
            if (Application.isEditor)
            {
                var flag = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation", "netauto.flag"));
                if (auto == null && File.Exists(flag)) { auto = File.ReadAllText(flag).Trim(); File.Delete(flag); outp = Path.Combine(Path.GetDirectoryName(flag), "net_auto_editor.json"); }
            }
            if (string.IsNullOrEmpty(auto)) return;
            var go = new GameObject("NetAutoTest"); DontDestroyOnLoad(go);
            var t = go.AddComponent<NetAutoTest>();
            var parts = auto.Split(':');
            t._role = parts[0]; t._code = parts.Length > 1 ? parts[1] : "TEST";
            if (t._role == "host" && parts.Length > 2) int.TryParse(parts[2], out t._humans);
            if (t._role == "client" && parts.Length > 2) t._name = parts[2];
            t._out = outp ?? Path.Combine(Application.persistentDataPath, $"net_auto_{t._role}_{t._name}.json");
        }

        IEnumerator HostDrawerSampler()
        {
            while (true)
            {
                var chest = MechFurniture.All.FirstOrDefault(f => f != null && f.Kind == "chest");
                if (chest != null && chest.Drawers.Count > 0) _hostDrawerMax = Mathf.Max(_hostDrawerMax, chest.Drawers[0].Opening);
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        /// <summary>Host: while a REMOTE seat holds the food bag, record what the authoritative physics does with it (tilt, kibble, stream).</summary>
        IEnumerator HostFeedSampler()
        {
            float last = -9f;
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                var round = GameServices.Round; var bag = RoomPropOfKind("pet_food"); var bowl = RoomPropOfKind("pet_bowl_dog");
                if (round == null || bag == null) continue;
                int holder = round.HolderOf(bag.Spec.Id); if (holder < 0 || holder == GameServices.LocalSlot) continue;
                if (Time.realtimeSinceStartup - last < 0.6f) continue; last = Time.realtimeSinceStartup;
                var lc = Vessels.Of(bag); var bl = bowl != null ? Vessels.Of(bowl) : null;
                float tilt = Vector3.Angle(Vector3.up, bag.transform.up);
                Note($"host feed sample: bag held by seat {holder + 1} at {bag.transform.position:F2} tilt {tilt:0} deg; kibble {(lc != null ? lc.Amount * 1000f : -1f):0} g, pouring {lc != null && lc.Pouring}, poured {(lc != null ? lc.PouredOut * 1000f : 0f):0} g, stream {(lc != null ? lc.StreamLanding : "-")}; bowl at {(bowl != null ? bowl.transform.position.ToString("F2") : "?")} has {(bl != null ? bl.Amount * 1000f : -1f):0} g");
            }
        }

        /// <summary>Client: my own tracked body (only exists where the Genies SDK runs, i.e. windowed / headset players).</summary>
        IEnumerator ClientSelfBodySampler()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(2f);
                var sb = GameServices.Room != null ? GameServices.Room.SelfBody : null; var b = sb != null ? sb.Body : null;
                _r["v12_self_body"] = sb == null ? "no self body (no human local seat)" : b == null ? $"self body view, Genies {(sb.Avatar != null && sb.Avatar.HasGeniesBody)}, not tracked" : $"my Genies body tracked: eyes {b.EyeHeight:0.00} m scale {b.Scale:0.00}, head {b.HeadError * 100f:0} cm, feet {b.FootLift * 100f:0} cm, calibrated {b.Calibrated}";
            }
        }

        /// <summary>Windowed test players only: a picture of the remote player's body as this peer renders it (camera 2 m in front).</summary>
        Vector3? _lastRemoteHead;
        void CaptureBody(RemoteHumanView v, int n)
        {
            try
            {
                var body = v.transform; var at = body.position + Vector3.up * 0.9f; var from = at + body.forward * 2.0f + Vector3.up * 0.3f;
                // an indoor viewpoint (ceiling above) with nothing between the camera and the body
                foreach (float ang in new[] { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 180f })
                    foreach (float dist in new[] { 1.8f, 1.3f })
                    {
                        var cand = at + Quaternion.AngleAxis(ang, Vector3.up) * body.forward * dist + Vector3.up * 0.35f;
                        if (Physics.CheckSphere(cand, 0.15f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (!Physics.Raycast(cand, Vector3.up, 3.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (Physics.Linecast(cand, at, out var blk, ~0, QueryTriggerInteraction.Ignore) && !blk.collider.transform.IsChildOf(body)) continue;
                        from = cand; goto picked;
                    }
                picked:
                // the scene's own (URP-configured) camera, moved for one render and put back
                var c = Camera.main; if (c == null) { Note("body capture: no main camera"); return; }
                var t = c.transform; var p0 = t.position; var r0 = t.rotation; var tt0 = c.targetTexture;
                t.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
                var rt = new RenderTexture(640, 480, 24); c.targetTexture = rt; c.Render(); c.targetTexture = tt0; t.SetPositionAndRotation(p0, r0);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(640, 480, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); tex.Apply(); RenderTexture.active = prev;
                var dir = string.IsNullOrEmpty(_out) ? Application.persistentDataPath : Path.GetDirectoryName(_out);
                File.WriteAllBytes(Path.Combine(dir, $"{_role}_sees_seat{v.Slot + 1}_{n}.png"), tex.EncodeToPNG());
                Destroy(tex); rt.Release(); Destroy(rt);
                Note($"captured seat {v.Slot + 1}'s body ({n} s)");
            }
            catch (Exception e) { Note("body capture: " + e.Message); }
        }

        /// <summary>Host: how this peer renders the remote player's body (Genies loaded, tracked by their head + hands, scale, errors).</summary>
        IEnumerator HostBodySampler()
        {
            float maxHead = 0f, maxHand = 0f, maxFoot = 0f; int samples = 0, reachable = 0, teleports = 0; string footCtx = ""; string last = "no remote human view";
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                foreach (var v in FindObjectsByType<RemoteHumanView>(FindObjectsSortMode.None))
                {
                    if (v == null || v.Avatar == null) continue;
                    var b = v.Body;
                    if (b == null) { last = $"seat {v.Slot + 1}: genies {v.Avatar.HasGeniesBody}, not tracked yet"; continue; }
                    if (!b.Calibrated) { last = $"seat {v.Slot + 1}: tracked, calibrating"; continue; }
                    // the test client teleports its head between probe spots - no person moves 1.5 m in a second: skip that second
                    var hNow = GameServices.RemotePose?.Invoke(v.Slot, 0);
                    bool jumped = hNow.HasValue && _lastRemoteHead.HasValue && Vector3.Distance(hNow.Value.position, _lastRemoteHead.Value) > 1.5f;
                    _lastRemoteHead = hNow.HasValue ? hNow.Value.position : (Vector3?)null;
                    if (jumped) { teleports++; continue; }
                    samples++; maxHead = Mathf.Max(maxHead, b.HeadError);
                    if (Mathf.Abs(b.FootLift) > maxFoot) { maxFoot = Mathf.Abs(b.FootLift); var hh = GameServices.RemotePose?.Invoke(v.Slot, 0); footCtx = $"head at {(hh.HasValue ? hh.Value.position.y : -1f):0.00} m, eyes calibrated {b.EyeHeight:0.00}, crouch {b.CrouchDepth:0.00}"; }
                    var f = GameServices.RemotePose; var hp = f?.Invoke(v.Slot, 0);
                    for (int k = 1; k <= 2; k++) { var hk = f?.Invoke(v.Slot, k); if (hp.HasValue && hk.HasValue && Vector3.Distance(hp.Value.position, hk.Value.position) < 1.0f) { maxHand = Mathf.Max(maxHand, k == 1 ? b.HandErrorL : b.HandErrorR); reachable++; } }
                    if (!Application.isBatchMode && (samples == 4 || samples == 20 || samples == 45 || samples == 80)) CaptureBody(v, samples);
                    last = $"seat {v.Slot + 1}: Genies body tracked by their head + hands; eyes {b.EyeHeight:0.00} m scale {b.Scale:0.00}; now head {b.HeadError * 100f:0} cm, wrists {b.HandErrorL * 100f:0}/{b.HandErrorR * 100f:0} cm, feet {b.FootLift * 100f:0} cm; worst over {samples} s ({teleports} s after test-client head teleports skipped): head {maxHead * 100f:0} cm, wrist {maxHand * 100f:0} cm (over {reachable} hand samples within a person's reach), feet {maxFoot * 100f:0} cm ({footCtx})";
                }
                _r["v12_remote_body"] = last;
            }
        }

        void Note(string s) { var l = $"[{Time.realtimeSinceStartup - _t0:0.0}] {s}"; _log.Add(l); Debug.Log("[SHHH][netauto] " + l); }

        IEnumerator Start()
        {
            _t0 = Time.realtimeSinceStartup;
            Application.runInBackground = true;
            Note($"role={_role} code={_code} name={_name} humans={_humans} batch={Application.isBatchMode}");
            _r["role"] = _role; _r["code"] = _code; _r["name"] = _name;
            while (GameRoot.I == null || GameRoot.I.State != AppState.FrontEnd || ShhhNet.I == null) yield return null;
            if (!string.IsNullOrEmpty(_lookFile)) { try { Settings.Current.GeniesDefinition = File.ReadAllText(_lookFile).Trim(); Note($"Genies look loaded from file ({Settings.Current.GeniesDefinition.Length} chars)"); } catch (Exception e) { Note("look file: " + e.Message); } }
            ShhhNet.I.Note += s => Note("net: " + s);
            GameRoot.I.Milestone += m => { Note("milestone " + m); if (m.StartsWith("round_start")) { _roundsLoaded++; _rooms.Add(GameServices.Room != null ? GameServices.Room.Def.Title : m); } if (m.StartsWith("eliminated_")) _elims.Add(m); };
            if (_role == "host") yield return Host(); else yield return Client();
            Finish();
        }

        IEnumerator Host()
        {
            GameRoot.AutopilotLocalSeat = true;     // host seat: real bot body on the authoritative side
            ShhhNet.I.CreateWithCode(_code, "HostBot", 0);
            float w = 0; while (!ShhhNet.I.Active && w < 30) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Wait(() => NetLobby.I != null, 30, "session object");
            _r["region"] = ShhhNet.I.Region;
            ShhhNet.I.SetReady(true);
            Note($"room {_code} open, waiting for {_humans - 1} clients");
            yield return Wait(() => CountHumans() >= _humans && ShhhNet.I.CanStart, 240, "all clients joined + ready");
            _r["humans_in_lobby"] = CountHumans();
            yield return new WaitForSecondsRealtime(1f);
            ShhhNet.I.StartMatch();
            StartCoroutine(HostDrawerSampler()); StartCoroutine(HostFeedSampler()); StartCoroutine(HostBodySampler());
            yield return Wait(() => GameServices.Match != null && (GameServices.Match.Phase == MatchPhase.Winner || GameServices.Match.Phase == MatchPhase.Results), 900, "match to finish");
            var m = GameServices.Match;
            if (m != null) { _r["winner"] = m.Summary.WinnerSlot; _r["winner_is_bot"] = m.Summary.WinnerIsBot; _r["elimination_order"] = string.Join(",", m.Summary.EliminationOrder); _r["final_phase"] = m.Phase.ToString(); _r["journal_kb"] = m.Journal != null ? m.Journal.Length / 1024 : 0; }
            _r["client_reports"] = string.Join(" | ", ShhhNet.I.ClientReports);
            yield return new WaitForSecondsRealtime(8f); // let clients see the result and report
            _r["client_reports"] = string.Join(" | ", ShhhNet.I.ClientReports);
            _r["genies_looks_known_before_leave"] = ShhhNet.I.LooksKnown;
            // V12 multiplayer evidence: client hand-moved mechanisms reached the host's rules, the host's copy moved, client slams were voiced
            _r["v12_mech_intents_applied"] = ShhhNet.I.MechIntentsApplied; _r["v12_client_slams_voiced"] = ShhhNet.I.ClientSlamsVoiced;
            _r["v12_host_drawer0_max_opening"] = _hostDrawerMax;
            var hr = GameServices.Round; if (hr != null) { _r["v12_host_pet_fed_g"] = hr.PetFedGrams; _r["v12_host_dog_meals"] = hr.PetMeals[1]; _r["v12_host_binned"] = hr.Binned.Count; _r["v12_host_pet_noise"] = hr.Log != null ? 0 : 0; } _r["v12_house_mech"] = m != null && GameServices.Round != null ? string.Join(",", GameServices.Round.HouseMech.Select(kv => kv.Key + ":" + kv.Value)) : "";
            ShhhNet.I.Leave();
        }

        IEnumerator Client()
        {
            ShhhNet.InputOverride = () =>
            {
                var h = _head == Vector3.zero ? new Vector3(0, 1.4f, -2f) : _head;
                return new NetHumanInput { Head = h, HeadRot = Quaternion.identity, R = _hand, RRot = _handRot, RTracked = _handOn, L = h + new Vector3(-0.25f, -0.5f, 0.1f), LRot = Quaternion.identity, LTracked = true };
            };
            // the host may still be creating the room: retry joining for up to a minute
            float w = 0;
            while (w < 60f)
            {
                ShhhNet.I.Join(_code, _name, Mathf.Abs(_name.GetHashCode()) % 4);
                float t = 0; while (t < 15f && !(ShhhNet.I.Active && NetLobby.I != null)) { t += Time.unscaledDeltaTime; yield return null; }
                if (ShhhNet.I.Active && NetLobby.I != null) break;
                Note("join failed: " + ShhhNet.I.Status + " - retrying"); w += t + 3f; yield return new WaitForSecondsRealtime(3f);
            }
            if (NetLobby.I == null) { _r["error"] = "could not join: " + ShhhNet.I.Status; yield break; }
            _r["joined"] = true; _r["region"] = ShhhNet.I.Region;
            yield return Wait(() => NetLobby.I.SeatOf(RunnerLocal()) >= 0, 20, "seated");
            yield return new WaitForSecondsRealtime(0.5f);
            ShhhNet.I.SetReady(true);
            Note($"seated in {NetLobby.I.SeatOf(RunnerLocal()) + 1}, ready");
            yield return Wait(() => GameServices.Match != null, 300, "match start (journal)");
            Report($"replica running, seat {GameServices.LocalSlot + 1}, Genies looks known {ShhhNet.I.LooksKnown}");
            if (Application.isEditor) StartCoroutine(RemoteGeniesEvidence());
            StartCoroutine(MechProbe()); StartCoroutine(ClientSelfBodySampler());
            while (GameServices.Match != null && GameServices.Match.Phase != MatchPhase.Winner && GameServices.Match.Phase != MatchPhase.Results && GameServices.Match.Phase != MatchPhase.Cancelled && ShhhNet.I.Active)
            {
                _maxPeers = Mathf.Max(_maxPeers, CountHumans());
                // a probe owns the (one) simulated hand while it runs: the chore autopilot waits instead of dragging the probe's item away
                if (_probing) { yield return new WaitForSecondsRealtime(0.25f); continue; }
                var round = GameServices.Round; var room = GameServices.Room;
                if (round != null && room != null && round.IsPlayable && round.Roster[GameServices.LocalSlot].IsActive && NetWorld.I != null && NetWorld.I.RoomKey == NetWorld.KeyFor(round))
                {
                    if (round.Recipes != null)
                    {
                        // V7 recipe rounds: follow the solver's plan for my recipe, acting only through network intents + my hand pose
                        var op = NextRecipeOp(round);
                        if (op.HasValue) { _inOp = true; yield return DoRecipeOp(round, room, op.Value); _inOp = false; continue; }
                    }
                    var step = NextStep(round);
                    if (step != null) { _inOp = true; yield return DoStep(round, room, step); _inOp = false; continue; }
                }
                // confrontation: show a true memory if I have one about this incident, then "nothing to add"
                if (round != null && round.Interro != null && round.Mom.State == MomState.Interrogating && round.Interro.EvidenceOpen && round.Roster[GameServices.LocalSlot].IsActive && !round.Interro.Done.Contains(GameServices.LocalSlot) && _confronted != round.Interro)
                {
                    _confronted = round.Interro;
                    foreach (var mm in round.Belief.MemoriesOf(GameServices.LocalSlot))
                        if (round.Interro.Incident != null && mm.Incident == round.Interro.Incident.Id && !mm.Presented) { GameServices.SendConfrontIntent?.Invoke(0, mm.Id); _cards++; Note("showed Mom memory M" + mm.Id + ": " + mm.Card); break; }
                    GameServices.SendConfrontIntent?.Invoke(2, 0); _passes++;
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
            _r["recipe_ops_attempted"] = _rOps; _r["recipe_ops_changed_state"] = _rOpsOk; _r["recipes_completed_by_me"] = _rDone; _r["confront_cards"] = _cards; _r["confront_passes"] = _passes;
            var m = GameServices.Match;
            if (m != null) { _r["winner"] = m.Summary.WinnerSlot; _r["final_phase"] = m.Phase.ToString(); _r["elimination_order"] = string.Join(",", m.Summary.EliminationOrder); }
            Report($"done: steps {_stepsDone}/{_steps}, grabs {_grabsGranted}/{_grabsAsked}, rooms {string.Join(">", _rooms)}, desync {ShhhNet.I.Desyncs}/{ShhhNet.I.Checksums}, winner {(m != null ? m.Summary.WinnerSlot + 1 : 0)}");
            yield return new WaitForSecondsRealtime(2f);
            ShhhNet.I.Leave();
        }

        float _hostDrawerMax; bool _probing, _inOp;
        /// <summary>V12 network probe (client): move a real house drawer the way a hand does (open, then shove it shut so it slams) and watch
        /// the replicated rules state; the host samples its own copy of the drawer (NetAutoTest host loop).</summary>
        IEnumerator MechProbe()
        {
            float w = 0; MechFurniture chest = null;
            while (w < 120f && (chest == null || chest.Drawers.Count == 0 || GameServices.Round == null || !GameServices.Round.IsPlayable)) { chest = MechFurniture.All.FirstOrDefault(f => f != null && f.Kind == "chest"); w += 0.5f; yield return new WaitForSecondsRealtime(0.5f); }
            if (chest == null || chest.Drawers.Count == 0) { Report("mech probe: no chest of drawers in the house"); yield break; }
            var d = chest.Drawers[0]; int id = d.HouseId, seat = GameServices.LocalSlot; int sent0 = ShhhNet.I.IntentsSent;
            d.ScriptedMove(seat, 1f); float t = 0;
            while (t < 12f && !(GameServices.Round != null && GameServices.Round.HouseMech.TryGetValue(id, out var lv) && lv >= 60)) { t += Time.unscaledDeltaTime; yield return null; }
            bool opened = GameServices.Round != null && GameServices.Round.HouseMech.TryGetValue(id, out var l1) && l1 >= 60;
            Report($"mech probe: drawer {id} opened by seat {seat + 1}; replicated level {(GameServices.Round.HouseMech.TryGetValue(id, out var x1) ? x1 : -1)} after {t:0.00} s ({(opened ? "OK" : "NOT REPLICATED")})");
            yield return new WaitForSecondsRealtime(2f);
            d.Shove(seat, -2.5f); t = 0;
            while (t < 12f && !(GameServices.Round.HouseMech.TryGetValue(id, out var lv2) && lv2 == 0)) { t += Time.unscaledDeltaTime; yield return null; }
            bool closed = GameServices.Round.HouseMech.TryGetValue(id, out var l2) && l2 == 0;
            Report($"mech probe: drawer {id} shoved shut (local slams {d.Slams}); replicated level {l2} after {t:0.00} s ({(closed ? "OK" : "NOT REPLICATED")}); intents sent {ShhhNet.I.IntentsSent - sent0}");
            _r["v12_mech_probe"] = opened && closed ? "OK" : "FAIL";
            // take the hand from the chore autopilot: wait for its current chore to finish, then run the hand probes alone
            _probing = true; float wq = 0; while (_inOp && wq < 30f) { wq += Time.unscaledDeltaTime; yield return null; }
            Report($"probes own the hand (autopilot paused after {wq:0.0} s)");
            yield return FeedProbe();
            yield return TrashProbe();
            _probing = false; _handOn = false;
        }

        PropBody RoomPropOfKind(string kind) { var room = GameServices.Room; if (room == null) return null; foreach (var p in room.Def.Props) if (p.Kind == kind) return room.Prop(p.Id); return null; }

        /// <summary>V12 network probe: feed the dog through the REAL client path (grab intent, hand pose incl. rotation, the host's physics
        /// pours the kibble, the host's dog eats, a journaled meal) and watch the replica.</summary>
        IEnumerator FeedProbe()
        {
            var bag = RoomPropOfKind("pet_food"); var bowl = RoomPropOfKind("pet_bowl_dog"); var round = GameServices.Round;
            if (bag == null || bowl == null || round == null) { Report($"feed probe: bag {bag != null} bowl {bowl != null}"); yield break; }
            { float wp = 0; while (!round.IsPlayable && wp < 60f) { wp += 0.25f; yield return new WaitForSecondsRealtime(0.25f); } }
            float fed0 = round.PetFedGrams; int meals0 = round.PetMeals[1];
            _handOn = true; var at = bag.Col != null ? bag.Col.bounds.center : bag.transform.position; _head = at + new Vector3(0, 0.75f, -0.45f);
            yield return MoveHand(at + Vector3.up * 0.3f, at + Vector3.up * 0.02f, 0.6f);
            ShhhNet.I.RequestGrab(bag.Spec.Id, 2); float t = 0;
            while (ShhhNet.I.HolderOf(bag.Spec.Id) != GameServices.LocalSlot && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            if (ShhhNet.I.HolderOf(bag.Spec.Id) != GameServices.LocalSlot) { Report("feed probe: the host did not give me the food bag"); yield break; }
            var b = bowl.transform.position; // the bag tips toward +z: hold it a little behind the bowl
            var over = b + Vector3.up * 0.45f - Vector3.forward * 0.12f; _head = b + new Vector3(0, 1.0f, -0.6f);
            yield return MoveHand(_hand, new Vector3(_hand.x, Mathf.Max(_hand.y, 0.9f), _hand.z), 0.5f);
            // carry at a walking pace (~1.1 m/s) - a superhuman hand sweep makes the host re-sync the bag through furniture
            // carried at chest height: the bag hangs ~25 cm under the hand and must clear sofa backs (1.06 m) along the route
            yield return CarryAlong(over + new Vector3(0, 0.45f, 0), Mathf.Max(_hand.y, 1.2f)); _head = b + new Vector3(0, 1.0f, -0.6f);
            yield return MoveHand(_hand, over, 0.5f);
            for (float a = 0; a <= 115f; a += 3f) { _handRot = Quaternion.AngleAxis(a, Vector3.right); yield return new WaitForSecondsRealtime(0.06f); }
            yield return new WaitForSecondsRealtime(2.5f);
            _handRot = Quaternion.identity; yield return new WaitForSecondsRealtime(0.6f);
            yield return MoveHand(_hand, b + new Vector3(0.5f, 0.2f, 0f), 0.6f);
            ShhhNet.I.RequestRelease(bag.Spec.Id, Vector3.zero, false, 2); yield return new WaitForSecondsRealtime(0.5f);
            float bowlMl = Vessels.Of(bowl) != null ? Vessels.Of(bowl).Amount * 1000f : -1f;
            Report($"feed probe: poured over the dog bowl by hand pose; bowl shows {bowlMl:0} g (host stream)");
            t = 0; while (t < 120f && round.PetFedGrams <= fed0 + 1f) { t += Time.unscaledDeltaTime; yield return new WaitForSecondsRealtime(0.5f); t += 0.5f; }
            bool ate = round.PetFedGrams > fed0 + 1f;
            Report($"feed probe: replica sees the pets ate {round.PetFedGrams - fed0:0} g (dog meals {meals0} -> {round.PetMeals[1]}, dog hunger {round.PetHunger[1]:0}) after {t:0} s ({(ate ? "OK" : "NOT SEEN")})");
            _r["v12_feed_probe"] = ate ? "OK" : "FAIL"; _r["v12_feed_bowl_g"] = bowlMl;
        }

        /// <summary>V12 network probe: open a bin lid as a house mechanism, drop a piece of trash in through the client path, watch the
        /// replica's journaled Binned set.</summary>
        IEnumerator TrashProbe()
        {
            var round = GameServices.Round; var room = GameServices.Room; if (round == null || room == null) yield break;
            var bin = MechFurniture.All.Where(f => f != null && f.Kind == "bin" && f.Lid != null).OrderBy(f => Vector3.Distance(f.transform.position, _hand)).FirstOrDefault();
            PropBody trash = null; foreach (var p in room.Def.Props) if (HouseClutter.IsTrash(p.Kind) && !round.Binned.Contains(p.Id)) { var pb = room.Prop(p.Id); if (pb != null && (trash == null || bin != null && Vector3.Distance(pb.transform.position, bin.transform.position) < Vector3.Distance(trash.transform.position, bin.transform.position))) trash = pb; }
            if (bin == null || trash == null) { Report($"trash probe: bin {bin != null} trash {trash != null}"); yield break; }
            _handOn = true; int seat = GameServices.LocalSlot; bin.Lid.ScriptedMove(seat, 1f); yield return new WaitForSecondsRealtime(2.5f);
            int lidLvl = round.HouseMech.TryGetValue(bin.Lid.HouseId, out var ll) ? ll : -1;
            Report($"trash probe: bin lid {bin.Lid.HouseId} opened here to {bin.Lid.Opening * 100f:0}%; replicated level {lidLvl}");
            var at = trash.Col != null ? trash.Col.bounds.center : trash.transform.position; _head = at + new Vector3(0, 0.75f, -0.45f);
            yield return MoveHand(at + Vector3.up * 0.3f, at + Vector3.up * 0.02f, 0.6f);
            float t = 0;
            for (int attempt = 0; attempt < 4 && ShhhNet.I.HolderOf(trash.Spec.Id) != GameServices.LocalSlot; attempt++)
            {
                // Mom's wake/confront sequence freezes grabs (host rules) - wait for play to resume, then reach again
                float wp = 0; while (!round.IsPlayable && wp < 60f) { wp += 0.25f; yield return new WaitForSecondsRealtime(0.25f); }
                ShhhNet.I.RequestGrab(trash.Spec.Id, 2); t = 0;
                while (ShhhNet.I.HolderOf(trash.Spec.Id) != GameServices.LocalSlot && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            }
            if (ShhhNet.I.HolderOf(trash.Spec.Id) != GameServices.LocalSlot) { Report("trash probe: the host did not give me the trash"); yield break; }
            var top = bin.Focus; _head = top + new Vector3(0, 0.8f, -0.6f);
            yield return MoveHand(_hand, new Vector3(_hand.x, Mathf.Max(_hand.y, top.y + 0.45f), _hand.z), 0.5f);
            yield return CarryAlong(top + Vector3.up * 0.3f, Mathf.Max(_hand.y, top.y + 0.45f)); _head = top + new Vector3(0, 0.8f, -0.6f);
            yield return MoveHand(_hand, top + Vector3.up * 0.12f, 0.4f);
            ShhhNet.I.RequestRelease(trash.Spec.Id, Vector3.zero, false, 2);
            t = 0; while (t < 10f && !round.Binned.Contains(trash.Spec.Id)) { t += Time.unscaledDeltaTime; yield return null; }
            bool ok = round.Binned.Contains(trash.Spec.Id);
            Report($"trash probe: {trash.Spec.Kind}#{trash.Spec.Id} dropped into the {(bin.transform.position.x > 0 ? "right" : "left")} bin; replica binned {ok} after {t:0.0} s (binned total {round.Binned.Count})");
            bin.Lid.ScriptedMove(seat, 0f);
            _r["v12_trash_probe"] = ok ? "OK" : "FAIL";
        }

        static Fusion.PlayerRef RunnerLocal()
        {
            var r = FindFirstObjectByType<Fusion.NetworkRunner>(); return r != null ? r.LocalPlayer : Fusion.PlayerRef.None;
        }

        int _rOps, _rOpsOk, _rDone, _cards, _passes; Interrogation _confronted;
        readonly Dictionary<string, int> _opTries = new Dictionary<string, int>();

        ROp? NextRecipeOp(RoundController round)
        {
            // continuous match: a seat owns one job per room - work through them in order, skipping a job whose next op keeps failing
            foreach (var mine in round.Recipes.Instances)
            {
                if (mine.Owner != GameServices.LocalSlot || mine.Complete) continue;
                var plan = RecipeSolver.Plan(round.Recipes, mine, GameServices.LocalSlot);
                if (plan.Count == 0) continue;
                var op = plan[0]; string key = mine.Def.Id + ":" + op;
                _opTries.TryGetValue(key, out int n); if (n > 3) continue; _opTries[key] = n + 1;
                _curInst = mine; return op;
            }
            return null;
        }
        RecipeInstance _curInst;

        IEnumerator DoRecipeOp(RoundController round, RoomRuntime room, ROp op)
        {
            _rOps++;
            var rt = RecipeRuntime.I; var w = round.Recipes.World; string before = w.Snapshot();
            RecipeInstance inst = _curInst;
            bool wasComplete = inst != null && inst.Complete;
            int roomProp = op.Prop >= 0 && round.RoomPropOf.TryGetValue(op.Prop, out var rp) ? rp : -1;
            switch (op.Verb)
            {
                case Verb.PickUp:
                {
                    var prop = room.Prop(roomProp); if (prop == null) break;
                    var at = prop.Col != null ? prop.Col.bounds.center : prop.transform.position;
                    _head = at + new Vector3(0, 0.75f, -0.45f);
                    yield return MoveHand(at + Vector3.up * 0.25f, at + Vector3.up * 0.02f, 0.5f);
                    _grabsAsked++; ShhhNet.I.RequestGrab(roomProp, 2);
                    float t = 0; while (ShhhNet.I.HolderOf(roomProp) != GameServices.LocalSlot && t < 2.5f) { t += Time.unscaledDeltaTime; yield return null; }
                    if (ShhhNet.I.HolderOf(roomProp) == GameServices.LocalSlot) _grabsGranted++;
                    break;
                }
                case Verb.PlaceInSocket:
                {
                    var fv = rt != null ? rt.FixtureView(op.Fixture) : null; if (fv == null || ShhhNet.I.HolderOf(roomProp) != GameServices.LocalSlot) break;
                    var target = fv.SlotPoint(op.Slot) + Vector3.up * 0.1f;
                    float safeY = Mathf.Max(_hand.y, target.y) + 0.3f;
                    _head = target + new Vector3(0, 0.6f, 0) + fv.transform.forward * 0.6f;
                    yield return MoveHand(_hand, new Vector3(_hand.x, safeY, _hand.z), 0.4f);
                    yield return MoveHand(_hand, new Vector3(target.x, safeY, target.z), 0.8f);
                    yield return MoveHand(_hand, target, 0.5f);
                    yield return new WaitForSecondsRealtime(0.4f);
                    ShhhNet.I.RequestRelease(roomProp, Vector3.zero, false, 2);
                    float t = 0; while (w.Snapshot() == before && t < 4f) { t += Time.unscaledDeltaTime; yield return null; }
                    break;
                }
                case Verb.PutDown: case Verb.Release:
                    if (roomProp >= 0 && ShhhNet.I.HolderOf(roomProp) == GameServices.LocalSlot) { ShhhNet.I.RequestRelease(roomProp, Vector3.zero, false, 2); yield return new WaitForSecondsRealtime(1f); }
                    break;
                case Verb.Open: case Verb.Close: case Verb.Press:
                    GameServices.SendRecipeIntent?.Invoke(op); yield return new WaitForSecondsRealtime(0.8f); break;
                case Verb.Stroke:
                {
                    var fv = rt != null && op.Fixture >= 0 ? rt.FixtureView(op.Fixture) : null; if (fv == null) break;
                    // V8: scrub on the active dirt patch (patch-local cleaning)
                    var pw = fv.PatchWorld(fv.ActivePatch); var c = new Vector3(pw.x, fv.transform.position.y + 0.04f, pw.z);
                    var side = fv.transform.right * Mathf.Min(0.05f, fv.PatchRadius(fv.ActivePatch));
                    _head = c + new Vector3(0, 0.6f, 0) + fv.transform.forward * 0.6f;
                    for (int k = 0; k < 5; k++) { yield return MoveHand(c - side, c + side, 0.4f); yield return MoveHand(c + side, c - side, 0.4f); }
                    break;
                }
                default: yield return new WaitForSecondsRealtime(0.5f); break; // fold / pair / attach need two tracked hands: not in this harness (noted)
            }
            _handOn = false;
            yield return new WaitForSecondsRealtime(0.4f);
            if (w.Snapshot() != before) _rOpsOk++; else Note($"recipe op {op} made no change (replica view)");
            bool nowComplete = inst != null && inst.Complete;
            if (nowComplete && !wasComplete) { _rDone++; Report($"RECIPE DONE by client hands ({inst.Def.Id})"); }
        }

        ChoreStep NextStep(RoundController round)
        {
            foreach (var c in round.Board.Chores)
                if (c.IsBonus && c.BonusOwner == GameServices.LocalSlot)
                    foreach (var s in c.Steps) if (!s.Done) return s;
            return null;
        }

        readonly Dictionary<int, int> _tries = new Dictionary<int, int>();

        IEnumerator DoStep(RoundController round, RoomRuntime room, ChoreStep step)
        {
            var prop = room.Prop(step.PropId); var anchor = room.Anchor(step.AnchorId);
            _tries.TryGetValue(step.PropId, out int tries); _tries[step.PropId] = tries + 1;
            if (prop == null || anchor == null || tries > 4) { yield return new WaitForSecondsRealtime(1f); yield break; }
            _steps++;
            // walk the head next to the item (avatar), move the tracked hand onto it, pinch = grab intent
            var at = prop.Col != null ? prop.Col.bounds.center : prop.transform.position;
            _head = at + new Vector3(0, 0.75f, -0.45f);
            yield return MoveHand(at + Vector3.up * 0.25f, at + Vector3.up * 0.02f, 0.5f);
            _grabsAsked++;
            ShhhNet.I.RequestGrab(step.PropId, 2);
            float w = 0; while (ShhhNet.I.HolderOf(step.PropId) != GameServices.LocalSlot && w < 2.5f) { w += Time.unscaledDeltaTime; yield return null; }
            if (ShhhNet.I.HolderOf(step.PropId) != GameServices.LocalSlot) { Note($"grab of #{step.PropId} not granted (holder {ShhhNet.I.HolderOf(step.PropId)})"); _handOn = false; yield break; }
            _grabsGranted++;
            // Like a player: lift, carry to just in front of the slot (room side), slide in, lower, let go.
            var target = anchor.PlacePoint + Vector3.up * 0.06f;
            var open = new Vector3(-anchor.transform.position.x, 0, -anchor.transform.position.z); open = open.sqrMagnitude > 0.01f ? open.normalized : Vector3.back;
            var front = target + open * 0.35f + Vector3.up * 0.1f;
            _head = front + new Vector3(0, 0.55f, 0) + open * 0.35f;
            float safeY = Mathf.Max(_hand.y, front.y) + 0.45f; // carry high over the furniture like a careful sibling
            yield return MoveHand(_hand, new Vector3(_hand.x, safeY, _hand.z), 0.5f);
            yield return MoveHand(_hand, new Vector3(front.x, safeY, front.z), 1.2f);
            yield return MoveHand(_hand, front, 0.5f);
            yield return MoveHand(_hand, target + Vector3.up * 0.04f, 0.6f);
            yield return new WaitForSecondsRealtime(0.5f);
            var hostPose = NetPropPos(step.PropId);
            int before = round.Board.ValidatedSteps(GameServices.LocalSlot);
            ShhhNet.I.RequestRelease(step.PropId, Vector3.zero, false, 2);
            // Same-kind items are interchangeable, so success = one more validated step credited to this seat (replica board).
            w = 0; while (round.Board.ValidatedSteps(GameServices.LocalSlot) <= before && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
            var after = NetPropPos(step.PropId);
            string where = $"hand {(_hand - target).magnitude:0.00} m from slot; host item at release {(hostPose - target).magnitude:0.00} m, after {(after - anchor.PlacePoint).magnitude:0.00} m";
            if (round.Board.ValidatedSteps(GameServices.LocalSlot) > before) { _stepsDone++; Note($"STEP DONE #{step.PropId} -> anchor {step.AnchorId} (validated by host physics, seen in replica); {where}"); }
            else Report($"step #{step.PropId} ({prop.Spec.Kind}) not validated (done={step.Done} credit={step.CreditTo}); {where}");
            _handOn = false;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        static Vector3 NetPropPos(int propId)
        {
            var room = GameServices.Room; if (room == null || NetWorld.I == null) return Vector3.zero;
            for (int i = 0; i < room.Def.Props.Count && i < NetWorld.MaxProps; i++) if (room.Def.Props[i].Id == propId) return NetWorld.I.Props[i].P;
            return Vector3.zero;
        }

        /// <summary>Carry like a walking player: follow the house NavMesh around walls and furniture at ~1.1 m/s with the hand at carry
        /// height and the head just behind it, then go straight to <paramref name="to"/>. Falls back to a straight line without a path.</summary>
        IEnumerator CarryAlong(Vector3 to, float carryY)
        {
            var path = new UnityEngine.AI.NavMeshPath(); int corners = 0;
            if (UnityEngine.AI.NavMesh.SamplePosition(new Vector3(_hand.x, 0f, _hand.z), out var ha, 1.5f, UnityEngine.AI.NavMesh.AllAreas)
                && UnityEngine.AI.NavMesh.SamplePosition(new Vector3(to.x, 0f, to.z), out var hb, 1.5f, UnityEngine.AI.NavMesh.AllAreas)
                && UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, UnityEngine.AI.NavMesh.AllAreas, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                var cs = path.corners; corners = cs.Length;
                for (int i = 1; i < cs.Length - 1; i++)
                {
                    var c = new Vector3(cs[i].x, carryY, cs[i].z); var dir = c - _hand; dir.y = 0f;
                    _head = c - (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward) * 0.35f + Vector3.up * 0.6f;
                    yield return MoveHand(_hand, c, Mathf.Max(0.3f, Vector3.Distance(_hand, c) / 1.1f));
                }
            }
            Note($"carry: {(corners > 0 ? corners + "-corner NavMesh path" : "no path, straight line")} to {to:F2}");
            yield return MoveHand(_hand, to, Mathf.Max(0.5f, Vector3.Distance(_hand, to) / 1.1f));
        }

        IEnumerator MoveHand(Vector3 from, Vector3 to, float seconds)
        {
            _handOn = true; float t = 0;
            while (t < seconds) { t += Time.unscaledDeltaTime; _hand = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds)); yield return null; }
            _hand = to;
        }

        void Report(string s) { Note(s); NetLobby.I?.RPC_Report($"{_name}: {s}"); }

        /// <summary>Editor peer with the real Genies SDK: remote humans' seat bodies must wear the definition that arrived over Photon.</summary>
        IEnumerator RemoteGeniesEvidence()
        {
            yield return new WaitForSecondsRealtime(25f);
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "QA_Evidence", "genies", "net"));
            Directory.CreateDirectory(dir);
            var lobby = NetLobby.I; int k = 0;
            for (int slot = 0; slot < 4 && GameServices.Room != null; slot++)
            {
                if (slot == GameServices.LocalSlot || lobby == null || lobby.SeatPlayer[slot] < 0) continue;
                var want = ShhhNet.I.LookOfPlayer(lobby.SeatPlayer[slot]);
                SiblingAvatar av = null; foreach (var a in GameServices.Room.GetComponentsInChildren<SiblingAvatar>()) if (a.Slot == slot && !a.IsBot) av = a;
                bool ok = av != null && av.HasGeniesBody && !string.IsNullOrEmpty(want) && av.RequestedDefinition == want;
                var detail = $"avatar={(av != null)} genies={(av != null && av.HasGeniesBody)} received={(want ?? "").Length} chars, requested={(av?.RequestedDefinition ?? "").Length} chars, loaded body json={(av?.GeniesDefinition ?? "").Length} chars";
                _r[$"remote_seat{slot + 1}_genies"] = ok; _r[$"remote_seat{slot + 1}_detail"] = detail;
                Report($"remote seat {slot + 1}: Genies body wearing the replicated look = {ok} ({detail})");
                try { File.AppendAllText(Path.Combine(dir, "remote_genies.txt"), $"{DateTime.UtcNow:o} seat {slot + 1}: ok={ok} {detail}\n"); } catch { }
                var cam = GameRoot.I != null && GameRoot.I.Rig != null ? GameRoot.I.Rig.Head.GetComponent<Camera>() : Camera.main;
                if (av != null && cam != null)
                {
                    var p = av.transform.position; var from = p + av.transform.forward * 1.7f + Vector3.up * 1.3f;
                    var head = GameRoot.I.Rig.Head; var hp = head.position; var hr = head.rotation;
                    head.SetPositionAndRotation(from, Quaternion.LookRotation(p + Vector3.up * 0.7f - from));
                    var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 }; var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    var t = new Texture2D(1280, 720, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); t.Apply();
                    File.WriteAllBytes(Path.Combine(dir, $"remote_seat{slot + 1}_{k++}.png"), t.EncodeToPNG());
                    cam.targetTexture = prev; RenderTexture.active = null; Destroy(t); rt.Release(); head.SetPositionAndRotation(hp, hr);
                }
            }
        }

        static int CountHumans() { int n = 0; if (ShhhNet.I == null) return 0; foreach (var s in ShhhNet.I.Seats) if (s.Human) n++; return n; }

        IEnumerator Wait(Func<bool> c, float seconds, string what)
        {
            float t = 0; while (!c() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            if (!c()) { Note("TIMEOUT waiting for " + what); _r["error"] = "timeout: " + what; }
        }

        void Finish()
        {
            if (_done) return; _done = true;
            _r["rounds_loaded"] = _roundsLoaded; _r["rooms"] = string.Join(" > ", _rooms); _r["eliminations_seen"] = string.Join(",", _elims);
            _r["grabs_asked"] = _grabsAsked; _r["grabs_granted"] = _grabsGranted; _r["steps_attempted"] = _steps; _r["steps_validated"] = _stepsDone;
            _r["desyncs"] = ShhhNet.I != null ? ShhhNet.I.Desyncs : -1; _r["checksums"] = ShhhNet.I != null ? ShhhNet.I.Checksums : -1;
            _r["journal_bytes_seen"] = ShhhNet.I != null ? ShhhNet.I.JournalBytes : 0; _r["max_humans_seen"] = _maxPeers;
            if (ShhhNet.I != null) { _r["voice_status"] = ShhhNet.I.VoiceStatus; _r["voice_remote_voices"] = ShhhNet.I.RemoteVoices; _r["voice_frames_decoded"] = ShhhNet.I.VoiceFramesDecoded; }
            if (ShhhNet.I != null) { _r["genies_looks_known"] = ShhhNet.I.LooksKnown; _r["my_look_chars"] = (Settings.Current.GeniesDefinition ?? "").Length; }
            _r["seconds"] = Time.realtimeSinceStartup - _t0; _r["utc"] = DateTime.UtcNow.ToString("o");
            var sb = new StringBuilder("{\n");
            foreach (var kv in _r) sb.Append($"  \"{kv.Key}\": {Json(kv.Value)},\n");
            sb.Append("  \"log\": [\n"); for (int i = 0; i < _log.Count; i++) sb.Append("    ").Append(Json(_log[i])).Append(i < _log.Count - 1 ? ",\n" : "\n");
            sb.Append("  ]\n}\n");
            try { File.WriteAllText(_out, sb.ToString()); } catch (Exception e) { Debug.LogException(e); }
            Debug.Log("[SHHH][netauto] wrote " + _out);
            if (!Application.isEditor) StartCoroutine(QuitSoon());
        }

        IEnumerator QuitSoon() { yield return new WaitForSecondsRealtime(2f); Application.Quit(); }

        static string Json(object v)
        {
            if (v is bool b) return b ? "true" : "false";
            if (v is int || v is long || v is float || v is double) return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
            return "\"" + (v?.ToString() ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ").Replace("\r", "") + "\"";
        }
    }
}
