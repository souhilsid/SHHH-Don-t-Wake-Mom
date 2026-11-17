using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 shared water levels: the authority's conserved liquid simulation reports a recipe vessel's amount as a journaled
    /// <see cref="Verb.Pour"/> op (millilitres) whenever it moved by a band (4 % of capacity, min 10 ml), when it empties, and once it settles;
    /// at most 4 reports per second. Every other peer runs the container as a replica (level + pour stream from the replicated RProp.Ml,
    /// no volume moved locally). House props that are not recipe props keep a host-local amount (not replicated yet).
    /// </summary>
    public sealed class VesselSync : MonoBehaviour
    {
        LiquidContainer _lc; PropBody _b; float _nextSend; public int Reports { get; private set; }

        void Awake() { _lc = GetComponentInChildren<LiquidContainer>(); _b = GetComponent<PropBody>(); }
        public LiquidContainer Liquid => _lc;

        void Update()
        {
            var rt = RecipeRuntime.I; if (_lc == null || _b == null || rt == null || rt.Round == null || rt.Round.Recipes == null) return;
            var rp = rt.RProp(_b); if (rp == null) return;
            if (!GameServices.IsAuthority) { _lc.Replica = true; if (!GameServices.HostStreamsLiquids) _lc.Amount = Mathf.Min(_lc.Capacity, rp.Ml / 1000f); return; }   // online the host's stream is the source
            _lc.Replica = false;
            if (Time.time < _nextSend || !rt.Round.IsPlayable) return;
            int ml = Mathf.RoundToInt(_lc.Amount * 1000f); if (ml == rp.Ml) return;
            int band = Mathf.Max(10, Mathf.RoundToInt(_lc.Capacity * 1000f * 0.04f));
            bool settled = !_lc.Pouring && Mathf.Abs(ml - rp.Ml) >= 2;
            if (Mathf.Abs(ml - rp.Ml) < band && ml != 0 && !settled) return;
            int actor = rp.HeldBy != ActorIds.None ? rp.HeldBy : rp.LastHolder != ActorIds.None ? rp.LastHolder : GameServices.LocalSlot;
            var err = RecipeRuntime.Submit(new ROp { Actor = actor, Verb = Verb.Pour, Prop = rp.Id, Prop2 = -1, Fixture = -1, Slot = -1, Arg = ml.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            if (err == null) Reports++;
            _nextSend = Time.time + 0.25f;
        }
    }
}
