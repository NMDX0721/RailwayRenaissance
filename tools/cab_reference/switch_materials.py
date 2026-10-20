"""Deterministic, embedded PBR maps for the cab switch study; no geometry edits."""
import math
import random
from pathlib import Path
import bpy


def srgb(value):
    value = max(0, min(1, value))
    return 12.92*value if value <= .0031308 else 1.055*value**(1/2.4)-.055


def image_map(name, pixels, size, directory, color=False):
    image = bpy.data.images.new(name, width=size, height=size, alpha=False)
    image.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(directory/(name+'.png'))
    image.file_format = 'PNG'
    image.save()
    image.pack()
    return image


def finish_material(mat, label, base, roughness, metallic, coat, grain_strength, seed, directory):
    size = 512
    rng = random.Random(seed)
    grain = [rng.uniform(-1,1) for _ in range(size*size)]
    colors, rough, normals = [], [], []
    for y in range(size):
        v = (y+.5)/size
        for x in range(size):
            u = (x+.5)/size
            index = y*size+x
            g = grain[index]
            cloud = .55*math.sin(u*13+math.sin(v*11))*math.sin(v*9+.4)+.12*math.sin(u*37+v*19)
            border = ((2*u-1)**6+(2*v-1)**6)/2
            touch = math.exp(-((u-.5)**2+(v-.5)**2)/.075)
            gain = 1+cloud*(.012 if label == 'Ivory' else .12)+g*.003
            linear = [c*gain for c in base]
            if label == 'Ivory':
                linear[2] *= 1-.018*border
                r = roughness+.045*border-.035*touch+.025*cloud+.012*g
            else:
                r = roughness+.045*cloud+.018*g
            colors.extend([*(srgb(c) for c in linear),1])
            r = max(.12,min(.65,r))
            rough.extend([r,r,r,1])
            nx = (grain[y*size+(x-1)%size]-grain[y*size+(x+1)%size])*grain_strength
            ny = (grain[((y-1)%size)*size+x]-grain[((y+1)%size)*size+x])*grain_strength
            nz = math.sqrt(max(.001,1-nx*nx-ny*ny))
            normals.extend([nx*.5+.5,ny*.5+.5,nz*.5+.5,1])
    base_image = image_map(label+'_BaseColor',colors,size,directory,True)
    rough_image = image_map(label+'_Roughness',rough,size,directory)
    normal_image = image_map(label+'_Normal',normals,size,directory)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['IOR'].default_value = 1.49
    bsdf.inputs['Specular IOR Level'].default_value = .45
    bsdf.inputs['Coat Weight'].default_value = coat
    bsdf.inputs['Coat Roughness'].default_value = .22
    mat.diffuse_color = (*base,1)
    for img, socket, location in ((base_image,'Base Color',(-600,250)),(rough_image,'Roughness',(-600,0))):
        node = nodes.new('ShaderNodeTexImage')
        node.image = img
        node.location = location
        links.new(node.outputs['Color'],bsdf.inputs[socket])
    texture = nodes.new('ShaderNodeTexImage')
    texture.image = normal_image
    texture.location = (-600,-250)
    normal = nodes.new('ShaderNodeNormalMap')
    normal.location = (-280,-230)
    links.new(texture.outputs['Color'],normal.inputs['Color'])
    links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])


def configure_materials(ivory, black, steel, root):
    directory = Path(root)/'Assets/Models/Cab/Textures/PianoSwitches'
    directory.mkdir(parents=True,exist_ok=True)
    finish_material(ivory,'Ivory',(.67,.59,.34),.275,0,.18,.012,281,directory)
    finish_material(black,'Paint',(.008,.010,.009),.29,0,.14,.020,673,directory)
    finish_material(steel,'Steel',(.16,.18,.17),.40,.85,0,.015,919,directory)


def ensure_uvs(objects):
    seen = set()
    for obj in objects:
        if obj.type != 'MESH' or obj.data in seen:
            continue
        mesh = obj.data
        seen.add(mesh)
        if mesh.uv_layers:
            continue
        uv = mesh.uv_layers.new(name='UVMap')
        for polygon in mesh.polygons:
            dropped = max(range(3),key=lambda j:abs(polygon.normal[j]))
            axes = [j for j in range(3) if j != dropped]
            points = [mesh.vertices[mesh.loops[i].vertex_index].co for i in polygon.loop_indices]
            low = [min(p[j] for p in points) for j in axes]
            span = [max(p[j] for p in points)-low[k] for k,j in enumerate(axes)]
            for i in polygon.loop_indices:
                point = mesh.vertices[mesh.loops[i].vertex_index].co
                uv.data[i].uv = tuple(.03+.94*(point[j]-low[k])/max(span[k],1e-9) for k,j in enumerate(axes))
        mesh.update()
