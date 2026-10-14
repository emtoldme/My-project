# HC-B 交付

基线 `9516b914ca57be950aafdb30f82824b5a8237900`（`hc00-v1`）。分支 `gameplay/hc-b-relics`。

`RelicInventory` 实现 `IRelicInventory`。开局固定 2 格，不接受别的起始值。老房子的针线盒和筒仓的手套各加 1 格，到 4 格为止。同一件东西换一个地点再交，不会加格，也不会进第二份。伪造的物品和槽位组合直接拒绝，背包保持原样。

旧吉他的残响在 `TryConsumeDodgeEcho`。宿主只能在一次真实成功闪避的边上调用它。它返回半径 3 米、伤害系数 0.5、最多 12 个目标，然后进入 8 秒冷却。冷却用局内时间，暂停时不要调用。卸下吉他、死亡、终局或重开都不会打出。这个调用本身不搜敌、不扣血，也不会再发出闪避事件。

## 宿主还要接的地方

不要改共享契约。下面这些由 Codex 接到现有的 `GameLoop → RunSimulation → CombatModule`。

- 地图种子就绪后 `Reset(runId, 2)`。重开再调一次。
- A 准备好领取后调用 `TryGrant`。返回 true 才 `ConfirmClaim(accepted: true)`。返回 false 就不要封账。
- 同一拍如果玩家已经死亡，不要调用 `TryGrant`。
- 装备加成和普通升级、局外加成加在一起。针线盒是最大生命 +8%。第一次装备只提高上限，不把当前生命补满。卸下后把当前生命裁到新上限，再装备不回血。
- 手套是拾取半径 +15%，只在装备时计入。
- 成功闪避仍走现在的 `NotifyRunDodge`。P17 和 W06 继续听那个入口。吉他不要走它。在闪避真正开始之后单独调用 `TryConsumeDodgeEcho`，再用战斗模块打一次圆，不要把这次伤害记成新的闪避。
- 换装沿用宿主的阶段检查：Playing，或者日志暂停且玩家还活着。

```csharp
relics.Reset(sim.RunId, HomecomingContract.StartingRelicSlots);
if (relics.TryGrant(reward)) places.ConfirmClaim(sim.RunId, siteId, true);

if (relics.TryConsumeDodgeEcho(playing, alive, now, out var pulse))
{
    // radius = pulse.RadiusMeters, damage = baseDamage * pulse.DamageFactor,
    // targets <= pulse.MaxTargets. Do not call NotifyRunDodge again.
}
```

纯规则已经能处理格子、入袋、换装和残响冷却。主场景还没接。这里没有跑 Unity。
