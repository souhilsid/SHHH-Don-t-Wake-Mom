using Fusion;
using UnityEngine;

namespace SHHH.Net
{
    /// <summary>
    /// Host-owned session object: the four lobby seats (replicated to every client for the lobby board) and the
    /// client -> host intent RPCs (hello, ready, grab, release, activity, tracking, room loaded). The host applies
    /// intents to the authoritative rules + physics in <see cref="ShhhNet"/>.
    /// </summary>
    public sealed class NetLobby : NetworkBehaviour
    {
        public static NetLobby I { get; private set; }
        [Networked, Capacity(4)] public NetworkArray<int> SeatPlayer => default;          // PlayerRef.PlayerId, -1 = bot
        [Networked, Capacity(4)] public NetworkArray<NetworkString<_32>> SeatName => default;
        [Networked, Capacity(4)] public NetworkArray<NetworkBool> SeatReady => default;
        [Networked, Capacity(4)] public NetworkArray<NetworkBool> SeatConnected => default;
        [Networked, Capacity(4)] public NetworkArray<int> SeatCosmetic => default;
        [Networked] public NetworkBool MatchRunning { get; set; }
        [Networked] public int MatchSerial { get; set; }

        public override void Spawned() { I = this; Runner.MakeDontDestroyOnLoad(gameObject); ShhhNet.I?.OnLobbySpawned(this); }
        public override void Despawned(NetworkRunner runner, bool hasState) { if (I == this) I = null; }

        public int SeatOf(PlayerRef p) { for (int i = 0; i < 4; i++) if (SeatPlayer[i] == p.PlayerId) return i; return -1; }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Hello(string playerName, int cosmetic, RpcInfo info = default) => ShhhNet.I?.HostOnHello(info.Source, playerName, cosmetic);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Ready(NetworkBool ready, RpcInfo info = default) => ShhhNet.I?.HostOnReady(info.Source, ready);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Grab(int propId, int hand, RpcInfo info = default) => ShhhNet.I?.HostOnGrab(info.Source, propId, hand);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Release(int propId, Vector3 velocity, NetworkBool thrown, int hand, RpcInfo info = default) => ShhhNet.I?.HostOnRelease(info.Source, propId, velocity, thrown, hand);
        /// <summary>V7 recipe poke intents (lid / control buttons) from a client; the host sets the actor from the RPC source.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Recipe(int verb, int fixture, string arg, RpcInfo info = default) => ShhhNet.I?.HostOnRecipe(info.Source, verb, fixture, arg);
        /// <summary>V12 hand-moved mechanism intents (door / drawer Open-Close with level, START Press, house Mech): whole op, actor from the source.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RecipeOp(int verb, int prop, int prop2, int fixture, int slot, string arg, RpcInfo info = default) => ShhhNet.I?.HostOnRecipeOp(info.Source, verb, prop, prop2, fixture, slot, arg);
        /// <summary>V7 confrontation input from a client: 0 show memory card, 1 point at seat, 2 nothing to add.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Confront(int kind, int arg, RpcInfo info = default) => ShhhNet.I?.HostOnConfront(info.Source, kind, arg);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Activity(RpcInfo info = default) => ShhhNet.I?.HostOnActivity(info.Source);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Tracking(NetworkBool lost, RpcInfo info = default) => ShhhNet.I?.HostOnTracking(info.Source, lost);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RoomLoaded(int roundIndex, RpcInfo info = default) => ShhhNet.I?.HostOnRoomLoaded(info.Source, roundIndex);
        /// <summary>
        /// Host -> all clients: the rules journal, in order. Fusion reliable RPCs (small chunks, 10 Hz) instead of one
        /// reliable-data transfer per frame: net_multi run 1 showed transfers queueing behind each other (client 120 s behind).
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
        public void RPC_Journal(int seq, byte[] data) => ShhhNet.I?.ClientOnJournal(seq, data);

        /// <summary>Genies look (avatar definition JSON, ~0.8 KB) in reliable chunks: owner -> host, then host -> everyone (keyed by PlayerId,
        /// so it survives seat changes). Only the SDK's definition string travels - meshes are resolved by each client's Genies SDK.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_LookIn(int part, int parts, string chunk, RpcInfo info = default) => ShhhNet.I?.HostOnLookChunk(info.Source.PlayerId, part, parts, chunk);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
        public void RPC_LookOut(int playerId, int part, int parts, string chunk) => ShhhNet.I?.ClientOnLookChunk(playerId, part, parts, chunk);

        /// <summary>Client test harness evidence travels to the host log too (independent-process QA).</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Report(string line, RpcInfo info = default) => ShhhNet.I?.HostOnReport(info.Source, line);
    }
}
