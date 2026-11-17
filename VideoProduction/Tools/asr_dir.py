import sys, glob, os, librosa
from faster_whisper import WhisperModel
m = WhisperModel("base.en", device="cpu", compute_type="int8", download_root="tts_models/whisper")
for f in sorted(glob.glob(os.path.join(sys.argv[1], sys.argv[2]))):
    y, _ = librosa.load(f, sr=16000, mono=True)
    segs, _ = m.transcribe(y, beam_size=5)
    print(f"{os.path.basename(f)}\t{len(y)/16000:.2f}s\t" + " ".join(s.text.strip() for s in segs), flush=True)
