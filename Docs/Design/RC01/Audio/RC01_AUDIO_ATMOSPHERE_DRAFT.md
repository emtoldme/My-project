# RC01 四图音频风格、声音语义与测试映射草稿 (RC01 Audio & Music Atmosphere Draft)

**任务标识**：Task RC01-Audio（四图音效/音乐气质草稿与测试声音映射：`feature/rc01-audio-draft`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`b8d3931`)  
**交付分支**：`feature/rc01-audio-draft`  
**协作边界**：仅写入 `Assets/Drafts/RC01/Audio/` 与 `Docs/Design/RC01/Audio/`；未修改共享 `AudioDirector.cs`、场景、VFX 或旧音频素材。  
**素材性质**：全部短音与双轨循环均为 **`TEST_ONLY` 技术测试信号**，不宣称正式音乐制作或人声配音。

---

## 一、四张地图的声音气质与层次草稿 (Four Map Sonic Landscapes)

本草稿严格围绕“**乡村怪谈 / 旧机械 / 迷幻音波**”核心审美，以**数学摇滚（Math Rock）的复节拍切分**进行节奏组织、以**后摇（Post-Rock）的动态呼吸**推进战场情绪、以**模拟电子（Analog Synth）的失真共振**刻画锈蚀机械质感：

```text
四图声音世界观演进谱系 (Sonic Evolution Continuum):
[秋收之路: 麦秸干燥与风鸣] 
    --> [旧农贸市场: 金属碰擦与中频切分] 
        --> [第四粮仓: 封闭箱体低频余震] 
            --> [无线电荒原: 宽广空间与高频电弧]
```

### 1. 地图一：秋收之路 (Harvest Road) —— 干燥风声、麦秆摩擦与稀疏切分
- **环境底床 (Ambience)**：
  - 微风穿过成熟麦穗的沙沙声（复用白噪滤波粉红底噪 + 1200Hz 动态轻扫）；
  - 远处电线杆木质轻微应力吱呀声与偶发的乌鸦低鸣残响。
- **战斗配乐节奏 (Music Texture)**：
  - 7/8 拍与 4/4 拍交错的清音电吉他点弦琶音（Karplus-Strong 拨弦算法原型）；
  - 鼓组轻质干燥，底鼓强调 65Hz 短促气动，军鼓带金属丝沙音，与开阔麦田的声学反射保持一致；
  - 动态：0~4 分钟单吉他单鼓点轻推进，4~8 分钟混入低音合成贝斯（49Hz），8~10 分钟收割机轰鸣与吉他过载破音齐发。

### 2. 地图二：旧农贸市场 (Old Market) —— 铁皮棚回响、中频机械切分与狭窄声场
- **环境底床 (Ambience)**：
  - 锈蚀铁皮摊位受风碰撞的短促空心当啷声；
  - 水泥与碎瓦地表上的狭窄回音（早期反射声强，混响时间短 $RT_{60} \approx 0.6\text{s}$）。
- **战斗配乐节奏 (Music Texture)**：
  - 11/8 拍复合节拍，密集的击弦（Slap）与机械棘轮摇把敲击声；
  - 极度强调中频（800Hz ~ 2500Hz）的金属感，声部频繁在左右声道穿插跳跃，强化街巷两端侧翼被围堵的压迫感；
  - 动态：遇摊位封路事件时，引入 120BPM 工业脉冲切片，突围后释放至宽广后摇长音。

### 3. 地图三：第四粮仓 (Fourth Granary) —— 巨型箱体共振、克制低频与工业压迫
- **环境底床 (Ambience)**：
  - 巨大混凝土/金属筒仓内部特有的超长低频混响（$RT_{60} > 3.0\text{s}$，150Hz 以下隆隆余响）；
  - 管道高压间歇性排气泄压阀的喷气声（白色高斯噪声脉冲）。
- **战斗配乐节奏 (Music Texture)**：
  - 5/4 拍稳重步进，BPM 下沉至 96；
  - 重型模拟合成器纯正低音（38Hz ~ 55Hz Sub-bass），极具实体冲击力，但音量受到硬限幅约束（-18dB 总线保护）；
  - 动态：排气危险预警区激活时，主旋律声部施加高通滤波（HPF 400Hz），给排气喷发留出极干净的低频冲击带。

### 4. 地图四：无线电荒原 (Radio Wasteland) —— 宽域电磁干扰、偶发高频电弧与虚无后摇
- **环境底床 (Ambience)**：
  - 宽域立体声广播短波杂音（静态粉红噪声调制 + 莫尔斯电码碎片残响）；
  - 高压电塔绝缘子微弱电晕放电声（50Hz 工频谐波群）。
- **战斗配乐节奏 (Music Texture)**：
  - 自由拍无固定网格后摇漫反射（Post-rock Shimmer & Delay Swells）；
  - 空间感极强（Stereo Spread 100%），高频明亮（3000Hz 以上空气感）；
  - 动态：进入信号干扰区时，整体音乐声部施加 8-bit 降采样抖动，脱离干扰区后瞬间展开为恢弘的后摇长音墙。

---

## 二、局内多阶段动态音乐推进方案 (Music Intensity Progression)

复用现有 `Assets/Audio/Music/` 与 `Assets/Audio/S03/` 经过离线量化的循环测试曲目：

| 局内时段 | 阶段定位 | 推荐挂接测试曲目 | 配器与声学密度方案 | 动态声学处理 |
|---|---|---|---|---|
| **0 ~ 2 分钟** | 开局识别与探索 | `bgm_s03_mathrock_polymeter_24s_loop.wav` | 仅保留清音电吉他琶音与轻质机械踩镲，空间宽阔。 | 低通滤波 3500Hz，音量 -4.0dB，为玩家熟悉初级武器射击声留出前景空间。 |
| **2 ~ 4 分钟** | 首波压力与精英出现 | 同上 (解除滤波) + 叠加底层低频 | 混入 49Hz 恒定低音底床，小军鼓全额推入，节奏锁定。 | 全频段放开，音量归一至 0.0dB 基准。 |
| **4 ~ 6 分钟** | 构筑成型与群怪涌出 | `bgm_combat_master_64s_seamless.wav` | 切换至 64 秒大循环，贝斯线切分加剧，环境扫频启动。 | 引入受击侧链压低（Ducking -8dB，10ms Attack / 600ms Release）。 |
| **6 ~ 8 分钟** | 首个武器进化与精英战 | `bgm_s03_postrock_space_24s_loop.wav` | 爆发性后摇空间回响，大和弦展开，伴随高频微光层。 | 进化宝箱开启时音乐瞬时下潜 -6dB 并施加 1 秒短延音。 |
| **8 ~ 10 分钟** | 终局决战与极限制裁 | `bgm_combat_master_64s_seamless.wav` *(复合叠加)* | 满配器输出，电感失真贝斯 + 密集金属切片，声部全开。 | 整体 SFX 总线微调 -1.5dB，防止多武器密集命中时总线削波。 |
| **升级三选一** | 升级暂停界面 | `bgm_levelup_suspended_loop.wav` | 悬浮环境低频嗡鸣，主战斗音乐平滑 1.0s 淡出，战斗恢复后 1.4s 淡入。 | 界面音效专属独立通道播放。 |

---

## 三、RC01 核心事件到测试音效映射总表 (Cue Map Summary)

完整的 JSON 格式文件已持久化于 [`Docs/Design/RC01/Audio/cue-map.json`](My project/Docs/Design/RC01/Audio/cue-map.json)。以下为核心精简对应：

### 1. 6 攻击家族与机制反馈 (Weapons & Mechanisms)
| 标识 ID | 逻辑事件名 | 推荐挂接资源路径 (TEST_ONLY) | 音量 (`dB`) | 变体数 | 最短间隔 | 并发 | 声学特征与防冲突设计 |
|:---:|---|---|:---:|:---:|:---:|:---:|---|
| **W01** | `WeaponFire_W01` (定向脉冲) | `Assets/Audio/S03/sfx_s03_shot.wav` | `-3.0` | 1 | 0.08s | 3 | 1680Hz 单体直射电容瞬态，干燥利落。 |
| **W02** | `WeaponFire_W02` (扇形散射) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_shot_spread.wav` | `-3.5` | 1 | 0.12s | 2 | **新变体**：95Hz 气动散弹冲程，多弹丸只响应一次射击音。 |
| **W03** | `WeaponHit_W03` (环绕护卫) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_guardian_orbit.wav` | `-4.0` | 1 | 0.10s | 2 | **新变体**：440/880Hz 金属公转回旋碰撞音，同一敌人接触冷却节流。 |
| **W04** | `WeaponTrigger_W04` (定点共振) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_resonance_drop.wav` | `-3.0` | 1 | 0.14s | 2 | **新变体**：580Hz$\to$110Hz 下潜内爆声，区域持续伤害不每帧响。 |
| **W05** | `WeaponChain_W05` (弹跳电弧) | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` *(pitch 1.15)* | `-4.0` | 1 | 0.06s | 2 | 2400Hz 快速电弧扫频，跳跃次数受预算封顶。 |
| **W06** | `WeaponTrigger_W06` (移动残响) | `Assets/Audio/S03/sfx_s03_dodge.wav` *(pitch 1.25)* | `-4.5` | 1 | 0.12s | 2 | 闪避布设留痕引爆，短促风鸣与电感泄压。 |
| **P12** | `DerivedEchoHit` (延迟回声) | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_echo_hit.wav` | `-3.5` | 1 | 0.06s | 2 | 180Hz 空心共振，原目标 0.5s 延迟二次受伤，**预备期严格静默**。 |
| **P13** | `DerivedPropagationHit` (目标传播) | `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` | `-4.5` | 1 | 0.05s | 2 | 0.1s 目标间折射跳跃，2400$\to$1200Hz 快速滑音。 |
| **P12_13**| `DerivedEchoPropagationHit` (回声传播)| `Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav` *(pitch 0.85)* | `-5.0` | 1 | 0.06s | 1 | 次生衍生音量压低，确保次级因果不盖过主目标。 |
| **Direct**| `Combat_DirectHit` (直接命中) | `Assets/Audio/S03/sfx_s03_enemy_hit.wav` | `-3.0` | 1 | 0.04s | 4 | 2800Hz 矿物脆断瞬态，代表物理首击起点。 |
| **Death** | `Combat_EnemyDeath` (敌人死亡) | `Assets/Audio/SFX/sfx_enemy_death_01.wav` ~ `03.wav` | `-2.0` | 3 | 0.06s | 3 | 次低频泄压 + 矿物碎裂，仅首次归零响，回池/重开不响。 |

### 2. 6 局内拾取物与成长 UI (Pickups & Growth UI)
| 标识 ID | 逻辑事件名 | 推荐挂接资源路径 (TEST_ONLY) | 音量 (`dB`) | 最短间隔 | 听觉特征与状态约束 |
|:---:|---|---|:---:|:---:|---|
| **D01** | `Pickup_ExpGem` (经验碎片) | `Assets/Audio/S03/sfx_s03_gem_pickup.wav` | `-3.0` | 0.03s | 连击拾取音调递增（+0.04 Pitch/个），超时 1.0s 重置。 |
| **D02** | `Pickup_HealPack` (生命回复) | `Assets/Audio/SFX/sfx_ui_levelup_confirm.wav` *(pitch 1.15)* | `-2.5` | 0.10s | 温暖电子管上扬琶音，20% HP 回复确认。 |
| **D03** | `Pickup_MagnetPull` (全场磁吸) | `Assets/Audio/S03/sfx_s03_dodge.wav` *(pitch 1.40)* | `-3.0` | 0.20s | 全场宝石引力聚拢风啸，不虚增经验总量。 |
| **D04** | `Pickup_ScreenPulse` (清场脉冲) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_resonance_drop.wav` *(pitch 0.85)* | `-1.5` | 0.30s | 重低频受控震荡，全屏清场冲击波。 |
| **D05** | `Pickup_BarrierShield` (临时屏障) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_guardian_orbit.wav` *(pitch 1.25)* | `-3.0` | 0.20s | 高亮金属护盾展开谐振，5s吸收屏障。 |
| **D06** | `Pickup_DodgeCharge` (闪避补给) | `Assets/Audio/SFX/sfx_player_dodge_01.wav` *(pitch 1.20)* | `-3.0` | 0.15s | 推进器高压短促泄压，闪避 CD 瞬间清零。 |
| **UI-1** | `Ui_OfferOpen` (三选一弹出) | `Assets/Audio/SFX/sfx_ui_levelup_appear.wav` | `0.0` | 0.10s | 卡牌悬浮展开，伴随音乐切换至挂起循环。 |
| **UI-2** | `Ui_CardSelected` (卡牌确认) | `Assets/Audio/S03/sfx_s03_levelup_chosen.wav` | `-1.0` | 0.10s | 簧片敲击与正向能量上扬，消费升级 Token。 |
| **UI-3** | `Ui_RerollClick` (卡牌重抽) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_ui_reroll.wav` | `-3.0` | 0.12s | **新变体**：机械齿轮棘轮三连轻拨声，Token 无效不播放。 |
| **UI-4** | `Ui_BanishClick` (卡牌放逐) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_ui_banish.wav` | `-3.0` | 0.15s | **新变体**：带通滤波降调消解声，确认剔除卡牌。 |
| **UI-5** | `Ui_SkipClick` (卡牌跳过) | `Assets/Audio/SFX/sfx_gem_pickup_01.wav` *(pitch 0.85)* | `-4.0` | 0.15s | 轻微退币回落声，伴随 5% 生命微量回血。 |
| **UI-6** | `Ui_EvolutionChestOpened` (进化宝箱) | `Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_evolution_unlocked.wav` | `-2.0` | 0.25s | **新变体**：G大调大三和弦金属共振展开，武器形态质变。 |

---

## 四、给 Codex 的跨目录最小接线请求 (Requests for Codex)

在后续 06B 将战斗与成长数据挂入 `AudioDirector` 时，请求提供以下支持：

1. **规则事件携带武器 ID 标识**：
   - 目前部分底层 `PresentationEvent` 仅包含位置与基础类型。请求在直接开火与直接命中事件中携带 `weaponId`（如 `"W01"`、`"W02"`），避免音效系统只能靠“当前选中的第一把武器”进行错误猜测；
2. **拾取物事件细分派发**：
   - 请求在玩家接触拾取物时，派发携带拾取类型的真实事件（`D01` ~ `D06`），以便播放对应的磁吸、急救或充能音效；
3. **成长 UI 按钮状态声学静音保护**：
   - 当 `Reroll`、`Banish` 或 `Skip` 剩余次数为 0，或当前 `OfferToken` 已失效被拒绝时，UI 请保持静音，不触发无效点击声。

---

## 五、需要用户在后续试玩中重点试听的 3 项 A/B 监听问题 (User Listening Checklist)

1. **【A/B 1】多武器同时发射时的声学辨识与干扰度**：
   - *监听重点*：当玩家同时装备 W01（定向单点）、W02（扇形霰弹）和 W03（环绕护卫）时，各武器的发射与击中声音能否清晰分辨？是否会出现中频泥泞或高频刺耳？
2. **【A/B 2】三选一界面中“重抽 / 放逐 / 跳过”的机械反馈质感**：
   - *监听重点*：点击重抽的齿轮棘轮声与放逐的消解降调声，是否符合“旧农机/电子调频器”的破损工业质感？是否能明确感知到资源已被消耗？
3. **【A/B 3】四图环境声与战斗音乐的动态平衡**：
   - *监听重点*：在秋收之路从 0 分钟过渡到 8 分钟的过程中，音乐从稀疏点弦推进到爆裂破音时，武器音效与敌人受击音是否依然清晰可闻？受击压低（Ducking）是否自然而不突兀？
