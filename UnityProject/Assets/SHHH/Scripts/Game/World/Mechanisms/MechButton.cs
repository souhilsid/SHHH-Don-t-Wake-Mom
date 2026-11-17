using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 real push-button (washer START, a light switch): a fingertip that pushes the cap INTO the panel moves it along its travel; it
    /// fires once past 70 % travel and re-arms after it springs back. It does only what the real button does (owner V10 rule: no
    /// poke-to-complete) - the appliance decides what a press means. Pressers are the local hands' index tips (<see cref="PokeButton.Pokers"/>);
    /// bots press it through <see cref="PressBy"/> after reaching it.
    /// </summary>
    public sealed class MechButton : MonoBehaviour
    {
        public static readonly List<MechButton> All = new List<MechButton>();
        /// <summary>Outward normal in this transform's parent space.</summary>
        public Vector3 Normal = Vector3.forward;
        public float Travel = 0.006f, Radius = 0.016f;
        public System.Action<MechButton, int> Pressed;
        public int Presses { get; private set; }
        public Vector3 FaceWorld => transform.parent != null ? transform.parent.TransformPoint(_restLocal) + NormalWorld * _faceOut : transform.position;
        public Vector3 NormalWorld => transform.parent != null ? transform.parent.TransformDirection(Normal).normalized : Normal;

        Vector3 _restLocal; float _faceOut, _depth, _botUntil = -1f; bool _armed = true, _init; int _botActor = ActorIds.None;

        /// <summary>faceOut: distance from the pivot to the cap face along the normal (world metres).</summary>
        public void Init(Vector3 normalWorld, float faceOut, float radius, float travel = 0.006f)
        {
            Normal = transform.parent != null ? transform.parent.InverseTransformDirection(normalWorld).normalized : normalWorld.normalized;
            _restLocal = transform.localPosition; _faceOut = faceOut; Radius = radius; Travel = travel; _init = true;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>A bot's finger presses it (after its reach animation): the same cap travel and the same event.</summary>
        public void PressBy(int actor) { _botActor = actor; _botUntil = Time.time + 0.25f; }

        void Update()
        {
            if (!_init) return;
            float depth = 0f; int actor = ActorIds.None;
            var face = (transform.parent != null ? transform.parent.TransformPoint(_restLocal) : _restLocal) + NormalWorld * _faceOut;
            foreach (var h in PokeButton.Pokers)
            {
                if (h == null || !h.Tracked) continue;
                var tip = h.IndexTip; var rel = tip - face;
                float along = Vector3.Dot(rel, NormalWorld);                       // > 0 in front of the cap
                float lateral = (rel - NormalWorld * along).magnitude;
                if (lateral > Radius + 0.008f || along > 0.012f || along < -Travel - 0.03f) continue;
                float d = Mathf.Clamp(-along + 0.004f, 0f, Travel);               // fingertip pad ~4 mm
                if (d > depth) { depth = d; actor = GameServices.LocalSlot; }
            }
            if (Time.time < _botUntil) { depth = Travel; actor = _botActor; }
            _depth = Mathf.MoveTowards(_depth, depth, (depth > _depth ? 0.3f : 0.08f) * Time.deltaTime * 10f);
            if (_armed && _depth >= Travel * 0.7f)
            {
                _armed = false; Presses++;
                if (AudioBank.I != null) AudioBank.I.Play("ui_click", face, 0.55f, 0.05f);
                Pressed?.Invoke(this, actor);
            }
            else if (!_armed && _depth <= Travel * 0.3f) _armed = true;
            var local = _restLocal - Normal * (_depth / Mathf.Max(1e-4f, transform.parent != null ? transform.parent.lossyScale.x : 1f));
            transform.localPosition = local;
        }
    }
}
