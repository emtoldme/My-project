# 《麦田里的混响》当前协作约定

更新 2026-10-13。用户最新反馈：视觉语言仍不明确，特效和光影平淡、艺术感不足。现阶段幸存者只作为基底，不再继续打磨或扩展玩法；工作重点为画面、氛围与动效。Codex直接修改主场景与表现，视觉R2以“风雨中的失谐声场”为方向，统一断裂共鸣弧、调谐柱、频率刻线和旧信号旗，使用墨蓝/旧黄铜/粉白，环境琥珀、玩家冷青、危险朱红。不能自行宣称达到主流独立游戏画质。入口 README.md、Docs/RESONANCE_R2.md、Docs/ATMOSPHERE_R1.md、Docs/ART_EDITING.md。

## 工作区与协作

用户编辑工程为 `G:/图书馆/新建文件夹 (2)/My project`，新视觉基线在独立 `art/visual-editing` 分支。`My project-integration` 是 Codex 的整合与构建工作目录。旧 art/manual-editing 与 safety/pre-visual-rebuild-20261013 保留此次重构前用户改动。不要同时打开两个工程修改同一批资源；后续用户编辑期间不自动覆盖其改动。

GP00、玩法整合及视觉R2使用独立目录 `Reviews/GP00-2026-10-13/workspace`，当前视觉分支 `art/resonance-language-r2`，从art/atmosphere-r1继续，玩法基础为已完成的gameplay/gp-integration。其余工作目录可能正被用户或外部AI使用其他分支；先确认，不自动切换、重置或覆盖。玩法基底说明见Docs/Project/GameplayNext/INTEGRATION.md。

四位外部 AI 的历史协作方式保留：用户转发需求、各自独立目录和分支上传，Codex审查接线合并。旧TASKS_01..08、实验和交接板已移除，不能重新执行。玩法第一轮已整合，用户现阶段要求把它作为基底，暂停继续打磨和扩展玩法，不自动执行旧任务卡里的后续派工。未经用户授权不主动联系其他AI。

本轮玩法使用 gameplay/gp00-ready 的 GP00 v1共同基线，准确版本见 Docs/Project/GameplayNext/GP00_RELEASE.md；不要假定 origin/main 已包含该接口。不要直接推 main、强推或盲合旧分支。原 PR33 是废弃假 HUD，不合入实际游戏。旧工程本地改动已保存于 safety/local-editor-20261013（a9f7b60），清理前基线为 8ba5162；保留 Git 历史以便恢复。不要清理用户根目录中的 Blender、图包、SourceArt/Intake01、Assets/ReverbInTheRye、Art_ThirdParty 与授权来源。

## 当前真实状态

Unity 6000.4.3f1 / URP17.4，Windows PC 斜俯视幸存者。唯一主场景 Assets/Game/Scenes/Main.unity；Build Settings 只含主场景。其他三张位于 Scenes/Layouts 的场景只是有效布局草稿，不是完成关卡。当前 6攻击/14被动/3进化/4拾取、4武器槽/6被动槽、三选一与重抽/放逐/跳过。正式美术/音乐尚未完成；声音R1为用户接受的基线。

GameLoop → RunSimulation → CombatModule 唯一管理 HP/XP/时钟/随机/伤害/死亡/奖励。规则位于 Assets/Waffle/Scripts/Rules；表现位于 Assets/Game/Presentation；内容目录与工具位于 Assets/Game/Content。新美术 Prefab 不扣血、不加另一个 GameLoop/播放器，不用粒子寿命决定伤害。W01组件迁移样板仍默认关闭，W05/W06/P17已真实接线；所有派生有限、每局清空、暂停/升级冻结。

三份视听清单统一放 Assets/Game/Content/Manifests，导入为 RC02ContentCatalog.asset，运行时只消费序列化引用。目录刷新不生成/重建美术资产。唯一 AudioDirector 消费声音/音乐；音乐仍整段测试循环、无动态分轨或拍点同步。

## 修改边界

本轮按用户授权由 Codex 修改 Assets/Game/Art、Environments、Scenes 与必要表现接线，后续用户可直接编辑。现用 PBR 材质、角色、道具在 Art/VisualIdentity；新地形与麦田在 Environments/AcousticGarden。原始素材保留。画完 Terrain 道路层后执行 Reverb→更新植被遮罩；此工具只更新遮罩，不重建场景。一次性 VisualRebuild/VisualPlayCapture 工具不会保留在 Assets。移动资源保留 .meta/GUID；场景保存/组件删除由 Unity 执行，不手写 YAML。

用户于2026-10-13明确要求：以后只需要验证本次模块可用，不必每次做全面检查。默认验证模块编译、实际接入和关键行为；出现失败或明确涉及共享逻辑的风险时再按需扩大范围，不默认跑全量回归、全面平衡/性能矩阵或重复构建。已保留的规则/战斗用例可按模块选取。完整整局试玩集中在阶段整合或发布节点进行。美术体验由用户验收，不能靠源码/截图宣称主流画质达标。
