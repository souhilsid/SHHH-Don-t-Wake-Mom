"""Meta VR Start 2026 demo narration - Kokoro-82M (offline, Apache-2.0), voice am_michael (same narrator as the trailer).
Writes Audio/narration/D##.wav (48 kHz mono, trimmed, gently processed) + narration_lengths.json.
Run with the venv: ../../Tools/tts_venv/Scripts/python -I gen_demo_narration.py [ids...]"""
import json, os, subprocess, sys, soundfile as sf
from kokoro_onnx import Kokoro
HERE = os.path.dirname(os.path.abspath(__file__))
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
M = os.path.join(HERE, "..", "..", "Tools", "tts_models")
LINES = {  # id: (text, speed)
    "D01": ("Welcome to Shhh! Don't Wake Mom.", 0.95),
    "D02": ("Four siblings. One sleeping Mom. A whole house of chores.", 0.97),
    "D03": ("Pick your look.", 0.95),
    "D04": ("Then play solo against bot siblings, or open a room and play with a friend online.", 1.0),
    "D05": ("Every job is hands-on.", 0.95),
    "D06": ("Press the toy chest open, and sort the blocks by colour.", 0.98),
    "D07": ("Pump soap onto the sponge,", 0.98),
    "D08": ("hold the plate, and scrub until it's actually clean.", 0.98),
    "D09": ("Open the fridge by its handle. Groceries go on the shelves.", 1.0),
    "D10": ("Lift the bin lid. In goes the trash.", 0.98),
    "D11": ("Hold the blanket with one hand, and rub with the other to fold it.", 1.0),
    "D12": ("Hop from room to room with a finger gun, while your siblings race you to every job.", 1.02),
    "D13": ("Your watch shows the clock, your points, and how close Mom is to waking up.", 1.0),
    "D14": ("The pets don't help.", 0.95),
    "D15": ("Every clink, crash and bang wakes Mom up a little more.", 0.98),
    "D16": ("And when Mom is up, she wants a name.", 0.95),
    "D17": ("Point at a sibling. Or blame the cat.", 0.95),
    "D18": ("Then, clean up the evidence.", 0.92),
    "D19": ("When the clock hits zero, the most helpful kid wins.", 0.98),
    "D20": ("Shhh. Don't wake Mom.", 0.8),
    "D21": ("Sneakily mess up a rival's job,", 0.97),
    "D22": ("but someone may be watching.", 0.95),
    "D23": ("Saw a sibling mess with your job? Show Mom.", 0.97),
}
out = os.path.join(HERE, "narration"); os.makedirs(out, exist_ok=True)
only = sys.argv[1:]
k = Kokoro(os.path.join(M, "kokoro-v1.0.onnx"), os.path.join(M, "voices-v1.0.bin"))
# "Shhh" must be a hush, not the letters S-H-H-H (the phonemizer spells unknown words): these lines are given as phonemes,
# with a long voiceless postalveolar fricative for the hush.
# The hush is the narrator's own "sh" (phoneme input), trimmed and stretched to HUSH seconds, then joined to the rest of the line.
import numpy as np, librosa
HUSH = 0.75
def hush(sp, sr=24000):
    # an unvoiced "shhh": white noise shaped like the /sh/ fricative (energy 2-6 kHz, peak ~3.2 kHz), soft attack, long tail
    from scipy.signal import butter, sosfilt
    rng = np.random.default_rng(7); n = int(HUSH * sr); x = rng.standard_normal(n).astype(np.float32)
    x = sosfilt(butter(4, [1900, 6200], btype="bandpass", fs=sr, output="sos"), x)
    x = x + 0.8 * sosfilt(butter(2, [2700, 3800], btype="bandpass", fs=sr, output="sos"), x)
    t = np.arange(n) / sr; env = np.minimum(1, t / 0.14) * np.minimum(1, (HUSH - t) / 0.32) * (1 + 0.08 * np.sin(2 * np.pi * 3 * t))
    x = x * env; return (0.35 * x / np.max(np.abs(x))).astype(np.float32), sr
def say(t, sp): return k.create(t, voice="am_michael", speed=sp, lang="en-us")
PARTS = {"D01": [("say", "Welcome to"), ("gap", 0.12), ("hush", None), ("gap", 0.22), ("say", "Don't Wake Mom!")],
         "D20": [("hush", None), ("gap", 0.35), ("say", "Don't wake Mom.")]}
for i, (t, sp) in LINES.items():
    if only and i not in only: continue
    if i in PARTS:
        chunks, sr = [], 24000
        for kind, v in PARTS[i]:
            if kind == "gap": chunks.append(np.zeros(int(v * sr), np.float32))
            elif kind == "hush": y, sr = hush(sp); chunks.append(y * 0.8)
            else: y, sr = say(v, sp); chunks.append(librosa.effects.trim(y, top_db=40)[0])
        s = np.concatenate(chunks)
    else: s, sr = say(t, sp)
    raw = os.path.join(out, f"{i}_raw.wav"); sf.write(raw, s, sr)
    soft = i == "D20"
    af = ("highpass=f=70,equalizer=f=180:t=q:w=1:g=2,equalizer=f=3500:t=q:w=1.5:g=1.5,acompressor=threshold=-20dB:ratio=2.5:attack=8:release=120,"
          + ("volume=-3dB,lowpass=f=7000," if soft else "")
          + "silenceremove=start_periods=1:start_threshold=-55dB,areverse,silenceremove=start_periods=1:start_threshold=-55dB,areverse,"
          + "afade=t=in:d=0.01,loudnorm=I=-18:TP=-2:LRA=7,aresample=48000")
    subprocess.run([FF, "-v", "error", "-y", "-i", raw, "-af", af, "-ac", "1", os.path.join(out, f"{i}.wav")], check=True); os.remove(raw)
    print(i, round(sf.info(os.path.join(out, f"{i}.wav")).duration, 3), t)
lens = {i: {"text": t, "speed": sp, "sec": round(sf.info(os.path.join(out, f"{i}.wav")).duration, 3)} for i, (t, sp) in LINES.items() if os.path.exists(os.path.join(out, f"{i}.wav"))}
json.dump({"voice": "am_michael", "model": "kokoro-v1.0.onnx", "lines": lens}, open(os.path.join(out, "narration_lengths.json"), "w"), indent=1)
