using System.Collections.Generic;
using CustomUtility;
using CustomComponent;

namespace Entity.Weapon
{
    public sealed class NearestTargetSelector : ITargetSelector
    {
        public EntityBase SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange)
        {
            if (weapon == null || candidates == null)
            {
                return null;
            }

            return TrySelectFromSpatialIndex(weapon, maxSqrRange, out EntityBase indexedTarget) ? indexedTarget : null;
        }

        private static bool TrySelectFromSpatialIndex(WeaponBase weapon, float maxSqrRange, out EntityBase target)
        {
            target = null;
            if (weapon == null || maxSqrRange <= 0f || weapon.CachedTransform == null)
            {
                return false;
            }

            var simulationWorld = GameEntry.SimulationWorld;
            if (simulationWorld == null)
            {
                return false;
            }

            if (!simulationWorld.TryGetNearestEnemyEntityId(weapon.CachedTransform.position, maxSqrRange,
                    out int entityId))
            {
                return false;
            }

            EnemyManagerComponent enemyManager = GameEntry.EnemyManager;
            if (enemyManager == null || !enemyManager.TryGetEnemy(entityId, out EntityBase enemy))
            {
                return false;
            }

            target = enemy;
            return true;
        }
    }
}
