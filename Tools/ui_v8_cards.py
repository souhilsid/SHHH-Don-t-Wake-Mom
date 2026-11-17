#!/usr/bin/env python3
"""SHHH! V8 original illustrated menu cards + plates (textless art; titles are live TMP text in Unity).
Writes DesignSources/UI_V8/svg/card_*.svg, plate_*.svg, map_*.svg. Uses the icon geometry from ui_v8_icons.py."""
import importlib.util, math, os, random, sys
spec = importlib.util.spec_from_file_location("icons", os.path.join(os.path.dirname(__file__), "ui_v8_icons.py"))
sys.argv = [sys.argv[0], "/tmp/_icons_unused"]; ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
OUT = "DesignSources/UI_V8/svg"
INK, CREAM, YEL, CORAL, PINK, PURP, MOON, MID, DEEP = ic.INK, ic.CREAM, ic.YEL, ic.CORAL, ic.PINK, ic.PURP, ic.MOON, "#211530", "#261239"
SIB = ["#F57F78", "#65C8C5", "#FFCF2E", "#9B7BE0"]

def icon_group(name, x, y, s, rot=0):
    """Place a 256-box icon at (x,y) top-left with size s."""
    k = s / 256.0; body = ic.svg(name, ic.I[name]).split("\n", 1)[1].rsplit("</svg>", 1)[0]
    return f'<g transform="translate({x},{y}) rotate({rot},{s/2},{s/2}) scale({k})">{body}</g>'

def stars(rng, n, w, h, ymax, col=MOON):
    out = []
    for _ in range(n):
        x, y, r = rng.uniform(20, w - 20), rng.uniform(20, ymax), rng.uniform(2, 6)
        out.append(f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{r:.1f}" fill="{col}" opacity="{rng.uniform(.45,.95):.2f}"/>')
    return "".join(out)

def card(name, top, bottom, motif, seed):
    W, H = 512, 704; rng = random.Random(seed)
    rays = "".join(f'<path d="M256,300 L{256+700*math.cos(math.radians(a)):.0f},{300+700*math.sin(math.radians(a)):.0f} L{256+700*math.cos(math.radians(a+9)):.0f},{300+700*math.sin(math.radians(a+9)):.0f}Z" fill="#FFFFFF" opacity=".06"/>' for a in range(0, 360, 18))
    s = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}"><title>SHHH! menu card {name}</title>
<defs><linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>
<radialGradient id="glow" cx=".5" cy=".42" r=".5"><stop stop-color="#FFF0BA" stop-opacity=".55"/><stop offset="1" stop-color="#FFF0BA" stop-opacity="0"/></radialGradient>
<clipPath id="clip"><rect x="0" y="0" width="{W}" height="{H}" rx="56"/></clipPath></defs>
<g clip-path="url(#clip)"><rect width="{W}" height="{H}" fill="url(#bg)"/>{rays}<ellipse cx="256" cy="300" rx="260" ry="240" fill="url(#glow)"/>{stars(rng, 26, W, H, 520)}
{motif}
<path d="M0,560 C120,530 392,530 512,560 V704 H0Z" fill="{DEEP}" opacity=".92"/><path d="M0,560 C120,530 392,530 512,560" fill="none" stroke="{YEL}" stroke-width="8"/></g>
<rect x="8" y="8" width="{W-16}" height="{H-16}" rx="50" fill="none" stroke="{CREAM}" stroke-width="14"/>
<rect x="20" y="20" width="{W-40}" height="{H-40}" rx="40" fill="none" stroke="{INK}" stroke-width="5" opacity=".5"/></svg>'''
    open(os.path.join(OUT, f"card_{name}.svg"), "w").write(s)

def kid(x, y, s, col, tiptoe=False):
    """Original chibi sibling silhouette (head, hair tuft, pyjama body), sticker outline."""
    k = s / 200
    legs = '<path d="M84,170 L74,206 M116,170 L130,200" stroke="#261239" stroke-width="20" stroke-linecap="round"/>' if tiptoe else '<path d="M86,170 V206 M114,170 V206" stroke="#261239" stroke-width="20" stroke-linecap="round"/>'
    return f'''<g transform="translate({x},{y}) scale({k})" stroke-linejoin="round">{legs}
<path d="M58,180 C52,120 72,98 100,98 C128,98 148,120 142,180Z" fill="{col}" stroke="{INK}" stroke-width="10"/>
<circle cx="100" cy="64" r="46" fill="#F6C9A8" stroke="{INK}" stroke-width="10"/>
<path d="M56,58 C58,20 92,10 112,18 C132,8 152,30 146,60 C130,40 104,34 80,44Z" fill="#6B3E2E" stroke="{INK}" stroke-width="8"/>
<circle cx="84" cy="70" r="6" fill="{INK}"/><circle cx="116" cy="70" r="6" fill="{INK}"/><path d="M92,90 Q100,96 108,90" stroke="{INK}" stroke-width="6" fill="none" stroke-linecap="round"/>
<path d="M140,120 L162,96" stroke="{INK}" stroke-width="18" stroke-linecap="round"/><path d="M140,120 L162,96" stroke="#F6C9A8" stroke-width="8" stroke-linecap="round"/></g>'''

def house_sil(y, col="#2E1C4A"):
    return f'<path d="M0,{y} H60 V{y-60} L110,{y-110} L160,{y-60} V{y} H210 V{y-40} H290 V{y-90} L340,{y-140} L390,{y-90} V{y} H430 V{y-50} L470,{y-80} L512,{y-50} V{y+200} H0Z" fill="{col}"/>' + "".join(f'<rect x="{x}" y="{y-y2}" width="22" height="26" rx="4" fill="{YEL}" opacity=".9"/>' for x, y2 in [(98, 70), (318, 100), (350, 100), (455, 40)])

card("solo", "#3B2466", "#211530", f'<circle cx="390" cy="120" r="56" fill="{MOON}"/><circle cx="414" cy="104" r="50" fill="#3B2466"/>' + house_sil(470) + kid(170, 250, 220, SIB[0], True) + icon_group("shush", 320, 250, 130, 8), 1)
card("friends", "#2F5E8C", "#211530", house_sil(480, "#1E3554") + "".join(kid(14 + i * 118, 300 - (i % 2) * 34, 150, SIB[i]) for i in range(4)) + icon_group("mic", 380, 90, 90, 12), 2)
card("practice", "#6E3F8F", "#261239", icon_group("practice", 126, 150, 260) + icon_group("star", 330, 90, 110, 15) + icon_group("hand", 60, 330, 130, -20), 3)
card("wardrobe", "#A54A6E", "#261239", '<path d="M110,128 H402" stroke="#C8A27A" stroke-width="14" stroke-linecap="round"/>' + "".join(f'<path d="M{150+i*110},128 V150" stroke="{CREAM}" stroke-width="6"/>' for i in range(3)) + "".join(icon_group("wardrobe", 80 + i * 110, 140, 140) for i in range(3)) + kid(170, 300, 200, SIB[3]), 4)
card("len5", "#2F6E6C", "#211530", icon_group("clock", 136, 120, 240) + icon_group("moon", 340, 70, 100), 5)
card("len10", "#7A3B5E", "#211530", icon_group("timer", 136, 120, 240) + icon_group("star", 350, 80, 90) + icon_group("star", 70, 380, 70, -12), 6)
card("settings", "#45507A", "#211530", icon_group("settings", 136, 140, 240), 7)
card("rules", "#4E6A3A", "#211530", icon_group("card", 120, 120, 250, -6) + icon_group("eye", 300, 330, 120), 8)

# ---- plates (title banner, timer face, results podium faces, map parchment) ----
def write(name, s): open(os.path.join(OUT, name + ".svg"), "w").write(s)
write("plate_ribbon", f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 256" width="1024" height="256"><title>SHHH! ribbon banner</title>
<path d="M30,96 L130,80 L120,130 L150,180 L40,190 L80,140Z" fill="#C92C4D" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>
<path d="M994,96 L894,80 L904,130 L874,180 L984,190 L944,140Z" fill="#C92C4D" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>
<path d="M120,60 C380,20 644,20 904,60 L904,190 C644,150 380,150 120,190Z" fill="{PINK}" stroke="{INK}" stroke-width="14" stroke-linejoin="round"/>
<path d="M140,80 C390,44 634,44 884,80" fill="none" stroke="#FFFFFF" stroke-width="10" opacity=".35" stroke-linecap="round"/></svg>''')
write("plate_timer", f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512"><title>SHHH! wrist timer face</title>
<circle cx="256" cy="256" r="240" fill="{DEEP}" stroke="{INK}" stroke-width="14"/><circle cx="256" cy="256" r="206" fill="#2E1C4A"/>
{"".join(f'<rect x="250" y="62" width="12" height="{34 if i%5==0 else 18}" rx="6" fill="{MOON if i%5==0 else PURP}" transform="rotate({i*6},256,256)"/>' for i in range(60))}
<circle cx="256" cy="256" r="150" fill="none" stroke="{PURP}" stroke-width="6" opacity=".6"/>{icon_group("moon", 208, 120, 96)}</svg>''')
write("plate_meter", f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 192" width="1024" height="192"><title>SHHH! Mom sleep meter frame</title>
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="0"><stop stop-color="#7BE39A"/><stop offset=".55" stop-color="{YEL}"/><stop offset=".8" stop-color="{CORAL}"/><stop offset="1" stop-color="#C92C4D"/></linearGradient></defs>
<rect x="10" y="40" width="1004" height="112" rx="56" fill="{DEEP}" stroke="{INK}" stroke-width="14"/><rect x="40" y="66" width="944" height="60" rx="30" fill="url(#g)" opacity=".28"/>
{"".join(f'<rect x="{40+i*94.4:.0f}" y="66" width="6" height="60" fill="{INK}" opacity=".35"/>' for i in range(1,10))}</svg>''')
rng = random.Random(9)
fib = "".join(f'<path d="M{rng.uniform(0,1024):.0f},{rng.uniform(0,1024):.0f} q{rng.uniform(-40,40):.0f},{rng.uniform(-8,8):.0f} {rng.uniform(-80,80):.0f},{rng.uniform(-10,10):.0f}" stroke="#C8A97A" stroke-width="{rng.uniform(1,3):.1f}" opacity=".35" fill="none"/>' for _ in range(260))
write("map_paper", f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" width="1024" height="1024"><title>SHHH! house-map paper (tileable-ish)</title>
<defs><radialGradient id="v" cx=".5" cy=".5" r=".75"><stop stop-color="#FFF6DC"/><stop offset=".8" stop-color="#F3DFB2"/><stop offset="1" stop-color="#E2C48E"/></radialGradient>
<pattern id="grid" width="64" height="64" patternUnits="userSpaceOnUse"><path d="M64,0 V64 H0" fill="none" stroke="#8FB6C9" stroke-width="2" opacity=".35"/></pattern></defs>
<rect width="1024" height="1024" fill="url(#v)"/><rect width="1024" height="1024" fill="url(#grid)"/>{fib}</svg>''')
print("cards + plates written")
