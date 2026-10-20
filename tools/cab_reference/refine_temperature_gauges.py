"""Refine readability and validate the two independently calibrated temperature indicators."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/temperature_review'
sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(OUT/'temperature_gauges_study.blend'))
R=(.0715**2+.006**2)/.012
low=math.asin(-.063/R)
def pose(u):
    a=low-2*low*u
    return .001+R-R*math.cos(a)-.006,.089+R*math.sin(a),a
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
report={'status':'refined game-art design','range_C':[0,150],'minor_C':5,'major_C':25,
 'historical_identity_verified':False,'unity_bound':False,'oil_temperature_signal_available':False,
 'alarm_thresholds_verified':False,'modules':[]}
for tag,value in [('Water',82),('Oil',65)]:
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
    assert len(ticks.data.polygons)==31
    for i,p in enumerate(ticks.data.polygons):
        a=ticks.data.vertices[p.vertices[0]].co
        b=ticks.data.vertices[p.vertices[3]].co
        baseline=(a+b)/2;x,z,_=pose(i/30)
        baseline_error=max(baseline_error,math.hypot(baseline.x-x,baseline.z-z))
    assert baseline_error<1e-6
    for i in range(361):
        x,z,a=pose(i/360)
        for v in head.data.vertices:
            xx=x+math.cos(a)*v.co.x+math.sin(a)*v.co.z
            zz=z-math.sin(a)*v.co.x+math.cos(a)*v.co.z
            gap=.015-abs(xx-bow(zz));minimum=min(minimum,gap)
            assert gap>.001 and .019<zz<.159
    x,z,a=pose(value/150);moving.location=(x,-.01855,z);moving.rotation_euler.y=a
    for obj in bpy.data.objects:
        if obj.name.startswith(tag+'_Value_'):obj.scale*=1.10
    module=bpy.data.objects[tag+'_Temperature_Meter']
    module['signal_chain']='matched temperature sender/converter -> calibrated electrical indicator; exact electrical interface unassigned'
    report['modules'].append({'name':tag,'preview_C':value,'travel_samples':361,
      'scale_baselines_checked':31,'baseline_error_m':baseline_error,'minimum_side_clearance_m':minimum})
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
