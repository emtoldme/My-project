# Assets/UI — PR33 修正版（只读 HUD 视图）

基于审查意见修正。基线 SHA：`391eabc2aab272029d494f18c15fc23b2cae8bf2`（origin/main，RF01 整合后）。

## 修正要点

1. **不再写 Time.timeScale**，不监听正式 Esc。暂停按钮只调用 `IHUDActions.OnTogglePause()`，由 Codex 适配到唯一 `GameLoop.TogglePause`。
2. **不持有第二套局内状态**。只暴露 `Refresh(HUDSnapshot)`。健康/模式/计数全部来自外部快照。
3. **模式按钮**调用 `OnRestartWithMode(string)`，由适配层转成 `IMechanismPlaytestControls.RestartWithMode`。
4. **测试输入默认关闭**（`enableTestInput = false`）。仅独立预览场景由装配工具显式开启。缺引用、MaxHealth==0 均安全。
5. **颜色统一**：回声紫红、传播金黄、Combined 中间色。不再使用青→品红作为机制色。

## 最小接口（供 Codex 适配）

```csharp
public struct HUDSnapshot { ... }          // 见 HUDSnapshot.cs
public interface IHUDActions {
    void OnRestartWithMode(string modeName);
    void OnTogglePause();
}
```

HUD 自身不引用 GameLoop / CombatModule。

## 独立预览

无 Unity 时交付幂等 Editor 工具：

- 菜单：`Reverb/UI/Create Preview (Idempotent)`
- 生成 `Assets/UI/Prefabs/ReverbHUD_Preview.prefab`
- 生成 `Assets/UI/Scenes/UI_Preview.unity`
- 预览中 `enableTestInput = true`，H/J 模拟血量，M 循环模式文字（仅本地快照），按钮触发回调（当前无适配则空操作）。

**未验证**：实际外观、中文字体渲染、Canvas Scaler 缩放、与真实 GameLoop 的适配。纯色占位不等于正式锈蚀 UI 完成。

## 文件

- `Scripts/ReverbHUD.cs`
- `Scripts/HUDSnapshot.cs`
- `Editor/UIPreviewSetup.cs`
- 本 README
