using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct ProjectileJobInputData
        {
            public int EntityId;
            public int OwnerEntityId;
            public float3 Position;
            public float3 Forward;
            public float3 Velocity;
            public float Speed;
            public float LifeTime;
            public float Age;
            public bool Active;
            public float RemainingLifetime;
            public int State;
        }
    }
}