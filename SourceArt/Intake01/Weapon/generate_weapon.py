import bpy
import bmesh
import math
from mathutils import Vector, Matrix
from pathlib import Path

# Clear all mesh objects
bpy.ops.wm.read_factory_settings(use_empty=True)

# 1. Load materials using the provided logic
kit_root = Path(r"G:\图书馆\Old_Sonic_Modulator_AssetKit")
textures_dir = kit_root / "textures"

def image_node(nt, filename, label, space):
    path = textures_dir / filename
    image = bpy.data.images.load(str(path), check_existing=True)
    image.colorspace_settings.name = space
    node = nt.nodes.new("ShaderNodeTexImage")
    node.label = label
    node.image = image
    return node

def pbr_material(prefix, name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial"); out.location = (800, 120)
    bsdf = nodes.new("ShaderNodeBsdfPrincipled"); bsdf.location = (480, 120)
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bc = image_node(mat.node_tree, prefix + "_BaseColor.png", "Base Color / sRGB", "sRGB")
    mt = image_node(mat.node_tree, prefix + "_Metallic.png", "Metallic / Non-Color", "Non-Color")
    ro = image_node(mat.node_tree, prefix + "_Roughness.png", "Roughness / Non-Color", "Non-Color")
    nm = image_node(mat.node_tree, prefix + "_Normal_OpenGL.png", "Normal / Non-Color", "Non-Color")
    bc.location = (-640, 440)
    mt.location = (-640, 150)
    ro.location = (-640, -120)
    nm.location = (-640, -390)
    normal = nodes.new("ShaderNodeNormalMap")
    normal.location = (210, -260)
    normal.inputs["Strength"].default_value = 0.55
    links.new(bc.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(mt.outputs["Color"], bsdf.inputs["Metallic"])
    links.new(ro.outputs["Color"], bsdf.inputs["Roughness"])
    links.new(nm.outputs["Color"], normal.inputs["Color"])
    links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])
    return mat

def decal_material(filename, name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); out.location = (550,0)
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled"); bsdf.location=(250,0)
    node = image_node(nt,filename,"Decal image", "sRGB"); node.location=(-130,0)
    nt.links.new(node.outputs["Color"],bsdf.inputs["Base Color"])
    nt.links.new(node.outputs["Alpha"],bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.65
    nt.links.new(bsdf.outputs["BSDF"],out.inputs["Surface"])
    return mat

def emission_material():
    mat = bpy.data.materials.get("WPN_Tube_AmberGlow") or bpy.data.materials.new("WPN_Tube_AmberGlow")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    out = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (0.47, 0.11, 0.025, 1)
    bsdf.inputs["Roughness"].default_value = 0.25
    ec = bsdf.inputs.get("Emission Color")
    es = bsdf.inputs.get("Emission Strength")
    if ec is not None: ec.default_value = (1, 0.26, 0.035, 1)
    if es is not None: es.default_value = 5.0
    return mat

def glass_material():
    mat = bpy.data.materials.get("WPN_Glass") or bpy.data.materials.new("WPN_Glass")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    out = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (0.95, 0.98, 1.0, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.08
    bsdf.inputs["IOR"].default_value = 1.45
    tw = bsdf.inputs.get("Transmission Weight")
    if tw is None:
        tw = bsdf.inputs.get("Transmission")
    if tw is not None:
        tw.default_value = 0.92
    return mat

MATS = {
    "01_Olive_ChippedPaint": "WPN_Painted_Olive",
    "02_Aged_Brass": "WPN_Aged_Brass",
    "03_Oxidized_Copper": "WPN_Copper_Coil",
    "04_Brown_Bakelite": "WPN_Bakelite",
    "05_Worn_Wood": "WPN_Worn_Wood",
    "06_Dusty_Canvas": "WPN_Dusty_Canvas",
}

for prefix, name in MATS.items():
    pbr_material(prefix, name)
decal_material("08_Analog_Meter_Face_RGBA.png", "WPN_Analog_Meter_Decal")
decal_material("09_Tuning_Dial_RGBA.png", "WPN_Tuning_Dial_Decal")
emission_material()
glass_material()

# Helper function to assign material and smart UV project
def setup_mesh_object(obj, mat_name=None, uv_project=True):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if mat_name:
        mat = bpy.data.materials.get(mat_name)
        if mat:
            obj.data.materials.append(mat)
    if uv_project:
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
        bpy.ops.object.mode_set(mode='OBJECT')

# ==============================================================================
# Step 1: Central Housing (WPN_MainHousing)
# Location: (0.15, 0, 0.22), Dimensions: (0.9, 0.5, 0.33)
# Ctrl+A -> Scale, Tab -> Bevel 0.035, 3 segments
# ==============================================================================
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.15, 0, 0.22))
housing = bpy.context.active_object
housing.name = "WPN_MainHousing"
housing.scale = (0.9, 0.5, 0.33)
bpy.ops.object.transform_apply(scale=True)

# Bevel modifier or bevel via edit mode
bev = housing.modifiers.new(name="Bevel", type='BEVEL')
bev.width = 0.035
bev.segments = 3
bpy.ops.object.modifier_apply(modifier="Bevel")
bpy.ops.object.shade_smooth()
setup_mesh_object(housing, "WPN_Painted_Olive")

# ==============================================================================
# Step 2: Speaker Horn (WPN_SpeakerHorn)
# Circle vertices 24, rotate Y 90, scale 0.18, location (-0.45, 0, 0.22)
# Extrusions:
# 1: E X -0.20 -> S 1.2  (x = -0.65, r = 0.18*1.2 = 0.216)
# 2: E X -0.35 -> S 1.7  (x = -1.00, r = 0.216*1.7 = 0.3672)
# 3: E X -0.20 -> S 1.25 (x = -1.20, r = 0.3672*1.25 = 0.459)
# Solidify 0.025, Bevel 0.015 segments 2
# ==============================================================================
horn_mesh = bpy.data.meshes.new("WPN_SpeakerHorn")
horn_obj = bpy.data.objects.new("WPN_SpeakerHorn", horn_mesh)
bpy.context.collection.objects.link(horn_obj)

bm = bmesh.new()
# Rings definition along -X
# ring 0: x = -0.45, r = 0.18
# ring 1: x = -0.65, r = 0.18 * 1.2 = 0.216
# ring 2: x = -1.00, r = 0.216 * 1.7 = 0.3672
# ring 3: x = -1.20, r = 0.3672 * 1.25 = 0.459
rings = [
    (-0.45, 0.18),
    (-0.65, 0.216),
    (-1.00, 0.3672),
    (-1.20, 0.459)
]

num_verts = 24
center_z = 0.22
prev_verts = []

for idx, (x_pos, radius) in enumerate(rings):
    curr_verts = []
    for i in range(num_verts):
        angle = 2.0 * math.pi * i / num_verts
        y = radius * math.sin(angle)
        z = center_z + radius * math.cos(angle)
        v = bm.verts.new((x_pos, y, z))
        curr_verts.append(v)
    if prev_verts:
        for i in range(num_verts):
            i_next = (i + 1) % num_verts
            bm.faces.new([prev_verts[i], prev_verts[i_next], curr_verts[i_next], curr_verts[i]])
    prev_verts = curr_verts

bm.to_mesh(horn_mesh)
bm.free()

horn_obj.select_set(True)
bpy.context.view_layer.objects.active = horn_obj
bpy.ops.object.shade_smooth()

# Modifiers: Solidify and Bevel
sol = horn_obj.modifiers.new(name="Solidify", type='SOLIDIFY')
sol.thickness = 0.025
sol.offset = 1.0  # outward or inward

bev_horn = horn_obj.modifiers.new(name="Bevel", type='BEVEL')
bev_horn.width = 0.015
bev_horn.segments = 2

setup_mesh_object(horn_obj, "WPN_Aged_Brass")

# ==============================================================================
# Step 3: Coil Core & Copper Coil (WPN_CoilCore, WPN_Copper_Coil)
# Cylinder: radius 0.16, depth 0.36, vertices 24, rot Y 90, center (-0.39, 0, 0.22)
# Torus: major 0.18, minor 0.012, rot Y 90, start (-0.55, 0, 0.22)
# 13 rings spaced 0.025 along +X
# ==============================================================================
bpy.ops.mesh.primitive_cylinder_add(
    vertices=24, radius=0.16, depth=0.36,
    location=(-0.39, 0, 0.22),
    rotation=(0, math.pi/2, 0)
)
coil_core = bpy.context.active_object
coil_core.name = "WPN_CoilCore"
bpy.ops.object.shade_smooth()
setup_mesh_object(coil_core, "WPN_Bakelite")

# Torus rings
torus_objs = []
for i in range(13):
    x_pos = -0.55 + i * 0.025
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.18, minor_radius=0.012,
        location=(x_pos, 0, 0.22),
        rotation=(0, math.pi/2, 0)
    )
    t_obj = bpy.context.active_object
    t_obj.name = f"WPN_CoilRing_{i:02d}"
    bpy.ops.object.shade_smooth()
    torus_objs.append(t_obj)

# Join torus rings into one single object WPN_CopperCoil
for obj in torus_objs:
    obj.select_set(True)
bpy.context.view_layer.objects.active = torus_objs[0]
bpy.ops.object.join()
coil_joined = bpy.context.active_object
coil_joined.name = "WPN_CopperCoil"
setup_mesh_object(coil_joined, "WPN_Copper_Coil")

# ==============================================================================
# Step 4: Top Meter and Vacuum Tubes
# Meter Face: Plane 0.31 x 0.17 at (-0.035, 0.07, 0.393)
# Tuning Dial: Cylinder / Disc at (0.35, 0.0, 0.39) radius 0.09 depth 0.02
# Vacuum Tubes: 2 tubes at (0.1, -0.12, 0.45) and (0.28, -0.12, 0.45)
#   Outer glass cylinder: r=0.06, depth=0.14
#   Inner filament cylinder: r=0.02, depth=0.10
# ==============================================================================
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(-0.035, 0.07, 0.390))
meter_plane = bpy.context.active_object
meter_plane.name = "WPN_MeterFace"
meter_plane.scale = (0.31, 0.17, 1.0)
bpy.ops.object.transform_apply(scale=True)
setup_mesh_object(meter_plane, "WPN_Analog_Meter_Decal", uv_project=False)

# Custom UV for meter face so the decal maps nicely
me = meter_plane.data
uv_layer = me.uv_layers.new(name="UVMap")
# plane vertices order: (-x,-y), (x,-y), (-x,y), (x,y)
for loop in me.loops:
    vert = me.vertices[loop.vertex_index]
    # normalize vert x [-0.155, 0.155] to [0, 1], y [-0.085, 0.085] to [0, 1]
    u = (vert.co.x + 0.155) / 0.31
    v = (vert.co.y + 0.085) / 0.17
    uv_layer.data[loop.index].uv = (u, v)

# Tuning Dial Knob
bpy.ops.mesh.primitive_cylinder_add(
    vertices=24, radius=0.09, depth=0.03,
    location=(0.40, -0.05, 0.395)
)
dial_knob = bpy.context.active_object
dial_knob.name = "WPN_TuningDial"
bpy.ops.object.shade_smooth()
setup_mesh_object(dial_knob, "WPN_Tuning_Dial_Decal")

# Vacuum Tubes
tube_positions = [(0.05, -0.12, 0.44), (0.22, -0.12, 0.44)]
for i, pos in enumerate(tube_positions):
    # Tube Base (Bakelite socket)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.07, depth=0.02,
        location=(pos[0], pos[1], 0.395)
    )
    base = bpy.context.active_object
    base.name = f"WPN_TubeBase_{i+1}"
    setup_mesh_object(base, "WPN_Bakelite")

    # Filament (Inner emitter)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=12, radius=0.022, depth=0.09,
        location=(pos[0], pos[1], pos[2])
    )
    fil = bpy.context.active_object
    fil.name = f"WPN_TubeFilament_{i+1}"
    setup_mesh_object(fil, "WPN_Tube_AmberGlow")

    # Outer Glass
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16, radius=0.058, depth=0.13,
        location=(pos[0], pos[1], pos[2] + 0.015)
    )
    glass = bpy.context.active_object
    glass.name = f"WPN_VacuumTube_{i+1}"
    setup_mesh_object(glass, "WPN_Glass")

# ==============================================================================
# Step 5: Rear Wooden Handle & Canvas Straps
# Cylinder: radius 0.095, depth 0.5, rot Y 90, location (0.86, 0, 0.24)
# Straps: cylinders / toruses around handle
# ==============================================================================
bpy.ops.mesh.primitive_cylinder_add(
    vertices=24, radius=0.08, depth=0.52,
    location=(0.86, 0, 0.22),
    rotation=(0, math.pi/2, 0)
)
handle = bpy.context.active_object
handle.name = "WPN_RearHandle"
bpy.ops.object.shade_smooth()
setup_mesh_object(handle, "WPN_Worn_Wood")

# Canvas straps wrapping around the handle
strap_x_positions = [0.72, 0.86, 1.00]
strap_objs = []
for i, sx in enumerate(strap_x_positions):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24, radius=0.086, depth=0.045,
        location=(sx, 0, 0.22),
        rotation=(0, math.pi/2, 0)
    )
    strap = bpy.context.active_object
    strap.name = f"WPN_CanvasStrap_{i+1}"
    bpy.ops.object.shade_smooth()
    setup_mesh_object(strap, "WPN_Dusty_Canvas")
    strap_objs.append(strap)

# Join straps
for s in strap_objs:
    s.select_set(True)
bpy.context.view_layer.objects.active = strap_objs[0]
bpy.ops.object.join()
joined_straps = bpy.context.active_object
joined_straps.name = "WPN_CanvasWraps"

# Save .blend file
blend_out_path = kit_root / "Old_Sonic_Modulator.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_out_path))
print("Saved blend file to:", blend_out_path)

# Set up camera and lighting to render preview shots
cam_data = bpy.data.cameras.new("RenderCam")
cam_obj = bpy.data.objects.new("RenderCam", cam_data)
bpy.context.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj

# Camera 1: Perspective hero angle (isometric 3/4 top view)
cam_obj.location = (-1.2, -1.8, 1.4)
# Point at center around (-0.1, 0, 0.25)
direction = Vector((-0.1, 0, 0.25)) - cam_obj.location
rot_quat = direction.to_track_quat('-Z', 'Y')
cam_obj.rotation_euler = rot_quat.to_euler()

# Lighting: Key light, Fill light, Rim light
light_data_key = bpy.data.lights.new(name="KeyLight", type='SUN')
light_data_key.energy = 4.0
light_obj_key = bpy.data.objects.new("KeyLight", light_data_key)
bpy.context.collection.objects.link(light_obj_key)
light_obj_key.rotation_euler = (math.radians(45), math.radians(20), math.radians(-30))

light_data_fill = bpy.data.lights.new(name="FillLight", type='POINT')
light_data_fill.energy = 150.0
light_obj_fill = bpy.data.objects.new("FillLight", light_data_fill)
bpy.context.collection.objects.link(light_obj_fill)
light_obj_fill.location = (0.5, 1.5, 1.2)

# World background
bpy.context.scene.world = bpy.data.worlds.new("StudioWorld")
bpy.context.scene.world.use_nodes = True
bg_node = bpy.context.scene.world.node_tree.nodes.get("Background")
if bg_node:
    bg_node.inputs["Color"].default_value = (0.05, 0.06, 0.08, 1.0)
    bg_node.inputs["Strength"].default_value = 0.8

# Render settings
bpy.context.scene.render.resolution_x = 1280
bpy.context.scene.render.resolution_y = 720
bpy.context.scene.render.engine = 'BLENDER_EEVEE'

preview_img1 = kit_root / "render_perspective.png"
bpy.context.scene.render.filepath = str(preview_img1)
bpy.ops.render.render(write_still=True)
print("Rendered hero shot to:", preview_img1)

# Camera 2: Top-down game perspective (as requested in design criteria: 俯视验证)
cam_obj.location = (-0.1, -0.6, 2.2)
direction = Vector((-0.1, 0, 0.22)) - cam_obj.location
cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

preview_img2 = kit_root / "render_topdown.png"
bpy.context.scene.render.filepath = str(preview_img2)
bpy.ops.render.render(write_still=True)
print("Rendered topdown shot to:", preview_img2)

# Save .blend file with camera setup
blend_out_path = kit_root / "Old_Sonic_Modulator.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_out_path))
print("Saved blend file to:", blend_out_path)

# Export FBX for Unity ready
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
print("Exported FBX to:", fbx_path)
