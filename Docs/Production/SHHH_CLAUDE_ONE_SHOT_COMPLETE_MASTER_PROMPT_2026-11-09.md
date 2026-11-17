# SHHH! — DON'T WAKE MOM!
## CLAUDE CODE: FINAL ONE-SHOT AUTONOMOUS FULL-GAME PRODUCTION DIRECTIVE
### Authoritative revision: 09 November 2026 | Unity 6 | URP 17 | Quest 2/3/3S | Cartoon physics comedy | 5/10-minute continuous match

**THIS DOCUMENT REPLACES AND OVERRIDES THE EARLIER SHHH! MASTER PROMPT WHERE CONFLICTING.** This is a request to edit/build/test the **existing** project, not to suggest ideas. Execute to the highest extent the actual tools and workspace access permit. Produce working sources and playable builds, not a planning-only response. Do not promise invisible background work; execute available work during the active session and provide evidence. Do not fabricate successful tests, headset footage, Mixamo logins, cloud voice services or completion.

**Owner instructions—six latest steps override all old assumptions:**
1. **COMPLETE UI/UX ART REPLACEMENT:** Current UI and interaction flow are unacceptable. Recreate logo treatment, icons, HUD, map, menu backgrounds, physical VR UI, microanimations and readable UX using available **Krita, Inkscape, Blender and Unity**; keep approved SHHH! branding. Replace flat nighttime window graphics with real exterior environment/skybox and intentional night lighting.
2. **AUDIT/IMPORT/USE REAL 3D & VFX ASSETS:** Inspect Unity packages plus eight attached asset/reference archives. Use suitable real meshes, redesign/retexture into SHHH! handcrafted toon style, add meaningful physics/animation/VFX and remove primitive placeholder usage. Import selective and license-safe; do not import everything merely to claim use.
3. **GENIES + MIXAMO ANIMATION:** Validate installed Genies Avatar SDK 3.8.6 in isolated scene, customize and animate character prototypes, use matching source character assets and the Mixamo MCP/FBX pipeline where connected. Preserve *approved* Pip/Nell/Rory/Tess/Mom identities; do not swap them for generic avatars just because SDK generation is easy.
4. **MULTIPLAYER RIGGING:** Study legacy Classroom-VR as conceptual code reference for local/remote VR head/hand targets, Humanoid animation and Photon ownership; reimplement using current Meta XR/Photon, not old Unity 2020 PUN packages.
5. **ONE CONTINUOUS 5- OR 10-MINUTE MATCH, ENTIRE HOUSE, REAL PHYSICS:** NO 4→3→2→1 elimination rounds. Every connected room accessible. Each player has personal beautiful VR house map/location/task indicators. Replace trivial grab-to-slot mechanics with convincing responsive scrubbing, water, spray, floor dirt, cooking, laundry, detailed manipulation, cartoon dynamics, procedural sabotage and fair repair. Real state, not merely effect animations.
6. **REBUILD ALL SOUND:** Retire old in-game audio content, develop a brand-new professional adaptive mysterious/playful/comedy soundtrack, spatial ambient layers, prop-specific collision/action SFX, Mom reactions/dialogue and voice fallback. Preserve sound-event logic where useful; do not indiscriminately delete functional networking/physics code.

**Explicit supersession:** Previous statements that three elimination rounds, a spectator after each round, one interrogation *per round*, resets between rounds, and a 4→3→2→1 winner are **RETIRED**. Convert old round controllers, task generator and Mom logic to one uninterrupted authoritative match clock. Keep the good parts of the original design: competition, tactical alliances/betrayal, sabotage/repair, blame, witnesses, optional speaking, four-character seats and funny Mom suspicion. A 'round' field may remain in legacy data only as migration alias; the active UX has one match.

**Known state at handoff; verify in repository, do not trust narrative as source code:** Latest reported release-signed APK `Builds/Quest2_3_3S/SHHH_alpha_0.6.2_c11.apk` (~122 MB, reported SHA-256 prefix `b78d35ec` suffix `569493f0`). It was **not** run on a headset. Desktop simulated-hands tests: all **61 reachable** recipes individually complete with sabotage and owner repair; long consolidated run **57/61 completed and 56/61 passed all checks**. `LD04`, `LD08`, `BR08` intermittently fail in long runs, risk `R21`; `HW03` solo passed after harness repair; `KT01` may drop a plate/crash late in combined runs. 11 more recipes need missing room-sized furniture/areas. Pair carry/detach fixes, join tolerance 4.5 cm, false noise suppression and laundry visual fixes reportedly landed. Family Stage positions revised; 18 audio clips and network sound dispatch were added but their actual *content* is rejected under Step 06. Mom perception/confrontation, bots, progression/outfits and autopilot simulated loop reportedly exist. Voice blocked by missing LiveKit backend/credentials; Mom speech text only; genuine post-fix two-player Photon test is pending. Treat all as audit targets, not verified in this environment.

**NONDESTRUCTIVE WORK:** Inspect `git status`; create a safe backup/branch/checkpoint and preserve stable GUIDs, scenes, prefab relationships, data IDs, currently passing tests and secret exclusion. One agent may be editing gameplay while another edited art: do not corrupt live work. Identify conflicts via git/diff, coordinate ownership with documented folders and isolated review scenes. Never silently erase a scene, animation rig, package or character source. Do not expose Adobe, Photon, LiveKit, Genies or store credentials.

**DEFINITION OF DONE:** A polished, physically interactive game: a real cartoon-designed Family Stage and usable spatial UX; solo and private multiplayer; recognizable rigged characters; finished connected house; coherent nighttime view; high-fidelity physical chores; competitive social stealth, Mom confrontation, pets/bots; new spatial music/SFX; reliable saved progression; successful builds and test evidence. **Quest validation requires a real headset.** Desktop tests, editor screenshots and signed APK existence are separate evidence categories.

---


---

## 00 — STARTING AUDIT, CAPABILITIES AND SOURCE-OF-TRUTH RULES

1. Read the **entire** directive and `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `.asmdef` boundaries, URP assets, XR settings, Android Player settings, XR rig, Photon configuration, Genies SDK, DOTween, audio and VFX. Exact versions in user's screenshot: Unity `6000.0.62f1`, Genies `3.8.6`, Meta XR Platform SDK `201.0.0` and Interaction SDK `207.0.1` were visible under My Assets; verify what is actually imported and installed. Do not mistake 'My Assets' or a download arrow for an installed Unity package. Photon Voice 2 and Fusion listings also appeared; **do not assume their product/version is integrated.**
2. Read current `Docs/PRODUCTION_STATE.md`, `Docs/RISK_REGISTER.md`, QA logs, current task catalog, scripts, scene GUIDs, artifacts and test harness. Confirm whether the `0.6.2_c11` APK and source commit actually match; compute actual SHA-256 if file present. Run a baseline compile and selected existing tests *before* modifying anything.
3. Inventory all 72 canonical recipe IDs and current 61 reachable/11 blocked; map each to room, physical primitive, prop source, sabotager/repairer, noise/evidence and runtime tests. Resolve long-run risks R19–R21 with reproduction logs; do not erase entries.
4. Inspect `.agents/skills` (user reported **117 installed skills from 49 archives**), `.claude/skills`, local Claude plugin registry, and MCP configurations. **Codex's `.codex/config.toml` does not automatically register tools in Claude.** Discover skills by reading their actual `SKILL.md` and verify callable Claude slash commands before invocation. The V1xel Mixamo MCP was installed in **Codex** project config; connect to Claude separately if permitted and needed. Do not say it is connected because another agent installed it.
5. Validate that Krita (`krita.exe`), Inkscape (`inkscape.exe`), Blender, Unity batch/editor and Android tooling are actually on the Windows machine. Work with native project formats and automated CLI/editor pipelines when supported. Use GUI/computer tools only if available; never fabricate a screenshot or save result.
6. Inspect all provided design sheets, real Unity screenshots, video/audio and review packs: approved SHHH! logo, `Mom Character Turnaround Sheet(4).png`, siblings turnaround sheet, Family Stage, living/kitchen/laundry references, current poor UI screenshots, cartoon VR UI examples and previous Codex v001/v002 work. References from other games control **quality/feel**, never asset copying or their exact graphic designs.
7. Prepare reproducible docs: `Docs/SHHH_Production_State.md`, `Docs/Current_Architecture.md`, `Docs/Canonical_Recipe_Audit.csv`, `Docs/Asset_Source_Licenses.csv`, `Docs/Scene_Room_Map.md`, `Docs/UI_Art_Bible.md`, `Docs/Audio_Bible.md`, `Docs/Animation_Rig_Contract.md`, `Docs/Network_Authority.md`, `Docs/QA_Evidence_Index.md`, `Docs/Blockers.md`. Documentation must follow real actions.
8. Add branch-safe asset import staging and import test scenes, not direct large-scale blind extraction into `Assets`. Avoid Unity `.meta` recreation by using Unity-aware import and preserving GUIDs. A tar-like `.unitypackage` must be imported through appropriate Unity workflow.

### Source precedence
1. This **09 November 2026** prompt and latest six owner steps; particularly one uninterrupted 5/10-minute match and total UI/audio redesign.
2. Approved original SHHH! characters/logo/colors and contemporary nighttime family home style.
3. Existing verified in-repo interfaces, IDs, saved data and stable scenes.
4. Uploaded archives and their actual licenses; avoid unlicensed assets and unsupported versions.
5. Existing older master prompt only where still compatible; never resurrect 4→3→2→1 or old primitive appearance.

### Autonomy contract
Proceed through implementation gates without repeated requests for permission for ordinary code/art iteration. Do not stop at audit or mockups. If a truly external human step is necessary (Adobe login, credentials, app ownership, device pairing), document the *minimal action* and continue with independent paths; install no untrusted packages and do not misrepresent offline fallback as live cloud connectivity. Ensure every claim of `PASS` cites actual logs/screenshots/recordings and environment.

---

## 01 — CANONICAL PRODUCT: SINGLE CONTINUOUS MATCH, NOT THREE ROUNDS

**Genre:** premium contemporary-nighttime, family-safe VR physical comedy/social stealth. Four sibling seats are Pip, Nell, Rory and Tess. Mom sleeps in her enclosed bedroom. The four compete to finish chores quietly, disrupt opponents, rescue things, and convince Mom with credible physical evidence. The entire house is one connected runtime scene/world.

**Match duration:** The Solo and private-room host chooses **5:00 or 10:00 minutes** before starting. A shared authoritative clock begins only after all participants spawn and calibration is completed. The timer runs continuously in real gameplay, including investigation and short confrontations, unless a narrowly defined optional 'Mom pause' is explicitly documented and tested; the default is **no time freeze**. The same remaining clock appears in everyone's HUD. No next-round screen, intermediate elimination, or between-round task reset. End at zero or explicitly documented immediate game-over condition; a single victory/results screen then permits replay.

**All four siblings remain active for the full match by default.** Mom can warn, temporarily detain/freeze a specific character with a short fair time penalty, require repair of a disturbance, confiscate a noisy prop, or assess a documented score penalty after a playable interrogation. Do not reintroduce player-count elimination. 'Sent to bed' may be a **brief, funny timeout** with clear recovery and nonharmful animation, never a permanent three-round elimination. For severe/repeated disturbances, allow a decisive game-over only if the rule is communicated and consistent; preferable default is penalties and escalating Mom pressure until match expiry. Spectator mode is for voluntarily exited players or post-match viewing, not routine between-round elimination.

**Mom confrontations:** Retain hybrid physical evidence and witnessed-memory cards, escalation and optional meaningful speech. Configure **one full major interrogation in a typical match** with optional short warnings and quick challenges; do not trigger one per legacy round. If a second genuine critical event occurs, use a short investigation or fast question sequence that does not repeatedly immobilize all players. Mom's hearing/perception are spatial, bounded, not omniscient. Official verdict comes from deterministic evidence rules, not the LLM. At 0:00 Mom may conduct a concise end-of-match inspection/closing verdict as final results presentation, without extending play indefinitely.

**Fair competitive scoring:** Award score for physically completed owned tasks, verified repairs, limited assist credit, legitimate rescues and clean execution. Penalize actual damaging disruption detected by game rules, attributable loud incidents when evidence supports, failures/unfinished owned chores where fair, and Mom penalties. Do **not** reward infinite sabotage-repair farming, self-made spills, alt accounts or fake gesture spam. Owner/assist/sabotage provenance recorded in authoritative event ledger. Show each player their own score and progress; rivals get only fair permitted score visibility. Define tunable values in data, publish a simple in-game rules/help card, record tie-break order (e.g., task contribution > fewer attributable noise penalties > earlier completion > deterministic seeded tie). An informative match winner must be determined even with offline Mom voice.

**Solo & multiplayer:** Solo one human plus three physical AI siblings. Friends 2–4 real human players in private Photon room, bot-fill vacant seats. No fake online buttons. Distinct avatar customization and exact location/owned tasks for each player. Disconnected human seat retains its identity for a grace window then moves to bot control, rejoin restores seat safely. No duplicated authority or double agent.

**Full spatial house:** Always accessible living, kitchen/dining, laundry, Mom bedroom (with appropriate stealth risk), children's rooms, bathroom, foyer/entry, staircase, hallways, pet area; preserve coherent house plan and actual navigation. Define safe world rules for Mom's private sleeping area and any hiding spaces. No disconnected teleport-only mini-game rooms. Different task spawn combinations create variety without locking entire rooms.

**Chore life cycle:** `Available -> Claimed/Assigned -> ActivePhysicalSteps -> Completed -> Disrupted -> Repaired/Completed`, with `Cancelled/ResetSafely/Blocked` for failures. Each meaningful action must be physically validated. Assist and sabotage operate on this same state. 72 approved recipe IDs are expected, but the **actual in-repo names/IDs** control migration. All must be playable in the connected home after missing prop/rooms are built.

**Alliances & betrayal:** informal temporary help and strategic sabotage, with hand gestures or optional speech. No forced teams, no social coercion, no voice-only advantages. Pet events have their own causal provenance. User controlled interaction should be fast, readable, fun and recoverable. Bot planners must play by same physical predicates.

**Game pacing template (tune through playtests):** first 15–25 seconds show house map/task card and invite players to explore; early easy chores establish mastery; mid-match AI and sabotage diversify actions; Mom becomes more alert as the situation warrants; final 60 seconds use adaptive music, a more visible but nonblocking countdown and clear finish cues. Do not script arbitrary instant chaos every match. Validate both 5- and 10-minute pacing, task feasibility and room traversal for solo and multiplayer.

---

## 02 — END-TO-END PLAYER JOURNEY AND ALL SCREEN/INPUT STATES

**First launch:** Show approved SHHH! logo and brief meaningful setup. Detect tracking/controllers, comfort settings, readable text, standing/seated calibration, left-handedness and input choice. Avoid obstructive splash text. Present a carefully designed physical Family Stage in the cozy house style; four sibling pedestals/characters, substantial 3D stage artwork, theatrical curtains and brand motifs. Existing family stage must be refined, not replaced by a blank panel.

**Main menu:** Solo, Play with Friends, Customize, How to Play, Settings, Credits/Quit as appropriate. Every button must have true pointer/pinch/poke events, elegant hover/pressed/selected animations, spatial feedback, error states, keyboard/controller fallback if supported, and real destinations. Character preview visibly reacts to selection. Distinct framing and negative space.

**Customize:** The Genies SDK can prototype avatar editor infrastructure, but visible approved characters Pip/Nell/Rory/Tess/Mom remain the artistic target. Make an actual in-world costume/wardrobe stage with rotatable avatar, swatches, hair accessories, glasses, hats, shoes, clothing, emotes, preview/reset/save. Persist per-player local customization and network only allowed appearance descriptors; don't break the disconnected anatomy. Show actual custom parts, not fake purchase buttons.

**Solo:** Select character, choose 5/10 minutes, optional difficulty, calibrate, load seamlessly, view personalized task card/map, play all connected rooms with three independent bots, get Mom warnings/interrogation in real time, finish to one scoreboard/results and save progression.

**Friends:** Create/join private room, code/presence, match version compatibility, player slots, character selections, ready status, timer mode 5/10 host choice, microphone/consent controls, load with synced match clock. Handle valid/invalid code, no network, missing permissions, full room, disconnect/rejoin, host migration limitations and rematch. No cosmetic UI without actual backend calls.

**Practice:** teach precise pinch/grab book contact; a two-handed step; scrub a real surface; turn faucet and pour water; mop an actual stain; demonstrate subtle task marker, personal map and noise indicator; do one fair reversible sabotage/repair; show Mom's warning. A short interactive sequence with one clear instruction at a time, no giant unreadable opaque `PRACTICE` floating wall. Mom grumbles but practice cannot unfairly end the game.

**Active match:** one continuous 5/10-minute countdown. House map opens from wrist/manual/physical map prop, never a constantly opaque panel. Show current location, facing direction, floors and nearby stair connection, room icons, tasks, owner and type, task progress, optional rival/public signals and Mom alert indicator. The player can select a task, trace path through rooms or follow concise contextual affordances. Each player sees **their own tasks** first; team/rival data according to fairness. Movement and tasks remain accessible during minor Mom investigation.

**Physics interaction:** player sees tool-appropriate hand grip and physical response, surface dirt removal, water/foam levels, cloth folding, sharp visual contact cues and small musical/UI confirmation on valid step; invalid actions must have gentle but clear feedback, not silent snapping. Real contact and evidence state.

**Mom playable interrogation:** visually staged in the house with Mom's real humanoid, gaze and gestures. Physical evidence cards, witnessed memory interface, hand-based choice/pointing and optional voice; can proceed without microphone, backend or latency. Short duration respectful of the single match clock. Avoid dramatic forced player camera motion. Siblings can offer only honest witnessed data or clearly labeled testimony; Mom may be wrong based on legitimate uncertainty, never because backend invents world truth.

**Results:** one decisive end, collectible/reward flair, cartoon score podium, all four personal contributions, accurate Mom incidents and verdicts, unlocks/XP only for validated outcomes, saved appearance, replay 5/10, return to Family Stage, exit. No old three-round intermediate elimination screens. Character celebration animations and original audio stingers.

**Accessibility & multiplayer:** subtitles, reduced motion, hand size/calibration, input remapping, comfort teleport/snap turn, audio controls, no-mic parity, UI scale/contrast and color-independent icons. Different players must not have one shared camera or one shared invisible task board.

---

## 03 — UNITY ARCHITECTURE AND UNINTERRUPTED MATCH MIGRATION

Do incremental refactoring around actual game architecture. **Migrate** old `RoundDirector`/`Elimination` state; do not delete dependent systems blindly. Inventory references by scene/prefab/assembly, stabilize IDs and add migration tests. Possible canonical state flow:

`Boot -> FamilyStage -> Mode/AvatarSelect -> Lobby/Calibration -> MatchSetup -> ContinuousMatch -> (MomWarning / Investigation / BriefPlayableConfrontation as nested events) -> MatchExpiredOrGameOver -> Results -> Rematch/Stage`.

The match timer is single authoritative monotonic network time source; not each client's `Time.deltaTime` counter. 5/10 min duration is host-chosen, stored and replicated. Late joins/reconnects receive exact remaining time. No resets after Mom warns or questions; the house and tasks persist unless explicit gameplay action repairs them. Avoid global pause during menu map viewing unless all players are in a separate non-match safe state.

**Suggested modular boundaries; adapt to existing source names:**
- `MatchState`, `ContinuousMatchDirector`, `AuthoritativeClock`, `MatchResultResolver`, `SeatRoster`, `MatchEventLedger`, `ProgressionAwarder`.
- `TaskRegistry`, `TaskRecipe`, `TaskInstance`, `TaskPlanner`, `TaskOwnership`, `TaskValidator`, `TaskAffordance`, `SpawnAuthority`, `TaskSpawnCoverage`, `RecoveryPolicy`, `TaskMapMarker`.
- `PhysicalInteractionAdapter` (Meta XR inputs), `GripAnchor`, `ContactSensor`, `PoseValidator`, `ToolMotionDetector`, `MaterialContactProfile`, `PhysicalResponse`, `NoiseEventEmitter`, `InteractableAudio` and `VfxEventEmitter`.
- `MomPerception`, `MomInvestigationDirector`, `MomBeliefLedger`, `EvidenceWorldTruth`, `WitnessMemory`, `VerdictResolver`, `InterrogationPresenter` and NPC action drivers.
- `BotBlackboard`, `BotPlanner`, `BotMotor`, `BotTaskExecutor`, `BotContactSimulator` and NPC animator/IK controllers.
- `PhotonAdapter`, `WorldAuthority`, `PropNetworkState`, `AvatarPoseReplicator`, `ReconnectSnapshot`, `SeatBotHandoff`; `LiveKitAdapter` is optional and unable to alter authority.
- `HouseMapModel`, `MapCamera/GeneratedFloorPlan`, `PlayerLocationMarker`, `TaskMarker`, `HudPresenter`, `CurvedCanvasBridge`, `AssetArtLibrary`, `AudioBank`, `AdaptiveMusicConductor`.

**Immutable event record** must contain event ID, match ID, sequence/server tick, actor seat, object stable ID, source/target task instance, action type, before/after state, legitimate location/observation, root cause, witness confidence and credited/responsible outcome. Retain older `roundId` only as migration metadata if serialized data requires it. Use idempotent authority updates; no duplicate XP/counter/impact. Event truth and Mom belief never conflated. Avoid sending raw camera frame/video or all hand joints over Photon.

**Resilience:** recover dropped props, unreachable tool, world-boundary failure, hand tracking loss, bot/nav failures, network departure, Unity scene recovery and invalid asset references; allow deterministic test seeds and snapshots. Trigger/VFX/animation cannot grant task completion by themselves. Profile and replace fragile long-run test harness movement without weakening physical predicates.

**Migration tests:** full timer 5/10, pause/remount map, midtimer Mom confrontation, room travel, task sabotage followed by repair, bot takeover, reconnect snapshots, late-running task when 0:00 hits, exact single settlement/reward, no elimination transitions, no old `NextRound` scene loaded, no task reset induced by warnings.

---

## 04 — PROCEDURAL CHORE GENERATOR: 60+ REAL ACTIVITIES, NOT FAKE LABELS

Build a reusable vocabulary of physical operations and a large library of *real* recipe combinations. A recipe is complete only when at least two and at most five meaningful physical steps are executed with actual props/states, its owner and helper attribution works, at least one fair reversible sabotage/complication exists, and one physical repair path is testable. Recipe examples below are **explicit required content targets**; refine asset names without changing semantics. All recipes must be selectable by the generation system and validated individually.

### 04.1 Primitive interaction vocabulary
`PickUp`, `Carry`, `PutDown`, `PlaceInSocket`, `Align`, `Rotate`, `OpenHingedDoor`, `CloseDoor`, `OpenDrawer`, `SlideDrawer`, `Stack`, `Unstack`, `SortByTag`, `Pair`, `FillToMark` (abstract visible level), `Empty`, `WipeStroke` (bounded progress), `SweepStroke`, `BrushStroke`, `FoldPresetTransition`, `HangOnHook`, `PressButton`, `FlipSwitch`, `DialTurn`, `PourSafeProp`, `Scoop`, `Shake`, `ArrangeOrder`, `CountRequired`, `HoldStill`, `Guard`, `Observe`, `Inspect`, `ShowEvidence`, `Repair`, `ReturnObject`, `CleanVirtualSpill`. These must work with simplified/forgiving VR collision, physical animation and explicit finish criteria; no demanding precision near unsafe real-world surfaces. Avoid unbounded fluid/cloth/fire solvers on Quest; implement *genuine interactive state-driven physical approximations* with real contact, wetness/flow, fabric deformation and appropriate VFX as defined in Section 24.

### 04.2 Recipe data model
Each recipe: `id`, `displayName`, `room`, `difficulty`, `durationEstimate`, `participants`, `steps[]` with verb/target/object/tag/order/undo, `propPrefabIds[]`, `anchorIds[]`, `startState[]`, `completionPredicate`, `noiseRisk`, `quietPath`, `sabotageVariants[]`, `repairVariants[]`, `witnessableEvidence[]`, `botCapabilities[]`, `handModeSupports`, `controllerSupports`, `availabilityRules`, `scoreWeights`, `cooldownPolicy`, `QA_scene`, `provenance`. Designer-authored seed variants and dynamic object placements produce procedural variety, but avoid unreachable/cross-wall targets. No duplication masquerading as 60 activities: each has a distinct meaningful state goal or action combination and is visibly recognizable.

### 04.3 Distribution & generator
Generate task distributions/continuous-match waves based on active player count, skill tier, last-used recipes, furniture location, accessible room zone, number of grabbables, average reach and quiet/noisy action budget. Fairly balance task difficulty, difficulty of rework and proximity between participants. Do not spawn mutually exclusive objectives on one prop simultaneously or place a task over an occupied object. Have a validator prove all starting states, required props and goals are reachable, with at least one comfortable grab angle. Assign ownership before spawning. Support temporary helper role via actual interaction—not a button that claims credit. A procedural seed is logged for reproduction.

### 04.4 Mandatory task catalog
For compactness `P:` means physical steps, `S:` reversible sabotage/complication, `R:` physical recovery. The implementation must unpack each into explicit data and test steps. These 72 recipe IDs are a canonical content crosswalk; if live repository IDs differ, preserve stable IDs while mapping equivalently. The older physical steps are **minimum stories**, NOT acceptance of grab-and-place. Every one must be upgraded with the Step 05 high-fidelity physical primitive contract from Section 24 (tool contact, forces, progress, VFX, audio, network authority).

#### LIVING ROOM — 14 recipes, LV01–LV14

- **LV01 Shelve bedtime books:** P: grab two scattered books, orient their spines, release each into correct shelf slots. S: rival quietly removes/swaps book. R: identify displaced book, restore proper order. Evidence: relocated spine, witness action, shelf state.
- **LV02 Stack magazines:** P: collect two magazines, align edges, place stack on coffee table tray. S: unstack or hide top magazine. R: retrieve and rebuild stack. Evidence: moved magazine, visible paper pile.
- **LV03 Rescue the TV remote:** P: locate remote, collect it, turn it button-side up, dock by TV. S: move it behind cushion. R: search and re-dock. Evidence: fingerprint-like contact event, witnessed movement.
- **LV04 Gather toy blocks:** P: find colored blocks, open toy chest, place groups by color, close lid. S: spill or mix blocks. R: sort and reseal. Evidence: chest position, scattered objects.
- **LV05 Park toy cars:** P: collect three toy cars, orient forward, line them on marked parking shelf. S: move one vehicle. R: restore sequence. Evidence: vehicle moved, noisy wheel rattle.
- **LV06 Match scattered shoes:** P: locate partner shoes, pair by model/color, stand them on rack. S: mismatch pairs or conceal one shoe. R: find correct match, re-pair. Evidence: distinctive shoe position.
- **LV07 Rebuild cushion arrangement:** P: grab two cushions, turn patterns forward, place at sofa corners. S: dislodge cushion. R: reposition. Evidence: cushion contact and witnessed displacement.
- **LV08 Fold and store sofa throw:** P: pick up throw bundle, trigger 2-stage authored fold, align on armrest. S: unfold/drag throw down. R: refold and store. Use interactive two-hand anchors and skinned folding deformations; not an instantaneous dropzone mesh swap.
- **LV09 Clear cups and coasters:** P: collect empty mugs, set each on tray, return coasters to coffee table. S: move cup or coaster. R: reclear and rearrange. Cup collision sound must be risk-scaled.
- **LV10 Arrange family board games:** P: pair box lid/base, place pieces inside, align two game boxes on shelf. S: slide pieces or remove lid. R: restore complete set. Evidence: exposed pieces.
- **LV11 Rehang crooked photo:** P: pick fallen frame, lift to wall hook, align upright until snap. S: tilt or remove it. R: rehang and align. No fragile glass shards.
- **LV12 Clean pretend popcorn:** P: use scoop/tray to collect cartoon kernels, transfer to bin, wipe table. S: knock lightweight popcorn bowl. R: repeat safe cleanup. Evidence: kernel cluster and noise event.
- **LV13 Return TV controller/gamepad:** P: locate controller, collect its detached accessory/strap, place in station. S: move controller under side table. R: recover both parts. Avoid copyrighted platform markings.
- **LV14 Quiet the squeaky dog toy:** P: find squeaky toy, gently pick it up, place inside padded toy bin, close lid. S: rival triggers a squeak or relocates toy. R: silence and store. Noise attribution must distinguish trap setter versus careless user.

#### KITCHEN & DINING — 16 recipes, KT01–KT16

- **KT01 Wash and rack plates:** P: collect dirty plate, perform forgiving bounded sponge gesture, show clean state, place plate upright in rack; repeat as assigned. S: remove/reroute clean plate. R: wash if dirtied or return to rack. A real physical flow/fill/rinse task state is required; use performant visualized liquid approximations.
- **KT02 Cup cleanup:** P: collect empty cup, rotate upright, rinse with real bounded faucet-flow and fill-state effect, place in rack. S: hide cup on counter. R: re-rinse/dock if necessary.
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

---

## 05 — SABOTAGE, PHYSICS COMEDY, RECOVERY, COVER-UPS AND FAIR BLAME

Core principle: **Every prank is a physical action with a real consequence, possible recovery and traceable but not omniscient evidence.** Keep approximately 80% opportunities in normal household objects and 20% exaggerated safe cartoon props/traps. Avoid damaging the room permanently, hiding critical objects out of reach or making one player indefinitely griefable. Implement target-specific cooldowns, interference limits, repair protection and debuggable responsibility.

Types: (1) undo completed task by removing/misplacing object; (2) introduce reversible clutter; (3) swap tags/objects; (4) create distracting noise by physically tapping or dislodging a designated safe prop; (5) place a visible prank affordance such as squeak toy or goofy spring box at designated spots; (6) exploit pet movement by leaving a toy in a legal pet path; (7) cover up by quietly fixing mess or moving evidence **without falsifying logged truth**; (8) help victim recover and earn capped actual helpfulness; (9) exploit rival interruption by timing a distracting activity; (10) intentionally create an avoidable hazard that triggers a warning. All use physics constraints appropriate for headset. No malicious-sounding injuries or real harmful instructions.

**Responsibility algorithm:** Log each causal action with actor and placement, inspect avoidability and witness provenance. If an object was rigged to create unavoidable noise when an innocent sibling follows a valid chore instruction, blame the trap setter where Mom has evidence. If a player carelessly smashes a normally safe object, responsibility can shift to that player. If Mom has insufficient evidence, uncertainty remains, and the verdict uses credible evidence and chore outcome rather than cheating. Maintain `truthCulprit` for QA and `knownCulpritCandidates` with confidence for Mom. Test ambiguous cases, multiple causal contributors, pet-induced events, failed sabotage attempts, allied help, and late repairs.

**Examples:** Pip leaves a squeaky toy inside an easy-to-grab laundry basket; Nell reaches in following her task and squeak occurs. Game truth records Pip's setup and Nell's unavoidable activation. Mom hears the squeak but does not magically know Pip planted it. Rory may have witnessed Pip. If Rory presents credible memory, Mom can shift suspicion. If no one saw Pip and the toy gives no distinguishable clue, Mom may instead favor whoever has unfinished chores; game outcome is allowed to be imperfect but must be rule-consistent. In another case, Nell throws a pan while trying to frame Pip; game truth records Nell's action, and Mom may infer from physical inspection or witness rather than a hidden cheating flag.

**Bot sabotage:** Must physically approach, reach, pick, carry, release and verify object changes, with same cooldown and visibility costs as humans. Bot 'intent to sabotage' is not itself a completed sabotage. Failure should be visible: misses, interrupted grabs, pet collision, relinquished object. Strategic bots cannot instantly modify task data in code to imitate sabotage.

**Anti-exploit:** No self-generated cleanup XP, loop spam, impossible placement, object duplication, prop translocation, intangible hands, task completer stealing without ownership logic, safe-zone griefing, deliberate countdown freezing, spectator interference, or repeated evidence forgery. Include telemetry counters and reproducible stress tests.

---

---

## 06 — MOM AI, PERCEPTION, CONTINUOUS-MATCH CONFRONTATION AND CAUSAL BLAME

Mom is the recognizable character in the approved reference: brown sculpted bun and loose face curls, purple robe, floral cream/pink apron, plush slippers, expressive face, genuine full-body humanoid rig and coherent animation. She sleeps in an **enclosed upstairs bedroom**, not a balcony or floating stage. Her physical state changes: sleeping/quiet breathing -> stirring -> partial wake -> short verbal warning -> sits up/leaves bed -> opens bedroom door -> walks/turns down real stairs -> investigates true noise location -> inspects and questions -> returns to sleep, remains patrolling, or issues a short penalty as state allows. No teleporting or sliding through closed doors. Actual navigation must handle blocked passages and other players.

**Sensory system:** logical `WorldTruth` (authoritative events) and `MomBelief` (only perceived observations) remain separate. Hearing depends on sound profile, material, impulse, room, distance, floors, closed doors and believable attenuation. Vision requires angle, line of sight, range and attention; object inspection requires Mom to be physically near; witnesses can provide contradictory claims but only have their own recorded memory. A player closest to a crash is not automatically culprit; a pet acting independently is not automatically a player. A sabotager who made a rack unsafe may be root cause, while a victim who had a clear chance to prevent a crash can carry bounded shared responsibility. Log the policy in comprehensible terms and test both outcomes.

**Single match behavior:** suspicion rises/falls throughout a 5/10-minute clock. Short room-specific warnings and grumbles are allowed. A major confrontation is a short playable experience, normally **at most one full 30–60s phase per match**, with quick subsequent challenges for further critical incidents; keep strict time budget and fairness. Don't suspend all gameplay indefinitely. After a critical sound, Mom may follow a real route and investigate instead of instantly summoning all siblings. Build corresponding HUD/map alerts, sound/music transitions, witness card affordances and NPC face/gestures.

**Interrogation:** physical evidence props/cards and an optional witnessed-memory panel. Player points, picks a card, makes a yes/no/doubt gesture or speaks optionally via LiveKit. Speech neither creates new world facts nor grants a superior persuasion score; full parity with hand controls. Mom can react naturally using filtered observations; deterministic verdict resolver chooses an explainable proportional warning, repair assignment or brief time/score penalty. Never full player elimination by old round logic. At timer zero, Mom performs short final inspection/result reaction as appropriate; does not restart another round.

**Audio:** Mom's sleep breath/snore, shoes on wood/tile, door, floor creak, gasp, grumble, suspicious hmm, warning, questioning, angry and relieved expressive nonverbal sounds plus actual bundled offline voice lines. Optional LiveKit AI TTS provides dynamic text/voice, with safe latency fallbacks and subtitles, facial lip-sync drivers and room spatialization. LiveKit is currently blocked by missing backend/credentials: build the backend skeleton and local fallback and do not hardcode secrets.

**AI bot parity:** every bot has bounded motive, legal prop reach, explicit task executor, contextual movement and genuine evidence. Bots can be temporarily helpful, strategically sabotage, conceal or repair; they must not complete physically impossible tasks because an animation clip played. Support bot witness reports and innocent mistakes; log actual cause and observed cause separately.

---

## 07 — SIBLING BOTS: FAIR EMBODIED STRATEGY AND OPTIONAL CONVERSATION

Pip: impulsive and lively, goggles/red hoodie. Nell: thoughtful, observant, glasses and purple. Rory: opportunistic and teasing, cap/blue. Tess: outgoing social negotiator, yellow/pink. These are *starting personality priors*, not hard-coded morality roles. Rory can sincerely help; Nell can sometimes mischievously sabotage; Pip can show restraint; Tess can betray after cooperation. Maintain unpredictability from state rather than random constant chaos.

**Bot hierarchy:** utility/GOAP or bounded behavior planner selects from chore, assist, sabotage, repair, hide, investigate incident, warn, seek ally, flee noisy location, collect prop, present witnessed memory, respond to Mom and spectate. Planner reads its own sensed world and known relationships; motor executes legal NavMesh/physical interactions. Audio/dialogue uses LiveKit when enabled; fallback templates and gesture meanings remain available. No LLM-driven direct transform manipulation or authoritative scoring mutation. Budget decisions in infrequent ticks, cache small world summaries, use hysteresis to prevent flip-flop, timebox remote requests and cancel stale goals.

**Social memory:** Record validated promises/help/betrayals seen by the bot, suspicion, reciprocity, grudges, confidence and time decay. Mutual support may improve willingness to help or defend, but cheating is permitted and alliance never alters game fairness. The bot can refuse a new alliance after witnessing a betrayal; it cannot know an unseen betrayal. Bot speech uses persona but never reveals future card secrets, private player data or authoritative truth.

**Physical presence:** Detached visible head/torso/hands/shoes still animate as one coherent character. Idle subtle bob, look target, lean, happy shrug, worried flinch, excited step, carrying pose, push/pull, place, drop, sneaky glance, nod, shake head, shush gesture, finger point, accusation, laughter, panic, celebration, crouched sneaking (comfort target only), and spectator reactions. Hands contact props with authored grip anchors; limbs should not stretch invisibly from shoulder to object without posing the floating hands credibly. Foot-to-floor alignment and shadows support grounded physicality. On path failure, retry with visible recovery and no teleport beyond legal room transitions.

**Bot quality targets:** Can finish every available primitive action; 100% of 72 task recipes have compatible bot step mapping or documented alternate physical execution; performance budget not tied to LLM latency; observe, plan and physically perform at least 10 diverse full activities in tests; player can meaningfully influence them with gestures and optional conversation. Use simulated tests with intentional mistakes and confrontation proof.

---

---

## 08 — CHARACTER ART, RIGGING, ANIMATION AND CUSTOMIZATION PIPELINE

Study the user-supplied four-view sibling sheet and Mom turnaround. They are **identity and silhouette references**, not literal UV maps or pre-rigged source geometry. Build editable Blender sources in named collections: original high-detail sculpt/model, game topology, hair/clothes, facial components, floating pieces, skeleton/constraints, animation actions, LODs and collision. Approved four siblings: Pip (goggles/red), Nell (glasses/purple), Rory (cap/blue), Tess (pink bow/yellow). Preserve personality and character readability even after cosmetics change.

**Sibling visible structure is fixed:** separate head, torso, two hands, two shoes, with NO visible arms or legs connecting pieces. A **hidden internal humanoid-compatible motion hierarchy** may exist to animate natural body timing and share/retarget motion. Do not assume Unity's ordinary humanoid avatar auto-rig will produce correct visuals without custom constraints; evaluate humanoid vs generic rig/retarget bridge, document tradeoff and implement actual transform/bone mapping. Head orientation toward interlocutor, independent hand posing, torso lean, shoe placement and root-motion compensation must stay coherent. No visible ghost limbs, disconnected hand collision jitter, or floating torso that ignores floor. Build full animation coverage listed in section 07, plus sleep, wake surprise, fall-safe reaction, apology, accusation, carry heavy object and spectator gesture. Include expressive face controls: blink, lip-sync viseme/shape or practical 2D mouth atlas, eyebrows and eye gaze with fallback.

**Mom:** conventional full-body rig (visible arms/legs), strong deformations at shoulder/elbow/knee, facial controls and hand grip targets. Make her robe/apron folds stylized but game-ready; avoid runtime cloth physics. She must handle walking, stairs, turns, door opening, inspection and interrogation while keeping believable hand-object contact. Do not fake her investigating by spawning a duplicate static statue. Her path starts in enclosed bedroom with valid bedroom door/hallway geometry.

**Mochi/Biscuit:** stylized cat/dog with walk, idle, sniff, sleep, alert, paw nudge, startled and playful reactions; low-cost behavior and animated props. Pet collision must not shove the user camera or create unfair unavoidable noise. Don't give pets unrestricted social agency; they are bounded independent actors.

**Customization:** modular outfits/hoodie styles, hats/caps, goggles, glasses, headbands/bows, hairstyles, shoe color/models, accessories, basic material colorways, unlockable emotes. Accessories attach to proper face/hair/body anchors and are excluded when colliding or hiding facial expressions. Provide a catalog with stable cosmetic IDs, unlock constraints, render cost, prefab references, thumbnails and actual save/load. Give all four siblings equivalent customization depth, but retain identity silhouettes and recognizable faces. Selected cosmetics replicate online with fallback to default if remote lacks asset.

**Quality gates:** orthographic front/back/left/right, three-quarter, expression board, gray/texture/final beauty, Unity URP actual prefab, turning animation, hand-contact pose, stress test with accessories and far-LOD. Characters must not T-pose in play. All animations have named root convention and import settings; document animation source/license. Verify humanoid or adapter rig type in Unity rather than merely labeling a Blender bone tree humanoid.

---

---

## 09 — HAND TRACKING, CONTROLLERS, LOCOMOTION AND PHYSICAL CONTACT

The project already contains the Unity Asset Store **Stylized - Simple Hands** package; reuse the existing installed models, not a guessed duplicate. Audit current white-hand prefab, mesh scale (prior prefab was oversized), local axes, left/right mirror, 16-bone skin, material and skeleton mapping. Retain Meta XR/OpenXR tracking and grab/poke/ray functionality; replace only visual skin where needed. The visual mesh may have fewer bones than tracked joints; implement stable pose retargeting and preserve pinch/index/opposition behavior. The user's supplied gripping screenshots exposed a pinch-pose issue: thumb and index sometimes approached the same side of a book. Make opposing contact across book faces and test from multiple camera angles. Avoid fists visibly penetrating covers or fingers that are huge relative to everyday props.

**Hybrid movement C:** primary tracked hands and interaction, optional controller locomotion using sticks where available, optional gesture/hand-UI movement scheme when controllers are absent, teleportation and snap-turn comfort choices. A user using hand tracking only must still traverse the entire house without requiring their physical room to be the size of the virtual building. Gesture travel must not collide with normal grabbing/pointing; select explicit movement mode, e.g. locomotion anchored hand menu or supervised point-teleport. Provide seated/standing floor calibration, height adjustment, handedness, snap-turn increments, optional vignette for smooth turning and movement speed cap, disable physical camera shake, and conservative stairs/threshold handling. Never attach permanent HUD to face or force player lean toward unreachable objects.

**Interaction tests:** both hands can pinch, grab, direct poke, ray select, open drawers, interact with doors and appliances, lift/rotate/stack props, activate contextual buttons and let go. Tracking loss makes object remain consistent and recoverable; hand switching while carrying behaves predictably; no object tunnels through walls or drops unpredictably. Each grab needs pose target, thumb opposition, collider and rigidbody constraints, contact SFX, optional subtle haptic for controllers and visually gentle feedback for hand-only. Every mode has an accessibility equivalent, including nonvoice witness response. Spectator or post-match hands cannot alter world physics; all four siblings remain active during the continuous match.

**VR comfort and spatial scale:** 1 Unity unit ≈1 meter; believable table and shelf heights, sufficient doorway clearance, no oversized book at eye level. Validate stereoscopic depth and reach with real headset. Ray panels generally at comfortable distance and slightly curved when appropriate; direct poke zones near hand reach. Don't apply aggressive bloom, motion blur, depth-of-field or strong full-screen distortions in headset. No artificial head bob or forced cinematics with fast camera motion.

---

---

## 10 — PHOTON MULTIPLAYER, AUTHORITY AND PRIVATE MATCHES

**Audit first:** Determine actual installed Photon product and version from current code/packages. Do not silently replace PUN with Fusion, change backend or introduce a second network transport. If Fusion is already present, use its authority model and tick/snapshot approach; if another Photon stack is installed, adapt stable abstractions and justify any migration separately. Private rooms require room lifecycle, join code, ready state, avatar/seat selection, late/rejoin logic, reconnection grace, bot fill and clean leave. No fake networking UI.

**Authoritative ownership:** One designated authority validates match timer, assignments, prop state, noise, evidence, Mom belief, bot actions, single-match results/short Mom penalties and progression rewards. For Shared mode, define who holds state authority and how handoff works when that peer disconnects. For host/server mode, document host migration limitations and provide safe exit/restart if full migration not feasible. The client streams pose intent and validates in-world reach for grabs and task events, rather than trusting arbitrary remote scoring requests. Replicate avatar root/head/hands, key prop transforms and ownership transitions with smoothing suitable for Quest. Avoid sending every finger joint at full physics rate if unnecessary. The local tracked hand remains responsive while network correction avoids visually explosive snaps.

**State updates:** Prop state ownership should be single-writer at any instant; task completion cannot race with sabotage; release/grab conflicts resolve deterministically; object transfer is acknowledged. On rejoin send snapshot + ordered authoritative events; don't duplicate XP, culprit decisions or objectives. Bot takeover includes exact seat and state handoff. LiveKit may carry conversational audio/data but MUST NOT independently mutate authoritative gameplay or be treated as a second object-replication channel.

**Test matrix:** Solo 1 human +3 bots; private 2+2, 3+1 and 4+0; room full, wrong code, duplicate selection, reconnect after grab, disconnect during Mom interrogation, packet loss/latency, two users grab same book, two users sabotage while owner repairs, post-match spectator attempting physics, AI/bot handoff, match end and rematch. Record actual two-client or multi-device evidence. Local fake multiple avatars do not qualify. If only one Quest device available, use separate network clients for desktop simulation but label headset tests as pending.

---

---

## 11 — LIVEKIT OPTIONAL VOICE AI FOR MOM AND SIBLINGS

Use the installed `shhh-livekit-unity-voice` skill and, if available, actual LiveKit Agents and Unity SDK docs matching installed versions. Audit where the runtime agent backend lives and how tokens are minted. Client must not ship long-lived LiveKit API keys or LLM secrets. Tokens are short lived, scoped to match/conversation, delivered via a secure authorized backend. No claim of unlimited free calls or unlimited concurrency; honor provider quotas, cost controls, timeout and reconnect limits.

**Topology:** Unity owns animation, authoritative game events, Mom's decision and bot physical action. LiveKit voice service handles real-time speech turn-taking, optional speech-to-text/LLM/TTS and conversation context scoped by room/player. Agent tool calls request *intents* or question templates, never unconditional `SetScore`, `ApplyPenalty`, `CompleteTask`, `RevealHiddenCulprit` or arbitrary scene writes. Voice service receives a filtered list of Mom observations or bot witnessed memories, not global truth or private client logs. Even if the language model says "I saw Rory", the ledger must verify actual observation or downgrade the claim to unsupported testimony.

**Human social voice:** Optional spatial mic for human-player communication, privacy controls, mute, volume, speaking indicator, safe fallback if denied mic, and clear distinction between speaking to other humans, speaking to Mom and speaking to NPCs. Prevent mic feedback, echo and duplicate voice playback when Photon Voice and LiveKit are both installed. Choose one consistent human voice transport and document bridge to agent if needed. Real 3D voice position should follow player's mouth/head; Mom and NPC voices should emanate from their bodies, with sensible distance roll-off and room filtering. A player should not be auto-listened-to by Mom through walls merely because a voice stream exists. Spoken noise events must use an explicit measured/abstracted gameplay policy, not accidental network amplitude secrets.

**Dialogue personality:** Pip energetic; Nell careful; Rory clever; Tess sociable; Mom tired, warm, appropriately stern. Short responsive dialogue, not constant LLM monologues; voice can ask for alliance, accuse with supported memory, apologize, plead, warn and negotiate. Mandatory gesture/card equivalents with equal mechanical strength. If LiveKit unavailable, bots follow deterministic planning, Mom uses recorded/generated locally bundled fallback lines, and every match mechanic remains playable. Agent failures must be non-blocking to continuous match state.

**Privacy and safety:** Default voice opt-in with accessible settings, user consent appropriate to jurisdiction/audience, do not retain raw voice recordings by default, avoid storing personal transcripts, provide mute/block/report mechanisms appropriate to platform and age rating, never echo credentials or sensitive metadata to AI. Add abuse controls for private human communication; no LLM speech used as sole source of blame. Build cost/latency instrumentation without collecting unnecessary user data.

**Tests:** agent online, agent offline, silence, accented English, two people speaking, rapid interruption, packet loss, player mute, no mic, false claim, correct witnessed clue, contradictory witness, speech when Mom physically too far, bot planning while dialogue delayed, join/rejoin mid-voice, invalid tokens and graceful fallback.

---

---

## 12 — CONNECTED HOUSE, ROOMS, NAVIGATION AND ENVIRONMENTAL STORYTELLING

One coherent modern suburban family home, observed from comfortable first-person VR eye level. Canonical dimensions and doors/arches/windows must reconcile all approved concept images. Use the existing V5 geometry as the start, not a new unconnected graybox. Produce an annotated top-down map, object/door/room anchors and traversal routes for human, bot, Mom and pets. All rooms physically connected and properly sealed; Mom cannot see through a balcony/open bedroom wall. Current source had issues with mirror/orientation, window placement and wall crossing a bedroom route; re-check visually in play mode, not solely NavMesh completeness.

**Family Stage:** stationary theatrical presentation in the house style; warm marquee and curtains; physical character pedestals, box/curtain reveal, real buttons. **Customization:** dressing-stage/wardrobe within same visual language, item display, 3D spin/try-on and soft curved spatial selector panels. **Living:** V5 oval oak coffee table, fully sculpted sofa/armchair, shelf, books, textured wood floor, rug, curtains, photos, lamps, toys, TV, shoe rack, pet bed; preserve V5 functional book anchors. **Kitchen:** genuinely dressed L-shaped work area, sink/plate rack, cabinets/handles, cooker/hood, fridge with child art, dining table with four chairs, fruit/cookies, cookware, organizing props and nighttime moon window; no fake task labels over empty cubes. **Laundry:** washer/dryer with usable doors, wicker baskets, folded clothes, rails, hooks, towel piles, detergent bottle props, safe folding surface, night window, trim and rugs. **Bedrooms:** Mom's private enclosed upstairs room with bed/bedside, wardrobe and door; one or more children's bedrooms with distinct play/study objects for BR recipes. **Bathroom:** compact clean stylized space with reachable safe props. **Foyer/hall/stairs:** safe traversable, signed, textured and dressed paths, proper collision and no visible voids. **Pet area:** reachable separate beds, bowls, low shelf, toys and animated pet path.

**Art story:** Evidence of four kids living here: drawings, homework, game boxes, stickers, toys, photo frames and minor task-related clutter. Static decorative pieces are merged/batched/reused; gameplay props have individual stable prefabs, IDs, collision and legal transformations. Have deliberate negative space around primary grabbables and doorways. Room visual target is not maximal prop count; it's handcrafted richness with high functional readability.

**Connected navigation QA:** For every room human path, bot path, Mom path and emergency return, verify proper doorway widths, collision walls, ceiling clearances, stair landings, usable hand height, task objects, absence of blocked doors and absence of object collision with XR player capsule. Include at least one fully recorded walk from living room to kitchen, laundry, upstairs Mom bedside and back. Run automated NavMesh paths but also inspect camera footage for clipping and teleport-through-wall errors.

---

---

## 13 — BLENDER ART PRODUCTION: THE 'HANDCRAFTED' GOLD STANDARD

Reference hierarchy: (1) approved SHHH! gameplay concepts, character turnarounds and logo; (2) current V5 source assets and established geometry; (3) uploaded stylized fantasy examples for *handcrafted mesh/texture craft only*; never transplant Viking, fantasy cottages, magic, weapons or horror. The style name is **SHHH! Soft-Sculpted Storybook Toon (Quest Edition)**: broad rounded silhouettes, mild deliberate asymmetry, carefully modeled cushions/edges, hand-painted color gradient and AO/curvature accents, softened bevels, illustrated texture detail, clean readable objects, warm cream/honey wood, lilac/plum upholstery, coral detail, cool moonlight and amber lamps. Avoid raw cubes with plain flat colors or procedural noise masquerading as paintings.

Use existing editable Blender source for V5 furniture. Reopen and inspect sofa pillow intersections, curved arm shading, wood grain, negative normals, UV mirror text and bevel weighting. Every hero prop needs correct-scale mesh, named collision proxy, stable anchor/handle, proper origin/pivot, neutral pose, UV atlas where useful, authored albedo and optional baked normals/AO, restrained roughness, image texture files and Unity prefab. For cloth choose sculpted/baked fold shapes rather than realtime cloth. For small items, atlas/batch; don't insist every decorative prop have its own 4K textures.

**Per-asset pipeline:** reference crop/turnaround → silhouette blockout → sculpt/model → retopology/LOD → UV/lightmap UV → hand-painted/stylized base color → optional bake AO/curvature/normal → material lookdev → Blender preview → consistent export orientation/units → Unity import/material remap → collider/rig/anchor setup → actual gameplay camera review → static/dynamic batching decisions → Android performance checks. Use separate source meshes from runtime FBX/GLB. Preserve licenses for imported store assets.

**Acceptance criteria:** no stretching/intersecting sofa cloth; no inverted text on book covers; no missing faces; no one-sided impossible normals; no material mismatches; no floating table feet; no action anchor buried in furniture; no implausibly oversized doors/books/hands; no yellow light that washes out all purple; no texture-only workaround that leaves large silhouettes square. Compare matched-angle Blender vs Unity screenshots and annotate differences. Scene must look inviting without HUD.

**Proposed initial art proof:** V5 living room existing art passes gold gate when its sofa, rounded table, bookshelf, illustrated books, textiles and ambient props look cohesive in Unity under Quest-like URP. Then scale same material/geometry language across the house; do not replace complete Blender assets with cubes merely because the queue is long. `AssetManifest` should track source, runtime prefab, texture budget, tris, colliders, task relation, visual rating, Unity validation and Quest validation.

---

---

## 14 — URP LIGHTING, MOONLIT SKYBOX, SHADERS, POST AND VFX

**Lighting identity:** cozy family house at **night**, not sunny daytime. Moonlit navy exterior with simple silhouettes/stars, warm window glows across street, cool subdued blue environmental fill; amber practical lights from pendant, lamp shades and cabinet strips. Build a genuine night skybox/panorama that matches each exterior window's direction. Avoid repeating identical moon in every mismatched window or giant impossible external buildings. Lamps use a budget-conscious combination of emissive material, baked point lights and selective shadow; dynamic characters get light probes, contact and carefully limited dynamic shadows.

**URP:** preserve 17.0.4 renderer assets and explicit quality tiers for Quest 2 baseline and Quest 3 higher tier. Baked lightmaps, reflection probes as appropriate, SRP Batcher-compatible shaders, limited transparencies, texture atlases, shader variant stripping, optimized shadow distance, minimal real-time light count, fixed/budgeted render resolution and MSAA under actual headset testing. Avoid desktop-only screen-space AO or volumetric effects unless proven safe on target. Shader Graph and HLSL must compile on Android GLES/Vulkan rendering path used by the project; don't trust a Windows DX11 preview.

**Materials:** reusable stylized lit shader(s) for wood, plush, painted plaster, ceramic, plastic, metal, glass fake, emissive accents, softly painted toon/ramp. Use painted albedo as main art content, gentle secondary highlights, reasonable roughness and stable specular response; not a harsh full black outline on every object. Robust handling for light probes, shadow casting and static baked illumination. Ensure no pink shaders, huge variant explosion, mirror flips or breaking GPU instancing.

**Post-processing:** subtle exposure, contrast and color grading. On standalone Quest no motion blur, cinematic depth of field, heavy bloom, expensive screen-space ray effects or temporal ghosting of hands. If bloom enabled, only when truly measured as cheap and visually necessary; otherwise use mesh glow sprites/emissive fake. VFX set: grab focus, placement settle burst, quiet-footstep cue, soft dust puff, dropped object impact, noise ripple, Mom suspicion pulse, warning, witness memory highlight, successful chore spark, failed placement and comedic victory. Use Shuriken particles/mesh VFX/flipbooks primarily; don't insist on GPU VFX Graph effects incompatible/expensive on standalone mobile. Use low overdraw and consistent diegetic origin.

**Performance:** Baseline target minimum 72 sustained frames/sec on Quest 2, with better tiers where physically supported. 72 Hz corresponds to ~13.89ms per frame total system/render/game budget; don't claim entire 13.89ms is available to application CPU or GPU. Measure actual headroom separately, including stereo rendering, CPU/main thread, GPU, memory, draw calls, thermal sustained play and battery. Test worst-case four avatars, interactive props, pets, speech/voice, particle noise and two rooms visible through arches. Scene beautiful in Blender but unreadable or low-FPS in headset is not finished.

---

---

## 15 — STEP 01: COMPLETE REPLACEMENT OF ALL UI/UX, HUD, ART, BRAND AND HOUSE MAP

**The owner explicitly rejects the current entire UI/UX as bad and unacceptable.** Do not apply a small redesign, recolor gray panels or merely enlarge text. Replace the complete *visual grammar and spatial interaction flows* with original high-quality cartoon game interface design. References the user attached (including Angry Birds VR-style stage/menu, playful reward/results panels, vivid spatial cards, cartoon kitchen game UI) illustrate **energy, dimensionality, art direction, fun and polish**, **not permission to copy assets, layout, logos, lettering or characters**. Original SHHH! design is mandatory. Review the actual current `YOUR JOB`, `PRACTICE`, HUD and onboarding screenshots and list why they are unreadable/overly large/boring; resolve each with new production assets and gameplay-aligned interaction.

### Exact branding
Use existing approved SHHH! title/logo as reference. Keep yellow chunky sculpted lettering, purple extrusion/outline, coral accent, crescent/home motif, and **DON'T WAKE MOM!** subtitle. Palette baseline from existing brand kit: midnight ink `#211530`, deep shadow `#261239`, purple `#70409F`, hush yellow `#FFCF2E`, moon cream `#FFF0BA`, coral `#F57F78`, pink `#F45F73`, soft white `#FFF8E8`. Treat these as art-direction anchors, not rigid constraints on shading; legibility and contrast prevail. Display font: approved expressive Luckiest Guy-inspired original logo letter shapes; Fredoka-style chunky cartoon display text, clean readable Nunito Sans-like body when font licenses permit. If font not present/allowed, use a verified OFL/commercially licensed alternative and document its files/license; never distribute proprietary font files without rights. Game UI must be **English** initially.

### Exact native art tools
**Inkscape:** author the original SVG primary logo and true vector shapes, illustrated icons, HUD meter frames, button states, ornate banners, room glyphs, map icon atlas, curves, nine-slice borders and scalable illustration masters. Keep editable `.svg` sources and stroke/outline correctness. Export resolution-appropriate PNG or compatible Unity sprites. **Krita:** create hand-painted backdrops, physically themed surface textures, illustrated panel trims, soft painterly shading, cartoony visual motifs, large UI art atlases, poster/house map parchment, painted sky/cloud textures and icons requiring brushwork. Keep editable `.kra` and exported PNG; true art files must exist, not only procedurally assembled generic shapes. **Blender/Unity:** build physical 3D/extruded frames, podiums, physical scoreboards and stage props; create exact interaction/animation/prefab hierarchy and use URP materials. If these installed desktop applications cannot be automated by the available execution tools, use their native CLI/documented automation or create native editable assets with available methods; report limitations rather than claiming program operation without evidence.

### Complete new UI art/component kit
Create a normalized master art kit: 3D and 2D button families (primary, secondary, tab, toggle, icon, slider, disabled, held, confirm, warning, rival color); background/panel families (curved, book/card/clipboard, wardrobe, evidence frame, wood/cream/purple stage); icon families (house/rooms/floors, task type, check/progress, scrub, faucet, spray, laundry, toys, pet, caution/noise, voice/mute, friend/ready, locomotion/teleport, home/map, settings, stars/XP, suspicion, repair and sabotage); animated affordances and small celebrations. Produce hover/pinch-push/selected/disabled/error states, sprite atlas, theme tokens, actual prefabs, localized layout constraints and style guide. Use UI tooltips only where appropriate; action hints appear near the *actual object*, not a permanent huge wall.

### Mandatory screens and overlays
`Boot/Comfort`, `FamilyStage`, `SoloSetup`, `CreatePrivateRoom`, `JoinPrivateRoom`, `FriendsLobby/Ready`, `CharacterSelect`, `Wardrobe/Customize`, `TutorialPractice`, `HUDMinimal`, `PersonalMap`, `TaskDetails`, `NoiseSuspicion`, `MomIncoming`, `ConfrontationEvidence`, `WitnessMemory`, `QuestSettings`, `Volume/Consent`, `Accessibility`, `Pause`, `ConnectionFailure`, `Reconnect`, `Progression/Unlock`, `TimerFinalMinute`, `FinalPodium`, `Rematch`. Add loading/empty/error/disabled states and explanatory no-voice choices. Every displayed button/slider/tab must operate and have a verified navigation route. Create 3D material and audio feedback integration.

### House map — critical new feature
A physical, beautiful interactive *personalized* miniature floor plan or illustrated folding map, generated from actual connected house geometry / named room anchors (not a random decorative floor plan). Open using one obvious hand-controller affordance (wrist button, pocket map or natural world-space key) without obstructing action. Show own position, facing vector, floor level, accurate connected halls/stairs, all room icons, task locations, owned objectives, remaining steps and distance/path hint if available; map updates while moving. Task icon signals colored by type and status plus shape for colorblind use; reveal rival positions/tasks only according to the game fairness policy and actual sensed/shared public info. Separate map instance and current-floor state for each human and simulated bot; map can be zoomed, floor toggled and task pin selected with pinch or controller ray. No cheating showing confidential bot knowledge or Mom hidden suspicion. Test a human going from the laundry to the kitchen via hall/stairs while map icon positions follow actual avatar transforms. Map marker must reflect room occupancy and floor level, not simply hard-coded coordinates.

### VR ergonomics
Use Meta Interaction SDK curved Canvas where compatible, otherwise implement a documented world-space uGUI curved mesh adapter with reliable accurate raycast/event mapping and proper TMP font rendering. Prefer short, contextual HUD around the player hand or world props; never glue a giant monitor to camera. Stage/lobby presentation at a comfortable reading distance; deliberately large hand targets, usable padding, readable text, tested stereo depth, safe occlusion and reduced motion. Direct poke for reachable objects, pinch ray for slightly distant menus; no overlapping hover colliders, lost-focus buttons, or invalid trigger masks. Show interactive 3D content and friendly animations rather than static floating web dashboard. No flashing seizures/visual overload; make strong graphics expressive and restrained during close VR manipulation.

### UI acceptance
Supply `.kra`, `.svg`, PNG/atlases, logo variants, font license notes, style tokens, original master designs, Unity materials/prefabs, navigation graph, screenshot comparison **inside the running Unity game**, and actual Quest review if hardware present. Verify each state in 5- and 10-minute modes, with Solo/Friends, occupied hands, left-handed controls and poor contrast rooms. Stage and map must be legible in real stereo; old oversized `YOUR JOB`/`PRACTICE` must not remain as active visual interface.

---

## 16 — STEP 06: COMPLETE ORIGINAL MUSIC, SFX AND SPATIAL SOUND REBUILD

**Owner rejects existing game audio. Replace ALL active old musical content and SFX clips with a coherent new original sound design.** Do a reversible audit and migration; do **not** blindly delete scripts/events that correctly map gameplay actions or replicate sound. Preserve existing reported network-audio event correctness and remap to new banks. The previous 18 new synthesized clips are still considered artistically insufficient; retire them from active content and document archived originals. Produce actual audible clips/tracks and licensed source records, not merely names or audio events.

### New SHHH! audio bible and musical identity
Original **cozy, mischievous, mysterious, energetic stealth-comedy** music, never a horror score or a recognizable melody from a commercial game. Design original title theme/hook, quiet explorative pulse, curious task underscore, mischievous sabotage layer, playful Mom suspicion layer, real-time escalating tick/heartbeat when appropriate, cheeky interrogation cue, final 60s urgency layer, spectacular-but-nonaggressive victory fanfare, comic failure sting, silent breathing room when critical physics sounds must remain heard. Use light pizzicato strings, warm clarinet/bassoon-esque timbres, marimba/soft xylophone, plucked textures, muted piano, brushed percussion and subtle synth/whimsical soundscape **as a style suggestion**, not licensed music copying. Compositional motif must be original. Integrate adaptive stems/vertical layering and/or seamless state-matched loops with phase/crossfade; music changes only from authoritative game states and local location/intensity. Calibrate perceived loudness below important interaction feedback and character speech.

### Authentic production pipeline
Survey installed sound tools (DAW, synth, editor, synthesis libraries, licensed sample packs), and select methods capable of delivering finished music/SFX files. Synthesized procedural sounds are valid only if they pass actual listening/repetition/mix tests; don't claim every audio effect becomes professional merely because a script saved WAV data. Source original recordings or assets with verified commercial permissions; keep a `Audio_License_Manifest.csv` and source project (MIDI/stems/audio edit sessions where available). Never repurpose the audio from the user's other-game references or extract commercial music. Confirm no unlicensed or unidentified music/voice is included in APK.

### Prop interaction SFX — EVERY SIGNIFICANT OBJECT
Implement data-driven `SurfaceMaterialAudioProfile` keyed by material, mass class, contact speed, impact impulse, action type and room acoustic zone. Wood soft knocks/creak/scrapes, ceramic subtle clinks versus loud drops, metal clanks/rattles, plastic taps/squeaks, books/paper folds and drops, clothing soft rustles/wring, sofa cushion squeak, drawer rails and latches, swinging lamp, toy car wheels and rubber ball, toy squeaks, pet bowl/paws/movement, glasslike cups, cutlery and knives on safe board, stove/cooking/stirring/sizzling, footsteps per tile/wood/carpet/stair, kitchen faucet valve/water stream/drips, cup pour/fill/overfill, soap pump/squish/bubbles, spray trigger/mist, sponge scrub/wet cloth wipe, dustpan/sweep, vacuum motor/nozzle blockage, mop water, washer/dryer doors, basket, switch and plug. Effects need short randomized variations, pitch/volume constraint, no 'machine gun' collision spam, sound-state enter/loop/exit, cooldown and distinct quiet/loud behaviors. Do not play a clash sound merely because a tool collider overlaps a surface.

### Characters, pets, Mom, and human voice
Create short appropriate optional vocal reaction catalog for Pip/Nell/Rory/Tess, pet woofs/meows/paws with variety, Mom snoring/breathing, sleepy mumbling, disturbed reaction, footsteps, suspicious `hmm`, inspection, warning, scolding, interrogation question, verdict, satisfied/relieved sigh, return-to-bed. Record or generate original, licensed voice if available. **Mom must not remain text-only because LiveKit is not connected:** include a fully working offline bundled short-line bank with subtitles and facial reaction; dynamic LiveKit voice is progressive enhancement only. Original voices must be suitable for family audience. Preserve player consent controls; human voice chat spatialized with clear mute and no duplicated Photon+LiveKit signal. Do not use real person's voice likeness without permission.

### Audio implementation
Unity Audio Mixer groups: `Master`, `Music`, `Ambient`, `PropSFX`, `Foley`, `UI`, `MomDialogue`, `BotDialogue`, `VoiceChat`, `VFXStingers`; manage bus dynamics and priority/ducking. 3D source positions match actual in-game objects with volume attenuation, no fixed mono-UI sound for a faraway crash; simple room reverb and door/wall occlusion budget compatible with Quest, avoid dozens of expensive real-time reverb sources. Consider installed Meta XR audio components only after checking package support/version/performance. Audio object pooling or bounded emitters, appropriate compression/sample rate/streaming settings, CPU/memory audit.

**Critical separation:** player-heard speaker volume != Mom's logical noise. Server computes sound magnitude, surface material, geometry/doors and culprit from physics event; local volume settings cannot change game detection. Physics object noise can be heard by humans, bots and Mom as consistent but perception-limited abstractions. Sound emission must be deterministic and replicated as compact events, with per-client spatialization; never independently multiply collision sounds across all network clients.

### Audio testing and acceptance
In actual full 5/10-minute match check peaceful, first suspicion, minor accident, escalating event, confrontation and final seconds; voices/subtitles, music crossfades, mute, no-mic, stereo/headphones, Quest speaker mix, networked multi-user prop sounds, late join/host migration, room/floor attenuation and dozens of simultaneous toy/plate effects. No silence where important feedback expected, no headache-level peaks, no repetitive ear-fatiguing cue, no AudioSource leak, no incorrect host-only sounds. Deliver sound bible, original assets, mixer, triggers, fallback voice line index, source/license manifest and capture evidence.

---

## 17 — PERSISTENT PROGRESSION, COSMETICS AND ACCESSIBILITY

Create a modest **local-first**, versioned progress system that records matches completed, validated accomplishments, XP, achievements, cosmetic unlocks and player preferences. No store currencies or in-app purchases are authorized by this specification; concept artwork may show stars/gems purely decoratively. Keep progression rewards after authoritative match results; don't let clients grant arbitrary XP or create farming loops through sabotage/repair. Cosmetic items may be unlocked through play and visible in the Family Stage/Customize area, with preview and save. Persist cross-session locally; cloud/profile sync only if current project already supports it and after checking platform/account permissions. Provide save migration and corrupt-file recovery.

Accessibility includes seated/standing height, reach assistance, left-handed options, snap/smooth turn controls, movement comfort, subtitles/captions, audio channel levels, color-independent task markers, high-contrast UI alternative, input tutorials, optional voice disabled, reduced VFX, text scaling where possible, and panic-free pause. No game mechanic requires a loud voice, reaching far beyond safe bounds, or fine precision motor skill. Consider a guardian-friendly content/privacy setup because game characters are children and private voice rooms may include children.

---

---

## 18 — INSTALLED CLAUDE SKILLS: ROUTE EACH TO THE RELEVANT TASK

The user reports **117 skill folders installed into Codex `.agents/skills`**, plus earlier Claude skill installs. The named capabilities below were present in prior skill snapshots but must be re-discovered in current Claude. **Before any slash invocation**, call Claude Code's skill discovery/help or inspect `.claude/skills` to learn each registered trigger and exact syntax. Skills might appear as `/name`, a slash subcommand or an automatic description trigger; this document names identifiers, not verified executable slash commands. If a skill needs Blender MCP, Unity editor MCP, Scenario MCP, LiveKit credentials or another service, verify availability first. Read the SKILL.md before using and honor its contracts. Skills do not replace a rendering engine or working execution backend. Never fake success because a skill name was mentioned.

**Game design + physical rules:** `game-designer` for chore taxonomy, GDD, fairness, progression and player journey. **Blender handcrafted art:** `stylized-style`, `cartoon-style`, `cozy-wholesome-mood`, `blender-image-to-3d`, `blender-game-assets`, `blender-modeler`, `environment-artist`, `prop-artist`, `set-dressing`, `materials`, `texture-workflow`, `uv-workflow`, `retopology`, `asset-optimization`, `lod-pipeline`, `collision-proxy`, `unity-export`, `qa-review`. **Character body and actions:** `character-artist`, `rigging`, `animation`, `scenario-unity-animation` and `scenario-consistency` when a compatible connected generation service exists. **Spatial UI/brand:** `game-ui-ux`, `game-ui-design`, `scenario-unity-ui`, `scenario-brand-kit`, `ui-ux-pro-max`, `impeccable`, `scenario-game-assets`, `scenario-consistency`; treat general web design suggestions as secondary to native Quest ergonomics. **Unity rendering and world:** `scenario-unity-world-building`, `scenario-unity-rendering-lighting`, `scenario-unity-shaders`, `scenario-unity-vfx`, `scenario-unity-performance`, `shhh-cozy-urp-lookdev`, `shhh-quest-vfx-shaders`. **Multiplayer:** `shhh-photon-network-audit` plus verified version-specific Photon docs. **AI voice:** `shhh-livekit-unity-voice` plus verified LiveKit docs. **Scenario integration:** `scenario` only for actual connected operations, `scenario-game-assets` for game art where available. **Video:** `remotion-best-practices` and `remotion-create` are installed but not relevant to main Unity implementation; only use if explicitly delivering an original trailer/tutorial. Inspect any official Unity/Meta/LiveKit plugins *actually installed*, but do not claim the pasted list proves those plugin connections.

Build `Docs/SKILL_ROUTING.md` with: task, available skills, exact invocation verified, connection availability, output files, validation method, fallback. Apply skills sequentially per objective, e.g., art director → Blender creation → UV/texture → export → Unity prefabs → Quest performance → QA. Never overwhelm a single context with 40 simultaneous agents/prompts. The project instructions should state that skills and authoritative owner constraints travel across subsequent Claude context windows, and maintain current state files as restartable checkpoints.

---

---

## 19 — NONNEGOTIABLE TESTS, REGRESSIONS AND ACTUAL HARDWARE EVIDENCE

Execute separate levels and label them accurately: (1) pure .NET/core rules, (2) Unity EditMode, (3) Unity PlayMode and **simulated hands** (existing harness), (4) honest real device Quest 2 / Quest 3 / Quest 3S sessions, (5) genuine two-or-more Photon clients/devices, (6) enabled LiveKit service when credentials/backend exist. A signed APK or editor camera screenshot proves neither hand tracking nor headset FPS. Each assertion references build hash, scene, seed, package versions, test timestamps, relevant screenshots, logs and actual device used. Missing verification is `UNVERIFIED`, not pass.

### Baseline regression tests
- Before refactor, reproduce `0.6.2_c11` known 61 individually reachable recipe paths, auto-match and selected physical interactions; keep game event log.
- Reproduce and repair R21 `LD04`, `LD08`, `BR08` failures only in combined run; `HW03` harness regressions; `KT01` unstable plate/noise. Avoid applying looser validation threshold blindly to make tests green.
- Existing paired socks/shoes, 4.5 cm join, false slam sound suppression, laundry pile alignment and task undo/repair all need regression tests.
- Existing Photon bot/authority and 18 sound trigger event logic must survive while new art/audio content replaces old.

### End-to-end acceptance list
1. **Menu:** professional finished game-specific art; active buttons; world-space ray/poke; true Solo/Friends/Customize/Settings and room errors; readable text/disabled/loading states.
2. **Timer:** choose 5/10; all clients see same clock; only **one** match; no 4→3→2→1 transitions; interruptions do not reset task world; one finish/results/XP transaction.
3. **House:** all approved rooms accessible and fully furnished with real usable assets/props, coherent lighting, actual nighttime skybox exterior through windows, no missing walls, large black panels or teleporting Mom through obstacles.
4. **Map:** each user's own room/floor/orientation/task markers and selected destination update correctly in physical navigation; icons are informative not cluttering gameplay.
5. **Task catalog:** 72 recipe IDs mapped to actual physically playable props and contact predicates; all 61 prior paths pass and 11 previously blocked can spawn, complete, be sabotaged and repaired. Each recipe has meaningful 2–5 steps.
6. **Interactivity:** all 26 interaction primitives have one working real-scene demo and a substantive chore use; no progress solely from entering a dropzone; motions reflect tool-to-surface contact, fill/volume, cloth endpoints, joint limits and collision.
7. **Mom:** truly rigged and animated, correct bedroom route, bounded hearing/vision/inspection, fair belief/evidence, one long/short optional playable questioning within continuous timer, proportional consequences, no permanent round elimination.
8. **Bots/pets:** all four seats filled by real players/bots, task planner drives actual action predicates, pet events have cause, good avatar animations/expression/prop grip.
9. **Photon:** two actual clients connect to private room; synchronize physical props, tasks, timer, sound/VFX events, avatars/head/hands, wardrobe, Mom and score; re-test pairs after recent changes, simultaneous touches and disconnect/rejoin. Verify which installed network package is actually used; no version replacement without migration plan.
10. **Voice:** game fully playable with no voice/back-end; Mom has original offline line audio; optional LiveKit/voice when configured has safe privacy and accurate game knowledge only.
11. **Graphics:** Krita/Inkscape original source for logo/UI/HUD/map; actual compliant font licenses; stylized handpainted materials; sourced prefabs and native Blender sources; coherent Quest-friendly cartoon VFX; lighting/culling/stereo.
12. **Audio:** old active music/SFX removed/replaced, original coherent mix, unique household foley, spatialization and Mom reaction bank; captions, bus controls and multi-user propagation.
13. **Quest interaction:** hand grabbing/pinching, thumb and fingertip opposition, two hands, lost tracking, gesture/controller locomotion, comfort, readable near/far UI, no camera shake; true hardware where accessible.
14. **Performance:** Quest 2 conservative baseline (aim supported stable 72 Hz, validate actual refresh), GPU/CPU render scale and profile traces, physics spikes in toy chaos, allocation spikes, audio voice counts, shader variant/overdraw, LOD and culling. A claimed FPS without device is invalid.
15. **Progression:** bounded valid experience, unlocked cosmetics saved after each complete single match and restored; no phantom monetization; rejoin never duplicates awards.
16. **Delivery:** working Unity repository, original editable 3D/2D/skybox/audio sources, clean asset/license manifest, build, playable path, test reports, risk register and clear `UNVERIFIED` items.

### Negative/failure tests
Try 0/1/2/3/4 human seats, bot fill, room full, lost network, lost hand tracking midgrab, impossible reaches, spilled water in doorway, Mom investigating during remote grab, task undo during repair, spamming a spray/soap trigger, cloth deformation under two hands, overfilled bucket, multiple users intercepting same falling plate, returning from map with held tool, host leaving midtimer, UI font failure, missing live service, microphone denied, audio mixer muted, controller versus hand tracking, expired timer during confrontation, duplicate end awards, and simulation of lots of toys with real on-device rendering.

**A/B screenshot and video evidence:** previous rejected UI vs new one, old flat window vs new night skybox exterior, primitive kitchen vs art-directed kitchen, old Mom v001 vs approved-quality model, surface contact before/after, new HUD map, actual network avatars and one complete 5-minute continuous match. Label if desktop simulated and where headset footage is missing.

---

## 20 — AUTONOMOUS IMPLEMENTATION ORDER, GATES AND DELIVERABLES

Work methodically in **one sustained production effort** without re-asking already answered design choices. Keep checkpoint commits and automated evidence after each gate, continue into next gate automatically. Use parallel subtasks only if tooling allows it and state/art ownership is isolated. Dependencies first; do not start 200 cosmetic objects before functional input and stable build.

**Gate 0 — Protect and audit baseline:** snapshots/version, exact package & SDK versions, verify tools/skills, repo layout, documentation, asset/license inventory, test baseline and known intermittent errors. No destructive reset.

**Gate 1 — Migrate to one continuous match:** update state machine, 5/10-minute authoritative timer, scores, Mom warning/interrogation outcomes, end/results, all Bot/UI/Photon references; remove legacy 4→3→2→1 active flow; unit and PlayMode tests.

**Gate 2 — Import assets and finish rooms:** audit 7 art/prop archives plus imported Unity Asset Store packages, create license matrix, stage asset import with consistent scale, meshes/materials, UV and pivots. Build missing rooms and room-sized furniture, usable 72 recipe spawn locations, real navigation and environment; low-quality placeholder geometry replaced/refined, Quest cost controlled.

**Gate 3 — UI art system and skybox:** vector/painted source assets via Inkscape/Krita, exact approved SHHH brand, rich physical Family Stage, personal house map, all major screens, interaction sound/motion, original nighttime skybox/environment/lighting. Test in stereo and actual runtime.

**Gate 4 — VR characters, Genies/Mixamo, rigging:** isolate Genies validation and assess artistic fit; Mom and first sibling reference match, avatar rig/IK, Mixamo acquisition where authorized/authenticated, facial expression, animation clips, remote pose mapping, local Simple Hands. Progress to all siblings/bots/pets, wardrobe; avoid generic substitutions.

**Gate 5 — Physics/action primitives and all recipes:** robust 26 action primitives, 12 incidents and five flagship vertical slices, new props and tools, water/cloth/spray, audio/VFX, fairness; then complete/validate all 72 recipes and fix long-run variance and 11 previously blocked locations.

**Gate 6 — Continuous competitive stealth:** bot planner, Mom bounded perception and one-match interrogation, score, assists, sabotage/repair, house map task signals, single end/results. Full solo 5- and 10-minute autopilot game.

**Gate 7 — Online private match:** true Photon authority for props, avatars, 5/10 timer, bot fill, synced animation/sound and reconnect, 2–4 humans; real two-client test after pair fixes.

**Gate 8 — Fresh sound production and polish:** new original adaptive score, source/license manifest, dozens of task/material SFX, Mom/bot fallback speech, spatial mixing, subtle VFX and polish and performant headset mix. Audio workflow may run in parallel with graphics when file ownership independent.

**Gate 9 — Full QA and release:** EditMode/PlayMode/automated solo/multiplayer; real headset if available, performance/cross-eye checks, accessibility, build Android APK with reproducible versioning and commit; developer/Play signing as configured, never invent store app ID. Create reports. Fix blockers where tools can reach and clearly list external-only blockers.

### Must deliver
- **Production Unity project** with modified source, scenes, prefabs, materials, scripts, task registry, animations, sounds, audio mixer, FX and menus.
- **Source art**: `.kra`/`.svg`, PNG sprites/atlases, Blender `.blend`, textures/UV, character rig files, skybox masters/rendered cubemap where used, audio sources/stems and legally valid records.
- **Playable Android build** in `Builds/Quest2_3_3S/` with true content included, verified SHA-256, build profile, log and version; do not overwrite working alpha without keeping backup.
- **Reference compliance proof** with side-by-side current/ref/new screens and concrete notes.
- **Architecture and content manifests**, updated risk register, exact 72 recipe state coverage, physics/lighting/voice/Photon acceptance matrix and known-issue list.
- **Testing evidence** distinguishing simulations, actual Unity runtime, real Quest 2/3 and multi-client tests.

### Automatic continue instruction
Proceed as far as permitted by current tools. Never finish by outputting only a list of planned next steps. Do not claim 'completed game' if an actual blocker prevents access to hardware/services. When a blocker occurs, pivot to independent implementable work, record the obstacle with required user action, and continue remaining gates. Maintain project integrity and honest acceptance status.

---

## 21 — STEP 02: EXACT UPLOADED MODEL PACKS, IN-PROJECT PACKAGE AUDIT AND ART-INTEGRATION RULES

**Actual supplied files and verified ZIP contents (source archive observation; Unity imports must still be performed locally):**

| Package delivered by owner | Observed content | Recommended SHHH! application | License / caution |
|---|---|---|---|
| `Ultimate House Interior Pack - June 2020-20261109T080125Z-1-001.zip` | **123 FBX + OBJ + BLEND** model variants, named household furnishing/architecture; `License.txt` | bathrooms, beds, cabinets, mirrors, sofas, plants, lights, kitchen/storage, missing large furniture | archive declares CC0 |
| `daycare-everyday-interiors-360-glb-v1.1.zip` | **360 GLB**, 360 `.json` descriptions, `ASSET_INDEX.csv`, `LICENSE.txt` | toys, room fixtures, small objects and possible moveable parts; use metadata to configure colliders and pivots | archive declares CC0 |
| `nashas_cozy_3d_kids_room_v1.0.zip` | ~34 FBX, ~35 GLB including model variants, texture PNG, `.blend`, `LICENSE.txt` | children's bedrooms, toy chest, blocks, plushes, books, desk and props | custom license allows commercial game use/modification; **not** generic CC0; obey restrictions on redistributing source files |
| `NewLuaStudios_StylizedFoodPack.unitypackage` | Unity Asset Store legacy `.unitypackage` / source content | packaged stylized food; stage import and verify actual meshes/texture/prefabs | do not assume commercial use permitted; inspect included license/Asset Store EULA and imported contents before shipping |
| `KayKit_Restaurant_Bits_1.0_FREE (1).zip` | FBX/OBJ/glTF food props including dirty dish state variants | prepare/cut/stir/cook, plate cleanup, matching food states and kitchen interaction | archive declares CC0 |
| `household_laundry_utility_v1.0.zip` | **20 FBX, 20 GLB** plus materials/previews | mop, bucket, spray bottle, broom, vacuum/laundry machine, hamper, ironing/cleaning and moving parts | archive declares CC0 |
| `Tiny_Treats_Charming_Kitchen_1.1_FREE.zip` | FBX/glTF/OBJ kitchen props, several color variants | cabinets, counters, food containers, appliances, sink, dining props | archive declares CC0 |
| `Classroom-VR-main(1).zip` | **legacy complete Unity 2020 Classroom-VR project** with Photon PUN2/XR/voice code | ONLY inspect as Step 04 technical reference; **not** a household 3D pack to import | third-party bundled content and Photon config; source project license does not cover every subasset |

The asset pack models are **starting assets**. Import only the formats/scenes needed; no duplicated FBX+GLB+OBJ all active. Build a source manifest including archive, license terms, original filename, source SHA, runtime prefab, visual adaptation, triangle/material/texture metrics, anchors, task IDs, collider choice, physics joints, room, audio/VFX profile, rig/animation requirements and QA state. License files remain with archived source but don't need to be exposed inside the runtime build. Do not share source files from restricted packs as standalone distributions.

**User's Unity Package Manager screenshots show these items in the library** (statuses vary, some show download/import icons): Genies Avatar SDK `3.8.6`; `Simple FX - Cartoon Particles`, `Bubbles VFX - URP`, `Free Toon Water`, `Stylized Water Effect Pack`, `Wind VFX - URP`, `Distortion VFX - URP`, `VFX - Impact and Hit - Light Version`, `Candle VFX - URP`, `Bubbles VFX`, `Bold VFX Pack DEMO`, `Invisible VFX - URP`, `Anime Star VFX - URP`, `Hologram VFX URP`, `VFX URP - Fire Package`, `Trails VFX - URP`, `Cartoon FX Remaster Free`, `DOTween (HOTween v2)`, `Photon Voice 2`, `Photon Fusion - Multiplayer SDK`, `Creative Characters FREE - Animated Pack`, `Stylized - Simple Hands`, `Free Pack - Cartoon Dog, Cat`, `Functional Washing Machines - Stylized Low Poly`, `Animals FREE - Animated Low Poly 3D Models`, `Low Poly Character Collection 3`, `Cute Low Poly Kitchen Pack`, `House Interior - Free`, `OpenCV plus Unity`, `Meta XR Platform SDK`, `Meta XR Interaction SDK`. **Verify package installation and licenses—My Assets is NOT equivalent to In Project.** Prioritize already imported compatible content before adding duplicates. No indiscriminate packages causing DLL/shader clashes.

### Art standard and toon conversion
Use existing handcrafted V5 work for comparison but raise quality across all rooms. Assets from disparate artists will clash unless united by systematic material/mesh treatment: rounded edges with meaningful bevels, carefully shaped sculptural silhouettes, brushed/painterly textures, hue and roughness variation, trim, hardware/detail, studio-to-night light coherence, UV integrity, consistent scale, visual density and characterful imperfection. Import each prop with sensible accurate handedness/orientation, pivot, clean transforms, unique identifiers, compression, collider and contact sensor; batch static art but preserve individually interactable dynamic pieces. The **entire connected home** must feel one art-directed world, not rooms made of seven unrelated packs. Original SHHH! branding, contemporary architecture and handcrafted style outrank external pack visuals.

### Physics prop conversion
For each chore source model, define hands' grips, one-/two-hand anchors, `Rigidbody` mass/center/friction, contact surface tags, gesture detector, dirty/clean/wet/filled/assembled/folded states, sabotage and recovery, stable IDs and authority serialization. Moving furniture hinges, doors, drawer pivots and appliance buttons must be rigged/segmented if original mesh is static. Physically important geometry can be original or remodeled in Blender. If pack model is too simplistic, replace/refine it—not keep placeholder and add excessive post effects. Every interactive prop gets correct visual/audio/VFX feedback and contextual Quest affordance.

### VFX pack audit and integration
Analyze water pack/foam, bubbles, cartoon particle, trails, wind, impact, candle/light, distortion and other selected effects; determine renderer pipeline support, shaders, depth/opaque texture dependencies, stereo single-pass compatibility, mobile shader target, alpha overdraw, particle count and draw calls. Produce original SHHH!-branded effect prefabs and reduce costs for standalone Quest 2; do not turn on expensive VFX Graph compute, full-screen distortion or volumetric effects by default. **No particle effect is authoritative simulation.** A water stream is a rendering of logical water flow; puddle/wet floor must be actual task state; a scrubbing sparkle must follow valid contact progress. Clip/soft-mask invalid shaders instead of filling screen with bright neon magic inconsistent with home.

### Buildings and environmental interaction
All connected rooms (living, dining/kitchen, laundry, bedrooms, bath, foyer, hall/stairs, Mom enclosed room, pet area) need coherent window locations, doors, stairs, navmesh/movement, Mom/bot routes, a map coordinate frame and real lighting. Exterior via windows must show a stylized continuous neighborhood with parallax/geometry, not a flat image wall. Adapt existing static geometry, fix cut-through walls and normal inversions. Use LOD, occlusion and lightmaps sensibly; retain decoration that communicates family life and gameplay affordances. No arbitrary magical outdoor theme copy.

---

## 22 — STEP 03: GENIES AVATAR SDK + MIXAMO + APPROVED CHARACTER PRODUCTION

### Do not compromise character identity
Approved **Pip, Nell, Rory, Tess** sibling turnaround sheet and **Mom** turnaround govern appearance. Pip: curly brown/auburn hair, goggles, red hoodie and red shoes; Nell: high dark hair/pink accent, round glasses, purple clothing/shoes; Rory: brown curls, backward blue cap, headphones, blue hoodie/shoes; Tess: reddish-orange ponytail, pink bow, yellow hoodie and pink shoes; Mom: mature expressive sculpted face, brown bun and loose curls, purple robe, cream apron with **floral pink flowers**, rear bow, gold earrings, plush pink slippers. Handcrafted stylized—not generic rounded primitive/flat-color doll. Mom previous `v001` was rejected as too angular, plastic/helmet hair and fixed expression. `v002` face was much improved but only **provisionally** approved before hair/full-body integration. Match front/profile/back/3-quarter and expression panel from images under both neutral and Unity nighttime lighting; approve technical art only based on model and runtime output.

**Sibling visible construction:** disconnected floating head, torso, hands and shoes, NO visible arms/legs; hidden logical humanoid rig may drive movement, animation and IK, with visible pieces attached/offset to control bones. Parent to body root but keep separated appearance, natural bobbing and expression. **Mom:** fully skinned humanoid, expressive eye/brow/mouth blend shapes or bones, lipsync support, gestures, walking/turning/bed exit. Pets Mochi and Biscuit need separate quadruped controllers (Mixamo auto-rigger is not a pet solution).

### Genies SDK exact next steps
User reports SDK installed/configured; screenshots show **Genies Avatar SDK 3.8.6** among Unity packages. In a **separate test scene and branch**, inspect actual installed Genies sample scenes/API. Create and customize a default avatar, modify face/hair/outfit/eye variants; save and reload local avatar definition, attach supported Animator Controller, test a locomotion/reaction and a blink/face animation if SDK supports. Audit the SDK license, runtime/account requirements, offline/online behavior, export rights, memory cost, Quest readiness, hand rig and integration with existing meta XR. Determine whether Genies can genuinely match **exact approved SHHH! characters**, not simply generate random cartoon children.

**Decision gate:** If Genies character fidelity diverges significantly, DO NOT replace approved characters. Use it only for modular preview/wardrobe technology, optional background NPCs or proof-of-concept; continue with custom Blender models and original modular cosmetics. Genies runtime avatars may have external account/service dependencies and assets/license limits; do not assume export to reusable `.blend`/FBX or unrestricted embedding is supported. Create a `KEEP/ADAPT/REJECT` report with screenshots, real import/animation tests and licensing caveats.

### Mixamo MCP and reusable animation
V1xel `mixamo-mcp` reportedly registered in Codex's `.codex/config.toml` and Python MCP dependency pinned 1.x; **not proof Claude can use it**. Detect Claude MCP, inspect `status`, `list_animations`, `select_animation`, `set_param`, `upload_character`, `download_animation`, `confirm_review`, `close_upload_modal` if actually available; read current repository for true names. Adobe sign-in is a human external action; do not copy cookies or embed login. Use legal Mixamo animation assets, verify export format and source terms. First Mom humanoid locomotion/gestures. For siblings, if directly uploaded disconnected parts fail, create connected **temporary proxy** solely for Mixamo animation acquisition/rigging, then retarget to hidden control skeleton and map motion to disconnected real visuals. Do not automatically apply a full-body skin to floating components.

**Animation library:** Mom: breathing/snore, yawning, wake, stand, walk, turn, stairs, investigate, listen, inspect, point, arms cross, accuse, think, warn, gasp, laugh/sigh, sleep; 10 distinct expression states. Siblings: distinct idle personality, walk/run/sneak, startled, focused, guilty/plead, high-five, celebrate, frustrated, talk/lipsync, grab/push/carry, scrub/mop/pour, fold, catch, repair, meaningful context IK. Mixamo gives generic base movements; task contact is runtime driven by target/constraints + custom clip layers. Bots operate actual tools; their hands must not visually penetrate props.

**The hands:** Existing Unity Asset Store **Stylized Simple Hands** already integrated in user workspace. For LOCAL VR users, use actual Meta XR tracking to drive hands, pinch/poke/grab and controller fallback; do not override live hand fingers with Mixamo. For REMOTE humans, replicate pose and interactions/locomotion in Photon; if avatar hands are visually character-specific, match surface style and skeleton while retaining original asset license. For BOTS, use authored hand poses and target IK. Document bone mapping, root units, scale, animator layers, head/hands/feet and costume attach anchors.

**Export:** `.blend` sources, suitable FBX, texture atlases/material profiles, facial blend shapes, Humanoid-compatible Avatar for Mom and hidden skeleton or Generic adapter for siblings as technically correct, signed-off expressions, animation source metadata, clip lists, retarget notes and Quest profiler screenshots. Reject T-pose/foot slide/broken eyes.

---

## 23 — STEP 04: LEGACY CLASSROOM-VR REFERENCE AND MODERN NETWORKED AVATARS

Inspect `Classroom-VR-main(1).zip` only as a research input. It is an older Unity **2020.3.27f1** classroom demo with Photon **PUN 2** and legacy XR, not directly source-compatible with Unity 6000.0.62f1, current URP/Meta XR or the current Photon implementation. Do **not** import its complete project, replace plugin versions, reuse its Photon App IDs, or expose credentials. Respect third-party model/SDK licenses; reproduce concepts in original modern code instead of copy-pasting fragile legacy scripts.

Specific source concepts to inspect: `NetworkManager.cs` (join/session flow), `PlatformManager.cs` (per-player role spawn), `FollowVRRig.cs` (local tracked head/hands to remote avatar targets), `Avatar.cs` (head/hand/body animation rig target transforms), `HandAnimator.cs` (gesture mapping; old controller-based not reliable Quest hand recognition), `XRGrabbablePun.cs` and offset variant (ownership transfer/attachment ideas), `UI_InteractionController.cs` (near versus far input), and `Marker.cs` + `Whiteboard.cs` (tip-on-surface raycast → UV painting concept). Reuse **the design pattern**, not CPU 2048² texture painting and frequent buffered Photon RPCs—replace with compact surface state maps/patch sync. Do not use teacher/student role labels or 20-player room default.

### SHHH! modern VR avatar pipeline
Local player: authoritative XR origin/camera head/left and right hands, local controller/hand tracking, game locomotion, own body visible if appropriate and no duplicate first-person face clipping. Remote player: one Photon network-owned player seat with replicated root/hips, head target, hands with constrained fingers/poses, feet positions/locomotion phase, eye/head look target and discrete animation state; interpolate movement, retarget hidden skeleton for floating-body visuals. Third-person hands exactly grip props in shared world; local tracked fingers not driven by network or Mixamo. Bots: local authoritative planner + action sequencer provides same networked avatar target parameters as humans. Mom: NPC owned by one authority, replicated root, animation state, head/gaze, voice and relevant interaction.

**Hand-object synchronization:** one writer owns/controls an active object at a time; pickup request reaches authority, validates proximity and joint, transfers safely when applicable; release gives bounded velocity and authoritative collision outcome. Finger animation and visuals can be local/remote optimized; do not send huge full skeleton streams at 90Hz. Avoid hand-to-object snapping, flipping left/right pose and thumb/index same-side bug noted in books. Network audio event must not be multiplied at every client.

**Rig mapping acceptance:** each character appears to peer at correct scale, with floating body pieces coherently animating, tracked head/hands matching own actions from all angles, independent correct avatar selection and cosmetics, no remote T-pose, correct grab pose on objects, safe reconnect, graceful tracking loss, correct bot control handoff and stable 5/10 timer. Run a true two-client test; old demo cannot prove compatibility.

---

## 24 — STEP 05: HIGH-FIDELITY PHYSICAL INTERACTION ENGINE — 26 PRIMITIVES

The existing interaction approach is **too shallow**: do not retain grab-and-drop completion as the dominant mechanics. Every relevant chore must ask the player to *perform* its physical action. Build reusable ScriptableObject-driven action detectors with actual `Meta XR -> real geometry/contact/motion -> authority -> prop state and Mom noise/evidence -> VFX/audio/UI -> test`. Use narrow/forgiving tolerances and purposeful animation assistance, not fake progress percentages. All 26 primitives below are **required reusable capabilities**, with one production-grade demo and at least one substantial recipe binding each.

### Cleaning and liquids
1. **Scrubbing:** sponge/brush hand pose, contact face collider, valid tangential velocity, 8×8 or appropriate coverage grid on UV/object plane, proper water/soap preconditions, clean mask reveal along contact path, foam, scrub-loop audio, authoritative completion. Fake in-air motion must never clean.
2. **Wiping:** cloth or paper contact with windows/counter/mirror, directional stroke and surface overlap; persistent dirt/wetness map, spraying prerequisite where relevant, visible wipe paths and residue, cloth friction. Sync changed cells/state not high-res textures.
3. **Sweeping:** long broom two-hand grip and bristle collision/overlap; apply bounded forces to crumb clusters, chips/blocks and dust; collect into dustpan/trash, directional cleaning progress; local dust effects. No hundreds of dynamic crumbs; represent tiny debris as clusters.
4. **Pouring:** liquid reservoir, orientation/spout threshold, fill rate and target receiver bounds; rising water level, lightweight ribbon stream with splash feedback, spill volume when missed, wet zones/cleanup task. Authoritative volumes and causality, not just particle emission.
5. **Squeezing:** grip closure/anchor compression (not fictitious force-sensing), normalized deformation of sponge/toy, controlled soap droplets or squeak, trigger/release thresholds and voice/impact consequence.
6. **Twisting:** constrained rotation with plausible detents, faucet valve flow mapped to angular position, cap screw rotation and slight axial change, water/SFX and authoritatively clamped end stops.
7. **Pulling:** one-axis drawers/handles, stiff drawers, lids and soft cables with constrained resistive interaction; actual motion limits, obstruction, spring/latch state and pull noise.
8. **Pushing:** actual hand/rig contact proxies and bounded force/velocity on cart/bin/light stool; rolling friction and collision; cannot move massive fixtures like featherweight props.
9. **Pumping:** soap dispenser top or spray trigger press/release cycle using poke/grab input; exact dose, foam/mist, cooldown, possible overflow and audible pump; no visual-only squeeze.
10. **Shaking:** filtered alternating motion of held pillows/boxes, dust scatter, real mixing progress and cooldown, cap/contents spill if unsafe; ignore tracking jitter.

### Food, toy and fabric
11. **Stirring:** spoon/whisk tip under bowl lip, track arcs around bowl center, progressive mixture shader state, changing swirl/viscosity visuals and utensil clinks; free wrist spin outside bowl does not qualify.
12. **Slicing:** cartoon safe knife with hand grip, predefined cutting planes and actual blade-crossing direction; ingredient becomes separate cut meshes/colliders and cooking state; no sharp-object realistic injury content.
13. **Rolling:** rolling-pin contact and physically moved cylinder over dough, changes dough thickness across sections; toy cars/balls respond to tuned rigidbody/wheels with realistic bounded inertia and material-based sound.
14. **Folding:** paired fabric anchors, soft constrained skin/bone deformation and bend targets, corner alignment and 2–4 real fold steps for shirts/blankets/towels; not instantaneous mesh swap simply on dropping into hamper. Avoid expensive unrestricted cloth unless benchmarked on Quest.
15. **Stretching:** two-hand endpoints, capped elastic response, spline/skinned mesh, fitted sheets or elastic toy; make visible tautness with controlled spring-release and no head-whipping.
16. **Stacking/balancing:** rigidbody contact, tuned center of mass, friction, temporary wobble, valid support and stabilization time; near-target alignment assist may be subtle but not replace actual stacking.
17. **Catching:** falling object swept contact/proximity plus genuine grab action and authority adjudication; catch/decelerate, rescue credit, avoid double grab and impact fake positive.
18. **Throwing/tossing:** Meta grab release vector and angular momentum with safe bounded velocity, laundry into basket, toy to chest, trash bin and impacts; actual settle in receiving volume, not single trigger entry.
19. **Vacuuming:** hand-guided nozzle across floor/carpet, overlap/suction zone, material-specific dirt coverage, clogged nozzle, motor SFX and dust effects; noise attracts Mom logically.
20. **Sliding:** tray/rail/book guide axis, friction and valid insert depth, audible contact/latch, proper constrained movement, no Transform teleport through walls.
21. **Hinging:** door/washer door/cabinet lid via bounded hinge joint/rotary transformer, drag, close velocity, creak, quiet latch vs slam, optional spring-return trap.
22. **Connecting:** align physical mating points with position/orientation tolerance, attach parts to sockets/joints and play tactile click; matches toy assembly, vacuum attachments and repairs.
23. **Plug/unplug:** oversized safe cartoon connector, insertion axis/orientation/stop, appliance power state and optional quiet click; not actual electrical hazard simulation.
24. **Pendulum:** a hanging lamp/decoration around fixed pivot with bounded swing, accidental bump and manual steadying/damping, real sound and Mom events when collision occurs.
25. **Spring compression:** bounded push on switch/doorstop/springy toy, squash-and-stretch animation and capped release impulse, responsive mechanical audio; no instability/excess stored energy.
26. **Two-handed wringing:** wet towel held at two ends, opposing twist/rotation and tension; water drips into designated receiver, wetness reduces, finite strokes, cloth skin bones; spill if bucket absent.

### Required flagship multi-step tasks
**Actual dishwashing:** turn faucet → grab dirty plate and sponge → scrub actual spots using contact → rinse with visible flow/foam runoff → put physically stable plate in rack → close faucet. A loose/sabotaged rack can tip; catch plate or repair brace. `KT01` long-run plate fall must be fixed without silencing real accidental impacts.

**Mopping/spraying:** fill/prepare water, wet mop, sweep valid floor paths across persistent dirt regions, deal with dirty bucket, genuine foam/footprints/wet trail and wipe/spray contingent states. No forced VR camera sliding on a wet surface.

**Tray balance:** two-hand support, rigidbody cups and plate with mass/friction tuning, walk through connected rooms, catch falling props, set down gently and preserve fair root cause after sabotage.

**Toy chaos:** rolling toy cars, chain-reaction stack, squeeze toy, pet pursuit, gather into chest, loss/repair with material-aware impact audio and limited Quest physics body count.

**Laundry:** grip cloth corners, align/smooth/fold via skinned mesh, sort pairs, wring towel, place in basket and operate washer doors/buttons with real constraints; undo/repair by physically messing arrangement.

### 12 required household comedy incidents
- Falling plate dominoes from genuine rack instability; catch, repair, causal blame.
- Runaway rolling/bouncing ball heading downstairs; intercept before bedroom.
- Wobbly stool/table with props; steady or reposition before collision.
- Squeaky floorboard with generated logical VR footsteps (Quest does NOT track feet).
- Vacuum hose/cable snags around furniture: bounded coarse collider spline, release tension; no long heavy rope physics.
- Overfilled bucket and sloshing/spill from movement; correct fill state and mop response.
- Rotating desk chair with angular damping; stabilize before impact.
- Spring-loaded drawer pops after unlatching; catch and quietly close.
- Swinging lamp rattles; stop pendulum without hitting nearby prop.
- Biscuit/Mochi chase a rolling squeaky toy, credited to pet unless player intentionally provoked with evidence.
- Unstable book tower begins leaning; brace/catch/rebuild.
- Soap overflow after too many real pump doses, new cleanup objective.

### Implementation and anti-fake rules
Use native Meta SDK grabbables/physical transformers, accurate collider layers, two-hand controls, articulated finger poses and Unity rigidbody/joint physics where physical action matters. Use `PhysicsMaterial`, continuous collision detection selectively, interpolated transforms, capped motion forces and stable fixed timestep. Custom motions need actual consistent hand contact and world response; animations/lights do not set truth. Prefer compact material maps/cleaning cell masks, local surface shader transitions and logical reservoir/flow simulation, not CPU/GPU-heavy full water solver. For fabric, use skinned/constraint-based deformation with purposeful folding rather than unrestricted full-scene cloth. Simulations must *feel* physical and recover realistically.

**Quality and multiplayer:** 26 single-primitives and 12 incident demos each have independent normal/interrupted/sabotaged/recovered cases. Single authority owns rigidbody interactions and task truth, with client-predicted local feel, validated grab handoff, compact snapshots and event propagation. VFX, sound and clothing deformation are reproduced locally from state; do not stream particles or cloth vertices. Each human and bot uses same completion requirements. Every impact has proper material and cause record. Avoid overwhelming Mom with meaningless scratch sounds while still honoring actual loud disturbances.

---

## 25 — STEP 01 EXTENSION: REAL NIGHT SKYBOX, NEIGHBORHOOD AND URP LOOK DEVELOPMENT

User explicitly rejects current windows containing a flat, simplistic night picture. The visual target is **a cozy stylized living neighborhood at night** with a genuine sense of environment beyond glass and believable continuity between interior nighttime mood and exterior colors. Make an original panoramic or six-face skybox source (using Krita painted base, Inkscape silhouettes as needed, Blender/Unity native cubemap pipeline), moon, gradient sky, stylized stars and layered soft clouds. Ensure proper horizon and seamless panoramic edges, not a repeating random texture. If camera cannot access outdoor area, still include enough actual low-poly houses, roofs, trees, far silhouettes, lamp posts and appropriately placed warm glowing windows to provide parallax through multiple windows. Do not place a gigantic near-vertical cartoon illustration directly outside every window.

Design moonlight key direction matching visible moon where plausible; warm practical amber lamps, cool blue/violet bounced night, tasteful hue separation and hand-painted PBR. Static lightmaps/probes, carefully budgeted shadow casting and reflection probes appropriate to Quest URP. Night skybox material imported into Unity Environment, with actual visible correct camera clear settings, exposure/tone mapping, window glass/opaque budget and matching distant fog. Configure LOD/culling of exterior while keeping significant reflections. Avoid light leaks, detached balcony architecture, overbright blown highlights, excessive blue face cast and screenspace bloom/refraction. Screenshots from both living room and kitchen windows must show a consistent exterior, and Mom's skin still look warm and readable.

Create three comparison scenes/camera shots: exterior from living room, kitchen, Mom's bedroom; moving viewpoint must demonstrate plausible exterior parallax. Provide editable skybox source file and built Unity material/shader, actual night lighting profile, probe/lightmap and device cost. Use Shader Graph and particles where appropriate; no need for real-time volumetric clouds.

---


## FINAL EXECUTION COMMAND — BEGIN WORK, NOT A PLAN

Claude, read this complete directive, the **actual current Unity SHHH repository**, all supplied visual references, and the seven prop archives plus Classroom-VR reference. Establish an honest baseline of the current `SHHH_alpha_0.6.2_c11` work. Preserve working features, repair its known regressions, and execute the six latest user directives **as production**: build a one-match 5/10-minute game; professional entirely new Krita/Inkscape cartoon UI art and personal house map; coherent handcrafted imported props and connected rooms; redesigned reference-accurate Genies/Blender/Mixamo-ready animated characters and multiplayer rigs; true multi-step physical water/spray/cloth/cleaning/physics mechanics with VFX; novel original spatial audio and Mom/bot voice fallback; genuine Photon competitive co-op/sabotage; safe optional LiveKit. Complete the 72 recipe catalog using actual physical action predicates, correct causal blame, and real networked state. Produce original source art, Unity prefabs, revised gameplay code, truthful tests and an Android APK wherever hardware/tools permit.

Do not interrupt the work for routine choices, do not discard working gameplay, do not import incompatible packages indiscriminately, do not create a new Unity project or a generic avatar in place of approved SHHH! characters, do not fake service access, do not silently resurrect three elimination rounds, and do not declare headset/full multiplayer verification without real evidence. If external credentials/device access cannot be obtained, finish all independent work and report the exact unresolved gate. Keep meaningful progress check-pointed in source files and evidence rather than relying on a single model context window.
