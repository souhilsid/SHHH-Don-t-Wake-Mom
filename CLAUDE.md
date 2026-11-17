# SHHH! — Don't Wake Mom!  (agent instructions / resume point)

## READ FIRST (V12, 2026-11-10) — assets, shared mechanisms, laundry, pouring (latest owner order)
Spec: `Docs/Production/SHHH_V12_INTERACTION_DESIGN_26_MECHANICS_12_INCIDENTS_2026-11-10.md`. Precedence **V12 > V11 > V10 > V9 > V8 > Nov 9**.
Owner considers plate washing done. Order: (1) import Tiny Treats / laundry / Ultimate House packs (DONE: catalog 570) -> (2) shared
interactions (handles, drawers, ...) -> (3) laundry and the other chores -> (4) hand-held pouring + water levels shared with other players.
Style: stylized hand-painted cartoon ("Fortnite-like" chunky, readable). Implemented so far: `Scripts/Game/World/Mechanisms/` (MechPart base,
HingedPart, SlidingPart, RotaryKnob, MechButton, ScrewCap, WasherAppliance, Vessels/VesselSync, SoftCloth, FabricLook, MechHaptics).
Rules: washer door locked while "start" is set; start refused with the door open; `Verb.Pour` + `RProp.Ml` replicate vessel amounts.
V12 2026-11-11: every player is a full-body Genies avatar (TrackedGeniesBody: head + hands -> humanoid IK; my own body with hidden head,
gloves as hands; friends from replicated poses; `recipe_diag "avatars!"`, `net_multi "2;windowed"` because Genies does not run in batch
players) and chore items start left all over the house (HouseClutter.ScatterChoreProps, PropSpec.StartHint -> task HUD). Classroom-VR
was concept reference only. PolyOne dog/cat (Asset Store) are the pets (PetAnimator).
Owner feedback 2026-11-10: the washer must face the room (fixed: front faces the laundry-room centre) and clothes must look like real soft
clothes (fixed: Blender cloth-sim garments `Tools/blender/make_clothes.py` + SoftCloth). QA: `recipe_diag "washer!"` (desktop only).

## READ FIRST (V11, 2026-11-10) — PHYSICAL INTERACTION ENGINE FIRST (supersedes V10 ordering)
Directive: `Docs/Production/SHHH_V11_PHYSICAL_INTERACTION_ENGINE_DIRECTIVE_2026-11-10.md`. Precedence **V11 > V10 > V9 > V8 > Nov 9**.
- Not ready for laundry. Order: (1) finish dishwashing completely (selection of close objects, stable grip, no furniture snagging, both plates in the
  rack, no duplicated impact noise, correct orientation, FACE-ON before/during/after stain captures) -> (2) rebuild water as gameplay-linked
  reservoirs/fill/pour/splash/puddles/foam with an original toon look -> (3) shared physical interaction foundation (handles, drawers, doors, knobs,
  hinges, one/two-hand carry, pour, squeeze, scrub/wipe/sweep/mop/spray at contact, fabric, stack/balance/catch/recover, appliances by moving parts)
  -> (4) all tasks at real house locations (no RecipeLayout table staging in gameplay) -> (5) real assets upgraded to the hand-painted style ->
  then laundry and the rest.
- No teleporting, no removing collisions, no silently disabled physics, no VFX-disguised triggers. Report rules / Editor / multi-instance Photon /
  APK / Quest separately.


## READ FIRST (V10, 2026-11-10) — HIGHEST-PRIORITY OWNER OVERRIDE: real house physics
Directive: `Docs/Production/SHHH_V10_REAL_HOUSE_PHYSICS_ART_INTERACTIONS_DIRECTIVE_2026-11-10.md` (+ reference board `SHHH_V10_VISUAL_REFERENCES.md`, reference-only images).
Resume point: **`Docs/SHHH_V10_RECOVERY_STATE.md`** (exact phase, evidence, blocker, next command). Rules: `.claude/rules/shhh-physics-production.md` (V10 block on top).
Precedence: **V10 > V9 > V8 Genies > Nov 9 master** where they conflict. The owner's decisions:
- No poke-button gameplay, no basic grab-and-place chores. Every household object gets the physical interaction of its real function
  (grasp handles, pull drawers on rails, rotate faucets, operate appliances, open doors, carry containers, pour finite liquids, scrub individual stains,
  manipulate cloth, stack objects, real collisions). A real push-button only does what that button really does.
- **NO central task-spawning table.** Tasks live where they belong in the connected house (dishes on the dining table/sink, laundry in bedrooms/hampers,
  toys in rooms, supplies in storage). Players travel, collect, carry, operate, clean and return. Task UI is information, never an object spawner.
- Water must never look like a thin line: stylized toon stream with real width/taper/foam, finite volumes in containers, splashes, droplets, puddles, wetness.
- All interactive models, stains, waste, food, laundry and VFX are hand-painted Soft-Sculpted Storybook Toon; imported packs are inputs, not finished art.
- Apply to all 26 primitives, 72 recipes (IDs LV01–HW06 unchanged), 12 comedy incidents, with sabotage, recovery, audio, haptics, Genies hands, Photon sync, Mom causality.
- Order: fix rack slot 2 + 441-clatter -> dishwashing hero slice (real kitchen, toon water) as the tested foundation -> other mechanics.
- Keep Genies + Photon working; never restart; never call Editor/desktop runs Quest tests (Quest = UNTESTED until performed).

**Workflow note (V10):** this PC copy has NO git and the old cloud mirror (`/home/claude/SHHH`) is not reachable from a PC-local session. Edit the PC tree
directly, snapshot first (`Backups/V10_*/*.tgz` + sha256 in the recovery state), compile through the Unity bridge (`bash Tools/cycle.sh - [cmd] [args]`).
- Rules tests on the PC (no .NET SDK installed): `bash Tools/core_tests.sh` (Unity's bundled .NET 6 runtime + Roslyn; same Core files Unity compiles).
- **Never write files under `UnityProject/Assets` while a PlayMode capture runs** (`recipe_diag`, `genies_journey`, ...): the Editor auto-refresh
  recompiles and the domain reload silently restarts play mode (the run is lost). Write Docs/Tools only, or wait for `"playing": false`.
- Python on this PC is `python` (no `python3`); `Tools/cycle.sh` picks whichever exists.
- `recipe_diag "look:Kitchen|name;ex;ey;ez;tx;ty;tz|..."` captures room-local views (QA_Evidence/recipe_v7/look_*.png) + colliders at each target.


## READ FIRST (V9, 2026-11-10) — durable owner requirements
1. `Docs/SHHH_OWNER_REQUIREMENTS.md` — every owner decision in force (Genies primary, continuous 5/10-min match, physics first, real assets, original audio, honesty rules).
2. `.claude/rules/shhh-physics-production.md` — non-negotiable production rules (loaded every session).
3. `Docs/SHHH_V8_Continuation_State.md` — exact resume point: last commit/sync pack, build, evidence, blocker, NEXT COMMAND.
4. `Docs/SHHH_PHYSICS_CONTRACT.md` (26 primitives, state contracts, status) and `Docs/SHHH_PHYSICS_TASK_COVERAGE.csv` (all 72 recipe IDs).
5. Latest directives: `Docs/Production/SHHH_V9_PHYSICS_ASSETS_VFX_MASTER_CONTINUATION_2026-11-10.md` > `Docs/Production/SHHH_V8_GENIES_PRIMARY_CONTINUATION_2026-11-09.md` > `Docs/Production/SHHH_CLAUDE_ONE_SHOT_COMPLETE_MASTER_PROMPT_2026-11-09.md`.
Precedence: latest explicit owner instruction > V9 physics directive > V8 Genies directive > Nov 9 master prompt (where compatible) > verified live source > older docs.

**Status words are distinct and never conflated:** IMPLEMENTED IN SOURCE · RULES TEST PASSED · UNITY PLAYMODE VERIFIED (desktop, simulated hands) · TWO-CLIENT VERIFIED · APK BUILT · REAL QUEST VERIFIED. No Quest test exists for any V8/V9 feature.

**Sync loop (cloud mirror -> PC):** edit in `/home/claude/SHHH` (git) -> `bash /home/claude/sync_pack.sh <name>` -> `device_commit_files` to `SHHH\_sync\<name>.tgz` -> on the PC `bash Tools/cycle.sh <name>.tgz [command] [args]`. PC project root: `C:\Users\aisci\Downloads\SHHH Game\SHHH` (device_bash: `$HOME/mnt/SHHH Game/SHHH`). Keep the Unity Editor window NOT minimized during capture runs (Vulkan renders black; `ShhhKeepVisible` restores it).

---

Older context (superseded where V8/V9 differ): `Docs/PRODUCTION_STATE.md`, `Docs/Production/SHHH_CLAUDE_MASTER_PRODUCTION_PROMPT_FINAL.md`.
V6 notes (still valid for tested systems): `Docs/CONTINUATION_V6.md`.
V5 notes: read `Docs/IMPLEMENTATION_STATUS.md`, `Docs/ISSUE_TRACKER.md`, `Docs/DECISION_LOG.md` first.
Read also: `Docs/PROGRESS.md` (what is done & verified), `Docs/BLOCKERS.md` (what needs the user), then `Docs/GAME_DESIGN_V4.md`.
Authority order: `SHHH_DESIGN_OVERRIDES_V4.md` > master prompt > `Docs/GAME_DESIGN_V4.md` > legacy v3 lock (DesignSources/References).

## Layout
- `UnityProject/` Unity 6000.0.62f1 (URP, OpenXR 1.16.1, Meta XR Core SDK 201). Android/Quest target.
  - `Assets/SHHH/Scripts/Core` — engine-free authoritative rules (asmdef `SHHH.Core`, noEngineReferences). Single source for the game rules.
  - `Assets/SHHH/Scripts/Game` — Unity layer (XR hands, props, anchors, bots, pets, Mom, diegetic UI, GameRoot flow).
  - `Assets/SHHH/Editor` — automation: bridge, asset pipeline, scene builder, build, play tests, diagnostics.
- `Core/SHHH.Core.Tests` — dotnet console test runner compiling the SAME Core files (`dotnet run` → 33 tests incl. 200 simulated matches).
- `Tools/CompileCheck` — dotnet project that type-checks all Unity C# against the Editor's exported DLLs (`Tools/UnityRefs`, via `export_engine_refs`).
- `Tools/job.sh <cmd> [args] [timeout]` — drop a job into `Automation/inbox`; the open Unity Editor executes it (only `[ShhhCommand]` methods) and writes `Automation/outbox/<id>.json`. `Tools/wait_ready.sh` waits for compile idle.
- `Builds/Quest2_3_3S/` — APKs + `BUILDS.csv` (sha256). `Secrets/` (never committed) holds release signing.

## Unity automation commands (see `list_commands`)
refresh, import_tmp, setup_layers, build_font, reimport_art, build_catalog, configure_project, build_scenes, ensure_build_scenes,
build_apk_dev, build_apk_release, playtest_autopilot, playtest_stop, debug_room, material_audit, screenshot_game, camera_to,
inspect_models, log_grep ("cur|prev;pattern;max"), log_range ("line;count"), editor_state, editor_log_tail, export_engine_refs,
import_package <repo-relative .unitypackage>, photon_status, photon_configure (App IDs from Secrets/photon.json), net_setup, net_bake, net_smoke [multipeer], net_smoke_stop, recompile_clean.

## Workflow rules learned the hard way
- The user never operates Unity. Unity runs as a normal Editor window driven by the file bridge; `metavr unity projects open` was unreliable (path prefix) — Hub opened it.
- Editor play tests: XR init on Standalone is disabled (PC Link runtime ate memory); `PlayerSettings.runInBackground = true`; QAReporter aborts above 4 GB and auto-stops after the winner.
- Imported FBX materials arrive as built-in Standard → `Mats.MakeUrpSafe` converts at runtime (+ import rule).
- Moving a physics prop: set `Rigidbody.position` as well as the transform (interpolation overrides transform writes).
- Quaternius women rig is not Humanoid-mappable (IK feet) → Generic (Mom). Siblings use Quaternius base humanoid + `ProceduralHumanoid`.
- Keep each MonoBehaviour in its own file named after the class.
- Commit messages end with the attribution lines from the session instructions.
- Fusion: every NetworkBehaviour in its own file named after the class (otherwise prefabs lose the script reference -> "not baked").
- Fusion: `Meta.XR.MultiplayerBlocks.Fusion` must be in AssembliesToWeave + `recompile_clean`, or runner start throws in NetworkTypesMeta.
- Fusion multi-peer tests: runners DontDestroyOnLoad + additive scene loads; net_smoke_stop restores PeerMode=Single (must be Single for APK builds).
- Don't change assets while an APK build runs (a reimport cancelled IL2CPP once).
- GLB assets: Unity has no glTF importer here; convert with `python3.13 Tools/glb2fbx.py <srcdir> <outdir>` (Blender 5.2 bpy in the cloud container) — keeps PART_* pivots, COL_* collision boxes, FRONT/SOCKET_* empties. Put FBX in `Assets/SHHH/Art/ThirdParty/<Pack>/` and run `build_catalog` (keys: dc_ = Daycare, tt_ = Tiny Treats, kr_ = KayKit Restaurant ...). `ArticulatedModel.Build` uses them at runtime.
- Measure real furniture before anchoring gameplay to it: `recipe_diag "probe:<Room>;x0;x1;z0;z1"` returns a room-local height map.
- `recipe_diag "<ID>!"` = desktop PlayMode run of one recipe by simulated hands with a screenshot per action (QA_Evidence/recipe_v7/).
- Open the project via Unity Hub (computer use, Hub + taskbar granted); `metavr unity projects open` passes a \\?\ path that Unity ignores.
