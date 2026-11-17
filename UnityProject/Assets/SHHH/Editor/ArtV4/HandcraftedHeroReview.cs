using System.IO;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class HandcraftedHeroReview {
  [ShhhCommand("v5_hero_mesh_review")]
  public static string Review(){
   if(EditorApplication.isPlaying||Lightmapping.isRunning)return "BUSY";
   string previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var sofa=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HandcraftedAssetImport.A+"/Prefabs/V5_Sofa.prefab"));
   var cam=new GameObject("Actual imported FBX review").AddComponent<Camera>();cam.transform.position=new Vector3(2.4f,1.8f,3.5f);cam.transform.LookAt(new Vector3(0,.5f,0));cam.orthographic=true;cam.orthographicSize=1.5f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.07f,.12f);
   var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.87f,.73f);light.intensity=2;light.transform.rotation=Quaternion.Euler(30,160,0);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.5f);LightmapSettings.lightmaps=new LightmapData[0];
   HandVisualReview.Render(cam,HandcraftedAssetImport.Output+"/Evidence/Unity/Imported_Sofa_Unbaked.png",1280,900);
   foreach(var r in sofa.GetComponentsInChildren<Renderer>()){var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",r.sharedMaterial.GetTexture("_BaseMap"));r.sharedMaterial=mat;}
   HandVisualReview.Render(cam,HandcraftedAssetImport.Output+"/Evidence/Unity/Imported_Sofa_Unlit.png",1280,900);
   EditorSceneManager.OpenScene(previous);return "Captured actual imported sofa with direct illumination and unlit material to separate mesh and bake defects.";
  }
 }
}
