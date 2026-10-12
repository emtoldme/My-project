# 给 Codex 的最小接线

秋收之路的 `GameLoop` 物体上已经有 `RC01GrowthHud`。请把实现了 `IRunContentViewSource` 和 `IRunContentCommands` 的宿主加在同一个物体上。不要把 `RC01GrowthPreviewSource` 放进这张场景。那个组件只属于 `RC01_GrowthPreview`，用来看排版，选择之后不会发下一手牌。

没有宿主时界面写“等待成长接线”，按钮是停用的。接上之后，三选一、重抽、放逐、跳过都只调用合同里的四个方法。界面不写 timeScale、HP、XP，也不再监听一个 Esc。暂停仍走现有输入。

升级队列请接到这套界面，不要同时留着旧的三选一。一次点击只消费一个奖励。旧的四模式按钮不要加回来。

这张场景没有把经验宝石改成 0。请保持 XP 可用。场景里的敌人上限只是预览用的低数量，不是 SCENARIOS.md 里的分钟表。分钟表、收割线、货道、排气和传输点都还是 planned，需要规则 Token：`id`、`warn/active/resolve`、成功、失败、放弃。表现不能自己计时扣血。

多武器命中要带武器或来源 ID。现在的界面不会从 `EnemyHit` 猜测是哪一把。

另外三张预览没有 GameLoop。以后接战斗时，把对应环境 Prefab 放进同一套循环，不要为每张图再复制一个状态机。市场摊位和粮仓柱是 TEST_ONLY 方块，没有碰撞，也不能当成正式建筑。

麦穗实例仍固定 LOD0。LOD1 比 LOD0 矮一截，这次没有改用户模型。
