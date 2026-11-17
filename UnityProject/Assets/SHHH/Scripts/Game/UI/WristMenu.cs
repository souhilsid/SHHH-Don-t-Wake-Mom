using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V8 (2026-11-09) wrist HUD on the LEFT hand, shown only while the palm faces the eyes (never head-locked, never a permanent
    /// panel): a clock dial with the one shared match clock (final minute turns coral and pulses), Mom's sleep meter with her state
    /// sticker, your score, your jobs (room icon, progress, done / messed-with), and a row of icon keys: MAP, turn left/right,
    /// walk/hop, recenter, music, menu (+ mic when online). MAP opens the personal house map above the wrist (<see cref="HouseMapView"/>).
    /// </summary>
    public sealed class WristMenu : MonoBehaviour
    {
        public PlayerRig Rig;
        public HandSource Hand;
        Transform _panel, _hud;
        TextMeshPro _clock, _momText, _score;
        Transform _meterFill; Renderer _meterR; GameObject _momZ, _momFace, _momAlert;
        MeshFilter _ring; float _ringK = -1f; Renderer _ringR;
        PokeButton _tpBtn, _musicBtn, _micBtn, _mapBtn;
        readonly List<(GameObject root, TextMeshPro name, TextMeshPro state, GameObject done, GameObject alert, Renderer roomIcon)> _jobs = new List<(GameObject, TextMeshPro, TextMeshPro, GameObject, GameObject, Renderer)>();
        float _vis; bool _mapOpen; bool _open; float _dwell, _closeT;
        /// <summary>QA: the watch is open (deliberate-look rule).</summary>
        public bool IsOpen => _open;
        HouseMapView _map;
        public System.Action OnMenuPressed;
        const float W = 0.3f, H = 0.25f;

        public void Build()
        {
            _panel = new GameObject("wristPanel").transform;
            _panel.SetParent(transform, false);
            UiKit.Panel(_panel, "bg", new Vector3(0, 0, 0.004f), new Vector2(W, H));
            _hud = new GameObject("hud").transform; _hud.SetParent(_panel, false);
            // clock dial (left)
            var dial = new GameObject("dial").transform; dial.SetParent(_hud, false); dial.localPosition = new Vector3(-0.088f, 0.052f, -0.006f);
            UiKit.Quad(dial, "face", UiKit.Cutout(UiKit.Tex("plate_timer")), Vector3.zero, new Vector2(0.115f, 0.115f));
            var ring = new GameObject("ring"); ring.transform.SetParent(dial, false); ring.transform.localPosition = new Vector3(0, 0, -0.002f); ring.layer = Layers.UI;
            _ring = ring.AddComponent<MeshFilter>(); _ringR = ring.AddComponent<MeshRenderer>(); _ringR.sharedMaterial = Mats.Unlit(UiKit.Hush);
            _ringR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _clock = UiKit.Text(dial, "5:00", new Vector3(0, -0.008f, -0.004f), 0.32f, UiKit.SoftWhite, UiKit.Display, 0.09f);
            // Mom (right): sticker + meter + state text
            var mom = new GameObject("mom").transform; mom.SetParent(_hud, false); mom.localPosition = new Vector3(0.055f, 0.075f, -0.006f);
            _momZ = UiKit.Icon(mom, "zzz", new Vector3(-0.055f, 0, 0), 0.04f);
            _momFace = UiKit.Icon(mom, "mom", new Vector3(-0.055f, 0, 0), 0.04f);
            _momAlert = UiKit.Icon(mom, "alert", new Vector3(-0.055f, 0, 0), 0.04f);
            UiKit.Quad(mom, "meterFrame", UiKit.Cutout(UiKit.Tex("plate_meter")), new Vector3(0.025f, 0.0f, 0.001f), new Vector2(0.12f, 0.0225f));
            _meterFill = UiKit.SlabObject(mom, "meterFill", new Vector3(-0.0335f, 0f, -0.002f), 0.1f, 0.009f, 0.0045f, 0.004f, 0.0015f, Mats.Unlit(UiKit.Mint)).transform;
            _meterR = _meterFill.GetComponent<Renderer>();
            _momText = UiKit.Text(mom, "Mom is asleep", new Vector3(0.025f, -0.022f, -0.002f), 0.15f, UiKit.MoonCream, UiKit.Body, 0.13f, TextAlignmentOptions.Center, 0.1f);
            // score
            var sc = new GameObject("score").transform; sc.SetParent(_hud, false); sc.localPosition = new Vector3(0.055f, 0.022f, -0.006f);
            UiKit.Icon(sc, "star", new Vector3(-0.055f, 0, 0), 0.032f);
            _score = UiKit.Text(sc, "0 pts", new Vector3(0.02f, 0, -0.002f), 0.24f, UiKit.Hush, UiKit.Display, 0.12f, TextAlignmentOptions.Left);
            // jobs (up to 3 rows)
            for (int i = 0; i < 3; i++)
            {
                var row = new GameObject("job" + i); row.transform.SetParent(_hud, false); row.transform.localPosition = new Vector3(0, -0.026f - i * 0.03f, -0.006f);
                var ic = UiKit.Icon(row.transform, "room_living", new Vector3(-0.128f, 0, 0), 0.026f);
                var nm = UiKit.Text(row.transform, "", new Vector3(-0.035f, 0, -0.002f), 0.16f, UiKit.SoftWhite, UiKit.Body, 0.16f, TextAlignmentOptions.Left, 0.1f);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                var stt = UiKit.Text(row.transform, "", new Vector3(0.105f, 0, -0.002f), 0.17f, UiKit.MoonCream, UiKit.Display, 0.05f, TextAlignmentOptions.Right, 0.12f);
                var done = UiKit.Icon(row.transform, "check", new Vector3(0.122f, 0, -0.001f), 0.026f);
                var alert = UiKit.Icon(row.transform, "sabotage", new Vector3(0.122f, 0, -0.001f), 0.026f);
                _jobs.Add((row, nm, stt, done, alert, ic.GetComponent<Renderer>()));
            }
            // icon keys
            var ks = new Vector2(0.034f, 0.034f); float ky = -0.104f;
            _mapBtn = PokeButton.Create(_panel, "", new Vector3(-0.123f, ky, 0), ks, UiKit.Hush, ToggleMap, 0.1f, "map");
            PokeButton.Create(_panel, "", new Vector3(-0.082f, ky, 0), ks, UiKit.Teal, () => Rig.SnapTurn(-1), 0.1f, "back");
            PokeButton.Create(_panel, "", new Vector3(-0.041f, ky, 0), ks, UiKit.Teal, () => Rig.SnapTurn(1), 0.1f, "play");
            _tpBtn = PokeButton.Create(_panel, "", new Vector3(0f, ky, 0), ks, UiKit.MoonCream, ToggleTeleport, 0.1f, "hand");
            PokeButton.Create(_panel, "", new Vector3(0.041f, ky, 0), ks, UiKit.MoonCream, () => Rig.Recenter(), 0.1f, "home");
            _musicBtn = PokeButton.Create(_panel, "", new Vector3(0.082f, ky, 0), ks, UiKit.MoonCream, ToggleMusic, 0.1f, Settings.Current.MusicOn ? "noise" : "quiet");
            PokeButton.Create(_panel, "", new Vector3(0.123f, ky, 0), ks, UiKit.Coral, () => OnMenuPressed?.Invoke(), 0.1f, "settings");
            _micBtn = PokeButton.Create(_panel, "", new Vector3(0.132f, 0.112f, 0), new Vector2(0.03f, 0.03f), UiKit.Teal, ToggleMic, 0.1f, "mic");
            foreach (var b in _panel.GetComponentsInChildren<PokeButton>(true)) b.RaySource = Hand;
            // personal house map (hidden until MAP)
            // the map page replaces the HUD rows on the same board (key row stays), so it is never off-screen above the wrist
            _map = HouseMapView.Build(_panel, new Vector3(0, 0.022f, -0.006f));
            _map.transform.localScale = Vector3.one * 0.78f;
            _map.gameObject.SetActive(false);
            // V12 owner 2026-11-12: my arms and hands never hide the watch
            TaskHud.DrawOnTop(_panel);
            _panel.gameObject.SetActive(false);
        }

        void ToggleMap() { _mapOpen = !_mapOpen; if (_map) _map.gameObject.SetActive(_mapOpen); if (_hud) _hud.gameObject.SetActive(!_mapOpen); _mapBtn.SetSelected(_mapOpen); AudioBank.I?.Play2D(_mapOpen ? "card_show" : "ui_click", 0.5f, AudioBus.UI); }

        /// <summary>QA (ui_capture): open the house map page as if the player pressed MAP.</summary>
        public void QaOpenMap() { if (!_mapOpen) ToggleMap(); }

        void ToggleTeleport()
        {
            string name = PlayerRig.CycleLocomotion(Settings.Current); Rig.ApplyLocomotion(Settings.Current); Settings.Save();
            string how = Settings.Current.TeleportMode ? "finger gun with either hand: point at the floor, drop your thumb" : Settings.Current.HopRightHand ? "finger gun: left hand walks (thumb held down), right hand hops (drop the thumb)" : "finger gun: point, hold your thumb down to walk";
            RoomFx.Say(_panel.position + Vector3.up * 0.08f, name + "\n<size=60%>" + how + "</size>", UiKit.MoonCream, 2.5f, 0.25f);
        }

        void ToggleMic()
        {
            var net = GameServices.Net; if (net == null || !net.Active) return;
            net.SetMic(!net.MicOn);
            _micBtn.SetColor(net.MicOn ? UiKit.Teal : UiKit.Coral);
        }

        void ToggleMusic()
        {
            Settings.Current.MusicOn = !Settings.Current.MusicOn; Settings.Apply(); Settings.Save();
            _musicBtn.SetColor(Settings.Current.MusicOn ? UiKit.MoonCream : UiKit.Coral);
        }

        void LateUpdate()
        {
            if (_micBtn != null) { bool on = GameServices.Net != null && GameServices.Net.Active; if (_micBtn.gameObject.activeSelf != on) _micBtn.gameObject.SetActive(on); }
            if (Hand == null || Rig == null || Rig.Head == null) return;
            bool show = false;
            if (Hand.Tracked)
            {
                // V12 (Quest run: the watch popped open whenever the left palm turned up and covered the work): open only on a deliberate
                // look - palm toward the eyes AND the wrist near the centre of the view, held 0.35 s; once open it stays while used
                // (looser thresholds, so tapping it with the other hand does not flicker it shut)
                Vector3 toHead = (Rig.Head.position - Hand.PalmCenter).normalized;
                float facing = Vector3.Dot(Hand.PalmNormal, toHead);
                float gaze = Vector3.Angle(Rig.Head.forward, Hand.PalmCenter - Rig.Head.position);
                bool busy = (Rig.LeftHand != null && (Rig.LeftHand.IsGrabbing || Rig.LeftHand.IsHovering)) || (Rig.RightHand != null && Rig.RightHand.IsGrabbing);
                bool wants = !busy && (_open ? facing > 0.6f && gaze < 40f : facing > 0.85f && gaze < 25f);
                if (wants) _dwell += Time.deltaTime; else _dwell = 0f;
                if (!_open && _dwell > 0.35f) _open = true;
                if (_open && !wants) { _closeT += Time.deltaTime; if (_closeT > 0.15f) { _open = false; _closeT = 0f; } } else _closeT = 0f;
                show = _open;
            }
            else { _open = false; _dwell = 0f; }
            _vis = Mathf.MoveTowards(_vis, show ? 1f : 0f, Time.deltaTime * 6f);
            bool vis = _vis > 0.05f;
            if (_panel.gameObject.activeSelf != vis) _panel.gameObject.SetActive(vis);
            if (!vis) return;
            // float just above the wrist, facing the head
            Vector3 pos = Hand.PalmCenter + Hand.PalmNormal * 0.05f + Vector3.up * 0.06f;
            _panel.position = pos;
            var look = pos - Rig.Head.position; look.y *= 0.5f;
            _panel.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            _panel.localScale = Vector3.one * (0.8f * Mathf.SmoothStep(0.6f, 1f, _vis));   // 20 % smaller than c13: it covered the table
            Refresh();
        }

        void Refresh()
        {
            var r = GameServices.Round;
            if (r == null)
            {
                _clock.text = "--"; SetRing(1f, UiKit.Purple); _momText.text = "Mom is asleep"; _score.text = "";
                foreach (var j in _jobs) j.root.SetActive(false);
                Mom(0); return;
            }
            float left = r.TimeRemaining; int t = Mathf.CeilToInt(left);
            _clock.text = $"{t / 60}:{t % 60:00}";
            float dur = Mathf.Max(1f, r.PlayTime + left);
            bool final = left <= 60f;
            SetRing(left / dur, final ? (Mathf.Repeat(Time.time, 1f) < 0.5f ? UiKit.Coral : UiKit.Pink) : UiKit.Hush);
            _clock.color = final ? UiKit.Coral : UiKit.SoftWhite;
            // Mom
            float k = Mathf.Clamp01(r.Meter.Value / 100f);
            _meterFill.localScale = new Vector3(Mathf.Max(0.02f, k), 1f, 1f);
            _meterFill.localPosition = new Vector3(-0.0335f + 0.05f * (Mathf.Max(0.02f, k) - 1f) + 0.05f, 0f, -0.002f) - new Vector3(0.0f, 0, 0);
            _meterR.sharedMaterial = TaskHud.Top(Mats.Unlit(k < 0.35f ? UiKit.Mint : k < 0.65f ? UiKit.Hush : k < 0.85f ? UiKit.Coral : UiKit.RibbonRed));
            // V12 (Quest run: the watch said "MOM IS UP!" for a whole minute while she was only stirring in bed - Stirring /
            // Suspicious / AlmostAwake are in-bed states): up = the visit (waking .. back to bed)
            bool awake = r.Mom.RoundFrozen;
            int st = awake ? 2 : r.Meter.Stage == SleepStage.DeepSleep ? 0 : r.Meter.Stage == SleepStage.Stirring ? 1 : 2;
            Mom(st);
            _momText.text = awake ? (r.Mom.State == MomState.Interrogating ? "Mom is asking!" : r.Mom.State == MomState.ReturningToBed ? "Back to bed..." : "MOM IS UP!") : r.Mom.State == MomState.AlmostAwake ? "Almost awake!" : st == 0 ? "Mom is asleep" : st == 1 ? "Mom is stirring..." : "Mom is suspicious!";
            // score + jobs
            int me = GameServices.LocalSlot;
            if (me >= 0 && me < 4) _score.text = $"{Mathf.RoundToInt(r.Scores[me].Total)} pts{(r.IsTimedOut(me) ? "  <size=70%><color=#F57F78>in bed!</color></size>" : "")}";
            int row = 0;
            if (r.Recipes != null)
                foreach (var inst in r.Recipes.Instances)
                {
                    if (inst.Owner != me || row >= _jobs.Count) continue;
                    var j = _jobs[row++]; j.root.SetActive(true);
                    j.name.text = inst.Def.DisplayName;
                    var pos = FixturePos(r, inst.Index);
                    var room = HouseLayout.RoomAt(pos);
                    j.roomIcon.sharedMaterial = TaskHud.Top(UiKit.Cutout(UiKit.Tex(RoomIcon(room))));
                    bool messed = false; foreach (var sb in r.Recipes.Sabotage) if (sb.Instance == inst.Index && !sb.Foiled && !inst.Complete) messed = true;
                    j.done.SetActive(inst.Complete); j.alert.SetActive(messed);
                    j.state.text = inst.Complete || messed ? "" : $"{inst.StepsDone}/{inst.Satisfied.Length}";
                    j.state.transform.localPosition = new Vector3(0.1f, 0, -0.002f);
                }
            for (; row < _jobs.Count; row++) _jobs[row].root.SetActive(false);
        }

        public static Vec3 FixturePos(RoundController r, int instance)
        {
            foreach (var f in r.RecipeFixtures) if (f.Instance == instance) return f.Position;
            return new Vec3(0, 0, 0);
        }

        public static string RoomIcon(RoomId room) => room == RoomId.Kitchen ? "icon_room_kitchen" : room == RoomId.Laundry ? "icon_room_laundry" : "icon_room_living";

        void Mom(int st) { if (_momZ.activeSelf != (st == 0)) _momZ.SetActive(st == 0); if (_momFace.activeSelf != (st == 1)) _momFace.SetActive(st == 1); if (_momAlert.activeSelf != (st == 2)) _momAlert.SetActive(st == 2); }

        void SetRing(float k, Color c)
        {
            k = Mathf.Clamp01(k);
            _ringR.sharedMaterial = Mats.Unlit(c);
            if (Mathf.Abs(k - _ringK) < 0.004f) return; _ringK = k;
            // annulus arc from 12 o'clock, clockwise, radius 0.047..0.054
            const int seg = 48; float r0 = 0.047f, r1 = 0.054f;
            int n = Mathf.Max(1, Mathf.CeilToInt(seg * k));
            var v = new Vector3[(n + 1) * 2]; var tri = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.PI / 2f - (i / (float)n) * k * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
                if (i < n) { int b = i * 2, o = i * 6; tri[o] = b; tri[o + 1] = b + 1; tri[o + 2] = b + 3; tri[o + 3] = b; tri[o + 4] = b + 3; tri[o + 5] = b + 2; }
            }
            var m = _ring.sharedMesh; if (m == null) { m = new Mesh { name = "clock_ring" }; m.MarkDynamic(); _ring.sharedMesh = m; }
            m.Clear(); m.vertices = v; m.triangles = tri; m.RecalculateBounds();
        }
    }
}
