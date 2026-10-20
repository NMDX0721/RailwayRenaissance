"""DF4-inspired retrofit console: real component geometry and removable panel apertures."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'tools/cab_reference';OUT=BASE/'console_review'
OUT.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(BASE))
from pressure_preview_lighting import configure
bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name,color,rough=.6,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m
paint=mat('Console_Olive_Painted_Steel',(.075,.105,.085),.65,.05)
topmat=mat('Console_Brown_Phenolic',(.065,.026,.014),.53,0)
black=mat('Console_Black_Panel',(.012,.017,.015),.55,.15)
steel=mat('Console_Fastener',(.17,.18,.16),.46,.85)
rubber=mat('Console_Dark_Gasket',(.007,.009,.008),.8)
root=bpy.data.objects.new('Main_Cab_Console_Assembly',None);bpy.context.collection.objects.link(root)
def cube(name,loc,size,material,parent=root,bevel=.002):
    bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.parent=parent;o.location=loc;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if material:o.data.materials.append(material)
    if bevel:
        b=o.modifiers.new('Small_Fabrication_Radius','BEVEL');b.width=bevel;b.segments=3
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o
def empty(name,loc=(0,0,0),rotation=(0,0,0),parent=root):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.parent=parent;o.location=loc;o.rotation_euler=rotation;return o
def bolt(name,loc,parent=root):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.004,depth=.002)
    o=bpy.context.object;o.name=name;o.parent=parent;o.location=loc;o.rotation_euler.x=math.pi/2;o.data.materials.append(steel)
    return o
worktop=cube('Worktop',(0,-.08,.825),(1.55,.66,.028),topmat,bevel=.009)
cube('Front_Edge_Steel',(0,-.412,.813),(1.55,.012,.035),steel,bevel=.003)
cube('Left_Cabinet',(-.63,-.07,.40),(.27,.55,.79),paint,bevel=.012)
cube('Right_Cabinet',(.63,-.07,.40),(.27,.55,.79),paint,bevel=.012)
cube('Rear_Crossmember',(0,.185,.72),(1.05,.045,.15),paint)
cube('Left_Service_Door',(-.63,-.351,.42),(.22,.012,.63),paint,bevel=.004)
cube('Right_Service_Door',(.63,-.351,.42),(.22,.012,.63),paint,bevel=.004)
for x in [-.63,.63]:
    cube('Door_Recessed_Latch',(x,-.361,.61),(.035,.008,.025),black)
    for z in [.20,.48]:cube('Door_Hinge',(x-.105,-.36,z),(.012,.012,.055),steel)
    for j in range(7):cube('Cabinet_Vent',(x,-.360,.16+j*.012),(.13,.004,.003),black,bevel=.001)
panel=empty('Instrument_Bank',(0,.14,.85),(-math.radians(15),0,0))
panelmesh=cube('Removable_Instrument_Panel',(0,0,.12),(1.50,.008,.27),black,panel,0)
cube('Instrument_Bank_Rear_Housing',(0,.10,.105),(1.50,.008,.235),paint,panel)
mounts=[];apertures=[]
def aperture(x,width,height,z=.09):
    cutter=cube('Aperture_Cutter',(x,0,z),(width,.08,height),None,panel,0)
    bpy.context.view_layer.update();bpy.context.view_layer.objects.active=panelmesh
    mod=panelmesh.modifiers.new('Through_Panel_Cutout','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    apertures.append({'x_m':x,'width_m':width,'height_m':height})
def append_asset(tag,path,parent,loc,cut=None):
    with bpy.data.libraries.load(str(BASE/path),link=False) as (src,dst):
        dst.objects=[n for n in src.objects if not n.startswith(('Preview_','Desk_'))]
    objects=[o for o in dst.objects if o and o.type in {'MESH','EMPTY'}]
    for o in objects:
        if o.name not in bpy.context.collection.objects:bpy.context.collection.objects.link(o)
    tops=[o for o in objects if o.parent is None]
    mount=empty(tag+'_Mount',loc,parent=parent)
    for o in tops:o.parent=mount
    # Remove identical common gauge material copies while preserving distinct materials.
    for o in objects:
        if o.type=='MESH':
            for i,m in enumerate(o.data.materials):
                if m and m.name.rsplit('.',1)[-1].isdigit():
                    original=bpy.data.materials.get(m.name.rsplit('.',1)[0])
                    if original and m.name.startswith('Gauge_'):o.data.materials[i]=original
    for r in tops:
        for name,position in [('Origin',(0,0,0)),('AxisX',(.01,0,0)),('AxisY',(0,.01,0)),('AxisZ',(0,0,.01))]:
            empty(tag+'_Calibration_'+name,position,parent=r)
    bpy.context.view_layer.update()
    points=[o.matrix_world@Vector(c) for o in objects if o.type=='MESH' for c in o.bound_box]
    bounds={'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]}
    mounts.append({'tag':tag,'source':path,'bounds':bounds})
    if cut:aperture(loc[0],*cut)
    return mount,objects
_,pressure_objects=append_asset('Pressure','pressure_review/duplex_air_gauges_final.blend',panel,(-.46,-.009,.026),(.255,.125,.085))
for tag in ['MR_BC','BP_ER']:
    pivot=next(o for o in pressure_objects if o.name==tag+'_Red_Pivot')
    for name,pos in [('Origin',(0,0,0)),('AxisY',(0,.01,0))]:empty(tag+'_Calibration_'+name,pos,parent=pivot.parent)
append_asset('Current','linear_gauge_review/linear_gauge_final.blend',panel,(-.245,-.002,.018),(.039,.163,.104))
append_asset('RPM','tachometer_review/diesel_tachometer_final.blend',panel,(-.175,-.002,.018),(.039,.163,.104))
_,temps=append_asset('Temperature','temperature_review/temperature_gauges_final.blend',panel,(-.065,-.002,.018))
for x in [-.095,-.035]:aperture(x,.039,.163,.104)
# Per-module calibration origins also include the temperature pair's internal offset.
for tag in ['Water','Oil']:
    r=next(o for o in temps if o.name==tag+'_Temperature_Meter')
    for name,pos in [('Origin',(0,0,0)),('AxisX',(.01,0,0)),('AxisY',(0,.01,0)),('AxisZ',(0,0,.01))]:empty(tag+'_Calibration_'+name,pos,parent=r)
cube('Future_Instrument_Blanking_Plate',(.36,-.007,.12),(.43,.006,.215),black,panel)
for x in [-.72,.72,.13]:
    for z in [.005,.245]:bolt('Instrument_Panel_Fastener',(x,-.006,z),panel)
append_asset('Speed','speedometer_review/speedometer_final.blend',root,(.56,.19,1.12))
cube('Speedometer_Stand',(.56,.23,1.09),(.19,.12,.06),paint)
append_asset('Controller','controller_review/controller_handwheel_final.blend',root,(-.10,-.15,.84))
append_asset('Brake','brake_review/brake_final_with_handles.blend',root,(-.58,-.02,.84))
switch_mount,switch_objects=append_asset('Switches','switches_review/piano_switches_10gang_optimized.blend',root,(.37,-.22,.839))
switch_mount.scale=(3,3,3)
switch_mount['integration_scale_basis']='source bank is 76 mm wide; retrofit fit scales complete assembly to 228 mm; unverified original dimensions'
cutter=cube('Switches_Through_Worktop',(.37,-.22,.825),(.209,.075,.12),None,root,0)
bpy.context.view_layer.update();bpy.context.view_layer.objects.active=worktop
mod=worktop.modifiers.new('Switch_Bank_Installation_Aperture','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
empty('Driver_Eye_Reference',(-.10,-.78,1.38))
empty('Seat_Center_Reference',(-.10,-.80,.48))
root['design_status']='DF4-inspired retrofit layout study; estimated dimensions; no certified ergonomics'
root['knee_opening_width_m']=.99
root['worktop_height_m']=.839
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in switch_objects if o.type=='MESH' for c in o.bound_box]
mounts[-1]['bounds']={'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]}
mounts[-1]['integration_scale']=3
report={'status':'first assembled console study','estimated_dimensions':True,'width_m':1.55,'worktop_top_m':.839,
 'panel_back_tilt_degrees':15,'knee_opening_width_m':.99,'mounts':mounts,'apertures':apertures,
 'not_yet_validated':['full human reach envelope','valve handle swept volumes','driver sightlines','wiring and pipe routing'],
 'interactive_controller_and_switches':False}
(OUT/'console_layout_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
parts=[o for o in bpy.data.objects if o.type in {'MESH','EMPTY'}]
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/main_console_study.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene
if scene.world is None:scene.world=bpy.data.worlds.new('Console_Preview_World')
configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.1
bpy.data.objects['Preview_Soft_Key'].data.energy=85;bpy.data.objects['Preview_Soft_Key'].location=(-2,-3,4);bpy.data.objects['Preview_Soft_Key'].data.size=4
bpy.ops.object.camera_add(location=(2,-3,2.1));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.72))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.15;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1400;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'main_console_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'main_console_study.blend'))
bpy.ops.render.render(write_still=True)
print('MAIN_CONSOLE_STUDY_OK')
