namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct AreaCollisionHitEventData
        {
            public int QueryId;
            public int SourceEntityId;
            public int SourceOwnerEntityId;
            public int TargetEntityId;
            public float SqrDistance;
        }
    }
}