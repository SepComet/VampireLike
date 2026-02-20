using System.Collections.Generic;
using System.Text;
using DataTable;
using Definition.DataStruct;
using Entity.EntityData;
using Entity.Weapon;
using UnityGameFramework.Runtime;

namespace CustomUtility
{
    public static class ItemDescUtility
    {
        private static readonly Dictionary<string, string> _paramsDict = new()
        {
            {"hitradius", "伤害范围"},
            {"sectorangle", "攻击角度"}
        };
        
        public static string CreatePropDescription(StatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            foreach (StatModifier modifier in modifiers)
            {
                sb.Append(modifier);
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public static string CreateWeaponDescription(DRWeapon drWeapon)
        {
            if (drWeapon == null)
            {
                return null;
            }

            return CreateWeaponDescription(drWeapon.Attack, drWeapon.Cooldown, drWeapon.AttackRange, drWeapon.Modifiers,
                drWeapon.Pramas);
        }

        public static string CreateWeaponDescription(WeaponBase weapon)
        {
            if (weapon == null || weapon.WeaponData == null)
            {
                return null;
            }

            return CreateWeaponDescription(weapon.WeaponData);
        }

        public static string CreateWeaponDescription(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return null;
            }

            return CreateWeaponDescription(weaponData.Attack, weaponData.Cooldown,
                weaponData.AttackRange, weaponData.Modifiers, weaponData.Params);
        }

        public static string CreatePropDescription(PropItem prop)
        {
            if (prop == null)
            {
                return null;
            }

            return CreatePropDescription(prop.Modifiers);
        }

        private static string CreateWeaponDescription(int attack, float cooldown, float attackRange,
            StatModifier[] modifiers, Dictionary<string, string> @params)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"攻击: {attack}");
            sb.AppendLine($"冷却: {cooldown}");
            sb.AppendLine($"范围: {attackRange}");

            string modifiersDesc = CreatePropDescription(modifiers);
            if (!string.IsNullOrEmpty(modifiersDesc))
            {
                sb.Append(modifiersDesc);
            }

            foreach (var kvp in @params)
            {
                if (!_paramsDict.TryGetValue(kvp.Key, out string value))
                {
                    value = kvp.Key;
                    Log.Warning($"{kvp.Key} is not defined.");
                }
                sb.AppendLine($"{value}: {kvp.Value}");
            }

            return sb.ToString();
        }
    }
}