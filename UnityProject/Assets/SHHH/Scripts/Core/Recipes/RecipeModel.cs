using System;
using System.Collections.Generic;

namespace SHHH.Core
{
    /// <summary>House areas of the connected home (spec §12). Recipes live in an area; the generator only offers areas the
    /// active house build can actually reach (see <see cref="RecipeGenerator"/>).</summary>
    public enum HouseArea { Living, Kitchen, Laundry, ChildBedroom, Bathroom, Foyer, PetArea, Hallway }

    /// <summary>Primitive physical interaction vocabulary (spec §04.1). Every recipe step is satisfied only by these
    /// operations changing real prop/fixture state; humans issue them through hands/controllers, bots through the motor.</summary>
    public enum Verb
    {
        PickUp, PutDown, PlaceInSocket, Align, Open, Close, Stroke, Fold, Press, Fill, Pair, Attach,
        // sabotage / complication operations (same physical layer, opposite effect)
        Unpair, Detach, Unfold, Smudge, Release,
        // V10 (appended - enum values above are journaled/replicated and must not shift)
        Scrub,
        // V12 (appended): the authority's liquid simulation reports a vessel's amount (Arg = millilitres) so every peer shows the same level
        Pour,
        // V12 (appended): a house mechanism (fridge door, drawer, bin lid) moved by a hand: Slot = mechanism id, Arg = opening 0..100.
        // Handled by the round (not the recipe world), journaled through RecipeAction so replicas and late joiners replay it.
        Mech
    }

    /// <summary>How a placement step judges the slots of its fixture.</summary>
    public enum SlotRule { Any, MatchTag, InOrder }

    /// <summary>Reversible complication kinds (spec §05: undo, clutter, swap, reopen, mess, unpair, detach, tilt).</summary>
    public enum SabotageKind { Remove, Swap, Reopen, Mess, Unpair, Detach, Tilt, Unflag }

    public enum RecipeDifficulty { Easy = 1, Medium = 2, Hard = 3 }

    /// <summary>Declaration of the props a recipe spawns. Kind maps to a Unity prefab; Tag drives sorting/matching
    /// (colour, owner name, size); Order drives sequences.</summary>
    [Serializable]
    public sealed class RecipePropDecl
    {
        public string Kind;
        public string Tag;
        public int Count = 1;
        public NoiseMaterial Material = NoiseMaterial.Soft;
        public string StartSpot = "loose";      // named loose location (evidence-readable: "floor", "behind_cushion", ...)
        public int StartStage;                   // e.g. dirty plate = 0, folded towel = 2
        public bool Ordered;                     // props carry Order 0..Count-1 for InOrder slots
        public bool StartsOriented;
        public string StartIn;                   // fixture kind the props start inside (e.g. laundry already in the washer)
        public int StrokeCap;                    // >0: "washable" - scrubbing (Stroke) only reaches this stage; a Fill AT a running tap goes further (rinse)
    }

    /// <summary>Declaration of a recipe fixture (shelf, chest, rack, rail, drawer, curtain, mirror ...).</summary>
    [Serializable]
    public sealed class RecipeFixtureDecl
    {
        public string Kind;
        public bool HasDoor;                     // lid/door/drawer/panel/curtain: must be open to place or take
        public bool StartsOpen;
        public int StartDecals;                  // dust/crumbs/smudges/sticky spots to stroke away
        public string[] SlotTags = Array.Empty<string>(); // one entry per slot; null/"" = any
        public string[] Flags = Array.Empty<string>();    // binary controls: "pressed", "clipped", "tucked"
    }

    public enum StepKind { Place, OpenState, CloseState, Strokes, Stage, Paired, Attached, Flag }

    /// <summary>One meaningful physical step. Completion is a predicate over world state (never an animation or a button).</summary>
    [Serializable]
    public sealed class RecipeStepDef
    {
        public StepKind Kind;
        public Verb Verb;                        // primary verb (for UI cue + bot mapping)
        public string Label;                     // short clipboard text
        public string PropKind;                  // Place/Stage/Paired/Attached
        public string PartnerKind;               // Attached
        public string Fixture;                   // Place/Open/Close/Strokes/Flag
        public string FlagName;
        public SlotRule Rule;
        public int Count = -1;                   // -1 = all declared props of PropKind
        public bool RequireOriented;
        public int StageTarget;                  // Stage steps
        public int[] After = Array.Empty<int>(); // prerequisite step indices (e.g. close the lid AFTER sorting)
    }

    [Serializable]
    public sealed class SabotageVariant
    {
        public SabotageKind Kind;
        public string Note;                      // designer text + evidence hint
        public string HideSpot = "elsewhere";    // where a removed prop ends up (evidence)
    }

    /// <summary>Full recipe record (spec §04.2). Content is authored in <see cref="RecipeCatalog"/>.</summary>
    [Serializable]
    public sealed class RecipeDef
    {
        public string Id;                        // stable: LV01..HW06
        public int Version = 1;
        public string DisplayName;
        public HouseArea Area;
        public RecipeDifficulty Difficulty;
        public float DurationEstimate = 45f;
        public int Participants = 1;             // 1 = owner (+ optional helpers)
        public float NoiseRisk;                  // 0..1
        public string QuietPath;                 // how to do it quietly
        public string Evidence;                  // witnessable traces
        public List<RecipePropDecl> Props = new List<RecipePropDecl>();
        public List<RecipeFixtureDecl> Fixtures = new List<RecipeFixtureDecl>();
        public List<RecipeStepDef> Steps = new List<RecipeStepDef>();
        public List<SabotageVariant> Sabotage = new List<SabotageVariant>();
        public string Repair;                    // repair description (the physical path is the solver from the sabotaged state)
        public bool HandMode = true, ControllerMode = true;
        public int CooldownRounds = 1;           // generator avoids repeating within N rounds
        public string QaScene = "Core:RecipeHarness";
        public string Provenance = "Master spec §04.4";
        public string[] LegacyTemplateIds = Array.Empty<string>(); // compatibility map to V4–V6 ChoreTemplate ids
    }
}
