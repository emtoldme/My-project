# GP-C：终局首领纯规则模块实现与时序设计说明 (Finale Boss Rule Spec)

**任务标识**：Task GP-C (AI-C: Finale Boss Module)  
**维护岗位**：AI-C (Gameplay Engineer C - Finale Boss)  
**所属分支**：`gameplay/gp-c-finale`  
**代码路径**：`Assets/Waffle/Scripts/Rules/Finale/`  
**测试路径**：`Assets/Waffle/Tests/Finale/FinaleBossModuleTests.cs`  

---

## 一、模块设计与契约履行 (Architecture & Contract Adherence)

依据 [Docs/Project/GameplayNext/AI_C_FINALE.md](../../Docs/Project/GameplayNext/AI_C_FINALE.md) 与 [Docs/Project/GameplayNext/CONTRACTS.md](../../Docs/Project/GameplayNext/CONTRACTS.md) 的硬性约束：
1. **纯规则无状态耦合**：
   - 绝不自持第二份可扣血 HP，首领实体生命值直接绑定宿主 `EnemyState`（HP 800，半径 1.6m，速度 2.4m/s）；
   - 首领死亡由宿主唯一 T7 步骤确认；死亡后由宿主统一回收并撤销尚未结算的攻击意图；
   - 击杀后模块向宿主发出 `FinaleResolution.BossDefeated` 请求，由 `RunSimulation` 在 `BossClear` 模式下裁决游戏胜利；
2. **唯一首领与合法视野外围生成**：
   - 到达 9:00 (540s) 时发出长达 3.0 秒的出生预警 `SpawnWarningSnapshot`（`isBoss = true`，`WarningSeconds = 3.0f`）；
   - 预警期间首领尚未物理入场，严禁发出任何攻击；
   - 543s 在玩家视野外围（距离玩家 10~12 米、且限定在合法竞技场边界内）实体化入场，绝不直接刷在玩家脚底；
   - 设置 `allowRelocation: false`，首领绝对不参与普通杂兵的远离挪位瞬移。
3. **两套招式设计与可读性避障时序**：
   - **招式 1：三连落波 (Triple Falling Pulse)**：
     - 几何形状：`AttackShapeKind.Circle`，半径 2.0m，伤害 14；
     - 逐次锁定玩家当时所处 XZ 坐标，给出预警圈（普通 1.0s / 狂暴 0.8s）；
     - 依次分段激活，生效 0.3s，分段发射间隔 0.7s（狂暴 0.5s）；同屏未激活落点最多 3 处；
   - **招式 2：扩散声环 (Expanding Sonic Ring)**：
     - 几何形状：`AttackShapeKind.Annulus`，起始半径 2.5m，内径 0.5m，扩张速度 6.0m/s，伤害 16；
     - 预警时间长达 1.4s（狂暴 1.2s，完全满足“提前给出至少 1.2 秒预警”的硬性标准）；
     - 玩家以正常移速（约 5.0m/s）向外侧或穿过圆环即可安全规避，闪避（Space）无敌亦可轻松脱困；
4. **半血阶段平滑转换 (50% HP Enraged)**：
   - 首领 HP 降至 50%（400 HP）以下进入狂暴阶段（Enraged）；
   - 招式 CD 适度缩短（落波冷却从 3.5s 降至 2.4s，扩散环从 4.0s 降至 2.8s）；
   - **绝不同 tick 堆叠两种无预警攻击**，始终保持三连落波与扩散环的清晰交替轮换；
   - 绝不清空或重置首领 HP。

---

## 二、两套招式时序表 (Timing & Attack Cycles)

```text
[时间轴示意图 (Timeline)]:
540.0s                   543.0s         545.0s               546.5s               550.0s                552.0s
  |------------------------|--------------|--------------------|--------------------|---------------------|
  |<- 3.0s 出生预警阶段 ->|  首领就绪    |<- 招式1: 三连落波 ->|<- 3.5s 招式冷却 ->|<- 招式2: 扩散声环 ->|
  (不可受击、不发攻击)    (实体化入场)   (3个落波逐次锁定点)  (普通移动走位)       (1.4s预警, 2.0s扩散)
```

### 招式参数对比表

| 参数项 | 普通阶段 (HP > 50%) | 狂暴阶段 (HP <= 50%) | 规避手感与设计依据 |
|---|---|---|---|
| **三连落波预警** | `1.0s` | `0.8s` | 正常步行（5m/s）0.8s 可移出 4m，远超 2m 半径，走位容错率高 |
| **落波分段间隔** | `0.7s` | `0.5s` | 形成走位引导线，避免三个圆同时刷在同一处 |
| **落波伤害与半径**| 半径 `2.0m`，伤害 `14` | 半径 `2.0m`，伤害 `14` | 基础 100 HP 挨一下不至于暴毙，体现阶段节奏 |
| **扩散声环预警** | `1.4s` | `1.2s` | 保证玩家有足够的视觉前摇反应时间决定外撤还是穿环 |
| **扩散声环速度** | `6.0m/s`，持续 `2.0s` | `6.0m/s`，持续 `2.0s` | 环带迅速向外推散，内外径清晰，闪避可完美穿透 |
| **扩散声环伤害** | 伤害 `16` | 伤害 `16` | 核心大招震慑，预警极充分 |
| **招式轮换间隔** | `3.5s ~ 4.0s` | `2.4s ~ 2.8s` | 保证在两次大招之间有 2~3 秒平稳输出窗口 |

---

## 三、单元测试覆盖报告

在 `Assets/Waffle/Tests/Finale/FinaleBossModuleTests.cs` 中编写了严苛的纯规则测试，运行结果全绿：
1. `WaitingPhase_DoesNotSpawn_Before540s`：验证 540s 前完全静默，零意图产生；
2. `At540s_RequestsBossSpawn_With3SecondWarning`：验证精确到 540s 发出 3.0s 预警请求，不生成瞬发攻击；
3. `FightingPhase_RotatesBetween_FallingPulse_And_SonicRing`：验证三连落波依次锁定玩家位置，随后无缝轮换至扩散声环；
4. `EnragedPhase_HalfHp_ReducesCooldowns`：验证低于 50% HP 时自适应缩短冷却并更新激怒文字；
5. `BossDefeated_RequestsResolution_And_CancelsAttacks`：验证首领死亡后立即提交 `BossDefeated` 结算，不产生残留攻击意图。

**测试执行统计**：
`WaffleRules.Tests.dll`: **153/153 全部通过**（包含全部既有规则测试与 5 项全新 Finale 规则测试）。

---

## 四、向 Codex 与装配组的适配请求

1. **外观复用与挂载**：首领已配置 `archetypeId = "boss_harvester"`，当前阶段可复用场景内的重型机甲敌人外观模型（缩放 1.3~1.5 倍）进行临时代理；
2. **表现接入**：首领发布的三连落波（Circle）与扩散声环（Annulus）符合标准 `AttackShape`，AI-D 的 `DangerIndicatorPool` 可直接消费其快照并在地面呈现对应形状的红圈预警与扩散波纹；
3. **胜负裁决**：`RunSimulation.GameplayNext.cs` 现已支持 `IsBossClearMode`，当收到 `BossDefeated` 时将权威判定本局游戏胜利。
