# HC-C 交付：局外经验、升级与可靠存档

- 分支：`gameplay/hc-c-profile`
- 基础基线：`hc00-v1`（`9516b914ca57be950aafdb30f82824b5a8237900`）
- PR 目标：`gameplay/homecoming-next-ready`

## 改动目录清单

| 路径 | 内容 | 说明 |
| --- | --- | --- |
| `Assets/Waffle/Scripts/Rules/Homecoming/Profile/` | `ProfileConfig.cs`、`ProfileData.cs`、`ProfileFileStorage.cs`、`HomecomingProfile.cs`、`IProfileStoragePort.cs` | 纯规则实现 `IHomecomingProfile`，零 UnityEngine 依赖，确定性数值与事务处理 |
| `Assets/Waffle/Tests/EditMode/HomecomingProfileTests.cs` | 9 组核心单测（全通过） | 覆盖首档、跨级升级、首次永久去重、重复RunId、最小失败探索判定、事务写盘失败回滚、备份自动恢复、版本防护等 |
| `Assets/Game/Presentation/Homecoming/Profile/` | `HomecomingProfileAdapter.cs`、`Reverb.Homecoming.Profile.Presentation.asmdef` | Unity 生命周期与路径注入适配器（`Application.persistentDataPath`），不创建第二个 GameLoop |
| `Docs/Project/HomecomingNext/DELIVERY_C.md` | 本文档 | 完整的数值公式、存档方案、恢复策略与 Codex 接线请求 |

*未修改 `HomecomingContracts.cs`、`GameLoop`、`RunSimulation`、Main 场景或任何其他外部 AI 的独占目录。*

---

## 1. 经验等级公式与修补点（Progression & Costs）

### 经验与等级递增表
- **升级阈值公式**：`Level 1 -> 2: 100 XP`，后续每级递增 `25 XP`：
  $$\text{XP Required for Level } L \to L+1 = 100 + (L - 1) \times 25$$
- **前 5 级升级预览**：
  - Level 1 → 2: 需要 100 XP（累计 100 XP），获得 1 点修补点
  - Level 2 → 3: 需要 125 XP（累计 225 XP），获得 1 点修补点
  - Level 3 → 4: 需要 150 XP（累计 375 XP），获得 1 点修补点
  - Level 4 → 5: 需要 175 XP（累计 550 XP），获得 1 点修补点
- **修补点获取**：每升 1 级自动获得 **1 点修补点**。局外经验完全独立于局内 XP，购买升级点不扣减当前经验。

### 局外奖励规则与上限
- **首次发现独有地点（ClaimedTemplateIds）**：每个模板永久首次结算奖励 **30 XP**（老房子、老筒仓、老吉他）。
- **首次完成风味目标（CompletedGoalIds）**：每个目标永久首次结算奖励 **40 XP**。
- **击败首领通关（BossDefeated && Victory）**：**30 XP**。
- **有效探索失败保底（Defeat Guarantee）**：仅在探索时长 $\ge 15$ 秒时发放 **10 XP**；立即重开/秒死不发保底，杜绝无限空刷。
- **单局经验与点数上限**：单局最多结算获取 **250 XP** 与 **10 修补点**。

---

## 2. 三条 3 级永久升级定义（Meta Upgrades）

| 升级 ID | 对应内容 | 等级上限 | 各级成本 | 属性收益 | 满级累计收益 |
| --- | --- | --- | --- | --- | --- |
| `meta.mended-coat` | 补好的外套 | 3 级 | 1 / 2 / 3 点 | 每级 +2% 最大生命 | +6% 最大生命 |
| `meta.travel-light` | 熟路的脚步 | 3 级 | 1 / 2 / 3 点 | 每级 +1% 移动速度 | +3% 移动速度 |
| `meta.sorted-satchel` | 整理行囊 | 3 级 | 1 / 2 / 3 点 | 每级 +3% 拾取范围 | +9% 拾取范围 |

- 接口 `ReadStartingModifiers()` 提供开局固定加成供宿主拷贝，局内不热改。

---

## 3. 可靠存档方案与恢复策略（Atomic Save & Backup）

1. **原子写入流程（Zero Data Loss）**：
   - 临时文件写入：将完整 JSON 先写入同目录 `.tmp` 临时文件，并校验文件尺寸与有效性。
   - 滚动备份：如果存在旧的正式存档，自动复制一份为 `.bak` 备份文件。
   - 原子替换：调用平台原子替换（`File.Replace` / 原子重命名），确保任何断电或崩溃不会留下损坏的正式存档。
   - 清理残留：成功或异常后清理未完成的 `.tmp` 临时文件。
2. **损坏容灾与自动回退**：
   - 若主档丢失或 JSON 解析格式异常，自动扫描 `.bak` 备份文件恢复。
   - 若主档损坏且备份也被破坏，自动将损坏文件重命名归档为 `.corrupted.<timestamp>`，保留现场供分析，并安全生成全新空档案，状态标识为 `CorruptedAllPreserved`，提供可读错误给上层。
3. **版本防踩踏机制（Future Version Guard）**：
   - 检查 `schemaVersion`，若遇到大于当前版本的存档，自动进入 `VersionTooNewReadOnly` 模式，禁止写入覆盖，保护用户在新版游戏中的高版本数据。
4. **事务边界保证（Write-Before-Publish）**：
   - 无论是 `TrySettle` 还是 `TryPurchase`，均在**磁盘写入成功后**才将经验、修补点与购买状态发布到内存中。若磁盘写入失败，内存数据立即回滚并返回 `false`，支持界面重试。

---

## 4. 真实测试验证记录

在独立测试工具套件（`Tools/RulesTests/WaffleRules.Tests.csproj`）执行验证：
```
已通过! - 失败: 0，通过: 186，已跳过: 0，总计: 186
```
其中专门针对 HC-C 模块的 9 组测试全数通过：
1. `InitialProfileCreation_StartsAtLevel1WithZeroPoints`：首档从 Level 1 起步，初始 0 点；
2. `ProgressionCalculation_MatchesSeedThresholdsAndPoints`：严格匹配 100/125/150 升级阈值与点数发放；
3. `Settlement_AwardsFirstTimeUniquePlacesAndGoals_WithPermanentDeduplication`：首次探索 130 XP 跨级提升，重复探索相同模板与目标零奖励，永久去重；
4. `Settlement_DuplicateRunId_ReturnsOriginalReceiptWithoutDuplicateAward`：重复点击结算或相同 RunId 返回原收据，不加第二次经验；
5. `Settlement_FailureGuarantee_RequiresMinimumDuration`：5 秒秒退 0 XP，20 秒探索失败发放 10 XP 保底；
6. `Settlement_RejectsInvalidInputs_AndNonTerminalPhases`：防守 NaN、负时间、空 RunId 与非终局状态；
7. `TransactionalSave_FailedSaveDoesNotCommitMemoryState_AndAllowsRetry`：模拟写盘失败零内存泄露，重试写入成功后正确发放；
8. `Upgrades_PurchasingDeductsPoints_IncreasesRank_AndUpdatesModifiers`：购买扣点、阶梯成本判定与属性加成计算；
9. `FileStorage_RoundTrip_WithBackupRecovery_AndAtomicReplace`：实际磁盘往返写入、原子替换及损坏后自动回退 `.bak` 备份；
10. `FutureVersion_TreatedAsReadOnly_PreventsOverwriting`：高版本存档只读保护。

---

## 5. 请求 Codex 适配的共享接线说明

1. **生命周期启动**：
   在 Main 场景的初始化流中通过 `HomecomingProfileAdapter` 获取 `HomecomingProfile` 实例；
2. **开局属性加成注入**：
   出发前调用 `profile.ReadStartingModifiers()` 复制到本局 `RunConfig` 初始加成中；
3. **终局结算调用**：
   在终局状态判定后（`Victory` 或 `Defeat`），由宿主构造 `ExplorationSettlement` 并调用 `profile.TrySettle(settlement, out receipt)`；若返回 `false`，在结算界面提供重试按钮；
4. **局外购买门控**：
   升级购买仅在非战斗中（出发前或局外营地）开放调用 `profile.TryPurchase(upgradeId)`；
5. **记忆持久化保存**：
   玩家在日志中阅读叙事时，调用 `profile.TrySaveDiscoveredNarratives(narrativeIds)` 保证关游戏后记忆不丢失且不产生非法经验。
