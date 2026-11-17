#!/usr/bin/env python3
"""SHHH! V8 (2026-11-09) original icon set. Sticker style: cream glyph + thick midnight outline, optional accent fills.
Writes DesignSources/UI_V8/svg/icon_<name>.svg ; export with Inkscape (see ui_v8_export.sh). All shapes authored here (project-owned)."""
import math, os, re, sys
OUT = sys.argv[1] if len(sys.argv) > 1 else "DesignSources/UI_V8/svg"
INK, CREAM, YEL, CORAL, PINK, PURP, MOON = "#261239", "#FFF8E8", "#FFCF2E", "#F57F78", "#F45F73", "#70409F", "#FFF0BA"
O = 14  # outline half-thickness (px in a 256 box)

def F(d, c=CREAM): return ("fill", d, c, 0)
def L(d, w=22, c=CREAM): return ("line", d, c, w)
def C(cx, cy, r): return f"M{cx-r},{cy} a{r},{r} 0 1,0 {2*r},0 a{r},{r} 0 1,0 {-2*r},0Z"
def RR(x, y, w, h, r): return f"M{x+r},{y} H{x+w-r} Q{x+w},{y} {x+w},{y+r} V{y+h-r} Q{x+w},{y+h} {x+w-r},{y+h} H{x+r} Q{x},{y+h} {x},{y+h-r} V{y+r} Q{x},{y} {x+r},{y}Z"
def poly(pts): return "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in pts) + "Z"
def star(cx, cy, R, r, n=5, rot=-90):
    p = []
    for i in range(2 * n):
        a = math.radians(rot + i * 180 / n); rr = R if i % 2 == 0 else r
        p.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return poly(p)
def gear(cx, cy, R, teeth=8):
    p = []
    for i in range(teeth * 4):
        a = math.radians(i * 360 / (teeth * 4)); rr = R if (i % 4) in (0, 1) else R * 0.76
        p.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return poly(p)

SHUSH = "M194 345 C166 312 162 276 176 259 C190 242 206 251 226 275 L225 140 C225 114 238 99 259 100 C281 101 289 117 289 140 L288 224 C301 204 325 204 341 218 C359 215 376 229 381 248 C401 255 401 278 389 305 L355 381 C345 399 325 412 302 412 L236 412 C213 412 201 387 194 345Z"

def _scale_path(d, k, dx, dy):
    toks = re.findall(r"[A-Za-z]|-?[\d.]+", d); out = []; i = 0; idx = 0
    for t in toks:
        if t.isalpha(): out.append(t); idx = 0; continue
        v = float(t); v = v * k + (dx if idx % 2 == 0 else dy); idx += 1; out.append(f"{v:.1f}")
    return " ".join(out)
SHUSH_S = _scale_path(SHUSH, 0.66, -58, -41)

I = {
 "play":     [F("M98,62 L198,128 L98,194Z")],
 "friends":  [F(C(92, 98, 28)), F("M42,196 C42,150 68,134 92,134 C116,134 142,150 142,196Z"), F(C(166, 90, 32)), F("M112,200 C112,148 140,130 166,130 C192,130 220,148 220,200Z")],
 "practice": [L(C(128, 128, 82), 20), L(C(128, 128, 46), 20), F(C(128, 128, 16), YEL)],
 "wardrobe": [F("M92,52 L60,64 L28,104 L58,128 L76,112 L76,206 L180,206 L180,112 L198,128 L228,104 L196,64 L164,52 C158,72 144,80 128,80 C112,80 98,72 92,52Z", CORAL)],
 "settings": [F(gear(128, 128, 92, 8)), F(C(128, 128, 30), INK)],
 "back":     [L("M188,128 H76 M120,80 L72,128 L120,176", 26)],
 "retry":    [L("M190,128 A62,62 0 1,1 162,76", 26), F("M150,40 L204,72 L148,106Z")],
 "home":     [F("M40,124 L128,46 L216,124 L194,124 L194,208 L62,208 L62,124Z"), F(RR(110, 150, 36, 58, 8), PURP)],
 "map":      [F("M36,74 L94,52 L162,74 L220,52 L220,184 L162,206 L94,184 L36,206Z", MOON), L("M94,56 V182 M162,78 V202", 10, INK), F("M150,58 C150,30 190,30 190,58 C190,80 170,98 170,112 C170,98 150,80 150,58Z", CORAL)],
 "clock":    [F(C(128, 132, 92)), L("M128,132 V82 M128,132 L162,150", 16, INK), F(C(128, 132, 10), INK), F(RR(104, 26, 48, 22, 8), CORAL)],
 "mic":      [F(RR(96, 36, 64, 112, 32)), L("M66,120 C66,196 190,196 190,120 M128,180 V218 M94,218 H162", 18)],
 "mic_off":  [F(RR(96, 36, 64, 112, 32)), L("M66,120 C66,196 190,196 190,120 M128,180 V218 M94,218 H162", 18), L("M50,40 L206,216", 22, CORAL)],
 "check":    [L("M58,134 L106,182 L200,78", 34, "#7BE39A")],
 "cross":    [L("M70,70 L186,186 M186,70 L70,186", 34, CORAL)],
 "star":     [F(star(128, 134, 104, 46), YEL)],
 "moon":     [F("M150,36 C86,50 48,108 62,170 C78,226 140,246 196,222 C150,230 112,196 108,146 C104,100 120,64 150,36Z", MOON), F(star(186, 82, 24, 9, 4), YEL)],
 "shush":    [F(SHUSH_S, CREAM)],
 "lock":     [L("M88,112 V84 C88,30 168,30 168,84 V112", 24), F(RR(62, 106, 132, 108, 22), YEL), F(C(128, 152, 14), INK), F(RR(122, 156, 12, 32, 6), INK)],
 "room_living":  [F(RR(34, 100, 188, 80, 22), CORAL), F(RR(54, 70, 148, 62, 20), CORAL), F(RR(40, 176, 18, 30, 6), INK), F(RR(198, 176, 18, 30, 6), INK), L("M128,104 V160", 8, INK)],
 "room_kitchen": [F("M50,110 H206 V176 C206,200 186,214 162,214 H94 C70,214 50,200 50,176Z", "#9ED9D6"), F(RR(36, 100, 184, 22, 10)), F(RR(110, 76, 36, 24, 8)), L("M92,60 C82,46 102,40 92,26 M128,60 C118,46 138,40 128,26 M164,60 C154,46 174,40 164,26", 8)],
 "room_laundry": [F(RR(48, 34, 160, 192, 22)), L("M48,82 H208", 10, INK), F(C(128, 152, 52), "#9ED9D6"), L("M96,152 C112,136 144,168 160,152", 10, CREAM), F(C(78, 58, 8), INK), F(C(104, 58, 8), INK)],
 "room_bedroom": [F(RR(30, 120, 196, 56, 14), PURP), F(RR(30, 80, 30, 120, 10)), F(RR(64, 98, 56, 28, 12)), F(RR(30, 172, 196, 24, 8), MOON), F(RR(196, 104, 30, 96, 10))],
 "room_bathroom":[F("M34,120 H222 V146 C222,190 192,212 150,212 H106 C64,212 34,190 34,146Z"), L("M62,120 V60 C62,40 90,36 98,56", 14), F(C(98, 64, 12), "#9ED9D6"), F(C(150, 104, 14), "#9ED9D6"), F(C(180, 92, 10), "#9ED9D6")],
 "room_foyer":   [F("M50,64 H118 V150 C150,150 206,160 216,190 V208 H50Z", CORAL), L("M54,186 H212", 10, INK), L("M84,100 H118 M84,124 H118", 8, CREAM)],
 "room_hallway": [F("M36,212 V172 H84 V132 H132 V92 H180 V52 H220 V212Z", MOON), L("M84,172 H220 M132,132 H220 M180,92 H220", 8, INK)],
 "room_pet":     [F("M128,124 C92,124 72,168 80,190 C88,212 110,206 128,200 C146,206 168,212 176,190 C184,168 164,124 128,124Z"), F(C(70, 112, 20)), F(C(102, 74, 22)), F(C(154, 74, 22)), F(C(186, 112, 20))],
 "mom":      [F(C(128, 64, 36), "#7A4A3A"), F(C(128, 144, 70), "#F6C9A8"), F("M58,132 C58,86 92,70 128,70 C164,70 198,86 198,132 C186,104 160,96 128,96 C96,96 70,104 58,132Z", "#7A4A3A"), L("M92,148 Q106,158 118,148 M138,148 Q150,158 164,148", 9, INK), F(RR(104, 176, 48, 12, 6), CORAL)],
 "alert":    [F("M128,30 L226,206 H30Z", YEL), F(RR(116, 84, 24, 70, 12), INK), F(C(128, 178, 14), INK)],
 "zzz":      [L("M54,92 H104 L54,148 H104 M120,52 H160 L120,96 H160 M168,132 H208 L168,180 H208", 18, MOON)],
 "trophy":   [F("M76,40 H180 V96 C180,140 156,160 128,160 C100,160 76,140 76,96Z", YEL), L("M76,62 C30,62 40,118 82,118 M180,62 C226,62 216,118 174,118", 16, YEL), F(RR(110, 156, 36, 30, 6), YEL), F(RR(78, 184, 100, 30, 10), PURP)],
 "crown":    [F("M40,190 L30,80 L86,126 L128,58 L170,126 L226,80 L216,190Z", YEL), F(C(128, 150, 14), PINK)],
 "eye":      [F("M24,128 C70,62 186,62 232,128 C186,194 70,194 24,128Z"), F(C(128, 128, 40), "#65C8C5"), F(C(128, 128, 18), INK)],
 "noise":    [F("M40,104 H82 L130,64 V192 L82,152 H40Z"), L("M156,96 C172,112 172,144 156,160 M180,72 C212,104 212,152 180,184", 16, YEL)],
 "card":     [F(RR(64, 30, 128, 176, 18), MOON), F(RR(80, 46, 96, 70, 10), "#9ED9D6"), L("M84,140 H172 M84,164 H150", 10, INK)],
 "hand":     [F("M84,226 C66,190 60,160 72,148 C82,138 96,146 104,160 V60 C104,42 132,42 132,60 V128 V44 C132,26 160,26 160,44 V128 V60 C160,42 188,42 188,60 V136 V86 C188,70 212,70 212,86 V170 C212,204 190,226 160,226Z")],
 "timer":    [F(C(128, 140, 88), YEL), F("M128,140 V52 A88,88 0 0,1 204,96Z", CORAL), F(RR(110, 24, 36, 24, 8), PURP)],
 "penalty":  [F(RR(28, 128, 200, 52, 14), PURP), F(RR(28, 96, 30, 112, 10)), F(RR(62, 110, 52, 26, 12)), L("M140,40 H176 L140,82 H176 M186,24 H210 L186,52 H210", 12, MOON)],
 "witness":  [F(RR(48, 40, 160, 176, 24), MOON), F("M68,128 C92,92 164,92 188,128 C164,164 92,164 68,128Z"), F(C(128, 128, 20), INK)],
 "sabotage": [F("M70,58 L150,46 L162,128 C164,146 150,158 134,160 L106,164 C90,166 76,156 74,138Z", "#9ED9D6"), F("M120,170 C150,176 210,168 226,196 C214,220 150,222 110,214 C80,208 84,176 120,170Z", "#9ED9D6"), F(C(186, 120, 10), "#9ED9D6"), F(C(206, 146, 7), "#9ED9D6"), L("M40,96 L56,104 M36,128 L54,128", 10, YEL)],
 "repair":   [F("M58,198 L150,106 C140,74 160,40 196,44 L172,70 L186,84 L212,60 C218,96 184,118 154,110 L62,202 C54,210 50,206 58,198Z", "#9ED9D6")],
 "quiet":    [F("M40,104 H82 L130,64 V192 L82,152 H40Z"), L("M160,100 L212,152 M212,100 L160,152", 16, CORAL)],
}

def svg(name, els):
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"><title>SHHH! icon {name}</title>',
           '<g fill="none" stroke-linecap="round" stroke-linejoin="round">']
    # pass 1: outline (ink) under everything
    for k, d, c, w in els:
        sw = 2 * O if k == "fill" else w + 2 * O
        out.append(f'<path d="{d}" fill="{INK if k == "fill" else "none"}" stroke="{INK}" stroke-width="{sw}"/>')
    # pass 2: colour
    for k, d, c, w in els:
        out.append(f'<path d="{d}" fill="{c}"/>' if k == "fill" else f'<path d="{d}" fill="none" stroke="{c}" stroke-width="{w}"/>')
    # pass 3: soft top highlight on fills (cartoon sheen)
    out.append('</g></svg>')
    return "\n".join(out)

os.makedirs(OUT, exist_ok=True)
for n, e in I.items():
    open(os.path.join(OUT, f"icon_{n}.svg"), "w").write(svg(n, e))
print(len(I), "icons ->", OUT)
