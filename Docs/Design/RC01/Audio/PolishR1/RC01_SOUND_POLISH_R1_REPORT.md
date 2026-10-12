# RC01 音效表现 R1 交付与设计报告 (RC01 Sound Polish R1 Report)

**任务标识**：Task RC01-Sound-Polish-R1（`feature/rc01-sound-polish-r1`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`9469ffd705a028dfffa876c7258cb85ac142ce13`)  
**交付分支**：`feature/rc01-sound-polish-r1`  
**协作边界**：仅写入 `Assets/Drafts/RC01/Audio/PolishR1/` 与 `Docs/Design/RC01/Audio/PolishR1/`；未修改共享 `AudioDirector.cs`、场景、规则、VFX 或他人 meta。  
**素材性质**：全部短音与试听混音均为 **`TEST_ONLY` 技术测试信号**，无版权商业素材，来源清晰可追溯。

---

> Codex复核：实际22段短音＋3段试听；文件测量与报告数值基本吻合。未人工监听、未硬件输出/实机总线测量，以下材料名称为合成音色的设计意图。单体峰值、RMS不能认证好听、无疲劳、无掩蔽或游戏混音不削波。旧版试听使用S03历史单音素材，且新旧时间线/RMS不同，不代表上一版Feedback01的严格等响A/B。

## 一、用户反馈与本轮音色重塑原则

用户试玩后核心反馈：“**音效比较简陋**”。
按设计评估（未人工监听认证），之前版本虽然接入了基础音效，但存在以下关键缺陷：
1. **单音色重复疲劳**：同类事件单 Clip 循环播放，缺乏打击细节与材料层次；
2. **缺乏实体工业质感**：之前的声音多为通用的电子合成哔哔声或薄脆物理声，缺乏“干燥麦田、旧农机生锈冲程、陶瓷矿物脆裂”的特定世界观质感；
3. **本轮重塑原则**：
   - **音色三层解构模型**：每一段短音必须由严格的 **瞬态（Transient）+ 主体（Body/Resonance）+ 尾部（Tail/Dissipation）** 复合构成；
   - **合成材料语义差异化变体**：同组 2~3 个变体不是单纯改变 Pitch，而是改变瞬态冲击硬度、滤波器截止频率、共振腔体谐波结构与材质包络；
   - **严格时序与因果纯洁性**：声音仅在规则层权威结算点触发；**回声预备期绝对保持声学静默**；同一次死亡仅响一次；
   - **母线与疲劳控制**：所有变体单体绝对峰值严格约束在 `-3.000 dBFS`；密集叠加是否削波还需检查具体总线。

---

## 二、8 组核心音效实际完成与参数对账表 (8 Core Sound Groups)

| 组别 | 机制语义 / 触发事件 | 变体数 | 资产文件名 | 时长 | 采样率 / 峰值 | 瞬态 + 主体 + 尾部 分层材质特征 |
|:---:|---|:---:|---|:---:|:---:|---|
| **1** | **W01 定向脉冲**<br>(`Shot`) | 3 | `TEST_ONLY_sfx_w01_pulse_v1.wav`<br>`TEST_ONLY_sfx_w01_pulse_v2.wav`<br>`TEST_ONLY_sfx_w01_pulse_v3.wav` | 65ms<br>75ms<br>85ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：1800~3200Hz 陶瓷针刺冲针；<br>**主体**：1550~1850Hz 纯石英高频电容放电；<br>**尾部**：高频细沙微电弧快速消散，短促干脆。 |
| **2** | **W02 扇形散射**<br>(`Shot`) | 3 | `TEST_ONLY_sfx_w02_spread_v1.wav`<br>`TEST_ONLY_sfx_w02_spread_v2.wav`<br>`TEST_ONLY_sfx_w02_spread_v3.wav` | 110ms<br>125ms<br>135ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：1200Hz 铸铁短管击发瞬态；<br>**主体**：75~105Hz 低频重气缸气动冲程；<br>**尾部**：散开的麦秸扬尘微粒中频摩擦，推力开阔。 |
| **3** | **W03 环绕护卫**<br>(`EnemyHit`) | 3 | `TEST_ONLY_sfx_w03_orbit_hit_v1.wav`<br>`TEST_ONLY_sfx_w03_orbit_hit_v2.wav`<br>`TEST_ONLY_sfx_w03_orbit_hit_v3.wav` | 120ms<br>135ms<br>150ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：2200~3400Hz 金属公转切削初击；<br>**主体**：440/880Hz（v1）、520/1040Hz（v2脆片）、380/760Hz（v3铜轨）谐波共振；<br>**尾部**：齿轮簧片震颤余音。 |
| **4** | **W04 共振落区**<br>(`WeaponZone`) | 3 | `TEST_ONLY_sfx_w04_zone_drop_v1.wav`<br>`TEST_ONLY_sfx_w04_zone_drop_v2.wav`<br>`TEST_ONLY_sfx_w04_zone_drop_v3.wav` | 150ms<br>170ms<br>185ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：无刺耳初击，呈声学向内收缩感；<br>**主体**：620$\to$90Hz、740$\to$110Hz、520$\to$70Hz 快速下潜扫频声波内爆；<br>**尾部**：55~75Hz 地表空洞挤压震荡。 |
| **5** | **回声实际命中**<br>(`EchoHit`) | 2 | `TEST_ONLY_sfx_echo_hit_v1.wav`<br>`TEST_ONLY_sfx_echo_hit_v2.wav` | 180ms<br>200ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：受低通滤波的 850Hz 钝击；<br>**主体**：180Hz（v1矿物空腔）、210Hz（v2铁皮空箱）二次共鸣；<br>**尾部**：原目标受震迟滞余响，**预备期绝对静默**。 |
| **6** | **传播实际命中**<br>(`PropagationHit`) | 2 | `TEST_ONLY_sfx_propagation_v1.wav`<br>`TEST_ONLY_sfx_propagation_v2.wav` | 95ms<br>110ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：极高频折射电火花破裂；<br>**主体**：2600$\to$1300Hz、2300$\to$1100Hz 快速滑音；<br>**尾部**：极短空间偏心跳跃微粒，迅速收干。 |
| **7** | **敌人死亡**<br>(`EnemyKilled`) | 3 | `TEST_ONLY_sfx_enemy_death_v1.wav`<br>`TEST_ONLY_sfx_enemy_death_v2.wav`<br>`TEST_ONLY_sfx_enemy_death_v3.wav` | 320ms<br>360ms<br>400ms | 48kHz / 24b<br>**-3.000 dBFS** | **瞬态**：2200Hz 陶石脆裂（v1）、1600Hz 锈铁撕裂（v2）、2800Hz 矿物粉碎（v3）；<br>**主体**：50~80Hz 次低频泄压冲击波；<br>**尾部**：残骸崩解散落扬尘。 |
| **8** | **成长确认 / 进化**<br>(`LevelUpChosen`<br>/ `Evolved`) | 3<br>(2+1) | `TEST_ONLY_sfx_ui_growth_confirm_v1.wav`<br>`TEST_ONLY_sfx_ui_growth_confirm_v2.wav`<br>`TEST_ONLY_sfx_evolution_flourish_v1.wav` | 480ms<br>540ms<br>1250ms | 48kHz / 24b<br>**-3.000 dBFS** | **成长确认**：880/1100Hz 机械簧片闭合 + C-E-G / A-E 正向能量琶音展开；<br>**进化解锁**：G大调大三和弦（392/494/588/784Hz）谐振展开 + 1.25s 宽阔后摇空间漫反射。 |

> **缺项说明**：已交8组、22段短音（进化仅1段），制作完整度与听感待验收。未实装武器（W05、W06）与地图事件按派工约定未虚增工作量。

---

## 三、触发密度压力测试与 3 段试听比对文件

在离线混音引擎中模拟了 4 种触发密度，检测在高并发下是否存在频段堆叠与母线失真：
- **1 次/秒**：Peak = -6.10 dBFS, RMS = -31.05 dBFS（离线采样测量，听感待验收）；
- **4 次/秒**：Peak = -6.10 dBFS, RMS = -24.63 dBFS（离线采样测量，声像待验收）；
- **10 次/秒**：Peak = -5.78 dBFS, RMS = -20.78 dBFS（仅轮换W01/W02/回声/传播，未检查低频掩蔽）；
- **20 次/秒**：Peak = -5.30 dBFS, RMS = -17.82 dBFS（此离线模拟采样峰值留有余量，不代表实机不会削波）。

### 25 秒试听比对文件清单（位于 `Assets/Drafts/RC01/Audio/PolishR1/`）
1. **`preview_old_baseline_25s.wav`**（旧版基线试听）：
   - *特征*：单 Clip 频繁触发，缺乏材料质感，长时间监听易产生听觉疲劳；
   - *实测*：48kHz 24-bit PCM Stereo，时长 25.00s，Peak = -5.73 dBFS，RMS = -22.32 dBFS。
2. **`preview_polish_r1_25s.wav`**（新版 Polish R1 试听）：
   - *特征*：8 组分层多变体交替触发，定向脉冲清脆、扇形霰弹沉稳、回声空心二次余振、成长簧片清透；
   - *实测*：48kHz 24-bit PCM Stereo，时长 25.00s，Peak = -6.12 dBFS，RMS = -25.13 dBFS。
3. **`preview_dense_combat_stress_25s.wav`**（密集战斗极限试听）：
   - *特征*：高频密集交火、回声传播连锁反应、敌人连续碎裂死亡，供后续检查离线母线和听感；
   - *实测*：48kHz 24-bit PCM Stereo，时长 25.00s，Peak = -5.79 dBFS，RMS = -24.31 dBFS。

> **真实环境声明**：以上 3 段试听均为由 `generate_previews.py` 驱动的**离线混音预览（Offline Mixdown Preview）**，用于客观比对新旧音色质感与多密度混音，**不是实机运行录屏**。

---

## 四、真实人声/拟音录制请求清单 (最多 3 条)

当前声音由正弦、滤波噪声和包络离线合成，未使用实物拟音录音。若后续追求更加极致的“乡村怪谈与锈蚀农机”生命力，请求提供以下 3 项实物拟音录音（采样率 48kHz / 24-bit WAV）：

1. **真实干燥麦穗/秸秆快速摩擦揉搓声 (Rye Straw Rustle & Snap)**：
   - *用途*：增强 W02 扇形散射扬尘尾部与怪物受击擦碰的乡村环境真实感；
   - *录制规范*：干燥麦秸用手紧握快速揉搓与折断，近距离（10cm）无混响干燥收音，时长 1~2s，无底噪。
2. **旧式拖拉机/农机气阀高压短促放气声 (Pneumatic Valve Relief)**：
   - *用途*：增强 W02 散弹气动冲程与敌人死亡次低频泄压的机械厚重度；
   - *录制规范*：农机气动阀泄压时“呲——嘭”短促脉冲，0.3~0.5s，避免过载削波。
3. **旧钟表/老式继电器金属簧片与棘轮拨动声 (Relay Switch & Ratchet Click)**：
   - *用途*：替换成长确认与重抽界面的纯合成物理瞬态；
   - *录制规范*：老式开关拨动“咔哒”声，接触干脆，尾音极短（<0.1s）。

---

## 五、给 Codex 的增量字段与接线请求 (最多 3 项)

1. **直接装配新版 `PolishR1` 变体数组**：
   - 新版配置文件位于 [`Docs/Design/RC01/Audio/PolishR1/cue-map.json`](cue-map.json)；请求在 `ConfigureAudio` 中将 W01、W02、W03、W04、Echo、Prop、Death、Growth 变体路径指向 `Assets/Drafts/RC01/Audio/PolishR1/` 对应的 `v1~v3` 资产，并启用 `sequential = true` 顺序轮换；
2. **武器事件与多变体精准绑定**：
   - 当 W02 扇形散射射击时，触发 `TEST_ONLY_sfx_w02_spread_v1~v3`；当 W04 定点共振引爆时，触发 `TEST_ONLY_sfx_w04_zone_drop_v1~v3`；
3. **暂停状态静音清理保护 (`pauseClear = true`)**：
   - 对于死亡（`EnemyDeath`）等较长尾音（300~400ms），在玩家按下 `Esc` 暂停时执行快速 5ms 淡出静音，防止暂停时仍有残骸崩解尾音残留。

---

## 六、给用户试玩的 3 项 A/B 监听建议清单

1. **【A/B 1】定向脉冲 (W01) 的高频矿物穿透感 vs 扇形散射 (W02) 的低频气缸冲程**：
   - *监听重点*：在开火时，单点脉冲的干脆针刺感是否让武器显得更精密？霰弹开火时的重气缸低音是否赋予了武器扎实的冲击力？
2. **【A/B 2】回声 (Echo) 延迟二次受震的“空心共鸣”辨识度**：
   - *监听重点*：在命中敌人后 0.5 秒，原目标身上的二次响动（180Hz/210Hz 空腔共振），能否无需看界面文字就明确辨别“这是原目标的能量回响”？
3. **【A/B 3】死亡碎裂 (Death) 的次低频泄压与残骸感**：
   - *监听重点*：怪物被击杀时，陶石/锈铁脆裂叠加低频泄压，击杀反馈是否足够干脆有力，同时在连续击杀多个目标时是否会产生低频轰鸣？
