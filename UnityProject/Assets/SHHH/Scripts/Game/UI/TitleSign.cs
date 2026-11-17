using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V8 front-end title: the approved SHHH! - DON'T WAKE MOM! lockup (brand kit original artwork, never retyped) on a hanging
    /// plum sign with a moon-cream rim, two ropes and a string of warm fairy bulbs. Motion follows the brand guide: whisper reveal ->
    /// wobble -> settle (~1.8 s), then a slow idle sway. Reduced motion = static.
    /// </summary>
    public sealed class TitleSign : MonoBehaviour
    {
        float _t; Quaternion _base; Vector3 _scale; Transform _bulbs; public static bool ReducedMotion;

        public static TitleSign Build(Transform parent, Vector3 pos, float yaw)
        {
            var g = new GameObject("TitleSign"); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var s = g.AddComponent<TitleSign>();
            var logo = UiKit.Tex("logo_primary");
            float w = 1.5f, h = logo != null ? w * logo.height / logo.width : 1.1f;
            UiKit.SlabObject(g.transform, "rim", new Vector3(0, 0, 0.03f), w + 0.14f, h + 0.1f, 0.16f, 0.04f, 0.014f, Mats.Lit(UiKit.MoonCream, 0.3f));
            UiKit.SlabObject(g.transform, "plate", new Vector3(0, 0, 0.012f), w + 0.08f, h + 0.04f, 0.13f, 0.04f, 0.016f, Mats.Lit(UiKit.Deep, 0.25f));
            if (logo != null) UiKit.Quad(g.transform, "logo", UiKit.Cutout(logo), new Vector3(0, 0, -0.012f), new Vector2(w, h));
            else UiKit.Text(g.transform, "SHHH!\n<size=40%>DON'T WAKE MOM!</size>", new Vector3(0, 0, -0.012f), 3f, UiKit.Hush, UiKit.Display, w);
            // ropes up to the ceiling
            var rope = Mats.Lit(Palette.Hex(0xC8A27A));
            for (int i = -1; i <= 1; i += 2) Prim.Shape(PrimitiveType.Cylinder, g.transform, "rope", new Vector3(i * w * 0.38f, h / 2f + 0.35f, 0.03f), new Vector3(0.012f, 0.35f, 0.012f), rope);
            // fairy lights along a sagging curve under the sign
            s._bulbs = new GameObject("bulbs").transform; s._bulbs.SetParent(g.transform, false);
            var cols = new[] { UiKit.Hush, UiKit.Pink, UiKit.MoonCream, UiKit.Teal, UiKit.Coral };
            int n = 15;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1), x = Mathf.Lerp(-w * 0.55f, w * 0.55f, u), y = -h / 2f - 0.06f - Mathf.Sin(u * Mathf.PI) * 0.12f;
                Prim.Shape(PrimitiveType.Sphere, s._bulbs, "bulb" + i, new Vector3(x, y, -0.02f), Vector3.one * 0.035f, Mats.Unlit(cols[i % cols.Length]));
                if (i < n - 1) { float u2 = (i + 1) / (float)(n - 1), x2 = Mathf.Lerp(-w * 0.55f, w * 0.55f, u2), y2 = -h / 2f - 0.06f - Mathf.Sin(u2 * Mathf.PI) * 0.12f;
                    var a = new Vector3(x, y, -0.02f); var b = new Vector3(x2, y2, -0.02f);
                    var wire = Prim.Shape(PrimitiveType.Cylinder, s._bulbs, "wire", (a + b) / 2f, new Vector3(0.004f, Vector3.Distance(a, b) / 2f, 0.004f), Mats.Unlit(UiKit.Deep));
                    wire.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized); }
            }
            s._base = g.transform.localRotation; s._scale = g.transform.localScale;
            return s;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (ReducedMotion) { transform.localRotation = _base; transform.localScale = _scale; return; }
            float k;
            if (_t < 0.4f) k = Mathf.SmoothStep(0.6f, 1f, _t / 0.4f);                                  // whisper reveal
            else if (_t < 0.85f) k = 1f + 0.06f * Mathf.Sin((_t - 0.4f) / 0.45f * Mathf.PI * 3f) * (1f - (_t - 0.4f) / 0.45f); // wobble
            else k = 1f;
            transform.localScale = _scale * k;
            float sway = _t < 1.8f ? 0f : Mathf.Sin(_t * 0.7f) * 1.2f;                                     // idle sway on the ropes
            transform.localRotation = _base * Quaternion.Euler(0, 0, sway);
            if (_bulbs != null) for (int i = 0; i < _bulbs.childCount; i++) { var c = _bulbs.GetChild(i); if (c.name.StartsWith("bulb")) c.localScale = Vector3.one * (0.032f + 0.008f * Mathf.Sin(_t * 2.2f + i * 1.3f)); }
        }
    }
}
