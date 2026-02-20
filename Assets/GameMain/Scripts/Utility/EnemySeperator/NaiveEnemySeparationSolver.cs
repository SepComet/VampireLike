using UnityEngine;

namespace CustomUtility
{
    public sealed class NaiveEnemySeparationSolver : IEnemySeparationSolver
    {
        private sealed class Agent
        {
            public Transform Transform;
            public float Radius;
        }

        private readonly System.Collections.Generic.Dictionary<Transform, Agent> _agents = new();
        private readonly System.Collections.Generic.List<Transform> _agentKeys = new();

        public void Register(Transform transform, float bodyRadius)
        {
            if (transform == null) return;

            if (!_agents.TryGetValue(transform, out var agent))
            {
                agent = new Agent();
                _agents.Add(transform, agent);
            }

            agent.Transform = transform;
            agent.Radius = Mathf.Max(0.01f, bodyRadius);
        }

        public void Unregister(Transform transform)
        {
            if (transform == null) return;
            _agents.Remove(transform);
        }

        public Vector3 Resolve(Transform transform, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations)
        {
            if (transform == null) return desiredPosition;
            if (!_agents.TryGetValue(transform, out var self)) return desiredPosition;

            Vector3 candidate = desiredPosition;
            candidate.y = 0f;

            Vector3 fallback = fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector3.right;
            fallback.y = 0f;

            _agentKeys.Clear();
            foreach (var pair in _agents)
            {
                _agentKeys.Add(pair.Key);
            }

            int effectiveIterations = Mathf.Max(1, iterations);
            for (int iter = 0; iter < effectiveIterations; iter++)
            {
                for (int i = 0; i < _agentKeys.Count; i++)
                {
                    Transform otherTransform = _agentKeys[i];
                    if (otherTransform == transform) continue;
                    if (!_agents.TryGetValue(otherTransform, out var other)) continue;
                    if (other.Transform == null) continue;

                    Vector3 otherPosition = other.Transform.position;
                    otherPosition.y = 0f;

                    Vector3 toSelf = candidate - otherPosition;
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
