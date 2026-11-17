"""Mix the Meta VR Start 2026 demo audio from ../Remotion/demo_timeline.json.

stems/demo_music.wav  licensed Kevin MacLeod tracks (CC BY 4.0) placed per the timeline
stems/demo_voice.wav  narrator (Kokoro TTS, made for this video) + Mom lines from the game's own VO bank
stems/demo_sfx.wav    the game's own sound effects on the frames where the actions happen + 3 editorial whooshes/sparkle
demo_master.wav       music ducked ~8 dB under voice (sidechain) + voice + sfx -> fixed gain to -14.5 LUFS -> 4x oversampled
                      peak limiter at -2 dBFS (true peak stays below -1 dBTP) -> 48 kHz stereo
Also writes the caption sidecar ../Final/SHHH_Meta_VR_Start_2026_Official_Demo.en.srt (never burnt in).
Run: ../../Tools/tts_venv/Scripts/python -I mix_demo_audio.py
"""
import json, os, re, subprocess, sys
import soundfile as sf

HERE = os.path.dirname(os.path.abspath(__file__))
DEMO = os.path.abspath(os.path.join(HERE, ".."))
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
OUT = os.path.join(HERE, "stems"); os.makedirs(OUT, exist_ok=True)
tl = json.load(open(os.path.join(DEMO, "Remotion", "demo_timeline.json")))
DUR = tl["durationInFrames"] / tl["fps"]


def run(args):
    r = subprocess.run([FF, "-hide_banner", "-nostdin", "-y", *args], capture_output=True, text=True, stdin=subprocess.DEVNULL)
    if r.returncode: print(r.stderr[-3000:]); sys.exit(1)
    return r.stderr


def place(events, name, music=False):
    ins, fl = [], []
    for k, e in enumerate(events):
        if music:
            ins += ["-ss", str(e["srcIn"]), "-t", str(e["dur"]), "-i", e["file"]]
            st = max(0, e["dur"] - e["fadeOut"])
            ch = (f"aresample=48000,aformat=channel_layouts=stereo,afade=t=in:d={e['fadeIn']},afade=t=out:st={st:.3f}:d={e['fadeOut']},"
                  f"volume={e['gainDb']}dB,adelay={int(e['at'] * 1000)}:all=1")
        else:
            ins += ["-i", e["file"]]
            ch = f"aresample=48000,aformat=channel_layouts=stereo,volume={e['gainDb']}dB,adelay={int(e['at'] * 1000)}:all=1"
        fl.append(f"[{k}:a]{ch}[a{k}]")
    # a silent bed of exactly DUR seconds is input #n and amix ends with it (duration=first) - apad+atrim after a 60-input amix hung
    n = len(events); ins += ["-f", "lavfi", "-t", f"{DUR:.3f}", "-i", "anullsrc=r=48000:cl=stereo"]
    fl.append(f"[{n}:a]" + "".join(f"[a{k}]" for k in range(n)) + f"amix=inputs={n + 1}:normalize=0:duration=first:dropout_transition=0[out]")
    p = os.path.join(OUT, f"demo_{name}.wav")
    run([*ins, "-filter_complex", ";".join(fl), "-map", "[out]", "-c:a", "pcm_s24le", "-ar", "48000", p])
    return p


music, voice, sfx = place(tl["music"], "music", True), place(tl["voice"], "voice"), place(tl["sfx"], "sfx")
pre = os.path.join(OUT, "demo_premaster.wav")
# voice is read twice (key input + mix input) instead of asplit: asplit feeding sidechaincompress and amix deadlocked here
run(["-i", music, "-i", voice, "-i", voice, "-i", sfx, "-filter_complex",
     "[0:a][1:a]sidechaincompress=threshold=0.015:ratio=6:attack=20:release=450:makeup=1[md];"
     f"[md][2:a][3:a]amix=inputs=3:normalize=0:duration=longest,atrim=0:{DUR:.3f}[m]", "-map", "[m]", "-c:a", "pcm_s24le", pre])
meas = run(["-i", pre, "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"])
j = json.loads(re.search(r"\{[^{}]*\"input_i\"[^{}]*\}", meas, re.S).group(0))
gain = -14.5 - float(j["input_i"])
master = os.path.join(HERE, "demo_master.wav")
run(["-i", pre, "-af", f"volume={gain:.2f}dB,aresample=192000,alimiter=limit=0.7943:attack=2:release=60:level=false,aresample=48000",
     "-c:a", "pcm_s24le", "-ar", "48000", master])
fin = run(["-i", master, "-af", "ebur128=peak=true", "-f", "null", "-"])
I = re.findall(r"I:\s+(-?[\d.]+) LUFS", fin)[-1]; TP = re.findall(r"Peak:\s+(-?[\d.]+) dBFS", fin)[-1]
print(f"premaster {j['input_i']} LUFS / {j['input_tp']} dBTP -> gain {gain:+.2f} dB -> master {I} LUFS, true peak {TP} dBTP")


def tc(s):
    h, s = divmod(s, 3600); m, s = divmod(s, 60); return f"{int(h):02d}:{int(m):02d}:{int(s):02d},{int(round((s % 1) * 1000)) % 1000:03d}"
srt = []
for n, e in enumerate(sorted(tl["voice"], key=lambda e: e["at"]), 1):
    d = sf.info(e["file"]).duration
    srt.append(f"{n}\n{tc(e['at'])} --> {tc(e['at'] + d + 0.25)}\n{e['caption']}\n")
p = os.path.join(DEMO, "Final", "SHHH_Meta_VR_Start_2026_Official_Demo.en.srt")
open(p, "w", encoding="utf-8").write("\n".join(srt)); print("captions:", len(srt))
