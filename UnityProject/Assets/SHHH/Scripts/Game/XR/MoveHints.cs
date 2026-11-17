using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (owner, Quest c15: "teleportation - I don't know how to gesture it; the gestures should be explained, well explained").
    /// Live coaching at the hands: turn a hand palm-up and a label appears right above it saying what that hand does and how
    /// (V13 finger gun: "HOP - finger gun, point at the floor, drop your thumb" / "WALK - finger gun, point, hold your thumb down");
    /// while aiming, a label at the end of the arc says "drop your thumb to hop" or why I cannot land there. The palm-up labels fade
    /// out after a few successful uses.
    /// Labels draw over everything (my own hands never hide them).
    /// </summary>
    public sealed class MoveHints : MonoBehaviour
    {
        public PlayerRig Rig;
        TextMeshPro[] _hand = new TextMeshPro[2]; TextMeshPro _aim;
        readonly float[] _palmUpFor = new float[2];
        bool _built;

        void Build()
        {
            _built = true;
            for (int h = 0; h < 2; h++) _hand[h] = Label("moveHint" + h, 0.16f);
            _aim = Label("hopHint", 0.2f);
        }

        TextMeshPro Label(string n, float size)
        {
            var g = new GameObject(n); DontDestroyOnLoad(g);
            var t = WorldText.Make(g.transform, "t", "", Vector3.zero, size, Palette.Cream, TextAlignmentOptions.Center, 0.36f);
            WorldText.Board(g.transform, "bg", new Vector3(0f, 0f, 0.004f), new Vector2(0.34f, 0.075f), Color.Lerp(Palette.Navy, Color.black, 0.2f));
            TaskHud.DrawOnTop(g.transform);
            g.SetActive(false); return t;
        }

        static void Show(TextMeshPro t, string text, Vector3 at, Color c)
        {
            var root = t.transform.parent; if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            if (t.text != text) t.text = text; t.color = c;
            root.position = at;
            var cam = Camera.main; if (cam != null) { var d = root.position - cam.transform.position; if (d.sqrMagnitude > 1e-4f) root.rotation = Quaternion.LookRotation(d); }
            float dist = cam != null ? Vector3.Distance(cam.transform.position, at) : 1f;
            root.localScale = Vector3.one * Mathf.Clamp(dist / 0.6f, 0.6f, 2.5f);
        }
        static void Hide(TextMeshPro t) { if (t != null && t.transform.parent.gameObject.activeSelf) t.transform.parent.gameObject.SetActive(false); }

        void LateUpdate()
        {
            if (Rig == null) return;
            if (!_built) Build();
            bool inPlay = GameServices.Round != null || GameServices.PracticeMode;
            for (int h = 0; h < 2; h++)
            {
                var src = h == 0 ? Rig.Left : Rig.Right; var hi = h == 0 ? Rig.LeftHand : Rig.RightHand;
                bool tracked = src != null && src.Tracked && src.HighConfidence;
                bool palmUp = tracked && src.PalmNormal.y > 0.6f && !src.GunPose;
                bool free = hi == null || (!hi.IsGrabbing && !hi.IsHovering);
                bool active = Rig.WalkingHand == h || Rig.AimingHand == h || Rig.WalkReadyHand == h;
                bool hop = Rig.TeleportMode || (Rig.HopRightHand && h == 1);
                bool learned = hop ? Rig.Hops >= 3 : Rig.Walks >= 3;
                _palmUpFor[h] = palmUp && free && !active && src.Pinch < 0.85f ? _palmUpFor[h] + Time.deltaTime : 0f;
                if (inPlay && Rig.LocomotionEnabled && _palmUpFor[h] > 0.35f && !learned)
                    Show(_hand[h], hop ? "<b>HOP</b>  finger gun, point at the floor, drop your thumb" : "<b>WALK</b>  finger gun, point, hold your thumb down",
                         src.PalmCenter + Vector3.up * 0.1f, hop ? Palette.Teal : Palette.Yellow);
                else if (Rig.WalkReadyHand == h && Rig.Walks < 3)
                    Show(_hand[h], "hold your thumb down to walk", src.IndexTip + Vector3.up * 0.1f, Palette.Yellow);
                else if (Rig.WalkingHand == h && Rig.Walks <= 3)
                    Show(_hand[h], "lift your thumb to stop", src.IndexTip + Vector3.up * 0.1f, Palette.Yellow);
                else Hide(_hand[h]);
            }
            if (Rig.AimingHand >= 0)
            {
                if (Rig.HopAimValid) Show(_aim, "drop your thumb to hop", Rig.HopAimTarget + Vector3.up * 0.35f, Palette.Teal);
                else if (Rig.HopAimHit) Show(_aim, "can't land here: " + Rig.LastHopRefusal, Rig.HopAimPoint + Vector3.up * 0.3f, Palette.Coral);
                else { var s = Rig.AimingHand == 0 ? Rig.Left : Rig.Right; if (s != null) Show(_aim, "point lower, at the floor", s.IndexTip + Vector3.up * 0.16f, Palette.Coral); }
            }
            else Hide(_aim);
        }
    }
}
