using System.Collections.Generic;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 a piece of real house furniture / an appliance that IS a recipe fixture (washer drum, dryer lint panel, chest-of-drawers towel
    /// drawer, lint bin). Shared behaviour for every host:
    /// <list type="bullet">
    /// <item>the fixture's door IS a moving part (<see cref="DoorFor"/>): when a hand opens / closes it, the Open / Close op is sent with
    /// the real opening (and re-sent where it comes to rest); every peer's part follows the replicated Open/Level.</item>
    /// <item>"placed" = physically inside (<see cref="Inside"/>) and resting 0.3 s, never a snap: the authority sends PlaceInSocket for a
    /// free slot (tag-matching first) and the item's squareness as Align.</item>
    /// </list>
    /// </summary>
    public abstract class MechHost : MonoBehaviour
    {
        public static readonly List<MechHost> Hosts = new List<MechHost>();
        protected readonly List<RecipeFixtureView> Bound = new List<RecipeFixtureView>();
        public IReadOnlyList<RecipeFixtureView> BoundFixtures => Bound;

        public abstract bool Serves(string fixtureKind);
        /// <summary>The moving part that is this fixture's door (null = an open container, e.g. a bin whose lid is just furniture).</summary>
        public abstract MechPart DoorFor(string fixtureKind);
        public abstract Vector3 SlotPoint(string fixtureKind, int k);
        public abstract bool Inside(string fixtureKind, Vector3 world);
        /// <summary>Where a person looks when working here.</summary>
        public abstract Vector3 Focus { get; }
        public virtual Vector3 Front => transform.forward;
        /// <summary>Direction things are put in from (washer drum: through the front; drawer, bin: from above).</summary>
        public virtual Vector3 LoadFrom(string fixtureKind) => Vector3.up;

        protected virtual void OnEnable() { Hosts.Add(this); }
        protected virtual void OnDisable() { Hosts.Remove(this); }

        public static MechHost Find(string fixtureKind, Vector3 near, float within = 1.0f)
        {
            MechHost best = null; float bd = within;
            foreach (var h in Hosts) { if (h == null || !h.Serves(fixtureKind)) continue; var d = h.transform.position - near; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = h; } }
            return best;
        }

        readonly HashSet<MechPart> _wired = new HashSet<MechPart>();
        public void Bind(RecipeFixtureView fv)
        {
            if (Bound.Contains(fv)) return;
            Bound.Add(fv);
            var d = DoorFor(fv.Decl.Kind);
            if (d != null && _wired.Add(d))
            {
                d.BoundToRecipe = true;
                d.Opened += DoorOpened; d.Closed += DoorClosed;
                d.Released += (m, actor) => { if (!m.IsClosed && !m.Locked) StartCoroutine(ReportRest(m, actor)); };
            }
        }

        void DoorOpened(MechPart part, int actor)
        {
            foreach (var fv in Bound)
            {
                if (DoorFor(fv.Decl.Kind) != part) continue; var st = fv.State; if (st == null || !st.HasDoor) continue;
                var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Open, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1, Arg = Mathf.Max(1, Mathf.RoundToInt(part.Opening * 100f)).ToString(System.Globalization.CultureInfo.InvariantCulture) });
                if (err != null) Debug.Log($"[SHHH][mech] open {st.Kind} refused: {err}");
            }
        }

        System.Collections.IEnumerator ReportRest(MechPart part, int actor)
        {
            yield return new WaitForSeconds(0.6f);   // let it settle, then publish where it really rests
            if (part != null && !part.IsClosed && !part.IsHeld) DoorOpened(part, actor);
        }

        void DoorClosed(MechPart part, int actor, bool slam)
        {
            foreach (var fv in Bound)
            {
                if (DoorFor(fv.Decl.Kind) != part) continue; var st = fv.State; if (st == null || !st.HasDoor || !st.Open) continue;
                var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Close, Fixture = st.Id, Prop = -1, Prop2 = -1, Slot = -1, Arg = slam ? "slam" : "" });
                part.PendingSlam = false;
                if (err != null) Debug.Log($"[SHHH][mech] close {st.Kind} refused: {err}");
            }
        }

        readonly Dictionary<MechPart, float> _stable = new Dictionary<MechPart, float>(); readonly Dictionary<MechPart, float> _lastOpen = new Dictionary<MechPart, float>();
        /// <summary>Every peer: bound doors follow the authoritative opening of their fixture; a door this peer moved reports where it rests.</summary>
        protected virtual void Update()
        {
            foreach (var d in _wired)
            {
                if (d == null) continue; _lastOpen.TryGetValue(d, out var lo); _stable.TryGetValue(d, out var sf);
                if (Mathf.Abs(d.Opening - lo) < 0.004f) sf += Time.deltaTime; else { sf = 0f; _lastOpen[d] = d.Opening; }
                _stable[d] = sf;
                if (sf > 0.4f && sf < 0.4f + Time.deltaTime * 1.5f && !d.IsHeld && !d.IsClosed && Time.time - d.LastLocalTouch < 4f)
                    foreach (var fv in Bound) { var st = fv.State; if (DoorFor(fv.Decl.Kind) == d && st != null && st.Open && Mathf.Abs(st.Level - Mathf.RoundToInt(d.Opening * 100f)) >= 3) { DoorOpened(d, d.LastActor); break; } }
            }
            foreach (var fv in Bound)
            {
                var d = DoorFor(fv.Decl.Kind); var st = fv.State; if (d == null || st == null || !st.HasDoor) continue;
                d.Remote = st.Open ? Mathf.Max(0.05f, st.Level / 100f) : 0f;
            }
        }

        // ------------------------------------------------------------------ authority: physically inside + resting = placed
        readonly Dictionary<PropBody, float> _restIn = new Dictionary<PropBody, float>();
        public int Loaded { get; private set; }
        protected virtual void FixedUpdate()
        {
            var rt = RecipeRuntime.I;
            if (!GameServices.IsAuthority || rt == null || Bound.Count == 0 || rt.Round == null || !rt.Round.IsPlayable) return;
            foreach (var p in rt.Bodies)
            {
                if (p == null || p.IsHeld || p.Body == null) { if (p != null) _restIn.Remove(p); continue; }
                var rp = rt.RProp(p); if (rp == null || rp.IsPlaced || rp.HeldBy != ActorIds.None) { _restIn.Remove(p); continue; }
                RecipeFixtureView target = null;
                foreach (var fv in Bound) { var st = fv.State; if (st != null && st.RecipeInstance == rp.RecipeInstance && (!st.HasDoor || st.Open) && Inside(fv.Decl.Kind, p.Col.bounds.center)) { target = fv; break; } }
                if (target == null) { _restIn.Remove(p); continue; }
                _restIn.TryGetValue(p, out var t); t = p.Speed < 0.4f ? t + Time.fixedDeltaTime : 0f; _restIn[p] = t;
                if (t < 0.3f) continue;
                var s = target.State; int slot = -1;
                for (int k = 0; k < s.Occupant.Length && slot < 0; k++) if (s.Occupant[k] < 0 && !string.IsNullOrEmpty(s.SlotTags[k]) && s.SlotTags[k] == rp.Tag) slot = k;
                for (int k = 0; k < s.Occupant.Length && slot < 0; k++) if (s.Occupant[k] < 0 && string.IsNullOrEmpty(s.SlotTags[k])) slot = k;
                for (int k = 0; k < s.Occupant.Length && slot < 0; k++) if (s.Occupant[k] < 0) slot = k;
                if (slot < 0) continue;
                int actor = p.LastActor != ActorIds.None ? p.LastActor : rp.LastHolder;
                var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.PlaceInSocket, Prop = rp.Id, Prop2 = -1, Fixture = s.Id, Slot = slot });
                if (err != null) { Debug.Log($"[SHHH][mech] {rp.Kind}#{rp.Id} inside {s.Kind} not placed: {err}"); _restIn[p] = -2f; continue; }
                _restIn.Remove(p); Loaded++;
                bool square = RecipeFixtureView.Square(transform, p.transform);
                if (rp.Oriented != square) RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Align, Prop = rp.Id, Prop2 = -1, Fixture = s.Id, Slot = slot, Arg = square ? "" : "tilt" });
                if (AudioBank.I != null) AudioBank.I.Play("rustle", p.Col.bounds.center, 0.5f);
            }
        }
    }
}
