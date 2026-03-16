using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Native buffers used by burst jobs and main-thread collision post-processing.
        private const int CollisionSourceTypeProjectile = 1;
        private const int CollisionSourceTypeArea = 2;
        private const int CollisionShapeCircle = 0;
        private const int CollisionShapeSector = 1;

        private NativeList<EnemyJobInputData> _enemyJobInputs;
        private NativeList<EnemyJobOutputData> _enemyJobOutputs;
        private NativeList<EnemyJobOutputData> _enemyJobSeparationOutputs;
        private NativeList<float2> _enemySeparationPreviousPushes;
        private NativeList<float2> _enemySeparationCurrentPushes;
        private NativeList<ProjectileJobInputData> _projectileJobInputs;
        private NativeList<ProjectileJobOutputData> _projectileJobOutputs;
        private NativeList<CollisionQueryData> _collisionQueryInputs;
        private NativeList<CollisionCandidateData> _collisionCandidates;
        private NativeParallelMultiHashMap<long, int> _enemySeparationBuckets;
        private NativeParallelMultiHashMap<long, int> _enemyCollisionBuckets;
        private readonly List<AreaCollisionRequestData> _areaCollisionRequests = new(16);
        private readonly List<AreaCollisionHitEventData> _areaCollisionHitEvents = new(32);
        private readonly HashSet<long> _areaCollisionHitDedupKeys = new();
        private int _lastCollisionQueryCount;
        private int _lastProjectileCollisionQueryCount;
        private int _lastAreaCollisionQueryCount;
        private int _lastCollisionCandidateCount;
        private int _lastProjectileCollisionCandidateCount;
        private int _lastAreaCollisionCandidateCount;
        private int _lastResolvedAreaHitCount;
        private float _lastCollisionCellSize;
        private bool _lastCollisionHasEnemyTargets;

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

        private void InitializeJobDataChannels()
        {
            if (AreJobDataChannelsUsable())
            {
                return;
            }

            DisposeJobDataChannels();
            _enemyJobInputs = new NativeList<EnemyJobInputData>(64, Allocator.Persistent);
            _enemyJobOutputs = new NativeList<EnemyJobOutputData>(64, Allocator.Persistent);
            _enemyJobSeparationOutputs = new NativeList<EnemyJobOutputData>(64, Allocator.Persistent);
            _enemySeparationPreviousPushes = new NativeList<float2>(64, Allocator.Persistent);
            _enemySeparationCurrentPushes = new NativeList<float2>(64, Allocator.Persistent);
            _projectileJobInputs = new NativeList<ProjectileJobInputData>(64, Allocator.Persistent);
            _projectileJobOutputs = new NativeList<ProjectileJobOutputData>(64, Allocator.Persistent);
            _collisionQueryInputs = new NativeList<CollisionQueryData>(64, Allocator.Persistent);
            _collisionCandidates = new NativeList<CollisionCandidateData>(128, Allocator.Persistent);
            _enemySeparationBuckets = new NativeParallelMultiHashMap<long, int>(256, Allocator.Persistent);
            _enemyCollisionBuckets = new NativeParallelMultiHashMap<long, int>(256, Allocator.Persistent);
            InitializeEnemyTargetSpatialIndex();
        }

        private void DisposeJobDataChannels()
        {
            if (_enemyJobInputs.IsCreated)
            {
                _enemyJobInputs.Dispose();
            }

            _enemyJobInputs = default;

            if (_enemyJobOutputs.IsCreated)
            {
                _enemyJobOutputs.Dispose();
            }

            _enemyJobOutputs = default;

            if (_enemyJobSeparationOutputs.IsCreated)
            {
                _enemyJobSeparationOutputs.Dispose();
            }

            _enemyJobSeparationOutputs = default;

            if (_enemySeparationPreviousPushes.IsCreated)
            {
                _enemySeparationPreviousPushes.Dispose();
            }

            _enemySeparationPreviousPushes = default;

            if (_enemySeparationCurrentPushes.IsCreated)
            {
                _enemySeparationCurrentPushes.Dispose();
            }

            _enemySeparationCurrentPushes = default;

            if (_projectileJobInputs.IsCreated)
            {
                _projectileJobInputs.Dispose();
            }

            _projectileJobInputs = default;

            if (_projectileJobOutputs.IsCreated)
            {
                _projectileJobOutputs.Dispose();
            }

            _projectileJobOutputs = default;

            if (_collisionQueryInputs.IsCreated)
            {
                _collisionQueryInputs.Dispose();
            }

            _collisionQueryInputs = default;

            if (_collisionCandidates.IsCreated)
            {
                _collisionCandidates.Dispose();
            }

            _collisionCandidates = default;

            if (_enemySeparationBuckets.IsCreated)
            {
                _enemySeparationBuckets.Dispose();
            }

            _enemySeparationBuckets = default;

            if (_enemyCollisionBuckets.IsCreated)
            {
                _enemyCollisionBuckets.Dispose();
            }

            _enemyCollisionBuckets = default;

            DisposeEnemyTargetSpatialIndex();
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
            ResetCollisionRuntimeStats();
        }

        private void ClearJobDataChannels()
        {
            if (!AreJobDataChannelsUsable())
            {
                _areaCollisionRequests.Clear();
                _areaCollisionHitEvents.Clear();
                _areaCollisionHitDedupKeys.Clear();
                ResetCollisionRuntimeStats();
                return;
            }

            if (_enemyJobInputs.IsCreated)
            {
                _enemyJobInputs.Clear();
            }

            if (_enemyJobOutputs.IsCreated)
            {
                _enemyJobOutputs.Clear();
            }

            if (_projectileJobInputs.IsCreated)
            {
                _projectileJobInputs.Clear();
            }

            if (_projectileJobOutputs.IsCreated)
            {
                _projectileJobOutputs.Clear();
            }

            if (_collisionQueryInputs.IsCreated)
            {
                _collisionQueryInputs.Clear();
            }

            if (_collisionCandidates.IsCreated)
            {
                _collisionCandidates.Clear();
            }

            if (_enemyJobSeparationOutputs.IsCreated)
            {
                _enemyJobSeparationOutputs.Clear();
            }

            if (_enemySeparationPreviousPushes.IsCreated)
            {
                _enemySeparationPreviousPushes.Clear();
            }

            if (_enemySeparationCurrentPushes.IsCreated)
            {
                _enemySeparationCurrentPushes.Clear();
            }

            if (_enemySeparationBuckets.IsCreated)
            {
                _enemySeparationBuckets.Clear();
            }

            if (_enemyCollisionBuckets.IsCreated)
            {
                _enemyCollisionBuckets.Clear();
            }

            ClearEnemyTargetSpatialIndex();
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
            ResetCollisionRuntimeStats();
        }

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

        private void SyncSimulationStateToJobInputs()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobInputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobInputs, _projectiles.Count);

            _enemyJobInputs.Clear();
            _projectileJobInputs.Clear();

            foreach (var data in _enemies)
            {
                _enemyJobInputs.Add(ConvertToEnemyJobInput(data));
            }

            foreach (var data in _projectiles)
            {
                _projectileJobInputs.Add(ConvertToProjectileJobInput(data));
            }
        }

        private void PrepareEnemyJobOutputBuffer(int enemyCount)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobOutputs, enemyCount);
            _enemyJobOutputs.Clear();

            if (enemyCount > 0)
            {
                _enemyJobOutputs.ResizeUninitialized(enemyCount);
            }
        }

        private void PrepareProjectileJobOutputBuffer(int projectileCount)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _projectileJobOutputs, projectileCount);
            _projectileJobOutputs.Clear();

            if (projectileCount > 0)
            {
                _projectileJobOutputs.ResizeUninitialized(projectileCount);
            }
        }

        private void CopyProjectileInputsToOutputs()
        {
            for (int i = 0; i < _projectileJobInputs.Length; i++)
            {
                ProjectileJobInputData input = _projectileJobInputs[i];
                _projectileJobOutputs[i] = new ProjectileJobOutputData
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
            }
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

        private void PrepareEnemySeparationJobBuffers(int enemyCount, int bucketCapacity)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobSeparationOutputs, enemyCount);
            _enemyJobSeparationOutputs.Clear();
            if (enemyCount > 0)
            {
                _enemyJobSeparationOutputs.ResizeUninitialized(enemyCount);
            }

            EnsureCapacity(ref _enemySeparationBuckets, bucketCapacity);
            _enemySeparationBuckets.Clear();

            EnsureCapacity(ref _enemySeparationPreviousPushes, enemyCount);
            EnsureCapacity(ref _enemySeparationCurrentPushes, enemyCount);

            if (_enemySeparationPreviousPushes.Length < enemyCount)
            {
                int oldLength = _enemySeparationPreviousPushes.Length;
                _enemySeparationPreviousPushes.ResizeUninitialized(enemyCount);
                for (int i = oldLength; i < enemyCount; i++)
                {
                    _enemySeparationPreviousPushes[i] = float2.zero;
                }
            }
            else if (_enemySeparationPreviousPushes.Length > enemyCount)
            {
                _enemySeparationPreviousPushes.ResizeUninitialized(enemyCount);
            }

            _enemySeparationCurrentPushes.Clear();
            if (enemyCount > 0)
            {
                _enemySeparationCurrentPushes.ResizeUninitialized(enemyCount);
            }
        }

        private void CommitEnemySeparationTemporalBuffers(int enemyCount)
        {
            if (!_enemySeparationPreviousPushes.IsCreated || !_enemySeparationCurrentPushes.IsCreated)
            {
                return;
            }

            int copyCount = math.min(enemyCount,
                math.min(_enemySeparationPreviousPushes.Length, _enemySeparationCurrentPushes.Length));
            for (int i = 0; i < copyCount; i++)
            {
                _enemySeparationPreviousPushes[i] = _enemySeparationCurrentPushes[i];
            }
        }

        private void OnEnemyAddedToSeparationTemporalBuffers()
        {
            if (_enemySeparationPreviousPushes.IsCreated)
            {
                _enemySeparationPreviousPushes.Add(float2.zero);
            }

            if (_enemySeparationCurrentPushes.IsCreated)
            {
                _enemySeparationCurrentPushes.Add(float2.zero);
            }
        }

        private void OnEnemyRemovedFromSeparationTemporalBuffers(int removedIndex)
        {
            if (_enemySeparationPreviousPushes.IsCreated && removedIndex >= 0 &&
                removedIndex < _enemySeparationPreviousPushes.Length)
            {
                _enemySeparationPreviousPushes.RemoveAtSwapBack(removedIndex);
            }

            if (_enemySeparationCurrentPushes.IsCreated && removedIndex >= 0 &&
                removedIndex < _enemySeparationCurrentPushes.Length)
            {
                _enemySeparationCurrentPushes.RemoveAtSwapBack(removedIndex);
            }
        }

        private void ApplyJobOutputsToSimulationState()
        {
            int enemyCount = Mathf.Min(_enemies.Count, _enemyJobOutputs.Length);
            bool hasEnemyPositionChanged = false;
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyJobOutputData output = _enemyJobOutputs[i];
                if (!hasEnemyPositionChanged)
                {
                    Vector3 currentPosition = _enemies[i].Position;
                    hasEnemyPositionChanged = currentPosition.x != output.Position.x ||
                                              currentPosition.z != output.Position.z;
                }

                _enemies[i] = ConvertToEnemySimData(output);
            }

            if (hasEnemyPositionChanged)
            {
                MarkEnemyTargetSpatialIndexDirty();
            }

            int projectileCount = Mathf.Min(_projectiles.Count, _projectileJobOutputs.Length);
            for (int i = 0; i < projectileCount; i++)
            {
                _projectiles[i] = ConvertToProjectileSimData(_projectileJobOutputs[i]);
            }
        }

        private bool AreJobDataChannelsUsable()
        {
            return IsNativeListUsable(_enemyJobInputs) &&
                   IsNativeListUsable(_enemyJobOutputs) &&
                   IsNativeListUsable(_enemyJobSeparationOutputs) &&
                   IsNativeListUsable(_enemySeparationPreviousPushes) &&
                   IsNativeListUsable(_enemySeparationCurrentPushes) &&
                   IsNativeListUsable(_projectileJobInputs) &&
                   IsNativeListUsable(_projectileJobOutputs) &&
                   IsNativeListUsable(_collisionQueryInputs) &&
                   IsNativeListUsable(_collisionCandidates) &&
                   IsNativeMultiHashMapUsable(_enemySeparationBuckets) &&
                   IsNativeMultiHashMapUsable(_enemyCollisionBuckets);
        }

        private static void EnsureCapacity<T>(ref NativeList<T> nativeList, int targetCount) where T : unmanaged
        {
            if (!nativeList.IsCreated || targetCount <= 0)
            {
                return;
            }

            if (nativeList.Capacity < targetCount)
            {
                nativeList.Capacity = targetCount;
            }
        }

        private static bool IsNativeListUsable<T>(NativeList<T> nativeList) where T : unmanaged
        {
            if (!nativeList.IsCreated)
            {
                return false;
            }

            try
            {
                _ = nativeList.Length;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private static bool IsNativeMultiHashMapUsable(NativeParallelMultiHashMap<long, int> hashMap)
        {
            if (!hashMap.IsCreated)
            {
                return false;
            }

            try
            {
                _ = hashMap.Count();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private static EnemyJobInputData ConvertToEnemyJobInput(in EnemySimData enemy)
        {
            return new EnemyJobInputData
            {
                EntityId = enemy.EntityId,
                Position = new float3(enemy.Position.x, enemy.Position.y, enemy.Position.z),
                Forward = new float3(enemy.Forward.x, enemy.Forward.y, enemy.Forward.z),
                Rotation = new quaternion(enemy.Rotation.x, enemy.Rotation.y, enemy.Rotation.z, enemy.Rotation.w),
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static EnemySimData ConvertToEnemySimData(in EnemyJobOutputData enemy)
        {
            return new EnemySimData
            {
                EntityId = enemy.EntityId,
                Position = new Vector3(enemy.Position.x, enemy.Position.y, enemy.Position.z),
                Forward = new Vector3(enemy.Forward.x, enemy.Forward.y, enemy.Forward.z),
                Rotation = new Quaternion(enemy.Rotation.value.x, enemy.Rotation.value.y, enemy.Rotation.value.z,
                    enemy.Rotation.value.w),
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static ProjectileJobInputData ConvertToProjectileJobInput(in ProjectileSimData projectile)
        {
            return new ProjectileJobInputData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = new float3(projectile.Position.x, projectile.Position.y, projectile.Position.z),
                Forward = new float3(projectile.Forward.x, projectile.Forward.y, projectile.Forward.z),
                Velocity = new float3(projectile.Velocity.x, projectile.Velocity.y, projectile.Velocity.z),
                Speed = projectile.Speed,
                LifeTime = projectile.LifeTime,
                Age = projectile.Age,
                Active = projectile.Active,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        private static ProjectileSimData ConvertToProjectileSimData(in ProjectileJobOutputData projectile)
        {
            return new ProjectileSimData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = new Vector3(projectile.Position.x, projectile.Position.y, projectile.Position.z),
                Forward = new Vector3(projectile.Forward.x, projectile.Forward.y, projectile.Forward.z),
                Velocity = new Vector3(projectile.Velocity.x, projectile.Velocity.y, projectile.Velocity.z),
                Speed = projectile.Speed,
                LifeTime = projectile.LifeTime,
                Age = projectile.Age,
                Active = projectile.Active,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        private static void EnsureCapacity(ref NativeParallelMultiHashMap<long, int> hashMap, int targetCount)
        {
            if (!hashMap.IsCreated || targetCount <= 0)
            {
                return;
            }

            if (hashMap.Capacity < targetCount)
            {
                hashMap.Capacity = targetCount;
            }
        }
    }
}
