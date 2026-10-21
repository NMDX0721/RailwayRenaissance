"""Final game-art DC ammeter candidate; no historical or metrological certification."""
import bpy, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/linear_gauge_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_calibrated.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
directory=ROOT/'Assets/Models/Cab/Textures/LinearGaugeFinal';directory.mkdir(parents=True,exist_ok=True)
# Painted steel is a dielectric coating; exposed fasteners and rails carry metallic response.
paint=bpy.data.materials['Gauge_BlackPaint']
for node in list(paint.node_tree.nodes):
    if node.type!='BSDF_PRINCIPLED' and node.type!='OUTPUT_MATERIAL':paint.node_tree.nodes.remove(node)
finish_material(paint,'Final_BlackEnamel',(.007,.010,.009),.53,0,.035,.005,4201,directory)
bezel=paint.copy();bezel.name='Gauge_Aged_Retainer_Enamel'
# Independent texture maps avoid treating old paint as solid polished metal.
for node in list(bezel.node_tree.nodes):
    if node.type!='BSDF_PRINCIPLED' and node.type!='OUTPUT_MATERIAL':bezel.node_tree.nodes.remove(node)
finish_material(bezel,'Final_Retainer',(.010,.013,.011),.46,0,.045,.004,4202,directory)
bpy.data.objects['Fixed_Raised_Metal_Bezel'].data.materials[0]=bezel
steel=bpy.data.materials['Gauge_Fastener'];bs=steel.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value=(.13,.145,.14,1)
bs.inputs['Metallic'].default_value=.88;bs.inputs['Roughness'].default_value=.43
for matname in ('Gauge_Scale','Gauge_Pointer'):
    mat=bpy.data.materials[matname];bs=mat.node_tree.nodes['Principled BSDF']
    bs.inputs['Metallic'].default_value=0
    if matname=='Gauge_Pointer':
        bs.inputs['Roughness'].default_value=.57
        bs.inputs['Base Color'].default_value=(.22,.033,.014,1)
    for node in mat.node_tree.nodes:
        if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.25
glass=bpy.data.objects['Fixed_Protective_Glass'].data.materials[0]
glass.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.055
root['asset_status']='final game-art design candidate'
root['historical_mechanism_verified']=False
root['quantity_unit']='DC current / A'
root['measurement_chain']='primary current -> external shunt or sensor -> low-level meter input; interface rating not selected'
root['display_rule']='I_display=clamp(I_A,0,1500); out-of-range diagnostics belong to simulation, not inferred overload certification'
parts=[o for o in bpy.data.objects if o.type=='MESH'];ensure_uvs(parts)
bpy.context.view_layer.update()
report=json.loads((OUT/'linear_gauge_calibrated_audit.json').read_text())
report.update(status='final game-art candidate',meshes=len(parts),triangles=0,unity_imported=False,
              measurement_interface_selected=False,historical_range_verified=False)
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in [root,bpy.data.objects['Curved_Indicator_Carriage'],*parts]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_final.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.context.scene.render.filepath=str(OUT/'linear_gauge_final_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_final.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
