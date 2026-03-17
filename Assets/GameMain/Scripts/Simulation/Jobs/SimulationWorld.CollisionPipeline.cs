namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Shared collision pipeline configuration and runtime state.
        // Request buffering, broad-phase scheduling, resolve, and presentation
        // dispatch live in dedicated partial files under Jobs/.
        private const int PlayerEntityId = -1;
    }
}
