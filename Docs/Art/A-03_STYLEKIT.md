# A-03 会共振的雕塑：S2 风格素材包说明 (A-03 Sculpture StyleKit)

> 2026-10-12：Codex已在Unity整合并修正：封闭倒角和面朝向、平面法线/UV、重复生成时的持久网格引用、脚底轴心和1.8/1.4米高度。下文原参数/效果描述是作者设计提案；实际装配与证据以Docs/Verification/S2-StyleLab为准。

**交付版本**：S2_StyleKit_v0.1  
**交付责任人**：主美  
**装配责任人**：2 号（P-03 装配与 S2_StyleLab 样板制作）  
**基准分支**：`origin/main` (`8e5fe719db2e1d57468e5f451be3e7732e7ea797`)  
**资产目录**：`Assets/Art/S2/`（含 Models、Materials、Prefabs 及 Editor 生成工具）

---

## 一、艺术母题与形体规范 (Design Philosophy)

本套风格包彻底替代 S1 占位用的 Quaternius 工业机甲，严格落实用户已确认的艺术总纲：
- **形体母题**：**偏心、开缝、分节**。主体由精密切割的哑光多面体构成，外甲与核心构件之间留有深色负空间缝隙，消除一切复杂贴图与风化噪波；
- **材质法则**：**细腻哑光陶瓷/涂层为主，局部半哑光金属与光学发光**。彻底根除油腻塑料高光与依赖 Bloom 掩盖瑕疵的假质感；
- **尺寸与轴向硬契约**：
  - 1 Unity 单位 = 1.0 米；
  - 根节点朝向正前方 `+Z`，竖直朝上 `+Y`；
  - 脚底接触中心为原点 `(0, 0, 0)`，根缩放严格保持 `(1, 1, 1)`；
  - 玩家总高度 **1.80m**，敌人总高度 **1.35m**，地面单元为 **6.0m x 6.0m** 标准倒角模块。

---

## 二、资产清单与节点层级表 (Asset & Hierarchy Manifest)

全部资源已在 Unity 6 URP 实际生成并持久化存储（带完整 `.meta` 与 GUID）：

| 资产名 | 路径 | 格式/尺寸 | 面数 | 材质绑定 | 挂点与节点层级说明 (供 2 号动效驱动) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`RS_Player.prefab`** | `Assets/Art/S2/Prefabs/RS_Player.prefab` | Prefab<br>高 1.80m, 宽 0.90m | 144 tris (极简纯净) | `M_S2_CeramicWhite`<br>`M_S2_SatinMetal`<br>`M_S2_Core_Cyan` | **根节点**：`RS_Player` (Pos 0, Rot 0, Scale 1)<br>└── **`MotionRoot`** (承载整体位移与回弹)<br>    ├── **`Core`** (偏心主体切块，Y=0.75m)<br>    ├── **`Segment_Upper`** (悬浮上甲罩，Y=1.35m)<br>    ├── **`Segment_FinL`** (左悬浮偏心导流片，X=-0.38m)<br>    ├── **`Segment_FinR`** (右非对称悬浮分节，X=0.34m)<br>    ├── **`SensorVisor`** (前方青蓝光学视窗，Y=1.28m, Z=0.34m)<br>    └── **`Muzzle_Socket`** (武器射弹挂点，Z=0.45m, Y=0.90m) |
| **`RS_Enemy.prefab`** | `Assets/Art/S2/Prefabs/RS_Enemy.prefab` | Prefab<br>高 1.35m, 宽 0.76m | 96 tris | `M_S2_GraphiteDark`<br>`M_S2_SatinMetal`<br>`M_S2_Alert_Orange` | **根节点**：`RS_Enemy` (Pos 0, Rot 0, Scale 1)<br>└── **`MotionRoot`**<br>    ├── **`Torso`** (钝角六棱柱躯干，Y=0.60m)<br>    ├── **`Segment_BladeL`** (左前冲刺切刃，X=-0.28m, Z=0.15m)<br>    ├── **`Segment_BladeR`** (右前冲刺切刃，X=0.28m, Z=0.15m)<br>    └── **`AlertCore`** (警戒核心，朱橙发光，Y=0.85m, Z=0.22m) |
| **`RS_Floor_Tile_6x6.prefab`** | `Assets/Art/S2/Prefabs/RS_Floor_Tile_6x6.prefab` | Prefab<br>6m x 6m x 0.25m | 24 tris | `M_S2_FloorSlab` | 边缘设计了 0.08m 结构倒角下凹，多块无缝平铺时产生自然的冷阴影分界缝隙，无需拼贴纹理。 |

---

## 三、三族材质母版参数 (URP Lit Material Matrix)

所有材质均配置为 URP Lit 物理着色，无外部纹理依赖，纯粹依靠几何法线与 PBR 光影塑造雕塑感：

| 材质文件 | 语义角色 | Base Color (RGB) | Metallic | Smoothness | Emission (自发光) | 视觉特性与去油腻控制 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`M_S2_CeramicWhite.mat`** | 玩家雕塑外甲 | `(0.88, 0.90, 0.92)` 瓷白 | 0.05 | **0.28** (细腻哑光) | Off | 类似精细打磨的未上釉白瓷，高漫反射但绝无刺眼白斑高光。 |
| **`M_S2_GraphiteDark.mat`** | 敌人雕塑主体 | `(0.12, 0.13, 0.15)` 石墨冷灰 | 0.15 | **0.32** (暗调哑光) | Off | 沉重的暗色雕塑体块，与瓷白形成鲜明的敌我明暗二元反差。 |
| **`M_S2_SatinMetal.mat`** | 悬浮分节/机械骨架 | `(0.38, 0.40, 0.44)` 结构中灰 | 0.65 | **0.45** (半哑光金属) | Off | 表达开缝内层的精密机械衬底与导轨。 |
| **`M_S2_FloorSlab.mat`** | 地面基底大板 | `(0.18, 0.20, 0.24)` 深冷灰 | 0.08 | **0.22** (超低哑光) | Off | 彻底吸光平整，为全场角色与特效留出最干净的对比背景。 |
| **`M_S2_Core_Cyan.mat`** | 玩家信息准星 | `(0.00, 0.85, 1.00)` 激光青蓝 | 0.00 | 0.50 | HDR `(0, 1.8, 2.2)` | 55° 斜俯视正交视角下的常驻移动焦点。 |
| **`M_S2_Alert_Orange.mat`** | 敌方威胁核心 | `(1.00, 0.28, 0.00)` 工业朱橙 | 0.00 | 0.50 | HDR `(2.5, 0.65, 0)` | 敌方冲刺与攻击意图指示。 |

---

## 四、升级卡 UI 样板规范 (Level-Up Card Layout Spec)

供 2 号在 P-03 中制作三选一面板（`RS_LevelUpCard`）：

1. **几何形体与切角**：
   - 采用 **45° 倒角切角矩形**（宽 260px，高 380px，长宽比约 1 : 1.46）；
   - 切角位于右上角与左下角（倒角深 16px），与机体雕塑语言保持统一切分；
2. **色彩与材质分层**：
   - **底板（Backing）**：深石墨半透磨砂底 `#14161AEB`（Alpha ~92%）；
   - **边框（Border）**：默认常态为 1px 冷白细线 `#D8DEE966`；
   - **选中高亮（Focused/Hover）**：边框宽度变为 2px，颜色转为战术暖金 `#F2A900`，伴随卡片微上浮 8px；
3. **排版与留白比例**：
   - **顶部 25%**（高 95px）：抽象几何武器/技能符号（青白矢量线框）；
   - **中部 25%**（高 95px）：技能名称（TextMeshPro 无衬线粗体，字号 20pt，纯白 `#FFFFFF`）；
   - **下部 40%**（高 150px）：数值强化描述与标签（字号 14pt，浅冷灰 `#A0A6B2`）；
   - **底部 10%**（高 40px）：段落切片式等级指示器（Segmented pips：`[ ■ ■ □ □ ]`）。

---

## 五、三大战斗动作语法与幅度建议 (Combat Animation Grammar)

动效通过代码或 Tween 驱动子节点（**严禁修改根 GameObject 的 Transform 与碰撞**），严格遵循“**短收束 → 错位展开 → 阶梯复位**”：

### 1. 普通攻击与命中反馈 (Shot & Impact)
- **玩家攻击发射 (Shot)**：
  - `Muzzle_Socket` 瞬间触发青白线段射弹；
  - `MotionRoot` 产生 1 帧微后坐（向后 -0.06m），悬浮片 `Segment_FinL/R` 同步微后扬 4°；
  - 耗时 **0.08s** 弹性复位（Elastic Ease-Out）。
- **敌人普通受击 (EnemyHit)**：
  - 敌人外甲高频闪白 1 帧（`_EmissionColor` 闪至纯白，0.05s 线性降回）；
  - 躯干 `Torso` 沿受击方向产生微幅倾斜（3°~5°），刃部 `Segment_Blade` 发生 0.04m 的张合错位；
  - 耗时 **0.12s** 平滑回弹复位。

### 2. 手动闪避动作 (Dodge)
- **第一阶段（瞬间收束，0.00s~0.04s）**：
  - 玩家按下 Space，`Segment_Upper` 与双侧悬浮片向内合拢收束（Scale 缩减 5%，紧贴 Core 主体），表现蓄力流线形态；
- **第二阶段（错位喷射，0.04s~0.15s）**：
  - 机体沿移动向量高速冲刺；
  - 尾部留下 2 具半透明切片残影（Mesh Snapshot）；
  - 悬浮片向外错位展开至最大角（左翼外展 15°，右翼外展 10°，形成明显的速度破风剪影）；
- **第三阶段（气压缓冲复位，0.15s~0.25s）**：
  - 到达目标点，机体前向微俯角下压，随后平滑悬浮复位回常态。

### 3. 精英敌人击杀强调 (Elite Kill / Destruction)
- **第一阶段（核心过载，0.0s~0.15s）**：
  - 击杀判定生效，`AlertCore` 亮度瞬间暴增至 HDR x5，伴随高频震颤（震幅 0.03m）；
- **第二阶段（错位解体，0.15s~0.40s）**：
  - 躯干 `Torso` 与双刃 `Segment_Blade` 之间的结构连接断开，3 块装甲向外斜上方抛射弹开（初速度约 4m/s）；
  - 伴随极少量几何薄片剥落，不使用任何传统火球浓烟；
- **第三阶段（坍缩消隐，0.40s~0.60s）**：
  - 碎片落地并在 0.2s 内均匀缩放至 0 消失，随后节点复位归池。

---

## 六、给 2 号的装配指引与工具 (Handoff to 2)

1. **一键生成工具**：
   - 位于 `Assets/Art/S2/Editor/SculptureKitBuilder.cs`；
   - 顶部菜单：`Art -> S2 -> Build Sculpture StyleKit (Models, Materials, Prefabs)`；
   - 具备完全幂等性，任何时候点击都会重新在 `Assets/Art/S2/` 重新构建并持久化全部网格与 Prefab。
2. **预制体直接引用**：
   - 玩家视觉装配：将 `Assets/Art/S2/Prefabs/RS_Player.prefab` 挂载在玩家表现根节点；
   - 敌人视觉装配：将 `Assets/Art/S2/Prefabs/RS_Enemy.prefab` 挂载在敌人表现根节点；
   - 地面铺设：使用 `Assets/Art/S2/Prefabs/RS_Floor_Tile_6x6.prefab` 进行平铺拼装。
3. **残影快照支持需求 (供 Codex 协同)**：
   - `RS_Player` 由 `Core`、`Segment_Upper`、`Segment_FinL`、`Segment_FinR` 等多个独立 MeshFilter 组成；
   - 建议 Codex 在更新 `VfxDirector` 的多网格残影捕获时，通过遍历 `GetComponentsInChildren<MeshFilter>()` 统一生成残影快照。
