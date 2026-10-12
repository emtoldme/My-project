import bpy
import bmesh
import math
from mathutils import Vector
from pathlib import Path

blend_path = r"G:\图书馆\Old_Sonic_Modulator_AssetKit\Old_Sonic_Modulator.blend"
bpy.ops.wm.open_mainfile(filepath=blend_path)

kit_root = Path(r"G:\图书馆\Old_Sonic_Modulator_AssetKit")

# 1. 调低绿色外壳的法线凹凸强度 (从 0.55 降至 0.18，减轻石质凹凸感，呈现平整金属旧漆质感)
mat_olive = bpy.data.materials.get("WPN_Painted_Olive")
if mat_olive and mat_olive.node_tree:
    for n in mat_olive.node_tree.nodes:
        if n.type == 'NORMAL_MAP':
            n.inputs["Strength"].default_value = 0.18
            print("Updated WPN_Painted_Olive normal strength to 0.18")

# 2. 移除旧的粗略电子管物体（保留底座或重新按精确规范构建）
old_tube_objs = [
    "WPN_TubeBase_1", "WPN_TubeBase_2",
    "WPN_TubeFilament_1", "WPN_TubeFilament_2",
    "WPN_VacuumTube_1", "WPN_VacuumTube_2"
]
for name in old_tube_objs:
    obj = bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)

# 3. 创建/更新玻璃材质 MAT_Vacuum_Glass
mat_glass = bpy.data.materials.get("MAT_Vacuum_Glass") or bpy.data.materials.new("MAT_Vacuum_Glass")
mat_glass.use_nodes = True
nodes = mat_glass.node_tree.nodes
links = mat_glass.node_tree.links
nodes.clear()
bsdf = nodes.new("ShaderNodeBsdfPrincipled")
out = nodes.new("ShaderNodeOutputMaterial")
links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

# 按照教程参数：Base Color 浅灰色, Metallic 0, Roughness 0.08, Transmission Weight 1.0, IOR 1.45
bsdf.inputs["Base Color"].default_value = (0.85, 0.88, 0.92, 1.0)
bsdf.inputs["Metallic"].default_value = 0.0
bsdf.inputs["Roughness"].default_value = 0.08
bsdf.inputs["IOR"].default_value = 1.45
tw = bsdf.inputs.get("Transmission Weight")
if tw is None:
    tw = bsdf.inputs.get("Transmission")
if tw is not None:
    tw.default_value = 1.0

# 优化透明渲染设置（Blender 5.3 EEVEE）
mat_glass.surface_render_method = 'BLENDED'
mat_glass.use_screen_refraction = True
# 适当调整 Alpha 让内部灯丝与玻璃外壁在 EEVEE 下都能通透展现
bsdf.inputs["Alpha"].default_value = 0.45

# 4. 创建/更新内部橙色发光材质 MAT_Amber_Filament
mat_filament = bpy.data.materials.get("MAT_Amber_Filament") or bpy.data.materials.new("MAT_Amber_Filament")
mat_filament.use_nodes = True
nodes_fil = mat_filament.node_tree.nodes
links_fil = mat_filament.node_tree.links
nodes_fil.clear()
bsdf_fil = nodes_fil.new("ShaderNodeBsdfPrincipled")
out_fil = nodes_fil.new("ShaderNodeOutputMaterial")
links_fil.new(bsdf_fil.outputs["BSDF"], out_fil.inputs["Surface"])

# 橙色 #FF8020 -> (1.0, 0.502, 0.125, 1.0), Emission Strength = 5.0
bsdf_fil.inputs["Base Color"].default_value = (1.0, 0.502, 0.125, 1.0)
bsdf_fil.inputs["Roughness"].default_value = 0.2
ec = bsdf_fil.inputs.get("Emission Color")
es = bsdf_fil.inputs.get("Emission Strength")
if ec is not None:
    ec.default_value = (1.0, 0.502, 0.125, 1.0)
if es is not None:
    es.default_value = 5.0

# 5. 制作两个电子管：
# 位置：tube 1 at (0.05, -0.12, 0.385), tube 2 at (0.22, -0.12, 0.385)
# 结构：
# a) 灰色金属/电木管座 (Base)
# b) 玻璃外壳 (Cylinder: Vertices 32, Radius 0.055, Depth 0.13, 顶部微倒角球化)
# c) 内部橙色发光灯丝 (Cylinder: Radius 0.018, Depth 0.09)
# d) 顶部内侧发光圆盘 (Emissive Disc: 确保俯视垂直角度直击醒目橙光)

tube_x_coords = [0.05, 0.22]
y_pos = -0.12
z_housing_top = 0.385  # 主盒上表面高约 0.385

for idx, x_pos in enumerate(tube_x_coords, start=1):
    # a. 底座 (Bakelite Socket Base)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32, radius=0.065, depth=0.025,
        location=(x_pos, y_pos, z_housing_top + 0.0125)
    )
    base_obj = bpy.context.active_object
    base_obj.name = f"WPN_TubeBase_{idx}"
    bpy.ops.object.shade_smooth()
    mat_bakelite = bpy.data.materials.get("WPN_Bakelite")
    if mat_bakelite:
        base_obj.data.materials.append(mat_bakelite)

    # b. 玻璃外壳 (Glass Envelope)
    # Radius 0.055, Depth 0.13. 底部微插底座 (z 从 z_housing_top + 0.015 到 + 0.145, 中心在 z = 0.45)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32, radius=0.055, depth=0.13,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.065)
    )
    glass_obj = bpy.context.active_object
    glass_obj.name = f"WPN_TubeGlass_{idx}"
    bpy.ops.object.shade_smooth()
    glass_obj.data.materials.append(mat_glass)

    # 给玻璃外壳上边缘倒角，形成圆润复古电子管顶端
    bev = glass_obj.modifiers.new(name="Bevel", type='BEVEL')
    bev.width = 0.012
    bev.segments = 3
    bpy.ops.object.modifier_apply(modifier="Bevel")

    # c. 内部橙色发光灯丝芯柱 (Amber Filament)
    # Radius 0.018, Depth 0.09, 放置在玻璃外壳正中心
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=0.018, depth=0.09,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.05)
    )
    fil_obj = bpy.context.active_object
    fil_obj.name = f"WPN_TubeFilament_{idx}"
    bpy.ops.object.shade_smooth()
    fil_obj.data.materials.append(mat_filament)

    # d. 顶部内侧发光圆盘 (Top Emissive Disc)
    # 尺寸略大于灯丝柱，朝上，俯视角提供强烈的穿透橙光
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=0.032, depth=0.008,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.095)
    )
    disc_obj = bpy.context.active_object
    disc_obj.name = f"WPN_TubeTopDisc_{idx}"
    bpy.ops.object.shade_smooth()
    disc_obj.data.materials.append(mat_filament)

# 6. 设置 EEVEE 场景光追与反射
bpy.context.scene.eevee.use_raytracing = True

# 7. 渲染输出
# A. 45° 完整俯视视角 (满足指导中的：补一张完整的 45° 俯视角画面)
# 相机放置在右上方斜看全貌，使大喇叭、电磁线圈、机身表盘和发光电子管一览无余
cam_obj = bpy.data.objects["RenderCam"]
target = Vector((0.0, 0.0, 0.22))

# 45度俯视全景：y 轴后退，z 轴抬高，x 轴稍偏向侧方形成良好层次
cam_obj.location = Vector((-0.2, -1.8, 1.8))
direction = target - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

bpy.context.scene.render.resolution_x = 1280
bpy.context.scene.render.resolution_y = 720
bpy.context.scene.render.engine = 'BLENDER_EEVEE'

render_top45_path = kit_root / "render_topdown_45deg.png"
bpy.context.scene.render.filepath = str(render_top45_path)
bpy.ops.render.render(write_still=True)
print("Rendered 45 deg topdown to:", render_top45_path)

# B. 顶部聚焦视口渲染 (特写呈现两枚发光电子管、玻璃折射与仪表配合)
cam_obj.location = Vector((0.13, -0.9, 1.3))
direction = Vector((0.13, -0.05, 0.35)) - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

render_tube_focus_path = kit_root / "render_tubes_focus.png"
bpy.context.scene.render.filepath = str(render_tube_focus_path)
bpy.ops.render.render(write_still=True)
print("Rendered tubes focus to:", render_tube_focus_path)

# C. 俯视游戏实战视角 (正上方稍带斜度)
cam_obj.location = Vector((0.0, -0.9, 2.2))
direction = target - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

render_top_path = kit_root / "render_topdown.png"
bpy.context.scene.render.filepath = str(render_top_path)
bpy.ops.render.render(write_still=True)
print("Updated render_topdown.png")

# 保存工程与更新 FBX
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("Saved blend file.")

fbx_path = kit_root / "WPN_OldSonicModulator.fbx"
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(fbx_path),
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL'
)
print("Updated FBX export.")
