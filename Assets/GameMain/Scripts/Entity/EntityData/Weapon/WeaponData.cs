using System;
using DataTable;
using Definition.DataStruct;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public class WeaponData : AccessoryObjectData
    {
        private DRWeapon _drWeapon;

        public WeaponData(int entityId, int typeId, int ownerId, CampType ownerCamp)
            : base(entityId, typeId, ownerId, ownerCamp)
        {
            _drWeapon = GameEntry.DataTable.GetDataTableRow<DRWeapon>(TypeId);
        }

        /// <summary>
        /// 攻击力。
        /// </summary>
        public int Attack => _drWeapon.Attack;

        /// <summary>
        /// 攻击间隔。
        /// </summary>
        public float Cooldown => _drWeapon.Cooldown;

        /// <summary>
        /// 攻击范围。
        /// </summary>
        public float AttackRange => _drWeapon.AttackRange;

        /// <summary>
        /// 攻击音效。
        /// </summary>
        public int AttackSoundId => _drWeapon.AttackSoundId;

        /// <summary>
        /// 额外参数。
        /// </summary>
        public string Params => _drWeapon.Pramas;

        /// <summary>
        /// 额外属性。
        /// </summary>
        public StatModifier[] Modifiers => _drWeapon.Modifiers;
    }
}