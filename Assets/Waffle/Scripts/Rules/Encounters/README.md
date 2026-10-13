# GP-A 遭遇模块（Rules/Encounters）

状态：**规则模块已验证，主场景待Codex接线**。未在 Unity 中编译或运行。

> ⚠ 本模块写于 GP00 共同契约之前（基线 `e5d2c64` = main `1d56f45` + 任务卡）。`Encounter*` 全部是**临时类型（PROVISIONAL）**，只为映射到 GP00 正式类型，不在 `GameplayNext` 命名空间、不与契约行同名。GP00 冻结后由 Codex 做薄映射或直接替换。

| 切片 | 状态 |
|---|---|
| A1 冲锋者 | 规则完成 + 必要检查 |
| A2 远程落波者 + 五段遭遇数据 | 规则完成 + 必要检查（未试玩调参） |

## API

```csharp
var enc = new EncounterModule(config, runSeed, runId); // 开局/重开各 new 一个
EncounterOutput o = enc.Tick(ctx, enemyViews);          // 仅 Playing 时调用
bool hit = enc.TryClaimHit(attackId, now);              // 宿主几何命中后申领；每招最多一次 true
long cancelled = enc.OnEnemyRemoved(serial, generation); // T7 死亡/回池
enc.TryGetAttack(id, out req);                          // 只读：给 D 的预警几何
```

- 输出：`Spawns`、`Moves`（每实体至多一条）、`NewAttacks`、`CancelledAttacks`、预算跳过计数。列表在下一次 Tick 被复用。
- 模块不持有 HP/XP、不读 Time/Input、无 MonoBehaviour、无物理；追击敌人（`chaser`）不给移动意图，继续走旧路径。模块关闭（不创建）时旧路径不变。
- 随机：`EncounterRng`（xorshift32），种子 = 共同种子 ^ `0xA11CE5E7`，不消耗升级/奖励/CombatRng。

## tick 对应

| 步骤 | 调用 |
|---|---|
| T2 之后 | 用玩家实际位置组 `EncounterContext`，`Tick` |
| T3/T4 | 执行 `Spawns`（出生环）与 `Moves`（Locked=true 不转向、不做普通挪位；冲锋越界停止） |
| T6–T7 | T7 死亡结算后对死亡/回池实体调 `OnEnemyRemoved`，并撤销返回的 AttackId |
| T8 前 | 对生效窗口内的攻击做几何（Capsule 扫掠 / Circle），命中则 `TryClaimHit`，true 才作为伤害候选进入既有减伤/无敌/接触聚合 |
| 暂停/升级 | 不调用 Tick（时间不推进） |
| 重开/终局 | 丢弃实例，新建 |

## 临时类型 → GP00 映射

| 临时类型 | GP00 契约行 | 备注 |
|---|---|---|
| EncounterContext | RunContext | 另含宿主当前普通数/池占用/攻击数，供预算判断 |
| EncounterEnemyView | EnemySnapshot | `Relocated` 可选，模块另有跳变兜底 |
| EncounterSpawnRequest | SpawnIntent | 出生区 = 以玩家为心的环 |
| EncounterMoveRequest | MovementIntent | Locked ⇒ AllowRelocate=false |
| EncounterAttackRequest | HostileAttackIntent | Shape 仅 Circle/Capsule；命中策略 = 每 AttackId 一次 |
| EncounterRng | 共同种子派生 | 模块盐 0xA11CE5E7 |

## 参数（`EncounterConfig`，全部可调）

| 参数 | 初值 | 说明 |
|---|---|---|
| 冲锋 接近速度/触发距离 | 3.0 m/s / 5 m | 进入后停步预警 |
| 冲锋 预警/冲刺/恢复 | 0.8 / 0.35 / 1.2 s | 预警开始即锁定方向与胶囊 |
| 冲锋 速度/长度 | 16 m/s / 5.6 m | 长度=速度×时长，略超触发距离以覆盖站桩玩家 |
| 冲锋 胶囊半宽 | 0.5 m | 不含玩家半径 |
| 落波 保持距离 | 6–9 m，2.6 m/s | 只直线进退，无寻路 |
| 落波 间隔/首发 | 4 s / 出生后 1.5–4 s 随机 | 错开同批齐射 |
| 落波 预警/生效/半径 | 1.0 / 0.2 s / 1.1 m | 锁定在发招时的玩家位置 |
| 预算 | 普通 48，池 56，攻击 12（普通 8），单实体 1 招 | 满则跳过；攻击延后 0.25 s 再试，不排队 |
| 出生环 / 每 tick 最多生成 | 14–18 m / 1 | 大 dt 不补发，累积器封顶 1 个间隔 |
| 挪位跳变阈值 | 3 m | 单 tick 位移超过即视为挪位 |

**侧躲推导：** 需横移距离 = 半宽 0.5 + 玩家半径 0.5 = 1.0 m；可用时间 = 预警 0.8 − 反应 0.25 = 0.55 s；步行 5 m/s ⇒ 2.75 m ≫ 1.0 m。冲锋长度不影响横向，只影响是否够得着。`SidestepFeasible()` 可在调参后自检；把预警压到 0.45 s 以下（5 m/s）才会不可行。

**五段混编（追击/冲锋/落波 权重，间隔，段上限）：**

| 时间 | 权重 | 间隔 | 上限 |
|---|---|---|---|
| 0 s | 0.92/0.08/0 | 1.20 s | 24 |
| 120 s | 0.72/0.18/0.10 | 1.00 s | 32 |
| 240 s | 0.60/0.22/0.18 | 0.85 s | 38 |
| 360 s | 0.52/0.24/0.24 | 0.72 s | 44 |
| 480 s | 0.46/0.26/0.28 | 0.62 s | 48 |

4:00/8:00 精英保留在宿主现有事件里（`keptEliteTimes` 只作记录），模块不生成精英、首领。调参：README 体验案建议约 1 分钟才出冲锋者，若要严格对齐可把首段冲锋权重设 0 并在 60 s 插一段（模块支持任意段数）。

**复现两种混编：** 种子固定，`new EncounterModule(new EncounterConfig(), 11, 1)`，从 480 s 起以 0.05 s 步长 Tick 到 600 s，`Spawns` 中两种新敌人都出现（见测试 `Dropper_KeepsRange_CircleAtLockedPlayerPos_StageMix`）。

## 给 Codex 的接线请求

1. GP00 落地后把 `Encounter*` 映射/替换为正式类型；`EnemySnapshot` 需提供 ArchetypeId 与 Serial+Generation。
2. CombatModule 在 T3 执行 `Spawns`（新增 `charger`/`dropper` 原型：半径、HP、外观由 Codex/D 定），T4 执行 `Moves`；Locked 移动禁用分离推挤转向与普通挪位，冲锋越界即停。
3. 宿主实现 Capsule 扫掠（起终点+半宽+玩家半径，按实际位移扫，不穿过玩家）与 Circle 检测，仅在 `ActiveStart..ActiveEnd` 检测；命中后 `TryClaimHit`。
4. T7 后调用 `OnEnemyRemoved`，并按 `CancelledAttacks` 撤销预警表现。
5. 在 Context 中填写当前普通敌人数、池占用、全部/普通活动攻击数（含 C 的攻击），以共享预算。
6. 新模式下用本模块的生成节奏替代旧普通刷怪（保留精英/推进/包围事件的取舍由 Codex 定）；首领阶段停刷/降压由宿主覆盖（不调用或把 Context 上限填满）。
7. 伤害 `damage=1` 为占位，需与现有接触伤害口径统一。

## 必要检查

`Tests~/`（Unity 忽略，dotnet 运行：`cd Tests~ && dotnet test`）7 项：确定性、预警无伤害+锁定不追踪+单次候选、死亡/世代/挪位撤销、预算满不爆发、重开干净、侧躲可行、落波距离/几何/五段数据。另 `Tools/RulesTests` 会一并编译本目录运行时代码。

## 已知缺口

- 未在 Unity/主场景运行；数值未试玩。
- 落波者只直线进退，可能被地形卡住（首轮不做寻路）。
- 预算以宿主传入的计数为准，本 tick 内只累计本模块新增。
- 时间用 double 秒；攻击生效窗口按计划时间，冲刺阶段切换精度受 tick 粒度影响（≤1 tick）。
