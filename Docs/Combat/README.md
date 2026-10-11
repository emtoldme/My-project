# C-01A 战斗模块接口说明（Waffle S1）

基线：`origin/main@b6df1fda73c6b0cf5d1e25227c2114a87dba0564`。分支：`feature/c01-combat-core`。
规则依据：《Waffle S1 玩法规则与测试方案 v0.2.1》§0.3、§1.2–§1.5、§1.8、§1.9。普通存活上限按 C-01A 任务卡改为 **30**（v0.2.1 原为 60）。
所有数值都是**设计初值**，未经 Unity 实测。

## 1. 固定名称

| 名称 | 类型 | 说明 |
|---|---|---|
| `Waffle.Combat.CombatModule` | 类 | 战斗模块本体。`new CombatModule(CombatConfig config, int seed)`；**新一局 new 一个新实例**，没有 static 局内状态 |
| `Waffle.Combat.CombatConfig` | `[Serializable]` 类 | 全部可调参数（武器/敌人/刷怪/宝石），公有字段可挂 Inspector |
| `Waffle.Combat.CombatInput` | 结构体 | 每 tick 输入 |
| `Waffle.Combat.CombatPlayerStats` | 结构体 | 框架按升级算好的玩家属性 |
| `Waffle.Combat.CombatStepResult` | 类 | 步骤输出（模块内复用同一个对象 `module.Result`） |
| `Waffle.Combat.EnemyState` / `ProjectileState` / `GemState` | 类 | 只读视图（属性 `internal set`） |
| `Waffle.Combat.CombatCounters` | 类 | 整局累计计数（日志/调试） |
| `Waffle.Combat.Vec2` | 结构体 | XZ 平面向量：`X` = 世界 x，`Y` = 世界 z |

代码位置：`Assets/Waffle/Scripts/Rules/Combat/`，编进现有 `Waffle.Rules` 程序集（`noEngineReferences: true`，不依赖 UnityEngine）。表现事件直接复用框架已有的 `Waffle.PresentationEvent` / `PresentationEventType`。

## 2. 输入

```csharp
struct CombatInput {
    float Dt;          // 本 tick 逻辑 dt（已夹 maxDt、乘 slowmo）
    float RunTime;     // 本 tick 开始时的 now（T11 之前）
    Vec2  PlayerPos;   // T2 之后的玩家 XZ
    float ViewRadius;  // R_view（m），框架按相机算；≤0 时用 14
    CombatPlayerStats Stats; // Damage / FireInterval / Pierce(int) / MagnetRadius
}
```

`Stats` 建议取值：`Damage = CurrentValue(Damage)`、`FireInterval = CurrentValue(FireRate)`、`Pierce = (int)CurrentValue(Pierce)`、`MagnetRadius = CurrentValue(PickupRange)`（以 RunSimulation 实际的 UpgradeKind 名为准）。拾取半径 1.0、弹速 15、射程 8 等不随升级变化的参数在 `CombatConfig`。

## 3. 调用顺序（对应 §0.3）

选择“三个步骤方法”而不是单个 `Tick`：因为 T9 失败时必须跳过 T10、直接到 T13；拆成三段，框架只需在原有 T8/T9/T10/T13 位置各插一行，不需要模块了解失败判定。

| 框架步骤 | 调用 | 模块内部 | 框架拿到结果后做什么 |
|---|---|---|---|
| T3–T8 | `var r = combat.StepCombat(input);` | T3 刷怪 → T4 敌人移动（追踪+分离+夹边界+远离挪位）→ T5 武器 → T6 子弹移动+相对运动扫掠命中 → T7 死亡结算/掉宝石 → T8 接触检测 | 转发 `r.Events`；`r.Kills` 加击杀数；T8：不在无敌期间且 `r.ContactDamage > 0` 时扣血并设受击无敌（叠加模式用 `r.ContactDamageSum`） |
| T9 | —（框架） | — | HP ≤ 0 → Defeat，跳到 T13，**不调用 StepGems** |
| T10 | `var g = combat.StepGems(input);` | 宝石寿命、磁吸、拾取（每颗只结算一次）；含满上限时直接计入的 XP | 转发 `g.Events`（GemPickup）；`g.XpCollected` 走现有加 XP/升级流程 |
| T13 | `combat.Release();` | 本 tick 标记回收的敌人/子弹/宝石回池，代号 +1 | **每个执行了 StepCombat 的 tick 都必须调用**（包括失败/胜利 tick） |

- `Result` 每次调用 `StepCombat` / `StepGems` 时清空，请在下一步调用前读完。
- `PresentationEvent.Frame` 由框架填写；`Now` = 本 tick 的 now，`X/Z` = 位置，`Value`/`Data` 见下表。
- 暂停、LevelUp 面板、终局之后：不调用任何步骤。重开：丢弃旧实例，new 新的。

| 事件 | 步骤 | Value | Data |
|---|---|---|---|
| `Shot` | T5 | 伤害 | 目标 Serial |
| `EnemyHit` | T6 | 伤害 | 敌人 Serial |
| `EnemyKilled` | T7 | 掉落 XP | 敌人 Serial |
| `GemPickup` | T10 | XP | 宝石 Serial |

## 4. 输出（只读状态，视图同步用）

- `combat.Enemies`：活动敌人（按 Serial 递增）。`IsAlive == false` 的是本 tick 刚死、等 T13 回收的，视图应隐藏。字段：`Serial/Generation/PoolIndex/Position/Hp/MaxHp/Radius/IsElite/Phase/SpawnSource`。
- `combat.Projectiles`：`Finished == true` 的隐藏。
- `combat.Gems`：`Life` 为 Free/Magnet 的显示；`IsBig` 大宝石；`IsBlinking(config.gemLifetime)` 最后 3 s 闪烁。
- `PoolIndex` 在池内固定，可用来一一对应预热好的视图对象；保存引用时同时保存 `Serial + Generation`，用 `TryGetEnemy` 校验。
- `combat.AliveCount`、`combat.WeaponTimer`、`combat.Counters`（PoolMiss、Truncated、RingSkipped、Relocated、GemsMerged 等）。

## 5. 已实现的规则（设计初值）

- 刷怪：四阶段波次 2/2.5s、3/1.5s、4/1.4s、4/1.0s，阶段首波在阶段开始时刷，间隔累加保留余数；刷怪环 [R_view+1, R_view+5]，合法落点 |x|,|z| ≤ 29，不合法整圈重试 3 次再朝中心 ±45°；30 s 起玩家近 0.5 s 平均速度 ≥ 1 m/s 时 50% 落在移动方向 ±45°。事件：方向推进 60 s/10、160 s/12；包围圈 90/120 s 各 16、165 s/20（场外点跳过）；精英 120/150/170 s。普通到 `aliveCap = 30` 截断，事件可到 `aliveCap + eventOverflow = 50`。now ≥ 180 不再刷。
- 敌人：HP 15/25/40/55、速度 3.0/3.5/3.75/4.0，**生成时锁定**；精英 HP×6、半径 0.8、速度×0.8、接触 20、掉 10 XP。分离 O(n²)：半径 1.0、推力上限 1.0 m/s，先算完再移动，与遍历顺序无关。距离 > R_view+10 挪到前方刷怪环（不算死亡、不掉落，prevPos = 新位置）。敌人不阻挡玩家。
- 武器：计时器每 tick 减 dt；没目标时停在 0；有目标且 ≤ 0 时开火并 += 间隔（保留余数，每 tick 最多 1 发）。目标 = 8 m 内最近的 Alive 敌人（距离相同取 Serial 小的），不保存目标引用。子弹直线、不追踪，速度 15、寿命 0.8 s、半径 0.2，池 16（池空记 PoolMiss 并跳过这一发）。
- 命中：相对运动扫掠（B0−E0 → B1−E1 对半径和），按进入参数 s 再按 Serial 排序依次结算直到穿透用完；本 tick 新生子弹 E0 = E1；新刷的敌人 prevPos = 生成点。每颗子弹按 (Serial, Generation) 去重；已死亡的敌人直接穿过、不消耗穿透。Alive→Dead 只转换一次，只有这一次产生击杀、掉落和事件。
- 接触：只看 Alive（本 tick 被打死的不造成伤害）；报告最大值、总和、接触数、来源。**模块不判无敌**。
- 宝石：普通 1 XP；寿命 20 s（磁吸后停表，大宝石永久）；磁吸半径由 Stats 传入，初速 8、加速度 20、上限 20；拾取半径 1.0。占用上限 150，满时在创建时合并：已有大宝石就加值，否则把最老的 Free 普通宝石原地转成大宝石，都没有则本 tick T10 直接计入 XP。XP 守恒。
- 随机：模块自带 xorshift 随机流（只用于刷怪/挪位），同种子同输入结果一致。

## 6. 未实现 / 与规范的差异

- 调试命令 `spawn n`、`setcap n`、`gemexpire`、`gemhold`、`gemflood` 的命令解析不在本模块（DebugCommands 归 Codex）；模块提供 `DebugSpawnEnemy`、`DebugSpawnGem` 和可改的 `Config.aliveCap/gemLifetime` 供接入。
- 每 tick 最多开 1 发；dt 远大于开火间隔时不补射（dt 上限 0.05 远小于最短间隔 0.30，正常不会触发）。
- 子弹池空时仍消耗这次冷却（避免每帧重试），规范未明确，记在这里。
- 精英、方向推进、包围圈已实现但只做了编译和 3 分钟冒烟（见 §7），未单独写用例。
- 视图对象、颜色档位、闪烁表现、相机算 R_view 均由 Codex 在视图层完成。

## 7. 验证范围（只在 dotnet 下）

- 临时工程 `/tmp/c01tests`（仓库外，net8.0，LangVersion 9）编译 `Rules/**/*.cs` + `Tests/EditMode/*.cs` + `Tests/Combat/*.cs`：**104 通过 / 0 失败**（原框架 85 + 战斗 19）。
- 3 分钟无升级、玩家不掉血的冒烟（seed 12345，dt 1/60，伤害 10、间隔 0.6）：站桩 刷 145、击杀 100、拾取 81 XP、最高同时存活 50、三种 PoolMiss 均 0；绕圈 刷 142、击杀 97、拾取 90 XP。这只是接线自检，**不是平衡数据**（没有升级、没有扣血）。
- 未在 Unity 中编译或运行；`.meta` 由本分支用随机 GUID 手写，需 Unity 导入确认。
