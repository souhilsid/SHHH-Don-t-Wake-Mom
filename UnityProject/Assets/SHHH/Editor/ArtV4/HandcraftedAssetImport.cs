using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class HandcraftedAssetImport {
  public const string A="Assets/SHHH/ArtProductionV5";
  public static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../SHHH_ArtProduction_V5"));
  [ShhhCommand("v5_import_heroes")]
  public static string Import(){
   if(EditorApplication.isPlaying)throw new Exception("Stop play mode before importing authored art");Directory.CreateDirectory(A+"/Materials");Directory.CreateDirectory(A+"/Prefabs");AssetDatabase.Refresh();
   foreach(string file in Directory.GetFiles(A+"/Textures","*.png")){
    var ti=(TextureImporter)AssetImporter.GetAtPath(file);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=!file.Contains("Roughness")&&!file.Contains("Metallic");ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=2048;ti.anisoLevel=2;ti.textureCompression=TextureImporterCompression.Compressed;var android=ti.GetPlatformTextureSettings("Android");android.overridden=true;android.format=TextureImporterFormat.ASTC_6x6;android.maxTextureSize=2048;ti.SetPlatformTextureSettings(android);ti.SaveAndReimport();
   }
   var painted=Material("SHHH_V5_PaintedHouse","SHHH_House_Painted_BaseColor_2048.png");painted.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Textures/SHHH_House_MetallicSmoothness_2048.png"));painted.EnableKeyword("_METALLICSPECGLOSSMAP");painted.SetFloat("_Smoothness",1);
   var story=Material("SHHH_V5_StoryIllustrations","SHHH_Story_Atlas_2048.png");story.SetFloat("_Smoothness",.17f);var glow=Material("SHHH_V5_LampGlow",null);glow.SetColor("_BaseColor",new Color(1,.78f,.46f));glow.SetColor("_EmissionColor",new Color(1,.55f,.18f)*1.2f);glow.EnableKeyword("_EMISSION");glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
   var night=Material("SHHH_V5_NightView","SHHH_Story_Atlas_2048.png");night.shader=Shader.Find("Universal Render Pipeline/Unlit");night.SetColor("_BaseColor",new Color(.8f,.88f,1));
   var sb=new System.Text.StringBuilder("asset_id,triangles,vertices,materials,width,height,depth,prefab,device_validated\n");
   foreach(var path in Directory.GetFiles(A+"/Models","*.fbx")){
    var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.globalScale=1;importer.importAnimation=false;importer.generateSecondaryUV=true;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Off;importer.importNormals=ModelImporterNormals.Import;
    importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
    foreach(var name in new[]{"SHHH_V5_PaintedHouse","SHHH_V5_StoryIllustrations","SHHH_V5_LampGlow","SHHH_V5_NightView"})importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),name.Contains("NightView")?night:name.Contains("Story")?story:name.Contains("Glow")?glow:painted);
    importer.SaveAndReimport();var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=Path.GetFileNameWithoutExtension(path);
    foreach(var r in go.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){string n=mats[i]?mats[i].name:"";mats[i]=n.Contains("NightView")?night:n.Contains("Story")?story:n.Contains("Glow")?glow:painted;}r.sharedMaterials=mats;}
    // Preserve the FBX axis conversion and authored origin below a clean Unity-space root.
    // Placement code is allowed to rotate the root; it must never overwrite the imported -90 degree correction.
    var imported=go;go=new GameObject(Path.GetFileNameWithoutExtension(path));imported.transform.SetParent(go.transform,true);imported.name="AuthoredMesh";
    var bounds=Prim.RendererBounds(go);var meshes=go.GetComponentsInChildren<MeshFilter>();string prefab=A+"/Prefabs/"+go.name+".prefab";
    if(new[]{"V5_Book","V5_Mug","V5_Plate","V5_ToyCar","V5_Cushion","V5_Remote"}.Contains(go.name)){
     // A disabled collision template travels with the visual prefab. PropBody copies
     // this hull into its authoritative root; ghost and static copies stay nonphysical.
     var collision=new GameObject("AuthoredCollision");collision.transform.SetParent(go.transform,false);var hull=collision.AddComponent<BoxCollider>();hull.center=bounds.center;hull.size=bounds.size;hull.enabled=false;
    }
    foreach(var side in new[]{"Left","Right"})if(new[]{"V5_Book","V5_Mug","V5_Plate","V5_ToyCar","V5_Cushion"}.Contains(go.name)){
     var grip=new GameObject("Grip_"+side).transform;grip.SetParent(go.transform,false);grip.localPosition=new Vector3((side=="Left"?-1:1)*bounds.extents.x,go.name=="V5_Book"?bounds.max.y-.018f:bounds.center.y,bounds.center.z);grip.localRotation=Quaternion.Euler(0,side=="Left"?90:-90,0);
    }
    PrefabUtility.SaveAsPrefabAsset(go,prefab);sb.AppendLine($"{go.name},{meshes.Sum(m=>m.sharedMesh.triangles.Length/3)},{meshes.Sum(m=>m.sharedMesh.vertexCount)},{go.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)},{bounds.size.x:F4},{bounds.size.y:F4},{bounds.size.z:F4},{prefab},NO");UnityEngine.Object.DestroyImmediate(go);
   }
   var catalog=AssetDatabase.LoadAssetAtPath<HandcraftedArtCatalog>("Assets/SHHH/Resources/SHHH_HandcraftedArt.asset");if(!catalog){catalog=ScriptableObject.CreateInstance<HandcraftedArtCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/SHHH/Resources/SHHH_HandcraftedArt.asset");}
   var mapping=new[]{("book","V5_Book"),("cup","V5_Mug"),("mug","V5_Mug"),("plate","V5_Plate"),("toy_car","V5_ToyCar"),("cushion","V5_Cushion"),("remote","V5_Remote")};catalog.props=mapping.Select(p=>new HandcraftedArtCatalog.Entry{kind=p.Item1,visual=AssetDatabase.LoadAssetAtPath<GameObject>(A+"/Prefabs/"+p.Item2+".prefab")}).ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Directory.CreateDirectory(Output+"/Evidence/Unity");File.WriteAllText(Output+"/UNITY_ASSET_IMPORT.csv",sb.ToString());return "Imported authored FBX/UV/ASTC textures/prefabs; current prop kinds bound through existing VisualFactory. Device unverified.";
  }
  static Material Material(string name,string texture){string path=A+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.name=name;m.SetColor("_BaseColor",Color.white);if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"/Textures/"+texture));m.enableInstancing=true;return m;}
 }
}
