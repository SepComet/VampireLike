using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using CustomDebugger;
using CustomEvent;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Entity.Weapon;
using CustomUtility;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private const int PlayerEntityId = -1;

        [Header("投射物模拟参数")] [Tooltip("投射物距离玩家超过该水平半径时回收。小于等于 0 表示不启用该回收条件。")] [SerializeField]
        private float _projectileMaxDistanceFromPlayer = 120f;

        [Tooltip("投射物与玩家的垂直高度差超过该值时回收。小于等于 0 表示不启用该回收条件。")] [SerializeField]
        private float _projectileMaxVerticalOffsetFromPlayer = 30f;

        [Tooltip("投射物 Broad Phase 命中查询半径。")] [SerializeField]
        private float _projectileCollisionQueryRadius = 0.35f;

        [Tooltip("每个投射物最多保留的候选目标数量。")] [SerializeField]
        private int _projectileMaxCandidatesPerQuery = 1;

        [Tooltip("投射物 Broad Phase 分桶网格尺寸。小于等于 0 时将按查询半径自动推导。")] [SerializeField]
        private float _projectileCollisionCellSize = 0f;

        [Header("投射物命中事件派发")] [Tooltip("是否派发投射物命中表现事件。")] [SerializeField]
        private bool _dispatchProjectileHitPresentationEvent = true;

        [Tooltip("命中时是否请求命中标记表现。")] [SerializeField]
        private bool _dispatchProjectileHitMarkerEvent = true;

        [Tooltip("命中时是否请求特效表现。")] [SerializeField]
        private bool _dispatchProjectileHitEffectEvent = true;

        [Tooltip("命中事件建议使用的特效实体类型 Id（<=0 表示不指定，由表现层决定）。")] [SerializeField]
        private int _projectileHitPresentationEffectTypeId = 0;

        [BurstCompile]
        private struct ProjectileMovementBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ProjectileJobInputData> Inputs;
            public NativeArray<ProjectileJobOutputData> Outputs;
            public float DeltaTime;
            public float3 PlayerPosition;
            public float MaxSqrDistanceFromPlayer;
            public float MaxVerticalOffsetFromPlayer;

            public void Execute(int index)
            {
                ExecuteProjectileMovement(index, Inputs, Outputs, DeltaTime, PlayerPosition,
                    MaxSqrDistanceFromPlayer, MaxVerticalOffsetFromPlayer);
            }
        }

        private struct ProjectileMovementJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ProjectileJobInputData> Inputs;
            public NativeArray<ProjectileJobOutputData> Outputs;
            public float DeltaTime;
            public float3 PlayerPosition;
            public float MaxSqrDistanceFromPlayer;
            public float MaxVerticalOffsetFromPlayer;

            public void Execute(int index)
            {
                ExecuteProjectileMovement(index, Inputs, Outputs, DeltaTime, PlayerPosition,
                    MaxSqrDistanceFromPlayer, MaxVerticalOffsetFromPlayer);
            }
        }

        public bool TryEnqueueAreaCollisionQuery(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, int maxTargets = 16)
        {
            return TryEnqueueAreaCollisionQueryInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeCircle, Vector3.forward, 180f);
        }

        public bool TryEnqueueSectorCollisionQuery(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, in Vector3 direction, float halfAngleDeg, int maxTargets = 16)
        {
            return TryEnqueueAreaCollisionQueryInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeSector, direction, halfAngleDeg);
        }

        private bool TryEnqueueAreaCollisionQueryInternal(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, int maxTargets, int shapeType, in Vector3 direction, float halfAngleDeg)
        {
            if (!_useSimulationMovement || !_useJobSimulation)
            {
                return false;
            }

            if (sourceEntityId == 0 || radius <= 0f || maxTargets <= 0)
            {
                return false;
            }

            int resolvedOwnerEntityId = sourceOwnerEntityId != 0 ? sourceOwnerEntityId : sourceEntityId;
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
                Center = center,
                Radius = Mathf.Max(0.01f, radius),
                MaxTargets = Mathf.Max(1, maxTargets),
                ShapeType = shapeType,
                Direction = normalizedDirection,
                HalfAngleDeg = Mathf.Clamp(halfAngleDeg, 0f, 180f)
            });

            return true;
        }

        private int GetPendingAreaCollisionQueryCount()
        {
            return _areaCollisionRequests.Count;
        }

        private int EstimatePendingAreaCollisionCandidateCount()
        {
            int expectedCount = 0;
            for (int i = 0; i < _areaCollisionRequests.Count; i++)
            {
                expectedCount += Mathf.Max(1, _areaCollisionRequests[i].MaxTargets);
            }

            return expectedCount;
        }

        private void ExecuteProjectileMovementJob(in SimulationTickContext context)
        {
            int projectileCount = _projectileJobInputs.Length;
            PrepareProjectileJobOutputBuffer(projectileCount);

            if (projectileCount == 0)
            {
                return;
            }

            if (context.DeltaTime <= 0f)
            {
                CopyProjectileInputToOutput();
                return;
            }

            float maxDistance = Mathf.Max(0f, _projectileMaxDistanceFromPlayer);
            float maxSqrDistanceFromPlayer = maxDistance > 0f ? maxDistance * maxDistance : -1f;
            float maxVerticalOffsetFromPlayer = Mathf.Max(0f, _projectileMaxVerticalOffsetFromPlayer);
            float3 playerPosition =
                new float3(context.PlayerPosition.x, context.PlayerPosition.y, context.PlayerPosition.z);
            NativeArray<ProjectileJobInputData> inputArray = _projectileJobInputs.AsArray();
            NativeArray<ProjectileJobOutputData> outputArray = _projectileJobOutputs.AsArray();

            JobHandle handle;
            if (_useBurstJobs)
            {
                ProjectileMovementBurstJob burstJob = new ProjectileMovementBurstJob
                {
                    Inputs = inputArray,
                    Outputs = outputArray,
                    DeltaTime = context.DeltaTime,
                    PlayerPosition = playerPosition,
                    MaxSqrDistanceFromPlayer = maxSqrDistanceFromPlayer,
                    MaxVerticalOffsetFromPlayer = maxVerticalOffsetFromPlayer
                };
                handle = burstJob.Schedule(projectileCount, 64);
            }
            else
            {
                ProjectileMovementJob job = new ProjectileMovementJob
                {
                    Inputs = inputArray,
                    Outputs = outputArray,
                    DeltaTime = context.DeltaTime,
                    PlayerPosition = playerPosition,
                    MaxSqrDistanceFromPlayer = maxSqrDistanceFromPlayer,
                    MaxVerticalOffsetFromPlayer = maxVerticalOffsetFromPlayer
                };
                handle = job.Schedule(projectileCount, 64);
            }

            handle.Complete();
        }

        private void BuildProjectileCollisionCandidates()
        {
            if (!_collisionQueryInputs.IsCreated || !_collisionCandidates.IsCreated ||
                !_enemyCollisionBuckets.IsCreated)
            {
                ResetCollisionRuntimeStats();
                ClearAreaCollisionFrameBuffers();
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
                    AddAreaCollisionQuery(queryId, request.SourceEntityId, request.SourceOwnerEntityId, in request.Center,
                        request.Radius, request.MaxTargets, request.ShapeType, in request.Direction, request.HalfAngleDeg);
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
                    BuildEnemyCollisionBucketsForProjectiles(cellSize);
                }
            }

            int projectileCandidateCount;
            int areaCandidateCount;
            using (CustomProfilerMarker.Collision_QueryCandidates.Auto())
            {
                QueryProjectileCollisionCandidates(cellSize, hasEnemyTargets, out projectileCandidateCount,
                    out areaCandidateCount);
            }

            _lastProjectileCollisionCandidateCount = projectileCandidateCount;
            _lastAreaCollisionCandidateCount = areaCandidateCount;
            _lastCollisionCandidateCount = projectileCandidateCount + areaCandidateCount;
        }

        private void ResolveProjectileCollisionCandidatesMainThread()
        {
            if (!_collisionCandidates.IsCreated)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionFrameBuffers();
                return;
            }

            _projectileResolvedEntityIds.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();

            if (_collisionCandidates.Length == 0)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionFrameBuffers();
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

                        if (!TryGetActiveProjectileData(projectileEntityId, out _, out ProjectileSimData projectile))
                        {
                            _projectileResolvedEntityIds.Add(projectileEntityId);
                            continue;
                        }

                        bool shouldExpireProjectile = true;
                        bool shouldDispatchPresentation = false;
                        int damage = 0;
                        Vector3 hitPosition = projectile.Position;
                        if (TryGetTargetableEntity(candidate.TargetEntityId, out TargetableObject target))
                        {
                            EntityBase sourceEntity = TryGetEntityById(candidate.SourceEntityId);
                            EntityBase ownerEntity = TryGetEntityById(candidate.SourceOwnerEntityId);
                            shouldExpireProjectile = ResolveProjectileHit(target, sourceEntity, ownerEntity, in projectile,
                                out damage, out hitPosition, out shouldDispatchPresentation);
                        }

                        if (shouldDispatchPresentation)
                        {
                            DispatchProjectileHitPresentationEvent(projectileEntityId, candidate.SourceEntityId,
                                candidate.SourceOwnerEntityId, candidate.TargetEntityId, damage, in hitPosition);
                        }

                        if (shouldExpireProjectile)
                        {
                            MarkProjectileExpired(projectileEntityId);
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
                resolvedAreaHitCount = ResolveAreaCollisionHitsMainThread();
            }

            _lastResolvedAreaHitCount = resolvedAreaHitCount;
            _projectileResolvedEntityIds.Clear();
            ClearAreaCollisionFrameBuffers();
        }

        private void RecycleInactiveProjectiles()
        {
            _projectileRecycleEntityIds.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                ProjectileSimData projectile = _projectiles[i];
                if (!ShouldRecycleProjectile(projectile))
                {
                    continue;
                }

                _projectileRecycleEntityIds.Add(projectile.EntityId);
            }

            if (_projectileRecycleEntityIds.Count == 0)
            {
                return;
            }

            var entityComponent = GameEntry.Entity;
            for (int i = 0; i < _projectileRecycleEntityIds.Count; i++)
            {
                int entityId = _projectileRecycleEntityIds[i];
                if (entityComponent != null)
                {
                    entityComponent.HideEntity(entityId);
                }

                RemoveProjectileByEntityId(entityId);
            }

            _projectileRecycleEntityIds.Clear();
        }

        private bool ResolveProjectileHit(TargetableObject target, EntityBase sourceEntity, EntityBase ownerEntity,
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

            damage = CalculateProjectileDamage(sourceImpact.AttackBase, sourceImpact.AttackStat,
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

        private static int CalculateProjectileDamage(int attack, StatProperty attackStat, StatProperty defenseStat,
            StatProperty dodgeStat)
        {
            if (dodgeStat != null)
            {
                if (UnityEngine.Random.value < Mathf.Clamp(dodgeStat.Percent, 0f, 0.9f))
                {
                    return 0;
                }
            }

            float damage = attack;
            if (attackStat != null)
            {
                damage = (attack + attackStat.Value) * attackStat.Percent;
            }

            if (defenseStat != null)
            {
                damage = (damage - defenseStat.Value) / defenseStat.Percent;
            }

            if (damage < 1f)
            {
                return 1;
            }

            return Mathf.CeilToInt(damage);
        }

        private static bool TryGetTargetableEntity(int entityId, out TargetableObject target)
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

        private bool TryGetActiveProjectileData(int projectileEntityId, out int simulationIndex,
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

        private bool MarkProjectileExpired(int projectileEntityId)
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

        private int ResolveAreaCollisionHitsMainThread()
        {
            if (_areaCollisionHitEvents.Count == 0)
            {
                return 0;
            }

            int resolvedHitCount = 0;
            for (int i = 0; i < _areaCollisionHitEvents.Count; i++)
            {
                AreaCollisionHitEventData hitEvent = _areaCollisionHitEvents[i];
                if (!TryGetCollisionQueryById(hitEvent.QueryId, out CollisionQueryData query) ||
                    query.SourceType != CollisionSourceTypeArea)
                {
                    continue;
                }

                EntityBase sourceEntity = TryGetEntityById(hitEvent.SourceEntityId);
                if (sourceEntity == null || !sourceEntity.Available)
                {
                    continue;
                }

                if (!TryGetTargetableEntity(hitEvent.TargetEntityId, out TargetableObject target))
                {
                    continue;
                }

                if (!IsAreaTargetInsidePreciseShape(in query, target))
                {
                    continue;
                }

                AIUtility.PerformCollision(target, sourceEntity);
                resolvedHitCount++;
            }

            return resolvedHitCount;
        }

        private bool TryGetCollisionQueryById(int queryId, out CollisionQueryData query)
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

        private void ClearAreaCollisionFrameBuffers()
        {
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
        }

        private void BuildEnemyCollisionBucketsForProjectiles(float cellSize)
        {
            _enemyCollisionBuckets.Clear();
            for (int i = 0; i < _enemyJobOutputs.Length; i++)
            {
                EnemyJobOutputData enemy = _enemyJobOutputs[i];
                int cellX = (int)math.floor(enemy.Position.x / cellSize);
                int cellZ = (int)math.floor(enemy.Position.z / cellSize);
                _enemyCollisionBuckets.Add(SeparationCellKey(cellX, cellZ), i);
            }
        }

        private void QueryProjectileCollisionCandidates(float cellSize, bool hasEnemyTargets,
            out int projectileCandidateCount, out int areaCandidateCount)
        {
            projectileCandidateCount = 0;
            areaCandidateCount = 0;
            bool hasPlayerTarget = TryGetPlayerCollisionTarget(out int playerTargetEntityId, out float3 playerPosition);

            for (int i = 0; i < _collisionQueryInputs.Length; i++)
            {
                CollisionQueryData query = _collisionQueryInputs[i];
                float radiusSqr = query.Radius * query.Radius;
                int centerCellX = (int)math.floor(query.Position.x / cellSize);
                int centerCellZ = (int)math.floor(query.Position.z / cellSize);
                int queryRange = math.max(1, (int)math.ceil(query.Radius / cellSize));
                int selectedCount = 0;
                bool reachedLimit = false;

                if (hasPlayerTarget && query.SourceEntityId != playerTargetEntityId &&
                    query.SourceOwnerEntityId != playerTargetEntityId)
                {
                    playerPosition.y = query.Position.y;
                    float3 playerDelta = playerPosition - query.Position;
                    float playerSqrDistance = math.lengthsq(playerDelta);
                    // Log.Info(
                    //     $"playerPos:{playerPosition} - queryPos:{query.Position} = playerSqrDistance:{playerSqrDistance}");
                    if (playerSqrDistance <= radiusSqr)
                    {
                        AddCollisionCandidate(
                            query.QueryId,
                            query.SourceType,
                            query.SourceEntityId,
                            query.SourceOwnerEntityId,
                            playerTargetEntityId,
                            playerSqrDistance);
                        if (query.SourceType == CollisionSourceTypeProjectile)
                        {
                            projectileCandidateCount++;
                        }
                        else if (query.SourceType == CollisionSourceTypeArea)
                        {
                            areaCandidateCount++;
                        }
                    }
                }

                if (!hasEnemyTargets)
                {
                    continue;
                }

                for (int dx = -queryRange; dx <= queryRange && !reachedLimit; dx++)
                {
                    for (int dz = -queryRange; dz <= queryRange && !reachedLimit; dz++)
                    {
                        long key = SeparationCellKey(centerCellX + dx, centerCellZ + dz);
                        if (!_enemyCollisionBuckets.TryGetFirstValue(key, out int enemyIndex,
                                out NativeParallelMultiHashMapIterator<long> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            if (enemyIndex < 0 || enemyIndex >= _enemyJobOutputs.Length)
                            {
                                continue;
                            }

                            EnemyJobOutputData enemy = _enemyJobOutputs[enemyIndex];
                            if (enemy.EntityId == query.SourceOwnerEntityId)
                            {
                                continue;
                            }

                            float3 delta = new float3(
                                enemy.Position.x - query.Position.x,
                                enemy.Position.y - query.Position.y,
                                enemy.Position.z - query.Position.z);
                            float sqrDistance = math.lengthsq(delta);
                            if (sqrDistance > radiusSqr)
                            {
                                continue;
                            }

                            AddCollisionCandidate(
                                query.QueryId,
                                query.SourceType,
                                query.SourceEntityId,
                                query.SourceOwnerEntityId,
                                enemy.EntityId,
                                sqrDistance);
                            if (query.SourceType == CollisionSourceTypeProjectile)
                            {
                                projectileCandidateCount++;
                            }
                            else if (query.SourceType == CollisionSourceTypeArea)
                            {
                                areaCandidateCount++;
                            }
                            selectedCount++;

                            if (selectedCount >= query.MaxTargets)
                            {
                                reachedLimit = true;
                                break;
                            }
                        } while (_enemyCollisionBuckets.TryGetNextValue(out enemyIndex, ref iterator));
                    }
                }
            }
        }

        private static bool TryGetPlayerCollisionTarget(out int playerEntityId, out float3 playerPosition)
        {
            playerEntityId = PlayerEntityId;
            playerPosition = default;

            if (!TryGetTargetableEntity(playerEntityId, out TargetableObject playerTarget))
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

        private static bool ShouldRecycleProjectile(in ProjectileSimData projectile)
        {
            if (!projectile.Active)
            {
                return true;
            }

            if (projectile.State == ProjectileStateExpired)
            {
                return true;
            }

            return projectile.LifeTime > 0f && projectile.Age >= projectile.LifeTime;
        }

        private static void ExecuteProjectileMovement(int index, NativeArray<ProjectileJobInputData> inputs,
            NativeArray<ProjectileJobOutputData> outputs, float deltaTime, float3 playerPosition,
            float maxSqrDistanceFromPlayer, float maxVerticalOffsetFromPlayer)
        {
            ProjectileJobInputData input = inputs[index];
            ProjectileJobOutputData output = new ProjectileJobOutputData
            {
                EntityId = input.EntityId,
                OwnerEntityId = input.OwnerEntityId,
                Position = input.Position,
                Forward = input.Forward,
                Velocity = input.Velocity,
                Speed = input.Speed,
                LifeTime = input.LifeTime,
                Age = input.Age,
                Active = input.Active,
                RemainingLifetime = input.RemainingLifetime,
                State = input.State
            };

            if (!input.Active)
            {
                output.State = ProjectileStateExpired;
                outputs[index] = output;
                return;
            }

            float3 position = new float3(input.Position.x, input.Position.y, input.Position.z);
            float3 forward = new float3(input.Forward.x, input.Forward.y, input.Forward.z);
            float3 velocity = new float3(input.Velocity.x, input.Velocity.y, input.Velocity.z);
            if (math.lengthsq(velocity) <= float.Epsilon && input.Speed > 0f)
            {
                float3 moveDirection = math.normalizesafe(forward, new float3(0f, 0f, 1f));
                velocity = moveDirection * input.Speed;
            }

            float3 nextPosition = position + velocity * deltaTime;
            float nextAge = math.max(0f, input.Age + deltaTime);
            float nextRemainingLifetime = input.RemainingLifetime;
            bool shouldExpire = false;

            if (input.LifeTime > 0f)
            {
                nextRemainingLifetime = math.max(0f, input.LifeTime - nextAge);
                shouldExpire = nextAge >= input.LifeTime;
            }
            else if (input.RemainingLifetime > 0f)
            {
                nextRemainingLifetime = math.max(0f, input.RemainingLifetime - deltaTime);
                shouldExpire = nextRemainingLifetime <= float.Epsilon;
            }

            if (!shouldExpire && maxSqrDistanceFromPlayer > 0f)
            {
                float3 horizontalDelta =
                    new float3(nextPosition.x - playerPosition.x, 0f, nextPosition.z - playerPosition.z);
                shouldExpire = math.lengthsq(horizontalDelta) > maxSqrDistanceFromPlayer;
            }

            if (!shouldExpire && maxVerticalOffsetFromPlayer > 0f)
            {
                shouldExpire = math.abs(nextPosition.y - playerPosition.y) > maxVerticalOffsetFromPlayer;
            }

            output.Position = new Vector3(nextPosition.x, nextPosition.y, nextPosition.z);
            output.Velocity = new Vector3(velocity.x, velocity.y, velocity.z);
            output.Age = nextAge;
            output.RemainingLifetime = nextRemainingLifetime;
            output.Active = !shouldExpire;
            output.State = shouldExpire ? ProjectileStateExpired : ProjectileStateActive;

            if (math.lengthsq(velocity) > float.Epsilon)
            {
                float3 moveForward = math.normalizesafe(velocity, forward);
                output.Forward = new Vector3(moveForward.x, moveForward.y, moveForward.z);
            }

            outputs[index] = output;
        }
    }
}
