# S1 音乐样板与过渡方案说明 (S1 Music Sample & Transition System)

**交付路径**：`Assets/Audio/Music/`  
**格式规范**：48 kHz / 24-bit PCM WAV / 立体声 (Stereo) / 峰值控制在 -1.8 dBFS 内  
**制作工艺**：纯海外专业 CC0 现成成熟音乐片段重采样、乐句剪辑、分轨混音与无缝循环工程（**零代码合成玩具声**）。

---

## 一、主推荐方向与备选方案对比

我们制作并交付了三套风格迥异且均可试听的完整循环样板：

### 1. 【主推荐】方案 A：精致冷峻的氛围电子 (Atmospheric Electronic)
- **试听与母带文件**：
  - 母带循环：`Assets/Audio/Music/bgm_combat_master_64s_seamless.wav` (64.0s 无缝循环)
  - 试听版：`Assets/Audio/Music/bgm_option_a_atmospheric_64s.wav`
- **音乐参数**：
  - **BPM / 拍号**：120 BPM，4/4 拍，完整 32 小节结构循环。
  - **调性**：D 小调 / 极简五度低频。
  - **素材溯源**：SRG774 的 `pulse.mp3`（次低频脉冲律动）+ `airy.mp3`（悬浮空气感），**CC0 1.0 (Public Domain)**。
- **听感特征**：具有极其干净、沉稳的呼吸级心跳律动，高频如冰晶般透亮，低频结实有弹性。与大理石、金属、晶体的视觉完美契合，长时间播放（3分钟甚至数小时）绝无烦躁或疲劳感。
- **分层音轨 (Stems) 交付**：
  - `bgm_stem_rhythm_pulse.wav`：次低频坚实脉冲层（战斗推进底床）；
  - `bgm_stem_ambient_airy.wav`：空灵浮动氛围层（空间深度与冷峻底色）；
  - `bgm_stem_tension_urgent.wav`：高频危机张力层（加压突围使用）。

---

### 2. 【备选 1】方案 B：神秘空旷、强调矿物与金属共振的实验电子 (Mineral & Resonant)
- **文件**：`Assets/Audio/Music/bgm_option_b_mineral_resonant_72s.wav` (72.0s 循环)
- **音乐参数**：BPM 自由呼吸，72 秒大跨度结构。
- **素材溯源**：Hazmat Harry 的金属管道残响 + SRG774 的 Transmission 电影长音 + TDC 真实金属薄片微颤。**CC0 1.0**。
- **听感特征**：空间声场极其宏大，充满古老遗迹与冷兵器碰撞共鸣感，艺术格调极高。
- **评价**：适合剧情模式、展示场景或解密探索；在三分钟高机动快节奏战斗中，击打的驱动感略微克制。

---

### 3. 【备选 2】方案 C：节奏强烈、强调战斗推进的极简电子 (Minimal Combat)
- **文件**：`Assets/Audio/Music/bgm_option_c_minimal_combat_64s.wav` (64.0s 循环)
- **音乐参数**：125 BPM，4/4 拍，64 秒双循环单元。
- **素材溯源**：iamoneabe 的 `dark_things_loop.wav` + MNDV 的 `battle_loop_iom2b.wav`。
- **听感特征**：低音合成器线条硬核抓耳，机械推进感强烈，战斗压迫感十足。
- **评价**：适合遭遇 Boss 或怪潮蜂拥时刻，但在日常循环中听觉刺激偏浓。

---

## 二、状态机无缝过渡与播放建议 (给2号开发者)

针对 Unity `Waffle.RunSimulation` 状态流，设计以下自动化混音与过渡规则：

| 游戏运行阶段 (RunPhase) | 播放音轨与行为 | AudioMixer 参数与音量过渡建议 |
|---|---|---|
| **Playing (正常战斗)** | 循环播放 `bgm_combat_master_64s_seamless.wav` (或开启 Rhythm + Ambient Stems) | 音乐音量：`0 dB`，保持饱满清晰。 |
| **Player Hurt (玩家受击)** | 音乐保持播放，但触发瞬间侧链闪避 | **Music Ducking**：音乐总线音量瞬间降至 `-8dB`（Attack 10ms，Hold 200ms，Release 600ms 平滑回弹）。 |
| **LevelUp (升级暂停三选一)** | 战斗 BGM 淡出，平滑切至 `bgm_levelup_suspended_loop.wav` | **Crossfade**：战斗音乐在 1.0 秒内渐隐至 `-80dB`；悬浮音乐在 1.0 秒内从 `-80dB` 渐显至 `-2dB`。抽离所有鼓点，保留纯粹空气微光与低频悬浮。 |
| **Confirm / Resume (升级确认恢复)** | 悬浮音乐停止，战斗 BGM 恢复播放 | 升级确认音播放后，战斗音乐在 1.4 秒内平滑淡入（从最近小节对齐恢复），带给玩家充能后的战斗爆发快感。 |
| **Victory (终局 3:00 获胜)** | 战斗 BGM 在 2.0 秒内自然渐隐，播放 `sfx_game_victory.wav` | 战火平息，留下空旷纯净的 3.2 秒 D 大调神圣共振长鸣。 |
| **Defeat (玩家死亡结案)** | 战斗 BGM 瞬间在 0.3 秒内截断并伴随低通下潜（Lowpass Filter Sweep 至 200Hz），播放 `sfx_game_defeat.wav` | 呈现结构断裂、系统过载关机的沉重消亡感。 |
