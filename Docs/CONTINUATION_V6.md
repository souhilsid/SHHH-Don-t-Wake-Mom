# CONTINUATION RECORD — V6 Critical Recovery (updated 2026-11-08 09:30 UTC, session 3 — Alpha 0.6.0 c9 built)

**Authority (newest first):** `Docs/SHHH_V6_OFFLINE_OVERRIDE.md` (no headset during development; offline validation; one final user-led Quest test) > `Docs/SHHH_CLAUDE_CRITICAL_RESCUE_MASTER_V6.md`. Results that need a headset are labelled **hardware-unverified**. Never ask the user to connect the Quest before final delivery.

## How work flows (session 3 setup)
- Source of truth = the PC folder `C:\Users\aisci\Downloads\SHHH Game\SHHH`. The cloud container keeps a git mirror at
  `/home/claude/SHHH` (dotnet 8 installed; `Tools/CompileCheck` type-checks all Unity C# in ~5 s against
  `/home/claude/refs/all` = Tools/UnityRefs + Library/ScriptAssemblies + Fusion DLLs from the PC).
- Sync cloud → PC: `/home/claude/sync_pack.sh <name>` → `device_commit_files` → on PC `bash Tools/cycle.sh <name>.tgz [cmd] [args]`
  (`Tools/apply_sync.py` rewrites files in place because deletes are forbidden on the mount; `cycle.sh` refreshes Unity,
  waits for the compile and prints errors).
- If the container was reset: re-pull sources with a tar on the PC (`_sync/pull_src_*.tgz`) and the refs (`_sync/refs2.tgz`, `refs3.tgz`, `unityrefs.tgz`).
- `ShhhLogTools.cs` on the PC is not writable from the mount; use the new `log_find "cur;a|b;N"` (last N matches).

## Gate status
| Gate | Status | Evidence |
|---|---|---|
| XR-G1 grab/UI | **EDITOR-ONLY PASS (149/150)**, hardware-unverified | `Docs/V6_INTERACTION_VALIDATION.md`, `QA_Evidence/xr_diag_20261108_0839.json` |
| UX-G2 no-reach menus | **EDITOR-ONLY PASS** | 68 UI rows: main/Customize/Settings(13 keys)/lobby by poke AND ray, standing+seated, reach ≤ 0.67 m |
| NET-G4 | **VERIFIED WITH INDEPENDENT CLIENTS** (desktop, Photon Cloud), Quest hardware-unverified | `Docs/V6_NETWORK_TWO_CLIENT_LOGS.md` runs 2–8 (`QA_Evidence/net_multi_run*`), 0 desyncs; Core NET-01/02 |
| FUN-G3 | **EDITOR-ONLY PASS** for implemented items; fun with humans unverified | DutyDemo, Mom ≤ 0.2 s reaction (FUN-01), sabotage wind-up tell (replicated, run 7), beacons/foil |
| VIS-G5 | **EDITOR-ONLY** | Astra cast + room dressing, `QA_Evidence/v6_cast_*`, `v6_room_*` (kitchen/laundry re-shot after beam fix) |
| AUD-G6 | **EDITOR-ONLY** (not ear-checked) | `Docs/V6_AUDIO.md` |
| Release | **BUILT + STRUCTURALLY VERIFIED** | `Docs/V6_RELEASE.md`, sha256 a652c8ae…a44 |

## Done in session 3
- `xr_diag` harness (real game stack, simulated hands) + 12 grab/UI root-cause fixes (see V6_INTERACTION_VALIDATION).
- Reach pass on all rooms + Practice bench with one of each grip archetype.
- `UiPointer` ray-pinch for every `PokeButton`; lectern placement for all boards; main menu redesign + Customize board.
- Online: Core `MatchJournal`; `SHHH.Net.ShhhNet` (Fusion session, lobby, journal streaming, replica, `NetWorld`,
  `RemoteHold`, `NetHuman` avatars, Photon Voice), lobby/keypad UI, client prediction; `NetAutoTest`; editor commands
  `net_game_setup`, `net_bake_game` (rebuilds the Fusion prefab table), `build_win` (Windows test player, switches back to Android),
  `net_multi <humans>`, `net_multi_status`, `net_multi_kill`, `net_auto_editor`.

## Done later in session 3
- AUD-G6 sound set (`Tools/synth_audio_v6.py`), Settings volume rows + Mic, Nell name VO (`Tools/vo/gen_mom_vo.py --names-only`).
- Bot sabotage wind-up tell (`BotBrain.Plotting` → `SiblingAvatar.SetPlotting`, replicated via `NetAgentPose.Plot`).
- Room dressing beam yaw fix; kitchen + laundry screenshots.
- Regression: xr_diag 149/150, Core 41/41, net_multi 2H/3H+1B/4H all 0 desyncs (runs 6–8) on the release code.
- `build_apk_release` → `SHHH_alpha_0.6.0_c9.apk` copied to `SHHH_Quest2_Quest3_Quest3S_Alpha.apk`; androguard manifest/signature check.

## Next steps, in order (after the user's headset test)
1. Triage the user's Quest recording against `Docs/V6_KNOWN_LIMITATIONS.md` (frame rate first; levers listed there).
2. ISSUE-12/13 (bot takeover body mid-round, client anchor rings); independent-client disconnect/rejoin run.
3. Facial animation (blend shapes) and spectator camera if the art budget allows on Quest 2.
4. Alpha upload once the user provides the App ID + token (`Docs/V6_RELEASE.md`).
5. Every new build: version is bumped automatically; rerun xr_diag, Core tests, net_multi 2/3/4 before `build_apk_release`.

## Known limitations so far
- No host migration: if the host leaves, the match ends for everyone (clear board message).
- A seat handed to a bot mid-round stays idle until the next room (the bot body spawns at room start).
- Client-side placement rings (AnchorFixture occupancy visuals) only update on the host; chore boards are correct everywhere.
