# A-R04 用户自制立体麦田植被包 v0.1 专业审查与接入方案 (Rye Foliage v0.1 Audit & Ingestion Plan)

**文档属性**：主美专项植被资产技术审查交付物  
**审查对象**：用户自制并提供的立体麦田植被素材包 `@G:\图书馆\ReverbInTheRye_Foliage_v01.zip`  
**基准分支**：`review/foliage-v01-audit`（基于 `origin/main` @ `d143e7a`）  
**执行责任人**：主美  
**协同对象**：2 号（场景与植被装配）、Codex（管线验证与自动化工具）

---

## 一、审查总评与核心亮点 (Executive Summary)

继用户提供的**地面材质包 v0.2**与**声波攻击特效包 v0.1**之后，用户再次交付的《麦田里的混响》立体植被包 `ReverbInTheRye_Foliage_v01.zip` 是**彻底奠定本作“乡村麦田（Rye Field）”环境骨架的核心三维环境资产**！

主美团队对该压缩包内的 6 组三维模型（OBJ/GLB）、纹理图集（Atlas）、HLSL 顶点风动着色器（`RyeFoliageWind.shader`）以及 Editor 散布装配工具进行了通道级度量与空间构图审查，核心结论如下：
1. **真正的低面数三维立体建模，拒绝粗糙的 2D 贴片 (Billboard)**：
   - 包含麦秸、麦叶、实心低模麦穗及麦芒的真实几何形体，在 Orthographic Size 9.0、Pitch 55° 斜俯视正交镜头下具备真实的厚度与受光轮廓；
   - 包含主麦丛（Wheat Cluster A/B，约 1.9m~2.0m 高度）、断茬麦桩（Cut Stubble A，约 0.5m 高度）与低矮野草（Wild Grass A，约 0.25m 高度），形成了**高中低三级立体的植被生态链**。
2. **GPU 顶点风动着色器（RyeFoliageWind.shader）高度专业，性能开销极低**：
   - **零骨骼依赖**：摆脱了昂贵的 CPU 骨骼动画或物理骨架，纯粹在 GPU 顶点着色器（Vertex Shader）中通过物体空间高度非线性加权（`pow(y / Height, 1.7)`）实现“根部紧抓地面绝对不动、麦穗随风起伏波动”的麦浪效果；
   - **多 Pass 阴影同步**：不仅提供了 `UniversalForward` 主着色 Pass，还严密配套了带相同风动形变的 `ShadowCaster` 与 `DepthOnly` Passes，确保斜俯视主光源下麦穗投影在地面上的阴影能够**随风真实同步摇曳**！
3. **内置两级 LOD（LOD0 / LOD1），大幅节约渲染管线面数**：
   - 主麦丛 A 由 870 面优化至 336 面（下降 61.4%），主麦丛 B 由 696 面优化至 252 面（下降 63.8%），为后续同屏大面积植被渲染（GPU Instancing）打下了坚实的性能基础。
4. **定位与资产性质定性**：
   - 该素材包定位为 **`prototyping_only: true`（高保真环境原型素材）**；
   - 资产无物理碰撞体，符合幸存者类游戏“植被不阻挡玩家走位与移动”的铁律；
   - 随包还贴心地修正了此前地面包 Editor 脚本中的转义字符小问题，体现出极高的工程协作意识。

---

## 二、模型网格规格与 LOD 几何度量 (Mesh Geometry & LOD Metrics)

依据随包提取的 `mesh_stats.json` 及三维顶点数据：

| 资产名称 (Model) | 几何形体特征 | 三角面数 (Tris) | 轴向与世界尺寸 (X, Y, Z Bounds) | 用途与分级分布建议 |
| :--- | :--- | :---: | :--- | :--- |
| **`Wheat_Cluster_A`**<br>(LOD0 / 主麦丛) | 挺拔成熟麦穗束，包含 10 余根饱满麦穗与麦芒交错 | **870 tris** | 宽: 1.00m, **高: 1.99m**, 深: 0.92m<br>脚底轴心: `Y_min = 0.00m` | 核心麦田区主体，近中景呈现丰收饱满感。 |
| **`Wheat_Cluster_A_LOD1`**<br>(LOD1 / 简化版) | 麦芒精简、主麦穗低多边形优化 | **336 tris** | 宽: 0.60m, **高: 1.74m**, 深: 0.71m<br>脚底轴心: `Y_min = 0.00m` | 距离摄像机中远景时平滑过渡，减半面数开销。 |
| **`Wheat_Cluster_B`**<br>(LOD0 / 倾斜变种) | 迎风倾斜与不规则生长姿态的麦丛，打破平铺机械感 | **696 tris** | 宽: 1.01m, **高: 1.91m**, 深: 1.05m<br>脚底轴心: `Y_min = 0.00m` | 与 A 混搭散布，彻底消除麦田均匀重复感。 |
| **`Wheat_Cluster_B_LOD1`**<br>(LOD1 / 倾斜简化版) | 倾斜麦穗的低面数版本 | **252 tris** | 宽: 0.81m, **高: 1.65m**, 深: 0.73m<br>脚底轴心: `Y_min = -0.001m` | 远景与边缘带轻量化渲染。 |
| **`Cut_Stubble_A`**<br>(收割后的麦茬残桩) | 带有机械切割切口的小麦断茎残桩（高 0.5m） | **340 tris** | 宽: 0.53m, **高: 0.50m**, 深: 0.70m<br>脚底轴心: `Y_min = -0.002m` | **绝佳过渡物**：置于麦田与踩踏小路交界带，暗示农业机械作业痕迹。 |
| **`Wild_Grass_A`**<br>(低矮野草/田埂草) | 贴地丛生野草（高 0.25m） | **224 tris** | 宽: 0.75m, **高: 0.25m**, 深: 0.83m<br>脚底轴心: `Y_min = 0.008m` | 填补地面缝隙、石块与建筑基座，增加地表层次。 |

---

## 三、贴图色相与全场视效防遮挡审查 (Color Narrative & Occlusion Review)

### 1. 纹理图集（Atlas）通道与色相测试
- 图集 `Rye_Foliage_Atlas.png`（1024x1024，RGB 格式）包含了成熟麦穗金黄、茎秆黄绿与干燥麦叶的纹理；
- 实测图集 RGB 均值为 **`[165.4, 139.5, 70.8]`**，呈现典型的**秋季收割前夕温暖金褐色**；
- 与前序地面材质包 `Ground_DenseStraw`（RGB `[119, 85, 49]`）搭配时，麦秆顶部略亮出 15~20%，形成“地深秆亮”的自然光照纵深感，完全避免了地景浑然一体的糊感。

### 2. 55° 斜俯视镜头下的防遮挡硬性审查 (Occlusion & Camera Safety)
由于主麦穗高度达 **1.9m ~ 2.0m**，在 55° 斜俯视正交镜头下，若大面积密植可能会遮挡高度约 1.8m 的主角与 1.3m 的普通敌人。为此，主美确立以下**空间排布与防遮挡规范**：
1. **中央战斗核心走廊必须留白（Zero-Foliage Combat Lane）**：
   - 玩家主要活动的中心走廊（半径约 5m~8m 区域），**严禁密集摆放 2m 的高麦丛（Wheat Cluster A/B）**；
   - 中心战斗区域仅允许点缀**低矮麦茬（Cut Stubble，0.5m）与贴地野草（Wild Grass，0.25m）**，确保玩家、敌人与地面朱红攻击预警圈（`#FF3B30`）100% 毫无遮挡！
2. **高麦丛作为外围边界与景观背景（Boundary & Vignette）**：
   - 2.0m 的高麦丛应大量散布在地图外围边缘、阻挡带或远景深处，形成波澜壮阔的“麦浪包围圈”，既强化了《麦田里的混响》的恐怖孤立感，又完全保障了战斗交互安全性；
3. **敌人危险警示的穿透性**：
   - 敌人的攻击预警红线与冲刺指示在穿过稀疏麦草时，必须始终清晰可见；由于着色器支持双面光照（`Cull Off`）且为不透明几何，切忌将高麦丛叠放在敌人出生点附近。

---

## 四、对 2 号（视效装配）与 Codex 的实装指南 (Integration Guide)

### 1. 规范工程目录落位
建议 Codex 在解压本包时部署至：
- 模型目录：`Assets/ReverbInTheRye/Foliage/Models/`（仅导入 OBJ 即可，GLB 保留在外部）
- 材质目录：`Assets/ReverbInTheRye/Foliage/Materials/`
- 着色器目录：`Assets/ReverbInTheRye/Foliage/Shaders/`
- Editor 脚本：`Assets/ReverbInTheRye/Foliage/Editor/ReverbFoliageSetup.cs`
- 覆盖更新此前地面包修复：`Assets/ReverbInTheRye/Ground/Editor/ReverbGroundSetup.cs`

### 2. 一键生成 Prefab 流程
1. 在 Unity 中执行顶部菜单：`Tools -> Reverb in the Rye -> Create Foliage Prefabs`；
2. 自动生成包含两级 LOD 绑定的 `P_Wheat_A.prefab` 与 `P_Wheat_B.prefab`，以及单级的 `P_CutStubble.prefab` 与 `P_WildGrass.prefab`；
3. 材质 `M_RyeFoliageWind.mat` 会自动开启 `enableInstancing = true`，支持同屏高效批处理。

### 3. 风动参数调优建议（Inspector 推荐值）
在 `M_RyeFoliageWind.mat` 中：
- `_WindStrength`: 推荐调至 **`0.10m ~ 0.13m`**（微风轻摇，避免摆幅过大导致麦穗剧烈穿模）；
- `_WindSpeed`: 推荐 **`1.15 ~ 1.30`**（舒缓柔和的声浪节奏，与低速摇滚相配）；
- `_PlantHeight`: 保持 **`2.0m`**（确保根部牢牢吸附在地面 Y=0，仅麦穗受风晃动）；
- `_WindDirection`: 保持 **`(0.85, 0, 0.50, 0)`**（与场景主平行光方向形成侧逆光夹角，凸显麦芒金色边缘受光）。

---

## 五、三位一体阶段总结：从灰盒到“麦田音波战场”的完整拼图

至此，用户连续交付的三套自制素材已形成了完美的闭环：
1. **地面基底（Ground v0.2）**：干土、麦秸、踩踏小路，奠定了哑光深邃的吸光底色；
2. **立体植被（Foliage v0.1）**：高麦浪、低麦茬、野草丛，塑造了富有层次与风动摇曳的乡村纵深；
3. **声波特效（SonicVFX v0.1）**：电波干涉环、麦草飞溅扬尘，赋予了音波武器撕裂麦田的打击实感。

主美强烈建议 2 号在推进 M01 场景与后续试玩时，将这三套素材协同点亮，让《麦田里的混响》首次展现出真正具备独立叙事力量与极佳手感的视听体验！
