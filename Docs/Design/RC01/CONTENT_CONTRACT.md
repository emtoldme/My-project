# RC01 并行交付合同 v0.1

Codex所有：本文、CONTENT_AND_COMBAT_DRAFT.md、Rules/RunContent/RunContentContracts.cs、共享父目录meta和最终GameLoop/CombatModule/AudioDirector接线。四位独立从包含TASKS_06的最新origin/main开始，严禁各自复制接口或互相合并。

## 固定ID与数据归属

W01..W06/P01..P18/D01..D06/E01..E06的功能含义见CONTENT_AND_COMBAT_DRAFT.md。地图ID为HarvestRoad/OldMarket/FourthGranary/RadioWasteland。名称允许保持功能名；ID不可因美术命名而变化。一个ID即一个内容家族，阶数/颜色/面额不能虚增计数。

1号唯一维护道具数值/阶数/资格/进化与掉落规则定义；2号维护场景布局/分钟段/刷怪与事件草稿；主美维护颜色/形状/材质/占位图示映射；音效维护逻辑事件到测试短音/循环的映射。三位表现作者只引用ID，不重抄一份有独立游戏效果的数值表。

目录定义至少包含schemaVersion、id、kind、displayName、tags、maxRank、各rank的effect参数、适用武器/前置、选择权重、说明文本、实现状态和owner。具体序列化由1号在自己目录确定并文档化，不修改只读合同去迁就实现。缺实现必须标planned，不伪装为可抽到的有效能力。

## 已发布纯C# UI合同

Waffle.RunContent中的RunContentChoiceView（Id/DisplayName/EffectText/CurrentRank/NextRank）、RunContentOwnedView（Id/DisplayName/Rank/MaxRank）、RunContentEvolutionView（Id/WeaponId/DisplayName/RequirementText/IsEligible）、RunContentView（OfferToken/Choices/Weapons/Passives/Evolutions/Rerolls/Banishes/Skips）均已入库；快照数组已防御复制为只读集合。规则作者按变化缓存快照，不在每帧/每个OnGUI分配全表。

进化缺项文本与可用状态由规则宿主给出，UI只显示，不自行重算等级/计时门槛。

IRunContentViewSource.ReadView()供UI读取；IRunContentCommands.TryChoose(token,id)/TryReroll(token)/TryBanish(token,id)/TrySkip(token)是唯一请求路径，bool表示接受/拒绝。1号实现规则核心命令，Codex适配器在实际LevelUp状态和现有升级队列内验证/调用。UI不能写Time.timeScale、HP、等级、RNG或自行出牌；LevelUp暂停和恢复仍属于RunSimulation。预览数据只能在命名TEST_ONLY的独立预览显式使用，正常场景无宿主就显示等待接线并禁用按钮。

OfferToken每次出牌/重抽/改变选项均更新，拒绝旧Token重复点击。有效选择消费一次待升级/宝箱奖励，重抽只花重抽次数，放逐只花放逐次数并补该卡合法候选，跳过消费一次奖励并发只读恢复请求；视图不能自行补HP。三选一剩余合法候选不足时由规则给合法兜底，不显示None空卡。同帧多次XP沿现有队列依次出牌，每次只消费一个奖励；同帧死亡/终局沿原RunSimulation优先级，不在终局继续弹升级或进化。

## 战斗与表现桥边界

GameLoop→RunSimulation→CombatModule保持唯一权威。1号新规则模块管理装备/选择状态，不保存敌人HP、不复制XP时钟、不创建自己的场景运行循环。多个武器最终由同一CombatModule/规则窗口调度，伤害进入同一路径，来源ID/序号/链预算的最小共享增补由Codex审查收口。

目前真实PresentationEvent已有Shot、EnemyHit、EnemyKilled、EchoHit、PropagationHit、EchoPropagationHit、GemPickup、Dodge、LevelUpShown/Chosen及Pause/Restart等。现有事件不都携带weaponId；新内容声音/VFX不能据名字或“当前武器”猜触发者，缺字段在REQUESTS.md提出，由Codex统一增补。尚无事件的效果只在预览展示，不自行用表现计时决定伤害。

6家族最终接线要求：多目标命中去重、同目标接触冷却、实际死亡只结算一次、派生不递归、事件携带武器/来源、暂停冻结、重开清理、池预算和旧代号失效。1号交最小接线请求，不越权覆盖现有CombatModule/RunSimulation；Codex负责共享扩展，必要时明确再派1号改。

## 提交与验证

Unity6/URP版本与Packages不变，默认BuildSettings不替换。场景/Prefab由Unity或幂等Editor入口生成，不手写YAML；没有Unity则源码＋实际可执行入口＋未验证清单交付，由Codex执行、生成meta与打包。共享父目录meta已有，不能另生成不同GUID覆盖。新asmdef只能放在自己获准的子目录，禁止在Assets/Drafts/RC01根建立asmdef把其他作者代码包含进去；跨作者程序集由Codex收口。

提交PR、分支、完整SHA、基线、修改范围、接口说明、实际运行与未验证项。必要规则用例覆盖槽满/满阶/抽卡去重/旧Token/进化一次/清局，初期不要求大套穷举或大量镜像测试。精细平衡、全图耐久、性能矩阵和完备测试在中后期按计划做。
