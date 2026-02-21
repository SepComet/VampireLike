using UnityEngine;

namespace CustomUtility
{
    public sealed class GridBucketEnemySeparationSolver : IEnemySeparationSolver
    {
        private struct Agent
        {
            public float Radius;
            public Vector3 Position;
            public int CellX;
            public int CellZ;
        }

        private readonly System.Collections.Generic.Dictionary<int, Agent> _agents = new();
        private readonly System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<int>> _buckets = new();
        private readonly System.Collections.Generic.Stack<System.Collections.Generic.List<int>> _bucketListPool = new();
        private readonly System.Collections.Generic.List<long> _activeBucketKeys = new();
        private readonly float _cellSize;

        private float _maxRadius = 0.45f;

        public GridBucketEnemySeparationSolver(float cellSize = 1f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        public void SetAgents(System.Collections.Generic.IReadOnlyList<EnemySeparationAgent> agents)
        {
            RecycleBucketsForSnapshot();
            _agents.Clear();
            _maxRadius = 0.01f;

            if (agents == null) return;

            for (int i = 0; i < agents.Count; i++)
            {
                EnemySeparationAgent input = agents[i];
                Vector3 position = input.Position;
                position.y = 0f;
                float radius = Mathf.Max(0.01f, input.Radius);

                Agent agent = new Agent
                {
                    Radius = radius,
                    Position = position,
                    CellX = ToCell(position.x),
                    CellZ = ToCell(position.z)
                };

                _agents[input.AgentId] = agent;
                AddToBucket(input.AgentId, agent.CellX, agent.CellZ);

                if (radius > _maxRadius)
                {
                    _maxRadius = radius;
                }
            }
        }

        public Vector3 Resolve(int agentId, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations)
        {
            if (!_agents.TryGetValue(agentId, out var self)) return desiredPosition;

            Vector3 candidate = desiredPosition;
            candidate.y = 0f;

            int effectiveIterations = Mathf.Max(1, iterations);
            int queryRange = Mathf.Max(1, Mathf.CeilToInt((self.Radius + _maxRadius) / _cellSize));
            Vector3 fallback = fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector3.right;
            fallback.y = 0f;

            for (int iter = 0; iter < effectiveIterations; iter++)
            {
                int cellX = ToCell(candidate.x);
                int cellZ = ToCell(candidate.z);

                for (int dx = -queryRange; dx <= queryRange; dx++)
                {
                    for (int dz = -queryRange; dz <= queryRange; dz++)
                    {
                        if (!_buckets.TryGetValue(CellKey(cellX + dx, cellZ + dz), out var bucket)) continue;

                        for (int i = 0; i < bucket.Count; i++)
                        {
                            int otherAgentId = bucket[i];
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
                }
            }

            SyncAgentPosition(agentId, ref self, candidate);

            candidate.y = desiredPosition.y;
            return candidate;
        }

        private void RecycleBucketsForSnapshot()
        {
            for (int i = 0; i < _activeBucketKeys.Count; i++)
            {
                long key = _activeBucketKeys[i];
                if (!_buckets.TryGetValue(key, out var bucket)) continue;

                bucket.Clear();
                _bucketListPool.Push(bucket);
                _buckets.Remove(key);
            }

            _activeBucketKeys.Clear();
        }

        private void SyncAgentPosition(int agentId, ref Agent agent, Vector3 position)
        {
            int newCellX = ToCell(position.x);
            int newCellZ = ToCell(position.z);

            if (agent.CellX != newCellX || agent.CellZ != newCellZ)
            {
                RemoveFromBucket(agentId, agent.CellX, agent.CellZ);
                AddToBucket(agentId, newCellX, newCellZ);
                agent.CellX = newCellX;
                agent.CellZ = newCellZ;
            }

            agent.Position = position;
            _agents[agentId] = agent;
        }

        private void AddToBucket(int agentId, int cellX, int cellZ)
        {
            long key = CellKey(cellX, cellZ);
            if (!_buckets.TryGetValue(key, out var list))
            {
                list = _bucketListPool.Count > 0
                    ? _bucketListPool.Pop()
                    : new System.Collections.Generic.List<int>(8);
                _buckets.Add(key, list);
                _activeBucketKeys.Add(key);
            }

            list.Add(agentId);
        }

        private void RemoveFromBucket(int agentId, int cellX, int cellZ)
        {
            long key = CellKey(cellX, cellZ);
            if (!_buckets.TryGetValue(key, out var list)) return;

            list.Remove(agentId);
        }

        private int ToCell(float value)
        {
            return Mathf.FloorToInt(value / _cellSize);
        }

        private static long CellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
