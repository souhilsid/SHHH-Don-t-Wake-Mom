# SHHH! — DON'T WAKE MOM!
## CLAUDE CODE MASTER PRODUCTION DIRECTIVE — FINAL LOCKED GAME SPECIFICATION
### Unity 6 • Standalone Meta Quest 2/3/3S • Handcrafted Stylized 3D • Physical Comedy • Social Stealth • Photon Multiplayer • LiveKit Voice AI

**Authority:** This document is an executable production brief. Work inside the existing SHHH! Unity repository. Your assignment is to deliver a functional, polished, playable standalone Quest game, source project, source artwork and credible test evidence. It is NOT a request for an architectural suggestion, generic design document, screenshot generator, placeholder scene, simulated UI, or trailer. Act on the project using available editor, shell, Blender, Unity and testing tools. Do not falsely claim anything was installed, rendered, compiled, run or tested.

**Owner's 16 final decisions (locked):** (1) hybrid locomotion with hand tracking, controller movement and comfort teleport; (2) sibling floating/disconnected visible head, torso, hands and shoes with internal logical/humanoid-compatible rigs; Mom conventional full-body humanoid; (3) shared sibling rig with personality-specific animation and modular customization; (4) **60+ complete procedural household activities**; (5) 2–5 meaningful physical steps per activity; (6) reversible chore sabotage, environmental traps, pet distractions, object manipulation, cover-up and rescue, with fair blame; (7) connected accessible house, task focus changes by round; (8) hybrid deterministic bot decision/planning with optional AI dialogue; (9) Mom investigates actual events with bounded perception and one major confrontation each round; (10) optional human spatial voice plus optional LiveKit Mom/bot voice, all with nonvoice alternatives; (11) replace disconnected seats with bots and give eliminated humans harmless spectating; (12) modular wardrobe, glasses, hats, emotes and accessory customization; (13) lightweight persisted XP/cosmetics/achievements, no assumed monetization; (14) dynamic stealth-comedy soundscape; (15) living, kitchen, laundry, Mom's room, children's bedrooms, bathroom, foyer, corridors, stairs and pet area; (16) preserve locked rules/art identity, creatively refine uncommitted details.

**Production ethics:** Do not run anything destructively without a recoverable backup. Do not overwrite verified working scenes, prefabs or gameplay scripts merely to simplify your job. Do not expose Photon/LiveKit/API secrets in the project, logs, builds or screenshots. Do not install or download copyrighted game assets without a valid license. Do not imply Quest headset certification from desktop emulation. In online play assume players may be children: privacy-aware voice and moderation defaults, minimal data storage and appropriate consent/parental controls must be considered for release.

---

## 00 — READ THIS FIRST: PROJECT HANDOFF, CAPABILITIES, STOPPING FALSE PROGRESS

This game has been developed across prior SHHH! versions and a Codex/Astra V4/V5 handcrafted-art recovery pass. **Continue that project; do not start another Unity project.** User-supplied history reports Unity **6000.0.62f1**, URP **17.0.4**, Meta XR **201**, OpenXR **1.16.1** and an installed `Stylized - Simple Hands` asset. These are historical snapshots, NOT a substitute for reading `Packages/manifest.json`, actual scene/prefabs and installed package versions in the current repo. Before architecture changes read `ProjectSettings/ProjectVersion.txt`, packages, XR setup, renderer assets, build target, assemblies, Photon versions and LiveKit presence.

Known V5 work from the handoff: improved handcrafted sofa, oak coffee table, bookshelf, books, lamp, and connected living room; hand visual bridge; room navigation to Mom's bedside; current books-on-shelf chore with rival sabotage and owner repair; 13 successful simulated desktop checks reported; some Blender/Unity import corrections; unfinished grip-pose correction and visual gate. Those are **reported**, not independently validated here. Independently reproduce from actual repo and results. Upper hall boundary and Mom bedroom were being corrected. Other rooms remained primitive. Explicitly audit the 3D pipeline's FBX axes, mirrored names/letters, face orientation, bake settings, material translation and object transforms.

**Required inspection evidence first:**
1. Print current git status; create timestamped backup branch or source archive; list editor version and package versions. Keep a machine-readable baseline inventory.
2. Locate existing Boot, GameRoot, Room runtime, GameServices, Core round controller, Mom director, task definitions, book prop and anchor scripts, Simple Hands integration, photon transport and bot controllers. Map contracts and public events.
3. Open actual working Unity scenes. Identify which is production gameplay, which is V4/V5 review-only, which contains the Family Stage, and which components spawn procedural props. Do not confuse review camera views with a shippable XR level.
4. Enumerate original nine task groups / 18 actions **as actual IDs if available**. Compare against the newer runtime/task code. Produce a compatibility map rather than clobbering identifiers. Legacy references are illustrative, not the final source of truth.
5. Read the approved brand ZIP, brand tokens JSON, primary full-color SVG, user-approved concept art (house, lobby, Mom, siblings), V1 world brief, V5 corrective brief and all QA screenshots. Build `Reference_Atlas.md` listing what each image controls and what it cannot prove.
6. Inspect installed skills and plugin/MCP connections. The text inventory shows about 42 named skills, not proof that all have functioning tools or valid slash invocation. Detect and use each as appropriate. Read skill instructions before use. Do not launch all skills at once.
7. Read the current Codex handoff / edited files and tests. Distinguish `verified`, `desktop-simulated`, `visually inspected`, `Quest-device-tested`, `missing`, and `broken` for every core feature.
8. Establish `Docs/PRODUCTION_STATE.md`, `Docs/DECISION_LOG.md`, `Docs/RISK_REGISTER.md`, `Docs/SKILL_ROUTING.md`, `Docs/ASSET_MANIFEST.csv`, `Docs/TASK_RECIPE_MATRIX.csv`, `Docs/TEST_EVIDENCE_INDEX.md` before significant modifications. Update continuously.

Do not use a successful console compilation, import, bake, automation log or Unity screenshot as proof of a playable VR mechanic. For everything that appears complete, demonstrate that the correct physical gesture triggers its state transition. Any missing headset/cloud/SDK access must be represented as a blocker with local partial evidence, not fabricated success.

### 00.1 Source-of-truth precedence and contradictions
1. These 16 owner choices and prior fixed SHHH! gameplay constraints outrank uncommitted design suggestions.
2. Actual functional code/contracts and stable IDs outrank old illustrative task names; migrate only with regression coverage.
3. The approved logo/brand kit and SHHH! concept images control appearance, palette, actor identity, and Family Stage motifs.
4. V5 handcrafted references control **craftsmanship**, not medieval/fantasy setting or expensive real-time render effects.
5. Hardware requirements and safety/comfort outrank prettier but unplayable imagery.
6. The game is **not** a generic 2D mobile menu, nor a realistic horror game, nor a fantasy village. Stay in a contemporary family house at night.

### 00.2 Deliverable completion definition
The shippable result boots to an interactive physical Family Stage, permits Solo and Private Friends (2–4 humans), fills empty sibling seats with embodied bots, provides functional character selection/customization, loads the connected house, runs 4→3→2→1 elimination, supplies varied chores and recoverable sabotage, drives genuine Mom confrontation, allows nonspoken evidence, resolves score/culpability and victory, saves rewards, and exits/restarts reliably. All 60+ activities must be reachable through an actual task generation system and have tested task/sabotage/repair state paths. Quest 2/3 runtime proof is needed for device-level acceptance.

---

## 01 — MASTER PRODUCT VISION AND NON-NEGOTIABLE GAME RULES

**Title:** SHHH! — DON'T WAKE MOM! (use the approved logo art; no arbitrary rebrand). **Genre:** first-person standalone VR competitive social-stealth domestic physics comedy with embodied AI siblings. **Premise:** Four siblings were supposed to be asleep. Mom is asleep in her own upstairs bedroom. They must quietly straighten the house. Finish chores, protect your reputation, cooperate tactically, disrupt others, and survive the increasingly suspicious Mom. Never describe elimination as violence; being sent to bed is funny and family-friendly. English UI/dialogue at release.

**Seats:** Pip, Nell, Rory, Tess. 1–4 humans possible; single-player is one human plus three autonomous bots. Online private room supports 2–4 humans; fill any remaining seats with bots. Where a player disconnects, preserve their identity/seat, give them a reconnection grace window and a controlled bot takeover if appropriate; don't double-possess a body. At round end, eliminate exactly one seat until one remains. Players who are sent to bed may spectate with harmless stickers/gestures, but cannot move real objects, feed evidence, speak to Mom as current witnesses, block active siblings, or affect match outcomes.

**Canonical match:** Three elimination rounds: four active players → three → two → one winner. Each round has an active chore/sabotage segment, escalating Mom warnings, one major playable interrogation or verdict confrontation, and one elimination. An early decisive noise can accelerate a confrontation; at timer expiration, Mom inspects accomplishments and available evidence. Timer should not produce indefinite waits. Players can still earn modest actual-help contribution credit. Threat and fairness scales with remaining siblings. Target first-version whole match ~12–16 minutes; make duration data-driven after playtesting, not hardcoded into scene art.

**Responsibility:** Each chore has an assigned owner and a physical true state. Any seat may assist or interfere. Helping does not change ownership. Completion is checked by actual authoritative object state, not merely by playing an animation or showing UI. Reversing a completed chore returns it to a disrupted state; it can be repaired. A successful sabotage can be hidden from Mom if genuinely unwitnessed. The game maintains `WorldTruth` and `MomBelief` separately. No omniscient blame based solely on server truth.

**Alliances:** Temporary informal cooperation and bilateral promises through contextual gestures and optional speech, non-binding, strategically reversible. Modest credit: suggested +5 per validated assisted chore and +10 contribution cap per round, tune after playtests. Players cannot gain credit by making a mess they themselves instantly repair or by repeatedly farming one prop. Betrayal has interpersonal consequences only when observed/inferred; no artificial automatic betrayal mark.

**Fairness:** Game state truth may be complete for host/server, but Mom/bots receive only legitimate sensed, heard, observed, inspected or reported information with confidence and provenance. Voice fluency does not generate a persuasion score. No player loses because they cannot or prefer not to speak. Noise, diligence, help, evidence and blame must be explainable by the game's rules.

---

## 02 — COMPLETE PLAYER JOURNEY AND EVERY PATH THAT MUST WORK

**Cold boot:** Splash and accessibility/readability, seated/standing calibration and input discovery; no unsafe camera acceleration. The player finds themself at a stationary theater-like Family Stage within the same cozy home aesthetic. The signature original SHHH! logo dominates the entrance and theatrical curtains reveal siblings. Hand ray + pinch selects Solo, Friends, Customize and Settings. Do not substitute five generic flat floating mobile buttons. If a tutorial exists it should be context-aware, skippable and physically meaningful.

**Tutorial / practice:** Introduce hand tracking via a book pickup and placement, deliberate quiet movement, a clearly controlled noise meter, repairing a displaced book, simple blame/witness mechanics and the difference between finger pinch, ray selection, direct poke and object grab. This practice is safe: Mom only grumbles and does not eliminate. Avoid a giant opaque `PRACTICE` wall of text; use short staged cards and object-level highlights. The existing `Moon Patrol` book practice screenshot demonstrates functioning choreography but also unreadable tiny instruction text and an oversized hand in some views; fix visual clarity and pose, preserve working task logic.

**Character selection:** Four siblings revealed physically in the Family Stage; pick Pip, Nell, Rory or Tess through real hand interactions; choices have charming reaction animation. Each character retains recognizably different silhouette and color. In Solo, only one human selects; bots occupy remaining seats. In Friends, identities and ready state synchronize and conflict resolution prevents duplicate selection except deliberately supported cosmetic duplication. The player can rotate/inspect chosen sibling, select cosmetics and save. Never invent store currencies simply because concept art depicts stars or gems.

**Friends flow:** Create private Photon room, generate/copy readable join code/invite where supported, friends join and see each other as embodied siblings; connection progress and error states are spatial UI. Players ready up; only valid owner/host starts. Interruption, version mismatch, room full, latency, host leave and reconnect have explicit feedback. A fake `Play with Friends` button or local mirror of multiple players is a failed feature.

**Round preparation:** Show short 3D chore cards or personalized clipboard with distinct owned tasks, helper opportunities, expected completion signals, room area, noise risk, and controls. All four siblings can walk within connected house. Mom is physically upstairs in enclosed bedroom, sleeping; not perched on an open balcony above the living room. Children may visit the task focus rooms; the remaining house still behaves as real architecture and can support incidental navigation/cover-up.

**Round active play:** Pick objects with real collision and tracked fingers, move around safely, combine low-level physical motions into multi-step household activities, help, negotiate, misdirect, repair and subtly sabotage. Chore progress updates when a valid physical state predicate is satisfied. The soundtrack/room acoustics convey tension. Disturbances may trigger Mom warnings (sleep movement/footsteps/voice), before one major confrontation. Pets wander, cause bounded occasional interactions and leave actual evidence; they are not random instant-kill machines.

**Confrontation:** Short playable interrogation, generally ~40–60 seconds, not a passive cinematic. Mom enters the relevant room or gathers siblings plausibly, points at disturbance, asks one or two contextual questions, and accepts gestures, witness-memory cards or optional voice. Evidence is grounded in world events; every statement gets source provenance. Mom's verdict is computed by transparent bounded criteria, spoken/acted believably. A player cannot invent an event by saying it. If the event is unresolvable, inspect chore contributions and responsibility under a deterministic fair fallback.

**Elimination:** Comedic send-to-bed walk, shrinking active roster, spectator transition, round-state reset without destroying persistent relationship/history. Review outcomes briefly without leaking hidden sabotage truth mid-match. Remaining players see reassignments, new room focus and faster tension curves. New round may reference known earlier incidents but should not treat a prior suspicion as proof.

**Final:** Last active sibling wins. Visual celebration, cartoon sting and optional Mom punchline; end-of-match reveal lists completion/helpfulness, detected sabotage, repaired incidents, action-based awards and progression. Offer Rematch, New Friends Match, Return to Family Stage and Settings. Save cosmetics and preferences. No phantom achievements/XP from test or development sessions.

**Recovery flows:** Sleep/wake headset, tracking loss, controller disconnect, audio permission denied, network loss, LiveKit outage, bot failure, pause, quit, return to lobby, rejoin. Each flow must maintain deterministic world state, restore or safely release grabbables and avoid progress duplication.

---

## 03 — UNITY SOFTWARE ARCHITECTURE AND STATE CONTRACTS

Create incremental modular assemblies around existing game code rather than replacing core `GameRoot`, task IDs, or XR systems. Use typed events and small interfaces. Proposed domain boundaries below are examples; adapt to actual repo names.

- `MatchDirector`: state machine `Boot -> FamilyStage -> CharacterSelect -> Lobby -> Calibration -> RoundSetup -> ActiveRound -> Warning -> Confrontation -> Verdict -> Elimination -> NextRound/FinalVictory -> Results`. Restrict transitions by authoritative preconditions; save event ledger and fail gracefully.
- `TaskRegistry`, `TaskRecipe`, `TaskInstance`, `TaskGenerator`, `TaskValidator`, `TaskOwnerService`, `TaskProgress`, `TaskVisualCue`: stable GUID/IDs, recipe versions, room/prop preconditions, action requirements, owner/helper attribution and client read-only indicators.
- `InteractionSource` and `InteractionIntent`: normalized hand gesture, controller ray, grab, proximity, grip pose, release, snap, poke, throw, deliberate loud action and no-input alternatives. Maintain current Meta/OpenXR input as source of truth; don't reinvent tracking.
- `PropState`, `PropOwnership`, `PropAuthority`, `PhysicalAnchor`, `GripAnchor`, `ManipulationPolicy`, `NoiseProfile`, `EvidenceProfile`, `RepairRecipe`, `EvidenceEvent`: stable physical ID, legal transforms, ownership, reachability and replication flags.
- `NoiseEmitter`, `AcousticZone`, `NoiseAggregator`, `MomPerception`, `MomBeliefLedger`, `WitnessMemoryLedger`, `IncidentResolver`, `CulpabilityAttribution`, `VerdictCalculator`: strict separation between truth and accessible evidence.
- `BotPlanner`, `BotBlackboard`, `BotPerception`, `BotMotor`, `BotSocialMemory`, `BotDialogueAdapter`: AI bot intents constrained to legal physical actions.
- `PhotonSessionAdapter`, `AuthoritativeGameClock`, `ReplicationBridge`, `PlayerSlotManager`, `ReconnectManager`, `SnapshotSync`, `NetworkEventAudit`: prevent two distinct sources of physical authority.
- `LiveKitVoiceAdapter`, `ConversationSession`, `VoiceConsentManager`, `MomSpeechIntent`, `BotSpeechIntent`, `NonVoiceDialogueRouter`: speech optional, safe backend boundaries, no cheating through agent tools.
- `ArtPrefabCatalog`, `RoomAssetCatalog`, `MaterialLibrary`, `CosmeticCatalog`, `AudioBank`, `VfxCatalog`: data-driven polished content.
- `SaveData`: local player preferences/cosmetics, versioned schema, basic progression, no secrets or raw speech transcript by default.

### 03.1 Event ledger
Define immutable structured events with at least: `eventId`, `sequenceNumber`, `matchId`, `roundId`, `timestampServer`, `actorSeatId`, `propStableId`, `taskInstanceId`, `actionType`, `beforeState`, `afterState`, `position`, `acousticZone`, `noiseStrength`, `actorIntentEvidence`, `visibleWitnessSeatIds`, `lineOfSightObservations`, `MomObservationId` if any, `causalParentEventIds`, `networkAuthority`, `validationResult`. Client-proposed events are untrusted until validated. Avoid sending large arbitrary object graphs over the network.

### 03.2 State determinism and replay
Only authoritative code may mark tasks complete, create sabotage outcomes or eliminate players. Distinguish input request from accepted action. Ensure every gameplay mutation is idempotent by event ID. Use versioned compact snapshot state for join/rejoin, plus recent event deltas. Provide reproducible test seeds and a debug replay viewer that can reconstruct objective evidence. Use fixed-timestep physics where relevant but do not claim fully deterministic PhysX across devices; replicate authoritative results or stable object states as appropriate.

### 03.3 Preserve progress
Before editing core scripts: capture test baseline, identify external API callers, add migrations or adapters, and protect existing book chore, current parent IDs, transforms and saved prefabs. Require regression tests for each change; never solve an interface mismatch by deleting tasks, simplifying Mom AI into a timer, or removing multiplayer.

---

## 04 — PROCEDURAL CHORE GENERATOR: 60+ REAL ACTIVITIES, NOT FAKE LABELS

Build a reusable vocabulary of physical operations and a large library of *real* recipe combinations. A recipe is complete only when at least two and at most five meaningful physical steps are executed with actual props/states, its owner and helper attribution works, at least one fair reversible sabotage/complication exists, and one physical repair path is testable. Recipe examples below are **explicit required content targets**; refine asset names without changing semantics. All recipes must be selectable by the generation system and validated individually.

### 04.1 Primitive interaction vocabulary
`PickUp`, `Carry`, `PutDown`, `PlaceInSocket`, `Align`, `Rotate`, `OpenHingedDoor`, `CloseDoor`, `OpenDrawer`, `SlideDrawer`, `Stack`, `Unstack`, `SortByTag`, `Pair`, `FillToMark` (abstract visible level), `Empty`, `WipeStroke` (bounded progress), `SweepStroke`, `BrushStroke`, `FoldPresetTransition`, `HangOnHook`, `PressButton`, `FlipSwitch`, `DialTurn`, `PourSafeProp`, `Scoop`, `Shake`, `ArrangeOrder`, `CountRequired`, `HoldStill`, `Guard`, `Observe`, `Inspect`, `ShowEvidence`, `Repair`, `ReturnObject`, `CleanVirtualSpill`. These must work with simplified/forgiving VR collision, physical animation and explicit finish criteria; no demanding precision near unsafe real-world surfaces. Avoid complex realtime fluid, cloth or fire simulation. Use object-local responsive visual approximation instead.

### 04.2 Recipe data model
Each recipe: `id`, `displayName`, `room`, `difficulty`, `durationEstimate`, `participants`, `steps[]` with verb/target/object/tag/order/undo, `propPrefabIds[]`, `anchorIds[]`, `startState[]`, `completionPredicate`, `noiseRisk`, `quietPath`, `sabotageVariants[]`, `repairVariants[]`, `witnessableEvidence[]`, `botCapabilities[]`, `handModeSupports`, `controllerSupports`, `availabilityRules`, `scoreWeights`, `cooldownPolicy`, `QA_scene`, `provenance`. Designer-authored seed variants and dynamic object placements produce procedural variety, but avoid unreachable/cross-wall targets. No duplication masquerading as 60 activities: each has a distinct meaningful state goal or action combination and is visibly recognizable.

### 04.3 Distribution & generator
Generate rounds based on active player count, skill tier, last-used recipes, furniture location, accessible room zone, number of grabbables, average reach and quiet/noisy action budget. Fairly balance task difficulty, difficulty of rework and proximity between participants. Do not spawn mutually exclusive objectives on one prop simultaneously or place a task over an occupied object. Have a validator prove all starting states, required props and goals are reachable, with at least one comfortable grab angle. Assign ownership before spawning. Support temporary helper role via actual interaction—not a button that claims credit. A procedural seed is logged for reproduction.

### 04.4 Mandatory task catalog
For compactness `P:` means physical steps, `S:` reversible sabotage/complication, `R:` physical recovery. The implementation must unpack each into explicit data and test steps. These 72 recipe IDs define minimum coverage; content quantity is NOT permission to make shallow, copied or nonfunctional mechanics.

#### LIVING ROOM — 14 recipes, LV01–LV14

- **LV01 Shelve bedtime books:** P: grab two scattered books, orient their spines, release each into correct shelf slots. S: rival quietly removes/swaps book. R: identify displaced book, restore proper order. Evidence: relocated spine, witness action, shelf state.
- **LV02 Stack magazines:** P: collect two magazines, align edges, place stack on coffee table tray. S: unstack or hide top magazine. R: retrieve and rebuild stack. Evidence: moved magazine, visible paper pile.
- **LV03 Rescue the TV remote:** P: locate remote, collect it, turn it button-side up, dock by TV. S: move it behind cushion. R: search and re-dock. Evidence: fingerprint-like contact event, witnessed movement.
- **LV04 Gather toy blocks:** P: find colored blocks, open toy chest, place groups by color, close lid. S: spill or mix blocks. R: sort and reseal. Evidence: chest position, scattered objects.
- **LV05 Park toy cars:** P: collect three toy cars, orient forward, line them on marked parking shelf. S: move one vehicle. R: restore sequence. Evidence: vehicle moved, noisy wheel rattle.
- **LV06 Match scattered shoes:** P: locate partner shoes, pair by model/color, stand them on rack. S: mismatch pairs or conceal one shoe. R: find correct match, re-pair. Evidence: distinctive shoe position.
- **LV07 Rebuild cushion arrangement:** P: grab two cushions, turn patterns forward, place at sofa corners. S: dislodge cushion. R: reposition. Evidence: cushion contact and witnessed displacement.
- **LV08 Fold and store sofa throw:** P: pick up throw bundle, trigger 2-stage authored fold, align on armrest. S: unfold/drag throw down. R: refold and store. Use shape-swapped mesh—not realtime expensive cloth.
- **LV09 Clear cups and coasters:** P: collect empty mugs, set each on tray, return coasters to coffee table. S: move cup or coaster. R: reclear and rearrange. Cup collision sound must be risk-scaled.
- **LV10 Arrange family board games:** P: pair box lid/base, place pieces inside, align two game boxes on shelf. S: slide pieces or remove lid. R: restore complete set. Evidence: exposed pieces.
- **LV11 Rehang crooked photo:** P: pick fallen frame, lift to wall hook, align upright until snap. S: tilt or remove it. R: rehang and align. No fragile glass shards.
- **LV12 Clean pretend popcorn:** P: use scoop/tray to collect cartoon kernels, transfer to bin, wipe table. S: knock lightweight popcorn bowl. R: repeat safe cleanup. Evidence: kernel cluster and noise event.
- **LV13 Return TV controller/gamepad:** P: locate controller, collect its detached accessory/strap, place in station. S: move controller under side table. R: recover both parts. Avoid copyrighted platform markings.
- **LV14 Quiet the squeaky dog toy:** P: find squeaky toy, gently pick it up, place inside padded toy bin, close lid. S: rival triggers a squeak or relocates toy. R: silence and store. Noise attribution must distinguish trap setter versus careless user.

#### KITCHEN & DINING — 16 recipes, KT01–KT16

- **KT01 Wash and rack plates:** P: collect dirty plate, perform forgiving bounded sponge gesture, show clean state, place plate upright in rack; repeat as assigned. S: remove/reroute clean plate. R: wash if dirtied or return to rack. No real water simulation.
- **KT02 Cup cleanup:** P: collect empty cup, rotate upright, rinse with authored sink effect, place in rack. S: hide cup on counter. R: re-rinse/dock if necessary.
- **KT03 Match lids to pots:** P: identify lid size, grasp handle, place each lid on correct pot, return pots to storage. S: swap lids. R: fix match.
- **KT04 Return cookware:** P: collect lightweight pan, carry by handle, rotate flat, hang on correct wall hook. S: switch hooks or pull down. R: rehang safely. A loud intentional drop creates evidence.
- **KT05 Clean breakfast table:** P: collect crumbs using cloth/wipe strokes, put plates on tray, return fruit bowl to center. S: sprinkle virtual crumbs or move bowl. R: wipe/replace.
- **KT06 Arrange dining chairs:** P: grip chair backs, slide chairs to accessible anchor positions, check alignment. S: pull one chair out. R: reposition. Use constrained non-overturning chair physics.
- **KT07 Sort cutlery:** P: open cutlery drawer, place blunt utensils into matching compartments, close drawer. S: mix two compartments. R: resort.
- **KT08 Organize pantry jars:** P: collect jars, compare colored labels, position on proper shelf, face labels forward. S: swap labels/positions through physical move. R: reorder.
- **KT09 Fill fruit bowl:** P: carry individual fruit props, sort damaged/normal tags, place target count in bowl. S: relocate an apple to windowsill. R: find and refill.
- **KT10 Make a safe pretend sandwich:** P: take bread prop, layer cheese/lettuce shapes, close sandwich, place on plate. S: remove one layer. R: reconstruct. Pure stylized plastic food, no knives or hot surfaces.
- **KT11 Organize recipe cards:** P: collect recipe cards, arrange numbered order, clip to board. S: swap cards. R: reorder and clip.
- **KT12 Refill cereal jars:** P: collect safe cereal token cup, pour through assisted animation to target fill mark, seal lid, shelf jar. S: open/move jar or displace fill token. R: recap/return; avoid actual granular simulation.
- **KT13 Wipe sticky counter:** P: pick cloth, perform 3 guided wipe strokes, rinse cloth at sink and store. S: reintroduce virtual sticky decal. R: repeat relevant strokes.
- **KT14 Hang kitchen towels:** P: gather two folded towels, hang on proper rails/hooks, orient stripes. S: swap/remove towel. R: rehang.
- **KT15 Arrange breakfast dishes:** P: set placemats, position safe unbreakable plates, place cups/coasters. S: displace place setting. R: rebuild.
- **KT16 Reset mixer/toaster station:** P: return inactive appliance accessories, wrap dummy cord through safe pose, slide appliances into marked spots. S: move accessory/prop. R: reassemble. No real electrical hazard interaction.

#### LAUNDRY & UTILITY — 10 recipes, LD01–LD10

- **LD01 Sort laundry colors:** P: pick three clothing bundles, identify tag/color, deposit in matching basket. S: mix basket contents. R: resort.
- **LD02 Pair socks:** P: locate pairs, match color/pattern, fold as preset bundle, place drawer. S: separate one pair. R: rematch.
- **LD03 Fold towels:** P: pick unfolded towel, perform two guided fold motions, place in stacked pile. S: unfold or scatter top towel. R: refold.
- **LD04 Load washing machine:** P: open door, add assigned garment bundles, press preset button, close door. S: remove a bundle while door open or reopen. R: restore load and closure. No unsupervised machine hazards.
- **LD05 Transfer laundry to dryer:** P: open washer, collect safe fabric bundles, place dryer, shut both doors. S: relocate one bundle. R: recover and finish transfer.
- **LD06 Hang shirts:** P: match shirt to hanger, align shoulders, place hanger on rail. S: unhook/mismatch hanger. R: rehang.
- **LD07 Stack storage baskets:** P: sort lids by shape, close two bins, align on shelf. S: swap lids or knock one bin down. R: pair and restack.
- **LD08 Refill towel cupboard:** P: collect clean towel pile, orient folded edges, place by color on cupboard shelf. S: mix color groups. R: resort.
- **LD09 Sweep dryer lint safely:** P: open safe mock lint panel, remove plush lint cluster, put in bin, shut panel. S: replace lint cluster. R: clean again. Stylized no real hazardous dust cloud.
- **LD10 Organize laundry products:** P: distinguish safe cartoon bottle labels, group into tray, move tray under shelf. S: scramble bottles. R: rearrange. Bottles are empty game props, no chemical gameplay.

#### CHILDREN'S BEDROOMS — 8 recipes, BR01–BR08

- **BR01 Make a bed:** P: pull preset blanket to anchor, place pillow at headboard, tuck final corner using simple animation. S: displace pillow. R: reposition and tuck. Never real cloth sim.
- **BR02 Tidy schoolbooks:** P: gather notebooks, order by color/subject, place on desk tray. S: swap two notebooks. R: reorder.
- **BR03 Hang backpacks:** P: gather backpack, zip/close flap, place on low wall hook. S: remove backpack. R: recover and hang.
- **BR04 Put plush toys to bed:** P: find plush pair, orient heads, place in toy hammock/bed. S: shift one plush. R: return.
- **BR05 Sort pencils and markers:** P: gather jumbo art pens, sort colors into three cups, put cups on desk. S: mix one cup. R: resort.
- **BR06 Organize model rocket toys:** P: match two large toy parts, fit them together, stand rocket on pedestal. S: detach top. R: reconnect. No small precision parts.
- **BR07 Close bedtime curtains:** P: pull two curtain handles to anchor, clip tieback, check closed position. S: reopen one. R: close. Curtains use authored animations.
- **BR08 Put laundry in hamper:** P: find clothing props, lift from chair/floor, place into hamper, close lid. S: pull item back out. R: replace.

#### BATHROOM — 6 recipes, BT01–BT06

- **BT01 Hang towels neatly:** P: collect hand towel, fold preset, place on rail. S: remove/crooked hang. R: fix.
- **BT02 Return toothbrush cups:** P: move oversized stylized brushes into individual cups, arrange cups by name-color. S: swap brushes. R: correct allocation. Avoid hygiene realism.
- **BT03 Wipe mirror spots:** P: pick safe cloth, move across three visible decals, place cloth in holder. S: add reversible smudge. R: wipe.
- **BT04 Sort bath toys:** P: pick rubber duck and bath boats, place in dry basket, close storage lid. S: spill toys. R: store.
- **BT05 Arrange toilet paper rolls:** P: collect unopened roll props, stack on shelf, fit one to holder using forgiving snap. S: remove roll. R: replace. No explicit toilet-use content.
- **BT06 Clear sink ledge:** P: collect toy cups, put soap pump and brush cup into marked places, wipe one water decal. S: displace pump. R: restore.

#### FOYER & ENTRY — 6 recipes, FY01–FY06

- **FY01 Align family shoes:** P: find two pairs, align toes, place on shoe mat. S: swap a pair. R: reorder.
- **FY02 Hang coats:** P: match coats to wall hooks, ensure clear door swing, verify hang. S: pull coat off. R: rehang.
- **FY03 Organize umbrellas:** P: collect closed umbrella props, put handles in stand, face badges outward. S: move one umbrella. R: replace.
- **FY04 Collect mail envelopes:** P: gather big paper envelopes, arrange into sorting slots, close small organizer. S: reslot. R: resort.
- **FY05 Put away shopping bags:** P: lift safe shopping bag, empty stylized groceries into basket, fold bag pose, place on hook. S: move groceries. R: repack correct basket.
- **FY06 Reset welcome mat:** P: carry small mat (constrained), align edges to doorway markers, flatten preset corners. S: shift it. R: re-center.

#### PET AREA — 6 recipes, PT01–PT06

- **PT01 Refill Biscuit's bowl:** P: bring pet food scoop, pour fake food token to mark, set bowl in dock. S: move bowl/scoop. R: refill or re-dock. No live feeding dependency.
- **PT02 Refill Mochi's bowl:** P: sort food can and scoop props, simulate fill, dock separate cat bowl. S: relocate bowl. R: recover.
- **PT03 Put away pet toys:** P: collect ball and squeak toy, sort into pet basket, shut lid. S: retrieve a toy. R: replace.
- **PT04 Wash pretend bowls:** P: pick empty bowl, animate sponge wipe, put on drying mat. S: move dirty token/relocate. R: rewash/dock.
- **PT05 Fluff pet beds:** P: move pet blanket pose, position cushion, dock plush pet bed away from walkway. S: move cushion. R: reset.
- **PT06 Match collar accessories:** P: collect safe tag prop, clip to decorative collar, hang collar by pet-name hook. S: switch collar hooks. R: correct.

#### HALLWAYS & STAIRS — 6 recipes, HW01–HW06

- **HW01 Collect loose slippers:** P: find pair along hall, align and dock outside correct bedroom. S: mismatch one. R: restore.
- **HW02 Dust stair banister:** P: pick duster, perform three safe bounded strokes from floor/landing without leaning, stow duster. S: reapply dust decal. R: repeat. Never encourage physical climbing/leaning in real room.
- **HW03 Hang hallway drawings:** P: collect artwork, orient upright, align with mounting hook. S: tilt. R: straighten.
- **HW04 Organize wall hooks:** P: collect two bags or jackets, assign named hooks, hang neatly. S: swap. R: restore.
- **HW05 Reposition floor runner:** P: grasp stylized rug handle, drag to snap guides on flat floor, flatten corner. S: shift corner. R: straighten.
- **HW06 Collect stray toys in hallway:** P: retrieve toy blocks/cars, put them in carry basket, carry to correct bin. S: scatter a safe toy. R: recover. Pathing must not create unavoidable collision traps.

The above is 72 authored recipes across eight physical areas. Implement **at least 72** if all are compatible with the actual map and prop library; if a particular recipe conflicts with a physical layout, document a functionally equivalent substitution rather than silently deleting it. The procedural system should permute object positions, route difficulty, partial completion, team assistance, noise sensitivity and select room focus without relying on procedural text only. Maintain a definitive CSV and deterministic replay tests for all recipes.

### 04.5 Task ergonomics
For VR comfort, use target zones that visibly fit the prop; object-local snap previews; magnetic placement in the final centimeters; forgiving collision proxy; grab points suited to child-sized avatar hands; stable release with no jitter; constrained two-hand interactions where appropriate; no spinning/scaling unintended by physics; no instantaneous teleport of a held object across room. Finger tracking loss freezes or gently releases grabbables according to a tested policy. If an operation is mechanically arduous, use a two-action shortcut available equally to bots and humans with a compensating, visible in-world animation. All core tasks must be playable with voice disabled.

### 04.6 Automated tests for every recipe
Per recipe instantiate start state, execute prescribed interaction primitives in test mode, assert intermediate states, complete predicate, owner/help scoring, inject valid sabotage, assert reversal, execute repair, verify stable endpoint, then test illegal/off-route sabotage blocked and reconstruction from network snapshot. `TEST_CASE_ID` must identify both state and physical reachability checks; use screenshots/recordings for representative hero mechanics. No change to recipe count is accepted without updating test coverage.

---

## 05 — SABOTAGE, PHYSICS COMEDY, RECOVERY, COVER-UPS AND FAIR BLAME

Core principle: **Every prank is a physical action with a real consequence, possible recovery and traceable but not omniscient evidence.** Keep approximately 80% opportunities in normal household objects and 20% exaggerated safe cartoon props/traps. Avoid damaging the room permanently, hiding critical objects out of reach or making one player indefinitely griefable. Implement target-specific cooldowns, interference limits, repair protection and debuggable responsibility.

Types: (1) undo completed task by removing/misplacing object; (2) introduce reversible clutter; (3) swap tags/objects; (4) create distracting noise by physically tapping or dislodging a designated safe prop; (5) place a visible prank affordance such as squeak toy or goofy spring box at designated spots; (6) exploit pet movement by leaving a toy in a legal pet path; (7) cover up by quietly fixing mess or moving evidence **without falsifying logged truth**; (8) help victim recover and earn capped actual helpfulness; (9) exploit rival interruption by timing a distracting activity; (10) intentionally create an avoidable hazard that triggers a warning. All use physics constraints appropriate for headset. No malicious-sounding injuries or real harmful instructions.

**Responsibility algorithm:** Log each causal action with actor and placement, inspect avoidability and witness provenance. If an object was rigged to create unavoidable noise when an innocent sibling follows a valid chore instruction, blame the trap setter where Mom has evidence. If a player carelessly smashes a normally safe object, responsibility can shift to that player. If Mom has insufficient evidence, uncertainty remains, and the verdict uses credible evidence and chore outcome rather than cheating. Maintain `truthCulprit` for QA and `knownCulpritCandidates` with confidence for Mom. Test ambiguous cases, multiple causal contributors, pet-induced events, failed sabotage attempts, allied help, and late repairs.

**Examples:** Pip leaves a squeaky toy inside an easy-to-grab laundry basket; Nell reaches in following her task and squeak occurs. Game truth records Pip's setup and Nell's unavoidable activation. Mom hears the squeak but does not magically know Pip planted it. Rory may have witnessed Pip. If Rory presents credible memory, Mom can shift suspicion. If no one saw Pip and the toy gives no distinguishable clue, Mom may instead favor whoever has unfinished chores; game outcome is allowed to be imperfect but must be rule-consistent. In another case, Nell throws a pan while trying to frame Pip; game truth records Nell's action, and Mom may infer from physical inspection or witness rather than a hidden cheating flag.

**Bot sabotage:** Must physically approach, reach, pick, carry, release and verify object changes, with same cooldown and visibility costs as humans. Bot 'intent to sabotage' is not itself a completed sabotage. Failure should be visible: misses, interrupted grabs, pet collision, relinquished object. Strategic bots cannot instantly modify task data in code to imitate sabotage.

**Anti-exploit:** No self-generated cleanup XP, loop spam, impossible placement, object duplication, prop translocation, intangible hands, task completer stealing without ownership logic, safe-zone griefing, deliberate countdown freezing, spectator interference, or repeated evidence forgery. Include telemetry counters and reproducible stress tests.

---

## 06 — MOM: EMBODIED HUMANOID, PERCEPTION, MEMORY, INVESTIGATION AND INTERROGATION

Mom is a full-body cartoon humanoid in a purple house robe with floral apron, hair bun, slippers, expressive face, distinct silhouette consistent with approved Mom turnaround. Unlike siblings, Mom has visible anatomical arms/legs and conventional retargetable humanoid motion. Her home is a *closed upstairs bedroom*, not an open landing. Show physical transitions: sleeping → subtle stir → warning/grumble → sit up → slippers on → stand → bedroom door open → hall → stair descent → identify disturbance → inspect → interrogate → verdict → return upstairs (or cut at appropriate authored transition). Use believable locomotion/door handling; no teleporting through walls, snap yaw, floaty sliding or body clipping. If a route is blocked, choose a safe path or pause logically.

**Mom senses:** Hearing by event strength, room topology, occlusion/proximity, accumulation and state; vision by limited field of view, clear line-of-sight and distance; inspection of world objects in reachable rooms; testimony from available witnesses; prior memories with confidence and decay. No all-seeing access to data. Mom may suspect based on context but must identify speculation as speculation. Invalid or contested evidence does not become true because the AI's text is persuasive.

**Suspicion escalation:** Sleep baseline, stirring, brief warning, investigation-ready, waking, active investigation, confrontation and verdict. Target one decisive confrontation per round. The system may issue short warnings without freezing player movement; physical and audio feedback should precede changes. Warning VO cannot delay actual rules indefinitely. Tension/music/VFX are driven from the same state. Round timers and escalation thresholds must be data driven and documented. Early huge noise may push investigation immediately; normal small activity does not.

**Mom's belief ledger:** Observation records: source type (seen/heard/inspected/witnessed report), scene/position/time, believed actor, confidence, contradicting details, corroborating details, memory age. Distinguish direct witness proof, partial visual observation, environmental trace, another sibling's report, a speaker's claims, and actual but unseen server truth. Agent prompts must not include `WorldTruth` culprits except for narrowly authorized non-judgment tool operation. A language model must not be asked to choose a final punishment from unvalidated raw chat.

**Playable confrontation:** ~40–60 seconds as playtested. Phase 1 (8–12s) Mom physically inspects and presents the incident. Phase 2 (20–30s) player uses physical evidence pointing, contextual gesture response, witness-memory physical cards, or optional live speech. Phase 3 (10–20s) Mom poses one concise question if relevant, evidence is reconciled by deterministic resolver, verdict animations occur. Avoid fixed cinematic railroading. No user must type or talk. During interrogation controls must be VR spatial, text readable, and every selected memory linked to a real witnessed ledger event; show only what that witness truly observed. The 'witness-memory UI' is a stylized card with action/time/location, not a magical live camera recording.

**Verdict calculation:** Evidence strength is gated by admissibility and confidence. Known confirmed causing-event with credible link can take precedence over small help credits; under genuine ambiguity, incomplete assigned chores, validated help and other visible behavior matter. Round timeout without decisive incident favors the documented 'least helpful' rule, with fair tie handling, not random elimination. Suspect scores, confidence categories and tie-breaker must be logged for audit, not shown fully to active players. Avoid philosophical omniscience or bias toward voice-chat users.

**Dialogue and expression:** Mom can say short family-friendly lines tied to actual observations (e.g., 'I heard a pan in the kitchen. What happened?'). Expressions include asleep, sleepy, suspicious, listening, warning, disappointed, surprised, thinking, angry, satisfied. Facial rig supports blend shapes or bone controls. Eye contact corresponds to seat/head position, not fixed camera. Provide fallback authored lines if speech backend unavailable. Player voice interaction must be optional and not determine task validity.

**Tests:** Mom downstairs from bedroom without clipping; alternate routes after obstacles; perceives loud sound through door with appropriate attenuation; fails to see hidden action; remembers true witness; ignores hallucinated witness claim; reacts to pet sound; correct culprit vs uncertain case; two opposing testimonies; all players silent; LiveKit offline; network reconnect mid-confrontation; final timeout. Provide sample event ledger and verdict trace for at least 12 scripted scenarios.

---

## 07 — SIBLING BOTS: FAIR EMBODIED STRATEGY AND OPTIONAL CONVERSATION

Pip: impulsive and lively, goggles/red hoodie. Nell: thoughtful, observant, glasses and purple. Rory: opportunistic and teasing, cap/blue. Tess: outgoing social negotiator, yellow/pink. These are *starting personality priors*, not hard-coded morality roles. Rory can sincerely help; Nell can sometimes mischievously sabotage; Pip can show restraint; Tess can betray after cooperation. Maintain unpredictability from state rather than random constant chaos.

**Bot hierarchy:** utility/GOAP or bounded behavior planner selects from chore, assist, sabotage, repair, hide, investigate incident, warn, seek ally, flee noisy location, collect prop, present witnessed memory, respond to Mom and spectate. Planner reads its own sensed world and known relationships; motor executes legal NavMesh/physical interactions. Audio/dialogue uses LiveKit when enabled; fallback templates and gesture meanings remain available. No LLM-driven direct transform manipulation or authoritative scoring mutation. Budget decisions in infrequent ticks, cache small world summaries, use hysteresis to prevent flip-flop, timebox remote requests and cancel stale goals.

**Social memory:** Record validated promises/help/betrayals seen by the bot, suspicion, reciprocity, grudges, confidence and time decay. Mutual support may improve willingness to help or defend, but cheating is permitted and alliance never alters game fairness. The bot can refuse a new alliance after witnessing a betrayal; it cannot know an unseen betrayal. Bot speech uses persona but never reveals future card secrets, private player data or authoritative truth.

**Physical presence:** Detached visible head/torso/hands/shoes still animate as one coherent character. Idle subtle bob, look target, lean, happy shrug, worried flinch, excited step, carrying pose, push/pull, place, drop, sneaky glance, nod, shake head, shush gesture, finger point, accusation, laughter, panic, celebration, crouched sneaking (comfort target only), and spectator reactions. Hands contact props with authored grip anchors; limbs should not stretch invisibly from shoulder to object without posing the floating hands credibly. Foot-to-floor alignment and shadows support grounded physicality. On path failure, retry with visible recovery and no teleport beyond legal room transitions.

**Bot quality targets:** Can finish every available primitive action; 100% of 72 task recipes have compatible bot step mapping or documented alternate physical execution; performance budget not tied to LLM latency; observe, plan and physically perform at least 10 diverse full activities in tests; player can meaningfully influence them with gestures and optional conversation. Use simulated tests with intentional mistakes and confrontation proof.

---

## 08 — CHARACTER ART, RIGGING, ANIMATION AND CUSTOMIZATION PIPELINE

Study the user-supplied four-view sibling sheet and Mom turnaround. They are **identity and silhouette references**, not literal UV maps or pre-rigged source geometry. Build editable Blender sources in named collections: original high-detail sculpt/model, game topology, hair/clothes, facial components, floating pieces, skeleton/constraints, animation actions, LODs and collision. Approved four siblings: Pip (goggles/red), Nell (glasses/purple), Rory (cap/blue), Tess (pink bow/yellow). Preserve personality and character readability even after cosmetics change.

**Sibling visible structure is fixed:** separate head, torso, two hands, two shoes, with NO visible arms or legs connecting pieces. A **hidden internal humanoid-compatible motion hierarchy** may exist to animate natural body timing and share/retarget motion. Do not assume Unity's ordinary humanoid avatar auto-rig will produce correct visuals without custom constraints; evaluate humanoid vs generic rig/retarget bridge, document tradeoff and implement actual transform/bone mapping. Head orientation toward interlocutor, independent hand posing, torso lean, shoe placement and root-motion compensation must stay coherent. No visible ghost limbs, disconnected hand collision jitter, or floating torso that ignores floor. Build full animation coverage listed in section 07, plus sleep, wake surprise, fall-safe reaction, apology, accusation, carry heavy object and spectator gesture. Include expressive face controls: blink, lip-sync viseme/shape or practical 2D mouth atlas, eyebrows and eye gaze with fallback.

**Mom:** conventional full-body rig (visible arms/legs), strong deformations at shoulder/elbow/knee, facial controls and hand grip targets. Make her robe/apron folds stylized but game-ready; avoid runtime cloth physics. She must handle walking, stairs, turns, door opening, inspection and interrogation while keeping believable hand-object contact. Do not fake her investigating by spawning a duplicate static statue. Her path starts in enclosed bedroom with valid bedroom door/hallway geometry.

**Mochi/Biscuit:** stylized cat/dog with walk, idle, sniff, sleep, alert, paw nudge, startled and playful reactions; low-cost behavior and animated props. Pet collision must not shove the user camera or create unfair unavoidable noise. Don't give pets unrestricted social agency; they are bounded independent actors.

**Customization:** modular outfits/hoodie styles, hats/caps, goggles, glasses, headbands/bows, hairstyles, shoe color/models, accessories, basic material colorways, unlockable emotes. Accessories attach to proper face/hair/body anchors and are excluded when colliding or hiding facial expressions. Provide a catalog with stable cosmetic IDs, unlock constraints, render cost, prefab references, thumbnails and actual save/load. Give all four siblings equivalent customization depth, but retain identity silhouettes and recognizable faces. Selected cosmetics replicate online with fallback to default if remote lacks asset.

**Quality gates:** orthographic front/back/left/right, three-quarter, expression board, gray/texture/final beauty, Unity URP actual prefab, turning animation, hand-contact pose, stress test with accessories and far-LOD. Characters must not T-pose in play. All animations have named root convention and import settings; document animation source/license. Verify humanoid or adapter rig type in Unity rather than merely labeling a Blender bone tree humanoid.

---

## 09 — HAND TRACKING, CONTROLLERS, LOCOMOTION AND PHYSICAL CONTACT

The project already contains the Unity Asset Store **Stylized - Simple Hands** package; reuse the existing installed models, not a guessed duplicate. Audit current white-hand prefab, mesh scale (prior prefab was oversized), local axes, left/right mirror, 16-bone skin, material and skeleton mapping. Retain Meta XR/OpenXR tracking and grab/poke/ray functionality; replace only visual skin where needed. The visual mesh may have fewer bones than tracked joints; implement stable pose retargeting and preserve pinch/index/opposition behavior. The user's supplied gripping screenshots exposed a pinch-pose issue: thumb and index sometimes approached the same side of a book. Make opposing contact across book faces and test from multiple camera angles. Avoid fists visibly penetrating covers or fingers that are huge relative to everyday props.

**Hybrid movement C:** primary tracked hands and interaction, optional controller locomotion using sticks where available, optional gesture/hand-UI movement scheme when controllers are absent, teleportation and snap-turn comfort choices. A user using hand tracking only must still traverse the entire house without requiring their physical room to be the size of the virtual building. Gesture travel must not collide with normal grabbing/pointing; select explicit movement mode, e.g. locomotion anchored hand menu or supervised point-teleport. Provide seated/standing floor calibration, height adjustment, handedness, snap-turn increments, optional vignette for smooth turning and movement speed cap, disable physical camera shake, and conservative stairs/threshold handling. Never attach permanent HUD to face or force player lean toward unreachable objects.

**Interaction tests:** both hands can pinch, grab, direct poke, ray select, open drawers, interact with doors and appliances, lift/rotate/stack props, activate contextual buttons and let go. Tracking loss makes object remain consistent and recoverable; hand switching while carrying behaves predictably; no object tunnels through walls or drops unpredictably. Each grab needs pose target, thumb opposition, collider and rigidbody constraints, contact SFX, optional subtle haptic for controllers and visually gentle feedback for hand-only. Every mode has an accessibility equivalent, including nonvoice witness response. Spectator hands cannot alter world physics.

**VR comfort and spatial scale:** 1 Unity unit ≈1 meter; believable table and shelf heights, sufficient doorway clearance, no oversized book at eye level. Validate stereoscopic depth and reach with real headset. Ray panels generally at comfortable distance and slightly curved when appropriate; direct poke zones near hand reach. Don't apply aggressive bloom, motion blur, depth-of-field or strong full-screen distortions in headset. No artificial head bob or forced cinematics with fast camera motion.

---

## 10 — PHOTON MULTIPLAYER, AUTHORITY AND PRIVATE MATCHES

**Audit first:** Determine actual installed Photon product and version from current code/packages. Do not silently replace PUN with Fusion, change backend or introduce a second network transport. If Fusion is already present, use its authority model and tick/snapshot approach; if another Photon stack is installed, adapt stable abstractions and justify any migration separately. Private rooms require room lifecycle, join code, ready state, avatar/seat selection, late/rejoin logic, reconnection grace, bot fill and clean leave. No fake networking UI.

**Authoritative ownership:** One designated authority validates match timer, assignments, prop state, noise, evidence, Mom belief, bot actions, elimination and progression rewards. For Shared mode, define who holds state authority and how handoff works when that peer disconnects. For host/server mode, document host migration limitations and provide safe exit/restart if full migration not feasible. The client streams pose intent and validates in-world reach for grabs and task events, rather than trusting arbitrary remote scoring requests. Replicate avatar root/head/hands, key prop transforms and ownership transitions with smoothing suitable for Quest. Avoid sending every finger joint at full physics rate if unnecessary. The local tracked hand remains responsive while network correction avoids visually explosive snaps.

**State updates:** Prop state ownership should be single-writer at any instant; task completion cannot race with sabotage; release/grab conflicts resolve deterministically; object transfer is acknowledged. On rejoin send snapshot + ordered authoritative events; don't duplicate XP, culprit decisions or objectives. Bot takeover includes exact seat and state handoff. LiveKit may carry conversational audio/data but MUST NOT independently mutate authoritative gameplay or be treated as a second object-replication channel.

**Test matrix:** Solo 1 human +3 bots; private 2+2, 3+1 and 4+0; room full, wrong code, duplicate selection, reconnect after grab, disconnect during Mom interrogation, packet loss/latency, two users grab same book, two users sabotage while owner repairs, spectator attempting physics, AI/bot handoff, match end and rematch. Record actual two-client or multi-device evidence. Local fake multiple avatars do not qualify. If only one Quest device available, use separate network clients for desktop simulation but label headset tests as pending.

---

## 11 — LIVEKIT OPTIONAL VOICE AI FOR MOM AND SIBLINGS

Use the installed `shhh-livekit-unity-voice` skill and, if available, actual LiveKit Agents and Unity SDK docs matching installed versions. Audit where the runtime agent backend lives and how tokens are minted. Client must not ship long-lived LiveKit API keys or LLM secrets. Tokens are short lived, scoped to match/conversation, delivered via a secure authorized backend. No claim of unlimited free calls or unlimited concurrency; honor provider quotas, cost controls, timeout and reconnect limits.

**Topology:** Unity owns animation, authoritative game events, Mom's decision and bot physical action. LiveKit voice service handles real-time speech turn-taking, optional speech-to-text/LLM/TTS and conversation context scoped by room/player. Agent tool calls request *intents* or question templates, never unconditional `SetScore`, `Eliminate`, `CompleteTask`, `RevealHiddenCulprit` or arbitrary scene writes. Voice service receives a filtered list of Mom observations or bot witnessed memories, not global truth or private client logs. Even if the language model says "I saw Rory", the ledger must verify actual observation or downgrade the claim to unsupported testimony.

**Human social voice:** Optional spatial mic for human-player communication, privacy controls, mute, volume, speaking indicator, safe fallback if denied mic, and clear distinction between speaking to other humans, speaking to Mom and speaking to NPCs. Prevent mic feedback, echo and duplicate voice playback when Photon Voice and LiveKit are both installed. Choose one consistent human voice transport and document bridge to agent if needed. Real 3D voice position should follow player's mouth/head; Mom and NPC voices should emanate from their bodies, with sensible distance roll-off and room filtering. A player should not be auto-listened-to by Mom through walls merely because a voice stream exists. Spoken noise events must use an explicit measured/abstracted gameplay policy, not accidental network amplitude secrets.

**Dialogue personality:** Pip energetic; Nell careful; Rory clever; Tess sociable; Mom tired, warm, appropriately stern. Short responsive dialogue, not constant LLM monologues; voice can ask for alliance, accuse with supported memory, apologize, plead, warn and negotiate. Mandatory gesture/card equivalents with equal mechanical strength. If LiveKit unavailable, bots follow deterministic planning, Mom uses recorded/generated locally bundled fallback lines, and every match mechanic remains playable. Agent failures must be non-blocking to round state.

**Privacy and safety:** Default voice opt-in with accessible settings, user consent appropriate to jurisdiction/audience, do not retain raw voice recordings by default, avoid storing personal transcripts, provide mute/block/report mechanisms appropriate to platform and age rating, never echo credentials or sensitive metadata to AI. Add abuse controls for private human communication; no LLM speech used as sole source of blame. Build cost/latency instrumentation without collecting unnecessary user data.

**Tests:** agent online, agent offline, silence, accented English, two people speaking, rapid interruption, packet loss, player mute, no mic, false claim, correct witnessed clue, contradictory witness, speech when Mom physically too far, bot planning while dialogue delayed, join/rejoin mid-voice, invalid tokens and graceful fallback.

---

## 12 — CONNECTED HOUSE, ROOMS, NAVIGATION AND ENVIRONMENTAL STORYTELLING

One coherent modern suburban family home, observed from comfortable first-person VR eye level. Canonical dimensions and doors/arches/windows must reconcile all approved concept images. Use the existing V5 geometry as the start, not a new unconnected graybox. Produce an annotated top-down map, object/door/room anchors and traversal routes for human, bot, Mom and pets. All rooms physically connected and properly sealed; Mom cannot see through a balcony/open bedroom wall. Current source had issues with mirror/orientation, window placement and wall crossing a bedroom route; re-check visually in play mode, not solely NavMesh completeness.

**Family Stage:** stationary theatrical presentation in the house style; warm marquee and curtains; physical character pedestals, box/curtain reveal, real buttons. **Customization:** dressing-stage/wardrobe within same visual language, item display, 3D spin/try-on and soft curved spatial selector panels. **Living:** V5 oval oak coffee table, fully sculpted sofa/armchair, shelf, books, textured wood floor, rug, curtains, photos, lamps, toys, TV, shoe rack, pet bed; preserve V5 functional book anchors. **Kitchen:** genuinely dressed L-shaped work area, sink/plate rack, cabinets/handles, cooker/hood, fridge with child art, dining table with four chairs, fruit/cookies, cookware, organizing props and nighttime moon window; no fake task labels over empty cubes. **Laundry:** washer/dryer with usable doors, wicker baskets, folded clothes, rails, hooks, towel piles, detergent bottle props, safe folding surface, night window, trim and rugs. **Bedrooms:** Mom's private enclosed upstairs room with bed/bedside, wardrobe and door; one or more children's bedrooms with distinct play/study objects for BR recipes. **Bathroom:** compact clean stylized space with reachable safe props. **Foyer/hall/stairs:** safe traversable, signed, textured and dressed paths, proper collision and no visible voids. **Pet area:** reachable separate beds, bowls, low shelf, toys and animated pet path.

**Art story:** Evidence of four kids living here: drawings, homework, game boxes, stickers, toys, photo frames and minor task-related clutter. Static decorative pieces are merged/batched/reused; gameplay props have individual stable prefabs, IDs, collision and legal transformations. Have deliberate negative space around primary grabbables and doorways. Room visual target is not maximal prop count; it's handcrafted richness with high functional readability.

**Connected navigation QA:** For every room human path, bot path, Mom path and emergency return, verify proper doorway widths, collision walls, ceiling clearances, stair landings, usable hand height, task objects, absence of blocked doors and absence of object collision with XR player capsule. Include at least one fully recorded walk from living room to kitchen, laundry, upstairs Mom bedside and back. Run automated NavMesh paths but also inspect camera footage for clipping and teleport-through-wall errors.

---

## 13 — BLENDER ART PRODUCTION: THE 'HANDCRAFTED' GOLD STANDARD

Reference hierarchy: (1) approved SHHH! gameplay concepts, character turnarounds and logo; (2) current V5 source assets and established geometry; (3) uploaded stylized fantasy examples for *handcrafted mesh/texture craft only*; never transplant Viking, fantasy cottages, magic, weapons or horror. The style name is **SHHH! Soft-Sculpted Storybook Toon (Quest Edition)**: broad rounded silhouettes, mild deliberate asymmetry, carefully modeled cushions/edges, hand-painted color gradient and AO/curvature accents, softened bevels, illustrated texture detail, clean readable objects, warm cream/honey wood, lilac/plum upholstery, coral detail, cool moonlight and amber lamps. Avoid raw cubes with plain flat colors or procedural noise masquerading as paintings.

Use existing editable Blender source for V5 furniture. Reopen and inspect sofa pillow intersections, curved arm shading, wood grain, negative normals, UV mirror text and bevel weighting. Every hero prop needs correct-scale mesh, named collision proxy, stable anchor/handle, proper origin/pivot, neutral pose, UV atlas where useful, authored albedo and optional baked normals/AO, restrained roughness, image texture files and Unity prefab. For cloth choose sculpted/baked fold shapes rather than realtime cloth. For small items, atlas/batch; don't insist every decorative prop have its own 4K textures.

**Per-asset pipeline:** reference crop/turnaround → silhouette blockout → sculpt/model → retopology/LOD → UV/lightmap UV → hand-painted/stylized base color → optional bake AO/curvature/normal → material lookdev → Blender preview → consistent export orientation/units → Unity import/material remap → collider/rig/anchor setup → actual gameplay camera review → static/dynamic batching decisions → Android performance checks. Use separate source meshes from runtime FBX/GLB. Preserve licenses for imported store assets.

**Acceptance criteria:** no stretching/intersecting sofa cloth; no inverted text on book covers; no missing faces; no one-sided impossible normals; no material mismatches; no floating table feet; no action anchor buried in furniture; no implausibly oversized doors/books/hands; no yellow light that washes out all purple; no texture-only workaround that leaves large silhouettes square. Compare matched-angle Blender vs Unity screenshots and annotate differences. Scene must look inviting without HUD.

**Proposed initial art proof:** V5 living room existing art passes gold gate when its sofa, rounded table, bookshelf, illustrated books, textiles and ambient props look cohesive in Unity under Quest-like URP. Then scale same material/geometry language across the house; do not replace complete Blender assets with cubes merely because the queue is long. `AssetManifest` should track source, runtime prefab, texture budget, tris, colliders, task relation, visual rating, Unity validation and Quest validation.

---

## 14 — URP LIGHTING, MOONLIT SKYBOX, SHADERS, POST AND VFX

**Lighting identity:** cozy family house at **night**, not sunny daytime. Moonlit navy exterior with simple silhouettes/stars, warm window glows across street, cool subdued blue environmental fill; amber practical lights from pendant, lamp shades and cabinet strips. Build a genuine night skybox/panorama that matches each exterior window's direction. Avoid repeating identical moon in every mismatched window or giant impossible external buildings. Lamps use a budget-conscious combination of emissive material, baked point lights and selective shadow; dynamic characters get light probes, contact and carefully limited dynamic shadows.

**URP:** preserve 17.0.4 renderer assets and explicit quality tiers for Quest 2 baseline and Quest 3 higher tier. Baked lightmaps, reflection probes as appropriate, SRP Batcher-compatible shaders, limited transparencies, texture atlases, shader variant stripping, optimized shadow distance, minimal real-time light count, fixed/budgeted render resolution and MSAA under actual headset testing. Avoid desktop-only screen-space AO or volumetric effects unless proven safe on target. Shader Graph and HLSL must compile on Android GLES/Vulkan rendering path used by the project; don't trust a Windows DX11 preview.

**Materials:** reusable stylized lit shader(s) for wood, plush, painted plaster, ceramic, plastic, metal, glass fake, emissive accents, softly painted toon/ramp. Use painted albedo as main art content, gentle secondary highlights, reasonable roughness and stable specular response; not a harsh full black outline on every object. Robust handling for light probes, shadow casting and static baked illumination. Ensure no pink shaders, huge variant explosion, mirror flips or breaking GPU instancing.

**Post-processing:** subtle exposure, contrast and color grading. On standalone Quest no motion blur, cinematic depth of field, heavy bloom, expensive screen-space ray effects or temporal ghosting of hands. If bloom enabled, only when truly measured as cheap and visually necessary; otherwise use mesh glow sprites/emissive fake. VFX set: grab focus, placement settle burst, quiet-footstep cue, soft dust puff, dropped object impact, noise ripple, Mom suspicion pulse, warning, witness memory highlight, successful chore spark, failed placement and comedic victory. Use Shuriken particles/mesh VFX/flipbooks primarily; don't insist on GPU VFX Graph effects incompatible/expensive on standalone mobile. Use low overdraw and consistent diegetic origin.

**Performance:** Baseline target minimum 72 sustained frames/sec on Quest 2, with better tiers where physically supported. 72 Hz corresponds to ~13.89ms per frame total system/render/game budget; don't claim entire 13.89ms is available to application CPU or GPU. Measure actual headroom separately, including stereo rendering, CPU/main thread, GPU, memory, draw calls, thermal sustained play and battery. Test worst-case four avatars, interactive props, pets, speech/voice, particle noise and two rooms visible through arches. Scene beautiful in Blender but unreadable or low-FPS in headset is not finished.

---

## 15 — GAME UI/UX, CURVED VR PANELS, BRANDING AND GRAPHIC ART

Use the provided SHHH! **approved logo**, primary full-color vector/faithful rendering as needed, brand guidelines and current tokens: midnight ink `#211530`, deep shadow `#261239`, royal purple `#70409F`, hush yellow `#FFCF2E`, moon cream `#FFF0BA`, coral `#F57F78`, party pink `#F45F73`, soft white `#FFF8E8`. Logo uses custom illustrated lettering inspired by **Luckiest Guy**; subtitle/UI display use **Fredoka** where licensing/package permits; secondary small body uses **Nunito Sans** or approved readable alternative. Do not substitute generic system UI or distort/shrink the complete logo below legibility. Use existing font licenses; do not invent new brand colors and monetization panels.

**UI language:** game-specific, softly extruded cartoon framing, interactive tangible objects and restrained curved world-space panels. Family Stage uses physical podiums and thick dimensional icon buttons. Customize uses waist-to-chest-height wardrobe and curved inspector with item thumbnails, selectable swatches and real changes to avatar mesh/material. Chore UI is a personal clipboard/object-local hint, not an enormous permanent curved `YOUR JOB` panel consuming the view. Interrogation evidence is a manipulable memory-card set with clear provenance. Spectator surface is small and harmless. Results are a celebratory physical scoreboard. Avoid a generic website/dashboard or floating 2D HUD.

**World-space placement:** prefer Meta Interaction SDK Curved Canvas prefab when compatible with current Meta XR version, otherwise implement a documented compatible mesh+uGUI adapter. Test both hand ray/pinch and close direct poke. Make target area large, distances comfortable, minimum text size verified through actual headset, contrast readable across warm/cool background, visual hover/pressed/disabled/focus states and clear click sound. Ensure all buttons have implementations including Back, Rematch, Solo, Friends, Customize, Settings, Save, Equip, Spectate, Report/Mute and Exit. No fake clickable surfaces. Use gaze only as optional assist, not sole input.

**Complete screens/states:** startup/comfort, main stage, solo options, create/join private room, join error, wait/ready, character selection, customization, input calibration, tutorial steps, in-round task card/noise, Mom warnings, physical evidence and memory cards, verdict, spectator, elimination, results, achievements/unlocks, audio/accessibility/settings and connection-recovery. Define transitions and screenshot tests for success/loading/error/disabled as well as ideal cases. Support small UI motion, physically anchored animation, proportional text hierarchy and minimal blocking during core physics.

---

## 16 — AUDIO, SPATIAL SOUND, VOICE OVER, MUSIC AND FEEDBACK

Define a SHHH! Audio Bible and organized `AudioBank` with consistent naming/loudness/routing. **Mood:** playful stealth comedy, not horror; calm pizzicato/marimba/muted piano-like motifs, quietly comic sneaking, rising tension during suspicious incidents, subtle warning stingers, brief confrontation rhythm, comedic elimination and joyful result. Prefer original or properly licensed musical cues. Do not reuse recognizable commercial game soundtracks. A dynamic mixer responds to Mom escalation, player distance from bedrooms, object noise and phase transitions without hard abrupt cuts.

**Spatial SFX:** distinct footsteps for wood/tile, couch fabric, cardboard books, cabinet hinge, ceramic clink, pan clang, soft toy squeak, pet paw/whine/purr, laundry soft thump, drawer slide, curtain swoosh, Mom footsteps/door, UI hover/select, evidence accepted/rejected, task completion/undo/repair, joke impact and muted room reverb. Use appropriate attenuation and occlusion; acoustic noise propagation informs Mom hearing via server rules rather than raw decorative loudness. Volume and dynamic range are tunable for Quest headset speakers and headphones; ensure no sudden painful peaks. Sound events are data-driven by physical collisions with velocity/threshold, no repeated machine-gun impact spam.

**Voice:** Mom and bot voice styles distinct, family-friendly and short. Author fallback VO line catalog and subtitles/accessibility options. Voice chat uses a controlled spatial audio route, allows player mute/volume, does not reveal private witness info to nonparticipants. A speech TTS API outage must not silence required game information—show gesture/text cues. Keep captured audio privacy by design. Provide list of audio source/license/creator and a profiler capture with worst-case concurrent sounds.

---

## 17 — PERSISTENT PROGRESSION, COSMETICS AND ACCESSIBILITY

Create a modest **local-first**, versioned progress system that records matches completed, validated accomplishments, XP, achievements, cosmetic unlocks and player preferences. No store currencies or in-app purchases are authorized by this specification; concept artwork may show stars/gems purely decoratively. Keep progression rewards after authoritative match results; don't let clients grant arbitrary XP or create farming loops through sabotage/repair. Cosmetic items may be unlocked through play and visible in the Family Stage/Customize area, with preview and save. Persist cross-session locally; cloud/profile sync only if current project already supports it and after checking platform/account permissions. Provide save migration and corrupt-file recovery.

Accessibility includes seated/standing height, reach assistance, left-handed options, snap/smooth turn controls, movement comfort, subtitles/captions, audio channel levels, color-independent task markers, high-contrast UI alternative, input tutorials, optional voice disabled, reduced VFX, text scaling where possible, and panic-free pause. No game mechanic requires a loud voice, reaching far beyond safe bounds, or fine precision motor skill. Consider a guardian-friendly content/privacy setup because game characters are children and private voice rooms may include children.

---

## 18 — INSTALLED CLAUDE SKILLS: ROUTE EACH TO THE RELEVANT TASK

The supplied installed-skills snapshot names the following capabilities. **Before any slash invocation**, call Claude Code's skill discovery/help or inspect `.claude/skills` to learn each registered trigger and exact syntax. Skills might appear as `/name`, a slash subcommand or an automatic description trigger; this document names identifiers, not verified executable slash commands. If a skill needs Blender MCP, Unity editor MCP, Scenario MCP, LiveKit credentials or another service, verify availability first. Read the SKILL.md before using and honor its contracts. Skills do not replace a rendering engine or working execution backend. Never fake success because a skill name was mentioned.

**Game design + physical rules:** `game-designer` for chore taxonomy, GDD, fairness, progression and player journey. **Blender handcrafted art:** `stylized-style`, `cartoon-style`, `cozy-wholesome-mood`, `blender-image-to-3d`, `blender-game-assets`, `blender-modeler`, `environment-artist`, `prop-artist`, `set-dressing`, `materials`, `texture-workflow`, `uv-workflow`, `retopology`, `asset-optimization`, `lod-pipeline`, `collision-proxy`, `unity-export`, `qa-review`. **Character body and actions:** `character-artist`, `rigging`, `animation`, `scenario-unity-animation` and `scenario-consistency` when a compatible connected generation service exists. **Spatial UI/brand:** `game-ui-ux`, `game-ui-design`, `scenario-unity-ui`, `scenario-brand-kit`, `ui-ux-pro-max`, `impeccable`, `scenario-game-assets`, `scenario-consistency`; treat general web design suggestions as secondary to native Quest ergonomics. **Unity rendering and world:** `scenario-unity-world-building`, `scenario-unity-rendering-lighting`, `scenario-unity-shaders`, `scenario-unity-vfx`, `scenario-unity-performance`, `shhh-cozy-urp-lookdev`, `shhh-quest-vfx-shaders`. **Multiplayer:** `shhh-photon-network-audit` plus verified version-specific Photon docs. **AI voice:** `shhh-livekit-unity-voice` plus verified LiveKit docs. **Scenario integration:** `scenario` only for actual connected operations, `scenario-game-assets` for game art where available. **Video:** `remotion-best-practices` and `remotion-create` are installed but not relevant to main Unity implementation; only use if explicitly delivering an original trailer/tutorial. Inspect any official Unity/Meta/LiveKit plugins *actually installed*, but do not claim the pasted list proves those plugin connections.

Build `Docs/SKILL_ROUTING.md` with: task, available skills, exact invocation verified, connection availability, output files, validation method, fallback. Apply skills sequentially per objective, e.g., art director → Blender creation → UV/texture → export → Unity prefabs → Quest performance → QA. Never overwhelm a single context with 40 simultaneous agents/prompts. The project instructions should state that skills and authoritative owner constraints travel across subsequent Claude context windows, and maintain current state files as restartable checkpoints.

---

## 19 — AUTOMATED TESTING, VISUAL ACCEPTANCE AND ACTUAL QUEST VALIDATION

Set up reproducible automated tests at four levels: pure rules, Unity edit mode, Unity play mode with simulated input, and **real headset device sessions**. For each feature record the exact test artifact, time, build hash, Unity package versions, device model and outcome. Missing device or account access is an explicit `UNVERIFIED` entry, not pass.

**Critical acceptance matrix:**
1. Build/compile clean with no severe console errors and no broken packages; production scene and Boot refer to correct files.
2. Family Stage is fully interactable with hand-ray/pinch; Solo/Friends/Customize/Settings all navigate real states.
3. All four selected siblings spawn with detached visible anatomy; offline bots actually move, physically grasp and execute actions; correct Mom rig and both pets.
4. Both hands track with correct pose and fingertip opposition; controller fallback, locomotion and calibration work; object ownership and grip are stable under loss/recovery.
5. At least 72 unique task recipes exist in data; every recipe can start, advance 2–5 real steps, complete, be sabotaged, and be repaired; no unreachable anchors, auto-completion or task farming.
6. Sabotage responsibility, witnessed memory, evidence provenance, pet interruption and helper credit work in both solo and multiplayer; test alternate witnesses and false claims.
7. Mom sleeps behind a door, moves downstairs through real navigation, inspects appropriate scenes, interrogates once per round and makes consistent verdicts without omniscience.
8. Match ends 4→3→2→1; timeout yields least-helpful rule; spectators cannot cheat; next round doesn't duplicate props or memory.
9. LiveKit voice is optional, permission-aware, stable and has fallback; nonvoice paths remain complete. No secret tokens leaked.
10. Photon private room tested with actual remote client(s), not just multi-avatar local simulation; owner transfer, latency, join/rejoin, bot fill and match termination verified.
11. Every room resembles the approved handmade concept art in actual **Unity URP** close-ups and first-person VR eye-level views; compare consistent camera angles, not cherry-picked Blender beauty shots.
12. Real Quest 2 baseline sustains target frame rate and acceptable memory/thermals; Quest 3/3S compatibility verified with actual tested hardware when available.
13. Audio dynamic states, spatial mixing, sound-based Mom reactions and all required fallback lines confirmed; no audio spam/unsafe peaks.
14. Cosmetics/emotes/save/reload/accessibility/settings functional, with correct multiplayer replication.
15. Exit, pause, restart, rematch, unexpected disconnect, tracking loss and voice outage recover without breaking state.

**Evidence:** Record video for at least one full 4→1 solo match, one genuine two-client match, one Mom interrogation without speech, one with optional voice, several physical task/sabotage/repair chains, character rigs from all sides, all room walkthroughs, lost tracking and reconnect. Show headset overlays/profiler logs for performance; provide average/p95 frame times over a representative active match, worst-case multiuser/AI, memory and draw-call data. Desktop screen captures and replay automation are useful but cannot stand in for real headset validation.

Visual QA uses side-by-side matched-angle capture of approved image vs latest Unity render with short diagnosis: silhouette, texture richness, material/lighting, UI framing, object reachability, visible defects, performance. A pass requires credible **human visual judgement** and functional test; arbitrary numerical image similarity alone is not enough. Reject missing faces, seam gaps, upside-down exports, floating objects, wrong lighting, undetailed placeholder props, impossible Mom routes and finger penetration.

---

## 20 — IMPLEMENTATION ORDER, DELIVERABLES AND FINAL HANDOFF

Proceed autonomously through **internal verification gates**. Do not ask the owner to approve routine implementation choices or to open the Unity Editor mid-cycle. However, do not hide real blockers such as missing credentials, licenses, access to device/cloud or inaccessible references. Keep work reviewable and reproducible after context compaction.

**Gate 0: Audit and safeguard.** Baseline and backups, package/version scan, runnable scenes, stable game IDs, existing Codex V5 status, skill connections, issue list and immediate regression suite.

**Gate 1: Repair core VR vertical slice.** Finish book grip thumb/index opposition, tactile grab/release, physically correct shelf interactions, sabotage/repair, noise, Mom warning, readable short practice UI; verify on existing Unity rig; preserve 13 reported checks and expand them.

**Gate 2: Art gold slice and connected world.** Finish living room quality to approved target; complete kitchen, laundry, Mom's room, bedrooms, bathroom, foyer, pet area and connected navigation; source Blender models, UV/painted textures, imported prefabs, static/dynamic colliders and baked nighttime light.

**Gate 3: All characters and physical animation.** Pip/Nell/Rory/Tess, Mom and pets, animation library, logical skeletal rigs, separate visual pieces, grip/IK targets and modular wardrobe; real expressive reactions.

**Gate 4: Real procedural game.** Implement/test >=72 task recipes, 2–5 steps each, procedural round focus, owner/help credit, fair reversible sabotage, evidence and all task actions for humans and bots. No placeholder activity entries.

**Gate 5: Intelligence and Mom drama.** Bot strategies, partial observation, social memory, Mom belief/investigation, physical evidence, witness cards and playable interrogation. All voice-off paths pass.

**Gate 6: True online private multiplayer.** Audit current Photon stack, integrate 2–4 humans plus fill bots, authoritative props/tasks/verdicts and meaningful reconnection, chat and spectator boundaries.

**Gate 7: LiveKit optional voice.** Secure voice backend, bounded tools, graceful fallback, 3D speech, privacy/mute, no second source of gameplay truth.

**Gate 8: Spatial presentation.** Family Stage, customization, polished VR curved UI, branded states, SFX, dynamic music, skybox, URP lookdev, lightweight VFX, cosmetics progression, accessibility.

**Gate 9: Ship verification.** Full playthrough and regression suite, genuine headset profiling, no secrets, licensed assets, Android IL2CPP ARM64 build, APK installation/test logs, quality gate records, source handoff and known-limitations report.

**Output artifact structure (adapt to repo, preserve existing):** `UnityProject/` with working source, scenes, prefabs and packages; `ArtSource/Blender/`, `ArtSource/Textures/`, `AudioSource/` (licensed originals where present); `Docs/README_BUILD.md`, `Docs/ARCHITECTURE.md`, `Docs/ART_BIBLE.md`, `Docs/CHARACTER_RIG_SPECS.md`, `Docs/TASK_RECIPE_MATRIX.csv`, `Docs/ASSET_MANIFEST.csv`, `Docs/NETWORK_AUTHORITY.md`, `Docs/LIVEKIT_SECURITY.md`, `Docs/QUEST_PERFORMANCE.md`, `Docs/SKILL_ROUTING.md`, `Docs/TEST_EVIDENCE_INDEX.md`, `Docs/KNOWN_ISSUES.md`, and builds under `Builds/Quest/SHHH_<version>_<buildhash>.apk` when actually produced. Keep secrets out of checked-in assets and APK. Preserve originals and license evidence.

**Final report:** State exact files/scenes changed; installed versions; number of actually functioning recipes; percent complete; which SDK tests and network tests truly passed; performance/device evidence; links/paths to Blender sources, QA captures and APK; unresolved gaps and reproduction steps. A project that compiles but has no real multiplayer, 72 task loops, correct character art, finished rooms, or Quest testing is **NOT** complete. If any critical task is impossible in available environment, deliver real partial work and clearly list blockers. Do not ask user to 'wait' for asynchronous work.

## FINAL EXECUTION COMMAND FOR CLAUDE

**Begin now by reading actual project files and all supplied references, auditing the current state, and restoring a reliable runnable baseline. Then implement the entire locked SHHH! experience end-to-end, using the skill-routing matrix, the 72 procedural chore recipes and the verification gates above. Do not stop at a plan, a render, or a compilation. Maintain source assets and proof. Preserve every approved visual and gameplay choice. Never report a feature as Quest-tested or online-tested without the actual evidence.**
