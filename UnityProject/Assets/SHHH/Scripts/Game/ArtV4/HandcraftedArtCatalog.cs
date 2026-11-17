using System;
using UnityEngine;
namespace SHHH.Game {
 [CreateAssetMenu(menuName="SHHH/V5 Handcrafted Art")]
 public sealed class HandcraftedArtCatalog:ScriptableObject {
  [Serializable]public class Entry{public string kind;public GameObject visual;}
  public Entry[] props;
  static HandcraftedArtCatalog instance;
  public static GameObject Prop(string kind){if(!instance)instance=Resources.Load<HandcraftedArtCatalog>("SHHH_HandcraftedArt");if(!instance)return null;foreach(var e in instance.props)if(e.kind==kind)return e.visual;return null;}
 }
}
