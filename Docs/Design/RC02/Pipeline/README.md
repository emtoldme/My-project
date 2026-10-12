# RC02 内容装配

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
