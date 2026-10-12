# 旧表现和目录

| 内容 | 现在谁在播 | 目录条目 | 样板里的处理 |
|---|---|---|---|
| W01 发射 | RF01FeedbackBridge | rc02.art.w01.shot / rc02.audio.w01.shot | 画面不双播。声音仍由 AudioDirector 播 |
| W04 落区 | RC01BattleView | rc02.art.w04.zone / rc02.audio.w04.zone | 同上 |
| P12 回声命中 | RC01BattleView | rc02.art.p12.echo / rc02.audio.p12.echo | 同上 |
| W02、W03 和其他旧事件 | 原来的桥 | 没有新条目 | 继续旧绑定 |
| W05、W06 | 还没有规则 | 只作预览 | 不接伤害 |
| 战斗 / 升级音乐 | AudioDirector | rc02.music.harvest | 不新建音乐宿主。bpm 为 0 |

目录播放器发现这些旧组件还在同一个 GameLoop 上时，会计数并跳过。预览按钮不受这张表影响，预览物体不会存进场景。
