using UnityEngine;

namespace Simulation
{
    public struct ProjectileSimData
    {
        public int EntityId;
        public int OwnerEntityId;
        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 Velocity;
        public float Speed;
        public float LifeTime;
        public float Age;
        public bool Active;
        public float RemainingLifetime;
        public int State;
    }
}
