# 下一阶段：回声＋传播机制试玩

2026-10-12。用户已明确选择独立机制试玩，正式美术仍后置。本阶段把已有E-01/E-02成果连接起来，验证组合是否有趣，不新增第四个大型实验、不制作正式关卡或替换默认S1/S2。以下提示词由用户分别转发，未自动发外部消息。

先读AGENTS.md、Docs/Verification/Reverb-Followup-02/REVIEW.md及本卡。从最新origin/main记录完整SHA，各用独立工作目录/分支。不得合并他人分支、直接push main或升级依赖。交PR、实际验证和跨目录请求；Codex负责共享接线与自动化，无Unity就明确写未运行，不把检查交给用户。

## 共同边界

- 试玩保持WASD移动、自动射击、Space闪避、Esc暂停；无模块/回声/传播/组合四组，每次切换重开整局，同seed比较。暂不接升级/存档/局外配置。
- 测试参数沿E-02：直接伤害沿真实武器，不再扣一次；回声为初始直接伤害的0.5倍、延迟0.5秒；所有传播均为该触发初始直接伤害的0.4倍，回声传播也不再乘0.5，延迟0.1秒、XZ平面5米。基础伤害10且目标都存活时对照10/15/14/23；是机制对照，不是平衡承诺。
- 公共合同位于Assets/Experiments/Mechanism/Contracts/，使用时在自己的asmdef显式引用Reverb.Experiments.Mechanism.Contracts；命名空间Reverb.Experiments.Mechanism。IsReady=false时UI不允许重开请求，不得复制一份同名接口。
- 每次直接命中最多一次回声、一次直接传播及一次回声传播；传播不再次生回声/传播。只对合法活目标伤害；目标Serial+Generation校验，近距同距用Serial决定。传播不得再命中原目标或同触发已传播目标。回声目标失效就跳过，不能误伤重用池对象。
- 直接命中来源在CombatModule T6规则路径收集；到期派生伤害在T7死亡结算前应用。伤害/死亡/掉落由唯一CombatModule负责，不在PresentationDispatched回写，也不让实验夹具拥有第二套正式HP。
- 暂停/升级/终局停止时钟与派生处理；重开销毁局内队列、预算和计数。模式选择是下一局配置，不是static局内状态。
- TEST_ONLY表现必须区分直接命中、延迟回声和目标间传播；UI显示当前模式、成功回声/传播次数与待处理数，实际未接线就显示等待接线，不编造数值。

## 转给1号：机制规则适配

~~~text
下一阶段为用户已选择的“回声＋传播”独立试玩。先读REVERB_TASKS_03.md和现有CombatModule，保持纯C#规则职责。新分支feature/m01-mechanism-rules，只写Assets/Experiments/Mechanism/Rules/、Docs/Experiments/M01/。不要改正式CombatModule/RunSimulation/GameLoop，也不要改共享Contracts或2号目录。

用本卡共同边界把E-02原理做成小型2D适配模块，不照搬一维ComboRun的HP副本。模块每局独立，配置开局拷贝；收到已发生的真实直接命中（运行时间、目标Serial/Generation、命中位置、实际伤害）才排队，不再次扣直接伤害。通过最小宿主接口查活目标/位置及同步申请派生伤害，宿主由Codex桥接唯一CombatModule，确保伤害在规则步骤内发生而非表现回调。宿主返回未命中/目标失效时不得继续产生其回声传播。方法命名和数据结构由你给出清晰README，Codex按实际代码适配，不索要已有字段。

派生时间锚定父计划时间；同Tick过多个到期点须稳定按时间/序号结算，应用一次后再选下一目标，不能把整批预计算后重复击杀。预算完成即回收，输出事件缓冲按Tick清空，不保留无限日志。使用共享MechanismMode，None不得产生任何派生。少量检查覆盖四组、二维同距/半径、池代号失效、同帧重复死亡、粗细dt和局间清理。提交接口说明及给Codex的最小接线需求，不造通用装备图或编辑器。
~~~

## 转给2号：独立试玩场景与操作面板

~~~text
下一阶段为机制试玩。新分支feature/m01-playtest-scene，只写Assets/Experiments/Mechanism/Presentation/、Assets/Experiments/Mechanism/Editor/、Assets/Experiments/Mechanism/Scenes/、Docs/Experiments/M01-View/。不改1号Rules、共享Contracts/Integration、正式Core/Input/Views或现有E-01资源。

复用E-01的实际模型、材质、相机和唯一GameLoop/CombatViews，创建M01_MechanismPlaytest独立场景与可重建的Editor装配入口，不接默认BuildSettings。不能调用E01.Setup去覆盖E01场景；可以读其装配代码、复用已提交的资产。只保留一套战斗/音效/特效管理器。

做最小TEST_ONLY面板：无模块/回声/传播/组合四按钮，调用共享IMechanismPlaytestControls.RestartWithMode；读取Mode、EchoHits、PropagationHits、PendingCount。挂载源先验证接口，尚无Codex适配组件时明确“等待机制接线”、禁用按钮，不自己实现伤害或第二个规则时钟。不让文本输入吞移动键的问题回归。

场景用相同seed和低数量敌群，给自动攻击足够机会展示延迟命中与近邻传播，不铺200敌或新环境。表现只读：为后续机制事件准备清晰的回声环和短传播连线池，等待Codex提供事件接线；不把普通EnemyHit猜成回声，也不与E01波纹重复播放。提交缺少的事件字段及接线请求，不擅自生成正式艺术。
~~~

## 转给主美：机制辨识审查

~~~text
新分支review/m01-visual-readability，仅写Docs/Art/Reverb/。基于现有真实截图和REVERB_TASKS_03.md，给一页TEST_ONLY辨识检查表：俯视下直接命中/0.5秒回声/目标间传播各靠什么形状和时间差区分，敌人危险提示如何保持可见。颜色/HDR/尺寸只是可调建议，不当用户批准常数；不要求正式素材，不制作整套UI/环境。尚无M01截图就明确待实机审查，不虚写通过。供2号和Codex在场景实际接线后对照。
~~~

## 转给音效总监：机制声响应接线表

~~~text
新分支review/m01-audio-response，仅写Docs/Experiments/M01-Audio/。本轮只给直接命中/延迟回声/传播/击杀的最小声响应接线表，复用已有合法TEST_ONLY SFX或指定“暂无”。说明短时间连发限频/并发上限和回声相对直接命中的时间关系；声音只跟随已结算事件，不能另设时钟决定伤害。V2双轨仍只用于同步测试，不宣称正式数学摇滚/后摇/电子配乐。不要改DSP控制器/场景、不新增正式音频、不要求用户素材；交可执行的字段与接线建议，Codex处理共享AudioDirector接入。
~~~

## Codex负责与用户验收

Codex提供共享Mode/UI合同，审查1号宿主接口后做最小可选规则桥，默认关闭、只在M01启用；合并2号场景并接真实派生事件、计数与声音，自动跑必要命中/暂停/重开检查，再构建独立Windows试玩。各AI无需相互合并或改共享入口。

用户最终体验同seed四组，判断回声是否可感知、传播是否可读、组合是否有趣、操作是否顺畅。届时提交可玩版本与简短验收步骤；现在的技术证据不等于该阶段已经验收。
