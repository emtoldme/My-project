# 给外部 AI-B：HC-B 遗物、额外物品栏与特殊能力

请直接实施，不只写方案。Unity6000.4.3f1/URP17.4。先读AGENTS.md及`Docs/Project/HomecomingNext/README.md`、`RELEASE.md`、`CONTRACTS.md`、`CONTENT_SEED.md`。从`hc00-v1`建立独立目录和`gameplay/hc-b-relics`。你负责遗物规则；宿主仍是GameLoop→RunSimulation→CombatModule，不再造一套角色属性/伤害权威。

独占新增`Assets/Waffle/Scripts/Rules/Homecoming/Relics/`、`Assets/Waffle/Tests/EditMode/HomecomingRelicsTests.cs`、`Docs/Project/HomecomingNext/DELIVERY_B.md`。不改共享契约、现有GrowthStats/Combat/RunSimulation/GameLoop/Main/HUD、A文本目录或C存档。适配需求精确列出，由Codex统一实施。

## 要交付

1. 实现`IRelicInventory`：开局2个额外遗物槽，老房子和筒仓各解锁1槽，最大4。保持现有4武器/6被动槽不变。全部局内遗物与解锁格数重开清空，永久升级本轮不加遗物槽。
2. `TryGrant`对扩槽、唯一物品入袋、首次自动装备原子处理。ClaimId去重，重复成功请求幂等返回true；旧RunId、未知ItemId、无效扩容、伪造模板组合拒绝。相同模板不同地点不能刷格，规则不能只相信AdditionalSlots。背包有限，装备满则入收纳，收纳中物品可换装，不丢奖励。
3. 可装备/卸装/替换，唯一物品不能双占格。提供不可变视图与纯额外加成，常规升级与永久加成均不写入B内部。最大HP变化的当前生命处理、组合上限由宿主统一执行，B提供明确规格。
4. 首批针线盒+8%HP、手套+15%拾取、旧吉他启用闪避残响。所有ItemId和NarrativeId按种子固定，正文由A提供，不在B复制叙事目录。
5. 旧吉他效果交付有限纯规则触发辅助类/规格：仅真实成功闪避后1次、初版8秒冷却/3米半径/0.5基础伤害/最多12目标，Playing规则时钟，暂停/升级冻结，终局/重开清空。不能自己在Update扫敌扣血，不能由VFX触发，不再触发自身/P17的派生循环；说明与P17/W06的既有事件如何区分，由Codex接Combat。不能只交一个永不消费的bool后宣称能力完成。
6. 提供便于D显示的能力描述与数值预览查询，可在B自有目录定义额外只读类型。显示数据必须对应实际输出，不能有假“装备成功”。

## 验证与交付

模块编译、2→3→4槽、重入/重复通知/旧局拒绝、同模板多站、满格收纳、换装无重复加成、不可变快照、特殊效果冷却/去递归/暂停清理。给真实宿主接线方案和最小调用示例；本包不直接修改Main，交付时明确哪些能力已纯规则可用、哪些仍待Codex战斗接入。无需跑全武器或全局性能矩阵。

提交分支/完整SHA/PR、文件清单、接口样例、验证记录、已知问题、宿主适配请求。PR目标`gameplay/homecoming-next-ready`，禁止推main、强推或合其他包。
