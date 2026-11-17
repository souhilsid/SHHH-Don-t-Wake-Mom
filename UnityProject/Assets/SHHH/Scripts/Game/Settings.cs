using System;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>Per-device player settings (stored locally with PlayerPrefs; no telemetry, no accounts).</summary>
    [Serializable]
    public sealed class Settings
    {
        public string PlayerName = "You";
        public bool VoiceChatOn = true;   // Photon Voice microphone (online only; never a noise input)
        public int Cosmetic = 0;
        /// <summary>V8: the player's saved Genies avatar definition (JSON from ManagedAvatar.GetDefinition); empty = seat starter preset.</summary>
        public string GeniesDefinition = "";
        public int BotDifficulty = 1;      // 0 Rookie, 1 Normal, 2 Clever
        public int MatchMinutes = 5;       // 2026-11-09: one continuous match, host/solo chooses 5 or 10 minutes
        public bool MusicOn = true;
        public float MasterVolume = 1f, SfxVolume = 1f, VoiceVolume = 1f, MusicVolume = 0.45f, AmbienceVolume = 0.6f;
        public bool TeleportMode = false;   // "hop only": both hands teleport, no hand-walk
        /// <summary>V12 (owner: "add teleportation in the house"): the right hand hops (V13: finger gun, drop the thumb) while the left hand
        /// still walks (finger gun, thumb held down). Off = walk only.</summary>
        public bool HopRightHand = true;
        /// <summary>V13: which locomotion defaults this save has seen. The finger gun (V13) resets the style once to right hand hops /
        /// left hand walks (owner, Quest c18: "both hands walk" - an older save had "Walk only").</summary>
        public int LocoVersion = 0;
        public const int CurrentLocoVersion = 13;
        public float SnapAngle = 30f;
        public bool Seated = false;
        public bool TutorialDone = false;
        public bool SkipIntro = false;
        public bool VoiceChatConsent = false;

        public static Settings Current = new Settings();
        const string Key = "shhh_settings_v1";

        public static void Load()
        {
            try { var j = PlayerPrefs.GetString(Key, ""); if (!string.IsNullOrEmpty(j)) Current = JsonUtility.FromJson<Settings>(j) ?? new Settings(); }
            catch (Exception e) { Debug.LogWarning("[SHHH] settings reset: " + e.Message); Current = new Settings(); }
            if (Current.LocoVersion < CurrentLocoVersion)
            {
                GrabLog.Add($"settings: locomotion reset to walk + hop (was {(Current.TeleportMode ? "hop only" : Current.HopRightHand ? "walk + hop" : "walk only")}, save v{Current.LocoVersion})");
                Current.TeleportMode = false; Current.HopRightHand = true; Current.LocoVersion = CurrentLocoVersion; Save();
            }
            GrabLog.Add("settings: locomotion " + (Current.TeleportMode ? "hop only" : Current.HopRightHand ? "walk + hop (right hand hops)" : "walk only"));
            Apply();
        }

        public static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current)); PlayerPrefs.Save(); }

        public static void Apply()
        {
            var c = Current;
            AudioBank.Master = c.MasterVolume;
            AudioBank.BusVolume[(int)AudioBus.Sfx] = c.SfxVolume;
            AudioBank.BusVolume[(int)AudioBus.Voice] = c.VoiceVolume;
            AudioBank.BusVolume[(int)AudioBus.Music] = c.MusicOn ? c.MusicVolume : 0f;
            AudioBank.BusVolume[(int)AudioBus.Ambience] = c.AmbienceVolume;
        }
    }
}
