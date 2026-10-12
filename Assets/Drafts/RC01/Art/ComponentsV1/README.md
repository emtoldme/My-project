# RC02 美术表现组件与资产包说明文档 (README)

**任务标识**：feature/rc02-art-components (Docs/Integration/REVERB_TASKS_08.md)  
**维护岗位**：主美 (Lead Artist)  
**基线分支**：`origin/main` (`6447af8a96496b2c606de4e586b4ea7eaae8aaf9`)  
**交付分支**：`feature/rc02-art-components`  
**唯一写入范围**：`Assets/Drafts/RC01/Art/ComponentsV1/` 与 `Docs/Design/RC02/Art/`  
**约束声明**：严格只读上游用户原素材（`Assets/ReverbInTheRye`）、战斗规则逻辑与中央管理代码；所有材质与预制体均为 **`TEST_ONLY` 可复用组件模板**，不宣称主流商业画质达标。

---

## 一、资产包结构与包含清单

本资产包遵循 [Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md](../../../../../Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md) 合同 v1 规范，提供标准化的三大类 Prefab 模板、调优材质副本与测试卡牌图标：

```text
Assets/Drafts/RC01/Art/ComponentsV1/
├── EditorTools/
│   └── validate_art_manifest.py           # 表现清单静态合规验证工具 (零依赖)
├── Icons/
│   ├── icon_W05_chain_arc.png             # W05 弹跳电弧卡牌图标 (青蓝折线电弧)
│   ├── icon_W06_motion_echo.png           # W06 移动残响卡牌图标 (草灰移动残留点阵)
│   ├── icon_P03_pulse_drive.png           # P03 脉冲推进卡牌图标 (风速浅青推进箭头)
│   ├── icon_P05_resonance_sustain.png     # P05 延音卡牌图标 (暮紫延音波形衰减线)
│   ├── icon_P08_shock_absorber.png        # P08 减伤垫片卡牌图标 (沉铁灰螺栓加固板)
│   └── icon_P17_dodge_discharge.png       # P17 闪避放电卡牌图标 (银紫多向辐射电离)
├── Materials/
│   ├── M_VFX_DirectedPulse.mat            # 暖白麦黄定向收束脉冲材质
│   ├── M_VFX_ChainArc.mat                 # 高亮冷蓝超导电弧材质
│   ├── M_VFX_ResonanceZone.mat            # 琥珀金定点共振波纹材质
│   ├── M_VFX_MotionEcho.mat               # 灰褐移动残响尘埃材质
│   ├── M_VFX_EchoHit.mat                  # 迷幻紫红原位回声收束环材质 (#B863A0)
│   └── M_VFX_DodgeDischarge.mat           # 银紫极光闪避放电冲击环材质 (#C288E8)
└── Prefabs/
    ├── TEST_ONLY_Projectile_DirectedPulse.prefab # Projectile 模板：W01 定向脉冲
    ├── TEST_ONLY_Hit_EchoPulse.prefab            # Hit 模板：W01 击中 / P12 回声震击
    ├── TEST_ONLY_Arc_ChainElectric.prefab        # Hit/Arc 模板：W05 弹跳电弧
    ├── TEST_ONLY_Zone_ResonanceField.prefab      # Zone 模板：W04 定点共振力场
    ├── TEST_ONLY_Trail_MotionResonance.prefab    # Trail 模板：W06 移动残留节点
    └── TEST_ONLY_Zone_DodgeDischarge.prefab      # Zone 模板：P17 闪避放电冲击波
```

---

## 二、三类 Prefab 模板规范与参数控制

所有模板均严格遵循同机位（55° Pitch / 45° Yaw）斜俯视规范：
1. **根节点标准化**：
   - Root 变换必须保持单位缩放 `(1, 1, 1)`；
   - 局部正向必须严格沿 **+Z 轴朝前**（米制单位，1 unit ≈ 1m）；
2. **三类模板分类与表现特征**：
   - **Projectile（投射物）**：
     - 代表：`TEST_ONLY_Projectile_DirectedPulse.prefab`
     - 结构：细长胶囊体沿 +Z 伸展，头部为高光高能脉冲，尾部伴随平滑光带衰减；
     - 调控参数：`scale`（体量）、`tailSeconds`（拖尾消散延迟）、`intensity`（HDR 亮度）。
   - **Hit / Arc（命中与电弧连线）**：
     - 代表：`TEST_ONLY_Hit_EchoPulse.prefab`、`TEST_ONLY_Arc_ChainElectric.prefab`
     - 结构：在两点间通过 From/To 坐标拉伸网格与半透明光带，形成瞬间跳跃感；
     - 调控参数：`width`（连线/冲击波厚度）、`tailSeconds`（留存时间 0.16s）、`intensity`。
   - **Zone / Trail（区域力场与残留足迹）**：
     - 代表：`TEST_ONLY_Zone_ResonanceField.prefab`、`TEST_ONLY_Trail_MotionResonance.prefab`、`TEST_ONLY_Zone_DodgeDischarge.prefab`
     - 结构：扁平贴地圆环/多边形（高度 Y=0.05），带向外扩张与呼吸起伏，中心保留半透明视认空隙；
     - 调控参数：`width`（扩散半径）、`tailSeconds`（地面残留生命周期 2.0s~3.0s）、`intensity`。
3. **运行时与池化契约**：
   - 模板内**严禁包含 Collider、Rigidbody、Update 循环或 AudioSource**；
   - 由 2 号组装工具根据 `art-manifest.json` 中的 `maxInstances`（8~20）与 `minInterval` 集中池化，当收到 `Paused` 事件时统一执行 `freeze` 冻结。

---

## 三、2 号（装配组）刷新与消费流程

1. **导入与解析**：
   - 2 号的 Editor 工具读取 `Docs/Design/RC02/Art/art-manifest.json`；
   - 通过 `kind="art"` 与 `eventType + contentId` 完成行为映射，解析资产路径并生成对应的 ScriptableObject 表现条目；
2. **事件监听与参数绑定**：
   - 监听中央 `PresentationEvent`：
     - 收到 `Shot (W01)` 时实例化 `TEST_ONLY_Projectile_DirectedPulse`，朝向设置为 `(DirectionX, 0, DirectionZ)`；
     - 收到 `PropagationHit (W05)` 时实例化 `TEST_ONLY_Arc_ChainElectric`，起点设为 `(FromX, 0, FromZ)`，终点设为 `(X, 0, Z)`；
     - 收到 `WeaponZone (W06)` 时在玩家后方残留点实例化 `TEST_ONLY_Trail_MotionResonance`；
     - 收到 `Dodge (P17)` 时在闪避原位释放 `TEST_ONLY_Zone_DodgeDischarge` 冲击环；
     - 收到 `EchoHit (P12)` 时在受击敌人处播放紫红色 `TEST_ONLY_Hit_EchoPulse`。

---

## 四、后续替换正式美术资产步骤 (Asset Handoff Steps)

后续若有高模、商业特效或正式精修图产生，开发团队**无需修改任何 C# 代码或 CombatModule 逻辑**，仅需执行以下标准替换流程：
1. **替换材质/模型/纹理**：将正式的模型 FBX 或粒子材质放入工程目录；
2. **修改 Prefab 挂接**：在对应的 Prefab（如 `TEST_ONLY_Projectile_DirectedPulse`）内将 MeshRenderer 替换为正式粒子系统（ParticleSystem）或模型节点；
3. **更新表现清单 (art-manifest.json)**：
   - 若新增了正式资源，直接修改条目的 `assets` 路径列表；
   - 微调 `parameters` 中的 `width`、`scale` 或 `intensity` 以匹配正式画面的曝光；
4. **重新校验**：
   - 运行 `python Assets/Drafts/RC01/Art/ComponentsV1/EditorTools/validate_art_manifest.py`，确认 0 错误；
5. **在 Unity 内刷新**：点击 `Waffle -> RC02 -> Refresh Content Catalog` 重新生成 ScriptableObject，即刻全自动化生效。
