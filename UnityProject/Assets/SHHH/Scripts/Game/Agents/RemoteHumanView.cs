using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Another human player (online): their sibling avatar follows their tracked head, and two rounded mitten hands
    /// follow their tracked hands, so you can see who is reaching for what. Pose comes from GameServices.RemotePose.
    /// </summary>
    public sealed class RemoteHumanView : MonoBehaviour
    {
        public int Slot;
        public SiblingAvatar Avatar;
        Transform _l, _r;
        Vector3 _vel; Vector3 _lastPos; bool _init, _genieHands;

        public void Build(int slot, SiblingAvatar avatar, Color color)
        {
            Slot = slot; Avatar = avatar;
            _l = Hand("handL", color); _r = Hand("handR", color);
        }

        Transform Hand(string n, Color c)
        {
            var root = new GameObject(n).transform; root.SetParent(transform.parent, false);
            var palm = Prim.Shape(PrimitiveType.Sphere, root, "palm", Vector3.zero, new Vector3(0.085f, 0.04f, 0.1f), Mats.Lit(Palette.Hex(0xF2C7A5)));
            Prim.Shape(PrimitiveType.Sphere, root, "cuff", new Vector3(0, 0, -0.06f), new Vector3(0.07f, 0.06f, 0.05f), Mats.Lit(c));
            foreach (var col in root.GetComponentsInChildren<Collider>()) Destroy(col);
            return root;
        }

        /// <summary>V12: this friend's Genies body is driven by their tracked head + hands (TrackedGeniesBody) - full body, scaled to
        /// their height, crouching, turning, hands with fingers that close on what they hold.</summary>
        public TrackedGeniesBody Body => Avatar != null ? Avatar.Tracked : null;

        void AttachBody()
        {
            var f = GameServices.RemotePose; if (f == null || Avatar == null) return;
            int s = Slot;
            TrackedGeniesBody.Attach(Avatar, transform, false, () => f(s, 0), () => f(s, 1), () => f(s, 2), () => GripOf(1), () => GripOf(2));
        }

        // a hand closes when this friend holds something right at it (replicated holder table on every peer)
        readonly float[] _grip = { 0.15f, 0.15f }; float _gripAt;
        float GripOf(int hand)
        {
            if (Time.time - _gripAt > 0.2f)
            {
                _gripAt = Time.time; _grip[0] = _grip[1] = 0.15f;
                var room = GameServices.Room; var f = GameServices.RemotePose;
                if (room != null && f != null && GameServices.HolderOf != null)
                    foreach (var p in room.Props)
                    {
                        if (p == null || GameServices.HolderOf(p.Spec.Id) != Slot) continue;
                        for (int h = 0; h < 2; h++) { var hp = f(Slot, h + 1); if (hp.HasValue && Vector3.Distance(hp.Value.position, p.Col != null ? p.Col.ClosestPoint(hp.Value.position) : p.transform.position) < 0.12f) _grip[h] = 0.9f; }
                    }
            }
            return _grip[Mathf.Clamp(hand - 1, 0, 1)];
        }

        void LateUpdate()
        {
            var f = GameServices.RemotePose; if (f == null) return;
            if (Avatar != null && Avatar.HasGeniesBody)
            {
                if (Avatar.Tracked == null) AttachBody();
                if (Avatar.Tracked != null)
                {
                    // the body does it all: the stand-in mittens are not needed
                    if (_l && _l.gameObject.activeSelf) _l.gameObject.SetActive(false);
                    if (_r && _r.gameObject.activeSelf) _r.gameObject.SetActive(false);
                    return;
                }
            }
            var head = f(Slot, 0);
            if (head.HasValue)
            {
                var hp = head.Value.position; var fwd = Vector3.ProjectOnPlane(head.Value.rotation * Vector3.forward, Vector3.up);
                float headH = Avatar != null && Avatar.Head != null ? Mathf.Clamp(Avatar.Head.position.y - Avatar.transform.position.y, 0.6f, 1.7f) : 1.2f;
                var body = new Vector3(hp.x, Mathf.Max(0f, hp.y - headH - 0.1f), hp.z) - (fwd.sqrMagnitude > 0.01f ? fwd.normalized * 0.08f : Vector3.zero);
                if (!_init) { transform.position = body; _lastPos = body; _init = true; }
                transform.position = Vector3.Lerp(transform.position, body, 1f - Mathf.Exp(-15f * Time.deltaTime));
                if (fwd.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd.normalized), 0.25f);
                _vel = Vector3.Lerp(_vel, (transform.position - _lastPos) / Mathf.Max(1e-3f, Time.deltaTime), 0.2f); _lastPos = transform.position;
                Avatar?.SetMotion(new Vector2(_vel.x, _vel.z).magnitude, false);
            }
            // Genies body: its own hands follow the tracked hands through IK, the stand-in mittens are only invisible IK targets.
            bool genie = Avatar != null && Avatar.HasGeniesBody;
            if (genie != _genieHands && _l && _r)
            {
                _genieHands = genie;
                foreach (var rr in _l.GetComponentsInChildren<Renderer>(true)) rr.enabled = !genie;
                foreach (var rr in _r.GetComponentsInChildren<Renderer>(true)) rr.enabled = !genie;
                Avatar.IkLeft = genie ? _l : null; Avatar.IkRight = genie ? _r : null;
            }
            var l = f(Slot, 1); if (l.HasValue && _l) { _l.gameObject.SetActive(true); _l.SetPositionAndRotation(l.Value.position, l.Value.rotation); } else if (_l) _l.gameObject.SetActive(false);
            var r = f(Slot, 2); if (r.HasValue && _r) { _r.gameObject.SetActive(true); _r.SetPositionAndRotation(r.Value.position, r.Value.rotation); } else if (_r) _r.gameObject.SetActive(false);
        }

        void OnDestroy() { if (_l) Destroy(_l.gameObject); if (_r) Destroy(_r.gameObject); }
    }
}
