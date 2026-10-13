# 自己修改美术

使用 `G:/图书馆/新建文件夹 (2)/My project` 工程，Unity 版本 6000.4.3f1。打开 **Reverb → 美术工作台 → 打开主场景**，或双击 `Assets/Game/Scenes/Main.unity`。按 Play 可直接体验当前完整玩法。

停止 Play 后再改并保存。运行中生成的敌人、子弹和特效是临时实例；要长期保存效果，请改它们的 Prefab、材质或场景上的绑定。当前分支 `art/manual-editing` 专门留给你，不需要回到早期主美交付分支。

| 想改什么 | 入口与方法 |
|---|---|
| 主角 | 工作台“选择玩家外观”，修改 Player 下的 Visual；复用外观在 Art/Actors/Player_George_Visual.prefab。保留 Player 移动根及 Visual 层级；替换模型放在 Visual 内，脚底对齐地面。 |
| 敌人 | Art/Actors/Enemy_Vanguard_Mike_Visual.prefab；GameLoop 上 CombatViews.enemyVisualPrefab 是实际绑定。可换成自己的 Prefab。normalizeEnemyVisuals 开启时自动归一到约1.5米；已有米制尺寸可关闭。tintEnemyMaterials 关闭时保留原材质。 |
| 子弹与经验宝石 | GameLoop → CombatViews 的 projectileVisualPrefab/gemVisualPrefab 与尺寸字段。未绑定时沿用现有简单形体；绑定 Prefab 后保留其自身材质。模型不需要碰撞或伤害组件。 |
| 环绕物与道具 | GameLoop → RC01BattleView 的 orbitVisualPrefab/pickupVisualPrefab、orbitSurface/pickupSurface。关闭 tintPickups 可保留自定义道具材质；开启时按功能上色。 |
| 地面与植被 | Game/Environments/P_RC01_HarvestRoad.prefab 中的 Ground/Foliage。TerrainData 在 Environments/Terrain，TerrainLayer 与原贴图在 ReverbInTheRye/Ground。要画地面层，在主场景选 Terrain 的 Paint Texture；材质和植被 Prefab 可直接编辑。 |
| 地面延伸 | 主场景 RC01_GroundContinuation 的材质在 Game/Art/Materials/M_RC01_HarvestRoad_Ground.mat。它和 Terrain 是两个表面，改地面整体风格时两处一起检查。 |
| 灯光、相机、环境色 | 主场景 Key Light、Main Camera；Window → Rendering → Lighting。现有 URP/Volume 在 Assets/Settings。停止运行修改并保存场景。 |
| 技能特效 | Game/Art/Effects/Prefabs 与 Materials。模板 Color 在 RC02VisualTemplate，宽度/强度/尾长等在清单中；真实半径由规则事件传入。可改几何、材质与形体，保留模板/池复用所需组件，不添加伤害逻辑。 |
| 升级图标 | Game/Art/Effects/Icons，替换对应 PNG 或在 art-manifest.json 改路径。布局代码为 Presentation/Gameplay/RC01GrowthHud.cs，当前是功能界面。 |
| 声音与音乐 | Game/Audio；绑定在 Game/Content/Manifests/audio-manifest.json、music-manifest.json，仍由唯一 AudioDirector 播放。 |

同路径修改 Prefab、模型或材质，可直接停止再 Play 看效果。如果更改了映射清单里的路径、参数或预算，执行 **Reverb → 刷新视听目录**。清单是这些目录绑定的来源，直接改 RC02ContentCatalog.asset 的内容会在下次刷新时覆盖。

特效模板根保持单位缩放、+Z朝前；尺寸改子物体或清单参数。移动残留与连线显示真实规则点，不能通过拉大装饰形体来改伤害范围。替换模型无需改纯规则代码。

移动/重命名资产请在 Unity Project 窗口操作，让 .meta/GUID 跟随。不要从资源管理器只拷贝 .fbx 而丢掉已有 .meta。新角色可以放 Art/Actors 内；现有第三方模型及其授权来源保留。

本次清理没有生成新的美术，也没有更改已有战斗数值。主场景里的三选一、闪避、磁吸和声音继续可用。旧场景重建器已删除，不会在你改完之后重新覆盖成旧样板。需要独立包时执行 **Reverb → 构建当前游戏**。
