# RF01 麦田＋声波反馈集成审查

2026-10-12 / Codex。基线 `origin/main@13be83d24504f83d5957a691e7caeccf440c999c`，分支 `integration/rf01-playtest`。保持用户转发、四位外部 AI 分摊工作、独立分支交付、Codex 收口、本地自动化与用户阶段验收。

## 已收交付

| 作者 / 分支 | 原始交付 SHA | 本轮用途 |
|---|---|---|
| 1号 feature/m01-echo-cues（PR28） | b5d7f4ad1a2079fb70663de3295da4dd514ebe1a | 只读待回声快照，真实目标与最早到期信息 |
| 2号 feature/m01-readable-feedback（PR29） | 3ab3838dd5f4c0fadb686681766563ca39769207 | 身体回声标记、实际伤害标签与传播连线 |
| 音效 feature/m01-audio-cues | abd9796f99c92408dac084631131e3813031d61f | 两段 TEST_ONLY 短音，替换 M01 机制通道素材 |
| 2号 feature/rf01-field-environment（PR32） | 9eda5412de45ce4753fb639b9cd3832d0985b32e | 独立环境 Editor 装配入口；原交付未在 Unity 运行 |
| 1号 review/rf01-rule-guards（PR31） | 89dbfe89c31dacb9e91f9cf0507fd784ac0f3d77 | 事件、目标失效、暂停与清理审查 |
| 音效 review/rf01-event-sound-map | 1462ba559d6de5dbebb41f1ead46db1ac24d1aca | 声画对应与监听建议 |
| 主美 review/rf01-art-direction | 4657d77f5da7cbcbe96707e3c9ff54f55fcb0def（含前一条9afc1ac武器审查） | 资源审查、武器审查与有限装配建议；非美术验收 |

通过合并保留原作者提交，没有让四位互相修改或合并分支。额外发现 PR33 HUD，存在独立写 Time.timeScale、假模式/生命值及空引用问题，暂不接入；返修要求见 Docs/Integration/RF01_HUD_PR33_REVIEW.md，未发外部消息。

## 实际整合与修复

- Unity 生成 RF01_EnvironmentPreview、P_RF01_Environment、Terrain 和 RF01_MechanismPlaytest；环境 24m×24m，固定种子 20261012，220 株（200 高麦＋20 低植被），中心半径 6m 无高麦、0 阻挡碰撞。高麦强制 LOD0，未重塑源网格。三种原有地表 TerrainLayer 以 4m 尺度混合。首包实机发现方形边缘，随后将同一地形的纯渲染地面延伸至48m，24m植被样板外缘平滑回干土；不增加植物或新玩法。
- 修复装配代码中不存在的 TerrainData.SetTerrainLayers 调用，改为 terrainLayers 属性。修复 Mathf.SmoothStep 参数用法：先 InverseLerp 再平滑，得到 0..1 的混合因子，避免负权重和路径过渡错误。新用例检查全部 256×256×3 权重与三个实际地表区域。
- 复用 M01 唯一 GameLoop → RunSimulation → CombatModule。HP、死亡、掉落、直接伤害 10、回声 0.5s、传播 0.1s、组合规则与闪避参数不变。四模式切换同 seed12345 重载。RF01 不替换默认 BuildSettings。
- M01PlaytestController 从真实队列按 DueAt/Serial/Generation 选择每目标最早预备，使用实时目标位置与局内时钟同步。死亡/代号失效撤标，终局禁用桥并清队列/表现，保留已结算统计。无命中不会播放假回声伤害标签。
- 普通直接命中使用小暖色脉冲/麦秸，回声采用身体紫红菱形收束和“回声 -5”，传播用金黄增长连线/落点和实际伤害标签。独立透明 Shader 正确使用顶点 alpha。动态系统字体优先 Microsoft YaHei；正常窗口的中文与面板仍须人工确认。
- RF01FeedbackBridge 只收真实 Shot/EnemyHit/EnemyKilled 驱动 SonicAttackVFX，不自己碰撞、扣血或播音。Shot 的只读事件增加 DirectionX/DirectionZ，在 T5 保存实际方向，避免目标同帧死亡后误朝另一个目标；新用例覆盖此情况。
- Sonic 开局预热 6 开火＋8 命中＋4 死亡实例，满池复用；粒子手动推进，干涉 Shader 使用 _VisualTime，暂停冻结。风动使用本局克隆材质的 _VisualTime，不修改源材质。重开/终局清理。未实现真实折射、骨骼扭曲或发布级性能认证。
- 音频仍唯一 AudioDirector，回声预备静默，实际派生命中才播新短音。保持 2D、总 trim -18dB、池满丢弃和最多两声部；EchoPropagation 音高 0.85。音效文档的更高空间混合/并发是建议，未照单全部启用。无新增正式 BGM。
- 武器按原始比例放在旁侧作材质展示，没有持握/发射挂点；角色 WIP 未导入，当前主角与敌人仍是旧 TEST_ONLY 临时模型。用户外部原件与 SourceArt 原始归档未修改；Assets 内 VFX/风动代码的时钟适配已明确记录，不能重新声称全部导入副本与输入逐字节相同。

## 自动检查

Unity6000.4.3f1 / URP17.4.0 / Windows。首次验证与首包源码 `c19a01cff1d7f04abaa31a0033c41156abf4bcdc`；之后仅补样板外缘地表过渡与显式后台测试键盘焦点，最终构建源码 `df6196de5c93ac39e6de48dc11fdf56c23d062b3`，详见 build-manifest.json。地表与测试输入补正后，针对性31项和两场景运行检查再次通过。

| 检查 | 实际结果 | 证据 |
|---|---|---|
| 本轮针对性 EditMode | 31/31，0 失败；22 规则＋7 实际桥＋地表/发射方向各1 | editmode.xml |
| 实际场景 PlayMode | 2/2，0 失败；每项遍历四模式，包含旧 M01 与新 RF01 | playmode.xml、playmode-observations.txt、m01-regression-observations.txt |
| 资源 metadata | 567 唯一 GUID，0 重复，Experiments/ReverbInTheRye 范围 0 缺 meta | metadata-check.json |
| Git LFS 本地完整性 | fsck OK | 本地执行记录 |

RF01 运行覆盖真实预备/派生命中、同种子重开、唯一管理器、18 实例固定池、实际直接事件与 VFX 次数一致、活动粒子/Shader风动暂停冻结、根缩放1、失败终局撤标。短时 PlayMode 尚无击杀，不把死亡 0=0 当成验证已触发；首包 Combined 独立运行实际出现3次击杀/3次死亡VFX，补足该事件路径的非零观察。

失败尝试如实保留在工作区 Reviews/RF01-2026-10-12：第一次环境编译失败来自 SetTerrainLayers，已修；首次 PlayMode 启动提前退出未产出结果，稍后正常启动；RF01 两次测试断言修正分别为首帧可能已开火，以及等待 LateUpdate 交付最后一个已模拟帧后再采暂停快照。首包 Combined 通过，随后 None 的隐藏窗口测试键盘因后台焦点被禁用，已限定在 --m01-smoke-output 模式设置 IgnoreFocus，普通运行仍使用原输入焦点行为。最终 XML 与最终包四模式记录才作为通过证据。没有运行全量160项 EditMode，也没有重新宣称历史全部测试已通过。

主美文档中的“100%无遮挡”和未经打开 Blender 确认的拓扑/绑定/UV结论仍待验证。无高麦中心留白不等于玩家走入外围后也无遮挡；零金属度/贴图均值不证明无塑料感。实际样板质感与声音辨识交用户验收。

Windows 构建、独立运行与画面检查结果见下文交付补录。试玩操作与阶段限制见 Docs/Integration/RF01_ACCEPTANCE.md。

## 最终 Windows 试玩交付

最终源码 df6196de5c93ac39e6de48dc11fdf56c23d062b3，Windows64 Development 构建成功，0错误、487警告，255,363,665构建字节。355个包内文件共256,115,788字节（不含manifest本身），SHA256清单随包与本目录保存。487项中485为已有AI Inference/Sentis计算Shader警告（包括ConvGeneric），另有RuntimePipelineConfig未配置和Cloud符号未上传；没有靠删除依赖消除警告。原默认场景、渲染设置的Unity自动生成变化已恢复。

最终独立包四模式均退出0：实际虚拟键盘移动/闪避/Esc暂停、根缩放1、预期机制事件、18实例池与真实VFX事件次数一致、URP相机渲染通过，日志检查无所列异常/缺脚本/Shader错误。Combined实际3次死亡/3次击杀，None1次，Echo2次，Propagation2次；精确次数以standalone-results.txt为准，不是平衡数据。F11进入2560×1600、恢复1280×720窗口、暂停中切换全部通过，见display.txt。

已查看最终实际相机截图：三种地表/小路/立体植被、原尺寸调频器展示和临时角色正常渲染，无粉色错误材质；传播线及中文“传播 -4”“回声 -5”可见。原小样板的方形硬边已通过同一地形外延与干土渐变修复。麦穗在此镜头仍偏细，地表有明显重复、色彩层级有限，不能据此认定精致麦海或主流画面达标。这些是真实待整改项，先交用户体验后给主美/2号下一轮有限任务。

相机抓图不包含IMGUI面板，也不证明正常窗口的字体/控件体验或人耳音色区分；这部分与场景质感由用户验收。包位于工作区Builds/RF01-RyeField，默认全屏启动器和窗口启动器已提供，普通启动不会注入测试输入。四位本轮交付冻结，用户反馈后继续原分工转发需求。
