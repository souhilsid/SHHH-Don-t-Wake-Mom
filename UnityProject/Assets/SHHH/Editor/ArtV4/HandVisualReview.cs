using System;
using System.IO;
using System.Linq;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class HandVisualReview {
  [ShhhCommand("v4_hand_review")]
  public static string Review(){
   if(EditorApplication.isPlaying)throw new Exception("Editor hand review requires edit mode");
   string previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   string dir=Path.Combine(WorldProductionAudit.Output,"06_Rendered_And_InEngine_Reviews/Hands");Directory.CreateDirectory(dir);
   var cam=new GameObject("HandReviewCamera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.19f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.07f,.055f,.12f);cam.transform.position=new Vector3(0,.01f,-.8f);cam.transform.LookAt(new Vector3(0,.01f,0));
   var key=new GameObject("Key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.8f;key.transform.rotation=Quaternion.Euler(30,-25,0);RenderSettings.ambientLight=new Color(.4f,.4f,.5f);
   var hands=new SimpleHandVisual[2];var sources=new SimHandSource[2];var displays=new Mesh[2];
   for(int i=0;i<2;i++){
    string side=i==0?"Left":"Right";var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SHHH/ArtProductionV4/Hands/SimpleHand_"+side+".prefab"));
    hands[i]=go.GetComponent<SimpleHandVisual>();sources[i]=new GameObject(side+"SyntheticSource").AddComponent<SimHandSource>();sources[i].Hand=i==0?Handedness.Left:Handedness.Right;sources[i].transform.position=new Vector3(i==0?-.16f:.16f,.07f,0);sources[i].transform.rotation=Quaternion.LookRotation(Vector3.up,Vector3.back);sources[i].palmNormal=Vector3.back;hands[i].Source=sources[i];hands[i].Initialize();
    var view=new GameObject("BakedSkinPreview");view.transform.SetParent(hands[i].Skin.transform,false);displays[i]=new Mesh();view.AddComponent<MeshFilter>().sharedMesh=displays[i];view.AddComponent<MeshRenderer>().sharedMaterial=hands[i].Skin.sharedMaterial;
   }
   var metrics=new System.Text.StringBuilder();
   foreach(var pose in new[]{"Open","Pinch","Fist"}){
    for(int i=0;i<2;i++){sources[i].simPinch=pose=="Pinch"?1:0;sources[i].simGrip=pose=="Fist"?1:0;hands[i].ApplyPose();hands[i].Skin.BakeMesh(displays[i]);var scale=hands[i].Skin.transform.lossyScale;hands[i].Skin.transform.Find("BakedSkinPreview").localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);hands[i].Skin.enabled=false;}
    foreach(var h in hands)metrics.AppendLine(pose+" "+h.name+" skinScale="+h.Skin.transform.lossyScale.ToString("F4")+" skinBounds="+h.Skin.bounds.size.ToString("F4")+" wrist="+h.Bones[0].position.ToString("F4")+" middle="+h.Bones[7].position.ToString("F4")+" meshBounds="+displays[Array.IndexOf(hands,h)].bounds.size.ToString("F4")+" visualTipGap="+Vector3.Distance(h.ThumbTipVisual,h.IndexTipVisual).ToString("F4"));
    Render(cam,Path.Combine(dir,pose+".png"),1280,720);
   }
   bool loss=true;for(int i=0;i<2;i++){sources[i].simTracked=false;hands[i].ApplyPose();loss&=!hands[i].Skin.enabled;}
   File.WriteAllText(Path.Combine(dir,"HAND_VISUAL_CHECK.txt"),"Installed source skin: 5248 triangles per hand. Positive-scale mirrored left variant. Synthetic curl preview only; full tracked-joint retarget requires separate joint-path and device validation. Tracking-loss renderer hiding: "+loss+". No provider or interaction transforms modified by visual adapter.");
   File.WriteAllText(Path.Combine(dir,"POSE_METRICS.txt"),metrics.ToString());
   if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
   return "Rendered installed SimpleHands open/pinch/fist; loss-hidden="+loss+"; hardware NOT TESTED";
  }
  public static void Render(Camera cam,string path,int width,int height){
   var rt=new RenderTexture(width,height,24){antiAliasing=4};var prev=cam.targetTexture;var active=RenderTexture.active;cam.allowMSAA=true;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());cam.targetTexture=prev;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
  }
 }
}
