# 《麦田里的混响》第一轮协作任务

> 历史记录：本轮四位作者已交付并整合。当前结果见Docs/Verification/Reverb-Experiments-01/REVIEW.md，后续只执行REVERB_TASKS_02.md，不重复领取此处旧任务。

2026-10-12。保留用户转发 → 各AI独立分支/目录交GitHub → Codex审查/本地接线与自动化 → 用户体验验收的模式。当前仅技术基线与三个小实验，不制作正式艺术或大型系统。先读AGENTS.md、Docs/Project/REVERB_CONTEXT_V1.md和REVERB_BASELINE_REVIEW.md。**撤回A-04/P-04/C-03/S-04旧雕塑返工卡**，若已开始，保留分支并回报，停止继续写入，不能删成果或自动合入。

统一要求：fetch最新origin/main，从独立目录开新分支，记录完整SHA；只写自己的目录，提交.meta、实际验证/缺项和跨目录需求；不能自行合并或改main，不能升级Package。共享Core/Input/Views/asmdef/渲染设置由Codex处理。初期只做必要验证。没有Unity如实说明并交可运行入口，不让用户补工具/导入/测试。每项只交一个小实验，不展开正式游戏系统。

## 转给1号：E-02 装备组合技术实验

~~~text
保持你负责规则层的分工，当前不是继续C-03或新增正式装备。读取Docs/Project/REVERB_CONTEXT_V1.md、Docs/Integration/REVERB_BASELINE_REVIEW.md与REVERB_TASKS_01.md。新分支experiment/e02-effect-combination，只写Assets/Experiments/Equipment/、Docs/Experiments/E02/，不要改现有Rules/Combat、RunSimulation、场景或共享程序集。

做隔离纯C#实验：固定几个带身份/生命状态的目标，一次直接命中；A为可配置延迟回声，B为单次传播；比较无模块/A/B/A+B，有次数/深度上限和确定性队列，不能递归无限增殖。暂定B让一次命中传播到另一个合法目标，A+B允许回声引发一次受限传播；这是技术夹具，不是最终装备设计。不要依赖只读PresentationDispatched回调反写正式战斗。

只提供最小配置和每局独立状态，明确生命周期/延迟/来源/目标/触发代号；暂停不推进，目标回池/代号变化不得误伤，重开清队列。少量高价值自动检查与可打印的结果序列即可，不造通用节点编辑器或几十装备。README给运行入口和未来适配正式规则的最小接口请求，Codex负责接线决定。提交分支/PR并声明实际执行环境与未验证项。
~~~

## 转给2号：E-01 资源、URP与声波通路

~~~text
保持你负责资源接入、场景和效果的分工；撤回P-04正式雕塑/庭院制作。读取新项目上下文和技术基线报告。新分支experiment/e01-asset-wave，只写Assets/Experiments/VisualPipeline/、Docs/Experiments/E01/；不要改Art/S2、Waffle/S2、共享Views、正式场景、Assets/Settings、Packages或ProjectSettings。

在独立实验场景复用现有GameLoop/CombatViews和唯一表现事件。验证一个现有/用户指定的模型导入与Prefab、URP材质、轴向/米制/节点；无新正式资源就标为TEST_ONLY使用现有模型，不自行生成“正式乡村美术”。一次真实Shot/EnemyHit驱动一个Mesh声波/Particle反馈，视觉不决定伤害。场景配置只能启用一套同类FX入口，避免现有VfxDirector叠加，需求交Codex。

做一个必要且简单的ShaderGraph通路（例如带Alpha裁切的声波环），记录URP兼容、透明/深度/过绘取舍；暂不做麦田、湿地反射和整关制作。独立材质/Volume/必要Renderer副本留在自己目录，不能切全局管线。能用现有配置就复用。动效只读规则时间，暂停/升级、回池/重开状态恢复。资产接入规格听主美本轮清单；她不并行改你的资源。

交实际场景或可重复生成工具、Unity运行/导入事实、同机位效果开关截图及一次简单CPU/GPU/GC观察（不能测就标未验证）；不要求全设备、压力或完整包。README列运行入口与共享程序集/相机Renderer需求。上传分支/PR供Codex导入与短运行。
~~~

## 转给主美：A-R01 资源规格与艺术审查

~~~text
保持主美角色，但撤回A-04雕塑正式资产任务。新项目方向以Docs/Project/REVERB_CONTEXT_V1.md为准：1988架空美国乡村、锈蚀农业机械、模拟广播与信号异常，Stylized PBR。正式资产由用户提供或指定；没有明确素材制作委托前，不开始制作或批量生成正式模型/贴图/UI。

新分支review/ar01-asset-brief，仅写Docs/Art/Reverb/。请给2号E-01一份精简接入清单：按实际斜俯视镜头需要的轮廓/尺度/层次、米制/轴向/节点、FBX/动画/Prefab交接、URP材质通道（粗糙度转Smoothness等）、贴图与字体/图标来源要求，以及第一批最少需要用户指定的模型/材质。区分必须和后续可选，不罗列全游戏资产需求。

用新黄/红/蓝/品红的叙事色彩给出战斗危险提示与敌我辨识的技术约束，不能因为叙事颜色就让危险与友方同色难辨。不擅自定最终配色，不把旧灰盒称为正式画面。审查E-01实际截图时分别写接入正确性与艺术不足，允许技术通过但艺术待制作。交文档/PR，涉及正式资产创作或采购由用户指定。
~~~

## 转给音效总监：E-03 同步混音技术实验

~~~text
保持音效总监角色，撤回S-04扩展式制作安排；正式音乐/音效/人声/stems由用户提供。读取新项目上下文与技术基线报告。新分支experiment/e03-dsp-sync，只写Assets/Experiments/AudioSync/、Docs/Experiments/E03/。不改现有AudioDirector、源音频、Mixer、正式场景或共享配置，不写正式配乐。

验证两条同采样率/长度的轨道共用DSP计划启动点，独立增益/混音，并能在暂停/恢复/重开时重新对齐。两轨是分层，不是战斗曲与升级曲交叉淡化。优先Unity AudioSource/AudioMixer，不新增FMOD或Package。没有用户stems可使用明确标记TEST_ONLY的短测试信号/静音，只证明调度与混音；记录是否可听检，静音不能证明无爆音。若你不能实现代码，交精确接口和测试夹具，明确未实现，由Codex接管缺项，不能只写通过报告。

做一个隔离实验入口，运行期间不同时启用正式AudioDirector，不改变游戏攻击时刻/命中规则。记录sampleRate、长度、DSP计划与实际采样位置差、设备/缓冲、容差和暂停策略；至少10次短循环、多次暂停/恢复与重开。漂移目标要带测量方法，不用一句“零延迟”。仅做必要小实验，不开发完整音乐状态机、自动配器或音游同步战斗。README提供复现、结果、缺项与用户未来分轨交付规格；上传分支/PR。
~~~

## Codex的范围

维护工程基线、任务边界和GitHub；审核实验是否确实隔离、规则/表现是否分离；补必要共享接口/程序集/.meta；在本地Unity复现可运行入口、执行相关短检查；只把验证成立且获授权的内容接入主线。用户此前已授权Codex管理GitHub和常规整合，这一授权继续有效，不擅自推main或合入未审查/未授权的艺术和大型系统。正式资源、玩法与阶段决策交用户。

E-01/E-02/E-03可以分工并行准备，最多三项；主美的规格审查不构成第四个制作实验。Lore/多关卡/完整装备/植被与正式音乐制作均未开工。任务没有自动发送给外部AI，需用户转发。
