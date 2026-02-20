using System;
using System.Collections.Generic;
using DataTable;
using Definition.DataStruct;
using Definition.Enum;

namespace Entity.EntityData
{
    [Serializable]
    public class WeaponData : AccessoryObjectData
    {
        private DRWeapon _drWeapon;

        private int _entityTypeId = 0;

        public WeaponData(int entityId, WeaponType weaponType, int ownerId, CampType ownerCamp)
            : base(entityId, (int)weaponType, ownerId, ownerCamp)
        {
            _drWeapon = GameEntry.DataTable.GetDataTableRow<DRWeapon>((int)weaponType);

            if (_drWeapon == null)
            {
                throw new Exception($"Weapon data table row is missing, WeaponType='{weaponType}'.");
            }

            _entityTypeId = _drWeapon.EntityTypeId;
        }

        public WeaponType WeaponType => (WeaponType)_drWeapon.Id;

        public string GetParamsString(string paramsName)
        {
            if (!Params.TryGetValue(paramsName.ToLower(), out var value))
            {
                throw new Exception($"Parameter '{paramsName}' not found.");
            }

            return value;
        }

        /// <summary>
        /// 攻击力。
        /// </summary>
        public int Attack => _drWeapon.Attack;

        public int EntityTypeId => _entityTypeId;

        /// <summary>
        /// 武器名称。
        /// </summary>
        public string Title => _drWeapon.Title;

        /// <summary>
        /// 图标资源名称。
        /// </summary>
        public string IconAssetName => _drWeapon.IconAssetName;

        public ItemRarity Rarity => _drWeapon.Rarity;

        public int Price => _drWeapon.Price;

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
        public Dictionary<string, string> Params => _drWeapon.Pramas;

        /// <summary>
        /// 额外属性。
        /// </summary>
        public StatModifier[] Modifiers => _drWeapon.Modifiers;
    }
}