using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Meta hand tracking (OVRHand + OVRSkeleton) adapter. Supports both the legacy OVR skeleton and the OpenXR (XRHand_*)
    /// skeleton ids so it keeps working across Meta XR SDK versions.
    /// </summary>
    public sealed class OvrHandSource : HandSource
    {
        public OVRHand ovrHand;
        public OVRSkeleton skeleton;

        Transform _wrist, _palm, _thumbTip, _indexTip, _middleTip, _ringTip, _littleTip, _indexProx, _littleProx, _middleProx;
        /// <summary>The skeleton is the OpenXR hand skeleton (XRHand_* ids) - else the legacy OVR one (Hand_* ids).</summary>
        public bool XrSkeleton { get; private set; }
        public string SkeletonName => skeleton != null ? skeleton.GetSkeletonType().ToString() : "none";

        /// <summary>
        /// V12 fix (Quest run 2026-11-11): XRHand_* and Hand_* are values of ONE enum, both numbered from 0. Looking up an XRHand id on a
        /// legacy skeleton returned an unrelated legacy bone (XRHand_IndexTip = 10 = Hand_Middle2, XRHand_ThumbTip = 5 = Hand_Thumb3, ...):
        /// the pinch point sat inside the hand. Ids now come only from the set that matches the skeleton type.
        /// </summary>
        public static OVRSkeleton.BoneId Id(bool xr, OVRSkeleton.BoneId xrId, OVRSkeleton.BoneId legacyId) => xr ? xrId : legacyId;
        public static bool IsXr(OVRSkeleton.SkeletonType t) => t == OVRSkeleton.SkeletonType.XRHandLeft || t == OVRSkeleton.SkeletonType.XRHandRight;
        bool _bonesBound;
        float _grip;

        public override bool Tracked => ovrHand != null && ovrHand.IsTracked && ovrHand.IsDataValid && _bonesBound;
        public override bool HighConfidence => Tracked && ovrHand.IsDataHighConfidence;
        public override float Pinch => Tracked ? ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
        public override float Grip => Tracked ? _grip : 0f;
        public override bool SystemGesture => ovrHand != null && ovrHand.IsSystemGestureInProgress;
        public override Vector3 PinchPoint => _bonesBound ? Vector3.Lerp(_thumbTip.position, _indexTip.position, 0.5f) : transform.position;
        public override Vector3 IndexTip => _bonesBound ? _indexTip.position : transform.position;
        public override Vector3 PalmCenter => _bonesBound ? (_palm != null ? _palm.position : Vector3.Lerp(_wrist.position, _middleProx.position, 0.55f)) : transform.position;
        public override Quaternion HandRotation => _bonesBound ? Quaternion.LookRotation((_middleProx.position - _wrist.position).normalized, -PalmNormal) : transform.rotation;

        public override Vector3 PalmNormal
        {
            get
            {
                if (!_bonesBound) return -transform.up;
                Vector3 a = _indexProx.position - _wrist.position, b = _littleProx.position - _wrist.position;
                Vector3 n = Vector3.Cross(a, b).normalized;
                return Hand == Handedness.Left ? n : -n;
            }
        }

        public override Ray PointerRay
        {
            get
            {
                if (ovrHand != null && ovrHand.IsPointerPoseValid && ovrHand.PointerPose != null)
                    return new Ray(ovrHand.PointerPose.position, ovrHand.PointerPose.forward);
                return base.PointerRay;
            }
        }

        void Update()
        {
            if (!_bonesBound) TryBind();
            if (!_bonesBound) return;
            // V12 fix (owner, Quest c15: "grab works, ungrab does not - everything gets stuck"): grip used to be the tip-to-palm distance
            // divided by the wrist->knuckle length. Once the real palm joint was used (c15 bone-id fix), a relaxed OPEN hand read 0.3-0.6
            // (little / ring fingers are short) while release needs < 0.4: the hand never counted as open. Now each finger's own curl:
            // how straight it is along its own joints (tip-to-knuckle distance / finger length) - independent of hand size and palm joint.
            _curl[0] = Chain(_fIndex); _curl[1] = Chain(_fMiddle); _curl[2] = Chain(_fRing); _curl[3] = Chain(_fLittle);
            _grip = _fMiddle[0] != null ? (_curl[1] + _curl[2] + _curl[3]) / 3f : LegacyGrip();
            UpdateGun();
        }

        // ---- V13 finger gun (index straight, the other three curled; thumb up = cocked, thumb down = trigger)
        bool _gun, _thumbDown; Vector3 _gunDir = Vector3.forward; float _thumbRatio;
        public override bool GunPose => Tracked && _gun;
        public override bool ThumbDown => Tracked && _gun && _thumbDown;
        public override Ray FingerRay => _bonesBound ? new Ray(_indexTip.position, _gunDir) : base.PointerRay;
        /// <summary>QA / tuning: thumb-tip distance to the side of the index / middle finger, in hand lengths (down below ~0.32).</summary>
        public float ThumbRatio => _thumbRatio;

        void UpdateGun()
        {
            if (_fMiddle[1] == null || _fIndex[1] == null) { _gun = false; return; }
            float others = (_curl[1] + _curl[2] + _curl[3]) / 3f;
            bool was = _gun;
            // hysteresis: easier to stay in the pose than to enter it (tracking jitter must not drop an aim)
            _gun = _curl[0] < (was ? 0.42f : 0.3f) && others > (was ? 0.42f : 0.55f) && ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < 0.5f;
            float size = Mathf.Max(0.05f, Vector3.Distance(_wrist.position, _middleProx.position));
            Vector3 t = _thumbTip.position;
            float d = Mathf.Min(Mathf.Min(Vector3.Distance(t, _fIndex[0].position), Vector3.Distance(t, _fIndex[1].position)),
                                Mathf.Min(Vector3.Distance(t, Vector3.Lerp(_fIndex[0].position, _fIndex[1].position, 0.5f)), Vector3.Distance(t, _fMiddle[1].position)));
            _thumbRatio = d / size;
            bool td = _thumbDown ? _thumbRatio < 0.45f : _thumbRatio < 0.32f;
            // the finger direction (knuckle -> tip), blended with the hand's forward for stability, smoothed
            var dir = ((_indexTip.position - _fIndex[0].position).normalized * 0.7f + (_middleProx.position - _wrist.position).normalized * 0.3f).normalized;
            _gunDir = was ? Vector3.Slerp(_gunDir, dir, 1f - Mathf.Exp(-Time.deltaTime * 16f)) : dir;
            if (_gun != was || (_gun && td != _thumbDown)) GrabLog.Add($"{Hand} GUN {(_gun ? "on" : "off")} thumb {(td ? "down" : "up")} r={_thumbRatio:0.00} curl i{_curl[0]:0.00} m{_curl[1]:0.00} r{_curl[2]:0.00} l{_curl[3]:0.00}");
            _thumbDown = td;
        }

        readonly Transform[] _fIndex = new Transform[4], _fMiddle = new Transform[4], _fRing = new Transform[4], _fLittle = new Transform[4];
        readonly float[] _curl = new float[4];
        /// <summary>Curl 0 (straight) .. 1 (fist) of index, middle, ring, little.</summary>
        public float FingerCurlOf(int f) => _curl[Mathf.Clamp(f, 0, 3)];
        public override bool OpenHand => Tracked && Pinch < 0.3f && _fMiddle[0] != null && _curl[0] < 0.3f && _curl[1] < 0.3f && _curl[2] < 0.35f && _curl[3] < 0.4f;
        float Chain(Transform[] c) => c[0] != null && c[1] != null && c[2] != null && c[3] != null ? FingerCurl(c[0].position, c[1].position, c[2].position, c[3].position) : 0f;

        /// <summary>0 = straight finger, 1 = fully curled: tip-to-knuckle distance against the finger's own length.</summary>
        public static float FingerCurl(Vector3 prox, Vector3 inter, Vector3 distal, Vector3 tip)
        {
            float len = Vector3.Distance(prox, inter) + Vector3.Distance(inter, distal) + Vector3.Distance(distal, tip);
            if (len < 1e-4f) return 0f;
            float r = Vector3.Distance(prox, tip) / len;   // straight ~1.0, relaxed ~0.9, fist ~0.4-0.5
            return Mathf.Clamp01(Mathf.InverseLerp(0.9f, 0.55f, r));
        }

        float LegacyGrip()
        {
            Vector3 pc = PalmCenter; float size = Mathf.Max(0.05f, Vector3.Distance(_wrist.position, _middleProx.position));
            return (Curl(_middleTip.position, pc, size) + Curl(_ringTip.position, pc, size) + Curl(_littleTip.position, pc, size)) / 3f;
        }

        /// <summary>The c15 grip (kept for QA comparison).</summary>
        public static float Curl(Vector3 tip, Vector3 palm, float size)
        {
            float d = Vector3.Distance(tip, palm) / size; // open hand ~1.6-1.9, fist ~0.6-0.8
            return Mathf.Clamp01(Mathf.InverseLerp(1.55f, 0.85f, d));
        }

        void TryBind()
        {
            if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null || skeleton.Bones.Count == 0) return;
            var map = new Dictionary<OVRSkeleton.BoneId, Transform>();
            foreach (var b in skeleton.Bones) if (b != null && b.Transform != null) map[b.Id] = b.Transform;
            bool xr = IsXr(skeleton.GetSkeletonType()); XrSkeleton = xr;
            Transform Get(OVRSkeleton.BoneId xrId, OVRSkeleton.BoneId legacy) => map.TryGetValue(Id(xr, xrId, legacy), out var t) ? t : null;
            _wrist = Get(OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
            _palm = null; if (xr) map.TryGetValue(OVRSkeleton.BoneId.XRHand_Palm, out _palm);
            _thumbTip = Get(OVRSkeleton.BoneId.XRHand_ThumbTip, OVRSkeleton.BoneId.Hand_ThumbTip);
            _indexTip = Get(OVRSkeleton.BoneId.XRHand_IndexTip, OVRSkeleton.BoneId.Hand_IndexTip);
            _middleTip = Get(OVRSkeleton.BoneId.XRHand_MiddleTip, OVRSkeleton.BoneId.Hand_MiddleTip);
            _ringTip = Get(OVRSkeleton.BoneId.XRHand_RingTip, OVRSkeleton.BoneId.Hand_RingTip);
            _littleTip = Get(OVRSkeleton.BoneId.XRHand_LittleTip, OVRSkeleton.BoneId.Hand_PinkyTip);
            _indexProx = Get(OVRSkeleton.BoneId.XRHand_IndexProximal, OVRSkeleton.BoneId.Hand_Index1);
            _middleProx = Get(OVRSkeleton.BoneId.XRHand_MiddleProximal, OVRSkeleton.BoneId.Hand_Middle1);
            _littleProx = Get(OVRSkeleton.BoneId.XRHand_LittleProximal, OVRSkeleton.BoneId.Hand_Pinky1);
            _fIndex[0] = _indexProx; _fIndex[1] = Get(OVRSkeleton.BoneId.XRHand_IndexIntermediate, OVRSkeleton.BoneId.Hand_Index2); _fIndex[2] = Get(OVRSkeleton.BoneId.XRHand_IndexDistal, OVRSkeleton.BoneId.Hand_Index3); _fIndex[3] = _indexTip;
            _fMiddle[0] = _middleProx; _fMiddle[1] = Get(OVRSkeleton.BoneId.XRHand_MiddleIntermediate, OVRSkeleton.BoneId.Hand_Middle2); _fMiddle[2] = Get(OVRSkeleton.BoneId.XRHand_MiddleDistal, OVRSkeleton.BoneId.Hand_Middle3); _fMiddle[3] = _middleTip;
            _fRing[0] = Get(OVRSkeleton.BoneId.XRHand_RingProximal, OVRSkeleton.BoneId.Hand_Ring1); _fRing[1] = Get(OVRSkeleton.BoneId.XRHand_RingIntermediate, OVRSkeleton.BoneId.Hand_Ring2); _fRing[2] = Get(OVRSkeleton.BoneId.XRHand_RingDistal, OVRSkeleton.BoneId.Hand_Ring3); _fRing[3] = _ringTip;
            _fLittle[0] = _littleProx; _fLittle[1] = Get(OVRSkeleton.BoneId.XRHand_LittleIntermediate, OVRSkeleton.BoneId.Hand_Pinky2); _fLittle[2] = Get(OVRSkeleton.BoneId.XRHand_LittleDistal, OVRSkeleton.BoneId.Hand_Pinky3); _fLittle[3] = _littleTip;
            _bonesBound = _wrist && _thumbTip && _indexTip && _middleTip && _ringTip && _littleTip && _indexProx && _middleProx && _littleProx;
            if (_bonesBound) { Debug.Log($"[SHHH] {Hand} hand skeleton bound ({skeleton.GetSkeletonType()}, {skeleton.Bones.Count} bones, {(xr ? "OpenXR" : "legacy")} ids)"); GrabLog.Add($"{Hand} skeleton {skeleton.GetSkeletonType()} {skeleton.Bones.Count} bones"); }
        }
    }
}
