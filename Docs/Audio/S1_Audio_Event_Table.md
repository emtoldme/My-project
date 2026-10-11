# S1 音频事件与系统接入契约表 (S1 Audio Event & Integration Contract)

**版本**：v1.0 (基线 S1 灰盒阶段)  
**编写人**：音效总监  
**对接负责人**：2 号（负责接入 AudioMixer、音频管理脚本与正式场景事件绑定）  
**协作接口基线**：以当前 Waffle 框架 `RunSimulation` 的 T0–T13 循环与 `RunPhase` 状态为准。

---

## 一、音频接入核心原则 (避免代码重复与混乱)

1. **单一入口触发 (Single Event Authority)**：
   - 同一个音效事件必须由唯一的业务逻辑入口触发（如伤害结算、经验收集），**严禁动画帧事件、特效 Prefab 唤醒脚本与战斗代码同时播放同一种音效**造成叠加轰鸣。
2. **音乐与音效分离解耦**：
   - 音乐的节拍仅服务于听觉氛围，游戏内命中与闪避判定完全由 1 号战斗系统负责，音频系统绝不反向干预玩法判定。
3. **全局防毛刺与声音挤占 (Voice Stealing)**：
   - 高频并发事件（如普通命中）必须设定并发上限（Max Polyphony）与最短触发间隔（Min Interval），防止怪物群成批受击时波形叠加出现数字削波失真（Digital Intersample Peak）。

---

## 二、S1 核心事件映射表

| 语义事件名称 (Semantic Event) | 对应音频资产 (Assets/Audio/SFX/) | 触发时序 / 游戏系统条件 (Tick / Phase) | 推荐音量 (dB) | 音高微随机 (Pitch Rnd) | 最大并发 (Max Poly) | 最短触发间隔 (Cooldown) | 优先级 (1最高) | 2D/3D 模式 | 停止 / 淡出规则 |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| `Event_Player_Shoot` | `sfx_player_shoot_01~03.wav` (随机抽取) | 玩家自动武器开火判定瞬间 (T3/T4) | `-2.0 dB` | `±0.05` | 3 | 80 ms | 3 | **2D** | 播放完自动释放 |
| `Event_Enemy_Hit` | `sfx_enemy_hit_01~03.wav` (随机抽取) | 子弹穿透碰撞体确认造成伤害瞬间 (T5) | `-3.0 dB` | `±0.08` | **4** (挤占) | 40 ms | 4 | 2D / 3D (Blend 0.4) | 播放完自动释放 |
| `Event_Enemy_Death` | `sfx_enemy_death_01~03.wav` (随机抽取) | 敌人 HP <= 0 触发死亡解构瞬间 (T6) | `-1.5 dB` | `±0.04` | 3 | 60 ms | 2 | **3D** (Max 20m) | 播放完自动释放 |
| `Event_Player_Hurt` | `sfx_player_hurt_01~02.wav` (轮换) | 玩家本体受到任何扣血伤害瞬间 (T7) | **`+0.5 dB`** | 无 (保持穿透基频) | 1 (重置) | 150 ms | **1 (最高)** | **2D** | 触发时向 Music 发送 Ducking 信号 |
| `Event_Player_Dodge` | `sfx_player_dodge_01~02.wav` | 玩家按下 Space / 手动闪避技能发动 (T1) | `-1.0 dB` | `±0.03` | 1 | 200 ms | 2 | **2D** | 播放完自动释放 |
| `Event_Gem_Pickup` | `sfx_gem_pickup_01~05.wav` (按连续拾取计数器顺序播放) | 玩家磁吸经验晶体接触碰撞 (T10) | `-2.5 dB` | 无 (保持纯净五声音阶) | 5 | 30 ms | 3 | **2D** | 播放完自动释放，1秒无拾取计数器归1 |
| `Event_LevelUp_Appear` | `sfx_ui_levelup_appear.wav` | 经验池满，进入 LevelUp Phase 时间停滞瞬间 | `0.0 dB` | 无 | 1 | - | 1 | **2D** | 独占播放，伴随战斗 BGM 淡出 |
| `Event_LevelUp_Confirm` | `sfx_ui_levelup_confirm.wav` | 玩家点击或按键确认三选一卡牌升级瞬间 | `+1.0 dB` | 无 | 1 | - | 1 | **2D** | 独占播放，触发战斗 BGM 淡入 |
| `Event_Game_Victory` | `sfx_game_victory.wav` | 局内倒计时到达 3:00，进入 Victory Phase | `+1.0 dB` | 无 | 1 | - | 1 | **2D** | 独占播放，战斗 BGM 淡出 |
| `Event_Game_Defeat` | `sfx_game_defeat.wav` | 玩家 HP <= 0，进入 Defeat Phase | `+1.0 dB` | 无 | 1 | - | 1 | **2D** | 独占播放，战斗 BGM 瞬间低通截断 |

---

## 三、Unity AudioMixer 架构设计建议

建议 2 号在 Unity 中创建 `Assets/Audio/AudioMixer_S1.mixer`，总线结构设计如下：

```text
Master Bus (0 dB, Peak Limiter: Ceiling -0.5dB)
├── Music Bus (推子 0 dB, Ducking 接收端: 降 -8dB)
│   ├── Combat_Music (战斗 BGM 主干与分轨)
│   └── LevelUp_Ambient (升级暂停空灵悬浮层)
└── SFX Bus (推子 0 dB)
    ├── SFX_Player (射击、闪避、UI)
    ├── SFX_Enemies (命中、死亡，带全局 Voice Limiter)
    └── SFX_Alert (玩家受伤 Hurt，内置 Ducking 发送端 DuckMusicSend)
```
