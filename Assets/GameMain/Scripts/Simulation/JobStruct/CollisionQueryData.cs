using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct CollisionQueryData
        {
            public int QueryId;
            public int SourceType;
            public int SourceEntityId;
            public int SourceOwnerEntityId;
            public bool SourceWasActiveAtQueryTime;
            public float3 Position;
            public float Radius;
            public int MaxTargets;
            public int ShapeType;
            public float3 Direction;
            public float HalfAngleDeg;
            public float HalfWidth;
            public float HalfLength;
        }
    }
}
