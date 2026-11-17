using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    public enum ChoreCategory { Sorting, Precision, CarryBalance, Operate, Cleaning, PetCare }
    public enum ChoreDifficulty { Easy = 10, Medium = 20, Hard = 30 }
    public enum ChoreState { Available, InProgress, Completed }

    [Serializable]
    public sealed class ChoreTemplate
    {
        public string Id;
        public string Title;          // English, shown on the in-world chore board
        public RoomId Room;
        public ChoreCategory Category;
        public ChoreDifficulty Difficulty;
        public string PropKind;       // which props
        public string AnchorKind;     // where they go
        public int MinSteps = 1, MaxSteps = 1;
        public float NoiseRisk;       // 0..1 hint for bots and the board ("fragile!")
        public float WorkSecondsPerStep = 4f;
        public bool CanBeBonus = true;
        public string Hint;           // short contextual tip near the object
    }

    [Serializable]
    public sealed class ChoreStep
    {
        public int PropId;
        public int AnchorId;
        public bool Done;
        public int CreditTo = ActorIds.None;    // who currently owns the credit for this step
        public int OriginalCredit = ActorIds.None;
        public int UndoneBy = ActorIds.None;
        public float DoneAt;
    }

    [Serializable]
    public sealed class ChoreInstance
    {
        public int Id;
        public ChoreTemplate Template;
        public readonly List<ChoreStep> Steps = new List<ChoreStep>();
        public ChoreState State;
        /// <summary>V5: a PERSONAL DUTY owned by one sibling (field name kept for save/test compatibility).</summary>
        public bool IsBonus;
        public int BonusOwner = ActorIds.None;
        public bool IsPersonal => IsBonus;
        public int Owner => BonusOwner;
        public int Points => IsBonus ? ChoreBoard.PersonalPointsPerStep * Steps.Count : (int)Template.Difficulty / 2;
        public int DoneSteps { get { int n = 0; foreach (var s in Steps) if (s.Done) n++; return n; } }
        public float Progress => Steps.Count == 0 ? 0f : (float)DoneSteps / Steps.Count;
        public string Title => Template.Title;
        public ChoreStep StepForProp(int propId) { foreach (var s in Steps) if (s.PropId == propId) return s; return null; }
    }

    /// <summary>A rival removed an item from somebody's personal duty. Foiled if the owner restores it in time.</summary>
    [Serializable]
    public sealed class SabotageRecord
    {
        public int Id, Saboteur, Victim, PropId, ChoreId;
        public float Time;
        public bool Foiled, Expired;
        public float FoiledAt;
    }

    public static class ChoreLibrary
    {
        public static readonly List<ChoreTemplate> All = new List<ChoreTemplate>
        {
            // Living room
            T("lr_books",   "Shelve the books",          RoomId.LivingRoom, ChoreCategory.Sorting,   ChoreDifficulty.Medium, "book", "shelf_book", 3, 5, 0.25f, 4f, "Slide books in gently — spines out."),
            T("lr_toys",    "Toys back in the toy box",  RoomId.LivingRoom, ChoreCategory.Sorting,   ChoreDifficulty.Easy,   "toy_brick", "toybox_slot", 3, 4, 0.45f, 3f, "Bricks rattle! Lower them in."),
            T("lr_cars",    "Park the toy cars",         RoomId.LivingRoom, ChoreCategory.Sorting,   ChoreDifficulty.Easy,   "toy_car", "toybox_slot", 2, 2, 0.4f, 3f, null),
            T("lr_cushion", "Fluff the sofa cushions",   RoomId.LivingRoom, ChoreCategory.Precision, ChoreDifficulty.Easy,   "cushion", "sofa_cushion", 2, 3, 0.02f, 3f, "Cushions are silent — easy points."),
            T("lr_remote",  "Return the TV remote",      RoomId.LivingRoom, ChoreCategory.Precision, ChoreDifficulty.Easy,   "remote", "tv_remote_spot", 1, 1, 0.3f, 3f, null),
            T("lr_cups",    "Collect the cups",          RoomId.LivingRoom, ChoreCategory.CarryBalance, ChoreDifficulty.Hard, "cup", "cup_tray", 2, 3, 0.8f, 5f, "Ceramic! Place, don't drop."),
            T("lr_shoes",   "Line up the shoes",         RoomId.LivingRoom, ChoreCategory.Sorting,   ChoreDifficulty.Easy,   "shoe", "shoe_rack_slot", 2, 4, 0.1f, 3f, null),
            T("lr_mags",    "Stack the magazines",       RoomId.LivingRoom, ChoreCategory.Precision, ChoreDifficulty.Easy,   "magazine", "magazine_stack", 2, 2, 0.1f, 3f, null),
            T("lr_dogtoy",  "Squeaky toy to the dog bed",RoomId.LivingRoom, ChoreCategory.PetCare,   ChoreDifficulty.Medium, "dog_toy", "dog_bed_toy", 1, 1, 0.5f, 4f, "It squeaks if you squeeze it!"),
            T("lr_frame",   "Hang the picture frame",    RoomId.LivingRoom, ChoreCategory.Precision, ChoreDifficulty.Hard,   "picture_frame", "frame_hook", 1, 1, 0.85f, 6f, "Glass frame — very loud if dropped."),
            T("lr_vase",    "Put the vase back",         RoomId.LivingRoom, ChoreCategory.Precision, ChoreDifficulty.Hard,   "vase", "vase_spot", 1, 1, 0.9f, 5f, null),
            // Kitchen
            T("k_plates",   "Rack the plates",           RoomId.Kitchen, ChoreCategory.CarryBalance, ChoreDifficulty.Hard,   "plate", "plate_rack", 3, 4, 0.85f, 5f, "Plates clatter — one at a time."),
            T("k_mugs",     "Hang the mugs",             RoomId.Kitchen, ChoreCategory.Precision,    ChoreDifficulty.Medium, "mug", "mug_hook", 2, 4, 0.75f, 4f, null),
            T("k_cutlery",  "Sort the cutlery",          RoomId.Kitchen, ChoreCategory.Sorting,      ChoreDifficulty.Medium, "fork", "cutlery_tray", 2, 3, 0.6f, 3f, "Forks left, spoons right."),
            T("k_spoons",   "Put the spoons away",       RoomId.Kitchen, ChoreCategory.Sorting,      ChoreDifficulty.Easy,   "spoon", "cutlery_tray", 2, 2, 0.6f, 3f, null),
            T("k_pan",      "Pan back on the stove",     RoomId.Kitchen, ChoreCategory.CarryBalance, ChoreDifficulty.Hard,   "pan", "stove_burner", 1, 1, 0.95f, 6f, "Heavy metal pan. Do. Not. Drop."),
            T("k_pot",      "Shelve the pot",            RoomId.Kitchen, ChoreCategory.CarryBalance, ChoreDifficulty.Medium, "pot", "pot_shelf", 1, 1, 0.9f, 5f, null),
            T("k_bowls",    "Set out the pet bowls",     RoomId.Kitchen, ChoreCategory.PetCare,      ChoreDifficulty.Medium, "pet_bowl", "pet_mat_slot", 2, 2, 0.6f, 4f, null),
            T("k_sponge",   "Sponge back in the sink",   RoomId.Kitchen, ChoreCategory.Cleaning,     ChoreDifficulty.Easy,   "sponge", "sink_spot", 1, 1, 0.0f, 3f, null),
            T("k_cattoy",   "Cat toy to the basket",     RoomId.Kitchen, ChoreCategory.PetCare,      ChoreDifficulty.Easy,   "cat_toy", "toy_basket", 1, 1, 0.3f, 3f, null),
            T("k_bottle",   "Bottle on the counter",     RoomId.Kitchen, ChoreCategory.Precision,    ChoreDifficulty.Medium, "bottle", "bottle_spot", 1, 1, 0.85f, 4f, null),
            T("k_bread",    "Bread in the bread bin",    RoomId.Kitchen, ChoreCategory.Sorting,      ChoreDifficulty.Easy,   "bread", "bread_bin", 1, 1, 0.0f, 3f, null),
            // Laundry
            T("l_towels",   "Hang the towels",           RoomId.Laundry, ChoreCategory.Cleaning,     ChoreDifficulty.Medium, "towel", "towel_rack_bar", 2, 4, 0.05f, 4f, null),
            T("l_socks",    "Pair up the socks",         RoomId.Laundry, ChoreCategory.Sorting,      ChoreDifficulty.Easy,   "sock", "sock_bin", 2, 4, 0.02f, 3f, null),
            T("l_washer",   "Load the washer",           RoomId.Laundry, ChoreCategory.Operate,      ChoreDifficulty.Medium, "shirt", "washer_drum", 2, 3, 0.4f, 4f, "Close the washer door slowly!"),
            T("l_basket",   "Carry the laundry basket",  RoomId.Laundry, ChoreCategory.CarryBalance, ChoreDifficulty.Hard,   "laundry_basket", "basket_spot", 1, 1, 0.5f, 6f, null),
            T("l_soap",     "Detergent on the shelf",    RoomId.Laundry, ChoreCategory.Precision,    ChoreDifficulty.Medium, "detergent", "detergent_shelf", 1, 1, 0.7f, 4f, null),
            T("l_shoes",    "Sneakers on the mat",       RoomId.Laundry, ChoreCategory.Sorting,      ChoreDifficulty.Easy,   "sneaker", "shoe_mat_slot", 2, 2, 0.1f, 3f, null),
            T("l_iron",     "Put the iron away",         RoomId.Laundry, ChoreCategory.Precision,    ChoreDifficulty.Hard,   "iron", "iron_spot", 1, 1, 0.9f, 5f, "Heavy and clanky."),
            T("l_dogtoy",   "Dog toy to the dog bed",    RoomId.Laundry, ChoreCategory.PetCare,      ChoreDifficulty.Easy,   "dog_toy", "dog_bed_toy", 1, 1, 0.4f, 3f, null),
            // Practice
            T("p_book",     "Shelve the book",           RoomId.Practice, ChoreCategory.Sorting,     ChoreDifficulty.Easy,   "book", "shelf_book", 1, 1, 0.2f, 3f, "Pinch to grab, open your hand to let go."),
            T("p_cup",      "Put the cup on the tray",   RoomId.Practice, ChoreCategory.Precision,   ChoreDifficulty.Easy,   "cup", "cup_tray", 1, 1, 0.8f, 3f, "Lower it slowly — fast = loud."),
            T("p_dogtoy",   "Give the dog its toy",      RoomId.Practice, ChoreCategory.PetCare,     ChoreDifficulty.Easy,   "dog_toy", "dog_bed_toy", 1, 1, 0.3f, 3f, null),
        };

        static ChoreTemplate T(string id, string title, RoomId room, ChoreCategory c, ChoreDifficulty d, string prop, string anchor, int min, int max, float risk, float work, string hint)
            => new ChoreTemplate { Id = id, Title = title, Room = room, Category = c, Difficulty = d, PropKind = prop, AnchorKind = anchor, MinSteps = min, MaxSteps = max, NoiseRisk = risk, WorkSecondsPerStep = work, Hint = hint };
    }

    /// <summary>
    /// V5 "Personal Duties": every active sibling owns ONE personal duty (2-3 colour-tagged items -> their own station);
    /// a few low-value shared house chores add optional points. Rivals may physically undo a duty step (sabotage);
    /// if the owner restores it within FoilWindow the saboteur is "caught" (penalty) and the owner earns a save.
    /// Contribution is split by validated steps; credited once; undo suspends credit; drop/re-place can never farm points.
    /// </summary>
    public sealed class ChoreBoard
    {
        readonly List<ChoreInstance> _chores = new List<ChoreInstance>();
        readonly Dictionary<int, ChoreInstance> _byProp = new Dictionary<int, ChoreInstance>();
        readonly Dictionary<int, int> _anchorOccupant = new Dictionary<int, int>(); // anchorId -> propId
        readonly List<SabotageRecord> _sabotage = new List<SabotageRecord>();
        public const int PersonalPointsPerStep = 15;
        public const float FoilWindow = 20f;
        public IReadOnlyList<SabotageRecord> Sabotage => _sabotage;
        public event Action<SabotageRecord> Sabotaged;
        public event Action<SabotageRecord> SabotageFoiled;
        public RoomDefinition Room { get; private set; }
        public IReadOnlyList<ChoreInstance> Chores => _chores;
        public event Action<ChoreInstance, int> ChoreCompleted;  // (chore, finishing actor)
        public event Action<ChoreInstance> ChoreReopened;
        public event Action<ChoreInstance, ChoreStep, int> StepChanged;

        public void Generate(RoomDefinition room, ulong seed, int sharedCount, IList<int> activeSlots)
        {
            _chores.Clear(); _byProp.Clear(); _anchorOccupant.Clear(); _sabotage.Clear();
            Room = room;
            var rng = new DetRng(seed ^ ((ulong)room.Id * 7919UL));
            var templates = new List<ChoreTemplate>();
            foreach (var t in ChoreLibrary.All) if (t.Room == room.Id) templates.Add(t);
            rng.Shuffle(templates);
            // Make sure we have an easy/medium/hard mix: sort shuffled list by a seeded rotation through difficulties.
            var usedProps = new HashSet<int>(); var usedAnchors = new HashSet<int>();
            int nextId = 1;
            var ordered = new List<ChoreTemplate>();
            var buckets = new[] { ChoreDifficulty.Easy, ChoreDifficulty.Medium, ChoreDifficulty.Hard };
            int bi = rng.Range(0, 3);
            var pool = new List<ChoreTemplate>(templates);
            while (pool.Count > 0)
            {
                int found = -1;
                for (int k = 0; k < 3 && found < 0; k++)
                {
                    var want = buckets[(bi + k) % 3];
                    found = pool.FindIndex(t => t.Difficulty == want);
                }
                if (found < 0) found = 0;
                ordered.Add(pool[found]); pool.RemoveAt(found); bi++;
            }

            // 1) Personal duties first: one per active sibling, distinct templates, 2-3 items each (most points live here).
            var usedTemplates = new HashSet<string>();
            foreach (int slot in activeSlots)
            {
                ChoreInstance duty = null;
                for (int pass = 0; pass < 2 && duty == null; pass++)
                    foreach (var t in ordered)
                    {
                        if (usedTemplates.Contains(t.Id)) continue;
                        duty = TryBuild(t, rng, usedProps, usedAnchors, ref nextId, maxStepsOverride: 3, minStepsOverride: pass == 0 ? 2 : 1);
                        if (duty != null) { usedTemplates.Add(t.Id); break; }
                    }
                if (duty == null) continue;
                duty.IsBonus = true; duty.BonusOwner = slot;
                _chores.Add(duty);
            }
            // 2) Shared house chores from the remaining templates (optional, lower value).
            foreach (var t in ordered)
            {
                if (CountShared() >= sharedCount) break;
                if (usedTemplates.Contains(t.Id)) continue;
                var c = TryBuild(t, rng, usedProps, usedAnchors, ref nextId, maxStepsOverride: 2);
                if (c != null) { _chores.Add(c); usedTemplates.Add(t.Id); }
            }
            // Pre-occupy anchors that were not chosen? No: unused anchors stay free (props may still be placed there harmlessly).
        }

        int CountShared() { int n = 0; foreach (var c in _chores) if (!c.IsBonus) n++; return n; }

        ChoreInstance TryBuild(ChoreTemplate t, DetRng rng, HashSet<int> usedProps, HashSet<int> usedAnchors, ref int nextId, int maxStepsOverride = -1, int minStepsOverride = -1)
        {
            var props = new List<PropSpec>(); var anchors = new List<AnchorSpec>();
            foreach (var p in Room.Props) if (p.Kind == t.PropKind && !usedProps.Contains(p.Id)) props.Add(p);
            foreach (var a in Room.Anchors) if (a.Kind == t.AnchorKind && a.AcceptsPropKind == t.PropKind && !usedAnchors.Contains(a.Id)) anchors.Add(a);
            int max = Math.Min(Math.Min(props.Count, anchors.Count), maxStepsOverride > 0 ? maxStepsOverride : t.MaxSteps);
            int min = minStepsOverride > 0 ? minStepsOverride : (maxStepsOverride > 0 ? 1 : t.MinSteps);
            if (max < min) return null;
            int n = rng.Range(min, max + 1);
            rng.Shuffle(props);
            var c = new ChoreInstance { Id = nextId++, Template = t, State = ChoreState.Available };
            for (int i = 0; i < n; i++)
            {
                c.Steps.Add(new ChoreStep { PropId = props[i].Id, AnchorId = anchors[i].Id });
                usedProps.Add(props[i].Id); usedAnchors.Add(anchors[i].Id);
                _byProp[props[i].Id] = c;
            }
            return c;
        }

        public ChoreInstance ChoreForProp(int propId) => _byProp.TryGetValue(propId, out var c) ? c : null;
        public bool AllSharedComplete { get { bool any = false; foreach (var c in _chores) if (!c.IsBonus) { any = true; if (c.State != ChoreState.Completed) return false; } return any; } }
        public int SharedCompletedCount { get { int n = 0; foreach (var c in _chores) if (!c.IsBonus && c.State == ChoreState.Completed) n++; return n; } }
        public int SharedTotal => CountShared();
        public bool AllPersonalComplete(IList<int> active)
        {
            if (active.Count == 0) return false;
            foreach (int slot in active) { var d = BonusFor(slot); if (d == null || d.State != ChoreState.Completed) return false; }
            return true;
        }

        /// <summary>Any anchor accepting this prop kind (so props of the same kind are interchangeable between slots).</summary>
        public AnchorSpec FindAcceptingAnchor(int propId, Vec3 pos, float slack = 0.05f)
        {
            var c = ChoreForProp(propId); if (c == null) return null;
            foreach (var s in c.Steps)
            {
                var a = Room.Anchor(s.AnchorId);
                if (a != null && Vec3.Distance(a.Position, pos) <= a.Radius + slack) return a;
            }
            return null;
        }

        /// <summary>
        /// Host validated that <paramref name="propId"/> rests inside a valid anchor for its chore (released gently, settled).
        /// Returns true if a step became done.
        /// </summary>
        /// <summary>Why the last <see cref="ReportPlaced"/> returned false (diagnostics / QA logs).</summary>
        public string LastRejectReason = "";

        public bool ReportPlaced(int propId, int anchorId, int actor, float time)
        {
            var c = ChoreForProp(propId);
            if (c == null || !ActorIds.IsSibling(actor)) { LastRejectReason = c == null ? "prop has no chore" : "actor is not a sibling"; return false; }
            if (c.IsBonus && c.BonusOwner != actor) { LastRejectReason = "personal job of another sibling"; return false; } // personal bonus: only its owner can earn it
            // Same-kind props are interchangeable: map this prop to any open step whose anchor is this anchor.
            ChoreStep step = null;
            foreach (var s in c.Steps) if (s.AnchorId == anchorId) { step = s; break; }
            if (step == null) { LastRejectReason = "anchor is not a target of this chore"; return false; }
            if (_anchorOccupant.TryGetValue(anchorId, out int occ) && occ != propId) { LastRejectReason = "anchor occupied by prop " + occ; return false; }
            if (step.Done) { LastRejectReason = "step already done"; return false; }
            // Same-kind props are interchangeable: re-point step bookkeeping to the prop actually used.
            var own = c.StepForProp(propId);
            if (own == null || own.Done && own != step) { LastRejectReason = "prop's own step already done elsewhere"; return false; }
            LastRejectReason = "";
            if (own != step) { int tmp = own.PropId; own.PropId = step.PropId; step.PropId = tmp; }

            // Counterplay: the owner restored an item a rival took -> sabotage foiled.
            if (c.IsBonus && step.UndoneBy != ActorIds.None && step.UndoneBy != actor)
                foreach (var r in _sabotage)
                    if (!r.Foiled && !r.Expired && r.ChoreId == c.Id && r.Saboteur == step.UndoneBy && time - r.Time <= FoilWindow)
                    { r.Foiled = true; r.FoiledAt = time; SabotageFoiled?.Invoke(r); break; }
            step.Done = true; step.DoneAt = time;
            // Anti-farm credit rules: undo+redo by the same actor restores the original owner's credit.
            if (step.OriginalCredit == ActorIds.None) step.OriginalCredit = actor;
            step.CreditTo = (step.UndoneBy != ActorIds.None && step.UndoneBy == actor) ? step.OriginalCredit : actor;
            step.UndoneBy = ActorIds.None;
            _anchorOccupant[anchorId] = propId;
            StepChanged?.Invoke(c, step, actor);
            if (c.State == ChoreState.Available) c.State = ChoreState.InProgress;
            if (c.DoneSteps == c.Steps.Count) { c.State = ChoreState.Completed; ChoreCompleted?.Invoke(c, actor); }
            return true;
        }

        /// <summary>Prop left its anchor (knocked off, taken away). Credit is suspended until restored.</summary>
        public bool ReportRemoved(int propId, int actor, float time)
        {
            var c = ChoreForProp(propId); if (c == null) return false;
            var step = c.StepForProp(propId);
            if (step == null || !step.Done) return false;
            step.Done = false; step.UndoneBy = actor;
            if (c.IsBonus && ActorIds.IsSibling(actor) && actor != c.BonusOwner)
            {
                var rec = new SabotageRecord { Id = _sabotage.Count + 1, Saboteur = actor, Victim = c.BonusOwner, PropId = propId, ChoreId = c.Id, Time = time };
                _sabotage.Add(rec); Sabotaged?.Invoke(rec);
            }
            _anchorOccupant.Remove(step.AnchorId);
            bool wasComplete = c.State == ChoreState.Completed;
            c.State = c.DoneSteps > 0 ? ChoreState.InProgress : ChoreState.Available;
            StepChanged?.Invoke(c, step, actor);
            if (wasComplete) ChoreReopened?.Invoke(c);
            return true;
        }

        /// <summary>Validated shared contribution per sibling = sum over COMPLETED shared chores of points * share of steps.</summary>
        public float SharedContribution(int slot)
        {
            float total = 0f;
            foreach (var c in _chores)
            {
                if (c.IsBonus || c.State != ChoreState.Completed) continue;
                int mine = 0; foreach (var s in c.Steps) if (s.CreditTo == slot) mine++;
                total += c.Points * (float)mine / c.Steps.Count;
            }
            return total;
        }

        /// <summary>In-progress partial credit (used only for inspection tie-breaks and bot reasoning, never for main score).</summary>
        public int ValidatedSteps(int slot)
        {
            int n = 0; foreach (var c in _chores) foreach (var s in c.Steps) if (s.Done && s.CreditTo == slot) n++;
            return n;
        }

        /// <summary>Personal duty points: PersonalPointsPerStep per item currently in place (partial credit counts).</summary>
        public float PersonalContribution(int slot)
        {
            foreach (var c in _chores) if (c.IsBonus && c.BonusOwner == slot) return c.DoneSteps * PersonalPointsPerStep;
            return 0f;
        }

        public int CaughtCount(int slot) { int n = 0; foreach (var r in _sabotage) if (r.Foiled && r.Saboteur == slot) n++; return n; }
        public int SaveCount(int slot) { int n = 0; foreach (var r in _sabotage) if (r.Foiled && r.Victim == slot) n++; return n; }
        public void ExpireSabotage(float now) { foreach (var r in _sabotage) if (!r.Foiled && !r.Expired && now - r.Time > FoilWindow) r.Expired = true; }

        public bool BonusComplete(int slot)
        {
            foreach (var c in _chores) if (c.IsBonus && c.BonusOwner == slot) return c.State == ChoreState.Completed;
            return false;
        }

        public ChoreInstance BonusFor(int slot) { foreach (var c in _chores) if (c.IsBonus && c.BonusOwner == slot) return c; return null; }
    }
}
