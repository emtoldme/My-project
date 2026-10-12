namespace Reverb.Experiments.Mechanism
{
    // TEST_ONLY shared contract. UI must not write HP, advance clocks, or resolve damage.
    public enum MechanismMode { None = 0, Echo = 1, Propagation = 2, Combined = 3 }

    public interface IMechanismPlaytestControls
    {
        // False until Codex connects the one authoritative combat adapter.
        bool IsReady { get; }
        MechanismMode Mode { get; }
        int EchoHits { get; }
        int PropagationHits { get; }
        int PendingCount { get; }

        // Always starts a clean run using the same seed, even if the mode is unchanged.
        // Counts refer to applied derived damage, excluding stale/dead skipped targets.
        void RestartWithMode(MechanismMode mode);
    }
}
