using CustomDebugger;
using Entity;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Collision Broad Phase

        private void PrepareCollisionCandidatesForFrame()
        {
            _collisionCandidateQueryScheduled = false;

            if (!_collisionQueryInputs.IsCreated || !_collisionCandidates.IsCreated ||
                !_enemyCollisionBuckets.IsCreated)
            {
                ResetCollisionRuntimeStats();
                ClearAreaCollisionTransientBuffers();
                return;
            }

            int projectileCount = _projectileJobOutputs.Length;
            int areaQueryCount = _areaCollisionRequests.Count;
            if (projectileCount == 0 && areaQueryCount == 0)
            {
                ResetCollisionRuntimeStats();
                return;
            }

            float queryRadius = Mathf.Max(0.01f, _projectileCollisionQueryRadius);
            int maxCandidatesPerQuery = Mathf.Max(1, _projectileMaxCandidatesPerQuery);
            float maxQueryRadius = queryRadius;
            int queryId = 0;
            int projectileQueryCount = 0;
            int builtAreaQueryCount = 0;
            using (CustomProfilerMarker.Collision_BuildQueries.Auto())
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    ProjectileJobOutputData projectile = _projectileJobOutputs[i];
                    if (!projectile.Active || projectile.State != ProjectileStateActive)
                    {
                        continue;
                    }

                    AddProjectileCollisionQuery(queryId, in projectile, queryRadius, maxCandidatesPerQuery);
                    queryId++;
                    projectileQueryCount++;
                }

                for (int i = 0; i < areaQueryCount; i++)
                {
                    AreaCollisionRequestData request = _areaCollisionRequests[i];
                    AddAreaCollisionQuery(queryId, request.SourceEntityId, request.SourceOwnerEntityId,
                        request.SourceWasActiveAtQueryTime, in request.Center, request.Radius, request.MaxTargets,
                        request.ShapeType, in request.Direction, request.HalfAngleDeg, request.HalfWidth,
                        request.HalfLength);
                    queryId++;
                    builtAreaQueryCount++;
                    if (request.Radius > maxQueryRadius)
                    {
                        maxQueryRadius = request.Radius;
                    }
                }
            }

            _lastProjectileCollisionQueryCount = projectileQueryCount;
            _lastAreaCollisionQueryCount = builtAreaQueryCount;
            _lastCollisionQueryCount = projectileQueryCount + builtAreaQueryCount;
            _lastResolvedAreaHitCount = 0;

            if (_collisionQueryInputs.Length == 0)
            {
                _lastCollisionCandidateCount = 0;
                _lastProjectileCollisionCandidateCount = 0;
                _lastAreaCollisionCandidateCount = 0;
                _lastCollisionCellSize = 0f;
                _lastCollisionHasEnemyTargets = _enemyJobOutputs.Length > 0;
                return;
            }

            float autoCellSize = maxQueryRadius * 2f;
            float configuredCellSize = _projectileCollisionCellSize > 0f ? _projectileCollisionCellSize : autoCellSize;
            float cellSize = Mathf.Max(0.1f, configuredCellSize);
            bool hasEnemyTargets = _enemyJobOutputs.Length > 0;
            _lastCollisionCellSize = cellSize;
            _lastCollisionHasEnemyTargets = hasEnemyTargets;

            using (CustomProfilerMarker.Collision_BuildBuckets.Auto())
            {
                if (hasEnemyTargets)
                {
                    BuildEnemyCollisionBuckets(cellSize);
                }
            }

            using (CustomProfilerMarker.Collision_QueryCandidates.Auto())
            {
                _collisionCandidateQueryScheduled = ScheduleCollisionCandidateQueryJob(cellSize, hasEnemyTargets,
                    out _collisionCandidateQueryHandle);
            }
        }

        private void CompleteCollisionCandidatesForFrame()
        {
            if (_collisionCandidateQueryScheduled)
            {
                _collisionCandidateQueryHandle.Complete();
                _collisionCandidateQueryScheduled = false;
            }

            CountCollisionCandidatesBySourceType(out int projectileCandidateCount, out int areaCandidateCount);
            _lastProjectileCollisionCandidateCount = projectileCandidateCount;
            _lastAreaCollisionCandidateCount = areaCandidateCount;
            _lastCollisionCandidateCount = projectileCandidateCount + areaCandidateCount;
        }

        private void BuildEnemyCollisionBuckets(float cellSize)
        {
            _enemyCollisionBuckets.Clear();
            int enemyCount = _enemyJobOutputs.Length;
            if (enemyCount <= 0)
            {
                return;
            }

            BuildCollisionBucketsBurstJob job = new BuildCollisionBucketsBurstJob
            {
                EnemyOutputs = _enemyJobOutputs.AsArray(),
                Buckets = _enemyCollisionBuckets.AsParallelWriter(),
                CellSize = cellSize
            };

            using (CustomProfilerMarker.TickEnemies_Complete.Auto())
            {
                JobHandle handle = job.Schedule(enemyCount, 64);
                handle.Complete();
            }
        }

        [BurstCompile]
        private struct BuildCollisionBucketsBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> EnemyOutputs;
            public NativeParallelMultiHashMap<long, int>.ParallelWriter Buckets;
            public float CellSize;

            public void Execute(int index)
            {
                EnemyJobOutputData enemy = EnemyOutputs[index];
                int cellX = (int)math.floor(enemy.Position.x / CellSize);
                int cellZ = (int)math.floor(enemy.Position.z / CellSize);
                Buckets.Add(SeparationCellKey(cellX, cellZ), index);
            }
        }

        private bool ScheduleCollisionCandidateQueryJob(float cellSize, bool hasEnemyTargets, out JobHandle handle)
        {
            handle = default;
            if (!_collisionQueryInputs.IsCreated || !_collisionCandidates.IsCreated)
            {
                return false;
            }

            _collisionCandidates.Clear();
            int queryCount = _collisionQueryInputs.Length;
            if (queryCount == 0)
            {
                return false;
            }

            bool hasPlayerTarget = TryGetPlayerCollisionTarget(out int playerTargetEntityId, out float3 playerPosition);
            QueryCollisionCandidatesBurstJob job = new QueryCollisionCandidatesBurstJob
            {
                Queries = _collisionQueryInputs.AsArray(),
                EnemyBuckets = _enemyCollisionBuckets,
                EnemyOutputs = _enemyJobOutputs.AsArray(),
                Candidates = _collisionCandidates.AsParallelWriter(),
                HasEnemyTargets = hasEnemyTargets,
                HasPlayerTarget = hasPlayerTarget,
                PlayerTargetEntityId = playerTargetEntityId,
                PlayerPosition = playerPosition,
                CellSize = cellSize
            };

            handle = job.Schedule(queryCount, 64);
            return true;
        }

        private void CountCollisionCandidatesBySourceType(out int projectileCandidateCount, out int areaCandidateCount)
        {
            projectileCandidateCount = 0;
            areaCandidateCount = 0;

            if (!_collisionCandidates.IsCreated)
            {
                return;
            }

            for (int i = 0; i < _collisionCandidates.Length; i++)
            {
                CollisionCandidateData candidate = _collisionCandidates[i];
                if (candidate.SourceType == CollisionSourceTypeProjectile)
                {
                    projectileCandidateCount++;
                }
                else if (candidate.SourceType == CollisionSourceTypeArea)
                {
                    areaCandidateCount++;
                }
            }
        }

        private static bool TryGetPlayerCollisionTarget(out int playerEntityId, out float3 playerPosition)
        {
            playerEntityId = PlayerEntityId;
            playerPosition = default;

            if (!TryGetAliveTargetableEntity(playerEntityId, out TargetableObject playerTarget))
            {
                return false;
            }

            Transform playerTransform = playerTarget.CachedTransform;
            if (playerTransform == null)
            {
                return false;
            }

            Vector3 position = playerTransform.position;
            playerPosition = new float3(position.x, position.y, position.z);
            return true;
        }

        #endregion
    }
}
