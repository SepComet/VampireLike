using CustomDebugger;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Orchestrates per-tick simulation pipeline and enemy movement/separation jobs.
        [Header("敌人互斥参数")]
        [Tooltip("敌人互斥分桶使用的网格尺寸。小于等于 0 时，将根据敌人体积半径自动计算。")]
        [SerializeField]
        private float _enemySeparationCellSize = 0f;

        [Tooltip("每次迭代对互斥推力累积值的阻尼系数。数值越大，分离速度越快。")]
        [SerializeField]
        private float _enemySeparationPushDamping = 0.75f;

        [Tooltip("每次迭代允许的最大互斥位移步长（按敌人体积半径倍率计算）。")]
        [SerializeField]
        private float _enemySeparationMaxStepScale = 1f;

        [Tooltip("敌人进入攻击范围后，互斥位移是否保持为相对玩家方向的切向分量（避免被径向推离玩家）。")]
        [SerializeField]
        private bool _enemySeparationUseTangentialInAttackRange = true;

        [Tooltip("互斥推力方向突变时的时间平滑系数。越大越稳定，但响应越慢。")]
        [SerializeField]
        private float _enemySeparationPushSmoothing = 0.55f;

        private void TickSimulationPipeline(in SimulationTickContext context)
        {
            // 1. 早退分支：deltaTime <= 0 时只清理碰撞通道和统计，然后返回。
            if (context.DeltaTime <= 0f)
            {
                PrepareCollisionQueryAndCandidateChannels(0, 0, 0);
                ResetCollisionRuntimeStats();
                ClearAreaCollisionTransientBuffers();
                return;
            }

            JobHandle enemyMovementHandle = default;
            JobHandle projectileMovementHandle = default;
            JobHandle enemySeparationHandle = default;
            bool hasEnemySeparationJob = false;
            bool hasEnemySeparationCandidates = false;
            int enemySeparationCount = 0;
            float enemySeparationMaxRadius = 0.45f;

            // 2. BuildInput 阶段：
            // 把 _enemies/_projectiles 同步到 Native 输入，准备输出缓冲；
            // 统计是否需要敌人分离 Job；
            // 预估并准备碰撞查询/候选缓冲。
            using (CustomProfilerMarker.TickEnemies_BuildInput.Auto())
            {
                SyncSimulationStateToJobInputs();
                int enemyCount = _enemyJobInputs.Length;
                int projectileCount = _projectileJobInputs.Length;
                PrepareEnemyJobOutputBuffer(enemyCount);
                PrepareProjectileJobOutputBuffer(projectileCount);

                enemySeparationCount = enemyCount;
                for (int i = 0; i < enemyCount; i++)
                {
                    EnemyJobInputData input = _enemyJobInputs[i];
                    if (!input.AvoidEnemyOverlap)
                    {
                        continue;
                    }

                    hasEnemySeparationCandidates = true;
                    float radius = input.EnemyBodyRadius > 0f ? input.EnemyBodyRadius : 0.45f;
                    if (radius > enemySeparationMaxRadius)
                    {
                        enemySeparationMaxRadius = radius;
                    }
                }

                if (hasEnemySeparationCandidates)
                {
                    int separationBucketCapacity = Mathf.Max(128, enemyCount * 2);
                    PrepareEnemySeparationJobBuffers(enemyCount, separationBucketCapacity);
                }

                int projectileQueryCount = _projectiles.Count;
                int areaQueryCount = GetPendingAreaCollisionRequestCount();
                int queryCount = projectileQueryCount + areaQueryCount;
                int projectileExpectedCount = projectileQueryCount * Mathf.Max(1, _projectileMaxCandidatesPerQuery);
                int areaExpectedCount = EstimatePendingAreaCollisionCandidateCountFromRequests();
                int expectedCandidateCount = Mathf.Max(16, projectileExpectedCount + areaExpectedCount);
                int bucketCapacity = Mathf.Max(256, _enemies.Count * 2 + queryCount);
                PrepareCollisionQueryAndCandidateChannels(queryCount, expectedCandidateCount, bucketCapacity);
            }

            // 3. StateUpdate 阶段（调度两个移动 Job）
            using (CustomProfilerMarker.TickEnemies_StateUpdate.Auto())
            {
                enemyMovementHandle = ExecuteEnemyMovementJob(in context);
                projectileMovementHandle = ExecuteProjectileMovementJob(in context);
            }

            // 4. Schedule 阶段（可选分离 Job）
            // 如果有需要分离的敌人，调度：
            // BuildEnemySeparationBucketsBurstJob（依赖 EnemyMovement）
            // EnemySeparationBurstJob（依赖分桶 Job）
            // 然后把“敌人链路 handle”和“投射物移动 handle”合并。
            JobHandle simulationHandle;
            using (CustomProfilerMarker.TickEnemies_Schedule.Auto())
            {
                hasEnemySeparationJob = TryScheduleEnemySeparationFromJobOutput(
                    in context,
                    enemyMovementHandle,
                    hasEnemySeparationCandidates,
                    enemySeparationCount,
                    enemySeparationMaxRadius,
                    out enemySeparationHandle);
                JobHandle enemyHandle = hasEnemySeparationJob ? enemySeparationHandle : enemyMovementHandle;
                simulationHandle = JobHandle.CombineDependencies(enemyHandle, projectileMovementHandle);
            }

            // 5. Complete 阶段：等待上述 Job 全部完成。
            using (CustomProfilerMarker.TickEnemies_Complete.Auto())
            {
                simulationHandle.Complete();
            }

            // 6. 主线程后处理阶段：
            // - 把分离结果覆盖回敌人输出（如果有分离 Job）
            // - 构建碰撞候选（投射物查询 + area 请求查询 + 网格筛选）
            using (CustomProfilerMarker.TickEnemies_MainThreadCommit.Auto())
            {
                if (hasEnemySeparationJob)
                {
                    CommitEnemySeparationFromJobOutput(enemySeparationCount);
                }
            }

            using (CustomProfilerMarker.Collision.Auto())
            {
                PrepareCollisionCandidatesForFrame();
                CompleteCollisionCandidatesForFrame();
            }

            // 7. MainThreadCommit 阶段
            // - ApplyJobOutputsToSimulationState（写回 _enemies/_projectiles）
            // - ResolveCollisionCandidatesOnMainThread（命中结算、事件、范围碰撞）
            // - RecycleInactiveAndExpiredProjectiles（隐藏并移除失效投射物）
            using (CustomProfilerMarker.TickEnemies_WriteBack.Auto())
            {
                using (CustomProfilerMarker.TickEnemies_MainThreadCommit.Auto())
                {
                    ApplyJobOutputsToSimulationState();
                    ResolveCollisionCandidatesOnMainThread();
                    RecycleInactiveAndExpiredProjectiles();
                }
            }
        }

        private JobHandle ExecuteEnemyMovementJob(in SimulationTickContext context)
        {
            int enemyCount = _enemyJobInputs.Length;
            if (enemyCount == 0)
            {
                return default;
            }

            if (context.DeltaTime <= 0f)
            {
                CopyEnemyInputsToOutputs();
                return default;
            }

            float3 playerPosition = new float3(context.PlayerPosition.x, 0f, context.PlayerPosition.z);
            NativeArray<EnemyJobInputData> inputArray = _enemyJobInputs.AsArray();
            NativeArray<EnemyJobOutputData> outputArray = _enemyJobOutputs.AsArray();

            EnemyMovementBurstJob burstJob = new EnemyMovementBurstJob
            {
                Inputs = inputArray,
                Outputs = outputArray,
                DeltaTime = context.DeltaTime,
                PlayerPosition = playerPosition
            };
            return burstJob.Schedule(enemyCount, 64);
        }

        private void CopyEnemyInputsToOutputs()
        {
            for (int i = 0; i < _enemyJobInputs.Length; i++)
            {
                EnemyJobInputData input = _enemyJobInputs[i];
                _enemyJobOutputs[i] = new EnemyJobOutputData
                {
                    EntityId = input.EntityId,
                    Position = input.Position,
                    Forward = input.Forward,
                    Rotation = input.Rotation,
                    Speed = input.Speed,
                    AttackRange = input.AttackRange,
                    AvoidEnemyOverlap = input.AvoidEnemyOverlap,
                    EnemyBodyRadius = input.EnemyBodyRadius,
                    SeparationIterations = input.SeparationIterations,
                    TargetType = input.TargetType,
                    State = input.State
                };
            }
        }

        private bool TryScheduleEnemySeparationFromJobOutput(in SimulationTickContext context, JobHandle dependency,
            bool hasSeparationCandidates, int enemyCount, float maxRadius, out JobHandle separationHandle)
        {
            separationHandle = dependency;
            if (enemyCount <= 0 || !hasSeparationCandidates)
            {
                return false;
            }

            float autoCellSize = maxRadius * 2f;
            float configuredCellSize = _enemySeparationCellSize > 0f ? _enemySeparationCellSize : autoCellSize;
            float cellSize = Mathf.Max(0.1f, configuredCellSize);
            float3 playerPosition = new float3(context.PlayerPosition.x, 0f, context.PlayerPosition.z);
            float pushDamping = Mathf.Clamp(_enemySeparationPushDamping, 0f, 2f);
            float maxStepScale = Mathf.Max(0.1f, _enemySeparationMaxStepScale);
            bool useTangentialInAttackRange = _enemySeparationUseTangentialInAttackRange;
            float pushSmoothing = Mathf.Clamp01(_enemySeparationPushSmoothing);

            NativeArray<EnemyJobOutputData> inputArray = _enemyJobOutputs.AsArray();
            NativeArray<EnemyJobOutputData> separatedOutputArray = _enemyJobSeparationOutputs.AsArray();
            NativeArray<float2> previousPushes = _enemySeparationPreviousPushes.AsArray();
            NativeArray<float2> currentPushes = _enemySeparationCurrentPushes.AsArray();


            BuildEnemySeparationBucketsBurstJob buildJob = new BuildEnemySeparationBucketsBurstJob
            {
                Inputs = inputArray,
                Buckets = _enemySeparationBuckets.AsParallelWriter(),
                CellSize = cellSize
            };
            JobHandle buildHandle = buildJob.Schedule(enemyCount, 64, dependency);
            EnemySeparationBurstJob separationJob = new EnemySeparationBurstJob
            {
                Inputs = inputArray,
                Buckets = _enemySeparationBuckets,
                PreviousPushes = previousPushes,
                Outputs = separatedOutputArray,
                CurrentPushes = currentPushes,
                CellSize = cellSize,
                MaxRadius = maxRadius,
                PlayerPosition = playerPosition,
                PushDamping = pushDamping,
                MaxStepScale = maxStepScale,
                UseTangentialInAttackRange = useTangentialInAttackRange,
                PushSmoothing = pushSmoothing
            };
            separationHandle = separationJob.Schedule(enemyCount, 64, buildHandle);
            return true;
        }

        private void CommitEnemySeparationFromJobOutput(int enemyCount)
        {
            if (enemyCount <= 0)
            {
                return;
            }

            CommitEnemySeparationTemporalBuffers(enemyCount);
            for (int i = 0; i < enemyCount; i++)
            {
                _enemyJobOutputs[i] = _enemyJobSeparationOutputs[i];
            }
        }

        private static long SeparationCellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}