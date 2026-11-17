#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace SHHH.Game {
 public sealed class HandcraftedRouteCapture:MonoBehaviour {
  static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
  static string Flag=>Root+"/Automation/v5_walk.flag";
  NavMeshData data;NavMeshDataInstance handle;int agentId;int frame;
  [Serializable]class Receipt{public string description="Actual Unity scene, camera following physics-derived route. Door opened for geometry review. Not Mom animation, VR tracking or player-input proof.";public Vector3[] route;public bool hardwareValidated=false;public int frames;public float nominalFps=8;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Init(){if(File.Exists(Flag))new GameObject("V5_RouteGeometryCapture").AddComponent<HandcraftedRouteCapture>();}
  IEnumerator Start(){
   var house=FindFirstObjectByType<HouseAssembly>();house.BindLighting();CartoonLook.Apply();var cam=Camera.main;
   var settings=NavMesh.CreateSettings();agentId=settings.agentTypeID;settings.agentRadius=.2f;settings.agentHeight=1.75f;settings.agentClimb=.19f;settings.agentSlope=35;settings.overrideVoxelSize=true;settings.voxelSize=.04f;
   var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(house.transform,(1<<0)|(1<<Layers.Furniture),NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
   data=NavMeshBuilder.BuildNavMeshData(settings,sources,house.WorldBounds,Vector3.zero,Quaternion.identity);handle=NavMesh.AddNavMeshData(data);
   var filter=new NavMeshQueryFilter{agentTypeID=agentId,areaMask=NavMesh.AllAreas};
   NavMesh.SamplePosition(new Vector3(1.7f,0,1.6f),out var start,1,filter);NavMesh.SamplePosition(house.Bedside.position,out var end,.5f,filter);
   var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,end.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Living-to-bedside route incomplete");
   string dir=Root+"/SHHH_ArtProduction_V5/Evidence/Runtime/RouteFrames";Directory.CreateDirectory(dir);cam.fieldOfView=72;cam.nearClipPlane=.04f;cam.orthographic=false;
   cam.transform.position=path.corners[0]+Vector3.up*1.6f;cam.transform.LookAt(path.corners[1]+Vector3.up*1.6f);yield return new WaitForSeconds(.5f);
   float next=0;for(int i=1;i<path.corners.Length;i++){
    Vector3 a=path.corners[i-1],b=path.corners[i];float duration=Vector3.Distance(a,b)/.7f,t=0;
    while(t<duration){t+=Time.deltaTime;Vector3 basePos=Vector3.Lerp(a,b,Mathf.Clamp01(t/duration));cam.transform.position=basePos+Vector3.up*1.6f;
     Vector3 aim=b-a;aim.y=0;if(aim.sqrMagnitude>.001f)cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(aim),Time.deltaTime*3);
     if(Vector3.Distance(basePos,house.BedroomDoor.position)<1.6f)house.BedroomDoor.localRotation=Quaternion.RotateTowards(house.BedroomDoor.localRotation,Quaternion.Euler(0,-90,0),Time.deltaTime*80);
     if(Time.time>=next){next=Time.time+.125f;Capture(cam,dir+"/"+(frame++).ToString("D5")+".png");}yield return null;
    }
   }
   cam.transform.LookAt(house.Bedside.position+new Vector3(1.1f,.6f,.2f));Capture(cam,Root+"/SHHH_ArtProduction_V5/Evidence/Runtime/Route_Bedside.png");
   File.WriteAllText(Root+"/SHHH_ArtProduction_V5/Evidence/Runtime/ROUTE_CAPTURE.json",JsonUtility.ToJson(new Receipt{route=path.corners,frames=frame},true));File.Delete(Flag);UnityEditor.EditorApplication.isPlaying=false;
  }
  static void Capture(Camera cam,string path){var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(960,540,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var img=new Texture2D(960,540,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,960,540),0,0);img.Apply();File.WriteAllBytes(path,img.EncodeToPNG());cam.targetTexture=old;RenderTexture.active=active;Destroy(img);rt.Release();Destroy(rt);}
  void OnDestroy(){handle.Remove();if(agentId!=0)NavMesh.RemoveSettings(agentId);if(data)Destroy(data);}
 }
}
#endif
