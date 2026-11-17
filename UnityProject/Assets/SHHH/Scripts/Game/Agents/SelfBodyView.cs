using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12: the local player's own Genies body, driven by the headset and the two hands (TrackedGeniesBody). Look down and you see
    /// your outfit, arms reaching to your gloves, legs that crouch with you. Your head and Genies hands are hidden for your own
    /// camera only; your friends see the whole body (their RemoteHumanView drives it from the same replicated poses).
    /// </summary>
    public sealed class SelfBodyView : MonoBehaviour
    {
        public SiblingAvatar Avatar;
        public PlayerRig Rig;
        public TrackedGeniesBody Body => Avatar != null ? Avatar.Tracked : null;

        public void Build(int slot, string displayName, int cosmetic, PlayerRig rig)
        {
            Rig = rig;
            Avatar = new GameObject("avatar").AddComponent<SiblingAvatar>(); Avatar.transform.SetParent(transform, false);
            Avatar.GeniesLoaded += OnLoaded;
            Avatar.BuildSelf(slot, displayName, cosmetic);
        }

        // my arms end at my gloves' wrists (the glove is my hand; its fingers follow real finger tracking)
        Pose? HandPose(HandSource h, bool left)
        {
            if (h == null || !h.Tracked) return null;
            var v = Glove(left);
            var wrist = v != null && v.PoseValid && v.Bones != null && v.Bones.Length > 0 && v.Bones[0] != null ? v.Bones[0].position : h.PinchPoint - h.HandRotation * Vector3.forward * 0.15f;
            return new Pose(wrist, h.HandRotation);
        }
        SimpleHandVisual _gl, _gr;
        SimpleHandVisual Glove(bool left)
        {
            var g = left ? _gl : _gr; if (g != null) return g;
            foreach (var v in FindObjectsByType<SimpleHandVisual>(FindObjectsSortMode.None)) if (v != null && v.IsLeft == left) { if (left) _gl = v; else _gr = v; return v; }
            return null;
        }

        void OnLoaded(SiblingAvatar a)
        {
            if (Rig == null) return;
            var t = TrackedGeniesBody.Attach(a, transform, true,
                () => Rig != null && Rig.Head != null ? new Pose(Rig.Head.position, Rig.Head.rotation) : (Pose?)null,
                () => HandPose(Rig != null ? Rig.Left : null, true), () => HandPose(Rig != null ? Rig.Right : null, false),
                () => Rig != null && Rig.Left != null ? Mathf.Max(Rig.Left.Grip, Rig.Left.Pinch) : 0f,
                () => Rig != null && Rig.Right != null ? Mathf.Max(Rig.Right.Grip, Rig.Right.Pinch) : 0f);
            if (t != null)
            {
                t.WristPoses = true;
                t.JointOf = (h, i) => { var g = Glove(h == 0); return g != null && g.PoseValid && g.TryJoint(i, out var p) ? p : (Vector3?)null; };
                ShowGloves(false);
            }
        }

        /// <summary>V12 owner 2026-11-12: my Genies hands are my hands (the gloves keep tracking the joints, unseen).</summary>
        void ShowGloves(bool on)
        {
            for (int s = 0; s < 2; s++) { var g = Glove(s == 0); if (g == null) continue; g.HiddenForBody = !on; foreach (var r in g.GetComponentsInChildren<Renderer>(true)) if (r != g.Skin) r.enabled = on; }
        }

        void OnDestroy() { if (Avatar != null) Avatar.GeniesLoaded -= OnLoaded; ShowGloves(true); }
    }
}
