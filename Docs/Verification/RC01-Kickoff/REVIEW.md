# RC01准备与TASKS_06发布检查

2026-10-12。运行代码/资产来自b6504ea72ba8f227731d4525bda640c14ed73abb（含80b772279c1c72d0ff0ea7f31297dce6a95e22e8任务/模式移除提交）；后续只补固定06B首批ID、证据和说明。

## 完成与限制

玩家四模式按钮已取消，普通独立运行固定当前Combined。内部模式仅Editor/显式自动化保留，不再要求用户四模式验收。已发布RunContent只读选择/装备/进化缺项视图与命令合同、四作者独占目录和任务。没有实际多武器/新被动/进化接线；RF01经验仍0，准备包不是成长体验交付。

四图/完整6攻击18被动6进化6拾取是草稿目标。06A四位交核心/数据/场景/风格后，Codex06B接首图4攻击10被动3进化4拾取及3种构筑，再交用户验收。不可把当前准备包或目录条数当目标完成。

## 必要修复

准备构建第一次暴露RF01_Terrain.asset读取失败：Unity返回Succeeded但errorCount=1，已明确拒绝该包。TerrainData是二进制，旧*.asset unity-yaml规则按文本换行转码，提交361660字节与Unity原始361664字节不同。已按原生成器重建并保持GUID8a238d0093b6d3d49ae51399d4a67f0c；新增Assets/**/Terrain/*.asset二进制例外。terrain-roundtrip.json证明工作树与Git取回的内容字节相同。BuildScene同时增加totalErrors>0就失败，避免类似假通过。

## 实际检查

- Unity6000.4.3f1：原RF01TerrainTests定向EditMode 1/1通过，检查真实TerrainData读取、三层权重归一及道路/土/秸秆分区；未重复大套测试。进化视图补充后由最终Player构建确认编译。
- 最终Windows Development包Succeeded，0错误、487已有依赖/配置警告。旧Sentis等Shader警告未在本轮消除，详见build-report.json。构建场景显式指定RF01，没有替换默认BuildSettings。
- 只跑当前Combined一次独立输入/渲染检查：移动、实际闪避、暂停冻结、根缩放、回声/传播、预备标记、实际Shot/Hit/Death反馈全部通过。详细计数见smoke.txt。
- F11进入2560×1600、恢复1280×720、暂停中全屏切换全部通过（display.txt）。相机world.png是实际URP场景，不含IMGUI；移除按钮以源码确认，不虚报自动化点击UI验收。
- 579唯一GUID、无重复；本轮RunContent/RC01范围无缺meta。源目录和用户原项目未改。
- 本次播放器日志无Exception/NullReference/Assertion failed/资源读取错误。不是完备测试或发布性能认证。

## 实机画面与后续

world.png仍可见细稀麦穗、重复地表和单一黄褐。没有称为正式质感达标。TASKS_06主美负责测试材质/布局草稿，2号独立装配第一图；正式美术/配乐/设定后置。

本地Builds/RC01-Preparation提供默认全屏和窗口启动器；无需用户重复验收旧比较模式。四位开始前拉含TASKS_06的最新main，录基线SHA，各自PR；PR33仍需处理暂停/假状态问题，未混入本轮。
