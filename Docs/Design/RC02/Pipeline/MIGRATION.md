# 旧表现和目录

## Codex 整合后的接管表（2026-10-13）

| 内容 | 规则入口 | 当前表现 |
|---|---|---|
| W01 | 原 CombatModule；组件迁移样板关闭 | 目录接管 Shot，旧桥保留计数 |
| W04 | 原 CombatModule | 保留旧预警；目录接管生效 WeaponZone |
| P12 | 原延迟结算 | 目录接管 EchoHit；预备提示保留旧桥 |
| W05 | RunComponentRuntime → 唯一伤害链 | EnemyHit + W05，From 为上一跳，链元数据透传 |
| W06 | 合法位移 → ResidualZoneField | 真实存活点快照，三阶连线、五阶成功闪避引爆 |
| P17 | 成功闪避通知 → 组件 AreaDamage | WeaponZone + P17，失败按键不发事件 |
| 其余旧事件 | 原入口 | 未接管事件继续原表现 |
| 声音 / 音乐 | 唯一 AudioDirector | RC02AudioConfiguration 注入目录 |

旧桥查询 IPresentationEventOwner，只跳过被接管的表现。W05 每次释放仅首跳允许回声/传播；派生链、W06/P17 不再起派生链。死亡、经验、掉落仍归原结算。素材替换需修改清单、刷新装配并重新打包。当前仅为测试模板基础。

---

以下为整合前历史表，不代表当前运行状态。

| 内容 | 现在谁在播 | 目录条目 | 样板里的处理 |
|---|---|---|---|
| W01 发射 | RF01FeedbackBridge | rc02.art.w01.shot / rc02.audio.w01.shot | 画面不双播。声音仍由 AudioDirector 播 |
| W04 落区 | RC01BattleView | rc02.art.w04.zone / rc02.audio.w04.zone | 同上 |
| P12 回声命中 | RC01BattleView | rc02.art.p12.echo / rc02.audio.p12.echo | 同上 |
| W02、W03 和其他旧事件 | 原来的桥 | 没有新条目 | 继续旧绑定 |
| W05、W06 | 还没有规则 | 只作预览 | 不接伤害 |
| 战斗 / 升级音乐 | AudioDirector | rc02.music.harvest | 不新建音乐宿主。bpm 为 0 |

目录播放器发现这些旧组件还在同一个 GameLoop 上时，会计数并跳过。预览按钮不受这张表影响，预览物体不会存进场景。
