# GP-D (AI-D)：危险可读性与玩法界面 D1 切片交付说明

**交付负责人**：AI-D（Gameplay Presentation & Danger Feedback）  
**交付分支**：`gameplay/gp-d-feedback`  
**对应需求规范**：[`Docs/Project/GameplayNext/AI_D_PRESENTATION.md`](../AI_D_PRESENTATION.md)  
**所有权目录**：`Assets/Game/Presentation/GameplayNext/`  
**切片状态**：**D1 已完成**（几何预警池、首领/目标/结算布局与隔离预览器）

---

## 一、 D1 切片完成内容与特性

依据 AI-D 任务卡与 CONTRACTS 要求，D1 切片实现了核心预警表现与信息面板，并提供了完全隔离的编辑器预览支持：

### 1. 三种共享几何危险预警器 (`DangerIndicatorPool.cs`)
* **圆形（Circle）**：
  * 用于远程落波者等范围打击；
  * 外轮廓 40 段平滑线框 + 动态内聚填充网格（随倒计时蓄力推进），微调 0.05m 高度避开麦田地面穿模。
* **胶囊体（Capsule）**：
  * 用于近战冲锋者蓄力直线冲锋；
  * 前后圆弧平滑衔接 + 冲锋中轴箭头指示器（明确指示冲锋朝向），矩形能量面随蓄力比例线性延伸。
* **圆环形（Annulus）**：
  * 用于首领全场声波震荡/扩散环；
  * 双同心 LineRenderer 边界（外圆 + 内圆）+ 环形面片填充网格，动态跟随内径外径扩散。
* **严格三态区分（绝不仅靠单一颜色）**：
  * **预警中（Telegraphing）**：橙红边界 + 呼吸扫描填充（4Hz 动态正弦脉冲波纹）+ 方向中轴指示；
  * **生效瞬间（Active）**：黄白高亮轮廓闪烁 + 饱和填充面；
  * **消散完毕（Resolved）**：灰白残响淡出并立即回收。
* **资源与性能控制**：
  * 严格遵守 **最大 12 个指示器** 的固定对象池预算；
  * 超出预算丢弃，回收时彻底清空 Mesh 和 LineRenderer 坐标，杜绝残留。

### 2. 玩法界面只读看板与安全布局 (`GameplayFeedbackView.cs`)
* **首领（Boss）血条与阶段看板**：
  * 布局位于顶部中央（`width/2 - 210, top: 92`），位于秋收时钟正下方；
  * 包含首领登场预警脉冲、首领名称、阶段编号、招式名称与实时血条数值；
  * 首领未登场或死亡时完全隐藏，无假数据。
* **地图调谐目标指示器**：
  * 左侧驻留面板（`x: 24, y: 140`），避开主角生命值条；
  * 包含调谐站名称、倒计时余量、蓄力进度条与奖励简述；
  * **屏幕边缘离屏导引（Off-screen indicator）**：当目标超出视野时，在屏幕边框显示指向性箭头与距离标签，引导玩家前往。
* **终局结算战果看板（Settlement View）**：
  * 展示真实胜负原因、存活总时长、总击杀数、角色最终等级、完成目标数与首领决战结果。
* **双分辨率与升级卡安全避让**：
  * 接入 `ImguiScale`，在 1280×720 和 1600×900 下自适应缩放；
  * 核心升级三选一卡片（中心大面板）出现或暂停时，**次要看板自动避让/隐藏，预警动画与倒计时严格冻结**。

### 3. 隔离预览器 (`GameplayFeedbackPreview.cs`)
* 允许在隔离的测试场景中通过 Inspector 滑条（`SimulatedProgress` 0.0~1.0）直观检验三种几何形状在各种角度下的线条粗细、填充透明度、首领血条缩放与结算弹窗；
* **严守边界**：绝不挂载到正式 `Main.unity` 场景。

---

## 二、 严格遵守的边界规则核查

| 规范约束项 | 遵守情况 | 验证说明 |
|---|:---:|---|
| 不扣血、不结算伤害、不发奖 | ✅ 100% 遵守 | 代码全部为只读描述符与视图渲染，无任何攻击伤害判定逻辑 |
| 沿用现有程序集，不新增 asmdef / 包 | ✅ 100% 遵守 | 源码直接归属 `Reverb.Drafts.RC01.Presentation`，零外部包引用 |
| 不修改 `Main.unity` 与正式场景 YAML | ✅ 100% 遵守 | `Main.unity` 保持原样未触碰，仅在新增独立目录产出 |
| 暂停 / 升级冻结倒计时与动画 | ✅ 100% 遵守 | `LateUpdate` 检测 `Phase == LevelUp \|\| Paused` 时立即 return 跳过渲染更新 |
| 对象池上限预算 | ✅ 100% 遵守 | 常量 `MaxDangerIndicators = 12` 强制截断，超额不实例化 |

---

## 三、 给 Codex 的 D2 接线适配建议清单

待 GP00 共同契约与 GP-A/B/C 逻辑在核心提交后，AI-D 将无缝提供以下接线：
1. **订阅冲锋者/落波者攻击**：将 GP-A 的 `HostileAttackIntent` 转为 `DangerAreaDescriptor` 喂入 `DangerIndicatorPool`；
2. **订阅调谐站快照**：将 GP-B 的 `ObjectiveSnapshot` 喂入 `GameplayFeedbackView.SetObjectiveSnapshot`；
3. **订阅首领实体**：将 GP-C 的首领 HP/Move 实时映射到 `GameplayFeedbackView.SetBossSnapshot`；
4. **终局结果呈现**：在 `Phase.IsTerminal()` 时读取 `RunOutcome` 弹出结算卡。
