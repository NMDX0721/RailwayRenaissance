"""Removable key, empty lock and inserted-key variants of the retrofit start panel."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/startup_review';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.read_factory_settings(use_empty=True)
def material(name,color,rough,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;return m
paint=material('Startup_Black_Paint',(.012,.018,.015),.61)
steel=material('Startup_Brushed_Steel',(.22,.24,.22),.46,.9)
grip=material('Startup_Bakelite',(.009,.011,.009),.47)
ivory=material('Startup_Engraved_Legends',(.65,.62,.48),.77)
brass=material('Startup_Key_Brass',(.29,.19,.065),.42,.85)
green=material('Startup_Unlit_Green_Lens',(.015,.09,.025),.34)
root=bpy.data.objects.new('Startup_Control_Panel',None);bpy.context.collection.objects.link(root)
def cube(name,loc,size,mat,parent=root,bevel=.001):
    bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.parent=parent;o.location=loc;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
    if bevel:
        m=o.modifiers.new('Edge_Radius','BEVEL');m.width=bevel;m.segments=3;o.modifiers.new('Weighted_Normal','WEIGHTED_NORMAL')
    return o
def cyl(name,loc,r,h,mat,parent=root):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=h);o=bpy.context.object;o.name=name;o.parent=parent;o.location=loc;o.data.materials.append(mat)
    m=o.modifiers.new('Machined_Radius','BEVEL');m.width=.0003;m.segments=2;return o
font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc')
def text(body,loc,size=.005):
    bpy.ops.object.text_add();o=bpy.context.object;o.name='Legend_'+body;o.parent=root;o.location=loc;o.data.body=body;o.data.font=font;o.data.size=size;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.materials.append(ivory);bpy.ops.object.convert(target='MESH')
cube('Removable_Panel',(0,0,0),(.240,.115,.007),paint,bevel=.003)
cube('Rear_Terminal_Enclosure',(0,0,-.021),(.218,.094,.034),paint,bevel=.003)
for x in [-.110,.110]:
    for y in [-.047,.047]:
        cyl('Captive_Panel_Screw',(x,y,.0045),.003,.002,steel)
        cube('Screw_Driver_Slot',(x,y,.0056),(.0035,.0006,.0002),paint,bevel=.0001)
lock=cyl('Fixed_Key_Bezel',(-.073,0,.008),.015,.010,steel)
plug=cyl('Rotating_Lock_Plug',(-.073,0,.014),.010,.004,steel)
cutter=cube('Keyway_Cutter',(-.073,0,.009),(.0065,.0028,.024),paint,bevel=0)
for target in [plug]:
    bpy.context.view_layer.objects.active=target;mod=target.modifiers.new('Real_Insertion_Keyway','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter;bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.data.objects.remove(cutter,do_unlink=True)
bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=.0103,depth=.028,location=(-.073,0,.008))
cutter=bpy.context.object;bpy.context.view_layer.objects.active=lock
mod=lock.modifiers.new('Rotating_Plug_Circular_Clearance','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
cyl('Keyway_Dark_Floor',(-.073,0,-.005),.008,.001,paint)
pivot=bpy.data.objects.new('Key_Turn_Pivot',None);bpy.context.collection.objects.link(pivot);pivot.parent=root;pivot.location=(-.073,0,.016)
plug.parent=pivot;plug.location=(0,0,-.002)
key=bpy.data.objects.new('Removable_Master_Key',None);bpy.context.collection.objects.link(key);key.parent=pivot
cube('Key_Blade',(0,0,-.006),(.006,.002,.028),brass,key,.00025)
cube('Key_Grip',(0,0,.019),(.026,.004,.023),grip,key,.003)
cube('Key_Shoulder',(0,0,.005),(.011,.003,.005),brass,key,.0004)
# Shallow grooves are visual orientation cues, not a claim to a particular lock standard.
for x in [-.0018,.0018]:cube('Key_Groove',(x,-.00105,-.006),(.0004,.00012,.020),steel,key,.00006)
button=cyl('Starter_Button',(0,0,.012),.011,.014,grip)
cyl('Starter_Bezel',(0,0,.006),.015,.005,steel)
cyl('Pump_Selector_Base',(.070,0,.007),.014,.006,steel)
lever=cube('Pump_Selector',(.070,0,.017),(.010,.027,.016),grip)
for x,name in [(-.025,'Master_Power_Lens'),(.025,'Engine_Running_Lens')]:cyl(name,(x,.033,.007),.0045,.005,green)
text('主控钥匙',(-.073,-.032,.0038));text('关       开',(-.073,.029,.0038),.004)
text('柴油机启动',(0,-.032,.0038));text('燃油泵',(.070,-.032,.0038));text('关 / 开',(.070,.029,.0038),.004)
text('电源',(-.025,.046,.0038),.0035);text('运行',(.025,.046,.0038),.0035)
root['key_removal_rule']='only OFF and engine stopped; local controller guards, not a certified locking system'
root['keyway_clearance_m']=[.0005,.0008];root['electrical_pinout']='unassigned generic enclosure'
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Startup_World');configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.1
bpy.data.objects['Preview_Soft_Key'].data.energy=6
bpy.ops.object.camera_add(location=(.27,-.34,.30));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.003))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=.32;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
for variant in ['with_key','without_key','key_only']:
    key_only=variant=='key_only'
    for o in root.children_recursive:
        belongs=o==key or o in key.children_recursive
        o.hide_render=(not belongs) if key_only else (belongs and variant=='without_key')
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*root.children_recursive]:
        if not o.hide_render or o.type=='EMPTY':o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(ROOT/f'Assets/Models/Cab/startup_controls_{variant}.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
    bpy.ops.export_scene.fbx(filepath=str(ROOT/f'Assets/Models/Cab/UnityStartupControls/{variant}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'startup_controls_{variant}.blend'))
    scene.render.filepath=str(OUT/f'startup_controls_{variant}.png');bpy.ops.render.render(write_still=True)
(ROOT/'Assets/Models/Cab/UnityStartupControls/materials.json').write_text(json.dumps([
    {'name':m.name,'color':list(m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value),
     'metallic':m.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value,
     'roughness':m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value}
    for m in [paint,steel,grip,ivory,brass,green]],indent=2))
print('STARTUP_VARIANTS_OK')
