# SHHH! Don't Wake Mom — V6 Alpha release

## Build
| Field | Value |
|---|---|
| File | `Builds/Quest2_3_3S/SHHH_Quest2_Quest3_Quest3S_Alpha.apk` (byte-identical copy of `SHHH_alpha_0.6.0_c9.apk`) |
| SHA-256 | `a652c8aebd3b11577dcecf76a4030ff5bb6de0847e7d4c5dadfdc817a2495a44` |
| Size | 108 265 070 bytes (103 MB) |
| Version | versionName **0.6.0**, versionCode **9** (`Builds/version.json`, `Builds/Quest2_3_3S/BUILDS.csv`) |
| Built | 2026-11-08 09:06 UTC, Unity 6000.0.62f1, 480 s, **0 errors**, 34 warnings |
| Build log | `QA_Evidence/build_apk_release_0.6.0_c9.json` (Unity build report messages) |
| Signing | release keystore from `Secrets/signing.json` (git-ignored; passwords are cleared from PlayerSettings after the build and never written to logs). APK Signature Scheme **v2**; certificate CN=SHHH Game, OU=Release, valid to 2056-10-28 |

## Structural checks (androguard on the final APK)
| Check | Result |
|---|---|
| ZIP integrity | OK (`testzip` clean) |
| Package | `com.shhhgame.dontwakemom`, label "SHHH! Don't Wake Mom" |
| SDK levels | minSdk 32, targetSdk 34; Horizon OS SDK min 60 / target 201 |
| ABI | **arm64-v8a only** (IL2CPP: `libil2cpp.so`, `libunity.so`, `libgame.so`) |
| XR | `com.oculus.supportedDevices = quest2|quest3|quest3s`, `com.oculus.intent.category.VR` launcher, `android.hardware.vr.headtracking` required, OpenXR loader + Meta XR feature, `com.oculus.vr.focusaware=true` |
| Hands | `oculus.software.handtracking` (required=false → hands **and** controllers), `com.oculus.permission.HAND_TRACKING`, hand-tracking frequency LOW (default) |
| Debuggable | not set (release) |
| Permissions | INTERNET, ACCESS_NETWORK_STATE (Photon), RECORD_AUDIO + MODIFY_AUDIO_SETTINGS + BLUETOOTH (Photon Voice; runtime permission, prompt flow hardware-unverified), HAND_TRACKING |
| Optional features | microphone and touchscreen declared `required=false` |
| Biggest contributors | assets 62.9 MB (art, audio, VO), native libs 38.2 MB, dex 6.4 MB |

Build warnings (none blocking): compressed icon texture, OpenXR feature patch-version notes, Vefects demo post-processing
profiles (BIRP scripts, unused at runtime), Photon Voice microphone-permission helper, Active Input Handling = Both, three
unused-variable C# warnings.

## Install on a Quest (sideload, for the user test)
1. Enable Developer Mode for the headset in the Meta Horizon mobile app (Devices → Headset settings → Developer mode).
2. Connect the Quest to the PC with USB-C, put it on, accept **Allow USB debugging** (tick *Always allow*).
3. Either drag `SHHH_Quest2_Quest3_Quest3S_Alpha.apk` onto the device in **Meta Quest Developer Hub**, or run
   `adb install -r SHHH_Quest2_Quest3_Quest3S_Alpha.apk`.
4. In the headset: Library → *Unknown sources* (or Apps → filter) → **SHHH! Don't Wake Mom**.
5. Put the controllers down to play with hands (the game is hand-first; controllers also work).

What to record first (covers the hardware-unverified list): main menu by poke and by pinch-ray; Practice bench grabs (cup,
plate, book, pan, cushion, sock); one Solo match through all three rooms; Settings volume rows; one Play With Friends room
with a second device (Quest or the Windows build) to hear voice.

## Meta Alpha distribution (needs the user's Meta developer login — the only remaining blocker)
1. Meta Horizon Developer Dashboard → create the app (or reuse an existing one) → note the **App ID** (Development → API).
2. Generate a user token (Account → Account settings → *Get Oculus platform utility token*) — treat it like a password; it is
   never stored in this project.
3. Upload to the Alpha channel with the OVR Platform Utility:
   `ovr-platform-util upload-quest-build --age-group TEENS_AND_ADULTS --app-id <APP_ID> --token <TOKEN> --apk SHHH_Quest2_Quest3_Quest3S_Alpha.apk --channel alpha --notes "V6 Alpha 0.6.0"`
   (pick the age group that matches your self-certification — the game is a family cartoon with no violence; without
   `--age-group` the upload lands as a draft).
4. Dashboard → Distribution → Release channels → **Alpha** → add tester emails; testers accept the invite and install from
   the Store app. Each further upload needs a higher versionCode (the build script bumps it automatically).
