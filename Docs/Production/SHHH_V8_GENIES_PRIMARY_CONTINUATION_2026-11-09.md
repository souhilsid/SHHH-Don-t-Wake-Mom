# SHHH! — V8 CONTINUATION: GENIES-FIRST AVATARS + FULL PHYSICAL GAMEPLAY
## AUTHORITATIVE ADDENDUM AND CLAUDE EXECUTION PROMPT — 09 November 2026

> **USER'S NEWEST, EXPLICIT DESIGN DECISION:** Genies Avatar SDK is SHHH!'s PRIMARY, DIRECT, IN-GAME avatar creation, customization, wardrobe, avatar persistence, and avatar-rendering system. **Both anatomically connected full-body avatars and stylized disconnected/floating-body avatars are acceptable.** Do not reject Genies because it doesn't replicate the old disconnected-body silhouettes. Prioritize functional Genies-created playable avatars, fluid animation, VR hand interaction, and beautiful immersive customization over the previous mandatory floating-body construction.

**PRECEDENCE:** This continuation supersedes ONLY conflicting avatar/character instructions in `SHHH_CLAUDE_ONE_SHOT_COMPLETE_MASTER_PROMPT_2026-11-09.md`. Every other original requirement, including the six approved steps, the canonical 72 recipes, 26 physical primitives, 12 comedy incidents, UI/audio overhaul, coherent connected house, 5/10-minute continuous match, competitive sabotage, Mom intelligence, Photon and QA, remains active. Read the whole original master prompt and the live repository as the source for their complete definitions; do not reduce or replace them with the abbreviated summaries below.

**WORKING PROJECT:** `C:\Users\aisci\Downloads\SHHH Game\SHHH`

**TARGET:** Existing Unity 6 / URP project, Meta Quest 3 primary, with Quest 2/3S compatibility where feasible. **Do not create a new Unity project or delete working code.** Follow the already-existing history, checkpoints, and document conventions. Inspect installed versions rather than trusting a Package Manager screenshot.

---

## EXECUTION START: RESUME AT THE REAL USAGE-LIMIT CUTOFF

This is a continuation of the SAME Claude conversation, not a new brief. You stopped because of the five-hour usage cap. Before stopping you reported: continuous house/match work; 156 passing core tests including eight continuous-match tests; a Unity autopilot crash in job clipboard room-index lookup followed by a proposed house-position fix; compile checks; new `UiKit`, wrist HUD, `HouseMapView`, results podium, nighttime exterior and shaders; new original music/SFX/Mom voice pipelines; patch-local scrubbing in progress; laundry props and room-furnishing work; and an Android APK build. **These are progress claims to VERIFY, not confirmed final functionality.** The last visible execution involved inspecting `assembled_laundry.png`, `ldr.jpg`, placing a sink/cupboard/cart along laundry walls, and running commands when the usage limit interrupted the session.

### First actions (do them, don't merely narrate them)
1. Read the latest original full master document, all current `Docs/` production handoffs, blocker/risk docs, asset licenses, architecture, scene/room map, QA evidence, cloud commits and PC sync patches. Inspect the code rather than assuming a task list accurately reflects it. Recover pending sync archives and identify any unsaved or partially applied changes.
2. Check whether the interrupted Unity APK build finished, failed, or remained active; inspect its REAL build output, Player.log/Editor.log and APK timestamp, version and SHA-256. Preserve a known-good APK and a source snapshot before modifying more. Do not call APK generation a successful headset test.
3. Confirm the state of room-index clipboard crash, `KT01` plate crash, intermittent long-run `LD04`, `LD08`, `BR08`, `HW03`, the 61 previously reachable versus 11 blocked recipes, and whether any of these issues were already resolved. Do not change stable recipe IDs.
4. Finish and validate patch-local scrubbing, the current laundry room's missing furniture/assets, door and route connectivity, and in-progress audio/VFX wiring, or record precisely what remains. Do not waste the session re-rendering completed icons or rebuilding the same audio stems.
5. Check Unity compilation and representative automated tests. Check the latest project against the master, then proceed **immediately** to the Genies avatar/wardrobe work below. Use a short factual progress checkpoint, not a planning-only response.
6. The earlier request to read `Classroom-VR-main.zip` outside the connected SHHH folder timed out. Treat it as **optional legacy reference**, not a blocker. If the file is not authorized or accessible, record it, skip it, and continue implementing with current Meta XR and Photon SDKs.

### Safety/continuity
- Do not interfere with an ongoing Unity build or editor import by launching conflicting processes.
- No destructive Unity package upgrades; no silent replacement of Photon, Meta XR, the original brand assets, or unrelated working systems.
- Windows PC may not have Git while cloud mirror does. Preserve source and GUIDs and establish a consistent checkpoint method after inspecting both environments.
- Never claim simulated hand, multiplayer or Mom tests are real headset/multi-client tests. No fictitious log, credentials, animation exports, connection success, or finished APK.

---

## PHASE 1: GENIES IS THE MAIN CHARACTER CREATOR, NOT A PROTOTYPE

**Nonnegotiable owner override:** The original master says Genies must be rejected as the main character system if it doesn't match exact Pip/Nell/Rory/Tess disconnected-body references. **REVOKE THIS DECISION GATE.** The original instruction to enforce separate visible head, torso, hands and shoes with no arms/legs is also **REVOKED FOR PLAYER AVATARS**. Connected Genies full-body humanoids are fully approved. A detached/floating style is OPTIONAL ONLY if it is robust, visually preferable and doesn't sabotage SDK functionality, animations, outfit meshes, IK, multiplayer or Quest FPS. Do not spend time mutilating functional Genies skeletons to preserve that prior silhouette.

### Roles vs appearances
- Pip, Nell, Rory and Tess remain the four gameplay **seats/roles/personality archetypes**, especially for bot personalities, score, voice, task ownership and progression. The original outfits/colors and reference art can inform four **starter presets** but no longer constrain a human player's identity.
- The player can create a **new personalized Genie avatar** from the actual Genies SDK's supported editor functions, or customize a starter preset. Their chosen appearance must truly render in the game, in the mirror, in matchmaking/lobby and to remote players.
- Mom remains a separate, reference-accurate, conventional full-body NPC with bun, purple robe and floral apron. Existing or future Mom rigging may use a compatible Genies-based approach if technically supported, but DO NOT replace Mom with a random generic Genies child/avatar.
- Pets Mochi and Biscuit remain animated pets, not Genies humanoid avatars.

### SDK audit and real integration
1. Confirm whether **Genies Avatar SDK 3.8.6** is actually present under `Packages/`/`Assets/` and usable in Unity, not merely listed in My Assets. Check Unity 6, URP, Meta XR, Android and installed dependency compatibility. Inspect current sample scenes, bootstrapping, login and user-specific local/global avatar definitions.
2. Start with a **real separate SDK integration/diagnostic scene** and make a complete avatar load, customization, save, destroy/reload, animation and Android-build smoke test. Import and inspect official *Creating Custom Editor* sample if present. Use the official current `Genies.Sdk` API, not made-up methods or obsolete v3.8.3 UI packages.
3. Use **Genies managed avatar state and definition as the character source of truth**. Never create a fake UI that only swaps four unrelated Blender prefabs. Do not recreate an entire unconnected custom character system while labeling it Genies.
4. Prefer Genies SDK's existing inventory: default and legitimately user-owned wearables, hairstyles, facial/body edit controls, colors and accessories. Equip/unequip through real SDK calls and persist actual avatar definitions. External licensed 3D clothing assets may be incorporated **only through a verified compatible Genies wearable/custom-asset pipeline**; unsupported meshes should not be displayed as selectable but broken items.
5. Check supported avatar creation, login, anonymous/demo modes, account dependence, asset caching and network requirements against the **installed version**, not hearsay. Never assert that global avatar creation or editing works offline if authentication/assets require a service. Provide a truthful usable fallback to a saved local/default avatar where the SDK permits, without substituting a non-Genies avatar system.
6. Check Genies EULA, user-generated wearable upload policy, runtime redistribution, commercial rights, child-safety implications and storage. Do not patch proprietary SDK internals or extract SDK meshes against license.

### Official references (check against installed version)
- https://docs.genies.com/docs/sdk-unity/sdk-avatar/frameworks/custom-editor/
- https://docs.genies.com/docs/sdk-unity/sdk-avatar/sample-scenes/creating-editor/
- https://docs.genies.com/docs/sdk-unity/sdk-avatar/frameworks/load-avatar/
- https://docs.genies.com/docs/sdk-unity/sdk-avatar/tools/vr-example/
- https://docs.genies.com/docs/sdk-unity/sdk-avatar/release-notes/sdk-avatar-3-8-6/
- https://github.com/geniesinc/genies-sdk-vr-example

**SDK methods to verify and leverage when present in the exact installed API:** `AvatarSdk.GetDefaultWearablesByCategoryAsync`, `AvatarSdk.GetUserWearablesByCategoryAsync`, `AvatarSdk.EquipWearableAsync`, `AvatarSdk.UnEquipWearableAsync`, `AvatarSdk.GetDefaultHairAssets`, `AvatarSdk.EquipHairAsync`, `AvatarSdk.SetColorAsync`, facial/body-feature editing APIs, `ManagedAvatar.GetDefinition`, `AvatarSdk.SaveAvatarDefinitionLocallyAsync`, `AvatarSdk.SaveUserAvatarDefinitionAsync`, `AvatarSdk.LoadAvatarByDefinitionAsync` and `AvatarSdk.LoadFromLocalAvatarDefinitionAsync`. Confirm actual overloads/namespaces and whether save/login is supported in the tested account mode. The v3.8.6 release notes specifically mention fixes for default avatar definition saving and the Avatar Editor sample.

**Important distinction:** The official Genies Quest VR Example provides headset retargeting, login and VR presentation, but **does not supply a complete VR avatar customization editor**. SHHH!'s custom world-space wardrobe UI must be driven by the Genies customization framework. Do not paste the old legacy mobile/editor UI into VR and claim the wardrobe is done.

### In-game character creator + physical wardrobe
Create a beautiful, **interactive in-world dressing room or wardrobe stage** in the existing SHHH! Family Stage art style, not a separate generic application.

**End-to-end journey:**
`Family Stage > Customize / Create Avatar > Genies avatar loaded or genuinely created > appearance categories > physical wardrobe and full-length mirror > outfit try-on > live 360-degree preview > save avatar definition > return to stage > Solo or Friends > spawn with that avatar > persist through results/rematch/relaunch`.

**Required player features, using actual installed SDK capabilities:**
- Create or edit an avatar; choose supported body type/features and facial parameters.
- Change supported hair styles, hair/eye/skin color, facial options, eyewear, headwear, tops, jackets/hoodies, bottoms, footwear and accessories.
- Browse assets by categories derived from the actual Genies inventory; items unavailable in the current inventory must be absent or clearly locked, never fake.
- Equip, unequip, preview, undo/cancel if feasible, reset to saved, and **Save** using the real avatar definition.
- Style presets based on the four original siblings, plus independent user-created appearance.
- Optionally save multiple outfits or avatar slots if SDK/data architecture supports them; at minimum persistent one active customized avatar.
- Use spatial, comfortable, cartoon-quality interaction with closet doors, racks, swatches, floating-but-grounded category cards, large grab/poke/ray targets, mirror, turntable and try-on feedback.
- Display avatar full-body in actual Unity scene under SHHH!'s night/wardrobe lighting with real cosmetics, body scaling, skinning and facial expression. A dressing mirror can use a controlled second-camera/render-texture solution or efficient alternative with Quest profiling; do not allow first-person face clipping.
- Support readable prompts, category names, load/error states, a working Save button, and safe returning to the previous menu.
- No payment/purchase screens unless a real authorized Genies entitlement flow is integrated. No fake buy/unlock buttons.

**Identity/persistence:** Store avatar-definition version/profile ID, appearance and wearable asset IDs by a stable local identity. Handle SDK caching, load errors and reconnection, and only transmit efficient appearance descriptors/valid asset IDs to Photon. Remote clients instantiate the same correct player avatar, with a documented cache/temporary visual fallback if assets aren't yet loaded. Do not duplicate cosmetic assets on every frame or sync full meshes/textures. A player who disconnects/rejoins should regain the same appearance and gameplay seat.

### Animation, full-body/IK, hands and character performance
- Prefer a **complete connected Genies character**, using its native skeleton and supported animation/retarget pipeline. Disconnected style is no longer required.
- Audit default Genies animations (including idle/wave), provided clips, SDK animation/retargeting, and the Meta Quest VR example. Build working Animator Controllers and IK/retarget bridges for avatar, tracked XR head/hands, locomotion, turns, crouch/sneak, idle, facial reactions, working, carrying, cleaning, folding, mopping, dishwashing, helping, sabotage, warning, panic, Mom confrontation, victory and defeat.
- **Mixamo is optional** for missing compatible base animations; verify whether `mixamo-mcp` is actually callable in Claude, not only Codex config. Use existing licensed FBX/Unity animations first. Never force a fragile Mixamo re-rig if native Genies motion works better. Physical task-specific hand contact and grip correction must be driven by live interactions + IK rather than unconnected canned clips.
- Local user: XR tracking has authority over hands and camera; hide or fade first-person head/face as appropriate. Align actual Genies visual hands or existing licensed stylized tracked hands to correct object grips; ensure left/right and thumb/index opposition are correct. Real Quest hands/controllers must grab and manipulate objects. Never let a Genies locomotion clip override tracked hands.
- Remote players: Photon pose replication + correct avatar appearance, head/hands/upper-body pose, locomotion phase and action states; no T-pose, disjointed arm stretch, sliding feet or duplicated camera.
- Bots: instantiate genuine animated compatible Genies characters using assigned role appearance; they must show proper work contact, movements, gestures, and reactions, with the same truth/authority rules as humans.
- Quest performance: cache wearable/anim assets, limit simultaneous costly skinned meshes/blendshapes, use appropriate LOD and lightweight cloth representations. Measure realistic four-avatar cost; avoid expensive constant mirror rendering.

**Genies Phase acceptance:** Starting on a clean launch, a player can use SHHH!'s **real Genies-backed** creator, customize appearance/clothes from real wearable inventory, save, exit and reload, enter a game with the customized animated avatar, manipulate a prop with accurately tracked hands, and be represented correctly in a two-client test (if available). Record each test's real environment; label unsupported steps honestly.

**If blocked by Genies auth/service/licensing:** Finish all SDK-independent development and robust integrations, document the exact external requirement and logs, leave an honest Genies-backed default/local-definition path where supported, and **do not quietly switch back to custom Blender avatars as the primary system**.

---

## PHASE 2: IMMEDIATELY PRIORITIZE TRUE PHYSICAL HOUSEHOLD GAMEPLAY

Once the Genies editor/rig is functional enough to spawn and interact, allocate the **largest share of engineering effort** to gameplay. Do not spend the entire allowance polishing thumbnails, avatar UI or disconnected anatomy. The original master Section **24 (Step 05)** is still authoritative and requires **all 26 reusable primitives**: scrubbing, wiping, sweeping, pouring, squeezing, twisting, pulling, pushing, pumping, shaking, stirring, slicing (stylized/safe), rolling, folding, stretching, stacking/balancing, catching, throwing/tossing, vacuuming, sliding, hinging, connecting, plugging/unplugging (safe cartoon), pendulum, spring compression and two-handed wringing.

### Must-build interaction substrate
`Meta XR tracked hands/controllers > physically valid contact/pose/tool motion > constrained physics/consumable state > authoritative task predicate and Mom noise/evidence > local Quest-optimized shader/particle/audio/haptic feedback > deterministic QA`.

- Reliable grab/release, correct physical grip points, two-hand interaction, correct thumb/index opposition, articulated fingers or meaningful controller grip poses, tracking loss recovery; hand interaction must never disappear after scene transitions.
- Actual Rigidbody/collider/joint constraints, friction/mass tuning, object ownership and fall/catch/sabotage detection. Minimize tunneling, jitter, teleporting, fake throw impulses and objects piercing props/walls.
- Stable surface-local UV/coverage masks for dirt/grease/dust; contact and tangential motion remove **only touched areas**, with soap/wetness prerequisites, foam/drips and spatial audio. Finish the previously started patch-local scrubbing rather than replacing it.
- Visually convincing **Quest-friendly** faucets, streams, liquid reservoirs, buckets, pour/splash/spill/foam/wetness; physics-relevant simplified fluid state, never merely looping particle visuals.
- Cloth grips/folds/wring using skinned mesh bones and constraints rather than unrealistic instantaneous swaps or expensive unrestricted cloth solvers.
- Real laundry room with washer/dryer, baskets, actual garment props, hinged doors, machine state, folding/hanging and meaningful sabotage-repair.
- Real kitchen dishwashing sequence: faucet, sponge, soap, actual scrubbing, rinse, stable dish rack, finish/turn off faucet; reproduce/fix late `KT01` plate issue.
- Actual mopping/sweeping/vacuuming, wiping/spraying, pouring, sorting and safe pretend-cooking operations.
- Original toon URP shaders, efficiently pooled Shuriken particles, physically contextual water/foam/dust/impact effects, audio and controller haptics. VFX MUST react to state; never count cosmetic particle playback as task completion.
- Material-dependent impact sound/noise with source location, causal actor, sabotager vs innocent victim, witnesses and Mom's evidence ledger.
- Real prop meshes from licensed provided packs and the existing project, remodeled/retextured into coherent SHHH! storybook toon; no primitive gray cubes for final appliances.

### Flagship vertical slices — prove these before bulk-upgrading recipes
1. **Dishwashing (`KT01`)**: connected room, working physical plates/rack, faucet, sponge, soap, precise scrubbing, rinse, rack settle, SFX, foam/water VFX, successful score, valid rack sabotage/repair, noise attribution, bot capability and network replication.
2. **Laundry (`LD04` and linked recipes)**: washer and real door, garments with Genies-avatar-independent fabric props, stateful load/detergent/close/start/finish/unload, folding/wringing, fluid/sound/animation response, physical sabotage and recovery.
3. **Surface/floor cleaning**: contact-local stains, true mop/sweep/vacuum tool handling, droplets/foam/dust/puddles, physically changed task state, pet/sabotage interference.
4. **Shared prop incident**: tray balance/rolling toy/squeaky item, catch and repair; one authority controls physics; real Mom investigates the resulting sound and considers evidence.

Do not stop after four tasks. Scale the reusable production-grade primitives across **all 72 canonical task IDs across eight areas**, preserving every ID and the original source's required minimum action/sabotage/repair. Resolve the missing 11 tasks by importing/building room-sized furniture and integrating them into the single connected house. Maintain independent test coverage per recipe plus long-run mixed-task coverage and physical scene/camera evidence. The original's **12 household comedy incidents** also remain mandatory.

### Fun, pacing and fair play
A mechanic must be tangible, satisfying, readable, fast enough for a 5- or 10-minute match, and recoverable when interrupted. Player shouldn't have to mimic dangerous real movements or reach outside comfort range. Don't overengineer scientific water/cloth/softbody solvers that make the Quest unplayable. No infinite sabotage-repair farming. A bot cannot score a chore without legal logical/physical operations; no cheating through direct task-state flags.

---

## PHASE 3: COMPLETE THE ORIGINAL REMAINING PRODUCTION GATES

All non-avatar content in the 09 November master remains in force, including any gate already started. Work on them as integrated game systems, not isolated demos:

**Continuous match:** one authoritative 5:00/10:00 uninterrupted clock, whole explorable connected home, all four siblings active, personal house map/HUD, player-specific task assignment, valid rewards, no 4→3→2→1 elimination. Correct score/results podium, no intermediate rounds; maintain timer during Mom's typical warnings/confrontation.

**Photon + avatar/props:** inspect actual installed Photon stack, functional private create/join/invite, 2–4 humans plus bot-fill, valid avatar appearance and pose, synchronized grab ownership, objects, tasks, noise, sabotage, Mom, clock and result. Test two truly independent clients; no fake Play with Friends. Network compact state/event IDs rather than full textures/particle streams. Handle reconnection/bot takeover without duplicate avatars or score.

**Bots:** physically embodied distinct sibling strategy, working NavMesh routes, object/tool handling, task state, help/sabotage/repair, visible IK action animations, fair perception/witness, no invisible task completion. Must execute same world rules as human and use real avatars/props.

**Mom:** approved recognizable full-body mother in enclosed bedroom; bounded hearing/vision/navigation, suspicion, warning, walking to source, actual evidence inspection, witnessed-memory/WorldTruth separate from MomBelief, short playable interrogation with hand-based decisions and optional voice, rule-grounded verdict. Only one typical full interrogation in continuous match; no arbitrary teleporting or omniscience. Offline VO/dialogue and gameplay must work without LiveKit. Real voice service only if authorized/connected and secure.

**Sabotage, pets, alliances:** 80/20 household/planted safe prank focus from original, proper root-cause attribution, physical recovery and counterplay, pets Mochi/Biscuit cause traceable events, temporary help and betrayals, score anti-exploits.

**Art, UI and audio:** continue existing full art overhaul and original music/SFX bank, not the rejected legacy assets. Proper Family Stage, aesthetically coherent Genies wardrobe, usable non-flat world-space UI, night neighborhood/skybox, physical house map and results podium. New musical layers and material-aware sounds play contextually with adaptive Mom suspicion and per-action feedback. Avoid inconsistent low-poly asset-pack collages.

**QA/Quest:** compile, automated physics/action tests, long-run 5/10-minute solo, two-client networking, media captures, actual APK build, Unity logs, checkpoint docs. Test native Quest only where a device is really accessible and permitted. Profile realistic four skinned avatars + house + fluid/particles + pets + Photon and audio; aim for comfortable sustained VR framerate (72 FPS or device-supported tested target), without inventing performance results.

---

## PRODUCTION GATE ORDER FOR THIS CONTINUATION

**Gate A — Recovery:** Save everything and finish the interrupted Unity/APK/laundry/scrubbing work. Establish tested baseline and source/asset checkpoint.

**Gate B — GENIES PRIMARY (FIRST NEW BIG GATE):** Real SDK-backed avatar creation + wardrobe/custom editor + actual cosmetics/inventory + persistence + animation/VR hands + workable local/remote/bot avatar binding. Connected bodies acceptable. Validate with screenshots/runtime and SDK/API evidence.

**Gate C — PHYSICAL GAMEPLAY (HIGHEST EFFORT):** Reliable grabbing and full 26-primitive system, four flagship vertical slices, shader/VFX/audio integration, scale through all 72 recipes and 12 incidents; actual action changes task truth; test sabotage-repair.

**Gate D — NETWORK AND AI:** Photon multi-client reality, bots, Mom evidence/interrogation and optional speech, pets, sabotage/alliance and competitive pacing.

**Gate E — POLISH AND RELEASE:** House/environment, main UX, music/SFX, fidelity/performance, full-match testing and Quest-ready release artifacts.

The order is an **implementation priority**, not a permission to drop already-approved UI, audio, character animation or unfinished prior gates. Perform integration QA and checkpointing throughout, not only at the end.

### Deliverable audit (do not invent success)
Maintain `Docs/SHHH_V8_Continuation_State.md` (or consistent repo equivalent) with: last completed step, next executable step, modified file paths, original master requirement crosswalk, Genies SDK version/API and login dependencies, wardrobe features, avatar definitions/persistence tests, animation/hand/Photon tests, 72 recipe/26 primitive/12 incident coverage, exact Unity log locations, APK path/version/hash, runtime captures, pending risks and verified vs simulated vs untested flags.

Before another usage cutoff: save source safely, sync changes, preserve working APK, update the recovery/checkpoint command and give a concise factual report. Resume without needing me to reconstruct context.

### Absolutely unacceptable shortcuts
- Genies relegated to optional preview, while a custom Blender system is secretly used as primary.
- Retaining a mandatory disconnected/floating body because old text says so.
- Fake wardrobe categories or cosmetic images that don't equip Genies items.
- A generic Genies avatar shown only in a sample scene, not the main game.
- Full-body animation that fights Quest head/hand tracking.
- A beautiful sink with no physical faucet/soap/rinse/task state.
- A sponge touching a trigger which instantly cleans an entire surface.
- Water/dust particles that never affect task logic.
- Network menu without genuine private session, two-client evidence, and sync.
- Bots that teleport/score through invisible task actions.
- Mom that simply plays random warning sounds without causal reasoning.
- Treating an APK, passing unit tests, or a screenshot as headset validation.
- Re-importing all third-party assets or changing SDK versions without compatibility/licensing checks.
- Abandoning 72 recipes, 26 primitives, 12 comedy incidents or the continuous match.

## BEGIN EXECUTION NOW

**Do not give me a new broad plan instead of working.** Read the actual master, source and available handoffs; recover the interrupted build and work; confirm the existing Genies SDK; implement the **real, Genies-first creation and immersive wardrobe** as the primary in-game character system; then immediately work through the demanding physical household systems, multiplayer and AI, all with truthful evidence and saved checkpoints. The latest Genies/connected-or-disconnected permission overrides conflicting original avatar aesthetics. Preserve every other production requirement. Continue as far as the live tools and available session permit, record the exact next action, and never call untested work verified.
