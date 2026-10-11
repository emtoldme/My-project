# C-01 战斗整合轻量验证

2026-10-12，Windows，Unity 6000.4.3f1。基线main b6df1fd，C-01A来源PR #9/81b1d66；C-01B代码与生成场景对应本记录所在整合提交。

| 检查 | 实际结果 |
|---|---|
| dotnet规则 | 111通过、0失败：框架85+战斗19+接线7 |
| Unity EditMode | 111通过、0失败，见editmode.xml |
| Unity PlayMode | 11通过、0失败，见playmode.xml；原10条加实际战斗场景攻击/拾取/暂停/升级/重开 |
| 最终场景装配 | 编译/装配/引用检查成功；玩家模型比例、青色位置环已纳入最终PlayMode复查 |
| Windows Development Build | 成功，0错误、487告警；未声明零告警 |
| Windows包启动 | 隐藏窗口D3D11启动到GameLoop；检查窗口内没有匹配到异常/缺失引用/不支持Shader，随后自动关闭 |
| .meta | 手写原GUID保留，Unity实际导入通过；所有Assets中无重复GUID |

combat-preview.png是实际运行场景的引擎渲染：轻量用例人为生成8个近处敌人用于检查视图，不含IMGUI HUD，不是最终美术或自然一局表现。

原来的两个框架用例按真实闪避更新：命令伤害顺序测试不再同时发动无敌闪避；输入缓冲测试在已发动闪避后再次排队，验证暂停清空。新增用例明确验证闪避位移/无敌/冷却，未通过放宽断言掩盖玩法变化。

场景与构建自动产生的编辑器设置、服务开关和URP预过滤缓存不作为本轮设计变更提交；原用户工作目录未切分支或覆盖未提交文件。

本地包：工作区Builds/S1-C01/，整个目录需一起保留。原始日志：Reviews/S1-2026-10-12/C-01/中的unity-final-setup.log、unity-editmode.log、unity-final-playmode.log、unity-final-build.log、player-startup-final.log与dotnet/。

本轮包核心程序集SHA256：
- Waffle.Runtime.dll：C303338D24AE33992B581FA72AD7A6D01612395D00EC3BA65BBC5B0B14FA0454
- Waffle.Rules.dll：C138BB814EFF2237644622E110C36C82FBF9B327507419FE2689DFFD22B2309C

未验证：完整3分钟真人操作、游戏听感、平衡、完整设备矩阵、长时压力与内存。精英/包围/方向事件仅初步覆盖。S1等待用户体验验收，下一步按C-01B_HANDOFF.md集中反馈。