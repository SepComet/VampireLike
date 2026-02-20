using Definition.Enum;

namespace Entity.EntityData
{
    public class WeaponSlashData : WeaponData
    {
        public WeaponSlashData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponSlash, ownerId, ownerCamp)
        {
        }
    }
}