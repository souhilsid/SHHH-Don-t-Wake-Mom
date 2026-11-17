#!/bin/bash
# Final trailer export: Remotion picture (07_Trailer/Renders) + mixed master (06_SoundDesign/stems) -> YouTube-ready H.264/AAC MP4.
FF="/c/Users/aisci/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.2-full_build/bin"
cd "$(dirname "$0")/.."
"$FF/ffmpeg" -v error -y -i 07_Trailer/Renders/trailer_picture_v1.mp4 -i 06_SoundDesign/stems/trailer_master.wav \
  -map 0:v -map 1:a -vf "scale=in_range=full:out_range=tv,format=yuv420p" -color_range tv -c:v libx264 -preset slow -crf 16 -profile:v high -level 4.2 -pix_fmt yuv420p -r 30 -g 60 -bf 2 \
  -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
  -c:a aac -b:a 320k -ar 48000 -ac 2 -shortest -movflags +faststart \
  -metadata title="SHHH! - Don't Wake Mom! | Official Game Trailer" 09_FinalExports/SHHH_Official_Game_Trailer_1080p.mp4
