# 《麦田里的混响》：开发协作约定

更新：2026-10-12。长期源指令见Docs/Project/REVERB_CONTEXT_V1.md。M01实测见Docs/Verification/M01/REVIEW.md，最新反馈与显示修复见Docs/Verification/M01-Feedback/REVIEW.md。用户肯定组合乐趣/改变玩法、闪避可保留；声波作用不明显，且旧启动器不能全屏。当前只执行Docs/Integration/REVERB_TASKS_04.md；TASKS_01/02/03为历史。保持原四位外部AI分担负载，用户转发需求，各AI独立目录/分支交GitHub，Codex审查、本地接线和自动化，再交用户体验验收。

## 当前范围

用户选择下一阶段独立“回声＋传播”机制试玩；正式美术稍后准备，不索要、不自行制作或采购正式素材。复用Unity6000.4.3f1/URP17.4.0及已有三个实验，不扩完整关卡/大框架/存档/装备编辑器，不升级依赖。新方向仍为乡村怪谈/锈蚀机械/迷幻音波，S2首版视觉被拒绝，旧雕塑返工任务撤回。

M01-R1只改机制反馈：回声预备→原目标二次受伤→传播去向与实际结果。回声不是AOE，不靠扩大地面圈暗示范围。保留现有数值/延迟/距离/闪避；组合值得深化已获用户肯定，不重复要求方向确认。Codex修全屏，四位按TASKS_04分担反馈工作；未交付不得宣称新动效/音色已完成。

M01独立场景比较None/Echo/Propagation/Combined，切换即同seed重开；保留WASD/自动攻击/Space闪避/Esc暂停。默认S1/S2不启用新机制。纯规则只由唯一CombatModule应用HP、死亡、掉落；只读PresentationDispatched不能反写伤害。最小可选规则钩子与共享程序集由Codex收口。

## 唯一写入范围

| 执行者 | 任务 | 可写范围 |
|---|---|---|
| 1号 | 只读PendingEchoInfo快照/少量规则检查 | Assets/Experiments/Mechanism/Rules/、Docs/Experiments/M01/ |
| 2号 | 回声预备/命中/结果的只读表现 | Mechanism下Presentation/；Docs/Experiments/M01-View/；本轮不写Editor/Scenes |
| 主美 | 单目标回声辨识分镜与审查 | 仅Docs/Art/Reverb/ |
| 音效总监 | TEST_ONLY回声/传播短音变体与接线表 | Mechanism下Audio/、Docs/Experiments/M01-Audio/；不写共享AudioDirector |
| Codex | Contracts/Integration、M01独立材质、正式共享钩子、验证、GitHub | 跨目录统一处理，不与派出任务同时写同一文件 |

IMechanismPlaytestControls和MechanismMode是已提交合同，不能自行改名/复制；M01PlaytestController已实现合同，通过M01CombatBridge接唯一CombatModule。模式配置跨场景重开传递，局内队列/预算/计数每局新建，不用static局内状态。1号不复制真实HP，2号不猜EnemyHit来源或实现规则；两者不相互合并分支。重建必须用集成入口M01IntegrationBatch.Setup，基础作者入口只生成未接线场景。

## 工程与交付

最新origin/main是唯一基线。先fetch记录完整SHA，再在独立工作目录开功能分支，不共用Unity Library/Temp、不操作别人窗口、不直接push main、不强推、不升级Package。跨目录只提最小请求，不能复制管理器绕开边界。

正式主线沿唯一GameLoop→RunSimulation→RunState、GameInput、CombatModule及AudioDirector/VfxDirector。新玩法只在M01显式启用；新场景不自动替换默认BuildSettings。测试资源TEST_ONLY，正式美术/音乐/音效/人声以后由用户提供或指定，作者颜色/数值/服装/字体提案非用户批准要求。

用户已授权Codex常规GitHub管理、审查整合与必要修复，持续有效；大重构/删功能/依赖升级不在授权内。初期只编译/导入与关键路径检查，中后期再按计划完善测试。没有Unity要如实声明，由Codex导入、生成meta、运行，不让用户处理自动化/Git冲突。测试/Profiler观察不能证明美术质量、游戏乐趣或发布性能。Codex实际接线和自动检查后构建独立试玩，再由用户验收。
