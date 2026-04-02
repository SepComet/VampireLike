using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponHandgunParamsData
    {
    }

    public class WeaponHandgunData : WeaponData
    {
        public WeaponHandgunParamsData ParamsData { get; }

        public WeaponHandgunData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponHandgun, ownerId, ownerCamp)
        {
            ParamsData = ParseParams<WeaponHandgunParamsData>();
        }
    }
}
