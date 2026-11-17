"""Trailer narration with Kokoro-82M (offline). Writes 05_Voiceover/narration/N##.wav (48 kHz mono, trimmed, gently processed)
and narration_lengths.json. Usage: python gen_narration.py"""
import json, os, subprocess, soundfile as sf, numpy as np
from kokoro_onnx import Kokoro
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
VOICE = "am_michael"
LINES = {  # id: (text, speed)
 "N01": ("It's late.", 0.85),
 "N02": ("The house is a total mess...", 0.88),
 "N03": ("and Mom is finally asleep.", 0.86),
 "N04": ("So tonight, you and your siblings have one job.", 0.92),
 "N05a": ("Pair up the slippers.", 0.95), "N05b": ("Stack the mugs.", 0.98), "N05c": ("Fold the towels.", 0.98), "N05d": ("Scrub the plates.", 0.98),
 "NR": ("Jobs in every room... and one clock for everybody.", 0.93),
 "N06": ("without making a single sound.", 0.86),
 "N07": ("Mostly.", 0.85),
 "N08a": ("The cat is bored.", 0.98), "N08b": ("The dog is hungry.", 0.98), "N08c": ("Your siblings are... not helping.", 0.94),
 "N09": ("And every little noise wakes Mom up a little more.", 0.95),
 "N10": ("And when she finds a mess... somebody's getting blamed.", 0.92),
 "N11": ("So be quick. Be helpful. And whatever you do...", 0.9),
 "N12": ("Shhh. Don't wake Mom.", 0.8),
}
out = "05_Voiceover/narration"; os.makedirs(out, exist_ok=True)
k = Kokoro("Tools/tts_models/kokoro-v1.0.onnx", "Tools/tts_models/voices-v1.0.bin")
lens = {}
ONLY = [a for a in __import__("sys").argv[1:]]
for i, (t, sp) in LINES.items():
    if ONLY and i not in ONLY: continue
    s, sr = k.create(t, voice=VOICE, speed=sp, lang="en-us")
    raw = f"{out}/{i}_raw.wav"; sf.write(raw, s, sr)
    soft = i == "N12"
    af = ("highpass=f=70,equalizer=f=180:t=q:w=1:g=2,equalizer=f=3500:t=q:w=1.5:g=1.5,acompressor=threshold=-20dB:ratio=2.5:attack=8:release=120,"
          + ("volume=-3dB,lowpass=f=7000," if soft else "")
          + "silenceremove=start_periods=1:start_threshold=-55dB,areverse,silenceremove=start_periods=1:start_threshold=-55dB,areverse,"
          + "afade=t=in:d=0.01,loudnorm=I=-18:TP=-2:LRA=7,aresample=48000")
    subprocess.run([FF, "-v", "error", "-y", "-i", raw, "-af", af, "-ac", "1", f"{out}/{i}.wav"], check=True); os.remove(raw)
    d = sf.info(f"{out}/{i}.wav").duration; lens[i] = round(d, 3); print(i, lens[i], t)
if not ONLY: json.dump({"voice": VOICE, "model": "kokoro-v1.0.onnx sha256 7d5df8ec...", "lines": {i: {"text": LINES[i][0], "speed": LINES[i][1], "sec": lens[i]} for i in LINES}},
          open(f"{out}/narration_lengths.json", "w"), indent=1)
