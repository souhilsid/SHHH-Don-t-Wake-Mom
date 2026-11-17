using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SHHH.Game
{
    /// <summary>
    /// Cartoon "Midnight Marmalade" look, Quest-budgeted: cool lavender sky / warm amber ground trilight ambient (feeds the toon
    /// shader's SH fill), soft linear fog for depth, and ONE cheap URP post stack: Neutral tonemapping + punchy colour adjustments +
    /// a very light vignette. No bloom, DoF, motion blur, chromatic aberration or screen-space refraction (VR comfort + GPU).
    /// </summary>
    public static class CartoonLook
    {
        static Volume _vol;

        public static void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex(0x7A7FC9);
            RenderSettings.ambientEquatorColor = Palette.Hex(0x9C7A9E);
            RenderSettings.ambientGroundColor = Palette.Hex(0x6B4A3E);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Hex(0x3A2F5E); RenderSettings.fogStartDistance = 6f; RenderSettings.fogEndDistance = 22f;
            if (_vol == null)
            {
                var go = new GameObject("CartoonPost"); Object.DontDestroyOnLoad(go);
                _vol = go.AddComponent<Volume>(); _vol.isGlobal = true; _vol.priority = 10;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
                var ca = prof.Add<ColorAdjustments>(true);
                ca.postExposure.Override(0.25f); ca.contrast.Override(14f); ca.saturation.Override(22f); ca.colorFilter.Override(Palette.Hex(0xFFF1E4));
                var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.14f); vg.smoothness.Override(0.5f); vg.color.Override(Palette.Hex(0x1B1338));
                _vol.sharedProfile = prof;
            }
            EnablePostOnCameras();
        }

        /// <summary>Post-processing is opt-in per camera in URP; call after the XR rig exists.</summary>
        public static void EnablePostOnCameras()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var d = cam.GetUniversalAdditionalCameraData();
                if (d != null) { d.renderPostProcessing = true; d.antialiasing = AntialiasingMode.None; }
            }
        }
    }
}
