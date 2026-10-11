# 闪避修复包交付

2026-10-12，Unity6000.4.3f1，Windows x64 Development。

代码/场景/材质与测试来源提交：1ef9ddadead284148a70322fbce33bf2fa3b76b2。后续交付记录提交只更新文档，不改变编译内容。基线main：0f313c3dec5e013dc147415b020b5eded15bf810。

本地完整包：工作区 Builds/S1-DodgeFix/，入口 Waffle.exe。原 S1-C01 包保留。

- Unity构建成功，0错误、487警告；沿用当前项目/包警告，不声称零警告。
- D3D11自动启动到GameLoop开局，随后短运行，日志未匹配Exception、NullReference、MissingReference、Unable to load或不支持Shader；仅停止本次自动检查启动的进程。
- Unity PlayMode轻量回归12通过、0失败；另做真实D3D11专项用例与三张相机渲染。规则层未改变，没有重复整套规则测试。
- 包内 Waffle.Runtime.dll SHA256：8600667CEC618BF672F40118FABBB18B309C35FF07200882CD67FC467F10388A。
- 包内 Waffle.Rules.dll SHA256：C138BB814EFF2237644622E110C36C82FBF9B327507419FE2689DFFD22B2309C，与上一包相同。

原始导入、测试、构建与启动日志保存在工作区 Reviews/S1-2026-10-12/DodgeFix；测试XML与实际画面已纳入本目录。包内build-info.json记录来源与哈希。

请用户关闭旧版，运行S1-DodgeFix，短试WASD+Space以及松开方向后单按Space，判断残影大小、人物冲出是否清楚、镜头是否舒服。自动化没有替代这项手感验收，也没有提前通过S1。
