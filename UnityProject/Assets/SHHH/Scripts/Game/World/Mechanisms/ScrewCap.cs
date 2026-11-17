using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 screw cap (mechanic 6 "Twisting"): a bottle is sealed (<see cref="LiquidContainer.Sealed"/>) until a hand pinches the cap and
    /// twists it counter-clockwise about the bottle's own axis - measured in the bottle's frame, so it works while the other hand holds
    /// and moves the bottle. The cap rises along its thread and, after <see cref="Turns"/> turns, comes off in the fingers; let go, it
    /// drops as a small loose piece. No "Open" button anywhere.
    /// </summary>
    public sealed class ScrewCap : MechPart
    {
        public float Turns = 1.5f, Pitch = 0.004f;   // thread: turns to free it, rise per turn (m)
        public LiquidContainer Liquid;
        public bool Off { get; private set; }
        public float Unscrewed { get; private set; }   // degrees
        public override float Opening => Off ? 1f : Mathf.Clamp01(Unscrewed / (Turns * 360f));
        public override bool IsClosed => !Off && Unscrewed < 5f;

        Transform _bottle; Vector3 _restLocal; Quaternion _restRot, _lr0; float _acc, _last, _u0; GameObject _visual;

        public static ScrewCap Add(Transform bottle, LiquidContainer liquid, float neckRadius, Vector3 neckTopLocal, Color colour)
        {
            var g = new GameObject("screw_cap"); g.transform.SetParent(bottle, false); g.transform.localPosition = neckTopLocal; g.layer = bottle.gameObject.layer;
            var cap = g.AddComponent<ScrewCap>(); cap._bottle = bottle; cap.Liquid = liquid; cap._restLocal = neckTopLocal; cap._restRot = g.transform.localRotation;
            var v = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(v.GetComponent<Collider>()); v.name = "cap_visual";
            v.transform.SetParent(g.transform, false); float r = Mathf.Max(0.012f, neckRadius * 1.5f); v.transform.localScale = new Vector3(r * 2f, 0.009f, r * 2f); v.transform.localPosition = new Vector3(0, 0.009f, 0);
            v.GetComponent<Renderer>().sharedMaterial = Mats.Lit(colour, 0.3f); v.layer = g.layer; cap._visual = v;
            cap.HandleLocal = new Vector3(0, 0.01f, 0); cap.GrabRadius = 0.04f; cap.Label = "bottle cap"; cap.Material = SHHH.Core.NoiseMaterial.Plastic;
            if (liquid != null) liquid.Sealed = true;
            return cap;
        }

        Quaternion HandInBottle => Quaternion.Inverse(_bottle.rotation) * Hand.Source.HandRotation;

        float TwistLocal()
        {
            // rotation of the hand about the bottle's local up since the grab (counter-clockwise seen from above = loosening = positive)
            var q = HandInBottle * Quaternion.Inverse(_lr0); var r = Vector3.right; var v = Vector3.ProjectOnPlane(q * r, Vector3.up);
            return v.sqrMagnitude < 1e-6f ? 0f : Vector3.SignedAngle(r, v, Vector3.up) * -1f;
        }

        protected override void OnGrab(Vector3 at) { _lr0 = HandInBottle; _acc = 0f; _last = 0f; _u0 = Unscrewed; }

        protected override void OnRelease()
        {
            if (!Off) return;
            // the freed cap drops from the fingers as a small loose piece
            transform.SetParent(null, true);
            var rb = gameObject.AddComponent<Rigidbody>(); rb.mass = 0.02f; rb.linearVelocity = Vector3.zero;
            var c = gameObject.AddComponent<CapsuleCollider>(); c.radius = 0.015f; c.height = 0.03f; c.direction = 1;
            enabled = false; Destroy(gameObject, 60f);
        }

        /// <summary>Another peer unscrewed it (replicated house mechanism level 100): unseal here too.</summary>
        void ForceOff() { Off = true; Unscrewed = Turns * 360f; if (Liquid != null) Liquid.Sealed = false; if (_visual != null) _visual.SetActive(false); }

        void Update()
        {
            if (_bottle == null) return;
            if (!Off && Hand == null && Remote.HasValue && Remote.Value >= 0.999f) { ForceOff(); return; }
            if (Hand == null || Off)
            {
                if (Off && Hand != null) transform.position = HandPoint;   // the loose cap is in the fingers
                return;
            }
            float tw = TwistLocal(); _acc += Mathf.DeltaAngle(_last, tw); _last = tw;
            float before = Unscrewed; Unscrewed = Mathf.Clamp(_u0 + _acc, 0f, Turns * 360f);
            if (Mathf.FloorToInt(before / 90f) != Mathf.FloorToInt(Unscrewed / 90f)) { if (AudioBank.I != null) AudioBank.I.Play("ui_click", transform.position, 0.2f, 0.2f); MechHaptics.Pulse(Hand, 0.15f, 0.015f); }
            transform.localPosition = _restLocal + Vector3.up * (Unscrewed / 360f * Pitch);
            transform.localRotation = _restRot * Quaternion.AngleAxis(-Unscrewed, Vector3.up);
            if (Unscrewed >= Turns * 360f - 0.5f)
            {
                Off = true; if (Liquid != null) Liquid.Sealed = false;
                if (AudioBank.I != null) AudioBank.I.Play("join_pop", transform.position, 0.45f);
                MechHaptics.Pulse(Hand, 0.4f, 0.04f);
            }
            if (Vector3.Distance(HandPoint, HandleWorld) > 0.12f && !Off) EndGrab();
        }
    }
}
