# 《麦田里的混响》地面材质 v0.2 — Unity 6 / URP 原型素材

## 包含内容
3 种可重复平铺的原型地面贴图：
1. Ground_DrySoil（麦田泥土）：上一版 v0.1 的干泥土、麦秆和碎石基础地表。
2. Ground_DenseStraw（密集麦草）：碎麦秆大量覆盖地面，适合麦田内部或边缘。
3. Ground_TrampledPath（踩踏小路）：较显露的压实干土、碎石和稀疏麦秆，适合小路/可行走区域。

每种提供 BaseColor、Normal、MetallicSmoothness 三张 1024x1024 PNG。
BaseColor 是 sRGB；Normal 是基于颜色明暗推算的切线空间近似法线；
MetallicSmoothness 是 Unity URP Lit 的 RGBA packed map（RGB 金属度 0，Alpha 为低光滑度）。
每种有单独 2x2 平铺预览 JPG；Ground_Pack_Overview.jpg 为对照图；
Rye_Field_Blend_Concept.jpg 为使用实际纹理做的混合效果样图，并非 Unity 实机截图。

## 最快导入方式（Unity）
1. 确保项目启用了 Universal Render Pipeline (URP)。
2. 将 ZIP 内的 Assets/ReverbInTheRye 整个文件夹复制到现有 Unity 项目的 Assets 下。
3. Unity 编译完成后，点 Tools > Reverb in the Rye > Create Ground Materials。
4. 生成三个 Material 位于 Assets/ReverbInTheRye/Ground/Materials。
5. 生成三个 TerrainLayer 位于 Assets/ReverbInTheRye/Ground/TerrainLayers。
6. 如果用 Unity Terrain：选 Terrain > Paint Texture > Edit Terrain Layers > Add Layer；
   添加 DrySoil 为基底，再用 DenseStraw 覆盖麦田，TrampledPath 刷出田间小路。
7. 如果用 Plane/Mesh 地面：直接将对应 Material 拖到 MeshRenderer 的 Materials 中；
   在材质 Inspector 的 Tiling 里先试 (4,4)，按实际对象世界尺寸调整。
8. 建议 Terrain Layers 的 Tile Size 初始为 (4,4) 米，继续观察麦秆尺度调节。

## 注意事项
- 这是 AI 图像 + 图块重组得到的可平铺 *原型* 贴图，并非物理测量 PBR 材质。
- 02 和 03 的 1024 像素是纹理输出尺寸，不代表有原生 1024 像素细节：它们是从较小的素材
  图块合成的，近景会模糊，正式发布前需用高分辨率纹理替换或绘制精修版。
- 贴图仍有一定的光照/阴影烘焙痕迹；BaseColor 并非严格去光照 albedo。
- 法线由亮度估算，与真实几何高度不完全一致；本包不提供可信高度贴图。
- TextureImporter 用 URP/Lit 的 MetallicSmoothness，不适用于 Terrain 的 mask map；
  TerrainLayer 使用 diffuse、normal 和独立 smoothness，故并未将金属贴图误接 maskMap。
- 麦子高度/密度必须通过草丛网格、GPU Instancing、LOD 和风摆 Shader 表现。
- 大面积使用时应避免每块地都完全一样，可加入地表贴花、杂草、石头与全局色彩变化。

## 场景测试建议
1. 顶视 45° 摄像机，弱暖色主方向光，雾效少量。
2. DrySoil 覆盖 55%，DenseStraw 覆盖 35%，TrampledPath 覆盖 10%，仅为构图参考。
3. 优先测试小路与麦田的过渡、角色轮廓清晰度、重复纹理的可见程度。
4. 发布用素材要明确版权/授权来源，并进行人工美术质量审核。
