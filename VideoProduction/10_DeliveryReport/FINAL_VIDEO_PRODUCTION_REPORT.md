# SHHH! — Don't Wake Mom! · Final video production report (2026-11-17)

## Deliverables (`VideoProduction/09_FinalExports/`)
| File | What | Length | Status |
|---|---|---|---|
| `SHHH_Official_Game_Trailer_1080p.mp4` | Official cinematic trailer | 1:43.1 | EXPORTED + TECH QA PASSED |
| `SHHH_Official_Game_Trailer_1080p.en.srt` | Caption sidecar (narrator + Mom lines; not burnt in) | 26 cues | done |
| `SHHH_Official_Gameplay_Walkthrough_1080p.mp4` | Authentic first-person Quest walkthrough | __WT_LEN__ | __WT_STATUS__ |
| `SHHH_Official_Gameplay_Walkthrough_chapters.txt` | YouTube chapter list for the description | | done |
| `YOUTUBE_DESCRIPTIONS.md` | Description text incl. the required music credit | | done |

Both: 1920×1080, 16:9 landscape only (no vertical/Shorts), H.264 High yuv420p TV range BT.709, 30 fps CFR (sources are ~30 fps VFR
Quest captures), AAC-LC 48 kHz stereo, `+faststart`.

## Technical QA (measured, `Tools/qa_export.sh`; full output in `07_Trailer/QA/` and `08_GameplayWalkthrough/QA/`)
| Check | Target | Trailer | Walkthrough |
|---|---|---|---|
| Integrated loudness | −14 to −16 LUFS | **−13.9 LUFS** | __WT_LUFS__ |
| True peak | ≤ −1 dBTP | **−1.5 dBTP** | __WT_TP__ |
| Video | H.264, yuv420p, 1920×1080 | H.264 High, yuv420p, 1920×1080, 30/1 | __WT_VID__ |
| faststart (moov before mdat) | yes | yes | __WT_FS__ |
| Black > 0.5 s | only intentional | 0.00–1.43 s fade-up from black (intentional) | __WT_BLACK__ |
| Frozen picture > 4 s | none | none | __WT_FREEZE__ |
| Voice intelligibility in the mix | script = heard | speech recognition (faster-whisper) on the MIXED master at each line: mean word match 0.93; the two lower scores are the game's own Mom clips (`stir_1`, `wake_1`), which transcribe the same way on their own | n/a (game audio) |
| Contact sheet review | | `07_Trailer/QA/trailer_sheet_01-02.jpg` (1 frame/s, viewed) | `08_GameplayWalkthrough/QA/walkthrough_sheet_*.jpg` |

What QA could **not** cover: nobody has listened to either export (I cannot hear audio; levels, ducking and speech were checked by
measurement and speech recognition only). The owner should watch both once on a TV/headphones before upload.

## How the trailer was made
- **Story (1:43):** the house at night → "Mom is finally asleep" → you and your siblings have one job → **EVERY CHORE.** (slippers,
  mugs, towels, plates, kitchen, washer; jobs in every room + finger-gun hop) → **NOT ONE SOUND.** → the practice book slips (real
  cartoon noise bursts) → "...Mostly." → pets, siblings, noise rings, Mom's meter → MOM IS UP → siblings near your jobs, Mom's
  sabotage line → **DON'T GET BLAMED.** → final inspection, "Who messed with Nell's job?", "Aha! It was you." → results (a bot wins,
  you're 2nd) → "Goodnight. And keep it down." → "Shhh. Don't wake Mom." → logo hold + snore.
- **Picture:** 33 shots, all real Quest gameplay recordings (F04 Nov 11 c13, F05 Nov 12 c15, F06 Nov 15 c20/21, F07 Nov 17 c21).
  Remotion composition `ShhhTrailer` (`SHHH/Remotion/src/remotion/shhh/`), driven by `07_Trailer/build_trailer_timeline.py`.
  Only image change: a slow, top-anchored push-in (zoom 1.04–1.30) that keeps the scene but crops most of the development HUD strip at
  the bottom. No speed ramps, no spins, no flashes added; the noise rings / bursts are the game's own feedback.
- **Text:** three phrase cards (EVERY CHORE. / NOT ONE SOUND. / DON'T GET BLAMED.), the approved logo and one tagline line
  ("A VR comedy about very quiet chores"). No lower thirds, labels, section numbers or watermark. Fonts: Luckiest Guy (OFL) and
  Fredoka (OFL) from the project's brand fonts. Logo: the approved transparent game logo (`DesignSources/References/
  SHHH_Approved_Logo_Figma_Import.svg`, embedded PNG, identical to the in-game `shhh_logo.png`). No new logo was made.
  Note: `Approved_Logo_Guideline.png` (Astra V4 deck) shows a different "sleeping Mom" lockup; the in-game logo was used because it
  is the one the game ships with - the owner may want to confirm which is primary.
- **Voices:** narrator = Kokoro-82M TTS (`am_michael`, offline, Apache-2.0), written and generated for the trailer, presented as a
  trailer narrator - never as game dialogue. Mom = the game's own shipped VO files (unchanged). Siblings = the game's own
  `bot_giggle` sounds (the bots have no spoken lines in the game, so they get none here). ElevenLabs needs an account authorization
  that is not available - not used.
- **Sound design:** game SFX assets placed exactly on the on-screen events they belong to (two wood impacts on the two noise bursts,
  meow/bark on the MEOWS/WOOF warnings, Mom wake stinger on MOM IS UP, snore on the logo). Stems: `06_SoundDesign/stems/`.
- **Music:** Kevin MacLeod, CC BY 4.0 (`04_MusicAndLicenses/MUSIC_LICENSE_MANIFEST.md`) - Sneaky Adventure → Scheming Weasel
  (faster) → Investigations → Sneaky Adventure. Licence checked at incompetech.com; attribution block must go in the YouTube description.

## How the walkthrough was made
Chronological player journey, picture and game audio unaltered (no zoom, no speed change), short fades only at cuts:
logo intro → GET READY (c21 Pick a mode + Genies wardrobe) → LEARN THE MOVES (Settings, How to win, Practice; the c15 book-drop
noise lesson) → THE MATCH (c21 10-minute solo match **uncut**, through final inspection, accusation and results) → ANOTHER NIGHT
(Nov 15 5-minute solo match through results) → PLAY WITH FRIENDS (creating a private room; empty seats filled by bot siblings) → logo.
Builder: `08_GameplayWalkthrough/build_walkthrough.py` (cards rendered by Remotion `WalkthroughCard`, joined by FFmpeg, two-pass
loudness normalisation).

## Honesty rules - how each was met
- Existing recordings only; Unity was not used for any capture; no new gameplay recorded; nothing staged or fabricated.
- The four siblings in every match are **bots** (Solo vs 3 bots); the narration says "you and your siblings" and the results board
  shows "(BOT)". No shot implies four human players.
- **No multiplayer claim in the trailer.** The walkthrough only shows the Play with friends menu/room as it exists; online play across
  regions is **not verified** (c21 findings: Qatar↔Algeria failed; region fix in c22 untested on two headsets).
- No release date, store listing, price, award, review score or platform-availability claim anywhere.
- Rejected material: the `SHHH_HERO_ANIMATION_TEST_V3.mp4` animation (not gameplay, old art), the early-alpha Nov 7 recordings, the
  8 s dark load, head-in-wall frames (`00_ProductionAudit/SHOT_SELECTION_DATABASE.csv`).
- No reference-trailer footage, sound or music is in either export (`03_ReferenceResearch/REFERENCE_NOTES.md` lists what was
  actually inspected; five long references were never fully downloaded and were **not** inspected).
- Known authentic blemishes left in: development captions ("Edge of your play space…", task strip, hint line) are part of the
  recorded game. The trailer crops most of them; the walkthrough shows them as recorded. The c21 match runs at 34–51 fps on device,
  so head turns judder in places - that is the real recording.
- Speech check of the c21 recording (`08_GameplayWalkthrough/QA/F07_speech_transcript.txt`): only the game's Mom lines, pet sounds
  and giggles were found - no player-microphone speech detected (speech recognition, not a listen).

## Source files and tools
- Audit: `00_ProductionAudit/FOOTAGE_MANIFEST.csv` (ffprobe data + content review per recording, duplicates noted),
  `ContactSheets/` (1 frame / 4 s + scene-change lists), `SHOT_SELECTION_DATABASE.csv`, `BEST_MOMENTS_SHORTLIST.md`.
- Brand: `02_BrandAssets/` (logos, colour tokens, OFL fonts). Voice: `05_Voiceover/` (script, narration WAVs, Mom VO transcripts).
- Tools: `Tools/contact_sheets.sh`, `shot_rows.py`, `gen_narration.py`, `asr_check.py`, `asr_windows.py`, `asr_long.py`,
  `music_analysis.py`, `qa_export.sh`; Python venv `Tools/tts_venv` (kokoro-onnx, faster-whisper, librosa) with models in `Tools/tts_models`.
- Originals untouched: all Quest recordings stay in `C:\Users\aisci\Downloads\` (read-only use); game project, VO bank and brand
  sources were only read.

## Open items for the owner
1. Watch/listen to both exports once (not possible for me).
2. Paste the description text (with the Kevin MacLeod credit) from `09_FinalExports/YOUTUBE_DESCRIPTIONS.md`.
3. Optional: confirm the primary logo lockup (in-game house/moon logo was used).
4. A new Quest recording of c23 (fridge, washer jobs, soap pump, hints, no dev captions) would make a stronger trailer v2 - not done,
   because this brief allows existing recordings only.
