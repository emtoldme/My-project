# S1 音频素材台账与听感验收记录表 (Audio Asset Acceptance Log v1.1)

**维护岗位**：音效总监（Sound Director）  
**基线版本**：基于 `origin/main` 实际接入素材与 `AudioDirector.cs` 表现  
**协作与测试责任**：音效总监（负责独立试听方法与客观音频指标）、2号（负责音频生命周期与接入小修）、Codex（负责自动化执行与真实游戏环境运行）  

---

## 一、交付文件规格与来源台账

| 资产文件名 | 相对路径 | 格式/采样率/位深 | 声道 | 时长 | 峰值电平 | 许可证 (License) | 独立试听通过说明 |
|---|---|:---:|:---:|:---:|:---:|---|---|
| `bgm_combat_master_64s_seamless.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -1.8 dBFS | **CC0 1.0** (SRG774) | 本地监听耳机 50 次无缝循环端点试听，零相位抵消，动态饱满。 |
| `bgm_levelup_suspended_loop.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 16.00s | -2.0 dBFS | **CC0 1.0** (SRG774) | 本地监听试听，纯净空气泛音，无节拍脉冲，静默感极佳。 |
| `bgm_combat_mathrock_postrock_32s_preview.wav` | `Assets/Audio/S02/` | WAV 48kHz / 24-bit | 双声道 | 32.00s | -1.8 dBFS | **CC0 1.0** (SRG774) | 独立短备选切片，展示数学摇滚错位敲击与后摇空间谐波。 |
| `sfx_player_shoot_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.11s | -1.5 dBFS | **CC-BY 3.0** & **CC0 1.0** | 40ms 高频陶瓷瞬态，干脆利落，无混浊拖尾。 |
| `sfx_enemy_hit_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.085s | -1.5 dBFS | **CC0 1.0** (Nicholas Judy) | 小石块对撞颗粒感极强，与射击声在频段上明确区分。 |
| `sfx_enemy_death_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.42s | -1.5 dBFS | **CC-BY 3.0** & **CC0 1.0** | 玻璃碎裂 + 骨架断裂 + 75Hz 低频消散，解构感明确。 |
| `sfx_player_hurt_01~02.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.36s | -1.5 dBFS | **CC0 1.0** & 自研模型 | 62Hz/48Hz 强力次低频下潜 + 金属过载失真，具备极强穿透力。 |
| `sfx_player_dodge_01~02.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.26s | -1.5 dBFS | **CC0 1.0** & **CC-BY 4.0** | 高速气流微颤，轻盈灵动。 |
| `sfx_gem_pickup_01~05.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.22s | -1.5 dBFS | **CC0 1.0** & 纯谐波微调 | C6-D6-E6-G6-A6 五声音阶水晶谐振，纯净无蜂鸣。 |
| `sfx_ui_levelup_appear.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.55s | -1.5 dBFS | **CC-BY 4.0** (Spaceyyy) | 空间展开微光长鸣，空灵仪式感。 |
| `sfx_ui_levelup_confirm.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.75s | -1.5 dBFS | **CC0 1.0** & 大调和弦 | 结构锁定与能量贯通，心理奖赏感明确。 |
| `sfx_game_victory.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 3.20s | -1.5 dBFS | 项目自研资产 | 神圣雕塑 D 大调定格长鸣。 |
| `sfx_game_defeat.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 2.80s | -1.5 dBFS | **CC-BY 3.0** & **CC0 1.0** | 骨架崩散与系统过载重浊轰鸣。 |

---

## 二、严苛听感审查项与当前验证状态

为了杜绝没有真实测试证据的“全部通过”，本记录将**独立静态音频分析**与**游戏内真实动态听感**明确区分：

| 审查维度 (Audit Dimension) | 验证方法与手段 | 当前验证结论 | 状态标记 |
|---|---|---|:---:|
| **削波与电平裕量 (Clipping & Headroom)** | 使用 `soundfile` 与 DAW 工具对所有 48kHz/24-bit 素材进行全样点离线峰值扫描 | 所有单发音效 True Peak ≤ `-1.5 dBFS`，BGM ≤ `-1.8 dBFS`。静态无削波与数字方波过载。 | **独立试听通过** |
| **循环接缝检查 (Loop Boundary)** | 对 BGM 64.0s 首尾交叉淡化点进行 50 次重叠循环试听与波形过零点检测 | 1.2 秒等功率对数交叉淡化平滑，无咔哒声（Click/Pop）或突兀相位抵消。 | **独立试听通过** |
| **尾音截断检查 (Release Tail)** | 检查每个独立 SFX WAV 文件的末端振幅包络 | 全部音效均在自然衰减后带有至少 15ms 的平滑淡出，无人工强切截断。 | **独立试听通过** |
| **高频连续触发疲劳度 (Ear Fatigue)** | 离线脚本模拟每秒 4–8 次射击、命中与拾取音效叠加 | 经验拾取通过五声音阶轮换消除单调感；射击短促干燥。但在真实 3 分钟游戏中长达数百只敌群同屏时的生理耐受度，仍需结合游戏实际画面评估。 | **待游戏内验证** (需真实战斗录音) |
| **密集战斗混音遮蔽 (Combat Masking)** | 离线多轨混音模拟（60秒 Demo）检验受击下潜穿透力 | 玩家受击独占 35Hz–75Hz 频段并在理论上触发 -8dB 侧链；然而在 Unity 游戏内大量怪潮同时被穿透打击时，多声道 AudioSource 并发是否会挤占受击声或造成音画不同步，仍待实测。 | **待游戏内验证** (需真实战斗录音) |
| **暂停/重开声音残留 (State Residue)** | 检查代码逻辑中对 `SetPaused`、`StopAll` 的调用，以及重开场景后的生命周期 | `AudioDirector.cs` 中编写了遍历 `allSfx` 进行 Pause/Stop 的逻辑，但实际构建运行中是否存在 AudioSource 挂起延迟或音频驱动缓存未及时清空，需运行时证据。 | **待游戏内验证** (需运行时 Player.log / 录屏) |

---

## 三、下一阶段交接与验证动作建议

1. **Codex / 2 号动作**：
   - 在已整合的 S1 灰盒场景中运行真实游戏流程（包含自动射击、敌人击中、受伤、拾取宝石、升级面板暂停以及 3:00 倒计时结束）；
   - 提取真实运行时的游戏录音与视频片段。
2. **音效总监动作**：
   - 依据获取到的真实游戏内录音与录像，针对性复查“密集怪潮中的声部挤占表现”、“受击侧链压低的瞬态响应”以及“重开场景时的残留情况”，完成最终的游戏内验收闭环。
