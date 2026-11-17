#!/usr/bin/env python3
"""
SHHH! V12 sound design - "cartoon detective" (owner 2026-11-15: detective / spy / spooky-caper feel, in the spirit of classic sneaky
cartoon-caper, spy-thriller and haunted-mystery cartoon music). EVERYTHING here is original: every instrument is synthesized below
(no samples, no soundfonts) and every melody / bass line / riff is written note by note in this file - genre traits only (swung walking
bass, chromatic creeping, finger snaps, vibraphone, a spy ostinato, spooky organ + theremin), no quotation of any existing theme.

Usage: python make_detective.py <out_dir>        -> WAV + OGG files named like the game's Resources/Audio keys (_1 suffix)
Needs numpy, scipy, ffmpeg (for OGG).
"""
import os, subprocess, sys
import numpy as np
from scipy.signal import lfilter, butter, sosfilt, fftconvolve
from scipy.io import wavfile

SR = 44100
OUT = sys.argv[1] if len(sys.argv) > 1 else "detective_out"; os.makedirs(OUT, exist_ok=True)
RNG = np.random.default_rng(20261115)

NOTES = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}
def midi(name): return 12 * (int(name[-1]) + 1) + NOTES[name[:-1]]
def hz(m): return 440.0 * 2 ** ((m - 69) / 12.0)

# ------------------------------------------------------------------ DSP helpers
def env(n, a=0.005, d=0.1, s=0.7, r=0.08, hold=None):
    """ADSR over n samples; hold = sustain length in samples (default: until release)."""
    a, d, r = int(a * SR), int(d * SR), int(r * SR)
    hold = max(0, n - a - d - r) if hold is None else hold
    e = np.concatenate([np.linspace(0, 1, max(1, a), endpoint=False), np.linspace(1, s, max(1, d), endpoint=False),
                        np.full(hold, s), np.linspace(s, 0, max(1, r))])
    return np.pad(e, (0, max(0, n - len(e))))[:n]

def lp(x, f, order=2): return sosfilt(butter(order, min(f, SR * 0.45), "low", fs=SR, output="sos"), x)
def hp(x, f, order=2): return sosfilt(butter(order, f, "high", fs=SR, output="sos"), x)
def bp(x, lo, hi, order=2): return sosfilt(butter(order, [lo, min(hi, SR * 0.45)], "band", fs=SR, output="sos"), x)
def t_(n): return np.arange(n) / SR

def ks(f, dur, bright=0.5, damp=0.996, noise=None):
    """Karplus-Strong plucked string (IIR form: fast)."""
    n = int(dur * SR); N = max(2, int(round(SR / f)))
    exc = np.zeros(n); burst = (noise if noise is not None else RNG.uniform(-1, 1, N))[:N]
    burst = lp(burst, 800 + 9000 * bright); exc[:N] = burst
    a = np.zeros(N + 2); a[0] = 1.0; a[N] = -damp * 0.5; a[N + 1] = -damp * 0.5
    return lfilter([1.0], a, exc)

# ------------------------------------------------------------------ instruments (mono, ~unit peak)
def upright_bass(m, dur, vel=1.0):
    f = hz(m); y = ks(f, dur + 0.25, bright=0.18, damp=0.9975)
    y = lp(y, 1100); thump = np.sin(2 * np.pi * f * t_(len(y))) * np.exp(-t_(len(y)) * 18) * 0.5
    y = (y / (np.abs(y).max() + 1e-9) + thump) * env(len(y), 0.004, 0.25, 0.55, 0.12, hold=int(dur * SR * 0.6))
    return y * vel * 0.9

def pizz(m, dur=0.35, vel=1.0):
    y = ks(hz(m), dur, bright=0.55, damp=0.985); y = lp(y, 4500)
    return y / (np.abs(y).max() + 1e-9) * env(len(y), 0.002, 0.12, 0.25, 0.08) * vel

def harpsi(m, dur=0.18, vel=1.0):
    f = hz(m); y = ks(f, dur + 0.15, bright=0.95, damp=0.993) + 0.6 * ks(f * 2.003, dur + 0.15, bright=0.95, damp=0.99)
    y = hp(y, 180); return y / (np.abs(y).max() + 1e-9) * env(len(y), 0.001, 0.08, 0.45, 0.06, hold=int(dur * SR)) * vel

def vibes(m, dur=0.9, vel=1.0, trem=5.6):
    f = hz(m); n = int((dur + 0.6) * SR); tt = t_(n)
    y = (np.sin(2 * np.pi * f * tt) * np.exp(-tt * 2.2) + 0.35 * np.sin(2 * np.pi * f * 3.98 * tt) * np.exp(-tt * 7)
         + 0.12 * np.sin(2 * np.pi * f * 9.9 * tt) * np.exp(-tt * 18))
    y *= 1 - 0.28 * (0.5 + 0.5 * np.sin(2 * np.pi * trem * tt))                 # the vibraphone motor
    y *= np.minimum(1, tt / 0.002); return y * vel * 0.8

def horn(m, dur, vel=1.0, mute=True, slide_to=None):
    """Muted trumpet / sneaky sax-ish lead: band-limited saw + breath + 'wah' filter + delayed vibrato."""
    n = int((dur + 0.12) * SR); tt = t_(n); f0 = hz(m)
    f = np.full(n, f0) if slide_to is None else f0 * (hz(slide_to) / f0) ** np.clip(tt / max(dur, 1e-3), 0, 1)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * tt) * np.clip((tt - 0.18) / 0.25, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    y = sum(np.sin(k * ph) / k for k in range(1, 18) if f0 * k < 9000)
    y += RNG.normal(0, 0.03, n)
    wah = 900 + 1800 * np.clip(tt / 0.08, 0, 1) * np.exp(-tt * (2.5 if mute else 0.6))
    y = np.concatenate([lp(y[i:i + 512], wah[min(i, n - 1)]) for i in range(0, n, 512)])[:n] if mute else lp(y, 4200)
    y = bp(y, 220, 5000) if mute else y
    return y / (np.abs(y).max() + 1e-9) * env(n, 0.03, 0.08, 0.8, 0.1, hold=int(dur * SR)) * vel

def organ(ms, dur, vel=1.0):
    n = int((dur + 0.25) * SR); tt = t_(n); y = np.zeros(n)
    for m in ms:
        f = hz(m) * (1 + 0.002 * np.sin(2 * np.pi * 6.0 * tt))
        ph = 2 * np.pi * np.cumsum(f) / SR
        for k, a in ((1, 1.0), (2, 0.6), (3, 0.35), (4, 0.25), (6, 0.12), (8, 0.08)): y += a * np.sin(k * ph)
    y *= 1 - 0.2 * (0.5 + 0.5 * np.sin(2 * np.pi * 6.7 * tt))                   # rotary-speaker wobble
    return y / (np.abs(y).max() + 1e-9) * env(n, 0.06, 0.1, 0.85, 0.25, hold=int(dur * SR)) * vel

def theremin(path, dur, vel=1.0):
    """path: list of (time 0..1, midi) - gliding sine with wide vibrato: the spooky 'ooo-eee-ooo'."""
    n = int(dur * SR); tt = np.linspace(0, 1, n)
    m = np.interp(tt, [p[0] for p in path], [p[1] for p in path])
    f = hz(m) * (1 + 0.012 * np.sin(2 * np.pi * 5.8 * t_(n)))
    ph = 2 * np.pi * np.cumsum(f) / SR; y = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.06 * np.sin(3 * ph)
    return y * env(n, 0.18, 0.1, 0.9, 0.35) * vel * 0.7

def snap(vel=1.0):
    n = int(0.09 * SR); y = bp(RNG.normal(0, 1, n), 1800, 5200) * np.exp(-t_(n) * 70)
    y[:60] += np.linspace(1, 0, 60) * 2; return y / (np.abs(y).max() + 1e-9) * vel

def brush(vel=1.0, dur=0.22):
    n = int(dur * SR); y = bp(RNG.normal(0, 1, n), 2500, 9000) * env(n, 0.03, 0.08, 0.4, 0.1)
    return y / (np.abs(y).max() + 1e-9) * vel * 0.5

def ride(vel=1.0):
    n = int(0.9 * SR); tt = t_(n); mod = np.sin(2 * np.pi * 523 * 1.47 * tt) * 900
    y = np.sin(2 * np.pi * 523 * tt + mod / 523) * 0.4 + hp(RNG.normal(0, 1, n), 6000) * 0.5
    return y * np.exp(-tt * 4.5) * vel * 0.45

def tom(m=midi("A1"), vel=1.0, dur=0.45):
    n = int(dur * SR); tt = t_(n); f = hz(m) * (1 + 0.6 * np.exp(-tt * 30))
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tt * 7) + lp(RNG.normal(0, 1, n), 1200) * np.exp(-tt * 40) * 0.3
    return y * vel

def timpani(m, vel=1.0, dur=1.6, roll=0.0):
    n = int(dur * SR); tt = t_(n); f = hz(m) * (1 + 0.02 * np.exp(-tt * 8))
    y = (np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.5 * np.sin(2 * np.pi * np.cumsum(f * 1.5) / SR) * np.exp(-tt * 3)) * np.exp(-tt * 2.2)
    if roll > 0: y *= 1 + roll * np.sin(2 * np.pi * 14 * tt) ** 2
    y += lp(RNG.normal(0, 1, n), 700) * np.exp(-tt * 25) * 0.4
    return y / (np.abs(y).max() + 1e-9) * vel

def cymbal_swell(dur=1.2, vel=1.0):
    n = int(dur * SR); tt = np.linspace(0, 1, n)
    return hp(RNG.normal(0, 1, n), 4500) * (tt ** 2.5) * vel * 0.5

def woodblock(m=midi("E6"), vel=1.0):
    n = int(0.12 * SR); tt = t_(n); f = hz(m)
    y = np.sin(2 * np.pi * f * tt) + 0.5 * np.sin(2 * np.pi * f * 2.7 * tt) * np.exp(-tt * 60)
    return y * np.exp(-tt * 45) * vel

def tuba(m, dur, vel=1.0, slide_to=None):
    return lp(horn(m, dur, vel, mute=False, slide_to=slide_to), 900)

# ------------------------------------------------------------------ mixing
class Mix:
    def __init__(self, seconds):
        self.n = int(seconds * SR) + SR * 3; self.L = np.zeros(self.n); self.R = np.zeros(self.n)
    def add(self, y, at, gain=1.0, pan=0.0):
        i = int(at * SR); j = min(self.n, i + len(y)); y = y[:j - i] * gain
        self.L[i:j] += y * np.sqrt(0.5 * (1 - pan)); self.R[i:j] += y * np.sqrt(0.5 * (1 + pan))

_IR = None
def room(x, wet=0.16):
    global _IR
    if _IR is None:
        n = int(0.9 * SR); tt = t_(n); _IR = lp(RNG.normal(0, 1, n), 5000) * np.exp(-tt / 0.28); _IR /= np.sqrt((_IR ** 2).sum())
    return x + wet * fftconvolve(x, _IR)[:len(x)]

def finish_loop(mx, loop_seconds, wet=0.16):
    """Reverb, then fold everything past the loop end back onto the start (seamless loop)."""
    L, R = room(mx.L, wet), room(mx.R, wet); n = int(round(loop_seconds * SR))
    for ch in (L, R):
        tail = ch[n:]; k = min(len(tail), n); ch[:k] += tail[:k]
    return np.stack([L[:n], R[:n]])

def finish_one(mx, wet=0.18, fade=0.25):
    L, R = room(mx.L, wet), room(mx.R, wet); st = np.stack([L, R])
    lvl = np.abs(st).max(axis=0); thr = lvl.max() * 10 ** (-45 / 20)   # trim the tail at -45 dB below the peak
    nz = np.nonzero(lvl > thr)[0]; end = (nz[-1] + 1) if len(nz) else st.shape[1]
    st = st[:, :end]; f = int(fade * SR); st[:, -f:] *= np.linspace(1, 0, min(f, st.shape[1]))
    return st

def write(name, st, peak_db, mono=False, ogg=False):
    st = np.atleast_2d(st)
    if mono: st = st.mean(axis=0, keepdims=True)
    st = st / (np.abs(st).max() + 1e-9) * 10 ** (peak_db / 20)
    path = os.path.join(OUT, name + ".wav"); wavfile.write(path, SR, (st.T * 32767).astype(np.int16))
    if ogg:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-c:a", "libvorbis", "-q:a", "6", os.path.join(OUT, name + ".ogg")], check=True)
        os.remove(path)
    print(f"  {name}: {st.shape[1] / SR:5.2f} s, {'mono' if st.shape[0] == 1 else 'stereo'}, peak {peak_db} dBFS")

def write_set(names, stems, peak_db=-1.0):
    """Layered stems share one gain so calm+tension+danger together peak at peak_db (layers keep their balance)."""
    total = sum(stems); g = 10 ** (peak_db / 20) / (np.abs(total).max() + 1e-9)
    for nm, st in zip(names, stems):
        path = os.path.join(OUT, nm + ".wav"); wavfile.write(path, SR, (np.clip(st * g, -1, 1).T * 32767).astype(np.int16))
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-c:a", "libvorbis", "-q:a", "6", os.path.join(OUT, nm + ".ogg")], check=True)
        os.remove(path)
        rms = 20 * np.log10(np.sqrt(np.mean((st * g) ** 2)) + 1e-9)
        print(f"  {nm}: {st.shape[1] / SR:5.2f} s, stereo, layer RMS {rms:5.1f} dBFS")

# ------------------------------------------------------------------ the score
# E minor "case file" blues (16 bars). Chord = (root midi in octave 2, quality)
PROG = [("E", "m"), ("E", "m"), ("A", "m"), ("A", "m"), ("E", "m"), ("E", "m"), ("B", "7"), ("B", "7"),
        ("C", "7"), ("B", "7"), ("E", "m"), ("E", "m"), ("A", "m"), ("B", "7"), ("E", "m"), ("B", "7")]
def root(ch, octv=2): return midi(ch[0] + str(octv))
def tones(ch): return [0, 3, 7, 10] if ch[1] == "m" else [0, 4, 7, 10]
def swing(b): fr = b % 1.0; return b - fr + (0.667 if abs(fr - 0.5) < 1e-6 else fr)

def walking_bass(mx, bpm, prog, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for i, ch in enumerate(prog):
        r = root(ch); tns = tones(ch); nxt = root(prog[(i + 1) % len(prog)])
        line = [r, r + tns[1] if i % 2 else r + 7, r + 7 if i % 2 else r + tns[1] + 12 - 12, nxt + (1 if (i % 3) else -1)]   # chromatic approach
        for k, m in enumerate(line): mx.add(upright_bass(m, sb * 0.9, 0.95 if k == 0 else 0.8), at0 + (i * 4 + k) * sb, gain, -0.1)

def snaps_brush(mx, bpm, bars, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for b in range(bars):
        for k in range(4):
            if k in (1, 3): mx.add(snap(0.9), at0 + (b * 4 + k) * sb, gain * 0.55, 0.35)
            mx.add(brush(0.6 if k % 2 == 0 else 0.4), at0 + swing(b * 4 + k + 0.5) * sb, gain * 0.35, -0.3)

def ride_pattern(mx, bpm, bars, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for b in range(bars):
        for beat in (0, 1, 1.5, 2, 3, 3.5): mx.add(ride(0.8 if beat % 1 == 0 else 0.55), at0 + swing(b * 4 + beat) * sb, gain * 0.35, 0.4)

# tiptoe vibes motif: 2-bar cell, semitones above the chord root (octave 5), staccato - a creeping chromatic sneak
TIPTOE = [(0.5, 7, 0.25), (1.5, 10, 0.25), (2.0, 11, 0.25), (3.5, 12, 0.4), (4.5, 12, 0.25), (5.0, 11, 0.25), (5.5, 10, 0.25), (6.5, 7, 0.6)]
def vibes_tiptoe(mx, bpm, prog, at0=0.0, gain=1.0, every=2):
    sb = 60.0 / bpm
    for i in range(0, len(prog), every):
        r = root(prog[i], 5) - 12
        for (b, s, d) in TIPTOE: mx.add(vibes(r + s, d * sb, 0.7), at0 + swing(i * 4 + b) * sb, gain, 0.25)

# sneaky lead phrases (4 bars each), semitones above the phrase's chord root (octave 4); (beat, semis, length)
LEAD = [
    [(0.0, 7, 1.5), (1.5, 6, 0.5), (2.0, 5, 0.5), (2.5, 4, 1.5), (5.0, 3, 0.5), (5.5, 4, 0.5), (6.0, 7, 2.0), (9.5, 10, 0.5), (10.0, 12, 1.5), (12.0, 11, 2.0)],
    [(0.5, 12, 0.5), (1.0, 10, 0.5), (1.5, 7, 1.0), (3.0, 6, 0.5), (3.5, 7, 2.0), (6.5, 3, 0.5), (7.0, 2, 0.5), (7.5, 3, 1.5), (10.0, 7, 0.5), (10.5, 6, 0.5), (11.0, 5, 3.0)],
    [(0.0, 0, 0.5), (0.5, 3, 0.5), (1.0, 5, 0.5), (1.5, 6, 1.5), (4.0, 7, 1.0), (5.5, 10, 0.5), (6.0, 9, 2.0), (9.0, 7, 0.5), (9.5, 6, 0.5), (10.0, 7, 2.5)],
    [(0.5, 7, 0.5), (1.0, 7, 0.5), (1.5, 6, 0.5), (2.0, 7, 1.0), (4.5, 12, 0.5), (5.0, 11, 0.5), (5.5, 10, 0.5), (6.0, 7, 1.5), (9.0, 3, 0.5), (10.0, 2, 0.5), (11.0, 0, 3.0)],
]
def sneak_lead(mx, bpm, prog, at0=0.0, gain=1.0, mute=True):
    sb = 60.0 / bpm
    for p in range(len(prog) // 4):
        r = root(prog[p * 4], 4)
        for (b, s, d) in LEAD[p % len(LEAD)]: mx.add(horn(r + s, d * sb * 0.92, 0.75, mute=mute), at0 + swing(p * 16 + b) * sb, gain, -0.2)

def pizz_stabs(mx, bpm, prog, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for i, ch in enumerate(prog):
        r = root(ch, 4); tns = tones(ch)
        for beat in (1.5, 3.0):
            for k, s in enumerate((tns[1], tns[2])): mx.add(pizz(r + s, 0.3, 0.7), at0 + swing(i * 4 + beat) * sb + k * 0.004, gain, -0.45 + k * 0.2)

# spy ostinato: straight 16ths, semitones above the chord root (octave 3)
SPY = [0, 0, 7, 0, 8, 0, 7, 6, 0, 0, 7, 0, 10, 8, 7, 3]
def spy_ostinato(mx, bpm, prog, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for i, ch in enumerate(prog):
        r = root(ch, 3)
        for k in range(16): mx.add(harpsi(r + SPY[k], sb * 0.22, 0.85 if k % 4 == 0 else 0.6), at0 + (i * 4 + k * 0.25) * sb, gain, 0.15)

def toms(mx, bpm, bars, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for b in range(bars):
        for beat in (0.0, 2.0, 2.75): mx.add(tom(midi("E2") if beat == 0 else midi("B1"), 0.9), at0 + (b * 4 + beat) * sb, gain, -0.05)
        if b % 4 == 3:
            for k, m in enumerate(("A2", "G2", "E2", "D2")): mx.add(tom(midi(m), 0.7, 0.3), at0 + (b * 4 + 3 + k * 0.25) * sb, gain * 0.8, -0.4 + k * 0.25)

def organ_pads(mx, bpm, prog, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for i in range(0, len(prog), 2):
        r = root(prog[i], 3); mx.add(organ([r + s for s in tones(prog[i])], sb * 8 * 0.95, 0.6), at0 + i * 4 * sb, gain, 0.0)

def theremin_glides(mx, bpm, bars, at0=0.0, gain=1.0):
    sb = 60.0 / bpm
    for b in range(2, bars, 4):
        e5 = midi("E5")
        mx.add(theremin([(0, e5), (0.35, e5 + 7), (0.6, e5 + 5), (1.0, e5 - 5)], sb * 7), at0 + b * 4 * sb, gain, -0.35)

def match_set(bpm, prefix, final=False):
    bars = len(PROG); loop = bars * 4 * 60.0 / bpm
    calm, ten, dng = Mix(loop), Mix(loop), Mix(loop)
    walking_bass(calm, bpm, PROG, gain=0.9); snaps_brush(calm, bpm, bars, gain=0.9); vibes_tiptoe(calm, bpm, PROG, gain=0.45, every=4 if not final else 2)
    if final: ride_pattern(calm, bpm, bars, gain=0.7)
    sneak_lead(ten, bpm, PROG, gain=0.55); pizz_stabs(ten, bpm, PROG, gain=0.45)
    if not final: ride_pattern(ten, bpm, bars, gain=0.8)
    spy_ostinato(dng, bpm, PROG, gain=0.35); toms(dng, bpm, bars, gain=0.6); organ_pads(dng, bpm, PROG, gain=0.22); theremin_glides(dng, bpm, bars, gain=0.4)
    write_set([prefix + "_calm_1", prefix + "_tension_1", prefix + "_danger_1"], [finish_loop(m, loop) for m in (calm, ten, dng)])

def menu():
    bpm = 92; prog = [("E", "m"), ("E", "m"), ("A", "m"), ("A", "m"), ("C", "7"), ("B", "7"), ("E", "m"), ("B", "7")] * 2
    bars = len(prog); loop = bars * 4 * 60.0 / bpm; mx = Mix(loop); sb = 60.0 / bpm
    walking_bass(mx, bpm, prog, gain=0.85); snaps_brush(mx, bpm, bars, gain=0.75)
    # the "case file" vibes theme, 8 bars, played twice (second time an octave up for the first half)
    THEME = [(0.5, "B4", 0.5), (1.0, "E5", 0.5), (1.5, "G5", 1.0), (3.0, "F#5", 0.5), (3.5, "F5", 0.5), (4.0, "E5", 1.5),
             (6.0, "D5", 0.5), (6.5, "E5", 1.5), (8.5, "A4", 0.5), (9.0, "C5", 0.5), (9.5, "E5", 1.0), (11.0, "D#5", 0.5),
             (11.5, "E5", 2.0), (16.5, "G5", 0.5), (17.0, "A#5", 0.5), (17.5, "B5", 1.5), (19.5, "A5", 0.5), (20.0, "F#5", 1.0),
             (21.5, "D#5", 0.5), (22.0, "E5", 2.0), (24.5, "B4", 0.5), (25.0, "D5", 0.5), (25.5, "C5", 0.5), (26.0, "B4", 3.5)]
    for rep in range(2):
        for (b, n, d) in THEME: mx.add(vibes(midi(n) + (12 if rep == 1 and b < 16 else 0), d * sb, 0.75), swing(rep * 32 + b) * sb, 0.6, 0.2)
    organ_pads(mx, bpm, prog, gain=0.12)
    e5 = midi("E5"); mx.add(theremin([(0, e5 + 7), (0.5, e5 + 12), (1.0, e5)], sb * 6), 28 * sb, 0.3, -0.4)   # a little spooky "ooo"
    mx.add(theremin([(0, e5), (0.4, e5 + 3), (0.7, e5 - 2), (1.0, e5 - 5)], sb * 6), 60 * sb, 0.28, 0.4)
    write_set(["music_menu_1"], [finish_loop(mx, loop)], peak_db=-1.5)

# ------------------------------------------------------------------ stingers (one-shots)
def sting(name, build, peak=-2.0, seconds=5.0, mono=False, maxlen=None):
    mx = Mix(seconds); build(mx); st = finish_one(mx, wet=0.07 if mono else 0.18, fade=0.04 if mono else 0.25)
    if maxlen is not None and st.shape[1] > int(maxlen * SR):
        st = st[:, :int(maxlen * SR)].copy(); f = int(min(0.06, maxlen * 0.3) * SR); st[:, -f:] *= np.linspace(1, 0, f)
    write(name, st, peak, mono=mono, ogg=not mono)

def s_mom_wake(mx):   # "dun... dun... DUNNN" + timpani + swell + a theremin wail falling away
    for k, (t, ms) in enumerate(((0.0, ["E3", "G3", "A#3"]), (0.45, ["E3", "G3", "A#3"]), (0.9, ["D#3", "F#3", "A3", "C4"]))):
        d = 0.3 if k < 2 else 1.6
        for i, m in enumerate(ms): mx.add(horn(midi(m), d, 0.9, mute=False), t, 0.55, -0.4 + i * 0.27)
        mx.add(timpani(midi("E2") if k < 2 else midi("D#2"), 1.0, 1.2 if k < 2 else 2.0, roll=0.0 if k < 2 else 0.6), t, 0.8)
    mx.add(cymbal_swell(0.9), 0.0, 0.8); mx.add(ride(1.0), 0.9, 0.8)
    mx.add(theremin([(0, midi("B5")), (0.3, midi("E6")), (1.0, midi("E4"))], 2.4), 1.1, 0.45, 0.3)

def s_inspect(mx):    # "hmm, what happened here?" - bass slides up, vibes tremolo question
    mx.add(upright_bass(midi("E2"), 0.3), 0.0, 0.9); mx.add(upright_bass(midi("G2"), 0.3), 0.25, 0.8); mx.add(upright_bass(midi("A#2"), 0.6), 0.5, 0.9)
    for k, n in enumerate(("B4", "D5", "F5", "G#5")): mx.add(vibes(midi(n), 1.6, 0.7, trem=7.5), 0.55 + k * 0.08, 0.5, -0.3 + k * 0.2)
    mx.add(horn(midi("D5"), 0.5, 0.7), 1.1, 0.4); mx.add(horn(midi("E5"), 0.9, 0.7, slide_to=midi("F5")), 1.6, 0.4)

def s_countdown(mx):  # three finger-snap / woodblock tiptoes, then a vibes "ding!" chord
    for k in range(3): mx.add(snap(1.0), k * 0.8, 0.8, 0.2); mx.add(woodblock(midi("E6") - k * 2), k * 0.8 + 0.01, 0.5, -0.2)
    for i, n in enumerate(("E4", "B4", "D5", "G5")): mx.add(vibes(midi(n), 1.8, 0.8), 2.4 + i * 0.02, 0.55, -0.3 + i * 0.2)
    mx.add(ride(0.8), 2.4, 0.6)

def s_win_you(mx):    # "case closed!" - brush roll, horn + vibes climb, big final chord
    for k in range(10): mx.add(brush(0.5 + k * 0.05, 0.12), k * 0.07, 0.6)
    for k, n in enumerate(("E4", "G4", "B4", "D5", "E5")): mx.add(horn(midi(n), 0.16, 0.8, mute=False), 0.7 + k * 0.13, 0.5); mx.add(vibes(midi(n) + 12, 0.3, 0.6), 0.7 + k * 0.13, 0.35)
    for i, n in enumerate(("E3", "B3", "D4", "G4", "B4", "F#5")): mx.add(horn(midi(n), 1.5, 0.8, mute=False), 1.45, 0.32, -0.5 + i * 0.2); mx.add(vibes(midi(n) + 12, 2.0, 0.6), 1.45, 0.25)
    mx.add(cymbal_swell(0.6), 0.85, 0.7); mx.add(ride(1.0), 1.45, 1.0); mx.add(timpani(midi("E2"), 1.0, 1.6), 1.45, 0.7)

def s_win_other(mx):  # "wah... wah... wahhh" muted horn, falling, a little sad
    for k, (n, d) in enumerate((("A#4", 0.4), ("A4", 0.4), ("G#4", 0.4), ("G4", 1.4))):
        mx.add(horn(midi(n), d, 0.8, mute=True, slide_to=midi(n) - (1 if k == 3 else 0)), k * 0.5, 0.6)
    mx.add(upright_bass(midi("E2"), 1.2, 0.8), 1.5, 0.6)

def s_caught(mx):     # "busted!" - theremin falls, tuba slides down, cymbal choke
    mx.add(snap(1.0), 0.0, 0.8); mx.add(woodblock(midi("C6")), 0.0, 0.6)
    mx.add(theremin([(0, midi("E6")), (1.0, midi("E4"))], 1.3), 0.08, 0.5, 0.3)
    mx.add(tuba(midi("E3"), 1.1, 0.9, slide_to=midi("A#1")), 0.15, 0.7, -0.2)
    mx.add(timpani(midi("B1"), 0.9, 1.0), 1.2, 0.6); n = int(0.18 * SR); mx.add(hp(RNG.normal(0, 1, n), 5000) * np.exp(-t_(n) * 25), 1.2, 0.4)

def s_caught_other(mx):   # lighter "uh-oh": two pizz notes down + a snap
    mx.add(pizz(midi("G4"), 0.3, 0.9), 0.0, 0.8); mx.add(pizz(midi("D#4"), 0.5, 0.9), 0.22, 0.8); mx.add(snap(0.7), 0.5, 0.5)

# UI / feedback (short, mono)
def u_click(mx): mx.add(snap(1.0), 0.0, 0.7); mx.add(woodblock(midi("A6"), 0.6), 0.0, 0.25)
def u_hover(mx): mx.add(vibes(midi("B6"), 0.08, 0.5), 0.0, 0.5)
def u_card(mx):      # "a clue!" - three vibes notes up + shimmer
    for k, n in enumerate(("E5", "G5", "B5")): mx.add(vibes(midi(n), 0.4, 0.7), k * 0.09, 0.6)
    n = int(0.6 * SR); mx.add(hp(RNG.normal(0, 1, n), 7000) * np.exp(-t_(n) * 6) * 0.25, 0.2, 0.5)
def u_point(mx): mx.add(pizz(midi("E4"), 0.25, 1.0), 0.0, 0.7); mx.add(pizz(midi("B4"), 0.3, 1.0), 0.09, 0.7); mx.add(snap(0.8), 0.18, 0.5)
def u_step(mx): mx.add(vibes(midi("G5"), 0.25, 0.8), 0.0, 0.6); mx.add(vibes(midi("D6"), 0.5, 0.8), 0.12, 0.6)
def u_chore(mx):     # "case closed" mini: bass walk-up + vibes chord + rimshot
    for k, n in enumerate(("E2", "G2", "A2", "B2")): mx.add(upright_bass(midi(n), 0.12, 0.8), k * 0.1, 0.7)
    for i, n in enumerate(("E4", "G4", "B4", "D5", "F#5")): mx.add(vibes(midi(n), 1.0, 0.7), 0.42 + i * 0.015, 0.45)
    mx.add(woodblock(midi("D6"), 1.0), 0.42, 0.5); mx.add(snap(1.0), 0.42, 0.5)
def u_undone(mx):    # "uh-oh, somebody messed with this" - theremin dips, low pizz
    mx.add(theremin([(0, midi("B4")), (0.5, midi("A#4")), (1.0, midi("E4"))], 0.9), 0.0, 0.6); mx.add(pizz(midi("E2"), 0.5, 1.0), 0.55, 0.7)
def u_trap(mx):      # spy gadget: ratchet clicks + low harpsichord trill
    for k in range(5): mx.add(woodblock(midi("G6") - (k % 2) * 3, 0.6), k * 0.05, 0.45)
    for k in range(6): mx.add(harpsi(midi("E3") + (k % 2), 0.08, 0.8), 0.3 + k * 0.06, 0.5)

if __name__ == "__main__":
    print("music:"); menu(); match_set(104, "music_match"); match_set(126, "music_final", final=True)
    print("stingers:")
    sting("mom_wake_stinger_1", s_mom_wake, -1.5, 5.0); sting("inspect_stinger_1", s_inspect, -3.0, 4.0); sting("countdown_1", s_countdown, -3.0, 5.0)
    sting("win_you_1", s_win_you, -1.5, 5.0); sting("win_other_1", s_win_other, -3.0, 4.0); sting("elim_you_1", s_caught, -2.0, 4.0)
    sting("elim_other_1", s_caught_other, -4.0, 2.0)
    print("ui:")
    for nm, fn, pk, ml in (("ui_click_1", u_click, -8, 0.15), ("ui_hover_1", u_hover, -12, 0.25), ("card_show_1", u_card, -6, 0.9), ("point_at_1", u_point, -6, 0.5),
                           ("step_done_1", u_step, -6, 0.7), ("chore_done_1", u_chore, -4, 1.4), ("recipe_undone_1", u_undone, -5, 1.1), ("trap_set_1", u_trap, -6, 0.8)):
        sting(nm, fn, pk, 2.5, mono=True, maxlen=ml)
