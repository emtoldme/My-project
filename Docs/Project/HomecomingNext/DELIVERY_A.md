# HC-A 交付：兴趣点、世界布局与全物品叙事

- 分支：`gameplay/hc-a-places`，基线 `hc00-v1`（9516b914ca57be950aafdb30f82824b5a8237900），PR 目标 `gameplay/homecoming-next-ready`。
- A1（纯规则+目录+测试）与 A2（表现适配器+编辑器Prefab生成工具+本文）在同一分支，完整SHA见PR。

## 改动目录

| 路径 | 内容 |
| --- | --- |
| `Assets/Waffle/Scripts/Rules/Homecoming/Places/` | `PoiModule`（IPoiModule）、`HomecomingPlacesLayout`、`HomecomingPlaceCatalog`、`HomecomingNarrativeCatalog`；Waffle.Rules，无UnityEngine |
| `Assets/Waffle/Tests/EditMode/HomecomingPlacesTests.cs` | 11个用例（含参数共15条） |
| `Assets/Game/Presentation/Homecoming/Places/` | 新asmdef `Reverb.Homecoming.Places.Presentation`（仅引用Waffle.Rules）：`HomecomingPlacesPresenter`、`HomecomingPlaceView`、`HomecomingPlacesSiteFeed` |
| `Assets/Game/Environments/HomecomingPlaces/` | `Editor/HomecomingPlacesPrefabBuilder.cs`（Editor-only asmdef，手动菜单）、`SOURCES.md` |

未改共享契约、GameLoop、RunSimulation、streamer/layout、Main、HUD、音频与其他包目录。

## 已实现

- **IPoiModule**：`Reset`清空本局全部账本；`Register`校验有限坐标、0<半径≤8、已知模板、叙事ID、`SiteId`与本种子/24米地块坐标/模板一致；唯一模板只接受本种子保留落点（防复制主角家）；同内容幂等true，冲突false；Reset前拒绝。`Step`仅`CanInteract`且RunId/种子匹配才推进，否则冻结（快照保留上一帧）。发现半径18米，交互半径2.4米。`ReadNearby`为只读副本，按距离→SiteId排序，≤8条。`TryPrepareClaim`用**当前**玩家位置检查距离，拒绝旧RunId/错种子/远程/死亡/非Playing；确认前重复调用返回同一`PoiReward`（ClaimId=SiteId）。`ConfirmClaim(false)`保留可领取，`(true)`封账；错RunId/无待确认忽略。地块卸载不影响状态（账本与Prefab无关，重新Register幂等）。
- **自动拾取**：附加`TrySelectAutoClaim(ctx,out siteId)`，从快照取最近Available一个；宿主每tick至多处理一个。
- **有限预算与超预算**：注册≤256（3个唯一站点永远有保留名额，超出的新通用站点`Register`返回false，宿主可忽略）；通用站点发现记录≤64，满后不再记为已发现（唯一站点不计入）；快照≤8按距离截断；`SitesNear`半径≤240米、≤64条。
- **账本查询**：`DiscoveredNarrativeIds()`、`ClaimedTemplateIds()`、`ClaimedSiteIds()`、`StatusOf(siteId)`、`HasPendingClaim`，均为副本，供宿主构造`ExplorationSettlement`。
- **奖励**：旧家→`item.sewing-tin`+1槽；筒仓→`item.work-gloves`+1槽；吉他→`item.old-guitar`+0槽。远处通用模板（`poi.field-swing/field-tractor/field-radio`）**只给叙事、无奖励**，不可能重发唯一奖励/栏位。
- **叙事目录**：`HomecomingNarrativeCatalog.Default.Resolve(id)` / `All` / `OfKind`。地点3+远处3、遗物3、目标3+完成补文3（`<goal>.complete`）、永久升级3、现有RunContent**全部36条**（W01–W06、P01–P18、E01–E06、D01–D06，标题沿用原名）+`HEAL`回退项，共59条，正文互不相同。注：卡上“约27条”与实际目录不符，以`RunContentCatalog.Default.All`为准（测试断言36）。

## 布局保底（世界坐标，Vec2.X=世界x，Vec2.Y=世界z）

道路沿用R4：x=72k、z=72k田间路，中线摆动≤6米；路口农机/秋千地标在路口±约19米内。所有站点放在**路段中点、距路中线12米**的田边空地，远离路口地标；门前/仓旁留开阔走位空间。

| 地点 | 位置 | 保证 |
| --- | --- | --- |
| 旧家 | 固定 (252, 12)，所有种子相同，+X方向沿东西主路 | 直线252米：5m/s≈50秒，6.2m/s≈41秒 |
| 筒仓 | 第一圈三选一（按种子）：(36,84) / (-84,36) / (-36,-84) | 宏格环1，不与旧家同方向 |
| 吉他 | 第二圈四选一（按种子）：(108,156) / (-156,108) / (-108,-156) / (156,-108) | 宏格环2，必在第一圈外 |
| 通用远处 | 宏格环≥3，每格30%至多1个，位置由(seed,mx,mz)哈希决定 | 与探索顺序无关；负坐标安全 |

保留格：宏格（72米，`MacroOf(v)=floor((v+36)/72)`）环≤2全部，以及三处唯一站点所在宏格±1，不放通用站点。站点ID用24米地块坐标：旧家`hc1:<seed>:10:0:poi.old-home`；筒仓/吉他按选中落点的`floor(x/24), floor(z/24)`。布局哈希盐`0x48434150`，只读世界种子，不消费战斗RNG。

## 创建与调用示例

```csharp
var poi = new PoiModule();
poi.Reset(runId, streamer.Seed);                         // 地图Seed就绪后
HomecomingPlacesSiteFeed.RegisterNear(poi, streamer.Seed, player.position); // 每隔一段时间/换地块时
// 每个Playing tick在T0–T13战斗/死亡裁决之后：
var ctx = new ExplorationContext(runId, streamer.Seed, now, dt, playerPos, phase, alive);
poi.Step(ctx);
if (poi.TrySelectAutoClaim(ctx, out var site) && poi.TryPrepareClaim(ctx, site, out var reward))
    poi.ConfirmClaim(runId, site, relics.TryGrant(reward));
var entry = HomecomingNarrativeCatalog.Default.Resolve("poi.old-home");
presenter.Source = poi.ReadNearby;                         // 或 presenter.Bind(viewSource)
```

## 表现（A2）

- `HomecomingPlacesPresenter`：只读PoiView快照，复用≤8个Prefab实例，按Status驱动`HomecomingPlaceView`；不写HP/XP、不调用Prepare/Confirm、不新建GameLoop。实例销毁不影响账本。
- `HomecomingPlaceView`：有实体灯罩+玻璃的暖色点光源，未见0.15/发现1.2/可领1.8/已领0.6；领取后隐藏遗物实体。无碰撞体。冷色异常光未新增。
- `HomecomingPlacesPrefabBuilder`：菜单“Reverb/Homecoming/生成三处地点Prefab（HC-A）”，用Unity原始几何+R3材质（Sun-bleached timber、Faded cream enamel、Old iron、Field limestone、Warm practical lamp glass等）生成`P_HC_OldHome/P_HC_OldSilo/P_HC_OldGuitar`到`Environments/HomecomingPlaces/Prefabs`，并删除所有Collider。不自动运行、不打开或保存Main。没有手写Prefab YAML。

## 验证

- dotnet（`Tools/RulesTests`，net8.0，编译全部Rules源码+EditMode测试）：**191/191通过**，其中HomecomingPlaces 15条、HomecomingContract 12条。覆盖：固定种子/多种子/负坐标/顺序无关、旧家时距、筒仓环1、吉他环≥2、注册幂等/冲突/伪造/复制、发现与交互半径、意图稳定/false可重试/true封账、旧RunId/错种子/远程拒绝、暂停/升级/胜/败/死亡冻结、卸载返回保持、Reset清空、预算有限、快照只读、叙事全覆盖与正文唯一。

## 未验证 / 已知问题

- **Unity未安装**：表现与编辑器脚本未经Unity编译；**三处Prefab尚未生成**，外观、灯光、比例均未在Unity中检视。需在Unity里执行菜单一次后人工检视并提交生成的Prefab。
- 旧家/筒仓/吉他坐标基于R4道路公式推算，未在Main中实走；理论上不与路口地标重叠，但`ProceduralFieldLayout.Landmark`不知道这些站点，无法在规则层保证麦田植被在站点处被清空。
- 通用远处地点暂无独立Prefab，可复用`ProceduralFields/FieldSwing、HarvestMachine、RadioOutpost`（未接）。
- 美术品质由用户试玩验收，不宣称商业品质。

## 请求Codex适配（共享文件）

1. 宿主（GameLoop适配器）：streamer Seed就绪后`Reset`，按上面示例在战斗裁决后`Step`+自动领取一条；死亡同tick优先。
2. `ProceduralFieldStreamer`/`ProceduralFieldLayout`：在三处唯一站点约10米半径内压低/清空麦秆与地标（可调用`HomecomingPlacesLayout.UniqueSites(seed)`），确保门前空地。
3. `IHomecomingViewSource.ReadNearby/ReadNarrative`：分别转发`PoiModule.ReadNearby()`与`HomecomingNarrativeCatalog.Default.Resolve`。
4. 结算：用`DiscoveredNarrativeIds()`/`ClaimedTemplateIds()`构造`ExplorationSettlement`。
5. Main：放置`HomecomingPlacesPresenter`并绑定生成后的三个Prefab；远处模板可选绑定现有景物Prefab。
6. 声音事件需求（不新建播放器）：`poi.discovered`（首次发现，轻微风铃/广播杂音）、`poi.claimed`（遗物入袋），由AudioDirector消费。
