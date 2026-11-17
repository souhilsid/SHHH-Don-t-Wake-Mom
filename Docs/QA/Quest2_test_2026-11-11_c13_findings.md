# Quest 2 hands-on test — build c13 (0.6.4), 2026-11-11

Source: the owner's headset recording `com.oculus.vrshell-20261111-170037-0.mp4` (5:48, 1920x1080, 30 fps, hand tracking).
Method: every second as timestamped contact sheets (348 frames), suspicious moments re-extracted at 4–6 fps,
audio silence scan. Problems that last < 1 s between sampled frames can be missed outside the zoomed segments.
Status: **REAL QUEST TESTED (owner) — problems below**. Nothing fixed yet.

## A. Stability
1. **FIXED (c14, not yet re-tested on Quest) - crash at 1:16.7** — in WARDROBE, tapping "›" for Glasses (all glasses locked): the game cuts to black
   instantly (no fade) and the Quest home appears. The headset log could not be read afterwards (headset not reachable).

## B. My own Genies body (new in c13)
2. **FIXED IN SOURCE (not in an APK yet) - My arms block my view.** Whenever a hand reaches forward the sleeve (pink/orange/green) fills 30–50 % of the
   view and hides the hand and the object: 2:36–2:47, 3:11–3:35, 5:05–5:21. Shoulders sit right behind the camera, the
   kid body is scaled up ~1.15 and the Genies sleeves are chunky.
3. Crouching shows my own knees/legs filling the view (3:44–3:45).
4. My body is drawn over the head-locked task HUD (3:44: knees cover the task strip).

## C. Grabbing (hand tracking)
5. **FIXED IN SOURCE (not in an APK yet)** - Pinch grabs mostly fail: mug (2:29–2:31), coasters (2:55–3:35), the Practice book with the red target dot
   (5:04–5:35, ~30 s of attempts). The cups-and-coasters job is still 0/2 at 4:12 after ~2 minutes.

## D. Bots / other bodies
6. At match start the bots spawn on top of me; their nameplates fill the view (2:20).
7. Bots walk through me: Nell's head and hair pass through the camera (3:55–3:56); Tess at 4:30 and 4:46.
8. Nameplates are huge up close (no size limit / fade by distance) (2:20, 3:50, 4:46).
9. A black silhouette figure flashes while a Genies body is loading (2:19.0).

## E. Mom and feedback
10. "MOM IS UP! −20 PTS" at 3:54 without ever seeing Mom or why; Mom only appears as text upstairs at 4:08
    ("*yawn* Don't make me come back out."). The penalty stays on the watch until the end.
11. Hungry pets show very large warning triangles + WOOF!/MEOW! (3:42, 4:30, 4:48–4:54); unclear whether a pet woke Mom
    and I was blamed.

## F. Menus / UI
12. Loading: ~14 s and ~17 s of grey void with a flickering black wedge and total silence (0:13–0:27, 1:32–1:46).
13. The wrist watch opens whenever the left palm turns up and covers a large part of the view (0:33–0:43, 1:46–1:48,
    2:27, 2:35–2:52, 4:12–4:22).
14. The main menu panel drifts with the head and is often seen at a steep angle / half off-screen (0:49–0:52, 1:03).
15. WARDROBE is the old pyjama/hat/glasses system ("Wardrobe – Nell", "3 locked"), not Genies customization; locked
    items give no feedback; the front-end stage mixes the old big-head cast with Genies bodies (0:49–1:16, 2:08–2:11).
16. "Customize" mixes pyjama colour and bot skill in one panel (0:58–1:04).
17. Practice HUD says "all done!" although the practice board shows steps (5:00 onward).
18. Job objects are hard to read: the "tray" looks like an open book, coaster spots labelled "stack 2" in tiny text, the
    job panel is unreadable at an angle (4:40 "Collect stray hallway toys"), the hint line under the HUD is tiny.

## G. Pets / world
19. The hungry dog waits in its bed; its bowl reads as a flat red disc (3:38–3:45, 4:29).
20. A yellow smiley ball sits at the very edge of the living-room side table the whole match (2:19–3:35) — check what it
    is and whether it floats.
21. The Quest boundary grid appears while reaching the dog bed (3:43): the job layout needs more walking than the play
    space allows.

## Worked
- Solo match start, 5-min clock, jobs + "mug: on the side table in the living room" hint line on the HUD.
- Bots are Genies bodies and walk/carry; cartoon dog and cat present and animated; pause menu, house map, quit.

## Crash 1 - root cause and fix (2026-11-12)
- Reproduced in the Editor: native crash in the Genies SDK (`libgnUnityPlugin` `AssetBuilder.Build` on a thread-pool thread via
  `NativeGenieBuilder.RebuildAsync`, `delete_AvatarController`) - a Genies body was destroyed while the SDK was still building it.
- Triggers: each wardrobe "›" press (even on a fully locked row) rebuilt the family stage = destroyed 4 Genies bodies mid-build and started
  8 new loads (two per sibling); 6 fast taps ~ 50 loads. The Editor hit the same crash when a room was torn down right after Genies became ready.
- Fix: bodies are built only in the backend's hidden workshop (a private slot each, unique name), handed over when fully loaded, a request
  nobody wants any more is skipped before the SDK starts or disposed only after its build; a hung SDK load times out after 45 s; locked
  wardrobe rows change nothing (sound only) and wardrobe pieces never rebuild Genies bodies; SiblingAvatar drops duplicate look requests.
  Note: `ShowLoadingSilhouette = false` makes some SDK loads never return - keep it true.
- Evidence (Editor, real Genies SDK): `recipe_diag wardrobe_spam` - 6 taps: 0 loads (was 8 per tap), 4 bodies; 5 stage rebuilds mid-build:
  9 skipped, 4 disposed after build, 4 bodies, no crash. `recipe_diag avatars!` 8/8. APK c14 built for the Quest re-test.

## Problem 2 - my arms (2026-11-12)
- My own camera's copy of my Genies body: upper-arm triangles removed, forearms slimmed to 70 % and tapered to a soft end at the elbow
  (`SelfArmTrim`, on a private mesh copy). Genies skins the mesh to helper bones (`LeftArmTwist1..3Bind`, `LeftForeArmTwist1..3Bind`,
  `LeftHandBind`), so the parts are found by name. Friends still see my full body (they build it on their own machine).
- Evidence (Editor, real Genies, same run, full arms vs forearms only): my body covers 0.9 % of my view when reaching for a table (8.4 % with
  full arms), 1.4 % reaching far (11.8 %), 2.4 % looking at the watch (4.4 %); pinch points never covered. `recipe_diag avatars!` 11/11.
  Captures: `QA_Evidence/recipe_v7/avatar_self_*.png` and `*_fullarms.png`.
- Note for capture runs: the Unity window must not be covered by another maximized window (renders black).

## Hands looked corrupted (owner, 2026-11-12) - fixed in source, not in an APK yet
- Quest frames (0:37, 0:38, 0:44): flattened paddle fingers, thumb fused into the palm, stretched palms, fingers curling backwards, long
  stick wrists. Cause in `SimpleHandVisual` (hand-tracking path only, never exercised by the desktop labs): every glove bone was moved
  onto the real joint position (a child-size cartoon glove forced onto adult joint spacing = stretched skin, up to 171 % bone stretch)
  and finger roll used `Vector3.up` for the glove's rest pose but the palm normal for the live hand (fingers could bend backwards).
- Fix: rotation-only retarget - glove bones keep their own lengths, each joint copies only the real segment direction, roll from the
  back-of-hand normal measured the same way on rest and live, uniform size from the real middle-finger length, thumb + index chains bent
  (rotations only) onto the real tips for an exact pinch. The desktop / controller fallback path is unchanged.
- Evidence (Editor, synthetic adult OpenXR hand joints, open / fist / pinch / point, both hands): bone stretch 0 % (old 171 %), thumb +
  index tips 0.5-1.4 cm from the real ones, other fingertips 1.5-3.3 cm; `recipe_diag hands` 4/4; captures `recipe_v7/hands_new_*` vs
  `hands_old_*`. REAL QUEST: not yet re-tested.

## Grabs / interactions (2026-11-12) - fixed in source, not in an APK yet
- Root cause 1 (hand tracking only): `OvrHandSource` and `SimpleHandVisual` looked bones up as "OpenXR id, else legacy id", but both id
  sets are ONE enum numbered from 0. On a legacy skeleton XRHand_IndexTip (10) = Hand_Middle2, XRHand_ThumbTip (5) = Hand_Thumb3,
  XRHand_Wrist (1) = the forearm stub: the pinch point sat inside the hand (no hover highlight while the glove was on the mug, 5:10)
  and the glove joints were driven from the wrong bones. Ids now come only from the set that matches `skeleton.GetSkeletonType()`;
  the legacy thumb maps proximal/distal like OpenXR. (The scene sets one hand to XRHandRight; the other uses the prefab default.)
- Root cause 2: a grab could only start within 0.8 s of the pinch closing - players who pinch first and then reach (seen on the
  coasters) never grabbed. Now a held pinch takes the item once the fingers are on it (< 3 cm) and the hand has stopped; never with the
  palm up (the walk gesture) and only while actually pinching (not in the 80 ms release grace).
- Reach measured from the pinch point OR the index fingertip.
- On-device log: every pinch start / grab / miss with pinch strength, grip, skeleton type, palm-up, hand speed, nearest item + distance
  -> `/sdcard/Android/data/<package>/files/shhh_grablog.txt` (adb pull) and the Unity log ([SHHH][grablog]).
- Evidence (Editor, simulated hand): `recipe_diag grabs` 6/6 - pinch-then-reach grabs, walk gesture never grabs (also when it opens),
  normal pinch grabs, sweeping past does not, id rule, log written. `recipe_diag pets!` 8/8 (food bag, pour, bin lid, trash).
  The tracked-skeleton lookup can only be confirmed on the headset (grab log + glove shape).

## UI (2026-11-12) - fixed in source, not in an APK yet
- #13 wrist watch: opens only on a deliberate look (palm facing me > 0.85 AND my gaze within 25 deg of it, held 0.35 s); stays open
  while I tap it (facing > 0.6, gaze < 40 deg, 0.15 s close grace); never while busy; 20 % smaller.
- #14 menu boards: one shared world spot (lectern, tilted 10 deg); the next popup opens in the same spot if I am still facing it
  (< 55 deg, < 0.7 m); `LecternFollow` glides the board back in front over 0.5 s only after a clear turn / step away. Never head-locked.
- #12 loading: the fade is a full quad right in front of the eyes (the 0.4 m sphere showed a flickering black wedge) and a
  "SHHH! loading the house..." card shows on the black while loading; it leaves with the fade.
- #4 HUD over my body: every HUD material (text, icons, hunger bars) draws after the world with depth test off (queue 3990/3995,
  below the black fade at 4000). Icon / bar colour swaps reuse one cached on-top copy per material (they used to revert to the
  depth-tested material on the first change).
- #6 nameplates: hidden within 0.7 m, scaled down to 0.35x up close (full size from 2.5 m).
- #15/#16 Customize: "Your colour" + "My Genies look" (opens the Genies wardrobe); the bot-skill row lives in the Solo panel only;
  locked wardrobe rows play a "locked" sound and change nothing.
- #17 Practice HUD: "Practice: follow the board" + "book > shelf > cup > pan > walk > wrist menu" (no false "all done!").
- #18 (part): HUD hint line bigger (0.24) and wider.
- Evidence (Editor, simulated head/hands): `recipe_diag ui12` 8/8 -> `QA_Evidence/recipe_diag_ui12_v12.json`, loading capture
  `QA_Evidence/recipe_v7/ui12_loading.png`. Rules tests 165/165. Comfort / readability can only be judged on the headset.

## Bots / Mom (2026-11-12) - fixed in source, not in an APK yet
- Root causes found with a new lab (`recipe_diag mom12`, a real solo house match): Mom DID come down, but she stopped 0.3 m from the
  player - the camera was inside her, so she was never seen; the watch said "MOM IS UP!" whenever she only stirred in bed (Stirring /
  Suspicious / AlmostAwake are in-bed states) - in the run Mom was out ~3:09-4:08 and the -20 came at ~3:52 after a 33 s confrontation
  whose tray had been left at the side table; bots started 0.82 m from the player and came within 0.04 m of the head (NavMesh agents
  did not know the player exists).
- #6/#7 bots: a non-carving NavMeshObstacle follows every human head (`PersonalSpace`, local + remote); bots use medium-quality
  avoidance; a bot whose seat spawn is within 1.5 m of a human's starts at the nearest walkable spot 1.5 m away (host).
- #10 Mom: she stands 1.0-1.6 m from the noise, at least 1.1 m from every player, with a clear line to it, preferring a spot in
  front of me; she steers around players and turns to face me while looking / asking / judging. A Mom line above the task strip says
  what she heard and where ("MOM IS UP! She heard the dog toy crash in the living room - she's coming to look"), her question with
  the countdown, and the verdict with the reason ("Mom caught YOU: -20 pts, 15 s in bed - Saw Pip make the dog toy crash (living
  room)"; "warning, no points lost - the dog acted on its own"). Her lines go on the HUD when I can't see her. The penalty card says
  why. The confrontation tray glides back in front of me after a turn / step. Watch: "MOM IS UP!" only during the visit; the wall
  sign shows "MOM IS UP!" while she is out.
- #11 pets: the hunger badge hides within 0.8 m and shrinks up close; a pet-caused wake now reads "the dog barking ... warning".
- Evidence (Editor): `recipe_diag mom12` 9/9 (bots 1.71 m at start, >= 0.55 m from my head, Mom 1.9 m away and in my view 36.8 s,
  HUD lines, tray follows), `confront` 12/12 (lab updated: pets quieted, a hard item - the job's first item is now soft), `pets` 8/8
  (one earlier run failed the kibble pour with an empty bag, the rerun passed - known flaky lab), rules 165/165.

## Remaining items #18-#21 (2026-11-12) - fixed in source, not in an APK yet
- #18 the "tray" is now a wooden tray (floor, raised rims, end grips - visual only, no colliders) with a round ring per mug; the
  coaster spot is a coaster-sized round mark with a standing tag "2 coasters here" that turns to face the player (was a flat pad
  saying "stack 2" in tiny text). The job card turns toward the player (up to 70 deg, only from > 1 m away so it never stands in
  front of the job up close) and grows to 1.5x with distance. HUD task label: 2 lines (was 4).
- #19 dog bowl 27 x 7 cm (was 22 x 5) on a teal feeding mat with the dog's picture (the cat's bowl gets a purple one).
- #20 the yellow smiley ball is the dog's squeaky toy, a leftover room prop that belongs to no job; it rested on the side table
  (0 cm gap - it did not float). It now lies on the floor by the dog bed (HouseClutter, a clear side other than the bowl).
- #21 the play space cannot be made bigger by the game; near its edge the HUD now reminds you to walk with the hand gesture
  ("palm up, pinch and hold, then move it"). Edge = the runtime's play-area rectangle when it reports one, else 1.1 m walked with
  your feet from where the match started. On the Quest the play-area path is untested.
- Evidence (Editor): `recipe_diag props12` 7/7 (pictures `QA_Evidence/recipe_v7/props12_*.png`); regressions ui12 8/8, pets 8/8,
  mom12 9/9, rules 165/165.

## Walls missing on the Quest + teleport (owner, 2026-11-12) - fixed in source, not in an APK yet
- Root cause of "the house is not showing the walls, showing directly the outside" (Quest run 2:21, 3:50): `NightExterior.OpenWindows`
  copied the living room's architecture mesh at runtime to delete its painted window panes. That mesh (V5_LivingArchitecture.fbx:
  all living-room walls + windows in ONE mesh) has Read/Write OFF; the Editor can still read it, an APK cannot - on the Quest the copy
  came out empty and every living-room wall disappeared (only the curtains stayed). The Editor labs could never show it.
  Fix: the pane material slots are swapped for faint glass (URP Unlit transparent, the UI's material) - no mesh access, identical in
  the Editor and on the Quest. Other runtime mesh edits (HouseAdditions, SoftCloth, DetachedLimbs) already skip unreadable meshes.
- Teleport ("hop"), on by default: RIGHT hand palm-up pinch held 0.4 s shows an arc (point your fingers where you want to go),
  release to hop with a quick blink. The left hand still walks. Wrist button / Settings cycle: walk + hop -> walk only -> hop only.
  The arc stops at the first thing it hits (the old optional arc flew on through walls), lands only on house floor connected to
  where you stand (not table tops / beds), within 7 m, with head room and nobody standing there; teal when valid, coral when not.
  Found on the way: a palm-up pinch (walk / hop) could pull-grab an item its pointer ray crossed - palm-up hands no longer pull from
  a distance; passing near an item no longer cancels a walk / aim already in progress.
- Evidence (Editor): `recipe_diag walls12` 3/3, `teleport12` 5/5 (hop lands 0 cm from the marker; wall and table top refused; left hand
  walks 1.03 m), regressions grabs 6/6, ui12 8/8, rules 165/165. The wall fix can only be confirmed on the headset (APK).

## Front-hall Family Stage showed old stand-ins (owner frame, 2026-11-12) - fixed in source, not in an APK yet
- Cause: each kid was built as the old big-head character first and swapped for Genies when the load finished. Nell / Rory / Tess
  load the seat's starter look, which existed only after the device had built it from the Genies SDK inventory (login + up to 90 s,
  cached per device) - on the Quest they never got one and kept the stand-ins while Pip wore the player's saved look. Picking a kid
  rebuilt the whole stage (all four Genies bodies thrown away and reloaded).
- Fix: the five verified clothed starter looks ship with the game (`Resources/Genies/shhh_genies_presets_v3.json`: Genies asset ids,
  colours, body shape - nothing account-related), used when the device has no built looks. While a Genies body loads, a kid shows a
  soft glowing silhouette in its colour (never the old character); the old character appears only if the Genies load fails or takes
  over 25 s, and a look that becomes available later is retried. Picking a kid re-dresses in place (only the kids whose look changes
  reload). The same applies to bots and friends in the house.
- Evidence (Editor): `recipe_diag stage12` 4/4 - all four in Genies after 1.4 s, stand-ins shown 0 times (was 3/4 samples), pick keeps
  the bodies, 5/5 shipped looks clothed (pictures `stage12_t0.png` silhouettes -> `stage12_loaded.png`). mom12 9/9, rules 165/165.
  Note: Editor captures after 14:50 rendered black because the Unity window was covered (Vulkan) - the avatars lab's view-coverage
  numbers from that run are not valid (that code did not change; it passed with real renders earlier today).

- Loading screen (#12, owner re-flagged the 0:20 grey void + black wedge of c13): already fixed in source (full-view black fade quad +
  "SHHH! loading the house..." card); also the task strip no longer draws over the loading card (hidden while the fade is > 40 %).
  `recipe_diag ui12` 9/9 (new check hud_hidden_while_loading). Not in an APK yet.

## Own body: complete clothes + real hands, UI never hidden (owner, 2026-11-12) - fixed in source, not in an APK yet
- Owner choice: "Full clothes + real hands". The sleeves are no longer cut away (0 triangles removed; forearm sleeves only slimmed to
  0.85), my Genies hands are shown and their fingers follow my tracked finger joints (rotation-only retarget in the hand's own frame;
  friends' hands still curl with the grip); the gloves keep tracking unseen (SimpleHandVisual.HiddenForBody). My body stands 5 cm further
  back under my eyes so the shoulders and sleeves stay lower in the view.
- UI on top of my body and hands: task strip, wrist watch, every menu board (LecternFollow re-checks twice a second) and Mom's
  confrontation tray draw after the world with depth test off; button colour / hover changes keep it. Found on the way: URP re-derives an
  alpha-clipped material's queue from _QueueOffset when it is first drawn, so the on-top copies of icon materials fell back to 2450 (the
  icons of the HUD and boards could be hidden after all) - TaskHud.Top now sets _QueueOffset too.
- Evidence (Editor): `recipe_diag avatars` 15/15 incl. own_outfit_complete, own_genies_hands_shown, own_fingers_follow_tracking
  (fingertip-to-wrist open 21 cm -> fist 9 cm; pinch brings thumb and index tips to 3.8 cm), ui_over_my_body; ui12 9/9, hands 4/4, rules 165/165.
  NOT measured: how much of the view the complete outfit covers when reaching - the Editor renders black while other windows cover Unity
  (the user was working), so the pixel checks read 0 %. To be judged on the headset.

## Build
- APK BUILT: `Builds/Quest2_3_3S/SHHH_alpha_0.6.6_c15.apk` (release-signed, versionCode 15 / 0.6.6, 175,767,016 bytes,
  sha256 14d0e5fac48998c8c977a3ff4462797f605966c63785d0b770e6195b18d8f2ab, 864 s, 54 warnings). Contains every fix since c13 (Genies crash,
  own body, hands, grabs, UI, bots / Mom, job objects, pets, walls, teleport, starter looks, loading). c13 / c14 kept. NOT installed yet -
  owner will connect the Quest and ask. Next: install (adb install -r), owner test, then pull shhh_grablog.txt.
