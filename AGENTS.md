# 《麦田里的混响》：开发协作约定

更新：2026-10-12。用户新总指令存Docs/Project/REVERB_CONTEXT_V1.md，作为长期项目背景；同时明确保持此前多AI协作以分摊负载。先读该源指令、Docs/Verification/Reverb-Experiments-01/REVIEW.md及Docs/Integration/REVERB_TASKS_02.md；REVERB_BASELINE_REVIEW.md和REVERB_TASKS_01.md仅为第一轮历史。后续任务以当前任务卡为准，不能从历史文件自行恢复旧任务。

## 当前目标与创作边界

复用Unity6000.4.3f1/URP17.4.0现有3D幸存者灰盒，第一轮三个隔离技术实验已收齐并做Unity验证；当前只复核与修补这三个实验；不立即开发完整游戏、不重构大框架、不升级依赖。新艺术背景为乡村怪谈/锈蚀机械/迷幻音波，替代旧抽象雕塑制作方向。S2首版视觉被用户拒绝，不能称有质感或正式关卡；A-04/P-04/C-03/S-04返工卡撤回，不继续按旧雕塑路线制作。

用户已选择继续技术实验、正式美术稍后准备，本轮不索要正式素材。正式美术/模型/贴图/动画/UI及音乐/音效/人声以后由用户提供或指定。主美和音效总监保留专业协作职责，但无具体委托不自行生产正式内容。技术占位必须TEST_ONLY，不能宣称艺术质量或正式配乐；不把新故事背景当作立即实现剧情/多关卡的任务。

## 分工与唯一写入范围

| 任务 | 唯一负责人 | 当前可写范围 |
|---|---|---|
| E-02 组合复核与适配表 | 1号 | Assets/Experiments/Equipment/、Docs/Experiments/E02/ |
| E-01 复核与小规模性能观察 | 2号 | Assets/Experiments/VisualPipeline/、Docs/Experiments/E01/ |
| A-R01 技术规范复核 | 主美 | 仅Docs/Art/Reverb/ |
| E-03 TEST_ONLY循环信号V2 | 音效总监 | AudioSync下音频资产与自己的生成工具、Docs/Experiments/E03/；不改控制器/探针/GUI/场景 |
| 共享接线/程序集/.meta/正式场景/渲染配置/整合与验证 | Codex | 跨目录需求统一收口，不和已派任务同时写同一文件 |

用户转发任务，AI在独立目录/分支交GitHub，Codex负责审查、本地开发和自动化，再由用户体验验收；此模式继续，不将制作负载集中给Codex。AI写明跨目录请求，不能复制管理器绕过边界。没有Unity如实写未运行，由Codex导入/补必要.meta/执行，不让用户处理自动化和Git冲突。音效AI若确实无法写调度代码，必须交明确接口/夹具和缺项，由Codex接管必要部分，不以文字宣称完成。

## 工程与Git约定

main为唯一共享基线。fetch最新origin/main并记录完整SHA；开独立功能分支、独立工作目录，不在别人的目录切分支，不共用Unity Library/Temp。不直接push main、不强推别人分支、不自行合并其他AI分支或PR。

用户此前已授权Codex管理GitHub、审查整合和常规修复，该授权继续有效；正式创作决策、大规模重构/删功能/升级依赖不在授权内。新原型不能因存在代码就自动接入正式玩法。每次交付记录基线/SHA、范围、实际验证与未验证项，保留作者提交及可回退成果。

正式主线沿用唯一GameLoop → RunSimulation → RunState、GameInput、CombatModule及AudioDirector/VfxDirector。PresentationDispatched只读、不反写伤害。独立实验允许自己的隔离运行状态与测试入口，但不能进入默认战斗或与正式管理器并行接管同一实例。原默认WASD/自动攻击/Space闪避保持，除非用户另定操作。

初期只做必要编译/导入和高价值关键路径；中后期完备测试计划继续有效。报告明确区分已实现、已验证、原型、未验证和设想，不以测试通过证明艺术完成或性能达标。每阶段仍由用户验收，必要人工检查由用户完成，其余自动化由Codex承担。
