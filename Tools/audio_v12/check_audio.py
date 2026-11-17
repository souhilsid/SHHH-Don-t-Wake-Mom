#!/usr/bin/env python3
"""Technical checks for the V12 detective sound set: duration, peak, RMS, clipped samples, loop seam (music), stem-length match,
and a spectrogram sheet for a visual sanity check. Usage: check_audio.py <dir> <sheet.png>"""
import glob, os, subprocess, sys, io
import numpy as np
from scipy.io import wavfile
from scipy.signal import spectrogram
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt

D = sys.argv[1]; SHEET = sys.argv[2]
def load(path):
    if path.endswith(".wav"): sr, x = wavfile.read(path)
    else:
        raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "wav", "-acodec", "pcm_s16le", "-"], capture_output=True, check=True).stdout
        sr, x = wavfile.read(io.BytesIO(raw))
    x = x.astype(np.float32) / 32768.0
    return sr, (x if x.ndim == 1 else x.mean(axis=1)), x
lens = {}
for f in sorted(glob.glob(os.path.join(D, "*.*"))):
    sr, m, x = load(f); n = os.path.basename(f)
    pk = 20 * np.log10(np.abs(x).max() + 1e-9); rms = 20 * np.log10(np.sqrt(np.mean(m ** 2)) + 1e-9); clip = int((np.abs(x) > 0.999).sum())
    line = f"{n:28s} {len(m) / sr:6.2f} s  peak {pk:6.1f}  rms {rms:6.1f}  clipped {clip}"
    if n.startswith("music_"):
        seam = np.abs(m[:64] - m[-64:][::-1]).mean() if False else abs(float(m[0]) - float(m[-1]))
        step = np.median(np.abs(np.diff(m)))
        line += f"  loop seam jump {seam:.4f} (typical step {step:.4f})"
        lens.setdefault(n.split("_")[1], set()).add(len(m))
    print(line)
for k, v in lens.items(): print(f"set {k}: {'equal lengths' if len(v) == 1 else 'LENGTHS DIFFER ' + str(v)}")
show = ["music_menu_1.ogg", "music_match_calm_1.ogg", "music_match_tension_1.ogg", "music_match_danger_1.ogg", "mom_wake_stinger_1.ogg", "win_you_1.ogg"]
fig, ax = plt.subplots(len(show), 1, figsize=(14, 2.2 * len(show)))
for a, n in zip(ax, show):
    sr, m, _ = load(os.path.join(D, n)); seg = m[: int(sr * min(12, len(m) / sr))]
    f, t, S = spectrogram(seg, sr, nperseg=2048, noverlap=1536)
    a.pcolormesh(t, f, 10 * np.log10(S + 1e-12), shading="auto", vmin=-110, vmax=-30); a.set_ylim(30, 4000); a.set_yscale("log"); a.set_title(n, fontsize=9)
plt.tight_layout(); plt.savefig(SHEET, dpi=80); print("sheet:", SHEET)
