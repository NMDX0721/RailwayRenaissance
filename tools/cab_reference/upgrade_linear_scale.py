"""Engineering-labelled design candidate; not an identified DF4 instrument."""
import bpy, math, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/linear_gauge_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_raised_bezel.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
ink=bpy.data.materials['Gauge_Ink']
R=(.0715**2+.006**2)/.012
lo=math.asin((.026-.089)/R);hi=-lo
def pose(u):
    theta=lo+(hi-lo)*u
    return .001+R-R*math.cos(theta)-.006,.089+R*math.sin(theta),theta
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
# Screw heads sit on the solid top/bottom lands of the retainer, outside its aperture.
for o in bpy.data.objects:
    if o.name.startswith(('Fixed_Frame_Screw','Frame_Screw_Slot')):
        o.location.z=.163 if o.location.z>.1 else .015
        o.location.y=-.0260 if o.name.startswith('Frame_Screw_Slot') else -.0254
        assert o.location.z-.0021>=.011 and o.location.z+.0021<=.167
        assert o.location.z+.0021<.019 or o.location.z-.0021>.159
bpy.data.objects.remove(bpy.data.objects['Fixed_Right_Hand_Scale_Marks'],do_unlink=True)
verts=[];faces=[];labels=[]
def text(body,x,z,size,name):
    bpy.ops.object.text_add(location=(x,-.01830,z),rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name=name;o.parent=root
    o.data.body=body;o.data.size=size;o.data.align_y='CENTER';o.data.resolution_u=4
    o.data.materials.append(ink)
    bpy.ops.object.convert(target='MESH')
    return bpy.context.object
for i in range(31):
    x,z,a=pose(i/30);major=i%5==0
    length=.0036 if major else .0021
    width=.00028 if major else .00016
    start=len(verts)
    for d,s in [(0,-1),(length,-1),(length,1),(0,1)]:
        verts.append((x+math.cos(a)*d+math.sin(a)*s*width/2,-.01825,
                      z-math.sin(a)*d+math.cos(a)*s*width/2))
    faces.append(tuple(start+j for j in range(4)))
    if major:
        o=text(str(i*50),x+.0046,z-math.sin(a)*length,.0025,'Scale_Value_'+str(i*50))
        labels.append(o)
m=bpy.data.meshes.new('Calibrated_Right_Scale');m.from_pydata(verts,[],faces);m.update()
o=bpy.data.objects.new('Fixed_Right_Hand_Scale_Marks',m);bpy.context.collection.objects.link(o)
o.parent=root;o.data.materials.append(ink)
text('A',-.010,.1535,.004,'Scale_Unit_A')
text('DC',-.011,.024,.0027,'Scale_DC_Identification')
# Uniform current increments follow uniform arc length rather than equal vertical spacing.
node=bpy.data.objects['Curved_Indicator_Carriage'];x,z,a=pose(.30)
node.location=(x,-.0189,z);node.rotation_euler.y=a
node['motion_design']='u=I/1500; theta=lerp(asin((.026-.089)/R),asin((.152-.089)/R),u); z=.089+R*sin(theta); x=.001+R-R*cos(theta)-.006; rotationY=theta'
root['design_quantity']='DC current; optional traction-motor current application, not historical identification'
root['design_range_A']=1500;root['minor_division_A']=50;root['major_division_A']=250
root['shunt_or_sensor_interface']='required; ratio and input rating not selected'
root['accuracy_class_and_overload_limits']='not specified; do not infer from graduation'
minimum=1
outline=[(0,0),(-.003,.0018),(-.0125,.0018),(-.0125,-.0018),(-.003,-.0018)]
for i in range(301):
    x,z,a=pose(i/300)
    for px,pz in outline:
        xx=x+math.cos(a)*px+math.sin(a)*pz;zz=z-math.sin(a)*px+math.cos(a)*pz
        gap=.015-abs(xx-bow(zz));minimum=min(minimum,gap)
        assert gap>0 and .019<zz<.159
bpy.context.view_layer.update()
for o in labels:
    for v in o.data.vertices:
        p=o.matrix_world@v.co
        assert abs(p.x-bow(p.z))<.0145 and .019<p.z<.159, 'Scale text outside window'
parts=[o for o in bpy.data.objects if o.type=='MESH']
report={'range_A':[0,1500],'minor_division_A':50,'major_division_A':250,
 'tick_count':31,'label_count':7,'full_range_positions_checked':301,
 'minimum_pointer_side_clearance_m':minimum,'screws_on_bezel':True,
 'historical_identity_verified':False,'accuracy_or_drive_certified':False,'triangles':0}
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in [root,node,*parts]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_calibrated.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_calibrated_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.context.scene.render.filepath=str(OUT/'linear_gauge_calibrated_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_calibrated.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
