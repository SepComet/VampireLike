using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponKnifeParamsData
    {
        public float HitRadius { get; set; }
    }

    public class WeaponKnifeData : WeaponData
    {
        public WeaponKnifeParamsData ParamsData { get; }

        public WeaponKnifeData(int entityId, int ownerId, CampType ownerCamp) : base(entityId, WeaponType.WeaponKnife,
            ownerId, ownerCamp)
        {
            ParamsData = ParseParams<WeaponKnifeParamsData>();
        }
    }
}
