"""Separate water/oil temperature instruments in the established narrow cab family."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/temperature_review'
OUT.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'tools/cab_reference/tachometer_review/diesel_tachometer_final.blend'))
original=bpy.data.objects['Diesel_Tachometer_Photo_Study'];hierarchy=[original,*original.children_recursive]
pair=bpy.data.objects.new('Water_Oil_Temperature_Gauge_Pair',None);bpy.context.collection.objects.link(pair)
pair['historical_identity_and_sensor_interface_verified']=False
font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc')
ink=bpy.data.materials['Gauge_Ink'];legend=bpy.data.materials['RPM_Case_Legend']
R=(.0715**2+.006**2)/.012;low=math.asin(-.063/R)
def pose(u):
    a=low-2*low*u
    return .001+R-R*math.cos(a)-.006,.089+R*math.sin(a),a
def text(body,loc,size,parent,mat,name,chinese=False,center=False):
    bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object
    o.name=name;o.parent=parent;o.data.body=body;o.data.size=size;o.data.align_y='CENTER'
    if center:o.data.align_x='CENTER'
    if chinese:o.data.font=font
    o.data.resolution_u=4;o.data.materials.append(mat);bpy.ops.object.convert(target='MESH')
modules=[]
for index,(tag,title,value) in enumerate([('Water','冷却水温',82),('Oil','机油温度',65)]):
    mapping={}
    for old in hierarchy:
        o=old.copy()
        if old.type=='MESH':o.data=old.data.copy()
        bpy.context.collection.objects.link(o);o.name=tag+'_'+old.name;mapping[old]=o
    for old,o in mapping.items():o.parent=mapping.get(old.parent,pair)
    module=mapping[original];module.name=tag+'_Temperature_Meter';module.location.x=(index-.5)*.060
    for key in list(module.keys()):
        if key!='_RNA_UI':del module[key]
    module['quantity']=title;module['unit']='degree Celsius';module['range_C']=[0,150]
    module['minor_division_C']=5;module['major_division_C']=25;module['preview_C']=value
    module['preview_only_not_simulation_measurement']=True
    module['sensor_interface']='matched RTD and converter required; exact R0, curve and connector pinout not selected'
    module['mechanism']='designed curved-guide visual indicator; not an identified historical meter movement'
    for old,o in list(mapping.items()):
        if old.name.startswith('Scale_Value_') or old.name in ('RPM_Unit','RPM_Instrument_Name'):
            bpy.data.objects.remove(o,do_unlink=True)
    for i in range(7):
        x,z,a=pose(i/6)
        text(str(i*25),(x+.0046,-.0183,z-math.sin(a)*.0036),.0029,module,ink,tag+'_Value_'+str(i*25))
    text('℃',(-.007,-.0183,.024),.0038,module,ink,tag+'_Unit',True,True)
    text(title,(0,-.0198,.172),.0028,module,legend,tag+'_Instrument_Name',True,True)
    moving=mapping[bpy.data.objects['Tachometer_Reading_Carriage']]
    moving.name=tag+'_Temperature_Indicator';x,z,a=pose(value/150)
    moving.location=(x,-.0189,z);moving.rotation_euler.y=a
    for key in list(moving.keys()):
        if key!='_RNA_UI':del moving[key]
    moving['motion_design']='u=clamp(temperature_C/150,0,1); same calibrated arc-length mapping as the scale'
    moving['preview_C']=value
    modules.append(module)
for old in reversed(hierarchy):bpy.data.objects.remove(old,do_unlink=True)
parts=[o for o in bpy.data.objects if o.type=='MESH']
report={'status':'temperature pair first design','modules':2,'independent_indicators':2,
 'range_C':[0,150],'minor_division_C':5,'major_division_C':25,
 'preview_C':[82,65],'water_signal_available':True,'oil_temperature_signal_available':False,
 'historical_identity_verified':False,'unity_bound':False,'meshes':len(parts),'triangles':0}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=pair
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/water_oil_temperature_gauges_study.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'temperature_gauges_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.15
bpy.data.objects['Preview_Soft_Key'].data.energy=5
camera=scene.camera;camera.location=(.17,-.45,.19)
camera.rotation_euler=(Vector((0,0,.089))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=.235
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.cycles.samples=32
scene.render.filepath=str(OUT/'temperature_gauges_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'temperature_gauges_study.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
