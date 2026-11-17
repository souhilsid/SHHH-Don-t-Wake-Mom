# NETWORK PROTOCOL (planned — Photon Fusion 2 Host Mode; NOT IMPLEMENTED, SDK pending)

- Fusion App ID / Voice App ID are kept in a local, uncommitted runtime config (`Assets/SHHH/Resources/PhotonLocal.json` generated from `Secrets/`), not in source.
- Session: private room named by a 5-letter join code; max 4 human peers; `SessionProperties` carry match seed & roster version.
- Host owns: MatchController/RoundController, all PropBody rigidbodies (state authority), bot & pet bodies, Mom. Clients send per-tick input: head pose, both hand poses, pinch/grip strengths, grab/release intents with target prop id.
- Replicated state: roster (seat kind, name, cosmetic, status), round phase/timer/meter, chore step states, scores (read-only), prop transforms for interest-relevant props (others at low rate), avatar poses, verdict/evidence.
- Scene flow: host loads room scenes via Fusion scene manager; countdown waits for all clients' scene-ready ack (timeout → proceed, late client spectates the countdown).
- Disconnect: 20 s grace (avatar frozen, held props released gently), then seat → bot takeover (Core: `MatchController.HumanDisconnected/HumanReconnected`, tested in MATCH-05). Host leaves → clean return to menu with message (no fake migration).
- Voice: Photon Voice 2 proximity; mic never feeds gameplay noise; mute + consent in Settings.
