# A-R05 用户自制旧式声波调频器资产包专业审查与接入规范 (Old Sonic Modulator Weapon AssetKit Audit & Ingestion Guide)

**文档属性**：主美专项主角核心武器资产技术审查交付物  
**审查对象**：用户自制并提供的旧式声波调频器核心武器套件 `@G:\图书馆\Old_Sonic_Modulator_AssetKit\`（包含 `Old_Sonic_Modulator.blend`、`WPN_OldSonicModulator.fbx`、PBR 贴图库、生成脚本与 4 张多角度实机离线渲染图）  
**基准分支**：`review/weapon-modulator-audit`（基于 `origin/main` @ `d143e7a`）  
**执行责任人**：主美  
**协同对象**：2 号（角色持枪装配与实机视效调试）、Codex（工程资源导入与武器脚本接线）

---

## 一、审查总评与核心艺术亮点 (Executive Summary)

继地面材质包（v0.2）、声波攻击特效包（v0.1）与立体麦田植被包（v0.1）之后，用户提供的**旧式声波调频器（Old Sonic Modulator）** 是**《麦田里的混响》整个项目的核心图腾与主角首发标志性武器**！

主美团队对该资产套件内的 Blender 源文件工程、导出的 FBX 模型结构、10 组贴图通道数据、生成/精修 Python 脚本，以及 4 张 720p 真实渲染视图进行了深入的几何拆解与通道级度量，核心结论如下：
1. **世界观深度咬合：Rustpunk × 乡村怪谈 × Shoegaze 模拟声学的完美具象化**：
   - 彻底摒弃了主流科幻的塑料流线感或工业机甲的笨重枪械感；
   - 采用了**掉漆橄榄绿军规外壳（Olive Chipped Paint）+ 大口径旧黄铜扬声器喇叭（Aged Brass Horn）+ 真实立体螺旋氧化铜线圈（Copper Helix）+ 双联发光电子真空管（Dual Vacuum Tubes）+ 模拟指针仪表盘与木质握把**；
   - 武器造型如同一台由 1980 年代报废广播站调频设备、农用发电机与军用雷达天线拼凑而成的“手持声波发射装置”，与“在荒芜麦田中调频怪异电波”的世界观叙事达成了 100% 的视觉统一！
2. **三维几何层级严谨，重点特征在 55° 斜俯视镜头下清晰可辨 (High Silhouette Readability)**：
   - 模型沿 X 轴布局，长度约 2.2m，发声大喇叭朝向 `-X`（射击方向），顶面为 `+Z`，充分考虑了俯视正交镜头的投射面；
   - 随套件中的精修脚本（`refine_tubes.py`）针对游戏俯视角进行了专项几何强化：将双联真空管避开仪表盘安全间距（X=0.185 与 0.325），内部构建了真实的双金属支柱（Support Rods）、云母固定绝缘垫片（Mica Spacers）、中心阴极柱、**3.5 圈立体螺旋发光灯丝（Beveled Helix Spline）** 与半透明倒角玻璃罩，在俯视角 45° 镜头下层次极其丰富生动。
3. **PBR 物理贴图规范完全贴合 Unity 6 URP 官方标准**：
   - 套件贴心地为全部 6 套基础材质提供了双套输出：既包含供 Blender 使用的独立通道，又预先打好了 **`_Unity_MetallicSmoothness.png`（R=金属度，Alpha=平滑度）**；
   - 彻底杜绝了外部常见 ORM 贴图导致的“粗糙度误当平滑度”的地面/武器反光过曝灾难。
4. **定位与资产定性**：
   - 该套件为 **可直接进实机装配的高保真核心资产（Ready-to-Assemble Asset Kit）**；
   - 其 FBX 已导出并应用了 `FBX_SCALE_ALL` 与 `-Z 前 / Y 上` 的 Unity 坐标约定，可直接挂载至主角手部骨骼或作为武器展示物。

---

## 二、六套核心材质通道实测与物理属性度量 (PBR Channel Measurements)

通过数字图像分析对 `textures/` 下的 10 组 1024x1024 贴图进行了通道级客观测量：

| 材质标识 / 部件 | 纹理通道配置 | 实测数值 (Channel Stats) | URP Lit 物理映射与材质评价 |
| :--- | :--- | :--- | :--- |
| **01_Olive_ChippedPaint**<br>*(中央主机箱外壳)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[64.1, 66.9, 46.9]` (低明度橄榄绿)<br>• Metallic (R): `mean = 22.0` (仅掉漆露出金属底处为高金属)<br>• Smoothness (A): `mean = 82.6` (**0.324**，克制哑光) | 优秀的掉漆军规质感，绝大部分区域为粗糙漆面（高散射），仅边缘划痕处透出底层铁锈与金属反光。 |
| **02_Aged_Brass**<br>*(扬声器喇叭、紧固件)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[135.5, 91.9, 42.9]` (陈旧暗黄铜色)<br>• Metallic (R): `mean = 240.0` (全金属特性)<br>• Smoothness (A): `mean = 156.7` (**0.615**，适度磨损铜光) | 极佳的黄铜质感，大喇叭内壁具有适度的各向异性泛光，在阳光照射下形成有力的武器轮廓线。 |
| **03_Oxidized_Copper**<br>*(外部缠绕螺旋铜线圈)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[132.2, 62.8, 31.2]` (氧化红铜色)<br>• Metallic (R): `mean = 223.7` (金属特性)<br>• Smoothness (A): `mean = 155.4` (**0.610**，半亮红铜) | 与中央电木内芯形成极强冷暖材质对比，螺旋立体几何产生强烈的工业机械节奏感。 |
| **04_Brown_Bakelite**<br>*(线圈内芯、管座、旋钮)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[29.9, 20.4, 15.7]` (深黑褐电木色)<br>• Metallic (R): `mean = 0.0` (绝对非金属)<br>• Smoothness (A): `mean = 163.2` (**0.640**，致密树脂光泽) | 完美的早期绝缘电木材料，深沉黑褐，为明亮的黄铜与铜线圈提供了优秀的暗调承托底衬。 |
| **05_Worn_Wood**<br>*(后部操作握把)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[84.6, 42.3, 20.5]` (风干老木纹)<br>• Metallic (R): `mean = 0.0` (绝对非金属)<br>• Smoothness (A): `mean = 68.8` (**0.270**，高度粗糙吸收) | 干燥开裂的老木头质感，握持手感厚重，契合农具与拼装枪械的质朴气质。 |
| **06_Dusty_Canvas**<br>*(握把防滑裹带)* | `_BaseColor`<br>`_Normal_OpenGL`<br>`_Unity_MetallicSmoothness` | • BaseColor RGB: `[160.0, 148.9, 122.2]` (灰土帆布白)<br>• Metallic (R): `mean = 0.0` (绝对非金属)<br>• Smoothness (A): `mean = 25.5` (**0.100**，极度吸光粗布) | 缠绕在握把上的陈旧布条，高漫反射完全吸光，强化了民间土制、反复包扎的废土历史感。 |
| **07~09 仪表与发光组件**<br>*(电子管灯丝与仪表盘)* | `07_Tube_Filament`<br>`08_Analog_Meter_Face`<br>`09_Tuning_Dial` | • 07 Emission: 橙红暖光灯丝纹理<br>• 08 Meter: 白底黑刻度指针面板（带透明边缘）<br>• 09 Dial: 带刻度调频大旋钮贴花 | 武器顶部的核心视觉焦点，为斜俯视镜头下的玩家提供清晰的“设备通电与调频就绪”信号。 |

---

## 三、斜俯视 55° 实机镜头下的辨识度审查 (Top-down Readability)

主美团队对套件中离线渲染的 4 张多角度图（`render_perspective.png`、`render_topdown.png`、`render_topdown_45deg.png`、`render_tubes_focus.png`）进行了构图与战斗辨识度分析：

1. **射击方向指向性极强（Clear Aiming Affordance）**：
   - 黄铜大喇叭（Speaker Horn）口径达到 0.92m（半径 0.46m），在 55° 斜俯视正交视角下形成了一个巨大的**漏斗形喇叭口剪影**；
   - 玩家一眼即可看出声波将从喇叭口向前喷薄而出，完全消除了手持枪支朝向模糊的问题。
2. **顶部设备三联焦点布局合理（No Occlusion Between Components）**：
   - 经 `refine_tubes.py` 优化后，顶部的四大元素在俯视镜头下呈梯形错落分布：
     - **左上方**：黑白分明的模拟指针表盘（`WPN_MeterFace`，宽 0.31m）；
     - **右上方**：两根发出微弱暖橙色光晕的真空电子管（`WPN_TubeGlass_1/2`，高 0.13m）；
     - **右下方**：大型调频旋钮（`WPN_TuningDial`，直径 0.18m）；
   - 三者互不遮挡，且高低错落，即便在角色奔跑与快速转身时，也能清晰捕捉到橙红灯丝与仪表指针的微光。
3. **与主角手持持枪姿态的尺寸匹配度（Proportion Check）**：
   - 武器整体长度约 2.2m（后握把 0.5m + 主箱体 0.9m + 喇叭 0.75m），属于典型的**双手中重型声波投射器（Two-handed Heavy Modulator）**；
   - 建议 2 号在配角色持枪动作时采用“右手握持后方木柄，左手横托中央线圈底部/主箱护木”的厚重重武器端枪姿态，彰显声波反冲力与物理分量感。

---

## 四、对 2 号（视效装配）与 Codex 的实装指南 (Integration Workflow)

### 1. 规范工程落位目录
建议 Codex 在合入资产时建立如下专用目录：
- 模型文件：`Assets/Art/Weapons/OldSonicModulator/WPN_OldSonicModulator.fbx`
- 贴图文件：`Assets/Art/Weapons/OldSonicModulator/Textures/`
- 材质文件：`Assets/Art/Weapons/OldSonicModulator/Materials/`
- 预制体：`Assets/Art/Weapons/OldSonicModulator/Prefabs/P_WPN_OldSonicModulator.prefab`

### 2. Unity 6 URP 材质设置关键要点
1. **基础部件材质（Olive, Brass, Copper, Bakelite, Wood, Canvas）**：
   - Shader: `Universal Render Pipeline/Lit`
   - `_BaseMap` 赋予对应 `_BaseColor.png`；
   - `_BumpMap` 赋予对应 `_Normal_OpenGL.png`（Texture Type 设为 `Normal map`）；
   - `_MetallicGlossMap` 赋予对应 `_Unity_MetallicSmoothness.png`（sRGB 必须取消勾选为 Linear），在 Inspector 中确认 `Smoothness Source: Metallic Alpha`。
2. **电子管发光灯丝材质（MAT_Amber_Filament）**：
   - Shader: `Universal Render Pipeline/Lit`
   - 开启 `Emission` 开关，Emission Color 选用暖橙色 `#FF6008`，HDR Intensity 设置为 **`1.5 ~ 2.0`**（切忌过曝，保证在关闭 Bloom 时仍可看清 3.5 圈螺旋金属丝细节）；
3. **电子管玻璃外壳材质（MAT_Vacuum_Glass）**：
   - Surface Type: `Transparent`，Blend Mode: `Alpha`；
   - Base Color 选用微冷浅蓝灰 `RGBA(235, 242, 250, 90)`（保持 0.35 适度透明度）；
   - Metallic: `0.0`，Smoothness: `0.95`（微弱高光反射，让玩家看到通透外壳的同时能窥见内部双金属立柱与灯丝）。

### 3. 枪口发射挂点设置（Muzzle Socket）
- 在预制体 `P_WPN_OldSonicModulator` 下建立空子节点：`Muzzle_Socket`；
- 坐标建议设在黄铜大喇叭最前端正中心：**`X = -1.20m, Y = 0.00m, Z = 0.22m`**，朝向为 `-X`（世界正前方转换）；
- 将前一刻审查通过的自制声波特效包 `SonicAttackVFX.PlayFire(muzzleTransform.position, muzzleTransform.forward)` 直接挂接于此位置，开火瞬间即可从大喇叭口同步激发出激光青蓝干涉环与前冲电波微粒！

---

## 五、全套四件套总成：麦田里的混响世界观样板完成

经过连续四轮自制资产的技术审查与规范构建，工程目前已集齐打造第一流独立试玩所需的全套自制素材：
1. **地面基底**：`Ground v0.2`（干土、麦秸、踩踏小路，绝对哑光去油腻）；
2. **立体麦浪**：`Foliage v0.1`（高矮立体麦丛与麦茬，GPU 顶点风动摇曳，防遮挡留白通廊）；
3. **声波特效**：`SonicVFX v0.1`（电波干涉脉冲、麦秸飞扬扬尘，非科幻激光的纯正音波反馈）；
4. **主角武器**：`Old Sonic Modulator`（掉漆橄榄绿箱体、黄铜喇叭、电木红铜线圈、双联发光电子管）。

主美强烈推荐 2 号与 Codex 尽快以此四件套为核心装配出 M01-R1 独立机制试玩的“黄金样板切片”，让玩家全面领略兼具 Rustpunk 手感与乡村怪谈意境的独特视听魅力！
