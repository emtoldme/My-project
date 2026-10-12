# E-01 整合复核

2026-10-12。基线 `origin/main` `2d113dd656933c48b5a510c86ea5993d169e7c2a`（Merge PR #20）。分支 `review/e01-integration-followup`。只读了现有实验和 `Docs/Verification/Reverb-Experiments-01/`，没有改 Codex 的 Editor/Verification 代码，没有改共享渲染配置、Core/Views 或主美目录，没有升级包。

本环境没有 Unity Profiler，也没有 Frame Debugger。下面没有新的 CPU、GPU、GC 或 DrawCall 数字。不能用帧时间或像素差换算 GPU 时间。

## 材质

`M_E01_WaveRing.mat` 指向已提交的 `E01_WaveRing.shadergraph`（guid `9b1ae498c1e9bd14c8e92b2ad6dba72e`），不是 HLSL 备选。

| 项 | 文件里的值 |
|---|---|
| 表面 | `m_SurfaceType` 0，不透明 |
| Alpha Clip | 开。阈值 `_Cutoff` 0.5 |
| 深度 | `m_ZWriteControl` 2。按 URP 17 的枚举，这是强制不写深度 |
| 朝向 | `m_RenderFace` 0。按同一套枚举，这是双面 |
| 环参数 | `_Radius` 0.4，`_Width` 0.09，`_QuadMeters` 6。播放时由属性块改半径 |
| 颜色 | `_BaseColor` (0.35, 0.9, 1)。命中环的品红在代码里写进属性块，不在材质默认色里 |

`M_E01_Floor_TestOnly.mat` 是不透明、写深度、底色 (0.42, 0.44, 0.40)。`M_E01_Spark.mat` 的 shader guid `0406db5a14f94604a8c57ccfbc9f3b46` 不在本仓库里，属于包内着色器；材质本身是不透明、写深度、Alpha Clip 关、青色。地面和火花的 shader 名字以 Unity 导入为准，这里不能从 guid 再猜一遍。

生成器仍反射调用 ShaderGraph / URP 17.4.0 的内部类型。已生成的这张图可以直接复用。不要为了它升级 Package。图还在时不必重跑生成器。

## 暂停、回池、重开

只对照当前 `E01WavePlayer.cs`，没有重新进 Play Mode。

- 只有 `RunPhase.Playing` 才把 `LastDt` 加进环的年龄，并 `ParticleSystem.Simulate`。暂停、升级、终局时 dt 为 0，环和粒子停住。
- `Restart` 在 `waveEnabled` 判断之前就 `Clear()`。关掉声波再重开也会清。
- `OnDisable` 会清池。关掉组件时临时网格只在自己 `Awake` 造出来的情况下销毁。
- 环最多 6 个，在 `Awake` 建好。播完就关掉，不在每次射击里 `Instantiate`。
- 粒子是一个 `ParticleSystem`，发射关着，每次事件 `Emit` 3 粒，用游戏 dt 手动推进。这不是方块冒充。

Codex 的 `e01-runtime.txt` 写：真实 Shot/Hit 各 1，暂停和禁用清理通过。那是他们的运行记录，不是这次又跑了一遍。

## 开关截图

看的是主线上这两张已提交的图，不是这次重新截的：

- `Docs/Verification/Reverb-Experiments-01/e01-wave-on.png`
- `Docs/Verification/Reverb-Experiments-01/e01-wave-off.png`

两边都是 1280×720、无 IMGUI。机位和两台机甲的位置一致。开的时候，脚下有一圈青色大环，旁边那台有一圈品红环。关的时候这两圈不在。脚下那圈更小的青线两边都有，对得上 `CombatViews` 的玩家脚圈，不是这次声波开关。

本机把这两张 RGB 逐像素比对，有 1464 个像素不同。Codex 日志写 1447。差 17，比较方法可能不同。这个差只说明画面变了，不是 DrawCall，也不是 GPU 毫秒。

俯视尺寸下看不清粒子碎粒。这两张图不能证明粒子已经被看见，只能证明网格环开关有效。画面仍是旧机甲和素色地面，不是正式乡村。

## Profiler / Frame Debugger

没有测。

| 项 | 结果 |
|---|---|
| 设备 | 不可用。本环境没有那台 Windows / D3D11 机器 |
| 分辨率 | 只知道已提交截图是 1280×720，不是新的一帧分析 |
| CPU | 不可用 |
| GPU | 不可用。没有 Frame Debugger，也没有 GPU 计时 |
| GC | 不可用 |
| DrawCall | 不可用 |

不把像素差、PlayMode 耗时或任何 FPS 写成 GPU 时间。没有做 200 个敌人，也没有重新打包。

## 正式模型还要等用户指定

现在的通路只用了 George，`fileScale` 0.01，`Visual` 缩放到高度 1.80 m、脚底 Y=0、玩法根 (1,1,1)，一个蒙皮渲染器。这是 TEST_ONLY。

以后用户指定正式模型时，最少要这些，多的先不要：

- 一个 FBX 或等价 Prefab。场景单位 1 米，玩法根比例 1。厘米源文件只允许留在导入或 `Visual` 适配里，并写出缩放结果。
- +Y 向上，脚底轴心，角色朝 +Z。玩家挂在 `Player/Visual` 下。高度约 1.75–1.85 m。
- URP Lit：BaseColor；Metallic 图线性，R 金属、A 平滑；Occlusion 槽读 G。外部 ORM 不能原样丢进 Metallic 槽。
- 一个可见网格节点就够当前声波实验。不要为了这一步交动画、麦田或整关。

本轮不制作、不采购、不批准这套正式模型。
