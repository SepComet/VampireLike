using UnityEngine;

namespace CustomUtility
{
    public struct EnemySeparationAgent
    {
        public int AgentId;
        public Vector3 Position;
        public float Radius;
    }

    public interface IEnemySeparationSolver
    {
        void SetAgents(System.Collections.Generic.IReadOnlyList<EnemySeparationAgent> agents);
        Vector3 Resolve(int agentId, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations);
    }
}
