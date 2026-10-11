# V-01 验证报告

被测代码：`s1-framework@0b7fa60`（PR #2/#3/#4 之后的框架）。
分支起点：`s1-framework@446e6c8`。它只比 `0b7fa60` 多了 PR #5 的交接文档，没有再改规则或场景。
`main` 实测为 `baf7de7`，不含 PR #5。本报告没有改 `main`，也没有改 `feature/task1-game-state@f5e6dc42`。

执行环境：没有 Unity Editor。规则层用例在 .NET SDK 8.0.425 上跑过，8 个通过、0 个失败。那次运行用的是与 `446e6c8` 上 `Assets/Waffle/Scripts/Rules` 同逻辑的本地副本，不是 Unity Test Runner，也不是 Development Build。仓库里的 `V01RulesTests.cs` 还没有在 Unity 里编译或运行。

状态只用两种：已复现、静态推断。没有 Unity 结果的项目一律不是已复现。

## 结果

| 项 | 状态 | 实际 |
|---|---|---|
| 暂停期间移动、闪避不生效，恢复后不补记闪避 | 已复现（规则层） | `PausedInput_DoesNotMoveOrDodge_AndDoesNotReplayAfterResume` 通过。暂停帧和暂停中的 dodge/move 不增加 `DodgeRequests`，不改变位置，`now` 保持 0。恢复帧按暂停态快照会把阶段切回 Playing，但闪避次数仍是 0。 |
| `givexp 30` 后 Lv4、6/14，三张面板 | 已复现（规则层） | 第一帧后 `Level=4`、`Xp=6`、`XpToNext=14`、`PendingLevelUps=3`、`PanelsShown=1`。选完 3 次后回到 Playing，待升级 0，等级和 XP 不变，`PanelsShown=3`。 |
| 升级面板上暂停再继续，选项不变 | 已复现（规则层） | 暂停前后 `CurrentOptions` 相同。恢复同一帧带上选择键时，0.3 秒防误触拒绝这次选择，待升级仍是 3。 |
| 同帧死亡且到 3:00 | 已复现（规则层） | `settime 179.99` + `hurt 100` + dt 0.05 → Defeat，原因 `hp<=0`，`now` 停在 179.99，不是 Victory。 |
| 只死亡 | 已复现（规则层） | `hurt 100` → Defeat，`now` 停在本帧开始时的 0。 |
| 只到时 | 已复现（规则层） | `settime 179.96` + dt 0.05 → Victory，原因 `timeout`，`now=180`，HP 仍是 100。 |
| 升级与 3:00 同帧 | 已复现（规则层） | `givexp 30` + `settime 179.96` + dt 0.05 → Victory，等级 4、XP 6，待升级被丢掉，`PanelsShown=0`，没有面板。 |
| 终态拒绝 `givexp` | 已复现（规则层）+ 一处文案是静态推断 | 数值不变化：仍是 Defeat、Lv1、XP 0，`RejectedTransitions` 增加。`DebugCommands.Execute` 仍返回“排队 +30 XP”。这句文案没有改状态，见下方变更请求。 |
| 新的一局不继承上一局 | 已复现（规则层） | 新的 `RunSimulation` 是 Playing、Lv1、XP 0、HP 100、`now` 0。这只说明规则对象没有可变 static，不能代替关 Domain Reload。 |
| 五种状态的场景重开 | 静态推断 | `GameLoop.Restart` 不看当前状态：先 `timeScale=1`、`Controls.DisableAll()`，再按 Build Settings 重载场景。新对象在 `Awake` 里新建 `RunSimulation`。`RunHud` 只在 Paused、Victory、Defeat 画了重开按钮。Playing 和 LevelUp 没有重开按钮。LevelUp 可以先暂停，暂停面板上有重开。 |
| 输入 Map 不把暂停期间的闪避漏到恢复后 | 静态推断 | `GameLoop.Update` 先按当前状态 `GameInput.Read`，再 `Tick`。暂停时只读 UI Map，快照里没有 Dodge。因此恢复帧的快照不会带上暂停期间的闪避。本环境没有输入设备，没有跑这条 Unity 路径。 |
| 失焦自动暂停 | 静态推断 | `OnApplicationFocus(false)` 调用 `RequestPause`。只对 Playing 和 LevelUp 生效。重新获得焦点不会自动恢复。终态和已经暂停时什么都不做。没有做 Alt-Tab，也没有区分 Editor Game 视图和播放器。 |
| 关闭 Domain Reload | 静态推断 | 读过的框架脚本里，局内数据都在实例上。`ImguiScale.Scale` 是静态，但每次 `OnGUI` 会重写。`GameLoop.Awake` 会把 `timeScale` 设回 1。没有实际关闭 Domain Reload 再进 Play Mode。 |
| Development Build | 静态推断 | 没有打播放器。`Docs/Verification/S1/` 里的 Player.log 是 F-01 的证据，不能算这次 V-01。 |

## 给 2 号的变更请求

不改共享文件。两项都未在 Unity 里点过。

1. 终态调试文案。复现：先 `hurt 100` 进入 Defeat，再执行 `givexp 30`。期望：返回拒绝。实际：等级和 XP 不变，但 `DebugCommands.Execute` 仍返回“排队 +30 XP”。位置：`Assets/Waffle/Scripts/Rules/DebugCommands.cs` 的 `givexp` 分支，它在 `DebugGiveXp` 已经拒绝之后仍返回成功句子。
2. 重开入口。若五种状态都要从界面重开，Playing 和 LevelUp 的 `RunHud` 目前没有按钮。`GameLoop.Restart` 本身可以在这五种状态调用。

## 仍需在 Unity 6000.4.3f1 里做

打开 `v01/verification`，不要开 `feature/task1-game-state`。

1. Test Runner 运行 `Waffle.Tests.Integration` 里的 `V01RulesTests`。这才是对着仓库源码的编译和运行。导出 XML 后补进本目录。
2. 打开 `Assets/Waffle/Scenes/S1_Graybox.unity`。在 Playing、Paused、LevelUp、Victory、Defeat 分别重开，核对计时、HP、等级、XP、待升级、面板、timeScale 和 Map。
3. 暂停时按 Space 和 WASD，恢复后看 Overlay 的“闪避请求”不增加，角色不补移动。
4. 调试框输入 `givexp 30`，连续三张面板，最终 Lv4、6/14。面板打开时暂停再继续，三个选项不变。
5. Game 视图失焦、关闭 Domain Reload、Development Build 各重复对应项。
6. `settime 179.99` 后 `hurt 100`，应为失败而不是胜利。`settime 179.96` 且同时 `givexp 30`，应为胜利且不弹面板。

## 本地命令记录

.NET SDK 8.0.425。结果：Passed 8，Failed 0，Skipped 0。没有把本地 TRX 提交进仓库，因为它包含本机路径。
