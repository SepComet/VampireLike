using Definition.Enum;

namespace Entity.EntityData
{
    public class WeaponKnifeData : WeaponData
    {
        public WeaponKnifeData(int entityId, int typeId, int ownerId, CampType ownerCamp) : base(entityId, typeId,
            ownerId, ownerCamp)
        {
        }
    }
}