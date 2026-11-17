#!/usr/bin/env python3
"""SHHH! V8 Mom offline voice bank. Offline TTS: Piper (piper-tts 1.8.0, GPL-3.0 engine - generated audio is not covered by the engine
licence) with the en_US-ljspeech-high voice (trained from scratch on the public-domain LJ Speech dataset). Lines: mom_lines_v8.json
(original). Post: sox (warm EQ, light compression, normalise). Usage: make_vo.py <voice.onnx> <out_dir>"""
import json, os, re, subprocess, sys, wave
from piper import PiperVoice
from piper.config import SynthesisConfig
here = os.path.dirname(os.path.abspath(__file__))
voice_path, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
v = PiperVoice.load(voice_path)
L = json.load(open(os.path.join(here, "mom_lines_v8.json"))); L.pop("_about", None)
NAMES = ["pip", "nell", "rory", "tess", "bea", "juno", "milo", "ozzie", "wren"]

def speak(text, key, length=1.06, noise=0.6):
    text = re.sub(r"\*yawn\*", "Ahh...", text); text = re.sub(r"\*snore\*", "Hmm...", text); text = text.replace("*", "")
    raw = os.path.join(out, "_raw_" + key + ".wav")
    cfg = SynthesisConfig(length_scale=length, noise_scale=noise, noise_w_scale=0.8)
    with wave.open(raw, "wb") as w: v.synthesize_wav(text, w, syn_config=cfg)
    dst = os.path.join(out, key + ".wav")
    # warmth: -35 cents, low-mid lift, de-harsh top, gentle compression, 60 ms fades, peak -2 dB
    subprocess.run(["sox", raw, "-r", "22050", "-c", "1", dst, "gain", "-4", "pitch", "-35", "highpass", "70", "equalizer", "250", "1q", "2.5",
                    "equalizer", "4500", "1q", "-3", "compand", "0.02,0.2", "6:-70,-60,-20", "-4", "-90", "0.05",
                    "fade", "t", "0.03", "-0", "0.06", "norm", "-2"], check=True)
    os.remove(raw); return dst

n = 0
for k, t in L.items():
    speak(t, "vo_" + k, 1.12 if k.startswith(("mom_stir", "mom_bed", "mom_inspect_look")) else 1.04); n += 1
for nm in NAMES:
    speak(nm.capitalize() + "!", "vo_name_" + nm, 1.0); n += 1
print(n, "clips ->", out)
