namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct CollisionCandidateData
        {
            public int QueryId;
            public int SourceType;
            public int SourceEntityId;
            public int SourceOwnerEntityId;
            public int TargetEntityId;
            public float SqrDistance;
        }
    }
}