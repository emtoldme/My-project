# 第一轮交付后的协作收尾与技术复核

> 已交付历史：本轮四位复核结果见Docs/Verification/Reverb-Followup-02/REVIEW.md。用户已选择下一阶段机制试玩，当前只执行REVERB_TASKS_03.md，不重复领取旧任务。

2026-10-12。本轮只收尾三个技术实验，不进入正式关卡/艺术制作或第四个大实验。统一从最新origin/main开独立工作目录/分支，记录完整基线SHA，保留原交付分支；提交PR和实际验证/缺项，不能自行合并，不升级依赖。先读Docs/Verification/Reverb-Experiments-01/REVIEW.md及最新AGENTS.md。用户已选择继续技术实验，正式美术稍后准备；本轮不向用户索要正式素材，不自行采购、生产或批准正式艺术。以下提示词待用户转发，未自动发外部消息。

## 转给1号

~~~text
E-02已由Codex收拢。请fetch最新main，复核Codex对parent.Time调度和暂停DirectHit门控的修复，尤其粗步长跨过回声/传播时与细步长的结果一致。不要继续旧分支或扩大装备数量。若无缺陷只交简短复核结论；若有真实问题，在Assets/Experiments/Equipment/和Docs/Experiments/E02/内最小修复，新分支review/e02-integration-followup。

补一张未来真实战斗适配表：直接命中的规则步、已有Enemy.Serial/IsAlive是否足够作句柄、由谁排队和清队列；先读现有CombatModule，不重复索要已有字段。仍不得在PresentationDispatched反写伤害，不接正式UpgradeRules、不重构规则。配置外部可变、预算字典/日志长时增长是一维夹具限制，明确记录，不为此制造大型框架。
~~~

## 转给2号

~~~text
E-01已由Codex修ShaderGraph生成并生成实际场景；碎块备选改为一个手动推进的Unity ParticleSystem。请fetch最新main读整合报告，复核材质、暂停/回池/重开和实机开关截图。新分支review/e01-integration-followup，只写Assets/Experiments/VisualPipeline/和Docs/Experiments/E01/；不改共享渲染配置、Core/Views或主美目录，不改Codex的Editor/Verification辅助代码。

补一次小规模真实Profiler/Frame Debugger观察：同相机、相同对象数量、声波开/关，记录设备/分辨率、实际测到的CPU/GPU/GC与DrawCall；获取不到GPU值就明确不可用，不能拿FPS换算冒充GPU时间。只测当前小场景，不铺200敌压力或重新打大包。记录ShaderGraph生成器依赖17.4.0内部API，已生成图可直接复用，不升级依赖。需要正式模型时写最少文件/通道/节点规格，等待用户指定。
~~~

## 转给主美

~~~text
A-R01已收拢。Codex修正了URP Lit贴图通道：Metallic槽R金属/A平滑，Occlusion槽读G；外部ORM须转换，不能照旧错误表导入。请fetch最新main复核，不恢复旧通道定义。新分支review/ar01-technical-audit，仅写Docs/Art/Reverb/。

本轮仅做技术审查：把现有规范压成一页接入检查表，校对模型轴向/米制/Visual适配、URP贴图通道、来源证据字段；结合E-01截图明确哪些只能证明接入，哪些仍缺正式美术。不要开正式素材包制作、采购或要求用户提供资源。角色服装/怪物形态、颜色/HDR/字体是提案，不能写用户已批准或工程强制常数。基于真实实验截图分别标技术接入与艺术待制作，用户未指定素材来源前不自行生成正式资源。
~~~

## 转给音效总监

~~~text
E-03已收拢并由Codex补共同DSP恢复、清理、兼容检查与测量说明。请fetch最新main阅读实际结果和audio-audit.json。原TEST_ONLY两轨首尾阶跃约0.47/0.49满幅，原GUI增益叠加超过0dBFS；Codex补0.5输出trim，但不代表循环无爆音。新分支fix/e03-test-signal-loops，只写Assets/Experiments/AudioSync/（排除Codex控制器/探针/GUI和场景，仅音频资产及自己的生成工具）、Docs/Experiments/E03/。

给明确标记TEST_ONLY的可复现测试信号修订：相同采样率/帧数/声道、同步标记与循环切点，足够低的峰值余量，输出首尾样点/导数连续性与叠加峰值记录。允许技术用简单合成信号，不制作正式音乐；保留原文件用于结果追溯，以V2命名，附实际生成源码/参数。不能用短淡变就声称真人听感通过。主线程timeSamples有缓冲量化，不能伪称微秒相位测量；音频线程首非零探针也只适用于已知测试起音，不能代表任意歌曲或硬件输出认证。正式stems仍由用户以后提供/指定，本轮不索要。同步起音标记必须在两轨同一采样位置，不应因V2静音前导而让探针测不到信号；缺少Unity时附可复现离线证据并明确未实机运行。
~~~

Codex继续整合、复现、自动化与必要共享修复；当前正式画面问题没有被技术实验解决，后续需在用户指定资产上验证材质和画面。没有把首轮实验验收当作正式美术或游戏阶段通过。
