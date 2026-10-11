# P-02 回复纪要（给 Codex）

这是 2 号给用户的回复记录，不是游戏内试听结论，也不是编译结论。技术细节以同目录 `P-02_HANDOFF.md` 为准。本文件只供 Codex 阅读，不要求用户操作。

基线：`origin/main` `b6df1fda73c6b0cf5d1e25227c2114a87dba0564`（Merge PR #8）。
分支：`feature/p02-presentation-polish`。未合并。

| 提交 | 说明 |
|---|---|
| `562ad709f0f18198a914b51d84baf4fb9528ad11` | 音频生命周期第一版 + `P-02_HANDOFF.md` |
| `8851355aa30b9c83b840347fe33fd12653f27c6e` | 暂停恢复后升级音乐仍能交叉淡入；终局不再被 `LevelUpShown` 拉回 |
| 本文件所在提交 | 把给用户的回复写成仓库文件 |

写过的路径只有：

- `Assets/Waffle/Scripts/Views/Audio/AudioDirector.cs`
- `Docs/Integration/P-02_HANDOFF.md`
- `Docs/Integration/P-02_REPLY.md`（本文件）

没有改场景、Editor 生成器、VfxDirector、GameLoop、Rules、音效规格或主美资源。没有新脚本，没有新的 `.meta`。没有第二个音频管理器。

## 回复 1（当时告诉用户的内容）

分支已推送。当时请用户在创建 PR 的确认卡上提交，所以那一刻 PR 还不存在。
告诉用户的事实：

- 基线是上面的 `b6df1fda`。
- 当时的两个提交是 `562ad709` 和 `8851355`。
- 当时只改了 `AudioDirector.cs` 和 `P-02_HANDOFF.md`。
- 没有 Unity，没有编译，没有试听。
- 哪些事件现在能响、哪些等 C-01，以及要回归的短流程，写在 `P-02_HANDOFF.md`，不要把代码存在当成试听通过。

之后用 GitHub 搜索 `head:feature/p02-presentation-polish`，结果 0 条。确认卡没有变成 PR。

## 回复 2（本文件）

用户要求把每次回复的精简完整报告上传到 GitHub 给 Codex 看。本文件就是那份报告。

### 代码实际做了什么

仍是原来的 `AudioDirector`。

- 升级：只有第一次 `LevelUpShown` 做 1 秒交叉淡化。连续升级只播出现音。战斗循环淡到静音后 `Pause` 播放头；最后一次 `LevelUpChosen`（分发时 `PendingLevelUps == 0`）再 `UnPause`，1.4 秒淡回。没有按 120 BPM 跳到小节。
- 暂停：声部、两条音乐、淡变、音效间隔、拾取音阶、受击压低的保持时间一起冻住。恢复不补播一次性音效。已停住的战斗循环不会被提前解开。
- 必须回归的点：战斗中升级循环音量是 0，暂停会把它一并 `Pause`。恢复时只要还没进终局就 `UnPause`。漏掉的话，暂停一次后再 `givexp`，悬浮层没有声音。
- 胜利：2 秒淡出 + 胜利音，淡完 `Stop` 一次（含已经 Pause、`isPlaying == false` 的声源）。
- 失败：0.3 秒淡出，低通扫到 200 Hz，失败音。新一局把低通直接设回 22000 Hz。
- 终局后 `LevelUpShown` 不再 `EnterLevelUp`，受击不再重新压低。
- `Restart` 只清场，不在即将销毁的对象上开新音乐。`BeginFreshRun` 在 `Play` 前 `Stop` + `UnPause`，避免暂停标记让新一局静音。场景重载成功时不要再调它；只有重载失败、旧对象还在时才调一次。
- 禁用组件会退订并 `Stop`。再启用走 `BeginFreshRun`，不继承旧状态。

### 现在会响 / 等 C-01

会响：`Dodge`、`PlayerHurt`（调试 `hurt`，无敌时不发）、`LevelUpShown`、`LevelUpChosen`、`Paused` / `Resumed`（无独立剪辑；终局发不出暂停）、`Victory`、`Defeat`、`Restart`（只清场）。
等 C-01，main 上的 `RunSimulation` 还没有 `Emit`：`Shot`、`EnemyHit`、`EnemyKilled`、`GemPickup`。
C-01 请用现有 `Emit`，不要新建音频管理器，也不要在动画或特效 Prefab 上再播这些事件。

`LevelUpChosen` 依赖 `GameLoop.Choose` 在 `ChooseUpgrade` 返回之后才分发。连续升级同一批的顺序是先 `LevelUpChosen`，再 `LevelUp → LevelUp` 的 `LevelUpShown`。不要把分发挪到 `ChooseUpgrade` 之前，也不要反转这个顺序。

终局不能暂停是 `RunState.CanTransition` 的规则，不是音频漏接。不要为终局暂停去改 GameLoop，除非产品要改状态机。

### 请别人补、本分支不改的

- 音效总监：CC-BY 的完整作者和链接（射击 spookymodem、死亡 Bottle Break、闪避 Shield、升级出现 Warp，以及其余素材的来源句）。120 BPM / 4/4 已在音乐规格里；本轮不跳播放头。若确认对齐，恢复前 `combatSource.time = round(time / 2) * 2`，超过剪辑长度则回 0，再 `UnPause`。`bgm_stem_*`、方案 B/C、Demos 不在 main，S1 不需要。台账里的“通过”是离线母带，不是游戏内录音。
- 主美：缺 `T_Trim_01_BaseColor.png` 和 `T_Trim_01_ORM.png`。M-06 现为纯色 `#242830`。模型没有自发光遮罩，眼部 `#00E5FF` 和敌人格栅 `#FF4500` 的局部自发光做不了。

### 验证

没有 Unity Editor，没有编译，没有 Play Mode，没有试听。GitHub 上的 wav 约 130 字节，是 Git LFS 指针，这里没有音频本体。
请 Codex 用 Unity 6000.4.3f1 编译，并跑 `P-02_HANDOFF.md` 里的短流程。结果记在验证目录。不要把本文件或交接文件的存在写成试听通过或编译通过。

脚本混音（`sfxTrimDb = -3 dB`，没有 `.mixer`）够 S1 原型。正式 Mixer、限幅、分轨以后再做，不因此阻塞。
