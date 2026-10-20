"""Single narrow electrical instrument study; range, units, and bank count unverified."""
import bpy
import math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/linear_gauge_review';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
root=bpy.data.objects.new('Linear_Instrument_Photo_Study',None)
bpy.context.collection.objects.link(root)
root['type_range_units_unconfirmed']=True
root['dimensions_estimated']=True
parts=[]
def mat(name,color):
    m=bpy.data.materials.new(name);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.52
    return m
black=mat('Gauge_BlackPaint',(.017,.020,.018))
cream=mat('Gauge_Scale',(.65,.62,.49))
ink=mat('Gauge_Ink',(.009,.010,.009))
red=mat('Gauge_Pointer',(.22,.05,.025))
steel=mat('Gauge_Fastener',(.18,.19,.17))
def box(name,loc,dims,material,parent=root,bevel=.0005):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object
    o.name,o.parent=name,parent;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    if bevel:
        m=o.modifiers.new('Edge_Radius','BEVEL');m.width,m.segments=bevel,2
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(o);return o
box('Fixed_Rear_Housing',(0,0,.087),(.043,.030,.174),black)
box('Fixed_Front_Frame',(0,-.017,.087),(.046,.005,.178),black)
box('Fixed_Scale_Window',(0,-.020,.089),(.031,.001,.143),cream)
box('Fixed_Scale_Rail',(.001,-.0207,.089),(.0006,.0002,.128),ink,bevel=0)
for i in range(21):
    z=.026+i*.0063
    box('Scale_Tick_'+str(i),(-.004,-.0208,z),(.010 if i%5==0 else .005,.00015,.0006),ink,bevel=0)
for z in (.008,.166):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.0021,depth=.001,
        location=(0,-.020,z),rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name='Fixed_Frame_Screw';o.parent=root;o.data.materials.append(steel);parts.append(o)
pointer=bpy.data.objects.new('Reading_Pointer_Carriage',None)
bpy.context.collection.objects.link(pointer);pointer.parent=root;pointer.location=(0,0,.064)
pointer['movement_axis']='local Z; actual movement mechanism unverified'
box('Moving_Pointer',(.006,-.0215,0),(.017,.0006,.0015),red,pointer,0)
# Consolidate static scale marks while preserving a separately movable pointer.
ticks=[o for o in parts if o.name.startswith('Scale_Tick_')]
bpy.ops.object.select_all(action='DESELECT')
for o in ticks:o.select_set(True)
bpy.context.view_layer.objects.active=ticks[0];bpy.ops.object.join();ticks[0].name='Fixed_Scale_Printing'
parts=[o for o in bpy.data.objects if o.type=='MESH']
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_photo_blockout.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Study_World');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
for loc in [(.2,-.3,.5),(-.2,-.1,.3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=7;o.data.size=.3
    o.rotation_euler=(Vector((0,0,.085))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.12,-.35,.16));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.087))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=.23;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x,scene.render.resolution_y=800,1100;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'linear_gauge_gray_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_photo_blockout.blend'))
bpy.ops.render.render(write_still=True)
