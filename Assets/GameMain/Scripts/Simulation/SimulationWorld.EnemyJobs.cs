using CustomDebugger;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [Header("敌人互斥参数")] [Tooltip("敌人互斥分桶使用的网格尺寸。小于等于 0 时，将根据敌人体积半径自动计算。")] [SerializeField]
        private float _enemySeparationCellSize = 0f;

        [Tooltip("每次迭代对互斥推力累积值的阻尼系数。数值越大，分离速度越快。")] [SerializeField]
        private float _enemySeparationPushDamping = 0.75f;

        [Tooltip("每次迭代允许的最大互斥位移步长（按敌人体积半径倍率计算）。")] [SerializeField]
        private float _enemySeparationMaxStepScale = 1f;

        [Tooltip("敌人进入攻击范围后，互斥位移是否保持为相对玩家方向的切向分量（避免被径向推离玩家）。")] [SerializeField]
        private bool _enemySeparationUseTangentialInAttackRange = true;

        [Tooltip("互斥推力方向突变时的时间平滑系数。越大越稳定，但响应越慢。")] [SerializeField]
        private float _enemySeparationPushSmoothing = 0.55f;

        [BurstCompile]
        private struct EnemyMovementBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobInputData> Inputs;
            public NativeArray<EnemyJobOutputData> Outputs;
            public float DeltaTime;
            public float3 PlayerPosition;

            public void Execute(int index)
            {
                ExecuteEnemyMovement(index, Inputs, Outputs, DeltaTime, PlayerPosition);
            }
        }

        private struct EnemyMovementJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobInputData> Inputs;
            public NativeArray<EnemyJobOutputData> Outputs;
            public float DeltaTime;
            public float3 PlayerPosition;

            public void Execute(int index)
            {
                ExecuteEnemyMovement(index, Inputs, Outputs, DeltaTime, PlayerPosition);
            }
        }

        [BurstCompile]
        private struct BuildEnemySeparationBucketsBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> Inputs;
            public NativeParallelMultiHashMap<long, int>.ParallelWriter Buckets;
            public float CellSize;

            public void Execute(int index)
            {
                BuildEnemySeparationBucket(index, Inputs, Buckets, CellSize);
            }
        }

        private struct BuildEnemySeparationBucketsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> Inputs;
            public NativeParallelMultiHashMap<long, int>.ParallelWriter Buckets;
            public float CellSize;

            public void Execute(int index)
            {
                BuildEnemySeparationBucket(index, Inputs, Buckets, CellSize);
            }
        }

        [BurstCompile]
        private struct EnemySeparationBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> Inputs;
            [ReadOnly] public NativeParallelMultiHashMap<long, int> Buckets;
            [ReadOnly] public NativeArray<float2> PreviousPushes;
            public NativeArray<EnemyJobOutputData> Outputs;
            public NativeArray<float2> CurrentPushes;
            public float CellSize;
            public float MaxRadius;
            public float3 PlayerPosition;
            public float PushDamping;
            public float MaxStepScale;
            public bool UseTangentialInAttackRange;
            public float PushSmoothing;

            public void Execute(int index)
            {
                ExecuteEnemySeparation(index, Inputs, Buckets, Outputs, CellSize, MaxRadius, PlayerPosition,
                    PushDamping, MaxStepScale, UseTangentialInAttackRange, PreviousPushes, CurrentPushes,
                    PushSmoothing);
            }
        }

        private struct EnemySeparationJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> Inputs;
            [ReadOnly] public NativeParallelMultiHashMap<long, int> Buckets;
            [ReadOnly] public NativeArray<float2> PreviousPushes;
            public NativeArray<EnemyJobOutputData> Outputs;
            public NativeArray<float2> CurrentPushes;
            public float CellSize;
            public float MaxRadius;
            public float3 PlayerPosition;
            public float PushDamping;
            public float MaxStepScale;
            public bool UseTangentialInAttackRange;
            public float PushSmoothing;

            public void Execute(int index)
            {
                ExecuteEnemySeparation(index, Inputs, Buckets, Outputs, CellSize, MaxRadius, PlayerPosition,
                    PushDamping, MaxStepScale, UseTangentialInAttackRange, PreviousPushes, CurrentPushes,
                    PushSmoothing);
            }
        }

        private void TickEnemiesJobified(in SimulationTickContext context)
        {
            if (context.DeltaTime <= 0f)
            {
                PrepareCollisionCandidateChannels(0, 0, 0);
                ResetCollisionRuntimeStats();
                ClearAreaCollisionFrameBuffers();
                return;
            }

            using (CustomProfilerMarker.TickEnemies_BuildInput.Auto())
            {
                SyncSimulationToJobInput();
                int projectileQueryCount = _projectiles.Count;
                int areaQueryCount = GetPendingAreaCollisionQueryCount();
                int queryCount = projectileQueryCount + areaQueryCount;
                int projectileExpectedCount = projectileQueryCount * Mathf.Max(1, _projectileMaxCandidatesPerQuery);
                int areaExpectedCount = EstimatePendingAreaCollisionCandidateCount();
                int expectedCandidateCount = Mathf.Max(16, projectileExpectedCount + areaExpectedCount);
                int bucketCapacity = Mathf.Max(256, _enemies.Count * 2 + queryCount);
                PrepareCollisionCandidateChannels(queryCount, expectedCandidateCount, bucketCapacity);
            }

            using (CustomProfilerMarker.TickEnemies_StateUpdate.Auto())
            {
                ExecuteEnemyMovementJob(in context);
                ExecuteProjectileMovementJob(in context);
            }

            using (CustomProfilerMarker.TickEnemies_MoveSeparation.Auto())
            {
                ApplyEnemySeparationForJobOutput(in context);
                BuildProjectileCollisionCandidates();
            }

            using (CustomProfilerMarker.TickEnemies_WriteBack.Auto())
            {
                ApplyJobOutputToSimulation();
                ResolveProjectileCollisionCandidatesMainThread();
                RecycleInactiveProjectiles();
            }

            MarkEnemyTargetSpatialIndexDirty();
            BuildEnemyTargetSpatialIndexIfNeeded();
        }

        private void ExecuteEnemyMovementJob(in SimulationTickContext context)
        {
            int enemyCount = _enemyJobInputs.Length;
            PrepareEnemyJobOutputBuffer(enemyCount);

            if (enemyCount == 0)
            {
                return;
            }

            if (context.DeltaTime <= 0f)
            {
                CopyEnemyInputToOutput();
                return;
            }

            float3 playerPosition = new float3(context.PlayerPosition.x, 0f, context.PlayerPosition.z);
            NativeArray<EnemyJobInputData> inputArray = _enemyJobInputs.AsArray();
            NativeArray<EnemyJobOutputData> outputArray = _enemyJobOutputs.AsArray();

            JobHandle handle;
            if (_useBurstJobs)
            {
                EnemyMovementBurstJob burstJob = new EnemyMovementBurstJob
                {
                    Inputs = inputArray,
                    Outputs = outputArray,
                    DeltaTime = context.DeltaTime,
                    PlayerPosition = playerPosition
                };
                handle = burstJob.Schedule(enemyCount, 64);
            }
            else
            {
                EnemyMovementJob job = new EnemyMovementJob
                {
                    Inputs = inputArray,
                    Outputs = outputArray,
                    DeltaTime = context.DeltaTime,
                    PlayerPosition = playerPosition
                };
                handle = job.Schedule(enemyCount, 64);
            }

            handle.Complete();
        }

        private void CopyEnemyInputToOutput()
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

        private void ApplyEnemySeparationForJobOutput(in SimulationTickContext context)
        {
            int enemyCount = _enemyJobOutputs.Length;
            if (enemyCount == 0)
            {
                return;
            }

            bool hasSeparationCandidates = false;
            float maxRadius = 0.45f;
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyJobOutputData output = _enemyJobOutputs[i];
                if (!output.AvoidEnemyOverlap)
                {
                    continue;
                }

                hasSeparationCandidates = true;
                float radius = output.EnemyBodyRadius > 0f ? output.EnemyBodyRadius : 0.45f;
                if (radius > maxRadius)
                {
                    maxRadius = radius;
                }
            }

            if (!hasSeparationCandidates)
            {
                return;
            }

            float autoCellSize = maxRadius * 2f;
            float configuredCellSize = _enemySeparationCellSize > 0f ? _enemySeparationCellSize : autoCellSize;
            float cellSize = Mathf.Max(0.1f, configuredCellSize);
            int bucketCapacity = Mathf.Max(128, enemyCount * 2);
            PrepareEnemySeparationJobBuffers(enemyCount, bucketCapacity);
            float3 playerPosition = new float3(context.PlayerPosition.x, 0f, context.PlayerPosition.z);
            float pushDamping = Mathf.Clamp(_enemySeparationPushDamping, 0f, 2f);
            float maxStepScale = Mathf.Max(0.1f, _enemySeparationMaxStepScale);
            bool useTangentialInAttackRange = _enemySeparationUseTangentialInAttackRange;
            float pushSmoothing = Mathf.Clamp01(_enemySeparationPushSmoothing);

            NativeArray<EnemyJobOutputData> inputArray = _enemyJobOutputs.AsArray();
            NativeArray<EnemyJobOutputData> separatedOutputArray = _enemyJobSeparationOutputs.AsArray();
            NativeArray<float2> previousPushes = _enemySeparationPreviousPushes.AsArray();
            NativeArray<float2> currentPushes = _enemySeparationCurrentPushes.AsArray();
            JobHandle buildHandle;

            if (_useBurstJobs)
            {
                BuildEnemySeparationBucketsBurstJob buildJob = new BuildEnemySeparationBucketsBurstJob
                {
                    Inputs = inputArray,
                    Buckets = _enemySeparationBuckets.AsParallelWriter(),
                    CellSize = cellSize
                };
                buildHandle = buildJob.Schedule(enemyCount, 64);
            }
            else
            {
                BuildEnemySeparationBucketsJob buildJob = new BuildEnemySeparationBucketsJob
                {
                    Inputs = inputArray,
                    Buckets = _enemySeparationBuckets.AsParallelWriter(),
                    CellSize = cellSize
                };
                buildHandle = buildJob.Schedule(enemyCount, 64);
            }

            JobHandle separationHandle;
            if (_useBurstJobs)
            {
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
            }
            else
            {
                EnemySeparationJob separationJob = new EnemySeparationJob
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
            }

            separationHandle.Complete();
            CommitEnemySeparationTemporalBuffers(enemyCount);
            for (int i = 0; i < enemyCount; i++)
            {
                _enemyJobOutputs[i] = _enemyJobSeparationOutputs[i];
            }
        }

        private static void BuildEnemySeparationBucket(int index, NativeArray<EnemyJobOutputData> inputs,
            NativeParallelMultiHashMap<long, int>.ParallelWriter buckets, float cellSize)
        {
            EnemyJobOutputData output = inputs[index];
            if (!output.AvoidEnemyOverlap)
            {
                return;
            }

            float3 position = output.Position;
            position.y = 0f;
            int cellX = (int)math.floor(position.x / cellSize);
            int cellZ = (int)math.floor(position.z / cellSize);
            buckets.Add(SeparationCellKey(cellX, cellZ), index);
        }

        private static void ExecuteEnemySeparation(int index, NativeArray<EnemyJobOutputData> inputs,
            NativeParallelMultiHashMap<long, int> buckets, NativeArray<EnemyJobOutputData> outputs,
            float cellSize, float maxRadius, float3 playerPosition, float pushDamping, float maxStepScale,
            bool useTangentialInAttackRange, NativeArray<float2> previousPushes,
            NativeArray<float2> currentPushes, float pushSmoothing)
        {
            currentPushes[index] = float2.zero;
            EnemyJobOutputData self = inputs[index];
            if (!self.AvoidEnemyOverlap)
            {
                outputs[index] = self;
                return;
            }

            float3 candidate = self.Position;
            candidate.y = 0f;
            float3 original = candidate;
            float3 fallback =
                math.normalizesafe(new float3(self.Forward.x, 0f, self.Forward.z), new float3(1f, 0f, 0f));
            float selfRadius = self.EnemyBodyRadius > 0f ? self.EnemyBodyRadius : 0.45f;
            int iterations = self.SeparationIterations > 0 ? self.SeparationIterations : 1;
            int queryRange = math.max(1, (int)math.ceil((selfRadius + maxRadius) / cellSize));

            for (int iter = 0; iter < iterations; iter++)
            {
                int cellX = (int)math.floor(candidate.x / cellSize);
                int cellZ = (int)math.floor(candidate.z / cellSize);
                float3 pushAccumulation = float3.zero;

                for (int dx = -queryRange; dx <= queryRange; dx++)
                {
                    for (int dz = -queryRange; dz <= queryRange; dz++)
                    {
                        long key = SeparationCellKey(cellX + dx, cellZ + dz);
                        if (!buckets.TryGetFirstValue(key, out int otherIndex,
                                out NativeParallelMultiHashMapIterator<long> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            if (otherIndex == index)
                            {
                                continue;
                            }

                            EnemyJobOutputData other = inputs[otherIndex];
                            if (!other.AvoidEnemyOverlap)
                            {
                                continue;
                            }

                            float otherRadius = other.EnemyBodyRadius > 0f ? other.EnemyBodyRadius : 0.45f;
                            float minDistance = selfRadius + otherRadius;
                            float minDistanceSqr = minDistance * minDistance;

                            float3 otherPosition = other.Position;
                            otherPosition.y = 0f;
                            float3 toSelf = candidate - otherPosition;
                            float sqrDistance = math.lengthsq(toSelf);

                            if (sqrDistance <= float.Epsilon)
                            {
                                float3 zeroDistanceAxis = GetZeroDistanceSeparationAxis(index, otherIndex);
                                float directionSign = index < otherIndex ? 1f : -1f;
                                pushAccumulation += zeroDistanceAxis * (selfRadius * 0.25f * directionSign);
                                continue;
                            }

                            if (sqrDistance >= minDistanceSqr)
                            {
                                continue;
                            }

                            float distance = math.sqrt(sqrDistance);
                            float penetration = minDistance - distance;
                            pushAccumulation += (toSelf / distance) * penetration;
                        } while (buckets.TryGetNextValue(out otherIndex, ref iterator));
                    }
                }

                if (math.lengthsq(pushAccumulation) <= float.Epsilon)
                {
                    continue;
                }

                float3 resolvedPush = pushAccumulation * pushDamping;

                float maxStep = selfRadius * maxStepScale;
                float pushLength = math.length(resolvedPush);
                if (pushLength > maxStep && pushLength > float.Epsilon)
                {
                    resolvedPush = resolvedPush / pushLength * maxStep;
                }

                candidate += resolvedPush;
            }

            float3 framePush = candidate - original;
            float2 previousPush2 = previousPushes[index];
            float3 previousPush = new float3(previousPush2.x, 0f, previousPush2.y);
            float3 smoothedPush = SmoothSeparationPush(framePush, previousPush, pushSmoothing);

            if (useTangentialInAttackRange && self.State == EnemyStateInAttackRange)
            {
                smoothedPush = ProjectToTangential(smoothedPush, playerPosition, original);
            }

            float maxTotalStep = selfRadius * maxStepScale * iterations;
            float smoothedLength = math.length(smoothedPush);
            if (smoothedLength > maxTotalStep && smoothedLength > float.Epsilon)
            {
                smoothedPush = smoothedPush / smoothedLength * maxTotalStep;
            }

            float3 finalPosition = original + smoothedPush;
            currentPushes[index] = new float2(smoothedPush.x, smoothedPush.z);
            self.Position = new float3(finalPosition.x, self.Position.y, finalPosition.z);
            if (math.lengthsq(smoothedPush) > float.Epsilon)
            {
                self.Forward = new float3(fallback.x, self.Forward.y, fallback.z);
            }

            outputs[index] = self;
        }

        private static float3 SmoothSeparationPush(float3 framePush, float3 previousPush, float pushSmoothing)
        {
            float frameLengthSqr = math.lengthsq(framePush);
            float previousLengthSqr = math.lengthsq(previousPush);

            if (frameLengthSqr <= float.Epsilon)
            {
                return float3.zero;
            }

            if (previousLengthSqr <= float.Epsilon || pushSmoothing <= 0f)
            {
                return framePush;
            }

            float frameLength = math.sqrt(frameLengthSqr);
            float previousLength = math.sqrt(previousLengthSqr);
            float3 frameDirection = framePush / frameLength;
            float3 previousDirection = previousPush / previousLength;
            float directionAlignment = math.dot(frameDirection, previousDirection);

            if (directionAlignment >= 0.35f)
            {
                return framePush;
            }

            float directionalFactor = math.saturate((0.35f - directionAlignment) / 1.35f);
            float smoothingStrength = pushSmoothing * directionalFactor;
            return math.lerp(framePush, previousPush, smoothingStrength);
        }

        private static float3 ProjectToTangential(float3 push, float3 playerPosition, float3 currentPosition)
        {
            if (math.lengthsq(push) <= float.Epsilon)
            {
                return push;
            }

            float3 toPlayer = playerPosition - currentPosition;
            float toPlayerSqr = math.lengthsq(toPlayer);
            if (toPlayerSqr <= float.Epsilon)
            {
                return push;
            }

            float3 radialDirection = toPlayer / math.sqrt(toPlayerSqr);
            float radialOffset = math.dot(push, radialDirection);
            return push - radialDirection * radialOffset;
        }

        private static long SeparationCellKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }

        private static float3 GetZeroDistanceSeparationAxis(int index, int otherIndex)
        {
            int lowIndex = math.min(index, otherIndex);
            int highIndex = math.max(index, otherIndex);
            uint pairHash = (uint)(lowIndex * 73856093) ^ (uint)(highIndex * 19349663);

            float axisX = (pairHash & 1023u) / 511.5f - 1f;
            float axisZ = ((pairHash >> 10) & 1023u) / 511.5f - 1f;
            float3 axis = new float3(axisX, 0f, axisZ);
            return math.normalizesafe(axis, new float3(1f, 0f, 0f));
        }

        private static void ExecuteEnemyMovement(int index, NativeArray<EnemyJobInputData> inputs,
            NativeArray<EnemyJobOutputData> outputs, float deltaTime, float3 playerPosition)
        {
            EnemyJobInputData input = inputs[index];
            float attackRange = input.AttackRange > 0f ? input.AttackRange : DefaultAttackRange;
            float attackRangeSqr = attackRange * attackRange;

            float3 currentPosition = input.Position;
            float3 horizontalPosition = new float3(currentPosition.x, 0f, currentPosition.z);
            float3 toPlayer = playerPosition - horizontalPosition;
            float sqrDistance = math.lengthsq(toPlayer);
            bool isInAttackRange = sqrDistance <= attackRangeSqr;
            bool canChase = !isInAttackRange && input.Speed > 0f && sqrDistance > float.Epsilon;

            float3 forward = input.Forward;
            float3 desiredPosition = currentPosition;
            quaternion rotation = input.Rotation;

            if (canChase)
            {
                forward = math.normalizesafe(toPlayer, forward);
                desiredPosition = currentPosition + forward * input.Speed * deltaTime;
                if (math.lengthsq(forward) > float.Epsilon)
                {
                    rotation = quaternion.LookRotationSafe(forward, math.up());
                }
            }

            int nextState;
            if (isInAttackRange)
            {
                nextState = EnemyStateInAttackRange;
            }
            else if (canChase)
            {
                nextState = EnemyStateChasing;
            }
            else
            {
                nextState = EnemyStateIdle;
            }

            outputs[index] = new EnemyJobOutputData
            {
                EntityId = input.EntityId,
                Position = desiredPosition,
                Forward = forward,
                Rotation = rotation,
                Speed = input.Speed,
                AttackRange = attackRange,
                AvoidEnemyOverlap = input.AvoidEnemyOverlap,
                EnemyBodyRadius = input.EnemyBodyRadius,
                SeparationIterations = input.SeparationIterations,
                TargetType = input.TargetType,
                State = nextState
            };
        }
    }
}
