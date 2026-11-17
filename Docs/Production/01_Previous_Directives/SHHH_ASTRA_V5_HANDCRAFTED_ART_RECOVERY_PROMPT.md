# SHHH! — ASTRA V5 HANDCRAFTED STYLIZED 3D ART RECOVERY
## Mandatory visual-direction override and continuation of the existing Unity 6 / Quest project

**READ THIS ENTIRE DIRECTIVE ALONGSIDE THE PREVIOUS `SHHH_Astra_Full_3D_World_Production_Prompt_v1.md`.** V5 overrides the earlier document specifically on visual standards, the art pipeline, room finishing, visual review and acceptance. Existing game rules, gameplay ownership, project structure and system contracts remain in force. This is **not** a restart, new game, license to replace existing systems, or request for more concept pictures.

**MISSION:** Transform the current working but visually unfinished SHHH! house into a beautifully authored, commercially credible, premium **handcrafted stylized 3D family home** with real, editable, performant game assets. It should look like the user's approved SHHH! concept artwork **from eye-height inside standalone Quest 2/3**, not like the primitive greybox screenshots. Retain all current working gameplay, hand interactions, tasks, bots, sabotage, round logic, networking and GUIDs.

**TARGET PROJECT:** The Astra audit reported Unity `6000.0.62f1`, URP `17.0.4`, Meta XR `201`, OpenXR `1.16.1`, project chore/sabotage/hand/multiplayer systems, and a Simple Hands prefab with a 16-bone skin. Verify these facts in the actual project and keep exact working versions unless a proven integration issue requires a documented change. No screenshot of Unity Editor is a headset performance verification.

---

## 0 — IMMEDIATE CORRECTION: THE PREVIOUS OUTPUT IS ONLY A BLOCKOUT

Your existing 3 room screenshots (`ChatGPT Image Nov 8, 2026, 01_13_43 PM.png`, `...01_13_49 PM.png`, `...01_13_53 PM.png`) show large untextured planes, square couches, featureless surfaces, primitive dining blocks, visible architectural voids, sparsely furnished spaces, minimal prop variety, almost absent material character, and a bed area opening onto a balcony/landing. That is NOT the visual target and cannot be called finished art. A compile pass, a lightmap bake, FBX import or a successful scene load only proves technical progress.

**DO NOT remove a working scene to replace it with an empty new demo.** First retain/backup the current working project, architecture transforms, task IDs, gameplay references, and hand rig integrations. Preserve this blockout as a geometry/navigation reference. Replace or refine its visible art using game-ready meshes and a high-quality, consistent material and lighting language.

### Failure conditions
- Walls/ceiling/furniture made of raw cubes with flat paint presented as production final.
- Four table legs plus a rectangle called a finished dining table; blocky cube couches without sculpted cushions, seams or soft silhouette.
- Giant empty floor/wall areas with no environmental storytelling; floating lights, uncovered dark voids or Mom's bed beside an open landing instead of a private enclosed bedroom.
- A room looking like daytime when the game is happening at night.
- Standard Unity flat-lit graybox colors with no texture mapping, roughness variation, intentional bevels or decorative detailing.
- Pure concept renders that were not produced from the actual source .blend / imported Unity scene.
- Deleting/rewriting working chore, bot, hand, networking or state systems to simplify the art task.
- Claiming production readiness because code compiles or the Game View appears without errors.

---

## 1 — HIERARCHY OF VISUAL REFERENCES: WHAT TO TAKE AND WHAT NOT TO TAKE

**A. PRIMARY SHHH! ART TARGETS — control appearance and house identity.** They are the approved reference images in the previous package:
- `SHHH! Family Stage VR Lobby(1).png` — stationary theatrical Family Stage, purple/gold marquee, curtain/character display and physical VR selection.
- `VR Character Customization Lobby(1).png` — stage-based editable character presentation and wardrobe.
- `Living Room VR Stealth Adventure-1.png`, `Shhh! Create a Distraction-2.png`, `Don’t Wake Mom_ Cozy Chore Quest-3.png`, `Shhh! Don’t Wake Mom!-4.png` — contemporary cozy living room at night; plush sofa, well-modeled coffee table, TV cabinet, bookcases, stairs, toys, moonlit window, warm lights, interactive items.
- `Moonlit Family Kitchen Adventure-1.png`, `Cozy Cartoon Kitchen at Night-2.png`, `Cozy Midnight Kitchen Chore Quest-3.png`, `Moonlit Family Kitchen Adventure(1).png` — coherent purple kitchen; sink below moonlit window, cream fridge, believable stove, checkered tablecloth, plates and dishes, jars, counter accessories.
- `Cozy Colorful Laundry Room-1.png`, `Cozy Moonlit Laundry Room-2.png`, `Cozy Sunlit Laundry Room-3.png` — washers, dryers, baskets, towels, shelves, laundry sink; reinterpret ALL daytime images to a nighttime palette.
- `Shhh! Sibling Character Turnaround Sheet(1).png`, `Mom Character Turnaround Sheet(1).png` — character identity, expression, proportions, colors. Pip/Nell/Rory/Tess have detached head + torso + hands + shoes with NO arms or legs; Mom is FULL BODY.
- `SHHH_Complete_Brand_Kit_v1(1).zip` — canonical illustrated logo, typography and palette; Luckiest Guy-influenced logo (do not replace with generic font), Fredoka-compatible legible UI.

**B. NEW 14 EXTERNAL STYLE REFERENCES — controls CRAFT, not world lore.** See `REFERENCE_MATRIX.csv`; all are user-supplied research images. Do not copy identifiable third-party assets, rigs, character designs, or painted textures into the game. Reinterpret their general modeling/texturing/lighting principles for an original family-home environment.

| Ref | Filename | Legitimate lesson | Do not carry over |
|---|---|---|---|
| R01 | widen_1220x0.jpg | Carefully layered wood meshes, beveled rims, painted edge wear, warm material breakup, contact shading | Viking tavern, boar, horns or weapons |
| R02 | 106816-stylized-countryside-house-interior-5-0-unreal-engine.webp | Dense believable interior set-dressing, depth layering, lived-in domestic clutter, warm/cool light pools | Rustic historical setting, exact room plan, cinematic rendering costs |
| R03 | images (14).jpg | Stylized mesh paired with baked/hand-painted texture atlas; broad color variation and visible painted detail | Medieval windmill/cottage identity |
| R04 | widen_2440x0.jpg | Charming curved silhouettes, chunky framing, bevels and palette-controlled texture painting | Fantasy rooflines, outdoors scene elements |
| R05 | maxresdefault.jpg | Layered wood/plaster/tiles and decorative shapes with cohesive asset language | Timber-framed fairy-tale exterior architecture |
| R06 | images (13).jpg | Intentional silhouettes and deliberate color grouping | Flat polygon-only faceting and bright magical environment |
| R07 | image_processing20260211-2-byrnb9.png | Readable nighttime warm/cool contrast, foreground-to-background depth and focal hierarchy | Fantasy mushroom world |
| R08 | images (12).jpg | Cool-vs-warm accent lighting and stylized nighttime composition | Haunted castle or horror identity, excessive emissive effects |
| R09 | dxohJe.jpg | Rounded playful stylized architecture and cohesive model/texture palette | Tall crooked fantasy building |
| R10 | images (11).jpg | Compact prop clusters, storytelling density and warm focal illumination | Cauldrons, spells or forest campsite |
| R11 | JacobClaussen_KingArthur_Final_Shot02.webp | Rich sculptural focal forms, visual hierarchy, selective depth and polished illumination | Expensive cinematic foliage/volumetrics, fantasy library |
| R12 | image-asset.webp | Subtle painted material layering, compositional storytelling and warm hanging-light placement | Tree-trunk fantasy interior and dense translucent foliage |
| R13 | images (16).jpg | Direct room reference: credible kitchen furniture/props, readable color families and domestic lived-in detail | Photographic/cinematic requirements, rustic layout if conflicting |
| R14 | images (15).jpg | Handcrafted wooden prop silhouettes, bevels, varied roughness and accessories | Medieval banquet/tavern props |

**REFERENCE PRIORITY:** verified current Unity project and approved game rules > SHHH! original concept/brand references > new craft references > generic artistic conventions. The references were created by different artists. Extract a coherent design language; NEVER paste all their visual motifs together. The direct work shall read **modern contemporary suburban family-home at night**, premium friendly cartoon, whimsical *physical comedy*, not medieval/fantasy/horror.

## 2 — NON-NEGOTIABLE STYLE BIBLE

**STYLE NAME:** `SHHH! Soft-Sculpted Storybook Toon (Quest Edition)`.

**Silhouette:** broad, comfortable furniture masses; softly rounded edges and purposeful chamfers; slightly oversize recognizable features: thick sofa arms, pillowy cushions, child-friendly cabinet handles, chunky book spines, bold table legs, readable toy shapes. Subtle 1–5% asymmetry and gentle wonk where appropriate; don't randomly distort floor levels, collision volumes or VR ergonomics.

**Proportion:** believable 1:1 human VR space, playful proportion primarily within furniture and props. Door clearances, reachable shelves, colliders and hand placement remain realistic. Keep readable important objects at headset resolution and comfortable reach distances.

**Sculpt-to-game technique:** create original mid/high-detail *sculpted* hero forms in Blender, retopologize or model a performant low-poly version, bake normals/curvature/AO where suitable, hand-paint stylized base-color and roughness breakup, bevel silhouette-critical edges and use edited custom/weighted normals for smooth soft shapes. Hero sculpt is SOURCE, not the runtime mesh. Do not hide weak modeling with a procedural noise shader or a single texture slapped on a cube.

**Material language:** warm wooden edges with softened contour and limited painted grain; plush velvety sofa with seams, stitches, piping and gradient fabric response; ceramic dishware with varied roughness and colored edges; plastic toys with subtle glossy highlights; tiled kitchen backsplash with gentle variation and clean grout; baskets with simple believable woven geometry/atlas; cotton towels and patterned cloth with actual folded silhouettes and simple fabric texture. Apply contact shadowing and paint carefully, not random high-frequency procedural noise.

**Palette:** SHHH! house is primarily soft lavender/purple, warm cream, honey wood, pink/coral, gold/amber practical light, and blue moonlight. Use approved brand kit for UI/accent coloring. Don't turn every wall and object into pure saturated purple. Warm neutral materials give contrast. Preserve bright task-prop color readability and character silhouettes.

**Lighting:** nighttime exterior, moon visible through windows; cool blue/cyan low-intensity environmental fill combined with warm yellow/orange practical lamps, baked bounce and contact; create strong focal accents. Use baked lightmaps for static architecture and light probes for mobile props/characters. Avoid faking a sunset/daylight room; no heavy realtime GI, volumetric fog, expensive full-screen glow, many shadowed point lights or cinematic depth-of-field in gameplay. Modest emissive UI and lamps should not cost more than the asset itself.

**Surface detail hierarchy:** strong large forms at distance, recognizable second-order modeling at 1–3 m, painted/integrated third-order details at close inspection; avoid high-frequency micro detail invisible in a headset. Favor a hand-authored graphic style instead of photorealism.

**Visual storytelling:** the family has four mischievous children; show their lived-in environment via picture frames, kid drawings, toys, homework books, stray shoes, pet bowls, scattered cushions, magazines and dishes. Natural placement should support tasks; don't turn every tabletop into an ungrabbable clutter pile. Lock noninteractive background props in low-cost static groups. Make grabbable items visually obvious WITHOUT giant always-on neon outlines.

**NO:** raw cube-only furniture, harsh black toon outlines on every mesh, randomly assembled primitives, high-frequency photogrammetry, cheap low-poly triangles as a substitute for authored detail, arbitrary shiny materials, medieval/fantasy props, excessive bloom, fake HDR cinema that cannot run on Quest 2, generic flat mobile HUDs, or objects with no purpose.

## 3 — CURRENT UNITY HOUSE: RESTORE ARCHITECTURE QUALITY AND CONNECTIVITY

Before replacing a single wall, export a canonical top-down plan from the current project with door openings, existing world transform anchors, active chore source/target positions, Mom's starting bed, stair waypoints, bot traversal, spawn zones and room dimensions. Preserve ID/coordinate semantics or provide an explicit migration with automated regression tests.

**Living room:** complete enclosing walls, ceiling and molding, believable staircase and railings, correctly oriented TV/sofa, coffee table with bevel and differentiated wood, armchair, substantial shelves with books and decorations, washable/cleanable cups, shoes/rack, cushions, toy box, pillows, framed family drawings, window/curtains, warm table/floor lamps, soft area rug, pet area. Keep a comfortable VR play zone near interaction objects. Make the first screenshot composition feel inhabited even in the absence of UI.

**Kitchen:** one consistent architecture across all views. Coherent L-shaped counters or validated alternative, cabinets with working door fronts, handles and rounded edges, stove/range hood, fridge, dishwasher or dishes/storage, rack/plates, sink and faucet, real table with FOUR seat positions, gingham tablecloth optimized for mobile, counter appliances, visible utensil hangers, fruit/vegetable/plate props, under-cabinet baked warm light, moonlit window. Stove/cooking elements should only be interactive if present in real task definitions; avoid adding unsupported scoring loops.

**Laundry:** dedicated accessible room with washer/dryer front loaders, laundry baskets, folded and unfoldable clothing, towel piles, sorting bins, low shelves, cabinets, cleaning-product bottles, hooks/hangers and a folding surface. Ensure clothes can be grabbed, sorted and set in actual physical sockets. Aesthetic warm interior night light and blue moon fill. Keep all laundry variants within one canonical layout rather than contradicting camera views.

**Mom's bedroom:** a genuinely enclosed bedroom with walls and ceiling, a door reached from the upstairs landing, bed fully contained in the bedroom, headboard, thick duvet, pillows, soft bedside lamp, side table, robe/clothing cues, small family photo, wardrobe/rug and privacy from downstairs. Mom is NOT exposed on a railing or permanently staged above the living room. Provide bed stand-up anchors, door-handle contact, walkable route to stairs and investigation areas.

**Family Stage & customization:** preserve existing stationary stage, curtain/box reveal and actual Solo/Friends/Customize logic. Premium 3D stage geometry and character pedestals, rather than a conventional mobile UI pasted into world space. No fake currencies/levels invented from concept images. Hand-ray + pinch and real tactile/haptic presentation where the existing input system permits.

All rooms must be clearly one house with shared doors/windows/ceiling heights, consistent trim profiles, material families, prop scales and connection to the existing VR travel/locomotion mechanic. Doors and stairways cannot be decorative scenery if bots/Mom need to traverse them.

## 4 — MANDATORY MODULAR ASSET LIBRARY (NOT ONE MONOLITHIC SCENE)

Author original source-ready assets, logically tagged and reusable. Keep hero objects separate from decor and be mindful of object-level optimization. Do not replace current licensed assets if they can be legally reused and polished. For every visual prop, classify:

- `A: Hero Interactable` — player/bot grabs/manipulates/places it; silhouette, collision, grip anchor, task evidence state and audio/FX hooks are primary; examples: book, mug, pan, plate, bowl, toy, folded towel, laundry basket, shoe, cushion, cabinet door.
- `B: Environment Hero` — large/focal static or hinged furnishing: sofa, armchair, coffee table, bookshelf, TV stand, kitchen counters, stove, fridge, washer/dryer, Mom's bed, stairs, doors and curtains. Must be artistic, not cubes.
- `C: Dressing` — repeated, baked/static set decorations: photo frames, some books, plants, boxes, lamps, jars, posters and stable wall accents. Use shared mesh/material families and sensible atlases.

Minimum authored categories must encompass: architecture/molding/arch/stairs/doors/window sets; sofa/chairs/TV tables/bookshelf/shoe rack/toy chest; kitchen cabinetry/fridge/sink/stove/utensils/plate/cup/pans/fruit/tablecloth; laundry washer/dryer/baskets/towels/clothing/hangers/bottles; bedroom headboard/bed/duvet/pillows/nightstand/lamp/wardrobe; pet bed/bowls/toys; stage/curtains/pedestals; UI boards and inspected object focus.

FOR EACH ASSET publish a manifest record with `asset_id`, `role(A/B/C)`, source blend, lowpoly FBX or compatible Unity mesh, prefab path, material family, atlas page/texture file, bounds dimensions, triangle count, vertex count, material slots, collider type, static vs movable, LOD or culling policy, UV/lightmap status, left/right grip anchor and placement socket when relevant, task ID mapping, evidence metadata, status, render preview and headset-validated yes/no. Keep each mesh original, editable and named. Do not copy licensed art examples into deliverables.

Quality control depends on close-up mesh/material evidence. An asset that is simply a rectangular solid with a flat color and renamed 'Stylized Sofa' FAILS.

## 5 — CHORE/SABOTAGE INTEGRATION: ACTUAL CURRENT GAME DEFINITIONS TAKE PRECEDENCE

Astra already detected a mismatch between a prior 9-group/18-action art manifest and the current game task definitions. DO NOT silently overwrite the newer game tasks with the older manifest, nor blindly implement every older animation as a live chore. Make a side-by-side mapping: `current_task_id`, `current_gameplay_contract`, `older_manifest_entry`, `shared_prop_archetype`, `needed_new_art`, `needed_interaction_anchor`, `supported_sabotage/repair`, `test_scene`.

Start with the confirmed book-to-shelf action used in both versions; visually finish it end to end. Then expand to all live task actions and flagged art-manifest coverage. Every action must use actual grabbable props, a physical source, a target socket, collision-free hand reach, feedback on valid placement, owner accounting from existing game state, reversible sabotage with a recoverable state, and a coherent visual proof sequence. Never fake completion by moving a prop in a cutscene or calling an arbitrary score increment.

Retain the responsibility/evidence rules: task ownership is personal, other siblings may help or sabotage, damage can be repaired, the true event log differs from what Mom actually knows. Astra's work is the visible physical contract and animation, NOT a license to rebuild the higher-level AI/multiplayer/LiveKit game logic.

## 6 — HANDS, CHARACTERS AND PAW CONTACT

**Use existing installed `Stylized - Simple Hands` prefabs:** first verify actual prefab path, left/right variants, 16-bone skin mapping, bind poses, scale, current tracked hand joint adapter and active Boot rig. Keep actual Meta/OpenXR hand tracking, ray/poke/grab and tracking-loss state authoritative. The previous audit indicated visuals were initially default Meta hands despite Simple Hands being installed; ensure the new glove mesh is the actual runtime hand visual, not just a test preview. Inspect real bone deformations and mirror orientation; validate open/pinch/grab/point and held-object contact. No new commercial hand asset download, raw asset redistribution, or unverified 'fully fixed' statement.

**Siblings:** Pip, Nell, Rory, Tess must retain their approved appearance, proportions, clothes and expressions. FLOATING/DISCONNECTED head, torso, hands and shoes: NO visible arms/legs. Reuse their rig contracts, animations and game logic. **Mom is full body** with correct bedroom/sleep/wake/door/inspection routes. **Mochi/Biscuit** maintain believable simple gait and small interactions. Actual interaction with animated props uses anchors, not hands penetrating meshes.

## 7 — QUEST 2 BASELINE ART OPTIMIZATION, WITHOUT EXCUSING BAD ART

**Performance target:** interactive Quest app must reach minimum 72 FPS (13.89 ms per frame), tested on actual standalone device with relevant live gameplay. Quest 3/3S may aim higher after Quest 2 passes; do not claim FPS from Unity desktop, render stills, or Link-only sessions. Profile CPU, GPU, main/render threads, triangles, draw calls, memory, thermals and physics during worst-case action with 4 sibling actors, Mom, pets and moving props. Keep budgets project-specific; Meta's documentation lists guidance ranges, not universal safe caps. Verify device config/refresh rate and whether Quest 2 is available. If not, clearly mark UNVERIFIED with reproducible build/profile instructions.

Use: baked static lighting and light probes for moving avatars and props; mobile-compatible URP shaders (Lit/Unlit or a small documented original optimized stylized shader); shared material families, reusable trim/texture atlases, ASTC texture compression, mesh/texture LODs, static batching/SRP batching/instancing as appropriate, occlusion culling where reliable, collision-only simple meshes, efficient shadows. Maximize perceived quality with deliberate modeling/painting rather than expensive per-pixel effects. Avoid SSAO, motion blur, high-cost bloom, real-time GI, volumetric fog, large transparent curtains over the entire view, dense physics objects always awake or many shadowed point lights.

Distinguish sculpted authoring mesh from game low-poly mesh. Prioritize visible geometry and texture quality. Deliberate hero-prop detail is worth polygons where tactile interaction justifies it; hidden backs/surfaces and unreachable distant items can be cheaper.

Relevant official documentation (verify current versions inside your environment):
- https://developers.meta.com/vr/documentation/unity/unity-perf/
- https://developers.meta.com/vr/documentation/unity/po-perf-opt-mobile/
- https://developers.meta.com/vr/documentation/unity/unity-mobile-performance-intro/
- https://developers.meta.com/vr/documentation/unity/unity-best-practices-intro/

## 8 — DELIVERABLE ORDER: STOP SCALING THE GREYBOX; FINISH ONE ROOM TO GOLD STANDARD

Produce and review the **same actual connected project** in the following gates. Proceed through gates if you can self-verify objectively; don't assert acceptance you cannot substantiate. The owner may review at any gate.

### GATE 0 — Baseline audit + visual gap document
- Back up/branch and inventory existing Unity scripts/art, version numbers, asset references and task IDs.
- Save three *existing* screenshots as BASELINE, with camera coordinates and actual scene path.
- Map old vs current task IDs and present a canonical room floor plan.
- Identify each unpolished primitive in priority order and list the new/reused asset destined to replace it.

### GATE 1 — One complete hero asset pack
- Create **original** handcrafted sofa, coffee table, readable bookcase and at least five hero interaction props: book, mug, plate, toy, and cushion or task-equivalent objects.
- For each, provide Blender source model, runtime model, UVs, painted textures/atlas, correct pivots, basic colliders, Unity prefab and a close-up turntable/side-by-side proof.
- Show bevels, silhouette, material breakup, texture detail and shadows. Do not proceed if these still read as generic primitives.

### GATE 2 — Living room visual gold slice + real functional book task
- Replace the blockout dressing and materials for the actual living room (not a detached demo). Finish walls, ceiling, moldings, arches, window, staircase, sofa/cushions, TV cabinet, books, toy box, light fixtures, kid decorations, rug and properly placed clutter.
- Make ONE book-to-shelf chore function end to end (pickup, carry, valid place, state transition, sabotage by other actor, player repair and state recovery) using current authoritative task IDs.
- Capture before/after from IDENTICAL existing camera transforms: wide entry view, shelf close-up, couch + coffee table, stairs arch, top-down physical layout; include VR eye-level head-height view. A clean Unity compile is not visual proof.
- Match SHHH! original concept language strongly, with original craft-reference polish and realistic Quest constraints.

### GATE 3 — Connected kitchen and laundry, both fully dressed
- Reuse same style bible, trim/color/material libraries and modeling fidelity. Build genuine cabinets, appliances and chore props; no empty showcase box.
- Test real tasks, sabotage and repair archetypes for each room against current game definitions.
- Provide same-angle comparison images and player-reach/AI-route validation.

### GATE 4 — Mom's bedroom and investigation route
- Repair any uncovered/invalid loft space. Furnish enclosed Mom bedroom and complete the physical path from bed to door, upstairs hall, stairs and incident location. Retain current Mom director and logic.
- Render and test modular sleep/wake/get-up/door-handle/investigate reactions with real contact anchors. No fake balcony bed.

### GATE 5 — Family Stage, customization, actors, pets, VR hands
- Implement art to the approved references; no connected sibling limbs, no disconnected Mom, no fake standalone menu/lobby game-state branches.
- Verify existing hand visual and prefab integration on both hands with actual interactions, and preserve fallback modes.

### GATE 6 — Performance + evidence, not merely export
- Produce standalone APK (where build environment permits) for actual project, capture real on-headset FPS/CPU/GPU/frame metrics if a headset is available. If unavailable, hand over build and exact reproducible validation checklist marked NOT YET TESTED.
- Package real `.blend`, source textures, exported mesh files, Unity assets/prefabs, scripts only as required, texture atlases, video captures from *real source meshes*, per-asset manifest, before-after images, change logs, license records, task mapping and acceptance report.

## 9 — ACCEPTANCE CRITERIA AND VISUAL REVIEW

**Visual review is a distinct mandatory gate from Unity compile, lightmap bake, navigation and gameplay regression tests.** No percentage score may be invented as an objective quality metric. Use the actual attached reference and human visual review against these binary criteria:

1. Character-size/proportion and house topology make sense from headset eye height; Mom's room is enclosed with door and navigable route.
2. Main sofa and furniture have deliberately shaped curved/padded silhouettes, beveled outlines and visible seams/variation—not primitive cubes.
3. Close-up wood, fabric, painted cabinets, ceramic/plastic and tiles have distinct, cohesive stylized textures/material responses.
4. Kitchen/laundry surfaces are genuinely furnished with household props and task-relevant objects; no conspicuously empty main room.
5. All primary rooms look like the SAME house, with coherent layout, trim, night lighting, color palette and window placements.
6. No oversized artificial screen-space HUD or unrealistic image-derived mechanics added without game authority.
7. Hero grabbable objects have functional correct hand grip/placement, physics, task ownership and sabotage/repair state.
8. Both hands, four siblings, Mom and pets retain their established real rig and roles.
9. Real before/after screenshots are captured at matched camera transforms; all original scene proof is from Unity project or source Blender files, not generative mockups.
10. Quest 2 frame-rate and rendering metrics are either evidenced on actual headset or explicitly tagged **UNVERIFIED**.

**MANDATORY RESPONSE FORMAT:** After each meaningful gate report: (a) changed files and their exact paths, (b) full-size before/after screenshot paths, (c) source/mesh/atlas/prefab paths, (d) completed chore/sabotage verified behavior or precise blocker, (e) desktop Unity compile/play checks, (f) actual Quest test yes/no and measured metrics if yes, (g) unmet visual requirements and next corrective step. Avoid celebratory "complete" wording for unfinished greybox output.

## 10 — EXECUTION REQUEST / IMMEDIATE NEXT ACTION

You already audited parts of the project, integrated the installed hand visual in a versioned code path, authored a first house assembly, corrected stair/arch conflicts, and baked a lightmap. **Do not restart or throw that away.** Return to your current Unity project, preserve verified working systems, inspect the 3 unacceptable screenshots and all 14 added art-style references, and implement **GATE 1 + GATE 2 first**. Specifically create 4 beautifully finished, original game-ready hero assets (sofa, coffee table, bookshelf, book) plus additional task props, author painted materials, and replace the first living-room primitive set. Render and inspect the result from the original camera angle. Keep refining until it is clearly visually closer to the approved SHHH! concept images while remaining feasible for Quest 2. Then expand the same system to the kitchen, laundry and Mom's bedroom, preserving actual gameplay.

**Produce assets and test results, not just a new plan.** If your environment cannot run Blender/Unity or lacks an actual headset, state exactly what you produced and what remains blocked. A working build and a beautiful polished world are TWO separate requirements; both must be demonstrated. Do not confuse an elegant written promise with physical source assets.
