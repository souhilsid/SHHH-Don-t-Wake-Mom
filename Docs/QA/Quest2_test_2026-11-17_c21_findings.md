# Quest 2 hands-on test - build c21 (0.6.12), 2026-11-17

Source: the owner's headset recording `com.oculus.vrshell-20261117-122141-0.mp4` (12:16, 1920x1080, hand tracking).
Method: one frame every 3 s as contact sheets (`QA_Evidence/quest_c21_video/sheet_00..10.jpg`), crops of the task strip's fps number,
loudness per 10 s. Status: **REAL QUEST TESTED (owner)**. The headset was not connected afterwards, so the on-device log (with the new
CPU / GPU timings) could not be pulled yet.

Timeline: 0:00 Quest home -> 0:06 Unity logo -> **0:12-0:20 dark, silent** -> 0:21 Pick a mode -> 0:33 Wardrobe (hair, eyes, save) ->
1:00 Solo, 10 min, Normal bots -> 1:12-11:15 the match (living room, kitchen, laundry; slippers, coasters, towels) -> 11:18 final
inspection -> 11:24-11:36 "Who messed with Nell's job?" -> 11:42 results (Tess wins, you 2nd, 74 pts) -> 11:54 Friends -> room YFFW ->
12:09 back -> 12:12 quit.

## A. Performance (the "vibration")
1. **Frame rate stays well under 72 in the match**: the strip shows 34, 36, 39, 49, 51, 18 (!), 36 fps (crops at 1:15-11:40). The menu
   shows 36 fps. The whole match judders on head turns. (c21 asked for sustained-high CPU / GPU; still far from 72.)

## B. Start-up and menus
2. 0:12-0:20: about 8 s dark and completely silent after the Unity logo (no loading card visible in these frames).
3. **The hop arc fires while using menus**: "can't land here: not walkable floor" at 0:30 and 1:00, "can't land here: not a floor" at
   11:51 / 11:57, a red cross on the wardrobe at 0:57 - pointing at a card with the thumb up is a finger gun.
4. 0:33: the wardrobe board appears with its left part black for a moment.
5. **After the match the task strip ("Pair the slippers 7") stays on** in the results, the front hall and the Friends room (11:42-12:09).

## C. During the match
6. **"Edge of your play space - walk with your left finger gun..." is on screen almost the whole match** (1:12 to 11:45, even on the
   results board) - it never goes away while the owner stands near the edge.
7. **The hop arc appears during normal hand use** (holding / reaching for items: 1:24, 1:51, 5:54, 6:00, 6:42, 10:15-10:24, 10:42,
   11:15): "drop your thumb to hop" / "can't land here" labels pop up while the owner is working, not moving.
8. Mom still wakes often: 3:33 (cat yowling), 4:51 (dog barking), 7:12, 8:27 (cat yowling), 9:36 (cat yowling), 10:45 (dog barking),
   11:15 - mostly hungry pets; several "no points lost - the cat / dog acted on its own" warnings. The pets were not fed.
9. The "slippers" are plain coloured blocks (pink, teal, purple boxes) - they do not look like slippers (4:51-6:15, 8:45-9:09).
10. Held items come very close and fill the view: the purple slipper box at 7:30-7:33, the pink one at 6:12-6:15.
11. Red noise ring right on the hand near the washer (10:48, 10:54, 11:03), with the Quest's own boundary lines (blue / red) - the owner
    was at the play-area edge.
12. The camera goes inside walls / door frames when standing close (1:54, 2:15, 4:18).
13. The yellow task hint line at the bottom ("slipper: on the sofa in the living room") is cut off by the bottom of the view all match.

## D. Sound (loudness per 10 s)
14. The menu music is about 8 dB louder than the match (menu -27 dB RMS, match -30 to -38 dB). No clipping.

## Worked
Genies wardrobe (hair / eyes, starter looks, save); solo setup; finger-gun hop with valid / invalid spots and labels; walking;
pairing slippers on the board; coasters; towels; wrist watch (time, Mom state, points, jobs); pause menu; final inspection and the
"who messed with Nell's job" confrontation; results; Friends room creation (YFFW, bots fill seats). No crash.

## Owner's fix list (2026-11-17) - in source, not in an APK yet
1. **View inside walls / door frames** -> PlayerRig: the view darkens as the head enters a wall or furniture (head sphere r 0.10 m,
   ComputePenetration; dark ~3 cm past the surface) with a "step back" caption; nothing is moved, no collision removed.
   `recipe_diag teleport12` 11/11 (head_in_wall_darkens: 12 cm away 0.00, 4 cm inside 0.94, back out 0.00).
2. **Multiplayer Qatar <-> Algeria "connection lost"** -> each headset picked its own best Photon region (the room board showed
   "region uae" in Qatar); a room exists only in its region. Now everyone uses ONE region (`ShhhNet.OnlineRegion = "eu"`). The room
   carries the game version; a player on another build gets "different game versions ... both need the same update" instead of a dropped
   link; shutdown reasons are explained in words; every network event goes to the grab log (adb-pullable from both headsets).
   NOT yet tested across two machines (needs build_win + net_multi, or two headsets).
3. **Bin lid by grab** -> the lid's whole front rim is now the handle (was one point, 8 cm reach); the hand keeps the spot it grabbed.
   `recipe_diag pets` 8/8 (lid grabbed 12 cm from the old point -> opened to 100 deg; trash binned by hand).
4. **Fridge + food (new)** -> original stylized models (`Tools/blender/make_fridge_food.py` -> `Art/ThirdParty/SHHHKitchen/`):
   mint fridge (hollow, 2 shelves, hinged door, magnets + kid drawing), pizza, burger, juice carton, apple. The baked kitchen fridge is
   replaced in place (HouseAdditions.BuildFridge, front -z). New recipe **KT17 "Put the groceries in the fridge"** (open by the handle,
   food on the shelves, close gently; sabotage: reopen / take food out). `recipe_diag KT17!` 28/28 (door 0 -> 115 deg by the handle,
   4 items placed on shelves, closed without a slam, complete 3/3, +67.5, rival sabotage + repair). Recipes 72 -> 73.
5. **Only easy jobs** -> 200 solo 10-min matches gave the player 65 % Easy / 8 % Hard; plates 8 %, load the washer 2.5 %. Featured jobs
   (KT01 plates, KT17 fridge, LD04 / LD05 washer) now go to human seats first; the room's other jobs match their difficulty; points
   weighted by difficulty (easy 48 / medium 60 / hard 78). Now: kitchen = plates 94 / fridge 106 of 200, laundry = washer job every match.
   Rules 166/166 (CM-01 human policy wins 8/30).
Editor online host (net_auto_editor host:QZRT:2): "Host in session SHHH-QZRT region=eu ... version=0.6.12" - the fixed region and the version
tag work; a real two-country join is still to be tried on two headsets.
`recipe_diag KT01!` (3 runs) stops after the tap opens: both simulated hands are destroyed by the engine in one frame while Windows reports
"The system is running out of memory ... used paged memory 94 % (57.6 of 61 GB)" - the PC's commit limit (Edge 10.8 GB, VS Code 4.1 GB,
Python services 3.4 GB, Unity 6.6 GB ...). Not a game fault; KT01 itself is unchanged and reached scrub + tap open each time. Re-run with
fewer apps open. (SimHandSource now logs who destroys it.)
- APK BUILT + INSTALLED 2026-11-17: `Builds/Quest2_3_3S/SHHH_alpha_0.6.13_c22.apk` (release-signed, versionCode 22 / 0.6.13, 186,242,961 bytes, sha256
  f9027cc2ceca4cc928ed4e4fd9bdb6859673b3ee759d80c7018e705b418c75d2). c21 + the five fixes above (head-in-wall fade, one Photon region "eu" + version check,
  bin rim handle, fridge + KT17, featured jobs + difficulty points). Installed via adb install -r (app data kept). Not yet played. c13-c21 kept.
- APK BUILT + INSTALLED 2026-11-17: `Builds/Quest2_3_3S/SHHH_alpha_0.6.14_c23.apk` (release-signed, versionCode 23 / 0.6.14, 186,303,745 bytes, sha256
  33e1f0a4a62492fbc86d6c97ba8667a8d8faa23c1ddbb567259008fa5e7ccfaa). c22 + soap pump pressed by any part of the hand over the pad top (RecipeRuntime.HandPress;
  fingertip / pinch reach points also used by the sink lever) + InteractionHints (highlight near hands, next-step arrow, ghost hand).
  Editor: KT01 pump_press_any_gesture OK (fingertip 3 doses, flat palm 1); hints13 3/3 (screenshots of arrow / ghost not confirmed - window covered).
  Installed via adb install -r (app data kept). Not yet played. c13-c22 kept.
