"""DF4-photo-led instrument, using historical rail gauges as comparative references."""
import bpy
import math
import sys
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/speedometer_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'speedometer_photo_blockout.blend'))
root=bpy.data.objects['Speedometer_Photo_Study']
root['reference_identity_unverified']=True
root['dial_maximum_visual_estimate_kmh']=150
parts=[o for o in bpy.data.objects if o.type=='MESH']
case=bpy.data.materials['Case_Gray'];ivory=bpy.data.materials['Dial_Ivory']
ink=bpy.data.materials['Dial_Ink'];yellow=bpy.data.materials['Dial_Center']
directory=ROOT/'Assets/Models/Cab/Textures/Speedometer'
directory.mkdir(parents=True,exist_ok=True)
finish_material(case,'Speedometer_Paint',(.025,.036,.032),.55,.05,0,.021,2211,directory)
finish_material(ivory,'Ivory',(.67,.64,.51),.57,0,0,.004,2212,directory)
panel=bpy.data.materials.new('Aged_Champagne_Panel');panel.use_nodes=True
finish_material(panel,'Speedometer_Panel',(.36,.33,.24),.48,.38,0,.012,2213,directory)
steel=bpy.data.materials.new('Screw_Steel');steel.use_nodes=True
p=steel.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(.17,.18,.16,1)
p.inputs['Metallic'].default_value=.85;p.inputs['Roughness'].default_value=.42

def box(name,loc,dims,mat,bevel=.0006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name,o.parent=name,root;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    if bevel:
        m=o.modifiers.new('Manufactured_Edge','BEVEL');m.width,m.segments=bevel,3
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(o);return o

box('Fixed_Front_Metal_Panel',(0,-.049,.097),(.159,.003,.176),panel)
# A faceplate seam is visible around the cabinet front, separate from the dial ring.
for x in (-.073,.073):
    for z in (.017,.177):
        bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.0031,depth=.0012,
            location=(x,-.052,z),rotation=(math.pi/2,0,0))
        o=bpy.context.object;o.name='Panel_Screw';o.parent=root;o.data.materials.append(steel);parts.append(o)
        box('Screw_Slot',(x,-.0527,z),(.0036,.00015,.00055),ink,0)
# Narrow upper keeper and a side service plug, preserving the plain industrial cabinet.
box('Top_Case_Lip',(0,-.040,.189),(.17,.014,.003),case)
bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.005,depth=.0015,
    location=(.0857,-.014,.057),rotation=(0,math.pi/2,0))
o=bpy.context.object;o.name='Side_Service_Plug';o.parent=root;o.data.materials.append(ink);parts.append(o)

def lettering(name,text,x,z,size):
    bpy.ops.object.text_add(location=(x,-.058,z),rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name=name;o.data.body=text;o.data.size=size
    o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.extrude=.000015
    o.data.materials.append(ink)
    bpy.ops.object.convert(target='MESH')
    o=bpy.context.object;o.parent=root;parts.append(o)
for value in range(0,151,10):
    angle=math.radians(-130+260*value/150)
    lettering('Scale_'+str(value),str(value),.048*math.sin(angle),.104+.048*math.cos(angle),.0062)
lettering('Unit_kmh','km/h',0,.075,.006)
# Tapered needle replaces the constant-width stick, retaining the original pivot.
needle=bpy.data.objects['Moving_Needle']
for v in needle.data.vertices:
    v.co.x *= .42 if v.co.z>0 else 1.1
ensure_uvs(parts)
bpy.context.view_layer.update()
report={'type_unconfirmed':True,'dimensions_estimated':True,'dial_range_visual_estimate':150,
        'meshes':len(parts),'triangles':0,'unity_validated':False}
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
assert report['triangles']<30000
bpy.ops.object.select_all(action='DESELECT')
for o in [root,*parts,bpy.data.objects['Speed_Needle_Pivot']]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/speedometer_photo_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'speedometer_refined_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=48
scene.render.filepath=str(OUT/'speedometer_refined_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'speedometer_photo_refined.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location=(0,-.5,.104);scene.camera.rotation_euler=(math.pi/2,0,0)
scene.camera.data.ortho_scale=.245
scene.render.filepath=str(OUT/'speedometer_refined_front.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
