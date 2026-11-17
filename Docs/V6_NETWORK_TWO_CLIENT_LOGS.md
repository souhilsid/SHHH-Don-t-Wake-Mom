# V6 — NET-G4 Photon multiplayer validation

**Label:** VERIFIED WITH INDEPENDENT CLIENTS (Windows desktop processes, real Photon Cloud, region `uae`).
**Hardware-unverified:** Quest ↔ Quest / Quest ↔ PC sessions, real microphones, Wi-Fi latency on headsets.

## SDK
Photon Fusion 2.1.4 (Host Mode) + Photon Voice 2.64 (Realtime 5, the pair required by Fusion 2.1). App IDs come from
`Secrets/photon.json` → `PhotonAppSettings` (git-ignored, never printed).

## Architecture (what is actually networked)
| Concern | Mechanism | Code |
|---|---|---|
| Private rooms | Fusion Host Mode session `SHHH-<CODE>`, 4-letter code without look-alike letters, `IsVisible=false`, `PlayerCount=4` (the session refuses a 5th) | `ShhhNet.StartSession` |
| Lobby | host seats humans as they join, bots fill the rest; seats/names/ready/cosmetic replicate in `NetLobby`; Start only when all humans are ready | `NetLobby`, `LobbyBoard`, `MenuBuilder.ShowLobby/ShowKeypad` |
| Rules | host runs the authoritative `MatchController`; **every rules call is recorded in a `MatchJournal`** and streamed to clients (reliable RPC chunks ≤ 400 B at 10 Hz); clients replay into an identical replica that drives the normal UI, Mom, boards, eliminations, results | `Core/Journal.cs`, `ShhhNet.HostUpdate/ClientUpdate` |
| Desync detection | host writes a state checksum every second; clients compare | `MatchJournal.Checksum` |
| Physics | host-simulated; props (pose + holder), bots, pets replicate via `NetWorld`; clients render kinematically and predict their own held item | `NetWorld`, `HandInteractor` (replica mode) |
| Client grabs | intent RPC → host applies the same rules (`Round.Grab`) and carries the item with that player's networked hand (`RemoteHold`); contention: first holder keeps it | `ShhhNet.HostOnGrab/HostOnRelease`, `RemoteHold` |
| Avatars | each human's head + hands via Fusion input → `NetHuman`; remote humans render as their sibling with mitten hands | `NetHuman`, `RemoteHumanView` |
| Scenes | every peer loads rooms locally from the replica's RoundStarting; the countdown starts when all humans have loaded (or 10 s) | `GameRoot.LoadRound`, `ShhhNet.RoomLoaded` |
| Voice | `FusionVoiceClient` + `Recorder` on the runner, `VoiceNetworkObject` + positional `Speaker` on every `NetHuman`; wrist **Mic** mute; Android microphone permission; the microphone is never a noise input (Core NOISE-12) | `ShhhNet.SetupVoice`, `WristMenu` |
| Disconnects | lobby leave frees the seat; mid-match: Core grace period then a bot takes the seat; same-name rejoin reclaims it and fast-forwards from the full journal (reliable transfer); host loss ends the match for everyone (**no host migration**) | `ShhhNet.OnPlayerLeft/HostOnHello/OnShutdown`, Core MATCH-05, NET-02 |

## Offline proof of the replication model (dotnet, same Core files as the game)
- **NET-01** 40 seeded matches (1–4 humans), journal streamed in random-size chunks while the host plays: replica logs
  identical line-for-line, same winner, same elimination order, same scores, 0 checksum mismatches (> 30 per match).
- **NET-02** a client that connects late gets the full journal (two complete matches incl. a rematch) and is identical.
Evidence: `QA_Evidence/core_tests_*`, `Core/SHHH.Core.Tests` → **41/41 passed**.

## Independent-process runs (Windows players, `-batchmode -nographics`, launched by `net_multi`)
Each process is a separate OS process with its own Photon connection. Clients play their personal job through the real
client path: networked hand pose + grab/release intents; the host validates placement with its physics; the client sees the
result in its replica. Evidence folders: `QA_Evidence/net_multi_run2_2H … run8_4H/` (`host.json`, `clientN.json`, full player logs).

| Run | Config | Result |
|---|---|---|
| 1 | 2H+2B | joined + replica, but the client fell **120 s behind** → root cause: one reliable-data transfer per frame queues behind each other. Fixed: journal via reliable RPC chunks at 10 Hz. |
| 2 | 2H+2B | full match, 3 rooms, winner seat 3 on both, **0/340 desyncs**, client grabs 8/8 granted, 3 steps validated by host physics. Found: neighbouring slots double-snapping one item (fixed in `AnchorFixture`), shoe rack slots too tight (widened). |
| 3 | 3H+1B | full match, 3 rooms, human winner, **0/137 desyncs** on both clients; voice stayed `PeerCreated` → fixed (voice client callbacks registered on the runner). |
| 4 | 4H+0B | full match, 3 rooms, identical elimination order `1,3,2` + winner on all four, **0/145 desyncs** ×3, voice **Joined** on all four, each decoded ~10 950–10 980 voice frames from **3 remote voices** (all-to-all), clients 18/20 grabs granted (2 contention denials), 9 steps validated. |
| 5 | 2H+2B (rerun after Astra cast + room dressing) | full match, **0/202 desyncs**, voice bidirectional (~5 080 frames each way), same winner/elimination order on both. |
| 6 | 2H+2B (after V6 audio set + Settings rows + sabotage tell; `NetAgentPose` gained `Plot`) | full match, 3 rooms, winner seat 2 + order `1,3,0` on both, **0/219 checksums mismatched**, client grabs 6/6 granted, 4/6 steps validated by host physics, voice Joined both ways (5 497 frames). |
| 7 | 3H+1B (same build) | full match, winner seat 3 + order `0,2,1` on all three, **0/176 desyncs** on both clients, 16 grabs granted of 19 (3 contention denials), 9 steps validated, voice all-to-all (2 remote voices each, 8 800–9 300 frames). **Bot sabotage tell replicated**: bot Tess plotted → `tell:` logged on the host AND both clients, then `SABOTAGE Tess took Kid1's item`. |
| 8 | 4H+0B (same build — the build the release APK was made from) | full match, winner seat 0 + order `2,1,3` on all four, **0/150–151 desyncs** ×3, client grabs **20/20** granted, 13 steps validated, voice Joined on all four, 3 remote voices each (11 330–11 380 frames). |

Notes
- The test clients carry items in straight lines and are often caught by Mom early (they are clumsy on purpose) – that is
  gameplay, not a network fault; step counts are therefore modest.
- Headless players have no microphone; they transmit a generated tone (`Recorder` Factory source). Real-mic capture,
  Android permission prompt and audibility on Quest are **hardware-unverified**.
