using UnityEngine;

namespace Simulation
{
    public struct ProjectileSimData
    {
        public int EntityId;
        public int OwnerEntityId;
        public Vector3 Position;
        public Vector3 Forward;
        public float Speed;
        public float RemainingLifetime;
        public int State;
    }
}
