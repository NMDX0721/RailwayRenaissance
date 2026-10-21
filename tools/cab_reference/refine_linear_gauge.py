"""Recessed edgewise gauge study; numerical scale and original mechanism unverified."""
import bpy
import math
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/linear_gauge_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_photo_blockout.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
directory=ROOT/'Assets/Models/Cab/Textures/LinearGauge';directory.mkdir(parents=True,exist_ok=True)
paint=bpy.data.materials['Gauge_BlackPaint']
finish_material(paint,'Linear_OldPaint',(.009,.013,.011),.51,.03,0,.020,2511,directory)
scale=bpy.data.materials['Gauge_Scale']
finish_material(scale,'Ivory',(.65,.61,.46),.61,0,0,.004,2512,directory)
frame=bpy.data.objects['Fixed_Front_Frame']
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,-.019,.089))
cutter=bpy.context.object;cutter.dimensions=(.030,.020,.140)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.context.view_layer.objects.active=frame
mod=frame.modifiers.new('True_Recessed_Window','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0)
bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.data.objects.remove(cutter,do_unlink=True)
# Scale and ink sit behind the metal bezel rather than on its outside surface.
bpy.data.objects['Fixed_Scale_Window'].location.y=-.0175
for name in ('Fixed_Scale_Rail','Fixed_Scale_Printing'):
    bpy.data.objects[name].location.y+=.0026
rail=bpy.data.objects['Fixed_Scale_Rail'];rail.scale.x*=.60
ink=bpy.data.objects['Fixed_Scale_Printing']
for v in ink.data.vertices:
    center=round(v.co.z/.0063)*.0063
    v.co.z=center+(v.co.z-center)*.62
for o in bpy.data.objects:
    if o.name.startswith('Fixed_Frame_Screw'):
        # Retain the steel screw heads and add a dark, fine slot.
        bpy.ops.mesh.primitive_cube_add(size=1,location=(o.location.x,-.0206,o.location.z))
        slot=bpy.context.object;slot.name='Frame_Screw_Slot';slot.parent=root
        slot.dimensions=(.0028,.00015,.00045)
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        slot.data.materials.append(bpy.data.materials['Gauge_Ink'])
indicator=bpy.data.objects['Reading_Pointer_Carriage'];indicator.name='Reading_Indicator_Study'
indicator['movement_axis']='unverified; no claim of a linear mechanical carriage'
pointer=bpy.data.objects['Moving_Pointer'];pointer.location.y=-.0186
pointer.scale.x*=.68;pointer.scale.z*=.60
for v in pointer.data.vertices:
    if v.co.x<0:v.co.z*=.20
glass=bpy.data.materials.new('Linear_Protective_Glass');glass.use_nodes=True
p=glass.node_tree.nodes['Principled BSDF'];p.inputs['Transmission Weight'].default_value=1
p.inputs['IOR'].default_value=1.46;p.inputs['Roughness'].default_value=.12
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,-.020,.089))
cover=bpy.context.object;cover.name='Fixed_Protective_Glass';cover.parent=root
cover.dimensions=(.030,.0006,.140)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
cover.data.materials.append(glass)
parts=[o for o in bpy.data.objects if o.type=='MESH'];ensure_uvs(parts)
bpy.context.view_layer.update()
report={'meshes':len(parts),'triangles':0,'range_units_mechanism_unverified':True,'unity_validated':False}
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in [root,indicator,*parts]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_photo_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_refined_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=32
scene.render.filepath=str(OUT/'linear_gauge_refined_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_photo_refined.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
