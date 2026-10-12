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
    /// 游戏暂停会冻住声部和淡变。禁用组件或收到 Restart 会停声并清掉本局状态；新一局由新实例或 BeginFreshRun 从头开始。
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        enum MusicMode
        {
            Combat,
            LevelUp,
            Terminal
        }

        public GameLoop loop;
        public bool enableMechanismAudio;
        public AudioEventRule echo = new AudioEventRule("M01_Echo", -3.5f, 0f, 2, 0.06f, 0f, 25f);
        public AudioEventRule propagation = new AudioEventRule("M01_Propagation", -4.5f, 0f, 2, 0.05f, 0f, 25f);
        public AudioEventRule echoPropagation = new AudioEventRule("M01_EchoPropagation", -5f, 0f, 1, 0.06f, 0f, 25f);

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

        private float combatGain, combatTarget, combatRate;
        private float levelUpGain, levelUpTarget, levelUpRate;
        private float lowpassTarget = 22000f, lowpassRate;

        private float duckAmount;
        private float duckHoldUntil = float.NegativeInfinity;
        private bool ducking;

        private float lastPickupTime = float.NegativeInfinity;
        private bool paused;
        private bool combatHeld;
        private bool started;
        private bool accepting = true;
        private bool terminalBedStopped;
        private float pauseStarted;
        private float pausedSeconds;
        private MusicMode musicMode = MusicMode.Combat;
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
            if (enableMechanismAudio)
            {
                BuildChannel(PresentationEventType.EchoHit, echo);
                BuildChannel(PresentationEventType.PropagationHit, propagation);
                BuildChannel(PresentationEventType.EchoPropagationHit, echoPropagation);
            }

            combatSource = CreateSource("Music_Combat", musicGroup, true);
            combatSource.clip = combatLoop;
            combatLowpass = combatSource.gameObject.AddComponent<AudioLowPassFilter>();
            combatLowpass.cutoffFrequency = 22000f;
            levelUpSource = CreateSource("Music_LevelUp", musicGroup, true);
            levelUpSource.clip = levelUpLoop;
        }

        private void Start()
        {
            started = true;
            BeginFreshRun();
        }

        private void OnEnable()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null && subscribed == null)
            {
                loop.PresentationDispatched += OnEvents;
                subscribed = loop;
            }
            // 禁用后再启用视为新的一局，不恢复禁用前的淡化、压低或拾取音阶。
            // 第一次启用发生在 Start 之前，由 Start 开局，避免播两次。
            if (started) BeginFreshRun();
        }

        private void OnDisable()
        {
            if (subscribed != null) subscribed.PresentationDispatched -= OnEvents;
            subscribed = null;
            Silence();
        }

        /// <summary>
        /// 把音乐和音效回到战斗开局。场景重载后的新实例会在 Start 里调用。
        /// 同一对象被禁用后再启用也会调用。不读取上一局的暂停、压低或升级淡化。
        /// </summary>
        public void BeginFreshRun()
        {
            accepting = true;
            paused = false;
            combatHeld = false;
            pauseStarted = 0f;
            pausedSeconds = 0f;
            terminalBedStopped = false;
            musicMode = MusicMode.Combat;
            duckAmount = 0f;
            ducking = false;
            duckHoldUntil = float.NegativeInfinity;
            lastPickupTime = float.NegativeInfinity;
            ResetChannels();
            StopSources();

            combatGain = combatTarget = 1f;
            combatRate = 0f;
            levelUpGain = levelUpTarget = 0f;
            levelUpRate = 0f;
            lowpassTarget = 22000f;
            lowpassRate = 0f;
            if (combatLowpass != null) combatLowpass.cutoffFrequency = 22000f;

            // Stop 之后再 UnPause：有的 Unity 版本里 Pause 标记在 Stop 之后还留着，Play 不会出声。
            RestartLoop(combatSource, combatLoop);
            RestartLoop(levelUpSource, levelUpLoop);
            AlignWithRunPhase();
            ApplyMusicVolumes();
        }

        private void AlignWithRunPhase()
        {
            // A component restart is not a gameplay restart. Events may have fired
            // before Start, or while this component was disabled: read the authority.
            if (loop == null || loop.Sim == null) return;
            var state = loop.Sim.State;
            RunPhase phase = state.Phase == RunPhase.Paused ? state.PhaseBeforePause : state.Phase;
            if (phase == RunPhase.LevelUp)
            {
                musicMode = MusicMode.LevelUp;
                combatGain = combatTarget = 0f;
                levelUpGain = levelUpTarget = DbToLinear(levelUpLoopDb);
                PauseIfPlaying(combatSource);
                combatHeld = true;
            }
            else if (phase == RunPhase.Victory || phase == RunPhase.Defeat)
            {
                musicMode = MusicMode.Terminal;
                combatGain = combatTarget = 0f;
                levelUpGain = levelUpTarget = 0f;
                StopSources();
                terminalBedStopped = true;
            }
            if (state.Phase == RunPhase.Paused) SetPaused(true);
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

        private void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            for (int i = 0; i < events.Count; i++) Handle(events[i]);
        }

        private void Handle(PresentationEvent e)
        {
            if (!accepting) return;
            switch (e.Type)
            {
                case PresentationEventType.Paused:
                    SetPaused(true);
                    return;
                case PresentationEventType.Resumed:
                    SetPaused(false);
                    return;
                case PresentationEventType.Restart:
                    // 场景即将重载。这里只停声，不在即将销毁的对象上再起一局。
                    Silence();
                    return;
                case PresentationEventType.LevelUpShown:
                    // 终局之后不再把音乐拉回升级。连续升级的后续面板只播提示音。
                    if (musicMode == MusicMode.Terminal) break;
                    if (musicMode != MusicMode.LevelUp) EnterLevelUp();
                    break;
                case PresentationEventType.LevelUpChosen:
                    // GameLoop 在 ChooseUpgrade 改完 PendingLevelUps 之后才分发。
                    // 还有后续面板时不要把战斗音乐淡回来。
                    if (musicMode != MusicMode.Terminal && IsLastLevelUpChoice()) LeaveLevelUp();
                    break;
                case PresentationEventType.Victory:
                    EnterTerminal(victoryFadeOut, false);
                    break;
                case PresentationEventType.Defeat:
                    EnterTerminal(defeatCut, true);
                    break;
                case PresentationEventType.PlayerHurt:
                    StartDuck();
                    break;
            }

            Channel ch;
            if (channels.TryGetValue(e.Type, out ch)) Play(ch, e);
        }

        private bool IsLastLevelUpChoice()
        {
            return loop != null && loop.Sim != null && loop.Sim.State.PendingLevelUps == 0;
        }

        private void Play(Channel ch, PresentationEvent e)
        {
            AudioEventRule rule = ch.rule;
            if (rule.clips == null || rule.clips.Length == 0 || paused) return;
            float now = AudioClock();
            if (now - ch.lastPlayTime < rule.minInterval) return;

            int clipIndex;
            if (e.Type == PresentationEventType.GemPickup)
            {
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

            Voice voice = null;
            for (int i = 0; i < ch.voices.Count; i++)
            {
                if (!ch.voices[i].source.isPlaying) { voice = ch.voices[i]; break; }
                if (voice == null || ch.voices[i].startTime < voice.startTime) voice = ch.voices[i];
            }

            AudioSource src = voice.source;
            if (rule.dropWhenFull && src.isPlaying) return;
            src.Stop();
            src.clip = clip;
            src.volume = DbToLinear(sfxTrimDb + rule.volumeDb);
            src.pitch = rule.pitch + (rule.pitchRandom > 0f ? Random.Range(-rule.pitchRandom, rule.pitchRandom) : 0f);
            if (rule.spatialBlend > 0f) src.transform.position = new Vector3(e.X, 0f, e.Z);
            src.Play();
            voice.startTime = now;
            ch.lastPlayTime = now;
        }

        /// <summary>暂停：声部和音乐淡变一起冻住。恢复时不补播暂停期间被丢掉的一次性音效。</summary>
        private void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (value)
            {
                pauseStarted = Time.unscaledTime;
                PauseIfPlaying(allSfx);
                PauseIfPlaying(combatSource);
                PauseIfPlaying(levelUpSource);
                return;
            }

            pausedSeconds += Time.unscaledTime - pauseStarted;

            for (int i = 0; i < allSfx.Count; i++)
            {
                if (allSfx[i] != null) allSfx[i].UnPause();
            }
            // 升级循环在战斗中音量是 0，但仍要保持播放，交叉淡化才听得见。
            // 只在音量大于 0 时解开的话，战斗中暂停一次之后，下次升级没有悬浮层。
            if (musicMode != MusicMode.Terminal)
            {
                if (!combatHeld) UnPauseIfExists(combatSource);
                UnPauseIfExists(levelUpSource);
            }
            else
            {
                if (combatGain > 0.001f || combatTarget > 0.001f) UnPauseIfExists(combatSource);
                if (levelUpGain > 0.001f || levelUpTarget > 0.001f) UnPauseIfExists(levelUpSource);
            }
        }

        private static void PauseIfPlaying(List<AudioSource> sources)
        {
            for (int i = 0; i < sources.Count; i++) PauseIfPlaying(sources[i]);
        }

        private static void PauseIfPlaying(AudioSource source)
        {
            if (source != null && source.isPlaying) source.Pause();
        }

        private static void UnPauseIfExists(AudioSource source)
        {
            if (source != null) source.UnPause();
        }

        private void EnterLevelUp()
        {
            musicMode = MusicMode.LevelUp;
            combatHeld = false;
            // 暂停过的升级层只 UnPause，不从 0 重播，避免接缝。
            UnPauseIfExists(levelUpSource);
            if (levelUpSource != null && !levelUpSource.isPlaying && levelUpLoop != null)
            {
                levelUpSource.clip = levelUpLoop;
                levelUpSource.Play();
            }
            FadeCombat(0f, levelUpCrossfade);
            FadeLevelUp(DbToLinear(levelUpLoopDb), levelUpCrossfade);
        }

        private void LeaveLevelUp()
        {
            musicMode = MusicMode.Combat;
            combatHeld = false;
            if (!paused && combatSource != null && !combatSource.isPlaying)
            {
                // Pause 过的源用 UnPause 接着播；从没播过才从头 Play。
                combatSource.UnPause();
                if (!combatSource.isPlaying && combatLoop != null) combatSource.Play();
            }
            FadeCombat(1f, resumeFadeIn);
            FadeLevelUp(0f, resumeFadeIn);
        }

        private void EnterTerminal(float seconds, bool lowpass)
        {
            musicMode = MusicMode.Terminal;
            combatHeld = false;
            terminalBedStopped = false;
            FadeCombat(0f, seconds);
            FadeLevelUp(0f, seconds);
            if (!lowpass || combatLowpass == null) return;
            lowpassTarget = defeatLowpassHz;
            float from = combatLowpass.cutoffFrequency;
            lowpassRate = Mathf.Abs(from - defeatLowpassHz) / Mathf.Max(0.01f, seconds);
        }

        /// <summary>停掉所有声部并拒绝后续事件，直到 BeginFreshRun。不启动新的音乐。</summary>
        private void Silence()
        {
            accepting = false;
            paused = false;
            combatHeld = false;
            pauseStarted = 0f;
            pausedSeconds = 0f;
            terminalBedStopped = true;
            musicMode = MusicMode.Terminal;
            duckAmount = 0f;
            ducking = false;
            duckHoldUntil = float.NegativeInfinity;
            lastPickupTime = float.NegativeInfinity;
            combatTarget = combatGain = 0f;
            levelUpTarget = levelUpGain = 0f;
            combatRate = 0f;
            levelUpRate = 0f;
            ResetChannels();
            StopSources();
            ApplyMusicVolumes();
        }

        private void ResetChannels()
        {
            foreach (var ch in channels.Values)
            {
                ch.lastPlayTime = float.NegativeInfinity;
                ch.nextIndex = 0;
            }
        }

        private void StopSources()
        {
            for (int i = 0; i < allSfx.Count; i++)
            {
                if (allSfx[i] != null) allSfx[i].Stop();
            }
            if (combatSource != null) combatSource.Stop();
            if (levelUpSource != null) levelUpSource.Stop();
        }

        private static void RestartLoop(AudioSource source, AudioClip clip)
        {
            if (source == null) return;
            source.Stop();
            source.UnPause();
            if (clip == null) return;
            source.clip = clip;
            source.time = 0f;
            source.Play();
        }

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
            if (paused || musicMode == MusicMode.Terminal) return;
            ducking = true;
            duckHoldUntil = AudioClock() + duckAttack + duckHold;
        }

        /// <summary>暂停期间不走。用于音效间隔、拾取音阶和压低保持，避免暂停把这些计时提前走完。</summary>
        private float AudioClock()
        {
            float t = Time.unscaledTime - pausedSeconds;
            if (paused) t -= Time.unscaledTime - pauseStarted;
            return t;
        }

        private void Update()
        {
            if (paused || !accepting) return;
            float dt = Time.unscaledDeltaTime;

            combatGain = Mathf.MoveTowards(combatGain, combatTarget, combatRate * dt);
            levelUpGain = Mathf.MoveTowards(levelUpGain, levelUpTarget, levelUpRate * dt);
            if (combatLowpass != null)
                combatLowpass.cutoffFrequency = Mathf.MoveTowards(combatLowpass.cutoffFrequency, lowpassTarget, lowpassRate * dt);

            if (musicMode == MusicMode.LevelUp && combatGain <= 0.001f && combatSource != null && combatSource.isPlaying)
            {
                // 淡出完成后停住播放头，恢复时从离开的位置继续，而不是在面板期间把循环悄悄走完。
                combatSource.Pause();
                combatHeld = true;
            }
            if (musicMode == MusicMode.Terminal && !terminalBedStopped
                && combatGain <= 0.001f && levelUpGain <= 0.001f)
            {
                // 已经 Pause 的声源 isPlaying 为 false，也要 Stop 一次，清掉暂停标记。
                if (combatSource != null) combatSource.Stop();
                if (levelUpSource != null) levelUpSource.Stop();
                terminalBedStopped = true;
            }

            if (ducking)
            {
                duckAmount = Mathf.MoveTowards(duckAmount, 1f, dt / Mathf.Max(0.001f, duckAttack));
                if (AudioClock() >= duckHoldUntil) ducking = false;
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
