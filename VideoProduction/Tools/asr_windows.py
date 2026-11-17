"""Intelligibility QA: transcribe the MIXED master at every voice event and compare with the script caption."""
import sys, json, librosa, re, difflib
from faster_whisper import WhisperModel
tl = json.load(open(sys.argv[2])); y, sr = librosa.load(sys.argv[1], sr=16000, mono=True)
m = WhisperModel("base.en", device="cpu", compute_type="int8", download_root=sys.argv[3])
norm = lambda s: re.sub(r"[^a-z ]", "", s.lower().replace("mom:", "")).split()
scores = []
for e in tl["voice"]:
    a = int(e["at"] * sr); seg = y[a:a + int((librosa.get_duration(path=e["file"]) + 0.3) * sr)]
    txt = " ".join(s.text.strip() for s in m.transcribe(seg, beam_size=5, language="en")[0])
    r = difflib.SequenceMatcher(None, norm(e["caption"]), norm(txt)).ratio(); scores.append(r)
    print(f"{e['at']:7.2f}  {r:4.2f}  SCRIPT: {e['caption'][:60]:60s} | HEARD: {txt[:70]}")
print(f"mean word match {sum(scores)/len(scores):.2f}, lines < 0.6: {sum(1 for s in scores if s < 0.6)}")
