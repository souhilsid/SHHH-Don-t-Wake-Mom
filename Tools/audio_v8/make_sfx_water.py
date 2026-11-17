#!/usr/bin/env python3
"""SHHH! Gate C water/dishwashing sounds (2026-11-10), synthesised from scratch with numpy like make_sfx.py (no samples).
tap_run_1 (seamless 4 s loop), splash_1..3, tap_turn_1..2, sponge_1..3, rinse_1..2. Usage: make_sfx_water.py <out_dir>"""
import math, os, sys, wave
import numpy as np
OUT = sys.argv[1] if len(sys.argv) > 1 else "sfx_out"; os.makedirs(OUT, exist_ok=True)
SR = 44100; rng = np.random.default_rng(20261110)
def noise(n): return rng.standard_normal(n)
def biquad(x, kind, fc, q=0.7):
    w = 2 * math.pi * fc / SR; al = math.sin(w) / (2 * q); c = math.cos(w)
    if kind == "bp": b0, b1, b2 = al, 0, -al
    elif kind == "lp": b0, b1, b2 = (1 - c) / 2, 1 - c, (1 - c) / 2
    else: b0, b1, b2 = (1 + c) / 2, -(1 + c), (1 + c) / 2
    a0, a1, a2 = 1 + al, -2 * c, 1 - al
    y = np.zeros_like(x); x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        o = (b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0; x2, x1, y2, y1 = x1, v, y1, o; y[i] = o
    return y
def bubbles(n, rate, fmin=500, fmax=2400, dur=(0.01, 0.04), amp=1.0):
    """Minnaert-style bubble 'plinks': short rising sine chirps with exponential decay at random times."""
    y = np.zeros(n); k = int(rate * n / SR)
    for _ in range(k):
        st = rng.integers(0, n); d = rng.uniform(*dur); m = int(d * SR * 4); f0 = rng.uniform(fmin, fmax)
        x = np.arange(min(m, n - st)) / SR
        f = f0 * (1 + 1.5 * x / d * 0.1); ph = 2 * np.pi * np.cumsum(f) / SR
        y[st:st + len(x)] += amp * rng.uniform(0.3, 1) * np.sin(ph) * np.exp(-x / d)
    return y
def save(key, n, y, fade=0.004, loop=False):
    y = np.asarray(y, float); m = np.max(np.abs(y)) or 1.0; y = y / m * 0.89
    if not loop:
        k = int(fade * SR); y[:k] *= np.linspace(0, 1, k); y[-k:] *= np.linspace(1, 0, k)
    p = os.path.join(OUT, f"{key}_{n}.wav")
    with wave.open(p, "wb") as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((y * 32767).astype(np.int16).tobytes())
    print(p)

def tap_run():
    L = 4.0; X = 0.5; n = int((L + X) * SR)
    hiss = biquad(noise(n), "bp", 1900, 0.5) * 0.55 + biquad(noise(n), "lp", 700) * 0.6   # stream body + basin rumble
    gurgle = bubbles(n, 140, 350, 1600, (0.008, 0.03), 0.5)
    mod = 1 + 0.12 * np.sin(2 * np.pi * 0.7 * np.arange(n) / SR) + 0.05 * noise(n).cumsum() / math.sqrt(n)
    y = (hiss + gurgle) * mod
    a = int(L * SR); xf = int(X * SR)                 # seamless loop: crossfade the tail into the head
    out = y[:a].copy(); r = np.linspace(0, 1, xf)
    out[:xf] = y[:xf] * r + y[a:a + xf] * (1 - r)
    return out
def splash(i):
    n = int((0.35 + 0.08 * i) * SR); x = np.arange(n) / SR
    body = biquad(noise(n), "bp", 1400 + 400 * i, 0.6) * np.exp(-x / (0.06 + 0.02 * i))
    return body + bubbles(n, 120, 600, 2600, (0.006, 0.02), 0.6) * np.exp(-x / 0.15)
def tap_turn(i):
    n = int(0.28 * SR); x = np.arange(n) / SR
    squeak = np.sin(2 * np.pi * np.cumsum(900 + 500 * i + 380 * np.sin(2 * np.pi * 7 * x)) / SR) * np.exp(-x / 0.07) * 0.5
    clunk = np.sin(2 * np.pi * 180 * x) * np.exp(-x / 0.03) + biquad(noise(n), "lp", 900) * np.exp(-x / 0.02) * 0.5
    return squeak + clunk
def sponge(i):
    n = int((0.45 + 0.1 * i) * SR); x = np.arange(n) / SR
    squish = biquad(noise(n), "bp", 900 + 250 * i, 0.9) * (0.5 + 0.5 * np.sin(2 * np.pi * (3 + i) * x - 1.4)) ** 2
    return squish * np.sin(np.pi * x / x[-1]) + bubbles(n, 220, 1500, 4200, (0.003, 0.01), 0.35)
def rinse(i):
    n = int((0.9 + 0.2 * i) * SR); x = np.arange(n) / SR
    return (biquad(noise(n), "bp", 2600 + 500 * i, 0.5) * 0.7 + bubbles(n, 260, 700, 3000, (0.005, 0.02), 0.5)) * np.sin(np.pi * x / x[-1]) ** 0.5

save("tap_run", 1, tap_run(), loop=True)
for i in range(3): save("splash", i + 1, splash(i)); save("sponge", i + 1, sponge(i))
for i in range(2): save("tap_turn", i + 1, tap_turn(i)); save("rinse", i + 1, rinse(i))
