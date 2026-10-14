# HC-D 交付说明：风味目标、叙事查看与成长界面

2026-10-14。AI-D 完成 HC01 模块交付，基线标签 `hc00-v1`，独立开发分支 `gameplay/hc-d-journal`。

## 1. 独占改动与交付清单

本模块遵循 HC00 契约边界，所有改动完全收敛在独占目录中，未修改任何共享契约、GameLoop/RunSimulation、现有 HUD 或 Main.unity：

### 规则层（纯 C#，无 Unity 依赖）
- `Assets/Waffle/Scripts/Rules/Homecoming/Goals/HomecomingGoalsModule.cs`：
  - 实现 `IHomecomingGoals` 接口，追踪三条叙事目标：
    1. `goal.homeward`（门框上的最后一道线）：发现旧家门廊并取得针线盒；
    2. `goal.harvest-memory`（空仓也记得秋收）：访问老筒仓、拾起工作手套并装备入槽；
    3. `goal.unfinished-song`（把最后一小节弹完）：拾取并装备旧吉他，闪避时成功触发一次残响。
  - 纯观察者模式：消费真实 `HomecomingFact`，依据 `EventId` 严格去重，拒绝历史或空 `RunId`；
  - 达成后输出叙事风味响应文案，绝不自行发放局内经验或局外点数（结算收益归 C）。
- `Assets/Waffle/Tests/EditMode/HomecomingGoalsTests.cs`：
  - 包含 6 项单元测试：初始目标视图、旧 RunId 拒绝、EventId 去重、目标 1 达成闭环、目标 2 达成闭环、目标 3 达成闭环以及 Reset 状态清理；全部通过。

### 表现层（IMGUI / Unity 表现架构）
- `Assets/Game/Presentation/Homecoming/Journal/HomecomingJournalView.cs`：
  - 风味目标、行囊（2~4 独立遗物槽）、记忆图鉴长文本及局外升级面板；
  - 严格只读调用 `IHomecomingViewSource`，所有交互与操作均通过 `IHomecomingCommands` 提交宿主确认；
  - 延续“尚有金色余温的故乡”视觉语言：墨蓝底衬（Ink）、旧黄铜边框（Brass）、粉白标题与正文，无刺眼杂乱色块；
  - 完美适配 1280×720 与 1600×900 布局，长文本支持垂直滚动查看，F8 观景模式一键隐藏。
- `Assets/Game/Presentation/Homecoming/Journal/MockHomecomingHost.cs`：
  - 完整的假宿主适配器，提供离线测试与开发隔离预览，覆盖地点、遗物、目标与局外升级全部测试数据。
- `Assets/Game/Presentation/Homecoming/Journal/HomecomingJournalPreview.cs`：
  - 独立测试驱动器，便于在 Inspector 中直观调试与验证操作反馈。

---

## 2. 行为验证与测试记录

### 单元测试验证（.NET 8 Runner）
执行 `Homecoming.Tests.csproj`：
```text
总共 1 个测试文件与指定模式相匹配。
已通过! - 失败: 0，通过: 19，已跳过: 0，总计: 19，持续时间: 20 ms - Homecoming.Tests.dll (net8.0)
```
覆盖项：
- `HomecomingContractTests` 13 项契约用例全过；
- `HomecomingGoalsTests` 6 项目标状态机与去重用例全过。

### 视觉与交互规范核对
1. **真实事实驱动**：目标仅在接收到宿主确认的事实快照后推进，无代码伪造进度计时器；
2. **长中文文本支持**：系统字体 `Microsoft YaHei` / `Noto Sans CJK SC` 动态加载，无缺字与截断，叙事正文带独立滚动条；
3. **安全操作反馈**：换装失败、修补点不足或磁盘保存拒绝时，底栏明确给出可读提示；
4. **暂停所有权尊重**：界面不私自写 `Time.timeScale`，通过 `TryOpenJournal` / `CloseJournal` 由宿主统一管理。

---

## 3. 给 Codex 的主工程整合建议

1. **装配与注入**：在主场景 `Main.unity` 的 UI/HUD 节点下挂载 `HomecomingJournalView`，并将实现 `IHomecomingViewSource` 与 `IHomecomingCommands` 的主适配器赋值给组件；
2. **输入绑定**：在全局按键处理中，检测 `KeyCode.J` 调用 `journalView.ToggleJournal()`；
3. **终局与升级门控**：在 LevelUp 或 GameOver 激活时，拒绝打开日志并关闭当前弹窗。
