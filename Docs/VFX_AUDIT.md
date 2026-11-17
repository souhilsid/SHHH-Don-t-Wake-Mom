# VFX pack audit (V5 §7) — updated 2026-11-07 19:20 (UTC+3)

Method: each owned pack was downloaded through Package Manager › My Assets, copied to `SourceAssets/AssetStore/` (git-ignored) and inspected **offline before import**. The inspection covered the file list, vendor root folder, texture count/size, demo scenes, max particle counts, any shader use of GrabPass / opaque / depth textures, and gore assets (raw data in `QA_Evidence/vfx_pkg_audit_raw.json`). Only the packs marked USE/MODIFY were imported, each into its own vendor folder. The chosen prefabs are copied to `Assets/SHHH/Resources/VFX/vfx_<role>.prefab`, with Quest-safe cloned materials in `Assets/SHHH/VFX/Materials`. A play-mode render verifies them: `QA_Evidence/vfx_gallery_t0.25.png`.

| # | Pack | Decision | Measured facts | Role in SHHH! |
|---|---|---|---|---|
| 1 | Trails VFX URP (Vefects) | **REFERENCE ONLY** (in project, unused) | 163 files, 44 textures (40.7 MB), URP shaders | Built-in TrailRenderer is cheaper for thrown-object micro-trails |
| 2 | Cartoon FX Remaster FREE (JMO) | **USE (modified)** | 522 files, 160 textures (9.9 MB), max 2000 particles, CFXR ubershader. Effects were **invisible in our URP setup until soft particles were disabled**: SHHH turns the camera depth texture off for Quest, so the materials are cloned with soft-particle fades off. Contains blood/skull/sword prefabs, which are **never referenced**. | POW (big crash), Hit D Yellow (medium impact), WHAM, WOW (job done), Falling Stars (sabotage foiled), water splash |
| 3 | VFX URP Fire Package (Wallcoeur) | **REFERENCE ONLY** | 28 files, 2.3 MB, max 20 particles, no screen-space shaders | Possible cartoon stove flame later; not imported |
| 4 | Hologram VFX URP (Sekochi) | **SKIP** (not imported) | 77 files, 22 MB, depends on VFX Graph (not in the project, compute-heavy on Quest) | Clashes with the cozy home |
| 5 | Bubbles VFX (Vefects) | **MODIFY** | 160 files, 52 textures (42.9 MB), max 30,000 particles in a demo, one GrabPass shader. Bubble shaders are built-in-RP only (pink in URP), so they are swapped to URP Particles/Unlit. Only the single-bubble prefab is used. | Dish-washing and washer bubbles |
| 6 | Candle VFX URP (Vefects) | **SKIP** | 202 files, 42.7 MB of textures for a candle | Too heavy for one ambient light; an in-house flicker is cheaper |
| 7 | VFX Impact and Hit (Light) (Wallcoeur) | **USE** | 196 files, 21.4 MB, mobile particle shaders (render fine in URP). Contains `VFX_Blood_01/02`, which are **never referenced**. | `toon_impact` for small and medium knocks |
| 8 | Distortion VFX URP (Vefects) | **SKIP** | Uses `_CameraOpaqueTexture`/scene colour + depth (screen-space refraction); a full-screen copy is too costly on Quest 2 | — |
| 9 | Wind VFX URP (Vefects) | **REFERENCE ONLY** (in project, unused) | 183 files, 62 textures (48.8 MB), demo max 10,000 particles | Too heavy for ambience; a small in-house curtain sway is cheaper |
| 10 | Anime Star VFX URP | **PENDING** (still not in the download cache after two attempts) | — | CFX Falling Stars covers comic stars for now |
| 11 | Invisible VFX URP (Vefects) | **SKIP** | 92 files, 48 MB of textures; no invisibility mechanic in the design | — |
| 12 | Bold VFX Pack DEMO (UIUX Forge) | **REFERENCE ONLY** (not imported) | 49 files, 0.1 MB, max 1000 particles, no special shaders | Cheap enough; consider for UI ready/confirm sparkles during the brand pass |
| + | Stylized – Simple Hands (sametsylmz3d) | **IMPORTED, not yet wired** | 23 files, rigged hands, 2K textures | Candidate replacement for the default dark Meta hand mesh (V5 §4.1) |

Not used: the "CFXR Flash" prefab. It renders a huge white starburst that would be uncomfortable in VR.

Housekeeping: two Package Manager clicks also imported **Distortion VFX URP**, **Invisible VFX URP** and Vefects' **Realistic Water VFX** (both SKIP), plus Wind and Trails. I moved the three SKIP folders to the Windows Recycle Bin with `trash_asset`, so they can be recovered. Wind and Trails stay in the project but are not referenced, so they don't go into the APK.
