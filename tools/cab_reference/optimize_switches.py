"""Build a ten-key photograph reference study; preserve previous candidate assets."""
import bpy
import math
from pathlib import Path
from mathutils import Vector
import json
import sys

sys.path.insert(0,str(Path(__file__).resolve().parent))
from switch_materials import configure_materials, ensure_uvs

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'tools/cab_reference/switches_review'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    return mat


ivory = material('Warm_Ivory_Phenolic', (.66, .57, .30), 0, .24)
ivory.node_tree.nodes.get('Principled BSDF').inputs['Coat Weight'].default_value = .12
ivory.node_tree.nodes.get('Principled BSDF').inputs['Coat Roughness'].default_value = .28
black = material('Black_Painted_Steel', (.009, .011, .009), .25, .23)
edge = material('Worn_Edge_Steel', (.20, .22, .21), .82, .34)
dark = material('Recess_Shadow', (.003, .003, .002), 0, .85)
wood = material('Preview_Desk_Not_Exported', (.065, .018, .009), 0, .39)
configure_materials(ivory,black,edge,ROOT)

KEY_COUNT = 10
root = bpy.data.objects.new('PianoSwitches_10Gang', None)
bpy.context.collection.objects.link(root)
root['reference'] = 'tools/cab_reference/crop_piano_keys.jpg'
root['dimensions_are_estimated'] = True
asset = []


def cube(name, loc, dims, mat, bevel=.00025, parent=root):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Small_Machined_Radius', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new('Weighted_Corner_Normals', 'WEIGHTED_NORMAL')
    obj.parent = parent
    if parent:
        asset.append(obj)
    return obj


# Dimensions estimated from a perspective photograph, not a manufacturing drawing.
key_w, key_l, gap = .0064, .0255, .00035
KEY_TOP, KEY_BOTTOM = .0065, -.0042
TOP_INSET = .0025
PIVOT_Z = -.0033
ROCKER_ANGLE = 22.0
rim_z = .0012
rim_bottom, rim_top = rim_z-.00075, rim_z+.00075
angle = math.radians(ROCKER_ANGLE)
upper_y = (key_l/2-TOP_INSET)*math.cos(angle)-KEY_TOP*math.sin(angle)
upper_z = PIVOT_Z+(key_l/2-TOP_INSET)*math.sin(angle)+KEY_TOP*math.cos(angle)
lower_y = key_l/2*math.cos(angle)-KEY_BOTTOM*math.sin(angle)
lower_z = PIVOT_Z+key_l/2*math.sin(angle)+KEY_BOTTOM*math.cos(angle)
def inner_edge_at(z):
    return lower_y+(upper_y-lower_y)*(z-lower_z)/(upper_z-lower_z)+.00030

bank_w = KEY_COUNT * key_w + (KEY_COUNT - 1) * gap
hole_w, hole_l = bank_w + .0018, 2*inner_edge_at(rim_top)
cavity_l = key_l + .003
outer_w, outer_l = hole_w + .007, max(hole_l + .006,cavity_l + .0012)
cube('Recess_Floor', (0, 0, -.014), (hole_w, cavity_l, .0015), dark, 0)
for sign in (-1, 1):
    cube('Cavity_Long_' + str(sign), (0,sign*(cavity_l/2-.0002),-.0075), (hole_w,.0004,.014), dark, 0)
    cube('Cavity_End_' + str(sign), (sign*(hole_w/2-.0002),0,-.0075), (.0004,cavity_l,.014), dark, 0)

def frame_rail(sign):
    # Underside clearance follows the tilted block; the visible top lip stays thin.
    yz = [(inner_edge_at(rim_bottom),rim_bottom),(outer_l/2,rim_bottom),
          (outer_l/2,rim_top),(inner_edge_at(rim_top),rim_top)]
    yz = [(sign*y,z) for y,z in yz]
    if sign < 0:
        yz.reverse()
    vertices = [(x,y,z) for x in (-outer_w/2,outer_w/2) for y,z in yz]
    polygons = [(3,2,1,0),(4,5,6,7)]
    polygons += [(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)]
    data = bpy.data.meshes.new('Relieved_Bezel_Rail')
    data.from_pydata(vertices,[],polygons)
    data.update()
    data.materials.append(black)
    obj = bpy.data.objects.new('Frame_Long_'+str(sign),data)
    bpy.context.collection.objects.link(obj)
    obj.parent = root
    bevel = obj.modifiers.new('Bezel_Edge_Radius','BEVEL')
    bevel.width = .00018
    bevel.segments = 3
    obj.modifiers.new('Weighted_Normals','WEIGHTED_NORMAL')
    asset.append(obj)

for sign in (-1, 1):
    frame_rail(sign)
    cube('Frame_End_' + str(sign), (sign*(hole_w/2+.00175), 0, rim_z), (.0035, hole_l, .0015), black)
    cube('Subtle_Edge_' + str(sign), (0, sign*(outer_l/2-.0001), .00065), (outer_w-.0005, .00018, .0002), edge, .00003)

# Rounded block body. Its exposed wedge is produced by tilt and panel occlusion,
# not by turning the entire physical key into a triangular prism.
# Keep a solid block; independently draft its end faces by insetting the top.
profile = [(-key_l/2,KEY_BOTTOM),(-key_l/2+TOP_INSET,KEY_TOP),
           (key_l/2-TOP_INSET,KEY_TOP),(key_l/2,KEY_BOTTOM)]
profile.reverse()  # Counterclockwise cross-section gives outward prism normals.
root['body_shape'] = 'solid block with inset top ends and narrow edge bevels'
root['nominal_key_body_m'] = [key_w,key_l,KEY_TOP-KEY_BOTTOM]
verts = [(x, y, z) for x in (-key_w/2, key_w/2) for y,z in profile]
n = len(profile)
faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
mesh = bpy.data.meshes.new('Rounded_Cuboid_Shared')
mesh.from_pydata(verts, [], faces)
mesh.update()
mesh.calc_loop_triangles()
signed_volume = sum(mesh.vertices[t.vertices[0]].co.dot(
    mesh.vertices[t.vertices[1]].co.cross(mesh.vertices[t.vertices[2]].co))
    for t in mesh.loop_triangles)/6
assert signed_volume > 0, 'Solid key body must have outward face winding'
mesh.materials.append(ivory)
for i in range(KEY_COUNT):
    pivot = bpy.data.objects.new(f'KeyPivot_{i+1:02d}', None)
    bpy.context.collection.objects.link(pivot)
    pivot.parent = root
    pivot.location = ((i-(KEY_COUNT-1)/2)*(key_w+gap), 0, PIVOT_Z)
    pivot.rotation_euler.x = math.radians(ROCKER_ANGLE if i < 2 else -ROCKER_ANGLE)
    pivot['on_angle_degrees'] = ROCKER_ANGLE
    pivot['off_angle_degrees'] = -ROCKER_ANGLE
    pivot['rotation_axis'] = 'local X in Blender; converted by glTF export'
    key = bpy.data.objects.new(f'Rocker_Key_{i+1:02d}', mesh)
    bpy.context.collection.objects.link(key)
    key.parent = pivot
    bevel = key.modifiers.new('Moulded_Edge_Radius', 'BEVEL')
    bevel.width = .00030
    bevel.segments = 5
    key.modifiers.new('Weighted_Normals', 'WEIGHTED_NORMAL')
    asset.extend([pivot, key])

bpy.context.view_layer.update()
alignment = []
for obj in asset:
    if obj.name.startswith('Rocker_Key_'):
        pivot = obj.parent
        points = [obj.matrix_world @ v.co for v in mesh.vertices]
        alignment.append({'key': obj.name, 'front_y': min(p.y for p in points),
                          'rear_y': max(p.y for p in points), 'min_z': min(p.z for p in points),
                          'angle_deg': math.degrees(pivot.rotation_euler.x)})
        for sign in (-1,1):
            bottom = obj.matrix_world @ Vector((0,sign*key_l/2,KEY_BOTTOM))
            top = obj.matrix_world @ Vector((0,sign*(key_l/2-TOP_INSET),KEY_TOP))
            assert bottom.z < rim_z-.00075, 'Solid end wall must extend beneath rim'
            if sign*pivot.rotation_euler.x > 0:
                assert top.z > rim_z+.00075, 'Raised cuboid edge must remain visible'
            else:
                assert top.z < rim_z-.00075-.00015, 'Low cuboid edge must clear the underside of the frame'
        assert max(abs(p.y) for p in points) < outer_l/2, 'Solid key body must remain inside frame footprint'
for group in (alignment[:2],alignment[2:]):
    for field in ('front_y','rear_y','min_z'):
        assert max(v[field] for v in group)-min(v[field] for v in group) < 1e-7
# The lower body is intentionally concealed. Verify the physical footprint and
# exposed wedge separately; a submerged end is not a physically shortened key.
assert max(v['rear_y']-v['front_y'] for v in alignment) < cavity_l
(OUT/'alignment_check.json').write_text(json.dumps(alignment, indent=2), encoding='utf-8')

# Clip against the actual convex rail solids, with expanded planes for clearance.
def clipped_triangle_in_planes(triangle, planes):
    polygon = list(triangle)
    for normal, bound in planes:
            if not polygon:
                return False
            clipped = []
            previous = polygon[-1]
            previous_distance = normal.dot(previous)-bound
            for current in polygon:
                distance = normal.dot(current)-bound
                if (distance >= 0) != (previous_distance >= 0):
                    clipped.append(previous+(current-previous)*(previous_distance/(previous_distance-distance)))
                if distance >= 0:
                    clipped.append(current)
                previous, previous_distance = current, distance
            polygon = clipped
    return len(polygon) >= 3

padding = .00015
rail_solids = []
for rail in [o for o in asset if o.name.startswith(('Frame_Long_','Frame_End_'))]:
    planes = []
    normal_matrix = rail.matrix_world.to_3x3().inverted().transposed()
    for polygon in rail.data.polygons:
        inward = -(normal_matrix @ polygon.normal).normalized()
        point = rail.matrix_world @ rail.data.vertices[polygon.vertices[0]].co
        planes.append((inward,inward.dot(point)-padding))
    rail_solids.append((rail.name,planes))
depsgraph = bpy.context.evaluated_depsgraph_get()
collisions = []
for key in [o for o in asset if o.name.startswith('Rocker_Key_')]:
    evaluated = key.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    evaluated_mesh.calc_loop_triangles()
    world_vertices = [evaluated.matrix_world @ v.co for v in evaluated_mesh.vertices]
    for rail_name, planes in rail_solids:
        if any(clipped_triangle_in_planes([world_vertices[i] for i in tri.vertices],planes)
               for tri in evaluated_mesh.loop_triangles):
            collisions.append([key.name,rail_name])
    evaluated.to_mesh_clear()
(OUT/'frame_clearance_check.json').write_text(json.dumps({'method':'evaluated key triangles vs padded convex frame solids','padding_m':padding,'pose_angles_deg':[-ROCKER_ANGLE,ROCKER_ANGLE],
                                                       'collisions':collisions},indent=2),encoding='utf-8')
assert not collisions, 'Key/bezel interference: '+repr(collisions)

# Recessed screw heads, restrained edge wear; no display desk in exported asset.
for xsign in (-1, 1):
    for ysign in (-1, 1):
        x, y = xsign*(outer_w/2-.0019), ysign*(outer_l/2-.0016)
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=.00078, depth=.00028, location=(x,y,.00206))
        screw = bpy.context.object
        screw.name = f'Screw_{xsign}_{ysign}'
        screw.parent = root
        screw.data.materials.append(edge)
        asset.append(screw)
        cube(f'Screw_Slot_{xsign}_{ysign}', (x,y,.00222), (.0011,.00015,.00004), dark, 0)

ensure_uvs(asset)
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for obj in asset:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
output = ROOT / 'Assets/Models/Cab/piano_switches_10gang_optimized.glb'
bpy.ops.export_scene.gltf(filepath=str(output), export_format='GLB', use_selection=True, export_apply=True, export_extras=True)

# Identical presentation setup for original and revised models.
def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


# Four strips leave a real mounting aperture; the recessed housing stays below desk level.
for sign in (-1, 1):
    cube('Desk_Front_Back_' + str(sign), (0,sign*(.075+cavity_l/2)/2,-.004),
         (.22,.075-cavity_l/2,.008), wood, 0, parent=None)
    cube('Desk_Left_Right_' + str(sign), (sign*(.11+hole_w/2)/2,0,-.004),
         (.11-hole_w/2,cavity_l,.008), wood, 0, parent=None)
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.18,.18,.18,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .4
for loc, energy, size in [((-.06,-.07,.12), .65,.10),((.06,.08,.12),.8,.09)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    lamp=bpy.context.object
    lamp.data.energy=energy
    lamp.data.shape='DISK'
    lamp.data.size=size
    point_at(lamp,(0,0,0))
bpy.ops.object.camera_add(location=(.067,-.14,.075))
cam=bpy.context.object
point_at(cam,(0,0,.002))
cam.data.type='ORTHO'
cam.data.ortho_scale=.123
scene.camera=cam
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.resolution_x=1200
scene.render.resolution_y=760
scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.view_settings.exposure=-.6
scene.render.filepath=str(OUT/'optimized_10gang.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'piano_switches_10gang_optimized.blend'))
bpy.ops.render.render(write_still=True)
# Orthographic front verification: every right-hand nose has one common baseline.
cam.location = (0,-.16,.022)
point_at(cam,(0,0,.002))
scene.render.resolution_x=1200
scene.render.resolution_y=420
scene.render.filepath=str(OUT/'alignment_front.png')
bpy.ops.render.render(write_still=True)
cam.location = (0,0,.20)
cam.rotation_euler = (0,0,0)
scene.render.resolution_x=1200
scene.render.resolution_y=760
scene.render.filepath=str(OUT/'alignment_top.png')
bpy.ops.render.render(write_still=True)
cam.location = (.16,-.008,.023)
point_at(cam,(0,0,.002))
scene.render.resolution_x=1200
scene.render.resolution_y=500
scene.render.filepath=str(OUT/'alignment_side.png')
bpy.ops.render.render(write_still=True)
print('EXPORTED', output)
