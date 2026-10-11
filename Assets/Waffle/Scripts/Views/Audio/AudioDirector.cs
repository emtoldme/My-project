using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Waffle
{
    /// <summary>
    /// S1 音频入口（唯一）。只读取 GameLoop 分发的表现事件和当前状态，不改玩法。
    /// 实现 Docs/Audio 的事件表：音量 dB、最大并发（挤掉最早）、最短间隔、2D/3D、拾取音阶、受击压低音乐、
    /// 音乐状态机（战斗循环 / 升级悬浮循环 / 胜利 / 失败）。
    /// 没有 .mixer 资源时，用 AudioSource 音量 + AudioLowPassFilter 在脚本里完成混音；
    /// 如果之后在 Unity 里建了 AudioMixer，可以把组拖到 musicGroup / sfxGroup，脚本内的音量计算照常生效。
    /// 所有淡入淡出用 unscaled 时间：升级面板期间 timeScale = 0，音乐仍要能交叉淡化。
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public GameLoop loop;

        [Header("混音（没有 AudioMixer 时由脚本完成）")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        [Tooltip("整体音效预留余量（dB）。事件表里有 +0.5 / +1 dB，AudioSource 音量上限是 1，所以整体先降 3 dB")]
        public float sfxTrimDb = -3f;
        public float musicDb = 0f;

        [Header("音乐")]
        public AudioClip combatLoop;
        public AudioClip levelUpLoop;
        public float levelUpLoopDb = -2f;
        public float levelUpCrossfade = 1.0f;
        public float resumeFadeIn = 1.4f;
        public float victoryFadeOut = 2.0f;
        public float defeatCut = 0.3f;
        public float defeatLowpassHz = 200f;

        [Header("受击压低音乐（-8 dB，10/200/600 ms）")]
        public float duckDb = -8f;
        public float duckAttack = 0.01f;
        public float duckHold = 0.2f;
        public float duckRelease = 0.6f;

        [Header("拾取音阶：超过这个时间没拾取就回到第 1 个音")]
        public float pickupStreakReset = 1.0f;

        [Header("音效事件（默认值来自事件表）")]
        public AudioEventRule shoot = new AudioEventRule("Event_Player_Shoot", -2f, 0.05f, 3, 0.08f, 0f, 25f);
        public AudioEventRule enemyHit = new AudioEventRule("Event_Enemy_Hit", -3f, 0.08f, 4, 0.04f, 0.4f, 25f);
        public AudioEventRule enemyDeath = new AudioEventRule("Event_Enemy_Death", -1.5f, 0.04f, 3, 0.06f, 1f, 20f);
        public AudioEventRule playerHurt = new AudioEventRule("Event_Player_Hurt", 0.5f, 0f, 1, 0.15f, 0f, 25f, true);
        public AudioEventRule dodge = new AudioEventRule("Event_Player_Dodge", -1f, 0.03f, 1, 0.2f, 0f, 25f);
        public AudioEventRule gemPickup = new AudioEventRule("Event_Gem_Pickup", -2.5f, 0f, 5, 0.03f, 0f, 25f, true);
        public AudioEventRule levelUpAppear = new AudioEventRule("Event_LevelUp_Appear", 0f, 0f, 1, 0f, 0f, 25f);
        public AudioEventRule levelUpConfirm = new AudioEventRule("Event_LevelUp_Confirm", 1f, 0f, 1, 0f, 0f, 25f);
        public AudioEventRule victory = new AudioEventRule("Event_Game_Victory", 1f, 0f, 1, 0f, 0f, 25f);
        public AudioEventRule defeat = new AudioEventRule("Event_Game_Defeat", 1f, 0f, 1, 0f, 0f, 25f);

        // ---- 运行时 ----
        private class Voice
        {
            public AudioSource source;
            public float startTime;
        }

        private class Channel
        {
            public AudioEventRule rule;
            public readonly List<Voice> voices = new List<Voice>();
            public float lastPlayTime = float.NegativeInfinity;
            public int nextIndex;
        }

        private readonly Dictionary<PresentationEventType, Channel> channels = new Dictionary<PresentationEventType, Channel>();
        private readonly List<AudioSource> allSfx = new List<AudioSource>();
        private AudioSource combatSource, levelUpSource;
        private AudioLowPassFilter combatLowpass;

        // 音乐目标音量（线性）与淡变速度
        private float combatGain, combatTarget, combatRate;
        private float levelUpGain, levelUpTarget, levelUpRate;
        private float lowpassTarget = 22000f, lowpassRate;

        // 受击压低：0 = 不压，1 = 压满
        private float duckAmount;
        private float duckHoldUntil = float.NegativeInfinity;
        private bool ducking;

        private float lastPickupTime = float.NegativeInfinity;
        private bool paused;
        private GameLoop subscribed;

        public static float DbToLinear(float db) { return db <= -80f ? 0f : Mathf.Pow(10f, db / 20f); }

        private void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            BuildChannel(PresentationEventType.Shot, shoot);
            BuildChannel(PresentationEventType.EnemyHit, enemyHit);
            BuildChannel(PresentationEventType.EnemyKilled, enemyDeath);
            BuildChannel(PresentationEventType.PlayerHurt, playerHurt);
            BuildChannel(PresentationEventType.Dodge, dodge);
            BuildChannel(PresentationEventType.GemPickup, gemPickup);
            BuildChannel(PresentationEventType.LevelUpShown, levelUpAppear);
            BuildChannel(PresentationEventType.LevelUpChosen, levelUpConfirm);
            BuildChannel(PresentationEventType.Victory, victory);
            BuildChannel(PresentationEventType.Defeat, defeat);

            combatSource = CreateSource("Music_Combat", musicGroup, true);
            combatSource.clip = combatLoop;
            combatLowpass = combatSource.gameObject.AddComponent<AudioLowPassFilter>();
            combatLowpass.cutoffFrequency = 22000f;
            levelUpSource = CreateSource("Music_LevelUp", musicGroup, true);
            levelUpSource.clip = levelUpLoop;
        }

        private void Start()
        {
            // 新一局（场景加载后）：战斗音乐从头开始
            combatGain = combatTarget = 1f;
            levelUpGain = levelUpTarget = 0f;
            if (combatLoop != null) combatSource.Play();
            if (levelUpLoop != null) levelUpSource.Play(); // 一直播放，音量 0，切换时只做交叉淡化
            ApplyMusicVolumes();
        }

        private void OnEnable()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null) { loop.PresentationDispatched += OnEvents; subscribed = loop; }
        }

        private void OnDisable()
        {
            if (subscribed != null) subscribed.PresentationDispatched -= OnEvents;
            subscribed = null;
        }

        private void BuildChannel(PresentationEventType type, AudioEventRule rule)
        {
            var ch = new Channel { rule = rule };
            int n = Mathf.Max(1, rule.maxVoices);
            for (int i = 0; i < n; i++)
            {
                var src = CreateSource("SFX_" + rule.name + "_" + i, sfxGroup, false);
                src.spatialBlend = rule.spatialBlend;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.maxDistance = rule.maxDistance;
                ch.voices.Add(new Voice { source = src, startTime = float.NegativeInfinity });
                allSfx.Add(src);
            }
            channels[type] = ch;
        }

        private AudioSource CreateSource(string name, AudioMixerGroup group, bool loopClip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loopClip;
            src.outputAudioMixerGroup = group;
            return src;
        }

        // =====================================================================
        // 事件
        // =====================================================================

        private void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            for (int i = 0; i < events.Count; i++) Handle(events[i]);
        }

        private void Handle(PresentationEvent e)
        {
            switch (e.Type)
            {
                case PresentationEventType.Paused:
                    SetPaused(true);
                    return;
                case PresentationEventType.Resumed:
                    SetPaused(false);
                    return;
                case PresentationEventType.Restart:
                    StopAll();
                    return;
                case PresentationEventType.LevelUpShown:
                    // 第一张面板：战斗音乐 1 s 淡出，悬浮循环 1 s 淡入；连续升级的后续面板只播提示音
                    FadeCombat(0f, levelUpCrossfade);
                    FadeLevelUp(DbToLinear(levelUpLoopDb), levelUpCrossfade);
                    break;
                case PresentationEventType.LevelUpChosen:
                    if (loop != null && loop.Sim != null && loop.Sim.State.PendingLevelUps == 0)
                    {
                        FadeCombat(1f, resumeFadeIn);
                        FadeLevelUp(0f, resumeFadeIn);
                    }
                    break;
                case PresentationEventType.Victory:
                    FadeCombat(0f, victoryFadeOut);
                    FadeLevelUp(0f, victoryFadeOut);
                    break;
                case PresentationEventType.Defeat:
                    FadeCombat(0f, defeatCut);
                    FadeLevelUp(0f, defeatCut);
                    lowpassTarget = defeatLowpassHz;
                    lowpassRate = (22000f - defeatLowpassHz) / Mathf.Max(0.01f, defeatCut);
                    break;
                case PresentationEventType.PlayerHurt:
                    StartDuck();
                    break;
            }

            Channel ch;
            if (channels.TryGetValue(e.Type, out ch)) Play(ch, e);
        }

        private void Play(Channel ch, PresentationEvent e)
        {
            AudioEventRule rule = ch.rule;
            if (rule.clips == null || rule.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (now - ch.lastPlayTime < rule.minInterval) return;

            // 选音色
            int clipIndex;
            if (e.Type == PresentationEventType.GemPickup)
            {
                // 连续拾取按 01→05 递增，超过 1 s 没拾取就回到 01
                if (now - lastPickupTime > pickupStreakReset) ch.nextIndex = 0;
                lastPickupTime = now;
                clipIndex = ch.nextIndex % rule.clips.Length;
                ch.nextIndex++;
            }
            else if (rule.sequential)
            {
                clipIndex = ch.nextIndex % rule.clips.Length;
                ch.nextIndex++;
            }
            else
            {
                clipIndex = Random.Range(0, rule.clips.Length);
            }
            AudioClip clip = rule.clips[clipIndex];
            if (clip == null) return;

            // 选声部：优先空闲，否则挤掉最早开始的
            Voice voice = null;
            for (int i = 0; i < ch.voices.Count; i++)
            {
                if (!ch.voices[i].source.isPlaying) { voice = ch.voices[i]; break; }
                if (voice == null || ch.voices[i].startTime < voice.startTime) voice = ch.voices[i];
            }

            AudioSource src = voice.source;
            src.Stop();
            src.clip = clip;
            src.volume = DbToLinear(sfxTrimDb + rule.volumeDb);
            src.pitch = 1f + (rule.pitchRandom > 0f ? Random.Range(-rule.pitchRandom, rule.pitchRandom) : 0f);
            if (rule.spatialBlend > 0f) src.transform.position = new Vector3(e.X, 0f, e.Z);
            src.Play();
            voice.startTime = now;
            ch.lastPlayTime = now;
        }

        // =====================================================================
        // 暂停 / 重开
        // =====================================================================

        /// <summary>暂停：所有音效与音乐一起暂停（音频规范没有单独规定暂停，这里按“完全冻结”处理）。</summary>
        private void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            for (int i = 0; i < allSfx.Count; i++)
            {
                if (value) allSfx[i].Pause(); else allSfx[i].UnPause();
            }
            if (value) { combatSource.Pause(); levelUpSource.Pause(); }
            else { combatSource.UnPause(); levelUpSource.UnPause(); }
        }

        /// <summary>重开：停止并清空所有声部（场景随后重新加载，本对象也会销毁）。</summary>
        private void StopAll()
        {
            for (int i = 0; i < allSfx.Count; i++) allSfx[i].Stop();
            combatSource.Stop();
            levelUpSource.Stop();
            foreach (var ch in channels.Values) { ch.lastPlayTime = float.NegativeInfinity; ch.nextIndex = 0; }
            duckAmount = 0f;
            ducking = false;
        }

        // =====================================================================
        // 音乐淡变与压低
        // =====================================================================

        private void FadeCombat(float target, float seconds)
        {
            combatTarget = target;
            combatRate = Mathf.Abs(target - combatGain) / Mathf.Max(0.001f, seconds);
        }

        private void FadeLevelUp(float target, float seconds)
        {
            levelUpTarget = target;
            levelUpRate = Mathf.Abs(target - levelUpGain) / Mathf.Max(0.001f, seconds);
        }

        private void StartDuck()
        {
            ducking = true;
            duckHoldUntil = Time.unscaledTime + duckAttack + duckHold;
        }

        private void Update()
        {
            if (paused) return;
            float dt = Time.unscaledDeltaTime;

            combatGain = Mathf.MoveTowards(combatGain, combatTarget, combatRate * dt);
            levelUpGain = Mathf.MoveTowards(levelUpGain, levelUpTarget, levelUpRate * dt);
            if (combatLowpass != null && combatLowpass.cutoffFrequency > lowpassTarget)
                combatLowpass.cutoffFrequency = Mathf.MoveTowards(combatLowpass.cutoffFrequency, lowpassTarget, lowpassRate * dt);

            // 压低包络：Attack 10 ms 压到 -8 dB，保持 200 ms，600 ms 回弹
            if (ducking)
            {
                duckAmount = Mathf.MoveTowards(duckAmount, 1f, dt / Mathf.Max(0.001f, duckAttack));
                if (Time.unscaledTime >= duckHoldUntil) ducking = false;
            }
            else
            {
                duckAmount = Mathf.MoveTowards(duckAmount, 0f, dt / Mathf.Max(0.001f, duckRelease));
            }

            ApplyMusicVolumes();
        }

        private void ApplyMusicVolumes()
        {
            float music = DbToLinear(musicDb + duckDb * duckAmount);
            if (combatSource != null) combatSource.volume = combatGain * music;
            if (levelUpSource != null) levelUpSource.volume = levelUpGain * music;
        }
    }
}
