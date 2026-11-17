#!/bin/bash
# QA one export: container/stream specs, loudness (EBU R128 + true peak), black segments, frozen segments, audio silence.
FF="/c/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin"
f="$1"
echo "== $f"; ls -l "$f" | awk '{print "size_bytes",$5}'; sha256sum "$f" | awk '{print "sha256",$1}'
"$FF/ffprobe" -v error -show_entries format=duration,bit_rate,format_name:stream=codec_name,profile,width,height,pix_fmt,r_frame_rate,avg_frame_rate,sample_rate,channels,field_order,color_space -of compact "$f"
python - "$f" <<'P'
import sys; b=open(sys.argv[1],'rb').read(4_000_000); m=b.find(b'moov'); d=b.find(b'mdat'); print("faststart (moov before mdat):", 0<=m<d if d>=0 else m>=0)
P
"$FF/ffmpeg" -hide_banner -nostats -i "$f" -map 0:a -af ebur128=peak=true -f null - 2>&1 | grep -E "^\s+(I|LRA|Peak):" | tr -s ' '
"$FF/ffmpeg" -hide_banner -nostats -i "$f" -map 0:v -vf "blackdetect=d=0.5:pix_th=0.06,freezedetect=n=0.002:d=4" -f null - 2>&1 | grep -E "black_start|freeze_start|freeze_duration" | head -20
"$FF/ffmpeg" -hide_banner -nostats -i "$f" -map 0:a -af silencedetect=n=-50dB:d=2 -f null - 2>&1 | grep -E "silence_(start|end)" | head -20
