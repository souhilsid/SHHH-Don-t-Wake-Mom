# V6 — AUD-G6 sound pass

**Status:** WORKS IN EDITOR ONLY (all files load, every gameplay key is mapped, levels measured). **Hardware-unverified:** how
it sounds through the Quest speakers / headphones, and the spatial falloff in the headset. No human has listened to it yet.

## What changed
- `Tools/synth_audio_v6.py` (new, original, procedural; numpy + scipy; seed 20261108, reproducible) replaces the V5 set key
  for key, so no game code depends on the new files. 32 kHz mono 16-bit, peak-normalised per key, 89 files.
  - **Material Foley**: every impact is contact click + modal body (measured-style partial ratios per material) + a smaller
    second bounce 50–110 ms later + a short small-room early-reflection tail. **6 variants** per material (V5 had 4), no
    immediate repeats (AudioBank) plus ±6 % pitch jitter.
  - **`impact_metal` existed in the rules but had no clips before V6 → metal props (pan, fork) were silent.** Fixed.
  - Toys have an inner rattle; paper has a page flutter after the slap; soft/fabric are low-passed thumps.
  - UI is a soft wooden click + marimba family (step done = one note, job done = rising triad, trap set = falling pair,
    countdown = three ticks + resolve).
  - Mom: comic bassoon-like wake stinger (not scary), warm door creak/close, filtered two-phase snore.
  - Pets: dog bark/huff, cat meow (pitch-glide formant).
  - **Menu music**: a quiet 24 s 3/4 celesta lullaby (F major) with a soft bass and pad; rooms stay music-free by design
    (silence is part of the game).
  - Night ambience: very low room tone, sparse distant crickets, a soft hall-clock tick.
  - **`bot_giggle`** (new): muffled "tee-hee" for the bot sabotage wind-up tell.
- `Tools/vo/gen_mom_vo.py`: names list now matches the Core roster (adds **Nell**, who replaced Juno); `--names-only` mode.
  `vo_name_nell_1.wav` generated with the same Piper voice + chain as the other 57 VO clips.
- Settings board: three columns — **Music −/+/on-off, Sounds −/+, Voices −/+** (each press plays a sample on that bus so the
  level is heard), Snap 30/45, Walk/Teleport, Seated/Standing, Skip intros, **Mic on/off** (Photon Voice transmit; never a noise
  input). Status line shows all levels. Values persist (`Settings.Save`).

## Sound-event audit (every key the code requests → file present)
| Key | Used by | Files |
|---|---|---|
| impact_{soft,fabric,paper,wood,plastic,toy,ceramic,glass,metal,appliance} | `RoomFx` impacts (all 10 `NoiseMaterial`s) | 6 each |
| squeak | dog/cat toy impacts | 3 |
| ui_click / ui_hover | `PokeButton` | 1 / 1 |
| grab | `HandInteractor` | 1 |
| step_done / chore_done | `DutyVisuals`, anchors, foil | 1 / 1 |
| trap_set, countdown | traps, swipe alert, rush | 1 / 1 |
| mom_snore, mom_babble, mom_wake_stinger | `MomCharacter` | 1 / 4 (V5 babble kept) / 1 |
| door_creak, door_close_soft | Mom's door | 1 / 1 |
| dog_bark, dog_huff, cat_meow | `PetBody` | 3 / 1 / 2 |
| music_menu, amb_night | front end / rooms | 1 / 1 |
| win_you, win_other, elim_you, elim_other | results / eliminations | 1 each |
| bot_giggle | sabotage tell | 3 |
| vo_* (57 + 9 names) | Mom English VO (V5, Piper) | unchanged + Nell |

Editor evidence: `AudioBank loaded 91 sound groups` (V5: 89), `QA_Evidence/xr_diag_20261108_0839.json` (Settings rows pressed by
poke and ray), spectrogram check of 12 keys during generation (no silent/NaN files; snore and ambience low-passed after the check
showed hiss). Levels: `Docs/AUDIO_V6_REPORT.txt` (seconds + RMS per file).
