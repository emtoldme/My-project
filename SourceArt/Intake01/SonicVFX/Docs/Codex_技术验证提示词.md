# Codex 执行提示词 — 麦田里的混响 / 声波反馈 VFX S1

你现在承担 Unity 工程师和 TA 适配/验证职责。项目名《麦田里的混响》（Reverb in the Rye），**不是工业机甲游戏**。美术基调为 Rustpunk × 美国乡村怪谈 × Shoegaze × 迷幻声波；由项目负责人提供正式美术、音频、UI、模型、贴图素材。你不得擅自生成、替换或重设计正式美术或音乐，不得扩大任务范围。

本次只验证三个既定视觉反馈：
1. **发射**：短暖白枪口闪光、青蓝/蓝紫不规则脉冲环、沿射击方向的少量电波颗粒。
2. **命中**：暖白短闪光、紫红共振冲击环、少量金褐色麦秆和尘埃。
3. **死亡**：不稳定错位的青蓝/紫红脉冲环，较多麦秆碎屑与消散尘粒。

交付文件包括 `Shaders/ResonancePulseURP.shader`、`Shaders/StrawDustURP.shader`、`Scripts/SonicAttackVFX.cs`、`Editor/SonicVFXSetupEditor.cs`。先阅读 `Docs/README_导入与预览.md`，再对照真实工程 Unity/URP 版本运行。

工作步骤：

A. 检查真实 Unity 项目、版本、URP renderer、Active Input Handling、已有枪口/伤害/死亡事件。不要假设已有接口名，不存在就先建立隔离测试用 Adapter。

B. 导入所附文件。运行 `Tools > Reverb in the Rye > Setup Sonic VFX Preview` 创建两个材质与独立场景 emitter，记录所有 Shader 编译器错误和 Unity Console 输出。进入 Play Mode，使用 Inspector ContextMenu 逐项触发发射/命中/死亡，截取同一俯视相机下的对比截图。

C. 在不改动现有战斗规则的前提下，以既有事件连接 `PlayFire(worldMuzzlePosition, worldForward)`、`PlayHit(worldHitPosition)`、`PlayDeath(worldDeathPosition)`；防止死怪重复触发。VFX 不负责碰撞、扣血、击退、死亡状态与音频播放。

D. 用真实 3D 麦田场景测试：视野可读性、重叠渲染、角色遮挡、地面穿插、URP 透明材质、帧耗时、发射频率。进行编辑器内和 Development Build 验证。

E. 如果源码无法编译或表现失效，**优先做最小修复**，列明错误、改动文件和修复依据。不得简单声称已验证，也不得用 Python 绘制假截图代替真实 Unity 渲染。

提交成果：
- 原始与修改后的文件路径、Git commit hash；
- Unity 和 URP 实际版本；
- Shader 编译情况和 Console 截图；
- 发射/命中/死亡各一张真实 Unity Game View 截图；
- 测试事件连接图及文件/行号；
- Profiler 信息和 Development Build 结果；
- 未完成项和具体阻塞原因。

成功定义：一次开火触发一次发射，一次有效碰撞触发一次命中，死亡只触发一次死亡表现。三类 VFX 与伤害逻辑解耦、在俯视相机下清晰可读，且项目原有功能与正式素材未被破坏。
