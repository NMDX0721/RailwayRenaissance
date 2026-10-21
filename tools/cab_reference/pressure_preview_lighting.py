"""Neutral, light-background asset review without rectangular glare on the glass."""
import bpy
from mathutils import Vector

def configure(scene):
    for o in list(bpy.data.objects):
        if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
    world=scene.world;world.use_nodes=True;nodes=world.node_tree.nodes;nodes.clear()
    output=nodes.new('ShaderNodeOutputWorld')
    environment=nodes.new('ShaderNodeBackground')
    environment.inputs['Color'].default_value=(.75,.78,.80,1)
    environment.inputs['Strength'].default_value=.8
    backdrop=nodes.new('ShaderNodeBackground')
    backdrop.inputs['Color'].default_value=(.82,.84,.86,1)
    backdrop.inputs['Strength'].default_value=1
    path=nodes.new('ShaderNodeLightPath');mix=nodes.new('ShaderNodeMixShader')
    links=world.node_tree.links
    links.new(path.outputs['Is Camera Ray'],mix.inputs[0])
    links.new(environment.outputs[0],mix.inputs[1]);links.new(backdrop.outputs[0],mix.inputs[2])
    links.new(mix.outputs[0],output.inputs['Surface'])
    bpy.ops.object.light_add(type='AREA',location=(-.30,-.40,.45))
    light=bpy.context.object;light.name='Preview_Soft_Key'
    light.data.energy=3;light.data.size=1.0
    light.visible_glossy=False
    light.rotation_euler=(Vector((0,0,.08))-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.film_transparent=False
    scene.view_settings.exposure=0

if __name__=='__main__':
    from pathlib import Path
    root=Path(__file__).resolve().parents[2]
    out=root/'tools/cab_reference/pressure_review'
    bpy.ops.wm.open_mainfile(filepath=str(out/'duplex_air_gauges_final.blend'))
    scene=bpy.context.scene;configure(scene)
    scene.render.filepath=str(out/'duplex_air_gauges_final_reference.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'duplex_air_gauges_final.blend'))
    bpy.ops.render.render(write_still=True)
