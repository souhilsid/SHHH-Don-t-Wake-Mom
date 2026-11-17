# GAME DESIGN v4 (supersedes Gameplay Design Lock v3 where they conflict)

## v4 overrides applied
- **Solo is a full competitive match**: 1 human + 3 bots, offline, Living Room → Kitchen → Laundry, 4→3→2→1. Practice/tutorial is a separate optional mode (Mom only warns there).
- **Online private matches always have FOUR seats**: 2H+2B, 3H+1B, 4H+0B. A lone host may wait in the lobby; starting alone fills bots. No public matchmaking. (v3's "2–3 players start later rounds" is SUPERSEDED.)
- **Bots are real competitors** (host-simulated, same prop/anchor APIs as hands, can win, can be eliminated). Lobby/nameplates always show HUMAN vs BOT truthfully.
- Full creative freedom on art/UI/logo (logo currently the supplied PNG; redesign pending).

## Kept from v3 (user-locked)
Independent competition; immediate noise elimination of the responsible sibling; quiet timer / all-chores-done inspection eliminating the least helpful; sandbox sabotage; trap-setter vs careless-victim rule; separate scene per round; shared + private bonus chores; independent cat & dog; spectators with harmless reactions; hand-gesture locomotion + optional teleport/snap turn; animated cartoon Mom.

## Round defaults (tunable, `RoundConfig.DefaultSequence`)
| Round | Room | Duration | Shared chores | Noise × | Meter baseline | Pet activity |
|---|---|---|---|---|---|---|
| 1 | Living Room Panic | 150 s | ≤8 (capped so every sibling gets a bonus) | 1.00 | 8 | 0.7 |
| 2 | Kitchen Chaos | 130 s | ≤7 | 1.15 | 14 | 0.85 |
| 3 | Laundry Final Cleanup | 110 s | ≤5 | 1.30 | 20 | 0.95 |

End precedence per tick: one survivor → qualifying wake → all shared chores done → timer.

## Scoring (inspection)
`Score = Shared + Bonus(12) + Rescue(+5, cap 15) − 0.35 × AttributedNoise − Idle(−10/episode, cap 20)`.
Ties: lower shared loses → higher noise loses → fewer validated steps loses → 20 s tie-break challenge → deterministic fallback (previous cumulative score, then seat number).
