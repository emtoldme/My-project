# 内容与视听管线

规则位于 Assets/Waffle/Scripts/Rules。GameLoop → RunSimulation → CombatModule 是唯一时钟、状态和结算链；表现只读规则事件。现用组件详情见 Components.md。

三个清单位于 Assets/Game/Content/Manifests：art-manifest.json、audio-manifest.json、music-manifest.json。菜单 Reverb/刷新视听目录 将它们校验并导入 Game/Content/RC02ContentCatalog.asset；运行时不读 JSON。没有 seed/复制样板/场景重建步骤。

格式 v1：根 schemaVersion=1、packageId、entries[]；条目含 id、kind(art/audio/music)、contentId、eventType、profileId、assets[{role,path}]、parameters[{key,value}]、maxInstances、minInterval、pausePolicy(freeze/stop)、status、source。素材路径必须指向实际 Assets 文件。匹配 kind+eventType+contentId，精确来源优先于*；音乐用profileId。重复/非法值报错并保留旧目录，缺资源可保留原绑定并提示。

美术资产位于 Game/Art/Effects，RC02CatalogPresenter 预热预算池并复用实例；通过 IPresentationEventOwner 接管确切事件，旧反馈只跳过被接管的表现。暂停/升级冻结、终局/重开清理。W06 由真实残留快照显示节点/连线，升级范围同步；伤害只由规则判断。

实际事件：W01 Shot；W04 WeaponZone（旧桥保留预警）；W05 EnemyHit+W05（From上一跳）；W06 WeaponZone+W06及残留快照；P17 WeaponZone+P17；P12 EchoHit；P13 PropagationHit。六张图标直接用于升级卡。

RC02AudioConfiguration 在唯一 AudioDirector 初始化前注入清单。保留R1变体/节流/优先级、30~65米空间参数、死亡暂停清尾、经验固定增益。首图使用harvest_road，另外三个音乐档案仅预置。音乐仍整段TEST_ONLY循环，bpm=0，不支持动态分轨和拍点调度。

替换美术：编辑当前Prefab/材质即可；更换路径或目录参数则编辑清单并刷新。新增玩法仍需规则实现、说明、实际事件与白名单接线，不能仅加目录条目冒充实装。当前33条记录不是33种玩法。
