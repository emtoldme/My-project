# S1 音频事件与系统接入契约表 (S1 Audio Event Table v1.1)

**维护岗位**：音效总监（Sound Director）  
**基线版本**：基于 `origin/main` 实际代码 `PresentationEventType` 与 `AudioDirector.cs` 实现  
**对接负责人**：2 号（负责音频生命周期与音频小修）、Codex（负责桥接与整合）  
**设计主键**：统一采用**语义事件（Semantic Presentation Event）**作为唯一主键，禁止使用自造的 T 编号作为主键。

---

## 一、核心接入原则

1. **表现事件解耦**：
   - 游戏规则层（`Rules/Combat`）负责根据时钟推进发出对应的 `PresentationEvent`，表现层 `AudioDirector` 纯粹订阅该事件队列分发声音，绝不反向干预玩法判定。
2. **唯一入口防轰鸣 (Single Event Authority)**：
   - 同一个语义事件只能由确定的代码入口发出（如扣血发出 `PlayerHurt`，武器开火发出 `Shot`），禁止在特效唤醒、动画事件中重复播放。
3. **并发抑制与声部挤占 (Voice Stealing)**：
   - 频繁击中敌人时，若并发超过声部上限（如 4 声部），自动抢占最早开始播放的声道，杜绝高频重叠引发数字削波。

---

## 二、实际代码事件映射表 (与 AudioDirector.cs 1:1 严格对齐)

| 语义事件名称 (`PresentationEventType`) | 对应音频资产 (`Assets/Audio/SFX/`) | 触发时机 / 业务系统语义 | 默认音量 (`volumeDb`) | 音高随机范围 (`pitchRandom`) | 声部并发上限 (`maxVoices`) | 最短触发间隔 (`minInterval`) | 空间声像 (`spatialBlend`) | 最大听觉距离 (`maxDistance`) | 特殊混音与生命周期规则 |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **`Shot`** | `sfx_player_shoot_01~03.wav` | 玩家武器判定开火瞬间 | `-2.0 dB` | `±0.05` | 3 | 0.08 s (80ms) | 0.0 (2D) | 25 m | 随机抽取变体，超时或被挤占自动停止 |
| **`EnemyHit`** | `sfx_enemy_hit_01~03.wav` | 子弹击中敌人并扣血瞬间 | `-3.0 dB` | `±0.08` | **4 (挤占)** | 0.04 s (40ms) | 0.4 (偏2D立体) | 25 m | 随机抽取，超过4并发自动抢占最早声部 |
| **`EnemyKilled`** | `sfx_enemy_death_01~03.wav` | 敌人生命值归零死亡结算瞬间 | `-1.5 dB` | `±0.04` | 3 | 0.06 s (60ms) | 1.0 (3D定位) | 20 m | 随机抽取，声音位置取事件发生的 `(X, 0, Z)` |
| **`PlayerHurt`** | `sfx_player_hurt_01~02.wav` | 玩家本体受到扣血伤害瞬间（无敌挡下时不发） | **`+0.5 dB`** | 0.0 (固定基频) | 1 | 0.15 s (150ms) | 0.0 (2D) | 25 m | **触发 Music Ducking**：压低音乐 `-8dB`（10ms攻/200ms持/600ms释放） |
| **`Dodge`** | `sfx_player_dodge_01~02.wav` | 玩家手动触发闪避动作瞬间 | `-1.0 dB` | `±0.03` | 1 | 0.20 s (200ms) | 0.0 (2D) | 25 m | 随机抽取变体 |
| **`GemPickup`** | `sfx_gem_pickup_01~05.wav` | 玩家触碰并吸收经验宝石瞬间 | `-2.5 dB` | 0.0 (保持五声音阶) | 5 | 0.03 s (30ms) | 0.0 (2D) | 25 m | **连续拾取连击**：1秒内连续吃音阶 01→05 递增，超时重置为 01 |
| **`LevelUpShown`** | `sfx_ui_levelup_appear.wav` | 进入 LevelUp Phase 升级面板展示瞬间 | `0.0 dB` | 0.0 | 1 | 0.00 s | 0.0 (2D) | 25 m | 战斗 BGM 在 1.0s 内淡出，悬浮循环音乐在 1.0s 内淡入 |
| **`LevelUpChosen`** | `sfx_ui_levelup_confirm.wav` | 玩家确认接受升级卡牌选择瞬间 | `+1.0 dB` | 0.0 | 1 | 0.00 s | 0.0 (2D) | 25 m | 若无剩余待升级卡牌，战斗 BGM 在 1.4s 内淡入，悬浮音乐淡出 |
| **`Paused`** | *(无单发音效)* | 玩家按下 Esc 进入暂停状态瞬间 | - | - | - | - | - | - | `AudioDirector.SetPaused(true)`：冻结所有正在播放的 SFX 与 BGM |
| **`Resumed`** | *(无单发音效)* | 玩家解除暂停回到游戏瞬间 | - | - | - | - | - | - | `AudioDirector.SetPaused(false)`：解冻恢复之前播放的声音 |
| **`Victory`** | `sfx_game_victory.wav` | 局内生存达到 3:00 判定胜利瞬间 | `+1.0 dB` | 0.0 | 1 | 0.00 s | 0.0 (2D) | 25 m | 战斗 BGM 在 2.0s 内淡出，长鸣和弦定格播放 |
| **`Defeat`** | `sfx_game_defeat.wav` | 玩家生命归零判定失败瞬间 | `+1.0 dB` | 0.0 | 1 | 0.00 s | 0.0 (2D) | 25 m | 战斗 BGM 在 0.3s 内急促切断并开启 Lowpass 下潜（22kHz→200Hz） |
| **`Restart`** | *(无单发音效)* | 游戏重开或重新加载场景前瞬间 | - | - | - | - | - | - | `AudioDirector.StopAll()`：立即截断清空全部音频声道，防场景残留 |

---

## 三、混音余量设计说明 (`sfxTrimDb`)

由于事件表中定义了部分需要强烈穿透的前景声音（例如 `PlayerHurt` 音量为 `+0.5 dB`，`LevelUpChosen` 为 `+1.0 dB`），而 Unity 原生 `AudioSource.volume` 线性范围上限为 `1.0`（对应 `0 dB`）。

因此在 `AudioDirector.cs` 内部已经设计了 **`sfxTrimDb = -3.0 dB`** 的统一全局衰减。所有音效的实际播放增益计算公式为：

$$\text{VolumeLinear} = 10^{\frac{\text{sfxTrimDb} + \text{volumeDb}}{20}}$$

这一设计既保证了正增益音效绝不失真溢出，又保留了受击与升级相对于普通射击（`-2dB`）和命中（`-3dB`）的高低动态差。
