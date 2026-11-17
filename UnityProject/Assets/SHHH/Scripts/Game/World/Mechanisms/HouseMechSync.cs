using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 shared house mechanisms: a door / drawer / lid that is NOT a recipe fixture reports its opening (0..100) as a journaled
    /// <see cref="Verb.Mech"/> op when a hand opens it, closes it, or lets go of it (host applies; clients send it as an intent), and
    /// every peer's part eases to the round's <c>HouseMech</c> value. Recipe-bound doors are driven by their fixture state instead.
    /// </summary>
    public sealed class HouseMechSync : MonoBehaviour
    {
        MechPart _p; int _sent = -1; float _resendAt = -1f; int _resendActor;
        public int Reports { get; private set; }

        void Awake()
        {
            _p = GetComponent<MechPart>(); if (_p == null) { enabled = false; return; }
            _p.Opened += (m, a) => Send(a); _p.Closed += (m, a, s) => Send(a);
            _p.Released += (m, a) => { _resendAt = Time.time + 0.6f; _resendActor = a; };   // publish where it comes to rest
        }

        void Send(int actor)
        {
            if (_p.BoundToRecipe || _p.HouseId < 0) return;
            var round = GameServices.Round; if (round == null) return;
            int lvl = Mathf.RoundToInt(Mathf.Clamp01(_p.Opening) * 100f); if (lvl == _sent) return;
            string arg = lvl.ToString(System.Globalization.CultureInfo.InvariantCulture) + (_p.PendingSlam ? ":slam" : ""); _p.PendingSlam = false;
            var op = new ROp { Actor = actor != ActorIds.None ? actor : GameServices.LocalSlot, Verb = Verb.Mech, Prop = -1, Prop2 = -1, Fixture = -1, Slot = _p.HouseId, Arg = arg };
            string err = GameServices.IsAuthority ? round.RecipeAction(op) : GameServices.SendRecipeIntent != null && GameServices.SendRecipeIntent(op) ? null : "not connected";
            if (err == null || err == "no change") { _sent = lvl; if (err == null) Reports++; }
        }

        float _stableFor, _lastOpening = -1f;
        void Update()
        {
            if (_p == null || _p.BoundToRecipe || _p.HouseId < 0) return;
            // settled after THIS peer moved it (hand, script, bot): publish the resting opening (not only the moment it left its stop)
            // at rest = slower than 3 % of travel per second (a per-frame threshold called a moving part "settled" at high frame rates)
            float rate = _lastOpening < 0f ? 1f : Mathf.Abs(_p.Opening - _lastOpening) / Mathf.Max(Time.deltaTime, 1e-4f); _lastOpening = _p.Opening;
            if (rate < 0.03f) _stableFor += Time.deltaTime; else _stableFor = 0f;
            if (!_p.IsHeld && _stableFor > 0.4f && Time.time - _p.LastLocalTouch < 4f && Mathf.Abs(Mathf.RoundToInt(_p.Opening * 100f) - _sent) >= 3) Send(_p.LastActor);
            if (_resendAt > 0f && Time.time > _resendAt && !_p.IsHeld) { _resendAt = -1f; Send(_resendActor); }
            var round = GameServices.Round;
            if (round != null && round.HouseMech.TryGetValue(_p.HouseId, out var lvl)) _p.Remote = lvl / 100f;
        }
    }
}
