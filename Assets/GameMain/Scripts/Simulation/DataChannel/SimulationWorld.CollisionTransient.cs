using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Collision Transient

        public int CollisionCandidateCount => _collisionCandidates.IsCreated ? _collisionCandidates.Length : 0;
        public int PendingAreaCollisionRequestCount => _areaCollisionRequests.Count;
        public int LastCollisionQueryCount => _lastCollisionQueryCount;
        public int LastProjectileCollisionQueryCount => _lastProjectileCollisionQueryCount;
        public int LastAreaCollisionQueryCount => _lastAreaCollisionQueryCount;
        public int LastCollisionCandidateCount => _lastCollisionCandidateCount;
        public int LastProjectileCollisionCandidateCount => _lastProjectileCollisionCandidateCount;
        public int LastAreaCollisionCandidateCount => _lastAreaCollisionCandidateCount;
        public int LastResolvedAreaHitCount => _lastResolvedAreaHitCount;
        public float LastCollisionCellSize => _lastCollisionCellSize;
        public bool LastCollisionHasEnemyTargets => _lastCollisionHasEnemyTargets;

        private void ResetCollisionRuntimeStats()
        {
            _lastCollisionQueryCount = 0;
            _lastProjectileCollisionQueryCount = 0;
            _lastAreaCollisionQueryCount = 0;
            _lastCollisionCandidateCount = 0;
            _lastProjectileCollisionCandidateCount = 0;
            _lastAreaCollisionCandidateCount = 0;
            _lastResolvedAreaHitCount = 0;
            _lastCollisionCellSize = 0f;
            _lastCollisionHasEnemyTargets = false;
        }

        private void PrepareCollisionQueryAndCandidateChannels(int queryCount, int expectedCandidateCount,
            int bucketCapacity)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _collisionQueryInputs, queryCount);
            EnsureCapacity(ref _collisionCandidates, expectedCandidateCount);
            EnsureCapacity(ref _enemyCollisionBuckets, bucketCapacity);

            _collisionQueryInputs.Clear();
            _collisionCandidates.Clear();
            _enemyCollisionBuckets.Clear();
        }

        private void AddProjectileCollisionQuery(int queryId, in ProjectileJobOutputData projectile, float radius,
            int maxTargets = 1)
        {
            if (!_collisionQueryInputs.IsCreated || radius <= 0f)
            {
                return;
            }

            _collisionQueryInputs.Add(new CollisionQueryData
            {
                QueryId = queryId,
                SourceType = CollisionSourceTypeProjectile,
                SourceEntityId = projectile.EntityId,
                SourceOwnerEntityId = projectile.OwnerEntityId,
                SourceWasActiveAtQueryTime = true,
                Position = projectile.Position,
                Radius = radius,
                MaxTargets = math.max(1, maxTargets),
                ShapeType = CollisionShapeCircle,
                Direction = new float3(0f, 0f, 1f),
                HalfAngleDeg = 180f
            });
        }

        private void AddAreaCollisionQuery(int queryId, int sourceEntityId, int sourceOwnerEntityId,
            bool sourceWasActiveAtQueryTime, in Vector3 center, float radius, int maxTargets, int shapeType,
            in Vector3 direction, float halfAngleDeg)
        {
            if (!_collisionQueryInputs.IsCreated || radius <= 0f)
            {
                return;
            }

            Vector3 normalizedDirection = direction;
            normalizedDirection.y = 0f;
            if (normalizedDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                normalizedDirection = Vector3.forward;
            }
            else
            {
                normalizedDirection.Normalize();
            }

            _collisionQueryInputs.Add(new CollisionQueryData
            {
                QueryId = queryId,
                SourceType = CollisionSourceTypeArea,
                SourceEntityId = sourceEntityId,
                SourceOwnerEntityId = sourceOwnerEntityId,
                SourceWasActiveAtQueryTime = sourceWasActiveAtQueryTime,
                Position = new float3(center.x, center.y, center.z),
                Radius = radius,
                MaxTargets = math.max(1, maxTargets),
                ShapeType = shapeType,
                Direction = new float3(normalizedDirection.x, normalizedDirection.y, normalizedDirection.z),
                HalfAngleDeg = Mathf.Clamp(halfAngleDeg, 0f, 180f)
            });
        }

        private void AddCollisionCandidate(int queryId, int sourceType, int sourceEntityId, int sourceOwnerEntityId,
            int targetEntityId, float sqrDistance)
        {
            if (!_collisionCandidates.IsCreated)
            {
                return;
            }

            _collisionCandidates.Add(new CollisionCandidateData
            {
                QueryId = queryId,
                SourceType = sourceType,
                SourceEntityId = sourceEntityId,
                SourceOwnerEntityId = sourceOwnerEntityId,
                TargetEntityId = targetEntityId,
                SqrDistance = sqrDistance
            });
        }

        #endregion
    }
}
