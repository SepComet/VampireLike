using CustomDebugger;
using CustomUtility;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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

        private void TickEnemiesJobified(in SimulationTickContext context)
        {
            using (CustomProfilerMarker.TickEnemies_BuildInput.Auto())
            {
                SyncSimulationToJobInput();
            }

            using (CustomProfilerMarker.TickEnemies_StateUpdate.Auto())
            {
                ExecuteEnemyMovementJob(in context);
            }

            using (CustomProfilerMarker.TickEnemies_MoveSeparation.Auto())
            {
                ApplyEnemySeparationForJobOutput();
            }

            using (CustomProfilerMarker.TickEnemies_WriteBack.Auto())
            {
                SyncProjectilesToJobOutput();
                ApplyJobOutputToSimulation();
            }
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

        private void ApplyEnemySeparationForJobOutput()
        {
            if (_enemyJobOutputs.Length == 0)
            {
                return;
            }

            _enemySeparationAgents.Clear();
            for (int i = 0; i < _enemyJobOutputs.Length; i++)
            {
                EnemyJobOutputData output = _enemyJobOutputs[i];
                if (!output.AvoidEnemyOverlap)
                {
                    continue;
                }

                Vector3 position = output.Position;
                position.y = 0f;
                _enemySeparationAgents.Add(new EnemySeparationAgent
                {
                    AgentId = output.EntityId,
                    Position = position,
                    Radius = output.EnemyBodyRadius > 0f ? output.EnemyBodyRadius : 0.45f
                });
            }

            if (_enemySeparationAgents.Count == 0)
            {
                return;
            }

            EnemySeparationSolverProvider.SetSimulationAgents(_enemySeparationAgents);
            for (int i = 0; i < _enemyJobOutputs.Length; i++)
            {
                EnemyJobOutputData output = _enemyJobOutputs[i];
                if (!output.AvoidEnemyOverlap || output.State != EnemyStateChasing)
                {
                    continue;
                }

                Vector3 resolvedPosition = EnemySeparationSolverProvider.ResolveSimulation(
                    output.EntityId,
                    output.Position,
                    output.Forward,
                    output.SeparationIterations > 0 ? output.SeparationIterations : 1);

                output.Position = resolvedPosition;
                _enemyJobOutputs[i] = output;
            }
        }

        private static void ExecuteEnemyMovement(int index, NativeArray<EnemyJobInputData> inputs,
            NativeArray<EnemyJobOutputData> outputs, float deltaTime, float3 playerPosition)
        {
            EnemyJobInputData input = inputs[index];
            float attackRange = input.AttackRange > 0f ? input.AttackRange : DefaultAttackRange;
            float attackRangeSqr = attackRange * attackRange;

            float3 currentPosition = new float3(input.Position.x, input.Position.y, input.Position.z);
            float3 horizontalPosition = new float3(currentPosition.x, 0f, currentPosition.z);
            float3 toPlayer = playerPosition - horizontalPosition;
            float sqrDistance = math.lengthsq(toPlayer);
            bool isInAttackRange = sqrDistance <= attackRangeSqr;
            bool canChase = !isInAttackRange && input.Speed > 0f && sqrDistance > float.Epsilon;

            float3 forward = new float3(input.Forward.x, input.Forward.y, input.Forward.z);
            float3 desiredPosition = currentPosition;
            quaternion rotation = new quaternion(input.Rotation.x, input.Rotation.y, input.Rotation.z, input.Rotation.w);

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
                Position = new Vector3(desiredPosition.x, desiredPosition.y, desiredPosition.z),
                Forward = new Vector3(forward.x, forward.y, forward.z),
                Rotation = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w),
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
