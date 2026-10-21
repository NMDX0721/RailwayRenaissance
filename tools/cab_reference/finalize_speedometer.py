"""Final art pass: modest service details, optimized dial printing, original silhouette."""
import bpy
import math
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/speedometer_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'speedometer_tapered_refined.blend'))
root=bpy.data.objects['Speedometer_Photo_Study']
front=bpy.data.objects['Fixed_Inclined_Front_Assembly']
steel=bpy.data.materials['Instrument_ScrewSteel']
dark=bpy.data.materials['Dial_Ink']

def cylinder(name,r,depth,loc,parent,mat=steel,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=depth,location=loc,rotation=rotation)
    o=bpy.context.object;o.name,o.parent=name,parent;o.data.materials.append(mat)
    bevel=o.modifiers.new('Small_Tool_Radius','BEVEL');bevel.width,bevel.segments=.00018,2
    o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o

def box(name,loc,dims,parent,mat=dark):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name,o.parent=name,parent;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat);return o

for x in (-.081,.081):
    cylinder('Foot_Mount_Washer',.0037,.0007,(x,-.049,.0074),root)
    cylinder('Foot_Mount_Screw',.0024,.0017,(x,-.049,.0084),root)
    box('Foot_Screw_Slot',(x,-.049,.00927),(.0032,.0006,.00012),root)
for x in (-.022,.022):
    cylinder('Nameplate_Rivet',.0012,.0007,(x,-.0525,.020),front,rotation=(math.pi/2,0,0))
# Narrow gasket line instead of thick plastic-looking frame trim.
for x in (-.080,.080):
    box('Front_Service_Gasket',(x,-.0495,.097),(.00045,.001,.173),front)
# Back service-cover screws and cable gland are restrained invention, not model identification.
for x in (-.067,.067):
    cylinder('Rear_Cover_Screw',.0022,.0012,(x,.0367,.029),root,rotation=(math.pi/2,0,0))
cylinder('Rear_Cable_Gland',.006,.009,(0,.040,.036),root,rotation=(math.pi/2,0,0))
cylinder('Rear_Cable_Gland_Cap',.004,.001,(0,.045,.036),root,dark,rotation=(math.pi/2,0,0))

# All dial print marks are static and share a material; merge only these to reduce renderers.
printing=[o for o in bpy.data.objects if o.type=='MESH' and
          (o.name.startswith(('Tick_','Scale_','Screw_Slot')) or o.name=='Unit_kmh')]
bpy.ops.object.select_all(action='DESELECT')
for o in printing:o.select_set(True)
bpy.context.view_layer.objects.active=printing[0]
bpy.ops.object.join()
printing[0].name='Fixed_Dial_Printing'
parts=[o for o in bpy.data.objects if o.type=='MESH']
ensure_uvs(parts)
root['art_status']='final'
root['rear_service_details_designed']=True
root['historical_dimensions_verified']=False
report={'art_status':'final','meshes':len(parts),'triangles':0,'historical_dimensions_verified':False,
        'unity_validated':False,'glass_requires_transmission_support':True,
        'front_inclination_visual_estimate_deg':7}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles)
    assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
    e.to_mesh_clear()
assert report['triangles']<30000
assert len(bpy.data.objects['Speed_Needle_Pivot'].children)==1
bpy.ops.object.select_all(action='DESELECT')
for o in [root,front,*parts,bpy.data.objects['Speed_Needle_Pivot']]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/speedometer_final.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'speedometer_final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=48
scene.render.filepath=str(OUT/'speedometer_final_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'speedometer_final.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
