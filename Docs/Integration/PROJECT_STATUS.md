# 项目状态与开发入口

2026-10-12。《麦田里的混响》，长期上下文见 Docs/Project/REVERB_CONTEXT_V1.md。保持四位外部 AI 独立提交、用户转发、Codex 整合/自动化/本地开发、用户阶段体验验收。

本轮四位交付已收齐，M01 独立回声＋传播试玩已接线并构建 Windows 包。规则只通过唯一 CombatModule 结算，实际派生事件驱动环/线和音效；四模式切换即同 seed 12345 重开，保留 WASD/自动攻击/Space 闪避/Esc 暂停，固定武器且冻结 XP/升级供比较。默认 S1/S2 未启用新机制。

EditMode153/153、实际场景及旧回归 PlayMode15/15 通过。当前实测、源码与独立包记录见 Docs/Verification/M01/REVIEW.md；用户三分钟体验步骤见 M01_ACCEPTANCE.md。集成重建入口 M01IntegrationBatch.Setup，独立构建入口 M01IntegrationBatch.Build，不更改默认 Build Settings。

本轮包含最小可选共享规则钩子和表现/音效通道，不能沿用上一轮“正式玩法源代码未改”的描述。旧默认场景、Packages 和共享渲染配置保持原样；Unity 自动序列化噪声已还原。旧包和原用户 Unity 窗口保留。

当前为机制体验待用户验收：是否改变走位、是否可辨认、是否值得继续。所有模型/地面/音效仍是 TEST_ONLY，M01 没有正式 BGM；S2 旧画面仍未通过美术验收。正式素材后置，本轮不索要或制作。下一轮四位 AI 需求依据本次体验反馈再发，不扩装备大系统/完整关卡/Lore。

第二轮 PR23 基线 dc8b096 的 V2 双轨与小场景观察保留于 Docs/Verification/Reverb-Followup-02；第一轮保留于 Reverb-Experiments-01。TASKS_01/02/03 是历史分工记录，不要重复执行。提示词仍由用户转发，没有自动发外部消息。
