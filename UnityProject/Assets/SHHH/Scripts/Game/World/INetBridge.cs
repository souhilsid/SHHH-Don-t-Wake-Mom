using System.Collections.Generic;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>One lobby seat as shown on the in-world lobby board.</summary>
    public struct NetSeat
    {
        public string Name; public bool Human; public bool Ready; public bool Local; public bool Connected; public int Cosmetic;
    }

    /// <summary>
    /// V6 NET-G4: the Unity game layer's view of the Photon Fusion session (implemented in the Networking assembly by
    /// <c>SHHH.Net.ShhhNet</c>, registered at startup). Clients never touch the rules engine directly: every intent goes to
    /// the host through these calls, and the host's MatchJournal comes back and drives the client's replica match.
    /// </summary>
    public interface INetBridge
    {
        bool Active { get; }               // in a Photon session (lobby or match)
        bool IsHost { get; }
        bool InMatch { get; }
        string RoomCode { get; }
        string Status { get; }             // human-readable connection status / last error
        string Region { get; }
        IReadOnlyList<NetSeat> Seats { get; }
        bool LocalReady { get; }
        bool CanStart { get; }             // host: everybody ready

        void Create(string playerName, int cosmetic);
        void Join(string code, string playerName, int cosmetic);
        void SetReady(bool ready);
        void StartMatch();                 // host only
        void RequestRematch();             // host only
        void Leave();

        // in-match intents (clients) - the host applies them to the authoritative rules + physics
        void RequestGrab(int propId, int hand);
        void RequestRelease(int propId, Vector3 velocity, bool thrown, int hand);
        void ReportActivity();
        void ReportTrackingLost(bool lost);
        void RoomLoaded(int roundIndex);   // every peer (host included) after its room scene is ready
        /// <summary>Slot currently holding the prop according to the host (-1 none), for client-side prediction checks.</summary>
        int HolderOf(int propId);

        // voice chat (Photon Voice 2, positional; the microphone is NEVER an input to Mom's noise meter)
        bool MicOn { get; }
        void SetMic(bool on);
        string VoiceStatus { get; }
    }
}
