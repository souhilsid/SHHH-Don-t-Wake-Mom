using System.Collections.Generic;
using SHHH.Core;
using SHHH.Game;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// HOST ONLY: physics for items held by remote humans. The item follows that player's networked hand pose with the
    /// same velocity-driven carry as a local hand (collisions respected), and is released with the client's throw
    /// velocity - so placement, noise and blame are all simulated by the one authoritative physics scene.
    /// </summary>
    public sealed class RemoteHold : MonoBehaviour
    {
        sealed class Hold { public PropBody Prop; public int Slot; public int Hand; public Vector3 LocalPos; public Quaternion LocalRot; public bool Frozen; public float EaseUntil; }
        readonly Dictionary<int, Hold> _holds = new Dictionary<int, Hold>();
        public System.Func<int, int, Pose?> HandPose; // (slot, hand 1/2)

        public int Count => _holds.Count;
        public bool IsHeldBy(int propId, int slot) => _holds.TryGetValue(propId, out var h) && h.Slot == slot;

        public void Begin(PropBody p, int slot, int hand)
        {
            var hp = HandPose?.Invoke(slot, hand) ?? new Pose(p.transform.position, Quaternion.identity);
            var cp = p.Col != null ? p.Col.ClosestPoint(hp.position) : p.transform.position;
            var inv = Quaternion.Inverse(hp.rotation);
            _holds[p.Spec.Id] = new Hold
            {
                Prop = p, Slot = slot, Hand = hand,
                LocalPos = inv * (p.transform.position - cp), // nearest surface point between the fingers
                LocalRot = inv * p.transform.rotation,
            };
            p.OnGrabbed(slot);
            p.Body.isKinematic = false; p.Body.useGravity = false; p.BoostHeldMass(true);
            p.IgnoreImpactsUntil = Time.time + 0.3f;
        }

        public bool End(int propId, int slot, Vector3 velocity, bool thrown)
        {
            if (!_holds.TryGetValue(propId, out var h) || h.Slot != slot) return false;
            _holds.Remove(propId);
            var p = h.Prop; if (p == null) return true;
            velocity = Vector3.ClampMagnitude(velocity, HandInteractor.MaxThrowSpeed);
            p.Body.useGravity = true; p.Body.isKinematic = false; p.BoostHeldMass(false);
            p.Body.linearVelocity = velocity;
            p.OnReleased(slot, velocity, thrown);
            GameServices.Round?.Release(slot, propId, velocity.magnitude, thrown);
            return true;
        }

        public void DropAll(int slot)
        {
            var ids = new List<int>(); foreach (var kv in _holds) if (kv.Value.Slot == slot) ids.Add(kv.Key);
            foreach (var id in ids) End(id, slot, Vector3.zero, false);
        }

        public void Clear() { _holds.Clear(); }

        void FixedUpdate()
        {
            if (_holds.Count == 0) return;
            List<int> gone = null;
            foreach (var kv in _holds)
            {
                var h = kv.Value;
                if (h.Prop == null || h.Prop.Body == null) { (gone ??= new List<int>()).Add(kv.Key); continue; }
                var hp = HandPose?.Invoke(h.Slot, h.Hand);
                var rb = h.Prop.Body;
                // V12: the remote hand lost tracking - the item stays gently where it is (kinematic), it never drifts, drops or chatters
                if (!hp.HasValue) { if (!h.Frozen) { h.Frozen = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; } continue; }
                if (h.Frozen)
                {
                    // tracking is back: hand the item back softly (no whip into furniture -> no fake crash noise for Mom)
                    h.Frozen = false; rb.isKinematic = false; h.EaseUntil = Time.time + 0.4f; h.Prop.IgnoreImpactsUntil = Time.time + 0.6f;
                }
                var targetPos = hp.Value.position + hp.Value.rotation * h.LocalPos;
                var targetRot = hp.Value.rotation * h.LocalRot;
                Vector3 delta = targetPos - rb.position;
                if (delta.magnitude > 1.2f) { rb.position = targetPos; rb.linearVelocity = Vector3.zero; continue; } // network hiccup: re-sync
                rb.linearVelocity = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime, Time.time < h.EaseUntil ? 1.5f : 10f);
                Quaternion dq = targetRot * Quaternion.Inverse(rb.rotation);
                dq.ToAngleAxis(out float ang, out Vector3 ax); if (ang > 180f) ang -= 360f;
                if (Mathf.Abs(ang) > 0.01f && !float.IsNaN(ax.x)) rb.angularVelocity = Vector3.ClampMagnitude(ax * (ang * Mathf.Deg2Rad / Time.fixedDeltaTime), 25f);
            }
            if (gone != null) foreach (var id in gone) _holds.Remove(id);
        }
    }
}
