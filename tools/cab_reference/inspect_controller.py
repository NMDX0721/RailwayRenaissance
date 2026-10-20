"""Read-only clay inspection of the existing controller asset; no asset replacement."""
import bpy
import json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/controller_review'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
source = ROOT/'Assets/Models/Cab/throttle_controller.glb'
bpy.ops.import_scene.gltf(filepath=str(source))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
corners = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
low = Vector(tuple(min(c[j] for c in corners) for j in range(3)))
high = Vector(tuple(max(c[j] for c in corners) for j in range(3)))
dimensions = high-low
center = (high+low)/2
report = {'source':str(source),'mesh_count':len(meshes),'source_dimensions_units_unknown':list(dimensions),
          'bounds_min':list(low),'bounds_max':list(high),'parts':[]}
scale = 1/max(dimensions)
transform = Matrix.Scale(scale,4) @ Matrix.Translation(-center)
clay = bpy.data.materials.new('Inspection_Clay')
clay.diffuse_color = (.43,.42,.38,1)
clay.use_nodes = True
clay.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.43,.42,.38,1)
clay.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .65
for obj in meshes:
    obj.data.calc_loop_triangles()
    report['parts'].append({'name':obj.name,'triangles':len(obj.data.loop_triangles)})
    obj.matrix_world = transform @ obj.matrix_world
    obj.data.materials.clear()
    obj.data.materials.append(clay)
(OUT/'existing_model_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

floor_z = (low.z-center.z)*scale-.012
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,floor_z))
floor = bpy.context.object
mat = bpy.data.materials.new('Inspection_Floor')
mat.diffuse_color = (.07,.075,.08,1)
floor.data.materials.append(mat)
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Inspection_World')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.25,.25,.25,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .4
def point_at(obj,target=(0,0,0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for location,energy,size in [((1,-2,3),200,2),((-2,1,2),150,2)]:
    bpy.ops.object.light_add(type='AREA',location=location)
    lamp = bpy.context.object
    lamp.data.energy, lamp.data.size = energy,size
    point_at(lamp)
bpy.ops.object.camera_add(location=(1.7,-2.4,1.6))
camera = bpy.context.object
point_at(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.5
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.render.resolution_x,scene.render.resolution_y = 1000,760
scene.render.resolution_percentage = 100
scene.render.filepath = str(OUT/'existing_model_clay.png')
bpy.ops.render.render(write_still=True)
