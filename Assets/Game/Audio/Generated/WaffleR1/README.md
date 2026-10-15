# WaffleR1 原创程序化音频（《麦田里的混响》）

全部由 `Tools/AudioGen/gen_waffle_r1.py` 用 numpy/scipy 纯合成生成（Karplus-Strong 拨弦吉他、减法合成 pad/贝斯、合成鼓、卷积混响、乒乓回声）。
**无采样、无第三方音色库 → 许可：项目原创 / procedural，可自由用于本项目。**
格式：48 kHz / 16-bit / 立体声 WAV。音乐 -16 LUFS（ffmpeg 实测后自动校准），峰值 ≤ -1.3 dBFS，SFX 峰值 -1.5 dBFS（ffmpeg ebur128 实测）。
重新生成：`python Tools/AudioGen/gen_waffle_r1.py Assets/Game/Audio/Generated/WaffleR1`（需 numpy scipy；随机种子固定 1988，可复现）。
本目录只新增资源，**未改动 AudioDirector / Main / 现有音频**，接线由整合方决定。

## 音乐（导入设置：Streaming，Vorbis 0.7，Normalize 关）
| 文件 | 时长 | 建议用途 |
|---|---|---|
| bgm_golden_field_explore_loop.wav | 106.67 s（72 BPM，32 小节，无缝循环） | 金色麦田探索；A 0:00–0:40 D–G–Bm–G 指弹 → B 间奏 0:40–1:07 转 Em–C–G–A、吉他退场、口琴式旋律+高位 pad → 回升 1:07–1:20 十六分琶音渐强 → A′ 1:20–1:47 |
| bgm_old_home_unfinished_song_loop.wav | 112.0 s（60 BPM，28 小节，无缝循环） | 老房子 / 老吉他“未完成的歌”；A 0:00–0:48 → B 间奏 0:48–1:20 Bm–G–D–A 稀疏独奏吉他+八音盒新动机 → A′ 1:20–1:52 残缺旋律回归且更早断开；带磁带底噪 |
| bgm_broadcast_anomaly_combat_loop.wav | 60.0 s（128 BPM，32 小节，无缝循环） | 冷广播异常战斗（可替换 `combatLoop` 试听）；A 0:00–0:22.5 锯齿贝斯+鼓+摩斯 → B 崩落间奏 0:22.5–0:37.5 滤波鼓、长音贝斯、广播人声式共振峰啁啾 → 回升 0:37.5–0:45 军鼓滚奏+噪声上扬 → A′ 0:45–1:00 |
| sting_settlement_homecoming.wav | 6.0 s（一次性） | 胜利 / 结算 / 归乡短乐句 |

循环做法：多渲染 4 s 尾音并叠回开头，首尾样本跳变 < 1.1%。

## 音效（导入设置：Decompress On Load，Normalize 关）
| 文件 | 时长 | 建议事件 id |
|---|---|---|
| sfx_poi_discovered.wav | 2.0 s | `poi.discovered`（HC-A 请求新增） |
| sfx_poi_claimed.wav | 2.5 s | `poi.claimed`（HC-A 请求新增） |
| sfx_relic_equip.wav | 0.9 s | 遗物装备 / 旧物共鸣 |
| sfx_guitar_dodge_echo.wav | 1.5 s | 吉他闪避残响（回声+传播） |
| sfx_radio_static_burst.wav | 0.7 s | 广播异常 / 静电爆发 |
| sfx_enemy_telegraph.wav | 0.8 s | 敌人预警（冲锋者 0.8 s 预警可对齐） |
| sfx_charger_dash.wav | 0.5 s | 冲锋者冲刺 |
| sfx_dropper_impact.wav | 1.2 s | 落波者落点冲击 |
| sfx_levelup_chime.wav | 2.2 s | 升级 |
| sfx_ui_click.wav | 0.12 s | UI 点击 |
| sfx_ui_journal_open.wav | 0.7 s | 打开日志 / 叙事目录 |
| sfx_silo_wind_echo.wav | 5.0 s | 老筒仓环境：风+筒体共鸣（可循环播放或 3D 点声源） |

## 未验证
生成机器上没有 Unity：导入设置、.meta、循环在 AudioSource 中的实际无缝度、与现有混音的相对响度均未在 Unity 内验证。
