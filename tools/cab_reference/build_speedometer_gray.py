"""Photo-led provisional stand-alone speedometer; no claimed calibrated dimensions."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/speedometer_review'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
def material(name,color):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = .52
    return m
case = material('Case_Gray',(.09,.115,.11))
ivory = material('Dial_Ivory',(.69,.66,.51))
black = material('Dial_Ink',(.012,.014,.012))
yellow = material('Dial_Center',(.42,.30,.06))
root = bpy.data.objects.new('Speedometer_Photo_Study',None)
bpy.context.collection.objects.link(root)
root['dimensions_and_scale_provisional'] = True
parts=[]
def box(name,loc,dims,mat,parent=root,bevel=.001):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o = bpy.context.object
    o.name,o.parent=name,parent
    o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    if bevel:
        m=o.modifiers.new('Edge_Radius','BEVEL');m.width,m.segments=bevel,3
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(o)
    return o
def disk(name,r,depth,loc,mat,parent=root):
    bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=r,depth=depth,location=loc,rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name,o.parent=name,parent;o.data.materials.append(mat);parts.append(o)
    return o
box('Fixed_Square_Housing',(0,0,.095),(.17,.095,.19),case)
box('Fixed_Foot',(0,0,.006),(.19,.12,.012),case)
disk('Dial_Dark_Bezel',.073,.006,(0,-.051,.104),black)
disk('Dial_Face',.067,.002,(0,-.055,.104),ivory)
disk('Dial_Yellow_Center',.023,.001,(0,-.0565,.104),yellow)
for i in range(61):
    a=math.radians(-130+260*i/60)
    r=.060 if i%4==0 else .062
    o=box('Tick_'+str(i),(r*math.sin(a),-.057,.104+r*math.cos(a)),
          (.0011,.0005,.008 if i%4==0 else .004),black,bevel=0)
    o.rotation_euler.y=a
pivot=bpy.data.objects.new('Speed_Needle_Pivot',None)
bpy.context.collection.objects.link(pivot);pivot.parent=root;pivot.location=(0,-.059,.104)
pivot['rotation_axis']='Blender local Y; initial angle is a visual study'
box('Moving_Needle',(0,0,.022),(.0016,.001,.051),black,pivot,bevel=0)
pivot.rotation_euler.y=math.radians(-130)
disk('Needle_Cap',.008,.003,(0,-.061,.104),black)
box('Fixed_Nameplate',(0,-.051,.020),(.055,.002,.012),black)
# Scale numerals remain omitted until a clearer original dial is available.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/speedometer_photo_blockout.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene
scene.world=bpy.data.worlds.new('Study_World');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
for loc in [(.3,-.4,.6),(-.3,-.1,.4)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=9;o.data.size=.4
    o.rotation_euler=(Vector((0,0,.09))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.24,-.5,.24));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.095))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=.30;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x,scene.render.resolution_y=1000,1000
scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'speedometer_gray_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'speedometer_photo_blockout.blend'))
bpy.ops.render.render(write_still=True)
print('SPEEDOMETER_STUDY_MESHES',len(parts))
