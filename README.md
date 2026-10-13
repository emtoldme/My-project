# 麦田里的混响 · Reverb in the Rye

Unity 6000.4.3f1 / URP17.4 的 3D 幸存者原型。当前有 6 攻击、14 被动、3 进化、4 类拾取。

**主场景：`Assets/Game/Scenes/Main.unity`**。打开工程后使用菜单 **Reverb → 美术工作台**；从工作台打开主场景、角色、环境、材质与特效。

当前重点已转为画面与氛围。视觉R2以“风雨中的失谐声场”为方向：有缺口的共鸣弧、调谐柱、磨损信号旗、声纹投光与双层残响贯穿建筑、光影和特效；**F8切换观景**。见 [视觉语言与版本说明](Docs/RESONANCE_R2.md)。幸存者玩法保留为基底，当前不继续扩展。此前的透视、风化、麦浪与踩水说明保留在 [氛围R1](Docs/ATMOSPHERE_R1.md)。

- 美术修改：Docs/ART_EDITING.md
- 当前渲染方案与生成图集：Docs/TECHNICAL_ART.md
- 当前状态：Docs/PROJECT_STATUS.md
- 视听目录格式：Docs/Architecture/ContentPipeline.md
- 规则组件接口：Docs/Architecture/Components.md
- 原始素材及来源：Assets/ReverbInTheRye、Assets/Art_ThirdParty、SourceArt/Intake01

WASD 移动、自动攻击、Space 闪避、Esc 暂停、F11 切换独立包全屏。Build Settings 已只保留主场景。Layouts 是另外三图的布局草稿。

动态光影与天气版：默认雨天，**F6 切换雨天 / 雷雨 / 晴天**，包含扫描灯软阴影、风雨、水花、积水涟漪与湿润PBR材质。通过 **Reverb → 天气** 调整，说明见 `Docs/WEATHER_LIGHTING.md`。

玩法第一轮已整合到Main：冲锋/落波敌人、两次可选调谐、9分钟终局首领、危险预警和实际结算已开启。见 [整合说明](Docs/Project/GameplayNext/INTEGRATION.md)。接口与后续分工见 [实际API](Docs/Project/GameplayNext/GP00_API.md) 和 [任务规划](Docs/Project/GameplayNext/README.md)。

旧实验、过期派工与淘汰测试素材已移除，历史可恢复。当前主场景为声学花园视觉版：共享麦穗图集、混合地形、陶瓷机甲、悬吊声学设施、技能光晕和成长卡片。画面由实际试玩验收；另外三图仍是布局草稿。目录刷新不会重建模型、地形或灯光，手工画完道路后可从工作台更新植被遮罩。
