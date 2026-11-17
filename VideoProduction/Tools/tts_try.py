import sys, soundfile as sf
from kokoro_onnx import Kokoro
k = Kokoro("tts_models/kokoro-v1.0.onnx", "tts_models/voices-v1.0.bin")
out = sys.argv[1]
line = "It's late. The house is a disaster. And Mom... is finally asleep."
for v in ["am_michael","am_fenrir","bm_george","am_puck","bm_fable","am_onyx"]:
    s, sr = k.create(line, voice=v, speed=0.92, lang="en-us" if v.startswith("a") else "en-gb")
    sf.write(f"{out}/try_{v}.wav", s, sr); print(v, round(len(s)/sr,2))
