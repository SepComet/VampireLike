using System.Collections.Generic;
using Components;
using CustomUtility;

namespace Entity.Weapon
{
    public sealed class HighestHealthTargetSelector : ITargetSelector
    {
        public EntityBase SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange)
        {
            if (weapon == null || candidates == null)
            {
                return null;
            }

            EntityBase target = null;
            float maxHp = float.MinValue;
            float maxSqr = maxSqrRange > 0f ? maxSqrRange : float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.Available) continue;

                if (maxSqrRange > 0f && AIUtility.GetSqrMagnitudeXZ(weapon, candidate) >= maxSqr) continue;

                if (!TryGetCurrentHealth(candidate, out var hp)) continue;

                if (hp <= maxHp) continue;

                maxHp = hp;
                target = candidate;
            }

            return target;
        }

        private static bool TryGetCurrentHealth(EntityBase target, out float health)
        {
            health = 0f;
            if (target == null) return false;

            var healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent == null) return false;

            health = healthComponent.CurrentHealth;
            return true;
        }
    }
}
