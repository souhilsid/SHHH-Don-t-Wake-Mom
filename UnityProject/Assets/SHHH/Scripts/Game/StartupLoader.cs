using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.Game
{
    /// <summary>
    /// V12 (owner, Quest c15: ~10 s of grey void with a black wedge and silence after the Unity logo). The first scene used to be Boot,
    /// whose start-up work (audio bank, Genies SDK, rig, wrist menu, catalogs) runs before the first frame - the headset gets no frames
    /// and shows its grey void. Now the first scene is this tiny one: a black camera with the "SHHH! loading the house..." card, drawn at
    /// once, which then loads Boot in the background so frames keep flowing while it loads.
    /// </summary>
    public sealed class StartupLoader : MonoBehaviour
    {
        public string Next = "Boot";
        TMPro.TextMeshPro _dots;

        IEnumerator Start()
        {
            BootTimeline.Mark("startup scene: first frame");
            var card = new GameObject("loadingCard").transform; card.SetParent(transform, false); card.localPosition = new Vector3(0f, -0.05f, 1.2f);
            WorldText.Make(card, "title", "SHHH!", new Vector3(0f, 0.08f, 0f), 2.0f, Palette.Yellow, TMPro.TextAlignmentOptions.Center, 1.0f);
            _dots = WorldText.Make(card, "line", "loading the house", new Vector3(0f, -0.08f, 0f), 0.7f, Palette.Cream, TMPro.TextAlignmentOptions.Center, 1.0f);
            yield return null; yield return null;   // the card is on screen before any heavy loading starts
            var op = SceneManager.LoadSceneAsync(Next, LoadSceneMode.Single);
            while (op != null && !op.isDone)
            {
                if (_dots != null) _dots.text = "loading the house" + new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);
                yield return null;
            }
        }
    }

    /// <summary>V12: start-up timing into the on-device log (shhh_grablog.txt), so the next headset run shows where the seconds go.</summary>
    public static class BootTimeline
    {
        static float _t0 = -1f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Begin() { _t0 = Time.realtimeSinceStartup; GrabLog.Add($"boot: app start (t={_t0:0.00})"); }
        public static void Mark(string what) { GrabLog.Add($"boot: {what} at +{Time.realtimeSinceStartup - Mathf.Max(0f, _t0):0.00} s"); }
    }
}
