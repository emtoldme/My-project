# 声学花园：当前视觉方案

动态光影和天气增量见 [天气说明](WEATHER_LIGHTING.md)：雨天/雷雨/晴天过渡、移动云影、扫描灯软阴影、风雨、水花、积水涟漪和湿润材质。

2026-10-13。用户要求 Codex 直接重构画面，暂不联系其他协作 AI。本轮主场景采用金色麦田、象牙色陶瓷、石墨色机械结构、少量香槟金和玉绿色信号。目标是自然材质衬托精密设施，战斗与环境有清晰层次。是否达到用户期望的优秀画面，以实际试玩验收为准。

Unity 6000.4.3f1、URP17.4 Forward+。标准 URP Lit / Terrain Lit 构成环境材质，4级联4096软阴影、SSAO、HDR调色、ACES、SMAA和轻量 Bloom；一个宽覆盖反射探针使用标准非图集路径。没有更换渲染管线、安装第三方渲染插件或引入光追依赖。功能依据可见 [Unity 渲染管线功能对照](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html)，实际 API 以本地17.4包与构建结果为准。

地面使用96米 Terrain、多层纹理混合和克制的法线/光滑度。麦田采用一张 RGBA 图集、3向交叉片、4份共享片区网格、GPU实例化、风动、与可见轮廓一致的透明裁切深度/阴影。玩家附近采用稳定抖动淡出，减少遮挡；道路遮罩来自 Terrain 第0层，通过工作台手动刷新。共享片区网格总量约0.88MiB，避免为每个片区复制整份高细节植被。

现有机甲模型保留原模型和图集，URP PBR 着色器重新组织陶瓷、深色关节和金属配色。环境模型使用实际倒角、结构接缝、鳍片、紧固件和薄环饰边。悬吊设施给玩家保留净空；边界外支撑不引入新的玩法碰撞规则。

子弹、碎片、环绕物和拾取物使用独立 Prefab。子弹拖尾在对象池序号改变时清空；技能保留实际规则半径、派生事件和现有池预算，增加柔边光晕、碎光与命中收束/扩散。环境动效读取局内时钟，升级和暂停时保持冻结。HUD只是读取规则状态与调用现有选择命令，不自行发放奖励。

临时重建、截图脚本归档在本机 Reviews/VisualRebuild-2026-10-13，不进入最终 Assets，也不会自动覆盖后续手工修改。持久保留的美术工具只有目录刷新、资源检查、构建、工作台和道路植被遮罩更新。

## 新图集来源

使用内置 `image_gen`，透明背景生成，保留原始 RGBA，不对图片做后处理。工程保存位置 `Assets/Game/Art/VisualIdentity/Textures/Rye_Atlas_RGBA.png`。这是一张生成素材，不是第三方扫描植被包。原始生成文件也保留在 Codex generated_images。

完整提示词：

> Use case: photorealistic-natural. Asset type: Unity foliage diffuse RGBA atlas for alpha clipped crossed billboards in a high quality stylized PBR 3D game. Create a wide landscape transparent atlas, exactly four isolated ripe rye / wheat clumps side by side in four equal horizontal cells. Each clump is a FULL plant from soil-level roots at bottom to delicate heavy golden seed heads at top; 8 to 12 naturally curved slender stalks with sharply defined individual plump wheat grains, fine awns and beautifully curling narrow sage green leaves. Ripe pale gold, honey cream and muted olive, not orange or dead brown. Plants are alive, clean, elegant, cultivated. Four distinct silhouettes, medium dense lush tufts, softly uneven heights. Neutral diffuse daylight, flat balanced albedo illumination, no cast shadows, no hard specular highlights, no sun baked into colors. Botanical photographic detail with art-directed clean readable shapes. Orthographic frontal elevation, no perspective view, no ground, no pots, no rocks, no extra objects, no labels, no text, no border, no dividing lines. The four plant clusters do not overlap each other or their cells, each fits entirely with generous transparent margins; all roots have the exact same bottom baseline. Most important: actual transparent alpha between all the fine stalks, leaves, awns and around each silhouette, not a painted checkerboard. Four clusters should fill most of the canvas height, remain highly detailed at 1080p game resolution. Intended to look like beautiful wheat in a calm golden meadow framing a precise ceramic and teal acoustic installation.
