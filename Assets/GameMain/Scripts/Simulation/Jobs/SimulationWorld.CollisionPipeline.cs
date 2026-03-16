using CustomDebugger;
using CustomEvent;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Entity.Weapon;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private const int PlayerEntityId = -1;
        private JobHandle _collisionCandidateQueryHandle;
        private bool _collisionCandidateQueryScheduled;

        [Header("Projectile Collision Query")]
        [Tooltip("Projectile broad-phase collision query radius.")]
        [SerializeField]
        private float _projectileCollisionQueryRadius = 0.35f;

        [Tooltip("Maximum retained candidates per projectile query.")]
        [SerializeField]
        private int _projectileMaxCandidatesPerQuery = 1;

        [Tooltip("Broad-phase bucket cell size. <=0 derives from query radius.")]
        [SerializeField]
        private float _projectileCollisionCellSize = 0f;

        [Header("Projectile Hit Event Dispatch")]
        [Tooltip("Dispatch projectile hit presentation event.")]
        [SerializeField]
        private bool _dispatchProjectileHitPresentationEvent = true;

        [Tooltip("Request hit marker when projectile hits.")]
        [SerializeField]
        private bool _dispatchProjectileHitMarkerEvent = true;

        [Tooltip("Request hit effect when projectile hits.")]
        [SerializeField]
        private bool _dispatchProjectileHitEffectEvent = true;

        [Tooltip("Default hit effect entity type id in presentation event. 0 means not specified.")]
        [SerializeField]
        private int _projectileHitPresentationEffectTypeId = 0;

        #region Area Query Request

        public bool TryRequestAreaCollision(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, int maxTargets = 16)
        {
            return TryRequestAreaCollisionInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeCircle, Vector3.forward, 180f);
        }

        public bool TryRequestSectorCollision(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, in Vector3 direction, float halfAngleDeg, int maxTargets = 16)
        {
            return TryRequestAreaCollisionInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeSector, direction, halfAngleDeg);
        }

        private bool TryRequestAreaCollisionInternal(int sourceEntityId, int sourceOwnerEntityId,
            in Vector3 center,
            float radius, int maxTargets, int shapeType, in Vector3 direction, float halfAngleDeg)
        {
            if (!_useSimulationMovement)
            {
                return false;
            }

            if (sourceEntityId == 0 || radius <= 0f || maxTargets <= 0)
            {
                return false;
            }

            int resolvedOwnerEntityId = sourceOwnerEntityId != 0 ? sourceOwnerEntityId : sourceEntityId;
            bool sourceWasActiveAtQueryTime = WasCollisionSourceActiveAtQueryTime(sourceEntityId);
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

            _areaCollisionRequests.Add(new AreaCollisionRequestData
            {
                SourceEntityId = sourceEntityId,
                SourceOwnerEntityId = resolvedOwnerEntityId,
                SourceWasActiveAtQueryTime = sourceWasActiveAtQueryTime,
                Center = center,
                Radius = Mathf.Max(0.01f, radius),
                MaxTargets = Mathf.Max(1, maxTargets),
                ShapeType = shapeType,
                Direction = normalizedDirection,
                HalfAngleDeg = Mathf.Clamp(halfAngleDeg, 0f, 180f)
            });

            return true;
        }

        private int GetPendingAreaCollisionRequestCount()
        {
            return _areaCollisionRequests.Count;
        }

        private int EstimatePendingAreaCollisionCandidateCountFromRequests()
        {
            int expectedCount = 0;
            for (int i = 0; i < _areaCollisionRequests.Count; i++)
            {
                expectedCount += Mathf.Max(1, _areaCollisionRequests[i].MaxTargets);
            }

            return expectedCount;
        }

        #endregion

        #region Collision Pipeline

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
                        request.ShapeType, in request.Direction, request.HalfAngleDeg);
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

        private void ResolveCollisionCandidatesOnMainThread()
        {
            if (!_collisionCandidates.IsCreated)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionTransientBuffers();
                return;
            }

            _projectileResolvedEntityIds.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();

            if (_collisionCandidates.Length == 0)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionTransientBuffers();
                return;
            }

            using (CustomProfilerMarker.Collision_ResolveProjectile.Auto())
            {
                for (int i = 0; i < _collisionCandidates.Length; i++)
                {
                    CollisionCandidateData candidate = _collisionCandidates[i];
                    if (candidate.SourceType == CollisionSourceTypeProjectile)
                    {
                        int projectileEntityId = candidate.SourceEntityId;
                        if (_projectileResolvedEntityIds.Contains(projectileEntityId))
                        {
                            continue;
                        }

                        if (!TryGetActiveProjectileSimData(projectileEntityId, out _, out ProjectileSimData projectile))
                        {
                            _projectileResolvedEntityIds.Add(projectileEntityId);
                            continue;
                        }

                        bool shouldExpireProjectile = true;
                        bool shouldDispatchPresentation = false;
                        int damage = 0;
                        Vector3 hitPosition = projectile.Position;
                        if (TryGetAliveTargetableEntity(candidate.TargetEntityId, out TargetableObject target))
                        {
                            EntityBase sourceEntity = TryGetEntityById(candidate.SourceEntityId);
                            EntityBase ownerEntity = TryGetEntityById(candidate.SourceOwnerEntityId);
                            shouldExpireProjectile = ResolveProjectileHitAgainstTarget(target, sourceEntity,
                                ownerEntity,
                                in projectile,
                                out damage, out hitPosition, out shouldDispatchPresentation);
                        }

                        if (shouldDispatchPresentation)
                        {
                            DispatchProjectileHitPresentationEvent(projectileEntityId, candidate.SourceEntityId,
                                candidate.SourceOwnerEntityId, candidate.TargetEntityId, damage, in hitPosition);
                        }

                        if (shouldExpireProjectile)
                        {
                            MarkProjectileAsExpired(projectileEntityId);
                            _projectileResolvedEntityIds.Add(projectileEntityId);
                        }

                        continue;
                    }

                    if (candidate.SourceType == CollisionSourceTypeArea)
                    {
                        long dedupKey = (((long)candidate.QueryId) << 32) ^ (uint)candidate.TargetEntityId;
                        if (!_areaCollisionHitDedupKeys.Add(dedupKey))
                        {
                            continue;
                        }

                        _areaCollisionHitEvents.Add(new AreaCollisionHitEventData
                        {
                            QueryId = candidate.QueryId,
                            SourceEntityId = candidate.SourceEntityId,
                            SourceOwnerEntityId = candidate.SourceOwnerEntityId,
                            TargetEntityId = candidate.TargetEntityId,
                            SqrDistance = candidate.SqrDistance
                        });
                    }
                }
            }

            int resolvedAreaHitCount;
            using (CustomProfilerMarker.Collision_ResolveArea.Auto())
            {
                resolvedAreaHitCount = ResolveAreaCollisionHitsOnMainThread();
            }

            _lastResolvedAreaHitCount = resolvedAreaHitCount;
            _projectileResolvedEntityIds.Clear();
            ClearAreaCollisionTransientBuffers();
        }

        #endregion

        #region Collision Resolve Helpers

        private bool ResolveProjectileHitAgainstTarget(TargetableObject target, EntityBase sourceEntity,
            EntityBase ownerEntity,
            in ProjectileSimData projectile, out int damage, out Vector3 hitPosition,
            out bool shouldDispatchPresentation)
        {
            damage = 0;
            hitPosition = projectile.Position;
            shouldDispatchPresentation = false;

            if (target == null || !target.Available || target.IsDead)
            {
                return true;
            }

            if (target.CachedTransform != null)
            {
                hitPosition = target.CachedTransform.position;
            }

            if (!TryResolveImpactSource(sourceEntity, ownerEntity, out EntityBase attacker,
                    out ImpactData sourceImpact))
            {
                shouldDispatchPresentation = true;
                return true;
            }

            ImpactData targetImpact = target.GetImpactData();
            if (AIUtility.GetRelation(targetImpact.Camp, sourceImpact.Camp) == RelationType.Friendly)
            {
                return false;
            }

            damage = AIUtility.CalcDamageHP(sourceImpact.AttackBase, sourceImpact.AttackStat,
                targetImpact.DefenseStat,
                targetImpact.DodgeStat);
            shouldDispatchPresentation = true;
            if (damage <= 0)
            {
                return true;
            }

            target.ApplyDamage(attacker ?? sourceEntity ?? ownerEntity, damage);
            return true;
        }

        private void DispatchProjectileHitPresentationEvent(int projectileEntityId, int sourceEntityId,
            int sourceOwnerEntityId, int targetEntityId, int damage, in Vector3 hitPosition)
        {
            if (!_dispatchProjectileHitPresentationEvent)
            {
                return;
            }

            var eventComponent = GameEntry.Event;
            if (eventComponent == null)
            {
                return;
            }

            eventComponent.Fire(this, ProjectileHitPresentationEventArgs.Create(
                projectileEntityId,
                sourceEntityId,
                sourceOwnerEntityId,
                targetEntityId,
                damage,
                hitPosition,
                _dispatchProjectileHitMarkerEvent,
                _dispatchProjectileHitEffectEvent,
                _projectileHitPresentationEffectTypeId));
        }

        private static bool TryResolveImpactSource(EntityBase sourceEntity, EntityBase ownerEntity,
            out EntityBase attacker,
            out ImpactData impactData)
        {
            if (TryResolveImpactFromEntity(sourceEntity, out impactData))
            {
                attacker = sourceEntity;
                return true;
            }

            if (TryResolveImpactFromEntity(ownerEntity, out impactData))
            {
                attacker = ownerEntity;
                return true;
            }

            attacker = null;
            impactData = default;
            return false;
        }

        private static bool TryResolveImpactFromEntity(EntityBase entity, out ImpactData impactData)
        {
            if (entity is WeaponBase weapon)
            {
                impactData = weapon.GetImpactData();
                return true;
            }

            if (entity is EnemyProjectile enemyProjectile)
            {
                impactData = enemyProjectile.GetImpactData();
                return true;
            }

            if (entity is TargetableObject targetableObject)
            {
                impactData = targetableObject.GetImpactData();
                return true;
            }

            impactData = default;
            return false;
        }

        private static bool TryGetAliveTargetableEntity(int entityId, out TargetableObject target)
        {
            target = null;

            var enemyManager = GameEntry.EnemyManager;
            if (enemyManager != null && enemyManager.TryGetEnemy(entityId, out EntityBase enemyEntity))
            {
                if (enemyEntity is TargetableObject enemyTarget && enemyTarget.Available && !enemyTarget.IsDead)
                {
                    target = enemyTarget;
                    return true;
                }
            }

            EntityBase entity = TryGetEntityById(entityId);
            if (entity is TargetableObject targetable && targetable.Available && !targetable.IsDead)
            {
                target = targetable;
                return true;
            }

            return false;
        }

        private static EntityBase TryGetEntityById(int entityId)
        {
            var entityComponent = GameEntry.Entity;
            return entityComponent != null ? entityComponent.GetGameEntity(entityId) : null;
        }

        private bool TryGetActiveProjectileSimData(int projectileEntityId, out int simulationIndex,
            out ProjectileSimData projectile)
        {
            simulationIndex = -1;
            projectile = default;

            if (!ProjectileBinding.TryGetSimulationIndex(projectileEntityId, out int foundIndex) || foundIndex < 0 ||
                foundIndex >= _projectiles.Count)
            {
                return false;
            }

            ProjectileSimData data = _projectiles[foundIndex];
            if (!data.Active || data.State != ProjectileStateActive)
            {
                return false;
            }

            simulationIndex = foundIndex;
            projectile = data;
            return true;
        }

        private bool MarkProjectileAsExpired(int projectileEntityId)
        {
            if (!ProjectileBinding.TryGetSimulationIndex(projectileEntityId, out int simulationIndex) ||
                simulationIndex < 0 || simulationIndex >= _projectiles.Count)
            {
                return false;
            }

            ProjectileSimData projectile = _projectiles[simulationIndex];
            projectile.Active = false;
            projectile.State = ProjectileStateExpired;
            projectile.RemainingLifetime = 0f;
            if (projectile.LifeTime > 0f && projectile.Age < projectile.LifeTime)
            {
                projectile.Age = projectile.LifeTime;
            }

            _projectiles[simulationIndex] = projectile;
            return true;
        }

        private int ResolveAreaCollisionHitsOnMainThread()
        {
            if (_areaCollisionHitEvents.Count == 0)
            {
                return 0;
            }

            int resolvedHitCount = 0;
            for (int i = 0; i < _areaCollisionHitEvents.Count; i++)
            {
                AreaCollisionHitEventData hitEvent = _areaCollisionHitEvents[i];
                if (!TryGetCollisionQueryByQueryId(hitEvent.QueryId, out CollisionQueryData query) ||
                    query.SourceType != CollisionSourceTypeArea)
                {
                    continue;
                }

                if (!query.SourceWasActiveAtQueryTime)
                {
                    continue;
                }

                EntityBase sourceEntity = TryGetEntityById(hitEvent.SourceEntityId);
                if (sourceEntity == null || !sourceEntity.Available)
                {
                    continue;
                }

                if (!TryGetAliveTargetableEntity(hitEvent.TargetEntityId, out TargetableObject target))
                {
                    continue;
                }

                if (!IsAreaTargetInsidePreciseShape(in query, target))
                {
                    continue;
                }

                AIUtility.PerformCollision(target, sourceEntity, true);
                resolvedHitCount++;
            }

            return resolvedHitCount;
        }

        private bool TryGetCollisionQueryByQueryId(int queryId, out CollisionQueryData query)
        {
            query = default;
            if (!_collisionQueryInputs.IsCreated || queryId < 0 || queryId >= _collisionQueryInputs.Length)
            {
                return false;
            }

            CollisionQueryData direct = _collisionQueryInputs[queryId];
            if (direct.QueryId == queryId)
            {
                query = direct;
                return true;
            }

            for (int i = 0; i < _collisionQueryInputs.Length; i++)
            {
                CollisionQueryData candidate = _collisionQueryInputs[i];
                if (candidate.QueryId != queryId)
                {
                    continue;
                }

                query = candidate;
                return true;
            }

            return false;
        }

        private static bool IsAreaTargetInsidePreciseShape(in CollisionQueryData query, TargetableObject target)
        {
            if (target == null || target.CachedTransform == null)
            {
                return false;
            }

            Vector3 center = new Vector3(query.Position.x, query.Position.y, query.Position.z);
            Vector3 toTarget = target.CachedTransform.position - center;
            toTarget.y = 0f;

            float radius = Mathf.Max(0.01f, query.Radius);
            float radiusSqr = radius * radius;
            float sqrDistance = toTarget.sqrMagnitude;
            if (sqrDistance > radiusSqr)
            {
                return false;
            }

            if (query.ShapeType != CollisionShapeSector)
            {
                return true;
            }

            if (sqrDistance <= Mathf.Epsilon)
            {
                return true;
            }

            Vector3 forward = new Vector3(query.Direction.x, query.Direction.y, query.Direction.z);
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            float halfAngle = Mathf.Clamp(query.HalfAngleDeg, 0f, 180f);
            float angle = Vector3.Angle(forward, toTarget.normalized);
            return angle <= halfAngle;
        }

        private void ClearAreaCollisionTransientBuffers()
        {
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
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

        private static bool WasCollisionSourceActiveAtQueryTime(int sourceEntityId)
        {
            EntityBase sourceEntity = TryGetEntityById(sourceEntityId);
            if (sourceEntity == null || !sourceEntity.Available)
            {
                return false;
            }

            if (sourceEntity is WeaponBase weapon)
            {
                return weapon.IsAttacking;
            }

            if (sourceEntity is EnemyProjectile projectile)
            {
                return projectile.IsActive;
            }

            return true;
        }

        #endregion
    }
}
