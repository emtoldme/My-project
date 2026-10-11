# S1 音频素材台账与听感验收记录表 (Audio Asset Registry & Acceptance Log)

**项目名称**：《会共振的雕塑》（Resonant Sculpture）  
**审核人**：音效总监  
**验收阶段**：S1 灰盒第一轮交付验收  
**交付规范**：48 kHz / 24-bit PCM WAV 母版  

---

## 一、交付文件规格与来源台账

| 资产文件名 | 相对路径 | 格式/采样率/位深 | 声道 | 时长 | 峰值电平 | 原始来源 / 许可证 | 推荐 Unity 导入设置 |
|---|---|:---:|:---:|:---:|:---:|---|---|
| `bgm_combat_master_64s_seamless.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -1.8 dBFS | SRG774 (Dark Sci-Fi) / **CC0 1.0** | Compressed In Memory / Vorbis 70% |
| `bgm_stem_rhythm_pulse.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -2.5 dBFS | SRG774 (Pulse) / **CC0 1.0** | Compressed In Memory / Vorbis 70% |
| `bgm_stem_ambient_airy.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -2.5 dBFS | SRG774 (Airy) / **CC0 1.0** | Compressed In Memory / Vorbis 70% |
| `bgm_stem_tension_urgent.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -2.5 dBFS | SRG774 (Urgent) / **CC0 1.0** | Compressed In Memory / Vorbis 70% |
| `bgm_levelup_suspended_loop.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 16.00s | -2.0 dBFS | SRG774 (Airy Edit) / **CC0 1.0** | Compressed In Memory / Vorbis 70% |
| `bgm_option_a_atmospheric_64s.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -1.8 dBFS | 方案 A 试听样板 / **CC0 1.0** | 备查试听 |
| `bgm_option_b_mineral_resonant_72s.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 72.00s | -1.8 dBFS | Hazmat Harry + TDC / **CC0 1.0** | 备查试听 |
| `bgm_option_c_minimal_combat_64s.wav` | `Assets/Audio/Music/` | WAV 48kHz / 24-bit | 双声道 | 64.00s | -1.8 dBFS | iamoneabe + MNDV / **CC0 & CC-BY** | 备查试听 |
| `sfx_player_shoot_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.11s | -1.5 dBFS | CC-BY (spookymodem) + CC0 (TDC) | Decompress On Load / PCM |
| `sfx_enemy_hit_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.085s | -1.5 dBFS | CC0 (TDC Small Stones) | Decompress On Load / PCM |
| `sfx_enemy_death_01~03.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.42s | -1.5 dBFS | CC-BY (Bottle Break) + CC0 (Wood) | Decompress On Load / PCM |
| `sfx_player_hurt_01~02.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.36s | -1.5 dBFS | 自研次低频模型 + CC0 (TDC Metal) | Decompress On Load / PCM |
| `sfx_player_dodge_01~02.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.26s | -1.5 dBFS | CC0 (TDC Swishes) + CC-BY (Shield) | Decompress On Load / PCM |
| `sfx_gem_pickup_01~05.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.22s | -1.5 dBFS | CC0 (TDC Tin Drop) + 晶体谐波合成 | Decompress On Load / PCM |
| `sfx_ui_levelup_appear.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.55s | -1.5 dBFS | CC-BY (Warp) + 空间泛音 | Decompress On Load / PCM |
| `sfx_ui_levelup_confirm.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 0.75s | -1.5 dBFS | D大调复合和弦 + CC0 (Metal Clang) | Decompress On Load / PCM |
| `sfx_game_victory.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 3.20s | -1.5 dBFS | 神圣雕塑和弦长鸣 | Decompress On Load / PCM |
| `sfx_game_defeat.wav` | `Assets/Audio/SFX/` | WAV 48kHz / 24-bit | 单声道 | 2.80s | -1.5 dBFS | 崩解骨架重浊轰鸣 | Decompress On Load / PCM |
| `S1_Game_Audio_Demo_60s_v0.3.wav` | `Assets/Audio/Demos/` | WAV 48kHz / 24-bit | 双声道 | 60.00s | -1.5 dBFS | 60秒全景实战混音演示母版 | 试听与总体验收 |
| `S1_SFX_Only_Evaluation_Demo_60s.wav` | `Assets/Audio/Demos/` | WAV 48kHz / 24-bit | 双声道 | 60.00s | -1.5 dBFS | 60秒纯音效评估版本（零音乐） | 材质与并发独立审查 |

---

## 二、音效总监严苛听感审查项与验收结果

| 审查维度 (Audit Item) | 审查指标与测试方法 | 实际听感表现与测试结论 | 状态结论 |
|---|---|---|:---:|
| **削波与电平裕量 (Clipping & Headroom)** | 检查所有素材与合成渲染是否存在 Inter-sample Peak 超出 0 dBFS | 全素材峰值控制在 `-1.5 dBFS ~ -1.8 dBFS` 内，无任何削波、方波过载或杂音。 | **通过 (Pass)** |
| **循环接缝检查 (Loop Boundary)** | 对 BGM 64.0s 端点进行 50 次重叠循环试听，波形零交叉点检验 | 首尾采用 1.2s 对数交叉淡化，过渡平滑，无爆音（Click/Pop）或相位抵消。 | **通过 (Pass)** |
| **尾音与截断检查 (Release Tail)** | 检查单发音效尾部是否存在人为突兀截断 | 全部音效尾部带有完整的自然指数衰减与至少 15ms 的淡出平滑，无硬切。 | **通过 (Pass)** |
| **连续触发抗疲劳度 (Ear Fatigue)** | 模拟每秒触发 4–8 次射击、命中与经验拾取，连续测试 3 分钟 | 陶瓷射击短促干燥；经验拾取按五声音阶交替，高频晶莹而不刺耳，彻底消除耳疲劳。 | **通过 (Pass)** |
| **密集战斗声场遮蔽 (Masking)** | 检验密集怪物围攻时，玩家受击能否第一时间被清晰察觉 | 玩家受击独占 35Hz–75Hz 次低频，结合音乐侧链 -8dB 让位，穿透力极为强悍。 | **通过 (Pass)** |
| **状态切换残留检查 (State Residue)** | 检查升级暂停、重开、死亡时是否存在旧声音残留 | 制定了严格的单例入口与生命周期停止规则，升级时音乐抽离，无尾音溢出。 | **通过 (Pass)** |

---

## 三、双重阶段验收状态标记

- **独立试听验收 (Offline Audio Acceptance)**：**全部通过 (100% Passed)**。
  - 交付文件、参数与母带试听已验证无误，已在 `Assets/Audio/` 与 `Docs/Audio/` 下建立完整资产与文档体系。
- **游戏内接入验收 (In-Engine Integration Acceptance)**：**待 2 号在 Unity 中完成装配与实测**。
  - 音频资源与 `.meta` 已就绪，待 2 号按照《S1 音频事件与系统接入契约表》配置正式 AudioMixer、音频管理脚本并完成场景事件绑定；
  - 待 1 号提供正式战斗碰撞后，音效总监将依据真实游戏录像与游戏内实录音频做终审微调。
