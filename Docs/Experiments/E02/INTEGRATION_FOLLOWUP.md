# E-02 复核与真实战斗适配表（1号）

- 基线：`origin/main` = `2d113dd656933c48b5a510c86ea5993d169e7c2a`；作者原始头 `f9ea275`
- 分支：`review/e02-integration-followup`
- 结论：**Codex 两处修复正确，未发现新缺陷**。本分支只新增检查，不改运行时代码。

## 1. 修复复核

| 修复 | 结论 |
|---|---|
| `Spawn` 派生事件时间 `Time + delay` → `parent.Time + delay` | 正确。父事件在 `Drain` 中按计划时间结算，子事件锚定父计划时间后，与步长无关。直接命中的 `parent.Time` = 命中时 `Time`，语义不变。 |
| `DirectHit` 暂停时返回 0、不改目标/队列/日志 | 正确。与真实规则 `RunSimulation.Tick` 中「非 Playing 不执行 T1–T13」一致。 |

## 2. 粗/细步长一致性

新增 `Tests/EffectComboStepInvarianceTests.cs`（含 `.meta`）：
- `CoarseSingleStep_EqualsFineSteps_ExactTimesAndDamage`（none/A/B/AB 四例）：`Tick(2.0)` 一步、`Tick(0.01)×200`、`Tick(0.3)×7` 三种步进，逐条比较 Seq/ParentSeq/Trigger/Time（`R` 精确格式，非两位小数）/Kind/Depth/Source/Target/Damage/HpAfter/Outcome，**完全一致**，队列清空。
- `PauseInMiddle_DoesNotShiftScheduledTimes`：中途暂停 5s（期间 DirectHit 被拒）后续推，与不暂停序列一致。
- 回归有效性：把运行时换回 `f9ea275` 版本后，新检查中 AB 例与暂停例失败（连同 Codex 的 2 项共 4 项失败），即能抓住原缺陷。

## 3. 测试证据（dotnet，非 Unity）

- 环境：Linux 6.12，.NET SDK 8.0.425，NUnit 3.14，`Assets/Experiments/Equipment/DotnetRunner~/Tests`（csproj 用通配包含 `Tests/*.cs`，Codex 新增测试已自动包含，无需改 runner）。
- 结果：**13 通过 / 0 失败 / 0 跳过**（原 8 项 + 新增 5 项）。
- 未在 Unity 中运行。

## 4. 未来真实战斗适配表（基于现有 `Assets/Waffle/Scripts/Rules/Combat`）

| 问题 | 现状（真实字段/步骤） | 适配建议 |
|---|---|---|
| 直接命中在哪一步 | `CombatModule.StepCombat` 的 **T6 `ProjectileStep`**：`e.Hp -= p.Damage; Counters.Hits++; Emit(EnemyHit…)`，Hp≤0 时 `Life=Dead` 入 `newlyDead`；T7 `DeathStep` 结算掉落与回收标记 | 直接命中即 T6 那一处。回声/传播应在 T6 记录“触发”，到期事件在同一模块内、建议 T6 之后 T7 之前结算（这样派生击杀仍走唯一的 T7 死亡/掉落入口，不另造死亡路径）。 |
| 目标句柄 | `EnemyState.Serial`（每次出池递增，一局不重复）+ `EnemyState.Generation`（`Release()` T13 回池时 +1）+ `IsAlive`；已有 `TryGetEnemy(serial, generation, out e)` 及内部 `Key(e)=(Serial<<32)|Generation` | **已足够**，无需新字段。夹具 `TargetHandle(Id, Generation)` 直接映射为 `(Serial, Generation)`；`SkippedStale/SkippedDead` 映射为 `TryGetEnemy` 失败。注意：同 tick 内被打死的敌人 `IsAlive=false` 但 Generation 到 T13 才变，`TryGetEnemy` 已同时检查 IsAlive，行为与夹具一致。 |
| 谁排队 | 夹具中 `ComboRun.Resolve/Spawn` | 由 `CombatModule` 自己持有队列（与池同生命周期），只在规则层入队；表现层只读事件，**不得**在 PresentationDispatched 回写伤害。 |
| 谁清队列 / 重开 | 真实重开：`GameLoop.Restart()` → `NotifyRestart` → 重新加载场景 → 新 `RunSimulation` → 新 `CombatModule` | 队列作为 `CombatModule` 实例字段即可随实例丢弃，天然清空；无需额外 Restart API。夹具里的 `Restart()` 仅是实验便利。 |
| 暂停 | `RunSimulation.Tick` T0 门控：非 `Playing` 直接 return，`StepCombat` 不被调用 | 队列时钟用 `CombatInput.RunTime`（即 `State.Now`），暂停期间不推进、也不会发生直接命中；无需在 Combat 内再加暂停标志。 |
| 时间基 | `State.Now` 为 float，`FixedDt` 可选 | 正式接入建议用整数 tick 或固定 dt，计划时间锚定父事件计划时间（同本次修复）。 |

## 5. 一维夹具的已知限制（仅记录，不扩框架）

- `ComboConfig` 是公有可变字段的外部引用，运行中修改会影响后续派生；正式版应在开局拷贝或只读。
- `budgets`（每次直接命中一条 `TriggerBudget`）与 `log` 在一局内只增不减，长局会线性增长；正式版应在触发的所有派生结算完后移除预算，日志改为每 tick 清空的事件缓冲（类似 `Result.Clear()`）。
- 位置为一维 `X`，传播选目标是 O(n) 线性扫描；队列取最小值也是 O(n)。
- 时间为 float；未测性能/GC。

## 6. 跨目录请求

无。未改 RunSimulation/CombatModule/UpgradeRules。
