# GP-D (AI-D)：危险可读性与玩法界面基于 GP00 的接入说明与交付

**交付负责人**：AI-D（Gameplay Presentation & Danger Feedback）  
**交付分支**：`gameplay/gp-d-feedback`  
**共同基线**：`origin/gameplay/gp00-ready`（Commit `45467b9`）  
**最新提交**：已完成 rebase 与真实宿主只读快照适配接线  
**所有权目录**：`Assets/Game/Presentation/GameplayNext/`  

---

## 一、 GP00 共同契约对接状态

本轮已在独立分支 `gameplay/gp-d-feedback` 上完整对接 GP00 v1 宿主接口：

1. **宿主只读视图订阅 (`IGameplayViewSource`)**：
   * `GameplayFeedbackView` 在 `LateUpdate()` 中自动获取宿主 `GameLoop as IGameplayViewSource`；
   * 调用 `ReadGameplayView()` 消费权威快照（`view.Attacks`、`view.SpawnWarnings`、`view.Objective`、`view.Finale`）；
   * 在终局时调用 `ReadRunOutcome()` 消费权威结算数据（`RunOutcome`）。
2. **危险区域几何与时序映射 (`DangerIndicatorPool`)**：
   * 自动将 `HostileAttackIntent.Shape` 映射到预警池（`Circle`、`Capsule`、`Annulus`）；
   * 严格按照 `intent.ActivateAt` 与 `intent.EndAt` 区分 **Telegraphing（预警充能）/ Active（生效中）/ Resolved（已消散）** 三态；
   * 动态计算首领扩散环（`Annulus`）随时间的扩张内外径（`shape.ExpansionSpeed * age`）；
   * 严格遵守 **最大 12 个预警指示器** 的固定对象池预算。
3. **调谐目标与首领看板**：
   * 调谐目标读取 `view.Objective`（展示当前进度百分比、剩余倒计时、D02/D03 真实奖励说明，以及屏幕边缘离屏导引）；
   * 首领看板读取 `view.Finale`（展示真实 HP、阶段与招式说明，首领未登场或已被击破时完全隐藏，绝不展示假血条）。
4. **终局与重开清理**：
   * `ResolveGameplayEnding` 触发终局后，`DangerIndicatorPool` 彻底清理隐藏，杜绝残响；
   * `RunOutcome` 显示真实胜负原因、存活时长、击杀数、角色等级、完成目标数与首领战果。
5. **升级与暂停冻结**：
   * 检测 `Phase == RunPhase.LevelUp || Phase == RunPhase.Paused` 时跳过动画与倒计时更新，三选一卡片弹出时次要 HUD 自动弱化避让。

---

## 二、 验证核查表

| 检查项 | 状态 | 验证说明 |
|---|:---:|---|
| **分支基线** | ✅ 正确 | 基于 `origin/gameplay/gp00-ready`（`45467b9`）完成变基与开发 |
| **模块规则测试** | ✅ 149 项全过 | `dotnet test` 运行 149 项测试通过，无任何失败 |
| **接口合规性** | ✅ 100% 遵照 GP00 | 仅使用 GP00 规定的 `RunGameplayView`、`AttackSnapshot`、`ObjectiveSnapshot`、`FinaleSnapshot`、`RunOutcome`，无私有破坏性修改 |
| **工作区独立性** | ✅ 严格隔离 | 所有代码与说明位于 `Assets/Game/Presentation/GameplayNext/`，不触碰正式 `Main.unity` 与用户美术工作区 |
