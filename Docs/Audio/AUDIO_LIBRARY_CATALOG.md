# 《麦田里的混响》高品质音频素材库总表 (Audio Library Catalog & Provenance)

> 生成时间：2026-10-13 | 岗位：音效总监 (Sound Director) | 存储路径：`Assets/ReverbInTheRye/AudioLibrary/`

本素材库整合了游戏开发全周期内经过严格筛选、多层物理建模与母带混音的高品质音频素材，涵盖**核心战斗、特色机制、UI/局内成长、掉落物、实物拟音采样、关卡音乐母带与动态分轨**。全部资产均为 100% 具备明确授权（CC0、CC-BY 3.0/4.0 或纯自研 DSP 物理建模合成），无版权商业纠纷风险。

---

## 一、素材库分类总览与统计

| 目录类别 | 文件数量 | 格式与规格 | 主要用途与声学特征 | 授权协议 |
|---|:---:|---|---|---|
| **SFX/CoreCombat** | 13 | 48kHz / 24-bit PCM Mono | 玩家发射、受击、敌人命中/死亡、闪避切风。陶瓷瞬态+真实矿物碰撞+75Hz次低频泄压。 | CC0 / CC-BY 3.0 / 自研物理建模 |
| **SFX/Mechanics** | 8 | 48kHz / 24-bit PCM Mono | 回声二次受震、电弧跳跃传播、共振落区、重抽/放逐及进化华彩。 | 自研 DSP 物理建模 (TEST_ONLY) |
| **SFX/Pickups** | 5 | 48kHz / 24-bit PCM Mono | 经验宝石拾取序列（C6-D6-E6-G6-A6纯净五度音阶），防听觉疲劳设计。 | CC0 (Judy TDC Metal) + 物理调律 |
| **SFX/UI_Growth** | 4 | 48kHz / 24-bit PCM Mono | 升级卡牌展开、确认锁定、关底胜利定格与失败解构长鸣。 | CC0 / CC-BY 4.0 / 自研和弦 |
| **SFX/Foley_Materials** | 8 | 44.1kHz / 16-bit PCM Stereo | 原始高保真拟音母版（碎瓶、木板、金属管、撞石、切风、薄铁片），供后续重构使用。 | CC0 (Judy TDC UCS) / CC-BY 3.0 |
| **Music/MasterLoops** | 3 | 48kHz / 24-bit PCM Stereo | 64s无缝战斗母带、16s升级悬浮长音、72s数学摇滚母带。 | CC0 (SRG774) + 剪辑母带处理 |
| **Music/Stems** | 6 | 48kHz / 24-bit PCM Stereo | 战斗音乐动态分轨（氛围层、律动低频层、张力危机层、旋律共振层），供混音器联动。 | CC0 (SRG774) |
| **Music/LevelDrafts** | 4 | 48kHz / 24-bit PCM Stereo | 四张地图设计草稿专属BGM（秋收之路、旧农贸市场、第四粮仓、无线电荒原）。 | CC0 (SRG774 / Hazmat Harry / iamoneabe) |
| **Demos** | 3 | 48kHz / 24-bit PCM Stereo | 60s全景实战战斗音频演示、60s全景纯音效视听对比母盘。 | 完整自研混音母带 |
| **合计** | **54** | **17.16 分钟无损音频** | **完整闭环声学体系** | **100% 授权透明** |

---

## 二、详细文件清单与声学参数

| 相对路径 | 时长 | 声道 | 采样率/深度 | 核心声学特征与授权来源 |
|---|:---:|:---:|:---:|---|
| `SFX/CoreCombat/sfx_player_shoot_01~03.wav` | 0.11s | 1ch | 48000Hz/PCM_24 | 陶瓷微裂瞬态 + 1450Hz 共鸣基频 + 物理撞击 (CC-BY 3.0 + CC0) |
| `SFX/CoreCombat/sfx_enemy_hit_01~03.wav` | 0.09s | 1ch | 48000Hz/PCM_24 | 真实矿物对撞击打瞬态 (Nicholas A. Judy TDC Rocks, CC0) |
| `SFX/CoreCombat/sfx_enemy_death_01~03.wav` | 0.42s | 1ch | 48000Hz/PCM_24 | 玻璃微裂 + 固体木骨架断裂 + 75Hz 次低频泄压 (CC-BY 3.0 + CC0) |
| `SFX/CoreCombat/sfx_player_hurt_01~02.wav` | 0.36s | 1ch | 48000Hz/PCM_24 | 62Hz/48Hz 纯物理次低频下潜 + 金属薄片应力失真 (CC0) |
| `SFX/CoreCombat/sfx_player_dodge_01~02.wav` | 0.26s | 1ch | 48000Hz/PCM_24 | 金属薄片高速划空气流切面 (TDC Swooshes, CC0) |
| `SFX/Mechanics/TEST_ONLY_sfx_echo_hit_v1~v2.wav` | 0.18s~0.20s | 1ch | 48000Hz/PCM_24 | 0.5s 延迟二次受震，180Hz 空心空腔共鸣，预备期严格静音 (自研DSP) |
| `SFX/Mechanics/TEST_ONLY_sfx_propagation_v1~v2.wav`| 0.10s~0.11s | 1ch | 48000Hz/PCM_24 | 0.1s 目标间折射跳跃，2600Hz->1300Hz 快速电弧滑音 (自研DSP) |
| `SFX/Mechanics/TEST_ONLY_sfx_resonance_drop.wav` | 0.16s | 1ch | 48000Hz/PCM_24 | 共振落区声波内爆快速下潜扫频 (自研DSP) |
| `SFX/Mechanics/TEST_ONLY_sfx_evolution_flourish_v1.wav`| 1.25s | 1ch | 48000Hz/PCM_24 | G大调大三和弦金属共振展开 + 宽阔后摇空间漫反射 (自研DSP) |
| `SFX/Mechanics/TEST_ONLY_sfx_ui_reroll.wav` | 0.12s | 1ch | 48000Hz/PCM_24 | 机械调谐旋钮拨动音 (自研DSP) |
| `SFX/Mechanics/TEST_ONLY_sfx_ui_banish.wav` | 0.18s | 1ch | 48000Hz/PCM_24 | 频率切断静电消散音 (自研DSP) |
| `SFX/Pickups/sfx_gem_pickup_01~05.wav` | 0.22s | 1ch | 48000Hz/PCM_24 | C6-D6-E6-G6-A6 五度音阶微晶碰击，连续吸收无疲劳感 (CC0) |
| `SFX/UI_Growth/sfx_ui_levelup_appear.wav` | 0.55s | 1ch | 48000Hz/PCM_24 | 空灵悬浮展开 (Spaceyyy Cozy Sampler, CC-BY 4.0) |
| `SFX/UI_Growth/sfx_ui_levelup_confirm.wav` | 0.75s | 1ch | 48000Hz/PCM_24 | D大调复合和弦与能量贯通，簧片闭合确认 (CC0) |
| `SFX/UI_Growth/sfx_game_victory.wav` | 3.20s | 1ch | 48000Hz/PCM_24 | 3.2s D大调纯净神圣共振和弦长鸣 (自研母带) |
| `SFX/UI_Growth/sfx_game_defeat.wav` | 2.80s | 1ch | 48000Hz/PCM_24 | 2.8s 崩解失压重浊轰鸣 (自研母带) |
| `SFX/Foley_Materials/Bottle_Break.wav` | 1.36s | 2ch | 44100Hz/PCM_16 | 原始碎瓶高频微裂母版 (spookymodem, CC-BY 3.0) |
| `SFX/Foley_Materials/board_drop_03.wav` | 0.91s | 2ch | 44100Hz/PCM_16 | 原始实木厚重撞击母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/metal_clang_01.wav` | 3.11s | 2ch | 44100Hz/PCM_16 | 原始金属管撞击延音母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/metal_thin_01.wav` | 2.14s | 2ch | 44100Hz/PCM_16 | 原始薄金属片微颤余音母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/stone_debris.wav` | 8.21s | 2ch | 44100Hz/PCM_16 | 原始真实矿物对撞与滑移母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/swish_big.wav` | 14.66s | 2ch | 44100Hz/PCM_16 | 原始切风快速划空母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/tin_drop.wav` | 17.94s | 2ch | 44100Hz/PCM_16 | 原始微机械微粒撞击母版 (Nicholas A. Judy, CC0) |
| `SFX/Foley_Materials/wood_break.wav` | 3.58s | 2ch | 44100Hz/PCM_16 | 原始小树枝断裂与结构崩解母版 (Nicholas A. Judy, CC0) |
| `Music/MasterLoops/bgm_combat_master_64s_seamless.wav`| 64.00s | 2ch | 48000Hz/PCM_24 | 主战斗无缝循环母带，融合脉冲推进与空旷氛围，1.2s等功率对数无缝淡化 (CC0) |
| `Music/MasterLoops/bgm_levelup_suspended_loop.wav`| 16.00s | 2ch | 48000Hz/PCM_24 | 升级时间冻结专属循环，抽离打击乐，保留纯净悬浮长音 (CC0) |
| `Music/MasterLoops/bgm_combat_loop_72s_master.wav`| 72.00s | 2ch | 48000Hz/PCM_24 | 完整72s数学摇滚/电子律动母带，120BPM 36小节 (CC0) |
| `Music/Stems/bgm_stem_ambient_airy.wav` | 64.00s | 2ch | 48000Hz/PCM_24 | 动态分轨：空灵浮动冷峻合成器空间层 (CC0) |
| `Music/Stems/bgm_stem_rhythm_pulse.wav` | 64.00s | 2ch | 48000Hz/PCM_24 | 动态分轨：沉稳强劲心跳式次低频脉冲律动层 (CC0) |
| `Music/Stems/bgm_stem_tension_urgent.wav` | 64.00s | 2ch | 48000Hz/PCM_24 | 动态分轨：高频张力与紧迫危机感层 (CC0) |
| `Music/Stems/bgm_stem_ambient.wav` | 72.00s | 2ch | 48000Hz/PCM_24 | 72s版动态分轨：深层底端张力与空旷残响 (CC0) |
| `Music/Stems/bgm_stem_resonance.wav` | 72.00s | 2ch | 48000Hz/PCM_24 | 72s版动态分轨：不规则多重节拍旋律拨动层 (CC0) |
| `Music/Stems/bgm_stem_rhythm.wav` | 72.00s | 2ch | 48000Hz/PCM_24 | 72s版动态分轨：重音低音脉冲与打击律动层 (CC0) |
| `Music/LevelDrafts/draft_bgm_harvest_road_64s.wav` | 64.00s | 2ch | 48000Hz/PCM_24 | 第一图（秋收之路）设计草稿背景音乐 (CC0) |
| `Music/LevelDrafts/draft_bgm_old_market_72s.wav` | 72.00s | 2ch | 48000Hz/PCM_24 | 第二图（旧农贸市场）工业金属管律动草稿音乐 (CC0) |
| `Music/LevelDrafts/draft_bgm_fourth_granary_64s.wav` | 64.00s | 2ch | 48000Hz/PCM_24 | 第三图（第四粮仓）极简战斗暗黑合成贝斯草稿音乐 (CC0) |
| `Music/LevelDrafts/draft_bgm_radio_wasteland_24s.wav`| 24.00s | 2ch | 48000Hz/PCM_24 | 第四图（无线电荒原）后摇电磁微光长音草稿音乐 (CC0) |
| `Demos/S1_Game_Audio_Demo_60s_v0.3.wav` | 60.00s | 2ch | 48000Hz/PCM_24 | 60s全景实战战斗音频演示（含开局试探、高密怪潮受击侧链、升级冻结与终局定格） |
| `Demos/S1_Master_Full_Mix_Demo_60s.wav` | 60.00s | 2ch | 48000Hz/PCM_24 | 60s完整母带演示（展示动态侧链Ducking与升级空间过渡） |
| `Demos/S1_SFX_Only_Evaluation_Demo_60s.wav` | 60.00s | 2ch | 48000Hz/PCM_24 | 60s纯音效版实战演示（闭眼直观审视材质碰撞、受击穿透力与并发动态） |

---

## 三、版权与溯源清单 (Third-Party Attribution & Licenses)

在游戏公开发布或演职员鸣谢（Credits）中保留以下署名声明：

```text
Audio Assets & Foley Credits:
- Bottle Break audio samples by spookymodem (OpenGameArt.org), licensed under CC-BY 3.0.
- Cozy SFX Sampler (warp, confirm) by Spaceyyy / Roberto Mellado (aquumgifts.itch.io), licensed under CC-BY 4.0.
- Foley and physical impacts from The Designer's Choice UCS Collection by Nicholas A. Judy, licensed under CC0 1.0 Universal (Public Domain).
- Ambient & Dark Sci-Fi music themes by SRG774 (OpenGameArt.org), licensed under CC0 1.0 Universal (Public Domain).
- Atmospheric industrial textures by Hazmat Harry & iamoneabe (OpenGameArt.org), licensed under CC0 1.0 Universal (Public Domain).
```

---

## 四、Unity 引擎接入与管线同步说明

1. **资产导入**：所有资产均已配套 Unity `.meta` 文件，并设置了标准的 `AudioImporter` 预设（48kHz 强制重采样、标准化、预加载）；
2. **表现管线映射**：`Assets/Game/Content/Manifests/audio-manifest.json` 与 `music-manifest.json` 可直接按需配置引用此库内的高保真资产；
3. **动态分轨支持**：`Music/Stems/` 目录中的分轨（`ambient_airy`、`rhythm_pulse`、`tension_urgent`）具备相同的 64.0s 长度与同步相位，可直接挂载在 `AudioMixerGroup` 动态调节音量。
