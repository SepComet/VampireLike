using UnityEngine;

namespace Simulation
{
    public readonly struct SimulationTickContext
    {
        public SimulationTickContext(float deltaTime, float realDeltaTime, Vector3 playerPosition)
        {
            DeltaTime = deltaTime;
            RealDeltaTime = realDeltaTime;
            PlayerPosition = playerPosition;
        }

        public float DeltaTime { get; }
        public float RealDeltaTime { get; }
        public Vector3 PlayerPosition { get; }
    }
}
