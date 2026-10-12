# 项目状态与开发入口

2026-10-12。《麦田里的混响》，长期上下文见Docs/Project/REVERB_CONTEXT_V1.md。保持四位外部AI独立交付，Codex整合/验证，用户转发和阶段体验验收。

第二轮基线main@2d113dd656933c48b5a510c86ea5993d169e7c2a，四位交付已收齐；验证源码见Docs/Verification/Reverb-Followup-02/REVIEW.md。Unity EditMode130/130、三项实验PlayMode3/3、补充Profiler单项1/1均通过。V2真实16秒双轨回绕10次及三次恢复通过，最大主线程游标差1024，不能认证连续相位；原V1保留。V2端点归零，仍有斜率变化，未经监听不保证无爆音。

同场景off/on/on/off已采ProfilerRecorder/UnityStats，含设备、CPU/GC/绘制统计；GPU不可用，CPU/GC包含编辑器与测试开销，不当作特效净成本或发布性能认证。正式Core/Input/Views、旧场景、渲染配置和Packages未改，本轮无新Windows包。S2旧画面仍未通过美术验收。

用户已选择下一阶段：在独立M01试玩中验证回声＋传播是否有趣，正式美术后置。当前只有共享UI合同和下一轮任务卡，没有声称M01已可玩。1号做二维纯规则/宿主接口，2号做独立场景/面板/表现池，主美做辨识审查，音效总监做声响应表。Codex负责唯一CombatModule可选桥和自动验证，之后交独立Windows试玩供用户验收。

当前转发提示词：REVERB_TASKS_03.md。TASKS_01/02及第一轮审查保留历史。所有对外提示词由用户转发，未自动发外部消息；正式素材本轮不索要，Lore与大系统后排。
