#!/usr/bin/env python3
"""
SHHH! V13 sound design - COMEDY cartoon detective (owner 2026-11-15: "I want comedy one with pink panther, scooby doo and so on";
the V12 synthesized noir set was rejected).

Style targets (genre traits only - every melody, riff, bass line and chord sequence below is ORIGINAL, written note by note here):
  * sneaky-caper jazz: swung tempo, breathy low tenor sax lead, chromatic leading-tone "tiptoe" figures, walking / two-feel bass,
    brushes, piano stabs, trombone "wah" sighs with falls, muted trumpet answers  -> menu + match (calm / tension layers)
  * spooky-mystery chase rock: straight 8ths, combo organ riff, fingered bass, guitar chops, bongos, tambourine, brass stabs,
    ghostly "ooh" choir and a sine-wave "theremin"                                  -> match danger layer + final-minute set
  * cartoon punctuation: slide whistle (whistle + pitch bend), boing (synthesized spring), woodblocks, cuica, guiro, xylophone runs,
    sad-trombone wobble                                                             -> stingers + UI

Instruments: MuseScore_General.sf3 0.2 (MIT, S. Christian Collins / FluidR3Mono / FluidR3 - see Docs/Audio_License_Manifest.csv),
rendered offline with FluidSynth 2.6.2 (LGPL tool, not shipped). Only the rendered audio goes into the game.

Usage: python make_comedy.py <out_dir> [only,comma,separated,names]
Needs numpy, scipy, mido, ffmpeg; FluidSynth + the soundfont under Tools/_ext (see FS / SF below).
"""
import os, subprocess, sys
import numpy as np
import mido
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

HERE = os.path.dirname(os.path.abspath(__file__))
EXT = os.path.normpath(os.path.join(HERE, "..", "_ext"))
FS = os.path.join(EXT, "fluidsynth_dl", "fluidsynth-v2.6.2-win10-x64-cpp11", "bin", "fluidsynth.exe")
SF = os.path.join(EXT, "soundfont_dl", "MuseScore_General.sf3")
SR = 44100; TPQ = 480
OUT = sys.argv[1] if len(sys.argv) > 1 else "comedy_out"; os.makedirs(OUT, exist_ok=True)
ONLY = set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None
TMP = os.path.join(OUT, "_render"); os.makedirs(TMP, exist_ok=True)
RNG = np.random.default_rng(20261115)

# ------------------------------------------------------------------ MIDI writing
class Song:
    """Beat-based MIDI builder. swing = where the off-beat eighth lands (0.5 straight, ~0.62 light swing, 0.67 triplet swing)."""
    def __init__(self, bpm, swing=0.5, hum=0.006, seed=7):
        self.bpm, self.sw, self.hum = bpm, swing, hum
        self.rng = np.random.default_rng(seed); self.ev = []; self.nch = 0; self.drums = False

    def b2t(self, b):
        fr = b % 1.0; base = b - fr
        if self.sw != 0.5: fr = fr / 0.5 * self.sw if fr < 0.5 else self.sw + (fr - 0.5) / 0.5 * (1 - self.sw)
        return (base + fr) * TPQ

    def _add(self, tick, order, msg): self.ev.append((int(max(0, round(tick))), order, msg))

    def _setup(self, ch, program, bank, vol, pan, rev, cho, bend):
        M = mido.Message
        if ch != 9: self._add(0, 0, M("control_change", channel=ch, control=0, value=bank))
        for m in (M("program_change", channel=ch, program=program), M("control_change", channel=ch, control=7, value=vol),
                  M("control_change", channel=ch, control=10, value=int(np.clip(64 + pan * 63, 0, 127))),
                  M("control_change", channel=ch, control=91, value=rev), M("control_change", channel=ch, control=93, value=cho),
                  M("control_change", channel=ch, control=101, value=0), M("control_change", channel=ch, control=100, value=0),
                  M("control_change", channel=ch, control=6, value=bend), M("control_change", channel=ch, control=38, value=0),
                  M("control_change", channel=ch, control=101, value=127), M("control_change", channel=ch, control=100, value=127),
                  M("control_change", channel=ch, control=11, value=127)):
            self._add(0, 0, m)

    def inst(self, program, bank=0, vol=100, pan=0.0, rev=40, cho=0, bend=2):
        ch = self.nch + (1 if self.nch >= 9 else 0); self.nch += 1
        assert ch <= 15, "too many channels"
        self._setup(ch, program, bank, vol, pan, rev, cho, bend); return ch

    def kit(self, program=0, vol=100, pan=0.0, rev=30):
        assert not self.drums; self.drums = True; self._setup(9, program, 0, vol, pan, rev, 0, 2); return 9

    def note(self, ch, b, pitch, dur, vel=90, human=True):
        j = self.rng.normal(0, self.hum) * TPQ if human else 0.0
        v = int(np.clip(vel + (self.rng.normal(0, 4) if human else 0), 1, 127))
        t0 = self.b2t(b) + j; t1 = max(t0 + 24, self.b2t(b + dur) + j - 6)
        self._add(t0, 2, mido.Message("note_on", channel=ch, note=int(pitch), velocity=v))
        self._add(t1, 1, mido.Message("note_off", channel=ch, note=int(pitch), velocity=0))

    def cc(self, ch, b, num, val): self._add(self.b2t(b), 1, mido.Message("control_change", channel=ch, control=num, value=int(np.clip(val, 0, 127))))
    def bend(self, ch, b, semis, rng): self._add(self.b2t(b), 1, mido.Message("pitchwheel", channel=ch, pitch=int(np.clip(semis / rng * 8191, -8192, 8191))))
    def glide(self, ch, b0, b1, s0, s1, rng, steps=24, curve=1.0):
        for k in range(steps + 1):
            f = k / steps; self.bend(ch, b0 + (b1 - b0) * f, s0 + (s1 - s0) * f ** curve, rng)
    def ramp(self, ch, b0, b1, num, v0, v1, steps=16):
        for k in range(steps + 1): f = k / steps; self.cc(ch, b0 + (b1 - b0) * f, num, v0 + (v1 - v0) * f)

    def save(self, path, total_beats):
        mid = mido.MidiFile(ticks_per_beat=TPQ); tr = mido.MidiTrack(); mid.tracks.append(tr)
        tr.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(self.bpm), time=0))
        ev = sorted(self.ev, key=lambda e: (e[0], e[1]))
        ev.append((int(total_beats * TPQ), 9, mido.Message("control_change", channel=15, control=3, value=0)))   # keeps the render running
        last = 0
        for t, _, m in ev: tr.append(m.copy(time=t - last)); last = t
        mid.save(path)

def render(song, name, total_beats):
    mid = os.path.join(TMP, name + ".mid"); wav = os.path.join(TMP, name + ".wav")
    song.save(mid, total_beats)
    subprocess.run([FS, "-ni", "-q", "-C", "0", "-R", "1", "-g", "0.45", "-r", str(SR), "-O", "float", "-T", "wav",
                    "-o", "synth.reverb.room-size=0.42", "-o", "synth.reverb.damp=0.35", "-o", "synth.reverb.width=0.9",
                    "-o", "synth.reverb.level=0.6", "-o", "synth.polyphony=384", "-F", wav, SF, mid], check=True, capture_output=True)
    sr, x = wavfile.read(wav); assert sr == SR, sr
    x = x.astype(np.float64)
    if x.dtype.kind == "i": x /= 32768.0
    return (x.T if x.ndim == 2 else np.stack([x, x]))

# ------------------------------------------------------------------ audio helpers
def fold_loop(st, loop_s):
    """Everything past the loop end (release + reverb tails) is folded back onto the start -> seamless loop."""
    n = int(round(loop_s * SR)); st = st.copy()
    if st.shape[1] < n: st = np.pad(st, ((0, 0), (0, n - st.shape[1])))
    tail = st[:, n:]; k = min(tail.shape[1], n); st[:, :k] += tail[:, :k]
    return st[:, :n]

def trim_one(st, fade=0.2, floor_db=-45):
    lvl = np.abs(st).max(axis=0); thr = lvl.max() * 10 ** (floor_db / 20)
    nz = np.nonzero(lvl > thr)[0]; a = max(0, nz[0] - int(0.002 * SR)) if len(nz) else 0; e = (nz[-1] + 1) if len(nz) else st.shape[1]
    st = st[:, a:e].copy(); f = min(int(fade * SR), st.shape[1]); st[:, -f:] *= np.linspace(1, 0, f)
    return st

def add(st, y, at_s, gain=1.0, pan=0.0):
    i = int(at_s * SR); j = min(st.shape[1], i + len(y)); y = y[:j - i] * gain
    st[0, i:j] += y * np.sqrt(0.5 * (1 - pan)); st[1, i:j] += y * np.sqrt(0.5 * (1 + pan))

def lp(x, f): return sosfilt(butter(2, f, "low", fs=SR, output="sos"), x)

def boing(dur=0.9, f0=150.0):
    """Cartoon spring: a plucked tone whose pitch wobbles fast and settles (the 'doinggg')."""
    t = np.arange(int(dur * SR)) / SR
    f = f0 * (1 + 0.55 * np.exp(-t * 4.5) * np.sin(2 * np.pi * 17 * t)) * (1 + 0.35 * np.exp(-t * 10))
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) + 0.45 * np.sin(2 * ph + 0.3) + 0.2 * np.sin(3 * ph) + 0.08 * np.sin(5 * ph)
    e = np.minimum(1, t / 0.003) * np.exp(-t * 3.2)
    return lp(y * e, 3500)

def write(name, st, peak_db, mono=False):
    st = np.atleast_2d(st)
    if mono: st = st.mean(axis=0, keepdims=True)
    st = st / (np.abs(st).max() + 1e-9) * 10 ** (peak_db / 20)
    path = os.path.join(OUT, name + ".wav"); wavfile.write(path, SR, (st.T * 32767).astype(np.int16))
    if not mono:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-c:a", "libvorbis", "-q:a", "6", os.path.join(OUT, name + ".ogg")], check=True)
        os.remove(path)
    print(f"  {name}: {st.shape[1] / SR:5.2f} s, {'mono' if st.shape[0] == 1 else 'stereo'}, peak {peak_db} dBFS")

def write_set(names, stems, peak_db=-1.0):
    """Layered stems share one gain so all layers together peak at peak_db (the layers keep their balance)."""
    total = sum(stems); g = 10 ** (peak_db / 20) / (np.abs(total).max() + 1e-9)
    for nm, st in zip(names, stems):
        path = os.path.join(OUT, nm + ".wav"); wavfile.write(path, SR, (np.clip(st * g, -1, 1).T * 32767).astype(np.int16))
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-c:a", "libvorbis", "-q:a", "6", os.path.join(OUT, nm + ".ogg")], check=True)
        os.remove(path)
        rms = 20 * np.log10(np.sqrt(np.mean((st * g) ** 2)) + 1e-9)
        print(f"  {nm}: {st.shape[1] / SR:5.2f} s, stereo, layer RMS {rms:5.1f} dBFS")

# ------------------------------------------------------------------ harmony helpers
def tones(q): return {"m": [0, 3, 7, 10], "7": [0, 4, 7, 10], "": [0, 4, 7, 12], "M7": [0, 4, 7, 11]}[q]
def above(pc, lo): return lo + ((pc - lo) % 12)
def voicing(root_pc, q, lo):
    rel = {"m": [3, 7, 10, 14], "7": [4, 10, 14, 21], "": [4, 7, 12, 16], "M7": [4, 7, 11, 14]}[q]
    return sorted(above((root_pc + r) % 12, lo) for r in rel)

# GM programs (0-based) / drum notes used below
SAX_T, TBN, TUBA, MUTE_TPT, BRASS, BSN, CLAR, WHISTLE = 66, 57, 58, 59, 61, 70, 71, 78
PIANO, MARIMBA, XYLO, VIBES, GLOCK, ORGAN, PERC_ORGAN, CLAV = 0, 12, 13, 11, 9, 16, 17, 7
AC_BASS, FINGER_BASS, PIZZ, TREM, OOHS, TIMP, CLEAN_GTR, SINE = 32, 33, 45, 44, 53, 47, 27, 80
K_BRUSH, K_STD = 40, 0
KICK, SNARE, CLAP, HH_C, HH_P, HH_O, CRASH, RIDE, SPLASH = 36, 38, 39, 42, 44, 46, 49, 51, 55
TAMB, VSLAP, BONGO_H, BONGO_L, WB_H, WB_L, CUICA_M, CUICA_O, GUIRO_S, GUIRO_L = 54, 58, 60, 61, 76, 77, 78, 79, 73, 74

# ================================================================== the sneak (menu + match calm / tension)
i_, iv_, bII7, bVI7, V7 = (0, "m"), (5, "m"), (1, "7"), (8, "7"), (7, "7")
A1 = [i_, i_, iv_, iv_, i_, i_, bII7, V7]; A2 = [i_, i_, iv_, iv_, i_, bVI7, V7, i_]; BR = [bVI7, bVI7, i_, i_, bII7, bII7, V7, V7]
SNEAK = A1 + A2 + BR + A1          # 32-bar AABA, relative to the tonic

# The "Tiptoe Caper" theme (original): (beat, semitones from the tonic, length in beats, articulation)
THEME_A1 = [(1.5, -1, .25), (2.0, 0, .4), (3.0, -1, .25), (3.5, 0, .4),
            (4.5, 3, .5), (5.0, 7, 2.5, "fall"),
            (9.5, 4, .25), (10.0, 5, .4), (11.0, 4, .25), (11.5, 5, .4),
            (12.5, 8, .5), (13.0, 12, 2.5, "fall"),
            (16.5, 10, .5), (17.0, 9, .5), (17.5, 8, .5), (18.0, 7, 1.0), (19.5, 3, .5),
            (20.0, 2, .5), (20.5, 3, .5), (21.0, 0, 2.0, "vib"),
            (24.5, 1, .5), (25.0, 0, .5), (25.5, 1, .5), (26.0, 8, 1.0), (27.0, 5, .5),
            (28.5, 6, .25), (29.0, 7, .5), (29.5, 11, 1.5, "vib")]
THEME_A2_END = [(16.5, 7, .5), (17.0, 10, .5), (17.5, 12, 1.0), (19.0, 10, .5), (19.5, 7, .5),
                (20.0, 6, .5), (20.5, 3, 1.5, "fall"),
                (24.5, 2, .5), (25.0, 5, .5), (25.5, 6, .25), (26.0, 7, 1.0),
                (28.0, 0, 3.0, "vib")]
THEME_B = [(0.5, 8, .5), (1.0, 6, .5), (1.5, 3, 2.0, "vib"),
           (4.5, 0, .5), (5.0, 1, .5), (5.5, 3, 2.0, "fall"),
           (8.5, 3, .5), (9.0, 7, .5), (9.5, 10, 2.0, "vib"),
           (12.5, 12, .5), (13.0, 10, .5), (13.5, 7, 1.5),
           (16.5, 8, .5), (17.0, 11, .5), (17.5, 13, 2.0, "vib"),
           (20.5, 13, .5), (21.0, 11, .5), (21.5, 8, 2.0),
           (24.0, 7, .5), (24.5, 11, .5), (25.0, 14, .5), (25.5, 17, 1.5, "shake"),
           (30.0, 2, .5), (30.5, -1, .5)]
def theme():
    a2 = [e for e in THEME_A1 if e[0] < 16] + THEME_A2_END
    return [(e[0] + off,) + tuple(e[1:]) for off, part in ((0, THEME_A1), (32, a2), (64, THEME_B), (96, THEME_A1)) for e in part]

def play_lead(s, ch, mel, base, at=0.0, vel=98, rng=4):
    for e in mel:
        b, r, d = e[0], e[1], e[2]; art = e[3] if len(e) > 3 else None; B = at + b; p = base + r
        s.note(ch, B, p, d * 0.94 if d >= 1 else d * 0.78, vel if d >= 1 else vel - 10)
        if art: s.ramp(ch, B + d * 0.3, B + d * 0.85, 1, 0, 72, 10)
        if art == "fall": s.glide(ch, B + d * 0.72, B + d * 0.94, 0, -4, rng, 10, 2.0); s.ramp(ch, B + d * 0.72, B + d * 0.94, 11, 127, 45, 8)
        if art == "shake":
            for k in range(int(d * 6)): s.bend(ch, B + 0.2 + k / 6, 1.6 if k % 2 == 0 else 0, rng)
        if art: s.bend(ch, B + d, 0, rng); s.cc(ch, B + d, 1, 0); s.cc(ch, B + d, 11, 127)

def bass_tiptoe(s, ch, prog, tonic, at=0.0, walk=(), vel=96):
    n = len(prog)
    for k, (r, q) in enumerate(prog):
        b = at + 4 * k; root = above((tonic + r) % 12, 36); nxt = above((tonic + prog[(k + 1) % n][0]) % 12, 36); t = tones(q)
        if k in walk:
            for i, p in enumerate([root, root + t[1], root + t[2], nxt + (1 if k % 2 else -1)]): s.note(ch, b + i, p, 0.85, vel - 6)
        else:
            s.note(ch, b, root, 1.3, vel); s.note(ch, b + 2, root + 7, 0.8, vel - 12)
            s.note(ch, b + 3.5, nxt + (1 if k % 2 else -1), 0.4, vel - 18)

def brushes(s, nbars, at=0.0, g=1.0):
    for k in range(nbars):
        b = at + 4 * k
        for beat, v in ((0, 50), (1, 62), (1.5, 40), (2, 50), (3, 62), (3.5, 40)): s.note(9, b + beat, RIDE, 0.3, int(v * g))
        for beat in (1, 3): s.note(9, b + beat, HH_P, 0.2, int(58 * g)); s.note(9, b + beat, SNARE, 0.2, int(46 * g))
        s.note(9, b, KICK, 0.3, int(42 * g))
        if k % 4 == 3: s.note(9, b + 3.5, SNARE, 0.2, int(58 * g)); s.note(9, b + 3.75, SNARE, 0.2, int(66 * g))

def piano_stabs(s, ch, prog, tonic, at=0.0, vel=58):
    pat = [[1.5, 3.5], [2.5], [1.5], [0.5, 2.5]]
    for k, (r, q) in enumerate(prog):
        for beat in pat[k % 4]:
            for p in voicing((tonic + r) % 12, q, 53): s.note(ch, at + 4 * k + beat, p, 0.28, vel)

def marimba_tiptoe(s, ch, prog, tonic, at=0.0, vel=70, every=4):
    """Little comic tiptoe on the marimba every few bars: chromatic step up into each chord tone."""
    for k, (r, q) in enumerate(prog):
        if k % every != every - 1: continue
        t = tones(q); base = above((tonic + r) % 12, 67); b = at + 4 * k
        for i, (beat, rel) in enumerate(((0.5, t[1] - 1), (1.0, t[1]), (1.5, t[2] - 1), (2.0, t[2]), (3.0, t[3]))):
            s.note(ch, b + beat, base + rel, 0.2, vel + (8 if i == 4 else 0))

def trombone_sighs(s, ch, tonic, at_list):
    """Comic 'bwah... bwaaah' under the sax's long notes (bars 2 and 4 of each A section): a step down that sags."""
    base = above(tonic % 12, 43)
    for at in at_list:
        for bb, hi, lo in ((6.0, 3, 2), (14.0, 8, 7)):
            B = at + bb; s.ramp(ch, B, B + 0.15, 11, 55, 127, 4); s.note(ch, B, base + hi, 0.45, 92)
            s.ramp(ch, B + 0.5, B + 0.7, 11, 50, 127, 4); s.note(ch, B + 0.5, base + lo, 1.3, 98)
            s.glide(ch, B + 1.2, B + 1.75, 0, -3, 4, 8, 2.0); s.bend(ch, B + 1.9, 0, 4); s.cc(ch, B + 1.9, 11, 127)

def muted_answers(s, ch, prog, tonic, at=0.0):
    for k, (r, q) in enumerate(prog):
        t = tones(q); base = above((tonic + r) % 12, 67)
        s.note(ch, at + 4 * k + 3.0, base + t[3], 0.35, 78); s.note(ch, at + 4 * k + 3.5, base + t[2], 0.4, 70)

# ------------------------------------------------------------------ danger overlay (same tempo / swing as the sneak)
def bongos(s, nbars, at=0.0, g=1.0):
    for k in range(nbars):
        b = at + 4 * k
        for beat in range(4):
            s.note(9, b + beat, BONGO_L, 0.2, int(72 * g)); s.note(9, b + beat + 0.5, BONGO_H, 0.2, int(88 * g))
        if k % 2 == 1:
            for i, nt in enumerate((BONGO_H, BONGO_H, BONGO_L, BONGO_L)): s.note(9, b + 3 + i * 0.25, nt, 0.15, int((70 + i * 8) * g))
        s.note(9, b + 1, TAMB, 0.2, int(64 * g)); s.note(9, b + 3, TAMB, 0.2, int(70 * g))

def organ_creep(s, ch, prog, tonic, at=0.0, vel=72):
    for k, (r, q) in enumerate(prog):
        root = above((tonic + r) % 12, 55); t = tones(q)
        for i, rel in enumerate((0, 7, 12, 7, t[1] + 12, 7, t[3], 7)): s.note(ch, at + 4 * k + i * 0.5, root + rel, 0.34, vel + (10 if i == 0 else 0))

def brass_stabs(s, brass, tuba, prog, tonic, at=0.0, vel=96):
    for k, (r, q) in enumerate(prog):
        t = tones(q); root = above((tonic + r) % 12, 48); b = at + 4 * k
        hits = [(0, 0.35, vel)] + ([(2.5, 0.3, vel - 12)] if k % 2 else [(3.5, 0.3, vel - 8)])
        for beat, d, v in hits:
            for rel in (0, t[1], t[3]): s.note(brass, b + beat, root + rel, d, v)
            s.note(tuba, b + beat, above((tonic + r) % 12, 28), d + 0.1, v)

def ghost_oohs(s, ch, prog, tonic, at=0.0, vel=52):
    for k, (r, q) in enumerate(prog):
        t = tones(q)
        for rel in t[1:]: s.note(ch, at + 4 * k, above((tonic + r + rel) % 12, 62), 3.8, vel)

def slide_whistle(s, ch, b, start, s0, s1, dur, rng=12, vel=95):
    s.bend(ch, b - 0.02, s0, rng); s.note(ch, b, start, dur, vel); s.glide(ch, b, b + dur * 0.92, s0, s1, rng, 28, 1.0)
    s.bend(ch, b + dur + 0.05, 0, rng)

# ------------------------------------------------------------------ builds
def sneak_rhythm(s, prog, tonic, at=0.0, walk=(), with_marimba=True, g=1.0):
    bass = s.inst(AC_BASS, vol=112, pan=-0.1, rev=18); pno = s.inst(PIANO, vol=84, pan=0.25, rev=38)
    s.kit(K_BRUSH, vol=104, rev=34)
    bass_tiptoe(s, bass, prog, tonic, at, walk=walk); brushes(s, len(prog), at, g); piano_stabs(s, pno, prog, tonic, at)
    if with_marimba: marimba_tiptoe(s, s.inst(MARIMBA, vol=88, pan=-0.35, rev=40), prog, tonic, at)

def match_set():
    bpm, sw, tonic = 120, 0.63, 7         # G minor
    beats = len(SNEAK) * 4; loop = beats * 60.0 / bpm; walk = tuple(range(16, 24))
    calm = Song(bpm, sw, seed=11); sneak_rhythm(calm, SNEAK, tonic, walk=walk)
    ten = Song(bpm, sw, seed=12)
    sax = ten.inst(SAX_T, vol=118, pan=0.05, rev=52, bend=4); play_lead(ten, sax, theme(), 55)
    trombone_sighs(ten, ten.inst(TBN, vol=96, pan=-0.3, rev=45, bend=4), tonic, (0, 32, 96))
    muted_answers(ten, ten.inst(MUTE_TPT, vol=74, pan=0.4, rev=50), BR, tonic, 64)
    dng = Song(bpm, sw, seed=13); dng.kit(K_STD, vol=100, rev=28); bongos(dng, len(SNEAK))
    organ_creep(dng, dng.inst(PERC_ORGAN, vol=78, pan=-0.45, rev=40), SNEAK, tonic)
    brass_stabs(dng, dng.inst(BRASS, vol=88, pan=0.3, rev=40), dng.inst(TUBA, vol=96, rev=30), SNEAK, tonic)
    ghost_oohs(dng, dng.inst(OOHS, vol=70, pan=0.0, rev=70), SNEAK, tonic)
    tim = dng.inst(TIMP, vol=96, rev=40)
    for bar in (0, 8, 16, 24): dng.note(tim, bar * 4, above(tonic, 40), 1.5, 100)
    for i in range(16): dng.note(tim, 92 + i * 0.25, above((tonic + 7) % 12, 40), 0.25, 60 + i * 3)       # roll into the last A
    wh = dng.inst(WHISTLE, vol=100, pan=0.5, rev=45, bend=12)
    slide_whistle(dng, wh, 62.0, 79, 0, 12, 1.5); slide_whistle(dng, wh, 126.0, 91, 12, -12, 1.6)
    stems = [fold_loop(render(x, nm, beats + 8), loop) for x, nm in ((calm, "match_calm"), (ten, "match_tension"), (dng, "match_danger"))]
    write_set(["music_match_calm_1", "music_match_tension_1", "music_match_danger_1"], stems)

def menu():
    bpm, sw, tonic = 108, 0.64, 0     # C minor; 4-bar tiptoe intro + the full theme
    intro = [i_, i_, bII7, V7]; prog = intro + SNEAK; beats = len(prog) * 4; loop = beats * 60.0 / bpm
    s = Song(bpm, sw, seed=21)
    sneak_rhythm(s, prog, tonic, walk=tuple(range(20, 28)))
    bsn = s.inst(BSN, vol=96, pan=-0.2, rev=40)
    for b, r, d in ((0.5, 0, .25), (1.0, 3, .25), (1.5, 7, .25), (2.5, 6, .25), (3.0, 7, .6), (4.5, 0, .25), (5.0, 3, .25), (5.5, 7, .25),
                    (6.5, 8, .25), (7.0, 7, .6), (8.5, 1, .25), (9.0, 5, .25), (9.5, 8, .25), (10.5, 11, .25), (11.0, 13, .6),
                    (12.5, 7, .25), (13.0, 11, .25), (13.5, 14, .25), (14.0, 17, .25), (15.0, 11, .5)):
        s.note(bsn, b, 48 + r, d, 88)
    sax = s.inst(SAX_T, vol=118, pan=0.05, rev=55, bend=4); play_lead(s, sax, theme(), 48, at=16)
    trombone_sighs(s, s.inst(TBN, vol=94, pan=-0.3, rev=45, bend=4), tonic, (16, 48, 112))
    muted_answers(s, s.inst(MUTE_TPT, vol=72, pan=0.4, rev=50), BR, tonic, 16 + 64)
    vib = s.inst(VIBES, vol=80, pan=0.35, rev=55)
    for e in THEME_A1: s.note(vib, 16 + 96 + e[0], 72 + e[1], max(0.3, e[2] * 0.8), 62)
    write_set(["music_menu_1"], [fold_loop(render(s, "menu", beats + 8), loop)], peak_db=-1.5)

# ================================================================== the chase (final minute)
ci, cIII, cIV, cVI, cVII, cV7 = (0, "m"), (3, ""), (5, ""), (8, ""), (10, ""), (7, "7")
CHASE = [ci, ci, cIII, cIV, ci, ci, cVI, cV7] * 2 + [cVI, cVII, ci, ci, cVI, cVII, cV7, cV7]
RIFF = [(0, 0, .5), (0.5, 3, .5), (1, 7, .5), (1.5, 6, .5), (2, 7, 1), (3, 3, .5), (3.5, 0, .5),
        (4, -2, .5), (4.5, 0, .5), (5, 3, .5), (5.5, 0, .5), (6, -5, 1.5), (7.5, -1, .5),
        (8, -2, .5), (8.5, 3, .5), (9, 7, .5), (9.5, 10, .5), (10, 7, 1), (11, 3, 1),
        (12, 5, .5), (12.5, 9, .5), (13, 12, 1), (14, 10, .5), (14.5, 9, .5), (15, 7, 1),
        (16, 0, .5), (16.5, 3, .5), (17, 7, .5), (17.5, 6, .5), (18, 7, 1), (19, 3, .5), (19.5, 0, .5),
        (20, 12, .5), (20.5, 10, .5), (21, 7, .5), (21.5, 3, .5), (22, 5, .5), (22.5, 6, .5), (23, 7, 1),
        (24, 8, .5), (24.5, 7, .5), (25, 3, .5), (25.5, 0, .5), (26, -4, 1), (27, 0, .5), (27.5, 3, .5),
        (28, 2, 1), (29, -1, .5), (29.5, 2, .5), (30, 7, 1.5)]
THEREMIN = [(64, 7, 3.6), (68, 5, 3.6), (72, 3, 1.8), (74, 7, 1.8), (76, 12, 3.6), (80, 8, 3.6), (84, 10, 3.6), (88, 11, 7.6)]

def chase_drums(s, nbars, g=1.0):
    for k in range(nbars):
        b = 4 * k; bridge = k >= 16
        for beat, v in ((0, 100), (1.5, 78), (2, 96)): s.note(9, b + beat, KICK, 0.3, int(v * g))
        for beat in (1, 3): s.note(9, b + beat, SNARE, 0.3, int(92 * g))
        for i in range(8): s.note(9, b + i * 0.5, RIDE if bridge else HH_C, 0.2, int((70 if i % 2 == 0 else 52) * g))
        if k % 2 == 1 and not bridge: s.note(9, b + 3.5, HH_O, 0.4, int(70 * g))
        if k % 8 == 0: s.note(9, b, CRASH, 1.5, int(96 * g))
        if k % 8 == 7:
            for i in range(4): s.note(9, b + 3 + i * 0.25, SNARE, 0.15, int((70 + i * 9) * g))

def chase_bass(s, ch, prog, tonic):
    n = len(prog)
    for k, (r, q) in enumerate(prog):
        root = above((tonic + r) % 12, 33); nxt = above((tonic + prog[(k + 1) % n][0]) % 12, 33)
        for i, p in enumerate((root, root, root + 12, root, root + 7, root, root + 12, nxt - 1)): s.note(ch, 4 * k + i * 0.5, p, 0.42, 100 if i in (0, 4) else 86)

def gtr_chops(s, ch, prog, tonic):
    for k, (r, q) in enumerate(prog):
        for beat in (0.5, 1.5, 2.5, 3.5):
            for p in voicing((tonic + r) % 12, q if q in ("m", "") else "", 52)[:3]: s.note(ch, 4 * k + beat, p, 0.18, 74)

def final_set():
    bpm, tonic = 150, 9                 # A minor, straight 8ths
    beats = len(CHASE) * 4; loop = beats * 60.0 / bpm
    calm = Song(bpm, 0.5, hum=0.004, seed=31); calm.kit(K_STD, vol=104, rev=24); chase_drums(calm, len(CHASE))
    chase_bass(calm, calm.inst(FINGER_BASS, vol=112, rev=12), CHASE, tonic)
    gtr_chops(calm, calm.inst(CLEAN_GTR, vol=82, pan=-0.35, rev=30), CHASE, tonic)
    ten = Song(bpm, 0.5, hum=0.004, seed=32)
    org = ten.inst(PERC_ORGAN, vol=112, pan=0.15, rev=38); clav = ten.inst(CLAV, vol=70, pan=-0.3, rev=30)
    for off in (0, 32):
        for b, r, d in RIFF:
            ten.note(org, off + b, 69 + r, d * 0.85, 100); ten.note(clav, off + b, 57 + r, d * 0.5, 80)
    for k in range(16, 24):          # bridge: organ off-beat chord pumps instead of the riff
        r, q = CHASE[k]
        for beat in (0.5, 1.5, 2.5, 3.5):
            for p in voicing((tonic + r) % 12, q if q in ("m", "") else "", 60)[:3]: ten.note(org, 4 * k + beat, p, 0.3, 84)
    ten.kit(K_STD, vol=90, rev=26)
    for k in range(len(CHASE)):
        for i in range(8): ten.note(9, 4 * k + i * 0.5, TAMB, 0.15, 60 if i % 2 else 76)
        for beat in (1, 3): ten.note(9, 4 * k + beat, CLAP, 0.2, 70)
    dng = Song(bpm, 0.5, hum=0.004, seed=33)
    brass_stabs(dng, dng.inst(BRASS, vol=96, pan=0.3, rev=36), dng.inst(TUBA, vol=96, rev=26), CHASE, tonic, vel=100)
    dng.kit(K_STD, vol=96, rev=26)
    for k in range(len(CHASE)):
        for i in range(16): dng.note(9, 4 * k + i * 0.25, BONGO_H if i % 4 in (1, 3) else BONGO_L, 0.12, 58 + (24 if i % 4 == 0 else 0))
    ghost_oohs(dng, dng.inst(OOHS, vol=74, rev=70), CHASE, tonic, vel=48)
    ther = dng.inst(SINE, bank=8, vol=96, pan=-0.2, rev=72, bend=2)
    for b, r, d in THEREMIN:
        dng.bend(ther, b - 0.01, -2, 2); dng.glide(ther, b, b + 0.4, -2, 0, 2, 8); dng.note(ther, b, 69 + r, d, 92)
        dng.ramp(ther, b + 0.3, b + 1.2, 1, 20, 90, 6); dng.cc(ther, b + d, 1, 0)
    xy = dng.inst(XYLO, vol=96, pan=0.45, rev=40)
    for bar in (7, 15):
        for i, p in enumerate((69, 71, 72, 74, 76, 77, 79, 81, 83, 84, 86, 88)): dng.note(xy, bar * 4 + 2 + i * (2 / 12), p, 0.12, 70 + i * 3)
    tim = dng.inst(TIMP, vol=96, rev=40)
    for bar in (0, 8, 16): dng.note(tim, bar * 4, above(tonic, 40), 1.2, 104)
    wh = dng.inst(WHISTLE, vol=100, pan=-0.5, rev=45, bend=12); slide_whistle(dng, wh, 94.0, 93, 12, -12, 1.8)
    stems = [fold_loop(render(x, nm, beats + 8), loop) for x, nm in ((calm, "final_calm"), (ten, "final_tension"), (dng, "final_danger"))]
    write_set(["music_final_calm_1", "music_final_tension_1", "music_final_danger_1"], stems)

# ================================================================== stingers (120 bpm: 1 beat = 0.5 s)
def sting(name, build, seconds, peak, mono=False, maxlen=None, fx=None, bpm=120):
    s = Song(bpm, 0.5, hum=0.002, seed=sum(map(ord, name))); build(s)
    st = render(s, "st_" + name, seconds * bpm / 60.0)
    if fx: fx(st)
    st = trim_one(st, fade=0.04 if mono else 0.2)
    if maxlen is not None and st.shape[1] > int(maxlen * SR):
        st = st[:, :int(maxlen * SR)].copy(); f = int(min(0.06, maxlen * 0.3) * SR); st[:, -f:] *= np.linspace(1, 0, f)
    write(name, st, peak, mono=mono)

def s_mom_wake(s):      # "DUN... DUN... DUNNNN!" (low brass + timpani + crash + tremolo strings), then a slide whistle "uh-oh" falls away
    br = s.inst(BRASS, vol=120, rev=50); tb = s.inst(TBN, vol=110, rev=45); tu = s.inst(TUBA, vol=120, rev=35)
    tr = s.inst(TREM, vol=96, rev=60); ti = s.inst(TIMP, vol=120, rev=45); s.kit(K_STD, vol=110, rev=50)
    for b, chord, bass, d, v in ((0, (50, 53, 56), 38, 0.6, 108), (1, (50, 53, 56), 38, 0.6, 112), (2, (51, 54, 57, 60), 39, 3.2, 124)):
        for p in chord: s.note(br, b, p + 12, d, v); s.note(tb, b, p, d, v)
        s.note(tu, b, bass, d, v); s.note(ti, b, bass + 12, 0.8, v)
    s.ramp(br, 2.2, 5.0, 11, 127, 60, 10)
    for p in (63, 66, 69, 72): s.note(tr, 2, p, 3.4, 96)
    for i in range(12): s.note(ti, 0.25 + i * 0.125, 50, 0.12, 50 + i * 5)
    s.note(9, 2, CRASH, 3, 120); s.note(9, 2, KICK, 0.5, 120)
    wh = s.inst(WHISTLE, vol=110, pan=0.3, rev=50, bend=12); slide_whistle(s, wh, 4.6, 88, 12, -12, 1.4, vel=100)

def s_inspect(s):       # "hmm... what's this?" pizz tiptoes, clarinet scoops up like a question, a vibes "ding?"
    pz = s.inst(PIZZ, vol=110, rev=40); cl = s.inst(CLAR, vol=118, rev=50, bend=7); vb = s.inst(VIBES, vol=100, rev=55)
    for i, p in enumerate((55, 56, 58, 59)): s.note(pz, i * 0.5, p, 0.3, 92 + i * 4)
    s.note(cl, 2.2, 62, 0.9, 92); s.bend(cl, 3.15, -2, 7); s.glide(cl, 3.2, 3.9, -2, 5, 7, 16, 2.0); s.note(cl, 3.2, 62, 1.4, 100)
    s.ramp(cl, 3.2, 4.4, 1, 0, 80); s.bend(cl, 4.8, 0, 7)
    for p in (79, 83, 86): s.note(vb, 4.2, p, 1.6, 84)
    s.kit(K_STD, vol=100, rev=40); s.note(9, 4.2, WB_H, 0.2, 90)

def s_countdown(s):     # "ready... set... GO!": woodblock tick-tocks speeding up, xylophone climbs, slide whistle up, brass "ta-DA!" + crash
    s.kit(K_STD, vol=110, rev=40); xy = s.inst(XYLO, vol=110, rev=40); br = s.inst(BRASS, vol=120, rev=45)
    tu = s.inst(TUBA, vol=110, rev=35); wh = s.inst(WHISTLE, vol=100, rev=40, bend=12)
    t = 0.0
    for i, gap in enumerate((0.8, 0.7, 0.55, 0.45, 0.35, 0.3)):
        s.note(9, t, WB_H if i % 2 == 0 else WB_L, 0.2, 90 + i * 4); s.note(xy, t, 72 + (0, 2, 4, 5, 7, 9)[i], 0.25, 84 + i * 4); t += gap
    slide_whistle(s, wh, t - 0.9, 84, -12, 12, 0.85, vel=96)
    for p in (60, 64, 67, 72): s.note(br, t, p, 0.35, 118)
    for p in (62, 65, 69, 74): s.note(br, t + 0.5, p + 2, 1.6, 124)
    s.note(tu, t, 36, 0.35, 118); s.note(tu, t + 0.5, 38, 1.6, 124)
    s.note(9, t + 0.5, CRASH, 2, 118); s.note(9, t + 0.5, KICK, 0.4, 118); s.note(9, t, SNARE, 0.2, 110)

def s_win_you(s):       # "case closed!" snare roll, big-band "ba-da-ba-DAAA" with a sax shake on top, xylophone gliss, crash + timpani
    s.kit(K_STD, vol=110, rev=45); br = s.inst(BRASS, vol=120, rev=50); sx = s.inst(SAX_T, vol=110, rev=50, bend=4)
    tu = s.inst(TUBA, vol=110, rev=35); xy = s.inst(XYLO, vol=100, rev=45); ti = s.inst(TIMP, vol=110, rev=45)
    for i in range(12): s.note(9, i * 0.083, SNARE, 0.08, 50 + i * 5)
    fan = [(1.0, (67, 72, 76), 0.4), (1.5, (69, 74, 77), 0.3), (1.83, (67, 72, 76), 0.3), (2.2, (72, 76, 79, 84), 3.0)]
    for b, ch, d in fan:
        for p in ch: s.note(br, b, p, d, 116 if d > 1 else 104)
        s.note(tu, b, (36, 41, 36, 36)[fan.index((b, ch, d))], d, 112)
    s.note(sx, 2.2, 74, 2.6, 110); s.ramp(sx, 2.6, 4.4, 1, 0, 110)
    for k in range(14): s.bend(sx, 2.5 + k * 0.16, 2 if k % 2 == 0 else 0, 4)
    s.bend(sx, 4.9, 0, 4)
    for i, p in enumerate(range(72, 97, 2)): s.note(xy, 1.0 + i * 0.07, p, 0.1, 80 + i * 2)
    s.note(9, 2.2, CRASH, 3, 124); s.note(9, 2.2, KICK, 0.4, 120); s.note(ti, 2.2, 48, 1.5, 118); s.note(ti, 1.0, 43, 0.5, 100)

def s_win_other(s):     # the sad trombone: three sagging "wah"s and a long wobbling "waaah"
    tb = s.inst(TBN, vol=124, rev=45, bend=4); tu = s.inst(TUBA, vol=90, rev=30)
    for i, (p, d) in enumerate(((58, 0.9), (57, 0.9), (56, 0.9))):
        b = i * 1.0; s.ramp(tb, b, b + 0.25, 11, 45, 127, 6); s.note(tb, b, p, d, 104); s.glide(tb, b + 0.55, b + 0.88, 0, -0.6, 4, 6)
        s.bend(tb, b + 0.95, 0, 4)
    s.ramp(tb, 3.0, 3.25, 11, 45, 127, 6); s.note(tb, 3.0, 55, 3.0, 110); s.note(tu, 3.0, 43, 3.0, 96)
    for k in range(36): s.bend(tb, 3.4 + k * 0.07, (-0.55 if k % 2 else 0.15) - k * 0.02, 4)
    s.ramp(tb, 4.6, 5.9, 11, 127, 0, 10)

def fx_caught(st):
    add(st, boing(0.9, 120.0), 0.95, 0.55, -0.2)
def s_caught(s):        # "BUSTED!" slide whistle falls, BONK (woodblock + floor tom + timpani), tuba "bwomp", cymbal splash
    wh = s.inst(WHISTLE, vol=116, rev=40, bend=12); tu = s.inst(TUBA, vol=124, rev=35, bend=4); ti = s.inst(TIMP, vol=110, rev=40)
    s.kit(K_STD, vol=116, rev=40)
    slide_whistle(s, wh, 0.0, 91, 12, -12, 1.7, vel=104)
    s.note(9, 1.85, WB_L, 0.3, 127); s.note(9, 1.85, 41, 0.5, 120); s.note(9, 1.85, SPLASH, 1.0, 100); s.note(ti, 1.85, 43, 1.0, 120)
    s.note(tu, 2.3, 34, 1.4, 120); s.glide(tu, 2.6, 3.6, 0, -4, 4, 12, 1.5)

def fx_oops(st):
    add(st, boing(0.75, 190.0), 0.0, 0.6, 0.15)
def s_caught_other(s):  # lighter "oops": a boing, two pizz notes stepping down, a cuica squeak
    pz = s.inst(PIZZ, vol=110, rev=40); s.kit(K_STD, vol=100, rev=40)
    s.note(pz, 0.5, 67, 0.4, 96); s.note(pz, 0.9, 61, 0.6, 96); s.note(9, 1.0, CUICA_O, 0.4, 90)

# ------------------------------------------------------------------ UI / feedback (mono, short)
def u_click(s): s.kit(K_STD, vol=110, rev=10); s.note(9, 0, WB_H, 0.1, 100)
def u_hover(s): x = s.inst(XYLO, vol=90, rev=20); s.note(x, 0, 96, 0.1, 70)
def u_card(s):          # "a clue!" xylophone run up + glockenspiel sparkle
    x = s.inst(XYLO, vol=110, rev=35); g = s.inst(GLOCK, vol=100, rev=45)
    for i, p in enumerate((72, 76, 79, 84)): s.note(x, i * 0.12, p, 0.2, 90 + i * 6)
    for p in (88, 91): s.note(g, 0.5, p, 0.8, 90)
def u_point(s):         # "boop!" little slide-whistle scoop + bongo tap
    w = s.inst(WHISTLE, vol=110, rev=20, bend=12); s.kit(K_STD, vol=100, rev=15)
    s.bend(w, -0.02, -7, 12); s.note(w, 0, 86, 0.35, 96); s.glide(w, 0, 0.3, -7, 0, 12, 10); s.note(9, 0.42, BONGO_H, 0.2, 100)
def u_step(s): x = s.inst(XYLO, vol=110, rev=30); s.note(x, 0, 79, 0.25, 96); s.note(x, 0.2, 84, 0.6, 108)
def u_chore(s):         # mini "ta-DA!" muted trumpet + brass + glock + a little cymbal
    t = s.inst(BRASS, vol=116, rev=35); g = s.inst(GLOCK, vol=100, rev=40); s.kit(K_STD, vol=96, rev=35)
    for p in (67, 72, 76): s.note(t, 0, p, 0.22, 104)
    for p in (72, 76, 79, 84): s.note(t, 0.45, p, 1.0, 118)
    s.note(g, 0.45, 96, 0.8, 90); s.note(9, 0.45, SPLASH, 1.0, 84)
def u_undone(s):        # "wah-waahh" - somebody messed with your chore
    tb = s.inst(TBN, vol=120, rev=30, bend=4)
    s.ramp(tb, 0, 0.12, 11, 50, 127, 4); s.note(tb, 0, 55, 0.4, 104); s.ramp(tb, 0.45, 0.6, 11, 50, 127, 4); s.note(tb, 0.45, 54, 1.2, 108)
    s.glide(tb, 0.75, 1.5, 0, -5, 4, 12, 1.6)
def u_trap(s):          # sneaky gadget: guiro ratchet, then a bassoon "hee-hee"
    s.kit(K_STD, vol=110, rev=20); b = s.inst(BSN, vol=116, rev=30)
    s.note(9, 0, GUIRO_L, 0.4, 100); s.note(9, 0.42, GUIRO_S, 0.15, 96)
    s.note(b, 0.62, 67, 0.12, 100); s.note(b, 0.86, 68, 0.14, 104)

if __name__ == "__main__":
    def want(n): return ONLY is None or n in ONLY
    print("music:")
    if want("menu"): menu()
    if want("match"): match_set()
    if want("final"): final_set()
    print("stingers:")
    for nm, fn, sec, pk, fx in (("mom_wake_stinger_1", s_mom_wake, 6.5, -1.5, None), ("inspect_stinger_1", s_inspect, 6.0, -3.0, None),
                                ("countdown_1", s_countdown, 6.5, -2.0, None), ("win_you_1", s_win_you, 6.5, -1.5, None),
                                ("win_other_1", s_win_other, 7.0, -3.0, None), ("elim_you_1", s_caught, 5.5, -2.0, fx_caught),
                                ("elim_other_1", s_caught_other, 3.0, -4.0, fx_oops)):
        if want("stingers") or want(nm): sting(nm, fn, sec, pk, fx=fx)
    print("ui:")
    for nm, fn, pk, ml in (("ui_click_1", u_click, -8, 0.15), ("ui_hover_1", u_hover, -12, 0.25), ("card_show_1", u_card, -6, 0.9),
                           ("point_at_1", u_point, -6, 0.6), ("step_done_1", u_step, -6, 0.7), ("chore_done_1", u_chore, -4, 1.4),
                           ("recipe_undone_1", u_undone, -5, 1.2), ("trap_set_1", u_trap, -6, 1.0)):
        if want("ui") or want(nm): sting(nm, fn, 2.5, pk, mono=True, maxlen=ml)
