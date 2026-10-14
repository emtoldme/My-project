# Human Fieldwalker source

The editable `Human_Fieldwalker.blend` is the R7 derivative, with original clothing,
hair, satchel, scarf, generic rig and deterministic skin weights. The FBX under
`Assets/Game/Art/VisualIdentity/HumanEchoesR7` is the exported runtime asset.

Anatomical body and eyes derive from the user's existing
`Protagonist_v05_Basemesh.blend`, which contains the stylized male body and eyes from
**Blender Human Base Meshes v1.4.1, Simon Thommes / Blender Studio, CC0**.
Official provenance: https://www.blender.org/download/demo-files/
and https://download.blender.org/demo/asset-bundles/human-base-meshes/ .

The user's original files and the full CC0 bundle remain untouched outside this
checkout. No BlenderKit clothing files are redistributed. Clothing, props, hair,
rig, runtime movement poses and ghost geometry/shading were authored for R7.

Open the derivative `.blend` to edit/export directly. `build_human.py` reconstructs
the derivative in Blender 5.x if the original v05 source is available; set
`REVERB_HUMAN_BASEMESH` to its absolute path on another computer. The script writes
only the derivative and exported FBX, never its input file. Keep Unity closed while
regenerating imported assets. Imported `.meta` GUIDs should be preserved.

The Unity-native ghost mesh asset is the editable runtime geometry. The one-time
native installer/review source is archived in this run's external Reviews/Tools
folder to prevent accidental rerunning over current scenes.
