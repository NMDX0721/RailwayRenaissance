"""Photo-led narrow diesel tachometer using the established cab instrument family.
Scale selection is researched; the curved guide remains an explicitly designed visual mechanism.
"""
import bpy, math, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/tachometer_review'
OUT.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'tools/cab_reference/linear_gauge_review/linear_gauge_final.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study'];root.name='Diesel_Tachometer_Photo_Study'
for key in list(root.keys()):
    if key!='_RNA_UI':del root[key]
root['quantity']='diesel engine rotational speed / r/min'
root['full_scale_rpm']=1500;root['minor_division_rpm']=50;root['major_division_rpm']=250
root['historical_gauge_identity_and_sensor_interface_verified']=False
root['mechanism']='designed curved-guide visual indicator; not an identified YDC-1 movement'
root['simulation_source']='CabEngineState.EngineRpm; binding not implemented yet'
root['reference_note']='DF4C repair manual 430-1000 r/min; not an automatic specification for early DF4-0001'
for name in ('Scale_Unit_A','Scale_DC_Identification'):
    bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
ink=bpy.data.materials['Gauge_Ink'];font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc')
def text(body,loc,size,name,chinese=False):
    bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object
    o.name=name;o.parent=root;o.data.body=body;o.data.size=size;o.data.align_x='CENTER';o.data.align_y='CENTER'
    if chinese:o.data.font=font
    o.data.resolution_u=4;o.data.materials.append(ink)
    bpy.ops.object.convert(target='MESH')
text('r/min',(-.0065,-.01830,.024),.003,'RPM_Unit')
label=ink.copy();label.name='RPM_Case_Legend'
label.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.55,.55,.45,1)
text('柴油机转速',(0,-.0198,.172),.0028,'RPM_Instrument_Name',True)
bpy.context.object.data.materials[0]=label
pointer=bpy.data.objects['Moving_Wide_Pointer'];pointer.name='RPM_Reading_Head'
mat=ink.copy();mat.name='RPM_Black_Indicator';mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.60
pointer.data.materials[0]=mat
node=bpy.data.objects['Curved_Indicator_Carriage'];node.name='Tachometer_Reading_Carriage'
R=(.0715**2+.006**2)/.012;low=math.asin(-.063/R)
u=650/1500;theta=low-2*low*u
node.location=(.001+R-R*math.cos(theta)-.006,-.0189,.089+R*math.sin(theta))
node.rotation_euler.y=theta
node['motion_design']='u=clamp(rpm/1500,0,1); theta=lerp(asin(-.063/R),asin(.063/R),u); z=.089+R*sin(theta); x=.001+R-R*cos(theta)-.006; rotationY=theta'
node['preview_rpm']=650
node['drive_and_original_mechanism_unverified']=True
metal=bpy.data.materials['Gauge_Fastener']
insulator=ink.copy();insulator.name='RPM_Connector_Insulator'
insulator.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.80
def cyl(name,r,depth,loc,mat,vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc,rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name=name;o.parent=root;o.data.materials.append(mat);return o
cyl('RPM_Rear_Connector_Seat',.0065,.002,(0,.016,.050),metal)
cyl('RPM_Recessed_Contact_Insert',.005,.004,(0,.019,.050),insulator)
vs=[];fs=[];n=64
for y,r in [(.016,.005),(.016,.006),(.024,.006),(.024,.005)]:
    for i in range(n):
        a=2*math.pi*i/n;vs.append((r*math.sin(a),y,.050+r*math.cos(a)))
for j in range(4):
    for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i))
m=bpy.data.meshes.new('RPM_Connector_Shell');m.from_pydata(vs,[],fs);m.update()
o=bpy.data.objects.new('RPM_Connector_Shell',m);bpy.context.collection.objects.link(o);o.parent=root;o.data.materials.append(metal)
for i in range(3):
    a=math.pi*2*i/3
    cyl('RPM_Recessed_Contact_'+str(i),.0006,.002,(.0024*math.sin(a),.022,.050+.0024*math.cos(a)),metal)
root['rear_connector']='unassigned three-contact visual interface; not a specified pinout or rated sensor input'
parts=[o for o in bpy.data.objects if o.type=='MESH'];report={'status':'tachometer first design',
 'range_rpm':[0,1500],'minor_division_rpm':50,'major_division_rpm':250,'preview_rpm':650,
 'historical_identity_verified':False,'unity_bound':False,'meshes':len(parts),'triangles':0}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/diesel_tachometer_study.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene;configure(scene);scene.render.filepath=str(OUT/'diesel_tachometer_reference.png')
scene.render.resolution_x=800;scene.render.resolution_y=1100;scene.cycles.samples=32
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'diesel_tachometer_study.blend'))
(OUT/'diesel_tachometer_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
