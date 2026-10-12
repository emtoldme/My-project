# 《麦田里的混响》：开发协作约定

更新：2026-10-12。长期源指令见Docs/Project/REVERB_CONTEXT_V1.md。当前事实见Docs/Verification/Reverb-Followup-02/REVIEW.md；只执行Docs/Integration/REVERB_TASKS_03.md，TASKS_01/02为已交付历史。保持原四位外部AI分担负载，用户转发需求，各AI独立目录/分支交GitHub，Codex审查、本地接线和自动化，再交用户体验验收。

## 当前范围

用户选择下一阶段独立“回声＋传播”机制试玩；正式美术稍后准备，不索要、不自行制作或采购正式素材。复用Unity6000.4.3f1/URP17.4.0及已有三个实验，不扩完整关卡/大框架/存档/装备编辑器，不升级依赖。新方向仍为乡村怪谈/锈蚀机械/迷幻音波，S2首版视觉被拒绝，旧雕塑返工任务撤回。

M01独立场景比较None/Echo/Propagation/Combined，切换即同seed重开；保留WASD/自动攻击/Space闪避/Esc暂停。默认S1/S2不启用新机制。纯规则只由唯一CombatModule应用HP、死亡、掉落；只读PresentationDispatched不能反写伤害。最小可选规则钩子与共享程序集由Codex收口。

## 唯一写入范围

| 执行者 | 任务 | 可写范围 |
|---|---|---|
| 1号 | M01二维机制模块/宿主接口 | Assets/Experiments/Mechanism/Rules/、Docs/Experiments/M01/ |
| 2号 | M01独立场景/面板/表现池 | Mechanism下Presentation/、Editor/、Scenes/；Docs/Experiments/M01-View/ |
| 主美 | 机制辨识技术审查 | 仅Docs/Art/Reverb/ |
| 音效总监 | 机制声响应接线表 | 仅Docs/Experiments/M01-Audio/ |
| Codex | Contracts/Integration、正式共享钩子、验证、GitHub | 跨目录统一处理，不与派出任务同时写同一文件 |

IMechanismPlaytestControls和MechanismMode是已提交合同，不能自行改名/复制；目前没有实现类，不把等待接线写成完成。模式配置可跨一次重开传递，局内队列/预算/计数必须每局新建，不用static局内状态。1号不复制真实HP，2号不猜EnemyHit来源或实现规则；两者不相互合并分支。

## 工程与交付

最新origin/main是唯一基线。先fetch记录完整SHA，再在独立工作目录开功能分支，不共用Unity Library/Temp、不操作别人窗口、不直接push main、不强推、不升级Package。跨目录只提最小请求，不能复制管理器绕开边界。

正式主线沿唯一GameLoop→RunSimulation→RunState、GameInput、CombatModule及AudioDirector/VfxDirector。新玩法只在M01显式启用；新场景不自动替换默认BuildSettings。测试资源TEST_ONLY，正式美术/音乐/音效/人声以后由用户提供或指定，作者颜色/数值/服装/字体提案非用户批准要求。

用户已授权Codex常规GitHub管理、审查整合与必要修复，持续有效；大重构/删功能/依赖升级不在授权内。初期只编译/导入与关键路径检查，中后期再按计划完善测试。没有Unity要如实声明，由Codex导入、生成meta、运行，不让用户处理自动化/Git冲突。测试/Profiler观察不能证明美术质量、游戏乐趣或发布性能。Codex实际接线和自动检查后构建独立试玩，再由用户验收。
