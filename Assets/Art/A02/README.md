# A02 独立参考素材说明 (A-02 Reference Assets)

本目录为 A-02 任务分配的**独立参考素材目录**（`Assets/Art/A02/`），供 2 号接入与用户视觉取舍参考，不包含或修改任何正式场景与共享材质。

---

## 一、资产清单与对应关系

| 文件名 | 格式 | 尺寸/格式 | 目标用途 | 对应材质/Prefab | 说明 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `George_Emissive_CyanMarker.png` | PNG | 2048x2048 RGBA (sRGB) | 玩家机甲局部发光遮罩 | `M_Player_HeavyMech.mat` (`_EmissionMap`) | 为玩家头部传感器/核心视窗提供青蓝（`#00E5FF`）光学信号点，其余全黑（遮蔽），避免受击闪白影响传感器 |
| `Mike_Emissive_OrangeAlert.png` | PNG | 2048x2048 RGBA (sRGB) | 重型前锋敌人警戒自发光遮罩 | `M_Enemy_Vanguard.mat` (`_EmissionMap`) | 为敌人前胸及散热格栅提供工业朱橙（`#FF4500`）微弱发光信号，区分敌我 |
| `Stan_Emissive_OrangeAlert.png` | PNG | 2048x2048 RGBA (sRGB) | 突击游击敌人瞄具发光遮罩 | `M_Enemy_Striker.mat` (`_EmissionMap`) | 武器瞄具与机体观察镜发光遮罩（朱橙 `#FF4500`） |
| `Leela_Emissive_OrangeAlert.png` | PNG | 2048x2048 RGBA (sRGB) | 蜂群骚扰敌人核心发光遮罩 | `M_Enemy_Flanker.mat` (`_EmissionMap`) | 蜂群微核心发光遮罩（朱橙 `#FF4500`） |
| `T_Trim_01_BaseColor_Reference.png` | PNG | 2048x2048 RGB (sRGB) | 场景建筑 Trim 贴图参照 | `M_SciFi_Architecture.mat` (`_BaseMap`) | 提取自 Quaternius SciFiKit 的标准 Trim 贴图，解决此前交接表中“找不到贴图”的疑问 |

---

## 二、2 号接入指引（明确参数，无需盲猜）

若 2 号在后续视觉微调中接入自发光遮罩：
1. **玩家机甲**：在 `M_Player_HeavyMech.mat` 的 `_EmissionMap` 槽位指定 `George_Emissive_CyanMarker.png`，`_EmissionColor` 设为 `Color(0, 0.9, 1.0, 1.0)`（强度 1.5）。
2. **敌人机甲**：在对应敌人物质材质的 `_EmissionMap` 槽位指定对应遮罩，`_EmissionColor` 设为 `Color(1.0, 0.27, 0.0, 1.0)`（强度 1.2）。
3. **低成本无贴图替代方案**：若 2 号暂不增加 EmissionMap 贴图采样开销，可在玩家 Prefab（`Player_George_Visual`）头部挂点创建一个 0.2m x 0.2m 的独立自发光小几何体（如微型 Quad 或 Cube，使用 `M_FX_Muzzle_Cyan` 材质），同样可实现第一眼视觉焦点锚定，零贴图改动。
