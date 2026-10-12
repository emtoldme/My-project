# M01-R1：让回声的作用看得懂

2026-10-12。用户已试玩：**组合明显更有趣、会改变玩法；声波作用不明显；闪避手感可以；不能全屏。** 本轮保留伤害、延迟、距离、闪避和四模式对照，集中修正机制反馈。全屏由 Codex 修复并交独立包；下面四项继续分担给原四位 AI，由用户转发。

从包含本卡和全屏修复的最新 origin/main 创建各自独立分支，先 fetch 并记录完整 SHA。遵循 AGENTS.md；不要互相合并、直接 push main、改依赖或复制 CombatModule。没有 Unity 就如实声明，由 Codex 导入、接线、自动验证和构建。新资源仅为 TEST_ONLY，正式素材/配乐继续后置。本卡没有要求新敌人、装备、关卡或重做闪避。

## 转给1号：只读回声预备信息

~~~text
执行 Docs/Integration/REVERB_TASKS_04.md 的1号部分。用户已确认组合有趣，但看不出声波的作用。分支 feature/m01-echo-cues；只写 Assets/Experiments/Mechanism/Rules/ 和 Docs/Experiments/M01/，不改 Contracts/Integration、CombatModule、Presentation 或场景。

在现有 MechanismRules 增加纯C#只读查询：
public void CopyPendingEchoes(List<PendingEchoInfo> destination)
以及 readonly struct PendingEchoInfo，公开 readonly MechanismTarget Target、double ScheduledAt、double DueAt，构造参数顺序相同。

调用时先清空调用者传入的复用List，按现有待处理顺序复制真正排队中的 DerivedKind.Echo；跳过宿主已判失效/死亡的原目标。ScheduledAt 为该回声的直接命中时间，DueAt 为原计划触发时间。没有预备信息时返回空列表。查询不得推进Tick、移除队列、修改计数、改变目标选择或申请任何伤害，不用独立时钟，也不保留无限历史。不把传播混作回声预备。多个回声允许同一目标多条记录，由Codex在视图接线时合并成一个最早到期标记。

保留现在的10/15/14/23、0.5s回声、0.1s传播、5m距离和有限触发预算。做少量有意义的检查：None/仅传播为空；回声待触发时有正确目标和时间；查询前后伤害/队列/计数不变；失效代号/已死亡不出标记；Clear/新局没有旧记录。不要扩装备编辑器或第二套HP。提交PR、README、实际验证与给Codex的请求；无Unity就明确写未运行。
~~~

## 转给2号：预备→命中→结果的可读动效

~~~text
执行 Docs/Integration/REVERB_TASKS_04.md 的2号部分。分支 feature/m01-readable-feedback；只写 Assets/Experiments/Mechanism/Presentation/ 和 Docs/Experiments/M01-View/。不要改 Editor/Scenes、Integration、Rules、共享CombatViews/AudioDirector；这些由Codex收口。不要改新全屏组件。

用户看不懂当前扩散双环的作用。回声实际是0.5秒后原目标再次受伤，不是范围伤害。做一套TEST_ONLY反馈：原目标身上有短暂预备标记→到期在原目标收束/脉冲→显示已发生的回声伤害；传播仍用有方向的目标间连线及落点反馈。优先把因果看清，不用更大地面圈、全屏闪白或大量光污染解决。保持紫红回声/金色传播的现有测试约定，参数可调，危险红橙留给未来预警。

在本目录定义 M01EchoCue（Serial、Generation为int；X、Z、Progress为float）并为 M01DerivedFx 增加 public void SyncEchoCues(IReadOnlyList<M01EchoCue> cues)。传入的是Codex根据真实规则计算的快照，Progress在0..1之间；你只同步目标位置与外观，不累计规则时间或自行触发命中。空快照、禁用和Restart清空标记；按Serial+Generation区分池对象；每目标只需一个标记，使用有上限的预热池。

给现有 PlayEcho 增加 (float x,float z,float damage) 重载，PlayPropagation 增加 (float fromX,float fromZ,float toX,float toZ,float damage) 重载，保留旧签名兼容。只有实际调用这两个命中接口才展示命中/伤害标签；预备标记不显示假伤害。轻量标签可用“回声 -5”“传播 -4”，有限池/有限寿命并在暂停时冻结，避免充满屏幕。未接规则适配时明确等待，不猜 EnemyHit、不再扣血、不猜下一传播目标。

提交最小实现、用法和PR。无Unity时只声明源码交付，Codex负责真实接线、导入和实机检查。不要额外修改战斗数值、相机、模型动画或正式艺术方向。
~~~

## 转给主美：审查“原目标二次伤害”的表达

~~~text
执行 Docs/Integration/REVERB_TASKS_04.md 的主美部分。分支 review/m01-readability-r1；仅写 Docs/Art/Reverb/。先看 Docs/Verification/M01 的实际相机截图和用户反馈，不把原双环出现当作辨识已通过。

交一页 M01-R1 分镜/验收表：直击→原目标被标记→0.5s后原目标二次受伤→金线跳向另一目标。说明哪个形状和运动表达“即将发生/正在发生/已经造成结果”，避免扩散圈被理解为AOE。现有回声是单目标，不能以5m大圈暗示伤害范围；5m仅是传播找目标距离。检查俯视下角色脚底/身体遮挡、预备标记与命中脉冲差异、伤害标签与主HUD的主次。参数是建议，不写成用户批准的硬阈值。

沿现有紫红/金色测试语言做小范围建议，不重新定整套美术，不索要或制作正式素材。2号R1尚未生成真实画面时，明确标待实机审查；不要虚写通过。需要跨目录修改时只给最小请求，由Codex执行。
~~~

## 转给音效总监：区分原位回响与目标间跳跃

~~~text
执行 Docs/Integration/REVERB_TASKS_04.md 的音效部分。分支 feature/m01-audio-cues；只写 Assets/Experiments/Mechanism/Audio/ 和 Docs/Experiments/M01-Audio/。不改AudioDirector、场景、Integration、DSP控制器、旧音频或BGM。

用户不易感知声波作用。基于现有合法TEST_ONLY短音，给回声与传播各一个更易区分的技术试听变体：回声体现“原目标延迟后的第二下”，传播体现“从一个目标跳到另一个”。可以通过简短包络/滤波/音高处理区分；优先复用/加工已有测试声音，不做正式配乐、循环BGM或复杂动态音乐。若产出WAV，命名显式TEST_ONLY，48kHz，单段尽量≤0.3秒，提供可复现处理脚本和源文件对应，保留旧源不覆写；只用现有环境依赖，不新增插件。没有合适音源则明确暂无，给可执行接线建议即可。

声音仅跟随成功结算事件，不另设0.5秒计时器决定伤害。预备阶段不播放“已经命中”的声音；目标提前死亡/失效时不响假回声。本轮先不加额外预备声道。提交原始样本峰值/时长、建议增益/并发/限频，以及哪些判断需用户实际监听；不把峰值测量写成True Peak、无爆音或听觉已验收。池满丢弃策略沿现状，不擅自要求实现5ms抢占淡出。Unity meta由Codex导入生成，无Unity须注明。
~~~

## Codex与验收

Codex负责全屏F11/按钮、规则快照到视图的只读桥、伤害值事件接线、声音通道与导入、少量关键检查及新试玩包。UI不反写伤害，不从预备标记触发命中；四模式重开、暂停和失效目标清理必须保持。

用户下一次只验收：**看一眼能否知道声波在谁身上、何时会再打一下、伤害是否已经发生；全屏是否顺手。** 组合机制值得继续已得到用户肯定，闪避维持；不重复索要同一项方向确认。
