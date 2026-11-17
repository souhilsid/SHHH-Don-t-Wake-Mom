using System.Collections;
using System.IO;
using UnityEngine;

namespace SHHH.QA
{
    /// <summary>Play-mode VFX check (editor flag Automation/vfxgallery.flag): spawns every Resources/VFX prefab in a row,
    /// renders the camera at 0.25/0.5/0.9 s into QA_Evidence/vfx_gallery_t*.png and logs renderer shaders.</summary>
    public sealed class VfxGalleryRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Application.isEditor) return;
            var flag = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Automation", "vfxgallery.flag"));
            if (!File.Exists(flag)) return;
            File.Delete(flag);
            new GameObject("VfxGallery").AddComponent<VfxGalleryRuntime>();
        }

        IEnumerator Start()
        {
            var cam = new GameObject("galleryCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f, 0.09f, 0.25f);
            cam.transform.position = new Vector3(0, 1f, -4.2f);
            var prefabs = Resources.LoadAll<GameObject>("VFX");
            for (int i = 0; i < prefabs.Length; i++)
            {
                var g = Instantiate(prefabs[i], new Vector3(-3.2f + (i % 5) * 1.6f, i < 5 ? 1.9f : 0.1f, 0), Quaternion.identity);
                g.transform.localScale = Vector3.one * 0.6f;
                foreach (var a in g.GetComponentsInChildren<AudioSource>()) a.enabled = false;
                Debug.Log("[VFX] " + prefabs[i].name + " renderers=" + g.GetComponentsInChildren<Renderer>().Length);
            }
            foreach (var t in new[] { 0.25f, 0.5f, 0.9f })
            {
                yield return new WaitForSeconds(t == 0.25f ? 0.25f : t == 0.5f ? 0.25f : 0.4f);
                var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "QA_Evidence", $"vfx_gallery_t{t:0.00}.png")), tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null;
            }
            Debug.Log("[VFX] gallery done");
        }
    }
}
