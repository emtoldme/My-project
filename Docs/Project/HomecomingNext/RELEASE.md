# HC00 v1 共同基线

2026-10-14。冻结标签：`hc00-v1`，共同开发分支：`gameplay/homecoming-next-ready`，基于探索R4 `f33edd41915bd11ddc49e1a1a45c23f9e63731ce`。

- 仓库：https://github.com/emtoldme/My-project
- 标签源码：https://github.com/emtoldme/My-project/tree/hc00-v1
- 任务入口：Docs/Project/HomecomingNext/README.md
- Unity6000.4.3f1 / URP17.4；唯一主场景Assets/Game/Scenes/Main.unity。
- 共享代码：Assets/Waffle/Scripts/Rules/Homecoming/HomecomingContracts.cs。
- 契约测试：Assets/Waffle/Tests/EditMode/HomecomingContractTests.cs。

各包独立检出此标签并建自己的分支；不要复制他人整个工程，不假定origin/main含探索R4或HC00接口。A/B/C/D不得改共享契约和现有核心；适配请求由Codex在共同分支实施。

本基线提供地点稳定ID、叙事条目、只读视图、四包模块端口、统一表现命令以及终局数据结构。它不包含四包业务实现，也未接Main；探索R4原游戏行为保持。共享契约验证记录见本次PR与本机Reviews/HomecomingNext-2026-10-14/contract-tests.xml及compile-tests.log。

本轮Unity编译成功，定向EditMode契约检查12/12通过：负坐标/种子/站点身份稳定、ID分隔符保护、叙事正文必填、终局与背包快照防修改、暂停/升级/终局/死亡的交互门控。未运行全量回归、未重新构建Windows包，因为本次没有新增Main业务接入；后续按各包和阶段节点验证。
