# 《麦田里的混响》立体麦田植被包 v0.1

**定位：**Unity 6 URP / 俯视角 3D 幸存者 Like 项目的**可运行原型素材**。不是实拍扫描资产、不是高精度写实 PBR、不是经过 Unity 编辑器实测的最终美术资源。

## 包内资产

| 资产名称 | 类型 | 三角面 | 用途 |
|---|---|---:|---|
| Wheat_Cluster_A | 直立成熟麦穗，约 2m | 870 | 麦田主体 |
| Wheat_Cluster_B | 有倾斜变化的麦穗，约 1.9m | 696 | 降低重复感 |
| Wheat_Cluster_A_LOD1 | A 的简化版 | 336 | LOD 远景 |
| Wheat_Cluster_B_LOD1 | B 的简化版 | 252 | LOD 远景 |
| Cut_Stubble_A | 割后的断茎，约 0.5m | 340 | 过渡带、收割区域 |
| Wild_Grass_A | 矮野草，约 0.2m 高 | 224 | 裂缝、田垄 |

每个模型提供 **OBJ（`Assets/ReverbInTheRye/Foliage/Models`，供 Unity）**及 **GLB（根目录 `Blender_Models`，供 Blender）** 两种文件；两种格式是相同模型的重复交付，GLB 放在 Unity Assets 之外以免重复导入。包含 `Rye_Foliage_Atlas.png`（原创程序纹理图集）；`.mtl` 可供第三方 OBJ 查看器使用。

**方向/尺寸：**Y 轴向上，模型根部在 Y=0，建模单位为米。麦秆与麦穗都是网格，没有物理骨骼；摆动采用 GPU 顶点 Shader。OBJ 和 GLB 都是静态模型，不自带骨骼动画。

## 最快导入 Unity

1. 确保项目为 Unity 6 URP。**只复制本 ZIP 中的 `Assets/ReverbInTheRye` 文件夹**到 Unity 项目的 `Assets/` 文件夹里。注意它包含 Foliage，并包含此前地面包的一个小型编译修复脚本。
2. 等待 Unity 资源导入及脚本编译。执行菜单：**Tools → Reverb in the Rye → Create Foliage Prefabs**。
3. 脚本会自动创建 `Foliage/Materials/M_RyeFoliageWind.mat`，并创建 4 个 Prefab：`P_Wheat_A`, `P_Wheat_B`, `P_CutStubble`, `P_WildGrass`，其中麦穗 prefab 内含两级 LOD。
4. 将 prefabs 拖入场景。如果只想快速看一小片麦田，在已保存的测试场景里执行 **Tools → Reverb in the Rye → Scatter Foliage Demo (Current Scene)**。这会在世界坐标原点创建 `Rye_Foliage_TestPatch_DeleteWhenDone`，有大约 200 个原型对象（中间留出通行带），删除父节点即可清理。**请先备份/保存场景，菜单不会自动保存。**
5. 进入 Play Mode，观察麦穗是否随风摆动。停靠静态 Scene 视图可能不会看到与 Play Mode 完全一致的动画。
6. 若需要调整效果，打开材质 `M_RyeFoliageWind`：
   - `_WindStrength` 默认 0.13 m，建议先试 0.07~0.16。
   - `_WindSpeed` 默认 1.15，建议先试 0.7~1.5。
   - `_WindDirection.xz` 风向，例如 `(0.85,0,0.5,0)`。
   - `_PlantHeight` 默认 2 m，让植被底部不动、顶端摆动更明显。
7. 复制到 Git 工程后，在 Unity 导入完毕时**提交生成的 `.meta` 文件**，避免多人协作出现 GUID 丢失。

## 与上一版地面包共同使用

前面的地面包 v0.2 提供干土、麦秆覆盖、踩踏小路；本包是浮在地面上的立体植物。地面材质与植被材质不需要合并成同一 Material。

**修复：**我们发现上一版 `Ground/Editor/ReverbGroundSetup.cs` 在 `EnsureFolder` 函数里有一个 C# 反斜杠转义错误（原先 `Replace('\\', '/')` 中的字符字面量书写不正确）。本包包含修正后的**同路径脚本**，覆盖老脚本即可解决这一处编译问题。其余旧版地面贴图不需要重复复制。

## 模型特征和限制

- 模型真正包含小麦秆、麦叶、麦穗、麦芒的几何体，适合中远景、俯视角。并非从展示图里抠出 2D billboard。
- 贴图是程序调色 + 纤维细节的低对比图集；不能与现实麦穗摄影扫描同等看待。
- 顶点摆动 Shader 提供 `UniversalForward`、`ShadowCaster` 和 `DepthOnly` passes；同一顶点变形用于阴影投射（属于实现设计，**尚未在实际 Unity Editor 编译/截图验证**）。
- `Cull Off` 渲染草叶两面，可能增加片元开销。密集植被时使用 LOD、批量 GPU Instancing / Terrain GPU Instancing，降低可见数量。
- 两级 LOD 近远可能有小幅抽跳，且简化版麦穗长度存在差异；正式项目宜把 LOD0/LOD1 调整为等尺度同轮廓，或引入 Dither LOD CrossFade。
- 这个原型还没有碰撞，默认植物不阻挡玩家移动。对于幸存者 Like 战斗，通常应保持小草不阻挡玩家；如果需要实体障碍，单独放碰撞体。
- 仿真风效不会导致真实弯折碰撞/受击反应，不包含触碰交互。
- 一次性 Demo 菜单会创建 GameObject，不适用于在整张大地图上直接创建数万实例。
- Shader 是基于 URP HLSL API 编写，但目前环境**没有 Unity Editor**，无法保证工程无警告、没有编译错误，首次导入请确认 Console 和 Game 视图，如有报错再依据 Unity 版本修复。

## 建议验收项

1. 模型资源在 Scene/Game 可见；麦穗实际高度 1.5～2m。
2. Atlas 色彩与植被位置正常，没有紫色材质；如果变紫色，先核对 URP Shader 状态。
3. Play Mode 中植被底部相对固定、穗顶轻微风摆，且阴影随之变化。
4. Camera 使用目标游戏俯视角，测试玩家/敌人不会被密集麦穗完全遮挡。
5. 在 100 / 500 / 2000 丛测试帧率和批次数，后续再进行 GPU Instancing、Terrain Details、LOD 和密度配置。

## Blender 使用

- 打开 Blender → File → Import → glTF 2.0 → 从根目录 `Blender_Models/` 导入单个 `.glb`；也可以用 OBJ 导入。
- 如轴向不合，可按 Blender 导入选项调整。GLB 文件携带颜色图集。
- 模型为面片与低面数立体穗，不含 Armature；如果以后想制作单根麦秆动画，可以自行添加骨骼，但当前推荐交给 Unity Shader 统一风摆。

## 源码与授权说明

网格和图集是本次通过自编程几何与程序纹理生成，非第三方商用模型抓取。`Source` 中存有生成和预览的 Python 代码（需调整代码开头输出路径才能在本机独立运行）。`Previews` 图片是真实网格的离线软件渲染，**不是 Unity 运行截图**。
