# M01 “回声＋传播”机制声响应接线与时序契约表 (M01 Audio Response Specification)

**任务标识**：Task M01（机制试玩声响应接线表：`review/m01-audio-response`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`dc8b096`)  
**交付分支**：`review/m01-audio-response`  
**协作边界**：仅写入 `Docs/Experiments/M01-Audio/`；未修改场景、`AudioDirector.cs`、DSP 控制器、或新增正式音频素材。

---

## 一、核心接入与声学设计原则

依据 `Docs/Integration/REVERB_TASKS_03.md` 与《麦田里的混响》（Reverb in the Rye）整体协作规范：
1. **纯被动响应原则 (Strictly Passive Audio)**：
   - 声音仅订阅并跟随规则层已经结算的事件（直接命中、回声伤害、传播伤害、敌人死亡），**绝不自行设立独立计时器或参与任何判定反写伤害**；
2. **时序与辨识分离 (Temporal & Frequency Separation)**：
   - **直接命中（Direct Hit）**：$t = 0.0\text{s}$，高频干燥、脆性物理打击瞬态；
   - **目标间传播（Propagation）**：$t = 0.1\text{s}$，极短快速电脉冲 / 偏心跳跃音色；
   - **延迟回声（Echo Hit）**：$t = 0.5\text{s}$，带轻微滤波衰减的次生敲击共振；
   - **回声传播（Echo Propagation）**：$t = 0.5\text{s} + 0.1\text{s} = 0.6\text{s}$，复合电脉冲跳跃；
   - 通过确定的时间阶梯（$0.0\text{s} \to 0.1\text{s} \to 0.5\text{s} \to 0.6\text{s}$），即使同屏密集触发，人耳也能清晰辨识出派生因果链。
3. **严格并发限制与声部挤占 (Voice Stealing & Rate Limiting)**：
   - 传播与回声可能在 1 秒内引发多级级联，必须施加严格的通道上限与最短触发间隔，防止高频叠加引起总线数字削波或刺耳轰鸣。
4. **现有合法 TEST_ONLY 资产复用**：
   - 充分复用 `Assets/Audio/S03/` 与 `Assets/Audio/SFX/` 中已验收的 48kHz / 24-bit 资产，避免重复造轮子或向用户索要正式素材。

---

## 二、机制声响应接线映射总表 (供 Codex 接入 AudioDirector)

| 机制事件语义 | 触发时机 / 规则来源 | 推荐复用音频资产 (TEST_ONLY) | 默认音量 (`volumeDb`) | 并发上限 (`maxVoices`) | 最短触发间隔 (`minInterval`) | 空间声像 (`spatialBlend`) | 衰减距离 (`maxDist`) | 声学特征与混音处理说明 |
|---|---|---|:---:|:---:|:---:|:---:|:---:|---|
| **直接命中 (`DirectHit`)** | 玩家自动武器子弹直接击中目标（$t=0.0\text{s}$） | `Assets/Audio/S03/sfx_s03_enemy_hit.wav` | `-3.0 dB` | 4 (挤占) | 0.04s (40ms) | 0.4 (偏2D立体) | 25m | 2800Hz 确定单音高矿物脆断瞬态（0.07s），快速利落，作为因果序列的起点。 |
| **目标间传播 (`Propagation`)** | 直接命中后延时 0.1s 传播至 5 米内邻近活目标 | `Assets/Audio/S03/sfx_s03_shot.wav` *(微调音高 +0.15)* | `-4.5 dB` | 3 (挤占) | 0.05s (50ms) | 0.6 (3D偏重) | 20m | 复用 1680Hz 物理瞬态，音量稍低（-4.5dB），音高微抬，表现电弧跳跃与偏心位移感。 |
| **延迟回声 (`EchoHit`)** | 直接命中后延时 0.5s 原目标本体产生回声伤害 | `Assets/Audio/SFX/sfx_enemy_hit_01.wav` *(带轻度低通滤波)* | `-3.5 dB` | 3 (挤占) | 0.06s (60ms) | 0.5 (3D立体) | 25m | 相比直接命中，声学上强调“共振余波”，音色稍厚、尾音稍长，与直接命中形成明显虚实对比。 |
| **回声传播 (`EchoPropagation`)** | 回声命中后延时 0.1s（总延时 0.6s）次生跳跃 | `Assets/Audio/S03/sfx_s03_shot.wav` *(微调音高 -0.1)* | `-5.0 dB` | 2 (挤占) | 0.06s (60ms) | 0.7 (3D定位) | 20m | 音量进一步微降，声像紧随受损次级目标，避免在多怪群中抢占前景主体。 |
| **机制击杀 (`MechanismKill`)** | 无论是直接、回声或传播伤害导致敌人生命归零 | `Assets/Audio/SFX/sfx_enemy_death_01.wav` (变体1~3随机) | `-2.0 dB` | 3 (挤占) | 0.08s (80ms) | 1.0 (纯3D定位) | 20m | 玻璃与矿物碎裂 + 次低频泄压，确立消灭目标的最终反馈。 |
| **未命中 / 目标失效** | 目标已死亡或代号回池无效 | *(无声音 / 保持静默)* | - | - | - | - | - | **严格静音**：无效回声不播放任何声音，避免虚假反馈。 |

---

## 三、时序与并发防轰鸣控制机制 (Rate-Limiting Specification)

在“回声＋传播”组合模式（`Combined`）下，1 次直接命中会在短时间内衍生出最多 3 次附加伤害（1 次直接传播 + 1 次延迟回声 + 1 次回声传播）。

```text
时间轴 (Time Axis):
  t = 0.0s : [DirectHit]          --> sfx_s03_enemy_hit (2800Hz 脆断瞬态, -3.0dB)
  t = 0.1s : [Propagation]        --> sfx_s03_shot (高音电脉冲, -4.5dB, 目标B位置)
  t = 0.5s : [EchoHit]            --> sfx_enemy_hit_01 (低通共振余音, -3.5dB, 目标A位置)
  t = 0.6s : [EchoPropagation]    --> sfx_s03_shot (低音电脉冲, -5.0dB, 目标C位置)
```

### 1. 最小时间间隔（Min Interval Throttle）
- 同类型机制音效如果由于散弹或多发武器在同一物理帧（Tick）内触发，强制执行 `minInterval` 限制：
  - `Propagation`：同一目标或相邻位置 50ms 内只播放 1 次；
  - `EchoHit`：同一目标 60ms 内只播放 1 次；
- 超出间隔但在同一帧触发的事件，静默处理（只计数值，不发重复声音）。

### 2. 声道抢占原则 (Voice Stealing)
- 传播与回声各自占用独立的并发通道池（`maxVoices = 3`）；
- 当并发超过 3 时，**自动抢占最早开始播放的声道**，并施加 5ms 平滑淡出，绝不直接硬截断（避免硬切咔哒声）。

---

## 四、关于测试双轨音乐（BGM）的边界声明

1. **当前 BGM 属性保持不变**：
   - 现有的 `TEST_ONLY_track_a_base_16s_v2.wav` 与 `TEST_ONLY_track_b_shimmer_16s_v2.wav` 依然作为**纯技术测试信号**保留在 `Assets/Experiments/AudioSync/`；
   - 它们已在上一轮中实现了首尾零阶跃（`end_jump = 0.000`）与充足的母带余量（单轨 -6.0 dBFS，叠加 -3.61 dBFS），**仅用于验证双轨 DSP 调度与抗漂移能力**；
2. **本轮不伪称正式配乐**：
   - 绝不将 V2 测试信号宣称为正式乡村怪谈、数学摇滚、后摇或电子配乐；
   - 正式风格配乐与分轨（Stems）将在后续由用户或专业音乐人提供/指定。

---

## 五、给 Codex 的跨目录接入建议 (AudioDirector 最小改造需求)

在后续 Codex 编写 M01 独立试玩桥接代码时，建议在 `AudioDirector` 或 M01 专有表现控制器中提供如下轻量接口或事件监听：

```csharp
// 建议的表现事件扩展或专有回调（由 Codex 统筹决定实现方式）：
public enum MechanismAudioEventType
{
    DirectHit = 0,
    Propagation = 1,
    EchoHit = 2,
    EchoPropagation = 3,
    Kill = 4
}

public void PlayMechanismAudio(MechanismAudioEventType type, Vector3 worldPosition);
```

- 该方法内部通过字典或结构体数组查表应用上述的 `volumeDb`、`pitch`、`spatialBlend` 与 `minInterval` 节流；
- 若当前尚未接入，保持当前默认的普通 `EnemyHit` 音效，系统平稳降级，绝不阻塞玩法运行。
