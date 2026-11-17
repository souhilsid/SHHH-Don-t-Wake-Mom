# BUILD & RELEASE

All builds are driven from the open Editor through the bridge:
- `Tools/job.sh build_apk_dev` → `Builds/Quest2_3_3S/SHHH_dev_<ver>_c<code>.apk` (Development, debug-signed). Each build bumps patch + versionCode and appends size + SHA-256 to `Builds/Quest2_3_3S/BUILDS.csv`.
- `Tools/job.sh build_apk_release` → `SHHH_alpha_...apk`, non-debuggable, signed with the release keystore described in `Secrets/signing.json` ({keystorePath, keystorePass, keyAlias, keyPass}). Passwords are cleared from PlayerSettings after the build. `Secrets/` is git-ignored — back it up privately; losing it means you can never update the app.
Manifest: minSdk 32, targetSdk 34, IL2CPP ARM64, Vulkan, `com.oculus.supportedDevices = quest2|quest3|quest3s` (OVRProjectConfig target devices), headtracking required, launcher category VR (Meta XR SDK manifest generation).

## Quest 2 dev loop
`metavr device list` (must be `device`, not `unauthorized`) → `metavr app install <apk>` → `metavr app launch com.shhhgame.dontwakemom` → `device_logcat` filtered on `Unity` / `[SHHH]`.

## Alpha (Meta Horizon Developer Dashboard → App Distribution → Alpha)
1. Create the app in the dashboard (user), note App ID. 2. `metavr auth login` (user approves). 3. `metavr store dist upload` with the release APK to channel ALPHA. 4. Add the tester account to the Alpha channel; confirm in dashboard. Not done yet — see BLOCKERS.
