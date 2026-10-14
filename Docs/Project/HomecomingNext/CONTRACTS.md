# HC00 v1：共享契约与宿主接线计划

实际代码：`Assets/Waffle/Scripts/Rules/Homecoming/HomecomingContracts.cs`，命名空间`Waffle.Homecoming`，归现有无Unity依赖的`Waffle.Rules`程序集。下面区分已存在的纯接口和后续Codex负责的接线；此版本没有运行时宿主实现。

## 统一编号与内容

- 地点模板：`poi.old-home`、`poi.old-silo`、`poi.old-guitar`。
- 遗物：`item.sewing-tin`、`item.work-gloves`、`item.old-guitar`。
- 目标：`goal.homeward`、`goal.harvest-memory`、`goal.unfinished-song`。
- 永久升级：`meta.mended-coat`、`meta.travel-light`、`meta.sorted-satchel`。
- 叙事ID：地点/遗物/目标/升级沿用其内容ID；已有W/P/E/D物品沿用现有目录的实际ID，禁止重编。
- `HomecomingContract.SiteId(seed,cellX,cellZ,templateId)`产生`hc1:seed:x:z:template`。出生区独有地点使用固定保留格坐标，A在布局说明中列明。不要把地块实例ID、时间戳、浮点位置或对象名称作为领取键。
- 世界种子使用`ProceduralFieldStreamer.Seed`，不是`RunConfig.seed`。GameLoop.Awake早于streamer.Start：宿主等地图完成初始化再Reset/Register；不能先读取默认0再悄悄换种子。
- 地点ID跨地块卸载稳定；单局奖励去重键为`RunId + SiteId`。跨局叙事去重使用NarrativeId，不能用RunId阻止下一局正常探索。

## A：IPoiModule

`Reset(runId,worldSeed)`每局调用一次。`Register(PoiSite)`只接受有限坐标、正的有界交互半径、已知模板与同种子合法ID；重复同内容注册幂等，冲突注册拒绝。地块卸载不清除发现/领取状态。宿主提供附近有限站点，不预生成无限世界。

`Step(ExplorationContext)`使用规则时钟，仅Playing且活着推进；`ReadNearby()`输出有序、数量受限快照。`TryPrepareClaim(context,siteId,out reward)`检查当前距离、状态和RunId，给出不可变`PoiReward`，不直接给能力。重试直到确认前应返回同一意图。`ConfirmClaim(runId,siteId,accepted)`仅接受对应待确认记录；false保留可领取，true封账。旧RunId/远程伪造/死亡帧不能领取。

## B：IRelicInventory

`Reset(runId,startingSlots)`开局2格，上限4格，与现有4武器/6被动槽完全独立。`TryGrant(PoiReward)`按ClaimId原子处理扩容、唯一物品入袋和首次自动装备，失败不消费任何部分；重复已成功请求返回true但不再次发奖，以支持A确认重试。不同RunId拒绝。

首个老房子及首个筒仓各扩容1格，同模板后续站点不得无限加格或重复堆物品。背包最多保存本轮已知有限遗物集合；满装备槽时物品进收纳，不消失、不掉在无限麦田里。`TryEquip/TryUnequip`由宿主先检查Phase/RunId，B再检查所有权和槽位；同一物品不得占两格。

`ReadModifiers()`只返回额外加成，不改State/Config/Combat。小数为加法比例：0.05=+5%，冷却缩减为正值。`DodgeEcho`只表示能力启用；宿主收到真实成功闪避事件后执行有上限的单次效果，不由动画触发、不递归触发P17或自身。B提供效果规格与必要纯规则辅助类，Codex改既有战斗接点。

## C：IHomecomingProfile

构造/工厂时通过注入存储加载档案，实际存储根由Runtime适配器使用`Application.persistentDataPath`传入。纯规则不引用Unity、不用PlayerPrefs存整份档案。C可在自身目录定义保存DTO与存储端口，不能改共享视图字段。

`Read()`为不可变副本；`ReadStartingModifiers()`在出发时读取一次，不在局中热改能力。`TrySettle(ExplorationSettlement,out receipt)`仅接收有效终局，由宿主从真实RunOutcome及确认账本构造，包含稳定内容ID清单，无客户端传入的XP数额。已结算RunId返回原收据且AlreadySettled=true，不加第二次。首次成功写盘后才发布经验/点数/图鉴状态。失败返回false，保留旧状态，可重试。

`TryPurchase(upgradeId)`验证等级上限、成本与剩余点，成功写盘后才发布；宿主只在出发前/终局开放购买。暂停期间可查看，不允许在局内购买重算能力。保存成功与“已经结算”必须同一事务。禁止删旧RunId记录造成重放再领；首轮数据量小可保留完整清单，后续压缩另行版本化。

## D：IHomecomingGoals 与表现端口

`Reset(runId)`；`Observe(HomecomingFact)`只消费宿主确认后的真实事实，按EventId去重且拒绝旧RunId；`Read()`显示有限目标状态。观察/显示不得发奖励。目标完成ID纳入结算，永久首次奖励归C。

`NarrativeViewed`仅在玩家实际打开对应正文且目录命中后由宿主确认，SubjectId为NarrativeId；不能把靠近/自动发现等同已阅读。查看命令走日志适配器的宿主确认入口，由Codex补桥，不由D直接改目标。重复阅读可产生去重事实，不重复发奖。

`IHomecomingViewSource`为宿主将A/B/C/D组合后的统一只读桥；`IHomecomingCommands`为表现向宿主提交交互、装备、升级和日志的命令桥。D可用测试适配器开发UI，但正式GameLoop实现这些接口归Codex。所有物品/地点/目标/升级从统一叙事目录读取`NarrativeEntry`，没有文本的内容视为交付缺陷。

`TryOpenJournal/CloseJournal`由宿主管理暂停所有权：只有从Playing主动打开才自动暂停并在关闭时恢复；从Esc暂停查看后关闭保持暂停；LevelUp不叠开，终局关闭不恢复Playing，失焦后关闭不擅自继续。D不得自行写Time.timeScale或新建一套输入循环。

换装只在Playing或由日志持有的Paused且玩家存活时接受；LevelUp/终局拒绝，查看文本仍可进行。永久升级购买仅出发前/终局，不能把局内暂停当局外。首次出发前由Codex门控创建/推进RunSimulation，先展示档案页再开始战斗，避免菜单背后计时。

## Codex后续整合顺序（尚未实施）

1. 场景适配器建立C档案；拷贝本局RunConfig再应用开局加成，不能修改共享序列化配置造成重开叠乘；GameLoop创建唯一RunSimulation。地图Seed就绪后Reset A/B/D。
2. 每个Playing tick按现有T0→T13完成战斗/死亡裁决后，只有仍Playing且活着才Step A、处理一次最近地点领取。靠近默认自动拾取；同帧多个候选按距离与SiteId排序，每tick只处理一个。E作为可选手动确认，不改变自动模式的默认需求。
3. A准备意图→B原子接受→A确认→宿主记录SiteClaimed/ItemEquipped事实→D观察。相同tick的死亡优先于兴趣点奖励。没有新MonoBehaviour直接改HP/XP。
4. B装备加成由统一统计函数结合普通升级和本局固定局外加成重算。卸装最大生命减少时裁到上限；重新装备不补当前HP，避免换装治疗循环。总加成有上限；既有P/W升级刷新也调用此函数，避免互相覆盖。
5. 终局捕获Outcome前后按固定顺序冻结探索账本，记录RunEnded一次，D结算目标→构造ExplorationSettlement→C保存与结算→D展示真实收据。重开前不得清掉未落盘结算，提供可见失败状态与重试。
6. 普通中断/关闭不伪造Victory/Defeat；已发现文本可另以独立去重日志事务保存，C提供适配方案。此次存档保存局外档案与图鉴，不承诺战斗中途续局。10分钟终局暂保留，后续探索模式时长由试玩再决定。

HC00验证只覆盖编号稳定性、输入文本约束、视图复制与交互门控；各模块行为和真实Main接入由后续各包及Codex验证。
