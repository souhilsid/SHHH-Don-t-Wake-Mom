using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SHHH.Game
{
    /// <summary>
    /// V12 owner pets ("Free Pack - Cartoon Dog, Cat", PolyOne Studio, Unity Asset Store EULA): the pack ships an Idle and a Run clip per
    /// animal (authored non-looping, demo controllers without parameters). A tiny PlayableGraph plays both, loops them by wrapping their
    /// time, cross-fades by the pet's real ground speed and speeds the run up with it - on the host (NavMesh pets) and on clients (network
    /// puppets) alike, since speed is measured from actual motion.
    /// </summary>
    public sealed class PetAnimator : MonoBehaviour
    {
        PlayableGraph _graph; AnimationMixerPlayable _mix; AnimationClipPlayable _idle, _run;
        float _idleLen, _runLen, _tIdle, _tRun, _w, _speed; Vector3 _last; bool _ok;
        public float RunSpeedForClip = 1.4f;   // ground speed (m/s) at which the run clip plays at 1x
        public float Speed => _speed;
        /// <summary>Eating / excited: a small extra bob of the run clip when standing (looks like eager trotting in place).</summary>
        public float Excite;

        public static PetAnimator Attach(GameObject model, AnimationClip idle, AnimationClip run)
        {
            var anim = model.GetComponentInChildren<Animator>(); if (anim == null) anim = model.AddComponent<Animator>();
            // The pack's clips key the animated root itself (path "": scale 1, position 0, rotation) - they would undo the size fit and
            // the facing applied to that object. Move its local pose onto a new parent so the clips only drive the skeleton.
            var at = anim.transform;
            var fit = new GameObject("fit").transform; fit.SetParent(at.parent, false);
            fit.localPosition = at.localPosition; fit.localRotation = at.localRotation; fit.localScale = at.localScale;
            at.SetParent(fit, false); at.localPosition = Vector3.zero; at.localRotation = Quaternion.identity; at.localScale = Vector3.one;
            anim.runtimeAnimatorController = null; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var pa = model.AddComponent<PetAnimator>(); pa.Build(anim, idle, run);
            pa.FaceForward(at, fit);
            return pa;
        }

        void Build(Animator anim, AnimationClip idle, AnimationClip run)
        {
            if (idle == null || run == null) return;
            _graph = PlayableGraph.Create("PetAnim_" + name); _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "out", anim);
            _mix = AnimationMixerPlayable.Create(_graph, 2);
            _idle = AnimationClipPlayable.Create(_graph, idle); _run = AnimationClipPlayable.Create(_graph, run);
            _idle.SetApplyFootIK(false); _run.SetApplyFootIK(false);
            _graph.Connect(_idle, 0, _mix, 0); _graph.Connect(_run, 0, _mix, 1);
            output.SetSourcePlayable(_mix);
            _idleLen = Mathf.Max(0.05f, idle.length); _runLen = Mathf.Max(0.05f, run.length);
            _tIdle = Random.value * _idleLen; _last = transform.position; _ok = true;
        }

        /// <summary>Turn the fit node about Y so the posed head points along the pet's forward (+Z of the pet body), whatever axis the pack used.</summary>
        void FaceForward(Transform animRoot, Transform fit)
        {
            if (_ok) _graph.Evaluate(0f);
            Transform head = null, tail = null;
            foreach (var t in animRoot.GetComponentsInChildren<Transform>(true)) { if (t.name == "Head") head = t; else if (t.name == "Tail") tail = t; }
            var body = fit.parent; if (head == null || tail == null || body == null) { Debug.LogWarning("[SHHH][pets] facing: Head/Tail bone not found"); return; }
            var d = body.InverseTransformDirection(head.position - tail.position); d.y = 0f;
            if (d.sqrMagnitude < 1e-8f) return;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            var pivot = body.TransformPoint(new Vector3(0f, 0f, 0f));
            fit.RotateAround(pivot, body.up, -yaw);
            Debug.Log($"[SHHH][pets] {body.name} facing corrected by {-yaw:0} deg (head was at {yaw:0} deg from forward)");
        }

        void LateUpdate()
        {
            if (!_ok) return;
            float dt = Time.deltaTime;
            var p = transform.position; var d = p - _last; d.y = 0f; _last = p;
            _speed = Mathf.Lerp(_speed, d.magnitude / Mathf.Max(1e-3f, dt), 0.25f);
            float target = Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.45f, _speed) + Excite * 0.35f);
            _w = Mathf.MoveTowards(_w, target, dt * 4f);
            float runRate = Mathf.Clamp(_speed / RunSpeedForClip, 0.6f, 1.8f);
            _tIdle = Mathf.Repeat(_tIdle + dt, _idleLen); _tRun = Mathf.Repeat(_tRun + dt * (_speed > 0.08f ? runRate : 0.8f), _runLen);
            _idle.SetTime(_tIdle); _run.SetTime(_tRun);
            _mix.SetInputWeight(0, 1f - _w); _mix.SetInputWeight(1, _w);
            _graph.Evaluate(0f);
        }

        void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }
    }
}
