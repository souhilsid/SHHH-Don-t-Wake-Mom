using System;
using System.Collections.Generic;
using System.Linq;
using SHHH.Core;

/// <summary>
/// Spec §04.6 per-recipe automated tests (RCP-<id>): start state, 2–5 real steps with observed intermediate states, completion,
/// owner credit, every sabotage variant reverses it and is recorded, physical repair foils it, illegal sabotage is blocked
/// (spectator, repair protection, cooldown, interference cap), self-farming earns nothing, helper credit is capped, and the
/// state reconstructs from a snapshot. Plus catalog, compatibility-map and generator tests (RCP-CAT, RCP-MAP, RCP-GEN-*).
/// </summary>
static class RecipeTests
{
    public static readonly HashSet<Verb> BotVerbs = new HashSet<Verb>
    { Verb.PickUp, Verb.PutDown, Verb.PlaceInSocket, Verb.Align, Verb.Open, Verb.Close, Verb.Stroke, Verb.Fold, Verb.Press, Verb.Fill, Verb.Pair, Verb.Attach, Verb.Unpair, Verb.Detach, Verb.Scrub };

    public static void Run(Action<string, Action> Test, Action<bool, string> Check)
    {
        // V13: the owner added KT17 "Put the groceries in the fridge" (2026-11-17) - 72 spec recipes + 1
        Test("RCP-CAT 73 unique recipes in 8 areas with the spec distribution (+ V13 KT17)", () =>
        {
            var all = RecipeCatalog.All;
            Check(all.Count == 73, "count " + all.Count);
            Check(all.Select(r => r.Id).Distinct().Count() == 73, "ids unique");
            var want = new Dictionary<HouseArea, int> { [HouseArea.Living] = 14, [HouseArea.Kitchen] = 17, [HouseArea.Laundry] = 10, [HouseArea.ChildBedroom] = 8, [HouseArea.Bathroom] = 6, [HouseArea.Foyer] = 6, [HouseArea.PetArea] = 6, [HouseArea.Hallway] = 6 };
            foreach (var kv in want) Check(all.Count(r => r.Area == kv.Key) == kv.Value, $"{kv.Key}: {all.Count(r => r.Area == kv.Key)} != {kv.Value}");
            // no duplicates masquerading: the (area, verbs, prop kinds, fixture kinds) signature must be unique
            var sig = all.Select(r => r.Area + "|" + string.Join(",", r.Steps.Select(s => s.Kind + ":" + s.PropKind + "@" + s.Fixture)) + "|" + string.Join(",", r.Props.Select(p => p.Kind))).ToList();
            Check(sig.Distinct().Count() == 73, "every recipe has a distinct state goal / action combination");
            foreach (var p in new[] { "LV01", "KT16", "LD10", "BR08", "BT06", "FY06", "PT06", "HW06" }) Check(RecipeCatalog.Get(p) != null, p);
        });

        Test("RCP-MAP every legacy chore id maps to a real ChoreTemplate (no identifier lost)", () =>
        {
            var legacy = ChoreLibrary.All.Select(t => t.Id).ToHashSet();
            foreach (var r in RecipeCatalog.All) foreach (var id in r.LegacyTemplateIds) Check(legacy.Contains(id), $"{r.Id} -> unknown legacy id {id}");
            int mapped = ChoreLibrary.All.Count(t => RecipeCatalog.All.Any(r => r.LegacyTemplateIds.Contains(t.Id)));
            Console.WriteLine($"     {mapped}/{ChoreLibrary.All.Count} legacy templates linked to recipes");
            Check(mapped >= 20, "most legacy templates linked: " + mapped);
        });

        foreach (var def in RecipeCatalog.All)
        {
            var d = def;
            Test($"RCP-{d.Id} {d.DisplayName}: steps/complete/sabotage/repair/illegal/snapshot", () => RecipeLifecycle(d, Check));
        }

        Test("RCP-KT01-WATER dishwashing: rinse needs the running tap, scrub before rinse, scrub stops at suds, leaving the tap on is a sabotage", () =>
        {
            var b = new RecipeBoard(); var k = b.Add(RecipeCatalog.Get("KT01"), 0); var w = b.World;
            int tap = k.Fixture("sink_tap"), plate = k.Props("plate").First();
            Check(w.Fixture(tap).HasDoor && !w.Fixture(tap).Open, "tap starts off");
            string Req(Verb v, int prop = -1, int fix = -1) { w.Time += 0.1f; return b.Request(new ROp { Actor = 0, Verb = v, Prop = prop, Prop2 = -1, Fixture = fix, Slot = -1 }); }
            Check(Req(Verb.PickUp, plate) == null, "pick the plate");
            Check(Req(Verb.Fill, plate, tap) != null, "rinse refused while the tap is off");
            Check(Req(Verb.Open, -1, tap) == null, "tap on");
            Check(Req(Verb.Fill, plate, tap) == "scrub it first", "rinse refused before scrubbing");
            int sponge = k.Props("sponge").First();
            Check(Req(Verb.PickUp, sponge) == null && Req(Verb.Fill, sponge, k.Fixture("soap_pump")) == null, "soap on the sponge");
            Check(b.Request(new ROp { Actor = 0, Verb = Verb.Scrub, Prop = plate, Prop2 = sponge, Fixture = -1, Slot = -1, Arg = w.Prop(plate).Dirt.ToString("X") }) == null && w.Prop(plate).Stage == 1, "soapy sponge scrubs every stain: food -> suds (stage 1)");
            Check(Req(Verb.Stroke, plate) != null && w.Prop(plate).Stage == 1, "scrubbing again does not rinse");
            Check(Req(Verb.PutDown, sponge) == null, "sponge down");
            Check(Req(Verb.Fill, plate, tap) == null && w.Prop(plate).Stage == 2, "rinsed under the running tap (stage 2)");
            var r = RecipeSolver.Solve(b, k, 0);
            Check(r.Complete && !w.Fixture(tap).Open, "solver finishes: plates racked and the tap off - " + r.Failure);
            var sab = k.Def.Sabotage.First(x => x.Kind == SabotageKind.Reopen); w.Time += 5;
            Check(RecipeSolver.ApplySabotage(b, k, sab, 1) == null && w.Fixture(tap).Open && !k.Complete, "a rival turns the tap back on -> recipe undone");
            w.Time += 5; Check(RecipeSolver.Solve(b, k, 0).Complete && !w.Fixture(tap).Open, "repair: tap off again");
        });

        Test("RCP-V10-DIRT plate stains are an 8x8 grid: only a soapy sponge in hand clears the scrubbed cells; suds when the last stain is gone; replicated; a smudge re-dirties", () =>
        {
            var b = new RecipeBoard(); var k = b.Add(RecipeCatalog.Get("KT01"), 0); var w = b.World;
            int plate = k.Props("plate").First(), plate2 = k.Props("plate").Last(), sponge = k.Props("sponge").First(), pump = k.Fixture("soap_pump");
            ulong d0 = w.Prop(plate).Dirt; int n0 = RecipeWorld.CountBits(d0);
            Check(n0 >= 4 && n0 <= 40, "plate starts with " + n0 + " stained cells");
            Check(w.Prop(plate2).Dirt != d0 && w.Prop(plate2).Dirt != 0, "the second plate has its own stains");
            Check(w.Prop(sponge).Dirt == 0, "a sponge has no stain grid");
            string Q(Verb v, int prop = -1, int prop2 = -1, int fix = -1, string arg = null) { w.Time += 0.1f; return b.Request(new ROp { Actor = 0, Verb = v, Prop = prop, Prop2 = prop2, Fixture = fix, Slot = -1, Arg = arg }); }
            Check(Q(Verb.PickUp, plate) == null, "pick up the plate");
            Check(Q(Verb.Stroke, plate) != null && w.Prop(plate).Stage == 0, "a bare rub no longer cleans a stained plate");
            ulong low = d0 & (ulong)(-(long)d0);   // one stained cell
            Check(Q(Verb.Scrub, plate, -1, -1, low.ToString("X")) != null, "no sponge in hand -> refused");
            Check(Q(Verb.PickUp, sponge) == null, "take the sponge");
            Check(Q(Verb.Scrub, plate, sponge, -1, low.ToString("X")) == "the sponge needs soap", "a dry sponge does not cut grease");
            Check(Q(Verb.Fill, sponge, -1, pump) == null && w.Prop(sponge).Stage == 1, "soap dose on the sponge");
            ulong clean = ~d0 & (d0 << 1 | d0 >> 1 | 1UL);
            if (clean != 0) Check(Q(Verb.Scrub, plate, sponge, -1, (clean & ~d0).ToString("X")) == "no stain there", "scrubbing a clean spot does nothing");
            Check(Q(Verb.Scrub, plate, sponge, -1, low.ToString("X")) == null && w.Prop(plate).Dirt == (d0 & ~low) && w.Prop(plate).Stage == 0, "one cell scrubbed off, the rest stays (stage 0)");
            var snap = w.Snapshot(); var b2 = new RecipeBoard(); b2.Add(RecipeCatalog.Get("KT01"), 0); b2.World.Restore(snap);
            Check(b2.World.Prop(plate).Dirt == w.Prop(plate).Dirt, "a joining client sees exactly the same remaining stains");
            Check(Q(Verb.Scrub, plate, sponge, -1, w.Prop(plate).Dirt.ToString("X")) == null && w.Prop(plate).Dirt == 0 && w.Prop(plate).Stage == 1, "last stain gone -> soapy (stage 1)");
            Check(Q(Verb.Scrub, plate, sponge, -1, "1") != null, "nothing left to scrub");
            w.Time += 30f; Check(b.Request(new ROp { Actor = 1, Verb = Verb.Smudge, Prop = plate, Prop2 = -1, Fixture = -1, Slot = -1 }) == null && w.Prop(plate).Stage == 0 && w.Prop(plate).Dirt != 0, "a smudge (sabotage) puts food back on it");
            var r = RecipeSolver.Solve(b, k, 0); Check(r.Complete, "the solver (bots) washes both plates through the same Scrub rule - " + r.Failure);
        });

        Test("RCP-V12-WASHER the washer door locks while it runs, start needs a closed door, loading happens in the house washer (anchored), not on a table", () =>
        {
            var b = new RecipeBoard(); var k = b.Add(RecipeCatalog.Get("LD04"), 0); var w = b.World;
            int washer = k.Fixture("washer"); var bundles = k.Props("laundry_bundle").ToList();
            string Q(Verb v, int prop = -1, int fix = -1, int slot = -1, string arg = null) { w.Time += 0.1f; return b.Request(new ROp { Actor = 0, Verb = v, Prop = prop, Prop2 = -1, Fixture = fix, Slot = slot, Arg = arg }); }
            Check(!w.Fixture(washer).Open, "the washer starts closed");
            Check(Q(Verb.Open, -1, washer, -1, "100") == null && w.Fixture(washer).Open, "open the door (hinge angle reported as level)");
            Check(Q(Verb.Press, -1, washer, -1, "start") == "close the door first", "a washer does not start with its door open");
            for (int i = 0; i < bundles.Count; i++) { Check(Q(Verb.PickUp, bundles[i]) == null, "pick up bundle"); Check(Q(Verb.PlaceInSocket, bundles[i], washer, i) == null, "bundle into the drum"); }
            Check(Q(Verb.Close, -1, washer) == null, "close the door");
            Check(Q(Verb.Press, -1, washer, -1, "start") == null && w.Fixture(washer).Flags.Contains("start"), "start pressed with the door shut");
            Check(Q(Verb.Open, -1, washer, -1, "100") == "the door is locked while it runs", "the door is locked while it runs");
            Check(Q(Verb.Press, -1, washer, -1, "!start") == null && Q(Verb.Open, -1, washer, -1, "40") == null, "stop, then the door opens again");
            var st = RecipeLayout.Stations(RoomId.Laundry)[0];
            var b1 = new RecipeBoard(); var k1 = b1.Add(RecipeCatalog.Get("LD04"), 0);
            Check(RecipeLayout.Fits(k1.Def, st), "LD04 fits a laundry station (only the bundles use the table)");
            var pls = RecipeLayout.Place(b1, k1, st, 0, out var spawn);
            var wp = pls.First(p => p.Kind == "washer");
            Check(wp.Anchored && Math.Abs(wp.Position.x + 0.60f) < 0.01f && Math.Abs(wp.Position.z - 2.20f) < 0.01f && wp.Slots.All(sl => sl.y > 0.3f && sl.y < 0.6f && Math.Abs(sl.x + 0.60f) < 0.16f), "the washer fixture is the house washer; its slots are inside the drum");
            var b5 = new RecipeBoard(); var k5 = b5.Add(RecipeCatalog.Get("LD05"), 0);
            var pl5 = RecipeLayout.Place(b5, k5, st, 0, out _);
            var dp = pl5.First(p => p.Kind == "dryer");
            Check(dp.Anchored && Math.Abs(dp.Position.x - 0.20f) < 0.01f, "LD05's dryer is the house dryer");
            var r = RecipeSolver.Solve(b1, k1, 0); Check(r.Complete, "bots load, close and start the washer through the same rules - " + r.Failure);
        });

        Test("RCP-V12-HOUSEMECH house doors / drawers / lids: a hand reports the opening, the round keeps it (journaled), spectators cannot", () =>
        {
            var roster = new CompetitorRoster(); roster.SeatHuman(0, "Pip", 0); roster.SeatHuman(1, "Rory", 2); roster.LockForMatch();
            var cfg = RoundConfig.PracticeConfig(); cfg.Room = RoomId.Laundry; cfg.RecipeMode = true; cfg.CountdownSeconds = 0.1f;
            var r = new RoundController(cfg, roster, 7); r.Begin(); for (int i = 0; i < 20 && !r.IsPlayable; i++) r.Tick(0.1f);
            ROp M(int actor, int id, string lvl) => new ROp { Actor = actor, Verb = Verb.Mech, Prop = -1, Prop2 = -1, Fixture = -1, Slot = id, Arg = lvl };
            Check(r.RecipeAction(M(0, 12, "80")) == null && r.HouseMech[12] == 80, "fridge door 80 % open");
            Check(r.RecipeAction(M(1, 12, "0")) == null && r.HouseMech[12] == 0, "another sibling shuts it");
            Check(r.RecipeAction(M(0, 12, "0")) == "no change", "same level is not a new event");
            Check(r.RecipeAction(M(1, 12, "60")) == null && r.RecipeAction(M(1, 12, "0:slam")) == null && r.HouseMech[12] == 0, "a client's slam arrives as level 0 + slam flag");
            Check(r.RecipeAction(M(0, 120013, "100")) == null && r.HouseMech[120013] == 100, "a bottle cap (100000 + prop id) comes off for everyone");
            Check(r.RecipeAction(M(0, 12, "130")) == "bad level" && r.RecipeAction(M(0, -1, "10")) == "no such mechanism", "invalid values refused");
            Check(r.RecipeAction(M(ActorIds.Dog, 12, "50")) != null, "a pet or unknown actor cannot report a hand on it (bots in seats can)");
        });

        Test("RCP-V12-PETS pets get hungry with the match clock (2-6 meals), eating real food lowers hunger and credits the feeder; trash in a bin counts once", () =>
        {
            var roster = new CompetitorRoster(); roster.SeatHuman(0, "Pip", 0); roster.SeatHuman(1, "Rory", 2); roster.LockForMatch();
            var cfg = RoundConfig.PracticeConfig(); cfg.Room = RoomId.Kitchen; cfg.RecipeMode = true; cfg.CountdownSeconds = 0.1f; cfg.Duration = 300f;
            var r = new RoundController(cfg, roster, 11); r.Begin(); for (int i = 0; i < 5 && !r.IsPlayable; i++) r.Tick(0.1f);
            Check(r.PetMealsTarget[0] >= 2 && r.PetMealsTarget[0] <= 6 && r.PetMealsTarget[1] >= 2 && r.PetMealsTarget[1] <= 6, $"meals per match {r.PetMealsTarget[0]} / {r.PetMealsTarget[1]}");
            float h0 = r.PetHunger[1]; for (int i = 0; i < 600; i++) r.Tick(0.1f);
            Check(r.PetHunger[1] > h0 + 5f, $"the dog gets hungrier with time ({h0:0} -> {r.PetHunger[1]:0})");
            float before = r.Scores[0].Total; while (r.PetHunger[1] < RoundController.HungryAt) r.Tick(0.5f);
            r.PetEat(ActorIds.Dog, RoundController.GramsPerFullMeal, 0);
            Check(r.PetHunger[1] < 30f && r.PetMeals[1] == 1 && r.Scores[0].Total > before, $"a full bowl eaten: hunger {r.PetHunger[1]:0}, meals {r.PetMeals[1]}, feeder bonus {r.Scores[0].Total - before:0.0}");
            r.Scores.SyncFromBoard(r.Board);
            Check(Math.Abs(r.Scores[0].Care - RoundController.FeedBonus) < 0.01f && r.Scores[0].Total > before, $"the care bonus survives the chore-board score sync (care {r.Scores[0].Care:0.0})");
            Check(r.Room.Props.Count(p => HouseClutter.IsTrash(p.Kind)) >= 4 && r.Room.Props.Any(p => p.Kind == "pet_food") && r.Room.Props.Any(p => p.Kind == "pet_bowl_dog"), "trash, the food bag and the bowls are in the house");
            var r2 = new RoundController(cfg, roster, 11); r2.Begin();
            Check(string.Join(",", r.Room.Props.Where(p => HouseClutter.IsTrash(p.Kind)).Select(p => p.Spawn.x.ToString("0.00"))) == string.Join(",", r2.Room.Props.Where(p => HouseClutter.IsTrash(p.Kind)).Select(p => p.Spawn.x.ToString("0.00"))), "the same seed scatters the same house on every peer");
            r.TrashBinned(1, 777); float s1 = r.Scores[1].Total; r.TrashBinned(1, 777);
            Check(r.Binned.Contains(777) && r.Scores[1].Total == s1, "a piece of trash counts once");
        });

        Test("RCP-V12-LIQUID vessel amounts replicate (Pour reports millilitres); snapshot keeps them; older snapshots read as empty", () =>
        {
            var b = new RecipeBoard(); var k = b.Add(RecipeCatalog.Get("KT01"), 0); var w = b.World;
            int plate = k.Props("plate").First();
            string Q(Verb v, int prop, string arg) { w.Time += 0.1f; return b.Request(new ROp { Actor = 0, Verb = v, Prop = prop, Prop2 = -1, Fixture = -1, Slot = -1, Arg = arg }); }
            Check(Q(Verb.Pour, plate, "250") == null && w.Prop(plate).Ml == 250, "250 ml reported");
            Check(Q(Verb.Pour, plate, "250") == "no change", "same amount is not a new event");
            Check(Q(Verb.Pour, plate, "-5") == "bad amount", "negative refused");
            var snap = w.Snapshot(); var b2 = new RecipeBoard(); b2.Add(RecipeCatalog.Get("KT01"), 0); b2.World.Restore(snap);
            Check(b2.World.Prop(plate).Ml == 250, "a joining client sees the same water level");
            Check(Q(Verb.Pour, plate, "0") == null && w.Prop(plate).Ml == 0, "poured out");
            var old = snap.Replace(",250|", "|"); var b3 = new RecipeBoard(); b3.Add(RecipeCatalog.Get("KT01"), 0); b3.World.Restore(old);
            Check(b3.World.Prop(plate).Ml == 0, "a pre-V12 snapshot restores as empty");
        });

        Test("RCP-V10-WORLD KT01 lives in the real kitchen: rack + pump on the sink counters, sponge at its spot, only the plates on the table; valve level replicates", () =>
        {
            var b = new RecipeBoard(); var k = b.Add(RecipeCatalog.Get("KT01"), 0); var w = b.World;
            var st = RecipeLayout.Stations(RoomId.Kitchen)[0];
            Check(RecipeLayout.Fits(k.Def, st), "KT01 fits the dining-table station (only plates use the table)");
            var pls = RecipeLayout.Place(b, k, st, 0, out var spawn);
            var rack = pls.First(p => p.Kind == "plate_rack"); var pump = pls.First(p => p.Kind == "soap_pump"); var sink = pls.First(p => p.Kind == "sink_tap");
            Check(rack.Anchored && Math.Abs(rack.Position.z - 2.47f) < 0.01f && Math.Abs(rack.Position.y - RecipeLayout.WorktopY) < 0.001f, "rack stands on the counter right of the sink, not on the table");
            Check(pump.Anchored && pump.Position.z > 2.3f, "soap pump stands on the counter left of the sink");
            Check(sink.Anchored, "the sink is the house sink");
            Check(Math.Abs(rack.Width - 0.45f) < 0.001f && rack.Slots.Length == 2 && Math.Abs(Math.Abs(rack.Slots[0].x - rack.Slots[1].x) - 0.10f) < 0.001f, "KIT018 at authored 1:1, slots one rack gap apart (0.10 m)");
            int sponge = k.Props("sponge").First();
            Check(spawn[sponge].z > 2.3f, "sponge starts at its spot by the sink");
            foreach (var p in k.Props("plate")) Check(Math.Abs(spawn[p].z - st.Center.z) < 0.4f && spawn[p].y > 0.7f, "dirty plate starts on the dining table");
            // continuous valve: Open carries the level, Close zeroes it, the snapshot round-trips it
            int tap = k.Fixture("sink_tap");
            Check(b.Request(new ROp { Actor = 0, Verb = Verb.Open, Fixture = tap, Prop = -1, Prop2 = -1, Slot = -1, Arg = "40" }) == null && w.Fixture(tap).Open && w.Fixture(tap).Level == 40, "tap opened to 40 %");
            w.Time += 0.2f; b.Request(new ROp { Actor = 0, Verb = Verb.Open, Fixture = tap, Prop = -1, Prop2 = -1, Slot = -1, Arg = "90" });
            Check(w.Fixture(tap).Level == 90, "lever pushed further: 90 %");
            var snap = w.Snapshot(); var b2 = new RecipeBoard(); b2.Add(RecipeCatalog.Get("KT01"), 0); b2.World.Restore(snap);
            Check(b2.World.Fixture(tap).Level == 90 && b2.World.Fixture(tap).Open, "a joining client gets the valve level");
            w.Time += 0.2f; b.Request(new ROp { Actor = 0, Verb = Verb.Close, Fixture = tap, Prop = -1, Prop2 = -1, Slot = -1 });
            Check(!w.Fixture(tap).Open && w.Fixture(tap).Level == 0, "closed = 0 %");
        });

        Test("RCP-HELP helper credit +5, capped +10 per round, none for a helper who sabotaged it", () =>
        {
            var b = new RecipeBoard();
            var a = b.Add(RecipeCatalog.Get("LV07"), 0); var c = b.Add(RecipeCatalog.Get("LV12"), 0); var e = b.Add(RecipeCatalog.Get("BR04"), 0);
            foreach (var inst in new[] { a, c, e }) { b.World.Time += 1; Check(RecipeSolver.Solve(b, inst, 1).Complete, "helper completes " + inst.Def.Id); }
            Check(b.Help(1) == RecipeBoard.HelpCapPerRound, "help capped at 10: " + b.Help(1));
            Check(b.OwnerProgress(0) >= (a.StepsDone * RecipeBoard.RecipePoints / a.Def.Steps.Count) - 0.01f, "the owner keeps chore progress");
            // a saboteur who then 'helps' fix the same recipe earns no help credit
            var b2 = new RecipeBoard(); var x = b2.Add(RecipeCatalog.Get("LV04"), 0);
            RecipeSolver.Solve(b2, x, 0); b2.World.Time += 1;
            Check(RecipeSolver.ApplySabotage(b2, x, x.Def.Sabotage[0], 2) == null, "sabotage applied");
            b2.World.Time += 1; RecipeSolver.Solve(b2, x, 2);
            Check(x.Complete && b2.Help(2) == 0, "no help credit for fixing your own mess: " + b2.Help(2));
        });

        Test("RCP-PET pet reversal is an incident, never a sabotage blame", () =>
        {
            var b = new RecipeBoard(); var i = b.Add(RecipeCatalog.Get("PT03"), 0);
            RecipeSolver.Solve(b, i, 0); b.World.Time += 5;
            Check(b.Request(new ROp { Actor = ActorIds.Dog, Verb = Verb.Open, Fixture = i.Fixture("pet_basket"), Prop = -1 }) == null, "dog nudges the lid open");
            Check(!i.Complete && b.Sabotage.Count == 0 && b.PetIncidents.Count == 1, "incident only");
        });

        Test("RCP-GEN-1 generator: deterministic, one recipe per seat, built areas only, no shared fixtures, all proven solvable", () =>
        {
            var built = new[] { HouseArea.Living, HouseArea.Kitchen, HouseArea.Laundry };
            var seats = new[] { 0, 1, 2, 3 };
            for (int round = 0; round < 3; round++)
            {
                var g1 = RecipeGenerator.Generate(77, round, seats, built); var g2 = RecipeGenerator.Generate(77, round, seats, built);
                Check(g1.Picks.Count == 4, $"round {round}: {g1.Picks.Count} picks; log: {string.Join(" / ", g1.Log)}");
                Check(g1.Picks.Select(p => p.Recipe.Id).SequenceEqual(g2.Picks.Select(p => p.Recipe.Id)), "same seed -> same picks");
                Check(g1.Picks.All(p => built.Contains(p.Recipe.Area)), "only built areas");
                Check(g1.Picks.Select(p => p.Recipe.Id).Distinct().Count() == 4, "no duplicate recipes");
                var fx = g1.Picks.SelectMany(p => p.Recipe.Fixtures.Select(f => f.Kind)).ToList();
                Check(fx.Distinct().Count() == fx.Count, "no exclusive-fixture conflicts");
                Console.WriteLine($"     round {round} focus [{string.Join(",", g1.Focus)}]: {string.Join(", ", g1.Picks.Select(p => $"seat{p.Seat}={p.Recipe.Id}"))}");
            }
            var other = RecipeGenerator.Generate(78, 0, seats, built);
            var variety = Enumerable.Range(0, 20).Select(s => string.Join(",", RecipeGenerator.Generate((ulong)s, 0, seats, built).Picks.Select(p => p.Recipe.Id))).Distinct().Count();
            Check(variety >= 5, "procedural variety across seeds: " + variety);
        });

        Test("RCP-GEN-2 generator: recently used recipes rest; full house uses every area across rounds", () =>
        {
            var all = Enum.GetValues(typeof(HouseArea)).Cast<HouseArea>().ToArray();
            // V13: over 10 seeds (one fixed seed made the check depend on the catalogue's exact contents)
            var spread = new List<int>();
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var used = new HashSet<string>(); var areas = new HashSet<HouseArea>();
                for (int round = 0; round < 3; round++)
                {
                    var g = RecipeGenerator.Generate(seed, round, new[] { 0, 1, 2, 3 }, all, used);
                    Check(g.Picks.Count == 4, "4 picks");
                    Check(g.Picks.All(p => !used.Contains(p.Recipe.Id)), "no repeats across rounds");
                    foreach (var p in g.Picks) { used.Add(p.Recipe.Id); areas.Add(p.Recipe.Area); }
                }
                spread.Add(areas.Count);
            }
            Check(spread.Count(a => a >= 5) >= 7 && spread.Min() >= 3, "round focus spreads the house: areas per seed " + string.Join(",", spread));
            // reachability: every one of the 72 passes the validator
            var bad = RecipeCatalog.All.Select(r => (r.Id, why: RecipeGenerator.Validate(r))).Where(x => x.why != null).ToList();
            Check(bad.Count == 0, "validator failures: " + string.Join("; ", bad.Select(x => x.Id + " " + x.why)));
        });
    }

    static void RecipeLifecycle(RecipeDef d, Action<bool, string> Check)
    {
        const int owner = 0, rival = 1, spectator = 2;
        // ---- structure
        Check(d.Steps.Count >= 2 && d.Steps.Count <= 5, $"{d.Steps.Count} steps");
        Check(d.Sabotage.Count >= 1, "has sabotage");
        Check(!string.IsNullOrEmpty(d.Evidence) && !string.IsNullOrEmpty(d.QuietPath) && !string.IsNullOrEmpty(d.Repair), "evidence / quiet path / repair documented");

        // ---- start -> intermediate states -> complete (owner)
        var b = new RecipeBoard(); var inst = b.Add(d, owner);
        Check(!inst.Complete && inst.Satisfied.Any(s => !s), "not complete at start");
        var seen = new HashSet<int>(); b.World.Accepted += _ => seen.Add(inst.Satisfied.Count(x => x));
        var sol = RecipeSolver.Solve(b, inst, owner);
        Check(sol.Complete, "owner completes: " + sol.Failure);
        Check(seen.Any(n => n > 0 && n < d.Steps.Count), "intermediate partial states observed (not auto-complete)");
        Check(inst.CreditedTo.All(c => c == owner), "owner credited for every step");
        Check(sol.Verbs.All(BotVerbs.Contains), "bot mapping: every verb used is a bot motor verb: " + string.Join(",", sol.Verbs.Distinct()));
        Check(b.World.Ledger.All(e => e.Validation == "accepted") && b.World.Ledger.Select(e => e.EventId).Distinct().Count() == b.World.Ledger.Count, "ledger: unique accepted events");

        // ---- snapshot reconstruction (join/rejoin)
        var snap = b.Snapshot();
        var b2 = new RecipeBoard(); var inst2 = b2.Add(d, owner); b2.Restore(snap);
        Check(inst2.Complete && b2.Snapshot() == snap, "snapshot reconstructs the completed state");

        // ---- idempotency: replaying an accepted event id is ignored
        var last = b.World.Ledger[b.World.Ledger.Count - 1];
        Check(b.World.Apply(new ROp { EventId = last.EventId, Actor = owner, Verb = Verb.Open, Fixture = 0, Prop = -1 }) != null, "duplicate event id rejected");

        // ---- every sabotage variant: reverses, recorded with evidence, repaired, foiled
        foreach (var v in d.Sabotage)
        {
            var sb = new RecipeBoard(); var si = sb.Add(d, owner);
            Check(RecipeSolver.Solve(sb, si, owner).Complete, "setup");
            sb.World.Time = 10f;
            var err = RecipeSolver.ApplySabotage(sb, si, v, rival);
            Check(err == null, $"sabotage {v.Kind} applies: {err}");
            Check(!si.Complete, $"sabotage {v.Kind} reverses the chore");
            var rec = sb.Sabotage.LastOrDefault();
            Check(rec != null && rec.Saboteur == rival && rec.Victim == owner && !string.IsNullOrEmpty(rec.Evidence), $"sabotage {v.Kind} recorded with evidence");
            sb.World.Time = 15f;
            var rep = RecipeSolver.Solve(sb, si, owner);
            Check(rep.Complete, $"repair after {v.Kind}: {rep.Failure}");
            Check(sb.Sabotage.Any(x => x.Foiled && x.FoiledBy == owner), $"repair foils {v.Kind}");
        }

        // ---- illegal / off-route sabotage is blocked
        {
            var ib = new RecipeBoard(); var ii = ib.Add(d, owner); RecipeSolver.Solve(ib, ii, owner);
            ib.World.CanAct = a => a != spectator;
            var before = ib.Snapshot();
            Check(RecipeSolver.ApplySabotage(ib, ii, d.Sabotage[0], spectator) != null && ib.Snapshot() == before, "spectator cannot touch the world");
            ib.World.Time = 30f; RecipeSolver.ApplySabotage(ib, ii, d.Sabotage[0], rival);
            ib.World.Time = 31f; RecipeSolver.Solve(ib, ii, owner);
            ib.World.Time = 32f; var snapP = ib.Snapshot();
            var blocked = RecipeSolver.ApplySabotage(ib, ii, d.Sabotage[0], rival);
            Check(blocked != null && ii.Complete && ib.RejectedSabotage > 0, "repair protection blocks immediate re-sabotage: " + blocked);
            ib.World.Time = 45f;   // protection over, cooldown (20 s since t=30) still running
            Check(RecipeSolver.ApplySabotage(ib, ii, d.Sabotage[0], rival) != null && ii.Complete, "per-target cooldown blocks spam");
        }

        // ---- self-farming earns nothing: owner undoes and redoes their own step
        {
            var fb = new RecipeBoard(); var fi = fb.Add(d, owner); RecipeSolver.Solve(fb, fi, owner);
            int sabBefore = fb.Sabotage.Count;
            fb.World.Time = 50f;
            var e = RecipeSolver.ApplySabotage(fb, fi, d.Sabotage[0], owner);
            if (e == null)
            {
                RecipeSolver.Solve(fb, fi, owner);
                Check(fb.Sabotage.Count == sabBefore, "owner messing their own chore is not sabotage");
                Check(fb.SelfMessCount.TryGetValue(owner, out var n) && n > 0, "self-redo detected (no farming credit)");
                Check(fi.CreditedTo.All(c => c == owner), "credit unchanged");
            }
        }
    }
}

static class RecipeMatrix
{
    static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'") + "\"";
    public static string Csv(Func<RecipeDef, bool> passed)
    {
        var built = new HashSet<HouseArea> { HouseArea.Living, HouseArea.Kitchen, HouseArea.Laundry };
        var sb = new System.Text.StringBuilder("id,version,area,name,difficulty,steps,step_detail,props,fixtures,noise_risk,quiet_path,sabotage_variants,repair,evidence,bot_verbs,legacy_template_ids,test_case_id,rules_test,unity_physical,quest\n");
        foreach (var r in RecipeCatalog.All)
        {
            var b = new RecipeBoard(); var i = b.Add(r, 0); var sol = RecipeSolver.Solve(b, i, 0);
            sb.Append(string.Join(",", new[] {
                r.Id, r.Version.ToString(), r.Area.ToString(), Q(r.DisplayName), r.Difficulty.ToString(), r.Steps.Count.ToString(),
                Q(string.Join(" | ", r.Steps.Select((s, k) => $"{k + 1}.{s.Verb}:{s.Label}" + (s.After.Length > 0 ? " (after " + string.Join("+", s.After.Select(a => a + 1)) + ")" : "")))),
                Q(string.Join("; ", r.Props.Select(p => $"{p.Count}x {p.Kind}" + (p.Tag != null ? "[" + p.Tag + "]" : "")))),
                Q(string.Join("; ", r.Fixtures.Select(f => f.Kind + (f.HasDoor ? "(door)" : "") + (f.SlotTags.Length > 0 ? $" {f.SlotTags.Length} slots" : "") + (f.StartDecals > 0 ? $" {f.StartDecals} decals" : "")))),
                r.NoiseRisk.ToString("0.00"), Q(r.QuietPath), Q(string.Join("; ", r.Sabotage.Select(s => s.Kind + ": " + s.Note))), Q(r.Repair), Q(r.Evidence),
                Q(string.Join(" ", sol.Verbs.Distinct())), Q(string.Join(" ", r.LegacyTemplateIds)), "RCP-" + r.Id, passed(r) ? "PASS" : "FAIL",
                built.Contains(r.Area) ? "rules engine only - Unity props/fixtures not mapped yet" : "area not built yet (generator excludes it)", "UNVERIFIED" })).Append('\n');
        }
        return sb.ToString();
    }
}

/// <summary>V7 recipe mode inside a real match: generation, layout, round API routing, scoring, sabotage, journal replication.</summary>
static class RecipeModeTests
{
    static Func<ROp, string> RoundExec(MatchController m) => op =>
    {
        var r = m.Round;
        switch (op.Verb)
        {
            case Verb.PickUp: return r.Grab(op.Actor, r.RoomPropOf[op.Prop]) ? null : "grab denied";
            case Verb.PutDown: case Verb.Release: r.Release(op.Actor, r.RoomPropOf[op.Prop], 0.2f, false); return null;
            case Verb.PlaceInSocket:
                // physical order in the game: the hand opens (release), then the slot snaps the item (last holder may place it)
                if (r.Recipes.World.Prop(op.Prop).HeldBy == op.Actor) r.Release(op.Actor, r.RoomPropOf[op.Prop], 0.2f, false);
                return r.RecipeAction(op);
            default: return r.RecipeAction(op);
        }
    };

    public static Func<ROp, string> Exec(MatchController m) => RoundExec(m);

    static MatchController Start(RoomId room, ulong seed, int humans, out MatchController replica, out Action flush)
    {
        var host = new MatchController();
        for (int h = 0; h < humans; h++) { host.Roster.SeatHuman(700 + h, "H" + h); host.Roster.SetReady(700 + h, true); }
        host.Journal = new MatchJournal();
        var rounds = RoundConfig.DefaultSequence(); foreach (var c in rounds) c.RecipeMode = true;
        host.Rounds = rounds.Where(c => c.Room == room).ToArray(); host.Rounds[0].Index = (int)room;
        var rep = new MatchController(); byte[] carry = null; replica = rep;
        flush = () => { var b = host.Journal.TakePending(); if (b != null) MatchJournal.Apply(rep, b, ref carry); };
        if (!host.Start(humans == 1 ? MatchMode.Solo : MatchMode.PrivateOnline, seed, out var err)) throw new Exception(err);
        host.SkipIntro(); host.Tick(0.01f); for (int i = 0; i < 40; i++) host.Tick(0.1f);
        flush();
        return host;
    }

    public static void Run(Action<string, Action> Test, Action<bool, string> Check)
    {
        foreach (var room in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
        {
            var rm = room;
            Test($"REC-MODE-{rm} recipes generated, laid out reachably, completed through the round API, scored and replicated", () =>
            {
                var host = Start(rm, 31, 1, out var replica, out var flush);
                var r = host.Round;
                Check(r != null && r.Phase == RoundPhase.Playing && r.Recipes != null, "recipe round playing");
                var active = host.Roster.ActiveIndices();
                Check(r.Recipes.Instances.Count == active.Count, $"one recipe per active seat: {r.Recipes.Instances.Count}/{active.Count} ({string.Join(" / ", r.RecipePicks.Log)})");
                Check(r.Recipes.Instances.All(i => RecipeLayout.AreasHostedBy(rm).Contains(i.Def.Area)), "recipes of the areas this room hosts");
                Check(r.Board.Chores.Count == 0, "legacy chores replaced");
                // layout: every recipe prop is a real room prop, on a table top >= 0.4 m, no two spawn points within 6 cm
                var all = r.Room.Props.Where(p => r.IsRecipeProp(p.Id)).ToList();
                Check(all.Count == r.Recipes.World.Props.Count, "every recipe prop is a room prop");
                // V12: chore items left around the house (StartHint) start on floors / sofas / tables by design; the station rules
                // below apply to the items that start at their job
                var scattered = all.Where(p => p.StartHint != null).ToList();
                Check(scattered.Count == r.ScatteredProps, $"scatter bookkeeping: {scattered.Count} hinted / {r.ScatteredProps}");
                Check(scattered.All(p => HouseClutter.ScatterCategory(p.Kind) != null), "only left-around things are scattered (tools stay at their job)");
                Check(scattered.All(p => Math.Abs(p.Spawn.x) <= r.Room.Width * 0.5f && Math.Abs(p.Spawn.z) <= r.Room.Depth * 0.5f && p.Spawn.y >= 0.02f && p.Spawn.y < 1.0f), "scattered items lie inside the room, on the floor or a low surface");
                for (int a = 0; a < scattered.Count; a++) for (int b = a + 1; b < scattered.Count; b++)
                    Check(scattered[a].Spawn.FlatDistance(scattered[b].Spawn) >= 0.19f || Math.Abs(scattered[a].Spawn.y - scattered[b].Spawn.y) >= 0.3f, $"scattered items apart: {scattered[a].Kind} / {scattered[b].Kind}");
                var rprops = all.Where(p => p.StartHint == null).ToList();
                Check(rprops.All(p => p.Spawn.y >= 0.4f), "reach rule (items at their job): spawns >= 0.4 m");
                // no two loose props overlap: footprints are rectangles (narrow side along the table, long side toward the player)
                Func<PropSpec, (float x, float z)> ext = p => { float n = RecipePropSize.NarrowOf(p.Kind) * 0.5f, l = RecipePropSize.Of(p.Kind) * 0.5f; return ((int)Math.Round(p.SpawnYaw / 90f)) % 2 == 0 ? (n, l) : (l, n); };
                for (int a = 0; a < rprops.Count; a++) for (int b = a + 1; b < rprops.Count; b++)
                {
                    bool inSlots = r.Recipes.World.Prop(r.RecipePropOf[rprops[a].Id]).IsPlaced || r.Recipes.World.Prop(r.RecipePropOf[rprops[b].Id]).IsPlaced;
                    if (inSlots) continue;
                    var ea = ext(rprops[a]); var eb = ext(rprops[b]); var d = rprops[a].Spawn - rprops[b].Spawn;
                    bool pile = rprops[a].Kind == rprops[b].Kind && RecipePropSize.IsFlat(rprops[a].Kind) && Math.Abs(d.y) >= RecipePropSize.Thickness(rprops[a].Kind) - 0.001f; // a loose pile
                    bool apart = pile || Math.Abs(d.x) >= ea.x + eb.x - 0.001f || Math.Abs(d.z) >= ea.z + eb.z - 0.001f;
                    Check(apart, $"props overlap: {rprops[a].Kind} / {rprops[b].Kind} d=({d.x:0.000},{d.z:0.000})");
                }
                // loose props stay on their station table (inside its rectangle, small overhang allowed)
                foreach (var p in rprops)
                {
                    var st = RecipeLayout.Stations(rm); bool on = false;
                    foreach (var s in st) { float du = s.AlongZ ? p.Spawn.z - s.Center.z : p.Spawn.x - s.Center.x, dt = s.AlongZ ? p.Spawn.x - s.Center.x : p.Spawn.z - s.Center.z; if (Math.Abs(du) <= s.Length * 0.5f && Math.Abs(dt) <= s.Depth * 0.5f + 0.01f && Math.Abs(p.Spawn.y - s.Center.y) < 0.2f) on = true; }
                    // V13: an item that starts AT its job's appliance (the sponge by the sink, wet laundry in the washer) is where it belongs
                    if (!on && r.RecipeFixtures.Any(f => f.Position.FlatDistance(p.Spawn) < 0.7f && Math.Abs(f.Position.y - p.Spawn.y) < 0.6f)) on = true;
                    Check(on, $"{p.Kind} spawn {p.Spawn} is on a station table");
                }
                Check(rprops.All(p => RecipePropSize.Of(p.Kind) <= RecipePropSize.StationMax), "only table-sized items on stations");
                Check(r.RecipeFixtures.Count == r.Recipes.World.Fixtures.Count && r.RecipeFixtures.All(f => f.Position.y >= 0.4f), "fixtures placed");
                // the human (slot 0) completes their recipe through the round API (grab/release/RecipeAction)
                var mine = r.Recipes.Instances.First(i => i.Owner == 0);
                float before = r.Scores[0].Total;
                var sol = RecipeSolver.Solve(r.Recipes, mine, 0, 200, RoundExec(host));
                Check(sol.Complete, $"{mine.Def.Id} completed via the round: {sol.Failure}");
                Check(r.Scores[0].Total > before && r.Scores[0].ValidatedSteps >= mine.Def.Steps.Count, $"scored: {before:0.0} -> {r.Scores[0].Total:0.0}, steps {r.Scores[0].ValidatedSteps}");
                Check(host.Log.Lines.Any(l => l.Contains("RECIPE DONE " + mine.Def.Id)), "log line");
                // a rival bot seat sabotages it through the round API; the owner repairs and foils it
                int rival = active.First(a => a != 0);
                for (int i = 0; i < 20; i++) host.Tick(0.1f);
                var v = mine.Def.Sabotage[0];
                var sab = SabotageVia(host, mine, v, rival);
                Check(sab == null && !mine.Complete, $"{mine.Def.Id} sabotage {v.Kind} via the round: '{sab}' complete={mine.Complete} held={string.Join(",", r.Recipes.World.Props.Where(p => p.HeldBy != ActorIds.None).Select(p => p.Kind + "@" + p.HeldBy))} last={string.Join(" | ", host.Log.Lines.Skip(host.Log.Lines.Count - 3))}");
                Check(host.Log.Lines.Any(l => l.StartsWith("[") && l.Contains("SABOTAGE") && l.Contains(mine.Def.Id)), "sabotage logged");
                for (int i = 0; i < 10; i++) host.Tick(0.1f);
                Check(RecipeSolver.Solve(r.Recipes, mine, 0, 200, RoundExec(host)).Complete, "owner repairs via the round");
                Check(r.Recipes.Sabotage.Any(x => x.Foiled), "foiled");
                // replication: the replica's recipe world is identical
                for (int i = 0; i < 15; i++) host.Tick(0.1f);
                flush();
                Check(replica.Round != null && replica.Round.Recipes != null, "replica built the recipe round");
                Check(replica.Round.Recipes.Snapshot() == r.Recipes.Snapshot(), "replica recipe state identical");
                Check(MatchJournal.Checksum(replica) == MatchJournal.Checksum(host), "checksum equal");
                Check(replica.Log.Lines.SequenceEqual(host.Log.Lines), "replica log identical");
            });
        }

        Test("REC-LAYOUT-POOL each built room has enough table-fit recipes for 4 seats (and reports which are excluded)", () =>
        {
            foreach (var room in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                var st = RecipeLayout.Stations(room); var areas = RecipeLayout.AreasHostedBy(room);
                var inArea = RecipeCatalog.All.Where(d => areas.Contains(d.Area)).ToList();
                var fit = inArea.Where(d => st.Any(s => RecipeLayout.Fits(d, s))).ToList();
                Console.WriteLine($"    {room}: {fit.Count}/{inArea.Count} fit [{string.Join(" ", fit.Select(d => d.Id + ":" + d.Difficulty.ToString()[0] + st.Count(s => RecipeLayout.Fits(d, s))))}]; excluded: {string.Join(", ", inArea.Except(fit).Select(d => d.Id))}");
                Check(fit.Count >= 4, $"{room} has {fit.Count} fitting recipes");
            }
        });

        Test("REC-LAYOUT-SEEDS 40 seeds per built room: 4 recipes generated and each assigned its own table", () =>
        {
            foreach (var room in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
            {
                int ok = 0; var used = new HashSet<string>();
                for (ulong seed = 1; seed <= 40; seed++)
                {
                    var host = Start(room, seed, 1, out _, out _); var r = host.Round;
                    var st = r.RecipeFixtures.Select(f => f.Station).Distinct().Count();
                    if (r.Recipes.Instances.Count == 4 && st == r.Recipes.Instances.Count) ok++;
                    foreach (var i in r.Recipes.Instances) used.Add(i.Def.Id);
                }
                Console.WriteLine($"    {room}: {ok}/40 seeds OK; {used.Count} distinct recipes used");
                Check(ok == 40, $"{room}: {ok}/40");
            }
        });

        Test("REC-BOTS embodied bots (headless bodies) complete their own recipes one solver step at a time, sabotage rivals, repair", () =>
        {
            int done = 0, total = 0, sab = 0, foiled = 0, botOps = 0;
            foreach (var room in new[] { RoomId.LivingRoom, RoomId.Kitchen, RoomId.Laundry })
                for (ulong seed = 61; seed < 64; seed++)
                {
                    var m = new MatchController();
                    m.Roster.SeatHuman(700, "H"); m.Roster.SetReady(700, true);
                    var rounds = RoundConfig.DefaultSequence(); foreach (var c in rounds) { c.RecipeMode = true; c.NoiseMultiplier = 0.2f; }
                    m.Rounds = rounds.Where(c => c.Room == room).ToArray();
                    var world = new HeadlessWorld(seed); m.RoundStarting += r => world.Attach(r);
                    if (!m.Start(MatchMode.Solo, seed, out var err)) throw new Exception(err);
                    m.SkipIntro();
                    for (int i = 0; i < 1500 && m.Phase != MatchPhase.Results; i++) { m.Tick(0.1f); if (m.Phase == MatchPhase.InRound && m.Round != null && m.Round.IsPlayable) world.Step(0.1f); if (m.Round != null && m.Round.Recipes != null && m.Round.Recipes.Instances.All(x => x.Complete)) break; }
                    var r = m.Round; if (r?.Recipes == null) { Check(false, $"{room}/{seed}: no recipe round"); continue; }
                    total += r.Recipes.Instances.Count; done += r.Recipes.Instances.Count(x => x.Complete);
                    sab += r.Recipes.Sabotage.Count; foiled += r.Recipes.Sabotage.Count(x => x.Foiled);
                    botOps += r.Recipes.World.Ledger.Count(e => e.Actor != 0);
                    if (seed == 61) Console.WriteLine($"    {room}: {string.Join(" ", r.Recipes.Instances.Select(x => $"{x.Def.Id}:{x.StepsDone}/{x.Def.Steps.Count}{(x.Complete ? "+" : "")}"))} t={r.Time:0}s sab={r.Recipes.Sabotage.Count}");
                }
            Console.WriteLine($"    bots: {done}/{total} recipes complete, {sab} sabotage actions ({foiled} foiled), {botOps} bot ledger events");
            Check(done >= total * 0.8, $"most recipes completed by embodied bots: {done}/{total}");
            Check(botOps > 0, "bots acted through the recipe world");
        });

        Test("MATCH-01R recipe mode (shipping default): 100 seeded solo matches go 4->3->2->1 across three rooms with embodied bots", () =>
        {
            RoundConfig.RecipeModeDefault = true;
            try
            {
                int botWins = 0, humanWins = 0, earlyDone = 0; var reasons = new Dictionary<EliminationReason, int>();
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    var m = MatchSimulator.RunSolo(seed, (BotDifficulty)(seed % 3), BotProfile.For(BotDifficulty.Normal));
                    Check(m.Phase == MatchPhase.Winner, $"seed {seed} ended in {m.Phase}");
                    Check(m.Summary.EliminationOrder.Count == 3 && m.Roster.ActiveCount == 1, $"seed {seed} elims {m.Summary.EliminationOrder.Count}");
                    var perRound = m.Roster.Slots.Where(s => s.EliminatedInRound >= 0).GroupBy(s => s.EliminatedInRound).Select(g => g.Count());
                    Check(perRound.All(c => c == 1), "one per round");
                    if (m.Summary.WinnerIsBot) botWins++; else humanWins++;
                    foreach (var s in m.Roster.Slots) if (s.Reason != EliminationReason.None) { reasons.TryGetValue(s.Reason, out int c); reasons[s.Reason] = c + 1; }
                    earlyDone += m.Log.Lines.Count(l => l.Contains("AllChoresDone") || l.Contains("all jobs done"));
                }
                Console.WriteLine($"     recipe solo sim: bot wins {botWins}, human(policy) wins {humanWins}; reasons: {string.Join(", ", reasons.Select(kv => kv.Key + "=" + kv.Value))}");
                Check(botWins > 0 && humanWins > 0, "both bots and the human policy can win");
                Check(reasons.Count >= 2, "more than one elimination path occurs");
            }
            finally { RoundConfig.RecipeModeDefault = false; }
        });

        Test("REC-MODE-SPECTATOR an eliminated seat cannot touch recipe props", () =>
        {
            var host = Start(RoomId.LivingRoom, 44, 2, out _, out _);
            var r = host.Round; var victim = r.Recipes.Instances[0];
            int out_ = host.Roster.ActiveIndices().Last();
            host.Roster.Eliminate(out_, EliminationReason.None, -1);
            var pid = r.RoomPropOf[victim.Props(victim.Def.Props[0].Kind)[0]];
            Check(!r.Grab(out_, pid), "spectator grab denied");
            Check(r.RecipeAction(new ROp { Actor = out_, Verb = Verb.Open, Fixture = 0, Prop = -1, Prop2 = -1, Slot = -1 }) != null, "spectator recipe action denied");
        });
    }

    static string SabotageVia(MatchController m, RecipeInstance inst, SabotageVariant v, int actor) => RecipeSolver.ApplySabotage(m.Round.Recipes, inst, v, actor, RoundExec(m));
}
