# SHHH! — Don't Wake Mom! · Trailer voiceover script (v1, 2026-11-17)

Three voice sources, kept apart on purpose:

| Voice | Source | What it is |
|---|---|---|
| **NARRATOR** | Kokoro-82M TTS (`am_michael`, offline, Apache-2.0 model + voices), generated for this trailer only | Trailer narration. It is **not** in the game and is never presented as game dialogue. |
| **MOM** | The game's own shipped VO bank `UnityProject/Assets/SHHH/Resources/Audio/VO/vo_mom_*` (Piper TTS, public-domain LJ-style voice, see `Tools/vo/`) | Real in-game Mom lines, used unchanged (level only). |
| **SIBLINGS** | The game's own `bot_giggle_1..3.wav` | Real in-game bot-sibling reactions. The siblings have no spoken lines in the game, so the trailer gives them none. |

Tone: warm, sly bedtime-story narrator; deadpan on the jokes; never shouting (Mom does the shouting).
On-screen text is limited to four short phrase cards plus the logo (see `07_Trailer/Storyboard/EDIT_DECISION_LIST.md`).

## Lines (in order)

| ID | Who | Line | Over |
|---|---|---|---|
| N01 | NARRATOR | It's late. | night living room (Nov 11 build) |
| N02 | NARRATOR | The house is a total mess... | stairs / balcony at night |
| N03 | NARRATOR | ...and Mom is finally asleep. | warm lamp, bookshelf |
| M01 | MOM (vo_mom_stir_1_1) | Mmm... five more minutes. | the dog asleep in its bed |
| N04 | NARRATOR | So tonight, you and your siblings have one job. | wardrobe avatar, siblings in the hall |
| CARD | — | EVERY CHORE. | |
| N05 | NARRATOR | Pair up the slippers. Stack the mugs. Fold the towels. Scrub the plates. | chore montage (each verb on its shot) |
| NR | NARRATOR | Jobs in every room... and one clock for everybody. | wrist house map, kitchen, laundry, finger-gun hop |
| CARD | — | NOT ONE SOUND. | |
| N06 | NARRATOR | ...without making a single sound. | practice table, book in hand |
| SFX | game | BANG (the book drop, real practice-mode noise burst) | Nov 12 practice 2:07 |
| N07 | NARRATOR | ...Mostly. | |
| M02 | MOM (vo_mom_investigate_2_1) | Something fell. Something LOUD. | |
| N08 | NARRATOR | The cat is bored. The dog is hungry. Your siblings are... not helping. | MEOWS / WOOF warnings, sibling with a book |
| S01 | SIBLING (bot_giggle) | (giggle) | |
| N09 | NARRATOR | And every little noise wakes Mom up a little more. | noise rings, wrist Mom meter |
| M03 | MOM (vo_mom_almost_3_1) | I am getting up. I am really getting up. | wrist: MOM IS UP |
| M04 | MOM (vo_mom_wake_1_1) | WHO is making that noise?! | "MOM IS UP!" in the living room |
| M07 | MOM (vo_mom_sabotage_1_1) | Why are things moving around out there? | siblings near your jobs (sabotage) |
| CARD | — | DON'T GET BLAMED. | |
| N10 | NARRATOR | And when she finds a mess... somebody's getting blamed. | "MOM: Who messed with Nell's job?" |
| M05a | MOM (vo_mom_question_1_1) | Alright... who did this? Show me. |
| M05 | MOM (vo_mom_caught_4_1) | Aha! It was you. | accusation board |
| S02 | SIBLING (bot_giggle) | (giggle) | |
| M06 | MOM (vo_mom_win_1) | ...the quietest, most helpful kid in this house. I'm SO proud. Now everybody... SLEEP! | results board |
| M08 | MOM (vo_mom_bed_1_1) | Goodnight. And keep it DOWN. | living room wide |
| N11 | NARRATOR | So be quick. Be helpful. And whatever you do... | night living room |
| N12 | NARRATOR (soft) | Shhh. Don't wake Mom. | logo reveal |
| SFX | game (mom_snore_1) | (snore) | logo hold |

**Verification (2026-11-17):** every narrator file and every Mom file used was transcribed with faster-whisper base.en
(`Tools/asr_check.py`, `game_mom_vo_transcripts.tsv`). The game's Mom VO files were re-recorded after the old
`Tools/vo/VO_MANIFEST.json` (e.g. `suspicious_4` now says "I'm awake-ish, don't test me"), so lines were chosen from the transcripts, not the old list.

Captions for all lines: `09_FinalExports/SHHH_Official_Game_Trailer_1080p.en.srt` (sidecar only, never burnt in).
