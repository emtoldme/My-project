# RC02-08A 整合与必要检查

2026-10-13。基线 main@6447af8a96496b2c606de4e586b4ea7eaae8aaf9；独立 integration/rc02-components。实际打包源码 f660bb129dd51382a031527533d4959e99fd4819，后续只改文档/证据。未操作用户原工程或 Unity 窗口。

| 岗位 | PR | 作者提交 |
|---|---|---|
| 1号规则 | 45 | 23c8be9540cebdb782798b4a720b62229c5bfb56 |
| 2号内容目录 | 46 | 5d8b6b8d8c6f015bc1a8cbcd4499f35a6775dafb |
| 主美模板 | 47 | 1a497121baf31756ad0c11b702e6e3091a3bcaf3 |
| 音效清单 | 48 | abe133f35df84de536b912a9cd54efa8897f59e1 |

Codex 关键修订：把纯组件接进 CombatModule 的真实结算，避免 W05/W06 落进旧弹体分支；仅 W05 首跳可起派生链，禁止残留/放电重复起链。每局独立组件、实际闪避通知、合法位移与暂停时钟。表现从临时创建/销毁改为预算池复用，按确切事件接管而非旧组件存在即屏蔽，W06 视图同步真实点。修复清单 seed 冲突/重复错误写坏资产/组合 ID、事件名称与材质参数不匹配；Unity 生成真实模板，保留合法 GUID。音效和音乐清单注入现有 AudioDirector，未新增播放器。首次可见窗口截图发现 W06 连线默认白色遮挡，已改成半透明麦草色并淡出；升级范围变化同步既有残留圈。

检查按用户要求收敛：

- dotnet 规则工程编译通过，0 错误/0 警告；没有运行或扩充全量 NUnit。
- 临时规则诊断 8 项通过，见 rules-probe.txt：链击唯一目标/有限派生、移动布点/真实伤害、成功与失败闪避、暂停、重开清理。
- Unity 装配重复刷新一致，33 条引用有效，刷新错误 0；6 攻击 / 14 被动进入白名单。catalog-refresh.txt 为装配报告。
- Windows Development 构建 Succeeded，0 错误、487 警告。与 PR44 相比部分完整警告文本因 com.unity.ai.inference/Sentis shader 变体不同而变化；不将所有警告称为逐字相同。完整依赖警告保留本地 build-report.json，摘要及文本差异见 build-summary.json；未在本轮改包或清除依赖。
- 资产 725 个合法唯一 GUID，无非法/重复；metadata.json。构建产生的已知设置噪音已恢复；预存 SceneTemplateSettings.json 保留。
- 独立包通过真实三选一命令取得六项新内容，实际观察 W05/W06/P17 EnemyHit 与池化表现生成；见 standalone-capture.txt。该脚本仅显式 --rc02-capture 时执行，普通游玩不启动。使用调试 XP、无敌和固定敌人，因此不是自然成长、难度、音频品质或性能证据。

启动入口：本地 Builds/RC02-Components/打开游戏.cmd，窗口模式同目录，F11 全屏。文件哈希和实际代码 SHA 见 build-manifest.json。当前声画仍为 TEST_ONLY，08B 和其他三张地图没有计入实装。用户按 RC02_ACCEPTANCE.md 做简短体验验收，不要求重复四模式或穷举构筑。
