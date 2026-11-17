# Weld coincident vertices, then report islands (parts) of each mesh with bounds - used to locate movable parts in triangle-soup FBX.
import bpy, bmesh, sys, os
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
out = open(argv[0], "w", encoding="utf-8")
for path in argv[1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=path)
    for o in [x for x in bpy.context.scene.objects if x.type == 'MESH']:
        bm = bmesh.new(); bm.from_mesh(o.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
        bm.verts.ensure_lookup_table(); seen = set(); isl = []
        for v in bm.verts:
            if v.index in seen: continue
            st = [v]; comp = []
            while st:
                x = st.pop()
                if x.index in seen: continue
                seen.add(x.index); comp.append(x); st.extend(e.other_vert(x) for e in x.link_edges)
            isl.append(comp)
        mw = o.matrix_world
        out.write(f"## {os.path.basename(path)} '{o.name}' welded verts={len(bm.verts)} islands={len(isl)}\n")
        for comp in sorted(isl, key=len, reverse=True)[:30]:
            p = [mw @ v.co for v in comp]
            a = Vector((min(q.x for q in p), min(q.y for q in p), min(q.z for q in p))); b = Vector((max(q.x for q in p), max(q.y for q in p), max(q.z for q in p)))
            out.write(f"   {len(comp):5d}v min={tuple(round(c,3) for c in a)} max={tuple(round(c,3) for c in b)}\n")
        bm.free()
out.close()
