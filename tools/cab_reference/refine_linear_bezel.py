"""Raised glass retainer and a single shared scale/guide baseline."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/linear_gauge_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_mechanism_refined.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
R=(.0715**2+.006**2)/.012
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
def cut(obj,cutter):
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('True_Clearance','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
def cube(name,loc,dims,mat=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.parent=root;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if mat:o.data.materials.append(mat)
    return o
# Restore the intact scale before removing the old left-hand slot.
plate=bpy.data.objects['Fixed_Scale_Window'];material=plate.data.materials[0]
with bpy.data.libraries.load(str(OUT/'linear_gauge_curved_refined.blend'),link=False) as (src,dst):
    dst.objects=['Fixed_Scale_Window']
source=dst.objects[0];plate.data=source.data.copy();plate.data.materials.clear();plate.data.materials.append(material)
bpy.data.objects.remove(source,do_unlink=True)
baseline=bpy.data.objects.get('Fixed_Scale_Rail')
if baseline:bpy.data.objects.remove(baseline,do_unlink=True)
# Reuse the actual curved guides as the slot cutting profile; move the whole guide system.
rails=[o for o in bpy.data.objects if o.name.startswith('Fixed_Curved_Guide_Rail')]
for rail in rails:
    for v in rail.data.vertices:v.co.x+=.012
verts=[];faces=[];n=48
for i in range(n+1):
    z=.021+.136*i/n
    for dx,dy in [(-1,-1),(1,-1),(1,1),(-1,1)]:verts.append((.001+bow(z)+dx*.0009,-.0175+dy*.004,z))
faces=[(3,2,1,0),(n*4,n*4+1,n*4+2,n*4+3)]
for i in range(n):
    for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
mesh=bpy.data.meshes.new('Shared_Slot_Cutter');mesh.from_pydata(verts,[],faces);mesh.update()
o=bpy.data.objects.new('Shared_Slot_Cutter',mesh);bpy.context.collection.objects.link(o);cut(plate,o)
stem=bpy.data.objects['Moving_Guide_Stem'];stem.location.x=0
# Keep the upper mounting screw and its engraved slot clear of the raised retainer.
for obj in bpy.data.objects:
    if obj.name.startswith(('Fixed_Frame_Screw','Frame_Screw_Slot')) and obj.location.z>.16:
        obj.location.z+=.0055

paint=bpy.data.materials['Gauge_BlackPaint']
bezel=cube('Fixed_Raised_Metal_Bezel',(0,-.0230,.089),(.046,.0045,.156),paint)
verts=[]
for i in range(n+1):
    z=.019+.140*i/n
    for dx,dy in [(-1,-1),(1,-1),(1,1),(-1,1)]:verts.append((bow(z)+dx*.015,-.023+dy*.010,z))
mesh=bpy.data.meshes.new('Curved_Bezel_Window');mesh.from_pydata(verts,[],faces);mesh.update()
cutter=bpy.data.objects.new('Curved_Bezel_Window',mesh);bpy.context.collection.objects.link(cutter);cut(bezel,cutter)
m=bezel.modifiers.new('Retainer_Edge_Radius','BEVEL');m.width,m.segments=.0006,3
bezel.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
glass=bpy.data.objects['Fixed_Protective_Glass'];glass.location.y=-.0217;glass.scale.x*=.044/.039
bs=glass.data.materials[0].node_tree.nodes['Principled BSDF']
bs.inputs['Roughness'].default_value=.045;bs.inputs['IOR'].default_value=1.50
# A narrow studio strip lets the real glass shader reflect without obscuring the full scale.
bpy.ops.object.light_add(type='AREA',location=(-.16,-.40,.105));light=bpy.context.object
light.name='Preview_Glass_Strip';light.data.energy=.8;light.data.shape='RECTANGLE'
light.data.size=.024;light.data.size_y=.21
light.rotation_euler=(Vector((0,0,.089))-light.location).to_track_quat('-Z','Y').to_euler()
root['shared_scale_and_guide_baseline']=True
root['historical_mechanism_verified']=False
parts=[o for o in bpy.data.objects if o.type=='MESH'];report={'meshes':len(parts),'triangles':0,
 'shared_baseline':True,'bezel_front_m':-.02525,'glass_front_m':-.0223,
 'glass_to_pointer_clearance_m':.00195,'upper_screw_to_bezel_clearance_m':.0024,
 'original_mechanism_and_drive_unverified':True}
bpy.context.view_layer.update()
for obj in parts:
    e=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles();report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,*parts,bpy.data.objects['Curved_Indicator_Carriage']]:obj.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_raised_bezel.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_raised_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.filepath=str(OUT/'linear_gauge_raised_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_raised_bezel.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
