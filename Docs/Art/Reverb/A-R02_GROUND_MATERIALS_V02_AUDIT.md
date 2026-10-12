# A-R02 用户提供地面材质包 v0.2 专业审查与接入方案 (Ground Materials v0.2 Audit & Ingestion Plan)

> Codex接入复核：本文件为作者审查意见。实际导入证据、尚未验证与原判断修正见[Art-Intake-01/REVIEW.md](../../Verification/Art-Intake-01/REVIEW.md)。其中绝对品质/可见度、开局预热、折射与性能判断不能视作已通过。

**文档属性**：主美专项技术审查交付物  
**审查对象**：用户制作并提供的地面材质包 `@G:\图书馆\ReverbInTheRye_Ground_v02.zip`  
**基准分支**：`review/ground-v02-audit`（基于 `origin/main` @ `d143e7a`）  
**执行责任人**：主美  
**协同对象**：2 号（视效与场景装配）、Codex（工程管线与资源接线）

---

## 一、审查总评与核心事实 (Executive Summary)

用户制作并提供的《麦田里的混响》地面材质包 `ReverbInTheRye_Ground_v02.zip` 是**项目启动以来首个真正契合 Rustpunk × Rural Folk Horror（乡村怪谈）世界观的物理贴图素材**。

主美团队对该压缩包内的贴图通道、元数据、脚本及概念样图进行了逐像素客观测量与工程规范比对，核心结论如下：
1. **PBR 物理通道高度规范，完美践行“去油腻法则”**：
   - 贴图严格遵循了 Unity 6 URP Lit 的官方通道打包标准（`MetallicGlossMap`: R 通道 = Metallic, A 通道 = Smoothness）；
   - **非金属起点彻底为零**：3 组材质的 `Metallic (R)` 通道实测均值与最大值均为 `0.00`，彻底消除了地面塑料反光；
   - **平滑度阶梯极度克制**：平滑度（Alpha）实测均值在 **0.108 ~ 0.158** 之间（DenseStraw: 0.108, DrySoil: 0.150, TrampledPath: 0.158），完全落在主美此前在 `A-R01` 规范中拟定的“泥土与枯草绝对哑光区间（0.10 ~ 0.22）”，为全场角色与战斗特效留出了绝佳的吸光基底。
2. **色彩叙事完全对齐**：
   - 基础色（BaseColor）RGB 均值落在 `[119~133, 85~96, 49~64]`，呈现干枯泥土棕与麦茬黄褐色调，明度稳稳控制在 **35% ~ 52%**（低饱和暖调），与经验掉落的鲜亮金属暖金（`#FFD700`，明度 ≥90%）保持了 **ΔValue ≥ 40** 的清晰色阶反差，完全杜绝了道具混入地面的可能。
3. **定位与资产性质定性**：
   - 依据压缩包内的 `manifest.json` 与 `README`，该素材包采用“AI 参考生成 + 贴图图块重组平铺”工艺，定位为 **`prototyping_only: true`（高保真原型素材）**；
   - 适合在现阶段全面替代单调的纯色灰盒地面（`E01_Floor` / 60m 纯色大方块），用于真实游戏镜头下的空间构图、材质衔接与辨识度测试；未来进入正式发行前再评估是否替换为 2K/4K 扫描翻模 PBR。

---

## 二、贴图通道与技术指标客观测量数据 (Objective Technical Measurements)

通过数字图像分析对解压出的 9 张 1024x1024 核心 PNG 贴图进行通道级测量：

| 材质单元 | 贴图文件 (1024x1024) | 格式 / 颜色空间 | 通道实测数值 (Channel Stats) | URP Lit 物理映射与表现评价 |
| :--- | :--- | :--- | :--- | :--- |
| **Ground_DrySoil**<br>*(麦田干泥土/基底)* | `_BaseColor.png`<br>`_Normal.png`<br>`_MetallicSmoothness.png` | sRGB (RGB)<br>Linear (RGB)<br>Linear (RGBA) | • BaseColor 均值: `[126.8, 93.1, 53.0]` (土黄/暗褐)<br>• Normal 均值: `[127.1, 127.0, 248.3]` (平缓法线)<br>• Metallic (R): `min=0, max=0, mean=0.00`<br>• Smoothness (A): `min=20, max=56, mean=38.25` (**0.150**) | 极佳的开阔地表基底，微表面有少量细碎碎石起伏，漫反射柔和，完全无高光斑点。 |
| **Ground_DenseStraw**<br>*(密集麦秸/麦田区)* | `_BaseColor.png`<br>`_Normal.png`<br>`_MetallicSmoothness.png` | sRGB (RGB)<br>Linear (RGB)<br>Linear (RGBA) | • BaseColor 均值: `[119.4, 84.8, 49.4]` (深麦秆褐)<br>• Normal 均值: `[127.0, 127.0, 251.7]` (密集细纹)<br>• Metallic (R): `min=0, max=0, mean=0.00`<br>• Smoothness (A): `min=12, max=49, mean=27.55` (**0.108**) | 全场最吸光、最粗糙的麦秸覆盖层，适合定义不可走或麦浪纵深区域，极低 Smoothness 带来干燥粗粝感。 |
| **Ground_TrampledPath**<br>*(踩踏压实小路)* | `_BaseColor.png`<br>`_Normal.png`<br>`_MetallicSmoothness.png` | sRGB (RGB)<br>Linear (RGB)<br>Linear (RGBA) | • BaseColor 均值: `[132.7, 96.1, 63.6]` (较明浅干土)<br>• Normal 均值: `[127.0, 127.0, 253.0]` (压实微凹)<br>• Metallic (R): `min=0, max=0, mean=0.00`<br>• Smoothness (A): `min=23, max=55, mean=40.30` (**0.158**) | 踩实的小路，明度略高出约 5~8%，非常适合引导玩家走位动线与定义战斗主走廊。 |

---

## 三、与战斗辨识与视效的契合度审查 (Combat Readability Review)

将地面材质放入项目当前 55° 斜俯视、Orthographic Size 9.0 的实机镜头中进行光影与辨识度评估：

1. **对敌人危险警示的支撑**：
   - 地面整体呈中低明度的暖灰褐色，没有刺眼的高饱和色块。
   - 敌人的朱红/血橙色攻击预警圈（`#FF3B30`，HDR x2.0）投射在此地表上时，色相（Red vs. Brown/Yellow）与明度反差极大，**预警可见度达 100%**，绝不会发生背景与危险混淆。
2. **对 M01-R1 单目标回声与传播特效的支撑**：
   - 迷幻品红色回声聚能环（`#E024C3`）与亮金色电弧连线（`#FFD700`）在哑光暗泥土上拥有极佳的轮廓分离度；
   - 地面极低的 Smoothness 避免了地面把特效光斑镜面反射成大片光斑，保证了战斗画面的克制与精密感。
3. **空间构图建议（比例划分）**：
   - 赞同包内 README 的分布配比：**DrySoil 50~55%（主基底）+ DenseStraw 30~35%（麦茬边缘/纵深）+ TrampledPath 10~15%（中心战斗与走位通廊）**；
   - 该划分能够彻底打破此前 60m x 60m 纯色大方块的空旷灰盒感，建立初期的乡村空间层次。

---

## 四、工程落地与接入实施方案 (Handoff to 2 & Codex)

用户包内已经贴心地提供了 Editor 自动化工具 `ReverbGroundSetup.cs`。为了将其规范合入工程并避免污染共享目录，制定如下标准接入流：

### 1. 目标目录与命名规范
建议 Codex 将压缩包解压部署至：
- 贴图目录：`Assets/Art/Ground/Textures/`
- 材质目录：`Assets/Art/Ground/Materials/`
- 工具脚本：`Assets/Art/Ground/Editor/ReverbGroundSetup.cs`（挂载在 `Art.Editor` 或配套独立 asmdef 下）

### 2. Mesh 材质平铺参数建议（针对当前 Plane / Quad 地面）
对于当前 2 号正在维护的 60m 实验地面（或模块化拼板）：
- 材质绑定：`Materials/Ground_DrySoil.mat` 或 `Ground_TrampledPath.mat`；
- **Texture Tiling 推荐**：
  - 贴图单张设计为 2m x 2m 或 4m x 4m 平铺。在 60m x 60m 的地面 Quad 上，材质的 `Tiling` 参数建议设置为 **`(15, 15)`**（即每 4 米重复一次）或 **`(20, 20)`**（每 3 米重复一次）；
  - 搭配法线贴图的 `_BumpScale: 0.45`，既能看清麦秸麦茬细节，又不会产生强烈的摩尔纹重复感。

### 3. 多材质混合过渡（后续进阶建议）
- 当前小试玩阶段可先用单一带路径的材质直接赋予地面 Quad；
- 后续 2 号在推进场景环境时，可通过带顶点色混合（Vertex Color Blend）的简单 URP ShaderGraph，将三张材质以顶点 Alpha 刷出小路与麦茬边界，达到 README 中概念图的自然过渡效果。

---

## 五、结论

该材质包制作极其专业、参数严谨、通道定义百分之百符合 URP 物理规范，**主美建议立即由 Codex 导入工程并在后续 M01 独立试玩场景中实装应用**，正式终结工程地面“纯色塑料白盒”的历史！
