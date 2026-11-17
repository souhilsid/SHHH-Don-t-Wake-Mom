using System.Collections.Generic;
using System.IO;
using SHHH.Core;
using UnityEngine;

namespace SHHH.Game
{
    /// <summary>
    /// Local-first progress on this device (Application.persistentDataPath/shhh_progress.sav): loaded once, saved atomically with a
    /// backup, recovered/migrated by <see cref="ProgressStore"/>. Matches award progress once, only from the authoritative summary,
    /// and never in development/QA sessions (editor play, autopilot, network test harness, tutorial).
    /// </summary>
    public static class ProgressService
    {
        static PlayerProgress _p; static string _report = "";
        static ulong _awardedSeed; static bool _awardedAny;
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "shhh_progress.sav");
        public static string LoadReport => _report;
        /// <summary>Set by the network test harness (no rewards from automated sessions).</summary>
        public static bool AutomatedSession;

        public static PlayerProgress Current
        {
            get { if (_p == null) { _p = ProgressStore.Load(Path, out _report); Debug.Log("[SHHH] progress: " + _report + $" (level {_p.Level}, xp {_p.Xp})"); } return _p; }
        }

        public static bool IsDevSession => Application.isEditor || GameRoot.Autopilot || AutomatedSession || GameServices.PracticeMode;

        public static List<string> AwardMatch(MatchController m, int localSeat)
        {
            if (m == null) return new List<string>();
            if (_awardedAny && _awardedSeed == m.MatchSeed) return Current.LastAwards; // once per match
            _awardedAny = true; _awardedSeed = m.MatchSeed;
            var lines = ProgressionRules.Award(Current, m.Summary, localSeat, IsDevSession);
            if (!IsDevSession) Save();
            return lines;
        }

        public static void Save()
        {
            try { ProgressStore.Save(Path, Current); }
            catch (System.Exception e) { Debug.LogWarning("[SHHH] progress save failed: " + e.Message); }
        }

        /// <summary>The cosmetic int this player sends for their seat (colour + equipped outfit for that character).</summary>
        public static int LocalCosmetic(int colour) => Outfit.ForLocal(Current, colour, CharacterCatalog.SiblingKeys[((colour % 4) + 4) % 4]);
    }
}
