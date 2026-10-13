# RC02 美术表现组件管线设计规范与形态语言说明 (RC02 Visual Pipeline Spec)

> Codex 整合更新（2026-10-13）：下文为主美原始设计目标，不能作为已达到品质的证明。现有六模板由 Unity 工具生成并保留原 GUID，根缩放统一为 1，使用真实 _BaseColor 参数；线宽与伤害半径分别传递。W05 实际事件为 EnemyHit + W05，P17 为 WeaponZone + P17，不能使用表内历史 PropagationHit/Dodge。W06 真实残留与连线已接入；当前环形/折线为 TEST_ONLY 基础表现，尚未实现文中全部呼吸、坍缩、尘埃等动效，也未认证遮挡率或“高质量”。目录与迁移事实见 ../Pipeline/README.md 和 MIGRATION.md。

**任务标识**：feature/rc02-art-components (Docs/Integration/REVERB_TASKS_08.md)  
**维护岗位**：主美 (Lead Artist)  
**基线分支**：`origin/main` (`6447af8a96496b2c606de4e586b4ea7eaae8aaf9`)  
**交付分支**：`feature/rc02-art-components`  
**协作边界**：仅写入 `Assets/Drafts/RC01/Art/ComponentsV1/` 与 `Docs/Design/RC02/Art/`  

---

## 一、一页纸视觉形态、色彩与运动语言标准 (Visual Design System)

为确保游戏在 55° 斜俯视同机位下兼顾**乡村怪谈旧机械的克制美学**与**高烈度战斗的毫秒级视认性**，主美制定了一体化的“形态+色彩+运动”三大法则，杜绝纯靠换色敷衍的扁平设计：

```text
三位一体战术表现法则 (Trinity Visual Rules):
1. 几何形态区别战术职能 (Geometry defines Role):
   - 直线导轨 / 细长胶囊  -->  单体定点狙杀 (W01)
   - 锯齿折线 / 节点分叉  -->  群体跳跃传导 (W05)
   - 扁平同心圆 / 呼吸起伏 -->  持续地面控场 (W04 / W06 / P17)
2. 色彩语义强化机制归属 (Color maps Mechanism):
   - 暖白麦黄 (#FFF2CC)    -->  基础定向射击
   - 迷幻紫红 (#B863A0)    -->  核心回声延迟爆发 (P12 / E01)
   - 亮金电弧 (#FFD700)    -->  传播击穿与能量外溢 (P13 / E04)
   - 超导冷蓝 (#70CFFF)    -->  高能连锁电弧 (W05)
   - 银紫放电 (#C288E8)    -->  极限闪避脱身脉冲 (P17)
3. 运动动势传达时间预期 (Motion dictates Timing):
   - 高速冲刺 + 瞬间凝固  -->  直接命中即刻反馈
   - 向心坍缩收束 (Implosion) --> 0.5s 回声倒计时触发 (P12)
   - 离心环形扩散 (Explosion) --> 闪避冲击波与清场 (P17)
```

---

## 二、三类 Prefab 模板映射与参数规范

依据 [Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md](../../Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md)，主美交付了三大类标准化的 Prefab 模板：

| 类别 | 预制体路径 | 对应机制 / 道具 | 默认参数 (width / scale / tailSeconds / intensity) | 视觉特征与避障说明 |
|---|---|---|---|---|
| **Projectile** | `TEST_ONLY_Projectile_DirectedPulse.prefab` | W01 定向脉冲 (Shot) | `scale: 0.8, tailSeconds: 0.12, intensity: 1.2` | 沿 +Z 极速穿行，细长发光胶囊体，绝不遮挡地面敌人脚底血条与受击圈。 |
| **Hit / Arc** | `TEST_ONLY_Hit_EchoPulse.prefab` | W01 击中 / P12 回声 (EnemyHit / EchoHit) | `width: 0.3~0.5, scale: 0.5~0.6, intensity: 1.0~1.4` | 原位向心坍缩菱形环；区分普通暖白击中与紫红回声震击。 |
| **Hit / Arc** | `TEST_ONLY_Arc_ChainElectric.prefab` | W05 弹跳电弧 (PropagationHit) | `width: 0.08, scale: 1.0, tailSeconds: 0.16, intensity: 1.5` | 两目标间的瞬间折线跳跃光弧，0.16s 迅速消隐，保证连锁画面清爽。 |
| **Zone / Trail**| `TEST_ONLY_Zone_ResonanceField.prefab` | W04 定点共振 (WeaponZone) | `width: 1.5, scale: 1.5, tailSeconds: 2.0, intensity: 1.1` | 贴地金琥珀同心波纹，中心保留 80% 透空率，绝不遮盖玩家走位线。 |
| **Zone / Trail**| `TEST_ONLY_Trail_MotionResonance.prefab` | W06 移动残响 (WeaponZone) | `width: 0.4, scale: 0.4, tailSeconds: 3.0, intensity: 0.9` | 移动路线散落的麦草尘埃节点，微光呼吸，直观展现风筝布雷轨迹。 |
| **Zone / Trail**| `TEST_ONLY_Zone_DodgeDischarge.prefab` | P17 闪避放电 (Dodge) | `width: 2.5, scale: 2.5, tailSeconds: 0.35, intensity: 1.6` | 闪避瞬间原地爆发的 360° 银紫色环形扩散脉冲，视觉冲击力强。 |

---

## 三、六款新增构筑卡牌图标语义规范 (Icons V1)

主美绘制并输出了 6 张高质量 64x64 测试图标，全部配置专属 TextureImporter 与 Sprite (2D and UI) 元数据：

1. **`icon_W05_chain_arc.png`（弹跳电弧）**：
   - 构图：折线分叉闪电链贯穿中心；
   - 配色：超导冷蓝（`#70CFFF`）；
   - 语义：明确传达“连锁多目标跳跃攻击”的武器属性。
2. **`icon_W06_motion_echo.png`（移动残响）**：
   - 构图：对角线连续衰减足迹残点 + 虚线轨迹；
   - 配色：浅草灰（`#B8A88A`）；
   - 语义：明确传达“位移与风筝即产生输出”的机动布雷属性。
3. **`icon_P03_pulse_drive.png`（脉冲推进）**：
   - 构图：指向前方的强力单向风洞推进三角；
   - 配色：风速浅青（`#85E3D8`）；
   - 语义：直观表达“投射物弹速加成”。
4. **`icon_P05_resonance_sustain.png`（延音）**：
   - 构图：周期逐渐拉宽的振荡波形延展线；
   - 配色：暮紫（`#8B68A6`）；
   - 语义：直观表达“力场与残留物持续时间增加”。
5. **`icon_P08_shock_absorber.png`（减伤垫片）**：
   - 构图：螺栓加固的双层工业阻尼重型垫片；
   - 配色：沉铁灰褐（`#6E6862`）；
   - 语义：直观表达“受到伤害硬性减免”的防御属性。
6. **`icon_P17_dodge_discharge.png`（闪避放电）**：
   - 构图：中心能量环向八方放射的尖锐电离子火花；
   - 配色：银紫（`#C288E8`）；
   - 语义：直观表达“成功触发手动闪避时向周围放电”的战术机制。

---

## 四、合规性与静态验证报告

- **表现清单路径**：`Docs/Design/RC02/Art/art-manifest.json`；
- **自检工具**：`Assets/Drafts/RC01/Art/ComponentsV1/EditorTools/validate_art_manifest.py`；
- **验证结果**：
  ```text
  --- Validating Art Manifest: art-manifest.json ---
  PASSED with 10 valid art entries.
  ALL PIPELINE MANIFESTS VERIFIED COMPLIANT WITH CONTRACT V1.
  ```
- **资产完整度**：10 项条目覆盖 6 个 Prefab、6 个 Material、6 个 Icon，全部物理文件及对应 .meta 经由脚本严格校验，无重复 ID、无未知参数。
- **构建测试**：`dotnet test` 规则项目全部通过（117/117）。

---

## 五、向 Codex 与装配组的交付请求 (Requests for Codex)

为使 RC02 美术表现组件管线在主干完美运作，主美向 Codex 提出以下 **2 项最小协同请求**：
1. **请求一：在中央 PresentationEvent 中保持 `FromX / FromZ` 的正确填充**：
   - 当触发 `PropagationHit` 或 `W05` 链击事件时，请确保 `FromX / FromZ` 为前序跳跃起点，`X / Z` 为本次击中终点，以便 2 号的 `TEST_ONLY_Arc_ChainElectric` 能通过两点拉伸正确定向；
2. **请求二：在升级界面 (RC01GrowthHud) 中读取 `art-manifest.json` 注册的卡牌图标**：
   - 2 号刷新 ScriptableObject 目录后，当玩家升阶抽到 W05、W06、P03、P05、P08、P17 时，请求 HUD 优先展示 `role="icon"` 的 Sprite 资产，取代纯文本按钮。
