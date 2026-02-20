using System.Collections.Generic;
using CustomUtility;

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
    }
}
