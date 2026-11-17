# SHHH! physics contract — 26 primitives, world-state rules, status

Last updated 2026-11-10 (V9). Owner directive: `Docs/Production/SHHH_V9_PHYSICS_ASSETS_VFX_MASTER_CONTINUATION_2026-11-10.md` §3–§5.
Status words: SRC = implemented in source · RULES = rules test passed (`Core/SHHH.Core.Tests`) · PLAY = Unity PlayMode verified (desktop, simulated
hands) · NET = two-client verified · QUEST = real headset verified (none yet). "Gesture" = the current implementation is a rub/poke gesture and
does NOT yet meet the V9 bar; it must be replaced, not counted.

## 0. V10 mechanical goals (2026-11-10, owner override — supersedes any "Gesture"/poke implementation below)

Every primitive must become a continuous physical mechanism, not an OFF->ON switch: lever angle -> flow rate; pump travel -> dose (finite reservoir);
cloth pressure/speed/contact -> local dirt mask; container tilt + volume -> pour flux; door angle + latch -> reachable drum; drawer travel -> access;
bucket volume + acceleration -> slosh/spill; impact impulse + material -> noise (aggregated, one Mom event per collision cluster).
Fixed counts: **26 primitives, 72 recipes (LV01–LV14, KT01–KT16, LD01–LD10, BR01–BR08, BT01–BT06, FY01–FY06, PT01–PT06, HW01–HW06),
12 comedy incidents** (plate dominoes, ball downstairs, wobbly stool, squeaky floorboard, vacuum hose snag, sloshing bucket, rotating chair,
spring drawer, swinging lamp, Biscuit/Mochi toy chase, unstable book tower, soap overflow).
World placement: no central task table; each recipe prop starts at an authored in-house location (world task anchor) — see `SHHH_V10_RECOVERY_STATE.md`.
Per-task acceptance (directive §K2): in-world location · art quality · mesh hierarchy · hand grip · constrained mechanism · contact · material response ·
persistent VFX/wet/dirt state · sound · wrong-action behaviour · completion predicate · sabotage/repair · bot · network · Quest status.
Any recipe missing a mandatory runtime category is **NOT PRODUCTION COMPLETE**.

## 1. Causality pipeline (how a step really advances)

```
tracked hand / controller (+ Genies hand IK)  ->  HandInteractor grab/pinch, RecipeRuntime hand points (local, remote via Photon, held tools)
 -> physical sensing in the Unity layer (contact on the right surface with the right tool, stream volume, slot gap + orientation, pad travel)
 -> authoritative op ROp{Actor,Verb,Prop,Fixture,Slot,Arg} -> RecipeWorld.Apply (legality: holding, open/closed, running tap, scrub-before-rinse,
    stroke caps, free slot ...) -> ledger REvent (append-only, idempotent EventId) -> RecipeBoard predicates (step done only from world state)
 -> Mom: Round.SubmitImpact/SubmitOperate noise events (material, actor, position) -> meter, incidents, witnesses (BeliefLedger)
 -> presentation on every peer from replicated state: fixture/prop views, VFX (RoomFx), SFX (AudioBank), clipboard
 -> network: host = authority; clients send intents (RecipeAction) + hand poses; journal replicates; desync checksums
```

Bots use the same ops (RecipeSolver plan -> bot motor -> `RecipeRuntime.Submit`), never a completion flag.

## 2. Core state per object (live source today)

`RProp`: Fixture/Slot (placed), Spot (loose location, evidence), Oriented, Stage (0..n: dirty->soapy->rinsed, folded, filled), StrokeCap (washable:
scrub reaches this stage; rinse at a running tap goes beyond), Partner, AttachedTo, HeldBy, LastHolder. `RFixture`: Open (door, lid — and for
`sink_tap` the VALVE), Decals (dirt patches), Occupant[] (slots), Flags. Snapshot `RW1` covers all of it (join/rejoin, desync checks).

V9 additions still to land in core (planned, not yet in source): per-surface dirt coverage masks (8×8 bits per patch / plate face, compact to
replicate), liquid volumes (container ml, capacity, spill), mechanism joint state (door/drawer angle or travel), cloth fold/wetness.

## 3. The 26 primitives — status matrix

| # | Primitive | Live implementation | Recipes bound now | Status |
|---|---|---|---|---|
| 1 | Scrubbing | V10: KT01 plates carry a replicated 8x8 stain grid (`RProp.Dirt`, snapshot field); `RecipeRuntime.ScrubContact` needs real sponge contact on the eating face (gap < 12 mm) WITH motion; work accumulates only in stained cells under the sponge footprint (5 cm of scrubbing per cell); cells go through the authoritative `Scrub` rule (plate held + soapy sponge in the same hand set; bare hand / dry sponge refused); stage 1 (suds) when the last stain is gone; smudge sabotage re-dirties; bots use the same rule. Painted per-plate stain decal (64x64) erodes cell by cell; suds where scrubbed. Fixture dirt patches (KT13 BT03 HW02 PT04) still use the V8 patch erosion | KT01 (grid), KT13 BT03 HW02 PT04 (patches) | SRC + RULES (RCP-V10-DIRT) + PLAY (scrub_stains checks); per-cell masks for fixture surfaces: TODO |
| 2 | Wiping | Same patch system with cloth/towel/mirror cloth/duster as tools | KT13 BT03 LV12 HW02 | SRC+PLAY; spray prerequisite, wet trail: TODO |
| 3 | Sweeping | — | (FY/HW floor tasks) | TODO |
| 4 | Pouring | V11: `LiquidContainer` = finite conserved litres per vessel (cylinder model: world-horizontal surface from volume, slosh lag); pour = weir flux over the lowest rim point once the surface reaches it; leaves as an arcing `ToonWaterStream` (Velocity0, sensed along the arc); landing in another opening transfers volume (overflow spills), elsewhere grows a persistent `Puddle`; faucet stream landing in an opening fills the vessel. Recipe Fill steps (KT11 PT01 PT02) still use the rub gesture - to be bound to container amounts | KT01 sink (fill), lab | SRC + PLAY (`recipe_diag water` liquid lab, SCRIPTED JIG vessels: fill 401/401 ml, pour conserved within 0.3 ml, puddles); hand-held pouring and recipe binding: TODO; volumes not replicated yet |
| 5 | Squeezing | — | LV14 (squeaky toy) | TODO |
| 6 | Twisting | V10: KT01 mixer lever is a 1-DOF mechanism dragged along its real arc between stops by a touching hand (local/remote/bot); valve 0..1 = continuous flow; replicated as `RFixture.Level` (Open op Arg, 10 % bands); no poke key | KT01 | SRC+RULES (RCP-V10-WORLD) + PLAY (`tap_lever_open` 2026-11-10, desktop sim) |
| 7 | Pulling | Drawers/lids open by poke key | LD08 BR-drawers | **Gesture** — needs sliding joint |
| 8 | Pushing | — | — | TODO |
| 9 | Pumping | `SoapPumpView`: daycare KIT026 (V10: re-oriented upright - it was imported Z-up), real `PART_PumpHead`; a hand pushing the pad = one stroke = one dose into the sponge held under the nozzle; V10: "Pump" poke key removed | KT01 | SRC+RULES+PLAY (desktop sim) |
| 10 | Shaking | — | LV07/BR (pillows) | TODO |
| 11 | Stirring | — | KT kitchen prep | TODO |
| 12 | Slicing | — | KT kitchen prep | TODO |
| 13 | Rolling | — | LV05 toy cars (roll noise only) | TODO |
| 14 | Folding | Stage `Fold` by rub gesture + flatten visual | LV08 LD BT01 | **Gesture** — needs two-hand corners + skinned cloth |
| 15 | Stretching | — | BR beds | TODO |
| 16 | Stacking / balancing | Stack fixtures snap flat items; no balance | LV02 LD | Partial — no centre-of-mass / wobble |
| 17 | Catching | — | incidents | TODO |
| 18 | Throwing / tossing | Release velocity + Throw noise cause | general | Partial — no target-volume settle rule |
| 19 | Vacuuming | — | floor tasks | TODO |
| 20 | Sliding | V10 rack: KIT018 at authored 1:1, upright (was lying on its back), plates = convex disk colliders that stay dynamic; `RackSeating` capped spring-damper guide only in the last cm; seated = at rest on the tray floor inside a 46 mm gap within 25 deg (no kinematic snap) | KT01 + all Place steps | SRC; PLAY in progress (see QA index) |
| 21 | Hinging | Lids animate on poke key; KT01 tap lever pushed by hand (valve = fixture door state) | LV04 LV14 BR KT01 | **Gesture** for lids; tap lever SRC+PLAY |
| 22 | Connecting | Attach by touching the partner piece | LV10 LV13 KT11 | Partial — orientation/axis not checked |
| 23 | Plug / unplug | — | KT appliance cord | TODO |
| 24 | Pendulum | — | incident 9 (lamp) | TODO |
| 25 | Spring compression | Pump head spring-return (visual) | KT01 | Partial |
| 26 | Two-handed wringing | — | LD towel | TODO |

## 4. Flagship slice 1 — dishwashing (KT01) as of 2026-11-10

Recipe (core, 5 steps, `RCP-KT01` + `RCP-KT01-WATER` rules tests pass, 157/157 suite):
1. Pump soap onto the sponge (`Fill` sponge at `soap_pump`) → 2. Sponge the food off each plate (`Stroke`, cap 1: food → suds) →
3. Tap on, rinse the suds off at the sink (`Fill` plate at `sink_tap`; refused while the tap is off or before scrubbing) →
4. Plates upright in the rack (`plate_rack` upright slots) → 5. Turn the tap off. Sabotage: remove a clean plate / dirty a plate / leave the tap running.

Physical layer (Unity): the sink is **the house's real kitchen sink** (KayKit counter sink; basin measured with `recipe_diag probe`:
floor y 0.36, x 0.48–1.12, z 2.46–2.78, faucet x 0.80); the daycare **KIT015 mixer lever** sits beside the faucet (push it with a hand, or poke
"Tap on/off"); the stream falls from the measured spout; a plate held IN the stream for 1.1 s rinses; running water makes Mom noise every 2.5 s
(louder while splashing) blamed on whoever opened it. Plates/sponge/pump/rack sit on a dining-table station; you carry each plate to the sink.
Dish rack = daycare **KIT018** with its authored divider colliders; plates must arrive on edge (≥35° from flat) and are seated upright.
Soap = daycare **KIT026** pump.

PlayMode evidence (desktop, simulated hands — NOT a headset): see `Docs/SHHH_PHYSICS_QA_EVIDENCE_INDEX.md`.
Known gaps: dirt is stage-based (no per-cell mask yet); plate faces are not distinguished; rinse runoff/foam draining is visual only; rack wobble
sabotage (incident 1) not built; the harness occasionally drops a plate off the table edge (layout puts plates at the front edge).

## 5. Quest feasibility rules
- No CFD, no unrestricted cloth: masks + finite volumes + constrained deformation.
- Particle bursts pooled/rate-limited (`RoomFx` 80 ms global cooldown); no per-particle networking; physics props ≤ ~60 dynamic bodies per room.
- Colliders: authored COL_* boxes from the daycare pack; MeshColliders only for static furniture.
