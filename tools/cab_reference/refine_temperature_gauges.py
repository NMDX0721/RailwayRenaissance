"""Refine readability and validate the two independently calibrated temperature indicators."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/temperature_review'
profile=json.loads((ROOT/'Assets/Resources/Cab/temperature_instrument_profile.json').read_text(encoding='utf-8'))
sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(OUT/'temperature_gauges_study.blend'))
R=(.0715**2+.006**2)/.012
low=math.asin(-.063/R)
def pose(u):
    a=low-2*low*u
    return .001+R-R*math.cos(a)-.006,.089+R*math.sin(a),a
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
report={'status':'refined game-art design','profile':profile,
 'historical_identity_verified':False,'unity_bound':False,'oil_temperature_signal_available':False,
 'alarm_thresholds_verified':False,'modules':[]}
red=bpy.data.materials.new('Temperature_Alarm_OxideRed');red.use_nodes=True
bs=red.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value=(.38,.018,.010,1)
bs.inputs['Roughness'].default_value=.72
report['alarm_design']={'water_red_start_C':100,'oil_red_start_C':110,
 'basis':'adjustable fictional retrofit design, not original locomotive limits',
 'runtime_alarm_implemented':False}
for tag,value in [('Water',82),('Oil',65)]:
    spec=profile[tag.lower()];lo=spec['minimumC'];hi=spec['maximumC']
    value=spec['previewC']
    moving=bpy.data.objects[tag+'_Temperature_Indicator']
    head=bpy.data.objects[tag+'_RPM_Reading_Head']
    # Keep the tip on the calibrated baseline; thin the blade vertically.
    for v in head.data.vertices:v.co.z*=.55
    for mod in head.modifiers:
        if mod.type=='BEVEL':mod.width=min(mod.width,.00006);mod.segments=2
    moving.location.y=-.01855
    moving['front_to_scale_separation_m']=.00025
    moving['construction']='thin reading blade; visual guide design, not verified original movement'
    minimum=1.;baseline_error=0.
    ticks=bpy.data.objects[tag+'_Fixed_Right_Hand_Scale_Marks']
    # Replace inherited RPM marks and labels with this channel's physical scale.
    ink=ticks.data.materials[0]
    for obj in list(bpy.data.objects):
        if obj.name.startswith(tag+'_Value_'):bpy.data.objects.remove(obj,do_unlink=True)
    verts=[];polys=[];count=int((hi-lo)/spec['minorStepC'])
    module=bpy.data.objects[tag+'_Temperature_Meter']
    for i in range(count+1):
        temperature=lo+i*spec['minorStepC'];x,z,a=pose(i/count)
        major=abs(temperature/spec['majorStepC']-round(temperature/spec['majorStepC']))<1e-6
        length=.0035 if major else .0018
        for dx,dz in [(0,-.000075),(length,-.000075),(length,.000075),(0,.000075)]:
            verts.append((x+math.cos(a)*dx+math.sin(a)*dz,-.0183,z-math.sin(a)*dx+math.cos(a)*dz))
        n=4*i;polys.append((n,n+1,n+2,n+3))
        if major:
            bpy.ops.object.text_add(location=(x+.0046,-.0183,z-math.sin(a)*.0036),rotation=(math.pi/2,0,0))
            obj=bpy.context.object;obj.name=tag+'_Value_'+str(temperature);obj.parent=module
            obj.data.body=str(temperature);obj.data.size=.00315;obj.data.align_y='CENTER'
            obj.data.resolution_u=4;obj.data.materials.append(ink);bpy.ops.object.convert(target='MESH')
    mesh=bpy.data.meshes.new(tag+'_Calibrated_Temperature_Marks');mesh.from_pydata(verts,[],polys);mesh.materials.append(ink)
    ticks.data=mesh
    for i,p in enumerate(ticks.data.polygons):
        a=ticks.data.vertices[p.vertices[0]].co
        b=ticks.data.vertices[p.vertices[3]].co
        baseline=(a+b)/2;x,z,_=pose(i/count)
        baseline_error=max(baseline_error,math.hypot(baseline.x-x,baseline.z-z))
    assert baseline_error<1e-6
    for i in range(361):
        x,z,a=pose(i/360)
        for v in head.data.vertices:
            xx=x+math.cos(a)*v.co.x+math.sin(a)*v.co.z
            zz=z-math.sin(a)*v.co.x+math.cos(a)*v.co.z
            gap=.015-abs(xx-bow(zz));minimum=min(minimum,gap)
            assert gap>.001 and .019<zz<.159
    x,z,a=pose((value-lo)/(hi-lo));moving.location=(x,-.01855,z);moving.rotation_euler.y=a
    moving['motion_design']='u=clamp((raw_C-minimum_C)/(maximum_C-minimum_C),0,1); retain raw and under/overrange flags'
    module=bpy.data.objects[tag+'_Temperature_Meter']
    start=spec['redStartC']
    module['range_C']=[lo,hi];module['minor_division_C']=spec['minorStepC'];module['major_division_C']=spec['majorStepC']
    module['red_zone_start_C']=start
    module['red_zone_basis']='game retrofit design, not certified engine safety limit'
    vertices=[];faces=[]
    for j in range(61):
        x,z,a=pose((start+(hi-start)*j/60-lo)/(hi-lo))
        vertices.extend([(x-.0016,-.01834,z),(x-.0008,-.01834,z)])
        if j:faces.append((2*j-2,2*j-1,2*j+1,2*j))
    mesh=bpy.data.meshes.new(tag+'_Alarm_Band_Mesh');mesh.from_pydata(vertices,[],faces)
    band=bpy.data.objects.new(tag+'_Alarm_Red_Zone',mesh)
    bpy.context.collection.objects.link(band);band.parent=module;mesh.materials.append(red)
    assert len(mesh.polygons)==60
    module['signal_chain']='matched temperature sender/converter -> calibrated electrical indicator; exact electrical interface unassigned'
    report['modules'].append({'name':tag,'preview_C':value,'travel_samples':361,
      'range_C':[lo,hi],'scale_baselines_checked':count+1,'baseline_error_m':baseline_error,'minimum_side_clearance_m':minimum})
# Coated metal and exposed steel remain different materials; no invented corrosion on the scale.
for name in ['Gauge_BlackPaint','RPM_BlackenedSteel_Retainer']:
    mat=bpy.data.materials.get(name)
    if mat:
        for n in mat.node_tree.nodes:
            if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.22
metal=bpy.data.materials['Gauge_Fastener'].node_tree.nodes['Principled BSDF']
metal.inputs['Roughness'].default_value=.52
glass=bpy.data.objects['Water_Fixed_Protective_Glass'].data.materials[0]
glass.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.045
parts=[o for o in bpy.data.objects if o.type=='MESH']
report['meshes']=len(parts)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['Water_Oil_Temperature_Gauge_Pair']
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/water_oil_temperature_gauges_refined.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'temperature_gauges_refined_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.15
bpy.data.objects['Preview_Soft_Key'].data.energy=5
scene.camera.location=(.12,-.48,.18)
scene.camera.rotation_euler=(Vector((0,0,.089))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=.225
scene.render.filepath=str(OUT/'temperature_gauges_refined_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'temperature_gauges_refined.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
