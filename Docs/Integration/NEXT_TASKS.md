# 下一轮派工：S2-01 首个联合视听样板

> 用户已拒绝S2首版视觉。当前A-04/P-04返工及C-03/S-04限定支持的完整提示词在[S2_VISUAL_REWORK.md](S2_VISUAL_REWORK.md)。以下S2-01派工卡仅存档，不重复开工。

2026-10-12。用户已反馈当前试玩“一切正常”，授权下一步。先读AGENTS.md、PROJECT_STATUS.md及S2_SAMPLE_CONTRACT.md。下列内容由用户转发给四位AI，没有自动发送外部消息。

新任务从包含本文件和S2_SAMPLE_CONTRACT.md的最新origin/main开独立目录/新分支，记录完整SHA。不要继续旧任务分支，不直接push main、不强推别人的分支、不自行合并。GitHub上传分支即可；能够创建PR就创建，不能创建则交分支/SHA，由Codex完成，不让用户卡在确认表单。交接必须写实际文件、验证事实和跨范围请求。

## 转发给1号：C-02，守住战斗边界

~~~text
你负责C-02规则边界收尾，帮助Codex让S2换美术时不破坏S1战斗。先fetch最新main，读取AGENTS.md、Docs/Integration/NEXT_TASKS.md及S2_SAMPLE_CONTRACT.md，开独立目录与feature/c02-combat-guards，记录完整基线SHA。

只写Assets/Waffle/Scripts/Rules/Combat/、Assets/Waffle/Tests/Combat/和Docs/Combat/。不改RunSimulation、GameLoop、Input、Views、场景、Prefab或asmdef；不要复制状态机或增加敌人/武器机制。

检查原C-01目前只有冒烟覆盖的精英、方向推进、包围圈：触发时刻/次数，合法出生位置，普通上限与事件额外数量，回收和重开后的干净实例。优先补3—5个能抓真实回归的固定seed用例；若发现缺陷，最小修复并写复现。不要把有意允许的事件额外数量误改成全部上限30，不改变默认难度/局长。

已有快速规则测试跑一次即可；不做完整设备矩阵或长时200敌压力。整理Docs/Combat/C-02_READINESS.md：测试证据、未覆盖点、表现层可依赖的精英/事件语义与现有只读字段，跨目录需求交Codex。

上传新分支和PR或分支SHA；说明实际编译/测试环境。没有Unity不宣称Unity通过。旧C-01报告与R-1/R-2已被主线整合，不能重复开发。
~~~

## 转发给2号：P-03，负责真正可玩的视听装配

~~~text
你负责P-03：把主美与音效总监的新样板用起来，做独立S2_StyleLab。先fetch最新main，读AGENTS.md、NEXT_TASKS.md、S2_SAMPLE_CONTRACT.md，开独立目录和feature/p03-style-lab。

唯一可写范围：Assets/Waffle/S2/（新表现适配、预制体、材质实例、UI、Editor工具及必要的新局部asmdef）、Assets/Waffle/Scenes/S2_StyleLab.unity及.meta、Docs/Integration/P-03_HANDOFF.md。不改正式S1_Graybox、现有Core/Input/Views、原程序集、主美/声音原始素材。P-02已经由Codex收拢，新的任务不再并行修改AudioDirector。

在新场景复用现有GameLoop/CombatViews/VfxDirector/AudioDirector与唯一事件通道。先用少量占位几何/既有音乐把装配工具搭好；主美A-03和声音S-03经Codex合入main后再fetch绑定，不能自行合其他AI分支。

完成30—60秒实际可玩样板：1玩家、1追踪敌人、1地面模块，攻击/命中、闪避、拾取、三选一及已有精英击杀强调。动作只改MotionRoot/分节等子节点，收束→错位展开→复位；不缩放判定根节点、不延迟真实攻击。用简单Mesh/Line/Particle母版和参数资产制作易复用FX，不强制引入新插件或复杂Shader。

制作一套实际三选一UI样板，使用现有GameLoop.Choose/TogglePause/Restart，不另写升级规则或输入系统；若替换RunHud须在样板禁用旧入口，避免双面板。只做样板需要的界面，不扩为完整产品UI。

提供一次运行即可生成/更新独立场景的幂等Editor入口，默认只操作自己的目录；样板不依赖手工拖引用。当前多Mesh玩家完整残影需要共享VfxDirector适配，由Codex负责：请给出节点结构、复现和所需接口，不复制第二个残影管理器。其它共享缺口也写入交接。

完成必要编译/暂停/升级/重开检查，上传分支和PR/SHA。没有Unity就交可运行生成工具并明确未验证，Codex导入、生成meta和自动检查。标清占位、概念与实际渲染，不让用户补环境或跑测试。
~~~

## 转发给主美：A-03，交可直接用的雕塑素材

~~~text
你负责A-03首套可导入的“会共振的雕塑”风格包。用户的方向已明确：抽象几何、偏心开缝/分节、干净色块、精致克制PBR、冷静精密/战斗爆发。工业机甲只是S1占位，本轮不用再让用户在机甲与抽象雕塑之间重复选择。

fetch最新main，读AGENTS.md、NEXT_TASKS.md、S2_SAMPLE_CONTRACT.md；开独立目录与feature/a03-sculpture-kit。只写Assets/Art/S2/（含自己的源文件、模型、材质、Prefab或Editor生成工具）和Docs/Art/A-03_STYLEKIT.md；不改共享场景、运行脚本和正式材质。

先交1个玩家、1个追踪敌人、1个地面模块与3族材质母版：哑光陶瓷/涂层、少量半哑光金属、局部信息发光。用轮廓/倒角/留白体现精细，避免军工纹理、满屏发光与油亮塑料。1单位=1米、+Z前、脚底轴心、根比例1；玩家约1.8米，敌人约1.3—1.5米，节点和分节按契约说明供2号驱动。

必须交可导入FBX/OBJ/Prefab，或一键生成持久Mesh/Prefab的Unity Editor工具，保留源文件；不能只交效果图、材质参数表或“已完成”报告。几何工具生成的倒角/法线须适合实际俯视光照，不能用未经验证的超大缩放。

同时给出一张升级卡的布局/切分比例及普通命中、闪避、精英击杀三类动作幅度/复位建议，供2号统一实现。不批量制作更多角色/场景。色彩按契约做第一版提案，实际效果由用户看样板决定。

交接列实际文件、导入方式、材质/节点、来源、待验证项。A-02旧遮罩仍属独立参考，未证明UV对位就不能说已可直接装配；截图标明概念或实际引擎。上传新分支和PR/SHA，没有Unity如实说明，Codex完成导入与自动化。
~~~

## 转发给音效总监：S-03，拿真实声音建立辨识度

~~~text
你负责S-03雕塑视听样板的声音。fetch最新main，读AGENTS.md、NEXT_TASKS.md、S2_SAMPLE_CONTRACT.md与Docs/Verification/Handoffs-2026-10-12/REVIEW.md；开独立目录和feature/s03-sonic-kit。只写Assets/Audio/S03/与Docs/Audio/，不改AudioDirector、场景、Mixer或他人的素材。

给最多2个20—30秒可循环音乐小样：A更突出精密清澈拨弦/错位重音，B更突出后摇空间发展/克制电子骨架。共同服务冷静精密→短时爆发，不做两套无关主题；不引入随音乐等待输入的节奏玩法。交能直接试听的音频，说明新增器乐/节奏结构；旧氛围曲裁短或改名不能代替新风格小样。S-02的32秒版本保留参考，但Codex测得它与现有64秒曲前32秒相关性很高，不能仅凭文件名确认数学摇滚特征。

再交6类同源小音色：Shot、EnemyHit、Dodge、PlayerHurt、GemPickup、LevelUpChosen。短干陶瓷/材料敲击、有限金属拨动共鸣、电子微脉冲；受击要与攻击区分，普通命中不每次变随机旋律。其余事件先给复用/后续替换建议，不批量生产多套变体。

提供WAV母带、事件映射、调性/拍速（确实可测时才写）、循环点、尾音、音量建议及源文件/生成或加工方法。样点峰值与True Peak分开记录；没有过采样测量不能把sample peak写成true peak。没有真实监听、游戏录音或测试文件就标待验证，不写50次试听全部通过或数小时不疲劳。

核对S-02来源表：实际源文件/下载页/作者/许可证、加工链与必要署名；保留许可页或原始说明证据。来源未核实的项明确标待核验，不写绝对授权或法律合规结论。优先自制/可追溯素材。模型材质与音色说明使用共同的分节/材料语言。

上传新分支和PR/SHA，交实际音频路径、客观测量与未验证项。Codex导入/自动检查并提供可玩的联合样板，用户只做必要的短试听/观感验收。
~~~

## Codex负责的难点

收拢本轮四位旧分支并保留原提交；修复并验证P-02生命周期边界；把事实与作者自述区分。下一轮独占共享Core/Input/Views、正式S1、共享Editor/程序集与任务板；根据2号真实模型结构补齐多Mesh残影、跨目录接线与资源引用。先合主美/声音独立素材，再整合2号装配和1号局部修正，做必要自动检查并交一个样板包，不让用户处理冲突或工具流程。
