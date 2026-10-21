"""Final art candidates: shoulder-supported square drive and coherent old-metal finish."""
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

def cylinder(name,r,h,z,parent,mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=h,location=(0,0,z))
    o = bpy.context.object
    o.name,o.parent = name,parent
    o.data.materials.append(mat)
    return o

def box(name,w,h,z,parent,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,z))
    o = bpy.context.object
    o.name,o.parent = name,parent
    o.dimensions = (w,w,h)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    return o

def boolean(obj,cutter,operation='DIFFERENCE'):
    bpy.context.view_layer.update()
    bpy.context.view_layer.objects.active = obj
    m = obj.modifiers.new('Mating_Surface','BOOLEAN')
    m.operation,m.solver,m.object = operation,'EXACT',cutter
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def bevel(o):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(o.data)
    bm.free()
    m = o.modifiers.new('Machining_Radius','BEVEL')
    m.width,m.segments = .00035,3
    o.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')

reports = []
for variant in ('without_handles','with_handles'):
    bpy.ops.wm.open_mainfile(filepath=str(OUT/f'brake_reference_{variant}.blend'))
    root = bpy.data.objects['Brake_Valve_Photo_Study']
    root['art_status'] = 'final'
    root['mechanical_design'] = 'shoulder-supported square shaft and mating square hub, axial retention'
    root['historical_or_load_validation'] = False
    mat = bpy.data.materials['Reference_MachinedSilver']
    for label,r,w in [('Tall',.016,.019),('Short',.013,.016)]:
        pivot = bpy.data.objects[label+'_Spindle_Pivot']
        old = bpy.data.objects[label+'_Exposed_Spindle']
        top = max(v.co.z for v in old.data.vertices)+old.location.z
        bottom = min(v.co.z for v in old.data.vertices)+old.location.z
        bpy.data.objects.remove(old,do_unlink=True)
        # One joined shaft: lower round journal, upper broad square drive, axial blind bore.
        shaft = cylinder(label+'_Exposed_Spindle',r,top-.013-bottom,(bottom+top-.013)/2,pivot,mat)
        tongue = box('Square_Drive',w,.015,top-.0065,pivot,mat)
        boolean(shaft,tongue,'UNION')
        bore = cylinder('Retention_Blind_Bore',.0022,.014,top-.004,pivot,mat)
        boolean(shaft,bore)
        bevel(shaft)
        # A shoulder washer carries axial seating load independently of the screw.
        washer = cylinder(label+'_Shoulder_Washer',r+.004,.001,top-.0125,pivot,mat)
        cutter = box('Washer_Clearance',w+.0006,.004,top-.0125,pivot,mat)
        boolean(washer,cutter)
        bevel(washer)
        if variant=='with_handles':
            for o in list(bpy.data.objects):
                if o.name.startswith(label+'_Drive_Dog_') or o.name==label+'_Removable_Hub':
                    bpy.data.objects.remove(o,do_unlink=True)
            hub = cylinder(label+'_Removable_Hub',r+.006,.017,top-.0035,pivot,mat)
            cutter = box('Square_Mating_Bore',w+.0004,.024,top-.010,pivot,mat)
            boolean(hub,cutter)
            bore = cylinder('Screw_Clearance',.0023,.026,top,pivot,mat)
            boolean(hub,bore)
            bevel(hub)
            washer = cylinder(label+'_Retention_Washer',.005,.0015,top+.00575,pivot,mat)
            bore = cylinder('Washer_Hole',.0023,.004,top+.00575,pivot,mat)
            boolean(washer,bore)
            bevel(washer)
        pivot['square_bore_side_clearance_m'] = .0002
        pivot['initial_pose_is_not_a_verified_running_detent'] = True

    # Slightly warmer, darker casting hue; keep the original normal and roughness maps.
    for name,gain in [('Reference_CastSilver',(.88,.89,.84)),('Reference_OldSteel',(.91,.92,.88))]:
        m = bpy.data.materials[name]
        nodes,links = m.node_tree.nodes,m.node_tree.links
        bs = nodes['Principled BSDF']
        source = bs.inputs['Base Color'].links[0].from_socket
        tint = nodes.new('ShaderNodeMixRGB')
        tint.blend_type = 'MULTIPLY'
        tint.inputs[0].default_value = 1
        tint.inputs[2].default_value = (*gain,1)
        links.new(source,tint.inputs[1])
        links.new(tint.outputs[0],bs.inputs['Base Color'])
    # Bake the constant tint directly into image pixels for portable glTF export.
    for material_name,gain in [('Reference_CastSilver',(.88,.89,.84)),('Reference_OldSteel',(.91,.92,.88))]:
        m = bpy.data.materials[material_name]
        bs = m.node_tree.nodes['Principled BSDF']
        tint = bs.inputs['Base Color'].links[0].from_node
        tex = tint.inputs[1].links[0].from_node
        img = tex.image.copy()
        img.name += '_FinalTint'
        px = list(img.pixels)
        for i in range(0,len(px),4):
            for j in range(3):
                px[i+j] *= gain[j]
        img.pixels.foreach_set(px)
        img.pack()
        tex.image = img
        m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
        m.node_tree.nodes.remove(tint)
    parts = [o for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Preview_')]
    ensure_uvs(parts)
    bpy.context.view_layer.update()
    report = {'variant':variant,'art_status':'final','historical_or_load_validation':False,
              'meshes':len(parts),'triangles':0}
    for o in parts:
        e = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = e.to_mesh()
        mesh.calc_loop_triangles()
        report['triangles'] += len(mesh.loop_triangles)
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
        e.to_mesh_clear()
    assert report['triangles']<35000
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*parts,bpy.data.objects['Tall_Spindle_Pivot'],bpy.data.objects['Short_Spindle_Pivot']]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(ROOT/f'Assets/Models/Cab/brake_valves_final_{variant}.glb'),
                             export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
    scene = bpy.context.scene
    scene.render.filepath = str(OUT/f'brake_final_{variant}.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'brake_final_{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    reports.append(report)
(OUT/'brake_final_audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print(json.dumps(reports))
