using System;
using System.Collections.Generic;
using System.Linq;

namespace SHHH.Core
{
    /// <summary>
    /// The 72 authored household activities of the master spec §04.4 (LV01–LV14, KT01–KT16, LD01–LD10, BR01–BR08, BT01–BT06,
    /// FY01–FY06, PT01–PT06, HW01–HW06). Each one: 2–5 physical steps judged by world state, ≥1 reversible sabotage, a repair path
    /// (the solver from the sabotaged state), evidence, a quiet path and a compatibility link to V4–V6 chore ids where one exists.
    /// Kinds are prefab keys for the Unity layer; tags drive sorting/matching.
    /// </summary>
    public static class RecipeCatalog
    {
        static List<RecipeDef> _all;
        public static IReadOnlyList<RecipeDef> All => _all ?? (_all = Build());
        public static RecipeDef Get(string id) => All.FirstOrDefault(r => r.Id == id);

        sealed class RB
        {
            public readonly RecipeDef D;
            public RB(string id, string name, HouseArea a, RecipeDifficulty d, float risk) { D = new RecipeDef { Id = id, DisplayName = name, Area = a, Difficulty = d, NoiseRisk = risk, DurationEstimate = 25f + 15f * (int)d }; }
            public RB Prop(string kind, NoiseMaterial m, params string[] tags) { foreach (var g in tags.GroupBy(t => t)) D.Props.Add(new RecipePropDecl { Kind = kind, Tag = g.Key, Count = g.Count(), Material = m }); return this; }
            public RB Props(string kind, NoiseMaterial m, int n, string spot = "loose", int stage = 0, bool ordered = false, string startIn = null) { D.Props.Add(new RecipePropDecl { Kind = kind, Count = n, Material = m, StartSpot = spot, StartStage = stage, Ordered = ordered, StartIn = startIn }); return this; }
            public RB Cap(int strokeCap) { D.Props[D.Props.Count - 1].StrokeCap = strokeCap; return this; }
            public RB StageAt(string label, Verb v, string kind, int target, string fixture, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Stage, Verb = v, Label = label, PropKind = kind, StageTarget = target, Fixture = fixture }, after);
            public RB Spot(string spot) { D.Props[D.Props.Count - 1].StartSpot = spot; return this; }
            public RB Fix(string kind, bool door = false, bool open = false, int decals = 0, string[] slots = null, string[] flags = null)
            { D.Fixtures.Add(new RecipeFixtureDecl { Kind = kind, HasDoor = door, StartsOpen = open, StartDecals = decals, SlotTags = slots ?? new string[0], Flags = flags ?? new string[0] }); return this; }
            RB Step(RecipeStepDef s, int[] after) { s.After = after ?? new int[0]; D.Steps.Add(s); return this; }
            public RB Place(string label, string kind, string fix, SlotRule rule = SlotRule.Any, int count = -1, bool oriented = false, params int[] after)
                => Step(new RecipeStepDef { Kind = StepKind.Place, Verb = Verb.PlaceInSocket, Label = label, PropKind = kind, Fixture = fix, Rule = rule, Count = count, RequireOriented = oriented }, after);
            public RB Open(string label, string fix) => Step(new RecipeStepDef { Kind = StepKind.OpenState, Verb = Verb.Open, Label = label, Fixture = fix }, null);
            public RB Close(string label, string fix, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.CloseState, Verb = Verb.Close, Label = label, Fixture = fix }, after);
            public RB Wipe(string label, string fix, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Strokes, Verb = Verb.Stroke, Label = label, Fixture = fix }, after);
            public RB Stage(string label, Verb v, string kind, int target, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Stage, Verb = v, Label = label, PropKind = kind, StageTarget = target }, after);
            public RB Pair(string label, string kind, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Paired, Verb = Verb.Pair, Label = label, PropKind = kind }, after);
            public RB Attach(string label, string kind, string partner, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Attached, Verb = Verb.Attach, Label = label, PropKind = kind, PartnerKind = partner }, after);
            public RB Flag(string label, string fix, string flag, params int[] after) => Step(new RecipeStepDef { Kind = StepKind.Flag, Verb = Verb.Press, Label = label, Fixture = fix, FlagName = flag }, after);
            public RB Sab(SabotageKind k, string note, string hide = "elsewhere") { D.Sabotage.Add(new SabotageVariant { Kind = k, Note = note, HideSpot = hide }); return this; }
            public RB Ev(string evidence, string quiet, string repair) { D.Evidence = evidence; D.QuietPath = quiet; D.Repair = repair; return this; }
            public RB Legacy(params string[] ids) { D.LegacyTemplateIds = ids; return this; }
        }

        static RB R(string id, string name, HouseArea a, RecipeDifficulty d, float risk) => new RB(id, name, a, d, risk);
        const SlotRule Tag = SlotRule.MatchTag, Ord = SlotRule.InOrder;
        static string[] S(params string[] t) => t;
        static string[] Any(int n) => Enumerable.Repeat("", n).ToArray();

        static List<RecipeDef> Build()
        {
            const HouseArea LV = HouseArea.Living, KT = HouseArea.Kitchen, LD = HouseArea.Laundry, BR = HouseArea.ChildBedroom, BT = HouseArea.Bathroom, FY = HouseArea.Foyer, PT = HouseArea.PetArea, HW = HouseArea.Hallway;
            const RecipeDifficulty E = RecipeDifficulty.Easy, M = RecipeDifficulty.Medium, H = RecipeDifficulty.Hard;
            const NoiseMaterial Pa = NoiseMaterial.Paper, Pl = NoiseMaterial.Plastic, To = NoiseMaterial.Toy, Fa = NoiseMaterial.Fabric, So = NoiseMaterial.Soft, Ce = NoiseMaterial.Ceramic, Me = NoiseMaterial.Metal, Wo = NoiseMaterial.Wood;
            var list = new List<RB>
            {
                // ================================================================ LIVING ROOM LV01–LV14
                R("LV01","Shelve bedtime books",LV,M,0.3f).Prop("book",Pa,"moon","star").Fix("bookshelf",slots:S("moon","star"))
                    .Place("First book, spine out, its own slot","book","bookshelf",Tag,1,true).Place("Second book in its slot","book","bookshelf",Tag,2,true,0)
                    .Sab(SabotageKind.Remove,"pull a book and hide it","behind_sofa").Sab(SabotageKind.Swap,"swap the two books")
                    .Ev("relocated spine, empty slot, witness","slide books in slowly","find the displaced book and restore order").Legacy("lr_books","p_book"),
                R("LV02","Stack magazines",LV,E,0.15f).Props("magazine",Pa,2,ordered:true).Fix("coffee_tray",slots:Any(2))
                    .Place("Magazines onto the tray","magazine","coffee_tray").Place("Edges aligned, in stack order","magazine","coffee_tray",Ord,-1,true,0)
                    .Sab(SabotageKind.Remove,"hide the top magazine","under_rug").Sab(SabotageKind.Tilt,"knock the stack askew")
                    .Ev("moved magazine, paper pile","set them down flat","retrieve and rebuild the stack").Legacy("lr_mags"),
                R("LV03","Rescue the TV remote",LV,E,0.3f).Props("remote",Pl,1,"between_cushions").Fix("remote_dock",slots:Any(1))
                    .Place("Remote back to the TV dock","remote","remote_dock").Place("Button side up","remote","remote_dock",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Remove,"tuck it behind a cushion","behind_cushion").Sab(SabotageKind.Tilt,"flip it face down")
                    .Ev("hand contact on the remote, witnessed move","lift, don't drag","search the sofa and re-dock").Legacy("lr_remote"),
                R("LV04","Gather toy blocks",LV,M,0.5f).Prop("toy_block",Wo,"red","red","blue","blue").Fix("toy_chest",door:true,slots:S("red","red","blue","blue"))
                    .Open("Open the toy chest","toy_chest").Place("Blocks by colour","toy_block","toy_chest",Tag).Close("Close the lid","toy_chest",1)
                    .Sab(SabotageKind.Remove,"spill a block","floor").Sab(SabotageKind.Swap,"mix the colours").Sab(SabotageKind.Reopen,"leave the lid open")
                    .Ev("chest lid position, scattered blocks","lower the lid with two fingers","sort again and reseal").Legacy("lr_toys"),
                R("LV05","Park toy cars",LV,E,0.4f).Props("toy_car",To,3,ordered:true).Fix("parking_shelf",slots:Any(3))
                    .Place("Cars nose-forward on the shelf","toy_car","parking_shelf",SlotRule.Any,-1,true).Place("In parking order","toy_car","parking_shelf",Ord,-1,true,0)
                    .Sab(SabotageKind.Remove,"roll one under the armchair","under_armchair").Sab(SabotageKind.Swap,"swap two cars")
                    .Ev("moved car, wheel rattle","carry, don't roll","restore the sequence").Legacy("lr_cars"),
                R("LV06","Match scattered shoes",LV,M,0.2f).Prop("shoe",Fa,"red","red","blue","blue").Fix("shoe_rack",slots:S("red","red","blue","blue"))
                    .Pair("Pair the shoes","shoe").Place("Pairs standing on the rack","shoe","shoe_rack",Tag,-1,false,0)
                    .Sab(SabotageKind.Unpair,"mismatch a pair").Sab(SabotageKind.Remove,"hide one shoe","behind_door")
                    .Ev("distinctive shoe moved","soft shoes are quiet","find the match and re-pair").Legacy("lr_shoes"),
                R("LV07","Rebuild cushion arrangement",LV,E,0.05f).Prop("cushion",So,"left","right").Fix("sofa_corners",slots:S("left","right"))
                    .Place("Cushions onto the sofa","cushion","sofa_corners").Place("Patterns forward, right corners","cushion","sofa_corners",Tag,-1,true,0)
                    .Sab(SabotageKind.Remove,"knock a cushion off","floor").Sab(SabotageKind.Tilt,"turn it backwards")
                    .Ev("cushion contact, witnessed displacement","cushions are silent","reposition").Legacy("lr_cushion"),
                R("LV08","Fold and store the sofa throw",LV,M,0.05f).Props("throw",Fa,1,"floor").Fix("armrest",slots:Any(1))
                    .Stage("Fold the throw (two folds)",Verb.Fold,"throw",2).Place("Lay it on the armrest","throw","armrest",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Mess,"shake it out").Sab(SabotageKind.Remove,"drag it to the floor","floor")
                    .Ev("unfolded throw on the floor","fabric is quiet","refold and store"),
                R("LV09","Clear cups and coasters",LV,H,0.8f).Props("mug",Ce,2).Props("coaster",Wo,2).Fix("tray",slots:Any(2)).Fix("coaster_spots",slots:Any(2))
                    .Place("Mugs onto the tray","mug","tray").Place("Coasters back on the table","coaster","coaster_spots")
                    .Sab(SabotageKind.Remove,"move a mug","side_table")
                    .Ev("ceramic clink, moved cup","lower ceramics slowly - fast is loud","re-clear and rearrange").Legacy("lr_cups"),
                R("LV10","Arrange family board games",LV,M,0.3f).Prop("game_pieces",To,"ludo","quiz").Prop("game_lid",Pa,"ludo","quiz").Prop("game_base",Pa,"ludo","quiz").Fix("game_shelf",slots:S("ludo","quiz"))
                    .Attach("Pieces into their box","game_pieces","game_base").Attach("Lids on the matching boxes","game_lid","game_base",0).Place("Both boxes on the shelf","game_base","game_shelf",Tag,-1,true,1)
                    .Sab(SabotageKind.Detach,"tip the pieces out").Sab(SabotageKind.Remove,"pull a box off the shelf","floor")
                    .Ev("exposed pieces","boxes are light","restore the complete set"),
                R("LV11","Rehang the crooked photo",LV,H,0.6f).Props("photo_frame",Wo,1,"floor").Fix("wall_hook",slots:Any(1))
                    .Place("Lift the frame onto the hook","photo_frame","wall_hook").Place("Straighten it","photo_frame","wall_hook",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Tilt,"tilt the frame").Sab(SabotageKind.Remove,"take it down","floor")
                    .Ev("crooked frame","hold it by both sides","rehang and align").Legacy("lr_frame"),
                R("LV12","Clean up pretend popcorn",LV,E,0.2f).Props("popcorn_cluster",So,3,"rug").Fix("popcorn_bin",slots:Any(3)).Fix("coffee_table",decals:2)
                    .Place("Scoop popcorn into the bin","popcorn_cluster","popcorn_bin").Wipe("Wipe the table","coffee_table")
                    .Sab(SabotageKind.Remove,"knock popcorn out","rug").Sab(SabotageKind.Mess,"sprinkle crumbs")
                    .Ev("kernel cluster, bowl knock","soft and quiet","repeat the safe cleanup"),
                R("LV13","Return the gamepad",LV,E,0.25f).Props("gamepad",Pl,1,"under_side_table").Props("gamepad_strap",Fa,1).Fix("console_station",slots:Any(1))
                    .Attach("Clip the strap on","gamepad_strap","gamepad").Place("Gamepad into its station","gamepad","console_station",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Remove,"slide it under the side table","under_side_table").Sab(SabotageKind.Detach,"unclip the strap")
                    .Ev("gamepad under furniture","set it down gently","recover both parts"),
            };
            list.Add(R("LV14","Quiet the squeaky dog toy",LV,M,0.7f).Props("squeaky_toy",To,1,"rug").Fix("padded_toy_bin",door:true,slots:Any(1))
                    .Open("Open the padded bin","padded_toy_bin").Place("Toy inside, gently","squeaky_toy","padded_toy_bin").Close("Close the lid","padded_toy_bin",1)
                    .Sab(SabotageKind.Remove,"put it back on the rug (trap)","rug").Sab(SabotageKind.Reopen,"open the bin")
                    .Ev("squeak event (setter vs careless user), toy location","pinch it by the tail - squeezing squeaks","silence and store").Legacy("lr_dogtoy"));

            list.AddRange(new[]
            {
                // ================================================================ KITCHEN & DINING KT01–KT16
                // V8 Gate C flagship (2026-11-10): real dishwashing - tap on, sponge each plate (food -> suds), rinse it under the running
                // water (suds -> clean), rack it upright, tap off. Running water is audible; leaving it on is the sabotage.
                R("KT01","Wash and rack plates",KT,H,0.85f).Props("plate",Ce,2,"table").Cap(1).Props("sponge",So,1,"table").Fix("soap_pump").Fix("sink_tap",door:true).Fix("plate_rack",slots:Any(2))
                    .StageAt("Pump soap onto the sponge",Verb.Fill,"sponge",1,"soap_pump")
                    .Stage("Sponge the food off each plate",Verb.Stroke,"plate",1,0)
                    .StageAt("Tap on - rinse the suds off at the sink",Verb.Fill,"plate",2,"sink_tap",1)
                    .Place("Plates upright in the rack","plate","plate_rack",SlotRule.Any,-1,true,2)
                    .Close("Turn the tap off","sink_tap",2)
                    .Sab(SabotageKind.Remove,"take a clean plate out","counter").Sab(SabotageKind.Mess,"dirty a plate").Sab(SabotageKind.Reopen,"leave the tap running")
                    .Ev("plate clatter, running water, wet plate on the counter","one plate at a time, tap on low, lower plates into the rack","wash again if dirtied, rack it, tap off").Legacy("k_plates"),
                R("KT02","Cup cleanup",KT,M,0.75f).Props("cup",Ce,1,"table").Fix("cup_rack",slots:Any(1))
                    .Stage("Rinse the cup",Verb.Stroke,"cup",1).Place("Upright in the rack","cup","cup_rack",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Remove,"hide the cup on the counter","behind_toaster").Sab(SabotageKind.Tilt,"tip it over")
                    .Ev("missing cup","set ceramics down slowly","re-rinse and dock").Legacy("k_mugs"),
                R("KT03","Match lids to pots",KT,M,0.8f).Prop("pot",Me,"small","big").Prop("pot_lid",Me,"small","big").Fix("pot_cupboard",slots:S("small","big"))
                    .Attach("Right lid on each pot","pot_lid","pot").Place("Pots into the cupboard","pot","pot_cupboard",Tag,-1,false,0)
                    .Sab(SabotageKind.Detach,"pop a lid off").Sab(SabotageKind.Swap,"swap the pots")
                    .Ev("metal clang, swapped lids","lids clang - go slow","fix the match").Legacy("k_pot"),
                R("KT04","Return cookware",KT,H,0.95f).Prop("pan",Me,"frying","sauce").Fix("wall_hooks",slots:S("frying","sauce"))
                    .Place("First pan, flat, on its hook","pan","wall_hooks",Tag,1,true).Place("Second pan on its hook","pan","wall_hooks",Tag,2,true,0)
                    .Sab(SabotageKind.Swap,"switch the hooks").Sab(SabotageKind.Remove,"pull a pan down","counter")
                    .Ev("loud drop = evidence","carry by the handle","rehang safely").Legacy("k_pan"),
                R("KT05","Clean the breakfast table",KT,M,0.4f).Props("plate",Ce,2,"dining_table").Props("fruit_bowl",Ce,1,"counter").Fix("dining_table",decals:3).Fix("tray",slots:Any(2)).Fix("table_center",slots:Any(1))
                    .Wipe("Wipe the crumbs","dining_table").Place("Plates onto the tray","plate","tray").Place("Fruit bowl to the centre","fruit_bowl","table_center")
                    .Sab(SabotageKind.Mess,"sprinkle crumbs").Sab(SabotageKind.Remove,"move the bowl","counter")
                    .Ev("crumb decal, moved bowl","wipe, don't bang","wipe and replace"),
                R("KT06","Arrange dining chairs",KT,M,0.5f).Props("dining_chair",Wo,2,"pulled_out").Fix("chair_anchors",slots:Any(2))
                    .Place("Slide the chairs in","dining_chair","chair_anchors").Place("Square them to the table","dining_chair","chair_anchors",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Remove,"pull a chair out","pulled_out").Sab(SabotageKind.Tilt,"knock it crooked")
                    .Ev("chair scrape","lift slightly, don't drag","reposition"),
                R("KT07","Sort cutlery",KT,M,0.6f).Prop("cutlery",Me,"fork","fork","spoon","spoon").Fix("cutlery_drawer",door:true,slots:S("fork","fork","spoon","spoon"))
                    .Open("Open the drawer","cutlery_drawer").Place("Forks left, spoons right","cutlery","cutlery_drawer",Tag).Close("Close the drawer","cutlery_drawer",1)
                    .Sab(SabotageKind.Swap,"mix two compartments").Sab(SabotageKind.Reopen,"leave it open")
                    .Ev("metal jingle, mixed tray","one at a time","resort").Legacy("k_cutlery","k_spoons"),
                R("KT08","Organize pantry jars",KT,M,0.5f).Prop("jar",Pl,"red","yellow","green").Fix("pantry_shelf",slots:S("red","yellow","green"))
                    .Place("Jars on their shelf spots","jar","pantry_shelf",Tag).Place("Labels facing out","jar","pantry_shelf",Tag,-1,true,0)
                    .Sab(SabotageKind.Swap,"swap two jars").Sab(SabotageKind.Tilt,"turn a label away")
                    .Ev("shuffled jars","plastic jars are quiet","reorder").Legacy("k_bottle"),
                R("KT09","Fill the fruit bowl",KT,E,0.2f).Prop("apple",So,"good","good","good","bruised").Fix("fruit_bowl",slots:S("good","good","good")).Fix("compost_caddy",slots:S("bruised"))
                    .Place("Three good apples in the bowl","apple","fruit_bowl",Tag,3).Place("Bruised one to the caddy","apple","compost_caddy",Tag,1,false,0)
                    .Sab(SabotageKind.Remove,"put an apple on the windowsill","windowsill")
                    .Ev("apple out of place","fruit is soft","find and refill").Legacy("k_bread"),
                R("KT10","Make a pretend sandwich",KT,M,0.1f).Props("bread_bottom",So,1).Props("cheese",So,1).Props("lettuce",So,1).Props("bread_top",So,1).Fix("sandwich_plate",slots:Any(1))
                    .Attach("Cheese on the bread","cheese","bread_bottom").Attach("Lettuce on","lettuce","bread_bottom",0).Attach("Close the sandwich","bread_top","bread_bottom",1).Place("Onto the plate","bread_bottom","sandwich_plate",SlotRule.Any,-1,false,2)
                    .Sab(SabotageKind.Detach,"pull out a layer")
                    .Ev("missing layer","toy food is silent","reconstruct"),
                R("KT11","Organize recipe cards",KT,E,0.05f).Props("recipe_card",Pa,3,ordered:true).Fix("recipe_board",slots:Any(3),flags:S("clipped"))
                    .Place("Cards in number order","recipe_card","recipe_board",Ord).Flag("Clip them in","recipe_board","clipped",0)
                    .Sab(SabotageKind.Swap,"swap two cards").Sab(SabotageKind.Unflag,"unclip")
                    .Ev("out-of-order cards","paper is silent","reorder and clip"),
                R("KT12","Refill the cereal jar",KT,M,0.3f).Props("cereal_jar",Pl,1,"counter").Props("jar_lid",Pl,1).Fix("cereal_shelf",slots:Any(1))
                    .Stage("Pour to the fill mark",Verb.Fill,"cereal_jar",3).Attach("Seal the lid","jar_lid","cereal_jar",0).Place("Jar back on the shelf","cereal_jar","cereal_shelf",SlotRule.Any,-1,false,1)
                    .Sab(SabotageKind.Detach,"open the lid").Sab(SabotageKind.Remove,"move the jar","counter")
                    .Ev("open jar","pour slowly (assisted)","recap and return"),
                R("KT13","Wipe the sticky counter",KT,E,0.1f).Props("cloth",Fa,1,"sink").Fix("counter",decals:3).Fix("cloth_hook",slots:Any(1))
                    .Wipe("Three guided wipes","counter").Stage("Rinse the cloth",Verb.Stroke,"cloth",1,0).Place("Hang the cloth","cloth","cloth_hook",SlotRule.Any,-1,false,1)
                    .Sab(SabotageKind.Mess,"add a sticky spot")
                    .Ev("sticky decal","wiping is silent","repeat the strokes").Legacy("k_sponge"),
                R("KT14","Hang kitchen towels",KT,E,0.05f).Prop("kitchen_towel",Fa,"red_stripe","blue_stripe").Fix("towel_rail",slots:S("red_stripe","blue_stripe"))
                    .Place("Towels on their rails","kitchen_towel","towel_rail",Tag).Place("Stripes straight","kitchen_towel","towel_rail",Tag,-1,true,0)
                    .Sab(SabotageKind.Swap,"swap towels").Sab(SabotageKind.Remove,"pull one off","floor")
                    .Ev("swapped towel","fabric is quiet","rehang"),
                R("KT15","Arrange breakfast dishes",KT,M,0.5f).Props("placemat",Fa,2).Props("kid_plate",Pl,2).Props("kid_cup",Pl,2).Fix("mat_spots",slots:Any(2)).Fix("plate_spots",slots:Any(2)).Fix("cup_spots",slots:Any(2))
                    .Place("Placemats down","placemat","mat_spots").Place("Plates on the mats","kid_plate","plate_spots",SlotRule.Any,-1,false,0).Place("Cups beside","kid_cup","cup_spots",SlotRule.Any,-1,false,1)
                    .Sab(SabotageKind.Remove,"move a place setting","counter")
                    .Ev("missing setting","plastic dishes, still go slow","rebuild"),
                R("KT16","Reset the mixer and toaster station",KT,M,0.4f).Props("mixer_whisk",Pl,1,"counter").Props("toaster_cord",Fa,1).Fix("appliance_station",slots:Any(1),flags:S("slid_in"))
                    .Place("Whisk back on the mixer","mixer_whisk","appliance_station").Stage("Wrap the dummy cord",Verb.Fold,"toaster_cord",1).Flag("Slide appliances to their marks","appliance_station","slid_in",0,1)
                    .Sab(SabotageKind.Remove,"take the whisk","counter").Sab(SabotageKind.Unflag,"pull an appliance out")
                    .Ev("loose accessory","no real electricity","reassemble"),
                // V13 (owner 2026-11-17: "putting the food in the fridge does not exist, so do it ... pizza, burgers, fruits, juice"): the
                // groceries are left on a kitchen table; open the real fridge by its handle, put every item on a shelf, close the door
                // gently (a slammed fridge door is a Mom-audible noise). The fridge is the house's own (HouseAdditions.BuildFridge).
                R("KT17","Put the groceries in the fridge",KT,M,0.35f).Prop("grocery",So,"pizza","burger","juice","apple").Fix("fridge",door:true,slots:Any(4))
                    .Open("Open the fridge","fridge").Place("Food on the shelves","grocery","fridge").Close("Close the fridge gently","fridge",1)
                    .Sab(SabotageKind.Reopen,"leave the fridge door open").Sab(SabotageKind.Remove,"take the pizza back out","floor")
                    .Ev("fridge left open, food on the floor","close the door softly","put the food back and close"),

                // ================================================================ LAUNDRY LD01–LD10
                R("LD01","Sort laundry colours",LD,E,0.05f).Prop("clothes",Fa,"light","light","dark","dark").Fix("basket_light",slots:S("light","light")).Fix("basket_dark",slots:S("dark","dark"))
                    .Place("Lights in the light basket","clothes","basket_light",Tag,2).Place("Darks in the dark basket","clothes","basket_dark",Tag,2)
                    .Sab(SabotageKind.Swap,"mix the baskets").Sab(SabotageKind.Remove,"toss one on the floor","floor")
                    .Ev("wrong colour in a basket","clothes are silent","resort"),
                R("LD02","Pair socks",LD,E,0.02f).Prop("sock",Fa,"stripe","stripe","dots","dots").Fix("sock_drawer",slots:Any(4))
                    .Pair("Match the pairs","sock").Stage("Roll each sock",Verb.Fold,"sock",1,0).Place("Into the drawer","sock","sock_drawer",SlotRule.Any,-1,false,1)
                    .Sab(SabotageKind.Unpair,"separate a pair").Sab(SabotageKind.Remove,"hide a sock","under_dryer")
                    .Ev("odd sock","silent","rematch").Legacy("l_socks"),
                R("LD03","Fold towels",LD,M,0.05f).Props("bath_towel",Fa,2,"basket").Fix("towel_pile",slots:Any(2))
                    .Stage("Two folds each",Verb.Fold,"bath_towel",2).Place("Stack the pile","bath_towel","towel_pile",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Mess,"unfold the top towel").Sab(SabotageKind.Remove,"scatter a towel","floor")
                    .Ev("unfolded towel","silent","refold").Legacy("l_towels"),
                R("LD04","Load the washing machine",LD,M,0.4f).Props("laundry_bundle",Fa,2,"basket").Fix("washer",door:true,slots:Any(2),flags:S("start"))
                    .Open("Open the washer door","washer").Place("Bundles in","laundry_bundle","washer").Close("Close the door","washer",1).Flag("Press start","washer","start",2)
                    .Sab(SabotageKind.Reopen,"reopen the door").Sab(SabotageKind.Remove,"take a bundle out","floor").Sab(SabotageKind.Unflag,"press stop")
                    .Ev("door slam risk, missing bundle","close the door slowly","restore the load and close").Legacy("l_washer"),
                R("LD05","Washer to dryer",LD,H,0.45f).Props("wet_bundle",Fa,2,startIn:"washer2").Fix("washer2",door:true,slots:Any(2)).Fix("dryer",door:true,slots:Any(2))
                    .Open("Open the washer","washer2").Open("Open the dryer","dryer").Place("Bundles into the dryer","wet_bundle","dryer",SlotRule.Any,-1,false,0,1).Close("Shut the dryer","dryer",2).Close("Shut the washer","washer2",2)
                    .Sab(SabotageKind.Remove,"hide a bundle","behind_basket").Sab(SabotageKind.Reopen,"open a door")
                    .Ev("bundle out of place","two quiet door closes","recover and finish"),
                R("LD06","Hang shirts",LD,M,0.1f).Prop("shirt",Fa,"small","large").Prop("hanger",Pl,"small","large").Fix("clothes_rail",slots:Any(2))
                    .Attach("Each shirt on its hanger","shirt","hanger").Place("Hangers on the rail, shoulders straight","hanger","clothes_rail",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Detach,"slip a shirt off").Sab(SabotageKind.Remove,"unhook a hanger","floor")
                    .Ev("shirt on the floor","hangers click softly","rehang"),
                R("LD07","Stack storage baskets",LD,M,0.35f).Prop("storage_bin",Pl,"round","square").Prop("bin_lid",Pl,"round","square").Fix("storage_shelf",slots:S("round","square"))
                    .Attach("Lids on by shape","bin_lid","storage_bin").Place("Bins on the shelf","storage_bin","storage_shelf",Tag,-1,false,0)
                    .Sab(SabotageKind.Detach,"pop a lid").Sab(SabotageKind.Remove,"knock a bin down","floor")
                    .Ev("fallen bin","set bins down flat","pair and restack"),
                R("LD08","Refill the towel cupboard",LD,M,0.1f).Prop("folded_towel",Fa,"blue","blue","yellow").Fix("towel_cupboard",door:true,slots:S("blue","blue","yellow"))
                    .Open("Open the cupboard","towel_cupboard").Place("By colour, folded edge out","folded_towel","towel_cupboard",Tag,-1,true).Close("Close it","towel_cupboard",1)
                    .Sab(SabotageKind.Swap,"mix the colour groups").Sab(SabotageKind.Reopen,"leave it open")
                    .Ev("mixed shelf","silent","resort"),
                R("LD09","Clear the dryer lint",LD,E,0.1f).Props("lint_cluster",So,1,startIn:"lint_panel").Fix("lint_panel",door:true,slots:Any(1)).Fix("lint_bin",slots:Any(1))
                    .Open("Open the lint panel","lint_panel").Place("Lint into the bin","lint_cluster","lint_bin",SlotRule.Any,-1,false,0).Close("Shut the panel","lint_panel",1)
                    .Sab(SabotageKind.Remove,"put the lint back out","dryer_top").Sab(SabotageKind.Reopen,"open the panel")
                    .Ev("lint fluff out","plush lint is silent","clean again"),
                R("LD10","Organize laundry products",LD,E,0.3f).Prop("empty_bottle",Pl,"blue","green","pink").Fix("product_tray",slots:S("blue","green","pink"),flags:S("stowed"))
                    .Place("Bottles into the tray by label","empty_bottle","product_tray",Tag).Flag("Slide the tray under the shelf","product_tray","stowed",0)
                    .Sab(SabotageKind.Swap,"scramble two bottles").Sab(SabotageKind.Unflag,"pull the tray out")
                    .Ev("scrambled bottles","empty bottles thunk - set down","rearrange").Legacy("l_soap"),

                // ================================================================ CHILDREN'S BEDROOMS BR01–BR08
                R("BR01","Make the bed",BR,E,0.05f).Props("pillow",So,1,"floor").Fix("bed",slots:Any(1),flags:S("blanket_pulled","tucked"))
                    .Flag("Pull the blanket up","bed","blanket_pulled").Place("Pillow to the headboard","pillow","bed",SlotRule.Any,-1,true).Flag("Tuck the last corner","bed","tucked",0,1)
                    .Sab(SabotageKind.Remove,"knock the pillow off","floor").Sab(SabotageKind.Unflag,"untuck")
                    .Ev("pillow on the floor","silent","reposition and tuck"),
                R("BR02","Tidy schoolbooks",BR,E,0.15f).Prop("notebook",Pa,"maths","reading","art").Fix("desk_tray",slots:S("maths","reading","art"))
                    .Place("Gather notebooks onto the tray","notebook","desk_tray").Place("Order by subject colour","notebook","desk_tray",Tag,-1,false,0)
                    .Sab(SabotageKind.Swap,"swap two notebooks")
                    .Ev("out-of-order notebooks","paper is quiet","reorder"),
                R("BR03","Hang the backpack",BR,E,0.2f).Props("backpack",Fa,1,"floor").Fix("low_wall_hook",slots:Any(1))
                    .Stage("Zip the flap closed",Verb.Fold,"backpack",1).Place("Hang it on the low hook","backpack","low_wall_hook",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Remove,"take it down","under_bed").Sab(SabotageKind.Mess,"unzip it")
                    .Ev("backpack under the bed","zips are quiet if slow","recover and hang"),
                R("BR04","Put plush toys to bed",BR,E,0.02f).Props("plush",So,2,"floor").Fix("toy_hammock",slots:Any(2))
                    .Place("Plushies into the hammock","plush","toy_hammock").Place("Heads on the pillow side","plush","toy_hammock",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Remove,"pull a plush out","floor").Sab(SabotageKind.Tilt,"flip one upside down")
                    .Ev("plush out of bed","silent","return"),
                R("BR05","Sort pencils and markers",BR,E,0.1f).Prop("marker",Pl,"red","blue","green").Fix("marker_cups",slots:S("red","blue","green"),flags:S("on_desk"))
                    .Place("Markers by colour into the cups","marker","marker_cups",Tag).Flag("Cups onto the desk","marker_cups","on_desk",0)
                    .Sab(SabotageKind.Swap,"mix a cup").Sab(SabotageKind.Unflag,"move the cups")
                    .Ev("wrong colour in a cup","jumbo markers, quiet","resort"),
                R("BR06","Assemble the model rocket",BR,M,0.2f).Props("rocket_base",Pl,1).Props("rocket_top",Pl,1,"shelf").Fix("rocket_pedestal",slots:Any(1))
                    .Attach("Fit the top on the base","rocket_top","rocket_base").Place("Stand it on the pedestal","rocket_base","rocket_pedestal",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Detach,"pull the top off").Sab(SabotageKind.Tilt,"tip the rocket")
                    .Ev("detached top","big toy parts click softly","reconnect"),
                R("BR07","Close the bedtime curtains",BR,E,0.05f).Fix("bedroom_curtains",flags:S("left_closed","right_closed","tieback"))
                    .Flag("Pull the left curtain","bedroom_curtains","left_closed").Flag("Pull the right curtain","bedroom_curtains","right_closed").Flag("Clip the tieback","bedroom_curtains","tieback",0,1)
                    .Sab(SabotageKind.Unflag,"reopen a curtain")
                    .Ev("open curtain, moonlight in","curtains swish softly","close again"),
                R("BR08","Laundry into the hamper",BR,E,0.05f).Props("dirty_clothes",Fa,2,"chair").Fix("hamper",door:true,slots:Any(2))
                    .Open("Open the hamper","hamper").Place("Clothes in","dirty_clothes","hamper").Close("Close the lid","hamper",1)
                    .Sab(SabotageKind.Remove,"pull an item back out","floor").Sab(SabotageKind.Reopen,"open the lid")
                    .Ev("clothes on the floor","silent","replace"),

                // ================================================================ BATHROOM BT01–BT06
                R("BT01","Hang towels neatly",BT,E,0.02f).Props("hand_towel",Fa,1,"sink_edge").Fix("towel_bar",slots:Any(1))
                    .Stage("Fold the towel",Verb.Fold,"hand_towel",1).Place("Straight on the bar","hand_towel","towel_bar",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Tilt,"hang it crooked").Sab(SabotageKind.Remove,"drop it","floor")
                    .Ev("crooked towel","silent","fix it"),
                R("BT02","Return the toothbrushes",BT,E,0.15f).Prop("toothbrush",Pl,"pip","nell").Fix("brush_cups",slots:S("pip","nell"))
                    .Place("Brushes into cups","toothbrush","brush_cups").Place("Each in its name colour","toothbrush","brush_cups",Tag,-1,false,0)
                    .Sab(SabotageKind.Swap,"swap the brushes")
                    .Ev("swapped brushes","plastic taps lightly","correct allocation"),
                R("BT03","Wipe the mirror spots",BT,E,0.02f).Props("mirror_cloth",Fa,1,"sink_edge").Fix("mirror",decals:3).Fix("cloth_holder",slots:Any(1))
                    .Wipe("Wipe the three spots","mirror").Place("Cloth back in its holder","mirror_cloth","cloth_holder",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Mess,"add a smudge")
                    .Ev("smudge on the mirror","silent","wipe"),
                R("BT04","Sort the bath toys",BT,E,0.2f).Props("bath_toy",To,3,"bath_edge").Fix("dry_basket",door:true,slots:Any(3))
                    .Open("Open the basket","dry_basket").Place("Duck and boats in","bath_toy","dry_basket").Close("Close the lid","dry_basket",1)
                    .Sab(SabotageKind.Remove,"spill a toy","bath_mat").Sab(SabotageKind.Reopen,"open the lid")
                    .Ev("toy on the mat","rubber toys squeak if squeezed","store"),
                R("BT05","Arrange toilet paper rolls",BT,E,0.02f).Props("paper_roll",Pa,3,"floor").Fix("roll_shelf",slots:Any(2)).Fix("roll_holder",slots:Any(1))
                    .Place("Two rolls on the shelf","paper_roll","roll_shelf",SlotRule.Any,2).Place("One on the holder","paper_roll","roll_holder",SlotRule.Any,1,false,0)
                    .Sab(SabotageKind.Remove,"take a roll","bathtub")
                    .Ev("missing roll","silent","replace"),
                R("BT06","Clear the sink ledge",BT,E,0.3f).Prop("ledge_item",Pl,"pump","brush_cup").Fix("sink_ledge",decals:1,slots:S("pump","brush_cup"))
                    .Place("Pump and cup to their marks","ledge_item","sink_ledge",Tag).Wipe("Wipe the water spot","sink_ledge",0)
                    .Sab(SabotageKind.Remove,"move the pump","bath_edge").Sab(SabotageKind.Mess,"splash the ledge")
                    .Ev("pump out of place","set it down gently","restore"),

                // ================================================================ FOYER FY01–FY06
                R("FY01","Align the family shoes",FY,E,0.1f).Prop("family_shoe",Fa,"big","big","small","small").Fix("shoe_mat",slots:S("big","big","small","small"))
                    .Pair("Pair them up","family_shoe").Place("Toes aligned on the mat","family_shoe","shoe_mat",Tag,-1,true,0)
                    .Sab(SabotageKind.Swap,"swap a pair").Sab(SabotageKind.Unpair,"split a pair")
                    .Ev("odd shoe out","silent","reorder"),
                R("FY02","Hang the coats",FY,E,0.05f).Prop("coat",Fa,"pip","tess").Fix("coat_hooks",slots:S("pip","tess"))
                    .Place("First coat on its named hook","coat","coat_hooks",Tag,1).Place("Second coat, door swing clear","coat","coat_hooks",Tag,2,true,0)
                    .Sab(SabotageKind.Remove,"pull a coat off","floor")
                    .Ev("coat on the floor","silent","rehang"),
                R("FY03","Organize the umbrellas",FY,E,0.2f).Props("umbrella",Pl,2,"doormat").Fix("umbrella_stand",slots:Any(2))
                    .Place("Handles into the stand","umbrella","umbrella_stand").Place("Badges facing out","umbrella","umbrella_stand",SlotRule.Any,-1,true,0)
                    .Sab(SabotageKind.Remove,"move an umbrella","behind_door").Sab(SabotageKind.Tilt,"turn it")
                    .Ev("umbrella out of place","slide in slowly","replace"),
                R("FY04","Collect the mail",FY,E,0.05f).Prop("envelope",Pa,"bills","letters","ads").Fix("mail_organizer",door:true,open:true,slots:S("bills","letters","ads"))
                    .Place("Envelopes into their slots","envelope","mail_organizer",Tag).Close("Close the organizer","mail_organizer",0)
                    .Sab(SabotageKind.Swap,"reslot two envelopes").Sab(SabotageKind.Reopen,"open it")
                    .Ev("misfiled envelope","silent","resort"),
                R("FY05","Put away the shopping bag",FY,M,0.3f).Prop("grocery",So,"bread","apples").Props("shopping_bag",Fa,1,"foyer_floor").Fix("pantry_basket",slots:Any(2)).Fix("bag_hook",slots:Any(1))
                    .Place("Groceries into the basket","grocery","pantry_basket").Stage("Fold the bag",Verb.Fold,"shopping_bag",1,0).Place("Bag on its hook","shopping_bag","bag_hook",SlotRule.Any,-1,false,1)
                    .Sab(SabotageKind.Remove,"move the groceries","stairs").Sab(SabotageKind.Mess,"unfold the bag")
                    .Ev("groceries out","soft goods are quiet","repack"),
                R("FY06","Reset the welcome mat",FY,E,0.05f).Props("welcome_mat",Fa,1,"skewed").Fix("doorway_markers",slots:Any(1),flags:S("flat"))
                    .Place("Mat edges to the markers","welcome_mat","doorway_markers",SlotRule.Any,-1,true).Flag("Flatten the corners","doorway_markers","flat",0)
                    .Sab(SabotageKind.Tilt,"shift it").Sab(SabotageKind.Unflag,"curl a corner")
                    .Ev("skewed mat","silent","re-centre"),

                // ================================================================ PET AREA PT01–PT06
                R("PT01","Refill Biscuit's bowl",PT,E,0.3f).Props("dog_bowl",Pl,1,"hallway").Fix("dog_bowl_dock",slots:Any(1))
                    .Stage("Scoop food to the mark",Verb.Fill,"dog_bowl",2).Place("Bowl into its dock","dog_bowl","dog_bowl_dock",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Remove,"move the bowl","hallway").Sab(SabotageKind.Mess,"empty it")
                    .Ev("bowl in the hall","scoop slowly","refill and re-dock").Legacy("k_bowls"),
                R("PT02","Refill Mochi's bowl",PT,E,0.25f).Props("cat_bowl",Ce,1).Props("food_can",Me,1,"counter").Fix("cat_dock",slots:Any(1)).Fix("pet_shelf",slots:Any(1))
                    .Stage("Fill the bowl",Verb.Fill,"cat_bowl",1).Place("Bowl to the cat dock","cat_bowl","cat_dock",SlotRule.Any,-1,false,0).Place("Can back on the shelf","food_can","pet_shelf")
                    .Sab(SabotageKind.Remove,"relocate the bowl","laundry")
                    .Ev("bowl moved","the can clanks - go slow","recover"),
                R("PT03","Put away pet toys",PT,E,0.4f).Prop("pet_toy",To,"ball","squeaker").Fix("pet_basket",door:true,open:true,slots:Any(2))
                    .Place("Toys into the basket","pet_toy","pet_basket").Close("Shut the lid","pet_basket",0)
                    .Sab(SabotageKind.Remove,"pull a toy out (pet path!)","hallway").Sab(SabotageKind.Reopen,"open the lid")
                    .Ev("toy in the pet path","the squeaker squeaks if squeezed","replace").Legacy("k_cattoy","l_dogtoy"),
                R("PT04","Wash the pretend bowls",PT,E,0.2f).Props("pet_dish",Pl,2,"floor").Fix("drying_mat",slots:Any(2))
                    .Stage("Sponge each dish",Verb.Stroke,"pet_dish",1).Place("Onto the drying mat","pet_dish","drying_mat",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Mess,"dirty a dish").Sab(SabotageKind.Remove,"move a dish","floor")
                    .Ev("dirty dish","plastic is quiet","rewash and dock"),
                R("PT05","Fluff the pet beds",PT,E,0.02f).Props("pet_blanket",Fa,1,"floor").Props("pet_cushion",So,1,"floor").Fix("pet_bed",slots:Any(1),flags:S("clear_of_walkway"))
                    .Stage("Fold the blanket",Verb.Fold,"pet_blanket",1).Place("Cushion into the bed","pet_cushion","pet_bed").Flag("Bed away from the walkway","pet_bed","clear_of_walkway",1)
                    .Sab(SabotageKind.Remove,"move the cushion","hallway").Sab(SabotageKind.Unflag,"shove the bed into the path")
                    .Ev("bed in the walkway","silent","reset"),
                R("PT06","Match collar charms",PT,M,0.15f).Prop("collar_charm",Me,"biscuit","mochi").Prop("collar",Fa,"biscuit","mochi").Fix("collar_hooks",slots:S("biscuit","mochi"))
                    .Attach("Clip each charm to its collar","collar_charm","collar").Place("Collars on the name hooks","collar","collar_hooks",Tag,-1,false,0)
                    .Sab(SabotageKind.Swap,"switch the hooks").Sab(SabotageKind.Detach,"unclip a charm")
                    .Ev("swapped collars","charms jingle - be gentle","correct it"),

                // ================================================================ HALLWAYS & STAIRS HW01–HW06
                R("HW01","Collect loose slippers",HW,E,0.05f).Prop("slipper",Fa,"pip","pip","nell","nell").Fix("bedroom_door_mats",slots:S("pip","pip","nell","nell"))
                    .Pair("Pair the slippers","slipper").Place("Outside the right bedroom","slipper","bedroom_door_mats",Tag,-1,false,0)
                    .Sab(SabotageKind.Unpair,"mismatch one").Sab(SabotageKind.Swap,"swap doors")
                    .Ev("odd slipper","silent","restore"),
                R("HW02","Dust the stair banister",HW,E,0.05f).Props("duster",Fa,1,"landing").Fix("banister",decals:3).Fix("duster_hook",slots:Any(1))
                    .Wipe("Three safe strokes from the landing (no leaning)","banister").Place("Stow the duster","duster","duster_hook",SlotRule.Any,-1,false,0)
                    .Sab(SabotageKind.Mess,"dust it again")
                    .Ev("dust on the rail","silent","repeat"),
                R("HW03","Hang hallway drawings",HW,E,0.1f).Prop("drawing",Pa,"rocket","family").Fix("drawing_hooks",slots:S("rocket","family"))
                    .Place("Drawings on their hooks","drawing","drawing_hooks",Tag).Place("Upright and straight","drawing","drawing_hooks",Tag,-1,true,0)
                    .Sab(SabotageKind.Tilt,"tilt one").Sab(SabotageKind.Remove,"take one down","floor")
                    .Ev("crooked drawing","paper is silent","straighten"),
                R("HW04","Organize the wall hooks",HW,E,0.05f).Prop("hook_item",Fa,"rory","tess").Fix("name_hooks",slots:S("rory","tess"))
                    .Place("Bag and jacket on named hooks","hook_item","name_hooks",Tag).Place("Hang them neatly","hook_item","name_hooks",Tag,-1,true,0)
                    .Sab(SabotageKind.Swap,"swap them")
                    .Ev("swapped hooks","silent","restore"),
                R("HW05","Reposition the floor runner",HW,E,0.05f).Props("floor_runner",Fa,1,"bunched").Fix("runner_guides",slots:Any(1),flags:S("corner_flat"))
                    .Place("Drag the runner onto its guides","floor_runner","runner_guides",SlotRule.Any,-1,true).Flag("Flatten the corner","runner_guides","corner_flat",0)
                    .Sab(SabotageKind.Tilt,"shift a corner").Sab(SabotageKind.Unflag,"flip the corner up")
                    .Ev("bunched runner (trip risk shown, not real)","rugs are silent","straighten"),
                R("HW06","Collect stray hallway toys",HW,M,0.4f).Props("stray_toy",To,3,"hall_floor").Fix("carry_basket",slots:Any(3),flags:S("at_toy_bin"))
                    .Place("Toys into the carry basket","stray_toy","carry_basket").Flag("Carry it to the toy bin","carry_basket","at_toy_bin",0)
                    .Sab(SabotageKind.Remove,"scatter a toy","hall_floor")
                    .Ev("toy in the hall","carry the basket level","recover"),
            });
            return list.Select(b => b.D).ToList();
        }
    }
}
