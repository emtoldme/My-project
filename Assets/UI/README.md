# 《麦田里的混响》UI 原型（锈蚀质感）

可直接在 Unity 中测试的 HUD 原型。风格：深色锈金属 + 麦穗元素 + 青色→品红信号光。

## 快速测试步骤

1. 拉取本分支 `feature/ui-rustpunk-hud`
2. 在任意场景中创建 UI Canvas（Screen Space - Overlay）
3. 在 Canvas 下创建空物体，命名为 `ReverbHUD`
4. 添加 `ReverbHUD.cs` 脚本
5. 按下面结构创建子物体并拖入脚本引用：

### 推荐层级结构

```
Canvas
└── ReverbHUD (挂 ReverbHUD.cs)
    ├── HealthPanel
    │   ├── Frame (Image, 颜色约 #5A4030, 透明度 0.95)
    │   ├── Fill (Image, Type=Filled, Fill Method=Horizontal, 初始颜色青色)
    │   └── Text (TMP_Text)
    ├── ModePanel
    │   ├── Glow (Image, 小圆形或方块)
    │   └── ModeText (TMP_Text)
    └── PausePanel (默认关闭)
        ├── Background (Image, 半透明深色)
        └── HintText (TMP_Text)
```

6. 把对应 Image / TMP 拖到脚本的公开字段
7. 进入 Play 模式测试：
   - **H** 减血 10
   - **J** 加血 10
   - **M** 切换模式（None / Echo / Propagation / Combined）
   - **Esc** 暂停 / 继续

## 视觉建议（无正式贴图时）

- Frame / 面板背景：深锈色 `#3A2A20` ~ `#5A4030`
- 边框可加 Outline 或 Shadow 组件模拟厚度
- Fill 使用脚本中的 Gradient（青 → 品红）
- 文字颜色：暖白 `#F5E8D0`
- 如需更强质感，可将之前生成的概念图导入为 Sprite，设为 9-slice 后替换 Frame

## 文件位置

- `Assets/UI/Scripts/ReverbHUD.cs`
- `Assets/UI/README.md`（本文件）

这是原型，正式美术仍由项目负责人验收与替换。
