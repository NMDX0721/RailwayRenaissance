"""Second valve study: tighten assembly seams and add restrained silver PBR."""
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material

bpy.ops.wm.open_mainfile(filepath=str(OUT/'brake_valves_photo_blockout.blend'))
parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
# Close unintended air gaps without treating the dark joint as a separate floating ring.
casting = bpy.data.objects['Tall_Lower_Casting']
for v in casting.data.vertices:
    if abs(v.co.z-.121)<.00001:
        v.co.z = .123
shaft = bpy.data.objects['Tall_Exposed_Spindle']
for v in shaft.data.vertices:
    if v.co.z<0:
        v.co.z -= .0005

directory = ROOT/'Assets/Models/Cab/Textures/BrakeValves'
directory.mkdir(parents=True,exist_ok=True)
body = bpy.data.materials['Valve_Clay']
body.name = 'Brake_Oxidized_Silver'
finish_material(body,'Brake_CastMetal',(.34,.35,.32),.43,.80,0,.023,1701,directory)
hardware = bpy.data.materials['Hardware_Clay']
hardware.name = 'Brake_Worn_Steel'
finish_material(hardware,'Brake_Steel',(.19,.20,.18),.32,.88,0,.010,1702,directory)

# Cylindrical coordinates shared across the casting, rather than repeating every face.
for obj in parts:
    mesh = obj.data
    uv = mesh.uv_layers.active or mesh.uv_layers.new(name='UVMap')
    if obj.name.startswith('Tall_') and obj.parent.name!='Tall_Spindle_Pivot':
        center = (-.024,.037)
    elif obj.name.startswith('Short_') and obj.parent.name!='Short_Spindle_Pivot':
        center = (.030,-.054)
    else:
        center = (0,0)
    # Primitive cylinders are already centered locally; custom castings are not.
    if 'Casting' not in obj.name:
        center = (0,0)
    zs = [v.co.z for v in mesh.vertices]
    zmin,zmax = min(zs),max(zs)
    for p in mesh.polygons:
        angles = [(math.atan2(mesh.vertices[mesh.loops[i].vertex_index].co.y-center[1],
                              mesh.vertices[mesh.loops[i].vertex_index].co.x-center[0])/(2*math.pi))%1
                  for i in p.loop_indices]
        crosses = max(angles)-min(angles)>.5
        for i,u in zip(p.loop_indices,angles):
            v = mesh.vertices[mesh.loops[i].vertex_index].co
            if abs(p.normal.z)>.95:
                uv.data[i].uv = ((v.x-center[0])/.10+.5,(v.y-center[1])/.10+.5)
            else:
                uv.data[i].uv = (u+1 if crosses and u<.5 else u,(v.z-zmin)/max(zmax-zmin,1e-8))

bpy.context.view_layer.update()
report = {'type_unconfirmed':True,'dimensions_estimated':True,'unity_validated':False,
          'meshes':len(parts),'triangles':0}
for obj in parts:
    e = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = e.to_mesh()
    mesh.calc_loop_triangles()
    report['triangles'] += len(mesh.loop_triangles)
    assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
    e.to_mesh_clear()
assert report['triangles']<20000
bpy.ops.object.select_all(action='DESELECT')
root = bpy.data.objects['Brake_Valve_Photo_Study']
for obj in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/brake_valves_photo_refined.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'brake_refined_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene = bpy.context.scene
scene.camera.location = (.43,-.43,.29)
scene.camera.rotation_euler = (Vector((0,0,.09))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.cycles.samples = 48
scene.render.filepath = str(OUT/'brake_material_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_valves_photo_refined.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location = (.34,.42,.30)
scene.camera.rotation_euler = (Vector((0,0,.09))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = str(OUT/'brake_material_reverse.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
