using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    public enum IncidentKind { Noise, Sabotage }

    /// <summary>
    /// Something that happened this round that Mom might ask about (a decisive noise, a sabotage). Created deterministically from
    /// rules events on every peer. <see cref="TruthActor"/> is WorldTruth: it is NEVER read by the verdict; only QA logs show it.
    /// </summary>
    public sealed class Incident
    {
        public int Id; public IncidentKind Kind; public float Time;
        public int TruthActor = ActorIds.None;    // QA only
        public int Victim = ActorIds.None;        // sabotage: whose recipe
        public string What = "";                  // present tense after "Saw Rory ...": "make the pan crash", "mess with Pip's ..."
        public string Question = "";              // Mom's one question: "Who made the pan crash?"
        public string Where = "";                 // human-readable place label
        public Vec3 Position;                     // for perception (host) and hearing
        public float Loudness;                    // noise value (0 for silent sabotage)
        public int RecipeInstance = -1, NoiseId = -1, SabotageIndex = -1;
        public override string ToString() => $"I{Id} {Kind} t{Time:0.0} '{What}' @{Where}";
    }

    /// <summary>A sibling's (or Mom's) own memory of an incident they actually perceived. Only the witness can present it.</summary>
    public sealed class WitnessMemory
    {
        public int Id, Incident, Witness; public int SeenActor = ActorIds.None; public float Confidence; public float Time;
        public string Card = "";                  // card text: "Saw Rory take Pip's book (living room, 0:42)"
        public bool Presented;
        public override string ToString() => $"M{Id} by {Witness} on I{Incident}: actor {SeenActor} conf {Confidence:0.00}";
    }

    public enum ObservationSource { Heard, SeenByMom, Inspected, WitnessCard, Accusation }

    /// <summary>One entry of Mom's belief ledger, with provenance. Mom reasons only over these.</summary>
    public sealed class Observation
    {
        public ObservationSource Source; public int Incident = -1; public int BelievedActor = ActorIds.None; public float Confidence;
        public int Reporter = ActorIds.None; public int MemoryId = -1; public float Time; public string Text = "";
        public override string ToString() => $"{Source} I{Incident} -> {BelievedActor} ({Confidence:0.00}) by {Reporter}: {Text}";
    }

    /// <summary>Witness memories (what each seat really perceived, reported by the host's perception) + Mom's belief ledger.</summary>
    public sealed class BeliefLedger
    {
        public readonly List<Incident> Incidents = new List<Incident>();
        public readonly List<WitnessMemory> Memories = new List<WitnessMemory>();
        public readonly List<Observation> Mom = new List<Observation>();
        public const int MomWitness = 99;          // witness id used for Mom's own eyes
        public const float NoiseIncidentMin = 4f;  // quieter noises are not worth an incident

        public Incident AddIncident(Incident i) { i.Id = Incidents.Count; Incidents.Add(i); return i; }
        public Incident Get(int id) => id >= 0 && id < Incidents.Count ? Incidents[id] : null;

        /// <summary>Host perception: <paramref name="witness"/> perceived the incident's actor with this confidence (0..1).</summary>
        public WitnessMemory AddWitness(int incident, int witness, float confidence, Func<int, string> name)
        {
            var inc = Get(incident); if (inc == null || witness == inc.TruthActor) return null;   // you don't "witness" yourself
            if (Memories.Any(m => m.Incident == incident && m.Witness == witness)) return null;
            var m = new WitnessMemory { Id = Memories.Count, Incident = incident, Witness = witness, SeenActor = inc.TruthActor, Confidence = Clamp01(confidence), Time = inc.Time };
            m.Card = $"Saw {name(inc.TruthActor)} {inc.What}" + (string.IsNullOrEmpty(inc.Where) ? "" : $" ({inc.Where})") + $" at {Clock(inc.Time)}";
            Memories.Add(m);
            if (witness == MomWitness) Mom.Add(new Observation { Source = ObservationSource.SeenByMom, Incident = incident, BelievedActor = inc.TruthActor, Confidence = m.Confidence, Time = inc.Time, Text = "Mom saw it herself" });
            return m;
        }

        public IEnumerable<WitnessMemory> MemoriesOf(int seat) => Memories.Where(m => m.Witness == seat);

        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static string Clock(float t) { int s = (int)Math.Max(0, t); return $"{s / 60}:{s % 60:00}"; }
    }

    /// <summary>
    /// The playable confrontation (spec §06, ~40–60 s): Mom presents the incident, every active seat may present witness-memory
    /// cards (only memories they truly have) or point at a suspect (an unsupported accusation), then a transparent bounded resolver
    /// decides. Inputs are journaled; bots act through the same calls.
    /// </summary>
    public sealed class Interrogation
    {
        public Incident Incident;                   // null = general inspection (no specific incident)
        public bool IsInspection;
        public float Started, Elapsed;
        public readonly HashSet<int> Done = new HashSet<int>();
        public readonly List<(int seat, int memory)> Cards = new List<(int, int)>();
        public readonly List<(int seat, int suspect)> Accusations = new List<(int, int)>();
        public string Question = "";
        public const float PresentSeconds = 8f, EvidenceSeconds = 25f; // defaults (legacy rounds); + verdict presentation by MomDirector
        /// <summary>Continuous match: the first confrontation is full, later ones are a quick challenge.</summary>
        public float Present = PresentSeconds, Evidence = EvidenceSeconds; public bool Quick;
        public bool EvidenceOpen => Elapsed >= Present && Elapsed < Present + Evidence;
        public float Remaining => Math.Max(0f, Present + Evidence - Elapsed);
    }

    /// <summary>Transparent, bounded verdict over Mom's belief ledger only. Never reads <see cref="Incident.TruthActor"/>.</summary>
    public static class VerdictCalculator
    {
        public const float ProofThreshold = 0.7f, Margin = 0.25f;
        public const float WCard = 1.0f, WMomSaw = 1.0f, WAccuse = 0.1f, AccuseCap = 0.2f, WPattern = 0.3f, WNearby = 0.15f;

        public sealed class Result
        {
            public int Culprit = ActorIds.None; public bool Proven; public readonly Dictionary<int, float> Suspicion = new Dictionary<int, float>();
            public readonly List<string> Trace = new List<string>();
        }

        /// <param name="nearbySeat">seat whose work area the incident happened at (location-based speculation), or None.</param>
        public static Result Resolve(BeliefLedger b, Interrogation q, IList<int> active, int nearbySeat)
        {
            var r = new Result();
            foreach (int s in active) r.Suspicion[s] = 0f;
            int inc = q.Incident?.Id ?? -1;
            void Add(int seat, float v, string why) { if (!r.Suspicion.ContainsKey(seat)) return; r.Suspicion[seat] += v; r.Trace.Add($"{why}: seat {seat} +{v:0.00}"); }
            // Mom's own eyes (she only sees what happens while she is in the room)
            foreach (var o in b.Mom.Where(o => o.Source == ObservationSource.SeenByMom && (inc < 0 || o.Incident == inc))) Add(o.BelievedActor, WMomSaw * o.Confidence, "Mom saw " + o.Text);
            // presented witness cards: a card about THIS incident is strong; cards about other sabotage show a pattern
            var credited = new HashSet<(int, int)>();
            foreach (var (seat, mid) in q.Cards)
            {
                var m = b.Memories[mid];
                if (m.Witness != seat || !credited.Add((m.Incident, m.Witness))) continue;   // only your own memory, once
                bool thisOne = inc >= 0 && m.Incident == inc;
                Add(m.SeenActor, (thisOne ? WCard : WPattern) * m.Confidence, $"card M{m.Id} from seat {seat} ({(thisOne ? "this incident" : "other incident")})");
            }
            // pointing without a memory: weak, capped per suspect (cannot invent an event)
            foreach (var g in q.Accusations.GroupBy(a => a.suspect)) Add(g.Key, Math.Min(AccuseCap, WAccuse * g.Select(a => a.seat).Distinct().Count()), $"pointed at by {g.Select(a => a.seat).Distinct().Count()}");
            // speculation: it happened at someone's work area (only for an actual incident)
            if (inc >= 0 && nearbySeat != ActorIds.None) Add(nearbySeat, WNearby, "it happened at their work area (speculation)");
            var ranked = r.Suspicion.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).ToList();
            if (ranked.Count > 0 && ranked[0].Value >= ProofThreshold && (ranked.Count == 1 || ranked[0].Value - ranked[1].Value >= Margin))
            { r.Culprit = ranked[0].Key; r.Proven = true; r.Trace.Add($"PROVEN: seat {r.Culprit} ({ranked[0].Value:0.00})"); }
            else r.Trace.Add("not proven: fall back to chores and responsibility");
            return r;
        }
    }
}
