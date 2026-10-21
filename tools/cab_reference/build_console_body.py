"""DF4-inspired retrofit console: real component geometry and removable panel apertures."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'tools/cab_reference';OUT=BASE/'console_review'
OUT.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(BASE))
from pressure_preview_lighting import configure
from switch_materials import finish_material,ensure_uvs
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
ivory=mat('Console_Engraved_Legend',(.65,.62,.48),.78)
red=mat('Console_Red_Control',(.25,.008,.005),.55)
amber=mat('Console_Amber_Lens',(.28,.12,.012),.35)
texture_dir=ROOT/'Assets/Models/Cab/Textures/MainConsole';texture_dir.mkdir(parents=True,exist_ok=True)
for material,label,base,rough,metal,seed in [
    (paint,'Console_Paint',(.075,.105,.085),.62,0,771),
    (topmat,'Console_Phenolic',(.055,.024,.014),.50,0,772),
    (black,'Console_Black',(.012,.017,.015),.57,.05,773),
    (steel,'Console_Steel',(.17,.18,.16),.46,.85,774)]:
    finish_material(material,label,base,rough,metal,0,.003,seed,texture_dir)
    for n in material.node_tree.nodes:
        if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.2
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
    cube(name+'_Driver_Slot',(loc[0],loc[1]-.0011,loc[2]),(.0045,.0003,.00065),black,parent,.0001)
    return o
def legend(name,body,loc,parent=root,size=.008,deck=False):
    bpy.ops.object.text_add();o=bpy.context.object;o.name=name;o.parent=parent;o.location=loc
    o.rotation_euler=(0,0,0) if deck else (math.pi/2,0,0)
    o.data.font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc');o.data.body=body;o.data.size=size
    o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.extrude=.00003;o.data.materials.append(ivory)
    bpy.ops.object.convert(target='MESH');return o
def deck_button(name,loc,radius,material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius+.004,depth=.005,location=loc)
    base=bpy.context.object;base.name=name+'_Bezel';base.parent=root;base.data.materials.append(steel)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=.009,location=(loc[0],loc[1],loc[2]+.006))
    o=bpy.context.object;o.name=name;o.parent=root;o.data.materials.append(material)
    bevel=o.modifiers.new('Grip_Edge','BEVEL');bevel.width=.0015;bevel.segments=3
    return o
def prism(name,polygon,axis,limits,material,bevel=.004,parent=root):
    # Extruded polygon: arbitrary section for folded console sheet-metal volumes.
    vertices=[]
    for distance in limits:
        for a,b in polygon:
            vertices.append((distance,a,b) if axis=='X' else (a,b,distance))
    n=len(polygon);faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);obj.parent=parent;mesh.materials.append(material)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');obj.select_set(False)
    if bevel:
        m=obj.modifiers.new('Folded_Edge_Radius','BEVEL');m.width=bevel;m.segments=3
        obj.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return obj
# A continuous cab-mounted shell with recessed knees replaces the desk-like two pedestals.
section=[(-.35,.805),(.30,.805),(.30,.045),(.035,.045),(.035,.40),(-.10,.57),(-.30,.65)]
prism('Continuous_Folded_Console_Shell',section,'X',(-.755,.755),paint,.008)
outline=[(-.775,-.32),(-.69,-.415),(-.39,-.415),(-.30,-.345),(.20,-.345),(.29,-.415),(.69,-.415),(.775,-.32),(.775,.20),(.71,.265),(-.71,.265),(-.775,.20)]
worktop=prism('Contoured_Control_Deck',outline,'Z',(.811,.839),topmat,.006)
cube('Recessed_Toe_Plinth',(0,.16,.038),(1.45,.31,.065),rubber,bevel=.008)
cube('Upper_Front_Service_Fascia',(0,-.312,.715),(1.42,.012,.105),paint,bevel=.004)
for x in [-.53,0,.53]:
    cube('Fascia_Service_Seam',(x,-.320,.715),(.002,.003,.087),black,bevel=.0003)
    for dx in [-.19,.19]:bolt('Fascia_Captive_Fastener',(x+dx,-.323,.744))
for x in [-.60,.60]:
    for j in range(6):cube('Lower_Recess_Vent',(x,.027,.22+j*.014),(.16,.006,.004),black,bevel=.001)
cube('Knee_Recess_Kick_Plate',(0,.025,.16),(.78,.008,.20),black)
for x in [-.36,.36]:
    for z in [.085,.235]:bolt('Kick_Plate_Fixing',(x,.019,z))
for j in range(5):cube('Kick_Plate_Pressed_Rib',(0,.018,.105+j*.025),(.61,.006,.004),black,bevel=.0015)
for x in [-.60,.60]:
    cube('Vent_Frame_Top',(x,.018,.309),(.19,.007,.006),paint)
    cube('Vent_Frame_Bottom',(x,.018,.207),(.19,.007,.006),paint)
    for dx in [-.09,.09]:bolt('Vent_Panel_Fixing',(x+dx,.017,.32))
panel=empty('Instrument_Bank',(0,.14,.85),(-math.radians(15),0,0))
panelmesh=cube('Removable_Instrument_Panel',(0,0,.12),(1.50,.008,.27),black,panel,0)
cube('Instrument_Bank_Rear_Housing',(0,.10,.105),(1.50,.008,.235),paint,panel)
cube('Instrument_Bank_Top_Return',(0,.05,.257),(1.52,.115,.012),paint,panel)
for side in [-1,1]:
    prism('Instrument_Bank_Side_Cheek', [(-.008,-.01),(.108,-.01),(.108,.255),(-.008,.255)],'X',
        (side*.755-.006,side*.755+.006),paint,.003,panel)
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
append_asset('OilPressure','oil_pressure_review/oil_pressure_gauge_final.blend',panel,(.055,-.002,.018),(.039,.163,.104))
# Per-module calibration origins also include the temperature pair's internal offset.
for tag in ['Water','Oil']:
    r=next(o for o in temps if o.name==tag+'_Temperature_Meter')
    for name,pos in [('Origin',(0,0,0)),('AxisX',(.01,0,0)),('AxisY',(0,.01,0)),('AxisZ',(0,0,.01))]:empty(tag+'_Calibration_'+name,pos,parent=r)
cube('Auxiliary_Control_Plate',(.36,-.007,.12),(.43,.006,.215),black,panel)
for i,(title,lens) in enumerate([('油压低',red),('水温高',red),('充电',amber),('故障',amber)]):
    x=.21+i*.095
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10)
    o=bpy.context.object;o.name='Unbound_Warning_Lens_'+str(i);o.parent=panel;o.location=(x,-.018,.18);o.scale=(.009,.004,.009);o.data.materials.append(lens)
    o['binding_status']='physical lens only; no live warning logic yet'
    legend('Warning_Legend_'+str(i),title,(x,-.019,.153),panel,.007)
legend('Auxiliary_Plate_Title','辅助控制 / 状态',(.35,-.019,.222),panel,.009)
legend('Future_Radio_Location','通信 / 备用安装位',(.35,-.019,.075),panel,.008)
for x in [-.72,.72,.13]:
    for z in [.005,.245]:bolt('Instrument_Panel_Fastener',(x,-.006,z),panel)
append_asset('Speed','speedometer_review/speedometer_final.blend',root,(.33,.21,1.12))
cube('Speedometer_Stand',(.33,.25,1.09),(.19,.12,.06),paint)
append_asset('Controller','controller_review/controller_handwheel_final.blend',root,(-.10,-.15,.84))
brake_mount,brake_objects=append_asset('Brake','brake_review/brake_final_with_handles.blend',root,(-.50,-.13,.84))
brake_root=next(o for o in brake_objects if o.name=='Brake_Valve_Photo_Study')
for o in brake_objects:
    if o.parent==brake_root and o.name.startswith('Short_'):
        o.location.x+=.03;o.location.y-=.10
brake_root['installation']='automatic and independent valves separated for visibility; functional roles are retrofit design'
legend('Automatic_Brake_Legend','大闸 / 列车制动',(-.52,-.027,.842),size=.007,deck=True)
legend('Independent_Brake_Legend','小闸 / 机车制动',(-.44,-.355,.842),size=.007,deck=True)
deck_button('Horn_Push',(-.30,-.30,.846),.011,black)
legend('Horn_Legend','鸣笛',(-.30,-.326,.842),size=.007,deck=True)
deck_button('Engine_Stop_Mushroom',(.62,-.28,.847),.016,red)
for x in [.59,.65]:cube('Stop_Button_Guard',(x,-.28,.860),(.006,.060,.027),steel)
legend('Engine_Stop_Legend','柴油机停机',(.62,-.33,.842),size=.0065,deck=True)
cube('Reverser_Mount',(.13,-.10,.846),(.055,.075,.010),black)
cube('Reverser_Handle',(.13,-.10,.868),(.012,.045,.025),black,bevel=.003)
legend('Reverser_Legend','后  空  前',(.13,-.155,.842),size=.0065,deck=True)
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
root['lower_structure']='continuous folded shell with recessed knees; no freestanding desk pedestals'
root['worktop_height_m']=.839
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in switch_objects if o.type=='MESH' for c in o.bound_box]
mounts[-1]['bounds']={'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]}
mounts[-1]['integration_scale']=3
report={'status':'revision 3: separated brake valves and auxiliary controls','estimated_dimensions':True,'width_m':1.55,'worktop_top_m':.839,
 'panel_back_tilt_degrees':15,'lower_structure':'continuous recessed shell','mounts':mounts,'apertures':apertures,
 'not_yet_validated':['full human reach envelope','valve handle swept volumes','driver sightlines','wiring and pipe routing'],
 'interactive_controller_and_switches':False,'automatic_brake_present':True,'independent_brake_present':True,
 'new_controls_unbound':['horn push','engine stop mushroom','reverser','warning lamps'],
 'incomplete_systems':['live speedometer','fuel gauge','electrical supply gauge','cab signal/ATP','vigilance','radio','wipers','parking brake','oil temperature simulation','equalizing reservoir']}
brake_pivots=[next(o for o in brake_objects if o.name==name) for name in ['Tall_Spindle_Pivot','Short_Spindle_Pivot']]
delta=brake_pivots[0].matrix_world.translation-brake_pivots[1].matrix_world.translation
report['brake_spindle_plan_separation_m']=math.hypot(delta.x,delta.y)
assert report['brake_spindle_plan_separation_m']>.20
(OUT/'console_layout_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
parts=[o for o in bpy.data.objects if o.type in {'MESH','EMPTY'}]
ensure_uvs([o for o in parts if o.type=='MESH'])
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
