"""Mix the SHHH! trailer audio from trailer_timeline.json -> 06_SoundDesign/stems/trailer_{music,voice,sfx}.wav + trailer_master.wav.

music  : licensed Kevin MacLeod tracks (04_MusicAndLicenses), placed/cut/faded per the timeline, ducked under the voice stem
voice  : narrator (Kokoro TTS, trailer only) + Mom lines from the game's own VO bank
sfx    : the game's own sound effects, synced to the on-screen events they belong to
master : music(ducked) + voice + sfx -> two-pass EBU R128 loudnorm to -14 LUFS integrated, true peak -1.5 dBTP, 48 kHz stereo.
"""
import json, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
VP = os.path.abspath(os.path.join(HERE, ".."))
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
OUT = os.path.join(VP, "06_SoundDesign", "stems")
os.makedirs(OUT, exist_ok=True)
tl = json.load(open(os.path.join(HERE, "trailer_timeline.json")))
DUR = tl["durationInFrames"] / tl["fps"]


def run(args):
    r = subprocess.run([FF, "-hide_banner", "-y", *args], capture_output=True, text=True)
    if r.returncode: print(r.stderr[-3000:]); sys.exit(1)
    return r.stderr


def place(events, name, music=False):
    """Each event -> resampled, gained, delayed to its start; amix (normalize=0) into one stem of exactly DUR seconds."""
    ins, fl = [], []
    for k, e in enumerate(events):
        if music:
            ins += ["-ss", str(e["srcIn"]), "-t", str(e["dur"]), "-i", e["file"]]
            fo_st = max(0, e["dur"] - e["fadeOut"])
            chain = (f"aresample=48000,aformat=channel_layouts=stereo,afade=t=in:d={e['fadeIn']},afade=t=out:st={fo_st:.3f}:d={e['fadeOut']},"
                     f"volume={e['gainDb']}dB,adelay={int(e['at'] * 1000)}:all=1")
        else:
            ins += ["-i", e["file"]]
            chain = f"aresample=48000,aformat=channel_layouts=stereo,volume={e['gainDb']}dB,adelay={int(e['at'] * 1000)}:all=1"
        fl.append(f"[{k}:a]{chain}[a{k}]")
    mix = "".join(f"[a{k}]" for k in range(len(events)))
    fl.append(f"{mix}amix=inputs={len(events)}:normalize=0:dropout_transition=0,apad,atrim=0:{DUR:.3f}[out]")
    path = os.path.join(OUT, f"trailer_{name}.wav")
    run([*ins, "-filter_complex", ";".join(fl), "-map", "[out]", "-c:a", "pcm_s24le", "-ar", "48000", path])
    return path


music = place(tl["music"], "music", music=True)
voice = place(tl["voice"], "voice")
sfx = place(tl["sfx"], "sfx")

# music ducks ~7 dB under any voice (narrator or Mom); voice and sfx summed on top
pre = os.path.join(OUT, "trailer_premaster.wav")
run(["-i", music, "-i", voice, "-i", sfx, "-filter_complex",
     "[1:a]asplit=2[v1][vsc];[0:a][vsc]sidechaincompress=threshold=0.02:ratio=5:attack=25:release=420:makeup=1[md];"
     "[md][v1][2:a]amix=inputs=3:normalize=0[m]", "-map", "[m]", "-c:a", "pcm_s24le", pre])

# two-pass loudness normalisation
meas = run(["-i", pre, "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"])
j = json.loads(re.search(r"\{[^{}]*\"input_i\"[^{}]*\}", meas, re.S).group(0))
master = os.path.join(OUT, "trailer_master.wav")
run(["-i", pre, "-af", f"loudnorm=I=-14:TP=-1.5:LRA=11:measured_I={j['input_i']}:measured_TP={j['input_tp']}:measured_LRA={j['input_lra']}:"
     f"measured_thresh={j['input_thresh']}:offset={j['target_offset']}:linear=true,aresample=48000", "-c:a", "pcm_s24le", "-ar", "48000", master])
print("premaster measured:", j["input_i"], "LUFS,", j["input_tp"], "dBTP ->", master)

# captions sidecar (never burnt in)
def tc(s):
    h, s = divmod(s, 3600); m, s = divmod(s, 60); return f"{int(h):02d}:{int(m):02d}:{int(s):02d},{int(round((s % 1) * 1000)):03d}"
import soundfile as sf
srt, n = [], 0
for e in sorted(tl["voice"], key=lambda e: e["at"]):
    if not e["caption"]: continue
    n += 1; d = sf.info(e["file"]).duration
    srt.append(f"{n}\n{tc(e['at'])} --> {tc(e['at'] + d + 0.25)}\n{e['caption']}\n")
p = os.path.join(VP, "09_FinalExports", "SHHH_Official_Game_Trailer_1080p.en.srt"); os.makedirs(os.path.dirname(p), exist_ok=True)
open(p, "w", encoding="utf-8").write("\n".join(srt)); print("captions:", n, "->", p)
