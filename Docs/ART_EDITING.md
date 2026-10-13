# 自己修改美术

使用 `G:/图书馆/新建文件夹 (2)/My project` 工程，Unity 版本 6000.4.3f1。打开 **Reverb → 美术工作台 → 打开主场景**，或双击 `Assets/Game/Scenes/Main.unity`。按 Play 可直接体验当前完整玩法。

停止 Play 后再改并保存。运行中生成的敌人、子弹和特效是临时实例；要长期保存效果，请改它们的 Prefab、材质或场景上的绑定。当前新视觉分支为 `art/visual-editing`；旧 `art/manual-editing` 和 safety/pre-visual-rebuild-20261013 保留重构前的本地改动。

| 想改什么 | 入口与方法 |
|---|---|
| 主角 | 工作台“选择玩家外观”，修改 Player/Visual；外观源为 Art/VisualIdentity/Prefabs/P_Player_Ceramic.prefab，材质 Player_Ceramic.mat。保留 Player 移动根与 Visual，替换模型脚底对齐地面，当前身高1.85米。 |
| 敌人 | Art/VisualIdentity/Prefabs/P_Enemy_Ceramic.prefab；GameLoop/CombatViews.enemyVisualPrefab 为实际绑定。当前米制身高1.5米，normalizeEnemyVisuals 与 tintEnemyMaterials 关闭。材质 Enemy_Coral.mat。 |
| 子弹与经验宝石 | GameLoop → CombatViews 的 projectileVisualPrefab/gemVisualPrefab 与尺寸字段。未绑定时沿用现有简单形体；绑定 Prefab 后保留其自身材质。模型不需要碰撞或伤害组件。 |
| 环绕物与道具 | GameLoop → RC01BattleView 的 orbitVisualPrefab/pickupVisualPrefab、orbitSurface/pickupSurface。关闭 tintPickups 可保留自定义道具材质；开启时按功能上色。 |
| 地面与道路 | 主场景 Acoustic Garden/Garden Terrain，地形数据 Environments/AcousticGarden/Terrain/AcousticGarden.asset。用 Terrain/Paint Texture 画三层：TrampledPath道路、DrySoil底土、DenseStraw秸秆。道路层是第0层，画完点击工作台“更新植被遮罩”。旧主场景的延伸平面已移除。 |
| 麦田 | Acoustic Garden/Meadow · wind batches 为共享片区实例，可直接移动或移除局部片区。4个共享网格位于 Environments/AcousticGarden/Foliage，图集在 Art/VisualIdentity/Textures/Rye_Atlas_RGBA.png。材质 Meadow_Gold.mat 可调色、风动、透明裁切和 Clearing 留白中心/半径。GroundMask 从 Terrain 道路层刷新。 |
| 声学设施 | Acoustic Garden/Acoustic stations；原型 Prefab 为 Environments/AcousticGarden/Prefabs/P_AcousticResonator.prefab。Suspended acoustic canopy 为场景内悬吊设施，可编辑子模型与材质；最低部位在玩家上方约3米，支撑柱放在可玩边界外。校准台面是可步行的薄嵌板。 |
| 灯光、相机、环境色 | 主场景 Key Light、Main Camera、Garden · color and atmosphere；调色在 Art/VisualIdentity/Lighting/Garden_Volume.asset，反射在同目录。相机后期已开启，URP PC资产仍在 Assets/Settings。停止运行修改并保存。 |
| 天气与动态光影 | Reverb → 天气 → 选择天气控制器，修改 Garden Weather · rain and light；Play 后 F6 切换晴天、雨天和雷雨。雨量、湿润度、阵风、云影、粒子预算和闪电均可调整。积水与雨材质在 Environments/AcousticGarden/Weather，详见 WEATHER_LIGHTING.md。 |
| 技能特效 | Game/Art/Effects/Prefabs 与 Materials。模板 Color 在 RC02VisualTemplate，宽度/强度/尾长等在清单中；真实半径由规则事件传入。可改几何、材质与形体，保留模板/池复用所需组件，不添加伤害逻辑。 |
| 升级图标与界面 | Game/Art/Effects/Icons，替换 PNG 或修改 art-manifest.json。当前深色半透明 HUD、生命条、装备栏和三张升级卡由 Presentation/Gameplay/RC01GrowthHud.cs 绘制，读取真实局内数据。 |
| 声音与音乐 | Game/Audio；绑定在 Game/Content/Manifests/audio-manifest.json、music-manifest.json，仍由唯一 AudioDirector 播放。 |

同路径修改 Prefab、模型或材质，可直接停止再 Play 看效果。如果更改了映射清单里的路径、参数或预算，执行 **Reverb → 刷新视听目录**。清单是这些目录绑定的来源，直接改 RC02ContentCatalog.asset 的内容会在下次刷新时覆盖。

特效模板根保持单位缩放、+Z朝前；尺寸改子物体或清单参数。移动残留与连线显示真实规则点，不能通过拉大装饰形体来改伤害范围。替换模型无需改纯规则代码。

移动/重命名资产请在 Unity Project 窗口操作，让 .meta/GUID 跟随。不要从资源管理器只拷贝 .fbx 而丢掉已有 .meta。新角色可以放 Art/Actors 内；现有第三方模型及其授权来源保留。

本轮只重构当前可玩主场景的视觉与表现，不更改战斗数值。三选一、闪避、磁吸和声音继续可用；另外三张地图仍是布局草稿。一次性重建与采图脚本已从 Assets 移除。需要独立包时执行 **Reverb → 构建当前游戏**。渲染方案与生成素材来源见 [技术美术说明](TECHNICAL_ART.md)。
