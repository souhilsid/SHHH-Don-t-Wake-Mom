#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace SHHH.Game {
 public sealed class HandcraftedSceneCapture:MonoBehaviour {
  static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
  static string Flag=>Root+"/Automation/v5_capture.flag";
  [Serializable]class Records{public Record[] records;}[Serializable]class Record{public string name;public Vector3 position,target;public float fov,orthoSize,nearClip;public bool orthographic;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Init(){if(File.Exists(Flag))new GameObject("V5_NativeSceneCapture").AddComponent<HandcraftedSceneCapture>();}
  IEnumerator Start(){
   var h=FindFirstObjectByType<HouseAssembly>();if(h)h.BindLighting();CartoonLook.Apply();
   var cam=Camera.main;var records=JsonUtility.FromJson<Records>(File.ReadAllText(Root+"/SHHH_ArtProduction_V5/00_Baseline/CAMERA_MATCH.json"));
   string label=File.ReadAllText(Flag).Trim();string output=Root+"/SHHH_ArtProduction_V5/"+(label=="Before"?"00_Baseline":"Evidence/Unity");Directory.CreateDirectory(output);
   yield return new WaitForSeconds(1);
   foreach(var r in records.records){cam.transform.position=r.position;cam.transform.LookAt(r.target);cam.fieldOfView=r.fov;cam.orthographic=r.orthographic;cam.orthographicSize=r.orthoSize>0?r.orthoSize:5;cam.nearClipPlane=r.nearClip>0?r.nearClip:.03f;yield return new WaitForSeconds(.2f);Capture(cam,output+"/"+label+"_"+r.name+".png");}
   File.WriteAllText(output+"/"+label+"_CAPTURE_RECEIPT.json","{\"source\":\"Unity Play mode native camera\",\"camera_record\":\"00_Baseline/CAMERA_MATCH.json\",\"hardware_validated\":false}");File.Delete(Flag);UnityEditor.EditorApplication.isPlaying=false;
  }
  static void Capture(Camera cam,string path){var prev=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24){antiAliasing=4};cam.allowMSAA=true;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var img=new Texture2D(1600,900,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,900),0,0);img.Apply();File.WriteAllBytes(path,img.EncodeToPNG());cam.targetTexture=prev;RenderTexture.active=active;Destroy(img);rt.Release();Destroy(rt);}
 }
}
#endif
