"""Writes COMPLETE_FOOTAGE_ANALYSIS.csv - every candidate moment found in the Quest recordings (contact sheets every 3-4 s for all
recordings + fine frame strips at 1-4 fps for every physical interaction, see Frames/), plus the features searched for and NOT recorded."""
import csv, os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
H = ["id", "source", "source_file", "t_in", "t_out", "action", "feature", "physical_detail", "visual_1to5", "comedy_1to5", "evidence_ref",
     "rec_dur_s", "edit_treatment", "proven", "limitation"]
F = {"F04": "com.oculus.vrshell-20261111-170037-0.mp4 (c13)", "F05": "com.oculus.vrshell-20261112-170302-0.mp4 (c15)",
     "F06": "com.oculus.vrshell-20261115-170859-0.mp4 (c20/21)", "F07": "com.oculus.vrshell-20261117-122141-0.mp4 (c21)",
     "F09": "Remotion/Raw/Records-20261117T150733Z-1-001/Records/com.oculus.vrshell-20261117-174620-0.mp4 (c23, newest)"}
R = [
    ("M01", "F09", "0:30.0", "0:32.5", "SHHH! loading the house... card", "boot/loading", "-", 2, 1, "ContactSheets/F09_01.jpg", "1.5", "skip", "yes", "black around the card"),
    ("M02", "F09", "0:33.0", "0:40.0", "Pick a mode: Solo / Friends / Practice / Wardrobe cards poked with a finger", "main menu", "finger poke on cards", 4, 1, "ContactSheets/F09_01.jpg", "3", "clean cut, slight push-in", "yes", ""),
    ("M03", "F09", "0:42.0", "1:20.0", "Genies wardrobe: hair/face/body/tops grids; the avatar on the pedestal updates", "Genies customization", "poke to try on, avatar changes live", 4, 1, "ContactSheets/F09_01.jpg", "3", "one shot of tops grid + avatar", "yes", "grids show blank tiles while loading 0:42-0:57"),
    ("M04", "F09", "1:33.0", "1:37.0", "Play with friends: Create room / Join with code", "private multiplayer menu", "finger poke", 4, 1, "ContactSheets/F09_01.jpg", "2", "", "yes", ""),
    ("M05", "F09", "2:55.0", "3:06.0", "Room PDBE (region eu): Player62 joins at 2:59, both READY, START", "REAL two-human online lobby", "-", 4, 2, "Frames/LOBBY.jpg", "4", "hold on the join + READY", "yes", "where player 2 was is not recorded - no location claim"),
    ("M06", "F09", "3:06.3", "3:08.3", "Tonight's siblings: Player48 HUMAN, Player62 HUMAN, Rory BOT, Tess BOT", "humans + bots in 4 seats", "-", 4, 1, "Frames/LOBBY.jpg", "2", "", "yes", ""),
    ("M07", "F09", "3:18.5", "3:40.5", "Gather toy blocks: press to open the toy chest, sort blocks into colour slots", "toy handling, press, sorting", "press opens the drawer; each block goes into its colour slot", 4, 2, "Frames/CHEST.jpg", "10", "two shots: press/open, then sorting; hands visible", "yes", "Quest boundary arc briefly at 3:25"),
    ("M08", "F09", "3:42.0", "3:54.0", "bot siblings walking and working in the living room", "bots, living house", "-", 4, 2, "ContactSheets/F09_03.jpg", "3", "", "yes", ""),
    ("M09", "F09", "3:56.5", "4:01.0", "finger-gun hop: aim arc, landing ring, drop thumb to hop", "hand-gesture locomotion", "finger-gun aim, thumb drop", 3, 1, "ContactSheets/F09_03.jpg", "3", "", "yes", "label text small"),
    ("M10", "F09", "4:24.0", "4:30.0", "walking into the kitchen: sink, stove, table, plates with food", "connected house", "-", 4, 1, "Frames/DISH.jpg", "3", "", "yes", ""),
    ("M11", "F09", "4:31.5", "4:34.1", "sponge to the soap pump; ghost-hand hint shows where to press; press -> burst VFX", "dishwashing: soap pump + interaction hints", "press the pump with the sponge under it", 4, 2, "Frames/PUMP.jpg", "2.6", "cut BEFORE 4:34.25 (full-frame bright flash)", "yes", "game VFX flashes bright at 4:34.25 and 4:36.5 - excluded for comfort"),
    ("M12", "F09", "4:42.6", "4:44.5", "picks up the egg plate... and the plate crashes", "physical accident", "plate dropped", 3, 5, "Frames/CRASH.jpg", "2", "hook; game ceramic impact on the drop", "yes", "the drop is fast / partly off-frame"),
    ("M13", "F09", "4:45.0", "4:47.0", "MOM IS UP! She heard the plate crash in the kitchen - she's coming to look", "noise -> Mom wakes", "-", 4, 5, "Frames/CRASH.jpg", "2", "", "yes", ""),
    ("M14", "F09", "4:47.0", "4:52.0", "Mom is looking around the kitchen - view down the hall", "Mom investigates", "-", 4, 4, "Frames/CRASH.jpg", "3", "suspense hold", "yes", "Mom herself is never visible in any recording"),
    ("M15", "F09", "4:52.5", "5:06.0", "MOM: Who made the plate crash? - Show: cat / dog; Point: Player62 / Rory / Tess", "playable interrogation incl. a human friend", "finger poke on the board", 4, 5, "Frames/DISH.jpg", "6", "", "yes", ""),
    ("M16", "F09", "5:08.5", "5:12.5", "Player62 (other human, green-haired Genies avatar) in the kitchen; warning - not enough proof", "second human visible", "-", 4, 4, "Frames/DISH.jpg", "3", "", "yes", ""),
    ("M17", "F09", "5:18.0", "5:25.0", "the crashed plates (egg, pizza) on the floor are picked back up", "recovery after the accident", "pick up from the floor", 3, 3, "Frames/DISH.jpg", "3", "", "yes", ""),
    ("M18", "F09", "5:27.3", "5:33.5", "holds the egg plate in one hand, scrubs with the soapy sponge in the other; the stain shrinks", "dishwashing: two-hand scrub, stain removal", "contact scrub reduces the stain step by step", 5, 2, "Frames/SCRUB.jpg", "6", "uninterrupted; cut before 5:33.67 flash", "yes", "bright VFX frame 5:33.67 excluded"),
    ("M19", "F09", "5:34.0", "5:38.5", "clean plate with soap bubbles carried to the sink", "dishwashing result", "bubbles on the clean plate", 4, 1, "Frames/SCRUB.jpg", "4", "", "yes", ""),
    ("M20", "F09", "5:41.5", "5:58.0", "at the sink: red noise rings, Quest boundary lines across the view", "noise feedback", "-", 2, 2, "Frames/DISH.jpg", "0", "REJECT - boundary lines fill the frame", "yes", "Quest guardian"),
    ("M21", "F09", "6:03.0", "6:08.0", "fridge door + Mom is almost awake - quiet!", "noise state", "-", 3, 3, "ContactSheets/F09_04.jpg", "2", "", "yes", ""),
    ("M22", "F09", "6:09.0", "6:16.0", "kitchen with the other human player and bots; MOM IS UP (dog barking)", "multiplayer + pets", "-", 4, 3, "ContactSheets/F09_04.jpg", "3", "", "yes", ""),
    ("M23", "F09", "6:29.5", "6:34.4", "puts a red toy block INTO the fridge", "comedy / player freedom", "open fridge, place block on a shelf", 3, 4, "Frames/REDFRIDGE.jpg", "4", "comedic beat", "yes", "noise ring at 6:26 - start after it"),
    ("M24", "F09", "7:15.0", "7:18.0", "wrist watch: 0:56 left, Mom is suspicious!, 20 PTS, jobs 1/3", "countdown, score, suspicion meter", "turn the wrist to read", 4, 2, "ContactSheets/F09_05.jpg", "2.5", "", "yes", ""),
    ("M25", "F09", "7:20.5", "7:27.0", "Mom: I am getting up. I am really getting up. -> MOM IS UP (cat yowling)", "escalation", "-", 4, 4, "ContactSheets/F09_05.jpg", "4", "", "yes", ""),
    ("M26", "F09", "7:48.0", "8:06.0", "washer button with Quest boundary lines", "laundry appliance", "press", 2, 1, "ContactSheets/F09_05.jpg", "0", "REJECT - boundary lines", "yes", ""),
    ("M27", "F09", "8:18.5", "8:20.5", "wrist: 0:00, MOM IS UP", "match end", "-", 4, 2, "Frames/MPEND.jpg", "2", "", "yes", ""),
    ("M28", "F09", "8:21.0", "8:33.0", "MOM: Who messed with Tess's job? - both human players in the living room", "multiplayer inspection", "-", 4, 4, "Frames/MPEND.jpg", "4", "", "yes", ""),
    ("M29", "F09", "8:39.5", "8:47.0", "RESULTS: RORY WINS (bot); Tess, Player48, Player62", "results, scoring", "-", 4, 3, "Frames/MPEND.jpg", "4", "", "yes", "a bot won - shown as is"),
    ("M30", "F09", "9:03.0", "9:20.0", "Solo setup: 10 MIN, Clever bots; Tonight's siblings You/Nell/Rory/Tess", "solo mode, bot skill", "-", 4, 1, "Frames/SOLOSET.jpg", "2", "", "yes", ""),
    ("M31", "F09", "10:03.5", "10:11.0", "grabs the fridge handle, swings the door open, places food on a shelf", "fridge: hinged door by handle + groceries", "hand on handle, hinge swing", 4, 2, "Frames/FRIDGE.jpg", "6", "uninterrupted", "yes", ""),
    ("M32", "F09", "10:29.0", "10:44.0", "picks the burger up, carries it, opens the fridge, puts it on the shelf", "fridge + food", "pick, carry, place", 4, 3, "Frames/FRIDGE.jpg", "5", "use 10:38-10:44", "yes", ""),
    ("M33", "F09", "10:47.5", "10:53.0", "closes the fridge door by hand", "hinge close", "-", 3, 1, "Frames/FRIDGE.jpg", "3", "", "yes", ""),
    ("M34", "F09", "11:08.0", "11:21.0", "picks up paper trash, walks to the bin, the lid lifts, drops it in", "bin lid by grab", "lid hinge + drop", 3, 2, "Frames/BIN.jpg", "4.5", "use 11:16.5-11:21", "yes", ""),
    ("M35", "F09", "11:54.0", "12:09.0", "carries a plate with food crumbs", "plates", "-", 3, 1, "ContactSheets/F09_07.jpg", "3", "", "yes", ""),
    ("M36", "F09", "12:57.0", "13:03.0", "toy chest open + Mom: Mmm... five more minutes", "toy chest", "-", 3, 2, "ContactSheets/F09_08.jpg", "3", "", "yes", ""),
    ("M37", "F09", "14:24.0", "14:45.0", "solo: MOM IS UP, Who messed with Nell's job? board, warning", "inspection (solo)", "-", 4, 3, "ContactSheets/F09_09.jpg", "3", "", "yes", ""),
    ("M38", "F09", "14:51.0", "15:00.0", "RESULTS: TESS WINS (bot), You 2nd 30 pts", "results (solo)", "-", 4, 2, "ContactSheets/F09_09.jpg", "3", "", "yes", ""),
    ("M39", "F09", "15:18.0", "15:24.0", "How to win + Settings boards (hand-walk/hop, snap turn, seated/standing)", "rules + comfort settings", "-", 3, 1, "ContactSheets/F09_09.jpg", "2", "", "yes", "small text"),
    ("M40", "F04", "3:58.6", "4:10.0", "the house at night: TV, siblings, stairs and balcony under the stars", "best night lighting", "-", 5, 1, "../../00_ProductionAudit/ContactSheets/20261111-170037_03.jpg", "3", "", "yes", "older build c13"),
    ("M41", "F04", "3:12.0", "3:28.0", "mugs onto the tray / coasters on the table", "object placement", "place by hand", 3, 1, "../../00_ProductionAudit/ContactSheets/20261111-170037_02.jpg", "3", "", "yes", "c13"),
    ("M42", "F04", "4:12.0", "4:20.0", "wrist: MOM IS UP, -20 PTS", "penalty on the wrist", "-", 4, 3, "../../00_ProductionAudit/ContactSheets/20261111-170037_03.jpg", "2", "", "yes", "c13"),
    ("M43", "F05", "2:03.0", "2:09.6", "practice: carrying a book, it slips - two cartoon noise bursts; Mom: Mmm... five more minutes", "physical accident + noise VFX", "drop -> burst", 4, 5, "../../07_Trailer/Storyboard/candidates/bang_detail2.jpg", "4", "impact accents at 2:08.05 / 2:08.95", "yes", "c15 build"),
    ("M44", "F06", "4:08.3", "4:15.0", "Fluff the pet beds - Fold the blanket (hold + rub with other hand): one hand holds, the other rubs; pink -> folded beige", "two-hand cloth manipulation", "hold + rub", 4, 1, "Frames/F06_taskcard.jpg; Frames/PLATES06.jpg", "5", "uninterrupted", "yes", "c20/21"),
    ("M45", "F06", "4:21.0", "4:34.0", "stacking napkins on the table; a bot sibling walks off with one", "stacking + bot interference", "-", 3, 3, "Frames/PLATES06.jpg", "3", "", "plausible", "what the bot took is not certain"),
    ("M46", "F06", "7:44.0", "7:57.0", "MOM: Who messed with Tess's job?", "interrogation", "-", 4, 3, "../../07_Trailer/Storyboard/candidates/rows_3.jpg", "3", "", "yes", ""),
    ("M47", "F07", "5:05.0", "5:19.0", "pair the slippers on the marker board", "matching/placement", "place on markers", 4, 1, "../../07_Trailer/Storyboard/candidates/rows_1.jpg", "4", "", "yes", "c21 dev caption at the bottom"),
    ("M48", "F07", "8:03.0", "8:09.5", "fold the towel on the laundry table", "cloth folding", "grab + fold + place", 3, 1, "Frames/TOWEL.jpg", "5", "", "yes", "c21"),
    ("M49", "F07", "4:46.9", "4:51.5", "red noise ring on the slipper board -> Mom is almost awake - quiet! -> MOM IS UP!", "noise ring feedback", "-", 4, 4, "../../07_Trailer/Storyboard/candidates/rows_2.jpg", "3", "", "yes", ""),
    ("M50", "F07", "2:34.0", "2:41.0", "MEOWS warning - the cat makes noise", "pets", "-", 4, 4, "../../07_Trailer/Storyboard/candidates/rows_2.jpg", "2", "", "yes", ""),
    ("M51", "F07", "5:57.0", "6:03.0", "house map on the wrist; WOOF warning", "house map, pets", "-", 3, 3, "../../07_Trailer/Storyboard/candidates/rows_2.jpg", "2", "", "yes", ""),
]
NR = ["dishwashing with running tap water (no running tap recorded)", "spraying cleaning products", "mopping", "sweeping", "cooking",
      "pouring liquids", "cutting food", "wringing cloth", "Mom character on camera (only voice, captions, meter and interrogation board)",
      "washer full cycle (only the button press, with boundary lines)"]
with open("COMPLETE_FOOTAGE_ANALYSIS.csv", "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(H)
    for r in R: w.writerow([r[0], r[1], F[r[1]], *r[2:]])
    for i, n in enumerate(NR, 1): w.writerow([f"NR{i:02d}", "-", "-", "-", "-", n, "NOT RECORDED", "-", "", "", "", "", "not used", "NO - in no recording", ""])
print(len(R), "moments +", len(NR), "not recorded")
