#!/bin/bash
# Contact sheets (one frame every 4 s, 6x5 tiles = 2 min per sheet, timecode burnt in) + scene-change list per source recording.
FF="/c/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin"
OUT="$(dirname "$0")/../00_ProductionAudit/ContactSheets"
FONT="C\:/Windows/Fonts/arial.ttf"
for src in "$@"; do
  id=$(basename "$src" .mp4 | sed 's/com.oculus.vrshell-//; s/-0$//')
  "$FF/ffmpeg" -v error -y -i "$src" -vf "fps=1/4,scale=320:-2,drawtext=fontfile='$FONT':text='%{pts\:hms}':x=6:y=6:fontsize=20:fontcolor=yellow:box=1:boxcolor=black@0.6,tile=6x5:padding=4:color=0x19183F" -q:v 4 "$OUT/${id}_%02d.jpg"
  "$FF/ffmpeg" -v info -i "$src" -vf "select='gt(scene,0.35)',showinfo" -an -f null - 2>&1 | grep -o "pts_time:[0-9.]*" | cut -d: -f2 > "$OUT/${id}_scenes.txt"
  echo "$id sheets=$(ls "$OUT"/${id}_[0-9]*.jpg | wc -l) scenes=$(wc -l < "$OUT/${id}_scenes.txt")"
done
