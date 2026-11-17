# START HERE — SHHH! CLAUDE CODE HANDOFF

1. Copy the file `SHHH_CLAUDE_MASTER_PRODUCTION_PROMPT_FINAL.md` into the root of the **existing** SHHH! repository (recommended: `Docs/Production/`).
2. Copy/extract the `References/` materials from the companion ZIP into a local project references directory. They are original user-supplied concept/reference imagery; the actual Unity repo, existing installed skills, Photon configuration, LiveKit service and Quest hardware are NOT contained in this package.
3. Open Claude Code **at the root of the existing Unity repository**. Ensure permissions/access for local Unity Editor, Blender and any required MCP tools, but do not paste permanent API secrets into chat.
4. Paste this kickoff request:

> Read `Docs/Production/SHHH_CLAUDE_MASTER_PRODUCTION_PROMPT_FINAL.md` in full, then read the supplied Codex handoff and all reference materials. The owner has finalized all 16 decisions; do NOT ask me to choose them again. Audit current Unity, Blender, Photon, LiveKit, XR hands, installed skills and V5 production state, preserving working functionality and assets. Use the master prompt as the authoritative engineering and art specification, implement the game progressively with tests, produce actual files and builds, and maintain verifiable acceptance evidence. Invoke each installed skill through its **actual available command**, only when relevant. Don't stop at a plan or placeholder demonstration and don't claim device validation without device evidence. Begin with baseline backup/status/skills audit and the unfinished hand grip/physical book interaction, then continue through the full production gates.

5. If Claude Code has context limits, require it to keep `Docs/PRODUCTION_STATE.md` and test evidence updated, and restart from those authoritative files, not from compressed conversation recollection.

### Honest scope notes
- This bundle is a **production instruction and visual reference packet**, NOT a completed game or a working Unity project.
- The approximately 12,000-word master brief defines 72 task recipes, but their engine implementations do not yet exist merely because they are described here.
- The existing V5 Codex project may contain substantive finished models and a working book vertical slice; Claude must inspect and reuse them.
- Local desktop simulations differ from Quest headset validation, real human multiplayer and LiveKit integration tests.
