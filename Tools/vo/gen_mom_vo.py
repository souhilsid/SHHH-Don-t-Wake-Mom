"""Generate Mom's English VO with Piper TTS (voice: en_US-kristin-medium, LibriVox public-domain dataset, trained from scratch).
Usage: python3 gen_mom_vo.py <voice.onnx> <out_dir>
Per-category performance direction -> speaking rate / expressiveness, then ffmpeg warmth EQ + slight pitch character.
Lines containing {name}: the name is removed here; the game plays a separate vo_name_<name> clip first."""
import json, os, re, subprocess, sys, wave
from piper import PiperVoice, SynthesisConfig
model, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
voice = PiperVoice.load(model)
lines = json.load(open(os.path.join(os.path.dirname(__file__), 'mom_lines.json')))
# category -> (length_scale, noise_scale, noise_w, pitch_factor, tempo)
DIRECTION = {  # (length_scale, noise_scale, noise_w, pitch_factor, tempo) - higher noise = livelier intonation
 'stir': (1.45, 0.95, 1.1, 0.95, 0.90), 'suspicious': (1.25, 0.9, 1.0, 1.0, 0.97), 'almost': (1.0, 1.0, 1.1, 1.05, 1.03),
 'wake': (0.88, 1.1, 1.2, 1.10, 1.08), 'investigate': (1.2, 0.9, 1.0, 0.99, 0.98), 'caught': (0.95, 1.05, 1.15, 1.08, 1.05),
 'warning': (1.3, 0.85, 0.95, 0.97, 0.95), 'inspect': (1.15, 0.85, 0.95, 1.01, 1.0), 'lazy': (1.0, 1.0, 1.1, 1.05, 1.02),
 'tiebreak': (0.9, 1.0, 1.1, 1.08, 1.06), 'bed': (1.45, 0.9, 1.0, 0.94, 0.90), 'intro': (1.05, 0.9, 1.0, 1.02, 1.0),
 'win': (0.95, 1.0, 1.1, 1.07, 1.02), 'rush': (1.3, 0.9, 1.0, 0.96, 0.94), 'sabotage': (1.2, 0.9, 1.0, 0.99, 0.97),
}
# per-emotion colour: sleepy = soft & roomy, angry = forward & punchy, warm = cosy low-mid
EMOTION_FX = {
 'stir': 'volume=-4dB,lowpass=f=5200,aecho=0.6:0.5:40:0.18', 'bed': 'volume=-4dB,lowpass=f=5200,aecho=0.6:0.5:40:0.18',
 'warning': 'volume=-2dB,aecho=0.6:0.4:30:0.12', 'rush': 'volume=-3dB,aecho=0.6:0.4:35:0.15',
 'wake': 'equalizer=f=2600:t=q:w=1:g=4,asoftclip=type=tanh,volume=2dB', 'caught': 'equalizer=f=2400:t=q:w=1:g=3,asoftclip=type=tanh,volume=1dB',
 'almost': 'equalizer=f=2400:t=q:w=1:g=2', 'tiebreak': 'equalizer=f=2400:t=q:w=1:g=2', 'lazy': 'equalizer=f=2200:t=q:w=1:g=2',
}
def cat(k):
    for c in DIRECTION:
        if c in k: return c
    return 'intro'
def clean(t):
    t = re.sub(r'\*[^*]*\*', ' ', t).replace('{name}', '')
    t = re.sub(r'^[\s,!.?-]+', '', t); t = re.sub(r'\s+([,!.?])', r'\1', t); t = re.sub(r'\s{2,}', ' ', t).strip()
    t = t.replace(' - ', ', ')
    return t
def synth(text, path, d):
    ls, ns, nw, pitch, tempo = d
    tmp = path + '.raw.wav'
    with wave.open(tmp, 'wb') as w:
        voice.synthesize_wav(text, w, syn_config=SynthesisConfig(length_scale=ls, noise_scale=ns, noise_w_scale=nw))
    sr = 22050
    fx = EMOTION_FX.get(CUR_CAT, 'anull')
    af = f"asetrate={int(sr*pitch)},aresample=44100,atempo={tempo/pitch:.4f},{fx},highpass=f=90,equalizer=f=220:t=q:w=1.0:g=2.5,equalizer=f=3200:t=q:w=1.2:g=1.5,acompressor=threshold=-18dB:ratio=3:attack=5:release=80,loudnorm=I=-18:TP=-2:LRA=9,afade=t=in:d=0.02,areverse,silenceremove=start_periods=1:start_threshold=-50dB,areverse,silenceremove=start_periods=1:start_threshold=-50dB"
    subprocess.run(['ffmpeg','-loglevel','error','-y','-i',tmp,'-af',af,'-ac','1','-ar','44100',path],check=True)
    os.remove(tmp)
report = []
CUR_CAT = 'intro'
NAMES_ONLY = '--names-only' in sys.argv
for k, t in ([] if NAMES_ONLY else lines.items()):
    text = clean(t)
    if not text: continue
    CUR_CAT = cat(k)
    p = os.path.join(out, f'vo_{k}_1.wav'); synth(text, p, DIRECTION[CUR_CAT]); report.append((k, CUR_CAT, text))
CUR_CAT = 'caught'
for n in ["Pip","Nell","Juno","Rory","Tess","Milo","Wren","Ozzie","Bea"]:  # keep in sync with Core Roster bot names
    synth(n + '!', os.path.join(out, f'vo_name_{n.lower()}_1.wav'), DIRECTION['caught'])
if not NAMES_ONLY: json.dump(report, open(os.path.join(out, 'VO_MANIFEST.json'), 'w'), indent=1)
print(len(report), 'lines + 9 names')
