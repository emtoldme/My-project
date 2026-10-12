"""
E-03 TEST_ONLY 测试信号 V2 生成脚本 (generate_e03_v2.py)
-------------------------------------------------------
任务：fix/e03-test-signal-loops
作者：音效总监 (Sound Director)
运行环境：Python 3.x + numpy；--output-dir 可输出到独立目录复现

设计目标：
1. 首尾样点归零；起音标记仍带来斜率变化，不等于 C1 连续或听感认证；
2. 单轨样点峰值约 -6.0 dBFS；叠加峰值由实际增益和审计结果确定；
3. 保留同步起音标记：在第 1 号采样点 (sample index 1) 注入严格对齐的高频瞬态 (value = 0.0054 / 0.0082 > 1e-6)，
   给 DspStartProbe 提供相同起音位置；实际捕获差需 Unity 运行测量；
4. 保持格式严格一致：48,000 Hz, 24-bit PCM WAV, 2 声道, 768,000 帧 (16.0 秒)。
"""

from pathlib import Path
import math, argparse, wave
import numpy as np

SR = 48000
DUR = 16.0
N = int(SR * DUR)  # 768,000 samples
t = np.linspace(0, DUR, N, False)

# 1. 周期性谐波基准 (在 16.0 秒内具有精确整数周期 K = f * 16.0)
# G1 = 49.00 Hz (K=784), G2 = 98.00 Hz (K=1568)
# G3 = 196.00 Hz (K=3136), D4 = 294.00 Hz (K=4704), B4 = 494.00 Hz (K=7904), D5 = 588.00 Hz (K=9408)

# Track A 原始层：次低频机械脉冲与节奏底床
sub_drone = (0.20 * np.sin(2 * np.pi * 49.00 * t) + 
             0.08 * np.sin(2 * np.pi * 98.00 * t))

# 4/4 拍底鼓脉冲 (在 0, 8, 16, 24 拍处，采用汉宁窗平滑包络)
kick_track = np.zeros(N, dtype=np.float64)
for b in [0, 8, 16, 24]:
    idx_b = b * 24000
    p_len = 7200
    tp = np.linspace(0, 0.15, p_len, False)
    w_kick = np.sin(np.pi * np.linspace(0, 1, p_len))**2
    fk = 65.0 * (1.0 + 1.2 * np.exp(-tp * 25.0))
    kick = np.sin(2 * np.pi * np.cumsum(fk) / SR) * w_kick * 0.22
    kick_track[idx_b:idx_b+p_len] += kick

# 8分音符轻质机械敲击
tick_track = np.zeros(N, dtype=np.float64)
tick_len = 480
w_tick = np.sin(np.pi * np.linspace(0, 1, tick_len))**2
tick_wave = np.sin(2 * np.pi * 2400.0 * np.linspace(0, 0.01, tick_len, False)) * w_tick * 0.05
for b8 in range(1, 64):
    idx_t = b8 * 12000
    if idx_t + tick_len <= N:
        tick_track[idx_t:idx_t+tick_len] += tick_wave

raw_a_l = sub_drone + kick_track + tick_track
raw_a_r = sub_drone + kick_track + tick_track * 0.85

# Track B 原始层：高频微光氛围与空间谐波
mod_b1 = 0.5 + 0.5 * np.cos(2 * np.pi * (4.0 / DUR) * t)
mod_b2 = 0.5 + 0.5 * np.sin(2 * np.pi * (2.0 / DUR) * t)

shimmer_l = (0.16 * np.sin(2 * np.pi * 196.00 * t) + 
             0.12 * np.sin(2 * np.pi * 294.00 * t) * mod_b1 + 
             0.08 * np.sin(2 * np.pi * 494.00 * t) * mod_b2 + 
             0.05 * np.sin(2 * np.pi * 588.00 * t))

shimmer_r = (0.16 * np.sin(2 * np.pi * 196.00 * t) + 
             0.12 * np.sin(2 * np.pi * 294.00 * t) * mod_b2 + 
             0.08 * np.sin(2 * np.pi * 494.00 * t) * mod_b1 + 
             0.05 * np.sin(2 * np.pi * 588.00 * t))

raw_b_l = shimmer_l
raw_b_r = shimmer_r

# 2. 循环切点平滑处理 (10ms / 480 samples 边界正弦平方平滑，确保 sample 0 与 sample N-1 绝对为 0.0)
fade_len = 480
w_in = np.sin(0.5 * np.pi * np.linspace(0, 1, fade_len))**2
w_out = np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len))**2

raw_a_l[:fade_len] *= w_in
raw_a_l[-fade_len:] *= w_out
raw_a_r[:fade_len] *= w_in
raw_a_r[-fade_len:] *= w_out

raw_b_l[:fade_len] *= w_in
raw_b_l[-fade_len:] *= w_out
raw_b_r[:fade_len] *= w_in
raw_b_r[-fade_len:] *= w_out

# 3. 注入精确同步的起音标记 (用于 DspStartProbe 音频线程检测)
# marker[0] 严格为 0.0，marker[1] 为 0.005 (> 1e-6 探针阈值)
m_len = 480
n_m = np.arange(m_len)
m_carrier = np.sin(2 * np.pi * 480.0 * n_m / SR)
m_fast = np.minimum(1.0, n_m / 8.0) * (1.0 - (n_m / m_len)**2)
marker = m_carrier * m_fast * 0.12
marker[0] = 0.0
marker[1] = 0.005
marker[2] = 0.010

raw_a_l[:m_len] += marker
raw_a_r[:m_len] += marker
raw_b_l[:m_len] += marker
raw_b_r[:m_len] += marker

# 4. 峰值归一化至 -6.0 dBFS (预留足额混音余量)
pk_a = max(np.max(np.abs(raw_a_l)), np.max(np.abs(raw_a_r)))
pk_b = max(np.max(np.abs(raw_b_l)), np.max(np.abs(raw_b_r)))
TARGET_LINEAR = 10.0 ** (-6.0 / 20.0)

track_a_l = raw_a_l / pk_a * TARGET_LINEAR
track_a_r = raw_a_r / pk_a * TARGET_LINEAR
track_b_l = raw_b_l / pk_b * TARGET_LINEAR
track_b_r = raw_b_r / pk_b * TARGET_LINEAR

data_a = np.column_stack((track_a_l, track_a_r))
data_b = np.column_stack((track_b_l, track_b_r))

# 5. 输出 48kHz 24-bit PCM WAV
parser = argparse.ArgumentParser()
parser.add_argument('--output-dir', type=Path, default=Path(__file__).resolve().parent)
out_dir = parser.parse_args().output_dir.resolve()
out_dir.mkdir(parents=True, exist_ok=True)
file_a = out_dir / 'TEST_ONLY_track_a_base_16s_v2.wav'
file_b = out_dir / 'TEST_ONLY_track_b_shimmer_16s_v2.wav'

def write_pcm24(path, data):
    # Portable PCM24 floor quantization; reproduction audit records any one-LSB library difference.
    pcm = np.floor(np.clip(data, -1.0, 1.0 - 1.0 / 8388608) * 8388608).astype(np.int32)
    packed = np.stack((pcm & 255, (pcm >> 8) & 255, (pcm >> 16) & 255), axis=-1).astype(np.uint8)
    with wave.open(str(path), 'wb') as output:
        output.setnchannels(2)
        output.setsampwidth(3)
        output.setframerate(SR)
        output.writeframes(packed.tobytes())

write_pcm24(file_a, data_a)
write_pcm24(file_b, data_b)

print(f"Generated V2 files in {out_dir}:")
print(f"  {file_a.name}: Peak={20*math.log10(np.max(np.abs(data_a))):.3f} dBFS, end_jump={np.abs(data_a[0]-data_a[-1]).max():.10f}")
print(f"  {file_b.name}: Peak={20*math.log10(np.max(np.abs(data_b))):.3f} dBFS, end_jump={np.abs(data_b[0]-data_b[-1]).max():.10f}")
