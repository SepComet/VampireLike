using Definition.Enum;

namespace Entity.EntityData
{
    public class WeaponHandgunData : WeaponData
    {
        public WeaponHandgunData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponHandgun, ownerId, ownerCamp)
        {
        }
    }
}
