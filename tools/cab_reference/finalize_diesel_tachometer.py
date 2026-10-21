"""Final game-art tachometer: PBR separation, connector fit, scale and travel checks."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/tachometer_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(OUT/'diesel_tachometer_study.blend'))
root=bpy.data.objects['Diesel_Tachometer_Photo_Study']
directory=ROOT/'Assets/Models/Cab/Textures/DieselTachometerFinal';directory.mkdir(parents=True,exist_ok=True)
retainer=bpy.data.materials.new('RPM_BlackenedSteel_Retainer');retainer.use_nodes=True
finish_material(retainer,'RPM_BlackenedSteel',(.016,.020,.018),.49,.72,0,.0035,6331,directory)
bpy.data.objects['Fixed_Raised_Metal_Bezel'].data.materials[0]=retainer
metal=bpy.data.materials['Gauge_Fastener'];bs=metal.node_tree.nodes['Principled BSDF']
bs.inputs['Metallic'].default_value=.88;bs.inputs['Roughness'].default_value=.45
bs.inputs['Base Color'].default_value=(.16,.175,.165,1)
for name in ('Gauge_Ink','RPM_Black_Indicator'):
    bs=bpy.data.materials[name].node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value=(.003,.004,.003,1)
    bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.63
for node in bpy.data.materials['Gauge_Scale'].node_tree.nodes:
    if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.12
glass=bpy.data.objects['Fixed_Protective_Glass'].data.materials[0]
glass.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.035
glass.node_tree.nodes['Principled BSDF'].inputs['IOR'].default_value=1.5
for obj in bpy.data.objects:
    if obj.name.startswith('Scale_Value_'):obj.scale*=1.10
bpy.data.objects['RPM_Unit'].scale*=1.10
# A shallow keyway establishes a unique mating orientation; no connector standard is claimed.
shell=bpy.data.objects['RPM_Connector_Shell']
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,.0235,.0558));cutter=bpy.context.object
cutter.parent=root;cutter.dimensions=(.0016,.0020,.0026)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.context.view_layer.objects.active=shell
mod=shell.modifiers.new('Connector_Orientation_Keyway','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
edge=shell.modifiers.new('Connector_Edge_Radius','BEVEL');edge.width=.00010;edge.segments=2
shell.modifiers.new('Connector_Weighted_Normals','WEIGHTED_NORMAL')
R=(.0715**2+.006**2)/.012;low=math.asin(-.063/R)
def pose(u):
    a=low-2*low*u
    return .001+R-R*math.cos(a)-.006,.089+R*math.sin(a),a
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
head=bpy.data.objects['RPM_Reading_Head'];node=bpy.data.objects['Tachometer_Reading_Carriage']
minimum=1
for i in range(361):
    x,z,a=pose(i/360)
    for v in head.data.vertices:
        xx=x+math.cos(a)*v.co.x+math.sin(a)*v.co.z
        zz=z-math.sin(a)*v.co.x+math.cos(a)*v.co.z
        gap=.015-abs(xx-bow(zz));minimum=min(minimum,gap)
        assert gap>.001 and .019<zz<.159
ticks=bpy.data.objects['Fixed_Right_Hand_Scale_Marks'];assert len(ticks.data.polygons)==31
error=0
for i,p in enumerate(ticks.data.polygons):
    a=ticks.data.vertices[p.vertices[0]].co;b=ticks.data.vertices[p.vertices[3]].co
    baseline=(a+b)/2;x,z,_=pose(i/30)
    delta=math.hypot(baseline.x-x,baseline.z-z);error=max(error,delta);assert delta<.000001
# Three recessed contacts: metal parts remain below the shell rim and separated in the insert.
pins=[bpy.data.objects['RPM_Recessed_Contact_'+str(i)] for i in range(3)]
pin_gap=min((pins[i].location-pins[j].location).length-.0012 for i in range(3) for j in range(i))
assert pin_gap>.0029
recess=.024-(pins[0].location.y+.001);assert recess>.0009
root['asset_status']='final game-art design; not historical or metrological certification'
root['scale_definition']='linear 0-1500 r/min; minor 50; major 250; preview 650'
root['signal_interface']='actual sensor/converter and supply not specified; no pinout assigned'
root['pulse_sensor_option']='if using pulse pickup: rpm=60*frequency_Hz/pulses_per_revolution; PPR must come from the selected sensor'
root['out_of_range_rule']='retain real input; clamp display; flag invalid or overrange; do not rescale engine physics silently'
parts=[o for o in bpy.data.objects if o.type=='MESH'];ensure_uvs(parts)
report={'status':'final game-art design','range_rpm':[0,1500],'minor_division_rpm':50,'major_division_rpm':250,
 'preview_rpm':650,'travel_samples':361,'scale_baselines_checked':31,'maximum_baseline_error_m':error,
 'minimum_pointer_side_clearance_m':minimum,'connector_contact_edge_clearance_m':pin_gap,
 'contact_recess_m':recess,'historical_identity_verified':False,'sensor_and_drive_certified':False,
 'unity_bound':False,'meshes':len(parts),'triangles':0}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/diesel_tachometer_final.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'diesel_tachometer_final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.15
bpy.data.objects['Preview_Soft_Key'].data.energy=5
scene.render.filepath=str(OUT/'diesel_tachometer_final_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'diesel_tachometer_final.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
