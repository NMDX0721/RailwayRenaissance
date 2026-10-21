"""Photo-estimated real shaft recesses and portable aged metal textures."""
import bpy
import math
import random
import json
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import image_map, srgb
bpy.ops.wm.open_mainfile(filepath=str(OUT/'brake_valves_photo_detailed.blend'))
directory = ROOT/'Assets/Models/Cab/Textures/BrakeValvesAged'
directory.mkdir(parents=True,exist_ok=True)

def subtract(obj,cutter):
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Actual_Slot_Cut','BOOLEAN')
    mod.operation,mod.solver,mod.object = 'DIFFERENCE','EXACT',cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

for label in ('Tall','Short'):
    obj = bpy.data.objects[label+'_Exposed_Spindle']
    # Boolean on the base mesh, then a small radius on the newly cut lips.
    obj.modifiers.clear()
    bpy.context.view_layer.update()
    corners = [obj.matrix_world@Vector(c) for c in obj.bound_box]
    zmin,zmax = min(c.z for c in corners),max(c.z for c in corners)
    center = obj.matrix_world.translation
    radius = .016 if label=='Tall' else .013
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=radius*.46,depth=.014,
                                       location=(center.x,center.y,zmax-.004))
    subtract(obj,bpy.context.object)
    # An open radial key notch leading into the top recess.
    bpy.ops.mesh.primitive_cube_add(size=1,location=(center.x-radius*.6,center.y,zmax-.001))
    cutter = bpy.context.object
    cutter.dimensions = (radius*1.35,radius*.42,.009)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    subtract(obj,cutter)
    for i in range(4):
        angle = i*math.pi/2
        bpy.ops.mesh.primitive_cube_add(size=1,location=(center.x+radius*math.cos(angle),
            center.y+radius*math.sin(angle),zmin+(zmax-zmin)*.48))
        cutter = bpy.context.object
        cutter.dimensions = (.004,.0045,(zmax-zmin)*.64)
        cutter.rotation_euler.z = angle
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        subtract(obj,cutter)
    bevel = obj.modifiers.new('Slot_Lip_Radius','BEVEL')
    bevel.width,bevel.segments = .0003,3
    obj.modifiers.new('Slot_Weighted_Normals','WEIGHTED_NORMAL')
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False) if hasattr(bpy.ops.mesh,'normals_make_consistent') else None
    bpy.ops.uv.smart_project(island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj['socket_and_notch_dimensions_photo_estimated'] = True

def age(material,label,base,rough,metal,seed):
    rng = random.Random(seed)
    n = 512
    noise = [rng.uniform(-1,1) for _ in range(n*n)]
    colors,rs,normals = [],[],[]
    for y in range(n):
        v = (y+.5)/n
        for x in range(n):
            u = (x+.5)/n
            g = noise[y*n+x]
            cloud = .5+.5*math.sin(u*6*math.pi+math.sin(v*4*math.pi))*math.sin(v*6*math.pi)
            edge = abs(2*v-1)**10
            tarnish = .10*cloud+.13*edge
            scratch = .07 if abs(math.sin(u*1800+math.sin(v*40)*2))>.997 and cloud>.55 else 0
            linear = [c*(1-tarnish)+scratch for c in base]
            linear[2] *= 1-.08*cloud
            colors.extend([*(srgb(c) for c in linear),1])
            r = max(.22,min(.65,rough+.10*cloud+.08*edge-.04*scratch+g*.025))
            rs.extend([r,r,r,1])
            nx = (noise[y*n+(x-1)%n]-noise[y*n+(x+1)%n])*.035
            ny = (noise[((y-1)%n)*n+x]-noise[((y+1)%n)*n+x])*.035
            normals.extend([nx*.5+.5,ny*.5+.5,math.sqrt(max(.001,1-nx*nx-ny*ny))*.5+.5,1])
    nodes,links = material.node_tree.nodes,material.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    # Drop unused earlier texture nodes so the exported graph has one active set.
    for node in list(nodes):
        if node.type not in ('BSDF_PRINCIPLED','OUTPUT_MATERIAL'):
            nodes.remove(node)
    material.name = label
    bsdf.inputs['Metallic'].default_value = metal
    bsdf.inputs['Coat Weight'].default_value = 0
    for suffix,pixels,socket,color in [('BaseColor',colors,'Base Color',True),('Roughness',rs,'Roughness',False)]:
        node = nodes.new('ShaderNodeTexImage')
        node.image = image_map(label+'_'+suffix,pixels,n,directory,color)
        links.new(node.outputs['Color'],bsdf.inputs[socket])
    texture = nodes.new('ShaderNodeTexImage')
    texture.image = image_map(label+'_Normal',normals,n,directory)
    normal = nodes.new('ShaderNodeNormalMap')
    links.new(texture.outputs['Color'],normal.inputs['Color'])
    links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])

age(bpy.data.materials['Brake_Oxidized_Silver'],'Brake_Aged_Cast',(.29,.30,.28),.39,.76,1811)
age(bpy.data.materials['Brake_Worn_Steel'],'Brake_Aged_Steel',(.22,.23,.21),.27,.88,1812)
parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
report = {'type_unconfirmed':True,'slot_dimensions_estimated':True,'unity_validated':False,
          'meshes':len(parts),'triangles':0}
bpy.context.view_layer.update()
for obj in parts:
    e = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = e.to_mesh()
    mesh.calc_loop_triangles()
    report['triangles'] += len(mesh.loop_triangles)
    assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
    e.to_mesh_clear()
assert report['triangles']<30000
bpy.ops.object.select_all(action='DESELECT')
root = bpy.data.objects['Brake_Valve_Photo_Study']
for obj in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/brake_valves_photo_slotted.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'brake_slotted_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene = bpy.context.scene
scene.render.filepath = str(OUT/'brake_slotted_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_valves_photo_slotted.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location = (.18,-.25,.32)
scene.camera.rotation_euler = (Vector((-.024,.037,.173))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale = .115
scene.render.filepath = str(OUT/'brake_slot_closeup.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
