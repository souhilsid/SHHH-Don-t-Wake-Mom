#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// `ui_capture &lt;tag&gt;` (2026-11-09 Gate 3): screenshots of the real running game for UI A/B review - front end, every popup,
    /// the family stage, the match spawn, the wrist HUD (simulated left palm turned to the face), the house map, and a survey of
    /// every room's walls/windows. Desktop render of the XR camera (1280x720, mono); headset look UNVERIFIED.
    /// Output: QA_Evidence/ui_v8/&lt;tag&gt;/*.png + index.json.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class UiCapture : MonoBehaviour
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static string Flag => Root + "/Automation/uicapture.flag";
        string _dir; readonly List<string> _shots = new List<string>(); readonly List<string> _notes = new List<string>();
        PlayerRig _rig; Camera _cam; SimHandSource _l, _r; Vector3 _headPos; Quaternion _headRot; bool _hold, _done; float _t0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() { if (!File.Exists(Flag)) return; var go = new GameObject("QA_UiCapture"); DontDestroyOnLoad(go); go.AddComponent<UiCapture>(); }

        void Start()
        {
            _t0 = Time.realtimeSinceStartup;
            var tag = File.ReadAllText(Flag).Trim(); if (string.IsNullOrEmpty(tag)) tag = "capture";
            _dir = Path.Combine(Root, "QA_Evidence", "ui_v8", tag); Directory.CreateDirectory(_dir);
            StartCoroutine(Safe(Run()));
        }
        void Update() { if (!_done && Time.realtimeSinceStartup - _t0 > 300f) { _notes.Add("timeout"); Finish(); } }
        void LateUpdate() { if (_hold && _rig && _rig.Head) _rig.Head.SetPositionAndRotation(_headPos, _headRot); }

        IEnumerator Safe(IEnumerator e)
        {
            while (true)
            {
                object cur;
                try { if (!e.MoveNext()) yield break; cur = e.Current; }
                catch (Exception ex) { _notes.Add("EXCEPTION " + ex); Debug.LogException(ex); Finish(); yield break; }
                yield return cur;
            }
        }

        void Head(Vector3 p, float yaw, float pitch = 8f) { _headPos = p; _headRot = Quaternion.Euler(pitch, yaw, 0); _hold = true; if (_rig && _rig.Head) _rig.Head.SetPositionAndRotation(_headPos, _headRot); }
        void Look(Vector3 from, Vector3 at) { _headPos = from; _headRot = Quaternion.LookRotation(at - from, Vector3.up); _hold = true; if (_rig && _rig.Head) _rig.Head.SetPositionAndRotation(_headPos, _headRot); }

        IEnumerator Frames(int n = 3) { for (int i = 0; i < n; i++) yield return null; }

        void Shot(string name)
        {
            if (_cam == null) return;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 }; var prev = _cam.targetTexture; var act = RenderTexture.active;
            _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            _cam.targetTexture = prev; RenderTexture.active = act; Destroy(tex); rt.Release(); Destroy(rt);
            _shots.Add(name);
        }

        IEnumerator Run()
        {
            float wait = 0; while ((GameRoot.I == null || GameRoot.I.State != AppState.FrontEnd) && wait < 60) { wait += Time.deltaTime; yield return null; }
            var game = GameRoot.I; _rig = game.Rig;
            var ovr = _rig.GetComponent<OVRCameraRig>(); if (ovr) ovr.enabled = false;
            _l = new GameObject("QA_Sim_Left").AddComponent<SimHandSource>(); _l.Hand = Handedness.Left;
            _r = new GameObject("QA_Sim_Right").AddComponent<SimHandSource>(); _r.Hand = Handedness.Right;
            DontDestroyOnLoad(_l.gameObject); DontDestroyOnLoad(_r.gameObject); // scene swaps (front end -> house) must not destroy them
            _rig.Left = _l; _rig.Right = _r; _rig.LeftHand.Source = _l; _rig.RightHand.Source = _r;
            if (game.Wrist != null) game.Wrist.Hand = _l;
            _l.transform.position = new Vector3(0, -5, 0); _r.transform.position = new Vector3(0, -5, 0);
            _cam = _rig.Head.GetComponent<Camera>(); if (_cam == null) _cam = Camera.main;

            // ---- front end
            var eye = MenuBuilder.FrontEndSpawn + Vector3.up * 1.55f; float yaw0 = MenuBuilder.FrontEndYaw;
            Head(eye, yaw0, 6f); yield return Frames();
            MenuBuilder.BringMenuToPlayer(); yield return Frames(6); Shot("01_menu_spawn");
            Head(eye, yaw0, -14f); yield return Frames(4); Shot("01b_menu_title");
            Look(eye + (Quaternion.Euler(0, yaw0, 0) * new Vector3(0.3f, 0.15f, -1.1f)), eye + (Quaternion.Euler(0, yaw0, 0) * new Vector3(0, -0.1f, 3f))); yield return Frames(4); Shot("02_hall_wide");
            if (FamilyStage.I != null) { Look(eye, FamilyStage.I.transform.position + Vector3.up * 0.75f); yield return Frames(4); Shot("03_family_stage"); }
            Head(eye, yaw0, 6f); yield return Frames(); MenuBuilder.BringMenuToPlayer();
            var popups = new (string, Action)[] {
                ("04_setup", () => MenuBuilder.ShowMatchSetup(game)),
                ("05_online", () => MenuBuilder.ShowOnline(game)),
                ("06_customize", () => MenuBuilder.ShowCustomize(game)),
                ("07_wardrobe", () => MenuBuilder.ShowWardrobe(game)),
                ("08_settings", () => MenuBuilder.ShowSettings(game)),
                ("09_keypad", () => MenuBuilder.ShowKeypad(game)),
                ("09b_rules", () => MenuBuilder.ShowRules(game)) };
            foreach (var (n, a) in popups) { try { a(); } catch (Exception ex) { _notes.Add(n + ": " + ex.Message); } yield return Frames(5); Shot(n); MenuBuilder.ClosePopup(); yield return Frames(2); }
            // hall walls / windows
            for (int k = 0; k < 4; k++) { Head(eye, yaw0 + k * 90f, 0f); yield return Frames(3); Shot($"10_hall_wall_{k * 90}"); }

            // ---- match
            _hold = false;
            Settings.Current.SkipIntro = true;
            game.StartSolo(); wait = 0;
            while ((GameServices.Round == null || GameServices.Round.Phase != RoundPhase.Playing) && wait < 60) { wait += Time.deltaTime; MenuBuilder.ClosePopup(); GameServices.Match?.SkipIntro(); yield return null; }
            yield return Frames(10);
            var spawn = _rig.transform.position + Vector3.up * 1.55f;
            Head(spawn, _rig.transform.eulerAngles.y, 6f); yield return Frames(4); Shot("20_match_spawn");
            // wrist HUD: left palm turned to the face, 35 cm in front/below the eyes
            var fwd = _headRot * Vector3.forward; fwd.y = 0; fwd.Normalize();
            _l.transform.position = _headPos + fwd * 0.32f + Vector3.down * 0.22f + Vector3.Cross(Vector3.up, fwd) * -0.06f;
            _l.palmNormal = (_headPos - _l.PalmCenter).normalized; _l.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            yield return Frames(20);
            Look(_headPos, _l.PalmCenter + Vector3.up * 0.08f); yield return Frames(4); Shot("21_wrist_hud");
            if (game.Wrist != null) game.Wrist.QaOpenMap();
            yield return Frames(10); Shot("22_house_map");
            _l.transform.position = new Vector3(0, -5, 0); _l.palmNormal = Vector3.down;
            // room survey: every built room, four walls each, from the room centre
            foreach (var rid in HouseLayout.Rooms)
            {
                var o = HouseLayout.Offset(rid); var c = new Vector3(o.x, 1.55f, o.z);
                for (int k = 0; k < 4; k++) { Head(c, k * 90f, 2f); yield return Frames(3); Shot($"30_{rid}_{k * 90}"); }
            }
            // looking up through the living room toward the sky, and the outside from the kitchen window height
            Head(new Vector3(0, 1.6f, 0), 0f, -35f); yield return Frames(3); Shot("40_up");
            // floor-plan views from just under the ceiling (wide FOV) for layout review
            float fov = _cam.fieldOfView; _cam.fieldOfView = 100f;
            foreach (var rid in HouseLayout.Rooms) { var o = HouseLayout.Offset(rid); Look(new Vector3(o.x, 2.45f, o.z - 0.01f), new Vector3(o.x, 0f, o.z)); yield return Frames(3); Shot($"41_plan_{rid}"); }
            _cam.fieldOfView = fov;
            // the street outside: from the road looking at the house, and over the roofs
            var hc = GameServices.Room != null ? GameServices.Room.transform.position : Vector3.zero;
            Look(new Vector3(3.2f, 1.7f, -11f), new Vector3(3.2f, 2.5f, 3f)); yield return Frames(3); Shot("50_outside_south");
            Look(new Vector3(3.2f, 9f, -9f), new Vector3(3.2f, 0f, 6f)); yield return Frames(3); Shot("51_outside_high");
            // end the match now (QA) -> one results board + podium, seen from the player's spawn
            Head(spawn, _rig.transform.eulerAngles.y, 6f); yield return Frames(2);
            _hold = false;
            GameServices.Round?.ForceResolve(RoundEndCause.TimerExpired);
            wait = 0; while (game.State != AppState.Results && wait < 20) { wait += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(1.2f);
            var eyeR = _rig.Head.position; var fr = _rig.Head.forward; fr.y = 0; fr.Normalize();
            Head(eyeR, Mathf.Atan2(fr.x, fr.z) * Mathf.Rad2Deg, 8f); yield return Frames(6); Shot("60_results");
            Head(eyeR, Mathf.Atan2(fr.x, fr.z) * Mathf.Rad2Deg, -12f); yield return Frames(4); Shot("61_podium");
            _notes.Add($"skybox={(RenderSettings.skybox ? RenderSettings.skybox.name : "none")} clear={_cam.clearFlags} fogEnd={RenderSettings.fogEndDistance} exterior={(GameObject.Find("NightExterior") != null)}");
            Finish();
        }

        void Finish()
        {
            if (_done) return; _done = true;
            var sb = new System.Text.StringBuilder("{\n  \"hardware\": \"UNVERIFIED: desktop render of the XR camera (mono), no Quest\",\n  \"utc\": \"" + DateTime.UtcNow.ToString("O") + "\",\n  \"shots\": [");
            for (int i = 0; i < _shots.Count; i++) sb.Append((i > 0 ? ", " : "") + "\"" + _shots[i] + "\"");
            sb.Append("],\n  \"notes\": [");
            for (int i = 0; i < _notes.Count; i++) sb.Append((i > 0 ? ", " : "") + "\"" + _notes[i].Replace("\\", "/").Replace("\"", "'").Replace("\n", " ") + "\"");
            sb.Append("]\n}\n");
            File.WriteAllText(Path.Combine(_dir, "index.json"), sb.ToString());
            try { File.Delete(Flag); } catch { }
            Debug.Log("[SHHH][ui_capture] done: " + _shots.Count + " shots -> " + _dir);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
#endif
