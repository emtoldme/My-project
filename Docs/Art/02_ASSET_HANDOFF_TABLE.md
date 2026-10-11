# S1 最小素材包交接规格表 (Asset Handoff Table)

**交付版本**：S1_AssetPack_v0.1  
**交付责任人**：主美  
**接入责任人**：2 号（框架与素材接入）  
**基准分支**：`art/s1-industrial-mech`  
**存放物理目录**：`Assets/Art_ThirdParty/`  

---

## 一、3D 模型与纹理资产交接

| 序号 | 资产名称 | 相对路径 | 资产用途与语义角色 | 格式 | 尺寸/多边形面数 | 轴心/原点 (Pivot) | 朝向规则 | 绑定贴图/色彩空间 | 导入建议设置 (2号执行) | 来源与许可协议 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | `George.fbx` | `Assets/Art_ThirdParty/Quaternius_Mech/George.fbx` | **玩家机甲样板**（重型双足漫步者） | FBX (含 Armature 骨骼) | 高度约 2.2m<br>面数: 2,416 tris | 底部脚掌中心 `(0, 0, 0)` | 正前方朝向 `+Z` 轴 | `George_Texture.png`<br>sRGB (Color) | Model: Scale 1.0, Normals: Import, Tangents: Calculate, Read/Write: False<br>Rig: Generic | [OpenGameArt: Animated Mech Pack](https://opengameart.org/content/animated-mech-pack)<br>作者: Quaternius<br>协议: **CC0 1.0 Universal** |
| **2** | `Mike.fbx` | `Assets/Art_ThirdParty/Quaternius_Mech/Mike.fbx` | **重型前锋敌人**（钝角突进机甲） | FBX (含骨骼) | 高度约 2.0m<br>面数: 2,832 tris | 底部脚掌中心 `(0, 0, 0)` | 正向 `+Z` | `Mike_Texture.png`<br>sRGB (Color) | 同上，Generic 骨骼 | Quaternius, CC0 1.0 |
| **3** | `Stan.fbx` | `Assets/Art_ThirdParty/Quaternius_Mech/Stan.fbx` | **突击/游击敌人**（长管锐角机体） | FBX (含骨骼) | 高度约 1.8m<br>面数: 1,940 tris | 底部脚掌中心 `(0, 0, 0)` | 正向 `+Z` | `Stan_Texture.png`<br>sRGB (Color) | 同上，Generic 骨骼 | Quaternius, CC0 1.0 |
| **4** | `Leela.fbx` | `Assets/Art_ThirdParty/Quaternius_Mech/Leela.fbx` | **轻量蜂群敌人**（三足侧翼骚扰机） | FBX (含骨骼) | 高度约 1.5m<br>面数: 1,728 tris | 底部脚掌中心 `(0, 0, 0)` | 正向 `+Z` | `Leela_Texture.png`<br>sRGB (Color) | 同上，Generic 骨骼 | Quaternius, CC0 1.0 |
| **5** | `Column_Astra.fbx` | `Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Column_Astra.fbx` | **场地重型立柱**（掩体与边缘边界） | FBX (静态网格) | 高度 6.0m, 直径 1.8m<br>面数: 480 tris | 底部中心 `(0, 0, 0)` | 竖直立向 `+Y` | `T_Trim_01_BaseColor.png`<br>sRGB (Color) | Model: Scale 1.0, Generate Colliders: True (Box/Capsule) | [OpenGameArt: Modular SciFi MegaKit](https://opengameart.org/content/modular-sci-fi-megakit)<br>作者: Quaternius<br>协议: **CC0 1.0 Universal** |
| **6** | `WallAstra_Straight.fbx` | `Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/WallAstra_Straight.fbx` | **场地防护墙体**（防爆外廊） | FBX (静态网格) | 宽 3.8m, 高 4.0m<br>面数: 360 tris | 底部几何中心 `(0, 0, 0)` | 墙面法向沿 `+Z` | `T_Trim_01_BaseColor.png`<br>sRGB | Generate Colliders: True (Box) | Quaternius, CC0 1.0 |
| **7** | `Prop_Crate3.fbx` / `Prop_Crate4.fbx` | `Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/` | **战术军需箱堆**（场地障碍点缀） | FBX (静态网格) | 1.4m x 1.4m x 1.2m<br>面数: 160 tris | 底部中心 `(0, 0, 0)` | 对称结构 | `T_Trim_01_BaseColor.png`<br>sRGB | Generate Colliders: True | Quaternius, CC0 1.0 |
| **8** | `Prop_Barrel_Large.fbx` | `Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Prop_Barrel_Large.fbx` | **危险重型燃料桶**（障碍物点缀） | FBX (静态网格) | 高 1.6m, 直径 1.0m<br>面数: 220 tris | 底部中心 `(0, 0, 0)` | 竖直立向 `+Y` | `T_Trim_01_BaseColor.png`<br>sRGB | Generate Colliders: True (Cylinder/Capsule) | Quaternius, CC0 1.0 |

---

## 二、基础材质与着色器参数分配表 (URP PBR Material Matrix)

所有材质均采用 URP 标准内置 Lit 着色器 (`Universal Render Pipeline/Lit`)：

| 材质标识 (Material ID) | 建议命名 | 赋着目标 | Base Map (颜色/贴图) | Metallic (金属度) | Smoothness (平滑度) | Emission (自发光) | Surface Inputs 备注 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **M-01** | `M_Precast_Concrete` | 地面网格 (`Industrial_Concrete_Floor`) | 单色 `#383D46`<br>`Color(0.25, 0.28, 0.33, 1.0)` | `0.08` | `0.25` (哑光) | 无 | 地面禁止高反光，平滑度严控 ≤0.30 |
| **M-02** | `M_Player_HeavyMech` | 玩家机甲 (`George`) | `George_Texture.png`<br>色相偏灰白冷调 | `0.28` | `0.48` (半哑光) | 传感器眼部 `#00E5FF` (强度 1.5) | 受击时通过脚本驱动 `_EmissionColor` 短暂全白 |
| **M-03** | `M_Enemy_Vanguard` | 重型前锋敌人 (`Mike`) | `Mike_Texture.png`<br>色相偏深铸铁暗灰 | `0.22` | `0.38` (粗糙) | 散热格栅 `#FF4500` (强度 1.2) | 形成敌对暗色群落感 |
| **M-04** | `M_Enemy_Striker` | 突击游击敌人 (`Stan`) | `Stan_Texture.png`<br>色相偏战术灰黑 | `0.25` | `0.40` | 光学瞄具 `#FF4500` (强度 1.5) | 与 Mike 保持同色系不同外轮廓 |
| **M-05** | `M_Enemy_Flanker` | 蜂群侦察敌人 (`Leela`) | `Leela_Texture.png`<br>色相偏深灰金属 | `0.20` | `0.35` | 核心指示 `#FF4500` (强度 1.0) | 体积小、数量多，控制反射消耗 |
| **M-06** | `M_SciFi_Architecture`| 场景柱体、墙体、掩体箱 | `T_Trim_01_BaseColor.png`<br>`T_Trim_01_ORM.png` | `0.32` | `0.42` | 警示黄色边缘条带条纹 | 统一 Trim 贴图复用，降低 DrawCall |

---

## 三、四大核心战斗动效与特效交互契约 (Combat Feedback Handoff)

| 事件类型 | 触发条件 | 视觉表现形式与挂点 | 空间模式 | 持续时间 (Duration) | 停止与回池重置规则 (Pooling Contract) | 关联音效语义 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1. 普通射击 / 攻击** | 武器 CD 结束，自动选敌发射 | **挂点**：机甲武器炮管口 (`Muzzle_Socket`)。<br>**形式**：极细青白激光束/线段弹道，炮口 1 帧菱形闪光（`Scale: 0.3m`）。 | World Space (射弹沿目标方向飞行) | 闪光: `0.04s`<br>射弹飞行: `0.30s` (速度 35m/s) | 命中敌人或超出 20m 射程后立即 `SetActive(false)` 并返还对象池。禁止在飞行中销毁 GameObject。 | 干燥金属敲击与高频电脉冲 (Transient Clang) |
| **2. 敌人受击 (Impact)** | 敌人受到伤害逻辑结算 | **挂点**：敌人模型受击根节点。<br>**形式**：材质瞬间闪白（Material Property Block 驱动 `_EmissionColor` 至纯白，耗时 0.05s 线性归零）；局部弹开 3 粒微型几何碎片。 | Local Space (随受击目标微位移) | 闪白: `0.06s`<br>微粒: `0.18s` | 闪白通过协程或 Tween 驱动，动画结束必须将 `_EmissionColor` 还原为初始警戒橙色。 | 短促陶瓷敲击声 (Ceramic Snap) |
| **3. 手动闪避 (Dodge)** | 玩家按下 Space / Dodge 键 | **挂点**：玩家机体外壳。<br>**形式**：尾部推进器喷射青蓝瞬态光束；在冲刺路径留下 2 具半透明切片残影（Mesh Snapshot），Alpha 从 0.5 线性渐变至 0。 | World Space (残留在原地) | 残影渐变: `0.15s`<br>总冲刺表现: `0.20s` | 残影 MeshRenderer 在 0.15s 后停用并回收。不能阻挡摄像机视线或物理射线。 | 低频气压释放与薄金属刷过声 (Whoosh Release) |
| **4. 敌人死亡 (Death)** | 敌人生命值归零 | **挂点**：敌人死亡世界坐标。<br>**形式**：机体碰撞体立即关闭；模型拆分为 3 块主要装甲碎片，带初始向外微小随机冲量弹开，落地后 0.4s 内整体缩放为 0 消失。 | World Space | `0.50s` 整体消隐完毕 | 碎片完全透明后重置 LocalPosition/LocalRotation，将整个 Enemy Prefab 状态重置归池。严禁产生长时间残留物。 | 机械解体与闷响短共鸣 (Damped Thud) |

---

## 四、能力缺项与后续迭代说明

1. **动画状态机 (Animator / Rig)**：
   - 当前 FBX 均内含完整 Armature 骨骼与 Run / Walk / Shoot 基础动作片段。S1 灰盒阶段 2 号先以 Generic 骨骼绑定或代码驱动位移；完整 Animator 状态融合计划在 S2 视听联合样板中正式展开。
2. **粒子与着色器特效 (VFX & Custom Shaders)**：
   - 当前受击闪白采用 Unity 官方推荐的 `MaterialPropertyBlock` 机制实现，无外部插件依赖；
   - 闪避残影使用通用 MeshRenderer 材质 Alpha 褪色处理；
   - 复杂 GPU 粒子（如能量爆燃、AOE 危险红圈动态扫掠 Shader）列为 S2 迭代项，S1 阶段严禁用未经审查的高消耗粒子包替代。
