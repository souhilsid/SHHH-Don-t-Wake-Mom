# VFX & SHADERS
- URP Lit/Unlit only (+ TMP SDF). No post-processing, no realtime shadows, no HDR. 1 directional + ≤2 point lights per room.
- Noise ripples: pooled ring meshes (≤16), unlit transparent, double-sided, size/colour by noise value. Toasts: pooled TMP.
- Trap clue: small coral bead wobbling above the trapped item. Chore done: green ring pulse + chime. Grab hover: emissive tint.
- Fade sphere for teleports/room changes (rendered only during fades).
- Planned: sleep "Zzz" particles, cartoon impact stars (particle system, ≤30 particles), door light flicker shader.
