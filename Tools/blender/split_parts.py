# SHHH! V12 asset pipeline (Blender 5, headless): turn single-mesh owner props into MECHANISMS with real moving parts.
#   blender -b -P split_parts.py -- <job> <in.fbx> <out.fbx>
# Output conventions (read by Unity ArticulatedModel): PART_<Name> objects with their ORIGIN AT THE PIVOT (hinge line, rail front,
# dial centre), FRONT empty (the side a person uses), COL_* boxes optional. Exported Y-up / -Z forward for Unity.
# Jobs:
#   washer      PtDesigns laundry_washing_machine_01: door (hinge = left edge), detergent drawer (+ tray), dial, start button, door hole
#               carved through the front + PART_Drum cavity (open cylinder, inward normals) so clothes can really go inside.
#   fridge      Quaternius Kitchen_Fridge: the two doors (+ their handles) are separate islands -> PART_DoorLow / PART_DoorHigh (hinge = side
#               away from the handle).
#   bin         Quaternius Trashcan_Large: lid + handle islands -> PART_Lid (hinge = back edge).
#   drawers3    Quaternius Kitchen_3Drawers: the drawer fronts were painted on a solid box -> rebuilt: carcass with 3 openings + 3 drawer boxes
#               (front panel, sides, back, bottom; the original handle island on each) = PART_Drawer0..2 (origin at the drawer front centre).
import bpy, bmesh, sys, math
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:]
job, src, dst = argv[0], argv[1], argv[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
body = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
bpy.context.view_layer.objects.active = body
# apply transforms so vertex coords are model-space metres (Blender: X right, -Y front, Z up)
body.select_set(True); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def weld(o, d=0.0006):
    bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=d); bm.to_mesh(o.data); bm.free()

def split_faces(o, pred, name):
    """Move faces whose (centroid, normal) satisfy pred into a new object; returns it (or None)."""
    bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table()
    sel = [f for f in bm.faces if pred(f.calc_center_median(), f.normal)]
    if not sel: bm.free(); return None
    new_me = bpy.data.meshes.new(name); nbm = bmesh.new()
    vmap = {}
    for f in sel:
        vs = []
        for v in f.verts:
            if v not in vmap: vmap[v] = nbm.verts.new(v.co)
            vs.append(vmap[v])
        try:
            nf = nbm.faces.new(vs); nf.material_index = f.material_index; nf.smooth = f.smooth
        except ValueError: pass
    # uv copy
    uv_src = bm.loops.layers.uv.active
    if uv_src is not None:
        uv_dst = nbm.loops.layers.uv.new("UVMap"); nbm.faces.ensure_lookup_table()
        for f_old, f_new in zip(sel, nbm.faces):
            for l_old, l_new in zip(f_old.loops, f_new.loops): l_new[uv_dst].uv = l_old[uv_src].uv
    nbm.to_mesh(new_me); nbm.free()
    bmesh.ops.delete(bm, geom=sel, context='FACES'); bm.to_mesh(o.data); bm.free()
    no = bpy.data.objects.new(name, new_me); bpy.context.scene.collection.objects.link(no)
    for m in o.data.materials: no.data.materials.append(m)
    return no

def set_origin(o, world_point):
    """Move the object's origin to a pivot without moving its geometry."""
    off = world_point - o.location
    o.data.transform(Matrix.Translation(-off)); o.location = world_point

def empty(name, loc):
    e = bpy.data.objects.new(name, None); e.location = loc; bpy.context.scene.collection.objects.link(e); return e

def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.diffuse_color = (*rgb, 1.0); return m

def box(name, mn, mx, material):
    me = bpy.data.meshes.new(name); bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((mn[0] + (v.co.x + 0.5) * (mx[0] - mn[0]), mn[1] + (v.co.y + 0.5) * (mx[1] - mn[1]), mn[2] + (v.co.z + 0.5) * (mx[2] - mn[2])))
    bm.to_mesh(me); bm.free(); o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o); me.materials.append(material); return o

def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.join(); objs[0].name = name; objs[0].data.name = name; return objs[0]

def carve(o, cutter):
    m = o.modifiers.new("carve", 'BOOLEAN'); m.operation = 'DIFFERENCE'; m.object = cutter
    try: m.solver = 'EXACT'
    except Exception: pass
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter, do_unlink=True)

def islands(o):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table(); seen = set(); out = []
    for v in bm.verts:
        if v.index in seen: continue
        st = [v]; comp = set()
        while st:
            x = st.pop()
            if x.index in seen: continue
            seen.add(x.index); comp.add(x.index); st.extend(e.other_vert(x) for e in x.link_edges)
        pts = [bm.verts[i].co.copy() for i in comp]
        out.append((comp, Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))), Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))))
    bm.free(); return out

def split_island_box(o, mn, mx, name):
    """Faces whose centroid lies inside the box."""
    return split_faces(o, lambda c, n: mn.x <= c.x <= mx.x and mn.y <= c.y <= mx.y and mn.z <= c.z <= mx.z, name)

if job == "washer":
    weld(body)
    C = Vector((-0.02, 0.0, 0.43)); R = 0.218
    door = split_faces(body, lambda c, n: (c.y < -0.2915 and (Vector((c.x, 0, c.z)) - C).length < R + 0.004) or (c.y < -0.296 and 0.14 < c.x < 0.24 and 0.37 < c.z < 0.58), "PART_Door")
    drawer = split_faces(body, lambda c, n: c.y < -0.2875 and -0.262 < c.x < -0.048 and 0.695 < c.z < 0.792, "PART_DetergentDrawer")
    dial = split_faces(body, lambda c, n: c.y < -0.2875 and (Vector((c.x, 0, c.z)) - Vector((0.10, 0, 0.76))).length < 0.026, "PART_Dial")
    button = split_faces(body, lambda c, n: c.y < -0.2875 and 0.155 < c.x < 0.215 and 0.675 < c.z < 0.718, "PART_StartButton")
    # door hole through the front + drum cavity behind it
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.19, depth=0.12, location=(C.x, -0.29, C.z), rotation=(math.pi / 2, 0, 0))
    carve(body, bpy.context.active_object)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.188, depth=0.40, location=(C.x, -0.29 + 0.20, C.z), rotation=(math.pi / 2, 0, 0))
    drum = bpy.context.active_object; drum.name = "PART_Drum"
    bmd = bmesh.new(); bmd.from_mesh(drum.data); bmd.faces.ensure_lookup_table()
    front_cap = [f for f in bmd.faces if f.calc_center_median().y < -0.28 and len(f.verts) > 4]
    bmesh.ops.delete(bmd, geom=front_cap, context='FACES')
    bmesh.ops.reverse_faces(bmd, faces=bmd.faces[:])              # seen from inside
    bmd.to_mesh(drum.data); bmd.free(); drum.data.materials.append(mat("drum_steel_painted", (0.62, 0.66, 0.72)))
    # drawer tray (compartment) behind the green front: it comes out with the drawer
    if drawer is not None:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(-0.155, -0.29 + 0.11, 0.742)); cut = bpy.context.active_object; cut.scale = (0.2, 0.22, 0.082)
        bpy.ops.object.transform_apply(scale=True); carve(body, cut)
        trayM = mat("detergent_tray_painted", (0.55, 0.78, 0.66)); parts = [drawer]
        for (mn, mx) in [((-0.255, -0.29, 0.703), (-0.247, -0.07, 0.781)), ((-0.063, -0.29, 0.703), (-0.055, -0.07, 0.781)), ((-0.255, -0.078, 0.703), (-0.055, -0.07, 0.781)), ((-0.255, -0.29, 0.703), (-0.055, -0.07, 0.711))]:
            parts.append(box("tray", mn, mx, trayM))
        drawer = join(parts, "PART_DetergentDrawer")
    if button is None: button = box("PART_StartButton", (0.172, -0.302, 0.684), (0.196, -0.288, 0.708), mat("start_button_red", (0.86, 0.3, 0.24)))
    if dial is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.022, depth=0.02, location=(0.10, -0.298, 0.76), rotation=(math.pi / 2, 0, 0)); dial = bpy.context.active_object; dial.name = "PART_Dial"
    set_origin(door, Vector((C.x - R, -0.304, C.z)))                # hinge line on the left edge (handle on the right)
    set_origin(drawer, Vector((-0.155, -0.29, 0.742)))              # front centre; slides out along -Y (Unity +Z)
    set_origin(dial, Vector((0.10, -0.29, 0.76)))                   # spins about Y (Unity Z)
    set_origin(button, Vector((0.184, -0.29, 0.696)))               # pressed along +Y
    set_origin(drum, Vector((C.x, -0.09, C.z)))
    empty("FRONT", Vector((0, -0.6, 0.4))); empty("SOCKET_DrumCentre", Vector((C.x, -0.1, C.z)))
elif job == "fridge":
    isl = sorted(islands(body), key=lambda t: len(t[0]), reverse=True)
    doors = [t for t in isl if t[1].y < -0.5 and (t[2].x - t[1].x) > 0.9 and (t[2].y - t[1].y) < 0.3]   # thin slabs only (not the body)
    handles = [t for t in isl if t[1].y < -0.6 and (t[2].x - t[1].x) < 0.1]
    names = []
    for k, (comp, mn, mx) in enumerate(sorted(doors, key=lambda t: t[1].z)):
        nm = "PART_DoorLow" if k == 0 else "PART_DoorHigh"
        hs = [h for h in handles if h[1].z >= mn.z - 0.01 and h[2].z <= mx.z + 0.01]
        hx = sum((h[1].x + h[2].x) * 0.5 for h in hs) / len(hs) if hs else mn.x
        boxmn = Vector((mn.x - 0.001, mn.y - 0.12, mn.z - 0.001)); boxmx = Vector((mx.x + 0.001, mx.y + 0.001, mx.z + 0.001))
        part = split_faces(body, lambda c, n, a=boxmn, b=boxmx: a.x <= c.x <= b.x and a.y <= c.y <= b.y and a.z <= c.z <= b.z, nm)
        hinge_x = mx.x if hx < (mn.x + mx.x) * 0.5 else mn.x       # hinge on the side away from the handle
        set_origin(part, Vector((hinge_x, mx.y, (mn.z + mx.z) * 0.5)))
    empty("FRONT", Vector((0, -1.5, 1.5)))
elif job == "bin":
    isl = sorted(islands(body), key=lambda t: len(t[0]), reverse=True)
    lid_top = min(t[1].z for t in isl if len(t[0]) < len(isl[0][0]))
    part = split_faces(body, lambda c, n: c.z > lid_top - 0.005, "PART_Lid")
    bb = [Vector(v) for v in part.bound_box]
    set_origin(part, Vector((0.0, max(v.y for v in bb), min(v.z for v in bb))))   # hinge at the back rim
    empty("FRONT", Vector((0, -1.5, 0.8)))
elif job == "drawers3":
    isl = islands(body); bb = [Vector(v) for v in body.bound_box]
    mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb))); mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
    handles = sorted([t for t in isl if (t[2].x - t[1].x) < 0.4 and t[1].y < -0.5], key=lambda t: t[1].z)
    # carcass: keep the original (painted materials), carve three drawer openings into the front
    front_y = -0.505; top_z = mx.z - 0.06; bottom_z = 0.06; n = max(1, len(handles))
    slot_h = (top_z - bottom_z) / n
    mats = body.data.materials; m_front = mats[0] if len(mats) else mat("kitchen_front", (0.9, 0.85, 0.7)); m_inner = mat("drawer_inside_wood", (0.78, 0.62, 0.42))
    hparts = [split_faces(body, lambda c, n_, a=h[1], b=h[2]: a.x - 0.001 <= c.x <= b.x + 0.001 and a.y - 0.001 <= c.y <= b.y + 0.001 and a.z - 0.001 <= c.z <= b.z + 0.001, f"handle{k}") for k, h in enumerate(handles)]
    for k in range(n):
        z0, z1 = bottom_z + k * slot_h + 0.012, bottom_z + (k + 1) * slot_h - 0.012
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, front_y + 0.4, (z0 + z1) * 0.5)); cut = bpy.context.active_object; cut.scale = (mx.x - mn.x - 0.12, 0.84, z1 - z0); bpy.ops.object.transform_apply(scale=True)
        carve(body, cut)
    for k in range(n):
        z0, z1 = bottom_z + k * slot_h + 0.014, bottom_z + (k + 1) * slot_h - 0.014; w0, w1 = mn.x + 0.065, mx.x - 0.065
        parts = [box("front", (w0 - 0.04, front_y - 0.03, z0 - 0.01), (w1 + 0.04, front_y, z1 + 0.01), m_front),
                 box("bottom", (w0, front_y, z0), (w1, front_y + 0.78, z0 + 0.015), m_inner),
                 box("sideL", (w0, front_y, z0), (w0 + 0.015, front_y + 0.78, z1 - 0.04), m_inner),
                 box("sideR", (w1 - 0.015, front_y, z0), (w1, front_y + 0.78, z1 - 0.04), m_inner),
                 box("back", (w0, front_y + 0.765, z0), (w1, front_y + 0.78, z1 - 0.04), m_inner)]
        if k < len(hparts) and hparts[k] is not None: parts.append(hparts[k])
        d = join(parts, f"PART_Drawer{k}")
        set_origin(d, Vector((0, front_y - 0.03, (z0 + z1) * 0.5)))
    empty("FRONT", Vector((0, -1.5, 0.8)))
else:
    raise SystemExit("unknown job " + job)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_ALL', path_mode='STRIP', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
print("SPLIT_OK", job, dst, [o.name for o in bpy.context.scene.objects])
