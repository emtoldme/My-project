# E-01 资源、URP 与声波通路

基线：`origin/main` `9c565ee0ca87c6858145e5f656996aa7711f916b`（Merge PR #17）。
分支：`experiment/e01-asset-wave`。
只写 `Assets/Experiments/VisualPipeline/` 和本目录。没有改 Art/S2、Waffle/S2、共享 Views、正式场景、Settings、Packages、ProjectSettings。P-04 雕塑返工没有继续。

本环境没有 Unity。没有编译，没有导入，没有 Play Mode，没有同机位截图，没有 Profiler。下面不是画面通过，也不是 ShaderGraph 通过。

## 跑什么

菜单 `Experiments/Setup E01 Visual Pipeline`。

```text
-executeMethod Waffle.Experiments.VisualPipeline.EditorTools.E01VisualPipelineSetup.Setup
```

幂等。只清空并重建 `Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity`。如果当前打开的不是这个场景，工具直接停，不改别的场景，也不弹“是否覆盖”。Unity 自己仍可能询问要不要保存当前脏场景；取消后工具会停。

成功日志：

```text
E01_VISUAL_PIPELINE_SETUP_PASSED shader=hlsl|shadergraph graph=... model=TEST_ONLY
```

只有出现 `E01_SHADERGRAPH_OK` 时，材质才改接到生成的 ShaderGraph。否则工具会删掉没接上的图，声波用 `Shaders/E01_WaveRing.shader`。不要把 HLSL 回退记成 ShaderGraph 已通过。失败原因在警告里。

场景不在 Build Settings 里（那份配置不在本轮可写范围）。编辑器里重开走现有 `GameLoop.Restart` 的 Play Mode 加载。独立播放器里重开会失败，除非 Codex 把该场景加进 Build Settings。

## 场景里有什么

- `GameLoop`、`CombatViews`、现有 `RunHud`、隐藏的 `DebugConsole`（F1）。
- 没有 `VfxDirector`，没有 `AudioDirector`。射击和命中只由 `E01WavePlayer` 做一种反馈：一块 Alpha Clip 网格环，加 3 个小立方碎粒。碎粒是网格，不是 ParticleSystem，避免粒子 Shader 套到 MeshRenderer 上变粉。
- 没有第二套炮口、命中碎片或残影。若把 `VfxDirector` 再挂上，Shot/EnemyHit 会叠两套。需要同时保留旧特效时，请 Codex 给 `VfxDirector` 加“跳过 Shot/EnemyHit”的开关，不要在本目录复制一份。
- 没有声音。安静不代表音频实验通过。
- 镜头沿用正交 Size 9、Pitch 55、Yaw 45。没有全局 Volume，没有切换 URP 资产，没有复制 Renderer。相机用工程现有默认渲染器。
- 地面是 60×60 的测试方片，根缩放 1。不是麦田，不是正式乡村。
- 玩法数值没改。波纹不参与伤害。

左下角按钮可在同一镜头关掉/打开声波，给对照截图。本环境没有截到。

## 模型（TEST_ONLY）

主美本轮清单 `Docs/Art/Reverb/` 不在这个基线上，用户也没有指定新的乡村模型。所以用现有第三方机甲，只读，不改 FBX 和原 Prefab。

| 项 | 事实 |
|---|---|
| 玩家 | `Assets/Art_ThirdParty/Quaternius_Mech/George.fbx`，Prefab `Assets/Waffle/Prefabs/Visual/Player_George_Visual.prefab` |
| 导入器（`.meta`，不是网格实测） | `globalScale` 1，`useFileScale` 1，`bakeAxisConversion` 0。FBX 本体是 Git LFS，这里没有网格 |
| 场景摆放 | 玩家根 `(1,1,1)`。高度差和脚底偏移写在 `Visual` 上，目标约 1.8 m，做法与 S1 装配相同 |
| 敌人 | 原样引用 `Enemy_Vanguard_Mike_Visual`。`CombatViews` 仍会把实例缩到约 1.5 m，并每帧写橙色属性块。这是旧视图行为，不是本实验改的难度 |
| 轴向 | 场景 +Y 向上。`bakeAxisConversion` 为 0 时由 Unity 在导入时做轴转换。玩法前方是 GameLoop 用的 +Z |

菜单会把当次实测包围盒、缩放和节点路径写到 `Assets/Experiments/VisualPipeline/E01_IMPORT_FACTS.txt`。那个文件要等 Unity 跑完才有数字。没有这份实测之前，不能说缩放已经正确。

## 声波

`E01WavePlayer` 只订 `PresentationDispatched`。

- `Shot`、`EnemyHit` 各放一环。位置用事件的 XZ，高度 0.05。颜色是测试青和测试品红，不是已定的叙事色板。
- 半径约 0.2 m 收到 2.2 m，0.32 秒。只在 `RunPhase.Playing` 加 `RunSimulation.LastDt`。暂停、升级、终局停住。`Restart` 清空。
- 环是 6×6 m 的四边形，Shader 用 Alpha Clip 留一条细环，`ZWrite Off`，不写空心。整块四边形仍会跑片元，这是过绘成本；中心被 clip 掉，不挡地面。边缘没有半透明抗锯齿。
- 对象在 `Awake` 建好，射击时不 `Instantiate`。
- 左下角只显示环数、碎粒数和累计次数。没有 CPU/GPU/GC 毫秒。Profiler 没跑。

## ShaderGraph

`E01ShaderGraphBuilder` 在菜单里反射创建 URP Unlit、打开 Alpha Clip，并试图把与 HLSL 相同的环公式放进 Custom Function。类型是内部 API，这里没有编辑器，所以没跑成。

取舍记在这里，供对照，不是验收：

- Alpha Clip + AlphaTest 队列：边缘硬，空心不混合，四边形中心不过绘到画布上，但片元仍计算。
- 不用透明混合，避免和地面叠一层。
- 不用 Bloom / Volume。
- 不改 `Assets/Settings` 里的 PC Renderer。

## 给 Codex

1. 导入后跑上面的菜单，看 `E01_VISUAL_PIPELINE_SETUP_PASSED` 和有没有 `E01_SHADERGRAPH_OK`。
2. 读生成的 `E01_IMPORT_FACTS.txt`。高度差、丢材质、节点以那份为准。
3. 同机位截两张：声波开、声波关。按钮在左下角。
4. 打一枪、打中一次，确认环跟着事件走，暂停时停住，重开后没有残留。伤害数字应与没挂这个组件时一样。
5. 若 ShaderGraph 导入失败，保留 HLSL 环，别假装图已经通了。反射失败的原因在警告里。
6. 不要把本场景和 `VfxDirector` 叠在一起，除非先加跳过 Shot/EnemyHit 的开关。
7. 正式乡村模型等用户指定或主美清单。这份机甲只证明通路。
