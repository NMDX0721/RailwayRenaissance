"""Photo-led twin upright valve study; model identity and dimensions unverified."""
import bpy
import bmesh
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, color, rough=.48):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color,1)
    m.use_nodes = True
    p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = rough
    return m

body = mat('Valve_Clay',(.32,.33,.31))
hardware = mat('Hardware_Clay',(.21,.22,.21))
dark = mat('Joint_Clay',(.065,.07,.065))
root = bpy.data.objects.new('Brake_Valve_Photo_Study',None)
bpy.context.collection.objects.link(root)
root['reference'] = 'tools/cab_reference/DF4_0001_Cab.jpg'
root['type_unconfirmed'] = True
root['dimensions_estimated'] = True
root['missing_operating_handles_not_reconstructed'] = True
parts = []

def finish(obj,name,material,parent=root,bevel=.0007):
    obj.name = name
    obj.parent = parent
    obj.data.materials.append(material)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    if bevel:
        m = obj.modifiers.new('Small_Cast_Radius','BEVEL')
        m.width,m.segments = bevel,2
        obj.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(obj)
    return obj

def cyl(name,xy,z,r,h,material=body,parent=root,vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=h,location=(*xy,z))
    obj = finish(bpy.context.object,name,material,parent)
    for p in obj.data.polygons:
        p.use_smooth = len(p.vertices)==4
    return obj

def lathe(name,xy,profile):
    n = 64
    verts = [(xy[0]+r*math.cos(i*2*math.pi/n),xy[1]+r*math.sin(i*2*math.pi/n),z)
             for r,z in profile for i in range(n)]
    faces = []
    for j in range(len(profile)):
        for i in range(n):
            faces.append((j*n+i,j*n+(i+1)%n,((j+1)%len(profile))*n+(i+1)%n,((j+1)%len(profile))*n+i))
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,name,body)
    for p in mesh.polygons:
        zs = [mesh.vertices[i].co.z for i in p.vertices]
        p.use_smooth = max(zs)-min(zs)>.00001
    return obj

def bolts(prefix,xy,z,r,count=6):
    for i in range(count):
        a = i*2*math.pi/count
        cyl(prefix+str(i+1),(xy[0]+r*math.cos(a),xy[1]+r*math.sin(a)),z,.0032,.003,
            hardware,vertices=6)

tall = (-.024,.037)
short = (.030,-.054)
lathe('Tall_Lower_Casting',tall,[(.001,0),(.045,0),(.045,.008),(.036,.014),
                               (.030,.024),(.029,.043),(.033,.048),(.033,.060),
                               (.025,.066),(.022,.101),(.023,.121),(.001,.121)])
cyl('Tall_Lower_Flange',tall,.053,.037,.008)
bolts('Tall_Lower_Bolt_',tall,.059,.030)
cyl('Tall_Middle_Seam',tall,.124,.0225,.002,dark)
lathe('Tall_Upper_Casting',tall,[(.001,.125),(.024,.125),(.026,.132),(.025,.153),
                               (.021,.164),(.026,.169),(.026,.178),(.001,.178)])
bolts('Tall_Cap_Bolt_',tall,.180,.021,4)

lathe('Short_Lower_Casting',short,[(.001,0),(.043,0),(.044,.007),(.037,.014),
                                (.029,.021),(.028,.033),(.033,.040),(.033,.050),(.001,.050)])
cyl('Short_Flange',short,.050,.036,.008)
bolts('Short_Flange_Bolt_',short,.056,.029)
cyl('Short_Cover',short,.060,.026,.009)

# Separate exposed shaft stubs, not a claim of working handle geometry or travel.
for label,xy,z,r,h in [('Tall',tall,.190,.016,.023),('Short',short,.082,.013,.035)]:
    pivot = bpy.data.objects.new(label+'_Spindle_Pivot',None)
    bpy.context.collection.objects.link(pivot)
    pivot.parent = root
    pivot.location = (*xy,z-h/2)
    pivot['travel_unverified'] = True
    shaft = cyl(label+'_Exposed_Spindle',(0,0),h/2,r,h,hardware,pivot,vertices=16)

bpy.context.view_layer.update()
report = {'type_unconfirmed':True,'dimensions_estimated':True,'meshes':len(parts),'triangles':0}
for obj in parts:
    e = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    m = e.to_mesh()
    m.calc_loop_triangles()
    report['triangles'] += len(m.loop_triangles)
    assert all(math.isfinite(c) for v in m.vertices for c in v.co)
    e.to_mesh_clear()
assert report['triangles'] < 20000
(OUT/'brake_blockout_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/brake_valves_photo_blockout.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)

bpy.ops.mesh.primitive_plane_add(size=200)
bpy.context.object.name = 'Preview_Desk'
bpy.context.object.data.materials.append(mat('Preview_Wood',(.075,.038,.022),.65))
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Study_Studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.18,.18,.18,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .4
def point(obj):
    obj.rotation_euler = (Vector((0,0,.09))-obj.location).to_track_quat('-Z','Y').to_euler()
for loc,energy in [((-.3,-.4,.7),12),((.4,.3,.6),8)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    lamp = bpy.context.object
    lamp.data.energy,lamp.data.size = energy,.5
    point(lamp)
bpy.ops.object.camera_add(location=(.31,-.53,.29))
camera = bpy.context.object
point(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = .32
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x,scene.render.resolution_y = 1100,1000
scene.render.resolution_percentage = 100
scene.render.filepath = str(OUT/'brake_gray_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_valves_photo_blockout.blend'))
bpy.ops.render.render(write_still=True)
camera.location = (.34,.42,.30)
point(camera)
scene.render.filepath = str(OUT/'brake_gray_reverse.png')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
