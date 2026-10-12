import bpy
import bmesh
import math
from mathutils import Vector
from pathlib import Path

blend_path = r"G:\图书馆\Old_Sonic_Modulator_AssetKit\Old_Sonic_Modulator.blend"
bpy.ops.wm.open_mainfile(filepath=blend_path)

kit_root = Path(r"G:\图书馆\Old_Sonic_Modulator_AssetKit")

# ==============================================================================
# 1. 清理旧电子管相关的所有部件
# ==============================================================================
objs_to_delete = [
    obj.name for obj in bpy.data.objects 
    if "Tube" in obj.name or "Filament" in obj.name or "Disc" in obj.name
]
for name in objs_to_delete:
    obj = bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)
print("Removed old tube objects:", objs_to_delete)

# ==============================================================================
# 2. 优化材质
# ==============================================================================
# A. 内部金属支架/阳极网板材质 MAT_Tube_Metal (带金属光泽的深灰镍铁合金)
mat_tube_metal = bpy.data.materials.get("MAT_Tube_Metal") or bpy.data.materials.new("MAT_Tube_Metal")
mat_tube_metal.use_nodes = True
nodes_tm = mat_tube_metal.node_tree.nodes
nodes_tm.clear()
bsdf_tm = nodes_tm.new("ShaderNodeBsdfPrincipled")
out_tm = nodes_tm.new("ShaderNodeOutputMaterial")
mat_tube_metal.node_tree.links.new(bsdf_tm.outputs["BSDF"], out_tm.inputs["Surface"])
bsdf_tm.inputs["Base Color"].default_value = (0.25, 0.26, 0.28, 1.0)
bsdf_tm.inputs["Metallic"].default_value = 0.9
bsdf_tm.inputs["Roughness"].default_value = 0.25

# B. 橙色发光材质 MAT_Amber_Filament
# 解决“发白”问题：RGB选用饱和饱满的深暖橙，Emission Strength 控制在 2.0（指导建议 1.5~2.5）
mat_filament = bpy.data.materials.get("MAT_Amber_Filament") or bpy.data.materials.new("MAT_Amber_Filament")
mat_filament.use_nodes = True
nodes_fil = mat_filament.node_tree.nodes
nodes_fil.clear()
bsdf_fil = nodes_fil.new("ShaderNodeBsdfPrincipled")
out_fil = nodes_fil.new("ShaderNodeOutputMaterial")
mat_filament.node_tree.links.new(bsdf_fil.outputs["BSDF"], out_fil.inputs["Surface"])
# 饱满鲜亮的橙黄色暖光
bsdf_fil.inputs["Base Color"].default_value = (1.0, 0.35, 0.02, 1.0)
bsdf_fil.inputs["Roughness"].default_value = 0.3
ec = bsdf_fil.inputs.get("Emission Color")
es = bsdf_fil.inputs.get("Emission Strength")
if ec is not None:
    ec.default_value = (1.0, 0.38, 0.03, 1.0)
if es is not None:
    es.default_value = 2.0  # 柔和且通透饱和，绝不过曝泛白

# C. 玻璃材质 MAT_Vacuum_Glass
mat_glass = bpy.data.materials.get("MAT_Vacuum_Glass")
if not mat_glass:
    mat_glass = bpy.data.materials.new("MAT_Vacuum_Glass")
mat_glass.use_nodes = True
nodes_g = mat_glass.node_tree.nodes
nodes_g.clear()
bsdf_g = nodes_g.new("ShaderNodeBsdfPrincipled")
out_g = nodes_g.new("ShaderNodeOutputMaterial")
mat_glass.node_tree.links.new(bsdf_g.outputs["BSDF"], out_g.inputs["Surface"])
bsdf_g.inputs["Base Color"].default_value = (0.92, 0.95, 0.98, 1.0)
bsdf_g.inputs["Metallic"].default_value = 0.0
bsdf_g.inputs["Roughness"].default_value = 0.04
bsdf_g.inputs["IOR"].default_value = 1.45
tw = bsdf_g.inputs.get("Transmission Weight") or bsdf_g.inputs.get("Transmission")
if tw is not None:
    tw.default_value = 1.0
mat_glass.surface_render_method = 'BLENDED'
mat_glass.use_screen_refraction = True
bsdf_g.inputs["Alpha"].default_value = 0.35  # 高透光度，清晰展示内部机械和灯丝

# ==============================================================================
# 3. 布局重排（解决电子管遮挡仪表的问题）
# ==============================================================================
# 仪表位于：X: -0.19 ~ +0.12, Y: -0.015 ~ +0.155
# 我们将两个电子管移至 X = 0.19 与 X = 0.33，Y = -0.11
# 此时：
# - 仪表右边缘为 X = 0.12，距离左电子管 (X=0.19, 半径0.055 -> 边缘0.135) 有安全间隔；
# - 从 45° 俯视视点看，视线完全分离，仪表表盘 100% 毫无遮挡！
# - 调频旋钮 (X=0.40, Y=-0.05) 也与右电子管保持适中距离，形成“仪表 - 双电子管 - 调频大旋钮”富有节奏感的高低台阶布局。

tube_positions = [
    (0.185, -0.10),
    (0.325, -0.10)
]
z_housing_top = 0.385

mat_bakelite = bpy.data.materials.get("WPN_Bakelite")

for idx, (x_pos, y_pos) in enumerate(tube_positions, start=1):
    # 1. 电木底座
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32, radius=0.062, depth=0.02,
        location=(x_pos, y_pos, z_housing_top + 0.01)
    )
    base_obj = bpy.context.active_object
    base_obj.name = f"WPN_TubeBase_{idx}"
    bpy.ops.object.shade_smooth()
    if mat_bakelite:
        base_obj.data.materials.append(mat_bakelite)

    # 2. 内部金属支架系统（真实真空管内部框架结构）
    # a. 两根立式金属支柱 (Two metal support rods)
    rod_offset = 0.028
    for rod_i, rx in enumerate([-rod_offset, rod_offset]):
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=12, radius=0.003, depth=0.105,
            location=(x_pos + rx, y_pos, z_housing_top + 0.015 + 0.055)
        )
        rod = bpy.context.active_object
        rod.name = f"WPN_TubeRod_{idx}_{rod_i+1}"
        bpy.ops.object.shade_smooth()
        rod.data.materials.append(mat_tube_metal)

    # b. 顶部与底部金属云母固定片 (Mica spacer plates)
    for spacer_i, sz in enumerate([0.02, 0.09]):
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=16, radius=0.038, depth=0.003,
            location=(x_pos, y_pos, z_housing_top + 0.015 + sz)
        )
        spacer = bpy.context.active_object
        spacer.name = f"WPN_TubeSpacer_{idx}_{spacer_i+1}"
        spacer.data.materials.append(mat_tube_metal)

    # 3. 内部发光结构（按照建议参数大幅细化）
    # a. 发光芯柱：半径 0.009 m (建议 0.008~0.012 m)，高度 0.075 m
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.009, depth=0.072,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.052)
    )
    fil_obj = bpy.context.active_object
    fil_obj.name = f"WPN_TubeFilament_{idx}"
    bpy.ops.object.shade_smooth()
    fil_obj.data.materials.append(mat_filament)

    # b. 真实几何螺旋灯丝 (Helical wire around center)
    curve_data = bpy.data.curves.new(name=f"FilamentHelix_{idx}", type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.0018  # 细金属丝管径
    curve_data.bevel_resolution = 3
    polyline = curve_data.splines.new('POLY')
    
    num_pts = 36
    polyline.points.add(num_pts - 1)
    turns = 3.5
    helix_r = 0.015
    helix_h = 0.060
    z_start = z_housing_top + 0.015 + 0.022
    for pt_i in range(num_pts):
        t = pt_i / (num_pts - 1)
        ang = 2.0 * math.pi * turns * t
        px = x_pos + helix_r * math.cos(ang)
        py = y_pos + helix_r * math.sin(ang)
        pz = z_start + helix_h * t
        polyline.points[pt_i].co = (px, py, pz, 1.0)
    
    helix_obj = bpy.data.objects.new(f"WPN_TubeHelix_{idx}", curve_data)
    bpy.context.collection.objects.link(helix_obj)
    helix_obj.data.materials.append(mat_filament)

    # c. 顶部微型发光圆盘：半径 0.013 m (建议 0.012~0.015 m)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.013, depth=0.004,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.092)
    )
    disc_obj = bpy.context.active_object
    disc_obj.name = f"WPN_TubeTopDisc_{idx}"
    bpy.ops.object.shade_smooth()
    disc_obj.data.materials.append(mat_filament)

    # 4. 玻璃外壳 (Radius: 0.055 m, Depth: 0.13 m, 优雅圆润顶盖)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32, radius=0.055, depth=0.13,
        location=(x_pos, y_pos, z_housing_top + 0.015 + 0.065)
    )
    glass_obj = bpy.context.active_object
    glass_obj.name = f"WPN_TubeGlass_{idx}"
    bpy.ops.object.shade_smooth()
    glass_obj.data.materials.append(mat_glass)

    # 外壳边缘倒角
    bev = glass_obj.modifiers.new(name="Bevel", type='BEVEL')
    bev.width = 0.014
    bev.segments = 3
    bpy.ops.object.modifier_apply(modifier="Bevel")

print("Created realistic dual vacuum tubes with inner supports and fine helix filaments.")

# ==============================================================================
# 4. 渲染多角度图以供评审
# ==============================================================================
cam_obj = bpy.data.objects["RenderCam"]
target = Vector((0.0, 0.0, 0.22))

bpy.context.scene.render.resolution_x = 1280
bpy.context.scene.render.resolution_y = 720
bpy.context.scene.render.engine = 'BLENDER_EEVEE'

# A. 45° 完整俯视全景 (验证无遮挡、武器全貌与层次)
cam_obj.location = Vector((-0.2, -1.8, 1.8))
direction = target - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

render_top45_path = kit_root / "render_topdown_45deg.png"
bpy.context.scene.render.filepath = str(render_top45_path)
bpy.ops.render.render(write_still=True)
print("Rendered topdown 45deg to:", render_top45_path)

# B. 电子管与仪表特写 (验证电子管内部机械、金属支柱、螺旋细灯丝与表盘无遮挡)
focus_target = Vector((0.15, -0.02, 0.40))
cam_obj.location = Vector((0.15, -1.0, 1.25))
direction = focus_target - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

render_tubes_focus = kit_root / "render_tubes_focus.png"
bpy.context.scene.render.filepath = str(render_tubes_focus)
bpy.ops.render.render(write_still=True)
print("Rendered tubes focus to:", render_tubes_focus)

# C. 游戏正俯视视线
cam_obj.location = Vector((0.0, -0.9, 2.2))
direction = target - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

render_top_path = kit_root / "render_topdown.png"
bpy.context.scene.render.filepath = str(render_top_path)
bpy.ops.render.render(write_still=True)
print("Rendered topdown to:", render_top_path)

# 保存 .blend
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("Saved blend file.")

# 导出更新后的 FBX (Curve 螺旋灯丝自动转换为 mesh 导出)
fbx_path = kit_root / "WPN_OldSonicModulator.fbx"
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.objects:
    if obj.type in ['MESH', 'CURVE']:
        obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(fbx_path),
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL'
)
print("Exported FBX successfully.")
