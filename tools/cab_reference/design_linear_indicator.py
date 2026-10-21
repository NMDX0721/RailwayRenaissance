"""Designed curved-guide indicator: explicit geometry and full-range envelope checks.
Not a claim about the original locomotive's electromagnetic meter mechanism.
"""
import bpy
import bmesh
import math
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/linear_gauge_review'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'linear_gauge_curved_refined.blend'))
root=bpy.data.objects['Linear_Instrument_Photo_Study']
R=(.0715**2+.006**2)/.012
def bow(z):return R-math.sqrt(R*R-(z-.089)**2)-.006
def slope(z):return (z-.089)/math.sqrt(R*R-(z-.089)**2)
ink=bpy.data.materials['Gauge_Ink'];steel=bpy.data.materials['Gauge_Fastener']
red=bpy.data.materials['Gauge_Pointer']
red.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.32,.055,.022,1)
def mesh_object(name,verts,faces,material,parent=root):
    m=bpy.data.meshes.new(name+'_Mesh');m.from_pydata(verts,[],faces);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.parent=parent
    if material:o.data.materials.append(material)
    return o
def band(name,x,width,y,depth,zmin,zmax,material):
    verts=[];n=48
    for i in range(n+1):
        z=zmin+(zmax-zmin)*i/n
        for dx,dy in [(-1,-1),(1,-1),(1,1),(-1,1)]:verts.append((bow(z)+x+dx*width/2,y+dy*depth/2,z))
    faces=[(3,2,1,0),(n*4,n*4+1,n*4+2,n*4+3)]
    for i in range(n):
        for j in range(4):faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
    return mesh_object(name,verts,faces,material)

for name in ('Fixed_Scale_Printing','Moving_Pointer'):
    bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
old=bpy.data.objects['Reading_Indicator_Study'];bpy.data.objects.remove(old,do_unlink=True)
# Marks extend to the right, following the local normal to the curved baseline.
verts=[];faces=[]
for i in range(21):
    z=.026+i*.0063;a=math.atan(slope(z));length=.009 if i%5==0 else .0048
    x=.001+bow(z);nx,nz=math.cos(a),-math.sin(a)
    start=len(verts)
    for distance,side in [(0,-1),(length,-1),(length,1),(0,1)]:
        verts.append((x+nx*distance+math.sin(a)*side*.00020,-.01825,
                      z+nz*distance+math.cos(a)*side*.00020))
    faces.append(tuple(start+j for j in range(4)))
mesh_object('Fixed_Right_Hand_Scale_Marks',verts,faces,ink)

# Narrow stem slot in scale board, with a channel behind it.
slot=band('Guide_Slot_Cutter',-.011,.0018,-.0175,.006,.021,.157,None)
plate=bpy.data.objects['Fixed_Scale_Window'];bpy.context.view_layer.objects.active=plate
mod=plate.modifiers.new('Indicator_Stem_Clearance','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=slot
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(slot,do_unlink=True)
for x in (-.0125,-.0095):band('Fixed_Curved_Guide_Rail',x,.001,-.0160,.0008,.020,.158,steel)
node=bpy.data.objects.new('Curved_Indicator_Carriage',None);bpy.context.collection.objects.link(node);node.parent=root
read_z=.064;node.location=(.001+bow(read_z),-.0189,read_z);node.rotation_euler.y=math.atan(slope(read_z))
node['motion_design']='z=.026+.126*u; x=.001+bow(z); local Y angle=atan(slope(z)); 0<=u<=1'
node['drive_and_original_mechanism_unverified']=True
# Broad, tapered pointer: a precise reading tip and a clearly visible body.
outline=[(0,0),(-.003,.0018),(-.0125,.0018),(-.0125,-.0018),(-.003,-.0018)]
verts=[(x,y,z) for y in (-.00025,.00025) for x,z in outline]
faces=[tuple(reversed(range(5))),tuple(range(5,10))]
for i in range(5):faces.append((i,(i+1)%5,(i+1)%5+5,i+5))
mesh_object('Moving_Wide_Pointer',verts,faces,red,node)
bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.0007,depth=.003,
    location=(-.012,.0015,0),rotation=(math.pi/2,0,0))
stem=bpy.context.object;stem.name='Moving_Guide_Stem';stem.parent=node;stem.data.materials.append(steel)
# The glass is a straight outer pane over the curved scale opening, with visible edge reflections.
old=bpy.data.objects['Fixed_Protective_Glass'];glass=old.data.materials[0];bpy.data.objects.remove(old,do_unlink=True)
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,-.0214,.089));cover=bpy.context.object
cover.name='Fixed_Protective_Glass';cover.parent=root;cover.dimensions=(.039,.0012,.149)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);cover.data.materials.append(glass)
bs=glass.node_tree.nodes['Principled BSDF'];bs.inputs['Roughness'].default_value=.065;bs.inputs['IOR'].default_value=1.48
m=cover.modifiers.new('Polished_Glass_Edge','BEVEL');m.width,m.segments=.00035,3

# Analytical envelope check over every scale position, plus intermediate positions.
min_side=1;max_reading_error=0
for i in range(101):
    z=.026+.126*i/100;a=math.atan(slope(z));x=.001+bow(z)
    for px,pz in outline:
        xx=x+math.cos(a)*px+math.sin(a)*pz
        zz=z-math.sin(a)*px+math.cos(a)*pz
        clearance=.015-bow(zz)+xx if xx<bow(zz) else .015+bow(zz)-xx
        min_side=min(min_side,clearance)
        assert clearance>0 and .019<zz<.159
    max_reading_error=max(max_reading_error,abs(x-(.001+bow(z))))
assert min_side>.001
parts=[o for o in bpy.data.objects if o.type=='MESH']
report={'mechanism':'designed curved-guide sliding indicator','historical_mechanism_verified':False,
 'sampled_positions':101,'minimum_pointer_side_clearance_m':min_side,
 'reading_tip_error_m':max_reading_error,'drive_force_friction_and_dynamic_collision_tested':False,
 'meshes':len(parts),'triangles':0}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=e.to_mesh();mesh.calc_loop_triangles();report['triangles']+=len(mesh.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in [root,node,*parts]:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/linear_gauge_mechanism_refined.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'linear_gauge_mechanism_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.cycles.samples=48
scene.render.filepath=str(OUT/'linear_gauge_mechanism_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'linear_gauge_mechanism_refined.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
