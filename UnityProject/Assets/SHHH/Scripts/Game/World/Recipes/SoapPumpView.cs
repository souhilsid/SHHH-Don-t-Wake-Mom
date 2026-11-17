using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V9 pumping primitive (KT01): the daycare KIT026 dish-soap pump (CC0) with its real PART_PumpHead pivot. Push the press pad down
    /// with a hand (or poke "Pump"): the head travels 8 mm and springs back; each stroke is one dose that comes out of the nozzle.
    /// A dose only counts when a held sponge is under the nozzle - then the authority submits Fill(sponge, this fixture) and the sponge
    /// turns soapy. A dose with nothing under it lands on the table (wasted, visible foam blob). Rapid repeated doses = overflow (logged
    /// for the soap-overflow comedy incident).
    /// </summary>
    public sealed class SoapPumpView : MonoBehaviour
    {
        RecipeFixtureView _fv; RecipeRuntime _rt; Transform _head, _pad, _nozzle; PokeButton _key;
        float _press, _cool, _doseWindowT, _clearFor; int _dosesInWindow; bool _handDown, _armed = true;
        Vector3 _headRest;
        public int Doses { get; private set; }
        public int WastedDoses { get; private set; }

        /// <summary>World point just below the nozzle outlet where a sponge catches the soap.</summary>
        public Vector3 CatchPoint => (_nozzle != null ? _nozzle.position : transform.position + Vector3.up * 0.2f) + Vector3.down * 0.04f;
        public Vector3 PadWorld => _pad != null ? _pad.position : transform.position + Vector3.up * 0.22f;
        /// <summary>V13: the TOP SURFACE of the press pad (from its mesh bounds, not the part's pivot) - where a hand actually pushes.</summary>
        public Vector3 PadTop
        {
            get
            {
                var r = _pad != null ? _pad.GetComponentInChildren<Renderer>() : null;
                if (r == null && _head != null) r = _head.GetComponentInChildren<Renderer>();
                if (r == null) return PadWorld;
                var b = r.bounds; return new Vector3(b.center.x, b.max.y, b.center.z);
            }
        }
        public static readonly System.Collections.Generic.List<SoapPumpView> All = new System.Collections.Generic.List<SoapPumpView>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Build(RecipeFixtureView fv, RecipeRuntime rt, float baseH)
        {
            _fv = fv; _rt = rt;
            var am = ArticulatedModel.Build("dc_kit026", transform, "soap_pump", 0.09f, Layers.Furniture);
            if (am.Ok)
            {
                am.Root.localPosition = new Vector3(0, baseH, 0);
                _head = am.Part("PART_PumpHead");
                foreach (var t in am.Root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("Pump_LowOvalPad")) _pad = t;
                    if (t.name.StartsWith("Pump_CurvedNozzle")) _nozzle = t;
                }
            }
            else
            {
                // fallback (model missing): a plain bottle so the mechanic still works; logged by ArticulatedModel
                Prim.Shape(PrimitiveType.Cylinder, transform, "bottle", new Vector3(0, baseH + 0.08f, 0), new Vector3(0.07f, 0.08f, 0.07f), Mats.Lit(new Color(0.55f, 0.85f, 0.75f)));
                _head = Prim.Shape(PrimitiveType.Cylinder, transform, "head", new Vector3(0, baseH + 0.18f, 0), new Vector3(0.03f, 0.01f, 0.03f), Mats.Lit(Palette.Cream)).transform;
            }
            if (_pad == null) _pad = _head; if (_nozzle == null) _nozzle = _head;
            if (_head != null) _headRest = _head.localPosition;
            // V10: no "Pump" poke key - the pad is pressed by a hand (or a bot's reach) like a real dispenser
        }

        /// <summary>One pump stroke by an actor (hand push, poke key, bot adapter).</summary>
        public void Stroke(int actor)
        {
            if (_cool > 0f) return;
            _cool = 0.35f; _press = 1f;
            AudioBank.I?.Play("sponge", CatchPoint, 0.35f, 0.15f);
            RoomFx.Burst("bubbles", CatchPoint, 0.06f, 0.8f);
            // only the authority doses (it sees every peer's hands, so a remote player's push on the pad is detected there)
            if (!GameServices.IsAuthority || _rt == null) return;
            Doses++;
            _doseWindowT = _doseWindowT <= 0f ? 5f : _doseWindowT; _dosesInWindow++;
            if (_dosesInWindow >= 4) { Debug.Log($"[SHHH][physics] soap overflow at {name}: {_dosesInWindow} doses in 5 s (incident candidate, actor {actor})"); RoomFx.Burst("bubbles", CatchPoint + Vector3.down * 0.1f, 0.2f, 1.5f); }
            // which sponge is under the nozzle? (held or resting) - the dose goes into it
            PropBody sponge = null; float best = 0.07f;
            foreach (var b in _rt.Bodies)
            {
                if (b == null || b.Spec.Kind != "sponge") continue;
                float d = Vector3.Distance(b.Col.ClosestPoint(CatchPoint), CatchPoint); if (d < best) { best = d; sponge = b; }
            }
            var rp = _rt.RProp(sponge);
            if (rp == null) { WastedDoses++; return; }
            int who = sponge.IsHeld ? sponge.HeldBy : actor;
            var err = RecipeRuntime.Submit(new ROp { Actor = who, Verb = Verb.Fill, Prop = rp.Id, Prop2 = -1, Fixture = _fv.State.Id, Slot = -1 });
            if (err != null) Debug.Log($"[SHHH][physics] soap dose into sponge#{rp.Id} rejected: {err}");
        }

        void Update()
        {
            _cool -= Time.deltaTime; _doseWindowT -= Time.deltaTime; if (_doseWindowT <= 0f) _dosesInWindow = 0;
            _press = Mathf.MoveTowards(_press, 0f, Time.deltaTime / 0.25f);
            if (_head != null) _head.localPosition = _headRest + Vector3.down * (0.008f / Mathf.Max(1e-4f, _head.lossyScale.y) * Mathf.SmoothStep(0, 1, _press));
        }

        void FixedUpdate()
        {
            if (!GameServices.IsAuthority || _rt == null || _rt.Round == null || !_rt.Round.IsPlayable) return;
            // V13: any part of a hand (fingertip, pinch, palm, fist) on the pad: within 5 cm of its centre, from 2.5 cm below its top to 3 cm
            // above it (a hand resting there counts once: one stroke per touch; lift the hand to pump again)
            bool down = _rt.HandPress(PadTop, 0.05f, 0.025f, 0.03f, out int actor);
            // one squirt per press: re-arm only once every part of the hand has lifted clearly off (8 cm) for 0.2 s
            bool clear = !_rt.HandPress(PadTop, 0.08f, 0.04f, 0.08f, out _);
            _clearFor = clear ? _clearFor + Time.fixedDeltaTime : 0f;
            if (_clearFor > 0.2f) _armed = true;
            if (down && _armed) { _armed = false; Stroke(actor); }
            _handDown = down;
        }
    }
}
