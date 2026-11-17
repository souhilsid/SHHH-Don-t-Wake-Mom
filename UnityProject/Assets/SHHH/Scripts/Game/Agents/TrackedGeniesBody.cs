using System;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 full-body tracking for a Genies humanoid (the player's own body and every friend's body). Concept (Classroom-VR "VRMap",
    /// reference only): the headset and the two hands drive the avatar's IK goals, the body stands under the head and turns with it.
    /// Our own implementation on Unity's humanoid IK:
    ///  - the body is scaled so its eyes sit at the player's measured standing eye height (feet stay on the floor);
    ///  - crouching lowers the hips and the feet stay planted (the knees bend); walking plays the locomotion clip by real speed;
    ///  - each hand goes to its tracked pinch point with the tracked rotation (calibrated once from the avatar's own hand bones);
    ///  - the head follows the headset (yaw / pitch / roll), shared down the neck, chest and spine so looking down bends the body;
    ///  - the body turns only when the head turns far past the shoulders, or when walking;
    ///  - fingers curl with the grip (friends see who is holding something); the player's own head and hands are hidden for
    ///    their own camera only (their gloves are their hands, so finger tracking stays exact).
    /// Pose inputs are the same on every peer (local rig, or the replicated head / hand poses), so everyone sees the same body.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class TrackedGeniesBody : MonoBehaviour
    {
        public Animator A;
        /// <summary>Moved and turned under the head (the avatar's parent object).</summary>
        public Transform Mover;
        /// <summary>The Genies root (its local scale is the body size).</summary>
        public Transform ScaleRoot;
        public SiblingAvatar Avatar;
        public bool Self;
        public Func<Pose?> HeadPose, LeftPose, RightPose;
        public Func<float> LeftGrip, RightGrip;
        /// <summary>The hand poses are wrist positions already (my own gloves' wrist bones), not pinch points.</summary>
        public bool WristPoses;

        Transform _head, _neck, _chest, _upperChest, _spine;
        readonly Transform[] _hand = new Transform[2], _midProx = new Transform[2], _idxProx = new Transform[2], _litProx = new Transform[2];
        readonly Quaternion[] _goalOffset = new Quaternion[2], _animGoal = new Quaternion[2];
        readonly bool[] _calib = new bool[2], _calibPending = new bool[2];
        readonly float[] _reach = new float[2], _armLen = new float[2], _stretch = { 1f, 1f };
        readonly Transform[] _upperArm = new Transform[2], _lowerArm = new Transform[2];
        float _eyeAt1 = -1f, _eye = 1.5f, _scale = -1f, _yaw, _speed, _crouch, _floor, _ankleAt1 = 0.08f, _lean, _corrY;
        Vector3 _corrXZ;
        bool _turning, _hasHead, _ready, _placed;
        Vector3 _lastRoot;
        Pose _headP;

        // QA / evidence (read by the labs and the network test)
        public float Scale => _scale;
        public float EyeHeight => _eye;
        public float CrouchDepth => _crouch;
        public float HandErrorL { get; private set; }
        public float HandErrorR { get; private set; }
        public float HandAngleErrL { get; private set; }
        public float HandAngleErrR { get; private set; }
        public float HeadError { get; private set; }
        public float FootLift { get; private set; }
        public bool Calibrated => _calib[0] && _calib[1];
        /// <summary>My own body: upper arms removed + forearms slimmed on my camera's copy (Quest run: the sleeve hid my hands).</summary>
        public SelfArmTrim ArmTrim;
        /// <summary>V12 owner 2026-11-12: my own body shows the complete outfit and my real Genies hands (was: upper arms cut away, gloves).</summary>
        public static bool SelfForearmsOnly = false;
        public static float SelfSlim = 0.85f;
        /// <summary>My own body stands this much further back under my eyes (shoulders behind the view, so the sleeves stay low).</summary>
        public static float SelfBack = 0.05f;
        /// <summary>Tracked hand joints for my own fingers: (hand 0/1, joint) -> world position. Joints: 0 wrist, 1 palm, 2/3 thumb
        /// proximal/distal, 4-6 index, 7-9 middle, 10-12 ring, 13-15 little (proximal, intermediate, distal), 16-20 tips.</summary>
        public Func<int, int, Vector3?> JointOf;
        /// <summary>QA: the fingers followed tracked joints this frame (else they curl with the grip).</summary>
        public bool[] FingersTracked = new bool[2];
        void OnDestroy() { ArmTrim?.Dispose(); }

        static readonly HumanBodyBones[][] Fingers =
        {
            new[] { HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal },
            new[] { HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal },
            new[] { HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal },
            new[] { HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal },
            new[] { HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal },
            new[] { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal },
            new[] { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal },
            new[] { HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal },
            new[] { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal },
            new[] { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal },
        };
        Transform[][] _fingers;

        public static TrackedGeniesBody Attach(SiblingAvatar avatar, Transform mover, bool self, Func<Pose?> head, Func<Pose?> left, Func<Pose?> right, Func<float> gripL, Func<float> gripR)
        {
            var a = avatar != null ? avatar.GeniesAnimator : null;
            if (a == null || !a.isHuman) { Debug.LogWarning($"[SHHH][body] seat {avatar?.Slot}: no humanoid Genies animator - body not tracked"); return null; }
            var t = a.GetComponent<TrackedGeniesBody>() ?? a.gameObject.AddComponent<TrackedGeniesBody>();
            t.A = a; t.Mover = mover; t.ScaleRoot = avatar.GeniesRoot; t.Avatar = avatar; t.Self = self;
            t.HeadPose = head; t.LeftPose = left; t.RightPose = right; t.LeftGrip = gripL; t.RightGrip = gripR;
            avatar.Tracked = t;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (self) foreach (var c in a.GetComponentsInChildren<Collider>(true)) Destroy(c);   // my own body never blocks my hands or props
            if (self) { t.ArmTrim = SelfArmTrim.Build(a, SelfForearmsOnly ? 0.7f : SelfSlim, SelfForearmsOnly); t.ArmTrim.Set(true); Debug.Log($"[SHHH][body] my arms: {t.ArmTrim.Report}"); }
            // V12: no real-time shadows are rendered on the Quest (URP main-light shadows off) - casting just costs; my own hands keep 4-bone skinning
            foreach (var r in a.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (self) foreach (var smr in a.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.quality = SkinQuality.Bone4;
            Debug.Log($"[SHHH][body] seat {avatar.Slot}: tracked Genies body ({(self ? "my own body - complete outfit + my Genies hands, head hidden for my camera" : "friend")})");
            return t;
        }

        void Init()
        {
            _head = A.GetBoneTransform(HumanBodyBones.Head); _neck = A.GetBoneTransform(HumanBodyBones.Neck);
            _chest = A.GetBoneTransform(HumanBodyBones.Chest); _upperChest = A.GetBoneTransform(HumanBodyBones.UpperChest); _spine = A.GetBoneTransform(HumanBodyBones.Spine);
            _hand[0] = A.GetBoneTransform(HumanBodyBones.LeftHand); _hand[1] = A.GetBoneTransform(HumanBodyBones.RightHand);
            _midProx[0] = A.GetBoneTransform(HumanBodyBones.LeftMiddleProximal); _midProx[1] = A.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            _idxProx[0] = A.GetBoneTransform(HumanBodyBones.LeftIndexProximal); _idxProx[1] = A.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            _litProx[0] = A.GetBoneTransform(HumanBodyBones.LeftLittleProximal); _litProx[1] = A.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            _fingers = new Transform[Fingers.Length][];
            for (int i = 0; i < Fingers.Length; i++) { _fingers[i] = new Transform[3]; for (int k = 0; k < 3; k++) _fingers[i][k] = A.GetBoneTransform(Fingers[i][k]); }
            float s = ScaleRoot != null ? Mathf.Max(1e-3f, ScaleRoot.lossyScale.y) : 1f;
            // eyes ~ 9 cm above the head bone (Genies head pivot is at the skull base)
            _eyeAt1 = _head != null ? (_head.position.y - A.transform.position.y) / s + 0.09f : 1.3f;
            { var lf0 = A.GetBoneTransform(HumanBodyBones.LeftFoot); var rf0 = A.GetBoneTransform(HumanBodyBones.RightFoot);
              if (lf0 != null && rf0 != null) _ankleAt1 = Mathf.Clamp((Mathf.Min(lf0.position.y, rf0.position.y) - A.transform.position.y) / s, 0f, 0.2f); }
            _upperArm[0] = A.GetBoneTransform(HumanBodyBones.LeftUpperArm); _upperArm[1] = A.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lowerArm[0] = A.GetBoneTransform(HumanBodyBones.LeftLowerArm); _lowerArm[1] = A.GetBoneTransform(HumanBodyBones.RightLowerArm);
            for (int h = 0; h < 2; h++) _armLen[h] = _upperArm[h] != null && _lowerArm[h] != null && _hand[h] != null ? (Vector3.Distance(_upperArm[h].position, _lowerArm[h].position) + Vector3.Distance(_lowerArm[h].position, _hand[h].position)) / s : 0.5f;
            for (int h = 0; h < 2; h++) _reach[h] = _hand[h] != null && _midProx[h] != null ? Vector3.Distance(_hand[h].position, _midProx[h].position) / s * 1.45f : 0.1f;
            _ready = _head != null && _eyeAt1 > 0.5f;
            Debug.Log($"[SHHH][body] seat {Avatar?.Slot}: eye height at scale 1 = {_eyeAt1:0.00} m, pinch reach {_reach[0]:0.000} m, bones head {_head != null} hands {_hand[0] != null}/{_hand[1] != null} fingers {_fingers[1][0] != null}");
        }

        float FloorY()
        {
            var room = GameServices.Room; return room != null ? room.transform.position.y : 0f;
        }

        void Update()
        {
            if (A == null || Mover == null) return;
            if (_eyeAt1 < 0f) Init();
            if (!_ready) return;
            // my own body: head / hands were collapsed for my camera last frame - restore them before the animation and IK run
            if (Self) { if (_head != null) _head.localScale = Vector3.one; }
            for (int h = 0; h < 2; h++) if (_upperArm[h] != null) _upperArm[h].localScale = Vector3.one * _stretch[h];
            var hp = HeadPose?.Invoke(); _hasHead = hp.HasValue; if (!_hasHead) return;
            _headP = hp.Value; float dt = Mathf.Max(1e-4f, Time.deltaTime);
            _floor = FloorY(); float rel = _headP.position.y - _floor;
            // standing eye height: rises at once, sinks only slowly and only while (nearly) standing - a crouch is not a new height
            if (_scale < 0f) _eye = Mathf.Clamp(rel, 0.9f, 1.9f);
            else if (rel > _eye) _eye = Mathf.Lerp(_eye, rel, 1f - Mathf.Exp(-4f * dt));
            else if (rel > _eye * 0.92f) _eye = Mathf.Lerp(_eye, rel, dt * 0.05f);
            _eye = Mathf.Clamp(_eye, 0.9f, 1.9f);
            float want = _eye / _eyeAt1;
            _scale = _scale < 0f ? want : Mathf.MoveTowards(_scale, want, dt * 0.25f);
            if (ScaleRoot != null) ScaleRoot.localScale = Vector3.one * _scale;
            _crouch = Mathf.Clamp(_eye - rel, 0f, _eye * 0.5f);
            // body yaw: follows the head when it turns far past the shoulders or while walking
            var f = Vector3.ProjectOnPlane(_headP.rotation * Vector3.forward, Vector3.up);
            if (f.sqrMagnitude < 0.04f) f = Vector3.ProjectOnPlane(_headP.rotation * Vector3.up, Vector3.up);
            float headYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            float d = Mathf.DeltaAngle(_yaw, headYaw);
            if (!_placed) { _yaw = headYaw; d = 0f; }
            if (Mathf.Abs(d) > 55f || _speed > 0.35f) _turning = true;
            if (_turning) { _yaw = Mathf.MoveTowardsAngle(_yaw, headYaw, (_speed > 0.35f ? 260f : 200f) * dt); if (Mathf.Abs(Mathf.DeltaAngle(_yaw, headYaw)) < 4f) _turning = false; }
            var rot = Quaternion.Euler(0f, _yaw, 0f);
            // stand under the head (the neck is a little behind the eyes)
            var root = new Vector3(_headP.position.x, _floor, _headP.position.z) - rot * Vector3.forward * ((0.07f + (Self ? SelfBack : 0f)) * _scale) + _corrXZ;
            if (!_placed) { Mover.position = root; _lastRoot = root; _placed = true; }
            Mover.position = Vector3.Lerp(Mover.position, root, 1f - Mathf.Exp(-18f * dt));
            Mover.rotation = rot;
            var v = Mover.position - _lastRoot; v.y = 0f; _lastRoot = Mover.position;
            _speed = Mathf.Lerp(_speed, v.magnitude / dt, 0.15f);
            Avatar?.SetMotion(_speed, false);
            Avatar?.SetPlateHeight(_eye + 0.28f);
        }

        Vector3 Wrist(int h, Pose p, Quaternion want) => WristPoses ? p.position : p.position - (want * Vector3.forward) * (_reach[h] * _scale);
        public float StretchL => _stretch[0];
        public float StretchR => _stretch[1];

        Quaternion HandFrame(int h)
        {
            var fingers = (_midProx[h].position - _hand[h].position).normalized;
            var side = _idxProx[h].position - _litProx[h].position; side = Vector3.ProjectOnPlane(side, fingers).normalized;
            return Quaternion.LookRotation(fingers, Vector3.Cross(fingers, side));
        }

        /// <summary>
        /// V12 fix (owner, Quest c15: "my hands' rotation is not the same as in real life - in the main menu it worked"). A tracked hand pose
        /// (OvrHandSource.HandRotation, also what friends receive) means forward = where the fingers point, up = the BACK of the hand.
        /// HandFrame (the Genies hand) is built as forward = fingers, up = cross(fingers, index-side), which is the PALM side on the right
        /// hand and the BACK on the left. The old code used cross(fingers, back) - a sideways axis - so every hand was rolled ~90 deg
        /// (opposite ways left / right). The labs only compared the body with the rotation it was given, never with a real hand.
        /// </summary>
        /// <summary>QA only: use the old (c15) conversion to measure what the owner saw.</summary>
        public static bool QaOldHandFrame;
        static Quaternion WantedFrame(int h, Pose p)
        {
            var fwd = p.rotation * Vector3.forward; var back = p.rotation * Vector3.up;
            if (QaOldHandFrame) return Quaternion.LookRotation(fwd, Vector3.Cross(fwd, back));   // QA only: the c15 conversion, for comparison
            return Quaternion.LookRotation(fwd, h == 1 ? -back : back);
        }

        void OnAnimatorIK(int layer)
        {
            if (!_ready || !_hasHead) return;
            var rot = Mover.rotation; var fwd = rot * Vector3.forward; var right = rot * Vector3.right;
            // crouch: hips go down, feet stay planted where the clip put them (knees bend forward)
            // feet: planted when standing still; while walking the clip moves them, but crouch-walking keeps them on the floor too
            float fw = Mathf.Max(Mathf.Clamp01(1f - _speed / 0.3f), Mathf.Clamp01(_crouch / 0.25f));
            A.bodyPosition += Vector3.up * (_corrY - _crouch * 0.92f);
            // hip hinge: lean forward with the gaze and further when the hands reach low (done BEFORE the arm IK, so the arms still land)
            float headPitch = Mathf.DeltaAngle(0f, (Quaternion.Inverse(rot) * _headP.rotation).eulerAngles.x);
            float handsY = 0f; int nh = 0;
            { var lp = LeftPose?.Invoke(); var rp = RightPose?.Invoke(); if (lp.HasValue) { handsY += lp.Value.position.y; nh++; } if (rp.HasValue) { handsY += rp.Value.position.y; nh++; } }
            float shoulderY = A.bodyPosition.y + 0.42f * _scale;
            float low = nh > 0 ? Mathf.Clamp01((shoulderY - handsY / nh - 0.35f * _scale) / (0.45f * _scale)) : 0f;
            float leanWant = Mathf.Clamp(Mathf.Max(0f, headPitch) * 0.35f + low * 35f, 0f, 50f);
            _lean = Mathf.Lerp(_lean, leanWant, 1f - Mathf.Exp(-10f * Time.deltaTime));
            if (_lean > 0.5f) A.bodyRotation = Quaternion.AngleAxis(_lean, right) * A.bodyRotation;
            foreach (var g in new[] { AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot })
            {
                var p = A.GetIKPosition(g);
                // never below the floor (the hips may be lowered a lot when crouching)
                p.y = Mathf.Max(p.y, _floor + _ankleAt1 * _scale);
                A.SetIKPositionWeight(g, fw); A.SetIKPosition(g, p);
                var hint = g == AvatarIKGoal.LeftFoot ? AvatarIKHint.LeftKnee : AvatarIKHint.RightKnee;
                A.SetIKHintPositionWeight(hint, fw * Mathf.Clamp01(_crouch / 0.2f)); A.SetIKHintPosition(hint, p + fwd * (0.5f * _scale) + Vector3.up * (0.35f * _scale));
            }
            for (int h = 0; h < 2; h++)
            {
                var goal = h == 0 ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
                var pose = (h == 0 ? LeftPose : RightPose)?.Invoke();
                if (!pose.HasValue || _hand[h] == null || _midProx[h] == null || _idxProx[h] == null || _litProx[h] == null)
                { A.SetIKPositionWeight(goal, 0f); A.SetIKRotationWeight(goal, 0f); continue; }
                if (!_calib[h])
                {
                    // one frame without IK: the animated goal and the hand bones it produces give the goal -> hand-frame offset
                    _animGoal[h] = A.GetIKRotation(goal); _calibPending[h] = true;
                    A.SetIKPositionWeight(goal, 0f); A.SetIKRotationWeight(goal, 0f); continue;
                }
                var want = WantedFrame(h, pose.Value);
                var goalRot = want * Quaternion.Inverse(_goalOffset[h]);
                var wrist = Wrist(h, pose.Value, want);
                A.SetIKPositionWeight(goal, 1f); A.SetIKRotationWeight(goal, 1f);
                A.SetIKPosition(goal, wrist); A.SetIKRotation(goal, goalRot);
                var elbow = h == 0 ? AvatarIKHint.LeftElbow : AvatarIKHint.RightElbow;
                A.SetIKHintPositionWeight(elbow, 0.5f);
                A.SetIKHintPosition(elbow, wrist + (h == 0 ? -right : right) * (0.3f * _scale) - Vector3.up * (0.3f * _scale) - fwd * (0.1f * _scale));
            }
        }

        void LateUpdate()
        {
            if (!_ready || !_hasHead) return;
            for (int h = 0; h < 2; h++)
                if (_calibPending[h] && _hand[h] != null) { _goalOffset[h] = Quaternion.Inverse(_animGoal[h]) * HandFrame(h); _calib[h] = true; _calibPending[h] = false; }
            // head: the headset orientation relative to the body, shared down the spine (looking down bends the back a little)
            var B = Mover.rotation * Quaternion.Euler(_lean, 0f, 0f);
            var rel = (Quaternion.Inverse(B) * _headP.rotation).eulerAngles;
            float yaw = Mathf.Clamp(Mathf.DeltaAngle(0f, rel.y), -75f, 75f), pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, rel.x), -55f, 75f), roll = Mathf.Clamp(Mathf.DeltaAngle(0f, rel.z), -35f, 35f);
            var D = B * Quaternion.Euler(pitch, yaw, roll) * Quaternion.Inverse(B);
            float rest = 1f;
            void Share(Transform bone, float w) { if (bone == null || w <= 0f) return; bone.rotation = Quaternion.Slerp(Quaternion.identity, D, w) * bone.rotation; rest -= w; }
            Share(_neck, 0.35f); Share(_head, rest);
            // fingers: my own hands follow my tracked finger joints; friends' (and untracked) hands curl with the grip
            for (int h = 0; h < 2; h++)
            {
                FingersTracked[h] = Self && JointOf != null && RetargetFingers(h);
                if (FingersTracked[h]) continue;
                float g = Mathf.Clamp01((h == 0 ? LeftGrip : RightGrip)?.Invoke() ?? 0.15f);
                for (int fi = h * 5; fi < h * 5 + 5; fi++) Curl(_fingers[fi], g, fi % 5 == 4);
            }
            // evidence: how close the body is to the tracking
            if (_head != null)
            {
                var eye = _head.position + Mover.rotation * new Vector3(0f, 0.09f, 0.07f + (Self ? SelfBack : 0f)) * _scale;
                var e = _headP.position - eye; HeadError = e.magnitude;
                _corrXZ = Vector3.ClampMagnitude(_corrXZ + new Vector3(e.x, 0f, e.z) * 0.25f, 0.35f * _scale);
                // up only a little: a head held higher than the body can reach must not lift the feet off the floor
                _corrY = Mathf.Clamp(_corrY + e.y * 0.25f, -0.3f * _scale, 0.05f * _scale);
            }
            for (int h = 0; h < 2; h++)
            {
                var pose = (h == 0 ? LeftPose : RightPose)?.Invoke(); if (!pose.HasValue || !_calib[h] || _hand[h] == null) continue;
                var want = WantedFrame(h, pose.Value); var wrist = Wrist(h, pose.Value, want);
                // a kid-proportioned body scaled to an adult reaches less far: let the arm stretch a little (max 25 %) only when needed
                if (_upperArm[h] != null)
                {
                    float need = Vector3.Distance(_upperArm[h].position, wrist) / Mathf.Max(1e-3f, _armLen[h] * _scale);
                    _stretch[h] = Mathf.MoveTowards(_stretch[h], Mathf.Clamp(need * 1.02f, 1f, 1.25f), Time.deltaTime * 1.5f);
                }
                float err = Vector3.Distance(_hand[h].position, wrist), ang = Quaternion.Angle(HandFrame(h), want);
                if (h == 0) { HandErrorL = err; HandAngleErrL = ang; } else { HandErrorR = err; HandAngleErrR = ang; }
            }
            var lf = A.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = A.GetBoneTransform(HumanBodyBones.RightFoot);
            if (lf != null && rf != null) FootLift = Mathf.Min(lf.position.y, rf.position.y) - _floor - _ankleAt1 * _scale;
            // my own body: no head in front of my eyes (my hands stay - they are my hands now)
            if (Self && _head != null) _head.localScale = Vector3.one * 0.001f;
        }

        /// <summary>
        /// Rotation-only retarget of my tracked fingers onto the Genies hand: each finger bone is turned so it points the way my real
        /// finger segment points, measured in the hand's own frame (wrist -> middle knuckle forward, index -> little knuckle across), so
        /// hand size and rest pose differences do not matter. Parents first (turning a knuckle moves its children).
        /// </summary>
        bool RetargetFingers(int h)
        {
            Vector3? J(int i) => JointOf(h, i);
            var w = J(0); var mp = J(7); var ip = J(4); var lp = J(13);
            if (!w.HasValue || !mp.HasValue || !ip.HasValue || !lp.HasValue || _hand[h] == null || _midProx[h] == null) return false;
            var tf = mp.Value - w.Value; var ts = Vector3.ProjectOnPlane(ip.Value - lp.Value, tf);
            if (tf.sqrMagnitude < 1e-6f || ts.sqrMagnitude < 1e-8f) return false;
            var T = Quaternion.LookRotation(tf.normalized, Vector3.Cross(tf.normalized, ts.normalized));
            var M = HandFrame(h) * Quaternion.Inverse(T);   // tracked hand frame -> Genies hand frame
            // tracked chains: index..little = [prox, inter, distal, tip]; thumb = [wrist, proximal, distal, tip]
            int[][] chains = { new[] { 0, 2, 3, 16 }, new[] { 4, 5, 6, 17 }, new[] { 7, 8, 9, 18 }, new[] { 10, 11, 12, 19 }, new[] { 13, 14, 15, 20 } };
            int[] genies = { 4, 0, 1, 2, 3 };   // Fingers[] order per hand: index, middle, ring, little, thumb
            int done = 0;
            for (int f = 0; f < 5; f++)
            {
                var bones = _fingers[h * 5 + genies[f]]; var c = chains[f];
                if (bones == null) continue;
                for (int k = 0; k < 3; k++)
                {
                    var b = bones[k]; if (b == null) continue;
                    var a0 = J(c[k]); var a1 = J(c[k + 1]); if (!a0.HasValue || !a1.HasValue) continue;
                    var next = k < 2 ? bones[k + 1] : null;
                    Vector3 cur = next != null ? next.position - b.position : (b.childCount > 0 ? b.GetChild(0).position - b.position : Vector3.zero);
                    if (cur.sqrMagnitude < 1e-10f) continue;
                    var want = M * (a1.Value - a0.Value); if (want.sqrMagnitude < 1e-10f) continue;
                    b.rotation = Quaternion.FromToRotation(cur, want) * b.rotation; done++;
                }
            }
            return done >= 10;
        }

        void Curl(Transform[] chain, float grip, bool thumb)
        {
            if (chain == null || chain[0] == null || chain[1] == null || chain[2] == null) return;
            // the finger's own bend axis (from its relaxed curve); fall back to the knuckle line
            var v1 = chain[1].position - chain[0].position; var v2 = chain[2].position - chain[1].position;
            var axis = Vector3.Cross(v1, v2);
            if (axis.sqrMagnitude < 1e-10f) return;
            axis.Normalize();
            float k = thumb ? 0.45f : 1f;
            float[] max = { 55f, 75f, 45f };
            for (int i = 0; i < 3; i++) chain[i].rotation = Quaternion.AngleAxis(max[i] * grip * k, axis) * chain[i].rotation;
        }
    }
}
