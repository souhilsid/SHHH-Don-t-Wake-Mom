# SHHH! V11 — Complete physical interaction engine and real household gameplay (owner directive, 2026-11-10)

Precedence: **V11 > V10 > V9 > V8 Genies > Nov 9 master** where they conflict. Saved verbatim in intent from the owner's chat message; reference
images attached in chat (messy sink with spray hose, mop/bucket with paint-splat floor mess, cluttered cozy room, colorful laundry room) are
REFERENCE ONLY - extract the feel (lots of believable mess in place, chunky readable props, bold splats), copy nothing.

The owner: "My highest priority is no longer adding more task-completion logic. It is achieving high-quality, convincing, physically responsive
gameplay." "We are not ready to move directly to basic laundry tasks."

1. **Finish dishwashing completely.** Fix: wrong plate selected when two objects are close; unstable gripping / objects slipping while carried;
   plates colliding excessively with furniture; both plates fitting naturally in the drying rack; excessive / duplicated impact sounds; incorrect
   plate orientation during manipulation; unverified stain appearance. Use stable physical grabbing, contact-aware constraints, meaningful hand
   poses, correct mass and collision handling, grip transitions. NO teleporting plates, NO removing collisions, NO silently disabling physics.
   Produce **face-on before / during / after captures proving dirt disappears under sponge contact.**
2. **Rebuild the water system.** Beautiful stylized water with an original SHHH! toon shader; gameplay-linked liquid reservoirs, visible water
   volume, animated flow, fill levels, pouring, splash response, droplets, wetness, puddles, interactive foam; responds to faucet position,
   container orientation, contact and available volume. Quest-compatible approximations, no CFD. Hand-painted, consistent with the art style.
3. **Production-grade physical interaction foundation**: grasp handles/objects; pull/push doors and drawers; rotate faucets, knobs, appliance
   controls; open/close hinges; carry/stabilise with one or two hands; pour/transfer liquids; compress/squeeze sponges; scrub, wipe, sweep, mop,
   spray at precise contact locations; manipulate fabrics; stack, balance, catch, drop, recover; operate appliances through their real moving parts.
   No poke-button substitutes (real buttons only where the real object has buttons). Best compatible Unity/Meta XR components, rigidbodies,
   configurable joints, grip constraints, animation, IK, collision systems.
4. **Remove central task staging** (RecipeLayout station tables) from active gameplay; keep recipe logic + stable IDs; every task's objects at
   real household locations with valid navigation and recovery (dishes at dining table/counters; sponge + detergent by the sink; laundry in
   baskets, bedrooms, laundry room). Players travel, find, perform, return/store.
5. **Import and upgrade the real assets** (archives re-attached 2026-11-10: Ultimate House Interior Pack, daycare-everyday-interiors-360-glb,
   nashas_cozy_3d_kids_room, NewLuaStudios_StylizedFoodPack.unitypackage, KayKit Restaurant Bits, household_laundry_utility, Tiny Treats Charming
   Kitchen). Inspect meshes, textures, materials, moving parts, pivots, licenses; import; adapt to SHHH! soft-sculpted hand-painted cartoon style;
   UVs, materials, colliders, mechanisms, rigs/animation; real meshes not primitives; toon water/foam/dirt/wetness shaders, transitions, particles,
   sound, haptics.
6. **Preserve everything**: 26 primitives, 72 recipes, 12 comedy incidents; Genies, Photon, Mom evidence, bots, sabotage, repair, scoring,
   uninterrupted 5/10-minute match. Do not restart.
7. **Validate before expanding**: complete dishwashing in Unity Editor runtime (both plates correctly in the rack, faucet off), then the shared
   physical systems, then laundry and the rest. Keep requirements in CLAUDE.md, owner requirements, physics contract, QA evidence, continuation
   state. Report separately: rules / Unity Editor / multi-instance Photon / APK / real Quest.

Desired experience: "a genuinely interactive cartoon house where physical movements, objects, mechanisms, liquids, fabrics and collisions behave
naturally. I do not want simplified task triggers disguised with VFX."
