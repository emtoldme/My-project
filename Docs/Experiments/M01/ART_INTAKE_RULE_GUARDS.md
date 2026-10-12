# RF-01 美术接入规则护栏：接线检查表（1号）

- 基线：`main` 13be83d24504f83d5957a691e7caeccf440c999c
- 参考：PR #28 `feature/m01-echo-cues` b5d7f4ad1a2079fb70663de3295da4dd514ebe1a（`CopyPendingEchoes`）；PR #29 `feature/m01-readable-feedback` 3ab3838dd5f4c0fadb686681766563ca39769207（`M01EchoCue`/`SyncEchoCues`/`PlayEcho` 重载）
- 范围：只读审查，本文不改代码、不新增模拟器/测试，不作性能结论；未在 Unity 中运行。
- 标记：**【规则保证】** = 现有规则代码已保证；**【适配层保证】** = 必须由 Codex 的接线/表现适配层保证。

## 1. 事件读取来源

| 事件 | 唯一来源 | 时机 | 保证 |
|---|---|---|---|
| 真实射击 Shot | `CombatModule.WeaponStep`（T5）`Emit(PresentationEventType.Shot, …)` → `GameLoop.PresentationDispatched` | T5，每次真实开火一条 | 【规则保证】 |
| 直接命中 | `CombatModule.ProjectileStep`（T6）`ApplyEnemyDamage(…, EnemyHit, …)` 产生 `EnemyHit` 事件；随后 `ruleExtension.OnDirectHit` → `M01CombatBridge.OnDirectHit` → `MechanismRules.ReportDirectHit` | T6 | 【规则保证】直接伤害只扣一次，M01 只登记不再扣 |
| 派生命中 | `StepCombat` 中 T6 与 T7 之间 `AfterProjectiles` → `MechanismRules.Tick` → `M01CombatBridge.ApplyDerivedDamage` → `CombatModule.ApplyRuleDamage`，产生 `EchoHit`/`PropagationHit`/`EchoPropagationHit` | T6–T7 之间 | 【规则保证】`ApplyRuleDamage` 只在 `resolvingRuleExtension` 期间有效，表现层回调无法扣血 |
| 死亡 | `CombatModule.DeathStep`（T7）`Emit(EnemyKilled, …)` | T7，每个死亡敌人一条（含派生致死） | 【规则保证】 |

适配层要求【适配层保证】：`SonicAttackVFX.PlayFire` 只订阅 `Shot`；`PlayHit` 只订阅 `EnemyHit`（派生命中走 `M01DerivedFx.PlayEcho/PlayPropagation`，不要重复调 `PlayHit`，除非有意且只作视觉）；`PlayDeath` 只订阅 `EnemyKilled`。不得从碰撞、动画事件或视觉计时器推断这些事件。

## 2. 回声预备标记的撤除

- `MechanismRules.CopyPendingEchoes`（PR #28）对每条排队回声调用 `host.TryGetLiveTarget(origin)`，即 `CombatModule.TryGetEnemy(serial, generation)`；原目标已死或回池后 `Generation++`（`CombatModule.Release`，T13）都会被过滤掉，队列本身不动。【规则保证】
- 撤标依赖“每帧都同步一次”：`M01DerivedFx.SyncEchoCues`（PR #29）把本次未出现的标记关掉。**适配层必须每个渲染帧（至少每个推进过的 tick 后）调用一次 `CopyPendingEchoes` → `SyncEchoCues`，列表为空时也要调用（传空列表）**，否则死掉的目标上的标记会残留。【适配层保证】
- 注意同一 tick 内被派生伤害打死的敌人：在 T7 后其 `Life=Dead`，`TryGetEnemy` 已返回 false（若其要求 IsAlive），标记在下一次同步时撤除。发现：无缺陷。

## 3. 同一目标多条回声只保留最早到期

- **发现（PR #29 `M01DerivedFx.SyncEchoCues`）**：同一 `(Serial, Generation)` 第二次出现时直接 `continue`，即保留的是**列表中第一条**，而不是 `DueAt` 最小的一条；`CopyPendingEchoes` 的输出顺序是内部 `pending` 列表顺序，规则层不承诺按 `DueAt` 排序。
- 最小修复建议（二选一，放在适配层，不改规则）：转换 `PendingEchoInfo → M01EchoCue` 前按 `DueAt` 升序（同值按 Serial、Generation）排序；或在 `SyncEchoCues` 中对同一目标比较并保留 progress 对应 `DueAt` 更早者（需要在 `M01EchoCue` 带 DueAt）。推荐前者，改动最小。【适配层保证】
- 进度：`progress = clamp01((now - ScheduledAt) / (DueAt - ScheduledAt))`，`now` 用 `Sim.State.Now`（规则时间），不要用 `Time.time`。分母为 0 时视为 1。【适配层保证】

## 4. 暂停 / 重开 / 终局清理

| 场景 | 现状 | 保证 |
|---|---|---|
| 暂停 | `RunSimulation.Tick` 只在 Playing 执行 T1–T13，`StepCombat`/`Tick` 不跑，回声不推进；`Time.timeScale=0`（`RunState.TimeScaleFor`）。 | 【规则保证】回声不前进。【适配层保证】暂停期间进度要按规则 `now` 计算（不变），标记可保留冻结 |
| 重开 | `GameLoop.Restart` → `Sim.NotifyRestart` 发 `Restart` 事件 → `M01PlaytestController.OnEvents` 禁用 bridge 并 `Rules.Clear()`；`M01DerivedFx` 收到 Restart 调 `Clear()`；随后重载场景，新建 `RunSimulation/CombatModule/M01CombatBridge`。 | 【规则保证】队列与计数清空。【适配层保证】`SonicAttackVFX` 无 Restart 处理，靠场景重载销毁；若以后改为不重载场景，需加 `Clear()` |
| 终局（Victory/Defeat） | `EnterTerminal` 后不再 `StepCombat`，排队回声永不结算；`CopyPendingEchoes` 仍会返回存活目标上的回声，标记会冻结显示“将要再挨一下”。 | **【适配层保证】发现**：收到 `Victory`/`Defeat` 事件后应停止同步并 `SyncEchoCues(空列表)`（或 `M01DerivedFx.Clear()`）。最小修复：在 `M01PlaytestController.OnEvents` 中对这两类事件加一行清标 |

## 5. SonicAttackVFX.cs 审计（`Assets/ReverbInTheRye/VFX/Scripts/SonicAttackVFX.cs`）

| 风险项 | 实际代码 | 结论 |
|---|---|---|
| 持有 HP / 伤害 / 死亡状态 | 只有 `Effect`/`Ring` 的视觉字段（elapsed、duration、尺寸、透明度） | 无发现 |
| 碰撞器 / 刚体 / OnTrigger / OnCollision | `CreateEffect`/`MakeRing` 只 `AddComponent<MeshFilter/MeshRenderer/ParticleSystem>`；无 Collider、Rigidbody、OnTrigger*/OnCollision* | 无发现。粒子未开启 collision/trigger 模块（默认关闭） |
| 扣血 / 调用规则 | 无任何对 Waffle/Mechanism 命名空间的引用（asmdef `Reverb.UserArt.VFX` 独立） | 无发现【适配层保证】：保持该 asmdef 不引用规则程序集 |
| 视觉计时器驱动下一次伤害 | `Update` 中 `elapsed += Time.deltaTime` 只用于回收和 `TickRings`；无回调/事件对外暴露 | 无发现。【适配层保证】不得给它加“播放完成”回调去触发伤害 |
| 静态状态 | 只有 `static readonly` 的 Shader 属性 ID 与颜色常量 | 无发现（无跨局残留） |
| 懒加载池 | `Acquire` 首次使用时 `CreateEffect` 新建 GameObject；满额回收最旧 | 规则无风险；纯视觉，首次生成可能有卡顿/分配（未测，不作性能结论）。可选：`Awake` 预热 |
| timeScale vs 规则时间 | 用 `Time.deltaTime`（受 timeScale 影响）：暂停时 timeScale=0 特效冻结，与规则一致；`GameLoop` 规则推进用 unscaled 时间 | 无规则风险；仅视觉时长与规则时间不同步，不能用于判定 |
| 缺材质 | `EnsureMaterials` 失败直接 return，不影响规则 | 无发现 |

## 6. 适配层必须保证的事项汇总

1. 每帧（含空列表）调用 `CopyPendingEchoes → SyncEchoCues`，否则不撤标。
2. 同目标按 `DueAt` 最早保留：转换前排序（PR #29 当前只保留首条）。
3. 进度用规则 `Sim.State.Now`，不用 Unity 时间。
4. Victory/Defeat 时清空预备标记（当前缺失）。
5. `SonicAttackVFX` 的 Play* 只从 `Shot`/`EnemyHit`/`EnemyKilled` 表现事件调用；不加回调、碰撞器或规则引用；若不再重载场景则需补 `Clear()`。
6. 只有 `CombatModule` 改 HP/死亡；表现层不得调 `ApplyRuleDamage`（规则已拒绝 StepCombat 之外的调用）。
