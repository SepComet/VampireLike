using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [BurstCompile]
        private struct EnemyMovementBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobInputData> Inputs;
            public NativeArray<EnemyJobOutputData> Outputs;
            public float DeltaTime;
            public float3 PlayerPosition;

            public void Execute(int index)
            {
                ExecuteEnemyMovement(
                    index,
                    Inputs,
                    Outputs,
                    DeltaTime,
                    PlayerPosition
                );
            }
        }

        private static void ExecuteEnemyMovement(
            int index,
            NativeArray<EnemyJobInputData> inputs,
            NativeArray<EnemyJobOutputData> outputs,
            float deltaTime,
            float3 playerPosition
        )
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