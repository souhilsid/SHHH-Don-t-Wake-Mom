# Quest 2 hands-on test — build c15 (0.6.6), 2026-11-12

Source: the owner's headset recording `com.oculus.vrshell-20261112-170302-0.mp4` (2:29, 1920x1080, 30 fps, hand tracking).
Build: c15. The Files app at 0:00–0:08 lists `SHHH_alph…6_c15.apk`, and the front-hall stage shows Genies kids (new in c15).
Installed by the owner (the PC's adb is still "unauthorized", so the grab log could not be pulled).
Method: one frame per second as contact sheets (150 frames); 4 fps on 0:17–0:28, 1:19–1:27, 1:32–1:38 and 2:08–2:16;
audio loudness per second. Status: **REAL QUEST TESTED (owner)**. Problems below; the owner's own list is still to come.

Timeline: 0:00 Files app → 0:09 launch → 0:14 Unity logo → **0:18–0:28 grey void** → 0:29 front hall, Pick a mode →
0:47 Wardrobe (hair, save) → 1:16 Pick a mode → 1:19 Solo match setup (**START never fires**) → 1:26 Practice →
1:27–2:22 practice room (book) → 2:22 Quest menu, quit.

## A. Start-up
1. **About 10 s of grey void after the Unity logo (0:17.75–0:27.75)**, with a black wedge of the last frame at the edge, and no
   sound (audio about −70 dB). The app submits no frames while the Boot / front-end setup runs in one go, so the new loading card
   ("SHHH! loading the house…") can never be drawn. The first app frame is at 0:28.

## B. Menus
2. **Solo START does not respond (1:19.5, 1:21, 1:23.5).** The finger is on START three times and nothing happens (the popup would
   close at once). Meanwhile the bot-skill buttons just above START toggle (Normal at 1:22.0, Rookie at 1:22.75), and Back fires at
   1:24.5. The owner gave up and chose Practice, so no match was played. A press seems to land about one button row higher than
   it is drawn, or START cannot be pressed at all.
3. The main "Pick a mode" board is big and close: its cards and buttons are often cut off at the left and right edges of the view
   (0:29–0:46, 1:16–1:18).
4. Wardrobe: the 9 hair tiles are empty cream squares with green ticks for about 8 s (0:47–0:54) before the pictures load; the
   loaded pictures are dark grey on dark purple and hard to tell apart (0:55–1:15).

## C. My body and hands (new in c15: full outfit + Genies hands)
5. The Genies hands show and the fingers follow tracking (pointing at 1:57, open / closed hands look right).
6. With the sleeveless outfit, the bare forearms are long and large: with both hands raised they cover a large part of the view
   (1:52–2:00, about half at 1:58–1:59).

## D. Grabbing
7. **The book cannot be let go.** It stays stuck to the open, spread hand from about 1:36 to 2:19 (1:42–1:51, 2:13.5–2:14.5: fingers
   wide open, book still on the palm). Shaking it to get rid of it swept the table: mug and plate knocked off and shattered,
   CLANG / POW / CRASH noise, and Mom woke three times ("five more minutes", "must have been the wind", "is somebody rattling?").
   Most of the practice was spent fighting this.
8. While held, the book is often tilted oddly and held very close: at 2:17–2:19 it fills the middle of the view.
9. A large yellow ring/arc appears around the held book and hand (1:41, 1:50, 2:13–2:16). It is unclear what it means (placing
   target? noise?) and it covers the view.

## E. World / feedback
10. The noise rings of the crash fill the whole view for seconds (2:08–2:16, several overlapping red rings).
11. In Practice, the mug and plate are gone from the table after the crash (shattered at 2:09–2:12). The practice steps cup / pan
    rely on them; nothing puts them back.
12. The PRACTICE instruction board hangs on the far wall (2:09, 2:19–2:21) and its text is too small to read from the table. The
    HUD says "follow the board", but the board is not where the player works.
13. The dog walks right under the player (2:02, 2:05–2:07): its head is right under the camera / hands. Pets do not keep the
    personal-space distance that bots now keep.
14. A red glove-shaped hint marker stands inside the book on the table (1:28–1:31).

## Worked
- Front-hall stage: all kids in Genies looks (0:33); wardrobe hair change + "Saved! Everyone will see this look" (1:11–1:15).
- No loading void between Pick a mode and the practice room (1:26 → 1:27). The task strip and hint line are readable. Mom's lines
  appear on the HUD while she is out of sight. The practice walls are complete (no missing walls in this run).
- No crash; the owner quit through the Quest menu.

## Not seen in this run (untested on the headset)
Solo / online match (blocked by #2), teleport / walk gestures, wrist watch, bots / Mom confrontation, job objects (tray, coasters,
dog bowl), living-room walls in the house, play-space reminder.

## Owner review (2026-11-12)
Confirmed: #1 grey void at start-up, #7 the book cannot be let go, #4 wardrobe tiles (empty for 8 s, dark grey on dark purple),
#8 + #9 holding the book (tilted, too close; unexplained yellow ring), #11 mug and plate gone after the crash, #12 practice board too
far / too small, #14 red glove hint inside the book.
Not a problem: #2 (Solo START). Not raised by the owner: #3, #6, #10, #13.

Owner's own problems:
- O1. Grab works, **release does not** - everything gets stuck in the hand (same as #7, everywhere).
- O2. The environment lighting is dark now.
- O3. Teleport: the owner does not know the gesture. Wanted: a clearly explained gesture, and good-looking teleport visuals (arc /
  ray shader, landing-area shader, ...).
- O4. **Big:** as my Genies character in gameplay, my hands' rotation and movement do not match my real hands ("a misunderstanding in
  the skeleton"); in the main menu (gloves) they matched.
- O5. **Big:** strong vibration / jitter when moving or turning the head - nothing looks stable.

## Fixes after the owner review (2026-11-12) - in source, not in an APK yet
- O4 hands: tracked-hand rotation converted with the wrong axis convention (palm 91 deg off in every pose) -> fixed (worst 3 deg fingers /
  6 deg palm over 6 poses x 2 hands, `recipe_diag avatars` my_hands_turn_like_my_real_hands). Friends' bodies use the same fix.
- O5 vibration: head motion never moves the rig and the body does not shake (`stable12` 2/2) -> most likely missed frames on the Quest.
  Added: on-device frame-rate log (PerfMonitor -> shhh_grablog.txt every 5 s, "NN fps" on the task strip when low), dynamic fixed
  foveated rendering, 2-bone skinning for other Genies bodies, no shadow casting. To confirm with the next headset log.
- O1 release: grip now = each finger's own straightness (c15 tip-to-palm grip could read an open hand as 0.3-0.6); an open hand always
  lets go; HOLD/RELEASE lines in the grab log (`grip12` 2/2, grabs 6/6). Device confirmation pending.
- #1 start-up: new tiny Startup scene shows the loading card at once and loads Boot in the background; boot timeline in the log
  (Editor: first frame +0.28 s, front end +4.2 s).
- O3 teleport: glowing flowing arc + landing spot shaders (Resources/Shaders), crossed-out spot + reason where you cannot land, HOP / WALK
  instructions above a palm-up hand, practice board explains both (`teleport12` 8/8).
- O2 dark lighting: lightmap + fog shader variants were stripped from the APK (Automatic stripping, build scenes have none) -> Custom
  stripping enforced by the build. Device confirmation pending.
- #9 yellow ring: noise ripples capped by distance from the eyes and faded up close. #4 wardrobe: no fake ticks while a drawer opens,
  icons drawn with alpha over a light backing, icons refresh as they load, other drawers prefetched.
- NOT done yet: #11 (mug / plate not put back in Practice), #12 (practice board far / small), #14 (red glove hint inside the book),
  #8 (book held tilted / too close - mostly caused by the stuck release). No new APK built.
- #11 / #12 / #14 done (2026-11-12): PracticeTidy returns practice items left on the floor / out of the room for 4 s (not while held);
  the practice board stands next to the bench facing the start, 1.25x; the round-start demo shows its glowing copy only once the ghost
  hand lifts it (it sat inside the real book). `recipe_diag practice12` 3/3, ui12 9/9, rules 165/165. All c15 items now addressed in
  source except #8 (book tilted / close - expected to go with the release fix). Next: build APK c16 and test on the Quest.
- APK BUILT 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.7_c16.apk` (release-signed, versionCode 16 / 0.6.7, 292,023,062 bytes, sha256
  2d40d351ce049c3487b3dd69cfa34969b80c053c512d41a4da1c312558bc13c9). Contains every c15 fix. Size +116 MB vs c15: keeping ALL lightmap / fog
  shader variants (URP Lit alone 50 MB). The build setting is now narrowed to non-directional lightmaps + linear fog only (next build).
  Not installed (Quest adb still "unauthorized" at last check). c13-c15 kept. The first c16 attempt (2026-11-12) was cancelled; the
  reason was not recoverable (log overwritten by two Editor restarts); the rebuild on 2026-11-15 succeeded unchanged.
- INSTALLED 2026-11-15: c16 (0.6.7, versionCode 16) on the owner's Quest 2 via adb install -r (now authorized). Not yet played.
- c15 grab log pulled (`QA_Evidence/quest_logs/shhh_grablog_c15.txt`): at 15 s, relaxed hands read grip 0.92 (left) / 1.00 (right) -
  confirms the release root cause (c15 grip read open hands as a fist; release needs < 0.4). c16 uses per-finger curl + open-hand release.
- APK BUILT 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.8_c17.apk` (release-signed, versionCode 17 / 0.6.8, 186,047,469 bytes, sha256
  5e014552690c72569ee9feeb10afdc2d9e1f48184e4c9a57aee398d3489188e7). c16 + V13 audio (Kevin MacLeod tracks: menu / match rotation / final
  minute; comedy stingers + UI; night ambience off) + narrowed lightmap/fog shader variants (-106 MB vs c16). Not installed yet. c13-c16 kept.
- INSTALLED 2026-11-15: c17 (0.6.8, versionCode 17) on the owner's Quest 2 via adb install -r (app data kept). Not yet played.
- V13 FINGER-GUN LOCOMOTION 2026-11-15 (owner: "make the teleport easier, using finger gun gesture and same for walking") - in source,
  not in an APK yet (c17 still has the palm-up pinch). Empty hand, index out + three fingers curled, thumb UP 0.2 s = cocked. Right hand:
  arc from the index finger, DROP the thumb = hop (lift it to aim again). Left hand: point, HOLD the thumb down = walk that way, thumb up
  = stop. A poke (thumb tucked from the start) never cocks; the gun pose never latches a grab. OVR detection: per-finger curl + thumb-tip
  distance to the side of the index / middle finger (down < 0.32, up > 0.45 hand lengths); GUN on/off + thumb lines go to the grab log
  for tuning. Hints / wrist / practice board / play-area reminder texts updated. `recipe_diag teleport12` 9/9 (UNITY PLAYMODE, simulated
  hands). The real-hand thresholds are untested on the Quest.
- APK BUILT 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.9_c18.apk` (release-signed, versionCode 18 / 0.6.9, 186,051,061 bytes, sha256
  a59a9367210391f83f256b082dc54be833abff971c2604cf41be8edb25430a83). c17 + finger-gun hop / walk. Install pending: Quest adb "unauthorized". c13-c17 kept.
- INSTALLED 2026-11-15: c18 (0.6.9, versionCode 18) on the owner's Quest 2 via adb install -r (app data kept; adb authorized after an adb server restart). Not yet played.
- OWNER, Quest c18 (2026-11-15): "both hands walk" (no arc on the right hand). Grab log `QA_Evidence/quest_logs/shhh_grablog_c18.txt`:
  finger gun detected on both hands (thumb down r 0.22-0.32, up 0.44-0.63). Right-hand walking only happens with the saved style "Walk only"
  (PlayerPrefs, survives updates; switched by the wrist / menu "Walk / hop" button). Fix in source: Settings.LocoVersion - every save older
  than V13 is reset once to walk + hop (right hand hops); style switches and the active style are written to the grab log.
  `recipe_diag teleport12` 10/10 (old_walk_only_save_reset_once). Not in an APK yet.
- APK BUILT + INSTALLED 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.10_c19.apk` (release-signed, versionCode 19 / 0.6.10, 186,053,193 bytes, sha256
  c0ba0e55b0010d98e3c2097a48bd7e5c2f84facff4e4eff715fd92107a01af3f). c18 + one-time locomotion reset (walk + hop). Installed via adb install -r (app data kept). Not yet played. c13-c18 kept.
- APK BUILT + INSTALLED 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.11_c20.apk` (release-signed, versionCode 20 / 0.6.11, 186,094,343 bytes, sha256
  338ebb191b86f09e30d421c632d0f42f507e03741eca8957e9591e4887e09922). c19 + sound budget, Mom voice director, calmer Mom (warning reset 30, calm pets quiet, slower starving cries), lid / creak variants. Installed via adb install -r (app data kept). Not yet played. c13-c19 kept.
- OWNER, Quest c20 (2026-11-15): "everything working but the vibrations appear again, I think fps". Grab log
  `QA_Evidence/quest_logs/shhh_grablog_c20.txt`: FrontEnd median 37 fps (88 % of frames over 21 ms, hitches of 495 / 1111 ms, 4-8 Genies
  skinned meshes up to 97k verts in view), Practice 57 fps; c18 log: FrontEnd 45.8, Practice 62.1 (fewer avatars in view). Below 72 Hz in
  every scene in both builds, so it is not a c19/c20 code regression alone. In source (not in an APK): PerfMonitor asks for sustained-high
  CPU / GPU levels (the app never set them) and logs Meta app CPU / GPU frame time + display Hz every 5 s, to tell CPU-bound from GPU-bound.
- APK BUILT + INSTALLED 2026-11-15: `Builds/Quest2_3_3S/SHHH_alpha_0.6.12_c21.apk` (release-signed, versionCode 21 / 0.6.12, 186,103,231 bytes, sha256
  90ff01add355f949429a614c007c72bb87285533db3a84af95e73ae36431cb06). c20 + sustained-high CPU / GPU levels + app CPU / GPU timing in the perf log. Installed via adb install -r (app data kept). Not yet played. c13-c20 kept.
