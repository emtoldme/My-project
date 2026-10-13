# 《麦田里的混响》当前协作约定

更新 2026-10-13。用户最新要求：由 Codex 直接重构当前可玩场景的画面，采用 Unity 成熟方案并提高实际观感，暂不联系其他 AI。此要求覆盖先前“用户独自编辑期间不改场景”的阶段限制。本轮视觉版本已落地为声学花园，后续按用户反馈继续修改，不能自行宣称画质验收达标。入口 README.md、Docs/ART_EDITING.md、Docs/TECHNICAL_ART.md。

## 工作区与协作

用户编辑工程为 `G:/图书馆/新建文件夹 (2)/My project`，新视觉基线在独立 `art/visual-editing` 分支。`My project-integration` 是 Codex 的整合与构建工作目录。旧 art/manual-editing 与 safety/pre-visual-rebuild-20261013 保留此次重构前用户改动。不要同时打开两个工程修改同一批资源；后续用户编辑期间不自动覆盖其改动。

四位外部 AI 的协作方式保留：用户转发需求、各自独立目录和分支上传，Codex 审查接线合并。旧 TASKS_01..08、实验和交接板已从当前树移除，不能重新执行。当前暂不发新一轮内容任务；等用户完成美术修改或提出新需求。

最新 origin/main 是开发基线。不要直接推 main、强推或盲合旧分支。原 PR33 是废弃假 HUD，不合入实际游戏。旧工程本地改动已保存于 safety/local-editor-20261013（a9f7b60），清理前基线为 8ba5162；保留 Git 历史以便恢复。不要清理用户根目录中的 Blender、图包、SourceArt/Intake01、Assets/ReverbInTheRye、Art_ThirdParty 与授权来源。

## 当前真实状态

Unity 6000.4.3f1 / URP17.4，Windows PC 斜俯视幸存者。唯一主场景 Assets/Game/Scenes/Main.unity；Build Settings 只含主场景。其他三张位于 Scenes/Layouts 的场景只是有效布局草稿，不是完成关卡。当前 6攻击/14被动/3进化/4拾取、4武器槽/6被动槽、三选一与重抽/放逐/跳过。正式美术/音乐尚未完成；声音R1为用户接受的基线。

GameLoop → RunSimulation → CombatModule 唯一管理 HP/XP/时钟/随机/伤害/死亡/奖励。规则位于 Assets/Waffle/Scripts/Rules；表现位于 Assets/Game/Presentation；内容目录与工具位于 Assets/Game/Content。新美术 Prefab 不扣血、不加另一个 GameLoop/播放器，不用粒子寿命决定伤害。W01组件迁移样板仍默认关闭，W05/W06/P17已真实接线；所有派生有限、每局清空、暂停/升级冻结。

三份视听清单统一放 Assets/Game/Content/Manifests，导入为 RC02ContentCatalog.asset，运行时只消费序列化引用。目录刷新不生成/重建美术资产。唯一 AudioDirector 消费声音/音乐；音乐仍整段测试循环、无动态分轨或拍点同步。

## 修改边界

本轮按用户授权由 Codex 修改 Assets/Game/Art、Environments、Scenes 与必要表现接线，后续用户可直接编辑。现用 PBR 材质、角色、道具在 Art/VisualIdentity；新地形与麦田在 Environments/AcousticGarden。原始素材保留。画完 Terrain 道路层后执行 Reverb→更新植被遮罩；此工具只更新遮罩，不重建场景。一次性 VisualRebuild/VisualPlayCapture 工具不会保留在 Assets。移动资源保留 .meta/GUID；场景保存/组件删除由 Unity 执行，不手写 YAML。

只做必要编译、引用检查和短时运行，不扩测试套件或全面平衡/性能矩阵。已保留纯规则与战斗用例；依赖已移除实验场景的旧场景测试一起移除。美术体验由用户验收，不能靠源码/截图宣称主流画质达标。
