using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle;
using Waffle.Combat;
using Waffle.Experiments.Mechanism.Presentation;

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
        public bool IsReady => bridge != null && !restarting;
        public MechanismMode Mode => bridge != null ? bridge.Rules.Mode : nextMode;
        public int EchoHits => bridge?.Rules.EchoHits ?? 0;
        public int PropagationHits => bridge?.Rules.PropagationHits ?? 0;
        public int PendingCount => bridge?.Rules.PendingCount ?? 0;
        public M01CombatBridge Bridge => bridge;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionConfig() { nextMode = MechanismMode.Combined; commandLineRead = false; }

        void Awake()
        {
            loop = GetComponent<GameLoop>(); fx = GetComponent<M01DerivedFx>();
            if (loop == null || loop.Sim?.Combat == null) { enabled = false; return; }
            if (!commandLineRead)
            {
                commandLineRead = true;
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                    if (args[i] == "--m01-mode" && Enum.TryParse(args[i + 1], out MechanismMode mode) && Enum.IsDefined(typeof(MechanismMode), mode)) nextMode = mode;
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
                { restarting = true; bridge.Enabled = false; bridge.Rules.Clear(); }
                else if (item.Type == PresentationEventType.EchoHit) fx?.PlayEcho(item.X, item.Z);
                else if (item.Type == PresentationEventType.PropagationHit || item.Type == PresentationEventType.EchoPropagationHit)
                    fx?.PlayPropagation(item.FromX, item.FromZ, item.X, item.Z);
            }
        }
        public void RestartWithMode(MechanismMode mode)
        {
            if (!IsReady || !Enum.IsDefined(typeof(MechanismMode), mode)) return;
            nextMode = mode;
            loop.Restart();
        }
    }
}
