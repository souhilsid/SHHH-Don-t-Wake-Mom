# SKILL ROUTING (verified 2026-11-09)

## Codex skills vs Claude skills — what is actually available

The project contains **117 Codex skills** in `SHHH/.agents/skills/`. Codex's configuration is not shared with Claude, so availability was checked
against the skills registered in this Claude session (Skill tool listing, 2026-11-09):

- **41 of 117 are registered Claude skills** (`anthropic-skills:<name>`, plus the four project skills in `SHHH/.claude/skills`):
  animation, asset-optimization, blender-game-assets, blender-image-to-3d, blender-modeler, cartoon-style, character-artist, collision-proxy,
  cozy-wholesome-mood, environment-artist, game-designer, game-ui-design, game-ui-ux, impeccable, lod-pipeline, materials, prop-artist, qa-review,
  retopology, rigging, scenario, scenario-brand-kit, scenario-consistency, scenario-game-assets, scenario-unity-animation, scenario-unity-performance,
  scenario-unity-rendering-lighting, scenario-unity-shaders, scenario-unity-ui, scenario-unity-vfx, scenario-unity-world-building, set-dressing,
  shhh-cozy-urp-lookdev, shhh-livekit-unity-voice, shhh-photon-network-audit, shhh-quest-vfx-shaders, stylized-style, texture-workflow,
  ui-ux-pro-max, unity-export, uv-workflow.
- **76 are not registered** (e.g. physics-sim, cloth-sim, vfx-fx, lighting, lookdev, genre-stealth, vector-style, hand-painted-style,
  procedural-modeling, export-pipeline, scene-assembly, plus the horror/genre/era style packs that do not apply to SHHH!). Their SKILL.md files are
  still readable on disk, so the SHHH-relevant ones (physics-sim, cloth-sim, vfx-fx, lighting, lookdev, genre-stealth, vector-style,
  hand-painted-style) are used **as reference documents** and never reported as "skills that ran".
- Additional Claude plugin skills that Codex does not have: `meta-vr:*` (35, incl. hz-unity-meta-interaction-sdk, hz-unity-meta-core-sdk,
  hz-perfetto-debug, hz-store-submit), `unity:*` (incl. ui, audio-setup-mixers, optimize-audio, initialize-ai-navigation, physics-3d-collision,
  urp-postprocessing, craft-game-floors), `unity-perf:*`.

## MCP connectivity (checked, not assumed)

| Connection | Status 2026-11-09 | Used for |
|---|---|---|
| Windows device bridge (file tools + Linux shell over the SHHH Game folder) | **connected** | sync, Unity automation bridge (`Tools/job.sh`) |
| Blender MCP on the PC (`mcp__remote-devices__Blender__*`) | **listed**; Blender must be open | prop/room adaptation, renders |
| metavr (Meta docs, device, perf) | **connected** | Quest facts, perf traces when a headset is attached |
| Mixamo MCP | **not available to Claude** (Codex-only config) | → blocker B2 |
| Scenario MCP | not connected | → Blender/Inkscape/Krita instead |
| Unity MCP | none; the project's own file bridge replaces it | — |
| Cloud workspace tools | Inkscape 1.2.2, Krita (+kritarunner, offscreen export), FluidSynth + FluidR3_GM (MIT), sox, LilyPond, dotnet | UI art, music, audio, type-check, core tests |

## Routing per objective

| Objective | Skills (exact) | Connection | Output / validation |
|---|---|---|---|
| Continuous match, scoring, fairness | `anthropic-skills:game-designer`; reference `genre-stealth` | none | Core tests (dotnet), Unity autopilot |
| UI art kit, logo, HUD, map | `anthropic-skills:game-ui-design`, `game-ui-ux`, `scenario-unity-ui`, `unity:ui`; reference `vector-style` | Inkscape/Krita (cloud) + Unity bridge | .svg/.kra masters → PNG atlases → in-game screenshots |
| Night skybox / neighbourhood | `shhh-cozy-urp-lookdev`, `scenario-unity-rendering-lighting`, `environment-artist`; reference `lighting` | Krita + Unity | matched-angle captures |
| Pack imports, toon adaptation | `asset-optimization`, `materials`, `set-dressing`, `environment-artist`, `collision-proxy`, `lod-pipeline` | Unity bridge (+ Blender MCP when open) | Asset_Source_Licenses.csv, budget table, room captures |
| Physical interactions (water/cloth/scrub) | `meta-vr:hz-unity-meta-interaction-sdk`, `unity:physics-3d-collision`; reference `physics-sim`, `cloth-sim` | Unity bridge | recipe_diag (desktop sim hands = UNVERIFIED on headset) |
| VFX / shaders | `shhh-quest-vfx-shaders`, `scenario-unity-vfx`, `scenario-unity-shaders`; reference `vfx-fx` | Unity bridge | captures + perf budget |
| Characters, rigs, animation | `character-artist`, `rigging`, `animation`, `scenario-unity-animation` | Blender + Unity | Animation_Rig_Contract.md |
| Multiplayer | `shhh-photon-network-audit` | Photon Cloud | `net_multi` independent Win64 clients |
| Voice (optional) | `shhh-livekit-unity-voice` | LiveKit backend (**blocked**, B4) | offline voice bank fallback |
| Audio | `unity:audio-setup-mixers`, `unity:optimize-audio` | FluidSynth/sox (cloud) | Audio_Bible.md + license manifest |
| Quest perf / store | `meta-vr:hz-perfetto-debug`, `hz-unity-code-review`, `hz-store-submit` | metavr + headset (B5/B6) | — |

Rule: read the SKILL.md before first use per objective; never claim a skill ran without its output file.
