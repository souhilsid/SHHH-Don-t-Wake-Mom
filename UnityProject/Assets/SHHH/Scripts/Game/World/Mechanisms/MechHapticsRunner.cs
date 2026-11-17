using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Stops controller vibration pulses started by <see cref="MechHaptics"/> (one hidden object, created on first use).</summary>
    public sealed class MechHapticsRunner : MonoBehaviour
    {
        static MechHapticsRunner _r;
        public static void Ensure() { if (_r == null) { var g = new GameObject("MechHaptics"); DontDestroyOnLoad(g); _r = g.AddComponent<MechHapticsRunner>(); } }
        void Update() { MechHaptics.Tick(); }
    }
}
