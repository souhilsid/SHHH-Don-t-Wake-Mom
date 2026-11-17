# Render a quick Workbench preview of an FBX (QA of generated art).  blender -b -P preview_fbx.py -- <in.fbx> <out.png>
import bpy, sys, math
from mathutils import Vector
a = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=a[0])
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in obs:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c); mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    print('OBJ', o.name, 'verts', len(o.data.vertices), 'dims', tuple(round(d, 3) for d in o.dimensions))
ctr = (mn + mx) / 2; size = max((mx - mn).length, 0.1)
print('BOUNDS', tuple(round(x, 3) for x in (mx - mn)))
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); bpy.context.collection.objects.link(cam)
cam.location = ctr + Vector((size * 0.9, -size * 1.3, size * 0.9)); cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z', 'Y').to_euler()
bpy.context.scene.camera = cam
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
sc.display.shading.show_cavity = True
sc.render.resolution_x = 640; sc.render.resolution_y = 480; sc.render.filepath = a[1]
bpy.ops.render.render(write_still=True)
print('PREVIEW_OK', a[1])
