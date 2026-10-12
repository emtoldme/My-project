# A-R06 锈蚀质感 UI 原型 (ReverbHUD PR33) 艺术审查与视觉适配规范

**文档属性**：主美专项 UI/HUD 视觉规范与工程适配指南  
**审查对象**：GitHub 新推送分支 `origin/feature/ui-rustpunk-hud`（Commit `586cccf`，PR #33），包含 `Assets/UI/Scripts/ReverbHUD.cs` 与 `Assets/UI/README.md`  
**基准分支**：`review/rf01-ui-audit`（基于 `origin/main` @ `13be83d`）  
**执行责任人**：主美  
**协同对象**：Codex（工程管线与 GameLoop/M01 桥接）、2 号（战斗与环境场景装配）

---

## 一、审查总评与核心定性 (Executive Summary)

GitHub 新推送的分支 `feature/ui-rustpunk-hud` 提出了首个针对本作世界观的 **Rustpunk 锈蚀风格 HUD 界面原型（ReverbHUD）**。

主美团队从视觉风格、叙事色彩映射、战斗信息层级、抗背景干扰度及工程契约兼容性五个维度进行了系统审查，核心定性如下：
1. **风格方向高度认可，迈出了从“极简白盒调试文本”走向“Rustpunk 实体仪表”的关键一步**：
   - 提出了“深色锈蚀金属基底（`#3A2A20` ~ `#5A4030`）+ 暖白文字（`#F5E8D0`）+ 动态信号光槽”的设计构想，与刚入库的旧式调频器（`Old Sonic Modulator`）上的电木面板与老旧仪表形成了极好的世界观呼应。
2. **当前资产状态定性为“代码行为原型”，绝非正式定稿美术**：
   - 目前推送内容仅包含 1 个 C# 脚本与 1 篇 Markdown 说明，**尚未提交 Canvas 预制体、9-slice 锈铁切片贴图、独立字体资源（Font Asset）或实机渲染截图**；
   - 严禁将“纯色 Image 色块拼出的占位框”当作“已完成的高品质锈蚀质感界面”；当前阶段它作为功能与色彩契约验证原型。
3. **暴露出的三大核心视觉与语义冲突（必须立即适配纠偏）**：
   - **色彩语义与机制冲突**：代码中将模式指示灯设置为“Echo 为青色、Propagation 为品红色”，这与此前主美确立并在 M01-R1/RF01 中严格落地的**“回声 = 迷幻紫红（#B863A0 / #E024C3）、传播 = 亮金电弧（#FFD700）”**色彩规范直接对冲！必须立即统一，严禁一套游戏两套色彩系统；
   - **生命值渐变逻辑倒置**：血条渐变设置成“满血青色 → 残血品红”，但品红（Magenta）属于音波核心机制色，低血量时使用强品红会导致玩家误以为处于某种“声波暴走”或“充能就绪”状态；残血警告必须使用**朱红/血橙色（`#FF3B30`）**；
   - **工程越界隐患**：脚本内直接修改 `Time.timeScale` 且自持测试状态，必须由 Codex 剥离为只读真实游戏状态的观察者组件。

---

## 二、UI 视觉设计规范与色彩体系适配表 (Visual Hierarchy & Palette Alignment)

为了让 ReverbHUD 完美融入已入库的地面（Ground v0.2）、麦浪（Foliage v0.1）与调频器武器，主美重构并制定如下标准色卡与层级规范：

| UI 模块 / 元素 | 原型推送现状 (PR33) | 主美重构与适配标准 (Adapted Spec) | 设计意图与抗干扰考量 |
| :--- | :--- | :--- | :--- |
| **底框面板 (Frame)** | 纯色 Image (`#5A4030`, Alpha 0.95) | **深黑褐重度锈蚀电木底板**<br>色号：`#2A1D15`<br>边框：1px 氧化铜绿线 (`#4A6B5D`) | 极度深沉吸光，与秋季金黄麦田和暗土背景形成清晰轮廓分离，即使在强烈阳光下也能看清读数。 |
| **生命信号条 (Health Fill)** | 青色 `#00F0FF` → 品红 `#FF00AB` | **真空管钨丝充能渐变**：<br>• 满血/健康 (100%~50%): **电波冷青** (`#77D9D4`)<br>• 预警状态 (50%~25%): **枯草暖黄** (`#E5B558`)<br>• 濒死危险 (<25%): **朱红警示** (`#FF3B30` 闪烁) | 彻底剔除品红，将危险警示色独家归还给系统级危险（朱红）；青色代表广播信号稳定，黄色代表微弱，红色代表天线断裂濒死。 |
| **模式指示灯 (Mode Glow)** | Echo=青色, Propagation=品红, Combined=紫红 | **严格对齐机制四色规范**：<br>• **None (无机制)**: 暗灰白 (`#7A7A7A`)<br>• **Echo (单目标回声)**: **迷幻紫红** (`#B863A0`)<br>• **Propagation (击中传播)**: **亮金电弧** (`#FFD700`)<br>• **Combined (双机制并存)**: **紫金双色互绕 / 亮紫罗兰** (`#D946EF`) | 确保 HUD 左上角/右上角的模式指示灯颜色，与实机战场中敌人身上的光环和电弧颜色**100% 绝对一致**！玩家看 UI 颜色就能直觉联想到战场反馈。 |
| **文字显示 (Text / Numbers)** | 默认字体，白色 | **仪表盘风化暖白刻度**<br>色号：`#F5E8D0`<br>字体建议：粗壮机械无衬线或等宽数字（带微小暗褐色阴影 `#1A100A`） | 杜绝刺眼的 100% 纯白 `#FFFFFF`，风化暖白更具 1980 年代模拟收音机印刷刻度感。 |
| **暂停遮罩 (Pause Panel)** | 纯黑半透明 | **广播静默暗场** (`#120E0B`, Alpha 0.82)<br>中心提示：`#F5E8D0`，加暗红辅助文字 | 暂停时麦浪与音波全场冻结，UI 呈现电视/收音机“无信号蓝/黑屏静默”的孤立恐怖感。 |

---

## 三、抗背景干扰度与布局硬性约束 (Layout & Readability Rules)

在当前的实际游戏视界下（Orthographic Size 9.0，Pitch 55°，45° 侧逆光主光），HUD 的排布必须严格遵循以下避让与抗干扰规则：

1. **左上角与右上角安全锚定（Corner Anchoring）**：
   - **左上角 (Top-Left)**：放置 `HealthPanel`（生命信号槽）与 `ModePanel`（当前机制状态），尺寸建议为屏幕宽度的 20% 以内；
   - **绝对避开屏幕中央（Center Safety Zone）**：屏幕正中央半径 7m 区域为主角、敌人冲刺与回声向心环的核心战斗舞台，UI 严禁在屏幕中下方堆叠大面积常驻常显面板，避免遮挡敌人地面朱红预警圈。
2. **多层背景防穿透（Backdrop Contrast）**：
   - 麦田环境由大量细碎麦芒（Wheat Cluster）与法线起伏的干土组成，高频细节极多；
   - 若直接在麦草上悬浮文字，极易发生“字条与麦芒交错看不清”的现象；
   - **硬性约束**：所有文字与血条必须包裹在带不透明度（Alpha ≥ 0.90）的深色底框（Frame）内部，严禁裸露文字悬浮。

---

## 四、给 Codex 的工程适配与接线契约 (Architecture & Ingestion Contract)

PR33 的原始代码具有较强的主观测试性质，Codex 在将其接入正式工程时，必须进行以下**四项标准化改造**：

### 1. 剥离伪状态，转为纯只读表现层（Read-only View Component）
- **禁止** 在 `ReverbHUD` 内部自持 `currentHealth`、`modeIndex`、`modes[]` 等假状态；
- 改为监听正式的战斗与机制接口：
  ```csharp
  // 必须只接收真实数据驱动：
  public void Refresh(float currentHp, float maxHp, MechanismMode activeMode);
  ```
- 血量来源：唯一 `CombatModule` 中的主角当前生命值；
- 模式来源：`IMechanismPlaytestControls.CurrentMode`（None / Echo / Propagation / Combined）。

### 2. 严禁直接写入 `Time.timeScale` 与拦截 Esc 键
- PR33 原代码中写有 `Time.timeScale = show ? 0f : 1f;` 以及 `keyboard.escapeKey.wasPressedThisFrame`；
- 正式框架的暂停由 `GameLoop` 状态机权威驱动（使用内部 `RunPhase` 与冻结更新）；
- `ReverbHUD` 中的 `pausePanel` 只应响应 `GameLoop.OnPauseStateChanged(bool isPaused)` 的广播，绝对不得私自修改全局物理时间！

### 3. 模式切换按钮契约
- 若 HUD 上提供可点击的模式切换按钮，点击事件必须回调：
  `controller.RestartWithMode(targetMode);`
  调用既有的同 seed 关卡重开逻辑，而不能仅仅在 UI 上修改一个 Label 字符串。

### 4. 健壮性与零报错防护（Null & Zero Division Safety）
- 当 `maxHealth <= 0` 时，强制 fallback 为 1.0f，防止发生除零异常（`DivideByZeroException`）；
- 检查所有公开引用的空检查（`healthFill != null`, `modeText != null` 等），即使部分面板未绑定，也严禁抛出 NullReferenceException 阻塞游戏主循环。

---

## 五、后续验收标准与流程建议

1. **第一阶段（当前）**：
   - Codex 先按上述契约规范接入，将 ReverbHUD 转换为安全的纯观察者组件，并在 `Assets/UI/` 下建立干净的 Prefab，使其能在 M01/RF01 场景中正确跟随战斗扣血与模式切换；
2. **第二阶段（视觉打磨）**：
   - 待 Codex 跑通逻辑并截取真实游戏分辨率下的实机画面后，主美将基于实际画面评估文字大小、DPI 缩放与中文字体显示效果；
   - 后续将引入带磨损倒角的 9-slice 锈蚀铁皮切片与调频旋钮贴图，正式替换纯色占位色块，打造出真正沉浸、粗粝的废土无线电风格界面。
