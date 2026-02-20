using System.Collections.Generic;

namespace Entity.Weapon
{
    public interface ITargetSelector
    {
        EntityBase SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange);
    }
}