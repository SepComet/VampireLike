using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Job Data Lifecycle

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

        #endregion
    }
}
