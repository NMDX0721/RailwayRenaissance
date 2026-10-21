"""Thin printed dial details, separate aged metal finishes, and protective glass."""
import bpy
import sys
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/speedometer_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'speedometer_photo_refined.blend'))
root=bpy.data.objects['Speedometer_Photo_Study']
directory=ROOT/'Assets/Models/Cab/Textures/SpeedometerMetal'
directory.mkdir(parents=True,exist_ok=True)

def metal(name,base,rough,metallic,grain,seed):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    finish_material(mat,name,base,rough,metallic,0,grain,seed,directory)
    return mat
paint=metal('Instrument_OldPaint',(.019,.029,.026),.57,.02,.026,2311)
panel=metal('Instrument_SatinMetal',(.40,.38,.30),.43,.84,.007,2312)
rim=metal('Instrument_BlackenedSteel',(.014,.016,.014),.35,.66,.009,2313)
steel=metal('Instrument_ScrewSteel',(.19,.20,.18),.32,.90,.006,2314)
for name in ('Fixed_Square_Housing','Fixed_Foot','Top_Case_Lip'):
    obj=bpy.data.objects[name];obj.data.materials[0]=paint
    for m in obj.modifiers:
        if m.type=='BEVEL':m.width=min(m.width,.00065)
bpy.data.objects['Fixed_Front_Metal_Panel'].data.materials[0]=panel
bpy.data.objects['Dial_Dark_Bezel'].data.materials[0]=rim
bpy.data.objects['Fixed_Nameplate'].data.materials[0]=rim
for obj in bpy.data.objects:
    if obj.name.startswith('Panel_Screw'):obj.data.materials[0]=steel
    if obj.name.startswith('Tick_'):
        obj.scale.y*=.20
        obj.scale.x*=.68
        obj.location.y=-.05612
    if obj.name.startswith('Scale_') or obj.name=='Unit_kmh':obj.location.y=-.05622
yellow=bpy.data.objects['Dial_Yellow_Center']
for v in yellow.data.vertices:v.co.z*=.12
yellow.location.y=-.0561
mat=yellow.data.materials[0]
bs=mat.node_tree.nodes['Principled BSDF']
bs.inputs['Roughness'].default_value=.63
bs.inputs['Metallic'].default_value=0
cap=bpy.data.objects['Needle_Cap']
cap.scale.x*=.75;cap.scale.y*=.75
cap.data.materials[0]=rim

# A shallow real glass cover gives a narrow reflection rather than a coated-plastic dial.
glass=bpy.data.materials.new('Instrument_Protective_Glass');glass.use_nodes=True
bs=glass.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value=(.985,.99,.985,1)
bs.inputs['Transmission Weight'].default_value=1
bs.inputs['Roughness'].default_value=.10
bs.inputs['IOR'].default_value=1.46
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=.067,depth=.0007,
    location=(0,-.064,.104),rotation=(1.57079632679,0,0))
cover=bpy.context.object;cover.name='Fixed_Protective_Glass';cover.parent=root
cover.data.materials.append(glass)
# Keep a real cabinet joint and shallow tooling marks; do not invent readable serial text.
parts=[o for o in bpy.data.objects if o.type=='MESH']
ensure_uvs(parts)
bpy.context.view_layer.update()
report={'meshes':len(parts),'triangles':0,'historical_dimensions_verified':False,
        'glass_requires_transmission_support':True,'unity_validated':False}
for obj in parts:
    e=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
assert report['triangles']<30000
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,*parts,bpy.data.objects['Speed_Needle_Pivot']]:obj.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/speedometer_metal_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'speedometer_metal_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=48
scene.render.filepath=str(OUT/'speedometer_metal_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'speedometer_metal_refined.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
