# M01 机制规则适配（1号，TEST_ONLY）

基线：`origin/main` dc8b096d7529cc07298b2407928a73009e1072e6。分支 `feature/m01-mechanism-rules`。
只写 `Assets/Experiments/Mechanism/Rules/` 与本目录；未改 CombatModule / RunSimulation / GameLoop / Contracts / 2号目录。

## 程序集
- `Reverb.Experiments.Mechanism.Rules`（noEngineReferences，autoReferenced=false），显式引用 `Reverb.Experiments.Mechanism.Contracts`，使用共享 `MechanismMode`，未复制合同。
- 测试 `Reverb.Experiments.Mechanism.Rules.Tests`（Editor，UNITY_INCLUDE_TESTS）。
- 作者报告曾在自己的 dotnet 运行器验证；本分支未交付 `DotnetRunner~/`，不能据此要求接收者直接运行 dotnet test。仓库内提供 Unity EditMode 测试。

## API（命名空间 Reverb.Experiments.Mechanism.Rules）
| 成员 | 说明 |
|---|---|
| `new MechanismRules(MechanismMode, MechanismRulesConfig, IMechanismHost)` | 每局一个实例；配置在构造时 Clone，局内改原对象无效 |
| `bool ReportDirectHit(in DirectHitRecord)` | 上报**已扣血**的真实直接命中（运行时间、Serial/Generation、XZ 位置、实际伤害）。只排队，不再扣伤害。None 模式返回 false、不排队 |
| `void Tick(double now)` | 结算计划时间 ≤ now 的派生事件；暂停/升级/终局时宿主不调用即可 |
| `IReadOnlyList<DerivedDamageEvent> Events` | 本次 Tick 已结算事件（类型、目标、伤害、计划时间、起点、结果），下一 Tick 开头清空 |
| `Mode / EchoHits / PropagationHits / PendingCount` | 直接对应 `IMechanismPlaytestControls`；计数只算 Applied/Killed |
| `ActiveChainCount` | 未完成的直接命中预算数（测试用） |
| `Clear()` | 清空队列、预算、计数（正常重开应直接新建实例） |

数据结构：`MechanismVec2(X,Z)`、`MechanismTarget(Serial,Generation)`、`MechanismLiveTarget`、`DirectHitRecord`、`DerivedKind{Echo,Propagation,EchoPropagation}`、`DerivedDamageRequest`、`DerivedDamageResult{Applied,Killed,Missed,Invalid}`、`MechanismRulesConfig`（0.5×/0.5s、0.4×/0.1s/5m）。

宿主接口 `IMechanismHost`：
- `bool TryGetLiveTarget(MechanismTarget, out MechanismVec2)`：活且代号一致才 true。
- `void CollectLiveTargets(List<MechanismLiveTarget>)`：追加所有活目标。
- `DerivedDamageResult ApplyDerivedDamage(in DerivedDamageRequest)`：同步扣血；失效 Invalid，击杀 Killed。

## 语义
- 每次直接命中：最多 1 回声（原目标，0.5×初始伤害，+0.5s）、1 直接传播（+0.1s，以命中位置为圆心）、1 回声传播（回声计划时间 +0.1s，以回声时原目标位置为圆心，0.4×**初始**伤害，不乘 0.5）。传播不生任何子事件。
- 传播目标在结算时刻选：XZ 距离 ≤ 5m（含边界），排除原目标与本触发已传播目标，最近优先，同距 Serial 小者。
- 回声目标失效/已死则跳过且不生回声传播；宿主返回 Missed/Invalid 同样终止该分支。
- 时间锚定父事件计划时间；队列按（时间，入队序号）稳定排序；一次 Tick 内逐个应用后再选下一目标，不整批预计算 → 不会对已死目标重复击杀。
- 预算（Chain）子事件全部完成即回收进池；无历史日志。
- 伤害取整：四舍五入（远离零）；结果 ≤0 则不申请。
- 对照（基础 10，目标全活）：None 10 / Echo 15 / Propagation 14 / Combined 23；Combined 顺序 直击T1 → 0.1s 传播T2 → 0.5s 回声T1 → 0.6s 回声传播T3。

## 给 Codex 的最小接线需求
1. 开局（RunSimulation 新建 CombatModule 处）按下一局配置的模式新建 `MechanismRules`，宿主适配器桥接唯一 CombatModule：`TryGetEnemy(serial, gen, out e)` → 位置取 e.Position(x,z)；活敌遍历；`ApplyDerivedDamage` 走 CombatModule 现有扣血路径（不处理死亡/掉落）。
2. T6 `ProjectileStep` 每次真实命中后调用 `ReportDirectHit`（时间用运行时间，伤害用实际扣血量）。
3. T6 与 T7 之间调用 `Tick(runTime)`，使派生击杀走现有 T7 死亡与掉落。
4. 表现：读 `Events` 发 TEST_ONLY 事件（区分 Echo / Propagation / EchoPropagation，`From`→`TargetPosition` 画连线），不在表现回调回写伤害。
5. 暂停/升级/终局跳过所有战斗步骤即可；重开丢弃实例。
6. `IMechanismPlaytestControls` 的计数直接读本实例。

## 环境与测试
- Linux，.NET SDK 8.0.425（~/.dotnet），NUnit 3.14：16 项全部通过（四模式、Combined 顺序、None、三模式×粗细 dt 三种步长、单 Tick 缓冲、同距/半径、池代号失效、同帧重复死亡、回声目标死亡、配置拷贝、局间清理/预算回收）。
- **未安装 Unity，未在 Unity 编译或运行**；.meta GUID 为新生成，需 Unity 导入确认。

## 未验证 / 缺口
- 时间为 double，未改整数 tick；同帧多次命中由宿主按命中顺序上报。
- 排除已传播目标仅按 Serial（CombatModule 的 Serial 每次出池递增唯一）。
- 未做性能/GC 测量（队列为 List 插入，低数量试玩足够）。
- Missed 结果目前只在伤害取整为 0 时由本模块产生；宿主何时返回 Missed 由 Codex 决定。
