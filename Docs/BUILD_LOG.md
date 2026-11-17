# BUILD LOG

| UTC | APK | Size | SHA-256 | Notes |
|---|---|---|---|---|
| 2026-11-05 15:42 | SHHH_dev_0.1.2_c3.apk | 63.6 MB | 90f0b25b…5525 | V4 baseline + pyjama kids |
| 2026-11-07 15:50 | **SHHH_dev_0.1.3_c4.apk** | 109.9 MB | 446c8c0050fdb249debf48c7c81a233cc21274ae9ab3a4253b915036bf3a453c | V5 personal duties/sabotage/rush, duty visuals, Mom English VO, Astra V2 props/furniture, Photon Fusion 2.1.4 + Voice 2.64 compiled in. Unity 6000.0.62f1, IL2CPP ARM64, minSdk 32 / targetSdk 34. Development build. First attempt (15:44) was cancelled mid-IL2CPP by a config reimport; the retry succeeded in 201 s. |

| 2026-11-07 16:25 | **SHHH_dev_0.1.4_c5.apk** | 115.8 MB | babed309773d756a09c02cd700f75e49c38b947af30ee901ded552e5bf2f0b31 | c4 + comic VFX (POW / hit / WOW / stars / toon impact) wired to impacts, finished jobs and foiled sabotage. **Installed on the Quest 2** at 19:25 local. |
| 2026-11-07 17:00 | SHHH_dev_0.1.5_c6.apk | 121.5 MB | a42b979ebbdccb5c476cf43b4c2487471d57194d9fde7264d2ca99fe87d3b367 | Look pass: SHHH/Toon cel shader, pastel palette, warm/cool lights, cartoon post, glass UI, Fredoka, new logo/icon, per-action VFX, job reveal, emotional VO |
| 2026-11-07 17:12 | **SHHH_dev_0.1.6_c7.apk** | 121.5 MB | cef396218f06ea4e52f8ba4f6eace63ae80ea9eb4da672e58d28f27ccba04b9e | c6 + one-shot on-device QA autopilot flag. **Installed on the Quest 2**; frame rate **not yet measured** (the unattended run didn't take focus without someone wearing the headset) |
| 2026-11-07 17:40 | **SHHH_dev_0.1.7_c8.apk** | 121.5 MB | 7ef77c94ade1c414f4256ca032dd390dbebe6dc0918815b83556265a6a11a126 | Fixes from the Quest recording review (see `QA_VIDEO_REVIEW_20261107.md`). **Installed on the Quest 2** (versionCode 8). |

Manifest of c4: INTERNET, ACCESS_NETWORK_STATE, RECORD_AUDIO, MODIFY_AUDIO_SETTINGS, BLUETOOTH (the last three come from Photon Voice, which is not used in gameplay yet; see ISSUE-05), com.oculus.permission.HAND, supportedDevices quest2|quest3|quest3s.
Path on the PC: `SHHH\Builds\Quest2_3_3S\SHHH_dev_0.1.3_c4.apk` (sha256 also in `BUILDS.csv`).
