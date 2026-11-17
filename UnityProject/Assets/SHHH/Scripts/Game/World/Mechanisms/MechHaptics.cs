using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// V12 tactile feedback for mechanisms (grab, detent click, end stop, rattle). Controllers get a short vibration through OVRInput;
    /// tracked hands have no actuator, so for them this is a no-op (the click sound and visible stop carry the feedback). Counts pulses
    /// so desktop QA can verify that feedback was requested (NOT a claim that a headset vibrated).
    /// </summary>
    public static class MechHaptics
    {
        public static int Requested;
        static float _stopL, _stopR;

        public static void Pulse(HandInteractor h, float amplitude, float seconds)
        {
            Requested++;
            if (h == null || h.Source == null) return;
            bool left = h.Source.Hand == Handedness.Left;
            try
            {
                var ctrl = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                if (!OVRInput.IsControllerConnected(ctrl)) return;
                OVRInput.SetControllerVibration(0.5f, Mathf.Clamp01(amplitude), ctrl);
                if (left) _stopL = Time.unscaledTime + seconds; else _stopR = Time.unscaledTime + seconds;
                MechHapticsRunner.Ensure();
            }
            catch (System.Exception) { /* no OVR runtime (desktop) */ }
        }

        internal static void Tick()
        {
            try
            {
                if (_stopL > 0 && Time.unscaledTime > _stopL) { OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch); _stopL = 0; }
                if (_stopR > 0 && Time.unscaledTime > _stopR) { OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch); _stopR = 0; }
            }
            catch (System.Exception) { }
        }
    }
}
