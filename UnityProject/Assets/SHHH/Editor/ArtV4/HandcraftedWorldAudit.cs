using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class HandcraftedWorldAudit {
  [Serializable]class Placement {public string asset;public Vector3 position,scale;public float yaw;}
  [Serializable]class Lamp {public string name,type;public Vector3 position;public Color color;public float intensity;}
  [Serializable]class RetainedPart {public string name,material,texture;public Color color;public Vector3[] vertices;public Vector2[] uv;public int[] triangles;}
  [Serializable]class RetainedRooms {public string provenance="Retained actual V4 project mesh; not V5 art acceptance";public List<RetainedPart> parts=new List<RetainedPart>();}
  [Serializable]class Manifest {public string sourceScene;public bool hardwareValidated=false;public Placement[] placements;public Lamp[] lights;public int activeMeshTriangles,activeRenderers,boundLightmapRenderers;public string[] lightmapPaths;}
  [ShhhCommand("v5_world_manifest")]
  public static string Export(){
   var h=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!h)throw new Exception("No connected house in open scene");var living=h.transform.Find("LivingRoom");
   var m=new Manifest{sourceScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,placements=living.Cast<Transform>().Where(t=>t.name.StartsWith("V5_")).Select(t=>new Placement{asset=t.name,position=t.localPosition,scale=t.localScale,yaw=t.localEulerAngles.y}).ToArray(),lights=h.GetComponentsInChildren<Light>().Select(l=>new Lamp{name=l.name,type=l.type.ToString(),position=l.transform.position,color=l.color,intensity=l.intensity}).ToArray()};
   var rs=h.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();m.activeRenderers=rs.Length;m.activeMeshTriangles=rs.Sum(r=>r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length/3??0);m.boundLightmapRenderers=rs.Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<LightmapSettings.lightmaps.Length);m.lightmapPaths=LightmapSettings.lightmaps.Select(l=>AssetDatabase.GetAssetPath(l.lightmapColor)).ToArray();
   var retained=new RetainedRooms();
   foreach(string roomName in new[]{"Kitchen","Laundry","MomBedroom"})foreach(var mf in h.transform.Find(roomName).GetComponentsInChildren<MeshFilter>()){
    var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!mf.sharedMesh)continue;var mesh=mf.sharedMesh;
    for(int s=0;s<mesh.subMeshCount;s++){var mat=renderer.sharedMaterials[Mathf.Min(s,renderer.sharedMaterials.Length-1)];retained.parts.Add(new RetainedPart{name=roomName+"_"+mf.name+"_"+s,material=mat.name,color=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):Color.white,texture=mat.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")):"",vertices=mesh.vertices.Select(v=>mf.transform.TransformPoint(v)).ToArray(),uv=mesh.uv,triangles=mesh.GetTriangles(s)});}
   }
   File.WriteAllText(HandcraftedAssetImport.Output+"/RETAINED_ROOMS_MESH_SOURCE.json",JsonUtility.ToJson(retained));
   File.WriteAllText(HandcraftedAssetImport.Output+"/UNITY_LIVING_ASSEMBLY.json",JsonUtility.ToJson(m,true));return "Exported actual placement/light/native geometry manifest; "+m.activeMeshTriangles+" house triangles / "+m.activeRenderers+" active renderers / "+m.boundLightmapRenderers+" bound lightmaps; Quest unverified.";
  }
 }
}
