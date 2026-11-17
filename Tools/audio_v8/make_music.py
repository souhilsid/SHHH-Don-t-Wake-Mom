#!/usr/bin/env python3
"""
SHHH! V8 original adaptive score (2026-11-09). Composed here note by note (no samples of existing music), written as MIDI with mido,
rendered with FluidSynth + FluidR3_GM (MIT soundfont), finished with sox. Usage: make_music.py <out_dir>

Match loop: D minor/dorian "tiptoe" theme, 96 bpm, 16 bars (40 s), rendered as three STEM LAYERS of identical length that the game
mixes by Mom's sleep meter (calm -> tension -> danger) + a faster final-minute loop. Menu: 3/4 lullaby. Stingers: Mom wakes, win, lose,
countdown, penalty, inspection.
"""
import os, subprocess, sys
import mido

OUT = sys.argv[1] if len(sys.argv) > 1 else "music_out"; os.makedirs(OUT, exist_ok=True)
SF2 = "/usr/share/sounds/sf2/FluidR3_GM.sf2"
TPB = 480

N = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}
def p(name):  # "D4" -> midi
    nm, oc = (name[:-1], int(name[-1])); return 12 * (oc + 1) + N[nm]

class Song:
    def __init__(self, bpm, beats_per_bar=4):
        self.bpm, self.bpb = bpm, beats_per_bar; self.tracks = {}
    def note(self, ch, prog, beat, dur, pitch, vel):
        tr = self.tracks.setdefault(ch, {"prog": prog, "ev": []})
        t0 = int(beat * TPB); t1 = int((beat + dur) * TPB)
        tr["ev"].append((t0, 1, pitch, vel)); tr["ev"].append((t1, 0, pitch, 0))
    def cc(self, ch, beat, ctl, val):
        self.tracks.setdefault(ch, {"prog": 0, "ev": []})["ev"].append((int(beat * TPB), 2, ctl, val))
    def save(self, path, total_beats):
        mid = mido.MidiFile(ticks_per_beat=TPB)
        meta = mido.MidiTrack(); mid.tracks.append(meta)
        meta.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(self.bpm), time=0))
        meta.append(mido.MetaMessage("time_signature", numerator=self.bpb, denominator=4, time=0))
        meta.append(mido.MetaMessage("end_of_track", time=int(total_beats * TPB)))
        for ch, tr in sorted(self.tracks.items()):
            t = mido.MidiTrack(); mid.tracks.append(t)
            if ch != 9: t.append(mido.Message("program_change", channel=ch, program=tr["prog"], time=0))
            t.append(mido.Message("control_change", channel=ch, control=91, value=50, time=0))  # reverb send
            last = 0
            for (tick, kind, a, b) in sorted(tr["ev"], key=lambda e: (e[0], e[1])):
                dt = tick - last; last = tick
                if kind == 1: t.append(mido.Message("note_on", channel=ch, note=a, velocity=b, time=dt))
                elif kind == 0: t.append(mido.Message("note_off", channel=ch, note=a, velocity=0, time=dt))
                else: t.append(mido.Message("control_change", channel=ch, control=a, value=b, time=dt))
            t.append(mido.MetaMessage("end_of_track", time=max(0, int(total_beats * TPB) - last)))
        mid.save(path)

def render(mid, wav, seconds, gain=0, reverb=True, fade_in=0.0, fade_out=0.0):
    raw = wav + ".raw.wav"
    subprocess.run(["fluidsynth", "-ni", "-g", "0.6", "-r", "44100", "-F", raw, SF2, mid], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    fx = ["trim", "0", f"{seconds:.3f}"]
    if reverb: fx += ["reverb", "28", "50", "70", "100", "12"]
    fx += ["fade", "t", f"{fade_in:.3f}", f"{seconds:.3f}", f"{fade_out:.3f}", "gain", str(gain), "norm", "-3"]
    subprocess.run(["sox", raw, "-r", "44100", "-c", "2", wav] + fx, check=True); os.remove(raw)

def loop_render(song, name, beats, layer_gain=0):
    """Loops: render 1 extra bar so reverb tails exist, then fold the tail onto the head for a seamless loop."""
    mid = os.path.join(OUT, name + ".mid"); song.save(mid, beats + song.bpb)
    secs = beats * 60.0 / song.bpm; raw = os.path.join(OUT, name + "_long.wav")
    subprocess.run(["fluidsynth", "-ni", "-g", "0.6", "-r", "44100", "-F", raw + ".fs.wav", SF2, mid], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["sox", raw + ".fs.wav", "-r", "44100", "-c", "2", raw, "reverb", "25", "50", "70", "100", "10"], check=True); os.remove(raw + ".fs.wav")
    head = os.path.join(OUT, name + "_head.wav"); tail = os.path.join(OUT, name + "_tail.wav"); dst = os.path.join(OUT, name + ".wav")
    subprocess.run(["sox", raw, head, "trim", "0", f"{secs:.4f}"], check=True)
    subprocess.run(["sox", raw, tail, "trim", f"{secs:.4f}", f"{60.0 / song.bpm * song.bpb:.4f}", "pad", "0", f"{secs - 60.0 / song.bpm * song.bpb:.4f}"], check=True)
    subprocess.run(["sox", "-m", head, tail, dst, "gain", str(layer_gain), "norm", "-3"], check=True)
    for f in (raw, head, tail): os.remove(f)
    return dst

# ------------------------------------------------------------------ the match theme (16 bars, Dm - Bb - C - A | Dm - Gm - Bb - A ...)
PROG = [("D", "min"), ("A#", "maj"), ("C", "maj"), ("A", "maj"), ("D", "min"), ("G", "min"), ("A#", "maj"), ("A", "maj"),
        ("D", "min"), ("A#", "maj"), ("F", "maj"), ("C", "maj"), ("G", "min"), ("D", "min"), ("E", "dim"), ("A", "maj")]
def triad(root, kind, octave):
    r = p(root + str(octave)); third = 3 if kind in ("min", "dim") else 4; fifth = 6 if kind == "dim" else 7
    return [r, r + third, r + fifth]
# tiptoe melody (scale degrees in D dorian/minor), one phrase per 4 bars, rests are None
MEL = [["A4", None, "D5", None, "C5", "A4", None, "F4", "G4", None, "A4", None, None, None, "E4", None],
       ["F4", None, "A4", None, "G4", "F4", None, "D4", "E4", "F4", "G4", None, "A4", None, None, None],
       ["D5", None, "C5", "A#4", "A4", None, "F4", None, "G4", "A4", "A#4", None, "C5", None, "A4", None],
       ["G4", None, "F4", "E4", "D4", None, None, "A3", "D4", None, "E4", "F4", "E4", None, "C#4", None]]

def match_layers(bpm, tag, final=False):
    bars = 16; beats = bars * 4
    calm, tense, danger = Song(bpm), Song(bpm), Song(bpm)
    for b, (root, kind) in enumerate(PROG):
        t = b * 4; ch = triad(root, kind, 3)
        # CALM: pizzicato bass tiptoe (root - fifth - octave - fifth on 8ths, staccato), soft string pad, celesta melody
        for k, off in enumerate([0, 1, 2, 3]):
            nn = [ch[0] - 12, ch[2] - 12, ch[0], ch[2] - 12][k]
            calm.note(0, 45, t + off, 0.35, nn, 78 if k == 0 else 64)
            calm.note(0, 45, t + off + 0.5, 0.25, nn + (12 if k % 2 else 0), 46)
        for nn in ch: calm.note(1, 48, t, 4, nn + 12, 34)
        phrase = MEL[(b // 4) % 4]; part = (b % 4) * 4
        for i in range(4):
            m = phrase[part + i]
            if m: calm.note(2, 8, t + i, 0.9, p(m) + 12, 72)
        # TENSION: clarinet staccato counter-line (chord tones, offbeats), woodblock tiptoes, bassoon on beat 1
        for i, off in enumerate([0.5, 1.5, 2.5, 3.5]):
            tense.note(3, 71, t + off, 0.2, ch[(i + b) % 3] + 12, 70)
        for off in [0.75, 1.75, 2.75, 3.75]: tense.note(9, 0, t + off, 0.1, 76 if off < 2 else 77, 64)
        tense.note(4, 70, t, 1.5, ch[0] - 12, 70)
        if b % 2 == 1: tense.note(4, 70, t + 2, 1.5, ch[2] - 12, 60)
        # DANGER: tremolo strings, timpani pulse, horn swell every 2 bars
        for nn in ch: danger.note(5, 44, t, 4, nn, 58 + (b % 4) * 6)
        danger.note(6, 47, t, 0.5, p(root + "2") if root not in ("A#", "A") else p(root + "1"), 96)
        danger.note(6, 47, t + 2, 0.5, ch[2] - 24, 80)
        if b % 2 == 0: danger.note(7, 60, t + 0.5, 3.5, ch[0], 64)
        if final:   # final minute: driving brush snare + xylophone doubling
            for off in [1, 3]: danger.note(9, 0, t + off, 0.1, 38, 70)
            for off in [0, 0.5, 1, 1.5, 2, 2.5, 3, 3.5]: tense.note(9, 0, t + off, 0.05, 42, 50)
            for i in range(4):
                m = phrase[part + i]
                if m: calm.note(8, 13, t + i, 0.4, p(m) + 12, 76)
    # endings are folded by loop_render (seamless)
    paths = [loop_render(calm, f"music_{tag}_calm", beats), loop_render(tense, f"music_{tag}_tension", beats), loop_render(danger, f"music_{tag}_danger", beats)]
    return paths

# ------------------------------------------------------------------ menu lullaby (3/4, 84 bpm, 16 bars)
def menu():
    s = Song(84, 3); bars = 16
    prog = [("D", "min"), ("D", "min"), ("A#", "maj"), ("A#", "maj"), ("G", "min"), ("G", "min"), ("A", "maj"), ("A", "maj"),
            ("F", "maj"), ("C", "maj"), ("D", "min"), ("A#", "maj"), ("G", "min"), ("A", "maj"), ("D", "min"), ("A", "maj")]
    mel = ["A4", "D5", "F5", "E5", "D5", "C5", "D5", "A#4", "G4", "A4", "F4", "E4", "F4", "G4", "A4", "C#5",
           "D5", "C5", "A#4", "A4", "G4", "A4", "F4", "E4", "D4", "F4", "A4", "G4", "F4", "E4", "D4", "E4",
           "F4", "G4", "A4", "C5", "A#4", "A4", "G4", "F4", "E4", "D4", "E4", "F4", "E4", "C#4", "D4", "A3"]
    for b, (root, kind) in enumerate(prog):
        t = b * 3; ch = triad(root, kind, 3)
        s.note(0, 46, t, 1, ch[0] - 12, 70); s.note(0, 46, t + 1, 1, ch[1], 50); s.note(0, 46, t + 2, 1, ch[2], 50)   # harp waltz
        for nn in ch: s.note(1, 48, t, 3, nn + 12, 30)
        for i in range(3):
            idx = b * 3 + i
            if idx < len(mel) and (i == 0 or (b % 2 == 0 and i == 2) or i == 1 and b % 4 == 1): s.note(2, 10, t + i, 1.2, p(mel[idx]) + 12, 78)
    return loop_render(s, "music_menu", bars * 3)

# ------------------------------------------------------------------ stingers
def stinger(name, build, beats, bpm=100, seconds=None):
    s = Song(bpm); build(s); mid = os.path.join(OUT, name + ".mid"); s.save(mid, beats + 2)
    render(mid, os.path.join(OUT, name + ".wav"), seconds or (beats + 2) * 60.0 / bpm, fade_out=0.4)
def wake(s):      # Mom wakes: falling brass + timpani roll + cymbal
    for i, nn in enumerate(["A4", "G#4", "G4", "F#4"]): s.note(0, 61, i * 0.5, 0.45, p(nn), 100 - i * 5)
    s.note(0, 61, 2, 2.5, p("D4"), 110); s.note(0, 61, 2, 2.5, p("F4"), 100); s.note(0, 61, 2, 2.5, p("G#3"), 100)
    for k in range(16): s.note(1, 47, k * 0.125, 0.12, p("D2"), 60 + k * 3)
    s.note(9, 0, 2, 2, 49, 110)
def win(s):       # bright fanfare
    for i, nn in enumerate(["D5", "F#5", "A5", "D6"]): s.note(0, 13, i * 0.25, 0.25, p(nn), 100)
    for nn in ["D4", "F#4", "A4", "D5"]: s.note(1, 61, 1, 2.5, p(nn), 105)
    s.note(9, 0, 1, 2, 49, 110); s.note(9, 0, 1, 0.2, 81, 90)
def lose(s):      # gentle "aww"
    for i, nn in enumerate(["A3", "G3", "F3", "E3"]): s.note(0, 70, i * 0.5, 0.5, p(nn), 80)
    s.note(1, 46, 2, 2, p("D3"), 70); s.note(1, 46, 2, 2, p("F3"), 60)
def countdown(s): # three tiptoe ticks + chime
    for i in range(3): s.note(9, 0, i, 0.1, 76, 100)
    s.note(0, 9, 3, 1.5, p("D6"), 100); s.note(0, 9, 3, 1.5, p("A5"), 90)
def penalty(s):   # caught: sliding bassoon + slap
    for i, nn in enumerate(["D3", "C#3", "C3", "B2"]): s.note(0, 70, i * 0.2, 0.2, p(nn), 95)
    s.note(9, 0, 0.9, 0.2, 39, 100)
def caught_other(s):  # someone else got caught: two sly pizzicato plucks + woodblock
    s.note(0, 45, 0, 0.3, p("A3"), 90); s.note(0, 45, 0.5, 0.3, p("D3"), 95); s.note(9, 0, 1.0, 0.1, 76, 90)
def inspect(s):   # final inspection: snare roll + horn call
    for k in range(12): s.note(9, 0, k * 0.125, 0.1, 38, 50 + k * 4)
    s.note(0, 60, 1.5, 1.5, p("A3"), 95); s.note(0, 60, 1.5, 1.5, p("D4"), 95)

if __name__ == "__main__":
    out = []
    out += match_layers(96, "match")
    out += match_layers(120, "final", final=True)
    out.append(menu())
    stinger("mom_wake_stinger", wake, 5); stinger("win_you", win, 4, 110); stinger("win_other", lose, 4, 90)
    stinger("countdown", countdown, 4, 100); stinger("elim_you", penalty, 2, 120); stinger("elim_other", caught_other, 2, 110); stinger("inspect_stinger", inspect, 3, 110)
    for f in os.listdir(OUT):
        if f.endswith(".mid"): os.remove(os.path.join(OUT, f))
    print("music:", sorted(os.listdir(OUT)))
