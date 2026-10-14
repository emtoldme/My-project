# 麦田里的混响 · Reverb in the Rye

Unity 6000.4.3f1 / URP17.4 的 3D 幸存者原型。当前有 6 攻击、14 被动、3 进化、4 类拾取。

**HC02旧物共鸣**：针线盒/手套/吉他组成互斥的“回针、牵弦、归巢”；真实拾取产生碎音，可用于战术行动或带回旧地返灯。J日志新增共鸣页。见[组合与循环](Docs/Project/HomecomingNext/RESONANCE_LOOPS_HC02.md)。

**归乡探索HC01已接入Main**：老房子、老筒仓、旧吉他可探索并收取，额外遗物槽2→4，实际能力、叙事日志、局外经验/修补升级与本地档案可用。出发前先整理行囊，**J打开日志**，靠近自动拾取。见[整合与试玩说明](Docs/Project/HomecomingNext/INTEGRATION.md)。原始任务与`hc00-v1`基线见[分包记录](Docs/Project/HomecomingNext/README.md)。

**主场景：`Assets/Game/Scenes/Main.unity`**。打开工程后使用菜单 **Reverb → 美术工作台**；从工作台打开主场景、角色、环境、材质与特效。

当前重点为画面与氛围。视觉R3把《秋收之路》调整为**尚有金色余温的故乡**：白青玩家、暖橙敌人、有实体来源的农机灯与木杆路灯，闲置广播结构退到外围；新增废弃农机、旧秋千、田间空地与麦茬，玩家和附近掉落物周围压低麦秆。见 [视觉R3说明](Docs/HOMECOMING_R3.md)。**F8切换观景**，幸存者玩法保留为基底。声学形状与残响沿用 [视觉R2](Docs/RESONANCE_R2.md)，透视、风化与踩水见 [氛围R1](Docs/ATMOSPHERE_R1.md)。

**探索R4已解除Main的固定方形空气墙**：麦田、弯曲道路、收割区、空地与零散地标随探索程序生成，默认驻留49个24米地块并循环回收。每次进场可生成新种子，敌人和攻击在远处仍能工作。见 [探索地图说明](Docs/EXPLORATION_R4.md)。

- 美术修改：Docs/ART_EDITING.md
- 当前渲染方案与生成图集：Docs/TECHNICAL_ART.md
- 当前状态：Docs/PROJECT_STATUS.md
- 视听目录格式：Docs/Architecture/ContentPipeline.md
- 规则组件接口：Docs/Architecture/Components.md
- 原始素材及来源：Assets/ReverbInTheRye、Assets/Art_ThirdParty、SourceArt/Intake01

WASD 移动、自动攻击、Space 闪避、Esc 暂停、F11 切换独立包全屏。Build Settings 已只保留主场景。Layouts 是另外三图的布局草稿。

动态光影与天气版：当前默认晴朗黄昏，**F6 切换晴天 / 雨天 / 雷雨**，保留风雨、水花、积水涟漪与湿润PBR。冷色广播灯按真实调谐和首领状态激活。通过 **Reverb → 天气** 调整，早期天气实现见 `Docs/WEATHER_LIGHTING.md`，当前状态以R3说明为准。

玩法第一轮已整合到Main：冲锋/落波敌人、两次可选调谐、9分钟终局首领、危险预警和实际结算已开启。见 [整合说明](Docs/Project/GameplayNext/INTEGRATION.md)。接口与后续分工见 [实际API](Docs/Project/GameplayNext/GP00_API.md) 和 [任务规划](Docs/Project/GameplayNext/README.md)。

旧实验、过期派工与淘汰测试素材已移除，历史可恢复。当前主场景为声学花园视觉版：共享麦穗图集、混合地形、陶瓷机甲、悬吊声学设施、技能光晕和成长卡片。画面由实际试玩验收；另外三图仍是布局草稿。目录刷新不会重建模型、地形或灯光，手工画完道路后可从工作台更新植被遮罩。
