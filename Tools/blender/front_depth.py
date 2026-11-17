# Front protrusion map (min y per x/z cell, Blender -Y = model front) of a mesh: locates doors, drawers, knobs, buttons by region.
import bpy, bmesh, sys, os, math
argv = sys.argv[sys.argv.index("--") + 1:]
out = open(argv[0], "w", encoding="utf-8"); step = float(argv[1])
for path in argv[2:]:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=path)
    o = [x for x in bpy.context.scene.objects if x.type == 'MESH'][0]; mw = o.matrix_world
    pts = [mw @ v.co for v in o.data.vertices]
    # faces: sample centroids too (big flat faces have few verts)
    for p in o.data.polygons: pts.append(mw @ p.center)
    xs = [p.x for p in pts]; zs = [p.z for p in pts]
    x0, x1, z0, z1 = min(xs), max(xs), min(zs), max(zs)
    nx, nz = int((x1 - x0) / step) + 1, int((z1 - z0) / step) + 1
    grid = [[None] * nx for _ in range(nz)]
    for p in pts:
        i, k = int((p.x - x0) / step), int((p.z - z0) / step)
        if grid[k][i] is None or p.y < grid[k][i]: grid[k][i] = p.y
    ys = [p.y for p in pts]; front = min(ys)
    out.write(f"## {os.path.basename(path)} x[{x0:.3f},{x1:.3f}] z[{z0:.3f},{z1:.3f}] frontmost y {front:.3f}; char = mm in front of y=-0.295 plane\n")
    for k in range(nz - 1, -1, -1):
        row = ""
        for i in range(nx):
            y = grid[k][i]
            if y is None: row += " "
            else:
                d = (-0.295 - y) * 1000.0
                row += "." if d < 1 else "1" if d < 6 else "2" if d < 12 else "3" if d < 20 else "4" if d < 28 else "5"
        out.write(f"{z0 + k * step:5.2f} {row}\n")
out.close()
