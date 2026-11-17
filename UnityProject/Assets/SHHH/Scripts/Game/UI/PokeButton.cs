using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Physical push-button for diegetic UI, pressed by a tracked index fingertip (no rays, no controllers needed).
    /// Large hit area, hover glow, real depress travel, click sound, debounce. In the Editor it can be clicked with the mouse.
    /// </summary>
    public sealed class PokeButton : MonoBehaviour
    {
        public static readonly List<HandSource> Pokers = new List<HandSource>();
        public static readonly List<PokeButton> All = new List<PokeButton>();
        public Action OnPress;
        public Vector2 Size = new Vector2(0.16f, 0.07f);
        public bool Interactable = true;
        public string Label;
        /// <summary>Hand whose wrist carries this button (its own ray must not target it).</summary>
        public HandSource RaySource;
        bool _rayHover; float _pop = -1f; Vector3 _baseScale = Vector3.one;
        Transform _cap; Renderer _capR; TextMeshPro _text;
        Color _color; float _press; float _cooldown; bool _hovered; readonly Dictionary<HandSource, float> _lastZ = new Dictionary<HandSource, float>();
        const float Travel = 0.012f;

        public static PokeButton Create(Transform parent, string label, Vector3 localPos, Vector2 size, Color color, Action onPress, float fontSize = 0.5f)
            => Create(parent, label, localPos, size, color, onPress, fontSize, null);

        /// <summary>
        /// V8 (2026-11-09) chunky toy key: dark bevelled well + a thick bevelled toon cap in the key colour, optional sticker icon
        /// (Resources/UI_V8/icon_*) and a Fredoka label in ink or cream depending on the cap colour. Same poke/ray behaviour as before.
        /// </summary>
        public static PokeButton Create(Transform parent, string label, Vector3 localPos, Vector2 size, Color color, Action onPress, float fontSize, string icon)
        {
            var root = new GameObject("btn_" + label); root.transform.SetParent(parent, false); root.transform.localPosition = localPos;
            var b = root.AddComponent<PokeButton>();
            b.Size = size; b._color = color; b.OnPress = onPress; b.Label = label;
            float r = Mathf.Min(size.x, size.y) * 0.42f;
            UiKit.SlabObject(root.transform, "well", new Vector3(0, -0.003f, 0.008f), size.x + 0.014f, size.y + 0.016f, r + 0.007f, 0.016f, 0.005f, Mats.Lit(UiKit.Deep, 0.2f));
            var cap = UiKit.SlabObject(root.transform, "cap", new Vector3(0, 0, -0.004f), size.x, size.y, r, 0.022f, Mathf.Min(0.008f, size.y * 0.16f), Mats.Lit(color, 0.45f));
            b._cap = cap.transform; b._capR = cap.GetComponent<Renderer>();
            float textX = 0f, textW = size.x * 0.92f;
            if (!string.IsNullOrEmpty(icon) && UiKit.Tex("icon_" + icon) != null)
            {
                float isz = size.y * 0.78f;
                bool solo = string.IsNullOrEmpty(label);
                float ix = solo ? 0f : -size.x / 2f + isz * 0.5f + size.y * 0.14f;
                UiKit.Icon(cap.transform, icon, new Vector3(ix, 0, -0.0115f), isz);
                if (!solo) { textX = isz * 0.5f + size.y * 0.06f; textW = size.x - isz - size.y * 0.35f; }
            }
            b._text = WorldText.Make(cap.transform, "label", label, new Vector3(textX, 0, -0.012f), fontSize, UiKit.InkFor(color), TextAlignmentOptions.Center, textW);
            if (UiKit.Label != null) b._text.font = UiKit.Label;
            b._text.outlineWidth = 0f; b._text.fontStyle = FontStyles.Bold;
            b._text.transform.localScale = Vector3.one;
            b._text.rectTransform.sizeDelta = new Vector2(textW, size.y);
            b._text.enableAutoSizing = true; b._text.fontSizeMax = fontSize; b._text.fontSizeMin = fontSize * 0.55f; b._text.textWrappingMode = TextWrappingModes.Normal;
            return b;
        }

        /// <summary>Press mechanics only: the caller builds the visuals under <see cref="Cap"/> (cards, medallions).</summary>
        public static PokeButton CreateBlank(Transform parent, string name, Vector3 localPos, Vector2 size, Action onPress)
        {
            var root = new GameObject("btn_" + name); root.transform.SetParent(parent, false); root.transform.localPosition = localPos;
            var b = root.AddComponent<PokeButton>(); b.Size = size; b.OnPress = onPress; b.Label = name; b._color = Color.white;
            var cap = new GameObject("cap").transform; cap.SetParent(root.transform, false); cap.localPosition = new Vector3(0, 0, -0.004f);
            b._cap = cap;
            return b;
        }

        public Transform Cap => _cap;
        /// <summary>Optional "selected" highlight (cards, toggles).</summary>
        public GameObject SelectedMark;
        public void SetSelected(bool on) { if (SelectedMark) SelectedMark.SetActive(on); }

        public int PressCount { get; private set; }
        public void SetLabel(string s) { Label = s; if (_text) _text.text = s; }
        public void SetColor(Color c) { _color = c; if (_capR) _capR.sharedMaterial = Keep(Mats.Lit(c, 0.45f)); if (_text) _text.color = UiKit.InkFor(c); }
        /// <summary>A button on an on-top panel (menus, wrist, tray) keeps drawing on top when its colour changes.</summary>
        Material Keep(Material m) => _capR != null && _capR.sharedMaterial != null && _capR.sharedMaterial.renderQueue >= 3990 ? TaskHud.Top(m) : m;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); _lastZ.Clear(); _rayHover = false; }
        void Start() { _baseScale = transform.localScale; }

        public void SetRayHover(bool on) { _rayHover = on; }
        public bool RayHovered => _rayHover;

        /// <summary>Ray vs. the button's front face (inflated 10% for forgiving aim). Works on scaled boards.</summary>
        public bool RayHit(Ray worldRay, out Vector3 point, out float dist)
        {
            point = default; dist = float.MaxValue;
            Vector3 lo = transform.InverseTransformPoint(worldRay.origin), ld = transform.InverseTransformVector(worldRay.direction);
            if (ld.z <= 1e-5f) return false;              // must travel into the face (button faces -Z)
            float t = (-0.013f - lo.z) / ld.z; if (t <= 0f) return false;
            var hit = lo + ld * t;
            if (Mathf.Abs(hit.x) > Size.x * 0.55f || Mathf.Abs(hit.y) > Size.y * 0.6f) return false;
            point = transform.TransformPoint(hit); dist = Vector3.Distance(point, worldRay.origin);
            return dist <= UiPointer.MaxDistance;
        }

        void Update()
        {
            _cooldown -= Time.deltaTime;
            bool hover = false; float depth = 0f;
            if (Interactable)
            {
                foreach (var h in Pokers)
                {
                    if (h == null || !h.Tracked) continue;
                    Vector3 lp = transform.InverseTransformPoint(h.IndexTip);
                    bool inside = Mathf.Abs(lp.x) < Size.x * 0.6f && Mathf.Abs(lp.y) < Size.y * 0.7f;
                    // Front face at z = -0.013 (button faces -Z). Penetration = how far the tip went past it.
                    float pen = -0.013f - lp.z;
                    if (inside && lp.z > -0.08f && lp.z < 0.03f) { hover = true; depth = Mathf.Max(depth, Mathf.Clamp(-pen, 0f, Travel)); }
                    _lastZ.TryGetValue(h, out float prevZ);
                    if (inside && lp.z > -0.002f && prevZ <= -0.002f && prevZ > -0.08f) Press();
                    _lastZ[h] = lp.z;
                }
            }
            if (_rayHover) hover = true;
            _press = Mathf.Lerp(_press, depth, 0.5f);
            if (_cap) _cap.localPosition = new Vector3(0, 0, -0.004f + _press);
            // Press confirmation: a short squash-and-pop on the whole key (cosmetic only, never on physics objects).
            if (_pop >= 0f)
            {
                _pop += Time.unscaledDeltaTime / 0.22f;
                float k = _pop < 1f ? 1f - 0.12f * Mathf.Sin(_pop * Mathf.PI) : 1f;
                transform.localScale = _baseScale * k;
                if (_pop >= 1f) _pop = -1f;
            }
            else if (_hovered && transform.localScale != _baseScale * 1.04f) transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * 1.04f, 0.3f);
            else if (!_hovered && transform.localScale != _baseScale) transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 0.3f);
            if (hover != _hovered)
            {
                _hovered = hover;
                if (_capR) _capR.sharedMaterial = Keep(Mats.Lit(hover ? Color.Lerp(_color, Color.white, 0.3f) : (Interactable ? _color : Color.gray), 0.45f));
            }
        }

        public void Press()
        {
            if (_cooldown > 0f || !Interactable) return;
            _cooldown = 0.45f; _pop = 0f; PressCount++;
            AudioBank.I?.Play("ui_click", transform.position, 0.6f, 0.03f, AudioBus.UI);
            try { OnPress?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

#if UNITY_EDITOR
        void OnMouseDown() { Press(); }
#endif
    }
}
