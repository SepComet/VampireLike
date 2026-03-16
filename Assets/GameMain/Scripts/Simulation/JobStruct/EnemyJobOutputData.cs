using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct EnemyJobOutputData
        {
            public int EntityId;
            public float3 Position;
            public float3 Forward;
            public quaternion Rotation;
            public float Speed;
            public float AttackRange;
            public bool AvoidEnemyOverlap;
            public float EnemyBodyRadius;
            public int SeparationIterations;
            public int TargetType;
            public int State;
        }
    }
}