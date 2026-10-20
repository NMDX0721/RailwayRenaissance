"""Render the current editable cuboid study from a higher reference-photo view."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/cab_reference/switches_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'piano_switches_10gang_optimized.blend'))
scene = bpy.context.scene
camera = scene.camera
camera.location = (.067,-.14,.18)
camera.rotation_euler = (Vector((0,0,.002))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x = 1200
scene.render.resolution_y = 760
scene.render.resolution_percentage = 100
scene.render.filepath = str(OUT/'final_material_reference.png')
bpy.ops.render.render(write_still=True)
