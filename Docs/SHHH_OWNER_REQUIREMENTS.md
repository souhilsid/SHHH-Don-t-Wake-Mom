# SHHH! — owner requirements in force (durable record)

Last updated 2026-11-10 (V10 override on top; V9 below). Sources, newest first: `Docs/Production/SHHH_V9_PHYSICS_ASSETS_VFX_MASTER_CONTINUATION_2026-11-10.md`,
`Docs/Production/SHHH_V8_GENIES_PRIMARY_CONTINUATION_2026-11-09.md`, `Docs/Production/SHHH_CLAUDE_ONE_SHOT_COMPLETE_MASTER_PROMPT_2026-11-09.md`
(its Section 04 holds the full step/sabotage/repair text of all 72 recipes). Precedence: latest explicit owner instruction > V9 > V8 > Nov 9 master
prompt where compatible > verified live source > older docs. Where live source and a document differ, reconcile and record the migration here.

## V11 — owner directive 2026-11-10 evening (HIGHEST PRIORITY; V11 > V10 > V9 > V8 > Nov 9)

Source: `Docs/Production/SHHH_V11_PHYSICAL_INTERACTION_ENGINE_DIRECTIVE_2026-11-10.md`.

| # | Decision |
|---|---|
| V11-1 | Physical interaction quality is the top priority; do not move to basic laundry until dishwashing is complete and proven. |
| V11-2 | Dishwashing blockers to fix: close-object selection, grip stability/slipping, furniture snagging, both plates in the rack, duplicated impact noise, plate orientation, face-on stain proof. |
| V11-3 | Water rebuilt as gameplay-linked reservoirs, volume, flow, fill, pouring, splash, droplets, wetness, puddles, foam; original toon look; Quest-practical. |
| V11-4 | Shared interaction foundation for all 26 primitives (handles, drawers, doors, knobs, hinges, carry, pour, squeeze, scrub/wipe/sweep/mop/spray, fabric, stack/balance/catch/recover, appliances). No poke substitutes. |
| V11-5 | Central RecipeLayout staging removed from gameplay for all tasks; real house locations with navigation and recovery. |
| V11-6 | Owner archives re-attached (Ultimate House Interior, daycare 360 GLB, Nasha kids room, StylizedFoodPack unitypackage, KayKit Restaurant, laundry utility, Tiny Treats) to be imported and upgraded to the hand-painted style. |
| V11-7 | Report rules / Editor / multi-instance Photon / APK / Quest separately; Genies, Photon, Mom, bots, sabotage, scoring, continuous match preserved. |

## V10 — owner override 2026-11-10 (HIGHEST PRIORITY; precedence V10 > V9 > V8 > Nov 9 master)

Source: `Docs/Production/SHHH_V10_REAL_HOUSE_PHYSICS_ART_INTERACTIONS_DIRECTIVE_2026-11-10.md` (owner: "I am explicitly rejecting the current quality of
SHHH!'s physics and household interactions"). Resume point: `Docs/SHHH_V10_RECOVERY_STATE.md`.

| # | Decision | Supersedes |
|---|---|---|
| V10-1 | No poke-button gameplay and no basic grab-and-place chores: each meaningful object has the interaction of its real function (handles, rails, rotary valves, spring plungers, latches, finite containers, cloth corners, stacking). | Poke keys for lids/taps/drawers; rub-gesture stages |
| V10-2 | **Remove the central task-spawning table.** Tasks exist naturally across the connected house; players travel to collect, move, operate, clean and return objects. Task UI informs only. | V5 `mess_table`/`V5_TaskTable` stations, clipboard stations, recipe props spawned on station tops |
| V10-3 | Water: original stylized toon shaders, substantial animated streams (width, taper, flow, foam, droplets), finite volumes in containers, splashes, puddles, wetness; physically meaningful (flow from valve angle). | Thin LineRenderer-style stream |
| V10-4 | All interactive models, stains, waste, food, laundry and VFX hand-painted in the SHHH! cartoon style; owner packs are imported, adapted, rigged, textured and integrated, not dropped in raw. | Raw stock-pack look |
| V10-5 | Applies to all 26 primitives, all 72 tasks and the 12 physics comedy incidents, with sabotage, recovery, contextual audio, haptics, shaders, Genies hand animation, Photon sync and Mom's causal reactions. | — |
| V10-6 | Order: recover -> fix dish-rack slot 2 + 441-clatter audio -> complete polished dishwashing in the real kitchen as the tested foundation -> every other mechanic. | V9 ordering where different |
| V10-7 | Requirements live in project files (CLAUDE.md, .claude/rules, this file, asset manifest, physics contract, recovery state) so every session recovers them. | Conversational memory |
| V10-8 | Do not restart; do not destroy Genies/Photon; no mockups or rules-only tests standing in for real Unity interactions; Quest validation stays UNTESTED until actually performed. | — |

## A. Decisions (owner-approved; do not re-ask)

| # | Decision | Since | Supersedes |
|---|---|---|---|
| A1 | **Genies Avatar SDK is the primary** avatar creator, customization editor, wardrobe, clothing and persistence system, used everywhere (stage, match, remote players, bots, podium). Pip/Nell/Rory/Tess are seats with starter looks; player-made Genies looks are allowed. | V8 2026-11-09 | Custom Blender sibling bodies as primary |
| A2 | Connected full-body **or** floating/disconnected bodies are both acceptable — pick what gives reliable VR embodiment and quality. | V8 | Mandatory floating anatomy |
| A3 | **One continuous 5- or 10-minute match** through a fully connected, explorable house. Four seats; solo with bots; private multiplayer up to four humans; no permanent within-match eliminations. | Nov 9 | The 4→3→2→1 elimination loop |
| A4 | **Physical household gameplay is the top priority**: all 26 primitives, all 72 recipes (stable IDs), all 12 comedy incidents, with human/bot parity, sabotage and physical repair. | V9 2026-11-10 | — |
| A5 | Important objects are **real imported/refined 3D assets** with working physics, grips, materials, state and VFX — no primitive placeholder appliances as final content. Reuse the owner's attachments and installed licensed packages (installed ≠ "in My Assets"). | V9 | Primitive grey-box fixtures |
| A6 | No "touch = done", no cosmetic-only streams/particles, no physics in name only, no transform teleporting, no T-pose, no nonfunctional wardrobe, no fake multiplayer. | V9 | — |
| A7 | **Original adaptive score and SFX** (V8 audio redesign); earlier game audio is rejected. Noise affects Mom and the evidence ledger. | Nov 9 / V8 | Pre-V8 audio |
| A8 | Never call Editor simulation a Quest test; preserve the last working APK; never expose credentials; never destroy source assets; report exactly what ran. | all | — |
| A9 | Mom is a separate full-body NPC; pets stay pets (Biscuit, Mochi). Mom's verdicts are deterministic and bounded by sensed/witnessed evidence (no LLM-invented verdicts). | V8/Nov 9 | — |
| A10 | Visual direction: SHHH! Soft-Sculpted Storybook Toon — rounded silhouettes, painterly surfaces, cream/warm wood, lilac/plum and coral accents, cool moonlight + amber lamps; family-safe cartoon (no realistic knife/fire/electrical hazards, no camera shake/slip). | Nov 9 | — |
| A11 | Quest-practical physics is accepted when state follows real actions: coverage masks, finite liquid volumes with gravity-respecting streams, constrained cloth/skinning, rigidbodies and joints — not CFD or full cloth FEM. | V9 | — |

## B. Acceptance bar (V9 §11)

A player enters with their saved Genies avatar, grabs the actual tool with aligned hands, contacts a real imported object, physically produces a
believable change, hears/sees localized feedback, can be interrupted or sabotaged, repairs the consequence, and a bot or a remote Photon
player sees the same world truth while Mom reacts only to justified evidence — for every chore and incident, not only dishwashing.

Status words (never conflated): IMPLEMENTED IN SOURCE · RULES TEST PASSED · UNITY PLAYMODE VERIFIED · TWO-CLIENT VERIFIED · APK BUILT · REAL QUEST VERIFIED.

## C. Execution gates (V9 §10) and where we are

| Gate | Scope | State 2026-11-10 |
|---|---|---|
| A | Recover `_sync/v9m.tgz`, compile, Genies/Photon preserved, first real dishwashing PlayMode run | **Done**: v9m verified fully applied (24/24 files identical), compile OK, KT01 PlayMode run by simulated hands completed tap→sponge→rinse→rack→tap off (run v9m) |
| B | Asset + FX audit, licenses, staged selective import | **In progress**: licenses verified for all reattached archives; daycare KIT015/KIT018/KIT026 converted + imported (dishwashing); manifest started |
| C | Physics foundation + dishwashing polished vertical slice | **In progress** (see `SHHH_PHYSICS_CONTRACT.md`) |
| D | Laundry, floor cleaning, tray balance, toy chaos, kitchen prep flagships | Not started |
| E | All 72 recipes on the primitive contracts; 11 blocked recipes get real furniture | Not started (coverage CSV tracks every ID) |
| F | 12 physical comedy incidents + social stealth | Not started (soap overflow detector logged only) |
| G | Multiplayer 3–4 seats, Genies hands, performance | Partly (2-instance Genies look sync verified earlier) |
| H | Final QA, signed APK, docs | Last APK c12 (pre-V8 features); no V8/V9 APK yet |

## D. Standing constraints carried forward
- Photon Fusion (host mode) is the transport; one authority writes physical task state; clients send intents/hand poses; compact replicated state only.
- Genies: no SDK internals patched, no meshes extracted, no purchase/unlock screens; `GeniesAuthSettings.bytes` never printed.
- LiveKit: no long-lived keys in clients (blocked until a token backend exists).
- Nasha's Cozy Kids Room: custom license — use in-game only; no redistribution as assets; no AI/ML training.
- Classroom-VR (`C:\Users\aisci\Downloads\Classroom-VR-main`, read-only grant 2026-11-10): Unity 2020.3 + PUN reference; only the VRMap concept (network head/hand poses drive body IK targets) was used.
- Store app IDs are never invented; signing secrets never printed.
