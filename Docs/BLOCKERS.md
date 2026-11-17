# BLOCKERS — what needs the owner, credentials, or hardware (updated 2026-11-10, V8 Genies-primary continuation)

(Windows file names are case-insensitive, so this file is also the directive's `Docs/Blockers.md`.)

Each blocker lists exactly what is missing and what still goes ahead without it.

| # | Blocker | Precise cause (verified 2026-11-09) | Owner action | Meanwhile (not blocked) |
|---|---------|-------------------------------------|--------------|-------------------------|
| B1 | ~~Genies avatar validation~~ **resolved in the Editor (2026-11-10)** | The project has Genies auth settings and a stored Genies session: `TryInstantLoginAsync` logs in, the full SDK path (load, inventories, equip, save, reload) passes (`genies_diag d1`, `genies_journey j5/j6`). Remaining unknown: the headset starts without that stored session (instant login → anonymous fallback); default-inventory access when anonymous is UNVERIFIED. | Nothing required now; if the headset shows only the default avatar, sign in to Genies on the device when the SDK login UI is added. | Genies is the primary avatar system (wardrobe, persistence, starter looks, bots, remote players). |
| B2 | **Mixamo pipeline** | There is no Mixamo MCP in this Claude session (the Windows device exposes only the Blender MCP and metavr). `Tools/mixamo-mcp` is a Codex-side config; Claude cannot log in to Adobe (directive: no cookies, no embedded login). | Optional: download the needed FBX clips from mixamo.com yourself into `Art/Incoming/Mixamo/` (FBX for Unity, with skin). | Animation stays on the current procedural humanoid + Quaternius clips; the retarget contract (`Docs/Animation_Rig_Contract.md`) defines the bone names, so dropped-in Mixamo clips are retargeted by script. |
| B3 | ~~Classroom-VR reference~~ **resolved (2026-11-10)** | Read-only access to `Downloads\Classroom-VR-main` was granted. It is a Unity 2020.3 / Photon PUN project; only the concept was used (VRMap: network head/hand poses drive body IK targets, body follows the head with a smoothed yaw). Nothing imported, no App IDs reused. | — | — |
| B4 | **LiveKit voice for Mom/bots** | No LiveKit server URL or token backend is configured; client builds must not ship long-lived keys. The previously exposed LiveKit secret still needs rotating. | Rotate the key in LiveKit Cloud; when wanted, provide a token-minting backend URL. | Mom's offline voice bank with subtitles, plus card/gesture interrogation, is the full-parity path (a directive requirement anyway). |
| B5 | **Headset verification** | No Quest test happens in this session. All hand interaction evidence is from desktop simulated hands (`SimHandSource`). | Install the APK and play a match when convenient. | Every headset item is labelled **UNVERIFIED (desktop sim)** in `Docs/QA_Evidence_Index.md`. |
| B6 | **Store / Alpha upload** | No Meta app ID or platform-utility token. A store app ID is never invented. | Create the app in the Meta Horizon Developer Dashboard. | Release-signed APKs are built and hashed locally. |
| B7 | **Keystore backup** | `Secrets/` holds the only release keystore. | Copy `SHHH\Secrets\` to private storage. | — |

Resolved: Photon Fusion 2 + Voice App IDs (configured; independent-client tests pass), Unity Editor automation bridge, Blender MCP (present on the device).
