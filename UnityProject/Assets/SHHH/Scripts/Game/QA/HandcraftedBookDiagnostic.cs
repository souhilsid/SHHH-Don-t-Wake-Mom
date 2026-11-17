#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SHHH.Core;
using UnityEngine;
namespace SHHH.Game {
 /// <summary>Offline acceptance fixture. Sends simulated hand poses into the installed Boot rig;
 /// all ownership, physical carry, snapping, scoring and sabotage remain in the production components.</summary>
 [DefaultExecutionOrder(10000)]
 public sealed class HandcraftedBookDiagnostic : MonoBehaviour {
  static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
  static string Flag=>Root+"/Automation/v5_book.flag";
  string Dir=>Root+"/SHHH_ArtProduction_V5/Evidence/Runtime";
  [Serializable]class Check { public string name,detail;public bool pass; }
  [Serializable]class Report { public string harness="Real Boot rig / SimHandSource / HandInteractor / PropBody / AnchorFixture / RoundController";public string hardware="UNVERIFIED: no Quest connected";public string capturedUtc;public ulong seed;public int pass,fail,frames;public float nominalFps=8;public List<Check> checks=new List<Check>();public List<string> events=new List<string>(); }
  readonly Report report=new Report(); PlayerRig rig;RoomRuntime room;RoundController round;ChoreInstance chore;SimHandSource left,right;HandInteractor hand;Camera captureCamera;
  bool finished,capture,contactCaptured;float start,nextFrame;int frame;Vector3 headPos;Quaternion headRot;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Init(){if(!File.Exists(Flag))return;var go=new GameObject("V5_PhysicalBookAcceptance");DontDestroyOnLoad(go);go.AddComponent<HandcraftedBookDiagnostic>();}
  void Start(){start=Time.realtimeSinceStartup;Directory.CreateDirectory(Dir);StartCoroutine(SafeRun(Run()));}
  void Update(){if(round!=null&&!finished)round.Tick(Time.deltaTime);if(!finished&&Time.realtimeSinceStartup-start>240){CheckIt("timeout",false,"240 seconds elapsed");Finish();}}
  void LateUpdate(){if(rig&&rig.Head){rig.Head.SetPositionAndRotation(headPos,headRot);}if(capture&&Time.time>=nextFrame){nextFrame=Time.time+.125f;Capture("Frames/"+(frame++).ToString("D5")+".png",960,540);}}
  IEnumerator SafeRun(IEnumerator e){while(true){object current;try{if(!e.MoveNext())yield break;current=e.Current;}catch(Exception ex){CheckIt("exception",false,ex.ToString());Finish();yield break;}yield return current;}}
  void CheckIt(string n,bool ok,string detail){report.checks.Add(new Check{name=n,pass=ok,detail=detail});Debug.Log("[SHHH][v5_book] "+(ok?"PASS ":"FAIL ")+n+" "+detail);}
  IEnumerator Run(){
   float wait=0;while((GameRoot.I==null||GameRoot.I.State!=AppState.FrontEnd)&&wait<60){wait+=Time.deltaTime;yield return null;}
   if(GameRoot.I==null)throw new Exception("Boot GameRoot unavailable");
   GameRoot.I.StartPractice();wait=0;while(GameRoot.I.State!=AppState.Practice&&wait<40){wait+=Time.deltaTime;yield return null;}
   if(GameRoot.I.State!=AppState.Practice)throw new Exception("Practice staging did not finish");
   var root=GameRoot.I;rig=root.Rig;root.enabled=false;if(root.Room)Destroy(root.Room.gameObject);yield return null;
   var roster=new CompetitorRoster();roster.SeatHuman(0,"Pip",0);roster.SeatHuman(1,"Rory",2);roster.LockForMatch();for(int i=2;i<4;i++)roster.Eliminate(i,EliminationReason.None,-1);
   for(ulong seed=1;seed<1000;seed++){var cfg=RoundConfig.PracticeConfig();cfg.Room=RoomId.LivingRoom;var candidate=new RoundController(cfg,roster,seed);candidate.Begin();var c=candidate.Board.Chores.FirstOrDefault(x=>x.Template.Id=="lr_books"&&x.Owner==0);if(c!=null){round=candidate;chore=c;report.seed=seed;break;}}
   if(round==null)throw new Exception("No deterministic owner-book seed");
   GameServices.Match=null;GameServices.PracticeRound=round;GameServices.LocalSlot=0;GameServices.IsAuthority=true;GameServices.PracticeMode=true;
   room=new GameObject("V5_Actual_LivingRoom_Runtime").AddComponent<RoomRuntime>();room.LocalHead=rig.Head;room.Build(round.Room);room.Bind(round,round.Seed);room.enabled=false;round.Pets.Clear();
   round.Log.Sink=line=>report.events.Add(line.ToString());
   var ovr=rig.GetComponent<OVRCameraRig>();if(ovr)ovr.enabled=false;rig.Spawn(new Vector3(-1.6f,0,-1.9f),-45);
   left=new GameObject("QA_Sim_Left").AddComponent<SimHandSource>();left.Hand=Handedness.Left;right=new GameObject("QA_Sim_Right").AddComponent<SimHandSource>();right.Hand=Handedness.Right;
   rig.Left=left;rig.Right=right;rig.LeftHand.Source=left;rig.RightHand.Source=right;hand=rig.RightHand;
   headPos=new Vector3(-1.7f,1.6f,-1.95f);headRot=Quaternion.LookRotation(new Vector3(-3.1f,1.05f,-.25f)-headPos);captureCamera=rig.Head.GetComponent<Camera>();
   if(UiPointer.I){UiPointer.I.Rebind(0,left,rig.LeftHand);UiPointer.I.Rebind(1,right,rig.RightHand);}PokeButton.Pokers.Clear();PokeButton.Pokers.Add(left);PokeButton.Pokers.Add(right);
   left.transform.position=headPos+new Vector3(-.18f,-.45f,.24f);right.transform.position=headPos+new Vector3(.18f,-.45f,.24f);left.palmNormal=right.palmNormal=Vector3.down;
   // Let the existing six-second tutorial finish before inspecting the real grip.
   yield return new WaitForSeconds(7);
   CheckIt("connected_house_loaded",room.GetComponentInChildren<HouseAssembly>()!=null,"Production RoomRuntime instantiated Resources/SHHH_WorldHouse; current living-room task IDs retained.");
   CheckIt("runtime_baked_probes_bound",LightmapSettings.lightProbes&&LightmapSettings.lightProbes.count==48,"Runtime prefab light probe samples="+(LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0));
   var visuals=rig.GetComponentsInChildren<SimpleHandVisual>(true);CheckIt("installed_hands_bound",visuals.Length==2&&visuals.All(v=>v.PoseValid&&v.Skin&&v.Skin.enabled),"Installed SimpleHands visual instances="+visuals.Length+"; simulated poses, not hardware tracking.");
   Capture("01_LivingRuntime.png",1600,900);capture=true;nextFrame=Time.time;
   foreach(var step in chore.Steps.ToArray()){
    var p=room.Prop(step.PropId);var a=room.Anchor(step.AnchorId);yield return Transfer(p,a.PlacePoint,0,true);yield return new WaitForSeconds(.8f);
    CheckIt("placed_book_"+p.Spec.Id,chore.StepForProp(p.Spec.Id).Done,"Anchor occupancy="+(a.Occupant==p)+"; base="+p.transform.position+"; reject="+round.Board.LastRejectReason);
   }
   CheckIt("whole_book_chore_completed",chore.State==ChoreState.Completed,chore.DoneSteps+"/"+chore.Steps.Count+" steps; authoritative ChoreBoard state.");Capture("02_BooksShelved.png",1600,900);
   if(chore.State!=ChoreState.Completed){Finish();yield break;}
   var first=chore.Steps[0];var stolen=room.Prop(first.PropId);var slot=room.Anchor(first.AnchorId);
   yield return Transfer(stolen,new Vector3(-1.8f,.755f,-1.24f),1,false);yield return new WaitForSeconds(.5f);
   CheckIt("rival_sabotage_recorded",round.Board.Sabotage.Any(s=>s.PropId==stolen.Spec.Id&&s.Saboteur==1&&s.Victim==0)&&!first.Done,"Rory physically removed Pip's placed book through the real HandInteractor.");Capture("03_Sabotage.png",1600,900);
   yield return Transfer(stolen,slot.PlacePoint,0,true);yield return new WaitForSeconds(.8f);
   CheckIt("owner_repair_foiled_sabotage",first.Done&&round.Board.Sabotage.Any(s=>s.PropId==stolen.Spec.Id&&s.Foiled),"Owner restored the book through physical release and AnchorFixture; foil window="+ChoreBoard.FoilWindow);
   CheckIt("chore_restored",chore.State==ChoreState.Completed,chore.DoneSteps+"/"+chore.Steps.Count+" completed after repair.");Capture("04_Repair.png",1600,900);
   yield return new WaitForSeconds(2);Finish();
  }
  IEnumerator Transfer(PropBody p,Vector3 destination,int actor,bool shelf){
   right.simPinch=0;right.simGrip=0;yield return new WaitForSeconds(.25f);GameServices.LocalSlot=actor;
   // Approach the top of the real collision hull, with no direct calls to Grab/Placed/Removed.
   bool extracting=p.transform.position.x<-3f;
   var touch=p.Col.bounds.center+Vector3.up*(p.Col.bounds.extents.y-.012f);right.transform.rotation=Quaternion.LookRotation(Vector3.left,Vector3.up);
   right.transform.position=touch+new Vector3(.18f,extracting?0:.12f,0);yield return new WaitForSeconds(.15f);yield return Move(touch,.35f);right.simPinch=1;yield return new WaitForSeconds(.3f);
   CheckIt("hand_grab_"+actor+"_"+p.Spec.Id,hand.Held==p,"Actual held="+(hand.Held?hand.Held.Spec.Id:-1)+"; failure="+hand.LastGrabFailure);
   if(hand.Held!=p){right.simPinch=0;yield break;}
   // A shelf volume must clear the front edge before it can be lifted toward the shelf above.
   if(extracting){yield return Move(right.transform.position+Vector3.right*.36f,.28f);yield return Move(right.transform.position+Vector3.up*.18f,.35f);}
   else {yield return Move(right.transform.position+Vector3.up*.32f,.35f);yield return Move(right.transform.position+Vector3.right*.22f,.35f);}
   if(!contactCaptured){yield return null;CaptureContact();contactCaptured=true;}
   // Match the authored spine-out shelf orientation by moving the hand, never writing the held prop transform.
   Quaternion begin=right.transform.rotation,end=Quaternion.Euler(0,-90,0)*Quaternion.Inverse(p.transform.rotation)*begin;float t=0;
   while(t<.5f){t+=Time.deltaTime;right.transform.rotation=Quaternion.Slerp(begin,end,t/.5f);yield return null;}
   yield return new WaitForSeconds(.15f);
   var handOffset=right.transform.position-p.transform.position;
   var approach=destination+new Vector3(shelf?.43f:0,shelf?0:.16f,0);
   yield return Move(approach+handOffset,.55f);yield return Move(destination+handOffset,.14f);yield return new WaitForSeconds(.4f);
   right.simPinch=0;yield return new WaitForSeconds(.25f);yield return Move(right.transform.position+Vector3.right*.3f+Vector3.up*.1f,.4f);
  }
  IEnumerator Move(Vector3 target,float speed){Vector3 from=right.transform.position;float duration=Mathf.Max(.1f,Vector3.Distance(from,target)/speed),t=0;while(t<duration){t+=Time.deltaTime;right.transform.position=Vector3.Lerp(from,target,Mathf.SmoothStep(0,1,t/duration));yield return null;}right.transform.position=target;}
  void CaptureContact(){
   var position=captureCamera.transform.position;var rotation=captureCamera.transform.rotation;float fov=captureCamera.fieldOfView,near=captureCamera.nearClipPlane;
   captureCamera.transform.position=right.PinchPoint+new Vector3(.30f,.16f,-.25f);captureCamera.transform.LookAt(right.PinchPoint);captureCamera.fieldOfView=42;captureCamera.nearClipPlane=.01f;
   Capture("05_BookContact_Closeup.png",1280,900);captureCamera.transform.SetPositionAndRotation(position,rotation);captureCamera.fieldOfView=fov;captureCamera.nearClipPlane=near;
   int angle=0;foreach(var offset in new[]{new Vector3(-.24f,.08f,-.29f),new Vector3(-.20f,-.06f,.28f)}){
    captureCamera.transform.position=right.PinchPoint+offset;captureCamera.transform.LookAt(right.PinchPoint);captureCamera.fieldOfView=42;captureCamera.nearClipPlane=.01f;Capture("05_BookContact_"+(angle++)+".png",1280,900);
   }
   captureCamera.transform.SetPositionAndRotation(position,rotation);captureCamera.fieldOfView=fov;captureCamera.nearClipPlane=near;
  }
  void Capture(string name,int width,int height){if(!captureCamera)return;string path=Dir+"/"+name;Directory.CreateDirectory(Path.GetDirectoryName(path));var prev=captureCamera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(width,height,24){antiAliasing=4};captureCamera.targetTexture=target;captureCamera.Render();RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());captureCamera.targetTexture=prev;RenderTexture.active=active;Destroy(image);target.Release();Destroy(target);}
  void Finish(){if(finished)return;finished=true;capture=false;report.frames=frame;report.capturedUtc=DateTime.UtcNow.ToString("O");report.pass=report.checks.Count(c=>c.pass);report.fail=report.checks.Count-report.pass;File.WriteAllText(Dir+"/BOOK_CHORE_RESULT.json",JsonUtility.ToJson(report,true));if(File.Exists(Flag))File.Delete(Flag);UnityEditor.EditorApplication.isPlaying=false;}
 }
}
#endif
