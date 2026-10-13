# RC02 内容装配

> **Codex 整合更新，2026-10-13（覆盖下方交付时状态）**：四位交付已接入同一宿主，当前 6 攻击 / 14 被动 / 3 进化 / 4 拾取。Unity 已生成目录、独立样板场景和报告；W05/W06/P17 已接真实规则，已不局限于预览。

新增菜单 `Waffle/RC02/Apply Integrated Components` 生成 Unity 模板、刷新目录并接线两个场景，不调用旧实验 Setup。外部清单优先，seed 仅补缺；同优先级重复匹配键报错。导入先校验，错误时保留旧目录；资源暂缺可保留原引用并报告。重复刷新得到同样目录，不生成新 GUID。

运行时只读序列化目录，不读 JSON。当前 33 条记录为 10 美术、19 音效、4 音乐档案，记录数不等于独立玩法数。六张新测试图标已进入升级卡。

`RC02CatalogPresenter` 按预算预热并复用实例，通过 `IPresentationEventOwner` 明确接管事件；旧桥仅跳过确切接管的表现，不再因旧组件存在整体屏蔽目录。暂停与升级面板都冻结规则时钟表现，终局/重开清理。W06 残留和连线由真实规则快照驱动，粒子不决定伤害。

`RC02AudioConfiguration` 在唯一 AudioDirector 初始化前注入规则，不增加播放器；保留 R1 变体、节流、空间距离、死亡暂停清尾和经验固定增益。首图使用 harvest_road 档案，其余三图只是预置。音乐仍为 TEST_ONLY 整段循环，BPM 未知，无动态分轨或对拍。

`RC02EncounterSheet.asset` 仍是规划数据，尚未替换首图既有刷怪规则。正式素材与全面验证后置。修改清单后需刷新、重新装配和打包，当前不支持运行中热加载。

---

以下保留 2 号原始交付记录，其“未生成/未接线”说明属于整合前历史。

基线 `6447af8a96496b2c606de4e586b4ea7eaae8aaf9`。分支 `feature/rc02-content-pipeline`。

菜单：

- `Waffle/RC02/Refresh Content Catalog`
- `Waffle/RC02/Open Assembly Lab`

批处理可以先跑刷新，再打开样板。刷新会读本目录的 `seed-package.json`。若主美或音效已经交了这些目录里的 json，会一起导入：

- `Docs/Design/RC02/Art`
- `Docs/Design/RC02/Audio`
- `Assets/Drafts/RC01/Art/ComponentsV1`
- `Assets/Drafts/RC01/Audio/PipelineV1`

导入结果写进 `Assets/Drafts/RC01/Pipeline/RC02ContentCatalog.asset`。同一资产反复刷新，不新建第二份，GUID 保持不变。运行时只读这份资产，不读 JSON。

匹配键是 `kind + eventType + contentId`。精确 ID 优先于 `*`。同一优先级有两条就报错，不用最后一条盖住前一条。音乐用 `profileId`，`eventType` 留空。`maxInstances` 缺了时，画面默认 8、声音默认 4、音乐默认 1。`minInterval` 缺了会被当成 0，因为 JSON 读不出来“没写”和“写了 0”。

缺资源不会删掉已经绑上的引用。未知参数会记在条目的 issues 里，不会拿去改伤害。bpm 大于 0 也只是记录，不对拍。

装配样板是 `Assets/Drafts/RC01/Scenes/RC02_AssemblyLab.unity`。第一次从现有秋收之路另存，不调用会重写旧场景的 Setup。原来的 GameLoop、真实三选一和 F11 都留在复制出来的场景里。本工具不再加一套全屏监听。

W05、W06 只在 Inspector 里预览。没有对应规则事件时，不会在局里冒充已经造成伤害。

这里没有 Unity。目录资产、样板场景和报告都还没生成。

## 换一条内容要改什么

1. 规则侧加配方和说明。表现侧选模板，在自己的 json 里改路径、预算和来源。
2. 跑 `Refresh Content Catalog`。看 `RC02_REFRESH_REPORT.txt` 有没有重复键或非法枚举。
3. 打开装配样板，在目录 Inspector 里预览这一条。
4. 要让它在局里替换旧特效，先看 [MIGRATION.md](MIGRATION.md)。现在旧表现还在，所以重叠事件不会再播一遍。

声音和音乐条目只是给现有 AudioDirector 的记录，这份工具不会新造播放器。
