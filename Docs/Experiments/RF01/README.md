# RF01 麦田环境样板

2026-10-12。基线 `origin/main` `13be83d24504f83d5957a691e7caeccf440c999c`（Merge PR #30）。分支 `feature/rf01-field-environment`。

只写 `Assets/Experiments/RyeField/` 和本目录。没有改 M01、`Assets/ReverbInTheRye`、SourceArt、Packages、Build Settings 或共享管理器。本环境没有 Unity。场景和 Prefab 都还没生成，没有截图，不能把这份说明当成画面已经达标。

## 怎么生成

菜单 `Experiments/Create RF01 Field Preview`。

```text
-executeMethod Reverb.Experiments.RyeField.EditorTools.RyeFieldSetup.CreatePreview
```

幂等。只重建：

- `Assets/Experiments/RyeField/Prefabs/P_RF01_Environment.prefab`
- `Assets/Experiments/RyeField/Scenes/RF01_EnvironmentPreview.unity`
- `Assets/Experiments/RyeField/Terrain/RF01_Terrain.asset`
- `Assets/Experiments/RyeField/Materials/M_RF01_Terrain.mat`
- `Assets/Experiments/RyeField/RF01_LAYOUT.txt`

不调用别的装配，也不把场景写进 Build Settings。当前如果开着 M01 或用户素材目录里的场景，工具会停。成功日志：

```text
RF01_ENVIRONMENT_SETUP_PASSED size=24 seed=20261012 instances=... wheatInClearing=0 colliders=0 tile=4
```

`RF01_LAYOUT.txt` 里的株数要等这次 Unity 跑完才有。跑完之前不要引用一份假数字。

## 环境 Prefab 里有什么

根节点 `RF01_Environment`。只有地表和植被。没有相机、灯、听众、敌人、GameLoop、音频导演。Codex 可以把这个 Prefab 挂进 M01，不必连带预览场景里的镜头。

预览场景另放：

- 正交相机，Size 9，Pitch 55，Yaw 45，看向原点。这是现有玩法镜头，用来看 24 m 样板的中心，不是保证整块 24 m 都进画面。
- 一盏方向光。没有第二盏灯，没有雾，没有 Bloom，没有用它们去遮接缝。
- `RF01_WeaponDisplay`：调频器 Prefab，比例 1，放在场地外角 `(16, 0, -16)`。不缩小，不猜手骨。

## 地表

直接引用已导入的三张 TerrainLayer，不改原文件。平铺仍是源数据里的 **4 m × 4 m**。24 m 边长大约重复 6 次。入库审查已经指出周期纹样偏明显；这次没有在看不见画面的情况下改这个尺度。

混合不是三块矩形：

- 中心约 6 m 以干土为主。
- 一条踏过的路径从西侧穿到东侧，核心约 0.85 m，外缘约 2.4 m 混开。路径在中心北侧轻轻弯过，不把留白铺满。
- 外围过渡到厚麦秸，过渡带大约从 5.4 m 到 8.2 m。
- 着色用项目里现成的 `Universal Render Pipeline/Terrain/Lit`。找不到就停止，不去改 URP 资产。

## 植被

固定种子 `20261012`。高麦只种在半径 6 m 以外，A/B 两种混用，间距至少 0.85 m。疏密来自种子噪声，朝向大多在 18° 附近 ±35°，少数转 180°。不缩放模型。

中心只留很少的麦茬和矮草，间隔约 1.15 m，上限 24 株。总实例不超过 300。植被上的碰撞体会从样板实例里去掉，不回写用户 Prefab。

麦穗 LOD1 比 LOD0 矮大约 12%～14%，轮廓也不一样。样板把实例的 LOD 收到 LOD0，并关掉名为 `LOD1` 的子物体。没有改用户网格。后续若要修，应让 LOD1 的高度和外轮廓对齐 LOD0，而不是在这个样板里压扁模型。

## 还没验证

没有导入，没有 Play，没有俯视截图。地表细节、麦浪层次、留白和战斗辨识都等 Codex 跑完菜单再看。角色 WIP 没有导入。
