# TASKS_08：玩法增量＋组件化＋视听装配管线

2026-10-12。用户已接受当前声音，优先丰富组合玩法和修bug，减少测试投入。本批Codex先修普通掉落物吸取，并下发原四位任务；用户分别转发下面四段。四位从包含本文件的最新origin/main记录完整SHA，各自独立分支PR，不合并。

共同必读AGENTS.md、Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md以及现有内容目录。目标是可复用组件和能执行的样板，不只交建议/表格。继续测试素材与现有声音，无需等正式美术。不新增测试工程/大矩阵，不改Packages，必要编译/最短样板运行即可。没有Unity如实说明，Codex最终导入接线。

## 转发给1号：规则组件与新构筑

~~~text
你负责 TASKS_08 的规则组件和玩法增量。先读取最新main的AGENTS.md、Docs/Integration/REVERB_TASKS_08.md、Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md；本批已修吸取范围，不覆盖旧分支文件。建立feature/rc02-rule-components独立分支，记录基线SHA。

唯一写入：Assets/Waffle/Scripts/Rules/RunContent/Logic/、Data/、新Components/（本批明确授权）、Docs/Design/RC01/Growth/与Docs/Design/RC02/Growth/。已有Tests可读，本轮不扩测试套件。不改RunContentContracts.cs、RunSimulation、CombatModule/GameLoop或其他作者目录。

把“触发→目标→载体→修饰”做成小而清楚的纯C#配方/运行时组件。优先冷却、实际移动累计、真正闪避、有限链击、残留区和属性修饰，避免万能DSL/反射/大框架。沿现有目录ID和GrowthStats，保留旧升级/抽卡/进化兼容。做W01旧行为适配样板，并真正实现W05弹跳电弧与W06移动残响的纯规则执行，输出由Codex在唯一CombatModule执行的命令；不能自己存敌人HP/XP或另造循环。API.md明确Advance/通知输入、敌人只读快照、命令字段、调用阶段、每局清理和接线示例，其他人无需猜接口。

新增本批P03弹速/P05持续/P08减伤/P17闪避放电对应组件和实际数值消费；说明必须写清适用武器、累计加成、限制与本阶变化。P17只在真正发动闪避时触发；W06按真实运动布置、限量、接触节流，闪避配合形成可辨打法；W05不重复链内目标，和回声/传播组合的深度/预算封顶。P03/P05/P08若已有聚合器就复用，不能再乘一次。共用参数唯一来自现有目录，允许合理修初值并说明。

本批预计接线后6攻击/14被动/3进化/4拾取。尚未接Unity宿主仍标RulesReady且不自行打开抽卡白名单；P10/P11/P14/P16、E04～06、D05/06保留planned，列为下一批明确接线清单。不要将新的配方或目录行冒充玩家已获得的内容。不把所有旧武器一次重写，新旧入口迁移表要明确避免双重开火。

提交能编译的代码与2～3个最小配方使用示例；只做必要编译/少量实际诊断，不增大套测试。交PR、完整SHA、基线、API.md、实际已做/未接线列表、最多3项Codex共享宿主请求。不要自行合并。
~~~

## 转发给2号：装配工具与场景组件

~~~text
你负责 TASKS_08 的Unity内容装配管线。读取最新main的AGENTS.md、TASKS_08与Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md，建feature/rc02-content-pipeline独立分支，记录基线SHA。

唯一写入：Assets/Drafts/RC01/Pipeline/（本批明确新增，Editor放Editor子目录）、Assets/Drafts/RC01/Scenes/RC02_AssemblyLab.unity及自己的配套meta、Docs/Design/RC02/Pipeline/。不要修改当前RC01_HarvestRoad、其他场景/现有Presentation与Editor脚本、主美/音效资产、AudioDirector/CombatModule/GameLoop、Packages/默认BuildSettings。Pipeline需要asmdef仅在本子目录建立，引用现有Rules/Runtime；不能在RC01根建asmdef。

实现合同v1 JSON→ScriptableObject表现目录的导入/刷新工具、清晰Inspector和单条预览。入口Waffle→RC02→Refresh Content Catalog与Open Assembly Lab。Editor完成路径解析，运行时用序列化引用；按kind+eventType+contentId匹配、精确优先、冲突报错。验证只需版本/重复/枚举/资源路径类型/非法预算。未知参数提示，缺资源保留旧绑定或占位，不销毁旧素材；刷新幂等并保留GUID。

你先自带当前W01/W04/P12的最小清单，引用现有测试资源跑通，不等待主美和音效新包。主美交ComponentsV1、音效交PipelineV1的JSON后应能直接刷新。只读既有PresentationEvent并用其位置/方向/半径/持续与来源；池化VFX实例、暂停冻结/停止和重开清理。旧表现与新目录明确迁移列表，不能同时双播。声音目录导出已有AudioContentBinding/音乐档案配置，最终AudioDirector消费桥由Codex接，不新建AudioManager。

独立AssemblyLab由幂等Editor工具复制当前HarvestRoad至新路径后装配，复用GameLoop、真实XP三选一和F11，不手写YAML。用新目录接W01/W04/P12样板，其余旧绑定保留；没有规则支持时W05/W06只预览不假装有伤害。提供一份首图分钟段/遭遇配置的组件数据样板，先复用现有敌人，不另外开四张完整地图。写清新增一个内容/替换一种素材所需步骤。

不合入PR33旧HUD状态逻辑，不添加第二时钟/抽卡或Time.timeScale控制。只必要编译/导入/样板操作，不建设测试矩阵。没有Unity则交源码与明确入口，不伪称场景已运行。交PR、完整SHA、范围、README、迁移/未接线清单和最多3项Codex请求，不自行合并。
~~~

## 转发给主美：可复用测试表现模板

~~~text
你负责 TASKS_08 的美术组件模板与资源包。读取最新main AGENTS.md、TASKS_08、Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md，建feature/rc02-art-components独立分支。唯一写Assets/Drafts/RC01/Art/ComponentsV1/与Docs/Design/RC02/Art/，不改原始用户素材、场景、共享运行时代码或其他作者目录。

本轮重点是方便后续填肉的可调模板，使用现有测试材质/模型，暂不重做正式角色/地面/大场景。保持当前乡村旧机械/迷幻声波统一：静态冷静克制、战斗短促爆发，哑光且避免均匀亮面塑料；颜色、形状、运动共同区分功能，不只换颜色。

交3类Prefab模板：Projectile（朝向、弹体、尾迹）、Hit/Arc（命中/电弧短连接）、Zone/Trail（预警、残留、结束）。根节点单位缩放、+Z朝前、米制，暴露宽度/尺寸/尾长/强度，有限寿命，停发/回池清拖尾与粒子。不在Prefab中加伤害碰撞/游戏Update/音频播放器；运行时参数与池由2号目录驱动。可用幂等Editor生成器创建真实Prefab/材质，不手写YAML；无Unity时给可执行生成入口与未导入说明。

先提供W01、W04、P12已有玩法模板映射与W05/W06/P17新增组合所需的电弧、移动残留、闪避放电变体。兼顾地面小体量可读性，预警与伤害形状能对应；不把装饰范围当真实伤害半径。附W05/W06/P03/P05/P08/P17的测试图标，优先清楚的轮廓和家族关系，不拼无来源素材。

交合同v1 art-manifest.json（在自己的Docs目录），路径指向自有组件包，来源/status/预算/暂停策略齐全；先用当前PresentationEvent已有枚举，没有事件的只标预览，向Codex提请求，不能自己添加共享枚举。给参数说明、替换正式资产步骤、一页形状/颜色/运动语言说明。不要无依据宣称达到最终画质，少量预览即可，不扩测试。交PR、完整SHA、基线、实物文件/生成入口和最多3项请求，不自行合并。
~~~

## 转发给音效总监：声音与音乐资源管线

~~~text
你负责 TASKS_08 的音频/音乐资源管线。用户接受当前R1声音，本轮不重做音色、不批量生成新音乐。读取最新main AGENTS.md、TASKS_08、Docs/Design/RC02/COMPONENT_PIPELINE_CONTRACT.md，建feature/rc02-audio-pipeline独立分支。唯一写Assets/Drafts/RC01/Audio/PipelineV1/、Docs/Design/RC02/Audio/；不覆盖PolishR1、旧Audio素材、AudioDirector、场景、规则或2号导入工具。

交合同v1 audio-manifest.json与music-manifest.json：复用已合入22段短音及已有战斗/升级循环，明确实际事件、contentId、资源路径/用途、音量、pitch范围、并发/节流、优先级、空间参数、暂停策略、来源。已有R1相机minDistance30/maxDistance65与节流不能被旧cue表默默覆盖，通用组与精确绑定按合同匹配，避免同一实际命中双播。W05/W06/P17先复用现有素材作为TEST_ONLY映射，没实装事件就标未接线，不假称已播放。

实现自己目录的资源包导出/静态检查工具与说明，验证路径存在、格式、ID/重复键、有限预算、声级参数；参数导入为2号目录/现有AudioDirector配置，不能另建常驻播放器。音乐档案定义首图combatLoop/levelUpLoop/音乐音量/过渡秒数、可选bpm和beatsPerBar；未知拍点填0。没有同步分轨时stem留空，先让现有整段循环可替换、暂停/升级/终局状态有明确策略，不承诺不存在的拍点同步。以后数学摇滚/后摇/电子曲目只需替换同档案资源，正式配乐本轮后置。

提交README说明导出→2号刷新→Codex接AudioDirector的步骤，列新增一个音效/替换一首音乐需要改的文件，以及授权来源/循环接缝/响度建议。现有声音不再重复3段大试听，也不建设压力测试；做必要文件检查即可。交PR、完整SHA、基线、manifest与工具、已做/未接线清单和最多3项Codex请求，不自行合并。
~~~

## Codex收口与下一步

Codex保留共享宿主/真实命令执行/事件字段/程序集最小接线、共享meta、GitHub整合与打包的唯一所有权。先规则API与目录工具各自审查，再接新配方执行与真实位移/闪避，最后刷新两份表现包。沿旧机制保留合法降级，避免迁移两套同时运行。只必要编译与一段实际操作，不重跑全项目测试。

08A验收给用户一个新版首图，不要用户手工串联四个包；看链击、移动残留/闪避组合是否改变打法，以及替换素材能否只改目录。08B在08A可用的组件上补齐剩余4被动/3进化/2拾取与更多遭遇组合，再逐图推进。阶段验收仍保留，用户不需要检查Git、meta或导入细节。
