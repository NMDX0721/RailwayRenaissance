"""Factory-catalog-led handle refinement and distinct, embedded surface finishes."""
import bpy
import sys
import math
import random
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import image_map, srgb, ensure_uvs

def surface(name,base,rough,metal,seed,grain,grip=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    rng = random.Random(seed)
    n = 512
    g = [rng.uniform(-1,1) for _ in range(n*n)]
    colors,rs,normals = [],[],[]
    for y in range(n):
        v = (y+.5)/n
        for x in range(n):
            u = (x+.5)/n
            cloud = .5+.5*math.sin(6*math.pi*u+math.sin(4*math.pi*v))*math.sin(4*math.pi*v)
            edge = abs(2*v-1)**12
            stain = .12*cloud+.07*edge
            line = max(0,math.sin(u*2*math.pi*121+math.sin(v*2*math.pi)*.8)-.995)*5
            basegain = 1-stain+line*.15
            colors.extend([*(srgb(c*basegain*(1-.035*cloud if j==2 else 1)) for j,c in enumerate(base)),1])
            r = rough+.07*cloud+.04*edge+g[y*n+x]*.025
            if grip:
                r -= .14*(1-edge)*cloud
            r = max(.24,min(.74,r))
            rs.extend([r,r,r,1])
            nx = (g[y*n+(x-1)%n]-g[y*n+(x+1)%n])*grain
            ny = (g[((y-1)%n)*n+x]-g[((y+1)%n)*n+x])*grain
            normals.extend([nx*.5+.5,ny*.5+.5,math.sqrt(max(.001,1-nx*nx-ny*ny))*.5+.5,1])
    nodes,links = mat.node_tree.nodes,mat.node_tree.links
    bs = nodes['Principled BSDF']
    bs.inputs['Metallic'].default_value = metal
    directory = ROOT/'Assets/Models/Cab/Textures/BrakeReference'
    directory.mkdir(parents=True,exist_ok=True)
    for suffix,pixels,socket,color in [('BaseColor',colors,'Base Color',True),('Roughness',rs,'Roughness',False)]:
        node = nodes.new('ShaderNodeTexImage')
        node.image = image_map(name+'_'+suffix,pixels,n,directory,color)
        links.new(node.outputs['Color'],bs.inputs[socket])
    node = nodes.new('ShaderNodeTexImage')
    node.image = image_map(name+'_Normal',normals,n,directory)
    normal = nodes.new('ShaderNodeNormalMap')
    links.new(node.outputs['Color'],normal.inputs['Color'])
    links.new(normal.outputs['Normal'],bs.inputs['Normal'])
    return mat

reports = []
for variant in ('without_handles','with_handles'):
    bpy.ops.wm.open_mainfile(filepath=str(OUT/f'brake_connection_{variant}.blend'))
    cast = surface('Reference_CastSilver',(.29,.30,.28),.57,.68,2111,.042)
    steel = surface('Reference_OldSteel',(.19,.20,.18),.43,.86,2112,.018)
    machined = surface('Reference_MachinedSilver',(.32,.33,.31),.36,.86,2113,.008)
    black = surface('Reference_WornBlack',(.010,.011,.009),.51,0,2114,.010,True)
    parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
    for o in parts:
        name = o.name
        if 'Distinct_Grip' in name:
            material = black
        elif any(term in name for term in ('Spindle','Removable_Hub','Screw','Short_Cover')):
            material = machined
        elif any(term in name for term in ('Bolt','Dogleg_Arm','Compact_Arm','Drive_Dog','Grip_Neck')):
            material = steel
        elif 'Seam' in name:
            continue
        else:
            material = cast
        o.data.materials.clear()
        o.data.materials.append(material)
    if variant=='with_handles':
        # Factory illustration suggests a short elongated grip, not the earlier ball knob.
        small = bpy.data.objects['Short_Distinct_Grip']
        for v in small.data.vertices:
            v.co.x *= 1.30
            v.co.y *= .72
            v.co.z *= .75
        small.location.x -= .012
        small.location.z -= .005
        neck = bpy.data.objects['Short_Grip_Neck']
        neck.location.x -= .006
        neck.scale.z *= .65
        # Thickened arched neck, preserving separate small/main proportions.
        arm = bpy.data.objects['Short_Compact_Arm']
        for v in arm.data.vertices:
            if v.co.x < 0:
                v.co.z += .004
    ensure_uvs(parts)
    bpy.context.view_layer.update()
    report = {'variant':variant,'meshes':len(parts),'triangles':0,'historical_model_verified':False}
    for o in parts:
        e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = e.to_mesh()
        mesh.calc_loop_triangles()
        report['triangles'] += len(mesh.loop_triangles)
        e.to_mesh_clear()
    assert report['triangles']<35000
    bpy.ops.object.select_all(action='DESELECT')
    root = bpy.data.objects['Brake_Valve_Photo_Study']
    for o in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(ROOT/f'Assets/Models/Cab/brake_valves_reference_{variant}.glb'),
                             export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
    scene = bpy.context.scene
    scene.cycles.samples = 64
    scene.render.filepath = str(OUT/f'brake_reference_{variant}.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'brake_reference_{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    reports.append(report)
(OUT/'brake_reference_audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print(json.dumps(reports))
