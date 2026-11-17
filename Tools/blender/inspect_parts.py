# Headless Blender: report objects / mesh islands / materials / bounds for FBX files (V12 asset audit).
# blender -b -P inspect_parts.py -- out.txt file1.fbx file2.fbx ...
import bpy, bmesh, sys, os
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
out = open(argv[0], "w", encoding="utf-8")
for path in argv[1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try: bpy.ops.import_scene.fbx(filepath=path)
    except Exception as e: out.write(f"## {os.path.basename(path)} IMPORT FAILED {e}\n"); continue
    out.write(f"## {os.path.basename(path)}\n")
    for o in bpy.context.scene.objects:
        line = f"  {o.type} '{o.name}' parent={o.parent.name if o.parent else '-'}"
        if o.type == 'MESH':
            me = o.data; bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
            # islands
            seen = set(); islands = []
            for v in bm.verts:
                if v.index in seen: continue
                stack = [v]; comp = []
                while stack:
                    x = stack.pop()
                    if x.index in seen: continue
                    seen.add(x.index); comp.append(x)
                    for e in x.link_edges: stack.append(e.other_vert(x))
                islands.append(comp)
            mw = o.matrix_world
            ws = [mw @ v.co for v in bm.verts]
            mn = Vector((min(p.x for p in ws), min(p.y for p in ws), min(p.z for p in ws))); mx = Vector((max(p.x for p in ws), max(p.y for p in ws), max(p.z for p in ws)))
            line += f" verts={len(bm.verts)} islands={len(islands)} mats={[m.name for m in me.materials]} bbox_min={tuple(round(c,3) for c in mn)} bbox_max={tuple(round(c,3) for c in mx)}"
            big = sorted(islands, key=len, reverse=True)[:12]
            for comp in big:
                pts = [mw @ v.co for v in comp]
                a = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))); b = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
                line += f"\n      island {len(comp)}v min={tuple(round(c,3) for c in a)} max={tuple(round(c,3) for c in b)}"
            bm.free()
        out.write(line + "\n")
out.close()
