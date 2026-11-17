using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>Per-tick input sent by each peer's tracked rig (head + one hand) to the host.</summary>
    public struct NetRigInput : INetworkInput
    {
        public Vector3 Head;
        public Vector3 Hand;
    }
}
