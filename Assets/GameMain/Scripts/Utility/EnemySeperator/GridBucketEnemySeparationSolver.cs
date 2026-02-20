using UnityEngine;

namespace CustomUtility
{
    public sealed class GridBucketEnemySeparationSolver : IEnemySeparationSolver
    {
        private sealed class Agent
        {
            public Transform Transform;
            public float Radius;
            public Vector3 Position;
            public int CellX;
            public int CellZ;
        }

        private readonly System.Collections.Generic.Dictionary<Transform, Agent> _agents = new();

        private readonly System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<Transform>>
            _buckets = new();

        private readonly System.Collections.Generic.List<Transform> _recycle = new();
        private readonly float _cellSize;

        private int _snapshotFrame = -1;
        private float _maxRadius = 0.45f;

        public GridBucketEnemySeparationSolver(float cellSize = 1f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

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
            if (agent.Radius > _maxRadius)
            {
                _maxRadius = agent.Radius;
            }

            _snapshotFrame = -1;
        }

        public void Unregister(Transform transform)
        {
            if (transform == null) return;
            if (!_agents.TryGetValue(transform, out var agent)) return;

            RemoveFromBucket(transform, agent.CellX, agent.CellZ);
            _agents.Remove(transform);
            RecalculateMaxRadius();
            _snapshotFrame = -1;
        }

        public Vector3 Resolve(Transform transform, Vector3 desiredPosition, Vector3 fallbackDirection,
            int iterations)
        {
            if (transform == null) return desiredPosition;
            if (!_agents.TryGetValue(transform, out var self)) return desiredPosition;

            EnsureSnapshot();

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
                            Transform otherTransform = bucket[i];
                            if (otherTransform == transform) continue;
                            if (!_agents.TryGetValue(otherTransform, out var other)) continue;

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

            SyncAgentPosition(transform, self, candidate);

            candidate.y = desiredPosition.y;
            return candidate;
        }

        private void EnsureSnapshot()
        {
            int frame = Time.frameCount;
            if (_snapshotFrame == frame) return;

            _snapshotFrame = frame;
            _buckets.Clear();
            _recycle.Clear();

            foreach (var pair in _agents)
            {
                Transform transform = pair.Key;
                Agent agent = pair.Value;
                if (transform == null || agent.Transform == null)
                {
                    _recycle.Add(transform);
                    continue;
                }

                Vector3 position = agent.Transform.position;
                position.y = 0f;
                agent.Position = position;

                agent.CellX = ToCell(position.x);
                agent.CellZ = ToCell(position.z);
                AddToBucket(transform, agent.CellX, agent.CellZ);
            }

            for (int i = 0; i < _recycle.Count; i++)
            {
                _agents.Remove(_recycle[i]);
            }
        }

        private void SyncAgentPosition(Transform transform, Agent agent, Vector3 position)
        {
            int newCellX = ToCell(position.x);
            int newCellZ = ToCell(position.z);

            if (agent.CellX != newCellX || agent.CellZ != newCellZ)
            {
                RemoveFromBucket(transform, agent.CellX, agent.CellZ);
                AddToBucket(transform, newCellX, newCellZ);
                agent.CellX = newCellX;
                agent.CellZ = newCellZ;
            }

            agent.Position = position;
        }

        private void AddToBucket(Transform transform, int cellX, int cellZ)
        {
            long key = CellKey(cellX, cellZ);
            if (!_buckets.TryGetValue(key, out var list))
            {
                list = new System.Collections.Generic.List<Transform>(8);
                _buckets.Add(key, list);
            }

            list.Add(transform);
        }

        private void RemoveFromBucket(Transform transform, int cellX, int cellZ)
        {
            long key = CellKey(cellX, cellZ);
            if (!_buckets.TryGetValue(key, out var list)) return;

            list.Remove(transform);
            if (list.Count == 0)
            {
                _buckets.Remove(key);
            }
        }

        private void RecalculateMaxRadius()
        {
            float max = 0.01f;
            foreach (var pair in _agents)
            {
                if (pair.Value.Radius > max)
                {
                    max = pair.Value.Radius;
                }
            }

            _maxRadius = max;
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
