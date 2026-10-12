# 1号 → Codex 最小接线请求（RC01 06A）

1号未改动 RunSimulation/CombatModule/GameLoop/合同/父目录 meta。以下全部待 Codex 决定与实现。

1. **创建与重开**：RunSimulation 开局 `new RunGrowthState(seed)`（seed 沿用现有局种子），重开重新 new；终局（胜/负）调用 `CloseForRunEnd()`。把它作为 `IRunContentViewSource`/`IRunContentCommands` 交给 2号 UI 适配器。
2. **接原升级队列**：T12 `PendingLevelUps>0 → LevelUp` 时，每消费一个待升级调用一次 `EnqueueLevelUpReward()`；LevelUp 面板改读 `ReadView()`，命令只经 `TryChoose/TryReroll/TryBanish/TrySkip`。命令返回 true 后 `DrainResults`：
   - `WeaponAdded/RankUp/PassiveAdded/RankUp/Evolved`：重算 `GrowthStats.From` 与 `WeaponRuntimeParams.From`；`PendingLevelUps--`；`PendingRewards==0` 时恢复 Playing（暂停/恢复仍归 RunSimulation）。
   - `Skipped`/`FallbackHeal`：按 `HealFraction×最大HP` 回复（不超上限），同样消费一次。
   - 旧 `UpgradeRules` 7 类升级建议在 RC01 正式局停用，避免两套成长叠加。
3. **多武器调度**：唯一 CombatModule 内按 `WeaponRuntimeParams` 每槽独立冷却计时（Playing 时钟，暂停冻结），同一规则窗口 T5/T6 结算；W02 同目标命中上限、W03/W04 同目标冷却/节流需在 CombatModule 内按 `(sourceId, slot, enemySerial)` 记账。不为每武器建碰撞/扣血组件。
4. **来源ID**：`ApplyRuleDamage` 与 PresentationEvent（Shot/EnemyHit/EnemyKilled/EchoHit/PropagationHit…）增加 `sourceId`（W/E/P/D 内容ID）与 `slot`；派生（P12/P13/P16）带 `rootSourceId`，派生不再触发派生，暴击仅直击。
5. **P12/P13 局内取得**：M01 `MechanismRules` 的回声/传播开关与系数改读 `GrowthStats.EchoCoef/PropCoef/PropRadius/PropBudget`（0=未取得），不再由固定组合预装。
6. **拾取结果**：D01–D04 在 T7 掉落/T10 拾取产出只读结果（`pickupId, x, z, serial`）；效果参数取 `RunContentCatalog.Default.Get("D0x").RankAt(1)`。D04 走 `ApplyRuleDamage`（sourceId="D04"），D03 走现有磁吸路径不重复加 XP。
7. **精英宝箱**：精英实际死亡（T7）生成宝箱，拾取时调 `TryEnqueueEliteReward(宝箱serial, 局内秒数)`；同箱只受理一次。
8. **抽卡池收窄**：未接线前，`new RunGrowthState(seed, null, null, allowList)` 只放实际生效的 ID，避免抽到无效果卡；接线后可把对应定义改为 `Wired`（我可按授权改 Data）。
9. **旧 Tools/RulesTests glob**：`Rules/**/*.cs` 会编入本目录 Tests（无害，现在 126 项）以及 `DotnetRunner~/obj` 生成文件（若本地跑过会重复特性编译失败）。建议排除 `**/*~/**` 与 `**/Tests/**`。
10. **meta**：我为新 .cs/.asmdef 手写了新 GUID meta，`DotnetRunner~` 无 meta（Unity 忽略）。请在 Unity 导入确认无冲突。
