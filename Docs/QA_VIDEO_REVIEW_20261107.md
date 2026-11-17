# Quest 2 recording review — 2026-11-07 (build c7)

Sources: `com.oculus.vrshell-20261107-201409-0.mp4` (155 s, menu → Living Room round), `…-202049-0.mp4` (56 s, menu → Play with Friends → quit). `…-124508-0` is the pre-V5 build, not reviewed again.

| # | Time | Problem seen | Severity | Fix in c8 |
|---|---|---|---|---|
| 1 | A 0:54–1:24 | Toy cars on the floor: the player had to crouch and fumble at floor level to pick up their job items | **Critical (fun)** | **Magic pull**: point an open hand at an item up to 3.5 m away, it highlights, pinch, and it flies gently into your hand. Same Round.Grab rules; impacts are ignored during the pull. |
| 2 | A 0:54, 1:24, 2:18 | The yellow "my job" chevron was huge and sat on top of the item, covering it | High | 55 % size, floats higher, hides when you're within 0.85 m |
| 3 | A 1:12 | Wrist menu popped open while reaching down to grab | High | Opens only if the palm really faces the eyes (dot > 0.8) and neither hand is holding or hovering |
| 4 | A 1:36 | Purple teleport/move ray appeared while grabbing in the toy box | High | Locomotion gesture is blocked while a hand hovers an item in reach |
| 5 | A 1:36, 2:06 | Opaque orange/green target discs and glowing ghost models flooded the toy box and sofa | Medium | Targets are thin translucent owner-colour rings; ghost previews only for your own job, at 16 % alpha |
| 6 | A 0:36–0:42, B 0:32 | Intro popup and "Play with Friends" popup spawned on the hall menu board, text over text | High | The hall board hides while a popup is open |
| 7 | B 0:36–0:44 | The popup was turned edge-on (it oriented to a missing head reference) | High | Faces the real camera |
| 8 | A, B | Hands are a dark rubber-glove mesh | Medium | Warm toon skin material on the tracked hand meshes (Simple Hands swap still pending) |
| 9 | A 0:06–0:22, B 0:04–0:22 | About 12 s of black/grey void with a stray grey polygon between the Unity splash and the menu | Medium | **Open**: branded boot scene + faster first load |
| 10 | A 0:48 | Dark wall board text is small/low contrast at distance | Medium | **Open**: larger type on in-room boards |
| 11 | A 1:54–2:00 | Mom is the CC0 adult mesh; her subtitle is tiny | Medium | **Open**: Astra Mom (reduced) + bigger subtitle |
| 12 | — | "Play with Friends" still says not available | High | **Open**: networked match (Fusion is proven in the editor) |
