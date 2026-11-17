"""
SHHH! V7 recipe/confrontation sounds (original, procedurally synthesised; adds keys only, never replaces V6 files).
Based on the V6 toolkit - original, procedurally synthesised, no third-party samples (no licence risk).
Replaces the V5 set key-for-key (same file names, more variants) so no game code has to change.

Design (Docs/AUDIO_DESIGN.md): cosy night house; silence is an asset; material-true Foley with a contact click,
modal body, a small bounce and a short room tail; music only in the menu (soft celesta lullaby), short stingers
for game moments; Mom's door sounds and snore stay quiet and warm.

usage: python3 Tools/synth_audio_v6.py <out_dir>
"""
import os, sys, wave
import numpy as np
from scipy.signal import lfilter, butter

SR = 32000
OUT = sys.argv[1] if len(sys.argv) > 1 else 'UnityProject/Assets/SHHH/Resources/Audio'
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(20261109)
REPORT = []


def t(d): return np.arange(int(SR * d)) / SR
def noise(d): return rng.standard_normal(int(SR * d))


def lp(x, fc, order=2):
    b, a = butter(order, min(fc, SR * 0.45) / (SR / 2), 'low'); return lfilter(b, a, x)


def hp(x, fc, order=2):
    b, a = butter(order, fc / (SR / 2), 'high'); return lfilter(b, a, x)


def bp(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), min(hi, SR * 0.45) / (SR / 2)], 'band'); return lfilter(b, a, x)


def env(d, attack=0.002, tau=0.1):
    tt = t(d); e = np.exp(-tt / tau); n = max(1, int(attack * SR)); e[:n] *= np.linspace(0, 1, n); return e


def pad(x, d):
    n = int(SR * d); return np.pad(x, (0, max(0, n - len(x))))[:n]


def room(x, amount=0.12):
    """Tiny early-reflection tail (small furnished room): a few damped taps + diffuse decay."""
    y = x.copy()
    for ms, g in ((11, .5), (17, .4), (23, .32), (31, .25), (43, .18), (61, .12)):
        k = int(SR * ms / 1000); y[k:] += x[:-k] * g * amount * 4
    tail = lp(noise(len(x) / SR) * np.exp(-t(len(x) / SR) / 0.12), 3500) * amount * 0.15
    return y + tail * np.max(np.abs(x))


def write(name, x, peak=0.85):
    x = np.asarray(x, dtype=np.float64)
    x = x - np.mean(x[: min(len(x), 64)]) * 0
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    fi = min(len(x), int(0.002 * SR)); x[:fi] *= np.linspace(0, 1, fi)
    fo = min(len(x), int(0.01 * SR)); x[-fo:] *= np.linspace(1, 0, fo)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())
    REPORT.append((name, len(x) / SR, float(np.sqrt(np.mean(x ** 2)))))


def modes(partials, d, jitter=0.03):
    tt = t(d); x = np.zeros(len(tt))
    for f, a, tau in partials:
        f *= rng.uniform(1 - jitter, 1 + jitter)
        x += a * np.sin(2 * np.pi * f * tt + rng.uniform(0, 6.28)) * np.exp(-tt / (tau * rng.uniform(0.85, 1.15)))
    return x


def click(d=0.012, fc=6000, amt=1.0):
    return lp(noise(d), fc) * env(d, 0.0003, d / 4) * amt


def marimba(freqs, d=0.6, gap=0.0, tau=0.22):
    out = np.zeros(int(SR * (d + gap * len(freqs))))
    for i, f in enumerate(freqs):
        x = modes([(f, 1, tau), (f * 3.93, .25, tau / 4), (f * 9.2, .06, tau / 10)], d, 0.002) + pad(click(0.004, 3000, .2), d)
        k = int(SR * gap * i); out[k:k + len(x)] += x[: len(out) - k]
    return room(out, 0.08)



# ---------------------------------------------------------------- V7 recipe gestures (quiet, close-up, presentation only)
for v in range(1, 4):   # item seats in its slot: small wooden tock + felt-pad thump
    x = modes([(240 * rng.uniform(.9, 1.1), 1, .035), (610, .45, .025), (1380, .18, .015)], .18) + pad(click(.006, 3500, .5), .18)
    write(f'slot_click_{v}', room(x, .05), .42)
for v in range(1, 3):   # two things joined (pair / lid / layer): soft "pmp" with a short downward glide
    d = .16; tt = t(d); f = 330 * rng.uniform(.92, 1.08) * (1 - .35 * tt / d)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(d, .002, .045) + pad(click(.004, 2500, .35), d)
    write(f'join_pop_{v}', room(x, .05), .4)
for v in range(1, 4):   # one wipe stroke: band-passed swish
    d = rng.uniform(.2, .28); tt = t(d)
    x = bp(noise(d), 700, 3200 * rng.uniform(.9, 1.2)) * np.sin(np.pi * tt / d) ** 2
    write(f'wipe_{v}', x, .3)
for v in range(1, 4):   # fold / roll cloth: grainy rustle
    d = .34; tt = t(d); g = np.zeros(len(tt))
    for _ in range(14):
        k = rng.integers(0, len(tt) - 900); g[k:k + 900] += np.hanning(900) * rng.uniform(.3, 1)
    x = lp(hp(noise(d), 400), 3800) * (0.25 + g) * np.sin(np.pi * tt / d) ** .6
    write(f'rustle_{v}', x, .3)
for v in range(1, 3):   # pretend pour (cereal / water stage): soft granular trickle
    d = .45; x = np.zeros(int(SR * d))
    for _ in range(38):
        k = rng.integers(0, len(x) - 400); x[k:k + 400] += modes([(rng.uniform(1800, 3600), 1, .004)], 400 / SR) * rng.uniform(.2, 1)
    x *= np.sin(np.pi * t(d) / d) ** .5
    write(f'pour_{v}', room(x, .04), .28)
write('lid_open_1', room(modes([(520, 1, .05), (1150, .4, .03)], .22) + pad(click(.008, 3000, .5), .22), .05), .35)
write('lid_close_1', room(modes([(300, 1, .06), (760, .4, .03)], .22) + pad(click(.006, 2500, .6), .22), .05), .38)
# ---------------------------------------------------------------- V7 recipe board + confrontation stingers
write('recipe_undone_1', marimba([659.3, 523.3], 0.5, 0.15, tau=0.16), .4)      # "uh-oh" (someone messed with your job)
fl = bp(noise(.09), 1500, 6000) * env(.09, .002, .025)
_m = marimba([1318.5, 1760], 0.6, 0.07, tau=0.15); write('card_show_1', pad(fl, len(_m) / SR) + _m * .8, .45)   # a memory card shown to Mom
write('point_at_1', marimba([440, 415.3], 0.35, 0.1, tau=0.1), .32)            # pointing (weak, no proof)

for name, d, rms in REPORT: print(f'{name:22s} {d:5.2f}s rms {rms:.3f}')
