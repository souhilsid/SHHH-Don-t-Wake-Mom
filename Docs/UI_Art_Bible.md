# UI Art Bible — V8 (2026-11-09, Gate 3)

Replaces the V5 "night glass" UI and the oversized YOUR JOB / PRACTICE boards. Everything here is in the build and captured inside the running
game (`ui_capture`, `QA_Evidence/ui_v8/<tag>/`; desktop render of the XR camera — **headset look UNVERIFIED**).

## Direction
Sneaky bedtime toy box: chunky, physical, friendly. Every interactive thing is an object you could poke — bevelled toy keys, thick boards with a
moon-cream rim, illustrated cards — never flat floating rectangles. Quality references (feel only, nothing copied): chunky round VR icon buttons,
tall illustrated mode cards with a title band, 3D score signs.

## Tokens (brand kit v1)
| Token | Hex | Use |
|---|---|---|
| Midnight Ink | `#211530` | night background, text outline base |
| Deep Shadow | `#261239` | outlines, key wells, card frames, text outline |
| Royal Purple | `#70409F` | secondary keys, accents |
| Hush Yellow | `#FFCF2E` | primary action, titles, clock ring, stars |
| Moon Cream | `#FFF0BA` | board rims, secondary text |
| Sneaky Coral | `#F57F78` | quit / danger, final-minute clock |
| Party Pink | `#F45F73` | ribbon banners, route dots |
| Ribbon Red | `#C92C4D` | ribbon tails, Mom-awake meter |
| Soft White | `#FFF8E8` | body text on dark |
| Board plum (derived) | `#34204F` | board body |
`Palette` (Mats.cs) now carries these values, so every legacy call site picked them up; `UiKit` exposes them by name.

## Type
- **Luckiest Guy** (Apache 2.0) — titles, ribbons, big numbers (clock, points). Never retyped as the logo.
- **Fredoka SemiBold** (OFL) — key labels / calls to action (brand guide).
- **Poppins SemiBold** (OFL) — information text (rules, job names, stats).
- Rule in code: `MenuBuilder.T` picks Luckiest for size ≥ 0.5, Poppins otherwise; all text has a Deep Shadow outline.

## Components (Scripts/Game/UI)
| Component | What | Notes |
|---|---|---|
| `UiKit.Slab` | bevelled extruded rounded rectangle mesh | smooth bevel catches the toon ramp highlight; opaque; cached |
| `PokeButton` (V8) | dark well + thick bevelled cap + sticker icon + Fredoka label; ink/cream text auto-contrast | same poke + ray behaviour; hover lighten + grow; squash on press |
| `UiKit.Panel` | plum board + moon-cream rim, optional pink ribbon title | replaces GlassMesh (no stacked transparency) |
| `UiKit.Card` | illustrated card (`card_*`) in a frame, Luckiest title on the band, selectable glow | used for the four modes |
| `TitleSign` | approved logo (brand kit original PNG) on a hanging sign with ropes + fairy lights | whisper → wobble → settle intro, idle sway; reduced-motion static |
| `WristMenu` (V8) | clock dial + ring, Mom meter + state sticker, score, job rows, 7 icon keys (+ mic online) | palm-to-face only; final minute ring/clock go coral and pulse |
| `HouseMapView` | personal house map page on the wrist board | rooms from HouseLayout/RoomCatalog, my job pins, NavMesh route, Mom door state, noise ripple; no rival positions |
| `ResultsPodium` | 1st–4th toy blocks with siblings, crown, names/points | one end screen per match |
| `NightExterior` | painted night sky skybox + low-poly street ring + window treatment | ~6 draw calls (merged per material) |
| `SHHH/WindowView` shader | windows painted on solid walls sample the panorama by view direction | parallax at infinity, one texture sample, SPI-safe |

## Sources (all original, project-owned)
| Asset | Master | Export |
|---|---|---|
| 41 sticker icons | `DesignSources/UI_V8/svg/icon_*.svg` (generator `Tools/ui_v8_icons.py`, Inkscape-valid SVG) | `Tools/ui_v8_export.sh` → `png/` → `Resources/UI_V8/` |
| 8 mode / setting cards | `svg/card_*.svg` (`Tools/ui_v8_cards.py`) | 512×704 PNG |
| ribbon, timer face, meter frame, map paper | `svg/plate_*.svg`, `svg/map_paper.svg` | PNG |
| night sky panorama (gradient, clouds, stars, moon, far town) | `kra/SHHH_NightSky_Panorama.kra` (5 Krita paint layers, built by `Tools/ui_v8_kra.py` via kritarunner) from `src/sky_*.png` (`Tools/ui_v8_sky.py`) | 4096×2048 equirect → skybox + WindowView |
| logo | brand kit `SHHH_Original_Approved_Transparent.png` (approved master) | `Resources/UI_V8/logo_primary.png` |
Import rules: `ShhhUiV8` postprocessor (clamp, mips, ASTC 4×4 UI / 6×6 sky). Materials cloned at runtime come from `Resources/UI_V8/Mats/*.mat`
so their shader variants ship in the APK (`ui_v8_setup`).

## Layout rules
- Boards are world-anchored at the lectern pose (0.56 m, chest height, 18° tilt, 72 %); the front end stands in the real living room facing the
  moonlit windows, title sign over the sofa, Family Stage to the right.
- Minimum key 34 mm (wrist), 85 mm (boards); minimum body text 0.16 TMP size on the wrist (≈ 9 mm cap height at arm's length).
- One primary action per board (Hush Yellow); destructive actions in coral; icon + word on every key except the wrist row (icons only, 7 keys).
- The wrist page shows only fair information: own score, own jobs, own position. Rivals' positions never.

## Known gaps / next
- Headset legibility and comfort: UNVERIFIED (desktop renders only).
- Intro board and confrontation tray inherit the V8 board/keys but keep their V7 layouts.
- Character look on the Family Stage is the existing detached-limb sibling rig (Gate 7 scope).
