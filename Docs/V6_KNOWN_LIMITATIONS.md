# SHHH! V6 Alpha — known limitations and hardware-unverified risks

No Quest was connected during V6 development (by design, `SHHH_V6_OFFLINE_OVERRIDE.md`). Everything below the first table
has automated evidence on the PC; nothing has been seen, heard or felt in a headset yet.

## Hardware-unverified (cannot be proven without the headset)
| Area | What was verified offline | What only the Quest test can show |
|---|---|---|
| Hand tracking feel (frequency LOW = OS default; HIGH is a one-line change in `ConfigureOvr` if grabs feel laggy, at some Quest 2 cost) | `xr_diag` drives the real `HandInteractor`/`PropBody`/`AnchorFixture`/`PokeButton` with simulated hands: 149/150 pass | real tracking jitter, pinch detection thresholds on real fingers, palm-up walk gesture sign, comfort |
| Grip alignment | numeric hand-object offsets (snap of closest surface point into the fingers) | how items look in the hand |
| Frame rate | editor 63–90 fps; lean URP (no shadows/HDR, 4× MSAA), reduced Astra meshes, no extra realtime lights | Quest 2 ≥ 72 fps — **main risk: the cartoon post-processing pass + 5 skinned characters + dressing**. If it drops, the first levers are: disable post grading, halve dressing, lower MSAA to 2× |
| Audio | files load, every key mapped, levels measured | loudness balance in the headset, spatial falloff, whether the lullaby is too quiet/loud |
| Microphone / voice | Photon Voice joined and decoded tone audio all-to-all across 2–4 independent processes | Android RECORD_AUDIO prompt flow, real mic quality, echo, positional voice on Quest |
| Online on Quest | real Photon Cloud with Windows clients, 0 desyncs in 7 full matches | Wi-Fi latency, Quest ↔ Quest, Quest ↔ PC |
| Quest 3 / 3S | build targets set (Quest 2/3/3S, arm64, OpenXR + Meta Quest feature) | actual runs on those devices |

## Functional limitations (known, documented, not hidden)
- **No host migration:** if the host leaves, the match ends for everyone with a clear message.
- A seat handed to a bot mid-round (disconnect) stays idle until the next room; the bot plays from the next room on.
- On clients, the placement rings on anchors (occupied/free colour) only update on the host; chore boards, scores and the
  results are correct everywhere (they come from the replicated rules journal).
- Mid-match disconnect/rejoin is covered by Core tests and code paths but not yet by an independent-client run.
- Nell's walk path can clip into the bookshelf at the shelf anchor (cosmetic).
- One `xr_diag` case fails by design: pinching 0.35 s too early above a cluttered table grabs the neighbour (a book) instead of
  the intended cup. Real players will sometimes do this; the item can simply be dropped back.
- Facial animation is clip-driven head/body acting only; there are no blend-shape faces or lip sync.
- Spectating is a board + free look; no spectator camera modes.
- Mom's voice is TTS-based (Piper, LibriVox-trained public voice) with performance processing — not a voice actor.
- Mom's sabotage remarks and some VO lines are English only (the game is English only).
- The Windows desktop build (`Builds/Win64`) exists only as an automated network test client; it is not a product.

## Not done from the V6 master target (honest gap list)
- Blend-shape facial animation / visemes for siblings and Mom.
- External human playtest of fun/pacing (only bot and scripted-client matches so far).
- Meta Alpha upload: needs the user's Meta developer login (see `Docs/V6_RELEASE.md` → Alpha distribution).
