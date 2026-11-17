using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// Host-authoritative shared prop: any peer requests a grab via RPC; the host grants it to one holder at a time
    /// (first come wins, contention resolved on the host) and moves it with the holder's networked hand.
    /// </summary>
    public sealed class NetSharedProp : NetworkBehaviour
    {
        public static readonly List<NetSharedProp> All = new List<NetSharedProp>();
        [Networked] public PlayerRef Holder { get; set; }
        [Networked] public Vector3 Pos { get; set; }
        [Networked] public int GrabCount { get; set; }

        public override void Spawned()
        {
            All.Add(this);
            var v = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform; v.SetParent(transform, false); v.localScale = new Vector3(0.24f, 0.01f, 0.24f);
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            if (HasStateAuthority) Pos = transform.position;
            Runner.MakeDontDestroyOnLoad(gameObject);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => All.Remove(this);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RequestGrab(PlayerRef who)
        {
            if (Holder != PlayerRef.None && Holder != who) return; // contention: holder keeps it
            Holder = who; GrabCount++;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Release(PlayerRef who) { if (Holder == who) Holder = PlayerRef.None; }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Holder == PlayerRef.None) return;
            var p = NetRigPlayer.For(Runner, Holder);
            if (p != null) Pos = p.Hand;
        }

        public override void Render() => transform.position = Pos;

        public static NetSharedProp In(NetworkRunner r) { foreach (var x in All) if (x != null && x.Runner == r) return x; return null; }
    }
}
