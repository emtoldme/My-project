# 探索R4：无空气墙的程序化麦田

2026-10-14。用户要求去掉明显空气墙，并程序生成可探索的麦田地图。本轮从视觉R3继续，保留暖色乡村、角色辨识、天气和既有战斗基底，增加连续地块生成。

## 空气墙的来源与修复

原RunSimulation每次移动都把玩家坐标限制在±29.5米；敌人移动、击退、生成点、弹丸回收以及遭遇/首领生成也默认使用原点附近的固定方形场地。只放宽玩家会导致远处战斗失效。

新增显式unboundedWorld模式，Main开启。该模式移除玩家行走/闪避、敌人追击/击退和弹丸的绝对方形边界，并让普通遭遇与首领在探索者当前位置附近合法生成。有限数值校验、攻击距离/寿命、刷怪与对象池预算继续有效。旧场景默认关闭该模式，原有限场地行为保持。

原田边连续围栏和边界石在Main停用，不添加新的不可见碰撞。新生成地面、麦田和地标均无阻挡碰撞体。

## 连续地图

ProceduralFieldLayout按世界坐标和地图种子生成弯曲田间道路、路口、密麦、收割麦茬、空地、成熟度变化和零散农业/广播地标。生成不依赖地块加载顺序、不消耗玩法随机流。相邻地块用同一世界坐标采样，道路、地面权重与纹理连续；负坐标使用向下取整。

出生区读取原96×96米Terrain涂层和R3田野整理纹样，保留主要手工布局；39～48米过渡带与外围生成田野衔接。原Terrain与旧植被资源保留作编辑预览，运行时隐藏，换成与规则XZ平面一致的平坦地面。角色、水花、脚步与危险边界使用该平面，避免越界后错误采样旧Terrain边缘高度。

每个地块24×24米，默认加载玩家周围7×7、最多49个地块。近处九块在首个可玩帧前完成，外圈逐帧生成；地面底衬覆盖加载过程。跨地块时优先补齐目标附近区域，回收离开的地块并重用网格，地标随所属块卸载。持续探索不会不断堆积地面与植被对象。

生成器复用原麦穗网格和图集、三种地面图层及其法线，沿用风雨、附近掉落压草和角色周围淡出。地标有农机、旧秋千与暗淡广播设备三类，布置在路口附近空地；新农机灯有实体光源，随雨势调整。原出生区生活道具和广播装置继续保留。

## 使用与范围

Main中GameLoop上的ProceduralFieldStreamer可调整worldSeed、randomizeSeed与loadRadius。默认每次进入场景生成新地图种子；关闭randomizeSeed可用指定种子复现。公开Seed属性和启动日志记录本局实际种子。

现有两处调谐锚点仍在出生区域，首领随探索者的位置生成；原10分钟一局的战斗流程继续使用。本轮提供连续地图探索，未改武器、成长、奖励数量或战斗时长。

核心文件：

- Assets/Game/Presentation/Gameplay/ProceduralFieldLayout.cs：坐标采样与地标布局。
- Assets/Game/Presentation/Gameplay/ProceduralFieldStreamer.cs：有限地块驻留、网格生成/回收及清理。
- Assets/Game/Art/VisualIdentity/Shaders/FieldGround.shader：世界空间图层混合、法线、湿润、阴影与雾。
- Assets/Game/Environments/ProceduralFields：地面材质与三种地标Prefab。
- Assets/Waffle/Tests/EditMode/ExplorationBoundaryTests.cs：本轮规则边界定向用例。

## 模块验证

7项规则用例通过：四方向行走/闪避越过旧边界、旧有限场地仍限制边界、远坐标敌人追击/弹丸命中，以及普通遭遇/首领跟随远处玩家生成。

14项真实场景检查通过：Main接线、种子复现与差异、负坐标分块、着色器编译、地块接缝权重、无碰撞替换、实际越过旧空气墙、外围地标、池预算、2公里外战斗、暂停、返回布局复现、关闭后恢复和运行时网格释放。检查记录累计生成196块，驻留仍为49块，设备RTX4070 Laptop GPU，检查地图种子1612691715。

日志与截图在上级Reviews/ExplorationR4-2026-10-14：boundary-tests.xml、play-review.log、home-streamed.png、field-landmark.png、far-field.png。仅验证当前模块，没有重跑整局回归或全面性能矩阵。编辑器搜索索引/AI插件的已有日志噪声不属于本模块验证结论。

源码分支world/procedural-fields-r4，接续art/harvest-homecoming-r3。Windows试玩在上级Builds/ExplorationR4/ReverbExplore.exe，保留整个目录。一次性安装、采图、构建脚本归档在Reviews的Tools，不留在正式Assets自动执行。

Windows构建Succeeded，0 errors，487 warnings，与R1～R3的警告数量相同，主要为现有Sentis计算着色器变体；本次地面/植被活动着色器编译检查通过。实际构建记录见试玩目录的build-summary.txt和该Reviews的build.log。
