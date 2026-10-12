using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle;
using Waffle.Combat;
using Waffle.Experiments.Mechanism.Presentation;
using Reverb.Experiments.Mechanism.Rules;

namespace Reverb.Experiments.Mechanism.Integration
{
    [DefaultExecutionOrder(-90)]
    public sealed class M01PlaytestController : MonoBehaviour, IMechanismPlaytestControls
    {
        // Only next-run configuration persists across scene reload. No run state is static.
        static MechanismMode nextMode = MechanismMode.Combined;
        static bool commandLineRead;
        GameLoop loop;
        M01DerivedFx fx;
        M01CombatBridge bridge;
        bool restarting;
        bool comparisonAllowed;
        bool terminal;
        int terminalEchoHits, terminalPropagationHits;
        readonly List<PendingEchoInfo> pendingEchoes = new List<PendingEchoInfo>(16);
        readonly List<M01EchoCue> echoCues = new List<M01EchoCue>(8);
        readonly HashSet<MechanismTarget> marked = new HashSet<MechanismTarget>();
        public bool IsReady => bridge != null && !restarting;
        public MechanismMode Mode => bridge != null ? bridge.Rules.Mode : nextMode;
        public int EchoHits => terminal ? terminalEchoHits : bridge?.Rules.EchoHits ?? 0;
        public int PropagationHits => terminal ? terminalPropagationHits : bridge?.Rules.PropagationHits ?? 0;
        public int PendingCount => bridge?.Rules.PendingCount ?? 0;
        public M01CombatBridge Bridge => bridge;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionConfig() { nextMode = MechanismMode.Combined; commandLineRead = false; }

        void Awake()
        {
            loop = GetComponent<GameLoop>(); fx = GetComponent<M01DerivedFx>();
            if (loop == null || loop.Sim?.Combat == null) { enabled = false; return; }
            string[] args = Environment.GetCommandLineArgs();
            comparisonAllowed = Application.isEditor || Array.IndexOf(args, "--m01-smoke-output") >= 0;
            if (!comparisonAllowed) nextMode = MechanismMode.Combined;
            if (!commandLineRead)
            {
                commandLineRead = true;
                for (int i = 0; i + 1 < args.Length; i++)
                    if (comparisonAllowed && args[i] == "--m01-mode" && Enum.TryParse(args[i + 1], out MechanismMode mode) && Enum.IsDefined(typeof(MechanismMode), mode)) nextMode = mode;
            }
            bridge = new M01CombatBridge(loop.Sim.Combat, nextMode);
            // Identical close initial cluster exposes the mechanism immediately in every mode.
            loop.Sim.Combat.DebugSpawnEnemy(new Vec2(3f, 0f), 60, 0.8f);
            loop.Sim.Combat.DebugSpawnEnemy(new Vec2(5f, 1.5f), 60, 0.8f);
            loop.Sim.Combat.DebugSpawnEnemy(new Vec2(5f, -1.5f), 60, 0.8f);
        }
        void OnEnable()
        {
            if (bridge != null) bridge.Enabled = true;
            if (loop != null) loop.PresentationDispatched += OnEvents;
        }
        void OnDisable()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
            if (bridge != null) { bridge.Enabled = false; bridge.Rules.Clear(); }
            if (fx != null) fx.Clear();
        }
        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            foreach (var item in events)
            {
                if (item.Type == PresentationEventType.Restart)
                { restarting = true; bridge.Enabled = false; bridge.Rules.Clear(); fx?.Clear(); }
                else if (item.Type == PresentationEventType.Victory || item.Type == PresentationEventType.Defeat)
                { terminalEchoHits = EchoHits; terminalPropagationHits = PropagationHits;
                    terminal = true; bridge.Enabled = false; bridge.Rules.Clear(); fx?.Clear(); }
                else if (item.Type == PresentationEventType.EchoHit) fx?.PlayEcho(item.X, item.Z, item.Value);
                else if (item.Type == PresentationEventType.PropagationHit || item.Type == PresentationEventType.EchoPropagationHit)
                    fx?.PlayPropagation(item.FromX, item.FromZ, item.X, item.Z, item.Value);
            }
        }
        void LateUpdate()
        {
            echoCues.Clear(); marked.Clear();
            if (bridge != null && !restarting && !terminal)
            {
                bridge.Rules.CopyPendingEchoes(pendingEchoes);
                pendingEchoes.Sort(CompareDue);
                double now = loop.Sim.State.Now;
                foreach (var pending in pendingEchoes)
                {
                    if (!marked.Add(pending.Target) || !bridge.TryGetLiveTarget(pending.Target, out var position)) continue;
                    double span = pending.DueAt - pending.ScheduledAt;
                    float progress = span <= 0 ? 1f : Mathf.Clamp01((float)((now - pending.ScheduledAt) / span));
                    echoCues.Add(new M01EchoCue(pending.Target.Serial, pending.Target.Generation, position.X, position.Z, progress));
                }
            }
            // Empty snapshots withdraw stale targets, including terminal frames.
            fx?.SyncEchoCues(echoCues);
        }
        static int CompareDue(PendingEchoInfo a, PendingEchoInfo b)
        {
            int due = a.DueAt.CompareTo(b.DueAt);
            if (due != 0) return due;
            int serial = a.Target.Serial.CompareTo(b.Target.Serial);
            return serial != 0 ? serial : a.Target.Generation.CompareTo(b.Target.Generation);
        }
        public void RestartWithMode(MechanismMode mode)
        {
            if (!IsReady || !Enum.IsDefined(typeof(MechanismMode), mode)) return;
            nextMode = comparisonAllowed ? mode : MechanismMode.Combined;
            loop.Restart();
        }
    }
}
