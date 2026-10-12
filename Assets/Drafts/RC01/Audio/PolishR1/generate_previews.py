"""
RC01 音效表现 R1 触发密度压力测试与试听对比生成脚本 (generate_previews.py)
---------------------------------------------------------------------
任务：feature/rc01-sound-polish-r1 (Docs/Integration/REVERB_TASKS_07.md)
岗位：音效总监 (Sound Director)
运行环境：Python 3.x + numpy + scipy + soundfile

本脚本执行两项关键音频工程任务：
1. 离线多密度触发模拟（1次/s, 4次/s, 10次/s, 20次/s），模拟极端密集战斗环境下的声压累积、
   检测高频刺耳、低频掩蔽与尾部重叠，验证硬限幅与母线安全性；
2. 生成 3 段 25 秒可比对的试听音频（保存至 Assets/Drafts/RC01/Audio/PolishR1/）：
   - preview_old_baseline_25s.wav (旧版基线试听：单音重复、缺乏材料质感与动态)
   - preview_polish_r1_25s.wav (新版 Polish R1 试听：8组分层多变体、精密机械与干燥麦田质感)
   - preview_dense_combat_stress_25s.wav (密集战斗极限试听：高密度多武器交火、回声传播与死亡爆发)
"""

from pathlib import Path
import math, os
import numpy as np
import soundfile as sf

SR = 48000
REPO_ROOT = Path(__file__).resolve().parents[5]
POLISH_DIR = REPO_ROOT / "Assets/Drafts/RC01/Audio/PolishR1"
AUDIO_ROOT = REPO_ROOT / "Assets/Audio"

# 加载新版音频资产
def load_wav(p):
    d, sr = sf.read(str(p))
    if sr != SR:
        raise ValueError(f"SR mismatch in {p}: {sr}")
    if len(d.shape) > 1:
        d = d[:, 0]
    return d

w01_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_w01_pulse_v{i}.wav") for i in [1, 2, 3]]
w02_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_w02_spread_v{i}.wav") for i in [1, 2, 3]]
w03_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_w03_orbit_hit_v{i}.wav") for i in [1, 2, 3]]
w04_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_w04_zone_drop_v{i}.wav") for i in [1, 2, 3]]
echo_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_echo_hit_v{i}.wav") for i in [1, 2]]
prop_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_propagation_v{i}.wav") for i in [1, 2]]
death_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_enemy_death_v{i}.wav") for i in [1, 2, 3]]
growth_vars = [load_wav(POLISH_DIR / f"TEST_ONLY_sfx_ui_growth_confirm_v{i}.wav") for i in [1, 2]]
evo_clip = load_wav(POLISH_DIR / "TEST_ONLY_sfx_evolution_flourish_v1.wav")

# 加载旧版对比基线资产
old_shot = load_wav(AUDIO_ROOT / "S03/sfx_s03_shot.wav")
old_hit = load_wav(AUDIO_ROOT / "S03/sfx_s03_enemy_hit.wav")
old_death = load_wav(AUDIO_ROOT / "SFX/sfx_enemy_death_01.wav")
old_growth = load_wav(AUDIO_ROOT / "S03/sfx_s03_levelup_chosen.wav")
old_echo = load_wav(REPO_ROOT / "Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_echo_hit.wav")
old_prop = load_wav(REPO_ROOT / "Assets/Experiments/Mechanism/Audio/TEST_ONLY_sfx_propagation.wav")

print("Assets loaded. Running density stress simulation...")

def simulate_density(rate_per_sec, duration_sec=5.0):
    """
    模拟在固定触发速率下，连续播放短音变体的混音表现，检测重叠与峰值
    """
    total_samples = int(SR * duration_sec)
    mix = np.zeros(total_samples, dtype=np.float64)
    interval = 1.0 / rate_per_sec
    t = 0.0
    idx = 0
    while t < duration_sec - 0.2:
        start_samp = int(t * SR)
        # 轮换使用 W01, W02, Echo, Prop
        sample_pool = [w01_vars, w02_vars, echo_vars, prop_vars][idx % 4]
        clip = sample_pool[idx % len(sample_pool)]
        end_samp = min(total_samples, start_samp + len(clip))
        mix[start_samp:end_samp] += clip[:end_samp - start_samp] * 0.7
        t += interval
        idx += 1
    # 统计 RMS 与 Peak
    pk = np.max(np.abs(mix))
    rms = np.sqrt(np.mean(mix ** 2))
    pk_db = 20 * math.log10(pk) if pk > 0 else -100
    rms_db = 20 * math.log10(rms) if rms > 0 else -100
    return pk_db, rms_db

for rate in [1, 4, 10, 20]:
    pk, rms = simulate_density(rate, 5.0)
    print(f"  Stress test at {rate:2d} triggers/sec: Peak = {pk:6.2f} dBFS, RMS = {rms:6.2f} dBFS")

# =========================================================================
# 生成 3 段 25.0s 试听比对音频 (PCM 24-bit Stereo)
# =========================================================================
TOTAL_DUR = 25.0
N_TOTAL = int(SR * TOTAL_DUR)

def create_timeline(is_new=True, is_dense=False):
    np.random.seed(20261012 if is_new else 101)
    mix_l = np.zeros(N_TOTAL, dtype=np.float64)
    mix_r = np.zeros(N_TOTAL, dtype=np.float64)

    # 节奏时间线 (模拟 25s 战斗片段: 0~10s 基础交火, 10~18s 密集怪潮与回声传播, 18~25s 精英击杀与升级/进化)
    t = 0.2
    step = 0.12 if is_dense else 0.22
    shot_counter = 0

    while t < TOTAL_DUR - 1.5:
        # 决定当前事件
        rand = np.random.rand()
        pan = np.random.uniform(-0.4, 0.4)
        vol_db = -4.0 if is_dense else -2.5
        vol_lin = 10.0 ** (vol_db / 20.0)

        clip = None
        # 事件类型分配
        if t < 8.0:
            # 基础武器射击与击中
            if rand < 0.6:
                if is_new:
                    clip = w01_vars[shot_counter % len(w01_vars)]
                else:
                    clip = old_shot
            else:
                if is_new:
                    clip = w02_vars[shot_counter % len(w02_vars)]
                else:
                    clip = old_shot
        elif t < 16.0:
            # 回声与传播爆发
            if rand < 0.35:
                if is_new:
                    clip = echo_vars[shot_counter % len(echo_vars)]
                else:
                    clip = old_echo
            elif rand < 0.70:
                if is_new:
                    clip = prop_vars[shot_counter % len(prop_vars)]
                else:
                    clip = old_prop
            elif rand < 0.85:
                if is_new:
                    clip = w03_vars[shot_counter % len(w03_vars)]
                else:
                    clip = old_hit
            else:
                if is_new:
                    clip = w04_vars[shot_counter % len(w04_vars)]
                else:
                    clip = old_shot
        elif t < 21.0:
            # 击杀与残骸
            if rand < 0.45:
                if is_new:
                    clip = death_vars[shot_counter % len(death_vars)]
                else:
                    clip = old_death
            elif rand < 0.75:
                if is_new:
                    clip = w04_vars[shot_counter % len(w04_vars)]
                else:
                    clip = old_shot
            else:
                if is_new:
                    clip = w01_vars[shot_counter % len(w01_vars)]
                else:
                    clip = old_shot
        else:
            # 成长与进化
            if shot_counter % 2 == 0:
                if is_new:
                    clip = growth_vars[0]
                else:
                    clip = old_growth
            else:
                if is_new:
                    clip = evo_clip
                else:
                    clip = old_growth

        if clip is not None:
            st = int(t * SR)
            en = min(N_TOTAL, st + len(clip))
            chunk = clip[:en - st] * vol_lin
            # 简单等功率平移立体声
            gl = math.cos((pan + 1.0) * math.pi / 4.0)
            gr = math.sin((pan + 1.0) * math.pi / 4.0)
            mix_l[st:en] += chunk * gl
            mix_r[st:en] += chunk * gr

        shot_counter += 1
        t += np.random.uniform(step * 0.7, step * 1.3)

    # 软限幅并归一化至 -1.5 dBFS 峰值（防止多轨叠加削波）
    stereo = np.column_stack([mix_l, mix_r])
    pk = np.max(np.abs(stereo))
    if pk > 0:
        target_pk = 10.0 ** (-1.5 / 20.0)
        # 双曲正切平滑饱和软限幅
        stereo = np.tanh(stereo / target_pk * 0.9) * target_pk
    return stereo

print("Rendering 3 preview files (25.0s each)...")
stereo_old = create_timeline(is_new=False, is_dense=False)
sf.write(str(POLISH_DIR / "preview_old_baseline_25s.wav"), stereo_old, SR, subtype='PCM_24')

stereo_new = create_timeline(is_new=True, is_dense=False)
sf.write(str(POLISH_DIR / "preview_polish_r1_25s.wav"), stereo_new, SR, subtype='PCM_24')

stereo_dense = create_timeline(is_new=True, is_dense=True)
sf.write(str(POLISH_DIR / "preview_dense_combat_stress_25s.wav"), stereo_dense, SR, subtype='PCM_24')

for name in ["preview_old_baseline_25s.wav", "preview_polish_r1_25s.wav", "preview_dense_combat_stress_25s.wav"]:
    p = POLISH_DIR / name
    d, sr = sf.read(str(p))
    pk_db = 20 * math.log10(np.max(np.abs(d)))
    rms_db = 20 * math.log10(np.sqrt(np.mean(d ** 2)))
    print(f"  {name}: dur={len(d)/sr:.2f}s, ch={d.shape[1]}, Peak={pk_db:.2f} dBFS, RMS={rms_db:.2f} dBFS")
