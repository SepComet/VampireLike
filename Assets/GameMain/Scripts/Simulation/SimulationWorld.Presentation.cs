using UnityEngine;

namespace Simulation
{
    public partial class SimulationWorld
    {
        private sealed class Presentation
        {
            private readonly SimulationWorld _world;

            public Presentation(SimulationWorld world)
            {
                _world = world;
            }

            public void OnLateUpdate()
            {
                if (_world == null || !_world.UseSimulationMovement)
                {
                    return;
                }

                var enemyManager = GameEntry.EnemyManager;
                if (enemyManager == null || enemyManager.Enemies == null)
                {
                    return;
                }

                var enemies = enemyManager.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] is not EnemyBase enemyEntity || !enemyEntity.Available)
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
        }
    }
}