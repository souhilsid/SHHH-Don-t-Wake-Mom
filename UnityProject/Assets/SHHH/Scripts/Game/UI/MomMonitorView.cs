using SHHH.Core;
using TMPro;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>"Baby monitor" on the wall by Mom's door: five lamps + status words for the sleep meter.</summary>
    public sealed class MomMonitorView : MonoBehaviour
    {
        Renderer[] _lamps; TextMeshPro _label;
        public void Build()
        {
            Prim.Box(transform, "body", new Vector3(0, 0, 0.02f), new Vector3(0.5f, 0.22f, 0.04f), Mats.Lit(Palette.Cream), false);
            _lamps = new Renderer[5];
            for (int i = 0; i < 5; i++) _lamps[i] = Prim.Shape(PrimitiveType.Sphere, transform, "lamp" + i, new Vector3(-0.18f + i * 0.09f, 0.05f, -0.005f), Vector3.one * 0.05f, Mats.Unlit(Palette.Ink)).GetComponent<Renderer>();
            _label = WorldText.Make(transform, "label", "Zzz...", new Vector3(0, -0.055f, -0.01f), 0.5f, Palette.Navy, TextAlignmentOptions.Center, 0.48f);
            _label.outlineWidth = 0;
        }

        void Update()
        {
            var r = GameServices.Round; if (r == null || _lamps == null) return;
            float v = r.Meter.Value; int lit = Mathf.Clamp(Mathf.CeilToInt(v / 20f), 0, 5);
            Color[] cols = { Palette.Teal, Palette.Teal, Palette.Yellow, Palette.Coral, Palette.Danger };
            bool blink = v >= 85f && Mathf.Repeat(Time.time, 0.5f) < 0.25f;
            for (int i = 0; i < 5; i++) _lamps[i].sharedMaterial = Mats.Unlit(i < lit && !(blink && i == lit - 1) ? cols[i] : Palette.Ink);
            if (r.Mom.RoundFrozen) { _label.text = "MOM IS UP!"; return; }   // V12: the sign said "ALMOST AWAKE" while Mom was out
            _label.text = r.Meter.Stage switch { SleepStage.DeepSleep => "Zzz... deep sleep", SleepStage.Stirring => "Stirring...", SleepStage.Suspicious => "Suspicious!", SleepStage.AlmostAwake => "ALMOST AWAKE!", _ => "AWAKE!!" };
        }
    }
}
