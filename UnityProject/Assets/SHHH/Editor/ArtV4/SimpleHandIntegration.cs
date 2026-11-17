using System;
using System.IO;
using System.Linq;
using SHHH.Automation;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class SimpleHandIntegration {
  const string Dir="Assets/SHHH/ArtProductionV4/Hands";
  [ShhhCommand("v4_integrate_hands")]
  public static string Build(){
   if(EditorApplication.isPlaying)throw new Exception("Stop play mode before editing Boot");
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleHands/Prefabs/WhiteHand.prefab");if(!source)throw new Exception("Installed WhiteHand prefab missing");
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Glove.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.96f,.93f,.88f));mat.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(mat,Dir+"/Glove.mat");}
   foreach(var left in new[]{false,true}){
    string side=left?"Left":"Right";var go=UnityEngine.Object.Instantiate(source);go.name="SHHH_SimpleHands_"+side;
    var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();
    if(left){
     var reflection=Matrix4x4.Scale(new Vector3(-1,1,1));
     foreach(var t in go.GetComponentsInChildren<Transform>(true)){var p=t.localPosition;p.x=-p.x;t.localPosition=p;var q=t.localRotation;t.localRotation=new Quaternion(q.x,-q.y,-q.z,q.w);}
     var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.name="InstalledSimpleHand_LeftMirror";
     mesh.vertices=mesh.vertices.Select(v=>new Vector3(-v.x,v.y,v.z)).ToArray();mesh.normals=mesh.normals.Select(v=>new Vector3(-v.x,v.y,v.z)).ToArray();mesh.tangents=mesh.tangents.Select(v=>new Vector4(-v.x,v.y,v.z,-v.w)).ToArray();mesh.bindposes=mesh.bindposes.Select(b=>reflection*b*reflection).ToArray();
     for(int s=0;s<mesh.subMeshCount;s++){var tris=mesh.GetTriangles(s);for(int i=0;i<tris.Length;i+=3){int swap=tris[i];tris[i]=tris[i+2];tris[i+2]=swap;}mesh.SetTriangles(tris,s);}
     mesh.RecalculateBounds();var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/InstalledSimpleHand_LeftMirror.asset");
     if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,Dir+"/InstalledSimpleHand_LeftMirror.asset");skin.sharedMesh=mesh;
    }
    skin.sharedMaterial=mat;skin.updateWhenOffscreen=true;var visual=go.AddComponent<SimpleHandVisual>();visual.Skin=skin;visual.IsLeft=left;visual.ModelScale=SimpleHandVisual.ChildScale;
    PrefabUtility.SaveAsPrefabAsset(go,Dir+"/SimpleHand_"+side+".prefab");UnityEngine.Object.DestroyImmediate(go);
   }
   var scene=EditorSceneManager.OpenScene("Assets/SHHH/Scenes/Boot.unity",OpenSceneMode.Single);
   var rig=UnityEngine.Object.FindFirstObjectByType<PlayerRig>();if(!rig)throw new Exception("Working PlayerRig missing from Boot");
   foreach(var hi in new[]{rig.LeftHand,rig.RightHand}){
    string side=hi.Source.Hand==Handedness.Left?"Left":"Right";
    var old=hi.transform.Find("InstalledSimpleHandVisual");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    foreach(var mr in hi.GetComponentsInChildren<OVRMeshRenderer>(true))mr.enabled=false;
    foreach(var sk in hi.GetComponentsInChildren<SkinnedMeshRenderer>(true))sk.enabled=false;
    var visualGo=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/SimpleHand_"+side+".prefab"),hi.transform);visualGo.name="InstalledSimpleHandVisual";
    var visual=visualGo.GetComponent<SimpleHandVisual>();visual.Interactor=hi;visual.Source=hi.Source;
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   return "Installed SimpleHands visuals bound to existing Boot HandInteractors; OVR hand/skeleton/input retained. Left mirror generated with positive scales. Device validation pending.";
  }
 }
}
