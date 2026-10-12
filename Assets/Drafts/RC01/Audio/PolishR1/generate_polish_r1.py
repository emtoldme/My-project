"""
RC01 音效表现 R1 离线音色合成与处理脚本 (generate_polish_r1.py)
----------------------------------------------------------------
任务：feature/rc01-sound-polish-r1 (Docs/Integration/REVERB_TASKS_07.md)
岗位：音效总监 (Sound Director)
运行环境：Python 3.x + numpy + scipy + soundfile

本脚本遵循严格的物理与声学分层设计模型：
  每一段音效 = [瞬态 (Transient)] + [主体 (Body / Material Resonance)] + [尾部 (Tail / Dissipation)]
杜绝单一参数或纯 pitch 抖动变体，通过改变不同物理材质冲击、滤波器截止频率、共振腔体谐波结构与包络轮廓，
离线生成 8 组共 21 个全新纯质感短音变体（全部归一化至 -3.000 dBFS 真实采样峰值）。
"""

from pathlib import Path
import math, os
import numpy as np
import scipy.signal as signal
import soundfile as sf

SR = 48000
TARGET_LINEAR = 10.0 ** (-3.0 / 20.0)  # -3.000 dBFS

REPO_ROOT = Path(__file__).resolve().parents[5]
OUT_DIR = REPO_ROOT / "Assets/Drafts/RC01/Audio/PolishR1"
OUT_DIR.mkdir(parents=True, exist_ok=True)

def apply_fade(sig, fade_in_samples=48, fade_out_samples=192):
    """确保首尾绝对平滑零阶跃，防止边缘咔哒声"""
    s = sig.copy()
    if fade_in_samples > 0:
        fi = np.sin(0.5 * np.pi * np.linspace(0, 1, min(fade_in_samples, len(s)))) ** 2
        s[:len(fi)] *= fi
    if fade_out_samples > 0:
        fo = np.sin(0.5 * np.pi * np.linspace(1, 0, min(fade_out_samples, len(s)))) ** 2
        s[-len(fo):] *= fo
    return s

def normalize_peak(sig, peak_linear=TARGET_LINEAR):
    pk = np.max(np.abs(sig))
    if pk > 1e-7:
        return sig / pk * peak_linear
    return sig

def bandpass_noise(duration, lowcut, highcut, decay_rate=20.0, seed=42):
    np.random.seed(seed)
    n = int(SR * duration)
    noise = np.random.uniform(-1, 1, n)
    nyq = SR / 2.0
    low = max(20.0, min(nyq - 100.0, lowcut))
    high = max(low + 50.0, min(nyq - 50.0, highcut))
    b, a = signal.butter(2, [low / nyq, high / nyq], btype='band')
    fnoise = signal.lfilter(b, a, noise)
    t = np.linspace(0, duration, n, False)
    env = np.exp(-t * decay_rate)
    return fnoise * env

print(f"Generating 8 groups of PolishR1 audio variants into: {OUT_DIR}")

# =========================================================================
# 组 1: W01 定向脉冲 (3 变体: 65ms, 75ms, 85ms)
# 听觉定位: 短促、干脆、高频矿物电容放电与针刺物理穿透
# =========================================================================
# 变体 1: 1800Hz 尖锐陶瓷矿物瞬态 + 2200Hz 电容泄流
dur1 = 0.065
n1 = int(SR * dur1)
t1 = np.linspace(0, dur1, n1, False)
tr1 = np.sin(2 * np.pi * 3200.0 * t1) * np.exp(-t1 * 180.0) * 0.7  # 瞬态针刺
bd1 = np.sin(2 * np.pi * 1850.0 * t1) * np.exp(-t1 * 75.0) * 0.5   # 陶瓷主体
tl1 = bandpass_noise(dur1, 1500, 4500, decay_rate=90.0, seed=101) * 0.3 # 泄流高频细沙
w01_v1 = normalize_peak(apply_fade(tr1 + bd1 + tl1, 24, 96))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w01_pulse_v1.wav"), w01_v1, SR, subtype='PCM_24')

# 变体 2: 1550Hz 金属冲针瞬态 + 1680Hz 纯石英放电
dur2 = 0.075
n2 = int(SR * dur2)
t2 = np.linspace(0, dur2, n2, False)
tr2 = np.sin(2 * np.pi * 2600.0 * t2) * np.exp(-t2 * 140.0) * 0.65
bd2 = np.sin(2 * np.pi * 1550.0 * t2) * np.exp(-t2 * 60.0) * 0.55
tl2 = bandpass_noise(dur2, 1200, 3800, decay_rate=80.0, seed=102) * 0.28
w01_v2 = normalize_peak(apply_fade(tr2 + bd2 + tl2, 24, 96))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w01_pulse_v2.wav"), w01_v2, SR, subtype='PCM_24')

# 变体 3: 2100Hz 双重击锤瞬态 (10ms 预冲) + 2400Hz 微电弧释放
dur3 = 0.085
n3 = int(SR * dur3)
t3 = np.linspace(0, dur3, n3, False)
tr3 = (np.sin(2 * np.pi * 3600.0 * t3) * np.exp(-t3 * 160.0) +
       np.sin(2 * np.pi * 2200.0 * np.maximum(0, t3 - 0.008)) * np.exp(-np.maximum(0, t3 - 0.008) * 120.0)) * 0.5
bd3 = np.sin(2 * np.pi * 1720.0 * t3) * np.exp(-t3 * 50.0) * 0.5
tl3 = bandpass_noise(dur3, 2000, 5000, decay_rate=70.0, seed=103) * 0.25
w01_v3 = normalize_peak(apply_fade(tr3 + bd3 + tl3, 24, 96))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w01_pulse_v3.wav"), w01_v3, SR, subtype='PCM_24')

# =========================================================================
# 组 2: W02 扇形散射 (3 变体: 110ms, 125ms, 135ms)
# 听觉定位: 低中频重冲程、散弹气动泄压与开阔扇面推力
# =========================================================================
# 变体 1: 85Hz 重气缸冲程 + 450Hz 铁管喷射 + 麦秸扬尘摩擦
dur_w02_1 = 0.110
n_w02_1 = int(SR * dur_w02_1)
t_w02_1 = np.linspace(0, dur_w02_1, n_w02_1, False)
tr_w02_1 = np.sin(2 * np.pi * 1200.0 * t_w02_1) * np.exp(-t_w02_1 * 120.0) * 0.5
thump1 = np.sin(2 * np.pi * 85.0 * t_w02_1) * np.exp(-t_w02_1 * 32.0) * 0.7
bd_w02_1 = np.sin(2 * np.pi * 420.0 * t_w02_1) * np.exp(-t_w02_1 * 40.0) * 0.4
dust1 = bandpass_noise(dur_w02_1, 600, 2800, decay_rate=35.0, seed=201) * 0.35
w02_v1 = normalize_peak(apply_fade(tr_w02_1 + thump1 + bd_w02_1 + dust1, 48, 144))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w02_spread_v1.wav"), w02_v1, SR, subtype='PCM_24')

# 变体 2: 105Hz 双联发喷射气浪 + 520Hz 铸铁短管啸音
dur_w02_2 = 0.125
n_w02_2 = int(SR * dur_w02_2)
t_w02_2 = np.linspace(0, dur_w02_2, n_w02_2, False)
tr_w02_2 = np.sin(2 * np.pi * 1400.0 * t_w02_2) * np.exp(-t_w02_2 * 110.0) * 0.45
thump2 = (np.sin(2 * np.pi * 105.0 * t_w02_2) * np.exp(-t_w02_2 * 30.0) +
          np.sin(2 * np.pi * 70.0 * np.maximum(0, t_w02_2 - 0.012)) * np.exp(-np.maximum(0, t_w02_2 - 0.012) * 28.0)) * 0.45
bd_w02_2 = np.sin(2 * np.pi * 520.0 * t_w02_2) * np.exp(-t_w02_2 * 36.0) * 0.38
dust2 = bandpass_noise(dur_w02_2, 500, 2400, decay_rate=30.0, seed=202) * 0.38
w02_v2 = normalize_peak(apply_fade(tr_w02_2 + thump2 + bd_w02_2 + dust2, 48, 144))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w02_spread_v2.wav"), w02_v2, SR, subtype='PCM_24')

# 变体 3: 75Hz 重度下潜冲程 + 380Hz 铜质弹托分离击音
dur_w02_3 = 0.135
n_w02_3 = int(SR * dur_w02_3)
t_w02_3 = np.linspace(0, dur_w02_3, n_w02_3, False)
tr_w02_3 = np.sin(2 * np.pi * 980.0 * t_w02_3) * np.exp(-t_w02_3 * 90.0) * 0.5
thump3 = np.sin(2 * np.pi * 75.0 * t_w02_3) * np.exp(-t_w02_3 * 26.0) * 0.65
bd_w02_3 = np.sin(2 * np.pi * 380.0 * t_w02_3) * np.exp(-t_w02_3 * 34.0) * 0.42
dust3 = bandpass_noise(dur_w02_3, 400, 2000, decay_rate=28.0, seed=203) * 0.40
w02_v3 = normalize_peak(apply_fade(tr_w02_3 + thump3 + bd_w02_3 + dust3, 48, 144))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w02_spread_v3.wav"), w02_v3, SR, subtype='PCM_24')

# =========================================================================
# 组 3: W03 环绕护卫接触 (3 变体: 120ms, 135ms, 150ms)
# 听觉定位: 短促金属公转切削、微小摩擦接触与齿轮簧片震颤
# =========================================================================
# 变体 1: 440Hz + 880Hz 谐波金属公转打击
dur_w03_1 = 0.120
n_w03_1 = int(SR * dur_w03_1)
t_w03_1 = np.linspace(0, dur_w03_1, n_w03_1, False)
tr_w03_1 = np.sin(2 * np.pi * 2800.0 * t_w03_1) * np.exp(-t_w03_1 * 150.0) * 0.5
bd_w03_1 = (np.sin(2 * np.pi * 440.0 * t_w03_1) * 0.5 + np.sin(2 * np.pi * 880.0 * t_w03_1) * 0.3) * np.exp(-t_w03_1 * 35.0)
whir1 = bandpass_noise(dur_w03_1, 1200, 3200, decay_rate=45.0, seed=301) * 0.25
w03_v1 = normalize_peak(apply_fade(tr_w03_1 + bd_w03_1 + whir1, 32, 128))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w03_orbit_hit_v1.wav"), w03_v1, SR, subtype='PCM_24')

# 变体 2: 520Hz + 1040Hz 较脆的高硬度弹簧钢片刮擦
dur_w03_2 = 0.135
n_w03_2 = int(SR * dur_w03_2)
t_w03_2 = np.linspace(0, dur_w03_2, n_w03_2, False)
tr_w03_2 = np.sin(2 * np.pi * 3400.0 * t_w03_2) * np.exp(-t_w03_2 * 140.0) * 0.55
bd_w03_2 = (np.sin(2 * np.pi * 520.0 * t_w03_2) * 0.45 + np.sin(2 * np.pi * 1040.0 * t_w03_2) * 0.35) * np.exp(-t_w03_2 * 32.0)
whir2 = bandpass_noise(dur_w03_2, 1400, 3800, decay_rate=40.0, seed=302) * 0.28
w03_v2 = normalize_peak(apply_fade(tr_w03_2 + bd_w03_2 + whir2, 32, 128))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w03_orbit_hit_v2.wav"), w03_v2, SR, subtype='PCM_24')

# 变体 3: 380Hz + 760Hz 偏沉的铜质回旋导轨压碾
dur_w03_3 = 0.150
n_w03_3 = int(SR * dur_w03_3)
t_w03_3 = np.linspace(0, dur_w03_3, n_w03_3, False)
tr_w03_3 = np.sin(2 * np.pi * 2200.0 * t_w03_3) * np.exp(-t_w03_3 * 130.0) * 0.48
bd_w03_3 = (np.sin(2 * np.pi * 380.0 * t_w03_3) * 0.55 + np.sin(2 * np.pi * 760.0 * t_w03_3) * 0.28) * np.exp(-t_w03_3 * 30.0)
whir3 = bandpass_noise(dur_w03_3, 1000, 2600, decay_rate=38.0, seed=303) * 0.24
w03_v3 = normalize_peak(apply_fade(tr_w03_3 + bd_w03_3 + whir3, 32, 128))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w03_orbit_hit_v3.wav"), w03_v3, SR, subtype='PCM_24')

# =========================================================================
# 组 4: W04 共振落区 (3 变体: 150ms, 170ms, 185ms)
# 听觉定位: 延迟落区引爆、低频声学内爆扫频与地表挤压
# =========================================================================
# 变体 1: 620Hz -> 90Hz 线性快速下潜内爆
dur_w04_1 = 0.150
n_w04_1 = int(SR * dur_w04_1)
t_w04_1 = np.linspace(0, dur_w04_1, n_w04_1, False)
f_sweep1 = np.linspace(620.0, 90.0, n_w04_1)
sw1 = np.sin(2 * np.pi * np.cumsum(f_sweep1) / SR) * np.exp(-t_w04_1 * 22.0) * 0.65
sub1 = np.sin(2 * np.pi * 65.0 * t_w04_1) * np.exp(-t_w04_1 * 20.0) * 0.45
tl_w04_1 = bandpass_noise(dur_w04_1, 100, 600, decay_rate=25.0, seed=401) * 0.35
w04_v1 = normalize_peak(apply_fade(sw1 + sub1 + tl_w04_1, 48, 192))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w04_zone_drop_v1.wav"), w04_v1, SR, subtype='PCM_24')

# 变体 2: 740Hz -> 110Hz 较宽频的声波凹陷挤压
dur_w04_2 = 0.170
n_w04_2 = int(SR * dur_w04_2)
t_w04_2 = np.linspace(0, dur_w04_2, n_w04_2, False)
f_sweep2 = np.linspace(740.0, 110.0, n_w04_2)
sw2 = np.sin(2 * np.pi * np.cumsum(f_sweep2) / SR) * np.exp(-t_w04_2 * 20.0) * 0.62
sub2 = np.sin(2 * np.pi * 75.0 * t_w04_2) * np.exp(-t_w04_2 * 18.0) * 0.42
tl_w04_2 = bandpass_noise(dur_w04_2, 120, 750, decay_rate=22.0, seed=402) * 0.38
w04_v2 = normalize_peak(apply_fade(sw2 + sub2 + tl_w04_2, 48, 192))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w04_zone_drop_v2.wav"), w04_v2, SR, subtype='PCM_24')

# 变体 3: 520Hz -> 70Hz 极深沉的地下空洞地鸣
dur_w04_3 = 0.185
n_w04_3 = int(SR * dur_w04_3)
t_w04_3 = np.linspace(0, dur_w04_3, n_w04_3, False)
f_sweep3 = np.linspace(520.0, 70.0, n_w04_3)
sw3 = np.sin(2 * np.pi * np.cumsum(f_sweep3) / SR) * np.exp(-t_w04_3 * 18.0) * 0.70
sub3 = np.sin(2 * np.pi * 55.0 * t_w04_3) * np.exp(-t_w04_3 * 16.0) * 0.50
tl_w04_3 = bandpass_noise(dur_w04_3, 80, 500, decay_rate=20.0, seed=403) * 0.32
w04_v3 = normalize_peak(apply_fade(sw3 + sub3 + tl_w04_3, 48, 192))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_w04_zone_drop_v3.wav"), w04_v3, SR, subtype='PCM_24')

# =========================================================================
# 组 5: 回声实际命中 (Derived Echo Hit, 2 变体: 180ms, 200ms)
# 听觉定位: 原目标 0.5s 延迟二次受伤，空心空腔共鸣，预备期绝对静默
# =========================================================================
# 变体 1: 180Hz 矿物空腔谐振 + 1100Hz 低通箱体
dur_ech1 = 0.180
n_ech1 = int(SR * dur_ech1)
t_ech1 = np.linspace(0, dur_ech1, n_ech1, False)
tr_ech1 = np.sin(2 * np.pi * 850.0 * t_ech1) * np.exp(-t_ech1 * 100.0) * 0.4
bd_ech1 = (np.sin(2 * np.pi * 180.0 * t_ech1) * 0.65 + np.sin(2 * np.pi * 360.0 * t_ech1) * 0.30) * np.exp(-t_ech1 * 26.0)
cavity1 = bandpass_noise(dur_ech1, 150, 900, decay_rate=28.0, seed=501) * 0.30
echo_v1 = normalize_peak(apply_fade(tr_ech1 + bd_ech1 + cavity1, 48, 144))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_echo_hit_v1.wav"), echo_v1, SR, subtype='PCM_24')

# 变体 2: 210Hz 锈蚀铁皮空箱微共振 + 1300Hz 迟滞余音
dur_ech2 = 0.200
n_ech2 = int(SR * dur_ech2)
t_ech2 = np.linspace(0, dur_ech2, n_ech2, False)
tr_ech2 = np.sin(2 * np.pi * 920.0 * t_ech2) * np.exp(-t_ech2 * 90.0) * 0.38
bd_ech2 = (np.sin(2 * np.pi * 210.0 * t_ech2) * 0.60 + np.sin(2 * np.pi * 420.0 * t_ech2) * 0.32) * np.exp(-t_ech2 * 24.0)
cavity2 = bandpass_noise(dur_ech2, 180, 1050, decay_rate=25.0, seed=502) * 0.32
echo_v2 = normalize_peak(apply_fade(tr_ech2 + bd_ech2 + cavity2, 48, 144))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_echo_hit_v2.wav"), echo_v2, SR, subtype='PCM_24')

# =========================================================================
# 组 6: 传播实际命中 (Derived Propagation Hit, 2 变体: 95ms, 110ms)
# 听觉定位: 目标间 0.1s 折射跳跃，高频电弧微粒与扫频偏心位移
# =========================================================================
# 变体 1: 2600Hz -> 1300Hz 线性高频电弧折射滑音
dur_prp1 = 0.095
n_prp1 = int(SR * dur_prp1)
t_prp1 = np.linspace(0, dur_prp1, n_prp1, False)
f_prp1 = np.linspace(2600.0, 1300.0, n_prp1)
sw_prp1 = np.sin(2 * np.pi * np.cumsum(f_prp1) / SR) * np.exp(-t_prp1 * 45.0) * 0.6
arc1 = bandpass_noise(dur_prp1, 1600, 4800, decay_rate=60.0, seed=601) * 0.4
prop_v1 = normalize_peak(apply_fade(sw_prp1 + arc1, 24, 96))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_propagation_v1.wav"), prop_v1, SR, subtype='PCM_24')

# 变体 2: 2300Hz -> 1100Hz 双折射电火花碎片
dur_prp2 = 0.110
n_prp2 = int(SR * dur_prp2)
t_prp2 = np.linspace(0, dur_prp2, n_prp2, False)
f_prp2 = np.linspace(2300.0, 1100.0, n_prp2)
sw_prp2 = (np.sin(2 * np.pi * np.cumsum(f_prp2) / SR) * 0.5 +
           np.sin(2 * np.pi * np.cumsum(f_prp2 * 1.25) / SR) * np.maximum(0, t_prp2 - 0.015) * 20.0 * 0.3) * np.exp(-t_prp2 * 40.0)
arc2 = bandpass_noise(dur_prp2, 1400, 4200, decay_rate=50.0, seed=602) * 0.38
prop_v2 = normalize_peak(apply_fade(sw_prp2 + arc2, 24, 96))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_propagation_v2.wav"), prop_v2, SR, subtype='PCM_24')

# =========================================================================
# 组 7: 敌人死亡 (Enemy Death, 3 变体: 320ms, 360ms, 400ms)
# 听觉定位: 材料崩解破裂、次低频泄压与残骸落地
# =========================================================================
# 变体 1: 陶石脆裂 + 65Hz 泄压气浪 + 碎屑落地
dur_dth1 = 0.320
n_dth1 = int(SR * dur_dth1)
t_dth1 = np.linspace(0, dur_dth1, n_dth1, False)
crack1 = np.sin(2 * np.pi * 2200.0 * t_dth1) * np.exp(-t_dth1 * 80.0) * 0.5
sub_dth1 = np.sin(2 * np.pi * 65.0 * t_dth1) * np.exp(-t_dth1 * 18.0) * 0.6
debris1 = bandpass_noise(dur_dth1, 300, 2400, decay_rate=14.0, seed=701) * 0.4
death_v1 = normalize_peak(apply_fade(crack1 + sub_dth1 + debris1, 48, 256))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_enemy_death_v1.wav"), death_v1, SR, subtype='PCM_24')

# 变体 2: 锈铁外壳撕裂 + 80Hz 气动冲激 + 金属残片弹跳
dur_dth2 = 0.360
n_dth2 = int(SR * dur_dth2)
t_dth2 = np.linspace(0, dur_dth2, n_dth2, False)
crack2 = (np.sin(2 * np.pi * 1600.0 * t_dth2) * np.exp(-t_dth2 * 75.0) +
          np.sin(2 * np.pi * 3100.0 * np.maximum(0, t_dth2 - 0.02)) * np.exp(-np.maximum(0, t_dth2 - 0.02) * 60.0)) * 0.45
sub_dth2 = np.sin(2 * np.pi * 80.0 * t_dth2) * np.exp(-t_dth2 * 16.0) * 0.55
debris2 = bandpass_noise(dur_dth2, 400, 3200, decay_rate=12.0, seed=702) * 0.38
death_v2 = normalize_peak(apply_fade(crack2 + sub_dth2 + debris2, 48, 256))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_enemy_death_v2.wav"), death_v2, SR, subtype='PCM_24')

# 变体 3: 玻璃矿物粉碎 + 50Hz 沉重塌陷 + 细沙扬尘
dur_dth3 = 0.400
n_dth3 = int(SR * dur_dth3)
t_dth3 = np.linspace(0, dur_dth3, n_dth3, False)
crack3 = np.sin(2 * np.pi * 2800.0 * t_dth3) * np.exp(-t_dth3 * 90.0) * 0.52
sub_dth3 = np.sin(2 * np.pi * 50.0 * t_dth3) * np.exp(-t_dth3 * 14.0) * 0.65
debris3 = bandpass_noise(dur_dth3, 200, 1800, decay_rate=10.0, seed=703) * 0.42
death_v3 = normalize_peak(apply_fade(crack3 + sub_dth3 + debris3, 48, 256))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_enemy_death_v3.wav"), death_v3, SR, subtype='PCM_24')

# =========================================================================
# 组 8: 成长确认 (2 变体: 480ms, 540ms) 与 进化解锁 (1 变体: 1250ms)
# 听觉定位: 卡牌注入金属簧片闭合 / G大调大三和弦恢弘短促展开
# =========================================================================
# 成长确认 变体 1: 簧片物理闭合 (880Hz) + 正向能量上扬琶音 (523Hz -> 659Hz -> 784Hz)
dur_cg1 = 0.480
n_cg1 = int(SR * dur_cg1)
t_cg1 = np.linspace(0, dur_cg1, n_cg1, False)
click1 = np.sin(2 * np.pi * 880.0 * t_cg1) * np.exp(-t_cg1 * 60.0) * 0.6
arpeggio1 = (np.sin(2 * np.pi * 523.25 * t_cg1) * np.exp(-t_cg1 * 12.0) * 0.35 +
             np.sin(2 * np.pi * 659.25 * np.maximum(0, t_cg1 - 0.05)) * np.exp(-np.maximum(0, t_cg1 - 0.05) * 10.0) * 0.35 +
             np.sin(2 * np.pi * 783.99 * np.maximum(0, t_cg1 - 0.10)) * np.exp(-np.maximum(0, t_cg1 - 0.10) * 8.0) * 0.40)
shimmer1 = bandpass_noise(dur_cg1, 2000, 6000, decay_rate=14.0, seed=801) * 0.22
growth_v1 = normalize_peak(apply_fade(click1 + arpeggio1 + shimmer1, 32, 192))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_ui_growth_confirm_v1.wav"), growth_v1, SR, subtype='PCM_24')

# 成长确认 变体 2: 旋钮拨合 (1100Hz) + 温暖五度和弦展开 (440Hz -> 659Hz)
dur_cg2 = 0.540
n_cg2 = int(SR * dur_cg2)
t_cg2 = np.linspace(0, dur_cg2, n_cg2, False)
click2 = np.sin(2 * np.pi * 1100.0 * t_cg2) * np.exp(-t_cg2 * 55.0) * 0.55
chord2 = (np.sin(2 * np.pi * 440.0 * t_cg2) * np.exp(-t_cg2 * 10.0) * 0.40 +
          np.sin(2 * np.pi * 659.25 * np.maximum(0, t_cg2 - 0.04)) * np.exp(-np.maximum(0, t_cg2 - 0.04) * 8.0) * 0.45)
shimmer2 = bandpass_noise(dur_cg2, 1800, 5500, decay_rate=12.0, seed=802) * 0.20
growth_v2 = normalize_peak(apply_fade(click2 + chord2 + shimmer2, 32, 192))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_ui_growth_confirm_v2.wav"), growth_v2, SR, subtype='PCM_24')

# 进化解锁 (精英宝箱质变): G大调纯正四音和弦谐振 (392Hz, 494Hz, 588Hz, 784Hz) + 1.25s 尾部漫反射
dur_evo = 1.250
n_evo = int(SR * dur_evo)
t_evo = np.linspace(0, dur_evo, n_evo, False)
evo_impact = np.sin(2 * np.pi * 196.0 * t_evo) * np.exp(-t_evo * 15.0) * 0.50
evo_chord = (np.sin(2 * np.pi * 392.00 * t_evo) * 0.40 * np.exp(-t_evo * 5.5) +
             np.sin(2 * np.pi * 493.88 * np.maximum(0, t_evo - 0.04)) * 0.35 * np.exp(-np.maximum(0, t_evo - 0.04) * 5.0) +
             np.sin(2 * np.pi * 587.33 * np.maximum(0, t_evo - 0.08)) * 0.32 * np.exp(-np.maximum(0, t_evo - 0.08) * 4.5) +
             np.sin(2 * np.pi * 783.99 * np.maximum(0, t_evo - 0.12)) * 0.28 * np.exp(-np.maximum(0, t_evo - 0.12) * 4.0))
evo_shimmer = bandpass_noise(dur_evo, 1500, 6500, decay_rate=4.5, seed=803) * 0.25
evo_full = normalize_peak(apply_fade(evo_impact + evo_chord + evo_shimmer, 48, 512))
sf.write(str(OUT_DIR / "TEST_ONLY_sfx_evolution_flourish_v1.wav"), evo_full, SR, subtype='PCM_24')

print("All 22 audio variants generated successfully.")
