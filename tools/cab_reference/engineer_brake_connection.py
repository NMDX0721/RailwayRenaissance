"""Designed mating geometry, differentiated handles, and photo-led disc enlargement.
This is a game-asset mechanical proposal, not a certified historical brake design.
"""
import bpy
import bmesh
import math
import sys
import json
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'tools/cab_reference/brake_review'
sys.path.insert(0,str(Path(__file__).parent))
from switch_materials import ensure_uvs
bpy.ops.wm.open_mainfile(filepath=str(OUT/'brake_without_handles.blend'))
root = bpy.data.objects['Brake_Valve_Photo_Study']
steel = bpy.data.materials['Brake_Matte_Steel']
grip = bpy.data.materials.new('Brake_Handle_Black')
grip.use_nodes = True
grip.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.012,.013,.011,1)
grip.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .53

def cube(name,loc,dims,parent,mat=steel,bevel=.0005):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o = bpy.context.object
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,parent,mat,bevel)

def finish(o,name,parent,mat=steel,bevel=.0005):
    o.name,o.parent = name,parent
    o.data.materials.append(mat)
    if bevel:
        mod = o.modifiers.new('Manufacturing_Radius','BEVEL')
        mod.width,mod.segments = bevel,3
        o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    return o

def cylinder(name,loc,r,h,parent,mat=steel,vertices=64):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=h,location=loc)
    o = finish(bpy.context.object,name,parent,mat)
    for p in o.data.polygons:
        p.use_smooth = len(p.vertices)==4
    return o

def boolean(o,cutter):
    bpy.context.view_layer.objects.active = o
    mod = o.modifiers.new('Mating_Cut','BOOLEAN')
    mod.operation,mod.solver,mod.object = 'DIFFERENCE','EXACT',cutter
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

# Enlarge the short valve's control disc, not its entire pedestal.
for name in ('Short_Flange','Short_Cover'):
    o = bpy.data.objects[name]
    o.scale.x *= 1.24
    o.scale.y *= 1.24
for o in list(bpy.data.objects):
    if o.name.startswith('Short_Flange_Bolt_'):
        o.location.x = .030+(o.location.x-.030)*1.24
        o.location.y = -.054+(o.location.y+.054)*1.24

interfaces = []
for label,r,h,angle in [('Tall',.016,.024,55),('Short',.013,.035,105)]:
    pivot = bpy.data.objects[label+'_Spindle_Pivot']
    old = bpy.data.objects[label+'_Exposed_Spindle']
    top = max(v.co.z for v in old.data.vertices)+old.location.z
    bpy.data.objects.remove(old,do_unlink=True)
    shaft = cylinder(label+'_Exposed_Spindle',(0,0,top-h/2),r,h,pivot)
    # Diametral drive seat; its sidewalls transfer torque to two matching drive dogs.
    seat_length = .022 if label=='Tall' else .018
    cutter = cube('Seat_Cutter',(0,0,top+.001),(seat_length,.006,.011),pivot,bevel=0)
    bpy.context.view_layer.update()
    boolean(shaft,cutter)
    # Blind axial retention bore. Thread engagement is schematic, not modeled helix.
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.0022,depth=.014,
        location=pivot.matrix_world@Vector((0,0,top-.003)))
    boolean(shaft,bpy.context.object)
    shaft['connection_design'] = 'diametral drive dogs plus axial retaining screw'
    pivot.rotation_euler.z = math.radians(angle)
    pivot['initial_pose'] = 'operator-facing layout proposal, not verified running detent'
    interfaces.append({'label':label,'top':top,'radius':r,'seat_length':seat_length,
                       'initial_angle_deg':angle,'side_clearance_m':.00025,
                       'bottom_clearance_m':.0005,'radial_clearance_m':.0003})

def export(tag):
    parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
    ensure_uvs(parts)
    bpy.context.view_layer.update()
    tris = 0
    for o in parts:
        e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        m = e.to_mesh()
        m.calc_loop_triangles()
        tris += len(m.loop_triangles)
        assert all(math.isfinite(c) for v in m.vertices for c in v.co)
        e.to_mesh_clear()
    assert tris<35000
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(ROOT/f'Assets/Models/Cab/brake_valves_{tag}.glb'),
                             export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
    return {'variant':tag,'meshes':len(parts),'triangles':tris}

scene = bpy.context.scene
scene.camera.data.ortho_scale = .36
scene.render.filepath = str(OUT/'brake_connection_without_handles.png')
reports = [export('connection_without_handles')]
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_connection_without_handles.blend'))
bpy.ops.render.render(write_still=True)

for spec in interfaces:
    label,top,r = spec['label'],spec['top'],spec['radius']
    pivot = bpy.data.objects[label+'_Spindle_Pivot']
    # Parent coordinates are local: the initial pose is applied once by the pivot.
    hub = cylinder(label+'_Removable_Hub',(0,0,top-.004),r+.006,.018,pivot)
    bpy.context.view_layer.update()
    bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=r+.0003,depth=.024,
        location=pivot.matrix_world@Vector((0,0,top-.009)))
    boolean(hub,bpy.context.object)
    # Interior roof is at top+.003. Dogs run from roof to top-.0045.
    half_length = (spec['seat_length']-.006)/2-.00025
    for sign in (-1,1):
        cube(label+'_Drive_Dog_'+str(sign),(sign*(.003+half_length/2),0,top-.0005),
             (half_length,.0055,.007),pivot,bevel=.00015)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.0023,depth=.025,
        location=pivot.matrix_world@Vector((0,0,top)))
    boolean(hub,bpy.context.object)
    cylinder(label+'_Retention_Screw_Shank',(0,0,top-.001),.002,.016,pivot,vertices=24)
    cylinder(label+'_Retention_Screw_Head',(0,0,top+.008),.004,.003,pivot,vertices=6)
    if label=='Tall':
        # Longer tapered dogleg arm and barrel grip for a distinct main control.
        verts = []
        for x,y,z,w in [(0,0,top+.001,.018),(-.035,-.006,top+.007,.018),(-.10,-.013,top+.009,.012)]:
            for yy,zz in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                verts.append((x,y+yy*w/2,z+zz*.004))
        faces = [(3,2,1,0),(8,9,10,11)]
        for j in range(2):
            for i in range(4):
                faces.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
        mesh = bpy.data.meshes.new('Main_Arm_Mesh')
        mesh.from_pydata(verts,[],faces)
        mesh.update()
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(mesh)
        bm.free()
        o = bpy.data.objects.new('Tall_Dogleg_Arm',mesh)
        bpy.context.collection.objects.link(o)
        finish(o,o.name,pivot)
        loc,scale = (-.119,-.013,top+.009),(.032,.011,.010)
    else:
        cube('Short_Compact_Arm',(-.031,0,top+.001),(.074,.017,.010),pivot)
        cylinder('Short_Grip_Neck',(-.066,0,top+.009),.006,.019,pivot)
        loc,scale = (-.066,0,top+.024),(.020,.015,.012)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=12,location=loc)
    o = bpy.context.object
    o.scale = scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(o,label+'_Distinct_Grip',pivot,grip)
    for p in o.data.polygons:
        p.use_smooth = True

reports.append(export('connection_with_handles'))
(OUT/'brake_connection_audit.json').write_text(json.dumps({'variants':reports,'interfaces':interfaces,
    'historical_interface_verified':False,'load_or_sweep_tested':False},indent=2),encoding='utf-8')
scene.camera.data.ortho_scale = .40
scene.camera.rotation_euler = (Vector((0,-.035,.09))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = str(OUT/'brake_connection_with_handles.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'brake_connection_with_handles.blend'))
bpy.ops.render.render(write_still=True)
print(json.dumps(reports))
