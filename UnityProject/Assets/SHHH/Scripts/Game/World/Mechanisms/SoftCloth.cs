using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 soft fabric secondary motion (owner: clothes must look like soft, "rigged" clothes, not rigid blocks): the cloth meshes of a
    /// garment / bundle lag behind the body's acceleration (the top sways more than the bottom), squash on impact and spring back, and
    /// settle to their sculpted rest shape (sleeping when still). One damped 3D offset + one squash value drive every vertex by its height,
    /// so it costs one vertex pass only while it moves - Quest-practical, no solver, never moves the collider or the camera.
    /// </summary>
    public sealed class SoftCloth : MonoBehaviour
    {
        public float Sway = 0.0045f, MaxOffset = 0.035f, Stiffness = 140f, Damping = 9f, SquashPerSpeed = 0.07f;
        Rigidbody _rb; Vector3 _lastVel, _off, _offVel; float _squash, _squashVel, _height = 0.1f, _bottom;
        readonly List<MeshFilter> _mfs = new List<MeshFilter>(); readonly List<Vector3[]> _rest = new List<Vector3[]>(); readonly List<Vector3[]> _work = new List<Vector3[]>();
        bool _sleeping = true;
        public float Wobble => _off.magnitude + _squash;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                var m = Instantiate(mf.sharedMesh); m.MarkDynamic(); mf.sharedMesh = m;
                var v = m.vertices; _mfs.Add(mf); _rest.Add(v); _work.Add((Vector3[])v.Clone());
                foreach (var p in v) { float y = transform.InverseTransformPoint(mf.transform.TransformPoint(p)).y; lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
            }
            if (_mfs.Count > 0) { _bottom = lo; _height = Mathf.Max(0.02f, hi - lo); }
        }

        void FixedUpdate()
        {
            if (_rb == null || _mfs.Count == 0) return;
            float dt = Time.fixedDeltaTime; var vel = _rb.linearVelocity; var acc = (vel - _lastVel) / Mathf.Max(1e-4f, dt); _lastVel = vel;
            if (acc.sqrMagnitude > 900f) acc = acc.normalized * 30f;
            var accLocal = transform.InverseTransformDirection(acc - Physics.gravity * 0f);
            _offVel += (-accLocal * Sway * Stiffness * 0.02f - _off * Stiffness - _offVel * Damping) * dt;
            _off += _offVel * dt; if (_off.magnitude > MaxOffset) _off = _off.normalized * MaxOffset;
            _squashVel += (-_squash * Stiffness * 0.8f - _squashVel * Damping) * dt; _squash = Mathf.Clamp(_squash + _squashVel * dt, -0.12f, 0.3f);
        }

        void OnCollisionEnter(Collision c)
        {
            float s = c.relativeVelocity.magnitude; if (s < 0.4f) return;
            _squashVel += Mathf.Min(0.35f, s * SquashPerSpeed) * 12f; _sleeping = false;
        }

        void LateUpdate()
        {
            if (_mfs.Count == 0) return;
            bool moving = _off.sqrMagnitude > 1e-8f || Mathf.Abs(_squash) > 1e-4f || _offVel.sqrMagnitude > 1e-6f;
            if (!moving) { if (!_sleeping) { for (int i = 0; i < _mfs.Count; i++) { _mfs[i].sharedMesh.vertices = _rest[i]; _mfs[i].sharedMesh.RecalculateBounds(); } _sleeping = true; } return; }
            _sleeping = false;
            for (int i = 0; i < _mfs.Count; i++)
            {
                var mf = _mfs[i]; var rest = _rest[i]; var w = _work[i]; var toRoot = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix; var fromRoot = toRoot.inverse;
                for (int k = 0; k < rest.Length; k++)
                {
                    var p = toRoot.MultiplyPoint3x4(rest[k]);
                    float h = Mathf.Clamp01((p.y - _bottom) / _height), wgt = h * h;
                    p.x += _off.x * wgt + p.x * _squash * 0.5f * h; p.z += _off.z * wgt + p.z * _squash * 0.5f * h;
                    p.y = _bottom + (p.y - _bottom) * (1f - _squash) + _off.y * wgt * 0.5f;
                    w[k] = fromRoot.MultiplyPoint3x4(p);
                }
                mf.sharedMesh.vertices = w; mf.sharedMesh.RecalculateBounds();
            }
        }
    }
}
