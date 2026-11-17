# SHHH! V13 original kitchen models (owner 2026-11-17: "putting the food in the fridge does not exist, so do it. Model the fridge ...
# stylized, even the food: pizza, burgers, fruits, juice"). Chunky storybook-toon shapes, soft bevels, flat pastel colours.
#   blender -b -P Tools/blender/make_fridge_food.py -- <out_dir>
# Conventions (ArticulatedModel): Z-up in Blender, exported Y-up / -Z forward; FRONT empty in front of the model; PART_* empties are
# real pivots with their meshes parented; COL_* meshes become hidden BoxColliders; SOCKET_* empties mark slots / the inside volume.
#   sk_fridge.fbx  : body shell (hollow fridge compartment, solid freezer), 2 shelves, PART_Door (hinge on the right, swings out),
#                    SOCKET_Slot0..5 (2 per level: floor, shelf 1, shelf 2), SOCKET_InsideMin / SOCKET_InsideMax, COL_* walls/shelves/door
#   sk_food_pizza / sk_food_burger / sk_food_juice / sk_food_apple : one item each, origin at the bottom centre, real size in metres
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index('--') + 1:]
OUT = argv[0]; os.makedirs(OUT, exist_ok=True)
random.seed(17)

def reset(): bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name, rgb, rough=0.6):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1.0); m.roughness = rough
        m.use_nodes = True; b = m.node_tree.nodes.get('Principled BSDF')
        if b: b.inputs['Base Color'].default_value = (*rgb, 1.0); b.inputs['Roughness'].default_value = rough
    return m

def link(o, parent=None):
    bpy.context.collection.objects.link(o)
    if parent is not None: o.parent = parent
    return o

def mesh_obj(name, bm, material, parent=None, smooth=True, bevel=0.0, segs=3):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
    o = bpy.data.objects.new(name, me); link(o, parent)
    if material is not None: o.data.materials.append(material)
    if bevel > 0:
        md = o.modifiers.new('bevel', 'BEVEL'); md.width = bevel; md.segments = segs; md.limit_method = 'ANGLE'; md.harden_normals = False
    return o

def box(name, mn, mx, material, parent=None, bevel=0.0, segs=3):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    c = (Vector(mn) + Vector(mx)) / 2; s = Vector(mx) - Vector(mn)
    for v in bm.verts: v.co = Vector((v.co.x * s.x + c.x, v.co.y * s.y + c.y, v.co.z * s.z + c.z))
    return mesh_obj(name, bm, material, parent, smooth=bevel > 0, bevel=bevel, segs=segs)

def cyl(name, r, h, z0, material, parent=None, seg=32, x=0.0, y=0.0, r2=None, bevel=0.0):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    for v in bm.verts: v.co += Vector((x, y, z0 + h / 2))
    return mesh_obj(name, bm, material, parent, bevel=bevel, segs=2)

def sphere(name, r, center, material, parent=None, scale=(1, 1, 1), subd=3):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=subd, radius=r)
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + Vector(center)
    return mesh_obj(name, bm, material, parent)

def empty(name, loc, parent=None):
    e = bpy.data.objects.new(name, None); e.empty_display_size = 0.03; e.location = loc; return link(e, parent)

def export(name, objs=None):
    # Unity folds a single top-level node into the model root (and ArticulatedModel resets the root's rotation): export the
    # pieces as separate top-level objects instead of children of one ROOT empty
    rt = bpy.data.objects.get('ROOT')
    if rt is not None:
        for o in list(rt.children):
            o.parent = None   # ROOT sits at the origin with no rotation: the local transform IS the world transform
        bpy.data.objects.remove(rt, do_unlink=True)
    if objs is not None:
        for o in bpy.context.scene.objects: o.select_set(o in objs)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=objs is not None, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', path_mode='STRIP',
                             use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
    print('MODEL_OK', name, len([o for o in bpy.context.scene.objects if o.type == 'MESH']), 'meshes')

# ===================================================================================================== the fridge
# outer 0.76 (x) x 0.70 (y) x 1.80 (z); front toward -Y; the fridge compartment z 0.10..1.28 is hollow; freezer above it is solid
reset()
BODY = mat('fridge_body', (0.36, 0.76, 0.66), 0.45)        # mint (strong enough to read under the bright kitchen light)
INNER = mat('fridge_inside', (0.98, 0.96, 0.90), 0.5)       # creamy liner
SHELF = mat('fridge_shelf', (0.72, 0.89, 0.98), 0.2)        # glassy pale blue
RIM = mat('fridge_rim', (0.99, 0.99, 0.97), 0.4)
HANDLE = mat('fridge_handle', (0.99, 0.72, 0.22), 0.35)     # mustard accent
KICK = mat('fridge_kick', (0.20, 0.34, 0.38), 0.6)
SEAL = mat('fridge_seal', (0.62, 0.66, 0.70), 0.7)
PINK = mat('magnet_pink', (0.98, 0.45, 0.62), 0.4); YEL = mat('magnet_yellow', (1.0, 0.86, 0.25), 0.4); PAPER = mat('paper', (1.0, 0.99, 0.95), 0.8)
BLUE = mat('crayon_blue', (0.30, 0.56, 0.95), 0.6); GREEN = mat('crayon_green', (0.40, 0.78, 0.36), 0.6)

W, D, H = 0.76, 0.70, 1.80; x0, x1 = -W / 2, W / 2; yF, yB = -D / 2, D / 2
T = 0.045                     # wall thickness
z_floor, z_top = 0.10, 1.28   # fridge compartment
root = empty('ROOT', (0, 0, 0))
# shell: back, sides, base (kick), floor slab, divider, freezer block - each a soft box
box('Body_Back', (x0, yB - T, 0.0), (x1, yB, H), BODY, root, 0.02)
box('Body_Left', (x0, yF + 0.012, 0.0), (x0 + T, yB, H), BODY, root, 0.022)
box('Body_Right', (x1 - T, yF + 0.012, 0.0), (x1, yB, H), BODY, root, 0.022)
box('Body_Kick', (x0 + T, yF + 0.04, 0.0), (x1 - T, yB - T, z_floor - 0.02), KICK, root, 0.012)
box('Body_Floor', (x0 + T, yF + 0.012, z_floor - 0.02), (x1 - T, yB - T, z_floor), BODY, root, 0.006)
box('Body_Divider', (x0 + T, yF + 0.012, z_top), (x1 - T, yB - T, z_top + 0.05), BODY, root, 0.008)
box('Body_Freezer', (x0 + T, yF + 0.06, z_top + 0.05), (x1 - T, yB - T, H - 0.02), BODY, root, 0.01)
box('Body_Top', (x0, yF + 0.012, H - 0.04), (x1, yB, H), BODY, root, 0.025)
# freezer door (static, part of the body) with a short handle + magnets and a kid's drawing
box('Freezer_Door', (x0 + 0.005, yF - 0.045, z_top + 0.06), (x1 - 0.005, yF + 0.012, H - 0.015), BODY, root, 0.03)
box('Freezer_Handle', (x0 + 0.05, yF - 0.085, z_top + 0.12), (x0 + 0.09, yF - 0.04, z_top + 0.30), HANDLE, root, 0.016)
star = bmesh.new(); vs = []
for k in range(10):
    a = math.pi / 2 + k * math.pi / 5; r = 0.045 if k % 2 == 0 else 0.02
    vs.append(star.verts.new((0.16 + r * math.cos(a), yF - 0.05, 1.62 + r * math.sin(a))))
f = star.faces.new(vs); bmesh.ops.extrude_face_region(star, geom=[f]);
for v in star.verts:
    if abs(v.co.y - (yF - 0.05)) < 1e-6: pass
for v in [v for v in star.verts][:10]: v.co.y -= 0.0
for v in [v for v in star.verts][10:]: v.co.y -= 0.012
mesh_obj('Magnet_Star', star, YEL, root, smooth=False)
heart = bmesh.new(); hv = []
for k in range(24):
    t = k / 24 * 2 * math.pi; hx = 16 * math.sin(t) ** 3; hz = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
    hv.append(heart.verts.new((-0.12 + hx * 0.0022, yF - 0.05, 1.47 + hz * 0.0022)))
hf = heart.faces.new(hv); r_ = bmesh.ops.extrude_face_region(heart, geom=[hf])
for v in r_['geom']:
    if isinstance(v, bmesh.types.BMVert): v.co.y -= 0.012
mesh_obj('Magnet_Heart', heart, PINK, root, smooth=False)
box('Drawing_Paper', (0.0, yF - 0.050, 1.40), (0.22, yF - 0.046, 1.56), PAPER, root)
cyl('Drawing_Sun', 0.025, 0.003, 0, YEL, root, 20)
bpy.data.objects['Drawing_Sun'].rotation_euler = (math.pi / 2, 0, 0); bpy.data.objects['Drawing_Sun'].location = (0.16, yF - 0.050, 1.52)
box('Drawing_House', (0.04, yF - 0.051, 1.42), (0.11, yF - 0.050, 1.48), BLUE, root)
box('Drawing_Grass', (0.01, yF - 0.0515, 1.405), (0.21, yF - 0.0505, 1.415), GREEN, root)
# liner inside the fridge compartment (thin cream panels just inside the shell)
L = 0.006
box('Liner_Back', (x0 + T, yB - T - L, z_floor), (x1 - T, yB - T, z_top), INNER, root)
box('Liner_Left', (x0 + T, yF + 0.02, z_floor), (x0 + T + L, yB - T, z_top), INNER, root)
box('Liner_Right', (x1 - T - L, yF + 0.02, z_floor), (x1 - T, yB - T, z_top), INNER, root)
box('Liner_Floor', (x0 + T, yF + 0.02, z_floor), (x1 - T, yB - T, z_floor + L), INNER, root)
box('Liner_Ceiling', (x0 + T, yF + 0.02, z_top - L), (x1 - T, yB - T, z_top), INNER, root)
# two glass shelves with a white front rim
shelves = [0.50, 0.88]
for i, z in enumerate(shelves):
    box(f'Shelf_{i}', (x0 + T + L, yF + 0.07, z - 0.012), (x1 - T - L, yB - T - L, z), SHELF, root, 0.004)
    box(f'Shelf_{i}_Rim', (x0 + T + L, yF + 0.055, z - 0.014), (x1 - T - L, yF + 0.075, z + 0.012), RIM, root, 0.006)
# a front seal frame around the opening
box('Seal_Top', (x0 + T, yF + 0.006, z_top - 0.012), (x1 - T, yF + 0.02, z_top + 0.012), SEAL, root)
box('Seal_Bottom', (x0 + T, yF + 0.006, z_floor - 0.012), (x1 - T, yF + 0.02, z_floor + 0.012), SEAL, root)
# the fridge door: pivot on the right-hand hinge line (front right corner); mesh spans to the left; swings out toward -Y
door = empty('PART_Door', (x1 - 0.005, yF - 0.002, 0.0), root)
dz0, dz1 = z_floor - 0.06, z_top + 0.045
def dbox(name, mn, mx, material, bevel=0.0):
    o = box(name, mn, mx, material, door, bevel); return o
dbox('Door_Slab', (-W + 0.01, -0.06, dz0), (0.0, 0.0, dz1), BODY, 0.03)
dbox('Door_Inner', (-W + 0.05, 0.0, dz0 + 0.04), (-0.04, 0.012, dz1 - 0.04), INNER, 0.01)
dbox('Door_Handle', (-W + 0.05, -0.115, 0.55), (-W + 0.10, -0.055, 1.10), HANDLE, 0.022)
dbox('Door_Handle_Foot_A', (-W + 0.055, -0.075, 0.57), (-W + 0.095, -0.05, 0.62), HANDLE, 0.01)
dbox('Door_Handle_Foot_B', (-W + 0.055, -0.075, 1.03), (-W + 0.095, -0.05, 1.08), HANDLE, 0.01)
for i, z in enumerate((0.40, 0.80)):   # door bins (shallow lips on the inside of the door)
    dbox(f'Door_Bin_{i}', (-W + 0.08, 0.012, z), (-0.07, 0.05, z + 0.012), RIM, 0.004)
    dbox(f'Door_Bin_{i}_Lip', (-W + 0.08, 0.044, z), (-0.07, 0.052, z + 0.06), SHELF, 0.003)
box('COL_Door', (-W + 0.01, -0.06, dz0), (0.0, 0.0, dz1), BODY, door)
# collision boxes of the shell (hollow compartment) + shelves
for nm, mn, mx in (('COL_Back', (x0, yB - T - L, 0), (x1, yB, H)), ('COL_Left', (x0, yF, 0), (x0 + T + L, yB, H)), ('COL_Right', (x1 - T - L, yF, 0), (x1, yB, H)),
                   ('COL_Floor', (x0 + T, yF, 0), (x1 - T, yB - T, z_floor + L)), ('COL_Ceiling', (x0 + T, yF, z_top - L), (x1 - T, yB - T, H)),
                   ('COL_Shelf0', (x0 + T, yF + 0.055, shelves[0] - 0.014), (x1 - T, yB - T, shelves[0] + 0.012)),
                   ('COL_Shelf1', (x0 + T, yF + 0.055, shelves[1] - 0.014), (x1 - T, yB - T, shelves[1] + 0.012))):
    box(nm, mn, mx, BODY, root)
# slots: two per level (floor, shelf 0, shelf 1), a little behind the middle of the depth
for lvl, z in enumerate([z_floor + L, shelves[0], shelves[1]]):
    for side, x in enumerate((-0.15, 0.15)):
        empty(f'SOCKET_Slot{lvl * 2 + side}', (x, 0.02, z + 0.005), root)
empty('SOCKET_InsideMin', (x0 + T + L, yF + 0.02, z_floor + L), root)
empty('SOCKET_InsideMax', (x1 - T - L, yB - T - L, z_top - L), root)
empty('FRONT', (0, -1.0, 0.9), root)
export('sk_fridge')

# ===================================================================================================== food

def wavy_disc(name, r, h, z0, material, parent, waves=10, amp=0.08, seg=60):
    bm = bmesh.new(); top = []; bot = []
    for k in range(seg):
        a = k / seg * 2 * math.pi; rr = r * (1 + amp * math.sin(a * waves)); x, y = rr * math.cos(a), rr * math.sin(a)
        bot.append(bm.verts.new((x, y, z0))); top.append(bm.verts.new((x, y, z0 + h)))
    bm.faces.new(top); bm.faces.new(list(reversed(bot)))
    for k in range(seg):
        a, b = k, (k + 1) % seg; bm.faces.new((bot[a], bot[b], top[b], top[a]))
    return mesh_obj(name, bm, material, parent, bevel=min(0.004, h * 0.4), segs=2)

def torus(name, R, r, z, material, parent, x=0.0, y=0.0, seg=40, ring=12):
    bm = bmesh.new(); grid = []
    for i in range(seg):
        a = i / seg * 2 * math.pi; row = []
        for j in range(ring):
            b = j / ring * 2 * math.pi
            row.append(bm.verts.new(((R + r * math.cos(b)) * math.cos(a) + x, (R + r * math.cos(b)) * math.sin(a) + y, z + r * math.sin(b))))
        grid.append(row)
    for i in range(seg):
        for j in range(ring):
            bm.faces.new((grid[i][j], grid[(i + 1) % seg][j], grid[(i + 1) % seg][(j + 1) % ring], grid[i][(j + 1) % ring]))
    return mesh_obj(name, bm, material, parent)

# ---- pizza: 0.24 m across
reset(); root = empty('ROOT', (0, 0, 0))
CRUST = mat('pizza_crust', (0.93, 0.70, 0.40), 0.7); SAUCE = mat('pizza_sauce', (0.86, 0.22, 0.16), 0.5); CHEESE = mat('pizza_cheese', (1.0, 0.84, 0.36), 0.45)
PEPP = mat('pepperoni', (0.75, 0.16, 0.14), 0.45); OLIVE = mat('olive', (0.18, 0.16, 0.18), 0.4); BASIL = mat('basil', (0.30, 0.68, 0.30), 0.5)
cyl('Pizza_Base', 0.112, 0.012, 0.0, CRUST, root, 48, bevel=0.004)
torus('Pizza_Crust', 0.106, 0.014, 0.016, CRUST, root)
cyl('Pizza_Sauce', 0.098, 0.004, 0.012, SAUCE, root, 48)
wavy_disc('Pizza_Cheese', 0.088, 0.006, 0.015, CHEESE, root, waves=9, amp=0.09)
for k, (dx, dy) in enumerate([(0.0, 0.0), (0.05, 0.02), (-0.045, 0.035), (0.02, -0.055), (-0.05, -0.035), (0.055, -0.03), (-0.01, 0.06)]):
    cyl(f'Pepperoni_{k}', 0.017, 0.005, 0.0205, PEPP, root, 20, x=dx, y=dy, bevel=0.0015)
for k, (dx, dy) in enumerate([(0.03, 0.05), (-0.03, -0.06), (0.065, 0.0), (-0.065, 0.01)]):
    torus(f'Olive_{k}', 0.007, 0.0035, 0.0235, OLIVE, root, dx, dy, 16, 8)
for k, (dx, dy, a) in enumerate([(0.025, 0.02, 0.4), (-0.03, 0.0, 1.9), (0.0, -0.03, 3.0)]):
    o = sphere(f'Basil_{k}', 0.012, (0, 0, 0), BASIL, root, (1.0, 0.55, 0.18), 2); o.location = (dx, dy, 0.025); o.rotation_euler = (0, 0, a)
export('sk_food_pizza')

# ---- burger: 0.11 m across, ~0.10 m tall
reset(); root = empty('ROOT', (0, 0, 0))
BUN = mat('bun', (0.93, 0.64, 0.30), 0.55); BUNTOP = mat('bun_top', (0.88, 0.52, 0.20), 0.5); PATTY = mat('patty', (0.40, 0.22, 0.13), 0.7)
SLICE = mat('cheese_slice', (1.0, 0.80, 0.22), 0.4); LETTUCE = mat('lettuce', (0.45, 0.80, 0.30), 0.5); TOMATO = mat('tomato', (0.92, 0.25, 0.20), 0.4)
SESAME = mat('sesame', (0.99, 0.94, 0.80), 0.6)
cyl('Bun_Bottom', 0.052, 0.022, 0.0, BUN, root, 40, bevel=0.009)
wavy_disc('Patty', 0.057, 0.018, 0.022, PATTY, root, waves=13, amp=0.035)
ch = bmesh.new(); s = 0.058
q = [ch.verts.new((x, y, 0.0)) for (x, y) in ((-s, -s), (s, -s), (s, s), (-s, s))]; ch.faces.new(q)
bmesh.ops.subdivide_edges(ch, edges=ch.edges[:], cuts=6, use_grid_fill=True)
for v in ch.verts:
    d = max(abs(v.co.x), abs(v.co.y)) / s; v.co.z = 0.040 - 0.012 * max(0.0, d - 0.6) ** 1.5 * 4
r_ = bmesh.ops.extrude_face_region(ch, geom=ch.faces[:])
for v in r_['geom']:
    if isinstance(v, bmesh.types.BMVert): v.co.z += 0.004
o = mesh_obj('Cheese_Slice', ch, SLICE, root); o.rotation_euler = (0, 0, math.pi / 4)
wavy_disc('Lettuce', 0.060, 0.007, 0.044, LETTUCE, root, waves=16, amp=0.10)
cyl('Tomato', 0.046, 0.008, 0.051, TOMATO, root, 32, bevel=0.003)
top = sphere('Bun_Top', 0.054, (0, 0, 0.059), BUNTOP, root, (1.0, 1.0, 0.70), 3)
bm = bmesh.new(); bm.from_mesh(top.data); bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 0.059 - 0.004], context='VERTS')
bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0); bm.to_mesh(top.data); bm.free()
for k in range(16):
    a = random.uniform(0, 2 * math.pi); r = random.uniform(0.0, 0.040); z = 0.059 + 0.038 * math.sqrt(max(0.0, 1 - (r / 0.054) ** 2))
    o = sphere(f'Sesame_{k}', 0.0035, (0, 0, 0), SESAME, root, (1.0, 0.55, 0.45), 1); o.location = (r * math.cos(a), r * math.sin(a), z); o.rotation_euler = (0, 0, a)
export('sk_food_burger')

# ---- juice carton: 0.075 x 0.075 x ~0.20
reset(); root = empty('ROOT', (0, 0, 0))
ORANGE = mat('juice_orange', (1.0, 0.60, 0.15), 0.45); WHITE = mat('carton_white', (0.99, 0.98, 0.95), 0.5); LEAF = mat('leaf_green', (0.35, 0.72, 0.30), 0.5)
FRUIT = mat('fruit_orange', (1.0, 0.52, 0.08), 0.4)
a = 0.0375
box('Carton_Body', (-a, -a, 0.0), (a, a, 0.165), ORANGE, root, 0.006)
gb = bmesh.new()
v = [gb.verts.new(p) for p in ((-a, -a, 0.165), (a, -a, 0.165), (a, a, 0.165), (-a, a, 0.165), (-a, 0.0, 0.198), (a, 0.0, 0.198))]
gb.faces.new((v[0], v[1], v[5], v[4])); gb.faces.new((v[3], v[4], v[5], v[2])); gb.faces.new((v[0], v[4], v[3])); gb.faces.new((v[1], v[2], v[5]))
mesh_obj('Carton_Gable', gb, ORANGE, root, smooth=False)
box('Carton_Fin', (-a, -0.003, 0.196), (a, 0.003, 0.212), ORANGE, root, 0.002)
box('Carton_Label', (-a - 0.001, -a - 0.001, 0.045), (a + 0.001, a + 0.001, 0.120), WHITE, root, 0.003)
o = sphere('Label_Orange', 0.022, (0, -a - 0.004, 0.083), FRUIT, root, (1.0, 0.35, 1.0), 2)
o = sphere('Label_Leaf', 0.010, (0.012, -a - 0.006, 0.106), LEAF, root, (1.4, 0.3, 0.7), 2)
cap = cyl('Carton_Cap', 0.011, 0.012, -0.002, WHITE, root, 20, bevel=0.002)   # built at the origin, then set on the front slope of the gable
cap.rotation_euler = (0.72, 0, 0); cap.location = (0.012, -0.017, 0.180)
export('sk_food_juice')

# ---- apple: ~0.085 m across
reset(); root = empty('ROOT', (0, 0, 0))
RED = mat('apple_red', (0.88, 0.18, 0.18), 0.35); STEM = mat('stem', (0.42, 0.26, 0.14), 0.7); LEAF = mat('leaf_green', (0.35, 0.72, 0.30), 0.5)
ap = sphere('Apple', 0.042, (0, 0, 0.040), RED, root, (1.0, 1.0, 0.92), 4)
for vv in ap.data.vertices:   # top dimple + slightly narrower bottom: an apple, not a ball
    rr = math.hypot(vv.co.x, vv.co.y); zz = vv.co.z - 0.040
    if zz > 0: vv.co.z -= 0.014 * math.exp(-(rr / 0.013) ** 2)
    if zz < 0: vv.co.x *= 1 - 0.10 * (-zz / 0.04); vv.co.y *= 1 - 0.10 * (-zz / 0.04); vv.co.z += 0.006 * math.exp(-(rr / 0.012) ** 2)
cyl('Apple_Stem', 0.0035, 0.024, 0.066, STEM, root, 10, r2=0.0025)
bpy.data.objects['Apple_Stem'].rotation_euler = (0.25, 0, 0)
o = sphere('Apple_Leaf', 0.014, (0.012, 0.004, 0.084), LEAF, root, (1.0, 0.45, 0.12), 2); o.rotation_euler = (0.3, -0.4, 0.5)
export('sk_food_apple')
print('ALL_DONE')
