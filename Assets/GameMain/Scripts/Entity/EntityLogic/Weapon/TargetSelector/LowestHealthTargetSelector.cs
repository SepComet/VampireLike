using System.Collections.Generic;
using Components;
using Definition.Enum;
using CustomUtility;

namespace Entity.Weapon
{
    public sealed class LowestHealthTargetSelector : ITargetSelector
    {
        public EntityBase SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange)
        {
            if (weapon == null || candidates == null)
            {
                return null;
            }

            EntityBase target = null;
            float minHp = float.MaxValue;
            float maxSqr = maxSqrRange > 0f ? maxSqrRange : float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.Available) continue;

                if (maxSqrRange > 0f && AIUtility.GetSqrMagnitudeXZ(weapon, candidate) >= maxSqr) continue;

                if (!TryGetHealth(candidate, out var hp)) continue;

                if (hp >= minHp) continue;

                minHp = hp;
                target = candidate;
            }

            return target;
        }

        private static bool TryGetHealth(EntityBase target, out float health)
        {
            health = 0f;
            if (target == null) return false;

            var statComponent = target.GetComponent<StatComponent>();
            if (statComponent == null) return false;

            var hpStat = statComponent.GetStat(StatType.MaxHealth);
            if (hpStat == null) return false;

            health = hpStat.Value;
            return true;
        }
    }
}
