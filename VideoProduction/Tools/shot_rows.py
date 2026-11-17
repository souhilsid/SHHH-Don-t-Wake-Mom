# Render candidate shots as rows of 8 evenly spaced frames (label = shot id, source, in-out) for shot selection.
import subprocess, sys, os, csv
FF=r"C:/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin/ffmpeg.exe"
SRC={"F04":"com.oculus.vrshell-20261111-170037-0.mp4","F05":"com.oculus.vrshell-20261112-170302-0.mp4","F06":"com.oculus.vrshell-20261115-170859-0.mp4","F07":"com.oculus.vrshell-20261117-122141-0.mp4"}
D="C:/Users/aisci/Downloads/"
def ts(s): m,x=s.split(":"); return int(m)*60+float(x)
rows=[l.split() for l in open(sys.argv[1]) if l.strip() and not l.startswith("#")]
out=sys.argv[2]; tmp=out+"_tmp"; os.makedirs(tmp,exist_ok=True)
for sid,src,a,b in rows:
    a,b=ts(a),ts(b); n=8; step=(b-a)/n
    vf=f"fps={1/step:.4f},scale=240:-2,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='{sid} {src} %{{pts\:hms}}':x=3:y=3:fontsize=13:fontcolor=yellow:box=1:boxcolor=black@0.6,tile=8x1:padding=2"
    subprocess.run([FF,"-v","error","-y","-ss",str(a),"-i",D+SRC[src],"-vf",vf.replace("%{pts\:hms}",f"%{{pts\:hms\:{a}}}"),"-frames:v","1",f"{tmp}/{sid}.png"],check=True)
ids=[r[0] for r in rows]
for k in range(0,len(ids),10):
    grp=ids[k:k+10]; args=[]
    for g in grp: args+=["-i",f"{tmp}/{g}.png"]
    subprocess.run([FF,"-v","error","-y",*args,"-filter_complex",f"vstack=inputs={len(grp)}" if len(grp)>1 else "null","-q:v","3",f"{out}_{k//10+1}.jpg"],check=True)
print("ok", len(ids))
