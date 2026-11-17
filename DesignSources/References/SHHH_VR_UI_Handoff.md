# SHHH! — Immersive VR UI Handoff

## Figma
File: https://www.figma.com/design/702WVhvaDyjbK0wVLJj7Fi
New immersive perspective artboards are on the page `VR IMMERSIVE • SHHH! Spatial UI v2` (page node 8:2), preserving the original 2D wireframes.

## Canva
Editable visual art-direction board: https://www.canva.com/d/CW74ptTQLyb_xos
Original transparent character-free SHHH! logo has been uploaded to Canva assets.

## Included Figma logo import
`SHHH_Approved_Logo_Figma_Import.svg` embeds the approved character-free logo as a transparent image. Drag it into Figma, or use the PNG directly. It retains the exact image artwork (but the internal image pixels are not vector-editable).

## Immersive VR mockup scenes (Figma)
01 Main Menu — spatial wall-mounted menu in living room
02 Lobby — 2–4 full-body sibling positions and physical nameplates
03 Sibling Customization — life-size avatar with spatial outfit selection
04 Mom Opening — in-world doorway cinematic and dialogue
05 Round 1 Countdown — spatial countdown overlay
06 Living Room HUD — limited sleep indicator, timer, in-world choreboard
07 Pet Alert — warning anchored near the cat and vase
08 Sabotage Trap — cabinet clue and reversible prank
09 Kitchen HUD — world-locked chores and object cues
10 Noise Burst — rings originating from colliding objects
11 Mom Investigation — doorway reaction and cinematic overlay
12 Elimination — culprit name and attributable noise reason
13 Round 2 Countdown — reduced family roster and new kitchen challenge
14 Final Round — laundry taskboard and minimal persistent HUD
15 Spectator — bedside viewing screen and voice-only spectating
16 Victory — in-home trophy and survivor reveal
17 Match Results — wall-mounted scoreboard with ranked siblings
18 Rematch — physical menu inside living room
19 Tutorial — hand-first grab and gentle-placement guidance
20 Settings — spatial comfort panel

## Unity integration guidance
These are non-interactive Figma concept mockups showing intended 3D scene composition, not rendered stereoscopic headset previews. In Unity, implement with World-Space Canvas (or 3D mesh labels), TextMeshPro, Meta XR Interaction SDK, and room-anchored UI. Align menu boards ~1.5–2 m from the seated user at comfortable eye level; validate in Quest. Keep task cues anchored to relevant objects. Avoid a permanent full-width 2D HUD. Provide scalable fonts, optional subtitles, snap-turn, and seated mode. Use server authority for shared noise meter and elimination conditions.

## Color palette
- Night Navy #19183F
- Sunny Yellow #FFD65A
- Coral #FF7769
- Teal #65C8C5
- Purple #7958BB
- Cream #FFF3DD

## Current status
20 Figma VR concept frames were created. The Figma Starter MCP call limit prevented final connector-based logo insertion, screen capture verification, and final polish. The approved logo itself is in Canva assets and included with this kit for manual drop-in. All game interface copy is in English.
