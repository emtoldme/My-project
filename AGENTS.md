# Waffle：开发协作约定

更新：2026-10-12。用户决定：Codex统筹GitHub、本地开发与自动化；用户转发外部AI任务，完成必要的体验和方向验收。初期轻量检查，中后期按计划补完测试。

## 当前实现与分工

正式主线只有一套 Waffle.GameLoop → RunSimulation → RunState 与 GameInput。状态、时钟、暂停、输入切换、升级、终局和重开不得另建平行实现。1号旧Survivor.GameManager分支保留参考，不直接整条合并。

main是唯一共享开发基线。当前任务详见 Docs/Integration/PROJECT_STATUS.md、TASK_BOARD.md、NEXT_TASKS.md；历史交接文档不能覆盖当前任务卡。编号按交付分支识别，不按Grok/Gemini名字推定。

| 当前任务及可写范围 | 唯一执行者 |
|---|---|
| C-01A：Assets/Waffle/Scripts/Rules/Combat/、Assets/Waffle/Tests/Combat/、Docs/Combat/ | 1号，纯规则战斗模块 |
| C-01B：共享主循环、输入/玩家、战斗视图、正式场景、Editor生成器、VfxDirector、程序集配置 | Codex，接入战斗、真实闪避与本地装配 |
| P-02：Assets/Waffle/Scripts/Views/Audio/、Docs/Integration/P-02_HANDOFF.md | 2号，本轮只做音频生命周期与交接小修 |
| A-02：Docs/Art/、Assets/Art/A02/ | 主美，建议与独立参考素材 |
| S-02：Docs/Audio/、Assets/Audio/S02/ | 音效总监，来源/事件交接与独立备选音频 |
| GitHub合并、任务板、整合修复与自动化证据 | Codex |

2号继续负责游戏素材、场景和效果的接入能力；每轮按任务卡分配实际可写范围，后续场景/VFX任务再单独派发，不能和Codex同时修改。跨范围需求写明调用处、输入输出、时序与验收条件，由Codex协调；不要复制管理器绕过边界。

主美负责视觉规范和素材，音效总监负责声音规范和素材；正式场景、共享材质、Mixer及表现入口不由素材作者并行修改。Codex可跨目录修复和接线，但必须记录变更并避开已派出的写入任务。

## 开工与交付

1. fetch最新origin/main，记录完整基线SHA；读取当前任务卡，确认可写路径和依赖。从main创建独立功能分支与独立工作目录，不在别人的目录切分支、不共用Unity Library/Temp。
2. 不直接push main，不强推别人分支，不自行合并PR。交付更新到最新基线；声明未合入依赖，冲突由Codex协调。
3. 新资源与脚本提交对应.meta，移动资源保留GUID。缺Unity环境准确说明，由Codex完成导入及必要自动化，不把生成.meta或处理冲突交给用户。
4. 一个PR一个明确目标，附基线/提交SHA、范围、真实验证结果、未验证项及依赖。不把代码存在写成游戏内验证通过。
5. 初期运行已有快速检查，确认编译、启动、引用和本轮关键路径。已发现的编译失败、崩溃、核心流程卡死及重复结算及时修复；其余按Docs/Verification/TEST_PLAN.md分阶段验证，不为每个小PR做全量测试与打包。
6. 常规技术修复与整合由Codex继续执行。用户验收准备好的试玩版本、方向对比和试听材料；每阶段仍须用户确认，技术合并不等于S1通过。

## 统一方向

3D类幸存者：移动为主、自动选敌攻击、手动闪避。视听遵守抽象几何、雕塑感、干净色块、克制精致PBR，冷静精密、战斗强烈爆发；音乐参考数学摇滚/后摇/电子。现有工业机甲只作临时参考，未获用户最终艺术验收。不批量扩充资产或自行推进S2。

本文件是协作约定，不是GitHub权限锁。两位AI若使用同一账号，CODEOWNERS不能区分身份；仍靠独立目录、明确任务范围和Codex合并审查避免冲突。用户当前授权优先于旧文档。