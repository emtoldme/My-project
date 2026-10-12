# M01 机制试玩场景

## R1 可读反馈（2026-10-12）

基线 `origin/main` `d143e7adefeb321c4b1fa6668f1cb493d4c8947e`。分支 `feature/m01-readable-feedback`。只改了 `Assets/Experiments/Mechanism/Presentation/` 和本文件。没有改场景、Editor、Integration、Rules、全屏组件或共享视图。本环境没有 Unity，下面是源码，不是实机通过。

用户看不懂向外扩散的双环。回声是 0.5 秒后**原来那个目标**再受伤，不是范围伤害。这一版把命中脉冲改成身上的紫红菱形，从大约 0.5 m 收到 0.1 m。旧的 `PlayEcho(x, z)` 仍然可用，但不再放大。

预备标记不显示伤害数字。Codex 用规则时间算出 `Progress`（0 到 1）后调用：

```csharp
fx.SyncEchoCues(cues); // 空列表会清掉标记
fx.PlayEcho(x, z, damage);                 // 已结算的回声，标签「回声 -5」
fx.PlayPropagation(fromX, fromZ, toX, toZ, damage); // 已结算的传播，标签「传播 -4」
```

`M01EchoCue` 的字段是 `Serial`、`Generation`、`X`、`Z`、`Progress`。同一 Serial+Generation 只留一个标记，池子最多 8 个，超出的丢掉。标记位置和大小只跟当次快照走，视图不累加 0.5 秒，也不会在 Progress 到 1 时自己扣血。目标从快照里消失、组件禁用或 `Restart` 都会清标记。

没调用过 `SyncEchoCues` 时，面板写“回声预备：等待规则快照”。不要用 `EnemyHit` 去猜标记或下一跳。传播仍是金色、从身体连到身体的短线，终点另有一个小落点。暂停时脉冲和标签冻结。颜色保持现有紫红 / 金色，可在组件上改半径和高度，不是已批准的正式色。

## Codex 集成后的入口（2026-10-12）

作者原始交付记录保留在下面；其中“等待接线”“没有场景/AudioDirector”“8/12 池”仅描述作者提交时，不能作为当前工程状态。

当前请用菜单 `Experiments/Setup Integrated M01 Playtest`，或执行 `Waffle.Experiments.Mechanism.EditorTools.M01IntegrationBatch.Setup`。它生成真实 M01 场景、规则桥、6/6 表现池和唯一 AudioDirector，启用真实派生伤害，使用紫红双环/金色定向连线。透明材质在 M01/Materials 内单独保存，不覆盖 E01。

独立构建入口为 `M01IntegrationBatch.Build`，通过 BuildPlayerOptions 显式只传 M01 场景，因此播放器可以重开；不必修改持久 Build Settings。模式配置跨重载保留，每局 CombatModule/桥/队列重新创建，同 seed 12345。XP/升级在本试玩冻结以固定武器数值。验收见 Docs/Integration/M01_ACCEPTANCE.md；技术证据见 Docs/Verification/M01/REVIEW.md。

## 作者原始交付记录（接线前的历史）

2026-10-12。基线 `origin/main` `dc8b096d7529cc07298b2407928a73009e1072e6`（Merge PR #23）。分支 `feature/m01-playtest-scene`。

只写了 `Assets/Experiments/Mechanism/Presentation/`、`Editor/`、`Scenes/` 和本目录。没有改 1 号规则、共享合同、正式 Core/Input/Views、E-01 资源、Build Settings 或包。本环境没有 Unity。菜单没跑，场景文件还没生成，没有播放，没有截图。

## 装配

菜单 `Experiments/Setup M01 Mechanism Playtest`。

```text
-executeMethod Waffle.Experiments.Mechanism.EditorTools.M01PlaytestSetup.Setup
```

只清空并保存 `Assets/Experiments/Mechanism/Scenes/M01_MechanismPlaytest.unity`。不调用 E01 的 Setup。若当前打开的就是 E01 场景，工具直接停。地面、地面材质、火花材质、George 和敌人 Prefab 都只读取已提交资产。成功日志：

```text
M01_PLAYTEST_SETUP_PASSED scene=... seed=12345 controls=waiting model=TEST_ONLY
```

场景不进 Build Settings。编辑器里重开走现有 `GameLoop.Restart`。独立播放器里重开会失败，除非 Codex 以后把该场景加进 Build Settings。本工具不加。

## 场景里有什么

- 一个 `GameLoop`、一个 `CombatViews`、现有 `RunHud`。没有 `VfxDirector`，没有 `AudioDirector`，没有 `E01WavePlayer`，没有 `DebugConsole`。调试控制台的文本框会吃掉 WASD，所以这场景不挂它。
- 相机沿用正交 Size 9、Pitch 55、Yaw 45。玩家根缩放 1，Visual 按约 1.8 m、脚底贴地摆，和 E01 同一套做法。模型仍是 TEST_ONLY 机甲，不是正式乡村。
- `seed` 固定 12345。刷怪改成每 2 秒 2 个，存活上限 8，敌人池 16，方向推进和包围圈数量为 0。这是为了在近处看见传播，不是 200 人压测，也不是平衡承诺。武器伤害和射速没改。
- 左上角仍是原来的 `RunHud`，用来处理暂停、升级三选一和胜负。机制面板不提供升级、存档或局外配置。`RunHud` 的“重开”只重载场景，不调用 `RestartWithMode`。接线之后，换模式要用下面四个按钮。

## 面板

`M01PlaytestPanel` 只在自己这个物体上找 `IMechanismPlaytestControls`。找不到，或 `IsReady` 为假：显示“等待机制接线”，回声 / 传播 / 待处理都是“—”，四个按钮不可用。不会把 0 写成已经统计过。

接上之后显示 `Mode`、`EchoHits`、`PropagationHits`、`PendingCount`，按钮分别以 None / Echo / Propagation / Combined 调用 `RestartWithMode`。面板不写 HP，也不自己走时钟。

## 回声环和传播线

`M01DerivedFx` 在 `Awake` 里做 8 个双线环和 12 条短线，射击时不 `Instantiate`。

- 现在谁都不调用 `PlayEcho` / `PlayPropagation`，所以开局看不见这些线。
- 不读 `Shot` 或 `EnemyHit`。普通命中仍只由 `CombatViews` 闪一下。不要把闪白或 E01 波纹当成回声。
- 只在 `Restart` 时把池清掉。
- 线段自己的 0.28 秒和 0.16 秒是画出之后的淡出，不是规则里的 0.5 秒回声或 0.1 秒传播。暂停、升级、终局不加 `LastDt`。
- 颜色是琥珀色双环和橙色短线，从火花材质复制出来再改色，不改 E01 材质本身。这不是用户批准的色板。两点距离小于 0.05 m 的线不画，避免变成一个点。

## 请 Codex 接这些

适配组件要挂在同一个 `GameLoop` 物体上，并实现已有接口。未接上唯一 `CombatModule` 时保持 `IsReady = false`。不要再复制一份同名接口。`RestartWithMode` 应走现有场景重载，种子仍是 12345，并清掉上一局的队列和计数。

现有 `PresentationEvent` 只有一个 XZ 和 `Data`，不够区分派生表现。不要把 `EnemyHit` 送进 `PlayEcho` 或 `PlayPropagation`。需要在规则步骤里结算之后，由唯一分发处调用：

| 调用 | 时机 | 还缺的字段 |
|---|---|---|
| `PlayEcho(x, z)` | 回声伤害已经打到活目标 | 目标 XZ、Serial、Generation、来源序号、实际伤害 |
| `PlayPropagation(fromX, fromZ, toX, toZ)` | 传播伤害已经打到另一个活目标 | 起点和终点、目标 Serial、Generation、来源序号、实际伤害 |

回声目标失效、或传播被规则丢掉时，不要调用。视图不负责再判 5 米、0.5 倍或 0.4 倍。伤害和时间仍只由 `CombatModule` 决定。
