using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Static access to the live authoritative simulation. Offline solo: this device is the authority.
    /// Online (Fusion host mode): only the host has IsAuthority = true; clients send intents instead.
    /// </summary>
    public static class GameServices
    {
        public static MatchController Match;
        public static RoundController Round => Match != null ? Match.Round : PracticeRound;
        public static RoundController PracticeRound;
        public static RoomRuntime Room;
        public static bool IsAuthority = true;
        /// <summary>Photon session bridge (null in builds without networking).</summary>
        public static INetBridge Net;
        /// <summary>Online client: the local match is a journal replica - never call its mutating API, send intents via Net.</summary>
        public static bool IsReplica => !IsAuthority && Match != null;
        /// <summary>Remote human seats (online): their avatars are driven by the network, not by a bot body.</summary>
        public static System.Func<int, bool> IsRemoteHuman = _ => false;
        /// <summary>Online: world pose of a remote human's head/hands (for AI queries and host-side holding). Null offline.</summary>
        public static System.Func<int, int, Pose?> RemotePose;
        /// <summary>V12: which seat holds a prop on THIS peer (online: the replicated holder table; offline: the round).</summary>
        public static System.Func<int, int> HolderOf = id => Round != null ? Round.HolderOf(id) : -1;
        /// <summary>V12: on an online client the host streams every liquid (vessels, puddles, trays): show, never simulate volume.</summary>
        public static bool HostStreamsLiquids; // (slot, 0 head / 1 left / 2 right)
        public static int LocalSlot = 0;
        /// <summary>Online: the Genies avatar definition a remote human published for their seat (null = not known yet).</summary>
        public static System.Func<int, string> RemoteLook;
        /// <summary>Online client: sends a recipe intent (poke button) to the host; returns false when not connected. Set by the Networking layer.</summary>
        public static System.Func<SHHH.Core.ROp, bool> SendRecipeIntent;
        /// <summary>Online client: confrontation input to the host (0 show card memoryId, 1 point at seat, 2 nothing to add). Set by Networking.</summary>
        public static System.Action<int, int> SendConfrontIntent;
        public static bool PracticeMode;
        public static bool RoundPlayable => Round != null && Round.IsPlayable;
        public static float Now => Round != null ? Round.Time : 0f;
    }
}
