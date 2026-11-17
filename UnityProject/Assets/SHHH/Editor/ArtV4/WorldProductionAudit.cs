using System;
using System.IO;
using System.Linq;
using System.Text;
using SHHH.Automation;
using SHHH.Core;
using SHHH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SHHH.EditorTools {
 public static class WorldProductionAudit {
  public static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../SHHH_Astra_3D_World_Production_V4"));
  [ShhhCommand("v4_audit")]
  public static string Audit(){
   var dir=Path.Combine(Output,"00_Audit_And_Project_Backup_Notes");Directory.CreateDirectory(dir);var sb=new StringBuilder();
   sb.AppendLine("Unity "+Application.unityVersion+"; target "+EditorUserBuildSettings.activeBuildTarget+"; playing "+EditorApplication.isPlaying);
   sb.AppendLine("Current scene "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
   sb.AppendLine("Android architectures "+PlayerSettings.Android.targetArchitectures+"; graphics "+string.Join(",",PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)));
   sb.AppendLine("Color space "+PlayerSettings.colorSpace+"; quality "+QualitySettings.names[QualitySettings.GetQualityLevel()]);
   sb.AppendLine("Scenes: "+string.Join(", ",EditorBuildSettings.scenes.Select(s=>s.path)));
   var rig=UnityEngine.Object.FindFirstObjectByType<PlayerRig>(FindObjectsInactive.Include);
   if(rig) sb.AppendLine("PlayerRig "+rig.name+"; left "+rig.Left+"; right "+rig.Right+"; hand interactors "+rig.LeftHand+" / "+rig.RightHand);
   var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleHands/Prefabs/WhiteHand.prefab");
   if(model){var go=UnityEngine.Object.Instantiate(model);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
    sb.AppendLine("SimpleHands prefab GUID "+AssetDatabase.AssetPathToGUID("Assets/SimpleHands/Prefabs/WhiteHand.prefab"));
    foreach(var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
     sb.AppendLine("Renderer "+sk.name+" triangles="+sk.sharedMesh.triangles.Length/3+" bounds="+sk.bounds+" root="+sk.rootBone.name+" bones="+sk.bones.Length);
     sb.AppendLine("bone,parent,world_x,world_y,world_z,local_x,local_y,local_z,qx,qy,qz,qw");
     foreach(var b in sk.bones){var p=b.position;var l=b.localPosition;var q=b.rotation;sb.AppendLine($"{b.name},{b.parent.name},{p.x:R},{p.y:R},{p.z:R},{l.x:R},{l.y:R},{l.z:R},{q.x:R},{q.y:R},{q.z:R},{q.w:R}");}
    }
    UnityEngine.Object.DestroyImmediate(go);
   }
   var tasks=new StringBuilder("current_template_id,title,room,prop_kind,anchor_kind\n");
   foreach(var t in ChoreLibrary.All)tasks.AppendLine($"{t.Id},{t.Title},{t.Room},{t.PropKind},{t.AnchorKind}");
   File.WriteAllText(Path.Combine(dir,"CURRENT_CHORE_TEMPLATES.csv"),tasks.ToString());
   foreach(RoomId id in Enum.GetValues(typeof(RoomId))){var d=RoomCatalog.Get(id);sb.AppendLine($"{id}: {d.Width} x {d.Depth} x {d.Height} m; {d.Props.Count} props, {d.Anchors.Count} anchors, {d.Furniture.Count} furniture");}
   sb.AppendLine("Installed ISDK manifests: "+string.Join(",",UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p=>p.name.Contains("meta")||p.name.Contains("openxr")||p.name.Contains("interaction")).Select(p=>p.name+" "+p.version)));
   File.WriteAllText(Path.Combine(dir,"UNITY_NATIVE_AUDIT.txt"),sb.ToString());return sb.ToString();
  }
 }
}
