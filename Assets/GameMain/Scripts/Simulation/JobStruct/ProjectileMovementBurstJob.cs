using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
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
                ExecuteProjectileMovement(
                    index,
                    Inputs,
                    Outputs,
                    DeltaTime,
                    PlayerPosition,
                    MaxSqrDistanceFromPlayer,
                    MaxVerticalOffsetFromPlayer);
            }
        }

        private static void ExecuteProjectileMovement(
            int index,
            NativeArray<ProjectileJobInputData> inputs,
            NativeArray<ProjectileJobOutputData> outputs,
            float deltaTime,
            float3 playerPosition,
            float maxSqrDistanceFromPlayer,
            float maxVerticalOffsetFromPlayer)
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

            float3 position = input.Position;
            float3 forward = input.Forward;
            float3 velocity = input.Velocity;
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

            output.Position = nextPosition;
            output.Velocity = velocity;
            output.Age = nextAge;
            output.RemainingLifetime = nextRemainingLifetime;
            output.Active = !shouldExpire;
            output.State = shouldExpire ? ProjectileStateExpired : ProjectileStateActive;

            if (math.lengthsq(velocity) > float.Epsilon)
            {
                float3 moveForward = math.normalizesafe(velocity, forward);
                output.Forward = moveForward;
            }

            outputs[index] = output;
        }
    }
}