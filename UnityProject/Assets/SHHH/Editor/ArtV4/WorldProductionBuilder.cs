using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SHHH.Automation;
using SHHH.Core;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace SHHH.EditorTools {
 /// <summary>Editable canonical architecture authoring. All saved geometry is real mesh data; no runtime generator required.</summary>
 public static class WorldProductionBuilder {
  const string A="Assets/SHHH/ArtProductionV4/World";
  static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
  static int meshId;
  static Material wall,wood,trim,gold,plum,cream,navy,glass,coral;
  static Transform root;
  [ShhhCommand("v4_build_house")]
  public static string Build(){
   if(EditorApplication.isPlaying)throw new Exception("Stop play mode before authoring house");
   Directory.CreateDirectory(A+"/Meshes");Directory.CreateDirectory(A+"/Materials");Directory.CreateDirectory(A+"/Scenes");AssetDatabase.Refresh();meshId=0;materials.Clear();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   root=new GameObject("SHHH_WorldHouse").transform;var house=root.gameObject.AddComponent<HouseAssembly>();
   wall=Mat("LavenderPlaster",0x9A789E);wood=Mat("HoneyOak",0xA66C43);trim=Mat("WarmIvory",0xE8CCAE);gold=Mat("AgedBrass",0xCF9B36);plum=Mat("DeepPlum",0x59366F);cream=Mat("Cream",0xF1D9AF);navy=Mat("NightBlue",0x0D194A);glass=Mat("MoonGlow",0x8DBDF8,true);coral=Mat("CurtainCoral",0xA44068);
   var living=Group("LivingRoom",Vector3.zero);Floor(living,7.2f,6.2f,false);Ceiling(living,7.2f,6.2f,5.7f);
   Wall(living,"West",new Vector3(-3.65f,0,0),6.2f,5.7f,90);Wall(living,"South",new Vector3(0,0,-3.15f),7.4f,5.7f,0);
   PortalWall(living,"KitchenArch",new Vector3(3.6f,0,0),6.2f,2.7f,90,2.15f,1.6f,true);
   // Living north: a stair entry at the west and the hall opening at the east.
   Wall(living,"NorthCenter",new Vector3(-.6f,0,3.1f),3.4f,2.7f,0);Wall(living,"NorthEast",new Vector3(3.05f,0,3.1f),1.1f,2.7f,0);
   Arch(living,new Vector3(1.8f,0,3.1f),1.4f,0);Beam(living,new Vector3(0,5.5f,0),new Vector3(7.2f,.24f,.22f));Beam(living,new Vector3(0,5.5f,2.6f),new Vector3(7.2f,.24f,.22f));
   Window(living,new Vector3(-1.75f,1.1f,-3.03f),1.65f,2.6f,0,true);Window(living,new Vector3(1.75f,1.1f,-3.03f),1.65f,2.6f,0,true);
   // Tall exterior wall above the kitchen; no false glass in shared walls.
   Box(living,"UpperEastWall",new Vector3(3.65f,4.2f,0),new Vector3(.15f,3,6.2f),wall,.025f,true);
   var kitchen=Group("Kitchen",HouseAssembly.Center(RoomId.Kitchen));Floor(kitchen,6.6f,6,true);Ceiling(kitchen,6.6f,6,2.7f);
   Wall(kitchen,"South",new Vector3(0,0,-3.05f),6.6f,2.7f,0);Wall(kitchen,"North",new Vector3(0,0,3.05f),6.6f,2.7f,0);Wall(kitchen,"East",new Vector3(3.35f,0,0),6,2.7f,90);
   PortalWall(kitchen,"LivingArch",new Vector3(-3.3f,0,0),6,2.7f,90,2.15f,1.6f,true);
   Window(kitchen,new Vector3(3.23f,1.05f,.1f),1.7f,1.3f,-90,false);CabinetDress(kitchen);
   var hall=Group("Hall",Vector3.zero);Box(hall,"HallFloor",new Vector3(0,-.065f,4.8f),new Vector3(7.2f,.13f,3.4f),wood,.018f,true);
   Wall(hall,"West",new Vector3(-3.65f,0,4.85f),3.5f,5.7f,90);Wall(hall,"North",new Vector3(0,0,6.65f),7.3f,3,0);
   PortalWall(hall,"LaundryDoor",new Vector3(3.6f,0,4.9f),3.6f,2.7f,90,4.15f-4.9f,1.2f,false);
   Box(hall,"HallCeilingEast",new Vector3(1.3f,2.93f,4.0f),new Vector3(4.6f,.14f,1.8f),plum,.02f,true);
   var laundry=Group("Laundry",HouseAssembly.Center(RoomId.Laundry));Floor(laundry,5.6f,5.4f,true);Ceiling(laundry,5.6f,5.4f,2.7f);
   Wall(laundry,"South",new Vector3(0,0,-2.75f),5.6f,2.7f,0);Wall(laundry,"North",new Vector3(0,0,2.75f),5.6f,2.7f,0);Wall(laundry,"East",new Vector3(2.85f,0,0),5.4f,2.7f,90);
   PortalWall(laundry,"HallDoor",new Vector3(-2.8f,0,0),5.4f,2.7f,90,-1.85f,1.2f,false);Window(laundry,new Vector3(.6f,1.23f,2.64f),1.5f,1.1f,180,false);
   Stairs(hall);
   var upper=Group("UpperLanding",Vector3.zero);
   Box(upper,"BalconyFloor",new Vector3(.65f,2.93f,2.5f),new Vector3(5.9f,.14f,1.2f),wood,.02f,true);
   Box(upper,"LandingFloor",new Vector3(1.325f,2.93f,3.95f),new Vector3(4.55f,.14f,1.7f),wood,.02f,true);
   Rail(upper,new Vector3(.65f,3,1.9f),5.9f,0);Rail(upper,new Vector3(-2.3f,3,2.5f),1.2f,90);Rail(upper,new Vector3(-.95f,3,3.95f),1.7f,90);
   var bedroom=Group("MomBedroom",new Vector3(1.3f,3,6.9f));Floor(bedroom,4.6f,4.2f,false);Ceiling(bedroom,4.6f,4.2f,2.7f);
   PortalWall(bedroom,"BedroomSouth",new Vector3(0,0,-2.1f),4.6f,2.7f,0,0,1.1f,false);Wall(bedroom,"North",new Vector3(0,0,2.15f),4.6f,2.7f,0);Wall(bedroom,"West",new Vector3(-2.35f,0,0),4.2f,2.7f,90);Wall(bedroom,"East",new Vector3(2.35f,0,0),4.2f,2.7f,90);
   Window(bedroom,new Vector3(0,1.05f,2.04f),1.6f,1.3f,180,false);Bedroom(bedroom);
   var hinge=new GameObject("MomBedroomDoorHinge").transform;hinge.SetParent(bedroom,false);hinge.localPosition=new Vector3(-.55f,0,-2.08f);var door=Box(hinge,"DoorPanel",new Vector3(.54f,1.07f,0),new Vector3(1.08f,2.13f,.055f),plum,.045f,true);door.layer=Layers.UI;door.isStatic=false;
   for(int p=0;p<2;p++)Box(hinge,"DoorInset",new Vector3(.54f,.6f+p*.92f,-.037f),new Vector3(.8f,.68f,.028f),wall,.045f,false).isStatic=false;
   Box(hinge,"BrassHandle",new Vector3(.95f,1,-.085f),new Vector3(.08f,.1f,.1f),gold,.035f,false).isStatic=false;
   house.BedroomDoor=hinge;house.Bedside=Marker(bedroom,"MomBedside",new Vector3(-1.35f,0,.2f));house.BedroomThreshold=Marker(bedroom,"MomThreshold",new Vector3(0,0,-1.7f));
   // Same current furniture/anchor layout in every round. Static decorations never register game authority.
   foreach(var id in new[]{RoomId.LivingRoom,RoomId.Kitchen,RoomId.Laundry}){
    var def=RoomCatalog.Get(id);var parent=id==RoomId.LivingRoom?living:id==RoomId.Kitchen?kitchen:laundry;
    foreach(var f in def.Furniture)VisualFactory.BuildFurniture(f,parent,Palette.WallLiving);
    Dress(parent,def);foreach(var r in parent.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(ToHouseMaterial).ToArray();
   }
   BakedLight(root,"LivingLamp",new Vector3(-2.9f,1.65f,2.6f),0xFFC078,3.5f,7);
   BakedLight(root,"KitchenPendant",new Vector3(7.5f,2.35f,-.2f),0xFFD09B,4,6);
   BakedLight(root,"LaundryPendant",new Vector3(6.4f,2.3f,6),0xFFCCA0,3.3f,5);
   BakedLight(root,"HallLamp",new Vector3(1.8f,2.2f,4.1f),0xE6A6FF,2,5);
   BakedLight(root,"LandingLamp",new Vector3(1.7f,5.3f,3.9f),0xFFCA90,2.5f,5);
   house.BedroomLight=BakedLight(root,"BedroomLamp",new Vector3(.05f,4.3f,7.4f),0xFFD09B,2,4);house.BedroomLight.lightmapBakeType=LightmapBakeType.Mixed;house.BedroomLight.shadows=LightShadows.None;
   var moon=new GameObject("Moonlight").AddComponent<Light>();moon.transform.SetParent(root,false);moon.type=LightType.Directional;moon.color=Palette.Hex(0xACBBFF);moon.intensity=.55f;moon.transform.rotation=Quaternion.Euler(48,-28,0);moon.lightmapBakeType=LightmapBakeType.Mixed;moon.shadows=LightShadows.None;
   CombineStaticMeshes(hinge);
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Palette.Hex(0x716C9D)*.7f;RenderSettings.ambientEquatorColor=Palette.Hex(0x695C83)*.65f;RenderSettings.ambientGroundColor=Palette.Hex(0x332839);RenderSettings.fog=false;
   var cam=new GameObject("ProductionReviewCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(-2.2f,1.6f,-2.8f);cam.transform.LookAt(new Vector3(.5f,1.5f,1.6f));cam.nearClipPlane=.05f;cam.farClipPlane=40;cam.fieldOfView=75;cam.backgroundColor=Palette.Hex(0x15152F);cam.clearFlags=CameraClearFlags.SolidColor;
   Save(house);EditorSceneManager.SaveScene(scene,A+"/Scenes/ConnectedHouse_Production.unity");AssetDatabase.SaveAssets();
   int tris=root.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh?m.sharedMesh.triangles.Length/3:0);int renderers=root.GetComponentsInChildren<Renderer>().Length;
   string report=$"Saved connected house mesh/prefab/scene. {tris} triangles, {renderers} renderers. Lighting not yet baked; hardware NOT VERIFIED.";File.WriteAllText(Path.Combine(WorldProductionAudit.Output,"07_Performance_And_QA/HOUSE_BUILD.txt"),report);return report;
  }
  static Material ToHouseMaterial(Material src){if(!src)return cream;if(src.shader.name.Contains("Unlit")||src.shader.name.Contains("TextMesh"))return src;string key="Reuse_"+src.name;if(materials.TryGetValue(key,out var found))return found;var m=Mat(key,0xFFFFFF);if(src.HasProperty("_BaseColor"))m.SetColor("_BaseColor",src.GetColor("_BaseColor"));if(src.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",src.GetTexture("_BaseMap"));return m;}
  static Material Mat(string name,int color,bool emissive=false){if(materials.TryGetValue(name,out var v))return v;string path=A+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",Palette.Hex(color));m.SetFloat("_Smoothness",.22f);m.enableInstancing=true;if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Palette.Hex(color)*.7f);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}materials[name]=m;return m;}
  static Transform Group(string name,Vector3 p){var g=new GameObject(name).transform;g.SetParent(root,false);g.localPosition=p;return g;}
  static Transform Marker(Transform p,string name,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=pos;return t;}
  static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material m,float radius=.02f,bool solid=false){
   var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;g.AddComponent<MeshFilter>().sharedMesh=Rounded(size,Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f));var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=m;renderer.receiveShadows=true;g.isStatic=true;if(solid)g.AddComponent<BoxCollider>().size=size;return g;
  }
  static Mesh Rounded(Vector3 size,float r){var verts=new List<Vector3>();var norms=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();var half=size*.5f;
   for(int face=0;face<6;face++){int axis=face/2;float sign=face%2==0?1:-1;int a=(axis+1)%3,b=(axis+2)%3;var samplesA=new[]{-half[a],-half[a]+r,0,half[a]-r,half[a]};var samplesB=new[]{-half[b],-half[b]+r,0,half[b]-r,half[b]};int start=verts.Count;
    for(int j=0;j<5;j++)for(int i=0;i<5;i++){var v=Vector3.zero;v[axis]=half[axis]*sign;v[a]=samplesA[i];v[b]=samplesB[j];var q=new Vector3(Mathf.Clamp(v.x,-half.x+r,half.x-r),Mathf.Clamp(v.y,-half.y+r,half.y-r),Mathf.Clamp(v.z,-half.z+r,half.z-r));var n=(v-q).normalized;verts.Add(q+n*r);norms.Add(n);uvs.Add(new Vector2(i*.25f,j*.25f));}
    for(int j=0;j<4;j++)for(int i=0;i<4;i++){int k=start+j*5+i;int[] t=sign>0?new[]{k,k+1,k+6,k,k+6,k+5}:new[]{k,k+6,k+1,k,k+5,k+6};tris.AddRange(t);}}
   var mesh=new Mesh();mesh.name="Rounded_"+meshId++;mesh.SetVertices(verts);mesh.SetNormals(norms);mesh.SetUVs(0,uvs);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
  }
  static void Wall(Transform p,string n,Vector3 c,float width,float h,float yaw){var t=Marker(p,n,c);t.localRotation=Quaternion.Euler(0,yaw,0);Box(t,"Plaster",new Vector3(0,h/2,0),new Vector3(width,h,.14f),wall,.025f,true);Box(t,"Skirting",new Vector3(0,.08f,-.085f),new Vector3(width,.16f,.05f),trim,.014f);Box(t,"Crown",new Vector3(0,h-.08f,0),new Vector3(width,.16f,.22f),trim,.03f);Box(t,"Dado",new Vector3(0,.9f,-.09f),new Vector3(width,.075f,.05f),wood,.012f);for(float x=-width/2+.15f;x<width/2;x+=.55f)Box(t,"PanelBatten",new Vector3(x,.46f,-.084f),new Vector3(.035f,.8f,.04f),wood,.009f);}
  // yaw 90 maps local +X to world -Z, so opening offset is negated for side walls.
  static void PortalWall(Transform p,string n,Vector3 c,float width,float h,float yaw,float opening,float gap,bool arch){var t=Marker(p,n,c);t.localRotation=Quaternion.Euler(0,yaw,0);float offset=yaw==90?-opening:opening;float left=offset-gap/2+width/2,right=width/2-offset-gap/2;if(left>0)Wall(t,"Left",new Vector3(-width/2+left/2,0,0),left,h,0);if(right>0)Wall(t,"Right",new Vector3(offset+gap/2+right/2,0,0),right,h,0);Box(t,"Header",new Vector3(offset,(2.3f+h)/2,0),new Vector3(gap,h-2.3f,.14f),wall,.015f,true);if(arch)Arch(t,new Vector3(offset,0,0),gap,0);else{for(int s=-1;s<=1;s+=2)Box(t,"Jamb",new Vector3(offset+s*(gap/2+.04f),1.15f,0),new Vector3(.08f,2.3f,.23f),trim,.02f);Box(t,"Lintel",new Vector3(offset,2.3f,0),new Vector3(gap+.16f,.12f,.23f),trim,.02f);}}
  static void Arch(Transform p,Vector3 pos,float width,float yaw){var t=Marker(p,"SoftArch",pos);t.localRotation=Quaternion.Euler(0,yaw,0);float spring=1.72f;for(int s=-1;s<=1;s+=2)Box(t,"Pillar",new Vector3(s*(width/2+.05f),spring/2,0),new Vector3(.1f,spring,.25f),trim,.02f);int count=12;float radius=width/2+.05f;for(int i=0;i<count;i++){float a=Mathf.PI*(i+.5f)/count;var segment=Box(t,"ArchVoussoir",new Vector3(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*.53f,0),new Vector3(radius*Mathf.PI/count+.035f,.13f,.25f),trim,.025f);segment.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(.53f*Mathf.Cos(a),-radius*Mathf.Sin(a))*Mathf.Rad2Deg);}}
  static void Floor(Transform p,float w,float d,bool tile){Box(p,"Subfloor",new Vector3(0,-.075f,0),new Vector3(w,.14f,d),wood,.01f,true);float sx=tile?.5f:1.2f,sz=tile?.5f:.3f;for(float z=-d/2;z<d/2-.01f;z+=sz)for(float x=-w/2;x<w/2-.01f;x+=sx){float wx=Mathf.Min(sx,w/2-x),dz=Mathf.Min(sz,d/2-z);int k=(Mathf.RoundToInt(x*17+z*11)%4+4)%4;var mat=Mat((tile?"Terracotta":"Oak")+k,tile?new[]{0xB9795D,0xC18A6A,0xAC735D,0xBC8468}[k]:new[]{0xA66D48,0xAD764E,0x996447,0xB47B52}[k]);Box(p,tile?"Tile":"Plank",new Vector3(x+wx/2,-.006f,z+dz/2),new Vector3(wx-.008f,.025f,dz-.006f),mat,.006f);}}
  static void Ceiling(Transform p,float w,float d,float h){Box(p,"Ceiling",new Vector3(0,h+.06f,0),new Vector3(w,.12f,d),plum,.01f,true);}
  static void Beam(Transform p,Vector3 pos,Vector3 size)=>Box(p,"CeilingBeam",pos,size,wood,.05f);
  static void Window(Transform p,Vector3 pos,float w,float h,float yaw,bool tall){var t=Marker(p,"NightWindow",pos);t.localRotation=Quaternion.Euler(0,yaw,0);Box(t,"OpaqueNightView",new Vector3(0,h/2,0),new Vector3(w,h,.02f),navy,.002f);for(int i=-1;i<=1;i++)Box(t,"WindowMullion",new Vector3(i*w/2,h/2,-.055f),new Vector3(.07f,h+.12f,.1f),wood,.017f);for(int i=0;i<=2;i++)Box(t,"WindowRail",new Vector3(0,i*h/2,-.055f),new Vector3(w+.12f,.07f,.1f),wood,.017f);Box(t,"Sill",new Vector3(0,-.04f,-.1f),new Vector3(w+.22f,.1f,.25f),trim,.025f);
   // An opaque low-poly moon avoids large transparent overdraw panes on standalone hardware.
   var moon=GameObject.CreatePrimitive(PrimitiveType.Sphere);moon.name="Moon";moon.transform.SetParent(t,false);moon.transform.localPosition=new Vector3(w*.24f,h*.72f,-.025f);moon.transform.localScale=new Vector3(.22f,.22f,.015f);UnityEngine.Object.DestroyImmediate(moon.GetComponent<Collider>());moon.GetComponent<Renderer>().sharedMaterial=glass;moon.isStatic=true;
   for(int side=-1;side<=1;side+=2)for(int fold=0;fold<5;fold++){float x=side*(w*.52f+fold*.045f);var curtain=Box(t,"CurtainFold",new Vector3(x,h*.48f,-.15f),new Vector3(.1f,h*1.05f,.12f),tall?plum:coral,.045f);curtain.transform.localRotation=Quaternion.Euler(0,0,side*(fold-2)*1.4f);}for(int side=-1;side<=1;side+=2)Box(t,"Tieback",new Vector3(side*w*.61f,h*.35f,-.24f),new Vector3(.25f,.1f,.06f),gold,.02f);
  }
  static void Rail(Transform p,Vector3 pos,float length,float yaw){var t=Marker(p,"SafetyBalustrade",pos);t.localRotation=Quaternion.Euler(0,yaw,0);Box(t,"TopRail",new Vector3(0,1.02f,0),new Vector3(length,.09f,.12f),wood,.035f);for(float x=-length/2;x<=length/2+.01f;x+=.25f)Box(t,"Baluster",new Vector3(x,.5f,0),new Vector3(.055f,.95f,.055f),trim,.018f);Box(t,"InvisibleRailCollider",new Vector3(0,.52f,0),new Vector3(length,1.04f,.08f),wood,0,true).GetComponent<MeshRenderer>().enabled=false;}
  static void Stairs(Transform p){for(int i=0;i<9;i++){float height=(i+1)/6f;Box(p,"StairTreadA",new Vector3(-2.95f,height-.035f,3.1f+(i+.5f)*.3f),new Vector3(1.2f,.07f,.31f),wood,.018f);Box(p,"StairRiserA",new Vector3(-2.95f,height-.085f,3.1f+i*.3f),new Vector3(1.2f,.17f,.035f),trim,.01f);float h=1.5f+(i+1)/6f;Box(p,"StairTreadB",new Vector3(-1.65f,h-.035f,5.8f-(i+.5f)*.3f),new Vector3(1.2f,.07f,.31f),wood,.018f);Box(p,"StairRiserB",new Vector3(-1.65f,h-.085f,5.8f-i*.3f),new Vector3(1.2f,.17f,.035f),trim,.01f);}
   Box(p,"HalfLanding",new Vector3(-2.3f,1.44f,6.15f),new Vector3(2.5f,.12f,.7f),wood,.018f,true);
   RampCollider(p,"FirstFlightWalkRamp",new Vector3(-2.95f,0,3.1f),1.2f,2.7f,1.5f,false);RampCollider(p,"ReturnFlightWalkRamp",new Vector3(-1.65f,1.5f,5.8f),1.2f,2.7f,1.5f,true);
   for(int flight=0;flight<2;flight++)for(int side=-1;side<=1;side+=2){float x=(flight==0?-2.95f:-1.65f)+side*.6f;for(int i=0;i<=9;i++){float z=flight==0?3.1f+i*.3f:5.8f-i*.3f;float y=(flight==0?0:1.5f)+i/6f;Box(p,"StairPost",new Vector3(x,y+.47f,z),new Vector3(.055f,.94f,.055f),trim,.015f);}var rail=Box(p,"SlopingHandrail",new Vector3(x,(flight==0?.75f:2.25f)+.98f,4.45f),new Vector3(.08f,.08f,3.09f),wood,.025f);rail.transform.localRotation=Quaternion.Euler(flight==0?-29.05f:29.05f,0,0);}
  }
  static void RampCollider(Transform p,string n,Vector3 start,float w,float run,float rise,bool reverse){var t=Marker(p,n,start);if(reverse)t.localRotation=Quaternion.Euler(0,180,0);var m=new Mesh{name=n};m.vertices=new[]{new Vector3(-w/2,0,0),new Vector3(w/2,0,0),new Vector3(-w/2,rise,run),new Vector3(w/2,rise,run),new Vector3(-w/2,-.1f,run),new Vector3(w/2,-.1f,run)};m.triangles=new[]{0,2,1,1,2,3,0,4,2,1,3,5,2,4,3,3,4,5,0,1,4,1,5,4};m.RecalculateNormals();AssetDatabase.CreateAsset(m,A+"/Meshes/"+n+".asset");t.gameObject.AddComponent<MeshCollider>().sharedMesh=m;t.gameObject.isStatic=true;}
  static void Bedroom(Transform p){Box(p,"BedFrame",new Vector3(.4f,.24f,.2f),new Vector3(1.7f,.35f,2.2f),wood,.07f,true);Box(p,"Mattress",new Vector3(.4f,.49f,.2f),new Vector3(1.62f,.22f,2.1f),cream,.09f);Box(p,"PurpleQuilt",new Vector3(.4f,.61f,-.12f),new Vector3(1.68f,.12f,1.45f),plum,.055f);Box(p,"Pillow",new Vector3(.4f,.65f,.98f),new Vector3(1.15f,.18f,.4f),cream,.085f);Box(p,"Headboard",new Vector3(.4f,.65f,1.32f),new Vector3(1.85f,1.2f,.13f),wood,.06f,true);Box(p,"BedsideTable",new Vector3(-.95f,.32f,1.1f),new Vector3(.5f,.64f,.5f),wood,.04f,true);Put(p,"a2_029_floor_lamp_LOD1",new Vector3(-1.6f,0,1.4f),1.3f,0);Put(p,"a2_040_wall_art_frame",new Vector3(2.18f,1.5f,.4f),.55f,-90);}
  static void CabinetDress(Transform p){for(int i=0;i<3;i++){float x=-.15f+i*.8f;Box(p,"UpperCabinet",new Vector3(x,1.92f,2.78f),new Vector3(.76f,.7f,.38f),plum,.04f,true);Box(p,"CabinetPanel",new Vector3(x,1.92f,2.57f),new Vector3(.65f,.59f,.045f),wall,.025f);Box(p,"CabinetHandle",new Vector3(x+.22f,1.77f,2.52f),new Vector3(.035f,.13f,.045f),gold,.014f);}Box(p,"WorktopGlow",new Vector3(.65f,1.55f,2.58f),new Vector3(2.35f,.025f,.04f),Mat("WarmStrip",0xFFB85E,true),.005f);}
  static void Dress(Transform p,RoomDefinition d){Put(p,"a2_028_pendant_light",new Vector3(0,2.1f,-.3f),.55f,0);Put(p,"a2_040_wall_art_frame",new Vector3(-d.Width/2+.1f,1.6f,1.4f),.6f,90);Put(p,"a2_041_wall_clock",new Vector3(-d.Width/2+.1f,2.1f,-.8f),.35f,90);Put(p,"a2_039_large_potted_plant",new Vector3(-d.Width/2+.4f,0,-d.Depth/2+.4f),.9f,0);Put(p,"a2_038_small_leafy_plant",new Vector3(d.Width/2-.4f,1.9f,d.Depth/2-.4f),.28f,0);}
  static void Put(Transform p,string key,Vector3 pos,float h,float yaw){var model=ArtCatalog.I.Model(key);if(!model)return;var g=VisualFactory.Normalise(model,h,p,key,true);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,yaw,0);foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);foreach(var t in g.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;}
  static Light BakedLight(Transform p,string n,Vector3 pos,int color,float intensity,float range){var l=Marker(p,n,pos).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=Palette.Hex(color);l.intensity=intensity;l.range=range;l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;l.gameObject.isStatic=true;return l;}
  static void CombineStaticMeshes(Transform dynamicDoor){
   foreach(var zone in root.Cast<Transform>().ToArray()){
    var groups=new Dictionary<Material,List<CombineInstance>>();var old=new List<MeshFilter>();foreach(var mf in zone.GetComponentsInChildren<MeshFilter>()){
     var r=mf.GetComponent<MeshRenderer>();if(!r||!r.enabled||mf.transform.IsChildOf(dynamicDoor)||!mf.sharedMesh)continue;
     for(int s=0;s<mf.sharedMesh.subMeshCount;s++){var m=r.sharedMaterials[Mathf.Min(s,r.sharedMaterials.Length-1)];if(!m)continue;if(!groups.ContainsKey(m))groups[m]=new List<CombineInstance>();groups[m].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=s,transform=zone.worldToLocalMatrix*mf.transform.localToWorldMatrix});}old.Add(mf);
    }
    foreach(var pair in groups){var mesh=new Mesh{name=zone.name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);Unwrapping.GenerateSecondaryUVSet(mesh);string path=A+"/Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);var go=new GameObject("Batched_"+pair.Key.name);go.transform.SetParent(zone,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;renderer.receiveGI=ReceiveGI.Lightmaps;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);}
    foreach(var mf in old){UnityEngine.Object.DestroyImmediate(mf.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(mf);}
   }
   foreach(var mf in dynamicDoor.GetComponentsInChildren<MeshFilter>()){string path=A+"/Meshes/Door_"+mf.name+"_"+meshId+++".asset";AssetDatabase.CreateAsset(mf.sharedMesh,path);}
  }
  static void Save(HouseAssembly house){Directory.CreateDirectory("Assets/SHHH/Resources");PrefabUtility.SaveAsPrefabAsset(house.gameObject,"Assets/SHHH/Resources/SHHH_WorldHouse.prefab");}
  [ShhhCommand("v4_bake_house")]
  public static string Bake(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");EditorSceneManager.OpenScene(A+"/Scenes/ConnectedHouse_Production.unity");var settings=new LightingSettings();settings.name="SHHH_QuestHouse_Baked";settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapResolution=10;settings.lightmapMaxSize=1024;LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;settings.indirectSampleCount=32;settings.directSampleCount=16;settings.environmentSampleCount=32;settings.maxBounces=2;Lightmapping.lightingSettings=settings;AssetDatabase.CreateAsset(settings,A+"/SHHH_QuestHouse_Lighting.asset");Lightmapping.BakeAsync();return "House lightmap bake started; use v4_finish_bake only after complete";}
  [ShhhCommand("v4_finish_bake")]
  public static string FinishBake(){if(Lightmapping.isRunning)return "BAKE_RUNNING";var h=UnityEngine.Object.FindFirstObjectByType<HouseAssembly>();if(!h)throw new Exception("Production house scene not open");var maps=LightmapSettings.lightmaps;if(maps.Length==0)throw new Exception("No baked lightmaps; cannot claim lighting complete");h.Lightmaps=maps.Select(m=>m.lightmapColor).ToArray();h.BakedRenderers=h.GetComponentsInChildren<Renderer>().Where(r=>r.lightmapIndex>=0&&r.lightmapIndex<maps.Length).ToArray();h.BakedIndices=h.BakedRenderers.Select(r=>r.lightmapIndex).ToArray();h.BakedScaleOffsets=h.BakedRenderers.Select(r=>r.lightmapScaleOffset).ToArray();Save(h);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return "Saved "+maps.Length+" lightmaps / "+h.BakedRenderers.Length+" renderer bindings in runtime house prefab";}
 }
}
