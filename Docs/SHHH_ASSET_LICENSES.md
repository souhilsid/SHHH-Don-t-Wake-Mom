# SHHH! asset licenses (checked 2026-11-10)

Every license below was read from the file inside the archive in this session (not assumed). Detailed per-object rows:
`Docs/SHHH_ASSET_IMPORT_MANIFEST.csv`. Older audit: `Docs/Asset_Source_Licenses.csv`.

| Source | License (as read) | Commercial game use | Redistribute as assets | AI/ML training | Attribution |
|---|---|---|---|---|---|
| Daycare & Everyday Interiors — 360 3D Props (kimchistan) | CC0 1.0 (LICENSE.txt, CC0-1.0.txt) | yes | yes | yes | not required (credit kimchistan) |
| PtDesigns — Colorful Laundry & Utility (household_laundry_utility_v1.0) | CC0 1.0 (LICENSE.txt) | yes | yes | yes | not required |
| Tiny Treats — Charming Kitchen 1.1 (Isa Lousberg) | CC0 1.0 (License.txt) | yes | yes | yes | not required (credit appreciated) |
| KayKit — Restaurant Bits 1.0 (Kay Lousberg) | CC0 1.0 (License.txt) | yes | yes | yes | not required (credit appreciated) |
| Ultimate House Interior Pack (Quaternius) | CC0 1.0 (License.txt) | yes | yes | yes | not required |
| Nasha's Cozy 3D: Kids Room v1.0 | Custom (LICENSE.txt) | yes, inside a larger work | **NO** | **NO** | not required |
| NewLua Studios Stylized Food Pack | Unity Asset Store EULA (to confirm inside Unity before import) | yes (Asset Store EULA) | no (as assets) | — | — |
| Free Pack - Cartoon Dog, Cat (PolyOne Studio, added by the owner 2026-11-11) | Unity Asset Store EULA (Standard) | yes (Asset Store EULA) | no (as assets) | — | not required |
| Installed Asset Store VFX/model packages | Unity Asset Store EULA | yes | no (as assets) | — | — |

Credits to show in-game: kimchistan, PtDesigns, Isa Lousberg (Tiny Treats), Kay Lousberg (KayKit), Quaternius, Nasha; music instruments: MuseScore_General (S. Christian Collins, Michael Cowgill, Frank Wen, Ethan Winer, Michael Schorsch).
Conversions: GLB → FBX with `Tools/glb2fbx.py` (Blender 5.2 bpy); source GLBs kept unchanged in `ArtSource/V9_Staging/` on the PC.

## Audio instrument samples (V13 music / stingers / UI, 2026-11-15)
- **MuseScore_General.sf3 0.2**: MIT. Downloaded 2026-11-15 from ftp.osuosl.org/pub/musescore/soundfont/MuseScore_General/,
  39,900,972 bytes, sha256 5b85b6c2c61d10b2b91cddd41efcce7b25cd31c8271d511c73afafbef20b6fa3. The licence is kept next to it in
  `Tools/_ext/soundfont_dl/MuseScore_General_License.md`. Only the rendered audio ships. Required credit (put it in the game credits):
  FluidR3 (c) 2000-02 Frank Wen; FluidR3Mono (c) 2014-17 Michael Cowgill; MuseScore_General adaptation (c) 2018-19 S. Christian Collins;
  Temple Blocks (c) 2002 Ethan Winer; Drumline Cymbals (c) 2016 Michael Schorsch.
- **FluidSynth 2.6.2** (win10-x64 release from github.com/FluidSynth/fluidsynth, LGPL-2.1). An offline rendering tool only; not shipped.
  zip sha256 808295345d43c9c428f3fa87d29fbf97a9926f356df95559ff2689f3bd4072d7.
- **mido** (MIT, pip). A MIDI writing library for the generator script; not shipped.

## Music tracks (owner-supplied, 2026-11-15) - Kevin MacLeod, incompetech.com, CC BY 4.0
Used as the game's music (V13): menu = "Fluffing a Duck"; match rotation = "Marty Gots a Plan", "Sneaky Adventure", "Investigations";
final minute = "Scheming Weasel (faster version)". CC BY 4.0 allows commercial use and the store; **attribution is required** (in-game credits
and the store description), one line per track:
- "Fluffing a Duck" Kevin MacLeod (incompetech.com) Licensed under Creative Commons: By Attribution 4.0 License http://creativecommons.org/licenses/by/4.0/
- "Marty Gots a Plan" Kevin MacLeod (incompetech.com) Licensed under Creative Commons: By Attribution 4.0 License http://creativecommons.org/licenses/by/4.0/
- "Sneaky Adventure" Kevin MacLeod (incompetech.com) Licensed under Creative Commons: By Attribution 4.0 License http://creativecommons.org/licenses/by/4.0/
- "Investigations" Kevin MacLeod (incompetech.com) Licensed under Creative Commons: By Attribution 4.0 License http://creativecommons.org/licenses/by/4.0/
- "Scheming Weasel (faster version)" Kevin MacLeod (incompetech.com) Licensed under Creative Commons: By Attribution 4.0 License http://creativecommons.org/licenses/by/4.0/
