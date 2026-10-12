# 《麦田里的混响》技术基线审查

> 历史记录：本轮四位作者已交付并整合。当前结果见Docs/Verification/Reverb-Experiments-01/REVIEW.md，后续只执行REVERB_TASKS_02.md，不重复领取此处旧任务。

2026-10-12。读取用户提供的总指令v1.0，并保留既有外部AI协作模式。检查基线main@959d541b59998b1652348ae1e297882ffcaabb4d；后续本轮改动仅为上下文、审查与任务文档。用户源指令存Docs/Project/REVERB_CONTEXT_V1.md。

## 结论

现有Unity工程适合开展隔离的小规模技术实验，无需重建框架或更换管线。已经能运行的是3分钟幸存者灰盒与技术表现接线；正式风格化乡村画面、动态分轨音乐、机制装备组合和环境叙事尚未成立。旧S2雕塑包视觉已被用户拒绝，不作为新方向的美术基准。

新艺术背景为Rustpunk × Rural Folk Horror × Psychedelic Audio。它覆盖之前抽象雕塑的正式制作路线。旧A-04/P-04/C-03/S-04任务撤回，停止按S2_VISUAL_REWORK.md开工；该文件保留反馈历史。正式模型/贴图/动画/UI及音乐/音效/人声由用户提供或指定。主美和音效总监继续承担专业规范、素材审查及技术协作，不自行决定正式作品或生成占位后宣称正式品质。

## 工程事实与复用范围

| 方向 | 实际证据 | 已有基础 / 缺项 |
|---|---|---|
| 引擎与渲染 | ProjectVersion.txt；Packages/manifest.json | Unity6000.4.3f1，URP17.4.0，InputSystem1.19.0，uGUI2.0.0；本轮不升级 |
| 主循环与输入 | GameLoop、GameInput、RunSimulation、RunState | 单一局内状态/时钟与输入权威；每局输入克隆、暂停/升级/终局/重开可复用 |
| 战斗与池 | Rules/Combat/CombatModule.cs；Views/Combat/CombatViews.cs | 纯C#规则、自动选敌、子弹/敌人/宝石、精英/方向推进/包围圈；表现按池/身份同步；未证明大敌群性能 |
| 成长 | UpgradeRules.cs、RunConfig.cs | XP与三选一，当前主要是数值/穿透升级；不是独立机制装备、局外配装或解锁系统 |
| 事件与VFX | PresentationEvent.cs；GameLoop.PresentationDispatched；VfxDirector | 单一只读表现事件，池化闪避/发射/命中等接线；规则不依赖粒子。无项目自有ShaderGraph或VFXGraph资产被此次文件搜索检出 |
| 模型与动画 | CombatViews、VfxDirector、S2MotionDriver | Prefab接入、静态多Mesh和单个SkinnedMesh残影路径已有；完整FBX/Animator/多SkinnedMesh/正式动画接入未形成已验证管线 |
| 画面 | Assets/Settings/PC_RPAsset.asset、PC_Renderer.asset；S2StyleLabSetup | PC管线要求深度/不透明纹理，Renderer含启用SSAO；存在基础材质和灯光。启用配置不等于接触/材料/画面质量达标；乡村资产、植被风动、湿面、黄昏/夜景均未验证 |
| 音频 | Views/Audio/AudioDirector.cs | AudioSource并发/限频/duck/交叉淡化/暂停生命周期；有MixerGroup接入口，未检出项目.mixer资产；该组件未实现PlayScheduled/dspTime调度，不是同步分轨音乐 |
| UI | RunHud、S2ChoiceHud；现有uGUI包 | 灰盒IMGUI可用；成套uGUI/TMP界面及中文字体打包仍待实际验证 |
| 环境叙事 | 本轮脚本搜索和现有场景 | 未发现专用Lore/环境状态触发系统；暂不为故事背景建大型剧情工具 |

现有正常输入保持WASD+自动攻击+Space闪避；新策划的“自动或半自动”不构成当前切换操作的授权。旧12分钟、固定武器/被动数量等计划留作历史，不再当作必须实现的最终内容范围。

## 本轮验证

本轮实际执行Unity6000.4.3f1 EditMode：117项通过、0失败，测试耗时约12.14秒（不含编辑器导入/编译）。真实结果见Docs/Verification/Reverb-Baseline/editmode.xml；运行基线959d541b59998b1652348ae1e297882ffcaabb4d，代码/资源未改。使用隔离整合目录，原用户Unity窗口未操作。退出日志有进程信息采集线程终止提示，不作为用例失败；结果以完整Test Runner XML为准。

此前源码6f9ed0f对应EditMode117通过、D3D11 PlayMode14通过；最终包d44a402仅补终局文案后重新构建/独立启动。旧包记录见Docs/Verification/S2-StyleLab。这些历史结果不代表新六方向或新美术已验证。

本轮未重新跑PlayMode、构建、Profiler、音频同步或真人试听；没有相关运行代码变化，不重复制造成本。后续实验只跑各自必要检查，中后期完备测试计划继续有效。

## 主要阻碍

1. **正式资源缺口与标准混淆。**旧雕塑/声音只是技术占位；缺乡村方向正式资产和用户分轨，不能靠占位扩量解决。先验证接入并写清资源要求。
2. **规则事件与表现事件边界。**PresentationDispatched在规则后分发且清空，不能让装备从此事件回调反写伤害，否则易重复/递归和破坏T0–T13顺序。组合实验在隔离纯规则层排队结算，再给表现只读事件。
3. **音乐同步尚无实现。**现有双AudioSource用于战斗/升级切换，不能当作同步stems；DSP暂停/恢复、采样率/长度、漂移与重开需专门实测。
4. **性能目标尚无证据。**池化和可用的4070 Laptop GPU环境不证明100/200敌人的CPU/GPU/GC或低配置表现；未来昂贵效果必须同时给开关和简化方案。

## 仅安排三个最小实验

| 优先级 / 实验 | 独立负责人 | 最小验证问题 | 通过的客观条件 |
|---|---|---|---|
| 1 / E-01 资源与声波表现通路 | 2号，主美给资源规格/审查 | 一个指定或标明临时的模型能否正确导入/绑定；一次实际Shot/Hit驱动Mesh声波与粒子；一个简单ShaderGraph能否兼容现有URP | 无丢材质/错误缩放，规则命中不变，暂停/回池/重开可恢复；记录实际镜头和效果开关的简单帧时间，不声称正式画面 |
| 2 / E-02 两模块组合 | 1号 | 在隔离规则实例中，延迟回声与一次传播是否能独立/组合且有明确行为差异 | 无模块/A/B/A+B可比较；身份失效不误伤；每目标/触发次数有限；相同seed可复现，暂停/重开清空队列，不接正式升级与局外装备 |
| 3 / E-03 双轨DSP同步 | 音效总监负责技术实验或交具体接口，Codex接管确实缺少的代码 | 两条同长度占位轨能否共用DSP启动点并独立混音；暂停/恢复/重开后是否重同步 | 记录调度时间与timeSamples差、容差、音频设备配置；循环至少10次和多次暂停/重开。无用户stems则用明确TEST_ONLY信号/静音，不宣称音乐质量或听感通过 |

E-01同时覆盖美术导入、渲染和VFX三个技术方向；E-02覆盖战斗成长，E-03覆盖音频。环境叙事排在这三项之后，再讨论一个不打断战斗的对象状态实验。首轮不同时摊开六个系统。

各实验放隔离目录/场景，不能覆盖S1/S2默认玩法、正式表现入口或共享工程配置。代码/资源集中由负责AI交GitHub；Codex处理共享接线、Unity运行、必要.meta与合并。完整转发任务见REVERB_TASKS_01.md。用户承担正式创作决策和必要体验验收。
