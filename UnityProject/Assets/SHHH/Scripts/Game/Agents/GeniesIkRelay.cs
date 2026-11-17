using UnityEngine;

namespace SHHH.Game
{
    /// <summary>OnAnimatorIK only reaches scripts on the Animator's own GameObject; this forwards it to the owning SiblingAvatar.</summary>
    public sealed class GeniesIkRelay : MonoBehaviour
    {
        public SiblingAvatar Owner;
        void OnAnimatorIK(int layer) { if (Owner != null) Owner.GeniesIk(); }
    }
}
