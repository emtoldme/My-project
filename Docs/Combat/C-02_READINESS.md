# C-02 战斗边界就绪报告（1 号）

> Codex整合复验（2026-10-12）：Unity6000.4.3f1 EditMode规则程序集117项通过，含本轮6条用例；.meta已由Unity导入确认。下文“Unity未运行”是作者交付时的环境记录。

## 基线与提交
- 基线：`origin/main@8e5fe719db2e1d57468e5f451be3e7732e7ea797`
- 分支：`feature/c02-combat-guards`（独立目录 `/workspace/wf-c02`）
- `1e4339b`：新增 `Assets/Waffle/Tests/Combat/CombatEventGuardTests.cs`（+ 手写 `.meta`）
- 本报告提交：见 PR 中最后一个提交
- 战斗规则代码 **未修改**：`Rules/Combat/` 与 C-01A `c5a5ed7` 相比无差异（Codex 整合时未改动该目录），本轮也没有发现需要修的缺陷。

## 测试证据（仅 dotnet，未在 Unity 中运行）
- 环境：Linux 6.12，.NET SDK 8.0.425，`Tools/RulesTests/WaffleRules.Tests.csproj`（NUnit 3.14，net8.0），命令 `dotnet test`
- 基线：111 条（本轮之前）
- 本轮后：**117 / 117 通过，0 失败，0 跳过**（新增 4 个方法、6 条用例）
- 回归敏感性自检：临时把事件上限改成 `aliveCap`（即误把事件夹到 30），新增用例中 2 条立即失败；改动已撤回，未提交。
- Unity EditMode / PlayMode：**未运行**。新测试文件的 `.meta` 为手写随机 GUID，需 Codex 在 Unity 导入时确认。

## 新增用例
| 用例 | 抓的回归 |
|---|---|
| `EventsOnly_FireOnceOnSchedule_OverflowTo50_NotClampedTo30` | 关闭普通波次、伤害 0，seed 4242：推进 60s/160s、包围 90s/120s/165s、精英 120s/150s/170s 各只触发一次；首次触发时刻误差 ≤1.5 帧；精确计数 推进 16、包围 32、精英 2、截断 27、峰值存活 50（>30）；出生点全部合法；池不缺 |
| `Elite_SpawnStats_KilledOnce_Drops10Xp_RecycledWithNewGeneration` | 精英 HP×6、速度×0.8、半径 0.8、接触 20、宝石 10；只结算一次击杀与一个 EnemyKilled 事件；拾取恰好 10 XP；回池后 Generation+1、旧 (Serial,Generation) 引用失效 |
| `SpawnPositions_LegalAndOutsideSafeDistance_NormalCapHeld` ×3 | seed 1/99/2026，玩家在中心、贴角 (26,26)、贴边 (-28,5)：全程 180s 所有出生点 |x|,|z|≤29 且距玩家 ≥ R_view+1；存活从不超过 50；池不缺 |
| `SameSeed_Deterministic_NewInstanceClean` | 同 seed 两局出生序列逐项一致；新实例（重开）敌人/宝石/计数器为空 |

## 表现层可依赖的语义与只读字段
- `EnemyState.IsElite`：精英标记，生成时锁定。精英没有专属生成事件，表现层在首次看到新 `Serial` 且 `IsElite` 时做出场强调。
- `EnemyState.SpawnSource`：0 普通波次，1 方向推进，2 包围圈，3 精英，9 调试。可用于事件出场提示（如包围圈警示）；同一 tick 内同来源的一批新 Serial 即为一次事件。
- `EnemyState.Radius / MaxHp / Hp / Phase / Serial / Generation / PoolIndex / Position / PrevPosition`：只读。
- 精英击杀：`PresentationEventType.EnemyKilled`，`Data`=Serial，`Value`=GemValue（精英为 10），可用于击杀强调。
- 上限语义：普通波次存活 ≤ `aliveCap`(30)；事件可额外到 `aliveCap + eventOverflow`(50)，超出部分计入 `Counters.Truncated`；包围圈出界点跳过计入 `Counters.RingSkipped`，不补位。这是有意设计，请勿改为一律 30。
- 触发：时间到达后一次性触发（`now >= 时刻`），`now >= runDuration` 后不再刷任何敌人；重开即新建模块，触发标记自然清零。

## 未覆盖 / 已知限制
- 方向推进的“玩家移动方向 ±20°”扇区角度没有单独断言（只验证了次数、合法性与距离）。
- 方向推进/普通波次落点非法时回退 `PickSpawnPoint` 的最终 Clamp 路径，在 R_view 远大于场地时理论上可能落进安全距离内；默认 R_view=14 下用例未触发。
- 远离挪位（relocate）后的位置未单独断言。
- 没有 200 敌压力、设备矩阵或 Unity 实测（任务要求不做）。
- 默认难度与局长未改动。

## 跨目录需求（交 Codex）
- 无代码需求。请在 Unity 导入时确认 `CombatEventGuardTests.cs.meta`，测试已在既有 `Waffle.Tests.Combat` 程序集内，无需新 asmdef。
- 若表现层需要精英/事件“出场”的显式事件（而不是靠 Serial+IsElite 推断），请提出需求，由 1 号在战斗模块中追加 Emit。
