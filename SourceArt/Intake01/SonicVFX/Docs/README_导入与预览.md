# 麦田里的混响 · 声波攻击反馈 VFX v0.1

> 交付类型：Unity 6 / Universal Render Pipeline (URP) 的 **技术可运行候选源码**，包括两份 Shader、一个 C# 视觉管理器和一个 Editor 一键安装菜单。
> 本交付没有 Unity Editor 实机或 GPU 渲染结果；Shader 编译、视觉效果、性能、Input System/工程兼容性均待 Codex 在真实工程中验证。不是正式的美术定稿。

## 0. 定位与艺术约束

作品：**《麦田里的混响》 (Reverb in the Rye)**。

锁定方向：**锈蚀朋克 Rustpunk × 美国乡村怪谈 × Shoegaze × 迷幻音波**。
场景底色：秋收后的干草、黄土、锈蚀农机、昏暗天空；声波不是蓝色激光，也不是工业机甲枪炮。特效通过电波干扰、空气折射、不稳定共振、麦秆及尘埃产生画面识别度。

| 阶段 | 主要画面 | 强调色 | 脉冲节奏 | 粒子行为 |
| --- | --- | --- | --- | --- |
| 发射 | 调频器暖白核心 + 两圈相互干涉的脉冲 | 青蓝 `#77D9D4` + 蓝紫 `#9780C2` | 0–0.18 s，余辉至 0.4 s | 少量青色 radio motes 沿射击方向飞散 |
| 命中 | 暖白闪光 + 紫红 / 蓝紫冲击环 | `#F1E6C7` + `#B863A0` | 0–0.3 s，碎屑至 0.7 s | 11 个金褐尘粒/麦秆碎屑 |
| 死亡 | 三圈不规则干涉波，带短暂错位的核心 | 紫红、蓝紫、青蓝、金褐 | 约 0.7 s 波形，1.05 s 对象生命周期 | 23 个麦秆灰尘粒扩散并下坠 |

注意：v0.1 仅实现发光环与粒子造成的信号崩解感，**没有对敌人现有 SkinnedMeshRenderer 做真正的模型抖动 / 错位材质处理**。禁止将“死亡模型信号错位已实现”写入验收报告。

## 1. 导入（Unity 6 URP）

1. 将包内 `Shaders/`、`Scripts/`、`Editor/` 三个文件夹完整复制到 `Assets/ReverbInTheRye/VFX/` 下。
2. 等待 Unity 导入和编译完成，检查 Console 是否有任何 C# / Shader 报错。
3. 点击 `Tools → Reverb in the Rye → Setup Sonic VFX Preview`。
4. 菜单会在 `Assets/ReverbInTheRye/VFX/Materials/` 下创建两个材质 `M_SonicPulse.mat`、`M_SonicStrawDust.mat`，并创建场景物体 `SonicAttackVFX_Prototype`，把引用连好。
5. **保存场景**，进入 Play Mode；在 `SonicAttackVFX` 组件右上角三个点菜单选用 `Preview / Fire At This Object`、`Preview / Hit Two Metres Ahead` 或 `Preview / Death Two Metres Ahead`。
6. 在真正的游戏**俯视相机**下看清楚波纹。当前波纹位于 XZ 面，靠近地面时应避开 z-fighting；可调整呼叫时的世界坐标高度。

> 菜单具有基本幂等性：不重复建材质，不重复建 emitter；不会自动绑定玩家、敌人、伤害、音效或动画。

## 2. 战斗事件接入（只由 Codex 做技术验证）

调用入口在 `ReverbInTheRye.VFX.SonicAttackVFX`，API：

```csharp
// 发射：在射击事件完成且弹丸/声波确实发射时调用，勿放在每帧 Update 中。
vfx.PlayFire(muzzleTransform.position, muzzleTransform.forward);

// 命中：伤害逻辑完成一次有效命中时，传世界坐标。
vfx.PlayHit(hitWorldPosition);

// 死亡：敌人死亡状态仅触发一次，在敌人 GameObject 被禁用或销毁前调用。
vfx.PlayDeath(enemyWorldPosition + Vector3.up * 0.25f);
```

必要规则：

- 战斗系统决定是否命中、损伤多少、死亡与否；VFX 仅响应事件，绝不自行触发伤害。
- VFX emitter 必须属于**持久场景对象**，不要挂在会被 Destroy 的敌人子物体下。
- 已死亡敌人必须保证一次死亡事件，不要既在 `Health.OnDeath` 又在 `EnemyController.Die()` 重复触发。
- 首先在独立测试场景中连接伪事件，运行后再用 Adapter 接真实接口；不要擅自覆盖用户战斗系统。
- 要保留用户提供的正式 Blender 模型、贴图、UI、音乐；这份包只包含临时可替换的 Shader 和粒子实现。

## 3. 可调字段

在 `SonicAttackVFX` Inspector：

- `Visual Scale`：统一大小。建议先 1。若俯视视角太小，可试 1.25–1.5。
- `Max Fire / Hit / Death Instances`：每类对象池上限，超出时回收最老的同类效果。
- 材质引用由菜单生成，不需要手动寻找 Shader。

具体时序、颜色、扭曲度、粒子量在 `CreateEffect` / `MakeParticles` 内配置，后续可将美术确认的参数整理成 ScriptableObject 供调节。

## 4. 验收清单

- [ ] Unity 6 + URP 打开 Console 无 C# 和 Shader 编译错误；所有效果材质无紫红色错误渲染。
- [ ] 发射在枪口世界坐标播放 1 次，未开火不播放。
- [ ] 命中仅在有效命中显示，画面颜色与发射区别明显。
- [ ] 死亡仅播放一次，敌人实体删除后 VFX 不随之被销毁；尸体与事件顺序正常。
- [ ] 游戏真实俯视相机下可读，圈不会穿得过分、不遮挡敌人、不与地面明显 z-fighting。
- [ ] Play Mode 连续开火 2 分钟，Frame Debugger/Profiler 检查每帧分配、ParticleSystem 数量和显著性能尖峰；必要时减少对象池上限和粒子数。
- [ ] 开启、关闭 Bloom 对比，确认低亮度场景下没有过曝；不能以 Bloom 掩盖材质问题。
- [ ] 验证 SRP Batcher 是否受 MaterialPropertyBlock 用法影响，并测实际 Draw Calls；不要声称它自动获得 SRP Batcher 优化。
- [ ] Development Build 中 shader 未被剥除：测试一回发射、命中、死亡。
- [ ] 仅提交相关新增 VFX 目录 / 原有授权集成文件及 `.meta`，不要改动美术与音乐素材。

## 5. 适配说明与已知取舍

- 这是 **Unity 粒子系统 Particle System** 的版本，不依赖 VFX Graph 或 Shader Graph，也不要求额外安装 VFX Graph 包。
- 使用 URP `Core.hlsl`、可直接渲染的透明 Unlit Pass；规范参考 Unity 6 URP shader 文档，但本包仍需在用户真实工程中编译确认。
- XZ 平面波纹主要服务俯视视角；它并非空间体积冲击波，需要调整相机遮挡、材质混合及敌人表面深度表现。
- `MaterialPropertyBlock` 是为了各次共振波拥有独立颜色/透明度；大量同时存在时请实测 SRP Batcher 与 draw call 开销。
- 粒子和环是占位可替换实现；正式贴图、模型、音频及统一美术由项目负责人提供。
