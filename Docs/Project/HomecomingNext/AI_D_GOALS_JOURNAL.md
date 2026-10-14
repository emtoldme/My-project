# 给外部 AI-D：HC-D 风味目标、叙事查看与成长界面

请直接实施，不只写方案。Unity6000.4.3f1/URP17.4。先读AGENTS.md及`Docs/Project/HomecomingNext/README.md`、`RELEASE.md`、`CONTRACTS.md`、`CONTENT_SEED.md`与Docs/HOMECOMING_R3.md。从`hc00-v1`建立独立目录/`gameplay/hc-d-journal`。以“旧物被认出来”为反馈，不做泛用打卡刷怪清单。

独占新增`Assets/Waffle/Scripts/Rules/Homecoming/Goals/`、`Assets/Game/Presentation/Homecoming/Journal/`、`Assets/Waffle/Tests/EditMode/HomecomingGoalsTests.cs`、`Docs/Project/HomecomingNext/DELIVERY_D.md`。不改共享契约、GameLoop/RunSimulation、现有HUD/Main、A文本/B背包/C档案。现有HUD整合适配由Codex负责。

## 要交付

1. `IHomecomingGoals`实现三条有限目标：回旧家/粮高记忆/未完的歌。消费宿主真实确认的HomecomingFact，按EventId去重，拒绝旧RunId。进度来自实际发现、领取、装备、遗物效果，不是UI倒计时或动画播放。仅观察不发奖励，永久首次奖励由C计算。
2. 每条目标有简短愿望、明确达成条件、完成后有叙事回应。剧情不靠大段弹窗截断走位；不用杀100只、收100次。可沿内容种子改良第二条为现有事实组合，复杂新事件先提Codex适配需求。
3. 地点邻近提示显示名称、可取得内容、已领取状态。自动拾取后的通知短且不遮玩家/危险；已拾取物品从日志/背包可随时再读。文案从A目录按ID查询，不复制另一套中文正文，也不能用数值代替叙事。
4. 日志可查看所有已发现地点、已获得物品、目标补文；既有攻击/被动/进化/拾取也能查看对应叙事，不显示未发现完整剧情剧透。所有物品的查看入口要覆盖鼠标与键盘，长中文正文能滚动、不被遮挡/截断；没有文本应显式报告内容缺失，不显示编造占位故事。
5. 独立额外遗物槽2→4，装备/收纳/换装显示真实B视图；发命令后按接受结果刷新，拒绝时有反馈。显示收益与特殊效果冷却含义，避免与原4武器/6被动槽混淆。暂停/日志中的换装允许由宿主明确门控，不能绕过LevelUp/终局限制。
6. 局外页显示C的经验、等级、修补点、3条升级当前/下一级收益及真实成本。开局前或终局可买并开始下一局；局内只查看。结算显示真实一次收据，分清失败获得/首次记忆/目标收益，不自己算经验动画后发奖。显示保存中/成功/失败、恢复档案状态及重试；适配C状态端口需协商，不能假显示“已保存”。
7. 面板只调用`IHomecomingViewSource/IHomecomingCommands`；提供可注入假宿主开发预览，正式GameLoop实现由Codex接入。打开日志走宿主暂停所有权，自己不写Time.timeScale；不与升级卡、Esc暂停、失焦/终局、F8观景争输入。首次出发前页面不能让战斗已经在背后计时，Codex提供宿主开局门控。
8. 延续墨蓝/旧黄铜/粉白、温暖故乡基调，减少满屏高亮卡框。新增提示优先为可读排版与短动效，不擅改既有天气/灯光/音频。使用支持中文的现有字体方案并在实际画面验证，不交缺字截图。

## 验证和交付

仅本模块编译、三个真实事件序列/去重/重开、所有叙事入口、2→4槽与满格、真实拒绝反馈、长中文滚动、保存失败提示、暂停所有权和F8隐藏。分别说明假宿主预览、模块实际行为、Main待接线项；截图不是规则接线证据。给Codex初始化/绑定代码与输入冲突说明，不直接改Main。

提交分支/完整SHA/PR、文件清单、各页截图或短演示、必要验证记录、已知问题、Codex适配请求。PR目标`gameplay/homecoming-next-ready`，不推main、不强推、不合其他包。
