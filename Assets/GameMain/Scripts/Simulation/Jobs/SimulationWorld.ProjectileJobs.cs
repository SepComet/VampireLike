using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [Header("Projectile Simulation")]
        [Tooltip("Recycle projectile when horizontal distance to player exceeds this range. <=0 disables this rule.")]
        [SerializeField]
        private float _projectileMaxDistanceFromPlayer = 120f;

        [Tooltip("Recycle projectile when vertical offset to player exceeds this range. <=0 disables this rule.")]
        [SerializeField]
        private float _projectileMaxVerticalOffsetFromPlayer = 30f;

        #region Projectile Movement Job

        private JobHandle ExecuteProjectileMovementJob(in SimulationTickContext context)
        {
            int projectileCount = _projectileJobInputs.Length;
            if (projectileCount == 0)
            {
                return default;
            }

            if (context.DeltaTime <= 0f)
            {
                CopyProjectileInputsToOutputs();
                return default;
            }

            float maxDistance = Mathf.Max(0f, _projectileMaxDistanceFromPlayer);
            float maxSqrDistanceFromPlayer = maxDistance > 0f ? maxDistance * maxDistance : -1f;
            float maxVerticalOffsetFromPlayer = Mathf.Max(0f, _projectileMaxVerticalOffsetFromPlayer);
            float3 playerPosition =
                new float3(context.PlayerPosition.x, context.PlayerPosition.y, context.PlayerPosition.z);
            NativeArray<ProjectileJobInputData> inputArray = _projectileJobInputs.AsArray();
            NativeArray<ProjectileJobOutputData> outputArray = _projectileJobOutputs.AsArray();


            ProjectileMovementBurstJob burstJob = new ProjectileMovementBurstJob
            {
                Inputs = inputArray,
                Outputs = outputArray,
                DeltaTime = context.DeltaTime,
                PlayerPosition = playerPosition,
                MaxSqrDistanceFromPlayer = maxSqrDistanceFromPlayer,
                MaxVerticalOffsetFromPlayer = maxVerticalOffsetFromPlayer
            };
            return burstJob.Schedule(projectileCount, 64);
        }

        #endregion

        #region Projectile Cleanup

        private void RecycleInactiveAndExpiredProjectiles()
        {
            _projectileRecycleEntityIds.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                ProjectileSimData projectile = _projectiles[i];
                if (!ShouldRecycleProjectileSimData(projectile))
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

        private static bool ShouldRecycleProjectileSimData(in ProjectileSimData projectile)
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

        #endregion
    }
}
