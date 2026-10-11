# P-02 交接：音频生命周期

基线：`origin/main` `b6df1fda73c6b0cf5d1e25227c2114a87dba0564`（Merge PR #8）。
分支：`feature/p02-presentation-polish`。只改 `Assets/Waffle/Scripts/Views/Audio/AudioDirector.cs` 和本文件。
没有改场景、Editor 生成器、VfxDirector、GameLoop、Rules、音效总监的规格或主美资源。

本环境没有 Unity Editor，没有编译，没有 Play Mode，没有试听。下面“已改代码”不是“游戏内试听通过”。GitHub 上的 wav 显示为约 130 字节，是 Git LFS 指针；这里没有拉取音频本体。

## 本轮代码改了什么

仍是原来的 `AudioDirector`，没有第二个管理器，也没有新的事件种类。

- 升级音乐：只有第一次 `LevelUpShown` 做 1 秒交叉淡化。连续升级的后面几张只播出现音，不把淡化再拉一遍。淡出到静音后把战斗循环 `Pause`，播放头停住；最后一次选择后再 `UnPause` 并在 1.4 秒内淡回。这不是按 120 BPM 对齐到小节。
- 暂停：声部、两条音乐和淡变一起冻住。音效间隔、拾取音阶和受击压低的保持时间在暂停期间不走。恢复后接着播，不补播暂停期间没有发出的一次性音效。升级面板里再暂停时，已经停住的战斗循环不会被提前解开。
- 胜利：两条音乐按 `victoryFadeOut`（2 秒）淡出，并播胜利音。淡完后停止，避免空转。
- 失败：按 `defeatCut`（0.3 秒）淡出，战斗源低通扫到 `defeatLowpassHz`（200 Hz），并播失败音。低通可以双向移动；新一局会直接设回 22000 Hz。
- 重开：`Restart` 只停声并清掉暂停、压低、音阶、低通和淡化，不在即将销毁的对象上再开一局。新场景的新实例在 `Start` 里从头播放战斗循环，升级循环音量为 0。
- 组件禁用：解除事件订阅并停声。子物体上的 `AudioSource` 不会因为禁用脚本而自己停止，所以这里显式 `Stop`。再次启用调用 `BeginFreshRun()`，不继承禁用前的状态。
- 终局之后不再接受升级淡回或受击压低。`Restart` 之后、重新开局之前也不再接事件。

`BeginFreshRun()` 是公开方法。正常重载不需要 Codex 调用。只有场景重载失败、旧对象还在时，才对它调用一次。

## 当前哪些事件会响

通道和剪辑槽在 P-01 已经接上。本表只说明 **main 上的 RunSimulation 现在会不会发出事件**。剪辑要由场景里的 `AudioDirector` 引用；本轮没有改场景。

| PresentationEventType | 音频事件 | 现在会不会发 | 剪辑（规格路径） |
|---|---|---|---|
| Dodge | Event_Player_Dodge | 会。按下即发，闪避仍是占位 | `Assets/Audio/SFX/sfx_player_dodge_01~02.wav` |
| PlayerHurt | Event_Player_Hurt | 会。调试 `hurt`，无敌挡住时不发 | `sfx_player_hurt_01~02.wav`，轮换，并压低音乐 |
| LevelUpShown | Event_LevelUp_Appear | 会。第一张面板才切音乐 | `sfx_ui_levelup_appear.wav` |
| LevelUpChosen | Event_LevelUp_Confirm | 会。待升级已归零才淡回战斗音乐 | `sfx_ui_levelup_confirm.wav` |
| Paused / Resumed | 规格没写单独音效 | 会。只冻结/恢复，没有独立剪辑 | 无 |
| Victory | Event_Game_Victory | 会，每局一次 | `sfx_game_victory.wav`；音乐 2 秒淡出 |
| Defeat | Event_Game_Defeat | 会，每局一次 | `sfx_game_defeat.wav`；0.3 秒截断 + 低通 |
| Restart | 规格没写 | 会，每个 RunSimulation 一次 | 无，只清场 |
| Shot | Event_Player_Shoot | 等 C-01 | `sfx_player_shoot_01~03.wav` |
| EnemyHit | Event_Enemy_Hit | 等 C-01 | `sfx_enemy_hit_01~03.wav` |
| EnemyKilled | Event_Enemy_Death | 等 C-01 | `sfx_enemy_death_01~03.wav` |
| GemPickup | Event_Gem_Pickup | 等 C-01 | `sfx_gem_pickup_01~05.wav`，1 秒无拾取回到 01 |

战斗循环：`Assets/Audio/Music/bgm_combat_master_64s_seamless.wav`。升级循环：`Assets/Audio/Music/bgm_levelup_suspended_loop.wav`。正式 Mixer、限幅和分轨不在本轮，不阻塞原型。脚本混音仍用 `sfxTrimDb = -3 dB`，因为事件表有 +0.5 / +1 dB，而 AudioSource 音量上限是 1。

## 给音效总监（S-02，不改 AudioDirector）

- 补被用到的文件的原作者、原始链接、许可证和署名。P-01 已记下射击、死亡、闪避、升级出现含 CC-BY，但规格里没有完整链接。
- 本轮没有把战斗音乐对齐到小节。若必须对齐，请给循环点、BPM 和拍号；不要另做一套事件名。现有行为是：淡出完成后暂停播放头，确认后从该位置淡入。
- `bgm_stem_*` 和方案 B/C 不在 main。S1 原型不需要它们。
- 没有游戏内录音。密集混音、暂停残留、64 秒接缝都还不能写成通过。

## 给主美（A-02，本轮未改画面）

P-01 仍缺的视觉项，不由本分支补：

- `T_Trim_01_BaseColor.png` 不在素材包里。
- 模型没有自发光遮罩，眼部和格栅的局部自发光做不了。

## 给 Codex

1. 编译本文件。确认场景上的 `AudioDirector` 仍引用原来的 `loop`、两条音乐和 10 组音效；本轮没有改字段名。
2. 短流程，不要当成听感验收：`givexp 30` 只在第一张面板交叉淡化；面板上暂停再继续，选项期间战斗音乐不提前满音量；最后一次选择后淡回；暂停时按闪避，恢复后不补播；`hurt` 压低在暂停期间不提前结束；胜利、失败各一次；五种状态重开后音乐从头且没有残留；禁用 `AudioDirector` 后子物体不再出声，再启用从头开始。
3. `LevelUpChosen` 依赖分发时 `PendingLevelUps` 已经减完。不要把 `DispatchPresentation` 挪到 `ChooseUpgrade` 之前。
4. C-01 在 T5/T6/T7/T10 用现有 `Emit` 发 Shot、EnemyHit、EnemyKilled、GemPickup。不要新建音频管理器。
5. 场景重载失败时，对还活着的 `AudioDirector` 调用 `BeginFreshRun()`。
6. 本分支没有 Unity 结果。请把编译和上述短流程的通过/失败记在验证目录，不要把本文件的存在写成试听通过。
