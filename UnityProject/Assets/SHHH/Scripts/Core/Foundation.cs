// SHHH! — Don't Wake Mom!  Core simulation (engine-agnostic, deterministic, host-authoritative).
// This assembly must not reference UnityEngine so it can be unit-tested headlessly (see /Core/SHHH.Core.Tests).
using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>Deterministic xorshift128+ RNG. Identical results on Mono/IL2CPP/.NET, unlike System.Random.</summary>
    public sealed class DetRng
    {
        ulong _s0, _s1;
        public DetRng(ulong seed)
        {
            // splitmix64 seeding
            _s0 = SplitMix(ref seed); _s1 = SplitMix(ref seed);
            if (_s0 == 0 && _s1 == 0) _s1 = 1;
        }
        static ulong SplitMix(ref ulong x)
        {
            ulong z = (x += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        public ulong NextU64()
        {
            ulong s1 = _s0, s0 = _s1;
            _s0 = s0; s1 ^= s1 << 23;
            _s1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
            return _s1 + s0;
        }
        /// <summary>[0,1)</summary>
        public double NextDouble() => (NextU64() >> 11) * (1.0 / 9007199254740992.0);
        public float Value() => (float)NextDouble();
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextU64() % (ulong)(maxExclusive - minInclusive));
        }
        public float Range(float min, float max) => min + (max - min) * Value();
        public bool Chance(float p) => Value() < p;
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = Range(0, i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; }
        }
        public DetRng Fork(ulong salt) => new DetRng(NextU64() ^ (salt * 0x2545F4914F6CDD1DUL));
    }

    /// <summary>Minimal engine-free vector for the headless simulation and bot reasoning (metres, Y up).</summary>
    [Serializable]
    public struct Vec3
    {
        public float x, y, z;
        public Vec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.x * s, a.y * s, a.z * s);
        public float Magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public float FlatDistance(Vec3 o) { float dx = x - o.x, dz = z - o.z; return (float)Math.Sqrt(dx * dx + dz * dz); }
        public static float Distance(Vec3 a, Vec3 b) => (a - b).Magnitude;
        public static Vec3 MoveTowards(Vec3 cur, Vec3 target, float maxDelta)
        {
            Vec3 d = target - cur; float m = d.Magnitude;
            if (m <= maxDelta || m < 1e-6f) return target;
            return cur + d * (maxDelta / m);
        }
        public override string ToString() => $"({x:0.00},{y:0.00},{z:0.00})";
    }

    public static class MathX
    {
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float InverseLerp(float a, float b, float v) => Math.Abs(b - a) < 1e-6f ? 0f : Clamp01((v - a) / (b - a));
    }

    /// <summary>Actor identities. Sibling slots are 0..3. Pets and environment use reserved ids.</summary>
    public static class ActorIds
    {
        public const int None = -1;
        public const int Environment = -2;
        public const int Cat = 100;
        public const int Dog = 101;
        public const int Mom = 200;
        public static bool IsSibling(int id) => id >= 0 && id < CompetitorRoster.SlotCount;
        public static bool IsPet(int id) => id == Cat || id == Dog;
        public static string Describe(int id, CompetitorRoster roster = null)
        {
            if (IsSibling(id)) return roster != null ? roster[id].DisplayName : ("Sibling " + (id + 1));
            switch (id)
            {
                case Cat: return "the cat";
                case Dog: return "the dog";
                case Environment: return "the house";
                case Mom: return "Mom";
                default: return "nobody";
            }
        }
    }

    /// <summary>Simple append-only explanation log, surfaced in QA evidence and the in-world "evidence board".</summary>
    public sealed class GameLog
    {
        readonly List<string> _lines = new List<string>(256);
        public Action<string> Sink;
        public IReadOnlyList<string> Lines => _lines;
        public void Add(float t, string msg)
        {
            string line = $"[{t,7:0.00}] {msg}";
            _lines.Add(line);
            if (_lines.Count > 4000) _lines.RemoveRange(0, 1000);
            Sink?.Invoke(line);
        }
    }
}
