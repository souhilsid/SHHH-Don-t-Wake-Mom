# V6 — XR-G1 / UX-G2 offline interaction validation

**Status label:** EDITOR-ONLY (automated, simulated hands). **Hardware-unverified**: real OVR pinch noise, hand-tracking latency,
finger-pose fidelity and comfort can only be confirmed on a Quest (final user-led test).

## Harness
`xr_diag` automation command → `Scripts/Game/QA/XrInteractionDiagnostic.cs` (Editor only, never in the APK).
It boots the **real** game (Boot scene, real `PlayerRig`, real `HandInteractor`s, `PokeButton`, `UiPointer`, `PropBody`,
`AnchorFixture`, real practice `RoundController`) and replaces only the *input source*: two `SimHandSource` hands parented to
the rig's tracking space (so they travel with locomotion like OVR hand anchors) and a scripted head pose (standing 1.62 m /
seated 1.15 m). Every attempt is logged with its cause. Output: `Automation/xr_diag_result.json`, copies in `QA_Evidence/xr_diag_*.json`.

## Root causes found and fixed (each reproduced by the harness first)
| # | Symptom on the Quest recording / harness | Root cause | Fix |
|---|---|---|---|
| 1 | pinch "does nothing" | grab only on the exact latch frame | 0.8 s grab window after the latch (`HandInteractor.GrabWindow`) |
| 2 | thin props missed | 9 cm probe | 15 cm probe / 15 cm surface accept (consistent radius) |
| 3 | phantom drops | pinch-strength flicker | release needs an open hand for 80 ms (harness: OVR-flicker carry 9/9 kept) |
| 4 | drop during fast swing | 0.45 m follow-error release | 0.7 m; harness: 2.0 and 2.4 m/s swings kept |
| 5 | cushion / book / pan jumped into the hand, 17–25 cm follow error | "far grab" decided by **pivot** distance (> 0.2 m) → big/tall items were magic-pulled pivot-first | far = collider-**surface** distance; pulls align the visual centre |
| 6 | magic pull never worked | `_farHover` cleared before `FindDistant` read it → target lost as soon as the pinch passed 0.6 | keep the previous far target while closing the pinch |
| 7 | item dangling 15 cm below fingers after an early pinch | grab at first contact kept the offset | nearest surface point snaps into the fingers in 0.12 s |
| 8 | held book blocked 20 cm behind the hand by a cup + pan | held body mass 0.45 kg vs 1.3 kg pan | held items get ≥ 4 kg inertia while held (`PropBody.BoostHeldMass`) |
| 9 | tracking loss: item kept chasing stale joints for 0.12 s | follow ran while untracked | untracked ⇒ item held still immediately, frozen after 0.12 s |
| 10 | placements rejected for ~16 s in the tutorial | practice "wake" ran the full Mom freeze | practice Mom only grumbles; warnings now run at 55 % length (~9 s), catches 80 % |
| 11 | early pinch sweeping past a neighbour grabbed it | latch frame accepted any hover | latch-frame grab requires the hand to have slowed (< 0.35 m/s) or touch the item |
| 12 | crouching for floor toys (recording 201409) | duty props spawned at y = 0.02 | reach pass: every grabbable prop now spawns on a 0.74–0.82 m "messy pile" table; toy chest and shoe shelf raised to 0.72/0.82 m |

## Latest run (2026-11-08 07:18 UTC): 143 PASS / 1 FAIL
- **Near grab → hold → move → release:** 53/54 (98 %, gate ≥ 90 %) over 9 grip archetypes (book, cup, plate, toy car,
  cushion, fork, sock, dog toy, pan) × 6 variants (centre, edge, hovering 4 cm above, pinch 0.35 s early, pinch late, OVR-style
  pinch flicker while carrying). Follow error 2.0–5.9 cm. Remaining miss: a deliberately early pinch over a cluttered table
  took the neighbouring book instead of the cup (documented, acceptable behaviour).
- **Shelf/socket placement:** 10/10 validated by the real rules (imprecise drops up to 5.5 cm off), each in ~0.47 s.
- **Fast carry 2.0 / 2.4 m/s:** kept. **Magic pull** (pan 2.35 m, cushion 2.40 m): both arrive between the fingers.
- **Tracking loss 0.5 s:** frozen in place, resumed, 0 noise events. **7 s:** lowered gently onto the table, 0 noise (no fake blame).
- **Throw:** release speed ≈ hand speed (3.1 m/s), clamp 5.5 m/s. **Two-hand cushion:** both hands hold, one-hand release keeps it.
- **No accidental locomotion:** 0.0 cm while carrying palm-up with a held pinch; 0.0 cm for a palm-down pinch in the air;
  the deliberate palm-up pinch-and-push gesture moves the player (control).
- **UI (UX-G2):** main board 6/6, Customize 8/8, Settings 10/10 buttons — every one by **poke and by ray-pinch**, standing
  and seated; max shoulder reach 0.64–0.67 m (comfort ≤ 0.68 m); flows open/close Customize by poke and Settings by ray.
- **Reach audit:** 27/27, 21/21, 17/17 grabbable props spawn ≥ 0.4 m (most at 0.74–0.82 m); chore targets ≥ 0.4 m except the
  pet mat (kitchen) and shoe mat (laundry) drop zones — large soft drop targets you can toss into from standing.
