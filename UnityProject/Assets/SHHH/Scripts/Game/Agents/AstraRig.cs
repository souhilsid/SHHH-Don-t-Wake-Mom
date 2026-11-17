using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SHHH.Game
{
    /// <summary>
    /// Plays Astra character art at runtime without editor-made controllers: the light V2 "_Reduced" skinned mesh driven by
    /// a Playables mixer (idle / walk / run blended by speed + one one-shot action slot). V2/V3 clips bind by bone path;
    /// the reduced mesh's "_Reduced" node suffixes are stripped first so both clip sets bind. Root translation in the
    /// clips is pinned (locomotion comes from NavMesh/network, never applied twice).
    /// </summary>
    public sealed class AstraRig : MonoBehaviour
    {
        public CharacterCatalog.Entry Entry;
        public Transform Model, Head, Root;
        PlayableGraph _g;
        AnimationMixerPlayable _mix;
        readonly AnimationClipPlayable[] _loop = new AnimationClipPlayable[3];
        readonly float[] _len = new float[3];
        AnimationClipPlayable _shot; float _shotT = -1f, _shotLen; bool _shotHold;
        float _speed, _phase;
        Vector3 _rootRest;
        public float WalkSpeedForClip = 0.9f, RunSpeedForClip = 2.2f;
        string _loopOverride;

        /// <summary>True when the model was turned 180° so its face points along +Z.</summary>
        public bool FacingFixed { get; private set; }

        public static AstraRig Create(Transform parent, CharacterCatalog.Entry e, float height = -1f)
        {
            if (e == null || e.model == null) return null;
            var holder = new GameObject("astra_" + e.key);
            holder.transform.SetParent(parent, false);
            var rig = holder.AddComponent<AstraRig>();
            rig.Build(e, height > 0 ? height : e.height);
            return rig;
        }

        void Build(CharacterCatalog.Entry e, float height)
        {
            Entry = e;
            var inst = Instantiate(e.model, transform);
            Model = inst.transform;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var a in inst.GetComponentsInChildren<Animator>()) Destroy(a);
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("_Reduced")) t.name = t.name.Substring(0, t.name.Length - 8);
                if (Head == null && t.name == "Head") Head = t;
                if (Root == null && t.name == "Root") Root = t;
                // helper empties exported for authoring (grip/contact/camera-cull markers) must never render
                if (t.name.StartsWith("LOCAL_HeadCull") || t.name.StartsWith("POSE_") || t.name.StartsWith("CONTACT_") || t.name.StartsWith("TARGET_")) { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; }
            }
            Mats.MakeUrpSafe(inst);
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = false; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            // uniform scale to the requested height, feet on the ground (bind pose bounds)
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one;
            var b = Prim.RendererBounds(inst);
            float s = b.size.y > 1e-3f ? height / b.size.y : 1f;
            inst.transform.localScale = Vector3.one * s;
            b = Prim.RendererBounds(inst);
            inst.transform.position += transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            if (Root != null) _rootRest = Root.localPosition;
            // V7: the sibling FBXs face -Z; the game moves bodies along +Z. Detect the face (mouth/eyes renderer in front of the head
            // bone) and turn the model so every character faces where it walks (verified by recipe_diag siblings: face_forward).
            if (Head != null)
            {
                Renderer face = null;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) { var n = r.name.ToLowerInvariant(); if (n.Contains("mouth") || n.Contains("face") || n.Contains("eye")) { face = r; break; } }
                if (face != null && transform.InverseTransformPoint(face.bounds.center).z < transform.InverseTransformPoint(Head.position).z - 0.01f)
                { inst.transform.RotateAround(transform.position, transform.up, 180f); FacingFixed = true; }
            }

            var anim = inst.AddComponent<Animator>();
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            _g = PlayableGraph.Create("Astra_" + e.key);
            _g.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(_g, "anim", anim);
            _mix = AnimationMixerPlayable.Create(_g, 4);
            output.SetSourcePlayable(_mix);
            string[] loops = { "idle", "walk", "run" };
            for (int i = 0; i < 3; i++)
            {
                var clip = e.Clip(loops[i]) ?? e.Clip("idle");
                if (clip == null) continue;
                _loop[i] = AnimationClipPlayable.Create(_g, clip); _loop[i].SetApplyFootIK(false); _len[i] = Mathf.Max(0.1f, clip.length);
                _g.Connect(_loop[i], 0, _mix, i);
            }
            _mix.SetInputWeight(0, 1f);
            _g.Play();
        }

        /// <summary>Locomotion speed (m/s). Walk/run cycles are time-scaled to the actual speed (no foot skating at low speed).</summary>
        public void SetSpeed(float metersPerSecond) { _speed = Mathf.Lerp(_speed, Mathf.Max(0f, metersPerSecond), 0.25f); }

        /// <summary>Play a named one-shot action (e.g. "react", "celebrate", "point"). hold = stay on the last frame until the next call.</summary>
        public bool Play(string clipName, bool hold = false, float speed = 1f)
        {
            if (!_g.IsValid()) return false;
            var clip = Entry != null ? Entry.Clip(clipName) : null; if (clip == null) return false;
            if (_shot.IsValid()) { _g.Disconnect(_mix, 3); _shot.Destroy(); }
            _shot = AnimationClipPlayable.Create(_g, clip); _shot.SetApplyFootIK(false); _shot.SetSpeed(speed);
            _g.Connect(_shot, 0, _mix, 3);
            _shotT = 0f; _shotLen = clip.length / Mathf.Max(0.05f, speed); _shotHold = hold;
            return true;
        }

        /// <summary>Use a named clip as the looping base pose instead of idle (e.g. Mom "sleep"); null = normal locomotion.</summary>
        public void SetBaseLoop(string clipName)
        {
            if (_loopOverride == clipName || !_g.IsValid()) return;
            _loopOverride = clipName;
            var clip = clipName != null && Entry != null ? Entry.Clip(clipName) : null;
            if (clip == null) clip = Entry?.Clip("idle");
            if (clip == null) return;
            if (_loop[0].IsValid()) { _g.Disconnect(_mix, 0); _loop[0].Destroy(); }
            _loop[0] = AnimationClipPlayable.Create(_g, clip); _loop[0].SetApplyFootIK(false); _len[0] = Mathf.Max(0.1f, clip.length);
            _g.Connect(_loop[0], 0, _mix, 0);
        }

        public void StopAction() { _shotT = -1f; _shotHold = false; }

        void Update()
        {
            if (!_g.IsValid()) return;
            float dt = Time.deltaTime;
            // locomotion weights
            float w = Mathf.InverseLerp(0.05f, WalkSpeedForClip, _speed), r = Mathf.InverseLerp(WalkSpeedForClip, RunSpeedForClip, _speed);
            float wIdle = 1f - w, wWalk = w * (1f - r), wRun = w * r;
            float rate = _speed < WalkSpeedForClip ? Mathf.Lerp(0.6f, 1f, w) : Mathf.Lerp(1f, 1.25f, r);
            _phase += dt * rate;
            for (int i = 0; i < 3; i++) if (_loop[i].IsValid()) _loop[i].SetTime(i == 0 ? Time.time % _len[0] : (_phase % _len[i]));
            // one-shot action
            float wShot = 0f;
            if (_shotT >= 0f && _shot.IsValid())
            {
                _shotT += dt;
                float fadeIn = Mathf.Clamp01(_shotT / 0.15f), fadeOut = _shotHold ? 1f : Mathf.Clamp01((_shotLen - _shotT) / 0.2f);
                wShot = Mathf.Min(fadeIn, fadeOut);
                if (_shotHold && _shotT > _shotLen) _shot.SetTime(_shot.GetAnimationClip().length - 0.01f);
                if (!_shotHold && _shotT >= _shotLen) _shotT = -1f;
            }
            float k = 1f - wShot;
            _mix.SetInputWeight(0, wIdle * k); _mix.SetInputWeight(1, wWalk * k); _mix.SetInputWeight(2, wRun * k); _mix.SetInputWeight(3, wShot);
        }

        void LateUpdate()
        {
            // clips carry scene-coordinate root translation: keep the body where gameplay put it (vertical bob allowed)
            if (Root != null) { var p = Root.localPosition; Root.localPosition = new Vector3(_rootRest.x, p.y, _rootRest.z); }
        }

        void OnDestroy() { if (_g.IsValid()) _g.Destroy(); }
    }
}
