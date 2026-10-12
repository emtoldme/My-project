# A-R03 用户自制声波攻击特效包 v0.1 专业审查与接入指南 (Sonic Attack VFX v0.1 Audit & Integration Guide)

**文档属性**：主美专项视效审查交付物  
**审查对象**：用户自制并提供的声波攻击特效包 `@G:\图书馆\ReverbInTheRye_SonicVFX_v0.1.zip`  
**基准分支**：`review/sonic-vfx-v01-audit`（基于 `origin/main` @ `d143e7a`）  
**执行责任人**：主美  
**协同对象**：2 号（视效装配）、Codex（技术接线与工程自动化）

---

## 一、审查总评与核心亮点 (Executive Summary)

用户制作并提供的声波攻击反馈视效包 `ReverbInTheRye_SonicVFX_v0.1.zip` 是**项目启动以来首个真正将 Shoegaze × 迷幻音波（Psychedelic Audio）的声学质感转化为视觉形态的高水准 URP 特效资产**。

主美团队对该压缩包内的 Shader 源码、C# 驱动逻辑、Editor 安装菜单及文档进行了全方位审查，核心结论如下：
1. **彻底打破“激光/机甲枪炮”陈词滥调，完美契合《麦田里的混响》世界观**：
   - 摒弃了廉价的蓝色科幻激光与传统火球浓烟，创新性地采用**电波干涉抖动、空气折射微畸变（Analog Interference）、不规则共振环，以及金褐色的麦秸碎屑与扬尘**；
   - 视效语言与前一刻用户提供的地面材质包（麦田干土、密集麦秸）形成了极强的世界观咬合——声波不再是抽象的光斑，而是**激起麦田麦秸与尘土的物理振荡**。
2. **底层架构克制专业，具备极高的工程严谨性**：
   - **零额外依赖**：纯手写 URP HLSL Shader（`ResonancePulseURP.shader` 与 `StrawDustURP.shader`），不依赖体积庞大的 VFX Graph 或非标准 ShaderGraph 插件，对 Unity 6 URP 17.4.0 具有原生兼容性；
   - **严格遵循物理与解耦原则**：C# 表现层 `SonicAttackVFX.cs` **完全不持有任何规则状态**，不计算伤害、不判断碰撞、不触发扣血，纯粹由外部事件驱动；
   - **内置完善的对象池与防泄漏设计**：开局预热 Quad Mesh 与 ParticleSystem，动态驱动通过 `MaterialPropertyBlock` 逐实例修改参数，`OnDestroy` 显式销毁临时网格，杜绝 GC 尖峰；
   - **时间权威守规**：粒子发射关闭了自动循环（`emission.enabled = false`），完全依赖外部手工调用 `Emit()` 触发定量麦秸颗粒，生命周期完全可控。
3. **定位定性与演进关系**：
   - 该特效包被作者明确标注为 **`TEST_ONLY`（高保真功能性原型）**；
   - 它与当前正在进行的 **M01-R1（单目标回声与传播）** 具有高度协同互补性：其发射环、命中电波脉冲与扬尘粒子，可直接为 2 号提供最核心的视效底层母版！

---

## 二、Shader 核心技术剖析与参数调优建议 (Shader Deep Dive & Tuning)

### 1. `ResonancePulseURP.shader`（声波共振脉冲着色器）
- **实现原理**：
  在单个 XZ 四边形面片（Quad）上，通过极坐标运算（`atan2(p.y, p.x)`）与高频正弦叠加（`sin(17θ)` + `sin(53θ)`），计算出带模拟无线电高频抖动（Jitter）与边缘扰动的共振环，并通过 `lerp(ring, core, _CoreMix)` 支持从中心暖白亮斑向外圈干涉环的平滑过渡。
- **渲染状态配置**：
  - `Tags: "Queue"="Transparent" "LightMode"="SRPDefaultUnlit"`；
  - `Blend SrcAlpha One`（经典加色半透明混合）；
  - `ZWrite Off`、`ZTest LEqual`、`Cull Off`。
- **主美调优与防穿模建议**：
  - **高度 Z-Fighting 防范**：Quad 贴地高度建议通过代码抬高 **`Y = 0.03m ~ 0.05m`**，避免与带有法线起伏的地面材质发生深度交错闪烁；
  - **加色混合过绘控制**：`Blend SrcAlpha One` 在暗调地面上极为通透，但若多个环重叠容易局部过曝。建议在 `SonicAttackVFX` 中保持 `_Opacity` 在 `0.8 ~ 1.0` 之间，不盲目调高 HDR 颜色倍率。

### 2. `StrawDustURP.shader`（麦秸碎屑与灰尘着色器）
- **实现原理**：
  极简高效的椭圆衰减（Elliptical falloff）Unlit 着色器，结合 ParticleSystem 的 `startSizeX (0.018m)` 与 `startSizeY (0.17m)`，自动将公告板粒子拉伸成**细长如麦芒/秸秆的扁平碎片**。
- **渲染状态配置**：
  - `Blend SrcAlpha OneMinusSrcAlpha`（标准 Alpha 混合）；
  - 颜色渐变内置了淡入淡出（`0.0 -> 1.0 -> 0.65 -> 0.0`），颗粒随重力（`gravityModifier = 0.24`）自然下坠沉降。
- **表现评价**：
  极具巧思！用极其廉价的计算量完美模拟了“冲击波扫过麦田卷起飞扬碎麦草”的泥土质感，完全摒弃了传统科幻游戏那种光滑塑料方块的廉价感。

---

## 三、三阶段视觉节奏与色彩叙事契合度 (Phase Review & Color Narrative)

| 阶段 / 表现 | 视觉特征与粒子行为 | 强调色与色号 (Hex) | 脉冲时长与节奏 | 与主美 A-R01 四色叙事的契合度评价 |
| :--- | :--- | :--- | :--- | :--- |
| **发射 (Fire)** | 暖白调频器核心 + 互相干涉的双圈脉冲 + 7 粒青色电波微粒沿射击方向喷射 | 激光青蓝 `#77D9D4`<br>蓝紫 `#9780C2` | **0.0 ~ 0.18s**<br>(余晖至 0.4s) | **高度契合**：激光青蓝完全符合此前规划的“主角广播接收与发射”信息色，射弹方向感极强，且 0.18s 极速消隐，不遮挡前方走位视线。 |
| **命中 (Hit)** | 暖白中心微闪光 + 紫红/蓝紫冲击环 + 11 粒金褐色麦秸尘粒溅射 | 暖白 `#F1E6C7`<br>迷幻紫红 `#B863A0`<br>麦秸金 `#B89565` | **0.0 ~ 0.30s**<br>(尘粒沉降至 0.7s) | **高度契合**：采用 `#B863A0`（迷幻紫红），既拉开了与常规直接命中的差异，又绝不侵占朱红（`#FF3B30`）危险报警色；麦秸飞散极大地增强了击中反馈的实在感。 |
| **死亡 (Death)** | 三圈不规则共振环 + 短暂错位震荡 + 23 粒麦秸灰尘向外爆开并下沉 | 紫红、蓝紫、青蓝、麦草金混合 | **0.0 ~ 0.70s**<br>(消散至 1.05s) | **层次丰富**：表现出异常信号的崩溃解体感，麦秸漫天飞舞后归于沉寂，带有强烈的乡村怪谈孤寂氛围。 |

---

## 四、如何接入 M01-R1 机制试玩（给 2 号与 Codex 的协同建议）

当前 2 号正在执行 `M01-R1`（让回声的作用看得懂），用户提供的这套特效可以作为**最理想的技术组件**，直接注入 M01 独立试玩场景中：

### 1. 对应 M01-R1 分镜的职责分工
- **直接命中 (Shot / Direct Hit)**：
  - 调用 `vfx.PlayFire(muzzlePos, forward)` 驱动玩家射击；
  - 目标受击时调用 `vfx.PlayHit(hitPos)` 产生紫红微环与麦秸飞溅。
- **0.5 秒延迟回声 (Delayed Echo)**：
  - 在 0.5s 回声到期时，在原目标位置调用 `vfx.PlayHit(enemyPos)`，并配合弹出 `回声 -5` 跳字；
  - 该共振环原生支持向心与波动抖动，正好呈现“原位二次震荡爆发”！
- **目标消灭 (Enemy Killed)**：
  - 敌人死亡回池前调用 `vfx.PlayDeath(enemyPos)`，激发出漫天飞舞的麦秸尘埃，形成击杀强高潮。

### 2. 避免叠播与事件冲突的关键契约
- **独占单通道原则**：
  在 M01 场景中，若启用了本组件 `SonicAttackVFX`，必须确保原有的临时方块粒子或旧版波纹处于**禁用状态**，严禁同屏同时触发两套命中粒子导致 DrawCall 激增；
- **暂停与时钟冻结**：
  当前 `SonicAttackVFX.cs` 在 `Update()` 中使用了 `Time.deltaTime`。在接入 GameLoop 权威时序时，建议将其改为读取规则步长（或在暂停时停止 Update 遍历），确保按 Esc 暂停时空中悬浮的麦秸与电波环绝对静止。

---

## 五、目录部署建议 (Handoff to Codex)

建议 Codex 在集成时将文件解压并放置于如下规范目录：
- 脚本：`Assets/Experiments/Mechanism/Presentation/SonicVFX/SonicAttackVFX.cs`
- 着色器：`Assets/Experiments/Mechanism/Presentation/SonicVFX/Shaders/`
- 材质：`Assets/Experiments/Mechanism/Presentation/SonicVFX/Materials/`
- Editor 菜单：`Assets/Experiments/Mechanism/Presentation/SonicVFX/Editor/`

本特效包设计克制、表现惊艳、无冗余依赖，主美强烈建议 2 号与 Codex 尽快将其引入 M01 试玩工程，让玩家在全屏模式下真正体验到麦田里被音波震颤激荡的极致视听！
