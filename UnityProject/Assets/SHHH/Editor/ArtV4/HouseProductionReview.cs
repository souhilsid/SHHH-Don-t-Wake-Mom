using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace SHHH.EditorTools {
 public static class HouseProductionReview {
  [ShhhCommand("v4_house_review")]
  public static string Review(){
   if(EditorApplication.isPlaying)throw new Exception("Use edit mode production scene");var house=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!house)throw new Exception("House missing");house.BindLighting();var cam=Camera.main;
   string dir=Path.Combine(WorldProductionAudit.Output,"06_Rendered_And_InEngine_Reviews/House");Directory.CreateDirectory(dir);
   var names=new[]{"LivingRoom","Kitchen","Laundry","Stairs","Landing","MomBedroom"};
   var positions=new[]{new Vector3(-2.5f,1.6f,-2.8f),new Vector3(8.6f,1.6f,-2.4f),new Vector3(7.7f,1.6f,3.75f),new Vector3(-2.9f,1.6f,2.9f),new Vector3(-1.6f,4.6f,2.6f),new Vector3(1.3f,4.6f,5.25f)};
   var targets=new[]{new Vector3(.7f,1.2f,1.1f),new Vector3(5.7f,1.2f,1.5f),new Vector3(6,1.1f,7.8f),new Vector3(-2.95f,2.2f,5.8f),new Vector3(1.3f,4,4.3f),new Vector3(1.6f,3.8f,7.4f)};
   for(int i=0;i<names.Length;i++){cam.transform.position=positions[i];cam.transform.LookAt(targets[i]);cam.fieldOfView=72;HandVisualReview.Render(cam,Path.Combine(dir,names[i]+".png"),1600,900);}cam.transform.position=positions[0];cam.transform.LookAt(targets[0]);
   return Navigation(house,Path.Combine(WorldProductionAudit.Output,"07_Performance_And_QA/HOUSE_NAVIGATION.txt"));
  }
  [ShhhCommand("v5_navigation")]
  public static string ReviewNavigation(){var house=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!house)throw new Exception("House missing");return Navigation(house,HandcraftedAssetImport.Output+"/Evidence/Runtime/HOUSE_NAVIGATION.txt",true);}
  static string Navigation(HouseAssembly house,string report,bool verifyVisibleGeometry=false){
   var sb=new StringBuilder();var settings=NavMesh.CreateSettings();settings.agentRadius=.2f;settings.agentHeight=1.75f;settings.agentClimb=.19f;settings.agentSlope=35;settings.overrideVoxelSize=true;settings.voxelSize=.04f;
   var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(house.transform,(1<<0)|(1<<Layers.Furniture),NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
   var data=NavMeshBuilder.BuildNavMeshData(settings,sources,house.WorldBounds,Vector3.zero,Quaternion.identity);var handle=NavMesh.AddNavMeshData(data);var filter=new NavMeshQueryFilter{agentTypeID=settings.agentTypeID,areaMask=NavMesh.AllAreas};
   Vector3[] routes={new Vector3(1.8f,0,2.7f),new Vector3(4.2f,0,2.15f),new Vector3(4.2f,0,4.15f),new Vector3(-2.95f,0,2.8f),new Vector3(-2.3f,1.5f,6.15f),new Vector3(-1.65f,3,2.7f),house.Bedside.position};
   int pass=0;Vector3 start=new Vector3(1.7f,0,1.6f);var startOk=NavMesh.SamplePosition(start,out var startHit,1,filter);sb.AppendLine("Physics-derived navmesh: radius 0.20m height 1.75m climb 0.19m slope 35; sources="+sources.Count);
   for(int i=0;i<routes.Length;i++){bool sampled=NavMesh.SamplePosition(routes[i],out var hit,.5f,filter);var path=new NavMeshPath();bool valid=startOk&&sampled&&NavMesh.CalculatePath(startHit.position,hit.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete;if(valid)pass++;sb.AppendLine((valid?"PASS":"FAIL")+" route from living to "+routes[i]+" status="+path.status+" corners="+path.corners.Length+" nearest="+(sampled?hit.position.ToString():"NONE"));}
   int geometryHits=0;
   if(verifyVisibleGeometry&&startOk&&NavMesh.SamplePosition(house.Bedside.position,out var bed,.5f,filter)){
    var route=new NavMeshPath();NavMesh.CalculatePath(startHit.position,bed.position,filter,route);
    var temporary=new List<MeshCollider>();var probeObject=new GameObject("V5_TemporaryBodyClearanceProbe");var probe=probeObject.AddComponent<CapsuleCollider>();probe.radius=.18f;probe.height=1.5f;probe.enabled=false;
    try{
     foreach(var mf in house.GetComponentsInChildren<MeshFilter>())if(mf.transform.parent&&mf.transform.parent.name.StartsWith("V5_")&&(mf.transform.parent.name=="V5_LivingArchitecture"||mf.transform.parent.name=="V5_HallStairs")){var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;temporary.Add(mc);}
     for(int i=1;i<route.corners.Length;i++){
      var a=route.corners[i-1];var b=route.corners[i];int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.12f));
      for(int j=0;j<=samples;j++){var p=Vector3.Lerp(a,b,j/(float)samples)+Vector3.up*.95f;
       foreach(var mc in temporary)if(Physics.ComputePenetration(probe,p,Quaternion.identity,mc,mc.transform.position,mc.transform.rotation,out var direction,out float depth)&&depth>.015f){geometryHits++;if(geometryHits<=12)sb.AppendLine("VISIBLE_GEOMETRY_OBSTRUCTION "+mc.transform.parent.name+" body="+p+" depth="+depth);}
      }
     }
     sb.AppendLine("Visible Blender architecture clearance: "+(geometryHits==0?"PASS":"FAIL")+"; capsule radius .18m / height 1.5m / base .20m above navmesh; tested meshes="+temporary.Count+"; penetrations="+geometryHits);
     if(temporary.Count<2){geometryHits++;sb.AppendLine("FAIL: both authored architecture meshes were not tested");}
    }finally{foreach(var mc in temporary)UnityEngine.Object.DestroyImmediate(mc);UnityEngine.Object.DestroyImmediate(probeObject);}
   }
   handle.Remove();NavMesh.RemoveSettings(settings.agentTypeID);UnityEngine.Object.DestroyImmediate(data);File.WriteAllText(report,sb.ToString());return "Physics-derived navigation "+pass+"/"+routes.Length+"; visible architecture obstructions="+geometryHits+". Device performance unverified.";
  }
  [Serializable] class Export { public string units="metres",coordinates="Unity Y up; convert Blender (x,z,y) and reverse winding";public List<Part> parts=new List<Part>(); }
  [Serializable] class Part { public string name,material,texture;public Color color;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles; }
  [ShhhCommand("v4_export_house_source")]
  public static string ExportSource(){var h=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!h)throw new Exception("House missing");var data=new Export();foreach(var mf in h.GetComponentsInChildren<MeshFilter>()){
    var r=mf.GetComponent<MeshRenderer>();if(!r||!r.enabled||!mf.sharedMesh)continue;var mesh=mf.sharedMesh;for(int s=0;s<mesh.subMeshCount;s++){var mat=r.sharedMaterials[Mathf.Min(s,r.sharedMaterials.Length-1)];data.parts.Add(new Part{name=mf.transform.parent.name+"_"+mf.name+"_"+s,material=mat.name,color=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):Color.white,texture=mat.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")):"",vertices=mesh.vertices.Select(v=>mf.transform.TransformPoint(v)).ToArray(),normals=mesh.normals.Select(v=>mf.transform.TransformDirection(v)).ToArray(),uv=mesh.uv,triangles=mesh.GetTriangles(s)});}}
   string file=Path.Combine(WorldProductionAudit.Output,"02_Blender_Source/ConnectedHouse_MeshSource.json");File.WriteAllText(file,JsonUtility.ToJson(data));return "Exported "+data.parts.Count+" authored/reused Unity mesh parts to "+file;}
 }
}
