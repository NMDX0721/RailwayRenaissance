"""Photo-led wedge cabinet study; inclination is estimated, not measured."""
import bpy
import math
import json
import bmesh
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/speedometer_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'speedometer_metal_refined.blend'))
root=bpy.data.objects['Speedometer_Photo_Study']
angle=math.radians(7)
hinge=Vector((0,-.049,.012))
transform=Matrix.Translation(hinge)@Matrix.Rotation(-angle,4,'X')@Matrix.Translation(-hinge)
front=bpy.data.objects.new('Fixed_Inclined_Front_Assembly',None)
bpy.context.collection.objects.link(front);front.parent=root
front['inclination_degrees_visual_estimate']=7
excluded={'Fixed_Square_Housing','Fixed_Foot','Side_Service_Plug'}
for obj in list(bpy.data.objects):
    if obj.parent==root and obj!=front and obj.name not in excluded:
        world=obj.matrix_world.copy()
        obj.parent=front
        obj.matrix_world=world
front.matrix_world=transform

housing=bpy.data.objects['Fixed_Square_Housing']
# Front plane matches the inclined faceplate; rear shell is slightly narrower and shallower.
for v in housing.data.vertices:
    p=housing.matrix_world@v.co
    if p.y<0:
        p.y+=math.tan(angle)*(p.z-.012)
    else:
        p.y=.036
        p.x*=.955
    v.co=housing.matrix_world.inverted()@p
bm=bmesh.new();bm.from_mesh(housing.data)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(housing.data);bm.free()
housing.name='Fixed_Tapered_Rear_Housing'
foot=bpy.data.objects['Fixed_Foot']
foot.scale.x*=.96;foot.scale.y*=.90;foot.scale.z*=.58
foot.location.z=.0035
plug=bpy.data.objects['Side_Service_Plug']
plug.location.x=.084
# Explicit geometry report separates the proposed inclination from source evidence.
parts=[o for o in bpy.data.objects if o.type=='MESH']
report={'meshes':len(parts),'triangles':0,'front_inclination_estimated_deg':7,
    'rear_half_depth_m':.036,'historical_dimensions_verified':False,'unity_validated':False}
bpy.context.view_layer.update()
for obj in parts:
    e=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,front,*parts,bpy.data.objects['Speed_Needle_Pivot']]:obj.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/speedometer_tapered_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'speedometer_tapered_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=32
scene.render.filepath=str(OUT/'speedometer_tapered_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'speedometer_tapered_refined.blend'))
bpy.ops.render.render(write_still=True)
scene.camera.location=(.5,0,.12)
scene.camera.rotation_euler=(Vector((0,0,.095))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=.25
scene.render.filepath=str(OUT/'speedometer_tapered_side.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
