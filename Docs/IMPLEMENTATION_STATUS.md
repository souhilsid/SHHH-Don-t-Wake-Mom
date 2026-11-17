# IMPLEMENTATION STATUS — V6 (updated 2026-11-08)

Tags: **WORKS ON QUEST** · **WORKS IN EDITOR ONLY** (automated evidence on the PC) · **VERIFIED WITH INDEPENDENT CLIENTS** ·
**PARTIAL** · **MISSING**. Per the V6 offline override no headset was used, so nothing is tagged WORKS ON QUEST; everything
that depends on the device is **hardware-unverified** (see `Docs/V6_KNOWN_LIMITATIONS.md`).

| Area | Status | Evidence |
|---|---|---|
| Rules engine: 4 seats, 4→3→2→1, causal noise blame, pets non-punitive, inspection, ties, personal duties, sabotage/foil, rush, match journal | WORKS IN EDITOR ONLY (headless) | `Core/SHHH.Core.Tests` **41/41** (200 solo sims, NET-01 40 streamed replays, NET-02 late join + rematch, FUN-01) |
| Hand grab / carry / place / throw / two hands / tracking loss / magic pull | WORKS IN EDITOR ONLY, hardware-unverified | `xr_diag` on the real stack with simulated hands: **149 pass / 1 fail** (deliberate early pinch on a cluttered table) — `QA_Evidence/xr_diag_20261108_0839.json`, `Docs/V6_INTERACTION_VALIDATION.md` |
| Menus without walking: poke + ray-pinch on every main / Customize / Settings / lobby key, standing + seated, reach ≤ 0.67 m | WORKS IN EDITOR ONLY | same run, 68 UI rows |
| Solo vs 3 bots, full 3-room match, winner + results + rematch | WORKS IN EDITOR ONLY | `playtest_autopilot` runs (status JSON), `QA_Evidence/v6_*.png` |
| Photon Fusion 2.1.4 private rooms (code), lobby, ready, bot fill, journal-replicated match, host-physics props, client grabs, avatars | **VERIFIED WITH INDEPENDENT CLIENTS** (Windows processes, Photon Cloud), Quest hardware-unverified | `Docs/V6_NETWORK_TWO_CLIENT_LOGS.md` runs 2–8: 2H+2B, 3H+1B, 4H+0B, all **0 desyncs** |
| Photon Voice 2.64 (all-to-all, positional, Mic toggle on wrist + Settings, Android permission request) | VERIFIED WITH INDEPENDENT CLIENTS with a generated tone; real microphone hardware-unverified | runs 4/6/7/8 voice frames decoded from every remote peer |
| Mid-match disconnect → bot takeover, same-name rejoin, host loss ends match cleanly | WORKS IN EDITOR ONLY (Core MATCH-05, NET-02) + code path | no independent-client disconnect run yet |
| Round demo ("show, don't tell": ghost hand carries YOUR first item to its target, YOURS/HERE labels) | WORKS IN EDITOR ONLY | `QA_Evidence/v6_demo_*.png` |
| Bot sabotage wind-up tell (giggle, ?! icon, "hehe..."), swipe beacon, foil stars | WORKS IN EDITOR ONLY + replicated | net run 7: tell logged on host and both clients before `SABOTAGE` |
| Mom reacts audibly within the same tick to loud events (stir line + 4 s startled hold), shorter warning/inspection pacing | WORKS IN EDITOR ONLY | Core FUN-01; autopilot logs |
| Astra cast: 4 siblings, Mom, cat Mochi, dog Biscuit (V2 `_Reduced` meshes 2.7–8.7 k tris, V2/V3 clips via Playables) | WORKS IN EDITOR ONLY | `QA_Evidence/v6_cast_1-3.png`, `inspect_rigs` |
| Room dressing (Astra V2 architecture/decor, wainscot, beams, pendant, windows, Mom's door frame/plaque, room-specific pieces) | WORKS IN EDITOR ONLY | `v6_room_a-d.png`, `v6_room_kitchen*.png`, `v6_room_laundry*.png` |
| Audio V6 (Foley ×6 variants incl. metal, lullaby menu music, stingers, giggle, Nell name VO, volume rows) | WORKS IN EDITOR ONLY, not ear-checked | `Docs/V6_AUDIO.md` |
| Release APK (ARM64 IL2CPP, release-signed) | see `Docs/V6_RELEASE.md` | build log + `apkanalyzer`/`aapt` checks |
| Facial animation (blend-shape faces) | PARTIAL — Astra clips include head motion; no dedicated facial rig/visemes | — |
| Spectator mode | PARTIAL — eliminated humans spectate with a spectator board and cannot score; no free-fly camera | autopilot logs |
| Quest performance (≥ 72 fps) | hardware-unverified | editor 63–90 fps only; risk list in limitations |
