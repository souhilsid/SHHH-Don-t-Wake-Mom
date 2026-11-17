# DEVICE TESTS

## 2026-11-07 — Quest 2 (serial 1WMHH8493D1016, Horizon OS build UP1A.231005.007.A1, Android 14) — build SHHH_dev_0.1.3_c4.apk

Installed with `metavr app install` and launched. Logcat was captured from the PC while the user played.

| Check | Result | Evidence (logcat) |
|---|---|---|
| Install + launch, OpenXR session, single-pass instanced | **PASS** | `OpenXR instance 0x3 session 0x51`, `Rendering Mode - SinglePassInstanced` |
| Hand tracking binds both hands (OpenXR skeleton, 26 joints) | **PASS** | `[SHHH] Left/Right hand skeleton bound (XRHand*, 26 bones)`. A device screenshot shows both tracked hands at the menu. |
| Poke UI with a real finger | **PASS** | The user pressed SOLO: `GameRoot:StartSolo() <- PokeButton:Press()` |
| Solo match starts (1 human + 3 bots), Living Room loads, NavMesh bakes on device | **PASS** | `MATCH START mode=Solo humans=1 bots=3`, `NavMesh baked … 111 sources` |
| Bots do personal jobs on device | **PASS** | `step: Tess placed shoe`, `CHORE DONE 'Return the TV remote' … by Juno` |
| Inspection → one elimination → round 2 | **PASS** | `INSPECTION (TimerExpired) => Eliminate Rory` at 63 s, then `ELIMINATED Rory` |
| Frame rate, menu | **72/72 FPS** sustained | App GPU ≈ 9–10 ms (budget 13.9 ms), GPU% ≈ 0.75–0.81 |
| Frame rate, Living Room with 4 siblings + 2 pets | **72/72 FPS** most seconds; dips to 59–66 FPS at load and inspection transitions | App GPU ≈ 8–10.9 ms, CPU&GPU ≈ 14–17.5 ms, Free mem ≈ 1.45 GB, 29.5 °C |
| Human contribution in round 1 | Human seat scored duty 0 and idle −10 | The player didn't complete their job in the first 60 s. Need the user's comprehension feedback. |

Not yet measured: hand grab reliability on every prop, Mom VO audibility, comfort, Kitchen/Laundry frame rate, Quest 3/3S, two-device Photon.
