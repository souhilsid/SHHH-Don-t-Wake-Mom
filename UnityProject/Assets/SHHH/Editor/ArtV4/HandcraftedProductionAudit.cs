using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SHHH.Automation;
using SHHH.Game;
namespace SHHH.EditorTools {
 public static class HandcraftedProductionAudit {
  [Serializable] class AssetRecord {
   public string asset_id,role,source_blend,runtime_mesh,prefab,prefab_guid,collider,static_or_movable,lod_culling,uv_status,status,preview;
   public Vector3 bounds_metres;public int triangles,vertices,material_slots;public string[] materials,textures,grip_anchors;
   public bool source_exists,mesh_exists,headset_validated;public List<string> current_task_ids=new List<string>();
  }
  [Serializable] class Audit { public string capturedUtc,unity;public string device="UNVERIFIED: no standalone headset measurement";public List<AssetRecord> assets=new List<AssetRecord>(); }
  [ShhhCommand("v5_asset_manifest")]public static string Inspect(){
   var audit=new Audit{capturedUtc=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion};string a=HandcraftedAssetImport.A,output=HandcraftedAssetImport.Output;
   foreach(var path in Directory.GetFiles(a+"/Prefabs","*.prefab").OrderBy(p=>p)){
    var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);var filters=go.GetComponentsInChildren<MeshFilter>();var renderers=go.GetComponentsInChildren<Renderer>();var mats=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray();
    bool movable=new[]{"V5_Book","V5_Mug","V5_Plate","V5_ToyCar","V5_Cushion","V5_Remote"}.Contains(go.name);bool dressing=new[]{"V5_FamilyFrame","V5_PottedPlant","V5_SnackCluster","V5_Rug"}.Contains(go.name);
    var hull=go.transform.Find("AuthoredCollision");var record=new AssetRecord{asset_id=go.name,role=movable?"A":dressing?"C":"B",source_blend=output+"/Blender_Source/"+go.name+".blend",runtime_mesh=a+"/Models/"+go.name+".fbx",prefab=path.Replace('\\','/'),prefab_guid=AssetDatabase.AssetPathToGUID(path),bounds_metres=Prim.RendererBounds(go).size,triangles=filters.Sum(f=>f.sharedMesh.triangles.Length/3),vertices=filters.Sum(f=>f.sharedMesh.vertexCount),material_slots=renderers.Sum(r=>r.sharedMaterials.Length),materials=mats.Select(AssetDatabase.GetAssetPath).ToArray(),textures=mats.SelectMany(m=>m.GetTexturePropertyNames().Select(n=>m.GetTexture(n))).Where(t=>t).Select(AssetDatabase.GetAssetPath).Distinct().ToArray(),collider=hull?"Disabled BoxCollider template copied into authoritative PropBody root at spawn":dressing?"Nonphysical static dressing":"Compound BoxColliders installed by connected-house assembly",static_or_movable=movable?"Movable when spawned as a current chore prop; static decorative instances also exist":"Static environment",lod_culling="No authored LOD chain yet. Renderer frustum culling; static house batching enabled. Quest profile pending.",uv_status=filters.All(f=>f.sharedMesh.uv.Length>0&&f.sharedMesh.uv2.Length>0)?"UV0 padded paint atlas; Unity-generated UV1 lightmap charts":"CHECK UV CHANNELS",status="Implemented in living slice; visual acceptance remains open",preview=output+"/Evidence/Blender/"+go.name+".png",grip_anchors=go.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Grip_")).Select(t=>t.name+" @ "+t.localPosition.ToString("F4")).ToArray()};
    record.source_exists=File.Exists(record.source_blend);record.mesh_exists=File.Exists(record.runtime_mesh);
    string kind=go.name=="V5_Book"?"book":go.name=="V5_Mug"?"mug":go.name=="V5_Plate"?"plate":go.name=="V5_ToyCar"?"toy_car":go.name=="V5_Cushion"?"cushion":go.name=="V5_Remote"?"remote":null;
    if(kind!=null)record.current_task_ids=SHHH.Core.ChoreLibrary.All.Where(t=>t.PropKind==kind||(kind=="mug"&&t.PropKind=="cup")).Select(t=>t.Id).ToList();
    audit.assets.Add(record);
   }
   Directory.CreateDirectory(output+"/Audit");File.WriteAllText(output+"/Audit/UNITY_ASSET_MANIFEST.json",JsonUtility.ToJson(audit,true));
   return "Inspected "+audit.assets.Count+" actual Unity prefabs; source, mesh, UV, material, grip and collision records written. Device unverified.";
  }
 }
}
