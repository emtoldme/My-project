# S1 最小音效包交付资产清单 (S1 SFX Isolated Asset Package)

**交付路径**：`Assets/Audio/SFX/`  
**格式规范**：48,000 Hz / 24-bit PCM WAV / 单声道 (Mono) 或 立体声 (Stereo)  
**电平标准**：峰值 True Peak 控制在 -1.5 dBFS 内，无任何削波失真  

---

## 一、S1 核心事件覆盖清单

| 资产文件名 | 对应事件 (Event Key) | 变体数 | 时长 | 材质与声学构成 | 反馈分类 | 状态 |
|---|---|:---:|:---:|---|---|:---:|
| `sfx_player_shoot_01.wav`<br>`sfx_player_shoot_02.wav`<br>`sfx_player_shoot_03.wav` | `Event_Player_Shoot` | 3 | 0.11s | 真实陶瓷 40ms 清脆瞬态 + 板材物理击打 + 1.5kHz 极微金属共鸣开缝，短促干燥 | 玩家主动反馈 | **已就绪** (2号可接入) |
| `sfx_enemy_hit_01.wav`<br>`sfx_enemy_hit_02.wav`<br>`sfx_enemy_hit_03.wav` | `Event_Enemy_Hit` | 3 | 0.085s | 真实小碎石对撞颗粒感瞬态 + 3.4kHz 高频矿物脆裂微响，无冗余长尾 | 敌人受击反馈 | **已就绪** (2号可接入) |
| `sfx_enemy_death_01.wav`<br>`sfx_enemy_death_02.wav`<br>`sfx_enemy_death_03.wav` | `Event_Enemy_Death` | 3 | 0.42s | 玻璃碎裂坍塌 + 结构骨架断裂 + 75Hz 下降能量泄压 | 敌人消亡反馈 | **已就绪** (2号可接入) |
| `sfx_player_hurt_01.wav`<br>`sfx_player_hurt_02.wav` | `Event_Player_Hurt` | 2 | 0.36s | 62Hz/48Hz 强力次低频下潜 + 薄金属过载失真 + 重浊撞击，极强穿透力 | 玩家危机警报 | **已就绪** (2号可接入) |
| `sfx_player_dodge_01.wav`<br>`sfx_player_dodge_02.wav` | `Event_Player_Dodge` | 2 | 0.26s | 薄金属片高速划空气流切面 + 能量护罩微颤 | 玩家位移反馈 | **已就绪** (2号可接入) |
| `sfx_gem_pickup_01.wav`<br>`sfx_gem_pickup_02.wav`<br>`sfx_gem_pickup_03.wav`<br>`sfx_gem_pickup_04.wav`<br>`sfx_gem_pickup_05.wav` | `Event_Gem_Pickup` | 5 | 0.22s | 金属微粒滴落 + C6/D6/E6/G6/A6 纯五声音阶水晶谐振，连吃音乐化且无疲劳 | 玩家收益反馈 | **已就绪** (2号可接入) |
| `sfx_ui_levelup_appear.wav` | `Event_LevelUp_Appear` | 1 | 0.55s | 雕塑展开虚空声 + F#5/C#6 空灵双音微光长鸣 | UI / 状态反馈 | **已就绪** (2号可接入) |
| `sfx_ui_levelup_confirm.wav` | `Event_LevelUp_Confirm` | 1 | 0.75s | D大调复合和弦定格 + 金属机构锁定 + 低频贯通能量脉冲 | UI / 状态反馈 | **已就绪** (2号可接入) |
| `sfx_game_victory.wav` | `Event_Game_Victory` | 1 | 3.20s | D大调纯净神圣的共振雕塑长鸣和弦舒展延展 | 终局胜败结算 | **已就绪** (待2号接入结算) |
| `sfx_game_defeat.wav` | `Event_Game_Defeat` | 1 | 2.80s | 雕塑整体骨架碎裂 + 55Hz 低频消散沉降轰鸣 | 终局胜败结算 | **已就绪** (待2号接入结算) |

---

## 二、2号接入技术建议（防疲劳与多并发抑制规则）

1. **射击声音 (`sfx_player_shoot`)**：
   - 触发时从 01–03 中随机抽取（避免同一音高连续播放）；
   - 每次播放应用 `Pitch: 1.0 ± 0.05` 微随机化；
   - 最短触发间隔：`80ms`。
2. **命中声音 (`sfx_enemy_hit`)**：
   - 采用 Voice Stealing（声音挤占）机制，最大全局并发上限限制为 **4**（超过 4 个命中声时抢占最早播放的声音，杜绝上百只敌人同时被穿透时造成白噪化）；
   - 最短触发间隔：`40ms`。
3. **经验拾取 (`sfx_gem_pickup`)**：
   - 建议在 2号脚本中维护一个连击计数器（Streak Counter）：连续拾取时按 01 -> 02 -> 03 -> 04 -> 05 循环递增音阶，若超过 1.0 秒未拾取则重置回 01；
   - 带来清脆悦耳、宛如弹奏竖琴/水晶琴一般的强烈成长正反馈。
