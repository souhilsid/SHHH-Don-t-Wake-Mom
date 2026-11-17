using System;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>A loaded character body (e.g. a Genies avatar) handed to SiblingAvatar.</summary>
    public sealed class AvatarHandle
    {
        public GameObject Root;
        public Animator Animator;
        public string Definition;
        public Action Release;
    }

    /// <summary>
    /// V8 (2026-11-09 continuation): the primary character system is the Genies Avatar SDK (assembly SHHH.Genies registers
    /// itself here at startup). SHHH.Game only talks to this interface so the game keeps running - with the existing sibling
    /// bodies as a temporary loading/offline visual - when the SDK cannot initialise or is not logged in.
    /// </summary>
    public interface IAvatarBackend
    {
        string Name { get; }
        /// <summary>SDK initialised and an avatar source is usable.</summary>
        bool Ready { get; }
        /// <summary>Human-readable state (init / login / preset building / error) for UI and QA.</summary>
        string Status { get; }
        void Warmup();
        /// <summary>Load a body: <paramref name="definition"/> = a saved Genies avatar definition; null = the starter preset of
        /// <paramref name="presetSeat"/> (Pip, Nell, Rory, Tess).</summary>
        void Load(string definition, int presetSeat, Transform parent, Action<AvatarHandle> done, Func<bool> wanted = null);
        /// <summary>Definition of a starter preset (null until built).</summary>
        string PresetDefinition(int seat);
        /// <summary>Opens the in-world Genies wardrobe in front of <paramref name="anchor"/>.</summary>
        void OpenWardrobe(Transform anchor, Action onClosed);
        /// <summary>V12: is this definition dressed (top + bottom)? Unclothed looks are never shown.</summary>
        bool IsClothed(string definition);
        bool WardrobeOpen { get; }
    }

    public static class AvatarService
    {
        public static IAvatarBackend Backend;
        /// <summary>QA counters (wardrobe crash 2026-11-12): bodies asked for / really built by the SDK / thrown away (parent gone) / alive.</summary>
        public static int LoadsRequested, LoadsBuilt, LoadsDiscarded, LiveBodies, LoadsSkipped;
        public static bool Available => Backend != null && Backend.Ready;
        public static string Status => Backend == null ? "no avatar backend (SHHH.Genies not loaded)" : Backend.Status;
        /// <summary>Raised when the local player's saved look changes (wardrobe Save) so stages can refresh.</summary>
        public static event Action LocalLookChanged;
        public static void NotifyLocalLookChanged() { LocalLookChanged?.Invoke(); SeatLooksChanged?.Invoke(); }
        /// <summary>Raised when any seat's look may have changed (local save, a remote player's look arrived over the network, presets built).
        /// Avatars in the house re-check <see cref="DefinitionFor"/> and re-dress if it differs.</summary>
        public static event Action SeatLooksChanged;
        public static void NotifySeatLooksChanged() => SeatLooksChanged?.Invoke();

        /// <summary>The look a seat shows: the local human's saved Genies definition, otherwise the seat's starter preset (null).</summary>
        public static string DefinitionFor(int slot, bool isBot)
        {
            bool ok(string d) => !string.IsNullOrEmpty(d) && (Backend == null || Backend.IsClothed(d));   // V12: always clothed
            if (!isBot && slot == GameServices.LocalSlot && ok(Settings.Current.GeniesDefinition)) return Settings.Current.GeniesDefinition;
            if (!isBot && GameServices.RemoteLook != null) { var d = GameServices.RemoteLook(slot); if (ok(d)) return d; }
            return null;
        }
    }
}
