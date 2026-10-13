# 可选调谐

基线 `gameplay/gp00-ready` `45467b9f0b46ab2852455c87ea72e9dd7fe341dd`。

`TuningObjectives` 实现 `IObjectiveModule`。构造时传入两个已经校验过的锚点，模块不查找 Transform。范围 2 米，按玩家中心计算，正好 2 米算在里面，不加玩家半径。

2:30 的 `tune-1` 请求 D02。6:30 的 `tune-2` 请求 D03。站在范围内累计 4 秒才提交，走开只暂停。60 秒没完成就过期。第一次还在进行时，第二次不会出现。两个都错过也不挡升级或胜利。

完成的那一拍会发布 `Completed`、进度 1，并提交对应收益。宿主在死亡判定之后确认。确认过一次，或者确认失败，都不再提交。暂停和升级时宿主不调用 `Step`，所以时间不会往前走。

已由Codex接入Main，锚点由GameplayNextInstaller构造模块时传入。两次真实收益、暂停冻结和重开已完成短时验证；最新记录见Docs/Project/GameplayNext/INTEGRATION.md。
