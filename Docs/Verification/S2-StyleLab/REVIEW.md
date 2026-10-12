# S2-01联合视听样板整合复核

2026-10-12 / Codex。整合基线main@8e5fe719db2e1d57468e5f451be3e7732e7ea797；本包源码d44a402d7467d81059afec548859e0f5ad43083c。技术交付完成，用户视听方向和S2阶段验收待进行；不进入S3或批量填充。

## 接收的四方成果

| 任务 | 分支及完整头SHA | 实物 |
|---|---|---|
| C-02 / 1号 | feature/c02-combat-guards / 5f89700e35f7cd4054d18b06258f4a0e71c56d23 | 6个规则边界用例和就绪报告，规则未改 |
| A-03 / 主美 | feature/a03-sculpture-kit / 635cbf3fa1bc2e89688b7453a55f0134b8bf6350 | 模型/材质/Prefab及Editor生成器 |
| S-03 / 音效 | feature/s03-sonic-kit / 0fc519fdc736f4115ddd8ad614115cdddf28ad6e | 两段24秒音乐与六个音效、作者报告 |
| P-03 / 2号 | feature/p03-style-lab / 9575b2840f69034916a1b45ed7981c542e9a8707 | 独立样板生成器、分节动作、FX、三选一UI |

逐条合并、保留作者提交。P-03交付的是生成工具，Codex在Unity生成并提交实际S2_StyleLab、资源和.meta；没有把“源码写好”当成运行通过。

## Codex补齐与修正

- A-03原倒角盒只有六张分离面，部分三角朝向不适合外表面；补封闭倒角、外向三角/平面法线/UV。生成器更新已有Mesh时原先仍把临时Mesh传给Prefab，现返回持久资产并保留GUID。玩家/敌人按1.8/1.4米对齐脚底、根比例1。重复生成后检查持久引用、尺寸/轴心、UV和面朝向通过。
- VfxDirector预热数组和目标网格，静态玩家快照合并全部五个部件及子网格；保留S1骨骼BakeMesh补偿，不叠第二套残影。禁用时清理在用FX。
- CombatViews新增默认保持S1行为的材质染色/尺寸归一化开关，S2保留主美材质与尺寸。S2MotionDriver识别实际Torso/Segment_*节点，使用当前帧逻辑dt、按敌人身份触发，避免每帧重扫整个预热层级。
- 场景识别实际6米地面模块、铺设60米范围并收敛缝隙亮度；接入新声音，加入暂停时A/B试听与精英波次预览。沿用唯一AudioDirector、VfxDirector和GameLoop，不增加第二套规则。样板加入Build Settings，播放器构建仅包含样板场景。
- 跨场景运行用例中发现闪避输入丢失：每局克隆输入资产，旧场景清理只关闭自己的Map，避免共享资产相互影响。旧S1与新S2运行回归通过。
- 三选一去掉工程说明文字，保留实际升级规则；点击选择后停止本次绘制迭代，避免用已切换的选项继续绘制。

## 验证事实

测试源码6f9ed0fcc663f1975eada65d50a86aacbe6e638c；随后仅将终局原始原因字符串改为玩家可读文案，最终源码重新构建并独立启动，未为纯文案修改重复整套测试。Unity6000.4.3f1，Windows：EditMode规则程序集117通过/0失败；D3D11 PlayMode14通过/0失败。原始逻辑结果在editmode.xml/playmode.xml（仅统一行尾）。PlayMode覆盖旧S1与音频生命周期，新增S2实际模型、自动攻击/命中、真实D+Space、全五部件残影顶点与大小、暂停冻结、A/B切换、经验/连续升级、真实120秒精英击杀环及重开输入/状态恢复。

S2短检查为可重复的隔离场景：固定dt、God、测试敌人、升级XP、跳时、关闭普通波次与高伤害用于迅速观察精英强调。生产场景保留原3分钟局长、正常属性/刷怪和120/150/170秒精英，不把短检查作为难度、战斗密度或性能基准。

模型/场景装配多次执行成功，日志S2_STYLELAB_SETUP_PASSED的placeholder=False、enemyPlaceholder=False、groundArt=True，引用/几何检查通过。combat.png、dodge.png、elite.png为真实运行时相机渲染的安排测试状态，**不含IMGUI界面**；scene-preview.png为编辑态静态预览，不用于代替实际运行光照。没有概念图冒充游戏截图。

Windows Development Build成功，0错误、487条告警；本轮未清理原有第三方/包告警。输出Builds/S2-StyleLab/Waffle.exe，整个目录保留，S1-DodgeFix未覆盖。最终d44a402包D3D11独立进程观察5.1秒，日志确认GameLoop开局、无异常，测试进程按计划停止；摘录player-startup.txt。此前6f9ed0f包另观察50.3秒，确认拾取XP和34.760秒进入首次升级，见player-upgrade-smoke.txt；该结果属于终局文案修改前的包，不冒充最终包长时验证。早期监控读取共享锁的布尔值未作结论，已改用共享读取并复核关闭后的日志。源码/测试版本与程序集SHA见build-info.json。

## 声音事实与限制

8个S03源WAV均48kHz/24bit，时长/样点峰值符合报告近似值，详见s03-audio-audit.json。4相、21tap窗化sinc仅作插值峰值估计，不是认证dBTP测量。零延迟Pearson约A/旧曲0.00663、B/旧曲0.00119、A/B0.01055，只表示该计算下的低线性相关，不能证明原创、器乐结构、风格或感知独立。

原音乐首尾阶跃A约0.146/B约0.191（满幅1），Dodge起始突变约0.757。保留Assets/Audio/S03原文件；Assets/Waffle/S2/Audio派生版本给音乐首尾12ms、音效起始1ms/末端8ms余弦淡变，首尾样点归零，B降低4.13dB以减小A/B响度偏差；不宣称真人试听无咔哒。加工脚本prepare_audio.py和输入/输出SHA见audio-processing.json，Unity按PCM导入。

作者仍未随交付附原始录音对应、生成源码或许可存档，来源、30次循环试听及绝对音色结论为作者自述，不能当成完成核验。已有死亡/升级展开/胜败/升级音乐暂复用S1。最终风格、连续触发耐受、密集混音与完整来源链待后续复核。

## 用户验收与下一步

按Docs/Integration/S2_STYLELAB_HANDOFF.md短试玩/试听，判断形体、哑光质感、敌我识别、爆发强度、UI可读性及音乐A/B。四位等待具体反馈，Codex负责技术整合与后续明确派工。完整设备矩阵、长时稳定、密集混音、最终画质/声音质量未在本轮宣称通过。
