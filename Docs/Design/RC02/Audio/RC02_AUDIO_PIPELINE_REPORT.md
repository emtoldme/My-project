# RC02 音频与音乐管线设计与实装交付报告 (RC02 Audio Pipeline Specification)

**任务标识**：Task RC02-Audio-Pipeline（`feature/rc02-audio-pipeline`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`6447af8a96496b2c606de4e586b4ea7eaae8aaf9`)  
**交付分支**：`feature/rc02-audio-pipeline`  
**协作边界**：仅写入 `Assets/Drafts/RC01/Audio/PipelineV1/` 与 `Docs/Design/RC02/Audio/`；未修改共享 `AudioDirector.cs`、场景、规则、2号目录工具或旧音频素材。  
**素材状态**：复用已在 `origin/main` 校验合入的 22 段 PolishR1 短音变体及既有测试音乐循环，本轮未额外伪造未经校验的资产。

---

## 一、管线设计与合同 V1 落地概况

按照 `Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md` 与 `REVERB_TASKS_08.md` 的指引，本轮音效总监的核心使命是**建立一套完备、可自动化校验、可热替换的音频/音乐表现管线**：
1. **彻底解耦音效与硬编码代码**：所有音效映射、多变体轮转、增益微调、空间衰减与优先级全部收敛至 JSON 清单；
2. **两级匹配与防一击双响**：
   - 精确 ID 优先匹配：`W01`、`W02`、`W03`、`W04`、`W05`、`W06`、`P12`、`P13`、`P17`、`D01~D04` 等专属内容均有独立精确条目；
   - 通用回退：未指定专有音色的物理命中回退至 `contentId: "*"` 的通用三变体击中声，避免缺失音效；
   - **严禁私建播放器**：所有条目均由唯一的 `AudioDirector` 统一调度消费，禁止任何特效预制体或场景组件内联音频触发；
3. **已实装与未接线状态透明对账**：
   - 已实装并已测试：W01、W02、W03、W04、P12、P13、D01~D04、EnemyDeath、GrowthConfirm、Evolution；
   - 临时复用待规则接入：W05（复用 PolishR1 传播滑音 +1.15 Pitch）、W06（复用 S03 闪避风鸣 +1.25 Pitch）、P17（复用 PolishR1 脉冲 v3 +1.20 Pitch）；待 1 号组件和 Codex 实际接入后即刻发声，目前在清单中清晰标注，不伪称已实测。

---

## 二、8 组核心机制短音与新增组合映射对账表

| 条目 ID | 机制内容 / 事件类型 | 关联短音资产 (TEST_ONLY) | 变体数 | 增益 (`gainDb`) | 优先级 | 空间衰减 | 暂停策略 | 当前状态与接线说明 |
|---|---|---|:---:|:---:|:---:|:---:|:---:|---|
| `rc02.audio.w01.shot` | **W01 定向脉冲**<br>(`Shot`) | `TEST_ONLY_sfx_w01_pulse_v1~v3.wav` | 3 | -3.5 dB | 64 | 30m~65m | freeze | **已实装接线**：陶瓷石英电容针刺穿透，顺序轮换。 |
| `rc02.audio.w02.shot` | **W02 扇形散射**<br>(`Shot`) | `TEST_ONLY_sfx_w02_spread_v1~v3.wav` | 3 | -3.5 dB | 70 | 30m~65m | freeze | **已实装接线**：低中频重气缸气动冲程，开阔扇面推力。 |
| `rc02.audio.w03.hit` | **W03 环绕护卫**<br>(`EnemyHit`) | `TEST_ONLY_sfx_w03_orbit_hit_v1~v3.wav` | 3 | -4.0 dB | 80 | 30m~65m | freeze | **已实装接线**：金属公转切削碰撞脉冲，多材质谐波共振。 |
| `rc02.audio.w04.zone` | **W04 共振落区**<br>(`WeaponZone`) | `TEST_ONLY_sfx_w04_zone_drop_v1~v3.wav` | 3 | -3.0 dB | 72 | 30m~65m | freeze | **已实装接线**：延迟落区引爆，下潜声学内爆扫频。 |
| `rc02.audio.w05.chain` | **W05 弹跳电弧**<br>(`EnemyHit`) | `TEST_ONLY_sfx_propagation_v1~v2.wav` | 2 | -4.0 dB | 85 | 30m~65m | freeze | **映射就绪 (待规则接入)**：复用传播滑音抬高音高，待 1 号组件派发。 |
| `rc02.audio.w06.residue` | **W06 移动残响**<br>(`WeaponZone`) | `Assets/Audio/S03/sfx_s03_dodge.wav` | 1 | -4.5 dB | 90 | 30m~65m | freeze | **映射就绪 (待规则接入)**：复用闪避风鸣抬升音高，待 1 号残留区引爆。 |
| `rc02.audio.p12.echo` | **P12 回声命中**<br>(`EchoHit`) | `TEST_ONLY_sfx_echo_hit_v1~v2.wav` | 2 | -3.5 dB | 75 | 30m~65m | freeze | **已实装接线**：0.5s 延迟二次受伤，空心空腔共鸣，**预备期严格静默**。 |
| `rc02.audio.p13.propagation`| **P13 传播命中**<br>(`PropagationHit`) | `TEST_ONLY_sfx_propagation_v1~v2.wav` | 2 | -4.5 dB | 85 | 30m~65m | freeze | **已实装接线**：0.1s 目标间折射跳跃，高频电弧微粒。 |
| `rc02.audio.p17.dodge_discharge`| **P17 闪避放电**<br>(`Dodge`) | `TEST_ONLY_sfx_w01_pulse_v3.wav` | 1 | -3.5 dB | 68 | 30m~65m | freeze | **映射就绪 (待规则接入)**：闪避真正发动时触发高频电弧短脉冲。 |
| `rc02.audio.generic.enemy_death`| **敌人死亡**<br>(`EnemyKilled`) | `TEST_ONLY_sfx_enemy_death_v1~v3.wav` | 3 | -2.5 dB | 50 | 30m~65m | **stop** | **已实装接线**：材料脆裂与次低频泄压，**暂停时立即 Stop 切断尾音**。 |
| `rc02.audio.ui.growth_confirm`| **成长确认**<br>(`LevelUpChosen`) | `TEST_ONLY_sfx_ui_growth_confirm_v1~v2.wav` | 2 | -1.5 dB | 20 | 30m~65m | freeze | **已实装接线**：机械簧片闭合与正向能量上扬琶音。 |
| `rc02.audio.ui.evolution`| **进化解锁**<br>(`Evolved`) | `TEST_ONLY_sfx_evolution_flourish_v1.wav` | 1 | -2.0 dB | 10 | 30m~65m | freeze | **已实装接线**：G大调大三和弦金属共振展开（1.25s 宽阔后摇）。 |
| `rc02.audio.pickup.gem` | **经验宝石**<br>(`GemPickup`) | `Assets/Audio/SFX/sfx_gem_pickup_01~05.wav` | 5 | -5.5 dB | 100 | 30m~65m | freeze | **已实装接线**：已校准的固定 -5.5dB 增益，五度上升轮转。 |

---

## 三、四图音乐档案配置表 (Music Profiles Manifest)

持久化于 `Assets/Drafts/RC01/Audio/PipelineV1/music-manifest.json`：

| 档案 ID (`profileId`) | 对应关卡 | 战斗循环曲目 (`combatLoop`) | 升级挂起曲目 (`levelUpLoop`) | 增益 (`musicGainDb`) | 淡入淡出 | 拍点规范 |
|---|---|---|---|:---:|:---:|:---:|
| **`harvest_road`** | 秋收之路 | `Assets/Audio/S03/bgm_s03_mathrock_polymeter_24s_loop.wav` | `Assets/Audio/Music/bgm_levelup_suspended_loop.wav` | -18.0 dB | 1.2s | bpm: 0 (未对齐)<br>beatsPerBar: 0 |
| **`old_market`** | 旧农贸市场 | `Assets/Audio/Music/bgm_combat_master_64s_seamless.wav` | `Assets/Audio/Music/bgm_levelup_suspended_loop.wav` | -18.0 dB | 1.0s | bpm: 0 (未对齐)<br>beatsPerBar: 0 |
| **`fourth_granary`** | 第四粮仓 | `Assets/Audio/S03/bgm_s03_postrock_space_24s_loop.wav` | `Assets/Audio/Music/bgm_levelup_suspended_loop.wav` | -18.0 dB | 1.5s | bpm: 0 (未对齐)<br>beatsPerBar: 0 |
| **`radio_wasteland`** | 无线电荒原 | `Assets/Audio/S03/bgm_s03_postrock_space_24s_loop.wav` | `Assets/Audio/Music/bgm_levelup_suspended_loop.wav` | -18.0 dB | 1.5s | bpm: 0 (未对齐)<br>beatsPerBar: 0 |

> **拍点与分轨真实性声明**：当前所有曲目均为整段无缝循环，**未引入假动态分轨（stems 留空），BPM 与拍号实事求是填 0**，禁止在未实现精确 DSP 拍点同步的情况下做虚假承诺。未来当作曲家交付正式分轨时，仅需在对应 profile 下补充分轨并填入真实 BPM 即可无缝切换。

---

## 四、自检工具与静态验证事实 (Validation Results)

交付包含原生自检工具 `Assets/Drafts/RC01/Audio/PipelineV1/validate_pipeline_manifests.py`：
- **物理文件存在性**：清单中引用的 22 个 PolishR1 短音文件、SFX 辅助文件及全部 BGM 循环文件均 100% 存在，路径无拼写错误；
- **枚举与 ID 唯一性**：19 项音频条目与 4 项音乐条目 ID 全局唯一，`eventType` 全部严格匹配权威 `PresentationEventType`；
- **参数范围与声学安全**：音量增益均在合理安全区间（-1.5dB ~ -18.0dB），`minDistance` (30m) 与 `maxDistance` (65m) 严格倒挂保护；
- **自检结果**：
  ```text
  --- Validating audio-manifest.json ---
  PASSED with 19 valid entries.
  --- Validating music-manifest.json ---
  PASSED with 4 valid entries.
  ALL PIPELINE MANIFESTS VERIFIED COMPLIANT WITH CONTRACT V1.
  ```

---

## 五、给 Codex 的跨目录最小接线请求 (最多 3 项)

1. **统一装配桥读取表现清单**：
   - 请求在 2 号生成 ScriptableObject 目录后，Codex 的 `ConfigureAudio` 优先从表现清单注入 `contentBindings`，替代分散的旧逻辑硬编码；
2. **新规则事件携带 contentId 标识**：
   - 当 1 号组件派发 W05（弹跳电弧）、W06（移动残响）、P17（闪避放电）时，确保事件携带正确 `contentId`，让管线自动命中专有短音；
3. **保持已校准的 -5.5dB 经验宝石增益与 R1 距离衰减**：
   - 场景重复配置时，请继续保持经验宝石固定 `-5.5dB` 增益与 `minDistance: 30m / maxDistance: 65m` 听音参数，防止被旧的默认参数覆盖。
