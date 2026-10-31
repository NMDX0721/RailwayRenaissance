import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/Unity Project/RailwayRenaissance/tools/cab_reference')
files=['speedometer_review/speedometer_final.blend','pressure_review/duplex_air_gauges_final.blend','tachometer_review/diesel_tachometer_final.blend','temperature_review/temperature_gauges_final.blend','linear_gauge_review/linear_gauge_final.blend','brake_review/brake_final_with_handles.blend','controller_review/controller_handwheel_final.blend','switches_review/piano_switches_10gang_optimized.blend']
for f in files:
 if not (root/f).exists():print('MISSING',f);continue
 bpy.ops.wm.open_mainfile(filepath=str(root/f))
 pts=[o.matrix_world@Vector(c) for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_') for c in o.bound_box]
 print('ASSET_BOUNDS',f,json.dumps({'min':[min(p[i] for p in pts) for i in range(3)],'max':[max(p[i] for p in pts) for i in range(3)],'roots':[o.name for o in bpy.data.objects if o.type=='EMPTY' and o.parent is None]}))
