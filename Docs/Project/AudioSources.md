# S1 音频资产授权与来源完整清单 (Audio Asset Provenance & License Record)

> 2026-10-12：核验状态：下表为作者提供的来源/许可声明，尚缺本轮独立核验的原始文件对应、许可页与加工链。不得将此页作为已完成授权审查；S-03补证。项目主题名“会共振的雕塑”是工作提案，未记录用户批准正式游戏名。 统一事实与证据见[收拢审查](../Verification/Handoffs-2026-10-12/REVIEW.md)。

**项目名称**：《会共振的雕塑》（Resonant Sculpture / Waffle S1）  
**维护岗位**：音效总监（Sound Director）  
**生效基线**：S-02 阶段（基于 `origin/main` 实际使用的音频文件）  
**母带格式**：48 kHz / 24-bit PCM WAV  

---

## 一、实际使用素材来源与授权总表

作者报告以下来源与许可，实际音频对应关系与证据仍待补齐；本轮不作绝对授权或法律合规结论。

| 资产文件名 | 游戏内用途 / 事件 | 原作者 | 原始素材名称 / 来源平台 | 许可证 (License) | 是否允许商用/改编 | 必须保留的署名文本 (Attribution) |
|---|---|---|---|:---:|:---:|---|
| `bgm_combat_master_64s_seamless.wav` | 局内主战斗无缝循环音乐 (64s) | **SRG774** | *Pulse* & *Airy* from [Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack) (OpenGameArt) | **CC0 1.0 (Public Domain)** | 是 / 是 | 推荐署名：“Music by SRG774 (OpenGameArt)”（CC0 免除强制署名，但予以保留致谢） |
| `bgm_levelup_suspended_loop.wav` | 升级暂停三选一悬浮长音 (16s) | **SRG774** | *Airy* from [Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack) (OpenGameArt) | **CC0 1.0 (Public Domain)** | 是 / 是 | 同上 |
| `bgm_combat_mathrock_postrock_32s_preview.wav` | S-02 短备选数学摇滚/后摇预览 (32s) | **SRG774** | *Pulse* from [Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack) (OpenGameArt) | **CC0 1.0 (Public Domain)** | 是 / 是 | 同上 |
| `sfx_player_shoot_01~03.wav` | 玩家武器开火瞬态 (3变体) | **spookymodem** & **Nicholas A. Judy** | 结合 [Bottle Break](https://opengameart.org/content/breaking-bottle) (OGA) 与 [The Designer's Choice UCS Collection: Wood/Metal](https://archive.org/download/Designers-Choice-Collection-Wood/) (Internet Archive) | **CC-BY 3.0** (spookymodem) / **CC0 1.0** (Judy) | 是 / 是 | 必须署名：“Glass transient based on Bottle Break by spookymodem (CC-BY 3.0); Physical impact by Nicholas A. Judy (TDC, CC0)” |
| `sfx_enemy_hit_01~03.wav` | 子弹击中敌人矿物碎裂 (3变体) | **Nicholas A. Judy** | *Small Stones, Kicked* from [The Designer's Choice UCS Collection: Rocks](https://archive.org/download/Designers-Choice-Collection-Rocks/) (Internet Archive) | **CC0 1.0 (Public Domain)** | 是 / 是 | “Foley samples from The Designer's Choice UCS Collection by Nicholas A. Judy (CC0)” |
| `sfx_enemy_death_01~03.wav` | 敌人核心碎裂坍塌 (3变体) | **spookymodem** & **Nicholas A. Judy** | [Bottle Break](https://opengameart.org/content/breaking-bottle) (OGA) + [TDC Wood Breaks](https://archive.org/download/Designers-Choice-Collection-Wood/) (IA) + 75Hz自研低频泄压 | **CC-BY 3.0** (spookymodem) / **CC0 1.0** (Judy) | 是 / 是 | “Structural shatter by spookymodem (CC-BY 3.0) and Nicholas A. Judy (CC0)” |
| `sfx_player_hurt_01~02.wav` | 玩家受到伤害次低频警报 (2变体) | **Nicholas A. Judy** & 音效总监自研 | *Metal, Clang, Thin 01* from [TDC Metal](https://archive.org/download/Designers-Choice-Collection-Metal/) (IA) + 62Hz/48Hz 纯物理次低频下潜 | **CC0 1.0 (Public Domain)** | 是 / 是 | “Metallic stress layers from The Designer's Choice UCS Collection by Nicholas A. Judy (CC0)” |
| `sfx_player_dodge_01~02.wav` | 玩家手动闪避切风 (2变体) | **Nicholas A. Judy** & **Spaceyyy** (Roberto Mellado) | *Swishes Big Low* from [TDC Swooshes](https://archive.org/download/Designers-Choice-Collection-Swooshes/) (IA) + *shield-up.wav* from [Cozy SFX Sampler](https://opengameart.org/content/cozy-sfx-sampler-%E2%80%94-60-cute-sound-effects-ui-farm-rpg-retro-spooky-footsteps) (OGA) | **CC0 1.0** (Judy) / **CC-BY 4.0** (Spaceyyy) | 是 / 是 | “Shield transient based on Cozy SFX Sampler by Spaceyyy / Roberto Mellado (aquumgifts.itch.io, CC-BY 4.0)” |
| `sfx_gem_pickup_01~05.wav` | 经验宝石连续拾取五声音阶 (5变体) | **Nicholas A. Judy** & 音效总监调谐 | *Small Tin Drop* from [TDC Metal](https://archive.org/download/Designers-Choice-Collection-Metal/) (IA) + C6/D6/E6/G6/A6 晶体谐波共振 | **CC0 1.0 (Public Domain)** | 是 / 是 | “Tin drop impact from The Designer's Choice UCS Collection by Nicholas A. Judy (CC0)” |
| `sfx_ui_levelup_appear.wav` | 升级卡牌展开悬浮提示 (1个) | **Spaceyyy** (Roberto Mellado) | *warp.wav* from [Cozy SFX Sampler](https://opengameart.org/content/cozy-sfx-sampler-%E2%80%94-60-cute-sound-effects-ui-farm-rpg-retro-spooky-footsteps) (OGA) + 空间泛音 | **CC-BY 4.0** | 是 / 是 | “Warp sound based on Cozy SFX Sampler by Spaceyyy / Roberto Mellado (aquumgifts.itch.io, CC-BY 4.0)” |
| `sfx_ui_levelup_confirm.wav` | 升级选择确定锁定 (1个) | **Nicholas A. Judy** | *Metal, Clang, Thin 01* from [TDC Metal](https://archive.org/download/Designers-Choice-Collection-Metal/) (IA) + D大调复合和弦 | **CC0 1.0 (Public Domain)** | 是 / 是 | “Metal clang from The Designer's Choice UCS Collection by Nicholas A. Judy (CC0)” |
| `sfx_game_victory.wav` | 3:00 终局胜利定格长鸣 (1个) | 音效总监自研设计 | 3.2s D大调纯净神圣共振雕塑和弦长鸣 | **CC0 / 自研** | 是 / 是 | 项目自研资产 |
| `sfx_game_defeat.wav` | 玩家死亡系统瓦解轰鸣 (1个) | **spookymodem** & **Nicholas A. Judy** | [Bottle Break](https://opengameart.org/content/breaking-bottle) + [TDC Wood Breaks](https://archive.org/download/Designers-Choice-Collection-Wood/) + 55Hz次低频消散 | **CC-BY 3.0** (spookymodem) / **CC0 1.0** (Judy) | 是 / 是 | “Shatter layers by spookymodem (CC-BY 3.0) and Nicholas A. Judy (CC0)” |

---

## 二、版权与署名合规落实建议 (致 2 号 / 最终发布负责人)

在游戏正式发布时的 `Credits`（演职人员名单）或工程的 `THIRD_PARTY_LICENSES.md` 中，以下为作者提供的署名草稿，需在来源与许可条款核对后完善，不能据此宣称全部要求已满足：

```text
Audio Assets & Foley Credits:
- Bottle Break audio samples by spookymodem (OpenGameArt.org), licensed under CC-BY 3.0.
- Cozy SFX Sampler (warp, shield) by Spaceyyy / Roberto Mellado (aquumgifts.itch.io), licensed under CC-BY 4.0.
- Foley and physical impacts from The Designer's Choice UCS Collection by Nicholas A. Judy, licensed under CC0 1.0 Universal (Public Domain).
- Ambient & Dark Sci-Fi music themes by SRG774 (OpenGameArt.org), licensed under CC0 1.0 Universal (Public Domain).
```
