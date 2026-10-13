# GP00 v1：实际接口与接线说明

状态：GP00 v1已实现，模块编译与真实Main场景接线验证通过。命名空间为`Waffle.GameplayNext`，属于既有`Waffle.Rules`程序集。入口代码为`GameplayContracts.cs`、`GameplayGeometry.cs`、`CombatModule.GameplayNext.cs`、`RunSimulation.GameplayNext.cs`。

新模式默认关闭，当前正式Main不安装A/B/C的占位模块。GP00只提供共用宿主，A/B/C/D内容并未因此自动完成。

## 开工方式

外部任务从发布的`gameplay/gp00-ready`共同提交开始，各自新建任务分支和独立目录。不要直接在Codex整合目录编写，也不要只从旧origin/main猜测接口。提交号与验证结果见GP00_RELEASE.md。

A实现`IEncounterModule`，B实现`IObjectiveModule`，C实现`IFinaleModule`；三者都需要：

```csharp
void Step(GameplayContext context, GameplayCommandBuffer commands);
void Clear();
```

B额外实现`ConfirmReward(Guid runId, string objectiveId, bool granted)`。Clear必须幂等地清理本局状态；工厂每局返回新实例，不跨局复用模块。

## 安装到真实主循环

Codex的场景适配组件实现`IGameplayModuleProvider.CreateGameplayModules()`，返回`new GameplayModules(encounters, objectives, finale)`。把适配组件绑定到GameLoop的`gameplayModuleProvider`，并开启`config.gameplayNext.enabled`。GameLoop在构造RunSimulation后、第一次Tick之前安装模块。

纯规则检查可直接构造RunSimulation、开启该开关，再调用`sim.InstallGameplayModules(...)`。安装只能执行一次且必须在首次Tick之前；BossClear必须有Finale模块；不同槽位不可共用同一模块实例。启用开关却不绑定场景工厂会明确报错，不静默假装新玩法已接入。

## 每个回调能读什么

- `RunId`为本局身份，重开不同；`Seed`为原始种子，`ModuleSeed`为固定分流种子。用自己的CombatRng，不消费旧刷怪/升级随机流。
- `Now/Dt`均为Playing规则时钟，暂停/升级不调用Step；`PlayerPreviousPosition/PlayerPosition`为T2前后位置。
- `RunDuration/ArenaHalfSize/PlayerRadius/ViewRadius`提供边界与空间口径。
- `Enemies`为当前只读快照，含Key、ArchetypeId、前后位置、真实HP/MaxHP、速度、半径、存活/精英/首领标记、RelocationVersion和SpawnRequest。
- `ActiveAttacks/SpawnWarnings`为上一完成步骤的只读状态，可用于检查请求是否进入宿主。快照集合属于借用视图，下一步骤会更新；保留历史时复制需要的值，不长期保存集合当历史。
- 普通挪位会取消该敌人的招式并增加RelocationVersion，行为模块据此重置锁定/冲锋；Serial+Generation复用必须丢弃旧状态。

## 输出命令

命令缓冲只在Step回调期间开放；保存缓冲后从UI/Update晚交命令无效。Try方法返回值仅表示本回调缓冲是否接收，最终仍由宿主做实体、预算与时序校验。

| API | 负责方 | 口径 |
|---|---|---|
| `TrySpawn(SpawnIntent)` | A/C | Sequence、合法位置、EnemySpawnSpec、WarningSeconds |
| `TryMove(MovementIntent)` | A/C | Owner、Velocity、UseSeparation、AllowRelocation |
| `TryAttack(HostileAttackIntent)` | A/C | Sequence、Owner、Shape、ActivateAt、EndAt、Damage、SweepOwnerMotion |
| `PublishEncounter(int phase, string label)` | A | 当前遭遇段与文案 |
| `PublishObjective(ObjectiveSnapshot)` | B | 本tick实际目标；不发布则本tick目标为空 |
| `TryReward(ObjectiveRewardIntent)` | B | RunId、ObjectiveId、RewardId；每tick最多一个 |
| `PublishFinaleAttack(string label)` | C | 当前招式文案，不写HP |
| `RequestFinaleResolution(FinaleResolution)` | C | 提示性请求，不能覆盖真实HP/死亡/截止裁决 |

生成和攻击Sequence分开计数，每个模块内从1开始严格递增。已接受请求不重复执行；预算不足或生成点后来被玩家占据的请求会跳过，不堆积补发。生成是否成功以`Enemies.SpawnRequest`为准，仍在预警中以`SpawnWarnings`为准；需要重试时等待合适时机，用新Sequence，不每帧爆量补发。

A控制非首领，C控制首领；B不能发生成/移动/攻击。生成Spec提供ArchetypeId、MaxHp、Speed、Radius、ContactDamage、GemValue、IsElite、IsBoss，首领只能由C请求且每局最多一只。HP/死亡始终写入宿主EnemyState，不另存可扣血副本。

安装A后A接管普通/事件生成，必须自己覆盖任务卡要求的波次与4:00/8:00精英；没有A时保留旧刷怪。首领存活时宿主停止旧刷怪，A的新普通生成受bossOrdinaryCap（默认16）限制并拒绝额外精英；已有敌人不会凭空消失。

## 攻击几何与预算

`AttackShape(kind, center, radius, end, innerRadius, expansionSpeed)`：Circle为圆；Capsule为Center→End的胶囊；Annulus为真实内外半径，按激活后的局内年龄同速扩张。只有Annulus允许非零ExpansionSpeed。

冲锋使用Capsule并设置`SweepOwnerMotion=true`：预警期间保留请求中的锁定路径；激活后宿主使用所有者本tick前后位置与玩家相对运动做连续扫掠，给D的快照也更新为实际扫掠段。普通静态圆/胶囊检测玩家本tick路径；扩散环采用连续相交计算，不靠离散取样碰运气。

总危险最多12，普通危险最多8，待出生预警最多8；普通实体同时最多一个危险形状。首领同一落波招式允许最多3个Circle，不与Annulus混合重叠；Annulus同时最多1个。每回调生成缓冲8、移动64、攻击12；所有数字都为上限，池不足会更少。

警告最长5秒、激活寿命最长15秒、移动速度最多30m/s；非有限值、非法半径、过期/回溯时序、失效所有者都被拒绝。一次攻击对玩家最多提交一次候选；被闪避/护盾挡下也消耗本次候选，不能等待无敌结束后再扣一次。

T7所有者死亡先取消攻击，再进入T8伤害聚合。接触与招式共享原有减伤、无敌、护盾与最大值/叠加设置，表现层不能另扣血。

## 目标奖励

B每tick发布当前目标；申请奖励时必须是Completed、Progress=1，Id与RewardId和请求一致，RunId必须是当前局。最多兑现两个不同ObjectiveId；重复提交返回确认但不重复发奖。

只允许D02与D03。D02复用回复20%最大HP的规则；D03复用磁吸当前Free经验的路径。T9死亡后不兑现，不复活玩家，不新增经验。确认回调的granted=true同时表示首次兑现或此前已经兑现。目标失败/旧RunId/非法收益返回false。

新增`ObjectiveRewardGranted`事件的SourceId为目标ID，RootSourceId为D02/D03。D02的Value是实际回复HP，D03的Value是转入磁吸的宝石数量；不能把该数量当成经验值。

## D读取与终局

GameLoop实现`IGameplayViewSource`：`ReadGameplayView()`返回当前RunGameplayView，未安装时为null；`ReadRunOutcome()`返回不可变结算快照，未终局时为null。包含实际阶段、目标、危险、出生预警、真实首领HP、完成目标数和Outcome。

BossSpawned/BossDefeated事件仅是反馈，不能据此直接改胜负。BossClear由宿主确认首领死亡后胜利；到局长仍未确认则失败。玩家同tick死亡始终优先；只在截止同tick击杀且玩家存活时胜利。SurvivalOnly保留旧优先级设置。

RunOutcome保存真实胜负原因、时间、击杀、等级、现有武器/被动、完成目标数及首领结果，不包含未经统计的DPS。终局/重开清空所有危险与预警；重开创建新RunId。预算与快照是固定宿主服务，不是通用脚本插件或第二套战斗循环。
