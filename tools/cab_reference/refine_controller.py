"""Refine the separate-part controller study and embed portable PBR maps."""
import bpy
import bmesh
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/cab_reference/controller_review'
sys.path.insert(0, str(Path(__file__).parent))
from switch_materials import finish_material, ensure_uvs

bpy.ops.wm.open_mainfile(filepath=str(OUT / 'controller_handwheel_blockout.blend'))
parts = [o for o in bpy.data.objects if o.type == 'MESH' and not o.name.startswith('Preview_')]
directory = ROOT / 'Assets/Models/Cab/Textures/Controller'
directory.mkdir(parents=True, exist_ok=True)

specs = [
    ('Blockout_Grip', 'Controller_Grip', (.006,.0065,.006), .235, 0, .18, .007, 731),
    ('Blockout_Housing', 'Controller_Housing', (.009,.010,.009), .34, 0, .10, .018, 732),
    ('Blockout_Base', 'Controller_Plate', (.007,.008,.007), .30, 0, .12, .014, 733),
    ('Blockout_Hardware', 'Controller_Steel', (.17,.18,.16), .33, .85, 0, .010, 734),
]
for old, label, base, rough, metal, coat, grain, seed in specs:
    mat = bpy.data.materials[old]
    mat.name = label
    finish_material(mat, label, base, rough, metal, coat, grain, seed, directory)

# Consistent outward winding is especially important on custom lathed surfaces.
for obj in parts:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()

# Small manufacturing radii on the pedestal steps, preserving the silhouette.
for name in ('Fixed_Pedestal', 'Fixed_Shaft_Collar', 'Moving_Hub', 'Fixed_Socket_Ring'):
    obj = bpy.data.objects[name]
    mod = obj.modifiers.new('Machined_Edge_Radius', 'BEVEL')
    mod.width = .00045
    mod.segments = 3
    mod.limit_method = 'ANGLE'
    obj.modifiers.new('Surface_Normals', 'WEIGHTED_NORMAL')

ensure_uvs(parts)
bpy.context.view_layer.update()
report = {'dimensions_estimated': True, 'spokes_provisional': True,
          'unity_validation': False, 'meshes': len(parts), 'triangles': 0}
for obj in parts:
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    report['triangles'] += len(mesh.loop_triangles)
    evaluated.to_mesh_clear()
assert report['triangles'] < 25000
assert len(bpy.data.objects['Handwheel_Pivot'].children) == 7

bpy.ops.object.select_all(action='DESELECT')
for obj in [bpy.data.objects['Controller_Handwheel_Study'], bpy.data.objects['Handwheel_Pivot'], *parts]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects['Controller_Handwheel_Study']
target = ROOT / 'Assets/Models/Cab/controller_handwheel_refined.glb'
bpy.ops.export_scene.gltf(filepath=str(target), export_format='GLB', use_selection=True,
                         export_apply=True, export_extras=True)
(OUT/'refined_audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
scene = bpy.context.scene
scene.cycles.samples = 48
scene.render.filepath = str(OUT/'controller_material_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'controller_handwheel_refined.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location = (0,0,.75)
scene.camera.rotation_euler = (0,0,0)
scene.camera.data.ortho_scale = .43
scene.render.resolution_x, scene.render.resolution_y = 1100,1000
scene.render.filepath = str(OUT/'controller_material_top.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
