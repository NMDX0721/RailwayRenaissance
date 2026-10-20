"""Final photo-led game-art gauges: independent channels, sleeves and range audit."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'tools/cab_reference/pressure_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import finish_material,ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'duplex_air_gauges_study.blend'))
root=bpy.data.objects['Duplex_Air_Pressure_Gauge_Pair']
directory=ROOT/'Assets/Models/Cab/Textures/DuplexPressureFinal';directory.mkdir(parents=True,exist_ok=True)
bezel=bpy.data.materials.new('Pressure_Blackened_Steel_Retainer');bezel.use_nodes=True
finish_material(bezel,'Pressure_BlackenedSteel',(.022,.027,.025),.43,.78,0,.004,5411,directory)
steel=bpy.data.materials['Pressure_AgedSteel'];bs=steel.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value=(.16,.18,.17,1);bs.inputs['Metallic'].default_value=.88
bs.inputs['Roughness'].default_value=.43
glass=bpy.data.materials['Pressure_ClearGlass'];bs=glass.node_tree.nodes['Principled BSDF']
bs.inputs['Roughness'].default_value=.025;bs.inputs['IOR'].default_value=1.5
red=bpy.data.materials['Pressure_RedNeedle'];bs=red.node_tree.nodes['Principled BSDF']
bs.inputs['Base Color'].default_value=(.23,.014,.008,1);bs.inputs['Roughness'].default_value=.56
ink=bpy.data.materials['Pressure_BlackInk'];ink.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.004,.005,.004,1)
legend=ink.copy();legend.name='Pressure_Rear_Legend'
legend.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.56,.55,.44,1)
for node in bpy.data.materials['Pressure_IvoryDial'].node_tree.nodes:
    if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.20
brass=bpy.data.materials['Pressure_BrassSocket'];brass.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.51
font=bpy.data.fonts.load('C:/Windows/Fonts/simsun.ttc')
def annulus(name,inner,outer,front,back,parent,mat,center_z=.074,hex_outer=False):
    vs=[];fs=[];n=64
    for j,(y,r) in enumerate([(front,inner),(front,outer),(back,outer),(back,inner)]):
        for i in range(n):
            a=2*math.pi*i/n
            rr=r
            if hex_outer and j in (1,2):rr=r*math.cos(math.pi/6)/math.cos(a%(math.pi/3)-math.pi/6)
            vs.append((rr*math.sin(a),y,center_z+rr*math.cos(a)))
    for j in range(4):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i))
    m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.parent=parent;o.data.materials.append(mat)
    return o
def cylinder(name,r,depth,loc,parent,mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=depth,location=loc,rotation=(math.pi/2,0,0))
    o=bpy.context.object;o.name=name;o.parent=parent;o.data.materials.append(mat)
    return o
def rear_label(body,x,z,parent,mat,name):
    bpy.ops.object.text_add(location=(x,.0332,z),rotation=(math.pi/2,0,math.pi));o=bpy.context.object
    o.name=name;o.parent=parent;o.data.body=body;o.data.font=font;o.data.size=.0038
    o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.resolution_u=4;o.data.materials.append(mat)
    bpy.ops.object.convert(target='MESH')
report={'status':'final game-art design','historical_identity_verified':False,
 'range_kPa':[1600,1000],'minor_division_kPa':20,'major_division_kPa':200,
 'drive_and_metrology_certified':False,'unity_imported':False,
 'equalizing_reservoir_signal_implemented':False,'needle_samples_per_channel':361,
 'minimum_radial_clearance_m':1,'black_sleeve_to_red_shaft_clearance_m':.0002,
 'glass_to_cap_clearance_m':.0006,'glass_to_inner_spindle_clearance_m':.0005,
 'meshes':0,'triangles':0}
for tag,full,labels in [('MR_BC',1600,('总风缸','制动缸')),('BP_ER',1000,('列车管','均衡风缸'))]:
    group=bpy.data.objects['Gauge_'+tag]
    bpy.data.objects[tag+'_Black_Bezel'].data.materials[0]=bezel
    # Replace a solid common front boss with an annular outer cover around the red spindle.
    bpy.data.objects.remove(bpy.data.objects[tag+'_Concentric_Spindle'],do_unlink=True)
    annulus(tag+'_Outer_Bearing_Cover',.0011,.0032,-.0308,-.0298,group,steel)
    black=bpy.data.objects[tag+'_Black_Needle'];pivot=bpy.data.objects[tag+'_Black_Pivot']
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.0009,depth=.004)
    cutter=bpy.context.object;cutter.parent=pivot;cutter.location=(0,0,0);cutter.rotation_euler=(math.pi/2,0,0)
    bpy.context.view_layer.objects.active=black;mod=black.modifiers.new('Inner_Spindle_Clearance','BOOLEAN')
    mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    annulus(tag+'_Black_Rotating_Sleeve',.0009,.0019,-.00036,.00075,pivot,steel,0)
    rp=bpy.data.objects[tag+'_Red_Pivot']
    cylinder(tag+'_Red_Inner_Spindle',.0007,.0035,(0,-.0003,0),rp,steel)
    for x,color,label in [(-.018,red,labels[0]),(.018,ink,labels[1])]:
        suffix='Red' if x<0 else 'Black'
        bpy.data.objects.remove(bpy.data.objects[tag+'_RearSocket_'+suffix],do_unlink=True)
        socket=annulus(tag+'_RearSocket_'+suffix,.0032,.0068,.0275,.0445,group,brass,hex_outer=True)
        socket.location.x=x
        # Coloured collars identify each independent pressure connection without inventing a thread standard.
        collar=annulus(tag+'_Port_Colour_Collar',.0033,.0064,.0416,.0425,group,color)
        collar.location.x=x
        rear_label(label,x,.093,group,legend,tag+'_Rear_Channel_Label')
    # A short unit legend remains visible away from the near-vertical needles.
    bpy.data.objects[tag+'_Unit'].location.x=-.012
    bpy.data.objects[tag+'_Unit'].location.z=.085
    for color in ('Red','Black'):
        needle=bpy.data.objects[tag+'_'+color+'_Needle'];node=bpy.data.objects[tag+'_'+color+'_Pivot']
        assert node['range_kPa']==full
        for i in range(361):
            a=math.radians(-135+270*i/360)
            for v in needle.data.vertices:
                x,z=v.co.x,v.co.z
                xx=math.cos(a)*x+math.sin(a)*z;zz=-math.sin(a)*x+math.cos(a)*z
                gap=.0495-math.hypot(xx,zz)
                report['minimum_radial_clearance_m']=min(report['minimum_radial_clearance_m'],gap)
                assert gap>.004
    group['pressure_reference']='gauge pressure relative to atmosphere; unit kPa'
    group['pressure_connections']='independent red/black identified rear inputs; no cross-feed model'
    group['reading_rule']='angleY=-135deg+270deg*clamp(pressure_kPa/range_kPa,0,1)'
    group['missing_or_invalid_input']='must be handled by future binding; do not fabricate equalizing reservoir values'
root['asset_status']='final game-art model; not factory-identified or pressure certified'
parts=[o for o in bpy.data.objects if o.type=='MESH'];ensure_uvs(parts);report['meshes']=len(parts)
bpy.context.view_layer.update()
for o in parts:
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();m.calc_loop_triangles()
    report['triangles']+=len(m.loop_triangles);e.to_mesh_clear()
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Assets/Models/Cab/duplex_air_gauges_final.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
(OUT/'duplex_air_gauges_final_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.filepath=str(OUT/'duplex_air_gauges_final_reference.png')
# Narrow strip produces an actual glass reflection; the light is excluded from GLB.
bpy.ops.object.light_add(type='AREA',location=(-.20,-.34,.30));light=bpy.context.object
light.name='Preview_Glass_Reflection';light.data.shape='RECTANGLE';light.data.energy=1.0
light.data.size=.025;light.data.size_y=.26
light.rotation_euler=(Vector((0,0,.08))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.light_add(type='AREA',location=(.04,-.40,.025));fill=bpy.context.object
fill.name='Preview_Lower_Mounting_Fill';fill.data.energy=2.2;fill.data.size=.24
fill.data.specular_factor=0;fill.visible_glossy=False
fill.rotation_euler=(Vector((0,0,.025))-fill.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'duplex_air_gauges_final.blend'))
if '--skip-render' not in sys.argv:bpy.ops.render.render(write_still=True)
print(json.dumps(report))
