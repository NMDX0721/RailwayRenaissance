"""Export native Unity FBX models and explicit URP-compatible texture channels."""
import bpy
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'Assets/Models/Cab/UnityTemperatureGauges'
OUT.mkdir(parents=True,exist_ok=True)
manifest = []
for variant in ('final',):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'tools/cab_reference/temperature_review/temperature_gauges_final.blend'))
    parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
    for mat in sorted({m for o in parts for m in o.data.materials if m},key=lambda m:m.name):
        if any(item['name']==mat.name for item in manifest):
            continue
        p = mat.node_tree.nodes.get('Principled BSDF')
        item = {'name':mat.name,'metallic':p.inputs['Metallic'].default_value,
                'smoothness':1-p.inputs['Roughness'].default_value,
                'transmission':p.inputs['Transmission Weight'].default_value,'color':list(p.inputs['Base Color'].default_value)}
        for socket,suffix in [('Base Color','BaseColor'),('Normal','Normal')]:
            links = p.inputs[socket].links
            if not links:
                continue
            node = links[0].from_node
            if socket=='Normal':
                node = node.inputs['Color'].links[0].from_node
            img = node.image
            target = OUT/(mat.name+'_'+suffix+'.png')
            img.filepath_raw,img.file_format = str(target),'PNG'
            img.save()
            item[suffix] = target.relative_to(ROOT).as_posix()
        if p.inputs['Roughness'].links:
            rough = p.inputs['Roughness'].links[0].from_node.image
            values = list(rough.pixels)
            packed = bpy.data.images.new(mat.name+'_MetallicSmoothness',width=rough.size[0],height=rough.size[1],alpha=True)
            packed.colorspace_settings.name = 'Non-Color'
            for i in range(0,len(values),4):
                values[i:i+4] = [item['metallic'],0,0,1-values[i]]
            packed.pixels.foreach_set(values)
            target = OUT/(mat.name+'_MetallicSmoothness.png')
            packed.filepath_raw,packed.file_format = str(target),'PNG'
            packed.save()
            item['MetallicSmoothness'] = target.relative_to(ROOT).as_posix()
        manifest.append(item)
    calibration=[]
    for tag in ('Water','Oil'):
        for name,loc in [('Origin',(0,0,0)),('AxisX',(.01,0,0)),('AxisY',(0,.01,0)),('AxisZ',(0,0,.01))]:
            node=bpy.data.objects.new(tag+'_Calibration_'+name,None);bpy.context.collection.objects.link(node)
            node.parent=bpy.data.objects[tag+'_Temperature_Meter'];node.location=loc;calibration.append(node)
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.objects:
        if o.type in {'MESH','EMPTY'}:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/'TemperatureGauges_final.fbx'),use_selection=True,
        object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
        bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
(OUT/'materials.json').write_text(json.dumps({'materials':manifest},indent=2),encoding='utf-8')
