# ISSUE TRACKER (updated 2026-11-08, V6 Alpha 0.6.0 c9)

| ID | Sev | Issue | Status / next action |
|---|---|---|---|
| ISSUE-01 | — | No headset during development | By design (V6 offline override). One user-led Quest test of the final APK; everything device-dependent is listed in `V6_KNOWN_LIMITATIONS.md` |
| ISSUE-02/03 | — | Multiplayer only in one Editor; gameplay offline-only | **Resolved**: real match over Photon Fusion with independent clients, 2H+2B / 3H+1B / 4H+0B, 0 desyncs (`V6_NETWORK_TWO_CLIENT_LOGS.md` runs 2–8) |
| ISSUE-04 | High | Exposed LiveKit API secret (pasted in a chat screenshot) | User: delete that key in LiveKit Cloud. LiveKit is not used by the V6 Alpha |
| ISSUE-05 | — | Voice permissions in the manifest before voice was wired | **Resolved**: Photon Voice wired (Mic toggle on wrist + Settings, runtime permission request) |
| ISSUE-06 | Low | APK 103 MB (was 110–121 MB in V5 dev builds) | Acceptable for Alpha; next: catalog only used keys |
| ISSUE-07/08 | — | Procedural rooms; Astra rigs too heavy | **Resolved**: Astra V2 `_Reduced` cast (2.7–8.7 k tris) + room dressing |
| ISSUE-09 | Low | Brand font lacks some symbols | ASCII fallbacks in use |
| ISSUE-11 | Low | One `xr_diag` case: early pinch over a cluttered table grabs the neighbour | Accepted; revisit after the headset test |
| ISSUE-12 | Med | Bot seat taken over mid-round idles until the next room | Spawn a bot body on takeover |
| ISSUE-13 | Med | Client placement rings don't reflect occupancy | Replicate anchor occupancy (rules are already correct) |
| ISSUE-14 | Med | No host migration | Clear message; consider Fusion host migration later |
| ISSUE-15 | Low | Nell can clip the bookshelf at the shelf anchor | Nav offset for that anchor |
| ISSUE-16 | Med | Quest 2 frame rate unmeasured (post-processing + 5 skinned characters) | Measure in the user test; levers listed in limitations |
