using SHHH.Core;
using UnityEngine;
namespace SHHH.Game {
 /// <summary>One canonical house. Only the coordinate origin changes with the existing round; room/task IDs do not.</summary>
 public sealed class HouseAssembly : MonoBehaviour {
  public Transform BedroomDoor;
  public Light BedroomLight;
  public Transform Bedside, BedroomThreshold;
  public Renderer[] BakedRenderers;
  public int[] BakedIndices;
  public Vector4[] BakedScaleOffsets;
  public Texture2D[] Lightmaps;
  public LightProbes BakedProbes;
  LightProbes runtimeProbes,previousProbes;
  public static Vector3 Center(RoomId id)=>id==RoomId.Kitchen?new Vector3(6.9f,0,0):id==RoomId.Laundry?new Vector3(6.4f,0,6):Vector3.zero;
  public static HouseAssembly Create(RoomDefinition active,Transform parent){
   var source=Resources.Load<HouseAssembly>("SHHH_WorldHouse");if(!source)return null;
   var house=Instantiate(source,parent);house.name="SHHH_ConnectedFamilyHouse";house.transform.localPosition=-Center(active.Id);house.BindLighting();return house;
  }
  public Bounds WorldBounds=>new Bounds(transform.TransformPoint(new Vector3(3.2f,2.8f,2.8f)),new Vector3(15,8,15));
  public RoomShell.Result ShellResult()=>new RoomShell.Result{Door=BedroomDoor,DoorLight=BedroomLight,MomOffstage=Bedside.position,MomDoorway=BedroomThreshold.position,PlayBounds=WorldBounds};
  public void BindLighting(){
   if(Lightmaps==null||Lightmaps.Length==0)return;
   var maps=new LightmapData[Lightmaps.Length];for(int i=0;i<maps.Length;i++)maps[i]=new LightmapData{lightmapColor=Lightmaps[i]};LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;LightmapSettings.lightmaps=maps;
   for(int i=0;i<BakedRenderers.Length;i++)if(BakedRenderers[i]){BakedRenderers[i].lightmapIndex=BakedIndices[i];BakedRenderers[i].lightmapScaleOffset=BakedScaleOffsets[i];}
   if(BakedProbes&&!runtimeProbes){
    previousProbes=LightmapSettings.lightProbes;runtimeProbes=Instantiate(BakedProbes);
    var positions=runtimeProbes.positions;for(int i=0;i<positions.Length;i++)positions[i]=transform.TransformPoint(positions[i]);
    runtimeProbes.SetPositionsSelf(positions,true);LightmapSettings.lightProbes=runtimeProbes;LightProbes.Tetrahedralize();
   }
  }
  void OnDestroy(){if(runtimeProbes){if(LightmapSettings.lightProbes==runtimeProbes)LightmapSettings.lightProbes=previousProbes;if(Application.isPlaying)Destroy(runtimeProbes);else DestroyImmediate(runtimeProbes);}}
 }
}
