# S1 闪避表现修复

日期：2026-10-12。来源：用户报告“骨架变大、蓝色透明特效，但人物没动”。基线：main 0f313c3dec5e013dc147415b020b5eded15bf810。

## 原因与修正

实际 Space 输入经 GameInput / GameLoop 已触发真实位移。旧残影对 FBX 骨骼的缩放处理重复：旧网格默认 BakeMesh 的快照又乘 renderer.lossyScale。实际场景记录：人物可见边界 (1.56, 1.80, 1.36)，旧残影 (29.40, 34.90, 28.69)，源节点缩放约23.16。before.xml 保留原残影代码触发的失败；该测试运行时镜头缓冲和本体固定比例已加入，不能作为全部改动之前的画面对照。

VfxDirector 显式使用 BakeMesh(mesh, true) 补偿骨骼节点缩放，随后只在快照 Transform 上应用一次。只选玩家 Visual 下的模型，静态网格备选跳过禁用的胶囊。第二个残影延迟采样当时的姿态和世界位置，避免两个影子叠在起点；计时与游戏实际 dt 一致，暂停冻结，恢复后继续，结束回池，重开取消排队采样。保留原对象池与唯一表现事件通道。

CombatViews 取消刚性机甲闪避时的挤压缩放。CameraFollow 增加0.08秒位置跟随缓冲，角度/正交尺寸与移动基向量保持既有配置；暂停时镜头冻结。正式灰盒增加淡色5米地面参照线，便于观察位移。这些是原型可读性修复，不代表最终美术方向已验收。

## 已执行

- Unity 6000.4.3f1 / D3D11 PlayMode，真实场景与 Input System 官方 InputTestFixture，模拟真实 D+Space 绑定；正常 GameLoop Update 推进，无测试替代位移。
- 专项用例通过：请求到达、真实玩家位移、刚性模型比例不变、镜头允许短位移、残影尺寸、两个采样点分离、暂停冻结、恢复后总位移3米、残影回池。
- 修复后实际记录：人物边界 (1.56, 1.80, 1.36)，残影 (1.27, 1.51, 1.24)。可见模型边界含保守包围盒，不要求快照几何与包围盒逐轴相等。
- before-dodge.png / during-dodge.png / after-dodge.png 为专项用例中真实相机渲染，不含IMGUI；during-dodge.png 已查看，残影比例与位置正常。

完整轻量PlayMode回归12通过、0失败，见playmode.xml；另修正旧独立组件冒烟用例的输入依赖，让它显式提供Actions，不依赖项目级Actions被其他测试保留。Windows构建结果见交付记录BUILD.md及包内build-info.json。纯Rules未修改，未为本次重复全量规则测试或中后期设备/压力矩阵。

## 试玩

使用工作区 Builds/S1-DodgeFix/Waffle.exe，旧 S1-C01 包保留。WASD + Space，随后松开方向再单按 Space；只需短试确认“人物确实冲出、残影不再巨大、镜头跟随舒服”。本次技术合并不等于S1阶段验收通过。

API语义核对：[Unity SkinnedMeshRenderer.BakeMesh](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html)。尺寸与位移结论以上述本地实际运行结果为准。
