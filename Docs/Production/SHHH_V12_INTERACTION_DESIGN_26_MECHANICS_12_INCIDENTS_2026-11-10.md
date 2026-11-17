# SHHH! V12 — Complete VR physics & interaction design (owner spec, 2026-11-10)

Owner's target gameplay specification (NOT a claim of implementation). Precedence: V12 > V11 > V10 > V9 > V8 > Nov 9.
Owner order for this phase: **(1) import the asset packs (Tiny Treats, laundry utility, Ultimate House), (2) shared interactions (handles,
drawers, ...), (3) laundry and the other chores, (4) hand-held pouring and sharing water levels with other players.** Style: stylized,
hand-painted, cartoonish, chunky "Fortnite-like" readable models. Reference images (chat, 2026-11-10) are reference only.

## Foundation — every interactive object has six connected systems
1. VR hands/fingers: grasp, pinch, push, rotate, hold, release.
2. Physical object/mechanism: mass, collisions, friction, hinges, joints, movement.
3. Simulation/world state: water quantity, stains, deformation, stability, object state.
4. Visual/audio/tactile: hand-painted shaders, cartoon VFX, spatial audio, haptics.
5. Game consequences + sync: task progress, Mom's hearing, sabotage, evidence, Photon, bots.
6. Comfort: responsive, forgiving; never throw the player's camera; no scientific-grade simulation needed.
A falling plate rotates under gravity, hits by material, may bounce or break safely, makes sound + VFX, and creates an event Mom can hear.

## Part I — the 26 mechanics (behaviour contract)
A. Cleaning and liquids
1. **Scrubbing** — plate in one hand, sponge in the other; cleaning only where the sponge contacts AND moves; soapy+wet > dry; stroke length/speed/coverage matter; stains vanish progressively; dirty -> soapy -> clean. Air-waving, wrong side, bare hands do not clean.
2. **Wiping** — cloth from a cupboard, wet it, spray, wipe; local dirt + wetness maps; clean path per pass, water streaks that fade or need a dry cloth; dirty cloth smears, dry cloth weak on sticky stains. Mirrors, windows, counters, sinks, furniture.
3. **Sweeping** — broom one/two hands; bristles contact debris; big pieces move by forces, crumbs as clusters; sweep toward dustpan gathers; dust puffs; no progress in empty space.
4. **Pouring** — tilt jug/bottle/bucket/cup; flow from angle, opening and remaining volume; receiver fills; misses make persistent puddles to clean.
5. **Squeezing** — compress a wet sponge (grip/two hands); visible squash; expels water/soap by wetness x compression; slow recovery; force inferred from grip + tracking (no force sensor claimed).
6. **Twisting** — faucet knob / bottle cap rotate on the correct axis with stops and resistance; partial opening = partial flow; caps travel the thread then separate. Never an "Open" button.
7. **Pulling** — hand stays on the drawer handle; rails, limits, friction; contents stay inside; mechanical stop, never detaches or passes through.
8. **Pushing** — carts, stools, chairs, bins move by mass/friction/wheels/collisions; things on top wobble; heavy fixtures don't move like toys; camera never thrown.
9. **Pumping** — sponge under the nozzle, push the plunger; compress to a threshold -> measured dose -> spring return; no sponge = soap wasted in the sink; over-pumping -> soap overflow.
10. **Shaking** — intentional alternating motion (not tracking noise); contents mix/shift, pillows release dust, open containers spill.

B. Food, toys, fabric
11. **Stirring** — spoon from a drawer, hold the bowl, stir INSIDE the mixture; speed + coverage drive uniformity; swirl shader, colour blending, viscosity; spoon contact sounds; outside the bowl doesn't count; rivals may tip the bowl.
12. **Slicing** (safe cartoon) — blunt cartoon tool through a defined cutting region with correct edge/orientation; ingredient splits into pieces with own colliders; painted inner faces; pieces stack/fall into bowls. No button swaps.
13. **Rolling** — dough: rolling pin contact path flattens a deformable region progressively; toys: rigidbody cars/balls, wheels turn, friction; a ball rolling toward Mom = noise incident.
14. **Folding** — pick a shirt by two points, spread, align sleeves, fold sides, fold bottom; optimized skinned/deformable mesh with corner/edge handles; folds depend on real alignment; folded shirt can be lifted/stacked/put in a drawer; releasing never auto-folds.
15. **Stretching** — two hands pull an elastic item; deformation grows with distance to a safe max; controlled spring retraction; never impulses the head/camera.
16. **Stacking/balancing** — mass, colliders, COM, friction; bad stacks lean/wobble; steady with a hand; creaks/taps; sabotage by moving a support -> collapse recorded as evidence; no "stable because inside a trigger".
17. **Catching** — falling item + closing hand in a timing window -> stable hold with believable momentum; small comic VFX; miss = it falls; catching a falling plate can prevent Mom waking (modest reward).
18. **Throwing/tossing** — release velocity from recent linear + angular hand motion, bounded; gravity; settles in basket/bin to count (bouncing out doesn't).

C. Appliances, mechanisms, environment
19. **Vacuuming** — take from storage, grip, nozzle near floor, real switch; body/handle/hose/nozzle constraints; only debris in the active suction region; nozzle can clog; motor loop varies with load; loud near Mom; sabotage by obstructing.
20. **Sliding** — trays/organizers/books along constrained axes; friction, limits, alignment; settles when inserted; never through walls or snapping across the room.
21. **Hinging** — grab the handle, rotate about the real hinge with limits, velocity response, soft-close near the latch; washer door only when the machine allows; intermediate angles; quiet click vs loud slam (Mom).
22. **Connecting** — compatible parts need position + orientation; subtle magnetic assist near the fit; joint/socket holds; latch moves + click; wrong orientation doesn't connect; repairs after sabotage.
23. **Plug/unplug** (safe cartoon connectors) — guided insertion axis, depth stop, changes appliance state; latch/indicator; never an invisible trigger.
24. **Pendulum** — hanging lamp swings about its anchor, damped; can be steadied by hand; light/shadow move; creaks vs rattles.
25. **Spring compression** — restoring force, return motion, capped impulse to light objects; spring drawers, toys, switches, dispensers; never solver explosions.
26. **Two-handed wringing** — towel held at both ends, opposite wrist twist under tension; twists along its length; water content drops with valid wringing, drips into the bucket (or makes floor wet patches).

## Part II — the 12 comedy incidents (emerge from the same world physics; one network authority decides cause)
1. Plate dominoes (unstable rack tips, plates collide in sequence; catch/brace/repair) · 2. Runaway ball (toward stairs/Mom; chase/catch/block) ·
3. Wobbly furniture (steady/reposition) · 4. Squeaky floorboard (step carefully/avoid) · 5. Vacuum hose snag (reverse/reposition/free) ·
6. Overflowing bucket (sloshes when carried, spills when tilted; carry carefully, reduce, mop) · 7. Rotating chair (grab backrest to slow) ·
8. Spring-loaded drawer (pops open; catch the handle, close quietly) · 9. Swinging lamp (reach up, damp it) · 10. Pet chase (Biscuit/Mochi chase a
squeaky toy; retrieve/redirect/prevent) · 11. Unstable book tower (catch/stabilize/rebuild) · 12. Soap overflow (stop pumping, clean the mess).
A falling plate is the ordinary plate physics, never a special scripted fall; players AND AI siblings can catch it. Other clients reproduce sound/VFX.
