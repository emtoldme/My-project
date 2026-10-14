# 给外部 AI-C：HC-C 局外经验、升级与可靠存档

请直接实施，不只写方案。Unity6000.4.3f1/URP17.4。先读AGENTS.md及`Docs/Project/HomecomingNext/README.md`、`RELEASE.md`、`CONTRACTS.md`、`CONTENT_SEED.md`。从`hc00-v1`建立独立目录/`gameplay/hc-c-profile`。纯规则实现`IHomecomingProfile`，Unity适配器只负责存储路径/生命周期，不创建另一个GameLoop。

独占新增`Assets/Waffle/Scripts/Rules/Homecoming/Profile/`、`Assets/Game/Presentation/Homecoming/Profile/`、`Assets/Waffle/Tests/EditMode/HomecomingProfileTests.cs`、`Docs/Project/HomecomingNext/DELIVERY_C.md`。不改HomecomingContracts.cs、GameLoop/RunSimulation/Main、A文本目录、B遗物或D界面。存档文件不提交Git。

## 具体要求

1. 余响经验、等级、修补点及三条3级永久升级。按内容种子完成阈值/奖励/成本表，交付计算公式与前几级预览。永久经验独立于局内XP，升级点不能扣局内经验。单局奖励/输入范围有限，拒绝NaN/Infinity、负时间、非终局、空RunId与未知ID，不由调用方指定经验数值。
2. 真实终局结算：从宿主提供的ExplorationSettlement计算，首次地点/首次目标永久去重；重复RunId返回原收据，无论重开应用或重复按钮都不重发。有效探索失败有有限所得，立即重开/无探索不刷保底，杀怪不作为无穷局外经验源。首领与失败奖励有单局上限。不得从UI显示或日志猜测真实结算。
3. 购买验证成本/上限/余额，提供`ReadStartingModifiers()`；本局已出发后不热改，出发前或终局由宿主门控。升级ID固定，叙事正文交A，等级、下一阶实际收益、成本由你提供给D。没有同一数据的第二份缓存权威。
4. 单本地档案首版：schemaVersion、revision、经验/点数、升级、已发现叙事、首次地点/目标、已结算RunId及收据，校验集合重复、非法数值、超上限、不合理文件尺寸。局内装备/当前HP/敌人/波次不保存，本轮不做中途续局、云存档或多角色槽。
5. 可靠保存：同目录临时文件→完整写入/关闭→原子替换正式档→保留最近有效备份；首次无档创建、正式损坏回退备份、备份也坏则保留损坏副本并给可见恢复状态。未知更高版本不能直接覆盖，提供只读/阻止保存状态。明确Windows文件替换失败与临时文件残留的处理。不要“捕获异常然后悄悄重置一切”。
6. **先写盘成功再发布内存经验、点数或购买**；奖励账本和结算同一事务。模拟写入/替换失败仍可重试且不重复奖励。不截掉旧RunId列表导致历史重放。存储端口可注入内存/失败测试实现，Serializer选既有可用能力，不新增包依赖。
7. 存储根由Unity适配器传入Application.persistentDataPath，纯规则不得引用Unity。Save/Load不用主线程每帧写；结算/购买写盘，失焦/退出只保存已有已确认数据。提供独立的“记忆发现保存”去重事务方案，让已读文本在普通关闭后可保留而不伪造终局发经验；需要新增共享接口先提请求，不能私改共享文件。
8. 提供加载/恢复/保存失败状态与可读消息给D，包含重试方法；不要仅写console。重开前未落盘结算由Codex阻止清空并让用户重试。编辑器验证使用独立临时目录，不写用户真实档案。

## 验证和交付

只验证本模块：首档/重启读取、阈值跨级、购买余额/上限、失败局与空局、永久首次/RunId去重、终局快照被冻结、写入失败与替换失败回滚、损坏主档恢复、双损坏、未来版本保护、残留tmp、升级/叙事迁移。给出一条实际磁盘往返记录及模拟失败结果；不用全面回归现有战斗。

提交分支/完整SHA/PR、文件清单、创建档案与路径注入样例、保存schema/恢复策略、数值表、验证记录、未接Main的明确说明和Codex适配请求。PR目标`gameplay/homecoming-next-ready`；不推main、不强推、不触碰用户已有数据。
