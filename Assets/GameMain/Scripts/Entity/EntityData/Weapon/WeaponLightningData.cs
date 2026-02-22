using Definition.Enum;

namespace Entity.EntityData
{
    public class WeaponLightningData : WeaponData
    {
        public WeaponLightningData(int entityId, int ownerId, CampType ownerCamp)
            : base(entityId, WeaponType.WeaponLightning, ownerId, ownerCamp)
        {
        }
    }
}
