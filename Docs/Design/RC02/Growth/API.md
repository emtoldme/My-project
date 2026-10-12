# RC02 规则组件 API（08A，1号）

代码：`Assets/Waffle/Scripts/Rules/RunContent/Components/`（命名空间 `Waffle.RunContent.Components`）。目录在 `Rules/` 下，**无 asmdef，随 Waffle.Rules 编译**（noEngineReferences，无 UnityEngine）；复用同程序集的 `Waffle.Combat.Vec2`。`Tools/RulesTests` 的通配会一并编译它。无 Unity，本批**未在 Unity 中编译/运行**。

## 1. 组件一览（触发→目标→载体→修饰）

| 层 | 类型 | 作用 |
|---|---|---|
| 触发 | `CooldownTrigger` | 冷却；无目标保持就绪（同旧武器） |
| 触发 | `DistanceAccumulator` | 只累计合法位移；超过 `MaxLegalStep` 视为传送不计；每 tick 最多 2 次，多余距离作废 |
| 触发 | `DodgeTrigger` | 只认宿主通知的真实闪避，独立节流 |
| 目标+载体 | `ChainArcCarrier` | 有限链击：最近未访问目标（等距按 serial），同链 serial+generation 去重，深度≤6、预算≤8，阶5第2跳分叉 |
| 载体 | `ResidualZoneField` | 残留点：上限≤12（目录 max）、寿命、同目标跨点节流、每 tick ≤24 命中、可连线（0.4m）、引爆最近 N 点 |
| 修饰 | `StatModifiers` | 唯一从 `GrowthStats` 取倍率；命令里数值已乘 |
| 装配 | `RunComponentRuntime` | 每局一个；按武器槽/被动阶读取目录，输出 `RuleCommand` |

## 2. 宿主调用（阶段固定）

```csharp
// 开局（InitGrowth 之后，第一次 StepCombat 之前）
components = new RunComponentRuntime(Growth);            // 每局 new；重开即清空
components.RebaseMovement(new Vec2(PlayerX, PlayerZ));
// 成长选择被接受后（与 Combat.RefreshRunContent 同处）
components.Refresh();
// T1：只在闪避真正发动的分支（发 Dodge 事件处）
components.NotifyDodge(new Vec2(PlayerX, PlayerZ));
// T6 之后、T7 之前（CombatModule 现有 AfterProjectiles/ResolveRunPending 位置）
snap.Clear(); foreach (var e in Enemies) if (e.IsAlive) snap.Add(new EnemySnapshot(e.Serial, e.Generation, e.Position, e.Radius, e.IsElite));
cmds.Clear();
components.Advance(new ComponentAdvanceInput { Now = now, Dt = dt, PlayerPos = player,
    MaxLegalStep = Math.Max(moveSpeed, dodgeSpeed) * dt * 1.25f }, snap, cmds);
foreach (var c in cmds) Execute(c);                      // 见第 4 节；随后 T7 照常结算死亡/掉落
// 调试传送/复活：RebaseMovement(pos)；暂停/LevelUp：不调用 Advance；终局：丢弃实例或 Clear()
```

- `Advance` 不推进任何别的时钟，不改敌人；`Now/Dt` 必须是规则 Playing 时钟。
- `NotifyDodge` 在 T1 缓存，下一次 `Advance` 消费；按键失败、冷却中、暂停中都不得调用。
- 组件不存 HP/XP/击杀；奖励只由 CombatModule T7 发一次。

## 3. `RuleCommand` 字段

| 字段 | 含义 |
|---|---|
| Kind | `TargetDamage`（单目标）/`AreaDamage`（圆，最多 BudgetLeft 个）/`FireProjectile`（W01 适配）/`ZoneSpawned`（纯表现） |
| ContentId, Slot, RootId, ChainId | 来源；被动 Slot=-1；ChainId 同一次 W05 释放相同 |
| Serial, Generation | TargetDamage 目标；宿主用 `TryGetEnemy` 校验，失效丢弃 |
| Position, From | 命中点/圆心；From=上一跳位置（电弧）；FireProjectile 时 From=方向 |
| Damage, Radius, Duration, Speed | **已乘 GrowthStats**，宿主不得再乘 DamageMul/AreaMul/ProjSpeedMul |
| Depth, BudgetLeft | 0=直接命中；>0 派生 |
| AllowsMechanism | 仅 true 时可交 `OnRunDirectHit`（P12/P13/P15）。W05 只有第一跳为 true ⇒ 回声/传播每次释放最多触发一次，传播不会重新起链 |
| Count, Pierce | FireProjectile 用 |

## 4. 执行映射（Codex）

- TargetDamage → `ApplyEnemyDamage(target, Damage, EnemyHit, From, ContentId, Slot, RootId)`；`AllowsMechanism` 时再 `OnRunDirectHit`。
- AreaDamage → 现有 `DamageArea(Position, Radius, Damage, ContentId, Slot, BudgetLeft)`。
- FireProjectile → 复用 RunWeaponStep 的弹体生成代码，Speed 直接用，不再 ×ProjSpeedMul。
- ZoneSpawned → 只发表现事件（Radius/Duration）。

## 5. P03 / P05 / P08（复用现有聚合器，不再乘）

| | 适用 | 消费位置 | 累计/上限 | 本阶变化 |
|---|---|---|---|---|
| P03 弹速 | 弹体：W01/W02/E01/E02；W03/W04/W05/W06 无效 | CombatModule 弹体 `Speed×ProjSpeedMul`（已存在）；W01 适配命令同口径 | 同组加算 ≤+100% | +15/30/45% |
| P05 持续 | W04 区域（宿主已乘）、W06 残留寿命（组件）；不延长无敌/回声延迟/闪避 | `DurationMul` | ≤+100% | +15/30/45% |
| P08 减伤 | 玩家受伤 | `RunSimulation.ApplyDamage` → `FinalIncoming`（已存在，向上取整，≥1） | ≤50% | 6/12/18% |

## 6. 最小配方示例

1. **W05 弹跳电弧**：`CooldownTrigger(cd)` → `ChainArcCarrier`（首跳 range 7m×AreaMul，跳距 jump×AreaMul，falloff 0.9，阶5 fork2/budget6）。诊断：8 个敌人一字排开，阶5 输出 6 条命令、目标 1..6 不重复、伤害 11/9.9/8.91×2/8.02×2，冷却内 0 条。
2. **W06 移动残响 + P17**：`DistanceAccumulator(spacing)` → `ResidualZoneField.Place/Contact`；实际闪避 → 阶5 `Detonate(3, detDmg 15, 1.5m)` + P17 `AreaDamage(12, 2.5m)`。诊断：走 36m 布置 16 次、同时存活 8 点（上限）；传送 500m 布置 0；无闪避通知 0 条；闪避后 3 引爆+1 放电；1.5s 内再闪避 P17 0 条。
3. **W01 旧行为适配**：`MigrateLegacy("W01")` 后 `CooldownTrigger` → 最近目标 → `FireProjectile`（阶5+P03 3阶：9 伤、速度 14×1.45=20.3、2 发、穿透 2）。默认**不迁移**。

（诊断为仓库外临时控制台程序，未入库，未扩测试套件。）

## 7. 新旧入口迁移表（防双重开火）

| 内容 | 当前入口 | 08A 后入口 | 宿主必须 |
|---|---|---|---|
| W01 | CombatModule.RunWeaponStep | 默认仍旧入口；样板可迁 | 若调用 `MigrateLegacy("W01")`，RunWeaponStep 跳过 W01 |
| W02/W03/W04、E01–E03 | 旧入口 | 不变 | — |
| W05/W06 | 无（若开白名单会误走旧弹体分支！） | 组件 | RunWeaponStep 跳过 `components.Owns(id)` 的武器，**之后**才可开白名单 |
| P17 | 无 | 组件 | T1 调 NotifyDodge |
| P03/P05/P08 | 宿主已消费 | 不变 | 只需开白名单 |
| P12/P13/P15 | OnRunDirectHit | 不变；组件命令 AllowsMechanism 时调用 | 派生命令不得调用 |

## 8. 已做 / 未接线

- 已做（纯规则可执行，dotnet 编译通过）：上述 6 个组件、W05、W06（含连线/引爆）、P17、W01 适配样板；目录 W05/W06/P03/P05/P08/P17 → RulesReady。
- **未接线**：游戏里尚无任何新效果；`RC01EnabledIds` 未改；无表现事件；W06/E06 进化、W05 + P11 暴击无实现。
- 下一批（08B，仍 planned）：P10 缓释恢复（TickGrowth 周期）、P11 暴击（直击处 RNG，派生不暴击）、P14 减速（直击，需敌人速度修饰入口）、P16 击杀余振（T7 死亡回调，深度1）、E04 共振网络、E05 连锁过载（ChainArcCarrier fork+1）、E06 疾行回路（Detonate 全部 + detCd）、D05 临时屏障、D06 闪避补给（重置 DodgeReadyAt）。

## 9. 给 Codex 的共享宿主请求（≤3）

1. **执行入口**：CombatModule 在 T6 后 T7 前调用 `Advance` 并执行命令（第 4 节）；`ApplyRuleDamage` 当前只接受 Echo/Propagation 类型，需加一个接受组件命令的内部路径（或直接在 CombatRunContent 内执行）。RunWeaponStep 跳过 `Owns(id)` 的武器。
2. **输入**：RunSimulation 在 T1 真闪避分支调用 `NotifyDodge`；开局/调试传送/复活调用 `RebaseMovement`；成长选择后 `Refresh`；提供 `MaxLegalStep`。
3. **白名单与表现**：完成 1/2 后把 W05/W06/P03/P05/P08/P17 加入 `RC01EnabledIds` 并标 Wired；为电弧跳/残留布置/放电提供表现事件（或复用 EnemyHit/WeaponZone + SourceId），这由 Codex 决定。

## 10. 新增一种内容的操作

目录加一行参数 → 若现有组件可组合，在 `RunComponentRuntime.Advance` 的 switch 加一个 case 组合触发/载体 → 加入 `DefaultOwnedIds` → 迁移表登记 → Codex 开白名单。特殊行为才新增一个组件类。
