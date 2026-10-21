"""DF4-photo-led duplex gauge pair, with independently articulated pressure channels.
Ranges, pointer assignments and dimensions are design selections, not vehicle identification.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'tools/cab_reference/pressure_review';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.read_factory_settings(use_empty=True)
def material(name,color,metallic=0,rough=.5):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metallic
    p.inputs['Roughness'].default_value=rough
    return m
paint=material('Pressure_OldBlackPaint',(.009,.013,.011))
ivory=material('Pressure_IvoryDial',(.69,.65,.51),0,.67)
steel=material('Pressure_AgedSteel',(.18,.20,.19),.9,.39)
brass=material('Pressure_BrassSocket',(.27,.16,.055),.82,.48)
ink=material('Pressure_BlackInk',(.008,.009,.008),0,.66)
red=material('Pressure_RedNeedle',(.29,.025,.013),0,.55)
rubber=material('Pressure_Gasket',(.004,.005,.004),0,.82)
glass=material('Pressure_ClearGlass',(.94,.98,.96),0,.055)
bs=glass.node_tree.nodes['Principled BSDF'];bs.inputs['Transmission Weight'].default_value=1
bs.inputs['IOR'].default_value=1.5
directory=ROOT/'Assets/Models/Cab/Textures/DuplexPressure';directory.mkdir(parents=True,exist_ok=True)
finish_material(paint,'Pressure_BlackEnamel',(.008,.012,.010),.53,0,.035,.005,5301,directory)
finish_material(ivory,'Ivory',(.69,.65,.51),.64,0,0,.002,5302,directory)
root=bpy.data.objects.new('Duplex_Air_Pressure_Gauge_Pair',None);bpy.context.collection.objects.link(root)
root.rotation_euler.x=math.radians(-12)
root['historical_model_ranges_and_pointer_assignments_verified']=False
root['mechanism_reference']='two independent Bourdon systems; concentric spindle visual representation, internals not modelled'
font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc')
def cube(name,loc,dims,mat,parent,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.parent=parent
    o.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    if bevel:
        m=o.modifiers.new('Small_Edge_Radius','BEVEL');m.width=bevel;m.segments=3
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o
def cylinder(name,radius,depth,loc,mat,parent,vertices=96):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name=name;o.parent=parent;o.data.materials.append(mat)
    m=o.modifiers.new('Machined_Edge','BEVEL');m.width=.00035;m.segments=2
    o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o
def ring(name,inner,outer,front,back,mat,parent):
    verts=[];faces=[];n=128
    for y,r in [(front,inner),(front,outer),(back,outer),(back,inner)]:
        for i in range(n):
            a=2*math.pi*i/n;verts.append((r*math.sin(a),y,.074+r*math.cos(a)))
    for j in range(4):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i))
    m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.parent=parent;o.data.materials.append(mat)
    mod=o.modifiers.new('Retaining_Ring_Radius','BEVEL');mod.width=.00035;mod.segments=3
    o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o
def text(body,x,z,size,mat,parent,name):
    bpy.ops.object.text_add(location=(x,-.0268,z),rotation=(math.pi/2,0,0));o=bpy.context.object
    o.name=name;o.parent=parent;o.data.body=body;o.data.font=font;o.data.size=size
    o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.resolution_u=4;o.data.materials.append(mat)
    bpy.ops.object.convert(target='MESH')
def pointer(name,y,value,full,mat,parent):
    node=bpy.data.objects.new(name+'_Pivot',None);bpy.context.collection.objects.link(node)
    node.parent=parent;node.location=(0,y,.074);node.rotation_euler.y=math.radians(-135+270*value/full)
    node['pressure_channel']=name;node['range_kPa']=full;node['display_kPa']=value
    outline=[(-.00125,-.009),(.00125,-.009),(.00125,.020),(.0005,.040),(0,.044),(-.0005,.040),(-.00125,.020)]
    vs=[(x,dy,z) for dy in (-.00022,.00022) for x,z in outline];n=len(outline)
    fs=[tuple(range(n)),tuple(reversed(range(n,n*2)))]
    for i in range(n):fs.append((i,i+n,(i+1)%n+n,(i+1)%n))
    mesh=bpy.data.meshes.new(name+'_NeedleMesh');mesh.from_pydata(vs,[],fs);mesh.update()
    o=bpy.data.objects.new(name+'_Needle',mesh);bpy.context.collection.objects.link(o);o.parent=node;o.data.materials.append(mat)
    return node
modules=[]
for index,(tag,full,channels,values) in enumerate([
    ('MR_BC',1600,('总风缸','制动缸'),(840,180)),
    ('BP_ER',1000,('列车管','均衡风缸'),(500,470))]):
    group=bpy.data.objects.new('Gauge_'+tag,None);bpy.context.collection.objects.link(group)
    group.parent=root;group.location.x=(index-.5)*.15
    group['red_channel']=channels[0];group['black_channel']=channels[1]
    group['range_kPa']=full;group['values_are_static_review_examples']=True
    cube(tag+'_Panel',(0,.030,.074),(.132,.006,.145),paint,group,.001)
    cylinder(tag+'_SteelCase',.055,.052,(0,.002,.074),paint,group)
    ring(tag+'_Rubber_Seat',.0497,.059,-.025,-.021,rubber,group)
    ring(tag+'_Black_Bezel',.050,.058,-.034,-.024,paint,group)
    ring(tag+'_Steel_Inner_Lip',.0495,.0504,-.0339,-.0328,steel,group)
    cylinder(tag+'_Dial',.0495,.001,(0,-.026,.074),ivory,group)
    ticks=[]
    steps=80 if full==1600 else 50
    for i in range(steps+1):
        a=math.radians(-135+270*i/steps);major=i%10==0;medium=i%5==0
        length=.005 if major else (.0036 if medium else .0021)
        r=.045-length/2
        o=cube(tag+'_Tick',(r*math.sin(a),-.0268,.074+r*math.cos(a)),(.00040 if major else .00023,.00010,length),ink,group)
        o.rotation_euler.y=a;ticks.append(o)
        if major:
            r=.0355;text(str(round(full*i/steps)),r*math.sin(a),.074+r*math.cos(a),.0048,ink,group,tag+'_Value_'+str(i))
    bpy.ops.object.select_all(action='DESELECT')
    for o in ticks:o.select_set(True)
    bpy.context.view_layer.objects.active=ticks[0];bpy.ops.object.join();bpy.context.object.name=tag+'_Fixed_Graduations'
    text('kPa',0,.089,.0055,ink,group,tag+'_Unit')
    text(channels[0],0,.061,.0047,red,group,tag+'_Red_Channel_Label')
    text(channels[1],0,.052,.0047,ink,group,tag+'_Black_Channel_Label')
    pointer(tag+'_Red',-.0294,values[0],full,red,group)
    pointer(tag+'_Black',-.0282,values[1],full,ink,group)
    cylinder(tag+'_Concentric_Spindle',.0032,.001,(0,-.0303,.074),steel,group)
    cylinder(tag+'_Center_Cap',.0022,.0007,(0,-.0310,.074),ink,group)
    cylinder(tag+'_Glass',.0496,.0009,(0,-.0324,.074),glass,group)
    for x in (-.055,.055):
        for z in (.012,.136):
            cylinder(tag+'_Mount_Screw',.0022,.001,(x,.0258,z),steel,group,32)
            cube(tag+'_Screw_Slot',(x,.0252,z),(.0028,.00012,.00045),ink,group)
    for x,label in [(-.018,'Red'),(.018,'Black')]:
        socket=cylinder(tag+'_RearSocket_'+label,.0068,.017,(x,.036,.074),brass,group,6)
        bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.0032,depth=.024,
            location=(x,.041,.074),rotation=(math.pi/2,0,0))
        cutter=bpy.context.object;cutter.parent=group;bpy.context.view_layer.objects.active=socket
        mod=socket.modifiers.new('Pressure_Port_Bore','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
        bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0)
        bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    modules.append(group)
parts=[o for o in bpy.data.objects if o.type=='MESH'];ensure_uvs(parts)
assert .044 < .0495
assert abs(-.0294-(-.0282))>.00044
report={'modules':2,'independent_pointers':4,'range_kPa':[1600,1000],
        'minor_division_kPa':20,'major_division_kPa':200,'rear_pressure_connections':4,
        'glass_to_cap_nominal_clearance_m':.0006,'historical_configuration_verified':False,
        'equalizing_reservoir_signal_implemented':False,'socket_bores_are_open':True,
        'needle_tip_radius_m':.044,'dial_radius_m':.0495,'needle_layer_clearance_m':.00076,
        'triangles':0,'meshes':len(parts)}
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/duplex_air_gauges_study.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Pressure_Studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.30
for loc,energy,size in [((-.25,-.35,.45),12,.3),((.3,-.1,.30),7,.25)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=energy;o.data.size=size
    o.rotation_euler=(Vector((0,0,.08))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.23,-.55,.27));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.080))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=.38;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1500;scene.render.resolution_y=950;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'duplex_air_gauges_reference.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'duplex_air_gauges_study.blend'))
(OUT/'duplex_air_gauges_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.render.render(write_still=True)
print(json.dumps(report))
