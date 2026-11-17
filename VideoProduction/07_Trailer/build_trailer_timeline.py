"""SHHH! trailer - single source of truth for the edit.

Builds `trailer_timeline.json` (frames @30 fps) used by:
  * the Remotion composition `ShhhTrailer` (video, phrase cards, logo)      -> SHHH/Remotion/src/remotion/shhh/
  * `mix_trailer_audio.py` (music, narration, Mom/sibling game VO, SFX)      -> 06_SoundDesign/stems
  * the caption sidecar (.srt)
and extracts one H.264 proxy per shot from the ORIGINAL Quest recordings (read-only) into SHHH/Remotion/public/trailer/.

Every shot is real, unaltered Quest gameplay footage (F04-F07 in 00_ProductionAudit/FOOTAGE_MANIFEST.csv). The only image
change is a gentle crop/push-in (zoom 1.0-1.3, anchored to the top) that hides most of the development HUD strip at the bottom.

Usage: python build_trailer_timeline.py [--proxies]
"""
import json, os, subprocess, sys

FPS = 30
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))               # .../SHHH
REMOTION_PUBLIC = os.path.join(ROOT, "Remotion", "public", "trailer")
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
DL = "C:/Users/aisci/Downloads/"
SRC = {"F04": DL + "com.oculus.vrshell-20261111-170037-0.mp4", "F05": DL + "com.oculus.vrshell-20261112-170302-0.mp4",
       "F06": DL + "com.oculus.vrshell-20261115-170859-0.mp4", "F07": DL + "com.oculus.vrshell-20261117-122141-0.mp4"}
VO = os.path.join(ROOT, "UnityProject", "Assets", "SHHH", "Resources", "Audio")
NARR = os.path.join(ROOT, "VideoProduction", "05_Voiceover", "narration")
MUSIC = os.path.join(ROOT, "VideoProduction", "04_MusicAndLicenses")


def t(s):  # "m:ss.s" -> seconds
    m, x = s.split(":"); return int(m) * 60 + float(x)


# ---- picture track: (kind, id, src, in, dur, zoom0, zoom1, note) ; kind = shot | card | logo | black ------------------------
# zoom: 1.0 = full frame; >1 crops from the bottom (anchor top) so the dev HUD strip leaves the frame. Slow push-ins only.
Z = 1.24
PIC = [
    ("black", "B0", None, None, 1.4, 1, 1, "fade up from black"),
    # ACT 1 - it's late
    ("shot", "A1", "F04", "3:58.6", 3.4, 1.20, 1.27, "night living room, TV, sibling (Nov 11 build, night lighting)"),
    ("shot", "A2", "F04", "4:05.0", 3.3, 1.22, 1.28, "stairs + balcony under the night sky"),
    ("shot", "A3", "F07", "2:23.5", 3.0, Z, 1.30, "warm living room, lamp, bookshelf"),
    ("shot", "A4", "F07", "3:42.4", 3.0, Z, 1.30, "the dog asleep in its bed"),
    ("shot", "A5", "F07", "0:36.0", 3.0, 1.12, 1.18, "Genies wardrobe - your look"),
    ("shot", "A6", "F07", "9:11.6", 3.2, Z, 1.28, "bot siblings walking down the hall, the cat"),
    ("card", "K1", None, None, 1.5, 1, 1, "EVERY CHORE."),
    # chores
    ("shot", "C1", "F07", "5:08.0", 2.3, Z, 1.28, "pair the slippers"),
    ("shot", "C2", "F04", "3:15.0", 2.2, Z, 1.28, "mugs onto the tray, coasters"),
    ("shot", "C3", "F07", "8:04.0", 2.2, Z, 1.28, "fold the towels"),
    ("shot", "C4", "F06", "4:08.0", 2.3, Z, 1.28, "scrub the plates"),
    ("shot", "C5", "F07", "1:39.0", 2.0, Z, 1.28, "kitchen table jobs"),
    ("shot", "C6", "F07", "10:45.3", 2.0, Z, 1.28, "the washing machine"),
    ("shot", "R1", "F07", "5:57.0", 2.3, 1.10, 1.16, "house map on the wrist"),
    ("shot", "R2", "F07", "9:24.0", 2.2, Z, 1.28, "kitchen, siblings at work"),
    ("shot", "R3", "F07", "8:12.0", 2.2, Z, 1.28, "laundry room"),
    ("shot", "R4", "F07", "6:38.6", 2.4, Z, 1.28, "finger-gun hop across the living room"),
    ("card", "K2", None, None, 1.5, 1, 1, "NOT ONE SOUND."),
    ("shot", "C7", "F05", "2:03.0", 6.6, Z, 1.30, "practice: carry the book... it drops (two real noise bursts at +5.05 s and +5.95 s)"),
    # ACT 2 - chaos
    ("shot", "D1", "F07", "2:35.6", 2.3, Z, 1.28, "MEOWS warning by the armchair"),
    ("shot", "D2", "F07", "6:00.2", 2.0, Z, 1.28, "WOOF warning, dog bowl"),
    ("shot", "D3", "F07", "2:51.4", 2.3, Z, 1.28, "a sibling with a book: 'is somebody... rattling?'"),
    ("shot", "D4", "F07", "4:46.9", 1.6, Z, 1.28, "red noise ring on the slipper board"),
    ("shot", "D5", "F07", "10:49.0", 1.8, Z, 1.28, "red noise ring at the washer"),
    ("shot", "D6", "F07", "5:47.5", 2.0, 1.10, 1.16, "wrist: Mom is suspicious"),
    ("shot", "D7", "F04", "4:13.0", 2.3, 1.10, 1.16, "wrist: MOM IS UP -20 PTS"),
    ("shot", "D8", "F07", "4:51.2", 2.6, 1.08, 1.12, "'MOM IS UP! She heard the dog barking...'"),
    # ACT 3 - sabotage + blame
    ("shot", "S1", "F07", "7:44.5", 2.2, Z, 1.28, "a sibling at the kitchen counter"),
    ("shot", "S2", "F06", "5:31.0", 2.3, Z, 1.28, "a sibling by the dog bed"),
    ("card", "K3", None, None, 1.5, 1, 1, "DON'T GET BLAMED."),
    ("shot", "E1", "F07", "11:18.4", 3.0, Z, 1.28, "final inspection: Mom is checking everyone's jobs"),
    ("shot", "E2", "F07", "11:26.2", 3.2, 1.06, 1.12, "MOM: Who messed with Nell's job?"),
    ("shot", "E3", "F06", "7:48.5", 2.2, 1.06, 1.12, "MOM: Who messed with Tess's job?"),
    ("shot", "E4", "F07", "11:42.6", 4.6, 1.04, 1.12, "results: TESS WINS (bot) - you 2nd"),
    ("shot", "E5", "F07", "2:18.6", 3.0, Z, 1.30, "living room wide"),
    ("shot", "E6", "F04", "4:00.4", 3.2, 1.20, 1.27, "night living room"),
    ("logo", "L1", None, None, 9.0, 1, 1, "logo reveal + tagline"),
]

CARD_TEXT = {"K1": "EVERY CHORE.", "K2": "NOT ONE SOUND.", "K3": "DON'T GET BLAMED."}
TAGLINE = "A VR comedy about very quiet chores"

# ---- voices / SFX: (anchor item id, offset s, file, gain dB, caption or None) ------------------------------------------------
N = lambda i: os.path.join(NARR, i + ".wav")
M = lambda i: os.path.join(VO, "VO", i + ".wav")
S = lambda i: os.path.join(VO, i)
VOICE = [
    ("B0", 0.55, N("N01"), 0, "It's late."),
    ("A1", 0.45, N("N02"), 0, "The house is a total mess..."),
    ("A2", 0.55, N("N03"), 0, "...and Mom is finally asleep."),
    ("A4", 0.35, M("vo_mom_stir_1_1"), -1, "MOM: Mmm... five more minutes."),
    ("A5", 0.30, N("N04"), 0, "So tonight, you and your siblings have one job."),
    ("C1", 0.25, N("N05a"), 0, "Pair up the slippers."),
    ("C2", 0.20, N("N05b"), 0, "Stack the mugs."),
    ("C3", 0.20, N("N05c"), 0, "Fold the towels."),
    ("C4", 0.25, N("N05d"), 0, "Scrub the plates."),
    ("R1", 0.35, N("NR"), 0, "Jobs in every room... and one clock for everybody."),
    ("C7", 0.70, N("N06"), 0, "...without making a single sound."),
    ("C7", 6.35, N("N07"), 0, "...Mostly."),
    ("D1", 0.80, N("N08a"), 0, "The cat is bored."),
    ("D2", 0.20, N("N08b"), 0, "The dog is hungry."),
    ("D3", 0.10, N("N08c"), 0, "Your siblings are... not helping."),
    ("D4", 0.15, N("N09"), 0, "And every little noise wakes Mom up a little more."),
    ("D7", 0.15, M("vo_mom_almost_3_1"), 0, "MOM: I am getting up. I am really getting up."),
    ("D8", 0.55, M("vo_mom_wake_1_1"), 1, "MOM: WHO is making that noise?!"),
    ("E1", 0.30, N("N10"), 0, "And when she finds a mess... somebody's getting blamed."),
    ("E2", 0.30, M("vo_mom_question_1_1"), 0, "MOM: Alright... who did this? Show me."),
    ("E3", 0.25, M("vo_mom_caught_4_1"), 0, "MOM: Aha! It was you."),
    ("E4", 0.15, M("vo_mom_win_1"), 0, "MOM: ...the quietest, most helpful kid in this house. I'm SO proud. Now everybody... SLEEP!"),
    ("E5", 0.40, M("vo_mom_bed_1_1"), 0, "MOM: Goodnight. And keep it DOWN."),
    ("E6", 0.15, N("N11"), 0, "So be quick. Be helpful. And whatever you do..."),
    ("L1", 0.90, N("N12"), 0, "Shhh. Don't wake Mom."),
]
# sabotage beat: reuse the game's own Mom sabotage line (real game VO) instead of new narration
VOICE.insert(17, ("S1", 0.20, M("vo_mom_sabotage_1_1"), 0, "MOM: Why are things moving around out there?"))
SFX = [
    ("C7", 5.05, S("impact_wood_4.wav"), -4, None),     # real noise burst #1 on screen (book hits the table)
    ("C7", 5.95, S("impact_wood_2.wav"), -6, None),     # burst #2
    ("D1", 0.05, S("cat_meow_1.wav"), -8, None),        # the on-screen MEOWS warning
    ("D2", 0.05, S("dog_bark_1.wav"), -8, None),        # the on-screen WOOF warning
    ("D2", 0.40, S("dog_bark_2.wav"), -10, None),
    ("D3", 2.05, S("bot_giggle_2.wav"), -6, None),      # sibling reaction (game asset)
    ("D8", 0.00, S("mom_wake_stinger_1.ogg"), -9, None),
    ("S2", 1.30, S("bot_giggle_1.wav"), -6, None),
    ("E3", 1.85, S("bot_giggle_3.wav"), -7, None),
    ("L1", 4.30, S("mom_snore_1.wav"), -6, None),
]
# ---- music: (file, source in s, anchor id, offset, dur s, fade in, fade out, gain dB) --------------------------------------------
MUS = [
    (os.path.join(MUSIC, "Sneaky_Adventure.mp3"), 0.0, "B0", 0.35, None, 0.8, 0.6, -4),  # until the book drop (dur filled in below)
    (os.path.join(MUSIC, "Scheming_Weasel_faster.mp3"), 0.0, "D1", 0.0, None, 0.05, 0.25, -5),  # chaos, ends on MOM IS UP
    (os.path.join(MUSIC, "Investigations.mp3"), 2.0, "S1", 0.0, None, 0.6, 1.2, -6),  # sabotage + blame + results
    (os.path.join(MUSIC, "Sneaky_Adventure.mp3"), 58.0, "E5", 0.0, None, 1.0, 2.5, -5),  # theme returns for the ending (to its own end)
]


def build():
    items, f = [], 0
    for kind, iid, src, tin, dur, z0, z1, note in PIC:
        n = round(dur * FPS)
        it = {"id": iid, "kind": kind, "from": f, "frames": n, "note": note}
        if kind == "shot":
            it.update({"src": src, "srcFile": SRC[src], "in": t(tin), "proxy": f"trailer/{iid}.mp4", "zoom": [z0, z1]})
        if kind == "card": it["text"] = CARD_TEXT[iid]
        if kind == "logo": it.update({"logo": "trailer/shhh_logo.png", "tagline": TAGLINE})
        items.append(it); f += n
    total = f
    at = {it["id"]: it["from"] / FPS for it in items}
    ev = lambda lst: [{"at": round(at[a] + o, 3), "file": fl, "gainDb": g, "caption": c} for a, o, fl, g, c in lst]
    voice, sfx = ev(VOICE), ev([(a, o, fl, g, None) for a, o, fl, g, _ in SFX])
    mus = []
    for i, (fl, sin, a, o, d, fi, fo, g) in enumerate(MUS):
        start = at[a] + o
        end = at[MUS[i + 1][2]] + MUS[i + 1][3] if i + 1 < len(MUS) else total / FPS
        if i == 0: end = at["C7"] + 4.6          # music drops out just before the book falls
        if i == 1: end = at["D8"] + 0.45         # chaos stops dead when Mom is up
        if i == 2: end = at["E5"] + 0.8
        mus.append({"file": fl, "srcIn": sin, "at": round(start, 3), "dur": round(end - start, 3), "fadeIn": fi, "fadeOut": fo, "gainDb": g})
    tl = {"fps": FPS, "width": 1920, "height": 1080, "durationInFrames": total, "seconds": round(total / FPS, 2),
          "items": items, "voice": voice, "sfx": sfx, "music": mus}
    for p in [os.path.join(HERE, "trailer_timeline.json"), os.path.join(ROOT, "Remotion", "src", "remotion", "shhh", "trailer_timeline.json")]:
        os.makedirs(os.path.dirname(p), exist_ok=True); json.dump(tl, open(p, "w"), indent=1)
    print(f"timeline: {total} frames = {total / FPS:.2f} s, {sum(1 for i in items if i['kind'] == 'shot')} shots")
    return tl


def proxies(tl):
    os.makedirs(REMOTION_PUBLIC, exist_ok=True)
    for it in tl["items"]:
        if it["kind"] != "shot": continue
        out = os.path.join(REMOTION_PUBLIC, it["id"] + ".mp4")
        dur = it["frames"] / FPS + 0.5
        # frame-accurate input seek, constant 30 fps, H.264 near-lossless intermediate, audio kept (reference only; the mix uses stems)
        subprocess.run([FF, "-v", "error", "-y", "-ss", f"{it['in']:.3f}", "-i", it["srcFile"], "-t", f"{dur:.3f}",
                        "-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-preset", "medium", "-crf", "12", "-g", "15",
                        "-c:a", "aac", "-b:a", "192k", "-ar", "48000", out], check=True)
        print("proxy", it["id"], it["src"], it["in"], f"{dur:.2f}s")


if __name__ == "__main__":
    tl = build()
    if "--proxies" in sys.argv: proxies(tl)
