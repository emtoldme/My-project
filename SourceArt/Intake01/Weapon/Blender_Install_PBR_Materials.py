# -*- coding: utf-8 -*-
"""Blender 4.x/5.x: install Old Sonic Modulator PBR texture materials.
Before running:
1. Unzip this asset kit.
2. Save your .blend in the asset kit's ROOT directory (same level as textures/).
3. Scripting workspace > Open this script > Run Script.
4. Choose materials from Material Properties for each mesh.
The script does NOT modify your geometry or existing materials.
"""
import bpy
from pathlib import Path

BASE = Path(bpy.path.abspath("//"))
TEXTURES = BASE / "textures"
if not TEXTURES.exists():
    raise FileNotFoundError(
        f"Cannot find {TEXTURES}. Please save the .blend in the unzipped kit root first."
    )

MATS = {
    "01_Olive_ChippedPaint": "WPN_Painted_Olive",
    "02_Aged_Brass": "WPN_Aged_Brass",
    "03_Oxidized_Copper": "WPN_Copper_Coil",
    "04_Brown_Bakelite": "WPN_Bakelite",
    "05_Worn_Wood": "WPN_Worn_Wood",
    "06_Dusty_Canvas": "WPN_Dusty_Canvas",
}


def image_node(nt, filename, label, space):
    path = TEXTURES / filename
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
    node = mat.node_tree.nodes.get("Principled BSDF")
    if node is None:
        mat.node_tree.nodes.clear()
        node = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        out = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
        mat.node_tree.links.new(node.outputs["BSDF"], out.inputs["Surface"])
    node.inputs["Base Color"].default_value=(0.47,0.11,0.025,1)
    node.inputs["Roughness"].default_value=.25
    ec = node.inputs.get("Emission Color")
    es = node.inputs.get("Emission Strength")
    if ec is not None: ec.default_value=(1,.26,.035,1)
    if es is not None: es.default_value=3.0
    return mat

for prefix, name in MATS.items():
    pbr_material(prefix, name)

decal_material("08_Analog_Meter_Face_RGBA.png", "WPN_Analog_Meter_Decal")
decal_material("09_Tuning_Dial_RGBA.png", "WPN_Tuning_Dial_Decal")
emission_material()
print("[Old Sonic Modulator] 9 materials created. Assign them via Material Properties.")
