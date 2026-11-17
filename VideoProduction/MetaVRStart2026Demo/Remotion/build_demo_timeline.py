"""SHHH! - Meta VR Start 2026 official demo: single source of truth for the edit.

Writes demo_timeline.json (30 fps; the Quest sources are ~30 fps, so 60 fps would only duplicate frames) used by
  * Remotion composition `SHHH-MetaVRStart2026Demo` (SHHH/Remotion/src/remotion/demo/)  - picture, transitions, accents, logo
  * ../Audio/mix_demo_audio.py                                                             - narration, game VO, game SFX, music
and cuts one H.264 proxy per shot from the ORIGINAL Quest recordings (read-only) into SHHH/Remotion/public/demo/.

Every shot is real, unaltered Quest gameplay (IDs = Analysis/COMPLETE_FOOTAGE_ANALYSIS.csv). Picture treatment: gentle push-ins
(zoom <= 1.10, top-anchored so hands and HUD stay in frame), one 0.5x slow-motion beat, cuts placed to avoid the game's
single-frame full-screen VFX flashes (F09 4:34.25, 4:36.5, 5:33.67).
Usage: python build_demo_timeline.py [--proxies] [--only ID,ID]
"""
import json, os, subprocess, sys

FPS = 30
HERE = os.path.dirname(os.path.abspath(__file__))
DEMO = os.path.abspath(os.path.join(HERE, ".."))
SHHH = os.path.abspath(os.path.join(DEMO, "..", ".."))
PUBLIC = os.path.join(SHHH, "Remotion", "public", "demo")
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
DL = "C:/Users/aisci/Downloads/"
SRC = {"F04": DL + "com.oculus.vrshell-20261111-170037-0.mp4", "F05": DL + "com.oculus.vrshell-20261112-170302-0.mp4",
       "F06": DL + "com.oculus.vrshell-20261115-170859-0.mp4", "F07": DL + "com.oculus.vrshell-20261117-122141-0.mp4",
       "F09": SHHH + "/Remotion/Raw/Records-20261117T150733Z-1-001/Records/com.oculus.vrshell-20261117-174620-0.mp4"}
GA = os.path.join(SHHH, "UnityProject", "Assets", "SHHH", "Resources", "Audio")
NARR = os.path.join(DEMO, "Audio", "narration")
EDSFX = os.path.join(DEMO, "Audio", "editorial_sfx")
MUSIC = os.path.join(SHHH, "VideoProduction", "04_MusicAndLicenses")


def t(s): m, x = s.split(":"); return int(m) * 60 + float(x)


ZOOM_ORIGIN = {"S1": "62% 56%"}   # push-in targets (the sabotaged job card)


# (kind, id, src, in, dur, zoom0, zoom1, transition INTO this item, analysis id, note)
# kind: shot | slow (0.5x) | sting | logo ; transition: cut | swoosh | moon | stars | flash-free dip
PIC = [
    # HOOK - straight into gameplay
    ("shot", "H1", "F09", "4:41.6", 3.0, 1.00, 1.05, "cut", "M12", "kitchen: grab the egg plate... crash"),
    ("shot", "H2", "F09", "4:45.0", 2.2, 1.02, 1.07, "cut", "M13", "MOM IS UP! She heard the plate crash in the kitchen"),
    ("shot", "H3", "F09", "4:47.2", 3.4, 1.00, 1.06, "cut", "M14", "Mom is looking around the kitchen"),
    ("sting", "T1", None, None, 1.6, 1, 1, "moon", None, "title sting: moon + stars + logo pop"),
    # ESTABLISH
    ("shot", "E1", "F09", "0:33.6", 2.8, 1.00, 1.05, "moon", "M02", "Pick a mode"),
    ("shot", "E2", "F09", "1:13.0", 2.6, 1.00, 1.05, "cut", "M03", "Genies wardrobe - tops, live avatar"),
    ("shot", "E3", "F09", "2:58.4", 4.4, 1.00, 1.04, "cut", "M05", "online room PDBE: Player62 joins, both READY"),
    ("shot", "E4", "F09", "3:06.3", 2.0, 1.00, 1.03, "cut", "M06", "Tonight's siblings: 2 humans + 2 bots"),
    ("shot", "E5", "F09", "3:09.2", 2.2, 1.00, 1.05, "cut", "M08", "the match starts in the living room"),
    # PHYSICAL
    ("shot", "P1", "F09", "3:19.0", 4.6, 1.00, 1.06, "swoosh", "M07", "toy chest: press to open"),
    ("shot", "P2", "F09", "3:31.4", 7.4, 1.00, 1.05, "cut", "M07", "blocks sorted into colour slots"),
    ("shot", "P0", "F09", "4:24.2", 2.6, 1.00, 1.04, "cut", "M10", "into the kitchen"),
    ("shot", "P3", "F09", "4:31.4", 2.7, 1.00, 1.05, "cut", "M11", "soap pump with the sponge (ghost-hand hint, press burst)"),
    ("shot", "P4", "F09", "5:27.3", 6.2, 1.00, 1.08, "cut", "M18", "two-hand scrub: stain shrinks"),
    ("shot", "P5", "F09", "5:34.0", 3.6, 1.00, 1.04, "cut", "M19", "clean plate with bubbles to the sink"),
    ("shot", "P6", "F09", "10:03.4", 7.6, 1.00, 1.05, "swoosh", "M31", "fridge: handle, hinge, food on a shelf"),
    ("shot", "P7", "F09", "10:38.4", 5.4, 1.00, 1.05, "cut", "M32", "burger onto the fridge shelf"),
    ("shot", "P8", "F09", "11:16.4", 4.6, 1.00, 1.05, "cut", "M34", "bin lid lifts, trash in"),
    ("shot", "P9", "F06", "4:08.2", 6.8, 1.00, 1.06, "swoosh", "M44", "fold the blanket: hold + rub with the other hand"),
    ("shot", "P10", "F07", "8:03.0", 4.0, 1.00, 1.05, "cut", "M48", "fold the towel"),
    ("shot", "P11", "F07", "5:08.0", 3.0, 1.00, 1.05, "cut", "M47", "pair the slippers on the board"),
    # LIVING WORLD
    ("shot", "W1", "F09", "3:56.6", 3.4, 1.00, 1.04, "stars", "M09", "finger-gun hop"),
    ("shot", "W2", "F09", "3:42.0", 3.6, 1.00, 1.05, "cut", "M08", "bot siblings at work in the living room"),
    ("shot", "W3", "F09", "6:42.0", 2.4, 1.00, 1.05, "cut", "M22", "kitchen: siblings racing for jobs"),
    ("shot", "W4", "F04", "3:58.8", 3.6, 1.00, 1.06, "cut", "M40", "the house at night"),
    ("shot", "W5", "F09", "7:15.0", 4.9, 1.00, 1.05, "cut", "M24", "wrist watch: clock, points, Mom is suspicious"),
    # SABOTAGE (F07 c21: you lift the green bowl off Nell's drying mat; Nell's card: "Someone messed with this!")
    ("shot", "S2", "F07", "3:21.9", 2.8, 1.00, 1.06, "swoosh", "M52", "sabotage: lifting the bowl off Nell's drying mat"),
    ("shot", "S1", "F07", "3:19.6", 2.6, 1.00, 1.40, "cut", "M53", "Nell's job card: Someone messed with this! (push-in)"),
    # CHAOS + MOM
    ("shot", "C0", "F07", "2:35.4", 2.2, 1.00, 1.05, "swoosh", "M50", "MEOWS warning"),
    ("shot", "C0b", "F07", "6:00.2", 2.0, 1.00, 1.05, "cut", "M51", "WOOF warning"),
    ("shot", "C1", "F09", "6:29.5", 4.9, 1.00, 1.05, "cut", "M23", "a toy block goes into the fridge"),
    ("shot", "C2a", "F05", "2:05.8", 2.1, 1.00, 1.04, "cut", "M43", "practice: carrying the book..."),
    ("slow", "C2b", "F05", "2:07.9", 1.6, 1.04, 1.08, "cut", "M43", "...it slips: noise bursts (0.5x)"),
    ("shot", "C2c", "F05", "2:08.7", 1.4, 1.08, 1.10, "cut", "M43", "second burst, real speed"),
    ("shot", "C3", "F07", "4:46.9", 3.2, 1.00, 1.05, "cut", "M49", "red noise ring -> Mom is almost awake"),
    ("shot", "C4", "F09", "7:20.6", 5.0, 1.00, 1.05, "cut", "M25", "I am getting up... MOM IS UP"),
    ("shot", "C5", "F09", "4:52.6", 8.6, 1.00, 1.04, "moon", "M15", "MOM: Who made the plate crash? - point at a sibling or a pet"),
    ("shot", "S3", "F07", "11:27.4", 3.0, 1.00, 1.08, "cut", "M54", "evidence option: Saw Nell mess with You's Refill Mochi's bowl"),
    ("shot", "C6", "F09", "5:08.6", 3.6, 1.00, 1.04, "cut", "M16", "the friend's avatar; not enough proof"),
    ("shot", "C7", "F09", "5:18.4", 3.4, 1.00, 1.05, "cut", "M17", "picking the crashed plates off the floor"),
    # PAYOFF
    ("shot", "R0", "F09", "8:21.0", 3.6, 1.00, 1.04, "swoosh", "M28", "Who messed with Tess's job? - both humans present"),
    ("shot", "R1", "F09", "8:18.6", 1.9, 1.00, 1.05, "cut", "M27", "wrist 0:00 MOM IS UP"),
    ("shot", "R2", "F09", "8:39.6", 8.2, 1.00, 1.06, "cut", "M29", "RESULTS: Rory (bot) wins; both humans ranked"),
    # ENDING montage + logo
    ("shot", "X1", "F09", "5:30.2", 0.9, 1.04, 1.10, "stars", "M18", "montage: scrub"),
    ("shot", "X2", "F09", "10:06.0", 0.9, 1.04, 1.10, "cut", "M31", "montage: fridge"),
    ("shot", "X3", "F09", "3:35.2", 0.9, 1.04, 1.10, "cut", "M07", "montage: toy chest"),
    ("shot", "X4", "F09", "4:45.2", 0.9, 1.04, 1.10, "cut", "M13", "montage: MOM IS UP"),
    ("logo", "L1", None, None, 9.0, 1, 1, "moon", None, "logo reveal"),
]

N = lambda i: os.path.join(NARR, i + ".wav")
MV = lambda i: os.path.join(GA, "VO", i + ".wav")
S = lambda i: os.path.join(GA, i)
E = lambda i: os.path.join(EDSFX, i)
# (anchor, offset s, file, gain dB, caption)
VOICE = [
    ("H2", 0.35, MV("vo_mom_suspicious_2_1"), 0, "MOM: I know that sound. That's a plate."),
    ("H3", 0.85, N("D01"), 0, "Welcome to SHHH! Don't Wake Mom."),
    ("E1", 0.25, N("D02"), 0, "Four siblings. One sleeping Mom. A whole house of chores."),
    ("E2", 1.60, N("D03"), 0, "Pick your look."),
    ("E3", 0.30, N("D04"), 0, "Then play solo against bot siblings, or open a room and play with a friend online."),
    ("P1", 0.30, N("D05"), 0, "Every job is hands-on."),
    ("P1", 2.20, N("D06"), 0, "Press the toy chest open, and sort the blocks by colour."),
    ("P3", 0.15, N("D07"), 0, "Pump soap onto the sponge,"),
    ("P4", 0.45, N("D08"), 0, "hold the plate, and scrub until it's actually clean."),
    ("P6", 0.50, N("D09"), 0, "Open the fridge by its handle. Groceries go on the shelves."),
    ("P8", 0.30, N("D10"), 0, "Lift the bin lid. In goes the trash."),
    ("P9", 0.50, N("D11"), 0, "Hold the blanket with one hand, and rub with the other to fold it."),
    ("W1", 0.30, N("D12"), 0, "Hop from room to room with a finger gun, while your siblings race you to every job."),
    ("W5", 0.15, N("D13"), 0, "Your watch shows the clock, your points, and how close Mom is to waking up."),
    ("S2", 0.25, N("D21"), 0, "Sneakily mess up a rival's job,"),
    ("S1", 0.10, N("D22"), 0, "but someone may be watching."),
    ("S3", 0.20, N("D23"), 0, "Saw a sibling mess with your job? Show Mom."),
    ("C0b", 0.85, N("D14"), 0, "The pets don't help."),
    ("C3", 0.10, N("D15"), 0, "Every clink, crash and bang wakes Mom up a little more."),
    ("C4", 0.40, MV("vo_mom_almost_3_1"), 0, "MOM: I am getting up. I am really getting up."),
    ("C5", 0.20, N("D16"), 0, "And when Mom is up, she wants a name."),
    ("C5", 2.70, MV("vo_mom_question_1_1"), 0, "MOM: Alright... who did this? Show me."),
    ("C5", 5.70, N("D17"), 0, "Point at a sibling. Or blame the cat."),
    ("C7", 0.50, N("D18"), 0, "Then, clean up the evidence."),
    ("R1", 0.10, N("D19"), 0, "When the clock hits zero, the most helpful kid wins."),
    ("R2", 1.60, MV("vo_mom_win_1"), 0, "MOM: ...the quietest, most helpful kid in this house. I'm SO proud. Now everybody... SLEEP!"),
    ("L1", 1.00, N("D20"), 0, "Shhh. Don't wake Mom."),
]
# game SFX on the frames where the action happens (frame analysis in Analysis/Frames/*), editorial whooshes on transitions
SFX = [
    ("H1", 2.40, S("impact_ceramic_3.wav"), -3), ("H1", 2.55, S("impact_ceramic_5.wav"), -6),
    ("H2", 0.00, S("mom_wake_stinger_1.ogg"), -10),
    ("T1", 0.00, E("whoosh_long.wav"), -8), ("T1", 0.45, S("card_show_1.wav"), -6),
    ("E1", 0.00, E("whoosh_short.wav"), -10), ("E2", 0.8, S("ui_click_1.wav"), -10), ("E3", 0.6, S("join_pop_1.wav"), -6), ("E3", 2.6, S("ui_click_1.wav"), -9),
    ("P1", 0.00, E("whoosh_short.wav"), -10), ("P1", 1.15, S("slot_click_1.wav"), -6), ("P1", 3.6, S("lid_open_2.wav"), -8),
    ("P2", 4.20, S("slot_click_2.wav"), -6), ("P2", 6.20, S("slot_click_3.wav"), -6),
    ("P3", 2.55, S("squeak_1.wav"), -8),
    ("P4", 1.70, S("sponge_1.wav"), -6), ("P4", 3.10, S("sponge_2.wav"), -6), ("P4", 4.60, S("sponge_3.wav"), -6),
    ("P5", 2.70, S("rinse_1.wav"), -10),
    ("P6", 0.00, E("whoosh_short.wav"), -10), ("P6", 1.20, S("door_creak_1.wav"), -9), ("P6", 6.20, S("grab_1.wav"), -10),
    ("P7", 4.60, S("grab_1.wav"), -10),
    ("P8", 1.90, S("lid_open_1.wav"), -7), ("P8", 3.60, S("rustle_1.wav"), -7), ("P8", 4.30, S("lid_close_1.wav"), -9),
    ("P9", 0.00, E("whoosh_short.wav"), -10), ("P9", 2.0, S("rustle_2.wav"), -8), ("P9", 4.0, S("rustle_3.wav"), -8),
    ("P10", 2.6, S("impact_fabric_2.wav"), -8), ("P11", 2.2, S("impact_soft_2.wav"), -9),
    ("W1", 0.00, E("sparkle.wav"), -12), ("W1", 2.4, S("point_at_1.wav"), -9),
    ("W5", 0.3, S("ui_hover_1.wav"), -10),
    ("S2", 0.00, E("whoosh_short.wav"), -10), ("S2", 1.2, S("grab_1.wav"), -9), ("S1", 1.6, S("recipe_undone_1.wav"), -8), ("S1", 1.75, S("bot_giggle_3.wav"), -8),
    ("S3", 0.6, S("ui_click_1.wav"), -9),
    ("C0", 0.00, E("whoosh_short.wav"), -10), ("C0", 0.10, S("cat_meow_1.wav"), -8), ("C0b", 0.05, S("dog_bark_1.wav"), -8), ("C0b", 0.35, S("dog_bark_2.wav"), -11),
    ("C1", 3.95, S("bot_giggle_2.wav"), -6),
    ("C2b", 0.30, S("impact_wood_4.wav"), -4), ("C2c", 0.25, S("impact_wood_2.wav"), -6),
    ("C4", 2.80, S("mom_wake_stinger_1.ogg"), -9),
    ("C5", 0.00, E("whoosh_long.wav"), -9), ("C5", 7.4, S("ui_click_1.wav"), -8),
    ("C6", 1.2, S("bot_giggle_1.wav"), -8),
    ("R0", 0.00, E("whoosh_short.wav"), -10), ("R2", 0.25, S("win_other_1.ogg"), -6),
    ("X1", 0.00, E("sparkle.wav"), -10), ("X2", 0.0, E("whoosh_short.wav"), -13), ("X3", 0.0, E("whoosh_short.wav"), -13), ("X4", 0.0, E("whoosh_short.wav"), -13),
    ("L1", 0.00, E("whoosh_long.wav"), -8), ("L1", 0.55, S("chore_done_1.wav"), -7), ("L1", 4.40, S("mom_snore_1.wav"), -6),
]
# music: (file, srcIn, start anchor, offset, end anchor, end offset, fadeIn, fadeOut, gain)
MUS = [
    ("Investigations.mp3", 0.0, "H1", 0.0, "P1", 0.0, 0.02, 0.4, -6),
    ("Marty_Gots_a_Plan.mp3", 38.0, "P1", 0.0, "C0", 0.0, 0.05, 0.5, -6),
    ("Scheming_Weasel_faster.mp3", 0.0, "C0", 0.0, "R0", 0.0, 0.05, 0.6, -7),
    ("Fluffing_a_Duck.mp3", 30.0, "R0", 0.0, "END", 0.0, 0.3, 1.5, -6),
]


def build():
    items, f = [], 0
    for kind, iid, src, tin, dur, z0, z1, tr, aid, note in PIC:
        n = round(dur * FPS)
        it = {"id": iid, "kind": kind, "from": f, "frames": n, "transitionIn": tr, "analysisId": aid, "note": note}
        if kind in ("shot", "slow"):
            rate = 0.5 if kind == "slow" else 1.0
            it.update({"src": src, "srcFile": SRC[src], "in": t(tin), "srcDur": round(dur * rate, 3), "playbackRate": rate,
                       "proxy": f"demo/{iid}.mp4", "zoom": [z0, z1], "zoomOrigin": ZOOM_ORIGIN.get(iid, "50% 30%")})
        items.append(it); f += n
    total = f
    at = {it["id"]: it["from"] / FPS for it in items}; at["END"] = total / FPS
    voice = [{"at": round(at[a] + o, 3), "file": fl, "gainDb": g, "caption": c} for a, o, fl, g, c in VOICE]
    sfx = [{"at": round(at[a] + o, 3), "file": fl, "gainDb": g} for a, o, fl, g in SFX]
    mus = [{"file": os.path.join(MUSIC, fl), "srcIn": si, "at": round(at[a] + o, 3), "dur": round(at[b] + bo - at[a] - o, 3), "fadeIn": fi, "fadeOut": fo, "gainDb": g}
           for fl, si, a, o, b, bo, fi, fo, g in MUS]
    import soundfile as sf
    ends = sorted((v["at"], v["at"] + sf.info(v["file"]).duration, v["caption"][:30]) for v in voice)
    for (a0, a1, c0), (b0, b1, c1) in zip(ends, ends[1:]):
        if b0 < a1 + 0.12: print(f"VOICE OVERLAP {a0:.2f}-{a1:.2f} '{c0}' / {b0:.2f} '{c1}'")
    tl = {"composition": "SHHH-MetaVRStart2026Demo", "fps": FPS, "width": 1920, "height": 1080, "durationInFrames": total,
          "seconds": round(total / FPS, 2), "brand": {"navy": "#19183F", "night": "#10132F", "yellow": "#FFD65A", "coral": "#FF7769",
          "teal": "#65C8C5", "purple": "#7958BB", "cream": "#FFF3DD", "ink": "#2A214B"},
          "color": {"note": "no grade: Quest captures are shown as recorded; vignette 0.30 only"},
          "items": items, "voice": voice, "sfx": sfx, "music": mus}
    for p in [os.path.join(HERE, "demo_timeline.json"), os.path.join(SHHH, "Remotion", "src", "remotion", "demo", "demo_timeline.json")]:
        os.makedirs(os.path.dirname(p), exist_ok=True); json.dump(tl, open(p, "w"), indent=1)
    print(f"timeline: {total} frames = {total / FPS:.2f} s ({int(total / FPS // 60)}:{total / FPS % 60:05.2f}), "
          f"{sum(1 for i in items if i['kind'] in ('shot', 'slow'))} shots")
    return tl


def proxies(tl, only=None):
    os.makedirs(PUBLIC, exist_ok=True)
    for it in tl["items"]:
        if it["kind"] not in ("shot", "slow") or (only and it["id"] not in only): continue
        out = os.path.join(PUBLIC, it["id"] + ".mp4"); dur = it["srcDur"] + 0.4
        subprocess.run([FF, "-v", "error", "-y", "-ss", f"{it['in']:.3f}", "-i", it["srcFile"], "-t", f"{dur:.3f}", "-an",
                        "-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-preset", "medium", "-crf", "12", "-g", "15", out], check=True)
        print("proxy", it["id"], it["src"], it["in"], f"{dur:.2f}s")


if __name__ == "__main__":
    tl = build()
    if "--proxies" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
        proxies(tl, only)
