using UnityEngine;

namespace Entity.Weapon
{
    public interface IWeaponAttackEffect
    {
        void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius);
    }
}