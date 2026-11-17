using System;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Core;
using SHHH.Game;
using SHHH.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class HandcraftedLivingIntegration {
  const string A=HandcraftedAssetImport.A;
  static Transform living;
  [ShhhCommand("v5_assemble_living")]
  public static string Assemble(){
   if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
   EditorSceneManager.OpenScene("Assets/SHHH/ArtProductionV4/World/Scenes/ConnectedHouse_Production.unity");var house=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!house)throw new Exception("Preserved connected house missing");
   living=house.transform.Find("LivingRoom");if(!living)throw new Exception("Canonical living root missing");
   foreach(string id in new[]{"V5_LivingArchitecture","V5_HallStairs","V5_Sofa","V5_CoffeeTable","V5_Bookcase","V5_TaskTable","V5_Armchair","V5_Book","V5_Cushion","V5_PottedPlant","V5_FamilyFrame","V5_CurtainPair","V5_SnackCluster","V5_Remote","V5_Mug","V5_ToyCar","V5_FloorLamp","V5_TVConsole","V5_ShoeRack","V5_ToyChest","V5_SideTable","V5_DogBed","V5_CatBed","V5_Rug"})if(!AssetDatabase.LoadAssetAtPath<GameObject>(A+"/Prefabs/"+id+".prefab"))throw new Exception("Import required before assembly: "+id);
   // Keep the validated metre positions and architecture colliders, replace only this room's visible art.
   foreach(var r in living.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r);
   foreach(var f in living.Cast<Transform>().Where(t=>t.name.StartsWith("F_")||t.name.StartsWith("a2_")).ToArray())UnityEngine.Object.DestroyImmediate(f.gameObject);
   Put("V5_LivingArchitecture",Vector3.zero,0);
   foreach(string section in new[]{"Hall","UpperLanding"})foreach(var r in house.transform.Find(section).GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r);
   Put("V5_HallStairs",Vector3.zero,0);
   // New upper enclosure planes have corresponding simple physical barriers.
   Collider(living,new Vector3(3.65f,4.35f,4),new Vector3(.12f,2.7f,1.8f));
   Collider(living,new Vector3(-2.3f,4.4f,6.65f),new Vector3(2.7f,2.6f,.15f));
   RemoveDuplicateSharedWall(house.transform.Find("Kitchen"));
   var def=RoomCatalog.Get(RoomId.LivingRoom);
   foreach(var f in def.Furniture){
    string id=f.Kind=="sofa"?"V5_Sofa":f.Kind=="coffee_table"?"V5_CoffeeTable":f.Kind=="bookshelf"?"V5_Bookcase":f.Kind=="tv_stand"?"V5_TVConsole":f.Kind=="lamp_floor"?"V5_FloorLamp":f.Kind=="rug"?"V5_Rug":f.Kind=="armchair"?"V5_Armchair":f.Kind=="side_table"?"V5_SideTable":f.Kind=="mess_table"?"V5_TaskTable":f.Kind=="shoe_rack"?"V5_ShoeRack":f.Kind=="toy_box"?"V5_ToyChest":f.Kind=="dog_bed"?"V5_DogBed":f.Kind=="cat_bed"?"V5_CatBed":null;
    if(id!=null){var pos=VisualFactory.V(f.Position); if(f.Kind=="lamp_floor")pos=new Vector3(-2.9f,0,.65f); var go=Put(id,pos,f.Yaw);FurnitureCollision(go,f);}else{
     var go=VisualFactory.BuildFurniture(f,living,Palette.WallLiving);foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(ToPainted).ToArray();
    }
   }
   Put("V5_Cushion",new Vector3(-.65f,.47f,-.3f),-12);Put("V5_Cushion",new Vector3(.61f,.47f,-.3f),18);
   Put("V5_PottedPlant",new Vector3(-3.1f,0,-2.65f),0);Put("V5_PottedPlant",new Vector3(2.85f,0,2.65f),-45);
   var shelfPlant=Put("V5_PottedPlant",new Vector3(3.28f,2.95f,-.34f),0);shelfPlant.transform.localScale=Vector3.one*.58f;
   for(int i=0;i<3;i++){var keepsakeBook=Put("V5_Book",new Vector3(3.32f,2.95f,-.93f+i*.09f),0);keepsakeBook.transform.localScale=Vector3.one*1.35f;}
   Put("V5_FamilyFrame",new Vector3(-2.2f,.61f,2.46f),180);
   Put("V5_CurtainPair",new Vector3(-1.75f,1.03f,-2.97f),0);Put("V5_CurtainPair",new Vector3(1.75f,1.03f,-2.97f),0);
   // Static household detail is placed clear of task slots and source tables.
   Put("V5_Mug",new Vector3(-.36f,.432f,.86f),-15);Put("V5_SnackCluster",new Vector3(.18f,.433f,.95f),0);Put("V5_Remote",new Vector3(-.12f,.433f,.7f),-15);Put("V5_ToyCar",new Vector3(.17f,.20f,.92f),-18);
   for(int i=0;i<3;i++){var book=Put("V5_Book",new Vector3(-.25f+i*.07f,.20f,1.02f),90);book.transform.localScale=Vector3.one*.75f;}
   // Mounted fixtures, with real stems to the ceiling instead of a floating floor-lamp-shaped pendant.
   var oldPendant=living.Find("a2_028_pendant_light");if(oldPendant)UnityEngine.Object.DestroyImmediate(oldPendant.gameObject);
   var lamp=house.transform.Find("LivingLamp");if(lamp){lamp.localPosition=new Vector3(-2.9f,1.4f,.65f);var light=lamp.GetComponent<Light>();light.color=new Color(1,.82f,.64f);light.intensity=1.8f;light.range=7;}
   var landingLamp=house.transform.Find("LandingLamp");if(landingLamp){landingLamp.localPosition=new Vector3(1.7f,5.05f,3.9f);landingLamp.GetComponent<Light>().intensity=1.4f;}
   // The shade and bulb share one optimized renderer. They must transmit the practical light.
   foreach(var r in living.Find("V5_FloorLamp").GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var fill=new GameObject("MoonWindowFill").AddComponent<Light>();fill.transform.SetParent(house.transform,false);fill.transform.localPosition=new Vector3(0,2.5f,-2.6f);fill.type=LightType.Rectangle;fill.areaSize=new Vector2(3.8f,2.5f);fill.color=Palette.Hex(0x719CE6);fill.intensity=4;fill.range=7;fill.lightmapBakeType=LightmapBakeType.Baked;fill.transform.rotation=Quaternion.Euler(0,0,0);
   var probeGo=new GameObject("LivingRoomLightProbes");probeGo.transform.SetParent(house.transform,false);var probes=probeGo.AddComponent<LightProbeGroup>();var points=new System.Collections.Generic.List<Vector3>();for(int x=-3;x<=3;x+=2)for(int z=-2;z<=2;z+=2)foreach(float y in new[]{.25f,1f,1.8f,2.4f})points.Add(new Vector3(x,y,z));probes.probePositions=points.ToArray();
   foreach(var r in living.GetComponentsInChildren<MeshRenderer>()){GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);r.receiveGI=ReceiveGI.Lightmaps;}
   // Replaced geometry invalidates earlier lightmap bindings. Never reuse them as evidence of a new bake.
   house.Lightmaps=Array.Empty<Texture2D>();house.BakedRenderers=Array.Empty<Renderer>();house.BakedIndices=Array.Empty<int>();house.BakedScaleOffsets=Array.Empty<Vector4>();LightmapSettings.lightmaps=Array.Empty<LightmapData>();
   Directory.CreateDirectory(A+"/Scenes");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),A+"/Scenes/ConnectedHouse_V5_Living.unity");PrefabUtility.SaveAsPrefabAsset(house.gameObject,"Assets/SHHH/Resources/SHHH_WorldHouse.prefab");AssetDatabase.SaveAssets();
   var cams=new[]{new{name="LivingRoom",position=new Vector3(-2.5f,1.6f,-2.8f),target=new Vector3(.7f,1.2f,1.1f)},new{name="Shelf",position=new Vector3(-1.8f,1.45f,-1.1f),target=new Vector3(-3.25f,1.1f,-.3f)},new{name="CouchTable",position=new Vector3(1.75f,1.3f,1.7f),target=new Vector3(0,.55f,-.1f)},new{name="StairsArch",position=new Vector3(.8f,1.6f,-1.8f),target=new Vector3(-.6f,2,3.1f)}};
   var records=cams.Select(c=>new CameraRecord{name=c.name,position=c.position,target=c.target,fov=72,nearClip=.03f}).ToList();
   records.Add(new CameraRecord{name="TopDownGround",position=new Vector3(3,15,2.8f),target=new Vector3(3,0,2.8f),fov=72,orthographic=true,orthoSize=7.5f,nearClip=12.5f});
   records.Add(new CameraRecord{name="TopDownUpper",position=new Vector3(0,13,5),target=new Vector3(0,3,5),fov=72,orthographic=true,orthoSize=5,nearClip=8.1f});
   File.WriteAllText(HandcraftedAssetImport.Output+"/00_Baseline/CAMERA_MATCH.json",JsonUtility.ToJson(new CameraRecords{records=records.ToArray()},true));
   return "V5 authored living art saved into connected house prefab and production scene; original architecture colliders retained. NEW LIGHTING BAKE REQUIRED. Other rooms remain V4 structural drafts.";
  }
  [Serializable]class CameraRecords{public CameraRecord[] records;}[Serializable]class CameraRecord{public string name;public Vector3 position,target;public float fov,orthoSize,nearClip;public bool orthographic;}
  static Material ToPainted(Material m){return m;}
  static void RemoveDuplicateSharedWall(Transform kitchen){
   // The V4 rooms each supplied an arch at the same boundary. The new shared wall
   // owns its complete reveal. Preserve the original mesh assets and all colliders.
   Directory.CreateDirectory(A+"/Meshes");int index=0;
   foreach(var mf in kitchen.GetComponentsInChildren<MeshFilter>()){
    var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!mf.sharedMesh)continue;
    var source=mf.sharedMesh;var vertices=source.vertices;bool changed=false;var subsets=new int[source.subMeshCount][];
    for(int s=0;s<source.subMeshCount;s++){
     var triangles=source.GetTriangles(s);var kept=new System.Collections.Generic.List<int>();
     for(int t=0;t<triangles.Length;t+=3){bool shared=true;
      for(int k=0;k<3;k++){var p=mf.transform.TransformPoint(vertices[triangles[t+k]]);shared&=p.x>3.4f&&p.x<3.8f&&p.y>.08f;}
      if(shared)changed=true;else{kept.Add(triangles[t]);kept.Add(triangles[t+1]);kept.Add(triangles[t+2]);}
     }subsets[s]=kept.ToArray();
    }
    if(!changed)continue;var mesh=UnityEngine.Object.Instantiate(source);mesh.name="SharedWallCleared_"+index;
    for(int s=0;s<subsets.Length;s++)mesh.SetTriangles(subsets[s],s);mesh.RecalculateBounds();
    string path=A+"/Meshes/SharedWallCleared_"+(index++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
    mf.sharedMesh=mesh;
   }
  }
  static GameObject Put(string id,Vector3 pos,float yaw){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(A+"/Prefabs/"+id+".prefab");if(!asset)throw new Exception("Authored prefab missing "+id);var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,living);go.name=id;go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,yaw,0);return go;}
  static void Collider(Transform p,Vector3 center,Vector3 size){var go=new GameObject("Collision");go.transform.SetParent(p,false);go.layer=Layers.Furniture;var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
  static void FurnitureCollision(GameObject go,FurnitureSpec f){
   var t=go.transform;if(f.Kind=="rug")return;
   if(f.Kind=="bookshelf"){foreach(float y in new[]{.02f,.60f,1.02f,1.33f,1.8f})Collider(t,new Vector3(0,y-.017f,0),new Vector3(1.06f,.034f,.34f));foreach(float x in new[]{-.555f,.555f})Collider(t,new Vector3(x,.98f,0),new Vector3(.11f,1.84f,.35f));Collider(t,new Vector3(0,.98f,-.14f),new Vector3(1.12f,1.75f,.035f));return;}
   if(f.Kind=="coffee_table"){Collider(t,new Vector3(0,.395f,0),new Vector3(1.14f,.07f,.64f));Collider(t,new Vector3(0,.18f,0),new Vector3(.87f,.035f,.48f));foreach(float x in new[]{-.39f,.39f})foreach(float z in new[]{-.22f,.22f})Collider(t,new Vector3(x,.18f,z),new Vector3(.1f,.36f,.1f));return;}
   if(f.Kind=="sofa"){Collider(t,new Vector3(0,.27f,0),new Vector3(2.08f,.5f,.87f));foreach(float x in new[]{-.94f,.94f})Collider(t,new Vector3(x,.55f,0),new Vector3(.32f,.5f,.95f));Collider(t,new Vector3(0,.73f,-.32f),new Vector3(1.98f,.66f,.26f));return;}
   var b=Prim.RendererBounds(go);Collider(t,t.InverseTransformPoint(b.center),new Vector3(f.Size.x,f.Size.y,f.Size.z));
  }
  [ShhhCommand("v5_bake_living")]
  public static string Bake(){
   if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
   EditorSceneManager.OpenScene(A+"/Scenes/ConnectedHouse_V5_Living.unity");
   var s=AssetDatabase.LoadAssetAtPath<LightingSettings>(A+"/V5Lighting.asset");
   if(!s){s=new LightingSettings();AssetDatabase.CreateAsset(s,A+"/V5Lighting.asset");}
   s.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;s.bakedGI=true;s.realtimeGI=false;
   s.directionalityMode=LightmapsMode.NonDirectional;s.lightmapResolution=24;s.lightmapMaxSize=2048;s.lightmapPadding=4;
   s.indirectSampleCount=128;s.directSampleCount=64;s.environmentSampleCount=64;s.maxBounces=3;
   s.ao=true;s.aoMaxDistance=.3f;s.aoExponentIndirect=1;s.aoExponentDirect=.15f;
   LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;Lightmapping.lightingSettings=s;
   Lightmapping.BakeAsync();return "V5 non-directional lighting/contact-shadow bake started.";
  }
  [ShhhCommand("v5_finish_living")]
  public static string Finish(){
   if(Lightmapping.isRunning)return "BAKE_RUNNING";
   var h=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();
   if(!h||LightmapSettings.lightmaps.Length==0)throw new Exception("No finished house bake");
   h.Lightmaps=LightmapSettings.lightmaps.Select(m=>m.lightmapColor).ToArray();
   h.BakedRenderers=h.GetComponentsInChildren<Renderer>().Where(r=>r.lightmapIndex>=0&&r.lightmapIndex<h.Lightmaps.Length).ToArray();
   h.BakedIndices=h.BakedRenderers.Select(r=>r.lightmapIndex).ToArray();h.BakedScaleOffsets=h.BakedRenderers.Select(r=>r.lightmapScaleOffset).ToArray();
   if(LightmapSettings.lightProbes&&LightmapSettings.lightProbes.count>0){
    string path=A+"/HouseBakedProbes.asset";var probes=AssetDatabase.LoadAssetAtPath<LightProbes>(path);
    if(probes)EditorUtility.CopySerialized(LightmapSettings.lightProbes,probes);
    else{probes=UnityEngine.Object.Instantiate(LightmapSettings.lightProbes);AssetDatabase.CreateAsset(probes,path);}
    probes.name="HouseBakedProbes";EditorUtility.SetDirty(probes);h.BakedProbes=probes;
   }
   PrefabUtility.SaveAsPrefabAsset(h.gameObject,"Assets/SHHH/Resources/SHHH_WorldHouse.prefab");
   EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
   return "Saved V5 baked bindings: "+h.Lightmaps.Length+" lightmaps / "+(h.BakedProbes?h.BakedProbes.count:0)+" runtime probe samples; visual approval remains separate.";
  }
  [ShhhCommand("v5_living_review")]
  public static string Review(){var h=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!h)throw new Exception("House missing");h.BindLighting();var cam=Camera.main;var records=JsonUtility.FromJson<CameraRecords>(File.ReadAllText(HandcraftedAssetImport.Output+"/00_Baseline/CAMERA_MATCH.json"));foreach(var c in records.records){cam.transform.position=c.position;cam.transform.LookAt(c.target);cam.fieldOfView=c.fov;HandVisualReview.Render(cam,HandcraftedAssetImport.Output+"/Evidence/Unity/"+c.name+".png",1600,900);}return "Rendered matched living entry + shelf + couch/table + stairs/arch views from actual connected Unity scene.";}
 }
}


