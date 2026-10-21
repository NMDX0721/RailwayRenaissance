"""Final material pass, layered glass and native source for Unity import."""
import bpy,sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/temperature_review'
sys.path.insert(0,str(Path(__file__).parent))
from pressure_preview_lighting import configure
bpy.ops.wm.open_mainfile(filepath=str(OUT/'temperature_gauges_refined.blend'))
for mat in bpy.data.materials:
    if not mat.use_nodes:continue
    bs=mat.node_tree.nodes.get('Principled BSDF')
    if not bs:continue
    if bs.inputs['Transmission Weight'].default_value>.5:
        bs.inputs['IOR'].default_value=1.52
        bs.inputs['Roughness'].default_value=.028
        bs.inputs['Base Color'].default_value=(.93,.975,.96,1)
    if 'Retainer' in mat.name:
        bs.inputs['Metallic'].default_value=.78
        for node in mat.node_tree.nodes:
            if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.18
    if mat.name=='Gauge_Fastener':
        bs.inputs['Metallic'].default_value=.9;bs.inputs['Roughness'].default_value=.43
    if mat.name=='Gauge_Scale':
        for node in mat.node_tree.nodes:
            if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.08
scene=bpy.context.scene;configure(scene)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.15
bpy.data.objects['Preview_Soft_Key'].data.energy=5
# Faint broad reflection adds a glass cue without a bright rectangle over the dial.
bpy.ops.object.light_add(type='AREA',location=(-.25,-.30,.35))
light=bpy.context.object;light.name='Preview_Glass_Fill';light.data.energy=.12;light.data.shape='DISK';light.data.size=.45
from mathutils import Vector
light.rotation_euler=(Vector((0,0,.09))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['Water_Oil_Temperature_Gauge_Pair']
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/water_oil_temperature_gauges_final.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene.render.filepath=str(OUT/'temperature_gauges_final_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'temperature_gauges_final.blend'))
bpy.ops.render.render(write_still=True)
print('TEMPERATURE_FINAL_MATERIALS_OK')
