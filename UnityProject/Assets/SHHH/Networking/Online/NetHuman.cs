using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>One connected human's tracked head + hands (input authority = that player; the host replicates it to all).</summary>
    public sealed class NetHuman : NetworkBehaviour
    {
        [Networked] public Vector3 Head { get; set; }
        [Networked] public Quaternion HeadRot { get; set; }
        [Networked] public Vector3 L { get; set; }
        [Networked] public Quaternion LRot { get; set; }
        [Networked] public Vector3 R { get; set; }
        [Networked] public Quaternion RRot { get; set; }
        [Networked] public NetworkBool LTracked { get; set; }
        [Networked] public NetworkBool RTracked { get; set; }

        public override void Spawned() { Runner.MakeDontDestroyOnLoad(gameObject); ShhhNet.I?.OnHumanSpawned(this); }
        public override void Despawned(NetworkRunner runner, bool hasState) { ShhhNet.I?.OnHumanDespawned(this); }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetHumanInput i))
            {
                Head = i.Head; HeadRot = Norm(i.HeadRot); L = i.L; LRot = Norm(i.LRot); R = i.R; RRot = Norm(i.RRot); LTracked = i.LTracked; RTracked = i.RTracked;
            }
        }

        /// <summary>The voice Speaker is a child: keep this object at the speaker's head so voice chat is positional.</summary>
        public override void Render() { if (Head != Vector3.zero) transform.SetPositionAndRotation(Head, Norm(HeadRot)); }

        static Quaternion Norm(Quaternion q) => q.x == 0 && q.y == 0 && q.z == 0 && q.w == 0 ? Quaternion.identity : q;
        public PlayerRef Player => Object != null ? Object.InputAuthority : PlayerRef.None;
    }
}
