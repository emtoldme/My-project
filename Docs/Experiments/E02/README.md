# E-02 装备组合技术实验（TEST_ONLY）

> 技术夹具，不是最终装备设计；未接入正式规则、升级或局外装备。

- 基线：origin/main `9c565ee0ca87c6858145e5f656996aa7711f916b`
- 分支：`experiment/e02-effect-combination`
- 写入范围：`Assets/Experiments/Equipment/`、`Docs/Experiments/E02/`（未改 Rules/Combat、RunSimulation、场景、共享程序集）

## 结构

| 文件 | 说明 |
|---|---|
| `Runtime/EffectComboLab.cs` | 纯 C# 夹具：`ComboConfig`（最小配置）、`ComboRun`（每局独立状态）、`TargetHandle`（Id+Generation）、`ComboRecord`（只读结算记录）、`ComboLabScenario`（none/A/B/A+B 对比入口） |
| `Waffle.Experiments.Equipment.asmdef` | `noEngineReferences: true`，无引用，`autoReferenced: false`（正式程序集不会引用它） |
| `Tests/EffectComboLabTests.cs` + `Tests/*.asmdef` | 6 个 NUnit 检查；Unity 下为 Editor 测试程序集（未在 Unity 验证） |
| `DotnetRunner~/Tests`、`DotnetRunner~/Print` | 盒子上 dotnet 运行入口；目录以 `~` 结尾，Unity 不导入 |

## 运行入口

```bash
cd Assets/Experiments/Equipment/DotnetRunner~
dotnet test Tests          # 自动检查
dotnet run --project Print # 打印 none/A/B/A+B 结果序列
```

## 语义

- 直接命中（Direct，深度 0）立即结算。
- 模块 A（延迟回声）：Direct 后 `EchoDelay` 秒对同目标造成 `EchoDamage`（深度 1）。回声不再产生回声。
- 模块 B（单次传播）：Direct 后 `PropagationDelay` 秒对范围内最近的另一个存活目标造成 `PropagationDamage`（同距离按 Id 小者）。传播不再派生。
- A+B：回声结算后再引发一次传播（深度 2），且同一触发内不会传到已传播过的目标。
- 上限：每次触发派生事件 ≤ `MaxEventsPerTrigger`（默认 4），深度 ≤ `MaxDepth`（默认 2）；派生规则固定、非递归。
- 队列：按（时间, 序号）确定性排序；无随机数，同输入同输出。
- 每条记录带：序号、父序号、触发代号 TriggerId、时间、类型、深度、来源句柄、目标句柄、伤害、剩余 HP、结果。
- 暂停：`Paused=true` 时 `Tick` 不推进时间、不结算。
- 回池/换代：`Recycle`/`Respawn` 使 Generation+1；排队事件按旧句柄结算为 `SkippedStale`，不误伤新实例；目标已死为 `SkippedDead`。
- 重开：`Restart()` 清队列、日志、时钟、序号；目标恢复初始 HP 且 Generation 递增，旧局句柄失效。
- 不使用 PresentationDispatched，也没有任何回写正式战斗的路径。

## 结果（dotnet 实际输出，场景：T1@0、T2@2、T3@3、T4@20，HP 30，命中 T1，推进 2s）

```
== none : events=1 totalDamage=10 pending=0
t=0.00 #1 trig=1 Direct d0 T1g1->T1g1 dmg=10 hp=20 Applied parent=0
== A : events=2 totalDamage=15 pending=0
t=0.00 #1 trig=1 Direct d0 T1g1->T1g1 dmg=10 hp=20 Applied parent=0
t=0.50 #2 trig=1 Echo d1 T1g1->T1g1 dmg=5 hp=15 Applied parent=1
== B : events=2 totalDamage=14 pending=0
t=0.00 #1 trig=1 Direct d0 T1g1->T1g1 dmg=10 hp=20 Applied parent=0
t=0.10 #2 trig=1 Propagation d1 T1g1->T2g1 dmg=4 hp=26 Applied parent=1
== A+B : events=4 totalDamage=23 pending=0
t=0.00 #1 trig=1 Direct d0 T1g1->T1g1 dmg=10 hp=20 Applied parent=0
t=0.10 #3 trig=1 Propagation d1 T1g1->T2g1 dmg=4 hp=26 Applied parent=1
t=0.50 #2 trig=1 Echo d1 T1g1->T1g1 dmg=5 hp=15 Applied parent=1
t=0.60 #4 trig=1 Propagation d2 T1g1->T3g1 dmg=4 hp=26 Applied parent=2
```

## 自动检查（dotnet test）

`Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6`

1. none/A/B/A+B 序列与总伤害各不相同（10/15/14/23），远处目标不受影响
2. 同输入完全一致；各局状态独立
3. 50 次连续命中（零延迟）每个触发不超过次数/深度上限，队列清空
4. 暂停不推进，恢复后正常结算
5. 目标回池/换代后排队的回声与传播不误伤
6. 重开清空队列/日志/时钟，旧句柄失效

## 执行环境

- Linux 容器，.NET SDK 8.0.425，net8.0，NUnit 3.14.0 / NUnit3TestAdapter 4.6.0
- **Unity 未安装，未在 Unity 6000.4.3f1 中导入、编译或运行 EditMode 测试。**

## 未验证项

- Unity 中两个 asmdef 是否正常编译；Tests asmdef 的 `nunit.framework.dll` 预编译引用写法沿用常规 Test Framework 配置，未实测。
- `Assets/Experiments.meta`（父目录）不在本人写入范围，未提交；由 Codex 导入时生成/提交。
- 本目录所有 .meta 为手写新 GUID，未经 Unity 重新序列化。
- 时间用 float 累加；长时间运行的精度未评估（正式接入建议用整数帧/tick）。
- 只有一维位置与最近目标选择，未对接真实空间查询或安全距离。
- 性能/GC 未测（队列为 List 线性选最小，仅适合夹具规模）。

## 未来接入正式规则的最小接口请求（由 Codex 决定是否/如何接线）

1. **目标句柄**：正式敌人需暴露稳定 `Id + Generation`（回池/重生递增），以及 `IsAlive(handle)` 查询。
2. **伤害入口**：规则层内一个 `ApplyDamage(handle, amount, sourceKind, triggerId)`，在 RunSimulation 固定步骤内调用，不经 PresentationDispatched。
3. **命中事件源**：CombatModule 在规则步内产出“直接命中”（目标句柄、时间、子弹/触发 Id），供效果队列消费。
4. **合法目标查询**：`FindNearestAlive(position, range, exclude)`，确定性 tie-break。
5. **时钟与生命周期**：效果队列由 RunSimulation 的暂停感知时钟推进；重开时与 RunState 一同重建。
6. **只读表现输出**：结算记录（类型/来源/目标/触发 Id/深度）作为只读表现事件发出，表现层不得回写。
7. **配置**：正式版用 ScriptableObject 包装 `ComboConfig` 字段，运行时状态独立对象（符合 REVERB_CONTEXT_V1）。
