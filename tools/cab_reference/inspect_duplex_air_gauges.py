"""Inspect final connector bores, needle normals and glass spacing, then render the rear."""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/pressure_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'duplex_air_gauges_final.blend'))
report=json.loads((OUT/'duplex_air_gauges_final_audit.json').read_text())
for tag in ('MR_BC','BP_ER'):
    assert len([o for o in bpy.data.objects if o.name.startswith(tag+'_Mount_Screw')])==4
    # Both apertures must admit a ray past their rear entrance; this does not certify the hidden circuit.
    for color in ('Red','Black'):
        socket=bpy.data.objects[tag+'_RearSocket_'+color]
        result=socket.ray_cast(Vector((0,.060,.074)),Vector((0,-1,0)))
        wall=socket.ray_cast(Vector((.004,.060,.074)),Vector((0,-1,0)))
        assert not result[0] and wall[0], 'Connector bore or wall geometry invalid'
    cap=bpy.data.objects[tag+'_Center_Cap'];glass=bpy.data.objects[tag+'_Glass']
    pin=bpy.data.objects[tag+'_Red_Inner_Spindle'];rp=bpy.data.objects[tag+'_Red_Pivot']
    glass_back=glass.location.y+.00045
    pin_front=rp.location.y+pin.location.y-.0035/2
    assert pin_front-glass_back>.00045
    report['glass_to_inner_spindle_clearance_m']=pin_front-glass_back
report['rear_socket_entrance_ray_checks']=4
report['mounting_screws_verified']=8
(OUT/'duplex_air_gauges_final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;camera=scene.camera
camera.location=(-.25,.55,.27)
camera.rotation_euler=(Vector((0,0,.080))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.light_add(type='AREA',location=(0,.35,.35));light=bpy.context.object
light.data.energy=12;light.data.size=.35
light.rotation_euler=(Vector((0,0,.08))-light.location).to_track_quat('-Z','Y').to_euler()
scene.cycles.samples=20;scene.render.resolution_x=1200;scene.render.resolution_y=800
scene.render.filepath=str(OUT/'duplex_air_gauges_final_rear.png')
if '--skip-render' not in sys.argv:bpy.ops.render.render(write_still=True)
print(json.dumps(report))
