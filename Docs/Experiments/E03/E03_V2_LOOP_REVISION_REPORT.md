# E-03 TEST_ONLY 测试信号 V2 循环修订与复核报告 (E-03 Test Signal V2 Loops Report)

**任务标识**：Task E-03（第二轮协作收尾与技术复核：`fix/e03-test-signal-loops`）  
**作者身份**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`2d113dd`)  
**交付分支**：`fix/e03-test-signal-loops`  
**协作边界**：仅写入 `Assets/Experiments/AudioSync/`（音频资产及生成工具）与 `Docs/Experiments/E03/`；未修改 `DspSyncExperimentController.cs`、`DspStartProbe.cs`、`DspSyncDebugGui.cs`、`E03_AudioSync.unity` 场景及其他非音频文件。

> Codex整合复核：作者交付头6052d21保留在Git历史。本页以下原始推论已纠正；实际离线数据见Docs/Verification/Reverb-Followup-02/audio-audit.json。生成器已改为numpy＋标准库wave，支持独立输出目录；重生成与作者PCM最大差1个24bit量化单位，不宣称逐字节一致。源WAV原样保留。Unity场景V2接线由Codex处理，非作者已运行证明。

---

## 一、复核背景与解决的问题

在第一轮实验收拢中，Codex 对 E-03 提交的原始测试信号（V1）执行了离线 PCM 分析与真实 Unity PlayMode 测试（详见 `Docs/Verification/Reverb-Experiments-01/REVIEW.md` 及 `audio-audit.json`），明确指出了以下客观边界与缺陷：
1. **首尾跳跃严重（Loop End Jump）**：原两轨首尾电平阶跃分别达 **0.471** 与 **0.487**（接近 50% 满幅），循环回绕时存在明显瞬态不连续；
2. **多轨叠加峰值超标（Mix Headroom Deficit）**：原两轨单轨峰值为 -2.0 dBFS，在 GUI 分层增益叠加时（如 1.0/0.7 与 0.8/1.0），未 Trim 混合电平分别达到 **+1.23 dBFS** 与 **+1.52 dBFS**，超出满幅。虽然 Codex 在控制器中增加了 0.5 的 Output Trim 临时保护，但源素材余量本身不足；
3. **测试探针与同步起音匹配**：Codex 补入了基于音频线程的 `DspStartProbe`，依靠非零样点（$|\text{sample}| \ge 1\times 10^{-6}$）检查两轨真实启动偏差。新修订的测试信号必须保留同一帧的同步起音标记，杜绝因静音前导导致探针捕获失效。

---

## 二、V2 测试信号技术修订方案

依据 `Docs/Integration/REVERB_TASKS_02.md` 的明确要求：
- **保留原 V1 文件**：保留 `TEST_ONLY_track_a_base_16s.wav` 与 `TEST_ONLY_track_b_shimmer_16s.wav`，供历史数据与基线结果追溯；
- **交付 V2 新文件**：新增 `TEST_ONLY_track_a_base_16s_v2.wav` 与 `TEST_ONLY_track_b_shimmer_16s_v2.wav`；
- **提供完整生成源码**：提供离线可复现的 Python 生成脚本 `Assets/Experiments/AudioSync/generate_e03_v2.py`。

### 1. 首尾样点归零，仍有起音斜率变化
- **原理**：所有基础音色均采用基于 16.0 秒基准周期的整数倍谐波（$K = f \times 16.0$，如 49Hz, 98Hz, 196Hz, 294Hz 等），并在首尾应用 10ms（480 samples）的正弦平方对称切点平滑；
- **端点测量结果**：
  - 首样点值（Sample 0）：**`0.000000`**；
  - 尾样点值（Sample 767,999）：**`0.000000`**；
  - **首尾阶跃（End Jump）**：**`0.0000000000`**（彻底归零，消除了 V1 的 0.47/0.49 阶跃）；
  - **首末一阶差分变化**：A约0.00542414，B约0.00815976满幅/样点；B并不小于0.008。同步标记会造成非零起音斜率，因此不能称C1连续、无爆音或真人听感通过。首尾归零只消除了原来的端点电平阶跃。

### 2. 充足的母线混音余量设计 (Ample Headroom)
- **单轨峰值控制**：两轨均严格归一化至 **`-6.000 dBFS`**（线性峰值约 0.501）；
- **全增益叠加测试**：
  - 在 `GainA = 1.0, GainB = 0.7` 时，**未 Trim 混合峰值仅为 `-3.929 dBFS`**，超过 0dBFS 样点数：**0**；
  - 在 `GainA = 0.8, GainB = 1.0` 时，**未 Trim 混合峰值仅为 `-3.611 dBFS`**，超过 0dBFS 样点数：**0**；
  - 在 `GainA = 0.7, GainB = 0.6` 时，**未 Trim 混合峰值仅为 `-6.325 dBFS`**，超过 0dBFS 样点数：**0**；
  - 新增实际1.0/1.0全开审计：未Trim样点峰值约-2.630dBFS、超过满幅0个样点，Trim后约-8.650dBFS，并非全开仍低于-9.6dBFS。结论限于这些源信号、对齐关系与0~1范围的增益；不能保证滤镜、混响、重采样或任意增益下的峰值，也不是True Peak认证。

### 3. 同步起音标记与 DspStartProbe 探针无缝兼容
- **精准帧对齐**：在第 0 采样点保持 `0.000000`（保证循环闭合），在第 1 采样点（Sample Index 1）注入严格相同的 480Hz 高频微脉冲标记（Track A 为 `0.00542`，Track B 为 `0.00816`，均显著高于探针的 $1.0\times 10^{-6}$ 门槛）；
- **探针响应测试**：两轨的第一有效非零样点均位于 Sample Index 1，音频线程捕获的第一信号时间差理论值为 **0 采样点**，完全满足 `ExperimentPlayTests.cs` 中 `StartProbeDifferenceSamples < 64` 的测试断言。

---

## 三、V1 vs V2 客观离线测量对比表

数据基于 `audio_audit.py` 相同算法离线计算：

| 指标 | V1 Track A (原版) | V1 Track B (原版) | **V2 Track A (新修订)** | **V2 Track B (新修订)** |
|---|:---:|:---:|:---:|:---:|
| **采样率 / 位深** | 48,000 Hz / 24-bit | 48,000 Hz / 24-bit | **48,000 Hz / 24-bit** | **48,000 Hz / 24-bit** |
| **总采样数 / 时长** | 768,000 帧 / 16.0s | 768,000 帧 / 16.0s | **768,000 帧 / 16.0s** | **768,000 帧 / 16.0s** |
| **单轨样点峰值 (Sample Peak)** | -2.000 dBFS | -2.000 dBFS | **-6.000 dBFS** | **-6.000 dBFS** |
| **首尾样点阶跃 (End Jump)** | **0.470761** | **0.487485** | **0.000000** | **0.000000** |
| **导数连续性阶跃 (Deriv Jump)** | 0.454077 | 0.485234 | **0.005424** | **0.008160** |
| **混合峰值 (Gains 1.0/0.7, 未Trim)** | +1.234 dBFS (削波 220 样点) | - | **-3.929 dBFS (削波 0 样点)** | - |
| **混合峰值 (Gains 0.8/1.0, 未Trim)** | +1.522 dBFS (削波 1323 样点) | - | **-3.611 dBFS (削波 0 样点)** | - |
| **SHA256 哈希值** | `c60a39f1...` | `06cc20d4...` | **`a87c8b4e459d57ac512b3a0fc4cc04d7224dc13e82d79cccafa1280b86bb5a9b`** | **`2c96ed75a55bad60c396b6c217c2e25c5d5656c63ba7587959d9a499a398359f`** |

---

## 四、真实边界与未验证项声明（符合 AGENTS 规范）

1. **测试信号属性明确**：当前生成的 V2 信号与 V1 相同，均为带清晰标记的 **`TEST_ONLY` 技术测试信号**，用于验证多轨调度与混音机制，**不代表正式音乐制作或艺术审美验收**；
2. **正式 Stems 仍由用户提供**：未来正式配乐分轨（Stems）由用户或专业作曲人交付，本轮不向用户索要正式音乐，也不擅自以合成信号冒充正式乡村配乐；
3. **测量维度诚实界定**：
   - 离线计算证明了首尾樣点差与导数平滑性，但**未进行真实声卡硬件输出端口抓轨或真人高保真监听**；
  - Unity 主线程 `timeSamples` 具有音频驱动缓冲区（如 1024 采样点）量化特性，不能伪称为亚微秒级硬件测量；本次离线分析不替代V2在Unity中的真实起音/恢复测试；
   - `DspStartProbe` 探针仅对本测试信号包含已知非零起音的特征有效，不能推广至任意全静音前导的商业歌曲。
