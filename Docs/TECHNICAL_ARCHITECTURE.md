# TECHNICAL ARCHITECTURE

```
SHHH.Core (pure C#, deterministic, unit-tested in dotnet)
  CompetitorRoster/CompetitorSlot ─ MatchController ─ RoundController ─┬ ChoreBoard (seeded)
                                                                       ├ ScoreLedger (inspection)
                                                                       ├ SleepMeter + NoiseModel + NoiseGate
                                                                       ├ CausalLedger (blame)
                                                                       ├ MomDirector (state timeline, verdict latch)
                                                                       └ PetBrain×2, BotBrain×N  ──► IAgentBody / IWorldQuery
SHHH.Game (Unity)
  GameRoot (Boot scene, app flow, additive room scenes) ─ RoomRuntime (builds room from RoomCatalog, NavMesh, binds round)
  PropBody (physics → SubmitImpact/Push/Trap) · AnchorFixture (snap + validate → Placed/Removed)
  BotBody/PetBody (IAgentBody on NavMeshAgent) · MomCharacter (walks in via door) · SiblingAvatar + ProceduralHumanoid
  XR: OvrHandSource → HandInteractor (grab/throw/tracking-loss) · PlayerRig (gesture walk/teleport/snap) · WristMenu · PokeButton
  UI: MenuBuilder boards, ChoreBoardView, MomMonitorView, RoomFx (ripples/toasts), AudioBank
```
- **Authority**: all gameplay facts enter RoundController through host-side physics (impacts, grabs, placements). No client reports culprits or scores.
- **Seat abstraction**: humans and bots are both `CompetitorSlot`s; a bot is driven by `BotBrain`+`BotBody`; a human by hands. Same `Grab/Release/Placed` APIs.
- **Offline solo** runs the full simulation locally with no network dependency.
- **Online (planned, Fusion 2 Host Mode)**: host runs Core + all bodies; clients send hand intents; see NETWORK_PROTOCOL.md.
- **Determinism**: own xorshift RNG; deterministic event ordering inside a tick; identical seeds reproduce identical logs (test MATCH-06).
