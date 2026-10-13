# GP-A 遭遇模块（Rules/Encounters）

最新状态：已由Codex整合到真实Main并完成Unity编译与短时运行，见`Docs/Project/GameplayNext/INTEGRATION.md`。下面保留原始模块交付记录，数值尚待完整局试玩。

> 本模块最初写于 GP00 之前（临时 `Encounter*` 类型，见 PR #52），现已移植到 GP00 v1（`gameplay/gp00-ready` `45467b9`）：临时类型已删除，直接实现 `Waffle.GameplayNext.IEncounterModule`。

| 切片 | 状态 |
|---|---|
| A1 冲锋者 | 规则完成，经真实 GP00 宿主（RunSimulation）检查 |
| A2 远程落波者 + 六段遭遇数据 + 4:00/8:00 精英 | 规则完成，经真实宿主检查；数值未试玩 |

## 接线（GP00 实际接口）

```csharp
// Codex 场景适配组件（IGameplayModuleProvider）每局：
return new GameplayModules(encounters: new Waffle.Rules.Encounters.EncounterModule(config));
// 纯规则：sim.InstallGameplayModules(new GameplayModules(new EncounterModule()));
```

- `Step(GameplayContext, GameplayCommandBuffer)`：只用 `TrySpawn / TryMove / TryAttack / PublishEncounter`。
- `Clear()`：幂等清空全部状态（含序号与随机源）；工厂每局新建实例。
- 随机：`new CombatRng(ModuleSeed ^ 0xA11CE5E7)`，首次 Step 时创建；不消耗旧刷怪/升级/奖励随机流。
- 安装后宿主关闭旧刷怪，本模块接管全部普通生成及 4:00/8:00 精英。

## 行为与 GP00 口径

| 内容 | 实现 |
|---|---|
| 追击 `chaser` / 精英 `elite` | 只发生成，不发移动 → 宿主既有追击 |
| 冲锋 `charger` | 接近（UseSeparation、可挪位）→ 进入 5 m 后提交 Capsule（Center=当前位置，End=锁定方向×5.6 m，`SweepOwnerMotion=true`，ActivateAt=+0.8 s，EndAt=+0.35 s）→ 预警/恢复期速度 0、无推挤、禁挪位 → 冲刺期按锁定方向 16 m/s，仅在与生效窗口重叠的时段位移 → 恢复 1.2 s |
| 落波 `dropper` | 保持 6–9 m（直线进退）；每 4 s 在玩家当时位置提交 Circle r=1.1，预警 1.0 s，生效 0.2 s；上一招结束前不再出招 |
| 命中/候选 | 全部由宿主：T8 前几何检测，每 AttackId 至多一次（闪避/护盾也消耗） |
| 死亡/世代 | 宿主 T7 撤销攻击；模块下次 Step 发现实体消失或 Generation 变化即丢弃状态 |
| 挪位 | 宿主撤销攻击并 `RelocationVersion++`；模块检测到版本变化即重置行为（落波首发重新计时） |
| 预算 | 模块按上一步 `ActiveAttacks`（普通 8/总 12）、`SpawnWarnings`（8）、存活普通数自限；不足则延后 0.25 s 用新 Sequence 再试；每 tick 最多 1 次生成、累积器封顶，不补发 |
| 精英 | 到点提交 IsElite 生成；以 `Enemies.SpawnRequest` 确认出生、`SpawnWarnings` 判断仍在预警；被拒则每 2 s 新 Sequence 重试，30 s 后放弃 |
| 暂停/升级 | 宿主不调用 Step |

tick 对应：宿主在 T2 后、T3 前调用 Step（玩家位置为 T2 后），T3 执行生成、T4 执行移动，T6/T7 后死亡撤销，T8 统一结算危险候选。

## 参数（`EncounterConfig`，全部可调）

| 参数 | 初值 | 说明 |
|---|---|---|
| 冲锋 接近速度/触发距离 | 3.0 m/s / 5 m | |
| 冲锋 预警/冲刺/恢复 | 0.8 / 0.35 / 1.2 s | 预警开始即锁定 |
| 冲锋 速度/长度/半宽 | 16 m/s / 5.6 m / 0.5 m | |
| 落波 保持距离/速度 | 6–9 m / 2.6 m/s | |
| 落波 间隔/首发 | 4 s / 出生后 1.5–4 s | |
| 落波 预警/生效/半径 | 1.0 / 0.2 s / 1.1 m | |
| 招式伤害 | 10 | 与现有普通接触伤害同口径 |
| 生成规格 hp/速度/半径/接触/宝石 | 追击 25/3.5/0.5/10/1；冲锋 30/3.0/0.5/10/2；落波 20/2.6/0.5/10/2；精英 240/2.8/0.8/20/10 | 初值，待试玩 |
| 出生环/出生预警 | 玩家周围 14–18 m（夹在场地内）/ 0.6 s | |
| 精英时刻 | 240 s、480 s | |

**侧躲推导：** 需横移 半宽 0.5 + 玩家半径 0.5 = 1.0 m；可用 0.8 − 反应 0.25 = 0.55 s；5 m/s ⇒ 2.75 m。`SidestepFeasible()` 可自检。真实宿主仿真：反应 0.3 s 后横走即完全躲开。

**六段混编（追击/冲锋/落波 权重，间隔，段上限）：**

| 时间 | 权重 | 间隔 | 上限 |
|---|---|---|---|
| 0 s | 1.00/0/0 | 1.20 s | 24 |
| 60 s | 0.92/0.08/0 | 1.20 s | 24 |
| 120 s | 0.72/0.18/0.10 | 1.00 s | 32 |
| 240 s | 0.60/0.22/0.18 | 0.85 s | 38 |
| 360 s | 0.52/0.24/0.24 | 0.72 s | 44 |
| 480 s | 0.46/0.26/0.28 | 0.62 s | 48 |

复现：种子 11、武器伤害 10、无敌，`Tests~` 中 `Stages_SixWithChargerAt60_DropperAt120_ElitesAt240And480` 跑 500 s：冲锋者首现于 60–120 s，落波者 ≥120 s，精英恰 2 只，无 `legacy-chaser`。

## 必要检查

`Tests~/`（Unity 忽略；`cd Tests~ && dotnet test`）编译整个 Waffle.Rules 源码，用真实 RunSimulation + GP00 宿主驱动，7 项：确定性；预警无伤害+锁定不追踪+单次候选；横移可躲；所有者死亡（T7）撤销；挪位重置；预算不爆发+暂停冻结+Clear/重开干净；六段与精英时刻。共享 EditMode 程序集不属本任务所有，未往里加测试。

## 给 Codex 的接线请求

1. 场景适配组件在 `CreateGameplayModules()` 中每局 `new EncounterModule(config)`，并在 Inspector 暴露 `EncounterConfig`。
2. `charger`/`dropper`/`elite` 外观与动画（D/Codex 复用现有资产）；`chaser`/`elite` 走宿主追击。
3. 宿主 `aliveCap` 若仍为 30，模块 48 的段上限会被宿主截断；请确认主场景值（任务卡口径 48）。
4. 冲刺部分重叠 tick 时，模块按重叠比例缩小速度保证总长度，但宿主扫掠把位移视为整 tick 匀速；误差 ≤1 tick，如需精确可在宿主按 ActivateAt 截段。
5. 建议在 `GameplayContext` 暴露上一步被拒绝的请求（或 Accepted 序号），便于模块区分“被拒”与“已出生”；目前只能靠 `SpawnRequest`/`SpawnWarnings` 推断。
6. 首领阶段降压由宿主 `bossOrdinaryCap` 覆盖；本模块不感知首领。

## 已知缺口

- 未在 Unity/主场景运行；数值未试玩；性能/GC 未测。
- 落波者无寻路，可能被地形卡住。
- 预算以上一步快照为准，同 tick 与 C 模块竞争时可能被宿主拒绝（会延后重试，不爆发）。
