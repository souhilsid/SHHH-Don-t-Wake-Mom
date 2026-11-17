#!/bin/bash
# Final export: Remotion picture (Renders/demo_picture.mp4, full-range JPEG frames -> converted to TV range) + Audio/demo_master.wav
FF="/c/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin"
cd "$(dirname "$0")/.."
"$FF/ffmpeg" -nostdin -v error -y -i Renders/demo_picture.mp4 -i Audio/demo_master.wav -map 0:v -map 1:a \
  -vf "scale=in_range=full:out_range=tv,format=yuv420p" -color_range tv -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
  -c:v libx264 -preset slow -crf 16 -profile:v high -level 4.2 -pix_fmt yuv420p -r 30 -g 60 -bf 2 \
  -c:a aac -b:a 320k -ar 48000 -ac 2 -shortest -movflags +faststart \
  -metadata title="SHHH! Don't Wake Mom! | Official VR Gameplay Demo | Meta VR Start 2026" \
  Final/SHHH_Meta_VR_Start_2026_Official_Demo.mp4
