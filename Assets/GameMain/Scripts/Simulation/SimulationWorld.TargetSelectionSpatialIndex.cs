using CustomDebugger;
using Unity.Collections;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private NativeParallelMultiHashMap<long, int> _enemyTargetBuckets;
        private bool _enemyTargetBucketsDirty = true;

        [SerializeField] private float _targetSelectionCellSize = 2f;

        public bool TryGetNearestEnemyEntityId(Vector3 origin, float maxSqrRange, out int enemyEntityId)
        {
            enemyEntityId = 0;
            if (maxSqrRange <= 0f || _enemies.Count == 0)
            {
                return false;
            }

            if (!_useSimulationMovement || !_useJobSimulation)
            {
                return false;
            }

            BuildEnemyTargetSpatialIndexIfNeeded();

            float cellSize = GetTargetSelectionCellSize();
            int centerCellX = ToCell(origin.x, cellSize);
            int centerCellZ = ToCell(origin.z, cellSize);
            float range = Mathf.Sqrt(maxSqrRange);
            int queryRange = Mathf.Max(1, Mathf.CeilToInt(range / cellSize));

            float minSqrDistance = maxSqrRange;
            bool found = false;

            using (CustomProfilerMarker.TargetSelection_QueryNeighbors.Auto())
            {
                for (int dx = -queryRange; dx <= queryRange; dx++)
                {
                    for (int dz = -queryRange; dz <= queryRange; dz++)
                    {
                        long key = CellKey(centerCellX + dx, centerCellZ + dz);
                        if (!_enemyTargetBuckets.TryGetFirstValue(key, out int enemyIndex,
                                out NativeParallelMultiHashMapIterator<long> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            if (enemyIndex < 0 || enemyIndex >= _enemies.Count)
                            {
                                continue;
                            }

                            EnemySimData enemy = _enemies[enemyIndex];
                            Vector3 delta = enemy.Position - origin;
                            delta.y = 0f;
                            float sqrDistance = delta.sqrMagnitude;
                            if (sqrDistance >= minSqrDistance)
                            {
                                continue;
                            }

                            minSqrDistance = sqrDistance;
                            enemyEntityId = enemy.EntityId;
                            found = true;
                        } while (_enemyTargetBuckets.TryGetNextValue(out enemyIndex, ref iterator));
                    }
                }
            }

            return found;
        }

        private void InitializeEnemyTargetSpatialIndex()
        {
            if (_enemyTargetBuckets.IsCreated)
            {
                return;
            }

            _enemyTargetBuckets = new NativeParallelMultiHashMap<long, int>(256, Allocator.Persistent);
            _enemyTargetBucketsDirty = true;
        }

        private void DisposeEnemyTargetSpatialIndex()
        {
            if (_enemyTargetBuckets.IsCreated)
            {
                _enemyTargetBuckets.Dispose();
            }

            _enemyTargetBuckets = default;
            _enemyTargetBucketsDirty = true;
        }

        private void ClearEnemyTargetSpatialIndex()
        {
            if (_enemyTargetBuckets.IsCreated)
            {
                _enemyTargetBuckets.Clear();
            }

            _enemyTargetBucketsDirty = true;
        }

        private void MarkEnemyTargetSpatialIndexDirty()
        {
            _enemyTargetBucketsDirty = true;
        }

        private void BuildEnemyTargetSpatialIndexIfNeeded()
        {
            InitializeEnemyTargetSpatialIndex();

            if (!_enemyTargetBucketsDirty)
            {
                return;
            }

            using (CustomProfilerMarker.TargetSelection_BuildBuckets.Auto())
            {
                int enemyCount = _enemies.Count;
                int desiredCapacity = Mathf.Max(256, enemyCount * 2 + 1);
                if (_enemyTargetBuckets.Capacity < desiredCapacity)
                {
                    _enemyTargetBuckets.Capacity = desiredCapacity;
                }

                _enemyTargetBuckets.Clear();
                float cellSize = GetTargetSelectionCellSize();

                for (int i = 0; i < enemyCount; i++)
                {
                    EnemySimData enemy = _enemies[i];
                    int cellX = ToCell(enemy.Position.x, cellSize);
                    int cellZ = ToCell(enemy.Position.z, cellSize);
                    _enemyTargetBuckets.Add(CellKey(cellX, cellZ), i);
                }
            }

            _enemyTargetBucketsDirty = false;
        }

        private float GetTargetSelectionCellSize()
        {
            return Mathf.Max(0.1f, _targetSelectionCellSize);
        }

        private static int ToCell(float value, float cellSize)
        {
            return Mathf.FloorToInt(value / cellSize);
        }

        private static long CellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
