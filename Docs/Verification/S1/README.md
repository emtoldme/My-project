# S1 验收证据（F-01）

对应提交：`dfc93f0`（分支 `f01/unity-evidence`）。场景与 `.meta` 的生成、测试与打包均在同一提交上复核。

| 文件 | 内容 | 来源 |
|---|---|---|
| `2026-10-12-EditMode-66-passed.png` | Unity Test Runner 截图：EditMode 66 用例全绿、0 失败，Console 0 错误 | 用户在 Unity 编辑器内亲自运行并截图 |
| `2026-10-12-EditMode-TestResults.xml` | 同一次运行导出的 NUnit XML：总计 66、通过 66、失败 0 | 用户会话，`%LOCALAPPDATA%Low\DefaultCompany\My project\TestResults.xml` |
| `dfc93f0-EditMode-TestResults.xml` | 在 `dfc93f0` 工作区重跑 EditMode：66/66 通过 | 批处理：`Unity.exe -batchmode -nographics -runTests -testPlatform EditMode` |
| `dfc93f0-PlayMode-TestResults.xml` | 在 `dfc93f0` 工作区重跑 PlayMode：1/1 通过（`GameLoop_CanBeCreated_AndStartsPlaying`） | 同上，`-testPlatform PlayMode` |
| `2026-10-12-Player.log` | Development Build（`BuildOptions.Development`）实跑日志，含 `[Waffle] 开局 seed=12345 frame=0` | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\Player.log` |

## 两个 bug 的回归断言

`dfc93f0-EditMode-TestResults.xml` 中两条用例均为 Passed：

- `BugA_BaseMoveSpeed7_Is7AtLv0_And7Point4AfterOneUpgrade`：基础移速 7 在 Lv0 生效（实测值 7），升级一次后 7.4。
- `BugB_BaseHp200_FirstUpgradeGives225_AndHeals25`：基础 HP 200 时 `MaxHp`/`Hp` 均为 200，首次生命强化后 225 且回满 225，不再是 125。

## 截图与 git-lfs

`*.png` 由 `.gitattributes` 交给 git-lfs 管理。未安装 git-lfs 时克隆得到的是指针文本而非图片。本机已实测：

- 匿名克隆（`credential.helper` 置空，lfs `auth=none`）取回的文件为 410532 字节、PNG 魔数 `89 50 4E 47`，与作者本地一致；
- `SHA256 = CBF59310EAD7B883D46CBE310D18CE88A5CBF1B387EF71620ECAC29BD13B95F7`。

复核前请先确认 `git lfs version` 可用，否则请用 `git lfs install` 后重新 `git lfs pull`。

## 复现命令

```powershell
git fetch --prune origin
git switch -c f01/unity-evidence origin/f01/unity-evidence
# 编辑器内：Waffle → Setup S1 Scene / Add S1 Scene To Build Settings / Window → General → Test Runner
```

Development Build 由临时编辑器脚本调用 `BuildPipeline.BuildPlayer`（`BuildOptions.Development`）产生，实测 `result=Succeeded errors=0`；该临时脚本只在隔离副本中存在，未入库。