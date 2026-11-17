using System.Collections.Generic;
using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V13 (owner, Quest c22: "for each interaction it should ghost or highlight"). Three layers of in-world guidance, presentation only
    /// (nothing here changes what a hand can do):
    /// <list type="bullet">
    /// <item><b>Highlight</b>: every operable part near one of my hands - door / drawer / lid handles (their whole grab line), knobs,
    /// buttons, the soap pump's pad, the sink lever - gets a soft teal outline that brightens as the hand comes closer, and turns yellow
    /// with its action word ("pull", "lift", "press", "turn") once the hand is close enough to take it.</item>
    /// <item><b>Next step</b>: what my current job needs next gets a bobbing arrow with the step's words, visible across the room.</item>
    /// <item><b>Ghost</b>: standing near that target for a few seconds without doing it, a see-through ghost hand shows the motion there
    /// (push the pad down, swing the door open, slide the drawer, move the lever, put the item in).</item>
    /// </list>
    /// </summary>
    public sealed class InteractionHints : MonoBehaviour
    {
        public PlayerRig Rig;
        public static bool On = true;
        /// <summary>QA: highlight outlines drawn this frame, the next-step label, ghost shown.</summary>
        public static int Shown { get; private set; }
        public static int ReadyShown { get; private set; }
        public static string NextLabel { get; private set; } = "";
        public static bool GhostOn { get; private set; }
        public static Vector3 NextAt { get; private set; }
        /// <summary>QA: why the ghost is / is not shown.</summary>
        public static string GhostWhy { get; private set; } = "";

        enum Motion { None, Press, Swing, Slide, Lever, Turn, Place, Pick }
        struct Cand
        {
            public Vector3 At, Span; public string Verb; public float Reach; public Motion Motion; public Object Key;
            public HingedPart Hinge; public SlidingPart Slider; public SinkTapView Tap; public Vector3 PlaceFrom; public bool Closing;
        }

        sealed class Marker { public GameObject Go; public LineRenderer Line; public TextMeshPro Label; }
        readonly List<Marker> _pool = new List<Marker>();
        readonly List<Cand> _cands = new List<Cand>();
        readonly List<(float d, Cand c)> _near = new List<(float, Cand)>();
        Material _soft, _ready, _ghostMat;
        Transform _arrow; LineRenderer _arrowLine; TextMeshPro _arrowLabel;
        Transform _ghost, _ghostFinger;
        float _nearSince = -1f; Object _nearKey;
        bool _built;

        void Build()
        {
            _built = true;
            _soft = Mats.Glow(new Color(0.35f, 0.95f, 0.90f, 0.55f));
            _ready = Mats.Glow(new Color(1.00f, 0.86f, 0.25f, 0.95f));
            _ghostMat = Mats.Glow(new Color(0.55f, 0.85f, 1.00f, 0.38f));
            // next-step arrow: a chevron + the step's words
            _arrow = new GameObject("nextStepArrow").transform; DontDestroyOnLoad(_arrow.gameObject);
            _arrowLine = _arrow.gameObject.AddComponent<LineRenderer>(); SetupLine(_arrowLine, _ready, 0.012f); _arrowLine.positionCount = 3; _arrowLine.useWorldSpace = false;
            _arrowLine.SetPositions(new[] { new Vector3(-0.04f, 0.05f, 0f), Vector3.zero, new Vector3(0.04f, 0.05f, 0f) });
            _arrowLabel = WorldText.Make(_arrow, "t", "", new Vector3(0f, 0.11f, 0f), 0.13f, Palette.Yellow, TextAlignmentOptions.Center, 0.45f);
            TaskHud.DrawOnTop(_arrow); _arrow.gameObject.SetActive(false);
            // ghost hand: a palm and a pointing finger (see-through)
            _ghost = new GameObject("ghostHand").transform; DontDestroyOnLoad(_ghost.gameObject);
            var palm = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(palm.GetComponent<Collider>()); palm.transform.SetParent(_ghost, false);
            palm.transform.localScale = new Vector3(0.075f, 0.024f, 0.085f); palm.GetComponent<Renderer>().sharedMaterial = _ghostMat;
            var f = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(f.GetComponent<Collider>()); f.transform.SetParent(_ghost, false);
            f.transform.localPosition = new Vector3(0.012f, 0f, 0.06f); f.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); f.transform.localScale = new Vector3(0.018f, 0.032f, 0.018f);
            f.GetComponent<Renderer>().sharedMaterial = _ghostMat; _ghostFinger = f.transform;
            var th = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(th.GetComponent<Collider>()); th.transform.SetParent(_ghost, false);
            th.transform.localPosition = new Vector3(0.045f, 0f, 0.015f); th.transform.localRotation = Quaternion.Euler(90f, 40f, 0f); th.transform.localScale = new Vector3(0.018f, 0.022f, 0.018f);
            th.GetComponent<Renderer>().sharedMaterial = _ghostMat;
            foreach (var r in _ghost.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ghost.gameObject.SetActive(false);
        }

        static void SetupLine(LineRenderer l, Material m, float w)
        {
            l.sharedMaterial = m; l.widthMultiplier = w; l.numCapVertices = 4; l.numCornerVertices = 2; l.alignment = LineAlignment.View;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
        }

        Marker Get(int i)
        {
            while (_pool.Count <= i)
            {
                var g = new GameObject("interactHint" + _pool.Count); DontDestroyOnLoad(g);
                var l = g.AddComponent<LineRenderer>(); SetupLine(l, _soft, 0.008f); l.useWorldSpace = true;
                var lab = WorldText.Make(g.transform, "t", "", Vector3.zero, 0.11f, Palette.Yellow, TextAlignmentOptions.Center, 0.25f);
                TaskHud.DrawOnTop(g.transform);
                _pool.Add(new Marker { Go = g, Line = l, Label = lab });
            }
            return _pool[i];
        }

        // ------------------------------------------------------------------ what can be operated
        void Collect()
        {
            _cands.Clear();
            foreach (var m in MechPart.All)
            {
                if (m == null || !m.isActiveAndEnabled || m.IsHeld) continue;
                var c = new Cand { At = m.HandleWorld, Span = m.transform.TransformVector(m.HandleSpanLocal), Reach = m.GrabRadius, Key = m };
                switch (m)
                {
                    case HingedPart h: c.Hinge = h; c.Motion = Motion.Swing; c.Verb = m.Locked ? "locked" : Mathf.Abs(h.AxisW.y) > 0.7f ? "pull" : "lift"; break;
                    case SlidingPart sp: c.Slider = sp; c.Motion = Motion.Slide; c.Verb = m.Locked ? "locked" : "pull"; break;
                    case RotaryKnob _: c.Motion = Motion.Turn; c.Verb = "turn"; break;
                    case ScrewCap _: c.Motion = Motion.Turn; c.Verb = "twist"; break;
                    default: c.Verb = "grab"; break;
                }
                _cands.Add(c);
            }
            foreach (var b in MechButton.All)
                if (b != null && b.isActiveAndEnabled) _cands.Add(new Cand { At = b.FaceWorld, Reach = b.Radius + 0.02f, Verb = "press", Motion = Motion.Press, Key = b });
            var rt = RecipeRuntime.I;
            if (rt != null)
                foreach (var fv in rt.Fixtures)
                {
                    if (fv == null) continue;
                    if (fv.Pump != null) _cands.Add(new Cand { At = fv.Pump.PadTop, Reach = 0.05f, Verb = "press", Motion = Motion.Press, Key = fv.Pump });
                    if (fv.Tap != null) _cands.Add(new Cand { At = fv.Tap.LeverTipAt(fv.Tap.Level), Reach = 0.06f, Verb = "move", Motion = Motion.Lever, Tap = fv.Tap, Key = fv.Tap, Closing = fv.Tap.Level > 0.5f });
                }
        }

        static float SegDist(Vector3 p, Vector3 a, Vector3 span)
        {
            if (span.sqrMagnitude < 1e-8f) return Vector3.Distance(p, a);
            var b0 = a - span; var ab = span * 2f; float t = Mathf.Clamp01(Vector3.Dot(p - b0, ab) / ab.sqrMagnitude);
            return Vector3.Distance(b0 + ab * t, p);
        }

        void LateUpdate()
        {
            if (Rig == null) return;
            if (!_built) Build();
            bool inPlay = On && (GameServices.Round != null || GameServices.PracticeMode) && Rig.LocomotionEnabled;
            Shown = 0; ReadyShown = 0;
            var cam = Camera.main;
            var pts = new List<Vector3>(6);
            foreach (var src in new[] { Rig.Left, Rig.Right })
                if (src != null && src.Tracked) { pts.Add(src.PinchPoint); pts.Add(src.IndexTip); pts.Add(src.PalmCenter); }
            int used = 0;
            if (inPlay && pts.Count > 0)
            {
                Collect();
                _near.Clear();
                foreach (var c in _cands)
                {
                    float d = float.MaxValue; foreach (var p in pts) d = Mathf.Min(d, SegDist(p, c.At, c.Span));
                    if (d < 0.45f) _near.Add((d, c));
                }
                _near.Sort((x, y) => x.d.CompareTo(y.d));
                for (int i = 0; i < _near.Count && used < 5; i++, used++) Draw(Get(used), _near[i].c, _near[i].d, cam);
            }
            for (int i = used; i < _pool.Count; i++) if (_pool[i].Go.activeSelf) _pool[i].Go.SetActive(false);
            Shown = used;
            NextStep(inPlay, pts, cam);
        }

        void Draw(Marker mk, Cand c, float d, Camera cam)
        {
            if (!mk.Go.activeSelf) mk.Go.SetActive(true);
            bool ready = d <= c.Reach + 0.03f && c.Verb != "locked";
            if (ready) ReadyShown++;
            mk.Line.sharedMaterial = ready ? _ready : _soft;
            float a = ready ? 1f : Mathf.InverseLerp(0.45f, 0.12f, d);
            mk.Line.widthMultiplier = Mathf.Lerp(0.004f, ready ? 0.011f : 0.008f, a);
            if (c.Span.sqrMagnitude > 1e-6f)
            {
                mk.Line.positionCount = 2; mk.Line.loop = false; mk.Line.SetPosition(0, c.At - c.Span); mk.Line.SetPosition(1, c.At + c.Span);
            }
            else
            {
                // a ring facing me around the handle
                const int N = 20; mk.Line.positionCount = N; mk.Line.loop = true;
                var fwd = cam != null ? (cam.transform.position - c.At).normalized : Vector3.forward;
                var r1 = Vector3.Cross(fwd, Vector3.up); if (r1.sqrMagnitude < 1e-4f) r1 = Vector3.right; r1.Normalize(); var r2 = Vector3.Cross(fwd, r1);
                float rad = Mathf.Clamp(c.Reach * 0.7f, 0.022f, 0.05f) * (ready ? 1f + 0.08f * Mathf.Sin(Time.time * 8f) : 1f);
                for (int k = 0; k < N; k++) { float t = k * Mathf.PI * 2f / N; mk.Line.SetPosition(k, c.At + (r1 * Mathf.Cos(t) + r2 * Mathf.Sin(t)) * rad); }
            }
            bool showWord = ready || d < 0.22f;
            mk.Label.text = showWord ? c.Verb : ""; mk.Label.color = ready ? Palette.Yellow : new Color(0.6f, 1f, 0.95f, 0.85f);
            var lp = c.At + Vector3.up * 0.07f; mk.Label.transform.position = lp;
            if (cam != null) mk.Label.transform.rotation = Quaternion.LookRotation(lp - cam.transform.position);
        }

        // ------------------------------------------------------------------ my job's next step: arrow + (when I hesitate) a ghost hand
        void NextStep(bool inPlay, List<Vector3> hands, Camera cam)
        {
            Cand? target = null; string label = "";
            if (inPlay && cam != null) target = FindNextTarget(cam.transform.position, out label);
            NextLabel = target.HasValue ? label : "";
            if (!target.HasValue) { if (_arrow.gameObject.activeSelf) _arrow.gameObject.SetActive(false); HideGhost(); return; }
            var c = target.Value; NextAt = c.At;
            float headD = Vector3.Distance(cam.transform.position, c.At);
            if (headD > 4.5f) { if (_arrow.gameObject.activeSelf) _arrow.gameObject.SetActive(false); HideGhost(); return; }
            if (!_arrow.gameObject.activeSelf) _arrow.gameObject.SetActive(true);
            _arrow.position = c.At + Vector3.up * (0.12f + 0.025f * Mathf.Sin(Time.time * 3f));
            var look = _arrow.position - cam.transform.position; look.y = 0f; if (look.sqrMagnitude > 1e-4f) _arrow.rotation = Quaternion.LookRotation(look);
            _arrow.localScale = Vector3.one * Mathf.Clamp(headD / 1.2f, 0.7f, 2.2f);
            if (_arrowLabel.text != label) _arrowLabel.text = label;
            // ghost: I am near it (head within 1 m) but no hand is at it - after 2.5 s show how
            float handD = float.MaxValue; foreach (var p in hands) handD = Mathf.Min(handD, SegDist(p, c.At, c.Span));
            bool hesitating = headD < 1.0f && handD > c.Reach + 0.06f && c.Motion != Motion.None;
            if (!ReferenceEquals(_nearKey, c.Key)) { _nearKey = c.Key; _nearSince = -1f; }
            if (!hesitating) _nearSince = -1f; else if (_nearSince < 0f) _nearSince = Time.time;
            GhostWhy = $"head {headD:0.00} m, hand {handD:0.00} m (reach {c.Reach:0.00}), motion {c.Motion}, near for {(_nearSince >= 0f ? Time.time - _nearSince : -1f):0.0} s, cam {cam.name}";
            if (hesitating && _nearSince >= 0f && Time.time - _nearSince > 2.5f) Ghost(c, cam); else HideGhost();
            GhostWhy += $"; arrow {(_arrow.gameObject.activeInHierarchy ? "on" : "off")} at {_arrow.position:F2} scale {_arrow.localScale.x:0.00} layer {_arrow.gameObject.layer}; ghost {(_ghost.gameObject.activeInHierarchy ? "on" : "off")} at {_ghost.position:F2}; line pts {_arrowLine.positionCount} mat {(_arrowLine.sharedMaterial != null ? _arrowLine.sharedMaterial.shader.name : "none")}";
        }

        void HideGhost() { GhostOn = false; if (_ghost != null && _ghost.gameObject.activeSelf) _ghost.gameObject.SetActive(false); }

        void Ghost(Cand c, Camera cam)
        {
            GhostOn = true; if (!_ghost.gameObject.activeSelf) _ghost.gameObject.SetActive(true);
            float u = Mathf.Repeat(Time.time / 1.6f, 1f); float k = u < 0.15f ? 0f : u > 0.85f ? 1f : Mathf.SmoothStep(0f, 1f, (u - 0.15f) / 0.7f);
            Vector3 pos = c.At; Quaternion rot = Quaternion.LookRotation(cam != null ? Vector3.ProjectOnPlane(c.At - cam.transform.position, Vector3.up).normalized : Vector3.forward, Vector3.up);
            switch (c.Motion)
            {
                case Motion.Press:
                    rot = Quaternion.LookRotation(Vector3.down, rot * Vector3.forward);
                    pos = c.At + Vector3.up * (0.075f - 0.07f * Mathf.Sin(u * Mathf.PI)); break;
                case Motion.Swing:
                    if (c.Hinge != null)
                    {
                        var piv = c.Hinge.transform.position; var r0 = c.At - piv; float sign = Mathf.Sign(c.Hinge.OpenLimit);
                        float ang = (c.Hinge.Opening > 0.5f ? -1f : 1f) * sign * 35f * k;
                        pos = piv + Quaternion.AngleAxis(ang, c.Hinge.AxisW) * r0;
                    }
                    break;
                case Motion.Slide:
                    if (c.Slider != null)
                    {
                        var dir = c.Slider.transform.parent != null ? c.Slider.transform.parent.TransformDirection(c.Slider.Axis).normalized : c.Slider.Axis;
                        pos = c.At + dir * (c.Slider.Opening > 0.5f ? -0.15f : 0.15f) * k;
                    }
                    break;
                case Motion.Lever:
                    if (c.Tap != null) pos = c.Tap.LeverTipAt(c.Closing ? 1f - k : k);
                    break;
                case Motion.Turn:
                    pos = c.At + rot * new Vector3(Mathf.Cos(u * 6.28f) * 0.02f, Mathf.Sin(u * 6.28f) * 0.02f, 0f); break;
                case Motion.Place:
                    pos = Vector3.Lerp(c.PlaceFrom, c.At + Vector3.up * 0.04f, k); break;
                case Motion.Pick:
                    // reach down onto the item, pinch, lift it a little
                    rot = Quaternion.LookRotation(Vector3.down, rot * Vector3.forward);
                    pos = c.At + Vector3.up * (u < 0.5f ? 0.12f - 0.11f * Mathf.SmoothStep(0f, 1f, u * 2f) : 0.01f + 0.09f * Mathf.SmoothStep(0f, 1f, (u - 0.5f) * 2f)); break;
            }
            _ghost.SetPositionAndRotation(pos - rot * new Vector3(0.012f, 0f, 0.075f), rot);   // the fingertip (not the palm) at the point
        }

        /// <summary>The next unfinished step of my nearest unfinished job, as a target in the house (null = nothing to point at).</summary>
        Cand? FindNextTarget(Vector3 head, out string label)
        {
            label = ""; var rt = RecipeRuntime.I; var round = GameServices.Round;
            if (rt == null || round == null || round.Recipes == null) return null;
            Cand? best = null; float bd = float.MaxValue;
            foreach (var inst in round.Recipes.Instances)
            {
                if (inst.Owner != GameServices.LocalSlot || inst.Complete) continue;
                int si = -1; for (int i = 0; i < inst.Def.Steps.Count; i++) if (!inst.Satisfied[i]) { si = i; break; }
                if (si < 0) continue;
                var s = inst.Def.Steps[si]; Cand? t = null;
                if (s.Fixture != null)
                {
                    RecipeFixtureView fv = null; foreach (var f in rt.Fixtures) if (f != null && f.Instance == inst && f.Decl != null && f.Decl.Kind == s.Fixture) { fv = f; break; }
                    if (fv != null)
                    {
                        if (fv.Pump != null) t = new Cand { At = fv.Pump.PadTop, Reach = 0.05f, Motion = Motion.Press, Key = fv.Pump };
                        else if (fv.Tap != null) t = new Cand { At = fv.Tap.LeverTipAt(fv.Tap.Level), Reach = 0.06f, Motion = Motion.Lever, Tap = fv.Tap, Key = fv.Tap, Closing = s.Kind == StepKind.CloseState };
                        else if (fv.Host != null)
                        {
                            var door = fv.Host.DoorFor(s.Fixture);
                            if ((s.Kind == StepKind.OpenState || s.Kind == StepKind.CloseState) && door != null)
                                t = new Cand { At = door.HandleWorld, Span = door.transform.TransformVector(door.HandleSpanLocal), Reach = door.GrabRadius, Motion = door is HingedPart ? Motion.Swing : Motion.Slide, Hinge = door as HingedPart, Slider = door as SlidingPart, Key = door };
                            else { var sp = fv.Host.SlotPoint(s.Fixture, 0); t = new Cand { At = sp, Reach = 0.08f, Motion = Motion.Place, PlaceFrom = sp + fv.Host.LoadFrom(s.Fixture) * 0.35f + Vector3.up * 0.05f, Key = fv.Host }; }
                        }
                        else t = new Cand { At = new Vector3(fv.transform.position.x, fv.Top + 0.06f, fv.transform.position.z), Reach = 0.1f, Motion = s.Kind == StepKind.Place ? Motion.Place : Motion.None, PlaceFrom = fv.transform.position + Vector3.up * 0.35f, Key = fv };
                    }
                }
                else if (s.PropKind != null)
                {
                    foreach (var b in rt.Bodies)
                    {
                        if (b == null || b.Spec.Kind != s.PropKind || b.IsHeld) continue;
                        var rp = rt.RProp(b); if (rp == null || rp.RecipeInstance != inst.Index) continue;
                        t = new Cand { At = new Vector3(b.Col.bounds.center.x, b.Col.bounds.max.y, b.Col.bounds.center.z), Reach = 0.08f, Motion = Motion.Pick, Key = b }; break;
                    }
                }
                if (!t.HasValue) continue;
                float d = Vector3.Distance(head, t.Value.At);
                if (d < bd) { bd = d; best = t; label = s.Label; }
            }
            return best;
        }
    }
}
