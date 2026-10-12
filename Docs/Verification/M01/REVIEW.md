# M01 回声＋传播整合审查

2026-10-12 / Codex。基线 main@dc8b096d7529cc07298b2407928a73009e1072e6。本轮是用户已选择的独立机制试玩；正式美术、音乐和完整关卡后置。保留原四位外部 AI 协作与作者提交，由用户转发后续需求，未自动发外部消息。

## 四位原始交付

| 协作者 | 交付头 | 收到的内容与边界 |
|---|---|---|
| 1号 / PR24 | 5c4a21cda4f67e75e6a43271ced3dc99dd18349d | 纯 C# 二维规则/宿主接口、16 项作者用例；作者仅报告 dotnet 检查，未运行 Unity；提到的 DotnetRunner~ 未提交，文档已纠正 |
| 2号 / PR25 | cd0e0ebd80438eeb84a7db00a367bb3ed1d07fbb | 独立装配脚本、只读面板和特效池；作者无 Unity，原交付没有生成场景 |
| 主美 | 940f4f9f22ba85ca59a187e795014d2399ef9ce0 | 机制辨识建议表，无 M01 实机截图/验收；颜色、HDR、材质阈值是提案，不是批准常数 |
| 音效总监 | 595683c78c2b98c703f9eaac8c3884cabf2f2cf0 | 既有 TEST_ONLY SFX 的时序/限频建议；未监听，未提交正式音频；已纠正“已验收”和 V2 全开峰值 -3.61 的错误说法，实测为 -2.630 dBFS |

## 实际接线

- 唯一链仍为 GameLoop → RunSimulation → CombatModule。新增可选 ICombatRuleExtension，只在 M01 安装。直接子弹伤害只扣一次，派生伤害在 T6 后、T7 前同步走同一 HP/死亡路径；死亡、击杀和掉落仍由 T7 统一处理。ApplyRuleDamage 在规则结算窗口外拒绝调用，表现回调不能借此补伤害。
- 四模式为 None/Echo/Propagation/Combined。初始伤害 10、各目标存活时总伤害 10/15/14/23；传播不递归。0.1 秒直接传播、0.5 秒原目标回声、成功回声后再延迟 0.1 秒传播。目标 Serial+Generation 校验、最近目标及同距 Serial 决定由规则层处理。
- 每局新 CombatModule 与 M01CombatBridge；只有“下一局模式配置”静态传递，没有静态局内队列或 HP。切换与普通重开走现有场景重载，种子 12345。暂停/终局不推进规则，重开清空计数、预算与特效。
- 实际场景已由 Unity 生成，初始三个近邻目标、8 敌上限、每 2 秒 2 敌，玩家根缩放 1。M01 冻结 XP/升级，固定武器以便比较；默认 S1/S2 保留原配置。
- 已结算事件增加 EchoHit/PropagationHit/EchoPropagationHit 及起点/代号字段，经过原唯一分发处驱动 CombatViews、M01DerivedFx、AudioDirector。6 个双环与 6 条连线在 Awake 预热；紫红扩张双环、金色 0.05 秒定向增长连线，寿命分别 0.28/0.16 秒。M01 单独保存透明材质确保打包包含该变体，未修改 E01 材质。
- 仅 M01 启用额外 AudioDirector 通道；现有音效、2D 声源、-18dB 总 trim、音高区分，最多两声部、限频及池满丢弃新声。没有建议中的 5ms 抢占淡出/回声低通；没有 BGM、硬件抓轨、True Peak 或监听认证。

桥接整数伤害采用远离零四舍五入，当前基础伤害 10 且禁止升级，满足本轮对照；尚未承诺任意小数伤害装备系统。机制乐趣、平衡和最终艺术质量均未通过用户验收。

## 自动化证据

Unity6000.4.3f1 / URP17.4.0 / Windows / Direct3D11。PlayMode 最终场景执行源码 1515357f5a437b86c4e12b0e0d1d4638c81e4bc1；之后 5167de42ad80f5165d1cb1a014f6960be1702659 只修正显式启用的独立包抓图工具，普通运行行为不变。

| 检查 | 结果 | 证据 |
|---|---|---|
| 全部 EditMode | 153/153，0 失败；原 130＋作者 16＋真实 CombatModule 桥 7 | editmode.xml |
| 实际场景与旧回归 PlayMode | 15/15，0 失败；原集成 14＋M01 四模式重开/冻结 1 | playmode.xml、playmode-observations.txt |
| meta | 418 个唯一 GUID，无重复，实验资产无缺 meta | metadata-check.json、check_metadata.py |
| Windows x64 Development | 构建成功，0 错误，487 警告；显式只构建 M01 | build-report.json、build-manifest.json |
| 独立包真实输入与规则 | 四模式全部通过移动/闪避/暂停/根缩放和预期派生事件检查；运行日志无异常 | standalone-results.txt、standalone-log-check.json |
| 独立包相机渲染 | 四模式 URP 真实相机抓图非黑屏；已目视确认模型、阴影、地面和紫红双环 | 四个 *-world.png、Combined-echo.png |

关键用例包含真实子弹四模式总伤害、派生击杀唯一结算/掉落、规则窗口外禁止扣血、排队中暂停冻结，以及实际场景同种子重载、计数清零、特效冻结、单一 GameLoop/AudioDirector。旧场景回归覆盖之前的闪避问题与音频生命周期。

最终包源码为 5167de42ad80f5165d1cb1a014f6960be1702659。独立包组合/无模块/回声/传播成功次数分别为（回声9、传播12）/（0、0）/（11、0）/（0、9）；这些是不同帧时间下的冒烟记录，不是 DPS 或平衡数据。Combined-propagation-start.png 在连线刚启用时抓取，仅证明启动时画面，不证明完整路径已足够可读。

构建 487 警告中 485 来自已有 com.unity.ai.inference/Sentis 资源着色器，另两项为未配置 RuntimePipelineConfig 和无 Cloud 符号上传令牌。本轮没有升级/删除依赖或伪报零警告；URP 相机抓图实际成功。这些不当作发布包认证，完整消息保留在 build-report.json。编辑器另有作者/旧测试的 FindObjectsSortMode 过时 API 提示，不是编译错误。

## Windows 包与人工验收

独立包位于工作区外部 Builds/M01-MechanismPlaytest，只构建 M01 一个场景，不修改默认 Build Settings。普通打开不执行测试输入；仅显式传入 --m01-smoke-output 才添加虚拟键盘、记录并自动退出。使用真正 InputSystem 键事件验证 WASD/Space/Esc，不直接改玩家坐标。

首次后台批处理无 Keyboard，抓到测试工具的空设备异常；已经修正为仅测试模式创建虚拟键盘。隐藏窗口的背缓冲截图为黑色，不能作为画面证据；改用 URP StandardRequest 抓取实际三维相机。该抓图不包含 IMGUI，中文字体、按钮可读性和点击感受仍由正常窗口试玩验收。初次补充打包日志还暴露了 Unity 6 的计数类型 int/uint 编译错误，已修正并重新构建；失败尝试未作为通过证据。

操作与三分钟验收步骤见 Docs/Integration/M01_ACCEPTANCE.md。当前只需用户判断组合是否有趣、反馈是否可辨及闪避手感，再决定下一轮四位协作者任务；不自动扩内容或回到旧雕塑制作任务。
