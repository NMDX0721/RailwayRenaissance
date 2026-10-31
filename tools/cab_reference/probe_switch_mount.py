import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Unity Project/RailwayRenaissance/tools/cab_reference/switches_review/piano_switches_10gang_optimized.blend')
for o in bpy.data.objects:
 print('SWITCH_OBJECT',o.name,o.type,tuple(o.dimensions),o.parent.name if o.parent else '',tuple(o.scale))
