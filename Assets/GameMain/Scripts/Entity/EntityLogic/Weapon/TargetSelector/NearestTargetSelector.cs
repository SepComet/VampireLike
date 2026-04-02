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

            if (TrySelectFromSpatialIndex(weapon, maxSqrRange, out EntityBase indexedTarget))
            {
                return indexedTarget;
            }

            EntityBase target = null;
            float minSqrMagnitude = maxSqrRange > 0f ? maxSqrRange : float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.Available) continue;

                float sqrMagnitude = AIUtility.GetSqrMagnitudeXZ(weapon, candidate);
                if (sqrMagnitude >= minSqrMagnitude) continue;

                minSqrMagnitude = sqrMagnitude;
                target = candidate;
            }

            return target;
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
