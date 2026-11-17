# SHHH! V12 original clothes models (owner 2026-11-10: "clothes are not look clothes").
# Headless Blender: real cloth simulation drapes child-size garments (a T-shirt, a towel) over a soft lumpy core so the result has
# genuine fabric folds, then exports FBX for Unity (Y-up, -Z forward). Original geometry made here - no third-party asset.
#   blender -b -P Tools/blender/make_clothes.py -- <out_dir>
# Outputs:
#   clothes_bundle.fbx  : CORE_Bundle (lumpy rolled clothes) + CLOTH_Towel + CLOTH_Shirt draped over it (laundry_bundle / wet_bundle)
#   clothes_shirt_crumpled.fbx : a single T-shirt dropped on the floor and crumpled (loose shirts / dirty clothes)
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
out = argv[0] if argv else os.path.join(os.getcwd(), 'clothes_out')
os.makedirs(out, exist_ok=True)
random.seed(11)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene; sc.frame_start = 1; sc.frame_end = 80
    return sc

def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1.0); return m

def inside(poly, x, y):
    c = False; j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]; xj, yj = poly[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi): c = not c
        j = i
    return c

def grid_shape(name, poly, w, h, nx, ny, material):
    """Quad grid w x h (centred) with faces kept only inside the outline polygon; UV = flat garment layout."""
    me = bpy.data.meshes.new(name); bm = bmesh.new()
    vs = [[bm.verts.new((-w / 2 + w * i / nx, -h / 2 + h * j / ny, 0.0)) for j in range(ny + 1)] for i in range(nx + 1)]
    uv = bm.loops.layers.uv.new('UVMap')
    for i in range(nx):
        for j in range(ny):
            cx = -w / 2 + w * (i + 0.5) / nx; cy = -h / 2 + h * (j + 0.5) / ny
            if poly is not None and not inside(poly, cx, cy): continue
            f = bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
            for l in f.loops: l[uv].uv = ((l.vert.co.x + w / 2) / w, (l.vert.co.y + h / 2) / h)
    for v in [v for v in bm.verts if not v.link_faces]: bm.verts.remove(v)
    if poly is not None:
        # snap the stair-stepped grid border onto the real garment outline (clean hems, no jagged pixels)
        segs = [(Vector((*poly[i], 0)), Vector((*poly[(i + 1) % len(poly)], 0))) for i in range(len(poly))]
        for v in [v for v in bm.verts if v.is_boundary]:
            best = None; bd = 1e9
            for a, b in segs:
                ab = b - a; t = max(0.0, min(1.0, (v.co - a).dot(ab) / max(1e-9, ab.dot(ab)))); q = a + ab * t; d = (q - v.co).length
                if d < bd: bd = d; best = q
            v.co = best
        for f in bm.faces:
            for l in f.loops: l[uv].uv = ((l.vert.co.x + w / 2) / w, (l.vert.co.y + h / 2) / h)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob); ob.data.materials.append(material)
    return ob

def lumpy_core(name, r, material):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=4, radius=r)
    for v in bm.verts:
        n = noise.noise(v.co * 14.0) * 0.18 + noise.noise(v.co * 30.0) * 0.06
        v.co *= 1.0 + n; v.co.z *= 0.72
    uv = bm.loops.layers.uv.new('UVMap')
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co.normalized(); l[uv].uv = (0.5 + math.atan2(p.y, p.x) / (2 * math.pi), 0.5 + math.asin(max(-1, min(1, p.z))) / math.pi)
    for f in bm.faces: f.smooth = True
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob); ob.data.materials.append(material)
    ob.location = (0, 0, r * 0.72)
    return ob

def collider(ob, thick=0.004):
    m = ob.modifiers.new('Collision', 'COLLISION'); ob.collision.thickness_outer = thick; ob.collision.cloth_friction = 40.0

def cloth(ob, mass=0.12, tension=6.0, bending=0.08, shrink=0.0):
    m = ob.modifiers.new('Cloth', 'CLOTH'); s = m.settings
    s.shrink_min = shrink   # > 0: the fabric contracts and hugs what it lies on (a wrapped bundle)
    s.quality = 7; s.mass = mass; s.tension_stiffness = tension; s.compression_stiffness = tension; s.shear_stiffness = tension * 0.8
    s.bending_stiffness = bending; s.air_damping = 1.2
    c = m.collision_settings; c.friction = 40.0; c.use_collision = True; c.distance_min = 0.003; c.use_self_collision = True; c.self_distance_min = 0.003; c.collision_quality = 4
    m.point_cache.frame_start = 1; m.point_cache.frame_end = 80
    return m

def simulate(sc, frames):
    for f in range(1, frames + 1): sc.frame_set(f)

def bake_apply(ob, thick=0.004):
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.modifier_apply(modifier='Cloth')
    sm = ob.modifiers.new('Smooth', 'CORRECTIVE_SMOOTH'); sm.factor = 0.6; sm.iterations = 6; sm.use_only_smooth = True
    bpy.ops.object.modifier_apply(modifier='Smooth')   # soft-sculpted folds, not crinkly noise
    sol = ob.modifiers.new('Solidify', 'SOLIDIFY'); sol.thickness = thick; sol.offset = 0.0
    bpy.ops.object.modifier_apply(modifier='Solidify')
    bpy.ops.object.shade_smooth()
    ob.select_set(False)

SHIRT = [(-0.15, -0.23), (0.15, -0.23), (0.15, 0.10), (0.25, 0.05), (0.30, 0.14), (0.17, 0.25), (0.065, 0.25), (0.0, 0.21),
         (-0.065, 0.25), (-0.17, 0.25), (-0.30, 0.14), (-0.25, 0.05), (-0.15, 0.10)]

def export(path):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', path_mode='STRIP', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)

# ---------------------------------------------------------------- 1. laundry bundle: core + towel + shirt draped over it
sc = reset()
ground = grid_shape('ground', None, 1.2, 1.2, 2, 2, mat('ground', (0.5, 0.5, 0.5))); collider(ground)
core = lumpy_core('CORE_Bundle', 0.095, mat('fabric_core', (0.85, 0.62, 0.55))); collider(core, 0.003)
towel = grid_shape('CLOTH_Towel', None, 0.34, 0.24, 34, 24, mat('fabric_towel', (0.38, 0.72, 0.78)))
towel.location = (0.0, 0.0, 0.23); towel.rotation_euler = (0.0, 0.0, 0.7)
cloth(towel, mass=0.18, tension=10.0, bending=0.4, shrink=0.1)
simulate(sc, 60); bake_apply(towel, 0.005); collider(towel, 0.003)
shirt = grid_shape('CLOTH_Shirt', SHIRT, 0.62, 0.50, 50, 40, mat('fabric_shirt', (0.98, 0.84, 0.36)))
shirt.location = (0.0, 0.02, 0.29); shirt.rotation_euler = (0.0, 0.0, -0.5)
for o in (core, towel): o.modifiers['Collision'].show_viewport = True
cloth(shirt, mass=0.12, tension=6.0, bending=0.06, shrink=0.12)
sc.frame_set(1); simulate(sc, 70); bake_apply(shirt, 0.004)
for o in (towel, core):
    for m in list(o.modifiers): o.modifiers.remove(m)
bpy.data.objects.remove(ground, do_unlink=True)
export(os.path.join(out, 'clothes_bundle.fbx'))
print('CLOTHES_OK bundle', [(o.name, len(o.data.vertices)) for o in bpy.context.scene.objects])

# ---------------------------------------------------------------- 2. a single crumpled T-shirt (dropped on the floor)
sc = reset()
ground = grid_shape('ground', None, 1.2, 1.2, 2, 2, mat('ground', (0.5, 0.5, 0.5))); collider(ground)
lump = lumpy_core('lump', 0.05, mat('x', (1, 1, 1))); collider(lump, 0.003); lump.location = (0.03, 0.02, 0.02)
shirt = grid_shape('CLOTH_Shirt', SHIRT, 0.62, 0.50, 50, 40, mat('fabric_shirt', (0.98, 0.84, 0.36)))
shirt.location = (0, 0, 0.22); shirt.rotation_euler = (0.5, 0.25, 0.3)
cloth(shirt, mass=0.12, tension=6.0, bending=0.05)
simulate(sc, 70); bake_apply(shirt, 0.004)
bpy.data.objects.remove(ground, do_unlink=True); bpy.data.objects.remove(lump, do_unlink=True)
export(os.path.join(out, 'clothes_shirt_crumpled.fbx'))
print('CLOTHES_OK shirt', [(o.name, len(o.data.vertices)) for o in bpy.context.scene.objects])
