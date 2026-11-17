# SHHH! — ASTRA MASTER PRODUCTION DIRECTIVE V1
## Full Quest-Ready 3D World, Characters, Room Architecture, Interactable Prop System, Chore and Sabotage Asset Library, Animation, Unity Integration, and Verifiable Handoff

**PROJECT:** SHHH! — Don't Wake Mom!  
**TARGET:** Unity 6 / existing URP project / standalone Meta Quest 2, Quest 3, Quest 3S  
**PRODUCTION ROLE:** Astra is the principal 3D art director, environment modeler, technical artist, prop and animation production lead, VR interaction asset integrator, and visual quality-assurance lead.  
**MODE:** CONTINUE THE REAL EXISTING PROJECT. DO NOT START A NEW GAME, REDESIGN THE APPROVED LOOP, OR RETURN ONLY A PLAN.

## 0. WHAT I NEED YOU TO ACTUALLY DO

I am providing concept images, character turnaround sheets, lobby and customization references, and the SHHH! brand archive. I also have an existing Unity project containing the licensed `Stylized - Simple Hands` asset and possibly earlier production assets. First inspect what truly exists in the project. Then BUILD, REFINE, IMPORT, WIRE, AND VALIDATE the complete playable 3D ART WORLD with actual scene geometry, optimized game-ready props, character models and rigs, required animations, collision and interaction anchors, visual task states, sabotage/reset variants, and working Unity previews. Do not confuse generated concept images with source mesh assets, and do not claim anything exists or functions before verifying the files and scene.

The goal is a high-quality, original, cohesive family home at night that looks recognizably like the supplied images IN A QUEST HEADSET, but uses physically plausible rooms, hardware-appropriate rendering, and gameplay-ready construction. The images are art direction, not requirements to reproduce every rendered pixel, fake 2D HUD, incoherent architectural feature, or high-cost cinematic effect.

Deliver real editable assets and scene files, not only renderings, screenshots, reports, or AI-generated illustrations.

## 1. INPUT AUDIT AND PRESERVATION — MANDATORY FIRST STEP

Inspect the currently opened Unity project and its Git/backup/version state before making changes. Find and report:
- Unity editor version, URP version, XR providers, packages, OpenXR/Meta XR Interaction SDK version, XR Origin/camera rig, installed hand prefabs, Android build configuration, scenes in build, and any working user features.
- All existing art folders, prefab GUIDs, mesh rigs, materials, animations, object scripts, physics settings, hand retargeting scripts and character assets. Identify the exact prefab/file paths and whether they work.
- If present, inspect `SHHH_ArtProduction_Delivery.zip`, any AnimationPolishV3 handoff, `TASK_GROUPS.json`, `ART_BIBLE.md`, `RIG_CONTRACTS.json`, `HAND_RETARGET_MAP.csv`, `Animation_Clips_Manifest.csv`, `Contact_And_Grip_Anchors.json`, and `CLAUDE_HANDOFF.md`. If missing, explicitly say so and search the current project before assuming no assets exist.
- Read the provided `SHHH_Complete_Brand_Kit_v1(1).zip`, especially its README, full/quick guidelines, original approved logo, SVG variants and `brand_tokens.json`.
- Examine each of the attached concept images, identify differences, and produce a concise canonical-room decision document.

**Preserve existing working functionality, references, GUIDs, prefabs, scenes, scripts, third-party licenses and imported asset metadata.** Work on a versioned branch or backup. If replacing a prefab, keep a migration map. If any automated script might overwrite handmade models or keyframes, protect or duplicate the source first.

Do not claim that an asset-store download, 3D image, Blender script, FBX import, clean console or `AssetDatabase.Refresh()` means functional hand tracking, true gameplay, or headset compatibility.

## 2. REFERENCE IMAGES: WHAT EACH ONE CONTROLS

These are the reference attachments expected with this prompt. Use the EXACT attached images, not web-search substitutes.

**Primary family stage and customization**
- `SHHH! Family Stage VR Lobby(1).png` — approved stationary theatrical family-stage concept, curtain and marquee shape, character presentation, hand-based selection. Keep the real existing UI/flow authoritative.
- `VR Character Customization Lobby(1).png` — stage-based character rotation/pedestal, wardrobe categories, accessories and colors. Use as visual direction; only implement categories actually supported in the game.

**Characters**
- `Shhh! Sibling Character Turnaround Sheet(1).png` — Pip, Nell, Rory, Tess silhouettes, colors, hair, accessories, front/back/left/right and emotions. CRITICAL: detached heads/torso/hands/shoes with no visible arms/legs, even when the concept mistakenly implies sleeves/arms.
- `Mom Character Turnaround Sheet(1).png` — Mom's hair, face, expressions, outfit palette, slippers and acting reference. CRITICAL: Mom is a FULL-BODY anatomical character in the real game as established in the older art bible and animation handoff; do not convert Mom into one of the floating-limb siblings merely because a diagram mentions detachable modules.

**Living room**
- `Living Room VR Stealth Adventure-1.png`
- `Shhh! Create a Distraction-2.png`
- `Don’t Wake Mom_ Cozy Chore Quest-3.png`
- `Shhh! Don’t Wake Mom!-4.png`
Use these for cozy furniture, living-room scale, game props, toy clutter, patterned rug, bookcase, staircase, kitchen access, moonlit windows and interactable objects. Do NOT canonize Mom sleeping on an upstairs landing, repeated fixed full-screen HUD overlays, or a particular disconnected spatial layout merely because an image renders it.

**Kitchen**
- `Moonlit Family Kitchen Adventure-1.png`
- `Cozy Cartoon Kitchen at Night-2.png`
- `Cozy Midnight Kitchen Chore Quest-3.png`
- `Moonlit Family Kitchen Adventure(1).png`
Use these for purple cabinetry, cream/light refrigerator, farmhouse sink, stove/oven, handles, dish rack, checked tablecloth, utensils, ingredients, cookware, warm under-cabinet lights and blue night sky. One image is a duplicated/incremental view; reconcile the architecture into ONE room.

**Laundry**
- `Cozy Colorful Laundry Room-1.png`
- `Cozy Moonlit Laundry Room-2.png`
- `Cozy Sunlit Laundry Room-3.png`
Use these for twin front-loading washer/dryer units, folding counters, baskets, sink, cabinets, hanging rails, drawers, towel piles and shelf storage. NIGHT IS CANONICAL: reinterpret daytime/sunlit image lighting as amber interior illumination and moonlit blue window fill. No daytime sky in the final evening game.

**Brand**
- `SHHH_Complete_Brand_Kit_v1(1).zip` — approved SHHH! illustrated logo, type treatment, colors, icons, logo spacing and font hierarchy. Main brand colors include midnight ink `#211530`, royal purple `#70409F`, hush yellow `#FFCF2E`, moon cream `#FFF0BA`, and sneaky coral `#F57F78`. Use logo lettering as artwork; the exact illustrated wordmark is not reproduced simply by typing a font. Luckiest Guy is the expressive display inspiration; Fredoka and Nunito Sans support UI.

**REFERENCE PRIORITY:** existing validated project files + agreed game design > art bible/task manifest > brand kit > concept image compositions. Resolve conflicts openly. No generated image alone authorizes new rules, currencies, mechanics, UI layouts, additional characters or architectural inconsistencies.

## 3. CORE WORLD: ONE CONNECTED HOUSE

Build an actual continuous stylized family residence, not three unrelated showroom scenes. Create an architecture blueprint, orthographic floor plan, room dimensions, door/corridor connectivity diagram, and an in-editor top-down view before high-detail modeling. The player must understand where rooms are in relation to each other.

Suggested relationship, to reconcile with existing scenes:
- Ground floor: living room with TV/sofa/coffee table/rug/bookcase/toy chest/shoe storage; open archway to kitchen with dining table and counters; accessible laundry room through a short ground-floor corridor/doorway; connecting hallway and stairs.
- Upstairs: a short landing leading to Mom's proper bedroom behind a door; Mom sleeps ON A BED INSIDE THAT ROOM. Hallway enables credible waking, door opening, footsteps and investigation. Her room should not be a bed balanced on a balcony or placed in line of sight from every downstairs interaction.
- Family Stage: stationary menu and customization theatrical set built within/adjacent to existing designated lobby scene. Preserve the curtain/box reveal and Solo/Friends/Customize logic. No generic floating flat app menu replacing the stage.

Author stable world-space units and 1:1-feeling VR dimensions. Provide clear walking/teleport routes without clipping, narrow accidental traps, excessively low collision ceilings, unreachable shelves or compulsory crouching. Hand-accessible chore hotspots should fit comfortable reach envelopes; placement zones should be intentionally forgiving. Use an approved movement system already in project; don't change locomotion or camera height arbitrarily.

Preserve consistent architectural markers across all camera angles: window positions, fridge/sink/stove arrangement, sofa-TV relationship, door placement, stairs and hallway. Build one canonical model first, then render from multiple camera positions.

## 4. PREMIUM VISUAL STYLE, WITHOUT PC-ONLY EXCESS

Creative territory: high-quality stylized cartoon, chunky rounded silhouettes, charming proportions, readable forms, layered purple/cream/wood materials, cozy family atmosphere, warm amber task lighting against cool moonlight, little homemade details, comedic visual staging. This is family-friendly sneaky party comedy, NOT horror, medieval fantasy, hyperrealism or gritty survival.

Materials should be Unity 6 URP compatible and use simple efficient shader variants. Prefer reusable trims, tileable surfaces, shared material families, atlas textures for props, baked/static lighting, light probes for movable actors and objects, reflection probes where truly useful, sensible shadow casters, LODs and occlusion. All dense plants, cloth folds, carpet yarns, micro-bevels, translucent volumetric beams, heavy bloom, glass reflections and high-frequency texture details in the images must be interpreted economically. Do not bake UI labels or glowing interaction outlines into texture albedo.

Room art should retain its charm with postprocessing disabled. Author a Quest 2 baseline and optional Quest 3/3S enhancement profile, without gameplay differences. Show explicit comparison renders and profiling evidence.

No eye-level screen-space HUD pasted across the player's entire headset view. Use minimal readable world-space/diegetic info, in-world focus cues, and comfort-tested near interactions. A concept-image timer/task banner is only a reference, not a command to add permanent head-locked overlays.

## 5. ASSET TAXONOMY AND COMPLETE PROP PRODUCTION

Produce a machine-readable `ASSET_MANIFEST.csv` with fields: stable_ID, room, asset_name, category, source/reference, ownership (new/reused), source_mesh_path, Unity_prefab_path, materials, texture_resolution, estimated_triangles, collider_type, grip_anchors, task_ids, animation_clips, physical_state_variants, noise_tag, LOD, license/source, QA_status. Each prop that has meaning for gameplay must have its own stable ID and reference-safe prefab.

### 5A. Living room
**Architecture and furniture:** rounded sofa, armchair, upholstery throw, cushions, coffee table, side/end tables, TV and low cabinet, shelves, bookshelf, books, rug, curtains, moonlit windows, lamps, picture frames, wall art, door and handle, stairs and banister, archway to kitchen, trim/baseboards, potted plants and simple decorations.
**Gameplay props:** books with shelf sockets and at least several distinct cover variants; toy chest with opening lid and toy sorting zones; grabbable blocks, toy cars, ball, simple plush toys, possible squeaky toy, lightweight cartoon prank object such as a rubber chicken if approved; shoe rack and different shoe pairs; cups, plates, coasters, cushion placement targets, remote, safe controllers, scattered clutter, safe toy boxes, small throw blanket, pet bed/bowl/toys when location is established in canonical layout.
**States:** books ordered/disordered; toys in chest/out; shoe pairs matched/mixed; cups cleared/not; cushions neat/displaced; remote in correct spot/moved; clutter/cleanup in-progress; evidence after disturbance. Interactable states must be real transform/state changes, not image swaps that hide physics problems.

### 5B. Kitchen and dining
**Architecture:** one fixed cabinet arrangement; consistent refrigerator/freezer, cabinet doors, drawers, sink, faucet, draining rack, countertop, cooker/oven/range hood, pan storage, wall hooks, dining table, chairs, window and curtains, pendant fixture, shelving, tile backsplash, doorway/archway and flooring.
**Gameplay props:** clean/dirty plate groups, cups, bowls, cutlery and cutlery drawer, cabinet target slots, saucepans and frying pans with proper handles, lids, lightweight kettle, utensils, chopping board, sponge, wipe cloth, paper towels, cartoon crumbs/spills, fruit bowl and individual fruits, tea towel, safe cleaning bottle mockups, pet food scoop/bowl if assigned, small movable pantry containers, organizers and no-sharp-edge props.
**States:** dirty/clean, in sink/in rack/in cabinet, cupboard opened/closed, cookware disordered/stowed, table messy/wiped, fruit misplaced/organized, food/pet bowl empty/refilled, noise from bumped pan or dropped light dish. Chores must be non-hazardous, stylized and physically plausible; do not force real-world flames, boiling water or sharp knives into a family VR game. Prop collisions and contact sounds should be controlled.

### 5C. Laundry
**Architecture:** one washer, one dryer, washer/dryer doors, top counters, wardrobe/supply cupboards, farmhouse/utility sink, wall shelves, towel shelf, closet/rail/hooks, hampers, baskets, laundry sorter, cabinets, window, curtains, pendant fixture, door, rug.
**Gameplay props:** differently colored and typed shirts, pants, socks, towels, laundry cloth piles, hangers, fold surface, separated baskets with sockets, washer load sockets, cloth bundles, bin lids, lightweight spray/bottle props, optional clothespins only if a real task needs them.
**States:** unsorted/sorted, basket filled/emptied, washer open/closed and loaded/unloaded, towels unfolded/folded/stacked/hung, items mixed after sabotage and restored after repair. To save Quest resources, use lightweight discrete cloth pose variants and authored fold animations rather than real-time physics cloth simulation.

### 5D. Family Stage lobby and customization
Stage frame, curtain meshes with low-cost animations, marquee bulbs or emissive fakes, spotlight cones as low-cost aesthetic effects, four clearly named sibling reveal positions, podiums, character focus pedestal, hand-point selection targets, decorative bookshelf/house ambience, stylized wardrobe display, accessory shelves, color swatches, framed preview elements, readable in-world labels and existing menu controls. Preserve the game's actual menu navigation and state, rather than introducing fake level/currency panels shown only in concept images. Customizable cosmetic pieces must be interchangeable meshes/material variants and should preserve character silhouettes, face visibility, and rig behavior.

### 5E. Mom's bedroom, hallway and pets
Actual Mom bed with pillow, blanket, slippers, bedside table, night lamp, door + rotating handle, wardrobe, basic carpet and decorative room assets, stair/landing props and collision. Author credible camera-visible transitions for wake → sit → stand → open door → walk → inspect downstairs. For Mochi and Biscuit, create anchored idle/sleep/walk/sniff/nudge/react behavior props such as bed, toy and bowl; they should occasionally create or encounter minor disturbances with physical evidence.

## 6. CHORE SYSTEM: NINE GROUPS, EIGHTEEN PRIMARY ACTIONS

The previous art handoff describes **9 task groups / 18 primary actions** across living room, kitchen and laundry. Read the actual `TASK_GROUPS.json` if available and treat it as authoritative for task IDs and precise actions. The examples below illustrate coverage, NOT a replacement for the manifest. If the source JSON is unavailable, first reconstruct a provisional task matrix and clearly label its unconfirmed entries; do not pretend you recovered the original 18 exact actions.

| Room | Expected group themes | Physical work examples |
|---|---|---|
| Living room | 4 groups | Books/shelf; toys/chest; shoes/rack; cups-and-cushions |
| Kitchen | 3 groups | Dishes/cabinet; cookware/cupboards; wipe-fruit-pet-feeding variations |
| Laundry | 2 groups | Sorting and loading clothing; folding/stacking/hanging towels |

For EVERY primary action, deliver: source prop, valid starting positions, visible target destination, hand collision/grab point, reachable anchor, beginning/intermediate/end states, progress condition, failure and retry, soft placement snap tolerance, object carry behavior, release pose, completion animation/FX, feedback sound hook, task ownership metadata, noise classification, supported reversals/sabotage, and an observable repair sequence.

Example implementation contract:
- Source object -> player approaches -> hand pose/grip -> pickup -> carries/manipulates -> enters destination zone -> releases -> destination validates -> visible settled result.
- Rival later moves/rearranges the object -> state returns to disrupted -> original assignee or helper notices -> physical recovery -> task state revalidates.
- A task doesn't complete because an NPC played an animation; it completes because the authoritative object state meets the true task predicate.

Use repeated prop archetypes and configurable scripts/anchors to support 18 actions efficiently, not 18 bespoke high-poly disconnected systems.

## 7. SABOTAGE AND REPAIR — PRODUCTION READY

Every designed household sabotage needs: a physical initiator; an observable object-state change; a fair, discoverable correction; a physical-evidence trace when relevant; a noise event; cooldown/protection hooks; and no impossible object teleports. Typical supported reversible acts: remove a book from shelf, place toys on floor, mismatch a pair of shoes, displace a cushion, move a cup, rearrange dishes, mix laundry colors, unfold a towel, knock a lightweight pan, relocate a bowl, and trigger a squeaky toy.

Use ordinary household interaction as the majority of sabotage opportunities, with a small optional set of playful cartoon props. Never make prank props substitute for the core chores. Do not reward infinite sabotage/repair loops; expose reliable events with initiator, observer and state transitions for later gameplay logic.

Create distinct visual/sound states for quiet normal handling, suspicious movement, mild clatter and big comedic disturbance. Do not drive Mom's guilt decision solely from the all-knowing event record.

## 8. CHARACTERS AND EXPRESSIVE ANIMATION

**Pip, Nell, Rory, Tess:** Preserve their approved names, colors, face/hairstyle, outfits/accessories and existing source rigs where present. Silhouette design = floating/disconnected HEAD, TORSO, TWO HANDS and TWO SHOES; **NO VISIBLE ARM OR LEG GEOMETRY** and no hidden rigid limb tubes. Character parts act as a coherent whole with procedural IK/targeting and expression. The turnaround sheet is style reference; any connected limbs inadvertently depicted in 2D concept art are not to be reproduced.

Produce/validate the necessary clips: idle, blink, listen, whisper/shush, point, grab low/mid/high, carry, shelf place, sort, stack, wipe, fold, cupboard open/close, cautious sneak, run, sudden stop, look back, reaction to noise, mischievous glance, surprise, disappointment, accusation, defensive protest, relief, elimination, victory, temporary cooperation, betrayal recognition. Ensure expressive facial shapes and mouths for optional speech. Retain in-place motion and root-motion variants according to the existing animation contract. Keep shoes planted and separate hands aligned to object anchors.

**Mom:** Full-body adult with approved robe/flower apron/pink slippers/hair and expressive face. Rig and animate sleep breathing/snore, stir, hear noise, rise from bed, slippers/contact, cautious approach, touch doorknob, swing door, hallway walk, descend/approach, scan, inspect object, listen to sibling, point/accuse, warn, disappointment, surprise, scold, settle and return. Support modular clips/branching, not a single noninteractive cinematic. Mom must physically reach her actual room door. She should not appear waiting downstairs as a permanent static prop.

**Mochi and Biscuit:** low-cost stylized cartoon cat and dog with correct paws/contacts and simple loopable idle, sleep, walk, sniff, nudge toy/bowl, startle and settle. No aggressive animations and no unexplained evidence events.

## 9. HANDS: REUSE THE INSTALLED ASSET, DON'T REMODEL IT

**Approved existing asset:** `Stylized - Simple Hands` from https://assetstore.unity.com/packages/3d/characters/stylized-simple-hands-221297 . It already exists in the Unity project according to the owner. First find its actual prefabs and mesh assets and verify their LICENSE and rig structure. Don't download/import a duplicate unless expressly needed. Do not redistribute raw asset-store models outside the properly licensed project/package.

Use this hand visual (both left/right, if available) as the player's VR hands and match its material presentation to the existing white rounded five-finger glove style. DO NOT assume a cute mesh is already hand-tracked merely because it is rigged. Inspect joint hierarchy, bone orientations, bind pose, scale, finger skinning and blend/grip pose compatibility. Retarget to the currently installed Meta Interaction SDK / OpenXR hand tracking rig without breaking input tracking; use Meta's existing Interactors and authoritative grab/poke/ray behavior. Prefer the installed project's XR setup and SDK versions, and identify any required migration rather than mixing older/newer skeleton systems blindly.

Required interactions: full tracked hand pose, open/closed fists, pinch, index pointing, palm contact, low/mid/high grabs, plate rim, cup handle, drawer/cabinet knob, book edge, two-hand laundry basket, soft object placement, rotation, and hand loss/reacquisition. Use object-specific grip and release anchors; avoid fingers passing through meshes, hands snapping meters away, floating props after tracking loss or hand-pose animation overriding actual real tracked motion.

Provide a fallback when tracking confidence drops: hide/soften hand visual appropriately, freeze/return held objects safely under deterministic policy, and avoid unintended repeated grabs. Support hand-ray + pinch for distant in-world lobby UI; physical grab/poke in reachable gameplay areas. If the user's actual project supports controller fallback, preserve that capability as well.

See relevant current Meta documentation before changing XR rig:
- https://developers.meta.com/vr/documentation/unity/unity-isdk-getting-started-unityxr/
- https://developers.meta.com/vr/documentation/unity/unity-isdk-customize-hand-model/
- https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/
- https://developers.meta.com/vr/documentation/unity/unity-isdk-interaction-sdk-overview/

## 10. PHYSICAL TASK AND BOT CONTACT CONTRACTS

Build task objects as real editable prefabs with discrete states, lightweight colliders and stable reference transforms, not just beautiful inert environment meshes. Each interaction-ready prop prefab should declare:
`AssetID`, `TaskGroupID`, `TaskActionID`, `OwnerSlot`, `Grabbable`, `GrabAnchorsLeft/Right`, `UseSocket`, `ValidDestination`, `CurrentState`, `ResetState`, `NoiseProfile`, `SabotageClass`, `InteractionTags`, `EvidenceTags`, `AnimationEvents`, `ContactThresholds`, `Revision`.

Provide the actor interaction anchor targets so human hands and AI siblings can perform the same task via shared action validation, e.g. bot approach/reach/grasp/carry/place/release with actual state change. AI action sequences cannot silently create/destroy objects or alter task state from the language model. The architecture should support later Unity gameplay logic and bot decision making by Claude/engineers. Mom's AI belief system must consume observed/heard/inspected evidence, not omniscient internal developer data.

Astra's requirement is to deliver fully operational *art-facing physical interaction components and test hooks* where the Unity environment permits it, and a documented contract for higher-level round management, alliances, LiveKit dialogue, interrogation and elimination. If those game systems already exist, CONNECT to them rather than creating parallel ones. Do not substitute staged pre-rendered cutscenes for physical task completion.

## 11. MOM'S WAKE-UP AND INTERROGATION STAGING

Model spatial routes, sightlines, door interaction anchors and readable actor positions so the established one-major-confrontation-per-round gameplay can actually happen in the 3D house. Mom begins asleep in her real upstairs bedroom. When a disturbance occurs, she stirs, warns, later approaches, enters the area, scans the actual incident, and can face siblings naturally without locking the headset to a cinematic camera.

Create 3D evidence and witness presentation assets only as needed: inspectable displaced props, small witness memory card/panel, readable object focus marker, short in-world name indicators, and Mom/sibling reaction animation. Provide both hand interaction and optional voice expression presentation; voice is not required for gameplay. Mom cannot know a secret event from the art layer unless there was an observable path for her perception.

Do not change camera position or forcibly turn the player's head during confrontation. The player must maintain comfortable free-looking first-person VR throughout.

## 12. UNITY STRUCTURE AND DELIVERABLE SCENES

Implement or integrate in the EXISTING Unity 6 URP project, without silently replacing it. Suggested versioned output subtree (adapt paths to the real codebase):

```
Assets/SHHH/ArtProductionV4/
  Architecture/Rooms/  Architecture/Blueprints/
  Props/LivingRoom/  Props/Kitchen/  Props/Laundry/  Props/Shared/
  Actors/Siblings/  Actors/Mom/  Actors/Pets/
  Hands/Adapters/  Hands/GripPoses/
  Animations/  Materials/  Textures/  Lighting/
  Interactions/Anchors/  Interactions/Sockets/  Interactions/TaskVisuals/
  Prefabs/Rooms/  Prefabs/Props/  Prefabs/Actors/
  Scenes/  AudioHooks/  Validation/
```

Provide functioning scene assets or non-destructive additive integration with existing scenes for: stage main menu, stage customization, contiguous house gameplay, Mom's bedroom/hallway, and a concise prop/hand-interaction test room. Preserve scene names and IDs if existing.

The main scene must contain physically positioned colliders/doors/cupboards, referenced scripts/events, sensible rigidbody settings, navmesh/path plans, room lighting, environmental audio emitters, and correctly placed interaction sockets. Keep source FBX/Blender/glTF as permitted, with clean origin/pivot transforms, unit scales, UVs, material assignments and naming. Do not use mesh colliders on every small dynamic prop if simple primitive/compound colliders suffice.

## 13. QUEST 2/3/3S TECHNICAL REQUIREMENTS

- Build for standalone Android Quest, not a Windows editor rendering or PCVR-only demo. Unity URP and Meta/OpenXR integration must match installed project versions.
- **Quest 2 is the minimum quality/performance baseline; Quest 3/3S may offer quality tiers**. Confirm device availability and test on actual hardware where possible. Minimum sustained target 72 FPS on Quest 2 in the heaviest representative scene, measured under representative gameplay conditions; adjust to the project's actual refresh/VRC settings if stricter.
- Optimize for the peak case: 4 sibling avatars with animation, Mom investigation, pets, hand meshes, numerous nearby props, audio and task state changes; no artificially empty-room-only benchmarks.
- Profile with actual on-device tools and report CPU/GPU frame times, FPS, draw calls, triangles, GC spikes, memory, render scale, overdraw and worst interactive scene. Record exact headset/software/build settings. Never fabricate those results.
- Favor baked lighting, shared materials and texture atlases, mesh batching/instancing when it truly saves performance, occlusion culling, limited dynamic light/shadow passes, fixed-point object physics where practical, low-cost decals and simple animation rigs.
- Replace expensive fluff from the concept images with optimized equivalents: authored rug texture instead of strand geometry, static stylized plants instead of alpha-overdraw bushes, fake cabinet light strips rather than dozens of realtime lights, stylized material highlights rather than SSR or heavy volumetrics.
- Keep hand interaction, physics and frame timing stable. No world-scale alterations, artificial camera motion, harsh flashes, excessive postprocess, head-locked bars obstructing vision, or mandatory small-font reading.
- Visual differences between Quest 2 and Quest 3/3S should be QUALITY only; task access and gameplay parity must remain.

Official reference:
https://developers.meta.com/vr/documentation/unity/unity-perf/
https://developers.meta.com/vr/documentation/unity/unity-best-practices-intro/

## 14. ASSET FACTORY AND ANIMATION QA

Create editable Blender source files/actions for newly authored assets where possible. Also deliver actual Unity-importable meshes, armatures, rigs, clips, material instances, prop variants, and prefab assemblies. Author a procedural batching or Blender script only if it actually outputs the source files and you inspect the results; do not substitute code that has never been executed for produced assets.

Every meaningful character/prop interaction needs demonstration with real mesh contact. No pose may show hands clipping deeply through plates, feet skating, cat paws gliding, drawers opening without hand proximity, an object magically teleporting into a cabinet, or Mom reaching for a handle she never touches. Record exceptions with status and remediation.

Maintain sensible origin/axes and global scale: prefab origin, world units, animation coordinate systems, mirrored left/right versions, pivot at appropriate hinge/handle, collision and usable grip axes, and deterministic physics state.

## 15. CHECKPOINTS: EXECUTE IN THIS ORDER

**Gate A — Audit & blueprint:** inventory actual project, verify references and library, backup/version, reconcile contradictory concept views, canonical floor plan, room adjacency, asset/reuse map, identify working XR rig and existing hand mesh. Show actual Unity scene evidence. Do not polish or replace blindly.

**Gate B — One production-quality vertical slice:** implement one contiguous living-room zone with 1 shelf/book task, 1 reversible sabotage, current VR hands, one bot physical approach/reach/place demo, Mom's physical evidence prop and visible clue, night lighting and Quest 2 performance profiling. This is the fidelity/interaction standard for all other work.

**Gate C — Full architecture:** build/finish the living room, kitchen, laundry, hall/stair and Mom bedroom with consistent connectivity and matching recognizable camera angles. Capture a real in-engine 360-degree/angle sweep, not just AI-generated renders.

**Gate D — All tasks and variants:** map to exact original 9 groups/18 actions, create the necessary dynamic props, task sockets, object state visuals and reversible sabotage variants; test each action independently by a user hand and scripted/simulated bot actions.

**Gate E — Character, Mom and pets:** integrate/repair existing rigs, animations, facial expressions, contact anchors, move logic, Mom bed/door/inspection staging and pet interactions; preserve character floating-limb contract.

**Gate F — Lobby/customization integration:** reuse approved stage and brand kit, actual working menu flows, character variants and interaction controls. Verify that turning/equipping cosmetics doesn't break rigs, names or prefab links.

**Gate G — Unity/Quest QA:** build, profile and test, fix high-priority issues, produce recorded evidence, manifest and an honest handoff. Loop over gates until objective failures are addressed or documented as blocked.

If tool or account access is limited, finish as much as you can in the current environment, isolate blockers, and provide runnable source artifacts and precise follow-up integration instructions. Never invent a completed Blender render, Unity Play Mode test, headset run, asset count or code compilation result.

## 16. REQUIRED EVIDENCE AND ACCEPTANCE CRITERIA

Deliver a real production report and videos/screenshots generated from assets in Blender or actual Unity runtime, not AI still images. Include:
1. Unity hierarchy showing same connected house; overhead blueprint and dimensioned room layout.
2. Four angles of **the same** living room, kitchen and laundry, with matching architecture across angles; screenshots ideally from real Unity scene.
3. Full Family Stage menu and customization interactions using actual in-world selectable props and buttons.
4. Real VR hand demo: both hands opening/closing, left/right grip, book, cup, plate, cupboard, towel, basket and temporary tracking loss test.
5. Four siblings idle/turn/sneak/react and correct disconnected-limb appearance in close-up and at ordinary VR distance.
6. Mom wake from bed, sit/stand, open bedroom door with real hand contact, move down, inspect, point and accuse; one coherent sequence and independent branching clips.
7. At least one complete action→sabotage→repair recorded vignette in each main room, then manifest-backed demos/valid reusable recipes for all original 18 actions.
8. Mochi/Biscuit movement, paw contact and at least one believable prop nudge reaction.
9. Standalone-device frame-time report in heavy scene, including Quest 2. If no Quest available, mark `NOT TESTED ON HEADSET` plainly; a PCVR capture is not proof.
10. No missing textures, no broken prefab GUID links, no imported-only stand-ins with non-functional interaction state; list exceptions by asset ID.

Use statuses `PASSED`, `ROUGH`, `BLOCKED`, `NOT TESTED` with evidence path, tested environment, failure details and next steps. Require visual QA, functional QA and device QA to pass independently; importing without errors does not prove quality.

## 17. DELIVERABLE FOLDER AND DOCUMENTATION

Package a complete versioned production source/handoff such as `SHHH_Astra_3D_World_Production_V4/`:
- `00_Audit_And_Project_Backup_Notes/` — inventory, compatibility, conflicts, existing IDs, backup/version reference.
- `01_Canonical_Architecture/` — floorplan, elevations, dimensions, room connections, camera maps.
- `02_Blender_Source/` — actual scene/prop/actor source projects and reusable tooling.
- `03_Unity_Game_Assets/` — importable models, rigs, clips, materials, textures, meshes, prefabs, safe scripts/components and additive scenes (or committed project branch).
- `04_Hand_Tracking_Integration/` — exact installed Stylized Simple Hands paths, rig/joint mapping, grip anchors, adapter, tests.
- `05_Task_And_Sabotage_Library/` — asset state matrix, authoritative task mappings and socket configurations.
- `06_Rendered_And_InEngine_Reviews/` — actual videos and multi-angle screenshots; do not pass AI artwork as game footage.
- `07_Performance_And_QA/` — device test logs, Unity validation, profiling and defect list.
- `08_Handoff_For_Claude_And_Unity/` — stable object IDs, components, object event schema, prefab paths, available animations and missing gameplay integration work.
- `ASSET_MANIFEST.csv`, `TASK_ACTION_MATRIX.csv`, `ANIMATION_CLIPS_MANIFEST.csv`, `PREFAB_MIGRATION_MAP.csv`, `ACCEPTANCE_REPORT.md`, `CHANGES_FROM_BASELINE.md`.

## 18. WHAT ASTRA OWNS VS WHAT CLAUDE/UNITY GAME LOGIC OWNS

Astra OWNS: the look and feel, 3D source files, physical environments, room connectivity, character and prop models/rigs/animations, task props, world-space UI art, collider/grip/socket and noise/evidence hook metadata, existing Unity scene and hand visual integration, visual and device-art QA.

Claude / the Unity gameplay layer OWNS (unless already working in project and merely needing integration): authoritative round state, individual chore assignment, scoring, alliance/betrayal decisions, AI sibling strategy, Mom's non-omniscient knowledge and suspicion model, LiveKit optional voice, witness memory event logic, interrogation verdict, eliminations and multiplayer network authority. Astra must create a real and testable 3D substrate for these systems and interface with any existing implementations; do not fake AI gameplay with storyboard animations or assume dialogue controls physics.

## 19. YOUR FIRST MESSAGE/OUTPUT AFTER READING THIS

Start by reporting the ACTUAL workspace/project paths found, existing scene names, installed Unity/XR stack, actual `Stylized - Simple Hands` assets and any prior SHHH rigs/props/tasks. Then present the canonical room layout and prioritized reuse/build matrix. **Immediately proceed to Gate B's real living-room vertical slice**, producing actual editable files, Unity import, visual proof and test results wherever the tool environment allows. Do not stop at ideas, prompts, unexecuted scripts or concept renders. If you lack authority, tools or local filesystem/Unity access, say precisely what is inaccessible and deliver only what you can genuinely create, with specific integration instructions.

**The primary acceptance criterion is simple: I should be able to open my EXISTING Unity project, enter the connected SHHH! house in Meta Quest, see the approved stylized environment and characters, use the installed tracked hands to interact with real chore props, observe visible task/sabotage/repair states, and reproduce your demonstrations.**
