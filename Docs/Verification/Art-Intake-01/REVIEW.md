# 首批美术素材接收与Unity导入检查

2026-10-12。基线origin/main@d143e7adefeb321c4b1fa6668f1cb493d4c8947e，集成分支integration/art-intake-01。用户新提交素材，路线由“全部美术后置”改为“现有素材入库、麦田环境样板、M01反馈接线”，保留四位外部AI分担。

## 收到什么

| 内容 | 实际来源 | 已完成 | 当前边界 |
|---|---|---|---|
| Ground v0.2 | G:/图书馆/ReverbInTheRye_Ground_v02.zip | 9张1024贴图，3个URP材质、3个TerrainLayer | 包内prototyping_only=true；实际平铺重复需改善 |
| Foliage v0.1 | 同目录ReverbInTheRye_Foliage_v01.zip | 6个OBJ，Atlas，4个Prefab，风动Shader | LOD1高度/宽度与LOD0不同，风动/阴影/暂停未动态验收 |
| SonicVFX v0.1 | 同目录ReverbInTheRye_SonicVFX_v0.1.zip | C#、2个Shader、材质及预览组件导入 | 原包明示候选源码/非美术定稿；尚未接真实战斗 |
| Old Sonic Modulator | 同目录已解压Old_Sonic_Modulator_AssetKit | FBX、33张贴图，URP材质与武器Prefab | ZIP只有材质/指南，模型来自用户已解压目录；挂点/持握待定 |
| 主角Blockout v01、稻草人ArmRig v04 | 工作区根目录的.blend及预览 | 作为源稿入库，未放进Unity Assets | 块面/骨架WIP，不代表正式角色、完整动画或品质验收 |

Assets/ReverbInTheRye下为导入副本；SourceArt/Intake01保留源说明、离线预览、GLB/Blender源稿和输入SHA256清单，共98个输入文件、45,884,094字节。没有改动用户外部原件，没有把GLB/Blend重复导入Unity。模型生成脚本只存档，不自动执行。

## 实际自动检查

- Unity **6000.4.3f1**，现有URP **17.4.0**，RTX4070 Laptop GPU；真实Unity编辑器批处理运行，未使用Python画图充当实机截图。
- 两轮资源检查执行完成，最后日志标记`ART_INTAKE_PASSED models=7 foliagePrefabs=4 groundMaterials=3 weaponPrefab=1`。新增C#可编译，3个自定义Shader在当前编辑器渲染路径无编译错误；模型均可读取，没有未映射武器材质槽。已有工程仍有过时API/服务日志，不称整个工程零警告。
- 地表3张打包图R通道全0；Alpha平均光滑度DenseStraw **0.1080**、DrySoil **0.1500**、TrampledPath **0.1580**。BaseColor用sRGB，Normal设NormalMap，金属/光滑度为线性；使用URP Metallic Alpha。数值正确不等于艺术效果已过关。
- OBJ未携带法线，Unity自动重算法线并产生导入提示。高度实测主麦丛A **1.988m**、B **1.907m**；LOD1分别 **1.739m/1.648m**，有约12%～14%的高度跳变，宽度也不一致。检查场景强制LOD0，不因此宣称LOD已修复。
- 调频器FBX **19,806 tris**，包围尺寸约 **2.315×0.954×0.954m**；13个URP材质映射有来源名称，Prefab保留模型层次与原始比例。玻璃用透明近似，不声称复现Blender Transmission/折射。
- 98份输入SHA256全部匹配；**538个唯一GUID，0重复，新导入资产0缺meta**，见source-and-metadata.json。

`Assets/Experiments/ArtIntake/Scenes/A01_AssetInspection.unity`是检查用拼板，含三种地表、四种植被、武器和两块静态Shader色板。它不是设计好的关卡，也不是用户品质验收画面。工具`Reverb.ArtIntake.EditorTools.ArtIntakeBatch.RunBatch`可重跑；仅写独立资源目录/检查场景，不改M01/S1/S2或默认BuildSettings。默认输出在本证据目录，可用--art-intake-output指定其他目录。

## 真实截图与可见问题

[全景](asset-inspection.png)、[植被近景](foliage-closeup.png)、[武器近景](weapon-closeup.png)为Unity URP相机渲染。首轮新导入后立即捕获出现纹理尚未呈现的深色植被；第二轮捕获前预渲染3次，并以近景确认Atlas和材质已正常显示。

地表已经有麦秸/土块细节，然而周期纹样和三材质直线边界很明显；检查拼板不能直接搬成关卡。植被是几根真实几何麦穗，单丛偏稀，要靠受控疏密、方向和中低层次形成麦田。武器可辨喇叭、线圈、仪表与电子管，但侧面有贴图拉伸，玻璃/灯丝的俯视表现还需调校。主角源预览仍明显是块面白模，尚不满足用户提出的细节和质感要求。

## 原审查需要修正的判断

A-R02/03/04作为主美作者意见入库，不能直接当实机证据：

1. 金属度0不保证材质绝无塑料感，贴图均值不能证明警示/掉落可见度100%。
2. 当前SonicAttackVFX.Acquire按需CreateEffect，不是开局全量预热；最大并发仍可能动态分配。
3. Time.deltaTime、ParticleSystem自身时间及Shader的_Time会绕过现有局内暂停；只冻结C#还不足以冻结整套表现。必须由Codex适配后再接M01。
4. ResonancePulse只是发光干涉环，没有采样场景颜色实现真正空气折射，也没实现敌人蒙皮错位。Shader有instancing声明不代表SRP Batcher/批处理/零GC已测。
5. 默认PlayHit紫红环与原位回声语义可能混淆，不能直接照原审查建议在直击和回声上重复播放同一大圈。

## 下一步与验收

可转发需求在Docs/Integration/REVERB_TASKS_05.md。2号负责独立RF01环境样板，主美按实际图审查，1号审查规则事件/取消边界，音效总监整理新武器事件映射。Codex收口已交M01-R1代码与短音，以及新VFX时钟/有限池/暂停/真实事件，再挂载环境构建独立试玩。

本轮没有改玩法、没有运行完整回归/性能测试、没有打新的可玩Windows包；M01-R1仍待接线。角色动画/武器持握、动态风浪/阴影、实际粒子、构建Shader保留均尚未通过本轮检查。用户随后分两步验收：场景质感与统一性；组合模式声波因果是否清晰。已肯定的组合乐趣与闪避保留。
