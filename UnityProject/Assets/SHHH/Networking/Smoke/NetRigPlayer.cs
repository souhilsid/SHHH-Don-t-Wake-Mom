using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>Networked sibling rig: host applies the owner's input; everybody renders Head/Hand.</summary>
    public sealed class NetRigPlayer : NetworkBehaviour
    {
        public static readonly List<NetRigPlayer> All = new List<NetRigPlayer>();
        [Networked] public Vector3 Head { get; set; }
        [Networked] public Vector3 Hand { get; set; }
        Transform _headVis, _handVis;

        public override void Spawned()
        {
            All.Add(this);
            _headVis = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform; _headVis.SetParent(transform, false); _headVis.localScale = Vector3.one * 0.25f;
            _handVis = GameObject.CreatePrimitive(PrimitiveType.Cube).transform; _handVis.SetParent(transform, false); _handVis.localScale = Vector3.one * 0.08f;
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            Runner.MakeDontDestroyOnLoad(gameObject);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => All.Remove(this);

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetRigInput i)) { Head = i.Head; Hand = i.Hand; }
        }

        public override void Render()
        {
            if (_headVis != null) _headVis.position = Head;
            if (_handVis != null) _handVis.position = Hand;
        }

        public static NetRigPlayer For(NetworkRunner r, PlayerRef p)
        {
            foreach (var x in All) if (x != null && x.Runner == r && x.Object != null && x.Object.InputAuthority == p) return x;
            return null;
        }
    }
}
