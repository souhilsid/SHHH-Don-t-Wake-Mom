#!/bin/bash
# SHHH! V8 audio: regenerate every original clip and lay it out for Unity (Resources/Audio, AudioBank naming <key>_<n>).
# Needs: python3 + numpy + mido + piper-tts, fluidsynth + FluidR3_GM.sf2, sox (with vorbis). Voice: en_US-ljspeech-high.onnx (public-domain data).
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../.." && pwd)"
VOICE="${1:?path to en_US-ljspeech-high.onnx}"; WORK="${2:-/tmp/shhh_audio_v8}"
DST="$ROOT/UnityProject/Assets/SHHH/Resources/Audio"
rm -rf "$WORK"; mkdir -p "$WORK"
python3 "$HERE/make_music.py" "$WORK/music" >/dev/null
python3 "$HERE/make_sfx.py" "$WORK/sfx" "$VOICE" >/dev/null
python3 "$HERE/make_vo.py" "$VOICE" "$WORK/vo" >/dev/null 2>&1
python3 "$HERE/gen_momlines_cs.py" >/dev/null
rm -rf "$DST"; mkdir -p "$DST/VO"
cp "$WORK"/sfx/*.wav "$DST/"
for f in "$WORK"/music/*.wav; do b=$(basename "$f" .wav); sox "$f" -C 5 "$DST/${b}_1.ogg"; done
for f in "$WORK"/vo/*.wav; do b=$(basename "$f" .wav); cp "$f" "$DST/VO/${b}_1.wav"; done
echo "$(ls "$DST" | wc -l) files + $(ls "$DST/VO" | wc -l) VO -> $DST"
