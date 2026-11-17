#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Genies
{
    /// <summary>
    /// `genies_journey &lt;tag&gt;` (V8 Gate B acceptance, desktop): the real game flow with the real Genies SDK -
    /// front end -> Family Stage shows Genies siblings -> WARDROBE -> try on hair / top / bottoms / colours through the same code path
    /// a poke uses -> Save -> back to the stage (picked seat wears the saved look) -> relaunch-equivalent reload of the saved
    /// definition -> Solo match: bots are Genies starter looks -> results podium. Screenshots of the XR camera (desktop render,
    /// headset UNVERIFIED) + rows in QA_Evidence/genies/&lt;tag&gt;/journey.json.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class GeniesWardrobeQa : MonoBehaviour
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static string Flag => Root + "/Automation/geniesjourney.flag";
        readonly List<string> _rows = new List<string>(); string _dir; Camera _cam; PlayerRig _rig; Vector3 _hp; Quaternion _hr; bool _hold, _done; float _t0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() { if (!File.Exists(Flag)) return; var go = new GameObject("QA_GeniesJourney"); DontDestroyOnLoad(go); go.AddComponent<GeniesWardrobeQa>(); }

        void Row(string step, bool ok, string detail) { _rows.Add($"{{\"step\":\"{E(step)}\",\"pass\":{(ok ? "true" : "false")},\"detail\":\"{E(detail)}\"}}"); Debug.Log($"[SHHH][genies_journey] {(ok ? "PASS" : "FAIL")} {step}: {detail}"); }
        static string E(string s) => (s ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");

        void Start() { _t0 = Time.realtimeSinceStartup; var tag = File.ReadAllText(Flag).Trim(); _dir = Path.Combine(Root, "QA_Evidence", "genies", string.IsNullOrEmpty(tag) ? "journey" : tag); Directory.CreateDirectory(_dir); StartCoroutine(Run()); }
        void Update() { if (!_done && Time.realtimeSinceStartup - _t0 > 420f) { Row("timeout", false, "420 s"); Finish(); } }
        void LateUpdate() { if (_hold && _rig != null && _rig.Head != null) _rig.Head.SetPositionAndRotation(_hp, _hr); }
        void Look(Vector3 from, Vector3 at) { _hp = from; _hr = Quaternion.LookRotation(at - from, Vector3.up); _hold = true; if (_rig?.Head != null) _rig.Head.SetPositionAndRotation(_hp, _hr); }

        void Shot(string n)
        {
            if (_cam == null) return;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 }; var prev = _cam.targetTexture; _cam.targetTexture = rt; _cam.Render(); RenderTexture.active = rt;
            var t = new Texture2D(1280, 720, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); t.Apply();
            File.WriteAllBytes(Path.Combine(_dir, n + ".png"), t.EncodeToPNG()); _cam.targetTexture = prev; RenderTexture.active = null; Destroy(t); rt.Release();
        }

        static IEnumerator Wait(Func<bool> cond, float timeout) { float w = 0; while (!cond() && w < timeout) { w += Time.unscaledDeltaTime; yield return null; } }
        static IEnumerator Await(UniTask task) { var t = task.ToCoroutine(); return t; }

        IEnumerator Run()
        {
            yield return Wait(() => GameRoot.I != null && GameRoot.I.State == AppState.FrontEnd, 60f);
            var game = GameRoot.I; _rig = game.Rig; var ovr = _rig.GetComponent<OVRCameraRig>(); if (ovr) ovr.enabled = false;
            _cam = _rig.Head.GetComponent<Camera>() ?? Camera.main;
            var be = GeniesBackend.I;
            yield return Wait(() => be != null && be.Ready, 60f);
            bool li = global::Genies.Sdk.AvatarSdk.IsLoggedIn;
            Row("backend_ready", be != null && be.Ready, be?.Status + " loggedIn=" + li);
            yield return Wait(() => be.PresetDefinition(0) != null && be.PresetDefinition(3) != null, 120f);
            int presets = 0; for (int i = 0; i < 4; i++) if (!string.IsNullOrEmpty(be.PresetDefinition(i))) presets++;
            Row("starter_presets", presets == 4, presets + "/4 real Genies definitions (Pip, Nell, Rory, Tess)");
            // family stage with Genies bodies
            var eye = MenuBuilder.FrontEndSpawn + Vector3.up * 1.55f;
            if (FamilyStage.I != null) FamilyStage.I.Rebuild();
            yield return new WaitForSeconds(1f);
            yield return Wait(() => FamilyStage.I != null && CountGenies(FamilyStage.I.transform) >= 4, 60f);
            Row("stage_genies", FamilyStage.I != null && CountGenies(FamilyStage.I.transform) >= 4, "Genies bodies on the Family Stage: " + (FamilyStage.I != null ? CountGenies(FamilyStage.I.transform) : -1));
            if (FamilyStage.I != null) { Look(eye, FamilyStage.I.transform.position + Vector3.up * 0.8f); yield return null; yield return null; Shot("01_family_stage_genies"); }
            _hold = false; _rig.Spawn(MenuBuilder.FrontEndSpawn, MenuBuilder.FrontEndYaw); yield return null;

            // wardrobe
            Settings.Current.GeniesDefinition = ""; // start from the seat preset so the change is visible
            MenuBuilder.OpenAvatarWardrobe(game);
            yield return Wait(() => GeniesWardrobe.Current != null && GeniesWardrobe.Current.AvatarReady, 60f);
            var w = GeniesWardrobe.Current;
            Row("wardrobe_open", w != null && w.AvatarReady, w != null ? "avatar loaded on the turntable" : "wardrobe not opened");
            if (w == null) { Finish(); yield break; }
            var he = _rig.Head.position; var hf = Vector3.ProjectOnPlane(_rig.Head.forward, Vector3.up).normalized;
            Look(he, he + hf * 2f + Vector3.down * 0.55f); yield return null; Shot("02_wardrobe_open");
            string d0 = w.CurrentDefinition;
            foreach (var (cat, idx, name) in new[] { (1, 6, "hair"), (5, 3, "top"), (6, 2, "bottoms"), (7, 1, "shoes"), (2, 4, "hair colour"), (3, 5, "skin"), (8, 2, "hat") })
            {
                yield return Await(w.QaApply(cat, idx));
                Row("try_on_" + name.Replace(' ', '_'), w.ListCount(cat) > idx, $"category list {w.ListCount(cat)} items, applied #{idx}");
            }
            string d1 = w.CurrentDefinition;
            Row("definition_changed", !string.IsNullOrEmpty(d1) && d1 != d0, $"{d0?.Length} -> {d1?.Length} chars");
            yield return new WaitForSeconds(1.2f); Look(he, he + hf * 2f + Vector3.down * 0.55f); yield return null; Shot("03_wardrobe_dressed");
            Look(he, he + hf * 1.4f + Vector3.down * 0.25f + Vector3.Cross(Vector3.up, hf) * -0.4f); yield return null; Shot("04_wardrobe_closet_board");
            yield return Await(w.QaSave());
            Row("save", Settings.Current.GeniesDefinition == d1, $"Settings.GeniesDefinition {Settings.Current.GeniesDefinition?.Length} chars, matches live avatar: {Settings.Current.GeniesDefinition == d1}");
            _hold = false; w.Close(); yield return new WaitForSeconds(0.5f);
            // relaunch-equivalent: settings round-trip through PlayerPrefs JSON, then the stage reloads the saved definition
            Settings.Save(); var saved = Settings.Current.GeniesDefinition; Settings.Current.GeniesDefinition = ""; Settings.Load();
            Row("persist_roundtrip", Settings.Current.GeniesDefinition == saved, "PlayerPrefs JSON round-trip");
            yield return Wait(() => FamilyStage.I != null && CountGenies(FamilyStage.I.transform) >= 4, 60f);
            yield return new WaitForSeconds(2f);
            if (FamilyStage.I != null) { Look(eye, FamilyStage.I.transform.position + Vector3.up * 0.8f); yield return null; Shot("05_stage_after_save"); }
            _hold = false;

            // match: bots are Genies starter looks
            Settings.Current.SkipIntro = true; game.StartSolo();
            yield return Wait(() => GameServices.Round != null && GameServices.Round.Phase == RoundPhase.Playing, 90f);
            yield return Wait(() => GameServices.Room != null && CountGenies(GameServices.Room.transform) >= 3, 60f);
            int n = GameServices.Room != null ? CountGenies(GameServices.Room.transform) : -1;
            Row("match_bots_genies", n >= 3, "Genies bodies in the house: " + n);
            yield return new WaitForSeconds(3f);
            int k = 0;
            foreach (var av in GameServices.Room.GetComponentsInChildren<SiblingAvatar>())
            {
                if (!av.HasGeniesBody || k >= 3) continue;
                var p = av.transform.position; var from = p + av.transform.forward * 1.6f + Vector3.up * 1.3f;
                Look(from, p + Vector3.up * 0.75f); yield return null; yield return null; Shot($"06_bot_{k}_{av.name}"); k++;
            }
            _hold = false;
            GameServices.Round?.ForceResolve(RoundEndCause.TimerExpired);
            yield return Wait(() => game.State == AppState.Results, 30f);
            yield return Wait(() => GameServices.Room != null && CountGenies(GameServices.Room.transform) >= 7, 30f);
            yield return new WaitForSeconds(2.5f);
            var eyeR = _rig.Head.position; var fr = Vector3.ProjectOnPlane(_rig.Head.forward, Vector3.up).normalized;
            Look(eyeR, eyeR + fr * 2.4f + Vector3.down * 0.4f); yield return null; Shot("07_results_podium");
            Finish();
        }

        static int CountGenies(Transform t) { int n = 0; foreach (var a in t.GetComponentsInChildren<SiblingAvatar>(true)) if (a.HasGeniesBody) n++; return n; }

        void Finish()
        {
            if (_done) return; _done = true;
            File.WriteAllText(Path.Combine(_dir, "journey.json"), "{\n \"environment\": \"Unity Editor play mode, Windows PC, real Genies SDK 3.8.6 + live Genies session; desktop render of the XR camera, NO Quest\",\n \"utc\": \"" + DateTime.UtcNow.ToString("O") + "\",\n \"rows\": [\n  " + string.Join(",\n  ", _rows) + "\n ]\n}\n");
            try { File.Delete(Flag); } catch { }
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
