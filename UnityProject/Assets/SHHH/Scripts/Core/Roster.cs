using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum SlotKind { Bot = 0, Human = 1 }
    public enum BotDifficulty { Rookie = 0, Normal = 1, Clever = 2 }
    public enum SlotStatus { Active = 0, Eliminated = 1, Spectating = 2 }
    public enum EliminationReason { None, CaughtMakingNoise, LeastHelpfulInspection, TieBreakLost, Withdrew, CaughtSabotaging }

    /// <summary>
    /// One of the FOUR competitor seats. Humans and bots share this abstraction; only the controller differs.
    /// Bots are host-simulated game entities, never fake network clients.
    /// </summary>
    [Serializable]
    public sealed class CompetitorSlot
    {
        public int Index;
        public SlotKind Kind;
        /// <summary>Network peer id for humans (Fusion PlayerRef.RawEncoded, or 0 = local offline human). -1 for bots.</summary>
        public int PeerId = -1;
        public string DisplayName;
        public BotDifficulty Difficulty = BotDifficulty.Normal;
        public int CosmeticId;      // outfit/hair palette — never affects gameplay
        public bool Ready;
        public SlotStatus Status = SlotStatus.Active;
        public EliminationReason Reason = EliminationReason.None;
        public int EliminatedInRound = -1;
        /// <summary>True if a human left mid-match and a bot took over this seat (the seat keeps competing).</summary>
        public bool BotTakeover;
        public float DisconnectedAt = -1f;

        public bool IsBot => Kind == SlotKind.Bot;
        public bool IsHuman => Kind == SlotKind.Human;
        public bool IsActive => Status == SlotStatus.Active;
        public override string ToString() => $"#{Index + 1} {DisplayName} [{(IsBot ? "BOT " + Difficulty : "HUMAN")}] {Status}";
    }

    /// <summary>
    /// Exactly four sibling seats. Enforces the v4 slot matrix: 1H+3B (solo, offline), 2H+2B, 3H+1B, 4H+0B.
    /// </summary>
    public sealed class CompetitorRoster
    {
        public const int SlotCount = 4;
        static readonly string[] BotNames = { "Pip", "Nell", "Rory", "Tess", "Milo", "Wren", "Ozzie", "Bea" };
        static readonly string[] DefaultHumanNames = { "Player 1", "Player 2", "Player 3", "Player 4" };

        readonly CompetitorSlot[] _slots = new CompetitorSlot[SlotCount];
        public bool Locked { get; private set; }      // locked once the match starts
        public event Action Changed;

        public CompetitorRoster()
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = MakeBot(i, BotDifficulty.Normal, i);
        }

        public CompetitorSlot this[int i] => _slots[i];
        public IReadOnlyList<CompetitorSlot> Slots => _slots;
        public int HumanCount { get { int n = 0; foreach (var s in _slots) if (s.IsHuman && !s.BotTakeover) n++; return n; } }
        public int BotCount => SlotCount - HumanCount;
        public int ActiveCount { get { int n = 0; foreach (var s in _slots) if (s.IsActive) n++; return n; } }

        static CompetitorSlot MakeBot(int index, BotDifficulty d, int nameIndex) => new CompetitorSlot
        {
            Index = index, Kind = SlotKind.Bot, PeerId = -1, Difficulty = d,
            DisplayName = BotNames[nameIndex % BotNames.Length], CosmeticId = index, Ready = true
        };

        /// <summary>Seat a human (pre-match only). Replaces the lowest-index bot seat. Returns slot index or -1 if full/locked.</summary>
        public int SeatHuman(int peerId, string name, int cosmeticId = -1)
        {
            if (Locked) return -1;
            foreach (var s in _slots) if (s.IsHuman && s.PeerId == peerId) return s.Index; // idempotent
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsBot)
                {
                    _slots[i] = new CompetitorSlot
                    {
                        Index = i, Kind = SlotKind.Human, PeerId = peerId,
                        DisplayName = string.IsNullOrEmpty(name) ? DefaultHumanNames[i] : name,
                        CosmeticId = cosmeticId >= 0 ? cosmeticId : i, Ready = false
                    };
                    Changed?.Invoke();
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Pre-match leave: the seat reverts to a bot so the lobby always shows four seats.</summary>
        public void UnseatHumanPreMatch(int peerId)
        {
            if (Locked) return;
            for (int i = 0; i < SlotCount; i++)
                if (_slots[i].IsHuman && _slots[i].PeerId == peerId)
                {
                    _slots[i] = MakeBot(i, BotDifficulty.Normal, i + 3);
                    Changed?.Invoke();
                }
        }

        public void SetBotDifficulty(BotDifficulty d)
        {
            foreach (var s in _slots) if (s.IsBot) s.Difficulty = d;
            Changed?.Invoke();
        }

        public void SetReady(int peerId, bool ready)
        {
            foreach (var s in _slots) if (s.IsHuman && s.PeerId == peerId) s.Ready = ready;
            Changed?.Invoke();
        }

        public bool AllHumansReady { get { foreach (var s in _slots) if (s.IsHuman && !s.Ready) return false; return true; } }

        public CompetitorSlot FindByPeer(int peerId)
        {
            foreach (var s in _slots) if (s.IsHuman && s.PeerId == peerId) return s;
            return null;
        }

        /// <summary>Validates the v4 matrix (always 4 occupied seats, 1-4 humans, no fake humans).</summary>
        public bool ValidateForStart(out string error)
        {
            int humans = 0;
            var peers = new HashSet<int>();
            foreach (var s in _slots)
            {
                if (s == null) { error = "Empty seat"; return false; }
                if (s.IsHuman)
                {
                    humans++;
                    if (!peers.Add(s.PeerId)) { error = "One human may occupy only one seat"; return false; }
                }
            }
            if (humans < 1) { error = "At least one human is required"; return false; }
            error = null; return true;
        }

        /// <summary>Client replica: the host's roster snapshot is copied in before the replayed Start.</summary>
        public void UnlockForReplay() { Locked = false; }

        public void LockForMatch()
        {
            Locked = true;
            foreach (var s in _slots) { s.Status = SlotStatus.Active; s.Reason = EliminationReason.None; s.EliminatedInRound = -1; }
            Changed?.Invoke();
        }

        /// <summary>Rematch keeps the same party; seats return to Active, humans to not-ready.</summary>
        public void ResetForRematch()
        {
            Locked = false;
            for (int i = 0; i < SlotCount; i++)
            {
                var s = _slots[i];
                if (s.BotTakeover) _slots[i] = MakeBot(i, s.Difficulty, i);
                else { s.Status = SlotStatus.Active; s.Reason = EliminationReason.None; s.EliminatedInRound = -1; s.Ready = s.IsBot; s.DisconnectedAt = -1; }
            }
            Changed?.Invoke();
        }

        public void Eliminate(int index, EliminationReason reason, int round)
        {
            var s = _slots[index];
            s.Status = s.IsHuman && !s.BotTakeover ? SlotStatus.Spectating : SlotStatus.Eliminated;
            s.Reason = reason; s.EliminatedInRound = round;
            Changed?.Invoke();
        }

        /// <summary>Mid-match permanent disconnect: a bot takes over the same seat (no free advancement, no fifth competitor).</summary>
        public void BotTakeOver(int index, BotDifficulty d)
        {
            var s = _slots[index];
            if (!s.IsHuman) return;
            s.Kind = SlotKind.Bot; s.BotTakeover = true; s.Difficulty = d; s.PeerId = -1;
            s.DisplayName = s.DisplayName + " (bot)";
            Changed?.Invoke();
        }

        /// <summary>Reconnect reclaim: a returning peer reclaims their original seat if a bot took it over.</summary>
        public bool TryReclaim(int index, int peerId, string originalName)
        {
            var s = _slots[index];
            if (!s.BotTakeover) return false;
            s.Kind = SlotKind.Human; s.BotTakeover = false; s.PeerId = peerId; s.DisplayName = originalName; s.DisconnectedAt = -1;
            Changed?.Invoke();
            return true;
        }

        public List<int> ActiveIndices()
        {
            var l = new List<int>(4);
            foreach (var s in _slots) if (s.IsActive) l.Add(s.Index);
            return l;
        }
    }
}
