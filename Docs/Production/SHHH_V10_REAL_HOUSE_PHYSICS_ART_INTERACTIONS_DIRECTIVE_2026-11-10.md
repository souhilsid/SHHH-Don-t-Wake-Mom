# SHHH! — DON'T WAKE MOM!
## V10 OWNER OVERRIDE: REAL HOUSEHOLD PHYSICS, IN-WORLD TASK PLACEMENT, HAND-PAINTED TOON ART, VOLUMETRIC-LOOK LIQUIDS, AND DIRECT MANIPULATION
**Production continuation directive — 10 November 2026**  
**Audience:** Claude implementing the **EXISTING** Unity 6 / URP 17 / Meta Quest SHHH! game.  
**Status:** Authoritative *owner correction* to physical gameplay and visual quality. Preserve all unrelated and already working requirements from the November 9 master prompt, Genies-first revision, and November 10 V9 physics directive.

> **TOP-PRIORITY DIRECTIVE:** The current physics are far too basic. I do NOT want a game where household tasks are completed by poking invisible/obvious buttons, tapping a tool once, placing an item on a table trigger, or moving an animation through prearranged steps while the world itself stays fake. SHHH! must feel like a playful, highly interactive, convincing **real home that I can physically manipulate in VR**, with hand-painted stylized cartoon art, actual moving 3D mechanisms, tangible object contacts, satisfying soft-body-like motion where practical, shader-driven liquid volumes and dirt/wetness, and integrated VFX, spatial sound, haptics, and Mom's causal noise system.

## A. OWNER OVERRIDES — READ THIS FIRST AND PERSIST IT

1. **No central task table / prop-spawn table.** No artificial station where all task objects appear stacked on a dining table or on empty gray boxes. Every active and potential task belongs in its **real environmental location**: dirty dishes naturally at the dining table and sink, a sponge at the caddy, soap at the sink, plates in cabinets or on the counter, clothes in bedrooms and hampers, toys in bedrooms/living room, dust on actual surfaces, mops and buckets in laundry storage, bins where bins belong, pet items at the pet area. Players walk across the connected house to discover, collect, transport, operate, clean, and return real objects. Task UI is information, not an object-generating machine.
2. **Real interaction over poke-script shortcuts.** A faucet is physically grasped and rotated or pulled along its mechanical limits; a dispenser has a modeled compressible plunger/spring and can be pressed physically with a held sponge positioned beneath it; the hand actually grasps the sponge; a door opens by grabbing its handle; the drawer has rails and collisions. A real household appliance can have a physical push-button, but the button only does what its real mechanical role would do, not instantly complete an unrelated chore. Do not use generic `PokeInteractable -> CompleteStep` as the main mechanic.
3. **Objects are mechanically functional.** Rigidbodies, appropriate collision proxies, mass/inertia/friction, continuous collision detection selectively, hinge/slider/configurable joints, physical grab transformers, constraints, partial openings, closing/latching behavior, obstructions, noise and physical recovery. A 3D model must not be a static decoration when its household purpose requires opening, pouring, holding, pulling, twisting, folding, balancing, breaking safely, or carrying.
4. **Every object and effect is stylized hand-painted.** Imported low-poly *does not automatically equal finished SHHH! art*. Unity materials, UV textures, scuffs, grunge, wetness, ceramic, wood, metal, sponge, cloth, food, soap, trash, stains, bubbles and splashes must share a handcrafted **Soft-Sculpted Storybook Toon** art bible. No raw gray primitive appliance, generic flat untextured blocks, harsh realistic stock PBR, or neon default particle effects. The underlying motion can feel realistic while its appearance is cartoony.
5. **Water must look like WATER, not a LineRenderer.** Make real spatially substantial, fluid-looking, stylized water streams, visible liquid levels/volumes inside cups, basins, buckets, and bottles, meaningful splash/foam/droplet interactions and flowing-draining wetness. Use **mobile-performance-conscious simulation**: finite reservoirs, trajectory and collision estimation, evolving procedural ribbon/tube meshes or particles, bounded liquid fill meshes and shader effects; full computational fluid dynamics or millions of particles are NOT required or appropriate on Quest. The physical state must be real even if rendering is approximated.
6. **Waste/mess must also be hand-painted and mechanically meaningful.** Dirt, crumbs, food residue, soap scum, puddles, mud, litter, foam, dust, pet mess and loose objects must match the game's stylized materials. Contact changes the specific touched portions, debris moves when brushed, wet zones behave like wet zones, and a stain cannot disappear by waving a sponge in empty air.
7. **Every one of the original 72 chores must ultimately obey this standard.** Preserve their IDs, goals and sabotage/repair semantics; rewrite their physical implementations where they are shallow. The 26 physics primitives and 12 comedy incidents in the original master remain required. Implement incrementally and validate each category in actual Unity PlayMode rather than mass-creating superficial definitions.
8. **The working Genies creator and two-client Photon work must remain intact.** Latest reported Editor result: Genies 15/15 customization checks, earlier two-client appearance synchronization with 0 mismatches in 339 checks. These are reported—not Quest-verified. Use Genies as PRIMARY avatar editor and runtime characters. Connected or disconnected character bodies are both acceptable. Upgrade hands/IK for physical props without breaking avatar identity, saving or remote synchronization.
9. **No headset verification is claimed until actual headset interaction is observed.** Desktop simulation is useful but cannot prove physical grasp, stereo shaders, comfort or Quest frame rate. Do not call a successful C# compile an interaction success.
10. **Memory is durable project state, not a conversational promise.** Add the exact owner change to `CLAUDE.md`, `.claude/rules/shhh-physics-production.md`, `Docs/SHHH_OWNER_REQUIREMENTS.md`, `Docs/SHHH_PHYSICS_CONTRACT.md`, and `Docs/SHHH_V10_RECOVERY_STATE.md` or their canonical existing equivalents. Make it the newest priority section so subsequent sessions cannot revert to poke-button workflows or central-spawn-table tasks. Update a precise resumption point after every integration checkpoint.

**Conflict precedence:** THIS V10 override > V9 physics directive > Genies-primary correction > 09 Nov master where conflicting. All preserved game rules are unchanged: one continuous 5- or 10-minute match; four sibling seats; entire connected house; private Photon multiplayer with bots; Mom perception, witnesses and interrogation; sabotage/repair; original soundtrack and cartoon nighttime atmosphere.

---

## B. RECOVER WHAT YOU ACTUALLY HAVE — DO NOT START ANOTHER PROJECT

Work in the actual existing SHHH! Unity project, not a new Unity scene collection masquerading as the game. Read the latest `CLAUDE.md`, the exact code, assets, `.meta`/GUID records, testing records and commit status. The last user-visible production report says:

- `_sync/v9m.tgz` already matched all 24 files on the Windows PC and had been applied; do not apply it a second time blindly.
- The latest changes compiled and all **157 rules tests** passed.
- Genies avatar editor and prior two-client Photon appearance sync were preserved.
- Dishwashing exists at the real kitchen sink and uses imported faucet lever, dish rack and soap dispenser.
- Latest real-model desktop simulation **stalled while positioning plate 2 in the rack**; plate 1 stood upright; plate 2 rattled in the divider with **441 clatter sounds** in a run. Another test lost a plate over the table edge.
- Soap → scrub → rinse were shown in Unity Editor simulation, but not yet proven in actual physical hand/controller interaction or on Quest.
- The last confirmed APK was **c12** and **did not include Genies/physics changes**.
- Bots, multiplayer dishwashing and headset play were not tested for the new feature.

**Required immediate actions:** inspect current workspace, sync state and Git; create rollback snapshot; confirm old assets preserved; diagnose rack collider/contact manifold and audio event storm; correct plate2 slot clearance, rack geometry, mass/friction/contact, settling and stabilization. Do not fix sound spam by deleting all collision noise. Use impulse threshold, relative velocity, contact aggregation, per-object-pair cooldown, minimum distinct impact interval and global voice budget, with one authoritative noise event per meaningful collision cluster. Distinguish sliding/rattling loops from discrete hits. Retest quiet and loud placement, sabotage and recovery. Verify the whole sink/chore geometry against the actual hand reach, not just autopilot target coordinates. Only then rebuild visual/physical quality, using the already working baseline as rollback.

**Important:** do not leave the task in a fake `5/5 PASS` state via widening triggers or forcibly snapping plates into slots. Find the geometric/physical cause and implement an ergonomic, physically credible settle action with proper contact and visually subtle guidance only at the final centimeters when required. Keep dishwashing playable in the actual kitchen.

---

## C. NON-NEGOTIABLE PHYSICAL INTERACTION ARCHITECTURE

### C1 — Local XR body and hands

Inspect the installed Meta XR Interaction SDK version and current project integration. Meta's default `GrabFreeTransformer` may update transforms directly and can permit penetration during the grab; use a physics-aware motion approach or joint when the real mechanic requires collision. Options to evaluate **against the installed version** include `GrabFreePhysicsTransformer` (Rigidbody velocity-based) and `OneGrabPhysicsJointTransformer` with suitable joints. For constrained actions use `OneGrabRotateTransformer` and `OneGrabTranslateTransformer`, with correctly oriented pivot/axis, hand anchoring and mechanical bounds. Do not blindly switch *all* objects to joints; tune by prop type and stability.

Refer to official Meta documentation (verify installed API before coding):
- https://developers.meta.com/vr/documentation/unity/unity-isdk-hand-grab-interaction/
- https://developers.meta.com/vr/documentation/unity/unity-isdk-constrained-grab-interactions/

Preserve XR tracked hand pose, pinch/palm grab start/end, controller equivalents, subtle haptics and Genies avatar retargeting. Build correct handle-relative grip anchors, palm contact, opposing thumb/index, fingers on tool surfaces, two-handed support when needed, grip switching, release velocities, no ghost-hand collision, no wrong handedness, and stable behavior when tracking is briefly lost. A static hovering hand icon is not enough.

### C2 — Household components, not flat button handlers

Create or extend modular components (use existing names if appropriate):

`PhysicalPropDefinition`, `GrabbablePhysicsBody`, `GripAnchor`, `GripPoseProfile`, `DynamicHandContactProxy`, `ContactMaterial`, `ContactManifold`, `ConstraintDriver`, `HingedAssembly`, `SlidingAssembly`, `RotaryValve`, `SpringPlunger`, `LatchMechanism`, `ApplianceStateMachine`, `ContainerVolume`, `Reservoir`, `PourSpout`, `FluidReceiver`, `LiquidTransfer`, `WetnessGrid`, `DirtCoverageMask`, `ToolContactSensor`, `SoapCoverageState`, `ClothGripRig`, `NoiseEventAggregator`, `PhysicsVfxProfile`, `PhysicalTaskProgress`, `TaskWorldPlacement`, `RecoveryAnchor`, `BotPhysicalAction`, `PhotonPropState`.

These are conceptual interfaces, not an instruction to create duplicate classes when the project already implements equivalent functionality. A component counts only when actually wired to runtime prefabs and tasks.

### C3 — Mechanics must respond continuously

A good interaction is not just `OFF -> ON`. Use physical state variables and continuous motion where appropriate: lever angle→water flow; pump compression→dispensed dose; cloth pressure/speed/contact→dirt map; container tilt+fluid volume→pour flux; washer door angle and latch→closed/open and reachable drum; bucket volume+acceleration→sloshing/spill; drawer translation→open-space access; object impulse+material→noise and wake-up risk.

Use `FixedUpdate` or consistent physics timing for force/joint response, continuous contact windows where needed, clamped velocities/accelerations, stable collision layers, and separation of **world truth** from render/audio. Simulated finger force cannot be truly measured from consumer hand tracking: use actual measured kinematics/contact proxy and a documented bounded pressure proxy; never falsely claim force-sensing hardware.

### C4 — Natural ergonomics

VR interactions should feel real but be physically comfortable: generous well-modeled handles, reachable shelves, gripping and regripping, small final alignment tolerances, clear collision surfaces, natural arm motion, accessible alternative for difficult gestures, no leaning across real physical furniture, locomotion or teleport through the house, no simulated force that drags the player's real head, no precise millimeter-level tuning that frustrates users. A subtle magnetic guiding field at last centimeters is acceptable; remote instant teleport or full scripted motion is not.

---

## D. WORLD-PLACED TASKS: REMOVE CENTRAL CHORE SPAWNING

### D1 — House as the only task space

Every chore has an **authored physical context** and a room anchor (or authored set of valid variants) in the actual connected home. Implement `WorldTaskAnchor` / `RoomTaskSet` / `TaskPlacementValidator`, preserving all 72 canonical IDs and current game architecture. Do not replace the connected world with separate task simulation pods or generic chore tables. The task registry *activates* existing scene props or creates a believable prop at a vetted location, never in the player's central view by default.

**Examples of correct environmental setup:**

| Chore | Real starting location | Player's physical route and actions | Real destination |
|---|---|---|---|
| Wash plates | Dirty plates on dining table/side counter | Carry plate to existing sink; get sponge from sink holder; use tap/soap/scrub/rinse | Rack next to sink; cabinet when dry |
| Clean counter | Multiple hand-painted stains at breakfast/prep areas | Retrieve sponge or spray from cleaning cupboard, physically apply and wipe | Dry cleaned counter; return tools |
| Mop floor | Mud around foyer/kitchen, not a floating marker | Retrieve mop/bucket in utility room, fill it, carry it, mop contact patches | Wet/drying floor, emptied/returned tools |
| Do laundry | Clothes on bedroom chair/floor and real hamper | Sort, carry to washer, open door, load, add detergent, close door, turn real selector, start | Washer, dryer, drying rail or shelf |
| Tidy bookshelf | Books on sofa, side table, floor near shelf | Pick up and arrange, open cupboard/shelf if needed | Bookcase in the living room |
| Pet mess | Actual pet area or room event | Retrieve relevant tools, clean/contain mess in situ | Safe bin or cleaned pet floor |
| Prepare food | Fridge/cupboard/counter | Open door, grasp ingredients, pour/mix/assemble, return items | Serving plate/table |
| Bathroom cleanup | Smudges on real mirror and sink | Retrieve cloth/spray from cabinet, spray physical trigger, wipe | Clean mirror and returned tools |
| Toy cleanup | Toys scattered naturally in living room/bedroom | Physically reach, grasp, sort, roll, open chest lid | Toy chest/shelf |

**Task generator constraint:** inject only believable change of initial state (a few toys fallen, a used cup on table, a towel unfurled, soap spill near sink), then assign objectives and object IDs. Existing furniture and main equipment must persist in their room. Never spawn washer, sink, full cabinet, or a large pile of plates out of nowhere as the task begins. Spawning a missing *small* prop must happen at its valid scene location and not visibly materialize from nowhere. Preserve environmental state across task ownership, sabotage, repair, Mom inspection and end of match. Avoid duplicated props due to multiple task activations.

### D2 — Discovery, routing and UI

The player's wrist/house map indicates: **room**, objective and approximate location; the player uses their eyes to find the object. Optional subtle in-world highlights only when selected/nearby, never giant glowing circles around every item. The task list must not teleport props. Show gentle physical affordance glints on handles and useful hints in practice. Hide the old central task spawning/clipboard table from active gameplay. Preserve 5/10-minute balance by keeping the route purposeful and optimizing task lengths, not by cheating physics.

### D3 — Persist and replicate the physical world

Each meaningful prop has stable ID, room/world anchor, start pose/state, runtime pose, ownership, active task, wet/dirt/fill/assembled state and provenance. Cleaning task state changes occur where material and object exist in the shared world; no task table can silently reverse positions. On reconnect, clients receive object snapshot and authoritative event deltas. Bots use the same locations and predicates.

---

## E. ART & ASSET PIPELINE: HAND-PAINTED TOON, NOT RAW PACKS

### E1 — Audit all actual attachments *again*

I am reattaching model packs, 3D art, VFX, screenshots and references. Before editing scenes: list every attachment, ZIP, `.unitypackage`, FBX, OBJ, GLB/glTF, BLEND, texture, material, rig, animation, `LICENSE`, `README` and reference image; compare them against the existing project and previously imported sources; deduplicate. Some archives may have already been imported; don't reimport needlessly. The original master specifically listed:

- `Ultimate House Interior Pack - June 2020-20261109T080125Z-1-001.zip` — household architecture/furniture.
- `daycare-everyday-interiors-360-glb-v1.1.zip` — 3D everyday props with metadata; currently three models reportedly imported.
- `nashas_cozy_3d_kids_room_v1.0.zip` — children's room meshes; respect custom license.
- `NewLuaStudios_StylizedFoodPack.unitypackage` — Unity Asset Store terms; verify actual entitlement/contents.
- `KayKit_Restaurant_Bits_1.0_FREE (1).zip` — food/kitchen props.
- `household_laundry_utility_v1.0.zip` — washer/cleaning/utility props.
- `Tiny_Treats_Charming_Kitchen_1.1_FREE.zip` — kitchen furniture and stylized small props.
- `Classroom-VR-main(1).zip` — **technical code reference only**, not a redistributable art source.

Also audit actual installed `Functional Washing Machines - Stylized Low Poly`, `Cute Low Poly Kitchen Pack`, `House Interior - Free`, `Free Toon Water`, `Stylized Water Effect Pack`, `Bubbles VFX - URP`, `Simple FX - Cartoon Particles`, `Cartoon FX Remaster Free`, `VFX - Impact and Hit - Light Version`, `Stylized - Simple Hands`, Genies, Meta XR, Photon and any other supplied packages. **In My Assets is not equivalent to installed/in-project**; import only licensed, needed assets and test each pipeline. The user's newest attachment list overrides stale file expectations.

Never claim GLB cannot be used at all because Unity doesn't directly import it in this setup: use Blender staging conversion to FBX or an appropriate installed authorized importer, preserving materials, coordinate systems, hierarchy, animations and pivots. For `unitypackage`, use Unity's correct importer, not arbitrary decompression as source files without `.meta` relationships. No silently broken licenses or copying proprietary demo-game screenshots into shipped game content.

### E2 — Per-model completion contract

For EACH actually interactive model, produce:

1. Source provenance and license; source archive and original file, hash if available.
2. Correct mesh hierarchy with separately moveable parts (door, handle, knob, drawer, lid, lever, plunger, spout, wheels, rack, hose, tray etc.). Remodel in Blender if source is a single non-movable mesh.
3. Proper scale, origin, pivot, normals, UV mapping, mesh parts, real geometry thickness and LOD if needed.
4. Carefully authored **hand-painted albedo textures** (not only solid diffuse color), restrained roughness, baked AO/curvature where useful, style-consistent materials and real URP compatible shaders.
5. Stable colliders chosen to avoid penetration: primitive compounds / convex colliders, not dynamic nonconvex MeshCollider abuse; actual gaps in racks, cabinet clearance and sink basin shape.
6. Rigidbody/constraints/hinges/sockets/grip anchors with correct local axes, masses, damping, material friction, interaction clearance, reach envelope.
7. Gameplay component state and audio/VFX profiles; stable Photon IDs; bot contact anchors; sabotage/repair conditions.
8. Unity scene prefab, PlayMode validation and actual gameplay camera screenshots; source `.blend` and baked textures retained.

**Aesthetic test:** screenshot the object in the actual night-lit SHHH! room next to Genies hands and neighboring props. If it resembles a raw asset-store demonstration, plain gray block, incompatible PBR prop or flat colored plastic toy, it's unfinished. Give hand-painted variation to ceramic rims, wood grain, cloth weave, worn metal edges, dirt, glass and soap; keep attractive without grime overwhelming the family-safe style. Standardize palette/roughness across different packs.

### E3 — Existing VFX packs are inputs, not the product

Open each actually available package, inspect render pipeline, XR stereo compatibility, shaders, alpha overdraw, scale, licensing and Android build support. Rebuild/retone FX to match SHHH!: soft cartoon outline/highlight accents, shaped white foam, color-gradient water, painterly wet patches, subtle stylized droplets, warm impact stars/dust, no neon magic blasts. Make editable `.mat`, `.shadergraph`/HLSL, texture atlases, flipbooks and prefab variants where appropriate. If a pack is unusable, build a minimal original equivalent; don't just leave a default blue line.

---

## F. FLUIDS: VISUAL VOLUME + PHYSICAL STATE + TOON SHADERS

**The user specifically rejects the current thin LineRenderer-style faucet flow and unstyled liquid.** Create a dedicated `SHHH_ToonLiquid` family with authored materials and mechanical state. The liquid should look **spatially continuous, fluid, reflective in a stylized way, and physically meaningful**, not like a laser or a string.

### F1 — Real faucet mechanism

Model/rig the actual faucet handle with pivot, stops, grip area and rotary/tilt input. Map valve angle to continuous flow-rate (not just a bool). Support partial flow, shutoff, delayed drops and visible change in stream thickness. Use a proper sink collision volume and drain. Opening the tap creates continuous loop audio and logically meaningful noise only within sensible severity; faucet can be turned by a hand, including from a Genies avatar. No hidden interaction button that activates the tap from a distance.

### F2 — Spatial toon water-stream renderer

Use an **animated procedurally extruded ribbon/tube mesh, cross-sectioned 3D mesh, or optimized pooled fluid particle ribbon** with visible finite width and movement. Evaluate materials under **Quest URP stereo**, including:

- Rounded fluid silhouette, tapering/breakup over falling distance.
- Animated normal/vertex variation with coherent water flow downward.
- Hand-painted-looking blue/cyan-to-clear tint gradient, depth/edge opacity, soft Fresnel rim, broad shaped cartoon highlights.
- Small streaks/ripples, controlled white froth/foam at impact, local splash flecks and secondary droplets.
- Contact point follows real stream intersection with plate, hand, sponge, sink, container; do not keep streaming through solid props.
- Partial opening changes thickness and sound; zero flow removes stream and creates a few finishing drops.
- Avoid expensive screen-space refraction if mobile performance suffers; prefer environment/painted highlight and measured transparent shaders. No dependence on desktop-only features.

Build a shader/material comparison scene: existing line vs upgraded toon stream, with nighttime SHHH! lighting and Genies hands. Take before/after renders.

### F3 — Actual bounded liquid in containers

Every relevant cup, bowl, bucket, sink basin, detergent bottle or reservoir should have a persistent **amount** and an appropriately shaped visible liquid region. Use a per-container liquid mesh with plane clipping / generated inner-fill mesh or comparable safe method respecting container geometry. The free surface should remain approximately horizontal in world space under tilt; slosh/wobble may lag according to acceleration and angular velocity. Show volume dropping/rising and wall contact, not a static flat blue decal. When a vessel tips past its spout orientation and contains liquid, compute rate, stream origin, hit/receiver, missed volume and spill. Spills create persistent wet zones and possible cleanup events; bubbles/foam vary with contents. A bucket can slosh with accelerated movement and overfill; distinguish water from soap solution and other stylized contents.

**Important engineering boundary:** do not attempt to solve Navier–Stokes or render huge real-time particle volumes on Quest. Provide physically plausible, continuous, finite-volume state and responsive spatial rendering. Correctness means world causality and player interaction, not scientific fluid precision.

### F4 — Soap and foam

A soap bottle has a real graspable body, physically compressible/pumpable dispenser mechanism and a defined reservoir. A pump dose exists only when the plunger travels far enough **and** there is soap left; require the sponge/target under the nozzle to receive the dose; otherwise drops land on a real surface and may add sticky/slippery mess. Foam visual grows in contacted areas, migrates/rinses only with running water, bubbles pop and attach to surfaces rather than random particle spam. Stylize textures with soft opaque shapes and subtle color variations rather than stock white quads. A soap overflow incident must emerge from too many legitimate doses and require actual repair.

### F5 — Spray bottles, drops and puddles

Implement a hand-held spray bottle with trigger motion, measured orientation, finite fluid reservoir and short nozzle cone. Only impacted surfaces become wet/soapy. Create local sparse droplets/mist and a persistent wetness map; optional puddle edge mesh/decal with hand-painted edge irregularity, ripples and foam accumulating at plausible locations. Tools transfer wetness (dry cloth vs wet cloth, sponge squeeze returning water) and leave stylized contact traces. Make spray art attractive, not realistic toxic aerosols; this is safe family gameplay.

### F6 — Evidence and network truth

Liquid transfer is state: source ID, receiver ID, amount, wet region and action/event time. One authority validates physical reach/tilt/flow; other clients reconstruct stream/foam/wetness from concise amounts/coverage state. Do not network every particle, shader pixel or vertex. A water rendering effect can never alone mark a task as completed.

---

## G. STYLIZED DIRT, WASTE, WETNESS, CONTACT, AND REAL CLEANING

### G1 — Actual paintable surface maps

For every cleanable hero mesh, author UVs / local surface parametrization and a per-surface dirt/soap/wetness coverage mask. At actual collision/contact points, map tool tip to local UV or projected cells. Track brush/sponge tip path, contact duration, tangential velocity, surface alignment, wetness/soap preconditions and sensible motion strength. Update mask only in swept contact footprint; transparent/in-air hands never remove dirt. Use optimized small mask tiles/RenderTexture when practical; for simpler meshes use grid/cell mask. The visual transitions must be continuous/local with brush-width matching tool geometry. Preserve mask while player leaves and returns.

### G2 — Hand-painted grime and surface-shader family

Create stylized material/texture presets for:

- Crumb clusters and food residue with warm painterly edges.
- Dirty plates (food spots, greasy smears, suds, rinse trails).
- Countertop splashes, sticky rings, flour and tomato marks.
- Floor dust, mud, footprints, small puddles and soap spills.
- Bathroom mirror spots, soap scum, water marks.
- Fabric stains/dust and dirty vs clean laundry colorways.
- Pet-area clutter and safe stylized mess.

Use layered albedo/roughness/coverage with stamped hand-painted patterns, edge darkening, wet specular change and local transition, not a crude gray transparent overlay. No ugly decal panels or placeholder sprites. VFX accompanies real progress: soap froths where scrubbing, crumbs move under broom/mop, droplets appear where contacted, dust puffs only where dirt exists.

### G3 — Physical loose waste

Small debris should be clustered/poolable for Quest, but must have meaningful contact, push/sweep direction and containment. Sweeping pushes crumb clusters along the floor toward a dustpan; dustpan can be lifted and tilted into a bin; bin lid opens by physical hinge; waste remains contained and its clean state is verified. Real loose toys/books are rigidbodies with real shape/center of mass, not icons that disappear when touched. Cat/dog mess is family-safe stylized graphics and physical cleanup, not grotesque content.

### G4 — Tools that act like tools

A sponge can absorb water/soap, be squeezed, scrub using its broad face and become dirty. Cloth can be grasped at corners, wetted, wrung, folded and wiped. Mop has a two-hand grip and a real head that contacts floor; bucket carries a finite amount of fluid. Broom bristles have a forgiving physical contact region and directional sweep. Vacuum has a handle/nozzle, tube and suction region connected to actual moving head; audible motor can wake Mom. No task completion from generic trigger hits alone.

---

## H. DISHWASHING REBUILD: COMPLETE HERO EXPERIENCE IN THE ACTUAL KITCHEN

The previously implemented sink, lever, rack, soap dispenser and dishwashing rules are a good foundation, not the final standard. Rebuild as follows **in the actual connected SHHH! kitchen**:

**Natural starting scene:** Dining table has 1–3 actual dirty plates in believable positions; cleanable sink is part of the kitchen, dish soap sits beside it, sponge rests at caddy, rack has proper empty slots; faucet is part of sink. Nothing materializes in a central chore table. The player walks to dining area, physically picks up one plate and carries it to sink, avoiding collisions with furniture. The rack should be near the sink and reachable from both controller and tracked hands.

**Exact interaction loop:**

1. Grip plate edges/underside with stable hand pose. Plate has real mass, thickness, damped edge collisions, correct ceramic material and dirt map; cannot sink through the counter. Handle optional two-hand carry.
2. Carry to sink and set in real basin/counter area. The sink has basin geometry/collision and drain, not an empty open trigger zone.
3. Grip faucet lever and rotate/pull actual mechanical hinge; partial angle affects toon-volume stream rate; water physically intersects basin, plate or sponge with splash and sound.
4. Grip sponge; put it under running water or dip into basin; visible absorption/darkening/squish. Grip soap bottle and compress actual pump top while sponge is under nozzle; the dose transfers to sponge with visible soft foam.
5. Scrub a plate's actual individual stain regions with sponge contact motion; brown food disappears along its local path. Foam travels with scrub and fades when rinsed. A bare hand rub may smear grease but cannot fully clean a soap-required plate.
6. Hold plate under falling spatial toon stream. Water contacts its actual current orientation; soap washes away along runoff paths; finite rinse needs running water and proper surface exposure. Turning water off stops stream immediately and prevents continuing rinse. Plate visual changes dirty → foamy → clean based on actual coverage.
7. Lift drained, clean plate; physically guide between the rack dividers with actual collision geometry and controlled light assistance. It stands upright under gravity with stable support. Releasing at the wrong angle can produce a real stumble that may require recovery; avoid huge constraints/441-sound jitter. Support two different rack slots without collider fights; prove at least 3 sequential plates can be inserted and removed.
8. Quietly close faucet by rotating lever back. End only if all required plates are actually clean/racked and tap is off. Show subtle completion without artificial prop disappearance.
9. Sabotage: opponent can physically move rack brace, tip plate, hide soap/sponge, turn faucet back on, move a cleaned plate; game records world truth and fair Mom evidence. Recover by actually finding/resetting object and repeating only invalidated work.

**PlayMode acceptance:** demonstrate in uninterrupted sequence with actual camera captures: natural table start → grip → travel → tap → soap → local scrubbing → local rinse → rack 1 and 2 and 3 → tap off → score. Show before/after images of water style, foam on plate, dirt mask, plate geometry and 441-clatter sound problem. Test wrong action, forceful drop, tracking loss, empty soap reservoir, dirty sponge, off-tap rinse, plate falls, sabotaged rack, bot, remote client. Full controller/hand test on real Quest pending until actually connected; label tests honestly.

---

## I. ALL 72 TASKS: REAL-WORLD SIMULATION, NOT 72 BUTTON RECIPES

**Canonical IDs and names from the 09 Nov authoritative file remain exact:**
- Living: `LV01–LV14` (14).
- Kitchen/dining: `KT01–KT16` (16).
- Laundry/utility: `LD01–LD10` (10).
- Bedrooms: `BR01–BR08` (8).
- Bathroom: `BT01–BT06` (6).
- Foyer: `FY01–FY06` (6).
- Pets: `PT01–PT06` (6).
- Hallways/stairs: `HW01–HW06` (6).

Do not rename/reset stable IDs. Keep actual current recipe definitions and remap step interactions to live props, mechanics and anchors. Every recipe should have a recognizable multi-action physical story and sabotage/recovery route. If a recipe currently uses a button press, `ClickToComplete`, one-step grab-to-socket or hand-waved gesture to simulate a multi-stage household operation, mark it **NOT PRODUCTION COMPLETE** and replace it.

### I1 — Minimum sample physical stories to extend to all categories

**Laundry washer:** shirts are actually scattered in bedroom/hampers. Player physically grips and transports them; sorts by tag/color; opens laundry room door/cupboard with a graspable handle; washer door rotates on true hinge and has an appropriate latch; reach through door opening; insert clothing bundle into realistic drum volume; take detergent from shelf, tilt/pour measured dose into tray; close drawer, shut door, select cycle via graspable dial, start using a natural real switch; washing action visibly tumbles clothes/water with smart low-cost animation/physics. The door must not be openable while running. The player can remove/fold/hang using two-handed fabric mesh rigs and realistic cloth-like reaction where practical. No arbitrary instant laundry spawn at table.

**Towel folding:** both hands grasp separate corners of actual cloth; cloth follows motion with skinned/bone or constrained mesh response, drapes when lowered, reveals approximate crease and folds in multiple meaningful stages; final tidy pose only after real corner alignment/contact. Not a single preset `Fold` gesture at an invisible marker.

**Mopping:** mop is stored on wall hook; grip handle with two hands, dip into water bucket, lift with wet drips and swaying head, physically stroke floor with proper head-ground contact, local muddy areas fade in actual traveled footprint, floor becomes wet with toon patches/footprints, and player may need to wring mop or empty bucket. No floor dirt progress by tapping a virtual button.

**Spray/window:** grasp bottle body, squeeze actual physical trigger repeatedly, visible stylized droplet cone hits exact glass/wood surface; wipe with cloth along contacted area; clean mask fades locally. Window glass still looks like glass in toon night environment; no enormous decal rectangle covering it.

**Cooking/food:** open fridge/cabinet by grabbing handles; collect actual ingredients; pour water/sauce from finite-volume vessels, stir utensil in bowl with collision, mix food using state-driven stylized material; place assembled ingredients on counter/plate. Heating can be safe/cartoon; no graphic hazards; physics objects should not simply snap/spawn from menu. Use the supplied food pack where licensed and visually converted.

**Room tidying:** lift physical book at an angle, stabilize and align on shelf; drawer must physically open; toy cars roll with actual wheel/inertia; cushions compress/squash and move; boxes have lids and contents; stack weight can topple; rug can be dragged and flattened, not reset from button.

**Trash:** trash/small debris naturally appears around relevant home surfaces, broom/vacuum moves/collects it, bins open and waste lands in physical receiver; trash visually hand-painted and mechanically meaningful, not flat gray sprites.

**Pet chores:** bowls and toys sit in pet area; actual scoop/bowl fill/transport/physical return, dog/cat may bump object; all events use real task/motion provenance.

### I2 — Twenty-six primitives are required, not just phrases in CSV

Rebuild/verify these exact primitive capabilities as **runtime physical interactions**: scrubbing, wiping, sweeping, pouring, squeezing, twisting, pulling, pushing, pumping, shaking, stirring, slicing (safe cartoon), rolling, folding, stretching, stacking/balancing, catching, throwing/tossing, vacuuming, sliding, hinging, connecting, plug/unplug (safe stylized), pendulum, spring compression, two-handed wringing.

For each of 26: one standalone mechanical demo scene/prefab and at least one integrated in-house recipe, direct hand-control tests, realistic dynamic state response, cosmetic/audio feedback, sabotage/repair case and recorded evidence. Verify hand mode and controller mode; don't allow pseudo gestures where only a command changes the state. For cloth or liquids, documented physically responsive approximations count where actually driven by motion/contact rather than pure timers.

### I3 — Twelve comedy incidents remain required

Preserve and physically integrate: falling plate dominoes; rolling ball downstairs; wobbly stool; squeaky floorboard; vacuum hose snag; overflowing/sloshing bucket; rotating chair; spring drawer; swinging lamp; Biscuit/Mochi toy chase; unstable book tower; soap overflow. Real collision/constraint/evidence and recovery; not just particle-triggered incident cards. Actual clatter/rattle from meaningful impulses influences Mom, with suppression for repetitive resting contacts.

---

## J. BOT, GENIES, MOM, MULTIPLAYER — ALL MUST TOUCH THE SAME WORLD

1. **Genies local and remote:** keep already passing editor avatar look creation, equip/save/load and Photon appearance sync. Implement correct IK/pose of human hand on each object (sponge, tray, towel, bucket, door handle) and remote hand position; no T-poses, camera-face clipping, or phantom hands. Avatar art may be full connected humanoid or disconnected stylized body; user no longer insists on disjointed visible anatomy.
2. **Bots:** navmesh into real rooms, arrive at prop within reach, animate grasp at actual anchor, manipulate through legal same state transitions, move mesh/object with network authoritative transform, finish tasks without instant hidden `SetComplete`, repair sabotage and react to real blocking. Bot-contact simulation is an acceptable *input source* but needs physical location and contact proof, not an entirely separate fake scoring route.
3. **Photon:** authoritative physical object ownership and causal events (grabs/releases, lever angle bands, fluid volumes, occupancy, clean masks, latch, impacts, task/sabotage state) with locally reconstructed expensive VFX. Prevent two players owning one rack/plate simultaneously. Verify two-player dishwashing: one scrubs while other moves/contests another prop, both see matching fill/soap/clean/rack; 0 state mismatches is the goal, not a pre-reported fact for this task.
4. **Mom:** real acoustic event from impulse/velocity/material, running water at a sensible ongoing level, high-noise incident with contact provenance; Mom awareness only hears credible sound and witnesses, not direct access to hidden truth. Noise aggregator must avoid duplicate spam to audio and suspicion. 441 rattles should not cause 441 distinct Mom observations. Interrogation and fair responsibility still needed.
5. **Scene:** in-world task changes persist for players, bots, Mom inspection and reconnecting clients. World anchor coordinates and map icons from actual object/room positions, not a central origin or obsolete round table.

---

## K. QA, QUALITY BAR, EVIDENCE, PERFORMANCE

### K1 — Evidence hierarchy

Distinguish and report separately:
1. Pure C# logic tests.
2. Unity EditMode/prefab validation.
3. Unity PlayMode with scripted/simulated hands.
4. Unity PlayMode with **actual tracked controller input** if available.
5. Multiple independent network clients.
6. Android APK build with hash and exact contents.
7. **Actual Quest 3/Quest 2 on-device hand/controller, shader, stereo and frame-time verification.**

No mixing categories or calling desktop physics simulations headset tests. If a device is not connected, document blocker and leave evidence as editor/standalone.

### K2 — Five production hero scenes, then full 72 coverage

Mandatory integrated hero scene slices in the real connected house: (1) full dishwashing, (2) water/mopping/spray surface cleaning, (3) laundry open/load/wash/transfer/fold, (4) cooking/mixing/pouring/cabinet/fridge, (5) room clutter/tray/stacking/sabotage. Each must visually pass art quality and mechanically pass human input + bot + authoritative network state where available. Then expand the same components across the remaining tasks, one category at a time, preserving exact ID evidence.

**Per-task acceptance matrix:** natural in-world location; art quality; authentic mesh hierarchy; actual hand grip; constrained mechanism; contact/collision; appropriate material/physics response; persistent local VFX/wet/dirt state; meaningful sound; right-to-wrong action behavior; completion predicate; sabotage/repair; bot execution; network sync; Quest test status. Mark **NOT COMPLETE** if any mandatory runtime category absent. Do not mark 72 complete simply because 72 definitions exist in CSV.

### K3 — High quality visual gate

Capture actual scene screenshots of: water flowing 0%, ~50%, 100% valve open; water hitting plate and falling into basin; container 25%/75% fill and tilted; soap droplets vs foam; scrub-local before/half/after; wet patch vs dry patch; a first- and second-plate rack physical settle; washer door open/partially open/closed; real towel folded in two poses; drawer 25/75% travel; whole-room view with no central task table. Compare with attached **reference-only** images; do not copy them into the commercial game.

### K4 — Quest optimization contract

Unity 6 URP 17 and actual installed XR/Android setup control implementation. Prefer small pooled effects, bounded simulation objects, low-overdraw stylized transparent shaders, baked/painted surface detail, physics contacts limited to interactables, non-allocating collision callbacks where possible, stable `FixedUpdate`, LOD/atlasing, small surface masks, reduced draw calls. A 72 fps target means measuring on actual headset; desktop framerate is insufficient. Profile multi-client avatars, 20+ household props, water at sink, Mom, pets and VFX together before claiming Quest ready. No expensive mobile-incompatible compute-based fluid solver or volumetric raymarch across rooms unless measured feasible.

### K5 — Fix, don't cosmetically suppress failures

For the second-plate rack issue inspect object dimensions, actual rack gap, collision mesh, penetration, center of mass, friction, solver iteration/step, contact threshold and assist geometry. The 441 clatters require physics and SFX policy fixes. Test run must prove peaceful settle and deliberate noisy drop as different outcomes. For each future interaction, fix root cause rather than making triggers huge, disabling collisions, silencing noise universally or teleports disguised as animated moves.

---

## L. DURABLE MEMORY / RESUMPTION / AUTONOMOUS EXECUTION

The user explicitly says **remember what I said**. Do not merely reply “I remember.” Write the exact V10 owner requirements into the real project source-of-truth files and verify they were saved and synchronized. `CLAUDE.md` should say READ FIRST and reference the authoritative owner changes; `.claude/rules/shhh-physics-production.md` should list no-table/no-poke/no-line-water/no-unpainted-assets/no-fake-physics requirements; `Docs/SHHH_OWNER_REQUIREMENTS.md` should include date/precedence; `Docs/SHHH_PHYSICS_CONTRACT.md` should enumerate 26/72/12 and realistic mechanical goals; `Docs/SHHH_ASSET_IMPORT_MANIFEST.csv` tracks actual imported assets and edits; `Docs/SHHH_V10_RECOVERY_STATE.md` tracks exact latest SHA/commit and next action. Keep existing QA evidence indexed. Saving a note to claude.ai Project is useful but cannot replace tracked project docs.

When session interrupted, next operator reads files and resumes exact phase. No promises about invisible background work; actually perform changes during active tool access, and never invent success on untested Quest hardware.

### Implementation sequence — don't stop at planning

0. Backup/checkpoint; verify V9 patch/current Genies/Photon; inspect new attachments and shaders; update durable V10 owner rules.
1. Fix rack slot 2 and 441 clatters, validate the working existing dishwashing logic; freeze a stable fallback.
2. Replace fake physics grab/control patterns with mechanical grips/joints for faucet, pump, plates, rack, cabinet/drawers; ensure input matches physical geometry.
3. Replace thin line water with convincing **toon volumetric-looking stream and finite-volume containers**, water contact/splash/foam/wetness, and artist-authored URP materials.
4. Replace generic stains/trash with hand-painted grime/dirt/waste shaders and actual local contact-cleaning; integrate step-by-step dishwashing within natural table-to-sink-to-rack spatial journey; remove centralized task spawning.
5. Build laundry and mopping with real doors, drawers, appliance interaction, cloth grip/deform and fluid-state responses; then cooking/toys/trays/room clutter.
6. Integrate all 26 reusable primitives, 12 incidents and 72 exact in-house recipes with real environmental prop placements and sabotage/repair.
7. Complete bot, Mom and Photon authority tests for all new mechanics, while preserving Genies and continuous match.
8. Render/QA/profiling/build updated Quest APK; label actual on-headset status truthfully.

**Do not use one session to produce a list of future plans instead of implementing.** Work on code, prefabs, Blender sources, materials/shaders, real house scene, gameplay tests, source-controlled progress and evidence. If blocked by user permissions or hardware, record precise step and continue independent work.

### Definition of done

I put on a Quest, enter a fully connected hand-painted cartoon family house as a Genies character, see real naturally located messes and household objects, reach with tracked hands, grasp a real faucet lever, twist it to different flow levels, see physically substantial stylized toon water occupy and splash into real vessels, wash dishes by actually contacting stains, feel soap and foam respond, place plates into a stable real rack, and hear Mom react to genuinely causal noises. I can repeat this realism across laundry, mopping, toys, food and the other 72 chores. Other players and bots interact with the SAME world and share its state. Nothing crucial is accomplished by tapping a fake completion button or collecting props from an artificial task table.

**Execute the V10 correction NOW, starting from the actual project and latest committed state.**

---

## M. REFERENCE LINKS — REFERENCE ONLY; DO NOT SHIP THEIR ART/BRANDING

The user wants visual references attached again. These URLs are used **only to understand interaction/art quality**; source images remain owned by their publishers/artists. See companion `SHHH_V10_VISUAL_REFERENCES.html` for a clickable visual board. Before implementation compare the attached reference pack with actual original SHHH art and prefer the owner-supplied assets.

**Official technical references:**
- Meta hand grab/transformers: https://developers.meta.com/vr/documentation/unity/unity-isdk-hand-grab-interaction/
- Meta constrained levers/drawers: https://developers.meta.com/vr/documentation/unity/unity-isdk-constrained-grab-interactions/
- Unity XR URP compatibility: https://docs.unity.com/en-us/engine/6000.7/manual/xr/graphics/render-pipeline-compatibility

**Artist/game visual references (not assets):**
1. BineBirk Tiny Kitchen, handpainted environment: https://www.binebirk.com/portfolio3d/tiny-kitchen
2. Unity stylized water shader discussed by 80 Level: https://80.lv/articles/stylized-water-shader-for-unity-a16
3. VR Essentials Pack physical drawer interaction image: https://www.fab.com/zh-cn/listings/1a148349-55dd-4c3c-b2f5-713dbf166f91
4. House Flipper 2 in-room debris and cleaning: https://mezha.ua/en/news/house-flipper-2-fix-it/
5. PowerWash Simulator local surface cleaning: https://www.gamesradar.com/powerwash-simulator-is-helping-me-unwind-one-clean-surface-at-a-time/
6. Vacation Simulator playful VR tabletop interactions: https://store.playstation.com/en-us/concept/232369
7. Meta IKEA VR kitchen early interactive drawers: https://www.emerce.nl/nieuws/ikea-start-proef-vr-keuken

**The reference images and web pages may show other titles, brands or older graphics. Copy none of those creative elements; extract principles of material rendering and interaction only.**
