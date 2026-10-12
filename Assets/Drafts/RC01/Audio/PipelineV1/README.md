# RC02 音频与音乐装配管线 README (Audio & Music Pipeline V1)

**任务标识**：Task RC02-Audio-Pipeline（`feature/rc02-audio-pipeline`）  
**维护岗位**：音效总监（Sound Director）  
**基线分支**：`origin/main` (`6447af8a96496b2c606de4e586b4ea7eaae8aaf9`)  
**交付分支**：`feature/rc02-audio-pipeline`  
**协作边界**：仅写入 `Assets/Drafts/RC01/Audio/PipelineV1/` 与 `Docs/Design/RC02/Audio/`；未修改共享 `AudioDirector.cs`、场景、规则代码、2号目录工具或旧音频资产。  
**素材状态**：全部复用已在 `origin/main` 校验入库的 22 段 PolishR1 短音变体及既有战斗/升级循环测试曲目。

---

## 一、管线定位与合同 V1 映射规则

本管线旨在落实 `Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md` 规范，实现**音频表现与代码逻辑的彻底解耦**。

### 1. 两级匹配与防双播原则
- 表现条目采用 `kind + eventType + contentId` 进行两级精确匹配；
- **精确 ID 优先**：例如当规则层派发 `W01` 的 `Shot` 事件时，优先匹配 `contentId: "W01"` 的专用短音；当遇到通用命中时，回退到 `contentId: "*"` 的通用音效；
- **唯一音频宿主**：所有导出的表现规则统一由既有 `AudioDirector` 在运行时消费，**严禁在 VFX 预制体内部或场景组件中新建常驻音频播放器**，彻底杜绝同一物理事件产生一击双响。

### 2. 空间声学与暂停保护
- **俯视视听优化**：继承已验证的斜俯视参数（`minDistance: 30.0m`, `maxDistance: 65.0m`），防止在相机高度 20m 下近战打击音被过早对数衰减吃掉；
- **暂停策略差异化**：
  - 常规击中与射击短音：`pausePolicy: "freeze"`（暂停时时间轴冻结，恢复继续，不补播）；
  - 死亡长尾音（300~400ms）：`pausePolicy: "stop"`（暂停时立即执行 Stop 切断，恢复不重新触发，杜绝死怪在暂停菜单里继续碎裂）。

---

## 二、三步流转操作说明 (导出 → 2号刷新 → Codex挂接)

```text
[音效总监] 
  修改/增补 audio-manifest.json / music-manifest.json 
  --> 运行 validate_pipeline_manifests.py 静态校验通过
        │
        ▼
[2号 装配负责人]
  Unity 菜单执行 Waffle -> RC02 -> Refresh Content Catalog
  --> 自动解析 JSON 并在 RC02_AssemblyLab / ScriptableObject 目录中生成资产引用
        │
        ▼
[Codex 整合]
  AudioDirector 桥接消费装配目录，绑定至权威 GameLoop / CombatModule 事件窗口
```

---

## 三、常见新增/替换操作所需修改文件表

| 操作需求 | 需要修改的文件 | 格式规范与说明 |
|---|---|---|
| **新增一种武器/机制短音** | `Assets/Drafts/RC01/Audio/PipelineV1/audio-manifest.json` | 1. 在 `entries` 数组中添加一项，填入唯一 `id`、对应 `contentId`（如 `W05`）与 `eventType`；<br>2. 填入 `assets` 路径（采样率 48kHz / 24-bit PCM 单声道 WAV，时长建议 50~200ms）；<br>3. 运行 `validate_pipeline_manifests.py` 校验合法性。 |
| **替换当前地图的战斗背景音乐** | `Assets/Drafts/RC01/Audio/PipelineV1/music-manifest.json` | 1. 找到对应 `profileId`（如 `harvest_road`）；<br>2. 修改 `assets` 中 `role: "combatLoop"` 的 WAV 文件路径；<br>3. 若已有确定速度，可补充 `bpm` 与 `beatsPerBar`（未知填 0，不承诺不存在的分轨对齐）。 |
| **调整某个事件的音量或空间衰减** | `Assets/Drafts/RC01/Audio/PipelineV1/audio-manifest.json` | 修改对应条目的 `parameters` 数组中 `gainDb`、`spatialBlend` 或 `minDistance`/`maxDistance`。 |

---

## 四、音乐档案与状态机设计 (Music Profiles)

在 `music-manifest.json` 中配置了四张地图的专属音乐档案：
1. **`harvest_road`（秋收之路）**：
   - 战斗曲目：`bgm_s03_mathrock_polymeter_24s_loop.wav`（数学摇滚 7/8+4/4 复节拍清音吉他点弦）；
   - 挂起曲目：`bgm_levelup_suspended_loop.wav`（升级三选一悬停低频嗡鸣）；
   - 衰减增益：`musicGainDb: -18.0 dB`，平滑淡入淡出 `1.2s`。
2. **`old_market`（旧农贸市场）**：
   - 战斗曲目：`bgm_combat_master_64s_seamless.wav`（中频金属节奏切分完整循环，64s 无缝）。
3. **`fourth_granary`（第四粮仓）**：
   - 战斗曲目：`bgm_s03_postrock_space_24s_loop.wav`（深沉后摇低频漫反射，24s）。
4. **`radio_wasteland`（无线电荒原）**：
   - 战斗曲目：`bgm_s03_postrock_space_24s_loop.wav`（宽域立体声电磁微光）。

> **正式音乐免责声明**：当前循环曲目均为用于验证管线流转与状态机淡出淡入的 **`TEST_ONLY` 技术测试信号**。正式数学摇滚/后摇商业品质配乐本轮严格后置，后续只需替换档案路径即可生效。

---

## 五、给 Codex 的跨目录最小接线请求 (最多 3 项)

1. **直接消费 `PipelineV1` 生成的表现清单**：
   - 2号工具刷新后，请求 Codex 在 `RC01IntegrationBatch` 或 `AssemblyLab` 装配脚本中，直接引用由 `audio-manifest.json` 与 `music-manifest.json` 衍生的配置，覆盖旧的硬编码音效查找；
2. **新武器 W05/W06 事件携带 contentId 标识**：
   - 当 1 号规则组件实现 W05 弹跳电弧与 W06 移动残响时，派发的 `EnemyHit` 或 `WeaponZone` 事件必须携带 `"W05"` / `"W06"` 字符串标识，以便音频管线准确命中专用跳跃音与风鸣音；
3. **保持已标定的 -5.5dB 经验宝石增益与 R1 距离衰减**：
   - 场景重复配置时，请继续保持经验宝石固定 `-5.5dB` 增益与 `minDistance: 30m / maxDistance: 65m` 听音参数，防止被旧的默认参数覆盖。
