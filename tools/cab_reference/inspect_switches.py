import bpy
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root / 'Assets/Models/Cab/piano_switches_8gang.glb'))
result = []
for obj in bpy.context.scene.objects:
    result.append({
        'name': obj.name,
        'type': obj.type,
        'location': list(obj.location),
        'dimensions': list(obj.dimensions),
        'rotation': list(obj.rotation_euler),
        'materials': [m.name for m in obj.data.materials] if obj.type == 'MESH' else [],
    })
out = root / 'tools/cab_reference/switches_inspection.json'
out.write_text(json.dumps(result, indent=2), encoding='utf-8')
print('INSPECTED', out)
