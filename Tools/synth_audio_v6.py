"""
SHHH! V6 sound redesign (AUD-G6) - original, procedurally synthesised, no third-party samples (no licence risk).
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
rng = np.random.default_rng(20261108)
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


# ---------------------------------------------------------------- material impacts (6 variants each)
MAT = {
    #            body partials (f, amp, decay)                                  click  bounce  dur   fc
    'ceramic':  ([(1720, 1, .16), (2630, .7, .12), (3790, .5, .08), (5310, .3, .05)], .5, .45, .55, 7000),
    'glass':    ([(2380, 1, .45), (3710, .7, .33), (5020, .45, .25), (6900, .3, .18)], .35, .5, 1.1, 9000),
    'metal':    ([(310, 1, .9), (317, .8, .9), (842, .7, .7), (1340, .55, .55), (2190, .4, .4), (3460, .25, .3)], .45, .35, 1.5, 6000),
    'wood':     ([(180, 1, .06), (420, .6, .045), (760, .4, .03), (1290, .2, .02)], 1.0, .25, .32, 4000),
    'plastic':  ([(560, 1, .045), (1220, .6, .035), (2080, .35, .025)], .8, .4, .3, 5000),
    'toy':      ([(830, 1, .05), (1690, .5, .035), (2850, .3, .025)], .8, .5, .35, 6000),
    'appliance': ([(120, 1, .22), (380, .6, .16), (690, .3, .1)], .9, .2, .55, 2500),
}
for mat, (parts, cl, bounce, dur, fc) in MAT.items():
    for v in range(1, 7):
        body = modes(parts, dur)
        x = pad(click(0.012, fc, cl), dur) + body
        if bounce > 0:  # a smaller second contact 50-110 ms later (things rarely land just once)
            k = int(SR * rng.uniform(0.05, 0.11)); x[k:] += (modes(parts, dur, 0.05)[: len(x) - k] + pad(click(0.01, fc, cl * .6), dur)[: len(x) - k]) * bounce * rng.uniform(.5, 1)
        if mat == 'toy':  # inner rattle
            for i in range(4):
                k = int(SR * (0.02 + i * rng.uniform(0.02, 0.045))); x[k:k + 300] += modes([(1300 + 400 * i, .5, .008)], 300 / SR)[: len(x[k:k + 300])]
        write(f'impact_{mat}_{v}', room(x, 0.1))

SOFTS = {'soft': (250, .09, .5), 'fabric': (600, .07, .6), 'paper': (4000, .045, .75)}
for mat, (fc, d, peak) in SOFTS.items():
    for v in range(1, 7):
        n = noise(d + 0.05)
        x = (lp(n, fc * rng.uniform(.8, 1.2)) if mat != 'paper' else bp(n, 900, fc * rng.uniform(.9, 1.3))) * env(d + 0.05, 0.002, d * 0.45)
        if mat == 'paper':  # page flutter after the slap
            k = int(SR * 0.03); fl = bp(noise(0.12), 1500, 5000) * env(0.12, 0.01, 0.04) * (0.5 + 0.5 * np.sin(2 * np.pi * 38 * t(0.12))) * 0.4
            x = pad(x, 0.2); x[k:k + len(fl)] += fl[: len(x) - k]
        if mat == 'soft': x = x + modes([(95, .6, .05)], len(x) / SR)
        write(f'impact_{mat}_{v}', room(x, 0.06), peak)

# rubber toy squeak: pitch glide with formant
for v in range(1, 4):
    d = 0.32; tt = t(d); f0 = rng.uniform(900, 1300)
    f = f0 * (1 + 0.35 * np.sin(np.pi * tt / d)) * (1 + 0.02 * np.sin(2 * np.pi * 28 * tt))
    ph = 2 * np.pi * np.cumsum(f) / SR
    x = (np.sin(ph) + 0.5 * np.sin(2 * ph) + 0.25 * np.sin(3 * ph)) * np.sin(np.pi * tt / d) ** 0.7
    write(f'squeak_{v}', bp(x, 600, 5000), .7)

# ---------------------------------------------------------------- UI (warm wooden clicks, soft marimba)
def marimba(freqs, d=0.6, gap=0.0, tau=0.22):
    out = np.zeros(int(SR * (d + gap * len(freqs))))
    for i, f in enumerate(freqs):
        x = modes([(f, 1, tau), (f * 3.93, .25, tau / 4), (f * 9.2, .06, tau / 10)], d, 0.002) + pad(click(0.004, 3000, .2), d)
        k = int(SR * gap * i); out[k:k + len(x)] += x[: len(out) - k]
    return room(out, 0.08)


write('ui_click_1', room(modes([(1750, 1, .018), (3900, .4, .01)], .07) + pad(click(.006, 5000, .6), .07), .05), .55)
write('ui_hover_1', modes([(2600, 1, .012)], .04), .18)
write('step_done_1', marimba([1046.5]), .45)
write('chore_done_1', marimba([784, 1046.5, 1318.5], 0.7, 0.09), .55)
write('grab_1', bp(noise(.11), 400, 2500) * np.sin(np.pi * t(.11) / .11) ** 1.5, .22)
write('trap_set_1', marimba([392, 370], 0.4, 0.13, tau=0.12), .38)
ticks = [marimba([1568], 0.12, tau=0.04) for _ in range(3)]
cd = np.concatenate([np.concatenate([tk, np.zeros(int(SR * 0.88))]) for tk in ticks] + [marimba([2093, 1568], 0.6, 0.0)])
write('countdown_1', cd, .5)

# ---------------------------------------------------------------- Mom's door, snore, stingers (bassoon-ish, comic not scary)
def reed(f, d, vib=5.0):
    tt = t(d); ph = 2 * np.pi * np.cumsum(f * (1 + 0.012 * np.sin(2 * np.pi * vib * tt))) / SR
    x = sum(np.sin(k * ph) / k ** 1.2 for k in range(1, 9))
    return lp(x, 1400) * np.minimum(1, tt / 0.03) * np.exp(-tt / (d * 0.9))


def seq(notes, voice):
    out = []
    for f, d in notes: out.append(voice(f, d) if f > 0 else np.zeros(int(SR * d)))
    return np.concatenate(out)


write('mom_wake_stinger_1', room(seq([(196, .16), (0, .04), (185, .16), (0, .04), (147, .8)], reed), .15), .8)
creak = np.zeros(int(SR * 0.9)); tt = t(0.9); rate = 60 + 140 * (tt / 0.9) ** 1.5; pos = 0.0
for i in range(len(tt)):
    pos += rate[i] / SR
    if pos >= 1: pos -= 1; creak[i] = rng.uniform(.6, 1)
write('door_creak_1', bp(lfilter([1], [1, -1.6, 0.81], creak), 300, 3000) * np.sin(np.pi * tt / 0.9), .45)
write('door_close_soft_1', room(modes([(105, 1, .12), (250, .5, .07)], .45) + pad(click(.02, 1500, .5), .45), .1), .42)
sn = np.zeros(int(SR * 3.2)); tt = t(3.2)
breath = (np.sin(np.pi * np.clip(tt / 1.4, 0, 1)) ** 2) * (tt < 1.4)
saw = 2 * ((tt * 52) % 1) - 1
sn = lp(saw * breath, 650, 4) + lp(noise(3.2), 800, 4) * breath * 0.2
exh = (np.sin(np.pi * np.clip((tt - 1.7) / 1.2, 0, 1)) ** 2) * ((tt > 1.7) & (tt < 2.9))
sn += bp(noise(3.2), 300, 1600, 3) * exh * 0.3
write('mom_snore_1', sn, .45)

def tune(notes, tau=0.2):
    return np.concatenate([marimba([f], d, tau=tau) for f, d in notes])
write('win_you_1', tune([(523, .12), (659, .12), (784, .12), (1046, .7)], .3), .7)
write('win_other_1', tune([(392, .15), (523, .15), (659, .6)]), .6)
write('elim_you_1', room(seq([(220, .2), (208, .2), (196, .2), (185, .7)], reed), .12), .65)
write('elim_other_1', tune([(330, .14), (262, .5)]), .5)

# ---------------------------------------------------------------- pets
def bark(v):
    d = 0.22; tt = t(d); f = rng.uniform(380, 480) * (1 - 0.35 * tt / d)
    ph = 2 * np.pi * np.cumsum(f) / SR
    x = sum(np.sin(k * ph) / k for k in range(1, 7)) * np.exp(-tt / 0.07) + noise(d) * 0.3 * np.exp(-tt / 0.03)
    return room(bp(x, 300, 3500), 0.12)
for v in range(1, 4): write(f'dog_bark_{v}', bark(v), .75)
write('dog_huff_1', bp(noise(.35), 300, 1400) * np.sin(np.pi * t(.35) / .35), .35)
for v in range(1, 3):
    d = 0.6; tt = t(d); f = 520 + 260 * np.sin(np.pi * tt / d) * rng.uniform(.8, 1.1)
    ph = 2 * np.pi * np.cumsum(f) / SR; x = sum(np.sin(k * ph) / k ** 1.4 for k in range(1, 6))
    form = bp(x, 700 + 0, 3500) * np.sin(np.pi * tt / d) ** 0.8
    write(f'cat_meow_{v}', form, .55)

# ---------------------------------------------------------------- music: one quiet menu lullaby; night ambience
def celesta(f, d):
    tt = t(d); mod = np.sin(2 * np.pi * f * 3.5 * tt) * 1.4 * np.exp(-tt / 0.25)
    return np.sin(2 * np.pi * f * tt + mod) * np.exp(-tt / 0.9) * np.minimum(1, tt / 0.004)

beat = 0.5  # 120 bpm quarters, 3/4 lullaby
mel = [(698, 1), (880, 1), (1047, 1), (932, 2), (880, 1), (784, 1), (698, 1), (659, 1), (698, 3),
       (587, 1), (698, 1), (880, 1), (784, 2), (698, 1), (659, 1), (587, 1), (523, 1), (587, 3),
       (698, 1), (880, 1), (1047, 1), (1175, 2), (1047, 1), (932, 1), (880, 1), (784, 1), (880, 3),
       (784, 1), (698, 1), (659, 1), (698, 2), (587, 1), (523, 1), (587, 1), (659, 1), (698, 3)]
bars = sum(n for _, n in mel)
music = np.zeros(int(SR * beat * bars) + SR)
pos = 0
for f, n in mel:
    k = int(SR * beat * pos); x = celesta(f, beat * n + 0.8) * 0.5; music[k:k + len(x)] += x[: len(music) - k]; pos += n
roots = [175, 175, 147, 131, 175, 175, 147, 131]
for b in range(bars // 3):
    f = roots[b % len(roots)]; k = int(SR * beat * 3 * b)
    pz = modes([(f, 1, .35), (2 * f, .3, .2)], 1.2, 0.0) * 0.35; music[k:k + len(pz)] += pz[: len(music) - k]
    padx = lp(sum(np.sin(2 * np.pi * f * m * t(1.5)) for m in (2, 3, 4)) * np.sin(np.pi * t(1.5) / 1.5), 900) * 0.06
    music[k:k + len(padx)] += padx[: len(music) - k]
music = room(music[: int(SR * beat * bars)], 0.2)
write('music_menu_1', music, .4)

amb = lp(noise(12), 380, 4) * 0.25
tt = t(12)
for c in range(18):  # far crickets, sparse
    s = rng.uniform(0, 11.5); k = int(s * SR); chirp = np.sin(2 * np.pi * 4200 * t(0.18)) * (0.5 + 0.5 * np.sin(2 * np.pi * 45 * t(0.18))) * np.sin(np.pi * t(0.18) / 0.18) * 0.05
    amb[k:k + len(chirp)] += chirp[: len(amb) - k]
for s in range(12):  # soft clock tick in the hall
    k = int(SR * (s + 0.5)); tk = click(0.008, 2500, 0.08); amb[k:k + len(tk)] += tk
write('amb_night_1', amb, .25)

# ---------------------------------------------------------------- bot sabotage tell: a muffled "tee-hee" giggle (formant voice)
def vowel(f0, d, formants, breath=0.15):
    tt = t(d); f = f0 * (1 + 0.06 * np.sin(np.pi * tt / d)) * (1 + 0.01 * np.sin(2 * np.pi * 6 * tt))
    ph = np.cumsum(f) / SR; src = 2 * (ph % 1) - 1 + breath * noise(d)
    y = sum(a * bp(src, fc * 0.85, fc * 1.15) for fc, a in formants)
    return y * np.sin(np.pi * tt / d) ** 0.6
for v in range(1, 4):
    f0 = rng.uniform(420, 520)
    parts = [bp(noise(0.05), 3000, 7000) * env(0.05, 0.002, 0.02) * 0.3, vowel(f0 * 1.1, 0.11, [(350, 1), (2300, .5), (3000, .25)]), np.zeros(int(SR * 0.06)),
             bp(noise(0.06), 1500, 5000) * env(0.06, 0.004, 0.03) * 0.4, vowel(f0, 0.16, [(330, 1), (2400, .45), (3100, .2)])]
    write(f'bot_giggle_{v}', room(lp(np.concatenate(parts), 6000), 0.08), .5)

with open(os.path.join(OUT, '..', 'AUDIO_V6_REPORT.txt'), 'w') as f:  # copy to Docs/ (kept out of Resources)
    f.write('key\tseconds\trms\n')
    for n, d, r in REPORT: f.write(f'{n}\t{d:.2f}\t{r:.3f}\n')
print(f'{len(REPORT)} files -> {OUT}')
