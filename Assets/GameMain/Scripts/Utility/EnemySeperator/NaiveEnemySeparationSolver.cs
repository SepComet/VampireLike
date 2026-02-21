using UnityEngine;

namespace CustomUtility
{
    public sealed class NaiveEnemySeparationSolver : IEnemySeparationSolver
    {
        private struct Agent
        {
            public float Radius;
            public Vector3 Position;
        }

        private readonly System.Collections.Generic.Dictionary<int, Agent> _agents = new();
        private readonly System.Collections.Generic.List<int> _agentKeys = new();

        public void SetAgents(System.Collections.Generic.IReadOnlyList<EnemySeparationAgent> agents)
        {
            _agents.Clear();
            _agentKeys.Clear();
            if (agents == null) return;

            for (int i = 0; i < agents.Count; i++)
            {
                EnemySeparationAgent input = agents[i];
                Vector3 position = input.Position;
                position.y = 0f;

                Agent agent = new Agent
                {
                    Radius = Mathf.Max(0.01f, input.Radius),
                    Position = position
                };

                _agents[input.AgentId] = agent;
                _agentKeys.Add(input.AgentId);
            }
        }

        public Vector3 Resolve(int agentId, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations)
        {
            if (!_agents.TryGetValue(agentId, out var self)) return desiredPosition;

            Vector3 candidate = desiredPosition;
            candidate.y = 0f;

            Vector3 fallback = fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector3.right;
            fallback.y = 0f;

            int effectiveIterations = Mathf.Max(1, iterations);
            for (int iter = 0; iter < effectiveIterations; iter++)
            {
                for (int i = 0; i < _agentKeys.Count; i++)
                {
                    int otherAgentId = _agentKeys[i];
                    if (otherAgentId == agentId) continue;
                    if (!_agents.TryGetValue(otherAgentId, out var other)) continue;

                    Vector3 toSelf = candidate - other.Position;
                    float minDistance = self.Radius + other.Radius;
                    float minDistanceSq = minDistance * minDistance;
                    float sqrDistance = toSelf.sqrMagnitude;

                    if (sqrDistance <= Mathf.Epsilon)
                    {
                        candidate += fallback * (self.Radius * 0.25f);
                        continue;
                    }

                    if (sqrDistance >= minDistanceSq) continue;

                    float distance = Mathf.Sqrt(sqrDistance);
                    float penetration = minDistance - distance;
                    candidate += (toSelf / distance) * penetration;
                }
            }

            candidate.y = desiredPosition.y;
            return candidate;
        }
    }
}
