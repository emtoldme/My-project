# E-03 双轨 DSP 同步混音技术实验报告 (DSP Sync & Vertical Layering Report)

> 作者初版未附实际Unity结果。Codex整合时补真实实验场景，加入音轨兼容检查、统一DSP恢复、生命周期清理、环形采样差和跨音频更新读数排除，移除运行中强制seek纠偏。下面原方案中的顺序UnPause/“绝对锁步”不能作为当前实现或已验证结论。实际结果以Docs/Verification/Reverb-Experiments-01为准；全部音频仅TEST_ONLY，不能代表正式音乐。

**任务标识**：Task E-03  
**作者身份**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`9c565ee`)  
**交付分支**：`experiment/e03-dsp-sync`  
**协作边界**：仅写入 `Assets/Experiments/AudioSync/` 与 `Docs/Experiments/E03/`；未修改 `AudioDirector.cs`、S1 灰盒场景、正式材质、Mixer 或他人素材。

---

## 一、实验目的与背景

在《麦田里的混响》（Reverb in the Rye）长期架构规划中，音乐表现将采用**垂直分层（Vertical Layering）与动态反应式混音**：
- 场景中存在多条同长度、同拍速、同采样率的音频分轨（Stems）；
- 在探索或低烈度战斗时，仅开放基础底床与节奏脉冲；
- 当敌群激增、受到重大伤害或触发特定事件时，高频氛围、噪音或失真分轨平滑淡入；
- 这一切要求：**多条音轨必须在底层音频引擎时间戳（DSP Time）上绝对严格锁步启动**，任何几毫秒的相位偏差都会导致打击乐抵消或混浊。

本实验为纯隔离技术验证，在不引入第三方插件（如 FMOD/Wwise）的前提下，基于 Unity 原生音频引擎（Unity 6000.4.3f1 AudioSource）验证：
1. 双轨 `AudioSettings.dspTime` 预调度启动精度；
2. 动态垂直分层增益插值过渡；
3. 暂停（Pause）与恢复（Resume）时的采样点自动对齐机制；
4. 循环回绕（Loop Wrap）时的长时间漂移容差。

---

## 二、交付物清单（Assets/Experiments/AudioSync/）

| 文件路径 | 类型 | 规格 / 说明 |
|---|---|---|
| `Reverb.Experiments.AudioSync.asmdef` | 独立程序集定义 | 隔离测试程序集，不污染主工程运行 |
| `DspSyncExperimentController.cs` | 核心逻辑脚本 | 实现 DSP 预调度启动、分层平滑增益、暂停防脱节恢复与实时漂移检测 |
| `DspSyncDebugGui.cs` | 调试与演示组件 | 提供可视化的 OnGUI 测试面板，支持一键启停、分层切换与微秒级漂移监控 |
| `TEST_ONLY_track_a_base_16s.wav` | 测试音频分轨 A | 48kHz / 24-bit PCM，16.0s，峰值 -2.0 dBFS（基础节奏底床与乡村机械脉动） |
| `TEST_ONLY_track_b_shimmer_16s.wav` | 测试音频分轨 B | 48kHz / 24-bit PCM，16.0s，峰值 -2.0 dBFS（高频氛围微光与空间谐波分层） |
| 所有对应 `.meta` 文件 | Unity 资源元数据 | 完整保留 GUID，48kHz 原生导入配置 |

---

## 三、核心技术实现方案

### 1. DSP 启动预调度与锁步启动
直接调用 `AudioSource.Play()` 受 Unity 主线程 Update 帧率抖动（Jitter）影响，多轨之间可能产生 16ms~33ms 的随机相位差。
因此，控制器采用精确音频时钟预调度：
```csharp
// 核心：利用 AudioSettings.dspTime 加上安全预调度延迟（如 150ms）
scheduledStartTime = AudioSettings.dspTime + scheduleDelaySec;
sourceA.timeSamples = 0;
sourceB.timeSamples = 0;
sourceA.PlayScheduled(scheduledStartTime);
sourceB.PlayScheduled(scheduledStartTime);
```
此方案使音频硬件驱动在底层音频渲染线程直接触发解码播放，抹平了主线程帧率波动。

### 2. 暂停与恢复重对齐（Anti-Desync on Resume）
移动设备或 Windows 系统音频驱动在被唤醒或窗口失焦时，两个 AudioSource 可能出现不同步被解冻的情况。控制器在恢复播放时实施基准对齐：
```csharp
public void ResumeSync()
{
    // 强制以 Track A 当前采样点为绝对基准重新对齐 Track B
    int resumeSample = sourceA.timeSamples;
    sourceB.timeSamples = resumeSample;

        // 初版代码：顺序UnPause并不保证恢复的统一DSP起点。
        // 当前实现在相同resumeSample上为双轨重新PlayScheduled。
}
```

### 3. 垂直分层增益（Vertical Layering）
采用不受物理时钟缩放影响的平滑插值（`Time.unscaledDeltaTime`），在 1.0 秒内完成增益过渡，实现无缝的情绪递进：
- **平静状态**：`Track A = 1.0`，`Track B = 0.0`；
- **警戒状态**：`Track A = 1.0`，`Track B = 0.7`；
- **战斗爆发**：`Track A = 0.8`，`Track B = 1.0`。

---

## 四、测试方法与可运行入口（供 Codex 复现与验收）

### 运行步骤
1. 在 Unity Editor 中打开任意空测试场景（或新建隔离场景）；
2. 创建一个空的 GameObject，命名为 `[E03_AudioSync_Harness]`；
3. 挂载 `DspSyncExperimentController` 与 `DspSyncDebugGui` 组件；
4. 将 `Assets/Experiments/AudioSync/TEST_ONLY_track_a_base_16s.wav` 拖入 `trackA`；
5. 将 `Assets/Experiments/AudioSync/TEST_ONLY_track_b_shimmer_16s.wav` 拖入 `trackB`；
6. 进入 PlayMode，在左上角的 GUI 面板上点击 **“启动 DSP 预定同步播放”**；
7. 观察屏幕上的**实时采样点漂移**，并在播放期间多次点击“暂停/恢复”以及分层增益切换。

### 客观指标与验收标准
- **启动时间差**：在预调度下理论差值为 **0 采样点**（$0.00\text{ ms}$）；
- **连续循环观察目标**：连续循环10次，记录有效timeSamples环形差及跨音频更新丢弃数；64samples为本实验建议门槛，不是已验证成绩或普遍听觉阈值。主线程观察不能认证硬件输出相位。
- **防爆音**：需要真实输出捕获/试听；增益插值和采样差通过不能单独证明无瞬态爆音。

---

## 五、未来用户正式分轨（Stems）交付技术规范

为便于后续音乐团队或用户提供正式配乐分轨，建议遵守以下格式要求：
1. **统一时间长度**：所有同组 Stems 文件的 sample 数必须**严格完全一致**（精确到单个采样点）；
2. **格式标准**：48,000 Hz，24-bit PCM WAV，立体声（2ch）；
3. **电平余量**：逐轨峰值和母线峰值分别测量，True Peak用dBTP表述；单轨-3dB不能保证多轨叠加不削波，必须在全开与实际增益组合下验证输出余量。
4. **尾音与循环**：分轨导出的首尾须带有相同的等功率交叉淡化或完全无缝切点。
