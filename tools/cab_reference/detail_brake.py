"""Close-photo casting details. Hidden geometry remains a visual approximation."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'brake_valves_photo_refined.blend'))
root = bpy.data.objects['Brake_Valve_Photo_Study']
silver = bpy.data.materials['Brake_Oxidized_Silver']
steel = bpy.data.materials['Brake_Worn_Steel']
parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]

def block(name,loc,dims,material=silver):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.parent = root
    o.data.materials.append(material)
    bevel = o.modifiers.new('Casting_Radius','BEVEL')
    bevel.width,bevel.segments = .0012,3
    o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(o)
    return o

# The close photo shows a broad lower housing rather than the former long thin neck.
cx,cy = -.024,.037
for v in bpy.data.objects['Tall_Lower_Casting'].data.vertices:
    if v.co.z>=.065:
        delta = Vector((v.co.x-cx,v.co.y-cy))
        radius = delta.length
        if radius>.005:
            desired = .031 if v.co.z<.11 else .029
            delta *= desired/radius
            v.co.x,v.co.y = cx+delta.x,cy+delta.y

# Shorten the exposed stub visually and make its cap less like a plain smooth tube.
for label in ('Tall','Short'):
    shaft = bpy.data.objects[label+'_Exposed_Spindle']
    for poly in shaft.data.polygons:
        poly.use_smooth = False

for index,a in enumerate((0,math.pi/2,math.pi,3*math.pi/2)):
    # Four broad casting ribs following the bell-shaped feet, not thin decorative fins.
    for label,center in [('Tall',(-.024,.037)),('Short',(.030,-.054))]:
        o = block(label+'_Foot_Rib_'+str(index),
                  (center[0]+.031*math.cos(a),center[1]+.031*math.sin(a),.014),
                  (.020,.009,.020))
        o.rotation_euler.y = math.radians(-25)
        o.rotation_euler.z = a

for index,a in enumerate((0,math.pi)):
    o = block('Tall_Lower_Flange_Ear_'+str(index),(cx+.036*math.cos(a),cy,.054),(.016,.022,.012))
    o.rotation_euler.z = a
    o = block('Tall_Cap_Ear_'+str(index),(cx+.026*math.cos(a),cy,.173),(.013,.018,.014))
    o.rotation_euler.z = a

# Visible front offset boss and two shoulder pads; no invented lettering.
block('Tall_Front_Cast_Boss',(cx,cy-.029,.090),(.025,.011,.027))
for dx in (-.022,.022):
    block('Tall_Shoulder_Pad_'+str(dx),(cx+dx,cy-.012,.121),(.012,.015,.008))
for dx in (-.042,.042):
    block('Short_Mounting_Lug_'+str(dx),(.030+dx,-.054,.005),(.017,.024,.010))

# Reuse planar coordinates for the small cast additions, keeping their grain subtle.
for o in parts:
    if o.data.uv_layers:
        continue
    uv = o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        drop = max(range(3),key=lambda j:abs(p.normal[j]))
        axes = [j for j in range(3) if j!=drop]
        for i in p.loop_indices:
            v = o.data.vertices[o.data.loops[i].vertex_index].co
            uv.data[i].uv = (v[axes[0]]*10+.5,v[axes[1]]*10+.5)

bpy.context.view_layer.update()
report = {'dimensions_estimated':True,'type_unconfirmed':True,'handle_geometry_pending':True,
          'meshes':len(parts),'triangles':0}
for o in parts:
    e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
    m = e.to_mesh()
    m.calc_loop_triangles()
    report['triangles'] += len(m.loop_triangles)
    e.to_mesh_clear()
assert report['triangles']<25000
bpy.ops.object.select_all(action='DESELECT')
for o in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
    o.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/brake_valves_photo_detailed.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'brake_detailed_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene = bpy.context.scene
scene.render.filepath = str(OUT/'brake_detailed_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_valves_photo_detailed.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
