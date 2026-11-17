import bpy, os, sys, json
src, out = sys.argv[1], sys.argv[2]
rep = {}
for f in sorted(os.listdir(src)):
    if not f.endswith(".glb"): continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(src, f))
    objs = [(o.name, o.type, o.parent.name if o.parent else None, [round(v, 3) for v in o.matrix_world.translation]) for o in bpy.context.scene.objects]
    rep[f] = {"count": len(objs), "nonleaf": [x for x in objs if x[1] != 'MESH' or 'PART' in x[0] or 'lever' in x[0].lower() or 'Pump' in x[0] or 'spout' in x[0].lower() or x[0].startswith('COL_')]}
    name = f.split("_")[0].lower()
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=False, object_types={'MESH', 'EMPTY'},
        apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', path_mode='COPY', embed_textures=True,
        use_mesh_modifiers=True, add_leaf_bones=False, bake_space_transform=False)
print(json.dumps(rep, indent=1))
