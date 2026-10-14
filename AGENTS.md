# 《麦田里的混响》当前协作约定

2026-10-14最新美术授权：人类替换机甲，所有怪物改红色幽灵并增加动态，处理天气变化后道路大片阴影闪烁。当前 `art/human-echoes-r7` 从已接受R6继续，仍使用独立Reviews/GP00-2026-10-13/workspace；入口Docs/HUMAN_ECHOES_R7.md。Main人类源为HumanEchoesR7/Fieldwalker.prefab，原机甲停用保留；幽灵通过只读快照/规则时间动画，死亡残影固定12槽。太阳方向固定，云影使用持久纹理连续混合，地面只接收阴影，局部聚光灯有限预算。原始Blender文件不覆盖；可编辑副本与CC0来源在SourceArt/HumanEchoesR7。此请求覆盖下文旧的机甲/暖橙敌人描述，视觉仍保留故乡的余温与白青主角，不扩玩法、不主动分派代理，只验证本次模块。

更新 2026-10-14。用户最新六项反馈要求：优先角色/敌人/攻击的无Bloom可读性，第一关保留金色乡村余温，灯光有实际来源，广播奇观退居次要并随真实玩法状态激活，麦田有空地/收割/倒伏变化，先加废弃农机与童年遗物。当前视觉R3为“尚有余温的故乡”，默认晴朗黄昏，雨夜保留为F6天气状态；白青玩家、暖橙敌人、暖色农机/路灯、冷色广播异常。幸存者只作为基底，不继续扩展玩法。不能自行宣称达到主流独立游戏画质。入口 README.md、Docs/HOMECOMING_R3.md、Docs/RESONANCE_R2.md、Docs/ART_EDITING.md。

用户最新要求解除空气墙并程序生成可探索麦田；此明确授权覆盖上述“不扩展玩法”在地图边界与探索接入方面的旧限制。探索R4开启Main的unboundedWorld，修复玩家/敌人/弹丸/遭遇/首领的绝对边界，按种子流式生成24米地块、默认驻留49块，复用R3乡村美术。地图默认每次进场新种子，可固定种子复现。原10分钟流程及两处出生区调谐仍保留。入口Docs/EXPLORATION_R4.md。

2026-10-14最新授权：新增有叙事文本的兴趣点（主角旧家/老筒仓/老吉他）、独立遗物栏与能力、局外经验/升级、存档及有风味的目标。这一明确请求覆盖上述暂停玩法扩展在这些系统上的旧限制，视觉方向仍沿用R3/R2。用户选择“外部AI任务卡，供我转发”，本轮入口为Docs/Project/HomecomingNext/README.md，HC00 v1共同标签hc00-v1，共同基线分支gameplay/homecoming-next-ready，当前整合分支gameplay/homecoming-integrated。四包分别在独立目录/分支开发；Codex负责共享契约/核心接线/Main/阶段整合。HC01四包现已接入Main，见下方整合约定，不执行旧GameplayNext任务卡，不把接口或独立预览等同完整玩法验收。

2026-10-14最新美术授权：将麦子从贴片换成模型，支持麦浪和透光。立体麦田R5在独立 `art/volumetric-wheat-r5` 分支，从HC02继续；同一Reviews/GP00-2026-10-13/workspace。入口Docs/WHEAT_VOLUME_R5.md。Main序列化使用WheatR5模型/材质，远景仍为模型；旧RyeCard组停用保留。WheatFieldPreview只做编辑/流式禁用后的预览，不再引入第二套运行地图。风读取规则时钟，沿用玩家/掉落压草、天气、阴影和49块回收；只验证本次模块。

2026-10-14最新反馈：角色/麦子互动突兀，光照与麦子着色简单、塑料感强。本轮 `art/wheat-contact-lighting-r6` 从R5继续，仍在同一独立workspace；入口Docs/WHEAT_CONTACT_R6.md。移除玩家周围整株裁切和瞬时缩短，使用GardenPresentation持有的64×64局部弹簧接触纹理；规则时钟冻结恢复、瞬移重置。麦材质使用纤维图、厚度/遮蔽数据与URP物理光照，保留旧模型/材质GUID，远景仍是模型。仅验证本次模块，不改Gameplay/存档，不用画面截图自称完成美术品质验收。

## 工作区与协作

2026-10-14 HC02最新授权为在HC01上打磨非线性组合与玩法循环，覆盖旧暂停玩法扩展在此范围的限制。当前分支gameplay/homecoming-resonance-loops，仍在同一独立workspace。入口Docs/Project/HomecomingNext/RESONANCE_LOOPS_HC02.md。共鸣三选一：回针/牵弦/归巢；碎音只来自实际拾取，返灯使用已熟悉的兴趣点，不能凭空发XP/强行改特殊敌人预警或复制一套伤害。存档版本2兼容读入1，新状态属于单局；保留R3/R4视觉世界和10分钟结局。用户没有要求新派工，本轮不主动联系外部AI或启动子代理。

HC01四包已在gameplay/homecoming-integrated整合，当前仍使用Reviews/GP00-2026-10-13/workspace。入口Docs/Project/HomecomingNext/INTEGRATION.md。GameLoop.Homecoming持有唯一档案桥与出发/日志门控，RunSimulation.Homecoming持有地点/遗物/目标，CombatHomecoming走原伤害链。正式Main开启homecomingEnabled；不重复安装旧任务或执行一次性构建脚本。存档在persistentDataPath/Homecoming，验证需独立路径，不覆盖用户真实档案。保留R4地图、R3视觉与原10分钟结局。

用户编辑工程为 `G:/图书馆/新建文件夹 (2)/My project`，新视觉基线在独立 `art/visual-editing` 分支。`My project-integration` 是 Codex 的整合与构建工作目录。旧 art/manual-editing 与 safety/pre-visual-rebuild-20261013 保留此次重构前用户改动。不要同时打开两个工程修改同一批资源；后续用户编辑期间不自动覆盖其改动。

GP00、玩法整合、探索R4及HC00使用独立目录 `Reviews/GP00-2026-10-13/workspace`，当前分支 `gameplay/homecoming-resonance-loops`，从world/procedural-fields-r4继续，玩法基础为已完成的gameplay/gp-integration。其余工作目录可能正被用户或外部AI使用其他分支；先确认，不自动切换、重置或覆盖。玩法基底说明见Docs/Project/GameplayNext/INTEGRATION.md。

四位外部 AI 的历史协作方式保留：用户转发需求、各自独立目录和分支上传，Codex审查接线合并。旧TASKS_01..08、实验和交接板已移除，不能重新执行。玩法第一轮已整合，用户现阶段要求把它作为基底，暂停继续打磨和扩展玩法，不自动执行旧任务卡里的后续派工。未经用户授权不主动联系其他AI。

本轮玩法使用 gameplay/gp00-ready 的 GP00 v1共同基线，准确版本见 Docs/Project/GameplayNext/GP00_RELEASE.md；不要假定 origin/main 已包含该接口。不要直接推 main、强推或盲合旧分支。原 PR33 是废弃假 HUD，不合入实际游戏。旧工程本地改动已保存于 safety/local-editor-20261013（a9f7b60），清理前基线为 8ba5162；保留 Git 历史以便恢复。不要清理用户根目录中的 Blender、图包、SourceArt/Intake01、Assets/ReverbInTheRye、Art_ThirdParty 与授权来源。

## 当前真实状态

Unity 6000.4.3f1 / URP17.4，Windows PC 斜俯视幸存者。唯一主场景 Assets/Game/Scenes/Main.unity；Build Settings 只含主场景。其他三张位于 Scenes/Layouts 的场景只是有效布局草稿，不是完成关卡。当前 6攻击/14被动/3进化/4拾取、4武器槽/6被动槽、三选一与重抽/放逐/跳过。正式美术/音乐尚未完成；声音R1为用户接受的基线。

GameLoop → RunSimulation → CombatModule 唯一管理 HP/XP/时钟/随机/伤害/死亡/奖励。规则位于 Assets/Waffle/Scripts/Rules；表现位于 Assets/Game/Presentation；内容目录与工具位于 Assets/Game/Content。新美术 Prefab 不扣血、不加另一个 GameLoop/播放器，不用粒子寿命决定伤害。W01组件迁移样板仍默认关闭，W05/W06/P17已真实接线；所有派生有限、每局清空、暂停/升级冻结。

三份视听清单统一放 Assets/Game/Content/Manifests，导入为 RC02ContentCatalog.asset，运行时只消费序列化引用。目录刷新不生成/重建美术资产。唯一 AudioDirector 消费声音/音乐；音乐仍整段测试循环、无动态分轨或拍点同步。

## 修改边界

本轮按用户授权由 Codex 修改 Assets/Game/Art、Environments、Scenes 与必要表现接线，后续用户可直接编辑。现用 PBR 材质、角色、道具在 Art/VisualIdentity；新地形与麦田在 Environments/AcousticGarden。原始素材保留。画完 Terrain 道路层后执行 Reverb→更新植被遮罩；此工具只更新遮罩，不重建场景。一次性 VisualRebuild/VisualPlayCapture 工具不会保留在 Assets。移动资源保留 .meta/GUID；场景保存/组件删除由 Unity 执行，不手写 YAML。

用户于2026-10-13明确要求：以后只需要验证本次模块可用，不必每次做全面检查。默认验证模块编译、实际接入和关键行为；出现失败或明确涉及共享逻辑的风险时再按需扩大范围，不默认跑全量回归、全面平衡/性能矩阵或重复构建。已保留的规则/战斗用例可按模块选取。完整整局试玩集中在阶段整合或发布节点进行。美术体验由用户验收，不能靠源码/截图宣称主流画质达标。
