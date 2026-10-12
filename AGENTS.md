# 《麦田里的混响》开发协作约定

更新2026-10-12。用户最新指令：声音R1先接受为当前基线，优先修玩法bug、增量玩法与可复用组件系统，搭好美术/音乐管线便于后续润色填充；目前不需要更多测试投入，仅做必要编译/导入/针对性诊断，完备验证中后期安排。最新任务入口Docs/Integration/REVERB_TASKS_08.md、合同Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md。仍保持取消四模式、局内成长/地图草稿、测试素材和设定后置。TASKS_01..07为阶段历史，不能据旧冻结状态阻止当前推进。

## 工作方式

保持原四位外部AI：用户转发需求，各自独立分支上传GitHub；Codex审查、管理仓库、共享接线、本地自动化与打包，用户体验每阶段。用户负责感受/方向等必要人工检查；不要把Git冲突/可自动测试任务交给用户。没有Unity如实声明，Codex执行装配并生成meta。初期只编译与关键路径，完备测试/平衡/性能矩阵中后期计划进行。

长期项目背景Docs/Project/REVERB_CONTEXT_V1.md，用户新指令优先。Windows PC / Unity6000.4.3f1 / URP17.4.0 / 斜俯视3D幸存者，WASD移动/自动攻击/手动闪避，F11全屏。保持乡村怪谈/旧机械/迷幻音波视听统一，但本轮功能名、测试素材与草稿先行，不增加剧情、正式角色制作或正式配乐阻塞玩法。

## 当前可交付目标

首轮完整目录6攻击家族/18被动/6进化/6类拾取，4武器槽/6被动槽、三选一＋重抽/放逐/跳过、精英奖励与进化。06A先并行交目录/规则核心/四图草稿/首图装配与视听样板；06B Codex接第一张完整局，先4攻击/10被动/3进化/4拾取、至少3可辨构筑；06C补齐完整目录并逐图实现。只把可获得且有真实效果的项目算实装，不能用换皮/阶数/表格/组合数学夸大丰富度。每个可玩批次用户验收。

四张地图沿背景中的秋收之路、旧农贸市场、第四粮仓、无线电荒原，每图交空间、分钟压力、敌群/事件/奖励与反制草稿。第一张先实际接线，后续复用同一规则链，普通事件不切场传送。

## 唯一写入范围

| 执行者 | 唯一范围与责任 |
|---|---|
| 1号 | Assets/Waffle/Scripts/Rules/RunContent/Logic/、Data/、Tests/；Docs/Design/RC01/Growth/。成长核心/目录/数值唯一作者；Tests自建Editor-only asmdef，不将NUnit编入运行时Rules |
| 2号 | Assets/Drafts/RC01/Scenes/、Environments/、Presentation/、Editor/；Docs/Design/RC01/Scenes/。四图战斗草稿、独立场景/UI装配；只读规则 |
| 主美 | Assets/Drafts/RC01/Art/；Docs/Design/RC01/Art/。测试材质副本/四图风格样板与图示语义 |
| 音效 | Assets/Drafts/RC01/Audio/；Docs/Design/RC01/Audio/。测试短音/既有循环映射与四图声音草稿 |
| Codex | 中央任务/合同、RunContentContracts.cs、共享父目录meta、GameLoop/RunSimulation/CombatModule/AudioDirector与程序集最小接线、GitHub和本地验证 |

TASKS_08本批覆盖以上旧范围：1号另获准Rules/RunContent/Components/与Docs/Design/RC02/Growth/，本轮不扩测试；2号本批只写RC01/Pipeline/、独立Scenes/RC02_AssemblyLab.unity及Docs/Design/RC02/Pipeline/，不改当前HarvestRoad与旧Presentation/Editor；主美只写RC01/Art/ComponentsV1/与Docs/Design/RC02/Art/；音效只写RC01/Audio/PipelineV1/与Docs/Design/RC02/Audio/。具体以TASKS_08为准。中央宿主仍Codex所有；四位不修改彼此接口与资源。

共享父目录meta已提供，不覆盖不同GUID。四位不互相合并/改文件，不擅改旧规则/管理器/用户原素材；跨范围最小请求交Codex。原PR33 HUD仍未接入，2号不能直接合入其独立Time.timeScale/假生命/假模式逻辑，不另抢Assets/UI目录。

## 唯一权威与合同

GameLoop→RunSimulation→RunState、GameInput、CombatModule仍唯一。新成长模块只管装备与选择，不复制XP/HP/时钟；Codex接现有经验与升级队列。多武器/拾取效果由同一规则伤害/死亡/掉落链处理；视图不能碰撞扣血、根据粒子计时决定伤害或再造CombatModule。

RunContentContracts.cs已发布Waffle.RunContent的只读快照和选择请求；各作者引用同一合同，不复制。固定ID见Docs/Design/RC01/CONTENT_CONTRACT.md。真实升级暂停/恢复归RunSimulation；HUD不能写Time.timeScale、自己抽卡/切规则或另监听Esc。预览数据只允许显式TEST_ONLY独立预览，无宿主正常场景禁按钮等待接线。

玩家四模式面板已移除，普通独立运行固定当前组合基底；内部模式枚举与回归测试可保留，仅用于Editor/显式自动化，不是游戏入口，不再要求用户逐模式验收。成长接线后P12/P13从局内取得，组合自然形成；旧RF01的XP关闭/固定武器实验配置不能延用到新的正式成长试玩。

## 素材与工程

用户原素材Assets/ReverbInTheRye和SourceArt/Intake01保留来源/原型标签；角色.blend仍WIP，不自动导入骨骼/假报品质完成。本轮可用现有测试模型/材质/声音与独立副本；不采购、不重塑原模型、不生成正式音乐/人声。主美零塑料/100%可见、音效无爆音等必须有对应实机/测量证据，不能靠源码或平均数认证。

最新origin/main唯一基线，先fetch记录完整SHA，独立功能分支与工作目录，不共用Unity Library/Temp、不操作用户窗口、不直接push main/强推、不升级Package/替换渲染管线/默认BuildSettings。场景由Unity或幂等Editor工具生成，不手写YAML。复制旧场景时先另存至自己的目录，不能调用会重写M01/RF01源场景的Setup。

用户常规GitHub管理、审查整合、必要修复授权持续有效；本轮新增局内道具/成长/地图草稿已明确授权。完备存档经济、剧情、正式制作、无关大重构不在本轮。验收前明确已实现/草稿/未验证，不能将当前测试素材画面称为主流品质已达标。

## 06B收口状态

四份06A已在integration/rc01-playable整合；实现边界与人工验收见Docs/Integration/RC01_ACCEPTANCE.md，验证见Docs/Verification/RC01/。待本批验收反馈后发下一轮任务；继续工作以最新main为基线，不重复沿旧分支覆盖Codex接线。

## RC01首次反馈

用户认为升级稍频繁但可接受、说明需明确、音效简陋。Codex微调等级4之后经验门槛、按实际阶参数生成说明并调整UI、接已有声音变体/密度；音色重做交TASKS_07音效总监，唯一写PolishR1子目录，不改共享宿主。其他三位本次不重复抢写。

## 音效R1收口

TASKS_07音效交付57d2e45（PR42）：8组22短音与3试听。Codex按真实事件接线、适配30米斜俯视听音距离并支持按事件清除暂停尾音；测量与验收边界见Docs/Verification/RC01-SoundR1/。听感待用户验收，不把合成材料语义或离线峰值当实物拟音/主流品质认证。升级说明/经验微调保留，不派四位重复写本批共享代码。

## RC02启动与吸取反馈

用户已接受R1声音为当前基线，不继续重做音色。吸取诊断：经验3/3.75/4.5/5.25米原已生效，普通回复/磁吸/清场道具此前固定接触1米；本批统一普通掉落物磁吸，复用同一移动函数、真实T10奖励路径，精英箱仍接触。HUD显示实际半径，说明同步。新版规则代码a6ba297，组件/视听管线本批仅发布合同与分工，不能称四位新系统已实现。

TASKS_08A先组件适配W01＋新增W05/W06与P03/P05/P08/P17，Codex接线后目标6/14/3/4；08B再补原6/18/6/6。2号搭目录导入/预览和独立场景，主美交三类模板，音效交复用R1/旧音乐的绑定与档案。不增加音频宿主，不一次重写全部旧武器。必要检查后给用户阶段试玩，其余时间投入玩法与生产管线。
