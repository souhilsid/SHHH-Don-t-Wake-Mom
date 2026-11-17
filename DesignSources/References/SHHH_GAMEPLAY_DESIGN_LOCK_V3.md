# SHHH! — DON'T WAKE MOM!
## Gameplay Design Lock v3.0 — Ready for Claude Code Implementation

**Status:** Twelve player decisions are USER-LOCKED. Supporting tuning rules below are **DESIGN DEFAULTS** recommended for implementation and playtesting; they are not new user approvals. The developer agent should not silently override the user-locked decisions.  
**Language:** English-only in-game.  
**Target:** Meta Quest 3 standalone, hand-tracking-first, online private multiplayer for 2–4 players, solo practice.  
**Technical baseline:** Unity 6, OpenXR/Meta XR Interaction SDK, Photon Fusion 2 Host Mode, Photon Voice, URP.  
**Brand:** User-approved SHHH! character-free house/moon/pillow logo. Night Navy `#19183F`, Sunny Yellow `#FFD65A`, Playful Coral `#FF7769`, Cozy Teal `#65C8C5`, Cartoon Purple `#7958BB`, Warm Cream `#FFF3DD`.

---

## A. USER-LOCKED DESIGN DECISIONS (DO NOT CHANGE)

1. **Independent competition.** Each sibling competes independently for chores, performance, and survival. This is not a cooperative team win.
2. **Immediate noise elimination.** When Mom wakes due to a qualifying player-caused incident, Mom identifies the person responsible for the decisive noise, eliminating that person.
3. **Quiet timer ending.** When no qualifying wake-up occurs by the timer (or all shared chores are completed), Mom inspects the chores and eliminates the least helpful sibling.
4. **Almost anything can be used for sabotage.** A physics sandbox allows improvised object-based pranks; do not reduce this to a handful of limited prank buttons.
5. **Sabotage responsibility.** The prank setter is blamed if their trap directly causes unavoidable noise; otherwise the careless player is blamed when meaningful counterplay existed.
6. **Separate rooms per round.** Load a separate playable room/Unity scene for each competitive round, keeping multiplayer state persistent.
7. **Shared + individual chores.** Common household objectives coexist with optional individual bonus chores.
8. **Cat and dog are independent NPCs.** Both can wander, interact with objects, and occasionally trigger household incidents.
9. **Eliminated-player spectator.** Spectators retain voice and harmless gestures/stickers but cannot affect the active match.
10. **Launch modes.** Private online multiplayer and solo practice only. No public matchmaking at launch.
11. **Locomotion.** Hand-gesture movement, with optional teleport and snap turning. All controls must work without controllers.
12. **Mom presentation.** Fully animated, exaggerated cartoon mother with comedic cinematic reactions.

Other established commitments: One match is normally 4→3→2→1 (three rounds, one elimination per round); house chores + noise + pets + sibling sabotage are the game pillars; English-only presentation; immersive open-world-style **diegetic spatial UI** rather than permanent conventional 2D/head-locked HUD; original family-friendly styling, use approved logo; free/authorized assets and documentation.

## B. CORE PILLARS AND WIN CONDITION

**Tagline:** Four siblings. One sleeping Mom. Three eliminations. One survivor.

**Micro loop (seconds):** Pick chore → perform tactile hand interaction → recognize a pet/sibling/physics complication → react or sabotage → generate/protect against noise → earn contribution or suffer consequence → move to next chore.

**Round loop (roughly two minutes):** Room loads → 3-second countdown → race to finish shared and bonus chores → handle pets and sabotages → either Mom wakes or all shared chores finish or timer expires → Mom resolves incident or inspects the work → precisely one player eliminated → survivors move to next room.

**Match loop:** Main menu → private lobby → cosmetic sibling selection and ready check → Mom's English intro → Living Room (4) → elimination (3) → Kitchen (3) → elimination (2) → Laundry (2) → elimination → winner presentation → match statistics, funny awards → same-party rematch with new generated tasks/incident seeds. For smaller parties start at the appropriate condensed format.

**Victory:** The last surviving sibling wins, regardless of cumulative score. Performance score is primarily used for quiet inspections and statistics, not as a replacement for elimination.

**NO:** city-building, HARRIK Energy, currency-chest gameplay, team victory, team mission completion or three-star level reward screens. Prior erroneous generated visuals depicting those systems are noncanonical.

## C. SCENES AND PHASES

| Runtime scene / state | Purpose / contents | Key transition |
|---|---|---|
| `Bootstrap` | XR, safe persistence, services, audio, network configuration, loading | Ready → frontend |
| `FrontEnd` | Cozy house menu, exact approved logo, Play, Customize, Practice, Settings, Exit | Play → private lobby |
| `Lobby` | Invite code, 2–4 networked sibling avatars, voice status, Ready, host Start | All ready + host starts |
| `CharacterSelect` | Life-size 3D cosmetic avatar; outfit colors/hairstyle, no stats | Confirm → Lobby |
| `Intro` (state) | Mom sets chore/quiet rule; short skippable first-time scene | Intro done → room 1 |
| `LivingRoom_R1` | 4 players; sofa/books/toys/cups; dog & cat; local choreboard | Wake / chores done / timeout → resolve |
| `MomInvestigation` (state) | Mom wakes or inspects; physical room freezes safely; culprit evidence/replay | Resolve → eliminate exactly one |
| `Kitchen_R2` | 3 players; dishes/utensils/cabinets/pet chores, rising noise sensitivity | Same resolution |
| `Laundry_R3` | 2 players; towels/washer/laundry/shoes; final higher-pressure tasks | Resolve → sole survivor |
| `Spectator` (persistent player role) | Observe from safe viewpoints, voice/stickers/gestures, no gameplay collision | Match result / re-lobby |
| `Winner` + `Results` (states) | Cartoon Mom response, one winning sibling, in-world scoreboard, accident highlight | Rematch / Lobby / Menu |
| `SoloPractice` | Instructional room, one player, no competitive elimination; grabbing, placing, noise and pet tutorials | Player exit |

**Technical scene rule:** Maintain a persistent `NetworkRunner`, party roster, Voice session, score ledger, and match state. Only the Fusion Host/scene authority loads and unloads room scenes through Fusion scene management. Non-authoritative clients wait for scene-ready acknowledgements; disable gameplay input until all active clients are synchronized or a timeout policy fires. Use additive room loading or equivalent controlled scene transitions. Do not have clients independently decide room changes. Recreate the bedroom-door motif in each standalone room so the fiction remains coherent.

## D. ROUND DEFAULTS / TASK BUDGETS

These are first-pass balancing defaults, not hard requirements:

| Round | Survivors | Room | Countdown | Initial shared tasks | Individual bonus offers | Pet activity | Noise multiplier |
|---|---:|---|---:|---:|---|---|---:|
| R1 | 4 | Living room | 150s | 8 | 1 per sibling | Both; gentle | 1.00 |
| R2 | 3 | Kitchen | 130s | 7 | 1 per sibling | Both; moderate | 1.15 |
| R3 | 2 | Laundry | 110s | 5 | 1 per sibling | Both; moderate | 1.30 |

Avoid excessive task density: generate tasks so successful participants can typically finish common chores before timeout, allowing some room for mistakes. Each task should be completable alone, or offer a slower solo alternative when assistance would otherwise be needed. Bonuses never block common chore completion.

**Join counts:** 4 players → full R1/R2/R3. 3 players → Kitchen then Laundry, 3→2→1. 2 players → balanced Laundry final (2→1). 1 player → Solo Practice only. If party size shrinks via disconnection, reduce to an appropriate next-round structure.

**Round setup:** Deterministic seed selected by host, valid spawn locations, task graph, 3-second countdown, clear physical taskboard, pets spawned at reachable locations, all interactables safely reset from previous scene.

**End conditions / deterministic precedence:** (1) if one participant remains, finish match; (2) valid attributed wake event; (3) all mandatory shared chores complete; (4) countdown expires. Freeze input after the first authoritative resolved condition. Completing bonus chores is not required for a quiet early inspection. No double elimination in one round.

## E. CHORES: TYPES, MECHANICS, CREDIT

**Six categories** (each with different interactions):
- **Sorting/organizing:** books, toy bricks, shoes, cutlery, folded clothes.
- **Precision placing/stacking:** plates, cups, picture frames, books, ceramic figurines that are inanimate décor (avoid living figurines if inappropriate).
- **Carry/balance:** tray of cups, laundry basket, pan, pile of towels.
- **Operate carefully:** squeaky drawer/door, washing machine door, faucet, light switch, drawer handle, safe knob.
- **Cleaning/repair:** wipe spill, fold towel, gather laundry, align crooked frame.
- **Pet care/intervention:** fill bowl, retrieve toy, distract pet from shelf, return pet to designated bed area.

**Illustrative room libraries:**

Living room: sofa cushions, bookshelves, remote on marked shelf, collect toys, line up shoes, stack magazines, straighten picture frame, tidy coffee table, retrieve dog toy, calm cat by toy distraction.

Kitchen: stack plates, wash cups, sort cutlery, carry pot, place mugs in cupboard, set dining table, wipe spill, close cabinet quietly, return pet dish, feed cat/dog, remove squeaky toy.

Laundry: fold towels, pair socks, load washer, close washer door gently, place clothes in two labeled baskets, move laundry basket, organize shoes, clean lint area, put pet supplies away.

**Task instance data:** `TaskTemplateId`, difficulty, target room, authorized object types, valid spawn anchors, target anchors, action steps/substeps, progress, state, estimated work-seconds, noise risk, interaction constraints, contribution ledger, recovery/reset rule, pet hooks, task dependencies, network object refs.

**Task lifecycle:** Available → InProgress → Completed, with optional Interrupted → Recovering → InProgress. If a prop becomes unreachable, recover at last valid anchor (not infinite failure). Only validated actions count; hovering/holding without progress does not.

**Generator rules:** seeded and host-authoritative; distribute easy/medium/hard by time cost and noise risk; no unreachable spawns, inaccessible props, mutually blocked stations, impossible item sequences, unintentional two-player requirements, duplicated unique props, or objects beyond comfortable reach. Generate all clients from networked seed/data and reconcile with authoritative object states. Stage some tasks after earlier completion without giving one player secret unfair information.

**Shared scores:** successful task yields 10 (easy), 20 (medium), or 30 (hard) contribution points. Split among actual participants proportional to validated work steps; do not award everyone equal points just for being nearby. Each task's total contribution is credited once. Undoing work removes or suspends the associated completion credit until restored. Never farm points by repeatedly dropping and replacing the same object.

**Personal bonus chores:** one optional bonus offer per active sibling per round. They are private or individually tagged; completion adds a proposed +12 points maximum per round. They cannot replace minimum participation in the shared chores, cannot block other players from completing the shared board, and need valid solo execution. These offers encourage strategy, not a separate winner condition.

**Rescues:** optional +5 for a verified meaningful rescue, capped +15/round to prevent point farming.

## F. INSPECTION PERFORMANCE SCORE — DESIGN DEFAULT

For player i in the current round:

`Score_i = SharedContribution_i + PersonalBonus_i + Rescue_i - 0.35 * AttributedNoise_i - InactivityPenalty_i`

- The credited shared-work points are recorded per validated substep.
- `PersonalBonus_i` is bounded at 12 for one bonus chore.
- `Rescue_i` is bounded at 15 per round and never earned from self-created repeated danger.
- `AttributedNoise_i` is all authoritative responsibility-weighted noise during the round, not raw local audio volume.
- `InactivityPenalty`: after warning, -10 per verified idle episode, capped at -20/round, excluding tracking/network loss, transit, meaningful assistance, and waiting on dependencies.
- **No direct score reward for sabotage.** Its value is tactical disruption, time pressure, and skillful risk; its risks are noise blame and losing valuable task time.

When Mom inspects quietly (time runs out or shared chores all completed), the **lowest Score** is eliminated. Explain the winner criterion to every player before start. On a bottom-place tie, compare higher shared-work contribution, then lower attributed noise; if still tied, trigger a 20-second simple deterministic tie-break chore between tied players, rather than choosing randomly.

These values are tuning constants; metrics should track whether bonus tasks or rescue points dominate shared chores.

## G. MOM'S SLEEP / NOISE SYSTEM

`SleepMeter` range 0–100, network-authoritative. Each sound-generating physics event adds `NoiseValue` according to material, impulse, distance to bedroom, room acoustics, and round modifier. It is **not derived from Photon Voice microphone loudness or the user's device volume**.

Suggested initial zones:
- 0–34: deep sleep, snoring.
- 35–64: stirs, bed creak, softer warning.
- 65–84: suspicious, audible comment.
- 85–99: nearly awake, animated bedroom door/light warning.
- >=100: authoritative wake event and investigation.

Suggested decay: after 4 continuous seconds without qualifying noise, subtract 3 points/second down to round-specific baseline; adjustable with tests. Distinguish `audible SFX` from `gameplay NoiseValue` for consistency. Material profiles and impulse categories define both.

**Decisive noise definition:** the authoritative collision/interaction causal chain that pushes the meter across the wake threshold, including direct instigator/trap origin. To avoid an unfair +1 noise event taking blame for earlier major collisions, a wake candidate must be **qualifying** (proposed at least 8 noise points), or responsibility is attributed to the largest recent qualifying causal chain in the last 8 seconds. This is a fairness implementation of 'responsible for the decisive noise,' not 'always the person who touched something last.' Freeze event processing at the winning authoritative network tick; resolve simultaneous events deterministically.

**Autonomous-pet-only wake:** Mom checks the room/gets irritated but does not eliminate an innocent sibling; reset meter to e.g. 60 and temporarily calm the pet, resume round. If the player provoked the pet through a valid causal chain, treat it as a player-sourced incident and apply standard blame.

**Mom state machine:** Sleeping → Stirring → Suspicious → AlmostAwake → Awake → Investigation → Elimination or Warning → ReturnsToBed; separate quiet timer inspection pathway. Fully animated facial/body gestures and authored English lines. Do not use a generative LLM to decide guilt or elimination.

**Mom narrative:** The same Mom is napping near each round room (room-specific bedroom door/hall connection). After removing one sibling she returns to bed; the survivors move to the next gameplay room. Her reactions become funnier and more sensitive each round.

## H. CAUSAL BLAME / SABOTAGE

**Physics sandbox:** Free player manipulation of permitted rigid-body household objects: lift, move, hide in valid spots, nudge, stack, topple, block a route within navigable limits, open/close doors, manipulate pet toys, set up improvised physical traps. Do not limit all sabotage to a fixed number of ability charges. Some specialized high-impact authored interactions can have cooldowns for network stability.

**Safety and fairness constraints:** Objects cannot be placed outside level bounds, permanently hidden in walls, clipped through geometry, made inaccessible, or used to immobilize another player's avatar. Static structure and critical task anchors remain protected. Quest players cannot physically push another player's camera rig/body into dangerous or nauseating movement. Recover lost objects after a short grace. Pranks must have some readable gameplay consequences or cues when counterplay is expected.

**Causal event ledger:** Host records `eventId`, `networkTick`, `instigator`, `lastInteractor`, `sourceObject`, `impactObject`, `parentEventId`, `trapOwner`, `counterplayAvailable`, `noiseValue`, `responsibilityWeights`, and chain expiry. Keep short causal windows and depth limits to avoid attributing the entire game to an ancient touch. Always use host-side impacts, not arbitrary client reports.

**Explicit guilt rules:**
1. Direct careless drop/slam/throw → actor responsible.
2. Player builds/arms a trap that makes noise without meaningful victim counterplay → trap setter responsible.
3. Victim visibly has a feasible warning/countermeasure and negligently slams or drops the object → careless actor responsible, not necessarily setter.
4. Multiple players deliberately contribute to one chain → recorded causal shares; decisive chain's principal contributor is responsible.
5. Pet naturally causes incident without provocation → environment/unattributed, no human elimination.
6. Player lures pet using a toy resulting in foreseeable crash → luring player attributed if supported by event chain.
7. Ambiguous collision with no trustworthy chain → don't arbitrarily eliminate; Mom warning and controlled meter reduction or defer to normal inspection.

**No unfair griefing:** No direct force-on-avatar, no dragging a critical task item through closed walls, no camping with a necessary item until it disappears, no repeated credit from resetting tasks, no unconstrained sound-spam scripts. Enforce object recovery and objective availability; apply sensible physics limits while preserving physical playfulness.

## I. PET AI — BOTH CAT AND DOG

Cat: wanders, observes moving items, approaches shelves, chases toys, may knock small objects if area is reachable.

Dog: wanders, follows toys, grabs harmless items, may interrupt routes or retrieve cloth/toy objects.

Pet state machine: Idle → Patrol → Investigate → Play/Interact → Startled → Calm/Rest. All gameplay decisions run on host; visuals/interpolation locally. Pet incidents are occasional and understandable, not perpetual chaotic noise. Players can distract/redirect pets and receive legitimate pet chores. Pets cannot intentionally target a specific player merely because of ranking. Do not use harmful interactions with pets.

Performance: bounded number of active pet interactions and physics-trigger events; avoid full ragdoll simulations for ordinary roaming. Reuse animation states and lightweight pathfinding.

## J. HANDS-FIRST LOCOMOTION / GRABBING

Primary: intentional gesture-controlled locomotion. Default proposed mapping: on an **empty hand**, palm-up + pinch-hold (e.g. 0.4s) enters a movement gesture state; modest hand tilt gives a comfort-limited travel vector, releasing pinch stops immediately. Do not activate movement while an object is currently grabbed with that hand; require gesture disambiguation and allow remapping. Use an independently accessible hand-based teleport arc to floor/safe anchors and spatial snap-turn controls (e.g., wrist menu buttons) for comfort. Test and tune in headset before freezing specific gestures.

Use Meta's XR hand interactions for grab/poke/throw and spatial UI; all menus usable with hands. Never require Quest controllers. Protect against tracking loss: held props must not be flung/drop explosively, locomotion stops, interaction resumes safely. Use optional seated comfort settings, accessible interaction ranges, dark/clear visual feedback, speed limits, and no forced camera rotation in cinematics.

## K. SPATIAL UI / BRAND / AUDIO-VISUAL

**In-world first:** Physical wall/notice boards, room clocks, fridge notes, wrist card or glanceable physical sleep cue. Sound ripples spawn at noisy objects; trap warnings near affected objects; Mom enters through actual doorway; elimination/victory information staged in room. Avoid persistent head-locked HUD or floating desktop-style windows obstructing VR view. Brief readable cinematic labels allowed only at pivotal moments and anchored comfortably in world space.

English-only all game text and dialogue. Exact approved logo (house, moon, pillow, yellow SHHH!) on branding/main menu only as appropriate; do not reproduce it everywhere in the gameplay room. Character proportions and clothing original and consistent, not copied from protected cartoons. Remote sibling avatars are in-world characters; don't require their models to visibly wear VR headsets merely because the real players are using Quest.

Audio: household Foley is core to play. Multiple SFX variants for ceramic/metal/wood/fabric/toys/pets, spatial attenuation, clear Mom warning voice, sound propagation effects; microphone voices stay completely separate from Mom's NoiseMeter. Sound should help identify danger without constant music; background music requires explicit user permission. Physics and sound both follow host event IDs for consistent scoring.

VFX: glowing grab affordances, placement previews, cartoon wobble, small impact stars, radial sound ripples localized in 3D, pet and trap clues, bedroom lighting and door drama. Optimize URP effects for Quest 3; avoid expensive full-screen post effects and clutter.

## L. MULTIPLAYER / TECHNOLOGY / AUTHORITY

Unity 6 + URP + Quest 3 standalone, OpenXR/Meta XR Interaction SDK, Photon Fusion 2 Host Mode + Photon Voice. Game must connect through private invite codes, maximum four players. Host governs match phase, scores, objects, noise, pet/mother AI, room selection, elimination, and randomized generation. Locally predict hand interaction feedback if needed; reconcile host authority for game state. Never trust arbitrary client 'noise' or 'culprit' messages.

Persistent network state: player IDs/names/cosmetics, ready status, alive status, contribution ledger, historic noise summary, Voice room membership, round index, spectator role, retained match stats. Room props are scene-owned and reset/regenerated per round. All clients wait for room load before countdown. Photon scene manager and `NetworkRunner` remain coherent during transitions.

Private join before game, no normal late joining to ongoing competition; optional spectating from an invite if feature ready. Solo practice uses no internet session dependency. If host leaves, implement a clear match-cancel/return-to-lobby state unless true host migration has been implemented and tested. Don't pretend host migration already exists.

Disconnects: grace period ~20 seconds, safe held-object release and temporary avatar freeze; no inactivity penalty due to disconnection. Permanent withdrawal counts as elimination for that round—do not also eliminate another competitor during that resolution. If one player remains, declare winner. Never let a disappeared client retain control over scene props.

Voice includes mute, adjustable volume, clear feedback, no voice recordings in baseline build. Game-scored noise never uses speech volume.

## M. SPECTATOR, RESULTS, REMATCH

Eliminated players are passive spectators and may speak or trigger local/non-colliding stickers/emotes; cannot cause household physics, influence pets, make game-scored noise or relay unfair game-state information via privileged views. Spectator camera switching and spatial comfort handling.

Results show survival ranking, shared task contribution, personal bonuses, attributed noise, rescues, funniest incident, pet interactions, and a comic replay/evidence label. Only one winner. Keep same party when rematching; generate new seeded task arrangements and pet incidents. Cosmetic progression, if present, must not change grip strength, speed, interaction range, or game scores.

## N. QA AND ACCEPTANCE — MUST PASS BEFORE 'FULLY PLAYABLE'

1. A Quest 3 standalone APK installs, launches, and runs entirely without controllers.
2. Player can intentionally locomote using gestures and use teleport/snap turning without accidental travel while gripping an item.
3. Shared chores and personal bonuses update correct player credit across networked clients.
4. No scores can be farmed by drop/re-grab/reset loops.
5. A direct dropped pan wakes Mom and eliminates the responsible sibling.
6. A player-set unavoidable trap eliminates its setter, not an innocent victim.
7. An avoidable trap with clear feedback can penalize a careless victim according to recorded actions.
8. An independently acting cat or dog cannot automatically eliminate a sibling.
9. Host and clients load a separate Kitchen scene after R1 while keeping voice, players, elimination, and match state.
10. Entire four-player test sequence progresses 4→3→2→1 with exactly one elimination per round.
11. Quiet timer expiry and early shared-task completion trigger Mom inspection and lowest-score elimination.
12. Ties resolve via published deterministic rules / brief tie-break, not arbitrary remote client selection.
13. Spectators cannot move props or influence active scoring, but retain allowed voice and non-physical reactions.
14. Hand tracking loss and reconnection do not cause explosive item throws or unearned penalties.
15. A host disconnect has an explicit gracefully handled result; clients do not remain stuck in invalid scenes.
16. Every scene has English-only physical/spatial UI; no permanent full-screen HUD.
17. VFX/SFX are audible/visible yet performance-safe and align with host events.
18. Audio volume/microphone sensitivity does not modify noise points.
19. Quest performance tested in headset; target stable 72 FPS, progressively optimize toward higher refresh.
20. Rematch preserves party while resetting competition, tasks, sound meter, props, and player state.

## O. DEVELOPMENT AGENT RULES

1. This document is the **gameplay authority**. No reinterpretation into cooperative team victory or a resource/city-building game.
2. User's 12 locked decisions are non-negotiable unless the user explicitly changes them.
3. Implementation defaults are adjustable by verified playtesting; changes must be documented and not silently change locked semantics.
4. Use licensed, authorized assets only; track source/license; maintain coherent cozy visual style.
5. Build true VR game scenes, not full-screen mockup screenshots. All interaction must be present in a working Quest APK.
6. Use iterative host-client tests, log facts, and distinguish implemented/tested/blocked, not broad 'completed' claims.
7. Never require the user to manually operate Unity; run Editor operations via approved automation/CLI. Ask only for account authentication, device authorization or actual headset playtesting.
