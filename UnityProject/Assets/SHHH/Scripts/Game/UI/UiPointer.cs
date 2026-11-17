using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V6 UX-G2 far interaction for diegetic UI: each hand casts its pointer ray (OVR pointer pose on device) at the
    /// <see cref="PokeButton"/>s within 2.5 m; the hovered button lights up, and a pinch presses it. A thin ray + cursor
    /// shows only while it is actually on a button, and disappears whenever the hand is close enough to poke, is holding
    /// or hovering a prop, or tracking is lost - so near (poke/grab) and far (ray) never compete for the same pinch.
    /// </summary>
    public sealed class UiPointer : MonoBehaviour
    {
        public const float MaxDistance = 2.5f, NearPokeRange = 0.14f;
        public static UiPointer I { get; private set; }
        public readonly List<HandSource> Hands = new List<HandSource>();
        public readonly List<HandInteractor> Interactors = new List<HandInteractor>();

        sealed class State { public PokeButton hover; public bool pinchWas; public LineRenderer line; public Transform dot; public float hit; }
        readonly Dictionary<HandSource, State> _s = new Dictionary<HandSource, State>();

        /// <summary>True while this hand's ray is on a button (the hand interactor then suppresses magic-pull aiming).</summary>
        public static bool IsPointingAtUi(HandSource h) => I != null && h != null && I._s.TryGetValue(h, out var st) && st.hover != null;
        public static PokeButton HoverOf(HandSource h) => I != null && h != null && I._s.TryGetValue(h, out var st) ? st.hover : null;
        public int Presses { get; private set; }

        void Awake() { I = this; }

        public void Bind(HandSource h, HandInteractor hi)
        {
            if (h == null || Hands.Contains(h)) return;
            Hands.Add(h); Interactors.Add(hi);
        }

        public void Rebind(int i, HandSource h, HandInteractor hi)
        {
            if (i < Hands.Count) { if (_s.TryGetValue(Hands[i], out var old)) { Hide(old); _s.Remove(Hands[i]); } Hands[i] = h; Interactors[i] = hi; }
            else Bind(h, hi);
        }

        State Get(HandSource h)
        {
            if (_s.TryGetValue(h, out var st)) return st;
            st = new State();
            var go = new GameObject("uiRay_" + h.Hand); go.transform.SetParent(transform, false);
            st.line = go.AddComponent<LineRenderer>();
            st.line.positionCount = 2; st.line.widthMultiplier = 0.004f; st.line.numCapVertices = 2;
            st.line.sharedMaterial = Mats.Glow(new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.55f));
            st.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; st.line.enabled = false;
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(dot.GetComponent<Collider>());
            dot.name = "uiCursor"; dot.transform.SetParent(transform, false); dot.transform.localScale = Vector3.one * 0.014f;
            dot.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Palette.Yellow); dot.SetActive(false);
            st.dot = dot.transform;
            _s[h] = st; return st;
        }

        static void Hide(State st) { if (st.line) st.line.enabled = false; if (st.dot) st.dot.gameObject.SetActive(false); }

        void Update()
        {
            for (int i = 0; i < Hands.Count; i++)
            {
                var h = Hands[i]; if (h == null) continue;
                var hi = i < Interactors.Count ? Interactors[i] : null;
                var st = Get(h);
                PokeButton best = null; Vector3 bestPoint = Vector3.zero; float bestD = MaxDistance;
                bool usable = h.Tracked && !h.SystemGesture && (hi == null || (!hi.IsGrabbing && !hi.IsHovering));
                if (usable && !NearAnyButton(h.IndexTip))
                {
                    var ray = h.PointerRay;
                    foreach (var b in PokeButton.All)
                    {
                        if (b == null || !b.Interactable || !b.isActiveAndEnabled) continue;
                        if (b.RaySource != null && b.RaySource == h) continue; // never point at the panel strapped to this same wrist
                        if (b.RayHit(ray, out var p, out float d) && d < bestD) { bestD = d; best = b; bestPoint = p; }
                    }
                }
                if (best != st.hover)
                {
                    if (st.hover != null) st.hover.SetRayHover(false);
                    st.hover = best;
                    if (best != null) { best.SetRayHover(true); AudioBank.I?.Play("ui_hover", bestPoint, 0.07f, 0.05f, AudioBus.UI); }
                }
                bool pinch = h.Tracked && h.Pinch > 0.8f;
                bool open = !h.Tracked || h.Pinch < 0.45f;
                if (best != null)
                {
                    var o = h.PointerRay.origin;
                    st.line.enabled = true; st.line.SetPosition(0, o); st.line.SetPosition(1, bestPoint);
                    st.dot.gameObject.SetActive(true); st.dot.position = bestPoint;
                    if (pinch && !st.pinchWas) { best.Press(); Presses++; }
                }
                else Hide(st);
                if (pinch) st.pinchWas = true; else if (open) st.pinchWas = false;
            }
        }

        static bool NearAnyButton(Vector3 tip)
        {
            foreach (var b in PokeButton.All)
                if (b != null && b.isActiveAndEnabled && (b.transform.position - tip).sqrMagnitude < NearPokeRange * NearPokeRange) return true;
            return false;
        }
    }
}
