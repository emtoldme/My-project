# 《麦田里的混响》：开发协作约定

更新：2026-10-12。长期源指令见Docs/Project/REVERB_CONTEXT_V1.md。M01实测见Docs/Verification/M01/REVIEW.md，反馈与显示修复见Docs/Verification/M01-Feedback/REVIEW.md。用户肯定组合乐趣/改变玩法、闪避可保留；声波作用不明显，全屏已修复。用户现已提交首批美术素材，资产清单与实际验证以Docs/Verification/Art-Intake-01/REVIEW.md为准。TASKS_04/05四位交付已收齐，由Codex在integration/rf01-playtest集中整合；本轮验收见Docs/Integration/RF01_ACCEPTANCE.md；已交任务见Docs/Integration/REVERB_TASKS_05.md，TASKS_01/02/03为历史。保持原四位外部AI分担负载，用户转发需求，各AI独立目录/分支交GitHub，Codex审查、本地接线和自动化，再交用户体验验收。

## 当前范围

继续独立“回声＋传播”机制试玩，同时接入用户已提供的地面、植被、调频器与声波VFX候选素材。不自行制作或采购正式素材；“用户提交正式素材”代表来源与使用意图，包内prototyping_only/TEST_ONLY和角色WIP标记仍如实保留，最终品质须用户验收。复用Unity6000.4.3f1/URP17.4.0及已有实验，不扩完整关卡/大框架/存档/装备编辑器，不升级依赖。新方向仍为乡村怪谈/锈蚀机械/迷幻音波，S2首版视觉被拒绝，旧雕塑返工任务撤回。

M01-R1只改机制反馈：回声预备→原目标二次受伤→传播去向与实际结果。回声不是AOE，不靠扩大地面圈暗示范围。保留现有数值/延迟/距离/闪避；组合值得深化已获用户肯定，不重复要求方向确认。Codex修全屏，四位按TASKS_04分担反馈工作；未交付不得宣称新动效/音色已完成。

M01独立场景比较None/Echo/Propagation/Combined，切换即同seed重开；保留WASD/自动攻击/Space闪避/Esc暂停。默认S1/S2不启用新机制。纯规则只由唯一CombatModule应用HP、死亡、掉落；只读PresentationDispatched不能反写伤害。最小可选规则钩子与共享程序集由Codex收口。

## 唯一写入范围

| 执行者 | 任务 | 可写范围 |
|---|---|---|
| 1号 | TASKS_04规则交付冻结；审查新视效取消/代号/事件边界 | TASKS_05只写Docs/Experiments/M01/ART_INTAKE_RULE_GUARDS.md；规则修复须Codex明确派回 |
| 2号 | TASKS_04表现交付冻结；独立麦田环境装配 | Assets/Experiments/RyeField/、Docs/Experiments/RF01/；不改M01/用户原始素材 |
| 主美 | 首批资源与RF01实际镜头审查 | 仅Docs/Art/Reverb/；不得把概念图/源码检查写成实机通过 |
| 音效总监 | 已交短音冻结；新武器/声波事件声画映射 | 仅Docs/Experiments/M01-Audio/RF01_EVENT_SOUND_MAP.md；无新增BGM授权 |
| Codex | 首批资源导入、M01-R1整合、VFX时钟与暂停、验证、GitHub | Assets/ReverbInTheRye/、SourceArt/、ArtIntake及共享接线；不同时写外部任务文件 |

IMechanismPlaytestControls和MechanismMode是已提交合同，不能自行改名/复制；M01PlaytestController已实现合同，通过M01CombatBridge接唯一CombatModule。模式配置跨场景重开传递，局内队列/预算/计数每局新建，不用static局内状态。1号不复制真实HP，2号不猜EnemyHit来源或实现规则；两者不相互合并分支。重建必须用集成入口M01IntegrationBatch.Setup，基础作者入口只生成未接线场景。

## 工程与交付

最新origin/main是唯一基线。先fetch记录完整SHA，再在独立工作目录开功能分支，不共用Unity Library/Temp、不操作别人窗口、不直接push main、不强推、不升级Package。跨目录只提最小请求，不能复制管理器绕开边界。

正式主线沿唯一GameLoop→RunSimulation→RunState、GameInput、CombatModule及AudioDirector/VfxDirector。新玩法只在M01显式启用；新场景不自动替换默认BuildSettings。已提供素材使用SourceArt/Intake01/intake-manifest.json追溯，不把.blend/GLB/离线预览放入Unity Assets重复导入，不擅改原文件。资源检查场景A01不等于游戏场景或美术验收。其余正式美术/音乐/音效/人声由用户提供或指定，作者颜色/数值/服装/字体提案非用户批准要求。

用户已授权Codex常规GitHub管理、审查整合与必要修复，持续有效；大重构/删功能/依赖升级不在授权内。初期只编译/导入与关键路径检查，中后期再按计划完善测试。没有Unity要如实声明，由Codex导入、生成meta、运行，不让用户处理自动化/Git冲突。测试/Profiler观察不能证明美术质量、游戏乐趣或发布性能。Codex实际接线和自动检查后构建独立试玩，再由用户验收。

## RF01 集成收口

RF01IntegrationBatch.Setup 生成独立 RF01_MechanismPlaytest 场景；Build 显式只构建 RF01，不替换默认 BuildSettings。复用唯一 GameLoop/CombatModule/AudioDirector；SonicAttackVFX 仅收真实 Shot/EnemyHit/EnemyKilled，M01DerivedFx 仅收真实派生命中与只读待回声快照。18个Sonic实例开局预热；粒子、干涉Shader和本局克隆的植被材质按局内时钟冻结。Shot事件附带真实发射方向供表现读取，不改伤害规则。终局清除待触发标记，保留本局命中统计。

四位已交版本冻结，等待RF01用户验收后再由Codex下达下一轮需求；不要自行扩大地图、导入角色WIP或做新增配乐。主美审查中的“100%无遮挡”、未经Blender实际打开确认的拓扑/骨骼/UV结论仍属待验证提案；Codex的实机证据优先。
