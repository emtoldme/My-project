# RF01 声画映射与事件响应总表 (RF01 Event Sound Map Specification)

**任务标识**：Task RF01（新武器与声波事件声画映射表：`review/rf01-event-sound-map`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`13be83d`)  
**交付分支**：`review/rf01-event-sound-map`  
**协作边界**：仅写入 `Docs/Experiments/M01-Audio/RF01_EVENT_SOUND_MAP.md`；未修改共享 `AudioDirector.cs`、场景、VFX、Integration、或覆写旧音频文件。  
**版本状态**：TASKS_04 已交付分支 `feature/m01-audio-cues` 中的短音变体（`TEST_ONLY_sfx_echo_hit.wav` 与 `TEST_ONLY_sfx_propagation.wav`）已提交并在此冻结，由 Codex 统一执行资源导入与接线。

---

## 一、用户反馈与声画映射痛点对账

针对 M01 首轮用户实测反馈“**声波作用不明显**”以及最新引入的资产：
- 用户自制武器：**旧式声波调频器（Old Sonic Modulator）**；
- 用户自制视效组件：**`SonicAttackVFX.cs`（含 `PlayFire`、`PlayHit`、`PlayDeath`）**；
- 独立机制试玩事件：**`M01CombatBridge`（直接命中、回声伤害、传播伤害、回声传播）**；

声学与视效之间必须建立**严密的一对一映射与层级防冲突机制**，防止在画面粒子与音频之间产生“各放各的、重复叠播、虚假抢跑”等听觉混乱。

---

## 二、RF01 事件声画映射总表 (Event Sound Map)

本表统一协调视觉发射/击中/击杀与规则层派生机制事件，明确各事件的**唯一声学出口**与防叠播策略：

| 事件名称 | 规则 / 触发来源 | 对应视觉表现 (RF01 / M01) | 推荐复用短音资源 (TEST_ONLY) | 建议增益 (`volumeDb`) | 并发上限 (`maxVoices`) | 最短间隔 (`minInterval`) | 空间声像 (`spatialBlend`) | 听觉设计意图与主次关系 |
|---|---|---|---|:---:|:---:|:---:|:---:|---|
| **武器调频发射 (`WeaponFire`)** | 玩家自动开火逻辑触发 | 调频器电子管高亮脉冲 + `SonicAttackVFX.PlayFire`（声波扩散环） | `Assets/Audio/S03/sfx_s03_shot.wav` | `-3.0 dB` | 3 | 0.08s | 0.0 (2D) | **前景基准音**：1680Hz 短暂高频机械电容放电瞬态（0.09s），作为武器节奏发射基底。 |
| **直接命中 (`DirectHit`)** | 规则层确认子弹首击目标 ($t=0.0\text{s}$) | `SonicAttackVFX.PlayHit`（麦秸扬尘粒 + 初始冲击环） | `Assets/Audio/S03/sfx_s03_enemy_hit.wav` | `-3.0 dB` | 4 | 0.04s | 0.4 | **打击基准点**：2800Hz 单音高矿物脆断瞬态（0.07s），快速利落，代表物理首击。 |
| **回声预备 (`EchoPending`)** | 规则层排队等待回声 ($t=0.0\text{s} \sim 0.5\text{s}$) | 原目标脚底/身体呈现紫红收束标记 (`M01EchoCue`) | *(严格保持静默)* | - | - | - | - | **声学静音**：杜绝“尚未命中就提前响”的虚假听觉反馈。等待视觉倒计时到期。 |
| **延迟回声生效 (`EchoHit`)** | 0.5s 倒计时结束，规则层对原目标结算二次伤害 | 原目标脉冲收束 + 弹出“回声 -5”飘字 | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_echo_hit.wav` | `-3.5 dB` | 2 | 0.06s | 0.5 | **中景共振音**：180Hz 矿物共振 + 1200Hz 腔体低通（0.20s），呈现原目标本体空心余震。 |
| **目标间传播 (`Propagation`)** | 0.1s 倒计时结束，规则层对5米内邻目标结算跳跃伤害 | 两目标间生成金黄折射连线 + 次级落点小环 | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` | `-4.5 dB` | 2 | 0.05s | 0.6 | **中景位移音**：2400Hz$\to$1200Hz 快速电弧扫频短脉冲（0.11s），体现音波跳跃折射感。 |
| **回声二次传播 (`EchoPropagation`)** | 0.6s 总延时结束，回声对次级目标再次跳跃结算 | 次生金黄细线连向第三目标 | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` *(微降音高 0.85)* | `-5.0 dB` | 1 | 0.06s | 0.7 | **背景辅助音**：音量进一步降至 -5.0dB，音高压低，确保次级衍生事件不抢占前景。 |
| **目标被消灭 (`EnemyDeath`)** | 无论直击、回声或传播导致目标生命归零 | `SonicAttackVFX.PlayDeath`（扩散双环 + 麦草崩解粒子） | `Assets/Audio/SFX/sfx_enemy_death_01.wav` ~ `03.wav` (随机变体) | `-2.0 dB` | 3 | 0.06s | 0.9 | **前景终止音**：次低频泄压 + 玻璃矿物碎裂（0.42s），确立目标生命被清除的确定反馈。 |
| **回声目标提前失效 (`EchoInvalid`)** | 原目标在 0.5s 内提前死亡或代号回池 | 预备标记立即撤销，不播放命中环 | *(严格保持静默)* | - | - | - | - | **静默撤销**：规则层判定失效后绝不播放回声音效，避免空放假声音。 |

---

## 三、避免重复播放与时序防冲突原则 (Conflict Prevention)

在当前 RF01 体系中，存在两套表现系统：传统的 `CombatViews` / `AudioDirector` 与新引入的 `SonicAttackVFX` / `M01DerivedFx`。为确保声音纯粹利落，必须执行以下防冲突契约：

1. **直接命中与派生命中绝不重复触发**：
   - 规则层中直接命中是 `PresentationEventType.EnemyHit`，回声与传播分别是 `EchoHit`、`PropagationHit`、`EchoPropagationHit`；
   - `SonicAttackVFX.PlayHit` 只能绑定在**直接命中**时由表现层调用；派生命中（回声/传播）由 `M01DerivedFx` 专用动效承接；
   - **音频通道由 `AudioDirector` 统一调度**，禁止在 `SonicAttackVFX` 内部自行 `AudioSource.PlayClipAtPoint`，彻底杜绝一击双响。
2. **死亡事件防止回池与重开重复触发**：
   - 敌人被击杀时，仅在生命值首次降至 0 的 Tick 触发一次 `EnemyDeath`；
   - 敌人 GameObject 回池（ReturnToPool）、场景重开（Restart）或游戏暂停时不触发死亡音效；
   - `PlayDeath` 仅由死亡事件驱动，不参与普通击中响应。
3. **因果与时钟纯洁性**：
   - 音效播放完全被动跟随规则层结算事件；**严禁音效脚本或 VFX 协程自行设立 0.5s / 0.1s 计时器来决定伤害或判定**；
   - 当按下 `Esc` 暂停时，`AudioDirector` 必须冻结所有正在播放的声源，恢复后继续，与规则层时钟严格步调一致。

---

## 四、音乐与风格方向澄清 (BGM & Stylistic Boundaries)

1. **数学摇滚 / 后摇 / 迷幻电子为长期美学方向**：
   - “乡村怪谈 / 锈蚀机械 / 迷幻音波”以及数学摇滚的复节拍特征，是游戏长期的风格指南；
   - **不能把新接收的用户美术素材（如调频器、麦田地表）混淆为“已有正式配乐”**；
2. **本轮冻结音频素材，不新造 BGM**：
   - 当前测试仍以 TEST_ONLY 离线音效和 V2 技术双轨为基础，不新加 BGM、不引入第三方音频中间件插件；
   - 正式配乐仍等待用户后续提供或指定。

---

## 五、需要用户实际试听的 3 项 A/B 监听问题 (供后续试玩验收)

离线波形计算与采样点测量不能代替人耳在游戏混响环境下的主观感受。在 Codex 完成 M01-R1 与 RF01 整合打包后，建议用户在实机试玩中重点监听以下 3 项：

1. **【A/B 1】回声原位共振感 vs 直接命中脆断感**：
   - *监听重点*：在开启 `Echo` 模式时，直接命中 0.5 秒后，原目标身上的二次响动（180Hz 空心共振）能否让您无需看飘字就能用耳朵听出“这是原目标本体产生的能量回响”，还是仍然感觉像普通武器补枪？
2. **【A/B 2】传播电弧扫频跳跃 vs 玩家自动射击**：
   - *监听重点*：在开启 `Propagation` 模式时，折射到次级目标的 0.1s 快速扫频音（2400$\to$1200Hz Chirp），能否与玩家手持调频器发射的自动子弹声（1680Hz 射击音）明显拉开距离，听出空间跳跃感？
3. **【A/B 3】密集怪群爆发下的声压与听觉疲劳度**：
   - *监听重点*：在切换至 `Combined`（组合模式）时，多次回声与传播同时触发，当前的并发上限（2 声部）与限频节流（50~60ms）是否足够清爽，还是会出现低频轰鸣或高频刺耳？
