"""
Procedural, original SFX/music generator for SHHH! (no third-party samples -> no licensing risk).
Writes 22.05 kHz mono 16-bit WAVs into UnityProject/Assets/SHHH/Resources/Audio/.
Material impacts use modal synthesis (damped inharmonic partials + noise transient) with per-variant randomisation.
Mom's voice is a cartoon formant "babble" (placeholder until licensed English VO is recorded/generated) - subtitles carry the English lines.
"""
import os, sys, wave
import numpy as np

SR = 22050
OUT = sys.argv[1] if len(sys.argv) > 1 else 'UnityProject/Assets/SHHH/Resources/Audio'
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(1105)

def t(d): return np.arange(int(SR * d)) / SR

def write(name, x, peak=0.89):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    fade = min(len(x), int(0.004 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())

def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR); y = np.zeros_like(x); acc = 0.0
    for i, v in enumerate(x): acc = (1 - a) * v + a * acc; y[i] = acc
    return y

def highpass(x, cutoff): return x - lowpass(x, cutoff)

def noise(d): return rng.standard_normal(int(SR * d))

def env_exp(d, tau): return np.exp(-t(d) / tau)

def modal(partials, d, transient=0.3, tcut=6000):
    x = np.zeros(int(SR * d)); tt = t(d)
    for f, a, tau in partials:
        f *= rng.uniform(0.97, 1.03)
        x += a * np.sin(2 * np.pi * f * tt + rng.uniform(0, 6.28)) * np.exp(-tt / tau)
    n = lowpass(noise(d), tcut) * env_exp(d, 0.006) * transient
    return x + n

MATERIALS = {
    'ceramic': ([(1850, 1, .18), (2760, .7, .14), (4120, .5, .1), (5600, .3, .07)], .5, .9),
    'glass':   ([(2500, 1, .5), (3920, .7, .38), (5230, .5, .3), (7050, .3, .2)], .4, 1.2),
    'metal':   ([(320, 1, 1.0), (845, .8, .8), (1350, .6, .6), (2210, .45, .5), (3480, .3, .35)], .4, 1.6),
    'wood':    ([(190, 1, .07), (430, .6, .05), (790, .4, .04)], 1.2, .35),
    'plastic': ([(620, 1, .05), (1310, .6, .04), (2150, .4, .03)], .8, .3),
    'toy':     ([(880, 1, .05), (1760, .5, .04), (2900, .3, .03)], .9, .3),
    'appliance': ([(140, 1, .2), (410, .6, .15)], .9, .5),
}

for mat, (parts, tr, dur) in MATERIALS.items():
    for v in range(1, 5):
        x = modal(parts, dur, transient=tr)
        if mat == 'toy':  # tiny rattle
            for k in range(3): s = int(SR * (0.03 + k * 0.035)); x[s:s + 400] += modal([(1200 + k * 300, .4, .01)], 400 / SR, 0.3)[:len(x[s:s + 400])]
        write(f'impact_{mat}_{v}', x)

for mat, cut, dur in [('soft', 300, .12), ('fabric', 700, .14), ('paper', 2500, .16)]:
    for v in range(1, 5):
        n = lowpass(noise(dur), cut * rng.uniform(.8, 1.2)) * env_exp(dur, dur / 4)
        if mat == 'paper': n += highpass(noise(dur), 3000) * env_exp(dur, .02) * .4
        write(f'impact_{mat}_{v}', n, .6 if mat == 'soft' else .75)

# Rubber squeak toy
for v in range(1, 4):
    d = .28; tt = t(d)
    f = 950 + 500 * np.sin(np.pi * tt / d) * rng.uniform(.8, 1.2)
    ph = 2 * np.pi * np.cumsum(f) / SR
    x = (np.sin(ph) + .5 * np.sin(2 * ph) + .25 * np.sin(3 * ph)) * np.sin(np.pi * tt / d) ** .6
    write(f'squeak_{v}', x, .8)

# UI
write('ui_click_1', modal([(2200, 1, .02), (4400, .3, .01)], .06, .2), .6)
write('ui_hover_1', np.sin(2 * np.pi * 1600 * t(.04)) * env_exp(.04, .01), .25)
def bell(freqs, d=.6, tau=.25):
    x = np.zeros(int(SR * d)); tt = t(d)
    for i, f in enumerate(freqs):
        s = int(i * .09 * SR)
        seg = (np.sin(2 * np.pi * f * tt) + .3 * np.sin(2 * np.pi * f * 2.76 * tt)) * np.exp(-tt / tau)
        x[s:] += seg[:len(x) - s]
    return x
write('step_done_1', bell([1046]), .45)
write('chore_done_1', bell([784, 1046, 1318], .9), .6)
write('grab_1', lowpass(noise(.08), 1500) * np.sin(np.pi * t(.08) / .08), .25)
write('trap_set_1', bell([392, 370], .5, .12), .4)
write('countdown_1', np.concatenate([np.concatenate([np.sin(2 * np.pi * 880 * t(.12)) * env_exp(.12, .05), np.zeros(int(.88 * SR))]) for _ in range(3)] + [np.sin(2 * np.pi * 1320 * t(.35)) * env_exp(.35, .15)]), .5)

# Mom: snore loop, babble (cartoon gibberish voice), stinger
d = 4.0; tt = t(d)
breath = np.clip(np.sin(2 * np.pi * tt / d - .5), 0, 1) ** 2
snore = lowpass(noise(d), 260) * (0.3 + 0.7 * breath) * (1 + .5 * np.sin(2 * np.pi * 38 * tt))
write('mom_snore_1', snore, .5)

def babble(dur, pitch, seed):
    r = np.random.default_rng(seed); x = []
    vowels = [(800, 1200), (400, 2000), (300, 870), (500, 1700), (650, 1080)]
    tleft = dur
    while tleft > 0:
        sd = r.uniform(.08, .18); tt = t(sd)
        p = pitch * r.uniform(.85, 1.25) * (1 + .06 * np.sin(2 * np.pi * 6 * tt))
        src = np.sign(np.sin(2 * np.pi * np.cumsum(p) / SR)) * .5 + .5 * np.sin(2 * np.pi * np.cumsum(p) / SR)
        f1, f2 = vowels[r.integers(len(vowels))]
        y = np.zeros_like(src)
        for f, bw in [(f1, 90), (f2, 120)]:
            rr = np.exp(-np.pi * bw / SR); w0 = 2 * np.pi * f / SR
            a1, a2 = 2 * rr * np.cos(w0), -rr * rr; y1 = y2 = 0.0; out = np.zeros_like(src)
            for i, s in enumerate(src): v = s + a1 * y1 + a2 * y2; out[i] = v; y2, y1 = y1, v
            y += out
        y *= np.sin(np.pi * tt / sd) ** .5
        x.append(y); x.append(np.zeros(int(r.uniform(.01, .05) * SR))); tleft -= sd
    return np.concatenate(x)
for v in range(1, 5): write(f'mom_babble_{v}', babble(1.4 + .3 * v, 230, v), .7)
def stab(f, d): tt = t(d); s = sum(np.sign(np.sin(2 * np.pi * f * k * tt)) / k for k in (1, 2, 3)); return lowpass(s, 2500) * env_exp(d, d / 2.5)
write('mom_wake_stinger_1', np.concatenate([stab(196, .18), np.zeros(800), stab(185, .18), np.zeros(800), stab(147, .7)]), .8)

# Door
d = .9; tt = t(d); f = 120 + 60 * np.sin(2 * np.pi * 1.3 * tt) + 40 * rng.standard_normal(len(tt)).cumsum() / 300
creak = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * lowpass(np.abs(noise(d)), 50) * np.sin(np.pi * tt / d)
write('door_creak_1', highpass(creak, 200), .5)
write('door_close_soft_1', modal([(110, 1, .12), (260, .5, .08)], .4, .6), .45)

# Pets
def bark(seed):
    r = np.random.default_rng(seed); parts = []
    for k in range(r.integers(1, 3)):
        d = r.uniform(.12, .18); tt = t(d)
        p = 420 * r.uniform(.9, 1.1) * (1 - .4 * tt / d)
        src = np.sign(np.sin(2 * np.pi * np.cumsum(p) / SR)) + .6 * lowpass(noise(d), 3000)
        parts.append(lowpass(src, 1800) * np.sin(np.pi * tt / d) ** .4); parts.append(np.zeros(int(.09 * SR)))
    return np.concatenate(parts)
for v in range(1, 4): write(f'dog_bark_{v}', bark(v), .8)
write('dog_huff_1', lowpass(noise(.3), 900) * np.sin(np.pi * t(.3) / .3), .4)
for v in range(1, 3):
    d = .55; tt = t(d); p = 520 + 380 * np.sin(np.pi * tt / d) * (1 + .1 * v)
    ph = 2 * np.pi * np.cumsum(p) / SR
    m = (np.sin(ph) + .4 * np.sin(2 * ph) + .2 * np.sin(3 * ph)) * np.sin(np.pi * tt / d) ** .7
    write(f'cat_meow_{v}', lowpass(m, 3500), .6)

# Ambience: crickets + faint wind + clock tick (seamless 8 s loop)
d = 8.0; tt = t(d)
amb = lowpass(noise(d), 400) * .08
for k in range(10):
    c0 = rng.uniform(0, d); f = rng.uniform(4200, 5200)
    for j in range(rng.integers(3, 6)):
        s = int(((c0 + j * .09) % d) * SR); n = int(.05 * SR)
        seg = np.sin(2 * np.pi * f * np.arange(n) / SR) * np.sin(np.pi * np.arange(n) / n) * .15
        e = min(len(amb), s + n); amb[s:e] += seg[:e - s]
for k in range(8): s = int(k * SR); amb[s:s + 200] += modal([(2500, .2, .004)], 200 / SR, .1)[:200]
write('amb_night_1', amb, .35)

# Music box lullaby (original melody), loopable ~13.7 s
notes = [72, 76, 79, 76, 74, 77, 81, 77, 72, 76, 79, 84, 83, 79, 74, 79, 76, 72, 74, 71, 72, None, 67, None]
beat = .57; music = np.zeros(int(SR * beat * len(notes)) + SR)
for i, n in enumerate(notes):
    if n is None: continue
    f = 440 * 2 ** ((n - 69) / 12); s = int(i * beat * SR); tt = t(1.4)
    tone = (np.sin(2 * np.pi * f * tt) + .35 * np.sin(2 * np.pi * f * 3.98 * tt) + .15 * np.sin(2 * np.pi * f * 6.1 * tt)) * np.exp(-tt / .45)
    music[s:s + len(tone)] += tone[:len(music) - s]
    if i % 4 == 0:
        b = 440 * 2 ** ((n - 12 - 69) / 12); bass = np.sin(2 * np.pi * b * tt) * np.exp(-tt / .8) * .5
        music[s:s + len(bass)] += bass[:len(music) - s]
music = music[:int(SR * beat * len(notes))]
write('music_menu_1', music, .5)

# Results / elimination stingers
def tone_seq(seq, wave_fn=np.sin, tau=.2):
    out = []
    for f, d in seq:
        tt = t(d); out.append(wave_fn(2 * np.pi * f * tt) * np.exp(-tt / tau) if f else np.zeros(len(tt)))
    return np.concatenate(out)
write('win_you_1', tone_seq([(523, .12), (659, .12), (784, .12), (1046, .5)], tau=.25), .7)
write('win_other_1', tone_seq([(392, .15), (523, .15), (659, .4)]), .6)
d = 1.6; tt = t(d)
seg_f = np.piecewise(tt, [tt < .35, (tt >= .35) & (tt < .7), tt >= .7], [233, 220, lambda x: 208 - 25 * (x - .7)])
wah = lowpass(np.sign(np.sin(2 * np.pi * np.cumsum(seg_f) / SR)), 1200) * (1 - .5 * np.abs(np.sin(2 * np.pi * 2.5 * tt))) * np.minimum(1, (d - tt) * 3)
write('elim_you_1', wah, .7)
write('elim_other_1', tone_seq([(330, .12), (262, .3)]), .5)
print('wrote', len(os.listdir(OUT)), 'files to', OUT)
