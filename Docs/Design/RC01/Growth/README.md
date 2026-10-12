# RC01 成长规则核心（1号）

基线 `b8d3931c83bf8fc94d678f6c9c127fa6b6a478d4`。分支 `feature/rc01-run-content`。目录见 CATALOG.md，跨目录请求见 REQUESTS.md。

## 文件
- `Rules/RunContent/Data/`：`ContentDefinition`（schemaVersion/id/kind/displayName/tags/maxRank/逐阶参数/前置/权重/说明/状态/owner）与 `RunContentCatalog.Default`。纯 C# 只读，无序列化包。
- `Rules/RunContent/Logic/`：`RunGrowthState`（实现 `IRunContentViewSource`、`IRunContentCommands`）、`GrowthStats`/`WeaponRuntimeParams`（属性聚合+上限）、`GrowthResult`、`GrowthRng`（SplitMix64，跨 Mono/.NET 一致）。
- `Rules/RunContent/Tests/`：Editor-only `Waffle.RunContent.Tests.asmdef`（引用 Waffle.Rules + nunit.framework.dll，UNITY_INCLUDE_TESTS），`DotnetRunner~/`（Unity 忽略）。

## 程序集选择
`RunContentContracts.cs` 没有独立 asmdef，编译在 `Waffle.Rules` 中（noEngineReferences）。Data/Logic 同样不建 asmdef，跟随进入 `Waffle.Rules`，命名空间 `Waffle.RunContent`，不引用 UnityEngine，NUnit 不进运行时。Tests 子目录的 asmdef 把测试从 Waffle.Rules 切出。

## 规则
- 每局 `new RunGrowthState(seed)`：W01 1阶，重抽3/放逐2/跳过2，4武器/6被动槽。武器5阶、被动3阶、进化1阶。
- 出牌：类别权重新武器30/武器升阶35/被动35，在非空类别间重归一，类内按定义权重。三张ID不同。保底：有已持有可升级项时必含一张；前5次出牌中武器槽未满且有合法新武器时，在第一次出现前强制一张新武器（允许拒选）。满槽/满阶/放逐/planned/已进化掉的原武器不进候选。合法候选不足3张时补一张 `HEAL`（回复10%，宿主执行），无空卡。
- Token：每次出牌/重抽/放逐都 +1；旧 token、重复点击、不在当前选项中的ID都返回 false 且不扣资源。
- 跳过：消费一次奖励，产出 `Skipped`（HealFraction 0.05），HP 由宿主改。
- 放逐：只能放逐当前选项，被放逐ID永久离池（含已持有项的后续升阶；会使依赖它的进化不可达，UI应提示）。补一张合法候选。
- 精英：`TryEnqueueEliteReward(token, runSeconds)` 同 token 只受理一次；满足进化条件时出进化卡（不可重抽/放逐，可跳过），否则出普通成长卡。进化替换原槽、一次消费、原武器不再出现。
- 终局 `CloseForRunEnd()` 丢弃剩余奖励，不再出牌。
- 视图按变化缓存，读取不推进 RNG。进化视图 `IsEligible` 只判断装备条件，时间门槛写在 RequirementText，以宝箱时宿主秒数为准。
- 不存 XP、敌人 HP、战斗时钟。

## 验证
`cd Assets/Waffle/Scripts/Rules/RunContent/Tests/DotnetRunner~ && dotnet test` → 9/9 通过（.NET SDK 8.0.425，Linux）。现有 `Tools/RulesTests` 由于 glob 也编入本测试：126/126 通过（原117+本9）。**未在 Unity 编译/运行**，meta 为手写新 GUID。
