using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum NoiseMaterial { Soft = 0, Fabric, Paper, Wood, Plastic, Toy, Ceramic, Glass, Metal, Appliance }

    public enum NoiseCause
    {
        DirectImpact,   // object a sibling was holding / just released hits something
        Throw,          // released above throw speed
        Slam,           // door/drawer/cabinet/washer closed too fast
        TrapTrigger,    // an armed trap fires
        PetAction,      // cat/dog knocked something or barked/meowed
        ChainReaction,  // an object set in motion by another noisy event
        Appliance,      // washer spin, faucet, etc.
        Scripted,
        Unknown
    }

    /// <summary>A host-authoritative gameplay noise event. Audio playback loudness is derived separately.</summary>
    [Serializable]
    public sealed class NoiseEvent
    {
        public int Id;
        public int Tick;
        public float Time;
        public int SourceObjectId = -1;
        public NoiseMaterial Material;
        public NoiseCause Cause;
        public float ImpactEnergy;      // joules (0.5 m v^2) for impacts
        public float Value;             // gameplay noise points after material/distance/round modifiers
        public int DirectActor = ActorIds.None;  // who released/threw/slammed (from the per-object interaction history)
        public int ParentEventId = -1;  // causal parent for chain reactions
        public int TrapId = -1;
        public int ProvokerActor = ActorIds.None; // e.g. sibling who lured the pet
        public int TriggeringActor = ActorIds.None; // e.g. victim who walked into a trap
        public bool VictimHadCounterplay;           // trap warning was visible long enough AND victim acted carelessly
        public Vec3 Position;
        public bool Qualifying(float minQualifying) => Value >= minQualifying;
        public override string ToString() => $"N{Id} {Cause} obj{SourceObjectId} {Material} v={Value:0.0} actor={DirectActor} trap={TrapId}";
    }

    /// <summary>Tunable noise physics model (see Docs/NOISE_CAUSALITY.md).</summary>
    [Serializable]
    public sealed class NoiseModel
    {
        public float ReferenceEnergy = 0.35f;     // J
        public float MinEnergy = 0.08f;           // below this an impact is silent for gameplay
        public float Scale = 9f;
        public float MaxEventValue = 85f;
        public float DistanceFalloffStart = 3f;   // m from Mom's door with full effect
        public float DistanceFalloffEnd = 14f;    // m where effect drops to MinDistanceFactor
        public float MinDistanceFactor = 0.45f;

        public static float Loudness(NoiseMaterial m)
        {
            switch (m)
            {
                case NoiseMaterial.Soft: return 0.05f;
                case NoiseMaterial.Fabric: return 0.12f;
                case NoiseMaterial.Paper: return 0.35f;
                case NoiseMaterial.Wood: return 0.9f;
                case NoiseMaterial.Plastic: return 0.75f;
                case NoiseMaterial.Toy: return 0.85f;
                case NoiseMaterial.Ceramic: return 1.45f;
                case NoiseMaterial.Glass: return 1.6f;
                case NoiseMaterial.Metal: return 2.0f;
                case NoiseMaterial.Appliance: return 1.2f;
            }
            return 1f;
        }

        public float ImpactValue(NoiseMaterial mat, float massKg, float relSpeed, float distToMom, float roundMultiplier)
        {
            float e = 0.5f * massKg * relSpeed * relSpeed;
            if (e < MinEnergy) return 0f;
            float raw = Scale * Loudness(mat) * (float)(Math.Log(1.0 + e / ReferenceEnergy) / Math.Log(2.0));
            float df = MathX.Lerp(1f, MinDistanceFactor, MathX.InverseLerp(DistanceFalloffStart, DistanceFalloffEnd, distToMom));
            return MathX.Clamp(raw * df * roundMultiplier, 0f, MaxEventValue);
        }
    }

    public enum SleepStage { DeepSleep, Stirring, Suspicious, AlmostAwake, Awake }

    /// <summary>Mom's network-authoritative SleepMeter (0..100). Never fed by voice chat amplitude.</summary>
    [Serializable]
    public sealed class SleepMeter
    {
        public float Value;
        public float Baseline;
        public float WakeThreshold = 100f;
        public float QuietDelay = 4f;     // seconds without qualifying noise before decay starts
        public float DecayPerSecond = 3f;
        public float MinQualifyingForDecayReset = 2f;
        float _lastNoiseTime = -999f;

        public SleepStage Stage
        {
            get
            {
                if (Value >= WakeThreshold) return SleepStage.Awake;
                if (Value >= 85f) return SleepStage.AlmostAwake;
                if (Value >= 65f) return SleepStage.Suspicious;
                if (Value >= 35f) return SleepStage.Stirring;
                return SleepStage.DeepSleep;
            }
        }

        public void Reset(float baseline) { Baseline = baseline; Value = baseline; _lastNoiseTime = -999f; }

        /// <returns>true if this addition crossed the wake threshold</returns>
        public bool Add(float amount, float now)
        {
            if (amount <= 0f) return false;
            bool wasBelow = Value < WakeThreshold;
            Value = MathX.Clamp(Value + amount, 0f, WakeThreshold + 20f);
            if (amount >= MinQualifyingForDecayReset) _lastNoiseTime = now;
            return wasBelow && Value >= WakeThreshold;
        }

        public void Tick(float now, float dt)
        {
            if (now - _lastNoiseTime >= QuietDelay && Value > Baseline)
                Value = Math.Max(Baseline, Value - DecayPerSecond * dt);
        }

        public void SetAfterWarning(float v, float now) { Value = Math.Min(v, WakeThreshold - 1f); _lastNoiseTime = now; }
    }

    /// <summary>
    /// Rate limiting + dedupe so tiny repeated contacts and "noise-spam scripts" can't farm the meter or score.
    /// One object may contribute at most one event per cooldown window (largest wins).
    /// </summary>
    public sealed class NoiseGate
    {
        public float PerObjectCooldown = 0.25f;
        public float MinEventValue = 0.6f;
        readonly Dictionary<int, float> _lastByObject = new Dictionary<int, float>();
        public bool Allow(NoiseEvent e)
        {
            if (e.Value < MinEventValue) return false;
            if (e.SourceObjectId < 0) return true;
            if (_lastByObject.TryGetValue(e.SourceObjectId, out float last) && e.Time - last < PerObjectCooldown) return false;
            _lastByObject[e.SourceObjectId] = e.Time;
            return true;
        }
        public void Clear() => _lastByObject.Clear();
    }
}
