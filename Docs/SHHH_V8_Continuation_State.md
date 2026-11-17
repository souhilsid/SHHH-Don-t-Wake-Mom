# >>> V10 SUPERSEDES THIS FILE AS THE RESUME POINT: read `Docs/SHHH_V10_RECOVERY_STATE.md` first (2026-11-10).

# >>> RESUME HERE (V9, 2026-11-10 13:30 UTC+3)

- Directive in force: `Docs/Production/SHHH_V9_PHYSICS_ASSETS_VFX_MASTER_CONTINUATION_2026-11-10.md` (+ `Docs/SHHH_OWNER_REQUIREMENTS.md`).
- Last cloud commit: see `git log -1` in `/home/claude/SHHH` (mirror) — last sync pack shipped AND applied on the PC: **v10g**. All packs v9m…v10g are applied (v9m verified byte-identical).
- Latest APK: still **c12** (`SHHH_alpha_0.6.3_c12.apk`, sha256 14f30ab8…4de0) — has none of the V8 Genies / V9 physics work. Do not overwrite it.
- Evidence: `Docs/SHHH_PHYSICS_QA_EVIDENCE_INDEX.md` (PlayMode desktop sim only; no headset).
- Current blocker: KT01 rack step — plate 1 seats upright in the KIT018 rack, plate 2 (gap 2, +0.0425 m) arrives on edge but never seats and
  chatters on the dividers (441 ceramic contact sounds in one run → also fix resting-contact audio spam in `PropBody` impacts). Check the gap
  positions against the scaled divider colliders (dividers every 0.0425 m; slots at ±0.0425), widen the snap capture for upright racks, and
  move plate spawns back from the table's front edge (one run lost a plate off the edge).
- NEXT COMMAND (PC, after the fix): `bash Tools/cycle.sh <pack>.tgz recipe_diag "KT01!"`, then inspect `QA_Evidence/recipe_v7/KT01_op*.png`.
- Then: per-cell dirt masks (scrubbing #1) → laundry flagship (LD04 washer with real hinge, LD08) → floor cleaning (KT13/BT03/HW02: bucket volume,
  mop wet trail) → replace gesture primitives (pouring, folding, lids-as-hinges) per `Docs/SHHH_PHYSICS_CONTRACT.md` §3.

## V9 work done 2026-11-10
- Durable requirement files: `CLAUDE.md` (READ FIRST block), `.claude/rules/shhh-physics-production.md`, `Docs/SHHH_OWNER_REQUIREMENTS.md`,
  `Docs/SHHH_PHYSICS_CONTRACT.md`, `Docs/SHHH_PHYSICS_TASK_COVERAGE.csv` (generated from the live catalog:
  `cd Core/SHHH.Core.Tests && dotnet run -c Release -- --coverage ../../Docs/SHHH_PHYSICS_TASK_COVERAGE.csv ../../Docs/Canonical_Recipe_Audit.csv`),
  `Docs/SHHH_ASSET_IMPORT_MANIFEST.csv`, `Docs/SHHH_ASSET_LICENSES.md`, `Docs/SHHH_PHYSICS_QA_EVIDENCE_INDEX.md`. Directives copied to `Docs/Production/`.
- Assets: licenses read for all reattached archives (CC0 except Nasha custom; Food pack = Asset Store EULA). Daycare KIT015/KIT018/KIT026 GLB →
  FBX (`Tools/glb2fbx.py`, Blender 5.2 bpy in the cloud) into `Assets/SHHH/Art/ThirdParty/Daycare/` (keys dc_kit015/018/026); source GLBs in
  `ArtSource/V9_Staging/daycare/`. `ArticulatedModel` uses PART_/COL_/FRONT/SOCKET_ nodes.
- KT01 dishwashing rebuilt: house kitchen sink anchoring (measured), KIT015 lever, soap pump primitive (KIT026), sponge-only soapy scrubbing,
  rinse in the faucet stream, upright KIT018 rack with real divider colliders, racked plates held; washables never start in a pile.
- New tools: `prefab_find`, `recipe_diag probe:…`, `log_copy cur|prev`, `ShhhKeepVisible`.

---

# SHHH! — V8 continuation state (Genies-primary directive, 2026-11-09)

Authoritative directive: `Docs/Production/SHHH_V8_GENIES_PRIMARY_CONTINUATION_2026-11-09.md` (supersedes the character section of the
one-shot master prompt; everything else in `SHHH_CLAUDE_ONE_SHOT_COMPLETE_MASTER_PROMPT_2026-11-09.md` stays active).
Evidence kinds as in `Docs/QA_Evidence_Index.md`: C core tests, E Editor play mode (desktop, simulated hands), N independent Win64 clients over
Photon, B build, D Quest headset. **No D (headset) result exists for anything below — all headset behaviour is UNVERIFIED.**

## Gate A — recovered interrupted state (done)

| Item | State | Evidence |
|---|---|---|
| APK interrupted at the usage limit | Recovered: `SHHH_alpha_0.6.3_c12.apk`, 162,224,681 B, sha256 `14f30ab8…4de0`, tag `apk-0.6.3-c12` (commit 83e8f0a) — re-hashed on the PC | `Builds/Quest2_3_3S/BUILDS.csv` |
| Patch-local scrubbing | Done, committed 63c7727; recipe_diag KT13/BT03/HW02/PT04 → 60 pass / 0 fail | `QA_Evidence/gateA/recipe_diag_scrub_patchlocal.json` |
| UI V8, audio V8, continuous match, net_multi | Done earlier (see QA index rows 2026-11-09) | `Docs/QA_Evidence_Index.md` |
| Laundry room dressing | **Pending** (PtDesigns CC0 props extracted; placement plan in Gate C below) | — |

## Gate B — Genies Avatar SDK as the primary avatar system

What exists (all real SDK 3.8.6 calls; no SDK internals patched, no meshes extracted):

- `Assets/SHHH/Genies/` (asmdef `SHHH.Genies`): `GeniesBackend` (init → instant login → anonymous fallback; starter looks; loading;
  persistence), `GeniesWardrobe` (in-world VR wardrobe), `GeniesDiag`, `GeniesWardrobeQa`.
- `Scripts/Game/Agents/AvatarService.cs`: engine-side interface (`IAvatarBackend`) so `SHHH.Game` never references the SDK; the backend
  registers itself before the first scene load.
- **Wardrobe** (front end → WARDROBE card, "create your Genie"): turntable plinth with the live Genie, closet board with 13 categories
  (Looks, Hair, Hair colour, Skin, Eyes, Tops, Bottoms, Shoes, Hats, Glasses, Extras, Body, Face) showing the SDK's own icons, 3×3 pages,
  equipped marks; side board: spin, the four starter looks, Undo, Reset, SAVE, Done. Every poke applies immediately on the live avatar.
  No purchase/unlock screens; only SDK default + the user's own inventory.
- **Persistence**: SAVE writes the SDK definition to `Settings.GeniesDefinition` (PlayerPrefs JSON) and to the SDK local profile `shhh_player`.
  The user's global Genies avatar is not overwritten.
- **Starter looks** Pip / Nell / Rory / Tess are real Genies definitions built at first run from one workshop avatar: body type, hair,
  hair and skin colour, top chosen by the SDK icon's colour against the seat palette (coral / teal / yellow / purple), distinct bottoms,
  shoes. Cached in `persistentDataPath/shhh_genies_presets_v2.json`.
- **Used everywhere**: Family Stage (4 Genies, picked seat wears the saved look), bots in the house, results podium, remote players.
- **Online**: each player's definition (~0.8 KB) travels as reliable chunked Fusion RPCs keyed by PlayerId (owner → host → all,
  re-sent to late joiners); seat avatars re-dress when it arrives. Remote humans' Genies hands follow their tracked hands through IK
  (Classroom-VR "VRMap" idea: the network pose drives the body's IK targets — concept only, nothing imported).
- Animation: SHHH humanoid controller built from Genies' own clips (idle / walk / run blend, carry, wave) by `genies_setup`.

Evidence:

| Run | Result |
|---|---|
| `genies_diag d1` (E) | every SDK step passes: init, login, load, inventories (hair 90, wearables 12 categories), equip, colours, body, feature stat, save local, reload | 
| `genies_journey j5` (E) | **15/15**: backend ready (logged in), starter looks 4/4, stage 4 Genies, wardrobe opens, try-on hair/top/bottoms/shoes/hair colour/skin/hat, definition changed, save, PlayerPrefs round-trip, 3 Genies bots in the match; screenshots 01–07 |
| `genies_journey j6` (E) | 15/15 with colour-picked starter tops (Nell "chill crop top", Rory "retro windbreaker", Tess "lilac crop top") |

Fixes found on the way: j1 starter looks 0/4 (Default loads without a controller timed out; several simultaneous loads returned null)
→ one workshop avatar + serialized loads; j3 the Editor process died inside native Genies code when the first edit came 0.2 s after the
load → settle 2.5 s and log each SDK step before calling it (j4–j6 clean). j4 screenshots were black because the Editor window was
minimized (Vulkan) — not a game bug.

Open in Gate B:
- Local player's own body in first person stays hidden (hands are the tracked hands) — by design for VR comfort; mirror view = wardrobe.
- Android/Quest: the SDK session on the headset is UNVERIFIED (the Editor uses a stored login; the headset will start with instant login
  → anonymous fallback). APK with Genies not yet built (next: c13).
- Quest performance of 4 Genies (16k verts, 12 materials each) UNVERIFIED; LOD option `TargetLods` available if needed.

## Gate C — physical household gameplay (in progress, highest effort)

**KT01 dishwashing flagship — implemented, core-tested, Unity run pending** (the PC link dropped while `recipe_diag KT01!` was starting):
- Core: `sink_tap` fixture (its door state is the valve), washable props (`StrokeCap`): sponge = food -> suds (stage 1, a second scrub does
  not rinse), `Fill` at the tap = rinse (refused while the tap is off or before scrubbing), rack upright, tap off. Leaving the tap on is a
  sabotage (Reopen). Solver opens the tap for rinse steps. `RCP-KT01` lifecycle + `RCP-KT01-WATER` → core tests **157/157**.
- Unity: `SinkTapView` (steel basin, gooseneck tap, push-the-lever with a hand or poke "Tap on/off", wobbling transparent stream,
  pool, splashes, looping water sound, Mom noise every 2.5 s while running, louder while splashing off a plate), `WashableView`
  (food smears erode while sponging, suds, suds melt in the stream, sparkle + drips when clean), sponge counts as a scrubbing tool
  (faster than a bare hand), rinse = hold the plate in the stream ~1.1 s. Original synthesized SFX: `tap_run` loop, `splash`,
  `tap_turn`, `sponge`, `rinse` (`Tools/audio_v8/make_sfx_water.py`).
- Next: run `recipe_diag KT01!` (the sync pack `v9m` is on the PC; apply it with `Tools/cycle.sh v9m.tgz recipe_diag "KT01!"`), review
  the per-action screenshots, then LD04.

Order: KT01 dishwashing (faucet, sponge, soap, rinse, rack; fix the KT01 plate bug) → LD04 laundry (washer door/state; R21 harness for
LD04/LD08/BR08) and laundry room dressing (sink (7.5,0,3.62), cupboard (8.15,0,3.62), tool rack south wall, ironing board (3.95,0,7.9),
cart (4.6,0,8.25), dehumidifier ≈(8.85,0,8.4)) → floor/surface cleaning → shared prop incident → HW03 → the 11 blocked recipes →
26 primitives audit → 12 comedy incidents.

## Gate D / E
- D: **N run 2026-11-10 (Win64 host + Editor client with the real Genies SDK, Photon Cloud uae)**: Genies looks replicated both ways
  (host received the Editor's 836-char look; the Editor's seat-1 avatar wears the host's 823-char look — `remote_seat1_genies: true`),
  client hands completed a recipe (LV04), **0 desyncs / 339 checksums**, single winner. Evidence `QA_Evidence/gateD/net_genies_2H_editorclient/`.
  The desktop screenshot of the remote Genie came out black (Editor minimized / OVR rig) — fixed in code (keep-visible + OVR rig off), not re-captured yet.
  Still to do: bots + Mom + pets in a 3–4 player net run.
- E: APK c13+ with sha, captures, docs.
