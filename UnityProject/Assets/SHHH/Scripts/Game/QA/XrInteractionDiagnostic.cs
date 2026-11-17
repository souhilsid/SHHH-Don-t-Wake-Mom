#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V6 XR-G1 offline interaction harness (Editor only, started by the `xr_diag` automation command).
    /// Drives two scripted <see cref="SimHandSource"/> hands through the REAL gameplay stack - the real Boot rig, the real
    /// <see cref="HandInteractor"/>, <see cref="PropBody"/>, <see cref="AnchorFixture"/>, <see cref="PlayerRig"/> and the
    /// real practice <see cref="RoundController"/> in the Practice room - and records every attempt with its cause.
    /// Evidence: Automation/xr_diag_result.json (+ a copy in QA_Evidence). Hardware-unverified: real OVR pinch noise,
    /// tracking latency and finger pose fidelity can only be measured on a Quest.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class XrInteractionDiagnostic : MonoBehaviour
    {
        public static bool Active;
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation"));
        static string Flag => Path.Combine(Dir, "xrdiag.flag");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Active = File.Exists(Flag);
            if (!Active) return;
            var go = new GameObject("XrInteractionDiagnostic"); DontDestroyOnLoad(go); go.AddComponent<XrInteractionDiagnostic>();
        }

        sealed class Row { public string group, name, detail; public bool pass; public float value; }
        readonly List<Row> _rows = new List<Row>();
        readonly List<string> _log = new List<string>();
        SimHandSource _l, _r; HandInteractor _hl, _hr; PlayerRig _rig;
        Vector3 _headLocal = new Vector3(0, 1.6f, 0); Quaternion _headRot = Quaternion.identity;
        int _noise; float _t0;
        public static readonly List<System.Func<XrInteractionDiagnostic, IEnumerator>> ExtraSuites = new List<System.Func<XrInteractionDiagnostic, IEnumerator>>();

        void Start() { _t0 = Time.realtimeSinceStartup; StartCoroutine(Run()); }

        void LateUpdate()
        {
            if (_rig != null && _rig.Head != null) { _rig.Head.localPosition = _headLocal; _rig.Head.localRotation = _headRot; }
            if (Time.realtimeSinceStartup - _t0 > 420f) { Note("ABORT: harness timeout"); Finish(); }
        }

        public void Record(string group, string name, bool pass, string detail, float value = 0f)
        {
            _rows.Add(new Row { group = group, name = name, pass = pass, detail = detail, value = value });
            Note($"{(pass ? "PASS" : "FAIL")} [{group}] {name}: {detail}");
        }

        public void Note(string s) { _log.Add($"{Time.realtimeSinceStartup - _t0:0.00}s {s}"); Debug.Log("[SHHH][xr_diag] " + s); }

        // ------------------------------------------------------------------ setup
        IEnumerator Run()
        {
            Note("waiting for GameRoot front end");
            float w = 0; while ((GameRoot.I == null || GameRoot.I.State != AppState.FrontEnd) && w < 60f) { w += Time.unscaledDeltaTime; yield return null; }
            if (GameRoot.I == null) { Note("ABORT: no GameRoot"); Finish(); yield break; }
            _rig = GameRoot.I.Rig;
            SetupSimulatedXr();
            yield return null; yield return null;
            yield return SafeRun(UiSuite());
            // UI suites run in the front end (menu board) before entering the practice room.
            foreach (var s in ExtraSuites) { IEnumerator e = null; try { e = s(this); } catch (System.Exception ex) { Note("suite error " + ex.Message); } if (e != null) yield return SafeRun(e); }
            GameRoot.I.StartPractice();
            w = 0; while ((GameRoot.I.State != AppState.Practice || GameServices.PracticeRound == null || !GameServices.PracticeRound.IsPlayable) && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
            if (GameServices.PracticeRound == null) { Note("ABORT: practice round did not start"); Finish(); yield break; }
            GameServices.PracticeRound.Pets.Clear(); // pets would wander into the test bench; they are covered by the match autopilot
            GameServices.PracticeRound.NoiseRegistered += (e, r) => _noise++;
            if (_rig.TeleportMode) _rig.TeleportMode = false;
            _rig.Spawn(new Vector3(-1.2f, 0, -2.0f), 0f);
            yield return new WaitForSeconds(1.0f);
            Note($"head at {_rig.Head.position}, props {CountProps()}");
            yield return SafeRun(NearGrabSuite());
            yield return SafeRun(FastCarry());
            yield return SafeRun(MagicPull());
            yield return SafeRun(Placement(10));
            yield return SafeRun(TrackingLossShort());
            yield return SafeRun(TrackingLossLong());
            yield return SafeRun(Throw());
            yield return SafeRun(TwoHands());
            yield return SafeRun(Locomotion());
            yield return SafeRun(ReachReport());
            Finish();
        }

        IEnumerator SafeRun(IEnumerator e)
        {
            while (true)
            {
                object cur;
                try { if (!e.MoveNext()) yield break; cur = e.Current; }
                catch (System.Exception ex) { Record("harness", "exception", false, ex.GetType().Name + ": " + ex.Message); ReleaseAll(); yield break; }
                yield return cur;
            }
        }

        int CountProps() { int n = 0; if (GameServices.Room != null) foreach (var _ in GameServices.Room.Props) n++; return n; }

        void SetupSimulatedXr()
        {
            var ovrRig = _rig.GetComponent<OVRCameraRig>(); if (ovrRig != null) ovrRig.enabled = false; // head pose is scripted
            _l = new GameObject("SimHand_L").AddComponent<SimHandSource>(); _l.Hand = Handedness.Left;
            _r = new GameObject("SimHand_R").AddComponent<SimHandSource>(); _r.Hand = Handedness.Right;
            // Tracked hands live in the rig's tracking space (they travel with locomotion, like real OVR hand anchors).
            var ts = _rig.TrackingSpace != null ? _rig.TrackingSpace : _rig.transform;
            _l.transform.SetParent(ts, true); _r.transform.SetParent(ts, true);
            _hl = _rig.LeftHand; _hr = _rig.RightHand;
            _hl.Source = _l; _hr.Source = _r; _rig.Left = _l; _rig.Right = _r;
            PokeButton.Pokers.Clear(); PokeButton.Pokers.Add(_l); PokeButton.Pokers.Add(_r);
            if (UiPointer.I != null) { UiPointer.I.Rebind(0, _l, _hl); UiPointer.I.Rebind(1, _r, _hr); }
            ParkHands();
            Note("simulated XR: OVRCameraRig pose disabled, sim hands bound to the real HandInteractors / PlayerRig / PokeButton");
        }

        public PlayerRig Rig => _rig;
        public SimHandSource Left => _l; public SimHandSource Right => _r;
        public HandInteractor LeftHi => _hl; public HandInteractor RightHi => _hr;
        public void SetHead(Vector3 local, float pitch = 0f) { _headLocal = local; _headRot = Quaternion.Euler(pitch, 0, 0); }

        public void ParkHands()
        {
            var h = _rig.Head;
            Place(_l, h.position + h.rotation * new Vector3(-0.25f, -0.55f, 0.15f), h.forward);
            Place(_r, h.position + h.rotation * new Vector3(0.25f, -0.55f, 0.15f), h.forward);
            _l.simPinch = _r.simPinch = 0f; _l.simGrip = _r.simGrip = 0f; _l.simTracked = _r.simTracked = true;
            _l.palmNormal = _r.palmNormal = Vector3.down;
        }

        static void Place(SimHandSource h, Vector3 p, Vector3 fwd)
        {
            fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            h.transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd.normalized, Vector3.up));
        }

        void ReleaseAll()
        {
            if (_l != null) _l.simPinch = 0; if (_r != null) _r.simPinch = 0;
            if (_hl != null && _hl.Held != null) _hl.Release(); if (_hr != null && _hr.Held != null) _hr.Release();
        }

        // ------------------------------------------------------------------ motion helpers
        public static IEnumerator Move(SimHandSource h, Vector3 to, float speed, System.Action<float> perFrame = null)
        {
            Vector3 from = h.transform.position; float T = Mathf.Max(0.05f, Vector3.Distance(from, to) / Mathf.Max(0.01f, speed)), t = 0;
            while (t < T) { t += Time.deltaTime; float k = Mathf.Clamp01(t / T); h.transform.position = Vector3.Lerp(from, to, k); perFrame?.Invoke(k); yield return null; }
            h.transform.position = to; perFrame?.Invoke(1f);
        }

        static IEnumerator Ramp(SimHandSource h, float to, float seconds)
        {
            float from = h.simPinch, t = 0;
            while (t < seconds) { t += Time.deltaTime; h.simPinch = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)); yield return null; }
            h.simPinch = to;
        }

        static IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.deltaTime; yield return null; } }

        PropBody Find(string kind)
        {
            if (GameServices.Room == null) return null;
            foreach (var p in GameServices.Room.Props) if (p.Spec.Kind == kind) return p;
            return null;
        }

        static Vector3 TopPoint(PropBody p) { var b = p.Col.bounds; return new Vector3(b.center.x, b.max.y - 0.01f, b.center.z); }

        /// <summary>Puts the tested item AND every displaced neighbour back on its spawn (deterministic bench between attempts).</summary>
        IEnumerator Reset(PropBody p)
        {
            ReleaseAll(); yield return null;
            foreach (var q in GameServices.Room.Props) if (q != p && !q.IsHeld && (q.transform.position - q.SpawnPos).sqrMagnitude > 0.0004f) q.Respawn();
            p.Respawn(); yield return Wait(0.7f);
        }

        /// <summary>One realistic grab: approach from above/front at 0.5 m/s, close the pinch around arrival (lead seconds early), wait for the hold.</summary>
        IEnumerator ApproachAndPinch(SimHandSource h, HandInteractor hi, Vector3 target, float lead, float speed = 0.5f)
        {
            var start = target + new Vector3(0, 0.22f, -0.2f);
            Place(h, start, target - _rig.Head.position); h.simPinch = 0f; h.palmNormal = Vector3.down;
            yield return Wait(0.15f);
            float T = Vector3.Distance(start, target) / speed; float rampAt = Mathf.Max(0f, T - lead - 0.15f); bool ramping = false; float t = 0;
            yield return Move(h, target, speed, k =>
            {
                t = k * T;
                if (!ramping && t >= rampAt) ramping = true;
                if (ramping) h.simPinch = Mathf.Clamp01(h.simPinch + Time.deltaTime / 0.15f);
            });
            while (h.simPinch < 1f) { h.simPinch = Mathf.Clamp01(h.simPinch + Time.deltaTime / 0.15f); yield return null; }
            float w = 0; while (hi.Held == null && w < 0.5f) { w += Time.deltaTime; yield return null; }
        }


        // ------------------------------------------------------------------ UI (UX-G2): poke + ray-pinch, standing and seated
        static List<PokeButton> ButtonsUnder(Transform root)
        {
            var l = new List<PokeButton>(); if (root == null) return l;
            foreach (var b in root.GetComponentsInChildren<PokeButton>()) if (b.isActiveAndEnabled) l.Add(b);
            return l;
        }

        IEnumerator Poke(SimHandSource h, PokeButton b)
        {
            var f = b.transform.forward; // into the board
            var tipStart = b.transform.TransformPoint(new Vector3(0, 0, -0.012f)) - f * 0.1f;
            var tipEnd = b.transform.TransformPoint(new Vector3(0, 0, -0.012f)) + f * 0.02f;
            h.transform.rotation = Quaternion.LookRotation(f, Vector3.up); h.simPinch = 0f; h.palmNormal = Vector3.down;
            h.transform.position = tipStart - f * 0.03f;
            yield return Wait(0.1f);
            yield return Move(h, tipEnd - f * 0.03f, 0.3f);
            yield return Wait(0.08f);
            yield return Move(h, tipStart - f * 0.03f, 0.4f);
        }

        IEnumerator RayPinch(SimHandSource h, PokeButton b, Vector3 handPos)
        {
            h.simPinch = 0f; h.transform.position = handPos;
            h.transform.rotation = Quaternion.LookRotation((b.transform.position - handPos).normalized, Vector3.up);
            yield return Wait(0.2f);
            bool hovered = b.RayHovered;
            yield return Ramp(h, 1f, 0.08f); yield return Wait(0.12f);
            yield return Ramp(h, 0f, 0.08f); yield return Wait(0.15f);
            _lastRayHover = hovered;
        }
        bool _lastRayHover;

        IEnumerator TestBoard(string boardName, Transform root, string posture, bool swapActions)
        {
            var buttons = ButtonsUnder(root);
            if (buttons.Count == 0) { Record("ui", boardName + "/" + posture, false, "no buttons found"); yield break; }
            var head = _rig.Head.position;
            int pokeOk = 0, rayOk = 0; float maxReach = 0f, minH = 9f, maxH = 0f;
            foreach (var b in buttons)
            {
                var orig = b.OnPress; if (swapActions) b.OnPress = () => { };
                // reach: from the matching shoulder (head - 25 cm, +-18 cm sideways) to the button face
                var right = _rig.Head.right; right.y = 0; right.Normalize();
                var side = Vector3.Dot(b.transform.position - head, right) >= 0 ? 1f : -1f;
                var shoulder = head + Vector3.down * 0.25f + right * 0.18f * side;
                float reach = Vector3.Distance(shoulder, b.transform.position);
                maxReach = Mathf.Max(maxReach, reach); minH = Mathf.Min(minH, b.transform.position.y); maxH = Mathf.Max(maxH, b.transform.position.y);
                int c0 = b.PressCount;
                yield return Poke(_r, b);
                bool poked = b.PressCount == c0 + 1;
                yield return Wait(0.5f); // cooldown
                int c1 = b.PressCount;
                yield return RayPinch(_r, b, shoulder + (b.transform.position - shoulder).normalized * 0.3f);
                bool rayed = b.PressCount == c1 + 1;
                if (poked) pokeOk++; if (rayed) rayOk++;
                Record("ui", $"{boardName}/{posture}/{b.Label}", poked && rayed, $"poke={poked}, ray hover={_lastRayHover} press={rayed}, shoulder reach {reach:0.00} m, height {b.transform.position.y:0.00} m");
                if (swapActions) b.OnPress = orig;
                yield return Wait(0.5f);
            }
            Record("ui", $"{boardName}/{posture}/SUMMARY", pokeOk == buttons.Count && rayOk == buttons.Count && maxReach <= 0.68f,
                $"poke {pokeOk}/{buttons.Count}, ray-pinch {rayOk}/{buttons.Count}, max shoulder reach {maxReach:0.00} m (comfort <= 0.68), button heights {minH:0.00}-{maxH:0.00} m", maxReach);
            ParkHands();
        }

        IEnumerator UiSuite()
        {
            foreach (var posture in new[] { "standing", "seated" })
            {
                SetHead(new Vector3(0, posture == "seated" ? 1.15f : 1.62f, 0));
                yield return Wait(0.1f);
                MenuBuilder.BringMenuToPlayer();
                yield return Wait(0.3f);
                yield return TestBoard("main", MenuBuilder.FrontEndBoard, posture, true);
                // Real flow: open Customize with the actual button, use it, close it with its Done button.
                PokeButton Find(Transform r, string label) { foreach (var b in ButtonsUnder(r)) if (b.Label == label) return b; return null; }
                var cust = Find(MenuBuilder.FrontEndBoard, "Customize");
                if (cust != null)
                {
                    yield return Poke(_r, cust); yield return Wait(0.4f);
                    bool opened = MenuBuilder.CurrentPopup != null && MenuBuilder.CurrentPopup.name == "Customize";
                    Record("ui", $"flow/{posture}/open_customize", opened, opened ? "Customize board opened in reach" : "popup did not open");
                    if (opened)
                    {
                        yield return TestBoard("customize", MenuBuilder.CurrentPopup, posture, true);
                        var done = Find(MenuBuilder.CurrentPopup, "Done");
                        if (done != null) { yield return Poke(_r, done); yield return Wait(0.4f); }
                        Record("ui", $"flow/{posture}/close_customize", MenuBuilder.CurrentPopup == null && MenuBuilder.FrontEndBoard.gameObject.activeSelf, "Done returns to the main board");
                    }
                }
                var set = Find(MenuBuilder.FrontEndBoard, "Settings");
                if (set != null)
                {
                    yield return RayPinch(_r, set, _rig.Head.position + new Vector3(0.2f, -0.35f, 0.25f)); yield return Wait(0.4f);
                    bool opened = MenuBuilder.CurrentPopup != null && MenuBuilder.CurrentPopup.name == "Settings";
                    Record("ui", $"flow/{posture}/open_settings_by_ray", opened, opened ? "Settings opened with a ray-pinch" : "popup did not open");
                    if (opened)
                    {
                        yield return TestBoard("settings", MenuBuilder.CurrentPopup, posture, true);
                        var done = Find(MenuBuilder.CurrentPopup, "Done");
                        if (done != null) { yield return RayPinch(_r, done, _rig.Head.position + new Vector3(0.2f, -0.35f, 0.25f)); yield return Wait(0.4f); }
                        Record("ui", $"flow/{posture}/close_settings_by_ray", MenuBuilder.CurrentPopup == null, "Done (ray) returns to the main board");
                    }
                }
            }
            SetHead(new Vector3(0, 1.6f, 0));
            yield return Wait(0.1f);
            MenuBuilder.BringMenuToPlayer();
        }

        // ------------------------------------------------------------------ suites
        IEnumerator NearGrabSuite()
        {
            string[] kinds = { "book", "cup", "plate", "toy_car", "cushion", "fork", "sock", "dog_toy", "pan" };
            int ok = 0, n = 0;
            foreach (var kind in kinds)
            {
                var p = Find(kind); if (p == null) { Record("near_grab", kind, false, "prop missing from practice room"); continue; }
                var ext = p.Col.bounds.extents;
                var variants = new (string name, Vector3 off, float lead, bool flicker)[]
                {
                    ("centre", Vector3.zero, 0f, false),
                    ("edge", new Vector3(ext.x * 0.8f, 0, 0), 0f, false),
                    ("hover4cm", new Vector3(0, 0.04f, 0), 0f, false),
                    ("early_pinch_0.35s", new Vector3(0, 0, -ext.z * 0.5f), 0.35f, false),
                    ("late_pinch", Vector3.zero, -0.12f, false),
                    ("ovr_flicker_carry", Vector3.zero, 0f, true),
                };
                foreach (var v in variants)
                {
                    yield return Reset(p);
                    var tgt = TopPoint(p) + v.off;
                    int noise0 = _noise;
                    yield return ApproachAndPinch(_r, _hr, tgt, v.lead);
                    bool grabbed = _hr.Held == p; string why = grabbed ? "" : (_hr.Held != null ? "grabbed other " + _hr.Held.Spec.Kind : _hr.LastGrabFailure);
                    bool phantom = false; float maxErr = 0f; bool released = false;
                    if (grabbed && v.name == "centre") yield return GripCheck(kind, p);
                    if (grabbed)
                    {
                        Vector3 off0 = p.Body.position - _r.transform.position;
                        var rnd = new System.Random(n);
                        yield return Move(_r, tgt + new Vector3(0.25f, 0.15f, 0.1f), 0.7f, k =>
                        {
                            if (v.flicker) _r.simPinch = rnd.NextDouble() < 0.18 ? 0.3f : 0.88f; // OVR pinch-strength dropouts mid-carry
                            if (_hr.Held != p) phantom = true;
                            maxErr = Mathf.Max(maxErr, ((p.Body.position - _r.transform.position) - off0).magnitude);
                        });
                        _r.simPinch = 1f; yield return Wait(0.25f);
                        if (_hr.Held != p) phantom = true;
                        yield return Move(_r, tgt + new Vector3(0.25f, 0.03f, 0.1f), 0.4f); // lower near the table before letting go
                        _r.simPinch = 0f; yield return Wait(0.35f);
                        released = _hr.Held == null && !p.IsHeld && !p.Body.isKinematic;
                    }
                    bool pass = grabbed && !phantom && released && maxErr < 0.12f;
                    n++; if (pass) ok++;
                    Record("near_grab", $"{kind}/{v.name}", pass, grabbed ? $"held, follow err {maxErr * 100f:0.0} cm, phantom drop={phantom}, released={released}, noise events={_noise - noise0}" : "NOT grabbed: " + why, maxErr);
                }
            }
            Record("near_grab", "TOTAL", ok >= Mathf.CeilToInt(n * 0.9f), $"{ok}/{n} grab-hold-move-release cycles (gate >= 90%)", ok);
        }

        // V7 Gate 1: thumb/index opposition on the held prop (visual SimpleHands pose) + 3-angle close-ups
        IEnumerator GripCheck(string kind, PropBody p)
        {
            yield return Wait(0.2f);
            var vis = _hr.GetComponentInChildren<SimpleHandVisual>(true);
            if (vis == null) { Record("grip", kind, false, "no SimpleHandVisual on the right hand"); yield break; }
            var g = vis.LastGrip;
            bool pass = g.Active && g.Opposed && g.ThumbGap > -0.004f && g.IndexGap > -0.004f && g.ThumbGap < 0.025f && g.IndexGap < 0.025f && g.FingersInside == 0;
            Record("grip", kind, pass, g.Active
                ? $"opposed={g.Opposed} axis={"xyz"[g.Axis]} thickness {g.Thickness * 100f:0.0} cm, thumb gap {g.ThumbGap * 100f:0.0} cm, index gap {g.IndexGap * 100f:0.0} cm, other fingertips inside={g.FingersInside}, hand scale {vis.ModelScale:0.00}"
                : "grip solve inactive", g.ThumbGap);
            if (kind == "book" || kind == "cup" || kind == "plate" || kind == "pan" || kind == "toy_car")
            {
                var ev = Path.GetFullPath(Path.Combine(Dir, "..", "QA_Evidence", "grip_v7")); Directory.CreateDirectory(ev);
                var centre = (vis.ThumbTipVisual + vis.IndexTipVisual) * 0.5f; // frame the grip, not the prop centre
                var views = new (string n, Vector3 dir)[] { ("front", _r.transform.rotation * new Vector3(0, 0.15f, -1f)), ("side", _r.transform.rotation * new Vector3(1f, 0.2f, 0)), ("top", new Vector3(0.2f, 1f, -0.3f)) };
                foreach (var view in views) Shot(Path.Combine(ev, $"{kind}_{view.n}.png"), centre + view.dir.normalized * 0.24f, centre);
            }
        }

        static void Shot(string path, Vector3 eye, Vector3 at)
        {
            var go = new GameObject("GripShotCam"); var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at - eye)); cam.fieldOfView = 50f; cam.nearClipPlane = 0.02f;
            var rt = new RenderTexture(960, 720, 24) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(960, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = prev; cam.targetTexture = null; Object.Destroy(tex); rt.Release(); Object.Destroy(rt); Object.Destroy(go);
        }

        IEnumerator FastCarry()
        {
            var p = Find("book"); if (p == null) yield break;
            yield return Reset(p);
            var tgt = TopPoint(p);
            yield return ApproachAndPinch(_r, _hr, tgt, 0f);
            if (_hr.Held != p) { Record("carry", "fast_2.0mps", false, "could not grab: " + _hr.LastGrabFailure); yield break; }
            bool drop = false;
            yield return Move(_r, tgt + new Vector3(0.9f, 0.2f, 0.2f), 2.0f, k => { if (_hr.Held != p) drop = true; });
            yield return Move(_r, tgt + new Vector3(-0.2f, 0.25f, 0.1f), 2.4f, k => { if (_hr.Held != p) drop = true; });
            Record("carry", "fast_2.0-2.4mps", !drop, drop ? "auto-released during a fast swing" : "kept hold through 2.0 and 2.4 m/s swings");
            yield return Move(_r, tgt + Vector3.up * 0.03f, 0.5f); _r.simPinch = 0; yield return Wait(0.4f);
            yield return Reset(p);
        }

        IEnumerator MagicPull()
        {
            foreach (var kind in new[] { "pan", "cushion" })
            {
                var p = Find(kind); if (p == null) continue;
                yield return Reset(p);
                var shoulder = _rig.Head.position + Vector3.down * 0.25f;
                var dir = (p.Col.bounds.center - shoulder).normalized;
                Place(_r, shoulder + dir * 0.4f, dir); _r.simPinch = 0f; _r.palmNormal = Vector3.down;
                yield return Wait(0.35f);
                bool aimed = _hr.HoverTarget == p && _hr.FarHover;
                float dist = Vector3.Distance(p.Col.bounds.center, _r.transform.position);
                yield return Ramp(_r, 1f, 0.15f);
                yield return Wait(0.6f);
                float arrive = Vector3.Distance(p.Col.bounds.center, _r.transform.position);
                bool pulled = _hr.Held == p && arrive < 0.15f;
                Record("magic_pull", kind, aimed && pulled, $"target {dist:0.00} m away, aimed={aimed}, held={(_hr.Held ? _hr.Held.Spec.Kind : "none")}, centre-to-fingers {arrive:0.00} m, attempts={_hr.GrabAttempts}, last failure='{_hr.LastGrabFailure}'", dist);
                yield return Move(_r, new Vector3(-1.2f, 0.82f, -1.35f), 0.6f);
                _r.simPinch = 0; yield return Wait(0.4f);
                yield return Reset(p);
            }
        }

        IEnumerator Placement(int reps)
        {
            AnchorFixture anchor = null; PropBody prop = null;
            foreach (var a in FindObjectsByType<AnchorFixture>(FindObjectsSortMode.None))
            {
                if (!a.Active) continue;
                foreach (var p in GameServices.Room.Props)
                    if (p.Spec.Kind == a.Spec.AcceptsPropKind && GameServices.PracticeRound.Board.ChoreForProp(p.Spec.Id) != null && a.Spec.Position.y > 0.3f) { anchor = a; prop = p; break; }
                if (anchor != null) break;
            }
            if (anchor == null) { Record("placement", "setup", false, "no active elevated chore anchor in practice room"); yield break; }
            Note($"placement target: {prop.Spec.Kind} -> {anchor.Spec.Kind} at {anchor.transform.position}");
            yield return Reset(prop);
            int ok = 0;
            for (int i = 0; i < reps; i++)
            {
                yield return ApproachAndPinch(_r, _hr, TopPoint(prop), 0.1f);
                if (_hr.Held != prop) { Record("placement", "rep" + (i + 1), false, "grab failed: " + _hr.LastGrabFailure); yield return Reset(prop); continue; }
                var hold = _r.transform.position - prop.Body.position;
                var desired = anchor.PlacePoint + Vector3.up * 0.035f + new Vector3((i % 3 - 1) * 0.03f, 0, ((i / 3) % 2) * 0.03f); // imprecise drops
                yield return Move(_r, desired + hold + Vector3.up * 0.1f, 0.6f);
                yield return Move(_r, desired + hold, 0.3f);
                yield return Wait(0.2f);
                _r.simPinch = 0f;
                float w = 0; while (anchor.Occupant != prop && w < 2.5f) { w += Time.deltaTime; yield return null; }
                var step = GameServices.PracticeRound.Board.ChoreForProp(prop.Spec.Id)?.StepForProp(prop.Spec.Id);
                bool pass = anchor.Occupant == prop && step != null && step.Done;
                if (pass) ok++;
                Record("placement", "rep" + (i + 1), pass, pass ? $"snapped + step validated in {w:0.00} s (offset {(desired - anchor.PlacePoint).magnitude * 100f:0.0} cm)" : $"not validated after {w:0.0} s; occupant={(anchor.Occupant ? anchor.Occupant.Spec.Kind : "none")}, step done={(step != null && step.Done)}, held={prop.IsHeld}, reject='{GameServices.PracticeRound.Board.LastRejectReason}', prop at {prop.transform.position.ToString("F3")}, anchor {anchor.transform.position.ToString("F3")}");
                yield return Move(_r, _r.transform.position + new Vector3(0, 0.15f, -0.15f), 0.6f);
            }
            Record("placement", "TOTAL", ok == reps, $"{ok}/{reps} socket placements validated (gate 10/10)", ok);
            yield return Reset(prop);
        }

        IEnumerator TrackingLossShort()
        {
            var p = Find("cup"); if (p == null) yield break;
            yield return Reset(p);
            var tgt = TopPoint(p);
            yield return ApproachAndPinch(_r, _hr, tgt, 0f);
            if (_hr.Held != p) { Record("tracking_loss", "short_0.5s", false, "could not grab"); yield break; }
            yield return Move(_r, tgt + Vector3.up * 0.25f, 0.5f);
            yield return Wait(0.2f);
            var before = p.Body.position; var handBefore = _r.transform.position; int n0 = _noise;
            _r.simTracked = false;
            yield return Move(_r, _r.transform.position + new Vector3(0.12f, -0.05f, 0), 0.4f); // hand keeps moving while untracked
            yield return Wait(0.3f);
            bool frozen = p.Body.isKinematic && (p.Body.position - before).magnitude < 0.02f && _hr.Held == p;
            _r.transform.position = handBefore; // hand comes back to the frozen item
            _r.simTracked = true; _r.simPinch = 1f;
            yield return Wait(0.25f);
            bool resumed = _hr.Held == p && !p.Body.isKinematic;
            Record("tracking_loss", "short_0.5s", frozen && resumed && _noise == n0, $"frozen in place={frozen}, resumed carry={resumed}, noise events during loss={_noise - n0}");
            yield return Move(_r, tgt + Vector3.up * 0.03f, 0.4f); _r.simPinch = 0; yield return Wait(0.4f);
            yield return Reset(p);
        }

        IEnumerator TrackingLossLong()
        {
            var p = Find("plate"); if (p == null) yield break;
            yield return Reset(p);
            var tgt = TopPoint(p);
            yield return ApproachAndPinch(_r, _hr, tgt, 0f);
            if (_hr.Held != p) { Record("tracking_loss", "long_7s", false, "could not grab"); yield break; }
            yield return Move(_r, tgt + Vector3.up * 0.35f, 0.5f);
            int n0 = _noise; float y0 = tgt.y;
            _r.simTracked = false;
            yield return Wait(7.6f);
            bool dropped = _hr.Held == null && !p.IsHeld && !p.Body.isKinematic;
            bool safe = Mathf.Abs(p.Col.bounds.min.y - (y0 - (p.Col.bounds.size.y - 0.01f))) < 0.08f || p.Col.bounds.min.y > 0.6f;
            Record("tracking_loss", "long_7s_soft_drop", dropped && safe && _noise == n0, $"lowered gently onto the surface={dropped && safe} (bottom y={p.Col.bounds.min.y:0.00}), noise events={_noise - n0} (must be 0: no fake blame)");
            _r.simTracked = true; yield return Wait(0.3f);
            yield return Reset(p);
        }

        IEnumerator Throw()
        {
            var p = Find("dog_toy"); if (p == null) yield break;
            yield return Reset(p);
            var tgt = TopPoint(p);
            yield return ApproachAndPinch(_r, _hr, tgt, 0f);
            if (_hr.Held != p) { Record("throw", "release_velocity", false, "could not grab"); yield break; }
            yield return Move(_r, tgt + new Vector3(0, 0.2f, -0.1f), 0.5f);
            bool released = false;
            var from = _r.transform.position;
            yield return Move(_r, from + new Vector3(0.6f, 0.25f, 0.9f), 3.2f, k => { if (k > 0.75f && !released) { released = true; _r.simPinch = 0f; } });
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            float v = p.Body.linearVelocity.magnitude;
            bool pass = _hr.Held == null && p.LastReleaseWasThrow && v > 1.5f && v <= HandInteractor.MaxThrowSpeed + 0.5f;
            Record("throw", "release_velocity", pass, $"release speed {v:0.00} m/s (hand ~3.2 m/s, clamp {HandInteractor.MaxThrowSpeed}), flagged as throw={p.LastReleaseWasThrow}", v);
            yield return Wait(1.5f);
            yield return Reset(p);
        }

        IEnumerator TwoHands()
        {
            var p = Find("cushion"); if (p == null) yield break;
            yield return Reset(p);
            var b = p.Col.bounds;
            var rt = new Vector3(b.max.x - 0.03f, b.max.y - 0.01f, b.center.z);
            var lt = new Vector3(b.min.x + 0.03f, b.max.y - 0.01f, b.center.z);
            yield return ApproachAndPinch(_r, _hr, rt, 0f);
            yield return ApproachAndPinch(_l, _hl, lt, 0f);
            bool both = _hr.Held == p && _hl.Held == p && p.HoldCount == 2;
            var lUp = _l.transform.position + Vector3.up * 0.2f; var rUp = _r.transform.position + Vector3.up * 0.2f;
            yield return Move(_r, rUp, 0.4f, k => _l.transform.position = Vector3.Lerp(lUp - Vector3.up * 0.2f, lUp, k));
            float lift = p.Col.bounds.center.y - b.center.y;
            _r.simPinch = 0f; yield return Wait(0.3f);
            bool kept = _hl.Held == p && p.IsHeld;
            Record("two_hand", "cushion", both && kept && lift > 0.1f, $"both hands holding={both} (HoldCount {p.HoldCount}), lifted {lift * 100f:0} cm, one-hand release keeps it in the other hand={kept}");
            yield return Move(_l, lt + Vector3.up * 0.03f, 0.4f); _l.simPinch = 0f; yield return Wait(0.4f);
            yield return Reset(p);
            ParkHands();
        }

        IEnumerator Locomotion()
        {
            // 1) Carrying an item with the palm up and the pinch held must never move the player.
            var p = Find("book");
            yield return Reset(p);
            yield return ApproachAndPinch(_r, _hr, TopPoint(p), 0f);
            var rig0 = _rig.transform.position;
            _r.palmNormal = Vector3.up;
            yield return Move(_r, _r.transform.position + new Vector3(0.12f, 0.1f, 0.05f), 0.08f);
            yield return Wait(0.5f);
            float moved1 = (_rig.transform.position - rig0).magnitude;
            Record("locomotion", "no_move_while_carrying", _hr.Held == p && moved1 < 0.01f, $"rig moved {moved1 * 100f:0.0} cm while carrying {p.Spec.Kind} palm-up with a held pinch");
            _r.palmNormal = Vector3.down; yield return Move(_r, TopPoint(p) + Vector3.up * 0.03f, 0.5f); _r.simPinch = 0; yield return Wait(0.4f);
            yield return Reset(p);
            // 2) A normal palm-down pinch in the air never moves the player.
            ParkHands(); rig0 = _rig.transform.position;
            var air = _rig.Head.position + _rig.Head.forward * 0.35f + Vector3.down * 0.1f;
            Place(_r, air, _rig.Head.forward); _r.simPinch = 1f;
            yield return Wait(0.6f);
            yield return Move(_r, air + new Vector3(0, 0, 0.1f), 0.2f);
            float moved2 = (_rig.transform.position - rig0).magnitude;
            Record("locomotion", "no_move_palm_down_pinch", moved2 < 0.01f, $"rig moved {moved2 * 100f:0.0} cm");
            _r.simPinch = 0f; yield return Wait(0.3f);
            // 3) Control: the deliberate gesture (V13: empty hand, finger gun cocked with the thumb up, then the thumb held down) moves the player.
            rig0 = _rig.transform.position; bool tm = _rig.TeleportMode, hrh = _rig.HopRightHand; _rig.TeleportMode = false; _rig.HopRightHand = false;
            Place(_r, air, _rig.Head.forward); _r.simGrip = 0.9f; _r.simGun = true; _r.simThumbDown = false;
            yield return Wait(0.4f);
            _r.simThumbDown = true; yield return Wait(0.6f);
            float moved3 = (_rig.transform.position - rig0).magnitude;
            _r.simThumbDown = false; _r.simGun = false; _r.simGrip = 0f; _rig.TeleportMode = tm; _rig.HopRightHand = hrh; yield return Wait(0.2f);
            Record("locomotion", "deliberate_gesture_moves", moved3 > 0.1f, $"rig moved {moved3 * 100f:0} cm with the deliberate finger-gun gesture (thumb held down 0.6 s)");
            _rig.Spawn(new Vector3(-1.2f, 0, -2.0f), 0f);
            yield return Wait(0.3f);
            ParkHands();
        }

        /// <summary>Numerical reach audit of every chore anchor and prop spawn in the three match rooms (comfort: 0.6-1.4 m, no floor reaching).</summary>
        IEnumerator ReachReport()
        {
            foreach (var id in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                var def = RoomCatalog.Get(id);
                int low = 0, n = 0; var lows = new List<string>();
                foreach (var pr in def.Props) { n++; if (pr.Spawn.y < 0.4f && pr.MassKg < 1.0f) { low++; lows.Add(pr.Kind); } }
                Record("reach", def.Title + " prop spawns", low <= 1, $"{n - low}/{n} grabbable props spawn at >= 0.4 m (low: {string.Join(",", lows)})", low);
                int alow = 0; var al = new List<string>();
                foreach (var a in def.Anchors) if (a.Position.y < 0.4f && a.Radius < 0.25f) { alow++; al.Add(a.Kind); }
                Record("reach", def.Title + " targets", true, $"{def.Anchors.Count - alow}/{def.Anchors.Count} chore targets at >= 0.4 m or large drop zones (low small targets: {string.Join(",", al)})", alow);
            }
            yield break;
        }

        // ------------------------------------------------------------------ output
        bool _finished;
        void Finish()
        {
            if (_finished) return; _finished = true;
            int pass = 0, fail = 0; foreach (var r in _rows) if (r.pass) pass++; else fail++;
            var sb = new StringBuilder();
            sb.Append("{\n  \"harness\": \"xr_diag (real HandInteractor/PropBody/AnchorFixture/PlayerRig/PokeButton driven by SimHandSource)\",\n");
            sb.Append($"  \"utc\": \"{System.DateTime.UtcNow:o}\",\n  \"pass\": {pass},\n  \"fail\": {fail},\n  \"hardware\": \"hardware-unverified (no Quest during development)\",\n  \"rows\": [\n");
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                sb.Append($"    {{\"group\": \"{Esc(r.group)}\", \"name\": \"{Esc(r.name)}\", \"pass\": {(r.pass ? "true" : "false")}, \"value\": {r.value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, \"detail\": \"{Esc(r.detail)}\"}}{(i < _rows.Count - 1 ? "," : "")}\n");
            }
            sb.Append("  ],\n  \"log\": [\n");
            for (int i = 0; i < _log.Count; i++) sb.Append($"    \"{Esc(_log[i])}\"{(i < _log.Count - 1 ? "," : "")}\n");
            sb.Append("  ]\n}\n");
            try
            {
                File.WriteAllText(Path.Combine(Dir, "xr_diag_result.json"), sb.ToString());
                var ev = Path.GetFullPath(Path.Combine(Dir, "..", "QA_Evidence")); Directory.CreateDirectory(ev);
                File.WriteAllText(Path.Combine(ev, $"xr_diag_{System.DateTime.UtcNow:yyyyMMdd_HHmm}.json"), sb.ToString());
                File.Delete(Flag);
            }
            catch (System.Exception e) { Debug.LogException(e); }
            Debug.Log($"[SHHH][xr_diag] finished: {pass} pass, {fail} fail");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
    }
}
#endif
