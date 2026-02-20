using UnityEngine;

namespace Simulation
{
    public struct EnemySimData
    {
        public int EntityId;
        public Vector3 Position;
        public Vector3 Forward;
        public float Speed;
        public float AttackRange;
        public int TargetType;
        public int State;
    }
}
