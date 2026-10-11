# V-01 交接：暂停 / 输入 / 重开独立验证（给 1 号）

发出人：2 号（框架与整合负责人）。依据：`AGENTS.md`、`Docs/Integration/TASK_BOARD.md`。

## 1. 验证基线
- 分支 `s1-framework`，提交 `0b7fa60`（已含 PR #2 Unity 证据、PR #3 IMGUI 缩放、ImguiScale.cs.meta）。
- 远端 `main@baf7de7` 已由用户合入同一内容；V-01 仍以 `s1-framework@0b7fa60` 为准。
- 正式入口：`Waffle.GameLoop → RunSimulation → RunState`，输入 `GameInput`，场景 `Assets/Waffle/Scenes/S1_Graybox.unity`。
- 已有证据：`Docs/Verification/S1/`（EditMode 66/66、PlayMode 1/1、Development Build Player.log，均为用户在 Unity 6000.4.3f1 实测）。

## 2. 工作方式
- 使用你自己的工作目录和功能分支（建议 `v01/verification`，从 `0b7fa60` 拉出），不要与 2 号共用 Library/Temp。
- 可写路径只有：`Assets/Waffle/Tests/Integration/`、`Docs/Verification/`。程序集 `Waffle.Tests.Integration` 已由 2 号创建，之后归你维护。
- 不修改 `Core/`、`Input/`、`UI/`、`Debug/`、`Rules/`、`Editor/`、场景、`.asmdef`（Integration 测试程序集除外）、`InputSystem_Actions.inputactions`、`Packages/`、`ProjectSettings/`。
- 不接入 `Survivor.GameManager`、`PauseActions`、`Task1SceneBootstrap`；`feature/task1-game-state` 只作参考，其中有价值的测试按统一框架改写后放进 Integration。
- 不 push main，不强推任何分支。

## 3. 验证范围（TASK_BOARD V-01）
逐项给出操作、预期、实际、证据：
1. 五种状态 Playing / Paused / LevelUp / Victory / Defeat 下执行重开，重开后计时、HP、等级、XP、待升级、面板、timeScale、输入 Map 全部回到初始（A-80–A-87）。
2. 暂停输入泄漏：暂停期间按闪避、移动，恢复后不补触发（F-4）。
3. 连续升级：`givexp 30` 依次出现 3 个面板，结果 Lv4、6/14（A-61）；面板上暂停再继续，选项不变（A-63）。
4. 失焦：Game 视图失焦自动暂停，恢复后计时不跳（A-75）。
5. 关闭 Domain Reload（Enter Play Mode Options）后重复 1–4，确认无静态残留。
6. Development Build 运行后重复 1–3。
7. 终局同帧：`hurt` / `settime` 组合验证 A-77a/b/c、A-79b（同帧死亡与 3:00 判失败；升级与 3:00 同帧判胜利且不弹面板）。

## 4. 交付格式
- 结果写入 `Docs/Verification/V-01/`：每项注明被测提交号、步骤、预期、实际、截图或日志、状态（已复现 / 静态推断）。
- 自动化测试放 `Assets/Waffle/Tests/Integration/`，附 Test Runner 导出的 XML。
- 新文件连同 Unity 生成的 `.meta` 一起提交。
- 发现框架问题：不要改共享文件，提交复现步骤、期望、实际和失败测试，由 2 号修复。
- 开 PR 到 `s1-framework`，正文列出基线、提交号、测试结果与未验证项。

## 5. 之后
V-01 通过并经本聊天复核后，2 号整理 C-00 战斗接入接口（T3–T8、T10、T13 的输入输出、伤害与 XP 提交、暂停不推进、重开清理），合入后 1 号再开始 C-01。
