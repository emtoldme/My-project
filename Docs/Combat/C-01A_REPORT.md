# C-01A 交付报告（最小战斗模块）

- 分支：`feature/c01-combat-core`，PR #9（基底 main，未合并）
- 基线：`origin/main@b6df1fda73c6b0cf5d1e25227c2114a87dba0564`
- 提交：`c5a5ed7` 战斗模块代码；`650a1c5` 19 条关键用例；`81b1d66` 接口说明 README 与 REQUESTS
- 可写范围：只改 `Assets/Waffle/Scripts/Rules/Combat/`、`Assets/Waffle/Tests/Combat/`、`Docs/Combat/`；框架、场景、Prefab、asmdef 未动。

## 接口
命名空间 `Waffle.Combat`，编入 `Waffle.Rules`（noEngineReferences: true），不依赖 UnityEngine。
- `new CombatModule(CombatConfig, int seed)`：每局新建，无 static 局内状态。
- `CombatInput`：`Dt`、`RunTime`（本帧开始时的 now）、T2 后的 `PlayerPos`（XZ）、`ViewRadius`（≤0 时按 14）、`Stats`（伤害、开火间隔、穿透、磁吸半径）。
- 三个调用（T9 判定失败须跳过 T10，因此拆分）：
  - T3–T8 `StepCombat`：返回本帧最大接触伤害（只算一次）、伤害总和、接触数与来源、击杀数、表现事件。模块不判无敌。
  - T10 `StepGems`：返回本帧拾取 XP。
  - T13 `Release()`：每帧必须调用，含失败/胜利帧。
- 表现事件复用框架 `PresentationEvent`（射击、命中、击杀、拾取），`Frame` 由框架填写。
- 只读状态 `Enemies`、`Projectiles`、`Gems`，带 `Serial`、`Generation`、`PoolIndex`；`TryGetEnemy(serial, generation)` 拦截回池后的旧引用。
- 普通敌人上限 30，事件刷怪最多超出 20；其余数值为 v0.2.1 设计初值，详见 README。

## 已验证（仅 dotnet，未在 Unity 中运行）
- 仓库外临时工程 `/tmp/c01tests`：104 通过、0 失败（原框架 85 + 战斗 19）。
- 用例：无目标不报错；同帧两弹击杀同一敌人只结算一次；下一发可合法再次命中，同一弹不重复命中；穿透与高速扫掠命中；多敌接触只报最大一次，本帧死亡敌人不造成伤害；经验拾取、宝石过期、上限合并 XP 守恒；重开为干净实例，同种子结果一致；上限 30 截断；旧引用拦截。
- 3 分钟冒烟（不升级、不掉血）：约 100 击杀、81 XP、同场最多 50 敌、无对象池不足。仅检查接线，不作平衡数据。

## 缺项
- 未在 Unity 中编译或运行。
- `.meta` 为手写、GUID 随机；按约定应由 Unity 生成，请 Codex 导入后确认，必要时由 Unity 重新生成。
- 精英、方向推进、包围圈事件已实现，仅经冒烟，无单独用例。
- `spawn`、`setcap`、`gemflood` 调试命令不在本模块解析；模块提供 `DebugSpawnEnemy`、`DebugSpawnGem` 和可改配置。
- 规范未定、由本模块决定：子弹池空时该发仍消耗冷却。

## 需 Codex 处理（详见 REQUESTS.md）
1. R-1：新建 `Waffle.Tests.Combat.asmdef` 或并入 EditMode 程序集；建议 `Tools/RulesTests/WaffleRules.Tests.csproj` 加入 Tests/Combat。
2. R-2（C-01B 接线）：开局/重开新建模块；按 T3、T8–T10、T13 调用；事件转发并填 Frame；视图按 PoolIndex 同步；按正交 55° 相机重算视野半径；接入调试命令。
