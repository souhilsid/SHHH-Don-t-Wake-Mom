#!/usr/bin/env python3
"""SHHH! V8 night-sky panorama (equirectangular, original procedural painting). Writes layer PNGs to DesignSources/UI_V8/src/sky_*.png
and the flattened sky to DesignSources/UI_V8/png/sky_night_pano.png. ui_v8_kra.py assembles the layers into a Krita .kra master."""
import numpy as np, os
from PIL import Image, ImageFilter
W, H = 4096, 2048
rng = np.random.default_rng(20261109)
SRC = "DesignSources/UI_V8/src"; PNG = "DesignSources/UI_V8/png"; os.makedirs(SRC, exist_ok=True)
y = np.linspace(0, 1, H)[:, None]            # 0 = zenith, 0.5 = horizon
def hexc(h): return np.array([int(h[i:i+2], 16) for i in (1, 3, 5)], float) / 255
zen, mid, hor, low = hexc("#0F0820"), hexc("#261239"), hexc("#5A3290"), hexc("#140B26")
t = np.clip(y / 0.5, 0, 1); t = np.nan_to_num(t)
col = np.where(t < 0.6, zen + (mid - zen) * (t / 0.6), mid + (hor - mid) * np.clip((t - 0.6) / 0.4, 0, 1) ** 1.6)
col = np.where(y > 0.5, hor + (low - hor) * np.clip((y - 0.5) / 0.08, 0, 1), col)
grad = np.broadcast_to(col[:, None, :] if col.ndim == 2 else col, (H, W, 3)).copy() if col.ndim == 3 else np.repeat(col[:, None, :], W, 1)
# horizon warm glow (town lights) around the whole ring
glow = np.exp(-((y - 0.5) / 0.07) ** 2)[:, :, None] * hexc("#F57F78")[None, None, :] * 0.2 * np.where(y < 0.5, 1, 0.4)[:, :, None]
grad = np.clip(grad + glow, 0, 1)
Image.fromarray((grad * 255).astype(np.uint8)).save(f"{SRC}/sky_gradient.png")
# clouds: fractal value noise, lit from the moon side
def vnoise(sh, sc):
    g = rng.random((sh[0] // sc + 2, sh[1] // sc + 2))
    im = Image.fromarray((g * 255).astype(np.uint8)).resize((sh[1] + sc * 2, sh[0] + sc * 2), Image.BICUBIC)
    return np.asarray(im, float)[:sh[0], :sh[1]] / 255
n = sum(vnoise((H, W), s) * a for s, a in [(256, .5), (128, .25), (64, .13), (32, .07), (16, .05)])
band = np.exp(-((y - 0.36) / 0.09) ** 2)
cl = np.clip((n - 0.52) * 3.2, 0, 1) * band
cloud_rgb = np.repeat(np.repeat(np.zeros((H, 1, 3)), W, 1), 1, 0) + hexc("#8A63C9")
lit = np.clip(cloud_rgb + (n[..., None] - .5) * 0.35, 0, 1)
rgba = np.dstack([lit, cl * 0.75])
Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(3)).save(f"{SRC}/sky_clouds.png")
# stars (denser near zenith), a few coloured, some sparkle crosses
st = np.zeros((H, W, 4))
N = 5200
ys = (rng.beta(1.3, 2.6, N) * 0.47 * H).astype(int); xs = rng.integers(0, W, N)
for xx, yy in zip(xs, ys):
    r = rng.choice([1, 1, 1, 2, 2, 3], p=[.35, .2, .15, .15, .1, .05]); a = rng.uniform(.35, 1)
    c = rng.choice(["#FFF8E8", "#FFF0BA", "#FFCF2E", "#BFD7FF", "#F45F73"], p=[.55, .2, .1, .12, .03])
    cc = hexc(c)
    yy0, yy1, xx0, xx1 = max(0, yy - r), min(H, yy + r + 1), max(0, xx - r), min(W, xx + r + 1)
    yy_, xx_ = np.ogrid[yy0:yy1, xx0:xx1]; m = np.clip(1 - np.sqrt((yy_ - yy) ** 2 + (xx_ - xx) ** 2) / (r + .5), 0, 1) * a
    st[yy0:yy1, xx0:xx1, :3] = np.maximum(st[yy0:yy1, xx0:xx1, :3], cc * m[..., None] ** 0.01 * (m[..., None] > 0))
    st[yy0:yy1, xx0:xx1, 3] = np.maximum(st[yy0:yy1, xx0:xx1, 3], m)
for _ in range(60):                         # 4-point sparkles
    xx, yy = rng.integers(40, W - 40), rng.integers(30, int(H * 0.42)); L = rng.integers(10, 26)
    for d in range(-L, L + 1):
        a = (1 - abs(d) / L) ** 2
        for (px, py) in [(xx + d, yy), (xx, yy + d)]:
            st[py, px, :3] = hexc("#FFF0BA"); st[py, px, 3] = max(st[py, px, 3], a)
Image.fromarray((np.clip(st, 0, 1) * 255).astype(np.uint8), "RGBA").save(f"{SRC}/sky_stars.png")
# moon with halo at azimuth 300 deg (north-west windows), altitude 28 deg
mx, my, R = int(W * 300 / 360), int(H * (0.5 - 28 / 180)), 70
yy_, xx_ = np.ogrid[0:H, 0:W]; d = np.sqrt((xx_ - mx) ** 2 + (yy_ - my) ** 2)
halo = np.exp(-(np.maximum(d - R, 0) / 160) ** 2) * 0.55
disk = np.clip(R + 1 - d, 0, 1)
crater = np.clip(1 - (np.sqrt((xx_ - mx + 22) ** 2 + (yy_ - my - 12) ** 2) < 16) * 0.12 - (np.sqrt((xx_ - mx - 18) ** 2 + (yy_ - my + 20) ** 2) < 11) * 0.1, 0, 1)
moon = np.zeros((H, W, 4)); moon[..., :3] = hexc("#FFF0BA") * np.where(disk[..., None] > 0, crater[..., None], 1)
moon[..., 3] = np.maximum(disk, halo)
Image.fromarray((np.clip(moon, 0, 1) * 255).astype(np.uint8), "RGBA").save(f"{SRC}/sky_moon.png")
# far town: painted roof-line silhouettes with lit windows all around the horizon (seen above the lawn, behind the 3D street)
town = np.zeros((H, W, 4)); hz = H // 2
x = 0
while x < W:
    w = int(rng.integers(60, 150)); hgt = int(rng.integers(26, 70)); roof = int(rng.integers(14, 34))
    body = hexc(["#2C1F47", "#33244F", "#291B3F", "#3A2A55"][int(rng.integers(0, 4))])
    town[hz - hgt:hz + 4, x:x + w, :3] = body; town[hz - hgt:hz + 4, x:x + w, 3] = 1
    for k in range(roof):                                   # gable
        x0 = x + int(k * w / (2 * roof)); x1 = x + w - int(k * w / (2 * roof))
        town[hz - hgt - k - 1, x0:x1, :3] = body * 0.8; town[hz - hgt - k - 1, x0:x1, 3] = 1
    for wy in range(hz - hgt + 8, hz - 6, 16):              # windows: most warm, some dark
        for wx in range(x + 8, x + w - 12, 18):
            if rng.random() < 0.55:
                c = hexc("#FFD36B") if rng.random() < 0.75 else hexc("#F5A867")
                town[wy:wy + 7, wx:wx + 6, :3] = c; town[wy:wy + 7, wx:wx + 6, 3] = 1
    if rng.random() < 0.5:                                  # tree
        tx = x + w + int(rng.integers(4, 20)); th = int(rng.integers(30, 60))
        yy_, xx_ = np.ogrid[hz - th:hz, max(0, tx - 20):min(W, tx + 20)]
        m = ((xx_ - tx) ** 2 / 400 + (yy_ - (hz - th * 0.6)) ** 2 / (th * th * 0.2)) < 1
        sub = town[hz - th:hz, max(0, tx - 20):min(W, tx + 20)]; sub[m, :3] = hexc("#22383F"); sub[m, 3] = 1
    x += w + int(rng.integers(6, 40))
Image.fromarray((np.clip(town, 0, 1) * 255).astype(np.uint8), "RGBA").save(f"{SRC}/sky_town.png")
# flatten
base = Image.open(f"{SRC}/sky_gradient.png").convert("RGBA")
for l in ["sky_clouds", "sky_stars", "sky_moon", "sky_town"]: base = Image.alpha_composite(base, Image.open(f"{SRC}/{l}.png"))
base.convert("RGB").save(f"{PNG}/sky_night_pano.png")
base.convert("RGB").resize((1024, 512)).save(f"{SRC}/sky_preview.jpg")
print("sky written")
