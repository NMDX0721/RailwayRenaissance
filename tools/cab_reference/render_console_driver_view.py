"""Driver-eye review only; no ergonomics compliance is implied."""
import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/console_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'main_console_study.blend'))
scene=bpy.context.scene;camera=scene.camera
camera.location=bpy.data.objects['Driver_Eye_Reference'].matrix_world.translation
camera.rotation_euler=(Vector((-.08,.15,.96))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='PERSP';camera.data.lens=20;camera.data.clip_start=.01
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.cycles.samples=24
scene.render.filepath=str(OUT/'main_console_driver_view.png')
bpy.ops.render.render(write_still=True)
print('CONSOLE_DRIVER_VIEW_OK')
