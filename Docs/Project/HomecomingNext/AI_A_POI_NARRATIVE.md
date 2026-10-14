# 给外部 AI-A：HC-A 兴趣点、世界布局与全物品叙事

请直接实施，不只写方案。工程《麦田里的混响》，Unity6000.4.3f1/URP17.4。先读工程AGENTS.md及`Docs/Project/HomecomingNext/README.md`、`RELEASE.md`、`CONTRACTS.md`、`CONTENT_SEED.md`。从标签`hc00-v1`在自己的独立目录建立`gameplay/hc-a-places`，不要从main/旧gp00-v1开始，不共享用户或其他AI工程。

目标：程序麦田中的可辨识叙事地点，玩家靠近可以获得真正的奖励意图，地点/物品都有可查看文本，返回被卸载的地块不重复领取。首批必须有主角老房子、老筒仓、老吉他所在角落；保留R3暖乡村方向，读Docs/HOMECOMING_R3.md与Docs/RESONANCE_R2.md。

## 你独占的文件

- 新增`Assets/Waffle/Scripts/Rules/Homecoming/Places/`：实现`IPoiModule`、有限地点布局及内容目录。
- 新增`Assets/Game/Presentation/Homecoming/Places/`：只读地点表现与布局适配器，不写HP/XP，不新建GameLoop。
- 新增`Assets/Game/Environments/HomecomingPlaces/`：地点Prefab与必要素材，标来源；用Unity API制作，保留.meta。
- 新增`Assets/Waffle/Tests/EditMode/HomecomingPlacesTests.cs`与`Docs/Project/HomecomingNext/DELIVERY_A.md`。
- 不改共享HomecomingContracts.cs、GameLoop、RunSimulation、现有streamer/layout、Main、HUD、音频和其他包目录。需要共享适配列在交付中，由Codex完成。

## 必须实现

1. `IPoiModule`按共享契约实现注册、发现、靠近候选、领取意图、确认及重开清理。半径初版2.4米，发现范围初版18米；玩家靠近默认自动领取，不依赖强制按键。离开可再来，奖励未被B接受时不能先标已领取。
2. 唯一旧家与筒仓布局可复现，旧吉他保底在第一圈外；固定Seed、负坐标、不同探索顺序结果一致，不消费战斗RNG。说明坐标/道路安全落点/保留格。近景少而精；别把每个程序路口都复制成主角家。
3. 按模板发放内容种子中的`PoiReward`；同模板第二站不能再加槽/重复发唯一遗物。领取账本不绑定Prefab生命周期。每tick候选与每局发现记录有有限预算，给出超预算处理。
4. 提供统一叙事目录查询`NarrativeEntry Resolve(string id)`与可枚举条目。地点3、遗物3、目标3、永久升级3均有独立中文正文，目标完成补文可另用稳定后缀ID。对**现有全部实际攻击/被动/进化/拾取物**补叙事条目，以真实RunContent目录ID为准，当前约27条；不改既有数值、名称ID或原目录代码。任何新增可交互物品都必须进目录，不能只用“+8%生命”代替故事。
5. 老房子/筒仓/吉他用可识别几何与风化材质交付Prefab，保持普通敌人和掉落可见，不用巨型发光圈当唯一识别。门前/仓旁留走位空间；首轮建筑为外部可接近，不强制室内导航或新增碰撞空气墙。保持已有农机、秋千。
6. 地点表现只读取PoiView；暖灯源有实体，异常冷光与真实激活关联。详细艺术质量由用户试玩验收，不能自称达到商业品质。

## 交付与验证

先交A1纯规则+完整文本目录，再交A2三处Prefab/布局适配器与接线样例。验证仅本模块编译、固定Seed/负坐标、靠近范围、旧RunId拒绝、重复注册/领取/确认、失败可重试、卸载返回、暂停/升级/死亡/重开。核对叙事覆盖全部实际目录物品。Main由Codex接线；可提供独立测试适配器/临时工具，标清未完成正式接入，正式Assets不留自动重建主场景脚本。

提交分支/完整SHA/PR、文件清单、创建模块/目录/适配器示例、布局保底说明、验证结果、已知问题、Codex需改的共享接点。PR目标`gameplay/homecoming-next-ready`，不推main、不强推，不自行合B/C/D。
