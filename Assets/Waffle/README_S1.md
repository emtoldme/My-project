# Waffle S1 最小可运行框架

对应《Waffle S1 玩法规则与测试方案 v0.2.1》结尾 §5 的“最小可运行框架”：GameLoop（T0–T13）、RunState 状态机、输入 Map 切换、计时与 dt 上限、暂停（含失去焦点）、XP 与待升级队列、升级三选一占位面板、最小调试钩子、结算与重开。**不包括**敌人、武器、宝石（对应步骤只打出“未实现”标记）。

## 在 Unity 里第一次打开

1. 用 Unity 6000.4.3f1 打开工程，等待编译。第一次打开会为新文件生成 `.meta`，**请把这些 `.meta` 一起提交**（仓库里没有手写 `.meta`）。
2. 菜单 **Waffle → Setup S1 Scene**：自动生成 `Assets/Waffle/Scenes/S1_Graybox.unity`（灯光、60×60 场地、胶囊玩家、斜俯视相机、GameLoop/HUD/调试控制台），并把 `Assets/InputSystem_Actions.inputactions` 赋给 GameLoop。不会改 SampleScene。
3. 打开 S1_Graybox，按 ▶ 运行。
4. （可选，做 Development Build / F-10 时需要）菜单 **Waffle → Add S1 Scene To Build Settings**。这一步会修改 `ProjectSettings/EditorBuildSettings.asset`，所以没有自动执行。编辑器里不加也能重开。
5. 运行测试：**Window → General → Test Runner → EditMode → Run All**（`Waffle.Tests.EditMode`）。

## 操作

| 键 | 作用 |
|---|---|
| WASD / 左摇杆 | 移动（相机相对，5 m/s，夹在 ±29.5 m） |
| Space / 手柄 B(East) | 闪避（占位：只记录请求次数，Overlay 显示“闪避请求”） |
| Esc / 手柄 Start | 暂停 / 恢复 |
| 1 / 2 / 3（或鼠标点按钮） | 升级面板选项 |
| F1 | 显示/隐藏调试 Overlay 和控制台 |

调试命令（在右上角输入框输入后回车）：`tracetick [on|off|n]`、`settime s`、`setdt s`（0 = 关）、`sethp n`、`givexp n`、`god [on|off]`、`hurt n`（下一个 tick 的 T8 结算一次伤害，代替敌人接触）、`slowmo x`、`help`。

## 结构

```
Assets/Waffle/
  Scripts/
    Rules/   纯 C#（Waffle.Rules.asmdef，noEngineReferences = true，不能引用 UnityEngine）
      RunPhase / RunState      状态机，唯一切换入口 TryTransition，§8.6 切换表
      RunSimulation            每帧一次 Tick：T0→T13 固定顺序、门控、终局优先级、升级面板、调试钩子
      XpRules                  5 + 3(n−1)，溢出与连续升级
      UpgradeRules             表 U-1、抽 3 张、不足时“回复”补位
      RunConfig                参数（设计初值）+ 3 个待决策开关
      DebugCommands            调试命令解析
      InputSnapshot / IRunLog
    Core/    GameLoop（场景里唯一有玩法 Update 的脚本）、CameraFollow、UnityRunLog
    Input/   GameInput：Player / UI 两个 Map 切换，T0 轮询
    UI/      RunHud：IMGUI 的 HUD、暂停、升级、结算面板
    Debug/   DebugConsole：Overlay + 命令输入
  Editor/    S1SceneSetup：菜单 Waffle/Setup S1 Scene
  Tests/EditMode/  NUnit 测试（只引用 Waffle.Rules）
Tools/RulesTests/  脱离 Unity 用 dotnet 跑同一批测试的工程（Unity 不会导入这里）
```

输入资源 `Assets/InputSystem_Actions.inputactions` 的改动：Player Map 新增 `Dodge`（Space、手柄 East）和 `Pause`（Esc、手柄 Start）；UI Map 新增 `Pause`、`Choose1/2/3`（1/2/3、小键盘、手柄十字键）。原有动作没动。

## 已按推荐默认值实现的待决策项（在 GameLoop 的 Config 里可改）

| 设置 | 默认 | 说明 |
|---|---|---|
| `defeatWinsSameTick` | true | 同一 tick 死亡并到 3:00 判失败（§5-1） |
| `stackContactDamage` | false | 接触伤害不叠加（§5-2）；本阶段没有敌人，只保存设置 |
| `gemLifetime` | 20 | 宝石寿命（§5-3）；本阶段没有宝石，只保存设置 |

## F-1 ~ F-10 覆盖情况

| 项 | 实现 | 自动测试（EditMode / dotnet） | 需要在 Unity 里确认 |
|---|---|---|---|
| F-1 tick 顺序 | `tracetick` 输出 T0…T13；非 Playing 只有 T0 | A05_* | Console 日志顺序 |
| F-2 唯一入口、拒绝切换 | `RunState.TryTransition` + 切换表；终态拒绝调试命令写 Error | TransitionTable_*、A79b_* | 日志中的红色 Error 是预期的 |
| F-3 暂停 | Paused 时不执行 T1–T13；dt 上限 0.05；失焦自动暂停；timeScale 由状态决定 | A70_*、A74_*、A75_*、DtIsCapped… | A-73 暂停面板按钮可用；A-74 Alt-Tab；连按 20 次 Esc 后 Overlay 的 timeScale 与状态一致 |
| F-4 输入切换 | Playing 只开 Player Map，其他只开 UI Map；离开 Playing 清空闪避缓冲 | F4_* | Overlay 的 Map 状态；暂停时按 Space，恢复后“闪避请求”不增加 |
| F-5 XP 与队列 | XpRules + 待升级计数 | 表 X-2 全部用例、A61_*、F5_* | `givexp 30` 连弹 3 张面板，最终 Lv4 6/14 |
| F-6 面板 | 同帧多次提交只算一次；0.3 s 防误触；暂停回同一张面板 | A62_*、A63_* | 鼠标与数字键同时提交 |
| F-7 计时与终局 | T9 在 T11 之前；同 tick 升级+到时只胜利 | A76_*、F7_*、A77a/b/c_*、A78_*、A79_* | 用 `setdt 0.05`、`sethp 0`、`settime 179.99` 复现 |
| F-8 重开 | 重新加载场景；重开前 timeScale = 1、关闭 Map；没有可变 static | Restart_*、A84_*（规则层） | A-80~A-87：各状态重开、HUD 复位、Map 状态、关闭 Domain Reload 后再测 |
| F-9 日志 | 状态切换（旧→新、now、帧号、timeScale）、升级（面板选项、所选）、受击、终局、重开、拒绝切换 | 部分（日志内容由测试断言） | Console / Player.log；每秒 CSV 尚未实现 |
| F-10 Development Build | — | — | 全部需要在 Unity 中执行 |

## 已知限制 / 需要在 Unity 里检查

- 本仓库的 Unity 侧脚本（Core/Input/UI/Debug/Editor）**没有在 Unity 中编译或运行过**，只做过人工检查。
- 调试控制台输入文字时，Player Map 仍然打开，WASD 会让角色移动；灰盒阶段可接受。
- IMGUI 默认字体显示中文依赖系统字体，若出现方块请告知。
- `InputSystem_Actions` 是项目级 Actions（会在启动时全部打开），GameInput 初始化时会先关闭全部 Map 再按状态打开。
- 每秒 CSV、`seed` 命令等其他调试钩子不在本阶段范围。
