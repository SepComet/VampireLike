using Entity;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private sealed class TransformSync
        {
            private readonly SimulationWorld _world;

            public TransformSync(SimulationWorld world)
            {
                _world = world;
            }

            public void OnLateUpdate()
            {
                if (_world == null)
                {
                    return;
                }

                var enemyManager = GameEntry.EnemyManager;
                if (enemyManager != null && enemyManager.Enemies != null)
                {
                    var enemies = enemyManager.Enemies;
                    foreach (var enemy in enemies)
                    {
                        if (enemy is not EnemyBase enemyEntity || !enemyEntity.Available)
                        {
                            continue;
                        }

                        if (!_world.TryGetEnemyData(enemyEntity.Id, out EnemySimData enemyData))
                        {
                            continue;
                        }

                        ApplyEnemyPresentation(enemyEntity, enemyData);
                    }
                }

                var projectiles = _world._projectiles;
                for (int i = 0; i < projectiles.Count; i++)
                {
                    ProjectileSimData projectileData = projectiles[i];
                    if (!projectileData.Active || projectileData.State != ProjectileStateActive)
                    {
                        continue;
                    }

                    EntityBase projectileEntity = TryGetEntityById(projectileData.EntityId);
                    if (projectileEntity == null || !projectileEntity.Available)
                    {
                        continue;
                    }

                    ApplyProjectilePresentation(projectileEntity, projectileData);
                }
            }

            private static void ApplyEnemyPresentation(EnemyBase enemyEntity, in EnemySimData enemyData)
            {
                Transform enemyTransform = enemyEntity.CachedTransform;
                enemyTransform.position = enemyData.Position;

                Quaternion rotation = enemyData.Rotation;
                float rotationMagnitude = Mathf.Abs(rotation.x) + Mathf.Abs(rotation.y) + Mathf.Abs(rotation.z) +
                                          Mathf.Abs(rotation.w);
                if (rotationMagnitude > float.Epsilon)
                {
                    enemyTransform.rotation = rotation;
                    return;
                }

                Vector3 forward = enemyData.Forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > float.Epsilon)
                {
                    enemyTransform.forward = forward.normalized;
                }
            }

            private static void ApplyProjectilePresentation(EntityBase projectileEntity,
                in ProjectileSimData projectileData)
            {
                Transform projectileTransform = projectileEntity.CachedTransform;
                if (projectileTransform == null)
                {
                    return;
                }

                projectileTransform.position = projectileData.Position;
                Vector3 forward = projectileData.Forward;
                if (forward.sqrMagnitude <= float.Epsilon)
                {
                    return;
                }

                projectileTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }
    }
}
