# 麦田里的混响 · Reverb in the Rye

下一轮玩法开发：[总规划与四份任务卡](Docs/Project/GameplayNext/README.md)。当前为待派发计划，包含敌人/遭遇、地图目标、终局首领、玩法反馈及统一接线约定。

Unity 6000.4.3f1 / URP17.4 的 3D 幸存者原型。当前有 6 攻击、14 被动、3 进化、4 类拾取。

**主场景：`Assets/Game/Scenes/Main.unity`**。打开工程后使用菜单 **Reverb → 美术工作台**；从工作台打开主场景、角色、环境、材质与特效。

- 美术修改：Docs/ART_EDITING.md
- 当前渲染方案与生成图集：Docs/TECHNICAL_ART.md
- 当前状态：Docs/PROJECT_STATUS.md
- 视听目录格式：Docs/Architecture/ContentPipeline.md
- 规则组件接口：Docs/Architecture/Components.md
- 原始素材及来源：Assets/ReverbInTheRye、Assets/Art_ThirdParty、SourceArt/Intake01

WASD 移动、自动攻击、Space 闪避、Esc 暂停、F11 切换独立包全屏。Build Settings 已只保留主场景。Layouts 是另外三图的布局草稿。

旧实验、过期派工与淘汰测试素材已移除，历史可恢复。当前主场景为声学花园视觉版：共享麦穗图集、混合地形、陶瓷机甲、悬吊声学设施、技能光晕和成长卡片。画面由实际试玩验收；另外三图仍是布局草稿。目录刷新不会重建模型、地形或灯光，手工画完道路后可从工作台更新植被遮罩。
