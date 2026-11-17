using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V10 impact policy (owner directive §B/§K5: "441 rattles must not become 441 Mom observations" — fix the policy, do not mute
    /// collisions). Decides whether a collision a <see cref="PropBody"/> reports is a NEW meaningful hit (sound + one Mom noise event)
    /// or part of something already heard:
    /// <list type="bullet">
    /// <item><b>Impulse check</b>: the solver's impulse / mass is the real change of velocity. A contact that changes almost nothing is
    /// resting or sliding contact, not a hit, whatever the relative velocity says (speculative CCD reports large approach speeds for
    /// bodies jittering against thin colliders).</item>
    /// <item><b>Per object-pair cooldown</b>: the same two colliders touching again within 0.35 s is a bounce/chatter of the same
    /// event unless it is clearly harder (1.5x).</item>
    /// <item><b>Collision cluster</b>: everything one prop hits within 0.5 s of its first hit is one cluster = one noise event; only a
    /// clearly louder hit inside the cluster (1.4x the peak) is reported, as an escalation.</item>
    /// <item><b>Rattle</b>: more than 6 contacts in one second is rattling. It is voiced as at most one quiet rattle every 0.6 s and
    /// never as a new Mom event unless a single hit inside it is hard (rel &gt; 1.6 m/s).</item>
    /// <item><b>Voice budget</b>: at most 8 impact sounds per 0.25 s across the house (presentation only; the noise event still counts).</item>
    /// </list>
    /// All counters are exposed for QA (<see cref="Stats"/>).
    /// </summary>
    public static class ImpactGate
    {
        public const float PairCooldown = 0.35f, ClusterWindow = 0.5f, RattleWindow = 1f, MinDeltaV = 0.15f;
        public const int RattleContacts = 6, VoiceBudget = 8; public const float VoiceWindow = 0.25f;

        sealed class PropState
        {
            public readonly Dictionary<int, (float t, float rel)> Pairs = new Dictionary<int, (float, float)>();
            public float ClusterStart = -99f, ClusterPeak, LastRattleSound = -99f;
            public readonly Queue<float> Contacts = new Queue<float>();
        }
        static readonly Dictionary<PropBody, PropState> _states = new Dictionary<PropBody, PropState>();
        static float _voiceT = -99f; static int _voiceN;

        public struct Counters { public int Contacts, Accepted, Resting, PairEcho, ClusterEcho, Rattle, RattleSounds, VoiceDropped; }
        public static Counters Stats;
        public static void ResetStats() { Stats = default; _states.Clear(); }

        /// <summary>True when this contact is a new meaningful impact (submit noise + play it).</summary>
        public static bool Accept(PropBody p, Collision c, float rel)
        {
            float now = Time.time; Stats.Contacts++;
            if (!_states.TryGetValue(p, out var s)) { s = new PropState(); _states[p] = s; }
            // rattle bookkeeping (every reported contact counts)
            s.Contacts.Enqueue(now); while (s.Contacts.Count > 0 && now - s.Contacts.Peek() > RattleWindow) s.Contacts.Dequeue();
            // 1. impulse: how much did the contact really change the body's velocity?
            var imp = c.impulse;
            if (!p.IsHeld && imp.sqrMagnitude > 0f && p.Body != null && !p.Body.isKinematic && imp.magnitude / Mathf.Max(0.05f, p.Body.mass) < MinDeltaV && rel < 1.6f) { Stats.Resting++; return false; }
            // 2. same pair again, not harder
            int key = c.collider != null ? c.collider.GetInstanceID() : 0;
            if (s.Pairs.TryGetValue(key, out var last) && now - last.t < PairCooldown && rel < last.rel * 1.5f) { s.Pairs[key] = (now, Mathf.Max(rel, last.rel)); Stats.PairEcho++; return false; }
            s.Pairs[key] = (now, rel);
            if (s.Pairs.Count > 24) s.Pairs.Clear();
            // 3. rattle: many contacts in a short time -> one quiet rattle voice, no new event unless one hit is hard
            if (s.Contacts.Count > RattleContacts && rel < 1.6f)
            {
                Stats.Rattle++;
                if (now - s.LastRattleSound > 0.6f) { s.LastRattleSound = now; Stats.RattleSounds++; AudioBank.I?.Play(AudioBank.MaterialKey(p.Spec.Material), c.contactCount > 0 ? c.GetContact(0).point : p.transform.position, 0.12f, 0.15f); }
                return false;
            }
            // 4. collision cluster: one event per cluster, escalations only
            if (now - s.ClusterStart < ClusterWindow)
            {
                if (rel < s.ClusterPeak * 1.4f) { Stats.ClusterEcho++; return false; }
                s.ClusterPeak = rel; Stats.Accepted++; return true;
            }
            s.ClusterStart = now; s.ClusterPeak = rel; Stats.Accepted++;
            return true;
        }

        /// <summary>Presentation voice budget for impact sounds (the gameplay noise event is not affected).</summary>
        public static bool TakeVoice()
        {
            float now = Time.time;
            if (now - _voiceT > VoiceWindow) { _voiceT = now; _voiceN = 0; }
            if (_voiceN >= VoiceBudget) { Stats.VoiceDropped++; return false; }
            _voiceN++; return true;
        }

        public static void Forget(PropBody p) => _states.Remove(p);
        public static string Report() => $"contacts {Stats.Contacts}, accepted {Stats.Accepted}, resting {Stats.Resting}, pair echo {Stats.PairEcho}, cluster echo {Stats.ClusterEcho}, rattle {Stats.Rattle} (voiced {Stats.RattleSounds}), voice dropped {Stats.VoiceDropped}";
    }
}
