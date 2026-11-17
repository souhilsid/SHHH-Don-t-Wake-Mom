"""SHHH! gameplay walkthrough - authentic first-person Quest footage, in player-journey order.

Segments are cut from the ORIGINAL Quest recordings (read-only), unaltered (no zoom, no speed change, no re-timing, original game
audio). Remotion renders the intro/outro logo cards and the short chapter cards; FFmpeg joins everything and normalises loudness.

Usage: python build_walkthrough.py [--cards] [--segments] [--join]   (no flag = all)
"""
import json, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
VP = os.path.abspath(os.path.join(HERE, ".."))
REM = os.path.abspath(os.path.join(VP, "..", "Remotion"))
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
DL = "C:/Users/aisci/Downloads/"
SRC = {"F05": DL + "com.oculus.vrshell-20261112-170302-0.mp4", "F06": DL + "com.oculus.vrshell-20261115-170859-0.mp4",
       "F07": DL + "com.oculus.vrshell-20261117-122141-0.mp4"}
MENU_MUSIC = os.path.join(VP, "04_MusicAndLicenses", "Fluffing_a_Duck.mp3")
WORK = os.path.join(HERE, "Editorial", "work"); os.makedirs(WORK, exist_ok=True)


def t(s): m, x = s.split(":"); return int(m) * 60 + float(x)


# (id, kind, chapter title for the description, src, in, out, note)
EDL = [
    ("W00", "intro", "Intro", None, None, None, "logo + 'Gameplay Walkthrough'"),
    ("K01", "chapter", "Get ready", None, None, None, "GET READY"),
    ("W01", "seg", None, "F07", "0:20.6", "0:59.6", "c21: Pick a mode, Genies wardrobe (hair, eyes, starter looks, save)"),
    ("K02", "chapter", "Learn the moves", None, None, None, "LEARN THE MOVES"),
    ("W02", "seg", None, "F06", "0:59.6", "2:13.0", "Settings board, How to win board, Practice: book > shelf > cup > pan, hop"),
    ("W03", "seg", None, "F05", "2:01.5", "2:14.0", "Practice (c15): drop the book - the noise burst and Mom stirring"),
    ("K03", "chapter", "The match", None, None, None, "THE MATCH"),
    ("W04", "seg", None, "F07", "1:00.0", "11:54.0", "c21: Solo, 10 min, Normal bots - the whole match uncut: jobs, pets, Mom wakes, final inspection, accusation, results"),
    ("K04", "chapter", "Another night", None, None, None, "ANOTHER NIGHT"),
    ("W05", "seg", None, "F06", "2:17.0", "8:11.0", "Nov 15 build: Solo 5 min - blanket, mats, kitchen plates, 'Who messed with Tess's job?', results"),
    ("K05", "chapter", "Play with friends", None, None, None, "PLAY WITH FRIENDS"),
    ("W06", "seg", None, "F07", "11:54.0", "12:09.6", "c21: Play with friends - create a private room (empty seats are filled by bot siblings)"),
    ("W07", "outro", "Outro", None, None, None, "logo"),
]


def run(args, quiet=True):
    r = subprocess.run(args, capture_output=True, text=True)
    if r.returncode: print(r.stderr[-3000:]); sys.exit(1)
    return r.stderr


def cards():
    for cid, kind, _, _, _, _, note in EDL:
        if kind not in ("intro", "chapter", "outro"): continue
        props = {"kind": kind, "text": note} if kind == "chapter" else {"kind": kind, "sub": "Gameplay Walkthrough" if kind == "intro" else ""}
        out = os.path.join(WORK, f"{cid}_card.mp4")
        pf = os.path.join(WORK, f"{cid}_props.json"); json.dump(props, open(pf, "w"))
        subprocess.run(f'npx remotion render src/remotion/index.ts WalkthroughCard "{out}" --props="{pf}" --muted --codec=h264 --crf=14 --log=error', cwd=REM, shell=True, check=True)
        print("card", cid, props)


def norm_card(cid, kind):
    """card video + audio bed (menu music tail for intro/outro, silence for chapters) -> uniform intermediate"""
    src = os.path.join(WORK, f"{cid}_card.mp4"); out = os.path.join(WORK, f"{cid}.mp4")
    if kind == "chapter":
        a = ["-f", "lavfi", "-i", "anullsrc=r=48000:cl=stereo"]; amap = "1:a"; af = []
    else:
        a = ["-ss", "0" if kind == "intro" else "52", "-i", MENU_MUSIC]; amap = "1:a"
        af = ["-af", "volume=-6dB,afade=t=in:d=0.4,afade=t=out:st=4.0:d=1.0"]
    # Remotion writes full-range (yuvj420p) frames: convert to TV range so cards and Quest footage share one colour range in the concat
    run([FF, "-y", "-i", src, *a, "-map", "0:v", "-map", amap, "-shortest", *af, "-vf", "scale=in_range=full:out_range=tv,format=yuv420p",
         "-color_range", "tv", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709", "-c:v", "libx264", "-preset", "medium", "-crf", "16",
         "-pix_fmt", "yuv420p", "-r", "30", "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", out])


def segment(sid, src, a, b):
    out = os.path.join(WORK, f"{sid}.mp4"); d = t(b) - t(a)
    # unaltered picture (1920x1080 source), CFR 30 fps; original audio; 8-frame fades only at the cut points
    run([FF, "-y", "-ss", f"{t(a):.3f}", "-i", SRC[src], "-t", f"{d:.3f}",
         "-vf", f"fps=30,format=yuv420p,fade=t=in:d=0.27,fade=t=out:st={d - 0.27:.3f}:d=0.27",
         "-af", f"aresample=48000,afade=t=in:d=0.25,afade=t=out:st={d - 0.3:.3f}:d=0.3",
         "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-g", "60", "-c:a", "aac", "-b:a", "192k", "-ac", "2", out])
    print("segment", sid, src, a, b, f"{d:.1f}s")


def join():
    lst = os.path.join(WORK, "concat.txt")
    with open(lst, "w") as fh:
        for it in EDL: fh.write(f"file '{os.path.join(WORK, it[0] + '.mp4').replace(chr(92), '/')}'\n")
    joined = os.path.join(WORK, "joined.mp4")
    run([FF, "-y", "-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", joined])
    meas = run([FF, "-hide_banner", "-i", joined, "-vn", "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"])
    j = json.loads(re.search(r"\{[^{}]*\"input_i\"[^{}]*\}", meas, re.S).group(0))
    final = os.path.join(VP, "09_FinalExports", "SHHH_Official_Gameplay_Walkthrough_1080p.mp4")
    run([FF, "-y", "-i", joined, "-c:v", "copy", "-af",
         f"loudnorm=I=-14:TP=-1.5:LRA=11:measured_I={j['input_i']}:measured_TP={j['input_tp']}:measured_LRA={j['input_lra']}:measured_thresh={j['input_thresh']}:offset={j['target_offset']}:linear=true,aresample=48000",
         "-c:a", "aac", "-b:a", "256k", "-ar", "48000", "-ac", "2", "-movflags", "+faststart", final])
    print("walkthrough audio measured", j["input_i"], "LUFS ->", final)
    # chapters for the YouTube description
    pos, lines = 0.0, []
    for cid, kind, title, *_ in EDL:
        d = float(subprocess.check_output([FF.replace("ffmpeg.exe", "ffprobe.exe"), "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", os.path.join(WORK, cid + ".mp4")]).decode().strip())
        if title: lines.append(f"{int(pos // 60)}:{int(pos % 60):02d} {title}")
        pos += d
    open(os.path.join(VP, "09_FinalExports", "SHHH_Official_Gameplay_Walkthrough_chapters.txt"), "w").write("\n".join(lines) + "\n")
    print("\n".join(lines)); print(f"total {pos / 60:.2f} min")


if __name__ == "__main__":
    flags = set(sys.argv[1:]) or {"--cards", "--segments", "--join"}
    if "--cards" in flags or "--normcards" in flags:
        if "--cards" in flags: cards()
        for cid, kind, *_ in EDL:
            if kind in ("intro", "chapter", "outro"): norm_card(cid, kind)
    if "--segments" in flags:
        for sid, kind, _, src, a, b, _ in EDL:
            if kind == "seg": segment(sid, src, a, b)
    if "--join" in flags: join()
