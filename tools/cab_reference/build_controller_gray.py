"""Photo-led, separate-part controller study. Dimensions and obscured details are provisional."""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/controller_review'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name,color,roughness=.48,metallic=.05):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color,1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = (*color,1)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    return mat

wheel_mat = material('Blockout_Grip',(.085,.09,.095),.38)
body_mat = material('Blockout_Housing',(.19,.20,.205))
plate_mat = material('Blockout_Base',(.12,.13,.135),.52)
hardware_mat = material('Blockout_Hardware',(.32,.33,.34),.35,.3)
shadow_mat = material('Socket_Interior',(.015,.017,.018),.8,0)
root = bpy.data.objects.new('Controller_Handwheel_Study',None)
bpy.context.collection.objects.link(root)
root['reference'] = 'tools/cab_reference/crop_levers_piano.jpg'
root['dimensions_estimated'] = True
root['obscured_spoke_layout_unverified'] = True
parts = []
wheel_pivot = bpy.data.objects.new('Handwheel_Pivot',None)
bpy.context.collection.objects.link(wheel_pivot)
wheel_pivot.parent = root
wheel_pivot.location = (-.025,.01,.074)
wheel_pivot['rotation_axis'] = 'local Z in Blender, converted to Y by glTF export'
wheel_pivot['mechanical_travel_unverified'] = True

def finish(obj,name,mat,parent=root,bevel=0):
    obj.name = name
    obj.parent = parent
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Small_Edge_Radius','BEVEL')
        mod.width,mod.segments = bevel,3
        obj.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    parts.append(obj)
    return obj

def cube(name,loc,dims,mat,parent=root,bevel=.001):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,name,mat,parent,bevel)

def cylinder(name,loc,radius,depth,mat,parent=root,vertices=64,bevel=.0006):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc)
    obj = finish(bpy.context.object,name,mat,parent,bevel)
    for polygon in obj.data.polygons:
        polygon.use_smooth = len(polygon.vertices)==4
    return obj

def lathe(name,profile,mat,parent=root,center=(0,0),segments=80):
    verts = [(center[0]+r*math.cos(i*2*math.pi/segments),
              center[1]+r*math.sin(i*2*math.pi/segments),z)
             for r,z in profile for i in range(segments)]
    faces = []
    for j in range(len(profile)):
        nj = (j+1)%len(profile)
        for i in range(segments):
            ni = (i+1)%segments
            faces.append((j*segments+i,j*segments+ni,nj*segments+ni,nj*segments+i))
    mesh = bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,name,mat,parent)
    for polygon in mesh.polygons:
        # Keep annular end faces flat, shade radial walls smoothly.
        zvals = [mesh.vertices[i].co.z for i in polygon.vertices]
        polygon.use_smooth = max(zvals)-min(zvals)>.00001
    return obj

# The installation plate and empty right-hand socket are visible in the photo.
plate = cube('Fixed_Mounting_Plate',(0,0,.006),(.31,.225,.012),plate_mat,bevel=0)
bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=.013,depth=.04,location=(.091,-.048,.006))
cutter = bpy.context.object
boolean = plate.modifiers.new('Socket_Opening','BOOLEAN')
boolean.operation = 'DIFFERENCE'
boolean.solver = 'EXACT'
boolean.object = cutter
bpy.context.view_layer.objects.active = plate
bpy.ops.object.modifier_apply(modifier=boolean.name)
bpy.data.objects.remove(cutter,do_unlink=True)
bevel = plate.modifiers.new('Plate_Corner_Radius','BEVEL')
bevel.width,bevel.segments = .003,3
plate.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')

for x in (-.139,.139):
    for y in (-.097,.097):
        cylinder('Mount_Bolt_'+str(x)+'_'+str(y),(x,y,.0135),.0042,.003,hardware_mat,vertices=12,bevel=.0003)

lathe('Fixed_Socket_Ring',[(.013,.012),(.019,.012),(.019,.014),(.017,.015),(.013,.015)],hardware_mat,
      center=(.091,-.048),segments=64)
cylinder('Socket_Dark_Floor',(.091,-.048,.001),.0127,.0015,shadow_mat,bevel=0)

# Fixed pedestal and shaft. The shaft enters the hub bore with radial clearance.
center = (-.025,.01)
lathe('Fixed_Pedestal',[(.018,.012),(.046,.012),(.046,.017),(.039,.022),
                         (.029,.031),(.026,.038),(.026,.053),(.018,.053)],body_mat,center=center)
cylinder('Fixed_Shaft',(*center,.056),.0165,.028,hardware_mat,bevel=.0005)
lathe('Fixed_Shaft_Collar',[(.017,.051),(.029,.051),(.030,.054),(.028,.058),(.017,.058)],body_mat,center=center)

# All moving meshes use local coordinates under the one rotation node.
bpy.ops.mesh.primitive_torus_add(major_segments=96,minor_segments=12,major_radius=.145,
                                minor_radius=.009,location=(0,0,.008))
rim = bpy.context.object
rim.scale.z = .78
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
finish(rim,'Moving_Handwheel_Rim',wheel_mat,wheel_pivot)
for polygon in rim.data.polygons:
    polygon.use_smooth = True

lathe('Moving_Hub',[(.0175,-.007),(.027,-.007),(.030,-.003),(.030,.003),
                    (.026,.007),(.0175,.007)],wheel_mat,wheel_pivot)

# The photo hides the spokes; this first study uses a provisional three-spoke layout.
for spoke_index in range(3):
    verts = []
    for i in range(5):
        t = i/4
        radius = .025+(.145-.025)*t
        theta = spoke_index*2*math.pi/3+math.radians(4)*t
        width = .013+.004*t
        z = -.001+.009*t
        for sideways,height in ((-1,-1),(1,-1),(1,1),(-1,1)):
            verts.append((radius*math.cos(theta)-sideways*width/2*math.sin(theta),
                          radius*math.sin(theta)+sideways*width/2*math.cos(theta),z+height*.003))
    faces = [(3,2,1,0),(16,17,18,19)]
    for i in range(4):
        for j in range(4):
            faces.append((i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j))
    mesh = bpy.data.meshes.new('Spoke_'+str(spoke_index))
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj = bpy.data.objects.new('Moving_Spoke_'+str(spoke_index+1),mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,obj.name,wheel_mat,wheel_pivot,.001)

cylinder('Moving_Center_Cap',(0,0,.008),.017,.003,wheel_mat,wheel_pivot,bevel=.0005)
cylinder('Moving_Retainer',(0,0,.010),.005,.002,hardware_mat,wheel_pivot,vertices=12,bevel=.0003)

bpy.context.view_layer.update()
depsgraph = bpy.context.evaluated_depsgraph_get()
report = {'model':'controller_handwheel_blockout','dimensions_estimated':True,
          'spokes_provisional':True,'parts':[],'triangles':0}
for obj in parts:
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    count = len(mesh.loop_triangles)
    report['parts'].append({'name':obj.name,'triangles':count,'parent':obj.parent.name})
    report['triangles'] += count
    evaluated.to_mesh_clear()
assert report['triangles'] < 20000
(OUT/'blockout_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,wheel_pivot,*parts]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/controller_handwheel_blockout.glb'),
                         export_format='GLB',use_selection=True,export_apply=True,export_extras=True)

desk = cube('Preview_Desk',(0,0,-.02),(1.5,1.5,.04),material('Preview_Wood',(.075,.038,.022),.65),bevel=0)
desk.parent = None
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Blockout_Studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.18,.18,.18,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .4
def point_at(obj,target=(0,0,.055)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for location,energy,size in [((-.3,-.4,.7),12,.6),((.4,.3,.6),8,.6)]:
    bpy.ops.object.light_add(type='AREA',location=location)
    lamp = bpy.context.object
    lamp.data.energy,lamp.data.size = energy,size
    point_at(lamp)
bpy.ops.object.camera_add(location=(.36,-.64,.29))
camera = bpy.context.object
point_at(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = .53
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x,scene.render.resolution_y = 1400,900
scene.render.resolution_percentage = 100
scene.render.filepath = str(OUT/'controller_gray_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'controller_handwheel_blockout.blend'))
bpy.ops.render.render(write_still=True)
camera.location = (0,0,.75)
camera.rotation_euler = (0,0,0)
camera.data.ortho_scale = .43
scene.render.resolution_x,scene.render.resolution_y = 1100,1000
scene.render.filepath = str(OUT/'controller_gray_top.png')
bpy.ops.render.render(write_still=True)
print('BLOCKOUT_TRIANGLES',report['triangles'])
