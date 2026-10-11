# C-01A 给 Codex 的需求

## R-1 测试程序集（阻塞 Unity 里跑战斗用例）

`Assets/Waffle/Tests/Combat/` 下没有 asmdef，Unity 会把它编进 Assembly-CSharp，找不到 NUnit。请创建 `Waffle.Tests.Combat.asmdef`，建议与 `Waffle.Tests.EditMode` 相同：

- references：`Waffle.Rules`、`UnityEngine.TestRunner`、`UnityEditor.TestRunner`
- includePlatforms：`Editor`；`optionalUnityReferences`/`defineConstraints`：`UNITY_INCLUDE_TESTS`；`precompiledReferences`：`nunit.framework.dll`；`overrideReferences: true`
- 或者直接把 `Tests/Combat` 纳入 EditMode 程序集（移动文件需保留 .meta GUID）。

同时建议把 `Tools/RulesTests/WaffleRules.Tests.csproj` 加一行 `<Compile Include="../../Assets/Waffle/Tests/Combat/*.cs" .../>`，让 dotnet 快速检查也覆盖战斗用例（该文件不在我的可写范围）。

## R-2 接线（C-01B）

1. `GameLoop`/`RunSimulation` 持有 `CombatModule`，开局与重开时 `new CombatModule(combatConfig, seed)`；`CombatConfig` 建议作为 GameLoop Inspector 字段。
2. 在 T3 位置调用 `StepCombat`，T8 用 `Result.ContactDamage` 替代/叠加调试 `hurt`，无敌、上帝模式、§5-2 叠加开关由框架判定。
3. T9 失败时跳过 `StepGems`；T10 用 `XpCollected` 走现有加 XP；T13 无论是否提前跳出都调用 `Release()`。
4. 转发 `Result.Events` 到 `Presentation`（填 `Frame`）。
5. 视图：按 `PoolIndex` 预热敌人/子弹/宝石视图，每帧按只读状态同步位置与显隐；`Phase`/`IsElite` 换外观；`IsBlinking` 闪烁。
6. `ViewRadius`：按 §1.4 用相机视锥四角与地面交点计算（正交 55° 下需重新算，14 只是假设）。
7. 调试命令：`spawn n` → `DebugSpawnEnemy`；`setcap n` → `Config.aliveCap`；`spawngems` → `DebugSpawnGem`。
