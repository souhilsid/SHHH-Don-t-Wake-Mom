# SHHH! V12 original HUD icons (pets + tasks), drawn as SVG in the house sticker style (thick plum outline, cream/painted fills,
# soft highlight), exported to 256 px PNG with Inkscape. Original artwork made here - no third-party icons.
#   python Tools/icons/make_icons.py <out_svg_dir> <out_png_dir> [inkscape.exe]
import os, sys, subprocess
out_svg, out_png = sys.argv[1], sys.argv[2]
ink = sys.argv[3] if len(sys.argv) > 3 else r"C:\Program Files\Inkscape\bin\inkscape.exe"
os.makedirs(out_svg, exist_ok=True); os.makedirs(out_png, exist_ok=True)
OL = '#2A1538'      # outline plum (matches UI_V8 icons)
CREAM = '#FFF5E1'; SHADE = '#F2DDBB'
def svg(body):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">
<defs><filter id="soft"><feGaussianBlur stdDeviation="2.2"/></filter></defs>
<g stroke="{OL}" stroke-width="13" stroke-linejoin="round" stroke-linecap="round">{body}</g></svg>'''
def hl(x, y, rx, ry, rot=-25):   # painted highlight
    return f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" transform="rotate({rot} {x} {y})" fill="#FFFFFF" fill-opacity="0.55" stroke="none" filter="url(#soft)"/>'
def bowl(fill, food=''):
    return (f'<path d="M40 150 Q128 135 216 150 L196 206 Q128 226 60 206 Z" fill="{fill}"/>' + food +
            f'<path d="M34 150 Q128 128 222 150" fill="none"/>' + hl(78, 178, 22, 8))
I = {}
I['pet_hungry_dog'] = svg(bowl('#E8695A') + '<path d="M78 112 a16 16 0 1 1 22 -14 L156 98 a16 16 0 1 1 22 14 a16 16 0 1 1 -22 14 L100 126 a16 16 0 1 1 -22 -14 Z" fill="' + CREAM + '"/>' + hl(120, 104, 16, 5, 0))
I['pet_hungry_cat'] = svg(bowl('#5BA4D9') + '<path d="M66 108 Q112 70 158 108 Q112 146 66 108 Z" fill="#F7B267"/><path d="M158 108 L196 82 L190 108 L196 134 Z" fill="#F7B267"/><circle cx="90" cy="104" r="5" fill="' + OL + '" stroke="none"/>' + hl(110, 96, 14, 5, -10))
I['pet_water'] = svg(bowl('#7FC8A9', '<path d="M62 152 Q128 140 194 152" fill="none" stroke="#9ED8F5" stroke-width="10"/>') + '<path d="M128 46 Q92 96 128 116 Q164 96 128 46 Z" fill="#7CC6F2"/>' + hl(118, 88, 8, 13, 15))
I['pet_fed'] = svg('<path d="M128 214 C60 168 30 132 52 92 C72 56 116 62 128 96 C140 62 184 56 204 92 C226 132 196 168 128 214 Z" fill="#FF7A9A"/>' + hl(88, 104, 20, 11))
I['pet_alert'] = svg('<path d="M128 28 L232 214 L24 214 Z" fill="#FFC84A"/><path d="M128 92 L128 150" stroke-width="20"/><circle cx="128" cy="184" r="9" fill="' + OL + '"/>' + hl(104, 120, 10, 26, 25))
I['pet_food_bag'] = svg('<path d="M62 70 L194 70 L210 214 L46 214 Z" fill="#E59A4C"/><path d="M62 70 Q128 40 194 70" fill="' + SHADE + '"/><circle cx="128" cy="146" r="32" fill="' + CREAM + '"/><path d="M114 140 a6 6 0 1 1 0.1 0 M142 140 a6 6 0 1 1 0.1 0 M128 160 l-10 -8 h20 z" fill="' + OL + '" stroke-width="5"/>' + hl(80, 112, 10, 30, 8))
I['task_dishes'] = svg('<circle cx="120" cy="136" r="78" fill="' + CREAM + '"/><circle cx="120" cy="136" r="46" fill="' + SHADE + '"/><circle cx="196" cy="70" r="20" fill="#BFE6FF"/><circle cx="218" cy="112" r="12" fill="#BFE6FF"/><circle cx="170" cy="40" r="10" fill="#BFE6FF"/>' + hl(86, 98, 24, 10))
I['task_sponge'] = svg('<rect x="44" y="96" width="168" height="96" rx="22" fill="#FFD84A"/><path d="M44 130 L212 130 L212 172 Q212 192 192 192 L64 192 Q44 192 44 172 Z" fill="#6CC56C"/><circle cx="90" cy="58" r="18" fill="#BFE6FF"/><circle cx="132" cy="44" r="12" fill="#BFE6FF"/>' + hl(90, 112, 26, 7, 0))
I['task_tap'] = svg('<path d="M40 92 L150 92 Q196 92 196 138 L196 150 L160 150 L160 136 Q160 126 150 126 L40 126 Z" fill="#C9D4E8"/><rect x="88" y="54" width="40" height="38" rx="8" fill="#E8695A"/><path d="M178 166 Q162 196 178 214 Q194 196 178 166 Z" fill="#7CC6F2"/>' + hl(96, 102, 30, 6, 0))
I['task_laundry'] = svg('<path d="M86 40 L58 52 L22 96 L56 124 L72 108 L72 216 L184 216 L184 108 L200 124 L234 96 L198 52 L170 40 Q128 74 86 40 Z" fill="#7FB6F0"/><path d="M86 40 Q128 74 170 40" fill="none"/>' + hl(104, 120, 14, 36, 6))
I['task_washer'] = svg('<rect x="40" y="28" width="176" height="204" rx="22" fill="' + CREAM + '"/><path d="M40 76 L216 76" /><circle cx="128" cy="152" r="56" fill="#9ED8F5"/><circle cx="128" cy="152" r="30" fill="#E59A9A"/><circle cx="178" cy="52" r="8" fill="#E8695A"/><rect x="62" y="44" width="54" height="16" rx="5" fill="' + SHADE + '" stroke-width="7"/>' + hl(108, 130, 16, 8))
I['task_towel'] = svg('<rect x="36" y="120" width="184" height="44" rx="12" fill="#5BA4D9"/><rect x="48" y="78" width="160" height="44" rx="12" fill="#FFB2C8"/><rect x="60" y="164" width="136" height="44" rx="12" fill="#FFD84A"/><path d="M70 92 L186 92" stroke="#FFFFFF" stroke-opacity="0.6" stroke-width="6"/>' + hl(84, 132, 24, 6, 0))
I['task_lint'] = svg('<path d="M60 150 Q48 100 92 92 Q100 52 144 64 Q186 54 196 98 Q228 116 206 156 Q206 200 160 196 Q128 222 96 196 Q52 200 60 150 Z" fill="#C8B9D9"/><path d="M90 140 q20 -20 40 0 q20 20 40 0" fill="none" stroke-width="8"/>' + hl(104, 104, 22, 9))
I['task_trash'] = svg('<path d="M66 92 L190 92 L176 218 L80 218 Z" fill="#6B7A8F"/><rect x="50" y="68" width="156" height="26" rx="10" fill="#8C9BB0"/><path d="M106 68 Q128 36 150 68" fill="none"/><path d="M100 120 L106 196 M128 120 L128 196 M156 120 L150 196" stroke-width="8"/>' + hl(96, 108, 22, 6, 0))
I['task_trash_bag'] = svg('<path d="M128 70 Q60 90 52 160 Q50 214 128 216 Q206 214 204 160 Q196 90 128 70 Z" fill="#4C5566"/><path d="M106 62 Q128 82 150 62 L160 36 L128 52 L96 36 Z" fill="#4C5566"/>' + hl(92, 128, 16, 30, 20))
I['task_toys'] = svg('<rect x="34" y="122" width="88" height="88" rx="12" fill="#E8695A"/><rect x="130" y="122" width="88" height="88" rx="12" fill="#5BA4D9"/><rect x="82" y="34" width="88" height="88" rx="12" fill="#FFD84A"/><path d="M110 64 L142 64 M126 50 L126 92" stroke-width="10"/>' + hl(60, 146, 14, 6, 0))
I['task_sweep'] = svg('<path d="M176 24 L118 142" stroke-width="16"/><path d="M96 128 L146 154 L128 222 L44 182 Z" fill="#FFD84A"/><path d="M70 186 L94 150 M96 198 L114 162 M118 208 L130 170" stroke-width="7"/><circle cx="200" cy="200" r="10" fill="' + SHADE + '"/><circle cx="222" cy="176" r="7" fill="' + SHADE + '"/>' + hl(108, 160, 12, 6, 25))
I['task_wipe'] = svg('<path d="M44 96 Q90 70 140 92 Q190 112 216 86 L216 176 Q176 206 128 184 Q80 162 44 188 Z" fill="#7FC8A9"/><path d="M60 120 Q100 104 140 122 Q178 138 204 120" fill="none" stroke-width="7" stroke-opacity="0.6"/><path d="M30 214 L74 214 M98 226 L170 226" stroke="#9ED8F5" stroke-width="10"/>' + hl(92, 108, 22, 7, -10))
I['task_pour'] = svg('<path d="M70 80 L150 80 L162 214 L58 214 Z" fill="' + CREAM + '"/><path d="M150 94 Q206 98 196 150 L160 150" fill="none"/><path d="M62 130 L158 130 L162 214 L58 214 Z" fill="#7CC6F2"/><path d="M70 80 L48 58" stroke-width="12"/>' + hl(86, 104, 10, 18, 0))
I['task_detergent'] = svg('<path d="M74 82 L170 82 Q194 82 194 106 L194 204 Q194 222 176 222 L68 222 Q50 222 50 204 L50 106 Q50 82 74 82 Z" fill="#5B7BF0"/><rect x="96" y="42" width="44" height="40" rx="8" fill="' + CREAM + '"/><rect x="74" y="128" width="96" height="58" rx="10" fill="' + CREAM + '"/><path d="M96 158 q12 -14 26 0 q12 14 26 0" fill="none" stroke-width="7"/>' + hl(70, 120, 9, 26, 0))
I['task_drawer'] = svg('<rect x="32" y="40" width="192" height="176" rx="14" fill="#D9A066"/><path d="M32 128 L224 128" /><rect x="40" y="136" width="176" height="64" rx="8" fill="#E8B680" transform="translate(0 16)"/><path d="M106 168 L150 168" stroke-width="16" transform="translate(0 16)"/><path d="M106 84 L150 84" stroke-width="16"/>' + hl(70, 66, 18, 6, 0))
I['task_find'] = svg('<circle cx="108" cy="108" r="66" fill="#BFE6FF"/><path d="M156 156 L218 218" stroke-width="30"/><path d="M156 156 L218 218" stroke="#E8695A" stroke-width="14"/>' + hl(84, 84, 22, 12))
I['task_feed'] = svg('<path d="M40 150 Q128 135 216 150 L196 206 Q128 226 60 206 Z" fill="#E59A4C"/><circle cx="96" cy="140" r="14" fill="#B5652D"/><circle cx="128" cy="132" r="14" fill="#B5652D"/><circle cx="160" cy="140" r="14" fill="#B5652D"/><circle cx="112" cy="116" r="13" fill="#B5652D"/><circle cx="146" cy="114" r="13" fill="#B5652D"/><path d="M128 36 L128 84 M106 62 L128 84 L150 62" stroke-width="14"/>' + hl(80, 178, 20, 7))
I['task_carry'] = svg('<rect x="58" y="70" width="140" height="110" rx="10" fill="#D9A066"/><path d="M58 104 L198 104 M128 70 L128 104"/><path d="M40 196 Q60 176 92 182 L150 182 Q166 182 166 196 Q166 210 150 210 L120 210 L176 210 Q200 210 214 196" fill="' + CREAM + '"/>' + hl(86, 88, 18, 6, 0))
I['task_done'] = svg('<circle cx="128" cy="128" r="98" fill="#6CC56C"/><path d="M80 130 L114 164 L178 96" fill="none" stroke-width="22"/>' + hl(92, 84, 26, 12))
for name, s in I.items():
    p = os.path.join(out_svg, name + '.svg'); open(p, 'w', encoding='utf-8').write(s)
    png = os.path.join(out_png, 'icon_' + name + '.png')
    r = subprocess.run([ink, p, '--export-type=png', '--export-filename=' + png, '--export-width=256'], capture_output=True, text=True)
    print('ICON', name, 'ok' if os.path.exists(png) else 'FAIL ' + r.stderr[-200:])
