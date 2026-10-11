# S1 音乐样板与过渡方案说明 (S1 Music Sample & Transition System v1.1)

> 2026-10-12：证据边界：音乐结构/调性/听感为作者评估，未独立试听验收。测得的是样点峰值而非True Peak；S02参考与原曲前32秒样点相关约0.98208，不据文件名确认新增数学摇滚风格。 统一事实与证据见[收拢审查](../Verification/Handoffs-2026-10-12/REVIEW.md)。

**维护岗位**：音效总监（Sound Director）  
**基线版本**：基于 `origin/main` 实际接入的 `bgm_combat_master_64s_seamless.wav` 与 `bgm_levelup_suspended_loop.wav`  
**对接负责人**：2 号（负责音频生命周期与过渡微调）、Codex（自动化与测试）  

---

## 一、现有 64 秒主战斗循环与升级循环方向评估

### 1. 现有战斗主音乐：`bgm_combat_master_64s_seamless.wav`
- **时长与母带规格**：64.0 秒，48 kHz / 24-bit PCM WAV (立体声)，样点峰值约 `-1.8 dBFS`。
- **音乐参数**：
  - **BPM**：120 BPM（每小节 2.0 秒，完整 32 小节结构）。
  - **调性**：D 小调 / 极简五度低频。
  - **循环点**：从 `0.00s` 循环至 `64.00s`。在 `62.80s ~ 64.00s` 区间内施加了 1.2 秒等功率对数交叉淡化，保证波形无相位抵消。
- **方向与听感评估**：
  - **沉稳精密，底床干净**：融合了后摇（Post-Rock）的空旷低频张力与现代极简电子脉冲，低频沉稳结实，高频具有冰冷空气感。
  - **抗疲劳目标**：克制、避免过度爆音；3分钟和长时间循环的实际耐受度待验证，不能预先确认。
  - **风格差距考量**：目前该曲目偏向“极简氛围电子与低频心跳脉动”，在“数学摇滚（Math-Rock）的复杂不对称重音（Polymeter）和清澈吉他/金属拨奏”维度上较为收敛。若用户或团队希望强化“雕塑错位敲击与数学摇滚律动”，可参考下文的 32 秒短备选方案。

---

### 2. 升级专属悬浮循环：`bgm_levelup_suspended_loop.wav`
- **时长与规格**：16.0 秒，48 kHz / 24-bit PCM WAV (立体声)，峰值 `-2.0 dBFS`。
- **循环点**：`0.00s ~ 16.00s`，首尾施加 1.0 秒平滑交叉淡化。
- **方向与听感评估**：
  - **彻底抽离节拍**：在玩家打开升级三选一界面时，将所有节奏鼓点与低频冲击完全清空，仅保留空灵浮动的空气微光与低频持续长音（Drone）。
  - **静默的心理张力**：创造出时间冻结的仪式感，给予玩家平复心流与思考卡牌策略的宁静空间。

---

## 二、S-02 短备选试听方案：数学摇滚/后摇共振预览 (Assets/Audio/S02/)

为了在不制作大批冗余曲目的前提下给用户和团队提供明确的听感对比，我们在 `Assets/Audio/S02/` 目录下交付了一个**32秒短备选音乐切片**：

- **文件路径**：`Assets/Audio/S02/bgm_combat_mathrock_postrock_32s_preview.wav` (32.0s 无缝循环，48kHz 24-bit)
- **风格差异**：
  - 侧重展示**数学摇滚的不对称敲击时钟**与**后摇式的空间金属谐波拨奏**；
  - 相比 64 秒主循环更加灵动跳跃，金属矿物打击感更清晰；
- **验收机制**：
  - 本素材作为独立备选，不强制覆盖主线的 `bgm_combat_master_64s_seamless.wav`；
  - 真实听感与风格取向由用户试听后决定是否在后续版本替换主音乐。

---

## 三、AudioDirector 音乐状态机与过渡建议

当前 `Assets/Waffle/Scripts/Views/Audio/AudioDirector.cs` 已实现状态驱动淡变；过渡听感待验收，以下为作者建议值，不代表所有值已在当前场景生效：

1. **升级面板展开 (`PresentationEventType.LevelUpShown`)**：
   - `levelUpCrossfade = 1.0f`：战斗音乐在 1.0 秒内通过 `Time.unscaledDeltaTime` 线性淡出至 0；悬浮循环在 1.0 秒内线性淡入至 `-2 dB`。
2. **升级卡牌确认 (`PresentationEventType.LevelUpChosen`)**：
   - `resumeFadeIn = 1.4f`：若 `Sim.State.PendingLevelUps == 0`，战斗音乐在 1.4 秒内平滑淡入回 1.0，悬浮循环在 1.4 秒内淡出，带来战斗重启的充能爆发感。
3. **玩家受击侧链压低 (`PresentationEventType.PlayerHurt`)**：
   - `duckDb = -8f`，`duckAttack = 0.01f`，`duckHold = 0.2f`，`duckRelease = 0.6f`。受击瞬间音乐避让 8dB，确保危险警报绝对突出。
4. **终局胜败结算 (`Victory` / `Defeat`)**：
   - `victoryFadeOut = 2.0f`：胜利时战斗音乐 2.0 秒平滑淡出，长鸣和弦定格；
   - `defeatCut = 0.3f`，`defeatLowpassHz = 200f`：失败时战斗音乐 0.3 秒急促淡出并伴随低通下潜，模拟系统瞬间关机崩溃。
