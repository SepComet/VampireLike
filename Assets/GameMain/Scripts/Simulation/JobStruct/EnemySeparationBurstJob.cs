using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
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
                ExecuteEnemySeparation(
                    index,
                    Inputs,
                    Buckets,
                    Outputs,
                    CellSize,
                    MaxRadius,
                    PlayerPosition,
                    PushDamping,
                    MaxStepScale,
                    UseTangentialInAttackRange,
                    PreviousPushes,
                    CurrentPushes,
                    PushSmoothing
                );
            }
        }

        private static void ExecuteEnemySeparation(
            int index,
            NativeArray<EnemyJobOutputData> inputs,
            NativeParallelMultiHashMap<long, int> buckets,
            NativeArray<EnemyJobOutputData> outputs,
            float cellSize,
            float maxRadius,
            float3 playerPosition,
            float pushDamping,
            float maxStepScale,
            bool useTangentialInAttackRange,
            NativeArray<float2> previousPushes,
            NativeArray<float2> currentPushes,
            float pushSmoothing
        )
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
    }
}