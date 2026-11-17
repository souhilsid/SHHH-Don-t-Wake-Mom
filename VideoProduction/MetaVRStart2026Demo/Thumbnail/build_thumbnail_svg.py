"""Inkscape stage of the thumbnail: writes SHHH_Demo_Thumbnail.svg (1280x720, editable in Inkscape) and exports the PNG/JPG with the
Inkscape CLI. Brand palette from 02_BrandAssets/SHHH_UI_Color_Tokens.json; the approved game logo; text set in the brand fonts
(Luckiest Guy / Fredoka, OFL) converted to vector outlines with fontTools, so the SVG renders identically without installing fonts.
The gameplay picture is the real Quest frame graded in Krita (krita_grade_frame.py -> frame_graded.png)."""
import base64, math, os, subprocess
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen

HERE = os.path.dirname(os.path.abspath(__file__))
BRAND = os.path.abspath(os.path.join(HERE, "..", "..", "02_BrandAssets"))
INK = r"C:/Program Files/Inkscape/bin/inkscape.com"
C = {"navy": "#19183F", "night": "#10132F", "sky": "#2A2470", "yellow": "#FFD65A", "coral": "#FF7769", "teal": "#65C8C5", "purple": "#7958BB", "cream": "#FFF3DD", "ink": "#2A214B"}


def text_path(text, font, size, x, y, tracking=0.0, anchor="start"):
    f = TTFont(font); gs = f.getGlyphSet(); cmap = f.getBestCmap(); upm = f["head"].unitsPerEm; s = size / upm
    adv = [gs[cmap[ord(ch)]].width * s + tracking for ch in text]; width = sum(adv) - tracking
    cx = x - (width if anchor == "end" else width / 2 if anchor == "middle" else 0)
    pen = SVGPathPen(gs); d = []
    for ch, a in zip(text, adv):
        g = cmap[ord(ch)]; tp = TransformPen(pen, (s, 0, 0, -s, cx, y)); gs[g].draw(tp); cx += a
    return pen.getCommands(), width


def b64(p, mime): return f"data:{mime};base64," + base64.b64encode(open(p, "rb").read()).decode()


def star(cx, cy, r, rot=0):
    pts = []
    for k in range(10):
        rr = r if k % 2 == 0 else r * 0.44; a = math.radians(rot - 90 + k * 36)
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{C["yellow"]}" stroke="{C["ink"]}" stroke-width="5" stroke-linejoin="round"/>'


def burst(cx, cy, r1, r2, n, rot=0, fill=C["yellow"]):
    pts = []
    for k in range(n * 2):
        rr = r1 if k % 2 == 0 else r2; a = math.radians(rot + k * 180 / n)
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{fill}" stroke="{C["ink"]}" stroke-width="7" stroke-linejoin="round"/>'


LG = os.path.join(BRAND, "Fonts", "LuckiestGuy-Regular.ttf"); FR = os.path.join(BRAND, "Fonts", "Fredoka-SemiBold.ttf")
t1, w1 = text_path("VR GAMEPLAY", LG, 74, 1010, 600, 2, "middle")
t2, w2 = text_path("DEMO", LG, 96, 1010, 692, 3, "middle")

stars = "".join(star(x, y, r, rot) for x, y, r, rot in [(690, 70, 22, 10), (1230, 380, 18, -8), (560, 690, 16, 20), (40, 60, 14, 0), (1245, 60, 13, 12)])
dots = "".join(f'<circle cx="{(i * 97) % 1280}" cy="{(i * 53) % 470}" r="{1.6 + (i % 3) * 0.8}" fill="{C["cream"]}" opacity="{0.35 + (i % 4) * 0.12:.2f}"/>' for i in range(70))

svg = f'''<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1280" height="720" viewBox="0 0 1280 720">
 <title>SHHH! Don't Wake Mom! - VR gameplay demo thumbnail</title>
 <defs>
  <radialGradient id="sky" cx="0.55" cy="0.35" r="0.8"><stop offset="0" stop-color="{C["sky"]}"/><stop offset="0.6" stop-color="{C["navy"]}"/><stop offset="1" stop-color="{C["night"]}"/></radialGradient>
  <clipPath id="panelClip"><rect x="0" y="0" width="640" height="480" rx="44"/></clipPath>
  <mask id="moonCut"><rect width="1280" height="720" fill="white"/><circle cx="1196" cy="196" r="58" fill="black"/></mask>
  <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%" color-interpolation-filters="sRGB">
   <feGaussianBlur in="SourceAlpha" stdDeviation="12" result="b"/><feOffset in="b" dy="14" result="o"/>
   <feFlood flood-color="#06051a" flood-opacity="0.55"/><feComposite in2="o" operator="in" result="s"/>
   <feMerge><feMergeNode in="s"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
 </defs>
 <g id="background"><rect width="1280" height="720" fill="url(#sky)"/>{dots}</g>
 <g id="moon"><circle cx="1170" cy="215" r="66" fill="{C["yellow"]}" stroke="{C["ink"]}" stroke-width="6" mask="url(#moonCut)"/></g>
 <g id="burst">{burst(560, 140, 150, 96, 14, 8)}</g>
 <g id="gameplay-panel" transform="translate(52,150) rotate(-4 320 240)" filter="url(#shadow)">
  <rect x="-16" y="-16" width="672" height="512" rx="58" fill="{C["cream"]}" stroke="{C["ink"]}" stroke-width="8"/>
  <image x="0" y="0" width="640" height="480" preserveAspectRatio="xMidYMid slice" xlink:href="{b64(os.path.join(HERE, "frame_graded.png"), "image/png")}" clip-path="url(#panelClip)"/>
  <circle cx="345" cy="345" r="132" fill="none" stroke="{C["coral"]}" stroke-width="8" stroke-dasharray="22 14" opacity="0.9"/>
 </g>
 <g id="noise-lines" stroke="{C["cream"]}" stroke-width="9" stroke-linecap="round" fill="none">
  <path d="M 700 560 q 22 -24 0 -48"/><path d="M 728 584 q 40 -48 0 -96"/>
 </g>
 <g id="logo" transform="rotate(4 1010 270)" filter="url(#shadow)"><image x="760" y="-6" width="500" height="500" xlink:href="{b64(os.path.join(HERE, "logo_clean.png"), "image/png")}"/></g>
 <g id="title" filter="url(#shadow)">
  <path d="{t1}" fill="{C["cream"]}" stroke="{C["ink"]}" stroke-width="12" stroke-linejoin="round" paint-order="stroke fill"/>
  <path d="{t2}" fill="{C["yellow"]}" stroke="{C["ink"]}" stroke-width="14" stroke-linejoin="round" paint-order="stroke fill"/>
 </g>
 <g id="stars">{stars}</g>
</svg>'''
p = os.path.join(HERE, "SHHH_Demo_Thumbnail.svg"); open(p, "w", encoding="utf-8").write(svg)
png = os.path.join(HERE, "SHHH_Demo_Thumbnail_1280x720.png")
subprocess.run([INK, p, "--export-type=png", f"--export-filename={png}", "--export-width=1280", "--export-height=720", "--export-background-opacity=1"], check=True,
               capture_output=True)
print("inkscape export ->", png, os.path.getsize(png))
