using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Lightweight procedural animation for humanoid rigs without clips: relaxes the bind pose (arms down), adds walk
    /// cycle (thigh/knee/arm swing, bob), carry pose, breathing, head look and a cheer. Cheap enough for 4 bots on Quest 2.
    /// </summary>
    public sealed class ProceduralHumanoid : MonoBehaviour
    {
        public Transform Root;
        public float Speed;        // m/s
        public bool Carrying;
        public float CheerUntil;
        public Vector3? LookAt;

        Transform _hips, _spine, _head, _armL, _armR, _foreL, _foreR, _legL, _legR, _kneeL, _kneeR;
        Quaternion _rHips, _rSpine, _rHead, _rArmL, _rArmR, _rForeL, _rForeR, _rLegL, _rLegR, _rKneeL, _rKneeR;
        Vector3 _hipsPos;
        float _armDownL, _armDownR, _phase, _speedSm;
        bool _ok;

        public void Bind(Animator anim, Transform root)
        {
            Root = root;
            if (anim == null || !anim.isHuman) return;
            _hips = anim.GetBoneTransform(HumanBodyBones.Hips); _spine = anim.GetBoneTransform(HumanBodyBones.Chest); if (_spine == null) _spine = anim.GetBoneTransform(HumanBodyBones.Spine);
            _head = anim.GetBoneTransform(HumanBodyBones.Head);
            _armL = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm); _armR = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _foreL = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm); _foreR = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _legL = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); _legR = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _kneeL = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg); _kneeR = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            if (_hips == null || _armL == null || _armR == null || _legL == null || _legR == null) return;
            anim.enabled = false; // we drive the bones directly
            Quaternion inv = Quaternion.Inverse(Root.rotation);
            _rHips = inv * _hips.rotation; _rSpine = _spine ? inv * _spine.rotation : Quaternion.identity; _rHead = _head ? inv * _head.rotation : Quaternion.identity;
            _rArmL = inv * _armL.rotation; _rArmR = inv * _armR.rotation; _rForeL = _foreL ? inv * _foreL.rotation : Quaternion.identity; _rForeR = _foreR ? inv * _foreR.rotation : Quaternion.identity;
            _rLegL = inv * _legL.rotation; _rLegR = inv * _legR.rotation; _rKneeL = _kneeL ? inv * _kneeL.rotation : Quaternion.identity; _rKneeR = _kneeR ? inv * _kneeR.rotation : Quaternion.identity;
            _hipsPos = Root.InverseTransformPoint(_hips.position);
            // How far each arm already hangs below horizontal in the bind pose (T-pose ~0°, A-pose ~45°).
            _armDownL = ArmDrop(_armL, _foreL); _armDownR = ArmDrop(_armR, _foreR);
            _ok = true;
        }

        float ArmDrop(Transform upper, Transform lower)
        {
            if (lower == null) return 0f;
            var d = Root.InverseTransformDirection((lower.position - upper.position).normalized);
            return Mathf.Asin(Mathf.Clamp(-d.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        void Set(Transform b, Quaternion bindRel, Quaternion localDelta)
        {
            if (b == null) return;
            b.rotation = Root.rotation * (localDelta * bindRel);
        }

        void LateUpdate()
        {
            if (!_ok) return;
            _speedSm = Mathf.Lerp(_speedSm, Speed, 0.15f);
            float k = Mathf.Clamp01(_speedSm / 1.2f);
            _phase += Time.deltaTime * (2.2f + 5.5f * k);
            float s = Mathf.Sin(_phase);
            Vector3 fwd = Vector3.forward, right = Vector3.right, up = Vector3.up; // root-local axes
            bool cheer = Time.time < CheerUntil;
            float breath = Mathf.Sin(Time.time * 1.6f) * 1.5f;

            // Legs: swing around the local right axis; knees bend on the back-swing.
            Set(_legL, _rLegL, Quaternion.AngleAxis(s * 28f * k, right));
            Set(_legR, _rLegR, Quaternion.AngleAxis(-s * 28f * k, right));
            Set(_kneeL, _rKneeL, Quaternion.AngleAxis(-Mathf.Max(0f, s) * 40f * k, right) * Quaternion.AngleAxis(s * 28f * k, right));
            Set(_kneeR, _rKneeR, Quaternion.AngleAxis(-Mathf.Max(0f, -s) * 40f * k, right) * Quaternion.AngleAxis(-s * 28f * k, right));
            // Arms: drop to the sides, swing opposite to legs; forward when carrying; up when cheering.
            float downL = 78f - _armDownL, downR = 78f - _armDownR;
            Quaternion armL, armR;
            if (cheer) { float w = Mathf.Sin(Time.time * 14f) * 12f; armL = Quaternion.AngleAxis(-70f + w, fwd); armR = Quaternion.AngleAxis(70f - w, fwd); }
            else if (Carrying) { armL = Quaternion.AngleAxis(-65f, right) * Quaternion.AngleAxis(downL * 0.85f, fwd); armR = Quaternion.AngleAxis(-65f, right) * Quaternion.AngleAxis(-downR * 0.85f, fwd); }
            else { armL = Quaternion.AngleAxis(-s * 22f * k, right) * Quaternion.AngleAxis(downL, fwd); armR = Quaternion.AngleAxis(s * 22f * k, right) * Quaternion.AngleAxis(-downR, fwd); }
            Set(_armL, _rArmL, armL); Set(_armR, _rArmR, armR);
            Set(_foreL, _rForeL, armL * Quaternion.AngleAxis(Carrying ? -35f : -10f, right));
            Set(_foreR, _rForeR, armR * Quaternion.AngleAxis(Carrying ? -35f : -10f, right));
            // Spine & head
            Set(_spine, _rSpine, Quaternion.AngleAxis(breath + 4f * k, right) * Quaternion.AngleAxis(s * 4f * k, up));
            if (_head)
            {
                Quaternion look = Quaternion.identity;
                if (LookAt.HasValue)
                {
                    var d = Root.InverseTransformDirection(LookAt.Value - _head.position); d.y *= 0.5f;
                    if (d.sqrMagnitude > 0.01f) look = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(fwd, d.normalized), 40f);
                }
                Set(_head, _rHead, look * Quaternion.AngleAxis(Mathf.Sin(Time.time * 0.7f) * 3f, up));
            }
            // Bob
            _hips.position = Root.TransformPoint(_hipsPos + new Vector3(0, Mathf.Abs(s) * 0.025f * k - 0.01f * k + (cheer ? Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.06f : 0f), 0));
            Set(_hips, _rHips, Quaternion.AngleAxis(s * 3f * k, up));
        }
    }
}
