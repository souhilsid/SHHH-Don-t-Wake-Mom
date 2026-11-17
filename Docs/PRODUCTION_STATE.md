# PRODUCTION STATE — authoritative restart point (V7, final locked spec)

**Spec:** `Docs/Production/SHHH_CLAUDE_MASTER_PRODUCTION_PROMPT_FINAL.md` (16 locked decisions, 72 recipes, gates 0–9).
**Last update:** 2026-11-09 11:4x UTC, Claude session V7-2 (Gate 4: every reachable recipe completed by simulated hands with sabotage + foil; Gate 5 confrontation loop; Gate 8 progression, wardrobe, Family Stage; recipe sounds/VFX). Headset: UNVERIFIED.
**Restart rule:** after any context loss, read THIS file, then `Docs/TEST_EVIDENCE_INDEX.md`, then `Docs/RISK_REGISTER.md`. Do not rely on
conversation memory.

## Baseline (Gate 0, 2026-11-08)
| Item | Value | Evidence |
|---|---|---|
| Unity | 6000.0.62f1 (f99f05b3e950) | `UnityProject/ProjectSettings/ProjectVersion.txt` |
| Packages | URP 17.0.4, ShaderGraph 17.0.4, VFX Graph 17.0.4, Meta XR Core 201.0.0, OpenXR 1.16.1, XR Mgmt 4.7.0, Input System 1.8.1, AI Navigation 2.0.0 | `Packages/manifest.json` |
| Networking | Photon Fusion 2.1.4 (Host mode) + Photon Voice 2.64; PeerMode=Single | `Assets/Photon`, `NetworkProjectConfig.fusion` |
| LiveKit | **not installed** (no SDK, no backend, no token service) | manifest + Assets scan |
| Hands | Stylized Simple Hands (`Assets/SimpleHands`) bound to tracked hands by Codex `SimpleHandVisual` | book run: "installed SimpleHands visual instances=2" |
| Build target | Android, IL2CPP ARM64, Vulkan, minSdk 32 / target 34 | V6 release APK check |
| Production scene flow | `Boot.unity` → GameRoot (front end, practice, rounds) — rooms are built at runtime by `RoomRuntime`; normal rounds instantiate `Resources/SHHH_WorldHouse.prefab` (Codex connected house, baked lightmaps/probes); Practice keeps the procedural shell | `GameRoot.cs`, `RoomRuntime.cs`, `HouseAssembly.cs` |
| Review-only scenes | `ArtProductionV4/World/Scenes/ConnectedHouse_Production.unity`, `ArtProductionV5/Scenes/ConnectedHouse_V5_Living.unity`, `Networking/Scenes/NetSmokeA/B.unity` — NOT shippable XR levels | scene inventory |
| Backup | `Backups/GATE0_before_claude_v7_20261108_132609Z_code_settings.zip` (767 files code/scenes/prefabs/settings/docs) + Codex `SHHH_ArtProduction_V5/00_Baseline/Before_V5_20261108.zip`; cloud mirror git tag `pc-baseline-v7` | |
| Last release build | `SHHH_Quest2_Quest3_Quest3S_Alpha.apk` 0.6.0 c9 (V6, **before** Codex V5 art) | `Docs/V6_RELEASE.md` |

## Baseline test results (re-run 2026-11-08 on the current PC project)
| Suite | Result | Type |
|---|---|---|
| Core rules (`Core/SHHH.Core.Tests`) | **41/41** | pure rules (dotnet) |
| C# type-check of all Unity code | clean | `Tools/CompileCheck` |
| Unity compile | ok, 1 obsolete-API warning | `Automation/compile_report.json` 12:33 UTC |
| Codex book chore/sabotage/repair (`v5_book_runtime`) | **13/13** (seed 3) | desktop-simulated hands on real Boot rig, connected house |
| `xr_diag` hands + UI (practice room) | **149/150** (known early-pinch case) | desktop-simulated |
| Photon independent clients | last run V6 runs 6–8: 0 desyncs — **not re-run on Codex art** | desktop independent clients |

## Feature status (verified · desktop-simulated · visually inspected · Quest-tested · missing · broken)
| Area | Status | Notes |
|---|---|---|
| 4-seat rules, 4→3→2→1, causal noise blame, pets non-punitive, journal replication | desktop-simulated (Core tests) | V6 |
| Personal duties + reversible sabotage + foil/repair (place-in-socket only) | desktop-simulated | ~30 single-verb templates (`ChoreLibrary`) |
| **72 multi-step recipes (LV/KT/LD/BR/BT/FY/PT/HW)** | rules 72/72 (R); **recipe rounds are the default**; 61/72 reachable in generated rounds (each room stages its focus group: living +foyer +hallway, kitchen +pet area, laundry +bathroom +bedroom); **every one of the 61 completed by simulated hands with rival sabotage + owner foil in a recorded run (P)**; consolidated single-session run (`recipe_diag all`, 2026-11-09 13:2x): **57/61 completed, 56/61 pass every check** (KT01 one plate fall; LD04/LD08/BR08 fail only late in long batches - pass alone, R21; HW03 fixed after the run); 11 recipes need furniture-scale fixtures or unbuilt rooms (R12/R13, Gate 2); bots execute recipes with committed solver plans (REC-BOTS 36/36, MATCH-01R 100 matches) | `Scripts/Core/Recipes/*`, `Scripts/Game/World/Recipes/*`, `QA_Evidence/recipe_diag_all_20261109_1330.json`, `QA_Evidence/recipe_v7/` |
| Grip pose (thumb/index opposition) on held props | **desktop-simulated 9/9** | `SimpleHandVisual.GripSolve`: thumb/index opposed, gaps 1–7 mm, 0 fingertips inside on all 9 kinds (xr_diag 14:39, 158/159); child glove scale .13 |
| Practice UI / job cards | recipe rounds: object-local clipboard per station (owner colour, steps struck through, gesture hint, quiet tip, "messed with" alert); wall `YOUR JOB` board hidden in recipe rounds; practice tutorial still legacy | `RecipeClipboard` |
| Connected house (living + stairs + upper hall + Mom bedroom) | visually inspected (Codex) | kitchen/laundry art still procedural + Astra dressing; no bedrooms/bathroom/foyer/pet area |
| Mom: full-body Astra rig, warning/wake/inspect | desktop-simulated | no belief ledger, no stairs descent proof, no interrogation |
| Mom belief ledger / witness memory / playable interrogation | **missing** | Gate 5 |
| Bots: embodied, real grabs, sabotage wind-up tell | desktop-simulated | no social memory, no recipe mapping |
| Siblings detached head/torso/hands/shoes | **satisfied by Astra's cast (visually inspected)** | 2026-11-09 studio captures (`QA_Evidence/recipe_v7/siblings_*.png`): Pip/Nell/Rory/Tess are floating head, torso, hands and shoes on a Root-Chest-Head-Wrist-Foot rig; NOT a Unity Humanoid avatar (no hips/legs bones) - a humanoid-compatible mapping is still open (R17) |
| Photon private rooms 2–4 + bots + voice | **recipe rounds on the connected house: independent Win64 clients over Photon Cloud, 2H+2B 0 desyncs / 312 checksums (2026-11-09)**; 3H run in progress | `QA_Evidence/net_multi_v7_2H_recipes/` |
| LiveKit optional voice | **missing** (blocked: no backend/credentials) | Gate 7 |
| Family Stage / character select / customization depth | **desktop-simulated (P)**: physical Family Stage in the front hall (4 siblings in their saved outfits, "Play as …" poke picks one, visible beside the lectern from the spawn); wardrobe (hat/glasses/pyjama/accessory/emote) with unlock gating, encoded in the replicated cosmetic int so every peer sees it; bots dress up | `FamilyStage`, `MenuBuilder.ShowWardrobe`, `OutfitVisuals`, `recipe_diag stage` 4/4 |
| Recipe audio/VFX feedback | **desktop-simulated (P)**: original synthesised sounds (slot click, join pop, wipe, cloth rustle, pour, lid, step/recipe done, "uh-oh" when your job is messed with, card shown, pointing) + sparkle/bubbles/puff bursts, all driven from replicated recipe state so every peer hears/sees them; `recipe_diag` asserts they played (`feedback_sfx`) | `Tools/synth_audio_v7.py`, `RecipeFixtureView.Sounds`, `RecipeClipboard.Sounds` |
| Progression / achievements / save | **desktop-verified (R)**: local versioned save (atomic, .bak, corruption recovery, v1 migration), XP/levels, 6 achievements, 18 cosmetics unlocked by level/achievement, awards on the results board from the authoritative summary only, none in editor/QA/tutorial sessions | `Core/Progression.cs`, `Game/Progress/*`, PROG-* |
| Quest performance | **UNVERIFIED** (no headset) | |

## Gate progress
- Gate 0 audit & safeguard: **done**.
- Gate 1 hands: grip **9/9** (xr_diag 14:39 158/159; only the known early-pinch row). Still to do: object-local practice cards for the tutorial.
- Gate 4 recipes:
  - Rules (Core): model, world, board, solver (`Plan`/`NextOp`), generator (seeded weighted difficulty, station-fit filter), layout
    (rows/side plans, stacks, narrow pitch, loose piles, station assignment), round integration (journal op `RRecipe`, checksum, MStart flag,
    early inspection when all recipes done, fair points 60/recipe + speed bonus). Tests 127/127 incl. REC-MODE ×3 (+replication),
    REC-LAYOUT-POOL/SEEDS, REC-BOTS (36/36 complete, sabotage + foils), MATCH-01R (100 recipe matches 4→3→2→1).
  - Unity runtime (`Scripts/Game/World/Recipes`): `RecipeRuntime` (wipe by rubbing, fold/fill by rubbing a held item with the other hand,
    pair/attach by holding items together, pull apart to split, knocked-out-of-slot and knocked-crooked detection with last-toucher blame,
    followers ride their partner), `RecipeFixtureView` (slot pads + tag stickers, hinged lid + poke button, smudges, control buttons, snap
    with orientation check), `RecipeClipboard`, `RecipeVisuals` (art where it exists, labelled placeholders otherwise, art orientation
    normalised). Bots: `BotBrain.DecideRecipe` + `BotBody.ActRecipe` (place by careful release onto the slot, lids/controls by reach,
    joins while carrying). Network: clients send lid/control pokes via `NetLobby.RPC_Recipe` (host sets the actor); all other gestures are
    detected on the host from replicated hand poses.
  - Evidence: `recipe_diag` (play mode, simulated hands on the Boot rig, connected house): all 61 reachable recipes completed + rival sabotage +
    owner foil (per-area runs 2026-11-09 08:2x–11:1x; consolidated runs 12:1x and 13:2x, see TEST_EVIDENCE_INDEX). Gesture fixes from these runs: R19.
  - Open: 7 furniture-scale recipes (R12) + 32 recipes in unbuilt areas (R13); headless noise eliminations are rare in recipe mode
    (1/300 vs 39/600 legacy) → Mom/noise balance pass in Gate 5; recipe practice cards; Unity bot run (autopilot) + net_multi on recipes.
- Gate 5 Mom belief + confrontation: **Core + Unity loop working (R + P)**.
  - Core `Belief.cs`: incidents (decisive noises, sabotage actions) with WorldTruth kept for QA only; witness memories created only
    from host perception (journaled `ReportWitness`); Mom's belief ledger (heard / seen by Mom / inspected / witness card / pointing);
    `Interrogation` (8 s question + 25 s evidence window, ends early when everyone is done); `VerdictCalculator` (card of THIS incident
    1.0×conf, Mom's own sight 1.0, other-incident pattern 0.3, pointing 0.1 capped 0.2, location speculation 0.15; proof ≥ 0.7 with
    0.25 margin; otherwise fair fallback = existing chore-contribution inspection / tie-break). Recipe rounds only (legacy V6 keeps its
    verdicts). Sibling wake → confrontation; pet-only wake → warning; timer/all-done inspection asks about the worst unrepaired sabotage.
  - Bots present their true memories / sometimes point at the leader / pass (same journaled calls).
  - Unity: `PerceptionService` (host; 6 m, FOV 140° bots / 110° players, environment line of sight), `ConfrontationPanel` (tray:
    question + countdown, own memory cards, point-at buttons, nothing to add, cards shown so far), Mom `Interrogating` state + lines,
    client RPC `NetLobby.RPC_Confront`.
  - Optional voice in the confrontation (LiveKit) not built (Gate 7).
- Gate 3 characters: siblings' detached-piece look already in Astra's cast (verified); Mom full-body Astra rig. Open: humanoid mapping (R17),
  customization depth (Gate 8).
- Gate 6 network: 2H+2B recipe match 0 desyncs (desktop clients, Photon Cloud). The netauto client now plays its recipe through
  network intents (grab/release/pokes/rub) and answers the confrontation (card/pass) - 3H/4H runs next.
- Full solo match in the Unity runtime (autopilot, desktop-simulated): 4→3→2→1 with recipes, sabotage, foils, witnesses, a proven
  "caught sabotaging" confrontation verdict and a winner (`QA_Evidence/playtest_recipe_autopilot_20261109.json`).
- Gate 8: progression + wardrobe + physical Family Stage done (R + P); recipe/confrontation sounds + VFX from replicated state done (P). Open: broader audio/VFX polish, Mom question VO lines (text-only today).
- Bug fixed 2026-11-09: Astra characters (siblings; Mom/pets if their face sits behind the head) were facing backwards while moving.
- Gates 2, 7, 9: open. APK `SHHH_alpha_0.6.1_c10` (09:35, before Family Stage / pairs / sounds) and `SHHH_alpha_0.6.2_c11.apk` (13:41, current; sha256 b78d35ec…569493f0, 122.2 MB) built release-signed; headset UNVERIFIED.

## How to work (unchanged tooling)
Cloud mirror `/home/claude/SHHH` (git) → `sync_pack.sh` → `_sync/*.tgz` → `Tools/cycle.sh` on the PC (Unity file bridge, `Tools/job.sh`).
Codex tooling also present: `Tools/v4_job.py <cmd>` (same bridge) with commands `v5_book_runtime`, `v5_walkthrough`, `v5_navigation`, …
