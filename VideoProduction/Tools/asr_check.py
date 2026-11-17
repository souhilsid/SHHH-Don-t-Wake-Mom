"""Transcribe audio files with faster-whisper (base.en, CPU int8) - QA that each voice clip says what the script says."""
import sys
from faster_whisper import WhisperModel
m = WhisperModel("base.en", device="cpu", compute_type="int8", download_root="tts_models/whisper")
for f in sys.argv[1:]:
    import librosa; y, _ = librosa.load(f, sr=16000, mono=True)
    segs, _ = m.transcribe(y, beam_size=5, vad_filter=False)
    print(f.split('/')[-1], "=>", " ".join(s.text.strip() for s in segs))
