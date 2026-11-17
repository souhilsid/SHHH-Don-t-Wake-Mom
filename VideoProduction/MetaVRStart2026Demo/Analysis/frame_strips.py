"""Fine temporal inspection: for each window (id src start end fps) render a tiled sheet of consecutive frames with burnt-in source time."""
import subprocess, sys, os
FF = r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
D = "C:/Users/aisci/Downloads/"
SRC = {"F04": D+"com.oculus.vrshell-20261111-170037-0.mp4", "F05": D+"com.oculus.vrshell-20261112-170302-0.mp4", "F06": D+"com.oculus.vrshell-20261115-170859-0.mp4",
       "F07": D+"com.oculus.vrshell-20261117-122141-0.mp4", "F09": D+"SHHH Game/SHHH/Remotion/Raw/Records-20261117T150733Z-1-001/Records/com.oculus.vrshell-20261117-174620-0.mp4"}
def ts(s): m, x = s.split(":"); return int(m)*60+float(x)
os.makedirs("Analysis/Frames", exist_ok=True)
for line in open(sys.argv[1]):
    if not line.strip() or line.startswith("#"): continue
    wid, src, a, b, fps = line.split()[:5]; a, b = ts(a), ts(b)
    n = int((b-a)*float(fps)); cols = 8; rows = (n+cols-1)//cols
    vf = (f"fps={fps},scale=240:-2,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='{wid} %{{pts\:hms\:{a}}}':x=3:y=3:fontsize=13:"
          f"fontcolor=yellow:box=1:boxcolor=black@0.6,tile={cols}x{rows}:padding=2")
    subprocess.run([FF, "-v", "error", "-y", "-ss", str(a), "-i", SRC[src], "-t", str(b-a), "-vf", vf, "-frames:v", "1", "-q:v", "3", f"Analysis/Frames/{wid}.jpg"], check=True)
    print(wid, src, n, "frames")
