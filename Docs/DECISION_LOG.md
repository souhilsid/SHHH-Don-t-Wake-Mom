# DECISION LOG

| Date | Decision | Why |
|---|---|---|
| 2026-11-07 | **Personal duties replace the shared chore list.** Every sibling gets ONE job (2-3 items in their colour → their own target). House chores remain as small optional points. | V5: individual duties must be central; humans must never watch bots do "the list". One job = understandable in seconds. |
| 2026-11-07 | **Sabotage = physically swiping a rival's already-placed item**, visible to everyone (beacon in the owner's colour) and **counterable**: restore within 20 s → saboteur caught (−8), owner saved (+4). Bots never swipe the same item twice. | Creates the "I saw that!" moment with clear counterplay and anti-grief bounds; no invisible/random penalties. |
| 2026-11-07 | **Rush timer**: first finished job → at most 30 s left (never below 60 s total). | Turns finishing early into pressure on everyone; tested in 180/180 simulated rounds. |
| 2026-11-07 | Own-job progress is the first tie-breaker at inspection. | Matches the new focus; explainable in one line. |
| 2026-11-07 | Mom VO via Piper TTS voice `en_US-kristin-medium` (trained from scratch on LibriVox public domain). Lines with a name play a separate name clip first. | Fully English, license-clean, regenerable; voice actor can replace it later without code changes. Rejected `hfc_female` (CC BY-NC-SA) and `lessac`-derived voices (non-commercial). |
| 2026-11-07 | Photon **Fusion 2.1.4 Stable + Voice 2.64 (Realtime 5)** — the matching pair per Photon's download page. App IDs live in `Secrets/photon.json` → written into `PhotonAppSettings.asset`, which is git-ignored. | V5 §9; no 2.0/Realtime4 mixing. |
| 2026-11-07 | Added `Meta.XR.MultiplayerBlocks.Fusion` to Fusion `AssembliesToWeave`. | Meta XR Core SDK compiles Fusion building blocks when Fusion is present; unwoven, they crash `NetworkTypesMeta` at runner start. |
| 2026-11-07 | First network proof uses Fusion **multi-peer** (host + client in one Editor, real Photon Cloud). Labelled honestly; not counted as the two-device proof. | Fast, automatable, no headset needed; the independent-device test follows. |
| 2026-11-07 | Astra V3 hero rigs not dropped into gameplay yet (≈28-30 k tris each). | Quest 2 budget; V5 says integrate selectively and measure. |

## V7 (2026-11-08) — final locked specification
- **D-V7-01** The master prompt `Docs/Production/SHHH_CLAUDE_MASTER_PRODUCTION_PROMPT_FINAL.md` and its 16 locked decisions supersede V6
  briefs where they conflict. V6 evidence stays valid for the systems it tested.
- **D-V7-02** Codex V4/V5 edits on the PC are the baseline (tag `pc-baseline-v7`); its connected house prefab, Simple Hands binding and
  book diagnostic are preserved. Review-only scenes are not edited.
- **D-V7-03** Recipes are implemented as an engine-free data model in `SHHH.Core` (stable IDs LV01…HW06, versioned) with a generic
  step machine over abstract prop/fixture states, so every recipe is testable headless before physical props exist. Existing
  `ChoreTemplate` IDs stay valid; a compatibility map links them to recipe IDs (no identifier is deleted).
- **D-V7-04** Areas not yet built (bedrooms, bathroom, foyer, pet area, halls) are data-complete but excluded by the generator's
  availability validator until their geometry exists — no recipe is offered for an unreachable room.
- **D-V7-05** Non-voice interrogation (evidence cards, gestures) is built before any LiveKit work; LiveKit stays behind an adapter and is
  blocked on a user-owned token backend.
