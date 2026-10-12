# M01-R1 TEST_ONLY 回声与传播音效变体与接线建议表 (M01-R1 Audio Cues Specification)

**任务标识**：Task M01-R1（回声与传播可读性短音变体：`feature/m01-audio-cues`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`d143e7a`)  
**交付分支**：`feature/m01-audio-cues`  
**协作边界**：仅写入 `Assets/Experiments/Mechanism/Audio/` 与 `Docs/Experiments/M01-Audio/`；未修改共享 `AudioDirector.cs`、场景、Integration、DSP控制器、旧音频或BGM。

---

## 一、用户反馈与声学辨识改进背景

在用户体验 M01 独立试玩后，反馈指出：“**组合明显更有趣、会改变玩法；声波作用不明显；闪避手感可以；不能全屏。**”

经声学审查，之前 M01 复用的音效存在辨识模糊点：
1. **原回声音效**：直接复用 `Assets/Audio/SFX/sfx_enemy_hit_01.wav`（仅在脚本中简单降低音高至 0.85），声学上仍像普通物理击中，缺乏“原目标被能量共振激发的第二下”的空心共振与尾部余波；
2. **原传播音效**：直接复用 `Assets/Audio/S03/sfx_s03_shot.wav`（仅音高抬升至 1.15），与玩家自动射击音色同源，在高射速下容易被玩家误以为是子弹枪声，难以听出“从一个目标跳跃至另一目标”的位移感；
3. **本轮改进目标**：
   - 基于现有合法 TEST_ONLY 短音，生成针对性音色变体；
   - 单段时长严格限制在 $\le 0.20\text{s}$（远低于 $0.3\text{s}$ 限制），干净利落；
   - 彻底拉开“原位共振敲击”与“目标间高频电弧跳跃”的频段与包络反差。

---

## 二、新生成的 TEST_ONLY 音效资产清单

源码与参数由 `Assets/Experiments/Mechanism/Audio/generate_m01_cues.py` 完整定义与离线生成，纯原生 Python + scipy + soundfile 实现：

| 资产文件名 | 语义与设计特征 | 源音频对应 | 采样规格 | 时长 | 原始峰值 | SHA256 哈希值 |
|---|---|---|:---:|:---:|:---:|---|
| **`TEST_ONLY_sfx_echo_hit.wav`** | **原目标延迟后的第二下**：1200Hz 低通滤波滤除刺耳锐利初击 + 180Hz 衰减阻尼金属/矿物空心共振腔体，体现原目标本体受震余响。 | `sfx_enemy_hit_01.wav` (SFX/) | 48kHz / 24-bit PCM Mono | **0.200s** (9,600帧) | **-3.000 dBFS** | `ee09aab1b6229846b622a28e8088ddfa1b52cc1e27d61f4faa71436f1f6efe41` |
| **`TEST_ONLY_sfx_propagation.wav`** | **从一个目标跳到另一个**：1500Hz 高通瞬态提取 + 2400Hz$\to$1200Hz 快速线性扫频短脉冲（Chirp），体现电弧/音波折射跳跃。 | `sfx_s03_shot.wav` (S03/) | 48kHz / 24-bit PCM Mono | **0.110s** (5,280帧) | **-3.000 dBFS** | `6bf21e49c0ed9634ee173a01ba2b2e20ccbaba204a22233b6c492235a2719d19` |

> **溯源说明**：所有原音频文件（`sfx_enemy_hit_01.wav`、`sfx_s03_shot.wav` 等）完整保留，未被覆写，确保工程历史资产链完整。

---

## 三、接线与混音参数建议表 (供 Codex 接线参考)

| 事件类型 | 推荐使用的音频资源 | 建议音量 (`volumeDb`) | 最大并发 (`maxVoices`) | 最短触发间隔 (`minInterval`) | 空间声像 (`spatialBlend`) | 衰减距离 | 声学设计说明 |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| **直接命中 (`DirectHit`)** | `Assets/Audio/S03/sfx_s03_enemy_hit.wav` | `-3.0 dB` | 4 | 0.04s | 0.4 | 25m | 2800Hz 脆断瞬态（0.07s），干燥利落，作为因果序列的基准起点。 |
| **延迟回声 (`EchoHit`)** | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_echo_hit.wav` | **`-3.5 dB`** | **2** | **0.06s** | **0.5** | **25m** | 180Hz 矿物共振 + 1200Hz 腔体低通。与直击形成“脆点 $\to$ 钝面”的强烈虚实反差。 |
| **目标间传播 (`Propagation`)** | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` | **`-4.5 dB`** | **2** | **0.05s** | **0.6** | **20m** | 2400$\to$1200Hz 电弧扫频短脉冲。音量适度压低，声像紧随受跳跃目标。 |
| **回声传播 (`EchoPropagation`)** | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` *(pitch = 0.85)* | **`-5.0 dB`** | **1** | **0.06s** | **0.7** | **20m** | 次级传播音量进一步压低，音高微降，避免次级事件抢占玩家听觉焦点。 |

---

## 四、时序控制与防误听原则 (Timing & Causal Integrity)

1. **严格事件驱动，不自立计时器**：
   - 声音严格跟随规则层派生伤害成功应用事件（`PresentationEventType.EchoHit`、`PropagationHit`、`EchoPropagationHit`），**绝不另设 0.5s 或 0.1s 本地计时器去决定音效播放**；
2. **预备阶段保持声学静默**：
   - 依照派工书要求，在 0.5s 回声预备期间，**本轮先不添加额外预备声道**，杜绝“尚未命中就响”的虚假听觉反馈；
3. **目标提前死亡 / 失效静音**：
   - 若原目标在 0.5s 期间因其他直接伤害提前死亡或回池失效，规则层将判定目标无效并丢弃派生，此时**绝对不播放回声音效**；
4. **并发丢弃策略**：
   - 继续沿用现有 `dropWhenFull = true` 策略，避免在大量密集敌人时高频连发造成声压超载。

---

## 五、真实边界与环境声明 (符合 AGENTS 规范)

1. **测试属性明确**：当前生成的短音变体仅为 **`TEST_ONLY` 技术测试音效**，用于验证机制辨识度，不代表正式乡村怪谈/迷幻音波的正式艺术音效；
2. **离线客观测量局限**：
   - 测量的单轨峰值 `-3.000 dBFS` 为采样点绝对峰值（Sample Peak），未进行 True Peak 跨采样过载仪校验；
   - 声音是否能在复杂战斗与密集射击中清晰被玩家大脑辨识，**必须依赖用户在打包实机或 Unity 编辑器中进行真人实际监听**，不得伪称听感已验收；
3. **无 Unity 运行环境说明**：
   - 当前在非 Unity 编辑器环境离线执行，已生成配套 `.wav` 与生成脚本；
   - Unity `.meta` 文件建议由 Codex 在 Unity 环境中导入统一生成或由批处理工具生成。
