#!/usr/bin/env python3
"""
SHHH! V8 original sound effects (2026-11-09). Every clip is synthesised here from scratch (modal/physical-style synthesis with numpy;
no recorded or third-party samples), except the cartoon pet and kid vocalisations, which are performed by the offline Piper voice
(onomatopoeia, pitch-shifted). Usage: make_sfx.py <out_dir> [piper_voice.onnx]
Output names follow AudioBank's convention: <key>_<n>.wav (44.1 kHz mono, 16-bit, peak -1 dBFS).
"""
import math, os, subprocess, sys, wave
import numpy as np

OUT = sys.argv[1] if len(sys.argv) > 1 else "sfx_out"; os.makedirs(OUT, exist_ok=True)
VOICE = sys.argv[2] if len(sys.argv) > 2 else None
SR = 44100
rng = np.random.default_rng(20261109)

def t(sec): return np.arange(int(sec * SR)) / SR
def env(n, a=0.002, d=0.2, curve=1.0):
    x = np.arange(n) / SR; e = np.minimum(1, x / max(a, 1e-4)) * np.exp(-x / max(d, 1e-4) * curve); return e
def noise(n): return rng.standard_normal(n)
def lp(x, fc):   # one-pole low-pass
    a = math.exp(-2 * math.pi * fc / SR); y = np.empty_like(x); s = 0.0
    for i, v in enumerate(x): s = (1 - a) * v + a * s; y[i] = s
    return y
def bp(x, fc, q=4.0):  # biquad band-pass (RBJ)
    w = 2 * math.pi * fc / SR; al = math.sin(w) / (2 * q); b0, b1, b2 = al, 0, -al; a0, a1, a2 = 1 + al, -2 * math.cos(w), 1 - al
    y = np.zeros_like(x); x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        o = (b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0; x2, x1, y2, y1 = x1, v, y1, o; y[i] = o
    return y
def modes(sec, freqs, decays, amps, jitter=0.02):
    x = t(sec); y = np.zeros_like(x)
    for f, d, a in zip(freqs, decays, amps):
        f *= 1 + rng.uniform(-jitter, jitter); y += a * np.sin(2 * np.pi * f * x + rng.uniform(0, 6.28)) * np.exp(-x / d)
    return y
def save(key, n, y, fade=0.004):
    y = np.asarray(y, float); m = np.max(np.abs(y)) or 1.0; y = y / m * 0.89
    k = int(fade * SR); y[:k] *= np.linspace(0, 1, k); y[-k:] *= np.linspace(1, 0, k)
    path = os.path.join(OUT, f"{key}_{n}.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype(np.int16).tobytes())
    return path

# ------------------------------------------------------------------ impacts per NoiseMaterial (6 takes each)
def impact(mat, i):
    s = 1 + 0.08 * (i - 2.5)
    if mat == "wood":
        y = modes(0.35, [210 * s, 540 * s, 960 * s, 1500 * s], [0.06, 0.035, 0.02, 0.012], [1, .6, .35, .2]) + 0.25 * lp(noise(int(.35 * SR)), 2500) * env(int(.35 * SR), d=.015)
    elif mat == "ceramic":
        y = modes(0.9, [1180 * s, 2950 * s, 4370 * s, 6120 * s], [0.25, 0.14, 0.09, 0.05], [1, .55, .4, .2]) + 0.3 * noise(int(.9 * SR)) * env(int(.9 * SR), d=.006)
    elif mat == "glass":
        y = modes(1.2, [2350 * s, 4980 * s, 7420 * s], [0.4, 0.22, 0.12], [1, .5, .3]) + 0.25 * noise(int(1.2 * SR)) * env(int(1.2 * SR), d=.004)
    elif mat == "metal":
        y = modes(1.0, [520 * s, 1311 * s, 2210 * s, 3390 * s, 4870 * s], [0.5, 0.3, 0.2, 0.14, 0.08], [1, .8, .5, .35, .2])
    elif mat == "plastic":
        y = modes(0.25, [620 * s, 1450 * s, 2600 * s], [0.03, 0.02, 0.012], [1, .5, .3]) + 0.4 * bp(noise(int(.25 * SR)), 1800 * s, 2) * env(int(.25 * SR), d=.01)
    elif mat == "toy":
        y = modes(0.3, [780 * s, 1820 * s, 3100 * s], [0.05, 0.03, 0.02], [1, .6, .3]) + 0.3 * bp(noise(int(.3 * SR)), 2400 * s, 3) * env(int(.3 * SR), d=.012)
    elif mat == "paper":
        n = int(0.28 * SR); y = bp(noise(n), 3200 * s, 1.2) * env(n, a=.003, d=.05) + 0.3 * bp(noise(n), 900, 1) * env(n, d=.02)
    elif mat == "fabric":
        n = int(0.3 * SR); y = lp(noise(n), 700 * s) * env(n, a=.008, d=.06)
    elif mat == "soft":
        n = int(0.3 * SR); y = lp(noise(n), 380 * s) * env(n, a=.01, d=.07) + 0.5 * modes(0.3, [90 * s], [0.05], [1])
    else:  # appliance: low metallic thunk + rattle
        n = int(0.6 * SR); y = modes(0.6, [140 * s, 410 * s, 880 * s, 1730 * s], [0.12, 0.08, 0.05, 0.03], [1, .7, .4, .3]) + 0.3 * bp(noise(n), 2600, 3) * env(n, d=.05)
    return y

# ------------------------------------------------------------------ UI + recipe feedback
def ui_click(): return modes(0.08, [1900, 3800], [0.012, 0.006], [1, .4]) + 0.3 * modes(0.08, [600], [0.02], [1])
def ui_hover(): x = t(0.09); return np.sin(2 * np.pi * (900 + 600 * x / 0.09) * x) * env(len(x), a=.005, d=.03)
def card_show(): n = int(.35 * SR); x = t(.35); return bp(noise(n), 1500, 0.8) * np.sin(np.pi * x / .35) ** 2 * 0.7 + 0.4 * np.sin(2 * np.pi * 1320 * x) * env(n, a=.12, d=.15)
def point_at(): x = t(.5); return sum(a * np.sin(2 * np.pi * f * x) * env(len(x), a=.003, d=d) for f, a, d in [(1568, 1, .2), (2093, .6, .16), (3136, .25, .1)])
def slot_click(i): s = 1 + .07 * i; return modes(.15, [1250 * s, 2700 * s], [.02, .01], [1, .5]) + 0.6 * modes(.15, [320 * s], [.025], [1])
def join_pop(i): x = t(.18); f = 420 + 900 * (x / .18) ** 0.5 + 60 * i; return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(x), a=.002, d=.05)
def wipe(i): n = int((.5 + .1 * i) * SR); x = np.arange(n) / SR; return bp(noise(n), 2400 + 300 * i, 0.9) * (0.5 + 0.5 * np.sin(2 * np.pi * 3.2 * x - 1.5)) * np.sin(np.pi * x / x[-1])
def rustle(i): n = int(.45 * SR); g = (rng.random(n) < 0.004 + 0.002 * i).astype(float); return bp(lp(g * noise(n) * 6, 6000) + 0.2 * noise(n), 3500, 0.7) * np.sin(np.pi * np.arange(n) / n)
def pour(i):
    n = int(1.1 * SR); y = bp(noise(n), 900 + 200 * i, 1.5) * 0.4
    for k in range(int(60 + 20 * i)): c = rng.integers(0, n - 2000); y[c:c + 2000] += np.sin(2 * np.pi * rng.uniform(500, 1400) * np.arange(2000) / SR) * np.exp(-np.arange(2000) / 300) * 0.3
    return y * np.sin(np.pi * np.arange(n) / n) ** 0.5
def lid(open_):
    x = t(.3); base = modes(.3, [380, 1150, 2050], [.05, .03, .02], [1, .5, .3]); sq = 0.2 * np.sin(2 * np.pi * (700 + (300 if open_ else -300) * x / .3) * x) * env(len(x), a=.02, d=.1)
    return base + sq
def chime(notes, d=.45):
    x = t(d + 0.15 * len(notes)); y = np.zeros_like(x)
    for k, f in enumerate(notes):
        o = int(k * 0.11 * SR); seg = x[: len(x) - o]; y[o:] += np.sin(2 * np.pi * f * seg) * env(len(seg), a=.003, d=d * .5) + 0.3 * np.sin(2 * np.pi * 2 * f * seg) * env(len(seg), d=d * .25)
    return y
def recipe_undone(): return chime([784, 659, 523, 440], .5) * 0.8
def grab(): n = int(.16 * SR); return lp(noise(n), 1200) * env(n, a=.01, d=.04) + 0.4 * modes(.16, [700], [.015], [1])
def squeak(i):
    x = t(.35); f = 1100 + 500 * np.sin(np.pi * x / .35) + 120 * i
    return bp(np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)), 1600, 2.5) * np.sin(np.pi * x / .35)
def trap_set(): x = t(.7); f = 180 * (1 + 0.8 * np.exp(-x * 6) * np.sin(2 * np.pi * 9 * x)); return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(x), a=.004, d=.25)
def door_creak():
    n = int(1.4 * SR); x = np.arange(n) / SR; f = 210 + 90 * np.sin(2 * np.pi * 0.7 * x) + 25 * rng.standard_normal(n).cumsum() / np.sqrt(n)
    stick = (rng.random(n) < 0.02) * 1.0; y = bp(np.sin(2 * np.pi * np.cumsum(f) / SR) * (0.6 + stick), 900, 1.5)
    return y * np.sin(np.pi * x / x[-1])
def door_close(): n = int(.5 * SR); return modes(.5, [95, 210, 430], [.12, .07, .04], [1, .5, .3]) + 0.3 * lp(noise(n), 900) * env(n, d=.03)
def snore():
    n = int(3.2 * SR); x = np.arange(n) / SR; inh = (x < 1.4); y = np.zeros(n)
    rasp = np.sign(np.sin(2 * np.pi * 34 * x)) * lp(noise(n), 900); y += np.where(inh, rasp * np.sin(np.pi * x / 1.4) ** 2, 0)
    exh = (x >= 1.7) & (x < 3.0); y += np.where(exh, bp(noise(n), 1600, 1.2) * 0.5 * np.sin(np.pi * (x - 1.7) / 1.3) ** 2, 0)
    return y
def night_amb():
    n = int(20 * SR); x = np.arange(n) / SR
    wind = lp(noise(n), 300) * (0.5 + 0.5 * np.sin(2 * np.pi * x / 20 * 3)) * 0.6
    cr = np.zeros(n)
    for k in range(70):   # crickets: chirp trains
        c = int(rng.uniform(0, 19.4) * SR); L = int(0.25 * SR); f = rng.uniform(3900, 4700)
        seg = np.arange(L) / SR; cr[c:c + L] += np.sin(2 * np.pi * f * seg) * (np.sin(2 * np.pi * 28 * seg) > 0.2) * np.sin(np.pi * seg / seg[-1]) * rng.uniform(.05, .15)
    y = wind + cr; k = int(0.5 * SR); y[:k] = y[:k] * np.linspace(0, 1, k) + y[-k:] * np.linspace(1, 0, k); return y[:-k]
def step_done(): return chime([659, 988], .4)
def chore_done(): return chime([523, 659, 784, 1047], .6)

def build():
    mats = ["wood", "ceramic", "glass", "metal", "plastic", "toy", "paper", "fabric", "soft", "appliance"]
    for m in mats:
        for i in range(6): save("impact_" + m, i + 1, impact(m, i))
    save("ui_click", 1, ui_click()); save("ui_hover", 1, ui_hover()); save("card_show", 1, card_show()); save("point_at", 1, point_at())
    for i in range(3): save("slot_click", i + 1, slot_click(i)); save("wipe", i + 1, wipe(i)); save("rustle", i + 1, rustle(i)); save("squeak", i + 1, squeak(i))
    for i in range(2): save("join_pop", i + 1, join_pop(i)); save("pour", i + 1, pour(i))
    save("lid_open", 1, lid(True)); save("lid_close", 1, lid(False)); save("recipe_undone", 1, recipe_undone())
    save("grab", 1, grab()); save("trap_set", 1, trap_set()); save("door_creak", 1, door_creak()); save("door_close_soft", 1, door_close())
    save("mom_snore", 1, snore()); save("amb_night", 1, night_amb(), fade=0.001); save("step_done", 1, step_done()); save("chore_done", 1, chore_done())
    if VOICE: voices()

def voices():
    """Cartoon vocalisations performed by the offline Piper voice and pitched with sox (original performances)."""
    from piper import PiperVoice
    from piper.config import SynthesisConfig
    v = PiperVoice.load(VOICE)
    def say(text, key, n, pitch, tempo=1.0, extra=()):
        raw = os.path.join(OUT, "_r.wav")
        with wave.open(raw, "wb") as w: v.synthesize_wav(text, w, syn_config=SynthesisConfig(length_scale=1.0, noise_scale=0.8, noise_w_scale=0.9))
        subprocess.run(["sox", raw, "-r", str(SR), "-c", "1", os.path.join(OUT, f"{key}_{n}.wav"), "gain", "-3", "pitch", str(pitch), "tempo", str(tempo)] + list(extra) + ["norm", "-1"], check=True)
        os.remove(raw)
    for i, (txt, pt) in enumerate([("Hee hee hee!", 700), ("Hi hi hi!", 820), ("Teehee!", 760)]): say(txt, "bot_giggle", i + 1, pt, 1.15)
    for i, (txt, pt) in enumerate([("Meeow?", 900), ("Mrrrow.", 700)]): say(txt, "cat_meow", i + 1, pt, 0.9)
    for i, (txt, pt) in enumerate([("Woof!", -300), ("Ruff ruff!", -250), ("Arf!", -150)]): say(txt, "dog_bark", i + 1, pt, 1.1, ("overdrive", "6"))
    say("Hmph.", "dog_huff", 1, -500, 1.0, ("lowpass", "1500"))
    for i, txt in enumerate(["Mmm hmm mm.", "Nnnh... mm.", "Hmm mm mmm?", "Mm... mmh."]): say(txt, "mom_babble", i + 1, -60, 0.95, ("lowpass", "2200"))

if __name__ == "__main__":
    build(); print(len([f for f in os.listdir(OUT) if f.endswith(".wav")]), "sfx ->", OUT)
