using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct EnemyJobInputData
        {
            public int EntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float Speed;
            public float AttackRange;
            public bool AvoidEnemyOverlap;
            public float EnemyBodyRadius;
            public int SeparationIterations;
            public int TargetType;
            public int State;
        }

        private struct EnemyJobOutputData
        {
            public int EntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float Speed;
            public float AttackRange;
            public bool AvoidEnemyOverlap;
            public float EnemyBodyRadius;
            public int SeparationIterations;
            public int TargetType;
            public int State;
        }

        private struct ProjectileJobInputData
        {
            public int EntityId;
            public int OwnerEntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public float Speed;
            public float RemainingLifetime;
            public int State;
        }

        private struct ProjectileJobOutputData
        {
            public int EntityId;
            public int OwnerEntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public float Speed;
            public float RemainingLifetime;
            public int State;
        }

        private NativeList<EnemyJobInputData> _enemyJobInputs;
        private NativeList<EnemyJobOutputData> _enemyJobOutputs;
        private NativeList<EnemyJobOutputData> _enemyJobSeparationOutputs;
        private NativeList<float2> _enemySeparationPreviousPushes;
        private NativeList<float2> _enemySeparationCurrentPushes;
        private NativeList<ProjectileJobInputData> _projectileJobInputs;
        private NativeList<ProjectileJobOutputData> _projectileJobOutputs;
        private NativeParallelMultiHashMap<long, int> _enemySeparationBuckets;

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
            _enemySeparationBuckets = new NativeParallelMultiHashMap<long, int>(256, Allocator.Persistent);
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

            if (_enemySeparationBuckets.IsCreated)
            {
                _enemySeparationBuckets.Dispose();
            }
            _enemySeparationBuckets = default;

            DisposeEnemyTargetSpatialIndex();
        }

        private void ClearJobDataChannels()
        {
            if (!AreJobDataChannelsUsable())
            {
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

            ClearEnemyTargetSpatialIndex();
        }

        private void SyncSimulationToJobInput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobInputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobInputs, _projectiles.Count);

            _enemyJobInputs.Clear();
            _projectileJobInputs.Clear();

            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemyJobInputs.Add(ConvertToEnemyJobInput(_enemies[i]));
            }

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobInputs.Add(ConvertToProjectileJobInput(_projectiles[i]));
            }
        }

        private void SyncSimulationToJobOutput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobOutputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobOutputs, _projectiles.Count);

            _enemyJobOutputs.Clear();
            _projectileJobOutputs.Clear();

            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemyJobOutputs.Add(ConvertToEnemyJobOutput(_enemies[i]));
            }

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobOutputs.Add(ConvertToProjectileJobOutput(_projectiles[i]));
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

        private void SyncProjectilesToJobOutput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _projectileJobOutputs, _projectiles.Count);
            _projectileJobOutputs.Clear();

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobOutputs.Add(ConvertToProjectileJobOutput(_projectiles[i]));
            }
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

        private void ApplyJobOutputToSimulation()
        {
            int enemyCount = Mathf.Min(_enemies.Count, _enemyJobOutputs.Length);
            for (int i = 0; i < enemyCount; i++)
            {
                _enemies[i] = ConvertToEnemySimData(_enemyJobOutputs[i]);
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
                   IsNativeMultiHashMapUsable(_enemySeparationBuckets);
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
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static EnemyJobOutputData ConvertToEnemyJobOutput(in EnemySimData enemy)
        {
            return new EnemyJobOutputData
            {
                EntityId = enemy.EntityId,
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
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
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
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
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        private static ProjectileJobOutputData ConvertToProjectileJobOutput(in ProjectileSimData projectile)
        {
            return new ProjectileJobOutputData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
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
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
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
