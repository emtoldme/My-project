"""
RC01 TEST_ONLY 局内音效变体生成脚本 (generate_rc01_audio.py)
-----------------------------------------------------------
任务：feature/rc01-audio-draft (Docs/Integration/REVERB_TASKS_06.md)
岗位：音效总监 (Sound Director)
运行环境：Python 3.x + numpy + scipy + soundfile

本脚本负责离线生成 6 段明确标记 TEST_ONLY 的核心机制/UI短音变体（单段均 <= 0.3s）：
1. TEST_ONLY_sfx_shot_spread.wav (W02 扇形散射：宽阔声学中低频瞬态，0.12s)
2. TEST_ONLY_sfx_guardian_orbit.wav (W03 环绕护卫：金属公转共振脉冲，0.14s)
3. TEST_ONLY_sfx_resonance_drop.wav (W04 定点共振：低频内爆扫频冲击，0.16s)
4. TEST_ONLY_sfx_ui_reroll.wav (UI 重抽：三连齿轮棘轮机械拨动声，0.12s)
5. TEST_ONLY_sfx_ui_banish.wav (UI 放逐：带通滤波降调消解声，0.18s)
6. TEST_ONLY_sfx_evolution_unlocked.wav (精英进化宝箱：谐波大三和弦共振鸣响，0.26s)
"""

from pathlib import Path
import math, os
import numpy as np
import scipy.signal as signal
import soundfile as sf

SR = 48000
TARGET_LINEAR = 10.0 ** (-3.0 / 20.0)  # 严格峰值 -3.0 dBFS
REPO_ROOT = Path(__file__).resolve().parents[4]
AUDIO_OUT_DIR = REPO_ROOT / "Assets/Drafts/RC01/Audio"

# 1. W02: 扇形散射 (TEST_ONLY_sfx_shot_spread.wav)
# 基于 sfx_player_shoot_01 叠加 95Hz 气动散弹次低频冲程
s01_path = REPO_ROOT / "Assets/Audio/SFX/sfx_player_shoot_01.wav"
s01, _ = sf.read(str(s01_path))
dur_w02 = 0.12
n_w02 = int(SR * dur_w02)
t_w02 = np.linspace(0, dur_w02, n_w02, False)
b_lp, a_lp = signal.butter(2, 2800.0 / (SR / 2), btype='low')
s01_f = signal.lfilter(b_lp, a_lp, s01)

w02_sig = np.zeros(n_w02, dtype=np.float64)
copy_len = min(len(s01_f), n_w02)
w02_sig[:copy_len] += s01_f[:copy_len] * 0.8
thump = np.sin(2 * np.pi * 95.0 * t_w02) * np.exp(-t_w02 * 35.0) * 0.40
w02_sig += thump
fade_len = int(SR * 0.015)
w02_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
w02_norm = w02_sig / np.max(np.abs(w02_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_shot_spread.wav"), w02_norm, SR, subtype='PCM_24')

# 2. W03: 环绕护卫接触 (TEST_ONLY_sfx_guardian_orbit.wav)
# 440Hz + 880Hz 双音色金属环绕公转共振脉冲
dur_w03 = 0.14
n_w03 = int(SR * dur_w03)
t_w03 = np.linspace(0, dur_w03, n_w03, False)
orb_tone = (np.sin(2 * np.pi * 440.0 * t_w03) * 0.45 + np.sin(2 * np.pi * 880.0 * t_w03) * 0.25) * np.exp(-t_w03 * 22.0)
w03_sig = orb_tone
fade_len = int(SR * 0.02)
w03_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
w03_norm = w03_sig / np.max(np.abs(w03_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_guardian_orbit.wav"), w03_norm, SR, subtype='PCM_24')

# 3. W04: 定点共振内爆 (TEST_ONLY_sfx_resonance_drop.wav)
# 580Hz -> 110Hz 快速下潜扫频内爆
dur_w04 = 0.16
n_w04 = int(SR * dur_w04)
t_w04 = np.linspace(0, dur_w04, n_w04, False)
f_res = np.linspace(580.0, 110.0, n_w04)
res_sweep = np.sin(2 * np.pi * np.cumsum(f_res) / SR) * np.exp(-t_w04 * 18.0) * 0.60
w04_sig = res_sweep
fade_len = int(SR * 0.02)
w04_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
w04_norm = w04_sig / np.max(np.abs(w04_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_resonance_drop.wav"), w04_norm, SR, subtype='PCM_24')

# 4. UI: 重抽轮盘拨动 (TEST_ONLY_sfx_ui_reroll.wav)
# 机械齿轮 3200Hz 快速三连棘轮短拨动
dur_re = 0.12
n_re = int(SR * dur_re)
t_re = np.linspace(0, dur_re, n_re, False)
re_sig = np.zeros(n_re, dtype=np.float64)
for click_idx in [0, int(SR * 0.03), int(SR * 0.065)]:
    n_ck = int(SR * 0.025)
    tc = np.linspace(0, 0.025, n_ck, False)
    ck = np.sin(2 * np.pi * 3200.0 * tc) * np.exp(-tc * 160.0) * 0.50
    re_sig[click_idx:click_idx+n_ck] += ck
fade_len = int(SR * 0.01)
re_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
re_norm = re_sig / np.max(np.abs(re_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_ui_reroll.wav"), re_norm, SR, subtype='PCM_24')

# 5. UI: 放逐消解 (TEST_ONLY_sfx_ui_banish.wav)
# 带通滤波噪声衰减 + 1200Hz->300Hz 降调余响
dur_ba = 0.18
n_ba = int(SR * dur_ba)
t_ba = np.linspace(0, dur_ba, n_ba, False)
np.random.seed(20261012)
noise = np.random.uniform(-0.35, 0.35, n_ba)
b_bp, a_bp = signal.butter(2, [800.0 / (SR/2), 3400.0 / (SR/2)], btype='band')
fnoise = signal.lfilter(b_bp, a_bp, noise) * np.exp(-t_ba * 18.0)
f_tone = np.linspace(1200.0, 300.0, n_ba)
tone_drop = np.sin(2 * np.pi * np.cumsum(f_tone) / SR) * np.exp(-t_ba * 25.0) * 0.35
ba_sig = fnoise + tone_drop
fade_len = int(SR * 0.02)
ba_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
ba_norm = ba_sig / np.max(np.abs(ba_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_ui_banish.wav"), ba_norm, SR, subtype='PCM_24')

# 6. 精英宝箱 / 进化解锁 (TEST_ONLY_sfx_evolution_unlocked.wav)
# G大调大三和弦金属共振展开 (392Hz, 494Hz, 588Hz, 784Hz)
dur_ev = 0.26
n_ev = int(SR * dur_ev)
t_ev = np.linspace(0, dur_ev, n_ev, False)
ev_chord = (np.sin(2 * np.pi * 392.0 * t_ev) * 0.35 +
            np.sin(2 * np.pi * 494.0 * t_ev) * 0.30 +
            np.sin(2 * np.pi * 588.0 * t_ev) * 0.25 +
            np.sin(2 * np.pi * 784.0 * t_ev) * 0.20) * np.exp(-t_ev * 12.0)
ev_sig = ev_chord
fade_len = int(SR * 0.03)
ev_sig[-fade_len:] *= np.sin(0.5 * np.pi * np.linspace(1, 0, fade_len)) ** 2
ev_norm = ev_sig / np.max(np.abs(ev_sig)) * TARGET_LINEAR
sf.write(str(AUDIO_OUT_DIR / "TEST_ONLY_sfx_evolution_unlocked.wav"), ev_norm, SR, subtype='PCM_24')

print(f"Generated 6 TEST_ONLY RC01 sound variants in {AUDIO_OUT_DIR}:")
for f in [
    "TEST_ONLY_sfx_shot_spread.wav",
    "TEST_ONLY_sfx_guardian_orbit.wav",
    "TEST_ONLY_sfx_resonance_drop.wav",
    "TEST_ONLY_sfx_ui_reroll.wav",
    "TEST_ONLY_sfx_ui_banish.wav",
    "TEST_ONLY_sfx_evolution_unlocked.wav"
]:
    p = AUDIO_OUT_DIR / f
    d, sr = sf.read(str(p))
    print(f"  {f}: frames={len(d)}, sr={sr}, dur={len(d)/sr:.3f}s, Peak={20*math.log10(np.max(np.abs(d))):.2f} dBFS")
