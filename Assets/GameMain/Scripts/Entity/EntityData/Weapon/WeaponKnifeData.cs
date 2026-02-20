using Definition.Enum;

namespace Entity.EntityData
{
    public class WeaponKnifeData : WeaponData
    {
        public WeaponKnifeData(int entityId, int ownerId, CampType ownerCamp) : base(entityId, WeaponType.WeaponKnife,
            ownerId, ownerCamp)
        {
        }
    }
}