"""Export native Unity FBX models and explicit URP-compatible texture channels."""
import bpy
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'Assets/Models/Cab/UnityBrake'
OUT.mkdir(parents=True,exist_ok=True)
manifest = []
for variant in ('without_handles','with_handles'):
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'tools/cab_reference/brake_review/brake_final_{variant}.blend'))
    parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
    for mat in sorted({m for o in parts for m in o.data.materials if m},key=lambda m:m.name):
        if any(item['name']==mat.name for item in manifest):
            continue
        p = mat.node_tree.nodes.get('Principled BSDF')
        item = {'name':mat.name,'metallic':p.inputs['Metallic'].default_value,
                'smoothness':1-p.inputs['Roughness'].default_value,
                'color':list(p.inputs['Base Color'].default_value)}
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
    bpy.ops.object.select_all(action='DESELECT')
    for o in [*parts,bpy.data.objects['Brake_Valve_Photo_Study'],bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'BrakeValves_{variant}.fbx'),use_selection=True,
        object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
        bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
(OUT/'materials.json').write_text(json.dumps({'materials':manifest},indent=2),encoding='utf-8')
