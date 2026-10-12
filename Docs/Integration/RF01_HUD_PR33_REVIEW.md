# PR33 HUD 初审与后续接线要求

2026-10-12，提交 `586cccf11814518ec672a420fe75f3cb2c003967`，分支 `feature/ui-rustpunk-hud`。本轮 RF01 集成时发现这份额外交付，暂不接入战斗场景；PR 保持开启，源提交未修改。

已收到三个文件：HUD 脚本、手动装配 README 与脚本 meta。未交 Canvas/Prefab/场景、字体资源或实际 UI 截图，不能把运行脚本当成“已完成锈蚀质感界面”。

## 接入前必须修正

1. `TogglePause` 直接写 `Time.timeScale`，没有经过 `GameLoop.TogglePause` / RunSimulation 状态机。正式循环使用 unscaledDeltaTime 且依据 RunPhase 推进，这种暂停不能作为游戏暂停；还会与已有 Esc 输入竞争。HUD 必须只读真实状态，按钮通过最小适配调用唯一 GameLoop。
2. `currentHealth` / `modeIndex` 是自设测试状态，`CycleMode` 只改文字，并未切换真实机制。生产接线必须读真实 HP 和 IMechanismPlaytestControls.Mode；模式按钮调用 RestartWithMode。同种子重开和清理继续由既有框架处理。
3. 默认测试键开启，`pausePanel` 可选但 Esc 无空引用保护；`maxHealth=0` 会除零。测试输入只能在独立预览显式开启，正常试玩禁用，缺引用不能导致异常。
4. 青/品红的模式映射与当前“紫红回声、金黄传播”不一致。先沿已接入反馈统一语义，禁止用 UI 另起一套机制配色。

## 由用户转发时的任务提示

继续修正 PR33，只修改 Assets/UI/ 及本模块文档，不改 Rules、GameLoop、Input、AudioDirector、M01 或默认 BuildSettings。交可复现的独立预览装配入口和场景/Prefab；没有 Unity 则只交幂等 Editor 装配工具，由 Codex 执行。HUD 视图只暴露 Refresh(真实快照) 和按钮回调，不持有第二套局内状态，不直接写 Time.timeScale、不监听正式 Esc。共享适配由 Codex 集中处理；先给出最小所需字段和接口，不复制管理器。正常运行默认关闭测试输入，处理未赋值引用和零最大生命，颜色与回声紫红/传播金黄一致。实际外观、中文字体与缩放要有实机证据；不能用纯色占位框声称已完成正式锈蚀 UI。以最新 origin/main 独立更新分支，附 SHA 与未验证项，由 Codex 审查后再接线。

该提示供用户决定下一轮时转发，本轮未向外部 AI 发送消息。
