# P-02 交接：音频生命周期

基线：`origin/main` `b6df1fda73c6b0cf5d1e25227c2114a87dba0564`（Merge PR #8）。
分支：`feature/p02-presentation-polish`。只改 `Assets/Waffle/Scripts/Views/Audio/AudioDirector.cs`、本文件和 `Docs/Integration/P-02_REPLY.md`（给 Codex 的回复纪要）。
没有改场景、Editor 生成器、VfxDirector、GameLoop、Rules、音效总监的规格或主美资源。

本环境没有 Unity Editor / Unity 编译器，没有编译，没有 Play Mode，没有试听。下面“已改代码”不是“游戏内试听通过”，也不是“编译通过”。GitHub 上的 wav 显示为约 130 字节，是 Git LFS 指针；这里没有拉取音频本体。

## 本轮代码改了什么

仍是原来的 `AudioDirector`，没有第二个管理器，也没有新的事件种类。

- 升级音乐：只有第一次 `LevelUpShown` 做 1 秒交叉淡化。连续升级的后面几张只播出现音，不把淡化再拉一遍。淡出到静音后把战斗循环 `Pause`，播放头停住；最后一次选择后再 `UnPause` 并在 1.4 秒内淡回。这不是按 120 BPM 对齐到小节。
- 暂停：声部、两条音乐和淡变一起冻住。音效间隔、拾取音阶和受击压低的保持时间在暂停期间不走。恢复后接着播，不补播暂停期间没有发出的一次性音效。升级面板里再暂停时，已经停住的战斗循环不会被提前解开。
- 升级循环在战斗中音量是 0，但必须保持播放。战斗中暂停会把这条静音循环一并 `Pause`。恢复时只要还没进终局，就 `UnPause` 它；若此时它其实没在播，`EnterLevelUp` 会补一次 `Play`，不把播放头拨回 0。漏掉这一步时，暂停一次后再 `givexp`，交叉淡化没有声音。
- 胜利：两条音乐按 `victoryFadeOut`（2 秒）淡出，并播胜利音。淡完后 `Stop` 一次（已经 `Pause` 的声源 `isPlaying == false`，也会停），避免空转。同一轮终局只停一次。
- 失败：按 `defeatCut`（0.3 秒）淡出，战斗源低通扫到 `defeatLowpassHz`（200 Hz），并播失败音。低通可以双向移动；新一局会直接设回 22000 Hz。
- 终局之后 `LevelUpShown` 不再调用 `EnterLevelUp`。`LevelUpChosen` 在终局也不会淡回。受击压低在终局或暂停中不重新触发。
- 重开：`Restart` 只停声并清掉暂停、压低、音阶、低通和淡化，不在即将销毁的对象上再起一局。新场景的新实例在 `Start` 里从头播放战斗循环，升级循环音量为 0。`BeginFreshRun` 在 `Play` 前执行 `Stop` + `UnPause`，避免上一局的暂停标记让新一局没声音。
- 组件禁用：解除事件订阅并停声。子物体上的 `AudioSource` 不会因为禁用脚本而自己停止，所以这里显式 `Stop`。再次启用调用 `BeginFreshRun()`，不继承禁用前的状态。
- 终局之后不再接受升级淡回或受击压低。`Restart` 之后、重新开局之前也不再接事件。

`BeginFreshRun()` 是公开方法。正常重载不需要 Codex 调用。只有场景重载失败、旧对象还在时，才对它调用一次。

胜利和失败是吸收态（`RunState.CanTransition` 对终态返回 false），现在的暂停键和失焦不会在终局发出 `Paused`。恢复逻辑仍按“终局淡出没完就解开还有音量的音乐”写了，不需要为这个去改 GameLoop。

## 当前哪些事件会响

通道和剪辑槽在 P-01 已经接上。本表只说明 **main 上的 RunSimulation 现在会不会发出事件**。剪辑要由场景里的 `AudioDirector` 引用；本轮没有改场景，也没有改字段名。

| PresentationEventType | 音频事件 | 现在会不会发 | 剪辑（规格路径） | 代码里的参数 |
|---|---|---|---|---|
| Dodge | Event_Player_Dodge | 会。按下即发，闪避仍是占位 | `Assets/Audio/SFX/sfx_player_dodge_01~02.wav` | -1 dB，±0.03，并发 1，间隔 0.2 s，2D |
| PlayerHurt | Event_Player_Hurt | 会。调试 `hurt`，无敌挡住时不发 | `sfx_player_hurt_01~02.wav`，轮换 | +0.5 dB，并发 1，间隔 0.15 s，2D，并压低音乐 -8 dB / 10 / 200 / 600 ms |
| LevelUpShown | Event_LevelUp_Appear | 会。第一张面板才切音乐；从暂停回到同一张面板不重切 | `sfx_ui_levelup_appear.wav` | 0 dB，2D。战斗 1 s 淡出，悬浮层 1 s 淡到 -2 dB |
| LevelUpChosen | Event_LevelUp_Confirm | 会。待升级已归零才淡回战斗音乐 | `sfx_ui_levelup_confirm.wav` | +1 dB。最后一次选择后 1.4 s 淡回 |
| Paused / Resumed | 规格没写单独音效 | 会。只冻结/恢复，没有独立剪辑 | 无 | 终局阶段发不出来 |
| Victory | Event_Game_Victory | 会，每局一次 | `sfx_game_victory.wav` | +1 dB；音乐 2 s 淡出后 Stop |
| Defeat | Event_Game_Defeat | 会，每局一次 | `sfx_game_defeat.wav` | +1 dB；0.3 s 截断 + 低通 200 Hz |
| Restart | 规格没写 | 会，每个 RunSimulation 一次 | 无，只清场 | 不在旧对象上重开音乐 |
| Shot | Event_Player_Shoot | 等 C-01。main 的 RunSimulation 没有 Emit | `sfx_player_shoot_01~03.wav` | -2 dB，±0.05，并发 3，间隔 0.08 s，2D |
| EnemyHit | Event_Enemy_Hit | 等 C-01 | `sfx_enemy_hit_01~03.wav` | -3 dB，±0.08，并发 4，间隔 0.04 s，spatial 0.4，25 m |
| EnemyKilled | Event_Enemy_Death | 等 C-01 | `sfx_enemy_death_01~03.wav` | -1.5 dB，±0.04，并发 3，间隔 0.06 s，3D，20 m |
| GemPickup | Event_Gem_Pickup | 等 C-01 | `sfx_gem_pickup_01~05.wav` | -2.5 dB，并发 5，间隔 0.03 s，2D；1 秒无拾取回到 01 |

战斗循环：`Assets/Audio/Music/bgm_combat_master_64s_seamless.wav`（规格：120 BPM，4/4，64 秒，0 dB）。升级循环：`Assets/Audio/Music/bgm_levelup_suspended_loop.wav`（-2 dB）。正式 Mixer、限幅和分轨不在本轮，不阻塞原型。脚本混音仍用 `sfxTrimDb = -3 dB`，因为事件表有 +0.5 / +1 dB，而 AudioSource 音量上限是 1。

## 给音效总监（S-02，不改 AudioDirector）

- 补被用到的文件的原作者、原始链接、许可证和署名。台账里射击（spookymodem）、死亡（Bottle Break）、闪避（Shield）、升级出现（Warp）标了 CC-BY，但没有完整链接。命中、受击、拾取、确认、胜利、失败的来源句子也不够做发布署名。
- 音乐规格已经写了 120 BPM、4/4、64 秒 = 32 小节（一小节 2.0 秒）。本轮仍不把播放头跳到小节线：没有游戏内试听，硬跳可能爆音。若确认要对齐，恢复战斗循环前把 `combatSource.time` 设为 `round(time / 2) * 2`，结果 ≥ 剪辑长度则回到 0，再 `UnPause`。不要另做一套事件名。
- `bgm_stem_*`、方案 B/C、`Assets/Audio/Demos/` 不在 main。S1 原型不需要它们。
- 台账第二节把循环接缝、抗疲劳、密集遮蔽、状态残留写成了“通过”。那是离线母带结论，不是本分支的游戏内录音。密集战斗混音、暂停后再升级、64 秒接缝都还不能写成游戏内通过。

## 给主美（A-02，本轮未改画面）

P-01 仍缺、且不由本分支补的视觉项：

- `T_Trim_01_BaseColor.png` 和 `T_Trim_01_ORM.png` 不在素材包里。M-06 现在用纯色 `#242830`，不是交接表里的 Trim。
- 模型没有自发光遮罩。眼部 `#00E5FF`、敌人格栅 `#FF4500` 的局部自发光做不了。不要让接入去猜一张遮罩。

## 给 Codex

1. 用 Unity 6000.4.3f1 编译本文件。确认场景上的 `AudioDirector` 仍引用原来的 `loop`、两条音乐和 10 组音效；本轮没有改字段名，没有新增脚本，所以没有新的脚本 `.meta`。
2. 短流程，不要当成听感验收：
   - `givexp 30` 只在第一张面板交叉淡化；后面几张只有出现音。
   - 面板上暂停再继续：选项期间战斗音乐不提前满音量；最后一次选择后淡回。
   - **Playing 里先暂停再继续，然后 `givexp 30`**：悬浮循环必须开始淡入。这是本轮要回归的点。
   - 暂停时按闪避，恢复后不补播；`hurt` 压低在暂停期间不提前结束。
   - 胜利、失败各一次；淡完后音乐停住。
   - 五种状态重开后音乐从头，没有残留，也没有因为暂停标记而静音。
   - 禁用 `AudioDirector` 后子物体不再出声，再启用从头开始。
3. `LevelUpChosen` 依赖分发时 `PendingLevelUps` 已经减完。`GameLoop.Choose` 是在 `ChooseUpgrade` 返回后才 `DispatchPresentation`。不要把分发挪到 `ChooseUpgrade` 之前。连续升级时同一批里先是 `LevelUpChosen`，然后才是 `LevelUp → LevelUp` 的 `LevelUpShown`；现在的顺序不能反。
4. C-01 在 T5/T6/T7/T10 用现有 `Emit` 发 Shot、EnemyHit、EnemyKilled、GemPickup。不要新建音频管理器，也不要在动画或特效 Prefab 上再播这些事件。
5. 场景重载失败时，对还活着的 `AudioDirector` 调用 `BeginFreshRun()`。重载成功时不要对即将销毁的对象再调。
6. 终局不能暂停是 `RunState` 的规则，不是音频脚本漏接。不要为了终局暂停去改本文件以外的目录，除非产品要改状态机。
7. 本分支没有 Unity 结果。请把编译和上述短流程的通过/失败记在验证目录，不要把本文件的存在写成试听通过。
