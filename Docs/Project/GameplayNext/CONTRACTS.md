# GP00：共同接口与所有权

GP00 v1已实现共同接口与宿主接线。具体签名、命令语义和预算以 [实际API说明](GP00_API.md) 及对应源代码为准；发布提交和验证结果见GP00_RELEASE.md。下面保留设计口径与所有权约定，不要复制另一套同名接口。

## GP00最小交付

1. 在`Assets/Waffle/Scripts/Rules/GameplayNext/`创建共用的值类型/只读快照/命令契约，属于现有Waffle.Rules程序集，避免新增通用事件总线或插件框架。
2. 由CombatModule在现有步骤内调用A/B/C模块；只执行有界的生成、移动意图与攻击意图。模块不持有Unity对象，不读Time/Input，不改HP/XP。
3. 由RunSimulation接受唯一伤害候选、一次奖励和终局请求；暴露给D的只读快照。扩展PresentationEvent及GameLoop分发仍由Codex完成。
4. 默认关闭新模式时保持旧路径；编译、现有关键规则回归及一次短时真实场景运行通过后，公布共同提交和准确签名。

## 需要冻结的数据

以下为字段需求，不强制把每一行做成独立类；名称、可见性和拷贝策略在GP00审查时确定。

| 数据 | 最少内容 | 写入/执行者 |
|---|---|---|
| GameplayContext | runId、Now、Dt、Playing、玩家前后位置/半径、场地边界、独立模块种子 | 宿主提供，A/B/C读取 |
| EnemySnapshot | Serial+Generation、ArchetypeId、前后位置、半径、是否存活/精英/首领 | CombatModule提供 |
| SpawnIntent | 请求唯一ID、敌人类型、允许的出生区、预警时间、生成预算 | A/C请求，CombatModule执行 |
| MovementIntent | Serial+Generation、方向或目标、速度/锁定方向、是否允许普通远离挪位 | A/C请求，CombatModule执行并做边界/分离 |
| HostileAttackIntent | AttackId、OwnerSerial+Generation、几何、锁定位置/方向、开始/激活/结束时间、伤害、命中策略 | A/C请求，宿主检测并结算 |
| ObjectiveSnapshot | ObjectiveId、位置/半径、Available/Channeling/Completed/Expired、进度、过期时间、收益说明 | B写，只读给D |
| ObjectiveRewardIntent | runId+ObjectiveId唯一键、既有收益ID、完成时刻 | B请求，RunSimulation确认与兑现 |
| FinaleSnapshot | 首领实体键、预警/存活/死亡、实际HP/MaxHP、当前招式、10:00截止点 | C汇总宿主状态，只读给D |
| RunGameplayView | 遭遇阶段、有限危险快照、当前目标、首领状态、规则终局原因 | 宿主聚合给D，不暴露可修改集合 |
| RunOutcome | 真实胜负/原因、时间、击杀、现有装备/等级、完成目标数、首领结果 | 宿主结算后生成 |

攻击只支持本轮需要的Circle、Capsule和Annulus三种几何；共享同一份几何参数和时序给规则与表现，禁止D凭感觉重新猜半径。环形攻击明确内外半径、扩散速度和玩家半径，冲锋明确扫掠起终点。

## 执行顺序与裁决

- 保持既有T0–T13顺序。T2之后取得玩家实际移动结果；A/C生成/移动在既有T3/T4适配；攻击命中候选在T8之前收集。
- T7既有敌人死亡结算先执行，死亡会撤销该实体尚未结算的攻击；之后才参与本tick玩家受伤。先明确这一口径，再做预警表现。
- T8所有接触与新危险统一通过既有减伤、无敌、护盾及接触伤害聚合策略。每个AttackId对玩家最多提交一次有效候选，不能碰一次每帧扣一次。
- 首领HP就是对应EnemyState的HP；死亡、击杀、掉落走现有路径。C不能维护第二份可扣血HP，首领也不能按普通远离规则瞬移。
- B按T2位置计算进度并提出完成请求，T9死亡裁决之后才兑现奖励；同tick死亡不发奖。经验/回复/磁吸复用已有结算，不由UI直接执行。
- `BossClear`的胜负由RunSimulation裁决：玩家死亡优先，其次本tick首领已被T7确认死亡，再判10:00超时。恰在10:00击杀且玩家存活算胜利；旧模式保持原口径。
- 停止Playing时不推进模块时间；暂停/升级不积攒补发攻击、波次或奖励；重开创建新的runId并清空状态。

## 预算口径

作为GP00默认安全边界，后续只能经整合修改：普通敌人沿用主场景48上限，总敌人池56；新增攻击同时最多12个，其中普通攻击最多8个；普通实体最多1个危险形状；首领同一落波招式最多3个圆，不能与声环混合重叠；目标同时最多1个；首领最多1只。

预算不足时跳过或延后当前请求，不能在恢复后集中补发。攻击所有者失效、世代变化、出池复用、终局与重开时立即回收。普通敌人挪位要撤销预警/锁定攻击并重置行为，不能隔着地图继承一次冲锋。

## 共享文件只由Codex修改

`RunSimulation*.cs`、`RunConfig.cs`、`CombatModule.cs`、`CombatTypes.cs`、`CombatConfig.cs`、`PresentationEvent.cs`、`GameLoop.cs`、既有CombatViews/RC01GrowthHud、程序集定义、Main.unity、视听清单与目录。

A/B/C/D只在各自新目录写代码与说明；对共享文件提交适配清单，Codex集中实现。需要新增契约时先说明实际用途，由Codex更新共同提交并通知其他任务；不能在各分支私自分叉类型。

## D的联调数据

D可以在隔离的编辑器预览中构造上述只读快照验证布局，不能将预览组件接到正式Main或把假血量/假进度当作实装。最终只接受来自真实宿主的快照和事件。
