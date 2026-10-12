# RC02 组件与内容管线合同 v1

2026-10-12。用户接受当前音效，下一步优先玩法增量、修bug和可复用装配。沿原四位外部AI协作，用户转发，Codex共享接线与整合。本合同是下一批实现约束，不能把合同/数据表本身算成已完成系统。

## 目标与批次

工业化在本项目中的含义：新增一种玩法通常是组合已有规则组件；换模型、特效、声音只改资产绑定；每个内容有固定ID、模板、预算、来源、状态和可执行装配入口。采用小而清楚的C#模块、纯规则配方、ScriptableObject表现目录与Prefab模板，不引入DOTS/ECS、反射插件框架、万能节点编辑器、新音频中间件或新Packages。

08A先交组件骨架与能运行的样板：W01旧行为适配、W05弹跳电弧、W06移动残响，增补P03弹速/P05持续/P08减伤/P17闪避放电；首图目标从4/10/3/4增加到6攻击/14被动/3进化/4拾取。此计数仅在Codex接入、正常可获得且实际有效后成立。E04～E06、P10/P11/P14/P16、D05/D06仍planned，下一批08B补齐原6/18/6/6目标。优先可组合的差异，不拿换色/阶数冒充内容。

四条可体验路线：点杀＋回声/传播、扇扫＋推退、绕身＋护盾、移动布置＋闪避引爆；新增链击可与传播组合，但每次派生必须有限。当前首图仍10分钟，其他三图只有草稿；本批不同时承诺四图完整制作。

## 权威与执行流

`内容目录/配方 → 局内装备与属性 → 唯一规则宿主 → 实际结算事件 → 表现目录 → 池化模型/VFX/现有AudioDirector`。

GameLoop→RunSimulation→CombatModule仍唯一；XP、HP、局内时钟、随机种子、伤害、击杀、掉落不在UI、Prefab或音效里另存一套。组件不各自Update，不读取UnityEngine/Time，不拿PresentationEvent倒灌玩法。纯规则每局新实例。旧武器逐个迁移，未迁移的沿旧入口运行，不能两边同时开火。

1号在获准Components目录内定义类型与运行时：配方分为触发（冷却、累计移动、实际闪避）、选目标、载体（弹/链击/残留区）、修饰（数量/范围/持续/击退/有限派生）。先实现本批用到的少量组件，不预建无消费者接口。提供明确的Advance(frame)、NotifyDodge/NotifyDirectHit等输入、只读敌人快照和输出命令；最终方法名由1号在API.md固定，其他作者不复制合同。命令携带contentId、slot、source/root、target serial+generation、位置/方向、伤害/半径/持续与链预算。命令经Codex适配后在既有T5/T6/T7规则窗口执行，禁止独立结算奖励。

所有派生携带root与深度/剩余预算；同目标去重按serial+generation，实际死亡奖励仅一次。已有P12/P13有限一次组合保持。W05同链不能重复目标，不能因传播重新无限起链；W06按实际位移累计，暂停/重开/传送式调试位移不能积攒爆发，残留数量与命中频率有上限；P17只认闪避真正发动，不能按键失败也放电。具体数值唯一来自目录，同一属性聚合一次。

普通掉落物与XP统一磁吸，精英箱接触拾取。当前P09半径3/3.75/4.5/5.25米来自宿主Config×成长倍率，组件迁移不得恢复普通道具固定1米的旧行为。

## 表现交换格式 v1

四位使用现有固定内容ID。交换JSON由作者维护，2号Editor工具导入为序列化目录资产；运行时不读磁盘JSON、不用字符串反射查类型、不用Resources.Load遍历目录。正式资产以后替换路径/GUID绑定，无需改规则代码。

每份包有schemaVersion=1、packageId、entries数组；每项字段如下，未用的数组为空：

| 字段 | 类型/约束 |
|---|---|
| id | 全局唯一条目，例如rc02.art.w05.hit；不能作为新的玩法ID |
| kind | art / audio / music |
| contentId | W01..W06/P01..P18/E01..E06/D01..D06；通用UI可为* |
| eventType | 现有PresentationEventType精确枚举名；music留空，由profileId选择 |
| profileId | 音乐档案ID，非音乐留空 |
| assets | [{role,path}]，path为工程相对Assets路径；role如prefab/material/icon/clip/combatLoop/levelUpLoop/stem |
| parameters | [{key,value}]数值数组，避免JsonUtility不支持Dictionary；参数只控制表现，不含伤害/真实半径规则 |
| maxInstances / minInterval | 正整数并发与非负秒节流；缺项用工具明确默认 |
| pausePolicy | freeze / stop；恢复不能补播旧触发 |
| status / source | TEST_ONLY或明确WIP，来源/处理说明必填 |

```json
{"schemaVersion":1,"packageId":"rc02.art.v1","entries":[{"id":"rc02.art.w05.hit","kind":"art","contentId":"W05","eventType":"EnemyHit","profileId":"","assets":[{"role":"prefab","path":"Assets/Drafts/RC01/Art/ComponentsV1/Prefabs/TEST_ONLY_ArcHit.prefab"}],"parameters":[{"key":"width","value":0.04}],"maxInstances":8,"minInterval":0.06,"pausePolicy":"freeze","status":"TEST_ONLY","source":"existing test material; see package README"}]}
```

匹配规则固定为kind＋eventType＋contentId，精确ID优先于*；同优先级重复报错，按variant/palette扩展必须下一版合同明确，不能随意最后覆盖。一个现有事件若基础AudioDirector已负责，不能再由新播放器重复播放。表现运行时消费事件真实位置/Direction/From/Radius/Duration与目标代号；没有字段列宿主请求，不按当前武器或粒子猜伤害时刻。

艺术参数首批width/scale/tailSeconds/intensity；声音gainDb/pitchMin/pitchMax/spatialBlend/minDistance/maxDistance/priority；音乐musicGainDb/fadeSeconds/bpm/beatsPerBar。参数由2号按kind白名单解析，未知键报告，不默默当玩法参数执行。音乐BPM/拍号未知时填0且禁止启用拍点对齐；没有对齐stem时assets不写stem，不伪称已实现动态分轨。

导入检查只需schema/重复键/ID/枚举/路径与资源类型/预算非法值，错误显示可定位清单；缺资源保留旧绑定或明确占位，不删除现有正常资产。刷新需幂等、保留GUID、不会反复复制素材。Inspector可看contentId、资源与预算，支持单条预览和暂停/清理。预览必须显式TEST_ONLY隔离；没有宿主不伪造游戏结果。

## 制作模板与后续填充

主美交Projectile、Hit/Arc、Zone/Trail三类Prefab模板和配色/材质预设，标准根节点单位缩放、+Z朝前、米制；可调尺寸与尾长，池回收清粒子/拖尾/颜色，停止发射后有有限寿命。布局、风格和资产说明与规则数据分开。

音效总监交复用现有22段短音的绑定包、现有战斗/升级音乐档案与导出检查工具；先把路铺好，本轮不重做音色、不生产大量新音乐、不添加新的常驻音频宿主。未来合法素材按包替换，音乐气质继续数学摇滚/后摇/电子，冷静精密、战斗爆发。

2号交内容目录Inspector/导入刷新/预览入口与独立装配样板场景；用当前W01/W04/P12和旧资源先跑通，主美/音效新包随后直接刷新。新场景必须复用当前正常HarvestRoad宿主与真实升级流程，不写另一个GameManager。场景事件未来按遭遇配置复用同一组件链，08A只给一分钟段配置样板，不同时扩四图规则。

新增内容操作应收敛为：1号增配方/目录与说明 → 表现作者选择模板、填绑定与来源 → 2号刷新目录/场景配置 → Codex接唯一宿主、编译打包 → 用户试玩验收。特殊新行为允许增加一个明确C#组件，不追求所有内容零代码。

## 所有权、交付与验收

具体唯一范围见TASKS_08，现有共享文件仍由Codex负责；1号此次额外获准新建Rules/RunContent/Components/，2号额外获准RC01/Pipeline/；其他人只读。跨范围写REQUESTS.md，不能顺手改CombatModule/GameLoop/AudioDirector/程序集根或别人目录。四位从含本合同的最新origin/main独立分支，不互相合并、不强推、不直接push main。

只做实际必要的编译/导入/样板运行，不新建全量矩阵、长时间挂机或为了数量写测试。没有Unity就交可执行工具＋未验证清单，Codex处理meta与最终装配。每份交PR、完整SHA、基线、修改范围、实际效果/未接线项、最多3项宿主请求与新增一种内容的操作说明。

验收看两点：新组件是否产生不同打法；新增/替换一种表现能否只改配方或绑定而不改主循环。性能/完整回归在中后期计划进行，不拿“工业化”名义提前搭大型平台。
