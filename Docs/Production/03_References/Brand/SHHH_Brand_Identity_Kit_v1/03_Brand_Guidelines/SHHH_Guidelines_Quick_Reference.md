# SHHH! — Brand Identity Guidelines (v1.0)
**Date:** 8 November 2026  
**Game:** SHHH! — Don't Wake Mom!  
**Creative territory:** Sneaky cartoon / nighttime party comedy.

## Brand positioning
Quiet is the setup; chaos is the punchline. SHHH! is a mischievous VR family party game where siblings try to keep Mom asleep while chores, alliances and sabotage collide. The brand is friendly, lively and physically playful, not scary or aggressive.

## Hero identity
Use the approved full-color illustrated SHHH! + DON’T WAKE MOM! lockup as the default in marketing, title screens and trailers. Preserve the three H characters, exclamation mark, illustration and subtitle. **The title lettering is illustrated custom lettering inspired by Luckiest Guy, not a live font that can be retyped to reproduce the exact artwork.**

## Logo variants
- `SHHH_Primary_FullColor_TRUE_VECTOR.svg`: actual SVG path shapes traced from the approved raster. Editable as color-grouped geometry. **Approximation:** some tiny curves and smooth gradients become color bands, not native editable gradient surfaces.
- `SHHH_Exact_Look_PNG_Embedded.svg`: an SVG wrapper containing the original PNG. Pixel-identical appearance; **not a true editable vector**.
- `SHHH_Primary_Flat_TRUE_VECTOR.svg`: fewer colors for simplified illustrations.
- `SHHH_Monochrome_Cream_TRUE_VECTOR.svg` and `SHHH_Monochrome_Ink_TRUE_VECTOR.svg`: single-color silhouette variants.
- `SHHH_Shush_Mark_TRUE_VECTOR.svg` and `SHHH_Moon_Mark_TRUE_VECTOR.svg`: newly authored independent vector motifs for small UI icons and stickers.
- `SHHH_Editable_Font_Layout.svg`: editable text demo requiring Luckiest Guy and Fredoka fonts; **not the approved master artwork**.

## Clear space and minimum size
- Reserve at least 8% of logo width on every side; 10% is recommended for promotional hero art.
- Use the entire lockup at **320 px wide or larger** when possible. Test subtitle legibility on actual headset.
- At small sizes, switch to the compact shush icon, ideally **48 px or larger**.
- Never stretch nonuniformly or crop off the subtitle for a supposed primary lockup.

## Color system
- **Midnight Ink** `#211530` — Primary game-night background.
- **Deep Shadow** `#261239` — Outlines and deep contrast.
- **Royal Purple** `#70409F` — Extrusion and energetic layers.
- **Moon Cream** `#FFF0BA` — Secondary letter highlights.
- **Hush Yellow** `#FFCF2E` — Primary title color / joyous accent.
- **Sneaky Coral** `#F57F78` — Small accent edges.
- **Party Pink** `#F45F73` — Banners, transitions, energy.
- **Ribbon Red** `#C92C4D` — Secondary ribbon / emphasis.
- **Soft White** `#FFF8E8` — Accessible light surfaces.

Color values are normalized sRGB tokens derived visually from the approved illustration. The richly shaded source artwork uses many intermediate tones; do not force every original pixel to match these nine tokens.

## Typography
- **Luckiest Guy** — primary expressive display inspiration and optional campaign headline font, regular 400. Official listing: https://fonts.google.com/specimen/Luckiest+Guy — Google's repository identifies license as **Apache 2.0**.
- **Fredoka** — game menu labels, CTA, secondary headlines, 600–700. https://fonts.google.com/specimen/Fredoka — **SIL OFL 1.1**.
- **Nunito Sans** — informative UI, accessibility text and instructions; do not use ultra-thin weights. https://fonts.google.com/specimen/Nunito+Sans .
- Do not assume the SVG full-color master consists of editable text. The primary artwork was generated as an illustration and subsequently traced.
- The enclosed archive does **not** distribute any font files. Download from the linked official sources if you need them.

## Supporting motifs
Use a compact shush hand, moon/crescent, tiny stars/sleepy Z and rounded ribbon shape. These are accents, not competing alternative hero logos. Avoid excessive stars and decorative particles.

## Backgrounds
The full logo reads best on midnight ink `#211530`, deep purple, or warm cream; verify contrast at thumbnail scale. Avoid busy high-frequency imagery directly behind the wordmark. A dark vignette plate is preferable in cinematic scenes.

## Do not
- Distort, flatten or retype the existing logo to claim equivalence.
- Omit the exclamation point or change the count of H letters.
- Remove the strong outer outline on light or busy backgrounds.
- Use the full logo as a tiny menu icon where the subtitle disappears.
- Treat the illustrative Mom or shush icon as an official in-game character asset without checking the source character art bible.

## Motion
Proposed ~1.8 s intro: soft whisper reveal (0–0.4 s) → sudden wobble (0.4–0.85 s) → freeze at offscreen crash (0.85–1.35 s) → quiet settle (1.35–1.8 s). For reduced-motion mode, use a static fade. UI audio must not be loud or startle sensitive audiences.

## Production and limitations
Original approved file is 1448 × 1086 transparent PNG. The traced SVG is a color-posterized approximation with real editable paths. Small vector geometry may be jagged at extreme zoom; if final print requires pristine strokes, a human illustrator should rebuild the original source as semantic vector layers. Trademark/name/logo clearance has not been performed. Check platform artwork requirements and test on Quest hardware before release.
