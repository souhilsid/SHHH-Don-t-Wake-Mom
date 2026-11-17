# SHHH! V6 — MANDATORY OVERRIDE: ONE-SHOT FULL PRODUCTION & OFFLINE VALIDATION

**These instructions take precedence over any conflicting milestone, testing, or delivery requirements in the previous SHHH! prompts.**

## 1. ONE-SHOT FULL GAME PRODUCTION — MANDATORY

I want you to execute the complete SHHH! development and reimplementation as one continuous, comprehensive production assignment.

Do not stop after:
- Creating the initial prototype.
- Fixing the first interaction.
- Completing a 60-second gameplay slice.
- Finishing the first room.
- Importing the 3D assets.
- Implementing one game system.
- Producing an intermediate APK.
- Generating mockups, documentation, or a development roadmap.

These are internal development checkpoints, not final deliverables.

You must continue implementing, integrating, debugging, validating, and polishing the entire game within your available execution sessions.

The final target includes:

- Complete solo gameplay with three competitive AI siblings.
- Functional Photon Fusion 2 multiplayer with 2–4 human players and bots filling remaining slots.
- Photon Voice multiplayer communication.
- All three gameplay rooms and their unique challenges.
- Reliable hand tracking, object grabbing, placement, throwing, ray interactions, and locomotion.
- Personal chores, competitive AI, creative physical sabotage, counterplay, and meaningful scoring.
- Mom's AI, investigations, voice acting, elimination, and cinematic reactions.
- Expressive sibling avatars, facial animation, Mom, and pets.
- Complete stylized environmental redesign.
- Cohesive cartoon URP lighting, shaders, materials, post-processing, particles, and VFX.
- Full spatial UI/UX redesign, branding, logo, fonts, and animation.
- Professional spatial sound design, Foley, SFX, and appropriate music.
- Complete onboarding, menus, customization, match flow, results, spectator mode, and rematch.
- Polished gameplay, replayability, and enjoyable moment-to-moment interactions.
- Standalone compatibility targets for Meta Quest 2, Quest 3, and Quest 3S.

You have creative freedom to improve mechanics, design systems, source assets, modify Blender models, replace weak implementations, and redesign audiovisual elements.

Use the supplied Astra deliveries, UI assets, references, videos, and existing Unity project as starting materials.

**Do not confuse a successful compilation with a complete game.**

Maintain internal checkpoints, Git commits, build logs, and recovery notes. If execution context is interrupted, leave a precise continuation record so the work can resume without losing progress.

Do not declare final completion while known critical features are missing or failing.

## 2. NO PHYSICAL VR TESTING DURING DEVELOPMENT

My Meta Quest headset is NOT connected to the development PC.

I will NOT:
- Connect the Quest through USB.
- Enable ADB debugging for this development cycle.
- Test intermediate APK builds.
- Open or operate Unity Editor.
- Repeatedly install new versions.
- Provide ongoing gameplay feedback during implementation.

Do not require a connected Quest as a prerequisite for starting or continuing development.

Do not repeatedly ask me to connect or test the headset.

You must perform every feasible development and validation task on my Windows PC, using the installed Unity tools, Claude Code, Blender, automated scripts, local simulation clients, and testing frameworks.

### Required offline testing

**A. Unity project validation**
- Verify package compatibility.
- Compile all C# assemblies.
- Resolve all build errors.
- Check scenes, prefabs, missing references, and material assignments.
- Validate animation controllers, rig mappings, and object scales.
- Run EditMode and PlayMode tests.
- Detect exceptions and runtime errors.

**B. VR interaction validation**
- Create automated simulated XR head and hand inputs.
- Test grabbing, holding, release, throwing, and placement.
- Test interaction-layer and collider configurations.
- Test near poke, hand ray selection, UI focus, and button activation.
- Test tracking-loss recovery logic through synthetic inputs.
- Validate reach distances, interface positioning, and hand-object alignment numerically.

These tests must use the actual gameplay systems wherever possible, not disconnected test-only substitutes.

**C. Multiplayer validation**
- Install and configure Photon Fusion 2 and compatible Photon Voice.
- Verify SDK initialization and application configuration.
- Create multiple independent desktop/headless testing clients where technically supported.
- Test room creation, joining, leaving, reconnecting, and player identities.
- Test synchronized objects, tasks, bot state, scoring, Mom AI, and elimination.
- Validate 2-human/2-bot, 3-human/1-bot, and 4-human configurations using independent clients.
- Test disconnects, authority, and synchronization failures.

Do not claim real online multiplayer is working based only on local mocks. Use actual Photon service connections whenever available and document any test limitations.

**D. Gameplay validation**
- Simulate complete matches from start to finish.
- Test all three rounds and their transitions.
- Verify that every task can be completed.
- Verify that bots can navigate and interact with real gameplay objects.
- Check scoring, sabotage, counterplay, noise attribution, and eliminations.
- Detect deadlocks, unreachable objectives, missing interactables, and invalid match states.
- Validate the full Solo and Multiplayer user journeys.

**E. Art and audiovisual validation**
- Render representative Unity scenes using available desktop preview tools.
- Inspect character placement, environmental scale, textures, lighting, and materials.
- Check rig deformation and animation transitions.
- Check UI contrast, legibility, and interaction targets.
- Verify that required audio clips exist and are correctly referenced.
- Inspect audio clipping, mixer routing, and sound-event mappings.
- Validate VFX dependencies, shader compatibility, and reasonable mobile rendering budgets.

**F. Android build verification**
- Complete a clean Android ARM64 IL2CPP build.
- Verify the output APK exists and is structurally valid.
- Check the package identifier, manifest, XR configuration, and target architecture.
- Analyze build errors and warnings.
- Inspect asset sizes and report relevant performance risks.
- Use available offline diagnostics without claiming to have measured Quest FPS.

### No false guarantees

You cannot prove actual headset tracking latency, perceived visual comfort, hardware FPS, microphone behavior, or real-device interaction fidelity without a Quest test.

Clearly label these items as **hardware-unverified**, even if all automated checks pass.

However, you must proactively investigate and minimize potential failures rather than postponing obvious problems until I test.

## 3. FINAL DELIVERY — ONLY AFTER COMPLETE IMPLEMENTATION

You may generate as many INTERNAL development builds as necessary.

However, do not treat intermediate APKs as final deliverables or ask me to test them.

When all implementable features are finished and all feasible offline acceptance checks pass, prepare the final delivery:

1. `SHHH_Quest2_Quest3_Quest3S_Alpha.apk`
2. Complete updated Unity project.
3. Final source assets and Blender files.
4. Final visual and audiovisual libraries.
5. Automated test reports and results.
6. Photon multiplayer validation report.
7. Known limitations and hardware-unverified risks.
8. Unity build logs.
9. Release version information and configuration.
10. Installation and Meta Alpha distribution instructions.

Use an appropriate release-signing configuration for the Alpha build. Do not expose signing passwords, private keys, or service secrets in output files.

Perform a final clean build and validate the APK after the final changes.

**I will personally install and test the completed APK on my Quest 2 once the entire offline production pass has finished.**

I will record gameplay afterward and provide feedback for any real-hardware defects that automated testing cannot detect.

## 4. EXECUTION POLICY

You are the autonomous lead engineer and creative director.

Do not stop to seek approval for ordinary artistic or technical implementation decisions.

Only request user intervention for genuine blockers such as authentication, legally required consent, paid purchases, missing credentials, or unavailable external services.

Do not substitute fabricated test results for real execution.

Do not silently remove major requirements when facing implementation difficulties.

If a feature cannot be completed, document the exact blocker and distinguish it from completed and verified functionality.

**The objective is one complete, polished SHHH! production pass followed by one user-led Quest testing session—not a sequence of incomplete VR prototypes.**

Begin and continue full production using the existing project and supplied materials.