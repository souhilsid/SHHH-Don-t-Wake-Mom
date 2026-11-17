using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>Per-tick tracked pose of one human's head and hands (world space; every peer builds the same room at the origin).</summary>
    public struct NetHumanInput : INetworkInput
    {
        public Vector3 Head; public Quaternion HeadRot;
        public Vector3 L; public Quaternion LRot;
        public Vector3 R; public Quaternion RRot;
        public NetworkBool LTracked, RTracked;
    }

    /// <summary>Host-simulated prop pose + current holder slot (-1 none).</summary>
    public struct NetPropPose : INetworkStruct
    {
        public Vector3 P; public Quaternion R; public sbyte Holder;
        /// <summary>V12: millilitres in this prop if it is a vessel (mug, bottle, bucket ...), host-simulated.</summary>
        public short Ml;
    }

    /// <summary>V12 host-simulated puddle (spilled liquid on a surface).</summary>
    public struct NetPuddle : INetworkStruct
    {
        public Vector3 P; public Vector3 N; public float Litres; public float MaxR; public NetworkBool Granular;
    }

    /// <summary>Host-driven body (bot sibling 0-3, cat 4, dog 5).</summary>
    public struct NetAgentPose : INetworkStruct
    {
        public Vector3 P; public float Yaw; public float Speed; public NetworkBool Carry; public NetworkBool Visible; public NetworkBool Plot;
    }
}
