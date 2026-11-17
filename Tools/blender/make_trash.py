# SHHH! V12 original trash props (chunky hand-made toon shapes; references: common CC0 garbage packs - shapes only, nothing copied).
#   blender -b -P Tools/blender/make_trash.py -- <out_dir>
# One FBX per piece (Y-up, -Z forward), origin at the bottom centre, real-world size in metres:
#   trash_paper_ball, trash_tissue, trash_can_crushed, trash_banana_peel, trash_chip_bag, trash_pizza_box, trash_bag_full, trash_juice_box
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, noise
argv = sys.argv[sys.argv.index('--') + 1:]
out = argv[0]; os.makedirs(out, exist_ok=True)
random.seed(5)

def reset(): bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1.0); return m
def obj(name, bm, material, smooth=True):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    if smooth:
        for p in me.polygons: p.use_smooth = True
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); o.data.materials.append(material); return o
def ground(o):
    """origin at bottom centre"""
    mn = Vector((min(v.co.x for v in o.data.vertices), min(v.co.y for v in o.data.vertices), min(v.co.z for v in o.data.vertices)))
    mx = Vector((max(v.co.x for v in o.data.vertices), max(v.co.y for v in o.data.vertices), max(v.co.z for v in o.data.vertices)))
    off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    for v in o.data.vertices: v.co -= off
def uv_box(bm, s=4.0):
    uv = bm.loops.layers.uv.new('UVMap')
    for f in bm.faces:
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co; a, b = [(c.y, c.z), (c.x, c.z), (c.x, c.y)][ax]; l[uv].uv = (a * s, b * s)
def export(name, objs):
    for o in bpy.context.scene.objects: o.select_set(o in objs)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + '.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', path_mode='STRIP', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
    print('TRASH_OK', name, [(o.name, len(o.data.vertices)) for o in objs])

def crumple(r, amount, seed, squash=1.0, subd=3):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=subd, radius=r)
    o = Vector((seed, seed * 1.7, seed * 0.3))
    for v in bm.verts:
        d = v.co.normalized()
        k = 1 + amount * (noise.noise(d * 3.1 + o) * 0.8 + abs(noise.noise(d * 7.3 + o)) * 0.9 - 0.25)
        v.co = d * r * k; v.co.z *= squash
    uv_box(bm); return bm

# 1. crumpled paper ball + tissue
reset(); o = obj('trash_paper_ball', crumple(0.035, 0.42, 1.0, 0.92, 3), mat('paper_cream', (0.96, 0.93, 0.84)), smooth=False); ground(o); export('trash_paper_ball', [o])
reset(); o = obj('trash_tissue', crumple(0.028, 0.5, 4.0, 0.7, 3), mat('tissue_white', (0.98, 0.98, 1.0)), smooth=False); ground(o); export('trash_tissue', [o])

# 2. crushed soda can (pinched waist, dented, tilted lid)
reset()
bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=20, radius1=0.033, radius2=0.033, depth=0.12)
bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.05], cuts=8, use_grid_fill=False)
for v in bm.verts:
    z = v.co.z / 0.06; pinch = 1 - 0.38 * math.exp(-(z * 2.2) ** 2)
    v.co.x *= pinch * (1 + 0.12 * noise.noise(v.co * 60)); v.co.y *= pinch * 0.82 * (1 + 0.12 * noise.noise(v.co * 60 + Vector((3, 0, 0))))
    v.co.z *= 0.78
uv_box(bm, 8)
can = obj('trash_can_crushed', bm, mat('can_red', (0.86, 0.22, 0.2)))
bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=0.031, radius2=0.025, depth=0.008)
for v in bm.verts: v.co.z += 0.05
lid = obj('can_lid', bm, mat('can_silver', (0.8, 0.82, 0.86)))
for v in can.data.vertices: v.co.z += 0.047
ground(can); [setattr(v, 'co', v.co + Vector((0, 0, 0.0))) for v in lid.data.vertices]
export('trash_can_crushed', [can, lid])

# 3. banana peel: 4 floppy petals from a stem
reset()
bm = bmesh.new()
for i in range(4):
    a = i * math.pi / 2 + 0.3
    pts = []
    for k in range(9):
        t = k / 8; w = 0.016 * math.sin(math.pi * min(1, t * 1.15)) + 0.004
        r = 0.012 + t * 0.085; h = 0.03 * (1 - t) ** 1.5 + 0.004 * math.sin(t * 9)
        c = Vector((math.cos(a) * r, math.sin(a) * r, h)); side = Vector((-math.sin(a), math.cos(a), 0))
        pts.append((c - side * w, c + side * w))
    vs = [(bm.verts.new(p[0]), bm.verts.new(p[1])) for p in pts]
    for k in range(8): bm.faces.new((vs[k][0], vs[k + 1][0], vs[k + 1][1], vs[k][1]))
bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.011, radius2=0.006, depth=0.05, matrix=__import__('mathutils').Matrix.Translation((0, 0, 0.045)))
uv_box(bm)
o = obj('trash_banana_peel', bm, mat('banana_yellow', (0.98, 0.84, 0.26)))
sol = o.modifiers.new('s', 'SOLIDIFY'); sol.thickness = 0.004
bpy.context.view_layer.objects.active = o; o.select_set(True); bpy.ops.object.modifier_apply(modifier='s'); ground(o); export('trash_banana_peel', [o])

# 4. chip bag: pillow with crimped ends, crinkled
reset()
bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=6, use_grid_fill=True)
for v in bm.verts:
    x, y, z = v.co.x, v.co.y, v.co.z
    puff = 1 - (2 * y) ** 4
    v.co = Vector((x * 0.13, y * 0.19, z * 0.07 * max(0.12, puff)))
    v.co += Vector((0, 0, 1)) * noise.noise(v.co * 70) * 0.004
uv_box(bm)
o = obj('trash_chip_bag', bm, mat('chips_orange', (0.95, 0.55, 0.18)), smooth=False); ground(o); export('trash_chip_bag', [o])

# 5. pizza box (lid a little open, grease spot as a darker inset)
reset()
def box(name, mn, mx, m):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((mn[i] + (v.co[i] + 0.5) * (mx[i] - mn[i]) for i in range(3)))
    uv_box(bm); return obj(name, bm, m, smooth=False)
card = mat('cardboard', (0.82, 0.62, 0.38))
base = box('trash_pizza_box', (-0.15, -0.15, 0), (0.15, 0.15, 0.035), card)
lid = box('pizza_lid', (-0.15, -0.15, 0.0), (0.15, 0.15, 0.006), card)
lid.location = (0, 0.15, 0.035); lid.rotation_euler = (math.radians(-18), 0, 0)
for v in lid.data.vertices: v.co.y -= 0.15
spot = box('grease', (-0.05, -0.04, 0.0355), (0.04, 0.05, 0.0365), mat('grease', (0.62, 0.42, 0.22)))
export('trash_pizza_box', [base, lid, spot])

# 6. full trash bag: lumpy sack + tied neck + ears
reset()
bm = crumple(0.16, 0.18, 9.0, 1.15, 3)
for v in bm.verts:
    if v.co.z > 0.1: v.co.x *= 1 - (v.co.z - 0.1) * 3.2; v.co.y *= 1 - (v.co.z - 0.1) * 3.2
sack = obj('trash_bag_full', bm, mat('bag_plum', (0.25, 0.22, 0.32)))
bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.03, radius2=0.012, depth=0.06, matrix=__import__('mathutils').Matrix.Translation((0, 0, 0.2)))
for i, sx in enumerate((-1, 1)):
    bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.016, radius2=0.004, depth=0.07,
                          matrix=__import__('mathutils').Matrix.Translation((sx * 0.03, 0, 0.245)) @ __import__('mathutils').Matrix.Rotation(sx * 0.9, 4, 'Y'))
uv_box(bm)
neck = obj('bag_knot', bm, mat('bag_plum', (0.25, 0.22, 0.32)))
ground(sack); export('trash_bag_full', [sack, neck])

# 7. squashed juice box with straw
reset()
jb = box('trash_juice_box', (-0.03, -0.02, 0), (0.03, 0.02, 0.09), mat('juice_green', (0.45, 0.78, 0.4)))
for v in jb.data.vertices:
    if v.co.z > 0.05: v.co.y *= 0.6
st = box('straw', (-0.004, -0.004, 0.085), (0.004, 0.004, 0.125), mat('straw_pink', (0.98, 0.5, 0.65)))
export('trash_juice_box', [jb, st])
