# F-01 框架修复与交接（2 号）

## 基线与提交

- 基线：`s1-framework@cd060ad04557305cf85a088529737e3e580967db`（开工前 `git fetch`，origin 未移动，无 rebase）
- `3b6d582` 协作：加入 AGENTS.md 与任务板（原样复制）
- `23ad8d4` 修复：升级数值以 RunConfig 为唯一来源，补回归测试
- `40071b3` 测试：新建 Waffle.Tests.Integration 程序集与占位冒烟测试
- 本文件与任务板状态更新所在的提交见 `git log`（在 40071b3 之后）

## 修复内容

用户在 Unity 中发现的两个问题：
- (a) 修改 Config 的基础移速没有效果；
- (b) 基础 HP 设为 200，第一次“生命强化”后最大 HP 变成 125。

根因：`UpgradeRules.cs` 里写死了绝对数值表（移速 5.0/5.4/5.8/6.2、HP 100/125/150/175/200 等），`RunSimulation` 移动时读表（忽略 `RunConfig.moveSpeed`），生命强化时把 MaxHp 直接设为表值（忽略 `RunConfig.maxHp`）。

修复：
- `RunConfig` 成为唯一来源：基础值 `maxHp`、`moveSpeed`、`damage`、`fireInterval`、`pierce`、`magnetRadius`、`dodgeCooldown`；新增可序列化的 `upgrades`（`UpgradeConfig`）保存每级增量数组、`maxHpUpgradeHeal`、`healFraction`。
- 数值 = 基础值 + 前 N 级增量之和；数组长度 = 最高等级。默认配置下与 v0.2.1 表 U-1 完全一致（移速 +0.4/级，HP +25/级等）。
- `UpgradeRules` 不再保存任何数值表；`RunSimulation.CurrentValue(kind)` 按当前等级从 Config 计算。
- 已检查：`GameLoop.config` 这个序列化对象被直接传给 `RunSimulation`，没有第二份配置副本（PlayMode 冒烟测试也断言 `AreSame`）。

新增：`Assets/Waffle/Tests/Integration/`（`Waffle.Tests.Integration.asmdef`，平台不限、引用 Runtime + Rules + TestRunner + nunit，可跑 PlayMode）和一个占位冒烟测试。按 AGENTS，之后此目录归 1 号。

## 测试证据（仅 dotnet，非 Unity）

环境：本机 .NET 8 SDK + NUnit 3.14，工程 `Tools/RulesTests`（编译 `Scripts/Rules/*.cs` 与 `Tests/EditMode/*.cs`，不含 Unity 侧脚本和 Integration 测试）。

```
Passed!  - Failed:     0, Passed:    66, Skipped:     0, Total:    66
```

新增回归用例（UpgradeConfigTests）：基础移速 7 → Lv0 实测 7、一次升级后 7.4；基础 HP 200 → 一次生命强化 MaxHp 225（满血时 HP 225，HP 100 时 125）；回复按 Config 的 MaxHp×30%；默认配置等于表 U-1 全部数值且总等级 23；伤害/射速/穿透/拾取范围/闪避冷却都使用 Config 基础值；修改增量数组生效；RunSimulation 与传入的 Config 是同一对象。

**未在 Unity 中编译或运行过。** Unity 侧脚本、asmdef、Integration 冒烟测试都没有 Unity 证据。

## 需要用户在 Unity 中完成并提交的证据

1. 用 Unity 6000.4.3f1 打开工程，等编译完成；确认 Console 无编译错误。Unity 会为所有新文件/目录生成 `.meta`，**请提交这些 .meta**（仓库里目前没有）。
2. 菜单 **Waffle → Setup S1 Scene**，生成 `Assets/Waffle/Scenes/S1_Graybox.unity`，提交场景及其 `.meta`。
3. Test Runner：EditMode 全部运行（`Waffle.Tests.EditMode`），PlayMode 运行 `Waffle.Tests.Integration`；截图或导出结果。
4. 在 S1_Graybox 里把 Config 的 Move Speed 改成 7、Max Hp 改成 200 运行，确认两个问题已修复。
5. （F-10）菜单 **Waffle → Add S1 Scene To Build Settings**（会修改 EditorBuildSettings），出 Development Build 运行一次，附 Player.log。

## V-01 交接给 1 号

- 测试对象：`s1-framework` 上**包含用户提交的 .meta 与 S1_Graybox 场景之后的那个提交**（在此之前只能测试代码，无法测试场景）。代码基线最早可用 `40071b3` 之后的本交接提交。
- 可写路径：`Assets/Waffle/Tests/Integration/`、`Docs/Verification/`。不修改 Core/Input/UI/Debug/Rules、asmdef、输入资源、场景、ProjectSettings。
- 验证范围（任务板 V-01）：Playing/Paused/LevelUp/Victory/Defeat 各状态重开、暂停输入泄漏、连续升级、失焦、关闭 Domain Reload、构建后运行。
- 失败报告：写在 `Docs/Verification/`，每条包含：测试提交号、操作步骤、期望、实际、日志/截图、是已复现还是静态推断；需要改共享文件的，提出复现与期望，由 2 号修复，不另造状态机或输入入口。
