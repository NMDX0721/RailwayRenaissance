"""Measure mounted geometry, distinguish static spacing from operational certification."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/console_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'main_console_study.blend'))
bpy.context.view_layer.update()
audit=json.loads((OUT/'console_layout_audit.json').read_text())
for entry in audit['mounts']:
    mount=bpy.data.objects[entry['tag']+'_Mount']
    pts=[obj.matrix_world@Vector(c) for obj in mount.children_recursive if obj.type=='MESH' for c in obj.bound_box]
    entry['bounds']={'min':[min(v[i] for v in pts) for i in range(3)],'max':[max(v[i] for v in pts) for i in range(3)]}
measures=[]
for name in ['Tall_Spindle_Pivot','Short_Spindle_Pivot']:
    pivot=bpy.data.objects[name];center=pivot.matrix_world.translation
    pts=[obj.matrix_world@Vector(v.co) for obj in pivot.children_recursive if obj.type=='MESH' for v in obj.data.vertices]
    radius=max(math.hypot(p.x-center.x,p.y-center.y) for p in pts)
    measures.append({'pivot':name,'center_m':list(center),'full_rotation_radial_envelope_m':radius,
        'moving_geometry_min_z_m':min(p.z for p in pts),'moving_geometry_max_z_m':max(p.z for p in pts)})
a,b=[Vector(m['center_m']) for m in measures];separation=math.hypot(a.x-b.x,a.y-b.y)
assert separation>.20
audit['brake_spindle_plan_separation_m']=separation
audit['brake_moving_geometry_envelopes']=measures
audit['conservative_plan_envelope_gap_m']=separation-sum(m['full_rotation_radial_envelope_m'] for m in measures)
audit['moving_geometry_vertical_gap_m']=measures[0]['moving_geometry_min_z_m']-measures[1]['moving_geometry_max_z_m']
audit['envelope_limitation']='unmodified mesh vertices, no hands/gloves or certified valve stops; not operational clearance certification'
(OUT/'console_layout_audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('CONSOLE_LAYOUT_CHECK',json.dumps({'spindle_spacing_m':separation,'conservative_plan_gap_m':audit['conservative_plan_envelope_gap_m'],'vertical_gap_m':audit['moving_geometry_vertical_gap_m']}))
