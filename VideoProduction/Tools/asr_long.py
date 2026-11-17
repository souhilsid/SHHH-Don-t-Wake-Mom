"""Transcribe a long recording with timestamps (faster-whisper base.en, VAD on) - used to check what speech is in the footage
(game Mom lines vs anything else, e.g. a player's microphone)."""
import sys, librosa
from faster_whisper import WhisperModel
y, _ = librosa.load(sys.argv[1], sr=16000, mono=True)
m = WhisperModel("base.en", device="cpu", compute_type="int8", download_root=sys.argv[2])
for s in m.transcribe(y, beam_size=3, vad_filter=True, language="en")[0]:
    print(f"{int(s.start//60)}:{s.start%60:05.2f}-{int(s.end//60)}:{s.end%60:05.2f}  {s.text.strip()}", flush=True)
