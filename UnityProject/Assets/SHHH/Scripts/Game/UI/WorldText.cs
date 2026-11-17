using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>World-space TextMeshPro helpers (diegetic labels, boards, subtitles). English only.</summary>
    public static class WorldText
    {
        public static TextMeshPro Make(Transform parent, string name, string text, Vector3 localPos, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, float width = 1f)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false); g.transform.localPosition = localPos;
            var t = g.AddComponent<TextMeshPro>();
            if (ArtCatalog.I.brandFont is TMP_FontAsset f) t.font = f;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(width, size * 0.02f + 0.1f);
            t.overflowMode = TextOverflowModes.Overflow;
            t.outlineWidth = 0.18f; t.outlineColor = new Color32(14, 13, 38, 255);
            g.layer = Layers.UI;
            return t;
        }

        /// <summary>V8 board (see UiKit.Panel): bevelled plum slab with a moon-cream rim; opaque and toon-lit (Quest-cheap).</summary>
        public static Transform Board(Transform parent, string name, Vector3 localPos, Vector2 size, Color? body = null)
        {
            // V8 (2026-11-09): chunky opaque toy board (UiKit.Panel) replaces the V5 translucent night-glass layers.
            return UiKit.Panel(parent, name, localPos, size, null, body == Palette.Navy ? (Color?)null : body);
        }

        /// <summary>Keeps a transform facing the main camera around the Y axis (labels, subtitles).</summary>
        public static void Billboard(Transform t)
        {
            var cam = Camera.main; if (cam == null) return;
            var d = t.position - cam.transform.position; d.y = 0;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d);
        }
    }
}
