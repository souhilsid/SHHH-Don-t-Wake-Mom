"""Competition gallery stills: for each moment, decode every frame in a short window of the ORIGINAL Quest recording and keep the
sharpest one (highest variance of the Laplacian = least VR head-motion blur). Full 1920x1080, unaltered pixels, PNG + high-quality JPG.
Run: ../../Tools/tts_venv/Scripts/python -I extract_gallery.py"""
import os, subprocess, numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
DL = "C:/Users/aisci/Downloads/"
SRC = {"F04": DL + "com.oculus.vrshell-20261111-170037-0.mp4", "F06": DL + "com.oculus.vrshell-20261115-170859-0.mp4",
       "F09": DL + "SHHH Game/SHHH/Remotion/Raw/Records-20261117T150733Z-1-001/Records/com.oculus.vrshell-20261117-174620-0.mp4"}
# (file name, source, window start, window end, caption for the gallery)
SHOTS = [
    ("01_two_hand_dishwashing", "F09", "5:29.9", "5:31.3", "Hold the plate in one hand and scrub with the soapy sponge in the other - the egg stain comes off."),
    ("02_soap_pump_hint", "F09", "4:32.3", "4:33.9", "Press the soap pump with the sponge; a ghost hand shows where to push."),
    ("03_toy_chest_sorting", "F09", "3:36.6", "3:39.6", "Press the toy chest open and sort the blocks into their colour slots."),
    ("04_fridge_groceries", "F09", "10:41.4", "10:44.0", "Open the fridge by its handle and put the groceries on the shelves."),
    ("05_fold_the_blanket", "F06", "4:09.0", "4:14.5", "Two-handed cloth: hold the blanket with one hand, rub with the other to fold it."),
    ("06_plate_crash_mom_wakes", "F09", "4:45.6", "4:46.9", "A dropped plate - and Mom heard it."),
    ("07_mom_interrogation", "F09", "4:54.0", "5:00.5", "Mom wants a name: point at a sibling, a friend, or blame the pets."),
    ("08_online_room_two_players", "F09", "3:01.0", "3:05.3", "Private online room: two players ready, bot siblings fill the empty seats."),
    ("09_tonights_siblings", "F09", "3:06.5", "3:08.2", "Four seats: two humans and two bot siblings."),
    ("10_siblings_in_the_house", "F09", "3:44.0", "3:52.0", "Bot siblings race you to the jobs all over the house."),
    ("11_wrist_watch_meter", "F09", "7:15.0", "7:18.0", "Your wrist watch: the clock, your points and Mom's sleep meter."),
    ("12_results", "F09", "8:40.5", "8:46.5", "Time's up - the most helpful kid wins."),
    ("13_house_at_night", "F04", "3:59.0", "4:04.0", "The house at night."),
    ("14_wardrobe", "F09", "1:15.6", "1:18.6", "Make your sibling yours in the wardrobe."),
    ("15_main_menu", "F09", "0:33.5", "0:40.0", "Solo, Friends, Practice or Wardrobe."),
]


def ts(s): m, x = s.split(":"); return int(m) * 60 + float(x)


def sharpness(img):
    g = np.asarray(img.convert("L").resize((960, 540)), dtype=np.float32)
    lap = g[1:-1, 1:-1] * 4 - g[:-2, 1:-1] - g[2:, 1:-1] - g[1:-1, :-2] - g[1:-1, 2:]
    return float(lap.var())


tmp = os.path.join(HERE, "_frames"); os.makedirs(tmp, exist_ok=True)
lines = ["# SHHH! Don't Wake Mom! - competition gallery images", "",
         "All images are unedited 1920x1080 frames from real Meta Quest recordings (the sharpest frame of each moment, picked automatically).", "",
         "| File | Source | Time | Suggested caption |", "|---|---|---|---|"]
for name, src, a, b, cap in SHOTS:
    for f in os.listdir(tmp): os.remove(os.path.join(tmp, f))
    subprocess.run([FF, "-nostdin", "-v", "error", "-ss", f"{ts(a):.3f}", "-i", SRC[src], "-t", f"{ts(b) - ts(a):.3f}", "-fps_mode", "passthrough",
                    os.path.join(tmp, "f_%04d.png")], check=True)
    files = sorted(os.listdir(tmp)); scores = []
    for k, f in enumerate(files):
        im = Image.open(os.path.join(tmp, f)); y = np.asarray(im.convert("L"), dtype=np.float32).mean()
        scores.append((sharpness(im) if 25 < y < 225 else -1, k, f))     # skip near-black / near-white (VFX flash) frames
    best = max(scores)
    k = best[1]; t = ts(a) + (ts(b) - ts(a)) * k / max(1, len(files) - 1)
    im = Image.open(os.path.join(tmp, best[2])).convert("RGB")
    im.save(os.path.join(HERE, f"SHHH_{name}.png"), optimize=True)
    im.save(os.path.join(HERE, f"SHHH_{name}.jpg"), quality=95, subsampling=0)
    lines.append(f"| SHHH_{name}.png / .jpg | {src} | ~{int(t // 60)}:{t % 60:05.2f} | {cap} |")
    print(name, src, f"{int(t // 60)}:{t % 60:05.2f}", "sharpness", round(best[0]))
for f in os.listdir(tmp): os.remove(os.path.join(tmp, f))
os.rmdir(tmp)
open(os.path.join(HERE, "GALLERY_CAPTIONS.md"), "w", encoding="utf-8").write("\n".join(lines) + "\n")
