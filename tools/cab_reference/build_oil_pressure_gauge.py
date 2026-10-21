"""Matching narrow electric oil-pressure instrument; 0-1 MPa = 0-10 bar."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/oil_pressure_review';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'tools/cab_reference/tachometer_review/diesel_tachometer_final.blend'))
root=bpy.data.objects['Diesel_Tachometer_Photo_Study'];root.name='Oil_Pressure_Instrument'
moving=bpy.data.objects['Tachometer_Reading_Carriage'];moving.name='Oil_Pressure_Indicator'
for key in list(root.keys()):del root[key]
root['range_MPa']=[0,1];root['minor_MPa']=.02;root['major_MPa']=.10
root['design_basis']='electric sender gauge; 1 MPa = 10 bar; no specified original model or pinout'
root['low_pressure_alarm']='requires engine running, RPM and startup delay; no invented universal static red band'
for o in list(bpy.data.objects):
    if o.name.startswith('Scale_Value_') or o.name in ['RPM_Unit','RPM_Instrument_Name','Fixed_Right_Hand_Scale_Marks']:
        bpy.data.objects.remove(o,do_unlink=True)
font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc');ink=bpy.data.materials['Gauge_Ink']
def text(name,body,loc,size,chinese=False):
    bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object;o.name=name;o.parent=root
    o.data.body=body;o.data.size=size;o.data.align_y='CENTER';o.data.align_x='CENTER';o.data.resolution_u=4
    if chinese:o.data.font=font
    o.data.materials.append(ink);bpy.ops.object.convert(target='MESH')
R=(.0715**2+.006**2)/.012;low=math.asin(-.063/R)
def pose(u):
    a=low-2*low*u
    return .001+R-R*math.cos(a)-.006,.089+R*math.sin(a),a
verts=[];faces=[]
for i in range(51):
    x,z,a=pose(i/50);length=.0035 if i%5==0 else .0018
    for dx,dz in [(0,-.000075),(length,-.000075),(length,.000075),(0,.000075)]:
        verts.append((x+math.cos(a)*dx+math.sin(a)*dz,-.0183,z-math.sin(a)*dx+math.cos(a)*dz))
    n=i*4;faces.append((n,n+1,n+2,n+3))
    if i%5==0:text('OilPressure_Value_'+str(i),f'{i/50:.1f}',(x+.007,-.0183,z),.0029)
mesh=bpy.data.meshes.new('OilPressure_ScaleMesh');mesh.from_pydata(verts,[],faces)
o=bpy.data.objects.new('OilPressure_Scale',mesh);bpy.context.collection.objects.link(o);o.parent=root;mesh.materials.append(ink)
text('OilPressure_Unit','MPa',(-.007,-.0183,.024),.0029)
text('OilPressure_Title','机油压力',(0,-.0198,.172),.0028,True)
bpy.data.objects['OilPressure_Title'].data.materials[0]=bpy.data.materials['RPM_Case_Legend']
x,z,a=pose(.45);moving.location=(x,-.0189,z);moving.rotation_euler.y=a
for key in list(moving.keys()):del moving[key]
moving['motion']='0-1 MPa calibrated arc; reference 0.45 MPa; preserve raw and invalid state'
minimum=1
head=bpy.data.objects['RPM_Reading_Head']
for i in range(361):
    x,z,a=pose(i/360)
    for v in head.data.vertices:
        xx=x+math.cos(a)*v.co.x+math.sin(a)*v.co.z;zz=z-math.sin(a)*v.co.x+math.cos(a)*v.co.z
        bow=R-math.sqrt(R*R-(zz-.089)**2)-.006
        minimum=min(minimum,.015-abs(xx-bow));assert .015-abs(xx-bow)>.001
report={'range_MPa':[0,1],'equivalent_bar':[0,10],'minor_MPa':.02,'major_MPa':.1,'preview_MPa':.45,'travel_samples':361,'minimum_side_clearance_m':minimum,'historical_model_verified':False,'static_alarm_band':False}
(OUT/'oil_pressure_audit.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/oil_pressure_gauge_final.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene;configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.15;bpy.data.objects['Preview_Soft_Key'].data.energy=5
scene.render.filepath=str(OUT/'oil_pressure_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'oil_pressure_gauge_final.blend'))
bpy.ops.render.render(write_still=True)
print('OIL_PRESSURE_GAUGE_OK')
