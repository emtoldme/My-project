# P-01 S1 素材接入与场景效果整合（2 号）

> 本文件所有内容**都没有在 Unity 中运行过**。只有规则层（表现事件通道）用 dotnet 测过。场景、材质、Prefab、音频、特效的效果都要用户在 Unity 6000.4.3f1 里生成并确认。

## 基线与依赖

- 分支：`p01/asset-integration`，起点 `origin/f02/framework-fixes@5a96c53`。
- **依赖 PR #6（F-02，尚未合并）**。请先合并 PR #6，再合并本 PR；本 PR 的目标分支是 `s1-framework`。
- 素材来源：`art/s1-industrial-mech@df2d369`（其中音频与 `audio/s1-sound-director` 的文件逐字节相同）。只按路径挑选文件，没有合并这两个分支。
- 用户决定：相机按美术规范（正交、Size 9、Pitch 55°、Yaw 45°）；事件时序以 v0.2.1 §0.3 为准；不引入未使用的音乐方案和 60 s 演示。

## 提交

| 提交 | 内容 |
|---|---|
| `b9cb12d` | 引入素材（原 `.meta` GUID 不变） |
| `62bdfff` | AGENTS.md 按 audio 分支原文更新；任务板加入 P-01 行，保留框架 V-01 行 |
| `c062f6f` | 规则层：`Rules/Presentation/PresentationEvent.cs` 表现事件通道 + 11 个测试 |
| `c40427a` | GameLoop 分发表现事件；CameraFollow 改为正交 55°/45° |
| `02b0597` | `Views/Audio/AudioDirector`、`Views/Visual/VfxDirector` |
| `c356198` | 菜单 `Waffle/Setup S1 Presentation` |
| 本文件所在提交 | 本文件、任务板 P-01 状态、README |

## 一、引入的素材

| 路径 | 内容 | 来源 / 许可（按作者文档） |
|---|---|---|
| `Assets/Art_ThirdParty/Quaternius_Mech/` | George（玩家）、Mike、Stan、Leela 的 FBX 和贴图 | Quaternius Animated Mech Pack，CC0 1.0 |
| `Assets/Art_ThirdParty/Quaternius_SciFiKit/` | 74 个 FBX（立柱、墙、箱、桶等）和贴图 | Quaternius Modular Sci-Fi MegaKit，CC0 1.0 |
| `Assets/Art_TestScene_IndustrialMech.unity`、`Assets/Art_GameView_IndustrialMech_1080p.png` | 主美的测试场景和截图（参考，不是正式场景） | 主美 |
| `Assets/Editor/ArtSceneBuilder.cs` | 菜单 `Art/Build Industrial Mech Test Scene`，只生成上面的测试场景 | 主美 |
| `Assets/Audio/Music/bgm_combat_master_64s_seamless.wav` | 战斗循环 64 s | SRG774，CC0 1.0 |
| `Assets/Audio/Music/bgm_levelup_suspended_loop.wav` | 升级悬浮循环 16 s | SRG774，CC0 1.0 |
| `Assets/Audio/SFX/` 全部 22 个 wav | 射击 3、命中 3、死亡 3、受击 2、闪避 2、拾取 5、升级出现/确认、胜利、失败 | 见下方 CC-BY 提醒 |
| `Docs/Art/`、`Docs/Audio/` | 原文，未修改（文件归主美/音效总监） | — |

所有文件都带原 `.meta`（GUID 未变），二进制在 Git LFS，本机已确认 LFS 对象完整、wav 为 48 kHz/24-bit。

**许可提醒（需用户确认）**：按 `Docs/Audio/S1_Audio_Acceptance_Log.md`，射击（spookymodem）、死亡（Bottle Break）、闪避（Shield）、升级出现（Warp）含 **CC-BY** 素材，发布时必须署名；文档没有给出完整作者和链接，请音效总监补全署名清单。

## 二、没有引入的文件

| 文件 | 原因 |
|---|---|
| `Assets/Audio/Music/bgm_option_a_atmospheric_64s.wav` | 未选用的试听版 |
| `Assets/Audio/Music/bgm_option_b_mineral_resonant_72s.wav` | 备选方案 B |
| `Assets/Audio/Music/bgm_option_c_minimal_combat_64s.wav` | 备选方案 C（含 CC-BY） |
| `Assets/Audio/Music/bgm_stem_rhythm_pulse.wav`、`bgm_stem_ambient_airy.wav`、`bgm_stem_tension_urgent.wav` | 分轨，S1 事件表不用 |
| `Assets/Audio/Demos/S1_Game_Audio_Demo_60s_v0.3.wav`、`S1_SFX_Only_Evaluation_Demo_60s.wav` | 离线演示 |
| 两个分支上的 `S1_Graybox.unity` | 只是重新保存，没有音频或美术内容，还丢了 URP 灯光数据；保留框架版本 |
| audio 分支的 `ImguiScale.cs.meta` | 框架已有，GUID 相同 |

（以上文件仍在原分支上，需要时可以再取。约 150 MB 不进入整合分支。）

## 三、事件与 tick 对应（以 v0.2.1 §0.3 为准）

音频事件表（`Docs/Audio/S1_Audio_Event_Table.md`）里的 tick 编号和 §0.3 不一致。该文件归音效总监，**这里不改它**，以下表为准：

| 表现事件 `PresentationEventType` | 音频事件 | §0.3 步骤 | 音频表写的 | 当前是否会发出 |
|---|---|---|---|---|
| `Dodge` | Event_Player_Dodge | T1 | T1 | **会**（闪避还是占位：按下即发） |
| `Shot` | Event_Player_Shoot | **T5** | T3/T4 | 预留，等 C-01 武器 |
| `EnemyHit` | Event_Enemy_Hit | **T6** | T5 | 预留，等 C-01 |
| `EnemyKilled` | Event_Enemy_Death | **T7** | T6 | 预留，等 C-01 |
| `PlayerHurt` | Event_Player_Hurt | **T8** | T7 | **会**（目前来自调试 `hurt n`；被无敌挡下时不发） |
| `GemPickup` | Event_Gem_Pickup | T10 | T10 | 预留，等 C-01 宝石 |
| `LevelUpShown` | Event_LevelUp_Appear | T12（连续升级时选择后重抽也发） | 进入 LevelUp | **会** |
| `LevelUpChosen` | Event_LevelUp_Confirm | 选择被接受时（UI 或 T0 按键） | 确认时 | **会** |
| `Victory` | Event_Game_Victory | T11 | 进入 Victory | **会**，每局最多 1 次 |
| `Defeat` | Event_Game_Defeat | T9 | 进入 Defeat | **会**，每局最多 1 次 |
| `Paused` / `Resumed` | （音频规范没写） | T0 / 按钮 / 失焦 | — | **会** |
| `Restart` | （音频规范没写） | GameLoop.Restart 重载场景前 | — | **会**，每个 RunSimulation 最多 1 次 |

通道规则：`RunSimulation.Presentation` 在每个 Tick 的 T0 清空；GameLoop 在 Tick 之后、UI 按钮/失焦/重开改变状态之后调用 `DispatchPresentation()` 分发给订阅者并清空。视图只读，不能改玩法状态。暂停期间 T1–T13 不执行，所以不会发出任何玩法事件。C-00 接口文档会规定战斗系统在 T5/T6/T7/T10 用 `RunSimulation.Emit` 写入预留事件。

## 四、现在已接上的 / 等待战斗的

| 项 | 状态 |
|---|---|
| 音乐：战斗循环、升级 1 s 交叉淡化到悬浮循环、确认后 1.4 s 恢复、胜利 2 s 淡出 + 胜利音、失败 0.3 s 截断 + 低通扫到 200 Hz + 失败音 | 已写好（未在 Unity 试听） |
| 升级出现/确认音、胜利/失败音、闪避音、受击音 + 音乐压低 -8 dB（10/200/600 ms） | 已写好（受击目前只能用 `hurt n` 触发） |
| 射击、命中、死亡、拾取音（含 01→05 音阶、1 s 重置、挤掉最早声部） | 代码已写，**等 C-01 发事件** |
| 暂停 | 所有音效和音乐一起暂停（音频规范没规定暂停，这是 2 号的决定，可改） |
| 重开 | 停止并清空所有声部，随后重载场景 |
| 混音 | 没有 `.mixer`：用 AudioSource 音量 + AudioLowPassFilter 在脚本里做。整体音效先降 3 dB（`sfxTrimDb`），因为事件表有 +0.5/+1 dB 而 AudioSource 音量上限是 1。限幅器无法用脚本实现 |
| 材质 M-01~M-06、特效材质、玩家 George 外观、4 个视觉 Prefab、相机、主光/补光、Bloom/Vignette | 菜单生成（未运行） |
| 特效：闪避残影、玩家受击闪白 | 已写好 |
| 特效：炮口闪光、命中碎片、死亡碎片 | 代码已写，**等 C-01**；敌人材质闪白要等敌人表现节点 |
| 特效冻结 | 只有 Playing 时推进；暂停/升级/终局时停在原地；重开时全部回池 |

真正的 AudioMixer（建议结构照音频事件表第三节）需要在 Unity 里手工创建：
`Master`（限幅 -0.5 dB）→ `Music`（`Combat_Music`、`LevelUp_Ambient`，接收压低）、`SFX`（`SFX_Player`、`SFX_Enemies`、`SFX_Alert`，受击发送压低）。建好后把 `Music`/`SFX` 组拖到 AudioDirector 的 `musicGroup`/`sfxGroup`。脚本里的压低和音量仍然生效；如果改用 Mixer 的 Duck Volume，就把 `duckDb` 设为 0。

与美术规范的差异（如实记录）：
- 交接表的 `T_Trim_01_BaseColor.png` 不在素材包里，M-06 先用纯色 `#242830`。
- 模型没有自发光遮罩贴图，所以眼部青色、敌人橙色格栅的局部自发光做不了；材质只打开 `_EMISSION`、默认黑色，供受击闪白使用。
- 规范里的 UI（TextMeshPro、切片血条、卡片样式）不在本次范围，界面仍是 IMGUI。
- 残影使用 `SkinnedMeshRenderer.BakeMesh`；闪避本身还是占位，残影在原地出现。

## 五、用户在 Unity 中要做的事

1. 先合并 PR #6，再切到本分支（或本 PR 合并后的 `s1-framework`）；确保安装了 Git LFS 并已拉取实际文件。
2. 用 Unity 6000.4.3f1 打开工程，等导入完成，Console 无编译错误（Editor 程序集新增了对 URP 的引用）。
3. 菜单 **Waffle → Setup S1 Presentation**（S1_Graybox 已存在；若不存在先运行 Waffle → Setup S1 Scene）。
4. 提交 Unity 生成或修改的文件：
   - 所有新脚本和新目录的 `.meta`（`Views/`、`Rules/Presentation/`、`PresentationEventTests.cs`、`S1PresentationSetup.cs` 等）；
   - `Assets/Waffle/Art/`（材质、`S1_PostFX.asset`）、`Assets/Waffle/Prefabs/Visual/`；
   - 修改后的 `S1_Graybox.unity`；
   - 音频导入设置改变后的 `Assets/Audio/**/*.wav.meta`。
5. Test Runner：EditMode 全部、PlayMode `Waffle.Tests.Integration`。
6. 截图 / 录像 / 试听（放到 `Docs/Verification/P-01/`）：
   - Game 视图截图（同机位与 `Art_GameView_IndustrialMech_1080p.png` 对照）；
   - 一段录像：移动、闪避（Space）、`hurt 10`、`givexp 30` 连续三张面板、暂停/恢复、`settime 179` 等到胜利、`hurt 100` 失败、各状态重开；
   - 试听：战斗循环接缝、升级交叉淡化、受击压低、胜利/失败。
7. Development Build（需先 Waffle → Add S1 Scene To Build Settings），运行并附 Player.log。

## 六、验证清单（全部待 Unity 验证）

| 项 | 方法 | 预期 |
|---|---|---|
| 引用 | 运行菜单后检查 Console | 没有“缺少音效/找不到模型”错误；AudioDirector 22 个音效、2 个音乐都已赋值；VfxDirector 三个材质已赋值 |
| 相机 | 选 Main Camera | Orthographic，Size 9，Rotation (55, 45, 0)；WASD 方向与屏幕一致 |
| 画面 | 对照 Docs/Art/03 五项 | 地面哑光、玩家最亮、无大面积泛光；后处理关闭时形体仍清楚 |
| 触发 | Space、`hurt 10`、`givexp 5`、选择、终局 | 每个动作只响一次对应音效；受击时音乐明显压低后回弹 |
| 并发与间隔 | 连按 Space | 200 ms 内只响一次 |
| 暂停清理 | 闪避残影出现时按 Esc | 残影、声音一起停住；恢复后继续，不会补播 |
| 升级 | `givexp 30` | 第一次面板时战斗音乐 1 s 淡出、悬浮循环淡入；最后一次选择后 1.4 s 恢复 |
| 终局 | 胜利 / 失败 | 胜利：2 s 淡出 + 胜利音；失败：0.3 s 截断、发闷 + 失败音；只响一次 |
| 重开 | 五种状态各重开一次 | 没有残留声音或特效；音乐从头开始 |
| 音乐循环 | 播放超过 64 s | 接缝无爆音 |
| 混音 | 听感 | 无削波；射击/命中（C-01 后）不盖过受击 |
| 密集特效可读性 | C-01 后 100 敌人 | 特效不遮挡玩家和地面预警超过 0.2 s |
| 性能 | Profiler | 特效与音频不产生每帧 GC；池满时放弃表现而不是新建 |
