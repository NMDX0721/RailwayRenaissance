"""Matte finish and detachable-handle visual candidates; interfaces remain estimated."""
import bpy
import math
import sys
import json
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material, ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'brake_valves_photo_detailed.blend'))
root = bpy.data.objects['Brake_Valve_Photo_Study']
directory = ROOT/'Assets/Models/Cab/Textures/BrakeValvesMatte'
directory.mkdir(parents=True,exist_ok=True)
body = bpy.data.materials['Brake_Oxidized_Silver']
steel = bpy.data.materials['Brake_Worn_Steel']
for mat,label,base,r,m,seed in [(body,'Brake_Matte_Casting',(.30,.31,.29),.60,.65,1981),
                               (steel,'Brake_Matte_Steel',(.20,.21,.19),.49,.85,1982)]:
    for node in list(mat.node_tree.nodes):
        if node.type not in ('BSDF_PRINCIPLED','OUTPUT_MATERIAL'):
            mat.node_tree.nodes.remove(node)
    mat.name = label
    finish_material(mat,label,base,r,m,0,.023,seed,directory)
grip = bpy.data.materials.new('Brake_Handle_Black')
grip.use_nodes = True
bs = grip.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value = (.012,.013,.011,1)
bs.inputs['Roughness'].default_value = .53

def cut(obj,cutter):
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Recess','BOOLEAN')
    mod.operation,mod.solver,mod.object = 'DIFFERENCE','EXACT',cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

# Replace the rejected round bore + side opening with a shallow rectangular key seat.
# Neither backside pattern nor mating dimensions is claimed as verified.
for label in ('Tall','Short'):
    obj = bpy.data.objects[label+'_Exposed_Spindle']
    obj.modifiers.clear()
    bpy.context.view_layer.update()
    top = max((obj.matrix_world@Vector(c)).z for c in obj.bound_box)
    center = obj.matrix_world.translation
    bpy.ops.mesh.primitive_cube_add(size=1,location=(center.x,center.y,top-.0015))
    cutter = bpy.context.object
    cutter.dimensions = (.021 if label=='Tall' else .017,.0055,.006)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    cut(obj,cutter)
    bevel = obj.modifiers.new('Seat_Edge_Radius','BEVEL')
    bevel.width,bevel.segments = .00035,3
    obj.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    obj['keyseat_photo_estimate'] = True

def mesh_parts():
    return [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]

def export(label):
    objects = mesh_parts()
    ensure_uvs(objects)
    bpy.context.view_layer.update()
    tris = 0
    for o in objects:
        e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = e.to_mesh()
        mesh.calc_loop_triangles()
        tris += len(mesh.loop_triangles)
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
        e.to_mesh_clear()
    assert tris<30000
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*objects,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(ROOT/f'Assets/Models/Cab/brake_valves_{label}.glb'),
                             export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
    return {'variant':label,'meshes':len(objects),'triangles':tris,'interface_verified':False}

reports = [export('without_handles')]
scene = bpy.context.scene
scene.camera.location = (.43,-.43,.29)
scene.camera.rotation_euler = (Vector((0,0,.09))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale = .34
scene.render.filepath = str(OUT/'brake_without_handles.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_without_handles.blend'))
bpy.ops.render.render(write_still=True)

def finish(o,name,mat,parent):
    o.name = name
    o.parent = parent
    o.data.materials.append(mat)
    mod = o.modifiers.new('Handle_Edge_Radius','BEVEL')
    mod.width,mod.segments = .001,3
    o.modifiers.new('Handle_Normals','WEIGHTED_NORMAL')
    return o

for label,length in [('Tall',.105),('Short',.080)]:
    pivot = bpy.data.objects[label+'_Spindle_Pivot']
    # Handle socket surrounds the shaft; actual bore/retention details await drawings.
    shaft = bpy.data.objects[label+'_Exposed_Spindle']
    top_local = max(v.co.z for v in shaft.data.vertices)+shaft.location.z
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.022 if label=='Tall' else .019,
                                       depth=.012,location=(0,0,top_local-.007))
    socket = finish(bpy.context.object,label+'_Handle_Socket',steel,pivot)
    # Real cavity to avoid a solid cap overlapping the shaft.
    bpy.context.view_layer.update()
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.0165 if label=='Tall' else .0135,
        depth=.014,location=pivot.matrix_world@Vector((0,0,top_local-.009)))
    cut(socket,bpy.context.object)
    # Short cranked metal arm and a horizontal dark grip, based on the cab-photo silhouette.
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-length*.48,0,top_local-.001))
    arm = bpy.context.object
    arm.dimensions = (length,.014,.009)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(arm,label+'_Handle_Arm',steel,pivot)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=12,location=(-length-.017,0,top_local-.001))
    handle = bpy.context.object
    handle.scale = (.035,.012,.011)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(handle,label+'_Handle_Grip',grip,pivot)
    for p in handle.data.polygons:
        p.use_smooth = True
    pivot['handle_shape_provisional'] = True

reports.append(export('with_handles'))
(OUT/'brake_variants_audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
scene.camera.data.ortho_scale = .40
scene.camera.rotation_euler = (Vector((-.035,0,.09))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = str(OUT/'brake_with_handles.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_with_handles.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(reports))
