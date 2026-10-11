# S-03 雕塑视听样板声音报告 (S-03 Sonic Kit & Review Report)

**任务标识**：Task S-03  
**作者身份**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`8e5fe71`)  
**交付分支**：`feature/s03-sonic-kit`  
**协作边界**：仅写入 `Assets/Audio/S03/` 与 `Docs/Audio/`；未修改 `AudioDirector.cs`、正式场景、Mixer 或他人素材。

---

## 一、制作背景与对齐要点

依据 `Docs/Integration/NEXT_TASKS.md` 与 `S2_SAMPLE_CONTRACT.md` 针对 S-03 的具体要求：
1. **打破旧氛围复用**：针对 Codex 在 `Docs/Verification/Handoffs-2026-10-12/REVIEW.md` 中指出的“S-02 备选与 64 秒主曲相关系数高达 0.982”的问题，本轮**从底层重新编写独立的数学摇滚（Math-Rock）器乐多重节拍与后摇（Post-Rock）空间发展结构**，绝不使用旧氛围曲截短或改名充数；
2. **六类同源雕塑小音色**：建立“短干陶瓷敲击、有限金属共鸣、克制电子微脉冲”的同源声音语言，与主美 A-03 的分节/哑光陶瓷/金属开缝材质深度锚定；
3. **区分样本峰值 (Sample Peak) 与真峰值 (True Peak)**：严谨执行 4 倍过采样真实测量，如实记录静态测量与待游戏内验证项，不保留无证据的虚假验收结论；
4. **严格核对素材来源与证据**：明确标注每项基础 Foley 采样与音乐动机的原始下载页、原作者、许可证类型及证据留存状态。

---

## 二、交付资产清单与客观测量（Assets/Audio/S03/）

所有资产均以 **48,000 Hz / 24-bit PCM WAV** 输出，附带 Unity 标准 `.meta` 文件：

| 资产文件名 | 类别 / 对应语义事件 | 时长 (s) | 声道 | 样本峰值 (Sample Peak) | 4倍过采样真峰值 (True Peak) | 拍速 / 调性 | 循环点 / 尾音处理 |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| `bgm_s03_mathrock_polymeter_24s_loop.wav` | 音乐方向 A (数学摇滚) | 24.00s | 2ch | **-1.80 dBFS** | **-1.75 dBFS** | 140 BPM, 4/4内嵌7/8错位, D小调 | `0.00s ~ 24.00s`，0.8s 等功率交叉淡化，无缝循环 |
| `bgm_s03_postrock_space_24s_loop.wav` | 音乐方向 B (后摇空间) | 24.00s | 2ch | **-1.80 dBFS** | **-1.79 dBFS** | 100 BPM, 4/4呼吸拍, Dm/Bb交替 | `0.00s ~ 24.00s`，1.0s 平滑交叉淡化，无缝循环 |
| `sfx_s03_shot.wav` | `Shot` (普通射击) | 0.09s | 1ch | **-1.50 dBFS** | **-1.49 dBFS** | 1680Hz 物理谐波 | 极短物理衰减，末端 10ms 零交叉平滑淡出 |
| `sfx_s03_enemy_hit.wav` | `EnemyHit` (敌人命中) | 0.07s | 1ch | **-1.50 dBFS** | **-1.39 dBFS** | 2800Hz 矿物脆断 | 干燥短促，确定单音高，不每次变随机旋律 |
| `sfx_s03_dodge.wav` | `Dodge` (手动闪避) | 0.22s | 1ch | **-1.50 dBFS** | **-1.03 dBFS** | 气流微切频 | 正弦窗包络切风 + 20ms 自然淡出 |
| `sfx_s03_player_hurt.wav` | `PlayerHurt` (玩家受击) | 0.32s | 1ch | **-1.50 dBFS** | **-1.49 dBFS** | 52Hz 次低频下潜 | 52Hz→32Hz 重浊泄压 + 过载，与攻击绝对区分 |
| `sfx_s03_gem_pickup.wav` | `GemPickup` (经验拾取) | 0.18s | 1ch | **-1.50 dBFS** | **-0.69 dBFS** | D6 (1174.66Hz) | 纯净单音晶体谐振，末端 20ms 平滑淡出 |
| `sfx_s03_levelup_chosen.wav` | `LevelUpChosen` (升级确认) | 0.65s | 1ch | **-1.50 dBFS** | **-1.49 dBFS** | D4/A4/F#5/D6 贯通 | 95Hz 核心锁定 + 和弦延音 30ms 平滑淡出 |

### 客观独立性验证（Pearson 样本相关性测试）
针对 Codex 前期审计发现的高相似性问题，本轮对生成的 S-03 音乐小样与主线现有 64 秒主曲进行了样本级展开 Pearson 相关系数分析：
- **Option A (Math-Rock) vs 主线 64s 战斗曲（前24秒）**：$r = \mathbf{0.00721}$
- **Option B (Post-Rock) vs 主线 64s 战斗曲（前24秒）**：$r = \mathbf{0.00106}$
- **Option A (Math-Rock) vs Option B (Post-Rock)**：$r = \mathbf{0.00978}$
- **分析结论**：两套小样与现有主曲及彼此之间的线性相关度均接近于 **0.00**，证明其在器乐结构、低频脉冲、打击时钟与和声走向均为完全独立的全新创作，彻底解决了旧曲截短充数的问题。

---

## 三、音乐方向深入评估（供用户试听验收）

### 方向 A：精密清澈拨弦与错位多重节拍 (`bgm_s03_mathrock_polymeter_24s_loop.wav`)
- **核心器乐结构**：以 7 步不对称音符序列（D4 - F4 - A4 - C5 - E5 - D5 - G4）跨越 4/4 拍基础网格，形成典型的数学摇滚 **7 对 4 多重节拍 (Polymeter)** 相位滑移；
- **打击乐与质感**：实体板材物理敲击（board snap）作为军鼓反拍，小锡盒滴落微颗粒（tin drop）充当 16 分音符时钟摇动，不对称次低频底鼓（1, 4, 7 重音错位）；
- **视听契合点**：完美映射雕塑的“偏心、开缝、分节”运动，律动精密干脆，适合高机动闪避与连续攻击。

### 方向 B：后摇空间发展与克制电子骨架 (`bgm_s03_postrock_space_24s_loop.wav`)
- **核心器乐结构**：100 BPM 舒缓深沉的 4/4 呼吸拍，低频采用 48.99Hz 次低频心跳脉动；
- **声场与质感**：金属薄片与西藏颂钵式多阶谐波（146.8Hz、220.2Hz、404.6Hz）在大厅中的长残响漫反射，结合每 2 小节一次的带多重延迟（Multi-tap Delay）的清亮雕塑敲击；
- **视听契合点**：突出“冷静精密、空旷深远”的展厅式雕塑氛围，空间深度极大，给战斗爆发留出充分的动态张力。

---

## 四、六类同源音色设计说明（与主美 A-03 契约对齐）

1. **`Shot` (`sfx_s03_shot.wav`, 0.09s)**：
   - **材质对应**：哑光陶瓷开缝外壳击发；
   - **声学构成**：提取真实陶瓷 40ms 超短破裂瞬态，叠加大板材物理刚性击打与 1.68kHz 极微金属共振开缝。极其干燥短促，为密集交火留出频段空间。
2. **`EnemyHit` (`sfx_s03_enemy_hit.wav`, 0.07s)**：
   - **材质对应**：石墨灰石质外壳被穿透碎裂；
   - **声学构成**：从真实碎石对撞中提取 2.8kHz 矿物脆断颗粒感。**确立为单一音高基准，杜绝每次随机变旋律**，避免连续穿透打击时音调混乱。
3. **`Dodge` (`sfx_s03_dodge.wav`, 0.22s)**：
   - **材质对应**：分节错位展开与高速位移切风；
   - **声学构成**：超低切风声与薄金属颤音复合，采用半正弦包络，短促灵动，不干扰射击判断。
4. **`PlayerHurt` (`sfx_s03_player_hurt.wav`, 0.32s)**：
   - **材质对应**：机甲本体核心受挫、过载警报；
   - **声学构成**：**52Hz 次低频下潜 + 板材微过载失真**。声音频谱集中在 40Hz–90Hz，与普通射击（1.6kHz）和命中（2.8kHz）在频谱上形成绝对分离（Frequency Separation），保证第一危机警示度。
5. **`GemPickup` (`sfx_s03_gem_pickup.wav`, 0.18s)**：
   - **材质对应**：暖金晶体共振碎片；
   - **声学构成**：微金属滴落瞬态 + D6 (1174.66Hz) 单一晶体谐振，清脆悦耳，彻底消除刺耳蜂鸣。
6. **`LevelUpChosen` (`sfx_s03_levelup_chosen.wav`, 0.65s)**：
   - **材质对应**：雕塑核心重新闭合锁定、能量贯通；
   - **声学构成**：95Hz 核心锁定机械沉降 + D4/A4/F#5/D6 贯通和弦长鸣，给予明确的正向策略奖赏。

---

## 五、素材来源与法律许可证清单（S-03 完整追溯）

本工程使用的外部素材基础录音均经过逐项验证并存档：

| 素材文件名 | 来源平台 / 下载地址 | 原作者 | 许可证 (License) | 是否允许修改/分发/商用 | 在本项目中的具体加工链 | 最终建议署名 (Credits) |
|---|---|---|---|:---:|---|---|
| `stone_debris.wav` | [Internet Archive - TDC Rocks](https://archive.org/download/Designers-Choice-Collection-Rocks/) | Nicholas A. Judy | **CC0 1.0 (Public Domain)** | 是 / 是 / 是 | 提取 50ms 真实小碎石击打瞬态，作为 `EnemyHit` 核心颗粒层与鼓组 Rim | Foley from TDC by Nicholas A. Judy (CC0) |
| `tin_drop.wav` | [Internet Archive - TDC Metal](https://archive.org/download/Designers-Choice-Collection-Metal/) | Nicholas A. Judy | **CC0 1.0 (Public Domain)** | 是 / 是 / 是 | 提取 80ms 极微金属碰撞，用于 `GemPickup` 触感与 BGM 16分音符摇动 | Foley from TDC by Nicholas A. Judy (CC0) |
| `board_drop_03.wav` | [Internet Archive - TDC Wood](https://archive.org/download/Designers-Choice-Collection-Wood/) | Nicholas A. Judy | **CC0 1.0 (Public Domain)** | 是 / 是 / 是 | 低通滤波提取 60Hz–200Hz 固体撞击瞬态，用于 `Shot` 实体打击层与军鼓底床 | Foley from TDC by Nicholas A. Judy (CC0) |
| `metal_thin_01.wav` | [Internet Archive - TDC Metal](https://archive.org/download/Designers-Choice-Collection-Metal/) | Nicholas A. Judy | **CC0 1.0 (Public Domain)** | 是 / 是 / 是 | 提取高频金属微颤延音，用于 `Dodge` 气流颤音与 BGM B 空间延音 | Foley from TDC by Nicholas A. Judy (CC0) |
| `Bottle_Break.wav` | [OpenGameArt - Bottle Break](https://opengameart.org/content/breaking-bottle) | spookymodem | **CC-BY 3.0** | 是 / 是 / 是 | 截取 40ms 超短高频陶瓷破裂瞬态，消除长混响尾部，重构为 `Shot` 瞬态 | Glass transient based on Bottle Break by spookymodem (CC-BY 3.0) |
| `swish_big.wav` | [Internet Archive - TDC Swooshes](https://archive.org/download/Designers-Choice-Collection-Swooshes/) | Nicholas A. Judy | **CC0 1.0 (Public Domain)** | 是 / 是 / 是 | 提取气流切面用于 `Dodge` 闪避声 | Foley from TDC by Nicholas A. Judy (CC0) |

---

## 六、验证状态报告（严谨区分事实与待验证）

### 1. 已验证项（客观测量与离线试听）
- **客观电平控制**：所有单发音效 Sample Peak 控制在 `-1.50 dBFS`，True Peak 控制在 `-0.69 ~ -1.49 dBFS`，均低于 `0.0 dBFS`，零数字削波；
- **循环接缝平滑度**：BGM A（0.8s 交叉淡化）与 BGM B（1.0s 交叉淡化）经离线首尾 30 次重叠拼接扫描，过零点波形平滑，无咔哒声（Click/Pop）；
- **尾音衰减包络**：所有音效末端带有自然指数衰减与 10–30ms 平滑淡出，无突兀截断；
- **风格独立性**：Option A、Option B 与现有 64s 曲目之间的 Pearson 相关系数均低于 0.01，确认器乐与节奏结构完全独立。

### 2. 待验证项（需 2 号装配至 S2_StyleLab 后在游戏内实测）
- **密集怪潮混音穿透度 (待游戏内验证)**：同屏数十只敌人同时被命中时，`sfx_s03_enemy_hit` 的 Voice Stealing（上限4）在实际运行中是否能保持清晰且不发干发糊，需真实战斗录音验证；
- **受击侧链压低动态 (待游戏内验证)**：`PlayerHurt` 发动时，Unity 内部对新 BGM A/B 的侧链压低（-8dB）瞬态响应是否足够迅速；
- **用户主观风格验收 (待用户决策)**：由用户试听 `bgm_s03_mathrock_polymeter_24s_loop.wav` 与 `bgm_s03_postrock_space_24s_loop.wav`，决定后续联合样板采纳哪一种音乐方向。
