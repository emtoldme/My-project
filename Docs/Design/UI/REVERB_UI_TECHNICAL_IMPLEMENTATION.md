# 《麦田里的混响》(Reverb in the Rye) UI 技术工程落地与实装全案 (Technical Implementation Plan)

**版本：** v1.0 Production Implementation  
**设计定位：** 80s 模拟音频与田野探测仪美学（80s Analog Audio / Field Instrument Aesthetic）  
**工程目标：** 解决此前“纯文字臆想、脱离实际管线”的弊端，紧密依托 Unity 6000.4.3f1 / URP 17.4 与现有只读观察者架构，提供**直接可运行、高帧率、零 GC 浪费、极具 80 年代电台模拟质感**的完整工业级 UI 落地全套技术方案。

---

## 一、架构定位与契约边界 (Architectural Boundaries)

依据项目的 `AGENTS.md`、`COMPONENT_PIPELINE_CONTRACT.md` 与战斗核心契约：
1. **纯只读观察者（Strictly Read-Only Observer）**：
   - UI 绝对不持有第二套局内状态，不写 `Time.timeScale`，不监听物理 Esc，不劫持输入逻辑；
   - 纯粹通过 `IRunContentViewSource.ReadView()` 与 `GameLoop.Sim.State` 驱动表现；
2. **零 GC 与单批次渲染（Zero-Allocation & Minimal Batching）**：
   - 常态战斗 HUD 采用单张 `HUD_Atlas.png`（由 `Assets/Game/Art/UI/Slices/` 切片集整合生成）；
   - 运行时避免任何字符串拼接，数字格式化复用预热字符缓存，Draw Call 严格控制在 3~6 个；
3. **两套渲染体系平滑过渡（Two-Tier Transition Strategy）**：
   - **Phase A（即刻生效层）**：重塑 `RC01GrowthHud` 的 IMGUI 皮肤层，赋予其真实的电木面板纹理底板、带内凹阴影的卡牌背景与高辨识度机制颜色（立即在 Play 模式中呈现优质质感）；
   - **Phase B（正式 UGUI 层）**：建立 `ReverbCombatHUD` 预制体（Canvas Overlay），集成受击白条残影、顺时针 CD 扇形遮罩与频率刻度经验条，无缝替换调试显示。

---

## 二、已生成的物理切片资产与 9-Slice 规制 (Generated Production Slices)

切片物理文件已存放于 `Assets/Game/Art/UI/Slices/`，全部配置 Sprite (2D/UI) 与精准 9-Slice 边框（Border）：

| 纹理切片文件名 | 原始尺寸 | 9-Slice Border (L,B,R,T) | 对应 UI 控件与材质质感 |
|---|---|---|---|
| `UI_Frame_Bakelite_64.png` | 64×64 | `(16, 16, 16, 16)` | 氧化深褐电木面板主外框，带四角十字沉孔螺丝与铜绿微线 |
| `UI_Panel_Recessed_32.png` | 32×32 | `(8, 8, 8, 8)` | 仪表内凹阴影槽、子模块背景，呈现压铸下沉质感 |
| `UI_Card_Frame_128.png` | 128×128 | `(24, 24, 24, 24)` | 三选一成长卡牌外框，做旧军工橄榄铁皮 + 铜边加固 |
| `UI_Button_Rust_64.png` | 64×64 | `(16, 16, 16, 16)` | 实体琴键按压开关、操作按钮，顶层带高光光泽 |
| `UI_XP_Track_128.png` | 128×16 | `(6, 6, 6, 6)` | 顶部收音机波段刻度尺背景（含 88~108 MHz 细分刻度点） |
| `UI_XP_Fill_32.png` | 32×16 | `(4, 4, 4, 4)` | 温暖麦穗金高亮经验填充条（`#E5A93B` → `#F5C869`） |
| `UI_Needle_Cursor_8.png` | 8×20 | `(2, 2, 2, 2)` | 垂直红色收音机调谐游标指针 |
| `UI_HP_Track_64.png` | 64×20 | `(6, 6, 6, 6)` | 底部音频 VU 电平分段血槽背景 |
| `UI_HP_Fill_32.png` | 32×20 | `(4, 4, 4, 4)` | 电平表头象牙暖黄生命水银柱（`#E2DCB8`） |
| `UI_Stagger_Fill_32.png` | 32×20 | `(4, 4, 4, 4)` | 受创即时白条残影（`#FFF3D1`，提供打击重量感） |
| `UI_Shield_Fill_32.png` | 32×8 | `(3, 3, 3, 3)` | 顶部电台示波冷青护盾条（`#38B2A3`） |
| `UI_Slot_Weapon_52.png` | 52×52 | `(10, 10, 10, 10)` | 4 武器卡带机按键槽位（深灰金属内凹） |
| `UI_Slot_Passive_42.png` | 42×42 | `(8, 8, 8, 8)` | 6 配件紧凑槽位（金属螺栓加固） |
| `UI_LED_Recording_16.png`| 16×16 | `(0, 0, 0, 0)` | 战局进行时慢速呼吸录音指示灯（REC ●） |

---

## 三、四大核心动态交互的技术实现算法 (Dynamic Feedback Algorithms)

### 1. 扣血白条残影（Damage Stagger Lag）
- **痛点**：传统血条扣血瞬间直接变短，玩家在激烈走位中难以感知被什么打中了、掉了多少血；
- **实现算法**：
  - 维护当前实际血量 `actualHp` 与残影血量 `staggerHp`；
  - 受到伤害时 `staggerHoldTimer = 0.25f`（冻结 0.25 秒）；
  - 冻结结束后，残影在 0.2 秒内匀速平滑追平；
- **表现**：底层为深黑底槽，中层为象牙白受创残影（Stagger），顶层为当前即时血量。挨打瞬间立刻留下一道清晰亮白残影，打击沉重感极强。

### 2. 顺时针 360° CD 扇形遮罩（Radial Wipe Cooldown）
- **痛点**：文字倒计时在 10 几发子弹同时开火时无法直观识别；
- **实现算法**：
  - 武器槽上覆盖一层黑色半透明 Sprite，组件设置为 Unity 原生 `Image`，`ImageType = Filled`，`FillMethod = Radial360`，`FillOrigin = Top`，`Clockwise = true`；
  - `fillAmount = Mathf.Clamp01((weapon.ReadyAt - now) / weapon.Cooldown)`；
  - 冷却归零时，触发 0.1s 边框亮青色发光闪烁，直观反馈下一次射击就绪。

### 3. 收音机频率平滑推进与调谐游标（Tuning Dial XP Lerp）
- **实现算法**：
  - 经验进度通过 `Mathf.Lerp` 平滑插值，游标指针的 `rectTransform.anchoredPosition.x` 与 `fillAmount * totalWidth` 同步；
  - 每次拾取经验，游标伴随 `Random.Range(-0.5f, 0.5f)` 的高频微震，模拟无线电接收到微波信号时的机械电表指针颤动。

---

## 四、落地实施路径 (Action Roadmap)

1. **第一步（已完成）**：全套 16 种高精度 9-Slice 切片贴图生成并导入 Unity 资产库；
2. **第二步**：将切片纹理赋予当前主场景的成长 HUD，在不破坏任何既有游戏逻辑的前提下，将三选一卡牌和常驻状态升级为高质感电台面板；
3. **第三步**：为 Codex 和装配组提供开箱即用的 UGUI 预制体代码，实现专业级的战斗 HUD。
