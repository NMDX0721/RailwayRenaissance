"""Restrained, reproducible age maps; preserve the approved controller silhouette."""
import bpy
import math
import random
import sys
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/controller_review'
sys.path.insert(0, str(Path(__file__).parent))
from switch_materials import image_map, srgb

bpy.ops.wm.open_mainfile(filepath=str(OUT/'controller_handwheel_refined.blend'))
directory = ROOT/'Assets/Models/Cab/Textures/ControllerFinal'
directory.mkdir(parents=True, exist_ok=True)
parts = [o for o in bpy.data.objects if o.type == 'MESH' and not o.name.startswith('Preview_')]

def aged_material(source, label, kind):
    mat = source.copy()
    mat.name = label
    rng = random.Random(1021+kind)
    scratches = [(rng.random(),rng.random(),rng.uniform(.008,.065)) for _ in range(38)]
    colors, roughness, metallic = [], [], []
    size = 512
    for y in range(size):
        v = (y+.5)/size
        for x in range(size):
            u = (x+.5)/size
            cloud = .5+.3*math.sin(u*6*math.pi+math.sin(v*4*math.pi))*math.sin(v*6*math.pi)
            grit = rng.random()
            if kind == 0:
                # Broad irregular polish around the grip, with a less touched underside.
                polish = (.5+.5*math.sin(u*6*math.pi+1))*(.55+.45*math.cos(v*2*math.pi))
                edge = 0
                dust = .12*(1-polish)*cloud
                base, r = (.006,.0065,.0058), .30-.13*polish
            elif kind == 1:
                edge = max(0,1-min(u,1-u,v,1-v)/.018)
                polish = 0
                dust = .35*cloud*(max(0,1-min(u,1-u,v,1-v)/.12))
                base, r = (.007,.008,.0067), .34
            else:
                edge = max(0,1-min(v,1-v)/.025)
                polish = 0
                dust = .20*cloud
                base, r = (.008,.009,.0075), .35
            scratch = 0
            for sx,sy,length in scratches:
                dx,dy = u-sx,v-sy
                if abs(dx)<length and abs(dy-dx*.18)<.0009:
                    scratch = max(scratch,(1-abs(dx)/length)*.4)
            chip = edge*max(0,(grit-.87)/.13)*.65
            abrasion = max(chip,scratch)
            linear = [c*(1+.12*cloud)+dust*t+abrasion*.028 for c,t in zip(base,(.025,.021,.014))]
            colors.extend([*(srgb(c) for c in linear),1])
            r = min(.65,max(.15,r+dust*.28+scratch*.12+(grit-.5)*.018))
            roughness.extend([r,r,r,1])
            m = chip*.65
            metallic.extend([m,m,m,1])
    nodes,links = mat.node_tree.nodes,mat.node_tree.links
    bsdf = nodes['Principled BSDF']
    bsdf.inputs['Coat Weight'].default_value = .08
    for label_suffix,pixels,socket,color in [('BaseColor',colors,'Base Color',True),
            ('Roughness',roughness,'Roughness',False),('Metallic',metallic,'Metallic',False)]:
        img = image_map(label+'_'+label_suffix,pixels,size,directory,color)
        node = nodes.new('ShaderNodeTexImage')
        node.image = img
        links.new(node.outputs['Color'],bsdf.inputs[socket])
    return mat

grip = aged_material(bpy.data.materials['Controller_Grip'],'Final_Aged_Grip',0)
plate = aged_material(bpy.data.materials['Controller_Plate'],'Final_Aged_Plate',1)
housing = aged_material(bpy.data.materials['Controller_Housing'],'Final_Aged_Housing',2)
for obj in parts:
    if obj.name == 'Fixed_Mounting_Plate':
        obj.data.materials[0] = plate
        uv = obj.data.uv_layers.active
        for loop in obj.data.loops:
            p = obj.data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv = (p.x/.31+.5,p.y/.225+.5)
    elif obj.name.startswith('Moving_') and obj.name != 'Moving_Retainer':
        obj.data.materials[0] = grip
    elif obj.name in ('Fixed_Pedestal','Fixed_Shaft_Collar'):
        obj.data.materials[0] = housing
        uv = obj.data.uv_layers.active
        zs = [v.co.z for v in obj.data.vertices]
        for polygon in obj.data.polygons:
            angles = [math.atan2(obj.data.vertices[obj.data.loops[i].vertex_index].co.y-.01,
                                 obj.data.vertices[obj.data.loops[i].vertex_index].co.x+.025)/(2*math.pi)%1
                      for i in polygon.loop_indices]
            crosses = max(angles)-min(angles)>.5
            for i,u in zip(polygon.loop_indices,angles):
                p = obj.data.vertices[obj.data.loops[i].vertex_index].co
                uv.data[i].uv = (u+1 if crosses and u<.5 else u,(p.z-min(zs))/(max(zs)-min(zs)))

root = bpy.data.objects['Controller_Handwheel_Study']
root['art_status'] = 'final_user_requested_aged_finish'
root['historical_reconstruction_verified'] = False
bpy.context.view_layer.update()
report = {'art_status':'final','historical_dimensions_verified':False,'unity_validated':False,
          'mesh_count':len(parts),'triangles':0,'moving_children':len(bpy.data.objects['Handwheel_Pivot'].children)}
for obj in parts:
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    report['triangles'] += len(mesh.loop_triangles)
    assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
    evaluated.to_mesh_clear()
assert report['moving_children'] == 7 and report['triangles'] < 25000
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,bpy.data.objects['Handwheel_Pivot'],*parts]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/controller_handwheel_final.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene = bpy.context.scene
scene.cycles.samples = 64
scene.render.filepath = str(OUT/'controller_final_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'controller_handwheel_final.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location = (0,0,.75)
scene.camera.rotation_euler = (0,0,0)
scene.camera.data.ortho_scale = .43
scene.render.resolution_x,scene.render.resolution_y = 1100,1000
scene.render.filepath = str(OUT/'controller_final_top.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
