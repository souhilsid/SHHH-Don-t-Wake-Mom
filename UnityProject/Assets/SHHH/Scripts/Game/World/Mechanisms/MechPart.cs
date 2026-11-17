using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 shared interaction foundation: a moving part of real furniture or an appliance that a hand GRASPS at its handle and moves along
    /// its own constraint (a hinge, a rail, a dial axis). The hand never teleports the part and the part never teleports the hand: while
    /// grasped the part follows the hand's position projected onto its constraint through a capped spring-damper; when released it keeps its
    /// velocity, slows by friction, stops at real end stops (soft-close latch or slam), and can be locked by the machine (washer door while
    /// running). <see cref="HandInteractor"/> asks <see cref="Nearest"/> before grabbing a loose prop, so the handle the hand is actually on
    /// wins over a prop behind it (V11 close-object selection).
    /// Network: the authority's recipe state (RFixture.Open / Level) is the truth for task progress; a part that no local hand holds eases
    /// toward <see cref="Remote"/> (the replicated opening 0..1) so every peer sees the same door / drawer.
    /// </summary>
    public abstract class MechPart : MonoBehaviour
    {
        public static readonly List<MechPart> All = new List<MechPart>();

        /// <summary>Grasp point in this transform's local space (it moves with the part).</summary>
        public Vector3 HandleLocal;
        public float GrabRadius = 0.06f;
        /// <summary>Machine lock (washer door while the drum turns). A locked part only rattles.</summary>
        public bool Locked;
        /// <summary>Replicated opening 0..1 to ease toward when no local hand holds it (null = free).</summary>
        public float? Remote;
        public string Label = "part";
        /// <summary>A recipe fixture's door: the rules' Open/Level drive <see cref="Remote"/> (else the house state does).</summary>
        public bool BoundToRecipe;
        /// <summary>House mechanism id for <see cref="SHHH.Core.Verb.Mech"/> replication (-1 = none).</summary>
        public int HouseId = -1;
        public NoiseMaterial Material = NoiseMaterial.Plastic;

        /// <summary>Fired on the peer whose hand moved it: (part, actor). Opened = left the closed stop; Closed = latched (slam = too fast).</summary>
        public System.Action<MechPart, int> Opened;
        public System.Action<MechPart, int, bool> Closed;
        /// <summary>The hand let go (part, actor) - the owner reports the resting opening so other peers show the same angle.</summary>
        public System.Action<MechPart, int> Released;
        /// <summary>A local hand took hold of this part (actor).</summary>
        public System.Action<MechPart, int> Grabbed;

        protected HandInteractor Hand;
        protected int HandActor = ActorIds.None;
        public int LastActor { get; protected set; } = ActorIds.None;
        public bool IsHeld => Hand != null;
        public HandInteractor HeldByHand => Hand;
        public Vector3 HandleWorld => transform.TransformPoint(HandleLocal);
        /// <summary>V13 (owner, Quest c21: "opening the bin by grabbing does not work"): a handle can be a LINE (half-length, local) - a lid's
        /// whole front rim - instead of one point; zero = a point handle. Players grab the edge where their hand is.</summary>
        public Vector3 HandleSpanLocal;
        Vector3 _grabLocal; bool _hasGrabLocal;
        /// <summary>Where on the handle this hand took hold (follows the part).</summary>
        public Vector3 GrabPointWorld => _hasGrabLocal ? transform.TransformPoint(_grabLocal) : HandleWorld;

        /// <summary>Closest handle point to p (on the handle line).</summary>
        public Vector3 ClosestHandlePoint(Vector3 p)
        {
            var a = HandleWorld; var s = transform.TransformVector(HandleSpanLocal);
            if (s.sqrMagnitude < 1e-8f) return a;
            var b0 = a - s; var ab = s * 2f; float t = Mathf.Clamp01(Vector3.Dot(p - b0, ab) / ab.sqrMagnitude);
            return b0 + ab * t;
        }
        public float HandleDistance(Vector3 p) => Vector3.Distance(ClosestHandlePoint(p), p);
        /// <summary>0 = closed stop, 1 = fully open (hinge angle / rail travel / dial position).</summary>
        public abstract float Opening { get; }
        public abstract bool IsClosed { get; }
        public int Grabs { get; private set; }
        public int Slams { get; protected set; }
        public int Rattles { get; protected set; }

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); if (Hand != null) EndGrab(); }

        public static MechPart ByHouseId(int id) { foreach (var m in All) if (m != null && m.HouseId == id) return m; return null; }
        /// <summary>The moving part that is a recipe fixture's door (bound host furniture).</summary>
        public static MechPart ByFixture(int fixtureId)
        {
            var rt = RecipeRuntime.I; var fv = rt != null ? rt.FixtureView(fixtureId) : null;
            return fv != null && fv.Host != null ? fv.Host.DoorFor(fv.Decl.Kind) : null;
        }
        /// <summary>Set by a client-side stop: the next report of this part carries the slam flag to the host.</summary>
        public bool PendingSlam;
        /// <summary>When this peer last moved it (grab, release, script, shove): only the mover reports where it comes to rest.</summary>
        public float LastLocalTouch { get; protected set; } = -99f;
        public void Touch(int actor) { LastActor = actor; LastLocalTouch = Time.time; }

        /// <summary>Host: a client's copy of this part slammed - the host makes the (single) noise event at the part, owned by that seat.</summary>
        public bool HostSlamFromClient(int seat)
        {
            var round = GameServices.Round; if (!GameServices.IsAuthority || round == null || !round.IsPlayable) return false;
            var at = HandleWorld; var e = round.SubmitOperate(seat, NoiseKeyOf(this), 14f, new Vec3(at.x, at.y, at.z), true);
            if (e != null) { Slams++; RoomFx.ImpactAt(at, Material, e.Value, "door"); if (AudioBank.I != null) AudioBank.I.Play("impact_appliance", at, 0.8f, 0.06f); }
            return e != null;
        }
        static int NoiseKeyOf(MechPart m) => m is HingedPart h ? h.NoiseKey : m is SlidingPart s ? s.NoiseKey : 950;

        /// <summary>Handle within reach of a pinch point (closest first). Distance is to the handle point minus the grab radius.</summary>
        public static MechPart Nearest(Vector3 p, out float dist)
        {
            MechPart best = null; dist = float.MaxValue;
            foreach (var m in All)
            {
                if (m == null || !m.isActiveAndEnabled || m.Hand != null) continue;
                float d = m.HandleDistance(p);
                if (d > m.GrabRadius) continue;
                d = Mathf.Max(0f, d - m.GrabRadius * 0.5f);
                if (d < dist) { dist = d; best = m; }
            }
            return best;
        }

        public static MechPart HeldBy(HandInteractor h)
        {
            if (h == null) return null;
            foreach (var m in All) if (m != null && m.Hand == h) return m;
            return null;
        }

        public virtual void BeginGrab(HandInteractor h, Vector3 at)
        {
            Hand = h; HandActor = h.Actor; LastActor = h.Actor; Grabs++; LastLocalTouch = Time.time;
            _grabLocal = transform.InverseTransformPoint(ClosestHandlePoint(at)); _hasGrabLocal = true;
            MechHaptics.Pulse(h, 0.35f, 0.04f);
            OnGrab(at);
            Grabbed?.Invoke(this, h.Actor);
        }

        public virtual void EndGrab()
        {
            if (Hand != null) MechHaptics.Pulse(Hand, 0.15f, 0.03f);
            bool had = Hand != null; Hand = null; OnRelease(); if (had) LastLocalTouch = Time.time;
            if (had) Released?.Invoke(this, LastActor);
        }

        protected abstract void OnGrab(Vector3 at);
        protected abstract void OnRelease();
        protected Vector3 HandPoint => Hand != null && Hand.Source != null ? Hand.Source.PinchPoint : HandleWorld;

        /// <summary>A part released at speed into its closed stop: one slam noise event (Mom hears it), cartoon thump; a gentle close is a
        /// quiet click. Noise flows through the round's Operate path with slam = true (NoiseCause.Slam).</summary>
        protected void StopNoise(float speed01, bool slam, Vector3 at, int objectKey)
        {
            var ab = AudioBank.I;
            if (slam)
            {
                Slams++;
                if (ab != null) { ab.Play("impact_appliance", at, Mathf.Clamp01(0.55f + speed01 * 0.45f), 0.06f); ab.Play("door_close_soft", at, 0.8f); }
                var round = GameServices.Round;
                if (!GameServices.IsAuthority) PendingSlam = true;   // client: the report to the host carries the slam (the host voices it)
                if (GameServices.IsAuthority && round != null && round.IsPlayable && LastActor != ActorIds.None)
                {
                    var e = round.SubmitOperate(LastActor, objectKey, 6f + 16f * speed01, new Vec3(at.x, at.y, at.z), true);
                    if (e != null) RoomFx.ImpactAt(at, Material, e.Value, "door");
                }
                else RoomFx.Burst("toon_impact", at, 0.18f, 1.2f);
            }
            else if (ab != null) ab.Play("lid_close", at, 0.25f + 0.25f * speed01, 0.05f);
        }

        float _lastRattle;
        protected void Rattle(Vector3 at)
        {
            if (Time.time - _lastRattle < 0.35f) return;
            _lastRattle = Time.time; Rattles++;
            if (AudioBank.I != null) AudioBank.I.Play("impact_plastic", at, 0.3f, 0.15f);
            if (Hand != null) MechHaptics.Pulse(Hand, 0.6f, 0.05f);
        }
    }
}
