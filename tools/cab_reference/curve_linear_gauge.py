"""Curved visible scale silhouette from the user's close photo; housing stays straight."""
import bpy
import bmesh
import json
import math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/linear_gauge_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_photo_refined.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
def bow(z):
    dz=max(-.0715,min(.0715,z-.089))
    radius=(.0715**2+.006**2)/(.012)
    return radius-math.sqrt(radius*radius-dz*dz)-.006
def strip(name,width,depth,y,height,material,x=0):
    verts=[];n=40
    for i in range(n+1):
        z=.089-height/2+height*i/n
        for xx,yy in [(-1,-1),(1,-1),(1,1),(-1,1)]:
            verts.append((x+bow(z)+xx*width/2,y+yy*depth/2,z))
    faces=[(3,2,1,0),(n*4,n*4+1,n*4+2,n*4+3)]
    for i in range(n):
        for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
    mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(verts,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.parent=root
    if material:o.data.materials.append(material)
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        p=mesh.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((p.x-x-bow(p.z))/width+.5,(p.z-.089)/height+.5)
    return o
for name,width,depth,y,height,x in [('Fixed_Scale_Window',.031,.001,-.0175,.143,0),
        ('Fixed_Protective_Glass',.0295,.0006,-.020,.139,0),
        ('Fixed_Scale_Rail',.00036,.0002,-.0181,.128,.001)]:
    old=bpy.data.objects[name];material=old.data.materials[0]
    bpy.data.objects.remove(old,do_unlink=True)
    strip(name,width,depth,y,height,material,x)
# Rebuild the frame opening to follow the bowed scale instead of clipping its middle.
old=bpy.data.objects['Fixed_Front_Frame'];mat=old.data.materials[0]
bpy.data.objects.remove(old,do_unlink=True)
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,-.017,.087))
frame=bpy.context.object;frame.name='Fixed_Front_Frame';frame.parent=root
frame.dimensions=(.046,.005,.178);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
frame.data.materials.append(mat)
cutter=strip('Curved_Window_Cutter',.030,.030,-.019,.140,None)
bpy.context.view_layer.objects.active=frame
mod=frame.modifiers.new('Curved_Recess','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
bevel=frame.modifiers.new('Fine_Frame_Radius','BEVEL');bevel.width,bevel.segments=.0003,2
frame.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
printing=bpy.data.objects['Fixed_Scale_Printing']
for v in printing.data.vertices:
    p=printing.matrix_world@v.co
    row=.026+round((p.z-.026)/.0063)*.0063
    radius=(.0715**2+.006**2)/(.012)
    slope=(row-.089)/math.sqrt(radius*radius-(row-.089)**2)
    p.z-=slope*(p.x-.001)
    p.x+=bow(row)
    v.co=printing.matrix_world.inverted()@p
indicator=bpy.data.objects['Reading_Indicator_Study'];indicator.location.x+=bow(indicator.location.z)
root['visible_scale_bow_m_visual_estimate']=-.006
parts=[o for o in bpy.data.objects if o.type=='MESH']
report={'meshes':len(parts),'triangles':0,'scale_bow_visual_estimate_m':-.006,
        'mechanism_and_curvature_unverified':True,'unity_validated':False}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles()
    report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in [root,indicator,*parts]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_curved_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_curved_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.filepath=str(OUT/'linear_gauge_curved_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_curved_refined.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
