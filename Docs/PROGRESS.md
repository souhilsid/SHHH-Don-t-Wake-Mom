# PROGRESS — honest status

## V6 session 3 (2026-11-08) — Alpha 0.6.0 c9 delivered for the user-led headset test
See `IMPLEMENTATION_STATUS.md` (per-area tags + evidence), `V6_KNOWN_LIMITATIONS.md`, `V6_RELEASE.md`.
- **Interaction (XR-G1/UX-G2):** `xr_diag` on the real stack with simulated hands → 149/150 (12 root causes fixed); every menu key by
  poke and ray-pinch, standing and seated, reach ≤ 0.67 m. Hardware-unverified.
- **Online (NET-G4):** Photon Fusion 2.1.4 host mode + Photon Voice 2.64 — private room codes, lobby, bot fill, journal-replicated rules,
  host physics, client grabs, avatars, voice. 7 full matches with independent Windows clients on Photon Cloud (2H+2B, 3H+1B, 4H+0B),
  0 desyncs. Quest-to-Quest hardware-unverified.
- **Fun (FUN-G3):** round demo with ghost hand, waist-high duty piles, Mom audible within 0.2 s of a loud crash, shorter warnings,
  visible bot sabotage wind-up (giggle + ?!), swipe beacons, foil stars.
- **Art (VIS-G5):** Astra cast (4 siblings, Mom, cat, dog) on reduced meshes with Playables animation; Astra room dressing in all rooms.
- **Audio (AUD-G6):** new original Foley/music/stinger set (`V6_AUDIO.md`), metal impacts no longer silent, volume rows + Mic toggle.
- **Release:** `SHHH_Quest2_Quest3_Quest3S_Alpha.apk` 0.6.0 c9, release-signed (v2), arm64 IL2CPP, 0 build errors, manifest checked.
- Core rules tests 41/41.

---
## Earlier (V4/V5, 2026-11-05) — kept for history

Legend: **VERIFIED** = executed test evidence exists · **IMPLEMENTED** = code compiles/runs but not yet verified in headset · **NOT STARTED/BLOCKED**

## P0 Environment & corpus — VERIFIED
- PC: Unity 6000.0.62f1 (Android module, SDK, OpenJDK 17), JDK 21, Unity Hub, Meta VR Developer Hub, metavr CLI, Blender (MCP connected).
- Quest 2 serial 1WMHH8493D1016 detected over USB — **unauthorized** (needs in-headset USB-debugging approval).
- 19 source archives inventoried, hashed, licensed → `Docs/ASSET_LICENSE_MANIFEST.csv`. All 3D packs CC0 except Poly-by-Google sneakers/basket (CC-BY 3.0, credit required). Font: Quicksand Bold (OFL-1.1).
- Project created with Meta's Simplified VR Setup (OpenXR + Meta Quest feature + hand tracking, Android), then configured by `configure_project`: package `com.shhhgame.dontwakemom`, minSdk 32 / targetSdk 34 (Meta release manifest doc, last updated 2026-10-15), IL2CPP ARM64, Vulkan, Linear, ASTC, Quest 2/3/3S device targets, controllers+hands, lean URP (no shadows/HDR/post, 4x MSAA).

## Core rules engine (SHHH.Core) — VERIFIED (33/33 tests, `QA_Evidence/core_tests_run2.txt`)
- Four-seat roster: 1H+3B solo, 2H+2B, 3H+1B, 4H+0B; pre-match bot replacement; mid-match disconnect → bot takeover after grace; rematch keeps party.
- Match: Living Room → Kitchen → Laundry, 4→3→2→1, exactly one elimination per round, single winner (bots can win).
- Noise model (material × impact energy × distance to Mom × round multiplier), SleepMeter with stages & decay, per-object rate limit, voice never an input.
- Causal blame ledger: direct drops, throws, slams, unavoidable vs avoidable traps (counterplay window), chain reactions (root actor), simultaneous releases (deterministic), pet-only incidents (warning, never elimination), pet lures (provoker blamed), qualifying-event rule (tiny last noise doesn't take blame).
- Chores: seeded generator, shared + one private bonus per sibling, proportional credit, undo suspends credit, anti-farming, inspection with explainable ties + deterministic tie-break.
- Bots: utility AI (chores, bonus, rescue/disarm, bounded sabotage: precarious traps & hiding items, freeze when Mom stirs), Rookie/Normal/Clever profiles (no hidden score bonuses). Pets: cat ledge-knocks, dog barks/chases toys, lures.
- 200 seeded solo sims: all complete 4→3→2→1; bots win 153, scripted human 47; eliminations 494 inspection / 106 noise.

## Unity layer — IMPLEMENTED, editor-verified (no headset yet)
- Automated in-editor match (`playtest_autopilot`, human seat driven by a bot body): full 3-room match completes with real physics noise, Mom wakes & inspections, traps + rescues, pet lure blame, winner + results board; memory flat ~2.4 GB; 63–81 FPS in editor. Evidence: `QA_Evidence/playtest_autopilot_run_ok.json`, screenshots in `QA_Evidence/`.
- Hands (OVRHand/OVRSkeleton adapter): pinch/fist grab with hysteresis, physics-following carry, two-hand hold, clamped throws, tracking-loss freeze + gentle lowering, palm swipes only for fast deliberate motions. **Untested on device.**
- Locomotion: palm-up pinch gesture walk, teleport mode, snap turn, recenter, seated offset. **Untested on device** (palm-normal sign must be confirmed in headset).
- Diegetic UI: front-hall notice board (Solo / Friends / Practice / Settings / Quit, bot difficulty, pyjama colour), intro roster (HUMAN/BOT truthfully), chore board, Mom baby-monitor, wrist panel, evidence card, spectator board, results/rematch, pause, tutorial board.
- Rooms built at runtime from `RoomCatalog` (same data as the sim) with CC0 models + procedural fixtures; runtime NavMesh; Mom walks in through her door (no teleport); cat & dog procedural models.
- Audio: 70 original synthesized clips (material impacts ×4 variants, squeaks, UI, pets, door, snore, stingers, crickets ambience, music-box loop). Mom voice = cartoon babble + English subtitles (**temporary** until licensed English VO).

## Not done yet / known gaps
- Quest 2 install/launch/hand-tracking validation (blocked: USB authorization).
- Online multiplayer (Photon Fusion 2 + Voice): **NOT STARTED** — SDK requires the user's Photon account download (see BLOCKERS).
- Release/Alpha: release keystore created on the PC (`Secrets/`, not committed — user must back it up); signed build + MVDH upload not done (needs Meta app entry).
- Siblings: original procedural kids in seat-coloured pyjamas + slippers (`KidModel`, ~1.3 m, 4 hair styles, walk/carry/cheer/shush) replaced the adult base meshes (which read as unclothed adults). Full 3-room autopilot match re-verified with them (`QA_Evidence/kid_*.png`).
- Art polish still open: Mom is a scaled CC0 adult mesh in nightwear; pets are procedural; no custom Blender assets yet. Logo is the supplied PNG.
- Ripple VFX is the only transparent effect; no particles yet. No music in rooms (by design: silence + Foley).
