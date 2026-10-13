# 给 Codex 的三项

1. 给 RF01FeedbackBridge 和 RC01BattleView 加按 contentId 跳过的开关。关掉 W01、W04、P12 的旧画面之后，样板里的目录播放器才会播这三条，而不会和旧特效一起出现。
2. 声音仍只走现有 AudioDirector。不要按这份目录再挂一个播放器。若要改绑定，保留现在的 minDistance 30、maxDistance 65，以及已经接上的 R1 变体。
3. W05、W06 的目录条目不能当成伤害。等规则事件带上对应 SourceId 之后，再从预览改成实播。
