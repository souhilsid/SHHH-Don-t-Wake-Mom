using System;
using System.Collections.Generic;
using UnityEngine;
namespace SHHH.Game {
 /// <summary>Visual-only retarget of the installed SimpleHands skin. Never writes to tracking, interactors or held objects.</summary>
 [DefaultExecutionOrder(120)]
 public sealed class SimpleHandVisual : MonoBehaviour {
  public HandInteractor Interactor;
  public HandSource Source;
  public SkinnedMeshRenderer Skin;
  public Transform[] Bones;
  public float ModelScale=.15f;
  public bool IsLeft;
  /// <summary>V12: my Genies hands are shown instead - the glove keeps tracking (its wrist and joints drive the body), unseen.</summary>
  public bool HiddenForBody;
  public bool PoseValid {get;private set;}
  public bool UsingTrackedJoints {get;private set;}
  public int BoundJointCount {get;private set;}
  Vector3[] restPosition,restLocal,restDirection;
  Quaternion[] restRotation,restLocalRotation,aimCorrection;
  Vector3 restWrist;Quaternion restFrame;
  // V12 (Quest run 2026-11-11: tracked gloves looked corrupted - stretched palms, fanned paddle fingers, claws bending backwards):
  // rotation-only retarget. The glove keeps its own bone lengths, every joint copies only the DIRECTION of the real finger segment,
  // the roll reference is the back-of-hand normal measured the same way on the glove's rest pose and on the live joints, and the
  // glove is scaled uniformly to the player's real palm length so fingertips still meet the real pinch.
  Vector3 restBack;Quaternion restFrameBack;Quaternion[] aimBack;float restPalm,liveScale,restHandLen;
  /// <summary>QA only: synthetic tracked joints (index 0..15 = Names order, 16..20 = thumb/index/middle/ring/little tips).</summary>
  public static Func<SimpleHandVisual,int,Vector3?> QaJoint;
  /// <summary>QA only: the pre-V12 position-forcing retarget (to show the old corruption next to the fix).</summary>
  public static bool LegacyRetarget;
  readonly Vector3[] targetPositions=new Vector3[16];readonly bool[] targetValid=new bool[16];
  readonly Dictionary<OVRSkeleton.BoneId,Transform> tracked=new Dictionary<OVRSkeleton.BoneId,Transform>();
  OvrHandSource boundSource;int boundCount;
  Vector3 thumbTipLocal,indexTipLocal;Vector3[] otherTipLocal;
  static readonly int[] IndexChain={4,5,6},ThumbChain={2,3};
  static readonly int[][] OtherChains={new[]{7,8,9},new[]{10,11,12},new[]{13,14,15}};
  /// <summary>V7 Gate 1: child-sized cartoon glove (was .15 = adult, 15 cm wide open hand).</summary>
  public const float ChildScale=.13f;
  /// <summary>Held-object grip metrics of the last frame (QA): thumb and index on opposite faces, distance outside the faces (negative = penetration).</summary>
  public struct GripInfo{public bool Active,Opposed;public int Axis;public float ThumbGap,IndexGap,Thickness;public int FingersInside;public string Kind;}
  public GripInfo LastGrip;
  public Vector3 ThumbTipVisual=>Bones!=null&&Bones[3]?Bones[3].TransformPoint(thumbTipLocal):transform.position;
  public Vector3 IndexTipVisual=>Bones!=null&&Bones[6]?Bones[6].TransformPoint(indexTipLocal):transform.position;
  static readonly string[] Names={"Wrist","Hand","Thumb","Thumb2","IndexFinger","Index2","Index3","MiddleFinger","Middle2","Middle3","RingFinger","Ring2","Ring3","LittleFinger","Little2","Little3"};
  static readonly OVRSkeleton.BoneId[] XR={OVRSkeleton.BoneId.XRHand_Wrist,OVRSkeleton.BoneId.XRHand_Palm,OVRSkeleton.BoneId.XRHand_ThumbProximal,OVRSkeleton.BoneId.XRHand_ThumbDistal,OVRSkeleton.BoneId.XRHand_IndexProximal,OVRSkeleton.BoneId.XRHand_IndexIntermediate,OVRSkeleton.BoneId.XRHand_IndexDistal,OVRSkeleton.BoneId.XRHand_MiddleProximal,OVRSkeleton.BoneId.XRHand_MiddleIntermediate,OVRSkeleton.BoneId.XRHand_MiddleDistal,OVRSkeleton.BoneId.XRHand_RingProximal,OVRSkeleton.BoneId.XRHand_RingIntermediate,OVRSkeleton.BoneId.XRHand_RingDistal,OVRSkeleton.BoneId.XRHand_LittleProximal,OVRSkeleton.BoneId.XRHand_LittleIntermediate,OVRSkeleton.BoneId.XRHand_LittleDistal};
  // legacy thumb: Thumb2 = proximal, Thumb3 = distal (the same anatomy as XRHand_ThumbProximal / ThumbDistal)
  static readonly OVRSkeleton.BoneId[] Legacy={OVRSkeleton.BoneId.Hand_WristRoot,OVRSkeleton.BoneId.Hand_WristRoot,OVRSkeleton.BoneId.Hand_Thumb2,OVRSkeleton.BoneId.Hand_Thumb3,OVRSkeleton.BoneId.Hand_Index1,OVRSkeleton.BoneId.Hand_Index2,OVRSkeleton.BoneId.Hand_Index3,OVRSkeleton.BoneId.Hand_Middle1,OVRSkeleton.BoneId.Hand_Middle2,OVRSkeleton.BoneId.Hand_Middle3,OVRSkeleton.BoneId.Hand_Ring1,OVRSkeleton.BoneId.Hand_Ring2,OVRSkeleton.BoneId.Hand_Ring3,OVRSkeleton.BoneId.Hand_Pinky1,OVRSkeleton.BoneId.Hand_Pinky2,OVRSkeleton.BoneId.Hand_Pinky3};
  static readonly OVRSkeleton.BoneId[] Tips={OVRSkeleton.BoneId.XRHand_ThumbTip,OVRSkeleton.BoneId.XRHand_IndexTip,OVRSkeleton.BoneId.XRHand_MiddleTip,OVRSkeleton.BoneId.XRHand_RingTip,OVRSkeleton.BoneId.XRHand_LittleTip};
  static readonly OVRSkeleton.BoneId[] OldTips={OVRSkeleton.BoneId.Hand_ThumbTip,OVRSkeleton.BoneId.Hand_IndexTip,OVRSkeleton.BoneId.Hand_MiddleTip,OVRSkeleton.BoneId.Hand_RingTip,OVRSkeleton.BoneId.Hand_PinkyTip};
  void Awake(){Initialize();}
  public void Initialize(){
   if(restPosition!=null)return;
   if(Skin==null)Skin=GetComponentInChildren<SkinnedMeshRenderer>();
   Bones=new Transform[16];var all=GetComponentsInChildren<Transform>(true);
   for(int i=0;i<16;i++)foreach(var b in all)if(b.name==Names[i]){Bones[i]=b;break;}
   if(Array.Exists(Bones,b=>b==null)){enabled=false;Debug.LogError("SimpleHands visual: incomplete installed prefab hierarchy");return;}
   restPosition=new Vector3[16];restLocal=new Vector3[16];restDirection=new Vector3[16];restRotation=new Quaternion[16];restLocalRotation=new Quaternion[16];aimCorrection=new Quaternion[16];
   for(int i=0;i<16;i++){
    var b=Bones[i];restPosition[i]=transform.InverseTransformPoint(b.position);restRotation[i]=Quaternion.Inverse(transform.rotation)*b.rotation;restLocal[i]=b.localPosition;restLocalRotation[i]=b.localRotation;
    Vector3 direction=i==1?Bones[7].position-b.position:b.childCount>0?b.GetChild(0).position-b.position:b.up;
    restDirection[i]=transform.InverseTransformDirection(direction).normalized;
    aimCorrection[i]=Quaternion.Inverse(SafeAim(restDirection[i],Vector3.up))*restRotation[i];
   }
   restWrist=restPosition[0];restFrame=SafeAim(restPosition[7]-restWrist,Vector3.up);
   restBack=BackNormal(restPosition[0],restPosition[4],restPosition[13]);restFrameBack=SafeAim(restPosition[7]-restWrist,restBack);
   aimBack=new Quaternion[16];for(int i=0;i<16;i++)aimBack[i]=Quaternion.Inverse(SafeAim(restDirection[i],restBack))*restRotation[i];
   restPalm=Mathf.Max(1e-4f,(restPosition[7]-restPosition[0]).magnitude);
   thumbTipLocal=Bones[3].InverseTransformPoint(Bones[3].position+(Bones[3].position-Bones[2].position)*.8f);
   indexTipLocal=Bones[6].InverseTransformPoint(Bones[6].position+(Bones[6].position-Bones[5].position)*.8f);
   otherTipLocal=new Vector3[3];for(int c=0;c<3;c++){var ch=OtherChains[c];otherTipLocal[c]=Bones[ch[2]].InverseTransformPoint(Bones[ch[2]].position+(Bones[ch[2]].position-Bones[ch[1]].position)*.8f);}
   ModelScale=Mathf.Min(ModelScale,ChildScale);
   if(Skin){Skin.updateWhenOffscreen=true;Skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
  }
  static Vector3 BackNormal(Vector3 wrist,Vector3 indexBase,Vector3 littleBase){var n=Vector3.Cross(indexBase-wrist,littleBase-wrist);return n.sqrMagnitude<1e-12f?Vector3.up:n.normalized;}
  /// <summary>Tracked joint position (see TrackedGeniesBody.JointOf for the index order).</summary>
  public bool TryJoint(int i,out Vector3 p)=>Joint(i,out p);
  bool Joint(int i,out Vector3 p){
   if(QaJoint!=null){var q=QaJoint(this,i);if(q.HasValue){p=q.Value;return true;}p=default;return false;}
   if(i==1&&!xrSkel){p=default;return false;}   // the legacy skeleton has no palm joint (Legacy[1] is the wrist again)
   return i<16?Get(XR[i],Legacy[i],out p):Get(Tips[i-16],OldTips[i-16],out p);
  }
  static Quaternion SafeAim(Vector3 direction,Vector3 normal){if(direction.sqrMagnitude<1e-8f)direction=Vector3.forward;if(Vector3.Cross(direction,normal).sqrMagnitude<1e-8f)normal=Vector3.right;return Quaternion.LookRotation(direction,normal);}
  bool xrSkel=true;
  bool Get(OVRSkeleton.BoneId id,OVRSkeleton.BoneId fallback,out Vector3 p){Transform t;if(tracked.TryGetValue(xrSkel?id:fallback,out t)){p=t.position;return true;}p=default;return false;}
  void Bind(OvrHandSource src){
   int count=src&&src.skeleton&&src.skeleton.Bones!=null?src.skeleton.Bones.Count:0;
   if(src==boundSource&&count==boundCount)return;
   tracked.Clear();boundSource=src;boundCount=count;xrSkel=src&&src.skeleton?OvrHandSource.IsXr(src.skeleton.GetSkeletonType()):true;
   if(count>0)foreach(var b in src.skeleton.Bones)if(b!=null&&b.Transform)tracked[b.Id]=b.Transform;
  }
  void LateUpdate(){ApplyPose();}
  public void ApplyPose(){
   Initialize();if(restPosition==null)return;
   var src=Interactor?Interactor.Source:Source;PoseValid=src&&src.Tracked&&src.HighConfidence&&!src.SystemGesture;
   if(Skin)Skin.enabled=PoseValid&&!HiddenForBody;if(!PoseValid){UsingTrackedJoints=false;return;}
   for(int i=0;i<16;i++){Bones[i].localPosition=restLocal[i];Bones[i].localRotation=restLocalRotation[i];}
   var ovr=src as OvrHandSource;Bind(ovr);Vector3 wrist,middle;Vector3 poseNormal=src.PalmNormal;
   var heldBook=Interactor&&Interactor.Held&&Interactor.Held.Spec.Kind=="book"?Interactor.Held:null;
   UsingTrackedJoints=(ovr||QaJoint!=null)&&Joint(0,out wrist)&&Joint(7,out middle);
   if(UsingTrackedJoints){Joint(0,out wrist);Joint(7,out middle);}
   else if(heldBook&&src.Pinch>.5f&&src.Grip<.5f){
    // Synthetic top-edge grip: reach across the thin axis of the volume. The
    // thumb stays on the near cover and the index passes over to the far cover.
    Vector3 forward=heldBook.transform.right;if(Vector3.Dot(forward,src.HandRotation*Vector3.forward)<0)forward=-forward;
    poseNormal=-heldBook.transform.up;wrist=src.PinchPoint-forward*.108f-poseNormal*.043f;middle=wrist+forward*.1f;
   }else{wrist=Vector3.Lerp(src.PalmCenter-src.HandRotation*Vector3.forward*.075f,src.PinchPoint-src.HandRotation*Vector3.forward*.115f-src.PalmNormal*.025f,src.Pinch);middle=wrist+src.HandRotation*Vector3.forward*.1f;}
   var rotation=SafeAim(middle-wrist,poseNormal)*Quaternion.Inverse(restFrame);
   transform.localScale=Vector3.one*ModelScale;transform.SetPositionAndRotation(wrist-rotation*(restWrist*ModelScale),rotation);
   transform.position+=wrist-Bones[0].position;
   BoundJointCount=0;
   if(UsingTrackedJoints&&!LegacyRetarget){
    var positions=targetPositions;var valid=targetValid;
    for(int i=0;i<16;i++){valid[i]=Joint(i,out positions[i]);if(i==1&&!valid[i]){positions[i]=Vector3.Lerp(wrist,middle,.35f);valid[i]=true;}if(valid[i])BoundJointCount++;}
    var w0=positions[0];var back=BackNormal(w0,positions[4],positions[13]);
    // uniform size from the real palm (wrist -> middle knuckle); smoothed so tracking noise does not pulse the glove
    // size from the whole middle finger (wrist -> knuckle -> 3 segments -> tip): the glove's wrist bone sits further back than a real
    // wrist, so the palm alone made the glove ~40 % too small (fingertips 3.5-4.5 cm short)
    if(restHandLen<=0f){var tipM=transform.InverseTransformPoint(Bones[9].TransformPoint(otherTipLocal[1]));restHandLen=(restPosition[7]-restPosition[0]).magnitude+(restPosition[8]-restPosition[7]).magnitude+(restPosition[9]-restPosition[8]).magnitude+(tipM-restPosition[9]).magnitude;}
    Vector3 tipReal;float realLen=(positions[7]-w0).magnitude+(positions[8]-positions[7]).magnitude+(positions[9]-positions[8]).magnitude+(Joint(18,out tipReal)?(tipReal-positions[9]).magnitude:0f);
    float want=Mathf.Clamp(realLen/Mathf.Max(1e-4f,restHandLen),.05f,.4f);liveScale=liveScale<=0f?want:Mathf.Lerp(liveScale,want,.2f);
    var rootRot=SafeAim(positions[7]-w0,back)*Quaternion.Inverse(restFrameBack);
    transform.localScale=Vector3.one*liveScale;transform.SetPositionAndRotation(w0-rootRot*(restWrist*liveScale),rootRot);
    for(int i=1;i<16;i++){
     if(!valid[i])continue;Vector3 end;bool hasEnd;
     if(i==1){end=positions[7];hasEnd=true;}
     else if(i==3||i==6||i==9||i==12||i==15){int f=i==3?0:(i-3)/3;hasEnd=Joint(16+f,out end);}
     else{end=positions[i+1];hasEnd=valid[i+1];}
     // direction only: the glove's bone keeps its own length (no stretching)
     if(hasEnd&&(end-positions[i]).sqrMagnitude>1e-10f)Bones[i].rotation=SafeAim(end-positions[i],back)*aimBack[i];
    }
    // pinch fidelity: the cartoon thumb sits and points differently from a real one - bend the thumb and index chains (rotations only,
    // bone lengths kept) so their tips land on the real thumb / index tips; that is where the player feels the pinch
    Vector3 tt,ti;if(Joint(16,out tt))SolveTip(ThumbChain,thumbTipLocal,tt,1f);if(Joint(17,out ti))SolveTip(IndexChain,indexTipLocal,ti,1f);
   }else if(UsingTrackedJoints){
    var positions=targetPositions;var valid=targetValid;
    for(int i=0;i<16;i++){valid[i]=Joint(i,out positions[i]);if(i==1&&!valid[i])positions[i]=Vector3.Lerp(wrist,middle,.35f);if(valid[i])BoundJointCount++;}
    for(int i=0;i<16;i++)if(valid[i]){
     Vector3 end;bool hasEnd;
     if(i==0){end=positions[1];hasEnd=true;}else if(i==1){end=positions[7];hasEnd=true;}
     else if(i==3||i==6||i==9||i==12||i==15){int f=i==3?0:(i-3)/3;hasEnd=Joint(16+f,out end);}
     else{end=positions[i+1];hasEnd=valid[i+1];}
     Bones[i].position=positions[i];if(hasEnd)Bones[i].rotation=SafeAim(end-positions[i],src.PalmNormal)*aimCorrection[i];
    }
   }else{
    // Offline visual fallback only. Full hardware pose always uses joint data above.
    float curl=Mathf.Max(src.Grip,src.Pinch*.55f);
    for(int i=2;i<16;i++){
     float angle=i<4?(i==2?35:65):((i-4)%3==0?48:(i-4)%3==1?100:140);
     float c=(i==4||i==5||i==6)?Mathf.Max(src.Grip,src.Pinch):curl;
     var axis=rotation*Vector3.Cross(restDirection[i],Vector3.up).normalized;
     Bones[i].rotation=Quaternion.AngleAxis(angle*c,axis)*rotation*restRotation[i];
    }
    if(src.Pinch>.01f&&src.Grip<.5f){
     // Keep a genuine opposed thumb/index contact at the interaction point for the
     // controller/simulation fallback. Hardware remains driven only by tracked joints.
     Vector3 side=Vector3.Cross(src.PalmNormal,src.HandRotation*Vector3.forward).normalized*(IsLeft?-1:1);
     Vector3 indexTarget=src.PinchPoint+side*.003f,thumbTarget=src.PinchPoint-side*.003f;
     // The authored book has a real thickness. Oppose its covers instead of closing
     // both tips through its centre. This only adjusts a synthetic visual pose.
     var held=Interactor?Interactor.Held:null;
     if(held&&held.Spec.Kind=="book"&&held.Col is BoxCollider box){
      Vector3 local=box.transform.InverseTransformPoint(src.PinchPoint);var half=box.size*.5f;
      local.y=Mathf.Clamp(local.y,box.center.y-half.y+.015f,box.center.y+half.y-.012f);
      local.z=Mathf.Clamp(local.z,box.center.z-half.z+.015f,box.center.z+half.z-.015f);
      local.x=box.center.x-half.x-.004f;Vector3 a=box.transform.TransformPoint(local);
      local.x=box.center.x+half.x+.004f;Vector3 b=box.transform.TransformPoint(local);
      bool aIsFar=Vector3.Dot(a-b,middle-wrist)>0;
      indexTarget=aIsFar?a:b;thumbTarget=aIsFar?b:a;
     }
     SolveTip(IndexChain,indexTipLocal,indexTarget,src.Pinch);
     SolveTip(ThumbChain,thumbTipLocal,thumbTarget,src.Pinch);
    }
   }
   GripSolve(src);
   // Source wrist, interaction pose, grab ownership and physics remain untouched.
  }

  /// <summary>
  /// V7 Gate 1 grip pose for ANY held prop, on tracked joints as well as the offline fallback: the real pinch closes the
  /// fingertips through the virtual object, so (visual only) the thumb is solved onto the near face and the index onto the
  /// opposite face along the graspable axis of the prop's collider, and the other fingertips are pushed out to wrap the far
  /// side. Interaction, ownership and physics are untouched.
  /// </summary>
  /// <summary>QA: the largest relative change of a glove bone length versus the glove's own model (0 = no stretching).</summary>
  public float QaMaxBoneStretch(){float m=0f;float sc=transform.lossyScale.x;for(int i=1;i<16;i++){var b=Bones[i];if(b.childCount==0)continue;var c=b.GetChild(0);int ci=ChildIndex(i);if(ci<0)continue;float rest=(restPosition[ci]-restPosition[i]).magnitude;if(rest<1e-6f)continue;float now=Vector3.Distance(c.position,b.position)/Mathf.Max(1e-6f,sc);m=Mathf.Max(m,Mathf.Abs(now/rest-1f));}return m;}
  int ChildIndex(int i){if(Bones[i].childCount==0)return -1;var c=Bones[i].GetChild(0);for(int k=0;k<16;k++)if(Bones[k]==c)return k;return -1;}
  /// <summary>QA: glove vs real hand frame - wrist offset, knuckle offset, back-of-hand alignment, thumb side.</summary>
  public string QaFrameReport(Vector3[] j){
   var gb=BackNormal(Bones[0].position,Bones[4].position,Bones[13].position);var rb=BackNormal(j[0],j[4],j[13]);
   var gside=Vector3.Dot(Bones[2].position-Bones[7].position,Vector3.Cross(Bones[7].position-Bones[0].position,gb));var rside=Vector3.Dot(j[2]-j[7],Vector3.Cross(j[7]-j[0],rb));
   return $"wrist off {Vector3.Distance(Bones[0].position,j[0])*100f:0.0} cm, middle knuckle off {Vector3.Distance(Bones[7].position,j[7])*100f:0.0} cm, back normals dot {Vector3.Dot(gb,rb):0.00}, thumb side glove {Mathf.Sign(gside)} real {Mathf.Sign(rside)}, scale {transform.lossyScale.x:0.000}";
  }
  /// <summary>QA: the visual fingertip of finger f (0 thumb .. 4 little).</summary>
  public Vector3 QaTip(int f){if(f==0)return ThumbTipVisual;if(f==1)return IndexTipVisual;var ch=OtherChains[f-2];return Bones[ch[2]].TransformPoint(otherTipLocal[f-2]);}
  void GripSolve(HandSource src){
   LastGrip=default;
   var held=Interactor?Interactor.Held:null;
   if(held==null||!(held.Col is BoxCollider box)||src.Pinch<.35f)return;
   var bt=box.transform;Vector3 c=box.center,h=box.size*.5f;
   Vector3 pinchL=bt.InverseTransformPoint(src.PinchPoint);
   Vector3 thumbL=bt.InverseTransformPoint(ThumbTipVisual),indexL=bt.InverseTransformPoint(IndexTipVisual);
   Vector3 dL=indexL-thumbL;if(dL.sqrMagnitude<1e-6f)dL=bt.InverseTransformDirection(Vector3.Cross(src.PalmNormal,src.HandRotation*Vector3.forward));
   // graspable axis: the one the thumb->index line follows best among thin enough axes (<=12 cm), else the thinnest
   int k=-1;float best=-1;for(int a=0;a<3;a++){if(box.size[a]>.12f)continue;float sc=Mathf.Abs(dL[a])/(dL.magnitude+1e-6f);if(sc>best){best=sc;k=a;}}
   if(k<0){k=0;for(int a=1;a<3;a++)if(box.size[a]<box.size[k])k=a;}
   // a thick prop is never pinched top-to-bottom: if the chosen axis is mostly vertical and too thick, use the thinnest horizontal axis
   Vector3 AxisW(int a)=>bt.TransformDirection(a==0?Vector3.right:a==1?Vector3.up:Vector3.forward);
   if(box.size[k]>.07f&&Mathf.Abs(Vector3.Dot(AxisW(k),Vector3.up))>.7f){int hk=-1;for(int a=0;a<3;a++){if(a==k)continue;if(hk<0||box.size[a]<box.size[hk])hk=a;}k=hk;}
   float side=Mathf.Sign(thumbL[k]-c[k]);if(Mathf.Abs(thumbL[k]-c[k])<1e-4f)side=-Mathf.Sign(dL[k]==0?1:dL[k]);
   const float r=.006f; // fingertip skin radius
   // thick props (> 7 cm, e.g. cup, toy car, cushion): a hand cannot pinch across them; grip the TOP EDGE instead -
   // thumb on the near side just below the rim, index (and the others) over the rim onto the top face
   int up=-1;bool edge=box.size[k]>.07f;
   if(edge){float bu=-1;for(int a=0;a<3;a++){if(a==k)continue;float d=Mathf.Abs(Vector3.Dot(bt.TransformDirection(a==0?Vector3.right:a==1?Vector3.up:Vector3.forward),Vector3.up));if(d>bu){bu=d;up=a;}}}
   float upSign=edge?Mathf.Sign(Vector3.Dot(bt.TransformDirection(up==0?Vector3.right:up==1?Vector3.up:Vector3.forward),Vector3.up)):1f;
   Vector3 Face(Vector3 l,float s){for(int j=0;j<3;j++){if(j==k)continue;float m=Mathf.Min(.012f,h[j]*.5f);l[j]=Mathf.Clamp(l[j],c[j]-h[j]+m,c[j]+h[j]-m);}
    if(edge&&s!=side){l[up]=c[up]+upSign*(h[up]+r);l[k]=c[k]+side*Mathf.Max(0f,h[k]-.025f);return bt.TransformPoint(l);} // over the rim, 2.5 cm in
    if(edge){l[up]=c[up]+upSign*Mathf.Max(0f,h[up]-.02f);}
    l[k]=c[k]+s*(h[k]+r);return bt.TransformPoint(l);}
   float Outside(Vector3 world){var l=bt.InverseTransformPoint(world);float d=-1e9f;for(int a=0;a<3;a++)d=Mathf.Max(d,Mathf.Abs(l[a]-c[a])-h[a]);return d;} // >0 = outside the box
   float w=UsingTrackedJoints?1f:Mathf.Clamp01(src.Pinch);
   Vector3 axisW=bt.TransformDirection(k==0?Vector3.right:k==1?Vector3.up:Vector3.forward)*side;
   for(int pass=0;pass<2;pass++){
    SolveTip(ThumbChain,thumbTipLocal,Face(pinchL,side),w);
    SolveTip(IndexChain,indexTipLocal,Face(pinchL,-side),w);
    // finger chains are short: if both tips miss in the same direction, slide the whole VISUAL hand along the grip axis
    // (<= 3 cm) so thumb and index both meet their covers; tracking and the interaction pinch point are not moved.
    var tl=bt.InverseTransformPoint(ThumbTipVisual);var il=bt.InverseTransformPoint(IndexTipVisual);
    float tg=side*(tl[k]-c[k])-h[k]-r,ig=edge?Outside(IndexTipVisual)-r:-side*(il[k]-c[k])-h[k]-r,t=Mathf.Clamp((ig-tg)*.5f,-.03f,.03f);
    if(Mathf.Abs(t)<.001f)break;
    transform.position+=axisW*t*w;
   }
   int inside=0;
   for(int f=0;f<3;f++){
    var ch=OtherChains[f];var tip=Bones[ch[2]].TransformPoint(otherTipLocal[f]);var tl=bt.InverseTransformPoint(tip);
    bool inBox=Mathf.Abs(tl.x-c.x)<h.x+r&&Mathf.Abs(tl.y-c.y)<h.y+r&&Mathf.Abs(tl.z-c.z)<h.z+r;
    if(inBox){
     // line the finger up behind the index on the far face at real knuckle spacing (natural pinch), then clamp to the face
     Vector3 spread=Bones[ch[0]].position-Bones[4].position;spread-=Vector3.Project(spread,axisW);
     Vector3 lineTarget=bt.InverseTransformPoint(IndexTipVisual+spread);
     SolveTip(ch,otherTipLocal[f],Face(lineTarget,-side),w);
     if(Outside(Bones[ch[2]].TransformPoint(otherTipLocal[f]))<0f){
      // unreachable: try the finger straight (rest), then fully curled; keep whichever ends outside the prop
      var keep=new Quaternion[3];for(int j=0;j<3;j++)keep[j]=Bones[ch[j]].localRotation;
      for(int j=0;j<3;j++)Bones[ch[j]].localRotation=restLocalRotation[ch[j]];
      if(Outside(Bones[ch[2]].TransformPoint(otherTipLocal[f]))<0f){
       var axis=transform.rotation*Vector3.Cross(restDirection[ch[0]],Vector3.up).normalized;
       for(int j=0;j<3;j++)Bones[ch[j]].rotation=Quaternion.AngleAxis(j==0?60:95,axis)*Bones[ch[j]].rotation;
       if(Outside(Bones[ch[2]].TransformPoint(otherTipLocal[f]))<0f)for(int j=0;j<3;j++)Bones[ch[j]].localRotation=keep[j];
      }
     }
     if(Outside(Bones[ch[2]].TransformPoint(otherTipLocal[f]))<0f)inside++;
    }
   }
   thumbL=bt.InverseTransformPoint(ThumbTipVisual);indexL=bt.InverseTransformPoint(IndexTipVisual);
   LastGrip=new GripInfo{Active=true,Axis=k,Kind=held.Spec.Kind,Thickness=box.size[k],FingersInside=inside,
    // edge grip counts as opposed when the thumb is on the side face and the index rests on the top face
    Opposed=edge?(side*(thumbL[k]-c[k])>h[k]-.005f&&upSign*(indexL[up]-c[up])>h[up]-.005f):(thumbL[k]-c[k])*(indexL[k]-c[k])<0,
    ThumbGap=Outside(ThumbTipVisual),IndexGap=Outside(IndexTipVisual)};
  }
  void SolveTip(int[] chain,Vector3 tipLocal,Vector3 target,float blend){
   var end= Bones[chain[chain.Length-1]];target=Vector3.Lerp(end.TransformPoint(tipLocal),target,blend);
   for(int iteration=0;iteration<10;iteration++)for(int j=chain.Length-1;j>=0;j--){var joint=Bones[chain[j]];Vector3 a=end.TransformPoint(tipLocal)-joint.position,b=target-joint.position;if(a.sqrMagnitude>1e-8f&&b.sqrMagnitude>1e-8f)joint.rotation=Quaternion.FromToRotation(a,b)*joint.rotation;}
  }
 }
}
