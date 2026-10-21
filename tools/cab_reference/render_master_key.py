import bpy
from mathutils import Vector
from pathlib import Path
p=Path(__file__).resolve().parent/'startup_review'
bpy.ops.wm.open_mainfile(filepath=str(p/'startup_controls_key_only.blend'))
s=bpy.context.scene;c=s.camera;c.location=(.0,-.09,.085);c.rotation_euler=(Vector((-.073,0,.025))-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=.075
s.render.resolution_x=900;s.render.resolution_y=700;s.render.filepath=str(p/'startup_controls_key_only.png');bpy.ops.render.render(write_still=True)
