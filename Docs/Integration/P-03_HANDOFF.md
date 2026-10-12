# P-03 交接：S2 视听样板装配

> 2026-10-12：作者提交时未运行Unity。Codex随后整合A-03/S-03并完成共享适配：全模型残影、保留敌人材质、分节命名/时序/身份接线、地面铺设、派生音频绑定、样板Build Settings及A/B试听。当前事实与运行证据见Docs/Verification/S2-StyleLab，以下原交接保留历史。

基线：`origin/main` `8e5fe719db2e1d57468e5f451be3e7732e7ea797`（Merge PR #12）。
分支：`feature/p03-style-lab`。样板代码提交：`0f370922bb108e462877e531a17acb38dee22841`。本交接的提交号补记可能使分支顶端比它新一个提交，以 `git rev-parse origin/feature/p03-style-lab` 为准。
可写范围只做了：`Assets/Waffle/S2/`、`Docs/Integration/P-03_HANDOFF.md`。
没有改 S1_Graybox、Core/Input/Views、AudioDirector、VfxDirector、原 asmdef、主美/声音原始素材，也没有合并 A-03 / S-03。
没有把本分支合进 main。

本环境没有 Unity Editor。没有编译，没有执行菜单，没有 Play Mode，没有截图，没有试听。`S2_STYLELAB_SETUP_PASSED` 没有出现过。不要把本文件当成编译通过或样板验收。

## 给用户的回复纪要

用户问「你在做什么」。当时 P-03 还停在本地未提交：脚本已写，场景还没生成。随后补了占位高度、地面网格写入顺序、回池复位、镜头底色和打开场景时的误伤保护，然后上传本分支。不是在改 P-02 音频。

分支已推送。PR：https://github.com/emtoldme/My-project/pull/14 （base `main`，head `feature/p03-style-lab`，顶端 `ff5d75a686782fa2c78a67c7ef0f1bb9abd0e77f`，加上本段后顶端会再新一个提交）。没有自行合并。本环境仍然没有编译、没有进游戏。

## 仓库里实际有什么

已提交的是生成器和运行脚本，不是 Unity 导入后的场景：

- `Assets/Waffle/S2/Waffle.S2.asmdef`（引用 `Waffle.Rules`、`Waffle.Runtime`）
- `Assets/Waffle/S2/Editor/Waffle.S2.Editor.asmdef`（只进 Editor；另引 `Waffle.S2`、`Unity.InputSystem`）
- `Assets/Waffle/S2/Scripts/S2FxProfile.cs`
- `Assets/Waffle/S2/Scripts/S2MotionDriver.cs`
- `Assets/Waffle/S2/Scripts/S2FxPlayer.cs`
- `Assets/Waffle/S2/Scripts/S2ChoiceHud.cs`
- `Assets/Waffle/S2/Editor/S2StyleLabSetup.cs`
- 上述文件的 `.meta`（脚本 guid 稳定；生成出来的材质/预制体/场景 meta 不在这次提交里）

菜单跑完之后才会出现、这次没有提交：

- `Assets/Waffle/Scenes/S2_StyleLab.unity`
- `Assets/Waffle/S2/S2FxProfile.asset`
- `Assets/Waffle/S2/Materials/M_S2_Ceramic.mat`、`M_S2_Graphite.mat`、`M_S2_Metal.mat`、`M_S2_Accent.mat`、`M_S2_Ground.mat`、`M_S2_Line.mat`、`M_S2_Shard.mat`、`M_S2_Burst.mat`
- `Assets/Waffle/S2/Meshes/ArenaFloor.asset`（60×60，顶点 ±30，物体缩放 1）
- `Assets/Waffle/S2/Prefabs/RS_Player.prefab`、`RS_Enemy.prefab`、`RS_GroundModule.prefab`、`FX_Shard.prefab`、`FX_EliteBurst.prefab`

Codex 导入后请为这些生成物补 `.meta`。不要让用户生成。

## 怎么生成场景

菜单：`Waffle/Setup S2 Style Lab`。
批处理：`-executeMethod Waffle.S2.EditorTools.S2StyleLabSetup.Setup`。

可重复运行。只重建 `S2_StyleLab` 和 `Assets/Waffle/S2` 下的占位资源。不改 S1，不写 Build Settings，不弹「要覆盖重建吗」。若最终打开的场景路径不是 `Assets/Waffle/Scenes/S2_StyleLab.unity`，抛异常并停止，避免删掉别的场景。Unity 自己仍可能在「当前场景有未保存修改」时询问；取消后工具会停。批处理从干净场景启动即可。

成功日志：

`S2_STYLELAB_SETUP_PASSED placeholder=True enemyPlaceholder=True groundArt=False`

A-03 还没进 main 时，这三项就应是占位玩家、占位敌人、没有地面美术。

## 占位、概念、实际渲染

占位（代码里的立方体，不是主美模型）：

- 玩家根缩放 (1,1,1)，脚底轴心。Core 中心 (0.06, 0.72, 0)、缩放 (0.46, 1.32, 0.34)，顶约 1.38 m。Segment_A 中心 (0.10, 1.66, 0.08)、缩放 (0.58, 0.28, 0.18)，顶约 1.80 m。Segment_B 偏左下。Core 下有 Slit（局部发光），不参与动作。
- 敌人顶约 1.40 m：Core 顶约 0.98 m，Segment_A 中心 y=1.28、缩放 y=0.24。
- 地面：一块 60×60 的平面，再加一个 4×4×0.08 m 的地面模块。冷灰底色，正交 Size 9、Pitch 55、Yaw 45。没有 S1 的 Bloom Volume。
- 节点：

```
Player
  Visual
    RS_Player
      MotionRoot
        Core
          Slit
        Segment_A
        Segment_B
```

敌人由现有 `CombatViews` 放进槽位，预制体内部同样是 `MotionRoot / Core / Segment_A / Segment_B`。

概念（契约里的第一版色彩，用户还没批）：环境冷灰、玩家瓷白、少量钴蓝只出现在升级卡色条、敌人石墨、危险朱橙、经验暖金。URP Lit，哑光陶瓷、少量金属、局部自发光。没有复杂 Shader。

实际渲染：没有。本环境没跑引擎，没有截图。

## 玩法接线（数值没改）

场景复用 `GameLoop`、`CombatViews`、`VfxDirector`、`AudioDirector`，只订 `PresentationDispatched`。没有第二个状态机，没有新武器，没有永久成长。`runDuration` 仍是 180 s，`eliteTimes` 仍是 120/150/170。刷怪表也没改。所以这不是「场上永远只有 1 个敌人」的展览关；30–60 秒里会按现有第一阶段刷普通追踪敌。「1 个玩家、1 种敌人、1 个地面模块」指的是素材种类。

精英击杀在 120 s 之前不会出现。调试台默认隐藏，编辑器里按 F1。`givexp` 可以看三选一。`settime 119` 再打一会儿才能看到精英。`spawn` 强制 `elite=false`，不能用来冒充精英。

`S2MotionDriver` 执行顺序 -250，在 `GameLoop`（-100）之前记下当时仍存活的精英 `Serial`。死亡事件发出时敌人已经 `Release`，`TryGetEnemy` 只返回活着的，所以不能在事件回调里再查 `IsElite`。`EnemyKilled.Data` 是 Serial，`Value` 是宝石数值。精英判断用 Serial，不用宝石是否等于 10。

动作只写 `Core` / `Segment_A` / `Segment_B` 的 localPosition / localRotation：收束 → 错位展开 → 复位。不改根缩放，不改 `CombatViews` 槽位缩放，不推迟已经发生的攻击。只在 `Playing` 用 `RunSimulation.LastDt` 推进；暂停、升级、终局停住。`Restart` 和槽位被关掉时写回静止姿态。

## 没有叠第二套表现

- 射击声、命中声、闪避声仍只走 `AudioDirector`。剪辑暂时指向现有文件，不是 S-03：`Assets/Audio/Music/bgm_combat_master_64s_seamless.wav`、`bgm_levelup_suspended_loop.wav`，以及 `Assets/Audio/SFX/` 里 P-02 交接写过的那些 wav。缺文件只打警告。
- 炮口、命中碎片、死亡碎片、闪避残影、受击闪白仍只走 `VfxDirector`。样板不关它们，也不再做一套。
- `S2FxPlayer` 只补两样原来没有的：精英击杀地面环（Line）加短粒子，以及拾取的三粒暖金碎片。粒子用 `ParticleSystem.Simulate`，暂停时不靠 `timeScale`。
- 三选一只挂 `S2ChoiceHud`。调用 `GameLoop.Choose` / `TogglePause` / `Restart`。本场景不挂 `RunHud`；工具若发现 `RunHud` 会抛异常。卡片进入用 unscaled 时间，升级途中暂停则停住。色条是提案，不是已验收色卡。

## A-03 / S-03 之后怎么绑

不要直接合那两个分支。等 Codex 把它们进 main 之后，再在本分支 fetch。工具会按这个顺序读，玩家和敌人必须带 `MotionRoot`，否则继续用占位并打警告：

- `Assets/Art/S2/RS_Player.prefab` 或 `Assets/Art/S2/Prefabs/RS_Player.prefab`（也试 `Player`）
- `Assets/Art/S2/RS_Enemy.prefab` 或 `Prefabs/RS_Enemy.prefab`（也试 `Enemy`）
- `Assets/Art/S2/RS_Ground.prefab`、`Ground.prefab`、`RS_GroundModule.prefab`（地面模块不要求 MotionRoot）

S-03 的新音乐/音效不要在这个工具里改路径，除非 Codex 先定好 `AudioDirector` 的字段该指到哪。本轮没有改 `AudioDirector`。

## 请 Codex 改的共享缺口

1. 多 Mesh 残影。`VfxDirector.Awake` 在没有 SkinnedMeshRenderer 时只记住第一个启用的 MeshFilter。占位玩家先创建 Core，所以 Space 闪避时大概只有 Core 有残影，Segment_A / Segment_B 没有。玩法根缩放仍是 1。请不要在 S2 再写一个残影管理器。

   复现：跑菜单生成 `S2_StyleLab`，Play，Space。期望失败现象：残影只有 Core。

   建议接口（二选一，由 Codex 加在 `VfxDirector` 或玩家表现上）：
   - 对 `playerVisual` 下每个启用的 MeshFilter 各做一个残影槽（两段残影都要），复制 `sharedMesh`、世界位置、旋转、`lossyScale`。不要改玩法根的 scale。
   - 或者如果表现根上有组件提供 `int Count` 和 `Copy(int index, Mesh mesh, out Vector3 position, out Quaternion rotation, out Vector3 lossyScale)`，就走它；没有则保持现在的单 Mesh。

   验收：Space 后 Core、Segment_A、Segment_B 都能看见残影，alpha 仍是现有的淡出，玩家根 localScale 保持 (1,1,1)。

2. `CombatViews.Sync` 每帧把敌人渲染器的 `_BaseColor` / `_EmissionColor` 写成 S1 的橙红色，精英槽位 `localScale` 还要乘 1.6，并且会把预制体实例缩放到大约 1.5 / 包围盒高度。这是现有视图，不是本轮改的难度。石墨占位会被盖住。若样板要看材质本色，请给 `CombatViews` 一个默认关闭的开关，只在 S2 场景关掉染色；不要为了这个去改 S1 的默认外观。敌人死亡同一帧会被 `Sync` 关掉，所以击杀瞬间的分节动作在隐藏物体上，看不见；看得见的精英强调是地面环和粒子。

3. `S2_StyleLab` 不在 Build Settings（那个文件不在本轮可写范围）。编辑器里 `GameLoop.Restart` 会走已有的 `EditorSceneManager.LoadSceneInPlayMode`。打成 Windows 播放器后，场景不在 Build Settings 里就重开失败。请 Codex 在整合时把该场景加进播放器构建，不要让用户改。

## 建议的短检查

有 Unity 时：编译这两个新程序集；跑上面的菜单；确认日志三项；确认场景里没有 `RunHud`、玩家根缩放是 1、有 `MotionRoot`。然后 Play：移动、自动射击、Space、拾取、F1 `givexp` 看三选一和暂停，Esc 暂停时升级卡停住，重开后分节回到原位。精英用 `settime 119`，不要改 `eliteTimes`。没有这些记录之前，不要写样板通过。
