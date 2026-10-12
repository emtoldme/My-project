"""
M01-R1 TEST_ONLY 机制音效变体生成脚本 (generate_m01_cues.py)
-----------------------------------------------------------
任务：feature/m01-audio-cues (Docs/Integration/REVERB_TASKS_04.md)
岗位：音效总监 (Sound Director)
运行环境：Python 3.x + numpy + scipy + soundfile

声学设计目标：
1. 回声音效 (TEST_ONLY_sfx_echo_hit.wav)：
   - 目标：体现“原目标延迟后的第二下”（共振余波、空心敲击、低频余震）；
   - 源音频：Assets/Audio/SFX/sfx_enemy_hit_01.wav (48kHz/24-bit)；
   - 处理链：1200Hz 二阶低通滤波提取中低频体态 + 180Hz 衰减正弦共振环 + 指数衰减尾音；
   - 时长：0.20 秒（<= 0.3s），单声道，峰值精确归一化至 -3.0 dBFS。

2. 传播音效 (TEST_ONLY_sfx_propagation.wav)：
   - 目标：体现“从一个目标跳到另一个”（快速电弧掠过、高频方向跳跃、偏心脉冲）；
   - 源音频：Assets/Audio/S03/sfx_s03_shot.wav (48kHz/24-bit)；
   - 处理链：1500Hz 二阶高通瞬态提取 + 2400Hz->1200Hz 快速线性扫频短脉冲 (0.08s)；
   - 时长：0.11 秒（<= 0.3s），单声道，峰值精确归一化至 -3.0 dBFS。
"""

from pathlib import Path
import math, os
import numpy as np
import scipy.signal as signal
import soundfile as sf

SR = 48000
REPO_ROOT = Path(__file__).resolve().parents[4]
AUDIO_OUT_DIR = REPO_ROOT / "Assets/Experiments/Mechanism/Audio"

# 1. 生成 回声变体 (TEST_ONLY_sfx_echo_hit.wav)
src_hit_path = REPO_ROOT / "Assets/Audio/SFX/sfx_enemy_hit_01.wav"
src_hit, sr_hit = sf.read(str(src_hit_path))
if sr_hit != SR:
    raise ValueError(f"Sample rate mismatch: {sr_hit} != {SR}")

# 1200Hz 低通滤波：滤除尖锐的初始冲击高频，模拟声波穿透/共振后的钝化特征
b_lp, a_lp = signal.butter(2, 1200.0 / (SR / 2), btype='low')
lp_hit = signal.lfilter(b_lp, a_lp, src_hit)

# 180Hz 阻尼正弦衰减：提供“原目标二次共振”的空心金属/矿物腔体余响
dur_echo = 0.20
n_echo = int(SR * dur_echo)
t_echo = np.linspace(0, dur_echo, n_echo, False)
ring_echo = np.sin(2 * np.pi * 180.0 * t_echo) * np.exp(-t_echo * 28.0) * 0.35

echo_signal = np.zeros(n_echo, dtype=np.float64)
copy_len = min(len(lp_hit), n_echo)
echo_signal[:copy_len] += lp_hit[:copy_len] * 0.90
echo_signal += ring_echo

# 尾部施加 20ms 平滑汉宁淡出，防止截断咔哒声
fade_len = int(SR * 0.02)
echo_signal[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2

# 归一化至 -3.0 dBFS
pk_echo = np.max(np.abs(echo_signal))
TARGET_LINEAR = 10.0 ** (-3.0 / 20.0)
echo_norm = echo_signal / pk_echo * TARGET_LINEAR

echo_out_path = AUDIO_OUT_DIR / "TEST_ONLY_sfx_echo_hit.wav"
sf.write(str(echo_out_path), echo_norm, SR, subtype='PCM_24')

# 2. 生成 传播变体 (TEST_ONLY_sfx_propagation.wav)
src_shot_path = REPO_ROOT / "Assets/Audio/S03/sfx_s03_shot.wav"
src_shot, sr_shot = sf.read(str(src_shot_path))
if sr_shot != SR:
    raise ValueError(f"Sample rate mismatch: {sr_shot} != {SR}")

# 1500Hz 高通滤波：保留清脆高频起音瞬态
b_hp, a_hp = signal.butter(2, 1500.0 / (SR / 2), btype='high')
hp_shot = signal.lfilter(b_hp, a_hp, src_shot)

# 2400Hz -> 1200Hz 快速扫频滑音 (Chirp)，模拟从点 A 掠向点 B 的电弧/音波位移
dur_prop = 0.11
n_prop = int(SR * dur_prop)
t_prop = np.linspace(0, dur_prop, n_prop, False)
f_glide = np.linspace(2400.0, 1200.0, n_prop)
phase_chirp = 2 * np.pi * np.cumsum(f_glide) / SR
w_chirp = np.sin(np.pi * np.linspace(0, 1, n_prop)) ** 2
chirp = np.sin(phase_chirp) * w_chirp * 0.45

prop_signal = np.zeros(n_prop, dtype=np.float64)
copy_shot_len = min(len(hp_shot), n_prop)
prop_signal[:copy_shot_len] += hp_shot[:copy_shot_len] * 0.55
prop_signal += chirp

# 尾部 15ms 平滑淡出
fade_prop = int(SR * 0.015)
prop_signal[-fade_prop:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_prop)) ** 2

# 归一化至 -3.0 dBFS
pk_prop = np.max(np.abs(prop_signal))
prop_norm = prop_signal / pk_prop * TARGET_LINEAR

prop_out_path = AUDIO_OUT_DIR / "TEST_ONLY_sfx_propagation.wav"
sf.write(str(prop_out_path), prop_norm, SR, subtype='PCM_24')

print(f"Generated M01-R1 sound cues:")
print(f"  {echo_out_path.name}: dur={dur_echo:.3f}s, Peak={20*math.log10(np.max(np.abs(echo_norm))):.2f} dBFS")
print(f"  {prop_out_path.name}: dur={dur_prop:.3f}s, Peak={20*math.log10(np.max(np.abs(prop_norm))):.2f} dBFS")
