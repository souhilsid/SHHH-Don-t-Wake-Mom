using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SHHH.Net
{
    /// <summary>One Fusion peer (runner callbacks): host spawns rigs + shared prop; every peer feeds its rig input.</summary>
    public sealed class NetPeer : MonoBehaviour, INetworkRunnerCallbacks
    {
        public NetSmokeHarness Harness; public string Name; public NetworkRunner Runner;
        public int SceneLoads;
        bool _propSpawned;

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Harness.Note($"{Name}: player joined {player}");
            if (!runner.IsServer) return;
            runner.Spawn(NetSmokeHarness.PlayerPrefab, new Vector3(player.AsIndex, 0, 0), Quaternion.identity, player);
            if (!_propSpawned) { _propSpawned = true; runner.Spawn(NetSmokeHarness.PropPrefab, new Vector3(0, 0.8f, 1f), Quaternion.identity); }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => Harness.Note($"{Name}: player left {player}");

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            float t = Time.time; float side = runner.IsServer ? 0f : 2f;
            input.Set(new NetRigInput
            {
                Head = new Vector3(side + Mathf.Sin(t * 1.3f) * 0.3f, 1.5f, Mathf.Cos(t * 1.1f) * 0.2f),
                Hand = new Vector3(side + 0.3f + Mathf.Sin(t * 2.1f) * 0.25f, 1.1f + Mathf.Cos(t * 1.7f) * 0.1f, 0.3f),
            });
        }

        public void OnSceneLoadDone(NetworkRunner runner) { SceneLoads++; Harness.Note($"{Name}: scene load done ({SceneManager.GetActiveScene().name})"); }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => Harness.OnPeerShutdown(this, shutdownReason);
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Harness.Note($"{Name}: disconnected {reason}");
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Harness.Note($"{Name}: connect failed {reason}");
        public void OnConnectedToServer(NetworkRunner runner) => Harness.Note($"{Name}: connected to server");
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
