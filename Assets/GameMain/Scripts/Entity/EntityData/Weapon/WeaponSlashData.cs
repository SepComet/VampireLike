using System;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public sealed class WeaponSlashParamsData
    {
        public float SectorAngle { get; set; }
    }

    public class WeaponSlashData : WeaponData
    {
        public WeaponSlashParamsData ParamsData { get; }

        public WeaponSlashData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponSlash, ownerId, ownerCamp)
        {
            ParamsData = ParseParams<WeaponSlashParamsData>();
        }
    }
}
