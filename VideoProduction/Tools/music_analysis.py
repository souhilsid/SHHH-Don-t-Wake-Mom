import sys, json, librosa, numpy as np
out = {}
for f in sys.argv[1:]:
    y, sr = librosa.load(f, sr=22050, mono=True)
    tempo, beats = librosa.beat.beat_track(y=y, sr=sr)
    bt = librosa.frames_to_time(beats, sr=sr)
    rms = librosa.feature.rms(y=y)[0]; t = librosa.frames_to_time(np.arange(len(rms)), sr=sr)
    # energy per 2 s window (dB)
    env = [round(float(20*np.log10(np.mean(rms[(t>=s)&(t<s+2)])+1e-6)),1) for s in range(0, int(t[-1]), 2)]
    out[f.split('/')[-1]] = {"tempo": round(float(np.atleast_1d(tempo)[0]),1), "beats": [round(float(b),3) for b in bt], "energy_db_per_2s": env, "duration": round(len(y)/sr,2)}
    print(f.split('/')[-1], out[f.split('/')[-1]]["tempo"], "bpm", len(bt), "beats", "energy:", env)
json.dump(out, open("04_MusicAndLicenses/music_analysis.json","w"), indent=1)
