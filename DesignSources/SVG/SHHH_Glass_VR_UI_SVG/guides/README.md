# SHHH! — Glass VR UI (SVG Design Library)

**Contains 53 complete journey-state mockups, 75 standalone UI-component SVGs, and 18 vector icon SVGs.**

This is a comprehensive **visual and interaction design library**, **not** a working Unity UI or a stereoscopic VR application. Each scene SVG is a 1600×900 *illustrated viewpoint* to communicate where UI objects sit in the room. Objects are deliberately not head-locked.

## Folder map
- `screens/` — Full main scenes and sub-states, covering initial launch, private play, avatars, Mom, three rounds, tasks, pets, sandbox sabotage, fairness investigation, spectators, results, settings, lost tracking and errors.
- `components/` — Truly standalone editable SVG units with transparent canvas, grouped by use.
- `icons/` — Small transparent SVG icons.
- `brand/SHHH_logo_true_vector.svg` — **True-vector recreation** of the approved logo's visual identity. Typography and geometry are approximations.
- `brand/SHHH_approved_exact_raster_inside_SVG.svg` — **Pixel-exact approved original logo**, embedded as PNG data **inside SVG**. This is SVG-container format, **not vectorized**. Use when exact branding matters.
- `guides/ui_manifest.json` — Full inventory and brand tokens.
- `guides/screens_index.csv`, `guides/components_index.csv` — Per-screen/component inventory.

## Locked SHHH! world rules
1. Competitive household cleaning; only **one sibling wins**, not a team.
2. Four players → three → two → one; exactly one elimination per round.
3. Round 1 living room; round 2 kitchen; round 3 laundry, with separate Unity room scenes.
4. Mom wakes and eliminates the attributable noise origin; otherwise she inspects chore contribution.
5. Sandbox sabotage using household physics, with causal responsibility, valid counterplay, and anti-griefing.
6. Independent roaming cat/dog, occasional incidents.
7. Private online Photon lobby; solo practice; English-only; hand-tracked locomotion with optional teleport/snap turn.
8. Spatial VR UI in physical rooms, no permanent flat overlay.

## Claude / Unity implementation notes
- The SVGs are **authored design sources** for layout and individual icons/components. Do not import full-scene SVG screenshots as VR panoramas or textured full-screen panels. Build actual 3D room geometry with sourced FBX assets.
- Rebuild live elements as world-space canvases, TextMeshPro labels and actual 3D interactable objects. Treat vector scene art as a layout reference.
- Use SVG importer or rasterized sprites/materials after testing. Gradients, transparency, SVG filters and nested drop shadows may not map 1:1 to Unity vector renderer. Prefer runtime shaders / meshes for translucent glass, UI backing, outline and bloom.
- Use `ui_manifest.json` to preserve names, palette and states.
- Use game-state conditions from `SHHH_GAMEPLAY_DESIGN_LOCK_V3.md` for authoritative logic; SVG screen IDs are **visual design states**, not separate Unity scene files.
- English only. No HARRIK city elements, coin reward screens, currency, or team victory.

## VR placement starting values (to validate on Quest 3)
- Main/menu board: ~1.5–1.8 meters away, roughly 0.85–1.1m wide.
- Smaller action card: 1.0–1.5 meters away, ~0.30–0.55m wide.
- Active in-world taskboard should be readable via physical approach or hand summoned board.
- Critical alerts emerge at the object location. Physical clock and bedroom sleep-state cue live in room.
- Avoid forced head rotation, avoid dark low-contrast text against transparent moving environments.

## Font
Editable text uses Trebuchet MS / DejaVu Sans / Arial fallback. Fonts are **not embedded or included**, by design. Claude should choose a licensed typeface and use TextMeshPro dynamic text in Unity.
