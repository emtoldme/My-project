# RC01 场景入口

基线 `b8d3931c83bf8fc94d678f6c9c127fa6b6a478d4`。分支 `feature/rc01-scenes-growth-ui`。

菜单 `Experiments/Create RC01 Scene Previews`。

```text
-executeMethod Reverb.Drafts.RC01.EditorTools.RC01SceneSetup.CreatePreviews
```

只生成 `Assets/Drafts/RC01/Environments/` 和 `Scenes/` 里的四张环境 Prefab、四张预览场景，以及 `RC01_GrowthPreview`。不调用 RF01 或 M01 的 Setup，不改 Build Settings。若当前开着那些场景，工具会停。

成功日志：

```text
RC01_SCENE_SETUP_PASSED maps=4 harvest=GameLoop growth=waiting preview=TEST_ONLY
```

本环境没有 Unity。场景和 Prefab 还没生成，没有截图，布局不能写成已经好看。战斗草稿在 [SCENARIOS.md](SCENARIOS.md)，接线请求在 [REQUESTS.md](REQUESTS.md)。
