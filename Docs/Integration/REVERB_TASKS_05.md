# 首批美术接入：让素材形成可验收的麦田战场

2026-10-12。用户已提供地面、立体麦田、旧式调频器和声波VFX。继续原四位AI分担，由用户转发下面各段；Codex负责GitHub与本地整合。首批清单、候选性质和检查证据见Docs/Verification/Art-Intake-01/REVIEW.md。

TASKS_04规则/表现/短音已提交，Codex接管这些已交版本的整合。四位不要继续同时改旧交付文件；发现缺陷先提最小请求。本轮从包含本卡和素材的最新origin/main独立开分支，记录完整基线SHA，不互相合并、不push main、不升级依赖。无Unity如实声明，Editor工具可源码交付，由Codex运行并生成meta/场景。提交PR或明确分支与SHA。

本轮成果是一个小型麦田环境样板和明确的接线审查；保留当前机制数值/四模式/闪避。角色.blend仍是WIP，不能重新用方块拼成人物冒充正式角色完成。

## 转给1号

~~~text
执行Docs/Integration/REVERB_TASKS_05.md的1号任务，分支review/rf01-rule-guards。你已经完成的feature/m01-echo-cues由Codex整合，不再重复实现。此次仅写Docs/Experiments/M01/ART_INTAKE_RULE_GUARDS.md。

结合现有唯一CombatModule、已交CopyPendingEchoes接口和新Assets/ReverbInTheRye/VFX/Scripts/SonicAttackVFX.cs，给Codex一张可执行的接线检查表：真实Shot/直接命中/派生命中/死亡各从哪里读取；回声原目标提前死亡或代号复用如何撤标；一个目标多条回声如何保留最早到期预备；暂停、重开、终局如何清理。明确哪些已有规则保证，哪些必须由适配层保证。新粒子不能持HP、自己碰撞/扣血，也不能由视觉计时器触发下一次伤害。

每个发现提供实际文件/方法及最小修复建议，无发现就明确说明；不为了交付新增第二套模拟器、大量镜像测试或性能认证。不改规则/Integration/Presentation/用户素材。现有必要规则修复由Codex审查后单独派回。
~~~

## 转给2号

~~~text
执行Docs/Integration/REVERB_TASKS_05.md的2号任务，分支feature/rf01-field-environment。只写Assets/Experiments/RyeField/及Docs/Experiments/RF01/。你已交的M01-R1表现冻结，由Codex接线。本轮不得改M01、Assets/ReverbInTheRye、SourceArt、Packages、默认BuildSettings或共享管理器。

使用仓库已导入的Ground三种材质、Foliage四种Prefab和OldSonicModulator武器Prefab，制作独立RF01环境样板。源码入口Reverb.Experiments.RyeField.EditorTools.RyeFieldSetup.CreatePreview；生成本目录Prefabs/P_RF01_Environment.prefab和Scenes/RF01_EnvironmentPreview.unity。主环境根节点命名RF01_Environment；环境Prefab只含地表/植被，不含相机、灯、敌人、GameLoop或音频，方便Codex挂到M01。场景可另放相机、单一主光及供检查的武器展示，禁止复制战斗框架。无Unity不要手写unity/prefab YAML，只交幂等Editor工具与说明。

采用现有实际游戏镜头：正交Size9、Pitch55/Yaw45。先装配一小块约24m×24m样板，按米检查贴图尺度；三种地表要有明确路径与过渡，不能三张大矩形硬拼，也不能靠浓雾/Bloom遮接缝。可制作本目录可调材质/简单混合Shader，复用用户贴图；不新生成正式贴图、不引入新插件。中心约6m半径清出高麦丛，中心仅极少矮草/麦茬；外围麦浪有密度疏密与方向变化，固定seed20261012且有明确实例上限（先≤300），不创造数千个GameObject。此配置是首轮样板建议，不是正式地图约束。

注意麦穗LOD1比LOD0矮约12%～14%、轮廓也不同；本轮可在样板强制LOD0，并记录后续LOD修复请求，不擅自重塑用户模型。植被不加阻挡碰撞。调频器用于展示原始尺寸/材质，不自行缩成挂件或猜手骨挂点。角色WIP不自动导入，不更改原模型/骨架/动画。

交清楚的用法、实际验证/未验证和PR。评判是实际俯视镜头下地表细节、麦浪层次、通行留白与战斗辨识；未运行不要写画面已达标。Codex负责Unity执行、M01挂载、声波时钟/事件和Windows试玩包。
~~~

## 转给主美

~~~text
执行Docs/Integration/REVERB_TASKS_05.md的主美任务，分支review/rf01-art-direction。仅写Docs/Art/Reverb/。阅读首批入库清单、SourceArt/Intake01原始说明及Art-Intake-01实际导入截图。纠正A-R02/03/04中超过证据的表述：零金属度不等于绝无塑料感，贴图均值不能证明预警可见度100%，源码有instancing关键字不等于批处理/零GC实测，当前Sonic VFX是懒创建池而非开局全量预热，也没有真正场景颜色折射。

交RF01_FIRST_ART_REVIEW.md，以“已观察/推断/待实机”区分：地面烘焙光影/重复、麦穗稀疏程度和LOD轮廓、调频器材质层次与俯视识别、紫红直接命中与回声是否混淆。给2号不超过5项按优先级的具体建议；已有素材允许落地，不再索要全部正式美术。角色块面稿/骨架稿仍是WIP，列出距可导入角色尚缺的具体信息，禁止假报角色品质达标。

若RF01场景还没产出，先完成资源级审查并明确等待实机，之后依据Codex截图更新结论。只做本轮小样板的审查，不重定游戏世界观、重设计服装或生成正式素材；用户负责最终气质/质感验收。
~~~

## 转给音效总监

~~~text
执行Docs/Integration/REVERB_TASKS_05.md的音效任务，分支review/rf01-event-sound-map。已交feature/m01-audio-cues短音由Codex接入，请冻结该版本。本轮只写Docs/Experiments/M01-Audio/RF01_EVENT_SOUND_MAP.md，不改共享AudioDirector、场景、VFX或旧音频文件。

按新旧式声波调频器与SonicAttackVFX的PlayFire/PlayHit/PlayDeath，以及M01真实Echo/Propagation/EchoPropagation事件，提交简短声画映射表：事件来源、已有短音路径、建议音高/增益/并发、主次与避免重复播放的办法。直接命中和派生命中不能因为同时跑两套反馈而重复播；死亡不可因回池/重开再响。无成功伤害不响假回声，不另设计时器决定命中；保留暂停和重开行为。

沿用现有TEST_ONLY样本及已提交短音，不新做BGM、不加音乐插件。数学摇滚/后摇/电子是整体音色方向，不能把新增美术视作已有正式配乐。区分源数据测量和实际监听；不给未经测量的无爆音/性能结论。列出最多3项需要用户实际听的AB问题，由Codex合成可玩的整合包再验收。
~~~

## Codex收口与用户验收

Codex负责新素材GUID/导入参数、M01-R1已交代码/音频接线、新VFX的粒子和Shader时钟、有限预热与暂停/清理、事件不叠播、场景挂载与必要自动检查。第一步资源导入通过不代表此链路已经完成。

外部交付后构建一个独立试玩包，验收分两步：先看地表/麦浪/武器是否形成统一、有细节的场景；再玩组合模式，判断预备→原目标回声→传播去向是否看懂。两步都由用户给感受，不重复索要已确认的组合乐趣或闪避方向。未得到用户验收不得写“主流品质已达标”。
