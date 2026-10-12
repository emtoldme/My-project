# 《麦田里的混响》旧式声波调频器 · Blender 材质包与制作指南

## 包内内容

- `textures/01_Olive_ChippedPaint_*`：掉漆橄榄绿金属，应用于外壳。
- `textures/02_Aged_Brass_*`：旧黄铜，应用于喇叭、边框、紧固件。
- `textures/03_Oxidized_Copper_*`：氧化铜，用于线圈（线圈必须通过真实螺旋几何体表现）。
- `textures/04_Brown_Bakelite_*`：深棕电木，用于旋钮、绝缘体。
- `textures/05_Worn_Wood_*`：做旧木材，用于握柄。
- `textures/06_Dusty_Canvas_*`：脏旧帆布，用于绑带。
- `textures/07_Tube_Filament_Emission.png`：可选的橙色灯丝自发光纹理。
- `textures/08_Analog_Meter_Face_RGBA.png`：模拟仪表面板（1024×512）。
- `textures/09_Tuning_Dial_RGBA.png`：旋钮刻度盘（1024×1024，有透明背景）。
- `scripts/Blender_Install_PBR_Materials.py`：建立上述 PBR 材质节点的 Blender 脚本，方便学习和应用。
- `references/Weapon_Concept_Reference.png`：概念参考图，仅用于视觉设计，不能当作精确正交工程图。
- `材质预览图.jpg`：六种表面材料速览。

所有六套基础材质都是 1024×1024，程序生成、可循环铺设；已提供 Blender OpenGL 法线和 Unity 金属度/光滑度打包图。适合作为第一版游戏资产材质，尚非扫描材质。

## 单张材质的 PBR 文件说明

| 贴图后缀 | Blender 节点 | 色彩空间 | Unity URP Lit |
|---|---|---|---|
| `_BaseColor.png` | Principled BSDF → Base Color | sRGB | Base Map |
| `_Roughness.png` | Principled BSDF → Roughness | Non-Color | Unity Lit 不直接使用此图，需转换成 Smoothness |
| `_Metallic.png` | Principled BSDF → Metallic | Non-Color | 金属度，或使用打包纹理 |
| `_Normal_OpenGL.png` | Normal Map 节点 → Principled Normal | Non-Color | 标记 Texture Type 为 Normal map |
| `_Unity_MetallicSmoothness.png` | 不用于 Blender 的上述四节点 | Non-Color | 金属度存 R，光滑度存 Alpha（Smoothness = 1−Roughness） |

### Blender 自动添加材质（推荐）
1. 将压缩包全部解压到一个文件夹，例如 `D:\GameAssets\Old_Sonic_Modulator_AssetKit\`。
2. Blender → File → Save As，**将 `.blend` 文件保存于该文件夹根目录**，与 `textures` 同级。
3. 打开 Scripting 工作区 → Open → 选择 `scripts/Blender_Install_PBR_Materials.py`。
4. 点击 Run Script。脚本只添加材质，不生成模型或修改已有几何。
5. 选中模型部件 → 右侧 Material Properties → 从材质下拉列表选择以 `WPN_` 开头的材质。
6. 在 Material Preview 查看。File → External Data → Pack Resources 后存档。

### Blender 手动添加 PBR 节点（学习用）
1. 选中对象 → Material Properties → New。
2. Shading 工作区，添加 `Image Texture` 节点并打开 `_BaseColor.png`，连到 Principled BSDF 的 Base Color。
3. 新建 Image Texture 载入 `_Metallic.png`，Color Space 设为 **Non-Color**，Color 接 Metallic。
4. 同法添加 `_Roughness.png`，Non-Color，连 Roughness。
5. 添加 `_Normal_OpenGL.png`，Non-Color；它的 Color 接 `Normal Map` 节点的 Color；Normal Map 节点 Normal 接 Principled BSDF 的 Normal。初次用 Strength = 0.55。
6. 选中网格 → Tab 编辑模式 → A 全选 → U → Smart UV Project，确认展开 UV（对于复杂喇叭、木柄和线圈，后期可改为手动标缝展开）。

## 建模顺序建议（单位：米）

坐标约定：沿 X 轴布置；喇叭朝 **−X** 发射；顶部是 **+Z**。

| 物体命名 | 结构与尺寸建议 |
|---|---|
| `WPN_MainHousing` | 中央主盒：长 0.9、宽 0.5、高 0.33，中心点 (0.15, 0, 0.22) |
| `WPN_SpeakerHorn` | 喇叭从 X≈−0.45 的窄口（半径 0.18）扩张到 X≈−1.20 的大口（半径 0.46） |
| `WPN_CoilCore` | 线圈绝缘内芯，可用横向圆柱体，长约 0.35，半径 0.18 |
| `WPN_CopperHelix` | 内芯外侧用 Curve 或 Spin/Screw 制作螺旋，12–18 圈，真实几何更清楚 |
| `WPN_MeterFace` | 主盒 +Z 顶部设置平面仪表，宽度约 0.30，贴仪表面板 |
| `WPN_TuningDial` | 主盒 +Z 顶部设置圆形大旋钮，半径 0.10，贴刻度图 |
| `WPN_VacuumTube_01/02` | 顶部两个发光管，半径 0.065，高约 0.17 |
| `WPN_RearHandle` | 右端 X>0.6 的握柄，长约 0.5，木材、帆布纹理 |

**低模重点：** 不要先做大量螺丝与细线。先让武器在相机高度较远的俯视角里可辨认；优先保留喇叭、线圈、仪表和发光部件四个视觉特征。

## Unity URP 基本导入
1. Blender 物体模式选中武器所有部件，在 Unity 交付前统一 Apply Scale，并检查各部件法线方向。
2. File → Export → FBX，选 `Selected Objects`，Transform 按 Unity 导入结果检查，最终确保长度方向、朝向和尺寸符合武器挂点规则。
3. 将 FBX 和 `textures` 一并放进 Unity 的 `Assets/Art/Weapons/OldSonicModulator/`。
4. 在 Unity 中创建 Shader 为 **Universal Render Pipeline/Lit** 的材质：Base Map 放 BaseColor，Normal Map 放 Normal_OpenGL。
5. Metallic Map 使用对应的 `_Unity_MetallicSmoothness.png`：R 通道为 Metallic，Alpha 通道为 Smoothness。确保材质使用 Metallic 工作流，Smoothness 来源设置为 Metallic Alpha（界面选项随 Unity/URP 版本可能不同）。
6. 为发光灯丝单独建立 URP/Lit 材质、开启 Emission 并选择暖橙色；Bloom 要想显示泛光，摄像机与后处理也要启用相关设置。
7. 俯视镜头验证：角色持有时，大喇叭轮廓、线圈、表盘和橙色光源能否看清。如果只有一堆细碎零件，应先加大这四个视觉特征。

## 使用提醒
- 本套是**原创程序材质贴图**，可以用于游戏原型与商业项目，但需要根据 Unity 实际环境光照评估最终呈现效果。
- 贴图是基础材质，不包含具体物体 UV，因此仍需在 Blender 展 UV 才能准确显示。
- 法线图属于微表面效果，不会替代真实线圈、喇叭边缘、电子管几何。
- 如果材质显示成粉色，优先确认文件没有移动，或用 `File → External Data → Find Missing Files` 指向 `textures/`。
