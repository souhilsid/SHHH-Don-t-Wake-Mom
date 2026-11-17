# Current architecture (baseline audit 2026-11-09, `SHHH_alpha_0.6.2_c11` → one-shot production)

## Stack (verified)
Unity **6000.0.62f1**, URP 17.0.4, OpenXR 1.16.1, Meta XR Core SDK 201 (the Interaction SDK package is **not** installed; hands use the
project's own `HandInteractor` + `OvrHandSource`/`SimHandSource`), Photon Fusion 2.1.4 (host mode) + Photon Voice, DOTween, Android IL2CPP ARM64.
Baseline APK `SHHH_alpha_0.6.2_c11.apk` 122,237,847 B, sha256 `b78d35ec…3f0` (re-verified on the PC). Git tag `baseline-0.6.2-c11` (cloud mirror).

## Layers
| Layer | Where | Role |
|---|---|---|
| **Core rules** (engine-free, `noEngineReferences`) | `Assets/SHHH/Scripts/Core` | single source of truth: `MatchController` (Match.cs), `RoundController` (Round.cs, now the continuous-match director), `HouseLayout`/`MatchRules` (House.cs), noise/causality, Mom belief + interrogation, bots (`Agents.cs`), scoring, progression, journal (deterministic replay/network), recipes (`Core/Recipes`: catalog of 72, generator, layout, board, world, solver) |
| Core tests | `Core/SHHH.Core.Tests` (dotnet console) | 156 tests incl. simulated matches; CM-01..08 cover the continuous match |
| **Game layer** | `Scripts/Game` | `GameRoot` flow, XR rig/hands, `PropBody`, `RoomRuntime`, recipe views (`RecipeRuntime`, `RecipeFixtureView`, `RecipeClipboard`), bots/pets/Mom bodies, diegetic UI (`MenuBuilder`, `FamilyStage`, `PokeButton`, `WorldText`, `WristMenu`, `ConfrontationPanel`), `AudioBank`, art catalog + `HouseAssembly` |
| **Networking** | `Assets/SHHH/Networking/Online` | `ShhhNet` (host authority; journal stream to clients; replica `MatchController` + checksums), `NetWorld` (prop holds/poses), `NetHuman`, `NetLobby`, `NetAutoTest` (headless clients for `net_multi`) |
| **Editor automation** | `Assets/SHHH/Editor` | file bridge (`Automation/inbox`→`outbox`), builds, playtest autopilot, recipe_diag, xr_diag, net_multi, asset pipeline |

## Authority & flow (2026-11-09)
`Boot → FamilyStage (menu) → Solo/Online setup (5 MIN / 10 MIN) → ContinuousMatch (one RoundController over the whole house) → final inspection → Results → Rematch/Stage`.
- One `RoundConfig.ContinuousMatch(seconds)`: `Continuous=true`, `JobsPerSeat` = 2 (5 min: living + kitchen) or 3 (10 min: + laundry).
- The clock never stops early; Mom warnings / confrontations are nested events. The first confrontation is the full one (8 s present + 25 s evidence),
  later ones are quick (3 s + 12 s). A proven disturbance → `MomVerdictKind.Penalty`: −20 points + a 15 s "sent to bed" timeout (not at the final inspection).
- At 0:00 Mom does a final inspection, then one results screen ranked by score > progress > less noise > earlier first job > seat.
- The host is authoritative for clock, props, recipes, noise, Mom, bots and scores; clients send intents (grab/release/recipe ops/cards) and
  rebuild state from the journal (checksummed).
- Legacy 4→3→2→1 rounds remain only behind `MatchController.ContinuousDefault=false` for old regression suites (not reachable from the UI).

## Known gaps carried into the gates
Interaction depth (most recipes are grab-to-slot; Gate 4/5), 11 recipes need rooms that are not built (Gate 2), UI art (Gate 3), audio (Gate 8),
Genies/Mixamo/LiveKit (blocked, see BLOCKERS.md), Mom's enclosed upstairs bedroom + stairs route (Gate 2/6), harness regressions R21 (LD04/LD08/BR08), HW03, KT01.
