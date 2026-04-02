using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponLightningParamsData
    {
        public float HitRadius { get; set; }
        public float HoverHeight { get; set; }
    }

    public class WeaponLightningData : WeaponData
    {
        public WeaponLightningParamsData ParamsData { get; }

        public WeaponLightningData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponLightning, ownerId, ownerCamp)
        {
            ParamsData = ParseParams<WeaponLightningParamsData>();
        }
    }
}
