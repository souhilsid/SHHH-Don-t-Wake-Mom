# SHHH! V10/V11 recovery state — RESUME HERE

**V11 PHASE (2026-11-10 evening)**: owner directive `Docs/Production/SHHH_V11_PHYSICAL_INTERACTION_ENGINE_DIRECTIVE_2026-11-10.md` - finish
dishwashing completely first (see V11 section at the end of this file for its exact state), then water reservoirs/pouring, then the shared
interaction foundation, then the remaining chores.

Directive: `Docs/Production/SHHH_V10_REAL_HOUSE_PHYSICS_ART_INTERACTIONS_DIRECTIVE_2026-11-10.md` (highest priority).
Older resume file (V8/V9 history): `Docs/SHHH_V8_Continuation_State.md`. Evidence: `Docs/SHHH_PHYSICS_QA_EVIDENCE_INDEX.md`.
Nothing below is a headset result: **Quest = UNTESTED** for every V10 item. Latest APK is still c12 (no V8/V9/V10 work).

## Baseline (recovered 2026-11-10 ~14:00 UTC+3)
- PC tree `C:\Users\aisci\Downloads\SHHH Game\SHHH` has **no git**; the cloud mirror is not reachable from PC-local sessions.
- Post-session-1 snapshot: `Backups/V10_preflight/shhh_src_post_v10_session1_20261110.tgz` sha256 `f7eaaf8e9479f6d0c519a1c51a50e995b0d5974260054199d6a1aa64001bf37d`.
- Rollback snapshot taken before any V10 change: `Backups/V10_preflight/shhh_src_pre_v10_20261110.tgz`
  sha256 `e0bec403fa9082d1a3bc4797dd2e7ba5edc86c1da93c92a6df03e14c9b4ae8cf` (Docs, Core, Tools scripts, Assets/SHHH/Scripts+Editor+Genies, ProjectSettings, Packages).
- The V10 zip held only the directive + reference board + 6 chat reference images. **No new model/VFX archives were attached**; packs already on
  disk were used (KayKit Restaurant, Daycare KIT015/018/026). Tiny Treats / laundry / Ultimate House packs are on disk in Downloads, not yet imported.

## What V10 changed (2026-11-10, all IMPLEMENTED IN SOURCE, compiled in Unity 6000.0.62f1)
1. **Durable rules**: CLAUDE.md READ FIRST V10 block, `.claude/rules/shhh-physics-production.md` V10 block, `Docs/SHHH_OWNER_REQUIREMENTS.md` V10-1..8,
   `Docs/SHHH_PHYSICS_CONTRACT.md` §0, this file, asset manifest rows. Directive copied to `Docs/Production/`.
2. **Root cause of "plate 2 never seats / 441 clatters"** (three compounding faults, all fixed at the source, no snapping):
   - The daycare GLB->FBX models were **imported Z-up**: KIT018 dish rack lay on its back since V9 (tray floor a vertical wall reaching 0.287 m),
     KIT015 mixer and KIT026 soap pump were tipped too. `ArticulatedModel` now re-orients any model whose authored FRONT is vertical
     (feet / working parts decide up vs down). Audit `recipe_diag look:Kitchen` -> `model_audit_dc_kit0xx` (KIT018 0.45 x 0.20 x 0.30, tray floor 0.031).
   - Plates had an AABB **box** collider: its corners landed on 3 mm rails. Round flat props (`plate`, `kid_plate`, `pot_lid`, `coaster`) now get a
     16-sided convex **disk** collider (`PropBody.DiskMesh`).
   - The rack was scaled to 0.85 (tray shallower than a plate). Now authored **1:1** (0.45 m, 46 mm clear gaps; divider boxes thickened to the 4 mm wire
     along the across-gap axis found in world space).
   - `RackSeating` replaces the kinematic snap: plates stay dynamic; a capped spring-damper (<= 2.5 m/s^2) only in the last centimetres; "seated" =
     at rest on the tray floor inside a gap within 25 deg of vertical -> `PlaceInSocket`. A plate standing in the rack is never a "precarious trap".
3. **Impact noise policy** (`ImpactGate`): impulse/mass check, per-object-pair cooldown, 0.5 s collision clusters, rattle classification (one quiet voice,
   no Mom event), 8-voice budget. Runs: 441 ceramic sounds (v10g) -> 41 (V10 run 1) -> 21 (run 3); contacts 724 -> 31 noise events.
4. **No central task table for KT01**: real counter run beside the sink (`HouseAdditions`: baked KayKit sink stretched x1.569 to a **0.80 m worktop**,
   basin floor 0.565 m; two KayKit straight counters fitted into the gaps sink-stove and sink-fridge). World anchors (`RecipeStation.WithAnchor`,
   `RecipeAnchor`): dish rack on the right counter, soap pump + sponge spot on the left; only the dirty plates start on the dining table. Fixtures on real
   furniture get no cream base / owner rim / label / pads. Rules test `RCP-V10-WORLD`.
5. **Faucet**: continuous valve. The hand drags the lever tip along its arc between stops (`SinkTapView`), level 0..100 % replicated
   (`RFixture.Level`, Open op Arg, snapshot 5th fixture field, backward compatible). "Tap on/off" and "Pump" poke keys removed.
6. **Toon water**: `SHHH/ToonWater` + `SHHH/ToonFoam` (original shaders, `Assets/SHHH/Resources/Shaders`), `ToonWaterStream` (tube mesh to the real
   first sphere-cast hit, continuity taper, flow-scaled radius/speed, splash crown, pooled droplets, detach + drips on shut-off), finite basin volume
   (inflow from the valve, sqrt-law drain) as a rippling surface. Rinse counts only when the stream's first hit is the plate (or the hand holding it);
   rinse speed scales with flow. Running-water noise scales with flow.
7. **Harness**: the simulated player walks (1.3 m/s, carrying held items) between table, sink and rack; tap operated by dragging the lever; rack lab
   (5 sequential insertions into different gaps with removals, quiet vs 30 cm drop, wrong-angle release + recovery).
8. **PC tooling**: `Tools/core_tests.sh` runs the rules tests with Unity's bundled .NET 6 (no SDK needed). 158/158 pass.

## Phase log
| Phase | State |
|---|---|
| 0 Persist V10 rules | DONE |
| 1 Rack slot 2 + 441-clatter | ROOT CAUSES FIXED; PLAY run 4: KT01 complete 5/5 by simulated hands with walking, both plates seated in different gaps, 31 ceramic sounds (was 441) |
| 2 Remove central task table (KT01) | DONE for KT01 (world anchors + real counters); all other recipes still use station tables -> NOT PRODUCTION COMPLETE |
| 3 Toon water stream + basin volume | IMPLEMENTED + visible in PLAY captures; Quest stereo/perf UNTESTED; side-by-side comparison scene not built yet |
| 4 Faucet continuous valve | IMPLEMENTED + PLAY (`tap_lever_open/close`) |
| 5 Dirt masks (KT01 plates) | DONE in rules + Unity: replicated 8x8 `RProp.Dirt`, `Verb.Scrub` (appended to the enum), contact scrubbing (`RecipeRuntime.ScrubContact`), painted stain decal + suds where scrubbed (`WashableView`), bots via the same rule (`BotBody` work time per cell). RULES 159/159; PLAY: both plates scrubbed 18->0 / 13->0 by sponge contact. NOT done: fixture surfaces (KT13 BT03 HW02 PT04 still patch erosion), sponge absorb/squeeze, foam travelling with the sponge, rinse runoff per cell, client-side partial erosion (clients see whole cells) |

## Owner feedback handled live (2026-11-10 afternoon)
- "The water is not exactly as you see": stream read as a thin line, basin water as a bullseye -> thicker painted column (3.4 cm, bulges, twist,
  bold streaks), pool rings replaced by impact-local ripples + froth + sheen.
- "There's an offset, the water and the ground water are a lot up": the house mesh is MERGED per material (`Batched_Reuse_*`) and statically batched,
  so the V10 sink stretch had moved only the sink's collider; the water used the invisible stretched sink. `HouseAdditions.StretchMerged` now
  stretches the sink's own vertices inside the merged meshes (selected by the sink collision volume, short of the wall) and swaps in un-batched
  copies (same materials + lightmaps). The stream now leaves the visible gooseneck tip; the pool sits on the visible basin floor
  (`recipe_diag water`: 8/8, captures `water_side_open.png`, `water_valve_100.png`). The stream cast also re-starts below the faucet mesh.
- Side effect to watch: 282 vertices of the merged `HoneyOak` wood mesh and a few floor-tile vertices inside the sink footprint were included in
  the stretch (floor y~0 does not move). Re-bake the house with the fitted sink run to remove this runtime pass.

## Open issues (precise)
- Harness hand selection: with two plates 5 cm apart in the rack the free sim hand grabbed the NEIGHBOUR plate (run 8), then plates were dropped
  (37.8 / 39.4 loudness). The HandInteractor picks the hovered item nearest the pinch point; aim the pinch ON the target plate's surface away from
  its neighbour (e.g. its outer face) or verify `Held == target` and re-grip before moving.
- Harness face-up while scrubbing logged 89 -> 90 deg (plate stayed on edge); scrubbing still worked edge-on, but the scrub captures show the plate
  edge-on, so the stain LOOK is verified only by `stain_plate_topdown.png` (water mode). Capture a face-on before/half/after set next.
- `BotBody.ActRecipe` "straightens" items by writing transforms (V7 code) - a V10 no-teleport violation; replace with a bot hand re-seat.
- RESOLVED in run 5: plates leaned 20-30 deg on the 2 cm low wire runs (seated, then flagged crooked -> straighten loops). `RackSeating` now measures
  the dividers' tall hook (z 0.081, 6.7 cm above the tray floor) and guides plates front-to-back onto a support line (z 0.023); rack `SlotPoint` aims
  there too. Plates now seat at 1-13 deg; rack lab 4/5 at 2-8 deg; no artefact crashes in the main run.
- Rack lab insertion 0: the sim hand did not turn the plate on edge (it lay flat on the wires, 85 deg). `RackDrop` should verify the plate is on
  edge before releasing and re-grip if not. Same harness weakness made the "recover after a flat release" check fail.
- `owner_repair_foil`: correct rule outcome - the repair took 24.2 s (2 actions) vs the 20 s `RecipeBoard.FoilWindow`; the simulated hand moves at
  0.2-0.5 m/s with a cautious lift-travel-lower path. Speed up the harness hand (not the rule) and re-check.
- Water look: improved after owner feedback but still a stylised tube; next: aerated/frothy top, splash flecks on plates, pool spread tied to basin shape,
  side-by-side comparison scene (old line vs new) as the directive asks.
- Desktop harness: grabbing an upright plate next to another one picked the neighbour (fixed: side grip for racked plates; re-verify).
- A plate fell off the dining-table edge once during walking runs (plates spawn in a station row near the edge) - give the plates natural table
  spots (V10 D1) instead of the station plan.
- Kitchen sink run is a runtime fit on the baked house; re-bake `SHHH_WorldHouse` with it (and lightmaps) when the house is next baked.
- Bots, two-client Photon dishwashing, Genies hand poses on the new lever/rack: not run after V10.

## Next command
```
bash Tools/cycle.sh - recipe_diag "KT01!"     # full dishwashing by simulated hands + rack lab; Automation/recipe_diag_result.json, QA_Evidence/recipe_v7/KT01_*.png
bash Tools/cycle.sh - recipe_diag "water"     # tap 0/50/100 %, stream on a held plate, side views (water_*.png) - re-run after ANY sink/house change
bash Tools/core_tests.sh                      # rules tests on the PC
```

## V11 state (2026-11-10 ~17:00 UTC+3) — dishwashing NOT yet complete end-to-end
Done this phase (Unity Editor, desktop simulated hands; Quest UNTESTED; no new APK; no Photon run):
- Close-object selection: grab diagnostics (`HandInteractor.LastGrabCandidates`) showed the pinch was inside the neighbour plate; harness now aims
  inside the target away from neighbours and re-grips when it took the wrong item (verified once).
- Carry: held items raised to chest height before walking (plates snagged chair backs at 0.9 m and were released).
- Rack: 22 cm plates (24 cm left 2.5 mm to the front rim), support line 12 mm clear of the rim, max lean derived from rack geometry
  (`RackSeating.MaxLean`, gap clearance over hook height + 4 deg). Plates seat but the 2nd plate still fails in full runs (run 12/13).
- Orientation: plates were often picked up on edge and would not turn flat in the hand (cause not found: no body/furniture block, single hand,
  not kinematic). Grip transition added: set down on the surface below and re-grip from above -> face-up (verified 89 -> 1 deg).
- Stain decal: orientation confirmed Euler(90,0,0) (a -90 "fix" hid it; reverted). Face-on capture helper `FaceShot` (camera on the face
  normal). Face-on proof of stains disappearing is STILL OPEN - re-check `QA_Evidence/recipe_v7/KT01_scrub_*_{before,half,after}.png` from run 13.
- Water: `LiquidContainer` (finite conserved volume, horizontal surface, slosh, weir pour over the low rim, arcing stream `ToonWaterStream.Velocity0`),
  `Puddle` (persistent spills), faucet fills vessels. Liquid lab (`recipe_diag water`, SCRIPTED JIG vessels): mug filled 401/401 ml at 60 % in 3.0 s;
  pour conserved within 0.3 ml; puddles formed. Pour aim corrected after (not re-run). Not replicated over Photon; hand-held pouring untested.
- V11 directive persisted: CLAUDE.md, rules, owner requirements, `Docs/Production/SHHH_V11_PHYSICAL_INTERACTION_ENGINE_DIRECTIVE_2026-11-10.md`.
Next: read run-13 result (`Automation/recipe_diag_result.json`, `[rack]` log lines, "not seated" events) -> fix the 2nd-plate seat -> KT01 5/5 with
face-on stain captures -> re-run `recipe_diag water` -> asset import (Tiny Treats / laundry / Ultimate House) -> shared interaction foundation.


## V12 state (2026-11-10 ~19:00 UTC+3) — assets imported, shared mechanisms, real washer, pouring + shared levels
Owner order: import packs -> shared interactions -> laundry + other chores -> hand-held pouring + shared water levels. Plate washing = done (owner).
**IMPLEMENTED IN SOURCE + compiled (Unity 6000.0.62f1):**
- `Scripts/Game/World/Mechanisms/`: `MechPart` (grasp at handle; HandInteractor gives a handle under the fingers priority over a prop
  behind it), `HingedPart` (real hinge, limits, friction, soft-close latch for gentle closes, slam = NoiseCause.Slam event, machine lock
  rattles), `SlidingPart` (rail, stops, soft close / slam, spring pop-open for incident 8), `RotaryKnob` (twist about own axis, unwrapped
  past 180 deg, detent clicks), `MechButton` (fingertip travel, fires at 70 %), `ScrewCap` (1.5 turns in the bottle's frame, comes off,
  bottle sealed until then), `MechHaptics` (OVR controllers only), `WasherAppliance`, `Vessels` + `VesselSync`, `SoftCloth`, `FabricLook`.
- House washer + dryer (`HouseAdditions.ReplaceAppliances`): the baked static shells are hidden (merged-mesh vertices collapsed, box 12 mm
  beyond the collider so the outer faces go too) and replaced by the split PtDesigns washer, front facing the laundry-room centre
  (owner screenshot: first version faced the wall). Door / detergent drawer (full rail, upright compartment frame) / dial / START / drum
  ring colliders with lifters that really turn; smooth gasket + clean door ring + see-through glass cover the split's stair-stepped edges.
- Rules (Core): Open refused while "start" (door locked); Press start refused with the door open; `Verb.Pour` + `RProp.Ml` (11th snapshot
  field) replicate vessel amounts; sabotage solver presses stop before opening a running washer. LD04/LD05 fixtures anchored INTO the
  house washer/dryer drums (RecipeLayout Laundry `Wash()` anchors). Detergent bottle kept in recipe rounds, on top of the dryer.
- Liquids: tint, neck radius (bottle lip), Sealed, Replica (clients show the replicated level, never move volume), a stream landing on
  its receiver's own floor counts as caught, a vessel never catches its own stream; puddles cannot be wider than their surface (run off
  the edge, max 3 hops, volume never dropped; `Puddle.Lost` counter).
- Clothes: original Blender cloth-sim garments (`Tools/blender/make_clothes.py`; T-shirt + towel wrapped round a rolled core; crumpled
  T-shirt) for laundry_bundle / wet_bundle / dirty_clothes / clothes, painted fabric, SoftCloth sway + squash. Owner: "clothes are not look clothes".
- Held props never collide with their own player's body capsule (bundle snagged on XRRig while loading the drum).
**RULES TEST PASSED:** 161/161 (`bash Tools/core_tests.sh`; new RCP-V12-WASHER, RCP-V12-LIQUID).
**UNITY PLAYMODE VERIFIED (desktop, simulated hands, NOT Quest):** `recipe_diag "washer!"` — LD04 complete by hands 4/4 (door by handle,
bundles pushed into the drum, door closed, START by fingertip), rival opened it only after pressing stop, owner repair foiled; washer lab:
door locked while running (rattle), laundry tumbles in the turning drum and stays inside, dial twisted to Spin (louder noise events), stop,
slam = 1 Mom noise event, slow close = quiet latch, drawer pulled 22 cm / pushed shut, cap unscrewed 540 deg with nothing spilled while
capped, detergent poured by hand into the drawer compartment (120 ml = full, rest overflowed to a puddle, conserved within 0.6 ml).
Evidence: `QA_Evidence/recipe_diag_washer_v12_*.json`, `QA_Evidence/recipe_v7/washer_*.png`, `detergent_pour.png`.
**Known / open:** one LD04 harness check ("rules closed" after the rival re-opened inside the sabotage cooldown — the door eases back to
the authoritative state); second-bundle loading can need a retry when the first bundle sits at the drum mouth; dryer uses the washer
model (no dryer split yet); fridge / bin / drawers3 mech models imported + audited but not placed; house props' liquid not replicated
(recipe props only); clients' door/drawer follow replicated Open/Level, a hand on another peer's door is not yet arbitrated;
TWO-CLIENT / APK / QUEST: not run for any V12 item.
**Next:** place fridge / bin / drawers as house furniture with HingedPart / SlidingPart (+ house mechanism replication registry), LD05
(washer -> dryer) and LD08/LD09 doors, then the remaining mechanics (wiping, sweeping, squeezing, folding, stirring ...) one by one.

Snapshot: `Backups/V10_preflight/shhh_src_v12_washer_20261110.tgz` (sha256 bf29b684d261c89d...).


## V12 part 2 (2026-11-10 ~19:30 UTC+3) — shared house mechanisms + laundry doors/drawers
- **Rules:** `Verb.Mech` (appended) + `RoundController.HouseMech` (id -> opening 0..100), journaled through RecipeAction, so house doors /
  drawers / lids that are not recipe fixtures replicate to every peer and late joiner (`HouseMechSync`). RULES TEST PASSED 162/162
  (new RCP-V12-HOUSEMECH).
- **MechHost** base (washer, dryer, chest, bin): a recipe fixture IS real furniture - its door is a moving part (Open/Close with the real
  opening, re-sent at rest), "placed" = physically inside + resting 0.3 s (tag-matching slot, squareness -> Align). The washer was
  refactored onto it.
- **MechFurniture** (uh_ split models, all colliders in the furniture's upright frame): chest of drawers (3 drawers on rails with real open
  boxes) at laundry (2.45, -0.55), lidded bin (hollow can + liner + hinged lid) at (2.52, 0.15), fridge (2 hinged doors; not placed - the
  baked kitchen fridge is a merged concave mesh without its own collider).
- **Anchors:** LD08 towel_cupboard = chest top drawer; LD09 lint_panel = dryer door (lint starts in the dryer drum), lint_bin = the bin;
  LD05 washer2/dryer = the house machines.
- **UNITY PLAYMODE VERIFIED (desktop, simulated hands, NOT Quest):** `recipe_diag "laundry!"` — LD08 3/3 by hands (top drawer pulled,
  towels set in square by colour, pushed shut) + sabotage/repair; LD05 5/5 (wet bundles reached through the washer door, carried above the
  open door, pushed into the dryer drum, both doors shut quietly) + sabotage/repair; furniture lab: drawers pulled/pushed, a shoved drawer
  slams, bin lid lifted/closed, house mechanism levels journaled. LD09: see QA index (harness approach fixed to come in above the bin rim).
- Harness lessons: steer by the ITEM not the hand (soft items hang off the fingers); approach containers above their rim; lift clear of open
  doors before carrying between machines.

Snapshot: `Backups/V10_preflight/shhh_src_v12_mech_20261110.tgz` (sha256 ec20ed3d6960dd2c...). LD09 3/3 by hands; its sabotage/foil checks still fail (open).


## V12 part 3 (2026-11-11) — multiplayer audit, pets, trash, HUD icons, Genies always clothed (owner message 2026-11-10 late)
**Multiplayer (owner: "very very important"):** audit found the V10-V12 client path broken for hand-moved mechanisms: `HostOnRecipe`
dropped Prop/Prop2/Slot and rejected `Verb.Mech`; client slams were never voiced; a client's START gave no machine noise owner; dial and
screw cap were local only; house liquids / puddles simulated per peer. Fixed: `RPC_RecipeOp` (whole op, host validates; Open/Close/Press/Mech
only - physics stays host-detected), slam flag (`Close "slam"`, `Mech "lvl:slam"`) -> host noise at the part, washer starter from the ledger,
dial + caps as house mechanisms (caps id 100000 + prop id), NetWorld streams vessel ml (NetPropPose.Ml), puddles (NetPuddle incl. granular)
and washer trays; clients never spill; MaxProps 48 -> 96. Settled-rest reports for parts moved by this peer (hands, scripts, bots) + no
remote snap-back of a part this peer just moved.
**TWO-CLIENT VERIFIED (real Photon, two Windows player processes, `net_multi 2`):** run TWDK - feed probe OK (client grab intent + hand
pose tipped the food bag, host poured 25 g, host dog ate, replica saw meal 0 -> 1), client slam voiced by host, mech intents applied,
desync 0 / 316 checksums. Mech + trash probes failed in that run (snap-back bug, fixed after); re-run DVRP pending at time of writing.
**Pets (rules + world):** `PetHunger` ticks with the journaled clock (2-6 meals per match by length), `PetEat` / `TrashBinned` journaled ops
(RPetEat, RTrash appended), feeder / tidy bonus; PetBrain walks a hungry pet to its bowl; `PetCare` (host: eats the real kibble in the bowl,
whines -> journaled pet-sound noise rising with hunger; every peer: badge + bark/meow from replicated noise). RULES 163/163.
**Clutter + trash:** `HouseClutter` (seeded, same on every peer): trash anywhere (5 per 5 min, cap 12), the food bag somewhere random,
bowls by the beds. Lidded bins in laundry, kitchen, living room (kitchen/living placed on measured free floor). `TrashSystem` (host).
**HUD:** `TaskHud` head-locked strip (my step icon + label, dog / cat hunger bars, trash left) + 25 original Inkscape icons.
**Genies:** five saved looks (Pip, Nell, Rory, Tess + Mom, cache v3 with top / bottom id sets); `IsClothed` guard everywhere (cached,
local, remote, presets); wardrobe refuses saving an unclothed look; no clothed look = clothed stand-in body, never a bare SDK default.
Verified in Editor with the real SDK (genies_journey v12looks): 3 cached looks failed the guard and were rebuilt dressed; Mom built
(charcoal knit sweater, flare pants, fuzzy slippers) and worn by MomCharacter.
**UNITY PLAYMODE VERIFIED (desktop):** `recipe_diag pets!` 7/7 (clutter, hungry dog at bowl + whine noise, kibble poured by hand -
pellet stream, conserved, dog ate real kibble, meal + feeder bonus, trash binned past a lifted lid, HUD).
**Open:** Asset Store "Free Pack - Cartoon Dog & Cat" not in the download cache - the owner must add it to their account (cannot accept
terms for them); bots do not feed pets / bin trash yet (parity gap); recipe props are not yet scattered house-wide (trash + food only).


## V12 part 4 (2026-11-11 later) — multiplayer re-verified, bot care, kibble physics fixes
- **TWO-CLIENT VERIFIED (real Photon, 2 Windows players), run MXNK:** drawer probe OK (client opened house drawer 200, resting level 68
  replicated in 4.5 s, the host's own drawer followed to 0.67; shove-shut replicated in 0.2 s, slam voiced by the host as one noise
  event), feed probe OK (journaled meal / grams seen on the replica), desync 0 / 335 checksums. Trash probe: host refused the client grab
  (`refused by rules`) - the probe's hand was not marked tracked; fixed in the probe; not re-run yet. Remote-hold robustness fixed: a held
  item whose remote hand loses tracking is frozen gently (kinematic) and handed back softly (the untracked bag had crashed repeatedly
  and made Mom confront the client).
- **Bot parity (UNITY PLAYMODE VERIFIED, desktop):** bots feed a hungry pet (fetch bag, aim the SPOUT over the bowl, tip until it flows,
  stop at a meal / on a miss, bag back where it was found): `bot_feeds_hungry_pet` PASS twice in a row (112-114 g poured, 100 % caught,
  dog meal 0 -> 1, bot care bonus). Bots bin trash (lid lifted as a house mechanism, closed after): binned 8-12 of 12 in a 75 s window.
- **Pets:** a hungry pet waits BESIDE its bowl and steps in only once food is in it and nobody is pouring (a dog in the stream wasted
  the food); the dog really walks there (start 2.5 m away -> 0.5 m) - an earlier pass with the room loop disabled was a false positive
  and was corrected.
- **Kibble (dry goods):** no slosh; only pours when tipped > 35 deg; bags start 70 % full (brim-full vessels spill when carried);
  open bowls catch inside the outer rim; in-flight streams are traced down to what is really below; kibble heaps (granular puddles)
  replicate as kibble. Bag opening 6 cm (no cap).
- **Open (harness, not gameplay):** the desktop harness's straight-line carry snags the bag on furniture and drops it, so the
  "player feeds the cat" check fails in the bot-active lab (player feeding itself passed in the first pets run: 151 g poured, dog ate
  101 g, meal + bonus). Asset Store dog/cat pack still needs the owner's account.

Snapshot: `Backups/V10_preflight/shhh_src_v12_pets_mp_20261111.tgz` (sha256 fb5b4349dd2317b3...).


## V12 part 5 (2026-11-11) — owner's Asset Store dog & cat in game, multiplayer re-run
- **Pets = "Free Pack - Cartoon Dog, Cat" (PolyOne Studio, Asset Store EULA; the owner added it):** prefabs + Idle/Run clips in
  `Resources/Pets/` (pack itself left in `Assets/PolyOne`). `PetBody.BuildPolyOne()` replaces the Astra/procedural pets (kept only as a
  fallback if the files are missing): normalised to 0.50 m (dog) / 0.34 m (cat), pack URP-Unlit material -> house toon Lit with the pack
  texture. `PetAnimator` (new): PlayableGraph mixer loops the non-looping clips, blends idle <-> run by real ground speed (works for host
  NavMesh pets and client puppets), run rate follows speed, hungry pets trot a little in place. The clips key the animated root (scale 1,
  pos 0) - the size fit lives on a parent "fit" node (first try made the dog ~3x too big). Facing is measured from Tail -> Head bones
  and corrected if needed (0 deg for this pack).
- **Multiplayer fixes found by the re-run:** `HouseMechSync` "at rest" is now a speed (< 3 %/s), not a per-frame delta - at high frame
  rates a moving lid/drawer was reported mid-move (lid replicated 12 % instead of 100 %). Probe harness: the client autopilot pauses
  while probes own the hand; probes wait out Mom's sequence (grabs correctly refused then) and carry along the NavMesh at ~1.1 m/s
  (a straight-line carry hit a wall and spilled the bag - real physics, unreal route). Host-side `HostFeedSampler` logs the remote-held
  bag (tilt, kibble, stream target).
- **TWO-CLIENT VERIFIED, run UVUW:** mech OK, feed OK (180 g into the bowl, replica saw the dog eat), trash OK, desync 0.
- **UNITY PLAYMODE VERIFIED (desktop):** PolyOne pets walk/idle, dog to bowl + whine, bot feeds the dog (meal 0 -> 1).
- **Open:** desktop `pets!` lab still uses a straight-line carry for its player-feed/trash-by-hand checks (same snag; the network probe's
  NavMesh carry should be ported to `RecipeDiagnostic`); recipe props not yet scattered house-wide; Quest NOT TESTED.

Snapshot: `Backups/V10_preflight/shhh_src_v12_polyone_pets_20261111.tgz` (sha256 0ee9cf8c0e09113e...). Rules tests 163/163.


## V12 part 6 (2026-11-11) — pets lab fixed, two real bugs found on the way
- **Bug (scoring, all matches):** `Scoring.SyncFromBoard` rebuilt `Bonus` from the chore board, wiping pet-care / trash points added by
  `AddBonus` at the next chore step. Care points now live in their own `RoundScore.Care` (in `Total`, never reset). Rules test added.
- **Bug (bots vs players):** bots chased the same trash and used whichever bin was nearest, even one a player was opening (a lid could be
  yanked out of a player's hand). New host-side courtesy claims in `RoundController` (not journaled, lapse after 20 s): `ClaimTrash`,
  `ClaimBin`. Bots skip claimed trash, pick an unclaimed bin (or wait), a local hand on a bin lid claims it (`MechPart.Grabbed` ->
  `MechFurniture.ClaimBinFor`), and the host claims a bin for a remote player whose lid move arrives as `Verb.Mech`.
- **Harness (desktop lab):** `WalkTo` follows the house NavMesh when the straight line is blocked (body + held items turn at corners);
  clear lines walk straight as before, so other labs are unchanged. Pets lab walks to the trash before grabbing it, uses the nearest
  bin, waits for a lid at rest with no bot at it, lifts a re-closed lid one-handed, and comes in from the front of the bin opening.
- **UNITY PLAYMODE VERIFIED (desktop):** `pets!` 8/8 twice in a row. **TWO-CLIENT VERIFIED:** run PWUD all three probes OK, desync 0.
- **Open:** recipe props not yet scattered house-wide; Quest NOT TESTED.

Snapshot: `Backups/V10_preflight/shhh_src_v12_petslab_20261111.tgz` (sha256 87a19c5b0f809c33...).


## V12 part 7 (2026-11-11) — full-body Genies avatars for every player + chore items left all over the house
Owner: "for me as player not only hands ... animation rigging with character from Genies ... when I play with my friends we will be
seeing each one avatar and moving"; "chore items aren't spread around the whole house yet". Classroom-VR used as CONCEPT ONLY
(its VRMap idea: headset + hands drive the avatar's IK targets, the body follows the head) - no code, assets or IDs taken.
- **`TrackedGeniesBody`** (Agents/, own file): one driver for my own body and every friend's body, on Unity humanoid IK. Body scaled to
  the player's measured standing eye height (feet on the floor); crouch lowers the hips with planted feet (knees bend); hip hinge lean
  with the gaze / low hands (in the IK pass, before the arms); hands to the tracked pose with rotation (goal->hand offset calibrated
  from the avatar's own bones, so no rig-axis guessing); arms may stretch up to 25 % (kid proportions scaled to adults) only when the
  target is out of reach; head follows the headset (neck + head share); body turns when the head passes 55 deg or while walking;
  fingers curl with the grip (friends: replicated holder table); feedback keeps the avatar's eyes on the headset (upward correction
  capped at 5 cm so feet never leave the floor); Animator always animates (my own body is mostly off-camera).
- **My own body (`SelfBodyView`)**: my saved Genies look under my headset, head and Genies hands hidden for my camera only (bone
  collapse after LateUpdate), arms end at my gloves' wrists (the tracked gloves stay my hands = exact finger tracking).
- **Friends (`RemoteHumanView`)**: their Genies body driven by their replicated head + hand poses (no new network data needed).
- **No Genies (batch players, offline)**: unchanged stand-in body + tracked mittens. `net_multi "2;windowed"` launches small visible
  players so the Genies SDK runs and the host can photograph the remote body (`Automation/net_multi/host_sees_seat2_*.png`).
- **Chore items left around the house** (`HouseClutter.ScatterChoreProps`, Core, seeded = same house on every peer): ~75 % of loose
  clothes / dishes / toys / books / shopping start elsewhere (60 % in another room): clothes on the sofa or floor, dishes on tables,
  the rest on floors/tables; tools and parts stay at their job. Surfaces measured in the baked house (`recipe_diag surfaces12`):
  coffee table 0.43, side table 0.60, dining table 0.75, sofa seat 0.52 match; armchair (0.96 box) and TV stand (0.93) excluded.
  Each moved item has `PropSpec.StartHint`; the task HUD shows it ("family shoe: on the kitchen floor"). Single-mechanism QA labs
  switch it off (`RoundConfig.ScatterChoreProps`). Pet bowls now pick a side of the bed clear of job tables and chore items (a job
  laid out over the dog bowl caught the kibble in network run SWFV).
- **RULES TEST PASSED 165/165** (+ CM-V12-SCATTER, CM-V12-BOWLS; layout test: station rules for items at their job, scatter rules
  for the rest). **UNITY PLAYMODE VERIFIED (desktop):** `recipe_diag avatars!` 8/8 (self body under me, friend stands / reaches /
  crouches to the floor / turns / walks; wrists 0-2 cm, head 0 cm, feet 0 cm); `recipe_diag scatter` 16/16 items at rest where left,
  HUD hint shown. **TWO-CLIENT VERIFIED (windowed, real Genies on both peers), run ZKUF:** the host renders the
  client's Genies body from their replicated head + hands (worst head 10 cm, wrist 10 cm over 324 s), all probes OK, desync 0.
  Also fixed on the way: kibble now flows like a granular solid (Beverloo, ~0.45 kg/s through the 6 cm bag opening).
- **Open:** Quest NOT TESTED (hand-tracking pose conventions on the headset may need the wrist offset tuned); self body in Practice
  depends on the Genies login on the device.

Snapshot: `Backups/V10_preflight/shhh_src_v12_bodies_scatter_20261111.tgz` (sha256 7724911cb313c155...). Rules tests 165/165.

## V12 part 8 (2026-11-11) — APK built and installed on the owner's Quest 2
- **APK BUILT:** `Builds/Quest2_3_3S/SHHH_alpha_0.6.4_c13.apk` (release-signed, 175,705,666 bytes, sha256 eef9213a93a8749b...; BUILDS.csv).
  c12 and older APKs kept.
- Installed over USB (adb, device 1WMHH8493D1016 Quest_2). The headset had the debug-signed dev 0.1.7 (c8): signature mismatch, so it was
  uninstalled with the owner's approval (its on-device saved data removed), then c13 installed (versionCode 13 / 0.6.4) and launched:
  process running, no game exceptions in logcat (release build: game logs stripped).
- **REAL QUEST VERIFIED: not yet** - waiting for the owner's hands-on test (self body / friend bodies / scattered chores / pets on device).

## V12 part 9 (2026-11-12) — owner's Quest 2 run analysed; Genies crash fixed
- Owner recording analysed frame by frame: 21 problems -> `Docs/QA/Quest2_test_2026-11-11_c13_findings.md` (REAL QUEST TESTED, c13).
- #1 crash = Genies native crash (body destroyed while the SDK builds it). Fixed in `GeniesBackend.LoadAsync` (workshop slot build,
  wanted-check, dispose only after build, 45 s timeout), `MenuBuilder.WardrobeStep` (locked rows change nothing), `FamilyStage.RefreshOutfits`,
  `SiblingAvatar.SetGeniesLook` (dedupe + wanted). UNITY PLAYMODE VERIFIED: wardrobe_spam 2/2, avatars! 8/8. APK c14 built -> Quest re-test.
- #2 my arms: `SelfArmTrim` (upper arms removed, forearms slimmed + tapered, my camera only). UNITY PLAYMODE VERIFIED: own body covers
  0.9-2.4 % of my view (was 4.4-11.8 %), avatars! 11/11. Not in an APK yet (c14 has the crash fix only). Capture runs need the Unity window
  uncovered (a maximized window in front makes Vulkan render black).
- Hands corrupted on Quest: `SimpleHandVisual` tracked path rewritten (rotation-only retarget, no stretch, back-of-hand roll, size from the
  real finger, thumb/index onto real tips). UNITY PLAYMODE VERIFIED with synthetic tracked joints: `recipe_diag hands` 4/4. Not in an APK yet.
- Grabs: bone ids by skeleton type (OvrHandSource + SimpleHandVisual - XRHand/Hand enums overlap), late grab on arrival (not palm-up,
  only while pinching), fingertip-aware reach, on-device grab log `shhh_grablog.txt`. UNITY PLAYMODE VERIFIED: grabs 6/6, pets! 8/8.
  Not in an APK yet (c14 = crash fix only); next Quest test: pull the grab log.

- UI (#4, #6, #12-#18 partly): deliberate-look wrist watch, one stable menu-board spot (`LecternFollow`), full-view black fade +
  loading card, HUD on top of my body (cached on-top materials in `TaskHud.Top`), nameplates hide/scale up close, Customize -> Genies
  look, Practice HUD text. UNITY PLAYMODE VERIFIED: `recipe_diag ui12` 8/8, rules 165/165. Not in an APK yet.
- Next: build APK c15 with crash + arms + hands + grabs + UI (`bash Tools/job.sh build_apk_release`), keep c13/c14 + sha256,
  install on the Quest, owner test, then `adb pull /sdcard/Android/data/<package>/files/shhh_grablog.txt`.
  Still open from the findings: Mom penalty feedback (#10-11), bots spawning on / walking through me (#6-7), job object readability
  (#18 tray/coasters), dog bowl look (#19), floating smiley ball (#20), boundary (#21).
- Bots / Mom (#6, #7, #10, #11): `PersonalSpace` (players are NavMesh avoidance obstacles), bot spawns 1.5 m from humans, Mom stands
  1-1.6 m from the noise in my view and faces me, HUD Mom line (heard / coming / question / verdict + reason), penalty card reason,
  tray follows, watch "MOM IS UP" only during the visit, smaller pet badges. UNITY PLAYMODE VERIFIED: mom12 9/9, confront 12/12,
  pets 8/8, rules 165/165. Not in an APK yet. Still open: job object readability (#18), dog bowl look (#19), smiley ball (#20), boundary (#21).
- Remaining findings (#18-#21): wooden tray + round mug rings, coaster tag, job card turns to me (> 1 m), HUD label 2 lines, bigger dog
  bowl on a feeding mat, dog toy by the dog bed (Core HouseClutter), play-space edge reminder (`Playspace`). UNITY PLAYMODE VERIFIED:
  props12 7/7, ui12 8/8, pets 8/8, mom12 9/9, rules 165/165. All 21 findings now addressed in source; none in an APK yet.
- Next: build APK c15 (`bash Tools/job.sh build_apk_release`), keep c13/c14 + sha256, install, owner test, pull shhh_grablog.txt.
  Note: `Tools/cycle.sh` can print "compile ok" from a stale report - confirm with `grep "error CS"` in the Editor.log tail.
- Walls missing on the Quest: root cause = runtime mesh cut of a Read/Write-off mesh (V5_LivingArchitecture) in NightExterior.OpenWindows
  -> now a material swap (faint glass). Rule: never copy/edit an imported mesh at runtime unless `mesh.isReadable` (the Editor hides
  this; APKs do not). Teleport (right-hand hop, arc stops at walls, floor-connected landings), palm-up hands never pull-grab.
  UNITY PLAYMODE VERIFIED: walls12 3/3, teleport12 5/5, grabs 6/6, ui12 8/8, rules 165/165. Not in an APK yet.
- Family Stage / all sibling bodies: starter Genies looks ship in Resources (fresh installs), loading silhouette instead of the old
  stand-in (stand-in only on failure / 25 s), stage pick re-dresses in place. UNITY PLAYMODE VERIFIED: stage12 4/4, mom12 9/9.
  Captures need the Unity window uncovered (black renders otherwise - checks that measure pixels then pass falsely).
- Loading screen (#12, owner re-flagged the 0:20 grey void + black wedge of c13): already fixed in source (full-view black fade quad +
  "SHHH! loading the house..." card); also the task strip no longer draws over the loading card (hidden while the fade is > 40 %).
  `recipe_diag ui12` 9/9 (new check hud_hidden_while_loading). Not in an APK yet.
- Own body: complete outfit + Genies hands with tracked fingers (gloves unseen), body 5 cm back; near-player UI drawn on top (HUD, wrist,
  boards, tray; _QueueOffset fix). avatars 15/15, ui12 9/9, hands 4/4. View coverage of the full sleeves not measured (black renders).
- APK BUILT: `Builds/Quest2_3_3S/SHHH_alpha_0.6.6_c15.apk` (release-signed, versionCode 15 / 0.6.6, 175,767,016 bytes,
  sha256 14d0e5fac48998c8c977a3ff4462797f605966c63785d0b770e6195b18d8f2ab, 864 s, 54 warnings). Contains every fix since c13 (Genies crash,
  own body, hands, grabs, UI, bots / Mom, job objects, pets, walls, teleport, starter looks, loading). c13 / c14 kept. NOT installed yet -
  owner will connect the Quest and ask. Next: install (adb install -r), owner test, then pull shhh_grablog.txt.

## Fixes after the owner review (2026-11-12) - in source, not in an APK yet
- O4 hands: tracked-hand rotation converted with the wrong axis convention (palm 91 deg off in every pose) -> fixed (worst 3 deg fingers /
  6 deg palm over 6 poses x 2 hands, `recipe_diag avatars` my_hands_turn_like_my_real_hands). Friends' bodies use the same fix.
- O5 vibration: head motion never moves the rig and the body does not shake (`stable12` 2/2) -> most likely missed frames on the Quest.
  Added: on-device frame-rate log (PerfMonitor -> shhh_grablog.txt every 5 s, "NN fps" on the task strip when low), dynamic fixed
  foveated rendering, 2-bone skinning for other Genies bodies, no shadow casting. To confirm with the next headset log.
- O1 release: grip now = each finger's own straightness (c15 tip-to-palm grip could read an open hand as 0.3-0.6); an open hand always
  lets go; HOLD/RELEASE lines in the grab log (`grip12` 2/2, grabs 6/6). Device confirmation pending.
- #1 start-up: new tiny Startup scene shows the loading card at once and loads Boot in the background; boot timeline in the log
  (Editor: first frame +0.28 s, front end +4.2 s).
- O3 teleport: glowing flowing arc + landing spot shaders (Resources/Shaders), crossed-out spot + reason where you cannot land, HOP / WALK
  instructions above a palm-up hand, practice board explains both (`teleport12` 8/8).
- O2 dark lighting: lightmap + fog shader variants were stripped from the APK (Automatic stripping, build scenes have none) -> Custom
  stripping enforced by the build. Device confirmation pending.
- #9 yellow ring: noise ripples capped by distance from the eyes and faded up close. #4 wardrobe: no fake ticks while a drawer opens,
  icons drawn with alpha over a light backing, icons refresh as they load, other drawers prefetched.
- NOT done yet: #11 (mug / plate not put back in Practice), #12 (practice board far / small), #14 (red glove hint inside the book),
  #8 (book held tilted / too close - mostly caused by the stuck release). No new APK built.
- #11 / #12 / #14 done (2026-11-12): PracticeTidy returns practice items left on the floor / out of the room for 4 s (not while held);
  the practice board stands next to the bench facing the start, 1.25x; the round-start demo shows its glowing copy only once the ghost
  hand lifts it (it sat inside the real book). `recipe_diag practice12` 3/3, ui12 9/9, rules 165/165. All c15 items now addressed in
  source except #8 (book tilted / close - expected to go with the release fix). Next: build APK c16 and test on the Quest.
- APK BUILT 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.7_c16.apk` (release-signed, versionCode 16 / 0.6.7, 292,023,062 bytes, sha256
  2d40d351ce049c3487b3dd69cfa34969b80c053c512d41a4da1c312558bc13c9). Contains every c15 fix. Size +116 MB vs c15: keeping ALL lightmap / fog
  shader variants (URP Lit alone 50 MB). The build setting is now narrowed to non-directional lightmaps + linear fog only (next build).
  Not installed (Quest adb still "unauthorized" at last check). c13-c15 kept. The first c16 attempt (2026-11-12) was cancelled; the
  reason was not recoverable (log overwritten by two Editor restarts); the rebuild on 2026-11-15 succeeded unchanged.
- INSTALLED 2026-11-15: c16 (0.6.7, versionCode 16) on the owner's Quest 2 via adb install -r (now authorized). Not yet played.
- c15 grab log pulled (`QA_Evidence/quest_logs/shhh_grablog_c15.txt`): at 15 s, relaxed hands read grip 0.92 (left) / 1.00 (right) -
  confirms the release root cause (c15 grip read open hands as a fist; release needs < 0.4). c16 uses per-finger curl + open-hand release.
