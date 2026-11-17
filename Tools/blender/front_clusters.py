# Cluster faces lying in front of a plane (y < Y0) into parts by xz proximity; print bbox, face count, mean depth.
import bpy, sys, os
argv = sys.argv[sys.argv.index("--") + 1:]
out = open(argv[0], "w", encoding="utf-8"); y0 = float(argv[1]); link = float(argv[2])
for path in argv[3:]:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=path)
    o = [x for x in bpy.context.scene.objects if x.type == 'MESH'][0]; mw = o.matrix_world
    fs = []
    for p in o.data.polygons:
        c = mw @ p.center
        if c.y < y0: fs.append((c.x, c.y, c.z))
    # single-link clustering on a grid
    cell = {}; 
    for i, f in enumerate(fs): cell.setdefault((int(f[0] / link), int(f[2] / link)), []).append(i)
    parent = list(range(len(fs)))
    def find(a):
        while parent[a] != a: parent[a] = parent[parent[a]]; a = parent[a]
        return a
    for (cx, cz), idx in cell.items():
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                for j in cell.get((cx + dx, cz + dz), []):
                    for i in idx:
                        if abs(fs[i][0] - fs[j][0]) <= link and abs(fs[i][2] - fs[j][2]) <= link: parent[find(i)] = find(j)
    groups = {}
    for i in range(len(fs)): groups.setdefault(find(i), []).append(fs[i])
    out.write(f"## {os.path.basename(path)} faces in front of y={y0}: {len(fs)} -> {len(groups)} clusters\n")
    for g in sorted(groups.values(), key=len, reverse=True)[:16]:
        xs = [f[0] for f in g]; ys = [f[1] for f in g]; zs = [f[2] for f in g]
        out.write(f"   {len(g):4d} faces x[{min(xs):.3f},{max(xs):.3f}] z[{min(zs):.3f},{max(zs):.3f}] y[{min(ys):.3f},{max(ys):.3f}]\n")
out.close()
